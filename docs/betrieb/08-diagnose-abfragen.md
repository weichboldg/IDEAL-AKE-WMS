# 08 – Diagnose-Abfragen (nur lesend)

**Stand:** v1.99.80, 2026-11-25 (A-8.8 Teilmeldungen, A-8.9 Terminal-Zuordnung, Hinweis A-8.7 Konflikt), v1.99.70 · **Zielgruppe:** IT-Administratoren (ohne Entwicklerwissen)

Sammlung **lesender** SQL-Abfragen für die WMS-Datenbank, gruppiert nach Thema. Jede Abfrage hat Zweck, erwartetes Ergebnis und Quelle.
Die Abfragen stammen aus dem Update-Manifest (`scripts/deploy/nachlauf.psd1`), aus `docs/TESTSZENARIEN.md` und aus den Testberichten;
Tabellen- und Spaltennamen sind gegen `SQL/00_FreshInstall.sql` geprüft (Zeilenangaben je Abfrage).

> [!warning] Nur lesen
> - Alle Abfragen sind reine `SELECT`. Keine `INSERT`/`UPDATE`/`DELETE`, keine Prozeduren mit Wirkung. Die **Reparaturen** aus dem
>   Manifest stehen hier bewusst nicht – die führt nur das Update-Skript nach Bestätigung aus
>   ([03-installation-update-rollback.md](03-installation-update-rollback.md)).
> - Auf **Produktivsystemen** nur mit einem **lesenden** Konto ausführen (z. B. Rolle `db_datareader`). **zu prüfen:** Ein solches Konto ist
>   im Projekt nicht dokumentiert; Konten: [09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md).
> - Datenbank vorher wählen (`USE <WMS-DB>` bzw. Auswahl in SSMS). Datenbanken je System: [02-systeme-umgebungen.md](02-systeme-umgebungen.md).
> - Zeitvergleiche mit `GETDATE()` nutzen die Uhr des SQL-Servers; die WMS-Zeitstempel sind Ortszeit des **schreibenden** Rechners
>   (ADR [[0025-zeitstempel-in-lokalzeit-des-schreibenden-rechners]]). Die Zeitzone der SQL-Server ist nicht belegt (ebd.) – Zeitfenster
>   deshalb großzügig wählen.
> - Keine Spalten mit Geheimnissen abfragen (`Users.PasswordHash`, ServiceSetting `SData:Password`).

Platzhalter in spitzen Klammern (`<…>`) vor dem Ausführen ersetzen.

---

## 1. Version, Migrationen, Update-Läufe

### A-1.1 Letzte Schema-Migrationen

```sql
SELECT TOP 10 MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId DESC;
```

- **Zweck:** Welcher Schema-Stand ist eingespielt?
- **Erwartet bei v1.99.23:** oberste Zeile `20261114080950_AddPurchaseOrderReceiptAbsendekennung` (SQL/150).
- **Quelle:** Tabelle `SQL/00_FreshInstall.sql:3013-3017`; MigrationId `SQL/00_FreshInstall.sql:3262-3264`, `scripts/deploy/nachlauf.psd1:97-98`.

### A-1.2 Ist ein bestimmtes Schema-Skript wirklich da?

```sql
SELECT
  (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261114080950_AddPurchaseOrderReceiptAbsendekennung') AS InHistory,
  COL_LENGTH(N'dbo.PurchaseOrderReceipts', N'Absendekennung') AS SpalteBytes;
```

- **Zweck:** History-Eintrag **und** Schema-Objekt prüfen (Beispiel SQL/150). Für andere Skripte MigrationId aus dem Manifest nehmen.
- **Erwartet:** `InHistory = 1`, `SpalteBytes = 16` (nicht `NULL`).
- **Quelle:** Prüfmuster aus `SQL/00_FreshInstall.sql:3262-3264` (History-Eintrag nur, wenn die Spalte existiert); Spalte `SQL/00_FreshInstall.sql:2410`.

### A-1.3 Marker der Daten- und Einmal-Skripte (Update-Skript)

```sql
IF OBJECT_ID(N'dbo.DeployScriptRuns', N'U') IS NULL
    SELECT N'Tabelle fehlt (SQL/142 nicht eingespielt)' AS Hinweis;
ELSE
    SELECT ManifestId, Mode, Hits, PackageVersion, PackageCommit, ExecutedAt, ExecutedBy, Note
    FROM dbo.DeployScriptRuns
    ORDER BY ExecutedAt DESC;
```

- **Zweck:** Welche Daten-/Einmal-Skripte liefen wann, in welchem Modus, mit wie vielen Treffern?
- **Erwartet:** höchstens eine Zeile je `ManifestId` (eindeutiger Index). Ein Eintrag mit Marker wird nie wieder angeboten. `ExecutedAt` ist die
  Uhr des SQL-Servers (Befund UP-3).
- **Quelle:** `SQL/00_FreshInstall.sql:2983-3005`; `scripts/deploy/nachlauf.psd1:18-20`; `scripts/deploy/Update-IdealAkeWms.ps1:325-332`;
  [[2026-11-14-review-befunde-update-skript]] UP-3.

### A-1.4 Voraussetzungen des Update-Skripts (P-B1)

```sql
SELECT SERVERPROPERTY('ProductVersion') AS SqlVersion;
SELECT * FROM sys.dm_os_file_exists(CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(4000)));
SELECT HAS_PERMS_BY_NAME(N'<WMS-DB>', 'DATABASE', 'ALTER') AS AlterDb;
```

- **Zweck:** SQL-Version, Standard-Sicherungsordner vorhanden, Recht `ALTER` des **ausführenden** Kontos (für die RCSI-Reparatur).
- **Erwartet:** Version ≥ 2017 (14.x); Backup-Ordner `file_exists`/`file_is_a_directory` = 1, sonst `-BackupPfad` angeben; `AlterDb = 1` für das
  Update-Konto (beim lesenden Konto ist 0 normal).
- **Quelle:** `scripts/deploy/nachlauf.psd1:342-344`; [[2026-11-12-deploy-fahrplan-buendel-v1-99]] (SQL ≥ 2017, Backup-Ordner);
  [[2026-11-11-buendel-deploy-update-skript]] (OS-Fehler 3).

### A-1.5 Anmeldeverfahren der eigenen Verbindung

```sql
SELECT auth_scheme FROM sys.dm_exec_connections WHERE session_id = @@SPID;
```

