---
type: spec
title: BDE-Masken vereinheitlichen - Teile- und FA-Sicht, AG-Status-Ampel, Teilfertigmeldung, BDE mobil kompakt
slug: 2026-11-25-bde-masken-vereinheitlichung-spec
status: Testbereit
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
  - "1 Teile Einzeln: AG-Status, Baugruppen-Kinder als Zeilen, Filter auf Kinder, Teile-Knopf vorn, BDE mobil kompakt (v1.99.73, TS-194, keine Migration)"
  - "2 Teile Summiert: Einzelauftraege aufklappbar, Summen-Haken, aggregierte Ampel (v1.99.74, TS-195, keine Migration)"
  - "3 Teilfertigmeldung mit Restmenge, ADR 0037, Baugruppe als Teil-Buchung; SQL/169 legt zusaetzlich die Spalte BdeStandardsichtTeile an (ohne Leser bis Etappe 4a) (v1.99.75, TS-196, SQL/169)"
  - "4a Wirksamkeits-Helfer, Feld/Maske BDE-Standardsicht, Reiter, FA-Tabelle + Anstempeln je Zeile, FA-Sicht auch an AKE (v1.99.76, TS-197, keine weitere Migration)"
  - "4b FA-Sicht Kopf-Buchungen, Modals (Mengen/Kollision/Gruppe/Close-Others), Scan, Taetigkeit, NurAg-Fertig je Zeile (v1.99.77, TS-198, keine Migration)"
  - "5 Tageshistorie eigene Seite + Knopf, alte Terminal-Seite an beiden Standorten abgeloest und entfernt (v1.99.78, TS-199, keine Migration)"
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
| `ProductionWorkplace.BdeStandardsichtTeile` (Werkbank-Maske, Anzeige „BDE-Standardsicht“) – **neu** (Spalte Etappe 3, Wirkung ab Etappe 4a) | Auswahl (gespeichert als bit) | `Teile` (1) · `FA` (0); Server prueft beim Speichern: „Teile“ nur mit verfuegbarer Teile-Sicht = Master an, BDE aktiv, Kuerzel gesetzt **und** `BdeTeileliste` an (Antwort 15; vorher ohne `BdeTeileliste` – *ueberholt*), sonst Fehler, nichts gespeichert | `FA` (0; DB-Default nur in SQL/Migration, kein EF-`HasDefaultValue`, Antwort 16); Vorbelegung durch SQL/169 *(v1.99.79 F-23: vorher „SQL/168“, gemeint 169)*: `Teile` an Werkbaenken mit wirksamer Teileliste (BDE aktiv, `BdeTeileliste`, Kuerzel) | **Nur der Startreiter** (Antwort 15): welcher Reiter beim Aufruf ohne gespeicherte Werker-Sicht oeffnet. Teile: Teileliste nach Matchcode, Fertigmeldung ohne Zeit. FA: Arbeitsgaenge der Werkbank mit Anstempeln. Beide Reiter bleiben umschaltbar, Teile nur bei verfuegbarer Teile-Sicht. Reine Oberflaeche – **keine** Wirkung auf Dienst oder Erkennung. | BDE-Seite `BdeTeileliste` (Reiter-Vorgabe), Redirect aus `BdeTerminal/Index` | IDEAL (Auswahl nur bei Master `ProduktionsauftragHierarchisch` sichtbar; an AKE immer FA) | – (keine Aussenwirkung) |
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

- **TS-194 Teileliste AG-Status, Baugruppen-Kinder, mobil** – Vorbedingung IDEAL-DEV, Werkbank mit Kuerzel und `BdeTeileliste` an, Baugruppe mit ≥ 2 Kindern,
  ein AG ueberfaellig. Schritte: Spalte pruefen (Symbole, Screenreader-Text), aufklappen, Filter auf Kind-Matchcode, sortieren, Zahnrad, Handy-Breite 360 px
  (Filter eingeklappt, Karten, 44 px). Negativ: Filter ohne Treffer → Leerzustand; Terminal-Seite optisch unveraendert.
- **TS-195 Summiert aufklappen, Summen-Haken, Ampel** – Gruppe mit 3 Auftraegen verschiedener FE-Termine. Schritte: Reihenfolge, Summen-Haken,
  Teilauswahl, Fertigmelden. Negativ: Summen-Haken + einzelnes Abwaehlen; bereits gemeldeter Auftrag erscheint nicht als waehlbar.
- **TS-196 Teilfertigmeldung** – Teilfertig 3/10, 4/10, Fertig mit Rest, Ruecknahmen, Baugruppe Teil/Fertig (Buchung in BDE-Buchungen pruefen),
  Admin-Liste Art; nach Teilfertig bzw. Fertig per SQL auf DEV pruefen: `IstAbschliessend = 0` bzw. `1` (AK 18a, Antwort 16 – nur gegen SQL Server
  aussagekraeftig). Uebermeldung (Antwort 18, AK 15a): bei 7/10 Teilfertig 5 → gespeichert, Zeile offen, „gemeldet 12 von 10“, Hinweis „Sollmenge
  erreicht – bitte Fertig melden“ sichtbar; danach Fertig (Vorgabe 0) schliesst. Negativ: Teilfertig ohne Menge; zweites Fertig; paralleles Fertig von
  zwei Geraeten (nur eines gewinnt); Ruecknahme nach Frist.
- ~~**TS-197 FA-Sicht** – IDEAL-DEV: Anstempeln, Kopf-Buchungen, Pause/Fortsetzen, Teilfertig, Fertig mit Gruppenpflicht, Close-Others, Kollision,
  Taetigkeit, Scan (exakt/mehrdeutig/unbekannt), …~~ *(ueberholt, Antwort 19: geteilt in TS-197 (4a) und TS-198 (4b))*
- **TS-197 FA-Sicht Tabelle, Anstempeln, Standardsicht (Etappe 4a)** – IDEAL-DEV: FA-Reiter, Reihenfolge, fremd/eigen laufend, Anstempeln Ruesten/Produktion,
  Kollision als sichtbare Meldung (nichts gebucht), neue Auswahl „BDE-Standardsicht“ neben dem unveraenderten Schalter „BDE-Teileliste“, gemerkte Sicht,
  Werkbank ohne Kuerzel (Teile-Reiter deaktiviert), ~~Werkbank mit Kuerzel aber `BdeTeileliste` aus (Teile-Reiter mit Banner, Baugruppen-Teilmeldung
  wird gebucht)~~ *(ueberholt, Antwort 15)* Werkbank mit Kuerzel aber `BdeTeileliste` aus (Teile-Reiter deaktiviert mit Hinweis, nur FA; direkter POST
  auf `Melden` abgewiesen, AK 31), Pruefung nach SQL/169: vorbelegte Standardsicht an den bisherigen Teileliste-Werkbaenken. AKE-DEV (Worktree-Stand, nie main):
  BDE an einer Werkbank aktivieren, FA-Sicht oeffnen, keine Teile-Reiter, Anstempeln. Negativ: „Teile“ ohne Kuerzel bzw. ohne `BdeTeileliste`
  speichern; `BdeTeileliste` ausschalten bei Standardsicht „Teile“ (Fehler, nichts gespeichert); manipulierter
  POST „Teile“ an AKE; `workplaceId` einer nicht BDE-aktiven Werkbank (kein Redirect-Kreis); `BdeNurFaMeldung = true` auf DEV setzen → FA-Sicht
  unveraendert, Server-Abweisungen aus den NurFA-Alt-Zweigen sind erwartete Restabweichung (Antwort 22, AK 32c), danach zuruecksetzen.
- **TS-198 FA-Sicht Kopf-Buchungen, Dialoge, Scan, Taetigkeit (Etappe 4b)** – IDEAL-DEV und AKE-DEV (Worktree-Stand): Kopf-Buchungen laufend/pausiert,
  Teilfertig/Pause/Fertig/Fortsetzen, Detailangaben-Schalter (gemerkt), NurAg-„Fertig melden“ je Zeile mit Mengen-Dialog, Taetigkeit starten/beenden,
  Scan (exakt/mehrdeutig/unbekannt). Je ein Fall nach AK 26: Kollision (Dialog mit Werkername, Werkbank, Uhrzeit; nichts gebucht), Gruppenpflicht
  (Tabelle aller FAs der Gruppe, „Alle fertig melden“ bucht je FA die Menge mit `IsFinal` laut Haken), Close-Others (Liste der anderen Werker, „Ja, alle
  beenden“ beendet deren Buchungen). Negativ: Gruppen-Dialog „Abbrechen“ bucht nichts; Close-Others „Nein, nur meine“ laesst die anderen laufen;
  Doppelklick auf Start bucht nur einmal.
