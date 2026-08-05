-- SQL/83_AddSageBookingQueue.sql
-- Migration 83: AddSageBookingQueue (Sage-Lagerbuchungen, Step 1)
-- Rein additiv, idempotent: neue Queue-Tabelle SageBookingQueueItems mit FK auf
-- StockMovements und Index auf Status (Worker-Read-Pfad). Traegt den Status-Automat
-- Offen(0) -> Gesendet(1) -> Bestaetigt(2) / Fehler(3) und die volle Nachvollziehbarkeit.
-- Tabellen-DDL in eigenem Batch (SQL Server parst Batches vorab -> Index/FK-Referenz
-- auf eine im selben Batch neu erstellte Tabelle scheitert sonst).
-- __EFMigrationsHistory-Insert in separatem Batch.
SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.SageBookingQueueItems', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SageBookingQueueItems] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [StockMovementId]   INT               NOT NULL,
        [Status]            INT               NOT NULL,
        [AttemptCount]      INT               NOT NULL,
        [LastAttemptAt]     DATETIME2         NULL,
        [LastError]         NVARCHAR(2000)    NULL,
        [SageResponseRaw]   NVARCHAR(MAX)     NULL,
        [SentAt]            DATETIME2         NULL,
        [ConfirmedAt]       DATETIME2         NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_SageBookingQueueItems] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_SageBookingQueueItems_StockMovements_StockMovementId]
            FOREIGN KEY ([StockMovementId]) REFERENCES [dbo].[StockMovements]([Id])
    );
    PRINT 'Tabelle SageBookingQueueItems erstellt.';
END
GO

IF OBJECT_ID('dbo.SageBookingQueueItems', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SageBookingQueueItems_Status')
    CREATE NONCLUSTERED INDEX [IX_SageBookingQueueItems_Status]
        ON [dbo].[SageBookingQueueItems]([Status]);
GO

-- UNIQUE: genau ein Queue-Eintrag je StockMovement (Doppel-Enqueue-/Doppelbuchungs-Schutz).
IF OBJECT_ID('dbo.SageBookingQueueItems', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SageBookingQueueItems_StockMovementId')
    CREATE UNIQUE NONCLUSTERED INDEX [IX_SageBookingQueueItems_StockMovementId]
        ON [dbo].[SageBookingQueueItems]([StockMovementId]);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '20260803112322_AddSageBookingQueue')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260803112322_AddSageBookingQueue', '10.0.2');
END
GO
