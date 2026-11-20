---
type: spec
title: "Bugfix: Dashboard-Kacheln wieder mit Symbolen (SymbolPfad im NavigationKatalog)"
slug: 2026-11-20-bugfix-dashboard-kacheln-symbole-fehlen-spec
status: Entwurf
created: 2026-11-20
updated: 2026-11-20
source_backlog: "[[2026-11-20-bugfix-dashboard-kacheln-symbole-fehlen]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.49 (Worktree 1.99.45 laut IdealAkeWms/AppVersion.cs:5; vorgeschlagen sind 1.99.46 Teil 6, 1.99.47 Sage-AG-Bugfix, 1.99.48 BDE-Liste Isolierungsfräse; Dev-Lauf prüft erneut)"
testszenario_vorschlag: "TS-174 (docs/TESTSZENARIEN.md im Worktree endet bei TS-170 Z. 13379; TS-171 BDE-Liste, TS-172 Sage-AG-Bugfix, TS-173 Teil 6 in Entwürfen reserviert; Dev-Lauf prüft erneut)"
bug: "[[2026-11-20-dashboard-kacheln-symbole-fehlen-bug]]"
affected_code:
  - "IdealAkeWms/Services/Navigation/NavigationKatalog.cs:8 (NavZiel um SymbolPfad erweitern), :22-99 (Kachel-Ziele erhalten ihr Symbol)"
  - "IdealAkeWms/Views/Home/Index.cshtml:21-29 (Symbol über dem Titel rendern)"
  - "IdealAkeWms.Tests/Services/NavigationSichtTests.cs:76-82 (T1 Katalog-Wächter), :240-245 (T4 Startseiten-Wächter)"
  - "docs/anwender/18-startseite-und-menue.md:48 (Satz „keine Symbole mehr“) + Fotos 18-0x"
  - "docs/TESTSZENARIEN.md (neu TS-174), Views/Help/Changelog.cshtml, beide AppVersion.cs"
  - "Bug-Record [[2026-11-20-dashboard-kacheln-symbole-fehlen-bug]]"
open_questions:
  - "1. Symbolzuordnung laut Tabelle „Symbol je Kachel“ (46 Kacheln, 9 alte Symbole wörtlich übernommen, Rest aus Projekt-Symbolen) so übernehmen? Empfehlung: ja; Änderungswünsche je Zeile hier nennen."
  - "2. Für Einbuchung/Ausbuchung (früher Text „+“/„-“) sowie Umbuchung/Lagerplatz umbuchen gibt es im Projekt kein passendes Inline-Symbol ohne fill-rule. Drei selbstgezeichnete Grundformen (Plus, Minus, Doppelpfeil, je ein Pfad aus Rechtecken/Dreiecken) zulassen? Empfehlung: ja. Alternative: Projekt-Symbole (Plus → collection, Minus → trash, Doppelpfeil → box)."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Seit v1.99.45 zeigt die Startseite nur Titel und Kurzbeschreibung. Das Symbol fehlt, an dem man eine Kachel auf einen Blick erkennt (Terminal in
der Fertigung, Handy im Lager). Die freigegebene Spec [[2026-11-19-dashboard-kacheln-nach-kategorie-und-berechtigung-spec]] hatte es
vorgesehen (Z. 230 `SymbolPfad`, Z. 257 „Symbole bleiben Inline-SVG wie heute … mit `aria-hidden="true"`“). Die Umsetzung hat es weggelassen,
und QA hat das als „bewusste Vereinfachung“ durchgelassen (Z. 444, Z. 469). Bug-Record: [[2026-11-20-dashboard-kacheln-symbole-fehlen-bug]].

### Ist-Stand (belegt, Worktree `2026-08-07-ideal-teile-1-5`, v1.99.45)

- **Definition:** `NavZiel(string Key, string Titel, string Controller, string Action, string? Kategorie = null, string? Beschreibung = null)`
  (`IdealAkeWms/Services/Navigation/NavigationKatalog.cs:8`). Ein Feld `SymbolPfad` gibt es nicht. Die Suche `SymbolPfad` über alle `*.cs`/`*.cshtml`
  des Worktrees ergibt keinen Treffer. Es wird also weder definiert noch gesetzt noch gerendert.
