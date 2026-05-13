-- =============================================
-- 60_ProductionOrderSplit.sql
-- Phase 1 Big-Bang Refactor: ProductionOrders -> 5 Tabellen
-- Idempotent, kann mehrfach ausgefuehrt werden (Reapply-fest).
--
-- HINWEIS: Das Skript ist die EINZIGE Wahrheit fuer Schema + Daten + Drop.
--          Es wird MANUELL im Wartungsfenster ausgefuehrt (DBA/Deploy-Skript).
--          Die zugehoerige EF-Migration 20260512120355_AddProductionOrderSplit
--          hat einen LEEREN Up()-Body (Round-4-Strategie, Spec 8.1) und dient
--          nur als History-Marker. Section G dieses Skripts legt den
--          __EFMigrationsHistory-Eintrag selbst an, damit EF beim App-Start
--          die Migration als applied erkennt.
--
-- Ausfuehrungs-Reihenfolge:
--   A) Tabellen anlegen (5 neue Tabellen, OBJECT_ID-Guard)
--   B) Daten-Kopie ProductionOrderPickingStatus (batched 5000/Loop)
--   C) Daten-Kopie ProductionOrderBdeStatus (batched 5000/Loop, IsDoneBde=0)
--   D) Daten-Kopie ProductionOrderAssemblyGroups (5 Zeilen/FA, batched)
--   E) Verifikations-Counts (PRINT)
--   F) Spalten droppen (Default-Constraints + FK + Indexes + 16 Columns)
--   G) __EFMigrationsHistory-Eintrag
-- =============================================

-- =============================================
-- SECTION A: TABELLEN ANLEGEN (idempotent)
-- =============================================

IF OBJECT_ID(N'dbo.ProductionOrderPickingStatus', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionOrderPickingStatus] (
        [Id]                    INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId]     INT               NOT NULL,
        [PickingStatus]         NVARCHAR(50)      NULL,
        [PickingPriority]       INT               NULL,
        [IsReleasedForPicking]  BIT               NOT NULL CONSTRAINT DF_ProductionOrderPickingStatus_IsReleasedForPicking DEFAULT 0,
        [ReleasedAt]            DATETIME2         NULL,
        [ReleasedBy]            NVARCHAR(200)     NULL,
        [AssignedPickerId]      INT               NULL,
        [AssignedPickerName]    NVARCHAR(200)     NULL,
        [HasGlass]              BIT               NOT NULL CONSTRAINT DF_ProductionOrderPickingStatus_HasGlass DEFAULT 0,
        [HasExternalPurchase]   BIT               NOT NULL CONSTRAINT DF_ProductionOrderPickingStatus_HasExternalPurchase DEFAULT 0,
        [HasCoatingParts]       BIT               NOT NULL CONSTRAINT DF_ProductionOrderPickingStatus_HasCoatingParts DEFAULT 0,
        [IsCoatingDone]         BIT               NOT NULL CONSTRAINT DF_ProductionOrderPickingStatus_IsCoatingDone DEFAULT 0,
        [IsDonePicking]         BIT               NOT NULL CONSTRAINT DF_ProductionOrderPickingStatus_IsDonePicking DEFAULT 0,
        [CreatedAt]             DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]             NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]      NVARCHAR(200)     NOT NULL,
        [ModifiedAt]            DATETIME2         NULL,
        [ModifiedBy]            NVARCHAR(200)     NULL,
        [ModifiedByWindows]     NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionOrderPickingStatus] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ProductionOrderPickingStatus_ProductionOrderId] UNIQUE ([ProductionOrderId]),
        CONSTRAINT [FK_ProductionOrderPickingStatus_ProductionOrder]
            FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductionOrderPickingStatus_AssignedPicker]
            FOREIGN KEY ([AssignedPickerId]) REFERENCES [dbo].[Users]([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_ProductionOrderPickingStatus_IsReleasedForPicking]
        ON [dbo].[ProductionOrderPickingStatus]([IsReleasedForPicking]);
    CREATE INDEX [IX_ProductionOrderPickingStatus_AssignedPickerId]
        ON [dbo].[ProductionOrderPickingStatus]([AssignedPickerId])
        WHERE [AssignedPickerId] IS NOT NULL;
    PRINT 'Tabelle ProductionOrderPickingStatus erstellt.';
END
ELSE
    PRINT 'Tabelle ProductionOrderPickingStatus bereits vorhanden - kein Anlegen.';
GO

IF OBJECT_ID(N'dbo.ProductionOrderBdeStatus', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionOrderBdeStatus] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId] INT               NOT NULL,
        [IsDoneBde]         BIT               NOT NULL CONSTRAINT DF_ProductionOrderBdeStatus_IsDoneBde DEFAULT 0,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionOrderBdeStatus] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ProductionOrderBdeStatus_ProductionOrderId] UNIQUE ([ProductionOrderId]),
        CONSTRAINT [FK_ProductionOrderBdeStatus_ProductionOrder]
            FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabelle ProductionOrderBdeStatus erstellt.';
