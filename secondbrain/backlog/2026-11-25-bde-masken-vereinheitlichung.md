---
typ: feature
status: offen
created: 2026-11-25
herkunft: "Anforderung Gerald 2026-11-25, Design-Gespräch mit Claude (brainstorming)"
standort: IDEAL
epic: true
---
# BDE-Masken vereinheitlichen: Teile- und FA-Sicht, AG-Status-Ampel, Teilfertigmeldung

Bezug: [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]] (Teileliste v1.99.32, Testbereit),
[[2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-spec]] (Terminal v1.64.0), ADR [[0030-teilmeldung-mit-fachlichem-schluessel-neben-workoperation]],
**aufgenommen:** [[2026-11-18-bde-vereinfacht-mobil-kompakt]] / [[2026-11-18-bde-vereinfacht-mobil-kompakt-spec]] (Gerald 2026-11-25: „wird in diesem Punkt bereits mit abgewickelt“).

Code-Belege: Bündel-Worktree `2026-08-07-ideal-teile-1-5`, Stand 2026-11-25.

## Anforderung (Gerald, Originaltext gekürzt)

- Teileliste und FA-Sicht vereinheitlichen; FA-Sicht sieht aus wie die Teile-Sicht, mit FA-spezifischen Feldern, alle FA-Felder im Zahnrad
  ein-/ausblendbar.
- „Aktuelle Buchungen“ in den Kopfbereich; Mitarbeiter- und KW-Block kleiner; Zeitstempel/Personal etc. in der obersten Zeile ein-/ausblendbar.
- Neue Spalte **AG-Status** in beiden Listen: erkannte Arbeitsgänge im Ampelsystem (rot/grün/überfällig …) wie in der FA-Struktur.
- Summiert: Summenzeile mit aggregiertem AG-Status; Einzelaufträge darunter aufklappbar mit eigenem AG-Status; Haken auf der Summenzeile wählt
  alle darunter; reicht die Menge nicht, wählt der Werker in der Unterebene, sortiert nach FE-Termin aufsteigend; nicht fertig gemeldete bleiben
  offen (AG gilt nicht als erledigt); **Teilfertigmeldung** muss möglich sein.
- FA-Sicht: Starten bleibt möglich; Anstempeln/Teil-/Fertigmelden/Pause im Kopf.
- Tageshistorie als eigener Menüpunkt und als Knopf im Kopf.
- Baugruppen: „Teile“-Knopf weiter nach vorn; darunterliegende Strukturebene in FA- und Teile-Sicht aufklappbar; Filter greift auch darauf;
  AG-Status auch dort.

Screenshots: ![[f1e1e625-7b5b-403c-aca0-055624a706ba.png]] (Baugruppe aufgeklappt, heute) ·
![[Screenshot 2026-11-25 143108.png]] (Terminal heute) · ![[Screenshot 2026-11-25 143220.png]] (Teileliste-Kopf = Vorbild FA-Sicht).

## Entscheidungen im Design-Gespräch (Gerald 2026-11-25)

1. **Teilfertigmeldung auch innerhalb eines Auftrags** (Teilmenge, Restmenge bleibt offen) → Datenmodell-Änderung, Migration, neuer ADR
   (superseded 0030 in diesem Punkt). Heute: genau eine Meldung je Schlüssel+Kürzel, gefilterter UNIQUE (`Data/ApplicationDbContext.cs:1577`),
   jede Meldung schließt die Zeile (`TeilelisteZeile.Meldbar`, `Services/BdeTeilelisteService.cs:47`).
2. **Kopf der FA-Sicht wie Teileliste bis unter die Kalenderwochen** (Screenshot 143220). Karten „1. Operator“ / „2. Arbeitsgang wählen“ entfallen.
3. **Standardsicht je Werkbank + Umschalten:** Reiter *Teile Einzeln · Teile Summiert · FA*; Werkbank-Stammdaten bekommen statt des Schalters
   `BdeTeileliste` (`Models/ProductionWorkplace.cs:58`) eine Auswahl „BDE-Standardsicht: Teile / FA“ (ADR 0020), **im bestehenden Feld** gespeichert.
4. **Bauweg A:** eine gemeinsame serverseitige Seite (Teileliste-Controller + Sicht „FA“), gleicher `_Kopf`, Zahnrad, Server-Spaltenfilter,
   Pagination (ADR 0005); Buchungsaktionen per kleinem JS gegen die **bestehende** BDE-API, danach Neuladen.
   Verworfen: B (FA-Sicht bleibt JS-gerendert in `bde-terminal.js`, nur optisch angeglichen) — doppelte Tabellenlogik, widerspricht ADR 0005.
5. **Etappen-Reihenfolge** wie unten (Datenmodell vor FA-Sicht).

## Vorschläge Claude (im Gespräch bestätigt mit „ja“, in der Spec als Entscheidung festhalten)

