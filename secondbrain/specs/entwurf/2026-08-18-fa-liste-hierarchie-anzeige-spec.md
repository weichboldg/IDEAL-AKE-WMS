---
type: spec
title: "FA-Liste und verwandte Ansichten hierarchiefaehig darstellen (dritte Fehlerklasse nach Teil 7)"
slug: 2026-08-18-fa-liste-hierarchie-anzeige-spec
status: Entwurf
created: 2026-08-18
updated: 2026-08-18
source_backlog: "[[2026-08-18-fa-liste-hierarchie-anzeige]]"
depends_on: "[[2026-07-29-standort-ideal-teil-7-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Controllers/ProductionOrdersController.cs (Gruppierung/Paginierung ueber HauptFA-Gruppen statt Zeilen, Suche SubOrderNumber-zuerst-dann-OrderNumber, nur wirksam bei Master true — heutiger Stand: Einzelzeilen-Pagination, Filter auf `order-number`)"
  - "IdealAkeWms/Views/ProductionOrders/Index.cshtml (Tbody-je-HauptFA-Gruppe analog Views/FaHierarchyKommissionierListen/Index.cshtml, Gruppen-Kopfzeile mit colspan+Chevron, neue Spalte parent-sub-order-number defaultHidden, SageMissingSince-Badge neben der FA-Nummer-Zelle analog zum bestehenden `item.IsCancelled`-Badge Zeile ~126-128 — eigener Text/Tooltip, nicht dieselbe Bedingung)"
  - "IdealAkeWms/Controllers/FaWorklistController.cs + IdealAkeWms/Views/FaWorklist/Index.cshtml (dieselbe Behandlung, verifiziert als ADR-0005-Liste: data-view-key=\"FaWorklist\", ein tbody, data-col-key=\"order-number\")"
  - "IdealAkeWms/Controllers/FaCompletionController.cs + IdealAkeWms/Views/FaCompletion/Index.cshtml (Anzeige-Teil analog; ACHTUNG Namenskollision — die Aktionen dieser View betreffen `FaWorkStep.IsSpecComplete`/`SpecCompletedAt` [\"Arbeitsschritt-Spezifikation vollstaendig definiert\"], NICHT eine FA-Fertigmeldung; siehe Fachliche Anforderungen Abschnitt 7 und Offene Rueckfrage 1)"
  - "IdealAkeWms/Controllers/TrackingController.cs + IdealAkeWms/Views/Tracking/Index.cshtml (dieselbe Behandlung, verifiziert als ADR-0005-Liste; reine Teileverfolgung, keine Erledigt-Aktion — Gruppierung/Suche ja, Kaskade-Abschnitt entfaellt hier)"
  - "IdealAkeWms/Controllers/PickingController.cs (Gruppierung/Paginierung/Suche wie oben; `ToggleDone`/`IProductionOrderPickingStatusRepository.SetIsDonePickingAsync` ist der staerkste Kandidat fuer die im Beschluss gemeinte Fertigmeldung auf Positions-/Kommissionierebene — heute EIN symmetrischer Toggle ohne getrenntes Setzen/Ruecknehmen, siehe Offene Rueckfrage 1/2) + IdealAkeWms/Views/Picking/Index.cshtml"
  - "IdealAkeWms/Controllers/PickingLeitstandController.cs + IdealAkeWms/Views/PickingLeitstand/Index.cshtml (verifiziert: TECHNISCH eine reguläre ADR-0005-Liste mit einem tbody, Server-Spaltenfilter, Pagination — entgegen der pauschalen Backlog-Einschaetzung \"eigenes Layout\"; ihre einzige Massenaktion ist aber `BulkRelease` [Freigabe], nicht Fertigmeldung — Kaskade-Bezug offen, siehe Offene Rueckfrage 3)"
  - "IdealAkeWms/Controllers/Api/BdeStatusApiController.cs (`POST api/bde-status/toggle`, Feld `IsDoneBde`, explizites true/false je `ProductionOrderId` — zweiter starker Kandidat fuer die Fertigmeldung, staerker als IsDonePicking weil kein reiner Toggle sondern expliziter Wert; Kaskade-Logik muesste HIER ansetzen, falls Offene Rueckfrage 1 dahin entschieden wird)"
  - "IdealAkeWms/Views/BdeCockpit/Index.cshtml + wwwroot/js/bde-cockpit.js (verifiziert: KEIN Tabellen-Layout — JS-gerendertes Karten-Grid `#cockpitGrid`, kein `<table>`/`<tbody>`; das Zwei-Ebenen-tbody-Muster ist hier strukturell nicht direkt anwendbar, siehe Offene Rueckfrage 4 — eigener Entwurf noetig, ggf. NICHT Teil des ersten Umsetzungsschritts)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (additiv: neue ColumnDef `parent-sub-order-number`, DefaultHidden: true, fuer ProductionOrders/FaWorklist/FaCompletion/Tracking/Picking/PickingLeitstand — sechsmal derselbe Key, kein neuer viewKey; `order-number` bleibt bestehen und wird NICHT umbenannt, siehe Fachliche Anforderungen Abschnitt 5)"
  - "IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs + IProductionOrderRepository.cs (neue gruppierte, seitenweise Abfrage — Gruppe=HauptFA/OrderNumber, TotalCount=Anzahl Gruppen, nur wirksam bei Master true; exakte Methodensignatur ist Dev-Lauf-Entscheidung)"
  - "IdealAkeWms/wwwroot/js/fa-liste-gruppierung.js (NEU, Vorschlag — Dev-Lauf darf abweichen; gemeinsam fuer die 5 tabellarischen Listen [ProductionOrders/FaWorklist/FaCompletion/Tracking/Picking] + PickingLeitstand falls Offene Rueckfrage 3 dafuer entschieden wird: Chevron-Collapse je Gruppen-tbody analog `oseon-toggle`/`oseon-chevron` aus Views/Tracking/OseonIndex.cshtml + Views/Tracking/_OseonGroupDetails.cshtml, PLUS Auto-Expand-bei-Suchtreffer-in-zugeklappter-Gruppe — reiner Zusatzbaustein, KEINE Aenderung an wwwroot/js/table-filter.js selbst, siehe Technischer Loesungsentwurf)"
  - "docs/TESTSZENARIEN.md"
  - "secondbrain/tests/testszenarien-index.md"
  - "GELESEN, NICHT GEAENDERT (Referenz): IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml (tbody-je-HauptFA-Gruppe-Pattern, colspan-Kopfzeile), IdealAkeWms/Views/Tracking/OseonIndex.cshtml + _OseonGroupDetails.cshtml (Chevron-Collapse-Pattern), IdealAkeWms/Controllers/StandortEinstellungenController.cs (Muster fuer Master-Lesezugriff: `_serviceSettings.GetValueAsync(StandortSettingsCatalog.MasterKey)` + `IsTrue(...)`), IdealAkeWms/Services/HierarchischeStruktur/HierarchischeStrukturKeys.cs (`Master = \"ProduktionsauftragHierarchisch\"`), IdealAkeWms/Models/ProductionOrder.cs (SubOrderNumber/ParentSubOrderNumber/SageMissingSince/IsCancelled/IsDone — alle bereits vorhanden aus Teil 7, keine Migration noetig)"
