-- ApiFunction menu metadata for the Nexora catalog.
-- Run this script after the application schema has been created.
-- It creates/updates menu-only parent rows and updates existing child rows;
-- it does not create or delete executable permissions.

BEGIN;

ALTER TABLE security."ApiFunctions"
    ADD COLUMN IF NOT EXISTS "menuParentCode" varchar(150),
    ADD COLUMN IF NOT EXISTS "displayOrder" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "isMenuVisible" boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS "isMobile" boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS "menuUrl" varchar(300);

CREATE INDEX IF NOT EXISTS "ix_api_functions_menu_parent_order_status"
    ON security."ApiFunctions" ("menuParentCode", "displayOrder", "status");

-- Controller/menu parent records. These are not HTTP permissions: route '#'
-- is reserved for menu-only nodes and no request can match it.
INSERT INTO security."ApiFunctions"
(
    "id",
    "code",
    "module",
    "name",
    "description",
    "httpMethod",
    "routeTemplate",
    "actionType",
    "riskLevel",
    "requiresConfirmation",
    "status",
    "version",
    "createdAt",
    "updatedAt",
    "menuParentCode",
    "displayOrder",
    "isMenuVisible",
    "isMobile",
    "menuUrl"
)
VALUES
    ('86e56039-fb01-4325-b344-5c8fe54ddc32', 'AUTH', 'Auth', 'Auth', 'Auth controller', 'ANY', '#', 'READ', 'LOW', false, 'ACTIVE', 1, now(), now(), NULL, 1, true, false, '#'),
    ('484e98a1-bbad-4e1e-836b-b5790bb6ad97', 'USER', 'User', 'User', 'User controller', 'ANY', '#', 'READ', 'LOW', false, 'ACTIVE', 1, now(), now(), NULL, 2, true, false, '#'),
    ('e1969f84-0f5f-425c-9cf9-c9bde478c0ab', 'ACTIVITY_LOG', 'ActivityLog', 'Activity Log', 'Activity log controller', 'ANY', '#', 'READ', 'LOW', false, 'ACTIVE', 1, now(), now(), NULL, 3, true, false, '#'),
    ('c734b3b6-9d85-41a4-bc6c-ac4547dd81f9', 'ROLE', 'Role', 'Role', 'Role controller', 'ANY', '#', 'READ', 'LOW', false, 'ACTIVE', 1, now(), now(), NULL, 4, true, false, '#'),
    ('67a5deed-690a-438f-b6ac-cf0569f8de04', 'DOCUMENT', 'Document', 'Document', 'Document controller', 'ANY', '#', 'READ', 'LOW', false, 'ACTIVE', 1, now(), now(), NULL, 5, true, false, '#'),
    ('6477172c-ddc1-43ed-af0b-2abb7e6abcc7', 'AUTHORIZATION', 'Authorization', 'Authorization', 'ApiFunction controller', 'ANY', '#', 'READ', 'LOW', false, 'ACTIVE', 1, now(), now(), NULL, 6, true, false, '#'),
    ('1b61804a-134a-42be-8bd0-c5dbe8aaf656', 'DEPARTMENT', 'Department', 'Department', 'Department controller', 'ANY', '#', 'READ', 'LOW', false, 'ACTIVE', 1, now(), now(), NULL, 7, true, false, '#'),
    ('b2317641-8a29-42c3-8842-5197e27b14a2', 'ORGANIZATION', 'Organization', 'Organization', 'Organization controller', 'ANY', '#', 'READ', 'LOW', false, 'ACTIVE', 1, now(), now(), NULL, 8, true, false, '#')
ON CONFLICT ("code") DO UPDATE SET
    "module" = EXCLUDED."module",
    "name" = EXCLUDED."name",
    "description" = EXCLUDED."description",
    "httpMethod" = EXCLUDED."httpMethod",
    "routeTemplate" = EXCLUDED."routeTemplate",
    "actionType" = EXCLUDED."actionType",
    "riskLevel" = EXCLUDED."riskLevel",
    "requiresConfirmation" = EXCLUDED."requiresConfirmation",
    "status" = EXCLUDED."status",
    "menuParentCode" = EXCLUDED."menuParentCode",
    "displayOrder" = EXCLUDED."displayOrder",
    "isMenuVisible" = EXCLUDED."isMenuVisible",
    "isMobile" = EXCLUDED."isMobile",
    "menuUrl" = EXCLUDED."menuUrl",
    "updatedAt" = now();

UPDATE security."ApiFunctions" AS f
SET
    "menuParentCode" = upper(v.parent_code),
    "displayOrder" = v.display_order,
    "isMenuVisible" = v.is_menu_visible,
    "isMobile" = false,
    "menuUrl" = NULL
