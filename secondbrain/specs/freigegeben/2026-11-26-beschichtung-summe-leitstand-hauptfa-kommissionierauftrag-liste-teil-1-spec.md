---
type: spec
title: "Beschichtungsauftrag: Druck/PDF immer summiert je Artikel, Summenzeile „Summe Teile im Auftrag“"
slug: 2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-1-spec
status: Testbereit
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste]]"
uebersicht: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-uebersicht]]"
depends_on: ""
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - IdealAkeWms/Models/ViewModels/BeschichtungsauftraegeViewModels.cs (BeschichtungsauftraegeDruck um Summieren erweitern, BeschichtungsauftragPrintViewModel Zeilen + Summe)
  - IdealAkeWms/Controllers/BeschichtungsauftraegeController.cs (PrintModelAsync Z. 320-338, Zuweisung Z. 332)
  - IdealAkeWms/Views/Beschichtungsauftraege/Print.cshtml (Kommentar Z. 70, Tabelle Z. 71-108 summiert, tfoot Summenzeile, CSS Z. 29-35)
  - IdealAkeWms.Tests/Controllers/BeschichtungsauftraegeControllerTests.cs (Print_Uebergeben_JePosition_Reihenfolge_BarcodeNummer Z. 502-511 auf Zeilen umstellen; neue Unit-Tests Summiert)
  - IdealAkeWms/Views/Help/Index.cshtml (Z. 1600 „Seit v1.99.41 eine Zeile je Position“ auf summiert umstellen)
  - IdealAkeWms/Views/Help/Changelog.cshtml (neuer Eintrag)
  - docs/anwender/15-beschichtung-beschichtungsauftrag.md (Z. 126 „eine Zeile je Position“ auf summiert umstellen; Stand-Version README/00-index)
  - docs/TESTSZENARIEN.md (neues Kapitel; TS-162 Fall „je Position eine Zeile“ 162.14 Z. 13198 als überholt kennzeichnen; Querverweise Z. 9494 und Z. 12610 „Druck (je Position) TS-162“ auf das neue Kapitel nachziehen)
open_questions:
  - "1 (entschieden Gerald 2026-11-26: ja, Antwort 11 der Spec 2026-11-18 für Druck/PDF aufgehoben, immer summiert, Pos. laufend 1..n) KONFLIKT mit Freigabe-Antwort 11 der Spec 2026-11-18 (Druck je Position, Gerald 2026-11-18): wird sie für den Auftragsdruck aufgehoben, Druck/PDF also wieder summiert je Artikelnummer (ohne Umschalter)?"
  - "2 (entschieden Gerald 2026-11-26: ja, Summe der Mengen + Anzahl Positionen) Was zählt die Summenzeile: Summe der Mengen (Stück) oder Anzahl Positionen/Zeilen? Empfehlung: Summe der Mengen, Anzahl Positionen in Klammern."
  - "3 (entschieden Gerald 2026-11-26: Maße sollen einheitlich sein, bei Abweichung Hinweis „Maße uneinheitlich“; sonst S1-S4) Gruppierung und Zellen der summierten Zeile: Schlüssel Artnr + Kennzeichen ohne FA, Verpackt-Spalte ☑/☐/„n von m verpackt“, Pos. = laufende Nummer je Summenzeile, Maße der ersten Position mit Hinweis bei Abweichung - einverstanden?"
  - "4 (entschieden Gerald 2026-11-26: ja, AKE und IDEAL gleich) Gilt das für AKE und IDEAL (ein gemeinsames Drucklayout, wie R5 der Spec 2026-11-18) oder nur IDEAL? Backlog nennt IDEAL."
  - "5 (entschieden Gerald 2026-11-26: nein, Vorschlagsliste nicht mitziehen) Betrifft „immer summiert + Summenzeile“ nur den Auftragsdruck (BA-Nummer) oder auch den Druck der Vorschlagsliste Beschichtung (Liste/Summiert)?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "Antworten 1-5 + Nachfragen + Kritische Pruefung (Gerald 2026-11-26): immer summiert je Artikel, Pos. 1..n, ohne Artnr je Position eigene Zeile"
freigabe_von: Gerald
freigabe_am: 2026-11-26
---

## Ziel / Nutzen (das Warum)

Der Lackierer bekommt den Beschichtungsauftrag als Brief. Er will je Artikel eine Zeile mit der Gesamtmenge sehen und am Ende
die Gesamtzahl der Teile, die er zählen und zurückschicken muss. Heute druckt der Auftrag je Position eine Zeile ohne Summe
(seit v1.99.41, Spec [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]] A11 a). Bei vielen
gleichen Teilen in verschiedenen Sub-FAs wird der Brief lang und unübersichtlich.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

- Bildschirmdruck `Beschichtungsauftraege/Print/{id}` und PDF `Beschichtungsauftraege/Pdf/{id}` (eine View, ein Modell):
  Positionen **immer** summiert je Artikelnummer, kein Umschalter; Positionen ohne Artnr bleiben je eine eigene Zeile (S1).
- Summenzeile am Tabellenende „Summe Teile im Auftrag“.

**Out-of-Scope**

- Detailseite des Auftrags (bleibt je Position, dort wird gehakt und bearbeitet).
- Vorschlagsliste Beschichtung und ihr Druck (`FaHierarchyBeschichtung/Print`, `PrintSummiert`): unverändert, keine
  Summenzeile (Antwort 5, Gerald 2026-11-26). Bei Bedarf eigener Backlog-Eintrag.
- Statusregeln, Verpackt-Logik, Brief-Anschrift, Barcode, Kopfblock: unverändert aus v1.99.41.
- Neue Einstellung oder Standort-Weiche: keine (Antwort 1 „immer“ = ohne Schalter; Antwort 4: AKE und IDEAL gleich).

## Fachliche Anforderungen

### Ist-Stand (belegt; Pfade relativ zum Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, AppVersion 1.99.81, Zeilennummern am 2026-11-26 gegen v1.99.84 nachgeprüft: unverändert)

- Druck und PDF rendern dieselbe View `Print.cshtml` aus demselben Modell: `Print` und `Pdf` rufen beide `PrintModelAsync`
  (`Controllers/BeschichtungsauftraegeController.cs:297-318`); das PDF rendert `"Print"` per `IRazorViewRenderer` (`:310`).
