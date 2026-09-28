---
type: spec
title: "IDEAL Kommissionierliste: Summierung je Artikel + Kommissionierziel, PDF nur gefiltert + nur sichtbare Spalten"
slug: 2026-09-25-kommissionierliste-summierung-pdf-spec
status: Entwurf
created: 2026-09-25
updated: 2026-09-28
source_backlog: "[[2026-09-23-kommissionierliste-summierung-pdf]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Services/KommissionierListenService.cs `SummiertColumnMap` (Z. 73-80) — auf 6 Spalten erweitern (B2): `matchcode` und `hauptlagerplatz` ergaenzen, Getter identisch zu `ColumnMap` (Z. 58/60)."
  - "IdealAkeWms/Services/KommissionierListenService.cs `AggregateSummiert` (Z. 139-201) — Schritt (1) (Z. 151-153, fruehere KW-Pflicht `AK N2d`) UNBEDINGT entfernen (Antwort 4, nicht mehr abhaengig von einer Rueckfrage): ohne `kwRange` werden ALLE HauptFA einbezogen (`includedHauptFas = null` = keine Einschraenkung). Schritt (4)/(5) (Z. 167-201): Aggregat-Projektion um `Matchcode = g.First().Matchcode` und `Hauptlagerplatz = g.First().Hauptlagerplatz` ergaenzen (B2, Artikeleigenschaften sind je Summenschluessel konstant, aendert die Gruppierung nicht). Server-Spaltenfilter (Z. 187) bleibt VOR dem Paging; NEU danach Gruppen-Paging (SOLLTE-5, S1): erst `GroupBy(a => a.HauptFA)`, `TotalGroupCount` zaehlt Gruppen, `Skip/Take` auf GRUPPEN (Seiteneinheit = HauptFA, analog `FaHierarchyListBuilder.Build`), dann `SelectMany` zurueck zu einer flachen Zeilenliste fuer die Seite. `TotalRowCount` bleibt zusaetzlich als informative Zeilenzahl NACH Filter erhalten (bestehende Tests pruefen es)."
  - "IdealAkeWms/Services/KommissionierListenService.cs — NEU: `ToggleSharedQueryKeys` (statisches `string[]`: `target, kwVon, kwBis, colf_hauptfa, colf_artnr, colf_matchcode, colf_kommissionieren, colf_hauptlagerplatz` — bewusst OHNE `colf_sollmenge`, Begruendung Abschnitt E) + `BuildToggleQuery(HttpRequest? request)` (liest `request.Query`, filtert auf die erlaubten Keys, gibt einen URL-Query-String zurueck) — EIN Ort fuer die Liste, von `Index.cshtml` UND `Summiert.cshtml` genutzt (ponytail Sprosse 2: DRY statt zweimal dieselbe Key-Liste)."
  - "IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierSummiertViewModel.cs — `FaHierarchyKommissionierSummiertRowViewModel` (Z. 13-29): neue Felder `Matchcode`, `Hauptlagerplatz` (string?). `FaHierarchyKommissionierSummiertViewModel` (Z. 50-74): neues Feld `PdfVerfuegbar` (bool, analog `FaHierarchyKommissionierListViewModel.PdfVerfuegbar`). `FaHierarchyKommissionierSummiertResult` (Z. 36-42): neues Feld `TotalGroupCount` (int). NEU: `FaHierarchyKommissionierSummiertPrintViewModel` (Rows, Barcodes, SelectedTarget, IsFiltered, VisibleColumns — analog `FaHierarchyKommissionierPrintViewModel`)."
  - "IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierGruppeViewModel.cs `FaHierarchyKommissionierPrintViewModel` (Z. 36-48) — neues Feld `VisibleColumns` (`IReadOnlyList<string>`, leer = alle sichtbar, Konvention identisch `BomViewModels.VisibleColumns`)."
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs — NEU nach `FaHierarchyKommissionierListen` (Z. 328-346): `FaHierarchyKommissionierSummiert`-Eintrag mit 6 Spalten `hauptfa[locked]/artnr/matchcode[locked]/kommissionieren/hauptlagerplatz/sollmenge` (B2; Reihenfolge: Identifikatoren zuerst, dann Ziel, dann Ort, Menge zuletzt — per Drag&Drop umsortierbar, `SupportsReorder: true`), `SupportsSortDefault: false` (gruppierte Darstellung wie `FaHierarchyKommissionierListen`, NICHT wie die flache `FaHierarchyVormontageSummiert`, seit SOLLTE-5 gruppiert). Registrierung in `GetByViewKey` (Z. 445 ff., nach Z. 456) — behebt den vorbestehenden Fund: ohne diese Registrierung lehnt `UserViewPreferencesApiController` Lesen/Schreiben von Praeferenzen fuer die Summiert-Ansicht ab (`fallstricke.md`, Eintrag „Neuer viewKey ohne GetByViewKey-Registrierung“)."
  - "IdealAkeWms/Controllers/FaHierarchyKommissionierListenController.cs — `IUserViewPreferenceRepository _viewPrefs` als neue Ctor-Abhaengigkeit. NEU: private `ResolveVisibleColumnsAsync(string? visibleColumns, string viewKey, IReadOnlyList<ColumnDef> defs)` (B1a): ist der Query-Parameter `visibleColumns` gesetzt, direkt `Split(',')` (PrintBom-Konvention, KEIN DB-Zugriff); ist er leer, Server-Rueckfall — `_viewPrefs.GetByUserAndViewAsync(userId, viewKey)`, `WarehousePickingPrintLayout.ParsePrefs(...)`, dann EINFACHER Filter ueber `defs` (`d.Locked || sichtbar`) OHNE die urspruenglich geplante `ResolveColumns(prefs, defs)`-Ueberladung — die Druckvorlagen brauchen nur eine sichtbar/unsichtbar-Menge, keine Reihenfolge (`ShowCol` prueft nur Mitgliedschaft, die Spaltenreihenfolge im Druck ist fest im `.cshtml` verdrahtet, siehe `PrintBom.cshtml`); keine gespeicherte Praeferenz oder kein eingeloggter Anwender ⇒ leere Liste = alle Spalten. `Print(string? target, string? visibleColumns)` (Z. 130-149) und `Pdf(int hauptFa, string? target, string? visibleColumns)` (Z. 155-182): `VisibleColumns` im ViewModel ueber den neuen Helfer befuellen. `Summiert` (Z. 91-124): `Pagination.TotalCount` von `result.TotalRowCount` auf `result.TotalGroupCount` umstellen (Gruppen-Paging). NEU: `PrintSummiert(string? target, string? kwVon, string? kwBis, string? visibleColumns)` und `PdfSummiert(int hauptFa, string? target, string? kwVon, string? kwBis, string? visibleColumns)` (Route `[HttpGet(\"FaHierarchyKommissionierListen/PdfSummiert/{hauptFa:int}\")]`, analog `Pdf`), rendern `PrintSummiert.cshtml`/reduziert auf eine Gruppe; `PdfSummiert`-Fehlerpfad (`PdfRenderException`) leitet auf `RedirectToAction(nameof(Summiert), new { target, kwVon, kwBis })` zurueck — NICHT auf `Index` (SOLLTE-6, das Vorbild-Verhalten von `Pdf` waere hier fachlich falsch)."
  - "IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml — Drucken-Link (Z. 16) und PDF-je-Gruppe-Link (Z. 96) bekommen `data-print-link=\"FaHierarchyKommissionierListen\"`; `#column-config`/`<th data-col-key>` UNVERAENDERT (bereits vollstaendig); neuer `@section Scripts`-Eintrag fuer `print-visible-columns.js`."
  - "IdealAkeWms/Views/FaHierarchyKommissionierListen/Print.cshtml — Positionsblock (Kopf Z. 106-117, Zeile Z. 120-134) hinter `ShowCol(key)` (Vorbild `Views/Picking/PrintBom.cshtml` Z. 196-260), gespeist aus `Model.VisibleColumns`. KEINE `colCount`-Aenderung noetig — am Code verifiziert: `Print.cshtml` hat aktuell KEINEN `colspan`-getriebenen Konstantenwert (anders als in der urspruenglichen Entwurfsannahme); die Positionstabelle hat keine Gruppen-Kopfzeile, die einen `colspan` braucht. Diese Annahme des Erstentwurfs war falsch und wird hier stillschweigend korrigiert (kein Rueckfrage-Gegenstand, reiner Code-Abgleich)."
  - "IdealAkeWms/Views/FaHierarchyKommissionierListen/PrintSummiert.cshtml (NEU) — analoges Layout zu `Print.cshtml`: je HauptFA-Gruppe (`Model.Rows.GroupBy(r => r.HauptFA)`, KEIN neuer Gruppen-ViewModel-Umweg, reines `GroupBy` auf der flachen Zeilenliste) ein `page-break-after`-Block, Barcode = HauptFA, `ShowCol` ueber die 5 Positions-Spalten (`artnr/matchcode/kommissionieren/hauptlagerplatz/sollmenge` — `hauptfa` ist Gruppen-Header, keine Positionsspalte, genau wie in `Print.cshtml`), zusaetzliches „Summierte Ansicht“-Banner analog dem bestehenden `filter-banner`."
  - "IdealAkeWms/Views/FaHierarchyKommissionierListen/Summiert.cshtml — grundlegend umgebaut (SOLLTE-5): der `!Model.HasKwFilter`-Zweig mit Hinweistext + versteckter Tabelle (Z. 64-109) ENTFAELLT (Antwort 4: KW ist nur noch optionaler Zusatzfilter, keine Bedingung fuers Anzeigen); Tabelle wird WIE `Index.cshtml` nach `Model.Rows.GroupBy(r => r.HauptFA)` gruppiert gerendert (eigenes `<tbody>` je Gruppe, Gruppen-Kopfzeile mit `HauptFA X` + PDF-Link `data-print-link=\"FaHierarchyKommissionierSummiert\"` sofern `Model.PdfVerfuegbar`); KEINE Auftrags-Kopfdaten in der Gruppen-Kopfzeile (die Summiert-Zeilen fuehren keine `OrderInfos`, ein Nachladen ist nicht angefordert — YAGNI, bewusst nicht gebaut). Neuer seitenweiter Drucken-Link (`data-print-link=\"FaHierarchyKommissionierSummiert\"`, Ziel `PrintSummiert`, `Context.Request.QueryString` wie `Index.cshtml`). Zwei neue `<th data-col-key>` (`matchcode` locked, `hauptlagerplatz`) + `#column-config` auf 6 Eintraege erweitert (B2, Reihenfolge wie `ColumnDefinitions.FaHierarchyKommissionierSummiert`). Umschalter-Links (Z. 16/19) nutzen NEU `KommissionierListenService.BuildToggleQuery` statt nur `asp-route-target` (E, SOLLTE-7). Neuer `@section Scripts`-Eintrag fuer `print-visible-columns.js`."
  - "IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml — Umschalter-Link (Z. 25/28) ebenfalls auf `KommissionierListenService.BuildToggleQuery` umstellen (E, SOLLTE-7, Gegenrichtung)."
  - "IdealAkeWms/wwwroot/js/print-visible-columns.js (NEU) — kleiner, wiederverwendbarer Click-Handler: fuer jedes `a[data-print-link]` beim Klick die sichtbaren `data-col-key`-Spalten der zugehoerigen `table[data-view-key=\"<data-print-link-Wert>\"]` auslesen (Muster `Views/Picking/Bom.cshtml` Z. 1260-1267: `th.style.display !== 'none' && !th.classList.contains('d-none')`) und als `visibleColumns`-Query-Parameter an `a.href` anhaengen, BEVOR der Browser dem Link folgt (kein `preventDefault`/`window.open` noetig, echte `<a href>`-Navigation bleibt erhalten). Ersetzt KEINEN Server-Code — reine Ergaenzung des schon vorhandenen `href`."
  - "IdealAkeWms/Views/Shared/_Layout.cshtml (Z. 184 Dropdown-Item, Z. 203-204 Nav-Link) — beide `asp-action` von `Index` auf `Summiert` (Antwort 2: Summiert wird Standard-Einstieg der Kommissionierlisten; `Index`/Liste bleibt ueber den Umschalter erreichbar, kein Redirect/Parameter-Sonderfall im Controller)."
  - "IdealAkeWms.Tests/Services/KommissionierListenServiceTests.cs `Summiert_NoKw_ReturnsEmpty_NoImplicitTotal` (Z. 162-176) UMKEHREN (SOLLTE-2, kein Loeschen — das Verhalten aendert sich bewusst): neuer Name/Assert `ohne KW werden ALLE HauptFA einbezogen` statt leeres Ergebnis. NEU: Tests fuer `Matchcode`/`Hauptlagerplatz` in der Aggregatzeile (B2, `g.First()` aendert die Gruppierung nicht — bestehende Summier-Tests bleiben unveraendert gruen) und fuer Gruppen-Paging (zwei HauptFA, `pageSize: 1` ⇒ Seite 1 enthaelt ALLE Zeilen von genau einer HauptFA, `TotalGroupCount == 2`)."
  - "IdealAkeWms.Tests/Views/KommissionierSpaltenKonsistenzTests.cs (NEU, SOLLTE-1) — Quelltext-Waechter analog `Helpers/TableScriptScopedSelectorTests.cs` (liest `.cshtml`-Dateien als Text, KEIN JS-Ausfuehrung): fuer `FaHierarchyKommissionierListen` und `FaHierarchyKommissionierSummiert` je die vier Spaltenschluessel-Quellen (`ColumnDefinitions.<View>.Columns`, `#column-config`-JSON, `<th data-col-key>` der Bildschirm-View, `ShowCol(\"...\")`-Aufrufe der zugehoerigen Druckvorlage abzueglich `hauptfa`, das dort Gruppen-Header ist) gegeneinander vergleichen — Drift macht den Test rot."
  - "secondbrain/architektur/fallstricke.md — neuer Eintrag Falle 2/Mengeneinheit: `FaHierarchyNode` hat kein Einheit-Feld; Quelle ist die IDEAL-Sage-View `vw_IDEAL-AKE_Kommissionierung_FAListe` (NICHT `KHKPpsRessourcenPositionen`, das ist die AKE-Stuecklistenquelle — Korrektur gegenueber der urspruenglichen Rueckfrage-3-Formulierung); Begruendung „Einheit ist Artikeleigenschaft, der Summierschluessel enthaelt die Artikelnummer“ + Verweis auf den Pruefschritt (Deploy-Abschnitt) und dass B3 NICHT blockierend gestellt wurde."
  - "secondbrain/specs/freigegeben/2026-07-29-standort-ideal-teil-3-spec.md — Vermerk an `AK N2d`: durch diese Spec ueberholt (Antwort 4), Wikilink hierher. HAUPTCHECKOUT-Datei, Dev-Lauf-Pflicht (nicht im Worktree)."
  - "docs/TESTSZENARIEN.md — neues Kapitel `TS-80` (Stand 2026-09-28 im Worktree: hoechstes Kapitel `TS-79`, [[2026-09-25-kommissionierung-nur-hauptfa-spec]] bereits Testbereit/v1.46.0 — Dev-Lauf verifiziert die Nummer erneut gegen den dann aktuellen Stand) + `secondbrain/tests/testszenarien-index.md`."
  - "IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs (voraussichtlich 1.47.0, Worktree-Stand 2026-09-28: 1.46.0 — Dev-Lauf verifiziert) + Views/Help/Changelog.cshtml (neuer Eintrag)."
  - "Out-of-Scope-Verweis (kein Umsetzungsgegenstand dieser Spec): [[2026-09-28-spaltenpraeferenz-speicherfehler-still]] — stilles Scheitern von `column-preferences.js` `saveSettings` (nur `console.warn`, Z. 312) als eigene Backlog-Notiz."
