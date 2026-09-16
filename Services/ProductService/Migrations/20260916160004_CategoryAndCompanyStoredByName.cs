using Microsoft.EntityFrameworkCore.Migrations;
using Store.Contracts.Catalog;

#nullable disable

namespace Store.ProductService.Migrations
{
    /// <summary>
    /// Category and Company were integers, so inserting a value in the middle of either enum
    /// would silently re-label every stored product. They are stored by name from now on;
    /// existing rows are mapped with the enum as it was at the time of this migration.
    /// </summary>
    public partial class CategoryAndCompanyStoredByName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ConvertToName<Category>(migrationBuilder, "Category");
            ConvertToName<Company>(migrationBuilder, "Company");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ConvertToNumber<Company>(migrationBuilder, "Company");
            ConvertToNumber<Category>(migrationBuilder, "Category");
        }

        private static void ConvertToName<TEnum>(MigrationBuilder migrationBuilder, string column) where TEnum : struct, Enum
        {
            var cases = string.Join("\n", Enum.GetValues<TEnum>()
                .Select(value => $"        WHEN {Convert.ToInt32(value)} THEN '{value}'"));

            migrationBuilder.Sql($"""
                ALTER TABLE "Products"
                ALTER COLUMN "{column}" TYPE character varying(50)
                USING (CASE "{column}"
                {cases}
                        ELSE "{column}"::text
                    END);
                """);
        }

        private static void ConvertToNumber<TEnum>(MigrationBuilder migrationBuilder, string column) where TEnum : struct, Enum
        {
            var cases = string.Join("\n", Enum.GetValues<TEnum>()
                .Select(value => $"        WHEN '{value}' THEN {Convert.ToInt32(value)}"));

            migrationBuilder.Sql($"""
                ALTER TABLE "Products"
                ALTER COLUMN "{column}" TYPE integer
                USING (CASE "{column}"
                {cases}
                        ELSE 0
                    END);
                """);
        }
    }
}