- ~~**TS-198 Tageshistorie und Abloesung**~~ *(umnummeriert, Antwort 19)* **TS-199 Tageshistorie und Abloesung (Etappe 5)** – Menue, Knopf, Inhalt (Buchungen + Teilmeldungen), Pagination; Aufruf `BdeTerminal/Index` an IDEAL- **und**
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
| 1 | Teile Einzeln: AG-Status (Status-Ladung und Symbol-Partial aus FA-Struktur herausgezogen), Kinder als Zeilen, Filter auf Kinder, Teile-Knopf vorn, BDE mobil kompakt (992 px, 44 px, Spaltenfilter in Karten nicht bedienbar – Antworten 8–10). Doku: Kapitel 17 Abschnitt 2 + Fotos `17-03`, Handy-Foto. v1.99.73, TS-194, keine Migration. | erledigt (2026-11-25, Umsetzung siehe unten) | `219a69c3` |
| 2 | Teile Summiert: Einzelauftraege aufklappbar, Summen-Haken, aggregierte Ampel ueber `Ableiten` (Antwort 11). Doku: Kapitel 17 Abschnitt 5 + Foto `17-06`. v1.99.74, TS-195, keine Migration. | erledigt (2026-11-25, Umsetzung siehe unten) | `eecc47ac` |
| 3 | Teilfertigmeldung: Migration + SQL/169 + FreshInstall, Service/Controller/Dialog, Ruecknahme, Waisen, Baugruppe `istFinal`, Admin-Spalte Art, ADR 0037. **Dieselbe Migration legt `ProductionWorkplaces.BdeStandardsichtTeile` an und belegt sie vor** (Antwort 3a; Model-Property ohne Leser bis Etappe 4a – Reihenfolge geprueft, siehe Etappe-3-Entwurf). Defaults beider Spalten nur in Migration/SQL/FreshInstall, kein `HasDefaultValue`, `IstAbschliessend` immer explizit gesetzt (Antwort 16, AK 18a). Index nach Antwort 12: wer zweitens baut, vereinigt den Filter mit SQL/164 – der Dev-Lauf prueft beim Start, ob SQL/164 schon im Branch liegt. Uebermeldung bei Teilfertig erlaubt mit Hinweis (Antwort 18, AK 15a). Doku: Kapitel 17 Abschnitte 3/4/6/7 + Fotos `17-05`, `17-07`. v1.99.75, TS-196. | erledigt (2026-11-25, Umsetzung siehe unten) | `94be5444` |
| 4 | ~~FA-Sicht an beiden Standorten: Wirksamkeits-Helfer … `TeilelisteWirksam` entfernt, `BdeBookingService` mit Master-Lesung, … Banner, …~~ *(ueberholt, Antwort 15)* FA-Sicht an beiden Standorten: Wirksamkeits-Helfer ohne NurFA (`SeiteVerfuegbar`, `TeilelisteWirksam(w)` ohne `nurFa`, `TeileSichtVerfuegbar` = Master && `TeilelisteWirksam`, `TeileStandard` ueber `BdeStandardsichtTeile`), Gate `BdeBookingService.cs:288` inhaltlich wie heute (ohne `nurFa`, ohne Master, kein neuer Ctor-Parameter), Zeilenquelle als gemeinsame Service-Methode, View `Fa`, AG-Status aller AG der Sub-FA (Antwort 4), Kopf-Buchungen, Modal-Partials, Scan, Standardsicht-Auswahl neben `BdeTeileliste` („Teile“ nur mit `BdeTeileliste`), Teile-Reiter nur mit `BdeTeileliste` (sonst deaktiviert mit Hinweis, kein Banner), gemerkte Sicht, Pagination statt MaxHauptFa (Antwort 14). Doku: Kapitel 17 (Maske, Reiter, Foto `17-01`) + Kapitel 11 FA-Sicht (neue Fotos). v1.99.76, TS-197, keine weitere Migration. *(ueberholt, Antwort 19: aufgeteilt in 4a und 4b)* | – | |
| 4a | FA-Sicht Grundgeruest an beiden Standorten (Antwort 19): Wirksamkeits-Helfer ohne NurFA (`SeiteVerfuegbar`, `TeilelisteWirksam(w)` ohne `nurFa`, `TeileSichtVerfuegbar` = Master && `TeilelisteWirksam`, `TeileStandard` ueber `BdeStandardsichtTeile`) samt Aufrufer-Umstellung, Gate `BdeBookingService.cs:288` inhaltlich wie heute (ohne `nurFa`, ohne Master, kein neuer Ctor-Parameter), Zeilenquelle als gemeinsame Service-Methode, View `Fa` mit Spalten/Filter/Pagination statt MaxHauptFa (Antwort 14), AG-Status aller AG der Sub-FA (Antwort 4), Anstempeln Ruesten/Produktion je Zeile (Kollision als sichtbare Meldung), Werkbank-Maske mit Standardsicht-Auswahl neben `BdeTeileliste` („Teile“ nur mit `BdeTeileliste`), Liste `ProductionWorkplaces`, Teile-Reiter nur mit `BdeTeileliste` (sonst deaktiviert mit Hinweis), gemerkte Sicht. AK 23, 24 (4a-Teil), 28–32d. Doku: Kapitel 17 (Maske, Reiter, Foto `17-01`) + Kapitel 11 FA-Sicht (Foto `11-05`). v1.99.76, TS-197, keine weitere Migration. | erledigt (2026-11-25, Umsetzung siehe unten) | `878aa76d` |
| 4b | FA-Sicht Bedienung (Antwort 19): Kopf-Buchungen laufend/pausiert mit Teilfertig/Pause/Fertig/Fortsetzen und Detailangaben-Schalter, `bde-fa-sicht.js`, Modal-Partials aus `Views/BdeTerminal/Index.cshtml` (Mengen, Kollision, Gruppe, Close-Others; beide Seiten nutzen sie bis Etappe 5), Scanzeile, Taetigkeit, NurAg-„Fertig melden“ je Zeile. AK 24 (4b-Teil), 25, 26, 27. Doku: Kapitel 11 Kopf/Dialoge/Scan (Foto `11-06`). v1.99.77, TS-198, keine Migration. | erledigt (2026-11-25, Umsetzung siehe unten) | `d61ea22c` |
| 5 | Tageshistorie inkl. Teilmeldungen (Seite, Knopf, NavZiel; Antwort 5), `BdeTerminal/Index` leitet an beiden Standorten immer um, alte View + `bde-terminal.js` geloescht, `BdeTerminalMaxHauptFa` entfernt, Katalogtext `BdeNurFaMeldung`, Betriebsdoku Handy-Konten (Antworten 1, 2, 6). Doku: Kapitel 11 umgeschrieben, Terminal-Fotos `11-01`–`11-04` ersetzt, Tageshistorie-Foto, Index-Titel 11/17 (Antwort 17). v1.99.78, TS-199, keine Migration (vor Antwort 19: v1.99.74, TS-195). | erledigt (2026-11-25, Umsetzung siehe unten) | `fbe29238` |

## Umsetzung Etappe 1 (2026-11-25)

Commit `219a69c3` im Worktree `feature/2026-08-07-ideal-teile-1-5` (auf v1.99.72 `4e8a344c`). **v1.99.73, TS-194, keine Migration.** Build 0 Fehler /
32 Warnungen (Basis), Tests Web 3636/1/0 (+5 `BdeTeilelisteAgStatusTests`), Dienst 847/0/0. qa-agent steht aus (nicht in diesem Lauf).
Plan `docs/superpowers/plans/2026-11-25-bde-masken-etappe-1-teile-einzeln.md` (Worktree). Changelog [[2026-11-25-v1-99-73-bde-masken-etappe-1-teile-einzeln]].

**Nummern-Umstellung** (TS-191–193 und v1.99.70–72 waren beim Start vergeben): E1 v1.99.73/TS-194 · E2 v1.99.74/TS-195 · E3 v1.99.75/TS-196/**SQL/169**/
**ADR 0037** · E4a v1.99.76/TS-197 · E4b v1.99.77/TS-198 · E5 v1.99.78/TS-199. Nachgezogen in Frontmatter `etappen:`, Test-Szenarien, Etappen-Tabelle
und Deploy-Tabelle; Fliesstext im Rumpf (z. B. „SQL/168“, „ADR 0036“, „TS-191“ in Etappe-Abschnitten) gilt mit dieser Zuordnung. Manifest-Info
als `v1.99.73-BdeTeilelisteAgStatusMobilInfo` in `scripts/deploy/nachlauf.psd1` eingetragen.

**Bauweise:**
- Status-Ladung an einem Ort: `Services/FaAgStatusService.cs` (`ZuordnenAsync` → `FaAgStatusLauf` mit Offsets/Rechner/Werkbänken), aus
  `FaHierarchyController` herausgezogen (vier Ctor-Parameter weniger); Symbol-Markup als `Views/Shared/_FaAgSymbole.cshtml` (FA-Baum unverändert).
- Teileliste: je Haupt-FA eine flache `FaHierarchyStructure` aus allen Zeilen, `FaArbeitsgangStatus.Zuordnen` **unverändert** wiederverwendet; die
  Teilmeldung als zweite Quelle ist ein eigener Schritt `BdeTeilelisteService.TeilmeldungenAnwenden` nur in der Teileliste – die FA-Struktur bleibt
  unberührt (Antwort 7). Teilmeldungen **aller** Kürzel (nicht stornierte FA-Meldungen) zählen; ein Token ohne Arbeitsgang, dessen Kürzel an einer
  Teileliste-wirksamen Werkbank steht, ist offen/überfällig (Soll aus FE-Termin), sonst „kein BDE-Arbeitsgang“.