- **Fertig vs. Teilfertig ausdrücklich** (wie Terminal `bde-terminal.js:868`): „Teilfertig“ lässt die Zeile offen, „Fertig“ schließt sie —
  unabhängig von der Menge. Zeile zeigt „gemeldet X von Soll“. UNIQUE künftig nur noch für die abschließende Meldung.
- Baugruppe: Teilmeldung → Teil-Buchung am Sub-FA-Arbeitsgang statt sofortiger Fertigmeldung (heute `ReportCompletionCoreAsync`,
  `BdeTeilelisteService.cs:383`).
- Summenzeilen-Haken = volle Fertigmeldung aller offenen Aufträge darunter.
- **Ampel-Aggregation** je Kürzel: schlechtester Stand gewinnt (überfällig > läuft > offen > erledigt; erledigt nur wenn alle). Bei Teilen
  gilt „teilgemeldet“ als „läuft“ (Teile werden nicht angestempelt). Status-Regel nur über `ArbeitsgangStatusRegel` (ein Ort), Symbole aus
  `FaAgStatusDarstellung` (wie Terminal `Views/BdeTerminal/Index.cshtml` `#bdeAgZeichen`).
- Kopf FA-Sicht: aktuelle und pausierte Buchungen als kompakte Zeilen in der Werker-Leiste mit Teilfertig/Pause/Fertig/Fortsetzen;
  Zeit-/Detailangaben per Schalter ein-/ausblendbar (gemerkt). Anstempeln je Tabellenzeile.
- Scanfeld FA-Nr bleibt als schmale Zeile über der Tabelle (`data-hw-scan-ziel`, Hardware-Scanner).
- Zuletzt gewählte Sicht je Werker+Werkbank merken (wie die Filter). Werkbank ohne Kürzel → nur FA-Sicht, Teile-Reiter mit Hinweis aus.
- Baugruppen: „Teile (n) ▸“ direkt neben dem Matchcode; Kinder als eingerückte Tabellenzeilen mit denselben Spalten inkl. Ampel (nur Anzeige).
  Spaltenfilter trifft auch Kinder: Baugruppe mit passendem Kind bleibt, ist aufgeklappt, Trefferzeile markiert (nicht nur Farbe).

## Etappen

| # | Inhalt | Migration |
|---|---|---|
| 1 | Teileliste: Spalte AG-Status, Baugruppen-Unterebene als Zeilen, Filter auf Kinder, „Teile“-Knopf nach vorn; **plus BDE mobil kompakt** (Inhalt aus [[2026-11-18-bde-vereinfacht-mobil-kompakt-spec]], dessen offene Fragen hier neu beantworten) | nein |
| 2 | Summiert: Einzelaufträge aufklappbar (FE-Termin aufsteigend), Summen-Haken, aggregierte Ampel | nein |
| 3 | Teilfertigmeldung mit Restmenge; neuer ADR; Baugruppe als Teil-Buchung | **ja** |
| 4 | FA-Sicht als Tabelle im gemeinsamen Kopf; Buchungen im Kopf; Reiter + Standardsicht je Werkbank | nein (Bedeutung `BdeTeileliste` ändert sich) |
| 5 | Tageshistorie eigene Seite + Knopf; alte Terminal-Seite ablösen (bis dahin erreichbar) | nein |

## Für die Spec offen / zu prüfen

- BDE mobil kompakt ist Teil dieses Epics (Etappe 1, Gerald 2026-11-25). Kompakter Kopf gilt für **alle** Sichten inkl. FA-Sicht (Etappe 4);
  Kartenansicht/eingeklappte Filter mit den neuen Spalten (AG-Status, Baugruppen-Kinder) abstimmen. Offene Fragen der Mobil-Spec übernehmen.
- Etappe 4 muss alle Abläufe aus `bde-terminal.js` übernehmen: Kollisions-Modal, Gruppen-Fertigmeldung, Close-Others, ungeplante Tätigkeit,
  Scan-Disambiguierung (`/api/bde/resolve-scan`), Session-Ablauf, Einstellungen `NurFaMode`/`NurAgFertigmeldung`/`Hierarchisch`/`MaxHauptFa`.
- Was ist eine Zeile der FA-Sicht genau (Arbeitsgang je Sub-FA, gruppiert nach Haupt-FA) und welche FA-Felder ins Zahnrad.
- Bedeutungsänderung `BdeTeileliste` (heute „Teileliste wirksam“, `WerkbankKuerzelFamilie.TeilelisteWirksam`) inkl. `BdeNurFaMeldung` global.
- Rücknahme bei mehreren Teilmeldungen (heute je Meldung bis Schichtende, `RuecknahmeFristAsync`); Waisen-Prüfung mit Mehrfachmeldungen.
- Rechte/Rollen: keine neuen erwartet; Tageshistorie-Menüpunkt über `NavigationKatalog` (ADR 0034).
- `frontend-design`-Skill Pflicht; Kontrast WCAG AA, Ampel nie nur Farbe.
