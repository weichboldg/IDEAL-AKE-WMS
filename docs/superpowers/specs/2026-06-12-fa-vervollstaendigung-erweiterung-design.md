# FA-Vervollstaendigung-Erweiterung (v1.22.0) — Design

**Datum:** 2026-06-12
**Status:** approved (Design-Review + kritische Pruefung durch User bestaetigt)
**Vorgaenger:** FA-Vervollstaendigung v1.13.0 (AssemblyGroupSpecs), ProductionOrder-Split v1.11.0

## 1. Kontext & Ziel

Die FA-Vervollstaendigung (Phase 4) arbeitet heute mit 5 fixen Vorbau-Kacheln
(`ProductionOrderAssemblyGroups`, GroupKey VK/VL/VE/VT/VA, `IsApplicable`/`IsCompleted`)
und Freitext-Specs. Die Kacheln werden in der Praxis behelfsmaessig als Erledigt-Marker
verwendet. Diese Erweiterung ersetzt das System durch:

1. **Arbeitsgaenge-Katalog** (Stammdaten, erweiterbar) mit Mapping-Suchstring fuer
   automatische Erkennung aus der Stueckliste
2. **FA-zu-AG-Tabelle** (welche Arbeitsgaenge braucht ein FA) — befuellt durch
   Sync-Erkennung + manuelle Pflege; spaeter auch Basis fuer BDE
3. **Strukturierte Vorbau-Merkmale** (Verdampfergroesse, Leitungsausgang, ...) als
   konfigurierbare Dropdowns/Booleans, je Arbeitsgang zugeordnet
4. **FA-Abarbeitungsliste** je Werkbank (ersetzt die Papier-Liste) mit Erledigt-Haken
   je AG und Absprung in eine read-only Stueckliste

Vorbauten zur Erinnerung: VK Kuehlung, VL Lueftung, VE Elektro, VT Tueren, VA Aufbau.

## 2. Datenmodell (8 neue Tabellen)

Alle Tabellen erben `AuditableEntity` (ausser N:M-Junctions, die nur CreatedAt/By fuehren
wie `UserRole`). Namen im Code englisch, UI-Labels deutsch.

### 2.1 `WorkSteps` — Arbeitsgaenge-Katalog

| Spalte | Typ | Beschreibung |
|---|---|---|
| `Code` | NVARCHAR(20), unique | z.B. "VL" |
| `Name` | NVARCHAR(100) | z.B. "Lueftung" |
| `SearchString` | NVARCHAR(500), null | kommaseparierte Suchbegriffe fuer BOM-Matching, z.B. "Luefter,Ventilator". Leer = keine automatische Erkennung |
| `SortOrder` | INT | Anzeige-Reihenfolge |
| `IsActive` | BIT | deaktivieren statt loeschen |

Loesch-Guard: WorkStep mit referenzierenden `FaWorkSteps`/Mappings ist nicht loeschbar
(App-Layer-Pruefung, DB-FK NO ACTION) — nur deaktivierbar.

### 2.2 `FaWorkSteps` — FA-zu-AG

| Spalte | Typ | Beschreibung |
|---|---|---|
| `ProductionOrderId` | INT FK → ProductionOrders, ON DELETE CASCADE | |
| `WorkStepId` | INT FK → WorkSteps, NO ACTION | |
| `IsCompleted` | BIT | Erledigt-Status (Abarbeitungsliste + FaCompletion) |
| `CompletedAt` / `CompletedBy` | DATETIME2 / NVARCHAR(200), null | gesetzt beim Haken |
| `Source` | NVARCHAR(20) | "Sync" oder "Manual" |
| `IsRemoved` | BIT, default 0 | Soft-Delete: manuell abgewaehlt — Sync darf NICHT re-adden |

Constraints: UNIQUE (`ProductionOrderId`, `WorkStepId`) — genau eine Zeile je FA+AG,
`IsRemoved` toggelt sie. Indexe: (`WorkStepId`), (`ProductionOrderId`, `IsRemoved`).
Manuelles Wieder-Hinzufuegen reaktiviert die Zeile (`IsRemoved=0`, `Source=Manual`).

### 2.3 `FaWorkStepSpecs` — Freitext-Specs (Nachfolger AssemblyGroupSpecs)

Gleiche Felder wie heute (`Description` required, `ArticleId` FK null/SET NULL,
`Quantity`, `Notes`, `SortOrder`), FK `FaWorkStepId` → FaWorkSteps ON DELETE CASCADE.

