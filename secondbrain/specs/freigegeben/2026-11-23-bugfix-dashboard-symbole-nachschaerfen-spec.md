---
type: spec
title: "Bugfix: Dashboard-Symbole nachschärfen – jede Kachel eindeutig und sinnfällig (Folge zu v1.99.46)"
slug: 2026-11-23-bugfix-dashboard-symbole-nachschaerfen-spec
status: Testbereit
created: 2026-11-23
updated: 2026-11-23
version_vorschlag: "1.99.50 (Worktree steht auf 1.99.47 laut IdealAkeWms/AppVersion.cs:5; 1.99.48 Sage-AG-Bugfix und 1.99.49 BDE-Liste Isolierungsfräse sind in Entwürfen reserviert; 1.99.50 kommt im Brain nirgends vor. Dev-Lauf prüft erneut)"
testszenario_vorschlag: "TS-175 (docs/TESTSZENARIEN.md im Worktree: höchstes Kapitel TS-174 Z. 13507, TS-173 Z. 13411; TS-171/TS-172 in Entwürfen reserviert, TS-165 reserviert; TS-175 kommt im Brain nirgends vor. Dev-Lauf prüft erneut)"
source_backlog: "[[2026-11-20-dashboard-symbole-nachschaerfen]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
vorgaenger: "[[2026-11-20-bugfix-dashboard-kacheln-symbole-fehlen-spec]]"
affected_code:
  - "IdealAkeWms/Services/Navigation/NavigationKatalog.cs:23-53 (Sym-Konstanten: neue Formen, SymTrash :33 und SymCartFill :44 entfallen), :56-133 (Symbol-Argument der betroffenen Kachel-Felder)"
  - "IdealAkeWms.Tests/Services/NavigationSichtTests.cs:86-92 (neben JedeKachel_HatEinSymbol neuer Wächter KachelSymbole_SindEindeutig)"
  - "docs/anwender/18-startseite-und-menue.md:3 und :48-49 + Fotos docs/anwender/bilder/18-01-startseite-ake.jpg, 18-02-startseite-ideal.jpg, 18-05-startseite-handy.jpg"
  - "IdealAkeWms/Views/Help/Index.cshtml:472-485 (Hilfe „Startseite und Menue“), Views/Help/Changelog.cshtml, docs/TESTSZENARIEN.md (TS-175), beide AppVersion.cs"
open_questions:
  - "1. Umfang: Variante B (alle Doppelbelegungen im Katalog auflösen, 21 Kacheln bekommen ein neues Symbol, Wächter „kein Symbol doppelt“) oder Variante A (nur die 9 im Backlog genannten Kacheln, kein Eindeutigkeits-Wächter)? Empfehlung: B."
  - "2. Symboltabelle „Ziel je Kachel“ (inkl. der selbstgezeichneten einfachen Formen: Lagerplatz-Rahmen mit Minus/Pfeil/Ausrufezeichen/Fächern, Blatt mit Σ/Zeilen/Tropfen, Eingangskorb, Tacho, Regal, Uhr, Werkbank, Sanduhr, Glühbirne, Schlüssel, Blechtafel, LKW, Tropfen) so übernehmen? Empfehlung: ja; Änderungswünsche je Zeile nennen."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: "B"
freigabe_von: "Gerald"
freigabe_am: "2026-11-23"
---

## Ziel / Nutzen (das Warum)

Seit v1.99.46 hat jede Startseiten-Kachel wieder ein Symbol ([[2026-11-20-bugfix-dashboard-kacheln-symbole-fehlen-spec]]). Am Doku-Foto
`18-02-startseite-ideal.jpg` zeigt sich: Mehrere Kacheln teilen dasselbe Symbol, drei zeigen ein leeres Blatt, und zwei Symbole führen in die Irre
(Cockpit als Einkaufswagen, „Lagerplatz ausbuchen“ als Papierkorb, also „löschen“). Wer auf Terminal oder Handy eine Kachel über das Symbol
sucht, greift daneben. Ziel: **Jede Kachel hat ein eigenes, zur Tätigkeit passendes Symbol.** Ein Wächter-Test verhindert, dass später wieder ein
Symbol doppelt vergeben wird.

### Ist-Stand (belegt, Worktree `2026-08-07-ideal-teile-1-5`, v1.99.47)

- **Eine Quelle:** Symbole stehen als `private const string Sym…` in `NavigationKatalog.cs:26-53`. Jede Kachel bekommt ihr Symbol als 7. Argument
  `SymbolPfad` (`:9-10`, Kachel-Felder `:56-133`). `Views/Home/Index.cshtml:26` rendert nur `<path d="@z.SymbolPfad" />` mit `aria-hidden="true"`
  und `focusable="false"`. An der View ändert diese Spec nichts.
- **Wächter heute:** `NavigationSichtTests.cs:86-92` `JedeKachel_HatEinSymbol` prüft nur „nicht leer“ und `^M[0-9A-Za-z .,-]+$`. Ob ein Symbol
  doppelt ist oder als volle Fläche rendert, prüft kein Test (vgl. [[fallstricke]] §58, Nachtrag QA v1.99.46).
- **Kein Bootstrap-Icons-Paket:** Unter `IdealAkeWms/wwwroot/lib/` liegt nichts zu bootstrap-icons. `bi bi-…` kommt nur in
  `Users/Create.cshtml:44`, `Users/Edit.cshtml:56` (`<i>` ohne Font) und `Tracking/Index.cshtml:233` (Inline-SVG) vor. Neue Symbole kommen also
  wie bisher aus Inline-SVGs im Projekt oder sind selbst gezeichnet.

### Fundstellen der Backlog-Kacheln

