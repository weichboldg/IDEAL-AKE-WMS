# ProductionOrder-Architektur: Roadmap zur Tabellen-Aufteilung

**Datum:** 2026-05-12
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
4. **Neue Rolle + Workflow** für FA-Vervollständigung (Spezifikation der Baugruppen-Inhalte).
5. **Atomare Datenmigration** (gewählte Strategie) — eine Migration kopiert Daten in neue Tabellen und droppt alte Spalten.

## 3. Out-of-Scope (für diese Roadmap)

- **Backward-Compat-Schicht** während des Refactors — wir migrieren atomar; bestehende Views werden in derselben Phase angepasst.
- **OSEON-Tracking-Refactor** — bleibt eigenständig (`OseonProductionOrders` ist separate Tabelle).
- **WorkOperations** — bleibt unverändert (Beziehung zu FA über `ProductionOrderId`).
- **Stueckliste / BOM** — Bestand bleibt unverändert; nur Phase 5 nutzt die BOM-Anzeige als read-only View.

## 4. Neue Tabellen-Architektur

### 4.1 `ProductionOrders` (schlank)

Spalten nach Refactor:
- `Id`, `OrderNumber`, `Quantity`, `Customer`, `ArticleNumber`, `Description1`, `Description2`
- `ProductionDate`, `DeliveryDate`
- `ProductionWorkplaceId` (cross-cutting — wird in mehreren Listen genutzt)
- `IsDone` (= Sage-Status, "WA in Sage erledigt"; bleibt auf der Master-Tabelle als globaler Abschlussindikator)
- `CreatedAt`, `CreatedBy`, `CreatedByWindows`, `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows`

**Entfernt** (nach Migration in neue Tabellen):
- `PickingStatus`, `PickingPriority`, `IsReleasedForPicking`, `ReleasedAt`, `ReleasedBy`, `AssignedPickerId`, `AssignedPickerName`
- `HasGlass`, `HasExternalPurchase`, `HasCoatingParts`, `IsCoatingDone`
- `HasCooling`, `HasFan`, `HasElectric`, `HasDoors`, `HasSuperstructure`

### 4.2 `ProductionOrderPickingStatus` (1:1 zu FA, optional)

Vorkommissionierungs-Status. Datensatz wird angelegt, sobald irgendein Komm-Feld ungleich Default gesetzt wird (lazy create) ODER beim Schema-Refactor für alle bestehenden FAs initialisiert.

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

**Entscheidung offen für Phase 1 Spec:** `IsDone` auf FA vs. `IsDonePicking` auf PickingStatus — siehe Sektion 5.1.

### 4.3 `ProductionOrderBdeStatus` (1:1 zu FA, optional)

BDE-/Fertigungs-Status. Heute praktisch leer; wächst in Phase 3.

Initiale Spalten:
- `Id`, `ProductionOrderId` (FK, UNIQUE)
- `IsDoneBde` (bit) — = "Auftrag komplett über BDE fertiggemeldet"
- Audit-Felder

Spätere Felder (Phase 3): Werkbank-Spezifika, Fertigungsphasen, KPIs.

### 4.4 `ProductionOrderAssemblyGroups` (1:N zu FA)

Eine Zeile pro FA × Baugruppe (max. 5 Zeilen pro FA: VK/VL/VE/VT/VA).

Spalten:
- `Id`, `ProductionOrderId` (FK)
- `GroupKey` (nvarchar(10)) — Enum-Werte: `VK`, `VL`, `VE`, `VT`, `VA`
- `IsApplicable` (bit) — = bisheriger `HasCooling` / `HasFan` / ...
- `IsCompleted` (bit) — neue Semantik: "Spezifikation für diese Gruppe ist vollständig dokumentiert"
- `CompletedAt` (datetime2?), `CompletedBy` (nvarchar(200))
- Audit-Felder

**Unique Constraint** auf (`ProductionOrderId`, `GroupKey`).

### 4.5 `ProductionOrderAssemblyGroupSpecs` (1:N zu AssemblyGroup)

Die "definierten Ausprägungen" pro Baugruppe (siehe User-Text: "Liste Kühler, mit der Liste aller definierten Ausprägungen"). Wird primär in Phase 4 + 5 genutzt.

Spalten:
- `Id`, `AssemblyGroupId` (FK)
- `ArticleGroupCode` (nvarchar(50)) oder `ArticleGroupId` (FK auf bestehende `ArticleGroup`-Tabelle — siehe Codebase-Check)
- `TextFilter` (nvarchar(500)?) — Free-Text-Spezifikation
- `Quantity` (decimal?) — wenn der Spec einen Mengen-Bedarf hat
- `Notes` (nvarchar(MAX)?)
- Audit-Felder

