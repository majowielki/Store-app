using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Shared.Utility
{
    public enum Category
    {
        All,
        Tables,
        Chairs,
        Kids,
        Sofas,
        Beds,
        Electronics,
        Clothing,
        Books,
        Home,
        Sports,
        Beauty,
        Toys,
        Other
    }

    public static class CategoryHelper
    {
        /// <summary>
        /// Get display name for category enum
        /// </summary>
        public static string GetDisplayName(this Category category) => category switch
        {
            Category.All => "All Categories",
            Category.Tables => "Tables",
            Category.Chairs => "Chairs",
            Category.Kids => "Kids Furniture",
            Category.Sofas => "Sofas",
            Category.Beds => "Beds",
            Category.Electronics => "Electronics",
            Category.Clothing => "Clothing",
            Category.Books => "Books",
            Category.Home => "Home Decor",
            Category.Sports => "Sports & Fitness",
            Category.Beauty => "Beauty",
            Category.Toys => "Toys",
            Category.Other => "Other",
            _ => category.ToString()
        };

        /// <summary>
        /// Get all categories except 'All'
        /// </summary>
        public static List<Category> GetActiveCategories()
        {
            return Enum.GetValues<Category>()
                .Where(c => c != Category.All)
                .ToList();
        }

        /// <summary>
        /// Parse string to Category enum, returns null if not found
        /// </summary>
        public static Category? ParseCategory(string categoryName)
        {
            if (Enum.TryParse<Category>(categoryName, true, out var category))
                return category;
            return null;
        }

        /// <summary>
        /// Validate if category exists in our enum
        /// </summary>
        public static bool IsValidCategory(string categoryName)
        {
            return ParseCategory(categoryName).HasValue;
        }
    }
}
