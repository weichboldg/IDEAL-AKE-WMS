---
type: spec
title: Vormontage-Listen — Montage-Abteilung als Spalte und Filter
slug: 2026-10-06-vormontage-abteilung-spalte-filter-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-06
source_backlog: "[[2026-10-06-vormontage-abteilung-spalte-filter]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Services/VormontageService.cs — A: ColumnMap :79-90 ein Eintrag (Knotenfeld), AggregateSummiert :216-274 Vorfilter je Knoten; B: Formatierer + Request-Lookup in BuildEinzelteileAsync :153-180 + HauptFA-Vorfilter in AggregateSummiert"
  - IdealAkeWms/Models/ViewModels/FaHierarchyVormontageAggregatViewModel.cs (:16-32) — neue Eigenschaft fuer die Abteilungs-Anzeige in Sicht 2 (A und B)
  - IdealAkeWms/Views/FaHierarchyVormontage/Index.cshtml (colCount :6, th :75-82, Zellen :131-140, column-config :153-164)
  - IdealAkeWms/Views/FaHierarchyVormontage/Summiert.cshtml (colCount :7, th :80-82, Zellen :96-100, column-config :112-118)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (FaHierarchyVormontageEinzeln :400-415, FaHierarchyVormontageSummiert :423-433)
  - IdealAkeWms.Tests/Services/VormontageServiceTests.cs — neue Faelle (je Variante verschieden)
  - IdealAkeWms.Tests/Views/ (Drift-Waechter A/B/C fuer die beiden Vormontage-Views, Muster KommissionierSpaltenKonsistenzTests.cs)
  - docs/TESTSZENARIEN.md (neues Kapitel, naechste freie Nummer)
  - IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs + Views/Help/Changelog.cshtml (Versions-Bump)
open_questions:
  - "1 (frueher 5, geschaerft) Welches Feld ist 'Abteilung'? Der Sage-Screenshot 'Fertigungsauftraege bearbeiten' zeigt Abteilung JE STRUKTURZEILE (H1-03, S-01, H2-02, K-02; leer bei Wurzel und z. B. G-KT-Korpus-Gepunktet). Variante A: Knotenfeld FaHierarchyNode.Arbeitsbereich (Models/FaHierarchyNode.cs:40) — oder Variante B: Kopffeld FaHierarchyOrderInfo.MontageAbteilung (FAInfos.[Abteilung], je HauptFA). Pruefabfragen P4/P5 (P5 = Abfrage aus [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]], Abschnitt 'Vorbild aus Sage') vor der Antwort laufen lassen. Spannungsfeld ADR 0014-Nachtrag/fallstricke §15 ('Arbeitsbereich ist Zielort') beachten."
  - "2 (frueher 1, nur Variante B) Kombinationsgeraete: Was zeigt die Spalte bei mehreren Abteilungen je HauptFA und wie filtert der Filter? (Notiz-Empfehlung: alle, kommagetrennt; Treffer, wenn eine passt; keine Positions-Verdopplung). Achtung Abweichung zur FA-Liste, die bei Kombigeraeten die Zeilen-Abteilung leer laesst. Entfaellt bei Variante A."
  - "3 (frueher 2) Summierte Ansicht: Abteilung nur Anzeige oder Teil des Summierschluessels — und wirkt der Abteilungs-Filter vor oder nach der Matchcode-Aggregation? (Notiz: keine Empfehlung; Spec-Agent: Anzeige + Filter VOR der Aggregation, in beiden Varianten)"
  - "4 (frueher 3) Spaltenfilter im Server-Mode (ADR 0005) und Spaltenauswahl-Registrierung (fallstricke §3) — Bestaetigung inkl. Spaltenposition, Default-Sichtbarkeit und Label (Notiz: keine Empfehlung; Spec-Agent: Server-Mode, alle drei Stellen, sichtbar, direkt nach HauptFA bzw. Matchcode)"
  - "5 (frueher 4) Loest diese Spec [[2026-08-06-kombinationsgeraete-montageabteilung]] teilweise mit, oder bleibt die Trennung dort ein eigener Schritt? (Notiz: keine Empfehlung; Spec-Agent: eigener Schritt, diese Spec trennt nichts)"
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

> [!note] Umsetzungsort (Hinweis, keine Entscheidung)
> Nach Schranke 2 des IDEAL-Buendels, aus `main` (neuer Worktree). Die Spec baut auf Teil 5
> ([[2026-07-29-standort-ideal-teil-5-spec]]) auf, der nur im Buendel-Worktree
> `feature/2026-08-07-ideal-teile-1-5` existiert. Alle Zeilenangaben unten beziehen sich auf den
> Buendel-Stand `.claude/worktrees/2026-08-07-ideal-teile-1-5` (`AppVersion.cs:5` = **1.47.2**) und
> sind beim Umsetzen gegen `main` nach dem Merge neu zu pruefen.

> [!warning] Rework 2026-10-06 — Quelle der „Abteilung" offen (Rueckfrage 1)
> Neuer Input: Sage-Screenshot „Fertigungsauftraege bearbeiten" (Anhang der Backlog-Notiz
> [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]], Abschnitt „Vorbild aus Sage"; gelesen, lesbar).
> Dort steht „Abteilung" **je Strukturzeile** (`H1-03`, `S-01`, `H2-02`, `K-02`; leer bei der Wurzel
> `KTZ-L3-76-5Z1T RAL` und bei `G-KT-Korpus-Gepunktet`). Damit gibt es zwei Kandidaten — **Variante A**
> Knotenfeld `FaHierarchyNode.Arbeitsbereich`, **Variante B** Kopffeld `FaHierarchyOrderInfo.MontageAbteilung`
> (bisheriger Entwurf). Diese Spec skizziert beide und entscheidet **nicht**. Die Rueckfragen sind neu
> nummeriert (alt 5 → neu 1, alt 1–4 → neu 2–5).

## Ziel / Nutzen (das Warum)

