using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Store.ProductService.Migrations
{
    /// <inheritdoc />
    public partial class ProductSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:fuzzystrmatch", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

            // unaccent() is only STABLE (its dictionary could change), so a generated column cannot
            // call it; this wrapper names the dictionary and is declared IMMUTABLE, the usual way
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION store_unaccent(text) RETURNS text
                    LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT
                    AS $$ SELECT public.unaccent('public.unaccent'::regdictionary, $1) $$;
                """);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Products",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('english'::regconfig, store_unaccent(\"Title\")), 'A') || setweight(to_tsvector('english'::regconfig, store_unaccent(regexp_replace(\"Category\", '([a-z])([A-Z])', '\\1 \\2', 'g') || ' ' || \"Company\")), 'B') || setweight(to_tsvector('english'::regconfig, store_unaccent(\"Description\")), 'C')",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_SearchVector",
                table: "Products",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_SearchVector",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Products");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS store_unaccent(text);");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:fuzzystrmatch", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");
        }
    }
}
