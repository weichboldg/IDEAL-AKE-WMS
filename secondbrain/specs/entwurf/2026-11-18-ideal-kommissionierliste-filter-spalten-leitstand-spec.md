---
type: spec
title: "IDEAL-Kommissionierliste: Vorfilter Ziele (Einstellung, je Benutzer gemerkt), Spalten Abteilung + Lagerplätze, sichtbar erst nach Leitstand-Freigabe"
slug: 2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand-spec
status: Entwurf
created: 2026-11-18
updated: 2026-11-18
source_backlog: "[[2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: IDEAL
affected_code:
  - "IdealAkeWms/Models/SettingDefinition.cs Z. 21-30 (enum SettingStammdaten): neuer Wert Kommissionierziel"
  - "IdealAkeWms/Models/AppSettingKeys.cs + IdealAkeWms/Services/Settings/AppSettingDefinitions.cs (Gruppe GIdeal, Z. 244-250): zwei neue Keys FaHierarchyKommissionierlisteVorfilterZiele (MultiChoice, Stammdaten Kommissionierziel) und FaHierarchyKommissionierlisteNurFreigegebene (Bool)"
  - "IdealAkeWms/Services/Settings/SettingValidator.cs Z. 51-59 (MultiChoice prueft nur gegen d.Werte): Teile gegen die uebergebene Stammdatenliste pruefen, wenn d.Werte null und Stammdaten gesetzt"
  - "IdealAkeWms/Models/ViewModels/SettingItem.cs Z. 69-74 + IdealAkeWms/Services/Settings/Einstellungspruefung.cs Z. 28-29 (Optionen/Pruefung heute nur fuer MasterData) + Controllers/SettingsController.cs OptionenAsync Z. 73-93 + Views/Settings/_SettingField.cshtml Z. 78-94 (MultiChoice-Checkboxen aus d.Werte!)"
  - "IdealAkeWms/Controllers/FaHierarchyKommissionierListenController.cs (Index 74-102, Summiert 109-143, Print 149-170, Pdf 176-205, PrintSummiert 210-229, PdfSummiert 234-264): Mehrfach-Ziel, Benutzer-Gedaechtnis, Zuruecksetzen, Gate-Banner"
  - "IdealAkeWms/Services/KommissionierListenService.cs (ColumnMap 54-68, SummiertColumnMap 75-84, BuildAsync 95-111, AggregateSummiert 148-213, ToggleSharedQueryKeys 219-223, BuildToggleQuery 227-241, BuildFlagPredicate 268-276)"
  - "IdealAkeWms/Data/Repositories/(I)ProductionOrderPickingStatusRepository.cs: neue Lesemethode freigegebene HauptFA-Nummern (Vorbild GetReleasedForPickingAsync Z. 189-200)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierSummiertViewModel.cs + Listen-/Print-ViewModels: Abteilung, Lagerplaetze, Ziele, Gate-Hinweis"
  - "IdealAkeWms/Views/FaHierarchyKommissionierListen/{Index,Summiert,Print,PrintSummiert}.cshtml: Ziel-Mehrfachauswahl, Spalten montage-abteilung + lagerplaetze, Banner"
  - "IdealAkeWms/Models/ColumnDefinitions.cs Z. 330-370 + Services/ColumnFilterSyntax.cs Z. 57 (Typen-Register)"
  - "IdealAkeWms.Tests/Views/KommissionierSpaltenKonsistenzTests.cs (Drift-Waechter vier Quellen), IdealAkeWms.Tests/Services/KommissionierListenServiceTests.cs, FaHierarchyListBuilderTests.cs Z. 195 (BuildFlagPredicate-Signatur), Settings-Validator-Tests"
  - "IdealAkeWms/Views/Help/Index.cshtml (Abschnitt Kommissionierlisten), Views/Help/Changelog.cshtml, README.md (AppSettings), beide AppVersion.cs, docs/TESTSZENARIEN.md, scripts/deploy/nachlauf.psd1"
open_questions:
  - "Soll das Gate zusaetzlich erledigte HauptFA ausblenden (IsDonePicking/IsDone/IsCancelled wie die AKE-Warteschlange GetReleasedForPickingAsync) - Empfehlung: nein, in dieser Spec zaehlt nur das Freigabe-Flag; Ausblenden erledigter Auftraege als eigener Backlog-Punkt"
  - "Uebergang beim Deploy: heute sichtbare, nicht freigegebene HauptFA verschwinden. Vor dem Umschalten im Leitstand freigeben (erzeugt Kommissionierauftraege) oder Schalter bis zur Freigabe auf Aus lassen - Empfehlung: Schalter wird mit an ausgeliefert, die Pruefung im Manifest zeigt die Anzahl, Gerald entscheidet am DEV-Test, ob er vorab freigibt"
  - "LeitstandAktiv an IDEAL aus, Gate an: Liste waere leer. Empfehlung: Gate bleibt wirksam, sichtbarer Warnbanner in der Liste plus Pruefung im Manifest (melden statt still), keine automatische Abhaengigkeit"
  - "Abteilung bei Kombigeraet (mehrere Montage-Abteilungen je HauptFA): nur 'Kombigerät' (wie Vormontage) oder 'Kombigerät (WKS, FKV)' - Empfehlung: 'Kombigerät (WKS, FKV)', damit der Spaltenfilter 'WKS' auch Kombigeraete findet"
  - "Lagerplaetze: Kommissionierwagen-Bestand mitzeigen und Mengenformat - Empfehlung: Kommissionierwagen ausgeschlossen (Standard-Overload), Menge ohne nachlaufende Nullen ('K-02 (12)', '(2,5)')"
