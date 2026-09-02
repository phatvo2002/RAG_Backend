using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Nexora.Infrastructure.Persistence;

#nullable disable

namespace Nexora.Infrastructure.Migrations;

[DbContext(typeof(NexoraDbContext))]
[Migration("20260829143000_SimplifyActivityLogColumns")]
public partial class SimplifyActivityLogColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "organizationId",
            schema: "security",
            table: "ActivityLogs",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<string>(
            name: "correlationId",
            schema: "security",
            table: "ActivityLogs",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100);

        migrationBuilder.AlterColumn<string>(
            name: "logHash",
            schema: "security",
            table: "ActivityLogs",
            type: "char(64)",
            maxLength: 64,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "char(64)",
            oldMaxLength: 64);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "organizationId",
            schema: "security",
            table: "ActivityLogs",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid?),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "correlationId",
            schema: "security",
            table: "ActivityLogs",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "logHash",
            schema: "security",
            table: "ActivityLogs",
            type: "char(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "char(64)",
            oldMaxLength: 64,
            oldNullable: true);
    }
}
