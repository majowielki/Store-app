using Store.ProductService.Data;
using Store.ReviewService.Data;
using Store.ReviewService.DTOs;
using Store.ReviewService.Models;
using Store.ReviewService.Services;
using Store.ReviewService.Validators;
using Store.Tests.Unit.TestSupport;
using Xunit;

namespace Store.Tests.Unit.ReviewService;

public class ReviewContentRulesTests
{
    [Theory]
    [InlineData("Great sofa, more pictures on http://example.test/sofa", ReviewContentRules.LinkMessage)]
    [InlineData("Look at www.cheap-sofas.example for a better one", ReviewContentRules.LinkMessage)]
    [InlineData("Same sofa is cheaper at cheap-sofas.com, honestly", ReviewContentRules.LinkMessage)]
    [InlineData("Write to me at anna.nowak@example.test for photos", ReviewContentRules.EmailMessage)]
    [InlineData("Call me on +48 600 700 800 and I will tell you more", ReviewContentRules.PhoneMessage)]
    [InlineData("Ring 600-700-800 if you want the old one", ReviewContentRules.PhoneMessage)]
    [InlineData("This shitty chair broke after a week", ReviewContentRules.ProfanityMessage)]
    [InlineData("What the fuck, the legs are uneven", ReviewContentRules.ProfanityMessage)]
    [InlineData("Kurwa, znowu się rozkleiło po tygodniu", ReviewContentRules.ProfanityMessage)]
    [InlineData("Zajebisty stół, polecam każdemu", ReviewContentRules.ProfanityMessage)]
    public void Links_addresses_phone_numbers_and_swearing_are_refused(string text, string expected)
    {
        Assert.Equal(expected, ReviewContentRules.Problem(text));
    }

    [Theory]
    [InlineData("The table is 180 cm long and 90 cm wide; it came in 3 days, in 2 boxes.")]
    [InlineData("Classic shape, assembled in an hour. The cushions pass the Scunthorpe test.")]
    [InlineData("Order 12345 arrived on 12.09.2026 - lovely oak, 4.5 stars from me.")]
    [InlineData("Świetna komoda, szuflady chodzą cicho, a debiutujący u nas kolor pasuje idealnie.")]
    [InlineData("Shiitake-coloured linen, a bass-heavy speaker fits on the shelf.")]
    public void Ordinary_reviews_pass(string text)
    {
        Assert.Null(ReviewContentRules.Problem(text));
    }
}

public class DemoReviewsTests
{
    private static readonly DateTime Now = new(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Every_product_of_the_demo_catalogue_has_reviews_and_only_those_do()
    {
        var catalogue = DemoCatalogue.Products(DemoPictures.Links).Select(p => p.Slug).Order(StringComparer.Ordinal).ToList();

        Assert.Equal(catalogue, DemoReviews.Products.Keys.Order(StringComparer.Ordinal).ToList());
    }

    [Fact]
    public void The_reviews_pass_the_rules_a_customer_review_is_held_to()
    {
        foreach (var (slug, kind) in DemoReviews.Products)
        {
            var reviews = DemoReviews.For(slug, kind, Now);

            Assert.InRange(reviews.Count, 3, 6);
            foreach (var review in reviews)
            {
                Assert.InRange(review.Rating, 1, 5);
                Assert.InRange(review.Body.Length, ReviewConstraints.BodyMinLength, ReviewConstraints.BodyMaxLength);
                Assert.True(review.Title is null || review.Title.Length <= ReviewConstraints.TitleMaxLength);
                Assert.True(review.AuthorName.Length <= ReviewConstraints.AuthorNameMaxLength);
                Assert.Null(ReviewContentRules.Problem(review.Title));
                Assert.Null(ReviewContentRules.Problem(review.Body));
                Assert.Equal(ReviewStatus.Published, review.Status);
                Assert.Equal(ReviewSource.Seed, review.Source);
                Assert.Null(review.ProductId);
                Assert.True(review.CreatedAt < Now && review.CreatedAt > Now.AddYears(-1));
            }
        }
    }

    [Fact]
    public void A_product_gets_the_same_reviews_in_every_database()
    {
        var first = DemoReviews.For("oak-writing-desk", DemoReviews.Kind.Surface, Now);
        var second = DemoReviews.For("oak-writing-desk", DemoReviews.Kind.Surface, Now);

        Assert.Equal(first.Select(r => (r.AuthorName, r.Rating, r.Title, r.Body, r.CreatedAt)), second.Select(r => (r.AuthorName, r.Rating, r.Title, r.Body, r.CreatedAt)));
        Assert.NotEqual(first.Select(r => r.Body), DemoReviews.For("walnut-sideboard", DemoReviews.Kind.Surface, Now).Select(r => r.Body));
    }

    [Fact]
    public void The_ratings_lean_positive_but_are_not_all_fives()
    {
        var ratings = DemoReviews.Products.SelectMany(p => DemoReviews.For(p.Key, p.Value, Now)).Select(r => r.Rating).ToList();

        Assert.InRange(ratings.Average(), 3.8, 4.7);
        Assert.Contains(ratings, r => r <= 3);
    }
}

public class ReviewRequestRulesTests
{
    private static CreateReviewRequest Valid() => new()
    {
        ProductId = 7,
        Rating = 4,
        Title = "Solid and warm",
        Body = "The oak has a lovely grain and nothing wobbles."
    };