open_questions:
  - "F4/Kaskade-Traeger: Es gibt keinen einheitlichen 'Fertigmeldung'-Button im Code. Kandidaten sind der symmetrische Toggle `PickingController.ToggleDone`/`IsDonePicking` und der explizite `BdeStatusApiController`-Toggle `IsDoneBde`. `FaCompletion` betrifft trotz seines Namens eine andere Sache (`IsSpecComplete`). Auf welche Aktion(en) bezieht sich die im Beschluss verlangte Kaskade?"
  - "Rücknahme: Da die gefundenen Erledigt-Mechanismen heute EIN symmetrischer Toggle sind (kein eigenes Setzen/Ruecknehmen), existiert die im Beschluss vorausgesetzte 'Ruecknahme-Funktion' im engeren Sinn nicht. Wird der Toggle in zwei Pfade aufgespalten (Setzen mit Kaskade / Ruecknehmen ohne Kaskade), oder entfaellt der ganze Ruecknahme-Abschnitt ersatzlos, wie im Backlog fuer den Fall 'gibt es nicht' vorgesehen?"
  - "PickingLeitstand ist entgegen der Backlog-Einschaetzung technisch eine reguläre ADR-0005-Liste (verifiziert: ein `<tbody>`, `data-server-column-filter=\"true\"`, Pagination). Bekommt sie dieselbe Zwei-Ebenen-Behandlung wie die fuenf anderen Listen? Und falls ja: bezieht sich die Kaskade-Regel dort auf `BulkRelease` (Freigabe), oder hat Leitstand ueberhaupt keine Kaskade-Aktion?"
  - "BDE-Cockpit ist tatsaechlich kein Tabellen-Layout, sondern ein JS-Karten-Grid ohne `<table>`/`<tbody>` (verifiziert). Das Zwei-Ebenen-tbody-Muster ist dort strukturell nicht direkt uebertragbar. Ist ein eigener Kartengruppierungs-Entwurf Teil DIESER Spec, oder wird das Cockpit fuer die erste Umsetzungsrunde bewusst ausgeklammert (nur die 5 Tabellen-Listen + ggf. Leitstand)?"
  - "SageMissingSince/Teil-7-AK-6: Der Sync-Mechanismus (Regel 2) ist laut Teil-7-Spec (AK 6/AK 12, Etappe C, Commits dc297d9..bc7e3d6) unit-getestet und funktionsfaehig — an der Erzeugung des Werts ist nichts nachzubessern. Ob im produktiven IDEAL-Bestand aktuell tatsaechlich ein Datensatz mit `SageMissingSince <> NULL` existiert, laesst sich aus Brain/Code nicht feststellen. Blockiert das den Dev-Lauf, oder wird die Anzeige unabhaengig vom aktuellen Datenstand gebaut (Empfehlung: Letzteres, siehe Fachliche Anforderungen)?"
  - "Bestaetigungsdialog-Zahl 'davon offene Arbeitsgaenge/Rueckmeldungen': Welche konkrete Datenquelle zaehlt das — offene `FaWorkStep`, `ProductionOrderBdeStatus`/laufende BDE-Buchungen, ungebuchte `StockMovement`, oder eine Kombination? Ohne Festlegung kann der Dev-Lauf die Zahl nicht berechnen."
  - "Kaskaden-Audit (Anforderung D, 'als EINE Kaskade erkennbar'): Es existiert aktuell keine generische User-Aktions-Audit-Tabelle (nur `AuditableEntity`-Felder je Zeile und `ISyncLogger` fuer Hintergrund-Services, ADR 0010). Wie wird die Kaskaden-Zusammengehoerigkeit technisch sichtbar — z. B. eine gemeinsame Korrelations-ID je betroffene Zeile in einer neuen, kleinen Tabelle (dann eigene Migration), Wiederverwendung eines bestehenden Mechanismus, oder reicht ein gemeinsamer Zeitstempel plus TempData-Meldung mit Anzahl? Migrationsentscheidung wird hier bewusst NICHT vorweggenommen."
  - "Column-Mapping 'nicht mehr in Sage': Verifiziert im Code unterscheiden sich AKE (`IsCancelled`-Badge 'In Sage geloescht', Auftrag wird automatisch storniert und verschwindet aus offenen Sichten) und IDEAL (`SageMissingSince`, Auftrag bleibt sichtbar/rueckmeldefaehig, nur zeitgestempelt markiert, Sync-Regel 2) fachlich. Empfehlung dieser Spec: kein Feld-Mapping, sondern nur dieselbe Render-Position (Badge neben der FA-/Sub-FA-Nummer-Zelle) mit eigenem Text/Tooltip fuer den hierarchischen Fall — bestaetigen?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Nach der Materialisierung (Teil 7, [[2026-07-29-standort-ideal-teil-7-spec]]) stehen die IDEAL-Sub-FAs
als echte `ProductionOrders` im System — 130 Stück beim ersten Lauf. Damit erscheinen sie in FA-Liste,
Leitstand, Arbeitsvorrat, Fertigmeldung, Tracking und Picking, dazu im BDE-Cockpit (Teil 8,
[[2026-07-29-standort-ideal-teil-8-spec]]). **Technisch funktioniert das; brauchbar ist es nicht:**
dieselbe `OrderNumber` (HauptFA) erscheint mehrfach — einmal je Sub-FA — ohne dass die Zeilen
unterscheidbar wären. `SubOrderNumber` steht nirgends, der Elternzeiger auch nicht, und eine Suche nach
der FA-Nummer liefert die ganze Gruppe ohne Auswahl.

Das ist eine **dritte Fehlerklasse** (Backlog-Tabelle): (1) mehrdeutige `OrderNumber`-Lookups —
behoben in Teil 7 Etappe D; (2) hart verdrahtete AKE-Views — Guard vorhanden, Volllösung offen; (3)
**Anzeige kennt die Hierarchie nicht** — Gegenstand dieser Spec. Teil 7 hat die betroffenen Controller
bereits angefasst, aber ausdrücklich **nur für die Lookup-Härtung**; Anzeige/Listen waren dort explizit
out of scope, weil es vor der Materialisierung nichts anzuzeigen gab.

Diese Spec macht die Sub-FA-Hierarchie in den sechs betroffenen Ansichten **sichtbar und bedienbar**,
ohne die Arbeitslisten-Charakter dieser Ansichten in einen Struktur-Browser zu verwandeln (der bleibt
unter `/FaHierarchy`).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:**

1. Master-gesteuerte Zwei-Ebenen-Gruppierung (Gruppen-Kopfzeile = HauptFA, darunter alle Nachfahren-
   Sub-FAs als flache Zeilen) für die fünf verifizierten ADR-0005-Standardlisten
   `ProductionOrders`, `FaWorklist`, `FaCompletion`, `Tracking`, `Picking`.
2. Dieselbe Behandlung für `PickingLeitstand`, vorbehaltlich Offener Rückfrage 3 (technisch ebenfalls
   eine ADR-0005-Liste, aber mit abweichender Massenaktion).
3. Spalten-Mapping: `order-number` zeigt im hierarchischen Modus `SubOrderNumber` statt `OrderNumber`
   (Gruppen-Kopfzeile trägt weiterhin `OrderNumber`); neue Spalte `parent-sub-order-number`
   (`defaultHidden`); Anzeige von `SageMissingSince` als eigenständiger Badge, kein Feld-Mapping auf
   `IsCancelled`.