- **Zweck:** Läuft die Verbindung über Kerberos oder NTLM (Protected Users, SPN)?
- **Erwartet:** `KERBEROS` nach gesetztem SPN; `NTLM` geht nur, wenn das Konto nicht in Protected Users ist.
- **Quelle:** [[2026-11-11-buendel-deploy-update-skript]] (Test mit `auth_scheme = KERBEROS`, „Danach geht sqlcmd (`auth_scheme = NTLM`)“).
  **zu prüfen:** ob das lesende Konto dafür `VIEW SERVER STATE` braucht.

### A-1.6 RCSI (Read Committed Snapshot)

```sql
SELECT name, is_read_committed_snapshot_on
FROM sys.databases
WHERE name = DB_NAME();

-- Prüfung wie im Update-Skript: 1 = Abweichung (RCSI aus, aber FA-Struktur-Import an)
SELECT CASE WHEN d.is_read_committed_snapshot_on = 0 AND EXISTS (
         SELECT 1 FROM dbo.ServiceSettings
         WHERE [Key] IN (N'Sync:HierarchicalFaEnabled', N'ProduktionsauftragHierarchisch')
           AND LOWER(LTRIM(RTRIM([Value]))) = N'true')
       THEN 1 ELSE 0 END AS Abweichung
FROM sys.databases d WHERE d.name = DB_NAME();
```

- **Zweck:** Der FA-Struktur-Import (IDEAL) braucht RCSI an.
- **Erwartet:** `Abweichung = 0`; mit FA-Struktur-Import `is_read_committed_snapshot_on = 1`.
- **Quelle:** `scripts/deploy/nachlauf.psd1:175-179`.

---

## 2. Dienst-Läufe (Aktivitäts-Protokoll `dbo.SyncLogs`)

Tabelle: `SQL/00_FreshInstall.sql:1193-1201` (Index auf `Timestamp` und `Service, Level`: Zeile 1239-1242). Stufen `Info`/`Warning`/`Error`
(`IdealAkeWms/Models/SyncLogLevel.cs:5-7`). Meldungstexte: `IdealAkeWms/Services/SyncLogger/SyncRun.cs:40,51,60-61`.

### A-2.1 Letzter Lauf je Dienst (7 Tage)

```sql
SELECT Service,
       MAX(CASE WHEN Message LIKE N'Run erfolgreich beendet%' THEN [Timestamp] END) AS LetzterErfolg,
       MAX(CASE WHEN Message LIKE N'Run fehlgeschlagen%'      THEN [Timestamp] END) AS LetzterFehler,
       MAX([Timestamp]) AS LetzterEintrag
FROM dbo.SyncLogs
WHERE [Timestamp] >= DATEADD(day, -7, GETDATE())
GROUP BY Service
ORDER BY Service;
```

- **Zweck:** Laufen die Syncs? (täglicher Check T1 in [06-logs-ueberwachung.md](06-logs-ueberwachung.md)).
- **Erwartet:** Für eingeschaltete Syncs `LetzterErfolg` innerhalb eines Takts (Standard 15 min, `README.md:935`); `LetzterFehler` älter als
  `LetzterErfolg`. Dienste, die ohne Arbeit keinen Lauf schreiben (z. B. `SageWareneingang`), können fehlen ([[services]] Sage-Wareneingang).
- **Quelle:** Meldungstexte `SyncRun.cs:40,51`; Dienst-Namen `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs:53-64`.

### A-2.2 Fehler und Warnungen der letzten 24 Stunden

```sql
SELECT TOP 200 [Timestamp], Service, [Level], Message, Reference
FROM dbo.SyncLogs
WHERE [Level] IN (N'Error', N'Warning')
  AND [Timestamp] >= DATEADD(hour, -24, GETDATE())
ORDER BY [Timestamp] DESC;
```

- **Zweck:** Neue Fehler finden; Einordnung über die Tabelle „Typische Warnungen“ in [06-logs-ueberwachung.md](06-logs-ueberwachung.md) §9.
- **Erwartet:** keine unbekannten `Error`; bekannte OSEON-Timeouts/Deadlocks möglich.
- **Quelle:** [[2026-10-20-ake-testsystem-bestandsaufnahme]] 2.3; [[teil-1]] BS16 (Fehler/Warnungen-Vergleich DEV ↔ akenet01).

### A-2.3 Läufe ohne Abschluss

```sql
SELECT TOP 50 [Timestamp], Service
FROM dbo.SyncLogs
WHERE Message = N'Run wurde unerwartet beendet (kein FinishXxx-Aufruf)'
ORDER BY [Timestamp] DESC;
```

- **Zweck:** Läufe, die ohne Ende-Meldung abbrachen (z. B. Dienst gestoppt während des Laufs).
- **Erwartet:** selten, nur um Dienst-Neustarts/Updates herum.
- **Quelle:** `IdealAkeWms/Services/SyncLogger/SyncRun.cs:56-62`; [[teil-1]] BS16 (Artikel-Lieferanten-Abgleich 07.11.).

### A-2.4 Einstellungsprüfung beim Web-Start

```sql
SELECT TOP 50 [Timestamp], [Level], Message, Reference
FROM dbo.SyncLogs
WHERE Service = N'Einstellungspruefung'
ORDER BY [Timestamp] DESC;
```

- **Zweck:** Ungültige gespeicherte Einstellungswerte nach einem Neustart/Update.
- **Erwartet:** keine Befunde; sonst die genannte Maske einmal speichern.
- **Quelle:** `IdealAkeWms/Program.cs:381-398`; `scripts/deploy/nachlauf.psd1:407-408`.

### A-2.5 Einstellungen ansehen (ohne Geheimnisse)

```sql
SELECT [Category], [Key], [Value]
FROM dbo.ServiceSettings
WHERE LOWER([Key]) <> N'sdata:password'
ORDER BY [Category], [Key];

-- Prüfungen aus dem Manifest, je 1 = Abweichung
SELECT
  (SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N'Lieferverzug:MailAktiv' AND LOWER(LTRIM(RTRIM([Value]))) = N'true')
                AND NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N'Lieferverzug:EinkaufEmpfaenger' AND LEN(LTRIM(RTRIM([Value]))) > 0)
               THEN 1 ELSE 0 END) AS LieferverzugOhneEmpfaenger,
  (SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N'Sync:ArticlesViewName' AND LTRIM(RTRIM(ISNULL([Value], N''))) <> N'')
               THEN 0 ELSE 1 END) AS ArticlesViewNameLeer_nurIDEAL;
```