### 2.4 `FaAttributeDefinitions` — Merkmal-Katalog

| Spalte | Typ | Beschreibung |
|---|---|---|
| `Name` | NVARCHAR(200) | z.B. "Verdampfergroesse" |
| `AttributeType` | INT | Enum wie ArticleAttributes: Boolean=0, Dropdown=1 |
| `SortOrder` | INT | |
| `IsActive` | BIT | |

### 2.5 `FaAttributeOptions` — Dropdown-Werte

`FaAttributeDefinitionId` FK CASCADE, `Value` NVARCHAR(200), `SortOrder`, `IsActive`.
Option mit referenzierenden Values ist nicht loeschbar (App-Guard) — nur deaktivierbar,
damit Techniker-Eingaben nie still verschwinden.

### 2.6 `FaAttributeWorkSteps` — Merkmal→AG-Zuordnung (N:M)

`FaAttributeDefinitionId` FK CASCADE + `WorkStepId` FK CASCADE, UNIQUE beides.
Ein Merkmal kann mehreren AGs zugeordnet sein; ohne Zuordnung erscheint es nirgends.

### 2.7 `FaAttributeValues` — eingegebene Werte

| Spalte | Typ | Beschreibung |
|---|---|---|
| `ProductionOrderId` | INT FK CASCADE | |
| `FaAttributeDefinitionId` | INT FK CASCADE | |
| `SelectedOptionId` | INT FK → FaAttributeOptions, NO ACTION, null | bei Dropdown |
| `BooleanValue` | BIT, null | bei Boolean |

UNIQUE (`ProductionOrderId`, `FaAttributeDefinitionId`). **Wert gilt je FA** — auch wenn
das Merkmal mehreren AGs zugeordnet ist, wird es einmal erfasst und ueberall angezeigt.
Keine Wert-Zeile = "leer" (Dropdowns haben implizit eine Leer-Option).

### 2.8 `ProductionWorkplaceWorkSteps` — Werkbank→AG-Mapping (N:M, mehrfach)

`ProductionWorkplaceId` FK CASCADE + `WorkStepId` FK CASCADE, UNIQUE beides.

### 2.9 Werkbank am FA

KEIN neues Feld: die Techniker-Werkbank-Auswahl schreibt das bestehende
`ProductionOrders.ProductionWorkplaceId` (vorbelegt wenn OSEON/Sync sie liefert).

## 3. Migration & Cutover (daten-erhaltend, deploy-kritisch)

EF-Migration + idempotentes `SQL/68_FaWorkStepsAndAttributes.sql` (naechste freie Nummer)
mit OBJECT_ID-Guards. Reihenfolge:

1. Neue Tabellen anlegen (Abschnitt 2)
2. **Seed `WorkSteps`**: VK/Kuehlung, VL/Lueftung, VE/Elektro, VT/Tueren, VA/Aufbau
   (SortOrder 1-5, SearchString leer — wird nachgepflegt)
3. **Konvertierung** `ProductionOrderAssemblyGroups` → `FaWorkSteps`:
   - Zeilen mit `IsApplicable=1` ODER mit vorhandenen Specs werden uebernommen
   - `IsRemoved = CASE WHEN IsApplicable=1 THEN 0 ELSE 1 END` (Specs an inaktiven
     Gruppen bleiben so erhalten, ohne den AG zu aktivieren)
   - `IsCompleted` wird uebernommen, `Source='Manual'`
4. Specs kopieren: `AssemblyGroupSpecs` → `FaWorkStepSpecs` (FK auf neue FaWorkSteps-Zeile
   desselben FA+GroupKey)
5. Alte Tabellen droppen (`ProductionOrderAssemblyGroupSpecs`, `ProductionOrderAssemblyGroups`)
6. **Seed Merkmale** (alle ohne AG-Zuordnung, wird nachgepflegt):
   - Verdampfergroesse (Dropdown): UKW 2/1, UKW 3/1, UKW 4/1, UKW 5/1 (Euro 4), UKW 6/1,
     Euro 2, Euro 3, Caleo 80, Caleo 120, Breite 60
   - Leitungsausgang (Dropdown): Standard, RG, Links, Links RG
   - Verdampfergehaeuse (Dropdown): 2/1 Standard, 3/1 RG, Sonder
   - Ventil aussenliegend (Boolean)
7. **Seed Rolle `vorbau`** (IsSystem=1, naechster SortOrder)
8. `SQL/00_FreshInstall.sql`: alte CREATE-Bloecke (Zeilen ~313-374) ersetzen, neue Tabellen
   + Seeds + MigrationId-Insert — BEIDE Pflicht-Stellen (bekannter Fallstrick)

