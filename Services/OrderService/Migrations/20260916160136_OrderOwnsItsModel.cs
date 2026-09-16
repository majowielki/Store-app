using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Store.OrderService.Migrations
{
    /// <summary>
    /// The order gets its own model: subtotal, discount, delivery fee and total become columns
    /// computed from the old pseudo-lines ("First Order Discount", "Delivery Fee"), which are
    /// then deleted; OrderItem becomes OrderLines with a unit price; the status is stored by
    /// name; a Customers table records how many orders each user placed (the row the checkout
    /// locks to decide about the first-order discount).
    /// </summary>
    public partial class OrderOwnsItsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountReason",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFee",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Amounts from the old lines: products (ProductId <> 0) form the subtotal, the
            // discount and delivery pseudo-lines (ProductId = 0) carried their amounts in
            // OrderDiscount and DeliveryCost
            migrationBuilder.Sql("""
                UPDATE "Orders" o SET
                    "Subtotal" = COALESCE(s."Subtotal", 0),
                    "DiscountAmount" = COALESCE(s."Discount", 0),
                    "DiscountReason" = CASE WHEN COALESCE(s."Discount", 0) > 0 THEN 'first-order' END,
                    "DeliveryFee" = COALESCE(s."Delivery", 0),
                    "Total" = COALESCE(s."Subtotal", 0) - COALESCE(s."Discount", 0) + COALESCE(s."Delivery", 0)
                FROM (
                    SELECT "OrderId",
                           SUM(CASE WHEN "ProductId" <> 0 THEN "Price" * "Quantity" ELSE 0 END) AS "Subtotal",
                           SUM(COALESCE("OrderDiscount", 0)) AS "Discount",
                           SUM(COALESCE("DeliveryCost", 0)) AS "Delivery"
                    FROM "OrderItem"
                    GROUP BY "OrderId"
                ) s
                WHERE s."OrderId" = o."Id";

                DELETE FROM "OrderItem" WHERE "ProductId" = 0;
                """);

            // Every existing order was "Completed" (0); in the new vocabulary it is Placed
            migrationBuilder.Sql("""
                ALTER TABLE "Orders" ALTER COLUMN "Status" TYPE character varying(20) USING 'Placed';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Orders",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "Orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // OrderItem -> OrderLines: same rows, product lines only, price per unit
            migrationBuilder.DropColumn(name: "DeliveryCost", table: "OrderItem");
            migrationBuilder.DropColumn(name: "OrderDiscount", table: "OrderItem");
            migrationBuilder.RenameColumn(name: "Price", table: "OrderItem", newName: "UnitPrice");
            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "OrderItem",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
            migrationBuilder.RenameTable(name: "OrderItem", newName: "OrderLines");
            migrationBuilder.RenameIndex(name: "IX_OrderItem_OrderId", table: "OrderLines", newName: "IX_OrderLines_OrderId");
            migrationBuilder.Sql("""
                ALTER TABLE "OrderLines" RENAME CONSTRAINT "PK_OrderItem" TO "PK_OrderLines";
                ALTER TABLE "OrderLines" RENAME CONSTRAINT "FK_OrderItem_Orders_OrderId" TO "FK_OrderLines_Orders_OrderId";
                """);

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    OrdersPlaced = table.Column<int>(type: "integer", nullable: false),
                    FirstOrderAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastOrderAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.UserId);
                });

            migrationBuilder.Sql("""
                INSERT INTO "Customers" ("UserId", "OrdersPlaced", "FirstOrderAt", "LastOrderAt")
                SELECT "UserId", COUNT(*), MIN("CreatedAt"), MAX("CreatedAt")
                FROM "Orders"
                GROUP BY "UserId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId",
                table: "Orders");

            migrationBuilder.Sql("""
                ALTER TABLE "OrderLines" RENAME CONSTRAINT "PK_OrderLines" TO "PK_OrderItem";
                ALTER TABLE "OrderLines" RENAME CONSTRAINT "FK_OrderLines_Orders_OrderId" TO "FK_OrderItem_Orders_OrderId";
                """);
            migrationBuilder.RenameIndex(name: "IX_OrderLines_OrderId", table: "OrderLines", newName: "IX_OrderItem_OrderId");
            migrationBuilder.RenameTable(name: "OrderLines", newName: "OrderItem");
            migrationBuilder.RenameColumn(name: "UnitPrice", table: "OrderItem", newName: "Price");
            migrationBuilder.AddColumn<decimal>(name: "DeliveryCost", table: "OrderItem", type: "numeric", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "OrderDiscount", table: "OrderItem", type: "numeric", nullable: true);

            // Recreate the pseudo-lines the old model derived its totals from
            migrationBuilder.Sql("""
                INSERT INTO "OrderItem" ("OrderId", "ProductId", "ProductTitle", "ProductImage", "Price", "Quantity", "Color", "Company", "OrderDiscount")
                SELECT "Id", 0, 'First Order Discount', '', -"DiscountAmount", 1, 'N/A', '', "DiscountAmount"
                FROM "Orders" WHERE "DiscountAmount" > 0;

                INSERT INTO "OrderItem" ("OrderId", "ProductId", "ProductTitle", "ProductImage", "Price", "Quantity", "Color", "Company", "DeliveryCost")
                SELECT "Id", 0, 'Delivery Fee', '', "DeliveryFee", 1, 'N/A', '', "DeliveryFee"
                FROM "Orders" WHERE "DeliveryFee" > 0;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Orders" ALTER COLUMN "Status" TYPE integer USING 0;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "UserEmail",
                table: "Orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.DropColumn(name: "DeliveryFee", table: "Orders");
            migrationBuilder.DropColumn(name: "DiscountAmount", table: "Orders");
            migrationBuilder.DropColumn(name: "DiscountReason", table: "Orders");
            migrationBuilder.DropColumn(name: "Subtotal", table: "Orders");
            migrationBuilder.DropColumn(name: "Total", table: "Orders");
        }
    }
}