END
ELSE
    PRINT 'Tabelle ProductionOrderBdeStatus bereits vorhanden - kein Anlegen.';
GO

IF OBJECT_ID(N'dbo.ProductionOrderAssemblyGroups', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionOrderAssemblyGroups] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [ProductionOrderId] INT               NOT NULL,
        [GroupKey]          NVARCHAR(10)      NOT NULL,
        [IsApplicable]      BIT               NOT NULL CONSTRAINT DF_ProductionOrderAssemblyGroups_IsApplicable DEFAULT 0,
        [IsCompleted]       BIT               NOT NULL CONSTRAINT DF_ProductionOrderAssemblyGroups_IsCompleted DEFAULT 0,
        [CompletedAt]       DATETIME2         NULL,
        [CompletedBy]       NVARCHAR(200)     NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionOrderAssemblyGroups] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ProductionOrderAssemblyGroups_PO_Key] UNIQUE ([ProductionOrderId], [GroupKey]),
        CONSTRAINT [FK_ProductionOrderAssemblyGroups_ProductionOrder]
            FOREIGN KEY ([ProductionOrderId]) REFERENCES [dbo].[ProductionOrders]([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_ProductionOrderAssemblyGroups_GroupKey_IsApplicable]
        ON [dbo].[ProductionOrderAssemblyGroups]([GroupKey], [IsApplicable]);
    PRINT 'Tabelle ProductionOrderAssemblyGroups erstellt.';
END
ELSE
    PRINT 'Tabelle ProductionOrderAssemblyGroups bereits vorhanden - kein Anlegen.';
GO

IF OBJECT_ID(N'dbo.ProductionOrderAssemblyGroupSpecs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionOrderAssemblyGroupSpecs] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [AssemblyGroupId]   INT               NOT NULL,
        [ArticleId]         INT               NULL,
        [Description]       NVARCHAR(500)     NOT NULL,
        [Quantity]          DECIMAL(18,3)     NULL,
        [Notes]             NVARCHAR(MAX)     NULL,
        [SortOrder]         INT               NOT NULL CONSTRAINT DF_ProductionOrderAssemblyGroupSpecs_SortOrder DEFAULT 0,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionOrderAssemblyGroupSpecs] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ProductionOrderAssemblyGroupSpecs_AssemblyGroup]
            FOREIGN KEY ([AssemblyGroupId]) REFERENCES [dbo].[ProductionOrderAssemblyGroups]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductionOrderAssemblyGroupSpecs_Article]
            FOREIGN KEY ([ArticleId]) REFERENCES [dbo].[Articles]([Id]) ON DELETE SET NULL
    );
    CREATE INDEX [IX_ProductionOrderAssemblyGroupSpecs_AssemblyGroupId]
        ON [dbo].[ProductionOrderAssemblyGroupSpecs]([AssemblyGroupId]);
    CREATE INDEX [IX_ProductionOrderAssemblyGroupSpecs_ArticleId]
        ON [dbo].[ProductionOrderAssemblyGroupSpecs]([ArticleId])
        WHERE [ArticleId] IS NOT NULL;
    PRINT 'Tabelle ProductionOrderAssemblyGroupSpecs erstellt.';
END
ELSE
    PRINT 'Tabelle ProductionOrderAssemblyGroupSpecs bereits vorhanden - kein Anlegen.';
GO