**Cutover (Wartungsfenster, eigenes Cutover-Dokument unter `docs/superpowers/cutover/`):**
`SQL/AgentJobs/01_Import_Produktionsauftraege.sql` Folge-MERGE 3 (Zeilen 127-143, eager
AssemblyGroups via CROSS JOIN) wird ERSATZLOS entfernt — FaWorkSteps entstehen nur noch
via Sync-Erkennung oder manuell. Job-Update MUSS im selben Fenster wie das DB-Deploy
laufen, sonst schlaegt der gesamte FA-Import fehl (PickingStatus/BdeStatus-MERGEs stecken
im selben Skript). **DB-Backup vor Deploy** (wie v1.19).

## 4. Stammdaten-Pflege (3 Stellen)

1. **Neue View "Arbeitsgaenge"** (`WorkStepsController`, Stammdaten-Menue):
   Class-Level `[RequireMasterDataReadAccess]`, Edit-Actions `[RequireMasterDataAccess]`
   (Read/Edit-Pattern v1.20). CRUD inkl. SearchString-Spalte. Listen-Pflicht-Pattern
   (Pagination + Server-Spaltenfilter).
2. **Neue View "FA-Merkmale"** (`FaAttributesController`, analog ArticleAttributes als
   Vorlage): Definitionen + Optionen + Checkbox-Liste "zugeordnete Arbeitsgaenge".
   Gleiche Filter-Attribute wie 1.
3. **Werkbank-Edit erweitert** (`ProductionWorkplacesController.Edit`): Multi-Select
   "Vorbaugruppen/Arbeitsgaenge dieser Werkbank" (Checkbox-Liste der aktiven WorkSteps).

## 5. Sync: FA-zu-AG-Erkennung (eigener idempotenter Schritt)

**WICHTIG (Korrektur aus kritischer Pruefung):** Das Matching haengt NICHT am
BomCache-Insert-Pfad — der skippt Artikel mit unveraendertem `ContentHash`, neue FAs
zu bereits gecachten Artikeln bekaemen sonst nie eine Erkennung.

Stattdessen: **`FaWorkStepDetectionService`** (Service-Projekt, eigene Klasse), laeuft im
SyncWorker direkt NACH dem BomCache-Sync. Eigenes Setting `Sync:FaWorkStepDetectionEnabled`
(Default `false`), eigener `SyncLogServices`-Eintrag "FaWorkStepDetection"
(ISyncLogger als letzter Ctor-Parameter, Konvention v1.15.2).

Ablauf je Lauf (set-basiert, idempotent):

1. Aktive `WorkSteps` mit nicht-leerem `SearchString` laden; Begriffe per `,` splitten + trimmen
2. Offene FAs (`IsDone=0`) mit `ArticleNumber` + vorhandenem `CachedBomHeaders`-Eintrag
3. Match: ein Begriff kommt case-insensitive per Contains in `Bezeichnung1` ODER
   `Bezeichnung2` eines `CachedBomItems` des Artikels vor
4. Insert `FaWorkSteps` (`Source='Sync'`) NUR wenn fuer FA+WorkStep noch KEINE Zeile
   existiert (auch keine mit `IsRemoved=1` — manuelle Abwahl gewinnt). Niemals entfernen
5. Counts ins Aktivitaets-Protokoll: `neu`, `uebersprungen`

Bewusste Eigenschaften: False-Positives sind akzeptiert ("Luefter" matcht auch
"Luefterabdeckung") — Techniker waehlt ab. FAs ausserhalb des BomCache-Fensters
(8 Wochen / 200 FAs) werden nicht automatisch erkannt — manuelle Pflege immer moeglich.
Suchstring-Aenderungen wirken ab dem naechsten Lauf (kein Rueckwirkungs-Rescan noetig).

## 6. FA-Vervollstaendigen-Umbau (`FaCompletionController`)

`Edit`-View zeigt kuenftig:

- **Werkbank-Dropdown** (FA-Feld `ProductionWorkplaceId`): vorbelegt wenn vom Sync
  geliefert, sonst Auswahl aus aktiven ProductionWorkplaces; Speichern schreibt das FA-Feld
- **AG-Kacheln aus `FaWorkSteps`** statt der 5 fixen: angezeigt werden Zeilen mit
  `IsRemoved=0` (Sync-erkannt + manuell); "AG hinzufuegen"-Dropdown aus dem Katalog
  (reaktiviert ggf. `IsRemoved`-Zeile); Abwaehlen setzt `IsRemoved=1`
