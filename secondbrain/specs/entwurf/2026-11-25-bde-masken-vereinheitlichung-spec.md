---
type: spec
title: BDE-Masken vereinheitlichen - Teile- und FA-Sicht, AG-Status-Ampel, Teilfertigmeldung, BDE mobil kompakt
slug: 2026-11-25-bde-masken-vereinheitlichung-spec
status: Entwurf
created: 2026-11-25
updated: 2026-11-25
source_backlog: "[[2026-11-25-bde-masken-vereinheitlichung]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: ""
standort: IDEAL
affected_code:
  - IdealAkeWms/Controllers/BdeTeilelisteController.cs
  - IdealAkeWms/Services/BdeTeilelisteService.cs
  - IdealAkeWms/Services/WerkbankKuerzelFamilie.cs
  - IdealAkeWms/Services/BdeBookingService.cs
  - IdealAkeWms/Services/ArbeitsgangStatusRegel.cs
  - IdealAkeWms/Models/ViewModels/FaArbeitsgangStatus.cs
  - IdealAkeWms/Models/ViewModels/BdeTeilelisteViewModel.cs
  - IdealAkeWms/Models/BdeTeilMeldung.cs
  - IdealAkeWms/Models/ProductionWorkplace.cs
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Controllers/BdeApiController.cs
  - IdealAkeWms/Controllers/BdeTerminalController.cs
  - IdealAkeWms/Controllers/BdeBookingsController.cs
  - IdealAkeWms/Controllers/ProductionWorkplacesController.cs
  - IdealAkeWms/Controllers/FaHierarchyController.cs
  - IdealAkeWms/Services/Navigation/NavigationKatalog.cs
  - IdealAkeWms/Services/ColumnDefinitions.cs
  - IdealAkeWms/Services/ColumnFilterSyntax.cs
  - IdealAkeWms/Views/BdeTeileliste/_Kopf.cshtml
  - IdealAkeWms/Views/BdeTeileliste/Index.cshtml
  - IdealAkeWms/Views/BdeTeileliste/Summiert.cshtml
  - IdealAkeWms/Views/BdeTeileliste/Anmelden.cshtml
  - IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml
  - IdealAkeWms/Views/ProductionWorkplaces/_BdeTeilelisteField.cshtml
  - IdealAkeWms/Views/ProductionWorkplaces/Index.cshtml
  - IdealAkeWms/Views/BdeBookings/Teilmeldungen.cshtml
  - IdealAkeWms/Views/Shared/_Layout.cshtml
  - IdealAkeWms/wwwroot/js/bde-teileliste.js
  - IdealAkeWms/wwwroot/js/bde-terminal.js
  - IdealAkeWms/wwwroot/css/bde.css
  - SQL/168_<Name>.sql
  - SQL/00_FreshInstall.sql
open_questions:
  - "1. (Gerald) Alte Terminal-Seite: Etappe 5 leitet nur an IDEAL (Master ProduktionsauftragHierarchisch an, BdeNurFaMeldung aus) auf die neue Seite um; fuer AKE und den NurFA-Modus bleibt die alte Seite unveraendert. Empfehlung: ja."
  - "2. (Gerald) NurFA-Modus an IDEAL: Die neue FA-Sicht gilt nur bei BdeNurFaMeldung aus (Pruefabfrage im Text). Empfehlung: ja."
  - "3. (Gerald) Erkennung V1 im Dienst bleibt an Standardsicht Teile gekoppelt; an Werkbaenken mit Standardsicht FA zeigt der Teile-Reiter einen Hinweis, dass Teilmeldungen dort keinen Vater-AG schliessen. Empfehlung: ja (Variante b)."
  - "4. (Fachbereich: Walter/Benjamin) AG-Status-Spalte in der FA-Sicht zeigt alle Arbeitsgaenge der Sub-FA, der Arbeitsgang der Zeile ist markiert. Empfehlung: ja."
  - "5. (Fachbereich: Walter/Benjamin) Tageshistorie zeigt neben den Buchungen auch die Teilmeldungen des Tages. Empfehlung: ja."
  - "6. (Fachbereich: Walter/Benjamin) Welches Konto/Geraet am Handy? Empfehlung: je Geraet ein Terminal-Benutzer, vom BDE-Admin angelegt; keine Codeaenderung."
  - "7. (Gerald) Soll die FA-Struktur die Teilmeldungen ebenfalls als Status zeigen? Empfehlung: nein, eigene Backlog-Notiz."
  - "8. Schwelle Kartenansicht 992 px oder 768 px? Empfehlung: 992 px."
  - "9. Filter-Bedienelemente 44 px ueberall (auch alte Terminal-Seite), Hinweistexte einklappbar? Empfehlung: ja."
  - "10. Spaltenfilter in der Kartenansicht nicht bedienbar (nur Zaehler + Zuruecksetzen) - akzeptabel? Empfehlung: ja."
  - "11. Aggregierte Ampel ueber ArbeitsgangStatusRegel.Ableiten: gemischt laeuft+ueberfaellig ergibt LaeuftUeberfaellig. Empfehlung: ja."
  - "12. Koordination mit SQL/164 (Isolierungsfraese-Spec), beide bauen UX_BdeTeilMeldungen_Schluessel neu. Empfehlung: wer zweitens baut, vereinigt die Filter."
  - "13. Teile-Sicht zusaetzlich an den Master-Schalter binden statt auf 'AKE hat keine Kuerzel' zu vertrauen (Pruefabfrage im Text). Empfehlung: Master-Gate."
  - "14. FA-Sicht nutzt Pagination statt BdeTerminalMaxHauptFa; die Einstellung wirkt nur noch auf der alten Terminal-Seite. Empfehlung: ja."
epic: true
etappen:
  - "1 Teile Einzeln: AG-Status, Baugruppen-Kinder als Zeilen, Filter auf Kinder, Teile-Knopf vorn, BDE mobil kompakt (v1.99.70, TS-191, keine Migration)"
  - "2 Teile Summiert: Einzelauftraege aufklappbar, Summen-Haken, aggregierte Ampel (v1.99.71, TS-192, keine Migration)"
  - "3 Teilfertigmeldung mit Restmenge, ADR 0036, Baugruppe als Teil-Buchung (v1.99.72, TS-193, SQL/168)"
  - "4 FA-Sicht als Tabelle, Buchungen im Kopf, Reiter + Standardsicht je Werkbank (v1.99.73, TS-194, keine Migration)"
  - "5 Tageshistorie eigene Seite + Knopf, alte Terminal-Seite an IDEAL abloesen (v1.99.74, TS-195, keine Migration)"
deploy:
  web: true
  service: false
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

> [!info] Herkunft
> Backlog [[2026-11-25-bde-masken-vereinheitlichung]] (Design-Gespraech Gerald 2026-11-25). Nimmt die ueberholte
> [[2026-11-18-bde-vereinfacht-mobil-kompakt-spec]] vollstaendig in Etappe 1 auf. Bezug: [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]]
> (Teileliste v1.99.32), [[2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-spec]] (Terminal v1.64.0),
> [[0030-teilmeldung-mit-fachlichem-schluessel-neben-workoperation]], [[0034-navigation-menue-und-kacheln-aus-einer-quelle]].
> Code-Belege: Buendel-Worktree `2026-08-07-ideal-teile-1-5`, Stand v1.99.69 (`IdealAkeWms/AppVersion.cs:5`).
> Die drei Screenshots im Backlog-Text sind keine `anhaenge:` und wurden nicht ausgewertet; sie dienen dem Dev-Lauf nur als Vorbild
> (Kopf der Teileliste = Vorbild FA-Sicht).

## Ziel / Nutzen (das Warum)

An IDEAL arbeiten heute zwei BDE-Masken mit verschiedener Technik nebeneinander: die serverseitige Teileliste (Tabelle, Zahnrad,
Server-Spaltenfilter, Pagination) und das JS-gerenderte Terminal (Karten „1. Operator“ / „2. Arbeitsgang wählen“, eigene Listenlogik in
`wwwroot/js/bde-terminal.js`, 1035 Zeilen). Werker wechseln zwischen zwei Bedienkonzepten, Status sehen sie nur im Terminal, eine Teilmenge
können sie in der Teileliste nicht melden, und am Handy ist die Teileliste kaum bedienbar.

