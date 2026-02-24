-- =============================================
-- SQL Server Agent Job: Artikel aus Sage importieren
-- Ziel:    [IDEAL_AKE_WMS].[dbo].[Articles]
-- Quelle:  [ake].[dbo].[KHKPpsRessourcenPositionen]
--          [ake].[dbo].[KHKArtikel]
--
-- Beschreibung:
--   Importiert neue Artikel (Bauteile/Ressourcen) aus Sage.
--   Bereits vorhandene Artikel (anhand ArticleNumber) werden uebersprungen.
--   Empfohlenes Intervall: taeglich oder stuendlich per SQL Server Agent.
--
-- Felder der Zieltabelle die NICHT befuellt werden (haben Defaults/NULL):
--   ReorderLevel     → NULL
-- =============================================

INSERT INTO [IDEAL_AKE_WMS].[dbo].[Articles]
    (ArticleNumber, Description, Unit, CreatedBy, CreatedAt, CreatedByWindows)

SELECT DISTINCT
    rp.Ressourcenummer  COLLATE Latin1_General_CI_AS,
    khk.Bezeichnung1,
    khk.Lagermengeneinheit,
    'Sage_Schnittstelle',
    GETDATE(),
    SYSTEM_USER

FROM [ake].[dbo].[KHKPpsRessourcenPositionen] rp
    INNER JOIN [ake].[dbo].[KHKArtikel] khk ON rp.Ressourcenummer = khk.Artikelnummer

WHERE NOT EXISTS (
    SELECT 1
    FROM [IDEAL_AKE_WMS].[dbo].[Articles] art
    WHERE art.ArticleNumber = rp.Ressourcenummer COLLATE Latin1_General_CI_AS
);