- `TeilelisteZeile.Kinder` sind jetzt `TeilelisteZeile` (mit eigenen Symbolen, `Treffer`, Eltern `Aufgeklappt`); der Status wird **vor** dem
  Spaltenfilter berechnet → Spalte `ag-status` serverseitig filterbar (Text = `aria-label`, z. B. „KA – offen“).
- Sortieren: `bde-teileliste.js` hängt nach `table-sorted` jede `tr[data-eltern]` hinter ihre `tr[data-tl-eltern-id]`; `table-filter.js`,
  `column-preferences.js`, `site.css/js`, `_Pagination.cshtml` unverändert. Mobil nur unter `.bde-tl-seite` in `bde.css` (B1, B3, B6, B7).

**Abweichungen von der Spec:**
- E1-3: Ein Kind wird nur „Treffer“, wenn die Baugruppe selbst den Filter **nicht** erfüllt (sonst bei Haupt-FA-Filter jede Baugruppe aufgeklappt).
  Kennzeichen: sichtbarer Text „◆ Treffer“ (Abzeichen schwarz auf weiß) plus Balken links – statt nur visually-hidden-Text (stärker als gefordert).
- Lagerbestell-Zeilen (v1.99.72) zeigen die Zeichen neutral (kein Status – keine FA-Struktur).
- Die W-4-Mehrfach-Warnung wird nur in der FA-Struktur protokolliert, nicht in der Teileliste.
- Summiert bekam `data-label` und `label.tl-check` mit (Karten auch dort, sonst wäre die Kartenansicht dort ohne Beschriftung).
- Filter-Kurzfassung: „KW ww/jjjj bis …“ bzw. „alle Wochen“, „gemeldete sichtbar/ausgeblendet“, „n Spaltenfilter“ nur bei n > 0.

**Befunde:**
- CSV-Export (`csv-export.js`, alle Zeilen ohne `td[colspan]`) exportiert die Kinderzeilen mit – bewusst nicht geändert (gemeinsamer Code); in TS-194.2
  als erwartet beschrieben. Falls unerwünscht: eigenes Merkmal zum Ausschließen in `csv-export.js` (Folgearbeit, nicht gebaut).
- Index nach Antwort 12 für Etappe 3: im Bündel baut **SQL/165** (nicht SQL/164) `UX_BdeTeilMeldungen_Schluessel` mit Filter
  `[IsCancelled] = 0 AND [WarehouseRequisitionItemId] IS NULL` neu – SQL/169 muss diesen Filter um `IstAbschliessend` vereinigen.
- Kontrast (gerechnet): Kinder-Grund #eef1f4 mit Text #212529 ≈ 14:1; Treffer-Balken/Abzeichen #212529 auf Weiß 15,4:1; Filter-Knopf
  `btn-outline-secondary` 4,7:1 (Bootstrap). Messung im Browser steht für die QA aus.
- Fotos nicht aufgenommen: `docs/anwender/bilder/17-03-teileliste-einzeln.jpg` ersetzen, `17-08-teileliste-handy.jpg` (360 px) neu.

## Umsetzung Etappe 2 (2026-11-25)

Commit `eecc47ac` im Worktree `feature/2026-08-07-ideal-teile-1-5` (auf v1.99.73 `219a69c3`). **v1.99.74, TS-195, keine Migration.** Build 0 Fehler /
32 Warnungen (Basis), Tests Web 3641/1/0 (+5 `BdeTeilelisteSummiertTests`, vor dem Code geschrieben), Dienst 847/0/0. qa-agent steht aus.
Changelog [[2026-11-25-v1-99-74-bde-masken-etappe-2-teile-summiert]]. Manifest-Info `v1.99.74-BdeTeilelisteSummiertInfo` (IDEAL, Nachher).

**Bauweise:**
- `TeilelisteAggregat` bekommt `Einzelzeilen` (FE-Termin aufsteigend, ohne Termin zuletzt; Termin = Lagerbestell-Termin bzw. frühester FE-Termin der
  Haupt-FA) und `Arbeitsgaenge` aus `BdeTeilelisteService.AgAggregieren`: je Kürzel (Trim, ohne Groß/Klein) zählen nur Stände ungleich
  `null`/`KeinBdeArbeitsgang`; erledigt nur, wenn alle erledigt; läuft, wenn einer läuft; Soll = frühester Soll der nicht erledigten; Ergebnis über
  `ArbeitsgangStatusRegel.Ableiten` (Status-Regel bleibt an EINEM Ort; läuft + überfällig ergibt `LaeuftUeberfaellig`). Nur neutrale Stände → neutral.
- `Summieren` bekommt optional `heute` (Tests); der Controller setzt im Summiert-Zweig zusätzlich `vm.Spalten` (Haupt-FA/Abteilung/FE-Termin je Unterzeile).
- Statuszelle als Partial `Views/BdeTeileliste/_TeilStatus.cshtml` (vorher lokale Funktion in `Index.cshtml`) – Einzeln und Summiert-Unterzeilen.
- `Summiert.cshtml`: Knopf „Aufträge (n)“ in der Matchcode-Zelle (Bootstrap-collapse, Muster Etappe 1 `tr[data-tl-eltern-id]`/`tr.bde-tl-kind`), neue
  Spalte `ag-status` (auch in `ColumnDefinitions.BdeTeilelisteSummiert`), Unterzeile mit eigenem Haken nur bei meldbaren Aufträgen.
  Sortieren, Kartenansicht und CSS aus Etappe 1 greifen unverändert (kein CSS geändert).
- `bde-teileliste.js`: Summen-Haken `[data-gruppe]` setzt alle `[data-in-gruppe]`; Abwählen eines Auftrags nimmt den Summen-Haken zurück; der Zähler
  zählt Auftrags-Haken unter gesetztem Summen-Haken nicht (gesendet werden beide, der Server entdoppelt per `DistinctBy` in `MeldenAsync`).

**Abweichungen von der Spec:**
- Kein eigener „Fertig melden“-Dialog je Unterzeile: Teilmengen gehen über Auftrags-Haken (volle Sollmenge je Auftrag); den Mengen-Dialog bringt Etappe 3.
- Baugruppen-Teile werden in den Summiert-Unterzeilen nicht nochmals aufgeklappt (nur in Einzeln).
- Der Spaltenfilter `ag-status` wirkt wie die übrigen Positions-Spalten als Vorfilter je Auftrag (filtert Aufträge, nicht Gruppen nach dem Aggregat).

**Befunde:**
- CSV-Export Summiert enthält die Unterzeilen (wie Etappe 1, `csv-export.js` unverändert) – in TS-195.5 als erwartet beschrieben.
- Haken-Kopplung und Zähler sind reines JS, nicht xUnit-testbar → Manual-UAT TS-195.2.
- Hinweis für Etappe 3: `UX_BdeTeilMeldungen_Schluessel` hat im Bündel durch **SQL/165** den Filter `[IsCancelled] = 0 AND
  [WarehouseRequisitionItemId] IS NULL` – SQL/169 vereinigt ihn mit `IstAbschliessend`. Der Summen-Haken meldet heute immer volle Sollmenge;
  „teilgemeldet = läuft“ (Etappe 3) erbt `AgAggregieren` automatisch über die Einzelzeilen (Quelle bleibt `TeilmeldungenAnwenden`).
- Foto nicht aufgenommen: `docs/anwender/bilder/17-06-teileliste-summiert.jpg` ersetzen (aufgeklappte Gruppe mit Ampel).

## Umsetzung Etappe 3 (2026-11-25)

Commit `94be5444` im Worktree `feature/2026-08-07-ideal-teile-1-5` (auf v1.99.74 `eecc47ac`). **v1.99.75, TS-196, Migration
`20261125191311_AddBdeTeilMeldungIstAbschliessendUndStandardsicht`, SQL/169, ADR [[0037-teilfertigmeldung-nur-abschliessende-meldung-eindeutig]].**
Build 0 Fehler / 32 Warnungen (Basis), Tests Web 3655/1/0 (+14: 13 `BdeTeilelisteTeilfertigTests`, 1 `BdeTeilelisteLagerTests`), Dienst 847/0/0.
qa-agent steht aus. Changelog [[2026-11-25-v1-99-75-bde-masken-etappe-3-teilfertigmeldung]].

**Bauweise:**
- Modell: `BdeTeilMeldung.IstAbschliessend`, `ProductionWorkplace.BdeStandardsichtTeile` (ohne Leser). Kein `HasDefaultValue`; Defaults nur in
  Migration (`defaultValue: true` bzw. false + `UPDATE`-Vorbelegung), SQL/169 und FreshInstall (Schema + MigrationId).