**Detail-Design folgt in Phase 4 Spec** — exakte Felder hängen ab vom konkreten Vervollständigungs-Workflow.

## 5. Offene Architekturfragen (für Phase-1-Detail-Spec)

### 5.1 `IsDone`-Semantik

Aktuell: ein einziges `IsDone` auf FA, das beim Komm-Abschluss UND/ODER beim BDE-Abschluss gesetzt wird. Refactor-Optionen:

- **A)** `IsDone` auf FA bleibt = Sage-Master-Flag ("WA erledigt in Sage"). `IsDonePicking` und `IsDoneBde` zusätzlich in den jeweiligen Status-Tabellen. View-Logik leitet "Auftrag wirklich abgeschlossen" aus Kombination ab.
- **B)** `IsDone` von FA entfernen, nur die spezifischen Statusse halten. Erfordert aber Aggregat-Sicht ("ist überhaupt etwas zu tun?").

Empfehlung: **A**, weil Sage IsDone als externer Trigger erhalten bleibt und App-Status orthogonal ist.

### 5.2 Lazy-Create vs. Eager-Create der 1:1-Tabellen

`ProductionOrderPickingStatus` und `ProductionOrderBdeStatus` sind 1:1-optional. Beim Schema-Refactor zwei Varianten:

- **A)** Für jede bestehende FA einen leeren Status-Datensatz anlegen (= Eager). Schema-Garantie: jede FA hat genau einen Status. Repo-Code einfacher (kein NULL-Check).
- **B)** Status erst beim ersten Set anlegen (= Lazy). Schmalere Datenmenge, aber Repo-Code muss NULL-Check + Auto-Create handhaben.

Empfehlung: **A** — bei <100k FAs ist die Daten-Inflation marginal, der Repo-Code-Vorteil ist groß.

### 5.3 Sage-Import-Job-Anpassung

`SQL/AgentJobs/01_Import_Produktionsauftraege.sql` schreibt heute in `ProductionOrders` und übergeht App-verwaltete Spalten. Nach dem Refactor:
- Job schreibt nur noch in die schlanke `ProductionOrders`-Tabelle (weniger Übergehen-Logik nötig).
- Beim INSERT eines neuen FA muss zusätzlich (im selben Batch oder nachgelagert) der PickingStatus + BdeStatus angelegt werden (wenn Eager-Strategie 5.2.A gewählt).

**Detail in Phase 1 Spec** — entweder als Trigger oder als Folge-MERGE im AgentJob.

### 5.4 UserViewPreferences-Migration

Bestehende User-Prefs referenzieren Spalten-Keys, die nach dem Refactor evtl. wandern (z. B. `glass`, `purchase` → bleiben im Picking-Leitstand-View, aber `picker`, `release` ebenfalls). Bei Phase-2 (Listen-Trennung) werden alte Prefs für `ProductionOrders` evtl. invalidiert. Optionen:
- Migration-Script: bestehende `ProductionOrders`-Pref auf `Picking/Leitstand`-Pref kopieren.
- Pref einfach behalten, alte Keys werden vom JS-Merge ignoriert (= heute schon der Fall durch `configKeySet`-Filter in [column-preferences.js](IdealAkeWms/wwwroot/js/column-preferences.js)).

## 6. Phasen-Übersicht

### Phase 1 — Schema-Refactor (atomar)
**Ziel:** Neue Tabellen + Datenmigration + alte Spalten droppen + ALLE bestehenden Konsumer auf neue Struktur umgestellt, **funktional identisch zu heute**.

**Scope:**
- 4 neue Tabellen (`PickingStatus`, `BdeStatus`, `AssemblyGroups`, `AssemblyGroupSpecs` — letzte nur leer angelegt).
- EF Migration + idempotentes SQL-Script.
- Datenmigration in der Migration: pro existierende FA werden `PickingStatus`, `BdeStatus`, und 5 `AssemblyGroups`-Zeilen (1 je VK/VL/VE/VT/VA) angelegt; Daten aus alten Spalten umkopiert.
- Anschließend `DROP COLUMN` für die 14 entfernten Spalten.
- Entity / ViewModel / Repository / Toggle-API / View komplett umgebaut.
- AgentJob aktualisiert.
- TESTSZENARIEN: kompletter Regression-Durchlauf der bestehenden FA-Funktionen.

**Aufwand:** groß. Vermutlich 7-10 Tasks im Plan.

