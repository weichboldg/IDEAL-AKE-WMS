---
type: spec
title: "Leitstand: Spaltenfilter Glas, Zukauf, Status, Kommissionierer (und Lack-T) filtern auf den angezeigten Wert"
slug: 2026-11-26-leitstand-spaltenfilter-glas-zukauf-ohne-wirkung-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-leitstand-spaltenfilter-glas-zukauf-ohne-wirkung]]"
bug: "[[2026-11-26-leitstand-spaltenfilter-glas-zukauf-ohne-wirkung-bug]]"
uebersicht: "[[2026-11-26-leitstand-qa-befunde-f1-f3-uebersicht]]"
depends_on: "[[2026-11-26-leitstand-kaskaden-zaehler-ohne-hauptfa-spec]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - "Bug-Record [[2026-11-26-leitstand-spaltenfilter-glas-zukauf-ohne-wirkung-bug]]"
  - IdealAkeWms/Controllers/PickingLeitstandController.cs:94-103 (Auswahl der Memory-Filter-Keys an EINER Menge), :804-815 (LeitstandMemoryText um glass, purchase, coating-part, status, picker)
  - IdealAkeWms/Views/PickingLeitstand/Index.cshtml:189 (coating-part data-filterable, nach Rückfrage 2)
  - IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs (Muster :434-468 Matchcode-Memory-Filter; je neuer Key flach + Nur-HauptFA)
  - docs/TESTSZENARIEN.md (neues TS-211), Views/Help/Index.cshtml (Abschnitt Leitstand, Filterwerte ja/nein)
open_questions:
  - "1 Filterwert der Haken-Spalten Glas/Zukauf: „ja“/„nein“ (gleich wie der CSV-Export, csv-export.js:94, und BdeTeilelisteController.cs:346) - einverstanden?"
  - "2 Lack-T hat heute KEIN Filterfeld (Index.cshtml:189, anders als die Backlog-Notiz annimmt). Filterfeld ergänzen mit „ja“ = erledigt, „nein“ = offen, leer = keine Lackteile (Empfehlung), oder ohne Filter lassen?"
  - "3 Status-Filter: Filtertext = angezeigte Badges „Offen“/„Erledigt“ und „Komm.“/„Teilkomm.“. Die Kommissionierauftrags-Nummern in derselben Zelle werden erst nach dem Blättern geladen (PickingLeitstandController.cs:320) und sind dann NICHT filterbar - einverstanden, oder sollen sie mitgefiltert werden (teurer: Aufträge vor dem Blättern laden)?"
  - "4 Zusatzfund: Spalte Kommissionierer (picker, nur mit KommissionierungMitZuweisung) hat ebenfalls ein wirkungsloses Filterfeld. Mitbeheben (Empfehlung, eine Zeile) oder eigener Eintrag?"
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

## Ziel / Nutzen (das Warum)

Ein Filterfeld, das nichts tut, ist ein stiller Fehler: Wer im Leitstand „alle mit Glas“ sucht, sieht die volle Liste und hält sie für das
Ergebnis. Die Hausregel „melden statt still behandeln“ und ADR [[0005-listen-view-pattern-mit-server-side-spaltenfilter]] verlangen: jede
Spalte mit Filterfeld filtert, auf den Wert, den die Zelle zeigt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (beide Standorte, flacher und hierarchischer Zweig von `/PickingLeitstand`)**

- `glass`, `purchase`, `status` filtern; `picker` nach Rückfrage 4; `coating-part` bekommt ein Filterfeld nach Rückfrage 2.
- Im Nur-HauptFA-Modus (IDEAL ohne „Sub-FAs anzeigen“) filtern Glas/Zukauf/Lack-T auf den **zusammengefassten** Wert (L2 der Teil-2-Spec:
  „Filter auf den angezeigten Wert“); mit Schalter je Zeile auf den eigenen Wert.
