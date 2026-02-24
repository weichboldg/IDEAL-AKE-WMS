-- =============================================
-- SQL Server Agent Job: Produktionsauftraege aus Sage importieren
-- Ziel:    [IDEAL_AKE_WMS].[dbo].[ProductionOrders]
-- Quelle:  [ake].[dbo].[vw_AKE_Kommissionierung_WAListe]
--
-- Beschreibung:
--   Importiert neue Werkstaettigungsauftraege aus der Sage-View.
--   Bereits vorhandene Auftraege (anhand OrderNumber) werden uebersprungen.
--   Empfohlenes Intervall: taeglich oder stuendlich per SQL Server Agent.
--
-- Felder der Zieltabelle die NICHT befuellt werden (haben Defaults):
--   PickingStatus    → NULL
--   HasGlass         → 0 (DEFAULT)
--   HasExternalPurchase → 0 (DEFAULT)
-- =============================================

INSERT INTO [IDEAL_AKE_WMS].[dbo].[ProductionOrders]
    (OrderNumber, Quantity, Customer, ArticleNumber, Description1, Description2,
     ProductionDate, DeliveryDate, IsDone, CreatedBy, CreatedAt, CreatedByWindows)

SELECT DISTINCT
    v.[WA Nummer],
    v.[Stückzahl],
    v.[Kunde]           COLLATE Latin1_General_CI_AS,
    v.[Artikelnummer]   COLLATE Latin1_General_CI_AS,
    v.[Bezeichnung1]    COLLATE Latin1_General_CI_AS,
    v.[Bezeichnung2]    COLLATE Latin1_General_CI_AS,
    v.[Fertigungstermin],
    v.[Liefertermin],
    0,
    'Sage_Schnittstelle',
    GETDATE(),
    SYSTEM_USER

FROM [ake].[dbo].[vw_AKE_Kommissionierung_WAListe] v

WHERE NOT EXISTS (
    SELECT 1
    FROM [IDEAL_AKE_WMS].[dbo].[ProductionOrders] p
    WHERE p.OrderNumber = v.[WA Nummer]
);
