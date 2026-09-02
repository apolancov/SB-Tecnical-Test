-- =============================================================================
-- Application database schema (SQL Server).
--
-- This file mirrors the Entity Framework Core migrations
--   `src/backend/Infrastructure/Migrations/20260828211315_AddInstitution.cs`,
--   `src/backend/Infrastructure/Migrations/20260829000000_AddUsers.cs`, and
--   `src/backend/Infrastructure/Migrations/20260901000000_AddClassificationLookupTables.cs`
-- for manual deployments. It also pre-registers the migrations in
-- `__EFMigrationsHistory` so that a subsequent `dotnet ef database update`
-- recognizes the schema as up to date.
--
-- When this script is executed, the database defined in the `DefaultConnection`
-- connection string must already exist. The script is idempotent: tables and
-- the migrations history rows are created only when missing.
--
-- The `Institutions.Category`, `Institutions.StatePower`, and `Institutions.Sector`
-- columns have been replaced with foreign keys (`CategoryId`, `StatePowerId`,
-- `SectorId`) that reference the `Categories`, `StatePowers`, and `Sectors`
-- lookup tables. Each lookup row carries the canonical name used for the
-- classification and is uniquely indexed on `Name`. The lookup tables are
-- pre-populated by `seed.sql`; no other write path is expected to create
-- classification rows.
--
-- Backwards compatibility with the pre-Fase-7 schema:
--   The `IF OBJECT_ID ... IS NULL` guards below only create each table when
--   it is missing. If `dbo.Institutions` already exists with the old string
--   columns (`Category`, `StatePower`, `Sector` as `nvarchar`), the migration
--   block further down converts it to the FK schema in place. The block
--   auto-populates the lookup tables from the distinct values found in the
--   existing `Institutions` rows so the FK columns can be resolved even
--   before `seed.sql` runs. The block is idempotent: it is a no-op whenever
--   `dbo.Institutions` already has the new schema.
-- =============================================================================

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        Id    uniqueidentifier NOT NULL,
        Name  nvarchar(128)    NOT NULL,

        CONSTRAINT PK_Categories PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX UX_Categories_Name
        ON dbo.Categories (Name);
END;
GO