Ziel: **eine** serverseitige Seite mit den Reitern *Teile Einzeln · Teile Summiert · FA*, gleichem kompakten Kopf, gleicher AG-Status-Ampel
wie in der FA-Struktur, Teilfertigmeldung mit Restmenge und einer mobil bedienbaren Darstellung. Die FA-Sicht ersetzt an IDEAL die alte
Terminal-Seite (Bauweg A, ADR 0005).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope** (Etappen siehe unten):
- Teile Einzeln: Spalte AG-Status, Baugruppen-Kinder als eingerueckte Tabellenzeilen, Spaltenfilter trifft Kinder, „Teile (n) ▸“ neben dem Matchcode.
- BDE mobil kompakt (F1–F6 der ueberholten Mobil-Spec) fuer alle Reiter dieser Seite.
- Teile Summiert: Einzelauftraege aufklappbar, Summen-Haken, aggregierte Ampel je Kuerzel.
- Teilfertigmeldung (mehrere Meldungen je Zeile, nur die abschliessende ist eindeutig) inkl. Migration, ADR 0036, Ruecknahme, Waisen, Baugruppe als Teil-Buchung.
- FA-Sicht als Tabelle im gemeinsamen Kopf, laufende/pausierte Buchungen im Kopf, Anstempeln je Zeile, alle Terminal-Ablaeufe uebernommen.
- Werkbank-Feld `BdeTeileliste` wird Auswahl „BDE-Standardsicht: Teile / FA“ (gleiches Feld), zuletzt gewaehlte Sicht je Werker+Werkbank.
- Tageshistorie als eigene Seite, Knopf im Kopf und Menuepunkt; Ablosung der alten Terminal-Seite an IDEAL.

**Out-of-Scope**:
- AKE / OSEON und der globale NurFA-Modus (`BdeNurFaMeldung`): alte Terminal-Seite bleibt unveraendert (Rueckfragen 1, 2).
- FA-Struktur (`FaHierarchy`) zeigt Teilmeldungen nicht als Status (Rueckfrage 7).
- Neue Rollen oder Rechte: keine.
- Loeschen von `bde-terminal.js` / `Views/BdeTerminal/Index.cshtml`: bleibt fuer AKE/NurFA.
- Zusatzfunde (nur melden, nicht bauen): totes `#activityModal` (`Views/BdeTerminal/Index.cshtml:136`, Handler `bde-terminal.js:707-714`; das Modal wird
  nirgends geoeffnet, `#activitySelect` nie gefuellt); totes `#sessionExpiry` (`Views/BdeTerminal/Index.cshtml:30`, wird nirgends gesetzt –
  der Backlog-Punkt „Session-Ablauf“ hat also keine Logik zum Uebernehmen); `_Pagination.cshtml:75-76` Label ohne `for` (aus Mobil-Spec).

## Fachliche Anforderungen

Entscheidungen aus dem Backlog („Entscheidungen“ 1–5 und „Vorschlaege Claude (bestaetigt)“) gelten als gesetzt und sind hier eingearbeitet.

### Gemeinsam (alle Etappen)

- **G1** Eine Seite `BdeTeileliste` mit Reitern *Teile Einzeln · Teile Summiert · FA* (FA ab Etappe 4). Gleicher `_Kopf`, Zahnrad, Server-Spaltenfilter, Pagination.
- **G2** AG-Status immer **Farbe + Zeichen + Text** (Haken / Kreis / Dreieck / Sanduhr aus `FaAgStatusDarstellung`, `FaArbeitsgangStatus.cs:100`),
  Name und Status in `aria-label`/`title`, fokussierbar – genau wie `Views/FaHierarchy/_FaHierarchyNode.cshtml:97-115`. Nie Farbe allein.
- **G3** Status nur ueber `ArbeitsgangStatusRegel` (ein Ort, fallstricke „Arbeitsgang-Status nur über ArbeitsgangStatusRegel“).
- **G4** Kontrast WCAG AA (4,5:1 Text, 3:1 Bedienelemente), Touch-Ziele ≥ 44 px, Skill `frontend-design` in jeder Etappe mit View-/CSS-/JS-Aenderung **explizit aufrufen**.

### Etappe 1 – Teile Einzeln + BDE mobil kompakt

- **E1-1** Neue Spalte **AG-Status** (`data-col-key="ag-status"`): je Token der Spalte Arbeitsschritte ein Symbol mit Kuerzel.
  Statusquelle je Token (ODER-Verknuepfung, dann `ArbeitsgangStatusRegel.Ableiten`):
  - erledigt = WorkOperation des Tokens erledigt (Zuordnung wie `FaArbeitsgangStatus.Zuordnen`, `FaArbeitsgangStatus.cs:32-95`) **oder**
    nicht stornierte abschliessende Teilmeldung mit diesem Kuerzel und Schluessel;
  - laeuft = WorkOperation laeuft **oder** (ab Etappe 3) nicht stornierte Teilmeldung mit Kuerzel, aber ohne abschliessende („teilgemeldet“ = läuft);
  - Soll-Termin = `ArbeitsgangStatusRegel.Soll` wie in der FA-Struktur;
  - weder WorkOperation noch Teilmeldung moeglich (kein BDE-Arbeitsgang) = neutral ohne Zeichen, Text „kein BDE-Arbeitsgang“.
  Hintergrund: Erkennung V1 legt fuer Blatt-Tokens eines Teileliste-Kuerzels keinen Vater-AG an
  (`IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs:96`), deshalb ist die Teilmeldung dort die einzige Quelle.
- **E1-2** Baugruppen: Knopf **„Teile (n) ▸“** direkt neben dem Matchcode (heute in der Aktionsspalte, `Views/BdeTeileliste/Index.cshtml:128-140`).
  Kinder als **eingerueckte Tabellenzeilen** mit denselben Spalten inkl. AG-Status, nur Anzeige (kein Haken, kein Melden).
  Ersetzt die `ul`-Liste in `tr.collapse #kinder-@i` (`Index.cshtml:142-162`).
- **E1-3** Spaltenfilter trifft auch Kinder: eine Baugruppe bleibt, wenn sie selbst oder ein Kind passt; ist ein Kind der Treffer, ist die
  Baugruppe aufgeklappt und die Kinderzeile markiert (Rahmen/Balken **und** Text „Treffer“ visually-hidden + sichtbares Zeichen, nicht nur Farbe).
  Pagination zaehlt nur Elternzeilen; Kinder wandern mit.
- **E1-4** Mobil kompakt (Inhalt der Mobil-Spec F1–F6, Varianten-Empfehlung A = CSS-Kartenansicht):
  - F1 weniger Weissraum (`mb-3`→`mb-2`, Werker-Badge `bde.css:6-14`);
  - F2 Filterkarte unter der Schwelle eingeklappt, Knopf zeigt Kurzfassung (KW-Bereich, „gemeldete“, Anzahl Spaltenfilter);
  - F3 Filter-Bedienelemente 44 px statt `-lg`, Hinweistext (`_Kopf.cshtml:71-75`) in `<details>`;
  - F4 Kartenansicht unter der Schwelle (`data-label` je Zelle, Zahnrad wirkt weiter, Baugruppen-Kinder als eingerueckte Karten, AG-Status als eigene Kartenzeile);
  - F5 Touch-Ziele ≥ 44 px (Haken, Pagination statt `pagination-sm`);
  - F6 Kontrast.
  Schwelle, Filtergroesse, Spaltenfilter in Kartenansicht und Handy-Konto: Rueckfragen 6, 8, 9, 10.
  Der kompakte Kopf ist der Kopf aller Reiter, also auch der FA-Sicht (Etappe 4).

### Etappe 2 – Teile Summiert

- **E2-1** Summenzeile aufklappbar: darunter die Einzelauftraege (heutige Einzeln-Zeilen derselben Gruppe), **FE-Termin aufsteigend**, je mit eigenem Haken und AG-Status.
- **E2-2** Haken auf der Summenzeile = **volle Fertigmeldung aller offenen Auftraege darunter** (heute schon so: Wert = Liste `OffeneSchluessel`,
  `Views/BdeTeileliste/Summiert.cshtml`; Server meldet je Schluessel, `BdeTeilelisteService.cs:256-275`). Reicht die Menge nicht, waehlt der Werker
  in der Unterebene einzelne Auftraege; nicht gemeldete bleiben offen.
  Summen-Haken und Einzel-Haken derselben Gruppe schliessen sich aus (JS: Summen-Haken setzt alle Einzel-Haken, Einzel-Haken nimmt Summen-Haken zurueck;
  der Server entdoppelt ohnehin per `DistinctBy`).
- **E2-3** Aggregierte Ampel je Kuerzel auf der Summenzeile: schlechtester Stand gewinnt – `Ableiten(alle erledigt, irgendeine laeuft,
  fruehester Soll der nicht erledigten)`; erledigt nur, wenn alle erledigt sind; „teilgemeldet“ zaehlt als laeuft (Rueckfrage 11).

### Etappe 3 – Teilfertigmeldung

