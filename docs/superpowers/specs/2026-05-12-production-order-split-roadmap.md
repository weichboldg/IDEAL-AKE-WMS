# ProductionOrder-Architektur: Roadmap zur Tabellen-Aufteilung

**Datum:** 2026-05-12 (Round 2 — kritisches Review eingearbeitet)
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
5. **Zwei-Schritt-Migration** (Schema+Copy in Phase 1a, Drop-Old in Phase 1b) statt atomar in einem Schritt — ermöglicht Live-Verifikation und Rollback ohne Backup-Restore.

## 3. Out-of-Scope (für diese Roadmap)

- **Backward-Compat-Schicht** während des Refactors — Phase 1a hält beide Welten kurz parallel, aber kein dauerhafter Dual-Write.
- **OSEON-Tracking-Refactor** — bleibt eigenständig (`OseonProductionOrders` ist separate Tabelle).
- **WorkOperations** — bleibt unverändert (Beziehung zu FA über `ProductionOrderId`).
- **Stueckliste / BOM** — Bestand bleibt unverändert; nur Phase 5 nutzt die BOM-ähnliche Anzeige als read-only Specs-View.

## 4. Neue Tabellen-Architektur

### 4.1 `ProductionOrders` (schlank)

Spalten nach Refactor:
- `Id`, `OrderNumber`, `Quantity`, `Customer`, `ArticleNumber`, `Description1`, `Description2`
- `ProductionDate`, `DeliveryDate`
- `ProductionWorkplaceId` (cross-cutting — wird in mehreren Listen genutzt)
- `IsDone` (= Sage-Status, "WA in Sage erledigt"; bleibt auf der Master-Tabelle als globaler Abschlussindikator)
- `CreatedAt`, `CreatedBy`, `CreatedByWindows`, `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows`

**Entfernt** (nach Phase 1b in neue Tabellen):
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
- `HasGlass` (bit)
- `HasExternalPurchase` (bit)
- `HasCoatingParts` (bit) — bleibt sync-calculated
- `IsCoatingDone` (bit) — user-toggleable
- `IsDonePicking` (bit) — = bisheriger `IsDone` für Kommissionierung
- Audit-Felder

### 4.3 `ProductionOrderBdeStatus` (1:1 zu FA, eager-created)

Initiale Spalten:
- `Id`, `ProductionOrderId` (FK, UNIQUE)
- `IsDoneBde` (bit) — = "Auftrag komplett über BDE fertiggemeldet"
- Audit-Felder

Spätere Felder (Phase 3): Werkbank-Spezifika, Fertigungsphasen, KPIs.

### 4.4 `ProductionOrderAssemblyGroups` (1:N zu FA, 5 Zeilen je FA eager-created)

Spalten:
- `Id`, `ProductionOrderId` (FK)
- `GroupKey` (nvarchar(10)) — Enum-Werte: `VK`, `VL`, `VE`, `VT`, `VA`
- `IsApplicable` (bit) — = bisheriger `HasCooling` / `HasFan` / ...
- `IsCompleted` (bit) — neue Semantik: "Spezifikation für diese Gruppe ist vollständig dokumentiert"
- `CompletedAt` (datetime2?), `CompletedBy` (nvarchar(200))
- Audit-Felder

**Unique Constraint** auf (`ProductionOrderId`, `GroupKey`).

### 4.5 `ProductionOrderAssemblyGroupSpecs` (1:N zu AssemblyGroup)

User-Klärung (Phase-4-Use-Case): *"zum FA sollen pro Vormontageplatz zb. (VE) Elektro, (VL) Lüfter ... zusätzliche **Merkmalsausprägungen** pro Vormontageplatz pro Fertigungsauftrag hinterlegt werden können. ... ich wähle zb. VL (Lüfter) und **pflege die definierten Lüfter-Ausprägungen**."*