- **Zweck:** Stand der Dienst-Einstellungen; zwei Manifest-Prüfungen.
- **Erwartet:** `LieferverzugOhneEmpfaenger = 0`; `ArticlesViewNameLeer_nurIDEAL = 0` auf IDEAL (bei AKE ist leer richtig).
- **Quelle:** Tabelle `SQL/00_FreshInstall.sql:1097-1102`; Prüfungen `scripts/deploy/nachlauf.psd1:211-214,231-234`; Geheimnis-Key
  `README.md:518-540`; abweichende Groß-/Kleinschreibung des Geheim-Keys als Befund: [[2026-11-14-review-befunde-sdata]] SD-3
  (deshalb `LOWER`).

---

## 3. Sage-Lagerbuchungen (`dbo.SageBookingQueueItems`)

Tabelle `SQL/00_FreshInstall.sql:318-338`; Status 0 Offen, 1 Gesendet, 2 Bestätigt, 3 Fehler (`IdealAkeWms/Models/SageBookingQueueStatus.cs:13-16`).

### A-3.1 Anzahl je Status

```sql
SELECT [Status],
       CASE [Status] WHEN 0 THEN N'Offen' WHEN 1 THEN N'Gesendet' WHEN 2 THEN N'Bestaetigt' WHEN 3 THEN N'Fehler' END AS StatusText,
       COUNT(*) AS Anzahl, MAX(CreatedAt) AS Juengster
FROM dbo.SageBookingQueueItems
GROUP BY [Status]
ORDER BY [Status];
```

- **Zweck:** Überblick wie auf `/SageBookingQueue`.
- **Erwartet:** `Fehler` = 0; `Offen` nur kurzzeitig (Takt 20 s) bzw. dauerhaft, wenn `SageLagerbuchungAktiv` aus ist.
- **Quelle:** `SQL/00_FreshInstall.sql:321`; [[services]] SageBookingWorker.

### A-3.2 Fehler mit Text

```sql
SELECT TOP 100 Id, StockMovementId, AttemptCount, LastAttemptAt, LastError, SageKennung, CreatedAt
FROM dbo.SageBookingQueueItems
WHERE [Status] = 3
ORDER BY LastAttemptAt DESC;
```

- **Zweck:** Ursache je Fehler-Eintrag (Spalte `SageResponseRaw` bewusst weggelassen, kann groß sein).
- **Erwartet:** leer. Bekannte Fehlerarten: SSL, 401, 400, 500 ([[2026-10-20-ake-testsystem-bestandsaufnahme]]).
- **Quelle:** `SQL/00_FreshInstall.sql:318-338`.

### A-3.3 Hängend in „Gesendet“

```sql
SELECT Id, StockMovementId, AttemptCount, SentAt, LastAttemptAt, SageKennung
FROM dbo.SageBookingQueueItems
WHERE [Status] = 1
  AND SentAt < DATEADD(minute, -10, GETDATE())
ORDER BY SentAt;
```

- **Zweck:** Einträge, die länger als die Hänge-Zeit (`Sync:SageLagerbuchungStuckMinutes`, Standard 10) in `Gesendet` stehen.
- **Erwartet:** leer; der Dienst klärt sie selbst per Suche in Sage. Bleiben sie stehen: Dienst-Log prüfen.
- **Quelle:** `IdealAkeWms/Data/Repositories/SageBookingQueueRepository.cs:62,96`; [[services]] SageBookingWorker (Recovery hängender `Gesendet`).

---

## 4. Sage-Wareneingänge und CSV

Tabelle `dbo.PurchaseOrderReceipts`: `SQL/00_FreshInstall.sql:2388-2424`. Status `SageWeStatus`: 0 NichtNoetig, 1 Offen, 2 Gesendet,
3 Uebergeben, 4 Fehler, 5 NurInSage, 6 Klaeren (`IdealAkeWms/Models/PurchaseOrderReceipt.cs:15-26`).

### A-4.1 Anzahl je Status

```sql
SELECT SageWeStatus,
       CASE SageWeStatus WHEN 0 THEN N'NichtNoetig' WHEN 1 THEN N'Offen' WHEN 2 THEN N'Gesendet' WHEN 3 THEN N'Uebergeben'
                         WHEN 4 THEN N'Fehler' WHEN 5 THEN N'NurInSage' WHEN 6 THEN N'Klaeren' END AS StatusText,
       COUNT(*) AS Anzahl
FROM dbo.PurchaseOrderReceipts
GROUP BY SageWeStatus
ORDER BY SageWeStatus;
```

- **Zweck:** Überblick wie auf `/SageBookingQueue?herkunft=Wareneingang` (bis v1.99.65 `/PurchaseOrders/SageWareneingaenge`; `NichtNoetig`/`NurInSage` zeigt die Seite nicht).
- **Erwartet:** `Fehler` und `Klaeren` = 0 bzw. in Bearbeitung; `Gesendet` nur kurzzeitig.
- **Quelle:** wie oben; Bedeutung `scripts/deploy/nachlauf.psd1:432-434,444-449`.

### A-4.2 Fehler und Klären mit Details

```sql
SELECT TOP 100 Id, Belegnummer, BelPosId, Quantity, SageWeStatus, SageWeVersuche, SageWeFehler,
       SageWeKennung, SageWeVorgang, SageWeBelegnummer, SageWeAm, CreatedAt, CreatedBy
FROM dbo.PurchaseOrderReceipts
WHERE SageWeStatus IN (4, 6)
ORDER BY CreatedAt DESC;
```

- **Zweck:** Grundlage für die Klärung in Sage (Kennung `WE#…#` steht im Sage-Memo).
- **Erwartet:** leer oder bekannte Fälle in Bearbeitung.
- **Quelle:** `SQL/00_FreshInstall.sql:2400-2410`; Kennung [[fallstricke]] §31.

### A-4.3 Wareneingang-CSV

```sql
SELECT [Status], CASE [Status] WHEN 1 THEN N'Offen' WHEN 2 THEN N'Geschrieben' WHEN 3 THEN N'Fehler' END AS StatusText, COUNT(*) AS Anzahl
FROM dbo.WareneingangCsvExporte
GROUP BY [Status];

SELECT TOP 50 Id, Barcode, Versuche, Fehler, Datei, GeschriebenAm, ModifiedAt
FROM dbo.WareneingangCsvExporte
WHERE [Status] = 3
ORDER BY ModifiedAt DESC;

-- Manifest-Prüfung (nur AKE): 1 = CSV an, aber Pfad leer oder Sage-WE aus
SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N'WareneingangCsvAktiv' AND LOWER(LTRIM(RTRIM([Value]))) = N'true')
             AND (NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N'WareneingangCsvPfad' AND LEN(LTRIM(RTRIM([Value]))) > 0)
                  OR NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N'SageWareneingangBelegAktiv' AND LOWER(LTRIM(RTRIM([Value]))) = N'true'))
            THEN 1 ELSE 0 END AS Abweichung;
```