epic: true
etappen:
  - "E1 Einstellungs-Infrastruktur MultiChoice aus Stammdaten + Vorfilter Ziele (Mehrfach, Benutzer-Gedaechtnis, Zuruecksetzen)"
  - "E2 Spalte Abteilung (Liste, Summiert, Druck/PDF, Spaltenfilter)"
  - "E3 Spalte Lagerplaetze (Batch-Bestand, Spaltenfilter, Druck/PDF)"
  - "E4 Leitstand-Gate + Banner + Manifest + Version/TS/Hilfe/Changelog"
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# IDEAL-Kommissionierliste: Vorfilter Ziele, Spalten Abteilung + Lagerplätze, Leitstand-Gate

> [!info] Umsetzungsort und Messstand
> Code nur im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Zweig
> `feature/2026-08-07-ideal-teile-1-5`); alle Zeilenangaben dort gemessen am 2026-11-18
> (`AppVersion` 1.99.41). Das Modul existiert nur dort. Der Dev-Lauf gleicht Zeilen ab.

> [!important] Entscheidungsstand (Gerald, 2026-11-18 15:52, verbindlich)
> R1 Vorfilter als Einstellung „K-01,K-02“, mehrere Ziele, je Benutzer gemerkt ·
> R2 (Backlog-Anf. 2) Summiert ist Hauptsicht · R3 Abteilung = **Montage-Abteilung des Hauptauftrags**
> (FAInfos `Abteilung`, WKS/FKV/TK) · R4 Lagerplätze = alle WMS-Plätze mit Bestand > 0 inkl. Menge,
> zusätzlich zum Hauptlagerplatz · R5 sichtbar erst ab Leitstand-Freigabe, für Liste + Summiert +
> Druck/PDF, Schalter Default an für IDEAL.

## Ziel / Nutzen

Die IDEAL-Kommissionierer sehen beim Öffnen nur ihre Ziele (K-01/K-02), nur Aufträge, die der
Leitstand zur Kommissionierung freigegeben hat, und je Position die Montage-Abteilung sowie wo das
Teil tatsächlich liegt. Heute erscheint jeder HauptFA sofort nach der Materialisierung, alle Ziele
gemischt, und nur der Sage-Hauptlagerplatz.

## Umfang

**Drin:**
- Einstellung Vorfilter-Ziele (Mehrfachauswahl aus Kommissionierzielen) und Mehrfach-Zielfilter in
  Liste/Summiert/Druck/PDF mit Benutzer-Gedächtnis und Zurücksetzen.
- Spalte „Abteilung“ (Montage-Abteilung des HauptFA) in Liste, Summiert, Druck/PDF, filterbar.
- Spalte „Lagerplätze“ (WMS-Bestand > 0 je Platz mit Menge) in Liste, Summiert, Druck/PDF, filterbar.
- Leitstand-Gate mit Schalter, Hinweisbanner, Manifest-Einträge.
- Regressions-AK: Menü öffnet Summiert (bereits umgesetzt, s. u.).

**Nicht drin:**
- Änderungen am Leitstand selbst, an der Freigabe-Logik oder an AKE-Kommissionieraufträgen.
- Ausblenden erledigter HauptFA (Rückfrage 1).
- Abteilung des Teils (`USER_OSAbteilung`) — von Gerald verworfen.
- Schema-Änderungen.

## Fachliche Anforderungen

### A2 Summiert als Hauptsicht — bereits erfüllt (Beleg)
Beide Menüeinträge zeigen schon auf `asp-action="Summiert"` (`Views/Shared/_Layout.cshtml:232`,
`:251-252`, umgesetzt v1.47.0, feature-map Z. 563-582). Es gibt keinen Dashboard-Link auf `Index`.
Nur der Fehlerweg von `Pdf()` leitet auf `Index` um (`FaHierarchyKommissionierListenController.cs`
176-205); das bleibt so, da er aus der Liste aufgerufen wird. **Kein Code, nur Regressions-AK.**

### A1 Vorfilter Ziele
- Neue Einstellung `FaHierarchyKommissionierlisteVorfilterZiele` (siehe „Einstellungen“), Default
  `K-01,K-02`.
- Der Zielfilter (`target`, heute Einzelwert, `KommissionierListenService.BuildFlagPredicate`
  268-276) wird mehrwertig: ein Knoten passt, wenn `Kommissionieren` (getrimmt, ohne Groß-/Klein)
  in der Zielmenge liegt. Leere Menge oder explizit „alle Ziele“ = heutiges Verhalten (alle
  kommissionier-relevanten Knoten, `KommissionierRelevanzFilter.IsRelevant`).
