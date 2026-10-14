using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentFolderAccessGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentFolderAccessGrants",
                schema: "document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    folderId = table.Column<Guid>(type: "uuid", nullable: false),
                    departmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    userId = table.Column<Guid>(type: "uuid", nullable: true),
                    accessLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    grantedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    validFrom = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expiresAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revokedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentFolderAccessGrants", x => x.id);
                    table.ForeignKey(
                        name: "FK_DocumentFolderAccessGrants_Departments_departmentId",
                        column: x => x.departmentId,
                        principalSchema: "organization",
                        principalTable: "Departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentFolderAccessGrants_DocumentFolders_folderId",
                        column: x => x.folderId,
                        principalSchema: "document",
                        principalTable: "DocumentFolders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolderAccessGrants_departmentId",
                schema: "document",
                table: "DocumentFolderAccessGrants",
                column: "departmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolderAccessGrants_folderId",
                schema: "document",
                table: "DocumentFolderAccessGrants",
                column: "folderId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolderAccessGrants_organizationId_departmentId",
                schema: "document",
                table: "DocumentFolderAccessGrants",
                columns: new[] { "organizationId", "departmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolderAccessGrants_organizationId_folderId",
                schema: "document",
                table: "DocumentFolderAccessGrants",
                columns: new[] { "organizationId", "folderId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolderAccessGrants_organizationId_userId",
                schema: "document",
                table: "DocumentFolderAccessGrants",
                columns: new[] { "organizationId", "userId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentFolderAccessGrants",
                schema: "document");
        }
    }
}
