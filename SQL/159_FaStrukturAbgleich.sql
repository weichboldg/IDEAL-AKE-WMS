-- =============================================================================
-- 159_FaStrukturAbgleich.sql  (v1.99.37, Spec 2026-11-17-fa-struktur-abgleich-statt-full-replace, ADR 0032)
-- FA-Struktur als Abgleich mit Lebenslauf statt Full-Replace:
--   dbo.FaHierarchyNodes, dbo.FaHierarchyOrderInfos, dbo.ProductionOrderPositions je
--     ErstGesehen datetime2 NOT NULL, ZuletztGesehen datetime2 NOT NULL (Startwert = SyncedAt),
--     VerschwundenSeit datetime2 NULL, VerschwundenGrund nvarchar(20) NULL
--   UX_FaHierarchyNodes_Schluessel      (HauptFA, VaterFA, SubFA, Position, Artnr)  UNIQUE, ungefiltert
--   UX_FaHierarchyOrderInfos_Schluessel (HauptFA, ABNr, Pos)                         UNIQUE, ungefiltert
--   (ProductionOrderPositions: UQ_ProductionOrderPositions_BelPosID besteht.)
-- Additiv, nicht daten-destruktiv: leere Artnr/ABNr/Pos werden NULL (wie der Abgleich speichert). Bei doppelten Schluesseln bricht das
-- Skript VOR dem Index mit lesbarer Meldung ab (Pruefung/FaStrukturSchluesselDoppelt) - keine halbe Aenderung am Index.
-- REIHENFOLGE: Dienst stoppen -> dieses Skript -> Web -> Dienst starten (alter Dienst-Code kennt die NOT-NULL-Spalten nicht).
-- Idempotent (COL_LENGTH-/sys.indexes-Guard), DDL je Schritt im eigenen Batch, History separat. SQL/AgentJobs nicht betroffen.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.FaHierarchyNodes', N'U') IS NULL OR OBJECT_ID(N'dbo.FaHierarchyOrderInfos', N'U') IS NULL
   OR OBJECT_ID(N'dbo.ProductionOrderPositions', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: FaHierarchyNodes/FaHierarchyOrderInfos/ProductionOrderPositions fehlen - SQL/89 bzw. SQL/101 zuerst. Keine Aenderung.';
    SET NOEXEC ON;
END
GO

-- 1) Spalten (je Tabelle, nullable anlegen)
IF COL_LENGTH(N'dbo.FaHierarchyNodes', N'ErstGesehen') IS NULL
    ALTER TABLE dbo.FaHierarchyNodes ADD ErstGesehen datetime2 NULL, ZuletztGesehen datetime2 NULL,
        VerschwundenSeit datetime2 NULL, VerschwundenGrund nvarchar(20) NULL;
GO
IF COL_LENGTH(N'dbo.FaHierarchyOrderInfos', N'ErstGesehen') IS NULL
    ALTER TABLE dbo.FaHierarchyOrderInfos ADD ErstGesehen datetime2 NULL, ZuletztGesehen datetime2 NULL,
        VerschwundenSeit datetime2 NULL, VerschwundenGrund nvarchar(20) NULL;
GO
IF COL_LENGTH(N'dbo.ProductionOrderPositions', N'ErstGesehen') IS NULL
    ALTER TABLE dbo.ProductionOrderPositions ADD ErstGesehen datetime2 NULL, ZuletztGesehen datetime2 NULL,
        VerschwundenSeit datetime2 NULL, VerschwundenGrund nvarchar(20) NULL;
GO

-- 2) Startwerte fuer Bestandszeilen (eigener Batch: die Spalten muessen beim Kompilieren existieren)
UPDATE dbo.FaHierarchyNodes SET ErstGesehen = SyncedAt, ZuletztGesehen = SyncedAt WHERE ErstGesehen IS NULL;
UPDATE dbo.FaHierarchyOrderInfos SET ErstGesehen = SyncedAt, ZuletztGesehen = SyncedAt WHERE ErstGesehen IS NULL;
UPDATE dbo.ProductionOrderPositions SET ErstGesehen = SyncedAt, ZuletztGesehen = SyncedAt WHERE ErstGesehen IS NULL;
GO

