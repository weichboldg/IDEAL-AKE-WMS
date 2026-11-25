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
standort: beide
affected_code:
  - IdealAkeWms/Controllers/BdeTeilelisteController.cs
  - IdealAkeWms/Services/BdeTeilelisteService.cs
  - IdealAkeWms/Services/WerkbankKuerzelFamilie.cs
  - IdealAkeWms/Services/BdeBookingService.cs
  - IdealAkeWms/Services/ArbeitsgangStatusRegel.cs
  - IdealAkeWms/Models/ViewModels/FaArbeitsgangStatus.cs
  - IdealAkeWms/Models/ViewModels/BdeTeilelisteViewModel.cs
  - IdealAkeWms/Models/ViewModels/ProductionWorkplaceEditViewModel.cs
  - IdealAkeWms/Models/BdeTeilMeldung.cs
  - IdealAkeWms/Models/ProductionWorkplace.cs
  - IdealAkeWms/Models/AppSettingDefinitions.cs
  - IdealAkeWms/Models/AppSettingKeys.cs
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Data/Repositories/ProductionWorkplaceRepository.cs
  - IdealAkeWms/Migrations/<Zeitstempel>_AddBdeTeilMeldungIstAbschliessendUndStandardsicht.cs
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
  - IdealAkeWms/Views/BdeTerminal/Index.cshtml
  - IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml
  - IdealAkeWms/Views/ProductionWorkplaces/_BdeTeilelisteField.cshtml
  - IdealAkeWms/Views/ProductionWorkplaces/Index.cshtml
  - IdealAkeWms/Views/BdeBookings/Teilmeldungen.cshtml
  - IdealAkeWms/Views/Shared/_Layout.cshtml
  - IdealAkeWms/wwwroot/js/bde-teileliste.js
  - IdealAkeWms/wwwroot/js/bde-terminal.js
  - IdealAkeWms/wwwroot/css/bde.css
  - SQL/168_AddBdeTeilMeldungIstAbschliessendUndStandardsicht.sql
  - SQL/00_FreshInstall.sql
  - docs/betrieb/09-sicherheit-konten-rechte.md
open_questions: []
epic: true
etappen:
  - "1 Teile Einzeln: AG-Status, Baugruppen-Kinder als Zeilen, Filter auf Kinder, Teile-Knopf vorn, BDE mobil kompakt (v1.99.70, TS-191, keine Migration)"
  - "2 Teile Summiert: Einzelauftraege aufklappbar, Summen-Haken, aggregierte Ampel (v1.99.71, TS-192, keine Migration)"
  - "3 Teilfertigmeldung mit Restmenge, ADR 0036, Baugruppe als Teil-Buchung; SQL/168 legt zusaetzlich die Spalte BdeStandardsichtTeile an (ohne Leser bis Etappe 4) (v1.99.72, TS-193, SQL/168)"
  - "4 FA-Sicht als Tabelle, Buchungen im Kopf, Reiter + neues Feld BDE-Standardsicht je Werkbank, FA-Sicht auch an AKE (v1.99.73, TS-194, keine weitere Migration)"
  - "5 Tageshistorie eigene Seite + Knopf, alte Terminal-Seite an beiden Standorten abgeloest und entfernt (v1.99.74, TS-195, keine Migration)"
deploy:
  web: true
  service: true
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

> [!note] Ueberarbeitet 2026-11-25 (Rework)
> Die Freigabe-Antworten 1–14 (Gerald 2026-11-25) sind in Rumpf, Etappen, Einstellungen, Migration, Akzeptanzkriterien und Testszenarien
> eingearbeitet. Wesentlich: neue Sicht ist Standard an **beiden** Standorten und ersetzt die alte Terminal-Seite ueberall (Antwort 1);
> `BdeNurFaMeldung` wird von der neuen Sicht nicht beachtet (Antwort 2); `BdeTeileliste` behaelt seine Bedeutung, die Standardsicht ist ein
> **neues** Feld `BdeStandardsichtTeile` (Antwort 3 = Variante a). Der Rumpf ist allein ein vollstaendiger Arbeitsauftrag.

## Ziel / Nutzen (das Warum)

An IDEAL arbeiten heute zwei BDE-Masken mit verschiedener Technik nebeneinander: die serverseitige Teileliste (Tabelle, Zahnrad,
Server-Spaltenfilter, Pagination) und das JS-gerenderte Terminal (Karten „1. Operator“ / „2. Arbeitsgang wählen“, eigene Listenlogik in
`wwwroot/js/bde-terminal.js`, 1035 Zeilen). Werker wechseln zwischen zwei Bedienkonzepten, Status sehen sie nur im Terminal, eine Teilmenge
können sie in der Teileliste nicht melden, und am Handy ist die Teileliste kaum bedienbar.

Ziel: **eine** serverseitige Seite mit den Reitern *Teile Einzeln · Teile Summiert · FA*, gleichem kompakten Kopf, gleicher AG-Status-Ampel
wie in der FA-Struktur, Teilfertigmeldung mit Restmenge und einer mobil bedienbaren Darstellung. Die FA-Sicht ersetzt die alte
Terminal-Seite an **beiden** Standorten (Bauweg A, ADR 0005; Antwort 1). AKE nutzt BDE derzeit nicht; seine Werkbaenke haben kein Kuerzel
und bekommen nur die FA-Sicht.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope** (Etappen siehe unten):
- Teile Einzeln: Spalte AG-Status, Baugruppen-Kinder als eingerueckte Tabellenzeilen, Spaltenfilter trifft Kinder, „Teile (n) ▸“ neben dem Matchcode.
- BDE mobil kompakt (F1–F6 der ueberholten Mobil-Spec) fuer alle Reiter dieser Seite.
- Teile Summiert: Einzelauftraege aufklappbar, Summen-Haken, aggregierte Ampel je Kuerzel.
- Teilfertigmeldung (mehrere Meldungen je Zeile, nur die abschliessende ist eindeutig) inkl. Migration, ADR 0036, Ruecknahme, Waisen, Baugruppe als Teil-Buchung.
- FA-Sicht als Tabelle im gemeinsamen Kopf, laufende/pausierte Buchungen im Kopf, Anstempeln je Zeile, alle Terminal-Ablaeufe uebernommen.
- ~~Werkbank-Feld `BdeTeileliste` wird Auswahl „BDE-Standardsicht: Teile / FA“ (gleiches Feld)~~ *(ueberholt durch Antwort 3 = Variante a)*:
  **neues** Werkbank-Feld `BdeStandardsichtTeile` als Auswahl „BDE-Standardsicht: Teile / FA“ (nur Oberflaeche); `BdeTeileliste` bleibt unveraendert.
  Zuletzt gewaehlte Sicht je Werker+Werkbank.
- Tageshistorie als eigene Seite, Knopf im Kopf und Menuepunkt; Abloesung der alten Terminal-Seite an **beiden** Standorten, Entfernen von
  `Views/BdeTerminal/Index.cshtml` und `wwwroot/js/bde-terminal.js` (Antwort 1).
- FA-Sicht auch an AKE (flache FAs, Werkbaenke ohne Kuerzel = nur FA-Sicht).

