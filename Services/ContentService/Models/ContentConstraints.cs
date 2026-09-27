namespace Store.ContentService.Models;

/// <summary>The limits the database schema and the request validators must agree on.</summary>
public static class ContentConstraints
{
    public const int SlugMaxLength = 120;
    public const int TitleMinLength = 3;
    public const int TitleMaxLength = 160;
    public const int ShortTextMaxLength = 500;
    public const int LineMaxLength = 200;
    public const int MarkdownMaxLength = 20000;
    public const int ImageMaxLength = 500;
    public const int ProductSlugMaxLength = 220;
    public const int MaxProducts = 40;
    public const int MaxHotspots = 20;

    /// <summary>Lowercase letters and digits in words joined by single dashes.</summary>
    public const string SlugPattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";
}
