---
type: spec
title: BDE-Masken vereinheitlichen - Teile- und FA-Sicht, AG-Status-Ampel, Teilfertigmeldung, BDE mobil kompakt
slug: 2026-11-25-bde-masken-vereinheitlichung-spec
status: InUmsetzung
created: 2026-11-25
updated: 2026-11-25
source_backlog: "[[2026-11-25-bde-masken-vereinheitlichung]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
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
  - docs/anwender/README.md
  - docs/anwender/11-bde-terminal.md
  - docs/anwender/17-bde-teileliste-werkbank.md
  - docs/anwender/bilder/11-*.jpg
  - docs/anwender/bilder/17-*.jpg
open_questions: []
epic: true
etappen:
  - "1 Teile Einzeln: AG-Status, Baugruppen-Kinder als Zeilen, Filter auf Kinder, Teile-Knopf vorn, BDE mobil kompakt (v1.99.70, TS-191, keine Migration)"
  - "2 Teile Summiert: Einzelauftraege aufklappbar, Summen-Haken, aggregierte Ampel (v1.99.71, TS-192, keine Migration)"
  - "3 Teilfertigmeldung mit Restmenge, ADR 0036, Baugruppe als Teil-Buchung; SQL/168 legt zusaetzlich die Spalte BdeStandardsichtTeile an (ohne Leser bis Etappe 4a) (v1.99.72, TS-193, SQL/168)"
  - "4a Wirksamkeits-Helfer, Feld/Maske BDE-Standardsicht, Reiter, FA-Tabelle + Anstempeln je Zeile, FA-Sicht auch an AKE (v1.99.73, TS-194, keine weitere Migration)"
  - "4b FA-Sicht Kopf-Buchungen, Modals (Mengen/Kollision/Gruppe/Close-Others), Scan, Taetigkeit, NurAg-Fertig je Zeile (v1.99.74, TS-195, keine Migration)"
  - "5 Tageshistorie eigene Seite + Knopf, alte Terminal-Seite an beiden Standorten abgeloest und entfernt (v1.99.75, TS-196, keine Migration)"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: "Antworten 1-22 (Gerald 2026-11-25); Bauweg A; Teile-Reiter nur mit BdeTeileliste; Etappe 4 als 4a/4b"
freigabe_von: Gerald
freigabe_am: 2026-11-25
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

> [!note] Ueberarbeitet 2026-11-25 (Rework 2, Antworten 15–17 zur Kritischen Pruefung)
> **Antwort 15 (B-1):** Teile-Reiter nur an Werkbaenken mit `BdeTeileliste` (ein Meldeweg je Teil); `BdeStandardsichtTeile` ist nur der Startreiter;
> Banner und AK 32 entfallen, AK 31 wird zum Negativfall (Melden ohne `BdeTeileliste` abgewiesen); Gate `BdeBookingService.cs:288` bleibt inhaltlich wie heute, **kein** `IServiceSettingRepository` im `BdeBookingService`.
> **Antwort 16 (S-1):** Defaults der beiden neuen bit-Spalten nur in Migration/SQL/168/FreshInstall, kein `HasDefaultValue` im EF-Modell, neues AK 18a.
> **Antwort 17 (S-3):** `docs/anwender/` (Kapitel 11 und 17, Index `README.md`) mit Playwright-Fotos gegen DEV je Etappe mit Oberflaechen-Aenderung.
> ~~S-2, S-4, S-5, S-6 der Pruefung sind **nicht** entschieden und nicht eingearbeitet.~~ *(erledigt, Rework 3)*

> [!note] Ueberarbeitet 2026-11-25 (Rework 3, Antworten 18–22)
> **Antwort 18 (S-2):** Uebermeldung bei „Teilfertig“ erlaubt, Zeile bleibt offen, Hinweis „Sollmenge erreicht – bitte Fertig melden“ (E3-1, AK 15a, TS-193).
> **Antwort 19 (S-4):** Etappe 4 geteilt in **4a** (Helfer/Feld/Maske, Reiter, FA-Tabelle + Anstempeln je Zeile) und **4b** (Kopf-Buchungen, Modals, Scan,
> Taetigkeit), je eigener Commit; Versionen/TS ab 4b um eins verschoben (4b v1.99.74/TS-195, Etappe 5 v1.99.75/TS-196 – vorlaeufig).
> **Antwort 20 (S-5):** AK 26 je Fall ausformuliert (ohne Vergleich mit der alten Seite).
> **Antwort 21 (S-6):** In Etappe 1–2 zaehlt jede nicht stornierte Teilmeldung als abschliessend (E1-1, AK 2).
> **Antwort 22:** NurFA-Alt-Zweige `BdeBookingService.cs:522,543` bleiben – bewusste Restabweichung zu G5, vermerkt bei G5 und AK 32c; kein Code.

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
  **neues** Werkbank-Feld `BdeStandardsichtTeile` als Auswahl „BDE-Standardsicht: Teile / FA“ (nur Oberflaeche, nur Startreiter); `BdeTeileliste` bleibt
  unveraendert und schaltet zusaetzlich die Teile-Reiter frei (Antwort 15). Zuletzt gewaehlte Sicht je Werker+Werkbank.
- Anwender-Doku `docs/anwender/` (Kapitel 11 und 17, Stand-Version im Index) mit Fotos per Playwright gegen DEV je Etappe mit Oberflaechen-Aenderung (Antwort 17).
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

- **G1** Eine Seite `BdeTeileliste` mit Reitern *Teile Einzeln · Teile Summiert · FA* (FA ab Etappe 4a). Gleicher `_Kopf`, Zahnrad, Server-Spaltenfilter, Pagination.
  Ab Etappe 4a an beiden Standorten; die Teile-Reiter nur, wo die Teile-Sicht verfuegbar ist (~~Master an, BDE aktiv, Kuerzel~~ *(ueberholt, Antwort 15)*
  Master an, BDE aktiv, Kuerzel **und** `BdeTeileliste` – E4-7).
- **G5** Die Seite liest `BdeNurFaMeldung` nicht (Antwort 2); die neuen Helfer haben keinen NurFA-Parameter.
  **Bewusste Restabweichung (Antwort 22):** Der Buchungsdienst liest die Einstellung weiter in `EnsureStartAllowedAsync` (`BdeBookingService.cs:522`,
  `NurAgFertigmeldungWirksam(..., BdeNurFaMeldung)`) und `EnsureWorkOperationInFamilyAsync` (`:543`, NurFA prueft gegen die FA-Werkbank statt die
  Kuerzel-Familie). Steht `BdeNurFaMeldung` an, weicht der Server daher von der FA-Sicht ab: `:543` lehnt einen Start ab, wenn der FA nicht an der
  Terminal-Werkbank haengt, auch wenn der AG zur Kuerzel-Familie gehoert („Dieser Arbeitsgang gehört nicht zu dieser Werkbank.“); `:522` laesst an
  `BdeNurAgFertigmeldung`-Werkbaenken einen Start zu, weil `NurAgFertigmeldungWirksam` bei `nurFaGlobal = true` falsch ist
  (`WerkbankKuerzelFamilie.cs:46-47`) – die FA-Sicht bietet dort aber nur „Fertig melden“ an. Das bleibt so (kein Code im Epic);
  bereinigt mit [[2026-11-25-bde-nur-fa-meldung-entfernen]], abgesichert durch die Manifest-Pruefung `Pruefung/BdeNurFaMeldungAn` (produktiv `false`).
- **G2** AG-Status immer **Farbe + Zeichen + Text** (Haken / Kreis / Dreieck / Sanduhr aus `FaAgStatusDarstellung`, `FaArbeitsgangStatus.cs:100`),
  Name und Status in `aria-label`/`title`, fokussierbar – genau wie `Views/FaHierarchy/_FaHierarchyNode.cshtml:97-115`. Nie Farbe allein.
- **G3** Status nur ueber `ArbeitsgangStatusRegel` (ein Ort, fallstricke „Arbeitsgang-Status nur über ArbeitsgangStatusRegel“).
- **G4** Kontrast WCAG AA (4,5:1 Text, 3:1 Bedienelemente), Touch-Ziele ≥ 44 px, Skill `frontend-design` in jeder Etappe mit View-/CSS-/JS-Aenderung **explizit aufrufen**.

### Etappe 1 – Teile Einzeln + BDE mobil kompakt

- **E1-1** Neue Spalte **AG-Status** (`data-col-key="ag-status"`): je Token der Spalte Arbeitsschritte ein Symbol mit Kuerzel.
  Statusquelle je Token (ODER-Verknuepfung, dann `ArbeitsgangStatusRegel.Ableiten`):
  - erledigt = WorkOperation des Tokens erledigt (Zuordnung wie `FaArbeitsgangStatus.Zuordnen`, `FaArbeitsgangStatus.cs:32-95`) **oder**
    nicht stornierte abschliessende Teilmeldung mit diesem Kuerzel und Schluessel. **In Etappe 1–2 zaehlt jede nicht stornierte Teilmeldung als
    abschliessend** (Antwort 21; heutiges Verhalten, `BdeTeilelisteService.cs:47` – die Spalte `IstAbschliessend` entsteht erst in Etappe 3); ab Etappe 3
    nur die mit `IstAbschliessend = 1`;
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
  Der kompakte Kopf ist der Kopf aller Reiter, also auch der FA-Sicht (Etappe 4a/4b).

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
  **Uebermeldung (Antwort 18):** „Teilfertig“ mit Gut ≥ Restmenge (Summe Gut danach ≥ Soll) ist erlaubt (Mehrmenge kommt vor); die Meldung wird
  gespeichert, die Zeile bleibt offen (nicht abschliessend), und das Ergebnis zeigt zusaetzlich zur Erfolgsmeldung den sichtbaren Hinweis
  „Sollmenge erreicht – bitte Fertig melden“ (`TempData["WarningMessage"]`). Die Zeile zeigt dann z. B. „gemeldet 12 von 10“; die Fertig-Vorgabe ist 0 (Rest mindestens 0).
