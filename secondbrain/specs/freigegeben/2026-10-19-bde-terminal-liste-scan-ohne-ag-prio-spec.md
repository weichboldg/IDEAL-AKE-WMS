---
type: spec
title: "BDE-Terminal: Arbeitsgang-Liste übersichtlich, Scan ohne AG, Prio"
slug: 2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-spec
status: Testbereit
created: 2026-10-19
updated: 2026-10-19
source_backlog: "[[2026-10-19-bde-terminal-liste-und-scan-ohne-ag]]"
depends_on: "[[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec]]"
task: "[[2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
version: "v1.64.0"
testszenario: "TS-97"
affected_code:
  - "IdealAkeWms/Services/ArbeitsgangStatusRegel.cs (BdeTerminalAuftraege.Ordnen :50-70 Gruppen-Sortierung Prio/Termin, Record BdeTerminalArbeitsgang :35 + Prio)"
  - "IdealAkeWms/Controllers/BdeApiController.cs (GetAvailableOperations JSON :299-318 + prio; sonst unveraendert)"
  - "IdealAkeWms/Services/BdeScanResolver.cs (ResolveAsync :107-115 NotInScope auch fuer existierende Sub-FA; Klassenkommentar :19-29)"
  - "IdealAkeWms/Controllers/BdeTerminalController.cs (Index :36-63 ViewBag Hierarchisch + MaxHauptFa; Ctor + IServiceSettingRepository)"
  - "IdealAkeWms/Views/BdeTerminal/Index.cshtml (#actionPanel :107-108 in die linke Spalte unter #operationsCard; Hidden-Inputs :199-203; Platzhalter :76)"
  - "IdealAkeWms/wwwroot/js/bde-terminal.js (Scan :161-184, resolveScanAndSelect :190-214, renderScanSelection :238-261, renderOrderBlocks :507-529, bindOperationButtonHandlers :531-564, renderState :290-304)"
  - "IdealAkeWms/wwwroot/css/bde.css (Gruppenkopf, Auswahl-Markierung; :22-40)"
  - "IdealAkeWms/Services/BdeBookingService.cs (Mengen >= 0 in PauseAsync :149, ReportCompletionAsync :262, FinishAsync :321, ReportPartialQuantityAsync :335, FinishGroupAsync :347)"
  - "IdealAkeWms/Models/ViewModels/BdeBookingEditViewModel.cs (:17-25 [Range] auf GoodQuantity/ScrapQuantity) + Views/BdeBookings/Edit.cshtml (:94 min=0 + Validierungsausgabe)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs (FaArbeitsschrittSymbole.NamenLookup :269-274)"
  - "IdealAkeWms/Models/AppSettingKeys.cs (neu BdeTerminalMaxHauptFa) + Program.cs (BDE-Seed :383-406) + Views/Settings/Index.cshtml (:46 Gruppe BDE)"
  - "IdealAkeWms/Views/Help/Index.cshtml (Andere Listen :243-249; Terminal-Bedienung :1130-1150; Kuerzel-Familie :1203-1208)"
  - "Tests: BdeTerminalAuftraegeTests, BdeApiControllerTests, BdeScanResolverTests, BdeBookingServiceTests/BdeBookingServiceStufe1Tests, BdeTerminalControllerTests, BdeBookingsControllerTests, FaArbeitsschrittSymboleTests"
  - "AppVersion.cs x2 (v1.64.0), Views/Help/Changelog.cshtml, README.md (AppSettings-Tabelle), docs/TESTSZENARIEN.md (TS-97)"
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-10-19"
---

> [!info] Quellen und Code-Stand
> Backlog [[2026-10-19-bde-terminal-liste-und-scan-ohne-ag]] (Entscheidungen Gerald 2026-10-19, gesetzt). Vorgänger
> [[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec]] (v1.63.0, Testbereit, QA-Befund Q4 und Umsetzungs-Befund
> `NamenLookup`). Fallstricke §25, §26, §28. **Alle Belege: Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`,
> Stand v1.63.0 (`b3daf566`), nur gelesen.** Pfade relativ zu `IdealAkeWms/`. Umsetzung im selben Bündel-Worktree vor dem Merge.

> [!important] Freigabe-Nachtrag 2026-10-19 (geht dem Rumpf vor)
> Antworten 1–3 wie empfohlen (Block „Freigabe-Antworten“): Prio aufsteigend, Kombigerät kleinster Wert; NurFA-Liste unverändert;
> Markierung, Knopfposition und Scan ohne AG auch bei AKE, Gruppierung und Grenze nur bei Master an. Befund P1–P3 siehe FA-4.6:
> Sage-Barcode = Haupt-FA → der Standardfall am Terminal ist „Haupt-FA scannen, Gruppe klappt auf, Sub-FA/AG antippen“.

## Ziel / Nutzen (das Warum)

Am IDEAL-Terminal „Entgraten Fläche“ (Werkbank-Id 65) stehen seit v1.63.0 alle offenen EG-Arbeitsgänge der Kürzel-Familie in
**einer** langen Liste, ein Block je Sub-FA (`wwwroot/js/bde-terminal.js:507-529`). Laut v1.63.0-Spec hängen an EG 93 Aufträge.
Ein angetippter AG wird nicht markiert, und die Knöpfe „Rüsten/Produktion starten“ erscheinen in `#actionPanel` unter **beiden**
Spalten (`Views/BdeTerminal/Index.cshtml:107-108`, gefüllt in `bde-terminal.js:298-301`). Bei langer Liste liegen sie außerhalb
des Sichtbereichs. Gerald bestätigt: „war ganz unten“. Dazu verlangt der Scan im Normal-Modus `FA,AG` (`bde-terminal.js:178-182`),
obwohl Sage nach Geralds Erinnerung nur die Haupt-FA druckt und das Kürzel durch die Werkbank feststeht.

Die Spec macht das Terminal bei IDEAL bedienbar: Gruppierung nach Haupt-FA mit Prio, eine sichtbare Auswahl, Knöpfe an der Liste,
Scan ohne AG. Dazu kommen drei kleine Restpunkte aus v1.63.0 (negative Mengen, FA-Baum-Name, Hilfetext).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Terminal-Liste (Normal-Modus, Master an): nach Haupt-FA gruppiert, höchstens N Haupt-FAs (neue AppSetting
   `BdeTerminalMaxHauptFa`, Standard 5), Rest über „Weitere anzeigen“. Sortierung Prio, dann Fertigungstermin.
2. Klick auf einen Haupt-FA klappt seine Sub-FAs mit ihren AGs auf. Die AG-Menge ist dieselbe wie heute (Kürzel-Familie).
3. Gewählter AG sichtbar markiert, auch ohne Farbe erkennbar (`aria-pressed`). Aktionsknöpfe direkt unter der Karte
   „2. Arbeitsgang wählen“.
4. Scan ohne AG (Normal-Modus): Haupt-FA-Scan klappt dieselbe Gruppe auf wie der Klick. Sub-FA-Scan wählt direkt, wenn eindeutig.
   `FA,AG` bleibt gültig. Eine kleine Server-Änderung in `BdeScanResolver` (Begründung unter FA-4.5).
5. Negative Mengen an allen BDE-Mengeneingängen serverseitig abweisen (Befund Q4 aus v1.63.0).
6. FA-Baum: Werkbank-Name am Symbol auch bei einem Kürzel an mehreren Werkbänken.
7. Hilfeseite, Absatz „Andere Listen“, korrigieren. Dazu Terminal-Hilfe, Changelog, README, v1.64.0 und TS-97.

**Out-of-Scope**
- Zwei gleichzeitige Fertigmeldungen desselben AG (Q5 aus v1.63.0, braucht Schema): eigene Notiz laut Backlog.
- Lastenheft Stufe 2 (Nr. 9, 2, 17, 7), Grob-/Feinplanung.
- Hardware-Scanner-Präfix: [[2026-10-15-scan-handler-hardware-scanner-spec]] (danach; übernimmt das Format ohne AG).
- NurFA-Liste (`BdeApiController.cs:216-253`): unverändert, vorbehaltlich Rückfrage 2.
- Ein DB-CHECK-Constraint „Menge ≥ 0“. Bestandsdaten sind unbekannt und eine Migration würde an ihnen scheitern. Die Prüfung läuft
  in der Anwendung (FA-5). Ein Constraint kann eine eigene Spec nach Prüfabfrage P6 sein.
- Zusatzbefund, nur gemeldet: Der Scan schließt AGs aus, die **irgendein** Werker gerade bucht (`BdeScanResolver.cs:186-189`). Die
  Liste zeigt AGs anderer Werker dagegen als „läuft“ (`BdeApiController.cs:284-286`, Fallstrick §25 „Terminal zeigt fremd gebuchte
  Arbeitsgänge“). Mit `BdeMehrfachBuchungProArbeitsgang` an kann man so einen AG antippen, aber nicht scannen. Das ist Bestand und
  wird hier nicht geändert.
- Zusatzbefund, nur gemeldet: `currentWorkOp` bleibt nach Start oder Beenden gesetzt (`bde-terminal.js:233`, `:548`; nur
  `reportCompletion` setzt es zurück, `:355`). Das wird hier nicht geändert, weil der Setup→Produktion-Übergang davon abhängt
  (`bde-terminal.js:307`, `:334`).

## Fachliche Anforderungen

### Ist-Stand (belegt)