- `PrintModelAsync` setzt `Positionen = BeschichtungsauftraegeDruck.Sortiert(a.Positionen)` (`:332`), also je Position.
- `BeschichtungsauftraegeDruck` kennt nur `IstDruckbar` und `Sortiert` (`Models/ViewModels/BeschichtungsauftraegeViewModels.cs:135-145`).
  Die frühere Summierung `BeschichtungsauftraegeDruck.Summiere` gibt es im Code nicht mehr (Suche `Summiere` im Web-Projekt:
  nur `SageSdataFaTreeBuilder.Summiere`, ein anderes Modul). Sie wurde mit v1.99.41 E5 (Commit `b1ce8608` laut QA-Nachweis der
  Spec 2026-11-18) entfernt. ~~Der Dev-Lauf kann sie aus dem Commit davor als Vorlage holen.~~ *(überholt, Kritische Prüfung
  SOLLTE 2)*: **nicht als Vorlage verwenden** - sie war nur ein Adapter auf `AggregateSummiert` mit Schlüssel `(HauptFA, Artnr)` ohne
  `OhneFa`, ohne Verpackt und mit alphabetischer Reihenfolge nach Artnr; Schlüssel und Reihenfolge weichen von S1/S3/S4 ab.
- `Print.cshtml:70-108`: Spalten Pos. (laufend `i + 1`, `:90`), Haupt-Artnr., Artnr. (+ Badge „ohne FA“, `:96`), Matchcode, Breite,
  Höhe, Tiefe, Menge (`FormatMass`, Format `0.###`, `:103`), Verpackt (`☑ verpackt` / `☐ offen`, `:104`). Kein `<tfoot>`, keine Summe.
- Sichtbare Spalten folgen der Detailtabelle (`PrintVisibleColumns.ResolveAsync` mit Katalog
  `ColumnDefinitions.BeschichtungsauftragPositionen`, `Controller :334-336`; `ShowCol` `ViewModels.cs:129-131`).
- Vorbild für die Aggregation einer Beschichtungsliste: `BeschichtungsauftragService.AggregateSummiert`
  (`Services/BeschichtungsauftragService.cs:209-242`): Schlüssel `(HauptFA, Artnr getrimmt)`, Summe `Sollmenge`, Eigenschaften und Maße
  der ersten Position, `MasseUneinheitlich`. Es arbeitet auf `FaHierarchyNode`, nicht auf `BeschichtungsauftragPosition`; es wird
  nicht wiederverwendet, nur das Muster.
- Summenzeilen gibt es in keiner Beschichtungs- oder Kommissionier-View (Suche `tfoot`/`Summe` in `Views/Beschichtungsauftraege`,
  `Views/FaHierarchyBeschichtung`, `Views/Picking`: nur Spaltenköpfe „Summe Sollmenge“).

### Konflikt mit der Spec 2026-11-18

Rückfrage 11 der Spec [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]] fragte „Druck je Position statt
summiert (hebt Teil-4-Antwort 6 auf)?“. Freigabe-Antwort 11: **Gerald 2026-11-18 12:55 „wie Empfehlung“ = je Position.** Die
Begründung damals: Positionsnummer und Haken je Position passen nicht zu einer Summenzeile mit gemischten Haken. Die neue Anforderung
dreht das zurück. ~~Diese Spec setzt voraus, dass Rückfrage 1 die Aufhebung bestätigt; sonst entfällt Teil 1 bis auf die Summenzeile.~~
*(überholt durch Antwort 1, Gerald 2026-11-26)*

**Entschieden (Antwort 1 + Nachfrage, Gerald 2026-11-26):** Antwort 11 der Spec 2026-11-18 ist für Druck und PDF des Auftrags
aufgehoben. Der Druck ist immer summiert je Artikel; die Spalte Pos. bleibt und zählt die gedruckten Zeilen laufend aufsteigend
(10 Zeilen = 1-10). Die Detailseite bleibt je Position (`Details.cshtml:289` nutzt weiter `Sortiert`).

### Soll

| ID | Anforderung |
|---|---|
| S1 | Druck und PDF zeigen **immer** je Artikel eine Zeile (Antwort 1). Schlüssel: `Artnr` getrimmt **und** `OhneFa` (freie Positionen nicht mit FA-Positionen mischen, Antwort 3). **Positionen ohne Artnr** (`string.IsNullOrWhiteSpace(Artnr)`; `Artnr` ist nullable, `BeschichtungsauftragPosition.cs:28`) werden **nicht** summiert: jede bleibt eine eigene Zeile (Schlüssel = Positions-Id; Antwort Gerald zur Kritischen Prüfung „Je Position eigene Zeile“). ~~leer = ein Schlüssel~~ *(überholt)* |
| S2 | Menge = Summe der Positionsmengen, Format `0.###` wie heute (`FormatMass`). Haupt-Artnr., Matchcode, Breite/Höhe/Tiefe aus der ersten Position in Druckreihenfolge (`Sortiert`). Die Maße einer Summenzeile sollen einheitlich sein (Antwort 3); weichen sie innerhalb der Zeile ab, wird trotzdem summiert und die Zeile trägt den Text „Maße uneinheitlich“ (gedruckt, nicht nur Farbe). Vergleich wie `AggregateSummiert` (`BeschichtungsauftragService.cs:237`): Breite, Höhe, Tiefe je gegen die erste Position. **Fester Ort des Hinweises:** zweite Zeile in der Verpackt-Zelle - die Spalte Verpackt ist immer sichtbar (kein `ShowCol`, `Print.cshtml:82/104`), der Hinweis kann also nicht durch ausgeblendete Maß- oder Artnr-Spalten verschwinden. |
| S3 | Pos. = laufende Nummer 1..n der **Summenzeilen** (Antwort 1 + Nachfrage: „Sind 10 Zeilen 1-10“). Reihenfolge = Reihenfolge der jeweils ersten Position in `Sortiert` (freie Positionen zuletzt). |
| S4 | Verpackt-Spalte je Summenzeile: alle verpackt → „☑ verpackt“; keine → „☐ offen“; sonst „◩ n von m verpackt“. Text immer dabei. Die Glyphe „◩“ (U+25E9) ist im Headless-Edge-PDF anders als ☑/☐ nicht erprobt: im PDF prüfen; wird sie nicht dargestellt (Ersatzkästchen), entfällt sie und es bleibt der Text „n von m verpackt“. |
| S5 | Summenzeile in `<tfoot>`: Beschriftung „Summe Teile im Auftrag“, Wert = Summe aller Mengen, dahinter „(m Positionen)“ mit m = Anzahl Auftragspositionen (Antwort 2); bei m = 1 Einzahl „(1 Position)“. Steht unter der Menge-Spalte; ist Menge ausgeblendet, steht der Wert in der Beschriftungszelle. `colspan` folgt den sichtbaren Spalten (`ShowCol`). |
| S6 | `<tfoot>` wiederholt sich im Druck nicht auf jeder Seite (`tfoot { display: table-row-group; }`), damit die Summe nur am Ende steht. |
| S7 | Kein Umschalter, keine Einstellung, keine Standort-Weiche: AKE und IDEAL drucken gleich (Antwort 4). Detailseite unverändert. |
| S8 | Druck der Vorschlagsliste Beschichtung (`FaHierarchyBeschichtung/Print`, `PrintSummiert`) bleibt unverändert (Antwort 5). |