| Backlog-Punkt | Kachel (Key) | Feld | Symbol-Konstante heute | Befund |
|---|---|---|---|---|
| doppelt | Umbuchung | `NavigationKatalog.cs:59` | `SymDoppelpfeil` `:28` | gleich wie Lagerplatz umbuchen |
| doppelt | LagerplatzUmbuchen | `:64` | `SymDoppelpfeil` `:28` | gleich wie Umbuchung |
| doppelt | MehrfachEinbuchung | `:57` | `SymCollection` `:29` | gleich wie Lager: Eingehende Listen |
| doppelt | LagerEingehendeListen | `:74` | `SymCollection` `:29` | gleich wie Mehrfach-Einbuchung |
| doppelt | MeineFehlteile | `:73` | `SymWarnung` `:36` | gleich wie Lager: Fehlteile |
| doppelt | LagerFehlteile | `:75` | `SymWarnung` `:36` | gleich wie Meine Fehlteile |
| leeres Blatt | Kommissionierlisten | `:79` | `SymFile` `:45` | file-earmark ohne Inhalt (QA-Ersatz Q1 für das unvollständige PDF-Symbol) |
| leeres Blatt | BeschichtungVorschlagsliste | `:102` | `SymFile` `:45` | wie oben |
| leeres Blatt | BdeBuchungen | `:90` | `SymFile` `:45` | altes Symbol vor v1.99.45, aber ebenfalls leer |
| missverständlich | BdeCockpit | `:89` | `SymCartFill` `:44` | Einkaufswagen (altes Symbol), Cockpit hat mit Einkauf nichts zu tun |
| missverständlich | LagerplatzAusbuchen | `:63` | `SymTrash` `:33` | Papierkorb, liest sich wie „löschen“ |
| QA Q1 | Einstellungen | `:130` | `SymPencil` `:41` | Stift (QA-Ersatz Q1), gleich wie FA-Vervollständigen `:85` |

Ursache „leeres Blatt“ (aus QA-Nachweis der Vorgänger-Spec belegt): Die Quelle `FaHierarchyKommissionierListen/Summiert.cshtml:112` ist ein
unvollständiger Bootstrap-Pfad (volle Fläche). QA hat deshalb auf `SymFile` ausgewichen, also das leere Blatt. Ebenso beim Zahnrad: Die Quelle
`Settings/Index.cshtml:15` endet nach dem äußeren Umriss, der innere Ring (`…zm-2.633.283c…`) fehlt.
**Ein vollständiges Zahnrad steht im Projekt:** `wwwroot/js/column-preferences.js:415-416` (Knopf „Spalten konfigurieren“). Dort enthält `:416`
den inneren Ring, und `:415` ist mit `z` geschlossen.

### Weitere Doppelbelegungen (belegt: Zählung der `Sym…`-Argumente in `NavigationKatalog.cs:56-133`)

13 Konstanten sind mehr als einer Kachel zugeordnet. Das betrifft 31 der 46 Kacheln:

| Konstante | Kacheln (Zeile) |
|---|---|
| `SymDoppelpfeil` | Umbuchung :59, LagerplatzUmbuchen :64 |
| `SymCollection` | MehrfachEinbuchung :57, LagerEingehendeListen :74 |
| `SymBox` | Bestaende :60, Vormontage :87 |
| `SymFileText` | Bewegungshistorie :61, AktivitaetsProtokoll :133 |
| `SymCart2` | OffeneBestellungenLager :66, Lagerbestellungen :72, OffeneBestellungenEinkauf :107 |
| `SymWarnung` | MeineFehlteile :73, LagerFehlteile :75 |
| `SymGrid` | LeitstandKommissionierung :80, Werkbaenke :113 |
| `SymJournalText` | FaListe :83, BfAuftraege :99, Beschichtungsauftraege :103 |
| `SymPencil` | FaVervollstaendigen :85, Einstellungen :130 |
| `SymCheckSquare` | WareneingangZuBestellung :67, FaAbarbeitungsliste :86, Rollen :127 |
| `SymFile` | Kommissionierlisten :79, BdeBuchungen :90, BeschichtungVorschlagsliste :102 |
| `SymGeoAlt` | TeileverfolgungOseon :94, Lagerplaetze :112, Standorteinstellungen :132 |
| `SymCart` | BfWarenkorb :98, Bestellvorschlaege :106 |

Auf einer Startseite stehen alle Kategorien untereinander. Darum fällt jede Doppelung auf, nicht nur die innerhalb einer Kategorie. Das Foto
`18-01-startseite-ake.jpg` zeigt in der Reihe Einkauf/Wareneingang zweimal denselben gefüllten Wagen (Offene Bestellungen und Offene Bestellungen
Einkauf, beide `SymCart2`). Dazu kommen drei fast gleiche Wagen (`SymCart`, `SymCart2`, `SymCartFill`) bei sechs Kacheln.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (Variante B, Empfehlung, Rückfrage 1)**
- 21 Kacheln bekommen ein neues Symbol laut Tabelle „Ziel je Kachel“, danach ist jeder `SymbolPfad` im Katalog eindeutig.
- Neue `Sym…`-Konstanten in `NavigationKatalog.cs`. Zusammengesetzte Formen entstehen per `const`-Verkettung aus Bausteinen. `SymTrash` und
  `SymCartFill` werden nicht mehr gebraucht und entfallen.
- Neuer Wächter `KachelSymbole_SindEindeutig`, TS-175, Anwenderdoku Kapitel 18 mit drei neuen Fotos, Hilfe, Changelog, Version.

**Variante A (Alternative):** Nur die 9 Backlog-Kacheln (LagerplatzAusbuchen, LagerplatzUmbuchen, LagerEingehendeListen, LagerFehlteile,
Kommissionierlisten, BeschichtungVorschlagsliste, BdeBuchungen, BdeCockpit, Einstellungen) mit denselben Zielsymbolen wie unten. Es gibt dann keinen
Eindeutigkeits-Wächter, weil 8 Doppelbelegungen bestehen blieben (`SymBox`, `SymFileText`, `SymCart2`, `SymGrid`, `SymJournalText`,
`SymCheckSquare`, `SymGeoAlt`, `SymCart`; u. a. zwei gleiche Wagen auf AKE).

