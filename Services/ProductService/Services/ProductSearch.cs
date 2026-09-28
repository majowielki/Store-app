using Microsoft.EntityFrameworkCore;
using Store.ProductService.Data;
using System.Text.RegularExpressions;

namespace Store.ProductService.Services;

/// <summary>What a search looks for: the words, and the words it was corrected to when the typed ones found nothing.</summary>
/// <param name="Words">The words matched, each as the start of a word in the product</param>
/// <param name="Correction">The corrected search as the shop shows it ("sofa" for "sfoa"); null when the typed words were used</param>
public sealed record SearchPlan(IReadOnlyList<string> Words, string? Correction)
{
    public static readonly SearchPlan None = new([], null);

    /// <summary>The text-search query: every word as a prefix ("oak:* &amp; tabl:*"); null without words.</summary>
    public string? Query => ProductSearch.PrefixQuery(Words);
}

/// <summary>
/// Full-text search of the catalogue. Every product carries a weighted tsvector (the title first,
/// then the category and the maker, then the description) that a GIN index serves; the typed
/// words match as word starts, so "oak tab" finds "Oak Table". A search that finds nothing is
/// corrected word by word to the nearest word of the catalogue's titles, categories and makers
/// (Levenshtein distance from fuzzystrmatch over the words ts_stat lists) - "sfoa" becomes "sofa",
/// which trigram similarity alone would miss.
/// </summary>
public sealed partial class ProductSearch
{
    /// <summary>The text-search configuration of the vector and the queries: English stemming ("sofas" is "sofa").</summary>
    public const string Config = "english";

    /// <summary>At most this many words of a search are used.</summary>
    public const int MaxWords = 8;

    /// <summary>
    /// The generated column's expression. store_unaccent (an immutable wrapper the migration
    /// creates) lets "boucle" find "Bouclé"; the category's enum name is split into words
    /// ("DiningTables" is "Dining Tables").
    /// </summary>
    public const string VectorSql =
        "setweight(to_tsvector('english'::regconfig, store_unaccent(\"Title\")), 'A') || " +
        "setweight(to_tsvector('english'::regconfig, store_unaccent(regexp_replace(\"Category\", '([a-z])([A-Z])', '\\1 \\2', 'g') || ' ' || \"Company\")), 'B') || " +
        "setweight(to_tsvector('english'::regconfig, store_unaccent(\"Description\")), 'C')";

    /// <summary>
    /// The nearest word to {0} within {1} edits, among the words a search is corrected to: the
    /// unstemmed words of the active products' titles, categories and makers, as ts_stat lists
    /// them. A constant; only the word and the distance are parameters.
    /// </summary>
    private const string NearestWordSql =
        "SELECT word AS \"Value\" FROM ts_stat('SELECT to_tsvector(''simple'', store_unaccent(\"Title\" || '' '' || " +
        "regexp_replace(\"Category\", ''([a-z])([A-Z])'', ''\\1 \\2'', ''g'') || '' '' || \"Company\")) FROM \"Products\" WHERE \"IsActive\"') " +
        "WHERE levenshtein(word, store_unaccent({0})) <= {1} ORDER BY levenshtein(word, store_unaccent({0})), ndoc DESC, word LIMIT 1";

    private readonly ProductDbContext _context;

    public ProductSearch(ProductDbContext context)
    {
        _context = context;
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex Word();

    /// <summary>
    /// The words of a search: lower case, letters and digits only, at most <see cref="MaxWords"/>.
    /// The accents go in the database (<see cref="ProductDbContext.Unaccent"/>), the way the vector
    /// drops them - the services run with invariant globalization, which cannot decompose "é".
    /// </summary>
    public static IReadOnlyList<string> Words(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return Word().Matches(text.ToLowerInvariant())
            .Select(match => match.Value)
            .Distinct()
            .Take(MaxWords)
            .ToList();
    }

    /// <summary>Every word as a prefix, all of them required: "oak:* &amp; tabl:*"; null without words.</summary>
    public static string? PrefixQuery(IReadOnlyList<string> words)
        => words.Count == 0 ? null : string.Join(" & ", words.Select(word => word + ":*"));

    /// <summary>How far a correction may be from the typed word: one edit for short words, two up to six letters, three beyond.</summary>
    public static int MaxDistance(string word) => word.Length switch
    {
        <= 3 => 1,
        <= 6 => 2,
        _ => 3
    };

    /// <summary>
    /// The words to search for. The typed ones when the catalogue has a match for them; otherwise
    /// each word no product starts a word with is replaced by the nearest catalogue word. The admin
    /// listing looks among the deleted products too (<paramref name="includeInactive"/>).
    /// </summary>
    public async Task<SearchPlan> PlanAsync(string? text, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var words = Words(text);
        if (words.Count == 0)
        {
            return SearchPlan.None;
        }

        if (await MatchesAsync(PrefixQuery(words)!, includeInactive, cancellationToken))
        {
            return new SearchPlan(words, null);
        }

        var corrected = new List<string>(words.Count);
        foreach (var word in words)
        {
            corrected.Add(await MatchesAsync(word + ":*", includeInactive, cancellationToken) ? word : await NearestAsync(word, cancellationToken) ?? word);
        }

        return corrected.SequenceEqual(words) ? new SearchPlan(words, null) : new SearchPlan(corrected, string.Join(' ', corrected));
    }

    private Task<bool> MatchesAsync(string query, bool includeInactive, CancellationToken cancellationToken)
        => _context.Products.AnyAsync(p => (includeInactive || p.IsActive) && p.SearchVector.Matches(EF.Functions.ToTsQuery(Config, ProductDbContext.Unaccent(query))), cancellationToken);

    /// <summary>The catalogue word nearest to <paramref name="word"/> within <see cref="MaxDistance"/>, the more common one on a tie.</summary>
    private Task<string?> NearestAsync(string word, CancellationToken cancellationToken)
        => _context.Database.SqlQueryRaw<string>(NearestWordSql, word, MaxDistance(word)).FirstOrDefaultAsync(cancellationToken);
}
