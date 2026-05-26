# ProductionOrder-Architektur: Roadmap zur Tabellen-Aufteilung

**Datum:** 2026-05-12 (Round 3 — kritisches Review eingearbeitet)
**Branch:** `refactor/production-order-split` (eigener WorkTree)
**Status:** Roadmap → Detail-Specs pro Phase folgen
**Phase:** Architektur-Refactor + neue Use-Case-Views. Mehrere AppVersion-Bumps zu erwarten.

---

## 1. Problemstellung

Die `ProductionOrders`-Tabelle hat heute **28 Spalten** in einer einzigen Tabelle, die drei separate Concerns mischen:

1. **Sage-Master-Daten** (OrderNumber, Quantity, Customer, ArticleNumber, Description1/2, ProductionDate, DeliveryDate, ProductionWorkplaceId, IsDone-Sage).
2. **Vorkommissionierungs-Status** (PickingStatus, PickingPriority, IsReleasedForPicking, ReleasedAt/By, AssignedPicker, HasGlass, HasExternalPurchase, HasCoatingParts, IsCoatingDone).
3. **Spezial-Flags** der letzten Iteration (HasCooling, HasFan, HasElectric, HasDoors, HasSuperstructure — eingebracht für ein zukünftiges Vervollständigungs-Konzept, derzeit nur als UI-Checkboxes).

**Folgen:**
- Jede neue Funktion (neue Liste, neuer Workflow) bläst die Master-Tabelle weiter auf.
- ViewModel + Repository laden volle Datensätze auch für teilweise Use Cases.
- Listen-Views (Leitstand-Komm, BDE, FA-Vervollständigung) konkurrieren um denselben FA-Datensatz und dieselbe Index.cshtml.
- Sage-Sync-Logik muss explizit App-verwaltete Spalten ausnehmen — Liste wächst.

## 2. Ziele

1. **Schlanke `ProductionOrders`** — nur Sage-Master-Daten + Cross-Cutting (`ProductionWorkplaceId`).
2. **Spezialisierungs-Tabellen** mit FK auf FA, jeweils 1:1 oder 1:N je nach Use Case.
3. **Mehrere View-Routen**, jede mit eigenem ViewModel + eigener Permission, eigene Liste.
4. **Neue Rolle + Workflow** für FA-Vervollständigung (Spezifikation der Baugruppen-Ausprägungen).
5. **Big-Bang-Migration** mit Wartungsfenster + Backup-Restore als Rollback-Pfad.

## 3. Out-of-Scope

- **Backward-Compat-Schicht** während des Refactors — atomare Migration, kein Dual-Write.
- **OSEON-Tracking-Refactor** — bleibt eigenständig (`OseonProductionOrders` ist separate Tabelle).
- **WorkOperations** — bleibt unverändert (Beziehung zu FA über `ProductionOrderId`).
- **Stueckliste / BOM** — Bestand bleibt unverändert; nur Phase 5 nutzt die BOM-ähnliche Anzeige als read-only Specs-View.

## 4. Neue Tabellen-Architektur

### 4.1 `ProductionOrders` (schlank, nach Refactor)

Spalten nach Refactor:
- `Id`, `OrderNumber`, `Quantity`, `Customer`, `ArticleNumber`, `Description1`, `Description2`
- `ProductionDate`, `DeliveryDate`
- `ProductionWorkplaceId` (cross-cutting)
- `IsDone` (= Sage-Status, bleibt als globaler Sage-Abschlussindikator)
- Audit-Felder

**Entfernt** (in Migration in neue Tabellen kopiert + alte Spalten gedroppt):
- `PickingStatus`, `PickingPriority`, `IsReleasedForPicking`, `ReleasedAt`, `ReleasedBy`, `AssignedPickerId`, `AssignedPickerName`
- `HasGlass`, `HasExternalPurchase`, `HasCoatingParts`, `IsCoatingDone`
- `HasCooling`, `HasFan`, `HasElectric`, `HasDoors`, `HasSuperstructure`

