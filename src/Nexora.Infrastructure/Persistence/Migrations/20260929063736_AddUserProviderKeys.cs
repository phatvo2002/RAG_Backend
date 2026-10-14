using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProviderKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProviderKeys",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ciphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                    sha256 = table.Column<byte[]>(type: "bytea", nullable: false),
                    endpointId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    maskedSuffix = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    lastUsedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProviderKeys", x => x.id);
                    table.ForeignKey(
                        name: "FK_UserProviderKeys_AspNetUsers_userId",
                        column: x => x.userId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProviderKeys_userId_provider",
                schema: "security",
                table: "UserProviderKeys",
                columns: new[] { "userId", "provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProviderKeys",
                schema: "security");
        }
    }
}
