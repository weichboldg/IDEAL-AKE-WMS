---
type: spec
title: "IDEAL: Kommissionierung nur auf dem HauptFA — gemeinsame Relevanzregel, fünfter Stückliste-Filter, Freigabe-Kaskade-Rückbau, Picking-Pfad auf HauptFA beschränkt"
slug: 2026-09-25-kommissionierung-nur-hauptfa-spec
status: Entwurf
created: 2026-09-25
updated: 2026-09-25
source_backlog: "[[2026-09-13-kommissionierung-nur-hauptfa]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Services/KommissionierListenService.cs Z. 228-236 (`BuildFlagPredicate`) — Kriterium NICHT ändern (bleibt einzig: `Kommissionieren` nicht leer), aber in eine neue, gemeinsame Methode auslagern statt es dort inline zu prüfen (siehe neue Datei unten). NICHT anfassen: `leafOnly:false`/`anomalyOnNonLeaf:false`-Aufruf (Nachtrag 2026-08-13) — SubFA spielt seit damals bewusst keine Rolle mehr."
  - "IdealAkeWms/Services/KommissionierRelevanzFilter.cs (NEU) — EINE reine, statische Methode `IsRelevant(string? kommissionieren) => !string.IsNullOrWhiteSpace(kommissionieren)`, dokumentiert als die von Kommissionierliste UND Stückliste-Checkbox geteilte Regel (C#-Seite; die Stückliste spiegelt dieselbe Regel client-seitig auf demselben Feld, siehe unten — echtes Teilen über die Sprachgrenze hinweg ist nicht möglich, ohne einen Server-Roundtrip einzuführen, der für einen Leerstring-Check unverhältnismäßig wäre)."
  - "NUR falls Freigabe-Antwort 2 einen Standorteinstellungen-Schalter verlangt (siehe offene Rückfrage 2): IdealAkeWms/Models/AppSettingKeys.cs (neuer Key `FaHierarchyKommissionierRelevanzAktiv`, Kommentarblock nach Vorlage Z. 63-68) + IdealAkeWms/Program.cs Z. 405-411 (`idealFaHierarchySettings`-Array, neue Zeile mit Default `\"true\"` — INVERTIERT zu den anderen vier Einträgen dort, die alle Default `\"false\"` sind, weil dieser Schalter das HEUTIGE Verhalten bit-identisch fortsetzen muss) + IdealAkeWms/Models/Standort/StandortSettingsCatalog.cs Z. 46-49 (`GroupToggles`, neue Zeile) + IdealAkeWms/Views/Settings/Index.cshtml Z. 44 (IDEAL-Gruppen-Tupel ergänzen) + IdealAkeWms/Services/KommissionierListenService.cs (Konstruktor bekommt `IAppSettingRepository`, `BuildFlagPredicate` liest den Schalter; bei `false` liefert die Methode `_ => true`, d.h. Kommissionierliste zeigt dann wieder ALLE Positionen ungefiltert — bewusste Rückfalloption, siehe Technischer Lösungsentwurf)."
  - "IdealAkeWms/Views/Picking/Bom.cshtml (fünfter Standardfilter, NUR `@if (Model.Hierarchical)`): neue Checkbox „Nur kommissionierrelevant\" (in der Werkzeugleiste neben `btnExpandAll`/`btnCollapseAll`, Z. 76-83), neue JS-Funktion `bomKommissionierRelevanzActive()` (liest Checkbox-Status), Erweiterung von `updateBomVisibility()` (Z. 855-895) um eine zusätzliche, NICHT spaltenwertbasierte Sichtbarkeitsbedingung (Zeile nur sichtbar, wenn zusätzlich `KommissionierRelevanzFilter`-Kriterium erfüllt — client-seitig identisch geprüft: Zellinhalt der Spalte `kommissionieren` nicht leer), Erweiterung von `renderDefaultFilterBadges()`/`resetDefaultFilter`-Mechanik (Z. 985-1033) um einen fünften Badge-Eintrag ohne zugehöriges `<input data-col-key>` (eigener Zweig, da die anderen vier über `bomFilterInputValue(key)` funktionieren, die Checkbox aber keinen Spaltenfilter-Input hat), Erweiterung von `resetAllBomFilters()` (Z. 1035-1044, das De-facto-„Filter löschen\" dieser Ansicht — siehe Fachliche Anforderung 3) um das Zurücksetzen der Checkbox, Erweiterung des Druck-Handlers `btnPrintBom` (Z. 1167-1211) um einen zusätzlichen `filterParts`-Eintrag „Nur kommissionierrelevant\", wenn die Checkbox aktiv ist (die eigentliche Zeilenbeschränkung im Ausdruck läuft bereits automatisch über den bestehenden `visiblePositions`-Mechanismus, siehe Technischer Lösungsentwurf — KEINE Änderung an `PrintBom.cshtml`/`colNames` nötig)."
  - "NUR falls Freigabe-Antwort 1 einen PRO BENUTZER gespeicherten Default verlangt: IdealAkeWms/Models/User.cs (neues Feld `DefaultFilterBomKommissionierRelevanz`, bool, non-nullable mit Default `false` ODER `bool?`, exaktes Vorbild `DefaultFilterBomKommissionierziel` Z. 86-91) + Migration `AddUserDefaultFilterBomKommissionierRelevanz` + `SQL/94_AddUserDefaultFilterBomKommissionierRelevanz.sql` (COL_LENGTH/OBJECT_ID-Guard nach Vorlage `SQL/92_...sql`) + `SQL/00_FreshInstall.sql` (beide Stellen) + `Models/ViewModels/UserEditViewModel.cs`/`ProfileViewModel.cs` + `Views/Users/Edit.cshtml`/`Views/Account/Profile.cshtml` (Checkbox statt Textfeld — anders als die vier Textfilter, weil hier kein OR-Freitext, sondern ein reiner Ja/Nein-Schalter nötig ist) + `Controllers/UsersController.cs`/`AccountController.cs` (Feld durchreichen) + `Controllers/PickingController.cs` (Bom-Action, analog Z. 296/307, ins BomViewModel legen) + `Models/ViewModels/BomViewModels.cs` (neue Property)."
  - "IdealAkeWms/Controllers/PickingController.cs Z. 280-290 (`Bom`-Action) — NEUER Guard direkt nach dem `order == null`-Check: ist `order.SubOrderNumber` gesetzt und ungleich `order.OrderNumber` (= Sub-FA, nicht die Wurzel — Invariante aus Fallstricke §9, für AKE folgenlos, da dort `SubOrderNumber == OrderNumber` immer gilt), TempData[\"WarningMessage\"] = \"Kommissionierung erfolgt nur am HauptFA {OrderNumber}.\" + Redirect (z. B. `RedirectToAction(\"Index\", \"FaHierarchy\")` oder zurück zum Leitstand über `returnUrl`, am Code zu verifizieren, was die bessere Landestelle ist). Rein datengetrieben, KEIN zusätzlicher Master-Switch-Read nötig."
  - "IdealAkeWms/Controllers/PickingLeitstandController.cs Z. 356-405 (`ToggleRelease`) — derselbe Sub-FA-Guard vor der Freigabe-Logik (order bereits geladen, `order.SubOrderNumber`/`OrderNumber` bereits vorhanden, keine zusätzliche Query)."
  - "IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs Z. 218-281 (`SetReleaseBatchAsync`) — analog zum bestehenden `SkippedNoArticle`-Zweig (Z. 246-250) einen neuen Skip-Zweig für Sub-FA-Zeilen (`row.ProductionOrder.SubOrderNumber != row.ProductionOrder.OrderNumber` bei `release == true`), neues Feld `BulkReleaseResult.SkippedSubFa` (`IdealAkeWms/Data/Repositories/IProductionOrderPickingStatusRepository.cs` Z. 5-9)."
  - "IdealAkeWms/Controllers/PickingLeitstandController.cs Z. 409-451 (`BulkRelease`) — neue `TempData[\"WarningMessage\"]`-Zeile für `batch.SkippedSubFa`, analog der bestehenden Zeile für `SkippedNoArticle` (Z. 446-447)."
  - "IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml — lokale Variable `isHauptFaRow` nach EXAKTEM Vorbild `Views/ProductionOrders/_ProductionOrderRow.cshtml` Z. 16-18 (`Model.Hierarchical && item.SubOrderNumber == item.OrderNumber`, kein neues ViewModel-Feld — bewusstes Precedent, siehe dortiger Kommentar); die Freigabe-Spalte (Z. 217-259: ToggleRelease-Formular, Freigeben-Button, Prioritäts-Input) UND die Bulk-Checkbox-Spalte (Z. 18-29) werden zusätzlich auf `!Model.Hierarchical || isHauptFaRow` bedingt — auf Sub-FA-Zeilen verschwindet das Bedienelement vollständig (Einschätzung aus dem Backlog: ein wirkungsloses Element ist schlimmer als keines)."
  - "IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml Z. 30-42 UND IdealAkeWms/Views/ProductionOrders/_ProductionOrderRow.cshtml Z. 30-49 — der INTERAKTIVE Stückliste-Link (`asp-controller=\"Picking\" asp-action=\"Bom\"`, NICHT der read-only `FaWorklist`-Zweig daneben) zusätzlich auf `!Model.Hierarchical || isHauptFaRow` bedingt. Der `FaWorklist/Bom`-Zweig (read-only, BDE-Referenz) bleibt UNVERÄNDERT auf jeder Sub-FA-Zeile sichtbar — Rückmeldung/Referenz ist nicht Kommissionierung."
  - "RÜCKBAU (Block 4/Tasks 12-14 aus [[2026-09-10-fa-liste-ausbau-matchcode-spec]], bereits gebaut, Testbereit, noch NICHT gemergt): IdealAkeWms/Controllers/PickingLeitstandController.cs Z. 543-616 (`CascadeReleasePreview`/`CascadeRelease` inkl. Kommentarblock Z. 543-549) vollständig entfernen. IdealAkeWms/Data/Repositories/IProductionOrderPickingStatusRepository.cs Z. 56 (`SetReleaseForOrderNumberAsync`) + Z. 62 (`CountReleasedByOrderNumberAsync`) + zugehörige Implementierung in ProductionOrderPickingStatusRepository.cs (Z. 283ff. und `CountReleasedByOrderNumberAsync`) entfernen — VORHER am Code prüfen, ob `CountReleasedByOrderNumberAsync` noch woanders verwendet wird (z. B. eine KPI-Anzeige), sonst mitreissen. IdealAkeWms/Views/PickingLeitstand/Index.cshtml Z. 212 (`.btn-cascade-release`-Button), Z. 400-451 (`cascadeReleaseModal`) und Z. 559-590ff. (zugehöriges JS) vollständig entfernen. IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs + IdealAkeWms.Tests/Repositories/ProductionOrderPickingStatusRepositoryTests.cs (Tests zu `CascadeRelease*`/`SetReleaseForOrderNumberAsync` entfernen). docs/TESTSZENARIEN.md TS-73 (v1.38.0) — die Freigabe-Kaskade-Szenarien als „zurückgebaut, siehe TS-79\" kennzeichnen statt löschen (Vorbild: ADR-0014-Rückbau, TS-71.6-71.9-Markierung). NICHT anfassen: `CascadeDonePreview`/`CascadeDone` (Z. 488-541, Fertigmeldungs-Kaskade Teil 7/8 — bleibt unverändert, betrifft `IsDoneBde`, nicht `IsReleasedForPicking`)."
  - "secondbrain/specs/freigegeben/2026-09-10-fa-liste-ausbau-matchcode-spec.md (HAUPTCHECKOUT, Brain-Update-Pflicht des Dev-Laufs, NICHT dieser Spec-Agent-Lauf) — Block 4/Tasks 12-14 als durch diese Spec überholt/zurückgebaut kennzeichnen, damit die freigegebene Spec nicht widersprüchlich zum tatsächlichen Codezustand bleibt."
  - "docs/TESTSZENARIEN.md (neues Kapitel TS-79) + secondbrain/tests/testszenarien-index.md (Eintrag TS-79)."
  - "IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs (Version) + Views/Help/Changelog.cshtml."
