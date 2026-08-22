using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Harness.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M15_WarehouseGeoCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                schema: "inventory",
                table: "warehouses",
                type: "double precision",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                schema: "inventory",
                table: "warehouses",
                type: "double precision",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "inventory",
                table: "warehouses",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 10.7723, 106.7043 });

            migrationBuilder.UpdateData(
                schema: "inventory",
                table: "warehouses",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 21.028500000000001, 105.78919999999999 });

            migrationBuilder.UpdateData(
                schema: "inventory",
                table: "warehouses",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Latitude", "Longitude" },
                values: new object[] { 10.9556, 106.68899999999999 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                schema: "inventory",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "Longitude",
                schema: "inventory",
                table: "warehouses");
        }
    }
}