### 4.2 `ProductionOrderPickingStatus` (1:1 zu FA, eager-created)

Spalten:
- `Id`, `ProductionOrderId` (FK, UNIQUE)
- `PickingStatus` (nvarchar(50))
- `PickingPriority` (int?)
- `IsReleasedForPicking` (bit)
- `ReleasedAt` (datetime2?), `ReleasedBy` (nvarchar(200))
- `AssignedPickerId` (FK auf User), `AssignedPickerName` (nvarchar(200))
- `HasGlass` (bit), `HasExternalPurchase` (bit)
- `HasCoatingParts` (bit) — sync-calculated
- `IsCoatingDone` (bit) — user-toggleable
- `IsDonePicking` (bit)
- Audit-Felder

### 4.3 `ProductionOrderBdeStatus` (1:1 zu FA, eager-created)

Initiale Spalten:
- `Id`, `ProductionOrderId` (FK, UNIQUE)
- `IsDoneBde` (bit)
- Audit-Felder

Spätere Felder (Phase 3): Werkbank-Spezifika, Fertigungsphasen, KPIs.

### 4.4 `ProductionOrderAssemblyGroups` (1:N zu FA, 5 Zeilen je FA eager-created)

Spalten:
- `Id`, `ProductionOrderId` (FK)
- `GroupKey` (nvarchar(10)) — `VK`, `VL`, `VE`, `VT`, `VA`
- `IsApplicable` (bit) — = bisheriger `HasCooling` / `HasFan` / ...
- `IsCompleted` (bit) — "Spezifikation ist vollständig dokumentiert"
- `CompletedAt` (datetime2?), `CompletedBy` (nvarchar(200))
- Audit-Felder

**Unique Constraint** auf (`ProductionOrderId`, `GroupKey`).

**Naming-Hinweis:** `IsApplicable` ist technisch korrekt, UX-mäßig sperrig. Phase-4-Detail-Spec kann das Display-Label im UI anders setzen (z. B. "Benötigt"). Code-Property bleibt `IsApplicable`.

### 4.5 `ProductionOrderAssemblyGroupSpecs` (1:N zu AssemblyGroup)

User-Klärung (Phase 4): *"pro Vormontageplatz pro Fertigungsauftrag Merkmalsausprägungen hinterlegen ... ich pflege die definierten Lüfter-Ausprägungen."*

Spalten (Phase-1-Schema, in Phase 1 leer angelegt):
- `Id`, `AssemblyGroupId` (FK)
- `ArticleId` (FK auf `Articles`, **nullable**, `ON DELETE SET NULL` — wenn der Artikel später aus dem Master gelöscht wird, bleibt die Spec mit `null`-ArticleId)
- `Description` (nvarchar(500), required) — Freitext-Beschreibung (z. B. "Lüfter 230V 80mm 2-stufig")
- `Quantity` (decimal(18,3)?, optional)
- `Notes` (nvarchar(MAX)?, optional)
- `SortOrder` (int) — Anzeige-Reihenfolge
- Audit-Felder

**Detail-Validierungen, Default-Werte, UI-Felder:** in Phase 4 Spec.

### 4.6 `ProductionWorkplaceAssemblyGroups` (Junction-Tabelle, für Phase 5)

Werkbank-zu-Baugruppe-Zuordnung. Pflege erfolgt in Phase 5, aber Schema wird in Phase 1 bereits angelegt.

Spalten:
- `Id`, `ProductionWorkplaceId` (FK)
- `GroupKey` (nvarchar(10)) — `VK` / `VL` / `VE` / `VT` / `VA`
- Audit-Felder

**Unique Constraint** auf (`ProductionWorkplaceId`, `GroupKey`).

**Designentscheidung Junction-Tabelle vs. CSV-Spalte:** Junction wegen Normalform und FK-Integrität. Bei nur 5 möglichen Werten wäre CSV einfacher, aber Junction-Tabelle erlaubt z. B. zukünftig Audit-Trail pro Zuordnung.

