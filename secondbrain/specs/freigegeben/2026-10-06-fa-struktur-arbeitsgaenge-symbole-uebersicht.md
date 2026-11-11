---
type: uebersicht
title: "FA-Struktur: Arbeitsgänge als Symbole, später BDE-Status in Farbe — Übersicht"
slug: 2026-10-06-fa-struktur-arbeitsgaenge-symbole-uebersicht
status: Freigegeben
created: 2026-10-06
updated: 2026-10-13
source_backlog: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]"
anhaenge:
  - "[[sage-fa-struktur-arbeitsschritte.png]]"
# Vorbedingung fuer Teil 2 und 3. ueberholt (2026-10-13): bis 2026-10-12 zeigte der Schluessel auf die Backlog-Notiz
# 2026-10-09-arbeitsgang-stammdaten-vereinheitlichen, weil die Spec noch nicht existierte; seit 2026-10-13 auf die Spec
# umgestellt (Testbereit, Buendel v1.54.0).
vorbedingung: "[[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]"
open_questions: []
freigabe_entscheidung: "Uebersicht freigegeben mit allen Teilen: Teil 1 Testbereit (v1.52.0), Vorbedingung Arbeitsgang-Stammdaten Testbereit (v1.54.0), Teil 2 und 3 freigegeben 2026-10-13; Reihenfolge Teil 2 -> Teil 3, direkt im Buendel."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-10-13
---