- **Pro AG strukturierte Merkmale**: die via `FaAttributeWorkSteps` zugeordneten
  Definitionen als Dropdown-/Ja-Nein-Inputs, Wert je FA (`FaAttributeValues`),
  Dropdowns mit Leer-Option
- **Freitext-Specs bleiben** (jetzt an `FaWorkSteps` haengend), CRUD wie bisher
- `ToggleIsCompleted` arbeitet auf `FaWorkSteps.IsCompleted`

`Index`-View: Pivot-Zaehler (Applicable/Completed/Specs) auf FaWorkSteps umgestellt.
Hinweis-Badge "keine Werkbank zugewiesen" wenn `ProductionWorkplaceId` leer.

API-Umstellung: `/api/assembly-groups/toggle-applicable` wird ersetzt durch
`/api/fa-work-steps/toggle` (Body: ProductionOrderId, WorkStepCode, Value; legt Zeile
an / setzt IsRemoved). Filter bleibt `[RequirePickingOrFaCompletionAccess]`.

## 7. FA-Abarbeitungsliste (NEU)

`FaWorklistController` + `Views/FaWorklist/Index.cshtml`, Rolle **`vorbau`** (neu) via
`[RequireVorbauAccess]` (admin, vorbau). Feature-Flag: bestehendes `FaCompletionAktiv`.

- **Pflicht-Filter Werkbank** (Dropdown): Liste zeigt offene FAs (`IsDone=0`; der
  Komm-Status `IsDonePicking` ist fuer Vorbau irrelevant) mit
  `ProductionWorkplaceId = gewaehlte Werkbank`, die mind. einen offenen (nicht erledigten,
  `IsRemoved=0`) AG aus dem **Werkbank-Mapping** haben
- **AG-Spalten = gemappte AGs der Werkbank**; pro Zeile Erledigt-Checkbox je benoetigtem AG
  (AJAX POST `/api/fa-work-steps/toggle-completed`, `[RequireVorbauAccess]`; Pattern wie
  Leitstand-Toggles). Nicht benoetigte AGs: leere Zelle
- **1 Zeile pro FA** (wie Papier-Vorlage): FA-Nr (Link → read-only Stueckliste),
  enaio-Badges (Wiederverwendung `EnaioDmsDocumentRepository.GetByOrderNumbersAsync` +
  Render wie FA-Liste), Artikelnummer, Stk, BG-Termin / Komm-Termin / Fert-Termin
  (gleiche Berechnung + KW-Format wie FA-Liste), **Merkmal-Spalten** (aktive Definitionen,
  die mind. einem gemappten AG der Werkbank zugeordnet sind; Werte aus FaAttributeValues)
- **Orphan-AG-Hinweis**: Badge "+N weitere AGs" in der Zeile, wenn der FA offene AGs hat,
  die NICHT zu den Spalten dieser Werkbank gehoeren (Luecke aus Filter-Option 2 sichtbar
  machen, ohne die Filterlogik aufzuweichen)
- Komplett erledigte FAs (alle benoetigten AGs der Werkbank `IsCompleted`) verschwinden
  aus der Default-Ansicht; Toggle "Erledigte anzeigen"
- Listen-Pflicht-Pattern: Pagination + Server-Spaltenfilter + `data-date-filter` auf
  den 3 Terminspalten

**Read-only Stueckliste:** Route `FaWorklist/Bom/{id}` (`[RequireVorbauAccess]`) rendert
die bestehende `Picking/Bom`-View mit `ReadOnly`-Flag im ViewModel: ausgeblendet werden
Picking-Checkboxen, Quell-/Ziel-Lagerplatz-Dropdowns, Umbuchen-Button, Foto-Upload und
Bedarfsmeldungs-Modal. Filter, Baugruppen-Navigation und Druck bleiben. Die bestehende
`Picking/Bom`-Route + `[RequirePickingAccess]` bleiben unangetastet.

## 8. Leitstand-Umstellung (bewusst minimal)