## 5. Phase 1 — Big-Bang Schema-Refactor

**Ziel:** Schema-Umbau in einem Wartungsfenster mit kompletter Daten-Migration. Nach erfolgreichem Cutover sind alte Spalten weg, App nutzt ausschließlich neue Tabellen.

### 5.1 Scope

- 5 neue Tabellen anlegen (`PickingStatus`, `BdeStatus`, `AssemblyGroups`, `AssemblyGroupSpecs`, `ProductionWorkplaceAssemblyGroups`).
- EF Migration + idempotentes SQL-Script.
- Datenmigration mit **Batch-Inserts** (siehe 5.4) — pro FA wird angelegt: 1× `PickingStatus`, 1× `BdeStatus`, 5× `AssemblyGroups`.
- Alte 14 Spalten in `ProductionOrders` droppen.
- App-Code (Repo, ViewModel, Controller, View, Toggle-API, AgentJob) komplett auf neue Tabellen umgestellt.
- Sage-AgentJob: Folge-MERGE für neue FAs erzeugt automatisch die 7 Status/AssemblyGroup-Zeilen.
- Toggle-API in separate Endpoints aufgeteilt (siehe 5.3).
- TESTSZENARIEN: kompletter Regression-Durchlauf der bestehenden FA-Funktionen.

**Aufwand:** groß. Vermutlich 10-12 Tasks im Plan.

### 5.2 Wartungsfenster (ehrlich)

App-Stop ist Pflicht. Dauer-Schätzung:
- **<10k FAs:** ~10 Minuten Migration + 5 Min Deploy + 5 Min Smoke-Test = **~20 Minuten** Wartungsfenster.
- **10k–50k FAs:** ~30 Min Migration + Deploy + Smoke = **~45 Minuten** Wartungsfenster.
- **>50k FAs:** ggf. 1 Stunde Wartungsfenster.

**Pre-Migration:** DB-Backup unmittelbar vor Start. AgentJob deaktivieren.

### 5.3 Toggle-API: Separate Endpoints pro Tabelle

Heute: ein einziger `POST /api/productionorders/toggle-field` mit Field-Whitelist.

Nach Refactor: drei separate Endpoints, View-JS sendet jeweils den passenden Endpoint:

| Endpoint | Felder | Tabelle |
|---|---|---|
| `POST /api/picking-status/toggle` | `HasGlass`, `HasExternalPurchase`, `IsCoatingDone`, `IsDonePicking` | `ProductionOrderPickingStatus` |
| `POST /api/assembly-groups/toggle-applicable` | `IsApplicable` (mit `groupKey`-Parameter) | `ProductionOrderAssemblyGroups` |
| `POST /api/bde-status/toggle` (Phase 3 ggf. erweitert) | `IsDoneBde` | `ProductionOrderBdeStatus` |

**View-JS-Anpassung (Phase 1):** Das `data-field`-Attribut wird ergänzt um `data-endpoint` und ggf. `data-group-key`. Beispiel:

```html
<!-- Heute: -->
<input type="checkbox" class="toggle-field" data-id="@item.Id" data-field="HasCooling" ... />

<!-- Nach Refactor: -->
<input type="checkbox" class="toggle-field"
       data-id="@item.Id"
       data-endpoint="/api/assembly-groups/toggle-applicable"
       data-group-key="VK"
       data-field="IsApplicable" ... />
```