| Thema | Beleg |
|---|---|
| Terminal-Sortierung heute: Fertigungstermin (Datumsteil, leer zuletzt), dann `OrderNumber`, `SubOrderNumber`, `ProductionOrderId`, `Sequence` | `Services/ArbeitsgangStatusRegel.cs:50-70` (`BdeTerminalAuftraege.Ordnen`), Aufruf `Controllers/BdeApiController.cs:297-298` |
| Fertigungstermin: IDEAL frühester nicht leerer `FE_Termin` der Kopfzeilen zur HauptFA (`int.TryParse(OrderNumber)`), sonst `ProductionDate` | `ArbeitsgangStatusRegel.cs:45-46` |
| Kopfzeilen werden schon geladen, je Aufruf eine Abfrage | `BdeApiController.cs:291-293` → `Data/Repositories/FaHierarchyOrderInfoRepository.cs:39-47` (`keys.Contains(o.HauptFA)`) |
| **Prio wird heute am Terminal nicht gelesen** | Grep `\.Prio\b` im Web: nur `ProductionOrdersController.cs:301`, `:309`, `:326`, `:233`, `:533` und Views der FA-Liste. Kein Treffer in `BdeApiController`/`BdeTerminalAuftraege` |
| **Schreibweg Prio:** nur der Struktur-Import aus der Sage-View FAInfos, Full-Refresh | `IDEALAKEWMSService/Services/FaHierarchySyncService.cs:265-306` (`Prio = IntN(r, oPrio)` `:301`), SQL `IDEALAKEWMSService/Services/FaHierarchySql.cs:153-161` (`i.[Prio]`, alle Zeilen, eine je Montage-Abteilung) |
| **Prio hängt nur am Haupt-FA**, Sub-FAs haben keine eigene | `Models/FaHierarchyOrderInfo.cs:17` (Schlüssel `HauptFA`), `:39` (`int? Prio`), keine Sub-FA-Spalte. Sub-FAs „erben“ über `ProductionOrder.OrderNumber` = HauptFA (`Models/ProductionOrder.cs:15-17`). Die FA-Liste setzt die Gruppen-Prio auf jede Sub-FA-Zeile, aber nur bei genau einer Kopfzeile (`ProductionOrdersController.cs:294-313`, `:331-336`). Bei Kombigeräten (mehrere Kopfzeilen) bleibt sie leer (`:314-329`) |
| Prio-Bedeutung laut View-Doku nur „Prioritaet“, Typ `int` | `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAInfos.sql:14`, `:34`. Die Richtung (1 = dringend?) steht nirgends im Code → Rückfrage 1 |
| Haupt-/Sub-FA im WMS: `OrderNumber` = Sage-HauptFA, `SubOrderNumber` = Sub-FA; AKE flach mit `SubOrderNumber == OrderNumber` | `Models/ProductionOrder.cs:12-22`, `IsHauptFa` `:29-30` |
| Terminal rendert heute je **ProductionOrder** einen Block, alle untereinander, ohne Grenze | `bde-terminal.js:507-529` (Wechsel bei `productionOrderId`) |
| AG-Klick setzt `currentWorkOp`, markiert nichts | `bde-terminal.js:547-550`; `.bde-ag-btn` setzt die `--bs-btn-active-*`-Variablen auf die Statusfarben (`wwwroot/css/bde.css:33`). Eine Bootstrap-`.active`-Klasse wäre also **nicht** sichtbar |
| Aktionsknöpfe in `#actionPanel` nach der zweispaltigen Zeile | `Views/BdeTerminal/Index.cshtml:107-108`, gefüllt `bde-terminal.js:290-304` |
| Scan Normal-Modus verlangt zwei Teile | `bde-terminal.js:178-182` (Meldung „Format: FA-Nummer,AG-Nummer …“) |
| Server nimmt AG optional | `BdeApiController.cs:85` (`string? opNumber`), `BdeScanResolver.cs:181-182` (Filter nur, wenn gesetzt) |
| Scan-Auflösung: 1. `OrderNumber` in der Familie → Exact/Ambiguous; 2. HauptFA existiert → NotInScope; 3. Fallback `SubOrderNumber` → Exact/Ambiguous mit `viaSubOrderFallback`; 4. NotFound | `BdeScanResolver.cs:95-115`, Familie `:167-169`, Antwort `BdeApiController.cs:95-112` |
| Lücke im Fallback: Existiert die Sub-FA, hat aber in der Familie keinen offenen AG, kommt **NotFound** („Arbeitsgang nicht gefunden“) statt NotInScope | `BdeScanResolver.cs:108-115`: nach leerem Schritt 3 direkt `NotFound`; JS `bde-terminal.js:211-212` |
| Scan-Kandidaten ⊆ Listenangebot (gleiche Familie, verpackt raus, fertig raus; Scan nimmt zusätzlich fremd laufende raus) | `BdeScanResolver.cs:170-189` vs. `BdeApiController.cs:256-286` |
| Terminal-Modus NurFA kommt aus der AppSetting, als Hidden-Input ans JS | `Controllers/BdeTerminalController.cs:56-58`, `Index.cshtml:201`, `bde-terminal.js:18` |
| Master `ProduktionsauftragHierarchisch` ist ein **ServiceSetting**, gelesen über `IServiceSettingRepository` | `ProductionOrdersController.cs:22`, `:241-243`; Key `Services/HierarchischeStruktur/HierarchischeStrukturKeys.cs:10` |
| **BDE-Einstellungen sind AppSettings** (nicht `ServiceSettingDefinitions`): Seed beim Start, Gruppe „BDE“ auf `/Settings` | `Program.cs:383-406` (Seed nur `if (!Any)`), `Models/AppSettingKeys.cs:9-15`, `Views/Settings/Index.cshtml:46`; Int-Lesen `Data/Repositories/AppSettingRepository.cs:26-30` (`GetIntValueAsync`, Fallback bei Nicht-Zahl). Präzedenz für einen AppSetting-Key **nur** im Startup-Seed, ohne SQL-Skript: `FaHierarchyMaxTiefe` (`Program.cs:416`, kein Treffer unter `SQL/`) |
| FA-Baum-Name: Kürzel an mehreren aktuellen Werkbänken fällt aus dem Lookup | `Models/ViewModels/FaHierarchyTreeViewModel.cs:269-274` (`.Where(g => g.Count() == 1)`), Aufruf `Controllers/FaHierarchyController.cs:114-115` |
| Bevorzugte Werkbank: BDE-aktive zuerst, dann kleinste Id. Die Struktur-Erkennung legt damit an und übernimmt deren Namen | `Services/WerkbankKuerzelFamilie.cs:42-43`; `IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs:84` (Katalog ohne gekennzeichnete), `:127`, `:200` |
| Hilfe „Andere Listen“ nennt BDE-Buchungen, Bedarfsmeldungen und Stammdaten als clientseitig | `Views/Help/Index.cshtml:243-249` |
| **BDE-Buchungen filtern serverseitig** | `Views/BdeBookings/Index.cshtml:82` (`data-server-column-filter="true"`), `Controllers/BdeBookingsController.cs:56` (`ColumnFilterHelper.ReadFromQuery`), `:73` (SQL-Filter an `GetHistoryAsync`), `:99-102` (berechnete Spalten über den Kern) |
| Auch Bedarfsmeldungen und Stammdaten sind Server-Mode | `Views/PartRequisitions/Index.cshtml:60`, z. B. `Views/ProductionWorkplaces/Index.cshtml:22`, `Views/Articles/Index.cshtml:59`, `Views/StorageLocations/Index.cshtml:63`, `Views/Users/Index.cshtml:26` (Grep `data-server-column-filter`) |
| Noch clientseitig sind nur kleine, unpaginierte Ansichten | `class=…filterable-table` ohne `data-server-column-filter`: `Views/Picking/Bom.cshtml`, `Views/WarehousePicking/Details.cshtml`, `Views/Tracking/ByWorkplace.cshtml`, `Views/Beschichtungsauftraege/Details.cshtml`, `Views/OrderRecipients/ArticleGroupMappings.cshtml`, `Views/OseonReporting/_OseonReportingTable.cshtml`; keine davon bindet `_Pagination` (Grep) |

### Mengeneingänge (vollständige Liste, Q4)

| # | Einstieg (HTTP) | Service-Methode | Mengen-Schreibstelle | Heute geprüft |
|---|---|---|---|---|
| M1 | `POST /BdeTerminal/Pause` (`BdeTerminalController.cs:104-109`) | `PauseAsync` (`BdeBookingService.cs:149-167`) | `:160-163` → `CreatePartialQuantity` | nein |
| M2 | `POST /BdeTerminal/Finish` (`:131-191`) | `FinishAsync` (`:321-333`) → `FinishInternalAsync` (`:430-443`) | `:436-441` | nur „beide leer“ (`:328`) |
| M3 | `POST /BdeTerminal/ReportPartial` (`:249-254`, `decimal` nicht nullable) | `ReportPartialQuantityAsync` (`:335-345`) | `:342` | nein |
| M4 | `POST /BdeTerminal/ReportCompletion` (`:195-201`) | `ReportCompletionAsync` (`:262-319`) | `:312-315` | nur „beide 0“ (`:280-281`) |
| M5 | `POST /BdeTerminal/FinishGroup` (`:226-247`, JSON) | `FinishGroupAsync` (`:347-382`) | `:373-378` | nein |
| M6 | `POST /BdeBookings/Edit/{id}` (`BdeBookingsController.cs:203-247`, `[RequireBdeAdminAccess]`) | — (Controller direkt) | `:235-236` | nein (`BdeBookingEditViewModel.cs:17-25` ohne Attribute) |

**Negativ belegt:** Weitere Schreibwege für `BdeBookingQuantity` gibt es nicht. Grep `GoodQuantity =|ScrapQuantity =|BdeBookingQuantities.Add|new BdeBookingQuantity`
außerhalb der Tests findet nur `BdeBookingService.cs:162`, `:315`, `:342`, `:377`, `:440`, `:458-463` und `BdeBookingsController.cs:235-236`.
`IDEALAKEWMSService` hat keinen Treffer. `BdeApiController` hat keine POST-Aktion (Grep `Http(Post|Put|Patch)` in `Controllers/Bde*.cs`).
Unter `SQL/` steht `BdeBookingQuantities` nur in Schema-Skripten (`00_FreshInstall.sql`, `42_AddBde.sql`, `44_MoveTablesToDboSchema.sql`),
nicht in `SQL/AgentJobs/`. `StartPlannedAsync`/`ResumeAsync` schließen eigene Buchungen nur mit `null`-Mengen (`:217`, `:221`).
Einen DB-CHECK auf Mengen gibt es nicht (`ApplicationDbContext.cs:1132-1138` nur auf `BdeBookings`). Der Browser setzt `min="0"`
(`Index.cshtml:116-117`, `bde-terminal.js:846-847`), aber `parseFloat` übernimmt jede getippte negative Zahl (`:411-412`, `:864-865`).

### FA-1 Liste nach Haupt-FA (IDEAL, Master an)

- FA-1.1 **Gilt nur bei Master `ProduktionsauftragHierarchisch` = true und Normal-Modus.** `BdeTerminalController.Index` liest den
  Master wie `ProductionOrdersController.cs:241-243` und gibt ihn als Hidden-Input `hierarchisch` (`"true"/"false"`) ans JS. Ohne
  Master (AKE) bleibt die Darstellung exakt wie heute (FA-8).
- FA-1.2 **Gruppe = `orderNumber`** (Haupt-FA). Der Gruppenkopf ist ein Knopf (`<button type="button" aria-expanded aria-controls>`)
  mit sichtbarem Text: `FA 1059463`, Prio-Badge `Prio 2` (nur wenn gesetzt), Fertigungstermin des ersten Eintrags (= frühester,
  folgt aus der Sortierung FA-1.4) bzw. „ohne Termin“, Anzahl „n Arbeitsgänge“ und ein Auf/Zu-Zeichen. Der Zustand steht in
  `aria-expanded` und im Zeichen (▸/▾), nicht nur in der Farbe.
- FA-1.3 **Höchstens N Gruppen sichtbar**, N = AppSetting `BdeTerminalMaxHauptFa` (neu, Standard `5`). Darunter ein Knopf
  „Weitere N anzeigen (noch M)“. Jeder Klick blendet die nächsten N ein. Nicht numerisch oder < 1 → 5
  (`GetIntValueAsync(key, 5)` und dann `< 1 ? 5`). Der Server liefert weiterhin **alle** AGs. Die Grenze ist reine Darstellung.
  So bleiben Scan-Auflösung (FA-4) und Server-Prüfungen unberührt.
- FA-1.4 **Sortierung** (`BdeTerminalAuftraege.Ordnen`, eine Stelle, rein, unit-testbar):
  1. Gruppen-Prio: gesetzt vor leer; aufsteigend (Richtung laut Rückfrage 1). Gruppen-Prio = **kleinster** gesetzter `Prio` der
     Kopfzeilen zur HauptFA (Kombigerät, Rückfrage 1), Lookup wie `Fertigungstermin` über `int.TryParse(OrderNumber)` aus dem
     schon geladenen `kopf` (`BdeApiController.cs:293`). Es gibt keine zusätzliche Abfrage.
  2. Gruppen-Termin: frühester `Fertigungstermin` (Datumsteil) der Einträge der Gruppe, leer zuletzt.
  3. `OrderNumber` ordinal.
  4. Innerhalb der Gruppe wie heute: Fertigungstermin (Datumsteil, leer zuletzt), `SubOrderNumber` ordinal, `ProductionOrderId`,
     `Sequence`.
  Ohne Prio (AKE, leerer Cache, Haupt-FA nicht in FAInfos) ergibt das genau die heutige Reihenfolge: Bei AKE ist jede Gruppe ein
  einzelner FA (`SubOrderNumber == OrderNumber`). Der Bestandstest `BdeTerminalAuftraegeTests.cs:55-68` bleibt unverändert grün,
  am Fixture nachgerechnet.
