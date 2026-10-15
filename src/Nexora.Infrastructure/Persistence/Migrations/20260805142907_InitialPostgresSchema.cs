using System;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Nexora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "security");

            migrationBuilder.EnsureSchema(
                name: "chat");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "calendar");

            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.EnsureSchema(
                name: "document");

            migrationBuilder.EnsureSchema(
                name: "meeting");

            migrationBuilder.EnsureSchema(
                name: "project");

            migrationBuilder.EnsureSchema(
                name: "rag");

            migrationBuilder.EnsureSchema(
                name: "task");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessTokens",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    tokenHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    tokenType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tokenName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    jti = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    issuedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expiresAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    lastUsedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revokedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    revokeReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    createdIp = table.Column<IPAddress>(type: "inet", nullable: true),
                    lastUsedIp = table.Column<IPAddress>(type: "inet", nullable: true),
                    userAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessTokens", x => x.id);
                    table.CheckConstraint("ck_access_token_hash_length", "octet_length(\"tokenHash\") = 32");
                    table.CheckConstraint("ck_access_token_revoke_actor", "(\"revokedAt\" IS NULL AND \"revokedByUserId\" IS NULL) OR (\"revokedAt\" IS NOT NULL AND \"revokedByUserId\" IS NOT NULL)");
                    table.CheckConstraint("ck_access_token_valid_range", "\"issuedAt\" < \"expiresAt\"");
                    table.ForeignKey(
                        name: "FK_AccessTokens_AspNetUsers_revokedByUserId",
                        column: x => x.revokedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessTokens_AspNetUsers_userId",
                        column: x => x.userId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApiFunctions",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    module = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    httpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    routeTemplate = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    actionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    riskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requiresConfirmation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiFunctions", x => x.id);
                    table.ForeignKey(
                        name: "FK_ApiFunctions_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "identity",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    dataResidency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleteBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.id);
                    table.ForeignKey(
                        name: "FK_Organizations_AspNetUsers_deleteBy",
                        column: x => x.deleteBy,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Organizations_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                schema: "organization",
                columns: table => new
                {
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    displayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    preferredLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "vi"),
                    timeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    avatarObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleteBy = table.Column<Guid>(type: "uuid", nullable: true),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.userId);
                    table.ForeignKey(
                        name: "FK_UserProfiles_AspNetUsers_deleteBy",
                        column: x => x.deleteBy,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserProfiles_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserProfiles_AspNetUsers_userId",
                        column: x => x.userId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                schema: "chat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ownerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    titleCiphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleteBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.id);
                    table.ForeignKey(
                        name: "FK_Conversations_AspNetUsers_deleteBy",
                        column: x => x.deleteBy,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversations_AspNetUsers_ownerUserId",
                        column: x => x.ownerUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversations_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversations_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    parentDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    managerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleteBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.id);
                    table.ForeignKey(
                        name: "FK_Departments_AspNetUsers_deleteBy",
                        column: x => x.deleteBy,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Departments_AspNetUsers_managerUserId",
                        column: x => x.managerUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Departments_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Departments_Departments_parentDepartmentId",
                        column: x => x.parentDepartmentId,
                        principalSchema: "organization",
                        principalTable: "Departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Departments_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentFolders",
                schema: "document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    parentFolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ownerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleteBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentFolders", x => x.id);
                    table.ForeignKey(
                        name: "FK_DocumentFolders_AspNetUsers_deleteBy",
                        column: x => x.deleteBy,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentFolders_AspNetUsers_ownerUserId",
                        column: x => x.ownerUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentFolders_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentFolders_DocumentFolders_parentFolderId",
                        column: x => x.parentFolderId,
                        principalSchema: "document",
                        principalTable: "DocumentFolders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentFolders_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMemberships",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joinedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    removedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    removedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    invitedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMemberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_AspNetUsers_invitedByUserId",
                        column: x => x.invitedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_AspNetUsers_removedByUserId",
                        column: x => x.removedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_AspNetUsers_userId",
                        column: x => x.userId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_Organizations_OrganizationId1",
                        column: x => x.OrganizationId1,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationRoleAssignments",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    roleId = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    validFrom = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expiresAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revokedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    assignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationRoleAssignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_OrganizationRoleAssignments_AspNetRoles_roleId",
                        column: x => x.roleId,
                        principalSchema: "identity",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationRoleAssignments_AspNetUsers_assignedByUserId",
                        column: x => x.assignedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationRoleAssignments_AspNetUsers_revokedByUserId",
                        column: x => x.revokedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationRoleAssignments_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationRoleAssignments_AspNetUsers_userId",
                        column: x => x.userId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationRoleAssignments_Organizations_OrganizationId1",
                        column: x => x.OrganizationId1,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_OrganizationRoleAssignments_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSettings",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    defaultLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "vi"),
                    timeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Asia/Ho_Chi_Minh"),
                    settingsJson = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSettings", x => x.id);
                    table.ForeignKey(
                        name: "FK_OrganizationSettings_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationSettings_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                schema: "project",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descriptionCiphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    ownerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleteBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.id);
                    table.ForeignKey(
                        name: "FK_Projects_AspNetUsers_deleteBy",
                        column: x => x.deleteBy,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_AspNetUsers_ownerUserId",
                        column: x => x.ownerUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RagIndexes",
                schema: "rag",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    indexName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "qdrant"),
                    collectionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    embeddingModel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    embeddingDimension = table.Column<int>(type: "integer", nullable: false),
                    distanceMetric = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    configurationHash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagIndexes", x => x.id);
                    table.ForeignKey(
                        name: "FK_RagIndexes_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleApiFunctions",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    roleId = table.Column<Guid>(type: "uuid", nullable: false),
                    functionId = table.Column<Guid>(type: "uuid", nullable: false),
                    effect = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    validFrom = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expiresAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revokedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    grantedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleApiFunctions", x => x.id);
                    table.CheckConstraint("ck_role_api_function_effect", "\"effect\" IN ('ALLOW', 'DENY')");
                    table.CheckConstraint("ck_role_api_function_revoke_actor", "(\"revokedAt\" IS NULL AND \"revokedByUserId\" IS NULL) OR (\"revokedAt\" IS NOT NULL AND \"revokedByUserId\" IS NOT NULL)");
                    table.CheckConstraint("ck_role_api_function_valid_range", "\"expiresAt\" IS NULL OR \"validFrom\" < \"expiresAt\"");
                    table.ForeignKey(
                        name: "FK_RoleApiFunctions_ApiFunctions_functionId",
                        column: x => x.functionId,
                        principalSchema: "security",
                        principalTable: "ApiFunctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleApiFunctions_AspNetRoles_roleId",
                        column: x => x.roleId,
                        principalSchema: "identity",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleApiFunctions_AspNetUsers_grantedByUserId",
                        column: x => x.grantedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleApiFunctions_AspNetUsers_revokedByUserId",
                        column: x => x.revokedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleApiFunctions_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "chat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    conversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    senderType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    senderUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    contentCiphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                    contentHash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    languageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    sequenceNo = table.Column<int>(type: "integer", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_Messages_AspNetUsers_senderUserId",
                        column: x => x.senderUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_conversationId",
                        column: x => x.conversationId,
                        principalSchema: "chat",
                        principalTable: "Conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Messages_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentMemberships",
                schema: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    departmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    membershipType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joinedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    leftAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    leftByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentMemberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_DepartmentMemberships_AspNetUsers_leftByUserId",
                        column: x => x.leftByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartmentMemberships_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartmentMemberships_AspNetUsers_userId",
                        column: x => x.userId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartmentMemberships_Departments_departmentId",
                        column: x => x.departmentId,
                        principalSchema: "organization",
                        principalTable: "Departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepartmentMemberships_Organizations_OrganizationId1",
                        column: x => x.OrganizationId1,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_DepartmentMemberships_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    projectId = table.Column<Guid>(type: "uuid", nullable: true),
                    actorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    apiFunctionId = table.Column<Guid>(type: "uuid", nullable: true),
                    module = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resourceType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    resourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ipAddress = table.Column<IPAddress>(type: "inet", nullable: true),
                    userAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correlationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    metadataJson = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    previousLogHash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: true),
                    logHash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    occurredAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_ApiFunctions_apiFunctionId",
                        column: x => x.apiFunctionId,
                        principalSchema: "security",
                        principalTable: "ApiFunctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_AspNetUsers_actorUserId",
                        column: x => x.actorUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Projects_projectId",
                        column: x => x.projectId,
                        principalSchema: "project",
                        principalTable: "Projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Meetings",
                schema: "meeting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    organizerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    projectId = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    scheduledStart = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    scheduledEnd = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    roomProvider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    roomExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recordingConsentStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleteBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meetings", x => x.id);
                    table.CheckConstraint("ck_meeting_schedule_range", "\"scheduledStart\" < \"scheduledEnd\"");
                    table.ForeignKey(
                        name: "FK_Meetings_AspNetUsers_deleteBy",
                        column: x => x.deleteBy,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meetings_AspNetUsers_organizerUserId",
                        column: x => x.organizerUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meetings_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meetings_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meetings_Projects_projectId",
                        column: x => x.projectId,
                        principalSchema: "project",
                        principalTable: "Projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                schema: "project",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    projectId = table.Column<Guid>(type: "uuid", nullable: false),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    membershipType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joinedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    leftAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    leftByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => x.id);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_AspNetUsers_leftByUserId",
                        column: x => x.leftByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_AspNetUsers_userId",
                        column: x => x.userId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_projectId",
                        column: x => x.projectId,
                        principalSchema: "project",
                        principalTable: "Projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                schema: "task",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    projectId = table.Column<Guid>(type: "uuid", nullable: true),
                    departmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    parentTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    descriptionCiphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    assigneeUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    createdByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    dueAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    completedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleteBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_Tasks_AspNetUsers_assigneeUserId",
                        column: x => x.assigneeUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_AspNetUsers_createdByUserId",
                        column: x => x.createdByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_AspNetUsers_deleteBy",
                        column: x => x.deleteBy,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_Departments_departmentId",
                        column: x => x.departmentId,
                        principalSchema: "organization",
                        principalTable: "Departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_Projects_projectId",
                        column: x => x.projectId,
                        principalSchema: "project",
                        principalTable: "Projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_Tasks_parentTaskId",
                        column: x => x.parentTaskId,
                        principalSchema: "task",
                        principalTable: "Tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiTurns",
                schema: "chat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    conversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    requestMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    responseMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    modelProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    modelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    promptVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    inputTokens = table.Column<int>(type: "integer", nullable: true),
                    outputTokens = table.Column<int>(type: "integer", nullable: true),
                    latencyMs = table.Column<int>(type: "integer", nullable: true),
                    noSourceReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correlationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    startedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    errorCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiTurns", x => x.id);
                    table.ForeignKey(
                        name: "FK_AiTurns_Conversations_conversationId",
                        column: x => x.conversationId,
                        principalSchema: "chat",
                        principalTable: "Conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiTurns_Messages_requestMessageId",
                        column: x => x.requestMessageId,
                        principalSchema: "chat",
                        principalTable: "Messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiTurns_Messages_responseMessageId",
                        column: x => x.responseMessageId,
                        principalSchema: "chat",
                        principalTable: "Messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiTurns_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                schema: "calendar",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    projectId = table.Column<Guid>(type: "uuid", nullable: true),
                    meetingId = table.Column<Guid>(type: "uuid", nullable: true),
                    organizerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    eventType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    startsAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    endsAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    allDay = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    descriptionCiphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleteBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.id);
                    table.CheckConstraint("ck_calendar_event_time_range", "\"startsAt\" < \"endsAt\"");
                    table.ForeignKey(
                        name: "FK_CalendarEvents_AspNetUsers_deleteBy",
                        column: x => x.deleteBy,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_AspNetUsers_organizerUserId",
                        column: x => x.organizerUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Meetings_meetingId",
                        column: x => x.meetingId,
                        principalSchema: "meeting",
                        principalTable: "Meetings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Projects_projectId",
                        column: x => x.projectId,
                        principalSchema: "project",
                        principalTable: "Projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingParticipants",
                schema: "meeting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    meetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    userId = table.Column<Guid>(type: "uuid", nullable: true),
                    guestEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    sourceLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    targetLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    joinedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    leftAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingParticipants", x => x.id);
                    table.CheckConstraint("ck_meeting_participant_identity", "\"userId\" IS NOT NULL OR \"guestEmail\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_MeetingParticipants_AspNetUsers_userId",
                        column: x => x.userId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingParticipants_Meetings_meetingId",
                        column: x => x.meetingId,
                        principalSchema: "meeting",
                        principalTable: "Meetings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingParticipants_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetrievalRuns",
                schema: "rag",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    aiTurnId = table.Column<Guid>(type: "uuid", nullable: false),
                    ragIndexId = table.Column<Guid>(type: "uuid", nullable: false),
                    strategy = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    topK = table.Column<int>(type: "integer", nullable: false),
                    filterHash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    configurationHash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    latencyMs = table.Column<int>(type: "integer", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetrievalRuns", x => x.id);
                    table.ForeignKey(
                        name: "FK_RetrievalRuns_AiTurns_aiTurnId",
                        column: x => x.aiTurnId,
                        principalSchema: "chat",
                        principalTable: "AiTurns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetrievalRuns_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetrievalRuns_RagIndexes_ragIndexId",
                        column: x => x.ragIndexId,
                        principalSchema: "rag",
                        principalTable: "RagIndexes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Citations",
                schema: "chat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    assistantMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    chunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    citationNo = table.Column<int>(type: "integer", nullable: false),
                    pageNo = table.Column<int>(type: "integer", nullable: true),
                    charStart = table.Column<int>(type: "integer", nullable: true),
                    charEnd = table.Column<int>(type: "integer", nullable: true),
                    quoteHash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citations", x => x.id);
                    table.ForeignKey(
                        name: "FK_Citations_Messages_assistantMessageId",
                        column: x => x.assistantMessageId,
                        principalSchema: "chat",
                        principalTable: "Messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citations_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConversationContexts",
                schema: "chat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    conversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    scopeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    departmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    projectId = table.Column<Guid>(type: "uuid", nullable: true),
                    documentId = table.Column<Guid>(type: "uuid", nullable: true),
                    selectedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationContexts", x => x.id);
                    table.ForeignKey(
                        name: "FK_ConversationContexts_AspNetUsers_selectedByUserId",
                        column: x => x.selectedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationContexts_Conversations_conversationId",
                        column: x => x.conversationId,
                        principalSchema: "chat",
                        principalTable: "Conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationContexts_Departments_departmentId",
                        column: x => x.departmentId,
                        principalSchema: "organization",
                        principalTable: "Departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationContexts_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversationContexts_Projects_projectId",
                        column: x => x.projectId,
                        principalSchema: "project",
                        principalTable: "Projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAccessGrants",
                schema: "document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    documentId = table.Column<Guid>(type: "uuid", nullable: false),
                    scopeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    principalDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    principalProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    principalUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    accessLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "VIEW"),
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
                    table.PrimaryKey("PK_DocumentAccessGrants", x => x.id);
                    table.CheckConstraint("ck_document_grant_revoke_actor", "(\"revokedAt\" IS NULL AND \"revokedByUserId\" IS NULL) OR (\"revokedAt\" IS NOT NULL AND \"revokedByUserId\" IS NOT NULL)");
                    table.CheckConstraint("ck_document_grant_scope_principal", "(\"scopeType\" = 'ORGANIZATION' AND \"principalDepartmentId\" IS NULL AND \"principalProjectId\" IS NULL AND \"principalUserId\" IS NULL) OR (\"scopeType\" = 'DEPARTMENT' AND \"principalDepartmentId\" IS NOT NULL AND \"principalProjectId\" IS NULL AND \"principalUserId\" IS NULL) OR (\"scopeType\" = 'PROJECT' AND \"principalDepartmentId\" IS NULL AND \"principalProjectId\" IS NOT NULL AND \"principalUserId\" IS NULL) OR (\"scopeType\" = 'USER' AND \"principalDepartmentId\" IS NULL AND \"principalProjectId\" IS NULL AND \"principalUserId\" IS NOT NULL)");
                    table.CheckConstraint("ck_document_grant_valid_range", "\"expiresAt\" IS NULL OR \"validFrom\" < \"expiresAt\"");
                    table.ForeignKey(
                        name: "FK_DocumentAccessGrants_AspNetUsers_grantedByUserId",
                        column: x => x.grantedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAccessGrants_AspNetUsers_principalUserId",
                        column: x => x.principalUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAccessGrants_AspNetUsers_revokedByUserId",
                        column: x => x.revokedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAccessGrants_Departments_principalDepartmentId",
                        column: x => x.principalDepartmentId,
                        principalSchema: "organization",
                        principalTable: "Departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAccessGrants_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentAccessGrants_Projects_principalProjectId",
                        column: x => x.principalProjectId,
                        principalSchema: "project",
                        principalTable: "Projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentChunkContents",
                schema: "document",
                columns: table => new
                {
                    chunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    storageProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "minio"),
                    storageBucket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    contentObjectKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    storageVersionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    encryptionKeyVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    contentSha256 = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunkContents", x => x.chunkId);
                });

            migrationBuilder.CreateTable(
                name: "DocumentChunks",
                schema: "document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    documentVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    contentHash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    languageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    tokenCount = table.Column<int>(type: "integer", nullable: true),
                    pageNo = table.Column<int>(type: "integer", nullable: true),
                    charStart = table.Column<int>(type: "integer", nullable: true),
                    charEnd = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ACTIVE"),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunks", x => x.id);
                    table.ForeignKey(
                        name: "FK_DocumentChunks_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RagIndexRecords",
                schema: "rag",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ragIndexId = table.Column<Guid>(type: "uuid", nullable: false),
                    chunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    vectorPointId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contentHash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    indexedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagIndexRecords", x => x.id);
                    table.ForeignKey(
                        name: "FK_RagIndexRecords_DocumentChunks_chunkId",
                        column: x => x.chunkId,
                        principalSchema: "document",
                        principalTable: "DocumentChunks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RagIndexRecords_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RagIndexRecords_RagIndexes_ragIndexId",
                        column: x => x.ragIndexId,
                        principalSchema: "rag",
                        principalTable: "RagIndexes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetrievalHits",
                schema: "rag",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    retrievalRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    chunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    rankNo = table.Column<int>(type: "integer", nullable: false),
                    similarityScore = table.Column<decimal>(type: "numeric(12,8)", nullable: true),
                    rerankScore = table.Column<decimal>(type: "numeric(12,8)", nullable: true),
                    selectedForContext = table.Column<bool>(type: "boolean", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetrievalHits", x => x.id);
                    table.ForeignKey(
                        name: "FK_RetrievalHits_DocumentChunks_chunkId",
                        column: x => x.chunkId,
                        principalSchema: "document",
                        principalTable: "DocumentChunks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetrievalHits_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetrievalHits_RetrievalRuns_retrievalRunId",
                        column: x => x.retrievalRunId,
                        principalSchema: "rag",
                        principalTable: "RetrievalRuns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                schema: "document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ownerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    folderId = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    currentVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleteBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_deleteBy",
                        column: x => x.deleteBy,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_ownerUserId",
                        column: x => x.ownerUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_DocumentFolders_folderId",
                        column: x => x.folderId,
                        principalSchema: "document",
                        principalTable: "DocumentFolders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVersions",
                schema: "document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    documentId = table.Column<Guid>(type: "uuid", nullable: false),
                    versionNo = table.Column<int>(type: "integer", nullable: false),
                    originalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mimeType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    sizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    storageProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "minio"),
                    storageBucket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    storageObjectKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    storageVersionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fileSha256 = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    extractedContentHash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: true),
                    parserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    parserVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    processingStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    failureCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    createdByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    readyAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersions", x => x.id);
                    table.CheckConstraint("ck_document_version_file_sha256_length", "octet_length(\"fileSha256\") = 32");
                    table.ForeignKey(
                        name: "FK_DocumentVersions_AspNetUsers_createdByUserId",
                        column: x => x.createdByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_Documents_documentId",
                        column: x => x.documentId,
                        principalSchema: "document",
                        principalTable: "Documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentVersions_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDocuments",
                schema: "project",
                columns: table => new
                {
                    organizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    projectId = table.Column<Guid>(type: "uuid", nullable: false),
                    documentId = table.Column<Guid>(type: "uuid", nullable: false),
                    linkedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    linkedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    rowVersion = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    deletedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleteBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDocuments", x => new { x.organizationId, x.projectId, x.documentId });
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_AspNetUsers_deleteBy",
                        column: x => x.deleteBy,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_AspNetUsers_linkedByUserId",
                        column: x => x.linkedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_AspNetUsers_updatedByUserId",
                        column: x => x.updatedByUserId,
                        principalSchema: "identity",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_Documents_documentId",
                        column: x => x.documentId,
                        principalSchema: "document",
                        principalTable: "Documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_Organizations_organizationId",
                        column: x => x.organizationId,
                        principalSchema: "organization",
                        principalTable: "Organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectDocuments_Projects_projectId",
                        column: x => x.projectId,
                        principalSchema: "project",
                        principalTable: "Projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessTokens_expiresAt",
                schema: "security",
                table: "AccessTokens",
                column: "expiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AccessTokens_jti",
                schema: "security",
                table: "AccessTokens",
                column: "jti",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessTokens_organizationId_tokenType_expiresAt",
                schema: "security",
                table: "AccessTokens",
                columns: new[] { "organizationId", "tokenType", "expiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessTokens_organizationId_userId_revokedAt_expiresAt",
                schema: "security",
                table: "AccessTokens",
                columns: new[] { "organizationId", "userId", "revokedAt", "expiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessTokens_revokedByUserId",
                schema: "security",
                table: "AccessTokens",
                column: "revokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessTokens_tokenHash",
                schema: "security",
                table: "AccessTokens",
                column: "tokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessTokens_userId",
                schema: "security",
                table: "AccessTokens",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_actorUserId",
                schema: "security",
                table: "ActivityLogs",
                column: "actorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_apiFunctionId",
                schema: "security",
                table: "ActivityLogs",
                column: "apiFunctionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_correlationId",
                schema: "security",
                table: "ActivityLogs",
                column: "correlationId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_logHash",
                schema: "security",
                table: "ActivityLogs",
                column: "logHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_organizationId_actorUserId_occurredAt",
                schema: "security",
                table: "ActivityLogs",
                columns: new[] { "organizationId", "actorUserId", "occurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_organizationId_module_action_occurredAt",
                schema: "security",
                table: "ActivityLogs",
                columns: new[] { "organizationId", "module", "action", "occurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_organizationId_projectId_occurredAt",
                schema: "security",
                table: "ActivityLogs",
                columns: new[] { "organizationId", "projectId", "occurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_projectId",
                schema: "security",
                table: "ActivityLogs",
                column: "projectId");

            migrationBuilder.CreateIndex(
                name: "IX_AiTurns_conversationId",
                schema: "chat",
                table: "AiTurns",
                column: "conversationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiTurns_organizationId_conversationId_startedAt",
                schema: "chat",
                table: "AiTurns",
                columns: new[] { "organizationId", "conversationId", "startedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiTurns_organizationId_status",
                schema: "chat",
                table: "AiTurns",
                columns: new[] { "organizationId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_AiTurns_requestMessageId",
                schema: "chat",
                table: "AiTurns",
                column: "requestMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_AiTurns_responseMessageId",
                schema: "chat",
                table: "AiTurns",
                column: "responseMessageId");

            migrationBuilder.CreateIndex(
                name: "ux_ai_turn_tenant_id",
                schema: "chat",
                table: "AiTurns",
                columns: new[] { "organizationId", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiFunctions_code",
                schema: "security",
                table: "ApiFunctions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiFunctions_module_status",
                schema: "security",
                table: "ApiFunctions",
                columns: new[] { "module", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiFunctions_updatedByUserId",
                schema: "security",
                table: "ApiFunctions",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                schema: "identity",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                schema: "identity",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                schema: "identity",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                schema: "identity",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "identity",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_deleteBy",
                schema: "calendar",
                table: "CalendarEvents",
                column: "deleteBy");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_meetingId",
                schema: "calendar",
                table: "CalendarEvents",
                column: "meetingId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_organizationId_organizerUserId_startsAt",
                schema: "calendar",
                table: "CalendarEvents",
                columns: new[] { "organizationId", "organizerUserId", "startsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_organizationId_projectId",
                schema: "calendar",
                table: "CalendarEvents",
                columns: new[] { "organizationId", "projectId" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_organizationId_startsAt_endsAt",
                schema: "calendar",
                table: "CalendarEvents",
                columns: new[] { "organizationId", "startsAt", "endsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_organizerUserId",
                schema: "calendar",
                table: "CalendarEvents",
                column: "organizerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_projectId",
                schema: "calendar",
                table: "CalendarEvents",
                column: "projectId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_updatedByUserId",
                schema: "calendar",
                table: "CalendarEvents",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Citations_assistantMessageId",
                schema: "chat",
                table: "Citations",
                column: "assistantMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Citations_chunkId",
                schema: "chat",
                table: "Citations",
                column: "chunkId");

            migrationBuilder.CreateIndex(
                name: "IX_Citations_organizationId_assistantMessageId_citationNo",
                schema: "chat",
                table: "Citations",
                columns: new[] { "organizationId", "assistantMessageId", "citationNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Citations_organizationId_chunkId",
                schema: "chat",
                table: "Citations",
                columns: new[] { "organizationId", "chunkId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationContexts_conversationId",
                schema: "chat",
                table: "ConversationContexts",
                column: "conversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationContexts_departmentId",
                schema: "chat",
                table: "ConversationContexts",
                column: "departmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationContexts_documentId",
                schema: "chat",
                table: "ConversationContexts",
                column: "documentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationContexts_organizationId",
                schema: "chat",
                table: "ConversationContexts",
                column: "organizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationContexts_projectId",
                schema: "chat",
                table: "ConversationContexts",
                column: "projectId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationContexts_selectedByUserId",
                schema: "chat",
                table: "ConversationContexts",
                column: "selectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_deleteBy",
                schema: "chat",
                table: "Conversations",
                column: "deleteBy");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_organizationId_ownerUserId_updatedAt",
                schema: "chat",
                table: "Conversations",
                columns: new[] { "organizationId", "ownerUserId", "updatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_organizationId_status",
                schema: "chat",
                table: "Conversations",
                columns: new[] { "organizationId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_ownerUserId",
                schema: "chat",
                table: "Conversations",
                column: "ownerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_updatedByUserId",
                schema: "chat",
                table: "Conversations",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_conversation_tenant_id",
                schema: "chat",
                table: "Conversations",
                columns: new[] { "organizationId", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentMemberships_departmentId",
                schema: "organization",
                table: "DepartmentMemberships",
                column: "departmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentMemberships_leftByUserId",
                schema: "organization",
                table: "DepartmentMemberships",
                column: "leftByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentMemberships_organizationId_departmentId_userId",
                schema: "organization",
                table: "DepartmentMemberships",
                columns: new[] { "organizationId", "departmentId", "userId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentMemberships_organizationId_userId",
                schema: "organization",
                table: "DepartmentMemberships",
                columns: new[] { "organizationId", "userId" });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentMemberships_OrganizationId1",
                schema: "organization",
                table: "DepartmentMemberships",
                column: "OrganizationId1");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentMemberships_updatedByUserId",
                schema: "organization",
                table: "DepartmentMemberships",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentMemberships_userId",
                schema: "organization",
                table: "DepartmentMemberships",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_deleteBy",
                schema: "organization",
                table: "Departments",
                column: "deleteBy");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_managerUserId",
                schema: "organization",
                table: "Departments",
                column: "managerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_organizationId_code",
                schema: "organization",
                table: "Departments",
                columns: new[] { "organizationId", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_organizationId_parentDepartmentId_name",
                schema: "organization",
                table: "Departments",
                columns: new[] { "organizationId", "parentDepartmentId", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_organizationId_status",
                schema: "organization",
                table: "Departments",
                columns: new[] { "organizationId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_parentDepartmentId",
                schema: "organization",
                table: "Departments",
                column: "parentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_updatedByUserId",
                schema: "organization",
                table: "Departments",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessGrants_documentId",
                schema: "document",
                table: "DocumentAccessGrants",
                column: "documentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessGrants_grantedByUserId",
                schema: "document",
                table: "DocumentAccessGrants",
                column: "grantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessGrants_organizationId_documentId_revokedAt_ex~",
                schema: "document",
                table: "DocumentAccessGrants",
                columns: new[] { "organizationId", "documentId", "revokedAt", "expiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessGrants_organizationId_principalDepartmentId",
                schema: "document",
                table: "DocumentAccessGrants",
                columns: new[] { "organizationId", "principalDepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessGrants_organizationId_principalProjectId",
                schema: "document",
                table: "DocumentAccessGrants",
                columns: new[] { "organizationId", "principalProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessGrants_organizationId_principalUserId",
                schema: "document",
                table: "DocumentAccessGrants",
                columns: new[] { "organizationId", "principalUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessGrants_principalDepartmentId",
                schema: "document",
                table: "DocumentAccessGrants",
                column: "principalDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessGrants_principalProjectId",
                schema: "document",
                table: "DocumentAccessGrants",
                column: "principalProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessGrants_principalUserId",
                schema: "document",
                table: "DocumentAccessGrants",
                column: "principalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessGrants_revokedByUserId",
                schema: "document",
                table: "DocumentAccessGrants",
                column: "revokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_documentVersionId",
                schema: "document",
                table: "DocumentChunks",
                column: "documentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_organizationId_documentVersionId_ordinal",
                schema: "document",
                table: "DocumentChunks",
                columns: new[] { "organizationId", "documentVersionId", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_chunk_tenant_id",
                schema: "document",
                table: "DocumentChunks",
                columns: new[] { "organizationId", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_deleteBy",
                schema: "document",
                table: "DocumentFolders",
                column: "deleteBy");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_ownerUserId",
                schema: "document",
                table: "DocumentFolders",
                column: "ownerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_parentFolderId",
                schema: "document",
                table: "DocumentFolders",
                column: "parentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFolders_updatedByUserId",
                schema: "document",
                table: "DocumentFolders",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_folder_parent_name",
                schema: "document",
                table: "DocumentFolders",
                columns: new[] { "organizationId", "parentFolderId", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_folder_tenant_id",
                schema: "document",
                table: "DocumentFolders",
                columns: new[] { "organizationId", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_currentVersionId",
                schema: "document",
                table: "Documents",
                column: "currentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_deleteBy",
                schema: "document",
                table: "Documents",
                column: "deleteBy");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_folderId",
                schema: "document",
                table: "Documents",
                column: "folderId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_organizationId_folderId",
                schema: "document",
                table: "Documents",
                columns: new[] { "organizationId", "folderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_organizationId_ownerUserId",
                schema: "document",
                table: "Documents",
                columns: new[] { "organizationId", "ownerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_organizationId_status",
                schema: "document",
                table: "Documents",
                columns: new[] { "organizationId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ownerUserId",
                schema: "document",
                table: "Documents",
                column: "ownerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_updatedByUserId",
                schema: "document",
                table: "Documents",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_document_tenant_id",
                schema: "document",
                table: "Documents",
                columns: new[] { "organizationId", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_createdByUserId",
                schema: "document",
                table: "DocumentVersions",
                column: "createdByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_documentId",
                schema: "document",
                table: "DocumentVersions",
                column: "documentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_organizationId_documentId_versionNo",
                schema: "document",
                table: "DocumentVersions",
                columns: new[] { "organizationId", "documentId", "versionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_organizationId_processingStatus",
                schema: "document",
                table: "DocumentVersions",
                columns: new[] { "organizationId", "processingStatus" });

            migrationBuilder.CreateIndex(
                name: "ux_document_version_tenant_id",
                schema: "document",
                table: "DocumentVersions",
                columns: new[] { "organizationId", "documentId", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipants_meetingId",
                schema: "meeting",
                table: "MeetingParticipants",
                column: "meetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipants_organizationId_meetingId",
                schema: "meeting",
                table: "MeetingParticipants",
                columns: new[] { "organizationId", "meetingId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipants_organizationId_meetingId_userId",
                schema: "meeting",
                table: "MeetingParticipants",
                columns: new[] { "organizationId", "meetingId", "userId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipants_userId",
                schema: "meeting",
                table: "MeetingParticipants",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_deleteBy",
                schema: "meeting",
                table: "Meetings",
                column: "deleteBy");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_organizationId_projectId",
                schema: "meeting",
                table: "Meetings",
                columns: new[] { "organizationId", "projectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_organizationId_scheduledStart",
                schema: "meeting",
                table: "Meetings",
                columns: new[] { "organizationId", "scheduledStart" });

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_organizerUserId",
                schema: "meeting",
                table: "Meetings",
                column: "organizerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_projectId",
                schema: "meeting",
                table: "Meetings",
                column: "projectId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_updatedByUserId",
                schema: "meeting",
                table: "Meetings",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_meeting_tenant_id",
                schema: "meeting",
                table: "Meetings",
                columns: new[] { "organizationId", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_conversationId",
                schema: "chat",
                table: "Messages",
                column: "conversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_organizationId_conversationId_sequenceNo",
                schema: "chat",
                table: "Messages",
                columns: new[] { "organizationId", "conversationId", "sequenceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_senderUserId",
                schema: "chat",
                table: "Messages",
                column: "senderUserId");

            migrationBuilder.CreateIndex(
                name: "ux_message_tenant_id",
                schema: "chat",
                table: "Messages",
                columns: new[] { "organizationId", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_org_membership_user",
                schema: "organization",
                table: "OrganizationMemberships",
                columns: new[] { "organizationId", "userId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_invitedByUserId",
                schema: "organization",
                table: "OrganizationMemberships",
                column: "invitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_organizationId_status",
                schema: "organization",
                table: "OrganizationMemberships",
                columns: new[] { "organizationId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_OrganizationId1",
                schema: "organization",
                table: "OrganizationMemberships",
                column: "OrganizationId1");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_removedByUserId",
                schema: "organization",
                table: "OrganizationMemberships",
                column: "removedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_updatedByUserId",
                schema: "organization",
                table: "OrganizationMemberships",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_userId",
                schema: "organization",
                table: "OrganizationMemberships",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "ux_org_membership_tenant_id",
                schema: "organization",
                table: "OrganizationMemberships",
                columns: new[] { "organizationId", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_assignedByUserId",
                schema: "organization",
                table: "OrganizationRoleAssignments",
                column: "assignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_organizationId_roleId_status",
                schema: "organization",
                table: "OrganizationRoleAssignments",
                columns: new[] { "organizationId", "roleId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_organizationId_userId_roleId",
                schema: "organization",
                table: "OrganizationRoleAssignments",
                columns: new[] { "organizationId", "userId", "roleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_organizationId_userId_status",
                schema: "organization",
                table: "OrganizationRoleAssignments",
                columns: new[] { "organizationId", "userId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_OrganizationId1",
                schema: "organization",
                table: "OrganizationRoleAssignments",
                column: "OrganizationId1");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_revokedByUserId",
                schema: "organization",
                table: "OrganizationRoleAssignments",
                column: "revokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_roleId",
                schema: "organization",
                table: "OrganizationRoleAssignments",
                column: "roleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_updatedByUserId",
                schema: "organization",
                table: "OrganizationRoleAssignments",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationRoleAssignments_userId",
                schema: "organization",
                table: "OrganizationRoleAssignments",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_deleteBy",
                schema: "organization",
                table: "Organizations",
                column: "deleteBy");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_slug",
                schema: "organization",
                table: "Organizations",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_status",
                schema: "organization",
                table: "Organizations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_updatedByUserId",
                schema: "organization",
                table: "Organizations",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSettings_organizationId",
                schema: "organization",
                table: "OrganizationSettings",
                column: "organizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSettings_updatedByUserId",
                schema: "organization",
                table: "OrganizationSettings",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_deleteBy",
                schema: "project",
                table: "ProjectDocuments",
                column: "deleteBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_documentId",
                schema: "project",
                table: "ProjectDocuments",
                column: "documentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_linkedByUserId",
                schema: "project",
                table: "ProjectDocuments",
                column: "linkedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_organizationId_documentId",
                schema: "project",
                table: "ProjectDocuments",
                columns: new[] { "organizationId", "documentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_projectId",
                schema: "project",
                table: "ProjectDocuments",
                column: "projectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDocuments_updatedByUserId",
                schema: "project",
                table: "ProjectDocuments",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_leftByUserId",
                schema: "project",
                table: "ProjectMembers",
                column: "leftByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_organizationId_projectId_userId",
                schema: "project",
                table: "ProjectMembers",
                columns: new[] { "organizationId", "projectId", "userId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_organizationId_userId",
                schema: "project",
                table: "ProjectMembers",
                columns: new[] { "organizationId", "userId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_projectId",
                schema: "project",
                table: "ProjectMembers",
                column: "projectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_updatedByUserId",
                schema: "project",
                table: "ProjectMembers",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_userId",
                schema: "project",
                table: "ProjectMembers",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_deleteBy",
                schema: "project",
                table: "Projects",
                column: "deleteBy");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_organizationId_code",
                schema: "project",
                table: "Projects",
                columns: new[] { "organizationId", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_organizationId_status",
                schema: "project",
                table: "Projects",
                columns: new[] { "organizationId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ownerUserId",
                schema: "project",
                table: "Projects",
                column: "ownerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_updatedByUserId",
                schema: "project",
                table: "Projects",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "ux_project_tenant_id",
                schema: "project",
                table: "Projects",
                columns: new[] { "organizationId", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RagIndexes_organizationId_indexName",
                schema: "rag",
                table: "RagIndexes",
                columns: new[] { "organizationId", "indexName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_rag_index_tenant_id",
                schema: "rag",
                table: "RagIndexes",
                columns: new[] { "organizationId", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RagIndexRecords_chunkId",
                schema: "rag",
                table: "RagIndexRecords",
                column: "chunkId");

            migrationBuilder.CreateIndex(
                name: "IX_RagIndexRecords_organizationId_ragIndexId_chunkId",
                schema: "rag",
                table: "RagIndexRecords",
                columns: new[] { "organizationId", "ragIndexId", "chunkId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RagIndexRecords_organizationId_ragIndexId_vectorPointId",
                schema: "rag",
                table: "RagIndexRecords",
                columns: new[] { "organizationId", "ragIndexId", "vectorPointId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RagIndexRecords_ragIndexId",
                schema: "rag",
                table: "RagIndexRecords",
                column: "ragIndexId");

            migrationBuilder.CreateIndex(
                name: "IX_RetrievalHits_chunkId",
                schema: "rag",
                table: "RetrievalHits",
                column: "chunkId");

            migrationBuilder.CreateIndex(
                name: "IX_RetrievalHits_organizationId_chunkId",
                schema: "rag",
                table: "RetrievalHits",
                columns: new[] { "organizationId", "chunkId" });

            migrationBuilder.CreateIndex(
                name: "IX_RetrievalHits_organizationId_retrievalRunId_rankNo",
                schema: "rag",
                table: "RetrievalHits",
                columns: new[] { "organizationId", "retrievalRunId", "rankNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RetrievalHits_retrievalRunId",
                schema: "rag",
                table: "RetrievalHits",
                column: "retrievalRunId");

            migrationBuilder.CreateIndex(
                name: "IX_RetrievalRuns_aiTurnId",
                schema: "rag",
                table: "RetrievalRuns",
                column: "aiTurnId");

            migrationBuilder.CreateIndex(
                name: "IX_RetrievalRuns_organizationId_aiTurnId_createdAt",
                schema: "rag",
                table: "RetrievalRuns",
                columns: new[] { "organizationId", "aiTurnId", "createdAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RetrievalRuns_ragIndexId",
                schema: "rag",
                table: "RetrievalRuns",
                column: "ragIndexId");

            migrationBuilder.CreateIndex(
                name: "ux_retrieval_run_tenant_id",
                schema: "rag",
                table: "RetrievalRuns",
                columns: new[] { "organizationId", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleApiFunctions_functionId",
                schema: "security",
                table: "RoleApiFunctions",
                column: "functionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleApiFunctions_grantedByUserId",
                schema: "security",
                table: "RoleApiFunctions",
                column: "grantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleApiFunctions_organizationId_functionId_effect",
                schema: "security",
                table: "RoleApiFunctions",
                columns: new[] { "organizationId", "functionId", "effect" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleApiFunctions_organizationId_roleId",
                schema: "security",
                table: "RoleApiFunctions",
                columns: new[] { "organizationId", "roleId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleApiFunctions_organizationId_roleId_functionId",
                schema: "security",
                table: "RoleApiFunctions",
                columns: new[] { "organizationId", "roleId", "functionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleApiFunctions_revokedByUserId",
                schema: "security",
                table: "RoleApiFunctions",
                column: "revokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleApiFunctions_roleId",
                schema: "security",
                table: "RoleApiFunctions",
                column: "roleId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_assigneeUserId",
                schema: "task",
                table: "Tasks",
                column: "assigneeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_createdByUserId",
                schema: "task",
                table: "Tasks",
                column: "createdByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_deleteBy",
                schema: "task",
                table: "Tasks",
                column: "deleteBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_departmentId",
                schema: "task",
                table: "Tasks",
                column: "departmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_organizationId_assigneeUserId_status",
                schema: "task",
                table: "Tasks",
                columns: new[] { "organizationId", "assigneeUserId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_organizationId_dueAt",
                schema: "task",
                table: "Tasks",
                columns: new[] { "organizationId", "dueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_organizationId_projectId_status",
                schema: "task",
                table: "Tasks",
                columns: new[] { "organizationId", "projectId", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_parentTaskId",
                schema: "task",
                table: "Tasks",
                column: "parentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_projectId",
                schema: "task",
                table: "Tasks",
                column: "projectId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_updatedByUserId",
                schema: "task",
                table: "Tasks",
                column: "updatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_deleteBy",
                schema: "organization",
                table: "UserProfiles",
                column: "deleteBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_updatedByUserId",
                schema: "organization",
                table: "UserProfiles",
                column: "updatedByUserId");

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
                name: "FK_ConversationContexts_Documents_documentId",
                schema: "chat",
                table: "ConversationContexts",
                column: "documentId",
                principalSchema: "document",
                principalTable: "Documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentAccessGrants_Documents_documentId",
                schema: "document",
                table: "DocumentAccessGrants",
                column: "documentId",
                principalSchema: "document",
                principalTable: "Documents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentChunkContents_DocumentChunks_chunkId",
                schema: "document",
                table: "DocumentChunkContents",
                column: "chunkId",
                principalSchema: "document",
                principalTable: "DocumentChunks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentChunks_DocumentVersions_documentVersionId",
                schema: "document",
                table: "DocumentChunks",
                column: "documentVersionId",
                principalSchema: "document",
                principalTable: "DocumentVersions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_DocumentVersions_currentVersionId",
                schema: "document",
                table: "Documents",
                column: "currentVersionId",
                principalSchema: "document",
                principalTable: "DocumentVersions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentFolders_AspNetUsers_deleteBy",
                schema: "document",
                table: "DocumentFolders");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentFolders_AspNetUsers_ownerUserId",
                schema: "document",
                table: "DocumentFolders");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentFolders_AspNetUsers_updatedByUserId",
                schema: "document",
                table: "DocumentFolders");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AspNetUsers_deleteBy",
                schema: "document",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AspNetUsers_ownerUserId",
                schema: "document",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AspNetUsers_updatedByUserId",
                schema: "document",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersions_AspNetUsers_createdByUserId",
                schema: "document",
                table: "DocumentVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_AspNetUsers_deleteBy",
                schema: "organization",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_AspNetUsers_updatedByUserId",
                schema: "organization",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentFolders_Organizations_organizationId",
                schema: "document",
                table: "DocumentFolders");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Organizations_organizationId",
                schema: "document",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersions_Organizations_organizationId",
                schema: "document",
                table: "DocumentVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersions_Documents_documentId",
                schema: "document",
                table: "DocumentVersions");

            migrationBuilder.DropTable(
                name: "AccessTokens",
                schema: "security");

            migrationBuilder.DropTable(
                name: "ActivityLogs",
                schema: "security");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "CalendarEvents",
                schema: "calendar");

            migrationBuilder.DropTable(
                name: "Citations",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "ConversationContexts",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "DepartmentMemberships",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "DocumentAccessGrants",
                schema: "document");

            migrationBuilder.DropTable(
                name: "DocumentChunkContents",
                schema: "document");

            migrationBuilder.DropTable(
                name: "MeetingParticipants",
                schema: "meeting");

            migrationBuilder.DropTable(
                name: "OrganizationMemberships",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "OrganizationRoleAssignments",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "OrganizationSettings",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "ProjectDocuments",
                schema: "project");

            migrationBuilder.DropTable(
                name: "ProjectMembers",
                schema: "project");

            migrationBuilder.DropTable(
                name: "RagIndexRecords",
                schema: "rag");

            migrationBuilder.DropTable(
                name: "RetrievalHits",
                schema: "rag");

            migrationBuilder.DropTable(
                name: "RoleApiFunctions",
                schema: "security");

            migrationBuilder.DropTable(
                name: "Tasks",
                schema: "task");

            migrationBuilder.DropTable(
                name: "UserProfiles",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Meetings",
                schema: "meeting");

            migrationBuilder.DropTable(
                name: "DocumentChunks",
                schema: "document");

            migrationBuilder.DropTable(
                name: "RetrievalRuns",
                schema: "rag");

            migrationBuilder.DropTable(
                name: "ApiFunctions",
                schema: "security");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Departments",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Projects",
                schema: "project");

            migrationBuilder.DropTable(
                name: "AiTurns",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "RagIndexes",
                schema: "rag");

            migrationBuilder.DropTable(
                name: "Messages",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "Conversations",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Organizations",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "Documents",
                schema: "document");

            migrationBuilder.DropTable(
                name: "DocumentFolders",
                schema: "document");

            migrationBuilder.DropTable(
                name: "DocumentVersions",
                schema: "document");
        }
    }
}