JS-Handler dispatcht auf `data-endpoint` und mappt die Payload. Code-Update in [Index.cshtml:517+](IdealAkeWms/Views/ProductionOrders/Index.cshtml#L517) (toggle-field-Listener).

**Permission:** alle drei Endpoints behalten `[RequirePickingAccess]` (analog zu heute). In Phase 4 wird `assembly-groups/toggle-applicable` zusätzlich für `fa_completion`-Rolle freigeschaltet.

### 5.4 Eager-Create Batching für Migration

Bei N FAs in der Quell-Tabelle entstehen N × 7 = 7N neue Zeilen. Migrations-Skript verwendet Batched-INSERT, nicht eine Mega-Transaktion:

```sql
-- Pseudo-Code für die Status-Tabellen
DECLARE @batchSize INT = 5000;
DECLARE @offset INT = 0;
WHILE EXISTS (SELECT 1 FROM dbo.ProductionOrders WHERE Id > @offset)
BEGIN
    BEGIN TRANSACTION;

    INSERT INTO dbo.ProductionOrderPickingStatus (ProductionOrderId, PickingStatus, HasGlass, ...)
    SELECT TOP (@batchSize) Id, PickingStatus, HasGlass, ...
    FROM dbo.ProductionOrders WHERE Id > @offset
    ORDER BY Id;

    SET @offset = (SELECT MAX(Id) FROM dbo.ProductionOrderPickingStatus);

    COMMIT TRANSACTION;
    -- Log-Backup zwischen Batches falls FULL recovery model
END
```

Gleiches Pattern für `BdeStatus` und `AssemblyGroups` (5 INSERTs pro FA in einer Batch-Iteration). Verhindert Lock-Eskalation und übergroße Transaction-Logs.

**Phase-1-Plan detailliert** das exakte SQL inkl. Idempotenz-Checks (für Wiederanlauf bei Abbruch).

### 5.5 Rollback-Pfad (ehrlich)

- **Während der Migration** (vor App-Start mit neuer Version): SQL-Skript abbrechen + `DROP` der neuen Tabellen + AgentJob reaktivieren. Alte Spalten unverändert.
- **Nach App-Start mit neuer Version, vor erstem User-Schreibzugriff**: Backup-Restore zur Vor-Migration-Zeit. Sehr enges Zeitfenster (Minuten).
- **Nach Live-Betrieb (Stunden / Tage)**: kein Rollback ohne Daten-Verlust mehr. **Forward-Fix only.**

→ Phase-1-Plan benennt klar: "Backup-Restore Window endet 30 Minuten nach App-Start. Danach ausschließlich Hotfix-Strategie."

### 5.6 Deploy-Reihenfolge

1. Wartungsfenster ankündigen (User-Kommunikation).
2. App-Stop.
3. DB-Backup.
4. AgentJob deaktivieren.
5. Migration-Skript ausführen (Schema + Batched Daten-Kopie + DROP der alten Spalten).
6. Verifikations-Query: pro alter Spalte vorher-Count == neue-Tabelle-nachher-Count.
7. Neue App-Version deployen + neuer AgentJob.
8. App-Start.
9. Smoke-Test (1× FA-Freigabe, 1× Toggle, 1× Stückliste, 1× BDE-Buchung).
10. AgentJob reaktivieren.
11. Wartungsfenster beenden.

## 6. Phase 2 — Leitstand-Kommissionierung-View extrahieren

**Ziel:** Bestehende `ProductionOrders/Index` in zwei Views aufspalten:
- `ProductionOrders/Index` → schlanke FA-Liste (Sage-Master + Cross-Cutting; für allgemeine User).
- `Picking/Leitstand` (neuer Controller/Action) → Komm-spezifische Spalten + Bulk-Release + Picker-Assign.

**Scope:**
- Neuer Controller `PickingLeitstandController`.
- ViewModel-Trennung: `ProductionOrderListItem` (schlank) vs. `PickingLeitstandItem` (mit Status-Joins).
- Permission: `[RequirePickingAccess]` für Picking-Leitstand; ProductionOrders/Index bekommt eine allgemeinere Permission.
- Routing-Update (Nav-Bar).
- UserViewPreferences: Detail-Entscheidung im Phase-2-Plan (siehe 8.2 Risiko).
- TESTSZENARIEN.

**Aufwand:** mittel. 4-5 Tasks.

## 7. Phase 3 — Leitstand-PPS-light/BDE-View

**Ziel:** Neue View `Bde/Leitstand` mit BDE-spezifischen Spalten + Funktionen (initial: `IsDoneBde`, Werkbank-Aggregat, später erweiterbar).

**Sequentiell nach Phase 2** (ViewModel-Pattern wiederverwendet).

**Funktionsumfang ist offen** — Detail-Spec in Phase 3 nach Klärung mit User.

**Aufwand:** mittel-groß, abhängig vom finalen Scope.

## 8. Phase 4 — FA-Vervollständigung

**Ziel:** Pro FA eine neue Page mit Tabs für VK/VL/VE/VT/VA, in denen Ausprägungen gepflegt werden.

**Scope:**
- Neue Rolle `fa_completion`.
- Neuer Controller `FaCompletionController` + View mit Bootstrap-Tabs.
- Pro Tab:
  - Liste der `AssemblyGroupSpec`-Einträge (sortiert nach `SortOrder`).
  - Add/Edit/Delete-Aktionen.
  - Add-Form: ArticleId via Select2 (Filter auf Artikel-Master nach Artikelgruppe + Freitext), Description (vorbefüllt aus Artikel falls gewählt), Quantity, Notes.
  - `IsApplicable`-Toggle pro Gruppe.
  - `IsCompleted`-Toggle pro Gruppe.
- TESTSZENARIEN.

**Permission-Detail:** `fa_completion`-Rolle bekommt zusätzlich `POST /api/assembly-groups/toggle-applicable`-Recht (siehe 5.3).

**Aufwand:** groß. Eigene neue UI-Schicht.

## 9. Phase 5 — Arbeitsplatz-BOM-View

**Ziel:** Werkbank-Page mit Spec-Listen aller offenen FAs in den werkbankseits konfigurierten Gruppen. Read-only, max. Bestellfunktion.

**Scope:**
- Stammdaten-Erweiterung: `ProductionWorkplace`-Edit-Form bekommt Checkbox-Liste der 5 GroupKeys (Pflege der `ProductionWorkplaceAssemblyGroups`).
- Neue View (Name TBD: `Bde/AssemblyTasks` oder `Workstation/Specs`).
- Liste-Render mit **Pagination + Initial-Filter**:
  - Default-Filter: nur FAs mit `IsApplicable=true` für die konfigurierten Gruppen UND `IsDone=false` UND `IsDonePicking=false` (= "noch zu fertigende FAs").
  - Pagination: 25 FAs pro Seite (analog OSEON-Tracking).
  - User-Filter: per FA-Nummer / Artikel / Freitext über Specs.
- Read-Only-Modus (keine Toggle-API verfügbar).
- Bestellung-Integration (`BestellungenAktiv`-Feature): Bestellen-Button pro Spec-Eintrag mit nullable ArticleId (kein Bestellen ohne Artikel).
- TESTSZENARIEN.

**Permission:** entweder bestehende `bde_user`-Rolle oder neue `production_worker`-Rolle. **Klärung in Phase-5-Detail-Spec.**

**Empty-State:** Wenn `fa_completion` noch keine Specs gepflegt hat, ist die Liste leer. Hinweis-Text "Noch keine Ausprägungen für diese Werkbank gepflegt." mit Link zu FA-Vervollständigung (für berechtigte User).

**Aufwand:** mittel.

## 10. Neue Rolle

**`fa_completion`** (analog zu `picking`, `tracking`, `bde_user`).

- Anzeige-Name: "FA-Vervollständigung"
- AD-Gruppe: optional konfigurierbar (`Role.AdGroup`)
- Berechtigungen: Lesezugriff auf alle FAs; Schreibzugriff auf `AssemblyGroupSpecs`; Toggle-Recht auf `IsApplicable` und `IsCompleted` pro AssemblyGroup.
- Eingeführt in **Phase 4**.

## 11. Abhängigkeiten

```
Phase 1 (Big-Bang Schema-Refactor)
  │
  ├── Phase 2 (Komm-Liste extrahieren)
  │     │
  │     └── Phase 3 (BDE-Leitstand) — sequentiell, nicht parallel
  │
  └── Phase 4 (FA-Vervollständigung) — parallel zu Phase 2/3 möglich
         │
         └── Phase 5 (Arbeitsplatz-BOM)
```

## 12. Risiken

### 12.1 Datenmigration verliert FA-Status (Phase 1)
Mitigation: DB-Backup vor Start. Batched-INSERT mit Idempotenz-Checks. Verifikations-Query nach jedem Batch. Wenn Rollback nötig: Backup-Restore innerhalb 30-Minuten-Fenster nach App-Start, danach Forward-Fix.

### 12.2 UserViewPreferences-Inkompatibilität (Phase 2)
Pref-Daten referenzieren Column-Keys, die nach Phase 2 wandern. Mitigation: bestehende [`mergeWithDefaults`-Logik](IdealAkeWms/wwwroot/js/column-preferences.js#L65) ignoriert unbekannte Keys bereits. Phase 2 Plan kann optional Daten-Migration (Pref auf neue View kopieren) — Entscheidung im Phase-2-Plan.

### 12.3 Sage-Import-Job-Inkompatibilität (Phase 1 Deploy)
AgentJob muss in derselben Wartung mit neuer MERGE-Logik aktualisiert werden. Phase-1-Plan dokumentiert exakte Reihenfolge (siehe 5.6).

### 12.4 Permission-Migration
Neue Rolle `fa_completion` in Phase 4. Mitigation: Phase-4-Detail-Spec dokumentiert exakte Rolle-zu-Feature-Mapping inkl. Toggle-API-Permissions.

### 12.5 Scope-Creep in Phase 3 (BDE)
Mitigation: Phase 3 erst starten, wenn User den BDE-spezifischen Funktionsumfang konkret beschreibt.

### 12.6 Toggle-API-Refactor erfordert View-JS-Update (Phase 1)
Heute pingt View einen einzigen Endpoint mit Field-Whitelist. Nach Refactor: drei Endpoints, View-JS dispatcht auf `data-endpoint`. Mitigation: Phase-1-Plan deckt View-Update explizit ab; alle Checkbox-Markups bekommen `data-endpoint`-Attribut.

### 12.7 JOIN-Performance auf FA-Liste (Phase 1)
Heute: FA-Liste liest 5 Bool-Spalten direkt. Nach Refactor: JOIN auf `AssemblyGroups` (5 Zeilen pro FA) + Pivot zu 5 Spalten. Mitigation: Repo-Method per Pivot-Query mit `LEFT JOIN` und Filter `GroupKey IN (...)`. Index auf `(ProductionOrderId, GroupKey)` durch Unique Constraint vorhanden. Phase-1-Plan misst Query-Performance mit Stage-DB vor Produktiv-Deploy.

### 12.8 WorkTree-Branch-Konflikte
`refactor/production-order-split` zweigt heute vom Bundle-Branch ab. Phase 1 startet nach Bundle-Merge in `main`. Rebase-Strategie:
```pwsh
git fetch origin
git rebase origin/main
# Konflikte vermutlich nur im ApplicationDbContextModelSnapshot.cs (EF Migration Snapshot)
# Lösung: bei Konflikt erst Snapshot regenerieren via "dotnet ef migrations remove --force" + neu hinzufügen
git push --force-with-lease origin refactor/production-order-split
```

### 12.9 AgentJob-Komplexitätszuwachs
Nach Refactor schreibt der Sage-Job in 4 Tabellen statt 1 (1× ProductionOrders, 1× PickingStatus, 1× BdeStatus, 5× AssemblyGroups pro neuem FA). MERGE-Statements werden länger. Mitigation: AgentJob-Skript in benannte Sections aufteilen mit Kommentar-Header je Section. Idempotenz durch `NOT EXISTS`-Guards je Tabelle.

### 12.10 AssemblyGroups-Lifecycle bei abgeschlossenen FAs
Wenn eine FA dauerhaft `IsDone=true` ist, bleiben ihre 5 AssemblyGroups + N Specs für immer. **Akzeptiert für v1.** Bei zukünftigem Archivierungs-Bedarf: separate Migration zum Verschieben in Archive-Tabellen. Roadmap-offen für später.

## 13. Operational-Strategie

### 13.1 Wartungsfenster
Big-Bang erfordert App-Stop. Geschätzte Dauer siehe 5.2. Vorab User-Kommunikation 24-48h vor dem Wartungsfenster.

### 13.2 Rollback-Pfade
- **Innerhalb 30 Minuten nach App-Start mit neuer Version**: Backup-Restore möglich.
- **Nach 30 Min Live-Betrieb**: Forward-Fix only. Hotfix-Strategie:
  - Bug in Repo-Query → Code-Hotfix + App-Restart.
  - Daten-Inkonsistenz → manuelles SQL-Fix-Skript.
  - Schema-Defekt → ad-hoc Migration als Hotfix.

### 13.3 Live-Verifikation (Tag 1-5 nach Deploy)
- Verifikations-Query täglich (Sum-Counts pro Status-Feld). Erwartung: stabil oder steigt linear mit neuen Buchungen.
- Monitor SuccessRate der drei Toggle-Endpoints.
- Picker-Workflow-Completion-Rate beobachten.
- Bei jedem User-Bericht "wo ist Feature X" → Untersuchung + Hotfix.

### 13.4 Test-Coverage-Anforderungen (Phase 1)
Existing Tests decken Toggle-API und Repo-Projektionen nicht ab. Phase 1 muss explizit Tests für:
- `ProductionOrderPickingStatusRepository` CRUD + Read-Through.
- `ProductionOrderAssemblyGroupRepository` CRUD + Pivot-Aggregation (5-Bool-Spalten aus 5 Rows).
- `PickingStatusApiController.Toggle` mit Whitelist (HasGlass/Purchase/Coating/IsDonePicking).
- `AssemblyGroupsApiController.ToggleApplicable` mit `groupKey`-Validierung.
- `BdeStatusApiController.Toggle` (auch wenn nur 1 Feld, für Konsistenz).
- AgentJob-Sync-Test: nach Sage-Import existiert für neue FA exakt 1× PickingStatus + 1× BdeStatus + 5× AssemblyGroups.

Coverage-Anforderung: jeder neue Repo + Endpoint hat mindestens 1 Happy-Path-Test.

## 14. Ablauf

1. Roadmap committen + reviewen (heute, Round 3 abgeschlossen).
2. Trigger: Bundle-Branch in `main` gemerged + Live-stabil (siehe 16).
3. Phase 1 Detail-Spec + Plan schreiben.
4. Phase 1 deployen mit Wartungsfenster.
5. 5 Tage Live-Verifikation.
6. Phase 2 → Phase 3 (sequentiell). Parallel: Phase 4 starten.
7. Phase 5 nach Phase 4.

## 15. Entscheidungen (geklärt am 2026-05-12)

| # | Thema | Entscheidung |
|---|---|---|
| Q1 (R1) | `IsDone`-Semantik | `IsDone` bleibt auf FA (Sage-Master). Zusätzlich `IsDonePicking` und `IsDoneBde` in den Status-Tabellen. |
| Q2 (R1) | Status-Tabellen-Create | Eager — beim Refactor wird für jede FA ein leerer Status-Datensatz angelegt. |
| Q3 (R1) | AgentJob-Anpassung | Folge-MERGE im Skript. |
| Q4 (R1) | Phase-1-Start | Erst nach Merge des aktuellen Bundle-Branchs in `main`. |
| Q5 (R1) | AssemblyGroups-Zeilen | Immer alle 5 Zeilen (VK/VL/VE/VT/VA) pro FA, `IsApplicable=false` Default. |
| Q6 (R2) | Phase-4-Spec-Modell | `AssemblyGroupSpecs` mit ArticleId (optional), Description (required), Quantity, Notes, SortOrder. |
| Q7 (R2) | Phase-5-Werkbank-Modell | `ProductionWorkplaceAssemblyGroups`-Junction-Tabelle (Werkbank → konfigurierte GroupKeys). |
| Q8 (R3, **revidiert**) | Migration-Strategie | **Big-Bang mit Wartungsfenster + Backup-Restore-Rollback**. (Round 2: war 2-Schritt, wurde verworfen — Round-3-Review zeigte, dass das Rollback-Versprechen irreführend war.) |
| Q9 (R3) | Toggle-API-Strategie | **Separate Endpoints pro Tabelle**: `/api/picking-status/toggle`, `/api/assembly-groups/toggle-applicable`, `/api/bde-status/toggle`. View-JS dispatcht auf `data-endpoint`. |
| Q10 (R3) | ArticleId-Cascade | `ON DELETE SET NULL` auf `AssemblyGroupSpecs.ArticleId` — bei Artikel-Löschung bleibt Spec mit `null`-ArticleId erhalten. |
| Q11 (R3) | Eager-Create für große DBs | Batched-INSERT (5000 Zeilen/Batch) in eigenen Transaktionen, **nicht** in einer Mega-Transaktion. |

## 16. Phase-1-Voraussetzungen / Trigger

Phase 1 ist blockiert bis:

1. ☐ `feature/sage-lagerbestand-sync` ist in `main` gemerged.
2. ☐ Live-Verifikation der Bundle-Features in Produktion (mindestens 1 Arbeitstag ohne Bugs).
3. ☐ Refactor-Branch auf `main` rebased (siehe 12.8).
4. ☐ Wartungsfenster mit Stakeholdern abgestimmt (24-48h Vorlauf).
5. ☐ DB-Backup-Strategie verifiziert (Restore-Test auf Stage).

Nach Trigger:
- **Phase-1 Detail-Spec** schreiben: `docs/superpowers/specs/2026-MM-DD-production-order-split-phase-1-schema-design.md`.
- **Phase-1 Plan** schreiben: `docs/superpowers/plans/2026-MM-DD-production-order-split-phase-1-schema.md`.

## 17. Critical-Review-Historie

### Round 1 (Commit `cc3a6dc`, 2026-05-12 vormittags)
Ursprünglicher Wurf der Roadmap mit Phasen 1-5, atomare Migration ohne Wartungsfenster-Detail.

### Round 2 (Commit `32e44c3`, 2026-05-12 nachmittags)
- Phase 4 Schema konkretisiert (Merkmalsausprägungen statt Filter-Regeln).
- Phase 5 Werkbank-Mapping ergänzt (`ProductionWorkplaceAssemblyGroups`).
- Phase 1 versuchsweise in 1a + 1b gesplittet (Rollback-Mitigation).
- Operational-Strategie, Risiken erweitert.

### Round 3 (dieses Update)
- **Verworfen: 2-Schritt-Migration.** Rollback-Versprechen war irreführend (App schreibt nach Cutover nur in neue Tabellen, alte Spalten veralten).
- **Bestätigt: Big-Bang mit Backup-Restore.** Ehrliches Wartungsfenster, klare Rollback-Grenzen.
- **Toggle-API separiert in 3 Endpoints.** View-JS-Refactor in Phase 1 explizit.
- **Batching für Migration** dokumentiert (Mega-Transaction vermieden).
- **AssemblyGroups-Lifecycle, AgentJob-Komplexität, ArticleId-Cascade** als explizite Risiken / Entscheidungen.
- **Phase 5 Permission + Pagination + Empty-State** klargestellt.
- **`IsApplicable`-Naming-Trade-off** dokumentiert (Code bleibt, UI-Label kann anders sein).

---

**Hinweis:** Dieses Dokument ist ein **lebendes Roadmap-Doc**. Pro Phase werden Detail-Specs + Pläne als separate Files angelegt:
- `docs/superpowers/specs/2026-MM-DD-production-order-split-phase-N-design.md`
- `docs/superpowers/plans/2026-MM-DD-production-order-split-phase-N.md`