- **Zweck:** Wurden die CSV-Dateien geschrieben? Fehlertexte (z. B. „Der Netzwerkname wurde nicht gefunden.“).
- **Erwartet:** keine Zeilen mit Status 3; `Abweichung = 0`.
- **Quelle:** Tabelle `SQL/00_FreshInstall.sql:2432-2447`; Prüfung `scripts/deploy/nachlauf.psd1:215-218`; Fehlertexte [[teil-1]] BS16.

---

## 5. Doppel-Prüfungen (TS-138, v1.99.23)

Seit v1.99.23 sollen doppelte Buchungen nicht mehr entstehen (ADR [[0026-schreibaktionen-senden-die-absicht-der-server-prueft-am-schreibort]]).
Diese Abfragen weisen Reste nach.

### A-5.1 Doppelter Wareneingang aus demselben Absenden

```sql
SELECT Absendekennung, BelPosId, COUNT(*) AS Anzahl
FROM dbo.PurchaseOrderReceipts
WHERE Absendekennung IS NOT NULL
GROUP BY Absendekennung, BelPosId
HAVING COUNT(*) > 1;
```

- **Erwartet:** leer (der eindeutige Index verhindert es zusätzlich). Zeilen von vor v1.99.23 haben keine Kennung und erscheinen hier nie.
- **Quelle:** `docs/TESTSZENARIEN.md` TS-138.4g; Index `SQL/00_FreshInstall.sql:2423-2424`.

### A-5.2 Doppelte AG-Fertigmeldung

```sql
SELECT b.WorkOperationId, COUNT(*) AS Anzahl
FROM dbo.BdeBookingQuantities q
JOIN dbo.BdeBookings b ON b.Id = q.BdeBookingId
WHERE q.IsFinal = 1 AND b.IsCancelled = 0
GROUP BY b.WorkOperationId
HAVING COUNT(*) > 1;
```

- **Erwartet:** leer. Vor v1.99.23 auf IDEAL-DEV belegt: 4 von 6 parallelen Läufen doppelt (BDE-1).
- **Quelle:** `docs/TESTSZENARIEN.md` TS-138.5b; Tabellen `SQL/00_FreshInstall.sql:2480-2491,2562-2568`; [[03-ideal-hierarchie-beschichtung-bde]] 96.7.

### A-5.3 Mehr als ein Folgeauftrag je Kommissionierauftrag

```sql
SELECT UrsprungsauftragId, COUNT(*) AS AnzahlFolgeauftraege
FROM dbo.PickingOrders
WHERE UrsprungsauftragId IS NOT NULL
GROUP BY UrsprungsauftragId
HAVING COUNT(*) > 1;
```

- **Erwartet:** leer (TS-138.1b erwartet je Auftrag genau einen Folgeauftrag).
- **Quelle:** abgeleitet aus `docs/TESTSZENARIEN.md` TS-138.1b (dort je Id: `SELECT COUNT(*) FROM PickingOrders WHERE UrsprungsauftragId = <Id>` = 1);
  Spalte `SQL/00_FreshInstall.sql:2849`. **zu prüfen:** ob mehrere Folgeaufträge je Ursprung fachlich vorkommen dürfen.

Freigegebene FAs ohne Auftrag (TS-138.3): A-6.3.

---

## 6. Kommissionierung

### A-6.1 Kommissionierbereiche und Standard

```sql
SELECT Id, Name, Kuerzel, Reihenfolge, IsActive, IsDefault, IstVorkommissionierung
FROM dbo.PickingAreas
ORDER BY Reihenfolge;

-- Manifest-Prüfungen: je 1 = Abweichung
SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.PickingAreas) THEN 0 ELSE 1 END AS KeineBereiche,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.PickingAreas)
             AND NOT EXISTS (SELECT 1 FROM dbo.PickingAreas WHERE IsDefault = 1 AND IsActive = 1) THEN 1 ELSE 0 END AS KeinAktiverStandard;
```

- **Erwartet:** mindestens ein Bereich (Seed: Lager, Glas, Vorkommissionierung), genau ein aktiver Standard; beide Prüfwerte 0.
- **Quelle:** `scripts/deploy/nachlauf.psd1:166-174`; Tabelle `SQL/00_FreshInstall.sql:2794-2801`; Seed [[2026-11-11-buendel-deploy-update-skript]] 16:24/16:41.

### A-6.2 Altbestand-Überführung

```sql
-- Spur des Einmal-Skripts (> 0 = lief schon)
IF OBJECT_ID(N'dbo.PickingOrders', N'U') IS NULL SELECT 0 AS Spur
ELSE SELECT COUNT(*) AS Spur FROM dbo.PickingOrders WHERE CreatedBy = N'Migration-KO-Altbestand';

-- Noch nicht abgearbeitete Liste "Altbestand pruefen"
SELECT o.Id, o.Nummer, o.ProductionOrderId, o.[Status], o.CreatedAt
FROM dbo.PickingOrders o
WHERE o.AltbestandPruefen = 1
ORDER BY o.ProductionOrderId, o.Nummer;
```

- **Erwartet:** nach dem Update mit Altbestand `Spur > 0`; die zweite Liste wird leer, sobald im Leitstand „Altbestand geprüft“ je FA bestätigt ist.
- **Quelle:** `scripts/deploy/nachlauf.psd1:281-285,417-418`; Rücksetzen des Kennzeichens `IdealAkeWms/Services/PickingOrderWorkflow.cs:282-291`;
  Spalten `SQL/00_FreshInstall.sql:2836-2861`.

### A-6.3 Freigegebener HauptFA ohne Kommissionierauftrag

```sql
SELECT po.Id, po.OrderNumber, ps.ReleasedAt, ps.ReleasedBy
FROM dbo.ProductionOrderPickingStatus ps
JOIN dbo.ProductionOrders po ON po.Id = ps.ProductionOrderId
WHERE ps.IsReleasedForPicking = 1
  AND ps.IsDonePicking = 0
  AND po.IsDone = 0 AND po.IsCancelled = 0
  AND (po.SubOrderNumber = N'' OR po.SubOrderNumber = po.OrderNumber)   -- HauptFA
  AND NOT EXISTS (SELECT 1 FROM dbo.PickingOrders o
                  WHERE o.ProductionOrderId = po.Id AND o.[Status] <> 90)   -- 90 = Storniert
ORDER BY ps.ReleasedAt;
```