- **Rendern:** `Views/Home/Index.cshtml:21-29`: Link → `card dashboard-card` → `<h4 class="h5 card-title">@z.Titel</h4>` +
  `<small class="text-muted">@z.Beschreibung</small>`. Es gibt kein `<svg>`.
- **Menü:** `Views/Shared/_Layout.cshtml` enthält kein `<svg>` (die `<svg`-Zählung über `Views/` listet `_Layout.cshtml` nicht). Das Menü
  hatte nie Symbole. Daran ändert dieser Bugfix nichts.
- **Wächter:** `NavigationSichtTests.cs:81` verlangt für Kacheln nur `Kategorie ∈ Kategorien` und `Beschreibung != null`.
  `:241-245` (T4) prüft nur „kein ViewBag, kein `asp-controller=`“.
- **Doku:** `docs/anwender/18-startseite-und-menue.md:48`: „Die Kacheln haben **keine Symbole** mehr, nur Name und Kurzbeschreibung.“
- **Farbe:** `.dashboard-card .card-text { color: var(--ake-primary); font-size: 2rem; }` (`wwwroot/css/site.css:377-381`). Die Klasse wird heute
  nicht mehr verwendet (Index.cshtml hat kein `card-text`), sie steht aber noch in der CSS-Datei. Mobil: `:897-899`.

### Alte Symbole (Stand vor v1.99.45)

Quelle: `IdealAkeWms/Views/Home/Index.cshtml` und `_ArtikelinfoTile.cshtml` im **Hauptcheckout** (`main`, v1.30.4). Diese Datei ist auf `main`
seit der Abzweigung des Bündels unverändert. Beleg: Die Zeilenbereiche im Ist-Stand der Vorgänger-Spec (Z. 59-69, Stand `f1747e10` = v1.99.44:
Lager 18-66, Fertigung 68-104, …, Stammdaten 258-280, SVG Kommissionierung 76-79) stimmen Zeile für Zeile mit dieser Datei überein.
Der Spec-Lauf hatte keine Shell. **Der Dev-Lauf bestätigt** den Stand mit
`git show f1747e10:IdealAkeWms/Views/Home/Index.cshtml` und `git show f1747e10:IdealAkeWms/Views/Home/_ArtikelinfoTile.cshtml`.

| Kachel alt (Zeile) | Symbol alt | Pfad(e) alt | Neuer Katalog-Key |
|---|---|---|---|
| Einbuchung (:26-27) | Text „+“, `--ake-green` | – (kein SVG) | Einbuchung |
| Ausbuchung (:37-38) | Text „-“, `--ake-orange` | – | Ausbuchung |
| Bestände (:48-49) | Emoji `&#128230;` (Paket) | – | Bestaende |
| Bewegungen (:59-60) | Emoji `&#128203;` (Klemmbrett) | – | Bewegungshistorie |
| Kommissionierung (:74-79) | SVG card-checklist | `:77`, `:78` | Kommissionierung |
| Fertigungsaufträge (:90-96) | SVG journal-text | `:93`, `:94`, `:95` | FaListe |
| Artikelinfo (`_ArtikelinfoTile.cshtml:5-10`) | SVG upc-scan | `_ArtikelinfoTile.cshtml:8`, `:9` | Artikelinfo |
| Leitstand Kommissionierung (:123-127) | SVG grid-fill | `:126` | LeitstandKommissionierung |
| Teileverfolgung OSEON (:145-149) | SVG geo-alt-fill | `:148` | TeileverfolgungOseon |
| BDE Terminal (:167-171) | SVG terminal-fill | `:170` | BdeTerminal |
| BDE Cockpit (:184-188) | SVG cart-fill | `:187` | BdeCockpit |
| BDE Buchungen (:199-203) | SVG file-earmark | `:202` | BdeBuchungen |
| OSEON AG-Übersicht (:222-226) | SVG bar-chart | `:225` | OseonAgUebersicht |
| Artikel, Lagerplätze, Benutzer, Arbeitsplätze (:236-280) | **kein Symbol** | – | Artikel, Lagerplaetze, Benutzer, Arbeitsplaetze |