IF OBJECT_ID(N'dbo.ProductionWorkplaceAssemblyGroups', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductionWorkplaceAssemblyGroups] (
        [Id]                    INT IDENTITY(1,1) NOT NULL,
        [ProductionWorkplaceId] INT               NOT NULL,
        [GroupKey]              NVARCHAR(10)      NOT NULL,
        [CreatedAt]             DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]             NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]      NVARCHAR(200)     NOT NULL,
        [ModifiedAt]            DATETIME2         NULL,
        [ModifiedBy]            NVARCHAR(200)     NULL,
        [ModifiedByWindows]     NVARCHAR(200)     NULL,
        CONSTRAINT [PK_ProductionWorkplaceAssemblyGroups] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_ProductionWorkplaceAssemblyGroups_WP_Key] UNIQUE ([ProductionWorkplaceId], [GroupKey]),
        CONSTRAINT [FK_ProductionWorkplaceAssemblyGroups_Workplace]
            FOREIGN KEY ([ProductionWorkplaceId]) REFERENCES [dbo].[ProductionWorkplaces]([Id]) ON DELETE CASCADE
    );
    PRINT 'Tabelle ProductionWorkplaceAssemblyGroups erstellt.';
END
ELSE
    PRINT 'Tabelle ProductionWorkplaceAssemblyGroups bereits vorhanden - kein Anlegen.';
GO

-- =============================================
-- SECTION B: DATEN-KOPIE PickingStatus (batched 5000/Loop)
-- Quelle: dbo.ProductionOrders (alte Spalten)
-- Ziel:   dbo.ProductionOrderPickingStatus
-- Idempotent: NOT EXISTS-Guard + ORDER BY Id schliesst Doppel-INSERT aus.
-- Vorbedingung: Alte Spalten muessen noch in ProductionOrders existieren.
--               Wenn Section F bereits gelaufen ist, wird der INNER-Check via
--               sys.columns uebersprungen (Reapply-fest).
-- =============================================
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders')
             AND name = N'PickingStatus')
BEGIN
    DECLARE @batchSizeB INT = 5000;
    DECLARE @rowsB INT = 1;
    DECLARE @lastIdB INT = 0;

    WHILE @rowsB > 0
    BEGIN
        BEGIN TRANSACTION;
        INSERT INTO dbo.ProductionOrderPickingStatus (
            ProductionOrderId, PickingStatus, PickingPriority,
            IsReleasedForPicking, ReleasedAt, ReleasedBy,
            AssignedPickerId, AssignedPickerName,
            HasGlass, HasExternalPurchase, HasCoatingParts, IsCoatingDone,
            IsDonePicking,
            CreatedAt, CreatedBy, CreatedByWindows)
        SELECT TOP (@batchSizeB)
            p.Id, p.PickingStatus, p.PickingPriority,
            p.IsReleasedForPicking, p.ReleasedAt, p.ReleasedBy,
            p.AssignedPickerId, p.AssignedPickerName,
            p.HasGlass, p.HasExternalPurchase, p.HasCoatingParts, p.IsCoatingDone,
            0,  -- IsDonePicking: bei Migration noch nicht gesetzt
            GETDATE(), 'Migration_60', SYSTEM_USER
        FROM dbo.ProductionOrders p
        WHERE p.Id > @lastIdB
          AND NOT EXISTS (SELECT 1 FROM dbo.ProductionOrderPickingStatus s WHERE s.ProductionOrderId = p.Id)
        ORDER BY p.Id;

        SET @rowsB = @@ROWCOUNT;
        IF @rowsB > 0
            SET @lastIdB = (SELECT MAX(ProductionOrderId) FROM dbo.ProductionOrderPickingStatus);
        COMMIT TRANSACTION;
    END
    DECLARE @cntPS INT = (SELECT COUNT(*) FROM dbo.ProductionOrderPickingStatus);
    PRINT 'PickingStatus-Migration abgeschlossen. Zeilen = ' + CAST(@cntPS AS NVARCHAR);
END
ELSE
    PRINT 'Section B uebersprungen: Spalte PickingStatus in dbo.ProductionOrders nicht (mehr) vorhanden.';
GO

-- =============================================
-- SECTION C: DATEN-KOPIE BdeStatus (batched 5000/Loop)
-- Eager-create fuer jeden ProductionOrder mit IsDoneBde=0.
-- Idempotent: NOT EXISTS-Guard.
-- =============================================
DECLARE @batchSizeC INT = 5000;
DECLARE @rowsC INT = 1;
DECLARE @lastIdC INT = 0;

WHILE @rowsC > 0
BEGIN
    BEGIN TRANSACTION;
    INSERT INTO dbo.ProductionOrderBdeStatus (
        ProductionOrderId, IsDoneBde,
        CreatedAt, CreatedBy, CreatedByWindows)
    SELECT TOP (@batchSizeC)
        p.Id, 0,
        GETDATE(), 'Migration_60', SYSTEM_USER
    FROM dbo.ProductionOrders p
    WHERE p.Id > @lastIdC
      AND NOT EXISTS (SELECT 1 FROM dbo.ProductionOrderBdeStatus s WHERE s.ProductionOrderId = p.Id)
    ORDER BY p.Id;

    SET @rowsC = @@ROWCOUNT;
    IF @rowsC > 0
        SET @lastIdC = (SELECT MAX(ProductionOrderId) FROM dbo.ProductionOrderBdeStatus);
    COMMIT TRANSACTION;
