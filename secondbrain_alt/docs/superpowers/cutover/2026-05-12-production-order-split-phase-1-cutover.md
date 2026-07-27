# ProductionOrder-Split — Phase 1 Cutover-Rehearsal-Checklist

**Datum:** 2026-05-12
**Branch:** `refactor/fa-logic` (Merge nach `main` vor Cutover)
**Ziel-Version:** `v1.11.0` (Phase 1) → spaeter `v1.12.0` (Phase 2, separate Auslieferung moeglich)
**Phase-1 Referenzen:**
- Roadmap: [docs/superpowers/specs/2026-05-12-production-order-split-roadmap.md](../specs/2026-05-12-production-order-split-roadmap.md)
- Design: [docs/superpowers/specs/2026-05-12-production-order-split-phase-1-design.md](../specs/2026-05-12-production-order-split-phase-1-design.md)
- Plan: [docs/superpowers/plans/2026-05-12-production-order-split-phase-1.md](../plans/2026-05-12-production-order-split-phase-1.md) (Task 10)

**Status:** Vorbereitet — wird am Cutover-Tag vom Operator manuell durchgearbeitet.

---

## 1. Zweck

Dieser Plan ist die **Operator-Checkliste** fuer das Big-Bang-Wartungsfenster, in dem die `ProductionOrders`-Tabelle in 5 neue Tabellen aufgeteilt wird. Die Checkliste ersetzt keine der inhaltlichen Specs — sie ist die ablauforientierte Zusammenfassung fuer den Cutover-Tag.

**Geschaetzte Wartungsfenster-Dauer:**

| FA-Bestand | Migration | Deploy + Smoke | Gesamt |
|---|---|---|---|
| < 10.000 | ~10 min | ~10 min | **~20 min** |
| 10.000 – 50.000 | ~30 min | ~15 min | **~45 min** |
| > 50.000 | ~45 min | ~15 min | **~60 min** |

**Backup-Restore-Fenster:** 30 Minuten nach App-Start (Step 8). Danach ausschliesslich Forward-Fix-Strategie.

---

## 2. Vorbedingungen (T-48h bis T-1d)

Alle Punkte muessen abgehakt sein, bevor das Wartungsfenster startet.

- [ ] **`refactor/fa-logic` in `main` gemerged** und auf den Web-/Service-Build-Server deployed.
- [ ] **Build + Tests gruen:** `dotnet build --nologo && dotnet test --nologo --no-build --filter "Category!=SqlServerOnly"` → 0 Fehler.
- [ ] **Stage-Probelauf erfolgreich:**
  - Stage-DB ist Kopie der Produktiv-DB (aktueller Stand).
  - `SQL/60_ProductionOrderSplit.sql` auf Stage gefahren → Verifikations-Counts stimmen.
  - Smoke-Test (Step 8 dieser Checkliste) auf Stage erfolgreich.
  - Backup-Restore-Probe auf Stage erfolgreich (Migrations-Rollback einmal durchgespielt).
