# FA-Vorbau (v1.22.0) — Cutover-Checklist

**Datum:** 2026-06-12
**Branch:** `feature/fa-vorbau` (Merge nach `main` vor Cutover)
**Ziel-Version:** `v1.22.0`
**Referenzen:**
- Spec: [secondbrain/docs/superpowers/specs/2026-06-12-fa-vervollstaendigung-erweiterung-design.md](../../../secondbrain/docs/superpowers/specs/2026-06-12-fa-vervollstaendigung-erweiterung-design.md)
- Plan: [secondbrain/docs/superpowers/plans/2026-06-12-fa-vervollstaendigung-erweiterung.md](../../../secondbrain/docs/superpowers/plans/2026-06-12-fa-vervollstaendigung-erweiterung.md) (Task 15)
- Muster: [2026-05-12-production-order-split-phase-1-cutover.md](2026-05-12-production-order-split-phase-1-cutover.md)

**Status:** Vorbereitet — wird am Cutover-Tag vom Operator manuell durchgearbeitet.

---

## 1. Zweck

Operator-Checkliste fuer das Wartungsfenster, in dem das fixe AssemblyGroups-Kachel-System
(VK/VL/VE/VT/VA) durch den Arbeitsgaenge-Katalog + FaWorkSteps + FA-Merkmale ersetzt wird.

**Drei Komponenten muessen ZEITGLEICH umgestellt werden:**

1. **SQL-Agent-Job** `01_Import_Produktionsauftraege` — der Folge-MERGE auf
   `ProductionOrderAssemblyGroups` wurde entfernt. Laeuft der alte Job-Body nach der
   Migration weiter, schlaegt er fehl (Tabelle existiert nicht mehr).
2. **Web-App** — Migration `20260612102225_FaWorkStepsAndAttributes` droppt die alten
   Tabellen (`ProductionOrderAssemblyGroups`, `ProductionOrderAssemblyGroupSpecs`,
   `ProductionWorkplaceAssemblyGroups`) und legt die 8 neuen an (daten-konvertierend).
3. **Windows-Service** — `SageImportService` enthaelt den AssemblyGroups-MERGE nicht mehr;
   neu dazu kommt der `FaWorkStepDetectionService`. Ein ALTER Service gegen das NEUE Schema
   wuerde beim Import-MERGE crashen → Service-Version muss zeitgleich mit der Web-App live gehen.

**WICHTIG — Rollback nur via DB-Restore:** Die Migration ist daten-destruktiv fuers
Alt-Schema (AssemblyGroups-Tabellen werden nach Konvertierung gedroppt). Ein `Down()`
stellt die alten Daten NICHT vollstaendig wieder her. Siehe §4.

---

## 2. Vorbedingungen (T-48h bis T-1d)

- [ ] `feature/fa-vorbau` in `main` gemerged, Build + Vollsuite gruen.
- [ ] Stage-Probelauf: Migration auf Kopie der Produktiv-DB gefahren, Smoke-Test (§3 Step 7) gruen.
- [ ] Wartungsfenster kommuniziert (fa_completion-/Leitstand-/Picking-User).
- [ ] Backup-Ablage hat Kapazitaet (mind. 2× DB-Groesse frei).
- [ ] Neues AgentJob-Skript `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` bereitgelegt.
- [ ] Suchstring-Liste fuer die Arbeitsgaenge vorbereitet (Fachbereich; Step 8).

---

## 3. Cutover-Ablauf (Wartungsfenster)

### Step 1 — DB-Backup `IDEAL_AKE_WMS`

- [ ] App-Verbindungen pruefen, dann Backup:

```sql
BACKUP DATABASE [IDEAL_AKE_WMS]
TO DISK = 'D:\Backups\IDEAL_AKE_WMS_v1.21_pre-fa-vorbau.bak'
WITH COMPRESSION, INIT, NAME = 'Pre v1.22 FA-Vorbau';
```

- [ ] **Verify:** Backup-Datei existiert, Groesse plausibel.

### Step 2 — SQL-Agent-Job pausieren + neues Skript einspielen

- [ ] SSMS → SQL Server Agent → `01_Import_Produktionsauftraege` → **Disable**.
- [ ] Job-Step-Body durch den neuen Stand von
      `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` ersetzen
      (Folge-MERGE 3 auf `ProductionOrderAssemblyGroups` ist entfernt).
- [ ] **Verify:**

```sql
SELECT name, enabled FROM msdb.dbo.sysjobs
WHERE name = '01_Import_Produktionsauftraege';
-- Erwartet: enabled = 0
```

### Step 3 — Windows-Service stoppen

- [ ] `IDEALAKEWMSService` → **Stop**.
- [ ] Grund: der alte Service-Build enthaelt noch den AssemblyGroups-MERGE im
      `SageImportService` — er darf NICHT gegen das neue Schema laufen.
      Die neue Service-Version (ohne MERGE, mit `FaWorkStepDetectionService`)
      geht in Step 5 zeitgleich live.

### Step 4 — Web-App-Deploy + Migration