**Out-of-Scope**:
- ~~AKE / OSEON und der globale NurFA-Modus (`BdeNurFaMeldung`): alte Terminal-Seite bleibt unveraendert (Rueckfragen 1, 2).~~ *(ueberholt, Antworten 1 und 2)*
- Entfernen der Einstellung `BdeNurFaMeldung` und ihrer NurFA-Zweige (`BdeApiController.cs:213-253`, `BdeTerminalController.StartProductionForOrder`
  `:79`, `BdeBookingService.cs:543`, `SettingsController.cs:131`): eigene Aufgabe [[2026-11-25-bde-nur-fa-meldung-entfernen]] (Antwort 2). Die neue Sicht
  liest die Einstellung nicht; produktiv steht sie auf `false` (Gerald 2026-11-25).
- Dienst-Code: keiner. Erkennung V1 bleibt an `BdeTeileliste` (Antwort 3).
- FA-Struktur (`FaHierarchy`) zeigt Teilmeldungen nicht als Status (Antwort 7).
- Neue Rollen oder Rechte: keine.
- Anlage der Handy-Konten (Android, lokales WMS-Konto + Terminal-Zuordnung): Betriebsaufgabe, kein Code (Antwort 6).
- ~~Loeschen von `bde-terminal.js` / `Views/BdeTerminal/Index.cshtml`: bleibt fuer AKE/NurFA.~~ *(ueberholt, Antwort 1: wird in Etappe 5 geloescht)*
- Zusatzfunde (nur melden, nicht bauen): totes `#activityModal` (`Views/BdeTerminal/Index.cshtml:136`, Handler `bde-terminal.js:707-714`; das Modal wird
  nirgends geoeffnet, `#activitySelect` nie gefuellt); totes `#sessionExpiry` (`Views/BdeTerminal/Index.cshtml:30`, wird nirgends gesetzt –
  der Backlog-Punkt „Session-Ablauf“ hat also keine Logik zum Uebernehmen); `_Pagination.cshtml:75-76` Label ohne `for` (aus Mobil-Spec).

## Fachliche Anforderungen

Entscheidungen aus dem Backlog („Entscheidungen“ 1–5 und „Vorschlaege Claude (bestaetigt)“) und die Freigabe-Antworten 1–14 (Gerald 2026-11-25)
gelten als gesetzt und sind hier eingearbeitet; Verweise „Antwort n“ zeigen auf den Block „Freigabe-Antworten“.

### Gemeinsam (alle Etappen)

- **G1** Eine Seite `BdeTeileliste` mit Reitern *Teile Einzeln · Teile Summiert · FA* (FA ab Etappe 4). Gleicher `_Kopf`, Zahnrad, Server-Spaltenfilter, Pagination.
  Ab Etappe 4 an beiden Standorten; die Teile-Reiter nur, wo die Teile-Sicht verfuegbar ist (Master an, BDE aktiv, Kuerzel – E4-7).
- **G5** Die Seite liest `BdeNurFaMeldung` nicht (Antwort 2); die neuen Helfer haben keinen NurFA-Parameter.
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
  Entschieden: Schwelle **992 px** (Antwort 8); Filter-Bedienelemente **44 px ueberall**, Hinweistexte in `<details>` (Antwort 9); Spaltenfilter
  in der Kartenansicht **nicht bedienbar**, dort nur Zaehler + „Zuruecksetzen“ (Antwort 10); Handy = Android mit lokalem WMS-Konto, siehe Betrieb (Antwort 6).
  Der kompakte Kopf ist der Kopf aller Reiter, also auch der FA-Sicht (Etappe 4).

### Etappe 2 – Teile Summiert

- **E2-1** Summenzeile aufklappbar: darunter die Einzelauftraege (heutige Einzeln-Zeilen derselben Gruppe), **FE-Termin aufsteigend**, je mit eigenem Haken und AG-Status.
- **E2-2** Haken auf der Summenzeile = **volle Fertigmeldung aller offenen Auftraege darunter** (heute schon so: Wert = Liste `OffeneSchluessel`,
  `Views/BdeTeileliste/Summiert.cshtml`; Server meldet je Schluessel, `BdeTeilelisteService.cs:256-275`). Reicht die Menge nicht, waehlt der Werker
  in der Unterebene einzelne Auftraege; nicht gemeldete bleiben offen.
  Summen-Haken und Einzel-Haken derselben Gruppe schliessen sich aus (JS: Summen-Haken setzt alle Einzel-Haken, Einzel-Haken nimmt Summen-Haken zurueck;
  der Server entdoppelt ohnehin per `DistinctBy`).
- **E2-3** Aggregierte Ampel je Kuerzel auf der Summenzeile: schlechtester Stand gewinnt – `Ableiten(alle erledigt, irgendeine laeuft,
  fruehester Soll der nicht erledigten)`; erledigt nur, wenn alle erledigt sind; „teilgemeldet“ zaehlt als laeuft; gemischt laeuft + ueberfaellig
  ergibt `LaeuftUeberfaellig` (beide Zeichen) (Antwort 11).

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
  An AKE (Werkbank ohne Kuerzel) ist die Familie nur die Werkbank selbst (`WerkbankKuerzelFamilie.cs:26`), die Kopfdaten-Abfrage liefert dort
  nichts (`BdeApiController.cs:290`, „AKE leer“) – Spalten aus der FA-Struktur (Kunde, Montage-Abteilung, FE-Termin aus Kopfzeile) bleiben leer.
- **E4-2** Spalten (Zahnrad, `defaultHidden` wo genannt): Haupt-FA, Sub-FA, Arbeitsgang (Kuerzel + Name), **AG-Status** (Antwort 4: alle AG der
  Sub-FA, der Arbeitsgang der Zeile markiert), Bezeichnung, Artikelnummer, Matchcode, Sollmenge, FE-Termin, Soll-Termin AG, Prio, Kunde, Montage-Abteilung;
  defaultHidden: Material, Arbeitsschritte, Arbeitsbereich, Bemerkung PN, Fertigungsinfo PN, Sage-Status, Vater-FA, Position.
- **E4-3** Aktion je Zeile: **Anstempeln** = „Rüsten“ / „Produktion“ (heutige Start-Knoepfe `bde-terminal.js:355-379`); bei
  `BdeNurAgFertigmeldung` stattdessen „Fertig melden“ (Mengen-Dialog, Vorgabe Sollmenge). Die FA-Sicht ermittelt das ueber
  `NurAgFertigmeldungWirksam(w, nurFaGlobal: false)` (G5).
- **E4-4** Kopf: Karten „1. Operator“ / „2. Arbeitsgang wählen“ entfallen. Werker-Leiste wie Teileliste; darin **laufende und pausierte Buchungen
  des Werkers als kompakte Zeilen** mit Teilfertig / Pause / Fertig bzw. Fortsetzen. Zeit-/Detailangaben (Startzeit, Laufzeit, Personal, Terminal)
  per Schalter ein-/ausblendbar, gemerkt (localStorage je Benutzer, rein darstellend).