- **E3-1** Melden-Dialog mit zwei ausdruecklichen Knoepfen **„Teilfertig melden“** (Zeile bleibt offen) und **„Fertig melden“** (schliesst die Zeile) –
  unabhaengig von der Menge (wie Terminal `bde-terminal.js:868`). Gut/Ausschuss wie heute; Vorgabe Gut bei „Fertig“ = Restmenge
  (Soll − Summe Gut der nicht stornierten Meldungen, mindestens 0), bei „Teilfertig“ leer und Pflicht > 0.
- **E3-2** Zeile zeigt „gemeldet X von Soll“ (Summe Gut der nicht stornierten Meldungen). Meldbar = keine abschliessende Meldung, eindeutig, nicht verschwunden.
- **E3-3** „Auswahl fertigmelden“ (Block, `Index.cshtml:12-22`) und Summen-Haken melden **abschliessend** mit Restmenge.
- **E3-4** Filter „gemeldete zeigen“ aus = nur abgeschlossene Zeilen ausblenden (heute jede Zeile mit Meldung, `BdeTeilelisteService.cs:187`).
- **E3-5** Ruecknahme je Meldung (eigene, bis `RuecknahmeFristAsync`, `BdeTeilelisteService.cs:398-399`); die Statuszelle listet die eigenen Meldungen
  der Zeile einzeln mit „Zurücknehmen“. Ruecknahme der abschliessenden Meldung oeffnet die Zeile wieder. Admin-Storno unveraendert mit Grund.
- **E3-6** Baugruppe: „Teilfertig“ erzeugt eine **Teil-Buchung** am Sub-FA-Arbeitsgang (Menge mit `IsFinal = false`), „Fertig“ wie heute die
  Fertig-Buchung (`IsFinal = true`). Ruecknahme storniert die verknuepfte Buchung wie heute.
- **E3-7** Waisen-Hinweis zaehlt **Schluessel**, nicht Meldungen.
- **E3-8** Admin-Liste Teilmeldungen (`BdeBookingsController.cs:267-309`) bekommt die Spalte **Art** (Teil / Fertig), filterbar.

### Etappe 4 – FA-Sicht

- **E4-1** Reiter **FA**: eine Zeile = **offener Arbeitsgang der Kuerzel-Familie dieser Werkbank** (Quelle und Reihenfolge wie heute
  `available-operations`, `BdeApiController.cs:204-327`, Sortierung `BdeTerminalAuftraege.Ordnen`). Fremd laufende Zeilen bleiben sichtbar mit
  Status „läuft“ (fallstricke „Terminal zeigt fremd gebuchte Arbeitsgänge“); eigene laufende Zeilen zeigen „läuft bei Ihnen“ ohne Start-Knopf.
- **E4-2** Spalten (Zahnrad, `defaultHidden` wo genannt): Haupt-FA, Sub-FA, Arbeitsgang (Kuerzel + Name), **AG-Status** (Rueckfrage 4: alle AG der
  Sub-FA, eigener markiert), Bezeichnung, Artikelnummer, Matchcode, Sollmenge, FE-Termin, Soll-Termin AG, Prio, Kunde, Montage-Abteilung;
  defaultHidden: Material, Arbeitsschritte, Arbeitsbereich, Bemerkung PN, Fertigungsinfo PN, Sage-Status, Vater-FA, Position.
- **E4-3** Aktion je Zeile: **Anstempeln** = „Rüsten“ / „Produktion“ (heutige Start-Knoepfe `bde-terminal.js:355-379`); bei
  `BdeNurAgFertigmeldung` stattdessen „Fertig melden“ (Mengen-Dialog, Vorgabe Sollmenge).
- **E4-4** Kopf: Karten „1. Operator“ / „2. Arbeitsgang wählen“ entfallen. Werker-Leiste wie Teileliste; darin **laufende und pausierte Buchungen
  des Werkers als kompakte Zeilen** mit Teilfertig / Pause / Fertig bzw. Fortsetzen. Zeit-/Detailangaben (Startzeit, Laufzeit, Personal, Terminal)
  per Schalter ein-/ausblendbar, gemerkt (localStorage je Benutzer, rein darstellend).
- **E4-5** Scanfeld FA-Nr als schmale Zeile ueber der Tabelle (`data-hw-scan-ziel`, Hardware-Scanner), Aufloesung ueber `/api/bde/resolve-scan`.
- **E4-6** Alle Terminal-Ablaeufe aus der Ablaufliste unten bleiben erhalten (Kollision, Gruppen-Fertigmeldung, Close-Others, ungeplante Taetigkeit, Mengen-Dialog, Doppelklick-Sperre).
- **E4-7** Standardsicht je Werkbank: Feld `BdeTeileliste` wird Auswahl „BDE-Standardsicht: Teile / FA“. Werkbank ohne Kuerzel → nur FA-Sicht,
  Teile-Reiter deaktiviert (`aria-disabled`, Hinweis „Diese Werkbank hat kein Arbeitsschritt-Kürzel – nur FA-Sicht“). Zuletzt gewaehlte Sicht je
  Werker+Werkbank gemerkt (vorhandener `GespeicherterFilter.Sicht`, `BdeTeilelisteController.cs:44,58`), ohne gespeicherten Wert die Standardsicht.

### Etappe 5 – Tageshistorie, Ablosung

- **E5-1** Tageshistorie als eigene Seite (Buchungen des Werkers heute; Rueckfrage 5: plus Teilmeldungen des Tages), Knopf im Kopf, Menuepunkt in
  Kategorie Produktion/BDE (ADR 0034).
- **E5-2** `BdeTerminal/Index` leitet an IDEAL (Master an, NurFA aus) auf die neue Seite mit der Standardsicht der Werkbank um; fuer AKE/NurFA bleibt die alte Seite (Rueckfrage 1).

## Technischer Loesungsentwurf

### Belege Ist-Stand (Auszug, weitere im Text)

| Aussage | Beleg |
|---|---|
| Genau eine nicht stornierte Meldung je Schluessel+Kuerzel | `Data/ApplicationDbContext.cs:1592-1595` (`UX_BdeTeilMeldungen_Schluessel`, Filter `[IsCancelled] = 0`) |
| Jede Meldung schliesst die Zeile | `Services/BdeTeilelisteService.cs:47` (`Meldbar => Meldung == null && …`) |
| Eine Meldung je Schluessel im Aufbau | `BdeTeilelisteService.cs:161` (`GroupBy(...).First()`) |
| Meldungen nur des eigenen Kuerzels geladen | `BdeTeilelisteService.cs:133-134` |
| Spaltenfilter nur auf Elternzeilen | `BdeTeilelisteService.cs:189-190` |
| Baugruppe meldet sofort fertig | `BdeTeilelisteService.cs:374-381` → `BdeBookingService.ReportCompletionCoreAsync` erzeugt Menge mit `IsFinal = true` (`BdeBookingService.cs:330-331`) |
| Gate „meldet ohne Zeit“ nutzt TeilelisteWirksam | `BdeBookingService.cs:288` |
| TeilelisteWirksam = BdeAktiv && BdeTeileliste && !NurFa && Kuerzel | `Services/WerkbankKuerzelFamilie.cs:52-53` |
| Teileliste-Redirect aus dem Terminal | `Controllers/BdeTerminalController.cs:65-66` |
| Gespeicherter Filter kennt die Sicht schon | `BdeTeilelisteController.cs:44,58,124` |
| Terminal-Pflicht der Seite | `BdeTeilelisteController.cs:87-91` |
| Erkennung V1 an `BdeAktiv && BdeTeileliste` (ohne NurFA) | `IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs:92-96` |
| FA-Struktur liest Teilmeldungen nicht | `FaArbeitsgangStatus.Zuordnen` (`FaArbeitsgangStatus.cs:32-95`) arbeitet nur auf WorkOperations + `FaBdeStatus` |
| erledigt = IsFinal-Menge, laeuft = `EndedAt` null | `Data/Repositories/IWorkOperationRepository.cs` (`FaBdeStatus`) |
| Ablauf Status-Ladung in der FA-Struktur | `Controllers/FaHierarchyController.cs:118-140` |
| Symbol-Markup der FA-Struktur | `Views/FaHierarchy/_FaHierarchyNode.cshtml:97-115` |
| Client-Sortierung verschiebt alle Zeilen je `<tbody>` | `wwwroot/js/table-filter.js:408-415`, danach Ereignis `table-sorted` `:419-421` |
| CSV-Export nimmt jede Zeile ohne `td[colspan]` | `wwwroot/js/csv-export.js:105-113` |

### Etappe 1

