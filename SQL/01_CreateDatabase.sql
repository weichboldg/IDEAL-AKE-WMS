-- AKE BDE Light - Datenbank erstellen
-- Server: AKESQL20.ake.at

USE [master]
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'AKE_BDE_Light')
BEGIN
    CREATE DATABASE [AKE_BDE_Light]
    COLLATE Latin1_General_CI_AS
END
GO

USE [AKE_BDE_Light]
GO