Die Bootstrap-Namen in dieser und der nächsten Tabelle sind nur Lesehilfe (aus der Pfadform erkannt). **Maßgeblich ist der Pfad-String an der
genannten Fundstelle**, den der Dev-Lauf wörtlich übernimmt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- `NavZiel` bekommt `SymbolPfad`. Jede Kachel (`Kategorie != null`) erhält einen Pfad: die 9 alten SVG-Pfade wörtlich, die übrigen aus
  Inline-SVGs, die schon im Projekt stehen, sowie 3 selbstgezeichnete Grundformen (Rückfrage 2).
- Die Kachel rendert das Symbol als dekoratives Inline-SVG.
- Wächter-Tests, TS-174, Anwenderdoku Kapitel 18, Anwender-Changelog, Version.

**Out-of-Scope**
- Kachelmenge, Kategorien, Reihenfolge, Titel, Beschreibungen, Sichtbarkeitsregeln (`NavigationSicht`), Berechtigungen, Filter.
- Symbole im Menü (`_Layout.cshtml` hatte nie welche, Bestand bleibt).
- Akzentfarben je Kachel (alt: `border-left-color` und Symbolfarbe grün/orange/hellblau). Sie lagen unter 3:1 (Vorgänger-Spec Z. 258-261).
  Die Bedeutung trägt der Titel, das Symbol bleibt einfarbig.
- Symbole für Ziele, die nur im Menü stehen (`Kategorie == null`). YAGNI, sie haben keine Kachel.
- Icon-Bibliothek, Sprite-Datei, eigenes Partial je Symbol.

## Fachliche Anforderungen

F1 **Jede Kachel hat ein Symbol**, über dem Titel, rein dekorativ (`aria-hidden="true"`, `focusable="false"`). Der Screenreader liest weiter
nur Titel und Beschreibung.

F2 **Eine Quelle (ADR [[0034-navigation-menue-und-kacheln-aus-einer-quelle]]).** Das Symbol steht im `NavZiel` des Katalogs, nirgends sonst.
`Home/Index.cshtml` liest es nur aus `z.SymbolPfad`. Es gibt keinen zweiten Symbolkatalog, keine `switch`/`if`-Kette über Keys in der View
und keine Symbol-Konstanten außerhalb von `NavigationKatalog.cs`.

F3 **Alte Symbole zuerst.** Hatte eine Kachel vor v1.99.45 ein SVG, kommt genau dieser Pfad zurück. Text- und Emoji-Symbole
(„+“, „-“, Paket, Klemmbrett) werden durch SVG ersetzt (wie schon in der Vorgänger-Spec Z. 261 vorgesehen).

F4 **Kontrast/Bedeutung:** Das Symbol hat die Farbe `--ake-primary` #053153 (über die bestehende Regel `.dashboard-card .card-text`,
gemessen ≥ 12:1 auf Weiß laut QA der Vorgänger-Spec Z. 469). Die Bedeutung trägt nie das Symbol oder seine Farbe, sondern der Titeltext.

F5 **Bestandsverhalten unverändert:** Welche Kachel in welcher Kategorie, in welcher Reihenfolge, mit welchem Titel und welcher Beschreibung
erscheint, bleibt exakt wie in v1.99.45.

### Symbol je Kachel (Vorschlag, Rückfrage 1)

Fundstellen im Worktree `2026-08-07-ideal-teile-1-5`, `IdealAkeWms/Views/…`. „alt“ = Hauptcheckout `Views/Home/Index.cshtml` bzw.
`_ArtikelinfoTile.cshtml` (siehe oben). Alle gewählten Fundstellen haben **kein** `fill-rule`-Attribut. Die `fill-rule`-Suche über `Views/` trifft
nur Chevrons/Pfeile (z. B. `Settings/Index.cshtml:117`, `Articles/Info.cshtml:18`), und die sind deshalb nicht gewählt (Begründung im Lösungsentwurf).