Die Vormontage plant ihre Arbeit nach Abteilung. Heute steht in Sicht 1 nur die FAInfos-Abteilung
(`MontageAbteilung`) im Gruppenkopf (`Index.cshtml:111-120`, unbeschriftet `:114`); sie ist weder
filterbar noch nach Sortieren/Spaltenwahl neben der Position sichtbar; in Sicht 2 (Summiert) fehlt sie
ganz. Das Knotenfeld `Arbeitsbereich` wird in den Vormontage-Views heute gar nicht angezeigt (nur in
Kommissionierlisten `FaHierarchyKommissionierListen/Index.cshtml:82,151` und FA-Struktur
`FaHierarchy/_FaHierarchyNode.cshtml:93`). Welches der beiden Felder gemeint ist, klaert Rueckfrage 1. Mit einer eigenen Spalte und
einem Server-Spaltenfilter kann ein Werker die Liste auf „seine" Abteilung eingrenzen.

Gilt nur am Standort **IDEAL**: die Datenbasis (`FaHierarchyNodes`/`FaHierarchyOrderInfos`) fuellt
nur der IDEAL-Sync; die Vormontage-Listen sind hinter dem Toggle `FaHierarchyVormontageAktiv`
(Default aus, `FaHierarchyVormontageController.cs:29-30`). AKE ist nicht betroffen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Sicht 1 „Einzelne Teile" (`/FaHierarchyVormontage/Index`): neue Spalte „Abteilung" + Server-Spaltenfilter
  — **Quelle haengt an Rueckfrage 1** (Variante A Knoten / B Kopf).
- Sicht 2 „Summiert" (`/FaHierarchyVormontage/Summiert`): Spalte + Filter — **Semantik haengt an
  Rueckfrage 1 und 3**.
- Spaltenregistrierung an allen drei Stellen (`<th data-col-key>`, `#column-config`,
  `ColumnDefinitions`) + Drift-Waechter-Test fuer beide Vormontage-Views.
- Automatisierte Tests, Testszenarien-Kapitel, Versions-Bump, Changelog (Anwender + Brain).

**Out-of-Scope**
- Echte Trennung von Kombinationsgeraeten nach Abteilung (Positions-Split, getrennte Dokumente,
  eindeutiger Barcode) — bleibt [[2026-08-06-kombinationsgeraete-montageabteilung]] (siehe Rueckfrage 5).
- Arbeitsschritte-Symbole je Baumzeile — eigener Backlog [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]
  (dieselbe Sage-Vorlage, anderes Feld).
- Vormontage-Druckansicht/PDF — es gibt keine (`Views/FaHierarchyVormontage/` enthaelt nur
  `Index.cshtml` und `Summiert.cshtml`); eigener Backlog [[2026-09-14-vormontage-druckansicht]].
- Kopfzeilen-Darstellung im Gruppenkopf (`Index.cshtml:111-120`) bleibt unveraendert.
- Hilfeseite: einen Vormontage-Abschnitt gibt es in `Views/Help/` nicht (nur Changelog-Treffer);
  wird hier nicht neu angelegt, nur der Changelog-Eintrag.
- Nebenbefund, nicht Teil dieser Spec: `ColumnDefinitions.FaHierarchyVormontageEinzeln` sagt
  `SupportsSortDefault: false` (`ColumnDefinitions.cs:402`), das Inline-`#view-config` derselben View
  sagt `"supportsSortDefault": true` (`Index.cshtml:151`). Nicht angefasst.

## Fachliche Anforderungen

Belegte Ausgangslage (Buendel-Stand) — zwei Kandidaten, Wahl offen (Rueckfrage 1):

- **Kandidat A — Knotenfeld `FaHierarchyNode.Arbeitsbereich`** (`Models/FaHierarchyNode.cs:40`, verifiziert).
  Einziger Schreibpfad: `FaHierarchySyncService` (`IDEALAKEWMSService/Services/FaHierarchySyncService.cs:212,240`)
  aus `f.[Arbeitsbereich]` der Sage-View `vw_IDEAL-AKE_Kommissionierung_FAListe` (`FaHierarchySql.cs:126`),
  Full-Refresh in einer Transaktion (`FaHierarchySyncService.cs:129-138`); kein weiterer Schreiber
  (alle `.Arbeitsbereich`-Treffer in `*.cs`/`*.cshtml` des Buendels sind Lesezugriffe, Mapping
  `ApplicationDbContext.cs:1016` oder Migrationen). Leser: Kommissionierlisten-Spaltenfilter
  (`KommissionierListenService.cs:64`, Spalte `ColumnDefinitions.cs:341`), FA-Struktur-Partial
  (`FaHierarchy/_FaHierarchyNode.cshtml:14,24,93`, Spalte `workarea` `ColumnDefinitions.cs:459`). Kein
  Vormontage-Code liest es. Granularitaet laut View-Doku „Position bevorzugt, Beleg als Fallback
  (positionsbezogen!)" (`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql:40`) — also **je
  Strukturzeile**, wie die Sage-Spalte im Screenshot. Der echte CREATE-VIEW-Text liegt nicht im Repo
  (`…FAListe.sql:57` TODO); die Sage-Herkunft `USER_OSAbteilung` steht nur im Brain ([[fallstricke]] §15,
  ADR-0014-Nachtrag) und ist hier **nicht** am Code belegt. `Str(...)` → NULL moeglich (`FaHierarchySyncService.cs:299`).
- **Kandidat B — Kopffeld** (bisheriger Entwurf), Details folgen:
- **Quelle der Abteilung (B).** `FaHierarchyOrderInfo.MontageAbteilung` (`Models/FaHierarchyOrderInfo.cs:21`).
  Einziger Schreibpfad: `FaHierarchySyncService.ReadOrderInfosAsync` (`IDEALAKEWMSService/Services/FaHierarchySyncService.cs:263,277`)
  aus `SELECT i.[Abteilung] AS [MontageAbteilung]` der Sage-View `vw_IDEAL-AKE_Kommissionierung_FAInfos`
  (`FaHierarchySql.cs:138`), Full-Refresh beider Tabellen in einer Transaktion
  (`FaHierarchySyncService.cs:129-138`). Keine weiteren Schreiber (Repository nur lesend,
  `FaHierarchyOrderInfoRepository.cs`; `ProductionOrderRepository.cs:129,387` nur `Any(...)`; kein
  AgentJob unter `SQL/AgentJobs/` beruehrt `FaHierarchy*`). `Str(...)` liefert bei DBNull `null`
  (`FaHierarchySyncService.cs:299`) — **NULL ist also moeglich**.
