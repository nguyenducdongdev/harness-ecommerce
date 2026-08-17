using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Harness.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M4_PromotionShipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_flash_sale_items_flash_sales_FlashSaleId",
                schema: "promotion",
                table: "flash_sale_items",
                column: "FlashSaleId",
                principalSchema: "promotion",
                principalTable: "flash_sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_flash_sale_items_flash_sales_FlashSaleId",
                schema: "promotion",
                table: "flash_sale_items");
        }
    }
}
