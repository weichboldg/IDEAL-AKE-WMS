---
type: spec
title: "IDEAL Kommissionierliste: Summierung je Artikel + Kommissionierziel, PDF nur gefiltert + nur sichtbare Spalten"
slug: 2026-09-25-kommissionierliste-summierung-pdf-spec
status: Entwurf
created: 2026-09-25
updated: 2026-09-25
source_backlog: "[[2026-09-23-kommissionierliste-summierung-pdf]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Services/KommissionierListenService.cs Z. 139-201 (`AggregateSummiert`) — KW-Pflicht (Z. 151-153, Nachtrag AK N2d) auf Rueckfrage 2 abhaengig lockern: ohne KW-Bereich alle HauptFA einschliessen statt leeres Ergebnis. Aggregationsschluessel `(HauptFA, Artnr, Kommissionieren)` (Z. 168-172) UNVERAENDERT — bereits Falle 1 + Falle 3 konform, siehe Fachliche Anforderungen."
  - "IdealAkeWms/Controllers/FaHierarchyKommissionierListenController.cs Z. 130-182 (`Print`, `Pdf`) — Spalten-Sichtbarkeit ergaenzen: `IUserViewPreferenceRepository` injizieren, `_user.GetCurrentAppUserId()` + `GetByUserAndViewAsync(userId, \"FaHierarchyKommissionierListen\")` lesen, `WarehousePickingPrintLayout.ResolveColumns(prefs, ColumnDefinitions.FaHierarchyKommissionierListen.Columns)` aufrufen, Ergebnis als Key-Liste ins ViewModel (`VisibleColumns`)."
  - "IdealAkeWms/Controllers/FaHierarchyKommissionierListenController.cs (NEU) — Actions `PrintSummiert(string? target, string? kwVon, string? kwBis)` und `PdfSummiert(int hauptFa, string? target, string? kwVon, string? kwBis)`, analog `Print`/`Pdf`, aber auf `KommissionierListenService.AggregateSummiert`-Ergebnis (nach HauptFA gruppiert fuer den Ausdruck), Spalten-Sichtbarkeit ueber `ColumnDefinitions.FaHierarchyKommissionierSummiert`."
  - "IdealAkeWms/Services/WarehousePickingPrintLayout.cs Z. 40-65 (`ResolveColumns`) — neue Overload `ResolveColumns(PrintPrefs? prefs, IReadOnlyList<ColumnDef> defs)`; bestehende `ResolveColumns(PrintPrefs?)` delegiert mit `ColumnDefinitions.WarehousePickingDetails.Columns` (kein Verhaltenswechsel fuer den bestehenden Aufrufer `WarehousePickingController`). Wiederverwendung statt einer zweiten, fast identischen Klasse (ponytail Sprosse 2) — bewusst KEIN Umbenennen der Klasse in dieser Spec (kleinerer Diff; Namens-Unschaerfe als `ponytail:`-Kommentar markieren, Umbenennen erst bei einem dritten Konsumenten)."
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs Z. 328-345 (`FaHierarchyKommissionierListen`) UNVERAENDERT (bereits vollstaendig registriert, deckt AK zu Spalten-Sichtbarkeit ab) — NEU: `FaHierarchyKommissionierSummiert`-Eintrag (4 Spalten: hauptfa[locked]/artnr/kommissionieren/sollmenge, identisch zu `Summiert.cshtml` `#column-config`) + Registrierung in `GetByViewKey` (Z. ~445-456). Behebt nebenbei einen vorbestehenden Fund (nicht Gegenstand des Backlogs, aber Voraussetzung fuer AK 8): ohne diese Registrierung lehnt `UserViewPreferencesApiController` das Speichern/Lesen von Spalten-Praeferenzen fuer die Summiert-Ansicht bereits heute ab (viewKey-Validierung, `fallstricke.md` §10)."
  - "IdealAkeWms/Views/FaHierarchyKommissionierListen/Print.cshtml — Spaltenbloecke (Kopf Z. 106-117, Zeile Z. 120-134) hinter `ShowCol(key)` (Vorbild `Views/Picking/PrintBom.cshtml` Z. 196-260), `colCount` dynamisch aus sichtbaren Spalten statt der festen Konstante 11 (Index.cshtml Z. 10 bleibt unveraendert — betrifft nur den Druck)."
  - "IdealAkeWms/Views/FaHierarchyKommissionierListen/PrintSummiert.cshtml (NEU) — analoges Layout zu `Print.cshtml` (je HauptFA-Gruppe eigener `page-break-after`-Block, Barcode = HauptFA, `ShowCol` ueber die 4 Summiert-Spalten), zusaetzlich ein `Summiert-Banner` analog dem bestehenden `filter-banner` (\"Summierte Ansicht\" statt/neben \"Gefilterte Ansicht\")."
  - "IdealAkeWms/Views/FaHierarchyKommissionierListen/Summiert.cshtml — Drucken-Link + PDF-Knoepfe je Gruppen-Kopfzeile ergaenzen, analog `Index.cshtml` Z. 15-19 + Z. 94-101 (dafuer muss die Ansicht wie `Index.cshtml` nach HauptFA gruppiert rendern; heute ist sie eine flache Tabelle ohne Gruppen-Kopfzeile — kleinste Aenderung: `@foreach` gruppieren wie `Print.cshtml`, ODER nur EIN seitenweiter Drucken-Link ohne Gruppen-PDF, siehe Rueckfrage 4)."
  - "IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierGruppeViewModel.cs (`FaHierarchyKommissionierListViewModel`, `FaHierarchyKommissionierPrintViewModel`) — neues Feld `IReadOnlyList<string> VisibleColumns` (leer = alle sichtbar, Konvention identisch `BomViewModels.VisibleColumns`)."
  - "IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierSummiertViewModel.cs — neues Feld `IReadOnlyList<string> VisibleColumns` an einem neuen `FaHierarchyKommissionierSummiertPrintViewModel` (analog `FaHierarchyKommissionierPrintViewModel`, gruppiert nach HauptFA fuer den Druck) + `IsSummiert`/`IsFiltered`-Flags fuer den Druckkopf."
  - "secondbrain/architektur/fallstricke.md — neuer Eintrag: `FaHierarchyNode` hat kein Mengeneinheit-Feld (Sage-View liefert keine Einheit) — die Kommissionier-Summierung kann unterschiedliche Einheiten technisch nicht erkennen; dokumentierte Wissensluecke, siehe Rueckfrage 3."
  - "docs/TESTSZENARIEN.md (neues Kapitel, naechste freie Nummer zum Umsetzungszeitpunkt) + secondbrain/tests/testszenarien-index.md."
  - "IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs (Version) + Views/Help/Changelog.cshtml (neuer Eintrag)."