- **Index-Entscheidung:** beide gefilterten UNIQUE (`_Schluessel` und `_Lagerbestellung`) mit `[IstAbschliessend] = 1`, den SQL/165-Filter
  `WarehouseRequisitionItemId IS [NOT] NULL` vereinigt (Antwort 12). SQL/169 baut nur neu, wenn `filter_definition` `IstAbschliessend` noch nicht
  enthält; Dubletten-Prüfung vorab mit WARNUNG + Prüfabfrage + NOEXEC; History nur, wenn beide Spalten und beide Filter passen.
- Service: `TeilelisteZeile.Meldungen` (alle, nach Zeit), `Abschluss`, `Gemeldet` (Summe Gut), `Rest`; `Meldbar` nur ohne Abschluss. Mengenregel
  `MengenFuerArtOk` an einem Ort für FA- und Lagerbestell-Zeilen. Doppelschutz: App-Prüfung auf abschließende Meldung + applock + Index.
- `TeilmeldungenAnwenden(..., teilgemeldet)`: nur Teilmeldungen → `Ableiten(false, laeuft: true, …)`; `AgAggregieren` erbt das ohne Änderung.
- Controller `Melden(..., art)`; Teilfertig mit mehr als einem Schlüssel → WarningMessage, nichts gemeldet. Block/Summiert: Restmenge, abschließend.
- Baugruppe: `IBdeBookingService.ReportCompletionCoreAsync(..., bool istFinal = true)` → `quantity.IsFinal`; `ReportCompletionAsync` unverändert.
- UI: Melden-Fenster mit zwei Knöpfen (btn-lg), Bestätigungsfenster als Bootstrap-Modal (Fokus auf Abbrechen) für Zurücknehmen und Block –
  capture-Listener vor der site.js-Doppelsende-Sperre, danach `requestSubmit`. Admin-Spalte `art` in `TeilmeldungSpalten` + `ColumnDefinitions`.
- Manifest `SQL/169` nach SQL/168 + Info `v1.99.75-BdeTeilfertigInfo`; Drift-Test `SchemaReihenfolge` ergänzt.

**Abweichungen von der Spec:**
- E3-1: Gut ist auch für „Teilfertig“ mit der Restmenge vorbelegt (ein Feld für beide Knöpfe) statt leer + Pflicht. Der Server erzwingt Gut > 0;
  eine versehentliche Übermeldung meldet der Hinweis. Falls „leer“ gewünscht: Vorbelegung in `bde-teileliste.js` per Knopf umschalten.
- Auch die Lagerbestell-Zeilen (ADR 0038) können Teilfertig (zweiter Index ebenfalls mit `IstAbschliessend = 1`) – sonst böte die Oberfläche
  Teilfertig an und der Index wiese es ab.
- Manifest-Info-Text etwas ausführlicher als die Deploy-Tabelle (Werker informieren, Browser-Cache).

**Befunde:**
- Baugruppe: „Fertig“ mit 0/0 nach Übermeldung weist `ReportCompletionCoreAsync` ab („Bitte eine Gut- oder Ausschussmenge angeben“); die
  Teilmeldung wird dann nicht gespeichert. Bei Blechteilen/Lagerbestellungen ist 0/0 als Abschluss erlaubt. Nicht geändert (Buchungsdienst) –
  in TS-196.7 Schritt 4 beschrieben; ggf. Entscheid Gerald.
- Die Tests wurden zusammen mit dem Code geschrieben; vor dem Code waren sie nicht ausführbar (neue Members, Kompilierfehler statt rot).
- Spec-Text „Vorbelegung durch SQL/168“ (Abschnitt Einstellungen) meint nach der Nummern-Umstellung SQL/169.
- Zusatzfund Doku: `docs/betrieb/03` Tabelle 5.2a hatte keine Zeile SQL/168 (vorbestehend, nicht nachgetragen).
- Fotos nicht aufgenommen: `docs/anwender/bilder/17-05-fertig-melden-fenster.jpg` (Melden-Fenster mit beiden Knöpfen) und
  `17-07-teilmeldungen-admin.jpg` (Spalte Art) ersetzen.

**Hinweise für Etappe 4a:** `TeilelisteWirksam`/`nurFa` unverändert; `BdeStandardsichtTeile` hat noch keinen Leser, steht nicht in
`EditableProperties` bzw. der Werkbank-Maske und nicht in der Liste `ProductionWorkplaces`. Neue Lesewege auf `BdeTeilMeldungen` (Tageshistorie
Etappe 5) beachten Fallstrick §69.

## Umsetzung Etappe 4a (2026-11-25)

Commit `878aa76d` im Worktree `feature/2026-08-07-ideal-teile-1-5` (auf v1.99.75 `94be5444`). **v1.99.76, TS-197, keine Migration.**
Build 0 Fehler / 32 Warnungen (Basis), Tests Web 3685/1/0 (+30), Dienst 847/0/0. qa-agent steht aus (nach allen Etappen).
Changelog [[2026-11-25-v1-99-76-bde-masken-etappe-4a-fa-sicht]].

**Bauweise:**
- Wirksamkeit an EINEM Ort `Services/WerkbankKuerzelFamilie.cs`: `TeilelisteWirksam(w)` (BdeAktiv + BdeTeileliste + Kürzel, ohne NurFA),
  `SeiteVerfuegbar(w)` (BdeAktiv), `TeileSichtVerfuegbar(w, hierarchisch)`, `TeileStandard(w, hierarchisch)`. Aufrufer: Gate
  `BdeBookingService` (ohne `nurFa`, ohne Master, kein Ctor-Parameter), `BdeTeilelisteService.MeldenAsync`, `BdeTerminalController.Index`
  (Redirect nur bei `TeileStandard`), `BdeTeilelisteController`.
- Zeilenquelle `Services/BdeFaAngebot.cs` (scoped): `LadenAsync(..., eigeneLaufendeAusblenden)` ist der aus `BdeApiController` gezogene Kern
  (API ruft mit `true`, JSON unverändert, `Ordnen` auf derselben Teilmenge); `FaSichtAsync` liefert alle Zeilen inkl. eigener laufender
  (`LaeuftBeiMir`), AG-Status aller WOs des Auftrags (Antwort 4) mit Markierung des Zeilen-AG (`FaArbeitsschrittSymbol.Markiert`, CSS
  `.fa-ag-markiert`), Knoten (SubFA = int(SubOrderNumber), SubFA ≠ 0, nicht verschwunden) und Kopfwerte. `Spalten` = ColumnMap für den Filter.
- `BdeTeilelisteController`: `SichtAufloesen` (gespeicherte/gewählte Sicht, ohne Angabe Standardsicht, Teile ohne Teile-Sicht → `fa`),
  Sicht in der gemerkten Ansicht; Zweig FA mit `ColumnFilterHelper.Apply` + `ColumnFilterTypes.For("BdeTeileliste/Fa")` + Pagination,
  View `Fa` (viewKey `BdeTeilelisteFa`, `ColumnDefinitions`); `NichtAktiv` statt Redirect-Kreis; `Melden` mit Gate `TeileSichtVerfuegbar`;
  neu `Anstempeln` (POST, Antiforgery, PRG) → `StartSetupAsync`/`StartProductionAsync`, Text über `AnstempelnText` (Kollision mit Werker,
  Werkbank, Uhrzeit, „nichts gebucht“).
- `_Kopf`: Reiter Teile Einzeln / Teile Summiert (nur hierarchisch; ohne Teile-Sicht `span.disabled` mit `aria-disabled` + Hinweis) / FA.
- Werkbank-Maske: Auswahl `BdeStandardsichtTeile` (FA/Teile) im Partial `_BdeTeilelisteField` (nur Master), Server-Validierung „Teile“ nur
  mit BdeAktiv + Kürzel + BdeTeileliste + Master (auch Ausschalten der Teileliste und AKE-POST), `EditableProperties`, ColumnMap/Kennzeichen
  „Standard Teile“. Der NurFA-Hinweis am Teileliste-Schalter entfällt (Option wirkt jetzt unabhängig).
- Manifest: `Pruefung/BdeNurFaMeldungAn` (beide, Vorher), `Pruefung/BdeStandardsichtOhneTeileSicht` (IDEAL, Nachher, Braucht SQL/169),
  `v1.99.76-BdeStandardsichtEinstellung`, `v1.99.76-BdeFaSichtAkeInfo`.

**Abweichungen von der Spec:**
- Die Werkbank-Auswahl im Kopf zielt jetzt auf `BdeTeileliste/Index` (statt Terminal), damit der Wechsel in der neuen Seite bleibt.
- AKE erreicht die FA-Sicht bis Etappe 5 nur über `/BdeTeileliste?workplaceId=…`; `BDE → Terminal` leitet nur bei `TeileStandard` um.
- AG-Status der FA-Sicht ohne Werkbank-Gate (alle WOs des Auftrags, Status aus `GetBdeStatusAsync`) – Vereinfachung.
- Haupt-FA-Zeilen (SubFA = HauptFA) treffen den Wurzelknoten; SubFA 0 (Blechteil) bewusst ohne Knoten.
- Werkbänke „nur AG-Fertigmeldung“ zeigen in 4a keinen Knopf (Fertig melden je Zeile = 4b).
- Manifest-Info AKE nennt zusätzlich den Aufrufweg.

