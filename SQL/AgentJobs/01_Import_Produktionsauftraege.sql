-- =============================================
-- SQL Server Agent Job: Produktionsauftraege aus Sage importieren
-- Ziel:    [IDEAL_AKE_WMS].[dbo].[ProductionOrders]
-- Quelle:  [ake].[dbo].[vw_AKE_Kommissionierung_WAListe]
--
-- Beschreibung:
--   Importiert neue Werkstaettigungsauftraege aus der Sage-View und aktualisiert
--   bestehende Auftraege wenn sich Sage-Daten geaendert haben (Fertigungstermin,
--   Liefertermin, Stueckzahl, Bezeichnung etc.).
--   Empfohlenes Intervall: taeglich oder stuendlich per SQL Server Agent.
--
-- Felder die aktualisiert werden (kommen aus Sage):
--   Quantity, Customer, ArticleNumber, Description1, Description2,
--   ProductionDate, DeliveryDate
--   ModifiedAt, ModifiedBy, ModifiedByWindows
--
-- Felder die NICHT ueberschrieben werden (App-verwaltet):
--   IsDone, PickingStatus, HasGlass, HasExternalPurchase,
--   HasCooling, HasFan, HasElectric, HasDoors, HasSuperstructure,
--   HasCoatingParts, IsCoatingDone,
--   CreatedAt, CreatedBy, CreatedByWindows
--
-- Felder der Zieltabelle die bei INSERT nicht befuellt werden (haben Defaults):
--   PickingStatus    → NULL
--   HasGlass         → 0 (DEFAULT)
--   HasExternalPurchase → 0 (DEFAULT)
--   HasCooling/HasFan/HasElectric/HasDoors/HasSuperstructure → 0 (DEFAULT)
-- =============================================

MERGE [IDEAL_AKE_WMS].[dbo].[ProductionOrders] AS p
USING (
    SELECT DISTINCT
        v.[WA Nummer]        AS OrderNumber,
        v.[Stückzahl]        AS Quantity,
        v.[Kunde]            COLLATE Latin1_General_CI_AS AS Customer,
        v.[Artikelnummer]    COLLATE Latin1_General_CI_AS AS ArticleNumber,
        v.[Bezeichnung1]     COLLATE Latin1_General_CI_AS AS Description1,
        v.[Bezeichnung2]     COLLATE Latin1_General_CI_AS AS Description2,
        v.[Fertigungstermin] AS ProductionDate,
        v.[Liefertermin]     AS DeliveryDate
    FROM [ake].[dbo].[vw_AKE_Kommissionierung_WAListe] v
) AS src ON p.OrderNumber = src.OrderNumber

-- Bestehenden Auftrag aktualisieren, aber nur wenn sich tatsaechlich etwas geaendert hat
WHEN MATCHED AND (
    p.Quantity <> src.Quantity OR
    ISNULL(p.Customer,         '') <> ISNULL(src.Customer,     '') OR
    ISNULL(p.ArticleNumber,    '') <> ISNULL(src.ArticleNumber, '') OR
    ISNULL(p.Description1,     '') <> ISNULL(src.Description1,  '') OR
    ISNULL(p.Description2,     '') <> ISNULL(src.Description2,  '') OR
    ISNULL(p.ProductionDate, '19000101') <> ISNULL(src.ProductionDate, '19000101') OR
    ISNULL(p.DeliveryDate,   '19000101') <> ISNULL(src.DeliveryDate,   '19000101')
) THEN
    UPDATE SET
        p.Quantity          = src.Quantity,
        p.Customer          = src.Customer,
        p.ArticleNumber     = src.ArticleNumber,
        p.Description1      = src.Description1,
        p.Description2      = src.Description2,
        p.ProductionDate    = src.ProductionDate,
        p.DeliveryDate      = src.DeliveryDate,
        p.ModifiedAt        = GETDATE(),
        p.ModifiedBy        = 'Sage_Schnittstelle',
        p.ModifiedByWindows = SYSTEM_USER

-- Neuen Auftrag einfuegen
WHEN NOT MATCHED BY TARGET THEN
    INSERT (OrderNumber, Quantity, Customer, ArticleNumber, Description1, Description2,
            ProductionDate, DeliveryDate, IsDone, CreatedBy, CreatedAt, CreatedByWindows)
    VALUES (src.OrderNumber, src.Quantity, src.Customer, src.ArticleNumber,
            src.Description1, src.Description2,
            src.ProductionDate, src.DeliveryDate,
            0, 'Sage_Schnittstelle', GETDATE(), SYSTEM_USER);