- FA-1.5 `BdeTerminalArbeitsgang` (`ArbeitsgangStatusRegel.cs:35`) bekommt `int? Prio`. Das JSON von `available-operations`
  (`BdeApiController.cs:299-318`) bekommt `prio`. Sonst bleibt das JSON unverändert.
- FA-1.6 Ungeplante Tätigkeiten bleiben unter der Gruppenliste wie heute (`bde-terminal.js:485-492`).

### FA-2 Aufklappen

- FA-2.1 Klick auf den Gruppenkopf zeigt oder verbirgt den Gruppenrumpf (`hidden`-Attribut; Fallstrick §17 „Bootstrap-`d-*`
  schlägt `hidden`“: kein `d-flex` am selben Element). Darin je Sub-FA der heutige Block (`bde-terminal.js:516-519`: Sub-FA-Nummer,
  Bezeichnung, Termin) mit den AG-Knöpfen wie heute (Status-Zeichen, `aria-label`, `bde-terminal.js:522-525`).
- FA-2.2 **AG-Menge unverändert:** Gezeigt wird genau, was `available-operations` liefert: offene AGs der Kürzel-Familie, ohne
  fertig gemeldete und ohne eigene laufende, mit fremd laufenden als „läuft“ (`BdeApiController.cs:255-286`, v1.57.0 Antwort 6,
  Fallstrick §25). Es gibt keinen zusätzlichen Filter. Das Zuschneiden auf „die AGs dieser Werkbank“ macht schon der Server über
  die Familie (Fallstrick §28: nie selbst `ProductionWorkplaceId == workplaceId` schreiben).
- FA-2.3 Mehrere Gruppen dürfen gleichzeitig offen sein. **Aufklapp-Zustand und Zahl der eingeblendeten Gruppen bleiben beim
  Neuladen der Liste erhalten** (JS-Zustand `expanded: Set<orderNumber>`, `shownGroups`). Die Liste lädt nach jeder Aktion neu
  (`bde-terminal.js:362-364`), sonst klappte sie nach jedem Start zu.
- FA-2.4 Beim Operator-Wechsel (`bde-terminal.js:134-153`) wird der Zustand zurückgesetzt.

### FA-3 Auswahl markieren, Knöpfe an der Liste (standortneutral, Rückfrage 3)

- FA-3.1 Jeder produktive AG-Knopf trägt `aria-pressed="false"`, der gewählte `aria-pressed="true"` plus Klasse `bde-ag-gewaehlt`.
  Die sichtbare Kennzeichnung braucht **keine Farbe**: dicker dunkler Rahmen außen (`outline: 3px solid #212529; outline-offset: 2px`
  auf dem weißen Kartengrund) und ein vorangestelltes „✓“ mit visuell verstecktem Text „gewählt“. **Nicht** über `.active`
  lösen: `bde.css:33` setzt die Active-Variablen gleich den Statusfarben, die Auswahl wäre unsichtbar.
- FA-3.2 Nach dem Neuladen der Liste wird die Markierung wieder gesetzt, wenn `currentWorkOp.id` noch in der Liste steht. Die Gruppe
  des gewählten AG ist dann offen.
- FA-3.3 `#actionPanel` zieht aus `Index.cshtml:107-108` in die **linke Spalte direkt unter `#operationsCard`** (nach `:85`). Über den
  Start-Knöpfen steht eine Textzeile „Gewählt: 1059463 / 1065150 · EG Entgraten Fläche“ (escaped). So ist die Auswahl auch beim
  Panel sichtbar. Nach einer Auswahl per Klick oder Scan scrollt das Panel in den Sichtbereich (`scrollIntoView({ block: 'nearest' })`).
- FA-3.4 Inhalt und Logik des Panels bleiben unverändert (`renderActionsForActive`, `renderStartButtons`, `bde-terminal.js:306-323`),
  inklusive „Fertig melden“ (v1.63.0) und NurFA.

### FA-4 Scan ohne AG (Normal-Modus)

- FA-4.1 `bde-terminal.js:178-182` wird gelockert: Ein Teil = nur FA, zwei Teile = FA,AG wie bisher. Leere Eingabe bleibt ohne
  Aktion (`:165`). Der Platzhalter `Index.cshtml:76` wird zu „Oder FA-Nr scannen (optional ,AG-Nr)…“. Die Trennung `,`/`/` bleibt
  (`:178`, Fallstrick §3 „QR-Code Komma-Suffix“).
- FA-4.2 **Master an (IDEAL)**, Auswertung der `resolve-scan`-Antwort:

  | Eingabe | Antwort | Verhalten |
  |---|---|---|
  | Haupt-FA ohne AG (`viaSubOrderFallback = false`) | Exact oder Ambiguous | Liste neu laden, Gruppe einblenden (auch jenseits der N-Grenze), aufklappen, in den Sichtbereich scrollen. **Keine** Vorauswahl, wie der Klick (Entscheidung Gerald) |
  | Sub-FA ohne AG (`viaSubOrderFallback = true`) | Exact | AG wählen wie ein Klick: `currentWorkOp`, Gruppe auf, Markierung, Panel (FA-3) |
  | Sub-FA ohne AG | Ambiguous (Sub-FA hat mehrere Familien-AGs) | Gruppe aufklappen, Fokus auf den Sub-FA-Block |
  | FA,AG | Exact | wie heute wählen (`bde-terminal.js:202-204`, `:232-235`), zusätzlich Markierung + Gruppe auf |
  | FA,AG | Ambiguous | Gruppe aufklappen (ersetzt im Master-Modus die Auswahlknöpfe `renderScanSelection`) |
  | beliebig | NotInScope / NotFound | Meldungen wie heute (`bde-terminal.js:208-212`) |

  Fallback: Fehlt die Gruppe nach dem Neuladen in der Liste (darf nach „Scan-Kandidaten ⊆ Angebot“ nicht vorkommen), zeigt das JS
  die bestehende Auswahl `renderScanSelection` mit den Kandidaten. Das ist Melden statt stiller Leere.
- FA-4.3 **Master aus (AKE, Rückfrage 3):** FA ohne AG → Exact wählt wie heute, Ambiguous zeigt die bestehende Auswahl
  `renderScanSelection`. Deren Überschrift (`bde-terminal.js:241-242`) wird neutral: „FA {fa}: {n} Treffer – bitte auswählen“, denn
  bei AKE unterscheiden sich die Treffer im AG, nicht im Sub-FA. `FA,AG`-Scans verhalten sich bitgleich wie heute.
- FA-4.4 NurFA-Modus: unverändert (`bde-terminal.js:168-175`).
- FA-4.5 **Server-Änderung (nötig, begründet):** Der Sub-FA-Scan erreicht jetzt Schritt 3 des Resolvers. Bisher war dieser Schritt
  laut Kommentar „heute unerreichbare Vorrüstung“ (`BdeScanResolver.cs:22-23`, `:107`). Hat die Sub-FA keinen offenen Familien-AG,
  meldet der Server heute `NotFound` („Arbeitsgang nicht gefunden“, `:114-115`), obwohl der Auftrag existiert. Das ist eine falsche
  Meldung. **Änderung:** Nach leerem Schritt 3 prüft der Resolver `ProductionOrders.Any(po => po.SubOrderNumber == fa)` und liefert dann
  `NotInScope` („Dieser Auftrag hat an dieser Werkbank keinen offenen Arbeitsgang.“). Der Klassenkommentar wird angepasst. Sonst
  ändert sich am Server nichts: Der AG ist schon optional (`BdeApiController.cs:85`, `BdeScanResolver.cs:181-182`), und
  `viaSubOrderFallback` unterscheidet Haupt- von Sub-FA-Scan (`BdeApiController.cs:99`).
- FA-4.6 **Unverifiziert (Sage):** dass Sage-Etiketten nur die Haupt-FA tragen (Prüfschritt P3) und dass Sub-FA-Nummern nie
  gleich einer Haupt-FA-Nummer sind (P2). Bei einer Kollision gewinnt Schritt 1, und der Scan gilt als Haupt-FA-Scan.
  **Befund Gerald 2026-10-19:** P3 – das Sage-Papier trägt den Barcode des **Haupt-FA**. P2 – Sub-FAs haben immer eine eigene
  Nummer; einzige Ausnahme ist die Zeile des Haupt-FA selbst (Kennzeichen „HauptFA“, z. B. 1059463 in der Struktur von 1059463).
  Ein Scan dieser Nummer klappt die Gruppe auf (Schritt 1); die AGs der Haupt-FA-Zeile gehören zur Gruppe. P2 liefert daher
  diese Selbst-Zeilen als Treffer – das ist kein Fehler. P1 – fast keine Haupt-FA hat eine Prio.

### FA-5 Negative Mengen serverseitig abweisen

- FA-5.1 Eine private statische Prüfung im `BdeBookingService`: `(good ?? 0) < 0 || (scrap ?? 0) < 0` →
  `BdeBookingResult.Invalid("Mengen dürfen nicht negativ sein.")`. Sie steht am **Anfang** von M1–M4, vor jeder Zustandsänderung:
  in `PauseAsync` vor `b.Status = Paused` (`:156`), in `ReportCompletionAsync` als Teil von Schritt 5 (`:280`), also vor dem Insert.
  Das Terminal zeigt die Meldung schon als Toast (`bde-terminal.js:386-389`).
- FA-5.2 M5 `FinishGroupAsync`: Hat **ein** Eintrag eine negative Menge, wird die ganze Gruppe abgewiesen mit
  `GroupFinishResult(0, "Mengen dürfen nicht negativ sein.")`, vor der Schleife `:367`. Die Antwort zeigt das JS schon an
  (`bde-terminal.js:889`).
- FA-5.3 M6 Admin-Korrektur: `[Range(0, double.MaxValue, ErrorMessage = "Menge darf nicht negativ sein.")]` auf `GoodQuantity` und
  `ScrapQuantity` von `BdeBookingQuantityEditRow`. Der Controller prüft `ModelState` schon (`BdeBookingsController.cs:208-212`).
  `Edit.cshtml` bekommt `min="0"` und `asp-validation-for` je Mengenfeld, damit die Ablehnung sichtbar ist. Heute rendert die View
  keine Validierung (Grep `asp-validation`: kein Treffer).
- FA-5.4 0 bleibt erlaubt, die bestehenden Regeln „mindestens eine Menge“ (`:280-281`, `:328`) bleiben unverändert.

### FA-6 FA-Baum: Werkbank-Name bei Kürzel an mehreren Werkbänken

- FA-6.1 **Regel:** Hängt ein Kürzel an mehreren **aktuellen** Werkbänken (nach dem bestehenden Filter `SageMissingSince == null`,
  `FaHierarchyTreeViewModel.cs:271`), zeigt das Symbol den Namen der **bevorzugten** Werkbank: `WerkbankKuerzelFamilie.Bevorzugt`
  (`WerkbankKuerzelFamilie.cs:42-43`, BDE-aktive zuerst, dann kleinste Id).