- Auflösung der wirksamen Ziele je Aufruf von Index/Summiert:
  1. Query enthält Zielparameter → diese gelten und werden gespeichert.
  2. Sonst gespeicherter Wert des Benutzers → Redirect mit diesem Wert (URL bleibt teilbar,
     Druck-Link trägt ihn; Muster `BdeTeilelisteController.cs:110-127`).
  3. Sonst Einstellungswert (Default `K-01,K-02`).
- „Zurücksetzen“ (POST, Muster `BdeTeilelisteController.cs:229-236`) löscht den gespeicherten Wert
  und führt zurück auf den Einstellungswert. Der bestehende „Zurücksetzen“-Knopf der Filterkarte
  (`Summiert.cshtml:60-65`, leert Spaltenfilter) bleibt; die Ziel-Rücksetzung ist ein eigener,
  beschrifteter Knopf „Ziele auf Standard“.
- Gedächtnis gemeinsam für Liste und Summiert (ein ViewKey `FaHierarchyKommissionier.Ziele`,
  ≤ 50 Zeichen wegen `ApplicationDbContext.cs:1218`), JSON `{"ziele":["K-01","K-02"]}` bzw.
  `{"ziele":["*"]}` für „alle“. Der Wert „*“ steht in der UI als „— alle Ziele —“.
- Druck/PDF lesen kein Gedächtnis, sondern nur die übergebenen Query-Parameter (die Bildschirm-URL
  wird 1:1 weitergereicht, `Index.cshtml:7-9`).
- UI: Mehrfachauswahl als Checkbox-Gruppe in der Filterkarte (zugängliche Labels, Absenden per
  „Anwenden“-Knopf statt `onchange`), Auswahlwerte = `GetTargetValuesAsync()` (251-261, alle Ziele
  aus den Knoten, nicht vom Gate beschnitten). Gespeicherte Ziele, die nicht mehr vorkommen, werden
  als „(nicht vorhanden)“ markiert angezeigt, nicht still verworfen.
- `ToggleSharedQueryKeys`/`BuildToggleQuery` (219-241) tragen den Mehrfachparameter (wiederholter
  Query-Key) beim Umschalten Liste ↔ Summiert mit.

### A3 Spalte „Abteilung“
- Wert = Montage-Abteilung der HauptFA-Kopfdaten. Quelle: `FaHierarchyOrderInfo.MontageAbteilung`
  (`Models/FaHierarchyOrderInfo.cs:21`), geschrieben aus Sage-View-Spalte `i.[Abteilung] AS
  [MontageAbteilung]` (`IDEALAKEWMSService/Services/FaHierarchySql.cs:158`, gelesen
  `SageFaStrukturReader.cs:175/189`).
- Wiederverwendung `VormontageService.FormatAbteilung` (105-110: trimmt, verwirft Leeres, distinct;
  0 → leer, 1 → Wert, mehrere → „Kombigerät“) und `BuildKopfLookup` (136-139). Die
  Kombigerät-Darstellung entscheidet Rückfrage 4; empfohlen: `FormatAbteilung` liefert bei
  mehreren Werten „Kombigerät (WKS, FKV)“ — betrifft dann auch Vormontage/BDE-Teileliste
  (`BdeTeilelisteService.cs:203/216`), daher ggf. ein optionaler Parameter statt Änderung des
  Standardverhaltens.
- Spaltenschlüssel `montage-abteilung` (wie Vormontage, `VormontageService.cs:149`), Anzeige
  „Abteilung“. Der Gruppenkopf (Index `:118-128`, Print `:71-93`) bleibt unverändert.
- Spalte in Liste (`ColumnMap`), Summiert (Zeilen-VM + `SummiertColumnMap`; Wert ist je HauptFA
  gleich), Print/PrintSummiert (`ShowCol`), `ColumnDefinitions`, inline `#column-config`,
  `<th data-col-key>`, Spaltenfilter Server-Mode (Text).

### A4 Spalte „Lagerplätze“
- Wert = alle Lagerplätze mit WMS-Bestand > 0 des Positionsartikels, Format
  `K-02 (12), H4-05 (3)`, Reihenfolge wie Repository (Hauptplatz zuerst, dann Menge absteigend,
  dann Code; `StockMovementRepository.cs:482-488`). Kein Bestand → „—“.
- Quelle: `IStockMovementRepository.GetStockByArticleNumbersAsync(List<string>)`
  (`IStockMovementRepository.cs:43`, `StockMovementRepository.cs:395-490`): **eine** Batch-Abfrage
  für alle Artikel, zwei gruppierte SQL-Abfragen, schließt Kommissionierwagen aus. Sie filtert
  `Quantity != 0` → der Aufrufer filtert `> 0` (Vorbild `MissingPartsLagerController.cs:48-69`).
  `Contains` wird von EF 10 als ein OPENJSON-Parameter übersetzt (`fallstricke.md` Z. 906-916),
  große Artikellisten sind unkritisch.
- Ladezeitpunkt (kein N+1):
  - Ohne `colf_lagerplaetze`: nach dem Gruppen-Paging nur für die distinct Artnr der Seite.
  - Mit `colf_lagerplaetze`: nach allen übrigen Filtern für die distinct Artnr der Restmenge, dann
    Spaltenfilter anwenden, dann pagen (Muster memoryFilters `PickingLeitstandController.cs:93-105`).
  - Druck/PDF: einmal für die gesamte (ungepagte) Druckmenge.
