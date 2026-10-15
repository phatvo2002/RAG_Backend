using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SetEnglishAsDefaultLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "preferredLanguage",
                schema: "organization",
                table: "UserProfiles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "en",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldDefaultValue: "vi");

            migrationBuilder.AlterColumn<string>(
                name: "defaultLanguage",
                schema: "organization",
                table: "OrganizationSettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "en",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldDefaultValue: "vi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "preferredLanguage",
                schema: "organization",
                table: "UserProfiles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "vi",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldDefaultValue: "en");

            migrationBuilder.AlterColumn<string>(
                name: "defaultLanguage",
                schema: "organization",
                table: "OrganizationSettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "vi",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldDefaultValue: "en");
        }
    }
}
