using System;
using System.Collections.Generic;
using System.Linq;

namespace Store.Shared.Utility
{
    public enum Category
    {
        All,
        Sofas,
        Chairs,
        Tables,
        Beds,
        Mattresses,
        Desks,
        TVStands,
        Bookcases,
        Wardrobes,
        Dressers,
        Nightstands,
        Sideboards,
        Lighting,
        Rugs,
        Outdoor,
        Office,
        Kids,
        Bathroom,
        Kitchen,
        Entryway,
        Decor,
        Storage,
        Other
    }

    // High-level grouping to support UI filters
    public enum Group
    {
        All,
        Furniture,
        Kitchen,
        Bathroom,
        Decorations,
        Lamps,
        Kids,
        Garden
    }

    public static class CategoryHelper
    {
        public static string GetDisplayName(this Category category) => category switch
        {
            Category.All => "All Categories",
            Category.Sofas => "Sofas",
            Category.Chairs => "Chairs",
            Category.Tables => "Tables",
            Category.Beds => "Beds",
            Category.Mattresses => "Mattresses",
            Category.Desks => "Desks",
            Category.TVStands => "TV Stands",
            Category.Bookcases => "Bookcases",
            Category.Wardrobes => "Wardrobes",
            Category.Dressers => "Dressers",
            Category.Nightstands => "Nightstands",
            Category.Sideboards => "Sideboards",
            Category.Lighting => "Lighting",
            Category.Rugs => "Rugs",
            Category.Outdoor => "Outdoor Furniture",
            Category.Office => "Office Furniture",
            Category.Kids => "Kids Furniture",
            Category.Bathroom => "Bathroom Furniture",
            Category.Kitchen => "Kitchen Furniture",
            Category.Entryway => "Entryway Furniture",
            Category.Decor => "Decor",
            Category.Storage => "Storage",
            Category.Other => "Other",
            _ => category.ToString()
        };

        // Group display names for UI
        public static string GetDisplayName(this Group group) => group switch
        {
            Group.All => "All products",
            Group.Furniture => "Furniture",
            Group.Kitchen => "Kitchen",
            Group.Bathroom => "Bathroom",
            Group.Decorations => "Decorations",
            Group.Lamps => "Lamps",
            Group.Kids => "Kids",
            Group.Garden => "Garden",
            _ => group.ToString()
        };

        public static List<Category> GetActiveCategories()
        {
            return Enum.GetValues<Category>()
                .Where(c => c != Category.All)
                .ToList();
        }

        public static Category? ParseCategory(string categoryName)
        {
            if (Enum.TryParse<Category>(categoryName, true, out var category))
                return category;
            return null;
        }

        public static bool IsValidCategory(string categoryName)
        {
            return ParseCategory(categoryName).HasValue;
        }

        public static Group? ParseGroup(string groupName)
        {
            if (Enum.TryParse<Group>(groupName.Replace(" ", string.Empty), true, out var group))
                return group;
            return null;
        }

        public static Group? GetGroup(this Category category) => category switch
        {
            Category.Lighting => Group.Lamps,
            Category.Rugs or Category.Decor => Group.Decorations,
            Category.Kids => Group.Kids,
            Category.Outdoor => Group.Garden,
            Category.Bathroom => Group.Bathroom,
            Category.Kitchen => Group.Kitchen,
            // Everything else is considered Furniture by default
            Category.Sofas or Category.Chairs or Category.Tables or Category.TVStands or Category.Bookcases or Category.Wardrobes or Category.Dressers or Category.Nightstands or Category.Sideboards or Category.Mattresses or Category.Desks or Category.Entryway or Category.Office or Category.Storage
                => Group.Furniture,
            _ => Group.Furniture
        };

        public static IReadOnlyCollection<Category> GetCategories(this Group group) => group switch
        {
            Group.All => Enum.GetValues<Category>().Where(c => c != Category.All && c != Category.Other).ToArray(),
            Group.Furniture => new[] { Category.Sofas, Category.Chairs, Category.Tables, Category.TVStands, Category.Bookcases, Category.Wardrobes, Category.Dressers, Category.Nightstands, Category.Sideboards, Category.Mattresses, Category.Desks, Category.Entryway, Category.Office, Category.Storage, Category.Beds },
            Group.Kitchen => new[] { Category.Kitchen, Category.Tables, Category.Chairs, Category.Storage },
            Group.Bathroom => new[] { Category.Bathroom, Category.Storage },
            Group.Decorations => new[] { Category.Decor, Category.Rugs },
            Group.Lamps => new[] { Category.Lighting },
            Group.Kids => new[] { Category.Kids, Category.Beds, Category.Storage },
            Group.Garden => new[] { Category.Outdoor },
            _ => Enum.GetValues<Category>().Where(c => c != Category.All).ToArray()
        };
    }
}
