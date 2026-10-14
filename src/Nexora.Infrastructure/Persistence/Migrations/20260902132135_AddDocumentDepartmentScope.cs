using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentDepartmentScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "departmentId",
                schema: "document",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "departmentId",
                schema: "document",
                table: "DocumentFolders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(name: "IX_Documents_organizationId_departmentId", schema: "document", table: "Documents", columns: new[] { "organizationId", "departmentId" });
            migrationBuilder.CreateIndex(name: "IX_DocumentFolders_organizationId_departmentId", schema: "document", table: "DocumentFolders", columns: new[] { "organizationId", "departmentId" });
            migrationBuilder.AddForeignKey(name: "FK_Documents_Departments_departmentId", schema: "document", table: "Documents", column: "departmentId", principalSchema: "organization", principalTable: "Departments", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_DocumentFolders_Departments_departmentId", schema: "document", table: "DocumentFolders", column: "departmentId", principalSchema: "organization", principalTable: "Departments", principalColumn: "id", onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Documents_Departments_departmentId", schema: "document", table: "Documents");
            migrationBuilder.DropForeignKey(name: "FK_DocumentFolders_Departments_departmentId", schema: "document", table: "DocumentFolders");
            migrationBuilder.DropIndex(name: "IX_Documents_organizationId_departmentId", schema: "document", table: "Documents");
            migrationBuilder.DropIndex(name: "IX_DocumentFolders_organizationId_departmentId", schema: "document", table: "DocumentFolders");
            migrationBuilder.DropColumn(name: "departmentId", schema: "document", table: "Documents");

            migrationBuilder.DropColumn(name: "departmentId", schema: "document", table: "DocumentFolders");
        }
    }
}