open_questions:
  - "Summierschluessel bestaetigen: der bereits gebaute Schluessel (HauptFA, Artikelnummer, Kommissionierziel) aus der bestehenden `/Summiert`-Ansicht — uebernehmen, oder soll stattdessen je Artikel EINE Zeile mit Ziel-Aufschluesselung entstehen?"
  - "Toggle Einzeln/Summiert: bestehende Nav-Pills-Umschaltung (Index=Liste/Summiert) als Loesung ausreichend, und soll der Standard beim Aufruf ohne Parameter kuenftig auf Summiert stehen (Backlog-Einschaetzung), oder bleibt Liste der Standard?"
  - "Mengeneinheit (Falle 2) laesst sich am Code nicht pruefen — `FaHierarchyNode` hat kein Einheit-Feld, die zugrundeliegende Sage-View liefert keines. Die Pflicht 'nur gleiche Einheiten summieren' kann damit heute technisch NICHT umgesetzt werden. Wird das als dokumentierte, bekannte Wissensluecke akzeptiert (Sollmenge wird ungeprueft summiert), oder ist ein Einheit-Feld an `FaHierarchyNode`/der Sage-View ein Vorbedingungs-Backlog-Punkt, der VOR dieser Spec zu klaeren ist?"
  - "Die bestehende `/Summiert`-Ansicht liefert ohne Kalenderwoche bewusst ein leeres Ergebnis (Nachtrag AK N2d, bereits freigegeben). Soll diese Pflicht fuer den allgemeinen Kommissionier-Alltag gelockert werden (ohne KW: alle HauptFA einschliessen, KW bleibt optionaler Zusatzfilter — analog Vormontage-Sicht 2), oder bleibt die KW-Pflicht bestehen und der neue Toggle bekommt eine dritte, von der Kalenderwoche unabhaengige Ansicht?"
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

> [!info] Umsetzungsort (Hinweis, keine Frontmatter-Vorgabe)
> Der gesamte betroffene Code (`FaHierarchyKommissionierListenController`, `KommissionierListenService`,
> `FaHierarchyVormontageController`, `PdfRenderService`, `Views/Picking/PrintBom.cshtml`,
> `WarehousePickingPrintLayout`, `ColumnDefinitions`) existiert **nur** im nicht gemergten
> Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch
> `feature/2026-08-07-ideal-teile-1-5`). Stand bei Spec-Erstellung (2026-09-25, am Worktree
> verifiziert): `AppVersion 1.45.0`, höchstes Testszenarien-Kapitel `TS-78`, höchste SQL-Nummer `93`.
> Im `main`-Checkout, in dem dieser Spec-Agent-Lauf ausgeführt wurde, existiert davon **nichts**. Die
> Umsetzung erfolgt dort als weitere Etappe desselben Bündels — kein neuer Worktree. Der Dev-Lauf
> verifiziert AppVersion/TS-Nummer/SQL-Nummer erneut gegen den dann aktuellen Worktree-Stand.
>
> **Überschneidung mit [[2026-09-25-kommissionierung-nur-hauptfa-spec]] (parallel, Status
> `InUmsetzung`, gleicher Worktree):** Diese Spec extrahiert `KommissionierListenService.BuildFlagPredicate`
> (Z. 228-236) in eine geteilte `KommissionierRelevanzFilter.IsRelevant`. Die vorliegende Spec ändert
> **andere** Methoden derselben Datei (`AggregateSummiert`, `BuildSummiertAsync`) sowie den Controller,
> die Print-Views und `ColumnDefinitions` — keine Zeilenüberschneidung, aber dieselbe Datei/derselbe
> Worktree/derselbe Branch. **Reihenfolge:** wer zuerst umgesetzt wird, vergibt Versionsnummer und
> Testszenarien-Kapitelnummer; der zweite Dev-Lauf prüft `AppVersion.cs`/`docs/TESTSZENARIEN.md`
> gegen den dann aktuellen Stand, bevor er sie festschreibt (wie im Info-Kasten oben beschrieben).

## Ziel / Nutzen (das Warum)

[[2026-09-23-kommissionierliste-summierung-pdf]] verlangt zwei fachlich verwandte Verbesserungen an
der IDEAL-Kommissionierliste (Teil 3, `FaHierarchyKommissionierListenController`):

