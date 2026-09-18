namespace Store.Contracts.Catalog;

/// <summary>The colour variants a product can be sold in; "All" is the filter value meaning any.</summary>
public enum Color
{
    All,
    White,
    Black,
    Gray,
    Red,
    Blue,
    Green,
    Yellow,
    Orange,
    Purple,
    Pink,
    Brown,
    Navy,
    Maroon,
    Teal,
    Silver,
    Gold
}

public static class ColorHelper
{
    /// <summary>The label the shop shows for a colour.</summary>
    public static string GetDisplayName(this Color color) => color switch
    {
        Color.All => "All Colors",
        Color.Navy => "Navy Blue",
        _ => color.ToString()
    };
}
