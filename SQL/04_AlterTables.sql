-- AKE BDE Light - Tabellen erweitern
-- Barcode-Wert für Lagerplätze + Passwort-Hash für Benutzer
USE [AKE_BDE_Light]
GO

-- StorageLocations: BarcodeValue hinzufügen
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StorageLocations') AND name = 'BarcodeValue')
BEGIN
    ALTER TABLE [dbo].[StorageLocations] ADD [BarcodeValue] NVARCHAR(50) NULL;
END
GO

-- BarcodeValue mit Code-Wert befüllen für bestehende Datensätze
UPDATE [dbo].[StorageLocations] SET [BarcodeValue] = [Code] WHERE [BarcodeValue] IS NULL;
GO

-- Users: PasswordHash hinzufügen
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'PasswordHash')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [PasswordHash] NVARCHAR(500) NULL;
END
GO