- Hauptlagerplatz-Spalte bleibt unverändert daneben.
- Filterbarkeit: **ja** (Text-Contains auf dem formatierten String, z. B. „K-02“ findet alle
  Positionen mit Bestand auf K-02). Begründung: Der Kommissionierer sucht genau danach; Kosten nur bei
  gesetztem Filter.

### A5 Leitstand-Gate
**Beleg Ist-Zustand:**
- Freigabe-Status (AKE und IDEAL gleich) = `ProductionOrderPickingStatus.IsReleasedForPicking`
  (`Models/ProductionOrderPickingStatus.cs:18`, 1:1 zur `ProductionOrder`, `AuditableEntity`).
- Einzige App-Schreibwege auf `true`/`false`: `ProductionOrderPickingStatusRepository.SetReleaseAsync`
  (Z. 82 raw-SQL-UPDATE, Z. 88) und `SetReleaseBatchAsync` (Z. 283, Sub-FA übersprungen 271-275),
  aufgerufen nur aus `PickingLeitstandController.ToggleRelease` (373-457, `[RequireLeitstandAccess]`)
  und `BulkRelease` (461-528).
- Anlage der Statuszeilen immer mit `false`: IDEAL `FaMaterializationSyncService.EnsureStatusRowsAsync`
  (`IDEALAKEWMSService/Services/FaMaterializationSyncService.cs:285-326`, Aufruf 198); AKE
  `SageImportService.cs:197-206` (MERGE mit 0); `FaZusatzinfoSyncService.cs:214-221` setzt nur
  `IsDonePicking`; `SetFieldAsync` (44-51) berührt das Flag nicht; DB-Default 0
  (`SQL/00_FreshInstall.sql:436`). **Kein Sync setzt das Flag.**
- IDEAL-HauptFA im Leitstand: Der Wurzelknoten hat `SubFA = HauptFA` und wird als `ProductionOrder`
  mit `OrderNumber = SubOrderNumber = HauptFA` materialisiert (`FaMaterializationSyncService.cs:146-163`,
  Quelle 71-83; Brain `sage-views-ideal.md` Z. 87) und ist damit `IsHauptFa`
  (`Models/ProductionOrder.cs:29-30`). Der Leitstand zeigt im hierarchischen Modus über
  `GetForLeitstandGroupedAsync` (`PickingLeitstandController.cs:206-207`,
  `ProductionOrderRepository.cs:54-92/218/231`) und liest das Flag in `MapItem` (152). Die
  Freigabe-UI erscheint nur bei `LeitstandAktiv && CanManagePickingRelease`
  (`Views/PickingLeitstand/Index.cshtml:66` u. a.).
- Die Kommissionierliste liest das Flag heute **nirgends** (`KommissionierListenService.BuildAsync`
  95-111 lädt nur Knoten + OrderInfos) → jeder HauptFA ist sofort sichtbar.

**Soll:**
- Schalter `FaHierarchyKommissionierlisteNurFreigegebene` an → ein HauptFA (alle seine Positionen)
  erscheint in Liste, Summiert, Druck, PDF, PrintSummiert und PdfSummiert nur, wenn eine
  `ProductionOrder` mit `IsHauptFa` und `OrderNumber = HauptFA` existiert, deren Statuszeile
  `IsReleasedForPicking = true` hat. Zurücknehmen der Freigabe im Leitstand blendet ihn wieder aus.
- Umsetzung: neue Repository-Methode liefert die Menge freigegebener HauptFA-Nummern (eine Abfrage,
  `IsHauptFa`-Ausdruck wiederverwenden); der Service parst auf `int` und nimmt sie ins Prädikat vor
  allen Spaltenfiltern auf. Mehrere HauptFA-Zeilen mit gleicher `OrderNumber`: freigegeben, sobald
  eine freigegeben ist.
- `Pdf(hauptFa)`/`PdfSummiert` für einen nicht freigegebenen HauptFA → `WarningMessage`
  „HauptFA … ist im Leitstand nicht zur Kommissionierung freigegeben.“ statt leerem PDF.
- Banner (melden statt still) bei aktivem Gate oberhalb der Tabelle:
  „Nur im Leitstand freigegebene Aufträge. n HauptFA mit Kommissionier-Positionen (aktuelle
  Ziele) sind noch nicht freigegeben.“ Bei `LeitstandAktiv = false` zusätzlich Warnbanner
  „Leitstand ist ausgeschaltet – Freigaben sind nicht möglich, die Liste bleibt leer.“
- Schalter aus → heutiges Verhalten, kein Banner.
- Bekannte Grenzen (im Hilfetext nennen): HauptFA ohne materialisierte `ProductionOrder` oder ohne
  Statuszeile kann nicht freigegeben werden (`ToggleRelease` → NotFound 389-390); Wurzel ohne
  Artikelnummer blockiert die Freigabe (405-410); die Freigabe erzeugt Kommissionieraufträge
  (`PickingOrderWorkflow.ErzeugeOffeneAsync`, Controller 450-453, Service Z. 56) — gewollter
  Nebeneffekt der bestehenden Freigabe, keine Änderung.

## Technischer Lösungsentwurf