**Befunde:**
- Tests zusammen mit dem Code geschrieben (neue Members → Kompilierfehler statt rot).
- Fotos nicht aufgenommen: `docs/anwender/bilder/17-01-werkbank-option.jpg` ersetzen (mit Auswahl BDE-Standardsicht), neu
  `docs/anwender/bilder/11-05-fa-sicht.jpg` (FA-Tabelle mit Rüsten/Produktion).

**Hinweise für Etappe 4b:** Zeilenquelle `BdeFaAngebot.FaSichtAsync`, Aktionsspalte in `Views/BdeTeileliste/Fa.cshtml` (heute Formular
Rüsten/Produktion, ausgeblendet bei `LaeuftBeiMir`/`NurAgFertigmeldung`); `BdeTeilelisteController.Anstempeln` ist PRG ohne JS – Dialoge,
Kopf-Buchungen, Scan, Tätigkeit und NurAg-„Fertig melden“ setzen dort an. `BdeTeilelisteViewModel.NurAgFertigmeldung` ist schon befüllt.

## Umsetzung Etappe 4b (2026-11-25)

Commit `d61ea22c` im Worktree `feature/2026-08-07-ideal-teile-1-5` (auf v1.99.76 `878aa76d`). **v1.99.77, TS-198, keine Migration.**
Build 0 Fehler / 32 Warnungen (Basis), Tests Web 3690/1/0 (+5), Dienst 847/0/0. qa-agent steht aus (nach allen Etappen).
Changelog [[2026-11-25-v1-99-77-bde-masken-etappe-4b-fa-buchungen]].

**Bauweise:**
- Kopf-Buchungen an EINEM Ort `Services/BdeKopfBuchungen.cs` (statisch, `LaufendeAsync`/`PausierteAsync` = die bisherigen Abfragen von
  `BdeTerminalController.ActiveBookings`/`PausedBookings`; das Terminal mappt daraus dasselbe JSON). `BdeFaAngebot.KopfBuchungenAsync` liefert
  beide Listen an die FA-Sicht; gerendert serverseitig in `Views/BdeTeileliste/_KopfBuchungen.cshtml` (nur Reiter FA, aus `_Kopf`).
- Dialoge als Partial `Views/Shared/_BdeDialoge.cshtml` (Mengen, Kollision, Gruppe, Close-Others; IDs unverändert), eingebunden in
  `Views/BdeTerminal/Index.cshtml` und `Views/BdeTeileliste/Fa.cshtml`. Tätigkeits-Modal des Terminals bleibt dort (bis Etappe 5).
- `wwwroot/js/bde-fa-sicht.js`: alle Buchungen über die bestehenden JSON-Aktionen `/BdeTerminal/*` (StartSetup, StartProduction, StartActivity,
  Pause, Resume, Finish, ReportPartial, ReportCompletion, FinishGroup, CloseOthers) mit Antiforgery und `wmsSperren`; Ergebnis-Auswertung
  Kollision → `collisionModal`, GroupFinishRequired → Gruppen-Modal, Erfolg mit anderen aktiven Buchungen → Close-Others, sonst sichtbare
  Meldung „… – nichts gebucht.“. Nach Erfolg Neuladen, Meldung über `sessionStorage` (`wms.bdeFaSicht.meldung`). Gut < Soll über
  `window.bdeBestaetigen` (Bestätigungsfenster aus `bde-teileliste.js`), kein Browser-Dialog. Anstempeln-Formular bleibt PRG-Fallback ohne JS.
- NurAg-Werkbänke: je Zeile „Fertig melden“ → `ReportCompletion` mit Sollmenge vorbelegt.
- Scan: Feld `#faScan` (einziges `data-hw-scan-ziel`), `/api/bde/resolve-scan?faSicht=true` – neuer Parameter `faSicht` ignoriert
  `BdeNurFaMeldung` (G5). Eindeutig → Zeile markieren (`?markiert=<woId>`, Controller blättert über `SeiteMitZeile`, sonst Warnung
  „… mit den aktuellen Spaltenfiltern nicht sichtbar“), mehrdeutig → Auswahl-Modal, NotFound/NotInScope → Meldung.
- Schalter „Zeiten und Details“ je Werker in `localStorage` (`wms.bdeKopfDetails.<operatorId>`), nur Anzeige.
- Manifest: `v1.99.77-BdeFaSichtBuchungenInfo` (beide, Info).

**Abweichungen von der Spec:**
- Kopf-Buchungen nur im Reiter FA, nicht in den Teile-Reitern (dort wird nicht angestempelt).
- Laufzeit = Stand Seitenaufbau, kein Live-Ticker.
- Scan „eindeutig“ schliesst bereits laufende Arbeitsgänge aus (Verhalten von `BdeScanResolver`) → Meldung „nicht gefunden (oder läuft bereits)“.
- Die Terminal-Modals bekamen `btn-lg` und `label for` (Barrierefreiheit), sonst unverändert.
- Die JSON-Aktionen nehmen `terminalId` weiter vom Client (wie am Terminal); die FA-Sicht übergibt den serverseitig ermittelten Terminal.

**Befunde:**
- Kein UI-Lauf lokal (DB nur AKESQL20, verboten) – Dialoge, Fokus und Kontrast prüft qa-agent/Manual-UAT (TS-198).
- Foto nicht aufgenommen: neu `docs/anwender/bilder/11-06-kopf-buchungen.jpg` (Meine Buchungen mit laufender und pausierter Buchung, Feld FA scannen).

**Hinweise für Etappe 5:** Tageshistorie gibt es nur im alten Terminal (`today-history`) – als eigene Seite bauen. Vom alten Terminal hängen noch ab:
`wwwroot/js/bde-terminal.js`, `Views/BdeTerminal/Index.cshtml` (inkl. `activityModal`), Test `HardwareScannerTests.BdeTerminal_BeideScanfelderSindZiele_OhneEigeneLogik`,
Redirects auf das Terminal. Die JSON-Aktionen von `BdeTerminalController` **bleiben** (die FA-Sicht bucht darüber), ebenso `_BdeDialoge`
und `BdeKopfBuchungen`. Beim Löschen der Terminal-View `BdeFaSichtViewTests` anpassen (prüft die Terminal-Einbindung).

## Umsetzung Etappe 5 (2026-11-25)

Commit `fbe29238` im Worktree `feature/2026-08-07-ideal-teile-1-5` (auf v1.99.77 `d61ea22c`). **v1.99.78, TS-199, keine Migration.**
Build 0 Fehler / 32 Warnungen (Basis), Tests Web 3690 gesamt (3689 bestanden, 1 übersprungen, 0 Fehler), Dienst 847/0/0.
qa-agent steht aus (alle Etappen gemeinsam). Changelog [[2026-11-25-v1-99-78-bde-masken-etappe-5-terminal-abloesung]].

**Bauweise:**
- Tageshistorie an EINEM Ort `Services/BdeTageshistorie.cs` (statisch, Präzedenz `BdeKopfBuchungen`): `LadenAsync(ctx, operatorId, tag)` kombiniert
  Buchungen des Werkers (nicht storniert, `StartedAt` am Tag, alle Werkbänke) und Teilmeldungen (`BdeTeilMeldungen`, nicht storniert, `GemeldetAm`
  am Tag). Spalten-Texte in `BdeTageshistorie.Spalten` (eine Quelle für View und Server-Spaltenfilter).
- **§69-Entscheidung:** ALLE Teilmeldungen des Tages, Typ nach `IstAbschliessend` („Fertig“/„Teilfertig“); FA- **und** Lagerbestell-Meldungen
  (§67, Präfix „Lagerbestellung · “). Eine Buchung, die als `BdeBookingId` an einer Meldung hängt (Baugruppen-Fertigmeldung), wird ausgeblendet –
  sonst stünde dieselbe Meldung doppelt da.
- Seite `BdeTeilelisteController.Tageshistorie` + `Views/BdeTeileliste/Tageshistorie.cshtml`: Gate wie `Index` (Terminal → Werkbank verfügbar →
  Werker), Server-Spaltenfilter (`ColumnFilterTypes.For("BdeTeileliste/Tageshistorie")`, dauer/gut/ausschuss numerisch), Pagination, ViewKey
  `BdeTeilelisteTageshistorie`, Karten am Handy über `.bde-tl-seite`.
- Knopf „Tageshistorie“ im Werker-Balken (`_Kopf.cshtml`, `btn-outline-secondary` – auf dem primären Balken weiss, 44 px); NavZiel
  `BdeTageshistorie` (Regel wie `BdeTerminal`, Symbol `SymVerlauf`), Layout-Link im BDE-Dropdown (ADR 0034).
- `BdeTerminalController.Index`: nur Login- und Terminal-Prüfung, dann immer `RedirectToAction("Index","BdeTeileliste", workplaceId ?? Default)`.
  Konstruktor ohne Werkbank-Repo und ServiceSettings. JSON-Aktionen, `StartProductionForOrder`, `_BdeDialoge`, `BdeKopfBuchungen` bleiben.