- **E3-2** Zeile zeigt „gemeldet X von Soll“ (Summe Gut der nicht stornierten Meldungen). Meldbar = keine abschliessende Meldung, eindeutig, nicht verschwunden.
- **E3-3** „Auswahl fertigmelden“ (Block, `Index.cshtml:12-22`) und Summen-Haken melden **abschliessend** mit Restmenge.
- **E3-4** Filter „gemeldete zeigen“ aus = nur abgeschlossene Zeilen ausblenden (heute jede Zeile mit Meldung, `BdeTeilelisteService.cs:187`).
- **E3-5** Ruecknahme je Meldung (eigene, bis `RuecknahmeFristAsync`, `BdeTeilelisteService.cs:398-399`); die Statuszelle listet die eigenen Meldungen
  der Zeile einzeln mit „Zurücknehmen“. Ruecknahme der abschliessenden Meldung oeffnet die Zeile wieder. Admin-Storno unveraendert mit Grund.
- **E3-6** Baugruppe: „Teilfertig“ erzeugt eine **Teil-Buchung** am Sub-FA-Arbeitsgang (Menge mit `IsFinal = false`), „Fertig“ wie heute die
  Fertig-Buchung (`IsFinal = true`). Ruecknahme storniert die verknuepfte Buchung wie heute.
- **E3-7** Waisen-Hinweis zaehlt **Schluessel**, nicht Meldungen.
- **E3-8** Admin-Liste Teilmeldungen (`BdeBookingsController.cs:267-309`) bekommt die Spalte **Art** (Teil / Fertig), filterbar.

### Etappe 4 – FA-Sicht (geteilt in 4a und 4b, Antwort 19)

Zuordnung: **4a** = E4-1, E4-2, E4-3 (nur Anstempeln Ruesten/Produktion), E4-7 und die Wirksamkeits-Helfer samt Aufrufer-Umstellung;
**4b** = E4-3 (NurAg-„Fertig melden“ mit Mengen-Dialog), E4-4, E4-5, E4-6. Jede Teil-Etappe hat einen eigenen Commit und baut/testet gruen fuer sich.
Zwischenstand nach 4a (bleibt nur bis 4b im Zweig, ein Merge am Ende): Ergebnisse ohne eigenen Dialog (Kollision, `InvalidState`) erscheinen als
sichtbare Server-Meldung nach Reload, nichts still verschluckt; an Werkbaenken mit `BdeNurAgFertigmeldung` hat die Zeile in 4a noch keinen Knopf
(Hinweis „Fertig melden folgt“ ist nicht noetig, weil 4a nicht einzeln deployt wird). Die Mengen-, Kollisions-, Gruppen- und Close-Others-Modals kommen mit 4b.

- **E4-1** *(4a)* Reiter **FA**: eine Zeile = **offener Arbeitsgang der Kuerzel-Familie dieser Werkbank** (Quelle und Reihenfolge wie heute
  `available-operations`, `BdeApiController.cs:204-327`, Sortierung `BdeTerminalAuftraege.Ordnen`). Fremd laufende Zeilen bleiben sichtbar mit
  Status „läuft“ (fallstricke „Terminal zeigt fremd gebuchte Arbeitsgänge“); eigene laufende Zeilen zeigen „läuft bei Ihnen“ ohne Start-Knopf.
  An AKE (Werkbank ohne Kuerzel) ist die Familie nur die Werkbank selbst (`WerkbankKuerzelFamilie.cs:26`), die Kopfdaten-Abfrage liefert dort
  nichts (`BdeApiController.cs:290`, „AKE leer“) – Spalten aus der FA-Struktur (Kunde, Montage-Abteilung, FE-Termin aus Kopfzeile) bleiben leer.
- **E4-2** *(4a)* Spalten (Zahnrad, `defaultHidden` wo genannt): Haupt-FA, Sub-FA, Arbeitsgang (Kuerzel + Name), **AG-Status** (Antwort 4: alle AG der
  Sub-FA, der Arbeitsgang der Zeile markiert), Bezeichnung, Artikelnummer, Matchcode, Sollmenge, FE-Termin, Soll-Termin AG, Prio, Kunde, Montage-Abteilung;
  defaultHidden: Material, Arbeitsschritte, Arbeitsbereich, Bemerkung PN, Fertigungsinfo PN, Sage-Status, Vater-FA, Position.
- **E4-3** Aktion je Zeile: *(4a)* **Anstempeln** = „Rüsten“ / „Produktion“ (heutige Start-Knoepfe `bde-terminal.js:355-379`); *(4b)* bei
  `BdeNurAgFertigmeldung` stattdessen „Fertig melden“ (Mengen-Dialog, Vorgabe Sollmenge). Die FA-Sicht ermittelt das ueber
  `NurAgFertigmeldungWirksam(w, nurFaGlobal: false)` (G5).
- **E4-4** *(4b)* Kopf: Karten „1. Operator“ / „2. Arbeitsgang wählen“ entfallen. Werker-Leiste wie Teileliste; darin **laufende und pausierte Buchungen
  des Werkers als kompakte Zeilen** mit Teilfertig / Pause / Fertig bzw. Fortsetzen. Zeit-/Detailangaben (Startzeit, Laufzeit, Personal, Terminal)
  per Schalter ein-/ausblendbar, gemerkt (localStorage je Benutzer, rein darstellend).
- **E4-5** *(4b)* Scanfeld FA-Nr als schmale Zeile ueber der Tabelle (`data-hw-scan-ziel`, Hardware-Scanner), Aufloesung ueber `/api/bde/resolve-scan`.
- **E4-6** *(4b)* Alle Terminal-Ablaeufe aus der Ablaufliste unten bleiben erhalten (Kollision, Gruppen-Fertigmeldung, Close-Others, ungeplante Taetigkeit, Mengen-Dialog, Doppelklick-Sperre).
- **E4-7** *(4a)* Standardsicht je Werkbank (Antwort 3 = Variante a): **neues** Feld `ProductionWorkplace.BdeStandardsichtTeile` (bit), in der Werkbank-Maske
  Auswahl „BDE-Standardsicht: Teile / FA“ – nur Oberflaeche. `BdeTeileliste` („Blechteile per Teilmeldung“) bleibt **unveraendert** in Bedeutung,
  Maske und Wirkung auf den Dienst (Erkennung V1).
  - ~~Teile-Sicht verfuegbar = Master `ProduktionsauftragHierarchisch` an **und** Werkbank BDE aktiv **und** Kuerzel gesetzt (Antworten 3, 13).
    `BdeTeileliste` ist dafuer **keine** Voraussetzung.~~ *(ueberholt, Antwort 15)*
  - **Teile-Sicht verfuegbar** = Master `ProduktionsauftragHierarchisch` an **und** Werkbank BDE aktiv **und** Kuerzel gesetzt **und** `BdeTeileliste` an
    (Antworten 13, 15). Damit gibt es je Teil genau einen Meldeweg: Teilmeldung nur, wo die Erkennung V1 fuer das Kuerzel keinen Vater-AG anlegt.
  - `BdeStandardsichtTeile` ist **nur der Startreiter** (Antwort 15); „Teile“ ist nur speicherbar, wenn die Teile-Sicht verfuegbar ist (Validierung s. Werkbank-Maske).
  - Master aus (AKE): Teile-Reiter werden nicht angezeigt, nur FA-Sicht.
  - ~~Master an, Werkbank ohne Kuerzel: Teile-Reiter deaktiviert (`aria-disabled`, Hinweis „Diese Werkbank hat kein Arbeitsschritt-Kürzel – nur FA-Sicht“).~~
    *(ueberholt, Antwort 15)*
  - Master an, Teile-Sicht nicht verfuegbar (kein Kuerzel **oder** `BdeTeileliste` aus): Teile-Reiter deaktiviert (`aria-disabled`, sichtbarer Hinweis
    „Teile-Sicht nur an Werkbänken mit „BDE-Teileliste (vereinfacht)“ und Arbeitsschritt-Kürzel – hier nur FA-Sicht“). Eine Werkbank mit Kuerzel,
    aber `BdeTeileliste` aus, ist also **nur FA**.
  - Standard beim Aufruf: zuletzt gewaehlte Sicht je Werker+Werkbank (vorhandener `GespeicherterFilter.Sicht`, `BdeTeilelisteController.cs:44,58`);
    ohne gespeicherten Wert Teile, wenn `BdeStandardsichtTeile` und Teile-Sicht verfuegbar, sonst FA. Eine gespeicherte Teile-Sicht an einer Werkbank
    ohne verfuegbare Teile-Sicht faellt auf FA zurueck.
  - ~~Teile-Reiter an einer Werkbank, deren Kuerzel an **keiner** BDE-aktiven Werkbank `BdeTeileliste` traegt (genau die Bedingung der Erkennung V1,
    `WorkOperationStructureDetectionService.cs:96`): Hinweis-Banner „Für dieses Kürzel legt die Struktur-Erkennung Vater-Arbeitsgänge an –
    Teilmeldungen schließen diese nicht ab.“~~ *(ueberholt, Antwort 15: kein Banner – der Teile-Reiter existiert nur an `BdeTeileliste`-Werkbaenken)*

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
- **EF-Default-Falle (Antwort 16, fallstricke.md:1808):** Der DEFAULT beider neuer bit-Spalten (`IstAbschliessend` 1, `BdeStandardsichtTeile` 0) steht
  **nur** in Migration, `SQL/168` und `SQL/00_FreshInstall.sql`. Im EF-Modell (`ApplicationDbContext`) **kein** `HasDefaultValue` – sonst speichert EF ein
  explizites `false` gar nicht und die DB setzt 1, d. h. jede Teilfertig-Meldung waere abschliessend. Vorbild mit Kommentar: `ApplicationDbContext.cs:1315-1316`
  (Kreditoren). `MeldeZeileAsync` setzt `IstAbschliessend` **immer explizit** (`= auftrag.Abschliessend`), nie ueber den Default.
  Die generierte Migration bekommt fuer `AddColumn<bool>` von Hand `defaultValue: true` (`IstAbschliessend`, Bestand) bzw. `false`; das erzeugt nur die
  DB-Constraint, das Modell bleibt ohne Default.