1. Kommt in einer Kommissionierliste dieselbe Artikelnummer mehrfach vor, soll eine **summierte**
   Zeile statt mehrerer Einzelzeilen möglich sein — der Kommissionierer holt eine Menge statt
   mehrfach dieselbe Lagerposition anzufahren.
2. Der Druck/PDF einer Kommissionierliste soll **nur** die aktuell gefilterten Zeilen und **nur**
   die für den jeweiligen Anwender sichtbaren Spalten enthalten — ein Ausdruck, der mehr zeigt als
   der Bildschirm, führt dazu, dass jemand nach der Gesamtmenge kommissioniert, obwohl nur ein
   Teil gebraucht wird (bzw. umgekehrt eine im Bildschirm ausgeblendete Spalte auf Papier fehlt und
   für eine Information gehalten wird, die es nicht gibt).

**Zentraler Befund dieser Spec (am Code erhoben, nicht angenommen):** Der Backlog geht davon aus,
dass die Summierung neu gebaut werden muss. Tatsächlich existiert seit der UAT-Anpassung vom
2026-08-13 (Etappe 8, v1.31.0, siehe [[2026-07-29-standort-ideal-teil-3-spec]]) bereits eine
**`/Summiert`-Ansicht** genau für die Kommissionierliste
(`FaHierarchyKommissionierListenController.Summiert`,
`KommissionierListenService.AggregateSummiert`). Sie aggregiert je
**`(HauptFA, Artnr, Kommissionieren)`** und summiert **nur `Sollmenge`** — das ist bereits exakt
Falle 1 (Kommissionierziel bleibt als Aggregationsschlüssel erhalten) und Falle 3 (Aggregation über
die Artikelnummer `Artnr`, nicht über `Matchcode`) aus dem Backlog, wortwörtlich im Code
dokumentiert (`FaHierarchyKommissionierSummiertRowViewModel`, Doc-Kommentar: „Der Matchcode ist der
Aggregations-Schlüssel (nicht die Artnr)" — nein, das gilt nur für die **Vormontage**-Summe;
die Kommissionierliste hat von Anfang an korrekt auf `Artnr` aggregiert). Es gibt bereits eine
Nav-Pills-Umschaltung „Liste" / „Summiert" — die vom Backlog vorgeschlagene Vormontage-Vorlage
(Teil 5, `Index`/`Summiert`) ist also für die Kommissionierliste **selbst schon** angewendet worden,
nicht erst zu übernehmen.

**Was tatsächlich fehlt** (am Code verifiziert, siehe Technischer Lösungsentwurf):

- Die bestehende Summiert-Ansicht ist an eine **Pflicht-Kalenderwoche** gebunden (ohne KW-Eingabe
  liefert sie bewusst ein leeres Ergebnis, Nachtrag „AK N2d") — sie dient heute einer
  wochenbezogenen Vorschau, nicht dem alltäglichen Kommissionier-Werkzeug „dieselbe Liste, aber
  zusammengefasst", das der Backlog beschreibt.
- Weder `Index`/`Print`/`Pdf` (Liste) noch `Summiert` tragen die **Spalten-Sichtbarkeit** des
  Anwenders (Zahnrad/`column-preferences.js`) in den Druck/PDF — der bestehende
  `Pdf`/`Print`-Weg rendert immer alle 10 Spalten hart codiert.
- Für die Summiert-Ansicht gibt es **gar keinen** Druck-/PDF-Weg.
- Falle 2 (nur gleiche Mengeneinheiten summieren) lässt sich am Datenmodell nicht umsetzen — dazu
  mehr unter Fachliche Anforderungen / Rückfrage 3.

Der **Filter-Durchschlag im PDF ist für die Liste bereits vorhanden** (siehe Beleg unten) — die im
Backlog offene Frage 3 wird damit beantwortet, ohne eine Rückfrage zu benötigen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

1. **Summierschlüssel bestätigen und wiederverwenden:** kein neuer Aggregationsmechanismus — die
   bestehende `KommissionierListenService.AggregateSummiert`-Logik `(HauptFA, Artnr, Kommissionieren)`
   bleibt die einzige Kommissionier-Summiermechanik (Rückfrage 1).
2. **KW-Pflicht der Summiert-Ansicht überprüfen/lockern**, damit sie als alltägliches
   Sammel-Werkzeug taugt (Rückfrage 4) — technisch eine einzeilige Änderung
   (`AggregateSummiert`, Schritt 1), fachlich eine Rücknahme einer bereits freigegebenen
   Entscheidung (AK N2d), deshalb Rückfrage statt Annahme.
3. **Spalten-Sichtbarkeit in Druck/PDF der Liste** (`Index`/`Print`/`Pdf`): serverseitiges Lesen der
   `UserViewPreference` für den View-Key `FaHierarchyKommissionierListen`, Muster
   `WarehousePickingPrintLayout` (siehe Technischer Lösungsentwurf) — **kein** JS-Query-Parameter-Weg
   wie bei `PrintBom`, weil die bestehenden Drucken-/PDF-Links serverseitig gerenderte `<a href>`
   ohne Klick-Handler sind (kein DOM-Scan zur Klickzeit nötig oder sinnvoll).
4. **Druck/PDF für die Summiert-Ansicht neu bauen** (`PrintSummiert`/`PdfSummiert`), inkl. Kopf-Hinweis
   „Summierte Ansicht" analog dem bestehenden „Gefilterte Ansicht"-Banner, ebenfalls mit
   Spalten-Sichtbarkeit über einen neuen View-Key `FaHierarchyKommissionierSummiert` in
   `ColumnDefinitions`.
5. **Falle 2 dokumentieren statt stillschweigend ignorieren:** `FaHierarchyNode` besitzt kein
   Mengeneinheit-Feld (verifiziert, siehe unten) — die Anforderung „nur gleiche Einheiten summieren"
   ist damit heute nicht prüfbar. Diese Spec baut **keine** Einheiten-Prüfung (es gibt nichts zu
   prüfen), dokumentiert die Lücke sichtbar (Fallstrick-Eintrag) und legt die fachliche Tragweite der
   Rückfrage 3 dem Menschen vor.
6. **Nebenbefund beheben:** `FaHierarchyKommissionierSummiert` fehlt in
   `ColumnDefinitions.GetByViewKey` — ohne diese Registrierung könnten Anwender für die
   Summiert-Ansicht heute schon keine Spalten-Präferenz speichern (Voraussetzung für Punkt 4).

**Out-of-Scope**

- **Vormontage (Teil 5) Druck/PDF.** Hat weiterhin keine Bildschirmdruck-Action (nur `Index` und
  `Summiert` als Listen-Views). Bereits in
  [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]] (Rückfrage 9) entschieden: „bleibt außen vor,
  bis Teil 5 selbst einen Bildschirmdruck bekommt" — kein Anlass, das hier zu öffnen.