| Kategorie | Key | Symbol (Lesehilfe) | Quelle Pfad |
|---|---|---|---|
| Lager | Einbuchung | Plus (selbst, ersetzt „+“) | `M7 2h2v5h5v2H9v5H7V9H2V7h5z` (Rückfrage 2) |
| Lager | MehrfachEinbuchung | collection | `Shared/_FaDocumentLinks.cshtml:15` |
| Lager | Ausbuchung | Minus (selbst, ersetzt „-“) | `M2 7h12v2H2z` (Rückfrage 2) |
| Lager | Umbuchung | Doppelpfeil (selbst) | `M1 3h10V1l4 3-4 3V5H1zm14 8H5V9l-4 3 4 3v-2h10z` (Rückfrage 2) |
| Lager | Bestaende | box (ersetzt Paket-Emoji) | `FaHierarchy/Index.cshtml:17` |
| Lager | Bewegungshistorie | file-earmark-text (ersetzt Klemmbrett-Emoji) | `Shared/_VaultLink.cshtml:27`, `:28` |
| Lager | Artikelinfo | upc-scan (**alt**) | alt `_ArtikelinfoTile.cshtml:8`, `:9` (identisch `Articles/Info.cshtml:45-46`) |
| Lager | LagerplatzAusbuchen | trash | `Settings/Index.cshtml:169`, `:170` |
| Lager | LagerplatzUmbuchen | Doppelpfeil (selbst) | wie Umbuchung |
| Bestellungen | Bedarfsmeldungen | envelope-check | `PartRequisitions/Index.cshtml:138`, `:139` |
| Bestellungen | Lagerbestellungen | cart2 | `Picking/Bom.cshtml:144` (dort Knopf „als Lagerbestellung“) |
| Bestellungen | MeineFehlteile | exclamation-triangle | `SageBookingQueue/Index.cshtml:20` |
| Bestellungen | LagerEingehendeListen | collection | `Shared/_FaDocumentLinks.cshtml:15` |
| Bestellungen | LagerFehlteile | exclamation-triangle | `SageBookingQueue/Index.cshtml:20` |
| Kommissionierung | Kommissionierung | card-checklist (**alt**) | alt `:77`, `:78` |
| Kommissionierung | Kommissionierlisten | file-earmark-pdf | `FaHierarchyKommissionierListen/Summiert.cshtml:112` (zwei `<path>`) |
| Kommissionierung | LeitstandKommissionierung | grid-fill (**alt**) | alt `:126` |
| Produktion/BDE | FaListe | journal-text (**alt**) | alt `:93`, `:94`, `:95` |
| Produktion/BDE | FaStruktur | folder | `FaHierarchy/Index.cshtml:153` (Strukturkopf der FA-Struktur) |
| Produktion/BDE | FaVervollstaendigen | pencil | `FaCompletion/_FaCompletionRow.cshtml:56` |
| Produktion/BDE | FaAbarbeitungsliste | check-square | `ProductionOrders/_ProductionOrderRow.cshtml:141` (zwei `<path>`) |
| Produktion/BDE | Vormontage | box | `FaHierarchy/Index.cshtml:17` (Legende „Baugruppe (Sub-FA)“) |
| Produktion/BDE | BdeTerminal | terminal-fill (**alt**) | alt `:170` |
| Produktion/BDE | BdeCockpit | cart-fill (**alt**) | alt `:187` |
| Produktion/BDE | BdeBuchungen | file-earmark (**alt**) | alt `:202` |
| Produktion/BDE | TeileverfolgungOseon | geo-alt-fill (**alt**) | alt `:148` |
| Produktion/BDE | OseonAgUebersicht | bar-chart (**alt**) | alt `:225` |
| Blechfertigung | BfWarenkorb | cart | `FaHierarchy/Index.cshtml:21` |
| Blechfertigung | BfAuftraege | journal-text | wie FaListe |
| Beschichtung | BeschichtungVorschlagsliste | file-earmark-pdf | wie Kommissionierlisten |
| Beschichtung | Beschichtungsauftraege | journal-text | wie FaListe |
| Einkauf/Wareneingang | OffeneBestellungenLager | cart2 | wie Lagerbestellungen |
| Einkauf/Wareneingang | WareneingangZuBestellung | check-square | wie FaAbarbeitungsliste |
| Einkauf/Wareneingang | Bestellvorschlaege | cart | wie BfWarenkorb |
| Einkauf/Wareneingang | OffeneBestellungenEinkauf | cart2 | wie Lagerbestellungen |
| Stammdaten | Artikel | search | `Articles/Info.cshtml:54` |
| Stammdaten | Lagerplaetze | geo-alt-fill | wie TeileverfolgungOseon |
| Stammdaten | Werkbaenke | grid-fill | wie LeitstandKommissionierung |
| Administration | Benutzer | lock-fill | `ArticleAttributes/Index.cshtml:93` |
| Administration | Arbeitsplaetze | printer | `Picking/Bom.cshtml:39`, `:40` |
| Administration | Rollen | check-square | wie FaAbarbeitungsliste |
| Administration | Arbeitsgaenge | stopwatch | `Tracking/_OseonGroupDetails.cshtml:77` (an Arbeitsgang-Zeilen) |
| Administration | ArbeitsgaengeSage | link-45deg | `Shared/_VaultLink.cshtml:17`, `:18` |
| Administration | Einstellungen | gear | `Settings/Index.cshtml:14`, `:15` |
| Administration | Standorteinstellungen | geo-alt-fill | wie TeileverfolgungOseon |
| Administration | AktivitaetsProtokoll | file-earmark-text | wie Bewegungshistorie |