Leiter (ponytail): Alles wird wiederverwendet — `UserViewPreference`-Repository (Server-Muster
BdeTeiliste), `FormatAbteilung`/`BuildKopfLookup`, `GetStockByArticleNumbersAsync`,
`GetKommissionierzieleAsync`, `ColumnFilterHelper.Apply`, Freigabe-Flag. Neu sind nur ein
Stammdaten-Enumwert, zwei Einstellungen und eine Lesemethode.

1. **Einstellungen (E1).**
   - `SettingStammdaten.Kommissionierziel` (`SettingDefinition.cs:21-30`); Optionen aus
     `IPickingAreaRepository.GetKommissionierzieleAsync()` (`PickingAreaRepository.cs:33-44`,
     distinct `FaHierarchyNodes.Kommissionieren`, getrimmt, sortiert) in
     `SettingsController.OptionenAsync` (73-93) und in der Start-Pruefung (`Einstellungspruefung`
     Stammdaten-Dictionary).
   - MultiChoice mit `Werte == null` und gesetzten Stammdaten: `_SettingField.cshtml` rendert die
     Checkboxen aus `Model.Optionen`; `SettingValidator` (51-59) prüft die Teile gegen die
     übergebene Stammdatenliste; `SettingItem.Aus` (69-74) und `Einstellungspruefung` (28-29)
     reichen die Optionen auch für diesen Fall durch. Ein gespeicherter Wert, der in den Daten
     fehlt, erscheint als ungültig mit Text, wird aber nicht still gelöscht.
   - Leere Stammdatenliste (z. B. DEV ohne Knoten): Speichern eines nicht leeren Werts wird
     abgewiesen; Default ungeprüft (wie heute, `SettingItem.cs:71-73`).
2. **Filter + Gedächtnis (E1).** Controller-Parameter `string[]? target`; Hilfsmethode im Controller
   löst die wirksamen Ziele nach A1 auf. Service-Signaturen `BuildAsync`/`BuildSummiertAsync`
   nehmen `IReadOnlyCollection<string>? ziele`; `BuildFlagPredicate` wird mengenwertig (Test
   `FaHierarchyListBuilderTests.cs:195` anpassen).
3. **Abteilung (E2).** Lookup HauptFA → Abteilungstext einmal je Request aus den bereits geladenen
   OrderInfos; in `ColumnMap`/`SummiertColumnMap` als Closure bzw. Zeilenfeld, damit der
   Spaltenfilter vor dem Paging greift. `ColumnFilterSyntax.cs:57` unverändert (Text-Default).
4. **Lagerplätze (E3).** Formatierfunktion statisch und rein (testbar); Laden nach A4. Für die Liste
   (`FaHierarchyListBuilder.Build` filtert vor dem Gruppieren, 138) wird bei gesetztem
   `colf_lagerplaetze` das Bestands-Lookup vorab für die Kandidaten-Artnr geladen und in die
   ColumnMap gegeben; ohne Filter erst nach dem Paging in das ViewModel gemappt.
5. **Gate (E4).** Neue Methode `GetReleasedHauptFaOrderNumbersAsync()` am
   `IProductionOrderPickingStatusRepository`; im Service ins Prädikat; Zähler für den Banner =
   distinct HauptFA, die ohne Gate (bei gleichen Zielen) sichtbar wären, minus sichtbare.
6. **Drift-Waechter.** `KommissionierSpaltenKonsistenzTests` erzwingt die vier Quellen (th,
   `#column-config`, `ColumnDefinitions`, `ShowCol`); neue Spalten an allen vier Stellen in beiden
   Ansichten.

## Migrations-/SQL-Auswirkungen

Keine. Einstellungen werden beim Web-Start geseedet (`seed: true`, Muster
`AppSettingDefinitions.cs:244-250`), `UserViewPreferences` existiert. Kein `00_FreshInstall`-,
kein AgentJobs-Eingriff. Falls doch nötig, nächste freie Nummer 164 (162/163 sind für v1.99.42
reserviert, `nachlauf.psd1:119/121`).

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `FaHierarchyKommissionierlisteVorfilterZiele` — Anzeige „Kommissionierliste: Standard-Ziele“ | MultiChoice, Stammdaten `Kommissionierziel` | Auswahl aus den Kommissionierzielen der FA-Struktur (`GetKommissionierzieleAsync`); leer erlaubt = alle Ziele; Server prüft jeden Wert beim Speichern | `K-01,K-02` | Vorauswahl der Ziele, wenn ein Benutzer noch keinen eigenen Filter gemerkt oder ihn zurückgesetzt hat | Kommissionierliste Liste + Summiert (IDEAL) | IDEAL (Gruppe „IDEAL FA-Hierarchie“) | keiner (keine Außenwirkung) |
| `FaHierarchyKommissionierlisteNurFreigegebene` — Anzeige „Kommissionierliste nur nach Leitstand-Freigabe“ | Bool | an / aus | an (`true`, Seed) | An: ein HauptFA erscheint in Liste, Summiert, Druck und PDF erst nach „zur Kommissionierung freigegeben“ im Leitstand. Aus: sofort nach der Übernahme aus Sage | Kommissionierliste alle sechs Aktionen | IDEAL | „Beim Einschalten verschwinden alle noch nicht freigegebenen Aufträge aus der Kommissionierliste. Freigaben erfolgen im Leitstand und legen Kommissionieraufträge an; der Leitstand muss aktiv sein (Einstellung LeitstandAktiv).“ |