- **Status-Ladung an einem Ort:** den Lade-Block aus `FaHierarchyController.cs:118-140` (BdeAktiv-Gate, `GetForFaStatusAsync`, `BdeRelevanteIds`,
  `GetBdeStatusAsync`, Feiertage, `GetDueDateOffsetsAsync`) in eine Methode herausziehen, die FA-Struktur und Teileliste aufrufen – keine Kopie.
  `FaArbeitsgangStatus.Zuordnen` bekommt eine Variante je Knoten (oder wird je Knoten aufgerufen) und eine optionale zweite Quelle
  „Teilmeldungen je (Schluessel, Kuerzel)“ (erledigt/laeuft), die nur die Teileliste uebergibt. Die FA-Struktur bleibt dadurch unveraendert (Rueckfrage 7).
- `LadenAsync` laedt fuer die Statusquelle die Teilmeldungen **aller** Kuerzel der geladenen Haupt-FAs (heute nur das eigene, `:133-134`);
  Melde-Logik (Meldbar, Ruecknahme) bleibt beim eigenen Kuerzel.
- **Symbol-Partial:** das Markup `_FaHierarchyNode.cshtml:107-113` in ein Partial `_FaAgSymbole` herausziehen, von FA-Struktur, Teileliste und FA-Sicht genutzt.
- **Kinder als Zeilen:** `TeilelisteZeile.Kinder` werden selbst `TeilelisteZeile` (mit Status), damit dieselben Getter `SpaltenEinzeln`
  (`BdeTeilelisteService.cs:98-115`) fuer Filter und Zellen gelten. `Bauen` (`:152-195`): Eltern-Treffer **oder** Kind-Treffer behalten; Flag
  `Treffer` je Kind, `Aufgeklappt` je Eltern. Pagination auf Elternzeilen.
- Markup: Kinderzeilen im selben `<tbody>` (eine filterbare Tabelle je Seite, fallstricke), `tr.bde-tl-kind collapse` mit `data-eltern`, alle Zellen mit
  `data-col-key` (Zahnrad blendet per Spaltenindex aus). **Sortierung:** `bde-teileliste.js` haengt sich an `table-sorted` (`table-filter.js:419`) und
  setzt jede Kinderzeile wieder hinter ihre Elternzeile. CSV: Kinder erscheinen als eigene Zeilen; die Spalte Vater-FA ordnet sie zu (in der
  Testbeschreibung nennen, kein Sonderweg).
- **Mobil:** Scope-Klasse `.bde-tl-seite` am Wrapper (bde.css wird auch von Terminal/Cockpit geladen, Mobil-Spec B1). Befunde B1–B14 der Mobil-Spec gelten
  unveraendert als Bauhinweise, insbesondere: `site.css:248-257` `max-content/nowrap !important` (B3), Inline-`display:none` aus
  `column-preferences.js:160-180` (B6), `tr.collapse` vs. `display:block` (B7), Filterzeile in `thead` (`table-filter.js:150-156`, B8),
  Hardware-Scanner (B9), Waechter `ResponsiveCssTests.cs:126-143` (B13) und `TeilelisteKuerzelKonstantenWaechterTests.cs:14-27` (B14).
- Register: `ColumnDefinitions` (`ColumnDefinitions.cs:457-458,506,919`) und `ColumnFilterSyntax` (`ColumnFilterSyntax.cs:47-48`) um `ag-status` (Text) ergaenzen
  (Drift-Waechter `ViewKeyDriftGuardTests`, `ColumnFilterTypesDriftTests`).

### Etappe 2

- `Summieren` (`BdeTeilelisteService.cs:199-225`) liefert je Gruppe zusaetzlich die Einzelzeilen (sortiert FE-Termin aufsteigend, dann Haupt-FA) und
  den aggregierten Status je Kuerzel ueber `ArbeitsgangStatusRegel.Ableiten`. Darstellung der Unterzeilen wie Etappe 1 (`tr` im selben `<tbody>`,
  `table-sorted`-Nachsortierung). Pagination auf Summenzeilen. Einzel-Haken sendet den einzelnen Schluessel an das bestehende `Melden`.

### Etappe 3 – Datenmodell (ADR 0036)

- `BdeTeilMeldung` + Spalte **`IstAbschliessend bit NOT NULL DEFAULT 1`** (Bestand = abschliessend, denn heute schliesst jede Meldung die Zeile, `:47`).
  Name deutsch wie die Nachbarfelder der Entitaet (`Kuerzel`, `GemeldetAm`, `SollmengeBeiMeldung`).
- UNIQUE neu: `UX_BdeTeilMeldungen_Schluessel` mit Filter `[IsCancelled] = 0 AND [IstAbschliessend] = 1`. Bestand kann nicht kollidieren: der neue
  Filter ist eine Teilmenge des alten, der alte war eindeutig (`ApplicationDbContext.cs:1592-1595`) – keine Pruef-/Bereinigungsstufe noetig.
- Service:
  - `TeilelisteZeile.Meldung` → `Meldungen` (Liste);
  - `Gemeldet` = Summe Gut;
  - `Meldbar => !Meldungen.Any(m => m.IstAbschliessend) && Eindeutig && !Verschwunden`;
  - `Bauen` gruppiert statt `.First()` (`:161`);
  - Waisen-Zaehlung nach Schluessel (`:166`).
- `MeldeAuftrag` + `bool Abschliessend`. In `MeldeZeileAsync` (`:277-392`):
  - `VorhandeneMeldungAsync` (`:445-449`) prueft nur noch abschliessende Meldungen (→ `BereitsGemeldet`);
  - Applock (`Sperre`, `:455-457`) bleibt;
  - 2601/2627-Abfang (`:358`) bleibt fuer den abschliessenden Fall.
  - Vorgabe Gut = Restmenge statt Sollmenge (`:327`).
- **Baugruppe:** `ReportCompletionCoreAsync` bekommt den Parameter `bool istFinal = true` (Default haelt alle bisherigen Aufrufer unveraendert);
  `MeldeZeileAsync` uebergibt `Abschliessend`. Die Pruefung „bereits fertig“ (`BdeBookingService.cs:301-303`) bleibt und greift nur bei `IsFinal`.
  Eine Teil-Buchung (Start = Ende, `IsFinal = false`) macht den AG im FA-Status weder erledigt noch laufend; der Teileliste-Status zeigt „läuft“
  ueber die Teilmeldung (E1-1).
- Controller `Melden` (`BdeTeilelisteController.cs:171-206`): Formularfeld `art` = `teil|fertig` (Server prueft, ADR 0026: Client sendet Absicht).
  `TryMenge` (`:255-263`) kulturunabhaengig wie bisher.
- Admin-Liste: `TeilmeldungSpalten` (`BdeBookingsController.cs:294-309`) + `["art"]`, View-Spalte `data-col-key="art"`, Register `ColumnFilterTypes`.
- **ADR 0036** „Teilfertigmeldung: mehrere Meldungen je Schluessel, eindeutig nur die abschliessende“ (supersedes ADR 0030 **in diesem Punkt**;
  0030 bekommt `superseded_by` nur fuer den Eindeutigkeits-Punkt als Hinweis, Text bleibt). Wird im Dev-Lauf im Hauptcheckout angelegt.

### Etappe 4 – FA-Sicht

- **Wirksamkeit neu** in `WerkbankKuerzelFamilie` (ersetzt `TeilelisteWirksam`):
  - `SeiteVerfuegbar(w, nurFa, hierarchisch) = w.BdeAktiv && !nurFa && hierarchisch`;
  - `TeileSichtVerfuegbar(...) = SeiteVerfuegbar && Code(w) != null`;
  - `TeileStandard(...) = TeileSichtVerfuegbar && w.BdeTeileliste`.
  - Master-Gate `hierarchisch`: Rueckfrage 13. Pruefabfrage je DB:
    `SELECT COUNT(*) FROM ProductionWorkplaces WHERE ISNULL(ArbeitsschrittCode,'') <> '';`
    Die Aussage in `WerkbankKuerzelFamilie.cs:10` „AKE hat keine Kuerzel“ ist **unbelegt**.
- Aufrufer umstellen:
  - `BdeBookingService.cs:288` → `TeileSichtVerfuegbar`. Sonst wird die Baugruppen-Buchung aus dem Teile-Reiter einer Werkbank mit Standard FA abgewiesen.
  - `BdeTeilelisteService.cs:261` → `TeileSichtVerfuegbar`.
  - `BdeTeilelisteController.cs:75` → `SeiteVerfuegbar`; der Teile-Reiter zusaetzlich `TeileSichtVerfuegbar`.
  - `BdeTerminalController.cs:65` → `TeileStandard` (bis Etappe 5).
  - `ProductionWorkplacesController.cs:54` (Listentext) und `Views/ProductionWorkplaces/Index.cshtml:69-72` (Badge „Standardsicht Teile“).