## Technischer Loesungsentwurf

Ponytail-Leiter: keine neue Klasse, keine neue Datei außer Tests.

1. `BeschichtungsauftraegeDruck` (`ViewModels.cs:135`) bekommt eine reine Funktion `Summiert(IEnumerable<BeschichtungsauftragPosition>)`
   → `List<BeschichtungsauftragDruckZeile>` (record im selben File: `HauptArtnr`, `Artnr`, `Matchcode`, `OhneFa`, `Breite`, `Hoehe`,
   `Tiefe`, `Menge`, `Positionen`, `Verpackt`, `MasseUneinheitlich`). Intern erst `Sortiert(...)`, dann `GroupBy` mit
   Reihenfolge der ersten Position (LINQ `GroupBy` erhält die Quellreihenfolge). Gruppenschlüssel: bei gefüllter Artnr
   `(Artnr.Trim(), OhneFa)`, bei leerer Artnr die Positions-Id (eigene Zeile, S1). Die alte `Summiere` aus `b1ce8608^` nicht
   zurückholen (siehe Ist-Stand).
2. `BeschichtungsauftragPrintViewModel.Positionen` wird `Zeilen` (Typ Druckzeile); `Summe => Zeilen.Sum(z => z.Menge)`,
   `PositionenGesamt => Zeilen.Sum(z => z.Positionen)`. Der Kommentar `:118-119` („summiert je Artnr“) stimmt dann wieder.
   Der `<summary>`-Kommentar `:123` („v1.99.41 A11: je Position“) und der `Sortiert`-Kommentar `:140-141` („Rueckfrage 11 hebt
   Teil-4-Antwort 6 auf“) werden auf die neue Regel angepasst (Druck summiert seit dieser Spec; `Sortiert` bleibt für Detailseite
   und als Reihenfolge-Grundlage).
3. `PrintModelAsync` (`Controller :332`): `Zeilen = BeschichtungsauftraegeDruck.Summiert(a.Positionen)`. `Print` und `Pdf` teilen
   das Modell (`:297-318`), daher keine eigene PDF-Änderung.
4. `Print.cshtml`: Schleife über `Zeilen` (`:86-106`), Pos. = `i + 1` je Zeile, Zellen nach S2-S4, `<tfoot>` nach S5/S6. Kommentar
   `:70` auf die neue Regel ändern (v1.99.41-Hinweis als überholt markieren).
5. Frontend: Skill `frontend-design` ausdrücklich aufrufen (CLAUDE.md). Druckbild ruhig halten wie v1.99.42 (Linien nur
   horizontal, `Print.cshtml:21-31`); Summenzeile fett, obere Linie wie `thead`. Hinweis „Maße uneinheitlich“ als zweite Zeile
   in der Verpackt-Zelle (S2; Stil wie Badge `.ohne-fa`, `:34`), als Text lesbar auch im Schwarzweißdruck.
   ~~klein unter den Maßen bzw. am Artnr.~~ *(überholt, Kritische Prüfung SOLLTE 6)*. Einzahl/Mehrzahl „Position(en)“ in der
   Summenzeile nach S5.
6. Tests: `Print_Uebergeben_JePosition_Reihenfolge_BarcodeNummer` (`BeschichtungsauftraegeControllerTests.cs:502-511`, prüft heute
   `vm.Positionen` je Position) auf `vm.Zeilen` umstellen und umbenennen; `Druck_Sortiert_FreiePositionenZuletzt` (`:513-526`)
   bleibt. Neue reine Unit-Tests für `Summiert` (AK 10).

## Migrations-/SQL-Auswirkungen

Keine. Reine Darstellung, kein Schema, keine Daten. `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt.

## Einstellungen

keine.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Der Druck liest nur (`GetDetailAsync`), es wird nichts geschrieben.

## Akzeptanzkriterien

1. Auftrag mit drei Positionen derselben Artnr (Mengen 2, 3, 5) und einer anderen Artnr (Menge 1): Druck zeigt zwei Zeilen,
   Mengen „10“ und „1“, Pos. 1 und 2.
2. Summenzeile zeigt „Summe Teile im Auftrag 11 (4 Positionen)“ (Summe der Mengen + Anzahl Positionen, Antwort 2); Auftrag mit
   genau einer Position zeigt „(1 Position)“.
3. Gemischte Haken in einer Summenzeile (2 von 3 verpackt): Verpackt-Zelle „◩ 2 von 3 verpackt“; alle → „☑ verpackt“; keine → „☐ offen“.
   Im PDF ist die Zelle lesbar (keine Ersatzkästchen; sonst Glyphe weglassen, Text bleibt, S4).
4. Freie Position (ohne FA) mit derselben Artnr wie eine FA-Position: zwei getrennte Zeilen, die freie mit Badge „ohne FA“ und zuletzt.
5. Positionen derselben Artnr mit verschiedenen Maßen: bleiben eine Summenzeile; sie zeigt die Maße der ersten Position und den
   Text „Maße uneinheitlich“ als zweite Zeile in der Verpackt-Zelle. Bei gleichen Maßen erscheint der Text nicht. Sind Breite,
   Höhe, Tiefe und Artnr. über die Spaltenauswahl ausgeblendet, steht der Hinweis trotzdem in der Verpackt-Zelle.
6. Spalte Menge über die Spaltenauswahl der Detailseite ausgeblendet: Summenzeile zeigt den Wert trotzdem, Tabelle bleibt bündig.
7. Mehrseitiger Ausdruck: Kopfzeile wiederholt sich, Summenzeile steht nur einmal am Ende.
8. PDF (`/Beschichtungsauftraege/Pdf/{id}`) zeigt dieselben Zeilen und dieselbe Summe wie der Bildschirmdruck; ☑, ☐ und ◩ (bzw.
   der Text ohne ◩) sind im PDF lesbar.
9. Detailseite des Auftrags zeigt weiter jede Position einzeln mit eigenem Haken.
10. Unit-Test `Summiert`: Gruppierung, Reihenfolge, Verpackt-Zählung, `MasseUneinheitlich`, leere Artnr (null, "" und nur
    Leerzeichen) je Position eine eigene Zeile, freie Positionen getrennt.
11. Build und alle Tests grün.
12. Gleiches Druckbild an AKE und IDEAL (DEV beider Standorte): summiert, Pos. 1..n, Summenzeile; keine Standort-Weiche im Code.
13. Druck der Vorschlagsliste Beschichtung (`/FaHierarchyBeschichtung/Print`, `/FaHierarchyBeschichtung/PrintSummiert`) unverändert, ohne Summenzeile.
14. Pos.-Spalte: Auftrag mit 10 Summenzeilen zeigt Pos. 1 bis 10 lückenlos aufsteigend, unabhängig von den Positionsnummern der Auftragspositionen.
15. Zwei Positionen ohne Artnr (verschiedener Matchcode, Mengen 2 und 3): Druck zeigt zwei eigene Zeilen mit Menge „2“ und „3“ und
    eigenem Matchcode, keine Zusammenfassung; beide zählen in der Summenzeile mit.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md`; die TS-Nummer legt erst der Dev-Lauf fest (nächste freie beim Dev-Start; Stand
2026-11-26 wäre es TS-205, TS-203/204 sind vergeben, `docs/TESTSZENARIEN.md:14760/:14819`; Teil 2 und 3 laufen parallel).
~~Stand jetzt nächste freie TS-203~~ *(überholt)*. Index in `secondbrain/tests/testszenarien-index.md`. In TS-162 den Fall
„je Position eine Zeile“ (162.14, `:13198`; AK 16 der Spec 2026-11-18) als überholt kennzeichnen, nicht löschen; die
Querverweise `:9494` und `:12610` („Druck (je Position) TS-162“) auf das neue Kapitel zeigen lassen.

