using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Store.ProductService.Migrations
{
    /// <inheritdoc />
    public partial class ProductSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Products",
                type: "character varying(220)",
                maxLength: 220,
                nullable: false,
                defaultValue: "");

            // Products created before slugs existed get one made from their title, the way
            // ProductSlug.From makes it (same accent folding); a title that repeats gets its id
            // appended. Frozen here: later changes to ProductSlug do not rewrite existing slugs.
            migrationBuilder.Sql("""
                UPDATE "Products" AS p
                SET "Slug" = CASE WHEN s.n = 1 THEN s.base ELSE s.base || '-' || p."Id" END
                FROM (
                    SELECT "Id", base, row_number() OVER (PARTITION BY base ORDER BY "Id") AS n
                    FROM (
                        SELECT "Id",
                               coalesce(nullif(trim(both '-' from regexp_replace(
                                   translate(lower("Title"),
                                       'àáâãäåąçćčďèéêëęěìíîïłńňñòóôõöøřśšťùúûüůýÿźżž',
                                       'aaaaaaacccdeeeeeeiiiilnnnoooooorsstuuuuuyyzzz'),
                                   '[^a-z0-9]+', '-', 'g')), ''), 'product') AS base
                        FROM "Products"
                    ) AS titles
                ) AS s
                WHERE p."Id" = s."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Slug",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Products");
        }
    }
}