- **`KommissionierRelevanzFilter`-Extraktion** aus `BuildFlagPredicate` — Gegenstand der parallelen
  Spec [[2026-09-25-kommissionierung-nur-hauptfa-spec]] (siehe Info-Kasten oben zur Reihenfolge).
- **Ein Einheit-Feld an `FaHierarchyNode`/der Sage-View ergänzen.** Wäre eine Schema-/Sync-Änderung
  (Migration, `FaHierarchySyncService`, Sage-View-Erweiterung) und damit ein eigener, größerer
  Backlog-Punkt — diese Spec erfindet dieses Feld nicht, sondern legt die Entscheidung offen
  (Rückfrage 3).
- **Beschichtungsauftrag (Teil 4).** Hat bereits Druck/PDF (siehe
  [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]]), aber keine Summiert-Ansicht und ist nicht
  Gegenstand des Backlogs.
- **Standorteinstellungen-Schalter** für Aggregations- oder Anzeigeverhalten — nicht verlangt.

## Beleg: Filter-Durchschlag im PDF der Liste funktioniert bereits (Backlog-Frage 3)

Verifiziert am Code (`.claude/worktrees/2026-08-07-ideal-teile-1-5`):

- `FaHierarchyKommissionierListenController.Print`/`Pdf` (Z. 130-182) lesen
  `ColumnFilterHelper.ReadFromQuery(HttpContext?.Request)` und rufen
  `_service.BuildAsync(target, columnFilters, 1, int.MaxValue, paginate: false)` — **dieselbe**
  gefilterte Menge wie der Bildschirm, nur ohne Paging.
- `FaHierarchyKommissionierPrintViewModel.IsFiltered` (Z. 45-47) wird aus
  `ColumnFilterHelper.HasAny(columnFilters) || !string.IsNullOrEmpty(target)` berechnet und in
  `Print.cshtml` (Z. 51-57) als **„Gefilterte Ansicht"**-Banner mit dem gewählten Kommissionier-Ziel
  angezeigt.
- Der Drucken-Link in `Index.cshtml` (Z. 7, 16-18) hängt `Context.Request.QueryString.ToString()` an
  — Spaltenfilter (`colf_*`) und `target` werden 1:1 mitgegeben.

Backlog-Frage 3 ist damit beantwortet: **der Filter-Durchschlag greift bereits**, für Spaltenfilter
UND das Ziel-Dropdown. Was **nicht** durchschlägt, ist die Spalten-**Sichtbarkeit** (siehe nächster
Abschnitt) — das ist die tatsächliche Lücke, nicht der Filter.

## Fachliche Anforderungen

### 1 — Summierschlüssel (bestehend, zu bestätigen)

Aggregiert wird je `(HauptFA, Artnr, Kommissionieren)`; Summe nur über `Sollmenge`. Ein Artikel mit
zwei unterschiedlichen Kommissionierzielen bleibt zwei Zeilen (Falle 1). Zwei Artikelnummern mit
gleichem `Matchcode` bleiben zwei Zeilen, weil über `Artnr` aggregiert wird, nicht über `Matchcode`
(Falle 3). Dieser Schlüssel existiert bereits unverändert seit v1.31.0 — siehe Rückfrage 1 zur
Bestätigung.

### 2 — Mengeneinheit (Falle 2) — technisch nicht prüfbar, keine Erfindung eines Feldes

`FaHierarchyNode` (Projektion der Sage-View `vw_IDEAL-AKE_Kommissionierung_FAListe`) hat **kein**
Mengeneinheit-Feld — verifiziert durch vollständiges Durchsuchen der Modellklasse
(`IdealAkeWms/Models/FaHierarchyNode.cs`, 19 Felder, keines davon eine Einheit). Die Sollmenge ist
eine reine `decimal`-Zahl ohne begleitende Einheitsangabe. Eine Prüfung „nur gleiche Einheiten
summieren" kann damit **nicht** implementiert werden, weil es keine Einheit gibt, die man
vergleichen könnte — nicht, weil die Prüfung vergessen würde. Diese Spec:

- baut **keine** Einheiten-Prüfung (es gäbe nichts zu prüfen, ein Schein-Code, der immer „gleich"
  zurückgibt, wäre ponytail-widrig — Attrappe statt Funktion),