IF OBJECT_ID(N'dbo.StatePowers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StatePowers
    (
        Id    uniqueidentifier NOT NULL,
        Name  nvarchar(128)    NOT NULL,

        CONSTRAINT PK_StatePowers PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX UX_StatePowers_Name
        ON dbo.StatePowers (Name);
END;
GO

IF OBJECT_ID(N'dbo.Sectors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sectors
    (
        Id    uniqueidentifier NOT NULL,
        Name  nvarchar(128)    NOT NULL,

        CONSTRAINT PK_Sectors PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX UX_Sectors_Name
        ON dbo.Sectors (Name);
END;
GO

IF OBJECT_ID(N'dbo.Institutions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Institutions
    (
        Id            uniqueidentifier NOT NULL,
        Name          nvarchar(256)    NOT NULL,
        CategoryId    uniqueidentifier NOT NULL,
        StatePowerId  uniqueidentifier NOT NULL,
        SectorId      uniqueidentifier NOT NULL,
        CreatedAt     datetime2        NOT NULL
            CONSTRAINT DF_Institutions_CreatedAt DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_Institutions PRIMARY KEY (Id),
        CONSTRAINT FK_Institutions_Categories_CategoryId
            FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (Id),
        CONSTRAINT FK_Institutions_StatePowers_StatePowerId
            FOREIGN KEY (StatePowerId) REFERENCES dbo.StatePowers (Id),
        CONSTRAINT FK_Institutions_Sectors_SectorId
            FOREIGN KEY (SectorId) REFERENCES dbo.Sectors (Id)
    );

    CREATE UNIQUE INDEX UX_Institutions_Name
        ON dbo.Institutions (Name);

    CREATE INDEX IX_Institutions_CategoryId
        ON dbo.Institutions (CategoryId);

    CREATE INDEX IX_Institutions_StatePowerId
        ON dbo.Institutions (StatePowerId);

    CREATE INDEX IX_Institutions_SectorId
        ON dbo.Institutions (SectorId);
END;
GO

-- -----------------------------------------------------------------------------
-- In-place migration: convert dbo.Institutions from the pre-Fase-7 schema
-- (`Category`/`StatePower`/`Sector` as `nvarchar`) to the current FK schema
-- (`CategoryId`/`StatePowerId`/`SectorId` as `uniqueidentifier`).
--
-- Runs only when the table exists but still carries the old string columns.
-- Auto-populates the lookup tables from the distinct values in the existing
-- `Institutions` rows so the FK columns can be resolved without depending on
-- `seed.sql`. Re-running this script on a database that already has the new
-- schema is a no-op.
-- -----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Institutions', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Institutions', N'Category') IS NOT NULL
   AND COL_LENGTH(N'dbo.Institutions', N'CategoryId') IS NULL
BEGIN
    PRINT 'Migrating dbo.Institutions from the pre-Fase-7 schema to the FK schema...';

    -- The statements below reference the pre-Fase-7 string columns (`Category`,
    -- `StatePower`, `Sector`) that do not exist on the current FK schema.
    -- SQL Server resolves column names during the batch's parse/compile phase,
    -- so referencing them directly inside this `IF` block would raise
    -- "Invalid column name" errors even though the IF condition is false on
    -- the current schema. Wrapping the body in `EXEC` defers parsing until
    -- execution time, so the statements are only parsed when the legacy
    -- schema is actually detected.
    DECLARE @migrationSql nvarchar(max) = N'
        -- Drop any pre-existing FK constraints (defensive; should not be present
        -- on the pre-Fase-7 schema, but keeps the block safe if it is ever
        -- executed against a partially migrated database).
        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N''FK_Institutions_Categories_CategoryId'')
            ALTER TABLE dbo.Institutions DROP CONSTRAINT FK_Institutions_Categories_CategoryId;
        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N''FK_Institutions_StatePowers_StatePowerId'')
            ALTER TABLE dbo.Institutions DROP CONSTRAINT FK_Institutions_StatePowers_StatePowerId;
        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N''FK_Institutions_Sectors_SectorId'')
            ALTER TABLE dbo.Institutions DROP CONSTRAINT FK_Institutions_Sectors_SectorId;

        -- Auto-populate the lookup tables from the values already present in the
        -- old `Institutions` rows. The `NOT EXISTS` guards keep the inserts
        -- idempotent against a database that already has the canonical names
        -- loaded by `seed.sql`.
        INSERT INTO dbo.Categories (Id, Name)
        SELECT NEWID(), src.Category
        FROM (SELECT DISTINCT Category FROM dbo.Institutions WHERE Category IS NOT NULL) AS src
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Name = src.Category);

        INSERT INTO dbo.StatePowers (Id, Name)
        SELECT NEWID(), src.StatePower
        FROM (SELECT DISTINCT StatePower FROM dbo.Institutions WHERE StatePower IS NOT NULL) AS src
        WHERE NOT EXISTS (SELECT 1 FROM dbo.StatePowers s WHERE s.Name = src.StatePower);

        INSERT INTO dbo.Sectors (Id, Name)
        SELECT NEWID(), src.Sector
        FROM (SELECT DISTINCT Sector FROM dbo.Institutions WHERE Sector IS NOT NULL) AS src
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Sectors s WHERE s.Name = src.Sector);

        -- Add the new FK columns as nullable so the UPDATE can run.
        ALTER TABLE dbo.Institutions ADD CategoryId   uniqueidentifier NULL;
        ALTER TABLE dbo.Institutions ADD StatePowerId uniqueidentifier NULL;
        ALTER TABLE dbo.Institutions ADD SectorId     uniqueidentifier NULL;

        -- Resolve every existing row''s FK by name.
        UPDATE i SET i.CategoryId   = c.Id FROM dbo.Institutions AS i INNER JOIN dbo.Categories   AS c ON c.Name = i.Category;
        UPDATE i SET i.StatePowerId = s.Id FROM dbo.Institutions AS i INNER JOIN dbo.StatePowers AS s ON s.Name = i.StatePower;
        UPDATE i SET i.SectorId     = s.Id FROM dbo.Institutions AS i INNER JOIN dbo.Sectors     AS s ON s.Name = i.Sector;

        -- Defensive check: every row must have been migrated before we tighten
        -- the schema. This surfaces any orphan classification values with a
        -- clear, actionable error rather than letting `ALTER COLUMN ... NOT NULL`
        -- fail with a generic constraint violation.
        IF EXISTS (
            SELECT 1 FROM dbo.Institutions
            WHERE CategoryId IS NULL OR StatePowerId IS NULL OR SectorId IS NULL
        )
            THROW 50000, ''Cannot migrate dbo.Institutions: at least one row references a Category/StatePower/Sector value not present in the lookup tables. Add the missing rows to dbo.Categories/StatePowers/Sectors (or fix the offending data) and re-run schema.sql.'', 1;

        -- Tighten the FK columns to NOT NULL, matching the EF Core model.
        ALTER TABLE dbo.Institutions ALTER COLUMN CategoryId   uniqueidentifier NOT NULL;
        ALTER TABLE dbo.Institutions ALTER COLUMN StatePowerId uniqueidentifier NOT NULL;
        ALTER TABLE dbo.Institutions ALTER COLUMN SectorId     uniqueidentifier NOT NULL;

        -- Drop the old secondary indexes if they survived from the pre-Fase-7
        -- schema, then drop the old string columns.
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N''dbo.Institutions'') AND name = N''IX_Institutions_Category'')
            DROP INDEX IX_Institutions_Category ON dbo.Institutions;
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N''dbo.Institutions'') AND name = N''IX_Institutions_StatePower'')
            DROP INDEX IX_Institutions_StatePower ON dbo.Institutions;
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N''dbo.Institutions'') AND name = N''IX_Institutions_Sector'')
            DROP INDEX IX_Institutions_Sector ON dbo.Institutions;

        ALTER TABLE dbo.Institutions DROP COLUMN Category;
        ALTER TABLE dbo.Institutions DROP COLUMN StatePower;
        ALTER TABLE dbo.Institutions DROP COLUMN Sector;

        -- Add the new secondary indexes and FK constraints using the same names
        -- as the `CREATE TABLE dbo.Institutions` block above and the EF Core
        -- migration `20260901000000_AddClassificationLookupTables`.
        CREATE INDEX IX_Institutions_CategoryId   ON dbo.Institutions (CategoryId);
        CREATE INDEX IX_Institutions_StatePowerId ON dbo.Institutions (StatePowerId);
        CREATE INDEX IX_Institutions_SectorId     ON dbo.Institutions (SectorId);

        ALTER TABLE dbo.Institutions
            ADD CONSTRAINT FK_Institutions_Categories_CategoryId
            FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (Id);
        ALTER TABLE dbo.Institutions
            ADD CONSTRAINT FK_Institutions_StatePowers_StatePowerId
            FOREIGN KEY (StatePowerId) REFERENCES dbo.StatePowers (Id);
        ALTER TABLE dbo.Institutions
            ADD CONSTRAINT FK_Institutions_Sectors_SectorId
            FOREIGN KEY (SectorId) REFERENCES dbo.Sectors (Id);
    ';

    EXEC sp_executesql @migrationSql;

    PRINT 'Migration of dbo.Institutions completed.';