Die 5 Spalten VK/VL/VE/VT/VA bleiben **statisch** (Bool-Properties HasCooling/HasFan/...,
Spaltenfilter-Keys cooling/fan/electric/doors/superstructure unveraendert). Datenquelle
wird das FaWorkSteps-Pivot (`GetWorkStepPivotAsync` ersetzt `GetIsApplicablePivotAsync`,
**Chunking-Pattern in 1000er-Bloecken uebernehmen** — SQL-2100-Parameter-Limit).
Die Leitstand-Toggle-Checkboxen rufen den neuen Endpoint `/api/fa-work-steps/toggle` auf
(legt Zeile an bzw. setzt IsRemoved). Dynamische Spalten fuer kuenftige sechste AGs sind
bewusst NICHT Teil dieses Rollouts (YAGNI — siehe §11).

## 9. Menue, Rollen, Feature-Flag

- Menue-Block **"Fertigungsauftraege"**: FA-Liste (bestehend), **FA-Vervollstaendigen**
  (verschoben aus bisherigem Standort), **FA-Abarbeitungsliste** (neu, vorbau)
- Neue Rolle `vorbau` ("FA-Abarbeitungsliste: Vorbau-AGs einsehen und abhaken"):
  Migration-Seed, `RoleKeys.cs`, `RequireVorbauAccessAttribute` (TypeFilter-Pattern wie
  v1.20), **`Views/Users/RoleOverview.cshtml` mitpflegen (hand-gepflegt!)**, CLAUDE.md
  Zugriffsschutz-Tabelle
- Feature-Flag: `FaCompletionAktiv` gated Vervollstaendigen UND Abarbeitungsliste
  (kein zweites Setting)

## 10. Edge-Cases

- **FA ohne Werkbank**: erscheint in keiner Werkbank-Liste; Badge in FaCompletion
  (Index + Edit). Bewusst KEINE "(ohne Werkbank)"-Pseudo-Auswahl im ersten Wurf
- **Merkmal-Umzuordnung**: Werte haengen am FA und bleiben erhalten; Anzeige folgt
  der aktuellen Zuordnung
- **WorkStep deaktiviert**: bestehende FaWorkSteps bleiben sichtbar/abhakbar; Katalog
  und Erkennung bieten ihn nicht mehr an
- **Option deaktiviert**: bestehende Werte zeigen den Wert weiter an (Option bleibt
  als Zeile erhalten); Dropdown bietet sie nicht mehr an
- **Gleichzeitiges Abhaken** (Abarbeitungsliste + FaCompletion): letzter Schreiber
  gewinnt — unkritisch, beide setzen denselben Bool + Audit

## 11. Bewusst NICHT in diesem Rollout (spaeter)

- BDE-Anbindung der FaWorkSteps (Buchen auf Vorbau-AGs) — Tabellen sind dafuer vorbereitet
- Dynamische Leitstand-Spalten fuer AGs jenseits der 5 geseedeten
- Nachschaerfen der Abarbeitungslisten-Filterlogik (User-Vorbehalt: "muessen wir dann
  nochmals drueber arbeiten")
- "(ohne Werkbank)"-Sicht in der Abarbeitungsliste

## 12. Tests (TDD je Schicht)

- **Repo**: FaWorkSteps CRUD + IsRemoved-Reaktivierung, Pivot-Chunking, Werkbank-Mapping-
  Query, Values-Upsert (unique FA+Definition), Options-Loesch-Guard
- **Service**: Detection — Mehrfach-Begriffe (OR), Case-Insensitivity, Re-Add-Sperre bei
  IsRemoved, kein Insert ohne BOM-Cache, SyncLog-Lifecycle (FakeSyncLogger)
- **Controller**: FaWorklist Werkbank-Filter + Orphan-Badge + Erledigt-Toggle + Spalten-
  filter; FaCompletion Edit-Speichern (Werkbank + Merkmale); Toggle-API (Whitelist
  aktiver WorkSteps, Berechtigung)
- **Migration**: Konvertierungs-Smoke (IsApplicable/Specs-Erhalt inkl. Specs an inaktiven
  Gruppen)
- **Doku**: TESTSZENARIEN-Kapitel (End-to-End: Suchstring pflegen → Sync → Vervollstaendigen
  → Abarbeitungsliste → Abhaken), Changelog v1.22.0, PROJECT_STATUS, CLAUDE.md
  (Zugriffsschutz + Fallstricke: AgentJob-Cutover, Detection-Idempotenz)

## 13. Umsetzung

Ein Rollout **v1.22.0** in **neuem Worktree** — Start erst NACH Merge des laufenden
Branches `bugfix/missingparts-include-pd` (v1.19–v1.21.1) in `main`. Plan via
`superpowers:writing-plans`, Ausfuehrung subagent-getrieben, Cutover-Dokument fuer das
Deploy-Fenster (AgentJob!).
