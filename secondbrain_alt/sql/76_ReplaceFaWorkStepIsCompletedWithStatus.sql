-- =============================================
-- SQL/76 — FA-Vorbau v1.24.0
-- FaWorkSteps.IsCompleted (bit) -> Status (int): 0=Offen, 1=InBearbeitung, 2=Fertig.
-- Daten-Konvertierung: IsCompleted=1 -> Fertig(2), sonst Offen(0). Idempotent.
-- =============================================

IF COL_LENGTH('dbo.FaWorkSteps', 'Status') IS NULL
BEGIN
    ALTER TABLE [dbo].[FaWorkSteps]
        ADD [Status] INT NOT NULL CONSTRAINT DF_FaWorkSteps_Status DEFAULT 0;
    PRINT 'Spalte FaWorkSteps.Status hinzugefuegt.';
END
GO

IF COL_LENGTH('dbo.FaWorkSteps', 'IsCompleted') IS NOT NULL
BEGIN
    UPDATE [dbo].[FaWorkSteps]
        SET [Status] = CASE WHEN [IsCompleted] = 1 THEN 2 ELSE 0 END;
    PRINT 'FaWorkSteps.Status aus IsCompleted konvertiert.';

    DECLARE @c NVARCHAR(200) = (
        SELECT dc.name FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id
            AND dc.parent_column_id = c.column_id
        WHERE c.object_id = OBJECT_ID('[dbo].[FaWorkSteps]') AND c.name = 'IsCompleted');
    IF @c IS NOT NULL EXEC('ALTER TABLE [dbo].[FaWorkSteps] DROP CONSTRAINT [' + @c + ']');

    ALTER TABLE [dbo].[FaWorkSteps] DROP COLUMN [IsCompleted];
    PRINT 'Spalte FaWorkSteps.IsCompleted entfernt.';
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260630104647_ReplaceFaWorkStepIsCompletedWithStatus')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260630104647_ReplaceFaWorkStepIsCompletedWithStatus', '10.0.2');
GO