- **E4-5** Scanfeld FA-Nr als schmale Zeile ueber der Tabelle (`data-hw-scan-ziel`, Hardware-Scanner), Aufloesung ueber `/api/bde/resolve-scan`.
- **E4-6** Alle Terminal-Ablaeufe aus der Ablaufliste unten bleiben erhalten (Kollision, Gruppen-Fertigmeldung, Close-Others, ungeplante Taetigkeit, Mengen-Dialog, Doppelklick-Sperre).
- **E4-7** Standardsicht je Werkbank (Antwort 3 = Variante a): **neues** Feld `ProductionWorkplace.BdeStandardsichtTeile` (bit), in der Werkbank-Maske
  Auswahl „BDE-Standardsicht: Teile / FA“ – nur Oberflaeche. `BdeTeileliste` („Blechteile per Teilmeldung“) bleibt **unveraendert** in Bedeutung,
  Maske und Wirkung auf den Dienst (Erkennung V1).
  - Teile-Sicht verfuegbar = Master `ProduktionsauftragHierarchisch` an **und** Werkbank BDE aktiv **und** Kuerzel gesetzt (Antworten 3, 13).
    `BdeTeileliste` ist dafuer **keine** Voraussetzung.
  - Master aus (AKE): Teile-Reiter werden nicht angezeigt, nur FA-Sicht.
  - Master an, Werkbank ohne Kuerzel: Teile-Reiter deaktiviert (`aria-disabled`, Hinweis „Diese Werkbank hat kein Arbeitsschritt-Kürzel – nur FA-Sicht“).
  - Standard beim Aufruf: zuletzt gewaehlte Sicht je Werker+Werkbank (vorhandener `GespeicherterFilter.Sicht`, `BdeTeilelisteController.cs:44,58`);
    ohne gespeicherten Wert Teile, wenn `BdeStandardsichtTeile` und Teile-Sicht verfuegbar, sonst FA. Eine gespeicherte Teile-Sicht an einer Werkbank
    ohne verfuegbare Teile-Sicht faellt auf FA zurueck.
  - Teile-Reiter an einer Werkbank, deren Kuerzel an **keiner** BDE-aktiven Werkbank `BdeTeileliste` traegt (genau die Bedingung der Erkennung V1,
    `WorkOperationStructureDetectionService.cs:96`): Hinweis-Banner „Für dieses Kürzel legt die Struktur-Erkennung Vater-Arbeitsgänge an –
    Teilmeldungen schließen diese nicht ab.“

### Etappe 5 – Tageshistorie, Ablosung

- **E5-1** Tageshistorie als eigene Seite (Buchungen des Werkers heute plus Teilmeldungen des Tages, Antwort 5), Knopf im Kopf, Menuepunkt in
  Kategorie Produktion/BDE (ADR 0034).
- **E5-2** ~~`BdeTerminal/Index` leitet an IDEAL (Master an, NurFA aus) auf die neue Seite um; fuer AKE/NurFA bleibt die alte Seite.~~ *(ueberholt, Antwort 1)*
  `BdeTerminal/Index` leitet an **beiden** Standorten immer auf die neue Seite um (Werkbank = Query oder Terminal-Vorgabe, Sicht wie E4-7).
  Die alte Seite (`Views/BdeTerminal/Index.cshtml`, `wwwroot/js/bde-terminal.js`) wird entfernt. Die JSON-Actions des `BdeTerminalController`
  bleiben (die FA-Sicht nutzt sie). `StartProductionForOrder` bleibt bis [[2026-11-25-bde-nur-fa-meldung-entfernen]] stehen (ohne Aufrufer).
- **E5-3** `BdeTerminalMaxHauptFa` hat nach E5-2 keinen Leser mehr (einziger Leser `BdeTerminalController.cs:72`, Ausgabe nur an `Index.cshtml:207`):
  Schluessel, Katalogeintrag (`AppSettingDefinitions.cs:235`) und Testtupel (`AppSettingDefinitionsTests.cs:25`) werden entfernt. Die DB-Zeile bleibt
  liegen und erscheint in `/Settings` als verwaist (`SettingsController.cs:69`, `Orphans`) – Folge von Antworten 1 + 14, keine neue Entscheidung.

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
  „Teilmeldungen je (Schluessel, Kuerzel)“ (erledigt/laeuft), die nur die Teileliste uebergibt. Die FA-Struktur bleibt dadurch unveraendert (Antwort 7).
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
- **Zusaetzlich (Antwort 3):** `ProductionWorkplace` + Spalte **`BdeStandardsichtTeile bit NOT NULL DEFAULT 0`** (0 = FA, 1 = Teile), Name analog
  `BdeTeileliste`/`BdeNurAgFertigmeldung`. Nur Model-Property mit `[Display(Name = "BDE-Standardsicht")]` + Migration; Leser, Maske und
  `EditableProperties` folgen in Etappe 4.
  **Reihenfolge geprueft:** Die Spalte entsteht in Etappe 3 und hat bis Etappe 4 keinen Leser. Das ist unschaedlich – EF liest eine Spalte ohne
  Verwendung problemlos mit, der Dienst mappt `ProductionWorkplace` nicht auf dieses Feld (er liest nur `BdeAktiv`/`BdeTeileliste`,
  `WorkOperationStructureDetectionService.cs:96`), und das Epic wird in einem Stueck gemergt und deployt. Eine eigene Migration in Etappe 4 wuerde
  eine zweite SQL-Nummer kosten, die Antwort 3 ausdruecklich nicht will.
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

- **Wirksamkeit neu** in `WerkbankKuerzelFamilie` (ersetzt `TeilelisteWirksam`; Antworten 1, 2, 3a, 13 – ohne NurFA-Parameter, G5):
  - `SeiteVerfuegbar(w) = w.BdeAktiv` (beide Standorte, kein Master-Gate fuer die Seite/FA-Sicht);
  - `TeileSichtVerfuegbar(w, hierarchisch) = w.BdeAktiv && hierarchisch && Code(w) != null` (Master-Gate, Antwort 13);
  - `TeileStandard(w, hierarchisch) = TeileSichtVerfuegbar(w, hierarchisch) && w.BdeStandardsichtTeile`.
  - `TeilelisteWirksam` entfaellt samt Test (`BdeTeilelisteOptionTests.cs:86-92`, auf die neuen Helfer umschreiben). `BdeTeileliste` wird im Web
    danach nur noch von Maske, Liste, Validierung und Banner-Bedingung gelesen; seine Wirkung bleibt die im Dienst.
  - ~~Pruefabfrage „AKE hat keine Kuerzel“~~ *(ueberholt: Antwort 13 bindet an den Master, die Abfrage entscheidet nichts mehr)*.
- Aufrufer umstellen:
  - `BdeBookingService.cs:288` → `NurAgFertigmeldungWirksam(...) || TeileSichtVerfuegbar(w, hierarchisch)`. Der Master ist eine ServiceSetting
    (`HierarchischeStrukturKeys.Master`); `BdeBookingService` bekommt dafuer `IServiceSettingRepository` als Ctor-Parameter (Lesemuster wie
    `ProductionWorkplacesController.cs:251-255`); Test-Konstruktionen (`BdeTeilelisteMeldenTests.cs:214` u. a.) anpassen.
    Folge von Antwort 3a, bewusst: „meldet ohne Zeit“ gilt damit an jeder IDEAL-Werkbank mit Kuerzel, nicht mehr nur an `BdeTeileliste`-Werkbaenken.
  - `BdeTeilelisteService.cs:261` → `TeileSichtVerfuegbar` (Master liest der Service ebenso ueber `IServiceSettingRepository`).
  - `BdeTeilelisteController.cs:75` → `SeiteVerfuegbar`; die Teile-Reiter (Index/Summiert, Melden) zusaetzlich `TeileSichtVerfuegbar`. Der Master
    wird im Controller schon gelesen (`BdeTeilelisteController.cs:212-214`) – in eine private Methode ziehen und wiederverwenden.
    `BdeTeilelisteViewModel.Kuerzel` wird nullable (AKE).
    **Nie zurueck auf `BdeTerminal` leiten**, wenn `SeiteVerfuegbar` falsch ist (Werkbank fehlt / BDE inaktiv): ab Etappe 5 leitet `BdeTerminal/Index`
    hierher – das ergaebe eine Schleife. Stattdessen Anmelde-/Kopfansicht mit Werkbank-Auswahl und Hinweis „Werkbank ist nicht BDE-aktiv“.
  - `BdeTerminalController.cs:65` → `TeileStandard` (nur bis Etappe 5, dann Redirect immer).
  - Liste/Maske: `ProductionWorkplacesController.cs:54` (Spaltentext `bde`: zusaetzlich „Standard Teile“ bei `BdeStandardsichtTeile`) und
    `Views/ProductionWorkplaces/Index.cshtml:69-72` (Badge). Der Text „Teileliste“ fuer `BdeTeileliste` bleibt.
