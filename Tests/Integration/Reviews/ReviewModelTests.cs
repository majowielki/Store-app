using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Store.Contracts.Audit;
using Store.Contracts.Audit.V1;
using Store.Contracts.Orders.V1;
using Store.Contracts.Reviews.V1;
using Store.ReviewService.Data;
using Store.ReviewService.Models;
using Store.ReviewService.Moderation;
using Store.Tests.Integration.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Store.Tests.Integration.Reviews;

/// <summary>
/// The review service with the model switched on (ADR 019) and played by <see cref="ScriptedReviewModel"/>:
/// a clean review publishes itself, a doubtful one waits with the model's reason, and without a
/// verdict a review waits for the administrator as it always did.
/// </summary>
[Collection(PostgresTests.Name)]
public sealed class ReviewModelTests : IClassFixture<ReviewModelApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static int _lastOrderId = 40000;
    private static int _lastProductId = 700;

    private readonly ReviewModelApiFactory _factory;

    public ReviewModelTests(ReviewModelApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
        => JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);

    /// <summary>A customer who paid for a new product and reviewed it; returns the product and the review.</summary>
    private async Task<(int Product, Guid Review)> ReviewAsync(string userId, string body, int rating = 4)
    {
        var product = Interlocked.Increment(ref _lastProductId);
        var orderId = Interlocked.Increment(ref _lastOrderId);
        await _factory.Bus.Bus.Publish(new OrderPaid(orderId, userId, $"{userId}@test.local", "Anna Nowak", 100m, "visa", "4242",
            [new OrderItem(product, $"Product {product}", 1, 100m)], null, null, DateTime.UtcNow));
        Assert.True(await Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed.Select<OrderPaid>(e => e.Context.Message.OrderId == orderId).Any()));

        using var customer = _factory.CreateClient().WithToken(TestTokens.Customer(userId, "Anna", "Nowak"));
        var response = await customer.PostAsJsonAsync("/api/v1/reviews", new { productId = product, rating, title = "Honest", body });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var written = await ReadJson(response);
        Assert.Equal("pending", written.GetProperty("status").GetString());
        return (product, written.GetProperty("id").GetGuid());
    }

    private static async Task<JsonElement> AdminViewAsync(int product, HttpClient admin)
    {
        var queue = await ReadJson(await admin.GetAsync($"/api/v1/reviews/admin?productId={product}&status=all"));
        return Assert.Single(queue.GetProperty("items").EnumerateArray());
    }

    private Task<bool> AuditedAsync(string action, Guid reviewId)
        => Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed
            .Select<AuditEvent>(e => e.Context.Message.Action == action && e.Context.Message.EntityId == reviewId.ToString()).Any());

    [Fact]
    public async Task A_clean_review_publishes_itself_bad_news_included()
    {
        var (product, id) = await ReviewAsync("model-clean", "The table wobbles on a flat floor and one leg is shorter.", rating: 1);

        Assert.True(await AuditedAsync(AuditActions.ReviewPublishedByModel, id));
        using var anonymous = _factory.CreateClient();
        var published = await ReadJson(await anonymous.GetAsync($"/api/v1/reviews?productId={product}"));
        Assert.Equal(id, Assert.Single(published.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.True(await Eventually.BecomesTrueAsync(() => _factory.Bus.Consumed
            .Select<ReviewSummaryChanged>(e => e.Context.Message.ProductId == product && e.Context.Message.ReviewCount == 1).Any()));

        using var trueAdmin = _factory.CreateClient().AsTrueAdmin();
        var seen = await AdminViewAsync(product, trueAdmin);
        Assert.Equal("published", seen.GetProperty("status").GetString());
        Assert.Equal("clean", seen.GetProperty("modelVerdict").GetString());
        Assert.Equal(ScriptedReviewModel.CleanReason, seen.GetProperty("modelReason").GetString());
        Assert.False(seen.TryGetProperty("moderatedBy", out var by) && by.ValueKind == JsonValueKind.String);
    }

    [Fact]
    public async Task A_doubtful_review_waits_for_the_administrator_with_the_models_reason()
    {
        var (product, id) = await ReviewAsync("model-doubtful", $"Lovely chair. {ScriptedReviewModel.DoubtfulMarker} for the rest.");

        Assert.True(await AuditedAsync(AuditActions.ReviewHeldByModel, id));
        using var trueAdmin = _factory.CreateClient().AsTrueAdmin();
        var held = await AdminViewAsync(product, trueAdmin);
        Assert.Equal("pending", held.GetProperty("status").GetString());
        Assert.Equal("doubtful", held.GetProperty("modelVerdict").GetString());
        Assert.Equal(ScriptedReviewModel.DoubtfulReason, held.GetProperty("modelReason").GetString());

        // The reason is about a text only the true administrator reads before it is published
        using var demoAdmin = _factory.CreateClient().AsDemoAdmin();
        var masked = await AdminViewAsync(product, demoAdmin);
        Assert.Equal("doubtful", masked.GetProperty("modelVerdict").GetString());
        Assert.False(masked.TryGetProperty("modelReason", out var reason) && reason.ValueKind == JsonValueKind.String);

        // The administrator still decides
        var approved = await trueAdmin.PostAsJsonAsync("/api/v1/reviews/admin/moderate", new { ids = new[] { id }, decision = "approve" });
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.Equal("published", (await AdminViewAsync(product, trueAdmin)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Without_a_verdict_a_review_waits_as_it_always_did()
    {
        var (product, _) = await ReviewAsync("model-silent", $"A sturdy bench. {ScriptedReviewModel.SilentMarker} today.");

        Assert.True(await Eventually.BecomesTrueAsync(() => Task.FromResult(_factory.Model.Read(ScriptedReviewModel.SilentMarker))));
        using var trueAdmin = _factory.CreateClient().AsTrueAdmin();
        var waiting = await AdminViewAsync(product, trueAdmin);
        Assert.Equal("pending", waiting.GetProperty("status").GetString());
        Assert.False(waiting.TryGetProperty("modelVerdict", out var verdict) && verdict.ValueKind == JsonValueKind.String);
    }
}

/// <summary>ReviewService with the model switched on and a scripted one in place of Claude.</summary>
public sealed class ReviewModelApiFactory : StoreApiFactory<ReviewDbContext>
{
    public ReviewModelApiFactory(PostgresFixture postgres) : base(postgres)
    {
    }

    public FakeCatalog Catalog { get; } = new();

    public ScriptedReviewModel Model { get; } = new();

    protected override string? DatabaseName => "store_review_model_test";

    protected override void ConfigureSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("Services:ProductService", "http://catalog.test");
        builder.UseSetting($"{ReviewModelOptions.SectionName}:{nameof(ReviewModelOptions.Enabled)}", "true");
        builder.UseSetting($"{ReviewModelOptions.SectionName}:{nameof(ReviewModelOptions.ApiKey)}", "integration-tests-model-key");
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => Catalog));
        services.RemoveAll<IReviewModel>();
        services.AddSingleton<IReviewModel>(Model);
    }

    protected override void ConfigureTestBus(IBusRegistrationConfigurator bus)
    {
        bus.AddConsumer<ReviewSummaryProbe>();
    }
}

/// <summary>
/// Plays the model: a review saying <see cref="DoubtfulMarker"/> is doubtful, one saying
/// <see cref="SilentMarker"/> gets no verdict (the model is unreachable), any other is clean.
/// </summary>
public sealed class ScriptedReviewModel : IReviewModel
{
    public const string DoubtfulMarker = "Ask me privately";
    public const string SilentMarker = "Nobody answers";
    public const string CleanReason = "An opinion about the product.";
    public const string DoubtfulReason = "It asks readers to get in touch.";

    private readonly System.Collections.Concurrent.ConcurrentBag<string> _read = [];

    /// <summary>Whether the model was asked about a review whose text mentions <paramref name="text"/>.</summary>
    public bool Read(string text) => _read.Any(body => body.Contains(text, StringComparison.Ordinal));

    public Task<ModelJudgement?> JudgeAsync(ReviewForModel review, CancellationToken cancellationToken = default)
    {
        _read.Add(review.Body);
        ModelJudgement? judgement = review.Body.Contains(SilentMarker, StringComparison.Ordinal) ? null
            : review.Body.Contains(DoubtfulMarker, StringComparison.Ordinal) ? new(ModelVerdict.Doubtful, DoubtfulReason)
            : new(ModelVerdict.Clean, CleanReason);
        return Task.FromResult(judgement);
    }
}
