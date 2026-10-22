using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeleteDocumentChunksOnDocumentDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Citations_DocumentChunks_chunkId",
                schema: "chat",
                table: "Citations");

            migrationBuilder.DropForeignKey(
                name: "FK_RetrievalHits_DocumentChunks_chunkId",
                schema: "rag",
                table: "RetrievalHits");

            migrationBuilder.AlterColumn<Guid>(
                name: "chunkId",
                schema: "rag",
                table: "RetrievalHits",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "chunkId",
                schema: "chat",
                table: "Citations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Citations_DocumentChunks_chunkId",
                schema: "chat",
                table: "Citations",
                column: "chunkId",
                principalSchema: "document",
                principalTable: "DocumentChunks",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RetrievalHits_DocumentChunks_chunkId",
                schema: "rag",
                table: "RetrievalHits",
                column: "chunkId",
                principalSchema: "document",
                principalTable: "DocumentChunks",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Citations_DocumentChunks_chunkId",
                schema: "chat",
                table: "Citations");

            migrationBuilder.DropForeignKey(
                name: "FK_RetrievalHits_DocumentChunks_chunkId",
                schema: "rag",
                table: "RetrievalHits");

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM chat."Citations" WHERE "chunkId" IS NULL)
                       OR EXISTS (SELECT 1 FROM rag."RetrievalHits" WHERE "chunkId" IS NULL) THEN
                        RAISE EXCEPTION 'Cannot restore required chunk references after chunks have been deleted';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "chunkId",
                schema: "rag",
                table: "RetrievalHits",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "chunkId",
                schema: "chat",
                table: "Citations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Citations_DocumentChunks_chunkId",
                schema: "chat",
                table: "Citations",
                column: "chunkId",
                principalSchema: "document",
                principalTable: "DocumentChunks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RetrievalHits_DocumentChunks_chunkId",
                schema: "rag",
                table: "RetrievalHits",
                column: "chunkId",
                principalSchema: "document",
                principalTable: "DocumentChunks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
