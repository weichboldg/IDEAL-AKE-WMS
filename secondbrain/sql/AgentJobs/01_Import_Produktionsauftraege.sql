-- =============================================
-- SQL Server Agent Job: Produktionsauftraege aus Sage importieren
-- Ziel:    [IDEAL_AKE_WMS].[dbo].[ProductionOrders] (+ 3 Status-Tabellen, siehe unten)
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
--   IsDone, ProductionWorkplaceId, CreatedAt, CreatedBy, CreatedByWindows
--
-- Schema-Hinweis (ab v1.11.0 / Migration AddProductionOrderSplit):
--   Die 16 fruehere Status-/Flag-Spalten existieren in [ProductionOrders] nicht mehr.
--   Sie wurden auf 3 dedizierte Tabellen aufgeteilt:
--
--     Frueheres Feld auf ProductionOrders        ->  Neue Heimat
--     -----------------------------------------------------------------------------
--     PickingStatus                              ->  ProductionOrderPickingStatus.PickingStatus
--     HasGlass                                   ->  ProductionOrderPickingStatus.HasGlass
--     HasExternalPurchase                        ->  ProductionOrderPickingStatus.HasExternalPurchase
--     HasCoatingParts                            ->  ProductionOrderPickingStatus.HasCoatingParts
--     IsCoatingDone                              ->  ProductionOrderPickingStatus.IsCoatingDone
--     IsDonePicking                              ->  ProductionOrderPickingStatus.IsDonePicking
--     IsReleasedForPicking                       ->  ProductionOrderPickingStatus.IsReleasedForPicking
--     PickingPriority                            ->  ProductionOrderPickingStatus.PickingPriority
--     ReleasedAt / ReleasedBy                    ->  ProductionOrderPickingStatus.ReleasedAt / ReleasedBy
--     AssignedPickerId / AssignedPickerName      ->  ProductionOrderPickingStatus.AssignedPickerId / AssignedPickerName
--     IsDoneBde                                  ->  ProductionOrderBdeStatus.IsDoneBde
--     HasCooling                                 ->  ProductionOrderAssemblyGroups (GroupKey='VK')
--     HasFan                                     ->  ProductionOrderAssemblyGroups (GroupKey='VL')
--     HasElectric                                ->  ProductionOrderAssemblyGroups (GroupKey='VE')
--     HasDoors                                   ->  ProductionOrderAssemblyGroups (GroupKey='VT')
--     HasSuperstructure                          ->  ProductionOrderAssemblyGroups (GroupKey='VA')
--
--   Die Migration (SQL/60_ProductionOrderSplit.sql) hat fuer alle bestehenden
--   FAs bereits Status-Zeilen erzeugt. Dieser AgentJob legt ab jetzt fuer
--   JEDEN neu importierten FA die fehlenden Zeilen via Folge-MERGE eager an:
--
--     ProductionOrderPickingStatus    (1 Zeile/FA, alle Bool=0)
--     ProductionOrderBdeStatus        (1 Zeile/FA, IsDoneBde=0)
--     ProductionOrderAssemblyGroups   (5 Zeilen/FA: VK/VL/VE/VT/VA, IsApplicable=0)
--
--   Alle Folge-MERGEs sind idempotent (WHEN NOT MATCHED BY TARGET).
--   Es gibt KEIN WHEN MATCHED — vom Anwender gesetzte Status-Werte
--   (z.B. IsReleasedForPicking=1) werden NIE vom AgentJob ueberschrieben.
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

-- =============================================
-- Folge-MERGE 1: ProductionOrderPickingStatus eager-create
-- Legt fuer JEDEN ProductionOrder genau eine Status-Zeile an, falls noch nicht vorhanden.
-- Idempotent durch NOT MATCHED BY TARGET (Status wird nie ueberschrieben).
-- =============================================
MERGE [IDEAL_AKE_WMS].[dbo].[ProductionOrderPickingStatus] AS s
USING (SELECT Id FROM [IDEAL_AKE_WMS].[dbo].[ProductionOrders]) AS src
    ON s.ProductionOrderId = src.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ProductionOrderId, IsReleasedForPicking,
            HasGlass, HasExternalPurchase, HasCoatingParts, IsCoatingDone, IsDonePicking,
            CreatedAt, CreatedBy, CreatedByWindows)
    VALUES (src.Id, 0, 0, 0, 0, 0, 0,
            GETDATE(), 'Sage_Schnittstelle', SYSTEM_USER);

-- =============================================
-- Folge-MERGE 2: ProductionOrderBdeStatus eager-create
-- 1 Zeile pro FA, IsDoneBde=0. Wird nie ueberschrieben.
-- =============================================
MERGE [IDEAL_AKE_WMS].[dbo].[ProductionOrderBdeStatus] AS s
USING (SELECT Id FROM [IDEAL_AKE_WMS].[dbo].[ProductionOrders]) AS src
    ON s.ProductionOrderId = src.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ProductionOrderId, IsDoneBde, CreatedAt, CreatedBy, CreatedByWindows)
    VALUES (src.Id, 0, GETDATE(), 'Sage_Schnittstelle', SYSTEM_USER);

-- =============================================
-- Folge-MERGE 3: ProductionOrderAssemblyGroups eager-create (5/FA)
-- CROSS JOIN auf VALUES erzeugt fuer jeden FA x jedem GroupKey eine Source-Zeile.
-- Idempotent: UQ_PO_Key-Index verhindert Duplikate, NOT MATCHED triggert nur fuer Luecken.
-- =============================================
MERGE [IDEAL_AKE_WMS].[dbo].[ProductionOrderAssemblyGroups] AS s
USING (
    SELECT p.Id AS ProductionOrderId, k.GroupKey
    FROM [IDEAL_AKE_WMS].[dbo].[ProductionOrders] p
    CROSS JOIN (VALUES ('VK'),('VL'),('VE'),('VT'),('VA')) k(GroupKey)
) AS src
    ON s.ProductionOrderId = src.ProductionOrderId AND s.GroupKey = src.GroupKey
WHEN NOT MATCHED BY TARGET THEN
    INSERT (ProductionOrderId, GroupKey, IsApplicable, IsCompleted,
            CreatedAt, CreatedBy, CreatedByWindows)
    VALUES (src.ProductionOrderId, src.GroupKey, 0, 0,
            GETDATE(), 'Sage_Schnittstelle', SYSTEM_USER);