open_questions:
  - "KONFLIKT (neu, 2026-09-28): Antwort 4 verlangt, dass der Umschalter Liste<->Summiert `target`, KW UND die gemeinsamen Spaltenfilter mitnimmt. Am Code verifiziert: `FaHierarchyKommissionierListenController.Index` (Liste) hat GAR KEINEN KW-Parameter (`Index(string? target, int page, int? pageSize)`) — nur `Summiert` kennt `kwVon`/`kwBis`. Eine beim Wechsel Summiert->Liste gesetzte KW kann in der Liste nirgends wirken. Zwei Wege: (a) die Liste bekommt selbst einen optionalen KW-Filter (Scope-Zuwachs ueber das Gemeldete hinaus, aber fachlich ehrlich: KW wirkt dort auch), oder (b) KW wird beim Wechsel zur Liste nur als toter Pass-Through-Query-Parameter mitgefuehrt (kein Codeeingriff in `Index`, KW wird erst beim Zurueckwechseln zu `Summiert` wieder wirksam). *Einschaetzung:* (b) ist der kleinere, unangeforderte Diff und aendert das gemeldete Verhalten der Liste nicht — wird aber inkonsistent aussehen, falls ein Anwender die URL mit `kwVon`/`kwBis` in der Liste inspiziert und sich fragt, warum sie nichts filtert. Menschliche Entscheidung noetig, NICHT selbst entschieden."
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
beantwortete_rueckfragen:
  - "1: Summierschluessel `(HauptFA, Artnr, Kommissionieren)` bestaetigt, keine Aenderung, Summe bleibt JE HauptFA (keine auftragsuebergreifende Sammelkommissionierung)."
  - "2: `Summiert` wird Standard-Einstieg (Menuepunkte zeigen kuenftig auf `/Summiert`), `Index`/Liste bleibt ueber den Umschalter erreichbar; kein Parameter-Sonderfall/Redirect."
  - "3: Falle 2 (Mengeneinheit) akzeptiert als strukturell erledigt, SOFERN `KHKPpsRessourcenPositionen` keine eigene, vom Artikel abweichende Positionseinheit fuehrt — Pruefschritt vor Abnahme (korrigiert 2026-09-28: die tatsaechliche Quelle ist `vw_IDEAL-AKE_Kommissionierung_FAListe`, siehe ANTWORTEN-Abschnitt)."
  - "4: KW-Pflicht der Summiert-Ansicht wird gelockert — ohne KW alle HauptFA, KW bleibt optionaler Zusatzfilter; fruehere Entscheidung AK N2d damit ueberholt."
---

> [!info] Umsetzungsort (Hinweis, keine Frontmatter-Vorgabe)
> Der gesamte betroffene Code (`FaHierarchyKommissionierListenController`, `KommissionierListenService`,
> `FaHierarchyVormontageController`, `PdfRenderService`, `Views/Picking/PrintBom.cshtml`,
> `WarehousePickingPrintLayout`, `ColumnDefinitions`) existiert **nur** im nicht gemergten
> Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch
> `feature/2026-08-07-ideal-teile-1-5`). Im `main`-Checkout existiert davon **nichts**. Die Umsetzung
> erfolgt dort als weitere Etappe desselben Bündels — kein neuer Worktree.
>
> **Stand bei Ersterstellung (2026-09-25):** `AppVersion 1.45.0`, höchstes Testszenarien-Kapitel
> `TS-78`, höchste SQL-Nummer `93`.
>
> **NACHGEZOGEN am Worktree-Stand 2026-09-28** (diese Nachbesserung): [[2026-09-25-kommissionierung-nur-hauptfa-spec]]
> ist inzwischen **umgesetzt und Testbereit** (`AppVersion 1.46.0`, `TS-79` vergeben) — NICHT mehr
> „parallel/InUmsetzung“, wie der ursprüngliche Info-Kasten noch behauptete (Korrektur zu S4 der
> Kritischen Prüfung, siehe dort). `KommissionierListenService.BuildFlagPredicate` delegiert seither an
> `KommissionierRelevanzFilter.IsRelevant` (Z. 228-236) — die vorliegende Spec baut auf diesem bereits
> extrahierten Stand auf, ohne `BuildFlagPredicate` selbst zu ändern. Der Dev-Lauf verifiziert
> `AppVersion.cs`/`docs/TESTSZENARIEN.md`/SQL-Nummer erneut gegen den dann aktuellen Worktree-Stand,
> bevor er festschreibt — voraussichtlich `1.47.0` / `TS-80`.

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
die Artikelnummer `Artnr`, nicht über `Matchcode`) aus dem Backlog. Es gibt bereits eine
Nav-Pills-Umschaltung „Liste" / „Summiert".

**Nach der Kritischen Prüfung (2026-09-25) und den Antworten darauf (2026-09-28) kommt dazu:** Die
Summiert-Ansicht wird durch Antwort 2 zum **Standard-Einstieg** der Kommissionierlisten — sie darf
dann nicht schlechter ausgestattet sein als die Liste. Sie bekommt deshalb `Matchcode` und
`Hauptlagerplatz` als Anzeigespalten (B2), Gruppen-Paging + Druck/PDF je HauptFA (SOLLTE-5), und der
Umschalter Liste↔Summiert nimmt neben `target` auch KW und die gemeinsamen Spaltenfilter mit (E).
Die Spalten-Sichtbarkeit im Druck/PDF liest **zur Klickzeit aus dem DOM** (B1, PrintBom-Muster mit
Server-Rückfall), nicht mehr rein serverseitig aus der (verzögert gespeicherten) Präferenz.

**Was tatsächlich gebaut wird** (nach Kritischer Prüfung + Antworten):

- Die KW-Pflicht der Summiert-Ansicht entfällt unbedingt (Antwort 4) — ohne KW-Eingabe werden alle
  HauptFA einbezogen, KW bleibt ein optionaler Zusatzfilter.
- `Print`/`Pdf` (Liste) und die neuen `PrintSummiert`/`PdfSummiert` lesen die aktuell **sichtbaren**
  Spalten zur Klickzeit aus dem DOM (Query-Parameter `visibleColumns`, PrintBom-Konvention) und
  fallen nur bei fehlendem Parameter (z. B. direkt aufgerufene URL) auf die gespeicherte Präferenz
  zurück.
- Für die Summiert-Ansicht entsteht ein vollständiger Druck-/PDF-Weg, gruppiert nach HauptFA wie die
  Liste (kein Seitenriss innerhalb einer HauptFA-Gruppe).
- Falle 2 (nur gleiche Mengeneinheiten summieren) bleibt technisch nicht prüfbar — dokumentiert,
  mit einem vor der Abnahme durchzuführenden Prüfschritt statt einer Blockade (B3).

Der **Filter-Durchschlag im PDF ist für die Liste bereits vorhanden** (siehe Beleg unten) — die im
Backlog offene Frage 3 wird damit beantwortet, ohne eine Rückfrage zu benötigen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

1. **Summierschlüssel bestätigt, unverändert:** die bestehende `KommissionierListenService.AggregateSummiert`-Logik
   `(HauptFA, Artnr, Kommissionieren)` bleibt die einzige Kommissionier-Summiermechanik (Rückfrage 1).
2. **KW-Pflicht der Summiert-Ansicht wird gelockert** (Rückfrage 4, unbedingt): ohne KW-Eingabe alle
   HauptFA, KW bleibt optionaler Zusatzfilter. Ersetzt die frühere, bereits freigegebene Entscheidung
   `AK N2d` (Vermerk in [[2026-07-29-standort-ideal-teil-3-spec]] nachziehen).
3. **Spalten-Sichtbarkeit in Druck/PDF der Liste** (`Index`/`Print`/`Pdf`): zur Klickzeit aus dem DOM
   gelesene sichtbare Spalten (`visibleColumns`-Query-Parameter, PrintBom-Muster), Server-Rückfall auf
   die gespeicherte `UserViewPreference` nur, wenn der Parameter fehlt (B1).
4. **Druck/PDF für die Summiert-Ansicht neu bauen** (`PrintSummiert`/`PdfSummiert`), inkl. Kopf-Hinweis
   „Summierte Ansicht", Spalten-Sichtbarkeit über den neuen View-Key `FaHierarchyKommissionierSummiert`,
   Gruppen-Paging wie die Liste (SOLLTE-5).
5. **Falle 2 dokumentieren statt stillschweigend ignorieren:** `FaHierarchyNode` besitzt kein
   Mengeneinheit-Feld — dokumentiert, mit Prüfschritt vor der Abnahme (B3), NICHT blockierend.
6. **Nebenbefund beheben:** `FaHierarchyKommissionierSummiert` fehlt in
   `ColumnDefinitions.GetByViewKey` — nachgetragen, jetzt mit 6 statt 4 Spalten (B2).
7. **Summiert wird Standard-Einstieg** (Antwort 2): beide Menüpunkte in `_Layout.cshtml` zeigen künftig
   auf `Summiert` statt `Index`.