- **Zusaetzlich (Antwort 3):** `ProductionWorkplace` + Spalte **`BdeStandardsichtTeile bit NOT NULL DEFAULT 0`** (0 = FA, 1 = Teile), Name analog
  `BdeTeileliste`/`BdeNurAgFertigmeldung`. Nur Model-Property mit `[Display(Name = "BDE-Standardsicht")]` (ohne `HasDefaultValue`, Antwort 16) + Migration; Leser, Maske und
  `EditableProperties` folgen in Etappe 4a.
  **Reihenfolge geprueft:** Die Spalte entsteht in Etappe 3 und hat bis Etappe 4a keinen Leser. Das ist unschaedlich – EF liest eine Spalte ohne
  Verwendung problemlos mit, der Dienst mappt `ProductionWorkplace` nicht auf dieses Feld (er liest nur `BdeAktiv`/`BdeTeileliste`,
  `WorkOperationStructureDetectionService.cs:96`), und das Epic wird in einem Stueck gemergt und deployt. Eine eigene Migration in Etappe 4a wuerde
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

Teil-Etappen (Antwort 19): **4a** = Wirksamkeits-Helfer + Aufrufer, Zeilenquelle als Service-Methode, View `Fa` mit Spalten/Filter/Pagination,
Anstempeln je Zeile (`StartSetup`/`StartProduction`, Ablaufliste Nr. 7 ohne NurAg), Werkbank-Maske, Reiter/Standardsicht/gemerkte Sicht, Liste
`ProductionWorkplaces`. **4b** = Kopf-Buchungen + `bde-fa-sicht.js`-Aktionen, Modal-Partials (Mengen, Kollision, Gruppe, Close-Others), Scanzeile,
Taetigkeit, NurAg-„Fertig melden“ je Zeile (Ablaufliste Nr. 3, 5, 6, 8–14, 17 und Nr. 7 fuer NurAg). In 4a laufen die Zeilen-Aktionen als Formular-POST
bzw. minimaler Aufruf der bestehenden JSON-Actions mit Reload und sichtbarer Meldung; 4b ergaenzt die Dialoge, ohne die 4a-Wege zu duplizieren.

- ~~**Wirksamkeit neu** in `WerkbankKuerzelFamilie` (ersetzt `TeilelisteWirksam`; …): `TeileSichtVerfuegbar(w, hierarchisch) = w.BdeAktiv && hierarchisch
  && Code(w) != null`; `TeilelisteWirksam` entfaellt samt Test; `BdeTeileliste` wird im Web danach nur noch von Maske, Liste, Validierung und
  Banner-Bedingung gelesen.~~ *(ueberholt, Antwort 15)*
- **Wirksamkeit neu** in `WerkbankKuerzelFamilie` (Antworten 1, 2, 3a, 13, 15 – ohne NurFA-Parameter, G5):
  - `SeiteVerfuegbar(w) = w.BdeAktiv` (beide Standorte, kein Master-Gate fuer die Seite/FA-Sicht);
  - `TeilelisteWirksam(w) = w.BdeAktiv && w.BdeTeileliste && Code(w) != null` – das heutige `TeilelisteWirksam` (`WerkbankKuerzelFamilie.cs:52-53`)
    **ohne** den Parameter `nurFaGlobal`;
  - `TeileSichtVerfuegbar(w, hierarchisch) = hierarchisch && TeilelisteWirksam(w)` (= BdeAktiv && Master && Kuerzel && `BdeTeileliste`, Antworten 13, 15);
  - `TeileStandard(w, hierarchisch) = TeileSichtVerfuegbar(w, hierarchisch) && w.BdeStandardsichtTeile`.
  - Test `BdeTeilelisteOptionTests.cs:86-92` auf die Signatur ohne `nurFa` umschreiben (Faelle `nurFa` entfallen) und um `TeileSichtVerfuegbar`
    (Master aus → false) und `TeileStandard` ergaenzen. Der `<summary>`-Verweis in `ProductionWorkplace.cs:56` bleibt gueltig.
  - ~~Pruefabfrage „AKE hat keine Kuerzel“~~ *(ueberholt: Antwort 13 bindet an den Master, die Abfrage entscheidet nichts mehr)*.
- Aufrufer umstellen:
  - ~~`BdeBookingService.cs:288` → `NurAgFertigmeldungWirksam(...) || TeileSichtVerfuegbar(w, hierarchisch)`; `BdeBookingService` bekommt
    `IServiceSettingRepository` als Ctor-Parameter … Folge von Antwort 3a, bewusst: „meldet ohne Zeit“ gilt damit an jeder IDEAL-Werkbank mit Kuerzel.~~
    *(ueberholt, Antwort 15: „meldet ohne Zeit“ gilt weiter nur an `BdeTeileliste`-Werkbaenken)*
  - `BdeBookingService.cs:287-288` → `NurAgFertigmeldungWirksam(workplace, nurFaGlobal: false) || TeilelisteWirksam(workplace)`; die Lesung
    `nurFa` in `:287` entfaellt (G5: beide Aufrufer – Teileliste-Melden `BdeTeilelisteService.cs:381` und `ReportCompletion` `BdeTerminalController.cs:212` –
    gehoeren nach Etappe 5 allein zur neuen Seite; die FA-Sicht zeigt „Fertig melden“ ebenfalls mit `nurFaGlobal: false`, E4-3, sonst wiese der Server
    ab, was die Oberflaeche anbietet). `ReadBoolSettingAsync` bleibt (weitere Leser `BdeBookingService.cs:58,203,312`; NurFA-Alt-Zweige `:522`, `:543` bleiben bis
    [[2026-11-25-bde-nur-fa-meldung-entfernen]]).
    **Kein Master-Gate hier, kein `IServiceSettingRepository` im `BdeBookingService`** – Begruendung: `BdeTeileliste = 1` ist nur mit Master an speicherbar
    (`ProductionWorkplacesController.cs:259-263`); der einzige Teilmeldungs-Einstieg ist zusaetzlich am Master gegated (Controller, Teile-Reiter + `Melden`).
    Das Gate bleibt damit inhaltlich das heutige (Antwort 15), ohne Ctor-Aenderung und ohne Anpassung der Test-Konstruktionen.
  - `BdeTeilelisteService.cs:260-261` → `TeilelisteWirksam(wp)`; die `nurFa`-Lesung `:260` entfaellt (G5). Ebenfalls kein Master-Gate im Service
    (gleiche Begruendung; der Controller gated `Melden` mit `TeileSichtVerfuegbar`).
  - `BdeTeilelisteController.cs:75` → `SeiteVerfuegbar`; die Teile-Reiter (Index/Summiert, Melden) zusaetzlich `TeileSichtVerfuegbar`. Der Master
    wird im Controller schon gelesen (`BdeTeilelisteController.cs:212-214`) – in eine private Methode ziehen und wiederverwenden.
    `BdeTeilelisteViewModel.Kuerzel` wird nullable (AKE).
    **Nie zurueck auf `BdeTerminal` leiten**, wenn `SeiteVerfuegbar` falsch ist (Werkbank fehlt / BDE inaktiv): ab Etappe 5 leitet `BdeTerminal/Index`
    hierher – das ergaebe eine Schleife. Stattdessen Anmelde-/Kopfansicht mit Werkbank-Auswahl und Hinweis „Werkbank ist nicht BDE-aktiv“.
  - `BdeTerminalController.cs:65` → `TeileStandard` (nur bis Etappe 5, dann Redirect immer).
  - Liste/Maske: `ProductionWorkplacesController.cs:54` (Spaltentext `bde`: zusaetzlich „Standard Teile“ bei `BdeStandardsichtTeile`) und
    `Views/ProductionWorkplaces/Index.cshtml:69-72` (Badge). Der Text „Teileliste“ fuer `BdeTeileliste` bleibt.
- **Erkennung V1** (`WorkOperationStructureDetectionService.cs:96`) bleibt an `BdeTeileliste` (Antwort 3a). Kein Dienst-Code, Service nur Versionsnummer.
  ~~Hinweis-Banner im Teile-Reiter nach der Bedingung in E4-7.~~ *(ueberholt, Antwort 15: kein Banner)*
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
  speichert `UpdateAsync` das Feld nicht). Server-Validierung analog `ValidateTeilelisteAsync`: „Teile“ nur mit ~~Master an, BDE aktiv und Kuerzel~~
  *(ueberholt, Antwort 15)* verfuegbarer Teile-Sicht = Master an, BDE aktiv, Kuerzel **und** `BdeTeileliste` an (Werte aus demselben POST), sonst
  `ModelState`-Fehler „Standardsicht Teile braucht die BDE-Teileliste (vereinfacht) mit Kürzel“, nichts gespeichert (auch ein manipulierter POST an AKE).
  Folge: Wer `BdeTeileliste` ausschaltet, muss die Standardsicht im selben Speichern auf FA stellen (kein stilles Zuruecksetzen – Melden statt still behandeln).

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
| – | *Zuordnung Antwort 19* | | | **4a:** Nr. 1, 2, 7 (ohne NurAg), 15, 19, 20. **4b:** Nr. 3, 5, 6, 7 (NurAg), 8–14, 17. **5:** Nr. 16. Entfaellt: 4, 18 |
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
- ~~Die Kritische Pruefung nennt mit S-2, S-4, S-5, S-6 noch **nicht entschiedene** SOLLTE-Befunde; sie sind nicht eingearbeitet (Stand Rework 2).~~
  *(erledigt, Rework 3: Antworten 18–21 eingearbeitet)*
