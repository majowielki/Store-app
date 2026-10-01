using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Store.AuditLogService.Migrations
{
    /// <inheritdoc />
    public partial class OrderReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderReceipts",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderReceipts", x => x.OrderId);
                });
            // Existing orders remain deduplicated after the upgrade, even if audit rows
            // contain historical duplicates or are later removed by retention.
            migrationBuilder.Sql("""
                INSERT INTO "OrderReceipts" ("OrderId")
                SELECT DISTINCT "EntityId"::integer FROM "AuditLogs"
                WHERE "Action" = 'ORDER_PLACED' AND "EntityName" = 'Order'
                  AND CASE WHEN "EntityId" ~ '^[0-9]{1,10}$'
                    THEN "EntityId"::bigint BETWEEN 1 AND 2147483647 ELSE FALSE END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderReceipts");
        }
    }
}
