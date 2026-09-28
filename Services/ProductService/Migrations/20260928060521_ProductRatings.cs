using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Store.ProductService.Migrations
{
    /// <inheritdoc />
    public partial class ProductRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RatingAverage",
                table: "Products",
                type: "numeric(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "RatingChangedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RatingCount",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Products_RatingAverage_RatingCount",
                table: "Products",
                columns: new[] { "RatingAverage", "RatingCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_RatingAverage_RatingCount",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RatingAverage",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RatingChangedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RatingCount",
                table: "Products");
        }
    }
}