### Phase 2 — Leitstand-Kommissionierung-View extrahieren
**Ziel:** Bestehende ProductionOrders/Index in zwei Views aufspalten:
- `ProductionOrders/Index` → schlanke FA-Liste (Sage-Master + Cross-Cutting; für allgemeine User).
- `Picking/Leitstand` (neuer Controller/Action) → die heute schon vorhandenen Komm-spezifischen Spalten + Bulk-Release + Picker-Assign.

**Scope:**
- Neuer Controller `PickingLeitstandController` oder neue Action am `PickingController`.
- ViewModel-Trennung: `ProductionOrderListItem` (schlank) vs. `PickingLeitstandItem` (mit Status-Joins).
- Permission: `[RequirePickingAccess]` für Picking-Leitstand; ProductionOrders/Index bekommt eine allgemeinere Permission.
- Routing-Update (Nav-Bar).
- TESTSZENARIEN.

**Aufwand:** mittel. 4-5 Tasks.

### Phase 3 — Leitstand-PPS-light/BDE-View
**Ziel:** Neue View `Bde/Leitstand` mit BDE-spezifischen Spalten + Funktionen (initial: IsDoneBde, Werkbank-Aggregat, später erweiterbar).

**Scope:**
- Neuer Controller `BdeLeitstandController`.
- `BdeStatus`-Tabelle wird Hauptdatenquelle (gejoint mit FA).
- Permission: `[RequireBdeShiftleadAccess]` oder neue Permission `bde_leitstand`.
- TESTSZENARIEN.

**Konkreter Funktionsumfang ist offen** — Detail-Spec in Phase 3 nach Klärung mit User.

**Aufwand:** mittel-groß, abhängig vom finalen Scope.

### Phase 4 — FA-Vervollständigung
**Ziel:** Pro FA eine neue Page mit Tabs für VK/VL/VE/VT/VA, in denen Spezifikationen (Artikelgruppen + Text-Filter) gepflegt werden.

**Scope:**
- Neue Rolle `fa_completion` (siehe Sektion 7 für Naming).
- Neuer Controller `FaCompletionController` + View `Edit.cshtml` mit Bootstrap-Tabs.
- Pro Tab: List/Add/Edit/Delete der `AssemblyGroupSpecs`.
- Artikelgruppen-Auswahl: Dropdown aus `ArticleGroup`-Master (oder freier Text).
- TESTSZENARIEN.

**Aufwand:** groß. Eigene neue UI-Schicht.

### Phase 5 — Arbeitsplatz-BOM-View
**Ziel:** Liste pro Werkbank/Arbeitsplatz mit allen FA + zugehörigen Spec-Listen; BOM-ähnliche Tree-Darstellung, **nicht für Kommissionierung verwendbar**, max. Bestellfunktion.

**Scope:**
- Neue View `Bde/AssemblyTasks` (Name TBD).
- Tree-Render: FA → Gruppe → Specs.
- Read-Only-Modus für Picking-Toggle (kein Toggle-API verfügbar).
- Bestellung-Integration aus dem bestehenden `BestellungenAktiv`-Feature.
- TESTSZENARIEN.

**Aufwand:** mittel.

## 7. Neue Rolle

Vorschlag: **`fa_completion`** (analog zu `picking`, `tracking`, `bde_user`).

- Anzeige-Name: "FA-Vervollständigung"
- AD-Gruppe: optional konfigurierbar (`Role.AdGroup`)
- Berechtigungen: Lesezugriff auf alle FAs; Schreibzugriff auf `AssemblyGroupSpecs`.
- Eingeführt in **Phase 4** (nicht früher — andere Phasen brauchen sie nicht).

Alternativen erwogen: `assembly_specifier` (Englisch konsistent zu Code), `vorbereitung` (Deutsch). Englischer Code-Name passt zum Pattern. Endgültiges Naming in Phase 4 Spec.

## 8. Abhängigkeiten

```
Phase 1 (Schema)
  │
  ├── Phase 2 (Komm-Liste)   ← parallel zu Phase 3 möglich
  ├── Phase 3 (BDE-Liste)
  └── Phase 4 (FA-Vervollständigung)
         │
         └── Phase 5 (Arbeitsplatz-BOM)
```

Phase 1 ist Blocker für alles. Phase 2 + 3 sind unabhängig voneinander, können parallel oder seriell. Phase 5 hängt von Phase 4 (Spec-Daten).

## 9. Risiken (Roadmap-Ebene)

