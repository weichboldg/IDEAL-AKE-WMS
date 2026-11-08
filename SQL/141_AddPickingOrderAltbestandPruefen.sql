-- =============================================================================
-- 141_AddPickingOrderAltbestandPruefen.sql  (v1.89.0, Spec 2026-11-08-kommissionierung-auftraege-kopf-zeilen, Entscheidung Q11)
-- Kennzeichen am Kommissionierauftrag: Altbestand-Auftrag eines offenen FA mit Auftragsmenge <> 1 – „Altbestand pruefen“.
-- Das Einmal-Skript SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql setzt es; solange ein Auftrag des FA es
-- traegt, legt die Kommissionierung fuer den FA keine Auftraege an. Der Leitstand bestaetigt „Altbestand geprueft“ und loescht es.
-- Nummer 141 = erste freie nach SQL/140. Rein additiv (NOT NULL DEFAULT 0). SQL/AgentJobs nicht betroffen.
-- Objektnamen wie die EF-Migration 20261108071811_AddPickingOrderAltbestandPruefen. Idempotent (COL_LENGTH-Guard), History separat.
-- REIHENFOLGE beim Deploy: nach SQL/133, VOR dem Einmal-Skript.
-- =============================================================================
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'[dbo].[PickingOrders]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.PickingOrders fehlt - zuerst SQL/133 ausfuehren. Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF COL_LENGTH(N'dbo.PickingOrders', N'AltbestandPruefen') IS NULL
BEGIN
    ALTER TABLE [dbo].[PickingOrders] ADD [AltbestandPruefen] bit NOT NULL CONSTRAINT [DF_PickingOrders_AltbestandPruefen] DEFAULT CAST(0 AS bit);
    PRINT N'Spalte PickingOrders.AltbestandPruefen angelegt.';
END
ELSE
    PRINT N'Spalte PickingOrders.AltbestandPruefen existiert bereits.';
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261108071811_AddPickingOrderAltbestandPruefen')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261108071811_AddPickingOrderAltbestandPruefen', '10.0.2');
    PRINT 'Migration 20261108071811_AddPickingOrderAltbestandPruefen eingetragen.';
END
GO

SET NOEXEC OFF;
GO
