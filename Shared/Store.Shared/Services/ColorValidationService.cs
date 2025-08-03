using Store.Shared.Utility;

namespace Store.Shared.Services;

public interface IColorValidationService
{
    bool IsValidColor(string colorName);
    List<string> ValidateColors(List<string> colors);
    ColorInfo? GetColorInfo(string colorName);
    List<ColorInfo> GetAvailableColors();
}

public class ColorValidationService : IColorValidationService
{
    public bool IsValidColor(string colorName)
    {
        return ColorHelper.ParseColor(colorName).HasValue;
    }

    public List<string> ValidateColors(List<string> colors)
    {
        return colors.Where(IsValidColor).Distinct().ToList();
    }

    public ColorInfo? GetColorInfo(string colorName)
    {
        var color = ColorHelper.ParseColor(colorName);
        if (!color.HasValue) return null;

        return new ColorInfo
        {
            Name = color.Value.ToString(),
            DisplayName = color.Value.GetDisplayName(),
            HexCode = color.Value.GetHexCode(),
            Value = (int)color.Value
        };
    }

    public List<ColorInfo> GetAvailableColors()
    {
        return ColorHelper.GetAllColors();
    }
}