- FA-6.2 **Warum diese Regel:** Es ist dieselbe Regel, mit der die Struktur-Erkennung den AG anlegt und seinen `Name` setzt
  (`WorkOperationStructureDetectionService.cs:127`, `:200`; v1.63.0 FA-a5). Symbol im FA-Baum und AG-Name im Terminal stimmen damit
  überein, und die Regel lebt an einer Stelle (Fallstrick §28). Verworfen wurde „alle Namen aufzählen“: Das wäre länger am Symbol und
  wiche vom AG-Namen ab.
- FA-6.3 `NamenLookup` ändert nur den Zweig `Count() > 1` (`:273-274`). Eindeutige Kürzel, gekennzeichnete Werkbänke und
  Trim/IgnoreCase bleiben. Der Doku-Kommentar `:266-268` („fallen heraus“) wird angepasst.

### FA-7 Hilfeseite

- FA-7.1 „Andere Listen“ (`Views/Help/Index.cshtml:243-249`) wird ersetzt. Neuer Sinn: „Fast alle Listen filtern seit v1.58.1 auf
  dem Server über alle Seiten, auch BDE-Buchungen, Bedarfsmeldungen und Stammdaten. Nur kleine Ansichten ohne Seitenumbruch filtern
  im Browser (z. B. Stückliste, Kommissionier-Details, Rückmeldung je Werkbank); dort wirkt der Filter auf alle angezeigten Zeilen.“
  Der Satz zur Drop-Down-Wahl „Alle“ entfällt, denn diese Ansichten haben keinen Seitenumbruch (Beleg in der Ist-Tabelle).
- FA-7.2 Terminal-Bedienung (`:1130-1150`): Gruppierung nach Haupt-FA, Prio, „Weitere anzeigen“, Markierung, Knöpfe unter der Karte,
  Scan ohne AG (Haupt-FA klappt auf, Sub-FA wählt). Kürzel-Familie (`:1207`): „beim Scan `FA,AG`“ → „beim Scan (FA, Sub-FA oder
  FA,AG)“. Einstellung `BdeTerminalMaxHauptFa` erklären.

### FA-8 AKE (Master aus) — keine Regression

| Stelle | AKE-Verhalten | Beleg |
|---|---|---|
| Liste | flach wie heute, **alle** FAs, ein Block je FA, keine Gruppenköpfe, keine N-Grenze | FA-1.1 Master-Gate |
| Reihenfolge | identisch: ohne Kopfzeilen ist `Prio` leer, jede Gruppe ist ein FA | `ArbeitsgangStatusRegel.cs:45-46`; `FaHierarchyOrderInfos` füllt nur der IDEAL-Struktur-Import (`FaHierarchySyncService.cs`), Datenstand je DB → P4 |
| AG-Menge, Buchung, Familie | unverändert (AKE-Werkbänke ohne Kürzel → Familie `{Id}`) | Fallstrick §28 „Ohne Kürzel = nur die Werkbank selbst“ |
| Scan `FA,AG` | bitgleich | FA-4.3 |
| Scan nur FA | neu erlaubt (bisher Formatfehler) | FA-4.3, Rückfrage 3 |
| Markierung, Panel-Position | neu, standortneutral | FA-3, Rückfrage 3 |
| Negative Mengen | ebenfalls abgewiesen (standortneutral, gewollt) | FA-5 |
| FA-Baum | nur bei Master, unberührt | `FaHierarchyController` |

## Technischer Lösungsentwurf

Muster: reine Sortier-/Regel-Klasse statt neuer Schicht (`BdeTerminalAuftraege`), Hidden-Input-Modus wie `nurFaMode`
(`Index.cshtml:201`), AppSetting-Seed wie die BDE-Gruppe (ADR [[0011-feature-toggles-ueber-appsettings]]), Familie nur über
`WerkbankKuerzelFamilie` (Fallstrick §28). Ponytail: keine neue API, kein neues Repository, keine Migration, keine zweite
Sortierung im JS. Die Gruppierung im JS folgt der Server-Reihenfolge.

1. **`BdeTerminalAuftraege.Ordnen`:** Prio je HauptFA aus `kopf` (`Min` der gesetzten Werte), Record um `Prio` erweitern.
   Sortierung nach FA-1.4: erst die Gruppen-Schlüssel berechnen (`GroupBy(OrderNumber)` über die projizierten Einträge), dann
   `OrderBy` Gruppe, `ThenBy` innerhalb. Signatur unverändert, `kopf` liegt schon vor.
2. **`BdeApiController.GetAvailableOperations`:** nur `prio = a.Prio` im JSON. Konstruktor unverändert.
3. **`BdeTerminalController.Index`:** `IServiceSettingRepository` injizieren, Master lesen (`HierarchischeStrukturKeys.Master`,
   `IsTrue`-Semantik wie `ProductionOrdersController`), `ViewBag.Hierarchisch`. `ViewBag.MaxHauptFa` = `GetIntValueAsync(
   AppSettingKeys.BdeTerminalMaxHauptFa, 5)`, `< 1 → 5`. Zwei Hidden-Inputs in `@section Scripts` (`Index.cshtml:199-203`), feste
   Werte, keine Werte in `<script>`.
4. **`bde-terminal.js`:** `renderOrderBlocks` bekommt einen Zweig `hierarchisch`: Gruppen in Server-Reihenfolge, Kopf-Knopf,
   Rumpf mit den bestehenden Sub-FA-Blöcken. Zustand `expanded`, `shownGroups`, Delegation für die Kopf-Knöpfe und „Weitere“
   (Fallstrick §3 „Event-Delegation“). Dazu `markSelection()` nach Klick, Scan und jedem Neuladen, die Panel-Zeile „Gewählt …“ mit
   Scroll, die Scan-Lockerung und die Antwort-Tabelle FA-4.2/4.3. Der flache Zweig bleibt die heutige Funktion.
5. **`bde.css`:** Gruppenkopf (volle Breite, `min-height: 50px` wie `.bde-op-btn` `:22`, dunkler Text auf hellem Grund, Rand),
   Prio-Badge `bg-light text-dark border` (v1.63.0 gemessen 14,6:1), `.bde-ag-gewaehlt` (FA-3.1).
6. **`BdeScanResolver`:** Schritt 3b (FA-4.5), Kommentar `:19-29` anpassen.
7. **`BdeBookingService`:** Mengen-Prüfung FA-5.1/5.2. **`BdeBookingEditViewModel` + `Edit.cshtml`:** FA-5.3.
8. **`NamenLookup`:** FA-6.
9. **AppSetting:** `AppSettingKeys.BdeTerminalMaxHauptFa = "BdeTerminalMaxHauptFa"`. Seed-Zeile im BDE-Block `Program.cs:384-393`:
   `("BdeTerminalMaxHauptFa", "5", "BDE-Terminal (IDEAL, Master an): Anzahl Haupt-FAs in der Arbeitsgang-Liste; Rest ueber 'Weitere anzeigen'. Standard 5.")`
   (Beschreibung rund 130 Zeichen, Spalte 500 laut `Models/AppSetting.cs:17`; Fallstrick §16 „Seed-Beschreibung > Spaltenlänge =
   Startup-Crash“). Key in die
   Gruppe „BDE“ `Views/Settings/Index.cshtml:46`. README-AppSettings-Tabelle +1 Zeile.
10. **UI:** `frontend-design`-Skill ist Pflicht (CLAUDE.md). Bootstrap 5, vorhandene Klassen. WCAG AA: Kopf-Text ≥ 4,5:1, Rahmen und
    Markierung ≥ 3:1 gegen den Grund; Bedeutung immer per Text oder Form.

**Rollen / Filter:** keine neuen. Terminal und API erben `[RequireBdeActive]` + `[RequireBdeUserAccess]`
(`BdeTerminalController.cs:14-15`, `BdeApiController.cs:12-15`). Admin-Korrektur bleibt `[RequireBdeAdminAccess]`
(`BdeBookingsController.cs:204`). Die neue AppSetting pflegt, wer heute `/Settings` bearbeiten darf (bestehender Filter, unverändert).
Kein Eintrag in `RoleOverview`.
**Listen-View-Pattern (ADR 0005):** nicht anwendbar. Das Terminal ist keine Tabellen-Liste (Knöpfe, keine `<table>`, kein
Spaltenfilter). Die N-Grenze ist eine konfigurierbare Anzeige-Grenze, der Rest bleibt per „Weitere“ erreichbar, der Server kappt nichts.

## Migrations-/SQL-Auswirkungen

**Keine Migration, kein SQL-Skript.** Es gibt keine Schemaänderung. Die neue AppSetting legt der Startup-Seed an
(`Program.cs:394-406`, `if (!db.AppSettings.Any(s => s.Key == key))`), auch auf frischen Installationen beim ersten Start. Präzedenz
ist `FaHierarchyMaxTiefe` (`Program.cs:416`, kein SQL-Skript, nicht in `00_FreshInstall.sql`). `SQL/00_FreshInstall.sql` und
`SQL/AgentJobs/` sind nicht betroffen. Ein DB-CHECK auf Mengen wird bewusst nicht gebaut (Out-of-Scope, P6).

## Audit-Feld-Auswirkungen

Keine neuen Entitäten. Die Mengen-Prüfung schreibt nichts (sie weist vor dem Schreiben ab). Bestehende Audit-Setzungen bleiben
(`BdeBookingService.SetAudit`/`SetAuditModified`, `BdeBookingsController.cs:222-224`, `:239-241`). `AppSettings` ist kein
`AuditableEntity` (Fallstrick §8), die neue Zeile bekommt keine Audit-Felder.

## Akzeptanzkriterien

1. **Gruppierung (IDEAL, Master an, Normal-Modus):** Das Terminal zeigt je Haupt-FA einen Gruppenkopf mit FA-Nummer, Prio
   (falls gesetzt), Termin und Anzahl AGs. Sub-FAs und AGs sind zunächst zugeklappt.
2. Es sind höchstens `BdeTerminalMaxHauptFa` Gruppen sichtbar (Standard 5). „Weitere N anzeigen (noch M)“ blendet je Klick die
   nächsten N ein. Der Wert 3 zeigt 3 Gruppen. Die Werte `0`, `-1` und `abc` wirken wie 5.
3. Reihenfolge der Gruppen: Prio gesetzt vor leer, aufsteigend (nach Rückfrage 1), dann frühester Termin, dann FA-Nummer. Innerhalb
   der Gruppe: Termin, Sub-FA-Nummer, Sequenz. Unit-Test mit Prio, ohne Prio, Kombigerät (zwei Kopfzeilen) und Gleichstand.
4. Ohne Prio (keine Kopfzeile oder `Prio` NULL) ist die Reihenfolge identisch zur heutigen (`BdeTerminalAuftraegeTests` Bestand grün).
5. Klick auf einen Gruppenkopf klappt auf (`aria-expanded="true"`, Zeichen ▾) und wieder zu. Der Rumpf enthält genau die AGs, die
   `available-operations` für diese Haupt-FA liefert, inklusive „läuft“ fremder Werker.
6. Aufklapp-Zustand und eingeblendete Gruppen bleiben nach Start, Pause und Beenden (Neuladen der Liste) erhalten. Ein
   Operator-Wechsel setzt sie zurück.