- **Zweck:** FAs, die durch den Deadlock-Fehler vor v1.99.23 (N-2) oder einen Stücklisten-Lesefehler (N-3) ohne Auftrag freigegeben sind.
- **Erwartet:** leer. Treffer: seit v1.99.70 holt der nächtliche Dienst-Lauf „Kommissionier-Nachtrag“ sie nach (A-6.5); sofort nur ein
  Admin im Leitstand mit „Fehlende Kommissionieraufträge … erzeugen“.
- **Quelle:** abgeleitet aus `docs/TESTSZENARIEN.md` TS-138.3 („kein freigegebener Test-FA ohne Kommissionierauftrag“); HauptFA-Bedingung aus
  `scripts/deploy/nachlauf.psd1:243-246`; Status `IdealAkeWms/Models/PickingOrder.cs:10-17`; [[01-kommissionierung]] N-2/N-3.
  **zu prüfen:** ob ein freigegebener FA ohne kommissionierbare Positionen legitim keinen Auftrag hat.

### A-6.4 Doppelte Kommissionier-Prio

```sql
-- 1) Doppelte Prio unter offenen HauptFAs
;WITH k AS (
    SELECT po.OrderNumber, ps.PickingPriority, ps.IsReleasedForPicking, ps.IsDonePicking,
           ps.ReleasedAt, ps.ModifiedAt, ps.ModifiedBy,
           COUNT(*) OVER (PARTITION BY ps.PickingPriority) AS AnzahlGleichePrio
    FROM dbo.ProductionOrderPickingStatus ps
    JOIN dbo.ProductionOrders po ON po.Id = ps.ProductionOrderId
    WHERE ps.PickingPriority IS NOT NULL
      AND po.IsDone = 0 AND po.IsCancelled = 0
      AND (po.SubOrderNumber = N'' OR po.SubOrderNumber = po.OrderNumber)
)
SELECT * FROM k WHERE AnzahlGleichePrio > 1
ORDER BY PickingPriority, IsReleasedForPicking DESC, OrderNumber;

-- 3) Offene/in Arbeit befindliche Auftraege VERSCHIEDENER FAs mit gleicher Prio
SELECT o.PickingPriority, COUNT(DISTINCT o.ProductionOrderId) AS AnzahlFAs, COUNT(*) AS AnzahlAuftraege
FROM dbo.PickingOrders o
JOIN dbo.ProductionOrders po ON po.Id = o.ProductionOrderId
WHERE o.[Status] IN (10, 20)
  AND o.PickingPriority IS NOT NULL
  AND po.IsDone = 0 AND po.IsCancelled = 0
GROUP BY o.PickingPriority
HAVING COUNT(DISTINCT o.ProductionOrderId) > 1
ORDER BY o.PickingPriority;
```

- **Erwartet:** nur Altbestand von vor v1.99.17 oder bewusst gleiche Prio (erlaubt). Korrektur im Leitstand.
- **Quelle:** Prüfabfragen 1 und 3 aus [[2026-11-13-doppelte-kommissionier-prio-nach-ruecknahme-spec]]; Manifest `scripts/deploy/nachlauf.psd1:243-246`;
  Spalten `SQL/00_FreshInstall.sql:428-435`.

### A-6.5 Kommissionier-Nachtrag (nachts) und Marker „Stückliste kleiner“ (seit v1.99.70)

```sql
-- 1) Letzte Laeufe "Kommissionier-Nachtrag" mit Warnungen (je FA eine Zeile, Referenz = FA-Nummer)
SELECT TOP 50 [Timestamp], Level, Message, Reference
FROM dbo.SyncLogs
WHERE Service = N'KommissionierNachtrag'
ORDER BY Id DESC;

-- 2) FAs mit Marker "Stueckliste kleiner als offene Kommissionierzeilen" (Leitstand-Badge)
SELECT po.OrderNumber, ps.StuecklisteKleiner, ps.ModifiedAt, ps.ModifiedBy
FROM dbo.ProductionOrderPickingStatus ps
JOIN dbo.ProductionOrders po ON po.Id = ps.ProductionOrderId
WHERE ps.StuecklisteKleiner IS NOT NULL
ORDER BY po.OrderNumber;
```

- **Zweck:** Lief der Nachtrag (Startzeile „Run gestartet“, Endzeile mit Zählern `fa_geprueft`, `auftraege_angelegt`, `fehler`,
  `altbestand_pruefen`, `stueckliste_kleiner`)? Welche FAs tragen den Marker?
- **Erwartet:** je Tag ein Lauf ab `Sync:KommissionierNachtragUhrzeit` (Default 2); `fehler` = 0. Den Marker schreibt nur der Lauf bzw.
  jedes Erzeugen (Freigabe, Admin-Knopf, „Altbestand geprüft“); `ModifiedBy` = `KommissionierNachtrag` beim Nachtlauf.
- **Quelle:** `IDEALAKEWMSService/Services/KommissionierNachtragService.cs`; Spalte SQL/168; TS-191.

---

## 7. Rollen, Benutzer, Stammdaten-Seeds

Tabellen `dbo.Roles` (`SQL/00_FreshInstall.sql:997-1011`), `dbo.UserRoles` (`:1022-1035`), `dbo.Users` (`:24-50`).

### A-7.1 Fehlen Rollen ohne Seed?

```sql
SELECT v.k AS RollenKey, CASE WHEN r.Id IS NULL THEN N'fehlt' ELSE N'vorhanden' END AS Stand
FROM (VALUES (N'beschichtungsauftrag'), (N'beschichtungsauftrag_schreiben'), (N'einkauf'), (N'schachtler')) v(k)
LEFT JOIN dbo.Roles r ON r.[Key] = v.k;
```

- **Erwartet:** alle `vorhanden` (das Update-Skript legt fehlende nach Bestätigung an; Zuweisen ist Hand-Schritt unter `/Roles`).
- **Quelle:** `scripts/deploy/nachlauf.psd1:180-197`.

### A-7.2 Benutzer je Rolle

```sql
SELECT r.[Key], r.[Name], COUNT(ur.Id) AS Benutzer
FROM dbo.Roles r
LEFT JOIN dbo.UserRoles ur ON ur.RoleId = r.Id
GROUP BY r.[Key], r.[Name], r.SortOrder
ORDER BY r.SortOrder;
```

