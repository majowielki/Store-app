using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Store.CartService.Migrations
{
    /// <summary>
    /// The cart stops keeping a copy of the catalogue: the Products table and the foreign key to
    /// it go, and each line carries its own product snapshot. One cart per user and one line per
    /// product and colour become unique constraints; rows that violate them are merged first.
    /// </summary>
    public partial class CartOwnsItsLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Duplicate carts of one user: move the lines into the oldest cart, drop the others
            migrationBuilder.Sql("""
                UPDATE "CartItems" ci SET "CartId" = k."KeepId"
                FROM (SELECT "UserId", MIN("Id") AS "KeepId" FROM "Carts" GROUP BY "UserId") k
                JOIN "Carts" c ON c."UserId" = k."UserId" AND c."Id" <> k."KeepId"
                WHERE ci."CartId" = c."Id";

                DELETE FROM "Carts" c
                USING (SELECT "UserId", MIN("Id") AS "KeepId" FROM "Carts" GROUP BY "UserId") k
                WHERE c."UserId" = k."UserId" AND c."Id" <> k."KeepId";
                """);

            // Duplicate lines for one product and colour: add the quantities up on the oldest line
            migrationBuilder.Sql("""
                UPDATE "CartItems" ci SET "Amount" = s."Total"
                FROM (SELECT MIN("Id") AS "KeepId", SUM("Amount") AS "Total"
                      FROM "CartItems" GROUP BY "CartId", "ProductId", "ProductColor" HAVING COUNT(*) > 1) s
                WHERE ci."Id" = s."KeepId";

                DELETE FROM "CartItems" ci
                USING (SELECT MIN("Id") AS "KeepId", "CartId", "ProductId", "ProductColor"
                       FROM "CartItems" GROUP BY "CartId", "ProductId", "ProductColor") k
                WHERE ci."CartId" = k."CartId" AND ci."ProductId" = k."ProductId"
                  AND ci."ProductColor" = k."ProductColor" AND ci."Id" <> k."KeepId";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Products_ProductId",
                table: "CartItems");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Carts_UserId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "CartItems",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "ProductColor",
                table: "CartItems",
                newName: "Color");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "CartItems",
                newName: "Quantity");

            // Existing lines were last refreshed when they were last updated
            migrationBuilder.AddColumn<DateTime>(
                name: "SnapshotAt",
                table: "CartItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");
            migrationBuilder.Sql("""UPDATE "CartItems" SET "SnapshotAt" = "UpdatedAt";""");
            migrationBuilder.Sql("""ALTER TABLE "CartItems" ALTER COLUMN "SnapshotAt" DROP DEFAULT;""");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductId_Color",
                table: "CartItems",
                columns: new[] { "CartId", "ProductId", "Color" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Carts_UserId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_ProductId_Color",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "SnapshotAt",
                table: "CartItems");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "CartItems",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "CartItems",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "Color",
                table: "CartItems",
                newName: "ProductColor");

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Colors = table.Column<List<string>>(type: "text[]", nullable: false),
                    Company = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DepthCm = table.Column<decimal>(type: "numeric", nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    Groups = table.Column<List<string>>(type: "text[]", nullable: false),
                    HeightCm = table.Column<decimal>(type: "numeric", nullable: true),
                    Image = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Materials = table.Column<List<string>>(type: "text[]", nullable: false),
                    NewArrival = table.Column<bool>(type: "boolean", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric", nullable: true),
                    WidthCm = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            // The lines no longer reference rows of the recreated (empty) Products table; the
            // foreign key of the old schema cannot be restored without them
        }
    }
}