- **Granularitaet.** Eine FAInfos-Zeile = ein Auftrag (ABNr + Pos); Kombinationsgeraete haben
  denselben `HauptFA` mit unterschiedlicher `[Abteilung]` (`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAInfos.sql:6-8,28`).
  Ob zwei Zeilen desselben `HauptFA` auch **dieselbe** Abteilung tragen koennen, ist am Code nicht
  entscheidbar → Pruefabfrage P1.
- ~~**Nicht am Strukturknoten.** `FaHierarchyNode` hat kein Abteilungsfeld (Import-SELECT
  `FaHierarchySql.cs:121-127` enthaelt keine Abteilung; `MontageAbteilung` kommt in keinem
  `FaHierarchyNode`-Code vor).~~ **ueberholt (Rework 2026-10-06):** richtig ist nur, dass es am Knoten
  kein Feld *namens* Abteilung/MontageAbteilung gibt. Ob das Knotenfeld `Arbeitsbereich` (`FaHierarchySql.cs:126`)
  die fachlich gemeinte „Abteilung" ist, ist offen (Kandidat A, Rueckfrage 1).
- **Herkunft B ist nicht der Sage-FA-Baum.** FAInfos kommt aus der Cross-DB-Sicht
  `[PPSImport].[dbo].[vw_PPSData]` (`…FAInfos.sql:9`), je Auftrag (ABNr+Pos), nicht je Strukturzeile. Die
  Sage-Spalte im Screenshot steht dagegen je Baumzeile. Ob die Werte beider Felder fuer denselben
  `HauptFA` uebereinstimmen, ist am Code nicht pruefbar → P4/P5.
- **Spannungsfeld (nicht gedeutet).** Brain: `Arbeitsbereich` = „**Zielort** (wohin das Teil kommt) …
  keine Werkbank" ([[fallstricke]] §15, [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]
  Nachtrag 2026-09-22, Rueckbau [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]]); FAInfos-`[Abteilung]`
  ist laut View-Doku „Montage-Abteilung" (`…FAInfos.sql:28`). Ob die Vormontage nach Zielort oder nach
  Montage-Abteilung plant, entscheidet der Mensch (Rueckfrage 1). Diese Spec aendert an der
  Zielort-Einordnung nichts und leitet daraus keine Werkbank ab.
- **Fan-out-Verbot.** Kopfdaten werden nie per Join an Positionen gehaengt; Sicht 1 ordnet sie per
  Dictionary je `HauptFA` zu (`FaHierarchyListBuilder.cs:151-163`).

Anforderungen:

1. **Sicht 1:** Jede Positionszeile zeigt in einer neuen Spalte „Abteilung"
   - *Variante A:* den `Arbeitsbereich` **dieser Position** (Knotenfeld, je Zeile verschieden moeglich);
   - *Variante B:* die Montage-Abteilung(en) ihres `HauptFA`; Darstellung bei mehreren Abteilungen
     **gemaess Rueckfrage 2** (Empfehlung: alle, distinct, sortiert, kommagetrennt).
   Positionsanzahl bleibt in beiden Varianten 1:1 (keine Verdopplung).
2. **Sicht 1 Filter:** Server-Spaltenfilter `colf_abteilung` mit der bestehenden Mini-Syntax
   (Komma = ODER, `!` = NICHT, Teilstring, case-insensitiv — `ColumnFilterHelper.cs:45-60,71-93`).
   - *Variante A:* wirkt **je Position**, wie die vorhandenen Filter `matchcode`/`artnr` (vor der
     Gruppierung, `FaHierarchyListBuilder.cs:135-137`): eine Gruppe bleibt, wenn mindestens eine Position
     passt, und zeigt dann **nur** die passenden Positionen.
   - *Variante B:* wirkt **je HauptFA-Gruppe** (alle Positionen tragen denselben Text);
     Kombigeraet-Verhalten **gemaess Rueckfrage 2** (Empfehlung: Treffer, wenn eine der Abteilungen passt).
3. **Sicht 2:** Spalte + Filter **gemaess Rueckfrage 3** (Variante A/B siehe Loesungsentwurf).
4. **Ohne Abteilung:** Zelle **leer**.
   - *Variante A:* `Arbeitsbereich` NULL/leer an der Position (laut Screenshot z. B. `G-KT-Korpus-Gepunktet`).
   - *Variante B:* keine OrderInfo-Zeile oder alle `MontageAbteilung` NULL/leer — wie die FA-Liste
     (`ProductionOrdersController.cs:235`, Zeilenfeld bleibt `null`).
   Der Gruppenkopf zeigt in beiden Varianten weiterhin die FAInfos-Abteilung bzw. „—" (`Index.cshtml:114`), unveraendert.
5. **Spaltenauswahl:** Spalte ist ueber das Zahnrad ein-/ausblendbar und umsortierbar; Einstellung
   ueberlebt einen Reload (Registrierung, Rueckfrage 4).
6. Keine Aenderung an Zugriff, Toggle, Reitern (`VMBedarf`), KW-Filter, Paging-Einheiten.

## Technischer Loesungsentwurf

Muster: Listen-View-Pattern ([[0005-listen-view-pattern-mit-server-side-spaltenfilter]]),
Universal-Filter-Pattern „Getter liefert gerenderten Text" ([[fallstricke]] §3). Keine neue
Abhaengigkeit, kein Repository-Aufruf mehr als heute — beide Sichten laden `allInfos` bereits
(`VormontageService.cs:162,195`).

Zwei Varianten, **keine Entscheidung** (Rueckfrage 1). Punkte 4–6 gelten fuer beide.

### Variante A — Knotenfeld `FaHierarchyNode.Arbeitsbereich`

**A1. Sicht 1 — ein Map-Eintrag.** Die Abteilung haengt am Knoten, also reicht der statische
`ColumnMap` (`VormontageService.cs:79-90`): `["abteilung"] = n => n.Arbeitsbereich`; Zelle
`<td>@pos.Arbeitsbereich</td>`. Vorbild: Kommissionierlisten (`KommissionierListenService.cs:64`,
`FaHierarchyKommissionierListen/Index.cshtml:82,151`). Kein Formatierer, kein Request-Lookup, kein Join
ueber `FaHierarchyOrderInfo`, kein Kombigeraete-Problem (Rueckfrage 2 entfaellt).
Ob der Col-Key `abteilung` oder (wie in den Kommissionierlisten) `arbeitsbereich` heisst, folgt dem
Label aus Rueckfrage 4 — in allen drei Registrierungen identisch.

