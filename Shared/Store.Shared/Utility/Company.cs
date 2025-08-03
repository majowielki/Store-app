using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Shared.Utility
{
    public enum Company
    {
        All,
        Modenza,
        Luxora,
        Artifex,
        Comfora,
        Homestead
    }

    public static class CompanyHelper
    {
        /// <summary>
        /// Get display name for company enum
        /// </summary>
        public static string GetDisplayName(this Company company) => company switch
        {
            Company.All => "All Companies",
            Company.Modenza => "Modenza",
            Company.Luxora => "Luxora",
            Company.Artifex => "Artifex",
            Company.Comfora => "Comfora",
            Company.Homestead => "Homestead",
            _ => company.ToString()
        };

        /// <summary>
        /// Get all companies except 'All'
        /// </summary>
        public static List<Company> GetActiveCompanies()
        {
            return Enum.GetValues<Company>()
                .Where(c => c != Company.All)
                .ToList();
        }

        /// <summary>
        /// Parse string to Company enum, returns null if not found
        /// </summary>
        public static Company? ParseCompany(string companyName)
        {
            if (Enum.TryParse<Company>(companyName, true, out var company))
                return company;
            return null;
        }

        /// <summary>
        /// Validate if company exists in our enum
        /// </summary>
        public static bool IsValidCompany(string companyName)
        {
            return ParseCompany(companyName).HasValue;
        }
    }
}