- **Vorbedingung:** Beschichtungsauftrag im Status Übergeben mit mindestens 4 Positionen, davon 3 mit gleicher Artnr aus
  verschiedenen Sub-FAs, eine freie Position (ohne FA), gemischte Verpackt-Haken.
- **x.1** Drucken → Zeilen summiert, Pos. laufend, Summenzeile (AK 1, 2).
- **x.2** Verpackt-Zelle bei gemischten Haken (AK 3).
- **x.3** Freie Position gleicher Artnr getrennt und zuletzt (AK 4).
- **x.4** Maße uneinheitlich → Hinweis in der Verpackt-Zelle; danach Breite/Höhe/Tiefe und Artnr. ausblenden und erneut drucken →
  Hinweis weiter sichtbar (AK 5).
- **x.5** Menge-Spalte ausgeblendet, dann drucken (AK 6).
- **x.6** Langer Auftrag (> 1 Seite) drucken (AK 7).
- **x.7** PDF gleich Druck, Zeichen ☑/☐/◩ im PDF lesbar (AK 3, 8).
- **x.8** Auftrag mit 10 verschiedenen Artikeln (bzw. 10 Summenzeilen) → Pos. 1-10 lückenlos (AK 14).
- **x.9** Gleiche Schritte x.1-x.7 auf DEV AKE und DEV IDEAL (AK 12).
- **x.10** Auftrag mit zwei Positionen ohne Artnr → zwei eigene Zeilen, nicht summiert (AK 15).
- **x.11** Auftrag mit genau einer Position → Summenzeile „(1 Position)“ (AK 2).
- **Negativ:** Auftrag im Entwurf → Druck abgelehnt wie heute; Detailseite unverändert je Position (AK 9); Auftrag mit gleichen
  Maßen je Artikel → kein Hinweis „Maße uneinheitlich“ (AK 5); Vorschlagsliste-Druck unverändert ohne Summenzeile (AK 13).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | (kein Epic) | – | – |

## Deploy

*Final (QA 2026-11-26) aus dem echten Diff `e67e9f6f`: geändert nur `IdealAkeWms/` (Controller, ViewModel, `Print.cshtml`, Hilfe,
Changelog, AppVersion), `IDEALAKEWMSService/AppVersion.cs` (nur Versionsnummer), Tests, `docs/`. Keine Datei unter `*/Migrations/`, kein `SQL/`.*

- **Web-App:** ja (fachlich die einzige Komponente)
- **Service:** nein (nur Versionsnummer 1.99.87; im Bündel trotzdem gemeinsam ausgerollt)
- **Migration:** nein
- **Manifest-Einträge (`scripts/deploy/nachlauf.psd1`):** keine nötig - kein SQL, kein Einmal-Skript, keine Einstellung, keine Leitstand-Aktion.
- **Publish:** erledigt im QA-Lauf nach `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\`
  (IDEALAKEWMSWeb + IDEALAKEWMSWebService + SQL + deploy, `publish-info.txt` Version 1.99.87, Commit `e67e9f6f`).
  Ablauf Mensch: Publish aus dem Worktree → Testsystem (`deploy\Update-IdealAkeWms.ps1`) → testen → Merge.
- Nach dem Merge nur dann neu aus `main` publishen, wenn der Merge getestete Dateien mit parallelen main-Änderungen kombiniert hat:
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`.
- Reihenfolge: keine Besonderheit (kein Backup-Zwang durch diese Spec, keine DB-Änderung).

## Offene Rueckfragen

> [!success] Alle fünf entschieden (Gerald 2026-11-26, Chat; siehe Freigabe-Antworten + Nachfrage). Im Rumpf eingearbeitet:
> 1 → S1/S3/Konflikt-Abschnitt, 2 → S5/AK 2, 3 → S1/S2/AK 5, 4 → S7/AK 12/`standort: beide`, 5 → Out-of-Scope/S8/AK 13.
> Keine neuen offenen Punkte.
> Kritische Prüfung 2026-11-26: SOLLTE 1 entschieden (Gerald: „Je Position eigene Zeile“ → S1/AK 10/AK 15), SOLLTE 2-7 eingearbeitet
> (Ist-Stand, Test-Szenarien-Nummer, affected_code, S5/AK 2, S2/AK 5, S4/AK 3/AK 8).

1. **Konflikt mit Freigabe-Antwort 11 (Spec 2026-11-18).** Dort wurde der Auftragsdruck am 2026-11-18 auf **je Position**
   umgestellt (hob Teil-4-Antwort 6 „summiert“ auf). Wird Antwort 11 jetzt für Druck und PDF des Auftrags aufgehoben, also immer
   summiert je Artikelnummer, ohne Umschalter? **Empfehlung:** ja, wie Backlog („immer Summiert“). Die Detailseite bleibt je Position,
   dort wird gehakt. Nein hieße: Druck bleibt je Position, nur die Summenzeile kommt dazu.