## Audit-Feld-Auswirkungen

Keine neue Entität. Gedächtnis über `UserViewPreferenceRepository.SaveAsync` (setzt
`Created*`/`Modified*`, `UserViewPreferenceRepository.cs:21-44`) mit Werten aus
`ICurrentUserService`. AppSettings über den bestehenden Speicherweg. Das Freigabe-Flag wird nur
gelesen.

## Rollen / Zugriff

Unverändert, wiederverwendet: Klasse `[RequireLagerProcessingAccess]` +
`[RequireFaHierarchyKommissionierlistenAktiv]` (`FaHierarchyKommissionierListenController.cs:30-31`)
für alle Aktionen inkl. „Ziele auf Standard“ (eigene Präferenz, kein Edit-Recht nötig). Freigabe
bleibt `[RequireLeitstandAccess]` (`PickingLeitstandController.cs:373`). Einstellungen
`[RequireAdminAccess]` (`SettingsController.cs:13`). Keine neue Rolle → `controller.md`/RoleOverview
unberührt.

## Listen-View-Pattern (ADR 0005)

Paging (Gruppen-Paging je HauptFA) und Filterkarte bestehen. Neue `<th>` mit `data-filterable` +
`data-col-key="montage-abteilung"` / `"lagerplaetze"` in Index und Summiert, Server-Mode bleibt,
`ColumnFilterHelper` über die erweiterten ColumnMaps; `ToggleSharedQueryKeys` um
`colf_montage-abteilung`, `colf_lagerplaetze` und den Mehrfach-`target` ergänzen. Datumsspalten
nicht betroffen.

## Akzeptanzkriterien

1. Erstes Öffnen über das Menü (Benutzer ohne Gedächtnis) zeigt Summiert mit Zielen K-01 und K-02
   vorausgewählt; nur Positionen dieser Ziele.
2. Benutzer wählt K-01 allein, verlässt die Seite, öffnet sie erneut → K-01 vorausgewählt; ein
   anderer Benutzer sieht weiter K-01 + K-02.
3. Wechsel Summiert → Liste behält die Ziele (und die gemeinsamen Spaltenfilter).
4. „Ziele auf Standard“ stellt den Einstellungswert her und löscht das Gedächtnis.
5. „— alle Ziele —“ zeigt alle kommissionier-relevanten Positionen und wird gemerkt.
6. Einstellung in `/Settings` zeigt die Ziele als Checkboxen aus den Stammdaten; ein per URL/Formular
   untergeschobener unbekannter Wert wird beim Speichern abgewiesen, nichts wird gespeichert.
7. Spalte „Abteilung“ zeigt in Liste, Summiert, Druck und PDF die Montage-Abteilung des HauptFA
   (z. B. WKS); Kombigerät gemäß Rückfrage 4; Spaltenfilter „WKS“ wirkt serverseitig vor dem Paging.
8. Spalte „Lagerplätze“ zeigt alle Plätze mit Bestand > 0 inkl. Menge, Hauptplatz zuerst; Plätze mit
   0 oder negativem Bestand und Kommissionierwagen fehlen; ohne Bestand „—“.
9. Spaltenfilter „K-02“ auf Lagerplätze liefert nur Positionen mit Bestand auf einem Platz, dessen
   Code „K-02“ enthält; Paging stimmt (Seitenzahl bezieht sich auf die gefilterte Menge).
10. Je Seitenaufruf genau eine Bestandsabfrage (zwei SQL-Statements), keine je Zeile (SQL-Profiler
    oder EF-Log im Test).
11. Gate an: ein nicht freigegebener HauptFA fehlt in Liste, Summiert, Druck, PDF; Banner nennt die
    Anzahl. Nach Freigabe im Leitstand erscheint er; nach Rücknahme verschwindet er wieder.
12. Gate an, `LeitstandAktiv` aus: Warnbanner sichtbar.
13. Gate aus: Verhalten wie v1.99.41, kein Banner.
14. Direktaufruf PDF eines nicht freigegebenen HauptFA bei Gate an → Warnmeldung, kein PDF.
15. AKE: Modul aus (`FaHierarchyKommissionierlistenAktiv = false`) → keine Änderung sichtbar;
    `/Settings` zeigt die neuen IDEAL-Einstellungen ohne Fehler.
16. Drift-Waechter, neue Unit-Tests (Prädikat mehrwertig, Lagerplatz-Format, Gate-Filter,
    Validator MultiChoice-Stammdaten), Build und alle Tests grün.

## Test-Szenarien

Neues Kapitel **TS-163** (vorläufig; Dev-Lauf prüft nächste freie Nummer) in `docs/TESTSZENARIEN.md`
+ `secondbrain/tests/testszenarien-index.md`. Standort IDEAL-DEV.
- **Vorbedingungen:** Modul Kommissionierlisten an, `LeitstandAktiv` an, Knoten mit Zielen K-01,
  K-02 und mindestens einem weiteren Ziel; zwei Benutzer mit Lagerrecht, einer mit Leitstandrecht;
  ein Artikel mit Bestand auf zwei Plätzen, einer mit Bestand nur auf einem Kommissionierwagen; ein
  Kombigerät-HauptFA.
