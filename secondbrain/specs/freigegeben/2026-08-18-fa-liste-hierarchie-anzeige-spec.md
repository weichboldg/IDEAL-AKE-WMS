---
type: spec
title: "FA-Liste und verwandte Ansichten hierarchiefaehig darstellen (dritte Fehlerklasse nach Teil 7)"
slug: 2026-08-18-fa-liste-hierarchie-anzeige-spec
status: InUmsetzung
created: 2026-08-18
updated: 2026-08-20
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
open_questions: []
beantwortete_rueckfragen:
  - "F4/Kaskade-Traeger: Es gibt keinen einheitlichen 'Fertigmeldung'-Button im Code. Kandidaten sind der symmetrische Toggle `PickingController.ToggleDone`/`IsDonePicking` und der explizite `BdeStatusApiController`-Toggle `IsDoneBde`. `FaCompletion` betrifft trotz seines Namens eine andere Sache (`IsSpecComplete`). Auf welche Aktion(en) bezieht sich die im Beschluss verlangte Kaskade?"
  - "Rücknahme: Da die gefundenen Erledigt-Mechanismen heute EIN symmetrischer Toggle sind (kein eigenes Setzen/Ruecknehmen), existiert die im Beschluss vorausgesetzte 'Ruecknahme-Funktion' im engeren Sinn nicht. Wird der Toggle in zwei Pfade aufgespalten (Setzen mit Kaskade / Ruecknehmen ohne Kaskade), oder entfaellt der ganze Ruecknahme-Abschnitt ersatzlos, wie im Backlog fuer den Fall 'gibt es nicht' vorgesehen?"
  - "PickingLeitstand ist entgegen der Backlog-Einschaetzung technisch eine reguläre ADR-0005-Liste (verifiziert: ein `<tbody>`, `data-server-column-filter=\"true\"`, Pagination). Bekommt sie dieselbe Zwei-Ebenen-Behandlung wie die fuenf anderen Listen? Und falls ja: bezieht sich die Kaskade-Regel dort auf `BulkRelease` (Freigabe), oder hat Leitstand ueberhaupt keine Kaskade-Aktion?"
  - "BDE-Cockpit ist tatsaechlich kein Tabellen-Layout, sondern ein JS-Karten-Grid ohne `<table>`/`<tbody>` (verifiziert). Das Zwei-Ebenen-tbody-Muster ist dort strukturell nicht direkt uebertragbar. Ist ein eigener Kartengruppierungs-Entwurf Teil DIESER Spec, oder wird das Cockpit fuer die erste Umsetzungsrunde bewusst ausgeklammert (nur die 5 Tabellen-Listen + ggf. Leitstand)?"
  - "SageMissingSince/Teil-7-AK-6: Der Sync-Mechanismus (Regel 2) ist laut Teil-7-Spec (AK 6/AK 12, Etappe C, Commits dc297d9..bc7e3d6) unit-getestet und funktionsfaehig — an der Erzeugung des Werts ist nichts nachzubessern. Ob im produktiven IDEAL-Bestand aktuell tatsaechlich ein Datensatz mit `SageMissingSince <> NULL` existiert, laesst sich aus Brain/Code nicht feststellen. Blockiert das den Dev-Lauf, oder wird die Anzeige unabhaengig vom aktuellen Datenstand gebaut (Empfehlung: Letzteres, siehe Fachliche Anforderungen)?"
  - "Bestaetigungsdialog-Zahl 'davon offene Arbeitsgaenge/Rueckmeldungen': Welche konkrete Datenquelle zaehlt das — offene `FaWorkStep`, `ProductionOrderBdeStatus`/laufende BDE-Buchungen, ungebuchte `StockMovement`, oder eine Kombination? Ohne Festlegung kann der Dev-Lauf die Zahl nicht berechnen."
  - "Kaskaden-Audit (Anforderung D, 'als EINE Kaskade erkennbar'): Es existiert aktuell keine generische User-Aktions-Audit-Tabelle (nur `AuditableEntity`-Felder je Zeile und `ISyncLogger` fuer Hintergrund-Services, ADR 0010). Wie wird die Kaskaden-Zusammengehoerigkeit technisch sichtbar — z. B. eine gemeinsame Korrelations-ID je betroffene Zeile in einer neuen, kleinen Tabelle (dann eigene Migration), Wiederverwendung eines bestehenden Mechanismus, oder reicht ein gemeinsamer Zeitstempel plus TempData-Meldung mit Anzahl? Migrationsentscheidung wird hier bewusst NICHT vorweggenommen."
  - "Column-Mapping 'nicht mehr in Sage': Verifiziert im Code unterscheiden sich AKE (`IsCancelled`-Badge 'In Sage geloescht', Auftrag wird automatisch storniert und verschwindet aus offenen Sichten) und IDEAL (`SageMissingSince`, Auftrag bleibt sichtbar/rueckmeldefaehig, nur zeitgestempelt markiert, Sync-Regel 2) fachlich. Empfehlung dieser Spec: kein Feld-Mapping, sondern nur dieselbe Render-Position (Badge neben der FA-/Sub-FA-Nummer-Zelle) mit eigenem Text/Tooltip fuer den hierarchischen Fall — bestaetigen?"
epic: true
etappen:
  - "A: Anzeige-Fundament (Repo-Gruppenabfrage master-gated + ColumnDefinitions parent-sub-order-number + JS-Modul fa-liste-gruppierung.js + Master-Lese-Muster) + ProductionOrders als Referenz-View (Gruppierung/order-number->SubOrderNumber/parent-sub-order-number/SageMissingSince-Badge/Gruppen-Pagination/Z4-Zaehlformat) + Z4-ERHEBUNG (alle Zaehlstellen katalogisieren, keine Umsetzung). STOPP+Melden nach A (Referenz-View am Testsystem ansehen)."
  - "B: Muster auf die 5 weiteren Views replizieren (FaWorklist, FaCompletion, Tracking, Picking, PickingLeitstand — je Anzeige-Teil: Gruppierung/Spalte/Suche/Pagination/Badge)"
  - "C: Kaskade auf PickingLeitstand-Gruppenkopfzeile 'Alle Sub-FAs fertigmelden' (IsDoneBde alle Nachfahren, atomar/Transaktion, Bestaetigungsdialog mit Offene-Buchung-Zaehlung [jede nicht beendete/nicht stornierte Buchung], ILogger EINE Kaskade); Zeilen-Toggle unveraendert; keine Gruppen-Ruecknahme"
  - "D: Z4-Zaehl-Sweep umsetzen (alle in A erhobenen Zaehlstellen auf 'N Auftraege · M Sub-FAs') + Z3-Sync-Meldung (neuer Sub-FA unter fertigem HauptFA: offen anlegen + loggen + Gruppen-Badge, keine Auto-Korrektur) + Z1-Regressionstest (fertigmelden->Sync->Status unveraendert)"
  - "E: Testszenarien (F1-F6 + Kaskade) + testszenarien-index + Brain + qa-agent -> Testbereit"
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-08-20
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