**Out-of-Scope**
- `Home/Index.cshtml`, CSS, Größe und Farbe des Symbols, `aria-hidden`. Das bleibt wie v1.99.46.
- Kachelmenge, Kategorien, Reihenfolge, Titel, Beschreibungen, `NavigationSicht`, Rechte, Menü (`_Layout.cshtml` hat keine Symbole).
- Kacheln ohne Doppelung und ohne Backlog-Befund behalten ihr Symbol, darunter alle 9 alten Symbole außer Cockpit (Beschluss F3 der Vorgänger-Spec).
  Ausnahme: Benutzer = Schloss bleibt, obwohl eine Person sinnfälliger wäre. Ein Wunsch dazu genügt in Rückfrage 2.
- Icon-Bibliothek, Font, Sprite, eigenes Partial.
- Die unvollständigen Quell-Pfade in `Settings/Index.cshtml:15` und `Summiert.cshtml:112` (16-px-Knöpfe, Bestand). Sie werden nicht angefasst.

## Fachliche Anforderungen

F1 **Eindeutig:** Keine zwei Kacheln im Katalog haben denselben `SymbolPfad` (Wächter-Test). Auch ähnliche Formen (z. B. zwei Wagen) unterscheiden
sich auf 32 px sichtbar.

F2 **Sinnfällig, mit Familien:** Gleiche Grundform heißt gleiche Art von Sache. Die Bedeutung trägt weiter der Titel:
- **Einzelbuchung:** Plus / Minus / Doppelpfeil (unverändert).
- **Ganzer Lagerplatz / Lager-Sicht:** ein hohler Rahmen (Lagerfach) mit Minus (ausbuchen), Pfeil (umbuchen), Ausrufezeichen (Lager: Fehlteile)
  oder Fächerkreuz (Stammdaten Lagerplätze). Kein Papierkorb, denn ausgebucht wird Bestand, gelöscht wird nichts.
- **Listen:** ein Blatt mit Inhalt: Σ (summierte Kommissionierliste), Zeilen (BDE-Buchungen), Tropfen (Beschichtungs-Vorschlagsliste).
- **Beschichtung:** Tropfen.

F3 **Keine volle Fläche:** Jedes Symbol zeigt auf 32 px seine Löcher und Innenformen (Rahmen hohl, Ring hohl, Löcher frei). Vor der Übernahme
wird es einmal groß gerendert ([[fallstricke]] §58).

F4 **Regeln der Vorgänger-Spec gelten weiter:** Inline-SVG, eine Quelle (ADR [[0034-navigation-menue-und-kacheln-aus-einer-quelle]]), keine
Icon-Bibliothek, `aria-hidden`. Die Pfade nutzen nur `nonzero`, ohne `fill-rule`. Selbstgezeichnete einfache Formen sind erlaubt (Antwort 2 der
Vorgänger-Spec).

F5 **Bestand unverändert:** Kacheln, Kategorien, Reihenfolge, Titel, Beschreibungen und Sichtbarkeit bleiben wie v1.99.47.

### Ziel je Kachel (Variante B; Rückfrage 2)

„Projekt“ = Pfad wörtlich aus der genannten Fundstelle. „selbst“ = neue einfache Form (Pfadvorschlag unten). Fett = im Backlog genannt.

| Kategorie | Key (Zeile) | heute | Ziel | Quelle |
|---|---|---|---|---|
| Lager | Umbuchung :59 | Doppelpfeil | Doppelpfeil (bleibt) | `SymDoppelpfeil` |
| Lager | MehrfachEinbuchung :57 | collection | collection (bleibt) | `SymCollection` |
| Lager | Bestaende :60 | box | **Regal mit Kisten** | selbst `SymRegal` |
| Lager | **LagerplatzAusbuchen :63** | Papierkorb | **Lagerfach-Rahmen mit Minus** | selbst `SymFachRahmen` + Minus |
| Lager | **LagerplatzUmbuchen :64** | Doppelpfeil | **Lagerfach-Rahmen mit Pfeil nach rechts** | selbst `SymFachRahmen` + Pfeil |
| Bestellungen | Lagerbestellungen :72 | cart2 | cart2 (bleibt, einziger gefüllter Wagen) | `SymCart2` |
| Bestellungen | MeineFehlteile :73 | Warndreieck | Warndreieck (bleibt) | `SymWarnung` |
| Bestellungen | **LagerEingehendeListen :74** | collection | **Eingangskorb mit Pfeil hinein** | selbst `SymEingangskorb` |
| Bestellungen | **LagerFehlteile :75** | Warndreieck | **Lagerfach-Rahmen mit „!“** | selbst `SymFachRahmen` + Ausrufezeichen |
| Kommissionierung | **Kommissionierlisten :79** | leeres Blatt | **Blatt mit Σ** („summiert“) | `SymFile` + selbst Σ |
| Kommissionierung | LeitstandKommissionierung :80 | grid (alt) | grid (bleibt) | `SymGrid` |
| Produktion/BDE | FaVervollstaendigen :85 | Stift | Stift (bleibt) | `SymPencil` |
| Produktion/BDE | FaAbarbeitungsliste :86 | check-square | check-square (bleibt) | `SymCheckSquare` |
| Produktion/BDE | Vormontage :87 | box | box (bleibt, wie Legende „Baugruppe“ der FA-Struktur) | `SymBox` |
| Produktion/BDE | **BdeCockpit :89** | Einkaufswagen | **Tacho** (laufende Arbeit im Überblick) | selbst `SymTacho` |
| Produktion/BDE | **BdeBuchungen :90** | leeres Blatt | **Blatt mit Zeilen** | `SymFile` + selbst Zeilen |
| Produktion/BDE | TeileverfolgungOseon :94 | geo-alt (alt) | geo-alt (bleibt) | `SymGeoAlt` |
| Blechfertigung | BfWarenkorb :98 | cart | cart (bleibt, Umriss-Wagen) | `SymCart` |
| Blechfertigung | BfAuftraege :99 | journal-text | **Blechtafel mit zwei Löchern** | selbst `SymBlechtafel` |
| Beschichtung | **BeschichtungVorschlagsliste :102** | leeres Blatt | **Blatt mit Tropfen** | `SymFile` + selbst kleiner Tropfen |
| Beschichtung | Beschichtungsauftraege :103 | journal-text | **Tropfen** (groß) | selbst `SymTropfen` |
| Einkauf/Wareneingang | OffeneBestellungenLager :66 | cart2 | **Sanduhr** (offen, Lieferung steht aus) | selbst `SymSanduhr` |
| Einkauf/Wareneingang | WareneingangZuBestellung :67 | check-square | **LKW** (Lieferung kommt an) | selbst `SymLkw` |
| Einkauf/Wareneingang | Bestellvorschlaege :106 | cart | **Glühbirne** (Vorschlag) | selbst `SymGluehbirne` |
| Einkauf/Wareneingang | OffeneBestellungenEinkauf :107 | cart2 | **Kalender** (Liefertermine) | Projekt `Help/Index.cshtml:749` (= `wwwroot/js/table-filter.js:195`) |
| Stammdaten | Lagerplaetze :112 | geo-alt | **Lagerfach-Rahmen mit Fächerkreuz** | selbst `SymFachRahmen` + Kreuz |
| Stammdaten | Werkbaenke :113 | grid | **Werkbank** | selbst `SymWerkbank` |
| Administration | Rollen :127 | check-square | **Schlüssel** | selbst `SymSchluessel` |
| Administration | **Einstellungen :130** | Stift | **Zahnrad (vollständig)** | Projekt `wwwroot/js/column-preferences.js:415` + `:416` |
| Administration | Standorteinstellungen :132 | geo-alt | **Haus** (Standort) | Projekt `FaHierarchy/Index.cshtml:13` (Legende „Wurzel/Endprodukt“) |
| Administration | AktivitaetsProtokoll :133 | file-earmark-text | **Uhr** (Läufe der Dienste) | selbst `SymUhr` |