- Etappe 4 laeuft als **4a** und **4b** mit je eigenem Commit (Antwort 19); bricht ein Lauf nach 4a ab, steht eine bedienbare FA-Tabelle mit Anstempeln
  im Zweig, die Dialoge fehlen noch.
- **Anwender-Doku (Antwort 17, Definition of Done):** jede Etappe mit Oberflaechen-Aenderung zieht im selben Commit `docs/anwender/` nach – betroffene
  Kapitel, Stand-Version im Index `docs/anwender/README.md` (Absatz „Stand und was noch folgt“, `README.md:47`, plus Zeile der Versionstabelle), Fotos per
  Playwright gegen **DEV** (nur ansehen, nichts buchen; Dialoge oeffnen und mit „Abbrechen“ schliessen). Kapitel je Etappe:
  - Etappe 1: `docs/anwender/17-bde-teileliste-werkbank.md` Abschnitt 2 „Ansicht Einzeln“ (AG-Status, „Teile (n) ▸“, Kinderzeilen, Filter-Treffer,
    Handy-Ansicht); Foto `17-03-teileliste-einzeln.jpg` ersetzen, neu `17-08-teileliste-handy.jpg` (360 px).
  - Etappe 2: Kapitel 17 Abschnitt 5 „Ansicht Summiert“ (Aufklappen, Summen-Haken, Ampel); Foto `17-06-teileliste-summiert.jpg` ersetzen.
  - Etappe 3: Kapitel 17 Abschnitte 3, 4, 6, 7 (Teilfertig/Fertig, „gemeldet X von Soll“, Ruecknahme je Meldung, Admin-Spalte Art); Fotos
    `17-05-fertig-melden-fenster.jpg`, `17-07-teilmeldungen-admin.jpg` ersetzen.
  - Etappe 4a: Kapitel 17 „Wer darf“ (Werkbank-Maske mit Auswahl BDE-Standardsicht, Foto `17-01-werkbank-option.jpg` ersetzen, Reiter, Teile-Sicht nur
    mit `BdeTeileliste`) und Kapitel `docs/anwender/11-bde-terminal.md` um die FA-Sicht (Tabelle, Anstempeln, „Unterschiede AKE und IDEAL“) ergaenzen;
    neues Foto `11-05-fa-sicht.jpg`.
  - Etappe 4b: Kapitel 11 um Kopf-Buchungen, Dialoge (Mengen, Kollision, Gruppe, Close-Others), Scan und Taetigkeit ergaenzen; neues Foto
    `11-06-kopf-buchungen.jpg`.
  - Etappe 5: Kapitel 11 auf die neue Seite umschreiben (alte Karten „1. Operator“/„2. Arbeitsgang wählen“ entfallen, Abschnitt 6 „BDE-Teileliste“ auf
    Kapitel 17 verweisen, Tageshistorie neu); die Terminal-Fotos `11-01` bis `11-04` werden **ersetzt** (die alte Seite existiert nicht mehr), neu
    `11-07-tageshistorie.jpg`. Titel der Zeilen 11 und 17 in der Kapiteltabelle des Index (`README.md:36,42`) anpassen.
  - Die Lauf-Notiz listet die geaenderten Kapitel **fuer spaetere Confluence-Uebernahme** (Confluence derzeit nicht, CLAUDE.md Definition of Done).
  Fotonamen sind Vorschlaege nach dem bestehenden Schema `<Kapitel>-<lfd>-<thema>.jpg`; der Dev-Lauf prueft die naechste freie Nummer.

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
    **Nach Antwort 15 geprueft:** Die Bedingung deckt sich mit der neuen `TeileSichtVerfuegbar` (BdeAktiv, `BdeTeileliste`, Kuerzel); das Master-Gate fehlt im
    SQL bewusst, weil `BdeTeileliste = 1` nur mit Master an speicherbar ist (`ProductionWorkplacesController.cs:259-263`) – es wird also keine Werkbank
    vorbelegt, deren Standardsicht die Validierung abwiese. Vorbelegung bleibt unveraendert.
  - Batch 3 Index nur neu bauen, wenn der Filter noch nicht `IstAbschliessend` enthaelt (`sys.indexes.filter_definition`), `DROP INDEX` + `CREATE UNIQUE … WHERE [IsCancelled] = 0 AND [IstAbschliessend] = 1`;
  - Batch 4 `__EFMigrationsHistory`-Insert nur, wenn beide Spalten und der Index-Filter stimmen.
  - `SQL/00_FreshInstall.sql` an zwei Stellen: Tabellendefinitionen (`BdeTeilMeldungen` + Index `:2662`ff., `ProductionWorkplaces` neben `BdeTeileliste` `:364`)
    und MigrationId (Muster `:3516-3518`). FreshInstall ohne Vorbelegung (leere Tabelle).
  - **Defaults nur hier (Antwort 16):** `DF_BdeTeilMeldungen_IstAbschliessend DEFAULT (1)` und `DF_ProductionWorkplaces_BdeStandardsichtTeile DEFAULT (0)`
    stehen in SQL/168, FreshInstall und (per Hand gesetztes `defaultValue`) in der EF-Migration; das EF-Modell bekommt **kein** `HasDefaultValue`
    (fallstricke.md:1808, Vorbild `ApplicationDbContext.cs:1315-1316`). Der Modell-Snapshot fuehrt deshalb keinen Default – gewollt.
  - Nicht daten-destruktiv (Vorbelegung setzt nur die neue Spalte).
  - `SQL/AgentJobs/` nicht betroffen (beide Spalten mit Default; grep 2026-11-25: kein Skript unter `SQL/` ausser 154/FreshInstall/Einmalig
    nennt `BdeTeileliste`). Der Dienst teilt das EF-Modell (`IDEALAKEWMSService.csproj:33` referenziert `IdealAkeWms.csproj`); neu angelegte
    Werkbaenke bekommen dort `BdeStandardsichtTeile = false` (= Default). Deshalb gilt: neue Dienst-Binaries erst nach SQL/168 starten (siehe Deploy).
- **SQL-Nummer:** naechste freie 168 (164/165 reserviert von [[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]], letzte belegte 167). Der Dev-Lauf prueft neu.
- **Konflikt:** SQL/164 der Isolierungsfraese-Spec baut denselben Index mit Filter `[IsCancelled]=0 AND [WarehouseRequisitionItemId] IS NULL` neu.
  Wer als zweiter baut, setzt den vereinigten Filter `[IsCancelled]=0 AND [IstAbschliessend]=1 AND [WarehouseRequisitionItemId] IS NULL` und
  prueft im Index-Guard auf beide Teile (Antwort 12, entschieden).
- Etappen 1, 2, 4a, 4b, 5: keine Migration. ~~Etappe 4 aendert nur die Bedeutung von `ProductionWorkplaces.BdeTeileliste` (true = Standardsicht Teile).~~
  *(ueberholt, Antwort 3a: `BdeTeileliste` behaelt seine Bedeutung; die Standardsicht ist die neue Spalte aus SQL/168.)*
- Tests: EF InMemory erzwingt weder gefilterten UNIQUE noch `sp_getapplock` → doppelte abschliessende Meldung im Manual-UAT gegen SQL Server pruefen (fallstricke „Tests, EF, SQL Server“).
  Ebenso kennt InMemory keine DB-Defaults: dass eine Teilfertig-Meldung mit `IstAbschliessend = 0` in der Tabelle steht (AK 18a), zeigt nur SQL Server (Antwort 16).

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| ~~`ProductionWorkplace.BdeTeileliste` als „BDE-Standardsicht“ (gleiches Feld)~~ *(ueberholt, Antwort 3a)* | | | | | | | |
| `ProductionWorkplace.BdeStandardsichtTeile` (Werkbank-Maske, Anzeige „BDE-Standardsicht“) – **neu** (Spalte Etappe 3, Wirkung ab Etappe 4a) | Auswahl (gespeichert als bit) | `Teile` (1) · `FA` (0); Server prueft beim Speichern: „Teile“ nur mit verfuegbarer Teile-Sicht = Master an, BDE aktiv, Kuerzel gesetzt **und** `BdeTeileliste` an (Antwort 15; vorher ohne `BdeTeileliste` – *ueberholt*), sonst Fehler, nichts gespeichert | `FA` (0; DB-Default nur in SQL/Migration, kein EF-`HasDefaultValue`, Antwort 16); Vorbelegung durch SQL/168: `Teile` an Werkbaenken mit wirksamer Teileliste (BDE aktiv, `BdeTeileliste`, Kuerzel) | **Nur der Startreiter** (Antwort 15): welcher Reiter beim Aufruf ohne gespeicherte Werker-Sicht oeffnet. Teile: Teileliste nach Matchcode, Fertigmeldung ohne Zeit. FA: Arbeitsgaenge der Werkbank mit Anstempeln. Beide Reiter bleiben umschaltbar, Teile nur bei verfuegbarer Teile-Sicht. Reine Oberflaeche – **keine** Wirkung auf Dienst oder Erkennung. | BDE-Seite `BdeTeileliste` (Reiter-Vorgabe), Redirect aus `BdeTerminal/Index` | IDEAL (Auswahl nur bei Master `ProduktionsauftragHierarchisch` sichtbar; an AKE immer FA) | – (keine Aussenwirkung) |
| `ProductionWorkplace.BdeTeileliste` („BDE-Teileliste (vereinfacht)“) – Feld, Maske und Validierung **unveraendert** (Antwort 3a), Wirkung im Web erweitert (Antwort 15) | Schalter (bit) | unveraendert, Validierung `ValidateTeilelisteAsync` | 0 | Unveraendert: „Blechteile per Teilmeldung“ – die Struktur-Erkennung legt fuer Blatt-Tokens dieses Kuerzels keinen Vater-AG an (`WorkOperationStructureDetectionService.cs:96`). Im Web: ~~steuert das Hinweis-Banner; nicht mehr Voraussetzung fuer die Teile-Sicht~~ *(ueberholt, Antwort 15)* **Voraussetzung fuer die Teile-Reiter** (Teilmeldung) und fuer „meldet ohne Zeit“ (`BdeBookingService.cs:288`, wie heute); ohne `BdeTeileliste` nur FA-Sicht. Hilfetext der Maske um „schaltet die Teile-Sicht der BDE-Seite frei“ ergaenzen. | Dienst-Lauf Arbeitsgang-Strukturerkennung, BDE-Seite (Teile-Reiter) | IDEAL | unveraendert (bestehender Hilfetext) |
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
2. Ein Token mit abschliessender Teilmeldung zeigt „erledigt“ (Haken), auch ohne WorkOperation. In Etappe 1–2 gilt jede nicht stornierte Teilmeldung
   als abschliessend (Antwort 21); ab Etappe 3 zeigt ein Token mit nur nicht abschliessenden Teilmeldungen „läuft“.
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
15a. Uebermeldung (Antwort 18): bei „gemeldet 7 von 10“ wird „Teilfertig melden“ mit 5 gespeichert, die Zeile bleibt offen (meldbar, nicht abschliessend)
     und zeigt „gemeldet 12 von 10“; zusaetzlich zur Erfolgsmeldung erscheint sichtbar „Sollmenge erreicht – bitte Fertig melden“. Dasselbe bei
     Teilfertig mit Gut genau = Restmenge (Summe = Soll). Ein anschliessendes „Fertig melden“ schlaegt Gut = 0 vor und schliesst die Zeile.