2. **Inhalt der Summenzeile.** „Wie viele Teile waren im Auftrag drauf“ kann die Summe der Stückzahlen oder die Zahl der Zeilen bzw.
   Positionen meinen. **Empfehlung:** Summe der Mengen als Hauptwert, Anzahl Positionen in Klammern: „Summe Teile im Auftrag 47
   (12 Positionen)“. Mengen mit Bruchteil (z. B. 2,5) werden mitsummiert und mit Bruchteil gezeigt.
3. **Zellen der Summenzeile je Artikel.** Schlüssel Artnr **und** „ohne FA“ getrennt; Verpackt „☑ verpackt“ / „☐ offen“ /
   „◩ n von m verpackt“; Pos. = laufende Nummer je Summenzeile; Maße der ersten Position plus Text „Maße uneinheitlich“ bei
   Abweichung (wie die Summiert-Vorschlagsliste). **Empfehlung:** ja, so (S1-S4).
4. **Standort.** Das Drucklayout ist für AKE und IDEAL dasselbe (R5 der Spec 2026-11-18: beide gleich). Ein IDEAL-only-Verhalten
   bräuchte eine Weiche (z. B. Master `ProduktionsauftragHierarchisch`). Gilt die Summierung für beide? **Empfehlung:** beide, ein
   Layout. Der Backlog nennt IDEAL; an der AKE ändert sich der Brief dann ebenfalls.
5. **Vorschlagsliste mitziehen?** Die Vorschlagsliste Beschichtung hat schon einen summierten Druck (`FaHierarchyBeschichtung/PrintSummiert`,
   ohne Summenzeile). Soll sie auch eine Summenzeile bekommen? **Empfehlung:** nein in diesem Teil, nur der Auftragsdruck; bei Bedarf
   eigener Backlog-Eintrag.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Konflikt Antwort 11 (Spec 2026-11-18): Druck wieder immer summiert? → **ja bitte positionsnummerierung beibehalten** (Gerald 2026-11-26, Chat)
2. Summenzeile: Summe der Mengen + Anzahl Positionen? → **ja** (Gerald 2026-11-26, Chat)
3. Zellen je Summenzeile (Schlüssel, Verpackt, Pos., Maße) wie S1-S4? → **muss immer einheitlich sein. Ansonsten gern uneinheitlich** (Gerald 2026-11-26, Chat)
4. Standort: AKE und IDEAL gleich? → **ja** (Gerald 2026-11-26, Chat)
5. Vorschlagsliste-Druck mitziehen? → **nein** (Gerald 2026-11-26, Chat)

**Nachfrage 2026-11-26 (Gerald, Chat) zu 1:** „Die Tabelle soll einfach eine Spalte Pos. mit laufender aufsteigender Positionsnummer
haben. Sind 10 Zeilen 1-10“ — also Druck summiert, Pos. = laufende Nummer je Summenzeile (wie S3).

**Nachfrage 2026-11-26 (Gerald, Chat) zu 3:** Auslegung bestätigt – „Korrekt, es sollte immer das gleiche Maß sein, somit sollte das
nie ein Problem werden“: eine Summenzeile je Artikel, bei Abweichung Hinweis „Maße uneinheitlich“, keine eigene Zeile je Maß.

## Kritische Pruefung (2026-11-26)

Geprüft gegen den Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (AppVersion 1.99.84). Zeilenangaben der Spec
stichprobenartig nachgeprüft und zutreffend: `Controller :297-318/:320-338/:332`, `ViewModels.cs:118-145`, `Print.cshtml:70-108`,
`BeschichtungsauftragService.cs:209-242`, Test `:502-511`. `BeschichtungsauftragPrintViewModel` wird nur von Controller, View und
diesem einen Test benutzt (Suche über `.cs`/`.cshtml`), die Umbenennung `Positionen` → `Zeilen` hat also keine weiteren Leser.

**Antworten:** alle fünf beantwortet; die knappen Antworten 1 („positionsnummerierung beibehalten“) und 3 („muss immer einheitlich
sein. Ansonsten gern uneinheitlich“) sind durch die zwei Nachfragen eindeutig gemacht und in S1-S5, AK 1-14 und Out-of-Scope
eingearbeitet. Kein Widerspruch zwischen Antwortblock und Rumpf gefunden.

### BLOCKER

- keine. Begründung: Alle Rückfragen sind entschieden und im Rumpf angekommen, kein Schema, kein Fremdsystem, nur eine View plus eine
  reine Funktion; die Befunde unten lassen sich im Dev-Lauf oder mit einer kurzen Ergänzung klären.

### SOLLTE

1. **Leere Artnr nicht zusammenfassen (Frage an Gerald).** S1 übernimmt „leer = ein Schlüssel“ aus `AggregateSummiert`. `Artnr` ist
   nullable (`BeschichtungsauftragPosition.cs:28`, `FaHierarchyNode.cs:36`). Im Brief an den Lackierer würden so verschiedene Teile
   ohne Artnr zu einer Zeile mit Mengensumme und dem Matchcode der ersten verschmelzen. Vorschlag: Positionen mit leerer Artnr
   bleiben je eine eigene Zeile (Schlüssel = Positions-Id). Frage: **Einverstanden, oder sollen leere Artnr bewusst zusammengefasst
   werden?**
2. **Vorlage aus `b1ce8608^` taugt nicht.** Die frühere `Summiere` (`git show b1ce8608^:…/BeschichtungsauftraegeViewModels.cs:141-149`)
   war nur ein Adapter auf `AggregateSummiert`: Schlüssel `(HauptFA, Artnr)` ohne `OhneFa`, kein Verpackt, Reihenfolge alphabetisch
   nach Artnr (`OrderBy(a => a.Artnr)`, `Service.cs:240-241`) statt nach erster Position. Wer sie zurückholt, verletzt S1, S3, S4 und
   AK 4. Vorschlag: den Satz „der Dev-Lauf kann sie aus dem Commit davor als Vorlage holen“ streichen oder mit „nicht als Vorlage,
   Reihenfolge und Schlüssel weichen ab“ ersetzen.
3. **TS-Nummer veraltet.** „nächste freie TS-203“ stimmt nicht mehr: TS-203 (v1.99.82) und TS-204 (v1.99.83) sind in
   `docs/TESTSZENARIEN.md:14760/:14819` schon vergeben. Vorschlag: „nächste freie Nummer beim Dev-Start (Stand 2026-11-26: TS-205)“;
   Teil 2 und 3 laufen parallel, die Nummer erst im Dev-Lauf festlegen.