- **Erkennung V1** (`WorkOperationStructureDetectionService.cs:96`) bleibt an `BdeTeileliste` (Antwort 3a). Kein Dienst-Code, Service nur Versionsnummer.
  Hinweis-Banner im Teile-Reiter nach der Bedingung in E4-7.
- **Zeilenquelle:** die Logik aus `BdeApiController.cs:204-327` (nicht-NurFA-Zweig) in eine Service-Methode ziehen, die API und FA-Sicht
  gemeinsam nutzen (eine Stelle). FA-Sicht serverseitig: Spalten-Getter-Dictionary, `ColumnFilterHelper.Apply` vor Pagination, Datumsspalten
  nach der Termin-Berechnung filtern (ADR 0005). Neue View `Views/BdeTeileliste/Fa.cshtml`, viewKey `BdeTeilelisteFa`, Register-Schluessel
  `BdeTeileliste/Fa` in `ColumnDefinitions` und `ColumnFilterSyntax`.
- **Kopf-Buchungen** serverseitig aus denselben Repository-Methoden wie `ActiveBookings`/`PausedBookings` (`BdeTerminalController.cs:270,294`);
  Aktionen per kleinem JS (`bde-fa-sicht.js`) gegen die **bestehenden** JSON-Actions, danach `location.reload()`.
  Mengen-, Kollisions-, Gruppen- und Close-Others-Modal als Partials aus `Views/BdeTerminal/Index.cshtml:114-200` herausziehen und auf beiden Seiten nutzen
  (keine Kopie). `wmsSperren`/`wmsFreigabeQtyOk` (`site.js`) fuer die Doppelklick-Sperre wiederverwenden.
- ~~**Werkbank-Maske:** `_BdeTeilelisteField.cshtml` Schalter → `<select>` „BDE-Standardsicht“ …; `[Display(Name=...)]` in `ProductionWorkplace.cs:57` → „BDE-Standardsicht“.~~
  *(ueberholt, Antwort 3a)*
- **Werkbank-Maske (Antwort 3a):** Schalter „BDE-Teileliste (vereinfacht)“ (`_BdeTeilelisteField.cshtml`, `ValidateTeilelisteAsync`
  `ProductionWorkplacesController.cs:259-268`) bleibt **unveraendert**. **Daneben** im selben Partial ein `<select>` „BDE-Standardsicht“ mit
  `Teile` / `FA` fuer `BdeStandardsichtTeile`; Anzeige wie der Schalter nur bei `ViewBag.Hierarchisch` (an AKE unsichtbar, Wert bleibt 0 = FA).
  `ProductionWorkplaceEditViewModel` + Property; `ProductionWorkplaceRepository.EditableProperties` (`:13-20`) + `BdeStandardsichtTeile` (sonst
  speichert `UpdateAsync` das Feld nicht). Server-Validierung analog `ValidateTeilelisteAsync`: „Teile“ nur mit Master an, BDE aktiv und Kuerzel,
  sonst `ModelState`-Fehler, nichts gespeichert (auch ein manipulierter POST an AKE).

#### Ablaufliste Terminal → FA-Sicht (nichts darf verloren gehen)

| # | Ablauf heute | Beleg | Endpunkt | FA-Sicht |
|---|---|---|---|---|
| 1 | Werker per Personalnummer | `bde-terminal.js:99-140` | GET `/api/bde/operator/{nr}` | vorhandene Anmeldung `Anmelden.cshtml` (Server) |
| 2 | Werker wechseln / Reset | `:142-162` | – | „Werker wechseln“ im Kopf (`_Kopf.cshtml:21-25`) |
| 3 | FA/AG-Scan, Mehrdeutigkeit, Gruppe oeffnen, Sub-FA-Fallback | `:165-231`, `:262-330` | GET `/api/bde/resolve-scan` | Scanzeile; Exact → Zeile markieren + Anstempeln anbieten; Ambiguous → Auswahldialog; NotInScope/NotFound → Meldung |
| 4 | Scan bucht FA direkt (ProductionOrder) | `:237-258` | POST `BdeTerminal/StartProductionForOrder` | nur NurFA → entfaellt (Antwort 2: Einstellung wird nicht beachtet; Endpunkt bleibt bis [[2026-11-25-bde-nur-fa-meldung-entfernen]]) |
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
| 18 | NurFA-Oberflaeche | `:722-728` | – | entfaellt (Antwort 2) |
| 19 | Gruppen auf-/zuklappen, „Weitere“ (MaxHauptFa) | `:685-699` | – | Pagination (Antwort 14); Einstellung entfaellt in Etappe 5 (E5-3) |
| 20 | Hidden Inputs NurFaMode/NurAgFertigmeldung/Hierarchisch/MaxHauptFa | `Views/BdeTerminal/Index.cshtml:201-210` | – | NurAg serverseitig je Zeile; Hierarchisch steuert nur die Teile-Reiter (an AKE aus); NurFa/MaxHauptFa entfallen |
| – | Activity-Modal, Session-Ablauf | `:707-714`, `Index.cshtml:30,136` | – | tot, nichts zu uebernehmen (Zusatzfund) |

### Etappe 5

- Action `BdeTeileliste/Tageshistorie(int? workplaceId, int? operatorId, page, pageSize)` mit denselben Gates (`[RequireBdeActive][RequireBdeUserAccess]`, Terminal-Pflicht).
  Ohne Werker → Anmelden. Quelle: `GetHistoryAsync` wie `BdeApiController.cs:147`, ohne die feste Kappung auf 50, mit Pagination +
  Server-Spaltenfilter (ADR 0005), plus Teilmeldungen des Tages (Antwort 5) als Spalte „Art“ (Buchung / Teilmeldung). An AKE gibt es keine
  Teilmeldungen; die Seite zeigt dort nur Buchungen.
- NavZiel `BdeTageshistorie` (Kategorie `KatProduktionBde`, `NavigationKatalog.cs:16`), Regel wie `BdeTerminal` (`bde && useBde`, `:271`), Link im Layout
  (ADR 0034, fallstricke §58, `NavigationSichtTests`).
