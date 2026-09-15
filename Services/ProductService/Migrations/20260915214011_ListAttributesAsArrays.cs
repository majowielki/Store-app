using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Store.ProductService.Migrations
{
    /// <summary>
    /// Colors, Groups and Materials move from comma-separated varchar columns to native text[]
    /// so catalogue filters run in SQL. Existing rows are converted in place.
    /// </summary>
    public partial class ListAttributesAsArrays : Migration
    {
        private static readonly (string Column, int Length)[] ListColumns =
        {
            ("Colors", 500),
            ("Groups", 200),
            ("Materials", 500)
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A plain ALTER COLUMN cannot cast text to text[]; the USING clause splits the CSV value
            foreach (var (column, _) in ListColumns)
            {
                migrationBuilder.Sql($"""
                    ALTER TABLE "Products"
                    ALTER COLUMN "{column}" TYPE text[]
                    USING COALESCE(string_to_array(NULLIF(btrim("{column}"), ''), ','), ARRAY[]::text[]);
                    """);
            }

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Colors",
                table: "Products",
                column: "Colors")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Company",
                table: "Products",
                column: "Company");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Groups",
                table: "Products",
                column: "Groups")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Materials",
                table: "Products",
                column: "Materials")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Title",
                table: "Products",
                column: "Title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var name in new[] { "Category", "Colors", "Company", "Groups", "IsActive", "Materials", "Title" })
            {
                migrationBuilder.DropIndex(name: $"IX_Products_{name}", table: "Products");
            }

            foreach (var (column, length) in ListColumns)
            {
                migrationBuilder.Sql($"""
                    ALTER TABLE "Products"
                    ALTER COLUMN "{column}" TYPE character varying({length})
                    USING array_to_string("{column}", ',');
                    """);
            }
        }
    }
}
