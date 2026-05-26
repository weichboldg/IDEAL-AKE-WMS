-- AKE BDE Light - Views erstellen
USE [IDEAL_AKE_WMS]
GO

-- =============================================
-- View: Aktuelle Artikelbestände pro Lagerplatz
-- =============================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CurrentStock')
    DROP VIEW [dbo].[vw_CurrentStock]
GO

CREATE VIEW [dbo].[vw_CurrentStock]
AS
SELECT
    a.Id AS ArticleId,
    a.ArticleNumber,
    a.Description AS ArticleDescription,
    a.Unit,
    sl.Id AS StorageLocationId,
    sl.Code AS StorageLocationCode,
    sl.Description AS StorageLocationDescription,
    sl.Zone,
    SUM(
        CASE
            WHEN sm.MovementType = 0 THEN sm.Quantity   -- Einbuchung
            WHEN sm.MovementType = 1 THEN -sm.Quantity  -- Ausbuchung
            ELSE 0
        END
    ) AS CurrentQuantity
FROM [dbo].[StockMovements] sm
INNER JOIN [dbo].[Articles] a ON sm.ArticleId = a.Id
INNER JOIN [dbo].[StorageLocations] sl ON sm.StorageLocationId = sl.Id
GROUP BY
    a.Id, a.ArticleNumber, a.Description, a.Unit,
    sl.Id, sl.Code, sl.Description, sl.Zone
GO

-- =============================================
-- View: Bewegungshistorie mit allen Joins
-- =============================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_MovementHistory')
    DROP VIEW [dbo].[vw_MovementHistory]
GO

CREATE VIEW [dbo].[vw_MovementHistory]
AS
SELECT
    sm.Id,
    sm.Timestamp,
    a.ArticleNumber,
    a.Description AS ArticleDescription,
    sm.Quantity,
    sl.Code AS StorageLocationCode,
    sl.Description AS StorageLocationDescription,
    CASE sm.MovementType
        WHEN 0 THEN 'Einbuchung'
        WHEN 1 THEN 'Ausbuchung'
    END AS MovementTypeName,
    sm.MovementType,
    u.Name AS UserName,
    sm.WindowsUser,
    sm.ProductionOrder,
    sm.CreatedAt
FROM [dbo].[StockMovements] sm
INNER JOIN [dbo].[Articles] a ON sm.ArticleId = a.Id
INNER JOIN [dbo].[StorageLocations] sl ON sm.StorageLocationId = sl.Id
LEFT JOIN [dbo].[Users] u ON sm.UserId = u.Id
GO
