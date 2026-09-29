START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity."__EFMigrationsHistory" WHERE "MigrationId" = '20260928072207_AddChatMessageDeletionAndSources') THEN
    ALTER TABLE chat."Messages" ADD "deletedAt" timestamptz;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity."__EFMigrationsHistory" WHERE "MigrationId" = '20260928072207_AddChatMessageDeletionAndSources') THEN
    ALTER TABLE chat."Messages" ADD "deletedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity."__EFMigrationsHistory" WHERE "MigrationId" = '20260928072207_AddChatMessageDeletionAndSources') THEN
    ALTER TABLE chat."Citations" ADD score real;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity."__EFMigrationsHistory" WHERE "MigrationId" = '20260928072207_AddChatMessageDeletionAndSources') THEN
    CREATE INDEX "IX_Messages_deletedByUserId" ON chat."Messages" ("deletedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity."__EFMigrationsHistory" WHERE "MigrationId" = '20260928072207_AddChatMessageDeletionAndSources') THEN
    CREATE UNIQUE INDEX "IX_AiTurns_organizationId_correlationId" ON chat."AiTurns" ("organizationId", "correlationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity."__EFMigrationsHistory" WHERE "MigrationId" = '20260928072207_AddChatMessageDeletionAndSources') THEN
    ALTER TABLE chat."Messages" ADD CONSTRAINT "FK_Messages_AspNetUsers_deletedByUserId" FOREIGN KEY ("deletedByUserId") REFERENCES identity."AspNetUsers" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM identity."__EFMigrationsHistory" WHERE "MigrationId" = '20260928072207_AddChatMessageDeletionAndSources') THEN
    INSERT INTO identity."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260928072207_AddChatMessageDeletionAndSources', '10.0.10');
    END IF;
END $EF$;
COMMIT;