Spalten (Phase-1-Schema, initial leer):
- `Id`, `AssemblyGroupId` (FK)
- `ArticleId` (FK auf `Articles`, **optional/nullable**) — wenn die Ausprägung auf einen konkreten Artikel referenziert
- `Description` (nvarchar(500), required) — Freitext-Beschreibung der Ausprägung (z. B. "Lüfter 230V 80mm 2-stufig")
- `Quantity` (decimal(18,3)?, optional) — Mengen-Bedarf
- `Notes` (nvarchar(MAX)?, optional)
- `SortOrder` (int) — Anzeige-Reihenfolge in der UI
- Audit-Felder

**Hinweis:** Tabelle wird in Phase 1 leer angelegt. Befüllung kommt mit Phase 4 (Vervollständigungs-UI). Detail-Validierungen, Default-Werte, UI-Felder werden in Phase 4 Detail-Spec festgelegt.

### 4.6 `ProductionWorkplaceAssemblyGroups` (NEU — für Phase 5)

Werkbank-zu-Baugruppe-Zuordnung. Eine Werkbank zeigt die Spezifikationen der konfigurierten Gruppen aus allen offenen FAs.

Spalten:
- `Id`, `ProductionWorkplaceId` (FK)
- `GroupKey` (nvarchar(10)) — `VK` / `VL` / `VE` / `VT` / `VA`
- Audit-Felder

**Unique Constraint** auf (`ProductionWorkplaceId`, `GroupKey`).

**Beispiel-Konfiguration:**
- Werkbank "VK-Montage-01" → Zeilen für `VK`
- Werkbank "Elektro/Lüfter-Station" → Zeilen für `VE` und `VL`
- Werkbank "Aufbau-Final" → Zeilen für `VA`

**Pflege:** Stammdaten-Erweiterung am `ProductionWorkplace`-Edit-Form (Checkbox-Liste der 5 GroupKeys). **Implementierung in Phase 5**, nicht Phase 1. Aber Schema wird in Phase 1a bereits angelegt, damit es keine spätere DB-Migration mehr braucht.

## 5. Phasen-Übersicht

### Phase 1a — Schema-Setup + Daten-Kopie (alte Spalten bleiben)

**Ziel:** Neue Tabellen anlegen, Daten kopieren, alle Konsumer auf neue Struktur umgestellt. **Alte Spalten in `ProductionOrders` bleiben unverändert als Read-only-Backup**, werden vom App-Code nicht mehr beschrieben.

**Scope:**
- 5 neue Tabellen anlegen (`PickingStatus`, `BdeStatus`, `AssemblyGroups`, `AssemblyGroupSpecs`, `ProductionWorkplaceAssemblyGroups`).
- EF Migration + idempotentes SQL-Script.
- Datenmigration: pro existierende FA werden `PickingStatus`, `BdeStatus`, und 5 `AssemblyGroups`-Zeilen (1 je VK/VL/VE/VT/VA mit `IsApplicable` aus alten `HasCooling/HasFan/...`) angelegt.
- App-Code (Repo, ViewModel, Controller, View, Toggle-API, AgentJob) komplett auf neue Tabellen umgestellt.
- Sage-AgentJob: Folge-MERGE für neue FAs erzeugt automatisch die 7 Status/AssemblyGroup-Zeilen.
- TESTSZENARIEN: kompletter Regression-Durchlauf der bestehenden FA-Funktionen.

**Verifikation:**
- Verifikations-Query: pro Feld in der alten Tabelle == aggregiertes Feld in der neuen Tabelle (z. B. `SELECT COUNT(*) FROM ProductionOrders WHERE HasGlass=1` == `SELECT COUNT(*) FROM ProductionOrderPickingStatus WHERE HasGlass=1`).
- Live-Verifikation min. 5 Arbeitstage in Produktion ohne neue Bugs.

**Aufwand:** groß. Vermutlich 7-10 Tasks im Plan.