Alle übrigen Kacheln bleiben unverändert: Einbuchung, Ausbuchung, Bewegungshistorie, Artikelinfo, Bedarfsmeldungen, Kommissionierung, FaListe,
FaStruktur, BdeTerminal, OseonAgUebersicht, Artikel, Benutzer, Arbeitsplaetze, Arbeitsgaenge, ArbeitsgaengeSage. Diese 15 plus die 10
„bleibt“-Zeilen oben plus die 21 neuen ergeben 46 Kacheln, jede mit eigenem Pfad.

**Bewusst ähnlich, aber verschieden:** Lagerbestellungen (gefüllter Wagen `SymCart2`) und BF-Warenkorb (Umriss-Wagen `SymCart`). Beides heißt
„bestellen“ (F2), die Formen unterscheiden sich sichtbar (Foto 18-02).

### Pfadvorschläge der selbstgezeichneten Formen (16×16, `nonzero`)

Regel für alles Selbstgezeichnete: Außenformen **im Uhrzeigersinn**, Löcher **gegen den Uhrzeigersinn**. Formen innerhalb eines Lochs (Rahmen-,
Blatt- oder Ring-Innenraum) dürfen beliebig laufen. Überlappende Teilformen laufen gleichsinnig. Die Pfade sind Vorschläge mit von Hand
nachgerechneter Geometrie. **Maßgeblich ist die Sichtprüfung auf 64 px.** Der Dev-Lauf darf Maße feinjustieren, das Motiv aber nicht wechseln.
Wechselt er doch, vermerkt er es in der Spec.

| Konstante | Pfad (Vorschlag) |
|---|---|
| `SymFachRahmen` (Baustein, keine eigene Kachel) | `M1 2h14v12H1zm1 1v10h12V3z` |
| LagerplatzAusbuchen | `SymFachRahmen + " M4 7h8v2H4z"` |
| LagerplatzUmbuchen | `SymFachRahmen + " M4 7h4V5l4 3-4 3V9H4z"` |
| LagerFehlteile | `SymFachRahmen + " M7 4h2v5H7zm0 6h2v2H7z"` |
| Lagerplaetze | `SymFachRahmen + " M7.5 3h1v10h-1zM2 7.5h12v1H2z"` |
| Kommissionierlisten | `SymFile + " M5 5h6v1.5H7.3l2 2.5-2 2.5H11V13H5v-1l2.5-3L5 6z"` |
| BdeBuchungen | `SymFile + " M5 6h6v1H5zm0 2.5h6v1H5zm0 2.5h6v1H5zm0 2.5h4v1H5z"` |
| BeschichtungVorschlagsliste | `SymFile + " M8 6c-1 1.5-2.5 3-2.5 4.75a2.5 2.5 0 0 0 5 0C10.5 9 9 7.5 8 6z"` |
| `SymTropfen` | `M8 1C6 4 3 7 3 10a5 5 0 0 0 10 0c0-3-3-6-5-9z` |
| `SymEingangskorb` | `M7 1h2v4h2.5L8 8.5 4.5 5H7zM1 9h3l1.5 2h5L12 9h3v6H1z` |
| `SymTacho` | `M8 3a7 7 0 0 0-7 7v2h2v-2a5 5 0 0 1 10 0v2h2v-2a7 7 0 0 0-7-7zM8 8.5a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3M7.5 9.5l3-3 .7.7-3 3z` |
| `SymRegal` | `M1 1h1v14H1zm13 0h1v14h-1zM2 5h12v1H2zm0 5h12v1H2zm0 4h12v1H2zM3 2h3v3H3zm5 1h4v2H8zM4 7h4v3H4zm6 1h3v2h-3zM3 12h3v2H3zm5-.5h5V14H8z` |
| `SymUhr` | `M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1m0 1.5a5.5 5.5 0 1 1 0 11 5.5 5.5 0 0 1 0-11M7.25 4h1.5v4.25l2.5 1.5-.75 1.3-3.25-2z` |
| `SymWerkbank` | `M0 5h16v2H0zm1 2h2v8H1zm12 0h2v8h-2zM3 11h10v1H3zM5 2h6v3H5z` |
| `SymSanduhr` | `M3 1h10v2h-1c0 2.5-2 4-3 5 1 1 3 2.5 3 5h1v2H3v-2h1c0-2.5 2-4 3-5-1-1-3-2.5-3-5H3z` |
| `SymGluehbirne` | `M8 1a5 5 0 0 0-3 9v2h6v-2a5 5 0 0 0-3-9M5.5 13h5v1h-5zm1 1.5h3V16h-3z` |
| `SymSchluessel` | `M5 3a4 4 0 1 0 0 8 4 4 0 0 0 0-8m0 2.5a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3M8.5 6v2H10v1.5h2V8h1v2h2V8h1V6z` |
| `SymBlechtafel` | `M4 4h11l-3 8H1zM5 7a1 1 0 1 0 0 2 1 1 0 0 0 0-2m6 0a1 1 0 1 0 0 2 1 1 0 0 0 0-2` |
| `SymLkw` | `M0 3h10v9H0zm11 3h2.5l2.5 3v3h-5zM3.5 11.5a1.75 1.75 0 1 1 0 3.5 1.75 1.75 0 0 1 0-3.5m8 0a1.75 1.75 0 1 1 0 3.5 1.75 1.75 0 0 1 0-3.5` |

