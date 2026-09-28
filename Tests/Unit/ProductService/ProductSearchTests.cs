using Store.ProductService.Services;
using Xunit;

namespace Store.Tests.Unit.ProductService;

public class ProductSearchTests
{
    [Theory]
    [InlineData("Oak  table", new[] { "oak", "table" })]
    [InlineData("BOUCLÉ sofa!", new[] { "bouclé", "sofa" })]
    [InlineData("Łóżko, stół & krzesło", new[] { "łóżko", "stół", "krzesło" })]
    [InlineData("oak oak OAK", new[] { "oak" })]
    [InlineData("3-seater", new[] { "3", "seater" })]
    [InlineData("  ", new string[0])]
    [InlineData(null, new string[0])]
    public void A_search_becomes_plain_lower_case_words(string? text, string[] expected)
    {
        Assert.Equal(expected, ProductSearch.Words(text));
    }

    [Fact]
    public void Punctuation_cannot_reach_the_query_and_the_words_are_capped()
    {
        var words = ProductSearch.Words("a:* | b & !c (d) 'e' f g h i j k");

        Assert.Equal(ProductSearch.MaxWords, words.Count);
        Assert.Equal("a:* & b:* & c:* & d:* & e:* & f:* & g:* & h:*", ProductSearch.PrefixQuery(words));
        Assert.Null(ProductSearch.PrefixQuery([]));
    }

    [Theory]
    [InlineData("oak", 1)]
    [InlineData("sfoa", 2)]
    [InlineData("walnutt", 3)]
    public void Longer_words_may_be_further_from_their_correction(string word, int distance)
    {
        Assert.Equal(distance, ProductSearch.MaxDistance(word));
    }
}
