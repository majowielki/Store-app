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
