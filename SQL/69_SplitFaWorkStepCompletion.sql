-- =============================================
-- 69_SplitFaWorkStepCompletion.sql (v1.22.0)
-- Trennt den FaWorkStep-Completion-Status in zwei Felder:
--   IsSpecComplete ("vollstaendig definiert", FA-Vervollstaendigung, blendet NICHT aus)
--   IsCompleted    ("Arbeit erledigt", FA-Abarbeitungsliste, blendet aus)
-- Idempotent, kann mehrfach ausgefuehrt werden (Reapply-fest).
--
-- Entspricht der EF-Migration 20260615070236_SplitFaWorkStepCompletion.
-- =============================================

-- =============================================
-- SECTION A: SPALTEN ANLEGEN (idempotent, jeweils eigener Guard)
-- =============================================
IF COL_LENGTH('dbo.FaWorkSteps', 'IsSpecComplete') IS NULL
BEGIN
    ALTER TABLE [dbo].[FaWorkSteps]
        ADD [IsSpecComplete] BIT NOT NULL
            CONSTRAINT DF_FaWorkSteps_IsSpecComplete DEFAULT 0;
    PRINT 'Spalte FaWorkSteps.IsSpecComplete erstellt.';
END
ELSE
    PRINT 'Spalte FaWorkSteps.IsSpecComplete bereits vorhanden - uebersprungen.';
GO

IF COL_LENGTH('dbo.FaWorkSteps', 'SpecCompletedAt') IS NULL
BEGIN
    ALTER TABLE [dbo].[FaWorkSteps]
        ADD [SpecCompletedAt] DATETIME2 NULL;
    PRINT 'Spalte FaWorkSteps.SpecCompletedAt erstellt.';
END
ELSE
    PRINT 'Spalte FaWorkSteps.SpecCompletedAt bereits vorhanden - uebersprungen.';
GO

IF COL_LENGTH('dbo.FaWorkSteps', 'SpecCompletedBy') IS NULL
BEGIN
    ALTER TABLE [dbo].[FaWorkSteps]
        ADD [SpecCompletedBy] NVARCHAR(200) NULL;
    PRINT 'Spalte FaWorkSteps.SpecCompletedBy erstellt.';
END
ELSE
    PRINT 'Spalte FaWorkSteps.SpecCompletedBy bereits vorhanden - uebersprungen.';
GO

-- =============================================
-- SECTION B: DATEN-VERSCHIEBUNG
-- Das alte IsCompleted war semantisch "Spec fertig" (v1.13-/Konvertierungs-Herkunft).
-- -> nach IsSpecComplete uebernehmen, Arbeit-erledigt frisch starten.
-- =============================================
UPDATE dbo.FaWorkSteps
SET IsSpecComplete = IsCompleted,
    SpecCompletedAt = CompletedAt,
    SpecCompletedBy = CompletedBy,
    IsCompleted = 0,
    CompletedAt = NULL,
    CompletedBy = NULL;
PRINT 'Daten-Verschiebung IsCompleted -> IsSpecComplete abgeschlossen.';
GO

-- =============================================
-- SECTION C: EF MIGRATIONS HISTORY
-- =============================================
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260615070236_SplitFaWorkStepCompletion')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260615070236_SplitFaWorkStepCompletion', '10.0.2');
GO

PRINT '69_SplitFaWorkStepCompletion abgeschlossen.';
GO