7. **Markierung:** Nach Klick auf einen AG trägt genau dieser `aria-pressed="true"` und den Rahmen mit „✓“, alle anderen
   `aria-pressed="false"`. Erkennbar auch in Graustufen. Nach dem Neuladen ist die Markierung wieder da, solange der AG in der Liste steht.
8. **Panel:** Die Aktionsknöpfe stehen in der linken Spalte direkt unter der Karte „2. Arbeitsgang wählen“, mit der Zeile „Gewählt: …“.
   Nach einer Auswahl sind sie ohne manuelles Scrollen im Sichtbereich. Unter beiden Spalten gibt es kein zweites Panel mehr.
9. **Scan Haupt-FA ohne AG** (IDEAL): Die Gruppe wird eingeblendet (auch wenn sie jenseits der N-Grenze liegt), aufgeklappt und in
   den Sichtbereich gerückt. Kein AG ist vorgewählt.
10. **Scan Sub-FA ohne AG** mit genau einem offenen Familien-AG: Der AG ist gewählt und markiert, die Start-Knöpfe sind sichtbar. Bei
    mehreren Familien-AGs der Sub-FA klappt die Gruppe auf.
11. **Scan `FA,AG`** liefert dasselbe Ziel wie in v1.63.0 (Exact wählt, Ambiguous klappt die Gruppe auf). Unit-Tests
    `BdeScanResolverTests` Bestand grün.
12. Scan einer **existierenden Sub-FA ohne offenen Familien-AG** → Meldung „Dieser Auftrag hat an dieser Werkbank keinen offenen
    Arbeitsgang.“ (`NotInScope`), nicht „nicht gefunden“. Unit-Test neu. Scan einer unbekannten Nummer → „Arbeitsgang nicht gefunden“
    wie bisher.
13. **Negative Mengen:** Pause, Beenden, Teilfertigmeldung und „Fertig melden“ mit Gut oder Ausschuss < 0 werden mit „Mengen dürfen
    nicht negativ sein.“ abgewiesen: keine `BdeBookingQuantity`, Buchungsstatus unverändert (Pause: Buchung bleibt Running).
    Ein Unit-Test je Methode M1–M4.
14. Gruppen-Abschluss mit einem negativen Eintrag wird ganz abgewiesen, keine Buchung beendet (Unit-Test M5).
15. Admin-Korrektur einer Menge auf −1 → Formular mit sichtbarer Meldung, Wert in der DB unverändert (Unit-Test Controller M6
    über `ModelState`; Darstellung UAT).
16. Mengen 0 und positive Werte verhalten sich wie in v1.63.0 (Bestandstests grün).
17. **FA-Baum:** Kürzel EG an zwei aktuellen Werkbänken → Symbol „EG – <Name der bevorzugten Werkbank>“. Ein eindeutiges Kürzel
    zeigt wie bisher den Namen. Ein Kürzel nur an gekennzeichneten Werkbänken bleibt ohne Namen (Unit-Tests `FaArbeitsschrittSymboleTests`).
18. **Hilfe:** „Andere Listen“ nennt BDE-Buchungen nicht mehr als clientseitig und beschreibt Server-Filterung plus die
    Browser-Ausnahmen. Die Terminal-Hilfe beschreibt Gruppierung, Prio, Scan ohne AG und `BdeTerminalMaxHauptFa`.
19. **AKE (Master aus):** gleiche AGs, gleiche Reihenfolge, flache Darstellung ohne Gruppenköpfe und ohne N-Grenze, `FA,AG`-Scan und
    Buchungen wie v1.63.0. Abweichend nur die nach Rückfrage 3 freigegebenen Bedienpunkte. Unit-Test: Ordnen ohne Kopf = heutige
    Reihenfolge. Darstellung im UAT.
20. **NurFA-Modus:** Liste, Scan und Start wie v1.63.0 (nach Rückfrage 2).
21. AppSetting `BdeTerminalMaxHauptFa` erscheint nach dem Start auf `/Settings` in der Gruppe „BDE“ mit Wert 5. Ein zweiter Start
    überschreibt einen geänderten Wert nicht.
22. Kontrast: Gruppenkopf-Text ≥ 4,5:1, Markierungsrahmen und Kopf-Rand ≥ 3:1 gegen den Grund (Werte in der QA nachgerechnet).
23. Build und alle Tests grün. Beide `AppVersion.cs` auf 1.64.0, Anwender-Changelog, README, TS-97 in `docs/TESTSZENARIEN.md`.

## Test-Szenarien

Neues Kapitel **TS-97 — BDE-Terminal: Liste nach Haupt-FA, Scan ohne AG, Prio** in `docs/TESTSZENARIEN.md`, danach Zeile 97 in
[[testszenarien-index]]. IDEAL-Testsystem: BDE an, Master an, `BdeNurFaMeldung` aus, Werkbank-Id 65 „Entgraten Fläche“ BDE-aktiv,
Terminal-Benutzer mit Default-Werkbank 65, FA 1059463 mit Sub-FA 1065150 (EG offen, laut TS-96.2/96.3). Vorher Prüfabfragen P1–P4.

- **TS-97.1 — Gruppierung und Grenze (AK 1, 2, 6, 21).** Vorbedingung: an 65 mehr als 5 Haupt-FAs mit offenen EG-AGs (P1).
  Schritte: 1. Operator scannen. 2. Gruppenköpfe zählen. 3. „Weitere 5 anzeigen“. 4. Eine Gruppe aufklappen, einen AG starten.
  5. `/Settings` → `BdeTerminalMaxHauptFa = 3`, Terminal neu laden. Erwartung: 5 Köpfe, dann 10; nach dem Start bleibt die Gruppe
  offen; mit 3 sind es 3 Köpfe. Negativ: a) Wert `0` bzw. `abc` → 5 Köpfe. b) Gibt es nur 2 Haupt-FAs → kein „Weitere“-Knopf.
- **TS-97.2 — Sortierung Prio/Termin (AK 3, 4).** Vorbedingung: P1 liefert Haupt-FAs mit und ohne Prio. Schritte: Reihenfolge der
  Köpfe gegen P1 abgleichen. Erwartung: Prio gesetzt zuerst (Richtung laut Rückfrage 1), dann frühester Termin; ohne Prio nach
  Termin. Negativ: Haupt-FA ohne FAInfos-Zeile steht bei „ohne Prio“ und zeigt kein Prio-Badge.
- **TS-97.3 — Aufklappen 1059463 (AK 5).** Schritte: Kopf „FA 1059463“ antippen. Erwartung: Block Sub-FA 1065150 mit Knopf EG
  (Status-Zeichen wie TS-90); keine fremden Kürzel (ST fehlt, gehört nicht zur EG-Familie). Erneut antippen → zu. Negativ: Ein von
  Werker B laufender EG-AG erscheint als „läuft“ (blau, Sanduhr), nicht ausgeblendet.
- **TS-97.4 — Markierung und Knöpfe (AK 7, 8, 22).** Schritte: EG von 1065150 antippen; Browser-Graustufen (DevTools „Rendering →
  Emulate vision deficiency: Achromatopsia“); Screenreader oder DevTools-Accessibility-Baum. Erwartung: Rahmen und „✓“ sichtbar,
  `aria-pressed="true"`; Zeile „Gewählt: 1059463 / 1065150 · EG …“ und „Rüsten starten / Produktion starten“ direkt unter der Karte
  im Sichtbereich; nichts mehr unter beiden Spalten. Negativ: einen zweiten AG antippen → nur dieser markiert.
- **TS-97.5 — Scan ohne AG (AK 9–12).** Schritte (Eingabefeld „Oder FA-Nr scannen…“, Tastatur oder Scanner):
  a) `1059463` → Gruppe offen, nichts vorgewählt. b) `1065150` → EG gewählt und markiert, Knöpfe sichtbar. c) `1059463,EG` →
  wie v1.63.0 gewählt. d) Haupt-FA, die jenseits der ersten 5 steht → eingeblendet und aufgeklappt.
  Negativ: e) Sub-FA, deren EG fertig gemeldet ist → „Dieser Auftrag hat an dieser Werkbank keinen offenen Arbeitsgang.“
  f) `9999999` → „Arbeitsgang nicht gefunden“. g) `1059463,ST` → Meldung wie TS-96.3 Negativ. h) Leere Eingabe → nichts.
  i) Echtes Sage-Etikett scannen (P3) und Ergebnis notieren: ______
- **TS-97.6 — Negative Mengen (AK 13–16).** Schritte: Produktion auf einem AG starten, dann je Fall im Mengen-Dialog `-1` eintippen:
  a) Pause, b) Teilfertigmeldung, c) Beenden. d) An einer Werkbank mit „nur AG-Fertigmeldung“: „Fertig melden“ mit Gut `-5`.
  e) Gruppen-Abschluss (Mehrfach-Einstellungen an, zwei Produktionen) mit einem Eintrag `-1`. f) BDE-Admin: Buchungen →
  Bearbeiten → Gutmenge `-1` → Speichern. Erwartung: a–e Toast „Mengen dürfen nicht negativ sein.“, Buchung unverändert (P5);
  f Formular mit Meldung am Feld. Negativ (Positivkontrolle): Werte `0`/`3` → wie v1.63.0.
- **TS-97.7 — FA-Baum-Name (AK 17).** Vorbedingung: Werkbank „Entgraten 2 (Test)“ mit Kürzel EG aus TS-96.4. Schritte: FA-Struktur,
  FA 1059463, EG-Symbol (Tooltip/aria-label). Erwartung: „EG – Entgraten Fläche“ (bevorzugt: BDE-aktiv, kleinste Id; per P5b
  prüfen). Negativ: „Entgraten 2“ deaktivieren und löschen bzw. Kürzel leeren → weiterhin „EG – Entgraten Fläche“.
- **TS-97.8 — Hilfe (AK 18).** Hilfe → Listen: Absatz „Andere Listen“ prüfen; BDE-Buchungen filtern seitenübergreifend (Filter auf
  Spalte Operator, Seite 2 enthält nur Treffer). Hilfe → BDE → Terminal-Bedienung lesen.
- **TS-97.9 — AKE-Regression (AK 19).** AKE-Testsystem (Master aus, BDE an, NurFA aus). Schritte: Operator scannen; Liste mit dem
  Stand v1.63.0 vergleichen (gleiche Blöcke, gleiche Reihenfolge, alle FAs, keine Gruppenköpfe, kein „Weitere“); AG antippen und
  starten; `FA,AG` scannen; Pause/Beenden mit Menge. Erwartung: wie v1.63.0, plus Markierung und Knöpfe unter der Karte, sofern
  Rückfrage 3 = ja. Negativ: FA ohne AG scannen → bei genau einem AG gewählt, bei mehreren Auswahl „Treffer – bitte auswählen“
  (bzw. Formatmeldung wie bisher, falls Rückfrage 3 = nein). P4 = 0.
- **TS-97.10 — NurFA unverändert (AK 20).** `BdeNurFaMeldung` an: Liste „Produktionsaufträge“, FA-Scan startet wie v1.63.0, keine
  Gruppenköpfe.

**Prüfabfragen (Mensch, vor dem UAT):**