46 Kacheln: Lager 9, Bestellungen 5, Kommissionierung 3, Produktion/BDE 10, Blechfertigung 2, Beschichtung 2, Einkauf/Wareneingang 4,
Stammdaten 3, Administration 8. Das entspricht genau den `NavZiel` mit Kategorie in `NavigationKatalog.cs:22-99`. Innerhalb einer Kategorie
wiederholt sich ein Symbol nur bei gleicher Tätigkeit (Umbuchung/Lagerplatz umbuchen, Fehlteile/Lager-Fehlteile, Offene Bestellungen Einkauf). Ein
Projekt-Symbol für Personen gibt es nicht. Die Suche nach den Bootstrap-Pfadanfängen von people/person, plus, dash und arrow-left-right in
`*.cshtml`/`*.js` findet nichts, deshalb sind Benutzer = lock-fill und Rückfrage 2.

## Technischer Loesungsentwurf

Ponytail-Leiter: Der Katalog existiert, und die Vorgänger-Spec hatte das Feld schon vorgesehen. Die Pfade stehen im Projekt. Die CSS-Regel für
die Symbolfarbe (`.dashboard-card .card-text`) steht ungenutzt in `site.css:377-381`. Es braucht also: ein Record-Feld, ein Argument je Kachel, vier Zeilen
Markup und zwei Testzeilen. Kein CSS, kein Partial, kein Helper.

1. **`NavigationKatalog.cs:8`**: `NavZiel(..., string? Kategorie = null, string? Beschreibung = null, string? SymbolPfad = null)`. Doc-Kommentar:
   „SymbolPfad = `d`-Attribut eines 16×16-Inline-SVG (viewBox 0 0 16 16), Pflicht bei Kachel (Wächter-Test)“.
2. **Pfade als Konstanten** in `NavZiele` (`private const string SymXxx = "…";`, je Symbol einmal, rund 25 Konstanten, im selben File oberhalb der
   Felder). Mehrteilige alte Symbole (mehrere `<path>`) werden zu **einem** `d`-String verkettet (Leerzeichen dazwischen). Das ist zulässig,
   weil alle gewählten Pfade die Standard-Füllregel `nonzero` nutzen. Bootstrap zeichnet Löcher mit Gegenrichtung, die Teilpfade überlappen nicht
   gleichsinnig. Die Sichtprüfung in TS-174.1 bestätigt es je Kachel. **Fällt ein Symbol nach dem Verketten sichtbar falsch aus**, nimmt der
   Dev-Lauf ein anderes Projekt-Symbol aus der Tabelle, ändert den Typ des Feldes nicht und vermerkt die Abweichung in der Spec.
   Pfade mit `fill-rule="evenodd"` (Chevrons, Pfeile, `Settings/Index.cshtml:117`) sind ausgeschlossen. Ein einzelner `d`-String kann die
   Regel nicht mittragen, und `evenodd` für alle würde andere Symbole verändern.