- Gelöscht (`git rm`): `Views/BdeTerminal/Index.cshtml`, `wwwroot/js/bde-terminal.js` (Verwendungen vorher per grep belegt).
- `BdeTerminalMaxHauptFa`: Key + Katalogeintrag entfernt (DB-Zeile bleibt, `/Settings` zeigt sie als verwaist). `BdeNurFaMeldung`-Text: „Wird von
  der BDE-Seite nicht beachtet, Altlast“.
- Manifest: `v1.99.78-BdeTerminalAbloesungInfo`, `v1.99.78-BdeHandyKontenInfo` (beide Firmen, Info, Nachher).
- Doku: Kapitel 11 umgeschrieben („BDE: Arbeitsgänge buchen“), Kapitel 17 Verweise, Index-Titel 11/17, Betrieb 09 § 4.1 „BDE am Handy“, 00-index.

**Tests:** neu `BdeTageshistorieTests` (3). Ersetzt: `BdeTerminalControllerTests.Index_HierarchischUndMaxHauptFa` →
`Index_LeitetImmerAufBdeSeiteWeiter` (2 Fälle) + `Index_OhneTerminal_Startseite_OhneAppUser_Login`. **Entfernt:**
`BdeTeilelisteServiceTests.Terminal_LeitetNurBeiStandardsichtTeileUm` (6 Fälle; Bedingung gibt es nicht mehr, Redirect immer) und
`HardwareScannerTests.BdeTerminal_BeideScanfelderSindZiele_OhneEigeneLogik` (View gelöscht). `BdeFaSichtViewTests` prüft jetzt, dass View und JS
weg sind; `AppSettingDefinitionsTests` Zählungen 40→39 / 70→69.

**Abweichungen von der Spec:**
- Quelle der Tageshistorie ist eine eigene Projektion über den DbContext (Action holt ihn per `[FromServices]`), nicht `GetHistoryAsync` –
  Buchungen und Teilmeldungen werden kombiniert, das kann `GetHistoryAsync` nicht.
- Ohne angemeldeten Werker zeigt die Tageshistorie die Anmeldung; nach dem Anmelden landet der Werker auf der Liste, nicht zurück in der Historie.

**Befunde:**
- Kein Gate-Test für `Tageshistorie` (der Controller wird in den Tests nicht konstruiert) – Manual-UAT TS-199.2/199.1 Negativfälle.
- Nach dem Löschen ohne Aufrufer (nicht entfernt, nur gemeldet): `BdeTerminal/ActiveBookings`, `PausedBookings`, `StartProductionForOrder`;
  API `api/bde/operator/{nr}`, `operator/{id}/active-booking`, `operator/{id}/today-history`, `available-operations/{wp}`.
- Fotos `11-02-liste-haupt-fa-gewaehlt.jpg` und `11-03-scan-und-knoepfe.jpg` sind nicht mehr referenziert (Dateien bleiben liegen).
- Fotos nicht aufgenommen: `11-01` (Anmelden BDE-Seite), `11-04` (Mengen melden), neu `11-07-tageshistorie.jpg`.

## Deploy

> [!success] QA-final 2026-11-25 (qa-agent) gegen den echten Diff `4e8a344c..fbe29238`
> **deploy.web = ja** (IdealAkeWms/), **deploy.service = ja** (IDEALAKEWMSService/AppVersion.cs + geteiltes EF-Modell), **deploy.migration = ja**
> (`IdealAkeWms/Migrations/20261125191311_AddBdeTeilMeldungIstAbschliessendUndStandardsicht`, SQL/169).
> **Paket:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` v1.99.78 (Web + Dienst, `SQL\`, `deploy\`, `publish-info.txt`).
> **Reihenfolge:** DB-Backup → Dienst stoppen → SQL/169 (Update-Skript, Schema-Phase) → Web → Dienst starten (neue Dienst-Binaries erst nach SQL/169).
> Alle zehn Manifest-Eintraege der Tabelle stehen in `scripts/deploy/nachlauf.psd1` (je 1 Treffer, geprueft). Keine Einmal-Skripte.
> Nach dem Merge nur dann neu aus `main` publishen, wenn der Merge getestete Dateien mit parallelen main-Aenderungen zusammengefuehrt hat.
> Ablauf Mensch: Publish aus dem Worktree → Testsystem → Test → Merge.

- **Web-App:** ja (alle Etappen).
- **Service:** kein Dienst-Code (Antwort 3a: Erkennung bleibt an `BdeTeileliste`); Versionsnummer in `AppVersion.cs` (beide). Weil der Dienst das
  EF-Modell des Web-Projekts teilt (`IDEALAKEWMSService.csproj:33`), kennt der neue Dienst-Build die Spalte `BdeStandardsichtTeile` → neue
  Dienst-Binaries **erst nach** SQL/169 starten (Regelreihenfolge des Update-Skripts: Schema vor Start).
- **Migration:** ja (Etappe 3, SQL/169, zwei Spalten + Index + Vorbelegung). Reihenfolge: SQL vor Web- und Dienst-Start. Der laufende alte Dienst
  stoert nicht (er kennt die neuen Spalten nicht; `BdeTeilMeldung` liest er nirgends – grep 2026-11-25 ohne Treffer).
- **Manifest-Eintraege** (`scripts/deploy/nachlauf.psd1`):

| Id | Version | Typ | Firma | Phase | Ausfuehrung | Text |
|---|---|---|---|---|---|---|
| `v1.99.73-BdeTeilelisteAgStatusMobilInfo` | 1.99.73 | Info | IDEAL | Nachher | – | „BDE-Teileliste: neue Spalte AG-Status, Baugruppen-Teile als Zeilen, kompakte Ansicht am Handy. Werker kurz informieren.“ (ersetzt den Info-Eintrag `v1.99.44-BdeTeilelisteMobilInfo` der ueberholten Mobil-Spec) |
| `v1.99.74-BdeTeilelisteSummiertInfo` | 1.99.74 | Info | IDEAL | Nachher | – | „Summiert: Auftraege aufklappbar, Haken auf Summenzeile meldet alle darunter fertig.“ |
| `v1.99.75-SQL169` | 1.99.75 | Schema | beide | Schema | automatisch | `SQL/169_AddBdeTeilMeldungIstAbschliessendUndStandardsicht.sql` – Spalte `IstAbschliessend` + gefilterter UNIQUE neu, Spalte `ProductionWorkplaces.BdeStandardsichtTeile` mit Vorbelegung bei Erstanlage; Reihenfolge nach SQL/164, falls diese im selben Paket ist (Antwort 12) |
| `v1.99.75-BdeTeilfertigInfo` | 1.99.75 | Info | IDEAL | Nachher | – | „Teilfertig/Fertig melden in der Teileliste; Teilfertig laesst die Zeile offen.“ |
| `Pruefung/BdeNurFaMeldungAn` | 1.99.76 | Pruefung | beide | Vorher | anzeigen | `SELECT COUNT(*) FROM dbo.AppSettings WHERE [Key] = N'BdeNurFaMeldung' AND LOWER(LTRIM(RTRIM([Value]))) IN (N'true', N'1', N'on')` – Treffer > 0: „BdeNurFaMeldung ist an; die neue BDE-Seite beachtet die Einstellung nicht (Antwort 2), der Buchungsdienst aber weiter in seinen Alt-Zweigen (Starts koennen abgewiesen werden, Antwort 22) – vor dem Update mit Gerald klaeren.“ Sichert die Restabweichung aus G5/AK 32c ab. |
| `Pruefung/BdeStandardsichtOhneTeileSicht` | 1.99.76 | Pruefung | IDEAL | Nachher | anzeigen | ~~`… WHERE BdeStandardsichtTeile = 1 AND (BdeAktiv = 0 OR ISNULL(LTRIM(RTRIM(ArbeitsschrittCode)), N'') = N'')`~~ *(ueberholt, Antwort 15)* `SELECT COUNT(*) FROM dbo.ProductionWorkplaces WHERE BdeStandardsichtTeile = 1 AND (BdeAktiv = 0 OR BdeTeileliste = 0 OR ISNULL(LTRIM(RTRIM(ArbeitsschrittCode)), N'') = N'')` – Treffer > 0: „Werkbaenke mit Standardsicht Teile ohne BDE aktiv, ohne BDE-Teileliste bzw. ohne Kuerzel – oeffnen FA; in den Werkbank-Stammdaten korrigieren.“ (Nach SQL/169 erwartet 0, weil die Vorbelegung dieselbe Bedingung nutzt.) |
| `v1.99.76-BdeStandardsichtEinstellung` | 1.99.76 | Einstellung | IDEAL | Nachher | – | „Werkbank-Stammdaten: neue Auswahl BDE-Standardsicht (Teile / FA) je BDE-Werkbank pruefen; vorbelegt mit Teile, wo bisher die Teileliste wirkte. Der Schalter BDE-Teileliste bleibt in der Maske unveraendert (steuert die Arbeitsgang-Erkennung) und schaltet die Teile-Sicht frei; ohne ihn nur FA-Sicht.“ |
| `v1.99.76-BdeFaSichtAkeInfo` | 1.99.76 | Info | AKE | Nachher | – | „BDE-Seite mit FA-Sicht gilt auch an AKE (derzeit ohne BDE-Nutzung); keine Aktion noetig.“ |
| `v1.99.78-BdeTerminalAbloesungInfo` | 1.99.78 | Info | beide | Nachher | – | „BDE-Terminal oeffnet an beiden Standorten die neue Seite mit Reitern; alte Terminal-Seite entfernt; Tageshistorie im Menue. Einstellung BdeTerminalMaxHauptFa entfaellt (in /Settings als verwaist sichtbar).“ |
| `v1.99.78-BdeHandyKontenInfo` | 1.99.78 | Info | beide | Nachher | – | „BDE am Android-Handy: je Geraet lokales WMS-Konto (Formular-Login) + Terminal-Zuordnung anlegen (docs/betrieb/09). Konten sind noch nicht angelegt.“ |

  SQL/169 ist ein Schema-Skript (kein Trockenlauf/Transaktion noetig; die Vorbelegung laeuft nur im Erstanlage-Zweig der Spalte). Keine Daten-/Einmal-Skripte.
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

## QA-Nachweis (qa-agent, 2026-11-25) – Epic gesamt, Etappen 1–5

Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`.
Review-Bereich `4e8a344c..fbe29238` (Etappe 1 bis 5; der im Auftrag genannte 557f1a9f ist ein Brain-Commit auf main).
QA-Commits im Worktree (nur Doku/Fotos): `6d98dd6a`, `a3287fb0`.

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 32 Warnungen (Bestand).
- `dotnet test`: **IdealAkeWms.Tests 3689 bestanden / 1 übersprungen / 0 fehlgeschlagen**; **IDEALAKEWMSService.Tests 847 / 0 / 0**.