- Suchsyntax wie alle Spalten (Kern `ColumnFilterSyntax`, Fallstrick 26): `ja`, `=nein`, `!ja`, `(leer)`, ODER mit `,`.

**Out-of-Scope**

- FA-Liste `/ProductionOrders`: hat keine Glas/Zukauf-Filterfelder (Suche `data-col-key="(glass|purchase)"` in `Views/` trifft nur den Leitstand);
  ihre Lack-T-Spalte hat ebenfalls kein Filterfeld (`ProductionOrders/Index.cshtml:119`), bleibt so.
- Keine Auswahlliste statt Textfeld im Filterkopf (Bestandsmuster Textfeld; die Filterhilfe nennt die Syntax).
- Kein neuer Drift-Wächter über alle Listen (Zusatzidee, siehe Rückfragen-Hinweis unten - nicht gebaut).

## Fachliche Anforderungen

### Ist-Stand (belegt; Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, v1.99.89)

- Filterfelder: `glass` `Index.cshtml:190`, `purchase` `:191`, `status` `:197`, `picker` `:206` mit `data-filterable`; `coating-part` `:189` ohne.
- Server-Modus (`:158`), Werte gehen nur an den Server (`table-filter.js:61-73`).
- Memory-Filter: Datum (`LeitstandDateColumnKeys` `PickingLeitstandController.cs:765-772`), VK-VA (`:778-785`), `matchcode` (`:94-103`); Text je
  Key in `LeitstandMemoryText` (`:804-815`), sonst leer. Alle übrigen Keys → `ApplyLeitstandColumnFilter` (`ProductionOrderRepository.cs:343-373`),
  unbekannter Key = No-Op.
- Memory-Filter laufen **nach** `MapItem` und damit nach der Zusammenfassung (`zusammenfassen?.Invoke(item)` `:183`; Filter `:263-277` bzw.
  flach `:345-356`). Aktive Memory-Filter laden alle Zeilen ungeblättert (`:99-106`, `:211-212`) - Bestand für Datum/VK-VA.
- Angezeigte Werte: Glas/Zukauf als Checkbox (`_PickingLeitstandRow.cshtml:135-149`), Lack-T nur bei `HasCoatingParts`, Haken = `IsCoatingDone`
  (`:115-134`); Status-Zelle: Badge „Erledigt“ bei `item.IsDone` (= `IsDone || IsDonePicking`, Controller `:142`) sonst „Offen“, dazu „Komm.“ bei
  `PickingStatus == "abgeschlossen"`, „Teilkomm.“ bei `"teilkommissioniert"` (`:171-190`), danach Kommissionierauftrags-Nummern (`:192-198`), die
  `AuftraegeAnhaengenAsync` erst für die fertige Seite lädt (Controller `:320`); Kommissionierer: `AssignedPickerName`, nur bei
  `IsReleasedForPicking` (`:311-320`).
- CSV-Export liest Checkboxen als „Ja“/„Nein“ (`csv-export.js:94`); Projektmuster „ja“/„nein“ auch `BdeTeilelisteController.cs:346`.
- Der Kern vergleicht klein geschrieben (`ColumnFilterSyntax.cs:387`).

### Soll

| ID | Spalte (`data-col-key`) | Filtertext je Zeile (= angezeigter Wert) |
|---|---|---|
| S1 | Glas (`glass`) | `item.HasGlass ? "ja" : "nein"` (Rückfrage 1) |
| S2 | Zukauf (`purchase`) | `item.HasExternalPurchase ? "ja" : "nein"` |
| S3 | Lack-T (`coating-part`, Rückfrage 2) | ohne Lackteile `""`, sonst `item.IsCoatingDone ? "ja" : "nein"` |
| S4 | Status (`status`) | `(item.IsDone ? "erledigt" : "offen")` + ` komm.` bzw. ` teilkomm.` nach `PickingStatus` (Rückfrage 3: ohne KO-Nummern) |
| S5 | Kommissionierer (`picker`, Rückfrage 4) | `item.IsReleasedForPicking ? item.AssignedPickerName ?? "" : ""` |
| S6 | Nur-HauptFA-Modus | S1-S3 lesen das Item nach der Zusammenfassung (Bestand der Reihenfolge, kein Zusatzcode) |
| S7 | Kein Key dieser Liste mit Filterfeld bleibt ohne Auswertung (Test belegt es für die fünf Keys). |