FROM (
    VALUES
        ('auth.login', 'auth', 1, false),
        ('auth.refresh', 'auth', 2, false),
        ('auth.logout', 'auth', 3, false),
        ('auth.select_organization', 'auth', 4, false),
        ('auth.me', 'auth', 5, false),
        ('account.profile.read', NULL, 86, false),
        ('account.profile.update', NULL, 87, false),
        ('account.keys.read', NULL, 88, false),
        ('account.keys.save', NULL, 89, false),
        ('account.keys.delete', NULL, 90, false),
        ('user.read', 'user', 10, true),
        ('user.create', 'user', 20, true),
        ('user.read.by_id', 'user', 21, true),
        ('user.update', 'user', 30, true),
        ('user.deactivate', 'user', 40, true),
        ('user.reactivate', 'user', 50, true),
        ('user.reset_password', 'user', 60, true),
        ('user.role.assign', 'user', 70, true),
        ('user.role.revoke', 'user', 80, true),
        ('user.read.roles', 'user', 22, true),
        ('activity_log.read', 'activity_log', 1, true),
        ('role.read', 'role', 10, true),
        ('role.create', 'role', 20, true),
        ('role.read.by_id', 'role', 21, true),
        ('role.update', 'role', 30, true),
        ('role.deactivate', 'role', 40, true),
        ('role.reactivate', 'role', 50, true),
        ('role.permission.grant', 'role', 60, true),
        ('role.permission.revoke', 'role', 70, true),
        ('role.permission.sync', 'role', 80, true),
        ('role.read.permissions', 'role', 22, true),
        ('document.upload', 'document', 1, true),
        ('document.read', 'document', 2, true),
        ('document.status', 'document', 3, true),
        ('document.delete', 'document', 5, true),
        ('document.folder.read', 'document', 10, true),
        ('document.folder.read.by_id', 'document', 11, true),
        ('document.folder.create', 'document', 20, true),
        ('document.folder.update', 'document', 30, true),
        ('document.folder.delete', 'document', 40, true),
        ('document.folder.grants.read', 'document', 50, true),
        ('document.folder.grants.create', 'document', 60, true),
        ('document.folder.grants.revoke', 'document', 70, true),
        ('api_function.read', 'authorization', 1, true),
        ('api_function.create', 'authorization', 2, true),
        ('api_function.read.by_id', 'authorization', 3, true),
        ('api_function.update', 'authorization', 4, true),
        ('api_function.deactivate', 'authorization', 5, true),
        ('api_function.reactivate', 'authorization', 6, true),
        ('department.read', 'department', 10, true),
        ('department.create', 'department', 20, true),
        ('department.read.by_id', 'department', 21, true),
        ('department.update', 'department', 30, true),
        ('department.archive', 'department', 40, true),
        ('department.member.add', 'department', 50, true),
        ('department.member.remove', 'department', 60, true),
        ('department.member.read', 'department', 51, true),
        ('organization.read', 'organization', 10, true),
        ('organization.create', 'organization', 20, true),
        ('organization.read.by_id', 'organization', 21, true),
        ('organization.update', 'organization', 40, true),
        ('organization.suspend', 'organization', 50, true),
        ('organization.activate', 'organization', 60, true),
        ('organization.archive', 'organization', 70, true)
) AS v(code, parent_code, display_order, is_menu_visible)
WHERE upper(f."code") = upper(v.code);

-- Review this result after execution. A non-zero count means the catalog was
-- not seeded/synchronized before this script was run.
SELECT v.code AS missing_catalog_code
FROM (
    VALUES
        ('auth.login'), ('auth.refresh'), ('auth.logout'), ('auth.select_organization'), ('auth.me'),
        ('account.profile.read'), ('account.profile.update'), ('account.keys.read'),
        ('account.keys.save'), ('account.keys.delete'),
        ('user.read'), ('user.create'), ('user.read.by_id'), ('user.update'), ('user.deactivate'),
        ('user.reactivate'), ('user.reset_password'), ('user.role.assign'), ('user.role.revoke'),
        ('user.read.roles'), ('activity_log.read'), ('role.read'), ('role.create'), ('role.read.by_id'),
        ('role.update'), ('role.deactivate'), ('role.reactivate'), ('role.permission.grant'),
        ('role.permission.revoke'), ('role.permission.sync'), ('role.read.permissions'),
        ('document.upload'), ('document.read'), ('document.status'), ('document.folder.read'),
        ('document.folder.read.by_id'), ('document.folder.grants.read'), ('document.folder.create'),
        ('document.folder.grants.create'), ('document.folder.update'), ('document.folder.delete'),
        ('document.folder.grants.revoke'), ('api_function.read'),
        ('api_function.create'), ('api_function.read.by_id'), ('api_function.update'),
        ('api_function.deactivate'), ('api_function.reactivate'), ('department.read'),
        ('department.create'), ('department.read.by_id'), ('department.update'), ('department.archive'),
        ('department.member.add'), ('department.member.remove'), ('department.member.read'),
        ('organization.read'), ('organization.create'), ('organization.read.by_id'), ('organization.update'),
        ('organization.suspend'), ('organization.activate'), ('organization.archive')
) AS v(code)
WHERE NOT EXISTS (
    SELECT 1
    FROM security."ApiFunctions" AS f
    WHERE upper(f."code") = upper(v.code)
);

COMMIT;