END
DECLARE @cntBde INT = (SELECT COUNT(*) FROM dbo.ProductionOrderBdeStatus);
PRINT 'BdeStatus-Migration abgeschlossen. Zeilen = ' + CAST(@cntBde AS NVARCHAR);
GO

-- =============================================
-- SECTION D: DATEN-KOPIE AssemblyGroups (5 Zeilen/FA, batched)
-- Vorbedingung: Alte HasCooling/HasFan/HasElectric/HasDoors/HasSuperstructure-
--               Spalten muessen noch vorhanden sein (kopiert nach IsApplicable).
--               Falls Section F bereits gelaufen ist, INSERT mit IsApplicable=0.
-- Idempotent: UQ_PO_Key + NOT EXISTS-Guard.
-- =============================================
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders')
             AND name = N'HasCooling')
BEGIN
    DECLARE @batchSizeD INT = 5000;
    DECLARE @rowsD INT = 1;
    DECLARE @lastIdD INT = 0;

    WHILE @rowsD > 0
    BEGIN
        BEGIN TRANSACTION;
        ;WITH NextBatch AS (
            SELECT TOP (@batchSizeD)
                   p.Id,
                   p.HasCooling, p.HasFan, p.HasElectric, p.HasDoors, p.HasSuperstructure
            FROM dbo.ProductionOrders p
            WHERE p.Id > @lastIdD
              AND NOT EXISTS (SELECT 1 FROM dbo.ProductionOrderAssemblyGroups g WHERE g.ProductionOrderId = p.Id)
            ORDER BY p.Id
        )
        INSERT INTO dbo.ProductionOrderAssemblyGroups (
            ProductionOrderId, GroupKey, IsApplicable, IsCompleted,
            CreatedAt, CreatedBy, CreatedByWindows)
        SELECT n.Id, k.GroupKey,
               CASE k.GroupKey
                   WHEN 'VK' THEN n.HasCooling
                   WHEN 'VL' THEN n.HasFan
                   WHEN 'VE' THEN n.HasElectric
                   WHEN 'VT' THEN n.HasDoors
                   WHEN 'VA' THEN n.HasSuperstructure
                   ELSE 0
               END AS IsApplicable,
               0 AS IsCompleted,
               GETDATE(), 'Migration_60', SYSTEM_USER
        FROM NextBatch n
        CROSS JOIN (VALUES ('VK'),('VL'),('VE'),('VT'),('VA')) k(GroupKey);

        SET @rowsD = @@ROWCOUNT / 5;
        IF @rowsD > 0
            SET @lastIdD = (SELECT MAX(ProductionOrderId) FROM dbo.ProductionOrderAssemblyGroups);
        COMMIT TRANSACTION;
    END
    DECLARE @cntAgCopy INT = (SELECT COUNT(*) FROM dbo.ProductionOrderAssemblyGroups);
    PRINT 'AssemblyGroups-Migration abgeschlossen (mit Spalten-Kopie). Zeilen = ' + CAST(@cntAgCopy AS NVARCHAR);
END
ELSE
BEGIN
    -- Section F bereits gelaufen: alle 5 Gruppen mit IsApplicable=0 anlegen
    DECLARE @batchSizeD2 INT = 5000;
    DECLARE @rowsD2 INT = 1;
    DECLARE @lastIdD2 INT = 0;

    WHILE @rowsD2 > 0
    BEGIN
        BEGIN TRANSACTION;
        ;WITH NextBatch AS (
            SELECT TOP (@batchSizeD2) p.Id
            FROM dbo.ProductionOrders p
            WHERE p.Id > @lastIdD2
              AND NOT EXISTS (SELECT 1 FROM dbo.ProductionOrderAssemblyGroups g WHERE g.ProductionOrderId = p.Id)
            ORDER BY p.Id
        )
        INSERT INTO dbo.ProductionOrderAssemblyGroups (
            ProductionOrderId, GroupKey, IsApplicable, IsCompleted,
            CreatedAt, CreatedBy, CreatedByWindows)
        SELECT n.Id, k.GroupKey, 0, 0,
               GETDATE(), 'Migration_60', SYSTEM_USER
        FROM NextBatch n
        CROSS JOIN (VALUES ('VK'),('VL'),('VE'),('VT'),('VA')) k(GroupKey);

        SET @rowsD2 = @@ROWCOUNT / 5;
        IF @rowsD2 > 0
            SET @lastIdD2 = (SELECT MAX(ProductionOrderId) FROM dbo.ProductionOrderAssemblyGroups);
        COMMIT TRANSACTION;
    END
    DECLARE @cntAgEmpty INT = (SELECT COUNT(*) FROM dbo.ProductionOrderAssemblyGroups);
    PRINT 'AssemblyGroups-Migration abgeschlossen (IsApplicable=0, Section F war bereits gelaufen). Zeilen = ' + CAST(@cntAgEmpty AS NVARCHAR);
