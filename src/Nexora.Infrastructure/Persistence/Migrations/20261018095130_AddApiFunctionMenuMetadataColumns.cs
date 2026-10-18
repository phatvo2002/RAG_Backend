using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApiFunctionMenuMetadataColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "menuParentCode",
                schema: "security",
                table: "ApiFunctions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "displayOrder",
                schema: "security",
                table: "ApiFunctions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "isMenuVisible",
                schema: "security",
                table: "ApiFunctions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "isMobile",
                schema: "security",
                table: "ApiFunctions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "menuUrl",
                schema: "security",
                table: "ApiFunctions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_api_functions_menu_parent_order_status",
                schema: "security",
                table: "ApiFunctions",
                columns: new[] { "menuParentCode", "displayOrder", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_api_functions_menu_parent_order_status",
                schema: "security",
                table: "ApiFunctions");

            migrationBuilder.DropColumn(name: "menuParentCode", schema: "security", table: "ApiFunctions");
            migrationBuilder.DropColumn(name: "displayOrder", schema: "security", table: "ApiFunctions");
            migrationBuilder.DropColumn(name: "isMenuVisible", schema: "security", table: "ApiFunctions");
            migrationBuilder.DropColumn(name: "isMobile", schema: "security", table: "ApiFunctions");
            migrationBuilder.DropColumn(name: "menuUrl", schema: "security", table: "ApiFunctions");
        }
    }
}