- **Zweck:** Wer hat welche Rolle (Vergleich mit `/Users/RoleOverview`).
- **Erwartet:** Rollen mit Benutzern wie vereinbart; akenet01 hatte am 20.10.2026 17 Rollen (Stand v1.30.1).
- **Quelle:** [[2026-10-20-ake-testsystem-bestandsaufnahme]] 2.4; Rollenkonzept [09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md).

### A-7.3 Benutzer zu einem Windows-Konto (SSO-Fehlersuche)

```sql
SELECT u.Id, u.[Name], u.WindowsUserName, u.IsActive,
       STRING_AGG(r.[Key], N', ') AS Rollen
FROM dbo.Users u
LEFT JOIN dbo.UserRoles ur ON ur.UserId = u.Id
LEFT JOIN dbo.Roles r ON r.Id = ur.RoleId
WHERE u.WindowsUserName LIKE N'%<sam-account>%'
GROUP BY u.Id, u.[Name], u.WindowsUserName, u.IsActive;
```

- **Zweck:** Hat der Windows-Benutzer einen aktiven Datensatz? Ohne Datensatz kommt das Anmeldeformular (TS-40.2).
- **Erwartet:** genau eine aktive Zeile. `STRING_AGG` braucht SQL Server 2017+ (vgl. A-1.4).
- **Quelle:** `README.md:355` (AD-Benutzer haben `WindowsUserName` (SAM)); `docs/TESTSZENARIEN.md` TS-40.1/40.2. **zu prüfen:** ob
  `WindowsUserName` mit oder ohne Domänen-Präfix gespeichert ist – deshalb `LIKE`.

### A-7.4 DUMMY-Artikel und Lagerplatz-Codes mit Komma

```sql
SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Articles WHERE ArticleNumber = N'DUMMY') THEN 0 ELSE 1 END AS DummyFehlt;

SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N'QrTrennzeichen' AND [Value] = N',')
            THEN (SELECT COUNT(*) FROM dbo.StorageLocations WHERE Code LIKE N'%,%') ELSE 0 END AS KommaKonflikte;
```

- **Erwartet:** beide 0. `DummyFehlt = 1` → Lagerbestellung lehnt unbekannte EK-Nummern ab; `KommaKonflikte > 0` → QR-Trennzeichen ändern.
- **Quelle:** `scripts/deploy/nachlauf.psd1:198-202,223-226`.

---

### A-7.5 Anmeldestand je Benutzer: ohne Kennwort, offenes Erstkennwort (ab v1.99.28)

```sql
SELECT u.[Name],
       CASE WHEN u.WindowsUserName IS NULL THEN N'Lokal' ELSE N'AD' END AS Typ,
       CASE WHEN u.PasswordHash IS NULL OR u.PasswordHash = N'' THEN N'nein' ELSE N'ja' END AS HatKennwort,
       u.MussKennwortAendern
FROM dbo.Users u
WHERE u.IsActive = 1
  AND (u.PasswordHash IS NULL OR u.PasswordHash = N'' OR u.MussKennwortAendern = 1)
ORDER BY u.[Name];
```

- **Zweck:** Wer kommt über das Formular nicht hinein (`HatKennwort = nein`), wer hat ein offenes Erstkennwort (`ja` + `MussKennwortAendern = 1`).
  Der Hash selbst wird nicht ausgegeben.
- **Erwartet:** `Lokal`/`nein` nur bei bewusst gesperrten Konten; `AD`/`nein` normal (nur Windows-Anmeldung). `nein` mit `MussKennwortAendern = 1`
  sollte seit der Nachbesserung B-2 (v1.99.29) nicht mehr neu entstehen; Altfälle stammen aus v1.99.28 und sind wirkungslos (Login meldet Regel R).
- **Quelle:** `IdealAkeWms/Controllers/UsersController.cs:80-84` (gleiche Regeln wie der Hinweis in `/Users`), Prüfung
  `Pruefung/BenutzerOhneKennwort` in `scripts/deploy/nachlauf.psd1`; Anwender-Anleitung `docs/anwender/13-anmeldung-und-benutzer.md`.

---

## 8. Neu seit v1.99.30: Sage-Arbeitsgänge, Blechfertigung, FA-Struktur, Benutzerfelder (IDEAL)

Alle Abfragen nur lesend gegen die **WMS**-Datenbank; nichts davon fragt Sage ab.

### A-8.1 Arbeitsgang-Anlage in Sage-FAs: Stand der Warteschlange (ab v1.99.38)

```sql
SELECT [Status],
       CASE [Status] WHEN 1 THEN N'Offen' WHEN 2 THEN N'Gesendet' WHEN 3 THEN N'Angelegt'
                     WHEN 4 THEN N'In Sage vorhanden' WHEN 5 THEN N'Fehler' END AS StatusText,
       COUNT(*) AS Anzahl
FROM dbo.SageFaArbeitsgangAnlagen
GROUP BY [Status];

SELECT TOP 50 Id, HauptFa, BelId, Kuerzel, SageArbeitsgangnummer, [Status], Versuche, Fehler, ModifiedAt
FROM dbo.SageFaArbeitsgangAnlagen
WHERE [Status] IN (2, 5)
ORDER BY ModifiedAt DESC;
```

- **Zweck:** Wie viele Arbeitsgänge warten, hängen oder sind gescheitert; Fehlertext je Eintrag.
- **Erwartet:** Solange `SageFaArbeitsgaengeAnlegenAktiv` aus ist, höchstens `Offen`. Fehler mit Text „Antwort unklar …“ werden **nie**
  automatisch wiederholt: in Sage prüfen, dann in der Oberfläche „In Sage vorhanden – bestätigen“ oder erneut einreihen (nicht per SQL).
- **Quelle:** Tabelle `SQL/160_AddSageFaArbeitsgangAnlagen.sql:24-43`, `SQL/00_FreshInstall.sql:2969`; Status
  `IdealAkeWms/Models/SageFaArbeitsgangAnlage.cs:4-13`; Texte `IdealAkeWms/Services/SageFaArbeitsgaenge.cs:16-45`.

### A-8.2 BF-Aufträge: WiCAM-CSV-Status und Fehler (ab v1.99.35)