16. „Fertig melden“ schlaegt Gut = 3 (Rest) vor und schliesst die Zeile; ein zweites „Fertig“ auf denselben Schluessel wird als „bereits gemeldet“ abgewiesen (auch parallel – Manual-UAT gegen SQL Server).
17. „Fertig melden“ mit weniger als Rest schliesst trotzdem.
18a. Nach „Teilfertig melden“ steht die Zeile in `dbo.BdeTeilMeldungen` mit `IstAbschliessend = 0`, nach „Fertig melden“ mit `1` (Manual-UAT gegen
     SQL Server, InMemory zeigt es nicht – Antwort 16): `SELECT Id, Kuerzel, IstAbschliessend, IsCancelled FROM dbo.BdeTeilMeldungen WHERE <Schluessel der Testzeile>`.
     Im EF-Modell steht fuer beide neuen Spalten kein `HasDefaultValue` (Code-Review).
18. Ruecknahme der abschliessenden Meldung oeffnet die Zeile wieder; Ruecknahme einer Teilmeldung senkt „gemeldet X“.
19. Baugruppe „Teilfertig“: am Sub-FA-AG entsteht eine Buchung mit Menge `IsFinal = false`; der AG ist danach in der FA-Struktur nicht erledigt.
    Baugruppe „Fertig“: Menge `IsFinal = true`, AG erledigt.
20. „gemeldete zeigen“ aus blendet nur abgeschlossene Zeilen aus.
21. Admin-Liste Teilmeldungen zeigt und filtert die Spalte Art.
22. SQL/168 laeuft zweimal ohne Fehler; FreshInstall erzeugt beide Spalten und den Index identisch. Nach dem ersten Lauf steht
    `BdeStandardsichtTeile = 1` genau an den Werkbaenken mit BDE aktiv + `BdeTeileliste` + Kuerzel, sonst 0; der zweite Lauf aendert keine Zeile.

**Etappe 4** (Antwort 19: Teil-Etappe je AK in Klammern; jede Teil-Etappe erfuellt ihre AK im eigenen Commit)
23. *(4a)* Reiter FA zeigt die offenen Arbeitsgaenge der Kuerzel-Familie in derselben Reihenfolge wie `available-operations`; fremd laufende mit „läuft“, eigene laufende ohne Start-Knopf.
24. *(4a)* Anstempeln je Zeile startet Ruesten bzw. Produktion; Kollision mit einem anderen Werker zeigt in 4a die Server-Meldung sichtbar, nichts gebucht.
    *(4b)* Bei `BdeNurAgFertigmeldung` steht dort „Fertig melden“ (Mengen-Dialog, Vorgabe Sollmenge).
25. *(4b)* Laufende und pausierte Buchungen erscheinen im Kopf; Teilfertig/Pause/Fertig/Fortsetzen wirken und laden neu.
26. ~~Kollisions-, Gruppen-Fertig- und Close-Others-Fall fuehren zum jeweiligen Dialog wie im Terminal (je ein Testfall).~~ *(ueberholt, Antwort 20 –
    nach Etappe 5 gibt es keine Vergleichsseite mehr)* *(4b)* Je ein Testfall, erwartetes Verhalten ausgeschrieben:
    - **Kollision:** Werker B stempelt einen Arbeitsgang an, auf dem Werker A schon laeuft (Ergebnis `CollisionOtherOperator`, `BdeBookingResult.cs:22`) →
      Dialog „Arbeitsgang bereits in Arbeit“ mit dem Text „AG ist bereits in Arbeit durch <Werkername> an <Werkbank> seit <Uhrzeit>.“ und Knopf „OK“;
      fuer Werker B ist **nichts gebucht** (keine neue Buchung in BDE-Buchungen).
    - **Gruppenpflicht:** „Fertig“ auf einer Produktions-Buchung, die zu einer Gruppe gehoert (Ergebnis `GroupFinishRequired`,
      `BdeTerminalController.cs:149-175`) → Dialog „Alle Produktionsaufträge abschliessen“ mit einer Tabellenzeile je FA/AG der Gruppe (FA/AG, Sollmenge,
      Gut, Ausschuss, Haken „Abschl.“ vorbelegt an); „Alle fertig melden“ (`FinishGroup`) beendet alle Buchungen der Gruppe mit demselben Endzeitpunkt
      und bucht je FA die eingegebene Menge, bei gesetztem Haken mit `IsFinal = true`; Meldung „<n> Buchungen fertig gemeldet.“, danach neu geladen.
      „Abbrechen“ bucht nichts.
    - **Close-Others:** „Fertig“ mit Menge auf einem AG, an dem noch andere Werker buchen (`otherActiveBookings`, `BdeTerminalController.cs:193-197`) →
      Dialog „Weitere aktive Buchungen“ mit der Liste der anderen Werker (Name, „seit“ Uhrzeit); „Ja, alle beenden“ (`CloseOthers`) beendet deren Buchungen,
      Meldung „<n> weitere Buchungen beendet.“; „Nein, nur meine“ laesst sie laufen.
27. *(4b)* Hardware-Scan einer FA-Nr markiert die Zeile; mehrdeutiger Scan oeffnet die Auswahl; unbekannte Nr gibt eine Meldung.
28. *(4a)* Werkbank-Maske (IDEAL) zeigt den Schalter „BDE-Teileliste (vereinfacht)“ unveraendert **und** daneben „BDE-Standardsicht“ als Auswahl Teile/FA;
    „Teile“ ohne Kuerzel, ohne BDE aktiv bzw. ohne `BdeTeileliste` (Antwort 15) wird beim Speichern abgewiesen, nichts gespeichert; ebenso das
    Ausschalten von `BdeTeileliste` bei stehender Standardsicht „Teile“; ein gueltiger Wechsel setzt `Modified*`.
    An AKE (Master aus) ist die Auswahl nicht sichtbar; ein manipulierter POST mit „Teile“ wird abgewiesen.
29. *(4a)* Ohne gespeicherte Sicht oeffnet die Standardsicht (`BdeStandardsichtTeile`); nach Reiterwechsel oeffnet beim naechsten Aufruf die zuletzt gewaehlte.
    Eine gespeicherte Teile-Sicht an einer Werkbank ohne verfuegbare Teile-Sicht oeffnet FA.
30. ~~Master an, Werkbank ohne Kuerzel: Teile-Reiter deaktiviert mit Hinweistext.~~ *(ueberholt, Antwort 15)* *(4a)* Master an, Werkbank ohne Kuerzel **oder**
    mit Kuerzel, aber `BdeTeileliste` aus: Teile-Reiter deaktiviert (`aria-disabled`) mit sichtbarem Hinweistext, nur FA-Sicht. Master aus: keine Teile-Reiter, nur FA.
31. ~~Baugruppen-Teilmeldung aus dem Teile-Reiter einer IDEAL-Werkbank mit Kuerzel, aber `BdeTeileliste` aus und Standardsicht FA, wird gebucht
    (Gate `TeileSichtVerfuegbar`, nicht mehr `BdeTeileliste`).~~ *(ueberholt, Antwort 15)*
    *(4a)* An einer IDEAL-Werkbank mit Kuerzel, aber `BdeTeileliste` aus, wird ein direkter POST auf `BdeTeileliste/Melden` abgewiesen (keine Teilmeldung,
    keine Buchung); `ReportCompletionCoreAsync` weist dort mit „Diese Werkbank meldet nicht ohne Zeit fertig.“ ab, sofern nicht `BdeNurAgFertigmeldung`
    gesetzt ist (Gate `BdeBookingService.cs:288` wie heute).
32. ~~Teile-Reiter an einer Werkbank, deren Kuerzel an keiner BDE-aktiven Werkbank `BdeTeileliste` traegt: Hinweis-Banner zur Erkennung sichtbar;
    mit `BdeTeileliste` an einer Familien-Werkbank: kein Banner.~~ *(ueberholt, Antwort 15: Banner entfaellt)*