## Etappen (epic: true — Umbau 2026-08-20 wegen Umfang, Entscheidung Mensch)

Diese freigegebene Spec wurde am 2026-08-20 zu einem Epic umstrukturiert, weil der Umfang (6 Views +
Gruppen-Pagination + neues JS-Modul + Kaskade + Z4-Sweep + Z1/Z3-Sync-Teile) einen /dev-Einzellauf
sprengt. Inhalt/Entscheidungen unveraendert; nur in Etappen geschnitten. Ein langlebiger Worktree
(Buendel `feature/2026-08-07-ideal-teile-1-5`), kein Zwischen-Merge, QA erst am Ende (Etappe E).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| A | Anzeige-Fundament (Repo-Gruppenabfrage master-gated, ColumnDefinitions `parent-sub-order-number`, JS-Modul `fa-liste-gruppierung.js`, Master-Lese-Muster) + **ProductionOrders als Referenz-View** + **Z4-ERHEBUNG** (Zaehlstellen katalogisieren, keine Umsetzung). **STOPP+Melden** nach A. | **erledigt** | `30b7a8c` (Repo) · `9315186` (ColumnDef+JS) · `abd2620` (View/Controller) |
| B | Muster auf die 5 weiteren Views replizieren (FaWorklist, FaCompletion, Tracking, Picking, PickingLeitstand — Anzeige-Teil) | offen | |
| C | Kaskade auf PickingLeitstand-Gruppenkopfzeile („Alle Sub-FAs fertigmelden", `IsDoneBde` alle Nachfahren, atomar, Dialog mit Offene-Buchung-Zahl, ILogger); Zeilen-Toggle unveraendert; keine Gruppen-Ruecknahme | **erledigt** | `a305d6f` |
| D | Z4-Zaehl-Sweep umsetzen + Z3-Sync-Meldung (neuer Sub-FA unter fertigem HauptFA) + Z1-Regressionstest | offen | |
| E | Testszenarien (F1–F6 + Kaskade) + Brain + qa-agent → Testbereit | offen | |

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

**ENTSCHIEDEN (2026-08-20): Traeger ist `IsDoneBde` (`BdeStatusApiController`), Wirt-View ist der
`PickingLeitstand`.** Eine einzelne, eindeutig benannte „Fertigmeldung"-Aktion gibt es im Repository
nicht; die Kandidaten wurden geprueft:

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

**Verbindliche Verortung (am Code verifiziert):** Der `IsDoneBde`-Zeilen-Toggle wird heute
**ausschliesslich** in `Views/PickingLeitstand/Index.cshtml` gerendert. Dort — und nur dort — lebt
daher auch die Gruppen-Aktion:
- Der **Zeilen-Toggle bleibt unveraendert** symmetrisch und wirkt nur auf seine eigene Zeile.
- Die **Gruppen-Kopfzeile bekommt zusaetzlich die Aktion „Alle Sub-FAs fertigmelden"**, die
  ausschliesslich auf `true` setzt.
- Die **fuenf Standardlisten exponieren `IsDoneBde` nicht** und bekommen deshalb **nur den
  Anzeige-Teil** (Gruppierung, Spalte, Suche, Paginierung, Badge) — keine Kaskade, kein neues
  Bedienelement. Wo heute keine Fertigmeldung stattfindet, wird auch keine eingefuehrt.

Die **Anforderungen A–D** gelten fuer diese Gruppen-Aktion:

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

**Ruecknahme: es gibt KEINE Gruppen-Ruecknahme [ENTSCHIEDEN 2026-08-20]**

Der urspruengliche Gedanke einer *asymmetrischen* Kaskade (Setzen kaskadiert, Zuruecknehmen nicht)
wird **verworfen**: Ein Toggle, der beim Einschalten zwanzig Auftraege mitnimmt und beim Ausschalten
nur einen, ist am Bildschirm nicht vermittelbar — und dasselbe Bedienelement haette in der
Gruppen-Kopfzeile eine andere Bedeutung als in der Zeile.

Stattdessen:
- Der **Zeilen-Toggle** (`IsDoneBde`) bleibt **exakt wie heute**: symmetrisch, nur die eigene Zeile.
  Keine Aenderung, damit auch keine Regression an dieser Stelle.
- Die **Gruppen-Aktion setzt ausschliesslich auf `true`.** Ein Gruppen-Zuruecknehmen wird **nicht
  angeboten**.
- Wer den Unterbaum wieder oeffnen will, tut das je Zeile. Damit bleiben eigenstaendig gesetzte
  Fertigmeldungen erhalten — genau das Ziel der urspruenglichen Asymmetrie, nur ohne schiefe
  Bedienlogik.

### 8. Per-View-Behandlung — nicht pauschal, sondern je Ansicht entschieden [F5]

| Ansicht | ADR-0005-Liste? | Gruppierung anwendbar? | Bestehende Aktion(en) | Kaskade-Bezug |
|---|---|---|---|---|
| `ProductionOrders` (FA-Liste) | Ja | Ja — volle Behandlung (Abschnitt 2–6) | Nur Anzeige (`IsDone`/`IsDonePicking` als kombiniertes Badge), keine eigene Erledigt-Aktion in dieser View | Keine eigene Kaskade-Aktion hier, nur Anzeige des Ergebnisses |
| `FaWorklist` | Ja (verifiziert: `data-view-key="FaWorklist"`, ein `<tbody>`) | Ja — volle Behandlung | Zu prüfen im Dev-Lauf (kein Erledigt-Toggle bekannt) | Voraussichtlich keine |
| `FaCompletion` | Ja (verifiziert), aber **Namenskollision** (siehe Abschnitt 7) | Ja — Anzeige-Teil | `IsSpecComplete`-Toggle, andere fachliche Bedeutung | Kaskade-Regel gilt hier NICHT ohne Weiteres — eigene Prüfung nötig, nicht automatisch übernehmen |
| `Tracking` | Ja (verifiziert) | Ja — volle Behandlung | Keine Erledigt-Aktion (reine Teileverfolgung) | Entfällt |
| `Picking` | Ja (verifiziert) | Ja — volle Behandlung | `ToggleDone`/`IsDonePicking` (symmetrischer Toggle) | **Keine Kaskade** — Kommissionierung kaskadiert bewusst nicht: jeder Sub-FA hat eigenes Material zu holen |
| `PickingLeitstand` | Ja (verifiziert) | **Ja — volle Behandlung** | `BulkRelease` (Freigabe) **+ `IsDoneBde`-Zeilen-Toggle** (einziger Ort im System) | **WIRT-VIEW der Gruppen-Kaskade.** `BulkRelease` kaskadiert **nicht**; die Aktion „Alle Sub-FAs fertigmelden" sitzt in der Gruppen-Kopfzeile |
| BDE-Cockpit (`BdeCockpit`) | **Nein** — verifiziert: JS-Karten-Grid, kein `<table>`/`<tbody>` | Nein, ohne eigenen Entwurf | `IsDoneBde`-Toggle (`BdeStatusApiController`) | **Ausgeklammert** fuer diese Runde → eigener Backlog-Eintrag. **Bekannte Einschraenkung:** zeigt im hierarchischen Modus ~130 statt ~30 Karten und wird voruebergehend unuebersichtlich |

Konsequenz fuer den Dev-Lauf: Die **fuenf** Standardlisten (`ProductionOrders`, `FaWorklist`,
`FaCompletion`, `Tracking`, `Picking`) **und** `PickingLeitstand` bekommen die volle
Zwei-Ebenen-Behandlung aus Abschnitt 2–6. Die **Gruppen-Kaskade** kommt ausschliesslich auf den
`PickingLeitstand`. Das **BDE-Cockpit** ist fuer diese Runde ausgeklammert (eigener Backlog-Eintrag,
bekannte Einschraenkung dokumentiert). Keine offenen Rueckfragen mehr.

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
    **Ausdruecklich zusaetzlich: kein neues Bedienelement, keine neue Aktion.** Insbesondere
    erscheint auf dem `PickingLeitstand` bei Master `false` **keine** Gruppen-Kopfzeile und damit
    auch kein „Alle Sub-FAs fertigmelden"-Knopf. Die Bedingung wird geprueft, nicht nur zufaellig
    erfuellt.
11. **Kaskade A — vollstaendiger Unterbaum.** Ein Klick auf „Alle Sub-FAs fertigmelden" in der
    Gruppen-Kopfzeile des `PickingLeitstand` setzt bei einer mindestens dreistufigen Teststruktur
    **alle** Nachfahren-Sub-FAs auf `IsDoneBde = true` — nicht nur die direkten Kinder.
12. **Kaskade B — Bestaetigungsdialog mit Zahl.** Der Dialog nennt die Gesamtzahl betroffener
    Sub-FAs und separat die Anzahl **mit offener Buchung** (jede nicht beendete, nicht stornierte
    BDE-Buchung, also auch pausierte/auto-pausierte); die Beschriftung lautet entsprechend „mit
    offener Buchung", nicht „wird gerade bearbeitet". Ein Abbruch im Dialog loest keine Buchung aus.
13. **Kaskade C — Atomarität**: Ein simulierter Fehler mitten in der Kaskade (z. B. gesperrter
    Datensatz eines Sub-FA) hinterlässt **keinen** Sub-FA als „halb erledigt" — entweder alle
    betroffenen Zeilen der Kaskade sind aktualisiert oder keine.
14. **Kaskade D — im Log als EINE Kaskade erkennbar.** Ein `ILogger`-Eintrag je Kaskade mit
    Haupt-FA, Anzahl betroffener Sub-FAs und Benutzer; die betroffenen Zeilen tragen ihre
    bestehenden Audit-Felder mit identischem Zeitstempel. **Keine neue Tabelle, keine Migration** —
    ein abfragbares Benutzer-Audit ist ein eigenes Feature.
15. **Keine Gruppen-Ruecknahme.** Die Gruppen-Aktion setzt ausschliesslich auf `true`; ein
    Gruppen-Zuruecknehmen wird nicht angeboten. Der `IsDoneBde`-Zeilen-Toggle verhaelt sich
    unveraendert symmetrisch und wirkt nur auf seine eigene Zeile — nachweisbar daran, dass ein
    Zuruecknehmen an einem Sub-FA weder Geschwister noch den Haupt-FA veraendert.
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
8. → siehe **Z5** im Abschnitt „Zusaetzliche Entscheidungen" unten — dort beantwortet.

## Zusaetzliche Entscheidungen (2026-08-18) — vier Punkte, die in keiner Frage stehen

### Z1 — Der Materialisierungs-Sync darf WMS-Zustaende NICHT anfassen [BEREITS ERFUELLT, nur Regressionstest]

> **Herabgestuft am 2026-08-20 nach Code-Pruefung.** Urspruenglich als KRITISCH gefuehrt — die
> Pruefung hat ergeben, dass die Eigenschaft **bereits gegeben** ist:
> `FaMaterializationSyncService`/`Planner` setzen die Quellfelder **einzeln** (kein `SetValues`,
> kein Entitaets-Ersatz), der Klassenkommentar haelt es ausdruecklich fest, und `IsDoneBde` liegt
> auf der **separaten Entitaet `ProductionOrderBdeStatus`**, die der Sync gar nicht laedt — doppelt
> abgesichert. **Kein Handlungsbedarf am Sync.**
>
> **Wortlaut-Korrektur:** Die Kaskade schreibt an **`ProductionOrderBdeStatus`**, nicht an
> `ProductionOrders`. Die urspruengliche Formulierung unten war an dieser Stelle ungenau.
>
> **Was bleibt:** ein **Regressionstest**, der die Eigenschaft festnagelt — fertigmelden, Sync
> laufen lassen, Status unveraendert. Damit sie nicht bei einer spaeteren Umstellung des
> Update-Pfads unbemerkt verlorengeht. Das ist der einzige Umsetzungsanteil von Z1.

Die Kaskade schreibt ein Status-Feld. Derselbe Datensatz wird alle 15 Minuten
vom Materialisierungs-Sync abgeglichen. **Ueberschriebe der Abgleich dabei WMS-eigene Felder, waere
die Fertigmeldung nach einer Viertelstunde weg** — ohne Fehler, ohne Meldung.

**Der konkrete Fallstrick ist ein EF-Muster:**
`context.Entry(vorhanden).CurrentValues.SetValues(neuAusDerView)` ueberschreibt **alle** Spalten —
auch die, die das Quellobjekt gar nicht kennt und deshalb auf Default stehen. Ein aus der View
gebautes Objekt hat `IsDoneBde = false`; damit setzt der Sync die Fertigmeldung still zurueck.

**Verbindlich:**
- Der Sync setzt beim Update **ausschliesslich die Quellfelder einzeln** (Struktur, Artikel,
  Bezeichnung, Mengen, `SageMissingSince`). **Kein `SetValues`, kein Ersetzen der Entitaet.**
- Alles, was im WMS entsteht — Fertigmeldungen, Kommissionier-Status, BDE-Buchungen, Zuordnungen,
  Notizen — bleibt unberuehrt.
- **Als Invariante formulieren, nicht als Sorgfaltshinweis**, und mit einem Test festnageln:
  Auftrag fertigmelden → Sync laufen lassen → Status ist unveraendert.
- **Vor der Umsetzung am bestehenden `FaMaterializationPlanner` pruefen**, wie der Update-Pfad
  heute gebaut ist. — **ERLEDIGT am 2026-08-20, siehe Kasten oben. Der Update-Pfad ist sauber.**

### Z2 — Sortierung innerhalb der Gruppe: Sub-FA-Nummer aufsteigend

Standard-Sortierung je Gruppe: **`SubOrderNumber` aufsteigend**. Begruendung: stabil (aendert sich
nicht, wenn sich die Struktur aendert), vorhersagbar, und da die Sage-BelIDs fortlaufend vergeben
werden, entspricht sie in der Praxis weitgehend der Struktur-Reihenfolge.
Es ist die **Vorbelegung** — ueber die Spaltensortierung kann der Anwender umstellen.

### Z3 — Invariante "Haupt fertig ⇒ alle Sub-FAs fertig" — und der Fall, der sie von selbst bricht

Fachliche Zusage: Der Zustand "Haupt-FA fertig, Sub-FA offen" darf nicht vorkommen. Die Kaskade
stellt das beim Setzen sicher.

**ABER: Der Sync legt neue Sub-FAs an (Sync-Regel 1).** Taucht in Sage ein neuer Sub-FA unter einem
bereits fertiggemeldeten Haupt-FA auf, entsteht **genau der verbotene Zustand — ohne Zutun eines
Menschen.**

**Fachliche Einordnung (2026-08-18):** Nach dem initialen Erzeugen eines Auftrags kommen **keine
neuen Sub-FAs mehr dazu** — der Fall gilt als **ausgeschlossen**. Er wird daher **nicht als
Regelfall behandelt und braucht keine Aufloesungslogik.**

**Trotzdem erkennen und melden — nicht auf die Zusage verlassen.** Genau dieses Muster hat sich in
diesem Paket schon zweimal ausgezahlt: Die `SubFA = 0`-Annahme bei den Kommissionierlisten galt
auch als sicher und wurde vom ersten echten Datenlauf widerlegt; das Banner hat es sichtbar gemacht,
statt neun Positionen still zu verschlucken.
Daher als **Fallstrick dokumentiert** und minimal abgesichert:
- Der neue Sub-FA wird **offen** angelegt (ihn automatisch fertigzumelden waere eine Behauptung
  ueber Arbeit, die niemand geleistet hat).
- Der Haupt-FA wird **nicht** automatisch wieder geoeffnet (widerspraeche der Nicht-Kaskade bei der
  Ruecknahme).
- Der Fall wird **protokolliert und gemeldet**, in der Liste an der Gruppe **sichtbar
  gekennzeichnet**. Ein Mensch entscheidet: Auftrag doch nicht fertig, oder Sage-Datenfehler.
- **Keine automatische Korrektur** — tritt der Fall wider Erwarten auf, wird die Behandlung dann
  fachlich entschieden, nicht vorab geraten.

Aufwand dafuer: eine Pruefung im Anlege-Pfad des Sync plus ein Log-Eintrag. Billig genug, um sich
den Beweis nicht schuldig zu bleiben.

### Z3b — Filterwirkung (folgt aus Z3)

Die Invariante gilt nur in eine Richtung: *Sub-FA fertig* heisst **nicht**, dass der Haupt-FA fertig
ist. Eine Gruppe kann also offene und fertige Zeilen mischen.
**Regel:** Filter (z. B. "nur offene") wirken auf **Zeilen**; eine Gruppe erscheint, solange
mindestens eine Zeile passt. Die Kopfzeile bleibt als Kontext stehen, auch wenn der Haupt-FA selbst
nicht auf den Filter passt. Faellt eine Gruppe auf null Zeilen, verschwindet sie ganz.

### Z4 — Zaehlungen: beide Zahlen, ueberall dieselbe Konvention

Im hierarchischen Modus zaehlt jede Anzahl-Anzeige plötzlich Sub-FAs statt Auftraege — aus 30 werden
130. Keine Fehlfunktion, aber jeder, der die Zahl kennt, haelt sie fuer falsch.

**Format: `30 Auftraege · 130 Sub-FAs`** — die Gruppenzahl primaer (sie passt zur Gruppierung und zur
Paginierung, die ebenfalls ueber Gruppen laeuft), die Zeilenzahl als Ergaenzung.
Im flachen Modus wie bisher **eine** Zahl (dort sind beide identisch).

**Der wichtigere Teil: dieselbe Konvention an JEDER Stelle** — Listenkopf, Startseite, Auswertungen,
Kacheln. Zeigt eine Stelle 30 und eine andere 130, weiss niemand mehr, welche stimmt — und das
untergraebt das Vertrauen in alle Zahlen der Anwendung, nicht nur in diese eine.
**Erhebung gehoert in den Umfang:** alle Stellen finden, die Auftraege zaehlen.

### Z5 — „nicht mehr in Sage": kein Feld-Mapping [ENTSCHIEDEN, Rueckfrage 8]

Die Empfehlung des Spec-Laufs wird uebernommen. Verifiziert im Code unterscheiden sich AKE
   (`IsCancelled`-Badge „In Sage gelöscht", Auftrag wird automatisch storniert und verschwindet aus
   offenen Sichten) und IDEAL (`SageMissingSince`, Auftrag bleibt sichtbar/rückmeldefähig, nur
   zeitgestempelt markiert, Sync-Regel 2) fachlich. Empfehlung dieser Spec: kein Feld-Mapping, sondern
   nur dieselbe Render-Position (Badge neben der FA-/Sub-FA-Nummer-Zelle) mit eigenem Text/Tooltip für
   den hierarchischen Fall — bestätigen?

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. → **Kaskaden-Traeger ist `IsDoneBde` (`BdeStatusApiController`), NICHT `IsDonePicking`.**
   Fachlich entscheidbar, nicht nur nachzuschlagen:
   - **Kommissionierung darf nicht kaskadieren.** Jeder Sub-FA hat eigenes Material zu holen.
     „Haupt-FA kommissioniert" heisst nicht, dass die Teile fuer Sub-FA 3 aus dem Lager sind — die
     Kaskade wuerde Arbeit als erledigt behaupten, die niemand gemacht hat.
   - **Produktions-/BDE-Fertigmeldung soll kaskadieren.** Ist das Endgeraet fertig, sind seine
     Baugruppen es zwangslaeufig auch. Das ist logisch zwingend, nicht nur bequem.
   - Technisch passt es ebenfalls: `BdeStatusApi` setzt einen **expliziten** Wert, kein Flip — das
     uebersetzt sich natuerlich in „setze alle Nachfahren auf `true`".
   `FaCompletion` (`IsSpecComplete`) traegt **nur** den Anzeige-Teil, keine Kaskade — die
   Namenskollision ist zutreffend erkannt.

2. → **Der Ruecknahme-Abschnitt entfaellt in seiner bisherigen Form. Die Kaskade wird eine EIGENE
   Aktion, kein aufgespaltener Toggle.**
   Begruendung: Ein Toggle, der beim Einschalten zwanzig Auftraege mitnimmt und beim Ausschalten nur
   einen, ist am Bildschirm nicht vermittelbar — und ein und dasselbe Bedienelement haette in der
   Gruppen-Kopfzeile eine andere Bedeutung als in der Zeile.
   **Stattdessen:**
   - Der **Zeilen-Toggle bleibt exakt wie heute**: symmetrisch, wirkt nur auf seine Zeile. Keine
     Aenderung, damit auch keine AKE-Regression an dieser Stelle.
   - Die **Gruppen-Kopfzeile bekommt eine eigene, ausdrueckliche Aktion** „Alle Sub-FAs fertigmelden"
     mit dem Bestaetigungsdialog aus Anforderung B. Sie setzt **nur** auf `true`.
   - **Ein Gruppen-Zuruecknehmen wird nicht angeboten.** Wer den Unterbaum wieder oeffnen will, tut
     das je Zeile — damit bleiben eigenstaendig gesetzte Fertigmeldungen erhalten.
   Damit loesen sich Massenaktion-durch-kleinen-Klick, schiefe Toggle-Semantik und die
   Asymmetrie-Erklaerung in einem Zug. **AK 15 ist entsprechend umzuformulieren** (kein
   Ruecknahme-Dialog am Haupt-FA mehr).

3. → **PickingLeitstand: Gruppierung JA, Kaskade NEIN.**
   Die Zwei-Ebenen-Behandlung bekommt er wie die fuenf anderen — er ist verifiziert eine
   ADR-0005-Liste, und eine Ansicht, die als einzige flach bleibt, waere der Sonderfall, den niemand
   erklaeren kann.
   `BulkRelease` ist **Freigabe, nicht Fertigmeldung** — fachlich etwas anderes, keine Kaskade.
   Der Mehrfachauswahl-Mechanismus bleibt **zeilenbasiert und unveraendert**; eine
   „Gruppe-auswaehlen"-Erweiterung ist **out of scope** (waere ein eigenes Feature mit eigenem
   Risiko).