## Technischer Loesungsentwurf

Ponytail: vorhandene Memory-Filter-Maschinerie, eine Menge, fünf Zeilen im vorhandenen `switch`. Kein SQL-Prädikat - die Werte liegen in
`ProductionOrderPickingStatus` bzw. entstehen erst in `MapItem`/der Zusammenfassung; ein SQL-Weg müsste die Zusammenfassung ein zweites Mal in SQL
nachbauen (Fallstrick „BuildExtraInfoOrContains … keine Subquery“: berechnete Werte über den C#-Postfilter).

1. **Controller:** eine Menge `LeitstandMemoryColumnKeys` = Datum ∪ VK-VA ∪ `{ matchcode, glass, purchase, coating-part, status, picker }`;
   die zwei Prädikate `:95-96` und `:100-101` lesen nur noch diese Menge (entfernt die doppelte Aufzählung).
2. `LeitstandMemoryText` (`:804-815`) um die Fälle S1-S5 ergänzen; Typ bleibt Text (kein Eintrag in `ColumnFilterTypes` nötig, Spalten ohne Eintrag = Text).
3. **View** (nach Rückfrage 2): `data-filterable` an `Index.cshtml:189`.
4. **Tests** (Muster `PickingLeitstandControllerTests.cs:434-468`): je Key eine flache Prüfung (Treffer + `capturedSqlFilters` enthält den Key nicht);
   eine Nur-HauptFA-Prüfung: HauptFA-Zeile ohne Glas, Sub-FA mit Glas → `colf_glass=ja` findet die HauptFA (S6).
5. Hilfe `Views/Help/Index.cshtml`, Abschnitt Leitstand: ein Satz „Glas/Zukauf/Lack-T filtern mit ja/nein“.
6. Frontend: Skill `frontend-design` aufrufen (nur ein Attribut; keine Stiländerung).

Hinweis (nicht gebaut): Ein Wächter-Test „jedes `data-filterable` einer Server-Liste hat einen Auswerter“ würde diese Fehlerklasse projektweit
fangen; `ApplyLeitstandColumnFilter` ist ein privater `switch` ohne aufzählbare Keys, der Wächter bräuchte einen Umbau. Eigener Backlog-Eintrag,
falls gewünscht.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein Schema, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt.

## Einstellungen

keine (`KommissionierungMitZuweisung` steuert weiter nur, ob die Spalte Kommissionierer erscheint, `PickingLeitstandController.cs:200-201`).

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine - reine Lese-/Filteränderung. Rollen unverändert (Klasse `[RequirePickingOrLeitstandAccess]`, `PickingLeitstandController.cs:12`).
Listen-View-Pattern bleibt erfüllt: Server-Modus, Pagination (Memory-Filter blättern in C#, `:276`, `:355`), `data-col-key` an allen `<th>`.

## Akzeptanzkriterien

1. AKE und IDEAL (mit Schalter „Sub-FAs anzeigen“): `colf_glass=ja` zeigt nur Zeilen mit Glas-Haken, `colf_glass=nein` nur ohne; Pagination und
   Trefferzahl stimmen.
2. Ebenso `colf_purchase=ja|nein`.
3. IDEAL ohne Schalter: HauptFA-Zeile ohne eigenes Glas, eine Sub-FA mit Glas → Zeile zeigt Glas-Haken und erscheint bei `colf_glass=ja`, nicht bei `=nein`.
4. Nach Rückfrage 2: Lack-T hat ein Filterfeld; `ja` = erledigt, `nein` = offen, `(leer)` = Zeilen ohne Lackteile; im Nur-HauptFA-Modus auf den
   zusammengefassten Wert (W3-Zeile „Lackteile nur an Sub-FAs“ eingeschlossen).
5. `colf_status=erledigt` mit „Erledigte anzeigen“ zeigt nur erledigte Zeilen; `teilkomm` nur teilkommissionierte; ohne „Erledigte anzeigen“ liefert
   `erledigt` 0 Treffer (Erledigte sind ausgeblendet - erwartet).
6. Nach Rückfrage 4: `colf_picker=<Name>` zeigt nur dessen freigegebene Aufträge.
7. Suchsyntax: `!ja`, `ja,nein` (alle), `=ja` verhalten sich wie in anderen Spalten.
8. CSV-Export mit aktivem Filter enthält nur die gefilterten Zeilen (Server-Modus holt dieselbe URL, `csv-export.js:3`).
9. Build und alle Tests grün; neue Controller-Tests je Key.

## Test-Szenarien

Neues Kapitel **TS-211** in `docs/TESTSZENARIEN.md` (Nummer beim Dev-Lauf nachprüfen), Index nachziehen:

- **TS-211.1** AKE: Glas/Zukauf `ja`/`nein`, Trefferzahl gegen Sichtprüfung; Seite 2 mit Filter.
- **TS-211.2** IDEAL ohne Schalter: Zusammenfassung (AK 3), Lack-T (AK 4, inkl. W3-Zeile).
- **TS-211.3** IDEAL mit Schalter: Filter je Zeile auf den eigenen Wert; Gruppe bleibt, solange eine Zeile passt (Z3b-Bestand).
- **TS-211.4** Status mit/ohne „Erledigte anzeigen“ (AK 5); Kommissionierer (AK 6, nur mit `KommissionierungMitZuweisung`).
- **TS-211.5** Negativ: unsinniger Wert `xyz` → 0 Treffer statt voller Liste (der eigentliche Bugnachweis); Filter leeren → volle Liste.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | kein Epic | – | – |

## Deploy

*Vorläufig - der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Controller, View)
- **Service:** nein
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree); Test auf DEV AKE **und** DEV IDEAL

