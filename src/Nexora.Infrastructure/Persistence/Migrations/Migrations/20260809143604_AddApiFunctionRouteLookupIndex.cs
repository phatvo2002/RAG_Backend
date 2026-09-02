using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApiFunctionRouteLookupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_api_functions_http_method_route_status",
                schema: "security",
                table: "ApiFunctions",
                columns: new[] { "httpMethod", "routeTemplate", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_api_functions_http_method_route_status",
                schema: "security",
                table: "ApiFunctions");
        }
    }
}