```sql
SELECT [Status], CsvStatus,
       CASE CsvStatus WHEN 0 THEN N'Keine' WHEN 1 THEN N'Offen' WHEN 2 THEN N'Geschrieben' WHEN 3 THEN N'Fehler' END AS CsvText,
       COUNT(*) AS Anzahl
FROM dbo.BfAuftraege
GROUP BY [Status], CsvStatus;

SELECT TOP 50 Id, Nummer, [Status], BestellerKuerzel, CsvVersuche, CsvFehler, CsvDatei, CsvGeschriebenAm, ModifiedAt
FROM dbo.BfAuftraege
WHERE CsvStatus = 3
ORDER BY ModifiedAt DESC;

-- Freigegebene/offene Aufträge mit Positionen ohne Arbeitsschritte (SQL/163 trägt nur nach, was das Artikel-Merkmal hergibt)
SELECT a.Nummer, p.Position, p.ArticleNumber, p.Matchcode
FROM dbo.BfAuftraege a
JOIN dbo.BfAuftragPositionen p ON p.BfAuftragId = a.Id
WHERE a.[Status] IN (1, 2) AND p.Gestrichen = 0 AND LTRIM(RTRIM(ISNULL(p.Arbeitsschritte, N''))) = N''
ORDER BY a.Nummer, p.Position;
```

- **Zweck:** Wurden die Pool-Dateien geschrieben? Fehlertext (Recht, Netz, „Datei existiert bereits im Pool – nicht überschrieben.“).
- **Erwartet:** keine Zeilen mit `CsvStatus = 3`. Status: 0 Entwurf, 1 Bestellt, 2 Freigegeben, 3 Fertig, 4 Storniert. Leere Arbeitsschritte sind
  erlaubt (leere Spalte in der CSV), aber für WiCAM ggf. nachzupflegen.
- **Quelle:** `SQL/158_AddBfAuftraege.sql:23-90`, `SQL/163_AddBfPositionArbeitsschritte.sql:22-27`, `SQL/00_FreshInstall.sql:2892,2935`;
  Status `IdealAkeWms/Models/BfAuftrag.cs:4-19`; Prüfungen `scripts/deploy/nachlauf.psd1:263-270`.

### A-8.3 BF-Besteller ohne Kürzel (ab v1.99.42)

```sql
SELECT DISTINCT u.Id, u.[Name], u.Kuerzel
FROM dbo.BfAuftraege a
JOIN dbo.Users u ON u.Id = a.BestellerUserId
WHERE a.[Status] IN (0, 1) AND LTRIM(RTRIM(ISNULL(u.Kuerzel, N''))) = N''
ORDER BY u.[Name];
```

- **Zweck:** Wessen offene BF-Aufträge sich nicht freigeben lassen, weil das Besteller-Kürzel (Spalte Besteller in der WiCAM-CSV) fehlt.
- **Erwartet:** keine Zeilen; sonst Kürzel in `/Users` pflegen (eindeutig, höchstens 10 Zeichen).
- **Quelle:** `SQL/162_AddUserKuerzelUndBfBestellerKuerzel.sql:23-37`; Manifest `scripts/deploy/nachlauf.psd1:119-120`.

### A-8.4 HauptFA mit Kommissionier-Positionen ohne Leitstand-Freigabe (ab v1.99.43)

```sql
SELECT n.HauptFA, COUNT(*) AS Positionen
FROM dbo.FaHierarchyNodes n
WHERE n.VerschwundenSeit IS NULL
  AND LTRIM(RTRIM(ISNULL(n.Kommissionieren, N''))) <> N''
  AND NOT EXISTS (SELECT 1 FROM dbo.ProductionOrders po
                  JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId = po.Id
                  WHERE po.OrderNumber = CAST(n.HauptFA AS nvarchar(50))
                    AND (po.SubOrderNumber = N'' OR po.SubOrderNumber = po.OrderNumber)
                    AND ps.IsReleasedForPicking = 1)
GROUP BY n.HauptFA
ORDER BY n.HauptFA;
```

- **Zweck:** Welche HauptFA fehlen in der Kommissionierliste, weil der Leitstand sie noch nicht freigegeben hat (gilt nur, wenn
  `FaHierarchyKommissionierlisteNurFreigegebene` an ist).
- **Erwartet:** nur HauptFA, die bewusst noch nicht freigegeben sind. Ist zusätzlich `LeitstandAktiv` aus, bleibt die Liste ganz leer
  (`Pruefung/KommissionierlisteLeitstandAus`).
- **Quelle:** aufgeschlüsselte Form von `Pruefung/KommissionierlisteNichtFreigegeben`, `scripts/deploy/nachlauf.psd1:142-149`.

### A-8.5 Verschwundene FA-Strukturen (ab v1.99.37)

```sql
SELECT HauptFA, VerschwundenGrund, COUNT(*) AS Zeilen, MIN(VerschwundenSeit) AS SeitFrühestens
FROM dbo.FaHierarchyNodes
WHERE VerschwundenSeit IS NOT NULL
GROUP BY HauptFA, VerschwundenGrund
ORDER BY MIN(VerschwundenSeit);
```

- **Zweck:** Zeilen, die Sage nicht mehr liefert und die das WMS gekennzeichnet behält (`beendet`, `inaktiv`, `unklar`).
- **Erwartet:** `unklar` nur vorübergehend (Mail „ungeklärt verschwundene Zeilen“ prüfen); ältere Zeilen als `Sync:FaStrukturAufbewahrungTage`
  dürfen nicht stehen bleiben, wenn deren WMS-Aufträge erledigt sind.
- **Quelle:** Spalten `SQL/159_FaStrukturAbgleich.sql:29-38`; Gründe `IdealAkeWms/Models/FaStrukturLebenslauf.cs:26-30`;
  [[0032-fa-struktur-abgleich-mit-lebenslauf-statt-full-replace]].

### A-8.6 Sage-Benutzerfelder: welche sind abweichend oder leer eingestellt? (ab v1.99.40)

```sql
SELECT [Category], [Key], [Value],
       CASE WHEN LTRIM(RTRIM(ISNULL([Value], N''))) = N'' THEN N'leer = nicht gelesen'
            WHEN [Value] <> SUBSTRING([Key], LEN([Key]) - CHARINDEX(N':', REVERSE([Key])) + 2, 200) THEN N'abweichend'
            ELSE N'Standard' END AS Bewertung
FROM dbo.ServiceSettings
WHERE [Key] LIKE N'Sync:Userfeld:%'
ORDER BY [Category], [Key];
```

- **Zweck:** Stand der 27 Feldnamen; der Standard ist der letzte Teil des Keys.
- **Erwartet:** AKE alles `Standard`. IDEAL: `Sync:Userfeld:Kreditoren:USER_EKMindestbestellwert` `leer` (Manifest
  `v1.99.40-Userfeld-EKMindestbestellwert-IDEAL`). Fehlt eine Zeile, gilt der Standard.
- **Quelle:** `IdealAkeWms/Services/SageSdata/SageUserfelder.cs:19,41-69`; [04 § 5.6](04-konfiguration.md).