4. **Weitere Stellen mit „je Position“ nachziehen.** Nicht in affected_code: `docs/anwender/15-beschichtung-beschichtungsauftrag.md:126`
   („eine Zeile je Position“), `Views/Help/Index.cshtml:1600` („Seit v1.99.41 eine Zeile je Position“), sowie zwei Querverweise in
   `docs/TESTSZENARIEN.md:9494` und `:12610` („Druck (je Position) TS-162“). Vorschlag: in affected_code aufnehmen (DoD verlangt den
   `docs/anwender`-Nachzug ohnehin).
5. **Singular in der Summenzeile.** AK 2 nennt nur „(4 Positionen)“. Ein Auftrag mit einer Position druckt sonst „(1 Positionen)“.
   Vorschlag: AK 2 ergänzen „bei m = 1: (1 Position)“.
6. **Ort des Hinweises „Maße uneinheitlich“ eindeutig festlegen.** Lösungsentwurf 5 sagt „unter den Maßen bzw. am Artnr.“. Sind
   Breite/Höhe/Tiefe **und** Artnr per Zahnrad ausgeblendet, hat der Hinweis keinen Platz und verschwindet still (gegen „Melden
   statt still behandeln“). Vorschlag: fester Ort in einer immer sichtbaren Spalte, z. B. in der Verpackt-Zelle oder in der Pos.-Zelle
   als zweite Zeile; AK 5 um den Fall „Maße-Spalten ausgeblendet“ ergänzen.
7. **Glyphe „◩“ im PDF prüfen.** ☑/☐ sind im Headless-Edge-PDF erprobt, U+25E9 nicht. Fehlt sie in der Schrift, steht im PDF ein
   Kästchen. Vorschlag: AK 3/AK 8 ausdrücklich „im PDF lesbar“ prüfen lassen; der Text „2 von 3 verpackt“ trägt die Bedeutung ohnehin.

### HINWEIS

- Gruppierung ist wie bei `AggregateSummiert` (Tupel-Gleichheit, `Service.cs:221`) groß/klein-sensitiv; „ab-1“ und „AB-1“ wären zwei
  Zeilen. In der Praxis kommt die Artnr aus Sage bzw. dem Artikelstamm, Schreibweise einheitlich zu erwarten.
- `HauptFA` steht nicht im Schlüssel. Belegt unkritisch: alle Schreibwege setzen die Positions-HauptFA auf die des Auftrags
  (`BeschichtungsauftragWorkflow.cs:125/213/470/698/710`).
- Matchcode und Haupt-Artnr kommen aus der ersten Position; bei gleicher Artnr innerhalb eines HauptFA sind beide gleich zu erwarten,
  ein Abweichen wird nicht gemeldet (bewusst nur Maße, Antwort 3).
- Leerer Auftrag im Druck ist praktisch ausgeschlossen (Übergeben/Versendet ohne Positionen abgewiesen, `Workflow.cs:515-521`;
  Entfernen auf 0 Positionen geprüft, `:280`). Summenzeile „0 (0 Positionen)“ wäre trotzdem korrekt.
- Ab Übergeben sind Haken und Menge noch änderbar (Hilfe `Index.cshtml:1576-1578`); der Druck zeigt den gespeicherten Stand zum
  Druckzeitpunkt, also kann „n von m verpackt“ auf einem früheren Ausdruck anders lauten. Kein Handlungszwang.
- Größe: 4 Code-Dateien + Tests + Hilfe/Changelog/Doku, eine Schicht (Darstellung). Gut in einem Dev-Lauf schaffbar.
- Deploy: nur Web, keine Migration, kein Manifest - plausibel. Kein Schreibzugriff, keine echten Daten entstehen; der Brief geht
  allerdings an einen externen Lackierer, AKE-Briefe ändern sich mit (Antwort 4, so gewollt).
- Version für die Änderung ist nicht genannt (Bündel bei v1.99.84, Teil 2/3 parallel) - im Dev-Lauf vergeben.

**Empfehlung:** BEREIT ZUR FREIGABE (SOLLTE 1 vorher kurz beantworten; SOLLTE 2-7 kann der Dev-Lauf ohne Rückfrage einarbeiten).

**Antwort Gerald 2026-11-26 (Chat) zur Kritischen Prüfung (Positionen ohne Artnr):** „Je Position eigene Zeile“ – Positionen ohne
Artikelnummer werden nicht summiert, jede bleibt eine eigene Zeile.

## QA-Nachweis (qa-agent, 2026-11-26)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, geprüfter Commit-Bereich
`300dbf25..e67e9f6f` (ein Commit `e67e9f6f`), AppVersion 1.99.87 (Web + Service). Build und Tests über den ganzen Worktree.

### 1. Build und Tests

- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 33 Warnungen (vorbestehend, u. a. CS8620 in `SageLagerplatzAnlageControllerTests`).
- `dotnet test`: **IdealAkeWms.Tests 3799 bestanden / 1 übersprungen / 0 Fehler (3800)**; **IDEALAKEWMSService.Tests 848 / 0 / 0 (848)**.

### 2. Code-Review (selbst, in diesem Lauf)

Gelesen: Diff `e67e9f6f` komplett (Controller `PrintModelAsync`, `BeschichtungsauftraegeDruck.Summiert` + Record `BeschichtungsauftragDruckZeile`,
`Print.cshtml` inkl. `thead`/`tfoot`/colspan, Hilfe, Changelog, Tests, `docs/`), dazu `PrintVisibleColumns.ResolveAsync`, `Details.cshtml:289`
(nutzt weiter `Sortiert`), Entität `BeschichtungsauftragPosition` (Menge/Maße `decimal` nicht nullable, `Artnr` nullable).

- Gruppenschlüssel `object`: Positions-Referenz (leere Artnr) bzw. geboxtes `(string, bool)`-Tupel - `GroupBy` nutzt `Equals` des geboxten
  ValueTuple, also Wertgleichheit; ein Referenz-Schlüssel ist nie gleich einem Tupel. Korrekt (Unit-Tests + DEV belegen).
- colspan: `vorMenge` = Pos. + sichtbare der sechs Spalten vor Menge; mit Menge: `vorMenge` + Menge-Zelle + leere Verpackt-Zelle, ohne Menge:
  `vorMenge` + leere Zelle - Spaltenzahl stimmt in beiden Fällen mit `thead` überein (gleiche `ShowCol`-Reihenfolge). Auf DEV beide Varianten bündig.
- Regel an einem Ort: Summierung nur in `Summiert`; Summe/Positionen als berechnete Properties aus den Zeilen, keine zweite Implementierung (JS/SQL).
- Fehlerklassen der Agent-Checkliste: kein `<script>`-Embedding, kein `hidden`+`d-*`, kein stiller Fallback (`Artnr?.Trim()` nur Anzeige,
  Menge nicht nullable), keine Model-Bindung/Arrays, keine Browser-only-Validierung (reine Lese-Ansicht).