### Phase 1b — Drop der alten Spalten

**Ziel:** Nach erfolgreicher Phase-1a-Verifikation die 14 entfernten Spalten in `ProductionOrders` droppen.

**Scope:**
- EF Migration + SQL-Script: `DROP COLUMN` für 14 Spalten.
- Sage-AgentJob: Spalten-Liste im INSERT/UPDATE entfernen (war seit Phase 1a schon nicht mehr genutzt).
- Code-Cleanup: alte `[NotMapped]` o. ä. Reste entfernen.

**Trigger:** Nach Phase 1a + 5 Arbeitstage Live-Verifikation **erfolgreich**.

**Rollback-Pfad:** Wenn in Phase 1a-Verifikationsphase Bugs auftauchen, **bevor** Phase 1b durchgeführt wird, kann der App-Code zurückgerollt werden — die alten Spalten enthalten noch die ursprünglichen Daten (sind read-only durch App, aber nicht verändert). Hotfix-fähig ohne Backup-Restore.

**Aufwand:** klein. 1-2 Tasks.

### Phase 2 — Leitstand-Kommissionierung-View extrahieren

**Ziel:** Bestehende `ProductionOrders/Index` in zwei Views aufspalten:
- `ProductionOrders/Index` → schlanke FA-Liste (Sage-Master + Cross-Cutting; für allgemeine User).
- `Picking/Leitstand` (neuer Controller/Action) → die heute schon vorhandenen Komm-spezifischen Spalten + Bulk-Release + Picker-Assign.

**Scope:**
- Neuer Controller `PickingLeitstandController` oder neue Action am `PickingController`.
- ViewModel-Trennung: `ProductionOrderListItem` (schlank) vs. `PickingLeitstandItem` (mit Status-Joins).
- Toggle-API in `PickingApiController` extrahiert (HasGlass, HasExternalPurchase, IsCoatingDone landen dort).
- Permission: `[RequirePickingAccess]` für Picking-Leitstand; ProductionOrders/Index bekommt eine allgemeinere Permission.
- Routing-Update (Nav-Bar).
- UserViewPreferences-Migration (Pref auf alte ProductionOrders-Liste → wird auf Picking-Leitstand-Liste übernommen falls Felder dort sichtbar).
- TESTSZENARIEN.

**Aufwand:** mittel. 4-5 Tasks.

### Phase 3 — Leitstand-PPS-light/BDE-View (SEQUENTIELL nach Phase 2)

**Ziel:** Neue View `Bde/Leitstand` mit BDE-spezifischen Spalten + Funktionen (initial: IsDoneBde, Werkbank-Aggregat, später erweiterbar).

**Scope:**
- Neuer Controller `BdeLeitstandController`.
- `BdeStatus`-Tabelle wird Hauptdatenquelle (gejoint mit FA).
- Toggle-API für `IsDoneBde` in `BdeApiController` (oder neuer).
- Permission: `[RequireBdeShiftleadAccess]` oder neue Permission `bde_leitstand`.
- TESTSZENARIEN.

**Konkreter Funktionsumfang ist offen** — Detail-Spec in Phase 3 nach Klärung mit User. **Sequentiell nach Phase 2**, weil das ViewModel-Pattern aus Phase 2 wiederverwendet wird.

**Aufwand:** mittel-groß, abhängig vom finalen Scope.

### Phase 4 — FA-Vervollständigung

**Ziel:** Pro FA eine neue Page mit Tabs für VK/VL/VE/VT/VA, in denen Ausprägungen (Spec-Einträge mit ArticleId/Description/Quantity) gepflegt werden.