Geprüfte Stolperstellen: Die Tacho-Nadel endet innerhalb des inneren Bogens (Radius 4,25 < 5). Sie schneidet den Bogen also nicht, dort entstünde
sonst ein Loch. Der Schlüsselschaft läuft gleichsinnig mit dem äußeren Kreis (gegen den Uhrzeigersinn), damit die Überlappung bei x 8,5–9 gefüllt
bleibt. Die Inhalte von Blatt, Rahmen und Uhr liegen ganz im jeweiligen Innenraum (`SymFile`-Innenraum x 3–13, y 4,5–15; Rahmen x 2–14, y 3–13).

## Technischer Loesungsentwurf

Ponytail-Leiter: Feld, View, Wächter-Gerüst und Ablageort gibt es schon (v1.99.46). Es ändern sich nur Konstanten und Argumente in **einer**
Datei. Dazu kommt ein Test mit vier Zeilen. Kein CSS, kein Markup, kein Helper.

1. **`NavigationKatalog.cs:23-53`:** Den Kommentar `:23-25` um eine Zeile ergänzen (v1.99.50: Symbole eindeutig, Familien Rahmen/Blatt/Tropfen,
   Zahnrad aus `column-preferences.js:415-416`). Neue Konstanten laut Tabelle anlegen, Projekt-Pfade wörtlich übernehmen. Mehrere `<path>` werden
   mit Leerzeichen verkettet (wie v1.99.46). Bausteine per `const`-Verkettung (`private const string SymLagerplatzAusbuchen = SymFachRahmen + " M4 7h8v2H4z";`),
   so steht jeder Rahmen- und Blatt-Pfad nur einmal. `SymTrash` (:33) und `SymCartFill` (:44) löschen, sie werden nicht mehr gebraucht.
2. **`:56-133`:** Bei den 21 Kacheln nur das 7. Argument tauschen. Key, Titel, Controller, Action, Kategorie, Beschreibung, `Alle` und `Kategorien`
   bleiben unverändert (F5).
3. **Vorab-Render (F3, [[fallstricke]] §58):** Der Dev-Lauf schreibt eine temporäre statische HTML-Datei außerhalb des Repos mit allen 46
   Kachel-Pfaden (64 px, Name daneben). Er rendert sie mit Headless-Edge und hängt den Screenshot als Nachweis in die Spec, nicht ins Repo.
   Jede neue Form muss dort ohne volle Fläche erscheinen.
4. **Wächter** (`IdealAkeWms.Tests/Services/NavigationSichtTests.cs`, neben `:86-92`):
   ```csharp
   [Fact]
   public void KachelSymbole_SindEindeutig()
   {
       var doppelt = NavZiele.Alle.Where(z => z.Kategorie != null).GroupBy(z => z.SymbolPfad)
           .Where(g => g.Count() > 1).Select(g => string.Join("/", g.Select(z => z.Key))).ToList();
       doppelt.Should().BeEmpty("jede Kachel braucht ein eigenes Symbol");
   }
   ```
   „Kein leerer Pfad“ prüft schon `JedeKachel_HatEinSymbol` (`:88-89`: `IsNullOrWhiteSpace` + Regex). Er bleibt unverändert. Alle neuen Pfade
   bestehen nur aus `[0-9A-Za-z .,-]`. Ob eine Form als volle Fläche rendert, kann ein Unit-Test ohne Renderer nicht sehen. Das deckt der Vorab-Render
   (Punkt 3) und TS-175.3 ab. Ein eigener Render-Test wäre eine neue Testinfrastruktur (Browser im CI) und wird nicht gebaut.
5. **Frontend:** Skill `frontend-design` aufrufen (CLAUDE.md), Leitplanke Konsistenz. Es gibt keine neue Klasse und kein neues Markup.
6. **Brain-Nachzug im Dev-Lauf:** [[fallstricke]] §58 additiv um einen Satz („Kachel-Symbole sind eindeutig, Wächter
   `KachelSymbole_SindEindeutig`; Familien Rahmen = Lagerplatz/Lager, Blatt = Liste, Tropfen = Beschichtung“). ADR 0034 bleibt unverändert.

Betroffene Module: [[controller]] (Start/Hilfe), [[module]].

**Variante A** (falls gewählt): Es ändern sich nur die 9 fett markierten Backlog-Zeilen. Dafür werden `SymFachRahmen`, Σ, Zeilen, kleiner Tropfen,
Eingangskorb, Tacho und Zahnrad gebaut. `SymTrash`/`SymCartFill` entfallen, Punkt 4 entfällt.

## Migrations-/SQL-Auswirkungen

Keine. Es gibt keine Migration, kein `SQL/XX`, keinen `00_FreshInstall`-Eintrag und keine AgentJobs. Die Änderung betrifft nur Code-Konstanten.