4. → **BDE-Cockpit: fuer diese Runde bewusst AUSGEKLAMMERT.** Ein Karten-Grid zu gruppieren ist ein
   eigener Entwurf, kein Nebenprodukt — das Zwei-Ebenen-`tbody`-Muster traegt dort nicht.
   **Aber die Folge muss benannt werden, nicht verschwiegen:** Im hierarchischen Modus zeigt das
   Cockpit statt ~30 nun ~130 Karten — es wird voruebergehend unuebersichtlich. Das gehoert als
   **bekannte Einschraenkung** in die Spec **und** als eigener Backlog-Eintrag
   („BDE-Cockpit hierarchiefaehig darstellen"), damit es nicht als vergessen durchgeht.

5. → **Anzeige unabhaengig vom aktuellen Datenstand bauen — blockiert nichts.** Empfehlung der Spec
   bestaetigt. Dass heute kein Datensatz mit `SageMissingSince <> NULL` existiert, ist erwartbar (der
   erste Materialisierungs-Lauf meldete **„0 vermisst"**) und sagt nichts ueber die Richtigkeit der
   Anzeige. Fuer den Test wird der Wert in der Testumgebung von Hand gesetzt.

6. → **Gezaehlt werden Sub-FAs mit einer LAUFENDEN, noch nicht beendeten BDE-Buchung.**
   Nicht offene `FaWorkStep` (das ist Spezifikations-Vollstaendigkeit, andere Sache), nicht
   ungebuchte `StockMovement` (Material, nicht Arbeit).
   Begruendung: Die Zahl soll genau eine Frage beantworten — **„Arbeitet gerade jemand daran?"**
   Einen Auftrag zu schliessen, an dem in diesem Moment gebucht wird, ist der Fall, den der Dialog
   verhindern soll. „Noch nicht fertig gemeldet" waere dagegen trivial wahr fuer alle betroffenen
   Zeilen und damit als Warnung wertlos.
   Laesst sich das mit den vorhandenen Abfragen nicht ermitteln, **zurueckmelden statt ersatzweise
   etwas anderes zaehlen** — eine falsch gefuellte Warnzahl ist schlechter als keine.

7. → **Audit: `ILogger` plus gemeinsamer Zeitstempel. KEINE neue Tabelle, KEINE Migration.**
   Anforderung D war eine Zusatzforderung von mir, keine fachliche Notwendigkeit. Wo keine
   Audit-Infrastruktur existiert, ist eine eigene Tabelle fuer einen Merkposten unverhaeltnismaessig
   — zumal diese Spec sonst migrationsfrei bleibt, was ihr groesster Vorzug ist.
   Umgesetzt wird: **ein** Log-Eintrag je Kaskade mit Haupt-FA, Anzahl betroffener Sub-FAs und
   Benutzer; die Zeilen selbst tragen ohnehin ihre bestehenden Audit-Felder mit identischem
   Zeitstempel.
   **AK 14 entsprechend abschwaechen:** „im Log als eine Kaskade erkennbar" statt „in der Auswertung
   als zusammengehoerige Aktion". Ein abfragbares Benutzer-Audit ist ein eigenes Feature — wenn
   gewuenscht, als Backlog-Eintrag, nicht hier eingeschmuggelt.

8. → siehe **Z5** im Abschnitt „Zusaetzliche Entscheidungen" — dort beantwortet (kein Feld-Mapping,
   gleiche Render-Position, eigener Text). **Bestaetigt.**

## Kritische Pruefung (2026-08-20)

Anwalt-des-Teufels-Durchsicht vor der Freigabe. Die Freigabe-Antworten 1–8 und Z1–Z5 sind als
massgeblich behandelt (nicht als offene Rueckfragen). Drei Punkte wurden gezielt am echten Code des
Buendel-Worktrees `.claude/worktrees/2026-08-07-ideal-teile-1-5` geprueft (dort liegen die
Teil-7-Felder). Belege mit `Datei:Zeile`.

### BLOCKER — vor der Freigabe zu klaeren

**BL1 — Die Kaskade-Aktion (Antwort 1) hat keine in-scope Wirt-View. Antworten 1/2/3/4 widersprechen
sich, WO das neue Gruppen-Bedienelement lebt.**
Kette der Befunde:
- Antwort 1 legt den Kaskade-Traeger auf `IsDoneBde` (`BdeStatusApiController`,
  `/api/bde-status/toggle`) fest. Verifiziert: `IsDoneBde` liegt auf der **eigenen** Entitaet
  `ProductionOrderBdeStatus` (`Models/ProductionOrderBdeStatus.cs:11`, keyed `ProductionOrderId`),
  nicht auf `ProductionOrder`.
- Antwort 2 verlangt in der **Gruppen-Kopfzeile** eine neue Aktion „Alle Sub-FAs fertigmelden" und
  sagt zugleich: „Der Zeilen-Toggle bleibt exakt wie heute … keine Aenderung."
- **Aber:** Die einzige Listen-View, die den `IsDoneBde`-Toggle heute ueberhaupt rendert, ist
  `PickingLeitstand` (`Views/PickingLeitstand/Index.cshtml:595`). Und Antwort 3 sagt fuer genau diese
  View **ausdruecklich „Kaskade NEIN"**. Die BDE-Cockpit-View (der andere natuerliche Ort fuer
  `IsDoneBde`) ist per Antwort 4 **ausgeklammert**.