- ~~`BdeTerminalController.Index`: `SeiteVerfuegbar` → Redirect, sonst alte Seite.~~ *(ueberholt, Antwort 1)*
  `BdeTerminalController.Index` (`:41-76`): Login- und Terminal-Pruefung (`:43-51`) bleiben; danach **immer** Redirect auf `BdeTeileliste/Index`
  mit `workplaceId ?? terminal.DefaultProductionWorkplaceId` (Sicht nach E4-7). ViewBag-Aufbau, NurFA-/MaxHauptFa-Lesen und die View entfallen;
  `bde-terminal.js` wird geloescht. Vor dem Loeschen per grep pruefen, dass kein anderer View/Test `bde-terminal.js` oder `Views/BdeTerminal/Index`
  referenziert (Stand 2026-11-25: nur `Views/BdeTerminal/Index.cshtml:209`); Waechter-Listen (Hardware-Scanner `data-hw-scan-ziel`, `ResponsiveCssTests`)
  entsprechend nachziehen. `BdeTerminalControllerTests` (`:34`, `:350-366`) auf den Redirect umschreiben.
- `#teilelisteWerkbank` (`_Kopf.cshtml:12`, Ziel `BdeTerminal/Index`) darf so bleiben – der Redirect fuehrt auf die neue Seite.

### Arbeitsweise

- Ein Worktree, je Etappe ein Commit, ein Merge am Ende. Branch waehrend der Arbeit mit `scripts/sync-worktree.ps1` auf main-Stand halten.
- Jede Etappe baut und testet gruen fuer sich. Alle Rueckfragen sind beantwortet (Gerald 2026-11-25); keine Etappe wartet auf eine Entscheidung.
- Antwort 9 („44 px ueberall, auch alte Terminal-Seite“) ist fuer die alte Seite durch Antwort 1 gegenstandslos: sie wird in Etappe 5 entfernt,
  Etappe 1 fasst sie nicht an (Scope `.bde-tl-seite`).

## Migrations-/SQL-Auswirkungen

- **Etappe 3 – einzige Schema-Aenderung (beide Spalten, Antwort 3):** `dotnet ef migrations add AddBdeTeilMeldungIstAbschliessendUndStandardsicht`
  → `SQL/168_AddBdeTeilMeldungIstAbschliessendUndStandardsicht.sql` nach Muster `SQL/154_AddBdeTeileliste.sql`:
  - NOEXEC-Guard;
  - Batch 1 `IF COL_LENGTH('dbo.BdeTeilMeldungen','IstAbschliessend') IS NULL ALTER TABLE … ADD … CONSTRAINT DF_BdeTeilMeldungen_IstAbschliessend DEFAULT (1)`;
  - Batch 2 `IF COL_LENGTH('dbo.ProductionWorkplaces','BdeStandardsichtTeile') IS NULL ALTER TABLE … ADD [BdeStandardsichtTeile] BIT NOT NULL
    CONSTRAINT DF_ProductionWorkplaces_BdeStandardsichtTeile DEFAULT (0)`; **im selben Guard-Zweig** (also nur bei Erstanlage, per `EXEC(N'…')`, weil
    die neue Spalte im selben Batch noch nicht bekannt ist) die Vorbelegung `UPDATE dbo.ProductionWorkplaces SET BdeStandardsichtTeile = 1
    WHERE BdeAktiv = 1 AND BdeTeileliste = 1 AND ISNULL(LTRIM(RTRIM(ArbeitsschrittCode)), N'') <> N''` – genau die Werkbaenke, die heute ueber
    `TeilelisteWirksam` (`WerkbankKuerzelFamilie.cs:52-53`, NurFA produktiv aus) im Terminal auf der Teileliste landen; ihr Verhalten bleibt so gleich.
    Ein zweiter Lauf aendert nichts (Guard). Die EF-Migration fuehrt dasselbe `UPDATE` per `migrationBuilder.Sql` nach dem `AddColumn` aus.
  - Batch 3 Index nur neu bauen, wenn der Filter noch nicht `IstAbschliessend` enthaelt (`sys.indexes.filter_definition`), `DROP INDEX` + `CREATE UNIQUE … WHERE [IsCancelled] = 0 AND [IstAbschliessend] = 1`;
  - Batch 4 `__EFMigrationsHistory`-Insert nur, wenn beide Spalten und der Index-Filter stimmen.
  - `SQL/00_FreshInstall.sql` an zwei Stellen: Tabellendefinitionen (`BdeTeilMeldungen` + Index `:2662`ff., `ProductionWorkplaces` neben `BdeTeileliste` `:364`)
    und MigrationId (Muster `:3516-3518`). FreshInstall ohne Vorbelegung (leere Tabelle).
  - Nicht daten-destruktiv (Vorbelegung setzt nur die neue Spalte).
  - `SQL/AgentJobs/` nicht betroffen (beide Spalten mit Default; grep 2026-11-25: kein Skript unter `SQL/` ausser 154/FreshInstall/Einmalig
    nennt `BdeTeileliste`). Der Dienst teilt das EF-Modell (`IDEALAKEWMSService.csproj:33` referenziert `IdealAkeWms.csproj`); neu angelegte
    Werkbaenke bekommen dort `BdeStandardsichtTeile = false` (= Default). Deshalb gilt: neue Dienst-Binaries erst nach SQL/168 starten (siehe Deploy).
- **SQL-Nummer:** naechste freie 168 (164/165 reserviert von [[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]], letzte belegte 167). Der Dev-Lauf prueft neu.
- **Konflikt:** SQL/164 der Isolierungsfraese-Spec baut denselben Index mit Filter `[IsCancelled]=0 AND [WarehouseRequisitionItemId] IS NULL` neu.
  Wer als zweiter baut, setzt den vereinigten Filter `[IsCancelled]=0 AND [IstAbschliessend]=1 AND [WarehouseRequisitionItemId] IS NULL` und
  prueft im Index-Guard auf beide Teile (Antwort 12, entschieden).
- Etappen 1, 2, 4, 5: keine Migration. ~~Etappe 4 aendert nur die Bedeutung von `ProductionWorkplaces.BdeTeileliste` (true = Standardsicht Teile).~~
  *(ueberholt, Antwort 3a: `BdeTeileliste` behaelt seine Bedeutung; die Standardsicht ist die neue Spalte aus SQL/168.)*