**A2. Folgen fuer Gruppen-Paging/Seitenzaehler (Sicht 1).** Der Filter laeuft **je Position vor der
Gruppierung** (`FaHierarchyListBuilder.cs:135-137`); gruppiert/gezaehlt wird danach (`:140-144`).
Folgen, die der Mensch kennen muss:
- Eine `HauptFA`-Gruppe erscheint, sobald **eine** Position passt, und zeigt dann **nur** die passenden
  Positionen — die Gruppe ist im gefilterten Zustand unvollstaendig. Das ist dasselbe Verhalten wie bei
  den heutigen Positionsfiltern `matchcode`/`artnr`/`material`, also keine neue Semantik.
- `TotalGroupCount`/Seitenzahl zaehlen Gruppen mit ≥ 1 passender Position (Gruppen-Paging bleibt
  Seiteneinheit, `:146-149`).
- Der Gruppenkopf zeigt weiter die FAInfos-Abteilung (`Index.cshtml:114`); dieser Wert kann von den
  `Arbeitsbereich`-Werten der Positionen abweichen. Ob das so bleiben soll, haengt an Rueckfrage 1
  (nicht in Scope, siehe Out-of-Scope Gruppenkopf).
- `!X` blendet nur Positionen mit Bereich `X` aus, nicht ganze Gruppen.

**A3. Sicht 2 — Vorfilter je Knoten.** Analog zum KW-Vorfilter (`VormontageService.cs:233-241`) die
Positionen **vor** der Aggregation per `ColumnFilterHelper.Apply` mit einer Ein-Key-Map
(`["abteilung"] = n => n.Arbeitsbereich`) einschraenken; `colf_abteilung` **nicht** in
`AggregatColumnMap` (`:97-103`, `Apply` ignoriert unbekannte Keys, `ColumnFilterHelper.cs:81`).
Die Summen enthalten dann exakt die Mengen der gefilterten Bereiche — keine Kombi-Doppelzaehlung, kein
Kombi-Hinweis noetig. Anzeige: neue Eigenschaft an `FaHierarchyVormontageAggregatViewModel` = distinct,
sortiert, kommagetrennt ueber die `Arbeitsbereich`-Werte der beitragenden Positionen (ein Matchcode kann
in mehreren Bereichen vorkommen). Alternative „Teil des Schluessels" (Rueckfrage 3): Gruppierung nach
`(Matchcode, Arbeitsbereich)` — derselbe Matchcode erscheint dann je Bereich in einer eigenen Zeile,
ohne Mischzeile.

### Variante B — Kopffeld `FaHierarchyOrderInfo.MontageAbteilung` (bisheriger Entwurf)

**B1. Ein Formatierer, zwei Verwender (Zelle = Getter).**
`VormontageService` bekommt eine statische Funktion, z. B.
`public static string? FormatAbteilungen(IEnumerable<FaHierarchyOrderInfo> infos)`:
nicht-leere `MontageAbteilung` trimmen, distinct (Vergleichsart → Rueckfrage 2 / P1), sortiert,
mit `", "` verbinden; keine → `null`. Die View ruft sie mit `group.OrderInfos` auf, der Getter mit
denselben Zeilen — beide stammen aus `allInfos` (`FaHierarchyListBuilder.cs:152-163` bzw. dem neuen
Lookup), damit Zelltext und Filtertext identisch sind.

**B2. Sicht 1 — Getter mit Request-Lookup.** `ColumnMap` ist `static readonly` und kennt nur den
Knoten (`VormontageService.cs:79-90`); die Abteilung haengt am `HauptFA`. In
`BuildEinzelteileAsync` (`:153-180`) deshalb vor `_builder.Build(...)`:
- Lookup `HauptFA → FormatAbteilungen(...)` aus `allInfos` bauen (Dictionary, fan-out-frei);
- eine Request-Kopie der Map uebergeben: `new Dictionary<…>(ColumnMap) { ["abteilung"] = n => lookup.GetValueOrDefault(n.HauptFA) }`.
Der Builder filtert vor dem Gruppieren (`FaHierarchyListBuilder.cs:136-144`); weil alle Positionen
eines `HauptFA` denselben Abteilungstext haben, faellt eine Gruppe immer ganz raus oder bleibt ganz —
`TotalGroupCount` bleibt korrekt.
Kombigeraet-Filter ergibt sich bei der empfohlenen Darstellung ohne Zusatzcode: `Apply` prueft
`value.Contains(token)` (`ColumnFilterHelper.cs:88`), „A" trifft also „A, B". Folge, die der Mensch
kennen muss: `!A` blendet das Kombigeraet „A, B" **komplett** aus (auch seinen B-Anteil).

**B3. Sicht 2 — abhaengig von Rueckfrage 3.** Empfohlene Variante (Spec-Agent):
- **Filter VOR der Aggregation** — gleiches Muster wie der KW-Vorfilter (`VormontageService.cs:233-241`):
  aus `allInfos` die `HauptFA`-Menge bestimmen, deren `FormatAbteilungen`-Text den Filter erfuellt
  (wiederverwendet `ColumnFilterHelper.Apply` mit einer Ein-Key-Map, kein neuer Parser), und die
  Positionen darauf einschraenken. So zeigen die Summen nur den Bedarf der gefilterten Abteilung.
  `colf_abteilung` wird **nicht** in `AggregatColumnMap` (`:97-103`) eingetragen — `Apply` ignoriert
  unbekannte Keys (`ColumnFilterHelper.cs:81`), der Nachfilter auf der Projektion entfaellt damit.
- **Anzeige:** neue Eigenschaft (z. B. `Abteilungen`) an `FaHierarchyVormontageAggregatViewModel`
  (`:16-32`) = `FormatAbteilungen` ueber die OrderInfos **aller** `HauptFA`, die zur Aggregatzeile
  beitragen (nach Vorfilter). Gruppierschluessel bleibt `Matchcode` (`:245-256`).
