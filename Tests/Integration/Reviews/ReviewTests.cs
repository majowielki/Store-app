using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Store.Contracts.Audit.V1;
using Store.Contracts.Orders.V1;
using Store.Contracts.Reviews.V1;
using Store.ReviewService.Data;
using Store.ReviewService.Services;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Reviews;

/// <summary>
/// Reviews end to end on the review service's side (ADR 012): the seeded ones find their products
/// and publish their ratings, a customer reviews what they paid for, the true administrator decides
/// what gets published, reports hide a review, and a demo visitor's reviews and reports stay in
/// their sign-in session and are gone a day later.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class ReviewTests : IClassFixture<ReviewApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static int _lastOrderId = 30000;
    private static int _lastProductId = 500;

    private readonly ReviewApiFactory _factory;

    public ReviewTests(ReviewApiFactory factory)
    {
        _factory = factory;
    }

    private static int NextProductId() => Interlocked.Increment(ref _lastProductId);

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    private HttpClient Customer(string id) => _factory.CreateClient().WithToken(TestTokens.Customer(id, "Anna", "Nowak"));

    private HttpClient Demo(Guid session) => _factory.CreateClient().WithToken(TestTokens.DemoUser(session));

    /// <summary>A paid order of <paramref name="userId"/>, taken in by the service.</summary>
    private async Task PayAsync(string userId, params int[] productIds)
    {
        var orderId = Interlocked.Increment(ref _lastOrderId);
        await _factory.Bus.Bus.Publish(new OrderPaid(orderId, userId, $"{userId}@test.local", "Anna Nowak", 100m, "visa", "4242",
            productIds.Select(id => new OrderItem(id, $"Product {id}", 1, 100m)).ToList(), null, null, DateTime.UtcNow));
        Assert.True(await Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed.Select<OrderPaid>(e => e.Context.Message.OrderId == orderId).Any()));
    }

    private static Task<HttpResponseMessage> WriteAsync(HttpClient client, int productId, int rating = 5, string body = "Solid oak, a lovely grain and nothing wobbles at all.", string? title = "Love it")
        => client.PostAsJsonAsync("/api/v1/reviews", new { productId, rating, title, body });

    private static async Task<Guid> WrittenAsync(HttpClient client, int productId, int rating = 5)
    {
        var response = await WriteAsync(client, productId, rating);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJson(response)).GetProperty("id").GetGuid();
    }

    private async Task<List<JsonElement>> PublishedAsync(int productId, HttpClient? client = null, string query = "")
    {
        using var anonymous = _factory.CreateClient();
        var page = await ReadJson(await (client ?? anonymous).GetAsync($"/api/v1/reviews?productId={productId}&pageSize=50{query}"));
        return page.GetProperty("items").EnumerateArray().ToList();
    }

    private async Task<JsonElement> SummaryAsync(int productId)
    {
        using var client = _factory.CreateClient();
        return await ReadJson(await client.GetAsync($"/api/v1/reviews/products/{productId}/summary"));
    }

    private async Task<HttpResponseMessage> ModerateAsync(Guid id, string decision, string? reason = null, HttpClient? admin = null)
    {
        using var trueAdmin = _factory.CreateClient().AsTrueAdmin();
        return await (admin ?? trueAdmin).PostAsJsonAsync("/api/v1/reviews/admin/moderate", new { ids = new[] { id }, decision, reason });
    }

    private Task<bool> RatingPublishedAsync(int productId, int count)
        => Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed
            .Select<ReviewSummaryChanged>(e => e.Context.Message.ProductId == productId && e.Context.Message.ReviewCount == count).Any());

    private Task<bool> AuditedAsync(string action, Guid reviewId)
        => Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed
            .Select<AuditEvent>(e => e.Context.Message.Action == action && e.Context.Message.EntityId == reviewId.ToString()).Any());

    [Fact]
    public async Task Seeded_reviews_find_their_products_and_publish_their_ratings()
    {
        var linker = _factory.Services.GetRequiredService<SeedReviewLinker>();
        // Every product but the one the catalogue does not know yet
        Assert.Equal(1, await linker.LinkAsync());

        var desk = FakeCatalog.Ids["oak-writing-desk"];
        var reviews = await PublishedAsync(desk);
        Assert.InRange(reviews.Count, 3, 6);
        Assert.All(reviews, r => Assert.True(r.GetProperty("verifiedPurchase").GetBoolean()));
        var summary = await SummaryAsync(desk);
        Assert.Equal(reviews.Count, summary.GetProperty("reviewCount").GetInt32());
        Assert.Equal(reviews.Count, summary.GetProperty("distribution").EnumerateObject().Sum(p => p.Value.GetInt32()));
        Assert.Equal(Math.Round((decimal)reviews.Average(r => r.GetProperty("rating").GetInt32()), 2), summary.GetProperty("averageRating").GetDecimal());
        Assert.True(await RatingPublishedAsync(desk, reviews.Count));

        var late = FakeCatalog.Ids[FakeCatalog.LateSlug];
        Assert.Empty(await PublishedAsync(late));
        _factory.Catalog.Show(FakeCatalog.LateSlug);
        Assert.Equal(0, await linker.LinkAsync());
        Assert.NotEmpty(await PublishedAsync(late));
        Assert.True(await Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed
            .Select<ReviewSummaryChanged>(e => e.Context.Message.ProductId == late && e.Context.Message.ReviewCount > 0).Any()));
    }

    [Fact]
    public async Task A_customer_reviews_a_paid_product_and_it_is_public_once_approved()
    {
        var product = NextProductId();
        using var customer = Customer("review-buyer");

        // Not bought yet
        var before = await ReadJson(await customer.GetAsync($"/api/v1/reviews/products/{product}/mine"));
        Assert.False(before.GetProperty("canReview").GetBoolean());
        Assert.Equal("notPurchased", before.GetProperty("reason").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await WriteAsync(customer, product)).StatusCode);

        await PayAsync("review-buyer", product);
        Assert.True((await ReadJson(await customer.GetAsync($"/api/v1/reviews/products/{product}/mine"))).GetProperty("canReview").GetBoolean());

        var written = await ReadJson(await WriteAsync(customer, product, rating: 4));
        var id = written.GetProperty("id").GetGuid();
        Assert.Equal("pending", written.GetProperty("status").GetString());
        Assert.Equal("Anna N.", written.GetProperty("authorName").GetString());

        // Waiting: its author sees it, nobody else does, and a second one is refused
        Assert.Empty(await PublishedAsync(product));
        var mine = await ReadJson(await customer.GetAsync($"/api/v1/reviews/products/{product}/mine"));
        Assert.Equal("alreadyReviewed", mine.GetProperty("reason").GetString());
        Assert.Equal(id, mine.GetProperty("review").GetProperty("id").GetGuid());
        Assert.Contains((await ReadJson(await customer.GetAsync("/api/v1/reviews/mine"))).EnumerateArray(), r => r.GetProperty("id").GetGuid() == id);
        Assert.Equal(HttpStatusCode.Conflict, (await WriteAsync(customer, product)).StatusCode);
        Assert.True(await AuditedAsync("REVIEW_SUBMITTED", id));

        // The queue shows it to the true administrator; the demo one decides nothing
        using var trueAdmin = _factory.CreateClient().AsTrueAdmin();
        var queue = await ReadJson(await trueAdmin.GetAsync($"/api/v1/reviews/admin?productId={product}"));
        var queued = Assert.Single(queue.GetProperty("items").EnumerateArray());
        Assert.Equal("review-buyer", queued.GetProperty("userId").GetString());
        using var demoAdmin = _factory.CreateClient().AsDemoAdmin();
        Assert.Equal(HttpStatusCode.Forbidden, (await ModerateAsync(id, "approve", admin: demoAdmin)).StatusCode);

        var approved = await ModerateAsync(id, "approve");
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.Equal(1, (await ReadJson(approved)).GetProperty("moderated").GetInt32());

        var published = Assert.Single(await PublishedAsync(product));
        Assert.Equal(id, published.GetProperty("id").GetGuid());
        var summary = await SummaryAsync(product);
        Assert.Equal(1, summary.GetProperty("reviewCount").GetInt32());
        Assert.Equal(4m, summary.GetProperty("averageRating").GetDecimal());
        Assert.Equal(1, summary.GetProperty("distribution").GetProperty("4").GetInt32());
        Assert.True(await RatingPublishedAsync(product, 1));
        Assert.True(await AuditedAsync("REVIEW_APPROVED", id));
    }

    [Fact]
    public async Task A_rejected_review_tells_its_author_why_and_may_be_written_again()
    {
        var product = NextProductId();
        using var customer = Customer("review-rejected");
        await PayAsync("review-rejected", product);
        var id = await WrittenAsync(customer, product);

        // A rejection needs a reason
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await ModerateAsync(id, "reject")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ModerateAsync(id, "reject", "It describes another product.")).StatusCode);

        var mine = await ReadJson(await customer.GetAsync($"/api/v1/reviews/products/{product}/mine"));
        Assert.True(mine.GetProperty("canReview").GetBoolean());
        Assert.Equal("rejected", mine.GetProperty("review").GetProperty("status").GetString());
        Assert.Equal("It describes another product.", mine.GetProperty("review").GetProperty("rejectionReason").GetString());
        Assert.True(await AuditedAsync("REVIEW_REJECTED", id));

        var rewritten = await ReadJson(await WriteAsync(customer, product, rating: 3, body: "The oak writing desk itself: sturdy, but the drawer runs a bit stiff."));
        Assert.Equal(id, rewritten.GetProperty("id").GetGuid());
        Assert.Equal("pending", rewritten.GetProperty("status").GetString());
        Assert.False(rewritten.TryGetProperty("rejectionReason", out var reason) && reason.ValueKind == JsonValueKind.String);
    }

    [Theory]
    [InlineData("Cheaper at www.cheap-sofas.example, honestly the same thing", "Links are not allowed")]
    [InlineData("Write to anna@example.test and I will send you photos", "E-mail addresses are not allowed")]
    [InlineData("Call +48 600 700 800 if you want to buy mine", "Phone numbers are not allowed")]
    [InlineData("Too short", "at least 20 characters")]
    public async Task The_automatic_checks_refuse_a_review_before_anyone_reads_it(string body, string message)
    {
        var product = NextProductId();
        using var customer = Customer("review-checks");
        await PayAsync("review-checks", product);

        var response = await WriteAsync(customer, product, body: body);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains(message, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_customer_writes_at_most_three_reviews_a_day()
    {
        int[] products = [NextProductId(), NextProductId(), NextProductId(), NextProductId()];
        using var customer = Customer("review-daily");
        await PayAsync("review-daily", products);

        foreach (var product in products[..3])
        {
            await WrittenAsync(customer, product);
        }

        var fourth = await WriteAsync(customer, products[3]);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, fourth.StatusCode);
        Assert.Contains("3 reviews a day", await fourth.Content.ReadAsStringAsync());
        Assert.Equal("dailyLimit", (await ReadJson(await customer.GetAsync($"/api/v1/reviews/products/{products[3]}/mine"))).GetProperty("reason").GetString());
    }

    [Fact]
    public async Task A_report_hides_a_review_until_the_administrator_shows_it_again()
    {
        var product = NextProductId();
        using var author = Customer("review-reported-author");
        await PayAsync("review-reported-author", product);
        var id = await WrittenAsync(author, product);
        await ModerateAsync(id, "approve");
        Assert.True(await RatingPublishedAsync(product, 1));

        // Nobody reports their own review
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await author.PostAsJsonAsync($"/api/v1/reviews/{id}/report", new { reason = "mine" })).StatusCode);

        using var reporter = Customer("review-reporter");
        Assert.Equal(HttpStatusCode.NoContent, (await reporter.PostAsJsonAsync($"/api/v1/reviews/{id}/report", new { reason = "Advertises another shop" })).StatusCode);

        Assert.Empty(await PublishedAsync(product));
        Assert.Equal(0, (await SummaryAsync(product)).GetProperty("reviewCount").GetInt32());
        Assert.True(await RatingPublishedAsync(product, 0));
        Assert.True(await AuditedAsync("REVIEW_REPORTED", id));
        // Its author still sees it, marked as reported; hidden, it cannot be reported again
        Assert.True((await ReadJson(await author.GetAsync($"/api/v1/reviews/products/{product}/mine"))).GetProperty("review").GetProperty("reported").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await reporter.PostAsJsonAsync($"/api/v1/reviews/{id}/report", new { })).StatusCode);

        using var trueAdmin = _factory.CreateClient().AsTrueAdmin();
        var reported = await ReadJson(await trueAdmin.GetAsync($"/api/v1/reviews/admin?status=reported&productId={product}"));
        var queued = Assert.Single(reported.GetProperty("items").EnumerateArray());
        Assert.Equal(1, queued.GetProperty("reportCount").GetInt32());
        Assert.Equal("Advertises another shop", Assert.Single(queued.GetProperty("reportReasons").EnumerateArray()).GetString());

        await ModerateAsync(id, "approve");
        Assert.Single(await PublishedAsync(product));
        Assert.True(await Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed
            .Select<ReviewSummaryChanged>(e => e.Context.Message.ProductId == product && e.Context.Message.ReviewCount == 1).Count() >= 2));
    }

    [Fact]
    public async Task The_demo_administrator_sees_the_queue_without_unread_texts_or_accounts()
    {
        var product = NextProductId();
        using var customer = Customer("review-masked");
        await PayAsync("review-masked", product);
        await WrittenAsync(customer, product);

        using var demoAdmin = _factory.CreateClient().AsDemoAdmin();
        var queue = await ReadJson(await demoAdmin.GetAsync($"/api/v1/reviews/admin?productId={product}"));

        var review = Assert.Single(queue.GetProperty("items").EnumerateArray());
        Assert.Equal(ReviewModeration.HiddenText, review.GetProperty("body").GetString());
        Assert.Equal(ReviewModeration.HiddenAuthor, review.GetProperty("authorName").GetString());
        Assert.Equal(ReviewModeration.AnonymizedUserId, review.GetProperty("userId").GetString());
        Assert.DoesNotContain("wobbles", queue.GetRawText());

        // Published text has been read by a person: the seeded reviews show in full
        var desk = FakeCatalog.Ids["oak-writing-desk"];
        var published = await ReadJson(await demoAdmin.GetAsync($"/api/v1/reviews/admin?status=published&productId={desk}"));
        Assert.All(published.GetProperty("items").EnumerateArray(), r => Assert.NotEqual(ReviewModeration.HiddenText, r.GetProperty("body").GetString()));
    }

    [Fact]
    public async Task A_demo_visitor_writes_and_reports_within_their_session_and_it_is_gone_a_day_later()
    {
        var product = NextProductId();
        var desk = FakeCatalog.Ids["oak-writing-desk"];
        await _factory.Services.GetRequiredService<SeedReviewLinker>().LinkAsync();
        await PayAsync("demo-user", product);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        using var visitor = Demo(first);
        using var nextVisitor = Demo(second);

        // One review per sign-in session, each seen only in its own
        var id = await WrittenAsync(visitor, product);
        Assert.Contains((await ReadJson(await visitor.GetAsync("/api/v1/reviews/mine"))).EnumerateArray(), r => r.GetProperty("id").GetGuid() == id);
        var theirs = await ReadJson(await nextVisitor.GetAsync($"/api/v1/reviews/products/{product}/mine"));
        Assert.True(theirs.GetProperty("canReview").GetBoolean());
        Assert.False(theirs.TryGetProperty("review", out var review) && review.ValueKind != JsonValueKind.Null);
        var otherId = await WrittenAsync(nextVisitor, product, rating: 2);
        Assert.NotEqual(id, otherId);

        // A demo report hides the review from that session only and leaves the rating alone
        var seeded = (await PublishedAsync(desk)).First().GetProperty("id").GetGuid();
        var count = (await SummaryAsync(desk)).GetProperty("reviewCount").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await visitor.PostAsJsonAsync($"/api/v1/reviews/{seeded}/report", new { reason = "test" })).StatusCode);
        Assert.DoesNotContain(await PublishedAsync(desk, visitor), r => r.GetProperty("id").GetGuid() == seeded);
        Assert.Contains(await PublishedAsync(desk, nextVisitor), r => r.GetProperty("id").GetGuid() == seeded);
        Assert.Contains(await PublishedAsync(desk), r => r.GetProperty("id").GetGuid() == seeded);
        Assert.Equal(count, (await SummaryAsync(desk)).GetProperty("reviewCount").GetInt32());

        // The administrator publishes the second visitor's review; a day later both are gone
        await ModerateAsync(otherId, "approve");
        Assert.True(await RatingPublishedAsync(product, 1));
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ReviewDbContext>();
            var past = DateTime.UtcNow.AddMinutes(-1);
            await context.Reviews.Where(r => r.Id == id || r.Id == otherId).ExecuteUpdateAsync(set => set.SetProperty(r => r.ExpiresAt, past));
            await context.ReviewReports.Where(r => r.DemoSessionId == first).ExecuteUpdateAsync(set => set.SetProperty(r => r.ExpiresAt, past));
        }

        Assert.True(await _factory.Services.GetRequiredService<DemoSandboxCleanup>().CleanAsync() >= 2);
        Assert.Empty((await ReadJson(await visitor.GetAsync("/api/v1/reviews/mine"))).EnumerateArray());
        Assert.Empty(await PublishedAsync(product));
        Assert.True(await RatingPublishedAsync(product, 0));
        Assert.Contains(await PublishedAsync(desk, visitor), r => r.GetProperty("id").GetGuid() == seeded);
    }

    [Fact]
    public async Task The_list_filters_and_sorts_and_the_ratings_come_for_many_products_at_once()
    {
        await _factory.Services.GetRequiredService<SeedReviewLinker>().LinkAsync();
        var desk = FakeCatalog.Ids["oak-writing-desk"];
        var sofa = FakeCatalog.Ids["boucle-modular-sofa"];

        var highest = (await PublishedAsync(desk, query: "&sort=highest")).Select(r => r.GetProperty("rating").GetInt32()).ToList();
        Assert.Equal(highest.OrderByDescending(r => r), highest);
        var lowest = (await PublishedAsync(desk, query: "&sort=lowest")).Select(r => r.GetProperty("rating").GetInt32()).ToList();
        Assert.Equal(lowest.Order(), lowest);
        var fives = await PublishedAsync(desk, query: "&rating=5");
        Assert.All(fives, r => Assert.Equal(5, r.GetProperty("rating").GetInt32()));
        Assert.Equal(highest.Count(r => r == 5), fives.Count);

        using var client = _factory.CreateClient();
        var summaries = (await ReadJson(await client.GetAsync($"/api/v1/reviews/summaries?ids={desk},{sofa},999999,oops"))).EnumerateArray().ToList();
        Assert.Equal(new[] { desk, sofa, 999999 }, summaries.Select(s => s.GetProperty("productId").GetInt32()));
        Assert.True(summaries[0].GetProperty("reviewCount").GetInt32() >= 3);
        Assert.Equal(0, summaries[2].GetProperty("reviewCount").GetInt32());
        Assert.Equal(0m, summaries[2].GetProperty("averageRating").GetDecimal());
    }

    [Fact]
    public async Task Writing_needs_a_signed_in_customer_and_moderating_an_administrator()
    {
        using var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await WriteAsync(anonymous, NextProductId())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/reviews/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/reviews/admin")).StatusCode);

        using var customer = _factory.CreateClient().AsUser("review-nosy");
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/v1/reviews/admin")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ModerateAsync(Guid.NewGuid(), "approve", admin: customer)).StatusCode);
    }
}