- **TS-163.1** Vorfilter/Gedächtnis/Zurücksetzen/alle Ziele (AK 1-5), zweiter Benutzer.
- **TS-163.2** Einstellung pflegen, Fehleingabe per Entwicklertools (AK 6).
- **TS-163.3** Abteilung inkl. Kombigerät und Filter (AK 7), Druck + PDF.
- **TS-163.4** Lagerplätze, Filter, Wagen-/Nullbestand, Druck ungepaged (AK 8-10).
- **TS-163.5** Gate: Freigabe/Rücknahme im Leitstand, Banner, PDF-Direktaufruf, Schalter aus
  (AK 11-14); Negativ: `LeitstandAktiv` aus (AK 12).
- **TS-163.6** AKE-Regression (AK 15) und Menü öffnet Summiert.

## Etappen

Epic im bestehenden Bündel-Worktree, **ein** Merge. Begründung: vier unabhängig testbare Schritte,
die dieselben Dateien (Views, ColumnDefinitions, Service) berühren — getrennte Merges würden nur
Konflikte erzeugen; Etappen halten jeden Dev-Lauf im Turn-Limit. Keine Etappe hat eine Migration,
keine wartet auf eine Sage-Prüfabfrage. Rückfragen 4/5 betreffen nur Formatdetails in E2/E3; ohne
Antwort baut der Lauf die Empfehlung und vermerkt das. Zweig während der Arbeit mit
`scripts/sync-worktree.ps1` aktuell halten.

| # | Etappe | Status | Commit |
|---|---|---|---|
| E1 | Stammdaten-MultiChoice (Validator, Feld, Prüfung) + Einstellung Vorfilter + Mehrfach-Ziel + Gedächtnis + „Ziele auf Standard“ (AK 1-6) | offen | |
| E2 | Spalte Abteilung an allen vier Stellen beider Ansichten + Druck (AK 7) | offen | |
| E3 | Spalte Lagerplätze, Batch-Laden, Filter, Druck (AK 8-10) | offen | |
| E4 | Gate + Repository-Methode + Banner + PDF-Warnung (AK 11-15), Manifest, Hilfe, README, Version 1.99.43 (vorläufig, 1.99.42 ist durch BF-Nachbesserung reserviert), Anwender- und Brain-Changelog, TS-163 | offen | |

## Deploy

*Vorläufig — der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Controller, Service, Views, Einstellungen).
- **Service:** nein.
- **Migration:** nein.

**Manifest-Einträge (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):**

| Id | Version | Typ | Firma | Phase | Ausführung | Text / Prüfung |
|---|---|---|---|---|---|---|
| `Pruefung/KommissionierlisteNichtFreigegeben` | 1.99.43 | Pruefung | IDEAL | Vorher | anzeigen | Zählt HauptFA mit Kommissionier-Positionen ohne Freigabe (verschwinden nach Deploy). SQL s. u. (Q-B als `COUNT`). Text: „Diese Aufträge sind nach dem Update nicht mehr in der Kommissionierliste, bis der Leitstand sie freigibt (oder Schalter aus).“ |
| `Pruefung/KommissionierlisteLeitstandAus` | 1.99.43 | Pruefung | IDEAL | Nachher | anzeigen | `SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key]=N'LeitstandAktiv' AND LOWER([Value])=N'true') THEN 0 WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key]=N'FaHierarchyKommissionierlisteNurFreigegebene' AND LOWER([Value])=N'false') THEN 0 ELSE 1 END` — Text: „Gate an, Leitstand aus: Kommissionierliste bleibt leer.“ |
| `Pruefung/KommissionierlisteVorfilterZiele` | 1.99.43 | Pruefung | IDEAL | Nachher | anzeigen | `SELECT CASE WHEN OBJECT_ID(N'dbo.FaHierarchyNodes',N'U') IS NULL THEN 0 WHEN EXISTS (SELECT 1 FROM dbo.FaHierarchyNodes WHERE VerschwundenSeit IS NULL AND LTRIM(RTRIM(Kommissionieren)) IN (N'K-01',N'K-02')) THEN 0 ELSE 1 END` — Text: „Standard-Ziele K-01/K-02 kommen in der FA-Struktur nicht vor – Einstellung prüfen.“ |
| `v1.99.43-IDEAL-Kommissionierliste-Schalter` | 1.99.43 | Einstellung | IDEAL | Nachher | anzeigen | „Kommissionierliste nur nach Leitstand-Freigabe ist an (Seed). Je nach Prüfung oben: Aufträge im Leitstand freigeben oder Schalter vorübergehend aus. Standard-Ziele unter /Settings prüfen.“ |
| `v1.99.43-IDEAL-Kommissionierliste-Info` | 1.99.43 | Info | IDEAL | Nachher | anzeigen | „Kommissionierer informieren: Summiert zeigt nur eigene Ziele, Ziele werden je Benutzer gemerkt, neue Spalten Abteilung/Lagerplätze.“ |