open_questions:
  - "Ist die Checkbox beim Öffnen der Stückliste standardmäßig an oder aus — und soll sie pro Benutzer speicherbar sein wie die anderen vier Standardfilter?"
  - "Der Code zeigt für die Kommissionierliste nur EIN Relevanzkriterium (Feld `Kommissionieren` nicht leer) statt der im Backlog vermuteten mehreren Parameter (SubFA/Beschaffungsartikel/Artikelgruppe) — SubFA-Filterung wurde am 2026-08-13 sogar bewusst entfernt. Reicht die reine Extraktion der Regel in EINE geteilte C#-Methode (kein Standorteinstellungen-Eintrag, da nichts Echtes zu konfigurieren ist), oder soll trotzdem ein Ein/Aus-Schalter `FaHierarchyKommissionierRelevanzAktiv` gebaut werden (z. B. als Rückfallebene, falls Sage das Feld `Kommissionieren` unzuverlässig befüllt)?"
  - "Im IDEAL-Testsystem existieren laut Backlog bereits einzeln freigegebene Sub-FAs (`IsReleasedForPicking=true`). Sollen diese Zeilen nach dem Rückbau/der Beschränkung unverändert stehen bleiben (Empfehlung dieser Spec: ja — reine Testdaten, keine produktive Migration nötig, die Zeile bleibt für Glas/Fremdbezug/Lackierung ohnehin bestehen), oder soll ein einmaliger Datenlauf sie im Testsystem manuell zurücksetzen?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> [!info] Umsetzungsort (Hinweis, keine Frontmatter-Vorgabe)