**Befunde:** keine Blocker, keine Sollte.
- *Hinweis H-1 (vorbestehend, nicht behoben):* Ist die Spalte Artnr. ausgeblendet, fehlt auch das Badge „ohne FA“ (sitzt seit v1.99.41 in der
  Artnr-Zelle). Die freie Zeile ist dann nur über die Reihenfolge (zuletzt) von einer gleichnamigen FA-Zeile zu unterscheiden (DEV AKE BA-2026-00005
  mit `visibleColumns=hauptartnr,matchcode,menge`: Zeilen 1 und 5 beide „QA Teil A“). Kein Spec-Verstoß (S1-S8 fordern das nicht); bei Bedarf Backlog.
- *Hinweis H-2:* Abweichende Matchcodes/Haupt-Artnr. innerhalb einer Summenzeile werden nicht gemeldet - in der Spec bewusst so (Kritische Prüfung, HINWEIS 3).

### 3. Nachweisart je AK

| AK | Nachweis |
|---|---|
| 1 | automatisiert `Druck_Summiert_GruppiertJeArtnr_ReihenfolgeErstePosition`, `Print_Uebergeben_Summiert_…`; DEV OK |
| 2 | Summe/Positionen automatisiert (`Print_Uebergeben_Summiert_…`); Text „(1 Position)“/„(7 Positionen)“ ist Razor → Manual-UAT, DEV OK |
| 3 | Verpackt-Zählung automatisiert (`Druck_Summiert_VerpacktZaehlung`); Zelltext + ◩ im PDF Manual-UAT, DEV OK |
| 4 | automatisiert `Druck_Summiert_FreiePositionGetrenntUndZuletzt`; Badge Manual-UAT, DEV OK |
| 5 | `MasseUneinheitlich` automatisiert; Ort in der Verpackt-Zelle bei ausgeblendeten Spalten Manual-UAT, DEV OK |
| 6, 7, 8, 12, 13 | Manual-UAT (View/Druck/PDF), DEV OK (13: Vorschlagsliste-Views nicht im Diff, kein `tfoot`) |
| 9 | Manual-UAT, DEV OK (Detail zeigt 7 Einzelpositionen) |
| 10 | automatisiert (`Druck_Summiert_*`, 5 Tests) |
| 11 | Build/Tests grün (s. o.) |
| 14 | Manual-UAT, DEV OK (Auftrag mit 45 Zeilen, Positionsnummern 10..450 → Pos. 1..45) |
| 15 | automatisiert `Druck_Summiert_OhneArtnr_JePositionEigeneZeile`; DEV OK |

### 4. Geänderte bestehende Tests

- `Print_Uebergeben_JePosition_Reihenfolge_BarcodeNummer` → `Print_Uebergeben_Summiert_Reihenfolge_BarcodeNummer`: **Verhalten bewusst durch
  die Spec geändert** (S1-S3/AK 1, hebt v1.99.41 A11 auf). Fixture `Anlegen` unverändert; die neue Erwartung (A1 4 Stück aus 2 Positionen,
  Maße uneinheitlich wegen Tiefe 3/4) folgt aus der bestehenden Fixture. Sonst nur neue Tests.

### 5. Checkliste CLAUDE.md / 5a ADR 0020

- Migration/SQL/FreshInstall/AgentJobs: nicht betroffen (kein Schema). Audit-Felder: nicht betroffen (nur lesen).
- Version 1.99.87 in **beiden** `AppVersion.cs`, Anwender-Changelog `Views/Help/Changelog.cshtml` ergänzt, Hilfe `Index.cshtml` nachgezogen.
- `docs/TESTSZENARIEN.md` TS-205 (205.1-205.11 + Negativfälle), TS-162.14 als überholt markiert, Querverweise TS-84/TS-145 nachgezogen;
  `docs/anwender/15-…` + `docs/anwender/README.md` Stand-Version. Testindex Zeile 205 im Hauptcheckout vorhanden.
- Brain-Changelog/feature-map: nicht Teil dieses QA-Commits (macht der Bündel-Lauf, wie bei v1.99.86).
- ADR 0020: keine neue oder geänderte Einstellung im Diff.

### 6. DEV-Deploy und Testlauf

`standort: beide` (Frontmatter vorhanden). Deploy in erhöhter Shell, Quelle = Worktree, Web + Dienst:
- **AKE:** `dev-deploy.ps1 -Standort AKE` → „HTTP 200, Fusszeile v1.99.87“, „Fertig: AKE v1.99.87 auf http://localhost:8081/“, Exit 0.
- **IDEAL:** `dev-deploy.ps1 -Standort IDEAL` → „HTTP 200, Fusszeile v1.99.87“, „Fertig: IDEAL v1.99.87 auf http://localhost:8082/“, Exit 0.

Testdaten (DEV-DB per SQL, Bemerkung „QA TS-205 …“, CreatedBy `QA`): je Standort ein Szenario-Auftrag im Status Übergeben
(QA-ART-A ×3 Mengen 2/3/5, Haken 2 von 3, Tiefe 3/4/3; QA-ART-B 1 verpackt; zwei Positionen ohne Artnr (NULL und Leerzeichen) Mengen 2/3;
freie Position QA-ART-A 1) und ein langer Auftrag (45 Artikel, Positionsnummern 10..450, Mengen 1..45).
AKE: **BA-2026-00005** (Id 5, Szenario), **BA-2026-00006** (Id 6, lang). IDEAL: **BA-2026-00012** (Id 12), **BA-2026-00013** (Id 13).

