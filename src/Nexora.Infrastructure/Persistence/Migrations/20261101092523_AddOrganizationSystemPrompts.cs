using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationSystemPrompts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "systemPromptVersionId",
                schema: "chat",
                table: "AiTurns",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrganizationSystemPrompts",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    promptType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    activeVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSystemPrompts", x => x.id);
                    table.CheckConstraint("ck_organization_system_prompt_type", "\"promptType\" IN ('CHAT', 'MEETING_TRANSLATION')");
                    table.ForeignKey(
                        name: "FK_OrganizationSystemPrompts_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationSystemPrompts_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSystemPromptVersions",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    systemPromptId = table.Column<Guid>(type: "uuid", nullable: false),
                    versionNo = table.Column<int>(type: "integer", nullable: false),
                    instructions = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    createdByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    publishedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    publishedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSystemPromptVersions", x => x.id);
                    table.CheckConstraint("ck_organization_system_prompt_version_number", "\"versionNo\" > 0");
                    table.CheckConstraint("ck_organization_system_prompt_version_status", "\"status\" IN ('DRAFT', 'PUBLISHED')");
                    table.ForeignKey(
                        name: "FK_OrganizationSystemPromptVersions_AspNetUsers_createdByUserId",
                        column: x => x.createdByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationSystemPromptVersions_AspNetUsers_publishedByUse~",
                        column: x => x.publishedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationSystemPromptVersions_OrganizationSystemPrompts_~",
                        column: x => x.systemPromptId,
                        principalSchema: "organization",
                        principalTable: "OrganizationSystemPrompts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiTurns_systemPromptVersionId",
                schema: "chat",
                table: "AiTurns",
                column: "systemPromptVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSystemPrompts_activeVersionId",
                schema: "organization",
                table: "OrganizationSystemPrompts",
                column: "activeVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSystemPrompts_organizationId_promptType",
                schema: "organization",
                table: "OrganizationSystemPrompts",
                columns: new[] { "organizationId", "promptType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSystemPrompts_updatedByUserId",
                schema: "organization",
                table: "OrganizationSystemPrompts",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSystemPromptVersions_createdByUserId",
                schema: "organization",
                table: "OrganizationSystemPromptVersions",
                column: "createdByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSystemPromptVersions_publishedByUserId",
                schema: "organization",
                table: "OrganizationSystemPromptVersions",
                column: "publishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSystemPromptVersions_systemPromptId",
                schema: "organization",
                table: "OrganizationSystemPromptVersions",
                column: "systemPromptId",
                unique: true,
                filter: "\"status\" = 'DRAFT'");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSystemPromptVersions_systemPromptId_versionNo",
                schema: "organization",
                table: "OrganizationSystemPromptVersions",
                columns: new[] { "systemPromptId", "versionNo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AiTurns_OrganizationSystemPromptVersions_systemPromptVersio~",
                schema: "chat",
                table: "AiTurns",
                column: "systemPromptVersionId",
                principalSchema: "organization",
                principalTable: "OrganizationSystemPromptVersions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationSystemPrompts_OrganizationSystemPromptVersions_~",
                schema: "organization",
                table: "OrganizationSystemPrompts",
                column: "activeVersionId",
                principalSchema: "organization",
                principalTable: "OrganizationSystemPromptVersions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiTurns_OrganizationSystemPromptVersions_systemPromptVersio~",
                schema: "chat",
                table: "AiTurns");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationSystemPrompts_OrganizationSystemPromptVersions_~",
                schema: "organization",
                table: "OrganizationSystemPrompts");

            migrationBuilder.DropTable(
                name: "OrganizationSystemPromptVersions",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "OrganizationSystemPrompts",
                schema: "organization");

            migrationBuilder.DropIndex(
                name: "IX_AiTurns_systemPromptVersionId",
                schema: "chat",
                table: "AiTurns");

            migrationBuilder.DropColumn(
                name: "systemPromptVersionId",
                schema: "chat",
                table: "AiTurns");
        }
    }
}
