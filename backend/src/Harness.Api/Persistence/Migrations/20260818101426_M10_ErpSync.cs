using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Harness.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M10_ErpSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "erp_sales_orders",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ErpOrderNo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DeliveryMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_erp_sales_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "erp_sync_records",
                schema: "integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TargetSystem = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_erp_sync_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_erp_sales_orders_OrderId",
                schema: "integration",
                table: "erp_sales_orders",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_erp_sales_orders_OrderNumber",
                schema: "integration",
                table: "erp_sales_orders",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_erp_sync_records_EventId",
                schema: "integration",
                table: "erp_sync_records",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_erp_sync_records_Status_CreatedAt",
                schema: "integration",
                table: "erp_sync_records",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "erp_sales_orders",
                schema: "integration");

            migrationBuilder.DropTable(
                name: "erp_sync_records",
                schema: "integration");
        }
    }
}
