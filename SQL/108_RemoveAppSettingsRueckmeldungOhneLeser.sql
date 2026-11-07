-- SQL/108_RemoveAppSettingsRueckmeldungOhneLeser.sql
-- v1.79.0, Spec 2026-11-08-bereinigungen-nach-buendel-2026-11-07 (F5, Freigabe-Antwort 3).
-- Reine DATEN-Bereinigung (KEIN Schema, KEIN __EFMigrationsHistory-Eintrag), wie SQL/86.
-- LOESCHT DATEN: die AppSettings-Zeilen 'OseonRueckmeldungAktiv' und 'SageRueckmeldungAktiv'. Fachlich ohne Wirkung - kein Leser im Code
-- (Grep 2026-11-07, Web/Dienst/SQL/JS); die Keys sind aus dem Katalog AppSettingDefinitions entfernt. Ohne dieses Skript stuenden sie nach dem
-- Deploy unter "Nicht im Katalog". Vorher P-4 ausfuehren und Werte notieren:
--   SELECT [Key],[Value] FROM [dbo].[AppSettings] WHERE [Key] IN (N'OseonRueckmeldungAktiv', N'SageRueckmeldungAktiv');
-- Idempotent: der zweite Lauf aendert nichts und meldet keinen Fehler.
-- Hinweis: SQL/27 legt die Zeilen bei einem erneuten Lauf wieder an (historisches Skript, wird nicht umgeschrieben); sie erscheinen dann ohne
-- Wirkung unter "Nicht im Katalog".
SET NOCOUNT ON;
GO

IF EXISTS (SELECT 1 FROM [dbo].[AppSettings] WHERE [Key] IN (N'OseonRueckmeldungAktiv', N'SageRueckmeldungAktiv'))
BEGIN
    DELETE FROM [dbo].[AppSettings] WHERE [Key] IN (N'OseonRueckmeldungAktiv', N'SageRueckmeldungAktiv');
    PRINT 'SQL/108: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' Rueckmelde-Schalter ohne Leser geloescht.';
END
ELSE
    PRINT 'SQL/108: keine Rueckmelde-Schalter vorhanden - nichts zu tun.';
GO
