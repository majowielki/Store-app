using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Store.Shared.Utility
{
    public enum Colors
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
        /// <summary>
        /// Get display name for color enum
        /// </summary>
        public static string GetDisplayName(this Colors color) => color switch
        {
            Colors.All => "All Colors",
            Colors.White => "White",
            Colors.Black => "Black",
            Colors.Gray => "Gray",
            Colors.Red => "Red",
            Colors.Blue => "Blue",
            Colors.Green => "Green",
            Colors.Yellow => "Yellow",
            Colors.Orange => "Orange",
            Colors.Purple => "Purple",
            Colors.Pink => "Pink",
            Colors.Brown => "Brown",
            Colors.Navy => "Navy Blue",
            Colors.Maroon => "Maroon",
            Colors.Teal => "Teal",
            Colors.Silver => "Silver",
            Colors.Gold => "Gold",
            _ => color.ToString()
        };

        /// <summary>
        /// Get hex color code for frontend styling
        /// </summary>
        public static string GetHexCode(this Colors color) => color switch
        {
            Colors.All => "#CCCCCC",
            Colors.White => "#FFFFFF",
            Colors.Black => "#000000",
            Colors.Gray => "#808080",
            Colors.Red => "#FF0000",
            Colors.Blue => "#0000FF",
            Colors.Green => "#008000",
            Colors.Yellow => "#FFFF00",
            Colors.Orange => "#FFA500",
            Colors.Purple => "#800080",
            Colors.Pink => "#FFC0CB",
            Colors.Brown => "#A52A2A",
            Colors.Navy => "#000080",
            Colors.Maroon => "#800000",
            Colors.Teal => "#008080",
            Colors.Silver => "#C0C0C0",
            Colors.Gold => "#FFD700",
            _ => "#CCCCCC"
        };

        /// <summary>
        /// Parse string to Colors enum, returns null if not found
        /// </summary>
        public static Colors? ParseColor(string colorName)
        {
            if (Enum.TryParse<Colors>(colorName, true, out var color))
                return color;
            return null;
        }

        /// <summary>
        /// Get all available colors as list
        /// </summary>
        public static List<ColorInfo> GetAllColors()
        {
            return Enum.GetValues<Colors>()
                .Select(c => new ColorInfo
                {
                    Name = c.ToString(),
                    DisplayName = c.GetDisplayName(),
                    HexCode = c.GetHexCode(),
                    Value = (int)c
                })
                .ToList();
        }
    }

    public class ColorInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string HexCode { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
