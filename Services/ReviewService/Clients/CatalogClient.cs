using Store.BuildingBlocks.Serialization;

namespace Store.ReviewService.Clients;

/// <summary>The catalogue as the review service sees it: the ids of products it knows by slug.</summary>
public interface ICatalogClient
{
    /// <summary>
    /// The ids of the active products among <paramref name="slugs"/>, by slug; a slug the
    /// catalogue does not know (or has retired) is left out.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> FindIdsAsync(IReadOnlyCollection<string> slugs, CancellationToken cancellationToken = default);
}

public sealed class CatalogClient : ICatalogClient
{
    // The catalogue's largest page; a longer list of slugs is asked for in parts
    private const int BatchSize = 100;

    private readonly HttpClient _httpClient;

    public CatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyDictionary<string, int>> FindIdsAsync(IReadOnlyCollection<string> slugs, CancellationToken cancellationToken = default)
    {
        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var batch in slugs.Distinct(StringComparer.Ordinal).Chunk(BatchSize))
        {
            var query = $"api/v1/products?slugs={Uri.EscapeDataString(string.Join(',', batch))}&pageSize={BatchSize}";
            var page = await _httpClient.GetFromJsonAsync<ProductPage>(new Uri(query, UriKind.Relative), StoreJson.Web, cancellationToken);
            foreach (var product in page?.Items ?? [])
            {
                ids[product.Slug] = product.Id;
            }
        }

        return ids;
    }

    private sealed record ProductPage(IReadOnlyList<ProductItem> Items);

    private sealed record ProductItem(int Id, string Slug);
}
