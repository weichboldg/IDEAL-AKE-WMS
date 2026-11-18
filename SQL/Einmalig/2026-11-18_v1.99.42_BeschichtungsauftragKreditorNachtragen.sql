-- =====================================================================================================
-- SQL/Einmalig/2026-11-18_v1.99.42_BeschichtungsauftragKreditorNachtragen.sql
--
-- EINMALIGES DATENSKRIPT (ADR 0015/0023) - KEINE Schema-Aenderung, KEIN __EFMigrationsHistory-Eintrag, KEIN Eintrag in
-- SQL/00_FreshInstall.sql, NICHT Teil der nummerierten SQL-Reihe. Nicht loeschend; DB-Backup vor dem Scharflauf trotzdem Pflicht.
-- Spec 2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck, Befund 15:05 (a).
--
-- Hintergrund: Seit v1.99.41 waehlt der Beschichtungsauftrag den Lackierer aus den Kreditoren (CreditorId). Neue Auftraege belegt
-- BeschichtungsauftragWorkflow.KreditorVorbelegen vor; Altauftraege (vor v1.99.41) haben CreditorId = NULL und nur den Text
-- Dienstleister - der Druck zeigt dann keine Anschrift.
--
-- Regel = KreditorVorbelegen: Dienstleister (getrimmt) = Kurzname (getrimmt) GENAU EINES aktiven Beschichter-Kreditors
-- (IsActive = 1, IsCoater = 1), Vergleich ohne Gross/Klein (Collation der DB, CI). Dann CreditorId setzen und Dienstleister = Name des
-- Kreditors (leerer Name -> Text bleibt). Kein Treffer oder mehrere -> nichts, nur in der Meldeliste (Kreditor im Auftrag waehlen).
-- Stornierte Auftraege bleiben unberuehrt.
--
-- Wiederholbar: nachgetragene Auftraege haben CreditorId, ein zweiter Trockenlauf liefert TREFFER: 0.
-- REIHENFOLGE: 1. SQL/161 da (v1.99.41).  2. Beschichter-Kreditoren gekennzeichnet (/Creditors, Pruefung BeschichterKreditoren).
--   3. Trockenlauf (Default).  4. DB-Backup.  5. @Trockenlauf = 0 (Update-Skript: SCHARF).  6. Zweiter Trockenlauf muss TREFFER: 0 liefern.
-- =====================================================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Trockenlauf BIT = 1;   -- <<< 0 = ausfuehren (nach DB-Backup und Freigabe der Zaehlung)

IF COL_LENGTH(N'dbo.Beschichtungsauftraege', N'CreditorId') IS NULL
BEGIN
    RAISERROR(N'SQL/161 fehlt (dbo.Beschichtungsauftraege.CreditorId). Abbruch, nichts geaendert.', 16, 1);
    RETURN;
END

IF OBJECT_ID('tempdb..#Kand') IS NOT NULL DROP TABLE #Kand;

-- Je Altauftrag die Zahl passender Beschichter (0, 1, mehrere) und - bei genau einem - dessen Id/Name.
SELECT b.Id, b.Nummer, b.Status, LTRIM(RTRIM(b.Dienstleister)) AS Text,
       COUNT(c.Id) AS Treffer, MIN(c.Id) AS CreditorId, MIN(c.Name) AS CreditorName
INTO #Kand
FROM dbo.Beschichtungsauftraege b
LEFT JOIN dbo.Creditors c
       ON c.IsActive = 1 AND c.IsCoater = 1 AND LTRIM(RTRIM(c.ShortName)) = LTRIM(RTRIM(b.Dienstleister))
WHERE b.CreditorId IS NULL
  AND b.Status <> 90   -- Storniert
  AND NULLIF(LTRIM(RTRIM(b.Dienstleister)), N'') IS NOT NULL
GROUP BY b.Id, b.Nummer, b.Status, b.Dienstleister;

PRINT N'--- Zaehlung (Trockenlauf und Ausfuehrung)';
SELECT SUM(CASE WHEN Treffer = 1 THEN 1 ELSE 0 END) AS Nachtragen,
       SUM(CASE WHEN Treffer = 0 THEN 1 ELSE 0 END) AS KeinBeschichter,
       SUM(CASE WHEN Treffer > 1 THEN 1 ELSE 0 END) AS Mehrdeutig
FROM #Kand;

PRINT N'--- Nachtragen: Auftrag -> Kreditor';
SELECT Nummer, Status, Text AS Dienstleister, CreditorId, CreditorName FROM #Kand WHERE Treffer = 1 ORDER BY Nummer;

PRINT N'--- Meldeliste: kein oder mehrdeutiger Beschichter - Kreditor im Auftrag waehlen (Entwurf/Uebergeben) oder Kurzname pflegen';
SELECT Nummer, Status, Text AS Dienstleister, Treffer FROM #Kand WHERE Treffer <> 1 ORDER BY Nummer;

IF @Trockenlauf = 1
BEGIN
    DECLARE @Treffer INT = (SELECT COUNT(*) FROM #Kand WHERE Treffer = 1);
    PRINT CONCAT(N'TREFFER: ', @Treffer);
    PRINT N'TROCKENLAUF: nichts geaendert. Ausfuehren mit @Trockenlauf = 0 (vorher DB-Backup, Zaehlung freigeben).';
    RETURN;
END

BEGIN TRANSACTION;
    UPDATE b
       SET b.CreditorId        = k.CreditorId,
           b.Dienstleister     = COALESCE(NULLIF(LTRIM(RTRIM(k.CreditorName)), N''), b.Dienstleister),
           b.ModifiedAt        = GETDATE(),
           b.ModifiedBy        = N'Einmalig-v1.99.42-KreditorNachtragen',
           b.ModifiedByWindows = SYSTEM_USER
      FROM dbo.Beschichtungsauftraege b
      JOIN #Kand k ON k.Id = b.Id
     WHERE k.Treffer = 1
       AND b.CreditorId IS NULL;   -- Doppelsicherung
    PRINT CONCAT(N'Kreditor nachgetragen: ', @@ROWCOUNT, N' Auftrag/Auftraege.');
COMMIT TRANSACTION;

DROP TABLE #Kand;