**Scope:**
- Neue Rolle `fa_completion` (siehe Sektion 6 für Naming).
- Neuer Controller `FaCompletionController` + View `Edit.cshtml` mit Bootstrap-Tabs.
- Pro Tab:
  - Liste der bestehenden `AssemblyGroupSpec`-Einträge dieser FA × Gruppe (sortiert nach `SortOrder`).
  - Add/Edit/Delete-Aktionen.
  - Add-Form: ArticleId per Select2 (mit Filter auf Artikel-Master, auch nach Artikelgruppe + Freitext), Description (vorbefüllt aus Artikel falls gewählt), Quantity, Notes.
  - `IsApplicable`-Toggle pro Gruppe (= heutiges VK/VL/...-Flag).
  - `IsCompleted`-Toggle pro Gruppe ("Vervollständigung abgeschlossen").
- TESTSZENARIEN.

**Aufwand:** groß. Eigene neue UI-Schicht.

### Phase 5 — Arbeitsplatz-BOM-View

**Ziel:** Werkbank-Page mit Spec-Listen aller offenen FAs, deren AssemblyGroups der Werkbank zugeordnet sind. Read-only, max. Bestellfunktion.

**Scope:**
- Stammdaten-Erweiterung: `ProductionWorkplace`-Edit-Form bekommt Checkbox-Liste der 5 GroupKeys (Pflege der `ProductionWorkplaceAssemblyGroups`-Zuordnung).
- Neue View `Bde/AssemblyTasks` oder `Workstation/Specs`.
- Tree-/Liste-Render: für aktuelle Werkbank → finde alle offenen FAs mit `AssemblyGroup.IsApplicable=true` in der Werkbank-konfigurierten Gruppe → liste alle `AssemblyGroupSpec`-Einträge.
- Filter: per FA / per Spec-ArticleGroup / Freitext.
- Read-Only-Modus für Picking-Toggle (kein Toggle-API verfügbar).
- Bestellung-Integration aus dem bestehenden `BestellungenAktiv`-Feature (Bestellen-Button pro Spec-Eintrag).
- TESTSZENARIEN.

**Aufwand:** mittel.

## 6. Neue Rolle

Vorschlag: **`fa_completion`** (analog zu `picking`, `tracking`, `bde_user`).

- Anzeige-Name: "FA-Vervollständigung"
- AD-Gruppe: optional konfigurierbar (`Role.AdGroup`)
- Berechtigungen: Lesezugriff auf alle FAs; Schreibzugriff auf `AssemblyGroupSpecs`.
- Eingeführt in **Phase 4** (nicht früher — andere Phasen brauchen sie nicht).

Endgültiges Naming in Phase 4 Spec.

## 7. Abhängigkeiten

```
Phase 1a (Schema-Setup + Daten-Kopie)
  │
  └── Phase 1b (Drop-Old) — nach 5 Arbeitstagen Live-Verifikation
         │
         ├── Phase 2 (Komm-Liste extrahieren)
         │     │
         │     └── Phase 3 (BDE-Leitstand) — sequentiell, nicht parallel
         │
         └── Phase 4 (FA-Vervollständigung) — kann parallel zu Phase 2/3 starten
                │
                └── Phase 5 (Arbeitsplatz-BOM)
```

**Wichtige Änderung gegenüber Round 1:** Phase 2 und 3 sind nicht mehr parallel. Begründung:
- Phase 3 reutilisiert das ViewModel-Pattern aus Phase 2.
- Phase 3 ist konzeptionell vage; Phase 2 zuerst liefert konkrete Lernkurve.
- Bei parallel parallel besteht Risiko: zwei Teams schreiben zwei separate ViewModel-Schichten, die später konsolidiert werden müssten.

Phase 4 kann parallel zu Phase 2/3 starten, weil es eine eigene UI-Schicht ist und keine FA-Listen-Logik teilt.

## 8. Risiken

### 8.1 Datenmigration verliert FA-Status (Phase 1a)
Mitigation: 2-Schritt-Migration (Phase 1a behält alte Spalten als Backup). Verifikations-Query nach Migration. Phase-1b erst nach 5 Arbeitstagen Verifikation.