4. Suche: erst gegen `SubOrderNumber`, dann gegen `OrderNumber`, mit Auto-Expand + Hervorhebung bei
   Treffer in zugeklappter Gruppe.
5. Paginierung über Gruppen, nicht über Zeilen.
6. Aktionen: Ebene und Kaskade für die identifizierte(n) Fertigmeldungs-Aktion(en) — Umfang hängt an
   Offener Rückfrage 1; die Anforderungen (A–D) und die Rücknahme-Asymmetrie sind in dieser Spec
   bereits vollständig spezifiziert, damit der Dev-Lauf sie anwenden kann, sobald Rückfrage 1/2
   beantwortet sind.
7. AKE-Regression: Bei Master `false` bleibt jede der sechs Ansichten bit-identisch zu heute.

**Out-of-Scope:**

- `/FaHierarchy` (Baumanzeige, Teil 2 + [[2026-08-12-fa-struktur-darstellung-spec]]) — bleibt der
  Struktur-Browser mit voller Tiefe, unverändert.
- Die vier flachen IDEAL-Listen aus [[2026-08-12-listen-spaltenauswahl-spec]]
  (Kommissionierlisten/Beschichtung/Vormontage) — die haben bereits eigene, bereits gruppierte
  `tbody`-je-HauptFA-Darstellung aus Teil 3–5 und sind von dieser Spec nicht betroffen.
- BDE-Cockpit-Kartengruppierung als **fertiger** Entwurf — abhängig von Offener Rückfrage 4;
  mindestens die Feststellung „kein Tabellen-Layout, eigener Baustein nötig" ist in dieser Spec
  bereits verankert.
- Migration/Schema — alle benötigten Felder (`SubOrderNumber`, `ParentSubOrderNumber`,
  `SageMissingSince`) existieren bereits aus Teil 7.
- Neue Rollen/Zugriffsfilter — die sechs Ansichten behalten ihre bestehenden `RequireXxxAccess`-Filter
  unverändert (siehe Betroffene Rollen / Zugriffsfilter).
- Die eigentliche BDE-Scan-Auflösung/Sub-FA-Auswahl am Terminal (Teil 8) — unverändert.

## Fachliche Anforderungen

### 1. Geltungsbereich: Master-Schalter `ProduktionsauftragHierarchisch`

Alle Punkte dieser Spec (Gruppierung, neue Spalte, Such-Semantik, Kaskade) sind **ausschließlich**
wirksam, wenn der Master `ProduktionsauftragHierarchisch` (Key-Konstante
`HierarchischeStrukturKeys.Master`, ADR 0008/ServiceSettings, geschützt durch
`HierarchischeStrukturGuard`) `true` ist. Lesezugriff im Controller/View folgt demselben bereits
etablierten Muster wie in `StandortEinstellungenController` (`_serviceSettings.GetValueAsync(...)` +
Boolean-Parsing) — kein neuer Lese-Mechanismus, nur eine weitere Verwendungsstelle desselben,
bestehenden ServiceSettings-Zugriffs. Bei Master `false` gilt `OrderNumber == SubOrderNumber` (Teil-7-
Backfill) — hier wäre jede Gruppierung sinnlos (siehe F1/AK 1).

### 2. Zwei Anzeige-Ebenen — Darstellung, nicht Umfang [ENTSCHIEDEN, aus Backlog übernommen]

- Gruppen-Kopfzeile = `HauptFA` (`OrderNumber`), darunter die Sub-FAs als **flache** Zeilen. Keine
  rekursive Baumdarstellung, keine Einrückung nach Tiefe — das bleibt `/FaHierarchy` vorbehalten.
- **Kritische Präzisierung:** Es erscheinen **ALLE** Nachfahren-Sub-FAs als Zeilen, unabhängig von
  ihrer Tiefe im Unterbaum — nicht nur direkte Kinder. Andernfalls verschwänden Enkel und tiefere
  Ebenen aus der Arbeitsliste, obwohl sie existieren und rückmeldefähig sind (stiller Datenverlust in
  der Oberfläche). Der Elternbezug bleibt über die neue Spalte `parent-sub-order-number`
  (`defaultHidden`) erhalten sowie vollständig unter `/FaHierarchy`.
- Wiederverwendetes Muster: `<tbody>` je Gruppe + Kopfzeile mit `colspan`, identisch zum bereits
  dreimal gebauten Muster in `Views/FaHierarchyKommissionierListen/Index.cshtml` (und den beiden
  Schwester-Views aus Teil 4/5) — kein neuer Mechanismus.

### 3. Standardsicht: Gruppen aufgeklappt

- Beim Laden sind alle Gruppen aufgeklappt (Arbeitsliste zeigt die Aufträge, nicht erst nach Klick).
- Zuklappen bleibt je Gruppe möglich — Chevron-Toggle je Gruppen-Kopfzeile, analog `oseon-toggle`/
  `oseon-chevron` aus `Views/Tracking/OseonIndex.cshtml`/`_OseonGroupDetails.cshtml`
  (Referenzimplementierung, bereits im selben Repo vorhanden, siehe auch
  [[2026-08-12-fa-struktur-darstellung-spec]] Abschnitt 5). Die bestehenden
  `Views/FaHierarchyKommissionierListen/*`-Gruppen-`tbody`s haben **kein** Collapse — dieser Teil des
  Musters ist neu zu übernehmen, nicht 1:1 vorhanden (Verifiziert im Code, siehe `affected_code`).

### 4. Suche — inkl. Auto-Expand bei zugeklappter Gruppe [F2, eigenes Akzeptanzkriterium]

- Erst gegen `SubOrderNumber` prüfen → Treffer selektiert genau diese Zeile.
- Sonst gegen `OrderNumber` prüfen → Treffer selektiert die Gruppe.
- **Entscheidend:** Trifft die Suche einen Sub-FA in einer zugeklappten Gruppe, wird die Gruppe
  **automatisch aufgeklappt** und der Treffer hervorgehoben. Ohne diese Regel bekäme der Anwender ein
  scheinbar leeres Ergebnis und hielte den Auftrag für nicht vorhanden — ein **stiller** Fehler, kein
  offensichtlicher. Dies ist der gefährlichste Fallstrick dieser Spec (F2) und bekommt ein eigenes
  Akzeptanzkriterium **und** ein eigenes Testszenario, nicht nur eine Nebenbedingung.
- Diese Erweiterung passiert im neuen, zusätzlichen JS-Baustein (`fa-liste-gruppierung.js`,
  Vorschlag), NICHT durch eine Änderung an `wwwroot/js/table-filter.js` selbst — die bestehende
  Server-Spaltenfilter-/Freitextsuche-Logik der sechs Listen bleibt unverändert, der neue Baustein
  reagiert nur zusätzlich auf das Suchergebnis (welche Zeilen sichtbar/hervorgehoben sind) und klappt
  bei Bedarf die Elterngruppe auf.

### 5. Spalten-Mapping statt Spalten-Wildwuchs — Feld für Feld [PFLICHT, wörtlich aus Backlog übernommen]

Leitregel: **Eine Spalte behält ihre BEDEUTUNG, auch wenn sich die Quelle ändert.**