- Tests: EF InMemory erzwingt weder gefilterten UNIQUE noch `sp_getapplock` → doppelte abschliessende Meldung im Manual-UAT gegen SQL Server pruefen (fallstricke „Tests, EF, SQL Server“).

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| ~~`ProductionWorkplace.BdeTeileliste` als „BDE-Standardsicht“ (gleiches Feld)~~ *(ueberholt, Antwort 3a)* | | | | | | | |
| `ProductionWorkplace.BdeStandardsichtTeile` (Werkbank-Maske, Anzeige „BDE-Standardsicht“) – **neu** (Spalte Etappe 3, Wirkung ab Etappe 4) | Auswahl (gespeichert als bit) | `Teile` (1) · `FA` (0); Server prueft beim Speichern: „Teile“ nur mit Master an, BDE aktiv, Kuerzel gesetzt, sonst Fehler, nichts gespeichert | `FA` (0); Vorbelegung durch SQL/168: `Teile` an Werkbaenken mit wirksamer Teileliste (BDE aktiv, `BdeTeileliste`, Kuerzel) | Welcher Reiter beim Aufruf ohne gespeicherte Werker-Sicht oeffnet. Teile: Teileliste nach Matchcode, Fertigmeldung ohne Zeit. FA: Arbeitsgaenge der Werkbank mit Anstempeln. Beide Reiter bleiben umschaltbar, Teile nur bei verfuegbarer Teile-Sicht. Reine Oberflaeche – **keine** Wirkung auf Dienst oder Erkennung. | BDE-Seite `BdeTeileliste` (Reiter-Vorgabe), Redirect aus `BdeTerminal/Index` | IDEAL (Auswahl nur bei Master `ProduktionsauftragHierarchisch` sichtbar; an AKE immer FA) | – (keine Aussenwirkung) |
| `ProductionWorkplace.BdeTeileliste` („BDE-Teileliste (vereinfacht)“) – **unveraendert** (Antwort 3a) | Schalter (bit) | unveraendert, Validierung `ValidateTeilelisteAsync` | 0 | Unveraendert: „Blechteile per Teilmeldung“ – die Struktur-Erkennung legt fuer Blatt-Tokens dieses Kuerzels keinen Vater-AG an (`WorkOperationStructureDetectionService.cs:96`). Neu im Web nur: steuert das Hinweis-Banner im Teile-Reiter (E4-7); ist **nicht** mehr Voraussetzung fuer die Teile-Sicht. | Dienst-Lauf Arbeitsgang-Strukturerkennung, Banner | IDEAL | unveraendert (bestehender Hilfetext) |
| `BdeTerminalMaxHauptFa` (AppSetting) – **entfaellt** in Etappe 5 (E5-3; Antworten 1 + 14) | Zahl | – | – | Kein Leser mehr nach Abloesung der alten Terminal-Seite; Schluessel und Katalogeintrag werden entfernt, DB-Zeile erscheint als verwaist in `/Settings`. | – | beide | – |
| `BdeNurFaMeldung` (AppSetting) – **unveraendert**, von der neuen Sicht nicht gelesen (Antwort 2) | – | – | `false` | Wirkt nur noch in den Alt-Zweigen (`BdeApiController`, `BdeBookingService`, `StartProductionForOrder`); Entfernen in [[2026-11-25-bde-nur-fa-meldung-entfernen]]. Katalog-Beschreibung (`AppSettingDefinitions.cs:221`) in Etappe 5 um „wird von der BDE-Seite nicht beachtet, Altlast“ ergaenzen. | – | beide | – |

Sonst keine neuen Einstellungen. Die Detailangaben-Umschaltung im Kopf (E4-4) ist eine Bedienvorliebe im Browser, keine Einstellung.

## Audit-Feld-Auswirkungen

- `BdeTeilMeldung` (AuditableEntity): Anlage setzt `Created*` wie heute (`BdeTeilelisteService.cs:346-352`), Ruecknahme/Storno `SetModified`
  (`:406-436`) – gilt je Meldung, auch fuer Teilmeldungen. Neue Spalte ohne eigene Audit-Wirkung.
- `ProductionWorkplace`: neues Feld `BdeStandardsichtTeile` in die Feldliste `EditableProperties` (`ProductionWorkplaceRepository.cs:13-20`), die
  `Modified*` bereits enthaelt – Aenderung in der Maske setzt die Audit-Felder wie bisher. Die Vorbelegung in SQL/168 setzt keine `Modified*`
  (Schema-Skript, einmalig bei Spaltenanlage; wie `SQL/154`).
- `UserViewPreference` (gemerkte Sicht/Filter): bestehendes `SaveAsync` mit Anzeige- und Windows-Name (`BdeTeilelisteController.cs:126`).
- Buchungen (`BdeBooking`, Mengen) nur ueber `IBdeBookingService` – dessen Audit unveraendert.

**Rollen / Zugriff:** unveraendert. Seite `[RequireBdeActive][RequireBdeUserAccess]` (`BdeTeilelisteController.cs:17-18`), Terminal-JSON-Actions gleiche
Filter, `api/bde` `[RequireBdeUserAccess]`, Admin-Storno `[RequireBdeAdminAccess]` (`BdeBookingsController.cs:312`). Neuer Menuepunkt mit
derselben Regel wie „Terminal“ (Filter-Kongruenz in `NavigationSichtTests`). Keine neue Rolle → keine Aenderung an `controller.md`/`RoleOverview`.

**Handy-Konten (Antwort 6, Betriebsaufgabe, kein Code):** Android-Handys, keine Windows-Konten. Je Geraet ein lokales WMS-Konto mit Anmeldung ueber
das Formular (`AccountController.Login`, `AccountController.cs:65,78`) und BDE-Benutzer-Recht, dazu eine Terminal-Zuordnung (ohne Terminal weist die
Seite ab, `BdeTeilelisteController.cs:87-91`). Konten sind noch nicht angelegt. Der Dev-Lauf beschreibt das in `docs/betrieb/09-sicherheit-konten-rechte.md`
(Abschnitt „BDE am Handy“); im Deploy steht ein Info-Eintrag.

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
7. Unter 992 px (Antwort 8): Filterkarte eingeklappt mit Kurzfassung, Kartenansicht, kein waagrechtes Scrollen bei 360 px Breite; in der
   Kartenansicht sind Spaltenfilter nicht bedienbar, Zaehler und „Zuruecksetzen“ sind sichtbar (Antwort 10).
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
22. SQL/168 laeuft zweimal ohne Fehler; FreshInstall erzeugt beide Spalten und den Index identisch. Nach dem ersten Lauf steht
    `BdeStandardsichtTeile = 1` genau an den Werkbaenken mit BDE aktiv + `BdeTeileliste` + Kuerzel, sonst 0; der zweite Lauf aendert keine Zeile.

**Etappe 4**
23. Reiter FA zeigt die offenen Arbeitsgaenge der Kuerzel-Familie in derselben Reihenfolge wie `available-operations`; fremd laufende mit „läuft“, eigene laufende ohne Start-Knopf.
24. Anstempeln je Zeile startet Ruesten bzw. Produktion; bei `BdeNurAgFertigmeldung` steht dort „Fertig melden“.
25. Laufende und pausierte Buchungen erscheinen im Kopf; Teilfertig/Pause/Fertig/Fortsetzen wirken und laden neu.
26. Kollisions-, Gruppen-Fertig- und Close-Others-Fall fuehren zum jeweiligen Dialog wie im Terminal (je ein Testfall).
27. Hardware-Scan einer FA-Nr markiert die Zeile; mehrdeutiger Scan oeffnet die Auswahl; unbekannte Nr gibt eine Meldung.
28. Werkbank-Maske (IDEAL) zeigt den Schalter „BDE-Teileliste (vereinfacht)“ unveraendert **und** daneben „BDE-Standardsicht“ als Auswahl Teile/FA;
    „Teile“ ohne Kuerzel bzw. ohne BDE aktiv wird beim Speichern abgewiesen, nichts gespeichert; ein gueltiger Wechsel setzt `Modified*`.
    An AKE (Master aus) ist die Auswahl nicht sichtbar; ein manipulierter POST mit „Teile“ wird abgewiesen.
29. Ohne gespeicherte Sicht oeffnet die Standardsicht (`BdeStandardsichtTeile`); nach Reiterwechsel oeffnet beim naechsten Aufruf die zuletzt gewaehlte.
    Eine gespeicherte Teile-Sicht an einer Werkbank ohne verfuegbare Teile-Sicht oeffnet FA.
30. Master an, Werkbank ohne Kuerzel: Teile-Reiter deaktiviert mit Hinweistext. Master aus: keine Teile-Reiter, nur FA.
31. Baugruppen-Teilmeldung aus dem Teile-Reiter einer IDEAL-Werkbank mit Kuerzel, aber `BdeTeileliste` aus und Standardsicht FA, wird gebucht
    (Gate `TeileSichtVerfuegbar`, nicht mehr `BdeTeileliste`).