32a. *(4a)* Aendern von `BdeStandardsichtTeile` aendert nichts an der Struktur-Erkennung (Dienst-Lauf legt dieselben Vater-AGs an wie vorher).
32b. *(4a; Kopf-Buchungen und Fertig ab 4b)* AKE-DEV (Master aus, Werkbank ohne Kuerzel, BDE aktiv): die Seite oeffnet die FA-Sicht mit den offenen Arbeitsgaengen dieser Werkbank;
     Anstempeln, Kopf-Buchungen und Fertig wirken wie an IDEAL.
32c. *(4a)* Die Seite liest `BdeNurFaMeldung` nicht: mit `BdeNurFaMeldung = true` auf DEV zeigt die FA-Sicht weiter Arbeitsgaenge (keine NurFA-Oberflaeche).
     **Bewusste Restabweichung (Antwort 22, kein Fehler):** In diesem Zustand darf der Server Starts nach seinen NurFA-Alt-Zweigen behandeln
     (`BdeBookingService.cs:522,543`, siehe G5) – z. B. Abweisung „Dieser Arbeitsgang gehört nicht zu dieser Werkbank.“ bei einem AG, dessen FA an einer
     anderen Werkbank haengt. Geprueft wird nur, dass die Oberflaeche unveraendert bleibt und Server-Meldungen sichtbar erscheinen.
32d. *(4a)* Werkbank nicht BDE-aktiv bzw. unbekannte `workplaceId`: Hinweis „Werkbank ist nicht BDE-aktiv“ mit Werkbank-Auswahl, **kein** Redirect-Kreis
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
37. Anwender-Doku (Antwort 17): je Etappe mit Oberflaechen-Aenderung sind die in „Arbeitsweise“ genannten Kapitel von `docs/anwender/` (11, 17) im
    Etappen-Commit nachgezogen, die Stand-Version im Index `docs/anwender/README.md` zeigt die Etappen-Version, die Fotos stammen per Playwright von DEV
    (nichts gebucht); nach Etappe 5 zeigt kein Foto mehr die alte Terminal-Seite (`11-01` bis `11-04` ersetzt). Die Lauf-Notiz listet die geaenderten
    Kapitel fuer spaetere Confluence-Uebernahme.

## Test-Szenarien

Nummern vorlaeufig (letztes TS-190, `docs/TESTSZENARIEN.md:13864`; der Dev-Lauf prueft neu). Bestehendes TS-152 (Teileliste) bleibt und bekommt Verweise.
Die geplante TS-165 der Mobil-Spec entfaellt.

- **TS-191 Teileliste AG-Status, Baugruppen-Kinder, mobil** – Vorbedingung IDEAL-DEV, Werkbank mit Kuerzel und `BdeTeileliste` an, Baugruppe mit ≥ 2 Kindern,
  ein AG ueberfaellig. Schritte: Spalte pruefen (Symbole, Screenreader-Text), aufklappen, Filter auf Kind-Matchcode, sortieren, Zahnrad, Handy-Breite 360 px
  (Filter eingeklappt, Karten, 44 px). Negativ: Filter ohne Treffer → Leerzustand; Terminal-Seite optisch unveraendert.
- **TS-192 Summiert aufklappen, Summen-Haken, Ampel** – Gruppe mit 3 Auftraegen verschiedener FE-Termine. Schritte: Reihenfolge, Summen-Haken,
  Teilauswahl, Fertigmelden. Negativ: Summen-Haken + einzelnes Abwaehlen; bereits gemeldeter Auftrag erscheint nicht als waehlbar.
- **TS-193 Teilfertigmeldung** – Teilfertig 3/10, 4/10, Fertig mit Rest, Ruecknahmen, Baugruppe Teil/Fertig (Buchung in BDE-Buchungen pruefen),
  Admin-Liste Art; nach Teilfertig bzw. Fertig per SQL auf DEV pruefen: `IstAbschliessend = 0` bzw. `1` (AK 18a, Antwort 16 – nur gegen SQL Server
  aussagekraeftig). Uebermeldung (Antwort 18, AK 15a): bei 7/10 Teilfertig 5 → gespeichert, Zeile offen, „gemeldet 12 von 10“, Hinweis „Sollmenge
  erreicht – bitte Fertig melden“ sichtbar; danach Fertig (Vorgabe 0) schliesst. Negativ: Teilfertig ohne Menge; zweites Fertig; paralleles Fertig von
  zwei Geraeten (nur eines gewinnt); Ruecknahme nach Frist.
- ~~**TS-194 FA-Sicht** – IDEAL-DEV: Anstempeln, Kopf-Buchungen, Pause/Fortsetzen, Teilfertig, Fertig mit Gruppenpflicht, Close-Others, Kollision,
  Taetigkeit, Scan (exakt/mehrdeutig/unbekannt), …~~ *(ueberholt, Antwort 19: geteilt in TS-194 (4a) und TS-195 (4b))*
- **TS-194 FA-Sicht Tabelle, Anstempeln, Standardsicht (Etappe 4a)** – IDEAL-DEV: FA-Reiter, Reihenfolge, fremd/eigen laufend, Anstempeln Ruesten/Produktion,
  Kollision als sichtbare Meldung (nichts gebucht), neue Auswahl „BDE-Standardsicht“ neben dem unveraenderten Schalter „BDE-Teileliste“, gemerkte Sicht,
  Werkbank ohne Kuerzel (Teile-Reiter deaktiviert), ~~Werkbank mit Kuerzel aber `BdeTeileliste` aus (Teile-Reiter mit Banner, Baugruppen-Teilmeldung
  wird gebucht)~~ *(ueberholt, Antwort 15)* Werkbank mit Kuerzel aber `BdeTeileliste` aus (Teile-Reiter deaktiviert mit Hinweis, nur FA; direkter POST
  auf `Melden` abgewiesen, AK 31), Pruefung nach SQL/168: vorbelegte Standardsicht an den bisherigen Teileliste-Werkbaenken. AKE-DEV (Worktree-Stand, nie main):
  BDE an einer Werkbank aktivieren, FA-Sicht oeffnen, keine Teile-Reiter, Anstempeln. Negativ: „Teile“ ohne Kuerzel bzw. ohne `BdeTeileliste`
  speichern; `BdeTeileliste` ausschalten bei Standardsicht „Teile“ (Fehler, nichts gespeichert); manipulierter
  POST „Teile“ an AKE; `workplaceId` einer nicht BDE-aktiven Werkbank (kein Redirect-Kreis); `BdeNurFaMeldung = true` auf DEV setzen → FA-Sicht
  unveraendert, Server-Abweisungen aus den NurFA-Alt-Zweigen sind erwartete Restabweichung (Antwort 22, AK 32c), danach zuruecksetzen.
- **TS-195 FA-Sicht Kopf-Buchungen, Dialoge, Scan, Taetigkeit (Etappe 4b)** – IDEAL-DEV und AKE-DEV (Worktree-Stand): Kopf-Buchungen laufend/pausiert,
  Teilfertig/Pause/Fertig/Fortsetzen, Detailangaben-Schalter (gemerkt), NurAg-„Fertig melden“ je Zeile mit Mengen-Dialog, Taetigkeit starten/beenden,
  Scan (exakt/mehrdeutig/unbekannt). Je ein Fall nach AK 26: Kollision (Dialog mit Werkername, Werkbank, Uhrzeit; nichts gebucht), Gruppenpflicht
  (Tabelle aller FAs der Gruppe, „Alle fertig melden“ bucht je FA die Menge mit `IsFinal` laut Haken), Close-Others (Liste der anderen Werker, „Ja, alle
  beenden“ beendet deren Buchungen). Negativ: Gruppen-Dialog „Abbrechen“ bucht nichts; Close-Others „Nein, nur meine“ laesst die anderen laufen;
  Doppelklick auf Start bucht nur einmal.
- ~~**TS-195 Tageshistorie und Abloesung**~~ *(umnummeriert, Antwort 19)* **TS-196 Tageshistorie und Abloesung (Etappe 5)** – Menue, Knopf, Inhalt (Buchungen + Teilmeldungen), Pagination; Aufruf `BdeTerminal/Index` an IDEAL- **und**
  AKE-DEV → Redirect auf die neue Seite; `/Settings` zeigt `BdeTerminalMaxHauptFa` nur noch als verwaist; Anmeldung am Android-Handy mit lokalem
  WMS-Konto + Terminal (sobald ein Konto angelegt ist, sonst als offen vermerken). Negativ: Benutzer ohne Terminal → Hinweis wie heute.

`secondbrain/tests/testszenarien-index.md` je Etappe nachziehen.

## Etappen (nur bei epic: true)

