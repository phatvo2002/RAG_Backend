using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageDeletionAndSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deletedAt",
                schema: "chat",
                table: "Messages",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deletedByUserId",
                schema: "chat",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "score",
                schema: "chat",
                table: "Citations",
                type: "real",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_deletedByUserId",
                schema: "chat",
                table: "Messages",
                column: "deletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiTurns_organizationId_correlationId",
                schema: "chat",
                table: "AiTurns",
                columns: new[] { "organizationId", "correlationId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_AspNetUsers_deletedByUserId",
                schema: "chat",
                table: "Messages",
                column: "deletedByUserId",
                principalSchema: "identity",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_AspNetUsers_deletedByUserId",
                schema: "chat",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_deletedByUserId",
                schema: "chat",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_AiTurns_organizationId_correlationId",
                schema: "chat",
                table: "AiTurns");

            migrationBuilder.DropColumn(
                name: "deletedAt",
                schema: "chat",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "deletedByUserId",
                schema: "chat",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "score",
                schema: "chat",
                table: "Citations");

        }
    }
}