### 9.1 Datenmigration verliert FA-Status
Im Schema-Refactor werden Daten aus 14 Spalten in 3-4 neue Tabellen kopiert. Fehler in der Migration → Status-Verlust für laufende FAs.
**Mitigation:** Migration in einer Transaktion + Verifikations-Query nach Migration (z. B. SUM(IsReleasedForPicking) vorher == COUNT(...) nachher). Backup vor Migration. Phase-1-Plan dokumentiert Rollback-Pfad.

### 9.2 Bestehende UserViewPreferences verlieren Sinn
Pref-Daten referenzieren Column-Keys, die nach Phase 2 in eine andere View wandern. Wenn ein User die Sortierung in `ProductionOrders` auf `picking-priority` gesetzt hatte, ist diese Spalte nach Phase 2 dort nicht mehr vorhanden.
**Mitigation:** Aktuelle [`mergeWithDefaults`-Logik](IdealAkeWms/wwwroot/js/column-preferences.js#L65) ignoriert unbekannte Keys bereits. Migration kann optional bestehende Prefs auf die neue View duplizieren (Phase-2-Detail).

### 9.3 Sage-Import-Job-Inkompatibilität
Job MUSS während des Refactor-Deploys angepasst werden. Wenn der alte Job nach dem Refactor noch läuft, schreibt er in nicht-mehr-existierende Spalten und schlägt fehl.
**Mitigation:** Deploy-Reihenfolge: 1. App-Stop, 2. Migration laufen lassen, 3. Neue App-Version + Neuer AgentJob deployen, 4. App-Start. Phase-1-Plan dokumentiert das.

### 9.4 Permission-Migration
Neue Rolle `fa_completion` wird in Phase 4 eingeführt. Bestehende Permission-Filter (z. B. `RequirePickingAccess`) bleiben unverändert. Risiko: doppelte Permission-Pfade.
**Mitigation:** Phase-4-Detail-Spec dokumentiert exakte Rolle-zu-Feature-Mapping. Kein Wildcard auf Admin.

### 9.5 Scope-Creep in Phase 3 (BDE)
"PPS light" ist heute kein konkretes Feature-Set, sondern eine Vision. Phase 3 droht ohne Detail-Spec zu wuchern.
**Mitigation:** Phase 3 erst starten, wenn der User den BDE-spezifischen Funktionsumfang konkret beschreibt. Heutige Roadmap markiert Phase 3 als "Funktionsumfang TBD".

### 9.6 WorkTree-Konflikte mit dem Hauptbranch
`refactor/production-order-split` zweigt vom aktuellen Bundle-Branch `feature/sage-lagerbestand-sync` ab. Wenn dieser noch wachsen sollte, ist Rebase notwendig.
**Mitigation:** Bundle-Branch sollte vor Phase-1-Start in `main` gemergt sein. Refactor zweigt dann von `main` ab oder rebased.

## 10. Ablauf

1. Roadmap committen (heute).
2. User reviewt Roadmap, gibt Phasen-Reihenfolge und Details frei.
3. Phase 1 Detail-Spec + Plan schreiben (separates Doc-Paar).
4. Pro Phase: Spec → User-Review → Plan → User-Review → Implementation → Verifikation.
5. Pro Phase eigener Sub-Branch im WorkTree (`refactor/production-order-split-phase-N`) oder direkt commits im Refactor-Branch — Entscheidung in Phase 1 Spec.

---

## 11. Sofortige Detail-Klärungen für Phase 1

Diese Punkte muss der User vor dem Phase-1-Detail-Plan beantworten:

- **(Q1)** `IsDone`-Splitting: Empfehlung Sektion 5.1.A — bestätigt?
- **(Q2)** Eager- vs. Lazy-Create der Status-Tabellen: Empfehlung Sektion 5.2.A — bestätigt?
- **(Q3)** AgentJob-Anpassung: Folge-MERGE im selben Skript ODER DB-Trigger ODER App-Logik beim Import-Read? Empfehlung: Folge-MERGE im AgentJob.
- **(Q4)** Wann startet Phase 1? Direkt oder erst nach Merge des aktuellen Bundles in `main`?
- **(Q5)** AssemblyGroups: 5 Zeilen pro FA (alle Gruppen immer angelegt) ODER nur die Gruppen mit `IsApplicable=true`?

---

**Hinweis:** Dieses Dokument ist ein **lebendes Roadmap-Doc**. Pro Phase werden Detail-Specs + Pläne als separate Files angelegt:
- `docs/superpowers/specs/2026-MM-DD-production-order-split-phase-N-design.md`
- `docs/superpowers/plans/2026-MM-DD-production-order-split-phase-N.md`