- **Erkennung V1** (`WorkOperationStructureDetectionService.cs:96`) bleibt am Feld (Rueckfrage 3). Damit kein Dienst-Code und keine Service-Neuinstallation ausser Versionsnummer.
  Teile-Reiter an Werkbank mit Standard FA: Hinweis-Banner „Teilmeldungen schließen hier keinen Arbeitsgang in der FA-Sicht“.
- **Zeilenquelle:** die Logik aus `BdeApiController.cs:204-327` (nicht-NurFA-Zweig) in eine Service-Methode ziehen, die API und FA-Sicht
  gemeinsam nutzen (eine Stelle). FA-Sicht serverseitig: Spalten-Getter-Dictionary, `ColumnFilterHelper.Apply` vor Pagination, Datumsspalten
  nach der Termin-Berechnung filtern (ADR 0005). Neue View `Views/BdeTeileliste/Fa.cshtml`, viewKey `BdeTeilelisteFa`, Register-Schluessel
  `BdeTeileliste/Fa` in `ColumnDefinitions` und `ColumnFilterSyntax`.
- **Kopf-Buchungen** serverseitig aus denselben Repository-Methoden wie `ActiveBookings`/`PausedBookings` (`BdeTerminalController.cs:270,294`);
  Aktionen per kleinem JS (`bde-fa-sicht.js`) gegen die **bestehenden** JSON-Actions, danach `location.reload()`.
  Mengen-, Kollisions-, Gruppen- und Close-Others-Modal als Partials aus `Views/BdeTerminal/Index.cshtml:114-200` herausziehen und auf beiden Seiten nutzen
  (keine Kopie). `wmsSperren`/`wmsFreigabeQtyOk` (`site.js`) fuer die Doppelklick-Sperre wiederverwenden.
- **Werkbank-Maske:** `_BdeTeilelisteField.cshtml` Schalter → `<select>` „BDE-Standardsicht“ (Teile = true, FA = false); Server-Validierung
  `ValidateTeilelisteAsync` (`ProductionWorkplacesController.cs:259-268`) gilt fuer „Teile“ unveraendert. Anzeige weiter nur bei
  `ViewBag.Hierarchisch`. `[Display(Name=...)]` in `ProductionWorkplace.cs:57` → „BDE-Standardsicht“.

#### Ablaufliste Terminal → FA-Sicht (nichts darf verloren gehen)

| # | Ablauf heute | Beleg | Endpunkt | FA-Sicht |
|---|---|---|---|---|
| 1 | Werker per Personalnummer | `bde-terminal.js:99-140` | GET `/api/bde/operator/{nr}` | vorhandene Anmeldung `Anmelden.cshtml` (Server) |
| 2 | Werker wechseln / Reset | `:142-162` | – | „Werker wechseln“ im Kopf (`_Kopf.cshtml:21-25`) |
| 3 | FA/AG-Scan, Mehrdeutigkeit, Gruppe oeffnen, Sub-FA-Fallback | `:165-231`, `:262-330` | GET `/api/bde/resolve-scan` | Scanzeile; Exact → Zeile markieren + Anstempeln anbieten; Ambiguous → Auswahldialog; NotInScope/NotFound → Meldung |
| 4 | Scan bucht FA direkt (ProductionOrder) | `:237-258` | POST `BdeTerminal/StartProductionForOrder` | nur NurFA → entfaellt hier (Rueckfrage 2) |
| 5 | Zustand des Werkers | `:339-353` | GET `operator/{id}/active-booking` | serverseitig im Kopf |
| 6 | Aktionsknoepfe je Zustand (Ruesten laeuft: Pause/Ruesten fertig/Produktion; Produktion: Pause/Teilfertig/Fertig; Taetigkeit: beenden) | `:355-379` | `Pause`, `Finish`, `StartProduction`, `ReportPartial` | Kopfzeile je Buchung |
| 7 | Start Ruesten/Produktion, NurAg-Fertigmeldung | `:355-379`, `:647-682` | `StartSetup`, `StartProduction`, `ReportCompletion` | Knopf je Tabellenzeile |
| 8 | Fertig mit Gruppenpflicht | `:387-421`, `:975-1031` | `Finish` → `GroupFinishRequired`, `FinishGroup` | Gruppen-Modal (Partial) |
| 9 | Fertig mit anderen offenen Buchungen am AG | `:387-421`, `:937-972` | `Finish` → `otherActiveBookings`, `CloseOthers` | Close-Others-Modal (Partial) |
| 10 | Kollision anderer Werker | `:428-448` | Ergebnis `CollisionOtherOperator` | Kollisions-Modal (Partial) |
| 11 | Mengenpflicht / InvalidState | `:428-481` | `QuantityRequired`, `InvalidState` | Mengen-Modal mit Soll-Hinweis, Meldung als Toast |
| 12 | Pausierte Buchungen + Fortsetzen | `:731-814` | GET `PausedBookings`, POST `Resume` (resumeAs=2) | Kopfzeile „pausiert“ mit Fortsetzen |
| 13 | Aktuelle Buchungen mit Teilfertig/Pause/Fertig, Bestaetigung bei Gut < Soll | `:817-934` | `ActiveBookings`, `ReportPartial`, `Pause`, `Finish` | Kopfzeilen (E4-4) |
| 14 | Ungeplante Taetigkeit starten | `:516-645` (Liste), `:647-682` | `available-operations` (Teil activities), POST `StartActivity` | Knopf „Tätigkeit“ im Kopf mit Auswahl |
| 15 | Werkbank wechseln | `:717-719` | – | `#teilelisteWerkbank` (`_Kopf.cshtml:6-19`) |
| 16 | Tageshistorie | `:484-513` | GET `operator/{id}/today-history` | Etappe 5 |
| 17 | Toasts, Laufzeit-Ticker | `:40-96` | – | Toast nach Reload ueber TempData; Laufzeit in Detailangaben (E4-4) |
| 18 | NurFA-Oberflaeche | `:722-728` | – | entfaellt (Rueckfrage 2) |
| 19 | Gruppen auf-/zuklappen, „Weitere“ (MaxHauptFa) | `:685-699` | – | Pagination (Rueckfrage 14) |
| 20 | Hidden Inputs NurFaMode/NurAgFertigmeldung/Hierarchisch/MaxHauptFa | `Views/BdeTerminal/Index.cshtml:201-210` | – | NurAg serverseitig je Zeile; Hierarchisch immer an; NurFa/MaxHauptFa entfallen |
| – | Activity-Modal, Session-Ablauf | `:707-714`, `Index.cshtml:30,136` | – | tot, nichts zu uebernehmen (Zusatzfund) |

### Etappe 5

- Action `BdeTeileliste/Tageshistorie(int? workplaceId, int? operatorId, page, pageSize)` mit denselben Gates (`[RequireBdeActive][RequireBdeUserAccess]`, Terminal-Pflicht).
  Ohne Werker → Anmelden. Quelle: `GetHistoryAsync` wie `BdeApiController.cs:147`, ohne die feste Kappung auf 50, mit Pagination +
  Server-Spaltenfilter (ADR 0005), plus Teilmeldungen des Tages (Rueckfrage 5) als Spalte „Art“ (Buchung / Teilmeldung).
- NavZiel `BdeTageshistorie` (Kategorie `KatProduktionBde`, `NavigationKatalog.cs:16`), Regel wie `BdeTerminal` (`bde && useBde`, `:271`), Link im Layout
  (ADR 0034, fallstricke §58, `NavigationSichtTests`).
- `BdeTerminalController.Index` (`:41-76`): `SeiteVerfuegbar` → Redirect auf `BdeTeileliste/Index` (Sicht = gespeicherte oder Standard); sonst alte Seite.

### Arbeitsweise

- Ein Worktree, je Etappe ein Commit, ein Merge am Ende. Branch waehrend der Arbeit mit `scripts/sync-worktree.ps1` auf main-Stand halten.
- Jede Etappe baut und testet gruen fuer sich und haengt nicht an einer offenen Rueckfrage (Empfehlungen als Vorgabe, abweichende Antwort = Nacharbeit vor der Etappe).

## Migrations-/SQL-Auswirkungen

- **Etappe 3 – einzige Schema-Aenderung:** `dotnet ef migrations add AddBdeTeilMeldungIstAbschliessend` → `SQL/168_AddBdeTeilMeldungIstAbschliessend.sql`
  nach Muster `SQL/154_AddBdeTeileliste.sql`:
  - NOEXEC-Guard;
  - Batch 1 `IF COL_LENGTH('dbo.BdeTeilMeldungen','IstAbschliessend') IS NULL ALTER TABLE … ADD … CONSTRAINT DF_BdeTeilMeldungen_IstAbschliessend DEFAULT (1)`;
  - Batch 2 Index nur neu bauen, wenn der Filter noch nicht `IstAbschliessend` enthaelt (`sys.indexes.filter_definition`), `DROP INDEX` + `CREATE UNIQUE … WHERE [IsCancelled] = 0 AND [IstAbschliessend] = 1`;
  - Batch 3 `__EFMigrationsHistory`-Insert nur, wenn Spalte und Index-Filter stimmen.
  - `SQL/00_FreshInstall.sql` an zwei Stellen: Tabellendefinition/Index (`:2662`, `:3516-3518`) und MigrationId.
  - Nicht daten-destruktiv.
  - `SQL/AgentJobs/` nicht betroffen (kein Pflichtfeld ohne Default).