## Offene Rueckfragen

1. **Filterwert Glas/Zukauf:** „ja“/„nein“ - gleich wie der CSV-Export (`csv-export.js:94`) und die BDE-Teileliste (`BdeTeilelisteController.cs:346`).
   Einverstanden?
2. **Lack-T-Filter:** Heute hat Lack-T kein Filterfeld (`Index.cshtml:189`), die Backlog-Notiz nimmt eines an. Empfehlung: ergänzen
   („ja“ = erledigt, „nein“ = offen, leer = keine Lackteile) - ein Attribut, eine Zeile Filtertext. Alternative: ohne Filter lassen.
3. **Status und Kommissionierauftrags-Nummern:** Gefiltert wird auf „Offen/Erledigt“ und „Komm./Teilkomm.“. Die KO-Nummern derselben Zelle lädt
   der Controller erst für die fertige Seite (`PickingLeitstandController.cs:320`); sie mitzufiltern hieße, für alle Zeilen vorab Aufträge zu laden.
   Empfehlung: ohne KO-Nummern. Einverstanden?
4. **Zusatzfund Kommissionierer:** `picker` (`Index.cshtml:206`) ist genauso wirkungslos. Empfehlung: hier mitbeheben (gleiche Stelle, eine Zeile).
   Alternative: eigener Backlog-Eintrag.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. Filterwert Glas/Zukauf „ja“/„nein“ - einverstanden? →
2. Lack-T-Filterfeld ergänzen (Empfehlung) oder ohne Filter lassen? →
3. Status-Filter ohne Kommissionierauftrags-Nummern (Empfehlung) oder mit? →
4. Kommissionierer-Filter hier mitbeheben (Empfehlung) oder eigener Eintrag? →