> Der gesamte betroffene Code (`FaHierarchyKommissionierListenController`, `KommissionierListenService`,
> `PickingController`, `PickingLeitstandController`, `Bom.cshtml`, `StandortSettingsCatalog` usw.)
> existiert **nur** im nicht gemergten Bündel-Worktree
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch `feature/2026-08-07-ideal-teile-1-5`,
> aktueller Stand bei Spec-Erstellung: `AppVersion 1.45.0`, höchstes Testszenarien-Kapitel `TS-78`,
> höchste SQL-Nummer `93`). Im `main`-Checkout, in dem dieser Spec-Agent-Lauf ausgeführt wurde,
> existiert davon **nichts**. Die Umsetzung erfolgt dort als weitere Etappe desselben Bündels — kein
> neuer Worktree. Der Dev-Lauf verifiziert AppVersion/TS-Nummer/SQL-Nummer gegen den dann aktuellen
> Worktree-Stand, bevor er sie festschreibt.

## Ziel / Nutzen (das Warum)

[[2026-09-13-kommissionierung-nur-hauptfa]] (Erweiterung 2026-09-23) macht die bisher offene
Scope-Frage endgültig: Kommissioniert wird **ausschließlich am HauptFA**, inklusive der kompletten
Stücklisten-Vollstruktur (Ruling 4 bleibt, Rückbau entfällt). Das löst den doppelten Materialzug
auf und macht eine bereits gebaute, aber jetzt gegenstandslose Freigabe-Kaskade auf Sub-FA-Ebene
überflüssig — und wirft die Frage auf, ob die Kommissionierliste ihre Positionen nach fest
codierten Regeln oder nach admin-pflegbaren Einstellungen aussiebt, und ob dieselbe Regel auch der
Stückliste hilft, aus der jetzt langen Vollstruktur nur die wirklich zu holenden Positionen
herauszufiltern. Diese Spec zieht vier zusammenhängende Fäden zusammen, die alle aus derselben
Entscheidung folgen:

1. Die Kommissionierliste (Teil 3) und die Stückliste (`/Picking`) sollen **dieselbe** Regel
   verwenden, statt zwei Implementierungen, die auseinanderlaufen können.
2. Eine bereits gebaute, aber durch die Entscheidung sinnlos gewordene Freigabe-Kaskade wird
   zurückgebaut, bevor sie in den Merge des Bündels geht.
3. Freigabe und interaktive Kommissionierung werden **strukturell** auf die HauptFA-Zeile
   beschränkt — ein Bedienelement, das nichts (mehr) bewirkt, verschwindet.
4. Die Druck-Whitelist-Erweiterung (Ebene/Komm.-Ziel), die der Backlog als „vor dem Merge fällig"
   markiert hatte, ist bereits durch [[2026-09-18-stueckliste-kommissionierziel-filter-spec]]
   (v1.42.0, Testbereit) erledigt — hier nur bestätigt, nicht erneut gebaut.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

1. **Erhebung + Korrektur der Backlog-Annahme:** Die Kommissionierliste filtert heute
   ausschließlich nach EINEM Kriterium — Feld `Kommissionieren` nicht leer
   (`KommissionierListenService.BuildFlagPredicate`, Z. 228-236). SubFA-Filterung wurde am
   2026-08-13 (Nachtrag 1, `leafOnly:false`) explizit entfernt; Beschaffungsartikel/Artikelgruppe
   spielen für die Relevanz **keine** Rolle (sie existieren nur als eigenständige, vom Anwender
   frei wählbare Spaltenfilter, `ColumnMap`, nicht als Bestandteil der Grundmenge). Diese Spec
   dokumentiert den Befund und zieht daraus die Konsequenz für den Umfang der „Standorteinstellung"
   (siehe offene Rückfrage 2).
2. **Die Relevanzregel wird an EINER Stelle definiert** (`KommissionierRelevanzFilter.IsRelevant`)
   und von der Kommissionierliste genutzt; die Stückliste spiegelt dieselbe Regel client-seitig auf
   demselben Feld (`Kommissionieren`/`Bom.Kommissionieren`) — echtes Teilen einer kompilierten
   Funktion über die C#/JS-Sprachgrenze ist nicht sinnvoll erreichbar, ohne einen Server-Roundtrip
   für einen reinen Leerstring-Check einzuführen.