END;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id            uniqueidentifier NOT NULL,
        Username      nvarchar(64)     NOT NULL,
        Email         nvarchar(256)    NOT NULL,
        PasswordHash  nvarchar(1024)   NOT NULL,
        Role          nvarchar(32)     NOT NULL,
        IsActive      bit              NOT NULL,
        CreatedAt     datetime2        NOT NULL
            CONSTRAINT DF_Users_CreatedAt DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_Users PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX UX_Users_Username
        ON dbo.Users (Username);

    CREATE UNIQUE INDEX UX_Users_Email
        ON dbo.Users (Email);
END;
GO

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__EFMigrationsHistory
    (
        MigrationId    nvarchar(150) NOT NULL,
        ProductVersion nvarchar(32)  NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20260828211315_AddInstitution'
)
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260828211315_AddInstitution', N'8.0.10');
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20260829000000_AddUsers'
)
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260829000000_AddUsers', N'8.0.10');
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20260901000000_AddClassificationLookupTables'
)
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260901000000_AddClassificationLookupTables', N'8.0.10');
END;
GO

-- =============================================================================
-- Request Management module (Fase 9).
--
-- Adds the six tables that back the request domain:
--   * Areas                       — catalog of organizational areas.
--   * RequestTypes                — catalog of request kinds.
--   * Requests                    — main aggregate.
--   * RequestStatusHistory        — append-only history of every status change.
--   * RequestComments             — public and internal comments.
--   * RequestNotifications        — persistence-only notifications (no provider).
--
-- Mirrors the Entity Framework Core migration
--   `src/backend/Infrastructure/Migrations/20260901010000_AddRequestManagement.cs`
-- for manual deployments and pre-registers it in `__EFMigrationsHistory`.
-- =============================================================================

