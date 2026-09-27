using Store.ProductService.Models;
using Xunit;

namespace Store.Tests.Unit.ProductService;

public class ProductSlugTests
{
    [Theory]
    [InlineData("Bouclé Modular Sofa", "boucle-modular-sofa")]
    [InlineData("Oak Dining Chair, set of 2", "oak-dining-chair-set-of-2")]
    [InlineData("3-Seater Sectional Sofa", "3-seater-sectional-sofa")]
    [InlineData("  Natural Latex Mattress 160x200  ", "natural-latex-mattress-160x200")]
    [InlineData("Żółta łódź – ÉTÉ", "zolta-lodz-ete")]
    [InlineData("!!!", ProductSlug.Fallback)]
    public void A_title_becomes_lowercase_words_joined_by_dashes(string title, string slug)
    {
        Assert.Equal(slug, ProductSlug.From(title));
    }

    [Fact]
    public void Every_accented_letter_has_a_plain_counterpart()
    {
        Assert.Equal(ProductSlug.Accented.Length, ProductSlug.Plain.Length);
    }
}