### 2. Code-Review (selbst, synchron)
Geprüft: Controller BdeTeileliste/BdeTerminal/ProductionWorkplaces, BdeTeilelisteService (Melden, Abschluss, Block, Rücknahme), BdeFaAngebot,
BdeTageshistorie, WerkbankKuerzelFamilie, AppSettingDefinitions, NavigationKatalog, Migration + SQL/169 + FreshInstall, Views `_Kopf`, `_BdeTeilelisteField`,
FA-Sicht-/Teileliste-Partials, `bde-fa-sicht.js`, `bde-teileliste.js`, `bde.css`, geänderte Tests, `nachlauf.psd1`.
Gezielt auf die bekannten Defektklassen: keine `@Html.Raw` in `<script>` (Werte per `data-*`), kein `hidden` + `d-*`, keine stillen `?? 0` in neuen
Meldepfaden (Mengen serverseitig geprüft, „Gut > 0“ bei Teilfertig), keine Parallel-Array-Bindung (Block-Melden über Schlüsselstrings, Server
dedupliziert `BdeTeilelisteService.cs:567`), Standardsicht-Regel nur serverseitig in `WerkbankKuerzelFamilie` + Speicher-Validierung, gefilterte
UNIQUE-Indizes in Migration, SQL/169 und FreshInstall identisch.

Bekannte Befunde (nicht neu gezählt): F-20 nicht nachgemessen; F-21 **entschärft** – der Block meldet „mit ihrer Restmenge“ (Dialogtext; DB Gut 22 = Summe
Rest); F-22 **bestätigt** (Browser + DB); F-25 bestätigt aus Code (`BdeFaAngebot.cs:99` alle AG des Auftrags), keine Fehlwirkung beobachtet; F-26 bestätigt
(kein Timer in `bde-fa-sicht.js`); F-27 bestätigt (Scan 9999999); F-28 bestätigt, gilt auch für `operatorId` der Tageshistorie (Query); F-29 bestätigt
(ActiveBookings/PausedBookings/StartProductionForOrder und die API-Routen ohne Aufrufer in `wwwroot/js` und `Views`); F-31 bestätigt (Login-Formular
GET `/BdeTeileliste` → Liste statt Historie).

Neue Befunde:

| Nr | Schwere | Befund | Behandlung |
|---|---|---|---|
| N1 | niedrig | Kontrast 4,13:1 (< 4,5) für inaktive Reiter-Pills („Teile Summiert“) und den Outline-Knopf „Tätigkeit starten“ auf grauem Seitenhintergrund (16 px) | offen, Folgearbeit (CSS) |
| N2 | niedrig | Tageshistorie, Ziel-Text `BdeTageshistorie.cs:73`: Blechteile zeigen „FA … / Sub-FA 0“; Lagerbestell-Meldungen ergeben „Lagerbestellung · FA 0 / Sub-FA 0“ (aus Code, nicht im Browser gesehen) | offen |
| N3 | niedrig/Doku | Spaltenfilter Typ `Fertig` trifft auch „Teilfertig“ (enthält-Semantik); exakt mit `=Fertig` | TS-199.2.6 angepasst (QA-Commit) |
| N4 | niedrig | „Meine Buchungen“ zeigt nur Haupt-FA/AG ohne Sub-FA; zwei Buchungen derselben Haupt-FA nicht unterscheidbar (Parität altes Terminal) | offen |
| N5 | Doku | „Seitengröße 10“ in TS-194/197/198/199 nicht wählbar (kleinste 25); TS-197.2.3/197.5.2 und xUnit-Liste noch mit alter Terminal-Seite | korrigiert (`6d98dd6a`) |
| N6 | niedrig | Summen-Haken wird nach erneutem Anhaken aller Einzel-Haken nicht wieder gesetzt (Zähler stimmt) | offen |
| N7 | niedrig | Kollisionsdialog: Fokus auf dem Dialog statt auf „OK“ | offen |
| N8 | Doku | Bildverweis `17-08-teileliste-handy.jpg` zeigte auf fehlende Datei (17-08 = Isolierfräse, vgl. F-32) | auf neues Foto `17-13-teileliste-handy.jpg` umgestellt |
| N9 | Hinweis | Lokale DB `IDEAL_AKE_WMS` auf Instanz `.` (nicht DEV): beim ersten FreshInstall-Testlauf versehentlich 3 Tabellen angelegt (BfAuftraege, BfAuftragPositionen, SageFaArbeitsgangAnlagen); nicht gelöscht | Mensch entscheidet |
| N10 | Hinweis | `SQL/00_FreshInstall.sql` fehlt die History-Zeile `20260910081155_AddProductionOrderMatchcode` (Bestand, nicht aus diesem Epic) | offen |

### 3. Nachweisart je AK

| AK | Nachweis |
|---|---|
| 1, 3, 5, 6 | xUnit + DEV-Browser IDEAL (aria-labels, Kinderzeilen „nur Anzeige“) |
| 2 | xUnit (`BdeTeilelisteServiceTests`) + DEV-Browser (KA „läuft“ nach Teilfertig) |
| 4 | xUnit; im Browser nicht separat nachgestellt → Manual-UAT |
| 7, 8 | Manual-UAT, auf DEV gemessen: 360 px ohne Querscrollen (scrollWidth 360), Karten, Filterkarte eingeklappt mit Kurzfassung; Kontrast s. N1; unter 44 px: CSV 36 px, Zahnrad 36 px, Haken 19 px (Summiert) |
| 9 | überholt durch AK 35 (Terminal-Seite entfernt) |
| 10 | xUnit (Partial); FA-Struktur visuell → Manual-UAT |
| 11–14 | xUnit + DEV-Browser IDEAL (11 Aufträge, Summen-Haken, Block 11 Zeilen/Summe 22, Ampel „überfällig“) |
| 15, 15a, 17, 18, 20 | xUnit + DEV-Browser (Teilfertig 1; 0 abgewiesen; 2 → „Sollmenge erreicht“; Fertig 0; Rücknahme) |
| 16 parallel, 18a | **Manual-UAT gegen SQL Server**: DEV-DB `IstAbschliessend` 0/0/1 nachgewiesen; paralleles Doppel-Fertig nicht nachgestellt |
| 19 | DEV-Browser + DB: Buchung 25 `IsFinal=0`, 26 `IsFinal=1`; Rücknahme storniert 26 |
| 21 | DEV-Browser (`colf_art=Teil` → 3 Zeilen) |
| 22 | **Manual-UAT SQL**: SQL/169 je DEV-DB zweimal, 2. Lauf ohne Änderung; Vorbelegung IDEAL nur Kanten (67), AKE keine; FreshInstall auf Wegwerf-DB identisch |
| 23–25, 27 | xUnit (`BdeFaAngebotTests`, `BdeBookingServiceStufe1Tests`) + DEV-Browser IDEAL |
| 24 (4b NurAg) | DEV-Browser: EG mit `BdeNurAgFertigmeldung` → je Zeile nur „Fertig melden“, Vorgabe Soll 2, Abbrechen bucht nichts, Bestätigen → Zeile weg |
| 26 | Kollision DEV-Browser OK; Gruppenpflicht/Close-Others → Manual-UAT |
| 28 | xUnit (`BdeTeilelisteOptionTests` inkl. AKE-POST) + DEV-Browser IDEAL (Abweisungen, Modified*); AKE-Maske ohne Auswahl, manipulierter POST → Standardsicht bleibt 0 (DB) |
| 29, 30, 32d | xUnit + DEV-Browser (gemerkte Sicht nach Reload, Fallback FA, ausgegraute Reiter, „nicht BDE-aktiv“ ohne Redirect-Kreis) |
| 31, 32a | xUnit; 32a Dienst-Lauf → Manual-UAT |
| 32b | DEV AKE: nur Reiter FA, Kopf-Buchungen, Tätigkeit Start/Beenden, Tageshistorie; Fortsetzen der Alt-Pausen abgewiesen („FA … bereits abgeholt“, Alt-Daten); auf DEV AKE keine anbuchbaren AG (alle offenen AG haben IsFinal-Mengen) → Anstempeln an AKE Manual-UAT |
| 32c | DEV-Browser IDEAL mit `BdeNurFaMeldung=true`: FA-Sicht/Teile-Reiter unverändert (danach zurück auf false) |
| 33, 34 | DEV-Browser (Menü, Startseiten-Kachel mit Symbol, Knopf im Kopf, Spaltenfilter serverseitig, Pagination) |
| 35, 35a | xUnit (Redirect) + DEV-Browser beide Standorte; `/Settings` zeigt `BdeTerminalMaxHauptFa` unter „Nicht im Katalog“ |
| 35b | Manual-UAT (Android-Handy, Konten noch nicht angelegt) |
| 36 | Build + Tests grün (s. 1) |
| 37 | Fotos per Headless-Browser von DEV IDEAL, Kapitel 11/17 + Index nachgezogen (QA-Commits) |

