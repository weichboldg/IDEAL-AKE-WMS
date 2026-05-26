-- AKE BDE Light - Datenbank erstellen
-- Server: AKESQL20.ake.at

USE [master]
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'IDEAL_AKE_WMS')
BEGIN
    CREATE DATABASE [IDEAL_AKE_WMS]
    COLLATE Latin1_General_CI_AS
END
GO

USE [IDEAL_AKE_WMS]
GO