- **SQL-Nummer:** naechste freie 168 (164/165 reserviert von [[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]], letzte belegte 167). Der Dev-Lauf prueft neu.
- **Konflikt:** SQL/164 der Isolierungsfraese-Spec baut denselben Index mit Filter `[IsCancelled]=0 AND [WarehouseRequisitionItemId] IS NULL` neu.
  Wer als zweiter baut, setzt den vereinigten Filter `[IsCancelled]=0 AND [IstAbschliessend]=1 AND [WarehouseRequisitionItemId] IS NULL` und
  prueft im Index-Guard auf beide Teile (Rueckfrage 12).
- Etappen 1, 2, 4, 5: keine Migration. Etappe 4 aendert nur die Bedeutung von `ProductionWorkplaces.BdeTeileliste` (true = Standardsicht Teile) –
  Bestandswerte bleiben gueltig, keine Datenumsetzung.
- Tests: EF InMemory erzwingt weder gefilterten UNIQUE noch `sp_getapplock` → doppelte abschliessende Meldung im Manual-UAT gegen SQL Server pruefen (fallstricke „Tests, EF, SQL Server“).

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `ProductionWorkplace.BdeTeileliste` (Werkbank-Maske, Anzeige „BDE-Standardsicht“) – **geaendert** ab Etappe 4 | Auswahl (gespeichert als bit im bestehenden Feld) | `Teile` (1) · `FA` (0); Server prueft beim Speichern: „Teile“ nur mit Master an, BDE aktiv, Kuerzel gesetzt (`ValidateTeilelisteAsync`) | `FA` (0) | Welcher Reiter beim Aufruf ohne gespeicherte Werker-Sicht oeffnet. Teile: Teileliste nach Matchcode, Fertigmeldung ohne Zeit. FA: Arbeitsgaenge der Werkbank mit Anstempeln. Beide Reiter bleiben umschaltbar, Teile nur mit Kuerzel. **Zusaetzlich (unveraendert):** „Teile“ laesst die Struktur-Erkennung fuer Blatt-Tokens dieses Kuerzels keinen Vater-AG anlegen (Rueckfrage 3). | BDE-Seite `BdeTeileliste` (Reiter-Vorgabe), Terminal-Redirect, Dienst-Lauf Arbeitsgang-Strukturerkennung | IDEAL (nur bei Master `ProduktionsauftragHierarchisch` sichtbar) | „Teile“ aendert die Arbeitsgang-Erkennung: Vater-Arbeitsgaenge dieses Kuerzels werden beim naechsten Dienst-Lauf nicht mehr angelegt und erscheinen nicht in der FA-Sicht. |
| `BdeTerminalMaxHauptFa` (AppSetting) – **Wirkung eingeschraenkt** ab Etappe 4 | Zahl | unveraendert | 5 | Nur noch alte Terminal-Seite (AKE/NurFA); die neue FA-Sicht nutzt Pagination. Beschreibungstext im Katalog anpassen. | `BdeTerminalController.cs:72` | beide | – |

Sonst keine neuen Einstellungen. Die Detailangaben-Umschaltung im Kopf (E4-4) ist eine Bedienvorliebe im Browser, keine Einstellung.

## Audit-Feld-Auswirkungen

- `BdeTeilMeldung` (AuditableEntity): Anlage setzt `Created*` wie heute (`BdeTeilelisteService.cs:346-352`), Ruecknahme/Storno `SetModified`
  (`:406-436`) – gilt je Meldung, auch fuer Teilmeldungen. Neue Spalte ohne eigene Audit-Wirkung.
- `ProductionWorkplace`: Feld unveraendert, Aenderung ueber die bestehende Feldliste (`ProductionWorkplaceRepository.cs:17-18`) mit `Modified*`.
- `UserViewPreference` (gemerkte Sicht/Filter): bestehendes `SaveAsync` mit Anzeige- und Windows-Name (`BdeTeilelisteController.cs:126`).
- Buchungen (`BdeBooking`, Mengen) nur ueber `IBdeBookingService` – dessen Audit unveraendert.

**Rollen / Zugriff:** unveraendert. Seite `[RequireBdeActive][RequireBdeUserAccess]` (`BdeTeilelisteController.cs:17-18`), Terminal-JSON-Actions gleiche
Filter, `api/bde` `[RequireBdeUserAccess]`, Admin-Storno `[RequireBdeAdminAccess]` (`BdeBookingsController.cs:312`). Neuer Menuepunkt mit
derselben Regel wie „Terminal“ (Filter-Kongruenz in `NavigationSichtTests`). Keine neue Rolle → keine Aenderung an `controller.md`/`RoleOverview`.

**Listen-View-Pattern:** jede neue/geaenderte Tabelle (Teile Einzeln, Summiert, FA, Tageshistorie, Admin-Teilmeldungen):
- `PageSize.Resolve` + `PaginationState` + `_Pagination`;
- Filterkarte;
- Server-Spaltenfilter (`data-server-column-filter="true"`, jedes `<th>` mit `data-col-key`);
- Register `ColumnDefinitions`/`ColumnFilterTypes`;
- eine filterbare Tabelle je Seite;
- `defaultHidden` fuer Zusatzspalten.

## Akzeptanzkriterien

**Etappe 1**
1. Teile Einzeln zeigt die Spalte AG-Status mit je einem Symbol pro Arbeitsschritt-Token; jedes Symbol hat Zeichen + Kuerzel + `aria-label` mit Name und Status.
2. Ein Token mit abschliessender Teilmeldung zeigt „erledigt“ (Haken), auch ohne WorkOperation.
3. „Teile (n) ▸“ steht in der Matchcode-Zelle; Aufklappen zeigt Kinder als eingerueckte Zeilen mit allen sichtbaren Spalten inkl. AG-Status, ohne Haken und ohne Melden-Knopf.
4. Spaltenfilter auf einen Matchcode, der nur ein Kind trifft: die Baugruppe erscheint aufgeklappt, die Kinderzeile ist mit Zeichen + Text „Treffer“ markiert.
5. Klick auf einen Spaltenkopf sortiert; jede Kinderzeile steht danach direkt unter ihrer Baugruppe.
6. Zahnrad blendet eine Spalte in Eltern- und Kinderzeilen gleichzeitig aus.
7. Unter der Schwelle (Rueckfrage 8): Filterkarte eingeklappt mit Kurzfassung, Kartenansicht, kein waagrechtes Scrollen bei 360 px Breite.
8. Alle Bedienelemente der Seite ≥ 44 px, Kontrast AA (Messung im Lauf festhalten).
9. Terminal-Seite und Cockpit sehen unveraendert aus (Scope `.bde-tl-seite`).
10. FA-Struktur zeigt dieselben Symbole wie vorher (Partial-Extraktion ohne Regression).

**Etappe 2**
11. Summenzeile aufklappbar; Einzelauftraege darunter nach FE-Termin aufsteigend, je mit Haken und AG-Status.
12. Summen-Haken setzt alle Einzel-Haken der Gruppe; Abwaehlen eines Einzel-Hakens nimmt den Summen-Haken zurueck; der Zaehler stimmt.
13. Fertigmelden mit Summen-Haken meldet alle offenen Auftraege der Gruppe; mit zwei Einzel-Haken nur diese zwei, die uebrigen bleiben offen.
14. Aggregierte Ampel: ein Auftrag ueberfaellig, einer erledigt → Summenzeile zeigt ueberfaellig; alle erledigt → erledigt.

**Etappe 3**
15. „Teilfertig melden“ mit 3 von 10 laesst die Zeile offen und zeigt „gemeldet 3 von 10“; zweite Teilmeldung 4 → „gemeldet 7 von 10“.
16. „Fertig melden“ schlaegt Gut = 3 (Rest) vor und schliesst die Zeile; ein zweites „Fertig“ auf denselben Schluessel wird als „bereits gemeldet“ abgewiesen (auch parallel – Manual-UAT gegen SQL Server).
17. „Fertig melden“ mit weniger als Rest schliesst trotzdem.
18. Ruecknahme der abschliessenden Meldung oeffnet die Zeile wieder; Ruecknahme einer Teilmeldung senkt „gemeldet X“.
19. Baugruppe „Teilfertig“: am Sub-FA-AG entsteht eine Buchung mit Menge `IsFinal = false`; der AG ist danach in der FA-Struktur nicht erledigt.
    Baugruppe „Fertig“: Menge `IsFinal = true`, AG erledigt.