- dokumentiert die Lücke sichtbar in `fallstricke.md`, damit sie nicht als „geprüft und für gut
  befunden" missverstanden wird,
- legt die fachliche Tragweite dem Menschen vor (Rückfrage 3): Wird das Fehlen einer Einheit als
  bekanntes, akzeptiertes Risiko hingenommen, oder ist ein Einheit-Feld eine Vorbedingung, bevor
  überhaupt summiert werden darf?

### 3 — Toggle Liste/Summiert (bestehend)

Die Nav-Pills-Umschaltung „Liste" / „Summiert" existiert bereits in `Index.cshtml` (Z. 22-30) und
`Summiert.cshtml` (Z. 13-21), inklusive Erhalt des `target`-Filters beim Wechsel. Offen ist nur, ob
`Index` (Liste) weiterhin der Standard beim Aufruf ohne Parameter bleibt, oder ob künftig `Summiert`
Standard sein soll (Rückfrage 2) — verknüpft mit Rückfrage 4 (KW-Pflicht), weil ein Default auf eine
Ansicht, die ohne weitere Eingabe leer bleibt, für den Anwender verwirrend wäre.

### 4 — PDF: nur gefilterte Zeilen (bereits erfüllt), nur sichtbare Spalten (neu)

- **Gefiltert:** siehe Beleg oben — keine Änderung nötig.
- **Sichtbare Spalten:** Druck/PDF sollen genau die Spalten zeigen, die der aufrufende Anwender
  gerade über sein Zahnrad (`column-preferences.js`, View-Key `FaHierarchyKommissionierListen` bzw.
  neu `FaHierarchyKommissionierSummiert`) eingestellt hat — nicht mehr, nicht weniger.