### A-8.7 Lager-/Lagerplatz-Anlage in Sage: Warteschlange und Plätze (ab v1.99.71)

```sql
-- Stand der Warteschlange (Status 0 Offen, 1 Gesendet, 2 Bestaetigt, 3 Fehler)
SELECT EntityType, Status, COUNT(*) AS Anzahl, MAX(ModifiedAt) AS Zuletzt
FROM dbo.SageStammdatenOutbox
WHERE EntityType IN (N'StorageArea', N'StorageLocation')
GROUP BY EntityType, Status ORDER BY EntityType, Status;

-- Nicht bestaetigte Plaetze mit Grund (SageUebergabeStatus 1 Offen, 3 Fehler)
SELECT l.Id, l.Code, l.SageUebergabeStatus, l.SageUebergabeFehler, o.Status AS OutboxStatus, o.AttemptCount, o.LastError
FROM dbo.StorageLocations l
JOIN dbo.SageStammdatenOutbox o ON o.EntityType = N'StorageLocation' AND o.EntityKey = CAST(l.Id AS nvarchar(100))
WHERE o.Status <> 2 ORDER BY o.Id;

-- Welche Sage-DB hat der Dienst zuletzt beschrieben?
SELECT TOP 1 Message FROM dbo.SyncLogs WHERE Message LIKE N'Sage-Datenbank:%' ORDER BY Id DESC;
```

- **Erwartet:** Solange `SageLagerplatzAnlageAktiv` aus ist, nur `Offen`. „Letzter Versuch fehlgeschlagen: …“ = technischer Fehler, der Dienst
  versucht es selbst erneut; `Fehler` = fachlich: Grund lesen, in Sage bzw. im WMS korrigieren, dann „Erneut senden“.
- Prüfabfragen gegen Sage (P-1 bis P-10, nur `SELECT`): Spec `2026-11-24-lagerplatz-anlage-wms-nach-sage`, Abschnitt
  „Migrations-/SQL-Auswirkungen“, und [05 § 4b](05-schnittstellen.md).
- Seit v1.99.79 steht bei einem Konflikt die belegende Sage-`PlatzID` im Fehlertext (`SageUebergabeFehler`); diesen Platz in Sage ansehen
  (nur lesen), bevor „Erneut senden“ gedrückt wird.

---

### A-8.8 BDE-Teilmeldungen: Teilfertig und Fertig (ab v1.99.75, IDEAL)

```sql
-- Meldungen eines Haupt-FA: IstAbschliessend 1 = Fertig (schliesst die Zeile), 0 = Teilfertig (Zeile bleibt offen)
SELECT m.Id, m.HauptFA, m.SubFA, m.Position, m.Matchcode, m.Kuerzel, m.GoodQuantity, m.ScrapQuantity, m.SollmengeBeiMeldung,
       m.IstAbschliessend, m.IsCancelled, m.CancellationReason, m.GemeldetAm, m.WarehouseRequisitionItemId
FROM dbo.BdeTeilMeldungen m
WHERE m.HauptFA = <HauptFA>
ORDER BY m.SubFA, m.Position, m.GemeldetAm;

-- Gemeldet X von Soll je Zeile (nur gueltige Meldungen)
SELECT m.HauptFA, m.SubFA, m.Position, m.Kuerzel, SUM(m.GoodQuantity) AS GutGesamt, MAX(m.SollmengeBeiMeldung) AS Soll,
       MAX(CAST(m.IstAbschliessend AS int)) AS Abgeschlossen
FROM dbo.BdeTeilMeldungen m
WHERE m.IsCancelled = 0 AND m.HauptFA = <HauptFA>
GROUP BY m.HauptFA, m.SubFA, m.Position, m.Kuerzel;
```

- **Erwartet:** je Zeile beliebig viele Teilfertig-Meldungen, aber höchstens **eine** nicht stornierte Fertig-Meldung (UNIQUE-Index mit
  `[IstAbschliessend] = 1`, SQL/169). Vor v1.99.75 angelegte Meldungen stehen auf `IstAbschliessend = 1`.
- **Quelle:** `IdealAkeWms/Models/BdeTeilMeldung.cs:14-65`, `SQL/169_AddBdeTeilMeldungIstAbschliessendUndStandardsicht.sql`,
  [[0037-teilfertigmeldung-nur-abschliessende-meldung-eindeutig]].

---

### A-8.9 BDE: Terminal-Zuordnung je WMS-Benutzer (ab v1.99.78)

```sql
-- Wer hat welches Terminal mit welcher Werkbank? (ohne Kennwort-Spalten)
SELECT u.Id AS UserId, u.Name, u.WindowsUserName, u.IsActive, t.Id AS TerminalId, t.Description, w.Name AS Werkbank, w.BdeAktiv
FROM dbo.Users u
LEFT JOIN dbo.BdeTerminals t ON t.UserId = u.Id
LEFT JOIN dbo.ProductionWorkplaces w ON w.Id = t.DefaultProductionWorkplaceId
WHERE u.IsActive = 1
ORDER BY CASE WHEN t.Id IS NULL THEN 1 ELSE 0 END, u.Name;
```

- **Zweck:** „Für diesen Benutzer ist kein Terminal konfiguriert“ (Benutzer ohne Zeile in `BdeTerminals`) und seit v1.99.79 „Dieses Terminal
  ist nicht dem angemeldeten Benutzer zugeordnet – Seite neu laden.“ (die Seite schickt ein anderes Terminal als das des angemeldeten
  Benutzers, z. B. nach Kontowechsel im selben Browser) eingrenzen. Werkbank mit `BdeAktiv = 0` → die Seite zeigt „Werkbank ist nicht BDE-aktiv“.
- **Quelle:** `IdealAkeWms/Controllers/BdeTerminalController.cs:39-50,273-280`, `IdealAkeWms/Controllers/BdeTeilelisteController.cs:97-104`.

## Verwandte Kapitel

[00-index.md](00-index.md) · [01-architektur-technologie.md](01-architektur-technologie.md) · [02-systeme-umgebungen.md](02-systeme-umgebungen.md) ·
[03-installation-update-rollback.md](03-installation-update-rollback.md) · [04-konfiguration.md](04-konfiguration.md) ·
[05-schnittstellen.md](05-schnittstellen.md) · [06-logs-ueberwachung.md](06-logs-ueberwachung.md) · [07-troubleshooting.md](07-troubleshooting.md) ·
[09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md) · [10-backup-restore.md](10-backup-restore.md)