3. **Je Kachel-Feld** (`NavigationKatalog.cs:22-99`, nur die 46 mit Kategorie) das 7. Argument `SymXxx` anhängen. Menü-only-Ziele bleiben
   unverändert (`null`). Key, Titel, Controller, Action, Kategorie, Beschreibung, `Alle`, `Kategorien` und der `switch` in `NavigationSicht`
   bleiben **unverändert** (F5).
4. **`Home/Index.cshtml:24-27`** im `card-body` vor dem `<h4>`:
   ```cshtml
   <div class="card-text mb-1">
       <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true" focusable="false"><path d="@z.SymbolPfad" /></svg>
   </div>
   ```
   Razor kodiert den Attributwert. Pfad-Strings enthalten nur `[0-9A-Za-z .,-]`, deshalb ist kein `@Html.Raw` nötig und keins erlaubt. Die Farbe kommt aus
   `.dashboard-card .card-text` (`site.css:380`). Die Größe ist fest 32 px wie früher, die mobile `font-size`-Regel `:897-899` wirkt nicht auf feste
   SVG-Maße. Das ist gewollt, die Trefferfläche bleibt die ganze Karte.
5. **Frontend:** Der Dev-Lauf ruft den Skill `frontend-design` ausdrücklich auf (CLAUDE.md). Es gelten die Leitplanken Konsistenz und Kontrast
   (F4), es gibt keine neue Klasse.
6. **Tests** (`IdealAkeWms.Tests/Services/NavigationSichtTests.cs`, vorhandene Datei, kein neuer Testansatz):
   - **T1 erweitern**, neuer `[Fact] JedeKachel_HatEinSymbol` neben `Katalog_KeyGleichFeldname_UndEindeutig` (:76-82):
     `NavZiele.Alle.Where(z => z.Kategorie != null)` → jedes `SymbolPfad` nicht leer **und** passt auf `^M[0-9A-Za-z .,-]+$`
     (beginnt mit moveto, nur Pfadzeichen, also kein Markup, kein `<path`, kein Anführungszeichen). Die Fehlermeldung nennt die Keys ohne Symbol.
   - **T4 erweitern** (`Startseite_OhneViewBagUndOhneHartcodierteLinks`, :241-245): `Home/Index.cshtml` enthält `z.SymbolPfad`,
     `aria-hidden="true"` und **kein** `Html.Raw`.
   - **Unverändert grün:** alle übrigen Fälle in `NavigationSichtTests` (T1-Matrix, T2 Filter-Kongruenz `SichtbaresZiel_LaesstJedenFilterDurch`,
     T3 `Layout_JederMenuelinkHaengtAnSeinemNavZiel_OhneEigeneRechteabfrage`) sowie `BeschichtungsauftraegeViewTests`. Damit ist F5
     automatisch belegt: Die Tests prüfen Sichtbarkeit und Menü-Kongruenz, und sie bleiben ohne Anpassung grün.

Betroffene Module: [[controller]] (Start/Hilfe), [[module]]. Brain-Nachzug im Dev-Lauf: [[fallstricke]] §58 additiv um einen Satz
„Kachel-`NavZiel` braucht `SymbolPfad` (nur `nonzero`-Pfade, Wächter `JedeKachel_HatEinSymbol`)“. ADR 0034 wird nicht umgeschrieben, die
Entscheidung bleibt dieselbe, nur das Record bekommt ein Feld.

## Migrations-/SQL-Auswirkungen

Keine. Keine Migration, kein `SQL/XX`, kein `00_FreshInstall`, keine AgentJobs (reine Code-/View-Änderung, keine Entität).

## Einstellungen

keine. Es gibt keine neue und keine geänderte Einstellung.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Reiner Lesepfad, es wird nichts geschrieben.

Rollen und Zugriffsfilter: unverändert. Es gibt keine neue Rolle, keinen Filter und keine Regeländerung in `NavigationSicht`. Listen-View-Pattern:
entfällt (keine Tabelle).

