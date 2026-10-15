using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingTranscript : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingTranscriptSegments",
                schema: "meeting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    meetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    speakerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    startedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    endedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    sourceLanguage = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    text = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingTranscriptSegments", x => x.id);
                    table.ForeignKey(
                        name: "FK_MeetingTranscriptSegments_AspNetUsers_speakerUserId",
                        column: x => x.speakerUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingTranscriptSegments_Meetings_meetingId",
                        column: x => x.meetingId,
                        principalSchema: "meeting",
                        principalTable: "Meetings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTranscriptSegments_meetingId_id",
                schema: "meeting",
                table: "MeetingTranscriptSegments",
                columns: new[] { "meetingId", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTranscriptSegments_organizationId_meetingId_startedAt",
                schema: "meeting",
                table: "MeetingTranscriptSegments",
                columns: new[] { "organizationId", "meetingId", "startedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTranscriptSegments_speakerUserId",
                schema: "meeting",
                table: "MeetingTranscriptSegments",
                column: "speakerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeetingTranscriptSegments",
                schema: "meeting");
        }
    }
}