- **Melden statt still:** Liegt im (gefilterten) Ergebnis ein Kombigeraet, dessen Mengen damit
  vollstaendig in der gefilterten Abteilung mitgezaehlt werden, zeigt die Seite einen sichtbaren
  Hinweis (Anzahl betroffener `HauptFA`, Text sinngemaess „Mengen von Kombigeraeten sind nicht
  nach Abteilung trennbar"). Begruendung: ohne Trennschluessel auf Positionsebene
  ([[2026-08-06-kombinationsgeraete-montageabteilung]], Punkt 1) zaehlt jede Kombi-Position in jeder
  ihrer Abteilungen voll.

Alternative (falls Rueckfrage 3 = „Teil des Summierschluessels"): Gruppierung nach
`(Matchcode, FormatAbteilungen(HauptFA))`. Ein Kombigeraet landet dann in einer eigenen Zeile
„A, B" (keine Doppelzaehlung), derselbe Matchcode erscheint aber in bis zu drei Zeilen (A / B /
„A, B"). Filter dann auf der Projektion wie die anderen Spalten (`AggregatColumnMap`).

### Gemeinsam (A und B)

**4. Views.**
- `Index.cshtml`: `<th data-filterable data-col-key="abteilung">Abteilung</th>` an der in Rueckfrage 4
  bestaetigten Position, Zelle *A:* `@pos.Arbeitsbereich` / *B:* `@VormontageService.FormatAbteilungen(group.OrderInfos)`,
  `colCount` 8 → 9 (`:6`), Eintrag im `#column-config` (`:153-164`).
- `Summiert.cshtml`: analog, `colCount` 3 → 4 (`:7`), `#column-config` (`:112-118`); Kombi-Hinweis-Alert nur Variante B.
- Skript-Reihenfolge `column-preferences.js` vor `table-filter.js` ist bereits richtig
  (`Index.cshtml:167-168`, `Summiert.cshtml:121-122`), nicht anfassen.
- Bestandsnutzer mit gespeicherten Spalten-Prefs: `column-preferences.js` `mergeWithDefaults`
  (`:75-105`) fuegt neue Keys an der Nachbarposition des Configs ein, sichtbar gemaess `defaultHidden`.

**5. `ColumnDefinitions`.** Neuer `ColumnDef("abteilung", "Abteilung", Locked: false)` in
`FaHierarchyVormontageEinzeln` (`:400-415`) und `FaHierarchyVormontageSummiert` (`:423-433`), selbe
Reihenfolge wie das `#column-config`. ViewKeys sind schon registriert (`:481-482`).

**6. Drift-Waechter.** `KommissionierSpaltenKonsistenzTests` prueft A/B/C(+D Druck) nur fuer die
Kommissionierlisten (`IdealAkeWms.Tests/Views/KommissionierSpaltenKonsistenzTests.cs:40-44`). Fuer die
beiden Vormontage-Views einen gleichartigen Test (nur A/B/C, kein Druck) ergaenzen — kleinster Weg:
die vorhandenen Extraktions-Helfer wiederverwenden. Grund: genau diese Drift ist [[fallstricke]] §3
(„Neue Spalte in einer filterable-table braucht ZWEI Registrierungen").

**Pruefabfragen fuer den Menschen** (am IDEAL-Testsystem, WMS-DB — der Cache spiegelt die Sage-View):

```sql
-- P1: Kombigeraete und Doppel-Abteilungen je HauptFA
SELECT HauptFA, COUNT(*) AS Zeilen, COUNT(DISTINCT MontageAbteilung) AS Abteilungen
FROM dbo.FaHierarchyOrderInfos GROUP BY HauptFA HAVING COUNT(*) > 1 ORDER BY Zeilen DESC;
-- P2: Leere Abteilungen
SELECT COUNT(*) FROM dbo.FaHierarchyOrderInfos
WHERE MontageAbteilung IS NULL OR LTRIM(RTRIM(MontageAbteilung)) = '';
-- P3: Werte mit Komma (kollidieren mit dem Komma-Trenner von Anzeige UND Filter-Syntax)
SELECT DISTINCT MontageAbteilung FROM dbo.FaHierarchyOrderInfos WHERE MontageAbteilung LIKE '%,%';
-- P4: Wertevorrat Abteilung vs. VMBedarf-Reiter vs. Arbeitsbereich (zu Rueckfrage 1)
SELECT DISTINCT MontageAbteilung FROM dbo.FaHierarchyOrderInfos ORDER BY 1;
SELECT DISTINCT VMBedarf FROM dbo.FaHierarchyNodes WHERE VMBedarf IS NOT NULL AND VMBedarf <> '' ORDER BY 1;
SELECT DISTINCT Arbeitsbereich FROM dbo.FaHierarchyNodes WHERE Arbeitsbereich IS NOT NULL AND Arbeitsbereich <> '' ORDER BY 1;
-- P5: Abgleich mit dem Sage-Screenshot (unveraendert aus [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]],
--     Abschnitt "Vorbild aus Sage"; zu Rueckfrage 1)
SELECT Artnr, Bezeichnung1, Arbeitsbereich, Arbeitsschritte
FROM dbo.FaHierarchyNodes
WHERE Artnr IN ('G-KT-A-BODEN-1320-660-V01', 'FR-4301-40-20-1,25-1530,0MM-V01',
                'G-KT-Mittelholmblende-40-760-V02', 'G-KT-Korpus-Gepunktet')
   OR Bezeichnung1 IN ('G-KT-A-BODEN-1320-660-V01', 'G-KT-Korpus-Gepunktet');
-- Erwartung laut Screenshot (Spalte Abteilung): A-BODEN = H1-03; FR-4301-...-1530 = S-01;
--   Mittelholmblende = H2-02; Korpus-Gepunktet = (leer)
-- P6: Kopf vs. Knoten fuer dieselben HauptFA (zu Rueckfrage 1)
SELECT n.HauptFA, i.MontageAbteilung, n.Arbeitsbereich, COUNT(*) AS Positionen
FROM dbo.FaHierarchyNodes n
LEFT JOIN dbo.FaHierarchyOrderInfos i ON i.HauptFA = n.HauptFA
WHERE n.HauptFA IN (SELECT HauptFA FROM dbo.FaHierarchyNodes
                    WHERE Artnr = 'G-KT-A-BODEN-1320-660-V01')
GROUP BY n.HauptFA, i.MontageAbteilung, n.Arbeitsbereich ORDER BY 1, 2, 3;
```

**Beziehung P4 ↔ P5/P6:** P4 vergleicht nur **Wertevorraete** (gleiche Code-Form `H1-03`/`K-02` in beiden
Feldern waere ein Hinweis, kein Beweis). P5 prueft **zeilengenau** gegen den Screenshot: treffen alle vier
Erwartungen fuer `Arbeitsbereich` zu, spricht das fuer Variante A. P6 zeigt fuer eine Struktur aus dem
Screenshot, ob die FAInfos-Abteilung einem der Knotenwerte entspricht oder ein eigenes Vokabular ist
(der Join in P6 ist eine reine Pruefabfrage; er faechert bei Kombigeraeten bewusst auf). Kein Ergebnis
wird hier vorweggenommen — Antwort in Rueckfrage 1.

Bis P1–P6 gelaufen sind, gelten als **unverifiziert**: welches Feld die Sage-Spalte „Abteilung" ist, Haeufigkeit von Kombigeraeten, ob
Doppel-Abteilungen je `HauptFA` vorkommen, ob Kommas in Werten vorkommen. Liefert P3 Treffer, ist
der Komma-Trenner der Anzeige nicht eindeutig (dann Trenner `" / "` erwaegen — Filter-Token-Trenner
bleibt `,`, `ColumnFilterHelper.cs:54`).

## Migrations-/SQL-Auswirkungen

Keine. Kein neues Feld, keine Migration, kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql` unveraendert,
keine AgentJobs betroffen. `MontageAbteilung` existiert bereits (`SQL/89_AddFaHierarchy.sql:62`,
`SQL/00_FreshInstall.sql:1807`); ebenso `FaHierarchyNodes.Arbeitsbereich` (Migration
`20260807105825_AddFaHierarchy.cs:35`, `nvarchar(200)`). Gilt fuer **beide** Varianten. Der Sync bleibt unveraendert.

## Audit-Feld-Auswirkungen

Keine. Reine Lesefunktion; `FaHierarchyOrderInfo` ist bewusst keine `AuditableEntity`
(Cache-Tabelle, `FaHierarchyOrderInfo.cs:10`), ebenso `FaHierarchyNode` (`FaHierarchyNode.cs:8`);
beide werden in keiner Variante geschrieben.

**Rollen/Zugriff:** unveraendert — Class-Level `[RequireVorbauAccess]` + Feature-Gate
`[RequireFaHierarchyVormontageAktiv]` (`FaHierarchyVormontageController.cs:29-30`). Kein neuer Filter,
keine neue Rolle, `RoleOverview` unberuehrt.

## Akzeptanzkriterien

AK 1–6 gibt es je Variante (A = Knotenfeld, B = Kopffeld); nach Rueckfrage 1 gilt nur der eine Satz,
der andere wird im Rework als ueberholt markiert. AK 4B–6B gelten fuer die **empfohlene** Variante zu
Rueckfrage 2/3; AK 4A–5A fuer die empfohlene Variante zu Rueckfrage 3.

**Variante A (Knotenfeld `Arbeitsbereich`)**

1A. Sicht 1 zeigt die Spalte „Abteilung"; jede Positionszeile zeigt **ihren eigenen** `Arbeitsbereich`
   (zwei Positionen derselben Gruppe mit `H1-03` und `H2-02` zeigen verschiedene Werte).
2A. Position mit `Arbeitsbereich` NULL/leer: Zelle leer; der Gruppenkopf ist unveraendert.
3A. Sicht 1, Filter `colf_abteilung=<Wert>`: nur Positionen, deren `Arbeitsbereich` den Wert (Teilstring,
   case-insensitiv) enthaelt; eine Gruppe mit mindestens einer passenden Position erscheint mit genau den
   passenden Positionen; `TotalCount` zaehlt Gruppen mit ≥ 1 Treffer; `!<Wert>` blendet nur die
   betroffenen Positionen aus.
4A. Sicht 2 zeigt je Aggregatzeile die (distinct, sortiert, kommagetrennt) `Arbeitsbereich`-Werte der
   beitragenden Positionen; Filter „A" reduziert die Summen auf Positionen mit Bereich A (Vorfilter vor
   der Aggregation), `TotalCount` zaehlt Aggregatzeilen nach Filter.
5A. Ein Kombigeraet (zwei OrderInfo-Zeilen) hat keinen Einfluss auf Anzeige oder Filter der Spalte;
   kein Kombi-Hinweis.
6A. (entfaellt)

**Variante B (Kopffeld `MontageAbteilung`)**

1B. Sicht 1 zeigt die Spalte „Abteilung"; fuer einen `HauptFA` mit genau einer OrderInfo-Zeile steht
   deren `MontageAbteilung` in **jeder** Positionszeile der Gruppe.
2B. Fuer einen `HauptFA` ohne nicht-leere `MontageAbteilung` ist die Zelle leer; der Gruppenkopf zeigt
   weiterhin „—".
3B. Sicht 1, Filter `colf_abteilung=<Wert>`: nur Gruppen, deren Abteilungstext den Wert (Teilstring,
   case-insensitiv) enthaelt; `TotalCount` der Pagination zaehlt die verbleibenden Gruppen.
4B. Kombigeraet (zwei OrderInfo-Zeilen, Abteilungen A und B): die Zelle zeigt „A, B" (sortiert,
   distinct); die Positionsanzahl der Gruppe ist identisch mit dem Stand ohne die neue Spalte; Filter
   „A" und Filter „B" zeigen die Gruppe jeweils, Filter „!A" blendet sie aus.
5B. Sicht 2 zeigt je Aggregatzeile die Abteilungen aller beitragenden `HauptFA`; Filter „A" reduziert
   die Summen auf Positionen von `HauptFA` mit Abteilung A (Vorfilter vor der Aggregation),
   `TotalCount` zaehlt Aggregatzeilen nach Filter.
6B. Sicht 2: Enthaelt das gefilterte Ergebnis ein Kombigeraet, erscheint ein sichtbarer Hinweis mit der
   Anzahl betroffener `HauptFA`; ohne Kombigeraet erscheint kein Hinweis.

**Beide Varianten**
7. Spalte ist in beiden Sichten ueber das Zahnrad ausblendbar; nach Reload bleibt die Wahl erhalten
   (keine 400 der Prefs-API).
8. Fuer beide Vormontage-Views sind die Key-Mengen von `<th data-col-key>`, `#column-config` und
   `ColumnDefinitions` identisch (Drift-Waechter-Test gruen), Reihenfolge `#column-config` = `ColumnDefinitions`.
9. Reiter (`VMBedarf`), KW-Filter und bestehende Spaltenfilter verhalten sich unveraendert
   (bestehende `VormontageServiceTests` gruen).
10. `dotnet build` und `dotnet test` gruen.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` mit der **naechsten freien TS-Nummer zum
Umsetzungszeitpunkt** (Buendel aktuell bis TS-80), Querverweis auf TS-61 (Vormontage-Grundfunktion);
Version = **naechste freie zum Umsetzungszeitpunkt** (Buendel aktuell v1.47.2). Danach
`secondbrain/tests/testszenarien-index.md` nachziehen.

**Automatisiert (`VormontageServiceTests`, InMemory/DB-frei):**
- *A:* Sicht 1 Getter: eine Gruppe mit Positionen `H1-03`/`H2-02`, Filter „H1" → Gruppe bleibt mit nur
  der `H1-03`-Position; `TotalGroupCount` zaehlt Gruppen mit Treffer; „!H1" entfernt nur die Position.
- *A:* Sicht 2 Vorfilter: gleicher Matchcode an Positionen verschiedener Bereiche → Filter reduziert die
  Summe auf den Bereich; Anzeige der Bereiche je Aggregatzeile korrekt.
- *B:* Formatierer: null/leer ignoriert, trim, distinct, sortiert, „A, B"; keine → `null`.
- *B:* Sicht 1 Getter: Filter auf Abteilung behaelt/entfernt ganze Gruppen; `TotalGroupCount` korrekt.
- *B:* Sicht 1 Kombigeraet: Positionsanzahl 1:1, Filter „A"/„B" treffen, „!A" entfernt.
- *B:* Sicht 2 Vorfilter: zwei `HauptFA` gleicher Matchcode, verschiedene Abteilungen → Filter reduziert
  die Summe auf einen; Abteilungs-Anzeige der Aggregatzeile korrekt; Kombi-Hinweis-Zaehler.
- Drift-Waechter A/B/C fuer `Index.cshtml` und `Summiert.cshtml` (beide Varianten).

**Manuell am IDEAL-Testsystem** (Vorbedingung: Toggle `FaHierarchyVormontageAktiv = true`, Benutzer mit
`vorbau`/`admin`, produktivnahe Daten; *B:* P1 liefert mindestens ein Kombigeraet, sonst Kombi-Faelle als
„nicht pruefbar" protokollieren):
1. Sicht 1 Grunddarstellung: Spalte „Abteilung" vorhanden. *A:* Werte je Position stimmen mit der
   Sage-Spalte „Abteilung" in „Fertigungsauftraege bearbeiten" fuer dieselbe Struktur ueberein (Abgleich
   wie P5). *B:* Wert = Abteilung im Gruppenkopf.
2. Spaltenfilter eintippen + ENTER: *A:* nur passende Positionen, Gruppen ohne Treffer verschwinden;
   *B:* nur passende Gruppen. Seitenzahl passt; Filter entfernen.
3. *B:* Kombigeraet aus P1: Zelle zeigt beide Abteilungen, keine doppelten Positionen; Filter je Abteilung
   und Negation pruefen. *A:* Kombigeraet aus P1 verhaelt sich wie jeder andere `HauptFA`.
4. Sicht 2: Spalte vorhanden; Filter auf eine Abteilung → Summen sinken plausibel; *B:* Kombi-Hinweis
   erscheint nur, wenn ein Kombigeraet beitraegt.
5. Zahnrad: Spalte ausblenden, verschieben, Reload — Einstellung bleibt; Benutzer mit alten Prefs sieht
   die neue Spalte an der Nachbarposition.
6. Kombination mit Reiterwechsel und KW-Filter: Filter bleibt wirksam, Ergebnisse konsistent.
7. Negativ: Benutzer ohne Vorbau-Zugriff → AccessDenied; Toggle aus → Redirect Home (unveraendert).
8. Regression: Kommissionierlisten/Beschichtung/FA-Liste unveraendert; AKE (Toggle aus) unberuehrt.

## Etappen (nur bei epic: true)

Entfaellt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | –      | –      | –      |

## Deploy

**Vorlaeufig** — der QA-Lauf bestaetigt gegen den echten Diff.

- **Web-App:** ja (Service, ViewModel, Views, `ColumnDefinitions`)
- **Service:** nein (Sync unveraendert; `AppVersion.cs` des Service wird nur mitgezaehlt)
- **Migration:** nein
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

Neu nummeriert im Rework 2026-10-06 (alt 5 → 1, alt 1 → 2, alt 2 → 3, alt 3 → 4, alt 4 → 5).

1. **Welches Feld ist „Abteilung"? (frueher 5, geschaerft.)** Der Sage-Screenshot „Fertigungsauftraege
   bearbeiten" (Anhang zu [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]], Abschnitt „Vorbild aus Sage")
   zeigt „Abteilung" **je Strukturzeile** (`H1-03`, `S-01`, `H2-02`, `K-02`; leer bei der Wurzel und z. B.
   `G-KT-Korpus-Gepunktet`). Kandidaten:
   - **A:** Knotenfeld `FaHierarchyNode.Arbeitsbereich` (`Models/FaHierarchyNode.cs:40`; Quelle
     `FAListe.[Arbeitsbereich]`, „positionsbezogen", `…FAListe.sql:40`) — Granularitaet passt zum Screenshot;
   - **B:** Kopffeld `FaHierarchyOrderInfo.MontageAbteilung` (FAInfos `[Abteilung]` aus PPSImport,
     `…FAInfos.sql:9,28`) — je Auftrag, nicht je Strukturzeile.
   Der Feldname entscheidet nichts. Bitte vor der Antwort **P5** laufen lassen (Abfrage aus der
   Backlog-Notiz; Erwartung A-BODEN = `H1-03`, FR-4301-…-1530 = `S-01`, Mittelholmblende = `H2-02`,
   Korpus-Gepunktet = leer), ergaenzend P4 (Wertevorraete) und P6 (Kopf vs. Knoten je `HauptFA`).
   Spannungsfeld, nicht gedeutet: Brain fuehrt `Arbeitsbereich` als „Zielort, keine Werkbank"
   ([[fallstricke]] §15, ADR-0014-Nachtrag), FAInfos nennt `[Abteilung]` „Montage-Abteilung". Plant die
   Vormontage nach dem einen oder dem anderen? Folge der Antwort: **A** → Rueckfrage 2 entfaellt, Filter je
   Position (Loesungsentwurf A2); **B** → bisheriger Entwurf, Rueckfrage 2 bleibt. Auch „beide Spalten" waere
   moeglich — dann Label je Spalte in Rueckfrage 4.
2. **Kombinationsgeraete (aus der Notiz; frueher 1; nur Variante B).** Was zeigt die Spalte, wenn ein `HauptFA` mehrere
   Abteilungen hat, und wie verhaelt sich der Filter? Positionen duerfen nicht verdoppelt werden.
   *Empfehlung der Notiz:* alle Abteilungen kommagetrennt; Filter trifft, wenn eine davon passt.
   *Ergaenzung Spec-Agent:* (a) Das weicht von der FA-Liste ab — dort bleibt die Zeilen-Abteilung
   bei Kombigeraeten **leer**, die Varianten stehen nur in einer Untertabelle
   (`ProductionOrdersController.cs:294-329`, `:235`); ein FA-Listen-Filter auf eine Abteilung blendet
   Kombigeraete dort also aus. Bewusst unterschiedlich, oder angleichen? (b) Folge der Empfehlung:
   `!A` blendet das Kombigeraet „A, B" komplett aus; Filter ist Teilstring („Montage 1" trifft auch
   „Montage 10"), wie ueberall. (c) Gleiche Abteilung in mehreren Zeilen eines `HauptFA` wird
   einmal gezeigt (distinct) — Haeufigkeit unverifiziert (P1). Die Empfehlung passt zum
   KW-Filter-Vorbild „enthalten, sobald **eine** Kopfzeile passt" (`IsoWeekRange.cs:75-97`).
   Entfaellt, wenn Rueckfrage 1 = A.
3. **Summierte Ansicht (aus der Notiz; frueher 2).** Abteilung nur Anzeige oder Teil des Summierschluessels?
   *Notiz:* keine Empfehlung. *Spec-Agent empfiehlt:* Anzeige, Gruppierschluessel bleibt `Matchcode`,
   der Abteilungs-Filter wirkt **vor** der Aggregation (wie der KW-Filter), plus sichtbarer Hinweis,
   wenn Kombigeraete in der Summe stecken (deren Mengen zaehlen voll in jeder ihrer Abteilungen).
   Grund: ein Filter auf der Aggregat-Projektion (heutiger Mechanismus, `VormontageService.cs:259`)
   wuerde eine Zeile behalten, deren Summe Mengen anderer Abteilungen enthaelt — fuer den Werker
   irrefuehrend. Alternative „Teil des Schluessels": siehe Loesungsentwurf B3 bzw. A3. Bei Variante A
   entfaellt der Kombi-Hinweis (Vorfilter je Position ist exakt); die Empfehlung „Anzeige + Vorfilter"
   gilt dort ebenso.
4. **Server-Mode + Registrierung (aus der Notiz; frueher 3).** Spaltenfilter im Server-Mode (ADR 0005) und
   Registrierung an allen Stellen ([[fallstricke]] §3). *Notiz:* keine Empfehlung (ist Pflicht laut
   ADR 0005). *Spec-Agent:* bestaetigen; zusaetzlich zu entscheiden: Spaltenposition (Vorschlag:
   direkt nach `HauptFA` in Sicht 1, direkt nach `Matchcode` in Sicht 2), Default sichtbar (nicht
   `defaultHidden`), nicht `locked`, Label „Abteilung" (oder „Montage-Abt." wie in der FA-Liste,
   `ColumnDefinitions.cs:36`?; bei Variante A auch „Arbeitsbereich" wie in den Kommissionierlisten,
   `ColumnDefinitions.cs:341`, oder „Abteilung" wie in Sage?).
5. **Bezug zu [[2026-08-06-kombinationsgeraete-montageabteilung]] (aus der Notiz; frueher 4).** Loest diese Spec
   sie teilweise mit, oder bleibt die Trennung ein eigener Schritt? *Notiz:* keine Empfehlung.
   *Spec-Agent empfiehlt:* eigener Schritt. Diese Spec macht die Abteilung nur sichtbar und filterbar;
   Trennschluessel auf Positionsebene, getrennte Dokumente und eindeutiger Barcode (dort Punkte 1–3)
   bleiben offen. Allenfalls Punkt 4 („Anzeige in den Listen") wird fuer die Vormontage teilweise
   beruehrt — im Backlog-Eintrag als Querverweis vermerken, nicht als erledigt. Bei Variante A beruehrt
   diese Spec die Kombigeraete-Trennung gar nicht.
   *(Die fruehere Rueckfrage 5 „Begriff Abteilung" ist als geschaerfte Rueckfrage 1 nach vorn gezogen.)*

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. „Abteilung" = Variante A (Knotenfeld `Arbeitsbereich`, je Strukturzeile wie Sage-Screenshot) oder Variante B (FAInfos.[Abteilung], je HauptFA)? Ergebnis P5 bitte mitnotieren →
2. (nur B) Kombinationsgeraete — Anzeige/Filter (Empfehlung: alle kommagetrennt, Treffer bei einer; Abweichung zur FA-Liste bewusst?) →
3. Summierte Ansicht — Anzeige oder Schluessel, Filter vor/nach Aggregation (Empfehlung: Anzeige + Vorfilter; Kombi-Hinweis nur bei B) →
4. Server-Mode + Registrierung; Position, Sichtbarkeit, Label (Empfehlung: nach HauptFA/Matchcode, sichtbar, „Abteilung") →
5. Bezug Kombigeraete-Backlog (Empfehlung: eigener Schritt, nur Querverweis) →
