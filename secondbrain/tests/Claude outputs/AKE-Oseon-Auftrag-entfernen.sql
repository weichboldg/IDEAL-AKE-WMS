-- =====================================================================================================
-- OSEON-Tracking: WMS-Einträge eines neu ausgeschriebenen Auftrags entfernen (Schleife, Muster per LIKE)
-- Ausführen in der WMS-Datenbank (AKE). Stand 2026-11-05.
--
-- Was es tut:
--   1. Sucht alle Einträge in dbo.OseonProductionOrders, deren OseonOrderNumber auf @Muster passt,
--      und zeigt sie an (Id, OseonId, Status, letzte Änderung, Anzahl Arbeitsgänge).
--   2. Löscht je Eintrag zuerst die Arbeitsgänge, dann den Auftrag; prüft die Zeilenzahlen.
--   3. @Ausfuehren = 0 (Vorgabe): alles läuft, wird am Ende aber ZURÜCKGEROLLT — reiner Probelauf.
--      @Ausfuehren = 1: COMMIT.
--
-- ACHTUNG: Der OSEON-Sync legt nur Aufträge neu an, die in OSEON seit MAX(LastChangedInOseon) − 5 min
-- geändert wurden (Delta-Sync, OseonSyncService.cs). Ein gelöschter Eintrag, den in OSEON niemand mehr
-- anfasst, kommt NICHT wieder. Das Muster deshalb so eng wie möglich wählen (am besten die Nummer aus der
-- Fehlermeldung, z. B. N'1617675-06').
-- =====================================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Muster       nvarchar(100) = N'1617675-%';  -- LIKE-Muster der Auftragsnummer
DECLARE @Ausfuehren   bit           = 0;             -- 0 = Probelauf (Rollback), 1 = wirklich löschen
DECLARE @MaxAuftraege int           = 20;            -- Reißleine: mehr Treffer = Abbruch

IF OBJECT_ID('tempdb..#Treffer') IS NOT NULL DROP TABLE #Treffer;

SELECT o.Id, o.OseonId, o.OseonOrderNumber, o.OseonStatus, o.LastChangedInOseon,
       (SELECT COUNT(*) FROM dbo.OseonWorkOperations w WHERE w.OseonProductionOrderId = o.Id) AS Arbeitsgaenge
INTO #Treffer
FROM dbo.OseonProductionOrders o
WHERE o.OseonOrderNumber LIKE @Muster;

SELECT * FROM #Treffer ORDER BY OseonOrderNumber, Id;

DECLARE @Anzahl int = (SELECT COUNT(*) FROM #Treffer);
DECLARE @msg nvarchar(2048);

IF @Anzahl = 0
BEGIN
    SET @msg = CONCAT(N'Keine Einträge für Muster ', @Muster, N' - nichts geändert.');
    THROW 50001, @msg, 1;
END
IF @Anzahl > @MaxAuftraege
BEGIN
    SET @msg = CONCAT(N'Muster ', @Muster, N' trifft ', @Anzahl, N' Einträge (> ', @MaxAuftraege, N') - abgebrochen, nichts geändert.');
    THROW 50002, @msg, 1;
END

DECLARE @Id int, @OseonId bigint, @Nr nvarchar(100), @Ag int, @Rows int,
        @SummeAuftraege int = 0, @SummeAg int = 0;

BEGIN TRAN;

DECLARE c CURSOR LOCAL FAST_FORWARD FOR
    SELECT Id, OseonId, OseonOrderNumber, Arbeitsgaenge FROM #Treffer ORDER BY OseonOrderNumber, Id;
OPEN c;
FETCH NEXT FROM c INTO @Id, @OseonId, @Nr, @Ag;

WHILE @@FETCH_STATUS = 0
BEGIN
    DELETE FROM dbo.OseonWorkOperations WHERE OseonProductionOrderId = @Id;
    SET @Rows = @@ROWCOUNT;
    IF @Rows <> @Ag
    BEGIN
        SET @msg = CONCAT(N'Auftrag ', @Nr, N' (Id ', @Id, N'): ', @Rows, N' Arbeitsgänge gelöscht, erwartet ', @Ag, N' - alles zurückgerollt.');
        THROW 50003, @msg, 1;
    END

    DELETE FROM dbo.OseonProductionOrders WHERE Id = @Id AND OseonId = @OseonId;
    IF @@ROWCOUNT <> 1
    BEGIN
        SET @msg = CONCAT(N'Auftrag ', @Nr, N' (Id ', @Id, N', OseonId ', @OseonId, N') nicht genau einmal gelöscht - alles zurückgerollt.');
        THROW 50004, @msg, 1;
    END

    PRINT CONCAT(N'gelöscht: ', @Nr, N'  Id ', @Id, N'  OseonId ', @OseonId, N'  Arbeitsgänge ', @Ag);
    SET @SummeAuftraege += 1;
    SET @SummeAg += @Ag;

    FETCH NEXT FROM c INTO @Id, @OseonId, @Nr, @Ag;
END

CLOSE c;
DEALLOCATE c;

IF @Ausfuehren = 1
BEGIN
    COMMIT;
    PRINT CONCAT(N'COMMIT: ', @SummeAuftraege, N' Aufträge, ', @SummeAg, N' Arbeitsgänge gelöscht.');
END
ELSE
BEGIN
    ROLLBACK;
    PRINT CONCAT(N'PROBELAUF (Rollback): ', @SummeAuftraege, N' Aufträge, ', @SummeAg, N' Arbeitsgänge würden gelöscht. Zum Ausführen @Ausfuehren = 1 setzen.');
END
