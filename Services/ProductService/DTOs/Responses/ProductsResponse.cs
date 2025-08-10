namespace Store.ProductService.DTOs.Responses;

public class ProductsResponse
{
    public List<ProductData> Data { get; set; } = new();
    public ProductsMeta Meta { get; set; } = new();
}

public class ProductData
{
    public int Id { get; set; }
    public ProductAttributes Attributes { get; set; } = new();
}

public class ProductAttributes
{
    public string Category { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Featured { get; set; }
    public string Image { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string PublishedAt { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public List<string> Colors { get; set; } = new();
}

public class ProductsMeta
{
    public List<string> Categories { get; set; } = new();
    public List<string> Companies { get; set; } = new();
    public PaginationMeta Pagination { get; set; } = new();
}

public class PaginationMeta
{
    public int Page { get; set; }
    public int PageCount { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
}