-- 3) NOT NULL (kein Default-Constraint: der Dienst setzt die Werte immer)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.FaHierarchyNodes') AND name = N'ErstGesehen' AND is_nullable = 1)
    ALTER TABLE dbo.FaHierarchyNodes ALTER COLUMN ErstGesehen datetime2 NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.FaHierarchyNodes') AND name = N'ZuletztGesehen' AND is_nullable = 1)
    ALTER TABLE dbo.FaHierarchyNodes ALTER COLUMN ZuletztGesehen datetime2 NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.FaHierarchyOrderInfos') AND name = N'ErstGesehen' AND is_nullable = 1)
    ALTER TABLE dbo.FaHierarchyOrderInfos ALTER COLUMN ErstGesehen datetime2 NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.FaHierarchyOrderInfos') AND name = N'ZuletztGesehen' AND is_nullable = 1)
    ALTER TABLE dbo.FaHierarchyOrderInfos ALTER COLUMN ZuletztGesehen datetime2 NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrderPositions') AND name = N'ErstGesehen' AND is_nullable = 1)
    ALTER TABLE dbo.ProductionOrderPositions ALTER COLUMN ErstGesehen datetime2 NOT NULL;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProductionOrderPositions') AND name = N'ZuletztGesehen' AND is_nullable = 1)
    ALTER TABLE dbo.ProductionOrderPositions ALTER COLUMN ZuletztGesehen datetime2 NOT NULL;
GO

-- 4) Leer -> NULL (wie der Abgleich speichert; der Index vergleicht '' und NULL verschieden)
UPDATE dbo.FaHierarchyNodes SET Artnr = NULL WHERE LTRIM(RTRIM(Artnr)) = N'';
UPDATE dbo.FaHierarchyOrderInfos SET ABNr = NULL WHERE LTRIM(RTRIM(ABNr)) = N'';
UPDATE dbo.FaHierarchyOrderInfos SET Pos = NULL WHERE LTRIM(RTRIM(Pos)) = N'';
GO

-- 5) Dubletten-Check, dann UNIQUE-Index Knoten (P-AB1; Schluessellaenge 4*4 + 400 Byte < 1700)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_FaHierarchyNodes_Schluessel' AND object_id = OBJECT_ID(N'dbo.FaHierarchyNodes'))
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.FaHierarchyNodes GROUP BY HauptFA, VaterFA, SubFA, Position, Artnr HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR(N'Doppelte FA-Struktur-Schluessel in dbo.FaHierarchyNodes - Pruefung/FaStrukturSchluesselDoppelt (P-AB1). SQL/159 abgebrochen, Index nicht angelegt.', 16, 1);
        SET NOEXEC ON;
    END
    ELSE
        CREATE UNIQUE INDEX [UX_FaHierarchyNodes_Schluessel] ON [dbo].[FaHierarchyNodes]([HauptFA],[VaterFA],[SubFA],[Position],[Artnr]);
END
GO

-- 6) Dubletten-Check, dann UNIQUE-Index OrderInfos (P-AB2)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_FaHierarchyOrderInfos_Schluessel' AND object_id = OBJECT_ID(N'dbo.FaHierarchyOrderInfos'))
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.FaHierarchyOrderInfos GROUP BY HauptFA, ABNr, Pos HAVING COUNT(*) > 1)
    BEGIN
        RAISERROR(N'Doppelte FA-Struktur-Schluessel in dbo.FaHierarchyOrderInfos (HauptFA, ABNr, Pos; P-AB2). SQL/159 abgebrochen, Index nicht angelegt.', 16, 1);
        SET NOEXEC ON;
    END
    ELSE
        CREATE UNIQUE INDEX [UX_FaHierarchyOrderInfos_Schluessel] ON [dbo].[FaHierarchyOrderInfos]([HauptFA],[ABNr],[Pos]);
END
GO

-- 7) History (separater Batch)
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261117183315_FaStrukturAbgleich')
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_FaHierarchyOrderInfos_Schluessel' AND object_id = OBJECT_ID(N'dbo.FaHierarchyOrderInfos'))
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20261117183315_FaStrukturAbgleich', '10.0.2');
    PRINT 'Migration 20261117183315_FaStrukturAbgleich eingetragen.';
END
GO

SET NOEXEC OFF;
GO