### 4. Geänderte bestehende Tests

| Test | Einordnung |
|---|---|
| `AppSettingDefinitionsTests` (40→39, 70→69) | Verhalten bewusst geändert, AK 35a |
| `BdeBookingServiceStufe1Tests` (Gate ignoriert NurFA) | bewusst, G5 / AK 32c |
| `BdeTeilelisteLagerTests` (Meldung→Meldungen/Abschluss, + neuer Test) | Typ-/Modelländerung E3 |
| `BdeTeilelisteMeldenTests` (InlineData false,true) | bewusst, G5 |
| `BdeTeilelisteServiceTests` (Fixtures `IstAbschliessend=true`, Terminal-Umleitungstest entfernt) | Modell E3 + bewusst E5 / AK 35 |
| `HardwareScannerTests` (Terminal-Test entfernt, View gelöscht) | bewusst E5 |
| `BdeApiControllerTests`, `BdeTeilelisteOptionTests`, `BdeTerminalControllerTests`, `NachlaufManifestDriftTests` (+1) | additiv bzw. bewusst |

Keine Kategorie „sonstige“.

### 5. CLAUDE.md-Checkliste / ADR 0020
Migration + `SQL/169` idempotent (zweifach gelaufen) ✓ · FreshInstall Schema + MigrationId ✓ · AgentJobs nicht betroffen ✓ · Audit `Modified*` bei Maske
und Rücknahme (DB) ✓ · Version 1.99.78 in beiden `AppVersion.cs` ✓ · `Changelog.cshtml` 1.99.73–78 ✓ · Brain-Changelog je Version ✓ · TESTSZENARIEN
TS-194–199 ✓, im Index (Zeilen 168–173) ✓ · Manifest (10 Einträge) ✓.
ADR 0020: `BdeStandardsichtTeile` als Auswahl Teile/FA, Server-Validierung (abgewiesen ohne Teile-Sicht, AKE-POST), Hilfetext mit Wirkung/Default/Standort/
„nur Oberfläche“ ✓; `BdeTerminalMaxHauptFa` entfernt (verwaist sichtbar) ✓; `BdeNurFaMeldung` Text „Altlast“ ✓.

### 6. DEV-Deploy und Testlauf
`standort: beide`. `dev-deploy.ps1 -Standort AKE|IDEAL -Quelle <worktree>` erhöht, je **EXIT 0**, Fußzeile **v1.99.78** an 8081 und 8082.
Werkzeug: claude-in-chrome-Screenshots liefen in Timeouts („renderer may be frozen“) → Browser-Schritte per Headless-Chrome (CDP, Windows-Auth
localhost); claude-in-chrome-Tab geschlossen.

| Szenario | Ergebnis |
|---|---|
| TS-194 Einzeln (AG-Status, Baugruppe 25 Kinder, 360 px) | OK (AK 4 „Treffer“ nicht separat) |
| TS-195 Summiert | OK (N6) |
| TS-196 Teilfertig/Fertig/Rücknahme/Baugruppe/Admin-Art | OK; F-22 bestätigt |
| TS-197.1 Werkbank-Maske IDEAL | OK |
| TS-197.2/3 Reiter, Fallback, gesperrte Reiter | OK; Werker B startet bei Standard FA mit FA, Reload behält gewählten Reiter |
| TS-197.5 AKE | OK (nur FA; Maske ohne Auswahl; manipulierter POST → 0 bleibt); Anstempeln NICHT MOEGLICH (keine anbuchbaren AG auf DEV AKE) |
| TS-197.6 / 198.3.4 NurFa | OK (kurz true, zurück false) |
| TS-198.1–198.5, 198.8 (Scan, Details, Tätigkeit, Kollision) | OK (N7); Mengen-Hinweis bei Rüsten während Produktion = Parität |
| TS-198.3 nur AG-Fertigmeldung | OK (EG kurz an, zurück aus) |
| TS-198.6/198.7 Gruppenpflicht/Close-Others | NICHT MOEGLICH (Mehrfachbuchungs-Einstellungen nicht umgestellt) |
| TS-199.1 Weiterleitungen beide Standorte, Hilfe-Hinweis | OK |
| TS-199.2 Tageshistorie (Inhalt, Filter, Pagination, 360 px Karten, Menü, Kachel, AKE leer/Tätigkeit) | OK (N2, N3) |
| Ohne Terminal → Startseiten-Hinweis | NICHT MOEGLICH (kein zweiter Web-Benutzer) |

Umgeschaltet und zurückgesetzt (DEV IDEAL): `FaHierarchyNodes` 1963692 Arbeitsschritte `PU`→`PU KA`→`PU` (+ App-Pool-Recycle); Werkbank 67 Kanten
Standardsicht Teile→FA→Teile (Maske); Werkbank 65 `BdeNurAgFertigmeldung` 0→1→0; AppSetting `BdeNurFaMeldung` false→true→false.
DEV AKE: `BdeNurFaMeldung` war bereits true (unverändert). Angelegte Testdaten (bleiben): BdeOperators 3 „QA Werker B“ (9901), BdeActivities 1 `QA-REIN`,
Teilmeldungen 16–21 + 11 Block-Meldungen (Kanten), Buchungen ab 25 an EG/Kanten (keine offene Buchung mehr), AKE eine beendete Tätigkeit „Rüsten“ (A1).
Keine Sage-, Sync- oder Mail-Schalter berührt.

### 7. Publish
`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` – `IDEALAKEWMSWeb` + `IDEALAKEWMSWebService` (Ordner vorher geleert, je EXIT 0),
`SQL\` (inkl. 169), `deploy\` (Update-IdealAkeWms.ps1), `publish-info.txt` Version 1.99.78, Commit `a3287fb0` (Binaries aus `6d98dd6a`; danach nur Doku).

### Manuelle Test-Checkliste (Mensch)
1. Testsystem: Update-Skript mit dem Paket (Backup, SQL/169, Web, Dienst); Prüfungen `BdeNurFaMeldungAn` / `BdeStandardsichtOhneTeileSicht` ansehen.
2. TS-198.6 Gruppenpflicht und TS-198.7 Close-Others mit den Mehrfachbuchungs-Einstellungen.
3. AK 16 parallel: zwei Geräte gleichzeitig „Fertig“ auf denselben Schlüssel → eine Meldung, zweite „bereits gemeldet“.
4. AKE: Anstempeln Rüsten/Produktion und Fertig an einer Werkbank mit offenen AG (AK 32b).
5. Benutzer ohne Terminal: Startseiten-Hinweis.
6. AK 4 (Filter trifft nur ein Kind → aufgeklappt, „Treffer“) und AK 10 (FA-Struktur-Symbole unverändert).
7. AK 35b Android-Handy mit lokalem Konto (docs/betrieb/09).
8. AK 32a: Dienst-Lauf Struktur-Erkennung nach Standardsicht-Wechsel unverändert.
9. Entscheiden: N1–N4, N6, N7, F-20…F-31; Fotos 11-02/11-03 (F-30, unreferenziert, nicht gelöscht); lokale DB `IDEAL_AKE_WMS` (N9).

**Schlusslauf v1.99.79 (2026-11-25):** QA bestanden (F-26/F-27/F-28/F-31/F-34 bis F-37 an DEV OK, fremde terminalId abgewiesen; Hinweis H-A: Pause/Fertig/Teilmenge ohne Terminal-Pruefung, Bestand) - Nachweis [[2026-11-25-kommission-lagerplatz-bde-goal-lauf]], Abschnitt „QA Schlusslauf v1.99.79“.