## Akzeptanzkriterien

1. `NavZiel` hat das Feld `SymbolPfad`. Jedes `NavZiel` mit `Kategorie != null` hat einen nicht leeren `SymbolPfad`, der auf
   `^M[0-9A-Za-z .,-]+$` passt (Test `JedeKachel_HatEinSymbol`).
2. Jede Kachel auf der Startseite zeigt über dem Titel ein 32×32-SVG-Symbol in `--ake-primary`.
3. Das Symbol-SVG trägt `aria-hidden="true"` und `focusable="false"`. Der Screenreader liest je Kachel nur Titel und Beschreibung (T4 + TS-174.4).
4. Die 9 Kacheln mit altem SVG (Kommissionierung, FA-Liste, Artikelinfo, Leitstand Kommissionierung, OSEON Aufträge, Terminal, Cockpit, Buchungen,
   OSEON AG-Übersicht) zeigen genau das Symbol von vor v1.99.45. Sichtvergleich mit dem alten Stand bzw. Pfadvergleich gegen
   `git show f1747e10:IdealAkeWms/Views/Home/Index.cshtml`.
5. Die Symbole der übrigen Kacheln entsprechen der freigegebenen Tabelle „Symbol je Kachel“ (Rückfragen 1 und 2).
6. Die Symbole stehen nur in `NavigationKatalog.cs`. `Home/Index.cshtml` enthält keinen Pfad-String und keine Key-Abfrage.
   `_Layout.cshtml` bleibt ohne `<svg>`.
7. **Bestandsverhalten unverändert:** Für dieselbe Rolle und Schalterlage erscheinen dieselben Kacheln in denselben Kategorien, in derselben
   Reihenfolge, mit denselben Titeln und Beschreibungen wie in v1.99.45. Das Menü ist unverändert. Alle bestehenden `NavigationSichtTests` sind ohne
   Anpassung grün. Im Diff von `NavigationKatalog.cs` ändern sich nur das Record, die Konstanten und das angehängte 7. Argument.
8. Kein `@Html.Raw` in `Home/Index.cshtml` (T4).
9. Bei 390 px eine Spalte, kein Querscrollen, Symbol nicht abgeschnitten. Fokusring der Kachel unverändert sichtbar.
10. `docs/anwender/18-startseite-und-menue.md` sagt nicht mehr „keine Symbole“. Die Fotos zeigen die Kacheln mit Symbol.
11. Build und alle Tests grün (Web + Service).

## Test-Szenarien

Neues Kapitel **TS-174 „Dashboard-Kacheln mit Symbolen (v1.99.49)“** in `docs/TESTSZENARIEN.md`, Querverweis auf TS-170. Danach eine Zeile in
[[testszenarien-index]]. Standort: DEV **AKE** und DEV **IDEAL**.

**Vorbedingungen:** Stand v1.99.49 auf DEV. Benutzer admin. Dazu ein Benutzer nur mit picking und einer nur mit stock_read (wie TS-170).
Prüfabfrage A aus TS-170 je DB ausgeführt (Schalterlage notiert).

