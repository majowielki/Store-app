using FluentValidation;
using Store.Contracts.Catalog;
using Store.ContentService.DTOs;
using Store.ContentService.Models;

namespace Store.ContentService.Validators;

public class MakerRequestValidator : AbstractValidator<MakerRequest>
{
    public MakerRequestValidator(TimeProvider time)
    {
        RuleFor(x => x.Slug).Slug();
        RuleFor(x => x.Name).Title();
        RuleFor(x => x.Company).IsInEnum().NotEqual(Company.All);
        RuleFor(x => x.Tagline).MaximumLength(ContentConstraints.LineMaxLength);
        RuleFor(x => x.Story).Markdown();
        RuleFor(x => x.Location).MaximumLength(ContentConstraints.LineMaxLength);
        RuleFor(x => x.FoundedYear).InclusiveBetween(1800, time.GetUtcNow().Year).When(x => x.FoundedYear.HasValue);
        RuleFor(x => x.CoverImage).Picture();
    }
}

public class CollectionRequestValidator : AbstractValidator<CollectionRequest>
{
    public CollectionRequestValidator()
    {
        RuleFor(x => x.Slug).Slug();
        RuleFor(x => x.Title).Title();
        RuleFor(x => x.Summary).MaximumLength(ContentConstraints.ShortTextMaxLength);
        RuleFor(x => x.Body).Markdown();
        RuleFor(x => x.CoverImage).Picture();
        RuleFor(x => x.ProductSlugs).ProductSlugs();
    }
}

public class ArticleRequestValidator : AbstractValidator<ArticleRequest>
{
    public ArticleRequestValidator()
    {
        RuleFor(x => x.Slug).Slug();
        RuleFor(x => x.Title).Title();
        RuleFor(x => x.Excerpt).MaximumLength(ContentConstraints.ShortTextMaxLength);
        RuleFor(x => x.Body).NotEmpty().Markdown();
        RuleFor(x => x.CoverImage).Picture();
        RuleFor(x => x.Author).MaximumLength(ContentConstraints.LineMaxLength);
        RuleFor(x => x.ProductSlugs).ProductSlugs();
    }
}

public class LookbookRequestValidator : AbstractValidator<LookbookRequest>
{
    public LookbookRequestValidator()
    {
        RuleFor(x => x.Slug).Slug();
        RuleFor(x => x.Title).Title();
        RuleFor(x => x.Summary).MaximumLength(ContentConstraints.ShortTextMaxLength);
        RuleFor(x => x.Image).Picture();
        RuleFor(x => x.Hotspots).NotNull()
            .Must(points => points.Count <= ContentConstraints.MaxHotspots).WithMessage($"At most {ContentConstraints.MaxHotspots} points.");
        RuleForEach(x => x.Hotspots).ChildRules(point =>
        {
            point.RuleFor(p => p.X).InclusiveBetween(0, 100);
            point.RuleFor(p => p.Y).InclusiveBetween(0, 100);
            point.RuleFor(p => p.ProductSlug).NotEmpty()
                .MaximumLength(ContentConstraints.ProductSlugMaxLength)
                .Matches(ContentConstraints.SlugPattern);
        });
    }
}