- [ ] IIS-Site `IdealAkeWms` → **Stop**, neuen `dotnet publish`-Output einspielen.
- [ ] IIS-Site → **Start**. Migration `20260612102225_FaWorkStepsAndAttributes`
      laeuft automatisch via `db.Database.Migrate()` beim App-Start
      (App-Start kann je nach FA-Bestand mehrere Minuten dauern).
- [ ] **Alternativ (DBA-Pfad):** `SQL/68_FaWorkStepsAndAttributes.sql` vor dem
      App-Start manuell in SSMS ausfuehren (idempotent, inkl. History-Insert) —
      dann startet die App ohne Migrationslauf.
- [ ] **Verify:**

```sql
SELECT OBJECT_ID('dbo.WorkSteps')               AS WorkSteps,
       OBJECT_ID('dbo.FaWorkSteps')             AS FaWorkSteps,
       OBJECT_ID('dbo.ProductionOrderAssemblyGroups') AS AltTabelle_NULL_erwartet;
-- Erwartet: WorkSteps/FaWorkSteps NOT NULL, Alt-Tabelle NULL
SELECT COUNT(*) FROM dbo.Roles WHERE [Key] = 'vorbau';  -- Erwartet: 1
```

### Step 5 — Service-Deploy + Start

- [ ] Neue `IDEALAKEWMSService`-Binaries einspielen.
- [ ] `IDEALAKEWMSService` → **Start**.
- [ ] **Verify:** Service-Log zeigt sauberen Start, keine SqlExceptions.

### Step 6 — AgentJob reaktivieren

- [ ] SSMS → `01_Import_Produktionsauftraege` → **Enable**.

```sql
SELECT name, enabled FROM msdb.dbo.sysjobs
WHERE name = '01_Import_Produktionsauftraege';
-- Erwartet: enabled = 1
```

### Step 7 — Smoke-Tests

- [ ] (1) **FA-Import 1× manuell ausfuehren** (Job "Start Job at Step…") →
      Job-History gruen, keine Fehler auf `ProductionOrderAssemblyGroups`.
- [ ] (2) **FaCompletion oeffnen** (`/FaCompletion`) → Liste laedt, Edit eines FA:
      Arbeitsgang-Kacheln laden ohne Fehler.
- [ ] (3) **Leitstand-Toggles klicken** (`/PickingLeitstand`) → Status-Toggles
      (Freigabe, HasGlass etc.) funktionieren, keine 500er im Network-Tab.
- [ ] (4) **Stammdaten → Arbeitsgaenge oeffnen** (`/WorkSteps`) → Seed-Katalog
      sichtbar (VK/VL/VE/VT/VA), Liste filterbar.
- [ ] Zusatz: Menue zeigt neuen Block "Fertigungsaufträge" (FA-Liste /
      FA-Vervollständigen / FA-Abarbeitungsliste je nach Rolle + `FaCompletionAktiv`).

**Bei Fehler:** App-Log `logs/idealakewms-YYYYMMDD.log` pruefen. Nicht behebbar
innerhalb 30 min → §4 Rollback.

### Step 8 — Detection aktivieren

- [ ] **Suchstrings pflegen:** Stammdaten → Arbeitsgaenge → je Arbeitsgang die
      Suchstrings (kommasepariert, Contains, case-insensitive) eintragen.
- [ ] **ServiceSetting aktivieren:** `Sync:FaWorkStepDetectionEnabled` = `true`
      (Service-Einstellungen-UI oder ServiceSettings-Tabelle).
- [ ] **Verify:** Nach dem naechsten Sync-Tick zeigt das Aktivitaets-Protokoll
      (`/SyncLog`) einen Detection-Lauf; Stichprobe: FA mit passendem BOM-Treffer
      hat FaWorkStep-Zeilen (`/FaCompletion/Edit/{id}`).

---

## 4. Rollback

**Nur via DB-Restore** — die Migration droppt die AssemblyGroups-Tabellen nach der
Daten-Konvertierung; das Alt-Schema laesst sich nicht verlustfrei aus dem neuen
Schema rekonstruieren.

- [ ] IIS-Site + Windows-Service stoppen.
- [ ] Restore:

```sql
RESTORE DATABASE [IDEAL_AKE_WMS]
FROM DISK = 'D:\Backups\IDEAL_AKE_WMS_v1.21_pre-fa-vorbau.bak'
WITH REPLACE, RECOVERY;
```

- [ ] Alte App-Version (v1.21.x) + alten Service-Build re-deployen.
- [ ] AgentJob-Step-Body auf den ALTEN Skript-Stand zuruecksetzen
      (mit AssemblyGroups-MERGE) und Job wieder enablen.
- [ ] **Sobald User nach dem Cutover gebucht haben:** Restore bedeutet Datenverlust →
      Forward-Fix-Strategie bevorzugen (Hotfix-Build / Folge-Migration).

---

## 5. Sign-off

- [ ] Step 7 Smoke-Tests (1)–(4) gruen.
- [ ] Step 8 Detection-Lauf im Aktivitaets-Protokoll sichtbar.
- [ ] T+1d: AgentJob-History ohne Fehler, keine Exceptions im App-/Service-Log.

**Cutover-Datum:** __________
**Operator-Signatur:** __________
