using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "actualEndedAt",
                schema: "meeting",
                table: "Meetings",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "actualStartedAt",
                schema: "meeting",
                table: "Meetings",
                type: "timestamptz",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "actualEndedAt",
                schema: "meeting",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "actualStartedAt",
                schema: "meeting",
                table: "Meetings");
        }
    }
}