- **Konsequenz, die zu dokumentieren ist (bereits vom Backlog benannt, hier bestätigt):** Da die
  Spalten-Sichtbarkeit je Anwender in der Datenbank (`UserViewPreference`) gespeichert ist, erzeugen
  zwei Anwender für denselben HauptFA möglicherweise unterschiedliche PDFs. Für ein internes
  Arbeitsdokument ist das gewollt (das PDF zeigt „was dieser Anwender sieht") — dieselbe
  Randnotiz, die der Backlog bereits für den Fall macht, dass die Liste dereinst an Dritte ginge.

### 5 — Summiert-Ansicht drucken + kennzeichnen

Ein Druck/PDF der Summiert-Ansicht enthält die Aggregatzeilen (nicht die Einzelpositionen) und
weist das im Dokumentkopf **sichtbar** als „Summierte Ansicht" aus — analog zum bestehenden
„Gefilterte Ansicht"-Banner, damit niemand einen summierten Ausdruck für eine vollständige
Positionsliste hält (umgekehrter Fehler zum Filter-Banner, gleiches Prinzip: melden statt still
verändern).

## Technischer Lösungsentwurf

### A — KW-Pflicht der Summiert-Ansicht (abhängig von Rückfrage 4)

`KommissionierListenService.AggregateSummiert` Schritt (1) liefert heute ohne `kwRange` ein leeres
Ergebnis (Z. 151-153). Wird Rückfrage 4 mit „lockern" beantwortet, entfällt dieser Frühausstieg;
ohne `kwRange` werden **alle** HauptFA einbezogen (Verhalten identisch zu Vormontage-Sicht 2,
`VormontageService.AggregateSummiert`, die genau so funktioniert — kein Sonderfall, sondern
Angleichung an das bereits etablierte Schwestermuster). Der KW-Filter bleibt als **optionale**
zusätzliche Einschränkung erhalten (Filterkarte unverändert). Wird Rückfrage 4 mit „KW-Pflicht
bleibt" beantwortet, entfällt dieser Punkt vollständig und die neue Summiert-Ansicht für den
Alltagsgebrauch wäre ein dritter, eigener Endpunkt (Mehraufwand, in der Rückfrage benannt).

### B — Spalten-Sichtbarkeit in Druck/PDF: `WarehousePickingPrintLayout`-Muster statt `PrintBom`-JS

Zwei Vorbilder existieren im Haus für „Druck zeigt nur sichtbare Spalten":

1. **`PrintBom`/`Bom.cshtml`:** JavaScript liest beim Klick auf den Drucken-Knopf die aktuell
   sichtbaren `<th data-col-key>` aus dem DOM aus, hängt sie als `visibleColumns`-Query-Parameter an
   eine `window.open(...)`-URL. Passt zu `Bom.cshtml`, weil dort ohnehin ein JS-Click-Handler die
   Druck-URL zusammenbaut (Filter, sichtbare Positionen).
2. **`WarehousePickingController.Print`/`WarehousePickingPrintLayout`:** liest die
   `UserViewPreference` (View-Key `WarehousePickingDetails`) **serverseitig** direkt aus der
   Datenbank (`IUserViewPreferenceRepository.GetByUserAndViewAsync`), parst `SettingsJson` zu
   `PrintPrefs` (`{ Columns: [{Key, Visible, Order}], DefaultSortColumn, DefaultSortDirection }`) und
   berechnet die sichtbaren Spalten serverseitig — **kein** JavaScript, kein Query-Parameter.

Die Kommissionierlisten-Drucken-/PDF-Links (`Index.cshtml` Z. 16-18, Z. 96-100) sind **serverseitig
gerenderte `<a href>`**, kein JS-Click-Handler wie in `Bom.cshtml` — ein DOM-Scan zur Klickzeit wäre
hier zusätzlicher, unnötiger JS-Code nur um etwas zu transportieren, das serverseitig ohnehin schon
in der Datenbank steht. **Diese Spec übernimmt deshalb das `WarehousePickingPrintLayout`-Muster, nicht
das `PrintBom`-Muster** (ponytail Sprosse 2/6: wiederverwenden, was da ist; kein DOM-Scan wo ein
DB-Read reicht):

```csharp
// FaHierarchyKommissionierListenController.Print / .Pdf
IReadOnlyList<string> visibleColumns = Array.Empty<string>();
var userId = _currentUserService.GetCurrentAppUserId();
if (userId.HasValue)
{
    var pref = await _viewPrefs.GetByUserAndViewAsync(userId.Value, "FaHierarchyKommissionierListen");
    var prefs = WarehousePickingPrintLayout.ParsePrefs(pref?.SettingsJson);
    visibleColumns = WarehousePickingPrintLayout.ResolveColumns(prefs, ColumnDefinitions.FaHierarchyKommissionierListen.Columns)
        .Select(c => c.Key).ToList();
}
```

`WarehousePickingPrintLayout.ResolveColumns` bekommt dafür eine zweite Überladung, die die
Spalten-Definitionsliste als Parameter statt hart codiert entgegennimmt; die bestehende Überladung
delegiert mit `ColumnDefinitions.WarehousePickingDetails.Columns` unverändert weiter (kein
Verhaltenswechsel für `WarehousePickingController`). Eine neue, fast identische Klasse für
denselben Zweck anzulegen wäre die unnötigere Lösung (ponytail Sprosse 2) — die Namensungenauigkeit
(„WarehousePicking…" für einen inzwischen generischen Helfer) wird als `ponytail:`-Kommentar an der
Klasse vermerkt, ein Umbenennen lohnt erst bei einem dritten Konsumenten.

`Print.cshtml`/neu `PrintSummiert.cshtml` bekommen denselben `ShowCol(key)`-Helfer wie
`PrintBom.cshtml` (Z. 196 ff.), gespeist aus `Model.VisibleColumns` (leere Liste ⇒ alle Spalten
sichtbar — Default-Verhalten wie bei `PrintBom`, deckt auch den Fall „kein eingeloggter Anwender/keine
gespeicherte Präferenz" aus dem obigen Codeblock ab).

### C — Neue Druck-/PDF-Wege für die Summiert-Ansicht

`PrintSummiert`/`PdfSummiert` folgen exakt dem Muster von `Print`/`Pdf`, aber auf
`KommissionierListenService.AggregateSummiert`-Zeilen statt Rohpositionen:

```csharp
public async Task<IActionResult> PrintSummiert(string? target, string? kwVon, string? kwBis)
{
    var kwRange = IsoWeekRange.Resolve(kwVon, kwBis);
    var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
    var result = await _service.BuildSummiertAsync(target, kwRange, columnFilters, 1, int.MaxValue, paginate: false);
    var groups = result.Rows.GroupBy(r => r.HauptFA).OrderBy(g => g.Key);
    // ... VisibleColumns wie oben, ColumnDefinitions.FaHierarchyKommissionierSummiert
    // ... Barcode je HauptFA wie Print/Pdf
}
```

Die flachen Aggregatzeilen werden für den Druck nach `HauptFA` gruppiert (kein neuer
`FaHierarchyListGroupViewModel`-Umweg nötig — reines `GroupBy` auf der bereits vorliegenden Liste),
damit dieselbe Seiten-je-HauptFA-Struktur mit Barcode entsteht wie beim bestehenden `Print`. Der
Dokumentkopf zeigt zusätzlich zum bestehenden „Gefilterte Ansicht"-Banner (falls Filter aktiv) ein
zweites Banner „Summierte Ansicht — diese Mengen sind über mehrere Positionen zusammengefasst".

### D — Nebenbefund: `ColumnDefinitions.FaHierarchyKommissionierSummiert` fehlt

`ColumnDefinitions.GetByViewKey` (Z. 445-456) kennt `FaHierarchyKommissionierListen`, aber keinen
Eintrag für `FaHierarchyKommissionierSummiert`, obwohl `Summiert.cshtml` bereits
`data-view-key="FaHierarchyKommissionierSummiert"` und ein eigenes `#column-config` trägt.
`UserViewPreferencesApiController` (Z. 29/45/67) lehnt Lesen/Schreiben von Präferenzen für einen
unregistrierten View-Key ab (`fallstricke.md` §10) — die Spalten-Auswahl (Zahnrad) der bestehenden
Summiert-Ansicht dürfte damit heute schon wirkungslos sein. Diese Spec trägt den fehlenden Eintrag
nach (4 Spalten: `hauptfa`/`artnr`/`kommissionieren`/`sollmenge`, `hauptfa` locked, identisch zum
inline `#column-config` in `Summiert.cshtml`) — Voraussetzung dafür, dass Druck/PDF dieser Ansicht
überhaupt eine Spalten-Präferenz zum Lesen vorfinden können.

## Migrations-/SQL-Auswirkungen

**Keine.** Es entstehen keine neuen Tabellen/Spalten, keine EF-Migration, kein `SQL/XX_*.sql`, keine
Änderung an `SQL/00_FreshInstall.sql`. `FaHierarchyNode` bleibt unverändert (Falle 2 wird
dokumentiert, nicht durch ein neues Feld gelöst — siehe Rückfrage 3). `UserViewPreference` (bereits
bestehende Tabelle) wird von dieser Spec nur **gelesen**, nicht erweitert. `ColumnDefinitions` ist
reiner In-Memory-Code, kein Datenbankobjekt.

## Audit-Feld-Auswirkungen

**Keine.** `FaHierarchyNode` ist kein `AuditableEntity` (reine Sync-Cache-Projektion, siehe
Klassenkommentar) — es wird ohnehin nur gelesen, nicht geschrieben. `UserViewPreference` wird durch
diese Spec ausschließlich gelesen (das Schreiben passiert weiterhin ausschließlich über
`UserViewPreferencesApiController`, unverändert). Es entsteht kein neuer Schreibpfad.

## Akzeptanzkriterien

1. In der Kommissionierlisten-Summiert-Ansicht erscheint je `(HauptFA, Artnr, Kommissionieren)`
   genau eine Zeile mit der Summe aller `Sollmenge`-Werte der zugrunde liegenden Positionen — nicht
   je Vorkommen eine eigene Zeile.
2. Kommt derselbe Artikel am selben HauptFA mit **zwei unterschiedlichen** Kommissionierzielen vor,
   bleiben es **zwei** Aggregatzeilen (Ziel bleibt erhalten, Falle 1).
3. Zwei unterschiedliche Artikelnummern mit **gleichem** Matchcode werden **nicht** zu einer Zeile
   zusammengefasst (Falle 3, Aggregation über `Artnr`).
4. Der Wechsel zwischen „Liste" und „Summiert" behält den gewählten Kommissionier-Ziel-Filter bei
   (Regressionscheck, bestehendes Verhalten).
5. Ein Druck/PDF der (Liste-)Kommissionierliste zeigt **ausschließlich** die Spalten, die der
   aufrufende Anwender aktuell über sein Zahnrad eingeblendet hat — eine ausgeblendete Spalte fehlt
   im Ausdruck vollständig (Kopf **und** Zellen), eine eingeblendete Spalte erscheint.
6. Hat ein Anwender keine gespeicherte Spalten-Präferenz (oder ist nicht eingeloggt erkennbar),
   zeigt der Druck/PDF **alle** Spalten (Default-Verhalten identisch zu `PrintBom`).
7. Ein Druck/PDF der Summiert-Ansicht zeigt die Aggregatzeilen (nicht die Rohpositionen) und trägt
   im Kopf sichtbar den Hinweis „Summierte Ansicht".
8. Ist zusätzlich ein Spaltenfilter oder ein Ziel-Filter aktiv, zeigt derselbe Druck/PDF **beide**
   Hinweise („Gefilterte Ansicht" und „Summierte Ansicht") nebeneinander.
9. Der bestehende Filter-Durchschlag (Spaltenfilter + Ziel) im Druck/PDF der Liste bleibt unverändert
   funktionsfähig (Regressionscheck zum Beleg oben).
10. `ColumnDefinitions.GetByViewKey("FaHierarchyKommissionierSummiert")` liefert einen gültigen
    `ViewConfig` mit den vier Spalten der Summiert-Ansicht; Speichern einer Spalten-Präferenz für
    diese Ansicht über `/api/userviewpreferences` gelingt (Regressionscheck zum Nebenbefund D).
11. `WarehousePickingPrintLayout.ResolveColumns(prefs, ColumnDefinitions.WarehousePickingDetails.Columns)`
    liefert für `WarehousePickingController.Print` exakt dasselbe Ergebnis wie vor dieser Spec
    (Regressionscheck zur neuen Überladung).
12. **Nur falls Rückfrage 4 mit „lockern" beantwortet wird:** Die Summiert-Ansicht liefert **ohne**
    Kalenderwochen-Eingabe die Summe über **alle** HauptFA (nicht mehr das leere „Bitte KW
    eingeben"-Ergebnis); mit KW-Eingabe filtert sie wie bisher auf die HauptFA der gewählten Woche.
13. **Nur falls Rückfrage 3 mit „bekanntes Risiko akzeptiert" beantwortet wird:** `fallstricke.md`
    enthält einen Eintrag, der das Fehlen eines Mengeneinheit-Felds an `FaHierarchyNode` und die
    daraus folgende Nicht-Prüfbarkeit von Falle 2 dokumentiert.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (nächste freie Nummer zum Umsetzungszeitpunkt — Stand
2026-09-25 im Worktree zuletzt TS-78 vergeben; die parallele Spec
[[2026-09-25-kommissionierung-nur-hauptfa-spec]] beansprucht bereits TS-79 — läuft sie zuerst, ist
die nächste freie Nummer für diese Spec entsprechend TS-80 oder höher; im Dev-Lauf gegen den dann
aktuellen Stand prüfen). Skizze der Szenarien:

- **TS-x.1 Summierung Grundfall:** HauptFA mit derselben Artikelnummer auf zwei Positionen desselben
  Ziels → Summiert-Ansicht zeigt eine Zeile mit der Summe; Liste-Ansicht zeigt weiterhin beide
  Einzelpositionen.
- **TS-x.2 Falle 1 (Ziel bleibt erhalten):** dieselbe Artikelnummer, zwei unterschiedliche
  Kommissionierziele → zwei Aggregatzeilen, keine Vermischung der Ziele.
- **TS-x.3 Falle 3 (Artikelnummer statt Matchcode):** zwei Artikelnummern mit identischem Matchcode
  → zwei getrennte Aggregatzeilen.
- **TS-x.4 Toggle:** Wechsel Liste ↔ Summiert behält den Ziel-Filter; (falls Rückfrage 2 „Summiert
  als Default" ergibt) Aufruf ohne Parameter landet auf der Summiert-Ansicht.
- **TS-x.5 PDF Spalten-Sichtbarkeit:** Anwender blendet über das Zahnrad 3 Spalten aus, druckt/lädt
  PDF → genau die verbleibenden Spalten erscheinen, Kopf UND Zellen; ein zweiter Anwender mit
  anderer Präferenz erhält ein anderes PDF-Layout für denselben HauptFA (dokumentierte Konsequenz).
- **TS-x.6 PDF ohne Präferenz:** Anwender ohne gespeicherte Spalten-Präferenz erhält alle Spalten.
- **TS-x.7 PDF Filter-Durchschlag (Regression):** Spaltenfilter + Ziel-Filter aktiv → PDF zeigt nur
  die gefilterten Zeilen, Banner „Gefilterte Ansicht" mit Ziel-Text.
- **TS-x.8 PDF Summiert:** Druck/PDF der Summiert-Ansicht → Aggregatzeilen, Banner „Summierte
  Ansicht"; kombiniert mit aktivem Filter zeigt der Kopf beide Banner.
- **TS-x.9 (nur falls Rückfrage 4 „lockern"):** Summiert-Ansicht ohne KW-Eingabe zeigt die Summe über
  alle HauptFA statt des Hinweistexts; mit KW-Eingabe filtert sie wie bisher.
- **TS-x.10 Nebenbefund:** Spalten-Präferenz für die Summiert-Ansicht lässt sich speichern und wirkt
  auf Bildschirm UND Druck.
- **TS-x.11 Regression Vormontage/Beschichtung:** Teil 4/5 unverändert funktionsfähig (kein
  gemeinsamer Code außer `FaHierarchyListBuilder`, der unangetastet bleibt).

## Deploy

- **Web-App: ja.** Neue/geänderte Controller-Actions, ViewModels, `ColumnDefinitions`-Eintrag,
  `WarehousePickingPrintLayout`-Überladung, zwei neue Views (`PrintSummiert.cshtml`), geänderte
  Views (`Print.cshtml`, `Summiert.cshtml`, ggf. `Index.cshtml`). Alle Änderungen unter
  `IdealAkeWms/`.
- **Service: nein.** Kein Service-seitiger Code betroffen (nur Versions-Bump in `AppVersion.cs` laut
  Checkliste).
- **Migration: nein.** Kein Schema-Impact (siehe Migrations-/SQL-Auswirkungen).
- **Betriebs-Vorbedingung:** keine neue — die PDF-Erzeugung nutzt weiterhin den bereits vorhandenen
  `IPdfRenderService`/Edge-Baustein aus [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]]
  unverändert.
- **Publish-Befehl (im Worktree, Mensch-Flow: Worktree → Testsystem → Testen → danach Merge):**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  ```
  Nach dem Merge nach `main`: nur erneut publishen, falls der Merge tatsächlich getestete Dateien mit
  parallelen `main`-Änderungen zusammengeführt hat.

## Offene Rückfragen

1. **Summierschlüssel bestätigen:** Der bereits gebaute Schlüssel `(HauptFA, Artnr, Kommissionieren)`
   aus der bestehenden `/Summiert`-Ansicht entspricht exakt Falle 1 (Ziel bleibt erhalten) und
   Falle 3 (Artikelnummer statt Matchcode) aus dem Backlog. *Einschätzung:* übernehmen, keine
   Änderung nötig — eine Aufschlüsselung der Ziele in einer einzigen Artikelzeile (die Backlog-Alternative)
   wäre unübersichtlicher und dem Kommissionierer schwerer lesbar als mehrere klar getrennte Zeilen.
2. **Toggle-Standard:** Bleibt „Liste" der Standard beim Aufruf ohne Parameter, oder soll „Summiert"
   künftig Standard sein (Backlog-Einschätzung)? Hängt an Rückfrage 4 — ein Default auf eine Ansicht,
   die ohne Kalenderwoche leer bleibt, wäre irreführend. *Einschätzung:* „Liste" bleibt Standard,
   solange Rückfrage 4 nicht mit „KW-Pflicht lockern" beantwortet ist.
3. **Mengeneinheit (Falle 2) nicht prüfbar:** `FaHierarchyNode` hat kein Einheit-Feld — verifiziert,
   keine Annahme. Wird das Fehlen als bekanntes, dokumentiertes Risiko akzeptiert (Sollmenge wird
   ungeprüft summiert, Fallstrick-Eintrag warnt), oder ist ein Einheit-Feld an
   `FaHierarchyNode`/der Sage-View eine Vorbedingung, die vor dieser Spec in einem eigenen
   Backlog-Punkt zu klären ist? *Einschätzung:* Sage liefert für Kommissionier-Positionen faktisch
   nahezu ausschließlich Stück-Mengen; das Risiko unentdeckter Mischeinheiten ist vermutlich klein,
   aber unbelegt — Entscheidung liegt beim Fachbereich.
4. **KW-Pflicht der bestehenden Summiert-Ansicht lockern?** Aktuell (Nachtrag AK N2d, bereits
   freigegeben) zeigt die Summiert-Ansicht ohne Kalenderwoche bewusst kein Ergebnis. Für den vom
   Backlog beschriebenen Alltagsgebrauch („dieselbe Liste, aber zusammengefasst") wäre das
   hinderlich. *Einschätzung:* lockern, analog zur bereits etablierten Vormontage-Sicht 2 (KW dort
   schon optional) — ohne KW alle HauptFA einschließen, KW bleibt als zusätzlicher Filter nutzbar.
   Das ändert eine bereits freigegebene Verhaltensentscheidung, deshalb Rückfrage statt Annahme.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
2. →
3. →
4. →