3. **Fünfter Standardfilter in der Stückliste** (`/Picking`, nur `Model.Hierarchical`): Checkbox
   „Nur kommissionierrelevant", die Positionen mit leerem `Kommissionieren`-Feld ausblendet — nach
   demselben Muster wie die vier bestehenden Standardfilter aus
   [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] (Badge bei aktivem Filter, Ein-Klick-
   Reset, Rücksetzung über den bestehenden „Alle Filter zurücksetzen"-Mechanismus dieser Ansicht).
4. **Freigabe-Kaskade zurückbauen** (Block 4/Tasks 12-14 aus
   [[2026-09-10-fa-liste-ausbau-matchcode-spec]], Testbereit, noch nicht gemergt): Sie gäbe
   Sub-FAs frei, die künftig nie kommissioniert werden — ein Zustand ohne Bedeutung.
5. **Freigabe- und interaktiver Kommissionierpfad strukturell auf die HauptFA-Zeile beschränkt**:
   Zeilen-Toggle (`ToggleRelease`), Mehrfachauswahl (`BulkRelease`), die interaktive
   Stückliste-Aktion (`Picking/Bom`) — sowohl das Bedienelement in der UI als auch ein
   serverseitiger Guard (Verteidigung in der Tiefe, „Melden statt still behandeln").
6. Testszenarien (neues Kapitel TS-79) inkl. Rückbau-Markierung in TS-73.

**Out-of-Scope**

- **Druck-Whitelist-Erweiterung (Ebene, Komm.-Ziel) in `PrintBom.cshtml`** — bereits umgesetzt durch
  [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] (v1.42.0, verifiziert:
  `Views/Picking/PrintBom.cshtml` Z. 207-259 enthält bereits `ShowCol("kommissionieren")`,
  `ShowCol("hauptlagerplatz")`, `ShowCol("ebene")`). Diese Spec fügt lediglich einen zusätzlichen
  `filterInfo`-Textbaustein hinzu, wenn die neue Checkbox aktiv ist (Punkt 3 oben) — keine neue
  Spalte, kein neuer `ShowCol`-Block.
- **BDE-Rückmeldung (Teil 8)** — bleibt unverändert auf Sub-FA-Ebene, nicht Gegenstand dieser Spec.
  Die `ProductionOrderPickingStatus`-Zeile der Sub-FAs wird **nicht** zurückgebaut (trägt weiterhin
  Glas/Fremdbezug/Lackierung, siehe [[2026-09-09-materialisierung-ohne-statuszeilen-bug]]).
- **Beschichtungsauftrag/Vormontage-Listen (Teil 4/5)** — gruppieren nach Arbeitsbereich, nicht
  nach FA-Ebene, unberührt.
- **`FaWorklist/Bom` (read-only Stücklisten-Referenz für BDE-Werker)** — bleibt auf jeder Sub-FA-
  Zeile sichtbar. Rückmeldung/Referenz ist nicht Kommissionierung (siehe Backlog, Abschnitt „Was
  ausdrücklich NICHT betroffen ist").
- **AKE** — jede Änderung dieser Spec ist entweder rein datengetrieben (Sub-FA-Guard über
  `SubOrderNumber != OrderNumber`, bei AKE wegen der Invarianz aus Fallstricke §9 immer falsch) oder
  hinter `Model.Hierarchical`/`@if (Model.Hierarchical)` gated — für AKE folgenlos, keine gesonderte
  Prüfung nötig, aber Testszenarien decken die Nicht-Wirkung ausdrücklich ab.
- **Kombinationsgeräte** — unverändert außen vor (paketweit entschieden, siehe Materialisierungs-
  Spec).

## Fachliche Anforderungen

### 1 — Gemeinsame Kommissionier-Relevanzregel

Die Kommissionierliste (`KommissionierListenService.BuildFlagPredicate`) prüft heute inline
`!string.IsNullOrWhiteSpace(n.Kommissionieren)`. Diese Prüfung wandert unverändert (keine
Verhaltensänderung) in eine neue, benannte, pure Methode `KommissionierRelevanzFilter.IsRelevant`,
die als die von beiden Verbrauchern geteilte Definition dokumentiert ist. `BuildFlagPredicate` ruft
diese Methode auf, statt die Prüfung erneut zu schreiben — **ein** Ort für die Regel, nicht zwei
Implementierungen, die mit der Zeit auseinanderlaufen (Leitgedanke des Auftrags).

Der Ziel-Wert-Einschränkung (`targetValue`-Parameter des Dropdowns in der Kommissionierliste) bleibt
davon unberührt — sie ist eine zusätzliche, vom Anwender gewählte Einschränkung, keine Änderung der
Grundregel.

### 2 — Standorteinstellungen (Teil 6): korrigierter Umfang

Der Auftrag verlangt, die „Filterparameter" der Kommissionierliste in die IDEAL-
Standorteinstellungen zu heben. Die Erhebung ergibt: **es gibt nur diesen einen Parameter**, und er
ist kein Wert, sondern eine feste Feldsemantik (das Feld `Kommissionieren` kommt 1:1 aus der
Sage-View und ist nicht sinnvoll gegen ein anderes Feld austauschbar). Es gibt damit **nichts
Echtes zu konfigurieren** — eine Standorteinstellung ohne echten Einstellwert wäre eine Attrappe.

Diese Spec baut deshalb **standardmäßig keinen** Standorteinstellungen-Eintrag, sondern nur die
Extraktion aus Anforderung 1. Falls der Mensch dennoch eine Rückfallebene für den Fall will, dass
Sage das Feld `Kommissionieren` in der Praxis unzuverlässig befüllt (dann könnte ein Admin die
Relevanzprüfung ohne Code-Deploy ganz abschalten und die Kommissionierliste zeigt wieder **alle**
Positionen), steht dafür der Entwurf in `affected_code` bereit: ein Bool-`AppSetting`
`FaHierarchyKommissionierRelevanzAktiv`, Default `"true"` (bit-identisch zum heutigen Verhalten),
Gruppe „Feature-Schalter" in der Standorteinstellungen-Maske. **Der Schalter beträfe ausschließlich
die Kommissionierliste** — die neue Stückliste-Checkbox (Anforderung 3) ist bereits selbst ein
Ein/Aus-Schalter pro Aufruf und braucht keine zusätzliche Admin-Ebene darüber.

Siehe offene Rückfrage 2.

### 3 — Fünfter Standardfilter in der Stückliste

Analog zu den vier bestehenden Standardfiltern (Beschaffung, Artikelgruppe, Bezeichnung 1,
Kommissionierziel — [[2026-09-18-stueckliste-kommissionierziel-filter-spec]]):

- Neue Checkbox „Nur kommissionierrelevant" **nur im hierarchischen Modus** (die Spalte
  `kommissionieren` existiert im AKE-Flachmodus gar nicht — dasselbe Gate wie beim
  Kommissionierziel-Dropdown).
- Aktiv geschaltet blendet sie alle Positionen aus, deren `Kommissionieren`-Zelle leer ist —
  **dieselbe** Regel wie die Kommissionierliste (Anforderung 1), hier client-seitig auf dem bereits
  gerenderten Zellentext geprüft (Client-Mode-Ausnahme dieser Ansicht, ADR 0005 Design F).
- **Aktiv muss sichtbar sein**: derselbe Badge-Mechanismus wie die vier bestehenden Filter
  („Gefiltert: Nur kommissionierrelevant"), Ein-Klick-Reset.
- Der bestehende „Alle Filter zurücksetzen"-Knopf dieser Ansicht (`resetAllBomFilters`, aktuell nur
  aus dem benannten Leerzustand heraus erreichbar, `#bomKzEmptyStateReset`) setzt die Checkbox mit
  zurück — das ist der in der Backlog-Erweiterung gemeinte „Filter löschen"-Knopf dieser Ansicht;
  ein zusätzlicher, generischer „Filter löschen"-Link existiert in `Bom.cshtml` nicht (verifiziert:
  kein `data-clear-table-filters`-Attribut in dieser View — dieses Muster gehört zum
  Server-Mode/sessionStorage-Mechanismus anderer Listen und wird hier NICHT nachgerüstet, das wäre
  Umfang, den niemand verlangt hat).
- **Druck:** Der Ausdruck respektiert eine aktive Checkbox bereits automatisch — der bestehende
  Druck-Handler sammelt `visiblePositions` direkt aus dem sichtbaren DOM-Zustand
  (`row.style.display !== 'none'`), unabhängig davon, wodurch eine Zeile ausgeblendet wurde. Nötig
  ist nur ein zusätzlicher Text-Hinweis im `filterInfo`-Parameter, damit der Ausdruck-Kopf „Nur
  kommissionierrelevant" nennt (Hausregel: sichtbar machen statt still filtern, gilt auch fürs
  Papier).