```sql
-- P1 (WMS-DB IDEAL): Prio-Abdeckung der Haupt-FAs mit offenen AGs an der EG-Familie
SELECT po.OrderNumber, MIN(i.Prio) AS Prio, COUNT(DISTINCT i.Id) AS Kopfzeilen, COUNT(DISTINCT wo.Id) AS OffeneAGs
FROM dbo.WorkOperations wo
JOIN dbo.ProductionOrders po ON po.Id = wo.ProductionOrderId
JOIN dbo.ProductionWorkplaces w ON w.Id = wo.ProductionWorkplaceId
LEFT JOIN dbo.FaHierarchyOrderInfos i ON i.HauptFA = TRY_CAST(po.OrderNumber AS int)
WHERE wo.IsReported = 0 AND LTRIM(RTRIM(w.ArbeitsschrittCode)) = 'EG'
GROUP BY po.OrderNumber ORDER BY CASE WHEN MIN(i.Prio) IS NULL THEN 1 ELSE 0 END, MIN(i.Prio), po.OrderNumber;
SELECT COUNT(DISTINCT HauptFA) AS HauptFAs, COUNT(DISTINCT CASE WHEN Prio IS NOT NULL THEN HauptFA END) AS MitPrio,
       MIN(Prio) AS MinPrio, MAX(Prio) AS MaxPrio FROM dbo.FaHierarchyOrderInfos;

-- P2 (WMS-DB IDEAL): Sub-FA-Nummern, die zugleich eine Haupt-FA-Nummer sind (erwartet nur die Haupt-FA-Selbstzeilen SubOrderNumber = OrderNumber, FA-4.6)
SELECT s.SubOrderNumber FROM dbo.ProductionOrders s
WHERE s.SubOrderNumber <> '' AND s.SubOrderNumber <> s.OrderNumber
  AND EXISTS (SELECT 1 FROM dbo.ProductionOrders h WHERE h.OrderNumber = s.SubOrderNumber);

-- P4 (WMS-DB AKE): Kopfzeilen-Cache leer (erwartet 0) -> Prio wirkt bei AKE nicht
SELECT COUNT(*) FROM dbo.FaHierarchyOrderInfos;

-- P5 (je Fall TS-97.6): keine neue Menge, Buchung unveraendert
SELECT TOP 5 b.Id, b.Status, b.EndedAt, q.GoodQuantity, q.ScrapQuantity, q.CreatedAt
FROM dbo.BdeBookings b LEFT JOIN dbo.BdeBookingQuantities q ON q.BdeBookingId = b.Id ORDER BY b.Id DESC;
-- P5b: bevorzugte Werkbank je Kuerzel EG
SELECT Id, Name, BdeAktiv, SageMissingSince FROM dbo.ProductionWorkplaces WHERE LTRIM(RTRIM(ArbeitsschrittCode)) = 'EG' ORDER BY BdeAktiv DESC, Id;

-- P6 (beide DBs, Info fuer einen spaeteren CHECK): Bestand negativer Mengen
SELECT COUNT(*) FROM dbo.BdeBookingQuantities WHERE GoodQuantity < 0 OR ScrapQuantity < 0;
```

- **P3 (Sage-Etikett, Mensch):** Ein echtes FA-Etikett (Laufzettel) aus Sage IDEAL in einen Texteditor scannen und den Rohinhalt
  notieren. Prüfen: nur die Haupt-FA-Nummer? Mit Komma-Suffix oder Präfix? Ist es Sub-FA oder Haupt-FA? Bis dahin ist „Sage druckt
  nur die Haupt-FA“ **unverifiziert** (Backlog). Ein Präfix gehört in [[2026-10-15-scan-handler-hardware-scanner-spec]].
- **Prio-Semantik (Sage/PPS, Mensch, zu Rückfrage 1):** In der PPS-Anwendung nachsehen, ob Prio 1 die höchste ist. Datenblick:
  `SELECT TOP 30 HauptFA, Prio, FE_Termin FROM [vw_IDEAL-AKE_Kommissionierung_FAInfos] ORDER BY Prio` auf der Sage-DB IDEAL
  (View-Name laut ServiceSetting `Sync:FaHierarchyInfosViewName`).

Regression stichprobenartig: TS-90 (Terminal-Status), TS-96.3/96.7/96.10 (Familie, Fertigmeldung, manipulierte POSTs).

## Etappen (nur bei epic: true)

Nicht zutreffend (`epic: false`). Umsetzung als ein Lauf im Bündel-Worktree, Commits nach Bedarf.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| — | — | — | — |

## Deploy

**Von der QA am 2026-10-19 gegen den echten Diff `b3daf566..a9558301` bestätigt** (`git diff --stat`: 25 Dateien + QA-Commit 2 Dateien).

- **Web-App:** ja. Geändert unter `IdealAkeWms/`: `Controllers/BdeApiController.cs`, `Controllers/BdeTerminalController.cs`,
  `Models/AppSettingKeys.cs`, `Models/ViewModels/BdeBookingEditViewModel.cs`, `Models/ViewModels/FaHierarchyTreeViewModel.cs`,
  `Program.cs` (Seed), `Services/ArbeitsgangStatusRegel.cs`, `Services/BdeBookingService.cs`, `Services/BdeScanResolver.cs`, Views
  `BdeBookings/Edit`, `BdeTerminal/Index`, `Help/Index`, `Help/Changelog`, `Settings/Index`, `wwwroot/css/bde.css`,
  `wwwroot/js/bde-terminal.js`, `AppVersion.cs`.
- **Service:** in diesem Spec-Diff nur `IDEALAKEWMSService/AppVersion.cs` (1.64.0), keine Verhaltensänderung. Der Dienst wird im
  Bündel wegen v1.63.0 ohnehin neu publiziert (Fallstrick §5 „Service referenziert das Web-Projekt“); für diese Spec allein nicht nötig.
- **Migration:** nein. Keine Datei unter `*/Migrations/`, kein `SQL/`-Skript, nichts unter `SQL/Einmalig/`. Die AppSetting legt der erste
  Web-Start an.
- **Reihenfolge:** Publish Web aus dem Worktree, App-Pool neu starten, `/Settings` → BDE prüfen (`BdeTerminalMaxHauptFa = 5`).
  Browser-Cache am Terminal: `bde-terminal.js`/`bde.css` laufen über `asp-append-version` (`Index.cshtml:5`, `:204`), ein harter
  Reload ist nicht nötig.
- **Publish-Befehle (im Worktree):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (Dienst nur im Bündel: `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`)
- **Ablauf Mensch:** Publish aus dem Worktree → Testsystem → TS-97 testen → Merge. Nach dem Merge nur dann erneut aus `main`
  publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.

## Offene Rueckfragen

**Keine mehr offen, beantwortet 2026-10-19** (Block „Freigabe-Antworten“). Fragen unten als Historie.

1. **Prio-Richtung und Kombigerät:** Ist in der PPS-Prio die kleinere Zahl die dringendere (1 vor 2)? Welcher Wert gilt bei
   Kombigeräten mit mehreren Kopfzeilen (verschiedene Prio je Montage-Abteilung)? Der Code kennt nur `int? Prio`
   (`FaHierarchyOrderInfo.cs:39`) und die Doku „Prioritaet“ (`vw_IDEAL-AKE_Kommissionierung_FAInfos.sql:34`); die Bedeutung liegt in
   Sage/PPS. **Empfehlung:** aufsteigend (1 zuerst); Kombigerät = kleinster gesetzter Wert, analog „frühester FE_Termin“
   (`ArbeitsgangStatusRegel.cs:45-46`). Prüfung siehe „Prio-Semantik“.
2. **NurFA-Modus:** Liste (`BdeApiController.cs:216-253`, Sortierung nur `ProductionDate` `:243`) unverändert lassen oder an Gruppierung
   und Prio angleichen? **Empfehlung:** unverändert. NurFA wird bei IDEAL nicht aktiviert (v1.63.0 Antwort 12), eine Angleichung wäre
   ungetesteter Code ohne Nutzer.
3. **AKE:** Sollen die standortneutralen Bedienverbesserungen (AG-Markierung FA-3, Aktionsknöpfe unter der Karte FA-3.3, Scan ohne
   AG FA-4.3) auch bei AKE gelten? Gruppierung und N-Grenze gelten in jedem Fall nur bei Master an (FA-1.1). **Empfehlung:** ja. Das
   ist weniger Code (keine zweite Darstellung), die Funktion bleibt gleich (FA-8), und die Knöpfe ganz unten betreffen AKE ebenso.
   Bei „nein“ bekommen FA-3/FA-4.3 eine Bedingung auf `hierarchisch`.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Prio-Richtung (kleinere Zahl = dringender?) und Wert bei Kombigeräten → **Aufsteigend (1 zuerst), Kombigerät = kleinster gesetzter Wert. Ungeprüft in Sage; kaum Prio-Werte vorhanden (P1), Richtung später mit einer Zeile drehbar.** (Gerald 2026-10-19)
2. NurFA-Liste unverändert lassen? → **Ja, unverändert.** (Gerald 2026-10-19)
3. Markierung, Knopfposition und Scan ohne AG auch bei AKE? → **Ja. Gruppierung und N-Grenze nur bei Master an (IDEAL).** (Gerald 2026-10-19)

## Umsetzung (Dev-Lauf 2026-10-19)

**Direkt im Bündel** `feature/2026-08-07-ideal-teile-1-5`, Start `b3daf566` (v1.63.0). Commits `63bf006c` (Server), `96e90402`
(Terminal-UI), `5613e5a2` (Doku, v1.64.0, TS-97, feature-complete), QA-Fix `a9558301`. Aufgabe [[2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-umsetzung]].