| Schritt | Durchgeführt | Ergebnis | Status |
|---|---|---|---|
| 205.1 | Print/5 (AKE), Print/12 (IDEAL) | QA-ART-A einmal Menge 10, QA-ART-B 1, Pos. 1-5, fett „Summe Teile im Auftrag 17 (7 Positionen)“ unter Menge | OK |
| 205.2 | dto. | „◩ 2 von 3 verpackt“, „☑ verpackt“, „☐ offen“ | OK |
| 205.3 | dto. | freie QA-ART-A als Pos. 5 zuletzt mit Badge „ohne FA“ | OK |
| 205.4 | Print/5 normal und `?visibleColumns=hauptartnr,matchcode,menge` | „Maße uneinheitlich“ umrandet als 2. Zeile in Verpackt, auch ohne Artnr-/Maß-Spalten; QA-ART-B ohne Hinweis | OK (Ausblenden per Query-Parameter; Zahnrad-Weg nutzt denselben `ResolveAsync`, bleibt Mensch) |
| 205.5 | Print/5 `?visibleColumns=hauptartnr,artnr,matchcode,breite,hoehe,tiefe` | „Summe Teile im Auftrag 17 (7 Positionen)“ in der Beschriftungszelle, Tabelle bündig | OK |
| 205.6 | PDF BA-6 (AKE) / BA-13 (IDEAL), 2 Seiten, per Windows.Data.Pdf gerendert | Kopfzeile auf Seite 1 und 2, Summenzeile nur einmal am Ende von Seite 2 („1035 (45 Positionen)“ = 45·46/2) | OK (PDF); Browser-Druckvorschau Mensch |
| 205.7 | PDF BA-5 (AKE), BA-12 (IDEAL) gerendert | gleiche Zeilen und Summe wie Bildschirmdruck; ☑ ☐ **◩ korrekt dargestellt** (eingebettet aus Segoe UI Symbol, ToUnicode 25E9), kein Ersatzkästchen | OK |
| 205.8 | Print/PDF BA-6/BA-13 | Pos. 1..45 lückenlos trotz Positionsnummern 10..450 | OK |
| 205.9 | AKE + IDEAL | identisches Druckbild (nur Briefkopf/Empfänger standortabhängig) | OK |
| 205.10 | BA-5/BA-12 | „QA ohne Artnr X“ 2 und „QA ohne Artnr Y“ 3 als eigene Zeilen, zählen in 17 mit | OK |
| 205.11 | Print/4 (AKE), Print/11 (IDEAL) | „2 (1 Position)“ bzw. „1 (1 Position)“ | OK |
| Neg. Entwurf | kein Entwurf-Auftrag auf DEV; automatisiert `Print_Entwurf_Warnung_RedirectDetail` (unverändert) | Abweisung per Test belegt | OK (Test), Browser Mensch |
| Neg. Detailseite | Details/5 (AKE) | 7 Einzelpositionen mit eigenem Haken | OK |
| Neg. gleiche Maße | BA-5 Zeile QA-ART-B, IDEAL BA-7 | kein Hinweis | OK |
| Neg. Vorschlagsliste | `FaHierarchyBeschichtung/*` nicht im Diff, kein `tfoot`/„Summe Teile“ in den Views | unverändert | OK (Code); Sichtprüfung Mensch |
| Neg. Schwarzweiß | PDF-Render | Hinweis und Verpackt-Stand als Text, Badges mit Rahmen | OK |

Keine DEV-Schalter berührt, nichts gebucht, kein Sage-Zugriff.

### 7. Publish

`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` neu befüllt (Ziele vorher geleert): `IDEALAKEWMSWeb`,
`IDEALAKEWMSWebService` (beide Exit 0), `SQL`, `deploy`; `publish-info.txt`: Version 1.99.87, Commit `e67e9f6ff215ca14862ae457bf8955518db7ee8c`,
Branch `feature/2026-08-07-ideal-teile-1-5`, Erstellt 2026-11-26 20:28, Komponenten IDEALAKEWMSWeb + IDEALAKEWMSWebService.
Manifest: kein Eintrag nötig (keine Deploy-Schritte).

**Ergebnis: Testbereit.**

### Manuelle Test-Checkliste (Mensch)

1. TS-205.4/205.5 über das **Zahnrad der Detailseite** (gespeicherte Spaltenauswahl) statt Query-Parameter wiederholen.
2. TS-205.6 **Browser-Druckvorschau** (Edge und Chrome) mit BA-2026-00006 (AKE) bzw. BA-2026-00013 (IDEAL): Kopf je Seite, Summe einmal.
3. TS-205.7 PDF auf dem **Testsystem-Server** (anderer Headless-Edge/andere Schriften): ◩ lesbar, sonst Fallback laut S4.
4. Negativfall Entwurf im Browser (Auftrag im Entwurf → Drucken/PDF abgewiesen).
5. Vorschlagsliste `/FaHierarchyBeschichtung/Print` und `/PrintSummiert` kurz ansehen: ohne Summenzeile.
6. TS-205 komplett am Testsystem AKE und IDEAL mit echten Aufträgen, Brief an den Lackierer gegenlesen.
7. Optional H-1 entscheiden (Badge „ohne FA“ bei ausgeblendeter Artnr-Spalte).

## Freigabe-Nachtrag N-1 (Gerald 2026-11-26, Chat, nach Fotos v1.99.87)

Frage Gerald: „Warum 6 (4 Positionen)? … wären meines Erachtens 2 Positionen. Gleiches mit 17 (7), da hätte ich 5.“ Die Klammer
zählte die Auftragspositionen vor dem Summieren. Entscheidung Gerald: **„Nur Teilesumme“** — die Summenzeile zeigt nur noch
„Summe Teile im Auftrag <Summe der Mengen>“, ohne Klammer und ohne Positionsanzahl (auch die Einzahl „(1 Position)“ entfällt).
Geht AK 2/S5 und TS-205 vor. Umsetzung im Bündel nach Teil 2 (Version des Folge-Commits), QA zusammen mit Teil 2.

### QA Nachtrag N-1 (qa-agent, 2026-11-26)

- Commit a89d98cc (v1.99.88): `Print.cshtml` `summeText` nur noch `FormatMass(Model.Summe)`; `PositionenGesamt` entfernt (keine weitere
  Verwendung, Build grün); `BeschichtungsauftraegeControllerTests` prüft nur noch `Summe` (beabsichtigte Änderung durch N-1);
  Hilfe, Changelog, `docs/anwender/15-…`, TS-205.1/.5/.11 angepasst. PDF rendert dieselbe View `Print` (`BeschichtungsauftraegeController.Pdf`).
  Review: keine Befunde.
- Build/Tests: siehe QA-Nachweis Teil 2 (3820/1/0 Web, 848/0/0 Service). DEV-Deploy AKE + IDEAL v1.99.88, Exit 0.
- DEV AKE: `/Beschichtungsauftraege/Print/6` → „Summe Teile im Auftrag 1035“ (ohne Klammer); `/Pdf/6` (pdftotext) → „Summe Teile im Auftrag 1035“. OK.
- DEV IDEAL: `/Beschichtungsauftraege/Print/12` → „17“ ohne Klammer; `/Pdf/12` → „Summe Teile im Auftrag 17“ (das PDF von 20:27 vor N-1 zeigte
  noch „17 (7 Positionen)“). OK.
- Status bleibt Testbereit. Offen für den Menschen: TS-205.1/.5/.11 am Testsystem AKE und IDEAL.
