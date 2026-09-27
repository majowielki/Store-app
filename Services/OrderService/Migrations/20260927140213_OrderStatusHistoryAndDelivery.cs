using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Store.OrderService.Migrations
{
    /// <inheritdoc />
    public partial class OrderStatusHistoryAndDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DeliveryFrom",
                table: "Orders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DeliveryTo",
                table: "Orders",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderStatusChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusChanges_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusChanges_OrderId",
                table: "OrderStatusChanges",
                column: "OrderId");

            // Orders placed before the history existed start it with the status they are in
            migrationBuilder.Sql("""
                INSERT INTO "OrderStatusChanges" ("OrderId", "Status", "ChangedAt", "ChangedBy")
                SELECT "Id", "Status", "CreatedAt", "UserId" FROM "Orders";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderStatusChanges");

            migrationBuilder.DropColumn(
                name: "DeliveryFrom",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryTo",
                table: "Orders");
        }
    }
}