Ein Worktree (`.claude/worktrees/2026-08-07-ideal-teile-1-5`), je Etappe ein Commit, **ein Merge am Ende**. Branch regelmaessig mit
`scripts/sync-worktree.ps1` auf main-Stand halten. Versionen und TS-Nummern vorlaeufig (letzte Version 1.99.69), der Dev-Lauf prueft neu.
Jede Etappe: `frontend-design` explizit aufrufen, Kontrast messen, Ampel nie nur Farbe, Anwender-Changelog + Hilfe, Testszenario,
**`docs/anwender/` mit Playwright-Fotos gegen DEV + Stand-Version im Index** (Antwort 17; Kapitel je Etappe siehe „Arbeitsweise“).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | Teile Einzeln: AG-Status (Status-Ladung und Symbol-Partial aus FA-Struktur herausgezogen), Kinder als Zeilen, Filter auf Kinder, Teile-Knopf vorn, BDE mobil kompakt (992 px, 44 px, Spaltenfilter in Karten nicht bedienbar – Antworten 8–10). Doku: Kapitel 17 Abschnitt 2 + Fotos `17-03`, Handy-Foto. v1.99.70, TS-191, keine Migration. | offen | |
| 2 | Teile Summiert: Einzelauftraege aufklappbar, Summen-Haken, aggregierte Ampel ueber `Ableiten` (Antwort 11). Doku: Kapitel 17 Abschnitt 5 + Foto `17-06`. v1.99.71, TS-192, keine Migration. | offen | |
| 3 | Teilfertigmeldung: Migration + SQL/168 + FreshInstall, Service/Controller/Dialog, Ruecknahme, Waisen, Baugruppe `istFinal`, Admin-Spalte Art, ADR 0036. **Dieselbe Migration legt `ProductionWorkplaces.BdeStandardsichtTeile` an und belegt sie vor** (Antwort 3a; Model-Property ohne Leser bis Etappe 4a – Reihenfolge geprueft, siehe Etappe-3-Entwurf). Defaults beider Spalten nur in Migration/SQL/FreshInstall, kein `HasDefaultValue`, `IstAbschliessend` immer explizit gesetzt (Antwort 16, AK 18a). Index nach Antwort 12: wer zweitens baut, vereinigt den Filter mit SQL/164 – der Dev-Lauf prueft beim Start, ob SQL/164 schon im Branch liegt. Uebermeldung bei Teilfertig erlaubt mit Hinweis (Antwort 18, AK 15a). Doku: Kapitel 17 Abschnitte 3/4/6/7 + Fotos `17-05`, `17-07`. v1.99.72, TS-193. | offen | |
| 4 | ~~FA-Sicht an beiden Standorten: Wirksamkeits-Helfer … `TeilelisteWirksam` entfernt, `BdeBookingService` mit Master-Lesung, … Banner, …~~ *(ueberholt, Antwort 15)* FA-Sicht an beiden Standorten: Wirksamkeits-Helfer ohne NurFA (`SeiteVerfuegbar`, `TeilelisteWirksam(w)` ohne `nurFa`, `TeileSichtVerfuegbar` = Master && `TeilelisteWirksam`, `TeileStandard` ueber `BdeStandardsichtTeile`), Gate `BdeBookingService.cs:288` inhaltlich wie heute (ohne `nurFa`, ohne Master, kein neuer Ctor-Parameter), Zeilenquelle als gemeinsame Service-Methode, View `Fa`, AG-Status aller AG der Sub-FA (Antwort 4), Kopf-Buchungen, Modal-Partials, Scan, Standardsicht-Auswahl neben `BdeTeileliste` („Teile“ nur mit `BdeTeileliste`), Teile-Reiter nur mit `BdeTeileliste` (sonst deaktiviert mit Hinweis, kein Banner), gemerkte Sicht, Pagination statt MaxHauptFa (Antwort 14). Doku: Kapitel 17 (Maske, Reiter, Foto `17-01`) + Kapitel 11 FA-Sicht (neue Fotos). v1.99.73, TS-194, keine weitere Migration. *(ueberholt, Antwort 19: aufgeteilt in 4a und 4b)* | – | |
| 4a | FA-Sicht Grundgeruest an beiden Standorten (Antwort 19): Wirksamkeits-Helfer ohne NurFA (`SeiteVerfuegbar`, `TeilelisteWirksam(w)` ohne `nurFa`, `TeileSichtVerfuegbar` = Master && `TeilelisteWirksam`, `TeileStandard` ueber `BdeStandardsichtTeile`) samt Aufrufer-Umstellung, Gate `BdeBookingService.cs:288` inhaltlich wie heute (ohne `nurFa`, ohne Master, kein neuer Ctor-Parameter), Zeilenquelle als gemeinsame Service-Methode, View `Fa` mit Spalten/Filter/Pagination statt MaxHauptFa (Antwort 14), AG-Status aller AG der Sub-FA (Antwort 4), Anstempeln Ruesten/Produktion je Zeile (Kollision als sichtbare Meldung), Werkbank-Maske mit Standardsicht-Auswahl neben `BdeTeileliste` („Teile“ nur mit `BdeTeileliste`), Liste `ProductionWorkplaces`, Teile-Reiter nur mit `BdeTeileliste` (sonst deaktiviert mit Hinweis), gemerkte Sicht. AK 23, 24 (4a-Teil), 28–32d. Doku: Kapitel 17 (Maske, Reiter, Foto `17-01`) + Kapitel 11 FA-Sicht (Foto `11-05`). v1.99.73, TS-194, keine weitere Migration. | offen | |
| 4b | FA-Sicht Bedienung (Antwort 19): Kopf-Buchungen laufend/pausiert mit Teilfertig/Pause/Fertig/Fortsetzen und Detailangaben-Schalter, `bde-fa-sicht.js`, Modal-Partials aus `Views/BdeTerminal/Index.cshtml` (Mengen, Kollision, Gruppe, Close-Others; beide Seiten nutzen sie bis Etappe 5), Scanzeile, Taetigkeit, NurAg-„Fertig melden“ je Zeile. AK 24 (4b-Teil), 25, 26, 27. Doku: Kapitel 11 Kopf/Dialoge/Scan (Foto `11-06`). v1.99.74, TS-195, keine Migration. | offen | |
| 5 | Tageshistorie inkl. Teilmeldungen (Seite, Knopf, NavZiel; Antwort 5), `BdeTerminal/Index` leitet an beiden Standorten immer um, alte View + `bde-terminal.js` geloescht, `BdeTerminalMaxHauptFa` entfernt, Katalogtext `BdeNurFaMeldung`, Betriebsdoku Handy-Konten (Antworten 1, 2, 6). Doku: Kapitel 11 umgeschrieben, Terminal-Fotos `11-01`–`11-04` ersetzt, Tageshistorie-Foto, Index-Titel 11/17 (Antwort 17). v1.99.75, TS-196, keine Migration (vor Antwort 19: v1.99.74, TS-195). | offen | |

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
| `Pruefung/BdeNurFaMeldungAn` | 1.99.73 | Pruefung | beide | Vorher | anzeigen | `SELECT COUNT(*) FROM dbo.AppSettings WHERE [Key] = N'BdeNurFaMeldung' AND LOWER(LTRIM(RTRIM([Value]))) IN (N'true', N'1', N'on')` – Treffer > 0: „BdeNurFaMeldung ist an; die neue BDE-Seite beachtet die Einstellung nicht (Antwort 2), der Buchungsdienst aber weiter in seinen Alt-Zweigen (Starts koennen abgewiesen werden, Antwort 22) – vor dem Update mit Gerald klaeren.“ Sichert die Restabweichung aus G5/AK 32c ab. |
| `Pruefung/BdeStandardsichtOhneTeileSicht` | 1.99.73 | Pruefung | IDEAL | Nachher | anzeigen | ~~`… WHERE BdeStandardsichtTeile = 1 AND (BdeAktiv = 0 OR ISNULL(LTRIM(RTRIM(ArbeitsschrittCode)), N'') = N'')`~~ *(ueberholt, Antwort 15)* `SELECT COUNT(*) FROM dbo.ProductionWorkplaces WHERE BdeStandardsichtTeile = 1 AND (BdeAktiv = 0 OR BdeTeileliste = 0 OR ISNULL(LTRIM(RTRIM(ArbeitsschrittCode)), N'') = N'')` – Treffer > 0: „Werkbaenke mit Standardsicht Teile ohne BDE aktiv, ohne BDE-Teileliste bzw. ohne Kuerzel – oeffnen FA; in den Werkbank-Stammdaten korrigieren.“ (Nach SQL/168 erwartet 0, weil die Vorbelegung dieselbe Bedingung nutzt.) |
| `v1.99.73-BdeStandardsichtEinstellung` | 1.99.73 | Einstellung | IDEAL | Nachher | – | „Werkbank-Stammdaten: neue Auswahl BDE-Standardsicht (Teile / FA) je BDE-Werkbank pruefen; vorbelegt mit Teile, wo bisher die Teileliste wirkte. Der Schalter BDE-Teileliste bleibt in der Maske unveraendert (steuert die Arbeitsgang-Erkennung) und schaltet die Teile-Sicht frei; ohne ihn nur FA-Sicht.“ |
| `v1.99.73-BdeFaSichtAkeInfo` | 1.99.73 | Info | AKE | Nachher | – | „BDE-Seite mit FA-Sicht gilt auch an AKE (derzeit ohne BDE-Nutzung); keine Aktion noetig.“ |
| `v1.99.75-BdeTerminalAbloesungInfo` | 1.99.75 | Info | beide | Nachher | – | „BDE-Terminal oeffnet an beiden Standorten die neue Seite mit Reitern; alte Terminal-Seite entfernt; Tageshistorie im Menue. Einstellung BdeTerminalMaxHauptFa entfaellt (in /Settings als verwaist sichtbar).“ |
| `v1.99.75-BdeHandyKontenInfo` | 1.99.75 | Info | beide | Nachher | – | „BDE am Android-Handy: je Geraet lokales WMS-Konto (Formular-Login) + Terminal-Zuordnung anlegen (docs/betrieb/09). Konten sind noch nicht angelegt.“ |

  SQL/168 ist ein Schema-Skript (kein Trockenlauf/Transaktion noetig; die Vorbelegung laeuft nur im Erstanlage-Zweig der Spalte). Keine Daten-/Einmal-Skripte.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService` (nur Versionsnummer + geteiltes Modell)

## Offene Rueckfragen

> [!success] Alle 14 beantwortet (Gerald 2026-11-25), siehe „Freigabe-Antworten“; im Rumpf eingearbeitet. Keine neuen offenen Rueckfragen.
> Die Fragen bleiben unten als Bezug der Antworten stehen; ihre Empfehlungen zu 1, 2, 3 und 6 sind durch die Antworten **ueberholt**.
> Rework 2: Antworten 15–17 zu den Pruefbefunden B-1, S-1, S-3 sind eingearbeitet. ~~Die Pruefbefunde S-2, S-4, S-5, S-6 (Abschnitt „Kritische Pruefung“)
> sind **noch nicht entschieden** und nicht eingearbeitet.~~ *(erledigt)*
> Rework 3: Antworten 18–22 (S-2, S-4, S-5, S-6, NurFA-Alt-Zweige `BdeBookingService.cs:522,543`) sind eingearbeitet. Keine offenen Rueckfragen.

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
15. (Pruefbefund B-1) Teile-Reiter nur mit `BdeTeileliste`? → **Ja, Vorschlag der Pruefung (Gerald 2026-11-25): Teile-Reiter nur an Werkbaenken mit `BdeTeileliste` an; `BdeStandardsichtTeile` = nur Startreiter, „Teile“ nur mit `BdeTeileliste` speicherbar; Banner und AK 31 entfallen; Gate `BdeBookingService.cs:288` inhaltlich wie heute.**
16. (Pruefbefund S-1) EF-Default-Falle `IstAbschliessend` → **Ja, Vorschlag der Pruefung (Gerald 2026-11-25): DEFAULT nur in Migration/SQL/FreshInstall, kein `HasDefaultValue` im EF-Modell, Wert immer explizit setzen; AK „Teilfertig = 0 in der DB“ (Manual-UAT SQL Server).**
17. (Pruefbefund S-3) Doku `docs/anwender/` → **Nachholen (Gerald 2026-11-25): `docs/anwender/` BDE-Kapitel + Fotos per Playwright gegen DEV + Stand-Version, je Etappe mit Oberflaechen-Aenderung.**
18. (Pruefbefund S-2) Uebermeldung bei Teilfertig → **Vorschlag uebernommen (Gerald 2026-11-25): erlaubt, Zeile bleibt offen, Ergebnis-Hinweis „Sollmenge erreicht – bitte Fertig melden“.**
19. (Pruefbefund S-4) Etappe 4 teilen → **Ja (Gerald 2026-11-25): 4a Helfer/Feld/Maske, Reiter, FA-Tabelle + Anstempeln je Zeile; 4b Kopf-Buchungen, Modals, Scan, Taetigkeit – je eigener Commit.**
20. (Pruefbefund S-5) AK 26 ausformulieren → **Ja (Gerald 2026-11-25): je Fall erwartetes Verhalten wie im Pruefbefund S-5 beschrieben.**
21. (Pruefbefund S-6) Etappe 1–2 vor Datenmodell → **Ja (Gerald 2026-11-25): in Etappe 1–2 zaehlt jede nicht stornierte Meldung als abschliessend.**
22. (Neuer Fund Rework 2) `BdeNurFaMeldung` in `BdeBookingService.cs:522,543` → **So lassen (Gerald 2026-11-25); bereinigt mit [[2026-11-25-bde-nur-fa-meldung-entfernen]]; abgesichert durch Manifest-Pruefung `Pruefung/BdeNurFaMeldungAn`.**

## Kritische Pruefung (2026-11-25)

Grundlage: Spec-Stand `11895bf8`, Backlog [[2026-11-25-bde-masken-vereinheitlichung]], ADR 0026/0030, fallstricke, Code im Buendel-Worktree
(Stichproben `BdeBookingService.cs:278-335`, `BdeTeilelisteService.cs:400-436`, `bde-terminal.js:671-727`, `fallstricke.md:1808`).
Antworten 1–14 sind vollstaendig (jede mit Text, keine Entweder-oder-Frage nur mit „ja“) und im Rumpf eingearbeitet.

### BLOCKER

- **B-1 Teile-Sicht ohne `BdeTeileliste` oeffnet einen zweiten Meldeweg fuer dasselbe Teil.** Die Rework-Fassung macht die Teile-Sicht an **jeder**
  Werkbank mit Kuerzel verfuegbar (E4-7, Helfer `TeileSichtVerfuegbar`, AK 31) und weitet das Gate „meldet ohne Zeit“ (`BdeBookingService.cs:288`)
  entsprechend aus. Das hat Gerald so nicht entschieden – Antwort 3a trennt nur *Standardreiter* von *Datenwirkung*. Folge: An einer Werkbank, deren
  Kuerzel Vater-AGs bekommt (`BdeTeileliste` aus, Erkennung V1 legt AGs an), kann ein Werker Blechteile im Teile-Reiter melden; diese Meldungen
  schliessen den Vater-AG nie (die Spec loest das nur mit einem Warn-Banner, E4-7 letzter Punkt). Genau das Doppelweg-Problem, das Variante a
  vermeiden sollte.
  **Frage an Gerald:** Teile-Reiter **nur** an Werkbaenken mit `BdeTeileliste` an (dann ist `BdeTeileliste` = „Blechteile per Teilmeldung + Teile-Sicht“,
  `BdeStandardsichtTeile` = nur Startreiter, „Teile“ als Standard nur mit `BdeTeileliste` speicherbar, Banner und AK 31 entfallen, Gate
  `BdeBookingService.cs:288` bleibt inhaltlich wie heute) – **oder** an jeder Kuerzel-Werkbank mit Banner wie im Entwurf?
  *Vorschlag der Pruefung: nur mit `BdeTeileliste`* (ein Meldeweg je Teil, kleinerer Diff, kein `IServiceSettingRepository` im `BdeBookingService` noetig
  ausser fuers Master-Gate).

### SOLLTE

- **S-1 EF-Default-Falle bei `IstAbschliessend`.** `fallstricke.md:1808`: `HasDefaultValue` auf bool verschluckt ein explizites `false` beim Insert.
  Mit `HasDefaultValue(true)` im Modell wuerde **jede** Teilfertig-Meldung abschliessend gespeichert. Vorschlag: DEFAULT (1) nur in Migration/SQL/168
  und FreshInstall, im EF-Modell **kein** `HasDefaultValue`; `MeldeZeileAsync` setzt den Wert immer explizit. Gleiches fuer `BdeStandardsichtTeile`
  (Default 0 ist zwar CLR-Default, trotzdem kein `HasDefaultValue` im Modell). Als AK ergaenzen: „Teilfertig-Meldung hat in der DB `IstAbschliessend = 0`“
  (InMemory zeigt das nicht – Manual-UAT gegen SQL Server).
- **S-2 Uebermeldung bei Teilfertig nicht geregelt.** Was passiert bei „Teilfertig“ mit Gut ≥ Restmenge bzw. Summe Gut > Soll? Vorschlag: erlaubt
  (Mehrmenge kommt vor), Zeile bleibt offen, Hinweis im Ergebnis „Sollmenge erreicht – bitte Fertig melden“. In E3-1 und AK ergaenzen.
- **S-3 Definition of Done Doku fehlt.** Die Spec nennt `docs/betrieb/09` (Handy), aber nicht `docs/anwender/` (BDE-Kapitel, neue Fotos per Playwright
  gegen DEV) und Stand-Version im Index (CLAUDE.md, Definition of Done). In „Arbeitsweise“ je Etappe bzw. spaetestens Etappe 5 ergaenzen; die
  Terminal-Fotos werden mit Etappe 5 ungueltig.
- **S-4 Etappe 4 ist allein ein voller Lauf.** 20 Ablaeufe, Zeilenquelle-Extraktion aus `BdeApiController.cs:204-327`, Modal-Partials, neue Wirksamkeits-
  Helfer mit Aufrufer-Umstellung, Werkbank-Maske. Vorschlag: Etappe 4 in **4a** (Helfer/Feld/Maske, Reiter, FA-Tabelle read-only + Anstempeln je Zeile)
  und **4b** (Kopf-Buchungen, Modals, Scan, Taetigkeit) teilen – je eigener Commit, sonst bleibt bei Abbruch eine halbe FA-Sicht im Zweig.
- **S-5 AK 26 ist nach Etappe 5 nicht mehr pruefbar** („wie im Terminal“ – die Vergleichsseite wird geloescht). Vorschlag: je Fall das erwartete
  Verhalten ausschreiben (Kollision: Dialog „Arbeitsgang bereits in Arbeit“ mit Werkername, nichts gebucht; Gruppenpflicht: Tabelle aller FAs der
  Gruppe, „Alle fertig melden“ bucht je FA eine IsFinal-Menge; Close-Others: Liste der anderen Werker, „Ja“ beendet deren Buchungen).
- **S-6 Etappe 1/2 vor dem Datenmodell.** E1-1/AK 2 sprechen von „abschliessender Teilmeldung“, die es erst ab Etappe 3 gibt. Klarstellen: in Etappe 1–2
  zaehlt jede nicht stornierte Meldung als abschliessend (heutiges Verhalten).

### HINWEIS

- H-1 Stichproben bestaetigt: `ReportCompletionCoreAsync` erzeugt heute immer `IsFinal = true` (`BdeBookingService.cs:330-331`), Ruecknahme storniert die
  verknuepfte Buchung (`BdeTeilelisteService.cs:426-431`), `#activityModal`-Handler ist tot, `StartActivity` lebt ueber die Taetigkeitsliste
  (`bde-terminal.js:671` vs. `:707-727`) – Ablaufliste Nr. 14 und Zusatzfund sind konsistent.
- H-2 Laufzeit-Ticker (`bde-terminal.js:40-96`) entfaellt durch `location.reload()`-Muster; die Kopfzeile zeigt die Laufzeit nur zum Ladezeitpunkt. Bewusst
  so akzeptieren oder einen kleinen Sekunden-Ticker im neuen JS vorsehen.
- H-3 Antwort 2 („steht auf false“) ist fuer beide Standorte durch die Manifest-Pruefung `Pruefung/BdeNurFaMeldungAn` abgesichert – gut.
- H-4 Versions-/TS-/SQL-Nummern sind vorlaeufig; die juengsten Commits zeigen v1.99.65 im Lauf – der Dev-Lauf muss neu zaehlen (steht drin).
- H-5 AKE-DEV nur mit Worktree-Stand deployen (Memory/Projektregel), TS-194 nennt das bereits.

**NACHBESSERUNG NOETIG: B-1 (Teile-Sicht nur mit `BdeTeileliste`?) entscheiden; S-1 und S-3 vor der Freigabe in den Rumpf ziehen.**