| Spalte | flach (AKE) | hierarchisch (IDEAL) | Zulässig? | Begründung |
|---|---|---|---|---|
| FA-Nummer (Zeile, `data-col-key="order-number"`) | `OrderNumber` | `SubOrderNumber` | **Ja** | Beides ist „die Nummer dieses Auftrags"; in AKE sind beide Werte ohnehin identisch (Backfill aus Teil 7), es ändert sich nur die Quelle, nicht die Bedeutung der Spalte. |
| Gruppen-Kopfzeile | entfällt (keine Gruppierung im Flachmodus) | `OrderNumber` (= HauptFA) | **Ja** | Neue Ebene, keine Umdeutung einer bestehenden Spalte. |
| Bezeichnung/Matchcode | wie bisher | wie bisher | Ja | Unverändert, betrifft nicht die Hierarchie. |
| Elternzeiger | — (keine Spalte) | `ParentSubOrderNumber` | **Eigene, neue Spalte** `parent-sub-order-number`, `defaultHidden: true` | Es gibt im Flachmodus keine entsprechende Bedeutung, auf die gemappt werden könnte — echte neue Information. |
| „nicht mehr in Sage" | bestehender Mechanismus (`IsCancelled`-Badge „In Sage gelöscht", storniert automatisch, Auftrag verschwindet aus offenen Sichten) | `SageMissingSince` (Zeitstempel, Auftrag bleibt sichtbar/rückmeldefähig, Sync-Regel 2 aus Teil 7) | **Erst prüfen — siehe unten und Offene Rückfrage 5/7** | Im Code verifiziert: die beiden Mechanismen sind fachlich **unterschiedliche Zustände** (harte Stornierung vs. weicher, selbstheilender Zeitstempel). Diese Spec empfiehlt daher **kein** Feld-Mapping auf dieselbe Spalte/Bedingung, sondern nur dieselbe Render-**Position** (Badge neben der FA-/Sub-FA-Nummer-Zelle, analog zur bestehenden `IsCancelled`-Badge-Stelle in `Views/ProductionOrders/Index.cshtml`) mit eigenem Text/Tooltip für den hierarchischen Fall. |

**Wo NICHT gemappt werden darf:** Bedeutete eine Spalte im hierarchischen Modus fachlich etwas
anderes als im flachen, bekommt sie eine eigene Spalte — eine Spalte, die je nach Schalter etwas
anderes meint, ist eine Falle, spätestens beim Export/Ausdruck/Weitergeben einer Nummer. Genau dieser
Fall liegt bei „nicht mehr in Sage" vor (siehe Tabelle) — deshalb dort **kein** Mapping auf dasselbe
Feld, sondern zwei getrennte, sich gegenseitig ausschließende Mechanismen (gegenseitig ausschließend,
weil `IsCancelled` nur im Flachmodus und `SageMissingSince` nur bei Master `true` gesetzt wird, siehe
Teil-7-Spec).

### 6. Paginierung über Gruppen, nicht über Zeilen [F3]

Wie in allen anderen Listen mit Gruppierung: `TotalCount` zählt **Gruppen** (= distinkte
`OrderNumber`-Werte auf der aktuellen Filterauswahl), eine Gruppe wird nie über Seiten getrennt.
Sonst entstehen Kopfzeilen ohne Zeilen bzw. abgeschnittene Strukturen. Bestehende
`PageSize.Resolve`/`PaginationState`/`_Pagination`-Bausteine bleiben strukturell dieselben — nur die
zugrunde liegende Repository-Abfrage muss auf Gruppen statt Zeilen zählen und seitenweise blättern.

### 7. Aktionen: Ebene und Kaskade [ENTSCHIEDEN 2026-08-18, Träger-Aktion noch zu klären — Offene Rückfrage 1]

**Grundregel aus dem Beschluss:**
- Fertigmeldung am Haupt-FA → alle zugehörigen Sub-FAs werden ebenfalls fertig.
- Fertigmeldung an einem Sub-FA → wirkt nur auf diesen einen.

**Befund aus dem Code (wichtig für die Umsetzung, siehe Offene Rückfrage 1):** Es gibt **keine**
einzelne, eindeutig benannte „Fertigmeldung"-Aktion im Repository. Kandidaten:

- `PickingController.ToggleDone` / `IProductionOrderPickingStatusRepository.SetIsDonePickingAsync`
  (`ProductionOrderPickingStatus.IsDonePicking`, „Kommissionierung erledigt") — heute ein
  **symmetrischer Toggle** (flippt den aktuellen Wert, kein separates Setzen/Zurücknehmen), pro
  `ProductionOrder` (= pro Sub-FA nach Teil 7) skaliert.
- `BdeStatusApiController.Toggle` (`POST api/bde-status/toggle`, Feld `IsDoneBde`,
  `IProductionOrderBdeStatusRepository.SetIsDoneBdeAsync`) — ruft mit **explizitem** `bool Value`
  auf (kein reiner Flip), ebenfalls pro `ProductionOrder`. Dieser Endpunkt ist der technisch
  passendere Träger für eine Kaskade, weil „Wert explizit setzen" sich natürlicher in „setze alle
  Nachfahren ebenfalls auf `true`" übersetzt als ein Toggle.
- `FaCompletionController` (trotz seines Namens): betrifft `FaWorkStep.IsSpecComplete`
  („Arbeitsschritt-Spezifikation vollständig definiert") — eine **andere fachliche Sache** als die
  im Beschluss gemeinte FA-Fertigmeldung. Diese View trägt daher primär den Anzeige-Teil dieser Spec
  (Gruppierung/Suche/Spalten), nicht zwingend die Kaskade-Aktion.

Diese Spec legt die **Anforderungen A–D** an die Kaskade unabhängig vom exakten Träger fest — sie
gelten für die Aktion(en), die Offene Rückfrage 1 als „die Fertigmeldung" bestimmt:

- **A — „Alle Sub-FAs" heißt ALLE NACHFAHREN, nicht nur direkte Kinder.** Die Struktur ist mehrstufig
  (Teil 7, Befund B1). Kaskadierte man nur eine Ebene tief, bliebe bei einer dreistufigen Struktur
  die unterste Ebene offen — Haupt-FA fertig, Enkel-Sub-FA nicht. Die Kaskade läuft über den
  gesamten Unterbaum (dieselbe „alle Nachfahren"-Regel wie in Abschnitt 2 für die Anzeige).
- **B — Massenaktion braucht eine Bestätigung mit Zahl.** Bestätigungsdialog nennt die Anzahl
  betroffener Sub-FAs **und gesondert**, wie viele davon noch offene Arbeitsgänge/Rückmeldungen
  haben (Datenquelle dafür: Offene Rückfrage 6). Nicht blockierend, aber sichtbar — wer einen
  Auftrag schließt, an dem noch gebucht wird, soll das vorher wissen.
- **C — Atomar.** Entweder werden alle Sub-FAs des Unterbaums fertig gemeldet oder keiner — eine
  Transaktion. Ein Abbruch mitten in der Kaskade hinterließe einen nicht erklärbaren Zustand.
- **D — Im Audit als EINE Kaskade erkennbar, nicht als N Einzelaktionen.** Wie das technisch
  umgesetzt wird (Korrelations-ID, neue kleine Tabelle, o. ä.), ist offen — siehe Offene
  Rückfrage 7; hier wird bewusst keine Migration vorweggenommen.

**Rücknahme — kaskadiert NICHT, falls sie überhaupt existiert [ENTSCHEIDUNG 2026-08-18, bedingt]:**

Der Beschluss sieht eine bewusst **asymmetrische** Kaskade vor: Setzen kaskadiert eindeutig
(Haupt fertig ⇒ alle fertig), Zurücknehmen würde das nicht — öffnete man am Haupt-FA alle Sub-FAs,
gingen eigenständige, bereits vorher gesetzte Fertigmeldungen einzelner Sub-FAs verloren. Deshalb:
Rücknahme wirkt **nur** auf den Auftrag, an dem sie ausgelöst wird; der Rücknahme-Dialog am Haupt-FA
**benennt** diese Asymmetrie (Hinweistext: Sub-FAs bleiben fertig).

**Aber:** Die identifizierten Kandidaten-Mechanismen (`IsDonePicking`, `IsDoneBde`) sind heute **ein
einziger, symmetrischer Toggle** — es gibt aktuell keine getrennte „Setzen"- und „Zurücknehmen"-
Aktion, an der man die Asymmetrie überhaupt festmachen könnte. Ob dieser Abschnitt in der Umsetzung
überhaupt zur Anwendung kommt, hängt an Offener Rückfrage 2. **Falls es keine (echte) Rücknahme gibt,
entfällt dieser Abschnitt ersatzlos** — er wird nicht künstlich erfunden (Backlog-Vorgabe).

### 8. Per-View-Behandlung — nicht pauschal, sondern je Ansicht entschieden [F5]

| Ansicht | ADR-0005-Liste? | Gruppierung anwendbar? | Bestehende Aktion(en) | Kaskade-Bezug |
|---|---|---|---|---|
| `ProductionOrders` (FA-Liste) | Ja | Ja — volle Behandlung (Abschnitt 2–6) | Nur Anzeige (`IsDone`/`IsDonePicking` als kombiniertes Badge), keine eigene Erledigt-Aktion in dieser View | Keine eigene Kaskade-Aktion hier, nur Anzeige des Ergebnisses |
| `FaWorklist` | Ja (verifiziert: `data-view-key="FaWorklist"`, ein `<tbody>`) | Ja — volle Behandlung | Zu prüfen im Dev-Lauf (kein Erledigt-Toggle bekannt) | Voraussichtlich keine |
| `FaCompletion` | Ja (verifiziert), aber **Namenskollision** (siehe Abschnitt 7) | Ja — Anzeige-Teil | `IsSpecComplete`-Toggle, andere fachliche Bedeutung | Kaskade-Regel gilt hier NICHT ohne Weiteres — eigene Prüfung nötig, nicht automatisch übernehmen |
| `Tracking` | Ja (verifiziert) | Ja — volle Behandlung | Keine Erledigt-Aktion (reine Teileverfolgung) | Entfällt |
| `Picking` | Ja (verifiziert) | Ja — volle Behandlung | `ToggleDone`/`IsDonePicking` (symmetrischer Toggle) | Kaskade-Kandidat 1 (Offene Rückfrage 1) |
| `PickingLeitstand` | Ja (verifiziert, entgegen Backlog-Einschätzung) | Technisch ja | `BulkRelease` (Freigabe) — **nicht** Fertigmeldung | Kaskade-Konzept passt nicht 1:1 — Offene Rückfrage 3 |
| BDE-Cockpit (`BdeCockpit`) | **Nein** — verifiziert: JS-Karten-Grid, kein `<table>`/`<tbody>` | Nein, ohne eigenen Entwurf | `IsDoneBde`-Toggle (`BdeStatusApiController`) | Kaskade-Kandidat 2, stärkster Kandidat für „die" Fertigmeldung — Offene Rückfrage 4 |

Konsequenz für den Dev-Lauf: Die **fünf** klaren Standardlisten (`ProductionOrders`, `FaWorklist`,
`FaCompletion`, `Tracking`, `Picking`) bekommen die volle Zwei-Ebenen-Behandlung aus Abschnitt 2–6
ohne weitere Rückfrage. `PickingLeitstand` und BDE-Cockpit brauchen vor der Umsetzung eine
Entscheidung (Offene Rückfragen 3/4).

### 9. AKE-Regression [F6, harte Bedingung wie in jedem Teil]

Bei Master `false` verhält sich jede der sechs Ansichten **bit-identisch** zu heute: keine
Gruppierung (weiterhin ein `<tbody>`, keine Kopfzeilen), keine neuen sichtbar geschalteten Spalten
(`parent-sub-order-number` bleibt `defaultHidden` und ist ohnehin für alle Zeilen leer, da
`ParentSubOrderNumber` im Flachmodus nie gesetzt wird), unveränderte Suche (Treffer weiterhin über
`OrderNumber`, das im Flachmodus `== SubOrderNumber` ist), unveränderte Paginierung (Zeilen, nicht
Gruppen).

## Technischer Lösungsentwurf

- **Serverseitig:** Jeder der sechs Controller liest den Master (Muster: injizierter ServiceSettings-
  Zugriff + `GetValueAsync`/`IsTrue`, identisch zu `StandortEinstellungenController`). Bei `true`
  liefert das Repository eine **gruppierte** Seite (Gruppe = `OrderNumber`, `TotalCount` = Anzahl
  Gruppen, Sortierung/Filter bleiben pro Gruppe erhalten); bei `false` unverändert die heutige
  Einzelzeilen-Abfrage. Exaktes Methoden-/Parametersignatur-Design ist Dev-Lauf-Sache, folgt aber dem
  bereits etablierten `GetAllByFaAndOperationAsync`-Vorbild aus Teil 7 (mengenwertige Variante neben
  dem bestehenden Einzel-Pfad, kein Bruch bestehender Aufrufer).
- **View:** Bei Master `true` rendert jede View eine `foreach`-Schleife über Gruppen, pro Gruppe ein
  `<tbody>` mit Kopfzeile (`colspan` über alle Spalten, `OrderNumber`, Chevron-Toggle, Positionszahl)
  gefolgt von den Sub-FA-Zeilen — Struktur identisch zum bestehenden Muster in
  `Views/FaHierarchyKommissionierListen/Index.cshtml`. Bei `false` bleibt die heutige einzelne
  `<tbody>` mit Einzelzeilen unverändert (kein zweiter Codepfad im Sinn von Duplikation, sondern eine
  Bedingung um den bestehenden Rendering-Block).
- **Neuer, gemeinsamer JS-Baustein (`wwwroot/js/fa-liste-gruppierung.js`, Namensvorschlag):**
  Chevron-Collapse je Gruppen-`tbody` (übernimmt das Verhaltensmuster aus
  `Views/Tracking/OseonIndex.cshtml`/`_OseonGroupDetails.cshtml`, dort bereits gebaut und bewährt)
  plus die Auto-Expand-bei-Suchtreffer-Logik aus Abschnitt 4. **Kein** Eingriff in
  `wwwroot/js/table-filter.js` — dieser Baustein ergänzt nur, beobachtet das vorhandene
  Such-/Filterergebnis und klappt bei Bedarf zu, auf oder hervor. Eingebunden **nach**
  `table-filter.js`, nur wenn Master `true` (z. B. über ein Daten-Attribut am Root-Element, das der
  Server rendert).
- **`ColumnDefinitions.cs`:** additiv je betroffener `ViewConfig` (`ProductionOrders`, `FaWorklist`,
  `FaCompletion`, `Tracking`, `Picking`, ggf. `PickingLeitstand`) eine neue `ColumnDef
  ("parent-sub-order-number", "Übergeordnete Sub-FA", Locked: false, DefaultHidden: true)`. Kein neuer
  `viewKey`, keine Migration (`UserViewPreference` ist bereits `viewKey`-agnostisch, siehe Teil-7- und
  Schwester-Spec-Präzedenz).
- **SageMissingSince-Badge:** eigenständiges Markup neben der FA-/Sub-FA-Nummer-Zelle, analog zur
  Position der bestehenden `IsCancelled`-Badge in `Views/ProductionOrders/Index.cshtml`
  (`bg-danger`-Badge „In Sage gelöscht"), aber **eigene Bedingung** (`SageMissingSince != null`) und
  **eigener Text/Tooltip** (Vorschlag: „Seit {SageMissingSince:dd.MM.yyyy} nicht mehr in der Struktur
  gemeldet — Rückmeldung weiterhin möglich"). Kein Eingriff in die `IsCancelled`-Logik.
- **Kaskade-Endpunkt(e):** abhängig von Offener Rückfrage 1; sobald geklärt, erweitert der
  entsprechende Controller/Service um: (a) Ermittlung aller Nachfahren-`ProductionOrder`s über
  `ParentSubOrderNumber` (rekursiv oder iterativ, analog der bereits vorhandenen Baum-Logik aus
  `FaHierarchyTreeBuilder`/Teil 1–2, aber auf `ProductionOrders` statt `FaHierarchyNode`), (b)
  Bestätigungsdialog mit Zählung (Offene Rückfrage 6), (c) eine Transaktion über alle betroffenen
  Zeilen, (d) eine Korrelationskennung fürs Audit (Offene Rückfrage 7).

## Migrations-/SQL-Auswirkungen

Keine. Alle benötigten Spalten (`SubOrderNumber`, `ParentSubOrderNumber`, `SageMissingSince`,
`IsCancelled`, `IsDone`, `IsDonePicking`, `IsDoneBde`) existieren bereits aus Teil 7 bzw. vorherigen
Runden. Die additive `ColumnDefinitions.cs`-Erweiterung braucht keine Migration
(`UserViewPreference` ist `viewKey`-agnostisch, bereits mehrfach ohne Migration erweitert, siehe
[[2026-08-12-listen-spaltenauswahl-spec]]). **Ausnahme, falls die Kaskade eine neue
Audit-Korrelationstabelle braucht (Offene Rückfrage 7):** dann eine eigene, kleine Migration — hier
bewusst nicht vorweggenommen, da die Notwendigkeit von der Antwort auf Rückfrage 7 abhängt.

## Audit-Feld-Auswirkungen

Reine Anzeige-Änderungen (Gruppierung, Spalte, Suche, Badge) schreiben nichts und ändern keine
Audit-Felder. Die Kaskade-Aktion selbst (sobald ihr Träger geklärt ist) schreibt je betroffenem
`ProductionOrder`/`ProductionOrderPickingStatus`/`ProductionOrderBdeStatus` die bereits etablierten
Audit-Felder über die bestehenden Repository-Methoden (`SetIsDonePickingAsync`/`SetIsDoneBdeAsync`
nehmen bereits `modifiedBy`/`modifiedByWindows` entgegen, aus `ICurrentUserService`) — pro Zeile
unverändert, nur die Anzahl der in einem Rutsch geschriebenen Zeilen wächst durch die Kaskade. Die
zusätzliche Kaskaden-Kennzeichnung fürs Audit (Anforderung D) ist eine **neue**, noch zu entscheidende
Ergänzung (Offene Rückfrage 7), kein Ersatz der bestehenden Audit-Felder.

## Betroffene Rollen / Zugriffsfilter

Keine Änderung an bestehenden Zugriffsfiltern. Verifiziert im Code, unverändert je View:

| Controller | Class-Level-Filter |
|---|---|
| `ProductionOrdersController` | `[RequirePickingOrTrackingOrLeitstandAccess]` |
| `FaWorklistController` | `[RequireVorbauAccess]` |
| `FaCompletionController` | `[RequireFaCompletionAccess]` |
| `TrackingController` | `[RequireTrackingAccess]` |
| `PickingController` | `[RequirePickingAccess]` (auch je Action) |
| `PickingLeitstandController` | `[RequirePickingOrLeitstandAccess]` (Class), `[RequireLeitstandAccess]` (Freigabe-Actions) |
| `BdeCockpitController` | `[RequireBdeShiftleadAccess]` |
| `BdeStatusApiController` | `[RequirePickingAccess]` |

Kein neues Feature-Toggle über `AppSettings`/`/Settings` (ADR 0011) — der Master
`ProduktionsauftragHierarchisch` ist bereits ein bestehender, geschützter ServiceSettings-Key (ADR
0008) aus Teil 7, keine neue Rolle, kein neuer Filter. Sollte die Kaskade-Aktion (Offene Rückfrage 1)
auf einen bislang nicht kaskadefähigen Endpunkt gemappt werden, gilt für diesen weiterhin derselbe
Zugriffsfilter wie für die Einzelaktion — keine neue Berechtigungsstufe für „Kaskade" vorgesehen,
solange nicht explizit anders entschieden.

## Listen-View-Pattern-Pflichten (ADR 0005)

Alle sechs Ansichten sind (mit der Einschränkung aus Abschnitt 8 für BDE-Cockpit) bereits heute
vollwertige ADR-0005-Listen (Pagination, Filterkarte, Server-Spaltenfilter, `column-preferences.js`-
Anschluss). Diese Spec ändert daran **nichts** Grundsätzliches — sie ergänzt: (1) eine bedingte
Gruppierungsebene (nur bei Master `true`), (2) eine neue, `defaultHidden`-Spalte, (3) eine
zusätzliche, additive Suchsemantik. Die Paginierung wechselt bei Master `true` von zeilenbasiert auf
gruppenbasiert (Abschnitt 6) — das ist eine bewusste Fortschreibung des in
[[2026-08-12-fa-struktur-darstellung-spec]] und [[2026-08-12-listen-spaltenauswahl-spec]] bereits
etablierten Gruppen-Musters, keine neue Pattern-Variante. BDE-Cockpit bleibt außerhalb des
ADR-0005-Patterns (Karten-Grid) und ist entsprechend gesondert zu behandeln, siehe Offene
Rückfrage 4.

## Akzeptanzkriterien

1. **F1 — Gruppierung ausschließlich bei Master `true`.** Bei Master `false` zeigt jede der sechs
   Ansichten weiterhin genau ein `<tbody>` mit Einzelzeilen, keine Gruppen-Kopfzeilen — geprüft an
   allen fünf Standardlisten plus `PickingLeitstand`.
2. **Zwei-Ebenen-Darstellung mit vollständigem Scope.** Bei Master `true` erscheint für jede
   `OrderNumber` genau eine Gruppen-Kopfzeile; darunter erscheinen **alle** Nachfahren-Sub-FAs als
   Zeilen — verifiziert an einer mindestens dreistufigen Teststruktur (Sub-FA unter Sub-FA), bei der
   ein Enkel-Sub-FA sichtbar und bedienbar ist, nicht nur die direkten Kinder.
3. **Elternbezug erhalten.** Die Spalte `parent-sub-order-number` zeigt `ParentSubOrderNumber` korrekt
   je Sub-FA-Zeile, ist im Erstzustand ausgeblendet (`defaultHidden`) und über den Zahnrad-Dialog
   einblendbar; bei der Wurzel/HauptFA-Zeile ist sie leer.
4. **Standardsicht aufgeklappt, Zuklappen je Gruppe funktioniert.** Beim ersten Laden sind alle Gruppen
   offen; ein Klick auf den Chevron einer Gruppe klappt nur diese zu, andere Gruppen bleiben
   unberührt.
5. **F2 — Suchtreffer in zugeklappter Gruppe klappt automatisch auf.** Eine Gruppe zuklappen, dann
   nach einer `SubOrderNumber` dieser Gruppe suchen → die Gruppe klappt automatisch auf, der Treffer
   ist hervorgehoben. Ohne diese Funktion würde derselbe Test ein scheinbar leeres Ergebnis liefern —
   das ist der zu verhindernde stille Fehler.
6. **Suchreihenfolge.** Eine Suche, die auf eine `SubOrderNumber` passt, selektiert genau diese Zeile;
   eine Suche, die nur auf eine `OrderNumber` passt (kein `SubOrderNumber`-Treffer), selektiert die
   ganze Gruppe.
7. **F3 — Paginierung über Gruppen.** Bei einer Filterauswahl mit mehr Gruppen als der eingestellten
   Seitengröße wird nie eine Gruppe über zwei Seiten getrennt; `TotalCount`/Seitenzahl basiert auf der
   Gruppenanzahl, nicht auf der Zeilenanzahl.
8. **Spalten-Mapping wie in Abschnitt 5 dokumentiert.** `order-number`-Zelle zeigt im hierarchischen
   Modus `SubOrderNumber`, in der Gruppen-Kopfzeile steht `OrderNumber`; keine Spalte trägt im
   hierarchischen und im flachen Modus unterschiedliche fachliche Bedeutungen ohne eigene Spalte.
9. **SageMissingSince-Badge unabhängig von `IsCancelled`.** Ein Sub-FA mit `SageMissingSince != null`
   zeigt einen eigenen Badge mit eigenem Tooltip-Text, bleibt in der Liste sichtbar und bedienbar
   (keine automatische Sperre/Ausblendung); ein `IsCancelled`-Auftrag im Flachmodus zeigt weiterhin
   ausschließlich den bestehenden „In Sage gelöscht"-Badge — die beiden Badges/Bedingungen greifen nie
   gleichzeitig für dieselbe Zeile.
10. **F6 — AKE-Regression, harte Bedingung.** Bei Master `false` ist jede der sechs Ansichten
    bit-identisch zum heutigen Verhalten: keine Gruppierung, keine neu sichtbare Spalte, unveränderte
    Suche, unveränderte (zeilenbasierte) Paginierung — geprüft für alle sechs Ansichten einzeln.
11. **Kaskade A — vollständiger Unterbaum**, sobald Offene Rückfrage 1 den Träger festlegt: Ein
    Fertigmeldungs-Aufruf am Haupt-FA einer mindestens dreistufigen Teststruktur markiert **alle**
    Nachfahren-Sub-FAs (nicht nur die direkten Kinder) als erledigt.
12. **Kaskade B — Bestätigungsdialog mit Zahl**, sobald Träger und Datenquelle (Offene Rückfrage 6)
    geklärt sind: Der Dialog nennt die Gesamtzahl betroffener Sub-FAs und separat die Anzahl mit noch
    offenen Arbeitsgängen/Rückmeldungen; ein Abbruch im Dialog löst keine Buchung aus.
13. **Kaskade C — Atomarität**: Ein simulierter Fehler mitten in der Kaskade (z. B. gesperrter
    Datensatz eines Sub-FA) hinterlässt **keinen** Sub-FA als „halb erledigt" — entweder alle
    betroffenen Zeilen der Kaskade sind aktualisiert oder keine.
14. **Kaskade D — als eine Kaskade im Audit erkennbar**, sobald der technische Weg (Offene
    Rückfrage 7) feststeht: die Auswertung zeigt die N betroffenen Zeilen als zusammengehörige Aktion,
    nicht als N unabhängige Einzeländerungen ohne erkennbaren Zusammenhang.
15. **Rücknahme-Asymmetrie, falls vorhanden** (Offene Rückfrage 2): Eine Rücknahme an einem Sub-FA
    öffnet **nur** diesen einen, nicht seine Geschwister oder den übergeordneten Haupt-FA; der
    Rücknahme-Dialog am Haupt-FA benennt explizit, dass Sub-FAs fertig bleiben.
16. Keine Änderung an bestehenden Zugriffsfiltern (siehe Tabelle „Betroffene Rollen / Zugriffsfilter")
    — Regressionstest je Rolle: ein Benutzer ohne den jeweiligen Filter sieht die Ansicht weiterhin
    nicht.

## Test-Szenarien

Neues Kapitel „IDEAL — FA-Liste und verwandte Ansichten: Hierarchische Darstellung" in
`docs/TESTSZENARIEN.md`, mit **eigenem** Unterabschnitt je Fallstrick (F1–F6) plus Kaskade:

- **F1 — Master aus/an:** dieselbe Ansicht einmal mit Master `false`, einmal mit Master `true` öffnen
  → im ersten Fall unverändert wie vor dieser Spec, im zweiten Fall gruppiert.
- **Vollständiger Nachfahren-Scope:** dreistufige Teststruktur (Sub-FA unter Sub-FA) anlegen/nutzen,
  prüfen, dass der Enkel-Sub-FA als eigene Zeile in der Gruppe erscheint, nicht nur die direkten
  Kinder.
- **Zuklappen je Gruppe:** eine Gruppe zuklappen, andere bleiben offen; erneut laden → Standardsicht
  wieder vollständig aufgeklappt.
- **F2 — Suchtreffer in zugeklappter Gruppe (eigenes Szenario, nicht Nebensatz):** Gruppe zuklappen,
  nach `SubOrderNumber` eines darin enthaltenen Sub-FA suchen → Gruppe klappt automatisch auf, Treffer
  hervorgehoben. Negativfall: Suche nach einer nicht existierenden Nummer → erwartbar leeres
  Ergebnis, klar als „kein Treffer" erkennbar (Abgrenzung zum stillen Fehler).
- **F3 — Gruppen-Paginierung:** Filterauswahl mit mehr Gruppen als Seitengröße → keine Gruppe über
  zwei Seiten getrennt, Seitenzahl entspricht Gruppenanzahl.
- **Spalten-Mapping:** `parent-sub-order-number` einblenden, Wert je Sub-FA-Zeile mit `/FaHierarchy`
  gegenprüfen; Zahnrad-Dialog zeigt die Spalte im Erstzustand ausgeblendet.
- **SageMissingSince-Badge:** einen Sub-FA mit gesetztem `SageMissingSince` anzeigen (Testdaten-
  Vorbedingung, ggf. manuell in Testumgebung gesetzt) → eigener Badge/Tooltip, Zeile bleibt bedienbar;
  gegenprüfen, dass ein AKE-`IsCancelled`-Auftrag im Flachmodus weiterhin nur den bestehenden Badge
  zeigt.
- **F6 — Regression Flachmodus:** bestehende TESTSZENARIEN-Kapitel der sechs Ansichten (falls
  vorhanden) unverändert grün, zusätzlich Sichtprüfung bei Master `false`.
- **Kaskade (sobald Träger geklärt):** Fertigmeldung am Haupt-FA einer mehrstufigen Struktur →
  Bestätigungsdialog mit korrekter Zahl (inkl. „davon offen") → Bestätigen → alle Nachfahren erledigt,
  eine erkennbare Kaskaden-Aktion im Audit. Abbruch im Dialog → keine Änderung.
- **Rücknahme-Asymmetrie (falls vorhanden):** Rücknahme an einem einzelnen Sub-FA → nur dieser öffnet
  sich wieder, Geschwister bleiben erledigt; Rücknahme-Dialog am Haupt-FA zeigt den Hinweistext.
- **Rollen-Regression:** je Ansicht ein Benutzer ohne den jeweiligen Zugriffsfilter → Ansicht bleibt
  gesperrt wie zuvor.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

- **Web-App:** ja — reine `IdealAkeWms`-Änderungen (Controller, Views, ein neues JS-Modul, additive
  `ColumnDefinitions.cs`-Erweiterung, ggf. neue Kaskade-Endpunkte).
- **Service:** nein — `IDEALAKEWMSService` ist nicht betroffen (die Materialisierungs-/Sync-Logik aus
  Teil 7 Etappe C bleibt unverändert; diese Spec ist reine Anzeige/Aktion im Web-Projekt).
- **Migration:** nein (Ausnahme siehe Migrations-/SQL-Auswirkungen, abhängig von Offener
  Rückfrage 7).
- **Kontext:** Die zugrunde liegenden `SubOrderNumber`/`ParentSubOrderNumber`/`SageMissingSince`-Felder
  existieren aktuell **ausschließlich** im noch nicht gemergten Epic-Worktree
  `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch `feature/2026-08-07-ideal-teile-1-5`, Teil 7
  Status laut Brain: Etappen A–E **erledigt**, wartet auf QA/Schranke 2 fürs gesamte Bündel). Diese
  Spec kann daher nur in **demselben** Worktree sinnvoll umgesetzt werden — dieselbe Konstellation wie
  bei den beiden Schwester-Specs vom 2026-08-12. Vor Umsetzungsbeginn `scripts/sync-worktree.ps1`
  laufen lassen, um den Branch aktuell zu halten.
- **Publish-Befehle (nachgelagert, im Worktree):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rückfragen

1. → **F4/Kaskade-Träger:** Es gibt keinen einheitlichen „Fertigmeldung"-Button im Code. Kandidaten
   sind der symmetrische Toggle `PickingController.ToggleDone`/`IsDonePicking` und der explizite
   `BdeStatusApiController`-Toggle `IsDoneBde`. `FaCompletion` betrifft trotz seines Namens eine
   andere Sache (`IsSpecComplete`). Auf welche Aktion(en) bezieht sich die im Beschluss verlangte
   Kaskade?
2. → **Rücknahme:** Da die gefundenen Erledigt-Mechanismen heute EIN symmetrischer Toggle sind (kein
   eigenes Setzen/Zurücknehmen), existiert die im Beschluss vorausgesetzte „Rücknahme-Funktion" im
   engeren Sinn nicht. Wird der Toggle in zwei Pfade aufgespalten (Setzen mit Kaskade / Zurücknehmen
   ohne Kaskade), oder entfällt der ganze Rücknahme-Abschnitt ersatzlos, wie im Backlog für den Fall
   „gibt es nicht" vorgesehen?
3. → **PickingLeitstand:** ist entgegen der Backlog-Einschätzung technisch eine reguläre
   ADR-0005-Liste (verifiziert: ein `<tbody>`, `data-server-column-filter="true"`, Pagination).
   Bekommt sie dieselbe Zwei-Ebenen-Behandlung wie die fünf anderen Listen? Und falls ja: bezieht sich
   die Kaskade-Regel dort auf `BulkRelease` (Freigabe), oder hat Leitstand überhaupt keine
   Kaskade-Aktion?
4. → **BDE-Cockpit:** ist tatsächlich kein Tabellen-Layout, sondern ein JS-Karten-Grid ohne
   `<table>`/`<tbody>` (verifiziert). Das Zwei-Ebenen-`tbody`-Muster ist dort strukturell nicht direkt
   übertragbar. Ist ein eigener Kartengruppierungs-Entwurf Teil DIESER Spec, oder wird das Cockpit für
   die erste Umsetzungsrunde bewusst ausgeklammert (nur die 5 Tabellen-Listen + ggf. Leitstand)?
5. → **SageMissingSince/Teil-7-AK-6:** Der Sync-Mechanismus (Regel 2) ist laut Teil-7-Spec (AK 6/
   AK 12, Etappe C, Commits dc297d9..bc7e3d6) unit-getestet und funktionsfähig — an der Erzeugung des
   Werts ist nichts nachzubessern. Ob im produktiven IDEAL-Bestand aktuell tatsächlich ein Datensatz
   mit `SageMissingSince <> NULL` existiert, lässt sich aus Brain/Code nicht feststellen. Blockiert
   das den Dev-Lauf, oder wird die Anzeige unabhängig vom aktuellen Datenstand gebaut (Empfehlung:
   Letzteres, siehe Fachliche Anforderungen)?
6. → **Bestätigungsdialog-Zahl** „davon offene Arbeitsgänge/Rückmeldungen": Welche konkrete
   Datenquelle zählt das — offene `FaWorkStep`, `ProductionOrderBdeStatus`/laufende BDE-Buchungen,
   ungebuchte `StockMovement`, oder eine Kombination? Ohne Festlegung kann der Dev-Lauf die Zahl nicht
   berechnen.
7. → **Kaskaden-Audit** (Anforderung D, „als EINE Kaskade erkennbar"): Es existiert aktuell keine
   generische User-Aktions-Audit-Tabelle (nur `AuditableEntity`-Felder je Zeile und `ISyncLogger` für
   Hintergrund-Services, ADR 0010). Wie wird die Kaskaden-Zusammengehörigkeit technisch sichtbar —
   z. B. eine gemeinsame Korrelations-ID je betroffene Zeile in einer neuen, kleinen Tabelle (dann
   eigene Migration), Wiederverwendung eines bestehenden Mechanismus, oder reicht ein gemeinsamer
   Zeitstempel plus TempData-Meldung mit Anzahl? Migrationsentscheidung wird hier bewusst NICHT
   vorweggenommen.
8. → **Column-Mapping „nicht mehr in Sage":** Verifiziert im Code unterscheiden sich AKE
   (`IsCancelled`-Badge „In Sage gelöscht", Auftrag wird automatisch storniert und verschwindet aus
   offenen Sichten) und IDEAL (`SageMissingSince`, Auftrag bleibt sichtbar/rückmeldefähig, nur
   zeitgestempelt markiert, Sync-Regel 2) fachlich. Empfehlung dieser Spec: kein Feld-Mapping, sondern
   nur dieselbe Render-Position (Badge neben der FA-/Sub-FA-Nummer-Zelle) mit eigenem Text/Tooltip für
   den hierarchischen Fall — bestätigen?

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →
2. →
3. →
4. →
5. →
6. →
7. →
8. →