> **Umsetzungsort (Entscheidung Gerald 2026-10-09):** direkt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`
> auf Zweig `feature/2026-08-07-ideal-teile-1-5` (kein eigener Worktree, ein Lauf zur Zeit, kein Warten auf
> Schranke 2 des Bündels). `worktree`/`branch` bleiben in allen Teil-Specs leer. Code-Grundlage: Bündel-Stand
> (v1.47.1/v1.47.4; im Rework 2026-10-12 angefasste Zeilenangaben gegen **v1.49.0** geprüft). TS-Nummern und
> Versionen: nächste freie **im Bündel** zum Umsetzungszeitpunkt (Stand 2026-10-12: v1.49.0, höchstes TS-82).
> (Vormals: „nach Schranke 2 des IDEAL-Bündels, aus `main`“ bzw. „nach dem Merge gegen `main` nachzuprüfen“ —
> überholt.)
> **Nachmessung 2026-10-13 (überholt den Stand 2026-10-12):** Bündel-Worktree HEAD `d56a340e`, **v1.54.0**; höchstes
> Kapitel **TS-87**, höchstes Skript **`SQL/95_RenameOperationConfigsAndAddAudit.sql`**, letzte Migration
> **`20261012140548_RenameOperationConfigsAndAddAudit`**; Hauptcheckout (`main`) höchstes Skript `SQL/88_*`. Teil 1
> (v1.52.0, TS-85) und Vorbedingung V (v1.54.0, TS-87) sind umgesetzt und Testbereit; Teil 2 → voraussichtlich TS-88,
> Teil 3 → TS-89 (jeweils „nächste freie zum Umsetzungszeitpunkt“). Teil 2 und 3 bringen keine Migration.

Zerlegung von [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]] (`split: true`) in einzeln mergebare Teile,
Reihenfolge wie „Gewünschter Schnitt“ der Notiz. Baut auf
[[2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec]] (v1.45, Modal) und
[[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] (v1.44, Arbeitsgänge aus der Struktur) auf.
Standort: Teile 1-2 **IDEAL** (FA-Struktur), Teil 3 **AKE und IDEAL** (BDE-Terminal, Teil-3-Antwort 5).
Vorbedingungen für Teil 2/3: Scope-Korrektur der Arbeitsgang-Erkennung **v1.47.4** im Bündel
([[2026-10-07-v1-47-4-arbeitsgang-erkennung-kind-sub-fa]], aus Teil-2-Antwort 7 W-1, umgesetzt) **und neu** die
vereinheitlichten Arbeitsgang-Stammdaten (Offsets je Kürzel, Teil-2-Antwort 8 / Teil-3-Antwort 7):
[[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] (aus der Backlog-Notiz
[[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen]]) — ~~Spec existierte am 2026-10-12 noch nicht; die
`depends_on` in Teil 2/3 zeigen deshalb auf die Backlog-Notiz und sind umzustellen, sobald die Spec existiert.~~ —
überholt (2026-10-13): Spec umgesetzt (v1.54.0, Testbereit), `depends_on` in Teil 2/3 umgestellt. Neu als
Vorbedingung im Teil-2-Lauf: [[2026-10-12-operationconfig-offset-leer-pruefen]] (leerer Offset serverseitig
abweisen, Teil 2 FA 4a).

Vorbild (Anhang der Notiz, [[sage-fa-struktur-arbeitsschritte.png]]): Sage „Fertigungsaufträge
bearbeiten“ zeigt je Baumzeile die Arbeitsschritte als Kürzel in **einer** Spalte und die Abteilung je Zeile.
Die WMS-Felder dazu (`FaHierarchyNode.Arbeitsschritte`/`Arbeitsbereich`) sind Kandidaten, die Gleichheit ist
ungeprüft (Prüfschritt P-K1 in Teil 1).

| # | Teil | Zweck | depends_on | Offene Rückfragen |
|---|------|-------|------------|-------------------|
| V | [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] (~~Backlog; Spec folgt~~ — Stand 2026-10-13: **umgesetzt v1.54.0, Testbereit**) | Vorbedingung, **nicht** Teil dieser Zerlegung: Arbeitsgang-Stammdaten OSEON + IDEAL als Reiter „Arbeitsgänge“ in den BDE-Stammdaten, Offset je Kürzel (`IOperationConfigRepository.GetDueDateOffsetsAsync`), „Fremdsystem-relevant“, Liste „nicht konfiguriert“; Migration `20261012140548_RenameOperationConfigsAndAddAudit` / `SQL/95` | – | eigene Spec (0) |
| 1 | [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]] (Stand 2026-10-13: **umgesetzt v1.52.0, Testbereit**) | Sage-Kürzel als Symbole direkt in den Baumzeilen der FA-Struktur, reine Anzeige ohne Status (Quelle S, Ort A); Modal bleibt unverändert | – | **0** (Nr. 1-4 beantwortet, Nr. 4 am 2026-10-09) |
| 2 | [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]] | Status erledigt/läuft/läuft+überfällig/überfällig/offen bzw. „kein BDE-Arbeitsgang“ an den Symbolen bei BDE global + Werkbank aktiv; überfällig = `FE_Termin` (Kombigerät: frühester) + Offset-Werktage aus den Stammdaten über `OseonDueDateCalculator`; W-2/W-3 kein Vater-Rückfall, W-4 zusammengefasst + Log; Farbe + Zeichen + `aria-label`; **neu 2026-10-13:** Offset-Pflichtprüfung in `OperationConfigsController` (FA 4a) | Teil 1, Vorbedingung V (+ Bündel v1.47.4) | ~~0~~ **1** (neu Nr. 11 Kontrast in gedimmten Kontextzeilen, aus dem Abgleich 2026-10-13; Nr. 1-10 beantwortet) |
| 3 | [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec]] | BDE-Terminal „Arbeitsgang wählen“ (Normalmodus): offene Aufträge je Werkbank gruppiert, nach Fertigungstermin sortiert (IDEAL `FE_Termin`, AKE `ProductionDate`), Arbeitsgänge mit Status aus Teil 2, fremd gebuchte sichtbar als „läuft“; NurFA unverändert — AKE und IDEAL, bestehende BDE-Rollen | Teil 2, Vorbedingung V | ~~1 (neu Nr. 9 …)~~ **0** (Nr. 9 am 2026-10-12 beantwortet: `ProductionDate`; Abgleich 2026-10-13 ohne neue Rückfrage) |

## Querschnitt

- **Entschieden: S** (Teil 1 Antwort 1, Gerald 2026-10-06 — Anzeige aus Sage-Kürzeln, Status aus W; Zuordnung
  in Teil 2 Antwort 6, dort mit vier dokumentierten Widersprüchen zum Erkennungs-Code, Teil-2-Rückfrage 7;
  W-1 seit 2026-10-07 entschieden, ~~W-2 bis W-4 offen~~ — seit 2026-10-09 entschieden: W-2/W-3 „kein
  BDE-Arbeitsgang“ ohne Vater-Rückfall, W-4 zusammengefasst + Log-Warnung).
  Ursprünglicher Text: **Rückfrage 1 von Teil 1 (Quelle: Sage-Kürzel S oder erkannte `WorkOperation` W) prägt Teil 2:** Status
  gibt es nur über `WorkOperation`. Bei S braucht Teil 2 eine Zuordnung Kürzel → `WorkOperation` (Blätter und
  Kinder-Kürzel landen per DirectChildren-Scope beim Vater-Sub-FA) und eine Regel für Kürzel ohne
  `WorkOperation` (Teil 2 Rückfrage 6). Bei W entfällt das, die Zeile weicht aber vom Sage-Bild ab.
- **Entschieden: Ort A** (Teil-1-Antwort 2, Keyuser 2026-10-07): Symbole direkt in den Baumzeilen; die
  v1.45-Freigabe „erst beim Öffnen laden“ gilt für die Symbole nicht mehr, Teil 2 fragt den Status im
  Seitenaufbau ab. ~~Ob das Modal unverändert bleibt, bestätigt Teil-1-Rückfrage 4.~~ — entschieden
  (Teil-1-Antwort 4, 2026-10-09): Modal bleibt unverändert, ohne Status. Ursprünglicher Text:
  „Rückfrage 2 von Teil 1 (Baum vs. Modal) prägt alle Teile … das muss die Antwort ausdrücklich sagen.“
- **Entschieden: „erledigt“ = `IsFinal`** (Teil-2-Antwort 3). Code-Befund dazu bleibt: Die BDE-Buchung setzt
  `WorkOperation.IsReported` nicht; `IsReported` schreibt nur die manuelle Tracking-Rückmeldung.
- **Entschieden: „überfällig“** = `FE_Termin` + Offset-Werktage über den vorhandenen `OseonDueDateCalculator`
  (Teil-2-Antwort 2 + Nachtrag). ~~Offen: wo die IDEAL-Offsets gepflegt werden (Teil-2-Rückfrage 8; Variante B
  brächte eine Migration) und die Kombigeräte-Regel (Teil-2-Rückfrage 9).~~ — entschieden 2026-10-09: Offsets
  aus den vereinheitlichten Arbeitsgang-Stammdaten (Vorbedingung V, ohne Eintrag 0 + Meldung „nicht
  konfiguriert“), Kombigerät = frühester `FE_Termin`; „läuft“ + Soll überschritten = „läuft“ mit Zusatz
  „überfällig“. Termine liegen nur je HauptFA; ihre Bedeutung bleibt unverifiziert (Teil-2-Prüfschritte P1/P2).
- **`OseonDueDateCalculator` (Befund 2026-10-12, v1.49.0):** reine Rechenregel `Calculate(baseDate, offsetDays,
  IBusinessDayService, holidays)` (`OseonDueDateCalculator.cs:9-13`), **Werktage** Mo-Fr ohne Feiertage
  (`BusinessDayService.cs:29-50`), ohne eigene Offset-Quelle; die heutigen Aufrufer holen den Offset aus
  `OseonOperationConfigs` (`OseonReportingController.cs:74-77`, `OseonGroupViewModelBuilder.cs:34-52`). Teil 2/3
  rufen dieselbe Regel auf und tauschen nur die Offset-Quelle; ob der Calculator umbenannt oder die
  OSEON-Aufrufer umgestellt werden, entscheidet die Vorbedingungs-Spec. Erwartete Schnittstelle: Teil 2 FA 4.
  **Ergebnis (2026-10-13, v1.54.0):** Calculator unverändert und gleichnamig (`OseonDueDateCalculator.cs:3-13`); die
  OSEON-Aufrufer lesen dieselbe, nur umbenannte Tabelle `dbo.OperationConfigs`. Schnittstelle erfüllt:
  `IOperationConfigRepository.GetDueDateOffsetsAsync()` (`OperationConfigRepository.cs:23`, `:67-77`; alle Zeilen,
  getrimmt, `OrdinalIgnoreCase`, kleinste `Id` gewinnt, fehlender Schlüssel = kein Eintrag), gecacht
  (`CachedOperationConfigRepository.cs:41-49`).
- **Entschieden: W-1** (Teil-2-Antwort 7) → Bugfix v1.47.4 im Bündel, nicht in diesen Teilen.
- **Teil 3 ist ausspezifiziert** (Antworten 1-8): gilt für AKE **und** IDEAL; AKE-Termin = `ProductionDate`
  (Antwort 7), fremd gebuchte Arbeitsgänge sichtbar als „läuft“ (Antwort 6 = A), NurFA unverändert (Antwort 8).
  ~~Offen nur Teil-3-Rückfrage 9.~~ — überholt: Nr. 9 am 2026-10-12 beantwortet (Rückfall auf `ProductionDate`);
  Teil 3 hat keine offene Rückfrage.
- **Angleichung zwischen den Teilen (Rework 2026-10-12):**
  - Status-Namen: überall erledigt / läuft / läuft + überfällig / überfällig / offen / kein BDE-Arbeitsgang; am
    Terminal nur offen / läuft / läuft + überfällig / überfällig (erledigte sind ausgeblendet, ohne
    `WorkOperation` gibt es keinen Knopf).
  - `aria-label`-Format überall ~~`„<Kürzel> <Name> – <Status>“`~~ **`„<Kürzel> – <Name> – <Status>“`** (Teil 1 liefert
    „KA – Name“ bzw. „KA“, `FaHierarchyTreeViewModel.cs:218-222`; Teil 2/3 hängen den Status an — angeglichen 2026-10-13).
  - Statusquelle „erledigt“ = `IsFinal` auf nicht stornierter Buchung, „läuft“ = `EndedAt == null &&
    !IsCancelled` — gleiche Prädikate in Teil 2 und am Terminal (`BdeApiController.cs:246-257`).
  - BDE-aktiv-Regel global + Werkbank: Teil 2 prüft beide, am Terminal ist sie durch die bestehenden Gates immer
    erfüllt.
  - Offset-Quelle und Default 0: identisch in Teil 2 und 3 (Vorbedingung V). Unterschied nur in der
    **Meldung**: Teil 2 zeigt einen Sammelhinweis „nicht konfiguriert“, das Terminal keinen (Festlegung der Spec,
    Meldung zentral in der Stammdaten-Liste).
  - Kombigeräte-Regel: frühester `FE_Termin` in Teil 2 und 3.
  - W-4-Zusammenfassung: nur Teil 2 (ein Symbol je Kürzel); am Terminal ist jede `WorkOperation` ein eigener
    Knopf.
  - **Gemeldet, nicht still entschieden:** IDEAL-Auftrag mit Kopfzeile(n) ohne `FE_Termin` — Teil 2 „nie
    überfällig“, Teil 3 (Arbeitsannahme) Rückfall auf `ProductionDate` wie die FA-Liste → Teil-3-Rückfrage 9.
    **Entschieden (Teil-3-Antwort 9, Gerald 2026-10-12):** Rückfall auf `ProductionDate` am Terminal, die Abweichung zu
    Teil 2 ist bewusst.
  - Offset-Pflichtprüfung (leerer Offset per manipuliertem POST wurde still 0, Aufgabe
    [[2026-10-12-operationconfig-offset-leer-pruefen]]): erledigt Teil 2 (FA 4a) im selben Lauf, wirkt für Teil 3 mit.
  - **Neu offen (2026-10-13):** Teil-2-Rückfrage 11 — Statusfarben in den gedimmten Kontextzeilen des
    Baum-Spaltenfilters (`opacity: 0.6`, Teil-1-QA M1) unter WCAG AA. Betrifft nur Teil 2 (das Terminal dimmt nicht).
- **BDE derzeit nirgends aktiv** (Entscheidung 1 der Backlog-Notiz, übernommen in
  [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]], dort Prüfschritt P5; DB-Wert
  ungeprüft): Teil 2 zeigt produktiv erst Status, Teil 3 ist erst erreichbar, wenn `BdeAktiv` eingeschaltet
  wird; Abnahme am Testsystem mit BDE an. Teil 1 wirkt unabhängig davon.
- Migration: keiner der drei Teile (Teil 2 seit Antwort 8 nicht mehr; eine etwaige Migration bringt die
  Vorbedingung V — umgesetzt als `20261012140548_RenameOperationConfigsAndAddAudit` / `SQL/95`, das vor Teil 2/3 auf
  den Site-DBs laufen muss). ~~Teil 2 nur bei Rückfrage 8 Variante B~~ — überholt. Keiner der Teile schreibt
  Bewegungsdaten; Deploy jeweils nur Web (vorläufig).
- **Reihenfolge der Umsetzung (im Bündel, ein Lauf zur Zeit):** Teil 1 → Vorbedingung V → Teil 2 → Teil 3.
  Begründung: Teil 1 hängt an nichts und ist ohne offene Rückfrage — er kann starten, sobald er freigegeben ist,
  ohne auf die noch nicht geschriebene Vorbedingungs-Spec zu warten. Harte Reihenfolge gilt nur für V und Teil 1
  **vor** Teil 2 (Offset-Lookup + Symbole) und Teil 2 **vor** Teil 3 (Status-Funktion). Wird V früher
  freigegeben als Teil 1, ist V → Teil 1 → Teil 2 → Teil 3 gleichwertig. (Vormals „Bündel-Merge (inkl. v1.47.4)
  → Teil 1 → Teil 2 → Teil 3“ — überholt, Umsetzung direkt im Bündel.)
  **Stand 2026-10-13:** Teil 1 (v1.52.0) und V (v1.54.0) sind umgesetzt; offen sind Teil 2 → Teil 3, in dieser
  Reihenfolge (Teil 3 nutzt die Status-Funktion aus Teil 2).

## Abgleich mit der Umsetzung (2026-10-13)

Teil 1 und Vorbedingung V sind umgesetzt (Bündel v1.54.0, HEAD `d56a340e`). Die Abweichungen zwischen den Annahmen
der Specs und dem Code stehen mit Beleg in den Teil-Specs, je im Abschnitt „Abgleich mit der Umsetzung (2026-10-13)“;
für die Übersicht wesentlich:

| Annahme | Umsetzung (Beleg) | Folge |
|---|---|---|
| V existiert nur als Backlog-Notiz | Spec Testbereit, v1.54.0 | `vorbedingung`/`depends_on` auf [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] |
| Offset-Lookup laut Teil 2 FA 4 | `IOperationConfigRepository.GetDueDateOffsetsAsync()` erfüllt alle vier Erwartungen (`OperationConfigRepository.cs:67-77`) | kein Umbau in Teil 2/3 |
| Calculator evtl. umbenannt | unverändert `OseonDueDateCalculator` | Teil 2/3 rufen ihn unter altem Namen |
| Offset-Pflege fertig | leerer Offset per manipuliertem POST = still 0 (`OperationConfigsController.cs:72`, `:107`) | Teil 2 FA 4a/AK 19 im selben Lauf |
| Teil-2-P4 mit alten Tabellennamen | `dbo.OperationConfigs` / `IsExternalSystemRelevant` | P4 in Teil 2 neu gefasst, alte Fassung überholt |
| Teil 1 Beschriftung „Kürzel Name“ | „KA – Name“ (`FaHierarchyTreeViewModel.cs:218-222`) | `aria-label` Teil 2/3 „KA – Name – Status“ |
| Teil-1-Chip neutral, Farbe frei | bestätigt; Kontextzeilen gedimmt (`site.css:1361`, Teil-1-QA M1 offen) | Teil-2-Rückfrage 11 |
| AKE nutzt dieselben Stammdaten | eine Tabelle je Site-DB für OSEON-Namen und WMS-Kürzel | Teil 3 P-T1 um Kollisionsabfrage ergänzt (melden, nicht entscheiden) |

## Offene Rückfragen

Keine zur Zerlegung selbst — der Schnitt folgt der Notiz. Die fachlichen Rückfragen stehen in den Teilen.
~~Stand 2026-10-12: Teil 1 **0**, Teil 2 **0**, Teil 3 **1** (Nr. 9, IDEAL-Kopfzeile ohne `FE_Termin`) — zusammen
**1** offene Rückfrage. Dazu kommt die Freigabe der Vorbedingungs-Spec (noch nicht geschrieben).~~ — überholt.

**Stand 2026-10-13:** Teil 1 **0** (umgesetzt), Teil 2 **1** (Nr. 11, Kontrast der Statusfarben in gedimmten
Kontextzeilen), Teil 3 **0** — zusammen **1** offene Rückfrage. Die Vorbedingungs-Spec ist freigegeben und umgesetzt.
Teil 3 ist freigabereif; Teil 2 nach Antwort auf Nr. 11.
