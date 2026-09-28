namespace Store.ProductService.DTOs.Responses;

/// <summary>
/// The values the catalogue can be filtered by, for the shop's menus and the admin form.
/// Keys use the same spelling as the product fields ("tvStands", "modenza"); "all" comes
/// first so a menu can default to it.
/// </summary>
public class ProductsMeta
{
    public List<string> Categories { get; set; } = new();
    public List<string> Groups { get; set; } = new();
    public List<string> Companies { get; set; } = new();
    public List<string> Colors { get; set; } = new();

    /// <summary>Groups with the categories that belong to them, for dependent dropdowns.</summary>
    public List<GroupWithCategories> GroupCategoryMap { get; set; } = new();

    /// <summary>How many products each value would show with the rest of the query the meta was asked for.</summary>
    public FilterCounts Counts { get; set; } = new();

    /// <summary>The search the counts are for, when the typed one found nothing and was corrected; null otherwise.</summary>
    public string? SearchCorrection { get; set; }
}

/// <summary>Option of a dropdown: the key sent back in queries and the label to show.</summary>
public class OptionItem
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class GroupWithCategories
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<OptionItem> Categories { get; set; } = new();
}

/// <summary>
/// How many active products each filter value would show together with the other filters of the
/// query (a value's own filter left out, so the other values of the same menu keep their counts).
/// </summary>
public class FilterCounts
{
    /// <summary>Products matching the whole query.</summary>
    public int Total { get; set; }

    /// <summary>By category key ("tvStands"); a category without products is left out.</summary>
    public Dictionary<string, int> Categories { get; set; } = new();

    /// <summary>By company key.</summary>
    public Dictionary<string, int> Companies { get; set; } = new();

    /// <summary>By colour key.</summary>
    public Dictionary<string, int> Colors { get; set; } = new();

    /// <summary>By group key.</summary>
    public Dictionary<string, int> Groups { get; set; } = new();

    /// <summary>Products on sale.</summary>
    public int Sale { get; set; }

    /// <summary>New arrivals.</summary>
    public int NewArrival { get; set; }
}

/// <summary>What the search box shows while typing: the best matches and the correction of a mistyped search.</summary>
public class ProductSuggestions
{
    /// <summary>The search as typed.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>The search the results are for, when the typed one found nothing ("sofa" for "sfoa"); null otherwise.</summary>
    public string? Correction { get; set; }

    /// <summary>Active products matching it.</summary>
    public int TotalCount { get; set; }

    /// <summary>The best matches, the title's words first.</summary>
    public List<ProductResponse> Products { get; set; } = new();
}
