-- =============================================================================
-- 145_AddPurchaseOrderReminders.sql  (v1.99.10, Spec 2026-11-12-offene-bestellungen-mahnmail-lieferverzug)
-- Lieferverzug-Mails: Protokolltabelle je versendeter Mail und Position (heute schon gesendet, Wiederholung, Nachweis) und zwei WMS-Felder
-- am Kreditor (Freigabe-Antwort 5):
--   dbo.PurchaseOrderReminders (neu, ohne FK, ohne Audit-Felder - nur der Dienst schreibt, nie geaendert)
--   Creditors.ReminderDisabled  BIT NOT NULL DEFAULT 0   (keine Erinnerungsmails)
--   Creditors.ReminderEmail     NVARCHAR(200) NULL       (Adresse fuer Erinnerungen; leer = Creditors.Email)
-- Rein additiv, nicht daten-destruktiv. SQL/AgentJobs nicht betroffen (der Kontokorrent-Abgleich schreibt die neuen Felder nicht).
-- Objektnamen wie die EF-Migration 20261112143552_AddPurchaseOrderReminders. Idempotent (OBJECT_ID-/COL_LENGTH-Guard), History separat.
-- =============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[dbo].[Creditors]', N'U') IS NULL
BEGIN
    PRINT N'WARNUNG: dbo.Creditors fehlt - zuerst SQL/104 ausfuehren. Keine Aenderung.';
    SET NOEXEC ON;
END
GO

IF OBJECT_ID(N'[dbo].[PurchaseOrderReminders]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PurchaseOrderReminders] (
        [Id]            INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_PurchaseOrderReminders] PRIMARY KEY,
        [Art]           INT               NOT NULL,  -- 0 = Einkauf-Sammelliste, 1 = Kreditor-Mail
        [BelPosId]      INT               NOT NULL,
        [Belegnummer]   NVARCHAR(20)      NOT NULL,
        [LieferantKto]  NVARCHAR(20)      NULL,
        [CreditorId]    INT               NULL,
        [Liefertermin]  DATE              NULL,
        [Rest]          DECIMAL(18,4)     NOT NULL,
        [Empfaenger]    NVARCHAR(1000)    NOT NULL,
        [Testbetrieb]   BIT               NOT NULL,
        [SentAt]        DATETIME2         NOT NULL
    );
    CREATE INDEX [IX_PurchaseOrderReminders_BelPosId_Art_SentAt] ON [dbo].[PurchaseOrderReminders]([BelPosId], [Art], [SentAt]);
    CREATE INDEX [IX_PurchaseOrderReminders_SentAt] ON [dbo].[PurchaseOrderReminders]([SentAt]);
    PRINT N'Tabelle PurchaseOrderReminders angelegt.';
END
GO

IF COL_LENGTH(N'dbo.Creditors', N'ReminderDisabled') IS NULL
BEGIN
    ALTER TABLE [dbo].[Creditors] ADD [ReminderDisabled] BIT NOT NULL CONSTRAINT [DF_Creditors_ReminderDisabled] DEFAULT (0);
    PRINT N'Spalte Creditors.ReminderDisabled angelegt.';
END
GO

IF COL_LENGTH(N'dbo.Creditors', N'ReminderEmail') IS NULL
BEGIN
    ALTER TABLE [dbo].[Creditors] ADD [ReminderEmail] NVARCHAR(200) NULL;
    PRINT N'Spalte Creditors.ReminderEmail angelegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261112143552_AddPurchaseOrderReminders')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261112143552_AddPurchaseOrderReminders', '10.0.2');
    PRINT 'Migration 20261112143552_AddPurchaseOrderReminders eingetragen.';
END
GO

SET NOEXEC OFF;
GO