| Nr | Schritt | Erwartet |
|---|---|---|
| 174.1 | admin, Startseite AKE und IDEAL, 1440 px | Jede sichtbare Kachel hat genau ein Symbol über dem Titel, dunkelblau, kein leerer Platz, kein Kästchen, kein Rest-Text („+“, Emoji). Jedes Symbol ist sauber gezeichnet, ohne gefüllte Löcher oder fehlende Teile (Prüfung der verketteten Pfade). |
| 174.2 | Alte Symbole vergleichen (Screenshot vor v1.99.45, z. B. Doku-Foto der Vorversion, oder Pfadvergleich `git show f1747e10:…`) | Kommissionierung, FA-Liste, Artikelinfo, Leitstand Kommissionierung, OSEON Aufträge (nur AKE), Terminal, Cockpit, Buchungen, OSEON AG-Übersicht (nur AKE) zeigen das alte Symbol. |
| 174.3 | Bestandsvergleich: je Benutzer (admin, picking, stock_read) Kachelliste mit TS-170-Protokoll vergleichen | Gleiche Kategorien, Kacheln, Reihenfolge, Titel und Beschreibungen. Nur das Symbol ist neu. Menü identisch. |
| 174.4 | Screenreader (NVDA oder Edge „Vorlesen“) über drei Kacheln, Tab-Taste | Vorgelesen werden Linkname = Titel + Beschreibung, **kein** „Grafik“/„Bild“. Fokusring sichtbar. |
| 174.5 | Handy 390 px (DevTools) | Eine Spalte, Symbol vollständig sichtbar, kein Querscrollen. |
| 174.6 | Negativ, Modul aus: einen Schalter aus (z. B. `BlechfertigungAktiv` auf IDEAL-DEV), Startseite neu laden, danach zurücksetzen | Kacheln und Kategorie verschwinden wie in TS-170.3. Es bleibt kein verwaistes Symbol und keine leere Karte stehen. |
| 174.7 | Negativ, Benutzer ohne Rolle | Nur der Hinweis „keine Funktionen freigeschaltet“, kein Symbol und keine leere Karte. |
| 174.8 | Negativ, Kontrast: Symbolfarbe messen (DevTools Farbwähler) | #053153 auf Weiß, ≥ 3:1 (Grafik, WCAG 1.4.11). Ohne Farbe bleibt die Kachel am Titel erkennbar (Graustufen-Ansicht). |

**Automatisch:** `JedeKachel_HatEinSymbol` (fällt, sobald jemand eine Kachel ohne Symbol anlegt) und die erweiterte T4.

## Etappen (nur bei epic: true)

Kein Epic.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vorläufig, der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Katalog, Startseite).
- **Service:** ja, **nur Versionsnummer** (`IDEALAKEWMSService/AppVersion.cs` → 1.99.49, damit Web und Dienst denselben Stand melden; keine Dienst-Logik).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine. Kein SQL, kein Einmal-Skript, keine Einstellung, kein Leitstand-Schritt.
  Die Rückkehr der Symbole braucht keine Anwender-Info im Update-Lauf, der Anwender-Changelog genügt.
- **Publish-Befehle:**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Doku-Pflichten:** `Views/Help/Changelog.cshtml` (v1.99.49: „Startseite: Kacheln wieder mit Symbolen“); `docs/anwender/18-startseite-und-menue.md:48`
  umschreiben und Fotos 18-0x per Playwright gegen DEV neu aufnehmen (nur ansehen); Stand-Version in `docs/anwender/README.md`. Brain:
  Changelog, [[feature-map]] (Startseite), [[fallstricke]] §58 (ein Satz), Bug-Record auf `behoben`, [[testszenarien-index]].

## Offene Rueckfragen

1. Symbolzuordnung laut Tabelle „Symbol je Kachel“ (46 Kacheln, 9 alte Symbole wörtlich übernommen, Rest aus Projekt-Symbolen) so übernehmen?
   **Empfehlung: ja.** Änderungswünsche bitte je Zeile hier nennen (Key → anderes Symbol aus der Tabelle).
2. Für Einbuchung und Ausbuchung (früher Text „+“/„-“) sowie Umbuchung und Lagerplatz umbuchen gibt es im Projekt kein passendes Inline-Symbol
   ohne `fill-rule`. Die Suche nach plus, dash, arrow-left-right und people in `*.cshtml`/`*.js` blieb ohne Treffer, vorhandene Pfeile/Chevrons
   nutzen `evenodd`. Drei selbstgezeichnete Grundformen zulassen (Plus, Minus, Doppelpfeil, je ein Pfad aus Rechtecken/Dreiecken, siehe Tabelle)?
   **Empfehlung: ja.** Keine Bibliothek, je eine Zeile, und so kommen die alten Zeichen „+“/„-“ sinngleich zurück. Alternative ohne Eigenzeichnung:
   Einbuchung → collection, Ausbuchung → trash, Umbuchung/Lagerplatz umbuchen → box.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Symbolzuordnung laut Tabelle „Symbol je Kachel“ übernehmen →
2. Selbstgezeichnete Grundformen Plus/Minus/Doppelpfeil zulassen →
