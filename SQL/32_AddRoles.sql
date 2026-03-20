-- =============================================
-- 32: Rollen-System (Phase 1)
-- Erstellt Roles + UserRoles Tabellen, migriert bestehende Boolean-Flags
-- =============================================

USE [IDEAL_AKE_WMS]
GO

-- =============================================
-- 1. Roles Tabelle
-- =============================================
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [Key]               NVARCHAR(50)      NOT NULL,
        [Name]              NVARCHAR(100)     NOT NULL,
        [Description]       NVARCHAR(500)     NULL,
        [AdGroup]           NVARCHAR(200)     NULL,
        [IsSystem]          BIT               NOT NULL DEFAULT 0,
        [SortOrder]         INT               NOT NULL DEFAULT 0,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id])
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Roles_Key] ON [dbo].[Roles]([Key]);
    PRINT 'Tabelle Roles erstellt.';
END
GO

-- =============================================
-- 2. Standard-Rollen einfuegen
-- =============================================
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'admin')
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('admin', 'Administrator', 'Vollzugriff auf alle Funktionen', 1, 0, GETDATE(), 'system', 'system');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'masterdata')
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('masterdata', 'Stammdaten', 'Zugriff auf Stammdatenverwaltung (Benutzer, Arbeitsplaetze, Einstellungen)', 1, 1, GETDATE(), 'system', 'system');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'picking')
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('picking', 'Kommissionierung', 'Kommissionierung, Lagerbewegungen, Bestaende', 1, 2, GETDATE(), 'system', 'system');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'stock')
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('stock', 'Lager', 'Lagerbewegungen und Bestandsuebersicht', 1, 3, GETDATE(), 'system', 'system');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'stock_keyuser')
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('stock_keyuser', 'Lager Key-User', 'Erweiterte Lagerfunktionen (Korrekturbuchungen, Bestandsbereinigung)', 1, 4, GETDATE(), 'system', 'system');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'tracking')
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('tracking', 'Teileverfolgung', 'Teileverfolgung und OSEON-Auftraege anzeigen', 1, 5, GETDATE(), 'system', 'system');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Key] = 'reporting')
    INSERT INTO [dbo].[Roles] ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
    VALUES ('reporting', 'Rueckmeldung', 'Arbeitsgaenge rueckmelden', 1, 6, GETDATE(), 'system', 'system');

PRINT 'Standard-Rollen eingefuegt.';
GO

-- =============================================
-- 3. UserRoles Tabelle
-- =============================================
IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [Id]                INT IDENTITY(1,1) NOT NULL,
        [UserId]            INT               NOT NULL,
        [RoleId]            INT               NOT NULL,
        [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
        [CreatedBy]         NVARCHAR(200)     NOT NULL,
        [CreatedByWindows]  NVARCHAR(200)     NOT NULL,
        [ModifiedAt]        DATETIME2         NULL,
        [ModifiedBy]        NVARCHAR(200)     NULL,
        [ModifiedByWindows] NVARCHAR(200)     NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_UserRoles_UserId_RoleId] ON [dbo].[UserRoles]([UserId], [RoleId]);
    CREATE NONCLUSTERED INDEX [IX_UserRoles_RoleId] ON [dbo].[UserRoles]([RoleId]);
    PRINT 'Tabelle UserRoles erstellt.';
END
GO

-- =============================================
-- 4. Bestehende Boolean-Flags nach UserRoles migrieren
-- =============================================

-- IsAdmin -> admin Rolle
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [CreatedByWindows])
SELECT u.[Id], r.[Id], GETDATE(), 'migration', 'migration'
FROM [dbo].[Users] u
CROSS JOIN [dbo].[Roles] r
WHERE u.[IsAdmin] = 1
  AND r.[Key] = 'admin'
  AND NOT EXISTS (
    SELECT 1 FROM [dbo].[UserRoles] ur WHERE ur.[UserId] = u.[Id] AND ur.[RoleId] = r.[Id]
  );

-- HasMasterDataAccess -> masterdata Rolle
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [CreatedByWindows])
SELECT u.[Id], r.[Id], GETDATE(), 'migration', 'migration'
FROM [dbo].[Users] u
CROSS JOIN [dbo].[Roles] r
WHERE u.[HasMasterDataAccess] = 1
  AND r.[Key] = 'masterdata'
  AND NOT EXISTS (
    SELECT 1 FROM [dbo].[UserRoles] ur WHERE ur.[UserId] = u.[Id] AND ur.[RoleId] = r.[Id]
  );

-- CanPick -> picking Rolle
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [CreatedByWindows])
SELECT u.[Id], r.[Id], GETDATE(), 'migration', 'migration'
FROM [dbo].[Users] u
CROSS JOIN [dbo].[Roles] r
WHERE u.[CanPick] = 1
  AND r.[Key] = 'picking'
  AND NOT EXISTS (
    SELECT 1 FROM [dbo].[UserRoles] ur WHERE ur.[UserId] = u.[Id] AND ur.[RoleId] = r.[Id]
  );

-- CanViewTracking -> tracking Rolle
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [CreatedByWindows])
SELECT u.[Id], r.[Id], GETDATE(), 'migration', 'migration'
FROM [dbo].[Users] u
CROSS JOIN [dbo].[Roles] r
WHERE u.[CanViewTracking] = 1
  AND r.[Key] = 'tracking'
  AND NOT EXISTS (
    SELECT 1 FROM [dbo].[UserRoles] ur WHERE ur.[UserId] = u.[Id] AND ur.[RoleId] = r.[Id]
  );

-- CanReportOperations -> reporting Rolle
INSERT INTO [dbo].[UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [CreatedByWindows])
SELECT u.[Id], r.[Id], GETDATE(), 'migration', 'migration'
FROM [dbo].[Users] u
CROSS JOIN [dbo].[Roles] r
WHERE u.[CanReportOperations] = 1
  AND r.[Key] = 'reporting'
  AND NOT EXISTS (
    SELECT 1 FROM [dbo].[UserRoles] ur WHERE ur.[UserId] = u.[Id] AND ur.[RoleId] = r.[Id]
  );

PRINT 'Bestehende Berechtigungen nach UserRoles migriert.';
GO

-- =============================================
-- 5. StammdatenADGruppe AppSetting -> Role.AdGroup migrieren
-- =============================================
IF EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = 'StammdatenADGruppe')
BEGIN
    DECLARE @adGroup NVARCHAR(500);
    SELECT @adGroup = [Value] FROM [dbo].[AppSettings] WHERE [Key] = 'StammdatenADGruppe';

    UPDATE [dbo].[Roles]
    SET [AdGroup] = @adGroup,
        [ModifiedAt] = GETDATE(),
        [ModifiedBy] = 'migration',
        [ModifiedByWindows] = 'migration'
    WHERE [Key] = 'masterdata'
      AND ([AdGroup] IS NULL OR [AdGroup] = '');

    DELETE FROM [dbo].[AppSettings] WHERE [Key] = 'StammdatenADGruppe';
    PRINT 'StammdatenADGruppe nach Role.AdGroup migriert und AppSetting entfernt.';
END
GO

-- =============================================
-- 6. EF Migrations History
-- =============================================
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '20260320101049_AddRolesAndUserRoles')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260320101049_AddRolesAndUserRoles', '10.0.2');
GO

PRINT 'Migration 32_AddRoles abgeschlossen.';
GO