- [ ] **Wartungsfenster kommuniziert:** Mail an Picking-/Tracking-/Leitstand-/BDE-User-Gruppe (T-48h und T-2h). Inhalt: Datum, Uhrzeit, voraussichtliche Dauer, Grund "DB-Refactor v1.11.0".
- [ ] **DBA verfuegbar** waehrend des Wartungsfensters (Migration laeuft via SSMS, Pfad B aus Spec §8).
- [ ] **Backup-Ablage gepruefte Kapazitaet** (`D:\Backups\` mind. 2× DB-Groesse frei).
- [ ] **Stage-Performance-Check:** FA-Liste mit >500 FAs laedt in < 1 s (Risiko 12.4 aus Spec).
- [ ] **Rollback-Skript bereitgelegt** (siehe §10). Pfad: `SQL/60_ProductionOrderSplit.sql` + Backup-Datei aus Step 3.

---

## 3. Cutover-Ablauf (T0 → T+~50min)

### Step 1 — Wartungsfenster eroeffnen (T0)

- [ ] Banner-/Maintenance-Page aktivieren (falls vorhanden) oder kurze Slack-/Mail-Info "App-Stop in 5 Minuten".

### Step 2 — App stoppen (T0)

- [ ] In IIS Manager: Site `IdealAkeWms` → **Stop**.
- [ ] Windows Service: `IDEALAKEWMSService` → **Stop**.
- [ ] **Verify:** keine aktiven App-Connections.

```sql
SELECT session_id, login_name, host_name, program_name, status
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID('IDEAL_AKE_WMS')
  AND program_name LIKE '%IdealAkeWms%';
-- Erwartet: 0 Zeilen
```

### Step 3 — Vollstaendiges DB-Backup (T+~2min)

- [ ] Backup ausfuehren:

```sql
BACKUP DATABASE [IDEAL_AKE_WMS]
TO DISK = 'D:\Backups\IDEAL_AKE_WMS_v1.10_pre-split.bak'
WITH COMPRESSION, INIT, NAME = 'Pre v1.11 ProductionOrder Split';
```

- [ ] Backup-Dauer notieren: __________ min
- [ ] Backup-Dateigroesse notieren: __________ MB
- [ ] **Verify:** Datei existiert und ist groesser als DB-Datenvolumen × 0.3 (mit Compression).

### Step 4 — SQL Agent Jobs deaktivieren (T+~5min)

- [ ] In SSMS → SQL Server Agent → Jobs:
  - `01_Import_Produktionsauftraege` → Disable
  - `02_Import_Artikel` → Disable
- [ ] **Verify:** kein Job-History-Eintrag in den naechsten 2 Minuten.

```sql
SELECT name, enabled FROM msdb.dbo.sysjobs
WHERE name IN ('01_Import_Produktionsauftraege', '02_Import_Artikel');
-- Erwartet: enabled = 0 fuer beide
```

### Step 5 — Migration ausfuehren (T+~10min, Pfad B empfohlen)

**Pfad B (Produktiv-Empfehlung):** Skript manuell vor App-Start in SSMS ausfuehren. DBA hat volle Kontrolle, PRINTs sind live sichtbar.

- [ ] In SSMS, gegen `IDEAL_AKE_WMS`:

```sql
USE [IDEAL_AKE_WMS];
:r C:\path\to\SQL\60_ProductionOrderSplit.sql
```

- [ ] Skript-Laufzeit notieren: __________ min
- [ ] **PRINT-Output pruefen:** alle Sektionen melden Erfolg, kein FAILED, kein ROLLBACK.

**Pfad A (Dev/Test, kleine DB):** Migration laeuft automatisch beim App-Start (Step 8). Nur fuer DBs < 10k FAs empfohlen.

### Step 6 — Verifikations-Counts (T+~30min)

- [ ] In SSMS:

```sql
DECLARE @PO INT, @PS INT, @BDE INT, @GROUPS INT;
SELECT @PO = COUNT(*) FROM dbo.ProductionOrders;
SELECT @PS = COUNT(*) FROM dbo.ProductionOrderPickingStatus;
SELECT @BDE = COUNT(*) FROM dbo.ProductionOrderBdeStatus;
SELECT @GROUPS = COUNT(*) FROM dbo.ProductionOrderAssemblyGroups;

PRINT 'ProductionOrders:               ' + CAST(@PO AS VARCHAR);
PRINT 'ProductionOrderPickingStatus:   ' + CAST(@PS AS VARCHAR) + ' (erwartet: ' + CAST(@PO AS VARCHAR) + ')';
PRINT 'ProductionOrderBdeStatus:       ' + CAST(@BDE AS VARCHAR) + ' (erwartet: ' + CAST(@PO AS VARCHAR) + ')';
PRINT 'ProductionOrderAssemblyGroups:  ' + CAST(@GROUPS AS VARCHAR) + ' (erwartet: ' + CAST(@PO * 5 AS VARCHAR) + ')';

IF @PS <> @PO OR @BDE <> @PO OR @GROUPS <> @PO * 5
    PRINT '*** MISMATCH — STOP, ROLLBACK noetig ***';
ELSE
    PRINT 'Counts OK — weiter zu Step 7';
```

- [ ] PS == PO ✅
- [ ] BDE == PO ✅
- [ ] Groups == 5 × PO ✅

**Bei Mismatch:** STOP. Springe zu §10 Rollback.

- [ ] Optionale Stichprobe — pruefe alte vs. neue Werte fuer 3 zufaellige FAs:

```sql
-- Beispiel-FA: vor Migration HasGlass=1, HasCoatingParts=1, IsCoatingDone=0
DECLARE @testId INT = (SELECT TOP 1 Id FROM dbo.ProductionOrders ORDER BY NEWID());
SELECT
    @testId AS PO_Id,
    (SELECT HasGlass FROM dbo.ProductionOrderPickingStatus WHERE ProductionOrderId = @testId) AS HasGlass_New,
    (SELECT HasCoatingParts FROM dbo.ProductionOrderPickingStatus WHERE ProductionOrderId = @testId) AS HasCoatingParts_New,
    (SELECT COUNT(*) FROM dbo.ProductionOrderAssemblyGroups WHERE ProductionOrderId = @testId) AS GroupCount_5_expected;
```

### Step 7 — Neue App-Version deployen (T+~35min)

- [ ] `dotnet publish` Output auf IIS-Host kopieren → IIS-Site-Verzeichnis.
- [ ] Neue `IDEALAKEWMSService.exe` auf Service-Host kopieren.
- [ ] Neuen `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` auf Job-Host kopieren **ODER** Job-Step-Body in SSMS aktualisieren.
- [ ] Verzeichnis-Rechte pruefen (App-Pool-Identity hat Lese-Zugriff auf `wwwroot/`).

### Step 8 — App-Start + Smoke-Test (T+~40min)

- [ ] IIS-Site `IdealAkeWms` → **Start**.
- [ ] Windows Service `IDEALAKEWMSService` → **Start**.
- [ ] Browser → App-URL → Login als Admin.
- [ ] **Verify Migrate()-Pfad:**
  - Pfad B (Skript manuell gelaufen): App-Start in < 10 s.
  - Pfad A: App-Start kann je nach DB-Groesse 30 s – 5 min dauern.

**Smoke-Test-Sequenz (10 Schritte):**

- [ ] (1) FA-Liste laedt in < 2 s.
- [ ] (2) 1× FA freigeben (Toggle "Freigegeben") → SSMS:
  ```sql
  SELECT IsReleasedForPicking, ReleasedAt, ReleasedBy
  FROM dbo.ProductionOrderPickingStatus
  WHERE ProductionOrderId = <ID>;
  -- Erwartet: IsReleasedForPicking = 1, ReleasedAt/By gesetzt
  ```
- [ ] (3) 1× **HasGlass**-Toggle (Picking-Endpoint).
- [ ] (4) 1× **IsCoatingDone**-Toggle (Picking-Endpoint).
- [ ] (5) 1× **IsDonePicking**-Toggle (Picking-Endpoint).
- [ ] (6) 1× **VK**-Toggle → SSMS:
  ```sql
  SELECT GroupKey, IsApplicable FROM dbo.ProductionOrderAssemblyGroups
  WHERE ProductionOrderId = <ID> AND GroupKey = 'VK';
  -- Erwartet: IsApplicable = 1
  ```
- [ ] (7) 1× je **VL / VE / VT / VA**-Toggle.
- [ ] (8) 1× **IsDoneBde**-Toggle (falls UI vorhanden — sonst skip in Phase 1).
- [ ] (9) 1× **Picking-Status setzen** ueber `PickingController.SetPickingStatus` → SSMS:
  ```sql
  SELECT PickingStatus FROM dbo.ProductionOrderPickingStatus WHERE ProductionOrderId = <ID>;
  ```
- [ ] (10) 1× **Stueckliste** zu beliebiger FA oeffnen → laedt ohne Fehler.

**Bei Fehler in (1)–(10):** STOP. Pruefe Browser-DevTools-Network-Tab (404 oder 500?). Bei 500: Application-Log unter `logs/idealakewms-YYYYMMDD.log` lesen. Bei nicht-behebbarem Bug innerhalb 30 min → §10 Rollback.

### Step 9 — AgentJob-Smoke-Test (T+~50min, Round-4-Pflicht)

**Wichtig:** Dieser Test laeuft **vor** Reaktivierung des Schedulers. Wenn der Folge-MERGE im AgentJob defekt ist, faellt es hier auf — und Backup-Restore ist noch moeglich.

- [ ] In Sage einen Test-FA anlegen (oder existierenden WA als "neu" markieren). Notiere Order-Number: __________
- [ ] In SSMS gegen `IDEAL_AKE_WMS` den Job-Body manuell ausfuehren:

```sql
:r C:\path\to\SQL\AgentJobs\01_Import_Produktionsauftraege.sql
```

- [ ] Verifikation:

```sql
DECLARE @testOrderNumber NVARCHAR(100) = '<ORDER_NUMBER>';
DECLARE @poId INT = (SELECT Id FROM dbo.ProductionOrders WHERE OrderNumber = @testOrderNumber);

SELECT
    @poId AS PO_Id,
    (SELECT COUNT(*) FROM dbo.ProductionOrderPickingStatus WHERE ProductionOrderId = @poId) AS PS_Count,
    (SELECT COUNT(*) FROM dbo.ProductionOrderBdeStatus    WHERE ProductionOrderId = @poId) AS BDE_Count,
    (SELECT COUNT(*) FROM dbo.ProductionOrderAssemblyGroups WHERE ProductionOrderId = @poId) AS Groups_Count;
-- Erwartet: PS=1, BDE=1, Groups=5
```

- [ ] PS=1 ✅
- [ ] BDE=1 ✅
- [ ] Groups=5 ✅

**Bei Mismatch:** Job-Skript reparieren, Re-Run. **Erst wenn 1/1/5 gruen → weiter zu Step 10.**

- [ ] Optional: AgentJob 2× hintereinander manuell ausfuehren → keine Duplikate (Idempotenz-Check).

### Step 10 — Agent Jobs reaktivieren (T+~55min)

- [ ] In SSMS:
  - `01_Import_Produktionsauftraege` → Enable
  - `02_Import_Artikel` → Enable
- [ ] **Verify:** Job laeuft beim naechsten Schedule-Tick (Default 15 min).

```sql
SELECT name, enabled, date_modified FROM msdb.dbo.sysjobs
WHERE name IN ('01_Import_Produktionsauftraege', '02_Import_Artikel');
-- Erwartet: enabled = 1
```

**Service-Worker-Ordering (Spec 12.12):** Der `CoatingDetectionService` (SyncIntervalMinutes=15) startet automatisch beim App-Start. Er schreibt in `ProductionOrderPickingStatus.HasCoatingParts`. Da Step 5 alle bestehenden FAs mit `PickingStatus`-Zeilen versehen hat und Step 9 die Folge-MERGE-Logik fuer neue FAs verifiziert hat, ist keine Race-Condition zu erwarten.

### Step 11 — Wartungsfenster schliessen (T+~60min)

- [ ] User informieren: System verfuegbar. Format:

> Wartung beendet. v1.11.0 ist live. Bei Auffaelligkeiten bitte sofort melden an [Operator]. Backup-Restore-Fenster endet um __:__ Uhr (T+30min nach App-Start).

- [ ] Banner / Maintenance-Page deaktivieren.
- [ ] **Backup-Restore-Window-Deadline notieren:** __________

---

## 4. Post-Cutover-Monitoring (T+30min bis T+5d)

### T+30min: Restore-Window-Deadline

- [ ] Erste echte User-Buchungen sind eingelaufen? (Quick-Check in App-Log)
- [ ] Keine `NullReferenceException` oder `InvalidOperationException` in den letzten 30 Minuten?
- [ ] Picking-/BDE-User berichten keine Anomalien?

**Ab hier: Rollback via Backup-Restore nicht mehr ohne Daten-Verlust moeglich. Forward-Fix-Only.**

### T+1d (Tag 1)

- [ ] Daily-Health-Check: alle 4 Tabellen-Counts wachsen plausibel?

```sql
SELECT 'ProductionOrders' AS T, COUNT(*) AS N FROM dbo.ProductionOrders
UNION ALL SELECT 'PickingStatus', COUNT(*) FROM dbo.ProductionOrderPickingStatus
UNION ALL SELECT 'BdeStatus', COUNT(*) FROM dbo.ProductionOrderBdeStatus
UNION ALL SELECT 'AssemblyGroups', COUNT(*) FROM dbo.ProductionOrderAssemblyGroups;
```

- [ ] Invariante: `PickingStatus.Count == BdeStatus.Count == ProductionOrders.Count`. `AssemblyGroups.Count == 5 × ProductionOrders.Count`.
- [ ] Stichprobe: 3 neue FAs aus Sage-Import pruefen → Status-Zeilen vorhanden?

### T+5d (Live-Verifikation laut Roadmap §13.3)

- [ ] Keine `NullReferenceException` auf `PickingStatus`/`BdeStatus`-Lookups im 5-Tages-Log.
- [ ] AgentJob hat keine Luecken in den 3 Status-Tabellen (Counts stimmen).
- [ ] Performance der FA-Liste vergleichbar mit pre-Refactor (subjektiv durch User, ggf. Performance-Profil).
- [ ] Picker-/BDE-/Leitstand-User haben keine offenen Anomalien gemeldet.

**Bei OK:** Phase-1 abgeschlossen. Phase-2-Cutover separat planen (View-Split ist bereits Code-fertig in v1.12.0, aber separate Aktivierung mit eigenem Smoke-Test sinnvoll).

---

## 5. Smoke-Test-Matrix (Step 8 detailliert)

| # | Aktion | Erwartetes Verhalten | DB-Pruefung |
|---|---|---|---|
| 1 | FA-Liste oeffnen | Laedt < 2 s, alle 5 Baugruppen-Checkboxen (VK/VL/VE/VT/VA) sichtbar | — |
| 2 | FA freigeben | UI zeigt "Freigegeben" mit Datum | `ProductionOrderPickingStatus.IsReleasedForPicking = 1` |
| 3 | HasGlass-Toggle | Checkbox togglet ohne Fehler | `ProductionOrderPickingStatus.HasGlass` flips |
| 4 | IsCoatingDone-Toggle | Checkbox togglet | `ProductionOrderPickingStatus.IsCoatingDone` flips |
| 5 | IsDonePicking-Toggle | Checkbox togglet | `ProductionOrderPickingStatus.IsDonePicking` flips |
| 6–7 | VK/VL/VE/VT/VA-Toggles | Je Checkbox togglet | `ProductionOrderAssemblyGroups.IsApplicable` flips fuer den passenden `GroupKey` |
| 8 | IsDoneBde-Toggle | Falls UI sichtbar | `ProductionOrderBdeStatus.IsDoneBde` flips |
| 9 | Picking-Status setzen | Status-Wechsel via PickingController | `ProductionOrderPickingStatus.PickingStatus` aktualisiert |
| 10 | Stueckliste oeffnen | BOM laedt ohne Fehler | — |

**Routing-Verifikation in DevTools-Network-Tab:**
- HasGlass / HasExternalPurchase / IsCoatingDone / IsDonePicking → `POST /api/picking-status/toggle`
- VK / VL / VE / VT / VA → `POST /api/assembly-groups/toggle-applicable`
- IsDoneBde → `POST /api/bde-status/toggle`

---

## 6. Rollback-Strategie

### 6.1 Waehrend Migration (Step 5, vor App-Start)

- [ ] Skript-Lauf abbrechen (Ctrl+Break in SSMS).
- [ ] Verifikation: alte Spalten in `ProductionOrders` noch vorhanden? Wenn ja → Section A des Skripts ist nicht durchgelaufen, **kein Rollback noetig**, einfach App-Stop bleiben und Bug debuggen.
- [ ] Wenn alte Spalten bereits gedroppt: **Backup-Restore** aus Step 3.

```sql
RESTORE DATABASE [IDEAL_AKE_WMS]
FROM DISK = 'D:\Backups\IDEAL_AKE_WMS_v1.10_pre-split.bak'
WITH REPLACE, RECOVERY;
```

- [ ] AgentJobs reaktivieren (Step 10 mit alter App-Version-Konfiguration).
- [ ] Alte App-Version weiter laufen lassen.

### 6.2 Nach App-Start, innerhalb 30 min Restore-Fenster

- [ ] App-Stop (Step 2).
- [ ] **Backup-Restore** aus Step 3 (siehe 6.1).
- [ ] Alte App-Version (v1.10.0-Build) re-deployen.
- [ ] AgentJobs reaktivieren mit alter Job-Skript-Version.
- [ ] App-Start. Smoke-Test mit alter Version.

### 6.3 Nach 30 min Live-Betrieb

- **Kein Rollback ohne Daten-Verlust** mehr (User haben Buchungen in neuen Tabellen gemacht).
- **Forward-Fix-Strategie:**
  - Bug in App-Code → Hotfix-Build + Restart.
  - Daten-Inkonsistenz → Ad-hoc SQL-Reparatur-Skript.
  - Schema-Defekt → Folge-Migration als Hotfix.
- **Eskalation:** [Tech-Lead] + [DBA] sofort kontaktieren.

---

## 7. Notfall-Kontakte

| Rolle | Name | Erreichbarkeit |
|---|---|---|
| Operator (Cutover-Lead) | ________________ | ________________ |
| DBA (SSMS-Migration) | ________________ | ________________ |
| Tech-Lead (Code-Hotfix) | ________________ | ________________ |
| Stakeholder (Picking) | ________________ | ________________ |
| Stakeholder (BDE) | ________________ | ________________ |

---

## 8. Sign-off

Cutover offiziell abgeschlossen, wenn alle Punkte gruen sind:

- [ ] Step 8 Smoke-Test alle 10 Punkte gruen.
- [ ] Step 9 AgentJob-Smoke 1/1/5 gruen.
- [ ] T+30min keine Anomalien.
- [ ] T+1d Daily-Check gruen.
- [ ] T+5d Live-Verifikation gruen.

**Cutover-Datum:** __________
**Operator-Signatur:** __________
**DBA-Signatur:** __________
**Tech-Lead-Signatur:** __________

---

## Anhang A — Schnellreferenz Verifikations-Queries

Alle Counts und Invarianten als Copy-Paste-Block fuer SSMS:

```sql
-- Globaler Health-Check (jederzeit ausfuehrbar)
DECLARE @PO INT, @PS INT, @BDE INT, @GROUPS INT, @SPECS INT, @WPG INT;
SELECT @PO = COUNT(*) FROM dbo.ProductionOrders;
SELECT @PS = COUNT(*) FROM dbo.ProductionOrderPickingStatus;
SELECT @BDE = COUNT(*) FROM dbo.ProductionOrderBdeStatus;
SELECT @GROUPS = COUNT(*) FROM dbo.ProductionOrderAssemblyGroups;
SELECT @SPECS = COUNT(*) FROM dbo.ProductionOrderAssemblyGroupSpecs;
SELECT @WPG = COUNT(*) FROM dbo.ProductionWorkplaceAssemblyGroups;

PRINT '== Health Check =='
PRINT 'ProductionOrders:                    ' + CAST(@PO AS VARCHAR);
PRINT 'ProductionOrderPickingStatus:        ' + CAST(@PS AS VARCHAR) + IIF(@PS = @PO, ' OK', ' *** MISMATCH ***');
PRINT 'ProductionOrderBdeStatus:            ' + CAST(@BDE AS VARCHAR) + IIF(@BDE = @PO, ' OK', ' *** MISMATCH ***');
PRINT 'ProductionOrderAssemblyGroups:       ' + CAST(@GROUPS AS VARCHAR) + IIF(@GROUPS = @PO * 5, ' OK', ' *** MISMATCH ***');
PRINT 'ProductionOrderAssemblyGroupSpecs:   ' + CAST(@SPECS AS VARCHAR) + ' (Phase 1: 0 erwartet, Phase 4: > 0)';
PRINT 'ProductionWorkplaceAssemblyGroups:   ' + CAST(@WPG AS VARCHAR) + ' (Phase 1: 0 erwartet, Phase 5: > 0)';
```

```sql
-- Waisen-Check: gibt es FAs ohne Status-Zeilen?
SELECT po.Id, po.OrderNumber, 'FEHLT PickingStatus' AS Problem
FROM dbo.ProductionOrders po
LEFT JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId = po.Id
WHERE ps.Id IS NULL
UNION ALL
SELECT po.Id, po.OrderNumber, 'FEHLT BdeStatus'
FROM dbo.ProductionOrders po
LEFT JOIN dbo.ProductionOrderBdeStatus bde ON bde.ProductionOrderId = po.Id
WHERE bde.Id IS NULL
UNION ALL
SELECT po.Id, po.OrderNumber, 'AssemblyGroups ' + CAST(COUNT(ag.Id) AS VARCHAR) + '/5'
FROM dbo.ProductionOrders po
LEFT JOIN dbo.ProductionOrderAssemblyGroups ag ON ag.ProductionOrderId = po.Id
GROUP BY po.Id, po.OrderNumber
HAVING COUNT(ag.Id) <> 5;
-- Erwartet: 0 Zeilen
```

```sql
-- Audit-Felder-Stichprobe (3 zufaellige FAs)
SELECT TOP 3
    po.Id, po.OrderNumber,
    ps.ModifiedAt AS PS_ModifiedAt, ps.ModifiedBy AS PS_ModifiedBy,
    bde.ModifiedAt AS BDE_ModifiedAt, bde.ModifiedBy AS BDE_ModifiedBy
FROM dbo.ProductionOrders po
INNER JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId = po.Id
INNER JOIN dbo.ProductionOrderBdeStatus bde ON bde.ProductionOrderId = po.Id
ORDER BY NEWID();
```

---

## Anhang B — Bekannte Risiken am Cutover-Tag

| # | Risiko | Mitigation |
|---|---|---|
| R1 | Migrations-Skript bricht in der Mitte ab | Idempotenz-Guards (`NOT EXISTS`) → Re-Run moeglich. Bei Verzweifelung: Backup-Restore. |
| R2 | Verifikations-Counts (Step 6) mismatchen | Skript-Output pruefen, fehlerhafte Section identifizieren. Wenn unklar: Backup-Restore. |
| R3 | AgentJob-Smoke (Step 9) liefert nicht 1/1/5 | Job-MERGE-Statements pruefen (Section "Folge-MERGE PickingStatus/BdeStatus/AssemblyGroups"). Forward-Fix vor Job-Reaktivierung. |
| R4 | Stale Browser-Tabs senden alte `/api/productionorders/toggle-field`-Requests → 404 | Bekannt (Spec 12.11), kein Show-Stopper. User wird beim naechsten Reload automatisch korrigiert. |
| R5 | `CoatingDetectionService` triggert vor AgentJob-Smoke | Reihenfolge in Step 10 erzwingt AgentJob-Reaktivierung erst nach Job-Smoke. SyncIntervalMinutes=15 puffert zusaetzlich. |
| R6 | FA-Liste laedt > 2 s nach Cutover | Index `IX_ProductionOrderAssemblyGroups_GroupKey_IsApplicable` pruefen. Falls fehlt: `CREATE INDEX` als Hotfix. |
| R7 | EF-`Migrate()`-Pfad-A laeuft beim App-Start (Pfad B war geplant), App-Start haengt | Akzeptabel — Skript ist idempotent, `Up()` findet bereits-applied Sections und skippt sie. App-Start dauert dann ein paar Sekunden laenger. |