END
GO

-- =============================================
-- SECTION E: VERIFIKATIONS-COUNTS
-- =============================================
DECLARE @poCount INT = (SELECT COUNT(*) FROM dbo.ProductionOrders);
DECLARE @psCount INT = (SELECT COUNT(*) FROM dbo.ProductionOrderPickingStatus);
DECLARE @bdeCount INT = (SELECT COUNT(*) FROM dbo.ProductionOrderBdeStatus);
DECLARE @grpCount INT = (SELECT COUNT(*) FROM dbo.ProductionOrderAssemblyGroups);
PRINT 'Verifikation: ProductionOrders=' + CAST(@poCount AS NVARCHAR);
PRINT 'Verifikation: ProductionOrderPickingStatus=' + CAST(@psCount AS NVARCHAR) + ' (Erwartet=' + CAST(@poCount AS NVARCHAR) + ')';
PRINT 'Verifikation: ProductionOrderBdeStatus=' + CAST(@bdeCount AS NVARCHAR) + ' (Erwartet=' + CAST(@poCount AS NVARCHAR) + ')';
PRINT 'Verifikation: ProductionOrderAssemblyGroups=' + CAST(@grpCount AS NVARCHAR) + ' (Erwartet=' + CAST(5 * @poCount AS NVARCHAR) + ')';
IF @psCount <> @poCount
    PRINT 'WARNUNG: PickingStatus-Anzahl weicht ab.';
IF @bdeCount <> @poCount
    PRINT 'WARNUNG: BdeStatus-Anzahl weicht ab.';
IF @grpCount <> 5 * @poCount
    PRINT 'WARNUNG: AssemblyGroups-Anzahl weicht ab (sollte 5 * FA-Anzahl sein).';
GO

-- =============================================
-- SECTION F: SPALTEN DROPPEN
--   F.1 DEFAULT-Constraints loeschen (dynamisch via sys.default_constraints)
--   F.2 FK_ProductionOrders_AssignedPicker loeschen
--   F.3 Indexes loeschen
--   F.4 16 Spalten loeschen
-- Idempotent via sys.*-Checks.
-- =============================================

-- F.1 DEFAULT-Constraints loeschen (dynamisch, da Namen je nach Branch variieren koennen)
DECLARE @dropSql NVARCHAR(MAX) = N'';
SELECT @dropSql = @dropSql + N'ALTER TABLE [dbo].[ProductionOrders] DROP CONSTRAINT [' + dc.name + N'];' + CHAR(13)
FROM sys.default_constraints dc
INNER JOIN sys.columns c
        ON dc.parent_object_id = c.object_id
       AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ProductionOrders')
  AND c.name IN (
        N'PickingStatus', N'PickingPriority', N'IsReleasedForPicking',
        N'ReleasedAt', N'ReleasedBy', N'AssignedPickerId', N'AssignedPickerName',
        N'HasGlass', N'HasExternalPurchase', N'HasCoatingParts', N'IsCoatingDone',
        N'HasCooling', N'HasFan', N'HasElectric', N'HasDoors', N'HasSuperstructure'
  );

IF LEN(@dropSql) > 0
BEGIN
    PRINT 'Section F.1: DEFAULT-Constraints loeschen...';
    EXEC sp_executesql @dropSql;
END
ELSE
    PRINT 'Section F.1: Keine zu loeschenden DEFAULT-Constraints gefunden.';
GO