## Einstellungen

keine.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Es ist ein reiner Lesepfad, nichts wird geschrieben.

Rollen und Zugriffsfilter bleiben unverändert (`NavigationSicht` nicht im Diff). Listen-View-Pattern entfällt (keine Tabelle).

## Akzeptanzkriterien

1. `KachelSymbole_SindEindeutig` ist grün: Kein `SymbolPfad` kommt bei zwei Kacheln vor. (Nur Variante B.)
2. `JedeKachel_HatEinSymbol` ist unverändert grün.
3. Die 21 Kacheln (Variante B) bzw. 9 Kacheln (Variante A) zeigen das Zielsymbol laut freigegebener Tabelle. Die übrigen Kacheln zeigen dasselbe
   Symbol wie v1.99.47.
4. Kein Symbol rendert auf 32 px als volle Fläche. Rahmen, Uhr-Ring und Zahnrad-Ring sind hohl, die Blechtafel hat zwei Löcher, und Σ, Zeilen und
   Tropfen stehen sichtbar im Blatt. Nachweis: Vorab-Render-Screenshot in der Spec + TS-175.3.
5. „Lagerplatz ausbuchen“ zeigt keinen Papierkorb, Cockpit keinen Wagen, Einstellungen ein Zahnrad.
6. `SymTrash` und `SymCartFill` sind aus `NavigationKatalog.cs` entfernt. Kein Symbolpfad steht außerhalb von `NavigationKatalog.cs`, und
   `Home/Index.cshtml` ist nicht im Diff.
7. **Bestandsverhalten unverändert:** Im Diff von `NavigationKatalog.cs` ändern sich je Kachelzeile nur das 7. Argument und die Konstanten. Kacheln,
   Kategorien, Reihenfolge, Titel, Beschreibungen und Menü sind wie v1.99.47. Alle bestehenden `NavigationSichtTests` sind ohne Anpassung grün.
8. Anwenderdoku Kapitel 18 nennt die Symbolfamilien, die Fotos 18-01, 18-02 und 18-05 zeigen die neuen Symbole, und die Hilfe „Startseite und Menue“
   hat den Satz zu den Familien.
9. Build und alle Tests (Web + Service) sind grün.

## Test-Szenarien

Neues Kapitel **TS-175 „Startseite: Kachel-Symbole eindeutig und sinnfällig (v1.99.50)“** in `docs/TESTSZENARIEN.md`, Querverweis auf TS-174.
Danach eine Zeile in [[testszenarien-index]]. Standort: DEV **AKE** und DEV **IDEAL**.

**Vorbedingungen:** Stand v1.99.50 auf DEV, Benutzer admin. Das TS-174-Protokoll (Kachellisten je Standort) liegt vor.

| Nr | Schritt | Erwartet |
|---|---|---|
| 175.1 | IDEAL, admin, Startseite 1440 px | Keine zwei Kacheln mit gleichem Symbol. Lagerplatz ausbuchen/umbuchen und Lager: Fehlteile zeigen den Rahmen mit Minus/Pfeil/„!“. Kommissionierlisten zeigt ein Blatt mit Σ, Vorschlagsliste ein Blatt mit Tropfen, Aufträge (Beschichtung) einen Tropfen, Buchungen ein Blatt mit Zeilen, Cockpit einen Tacho, BF-Aufträge eine Blechtafel. |
| 175.2 | AKE, admin, Startseite 1440 px | Keine zwei gleichen Symbole. In Einkauf/Wareneingang stehen Sanduhr, LKW, Glühbirne und Kalender (kein doppelter Wagen). OSEON Aufträge zeigt weiter den Ortsmarker, Stammdaten Lagerplätze den Rahmen mit Fächerkreuz. |
| 175.3 | Sichtprüfung je neues Symbol, Browser-Zoom 200 % | Keine volle Fläche. Rahmen, Uhr-Ring und Zahnrad hohl, Blechtafel mit zwei Löchern, Tacho-Nadel ohne Lücke, Schlüssel ohne Kerbe am Schaft. |
| 175.4 | Handy 390 px (beide Standorte) | Eine Spalte, Symbol ganz, kein Querscrollen. |
| 175.5 | Bestandsvergleich mit TS-174-Protokoll | Gleiche Kacheln, Kategorien, Reihenfolge, Titel, Beschreibungen. Nur die Symbole der 21 (bzw. 9) Kacheln sind anders. |
| 175.6 | Negativ, Screenreader (Edge „Vorlesen“) über Cockpit und Lagerplatz ausbuchen | Vorgelesen werden nur Titel + Beschreibung, kein „Grafik“. |
| 175.7 | Negativ, Graustufen-Ansicht | Rahmen-, Blatt- und Tropfen-Familien bleiben unterscheidbar. Jede Kachel ist am Titel erkennbar. |

**Automatisch:** `KachelSymbole_SindEindeutig` (neu), `JedeKachel_HatEinSymbol`, T4 Startseiten-Wächter (unverändert).

## Etappen (nur bei epic: true)

Kein Epic.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vorläufig – vom QA-Lauf 2026-11-23 bestätigt, siehe „QA-Nachweis → Deploy (final)“ am Ende. Abweichung: Paket ist das Bündel v1.99.53.*

- **Web-App:** ja (`NavigationKatalog.cs`, `Views/Help/Index.cshtml`, `Views/Help/Changelog.cshtml`, `AppVersion.cs`).
- **Service:** ja, **nur Versionsnummer** (`IDEALAKEWMSService/AppVersion.cs` → 1.99.50). Web und Dienst melden so denselben Stand, wie bei v1.99.46.
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine. Es gibt kein SQL, kein Einmal-Skript, keine Einstellung, keinen Leitstand-Schritt
  und keine Anwender-Info (der Anwender-Changelog genügt).