32. Teile-Reiter an einer Werkbank, deren Kuerzel an keiner BDE-aktiven Werkbank `BdeTeileliste` traegt: Hinweis-Banner zur Erkennung sichtbar;
    mit `BdeTeileliste` an einer Familien-Werkbank: kein Banner.
32a. Aendern von `BdeStandardsichtTeile` aendert nichts an der Struktur-Erkennung (Dienst-Lauf legt dieselben Vater-AGs an wie vorher).
32b. AKE-DEV (Master aus, Werkbank ohne Kuerzel, BDE aktiv): die Seite oeffnet die FA-Sicht mit den offenen Arbeitsgaengen dieser Werkbank;
     Anstempeln, Kopf-Buchungen und Fertig wirken wie an IDEAL.
32c. Die Seite liest `BdeNurFaMeldung` nicht: mit `BdeNurFaMeldung = true` auf DEV zeigt die FA-Sicht weiter Arbeitsgaenge (keine NurFA-Oberflaeche).
32d. Werkbank nicht BDE-aktiv bzw. unbekannte `workplaceId`: Hinweis „Werkbank ist nicht BDE-aktiv“ mit Werkbank-Auswahl, **kein** Redirect-Kreis
     zwischen `BdeTerminal/Index` und `BdeTeileliste/Index`.

**Etappe 5**
33. Menuepunkt „Tageshistorie“ sichtbar genau wenn „Terminal“ sichtbar ist; Knopf im Kopf oeffnet sie fuer den angemeldeten Werker.
34. Tageshistorie listet Buchungen und Teilmeldungen des Tages, paginiert, Spaltenfilter wirkt serverseitig.
35. ~~`BdeTerminal/Index` an IDEAL leitet um; an AKE bzw. mit NurFA erscheint die alte Seite unveraendert.~~ *(ueberholt, Antwort 1)*
    `BdeTerminal/Index` leitet an IDEAL- **und** AKE-DEV auf die neue Seite um (Werkbank aus Query bzw. Terminal-Vorgabe); ohne Terminal kommt der
    bisherige Hinweis. `Views/BdeTerminal/Index.cshtml` und `bde-terminal.js` sind entfernt; die JSON-Actions des Terminals funktionieren weiter.
35a. `BdeTerminalMaxHauptFa` ist aus dem Katalog entfernt; eine vorhandene DB-Zeile erscheint in `/Settings` als verwaist.
35b. Anmeldung am Android-Handy mit lokalem WMS-Konto (Formular-Login) und zugeordnetem Terminal oeffnet die Seite; `docs/betrieb/09-…` beschreibt das.

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
- **TS-194 FA-Sicht** – IDEAL-DEV: Anstempeln, Kopf-Buchungen, Pause/Fortsetzen, Teilfertig, Fertig mit Gruppenpflicht, Close-Others, Kollision,
  Taetigkeit, Scan (exakt/mehrdeutig/unbekannt), neue Auswahl „BDE-Standardsicht“ neben dem unveraenderten Schalter „BDE-Teileliste“, gemerkte Sicht,
  Werkbank ohne Kuerzel (Teile-Reiter deaktiviert), Werkbank mit Kuerzel aber `BdeTeileliste` aus (Teile-Reiter mit Banner, Baugruppen-Teilmeldung
  wird gebucht), Pruefung nach SQL/168: vorbelegte Standardsicht an den bisherigen Teileliste-Werkbaenken. AKE-DEV (Worktree-Stand, nie main):
  BDE an einer Werkbank aktivieren, FA-Sicht oeffnen, keine Teile-Reiter, Anstempeln/Fertig. Negativ: „Teile“ ohne Kuerzel speichern; manipulierter
  POST „Teile“ an AKE; `workplaceId` einer nicht BDE-aktiven Werkbank (kein Redirect-Kreis); `BdeNurFaMeldung = true` auf DEV setzen → FA-Sicht
  unveraendert, danach zuruecksetzen.
- **TS-195 Tageshistorie und Abloesung** – Menue, Knopf, Inhalt (Buchungen + Teilmeldungen), Pagination; Aufruf `BdeTerminal/Index` an IDEAL- **und**
  AKE-DEV → Redirect auf die neue Seite; `/Settings` zeigt `BdeTerminalMaxHauptFa` nur noch als verwaist; Anmeldung am Android-Handy mit lokalem
  WMS-Konto + Terminal (sobald ein Konto angelegt ist, sonst als offen vermerken). Negativ: Benutzer ohne Terminal → Hinweis wie heute.

`secondbrain/tests/testszenarien-index.md` je Etappe nachziehen.

## Etappen (nur bei epic: true)

Ein Worktree (`.claude/worktrees/2026-08-07-ideal-teile-1-5`), je Etappe ein Commit, **ein Merge am Ende**. Branch regelmaessig mit
`scripts/sync-worktree.ps1` auf main-Stand halten. Versionen und TS-Nummern vorlaeufig (letzte Version 1.99.69), der Dev-Lauf prueft neu.
Jede Etappe: `frontend-design` explizit aufrufen, Kontrast messen, Ampel nie nur Farbe, Anwender-Changelog + Hilfe, Testszenario.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | Teile Einzeln: AG-Status (Status-Ladung und Symbol-Partial aus FA-Struktur herausgezogen), Kinder als Zeilen, Filter auf Kinder, Teile-Knopf vorn, BDE mobil kompakt (992 px, 44 px, Spaltenfilter in Karten nicht bedienbar – Antworten 8–10). v1.99.70, TS-191, keine Migration. | offen | |
| 2 | Teile Summiert: Einzelauftraege aufklappbar, Summen-Haken, aggregierte Ampel ueber `Ableiten` (Antwort 11). v1.99.71, TS-192, keine Migration. | offen | |
| 3 | Teilfertigmeldung: Migration + SQL/168 + FreshInstall, Service/Controller/Dialog, Ruecknahme, Waisen, Baugruppe `istFinal`, Admin-Spalte Art, ADR 0036. **Dieselbe Migration legt `ProductionWorkplaces.BdeStandardsichtTeile` an und belegt sie vor** (Antwort 3a; Model-Property ohne Leser bis Etappe 4 – Reihenfolge geprueft, siehe Etappe-3-Entwurf). Index nach Antwort 12: wer zweitens baut, vereinigt den Filter mit SQL/164 – der Dev-Lauf prueft beim Start, ob SQL/164 schon im Branch liegt. v1.99.72, TS-193. | offen | |
| 4 | FA-Sicht an beiden Standorten: Wirksamkeits-Helfer ohne NurFA (`SeiteVerfuegbar`, `TeileSichtVerfuegbar` mit Master-Gate, `TeileStandard` ueber `BdeStandardsichtTeile`), `TeilelisteWirksam` entfernt, `BdeBookingService` mit Master-Lesung, Zeilenquelle als gemeinsame Service-Methode, View `Fa`, AG-Status aller AG der Sub-FA (Antwort 4), Kopf-Buchungen, Modal-Partials, Scan, Standardsicht-Auswahl neben `BdeTeileliste`, Banner, gemerkte Sicht, Pagination statt MaxHauptFa (Antwort 14). v1.99.73, TS-194, keine weitere Migration. | offen | |
| 5 | Tageshistorie inkl. Teilmeldungen (Seite, Knopf, NavZiel; Antwort 5), `BdeTerminal/Index` leitet an beiden Standorten immer um, alte View + `bde-terminal.js` geloescht, `BdeTerminalMaxHauptFa` entfernt, Katalogtext `BdeNurFaMeldung`, Betriebsdoku Handy-Konten (Antworten 1, 2, 6). v1.99.74, TS-195, keine Migration. | offen | |