-- F.2 FK loeschen
IF EXISTS (SELECT 1 FROM sys.foreign_keys
           WHERE name = N'FK_ProductionOrders_AssignedPicker'
             AND parent_object_id = OBJECT_ID(N'dbo.ProductionOrders'))
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] DROP CONSTRAINT [FK_ProductionOrders_AssignedPicker];
    PRINT 'Section F.2: FK_ProductionOrders_AssignedPicker geloescht.';
END
ELSE
    PRINT 'Section F.2: FK_ProductionOrders_AssignedPicker nicht (mehr) vorhanden.';
GO

-- F.2b FK aus EF-Konvention (falls von EF mit anderem Namen angelegt)
IF EXISTS (SELECT 1 FROM sys.foreign_keys
           WHERE name = N'FK_ProductionOrders_Users_AssignedPickerId'
             AND parent_object_id = OBJECT_ID(N'dbo.ProductionOrders'))
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] DROP CONSTRAINT [FK_ProductionOrders_Users_AssignedPickerId];
    PRINT 'Section F.2b: FK_ProductionOrders_Users_AssignedPickerId geloescht.';
END
GO

-- F.3 Indexes loeschen
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = N'IX_ProductionOrders_IsReleasedForPicking_IsDone'
             AND object_id = OBJECT_ID(N'dbo.ProductionOrders'))
BEGIN
    DROP INDEX [IX_ProductionOrders_IsReleasedForPicking_IsDone] ON [dbo].[ProductionOrders];
    PRINT 'Section F.3: Index IX_ProductionOrders_IsReleasedForPicking_IsDone geloescht.';
END
ELSE
    PRINT 'Section F.3: Index IX_ProductionOrders_IsReleasedForPicking_IsDone nicht (mehr) vorhanden.';
GO

IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = N'IX_ProductionOrders_AssignedPickerId'
             AND object_id = OBJECT_ID(N'dbo.ProductionOrders'))
BEGIN
    DROP INDEX [IX_ProductionOrders_AssignedPickerId] ON [dbo].[ProductionOrders];
    PRINT 'Section F.3: Index IX_ProductionOrders_AssignedPickerId geloescht.';
END
ELSE
    PRINT 'Section F.3: Index IX_ProductionOrders_AssignedPickerId nicht (mehr) vorhanden.';
GO

-- F.4 16 Spalten loeschen (idempotent via sys.columns-Check)
DECLARE @cols TABLE (name NVARCHAR(128));
INSERT INTO @cols (name) VALUES
    (N'PickingStatus'), (N'PickingPriority'), (N'IsReleasedForPicking'),
    (N'ReleasedAt'), (N'ReleasedBy'), (N'AssignedPickerId'), (N'AssignedPickerName'),
    (N'HasGlass'), (N'HasExternalPurchase'), (N'HasCoatingParts'), (N'IsCoatingDone'),
    (N'HasCooling'), (N'HasFan'), (N'HasElectric'), (N'HasDoors'), (N'HasSuperstructure');

DECLARE @colName NVARCHAR(128);
DECLARE @colSql  NVARCHAR(MAX);
DECLARE col_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM @cols;
OPEN col_cursor;
FETCH NEXT FROM col_cursor INTO @colName;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID(N'dbo.ProductionOrders')
                 AND name = @colName)
    BEGIN
        SET @colSql = N'ALTER TABLE [dbo].[ProductionOrders] DROP COLUMN [' + @colName + N'];';
        EXEC sp_executesql @colSql;
        PRINT 'Section F.4: Spalte ' + @colName + ' geloescht.';
    END
    FETCH NEXT FROM col_cursor INTO @colName;
END
CLOSE col_cursor;
DEALLOCATE col_cursor;
GO

-- =============================================
-- SECTION G entfernt (Round 6, 2026-05-13)
--
-- Frueher: explizite INSERT INTO __EFMigrationsHistory, damit EF beim App-Start
-- die leere Up() ueberspringt. Round 5 hat Up() umgestellt auf File-Load — EF
-- ruft Up() ohnehin und inserted danach den History-Eintrag automatisch.
-- Section G wuerde jetzt mit dem EF-eigenen Insert in PK-Konflikt geraten
-- (PK-Violation auf MigrationId).
--
-- Manueller Cutover-Pfad bleibt funktional: DBA fuehrt SQL/60 manuell aus
-- (Schema+Daten+Drop), App-Start ruft Up() erneut, alle Idempotenz-Guards
-- ueberspringen die Wiederholungen, EF schreibt den History-Eintrag normal.
-- =============================================

PRINT '60_ProductionOrderSplit.sql erfolgreich abgeschlossen.';
GO