    [Fact]
    public void A_proper_review_passes()
    {
        Assert.True(new CreateReviewRequestValidator().Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Stars_are_one_to_five(int rating)
    {
        var request = Valid();
        request.Rating = rating;

        Assert.False(new CreateReviewRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void The_text_is_20_to_1000_characters_after_trimming()
    {
        var shortOne = Valid();
        shortOne.Body = "   Nice.              " + new string(' ', 30);
        var longOne = Valid();
        longOne.Body = new string('a', ReviewConstraints.BodyMaxLength + 1);

        Assert.False(new CreateReviewRequestValidator().Validate(shortOne).IsValid);
        Assert.False(new CreateReviewRequestValidator().Validate(longOne).IsValid);
    }

    [Fact]
    public void The_title_is_checked_like_the_text()
    {
        var request = Valid();
        request.Title = "see www.example.com";

        var result = new CreateReviewRequestValidator().Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReviewRequest.Title) && e.ErrorMessage == ReviewContentRules.LinkMessage);
    }

    [Fact]
    public void A_rejection_needs_a_reason_and_a_batch_has_at_most_a_hundred_reviews()
    {
        var validator = new ModerateReviewsRequestValidator();

        Assert.False(validator.Validate(new ModerateReviewsRequest { Ids = [Guid.NewGuid()], Decision = ModerationDecision.Reject }).IsValid);
        Assert.True(validator.Validate(new ModerateReviewsRequest { Ids = [Guid.NewGuid()], Decision = ModerationDecision.Reject, Reason = "Not about the product" }).IsValid);
        Assert.True(validator.Validate(new ModerateReviewsRequest { Ids = [Guid.NewGuid()], Decision = ModerationDecision.Approve }).IsValid);
        Assert.False(validator.Validate(new ModerateReviewsRequest { Ids = [], Decision = ModerationDecision.Approve }).IsValid);
        Assert.False(validator.Validate(new ModerateReviewsRequest
        {
            Ids = Enumerable.Range(0, ModerateReviewsRequestValidator.MaxBatch + 1).Select(_ => Guid.NewGuid()).ToList(),
            Decision = ModerationDecision.Approve
        }).IsValid);
    }

    [Theory]
    [InlineData("Anna", "nowak", null, "Anna N.")]
    [InlineData(" Kuba ", "", "Kuba Z.", "Kuba")]
    [InlineData(null, null, "Demo User", "Demo User")]
    [InlineData(null, null, "someone@example.test", "Customer")]
    [InlineData("", "", "", "Customer")]
    public void A_review_is_signed_with_a_first_name_and_an_initial(string? firstName, string? lastName, string? displayName, string expected)
    {
        Assert.Equal(expected, ReviewViewer.AuthorNameOf(firstName, lastName, displayName));
    }
}
