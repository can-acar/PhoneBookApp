using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInitialIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContactId",
                table: "ContactInfos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContactHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Data = table.Column<string>(type: "jsonb", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IPAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AdditionalMetadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EventData = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactInfos_ContactId",
                table: "ContactInfos",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactHistory_ContactId_Lookup",
                table: "ContactHistory",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactHistory_ContactId_Timestamp",
                table: "ContactHistory",
                columns: new[] { "ContactId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactHistory_CorrelationId",
                table: "ContactHistory",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactHistory_OperationType",
                table: "ContactHistory",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_ContactHistory_Timestamp",
                table: "ContactHistory",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_CorrelationId",
                table: "OutboxEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_EventType",
                table: "OutboxEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Status",
                table: "OutboxEvents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Status_CreatedAt",
                table: "OutboxEvents",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Status_NextRetryAt",
                table: "OutboxEvents",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Status_ProcessedAt",
                table: "OutboxEvents",
                columns: new[] { "Status", "ProcessedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ContactInfos_Contacts_ContactId",
                table: "ContactInfos",
                column: "ContactId",
                principalTable: "Contacts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactInfos_Contacts_ContactId",
                table: "ContactInfos");

            migrationBuilder.DropTable(
                name: "ContactHistory");

            migrationBuilder.DropTable(
                name: "OutboxEvents");

            migrationBuilder.DropIndex(
                name: "IX_ContactInfos_ContactId",
                table: "ContactInfos");

            migrationBuilder.DropColumn(
                name: "ContactId",
                table: "ContactInfos");
        }
    }
}
