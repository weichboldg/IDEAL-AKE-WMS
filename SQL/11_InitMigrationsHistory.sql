USE [AKE_BDE_Light]
GO

-- =============================================
-- 11_InitMigrationsHistory.sql
-- EF Core Migrations: Baseline initialisieren
-- Muss auf bestehender Produktion ausgeführt werden,
-- BEVOR die App mit db.Database.Migrate() startet.
-- =============================================

-- Schritt 1: Tabelle erstellen falls nicht vorhanden
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] NVARCHAR(150) NOT NULL,
        [ProductVersion] NVARCHAR(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
    PRINT '__EFMigrationsHistory Tabelle erstellt.';
END
ELSE
BEGIN
    PRINT '__EFMigrationsHistory existiert bereits.';
END
GO

-- Schritt 2: InitialCreate Migration eintragen (eigener Batch nach GO)
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260217074157_InitialCreate')
    BEGIN
        INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
        VALUES (N'20260217074157_InitialCreate', N'10.0.2');
        PRINT 'InitialCreate Migration als angewendet markiert.';
    END
    ELSE
    BEGIN
        PRINT 'InitialCreate Migration war bereits eingetragen.';
    END
END
ELSE
BEGIN
    PRINT 'FEHLER: __EFMigrationsHistory Tabelle konnte nicht erstellt werden!';
END
GO