- **Publish-Befehle:**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Doku-Pflichten (Hilfe-Detail-Regel):**
  - `docs/anwender/18-startseite-und-menue.md:3`: Stand 1.99.50, Fotos 18-01/18-02/18-05 mit 1.99.50.
  - `:48-49`: nach dem Satz zu Plus/Minus/Doppelpfeil ergänzen: „Ein hohler Rahmen steht für einen ganzen Lagerplatz (mit Minus = Lagerplatz
    ausbuchen, Pfeil = umbuchen, ‚!‘ = Lager: Fehlteile). Ein Blatt mit Inhalt ist eine Liste, ein Tropfen steht für Beschichtung. Jedes Symbol gibt es
    nur einmal.“
  - Fotos per Playwright gegen DEV neu aufnehmen (gleiche Maße wie bisher): **18-01** AKE 1440 px, **18-02** IDEAL 1440 px, **18-05** Handy 390 px.
    18-03/18-04 bleiben (Menü und Fokusring ohne Änderung).
  - `Views/Help/Index.cshtml:474-480` („Startseite und Menue“): ein Satz zu den Symbolfamilien wie oben, mit Hinweis „Das Symbol ist nur
    Erkennungshilfe, maßgeblich ist der Name“.
  - `Views/Help/Changelog.cshtml` (v1.99.50: „Startseite: jedes Symbol nur einmal; Lagerplatz ausbuchen ohne Papierkorb, Cockpit als Tacho,
    Einstellungen als Zahnrad, Listen mit Inhalt statt leerem Blatt“), Stand in `docs/anwender/README.md`.
  - Brain: Changelog, [[feature-map]] (Startseite), [[fallstricke]] §58 (ein Satz), [[testszenarien-index]].

## Offene Rueckfragen

1. **Umfang:** Variante B = alle 13 Doppelbelegungen im Katalog auflösen. 21 Kacheln bekommen ein neues Symbol, der Wächter
   `KachelSymbole_SindEindeutig` hält es dauerhaft. Variante A = nur die 9 Backlog-Kacheln, ohne Eindeutigkeits-Wächter.
   **Empfehlung: B.** Die Startseite zeigt alle Kategorien zusammen, und auf AKE stehen heute zwei identische Wagen nebeneinander
   (Foto 18-01). Nur mit B kann ein Test „jede Kachel eindeutig“ dauerhaft absichern. Mehraufwand: 12 weitere Konstanten und Argumente in derselben
   Datei, keine weitere Datei.
2. **Symboltabelle „Ziel je Kachel“** samt selbstgezeichneter Formen übernehmen: Lagerfach-Rahmen mit Minus/Pfeil/„!“/Fächerkreuz, Blatt mit
   Σ/Zeilen/Tropfen, Eingangskorb, Tacho, Regal, Uhr, Werkbank, Sanduhr, Glühbirne, Schlüssel, Blechtafel, LKW, Tropfen; aus dem Projekt
   Zahnrad, Haus, Kalender. **Empfehlung: ja.** Antwort 2 der Vorgänger-Spec erlaubt einfache Eigenformen. Im Projekt gibt es kein passendes Symbol
   für Tacho, LKW, Regal & Co., und ein Bibliotheks-Import ist ausgeschlossen. Änderungswünsche bitte je Zeile (Key → Motiv). Benutzer = Schloss bleibt,
   außer hier wird eine Person gewünscht.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Umfang Variante A oder B → **Variante B, wie empfohlen (Gerald 2026-11-23)**
2. Symboltabelle „Ziel je Kachel“ inkl. selbstgezeichneter Formen übernehmen → **ja, wie empfohlen (Gerald 2026-11-23)**

## QA-Nachweis (qa-agent, 2026-11-23)

Bündel-QA im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Bereich `0b6fff36..a93bd910`
(vier Specs v1.99.50–v1.99.53 aus [[2026-11-23-goal-lauf-dashboard-we-lagerbestellung]]). Die QA prüft je Spec den eigenen Commit-Bereich, Build und Tests decken den ganzen Worktree ab.

### 1. Build und Tests

- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 30 Warnungen (alle bekannt, u. a. CS8620 in `BfAuftragWorkflowTests`, NU1903 Pkcs).
- `dotnet test` vor dem QA-Fix: IdealAkeWms.Tests 3472 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 790 / 0 / 0.
- Nach dem QA-Fix `a93bd910`: IdealAkeWms.Tests **3473 bestanden / 1 übersprungen / 0 Fehler**, IDEALAKEWMSService.Tests **790 / 0 / 0**.

### 6. DEV-Deploy

- `powershell -ExecutionPolicy Bypass -File D:\Dev\scripts\dev-deploy.ps1 -Standort AKE|IDEAL -Quelle <worktree>` in einer erhöhten Shell, Web und Dienst,
  zweimal (nach Build-Grün `f7b26ebc`, nach dem QA-Fix `a93bd910`). Ausgabe jeweils „HTTP 200, Fusszeile v1.99.53 … Fertig: AKE v1.99.53“ bzw. „IDEAL v1.99.53“, Exit 0.
- AKE http://localhost:8081 und IDEAL http://localhost:8082 zeigen in der Fußzeile **v1.99.53** = `AppVersion` des Branches.
- Beide DEV-DBs: letzte Migration `20261123124036_AddStockMovementWarehouseRequisitionItem`, Spalte `StockMovements.WarehouseRequisitionItemId` vorhanden.
- DEV-Sicherheit geprüft: ServiceSetting `SageLagerbuchungAktiv` = false auf AKE_WMS_DEV und IDEAL_WMS_DEV, kein `Sync:*`, keine Mail eingeschaltet.
  `SData:BaseUrl` zeigt auf AKE DEV auf sagetest01, auf IDEAL DEV auf idealsage2. Deshalb wurde keine Sage-Übergabe ausgelöst.

### 7. Publish

