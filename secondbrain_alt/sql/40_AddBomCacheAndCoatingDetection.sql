-- SQL/40_AddBomCacheAndCoatingDetection.sql
-- Adds CachedBomHeaders + CachedBomItems tables and HasCoatingParts / IsCoatingDone on ProductionOrders.

SET NOCOUNT ON;
GO

-- ==========================================================================
-- CachedBomHeaders
-- ==========================================================================
IF OBJECT_ID('dbo.CachedBomHeaders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CachedBomHeaders
    (
        Id             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CachedBomHeaders PRIMARY KEY,
        Artikelnummer  NVARCHAR(100)     NOT NULL,
        Source         NVARCHAR(20)      NOT NULL,
        ItemCount      INT               NOT NULL,
        ContentHash    NVARCHAR(64)      NOT NULL,
        CachedAt       DATETIME2         NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CachedBomHeaders_Artikelnummer' AND object_id = OBJECT_ID('dbo.CachedBomHeaders'))
BEGIN
    CREATE UNIQUE INDEX IX_CachedBomHeaders_Artikelnummer
        ON dbo.CachedBomHeaders (Artikelnummer);
END
GO

-- ==========================================================================
-- CachedBomItems
-- ==========================================================================
IF OBJECT_ID('dbo.CachedBomItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CachedBomItems
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CachedBomItems PRIMARY KEY,
        CachedBomHeaderId   INT               NOT NULL,
        Position            NVARCHAR(50)      NULL,
        Baugruppe           NVARCHAR(200)     NULL,
        Ressourcenummer     NVARCHAR(100)     NULL,
        Bezeichnung1        NVARCHAR(500)     NULL,
        Bezeichnung2        NVARCHAR(500)     NULL,
        Menge               DECIMAL(18,3)     NOT NULL,
        Beschaffungsartikel NVARCHAR(100)     NULL,
        Artikelgruppe       NVARCHAR(100)     NULL,
        SortOrder           INT               NOT NULL,
        CONSTRAINT FK_CachedBomItems_CachedBomHeaders
            FOREIGN KEY (CachedBomHeaderId)
            REFERENCES dbo.CachedBomHeaders (Id)
            ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CachedBomItems_CachedBomHeaderId' AND object_id = OBJECT_ID('dbo.CachedBomItems'))
BEGIN
    CREATE INDEX IX_CachedBomItems_CachedBomHeaderId
        ON dbo.CachedBomItems (CachedBomHeaderId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CachedBomItems_Ressourcenummer' AND object_id = OBJECT_ID('dbo.CachedBomItems'))
BEGIN
    CREATE INDEX IX_CachedBomItems_Ressourcenummer
        ON dbo.CachedBomItems (Ressourcenummer);
END
GO

-- ==========================================================================
-- ProductionOrders: HasCoatingParts + IsCoatingDone
-- ==========================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'HasCoatingParts' AND Object_ID = OBJECT_ID(N'dbo.ProductionOrders'))
BEGIN
    ALTER TABLE dbo.ProductionOrders ADD HasCoatingParts BIT NOT NULL CONSTRAINT DF_ProductionOrders_HasCoatingParts DEFAULT (0);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'IsCoatingDone' AND Object_ID = OBJECT_ID(N'dbo.ProductionOrders'))
BEGIN
    ALTER TABLE dbo.ProductionOrders ADD IsCoatingDone BIT NOT NULL CONSTRAINT DF_ProductionOrders_IsCoatingDone DEFAULT (0);
END
GO

-- ==========================================================================
-- __EFMigrationsHistory
-- ==========================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260407091723_AddBomCacheAndCoatingDetection')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260407091723_AddBomCacheAndCoatingDetection', N'10.0.0');
END
GO
