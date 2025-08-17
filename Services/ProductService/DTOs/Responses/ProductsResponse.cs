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
    public bool NewArrival { get; set; }
    public string Image { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string? SalePrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public string PublishedAt { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
    public List<string> Colors { get; set; } = new();
    public List<string> Groups { get; set; } = new();

    // New fields exposed to frontend
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? DepthCm { get; set; }
    public decimal? WeightKg { get; set; }
    public List<string> Materials { get; set; } = new();
}

// Generic option item for UI dropdowns
public class OptionItem
{
    public string Key { get; set; } = string.Empty; // slug or enum value lowercased
    public string Name { get; set; } = string.Empty; // display name
}

// Group with its available categories for UI mapping
public class GroupWithCategories
{
    public string Key { get; set; } = string.Empty; // group key (lowercase)
    public string Name { get; set; } = string.Empty; // display name
    public List<OptionItem> Categories { get; set; } = new(); // categories under this group
}

public class ProductsMeta
{
    public List<string> Categories { get; set; } = new();
    public List<string> Groups { get; set; } = new();
    public List<string> Companies { get; set; } = new();
    public List<string> Colors { get; set; } = new();
    public PaginationMeta Pagination { get; set; } = new();

    // New: groups with categories for UI dropdown mapping
    public List<GroupWithCategories> GroupCategoryMap { get; set; } = new();
}

public class PaginationMeta
{
    public int Page { get; set; }
    public int PageCount { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
}