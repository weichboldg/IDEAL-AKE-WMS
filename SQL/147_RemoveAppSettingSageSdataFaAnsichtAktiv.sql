-- SQL/147_RemoveAppSettingSageSdataFaAnsichtAktiv.sql
-- v1.99.14, Spec 2026-11-13-sdata-teil-2-userfelder-stueckliste (F5, Freigabe-Antwort 2), Muster SQL/108.
-- Reine DATEN-Bereinigung (KEIN Schema, KEIN __EFMigrationsHistory-Eintrag).
-- LOESCHT DATEN: die AppSettings-Zeile 'SageSdataFaAnsichtAktiv' (Gate der FA-Struktur live, v1.71.0). Ohne Wirkung - die Seite ist seit
-- v1.99.14 ein Admin-Werkzeug im Register Sage-Test, der Key ist aus Katalog, Standort-Maske und Layout entfernt (Grep Web/Dienst = 0 Leser).
-- Ohne dieses Skript stuende die Zeile unter "Nicht im Katalog". Vorher notieren:
--   SELECT [Key],[Value] FROM [dbo].[AppSettings] WHERE [Key] = N'SageSdataFaAnsichtAktiv';
-- Idempotent; laeuft im Update-Skript in EINER Transaktion (ADR 0023): ohne eigene Transaktionssteuerung und ohne sqlcmd-Direktiven.
SET NOCOUNT ON;
GO

IF EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] = N'SageSdataFaAnsichtAktiv')
BEGIN
    DELETE FROM [dbo].[AppSettings] WHERE [Key] = N'SageSdataFaAnsichtAktiv';
    PRINT 'SQL/147: Schalter SageSdataFaAnsichtAktiv geloescht (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' Zeile).';
END
ELSE
    PRINT 'SQL/147: SageSdataFaAnsichtAktiv nicht vorhanden - nichts zu tun.';
GO