Alle Prüfungen: 0 = in Ordnung (Format wie `nachlauf.psd1:128-141`); in der psd1 Hochkommas
verdoppeln. Keine Daten-/Einmal-Skripte, daher kein Trockenlauf.

**Publish-Befehle:** wie Bündel üblich (nur Web, IDEAL-DEV); kein Service-Stopp.

## Prüfabfragen für Gerald (WMS-DB IDEAL, vor Freigabe/Deploy)

Fakten, die nur in der IDEAL-Datenbank stehen — hängen davon ab: Übergangsrisiko (Rückfrage 2/3).

```sql
-- Q-A: Leitstand an IDEAL aktiv?
SELECT [Value] FROM dbo.AppSettings WHERE [Key] = N'LeitstandAktiv';

-- Q-B: HauptFA mit Kommissionier-Positionen nach Freigabestand (ohne Statuszeile = nie freigebbar)
SELECT CASE WHEN po.Id IS NULL THEN N'keine ProductionOrder'
            WHEN ps.Id IS NULL THEN N'keine Statuszeile'
            WHEN ps.IsReleasedForPicking = 1 THEN N'freigegeben'
            ELSE N'nicht freigegeben' END AS Stand,
       COUNT(DISTINCT n.HauptFA) AS HauptFA
FROM dbo.FaHierarchyNodes n
LEFT JOIN dbo.ProductionOrders po
       ON po.OrderNumber = CAST(n.HauptFA AS nvarchar(50))
      AND (po.SubOrderNumber = N'' OR po.SubOrderNumber = po.OrderNumber)
LEFT JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId = po.Id
WHERE n.VerschwundenSeit IS NULL
  AND LTRIM(RTRIM(ISNULL(n.Kommissionieren, N''))) <> N''
GROUP BY CASE WHEN po.Id IS NULL THEN N'keine ProductionOrder'
              WHEN ps.Id IS NULL THEN N'keine Statuszeile'
              WHEN ps.IsReleasedForPicking = 1 THEN N'freigegeben'
              ELSE N'nicht freigegeben' END;

-- Q-C: Ziele, die tatsaechlich vorkommen (Schreibweise K-01/K-02?)
SELECT LTRIM(RTRIM(Kommissionieren)) AS Ziel, COUNT(*) AS Knoten
FROM dbo.FaHierarchyNodes WHERE VerschwundenSeit IS NULL
GROUP BY LTRIM(RTRIM(Kommissionieren)) ORDER BY Ziel;

-- Q-D: HauptFA-Wurzeln ohne Artikelnummer (Freigabe im Leitstand blockiert)
SELECT po.OrderNumber FROM dbo.ProductionOrders po
WHERE (po.SubOrderNumber = N'' OR po.SubOrderNumber = po.OrderNumber)
  AND LTRIM(RTRIM(ISNULL(po.ArticleNumber, N''))) = N'';
```

Spaltennamen der Statustabelle belegt: `SQL/00_FreshInstall.sql:431-436` (`Id`, `ProductionOrderId`,
`IsReleasedForPicking`).

## Offene Rueckfragen

1. Soll das Gate zusätzlich erledigte HauptFA ausblenden (`IsDonePicking`/`IsDone`/`IsCancelled` wie
   die AKE-Warteschlange `GetReleasedForPickingAsync`, `ProductionOrderPickingStatusRepository.cs:189-200`)?
   **Empfehlung:** nein — in dieser Spec zählt nur das Freigabe-Flag; Ausblenden erledigter Aufträge
   als eigener Backlog-Punkt.
2. Übergang beim Deploy: heute sichtbare, nicht freigegebene HauptFA verschwinden. Vorab im Leitstand
   freigeben (legt Kommissionieraufträge an) oder Schalter bis dahin aus? **Empfehlung:** Schalter an
   ausliefern, Prüfung `KommissionierlisteNichtFreigegeben` + Q-B zeigen die Menge, Gerald entscheidet
   am DEV-Test.
3. `LeitstandAktiv` an IDEAL aus bei Gate an → leere Liste. **Empfehlung:** Gate bleibt wirksam,
   Warnbanner + Manifest-Prüfung, keine automatische Kopplung.
4. Abteilung bei Kombigerät: nur „Kombigerät“ (wie Vormontage) oder „Kombigerät (WKS, FKV)“?
   **Empfehlung:** „Kombigerät (WKS, FKV)“, damit der Spaltenfilter „WKS“ auch Kombigeräte findet;
   Vormontage/BDE bleiben unverändert (optionaler Parameter).
5. Lagerplätze: Kommissionierwagen-Bestand anzeigen, und Mengenformat? **Empfehlung:** Wagen
   ausgeschlossen (Standard-Overload), Menge ohne nachlaufende Nullen („K-02 (12)“, „(2,5)“).

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

—

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. Gate blendet zusätzlich erledigte HauptFA aus? →
2. Deploy-Übergang: vorab freigeben oder Schalter vorübergehend aus? →
3. LeitstandAktiv aus bei Gate an: Banner + Prüfung genügt? →
4. Abteilung Kombigerät: „Kombigerät“ oder „Kombigerät (WKS, FKV)“? →
5. Lagerplätze: Wagen ausschließen, Format ohne nachlaufende Nullen? →