## Deploy

> [!warning] Vorlaeufig – der qa-agent bestaetigt gegen den echten Diff.

- **Web-App:** ja (alle Etappen).
- **Service:** kein Dienst-Code (Antwort 3a: Erkennung bleibt an `BdeTeileliste`); Versionsnummer in `AppVersion.cs` (beide). Weil der Dienst das
  EF-Modell des Web-Projekts teilt (`IDEALAKEWMSService.csproj:33`), kennt der neue Dienst-Build die Spalte `BdeStandardsichtTeile` → neue
  Dienst-Binaries **erst nach** SQL/168 starten (Regelreihenfolge des Update-Skripts: Schema vor Start).
- **Migration:** ja (Etappe 3, SQL/168, zwei Spalten + Index + Vorbelegung). Reihenfolge: SQL vor Web- und Dienst-Start. Der laufende alte Dienst
  stoert nicht (er kennt die neuen Spalten nicht; `BdeTeilMeldung` liest er nirgends – grep 2026-11-25 ohne Treffer).
- **Manifest-Eintraege** (`scripts/deploy/nachlauf.psd1`):

| Id | Version | Typ | Firma | Phase | Ausfuehrung | Text |
|---|---|---|---|---|---|---|
| `v1.99.70-BdeTeilelisteAgStatusMobilInfo` | 1.99.70 | Info | IDEAL | Nachher | – | „BDE-Teileliste: neue Spalte AG-Status, Baugruppen-Teile als Zeilen, kompakte Ansicht am Handy. Werker kurz informieren.“ (ersetzt den Info-Eintrag `v1.99.44-BdeTeilelisteMobilInfo` der ueberholten Mobil-Spec) |
| `v1.99.71-BdeTeilelisteSummiertInfo` | 1.99.71 | Info | IDEAL | Nachher | – | „Summiert: Auftraege aufklappbar, Haken auf Summenzeile meldet alle darunter fertig.“ |
| `v1.99.72-SQL168` | 1.99.72 | Schema | beide | Schema | automatisch | `SQL/168_AddBdeTeilMeldungIstAbschliessendUndStandardsicht.sql` – Spalte `IstAbschliessend` + gefilterter UNIQUE neu, Spalte `ProductionWorkplaces.BdeStandardsichtTeile` mit Vorbelegung bei Erstanlage; Reihenfolge nach SQL/164, falls diese im selben Paket ist (Antwort 12) |
| `v1.99.72-BdeTeilfertigInfo` | 1.99.72 | Info | IDEAL | Nachher | – | „Teilfertig/Fertig melden in der Teileliste; Teilfertig laesst die Zeile offen.“ |
| `Pruefung/BdeNurFaMeldungAn` | 1.99.73 | Pruefung | beide | Vorher | anzeigen | `SELECT COUNT(*) FROM dbo.AppSettings WHERE [Key] = N'BdeNurFaMeldung' AND LOWER(LTRIM(RTRIM([Value]))) IN (N'true', N'1', N'on')` – Treffer > 0: „BdeNurFaMeldung ist an; die neue BDE-Seite beachtet die Einstellung nicht (Antwort 2) – vor dem Update mit Gerald klaeren.“ |
| `Pruefung/BdeStandardsichtOhneTeileSicht` | 1.99.73 | Pruefung | IDEAL | Nachher | anzeigen | `SELECT COUNT(*) FROM dbo.ProductionWorkplaces WHERE BdeStandardsichtTeile = 1 AND (BdeAktiv = 0 OR ISNULL(LTRIM(RTRIM(ArbeitsschrittCode)), N'') = N'')` – Treffer > 0: „Werkbaenke mit Standardsicht Teile ohne BDE aktiv bzw. ohne Kuerzel – oeffnen FA; in den Werkbank-Stammdaten korrigieren.“ |
| `v1.99.73-BdeStandardsichtEinstellung` | 1.99.73 | Einstellung | IDEAL | Nachher | – | „Werkbank-Stammdaten: neue Auswahl BDE-Standardsicht (Teile / FA) je BDE-Werkbank pruefen; vorbelegt mit Teile, wo bisher die Teileliste wirkte. Der Schalter BDE-Teileliste bleibt unveraendert (steuert die Arbeitsgang-Erkennung).“ |
| `v1.99.73-BdeFaSichtAkeInfo` | 1.99.73 | Info | AKE | Nachher | – | „BDE-Seite mit FA-Sicht gilt auch an AKE (derzeit ohne BDE-Nutzung); keine Aktion noetig.“ |
| `v1.99.74-BdeTerminalAbloesungInfo` | 1.99.74 | Info | beide | Nachher | – | „BDE-Terminal oeffnet an beiden Standorten die neue Seite mit Reitern; alte Terminal-Seite entfernt; Tageshistorie im Menue. Einstellung BdeTerminalMaxHauptFa entfaellt (in /Settings als verwaist sichtbar).“ |
| `v1.99.74-BdeHandyKontenInfo` | 1.99.74 | Info | beide | Nachher | – | „BDE am Android-Handy: je Geraet lokales WMS-Konto (Formular-Login) + Terminal-Zuordnung anlegen (docs/betrieb/09). Konten sind noch nicht angelegt.“ |

  SQL/168 ist ein Schema-Skript (kein Trockenlauf/Transaktion noetig; die Vorbelegung laeuft nur im Erstanlage-Zweig der Spalte). Keine Daten-/Einmal-Skripte.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService` (nur Versionsnummer + geteiltes Modell)

## Offene Rueckfragen

> [!success] Alle 14 beantwortet (Gerald 2026-11-25), siehe „Freigabe-Antworten“; im Rumpf eingearbeitet. Keine neuen offenen Rueckfragen.
> Die Fragen bleiben unten als Bezug der Antworten stehen; ihre Empfehlungen zu 1, 2, 3 und 6 sind durch die Antworten **ueberholt**.

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
7. FA-Struktur ohne Teilmeldungs-Status (eigene Notiz) → **Ja, Empfehlung uebernommen (Gerald 2026-11-25)**
8. Schwelle 992 px → **Ja, Empfehlung uebernommen (Gerald 2026-11-25)**
9. Filter 44 px ueberall → **Ja, Empfehlung uebernommen (Gerald 2026-11-25)**
10. Spaltenfilter in Kartenansicht nicht bedienbar → **Ja, Empfehlung uebernommen (Gerald 2026-11-25)**
11. Aggregation ueber Ableiten (LaeuftUeberfaellig) → **Ja, Empfehlung uebernommen (Gerald 2026-11-25)**
12. SQL/164-Koordination: Zweiter vereinigt die Filter → **Ja, Empfehlung uebernommen (Gerald 2026-11-25)**
13. Master-Gate fuer Teile-Sicht → **Ja, Empfehlung uebernommen (Gerald 2026-11-25)**
14. Pagination statt BdeTerminalMaxHauptFa → **Ja, Empfehlung uebernommen (Gerald 2026-11-25)**