- **Default-Zustand und Persistenz:** offen, siehe Rückfrage 1.

### 4 — Rückbau der Freigabe-Kaskade

[[2026-09-10-fa-liste-ausbau-matchcode-spec]] Block 4 (Tasks 12-14) ist bereits gebaut, Status
`Testbereit`, **noch nicht gemergt** (verifiziert: `CascadeReleasePreview`/`CascadeRelease` in
`PickingLeitstandController.cs` Z. 543-616, Button+Modal in `Views/PickingLeitstand/Index.cshtml`,
Repository-Methode `SetReleaseForOrderNumberAsync`). Mit der Entscheidung „Kommissionierung nur am
HauptFA" gibt sie Sub-FAs frei, die nie kommissioniert werden — ein Zustand ohne fachliche Bedeutung
(genau der in der Backlog-ERWEITERUNG beschriebene Fall „falls ja: zurückbauen").

Vollständig zu entfernen: Controller-Actions, Repository-Methode(n) (`SetReleaseForOrderNumberAsync`
sicher, `CountReleasedByOrderNumberAsync` nach Prüfung auf Fremdverwendung), View-Button/-Modal,
zugehörige Tests. **Nicht** zu entfernen: die Fertigmeldungs-Kaskade `CascadeDonePreview`/
`CascadeDone` (Teil 7/8, betrifft `IsDoneBde`, ein anderer Sachverhalt — Rückmeldung, nicht
Freigabe). Der Zeilen-Toggle `ToggleRelease` und die Mehrfachauswahl `BulkRelease` bleiben
bestehen, werden aber durch Anforderung 5 auf HauptFA-Zeilen beschränkt.

TS-73 (v1.38.0) wird nicht gelöscht, sondern die Kaskade-Szenarien werden als „zurückgebaut, siehe
TS-79" gekennzeichnet — Vorbild ist der TS-71-Rückbau-Vermerk aus der ADR-0014-Korrektur (v1.43.0).

### 5 — Freigabe- und Picking-Pfad strukturell auf die HauptFA-Zeile beschränkt

Backlog-Frage 2 („Verschwindet der Freigabe-Knopf an Sub-FA-Zeilen?") wird mit **ja** beantwortet —
konsistent mit der im Backlog selbst festgehaltenen Einschätzung. Backlog-Frage 3 („Gilt das auch
für AKE?") wird mit **ja, aber folgenlos** beantwortet: Alle Sperren dieser Spec sind entweder rein
datengetrieben über die Schema-Inversions-Invariante (`SubOrderNumber == OrderNumber` bei AKE immer
wahr, Fallstricke §9) oder zusätzlich hinter `Model.Hierarchical` gated — kein Master-Switch-Read
nötig, keine AKE-Sonderbehandlung, die Sperre „hängt" also am Datenmodell, nicht an einem
zusätzlichen Schalter.

Konkret, doppelt abgesichert (UI **und** Server — „Melden statt still behandeln", ein Element, das
nichts bewirkt, verschwindet; ein direkter POST auf eine gesperrte Aktion wird abgewiesen, nicht
still ignoriert):

- **UI (Leitstand + FA-Liste):** Freigabe-Formular, Prioritäts-Input, Bulk-Checkbox und der
  **interaktive** Stückliste-Link (`Picking/Bom`) erscheinen auf Sub-FA-Zeilen im hierarchischen
  Modus nicht mehr. Die HauptFA-Zeile trägt sie unverändert.
- **Server:** `PickingController.Bom`, `PickingLeitstandController.ToggleRelease` und
  `ProductionOrderPickingStatusRepository.SetReleaseBatchAsync` weisen eine Sub-FA-Zeile mit einer
  sichtbaren `WarningMessage` zurück, statt sie stillschweigend zu verarbeiten oder mit einem
  unspezifischen Fehler abzubrechen.
- **Unverändert:** die read-only Stückliste (`FaWorklist/Bom`) für BDE-Referenz auf jeder Sub-FA-
  Zeile, sowie die Sichtbarkeit/Existenz der `ProductionOrderPickingStatus`-Zeile selbst (trägt
  weiterhin Glas/Fremdbezug/Lackierung-Flags unabhängig von der Freigabe).

## Technischer Lösungsentwurf

**Muster:** Repository-Pattern (ADR 0001), Listen-View-Pattern mit Client-Mode-Ausnahme für
`Bom.cshtml` (ADR 0005 Design F, wie bereits in [[2026-09-18-stueckliste-kommissionierziel-filter-spec]]
etabliert), Feature-Toggles über AppSettings (ADR 0011), Standorteinstellungen als kuratierte
Zweitansicht ohne eigenen Speicherort (Teil 6, [[2026-07-29-standort-ideal-teil-6-spec]]).

- **Geteilte Regel statt zweier Implementierungen:** `KommissionierRelevanzFilter.IsRelevant`
  ersetzt die Inline-Prüfung in `KommissionierListenService.BuildFlagPredicate`. Die Stückliste
  kann diese C#-Methode nicht direkt aufrufen (Client-Mode, kein Server-Roundtrip pro
  Sichtbarkeitsänderung) — sie spiegelt dieselbe Regel im JS auf demselben Feld. Diese
  Sprachgrenzen-Grenze wird explizit im Code-Kommentar der neuen Datei benannt, damit eine
  künftige Änderung der Regel (z. B. Trimmen zusätzlicher Werte) NICHT nur in der C#-Methode
  landet, sondern bewusst auch in `Bom.cshtml` nachgezogen wird — als Dauerwissen in
  `fallstricke.md` festzuhalten (siehe Checkliste).
- **Standorteinstellungen-Schalter (nur falls Freigabe-Antwort 2 ihn verlangt):**
  `KommissionierListenService` bekommt `IAppSettingRepository` als weiteren Konstruktor-Parameter;
  `BuildFlagPredicate` liest `FaHierarchyKommissionierRelevanzAktiv` und liefert bei `false`
  `_ => true` (alle Positionen, kein Filter) statt der Relevanzprüfung. Seed-Eintrag in
  `Program.cs` mit Default `"true"` — bewusst **nicht** invertiert wie die vier Nachbar-Einträge im
  selben Array (die sind Feature-Gates mit Default „aus", dieser hier ist eine Rückfallebene mit
  Default „heutiges Verhalten fortsetzen").
- **Stückliste-Checkbox:** rein clientseitig, keine neue Server-Abfrage — die Positionen (inkl.
  `Kommissionieren`-Wert) liegen beim Rendern bereits vollständig vor (ponytail Sprosse 2/3,
  dasselbe Argument wie beim Kommissionierziel-Dropdown). `updateBomVisibility()` bekommt eine
  zusätzliche boolesche Bedingung, die parallel zu den spaltenwertbasierten Filtern ausgewertet
  wird (UND-verknüpft mit Baum-Zustand und bestehenden Spaltenfiltern). Der Badge-Mechanismus
  bekommt einen fünften Eintrag mit eigenem Renderer (kein `<input data-col-key>` vorhanden, daher
  kein 1:1-Wiederverwenden von `bomFilterInputValue`, sondern eine eigene kleine Funktion, die den
  Checkbox-Status liest).
- **Sub-FA-Guard, rein datengetrieben:** Weder `PickingController.Bom` noch
  `PickingLeitstandController.ToggleRelease`/`SetReleaseBatchAsync` müssen den Master-Switch lesen
  — sie prüfen ausschließlich `order.SubOrderNumber != order.OrderNumber` auf dem ohnehin bereits
  geladenen `ProductionOrder`. Das ist dieselbe Formel, die `_ProductionOrderRow.cshtml` Z. 16-18
  bereits für die HauptFA-Badge-Anzeige verwendet — bewusst **kein** neues ViewModel-Feld, sondern
  lokal berechnet, exakt das dort dokumentierte Precedent („Die Materialisierung nimmt die Zeilen
  mit `SubFA != 0` plus die Wurzel [...], die Erkennung braucht kein eigenes ViewModel-Feld").
- **`SetReleaseBatchAsync`:** bekommt einen zusätzlichen Skip-Zweig direkt neben dem bestehenden
  `SkippedNoArticle`-Zweig (Z. 246-250) — dieselbe Stelle, dieselbe Struktur, ein zusätzliches Feld
  `BulkReleaseResult.SkippedSubFa`. Kein neuer Repository-Aufruf im Controller nötig, weil die
  Methode `.Include(s => s.ProductionOrder)` bereits fährt.

## Migrations-/SQL-Auswirkungen

**Im Standardumfang dieser Spec: keine.** Der neue Standorteinstellungen-Schalter (falls gebaut,
Rückfrage 2) ist ein `AppSetting` — generische Key/Value-Tabelle, kein Schema-Zugriff, kein
`dotnet ef migrations add` nötig (Seed-Insert in `Program.cs`, idempotent über
`if (!db.AppSettings.Any(s => s.Key == key))`). Der Rückbau der Freigabe-Kaskade entfernt nur
C#-Code (Controller-Actions, eine Repository-Methode) — keine Schema-Änderung, `SetReleaseAsync`/
`SetReleaseBatchAsync` (Einzel-/Mehrfachfreigabe) bleiben unverändert in der Tabelle
`ProductionOrderPickingStatuses`.

**Nur falls Freigabe-Antwort 1 einen pro Benutzer gespeicherten Checkbox-Default verlangt:**
Migration `AddUserDefaultFilterBomKommissionierRelevanz` (nächste freie Nummer am Worktree zu
verifizieren, Stand bei Spec-Erstellung `94` — höchste vorhandene Datei
`SQL/93_AddProductionWorkplaceSageFields.sql`), `SQL/94_AddUserDefaultFilterBomKommissionierRelevanz.sql`
nach exakter Vorlage `SQL/92_AddUserDefaultFilterBomKommissionierziel.sql`
(`COL_LENGTH`-Guard, `ALTER TABLE [dbo].[Users] ADD [DefaultFilterBomKommissionierRelevanz] BIT NOT NULL
DEFAULT 0` in eigenem Batch, `__EFMigrationsHistory`-Insert in separatem Batch),
`SQL/00_FreshInstall.sql` an beiden Stellen (Schema-Block `Users` + `MigrationId`-Liste). Nullable/
Default-Spalte an einer Bestandstabelle, **nicht daten-destruktiv**.

## Audit-Feld-Auswirkungen

Keine neue fachliche Entität. `ProductionOrderPickingStatus` bleibt `AuditableEntity` — die
entfernten Kaskade-Methoden hatten dieselben Audit-Aufrufe wie `SetReleaseAsync`/
`SetReleaseBatchAsync`, deren Rückbau ändert an den verbleibenden Schreibpfaden nichts. Der neue
Sub-FA-Skip-Zweig in `SetReleaseBatchAsync` schreibt für übersprungene Zeilen **keine**
Audit-Felder (analog zum bestehenden `SkippedNoArticle`-Zweig — Skip heißt keine Änderung, also
keine Modified-Felder). Falls das User-Feld aus Rückfrage 1 gebaut wird: `User` erbt
`AuditableEntity`, das Speichern läuft über denselben bestehenden Update-Pfad wie
`DefaultFilterBomKommissionierziel` (`UsersController`/`AccountController`, setzt bereits
`ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` über `ICurrentUserService`) — keine neue Logik nötig.

## Akzeptanzkriterien

1. `KommissionierListenService.BuildFlagPredicate` liefert für dasselbe Eingabe-Set exakt dieselbe
   Positionsmenge wie vor dieser Spec (reine Extraktion, keine Verhaltensänderung) — durch
   bestehende Unit-Tests der Kommissionierliste nachgewiesen.
2. `KommissionierRelevanzFilter.IsRelevant` ist eine eigenständig unit-testbare, reine Methode ohne
   DB-/HTTP-Abhängigkeit.
3. In der hierarchischen Stückliste (`Model.Hierarchical == true`) zeigt die Werkzeugleiste eine
   Checkbox „Nur kommissionierrelevant"; aktiviert blendet sie alle Positionen mit leerem
   `Kommissionieren`-Feld aus (Baugruppen-Eltern bleiben sichtbar, wenn ein Kind sichtbar ist —
   bestehendes Baum-Verhalten).
4. Im flachen AKE-Modus existiert die Checkbox nicht — unverändert zum heutigen Verhalten.
5. Ist die Checkbox aktiv, erscheint ein Badge „Gefiltert: Nur kommissionierrelevant" mit
   Ein-Klick-Reset; der Reset setzt nur diesen Filter zurück, nicht die anderen vier.
6. Der bestehende „Alle Filter zurücksetzen"-Mechanismus dieser Ansicht setzt auch die neue
   Checkbox zurück.
7. Ein Ausdruck (`PrintBom`) mit aktiver Checkbox zeigt im Kopf „Nur kommissionierrelevant" als
   Teil des Filterhinweises UND enthält nur die beim Klick sichtbar gewesenen Positionen
   (bestehender `visiblePositions`-Mechanismus, unverändert).
8. `PickingLeitstandController` enthält nach dem Rückbau **keine** Actions `CascadeReleasePreview`/
   `CascadeRelease` mehr; `IProductionOrderPickingStatusRepository` enthält **keine**
   `SetReleaseForOrderNumberAsync`-Methode mehr; `Views/PickingLeitstand/Index.cshtml` enthält
   **keinen** `.btn-cascade-release`-Button und **kein** `cascadeReleaseModal` mehr.
9. `CascadeDonePreview`/`CascadeDone` (Fertigmeldungs-Kaskade) sind vom Rückbau **unberührt** und
   funktionieren unverändert.
10. Auf einer Sub-FA-Zeile (hierarchischer Modus, `SubOrderNumber != OrderNumber`) sind in
    `PickingLeitstand/Index` und `ProductionOrders/Index` weder das Freigabe-Bedienelement noch
    die Bulk-Checkbox noch der interaktive Stückliste-Link (`Picking/Bom`) sichtbar; auf der
    HauptFA-Zeile derselben Gruppe sind sie unverändert sichtbar.
11. Ein direkter `GET /Picking/Bom/{id}` auf eine Sub-FA-Id (hierarchischer Modus) liefert
    **keine** Stückliste, sondern eine Weiterleitung mit sichtbarer `WarningMessage`.
12. Ein direkter `POST /PickingLeitstand/ToggleRelease` auf eine Sub-FA-Id liefert **keine**
    Zustandsänderung, sondern eine sichtbare `WarningMessage`.
13. `BulkRelease` mit einer gemischten Auswahl (HauptFA-Ids + Sub-FA-Ids) verarbeitet nur die
    HauptFA-Ids und meldet die übersprungenen Sub-FA-Ids sichtbar (`WarningMessage`, analog zum
    bestehenden `SkippedNoArticle`-Hinweis).
14. Im flachen AKE-Modus zeigt AK 10-13 **keine** Wirkung (jede Zeile ist dort ihre eigene
    HauptFA-Zeile per Invariante) — Regressionsnachweis.
15. `FaWorklist/Bom` (read-only) bleibt auf jeder Sub-FA-Zeile unverändert erreichbar.
16. Die `ProductionOrderPickingStatus`-Zeile einer Sub-FA bleibt bestehen und weiterhin über
    Glas/Fremdbezug/Lackierung-Checkboxen bedienbar, unabhängig vom Freigabe-Status.
17. **Nur falls Freigabe-Antwort 2 den Standorteinstellungen-Schalter verlangt:** Bei
    `FaHierarchyKommissionierRelevanzAktiv = false` zeigt die Kommissionierliste alle Positionen
    ungefiltert (kein Relevanzkriterium mehr); bei `true` (Default, unverändert zu vor dieser
    Spec) bleibt das heutige Verhalten bit-identisch erhalten.
18. **Nur falls Freigabe-Antwort 1 Persistenz verlangt:** Ein gespeicherter Checkbox-Default wird
    beim nächsten Öffnen der Stückliste automatisch angewendet, inkl. Badge; Migration läuft
    idempotent auf einer Bestandsdatenbank.

## Test-Szenarien

Neues Kapitel **TS-79** in `docs/TESTSZENARIEN.md` (nächstes freies Kapitel nach TS-78, am
Worktree zu verifizieren), Abschnitte:

- **Relevanzregel:** Kommissionierliste vor/nach der Extraktion liefert identische Ergebnismenge
  (Regressionsvergleich).
- **Fünfter Filter:** Stückliste öffnen (hierarchisch), Checkbox aktivieren → nur Positionen mit
  gesetztem Komm.-Ziel bleiben sichtbar (inkl. deren aufgeklappte Baugruppen-Eltern); Badge
  erscheint; Reset über Badge UND über „Alle Filter zurücksetzen" funktioniert; Druck mit aktiver
  Checkbox zeigt den Hinweis im Kopf und nur die sichtbaren Positionen.
- **Negativfall AKE:** Flache Stückliste öffnen → keine Checkbox, keine Wirkung.
- **Rückbau Freigabe-Kaskade:** `/PickingLeitstand` öffnen → kein „Alle Sub-FAs freigeben"-Button
  in der Gruppen-Kopfzeile mehr; `CascadeReleasePreview`/`CascadeRelease`-Routen liefern 404.
  Fertigmeldungs-Kaskade („Alle Sub-FAs fertigmelden") funktioniert unverändert.
- **Freigabe/Picking nur am HauptFA:** In einer hierarchischen Gruppe mit mehreren Sub-FAs prüfen,
  dass nur die HauptFA-Zeile Freigabe-Button, Prioritäts-Input, Bulk-Checkbox und
  Stückliste-Icon zeigt; Sub-FA-Zeilen zeigen keines davon.
- **Negativfall direkter Zugriff:** `/Picking/Bom/{sub-fa-id}` direkt aufrufen →
  Weiterleitung + Warnhinweis, keine Stückliste. `POST /PickingLeitstand/ToggleRelease` mit
  Sub-FA-Id → keine Änderung + Warnhinweis.
- **BulkRelease gemischt:** HauptFA- und Sub-FA-Ids gemeinsam auswählen und freigeben → nur
  HauptFA wird freigegeben, Sub-FA wird mit Hinweis übersprungen.
- **Negativfall AKE (Freigabe):** Flacher Modus, `BulkRelease`/`ToggleRelease` auf eine normale
  FA-Id → funktioniert unverändert (jede Zeile ist ihre eigene HauptFA-Zeile).
- **Unberührt:** `FaWorklist/Bom` weiterhin auf jeder Sub-FA-Zeile erreichbar; Glas/Fremdbezug/
  Lackierung-Checkboxen einer Sub-FA weiterhin bedienbar, auch wenn die Zeile nie freigegeben wird.
- **Nur falls Standorteinstellungs-Schalter gebaut wird:** Schalter aus → Kommissionierliste zeigt
  alle Positionen; Schalter an (Default) → unverändertes Verhalten.
- **Nur falls Persistenz gebaut wird:** Checkbox-Default im Profil setzen → Stückliste öffnet mit
  vorbelegter Checkbox + Badge.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen (TS-79-Eintrag), TS-73
(v1.38.0) um den Rückbau-Vermerk bei den Kaskade-Szenarien ergänzen.

## Deploy

- **Web-App:** ja — Controller/Views/Services im `IdealAkeWms`-Projekt.
- **Service:** nein — `IDEALAKEWMSService` unberührt (keine Sync-/Worker-Änderung).
- **Migration:** nein im Standardumfang; **ja**, falls Freigabe-Antwort 1 einen pro Benutzer
  gespeicherten Checkbox-Default verlangt (Migration 94, nullable/Default-Spalte an `Users`, nicht
  daten-destruktiv).
- **Kontext:** Der betroffene Code existiert ausschließlich im nicht gemergten Bündel-Worktree
  `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch `feature/2026-08-07-ideal-teile-1-5`).
  Diese Spec wird dort als weitere Etappe umgesetzt — kein zusätzlicher eigener Deploy-Schritt,
  sie geht im selben Publish/Merge des Bündels mit (Vorbedingung: der Dev-Lauf bestätigt diesen
  provisorischen Ausführungsort gegen den dann aktuellen Worktree-/Branch-Stand).
- **Publish-Befehle (nachgelagerter Fall, im Worktree auszuführen):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- Ablauf: Publish **aus dem Worktree** → Testsystem → Test → **danach** Merge (Schranke 2, Mensch).
  Nach dem Merge nur dann erneut aus `main` publishen, wenn der Merge tatsächlich getestete
  Dateien mit parallelen `main`-Änderungen zusammenführt (Buendel-Worktree mit vielen Etappen —
  Merge-Diff prüfen).
- **Brain-Nachzug (Dev-Lauf, HAUPTCHECKOUT):** `secondbrain/specs/freigegeben/2026-09-10-fa-liste-ausbau-matchcode-spec.md`
  um einen Hinweis ergänzen, dass Block 4/Tasks 12-14 durch diese Spec zurückgebaut wurden —
  sonst widerspricht die freigegebene Spec dem tatsächlichen Codezustand.

## Offene Rückfragen

1. → **Ist die Checkbox „Nur kommissionierrelevant" beim Öffnen der Stückliste standardmäßig an
   oder aus — und soll sie, wie die anderen vier Standardfilter, pro Benutzer speicherbar sein?**
   Einschätzung dieser Spec: Standardmäßig **aus** ist der risikoärmere Start (Vollstruktur bleibt
   die Baseline, siehe Ruling 4 — ein zusätzlicher, activ gewählter Filter überrascht niemanden).
   Persistenz analog den vier Textfiltern ist eine kleine Erweiterung (ein Bool-Feld statt eines
   Textfelds), aber zusätzlicher Umfang (Migration, zwei Einstellungsseiten). Falls „an per
   Default" gewünscht ist: aus Sicherheitsgründen (kein Material vergessen) nachvollziehbar, dann
   aber bitte auch zur Persistenz-Frage Stellung nehmen.
2. → **Reicht die reine Extraktion der Relevanzregel in eine geteilte C#-Methode (kein
   Standorteinstellungen-Eintrag), da der Code nur EIN Kriterium zeigt statt der im Backlog
   vermuteten mehreren Parameter — oder soll dennoch der beschriebene Ein/Aus-Schalter
   `FaHierarchyKommissionierRelevanzAktiv` als Rückfallebene gebaut werden?** Einschätzung dieser
   Spec: Ohne einen echten zweiten Konfigurationswert wäre ein Standorteinstellungen-Eintrag eine
   Attrappe (ponytail Sprosse 1 — „Muss das überhaupt existieren?"). Ein Schalter lohnt sich nur,
   wenn die reale Sorge besteht, dass Sage das Feld `Kommissionieren` unzuverlässig befüllt und man
   dann ohne Deploy zurückschalten will.
3. → **Sollen bereits im IDEAL-Testsystem einzeln freigegebene Sub-FAs (`IsReleasedForPicking=true`)
   nach dieser Spec stehen bleiben oder zurückgesetzt werden?** Einschätzung dieser Spec: stehen
   lassen — es sind Testdaten, keine produktive Migration nötig; die betroffenen Zeilen werden
   durch diese Spec ohnehin nicht mehr über die UI erreichbar/änderbar (Freigabe nur noch am
   HauptFA), ihr bisheriger Freigabe-Status ist damit folgenlos.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →

2. →

3. →
