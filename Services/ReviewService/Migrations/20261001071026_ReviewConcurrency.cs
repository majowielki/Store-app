using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Store.ReviewService.Migrations
{
    /// <inheritdoc />
    public partial class ReviewConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReviewSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DemoSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReviewSummaryClocks",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewSummaryClocks", x => x.ProductId);
                });

            // Preserve the current rolling quota when upgrading an existing database.
            migrationBuilder.Sql("""
                INSERT INTO "ReviewSubmissions" ("Id", "UserId", "DemoSessionId", "SubmittedAt")
                SELECT "Id", "UserId", "DemoSessionId", "SubmittedAt" FROM "Reviews" WHERE "UserId" IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewSubmissions_UserId_DemoSessionId_SubmittedAt",
                table: "ReviewSubmissions",
                columns: new[] { "UserId", "DemoSessionId", "SubmittedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewSubmissions");

            migrationBuilder.DropTable(
                name: "ReviewSummaryClocks");
        }
    }
}