| FA / AK | Umsetzung | Nachweis |
|---|---|---|
| FA-1.4/1.5, AK 3/4/19 | `BdeTerminalAuftraege.Ordnen`: `GroupBy(OrderNumber)`, Gruppen nach Prio (Min, leer zuletzt), frühestem Termin, OrderNumber; innerhalb Termin, Sub-FA, Auftrag, Sequence. Neu `BdeTerminalAuftraege.Prio`, Record `+ int? Prio`, JSON `prio` | `BdeTerminalAuftraegeTests`: Bestandstest unverändert grün; neu `Gruppen_PrioAufsteigend_…`, `Gruppen_Kombigeraet_…`, `Gruppen_SubFasBleibenZusammen_…`, `Ake_OhneKopf_…_AK19` |
| FA-1.1/1.3, AK 2/21 | `BdeTerminalController.Index`: `IServiceSettingRepository` (Master, `IsTrue` wie `ProductionOrdersController`), `GetIntValueAsync(BdeTerminalMaxHauptFa, 5)`, `< 1 → 5`; Hidden-Inputs `hierarchisch`, `maxHauptFa`. Key in `AppSettingKeys`, Seed im BDE-Block `Program.cs`, Gruppe „BDE“ `/Settings` | `BdeTerminalControllerTests.Index_HierarchischUndMaxHauptFa` (5 Fälle inkl. `0`, `-1`, `abc`); Seed: UAT |
| FA-1.2/2/3, AK 1/5–8 | `bde-terminal.js`: `renderGroups` (Kopf-Knopf `aria-expanded`/`aria-controls`, ▸/▾, Prio-Badge, Anzahl, Termin; Rumpf `hidden` mit den Bestands-Sub-FA-Blöcken), „Weitere N anzeigen (noch M)“, Zustand `expanded`/`shownGroups` (Reset beim Operator-Wechsel), `markSelection` (`aria-pressed`, `.bde-ag-gewaehlt`), `gewaehltZeile`, `scrollPanel`. `#actionPanel` in der linken Spalte unter `#operationsCard`. CSS `bde.css` | `node --check`; Darstellung UAT TS-97.1–97.4 |
| FA-4, AK 9–11 | Scan: ein Teil = FA, zwei = FA,AG. Master an: Haupt-FA Exact/Ambiguous → `openGroup` (Neuladen, einblenden jenseits der Grenze, aufklappen, Scroll, keine Vorauswahl); Sub-FA Exact → Auswahl wie Klick; Sub-FA Ambiguous → Gruppe auf, Scroll zum Sub-FA-Block; fehlt die Gruppe → bestehende Auswahl. Master aus: Exact wählt, Ambiguous Auswahl „n Treffer“. NurFA unverändert | UAT TS-97.5/97.9/97.10 |
| FA-4.5, AK 12 | `BdeScanResolver` Schritt 3b: `ProductionOrders.Any(SubOrderNumber == fa)` → `NotInScope`; Klassenkommentar | `BdeScanResolverTests.Normal_SubFaOhneAg_…_AK10`, `Normal_SubFaExistiert_OhneOffenenAg…_NotInScope_AK12` |
| FA-5, AK 13–16 | `BdeBookingService.IstNegativ` am Anfang von Pause/Finish/ReportPartial, in ReportCompletion vor Schritt 5 (0/0), FinishGroup vor der Schleife; `[Range]` + `min="0"` + `asp-validation-for` in der Admin-Korrektur | `BdeNegativeMengenTests` (M1 ×2, M2–M5, Positivkontrolle, M6 ×4 über `Validator`) |
| FA-6, AK 17 | `NamenLookup`: Gruppen > 1 → `WerkbankKuerzelFamilie.Bevorzugt` | `FaArbeitsschrittSymboleTests.NamenLookup_Mehrere_BevorzugteWerkbank`, `…_NurGekennzeichnete_OhneNamen` (Bestandstest „Mehrdeutig_OhneNamen“ bewusst ersetzt) |
| FA-7, AK 18/23 | Hilfe „Andere Listen“ ersetzt, Terminal-Bedienung + Kürzel-Familie ergänzt; Changelog, README (AppSettings-Zeile), v1.64.0 ×2, TS-97.1–97.10 | Diff `5613e5a2` |

**Build/Test (Worktree, nach `5613e5a2`):** `dotnet build IdealAkeWms.slnx` → 0 Fehler (28 Warnungen, alle Bestand; die einzige in einer
berührten Datei, `BdeTerminalAuftraegeTests.cs:23`, stammt aus dem alten `Po`-Helfer). `dotnet test IdealAkeWms.slnx` → Web
**2111 bestanden, 1 übersprungen** (wie vorher), Service **316 bestanden**.

**Abweichungen / Entscheidungen im Lauf**
- Gruppe einblenden „jenseits der Grenze“ (FA-4.2): Das JS erhöht `shownGroups` auf das nächste Vielfache von N, das die Gruppe
  enthält. Dazwischenliegende Gruppen werden damit ebenfalls sichtbar. Das ist dieselbe Seitenlogik wie „Weitere“, statt einer
  Sonderanzeige für eine einzelne Gruppe.
- Markierung „✓“ per CSS `::before` statt eines versteckten Textes „gewählt“: Die AG-Knöpfe tragen ein `aria-label`, das den Inhalt
  für Screenreader ohnehin überschreibt. Der Zustand geht deshalb über `aria-pressed`.
- Die Überschrift „n Treffer“ der Scan-Auswahl gilt im Normal-Modus beider Standorte; NurFA behält „Sub-Aufträge“ (Antwort 2).
- Sortierung: Bei IDEAL **ohne** Kopfzeile mit Sub-FAs verschiedener `ProductionDate` bleibt die Gruppe jetzt zusammen (Gruppen-Termin =
  frühester), vorher lagen sie nach Datum verstreut. Das ist die gewollte Gruppierung (Test `Gruppen_SubFasBleibenZusammen_…`). Bei AKE
  (ein Auftrag je OrderNumber) bleibt die Reihenfolge gleich. Hat AKE mehrere Aufträge mit derselben OrderNumber und verschiedenem
  Datum, stehen sie jetzt beieinander. Das ist eine Abweichung von „bitgleich“, nur in diesem Randfall, und wird im UAT TS-97.9 sichtbar.
- Außerhalb von `affected_code`: keine Datei.

## QA (qa-agent, 2026-10-19)

Geprüft im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Diff `b3daf566..5613e5a2` (Commits `63bf006c`, `96e90402`,
`5613e5a2`), danach QA-Commit `a9558301`. Review synchron in diesem Lauf, nicht delegiert.

### 1. Build und Tests (selbst ausgeführt, nach `a9558301`)

| Befehl | Ergebnis |
|---|---|
| `dotnet build IdealAkeWms.slnx` | **0 Fehler**, 28 Warnungen (Bestand; einzige in berührter Datei `BdeTerminalAuftraegeTests.cs(23,70)` CS8601 aus dem alten `Po`-Helfer) |
| `dotnet test IdealAkeWms.slnx`, `IdealAkeWms.Tests` | **2111 bestanden, 1 übersprungen, 0 fehlgeschlagen** (gesamt 2112) |
| `dotnet test IdealAkeWms.slnx`, `IDEALAKEWMSService.Tests` | **316 bestanden, 0 übersprungen, 0 fehlgeschlagen** |
| Filter auf `BdeNegativeMengenTests`, `BdeTerminalAuftraegeTests`, `BdeScanResolverTests`, `FaArbeitsschrittSymboleTests`, `BdeTerminalControllerTests` | 78 bestanden, 0 fehlgeschlagen |
| `node --check IdealAkeWms/wwwroot/js/bde-terminal.js` | OK |
| `git ls-files --eol` für `docs/TESTSZENARIEN.md`, `bde-terminal.js`, `bde.css` | `i/lf` (Fallstrick §21 erfüllt) |

### 2. Code-Review – Befunde

| # | Schwere | Befund (Fundstelle) | Behandlung |
|---|---|---|---|
| Q1 | **mittel** | `bde.css:44` `.bde-ag-gewaehlt::before { content: "¹3" }`, Bytes `C2 B9 33`: das gemeinte CSS-Escape `"\2713"` (✓) ist beim Schreiben als Oktal `\271` zerschossen worden. Der gewählte AG hätte „¹3“ statt „✓“ gezeigt (AK 7, FA-3.1). | **behoben** `a9558301`: `content: "\2713"` (ASCII-Escape, unabhängig vom Charset) |
| Q2 | **mittel** | `bde-terminal.js` `openGroup` setzte nach dem Scan `kopf.focus()`. Mit einem Tastatur-Scanner landet der nächste Scan dann im Kopf-Knopf, sein Enter klappt die Gruppe wieder zu, die Ziffern gehen verloren. Der Bestand setzte nach Scans nie Fokus. | **behoben** `a9558301`: Fokus-Zeile entfernt, nur `scrollIntoView`. **Abweichung** von FA-4.2 „Fokus auf den Sub-FA-Block“: Der Block wird in den Sichtbereich gescrollt, der Fokus bleibt im Scanfeld. |
| Q3 | niedrig | `bookScannedCandidate` (Sub-FA-Exact bzw. `FA,AG`-Exact, Master an): Lag die Gruppe jenseits der N-Grenze, war der AG gewählt (Panel da), aber in der Liste nicht sichtbar markiert (AK 10, FA-3.2). | **behoben** `a9558301`: `if (hierarchisch) mustShow = candidate.orderNumber;` (dieselbe Einblende-Logik wie `openGroup`) |
| Q4 | Info | `BdeTerminalAuftraege.Ordnen`, AKE-Randfall (dokumentierte Abweichung): Mehrere `ProductionOrders` mit gleicher `OrderNumber` und verschiedenem Termin stehen jetzt beieinander (Gruppen-Termin = frühester). Nachgerechnet: Bei einem Auftrag je `OrderNumber` ist die Reihenfolge identisch (Gruppen-Termin = Auftrags-Termin, Gleichstand `OrderNumber` ordinal = vorher `OrderNumber`/`SubOrderNumber`). Test `Ake_OhneKopf_…_AK19` vergleicht gegen die alte Sortierung. | akzeptiert. Bewertung: fachlich konsistent mit der Gruppierung, nur Reihenfolge, keine Mengen-/Buchungsänderung. Prüfabfrage AKE-DB: `SELECT OrderNumber, COUNT(*) FROM dbo.ProductionOrders GROUP BY OrderNumber HAVING COUNT(*) > 1;` (0 Zeilen = Fall tritt nicht auf) |
| Q5 | Info | `BdeScanResolver` Schritt 3b gilt modusübergreifend. Im NurFA-Modus meldet der Scan einer existierenden Sub-FA ohne Treffer an der Werkbank jetzt „keinen offenen Arbeitsgang“ statt „nicht gefunden“. Liste und Start NurFA unverändert (Antwort 2). | akzeptiert (richtigere Meldung, keine Buchungsänderung) |
| Q6 | Info | Grenze „< 1 → 5“ steht doppelt: Server `BdeTerminalController.Index` und JS `Math.max(1, parseInt(…) || 5)`. Das JS bekommt nur den schon normalisierten Server-Wert; die Abweichung (JS würde -1 zu 1 machen) kann nicht eintreten. | offen gelassen, Server ist die Quelle |
| Q7 | Info | „Weitere … anzeigen“ rendert die Liste neu, der Fokus geht danach an `body` (Tastaturbedienung). Am Touch-Terminal ohne Wirkung. | offen, niedrig |
| Q8 | Info | `prio` im JSON von `available-operations` ist nicht per `BdeApiControllerTests` belegt (nur über `Ordnen`-Tests am Record). | Manual-UAT TS-97.2 (Prio-Badge) |

Ohne Befund geprüft:
- **Werte in `<script>`:** keine. `hierarchisch`/`maxHauptFa` als Hidden-Inputs (Razor-Attribut-Encoding), `Views/BdeTerminal/Index.cshtml`.
- **Escaping:** alle Sage-Texte im JS über `escapeHtml` (`orderNumber`, `subOrderNumber` inkl. `data-sub`/`data-gruppe`, `description`,
  `operationNumber/Name`, `prio`, Panel-Zeile `gewaehltZeile`). `escapeHtml` ist null- und zahlsicher (`String(s == null ? '' : s)`).
  Der Termin kommt server-formatiert `yyyy-MM-dd`.
- **`hidden` + `d-*`:** `.bde-gruppe-rumpf` trägt nur `hidden`, kein `d-*`, CSS ohne `display` (Fallstrick §17).
- **Event-Delegation:** ein Listener an `#operationButtons` für Kopf und „Weitere“. Kopf-Knöpfe tragen nicht `.bde-op-btn` und kollidieren
  nicht mit `bindOperationButtonHandlers`.
- **Zustand:** `expanded`/`shownGroups` überleben `loadAvailableOperations`; der Operator-Wechsel setzt alles zurück (`btnChangeOperator`).
  `markSelection` läuft nach jedem `renderOperationButtons`.
- **Scan-Tabelle FA-4.2/4.3:** Exact Haupt-FA (Master an) → `openGroup` ohne Vorauswahl; Sub-FA-Exact/`FA,AG`-Exact → Auswahl; Ambiguous →
  Gruppe (Sub-FA: Scroll zum Block); fehlt die Gruppe → `renderScanSelection` (Melden statt Leere). Master aus: Exact wählt, Ambiguous
  zeigt die Auswahl „n Treffer“. NurFA-Pfad (`scanFaAgInput` erster Zweig, `renderOperationButtons` NurFA-Zweig) unverändert, `gruppe` ist dort false.