20. „gemeldete zeigen“ aus blendet nur abgeschlossene Zeilen aus.
21. Admin-Liste Teilmeldungen zeigt und filtert die Spalte Art.
22. SQL/168 laeuft zweimal ohne Fehler; FreshInstall erzeugt Spalte und Index identisch.

**Etappe 4**
23. Reiter FA zeigt die offenen Arbeitsgaenge der Kuerzel-Familie in derselben Reihenfolge wie `available-operations`; fremd laufende mit „läuft“, eigene laufende ohne Start-Knopf.
24. Anstempeln je Zeile startet Ruesten bzw. Produktion; bei `BdeNurAgFertigmeldung` steht dort „Fertig melden“.
25. Laufende und pausierte Buchungen erscheinen im Kopf; Teilfertig/Pause/Fertig/Fortsetzen wirken und laden neu.
26. Kollisions-, Gruppen-Fertig- und Close-Others-Fall fuehren zum jeweiligen Dialog wie im Terminal (je ein Testfall).
27. Hardware-Scan einer FA-Nr markiert die Zeile; mehrdeutiger Scan oeffnet die Auswahl; unbekannte Nr gibt eine Meldung.
28. Werkbank-Maske zeigt „BDE-Standardsicht“ als Auswahl; „Teile“ ohne Kuerzel wird beim Speichern abgewiesen.
29. Ohne gespeicherte Sicht oeffnet die Standardsicht; nach Reiterwechsel oeffnet beim naechsten Aufruf die zuletzt gewaehlte.
30. Werkbank ohne Kuerzel: Teile-Reiter deaktiviert mit Hinweistext.
31. Baugruppen-Teilmeldung aus dem Teile-Reiter einer Werkbank mit Standard FA wird gebucht (Gate `TeileSichtVerfuegbar`).
32. Werkbank mit Standard FA, Teile-Reiter: Hinweis-Banner zur Erkennung sichtbar.

**Etappe 5**
33. Menuepunkt „Tageshistorie“ sichtbar genau wenn „Terminal“ sichtbar ist; Knopf im Kopf oeffnet sie fuer den angemeldeten Werker.
34. Tageshistorie listet Buchungen und Teilmeldungen des Tages, paginiert, Spaltenfilter wirkt serverseitig.
35. `BdeTerminal/Index` an IDEAL leitet auf die neue Seite um; an AKE (Master aus) bzw. mit NurFA erscheint die alte Seite unveraendert.

**Alle**
36. Build + alle Tests gruen; Drift-Waechter (`ViewKeyDriftGuardTests`, `ColumnFilterTypesDriftTests`, `NavigationSichtTests`, `ResponsiveCssTests`, `TeilelisteKuerzelKonstantenWaechterTests`, `NachlaufManifestDriftTests`) gruen.

## Test-Szenarien

Nummern vorlaeufig (letztes TS-190, `docs/TESTSZENARIEN.md:13864`; der Dev-Lauf prueft neu). Bestehendes TS-152 (Teileliste) bleibt und bekommt Verweise.
Die geplante TS-165 der Mobil-Spec entfaellt.

- **TS-191 Teileliste AG-Status, Baugruppen-Kinder, mobil** – Vorbedingung IDEAL-DEV, Werkbank mit Kuerzel, Standard Teile, Baugruppe mit ≥ 2 Kindern,
  ein AG ueberfaellig. Schritte: Spalte pruefen (Symbole, Screenreader-Text), aufklappen, Filter auf Kind-Matchcode, sortieren, Zahnrad, Handy-Breite 360 px
  (Filter eingeklappt, Karten, 44 px). Negativ: Filter ohne Treffer → Leerzustand; Terminal-Seite optisch unveraendert.
- **TS-192 Summiert aufklappen, Summen-Haken, Ampel** – Gruppe mit 3 Auftraegen verschiedener FE-Termine. Schritte: Reihenfolge, Summen-Haken,
  Teilauswahl, Fertigmelden. Negativ: Summen-Haken + einzelnes Abwaehlen; bereits gemeldeter Auftrag erscheint nicht als waehlbar.
- **TS-193 Teilfertigmeldung** – Teilfertig 3/10, 4/10, Fertig mit Rest, Ruecknahmen, Baugruppe Teil/Fertig (Buchung in BDE-Buchungen pruefen),
  Admin-Liste Art. Negativ: Teilfertig ohne Menge; zweites Fertig; paralleles Fertig von zwei Geraeten (nur eines gewinnt); Ruecknahme nach Frist.
- **TS-194 FA-Sicht** – Anstempeln, Kopf-Buchungen, Pause/Fortsetzen, Teilfertig, Fertig mit Gruppenpflicht, Close-Others, Kollision, Taetigkeit,
  Scan (exakt/mehrdeutig/unbekannt), Standardsicht in der Werkbank-Maske, gemerkte Sicht, Werkbank ohne Kuerzel. Negativ: „Teile“ ohne Kuerzel speichern.
- **TS-195 Tageshistorie und Ablosung** – Menue, Knopf, Inhalt, Pagination; Aufruf `BdeTerminal/Index` an IDEAL (Redirect) und an AKE-DEV
  falls verfuegbar (alte Seite). Negativ: Benutzer ohne Terminal → Hinweis wie heute.

`secondbrain/tests/testszenarien-index.md` je Etappe nachziehen.

## Etappen (nur bei epic: true)

Ein Worktree (`.claude/worktrees/2026-08-07-ideal-teile-1-5`), je Etappe ein Commit, **ein Merge am Ende**. Branch regelmaessig mit
`scripts/sync-worktree.ps1` auf main-Stand halten. Versionen und TS-Nummern vorlaeufig (letzte Version 1.99.69), der Dev-Lauf prueft neu.
Jede Etappe: `frontend-design` explizit aufrufen, Kontrast messen, Ampel nie nur Farbe, Anwender-Changelog + Hilfe, Testszenario.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | Teile Einzeln: AG-Status (Status-Ladung und Symbol-Partial aus FA-Struktur herausgezogen), Kinder als Zeilen, Filter auf Kinder, Teile-Knopf vorn, BDE mobil kompakt. v1.99.70, TS-191, keine Migration. Vorgabe aus Empfehlungen zu Rueckfragen 6, 8–10. | offen | |
| 2 | Teile Summiert: Einzelauftraege aufklappbar, Summen-Haken, aggregierte Ampel. v1.99.71, TS-192, keine Migration. Vorgabe Rueckfrage 11. | offen | |
| 3 | Teilfertigmeldung: Migration + SQL/168 + FreshInstall, Service/Controller/Dialog, Ruecknahme, Waisen, Baugruppe `istFinal`, Admin-Spalte Art, ADR 0036. v1.99.72, TS-193. **Abhaengig von Rueckfrage 12** (Reihenfolge zu SQL/164) – ohne Antwort: Filter nur mit `IstAbschliessend` bauen und den Konflikt in der Lauf-Notiz als Blocker fuer die Isolierungsfraese-Spec festhalten. | offen | |
| 4 | FA-Sicht: Wirksamkeits-Helfer, Zeilenquelle als gemeinsame Service-Methode, View `Fa`, Kopf-Buchungen, Modal-Partials, Scan, Standardsicht-Auswahl, gemerkte Sicht. v1.99.73, TS-194, keine Migration. **Abhaengig von Rueckfragen 2, 3, 4, 13, 14** (Empfehlungen als Vorgabe). | offen | |
| 5 | Tageshistorie (Seite, Knopf, NavZiel) und Redirect der alten Terminal-Seite an IDEAL. v1.99.74, TS-195, keine Migration. **Abhaengig von Rueckfragen 1, 5.** | offen | |

## Deploy

> [!warning] Vorlaeufig – der qa-agent bestaetigt gegen den echten Diff.

- **Web-App:** ja (alle Etappen).
- **Service:** nein, nur Versionsnummer in `AppVersion.cs` (beide) – vorausgesetzt Rueckfrage 3 = ja (Erkennung bleibt am Feld). Bei anderer Antwort: ja.
- **Migration:** ja (Etappe 3, SQL/168). Reihenfolge: SQL vor Web-Start; Dienst muss nicht gestoppt werden (der Dienst-Code `IDEALAKEWMSService/` referenziert `BdeTeilMeldung` an keiner Stelle – grep 2026-11-25 ohne Treffer).
- **Manifest-Eintraege** (`scripts/deploy/nachlauf.psd1`):