`<worktree>\publish\` geleert und neu erstellt: `IDEALAKEWMSWeb` + `IDEALAKEWMSWebService` (`dotnet publish -c Release`, beide Exit 0), `SQL\` → `publish\SQL\`
(inkl. `166_…`), `scripts\deploy\` → `publish\deploy\` (inkl. `Update-IdealAkeWms.ps1`), `publish-info.txt`: Version 1.99.53, Commit
`a93bd91039e22bae194422e11e253d1cd6cbab78`, Branch, vier Spec-Slugs, Erstellt 2026-11-23 14:12, Komponenten IDEALAKEWMSWeb + IDEALAKEWMSWebService.
Paket-ZIP/Shares macht der Koordinator.

### Bündel-weite geänderte Bestandstests (Punkt 4)

| Test | Einordnung |
|---|---|
| `PickingControllerTests` | nur Typ: Quell-Vorschlag nach `QuellLagerplatzVorschlag.Waehle` verschoben (Lagerbestellung) |
| `PurchaseOrdersControllerTests`, `SageWareneingaengeListeTests` | nur Typ: Ctor um `IArticleRepository` (WE) |
| `PurchaseOrdersReceiveTests` | Ctor + erwartete Meldung bewusst geändert (WE F1/AK 5, Mehrdeutigkeit) |
| `WarehousePickingControllerTests` | Ctor + `GetByIdAsync`-Setup wegen Status-Guard F4 (Lagerbestellung AK 16, bewusst) |
| `NachlaufManifestDriftTests` | + SQL/166 (Lagerbestellung) |
| `CsvExportCoverageTests` | bewusst: Sortiertext `data-filter-text` (Lagerbestellung Abweichung 6) |
| `AppSettingDefinitionsTests` | Anzahl 67 → 68 (neue Einstellung Lagerbestellung) |

Keine Änderung der Klasse „sonstiges“: Keine Fixture wurde angepasst, nur damit ein Test grün wird.

### 2. Code-Review (Commit `af6eadd7`, selbst durchgeführt)

Geprüft: `NavigationKatalog.cs` (21 neue Sym-Konstanten, `SymTrash`/`SymCartFill` entfernt, nur das 7. Argument je Kachelzeile geändert),
`NavigationSichtTests` (`KachelSymbole_SindEindeutig`, `JedeKachel_HatEinSymbol`), Hilfe/Changelog/Kapitel 18, Versions-Bump in beiden `AppVersion.cs`.
Pfade sind Konstanten, gerendert über den bestehenden Partial mit `aria-hidden`. Keine Werte landen in `<script>`, `Home/Index.cshtml` ist im Commit nicht geändert.
**Befunde: keine.** Hinweise ohne Handlungsbedarf: `SymHaus` enthält „.20“ aus der Vorlage FaHierarchy/Index.cshtml:13 und rendert korrekt.
Uhr (Aktivitäts-Protokoll) und Stoppuhr (Arbeitsgänge) sind ähnlich, aber unterscheidbar.

### 3. Nachweisart je AK

| AK | Nachweis |
|---|---|
| 1, 2, 9 | automatisch (`KachelSymbole_SindEindeutig`, `JedeKachel_HatEinSymbol`, Build/Tests) |
| 3, 4, 5 | DEV-Sicht (Headless-Screenshots AKE und IDEAL 1440 px), dazu Manual-UAT TS-175 |
| 6, 7 | Diff-Review (siehe 2) + unveränderte `NavigationSichtTests` |
| 8 | Text Kapitel 18 und Hilfe vorhanden (`docs/anwender/18-startseite-und-menue.md:60-62`, `Views/Help/Index.cshtml:500-502`); **Fotos 18-01/18-02/18-05 offen** (Doku-Phase des Koordinators) |

### 5. Checkliste

Migration/SQL/FreshInstall: entfällt. Version 1.99.50 in beiden `AppVersion.cs` (der Bündel-Stand ist 1.99.53), Anwender-Changelog v1.99.50 vorhanden,
`docs/TESTSZENARIEN.md` TS-175, Testindex Zeile 175 vorhanden. Manifest: keine Einträge, richtig so. Einstellungen (ADR 0020): keine.
Brain-Changelog, feature-map und fallstricke §58 macht der Koordinator (Lauf-Notiz).

### 6c. Testlauf auf DEV (TS-175)

| Schritt | Ergebnis | Status |
|---|---|---|
| 175 IDEAL 1440 px (Headless-Edge, Kerberos) | alle Kacheln mit Symbol, keine zwei gleich, Rahmen/Uhr/Zahnrad hohl, Blechtafel mit Löchern | OK |
| 175 AKE 1440 px | alle Kategorien; Lagerplatz ausbuchen = Rahmen mit Minus, Cockpit = Tacho, Einstellungen = Zahnrad, Sterne sichtbar | OK |
| 175 Handy 390 px | Headless-Edge rendert mindestens etwa 500 px breit, der Screenshot schneidet rechts ab (Messartefakt, kein Befund) | NICHT MOEGLICH, Manual-UAT |
| Vorab-Render 32/64 px (Dev-Lauf, `scratchpad\sym.png`) | keine Vollflächen | OK |

### Deploy (final, aus dem echten Diff)

- Web: ja (`NavigationKatalog.cs`, Hilfe, Changelog, AppVersion). Dienst: ja, **nur Versionsnummer** (`IDEALAKEWMSService/AppVersion.cs`). Migration: nein.
  Manifest: nichts.
- Paket: `<worktree>\publish` mit **v1.99.53** (ganzes Bündel, Web + Dienst), Commit `a93bd910`.
- Ablauf des Menschen: Publish aus dem Worktree → Testsystem → testen → Merge. Nach dem Merge nur dann neu aus main publishen, wenn der Merge
  getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.

### Manuelle Testcheckliste (offen für den Menschen)

1. TS-175 auf dem Testsystem AKE und IDEAL bei 1440 px: jedes Symbol nur einmal, Zielsymbole laut Tabelle.
2. TS-175 am Handy oder in DevTools bei 390 px (im Headless-Lauf nicht möglich).
3. Bei 32 px am Terminal prüfen, dass kein Symbol als Vollfläche erscheint.
4. Die Doku-Fotos 18-01/18-02/18-05 sind nachgezogen (Koordinator).