8. **Matchcode + Hauptlagerplatz als Anzeigespalten der Summiert-Ansicht** (B2), reine
   Anzeigefelder — der Summierschlüssel bleibt `(HauptFA, Artnr, Kommissionieren)` unverändert.
9. **Umschalter Liste↔Summiert nimmt Filter mit** (E, SOLLTE-7 abweichend vom ursprünglichen
   Prüfvorschlag „bewusst zurücksetzen"): `target`, KW (Pass-Through) und die auf beiden Ansichten
   existierenden Spaltenfilter (`hauptfa`, `artnr`, `matchcode`, `kommissionieren`, `hauptlagerplatz`)
   bleiben erhalten; nur-eine-Ansicht-Filter entfallen (Begründung siehe Abschnitt E).
10. **Vier-Stellen-Konsistenz als Drift-Guard-Test** (SOLLTE-1): `ColumnDefinitions`, `#column-config`,
    `<th data-col-key>` und `ShowCol(...)` müssen für beide Views je dieselbe Spaltenmenge tragen.
11. **JSON-Einbettungsregel** (fallstricke.md „Freitext nie per `@Html.Raw(...)` in ein JS-Literal"):
    verbindlich für jede künftige Einbettung von Filterwerten/Spaltenlisten in `<script>`-Blöcke
    dieser Ansicht (Abschnitt F).

**Out-of-Scope**

- **Vormontage (Teil 5) Druck/PDF.** Bereits in
  [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]] (Rückfrage 9) entschieden: „bleibt außen vor,
  bis Teil 5 selbst einen Bildschirmdruck bekommt".
- **`KommissionierRelevanzFilter`-Extraktion** aus `BuildFlagPredicate` — bereits erledigt durch
  [[2026-09-25-kommissionierung-nur-hauptfa-spec]] (Testbereit, v1.46.0); diese Spec setzt darauf auf.
- **Ein Einheit-Feld an `FaHierarchyNode`/der Sage-View ergänzen.** Wäre eine eigene Schema-/Sync-
  Änderung — diese Spec erfindet dieses Feld nicht (Rückfrage 3, B3).
- **Beschichtungsauftrag (Teil 4).** Hat bereits Druck/PDF, aber keine Summiert-Ansicht.
- **Standorteinstellungen-Schalter** für Aggregations- oder Anzeigeverhalten — nicht verlangt.
- **KW-Filter an der Liste (`Index`).** Siehe die neue KONFLIKT-Rückfrage 5 — solange nicht
  entschieden, bleibt die Liste ohne KW-Parameter; KW wird beim Wechsel zur Liste nur als
  Pass-Through-Parameter mitgeführt, ohne dort zu wirken.
- **Stilles Speicher-Scheitern der Spaltenauswahl** (`column-preferences.js`, `console.warn`) —
  eigene Backlog-Notiz [[2026-09-28-spaltenpraeferenz-speicherfehler-still]], nicht Gegenstand dieser
  Spec (B1 löst das Problem für den Normalfall „Klick auf der Seite" strukturell, siehe Abschnitt B).

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
UND das Ziel-Dropdown. Was **nicht** durchschlägt, ist die Spalten-**Sichtbarkeit** — das ist die
tatsächliche Lücke, die diese Spec schließt.

## Fachliche Anforderungen

### 1 — Summierschlüssel (bestätigt)

Aggregiert wird je `(HauptFA, Artnr, Kommissionieren)`; Summe nur über `Sollmenge`. Ein Artikel mit
zwei unterschiedlichen Kommissionierzielen bleibt zwei Zeilen (Falle 1). Zwei Artikelnummern mit
gleichem `Matchcode` bleiben zwei Zeilen, weil über `Artnr` aggregiert wird, nicht über `Matchcode`
(Falle 3). Summiert wird **je HauptFA**, nicht auftragsübergreifend (Antwort 1, ausdrücklich
festgehalten — eine auftragsübergreifende Summe wäre Sammelkommissionierung, nicht angefordert).

### 2 — Mengeneinheit (Falle 2) — technisch nicht prüfbar, Prüfschritt statt Blockade

`FaHierarchyNode` (Projektion der IDEAL-Sage-View **`vw_IDEAL-AKE_Kommissionierung_FAListe`** —
**nicht** `KHKPpsRessourcenPositionen`, das ist die AKE-Stücklistenquelle; Korrektur gegenüber der
ursprünglichen Rückfrage-3-Formulierung, siehe „ANTWORTEN auf die Kritische Prüfung") hat **kein**
Mengeneinheit-Feld. Die Sollmenge ist eine reine `decimal`-Zahl ohne begleitende Einheitsangabe. Eine
Prüfung „nur gleiche Einheiten summieren" kann damit **nicht** implementiert werden.

Die Mengeneinheit ist in Sage eine **Artikeleigenschaft** (`KHKArtikel.Lagermengeneinheit`, im WMS
`Article.Unit`), und der Summierschlüssel enthält die **Artikelnummer**: zwei Zeilen desselben
Summenschlüssels sind damit **derselbe Artikel mit derselben Einheit**, solange
Stücklistenpositionen keine vom Artikel abweichende Positionseinheit führen (Antwort 3). Diese Spec:

- baut **keine** Einheiten-Prüfung (es gäbe nichts zu prüfen),
- dokumentiert die Lücke sichtbar in `fallstricke.md` mit der korrigierten Quelle,
- verlangt vor der Abnahme einen konkreten, kurzen Prüfschritt (B3, siehe Deploy-Abschnitt):
  `EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAListe'` (welche Positionstabelle liefert
  `Sollmenge`?) + `SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME =
  '<Positionstabelle>' AND COLUMN_NAME LIKE '%einheit%'` auf der Sage-DB IDEAL. Kein Treffer ⇒ Falle 2
  gilt als strukturell erledigt. Treffer ⇒ **melden, nicht still mitlösen** (die abweichende Spalte
  gehört dann in den Summierschlüssel — eigener Nacharbeits-Punkt).
- **blockiert die Umsetzung NICHT** (B3): Die bestehende Summiert-Ansicht summiert mit genau diesem
  Schlüssel bereits seit v1.31.0 — ein Einheitenproblem wäre vorbestehend, kein neues Risiko dieser
  Spec.

### 3 — Toggle Liste/Summiert, Summiert als Standard

Die Nav-Pills-Umschaltung „Liste" / „Summiert" existiert bereits in `Index.cshtml` (Z. 22-30) und
`Summiert.cshtml` (Z. 13-21). **`Summiert` ist der neue Standard-Einstieg** (Antwort 2): beide
Menüpunkte in `_Layout.cshtml` zeigen künftig auf `Summiert`. `Index` (Liste) bleibt über den
Umschalter erreichbar, um nachzuvollziehen, woher eine Summe kommt — ohne Parameter-Sonderfall oder
Redirect-Logik im Controller. Der Umschalter selbst nimmt `target`, KW (Pass-Through) und die
gemeinsamen Spaltenfilter mit (Abschnitt E).

### 4 — PDF: nur gefilterte Zeilen (bereits erfüllt), nur sichtbare Spalten (neu, DOM-basiert)

- **Gefiltert:** siehe Beleg oben — keine Änderung nötig.
- **Sichtbare Spalten:** Druck/PDF zeigen genau die Spalten, die auf dem Bildschirm **im Moment des
  Klicks** sichtbar sind (B1) — nicht den zuletzt gespeicherten (ggf. um bis zu 1,5 s veralteten oder
  wegen eines Speicherfehlers nie gespeicherten) Präferenz-Stand. Nur bei direkt aufgerufener
  Druck-/PDF-URL ohne `visibleColumns`-Parameter greift der Server-Rückfall auf die gespeicherte
  Präferenz.
- **Konsequenz, die zu dokumentieren ist:** Da die Spalten-Sichtbarkeit je Anwender unterschiedlich
  sein kann, erzeugen zwei Anwender für denselben HauptFA möglicherweise unterschiedliche PDFs. Für
  ein internes Arbeitsdokument ist das gewollt.

### 5 — Summiert-Ansicht drucken + kennzeichnen, Gruppen-Paging

Ein Druck/PDF der Summiert-Ansicht enthält die Aggregatzeilen (nicht die Einzelpositionen) und weist
das im Dokumentkopf **sichtbar** als „Summierte Ansicht" aus. Die Bildschirm-Ansicht wird — wie die
Liste — nach HauptFA gruppiert gerendert und paginiert (Seiteneinheit = HauptFA, keine Gruppe wird
über zwei Seiten getrennt, SOLLTE-5): Eine HauptFA, die auf dem Bildschirm risse, wäre störend, im
Druck ein handfester Fehler (fehlende Zeilen auf der Folgeseite).

### 6 — Matchcode + Hauptlagerplatz in der Summiert-Ansicht (B2)

Da `Summiert` zum Standard-Einstieg wird (Antwort 2), darf sie dem Kommissionierer nicht weniger
zeigen als die Liste: `Matchcode` (Locked, identifizierender Schlüssel wie in der Liste) und
`Hauptlagerplatz` werden als reine **Anzeigespalten** ergänzt (beide Artikeleigenschaften, innerhalb
eines Summenschlüssels konstant — `g.First()` genügt, der Aggregationsschlüssel bleibt unverändert
`(HauptFA, Artnr, Kommissionieren)`).

## Technischer Lösungsentwurf

### A — KW-Pflicht der Summiert-Ansicht entfällt (unbedingt)

`KommissionierListenService.AggregateSummiert` Schritt (1) lieferte bisher ohne `kwRange` ein leeres
Ergebnis (Z. 151-153, `AK N2d`). Dieser Frühausstieg entfällt ersatzlos (Antwort 4): ohne `kwRange`
werden **alle** HauptFA einbezogen (Verhalten identisch zum bereits etablierten Schwestermuster
`VormontageService.AggregateSummiert`, Sicht 2). Der KW-Filter bleibt als **optionale** zusätzliche
Einschränkung erhalten (Filterkarte unverändert). Die frühere Entscheidung `AK N2d` wird in
[[2026-07-29-standort-ideal-teil-3-spec]] als überholt vermerkt, mit Verweis auf diese Spec — nicht
stillschweigend geändert.

```csharp
// (1) KW waehlt die HauptFA optional ueber KO_Termin. null = keine Einschraenkung (Antwort 4).
var includedHauptFas = kwRange is null
    ? null
    : IsoWeekRange.HauptFasInRange(allInfos ?? Enumerable.Empty<FaHierarchyOrderInfo>(), o => o.KO_Termin, kwRange.Value);

var predicate = BuildFlagPredicate(targetValue);
var relevant = allNodes.Where(n => (includedHauptFas is null || includedHauptFas.Contains(n.HauptFA)) && predicate(n));
```

### B — Spalten-Sichtbarkeit in Druck/PDF: PrintBom-Muster mit Server-Rückfall (B1, korrigiert)

Der Erstentwurf hatte das `WarehousePickingPrintLayout`-Server-Muster gewählt („kein DOM-Scan wo ein
DB-Read reicht"). Die Kritische Prüfung (B1) zeigt: das Server-Muster liest die **gespeicherte**
Präferenz, die bis zu `SAVE_DELAY = 1500` ms hinter dem Bildschirm hinterherhinkt
(`column-preferences.js` Z. 20/297-302) — und bei einem stillen Speicherfehler (Z. 312, nur
`console.warn`) dauerhaft. Beides widerspricht der Anforderung „das PDF zeigt, was dieser Anwender
gerade sieht". **Entscheidung des Menschen: PrintBom-Weg (a).**

`print-visible-columns.js` (neu, gemeinsam für `Index.cshtml` und `Summiert.cshtml`) liest beim Klick
auf einen mit `data-print-link="<ViewKey>"` markierten Link die sichtbaren `data-col-key`-Spalten der
zugehörigen `table[data-view-key="<ViewKey>"]` aus dem DOM (identisches Muster zu
`Views/Picking/Bom.cshtml` Z. 1260-1267) und hängt sie als `visibleColumns`-Query-Parameter an
`a.href`, bevor der Browser der Verlinkung folgt:

```javascript
document.querySelectorAll('a[data-print-link]').forEach(function (a) {
    a.addEventListener('click', function () {
        var viewKey = a.getAttribute('data-print-link');
        var visible = [];
        document.querySelectorAll('table[data-view-key="' + viewKey + '"] thead th[data-col-key]')
            .forEach(function (th) {
                if (th.style.display !== 'none' && !th.classList.contains('d-none')) {
                    visible.push(th.getAttribute('data-col-key'));
                }
            });
        if (visible.length === 0) return;
        var url = new URL(a.href);
        url.searchParams.set('visibleColumns', visible.join(','));
        a.href = url.toString();
    });
});
```

Server-seitig (Controller) gilt: ist der Parameter gesetzt, wird er direkt verwendet (Split auf
`,`, PrintBom-Konvention); ist er leer (z. B. eine direkt aufgerufene/gebookmarkte Druck-URL ohne
vorherigen Klick auf der Seite), fällt der Server auf die gespeicherte Präferenz zurück — **ohne**
die ursprünglich geplante `WarehousePickingPrintLayout.ResolveColumns(prefs, defs)`-Überladung:

```csharp
private async Task<IReadOnlyList<string>> ResolveVisibleColumnsAsync(
    string? visibleColumns, string viewKey, IReadOnlyList<ColumnDef> defs)
{
    if (!string.IsNullOrWhiteSpace(visibleColumns))
        return visibleColumns.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

    var userId = _currentUserService.GetCurrentAppUserId();
    if (!userId.HasValue) return Array.Empty<string>();

    var pref = await _viewPrefs.GetByUserAndViewAsync(userId.Value, viewKey);
    var prefs = WarehousePickingPrintLayout.ParsePrefs(pref?.SettingsJson);
    if (prefs?.Columns == null || prefs.Columns.Count == 0) return Array.Empty<string>();

    var hidden = new HashSet<string>(prefs.Columns.Where(c => !c.Visible).Select(c => c.Key));
    return defs.Where(d => d.Locked || !hidden.Contains(d.Key)).Select(d => d.Key).ToList();
}
```

**Warum die geplante Überladung entfällt:** `ShowCol(key)` (`PrintBom.cshtml` Z. 196:
`!Model.VisibleColumns.Any() || Model.VisibleColumns.Contains(key)`) prüft nur **Mitgliedschaft**,
keine Reihenfolge — die Spaltenreihenfolge im Druck ist im `.cshtml` fest verdrahtet (jede Spalte hat
ihren eigenen `@if (ShowCol("..."))`-Block an fester Stelle). Der Server-Rückfall braucht also nur
eine sichtbar/unsichtbar-**Menge**, keine geordnete `PrintColumn`-Liste. `WarehousePickingPrintLayout`
bleibt dadurch **unverändert** — kein neuer Konsument, keine neue Überladung, kein
Regressionsrisiko für `WarehousePickingController.Print`.

**Strukturelle Auflösung von S3 (Kritische Prüfung):** Der Normalfall — ein Anwender klickt auf der
Seite auf „Drucken"/„PDF" — transportiert den DOM-Stand direkt, unabhängig vom 1,5-s-Entprell-Fenster
und unabhängig von einem stillen Speicherfehler. Divergenz zwischen Bildschirm und Druck bleibt nur
für direkt aufgerufene Druck-/PDF-URLs ohne `visibleColumns`-Parameter denkbar (Server-Rückfall) —
dort ist per Definition kein vorheriger Klick auf der Seite erfolgt, also auch kein „was ich gerade
sehe" zu erwarten. Das stille Speicherscheitern selbst bleibt eine offene, eigene Baustelle
([[2026-09-28-spaltenpraeferenz-speicherfehler-still]], out-of-scope).

`Print.cshtml`/neu `PrintSummiert.cshtml` bekommen denselben `ShowCol(key)`-Helfer wie
`PrintBom.cshtml`, gespeist aus `Model.VisibleColumns` (leere Liste ⇒ alle Spalten sichtbar).

### C — Neue Druck-/PDF-Wege für die Summiert-Ansicht (mit Gruppen-Paging)

`PrintSummiert`/`PdfSummiert` folgen dem Muster von `Print`/`Pdf`, aber auf
`KommissionierListenService.AggregateSummiert`-Zeilen statt Rohpositionen:

```csharp
public async Task<IActionResult> PrintSummiert(string? target, string? kwVon, string? kwBis, string? visibleColumns)
{
    var kwRange = IsoWeekRange.Resolve(kwVon, kwBis);
    var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
    var result = await _service.BuildSummiertAsync(target, kwRange, columnFilters, 1, int.MaxValue, paginate: false);

    var barcodes = result.Rows.Select(r => r.HauptFA).Distinct()
        .ToDictionary(h => h, h => _barcode.EncodeCode128Base64(h.ToString(CultureInfo.InvariantCulture)));

    var vm = new FaHierarchyKommissionierSummiertPrintViewModel
    {
        Rows = result.Rows,
        Barcodes = barcodes,
        SelectedTarget = target,
        IsFiltered = ColumnFilterHelper.HasAny(columnFilters) || !string.IsNullOrEmpty(target),
        VisibleColumns = await ResolveVisibleColumnsAsync(visibleColumns, "FaHierarchyKommissionierSummiert",
            ColumnDefinitions.FaHierarchyKommissionierSummiert.Columns)
    };
    return View(vm);
}
```

Die flachen Aggregatzeilen werden im `.cshtml` per `GroupBy(r => r.HauptFA)` für den Druck gruppiert
(kein neuer `FaHierarchyListGroupViewModel`-Umweg). `PdfSummiert` reduziert `result.Rows` auf genau
eine HauptFA (`Where(r => r.HauptFA == hauptFa)`, `NotFound()` wenn leer) und rendert dieselbe
`PrintSummiert.cshtml`. Schlägt die PDF-Erzeugung fehl (`PdfRenderException`), führt der Rückweg auf
`RedirectToAction(nameof(Summiert), new { target, kwVon, kwBis })` — **nicht** auf `Index` wie beim
Vorbild `Pdf` (SOLLTE-6: der fachlich richtige Rückfallort ist dieselbe Ansicht mit denselben
Parametern).

**Gruppen-Paging (SOLLTE-5):** `AggregateSummiert` paginiert **nicht mehr** auf Aggregatzeilen-Ebene,
sondern auf HauptFA-Gruppen — analog `FaHierarchyListBuilder.Build` (Schritt 4, „Seiteneinheit =
Gruppe, nie über Seiten getrennt"):

```csharp
// (5) Server-Spaltenfilter VOR dem Gruppen-Paging (wie im Baustein).
var filtered = ColumnFilterHelper.Apply(aggregates, columnFilters, SummiertColumnMap).ToList();
var totalRowCount = filtered.Count;

// (6) Gruppen-Paging: Seiteneinheit = HauptFA, keine Gruppe wird ueber Seiten getrennt.
var groupedByHauptFa = filtered.GroupBy(a => a.HauptFA).ToList(); // bereits nach HauptFA sortiert
var totalGroupCount = groupedByHauptFa.Count;
var pageGroups = paginate
    ? groupedByHauptFa.Skip(Math.Max(0, (page - 1) * pageSize)).Take(pageSize)
    : groupedByHauptFa;
var pageRows = pageGroups.SelectMany(g => g).ToList();

return new FaHierarchyKommissionierSummiertResult
{
    Rows = pageRows,
    TotalRowCount = totalRowCount,
    TotalGroupCount = totalGroupCount
};
```

`pageSize` bedeutet damit — wie bei der Liste — „Gruppen je Seite", nicht „Zeilen je Seite". Der
Controller setzt `Pagination.TotalCount = result.TotalGroupCount`. Bestehende Tests bleiben grün
(sie arbeiten mit genau einer HauptFA und `pageSize: 100`, also keine beobachtbare Änderung).

Die Bildschirm-Ansicht (`Summiert.cshtml`) rendert die Zeilen der aktuellen Seite ebenfalls
`GroupBy(r => r.HauptFA)`, mit eigenem `<tbody>` je Gruppe (wie `Index.cshtml`) und einem
PDF-Link je Gruppen-Kopfzeile (sofern `Model.PdfVerfuegbar`). Anders als die Liste führt die
Gruppen-Kopfzeile **keine** Auftrags-Kopfdaten (Montage-Abteilung/Kunde/AB-Nr./Termine) — die
Aggregatzeile kennt keine `OrderInfos`, ein Nachladen ist nicht angefordert (YAGNI).

### D — `ColumnDefinitions.FaHierarchyKommissionierSummiert`: 6 statt 4 Spalten (B2)

```csharp
public static readonly ViewConfig FaHierarchyKommissionierSummiert = new(
    "FaHierarchyKommissionierSummiert", "Kommissionierlisten — Summiert (IDEAL)",
    SupportsReorder: true, SupportsSortDefault: false)
{
    Columns =
    [
        new ColumnDef("hauptfa",        "HauptFA",         Locked: true),
        new ColumnDef("artnr",          "Artnr.",          Locked: false),
        new ColumnDef("matchcode",      "Matchcode",       Locked: true),
        new ColumnDef("kommissionieren","Kommissionieren", Locked: false),
        new ColumnDef("hauptlagerplatz","Hauptlagerplatz", Locked: false),
        new ColumnDef("sollmenge",      "Summe Sollmenge", Locked: false, DefaultWidth: 55),
    ]
};
```

`SupportsSortDefault: false` (nicht `true` wie die flache `FaHierarchyVormontageSummiert`) — die
Ansicht wird seit SOLLTE-5 wie `FaHierarchyKommissionierListen` gruppiert gerendert. Registrierung in
`GetByViewKey` behebt den Nebenbefund: ohne sie lehnt `UserViewPreferencesApiController` Lesen/
Schreiben von Präferenzen für die Summiert-Ansicht ab.

`SummiertColumnMap` (Service) und `#column-config`/`<th data-col-key>` (`Summiert.cshtml`) sowie
`ShowCol(...)` (`PrintSummiert.cshtml`, dort ohne `hauptfa` — Gruppen-Header) müssen exakt dieselben
6 (bzw. 5) Schlüssel tragen — abgesichert durch den neuen Drift-Guard-Test (SOLLTE-1).

### E — Umschalter Liste↔Summiert nimmt Filter mit (SOLLTE-7, abweichend vom Prüfvorschlag)

Der ursprüngliche Prüfvorschlag („`target` bleibt, Spaltenfilter/KW werden bewusst zurückgesetzt")
wird **nicht** übernommen — Entscheidung des Menschen: Der Umschalter nimmt `target`, KW **und** die
Spaltenfilter der auf **beiden** Ansichten existierenden Spalten mit. Begründung: Antwort 4 stützt
sich auf gleichbleibendes Filterverhalten zwischen den Ansichten („eine Summenansicht, die beim
Umschalten plötzlich anders filtert, wäre genau die Inkonsistenz, die ausgeschlossen werden soll").
Ein stilles Zurücksetzen widerspräche dem.

**Gemeinsame Spalten (nach B2):** `hauptfa`, `artnr`, `matchcode`, `kommissionieren`,
`hauptlagerplatz` — deren `colf_*`-Filter werden 1:1 mitgenommen.

**`sollmenge` wird NICHT mitgenommen**, obwohl der Schlüssel in beiden `ColumnMap`s existiert (am
Code geprüft: `KommissionierListenService.cs`, `ColumnMap["sollmenge"]` vs. `SummiertColumnMap
["sollmenge"]"`, beide `N3`-formatiert). Der Filterwert bedeutet in der Liste „Sollmenge dieser
EINEN Position", in der Summiert-Ansicht „Summe der Sollmenge über N Positionen" — dieselbe
Filtersyntax auf einer anderen Grundmenge. Ein Anwender, der in der Liste nach `sollmenge=5` filtert
und beim Umschalten denselben Filter auf die Summenspalte übertragen bekäme, würde eine andere
Aussage erhalten, als er getippt hat, ohne dass das sichtbar würde — genau die Art von stillem
Bedeutungswechsel, die dieses Projekt ausdrücklich vermeidet (`melden statt still behandeln`). Im
Zweifel nicht mitnehmen: `sollmenge` entfällt beim Wechsel.

**Spalten nur einer Ansicht** (Liste: `hauptartnr`, `arbeitsbereich`, `artikeltyp`, `beschichtet`,
`material`) entfallen naturgemäß — es gibt in der Zielansicht keine solche Spalte, auf die ein Filter
wirken könnte.

`target` und KW werden **immer** mitgenommen (KW als reiner Pass-Through Richtung Liste, siehe
KONFLIKT-Rückfrage 5 — die Liste hat aktuell keinen KW-Parameter, der ihn auswerten würde).

Umsetzung: `KommissionierListenService.ToggleSharedQueryKeys` (die erlaubten Query-Keys) +
`BuildToggleQuery(HttpRequest?)` (liest `Request.Query`, filtert, baut den Query-String) — ein
gemeinsamer, kleiner Helfer statt einer doppelt gepflegten Key-Liste in zwei `.cshtml`-Dateien.

### F — JSON-Einbettung Pflicht für Filterwerte/Spaltenlisten in Script-Blöcken

`fallstricke.md` („Freitext nie per `@Html.Raw(...)` in ein JS-Literal", Muster
`@Html.Raw(System.Text.Json.JsonSerializer.Serialize(wert ?? ""))`) gilt uneingeschränkt für diese
Spec. Der aktuelle Entwurf **benötigt** keine solche Einbettung: Der Click-Handler
(`print-visible-columns.js`) liest ausschließlich `data-col-key`-Attribute (feste, entwicklerdefinierte
Bezeichner) aus dem DOM und ergänzt eine bereits Razor-kodierte `href` um einen weiteren
Query-Parameter — Filterwerte/Freitext (`target`, `colf_*`) wandern nirgends in einen `<script>`-Block,
sondern bleiben in HTML-Attributen (`asp-route-*`/`Url.Action`), wo Razor automatisch
HTML-attribut-kodiert. **Sollte im Dev-Lauf doch eine Stelle entstehen, die einen Filterwert oder eine
Spaltenliste in ein `<script>`-Literal einbettet** (z. B. eine künftige Fehlermeldung mit dem
gewählten Ziel-Text), gilt zwingend das JSON-Muster aus `fallstricke.md` — kein Bau einer neuen,
ungeschützten Stelle. AK 18 macht das testbar.

## Migrations-/SQL-Auswirkungen

**Keine.** Es entstehen keine neuen Tabellen/Spalten, keine EF-Migration, kein `SQL/XX_*.sql`, keine
Änderung an `SQL/00_FreshInstall.sql`. `FaHierarchyNode` bleibt unverändert (Falle 2 wird
dokumentiert, nicht durch ein neues Feld gelöst). `UserViewPreference` (bereits bestehende Tabelle)
wird von dieser Spec nur **gelesen**, nicht erweitert. `ColumnDefinitions` ist reiner In-Memory-Code,
kein Datenbankobjekt.

## Audit-Feld-Auswirkungen

**Keine.** `FaHierarchyNode` ist kein `AuditableEntity` (reine Sync-Cache-Projektion) — es wird
ohnehin nur gelesen, nicht geschrieben. `UserViewPreference` wird durch diese Spec ausschließlich
gelesen (das Schreiben passiert weiterhin ausschließlich über `UserViewPreferencesApiController`,
unverändert). Es entsteht kein neuer Schreibpfad.

## Akzeptanzkriterien

1. In der Kommissionierlisten-Summiert-Ansicht erscheint je `(HauptFA, Artnr, Kommissionieren)`
   genau eine Zeile mit der Summe aller `Sollmenge`-Werte der zugrunde liegenden Positionen.
2. Kommt derselbe Artikel am selben HauptFA mit **zwei unterschiedlichen** Kommissionierzielen vor,
   bleiben es **zwei** Aggregatzeilen (Falle 1).
3. Zwei unterschiedliche Artikelnummern mit **gleichem** Matchcode werden **nicht** zu einer Zeile
   zusammengefasst (Falle 3, Aggregation über `Artnr`).
4. Ein Aufruf der Kommissionierlisten-Menüpunkte (beide Stellen in `_Layout.cshtml`) ohne weitere
   Parameter führt direkt zur **Summiert**-Ansicht (Antwort 2).
5. Der Umschalter Liste↔Summiert bleibt in beide Richtungen erreichbar und nimmt `target`, KW
   (Pass-Through Richtung Liste) sowie die Spaltenfilter der gemeinsamen Spalten (`hauptfa`, `artnr`,
   `matchcode`, `kommissionieren`, `hauptlagerplatz`) mit; ein Filter auf `sollmenge` oder einer
   nur-in-einer-Ansicht existierenden Spalte entfällt beim Wechsel (Abschnitt E).
6. Ein Druck/PDF der (Liste-)Kommissionierliste zeigt **ausschließlich** die Spalten, die im Moment
   des Klicks auf dem Bildschirm sichtbar sind (Kopf **und** Zellen) — auch wenn die letzte
   Zahnrad-Änderung weniger als 1,5 s zurückliegt (B1, umgeht die Speicherverzögerung strukturell).
7. Wird eine Druck-/PDF-URL **ohne** `visibleColumns`-Parameter direkt aufgerufen (kein vorheriger
   Klick auf der Seite) und existiert eine gespeicherte Spalten-Präferenz, zeigt der Druck/PDF genau
   die dort als sichtbar markierten Spalten (Server-Rückfall).
8. Hat ein Anwender keine gespeicherte Spalten-Präferenz (oder ist nicht eingeloggt erkennbar) und
   ruft die URL ohne `visibleColumns`-Parameter auf, zeigt der Druck/PDF **alle** Spalten.
9. Ein Druck/PDF der Summiert-Ansicht zeigt die Aggregatzeilen (nicht die Rohpositionen) und trägt im
   Kopf sichtbar den Hinweis „Summierte Ansicht".
10. Ist zusätzlich ein Spaltenfilter oder ein Ziel-Filter aktiv, zeigt derselbe Druck/PDF **beide**
    Hinweise („Gefilterte Ansicht" und „Summierte Ansicht") nebeneinander.
11. Der bestehende Filter-Durchschlag (Spaltenfilter + Ziel) im Druck/PDF der Liste bleibt unverändert
    funktionsfähig (Regressionscheck).
12. `ColumnDefinitions.GetByViewKey("FaHierarchyKommissionierSummiert")` liefert einen `ViewConfig` mit
    genau 6 Spalten (`hauptfa`, `artnr`, `matchcode`, `kommissionieren`, `hauptlagerplatz`,
    `sollmenge`); Speichern einer Spalten-Präferenz für diese Ansicht über
    `/api/userviewpreferences` gelingt.
13. Die Summiert-Ansicht liefert **ohne** Kalenderwochen-Eingabe die Summe über **alle** HauptFA
    (nicht mehr das leere „Bitte KW eingeben"-Ergebnis); mit KW-Eingabe filtert sie wie bisher auf die
    HauptFA der gewählten Woche.
14. `fallstricke.md` enthält einen Eintrag, der das Fehlen eines Mengeneinheit-Felds an
    `FaHierarchyNode` dokumentiert, mit der korrigierten Quelle (`vw_IDEAL-AKE_Kommissionierung_FAListe`)
    und der Artikeleigenschafts-Begründung (Antwort 3, B3).
15. Bei mehr HauptFA-Gruppen als die eingestellte Seitengröße erlaubt, reißt keine HauptFA über zwei
    Seiten — weder auf dem Bildschirm der Summiert-Ansicht noch im seitenweisen Druck (SOLLTE-5).
16. Schlägt die PDF-Erzeugung für `PdfSummiert` fehl, kehrt der Anwender zur **Summiert**-Ansicht mit
    denselben Query-Parametern (`target`, `kwVon`, `kwBis`) zurück, mit `WarningMessage` (SOLLTE-6).
17. Für `FaHierarchyKommissionierListen` UND `FaHierarchyKommissionierSummiert` sind die Spaltenschlüssel
    aus `ColumnDefinitions`, `#column-config`, `<th data-col-key>` und `ShowCol(...)` der jeweiligen
    Druckvorlage identisch — ein automatisierter Drift-Guard-Test schlägt fehl, sobald das nicht mehr
    stimmt (SOLLTE-1).
18. Ein Kommissionier-Ziel- oder Spaltenfilterwert mit Anführungszeichen bzw. `</script>`-artigem
    Inhalt (z. B. `LAGER");</script><script>alert(1)` als Testwert) bricht weder die Bildschirmansicht
    noch Druck/PDF — es entsteht keine neue Einbettung eines Filterwerts in einen `<script>`-Block
    ohne JSON-Kodierung (Abschnitt F).
19. `Summiert.cshtml` zeigt `Matchcode` und `Hauptlagerplatz` als zusätzliche Spalten (Bildschirm,
    Druck und PDF), ohne dass sich die Zeilenanzahl oder der Aggregationsschlüssel ändert — die
    bestehenden Aggregations-Tests (`Summiert_SumsSollmenge_PerHauptFaArtnrZiel` u. a.) bleiben
    unverändert grün (B2).
20. `WarehousePickingController.Print`/`WarehousePickingPrintLayout` sind von dieser Spec
    **unberührt** — keine neue Überladung, kein Verhaltenswechsel (Regressionscheck zu Abschnitt B).

## Test-Szenarien

Neues Kapitel `TS-80` in `docs/TESTSZENARIEN.md` (Stand 2026-09-28 im Worktree: höchstes Kapitel
`TS-79`, bereits vergeben durch [[2026-09-25-kommissionierung-nur-hauptfa-spec]], Testbereit; der
Dev-Lauf prüft die Nummer erneut gegen den dann aktuellen Stand). Skizze der Szenarien:

- **TS-80.1 Summierung Grundfall:** HauptFA mit derselben Artikelnummer auf zwei Positionen desselben
  Ziels → Summiert-Ansicht zeigt eine Zeile mit der Summe; Liste-Ansicht zeigt weiterhin beide
  Einzelpositionen.
- **TS-80.2 Falle 1 (Ziel bleibt erhalten):** dieselbe Artikelnummer, zwei unterschiedliche
  Kommissionierziele → zwei Aggregatzeilen, keine Vermischung der Ziele.
- **TS-80.3 Falle 3 (Artikelnummer statt Matchcode):** zwei Artikelnummern mit identischem Matchcode
  → zwei getrennte Aggregatzeilen.
- **TS-80.4 Standard-Einstieg:** Menüpunkt „Kommissionierlisten" (beide Stellen) führt ohne weitere
  Eingabe direkt zur Summiert-Ansicht.
- **TS-80.5 Umschalter nimmt Filter mit:** In der Liste `target` + Spaltenfilter auf `artnr` und
  `hauptlagerplatz` setzen, zu Summiert wechseln → beide Filter + `target` sind noch aktiv; einen
  zusätzlich in der Liste gesetzten Spaltenfilter auf einer Liste-exklusiven Spalte (z. B.
  `arbeitsbereich`) setzen → dieser Filter ist nach dem Wechsel weg (kein Fehler, keine Fehlermeldung
  nötig, stille aber dokumentierte Grenze).
- **TS-80.6 PDF Spalten-Sichtbarkeit — Spalte ausblenden und SOFORT drucken:** Anwender blendet über
  das Zahnrad eine Spalte aus und klickt **innerhalb von 1,5 s** auf Drucken/PDF → das Dokument zeigt
  die Spalte **nicht** (Nachweis, dass B1 die Speicherverzögerung umgeht).
- **TS-80.7 PDF ohne Präferenz/ohne Parameter:** Druck-/PDF-URL wird direkt (ohne vorherigen Klick,
  ohne `visibleColumns`) aufgerufen, Anwender hat keine gespeicherte Präferenz → alle Spalten
  erscheinen.
- **TS-80.8 PDF ohne Parameter, MIT gespeicherter Präferenz:** wie 80.7, aber mit gespeicherter
  Präferenz → genau die dort sichtbaren Spalten erscheinen (Server-Rückfall).
- **TS-80.9 PDF Filter-Durchschlag (Regression):** Spaltenfilter + Ziel-Filter aktiv → PDF zeigt nur
  die gefilterten Zeilen, Banner „Gefilterte Ansicht" mit Ziel-Text.
- **TS-80.10 PDF Summiert:** Druck/PDF der Summiert-Ansicht → Aggregatzeilen, Banner „Summierte
  Ansicht"; kombiniert mit aktivem Filter zeigt der Kopf beide Banner.
- **TS-80.11 Ohne KW:** Summiert-Ansicht ohne KW-Eingabe zeigt die Summe über alle HauptFA statt des
  früheren Hinweistexts; mit KW-Eingabe filtert sie wie bisher.
- **TS-80.12 Gruppen-Paging:** Mehr HauptFA als die eingestellte Seitengröße → keine HauptFA-Gruppe
  reißt über zwei Bildschirmseiten; derselbe Effekt im seitenweisen Ausdruck.
- **TS-80.13 PdfSummiert-Fehler-Rückweg:** PDF-Rendering künstlich zum Scheitern bringen (z. B. Edge
  nicht verfügbar) → Anwender landet auf der Summiert-Ansicht mit denselben Filterwerten, sichtbare
  Warnmeldung.
- **TS-80.14 Nebenbefund:** Spalten-Präferenz für die Summiert-Ansicht (inkl. Matchcode/
  Hauptlagerplatz) lässt sich speichern und wirkt auf Bildschirm UND Druck.
- **TS-80.15 Regression Vormontage/Beschichtung:** Teil 4/5 unverändert funktionsfähig.
- **TS-80.16 Einheit-Prüfschritt (Vorbedingung der Abnahme, kein UI-Test):** Auf der Sage-DB IDEAL
  `EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAListe'` ausführen, die dort gelesene
  Positionstabelle ermitteln, anschließend `SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE
  TABLE_NAME = '<Positionstabelle>' AND COLUMN_NAME LIKE '%einheit%'` — kein Treffer bestätigt Falle 2
  als strukturell erledigt; ein Treffer wird gemeldet, nicht still mitgelöst (B3).
- **TS-80.17 Skript-Sicherheit:** ein Kommissionier-Ziel-Testwert mit `</script>`-artigem Inhalt
  bricht weder Seite noch Druck/PDF (AK 18).

## Deploy

- **Web-App: ja.** Neue/geänderte Controller-Actions, ViewModels, `ColumnDefinitions`-Eintrag, ein
  neues JS (`print-visible-columns.js`), zwei geänderte Views (`Print.cshtml`, `Summiert.cshtml`,
  `Index.cshtml`), eine neue View (`PrintSummiert.cshtml`), `_Layout.cshtml`. Alle Änderungen unter
  `IdealAkeWms/`.
- **Service: nein.** Kein Service-seitiger Code betroffen (nur Versions-Bump in `AppVersion.cs` laut
  Checkliste).
- **Migration: nein.** Kein Schema-Impact (siehe Migrations-/SQL-Auswirkungen).
- **Betriebs-Vorbedingung — Einheit-Prüfschritt VOR der Abnahme (B3, nicht blockierend für den
  Dev-Lauf, aber Bedingung für `Testbereit`→Abnahme):** Auf der Sage-DB IDEAL
  `EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAListe'` und anschließend
  `SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '<dort gefundene
  Positionstabelle>' AND COLUMN_NAME LIKE '%einheit%'` ausführen. Kein Treffer: Fallstrick-Eintrag
  bleibt wie dokumentiert. Treffer: melden, nicht still weiterbauen.
- **Publish-Befehl (im Worktree, Mensch-Flow: Worktree → Testsystem → Testen → danach Merge):**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  ```
  Nach dem Merge nach `main`: nur erneut publishen, falls der Merge tatsächlich getestete Dateien mit
  parallelen `main`-Änderungen zusammengeführt hat.

## Offene Rückfragen

> **Rückfragen 1-4: ALLE BEANTWORTET** (Freigabe-Antworten unten, Schranke 1 für diesen Teil erledigt;
> Antwort 3 trägt seit 2026-09-28 einen Korrektur-Vermerk zur Quelle, siehe dort). Dieser Abschnitt
> ist ab hier **Protokoll**, keine offene Entscheidung mehr — mit EINER neuen Ausnahme (5.), die die
> Kritische Prüfung vom 2026-09-25 nicht kennen konnte, weil sie erst bei der Umsetzung der
> Umschalter-Filterübernahme (Antwort 4 / SOLLTE-7) am Code sichtbar wurde.

1. ~~Summierschlüssel bestätigen~~ → Antwort 1: bestätigt, unverändert.
2. ~~Toggle-Standard~~ → Antwort 2: „Summiert" wird Standard. (Die ursprüngliche *Einschätzung* in
   diesem Abschnitt „Liste bleibt Standard, solange Rückfrage 4 nicht mit lockern beantwortet ist"
   ist damit gegenstandslos — Rückfrage 4 wurde mit „lockern" beantwortet, Antwort 2 zieht die
   Konsequenz.)
3. ~~Mengeneinheit (Falle 2)~~ → Antwort 3: akzeptiert als strukturell erledigt (mit Prüfschritt),
   **Quelle 2026-09-28 korrigiert**: `vw_IDEAL-AKE_Kommissionierung_FAListe`, nicht
   `KHKPpsRessourcenPositionen`.
4. ~~KW-Pflicht lockern~~ → Antwort 4: gelockert, KW bleibt optionaler Filter.

5. **KONFLIKT (neu, 2026-09-28):** Antwort 4 verlangt, dass der Umschalter Liste↔Summiert `target`,
   KW und die gemeinsamen Spaltenfilter mitnimmt. Am Code verifiziert:
   `FaHierarchyKommissionierListenController.Index` (Liste) hat **keinen** KW-Parameter — nur
   `Summiert` kennt `kwVon`/`kwBis`. Eine beim Wechsel Summiert→Liste aktive KW kann in der Liste
   nirgends wirken. *Zwei Wege:* (a) die Liste bekommt selbst einen optionalen KW-Filter
   (Scope-Zuwachs, aber ehrlich: KW wirkt dann auch dort), oder (b) KW wird beim Wechsel zur Liste nur
   als toter Pass-Through-Query-Parameter mitgeführt (kein Codeeingriff in `Index`, KW wird erst beim
   Zurückwechseln zu `Summiert` wieder wirksam — diese Spec entwirft vorerst (b), siehe Abschnitt E/
   Out-of-Scope). *Einschätzung:* (b) ist der kleinere, nicht angeforderte Diff, wirkt aber
   inkonsistent, falls ein Anwender die URL inspiziert. **Menschliche Entscheidung nötig.**

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → **Bestaetigt: `(HauptFA, Artnr, Kommissionieren)` bleibt, keine Aenderung.** Mehrere klar getrennte
   Zeilen je Ziel sind fuer den Kommissionierer lesbarer als eine Zeile mit Aufschluesselung.
   **Ausdruecklich festgehalten:** Summiert wird **je HauptFA**, nicht ueber Auftraege hinweg. Das ist
   gewollt und passt zu [[2026-09-25-kommissionierung-nur-hauptfa-spec]]: Kommissioniert wird je
   HauptFA, also braucht der Kommissionierer die Summe **seines** Auftrags. Eine auftragsuebergreifende
   Summe waere Sammelkommissionierung — nicht angefordert, eigener Umfang.

2. → **"Summiert" wird Standard** (setzt Antwort 4 voraus). Die Anforderung lautet, mehrfach vorkommende
   Artikel zu summieren — das ist die Ansicht, die der Kommissionierer im Alltag braucht. Die Liste
   bleibt ueber den Umschalter erreichbar, um nachzuvollziehen, **woher** eine Summe kommt.
   **Umsetzung ohne Weiterleitungslogik:** Der Menuepunkt zeigt kuenftig auf `/Summiert`, `Index` bleibt
   unveraendert per Umschalter erreichbar. Kein Parameter-Sonderfall, keine Redirect-Action.

3. → **Akzeptiert — aber mit einer staerkeren Begruendung als "vermutlich klein".** Die Mengeneinheit
   ist in Sage eine **Artikeleigenschaft** (`KHKArtikel.Lagermengeneinheit`, im WMS `Article.Unit`). Der
   Summierschluessel enthaelt die **Artikelnummer**. Zwei Zeilen desselben Summenschluessels sind damit
   **derselbe Artikel mit derselben Einheit** — eine Vermischung ist nicht "unwahrscheinlich", sondern
   durch den Schluessel **ausgeschlossen**, solange Stuecklistenpositionen keine vom Artikel
   abweichende Positionseinheit fuehren.
   **Diese Bedingung einmal belegen statt annehmen** (Dev-Lauf oder Mensch): Fuehrt
   `KHKPpsRessourcenPositionen` eine eigene Mengeneinheits-Spalte, die von der Lagermengeneinheit des
   Artikels abweichen kann? Falls **nein**: Falle 2 ist strukturell erledigt, der Fallstrick-Eintrag
   haelt die Begruendung fest. Falls **ja**: Die Spalte gehoert in den Summierschluessel — dann
   Rueckmeldung, nicht still weiterbauen.
   **Kein neues Einheit-Feld** an `FaHierarchyNode` in dieser Spec.

   > **Korrektur (2026-09-28 laut Antworten auf die Kritische Pruefung):** Die hier genannte Tabelle
   > `KHKPpsRessourcenPositionen` ist die **falsche** Quelle — sie ist die AKE-Stuecklistenquelle
   > (`SageImportService.cs`, `SQL/AgentJobs/02_Import_Artikel.sql`). `FaHierarchyNode` stammt aus der
   > IDEAL-Sage-View **`vw_IDEAL-AKE_Kommissionierung_FAListe`** (`FaHierarchyNode.cs`,
   > `FaHierarchySyncService.cs`). Der oben beschriebene Pruefschritt (Frage: "eigene, abweichende
   > Positionseinheit?") bleibt inhaltlich gueltig, muss aber auf die tatsaechliche View gerichtet
   > werden: `EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAListe'` + `INFORMATION_SCHEMA.COLUMNS`
   > auf die dort gelesene Positionstabelle (siehe Fachliche Anforderung 2 + Deploy-Abschnitt). Diese
   > Korrektur blockiert die Umsetzung NICHT (B3) — die bestehende Summiert-Ansicht summiert bereits
   > seit v1.31.0 mit demselben Schluessel, ein Einheitenproblem waere vorbestehend.

4. → **Lockern, wie eingeschaetzt.** Ohne KW alle HauptFA, die KW bleibt als optionaler Filter.
   **Warum die Umkehr hier vertretbar ist:** Weil je HauptFA summiert wird, entstehen ohne KW keine
   sinnlosen woechentlichen Gesamtsummen — jede Zeile bleibt eine Summe innerhalb eines Auftrags. Und
   erst dadurch werden Liste und Summenansicht **symmetrisch**: Die Liste zeigt ohne KW alle HauptFA; eine
   Summenansicht, die beim Umschalten ploetzlich leer wird oder anders filtert, waere genau die
   Inkonsistenz, die TS-x.4 ("Umschalten behaelt den Filter") ausschliessen will. Vorbild ist die
   Vormontage-Sicht 2, wo die KW bereits optional ist.
   **Die fruehere Entscheidung (AK N2d) als ueberholt kennzeichnen**, mit Verweis auf diese Spec — nicht
   stillschweigend aendern.

**Reihenfolge:** Diese Spec **nach** [[2026-09-25-kommissionierung-nur-hauptfa-spec]] umsetzen — jene
ist inzwischen Testbereit (v1.46.0), die Bedingung ist damit erfüllt. Beide ändern
`KommissionierListenService.cs`; die Summenansicht setzt auf dem bereits extrahierten
`KommissionierRelevanzFilter`-Stand auf.

## Kritische Pruefung (2026-09-25)

> Anwalt-des-Teufels-Durchsicht **vor** dem Dev-Lauf, am Code des Buendel-Worktrees
> `feature/2026-08-07-ideal-teile-1-5` gegengeprueft. Die vier Freigabe-Antworten sind vollstaendig und
> untereinander widerspruchsfrei — aber **keine** davon steht im Rumpf, und zwei Antworten treffen am
> Code auf Stellen, die der Entwurf nicht kennt.

### Die fuenf Schwerpunkte des Menschen

**S1 — Bedingte Stellen, die durch Antwort 2 + 4 unbedingt werden: NICHT vollstaendig erfasst.** Die
beiden genannten (TS-x.4, TS-x.9) sind nur ein Teil. Vollstaendige Liste der Stellen, die der
Rumpf-Nachzug aendern muss:
- Frontmatter `affected_code` (KommissionierListenService-Eintrag): „KW-Pflicht … **auf Rueckfrage 2**
  abhaengig lockern" — **falsche Nummer** (gemeint ist Rueckfrage 4); jetzt unbedingt.
- Frontmatter `affected_code` (Summiert.cshtml-Eintrag): „… ODER nur EIN seitenweiter Drucken-Link ohne
  Gruppen-PDF, **siehe Rueckfrage 4**" — eine offene Designwahl, die Rueckfrage 4 (KW) gar nicht
  beantwortet (siehe SOLLTE-5).
- Frontmatter `open_questions`: noch alle vier gefuellt — auf `[]` setzen bzw. nach
  `beantwortete_rueckfragen` verschieben.
- In-Scope Punkt 2 („ueberpruefen/lockern … Rueckfrage 4") → „lockern".
- Fachliche Anforderung 3 („Offen ist nur, ob `Index` … Standard bleibt") → „`Summiert` ist Standard,
  Menue zeigt auf `/Summiert`" (Antwort 2).
- Technischer Entwurf A: beide Zweige („Wird Rueckfrage 4 mit … beantwortet") → nur der Lockern-Zweig;
  der Satz zum „dritten, eigenen Endpunkt" entfaellt.
- Offene Rueckfrage 2, *Einschaetzung* „‚Liste' bleibt Standard" — widerspricht jetzt Antwort 2;
  Abschnitt zum Protokoll machen („ALLE BEANTWORTET"), wie in anderen Specs.
- **AK 12** („Nur falls Rueckfrage 4 …") → unbedingt. **AK 13** („Nur falls Rueckfrage 3 …") → unbedingt,
  aber inhaltlich anders formuliert (siehe S2: Begruendung „Einheit ist Artikeleigenschaft, Schluessel
  enthaelt die Artnr" statt „Nicht-Pruefbarkeit").
- **Neues AK fehlt fuer Antwort 2:** „Der Menuepunkt ‚Kommissionierlisten' oeffnet `/Summiert`" — heute
  gibt es keines.
- **TS-x.4** (Toggle-Default) und **TS-x.9** (ohne KW) → unbedingt.

**S2 — `KHKPpsRessourcenPositionen`: die Frage trifft die falsche Tabelle.** Am Code belegt:
- `FaHierarchyNode` stammt **nicht** aus `KHKPpsRessourcenPositionen`, sondern aus der IDEAL-Sage-View
  `vw_IDEAL-AKE_Kommissionierung_FAListe` (`FaHierarchyNode.cs:6`, `FaHierarchySyncService.cs:53`,
  `ServiceSettingDefinitions.cs:38`). `KHKPpsRessourcenPositionen` ist die **AKE**-Stuecklistenquelle
  (`SQL/AgentJobs/02_Import_Artikel.sql:38`, `[ake].[dbo]`; `SageImportService.cs:392`).
- Wo das Repo eine Einheit liest, kommt sie **immer** aus `KHKArtikel.Lagermengeneinheit`
  (`SageImportService.cs:387/403` → `Article.Unit`), nie aus einer Positionsspalte. Das ist ein Indiz,
  **kein** Beleg: weder das Sage-Schema noch die Definition der IDEAL-View liegen im Repo.
- **Nicht am Code pruefbar → offener Pruefschritt (Mensch, Sage-DB IDEAL):**
  (a) `EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAListe'` — aus welcher Positionstabelle liest die
  View `Sollmenge`, und fuehrt sie dort eine Positions-Einheit mit?
  (b) `SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '<Positionstabelle aus (a)>'
  AND COLUMN_NAME LIKE '%einheit%'`.
  Kein Treffer ⇒ Antwort 3 greift („strukturell erledigt"), AK 13 = Fallstrick-Eintrag mit dieser
  Begruendung. Treffer ⇒ Rueckmeldung, nicht weiterbauen (Antwort 3 wortwoertlich). **Der Dev-Lauf kann
  das nicht selbst** (keine Sage-Verbindung aus der Umsetzung) — siehe B3.

**S3 — Sitzungs-Spalten vs. gespeicherte Praeferenz: einen Sitzungs-Modus gibt es nicht, aber zwei echte
Divergenz-Faelle.** `column-preferences.js` speichert jede Zahnrad-Aenderung automatisch per `PUT`
(`scheduleSave` Z.297-302, `SAVE_DELAY = 1500` Z.20). Trotzdem kann das PDF vom Bildschirm abweichen:
1. **Entprell-Fenster:** Spalte ausblenden und innerhalb von 1,5 s auf Drucken/PDF klicken ⇒ die DB hat den
   alten Stand ⇒ PDF zeigt die Spalte noch.
2. **Stiller Speicherfehler:** `saveSettings` meldet Fehler nur per `console.warn` (Z.312). Schlaegt das
   Speichern fehl (Netz — **oder unregistrierter viewKey, genau Nebenbefund D fuer Summiert**), sieht der
   Anwender seine Spalten, das PDF aber den gespeicherten Alt-/Standardstand.

**`PrintBom` ist davon frei:** Es liest die sichtbaren `<th data-col-key>` **zur Klickzeit aus dem DOM** und
haengt sie als `visibleColumns` an — der Druck ist per Konstruktion identisch mit dem Bildschirm, gespeichert
oder nicht. Das Server-Muster (`WarehousePickingPrintLayout`) ist also **nicht** gleichwertig: es tauscht
„kein JS" gegen „PDF = gespeicherter Stand". Das Argument des Entwurfs („die Links sind serverseitige
`<a href>`") begruendet den kleineren Diff, nicht das richtige Verhalten. → **B1.**
Positiv bestaetigt: Die Identitaet stimmt — `Pdf` rendert `Print` **in-process** im Request des Anwenders
(`_viewRenderer.RenderToStringAsync`, Controller ~Z.170), kein Edge-Aufruf einer URL; `GetCurrentAppUserId()`
liefert also den richtigen Benutzer.

**S4 — Abhaengigkeit zur HauptFA-Spec: der Rumpf behauptet das Gegenteil des Codes.** Der Info-Kasten sagt
„**andere** Methoden … **keine Zeilenueberschneidung**". Am Code: `AggregateSummiert` ruft
`BuildFlagPredicate(targetValue)` **direkt** auf (`KommissionierListenService.cs`, Schritt 3, ~Z.160). Die
Freigabe-Antworten (Abschnitt „Reihenfolge") haben das richtig erkannt, der Rumpf nicht. Entschaerfend: Die
HauptFA-Spec (jetzt `InUmsetzung`) laesst `BuildFlagPredicate` als Signatur **bestehen** und delegiert nur an
`KommissionierRelevanzFilter.IsRelevant` (dort `affected_code`, Z.14). Die Summenansicht erbt die Regel damit
**automatisch** — keine eigene Aenderung an `AggregateSummiert` noetig, aber dieselbe Datei ⇒ Reihenfolge
einhalten. → SOLLTE-8.

**S5 — View-Key-Nachtrag: ja, und zwar vier Stellen, nicht eine.** `fallstricke.md` §3 („`data-col-key` ist
Pflicht", „Neuer viewKey ohne `GetByViewKey`-Registrierung → Prefs-API 400") und §10 („Neue Spalte … braucht
ZWEI Registrierungen": `#column-config` ist die **Client-Wahrheit**, `ColumnDefinitions` nur die
viewKey-Validierung) plus die Lagerbestellungs-Druck-Regel („drei Stellen synchron"). Heute passen
`<th data-col-key>` (Summiert.cshtml Z.79-82) und `#column-config` (Z.116-119) zusammen: 4 Keys
`hauptfa/artnr/kommissionieren/sollmenge`. Der Entwurf fuegt zwei Stellen hinzu — den neuen
`ColumnDefinitions.FaHierarchyKommissionierSummiert`-Eintrag **und** die `ShowCol`-Keys in
`PrintSummiert.cshtml` (analog `Print.cshtml` gegen die 11 Keys von `FaHierarchyKommissionierListen`).
→ SOLLTE-1.

### BLOCKER

**B1 — Druck-Spalten: Server-Praeferenz oder DOM-Stand? (Frage an den Menschen)** Der Entwurf waehlt das
Server-Muster; es liefert in den zwei Faellen aus S3 ein PDF, das **anders** aussieht als der Bildschirm —
genau das, was die Anforderung ausschliesst („das PDF zeigt, was dieser Anwender gerade sieht").
**Frage:** (a) `PrintBom`-Muster — sichtbare `<th data-col-key>` beim Klick per kleinem JS an die
Druck-/PDF-URL haengen; der Server nutzt den Parameter und faellt ohne ihn auf die gespeicherte Praeferenz
zurueck — oder (b) Server-Muster wie entworfen, Divergenz im Entprell-Fenster/bei Speicherfehler als
bekannte Grenze akzeptiert und dokumentiert? *Vorschlag:* (a) mit Server-Rueckfall. Nur der Parameter-Weg
garantiert „was ich sehe"; der Server-Read bleibt Rueckfall fuer direkt aufgerufene URLs.

**B2 — Summiert wird Standard, zeigt aber weder Matchcode noch Lagerplatz. (Frage an den Menschen)** Antwort 2
macht `/Summiert` zur Alltagsansicht des Kommissionierers. Die Ansicht hat heute **vier** Spalten
(`hauptfa/artnr/kommissionieren/sollmenge`; das RowViewModel hat keine weiteren Felder). Die Liste fuehrt
dagegen `matchcode` (**Locked**, „identifizierender Schluessel", `ColumnDefinitions.cs` ~Z.324/338) und
`hauptlagerplatz` — also **was** und **wo**. Wer kuenftig standardmaessig auf der Summe landet, verliert
beides. Der Backlog sagt ausdruecklich „der Matchcode ist **Anzeige**" (Falle 3) — der Entwurf setzt das nicht
um. Beide Felder liegen schon am `FaHierarchyNode` (`KommissionierListenService.cs` Z.58/60) und sind
Artikeleigenschaften, also je Summenschluessel konstant — als Anzeigefeld (`g.First().Matchcode`) ohne
Aenderung des Schluessels mitnehmbar.
**Frage:** Bekommt die Summiert-Ansicht `Matchcode` (Locked) und `Hauptlagerplatz` (ggf. Bezeichnung) als
Anzeigespalten? *Vorschlag:* ja, beide — sonst ist die neue Standardansicht fuer die Kommissionierung
schlechter als die alte. Folge: `#column-config`/`<th>`/`ColumnDefinitions`/`PrintSummiert` mit 6 statt 4 Keys.

**B3 — Einheit-Pruefschritt vor dem Dev-Lauf (S2).** Aus der Umsetzung heraus nicht pruefbar, und die
Frage in Antwort 3 zielt auf `KHKPpsRessourcenPositionen` (AKE-Quelle). **Frage:** Fuehrt der Mensch die zwei
Abfragen aus S2 vor der Freigabe auf der IDEAL-Sage-DB aus — oder wird es als **harte Stopp-Bedingung** in
den Rumpf geschrieben (Dev-Lauf baut, Fallstrick-Eintrag bleibt „unbelegt" markiert, Testbereit erst nach
Beleg)? *Vorschlag:* vor der Freigabe pruefen — zwei Abfragen, fuenf Minuten.

### SOLLTE

1. **Vier-Stellen-Konsistenz festschreiben (S5):** AK ergaenzen „Die Keys in `ColumnDefinitions.<View>`,
   `#column-config`, `<th data-col-key>` und `ShowCol(...)` der Druckansicht sind fuer beide Views
   identisch" + ein kleiner Unit-Test, der fuer `FaHierarchyKommissionierListen` und
   `FaHierarchyKommissionierSummiert` die `ColumnDefinitions`-Keys gegen die Print-Keys prueft (Drift-Guard).
2. **Bestehenden Test umkehren, nicht loeschen:** `KommissionierListenServiceTests.Summiert_NoKw_ReturnsEmpty_NoImplicitTotal`
   (Z.163) zementiert N2d und wird rot. In `affected_code` aufnehmen: umbenennen/umkehren zu „ohne KW alle
   HauptFA" (deckt AK 12 automatisiert ab).
3. **N2d als ueberholt kennzeichnen — wo?** Antwort 4 verlangt es; `affected_code` nennt die Stelle nicht.
   Ergaenzen: [[2026-07-29-standort-ideal-teil-3-spec]] (Hauptcheckout) — Vermerk an AK N2d mit Wikilink auf
   diese Spec; zusaetzlich die Doc-Kommentare in `AggregateSummiert` (Schritt 1: „Ohne KW … leeres Ergebnis")
   und am Controller (`Summiert`: „Ohne KW-Eingabe Hinweistext") anpassen, sonst luegt der Kommentar.
4. **Menue: zwei Links, nicht einer.** `_Layout.cshtml` fuehrt „Kommissionierlisten" an **zwei** Stellen auf
   `Index` (Z.184 im Dropdown, Z.203-204 als eigener Nav-Link). Beide in `affected_code` + AK.
5. **Druckknopf in der Summiert-Ansicht entscheiden, nicht offen lassen:** *Vorschlag:* seitenweiter
   Drucken-Link (wie Liste) **plus** PDF je HauptFA, dafuer Summiert wie `Index` nach HauptFA gruppiert
   rendern. **Achtung Paging:** Summiert paginiert heute auf **Aggregatzeilen** (`AggregateSummiert`
   Schritt 5) — eine HauptFA kann ueber zwei Seiten reissen. Bei gruppierter Darstellung entweder auf
   Gruppen-Paging umstellen (wie Liste) oder den Riss bewusst hinnehmen; festlegen.
6. **Fehlerrueckweg von `PdfSummiert`:** Das Vorbild `Pdf` leitet bei `PdfRenderException` auf `Index`.
   `PdfSummiert` muss auf `Summiert` (mit denselben Query-Parametern) zurueck. AK dazu.
7. **AK 4 praezisieren:** Der Umschalter traegt heute **nur** `target` (Index.cshtml Z.25/28,
   Summiert.cshtml Z.16/19); Spaltenfilter (`colf_*`, verschiedene Key-Mengen) und KW (nur Summiert) gehen
   beim Wechsel verloren. Antwort 4 argumentiert mit „Umschalten behaelt den Filter" — pruefbar ist nur:
   „`target` bleibt erhalten; Spaltenfilter und KW werden beim Wechsel bewusst zurueckgesetzt". So
   hinschreiben, sonst testet UAT etwas, das nie gebaut wird.
8. **Info-Kasten korrigieren (S4)** und „Status `InUmsetzung`" der HauptFA-Spec als Momentaufnahme
   kennzeichnen.

### HINWEIS

- Der zentrale Befund des Entwurfs (Summiert existiert, Schluessel erfuellt Falle 1+3, Filter-Durchschlag
  im PDF greift) ist am Code bestaetigt — die Spec ist deutlich kleiner, als der Backlog vermuten liess.
- Faellt B1 auf (a), wird die `WarehousePickingPrintLayout`-Ueberladung ggf. gar nicht gebraucht
  (`PrintBom`-Konvention: leere Liste = alle Spalten) — der Diff schrumpft weiter.
- Groesse: ~10-12 Dateien, nur Web-Schicht, keine Migration — ein Dev-Lauf, kein split/epic.
- Deploy-Abschnitt plausibel (web ja, Service nur Versions-Mirror, Migration nein). Kein Schreibzugriff auf
  Fremdsysteme, es entstehen keine echten Daten.
- Die Konsequenz „PDF je Anwender verschieden" ist benannt und fuer ein internes Arbeitsdokument richtig.

**NACHBESSERUNG NOETIG: Antworten nicht im Rumpf (S1), Druckspalten-Divergenz (B1), neue Standardansicht ohne Matchcode/Lagerplatz (B2), Einheit-Pruefschritt an der falschen Tabelle (B3).**

## ANTWORTEN auf die Kritische Pruefung (2026-09-28)

> Entscheidungen des Menschen (Gerald Weichbold), aufgenommen und — wo noetig — am Worktree-Stand
> vom 2026-09-28 gegengeprueft. [[2026-09-25-kommissionierung-nur-hauptfa-spec]] ist seither
> **Testbereit** (v1.46.0, TS-79) — `BuildFlagPredicate` delegiert an `KommissionierRelevanzFilter.IsRelevant`
> (Z.228-236), diese Spec setzt darauf auf, ohne die Methode selbst zu aendern.

**B1 ENTSCHIEDEN: Weg (a), PrintBom-Muster mit Server-Rueckfall.** Sichtbare Spalten werden beim Klick
aus der Seite gelesen und als `visibleColumns`-Parameter mitgegeben; der Rueckfall auf die gespeicherte
Praeferenz gilt NUR, wenn der Parameter fehlt. Das PDF zeigt damit strukturell, was der Bildschirm im
Moment des Klicks zeigt — S3 (Entprell-Fenster, stiller Speicherfehler) ist fuer den Normalfall
aufgeloest, siehe Abschnitt B im Rumpf. **Am Code geprueft, ob die `WarehousePickingPrintLayout`-
Ueberladung dadurch wegfaellt:** ja — `ShowCol(key)` prueft nur Mitgliedschaft, keine Reihenfolge; der
Rueckfall braucht deshalb nur `ParsePrefs` + einen einfachen `defs.Where(d => d.Locked || sichtbar)`-Filter
ueber die `ColumnDefinitions`-Keys, OHNE die geplante `ResolveColumns(prefs, defs)`-Ueberladung. Die
Ueberladung entfaellt ersatzlos aus `affected_code`; `WarehousePickingPrintLayout` bleibt fuer
`WarehousePickingController` unveraendert (AK 20). Das stille Speicher-Scheitern von
`column-preferences.js` (`console.warn`, Z.312) bleibt als eigener, nicht in dieser Spec behandelter
Punkt bestehen: [[2026-09-28-spaltenpraeferenz-speicherfehler-still]] (vom Aufrufer anzulegen, hier nur
verlinkt).

**B2 ENTSCHIEDEN: Matchcode und Hauptlagerplatz als Anzeigespalten der Summenansicht.** Beides
Artikeleigenschaften, innerhalb eines Summenschluessels konstant — kein Eingriff in den
Aggregationsschluessel `(HauptFA, Artnr, Kommissionieren)`. Umgesetzt in `ColumnDefinitions`
(6 Spalten), `#column-config`, `<th data-col-key>` (`Summiert.cshtml`) und `ShowCol` (`PrintSummiert.cshtml`),
siehe Abschnitt D im Rumpf. Reihenfolge (Identifikatoren → Ziel → Ort → Menge) ist eine eigene,
begruendete Entscheidung dieser Nachbesserung (keine Vorgabe des Menschen) — per `SupportsReorder: true`
vom Anwender ohnehin frei sortierbar.

**B3 ENTSCHIEDEN: blockiert NICHT.** Die bestehende Summiert-Ansicht summiert mit diesem Schluessel seit
v1.31.0 — ein Einheitenproblem waere vorbestehend, kein neues Risiko dieser Spec. **Antwort 3 wird
korrigiert:** Die tatsaechliche Quelle ist `vw_IDEAL-AKE_Kommissionierung_FAListe`, nicht
`KHKPpsRessourcenPositionen` (S2 der Kritischen Pruefung, am Code belegt: `FaHierarchyNode.cs`,
`FaHierarchySyncService.cs` vs. `SageImportService.cs`/`SQL/AgentJobs/02_Import_Artikel.sql`). Der
Pruefschritt (`sp_helptext` auf die View + `INFORMATION_SCHEMA.COLUMNS` auf die dort gelesene
Positionstabelle) wird als **Vorbedingung vor der Abnahme** im Deploy- und Test-Abschnitt dokumentiert
(TS-80.16), NICHT als Stopp-Bedingung fuer den Dev-Lauf. Ergibt sich eine abweichende Positionseinheit:
melden, nicht still mitloesen (Fallstrick-Eintrag bleibt bis dahin mit dieser Bedingung stehen).

**SOLLTE 5 ENTSCHIEDEN: Gruppen-Paging wie die Liste.** Eine HauptFA reisst nicht ueber zwei Seiten
(am Bildschirm stoerend, im Druck ein Fehler). **Am Code geprueft, wie die Liste Gruppen-Paging macht:**
`FaHierarchyListBuilder.Build`, Schritt 4 (`FaHierarchyListBuilder.cs` Z.146-149): erst `GroupBy(HauptFA)`,
`TotalGroupCount` zaehlt Gruppen, `Skip/Take` auf den GRUPPEN, nicht auf den Positionen. Dasselbe Muster
wird auf `AggregateSummiert` uebertragen (Abschnitt C im Rumpf) — Aggregatzeilen werden zuerst gefiltert,
dann nach `HauptFA` gruppiert, dann wird auf Gruppen-Ebene paginiert. Summiert wird nach HauptFA gruppiert
gerendert (wie `Index.cshtml`); seitenweiter Drucken-Link + PDF je HauptFA (wie die Liste). Bestehende
Tests bleiben gruen, da sie mit genau einer HauptFA arbeiten (keine beobachtbare Verhaltensaenderung dort).

**SOLLTE 7 ENTSCHIEDEN, ABWEICHEND vom Pruefvorschlag:** Der Umschalter Liste↔Summiert nimmt `target`, KW
UND die Spaltenfilter auf den Spalten mit, die in BEIDEN Ansichten existieren. Filter auf Spalten, die nur
eine Ansicht hat, entfallen. **NICHT** „bewusst zuruecksetzen" — die Begruendung zu Antwort 4 stuetzt sich
auf gleichbleibendes Filterverhalten. **Am Code geklaert:** Hat die Liste (`Index`) ueberhaupt einen
KW-Filter? **Nein** — `Index(string? target, int page, int? pageSize)` kennt kein `kwVon`/`kwBis`. Das ist
ein **KONFLIKT** (KW kann beim Wechsel Summiert→Liste nicht sinnvoll „mitgenommen" werden, bzw. die Liste
braeuchte dann selbst einen KW-Filter) — **NICHT selbst entschieden**, sondern als neue offene Rueckfrage 5
gefuehrt (siehe „Offene Rueckfragen" oben) und hier im Bericht gemeldet. Diese Spec entwirft vorerst Weg
(b) aus der Rueckfrage (KW als toter Pass-Through-Parameter Richtung Liste) als kleinsten, nicht
verhaltensaendernden Diff — die endgueltige Entscheidung liegt beim Menschen.
**Gemeinsame Spalten nach B2:** `hauptfa`, `artnr`, `matchcode`, `kommissionieren`, `hauptlagerplatz`.
**`sollmenge` — am Code geprueft:** derselbe Schluessel (`ColumnMap["sollmenge"]` vs.
`SummiertColumnMap["sollmenge"]`), aber semantisch verschieden (Einzelposition vs. Summe). **Entscheidung:
NICHT mitnehmen** — im Zweifel nicht uebertragen, Begruendung siehe Abschnitt E im Rumpf (stiller
Bedeutungswechsel waere sonst die Folge).

**UEBRIGE SOLLTE uebernommen:**
- SOLLTE 2: Test `Summiert_NoKw_ReturnsEmpty_NoImplicitTotal` umgedreht (nicht geloescht) — legitime
  Verhaltensaenderung, keine Symptom-Verdeckung.
- SOLLTE 3: Vermerk an `AK N2d` in [[2026-07-29-standort-ideal-teil-3-spec]] (Hauptcheckout) +
  Doc-Kommentare in `AggregateSummiert`/Controller angepasst (in `affected_code` aufgenommen).
- SOLLTE 4: beide Menue-Links in `_Layout.cshtml` (Z.184 + Z.203-204) auf `Summiert`.
- SOLLTE 6: `PdfSummiert`-Fehlerrueckweg auf `Summiert` (mit denselben Parametern), nicht `Index`.
- SOLLTE 8: Info-Kasten korrigiert, „InUmsetzung" als ueberholte Momentaufnahme gekennzeichnet — die
  HauptFA-Spec ist inzwischen Testbereit.
- SOLLTE 1: Drift-Guard-Test (Vier-Stellen-Konsistenz) + AK fuer beide Views ergaenzt.

**Fallstricke „Freitext nie per `@Html.Raw(...)` in ein JS-Literal" beachtet:** als Pflicht in den
Loesungsentwurf aufgenommen (Abschnitt F) + AK 18. Diese Nachbesserung baut selbst keine neue Einbettung
(der Click-Handler liest nur DOM-Attribute), die Regel gilt aber verbindlich fuer jede kuenftige
Erweiterung, die Filterwerte in einen Script-Block legt.

**Kein weiterer Widerspruch gefunden** zwischen den Entscheidungen B1/B2/B3/SOLLTE-5/SOLLTE-7
untereinander oder mit dem Code — mit der einen genannten Ausnahme (KONFLIKT Rueckfrage 5, fehlender
KW-Filter an der Liste), die als offene Rueckfrage gefuehrt wird statt still geloest.