| Id | Version | Typ | Firma | Phase | Ausfuehrung | Text |
|---|---|---|---|---|---|---|
| `v1.99.70-BdeTeilelisteAgStatusMobilInfo` | 1.99.70 | Info | IDEAL | Nachher | – | „BDE-Teileliste: neue Spalte AG-Status, Baugruppen-Teile als Zeilen, kompakte Ansicht am Handy. Werker kurz informieren.“ (ersetzt den Info-Eintrag `v1.99.44-BdeTeilelisteMobilInfo` der ueberholten Mobil-Spec) |
| `v1.99.71-BdeTeilelisteSummiertInfo` | 1.99.71 | Info | IDEAL | Nachher | – | „Summiert: Auftraege aufklappbar, Haken auf Summenzeile meldet alle darunter fertig.“ |
| `v1.99.72-SQL168` | 1.99.72 | Schema | beide | Schema | automatisch | `SQL/168_AddBdeTeilMeldungIstAbschliessend.sql` – Spalte + gefilterter UNIQUE neu; Reihenfolge nach SQL/164, falls diese im selben Paket ist |
| `v1.99.72-BdeTeilfertigInfo` | 1.99.72 | Info | IDEAL | Nachher | – | „Teilfertig/Fertig melden in der Teileliste; Teilfertig laesst die Zeile offen.“ |
| `v1.99.73-BdeStandardsichtPruefung` | 1.99.73 | Pruefung | IDEAL | Nachher | anzeigen | `SELECT Name, ArbeitsschrittCode, BdeTeileliste FROM ProductionWorkplaces WHERE BdeAktiv = 1 ORDER BY Name;` – Standardsicht je BDE-Werkbank zur Kontrolle |
| `v1.99.73-BdeStandardsichtEinstellung` | 1.99.73 | Einstellung | IDEAL | Nachher | – | „Werkbank-Stammdaten: BDE-Standardsicht je Werkbank (Teile / FA) pruefen und setzen.“ |
| `v1.99.74-BdeTerminalAbloesungInfo` | 1.99.74 | Info | IDEAL | Nachher | – | „BDE-Terminal oeffnet an IDEAL die neue Seite mit Reitern; Tageshistorie im Menue.“ |

  SQL/168 ist ein Schema-Skript (kein Trockenlauf/Transaktion noetig). Keine Daten-/Einmal-Skripte.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (Service nur bei geaenderter Antwort auf Rueckfrage 3: `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`)

## Offene Rueckfragen

**Fachbereich / Gerald**
1. (Gerald) Alte Terminal-Seite: Etappe 5 leitet nur an IDEAL (Master `ProduktionsauftragHierarchisch` an, `BdeNurFaMeldung` aus) auf die neue Seite um;
   fuer AKE und den NurFA-Modus bleibt die alte Seite unveraendert. **Empfehlung: ja** – AKE arbeitet flach/OSEON, die neue Seite setzt Sage-Struktur voraus.
2. (Gerald) NurFA-Modus an IDEAL: Die neue FA-Sicht gilt nur bei `BdeNurFaMeldung` aus. Pruefabfrage IDEAL-DB:
   `SELECT [Key],[Value] FROM AppSettings WHERE [Key] = 'BdeNurFaMeldung';` **Empfehlung: ja** (NurFA bucht Fertigungsauftraege statt Arbeitsgaenge, eigener Ablauf `StartProductionForOrder`).
3. (Gerald) Erkennung V1 (`WorkOperationStructureDetectionService.cs:96`) bleibt an Standardsicht „Teile“ gekoppelt; an Werkbaenken mit Standard FA zeigt der
   Teile-Reiter einen Hinweis, dass Teilmeldungen dort keinen Vater-AG schliessen. Alternative (a): Erkennung an „Kuerzel vorhanden“ – dann verschwinden
   Vater-AGs fuer alle Kuerzel-Werkbaenke auch aus der FA-Sicht, Dienst-Deploy noetig. **Empfehlung: ja (b, Kopplung bleibt).**
4. (Fachbereich: Walter/Benjamin) AG-Status-Spalte in der FA-Sicht zeigt alle Arbeitsgaenge der Sub-FA, der Arbeitsgang der Zeile ist markiert. **Empfehlung: ja.**
5. (Fachbereich: Walter/Benjamin) Tageshistorie zeigt neben den Buchungen auch die Teilmeldungen des Tages. **Empfehlung: ja.**
6. (Fachbereich: Walter/Benjamin) Welches Konto/Geraet am Handy? **Empfehlung:** je Geraet ein Terminal-Benutzer, vom BDE-Admin angelegt (Terminal-Pflicht
   `BdeTeilelisteController.cs:87-91`); keine Codeaenderung.
7. (Gerald) Soll die FA-Struktur die Teilmeldungen ebenfalls als Status zeigen (heute nur WorkOperations, `FaArbeitsgangStatus.cs:32-95`)? **Empfehlung: nein**, eigene Backlog-Notiz.

**Technisch**

8. Schwelle Kartenansicht 992 px oder 768 px? **Empfehlung: 992 px** (Tablets hochkant bekommen Karten).
9. Filter-Bedienelemente 44 px ueberall (auch alte Terminal-Seite), Hinweistexte einklappbar? **Empfehlung: ja.**
10. Spaltenfilter in der Kartenansicht nicht bedienbar (Filterzeile im `thead`, `table-filter.js:150-156`; nur Zaehler + Zuruecksetzen) – akzeptabel? **Empfehlung: ja.**
11. Aggregierte Ampel ueber `ArbeitsgangStatusRegel.Ableiten`: gemischt laeuft+ueberfaellig ergibt `LaeuftUeberfaellig` (beide Zeichen). **Empfehlung: ja** (ueberfaellig bleibt sichtbar, eine Regel).
12. Koordination mit SQL/164 ([[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]]): beide bauen `UX_BdeTeilMeldungen_Schluessel` neu. **Empfehlung:**
    wer zweitens baut, vereinigt die Filter und prueft im Index-Guard beide Bedingungen.
13. Teile-Sicht zusaetzlich an den Master-Schalter binden statt auf „AKE hat keine Kuerzel“ (`WerkbankKuerzelFamilie.cs:10`, unbelegt) zu vertrauen.
    Pruefabfrage je DB: `SELECT COUNT(*) FROM ProductionWorkplaces WHERE ISNULL(ArbeitsschrittCode,'') <> '';` **Empfehlung: Master-Gate.**
14. FA-Sicht nutzt Pagination statt `BdeTerminalMaxHauptFa` (Gruppen + „Weitere“, `bde-terminal.js:685-699`); die Einstellung wirkt nur noch auf der alten Seite. **Empfehlung: ja.**

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Alte Terminal-Seite nur an IDEAL ersetzen, AKE/NurFA behalten → **Nein: neue Sicht wird Standard auch fuer AKE (AKE nutzt BDE derzeit nicht). Gerald 2026-11-25**
2. FA-Sicht nur bei BdeNurFaMeldung aus → **BdeNurFaMeldung steht produktiv auf false (Gerald 2026-11-25), gilt als Altlast: neue Sicht beachtet die Einstellung nicht. Entfernen der Einstellung = eigene Aufgabe [[2026-11-25-bde-nur-fa-meldung-entfernen]], nicht im Epic.**
3. Erkennung V1 bleibt an Standardsicht Teile (Variante b) → **Variante a (Gerald 2026-11-25): `BdeTeileliste` behaelt seine Bedeutung „Blechteile per Teilmeldung“ (steuert Dienst/Erkennung V1, unveraendert); NEUES Feld „BDE-Standardsicht: Teile / FA“ nur fuer die Oberflaeche, Spalte in der Migration von Etappe 3 (SQL/168) mit anlegen. Kein Dienst-Code.**
4. AG-Status FA-Sicht: alle AG der Sub-FA, eigener markiert → **Ja (Gerald fuer Fachbereich, 2026-11-25)**
5. Tageshistorie inkl. Teilmeldungen → **Ja (Gerald, 2026-11-25)**
6. Handy-Konto: Terminal-Benutzer je Geraet → **Android-Handys, keine Windows-Konten; Konten noch nicht angelegt (Gerald 2026-11-25). Anmeldung ueber lokales WMS-Konto (Formular-Login `AccountController.Login`) + Terminal-Zuordnung – Betriebsaufgabe, kein Code.**
7. FA-Struktur ohne Teilmeldungs-Status (eigene Notiz) →
8. Schwelle 992 px →
9. Filter 44 px ueberall →
10. Spaltenfilter in Kartenansicht nicht bedienbar →
11. Aggregation ueber Ableiten (LaeuftUeberfaellig) →
12. SQL/164-Koordination: Zweiter vereinigt die Filter →
13. Master-Gate fuer Teile-Sicht →
14. Pagination statt BdeTerminalMaxHauptFa →