- **Negative Mengen:** `IstNegativ` steht in M1/M2/M3 vor `FindAsync`, in M4 vor dem ersten Schreibzugriff (davor nur Lesen), in M5 vor
  dem Laden der Gruppe. null zählt als nicht negativ, ohne den Wert zu ersetzen. M6: `[Range(0, double.MaxValue)]`, der Controller
  prüft `ModelState` vor jedem Schreiben (`BdeBookingsController.cs:208-212`). Die View bindet indiziert (`Quantities[i].…`), keine
  positionsabhängigen Parallel-Arrays.
- **`NamenLookup`:** nutzt `WerkbankKuerzelFamilie.Bevorzugt` (eine Regel, Fallstrick §28); Trim/IgnoreCase und `SageMissingSince`-Filter bleiben.
- **`BdeTerminalController`:** `IServiceSettingRepository` ist im Web registriert (`ProductionOrdersController` nutzt dieselbe Abhängigkeit);
  `IsTrue` mit gleicher Semantik.
- **AppSetting-Seed:** Beschreibung 118 Zeichen < 500 (`Models/AppSetting.cs:17`, Fallstrick §16); Seed nur `if (!Any)`, ein zweiter Start überschreibt nicht.
- **Eine Regel an einem Ort:** Sortierung nur serverseitig (`Ordnen`), das JS gruppiert nur aufeinanderfolgende `orderNumber`.

**Kontrast (AK 22, nachgerechnet, relative Luminanz WCAG 2.x):** `#212529` L = 0,0181; `#fff` L = 1; `#f8f9fa` L = 0,9461; `#6c757d` L = 0,1739.
Kopf-Text `#212529` auf `#fff` **15,4:1**, auf `#f8f9fa` (offen) **14,6:1** (≥ 4,5). Kopf-Rand `#6c757d` auf `#fff` **4,7:1**, offen `#212529`
15,4:1 (≥ 3). Markierungsrahmen `#212529` auf dem Kartengrund `#fff` **15,4:1** (≥ 3). Prio-Badge `text-dark` auf `bg-light` 14,6:1.
Der Zustand steht zusätzlich in Zeichen ▸/▾, `aria-expanded`, `aria-pressed` und ✓, nicht nur in der Farbe. Darstellung selbst: Manual-UAT TS-97.4.

### 3. Beweisart je AK

| AK | Beweis | Beleg |
|---|---|---|
| 1 | Manual-UAT | Browser-JS (TS-97.1) |
| 2 | teils Unit (Server-Normalisierung `< 1 → 5`; der Mock emuliert `GetIntValueAsync`), Darstellung Manual-UAT | `BdeTerminalControllerTests.Index_HierarchischUndMaxHauptFa`; TS-97.1 |
| 3 | Unit | `Gruppen_PrioAufsteigend_…_AK3`, `Gruppen_Kombigeraet_…`, `Gruppen_SubFasBleibenZusammen_…` |
| 4 | Unit | Bestandstest `BdeTerminalAuftraegeTests` unverändert grün |
| 5, 6, 7, 8 | Manual-UAT | Browser-JS/CSS (TS-97.1, 97.3, 97.4) |
| 9 | Manual-UAT | JS (TS-97.5 a, d) |
| 10 | Unit (Server: Sub-FA Exact via Fallback) + Manual-UAT (Auswahl, Markierung) | `Normal_SubFaOhneAg_EinFamilienAg_Exact_ViaFallback_AK10`; TS-97.5 b |
| 11 | Unit (Server, Bestand grün) + Manual-UAT (JS) | `BdeScanResolverTests`; TS-97.5 c |
| 12 | Unit | `Normal_SubFaExistiert_OhneOffenenAgAnWerkbank_NotInScope_AK12` |
| 13, 14, 16 | Unit | `BdeNegativeMengenTests` M1–M5, `NullUndPositiv_WieBisher_AK16` |
| 15 | Unit (Attribut über `Validator`), **echtes Model-Binding und Anzeige Manual-UAT** | `AdminKorrektur_RangeAufMengen_M6`; TS-97.6 f |
| 17 | Unit | `FaArbeitsschrittSymboleTests.NamenLookup_Mehrere_BevorzugteWerkbank`, `…_NurGekennzeichnete_OhneNamen` |
| 18 | Manual-UAT | Hilfe-View (TS-97.8) |
| 19 | Unit (Reihenfolge) + Manual-UAT (Darstellung) | `Ake_OhneKopf_…_AK19`; TS-97.9 |
| 20 | Manual-UAT | JS (TS-97.10) |
| 21 | Manual-UAT | Startup-Seed gegen die echte DB (TS-97.1) |
| 22 | QA-Rechnung oben + Manual-UAT | TS-97.4 |
| 23 | Build/Test + Diff | Build/Test oben; `AppVersion.cs` ×2 = 1.64.0, Changelog, README, TS-97 im Diff |

Nicht als grün gezählt: alle Manual-UAT-Anteile (Browser-JS, Model-Binding, Startup-Seed, Sage-Etikett P3).

### 4. Geänderte Bestandstests

| Test | Einordnung |
|---|---|
| `FaArbeitsschrittSymboleTests.NamenLookup_Mehrdeutig_OhneNamen` → ersetzt durch `NamenLookup_Mehrere_BevorzugteWerkbank` | Verhalten **bewusst geändert** durch FA-6 / **AK 17** (vorher: mehrdeutiges Kürzel ohne Namen) |
| `BdeTerminalControllerTests.CreateTerminalController` (Factory) | **Typ-/Compile-Anpassung**: neuer Ctor-Parameter `IServiceSettingRepository`, optionale Parameter für den neuen Test; die Bestandstests rufen unverändert auf |
| `BdeTerminalAuftraegeTests`, `BdeScanResolverTests` | nur Ergänzungen, keine Bestandszeile geändert |

Kein Fall „Sonstiges“.

### 5. CLAUDE.md-Checkliste

- Migration / SQL / FreshInstall / AgentJobs / `SQL/Einmalig/`: nicht betroffen (kein Schema; Seed beim Start wie `FaHierarchyMaxTiefe`).
- Audit-Felder: keine neuen Schreibwege; M6 setzt `ModifiedAt/By/ByWindows` wie bisher.
- Version 1.64.0 in beiden `AppVersion.cs`, `Views/Help/Changelog.cshtml` ergänzt, README-AppSettings-Zeile: ja.
- `docs/TESTSZENARIEN.md` TS-97.1–97.10 vollständig (Vorbedingungen, Schritte, Erwartet, Negativ); Index-Zeile 97 in
  [[testszenarien-index]] vorhanden.
- Rollen/Filter: keine neuen. Brain-Changelog und feature-map: Sache des Hauptlaufs.

## Manuelle Test-Checkliste (Mensch, Schranke 2)

Vorher: Publish Web aus dem Worktree (Abschnitt Deploy), Prüfabfragen P1, P2, P4, P5b, P6 und die AKE-Abfrage aus Q4.

1. `/Settings` → Gruppe „BDE“: `BdeTerminalMaxHauptFa = 5` vorhanden (AK 21). Wert auf 7 ändern, App-Pool neu starten → bleibt 7; zurück auf 5.
2. IDEAL-Terminal Werkbank 65, Operator scannen: höchstens 5 Gruppenköpfe „FA …“, alle zu (▸), mit Anzahl und Termin, Prio-Badge nur bei gesetzter Prio (TS-97.1/97.2).
3. Reihenfolge der Köpfe gegen P1 abgleichen (Prio aufsteigend, dann Termin, dann FA-Nr.) (AK 3).
4. „Weitere 5 anzeigen (noch M)“ → 10 Köpfe; bei ≤ 5 Haupt-FAs kein Knopf (AK 2).
5. `BdeTerminalMaxHauptFa` = 3, dann `0`, dann `abc` → 3, 5, 5 Köpfe (AK 2).
6. Kopf „FA 1059463“ antippen → ▾, Sub-FA 1065150 mit EG, kein ST; erneut → zu (AK 5). Ein von Werker B laufender EG-AG erscheint als „läuft“.
7. EG antippen → dunkler Rahmen und **✓** (nicht „¹3“, QA-Befund Q1), `aria-pressed="true"`, Graustufen-Emulation prüfen; Zeile „Gewählt: 1059463 / 1065150 · EG …“ und Start-Knöpfe direkt unter Karte 2 im Sichtbereich; unter beiden Spalten kein Panel (AK 7, 8, 22).
8. Zweiten AG antippen → nur dieser markiert. Produktion starten, pausieren, fortsetzen, beenden → Gruppe bleibt offen; Operator wechseln → alles zu (AK 6).
9. Scan `1059463` (Enter im Scanfeld) → Gruppe offen, nichts vorgewählt; **gleich danach einen zweiten Scan** → kommt im Scanfeld an, die Gruppe klappt nicht zu (QA-Befund Q2) (AK 9).
10. Scan `1065150` → EG gewählt und markiert, Knöpfe sichtbar (AK 10). Scan einer Sub-FA, deren Haupt-FA jenseits der ersten 5 steht → Gruppe eingeblendet, AG sichtbar markiert (QA-Befund Q3).
11. Scan `1059463,EG` → gewählt wie v1.63.0; `1059463,ST` → Meldung wie TS-96.3; `9999999` → „Arbeitsgang nicht gefunden“; Sub-FA mit fertig gemeldetem EG → „Dieser Auftrag hat an dieser Werkbank keinen offenen Arbeitsgang.“; leere Eingabe → nichts (AK 11, 12).
12. Echtes Sage-Etikett scannen (P3), Rohinhalt notieren: ______
13. Negative Mengen `-1` bei Pause, Teilfertigmeldung, Beenden, „Fertig melden“ (Werkbank nur AG-Fertigmeldung), Gruppen-Abschluss → Toast „Mengen dürfen nicht negativ sein.“, P5 unverändert; `0`/`3` wie v1.63.0 (AK 13, 14, 16).
14. BDE-Admin → Buchungen → Bearbeiten → Gutmenge `-1` → Speichern: Meldung „Menge darf nicht negativ sein.“ am Feld, DB unverändert (AK 15, echtes Model-Binding).
15. FA-Struktur 1059463 → EG-Symbol: „EG – Entgraten Fläche“ (P5b) (AK 17).
16. Hilfe → „Andere Listen“ und BDE-Terminal-Bedienung lesen; BDE-Buchungen nach Operator filtern, Seite 2 nur Treffer (AK 18).
17. AKE-Testsystem (Master aus): Liste flach, alle FAs, gleiche Reihenfolge wie v1.63.0, keine Köpfe, kein „Weitere“; FA ohne AG → genau ein Treffer gewählt, sonst „FA …: n Treffer – bitte auswählen“; `FA,AG` und Buchungen wie v1.63.0 (AK 19).
18. `BdeNurFaMeldung` an: Liste „Produktionsaufträge“, FA-Scan startet wie v1.63.0, keine Gruppenköpfe (AK 20).
19. Stichprobe Regression TS-90, TS-96.3/96.7/96.10.
- Browser-Abnahme 2026-10-21: [[2026-10-21-ideal-v1-59-bis-v1-64-browser-testbericht]] - 45 OK, 2 FEHLER, 1 nicht pruefbar, 0 Protokoll passt nicht
