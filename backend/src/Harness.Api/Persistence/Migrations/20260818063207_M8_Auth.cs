using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Harness.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M8_Auth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "shared");

            migrationBuilder.CreateTable(
                name: "admin_roles",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "admin_users",
                schema: "shared",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "admin_user_roles",
                schema: "shared",
                columns: table => new
                {
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminRoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_user_roles", x => new { x.AdminUserId, x.AdminRoleId });
                    table.ForeignKey(
                        name: "FK_admin_user_roles_admin_roles_AdminRoleId",
                        column: x => x.AdminRoleId,
                        principalSchema: "shared",
                        principalTable: "admin_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_admin_user_roles_admin_users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalSchema: "shared",
                        principalTable: "admin_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_roles_Name",
                schema: "shared",
                table: "admin_roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_user_roles_AdminRoleId",
                schema: "shared",
                table: "admin_user_roles",
                column: "AdminRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_admin_users_Username",
                schema: "shared",
                table: "admin_users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_user_roles",
                schema: "shared");

            migrationBuilder.DropTable(
                name: "admin_roles",
                schema: "shared");

            migrationBuilder.DropTable(
                name: "admin_users",
                schema: "shared");
        }
    }
}