### 8.2 UserViewPreferences-Inkompatibilität (Phase 2)
Pref-Daten referenzieren Column-Keys, die nach Phase 2 wandern. Mitigation: bestehende [`mergeWithDefaults`-Logik](IdealAkeWms/wwwroot/js/column-preferences.js#L65) ignoriert unbekannte Keys bereits. Phase 2 Plan kann optional eine UserViewPreferences-Daten-Migration aufnehmen, die die alte ProductionOrders-Pref auf die neue Picking-Leitstand-Pref überträgt.

### 8.3 Sage-Import-Job-Inkompatibilität (Phase 1a Deploy)
Job MUSS während des Refactor-Deploys mit der neuen Folge-MERGE-Logik aktualisiert werden. Phase-1-Plan dokumentiert Deploy-Reihenfolge.

### 8.4 Permission-Migration
Neue Rolle `fa_completion` in Phase 4. Bestehende Permission-Filter bleiben unverändert. Mitigation: Phase-4-Detail-Spec dokumentiert exakte Rolle-zu-Feature-Mapping.

### 8.5 Scope-Creep in Phase 3 (BDE)
"PPS light" ist heute kein konkretes Feature-Set, sondern eine Vision. Mitigation: Phase 3 erst starten, wenn der User den BDE-spezifischen Funktionsumfang konkret beschreibt.

### 8.6 Toggle-API-Komplexität (Phase 1a)
Heute: `POST /api/productionorders/toggle-field` schreibt in **eine** Tabelle (8 Branches in if/else). Nach Refactor: muss in 3 Tabellen routen (`PickingStatus` für HasGlass/Purchase/Coating, `AssemblyGroups` für VK/VL/VE/VT/VA mit GroupKey-Mapping). Mitigation: API in Phase 1a entweder als Mapping-Dictionary refactoren oder pro Tabelle eigenen Endpoint anlegen — Detail in Phase 1a Plan. **Bevorzugt:** ein einziger Endpoint mit interner Routing-Map, weil bestehende View-JS schon einheitlich auf `toggle-field` pingt.

### 8.7 JOIN-Performance auf FA-Liste (Phase 1a)
Heute: FA-Liste liest 5 Bool-Spalten direkt. Nach Refactor: JOIN auf `AssemblyGroups` (5 Zeilen pro FA) + Pivot zu 5 Spalten. Bei 1000 FAs in der Liste = 5000 AssemblyGroup-Zeilen + Pivot. Mitigation: Repo-Method aggregiert per Pivot-Query oder per `LEFT JOIN` mit fünf separaten Joins (auf `GroupKey` gefiltert). Index auf `(ProductionOrderId, GroupKey)` schon vorhanden (Unique Constraint). Phase-1a-Plan verifiziert Query-Performance mit realer Datenmenge bevor Deploy.

### 8.8 WorkTree-Branch-Konflikte
`refactor/production-order-split` zweigt heute vom Bundle-Branch ab. Phase 1 startet nach Bundle-Merge in `main`. Mitigation: Rebase-Strategie:
```
git fetch origin
git rebase origin/main
# Konflikte lösen (vermutlich nur in Migrations-Snapshot)
git push --force-with-lease origin refactor/production-order-split
```
Falls Rebase zu schwierig wird: alternativ `git merge origin/main` (Merge-Commit statt linear). Phase-1-Plan-Detail.

## 9. Operational-Strategie

### 9.1 Wartungsfenster
Phase 1a benötigt **kein** App-Stop, weil 2-Schritt-Migration:
- Schema-Anlage + Daten-Kopie laufen während App online (Migration schreibt in neue, unbenutzte Tabellen).
- Sobald App-Deploy mit neuer Codebase erfolgt, lesen/schreiben Requests in neue Tabellen.
- Alte Tabellen bleiben als Backup.

**Aber:** Sage-AgentJob muss zeitlich abgestimmt sein. Wenn AgentJob mitten in der Migration läuft, schreibt er in alte Spalten. Empfehlung: AgentJob für 10-30 Minuten deaktivieren, Migration laufen lassen, AgentJob mit neuem Skript reaktivieren.

Phase 1b benötigt App-Restart (Schema-Change), aber keine Datenmigration → < 5 Minuten Downtime.

### 9.2 Rollback-Pfade
- **Während Phase 1a, vor Verifikation:** App-Code zurückrollen (alte App-Version), alte Spalten enthalten weiterhin korrekte Daten. AgentJob auf alte Version setzen.
- **Während Phase 1a, nach Verifikation:** Bug-Fix in Phase 1a vor Phase 1b. Kein voller Rollback nötig.
- **Nach Phase 1b:** Backup-Restore. Alte Spalten sind weg.

### 9.3 Live-Verifikation während Phase 1a (5 Arbeitstage)
- Tägliche Verifikations-Query (siehe 8.1).
- Monitor: SuccessRate des Toggle-API, Picker-Workflow-Completion-Rate.
- Bei jedem produktiven User-Bericht über "wo ist die Funktion X hin" → Untersuchung + Phase-1a-Hotfix.

### 9.4 Deploy-Reihenfolge Phase 1a
1. AgentJob deaktivieren.
2. SQL-Migration laufen lassen (Tabellen + Daten-Kopie + Eager-Create-Status für bestehende FAs).
3. Verifikations-Query — vorher/nachher-Counts müssen matchen.
4. Neue App-Version deployen.
5. Neuen AgentJob deployen.
6. AgentJob reaktivieren.
7. Smoke-Test (1 Buchung, 1 FA-Freigabe, 1 BDE-Buchung).

### 9.5 Test-Coverage-Risiko
Existing Tests decken Toggle-API und Repo-Projektionen nicht ab (siehe vorherige Feature-Implementierungen). Phase 1a sollte **explizit** Tests für:
- `ProductionOrderPickingStatusRepository`-CRUD
- `ProductionOrderAssemblyGroupRepository`-CRUD und Pivot-Aggregation
- Toggle-API-Routing (HasGlass → PickingStatus, HasCooling → AssemblyGroup mit GroupKey=VK)

Sonst stille Regressionen wahrscheinlich.

## 10. Ablauf

1. Roadmap committen + reviewen (heute).
2. Trigger: Bundle-Branch in `main` gemerged + Live-stabil (siehe Sektion 12).
3. Phase 1a Detail-Spec + Plan schreiben.
4. Phase 1a implementieren + 5-Tage-Verifikation.
5. Phase 1b implementieren (Drop-Old).
6. Phase 2 → Phase 3 (sequentiell). Parallel: Phase 4 starten.
7. Phase 5 nach Phase 4.

## 11. Entscheidungen (geklärt am 2026-05-12)

| # | Thema | Entscheidung |
|---|---|---|
| Q1 | `IsDone`-Semantik | **A:** `IsDone` bleibt auf FA (= Sage-Master "WA in Sage erledigt"). Zusätzlich `IsDonePicking` auf `PickingStatus`, `IsDoneBde` auf `BdeStatus`. |
| Q2 | Status-Tabellen-Create | **A:** Eager — beim Schema-Refactor wird für jede bestehende FA ein leerer `PickingStatus`- und `BdeStatus`-Datensatz angelegt. Sage-AgentJob legt für neue FAs gleich die Status-Datensätze mit an (Folge-MERGE). |
| Q3 | AgentJob-Anpassung | **Folge-MERGE im AgentJob-Skript** (kein DB-Trigger, keine App-Logik beim Read). |
| Q4 | Phase-1-Start | **Erst nach Merge des aktuellen Bundle-Branchs in `main`.** Refactor zweigt dann sauber von `main` ab (Rebase). |
| Q5 | AssemblyGroups | **A:** Immer alle 5 Zeilen (VK/VL/VE/VT/VA) pro FA, `IsApplicable=false` als Default. |
| Q6 (R2) | Phase-4-Spec-Modell | **AssemblyGroupSpecs mit ArticleId (optional), Description (required), Quantity, Notes, SortOrder.** Pro FA × Gruppe können Ausprägungen hinterlegt werden — User-Quote: "pflege die definierten Lüfter-Ausprägungen". |
| Q7 (R2) | Phase-5-Werkbank-Modell | **`ProductionWorkplaceAssemblyGroups`-Mapping-Tabelle** (Werkbank → konfigurierte GroupKeys). Eine Werkbank zeigt Specs aller offenen FAs in den konfigurierten Gruppen. |
| Q8 (R2) | Migration-Strategie | **2 Schritte:** Phase 1a (Schema+Copy, alte Spalten bleiben) → 5 Tage Verifikation → Phase 1b (Drop alte Spalten). Ermöglicht Hotfix ohne Backup-Restore. |

## 12. Phase-1-Voraussetzungen / Trigger

Phase 1a ist **blockiert** bis:

1. ☐ `feature/sage-lagerbestand-sync` ist in `main` gemerged.
2. ☐ Live-Verifikation der Bundle-Features in Produktion (mindestens 1 Arbeitstag ohne Bugs).
3. ☐ Refactor-Branch `refactor/production-order-split` auf `main` rebased (siehe Sektion 8.8 für Strategie).

Nach Trigger:
- **Phase-1a Detail-Spec** schreiben: `docs/superpowers/specs/2026-MM-DD-production-order-split-phase-1a-schema-design.md`.
- **Phase-1a Plan** schreiben: `docs/superpowers/plans/2026-MM-DD-production-order-split-phase-1a-schema.md`.

## 13. Critical-Review-Notizen (Round 2 — 2026-05-12)

Folgende Punkte wurden gegenüber Round 1 (Commit `cc3a6dc`) geändert:

- **Phase 4 Schema** (4.5): War "Filter-Speicherung", ist jetzt "Merkmalsausprägungen-Liste" — eigene Spalten ArticleId/Description/Quantity/Notes/SortOrder. Basiert auf User-Klärung.
- **Phase 5 Werkbank-Mapping** (4.6 NEU): Tabelle `ProductionWorkplaceAssemblyGroups` ergänzt — Werkbank konfiguriert welche Gruppen sie zeigt.
- **Phase 1 gesplittet in 1a + 1b** (Sektion 5): 2-Schritt-Migration mit 5-Tage-Verifikation zwischen Phase 1a und 1b. Rollback ohne Backup möglich.
- **Phase 2 vor Phase 3 fixiert** (Sektion 7): nicht mehr parallel, sequentiell. Begründung: Phase 3 reutilisiert Phase-2-Patterns; parallele Arbeit würde Konsolidierung erfordern.
- **Operational-Strategie ergänzt** (Sektion 9 NEU): Wartungsfenster, Rollback-Pfade, Deploy-Reihenfolge, Test-Coverage-Anforderungen.
- **Risiken erweitert** (Sektion 8): Toggle-API-Komplexität (8.6) und JOIN-Performance (8.7) ergänzt, weil sie konkrete Implementations-Konsequenzen haben.
- **WorkTree-Rebase-Strategie konkretisiert** (8.8): exakte git-Kommandos.
- **Q6/Q7/Q8 in der Entscheidungs-Tabelle ergänzt** (Sektion 11): die Round-2-Klärungen.

---

**Hinweis:** Dieses Dokument ist ein **lebendes Roadmap-Doc**. Pro Phase werden Detail-Specs + Pläne als separate Files angelegt:
- `docs/superpowers/specs/2026-MM-DD-production-order-split-phase-N-design.md`
- `docs/superpowers/plans/2026-MM-DD-production-order-split-phase-N.md`