- Die fuenf „Standardlisten" (`ProductionOrders`, `FaWorklist`, `FaCompletion`, `Tracking`,
  `Picking`) exponieren `IsDoneBde` heute **gar nicht** — `ProductionOrders` zeigt laut Spec nur ein
  kombiniertes Badge, keinen Erledigt-Toggle.

**Folge:** Es gibt aktuell **keine** in-scope View, auf der die `IsDoneBde`-Kaskade laut den
getroffenen Entscheidungen ansetzen darf. Antwort 2 „der Zeilen-Toggle bleibt wie heute" trifft ins
Leere, weil auf den fuenf Standardlisten kein `IsDoneBde`-Zeilen-Toggle existiert, den man behalten
koennte.

**Konkrete Frage an den Menschen:** Auf welcher konkreten View erscheinen (a) der einzelne
`IsDoneBde`-Zeilen-Toggle (Grundregel „Sub-FA fertigmelden wirkt nur auf diesen einen") und (b) die
neue Gruppen-Kopfzeilen-Aktion „Alle Sub-FAs fertigmelden"? Die drei plausiblen Auswege schliessen
sich gegenseitig aus:
- **Auf `PickingLeitstand`** → widerspricht direkt Antwort 3 („Kaskade NEIN" dort).
- **Auf `ProductionOrders` (FA-Liste)** → dann sind sowohl der `IsDoneBde`-**Zeilen**-Toggle als auch
  die Gruppen-Kaskade dort **neu** (heute nur Badge). Dann ist Antwort 2s „bleibt exakt wie heute"
  faktisch falsch und muss umformuliert werden; ausserdem ist das eine echte neue Aktion auf einer
  bislang aktionsfreien Liste.
- **Auf dem BDE-Cockpit** → widerspricht Antwort 4 (ausgeklammert) und Abschnitt 8 (kein
  Tabellen-Layout).

Ohne diese Festlegung raet der Dev-Lauf die Wirt-View — genau der teure Fehler, den diese Pruefung
verhindern soll. (AK 11–14 haengen alle daran.)

### SOLLTE — macht den Dev-Lauf sicherer

**S1 — AK 10 / F6 (AKE-Regression) deckt die neue Aktion aus Antwort 2 nicht ab.**
Der zur Antwort 2 vom Menschen gestellte Regressionsgedanke ist berechtigt: Die Gruppen-Kopfzeile
bekommt ein Bedienelement, das die flache Liste nicht hat. Technisch ist das **konsistent** mit F6,
weil bei Master `false` keine Gruppen-Kopfzeilen gerendert werden — also auch kein neuer Knopf. Aber
AK 10 zaehlt heute nur „keine Gruppierung, keine neu sichtbare Spalte, unveraenderte Suche,
unveraenderte Paginierung" auf — **die neue Kaskade-Aktion fehlt in der Aufzaehlung.** Vorschlag: AK
10 und Abschnitt 9 explizit ergaenzen um „… und **keine neue Aktion / kein neues Bedienelement**
sichtbar (weder in einer Zeile noch als Kopfzeile), da bei Master `false` keine Gruppen-Kopfzeile
existiert." Damit wird der Regressions-Test scharf pruefbar statt implizit.

**S2 — Abschnitt 7 und die Per-View-Tabelle (Abschnitt 8) widersprechen jetzt Antwort 1 und muessen
nachgezogen werden.**
Antwort 1 schliesst `IsDonePicking`/Kommissionierung ausdruecklich von der Kaskade aus („Kommissionierung
darf nicht kaskadieren"). Der Spec-**Text** listet `Picking`/`IsDonePicking` aber weiterhin als
„Kaskade-Kandidat 1" (Abschnitt 8, Zeile ~248) und fuehrt beide Toggle als gleichrangige Kandidaten
(Abschnitt 7). Das ist nach der Entscheidung **veraltet** und wird den Dev-Lauf in die Irre fuehren.
Vorschlag: Abschnitt 7/8 auf den Stand von Antwort 1 bringen — `IsDoneBde` ist der **einzige**
Kaskade-Traeger, `IsDonePicking`/`Picking` traegt nur den Anzeige-Teil (Gruppierung/Suche/Spalten),
keine Kaskade. (Haengt mit BL1 zusammen: erst wenn die Wirt-View feststeht, ist die Tabelle
widerspruchsfrei fuellbar.)

**S3 — Antwort 6 ist erfuellbar, aber die konkrete Abfrage existiert noch nicht und braucht zwei
Praezisierungen.**
Verifiziert: Das Muster „laufende, nicht beendete BDE-Buchung" existiert im Code —
`BdeBookingRepository.GetActiveForWorkOperationAsync` filtert `EndedAt == null && !IsCancelled`
(`Data/Repositories/BdeBookingRepository.cs:37,43`), ebenso `GetActiveCockpitAsync` (Zeile 55-62).
Der Join-Weg zur Sub-FA steht ebenfalls: `WorkOperation.ProductionOrderId`
(`Models/WorkOperation.cs:7`). Antwort 6 ist damit **kein Blocker**. Aber:
- Eine fertige Aggregation „hat diese Menge von `ProductionOrder`-Ids eine aktive Buchung?" gibt es
  **nicht** — sie ist als neue Repository-Abfrage zu bauen (BdeBooking → WorkOperation →
  ProductionOrder). Als konkreten Umsetzungshinweis in die Spec aufnehmen, damit der Dev-Lauf nichts
  erfindet.
- **Statusabgrenzung offen:** `BdeBookingStatus` kennt `Running`, `Paused`, `Finished`, `Resumed`,
  `AutoPaused` (`Models/BdeBookingStatus.cs`). `Paused`/`AutoPaused` haben ebenfalls `EndedAt == null`.
  Antwort 6 fragt „Arbeitet gerade jemand daran?" — eine pausierte Buchung ist begonnen, aber nicht
  aktiv. Kurz festlegen, ob „laufend" nur `Running`/`Resumed` meint oder jede nicht-beendete Buchung
  (so wie `GetActiveCockpitAsync` es heute zaehlt). Sonst zaehlt der Dialog je nach Auslegung anders.

### HINWEIS — Beobachtung ohne Handlungszwang

**H1 — Z1 (der als „teuerster Fund" markierte Check) ist am Code bereits entschaerft — zugunsten der
Spec.** Verifiziert: `FaMaterializationSyncService`/`FaMaterializationPlanner` enthalten **kein**
`SetValues`/`CurrentValues`/`Entry(...)` (grep leer). Der Update-Pfad setzt ausschliesslich
**einzeln** die Quellfelder (`SageMissingSince`, `Quantity`, `ArticleNumber`, `Description1/2`,
`ParentSubOrderNumber` + Audit — `FaMaterializationSyncService.cs:138-155`), und der Klassenkommentar
haelt ausdruecklich fest: „App-verwaltete Felder (IsDone/PickingStatus/BdeStatus/Workplace/Storno/
ExtraInfo) werden bei Updates NIE ueberschrieben" (Zeile 21-22). Zusaetzlich liegt der
Kaskade-Traeger `IsDoneBde` auf der **separaten** Entitaet `ProductionOrderBdeStatus`, die der Sync
gar nicht laedt. **Der von Z1 gefuerchtete stille Ruecksetzer kann mit dem heutigen Code nicht
auftreten.** Zwei kleine Korrekturen an Z1: (1) die Formulierung „Die Kaskade schreibt ein Status-Feld
an `ProductionOrders`" ist ungenau — sie schreibt an `ProductionOrderBdeStatus`; (2) der „erste
Handgriff dieser Spec" (Update-Pfad pruefen) ist faktisch schon erledigt und gruen. Die von Z1
verlangte **Invariante als Test** trotzdem behalten — als Regressionsschutz gegen einen kuenftigen
Umbau des Sync, nicht als offene Sorge. Die Dringlichkeit „[KRITISCH]" darf auf „bereits erfuellt,
Test als Absicherung" heruntergestuft werden.

**H2 — Z3 („neuer Sub-FA unter fertigem Haupt-FA") ist am Anlege-Pfad konsistent.** Verifiziert: der
Sync legt neue Orders mit `IsDone = false` an (`FaMaterializationSyncService.cs:129`) — genau das von
Z3 geforderte „offen anlegen". Kein Handlungsbedarf, nur Bestaetigung.

### Empfehlung

**NACHBESSERUNG NOETIG: BL1 — die Kaskade-Aktion (`IsDoneBde`) hat nach den getroffenen Antworten
1/2/3/4 keine widerspruchsfreie Wirt-View; der Mensch muss festlegen, auf welcher View der
Zeilen-Toggle und die Gruppen-Kaskade erscheinen.** (S1–S3 sind danach schnell nachziehbar; Z1 ist
entgegen seiner Kennzeichnung bereits am Code abgesichert.)

## ANTWORTEN auf die Kritische Pruefung (2026-08-20)

### Zu BL1 — Blocker berechtigt. **Antwort 3 war zu breit formuliert; sie wird praezisiert.**

Der Widerspruch entstand in meiner Antwort 3, nicht in 1/2/4. Ich hatte den Leitstand pauschal auf
„Kaskade NEIN" gesetzt — gedacht war damit **`BulkRelease`**, das tatsaechlich eine Freigabe ist und
nicht kaskadieren darf. Dass der Leitstand **zugleich der einzige Ort mit dem
`IsDoneBde`-Zeilen-Toggle** ist, war mir nicht bekannt. Der Reviewer hat das am Code belegt.

**Praezisierte Antwort 3 — verbindlich:**

| Auf dem Leitstand | Verhalten |
|---|---|
| Zwei-Ebenen-Gruppierung | **ja**, wie die fuenf Standardlisten |
| `BulkRelease` (Freigabe) | **keine Kaskade**, bleibt zeilenbasiert und unveraendert |
| **`IsDoneBde`-Zeilen-Toggle** | **bleibt exakt wie heute** — symmetrisch, nur die eigene Zeile |
| **Gruppen-Aktion „Alle Sub-FAs fertigmelden"** | **hier zu Hause** — in der Gruppen-Kopfzeile, mit Bestaetigungsdialog (Anforderung B) |

Damit hat die Kaskade genau **einen** Ort, und die Antworten 1, 2 und 4 bleiben unveraendert gueltig:
Traeger ist `IsDoneBde` (1), sie ist eine eigene Gruppen-Aktion statt eines aufgespaltenen Toggles (2),
und das BDE-Cockpit bleibt ausgeklammert (4).

**Folge fuer die fuenf Standardlisten:** Sie exponieren `IsDoneBde` nicht und bekommen deshalb
**ausschliesslich den Anzeige-Teil** — Gruppierung, Spalte, Suche, Paginierung, Badge. Keine
Kaskade, kein neues Bedienelement. Das ist kein Mangel, sondern die richtige Abgrenzung: Wo heute
keine Fertigmeldung stattfindet, wird auch keine eingefuehrt.

### Zu Z1 — herabgestuft und praezisiert. Der Reviewer hat recht, und zwar in beiden Punkten.

`FaMaterializationSyncService`/`Planner` setzen die Quellfelder **einzeln**; kein `SetValues`, kein
Entitaets-Ersatz. Der Klassenkommentar haelt es sogar ausdruecklich fest. Zusaetzlich liegt
`IsDoneBde` auf der **separaten Entitaet `ProductionOrderBdeStatus`**, die der Sync gar nicht laedt —
doppelt abgesichert.

- **Kennzeichnung von „KRITISCH" auf „bereits erfuellt" herabstufen.** Was bleibt, ist ein
  **Regressionstest**, der die Eigenschaft festnagelt (fertigmelden → Sync → Status unveraendert) —
  damit sie nicht bei einer spaeteren Umstellung des Update-Pfads unbemerkt verlorengeht.
- **Wortlaut korrigieren:** Die Kaskade schreibt an `ProductionOrderBdeStatus`, **nicht** an
  `ProductionOrders`. Meine Formulierung war ungenau.
- Der „erste Handgriff dieser Spec" entfaellt — die Pruefung ist erledigt.

### Zu Antwort 6 — praezisiert: **jede nicht beendete Buchung**, nicht nur die aktiven.

Die Rueckfrage ist berechtigt: `Paused`/`AutoPaused` haben ebenfalls `EndedAt == null`.
**Gezaehlt wird jede nicht beendete, nicht stornierte Buchung** — also auch pausierte.
Begruendung: Der Dialog warnt nicht davor, jemanden in diesem Moment zu stoeren, sondern davor,
**unfertige Arbeit als fertig zu erklaeren**. Eine pausierte Buchung ist genau das — besonders
`AutoPaused` (Schichtende, Arbeit geht morgen weiter).

**Folge fuer den Text im Dialog:** nicht „wird gerade bearbeitet", sondern **„mit offener Buchung"**.
Die Formulierung muss zum Gezaehlten passen, sonst wundert sich der Anwender ueber eine Warnung fuer
einen Auftrag, an dem sichtbar niemand arbeitet.
Die Aggregation („hat dieser Sub-FA eine offene Buchung?") ist neu zu bauen — das Muster
(`EndedAt == null && !IsCancelled`) und der Join-Weg (`WorkOperation.ProductionOrderId`) existieren.

### Zu S1 — uebernommen.

AK 10/F6 wird ausdruecklich um **„kein neues Bedienelement, keine neue Aktion"** ergaenzt. Technisch
ist es gedeckt (bei Master `false` rendert keine Gruppen-Kopfzeile, also auch kein Gruppen-Knopf) —
aber die Regressionsbedingung soll das **behaupten und pruefen**, nicht bloss zufaellig erfuellen.

### Zu S2 — uebernommen.

In den Abschnitten 7 und 8 ist `Picking`/`IsDonePicking` **nicht mehr als Kaskade-Kandidat** zu
fuehren — Antwort 1 hat das entschieden. In der Tabelle in Abschnitt 8 steht dort kuenftig
„keine Kaskade (Kommissionierung kaskadiert bewusst nicht — eigenes Material je Sub-FA)".

### Zu S3 und den uebrigen Bestaetigungen — uebernommen bzw. zur Kenntnis.

Keine Einwaende; die Punkte sind in der Nachbesserung mitzuziehen.

**Damit ist BL1 aufgeloest. Die Spec kann nachgebessert und anschliessend freigegeben werden.**