IF OBJECT_ID(N'dbo.Areas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Areas
    (
        Id       uniqueidentifier NOT NULL,
        Name     nvarchar(128)    NOT NULL,
        IsActive bit              NOT NULL,

        CONSTRAINT PK_Areas PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX UX_Areas_Name
        ON dbo.Areas (Name);

    CREATE INDEX IX_Areas_IsActive
        ON dbo.Areas (IsActive);
END;
GO

IF OBJECT_ID(N'dbo.RequestTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RequestTypes
    (
        Id          uniqueidentifier NOT NULL,
        Name        nvarchar(128)    NOT NULL,
        Description nvarchar(512)    NOT NULL,
        IsActive    bit              NOT NULL,

        CONSTRAINT PK_RequestTypes PRIMARY KEY (Id)
    );

    CREATE UNIQUE INDEX UX_RequestTypes_Name
        ON dbo.RequestTypes (Name);

    CREATE INDEX IX_RequestTypes_IsActive
        ON dbo.RequestTypes (IsActive);
END;
GO

IF OBJECT_ID(N'dbo.Requests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Requests
    (
        Id            uniqueidentifier NOT NULL,
        Code          nvarchar(32)     NOT NULL,
        Title         nvarchar(256)    NOT NULL,
        Description   nvarchar(4096)   NOT NULL,
        Priority      nvarchar(16)     NOT NULL,
        Status        nvarchar(16)     NOT NULL,
        CreatedAt     datetime2        NOT NULL
            CONSTRAINT DF_Requests_CreatedAt DEFAULT (GETUTCDATE()),
        DueDate       datetime2        NULL,
        EvidenceUrl   nvarchar(2048)   NULL,
        ClosedAt      datetime2        NULL,
        RequesterId   uniqueidentifier NOT NULL,
        ResponsibleId uniqueidentifier NULL,
        AreaId        uniqueidentifier NOT NULL,
        RequestTypeId uniqueidentifier NOT NULL,

        CONSTRAINT PK_Requests PRIMARY KEY (Id),
        CONSTRAINT FK_Requests_Areas_AreaId
            FOREIGN KEY (AreaId) REFERENCES dbo.Areas (Id),
        CONSTRAINT FK_Requests_RequestTypes_RequestTypeId
            FOREIGN KEY (RequestTypeId) REFERENCES dbo.RequestTypes (Id),
        CONSTRAINT FK_Requests_Users_RequesterId
            FOREIGN KEY (RequesterId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Requests_Users_ResponsibleId
            FOREIGN KEY (ResponsibleId) REFERENCES dbo.Users (Id)
    );

    CREATE UNIQUE INDEX UX_Requests_Code
        ON dbo.Requests (Code);

    CREATE INDEX IX_Requests_Status
        ON dbo.Requests (Status);

    CREATE INDEX IX_Requests_Priority
        ON dbo.Requests (Priority);

    CREATE INDEX IX_Requests_RequesterId
        ON dbo.Requests (RequesterId);

    CREATE INDEX IX_Requests_ResponsibleId
        ON dbo.Requests (ResponsibleId);

    CREATE INDEX IX_Requests_AreaId
        ON dbo.Requests (AreaId);

    CREATE INDEX IX_Requests_RequestTypeId
        ON dbo.Requests (RequestTypeId);

    CREATE INDEX IX_Requests_CreatedAt
        ON dbo.Requests (CreatedAt);

    CREATE INDEX IX_Requests_DueDate
        ON dbo.Requests (DueDate);

    CREATE INDEX IX_Requests_ClosedAt
        ON dbo.Requests (ClosedAt);
END;
GO

IF OBJECT_ID(N'dbo.RequestStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RequestStatusHistory
    (
        Id             uniqueidentifier NOT NULL,
        RequestId      uniqueidentifier NOT NULL,
        PreviousStatus nvarchar(16)     NOT NULL,
        NewStatus      nvarchar(16)     NOT NULL,
        Date           datetime2        NOT NULL,
        Comment        nvarchar(1024)   NOT NULL,
        ChangedById    uniqueidentifier NOT NULL,

        CONSTRAINT PK_RequestStatusHistory PRIMARY KEY (Id),
        CONSTRAINT FK_RequestStatusHistory_Requests_RequestId
            FOREIGN KEY (RequestId) REFERENCES dbo.Requests (Id) ON DELETE CASCADE,
        CONSTRAINT FK_RequestStatusHistory_Users_ChangedById
            FOREIGN KEY (ChangedById) REFERENCES dbo.Users (Id)
    );

    CREATE INDEX IX_RequestStatusHistory_RequestId
        ON dbo.RequestStatusHistory (RequestId);

    CREATE INDEX IX_RequestStatusHistory_Date
        ON dbo.RequestStatusHistory (Date);

    CREATE INDEX IX_RequestStatusHistory_ChangedById
        ON dbo.RequestStatusHistory (ChangedById);
END;
GO

IF OBJECT_ID(N'dbo.RequestComments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RequestComments
    (
        Id         uniqueidentifier NOT NULL,
        RequestId  uniqueidentifier NOT NULL,
        AuthorId   uniqueidentifier NOT NULL,
        Text       nvarchar(4096)   NOT NULL,
        Visibility nvarchar(16)     NOT NULL,
        Date       datetime2        NOT NULL,

        CONSTRAINT PK_RequestComments PRIMARY KEY (Id),
        CONSTRAINT FK_RequestComments_Requests_RequestId
            FOREIGN KEY (RequestId) REFERENCES dbo.Requests (Id) ON DELETE CASCADE,
        CONSTRAINT FK_RequestComments_Users_AuthorId
            FOREIGN KEY (AuthorId) REFERENCES dbo.Users (Id)
    );

    CREATE INDEX IX_RequestComments_RequestId
        ON dbo.RequestComments (RequestId);

    CREATE INDEX IX_RequestComments_AuthorId
        ON dbo.RequestComments (AuthorId);

    CREATE INDEX IX_RequestComments_RequestId_Visibility
        ON dbo.RequestComments (RequestId, Visibility);
END;
GO

IF OBJECT_ID(N'dbo.RequestNotifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RequestNotifications
    (
        Id                uniqueidentifier NOT NULL,
        RequestId         uniqueidentifier NOT NULL,
        DestinationUserId uniqueidentifier NOT NULL,
        Channel           nvarchar(16)     NOT NULL,
        Status            nvarchar(16)     NOT NULL,
        Subject           nvarchar(256)    NOT NULL,
        Message           nvarchar(4096)   NOT NULL,
        Date              datetime2        NOT NULL,

        CONSTRAINT PK_RequestNotifications PRIMARY KEY (Id),
        CONSTRAINT FK_RequestNotifications_Requests_RequestId
            FOREIGN KEY (RequestId) REFERENCES dbo.Requests (Id) ON DELETE CASCADE,
        CONSTRAINT FK_RequestNotifications_Users_DestinationUserId
            FOREIGN KEY (DestinationUserId) REFERENCES dbo.Users (Id)
    );

    CREATE INDEX IX_RequestNotifications_RequestId
        ON dbo.RequestNotifications (RequestId);

    CREATE INDEX IX_RequestNotifications_DestinationUserId
        ON dbo.RequestNotifications (DestinationUserId);

    CREATE INDEX IX_RequestNotifications_Status
        ON dbo.RequestNotifications (Status);

    CREATE INDEX IX_RequestNotifications_Date
        ON dbo.RequestNotifications (Date);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20260901010000_AddRequestManagement'
)
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260901010000_AddRequestManagement', N'8.0.10');
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20260901120000_AddRequestEvidenceAndClosedAt'
)
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260901120000_AddRequestEvidenceAndClosedAt', N'8.0.10');
END;
GO

-- =============================================================================
-- Audit Log module.
--
-- Adds the append-only `dbo.AuditLog` table that backs the technical auditing
-- endpoint `GET /api/auditoria` (Admin role only).
--
-- Mirrors the Entity Framework Core migration
--   `src/backend/Infrastructure/Migrations/20260902000000_AddAuditLog.cs`
-- for manual deployments and pre-registers it in `__EFMigrationsHistory`.
--
-- Columns:
--   * Id              — surrogate primary key (uniqueidentifier).
--   * Timestamp       — UTC timestamp when the audited event happened.
--   * Action          — action name (LoginSucceeded, LoginFailed,
--                       RequestCreated, RequestUpdated, RequestAssigned,
--                       RequestStatusChanged, RequestReopened,
--                       RequestCommentAdded, InstitutionCreated,
--                       InstitutionUpdated, InstitutionDeleted,
--                       AuthorizationDenied, ...).
--   * Outcome         — Success, Failure, or Denied.
--   * EntityType      — type of the audited entity (e.g. 'Request', 'User',
--                       'Institution'). Indexed for filter lookups.
--   * EntityId        — string identifier of the audited entity when present.
--   * Details         — short human-readable description of the event.
--   * IpAddress       — remote client IP (honors X-Forwarded-For).
--   * ActorUserId     — Guid of the user that triggered the event when known.
--   * ActorUserName   — username at the time of the event (cached for audit
--                       trails even after the user is later renamed).
--
-- The table is intentionally append-only. No update or delete operations are
-- generated by the application. There are no foreign keys to `dbo.Users` because
-- the actor may be anonymous (failed login attempts) or the user may be
-- removed in the future while the audit row must remain.
-- =============================================================================

IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLog
    (
        Id            uniqueidentifier NOT NULL,
        Timestamp     datetime2        NOT NULL,
        Action        nvarchar(48)     NOT NULL,
        Outcome       nvarchar(16)     NOT NULL,
        EntityType    nvarchar(64)     NOT NULL,
        EntityId      nvarchar(64)     NULL,
        Details       nvarchar(2048)   NULL,
        IpAddress     nvarchar(64)     NULL,
        ActorUserId   uniqueidentifier NULL,
        ActorUserName nvarchar(64)     NULL,

        CONSTRAINT PK_AuditLog PRIMARY KEY (Id)
    );

    CREATE INDEX IX_AuditLog_Timestamp
        ON dbo.AuditLog (Timestamp);

    CREATE INDEX IX_AuditLog_Action
        ON dbo.AuditLog (Action);

    CREATE INDEX IX_AuditLog_Outcome
        ON dbo.AuditLog (Outcome);

    CREATE INDEX IX_AuditLog_EntityType
        ON dbo.AuditLog (EntityType);

    CREATE INDEX IX_AuditLog_ActorUserId
        ON dbo.AuditLog (ActorUserId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20260902000000_AddAuditLog'
)
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260902000000_AddAuditLog', N'8.0.10');
END;
GO