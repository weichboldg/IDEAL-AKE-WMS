---
type: spec
title: Vormontage-Listen — Montage-Abteilung als Spalte und Filter
slug: 2026-10-06-vormontage-abteilung-spalte-filter-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-07
source_backlog: "[[2026-10-06-vormontage-abteilung-spalte-filter]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Services/VormontageService.cs — ColumnMap :79-90 ein Eintrag [\"arbeitsbereich\"] (Knotenfeld Arbeitsbereich); AggregateSummiert :216-274 Vorfilter je Knoten vor der Aggregation (analog KW-Vorfilter :233-241) + Bereichs-Anzeige je Aggregatzeile"
  - IdealAkeWms/Models/ViewModels/FaHierarchyVormontageAggregatViewModel.cs (:16-32) — neue Eigenschaft fuer die Arbeitsbereich-Anzeige in Sicht 2
  - "IdealAkeWms/Views/FaHierarchyVormontage/Index.cshtml (colCount :6, th arbeitsbereich direkt nach hauptfa :75, Zellen :131-140, column-config :153-164)"
  - "IdealAkeWms/Views/FaHierarchyVormontage/Summiert.cshtml (colCount :7, th arbeitsbereich direkt nach matchcode :80, Zellen :96-100, column-config :112-118)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (FaHierarchyVormontageEinzeln :400-415 nach hauptfa :406, FaHierarchyVormontageSummiert :423-433 nach matchcode :429; ColumnDef(\"arbeitsbereich\", \"Arbeitsbereich\", Locked: false))"
  - IdealAkeWms.Tests/Services/VormontageServiceTests.cs — neue Faelle (Filter je Position, Vorfilter Sicht 2)
  - IdealAkeWms.Tests/Views/ (Drift-Waechter A/B/C fuer die beiden Vormontage-Views, Muster KommissionierSpaltenKonsistenzTests.cs)
  - docs/TESTSZENARIEN.md (neues Kapitel, naechste freie Nummer)
  - IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs + Views/Help/Changelog.cshtml (Versions-Bump)
open_questions:
  - "3 (vorher 5) Loest diese Spec [[2026-08-06-kombinationsgeraete-montageabteilung]] teilweise mit, oder bleibt die Trennung dort ein eigener Schritt? (Notiz: keine Empfehlung; Spec-Agent: eigener Schritt, diese Spec trennt nichts; mit Variante A beruehrt sie die Kombigeraete-Trennung gar nicht)"
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

> [!warning] Aufgenommen in eine neue Spec (Gerald 2026-11-06)
> Diese Spec wird nicht mehr eigenständig umgesetzt. Ihr Inhalt geht in die Spec zum Backlog-Eintrag
> [[2026-11-06-lagerplatz-etikett-rolle-und-vormontage-felder]] über (Vormontage-Felder inkl. Arbeitsbereich).

> [!note] Umsetzungsort (Entscheidung Gerald 2026-10-09)
> direkt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` auf Zweig `feature/2026-08-07-ideal-teile-1-5` (kein eigener Worktree, ein Lauf zur Zeit). Die Spec baut auf Teil 5
> ([[2026-07-29-standort-ideal-teil-5-spec]]) auf, der nur im Buendel-Worktree
> `feature/2026-08-07-ideal-teile-1-5` existiert. Alle Zeilenangaben unten beziehen sich auf den
> Buendel-Stand `.claude/worktrees/2026-08-07-ideal-teile-1-5` (`AppVersion.cs:5` = **1.47.2**) und
> sind beim Umsetzen gegen `main` nach dem Merge neu zu pruefen.

> [!warning] Vorbehalt P5 — Bedingung fuer diese Spec
> Entscheidung **Variante A** (Knotenfeld `FaHierarchyNode.Arbeitsbereich`) laut Freigabe-Antwort 1
> vom 2026-10-06, **vorbehaltlich P5**. Das P5-Ergebnis steht noch aus.
> **Weicht das Ergebnis von P5 von den Screenshot-Werten ab** (A-BODEN ≠ `H1-03`, FR-4301-…-1530 ≠ `S-01`,
> Mittelholmblende ≠ `H2-02` oder Korpus-Gepunktet nicht leer): **Spec zurueck an Schranke 1** — die
> Feldwahl ist dann neu zu entscheiden. Vor Umsetzungsbeginn P5 ausfuehren und das Ergebnis im
> Freigabe-Block bei Antwort 1 nachtragen lassen.

> [!info] Rework 2026-10-06 — Antwort 1 eingearbeitet
> Variante A ist festgeschrieben. Variante B (Kopffeld `FaHierarchyOrderInfo.MontageAbteilung`) samt AK 1B–6B,
> Formatierer, Request-Lookup, Kombi-Hinweis und B-Testschritten ist **verworfen (Antwort 1, 2026-10-06)** und
> steht nur noch im Abschnitt [[#Protokoll — verworfene Variante B (Antwort 1, 2026-10-06)]]. Rueckfrage 2
> (Kombinationsgeraete, nur B) entfaellt mit Antwort 1. Die Rueckfragen sind neu nummeriert (alt 3/4/5 → neu 1/2/3).

> [!info] Rework 2026-10-07 — Keyuser-Antworten zu Rueckfrage 1 und 2 eingearbeitet
> - **Rueckfrage 1 (Sicht 2):** Anzeige, Gruppierschluessel bleibt `Matchcode`, Filter wirkt **vor** dem
>   Zusammenzaehlen je Position. Die Alternative „Teil des Summierschluessels" ist **verworfen (Antwort 1 Offen,
>   Keyuser 2026-10-07)**.
> - **Rueckfrage 2 (Spalte):** Label **„Arbeitsbereich"**, Col-Key **`arbeitsbereich`** (wie Kommissionierlisten,
>   `ColumnDefinitions.cs:341`); Server-Mode, Registrierung an allen drei Stellen, Position direkt nach `HauptFA`
>   (Sicht 1) bzw. direkt nach `Matchcode` (Sicht 2), Default sichtbar, nicht `locked`. Das Label/der Col-Key
>   **„Abteilung"/`abteilung` ist verworfen (Keyuser 2026-10-07)**.
> - Offen bleibt nur Rueckfrage 3 (Kombigeraete-Backlog).

## Ziel / Nutzen (das Warum)

Die Vormontage plant ihre Arbeit nach Abteilung. Die Sage-Maske „Fertigungsauftraege bearbeiten" zeigt
„Abteilung" **je Strukturzeile**; im WMS ist das das Knotenfeld `FaHierarchyNode.Arbeitsbereich`
(Antwort 1; Brain-Beleg [[fallstricke]] Z. 1118: `Arbeitsbereich` = Sage `USER_OSAbteilung`, Werte wie
`K-02`, `S-01`, `H4-04`). In den Vormontage-Views wird dieses Feld heute gar nicht angezeigt (nur in
Kommissionierlisten `FaHierarchyKommissionierListen/Index.cshtml:82,151` und FA-Struktur
`FaHierarchy/_FaHierarchyNode.cshtml:93`). Im Gruppenkopf von Sicht 1 steht nur die FAInfos-Abteilung
(`MontageAbteilung`, `Index.cshtml:111-120`, unbeschriftet `:114`) — ein anderes Feld, je HauptFA. Mit einer
eigenen Spalte je Position und einem Server-Spaltenfilter kann ein Werker die Liste auf „seine" Abteilung
eingrenzen.

Gilt nur am Standort **IDEAL**: die Datenbasis (`FaHierarchyNodes`) fuellt nur der IDEAL-Sync; die
Vormontage-Listen sind hinter dem Toggle `FaHierarchyVormontageAktiv` (Default aus,
`FaHierarchyVormontageController.cs:29-30`). AKE ist nicht betroffen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Sicht 1 „Einzelne Teile" (`/FaHierarchyVormontage/Index`): neue Spalte „Arbeitsbereich" (Col-Key
  `arbeitsbereich`, direkt nach `HauptFA`) mit dem `Arbeitsbereich` **der Position** + Server-Spaltenfilter je Position.
- Sicht 2 „Summiert" (`/FaHierarchyVormontage/Summiert`): Spalte „Arbeitsbereich" (direkt nach `Matchcode`) als
  **Anzeige** + Filter als **Vorfilter je Position vor der Aggregation** (Antwort 1, Keyuser 2026-10-07).
- Spaltenregistrierung an allen drei Stellen (`<th data-col-key>`, `#column-config`,
  `ColumnDefinitions`) + Drift-Waechter-Test fuer beide Vormontage-Views.
- Automatisierte Tests, Testszenarien-Kapitel, Versions-Bump, Changelog (Anwender + Brain).

**Out-of-Scope**
- Echte Trennung von Kombinationsgeraeten nach Abteilung (Positions-Split, getrennte Dokumente,
  eindeutiger Barcode) — bleibt [[2026-08-06-kombinationsgeraete-montageabteilung]] (siehe Rueckfrage 3).
  Mit Variante A entsteht in dieser Spec **kein** Kombigeraete-Problem (Wert haengt an der Position, nicht
  am HauptFA); der Querverweis bleibt.
- Kein Join/Lookup ueber `FaHierarchyOrderInfo` fuer die neue Spalte (entfaellt mit Variante A).
- Arbeitsschritte-Symbole je Baumzeile — eigener Backlog [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]
  (dieselbe Sage-Vorlage, anderes Feld).
- Vormontage-Druckansicht/PDF — es gibt keine (`Views/FaHierarchyVormontage/` enthaelt nur
  `Index.cshtml` und `Summiert.cshtml`); eigener Backlog [[2026-09-14-vormontage-druckansicht]].
- Kopfzeilen-Darstellung im Gruppenkopf (`Index.cshtml:111-120`, FAInfos-`MontageAbteilung`) bleibt
  unveraendert — auch wenn ihr Wert von den `Arbeitsbereich`-Werten der Positionen abweicht.
- Zielort-Einordnung von `Arbeitsbereich` (ADR 0014) — wird nicht angefasst, keine Werkbank-Ableitung.
- Hilfeseite: einen Vormontage-Abschnitt gibt es in `Views/Help/` nicht (nur Changelog-Treffer);
  wird hier nicht neu angelegt, nur der Changelog-Eintrag.
- Nebenbefund, nicht Teil dieser Spec: `ColumnDefinitions.FaHierarchyVormontageEinzeln` sagt
  `SupportsSortDefault: false` (`ColumnDefinitions.cs:402`), das Inline-`#view-config` derselben View
  sagt `"supportsSortDefault": true` (`Index.cshtml:151`). Nicht angefasst.

## Fachliche Anforderungen

Belegte Ausgangslage (Buendel-Stand), Quelle festgelegt durch Antwort 1:

- **Quelle: Knotenfeld `FaHierarchyNode.Arbeitsbereich`** (`Models/FaHierarchyNode.cs:40`, verifiziert).
  Einziger Schreibpfad: `FaHierarchySyncService` (`IDEALAKEWMSService/Services/FaHierarchySyncService.cs:212,240`)
  aus `f.[Arbeitsbereich]` der Sage-View `vw_IDEAL-AKE_Kommissionierung_FAListe` (`FaHierarchySql.cs:126`),
  Full-Refresh in einer Transaktion (`FaHierarchySyncService.cs:129-138`); kein weiterer Schreiber
  (alle `.Arbeitsbereich`-Treffer in `*.cs`/`*.cshtml` des Buendels sind Lesezugriffe, Mapping
  `ApplicationDbContext.cs:1016` oder Migrationen). Leser: Kommissionierlisten-Spaltenfilter
  (`KommissionierListenService.cs:64`, Spalte `ColumnDefinitions.cs:341`), FA-Struktur-Partial
  (`FaHierarchy/_FaHierarchyNode.cshtml:14,24,93`, Spalte `workarea` `ColumnDefinitions.cs:459`). Kein
  Vormontage-Code liest es (`VormontageService.cs:79-90` enthaelt keinen Eintrag). Granularitaet laut View-Doku
  „Position bevorzugt, Beleg als Fallback (positionsbezogen!)" (`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql:40`)
  — also **je Strukturzeile**, wie die Sage-Spalte im Screenshot. `Str(...)` → NULL moeglich
  (`FaHierarchySyncService.cs:299`).
- **Nur im Brain, nicht am Code belegt:** Der echte CREATE-VIEW-Text liegt nicht im Repo (`…FAListe.sql:57` TODO);
  die Sage-Herkunft `USER_OSAbteilung` steht nur in [[fallstricke]] (Z. 1118, Tabelle Z. 1151) und im
  ADR-0014-Nachtrag. Die Gleichsetzung „Sage-Spalte Abteilung = `Arbeitsbereich`" ist daher bis P5
  **unverifiziert** (siehe Vorbehalt oben).
- **Begriffsnaehe zu `VMBedarf`:** Der Code nennt die `VMBedarf`-Reiter ebenfalls „Arbeitsbereich(e)"
  (`VormontageService.cs:107,122`), und der sichtbare Untertitel von Sicht 2 sagt „summierte Mengensicht des
  gewählten Arbeitsbereichs" (`Summiert.cshtml:68`) — gemeint ist dort der `VMBedarf`-Reiter. Das ist ein
  **anderes** Feld (`n.VMBedarf`) und bleibt unberuehrt. Mit dem Label „Arbeitsbereich" (Antwort 2, Keyuser
  2026-10-07) steht dasselbe Wort in Sicht 2 fuer zwei Felder; die Spec aendert den Untertitel nicht (nicht
  beauftragt), der manuelle Test (Schritt 4) prueft nur, dass die Spalte die Knotenwerte zeigt.
- **ADR-0014-Hinweis (nicht gedeutet):** Brain fuehrt `Arbeitsbereich` als „**Zielort** (wohin das Teil
  kommt) … keine Werkbank" ([[fallstricke]] §15, [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]
  Nachtrag 2026-09-22, Rueckbau [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]]). Diese Spec zeigt
  den Wert nur an und filtert danach; sie aendert an der Zielort-Einordnung nichts und leitet keine Werkbank
  ab. Die Spaltenbeschriftung ist „Arbeitsbereich" (Antwort 2, Keyuser 2026-10-07).
- **Kein Kopf-Join:** Die neue Spalte liest ausschliesslich den Knoten; `FaHierarchyOrderInfo` wird dafuer
  nicht herangezogen. Die bestehende fan-out-freie Kopfzuordnung fuer den Gruppenkopf
  (`FaHierarchyListBuilder.cs:151-163`) bleibt unveraendert.

Anforderungen:

1. **Sicht 1:** Jede Positionszeile zeigt in einer neuen Spalte den `Arbeitsbereich` **dieser Position**
   (je Zeile verschieden moeglich). Positionsanzahl bleibt 1:1 (keine Verdopplung).
2. **Sicht 1 Filter:** Server-Spaltenfilter (`colf_arbeitsbereich`) mit der bestehenden Mini-Syntax
   (Komma = ODER, `!` = NICHT, Teilstring, case-insensitiv — `ColumnFilterHelper.cs:45-60,71-93`). Er wirkt
   **je Position**, wie die vorhandenen Filter `matchcode`/`artnr` (vor der Gruppierung,
   `FaHierarchyListBuilder.cs:135-137`): eine Gruppe bleibt, wenn mindestens eine Position passt, und zeigt
   dann **nur** die passenden Positionen.
3. **Sicht 2:** Spalte zeigt je Aggregatzeile die `Arbeitsbereich`-Werte der beitragenden Positionen
   (Anzeige, Schluessel bleibt `Matchcode`); der Filter wirkt **je Position vor dem Zusammenzaehlen**, die
   Summen enthalten nur Mengen der passenden Positionen (Antwort 1, Keyuser 2026-10-07; Umsetzung A3).
4. **Ohne Arbeitsbereich:** `Arbeitsbereich` NULL/leer an der Position (laut Screenshot z. B.
   `G-KT-Korpus-Gepunktet`) → Zelle **leer**. Der Gruppenkopf zeigt weiterhin die FAInfos-Abteilung bzw. „—"
   (`Index.cshtml:114`), unveraendert.
5. **Spaltenauswahl:** Spalte ist ueber das Zahnrad ein-/ausblendbar und umsortierbar; Einstellung
   ueberlebt einen Reload (Registrierung an allen drei Stellen, Antwort 2). Default sichtbar, nicht `locked`.
6. Keine Aenderung an Zugriff, Toggle, Reitern (`VMBedarf`), KW-Filter, Paging-Einheiten.

## Technischer Loesungsentwurf

Muster: Listen-View-Pattern ([[0005-listen-view-pattern-mit-server-side-spaltenfilter]]),
Universal-Filter-Pattern „Getter liefert gerenderten Text" ([[fallstricke]] §3). Keine neue
Abhaengigkeit, kein zusaetzlicher Repository-Aufruf (beide Sichten laden die Knoten bereits,
`VormontageService.cs:161,194`).

### Variante A — Knotenfeld `FaHierarchyNode.Arbeitsbereich` (festgelegt, Antwort 1)

**A1. Sicht 1 — ein Map-Eintrag.** Der Wert haengt am Knoten, also reicht der statische
`ColumnMap` (`VormontageService.cs:79-90`): `["arbeitsbereich"] = n => n.Arbeitsbereich`; Zelle
`<td>@pos.Arbeitsbereich</td>`. Vorbild: Kommissionierlisten (`KommissionierListenService.cs:64`,
`FaHierarchyKommissionierListen/Index.cshtml:82,151`, `ColumnDefinitions.cs:341`). Kein Formatierer, kein
Request-Lookup, kein Join ueber `FaHierarchyOrderInfo`, kein Kombigeraete-Problem.
Col-Key `arbeitsbereich`, Label „Arbeitsbereich" (Antwort 2, Keyuser 2026-10-07) — in allen drei
Registrierungen identisch. ~~Col-Key `abteilung` / Label „Abteilung"~~ — verworfen (Keyuser 2026-10-07).

**A2. Folgen fuer Gruppen-Paging/Seitenzaehler (Sicht 1).** Der Filter laeuft **je Position vor der
Gruppierung** (`FaHierarchyListBuilder.cs:135-137`); gruppiert/gezaehlt wird danach (`:140-144`).
- Eine `HauptFA`-Gruppe erscheint, sobald **eine** Position passt, und zeigt dann **nur** die passenden
  Positionen — die Gruppe ist im gefilterten Zustand unvollstaendig. Das ist dasselbe Verhalten wie bei
  den heutigen Positionsfiltern `matchcode`/`artnr`/`material`, also keine neue Semantik.
- `TotalGroupCount`/Seitenzahl zaehlen Gruppen mit ≥ 1 passender Position (Gruppen-Paging bleibt
  Seiteneinheit, `:146-149`).
- Der Gruppenkopf zeigt weiter die FAInfos-Abteilung (`Index.cshtml:114`); dieser Wert kann von den
  `Arbeitsbereich`-Werten der Positionen abweichen. Bleibt so (Out-of-Scope Gruppenkopf); im manuellen
  Test wird eine solche Abweichung nur beobachtet, nicht als Fehler gewertet.
- `!X` blendet nur Positionen mit Bereich `X` aus, nicht ganze Gruppen.

**A3. Sicht 2 — Vorfilter je Knoten (festgelegt, Antwort 1 Keyuser 2026-10-07).** Analog zum KW-Vorfilter
(`VormontageService.cs:233-241`) die Positionen **vor** der Aggregation per `ColumnFilterHelper.Apply` mit
einer Ein-Key-Map (`["arbeitsbereich"] = n => n.Arbeitsbereich`) einschraenken; `colf_arbeitsbereich` **nicht** in
`AggregatColumnMap` (`:97-103`; `Apply` ignoriert unbekannte Keys, `ColumnFilterHelper.cs:81`), damit der
Nachfilter auf der Projektion (`:259`) ihn nicht erneut auswertet. Die Summen enthalten dann exakt die
Mengen der gefilterten Bereiche — keine Doppelzaehlung, kein Hinweis-Banner noetig. Anzeige: neue
Eigenschaft an `FaHierarchyVormontageAggregatViewModel` (`:16-32`) = distinct, sortiert, kommagetrennt
ueber die `Arbeitsbereich`-Werte der beitragenden Positionen (ein Matchcode kann in mehreren Bereichen
vorkommen); Gruppierschluessel bleibt `Matchcode` (`:245-256`).
~~Alternative „Teil des Schluessels": Gruppierung nach `(Matchcode, Arbeitsbereich)` — derselbe Matchcode
erscheint dann je Bereich in einer eigenen Zeile, ohne Mischzeile; Filter dann auf der Projektion ueber
`AggregatColumnMap`.~~ — **verworfen (Antwort 1, Keyuser 2026-10-07)**.

### Variante B — verworfen

~~Kopffeld `FaHierarchyOrderInfo.MontageAbteilung`~~ — **verworfen (Antwort 1, 2026-10-06)**. Vollstaendiger
bisheriger Text (B1–B3) im [[#Protokoll — verworfene Variante B (Antwort 1, 2026-10-06)]].

### Gemeinsame Umsetzungsschritte

**4. Views.**
- `Index.cshtml`: `<th data-filterable data-col-key="arbeitsbereich">Arbeitsbereich</th>` direkt nach
  `hauptfa` (`:75`), Zelle `@pos.Arbeitsbereich` an derselben Position, `colCount` 8 → 9 (`:6`), Eintrag im
  `#column-config` (`:153-164`) direkt nach `hauptfa`, nicht `defaultHidden`, nicht `locked`.
- `Summiert.cshtml`: `<th data-filterable data-col-key="arbeitsbereich">Arbeitsbereich</th>` direkt nach
  `matchcode` (`:80`), `colCount` 3 → 4 (`:7`), `#column-config` (`:112-118`) direkt nach `matchcode`.
  Kein Hinweis-Alert.
- Skript-Reihenfolge `column-preferences.js` vor `table-filter.js` ist bereits richtig
  (`Index.cshtml:167-168`, `Summiert.cshtml:121-122`), nicht anfassen.
- Bestandsnutzer mit gespeicherten Spalten-Prefs: `column-preferences.js` `mergeWithDefaults`
  (`:75-105`) fuegt neue Keys an der Nachbarposition des Configs ein, sichtbar gemaess `defaultHidden`.

**5. `ColumnDefinitions`.** Neuer `ColumnDef("arbeitsbereich", "Arbeitsbereich", Locked: false)` in
`FaHierarchyVormontageEinzeln` (`:400-415`, direkt nach `hauptfa` `:406`) und `FaHierarchyVormontageSummiert`
(`:423-433`, direkt nach `matchcode` `:429`), selbe Reihenfolge wie das `#column-config`. ViewKeys sind schon
registriert (`:481-482`).

**7. Anwender-Changelog.** Eintrag in `Views/Help/Changelog.cshtml` nennt die Spalte „Arbeitsbereich"
(nicht „Abteilung") und erklaert in einem Satz: Wert je Position aus Sage; in „Summiert" wirkt der Filter vor
dem Zusammenzaehlen. Einen Vormontage-Hilfeabschnitt gibt es nicht (siehe Out-of-Scope).

**6. Drift-Waechter.** `KommissionierSpaltenKonsistenzTests` prueft A/B/C(+D Druck) nur fuer die
Kommissionierlisten (`IdealAkeWms.Tests/Views/KommissionierSpaltenKonsistenzTests.cs:40-44`). Fuer die
beiden Vormontage-Views einen gleichartigen Test (nur A/B/C, kein Druck) ergaenzen — kleinster Weg:
die vorhandenen Extraktions-Helfer wiederverwenden. Grund: genau diese Drift ist [[fallstricke]] §3
(„Neue Spalte in einer filterable-table braucht ZWEI Registrierungen").

**Pruefabfragen fuer den Menschen** (am IDEAL-Testsystem, WMS-DB — der Cache spiegelt die Sage-View).
P5 ist **Pflicht vor Umsetzungsbeginn** (Vorbehalt oben); P3/P4 ergaenzend. P1, P2, P6 betrafen nur
Variante B und stehen im Protokoll-Abschnitt.

```sql
-- P3 (fuer Variante A auf Arbeitsbereich umgestellt): Werte mit Komma
--     (kollidieren mit dem Komma-Trenner der Sicht-2-Anzeige UND der Filter-Syntax)
SELECT DISTINCT Arbeitsbereich FROM dbo.FaHierarchyNodes WHERE Arbeitsbereich LIKE '%,%';
-- P4 (auf Variante A reduziert): Wertevorrat Arbeitsbereich vs. VMBedarf-Reiter
SELECT DISTINCT Arbeitsbereich FROM dbo.FaHierarchyNodes WHERE Arbeitsbereich IS NOT NULL AND Arbeitsbereich <> '' ORDER BY 1;
SELECT DISTINCT VMBedarf FROM dbo.FaHierarchyNodes WHERE VMBedarf IS NOT NULL AND VMBedarf <> '' ORDER BY 1;
-- P5: Abgleich mit dem Sage-Screenshot (unveraendert aus [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]],
--     Abschnitt "Vorbild aus Sage") — VORBEHALT der Antwort 1
SELECT Artnr, Bezeichnung1, Arbeitsbereich, Arbeitsschritte
FROM dbo.FaHierarchyNodes
WHERE Artnr IN ('G-KT-A-BODEN-1320-660-V01', 'FR-4301-40-20-1,25-1530,0MM-V01',
                'G-KT-Mittelholmblende-40-760-V02', 'G-KT-Korpus-Gepunktet')
   OR Bezeichnung1 IN ('G-KT-A-BODEN-1320-660-V01', 'G-KT-Korpus-Gepunktet');
-- Erwartung laut Screenshot (Spalte Abteilung): A-BODEN = H1-03; FR-4301-...-1530 = S-01;
--   Mittelholmblende = H2-02; Korpus-Gepunktet = (leer)
```

Bis P5 gelaufen ist, gilt als **unverifiziert**, dass die Sage-Spalte „Abteilung" dem Feld `Arbeitsbereich`
entspricht (Antwort 1 stuetzt sich darauf und auf [[fallstricke]] Z. 1118). Liefert P3 Treffer, ist der
Komma-Trenner der Sicht-2-Anzeige nicht eindeutig (dann Trenner `" / "` erwaegen — Filter-Token-Trenner
bleibt `,`, `ColumnFilterHelper.cs:54`).

## Migrations-/SQL-Auswirkungen

Keine. Kein neues Feld, keine Migration, kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql` unveraendert,
keine AgentJobs betroffen. `FaHierarchyNodes.Arbeitsbereich` existiert bereits (Migration
`20260807105825_AddFaHierarchy.cs:35`, `nvarchar(200)`). Der Sync bleibt unveraendert.

## Audit-Feld-Auswirkungen

Keine. Reine Lesefunktion; `FaHierarchyNode` ist bewusst keine `AuditableEntity` (Cache-Tabelle,
`FaHierarchyNode.cs:8`) und wird nicht geschrieben.

**Rollen/Zugriff:** unveraendert — Class-Level `[RequireVorbauAccess]` + Feature-Gate
`[RequireFaHierarchyVormontageAktiv]` (`FaHierarchyVormontageController.cs:29-30`). Kein neuer Filter,
keine neue Rolle, `RoleOverview` unberuehrt.

## Akzeptanzkriterien

Variante A (Knotenfeld `Arbeitsbereich`) gilt laut Antwort 1 (2026-10-06). Sicht 2 = Anzeige + Vorfilter
(Antwort 1, Keyuser 2026-10-07). Col-Key `arbeitsbereich`, Label „Arbeitsbereich" (Antwort 2, Keyuser
2026-10-07). AK 1B–6B: verworfen, siehe Protokoll-Abschnitt.

1A. Sicht 1 zeigt die Spalte **„Arbeitsbereich"** direkt rechts neben `HauptFA`; jede Positionszeile zeigt
   **ihren eigenen** `Arbeitsbereich` (zwei Positionen derselben Gruppe mit `H1-03` und `H2-02` zeigen
   verschiedene Werte).
2A. Position mit `Arbeitsbereich` NULL/leer: Zelle leer; der Gruppenkopf ist unveraendert.
3A. Sicht 1, Filter `colf_arbeitsbereich=<Wert>`: nur Positionen, deren `Arbeitsbereich` den Wert (Teilstring,
   case-insensitiv) enthaelt; eine Gruppe mit mindestens einer passenden Position erscheint mit genau den
   passenden Positionen; `TotalCount` zaehlt Gruppen mit ≥ 1 Treffer; `!<Wert>` blendet nur die
   betroffenen Positionen aus.
4A. Sicht 2 zeigt die Spalte **„Arbeitsbereich"** direkt rechts neben `Matchcode`, je Aggregatzeile die
   (distinct, sortiert, kommagetrennt) `Arbeitsbereich`-Werte der beitragenden Positionen; je Matchcode
   genau eine Zeile (Schluessel bleibt `Matchcode`); Filter „A" reduziert die Summen auf Positionen mit
   Bereich A (Vorfilter vor der Aggregation), `TotalCount` zaehlt Aggregatzeilen nach Filter.
5A. Ein Kombigeraet (zwei OrderInfo-Zeilen) hat keinen Einfluss auf Anzeige oder Filter der Spalte;
   es erscheint kein Hinweis-Banner.
6A. (entfaellt)
7. Spalte ist in beiden Sichten per Default sichtbar und ueber das Zahnrad (Eintrag „Arbeitsbereich")
   ausblendbar; nach Reload bleibt die Wahl erhalten (keine 400 der Prefs-API).
8. Fuer beide Vormontage-Views sind die Key-Mengen von `<th data-col-key>`, `#column-config` und
   `ColumnDefinitions` identisch und enthalten `arbeitsbereich` (kein Key `abteilung`) (Drift-Waechter-Test
   gruen), Reihenfolge `#column-config` = `ColumnDefinitions`.
9. Reiter (`VMBedarf`), KW-Filter und bestehende Spaltenfilter verhalten sich unveraendert
   (bestehende `VormontageServiceTests` gruen).
10. `dotnet build` und `dotnet test` gruen.
11. P5 ist vor Umsetzungsbeginn gelaufen und entspricht den Screenshot-Werten (sonst Spec zurueck an
   Schranke 1, siehe Vorbehalt).

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` mit der **naechsten freien TS-Nummer zum
Umsetzungszeitpunkt** (Buendel aktuell bis TS-80), Querverweis auf TS-61 (Vormontage-Grundfunktion);
Version = **naechste freie zum Umsetzungszeitpunkt** (Buendel aktuell v1.47.2). Danach
`secondbrain/tests/testszenarien-index.md` nachziehen.

**Automatisiert (`VormontageServiceTests`, InMemory/DB-frei):**
- Sicht 1 Getter: eine Gruppe mit Positionen `H1-03`/`H2-02`, Filter „H1" → Gruppe bleibt mit nur
  der `H1-03`-Position; `TotalGroupCount` zaehlt Gruppen mit Treffer; „!H1" entfernt nur die Position.
- Sicht 1: Position mit `Arbeitsbereich = null` → Getter liefert leer, Filter „H1" entfernt sie.
- Sicht 2 Vorfilter: gleicher Matchcode an Positionen verschiedener Bereiche → ohne Filter eine Zeile mit
  beiden Bereichen; Filter reduziert die Summe auf den Bereich; Anzeige der Bereiche je Aggregatzeile korrekt
  (distinct, sortiert).
- Drift-Waechter A/B/C fuer `Index.cshtml` und `Summiert.cshtml` (Key `arbeitsbereich` in allen drei Quellen).

**Manuell am IDEAL-Testsystem** (Vorbedingung: Toggle `FaHierarchyVormontageAktiv = true`, Benutzer mit
`vorbau`/`admin`, produktivnahe Daten; P5 gelaufen und konform):
1. Sicht 1 Grunddarstellung: Spalte „Arbeitsbereich" direkt nach `HauptFA` vorhanden; Werte je Position
   stimmen mit der Sage-Spalte „Abteilung" in „Fertigungsauftraege bearbeiten" fuer dieselbe Struktur ueberein
   (Abgleich wie P5 — in Sage heisst die Spalte weiterhin „Abteilung", im WMS „Arbeitsbereich").
   Weicht der Gruppenkopf (FAInfos-Abteilung) davon ab, nur notieren.
2. Spaltenfilter „Arbeitsbereich" eintippen + ENTER (URL enthaelt `colf_arbeitsbereich=`): nur passende
   Positionen, Gruppen ohne Treffer verschwinden; Seitenzahl passt; Negation `!<Wert>` entfernt nur
   Positionen; Filter entfernen.
3. Kombigeraet (HauptFA mit zwei FAInfos-Zeilen) verhaelt sich wie jeder andere `HauptFA`.
4. Sicht 2: Spalte „Arbeitsbereich" direkt nach `Matchcode` vorhanden, zeigt die Knotenwerte (nicht den
   gewaehlten `VMBedarf`-Reiter); je Matchcode eine Zeile; Filter auf einen Bereich → Summen sinken plausibel
   (nur Mengen dieses Bereichs, vor dem Zusammenzaehlen gefiltert); kein Hinweis-Banner.
5. Zahnrad: Eintrag „Arbeitsbereich" ausblenden, verschieben, Reload — Einstellung bleibt; Benutzer mit alten
   Prefs sieht die neue Spalte an der Nachbarposition.
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
- **Vor Deploy:** P5 am IDEAL-Testsystem gelaufen und konform (Vorbehalt Antwort 1).
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Protokoll — verworfene Variante B (Antwort 1, 2026-10-06)

> [!failure] Verworfen (Antwort 1, 2026-10-06)
> Alles in diesem Abschnitt ist **nicht umzusetzen**. Es bleibt nur als Audit-Trail des Entwurfsstands vor
> Antwort 1 stehen. Der Dev-Lauf ignoriert diesen Abschnitt.

**Fachliche Ausgangslage B (ueberholt):**
- Quelle B: `FaHierarchyOrderInfo.MontageAbteilung` (`Models/FaHierarchyOrderInfo.cs:21`). Einziger
  Schreibpfad `FaHierarchySyncService.ReadOrderInfosAsync` (`FaHierarchySyncService.cs:263,277`) aus
  `SELECT i.[Abteilung] AS [MontageAbteilung]` der Sage-View `vw_IDEAL-AKE_Kommissionierung_FAInfos`
  (`FaHierarchySql.cs:138`); keine weiteren Schreiber; NULL moeglich (`FaHierarchySyncService.cs:299`).
- Granularitaet: eine FAInfos-Zeile = ein Auftrag (ABNr + Pos); Kombinationsgeraete haben denselben `HauptFA`
  mit unterschiedlicher `[Abteilung]` (`…FAInfos.sql:6-8,28`), Herkunft `[PPSImport].[dbo].[vw_PPSData]` (`…FAInfos.sql:9`).
- ~~„Nicht am Strukturknoten — `FaHierarchyNode` hat kein Abteilungsfeld"~~ — schon im ersten Rework
  ueberholt; richtig war nur, dass es am Knoten kein Feld *namens* Abteilung gibt.

**Loesungsentwurf B (verworfen):**
- ~~B1. Statischer Formatierer `VormontageService.FormatAbteilungen(IEnumerable<FaHierarchyOrderInfo>)`:
  trim, distinct, sortiert, `", "`; View und Getter rufen ihn mit denselben OrderInfos auf.~~
- ~~B2. Sicht 1: Request-Lookup `HauptFA → FormatAbteilungen(...)` aus `allInfos` in `BuildEinzelteileAsync`
  (`:153-180`), Request-Kopie der `ColumnMap` mit `["abteilung"] = n => lookup.GetValueOrDefault(n.HauptFA)`;
  Filter wirkt je Gruppe; `!A` blendet das Kombigeraet „A, B" komplett aus.~~
- ~~B3. Sicht 2: HauptFA-Vorfilter vor der Aggregation; Anzeige `Abteilungen` je Aggregatzeile; sichtbarer
  Kombi-Hinweis „Mengen von Kombigeraeten sind nicht nach Abteilung trennbar"; Alternative Schluessel
  `(Matchcode, FormatAbteilungen(HauptFA))`.~~

**Akzeptanzkriterien B (verworfen):**
- ~~1B. Fuer einen `HauptFA` mit genau einer OrderInfo-Zeile steht deren `MontageAbteilung` in jeder Positionszeile.~~
- ~~2B. `HauptFA` ohne nicht-leere `MontageAbteilung`: Zelle leer, Gruppenkopf „—".~~
- ~~3B. Filter behaelt/entfernt ganze Gruppen; `TotalCount` zaehlt verbleibende Gruppen.~~
- ~~4B. Kombigeraet A/B: Zelle „A, B", Positionsanzahl 1:1, Filter „A"/„B" treffen, „!A" blendet aus.~~
- ~~5B. Sicht 2: Abteilungen aller beitragenden `HauptFA`; HauptFA-Vorfilter vor der Aggregation.~~
- ~~6B. Sicht 2: Kombi-Hinweis mit Anzahl betroffener `HauptFA`, sonst kein Hinweis.~~

**Testschritte B (verworfen):**
- ~~Automatisiert: Formatierer (null/leer, trim, distinct, sortiert, „A, B"); Getter je Gruppe; Kombigeraet 1:1 +
  Filter/Negation; Sicht-2-Vorfilter je HauptFA + Kombi-Hinweis-Zaehler.~~
- ~~Manuell: Wert = Abteilung im Gruppenkopf; nur passende Gruppen; Kombigeraet aus P1 zeigt beide
  Abteilungen; Kombi-Hinweis in Sicht 2.~~

**Pruefabfragen nur fuer B (verworfen, Text unveraendert):**

```sql
-- P1: Kombigeraete und Doppel-Abteilungen je HauptFA
SELECT HauptFA, COUNT(*) AS Zeilen, COUNT(DISTINCT MontageAbteilung) AS Abteilungen
FROM dbo.FaHierarchyOrderInfos GROUP BY HauptFA HAVING COUNT(*) > 1 ORDER BY Zeilen DESC;
-- P2: Leere Abteilungen
SELECT COUNT(*) FROM dbo.FaHierarchyOrderInfos
WHERE MontageAbteilung IS NULL OR LTRIM(RTRIM(MontageAbteilung)) = '';
-- P3 (Fassung B): Werte mit Komma
SELECT DISTINCT MontageAbteilung FROM dbo.FaHierarchyOrderInfos WHERE MontageAbteilung LIKE '%,%';
-- P4 (Teil B): Wertevorrat FAInfos-Abteilung
SELECT DISTINCT MontageAbteilung FROM dbo.FaHierarchyOrderInfos ORDER BY 1;
-- P6: Kopf vs. Knoten fuer dieselben HauptFA
SELECT n.HauptFA, i.MontageAbteilung, n.Arbeitsbereich, COUNT(*) AS Positionen
FROM dbo.FaHierarchyNodes n
LEFT JOIN dbo.FaHierarchyOrderInfos i ON i.HauptFA = n.HauptFA
WHERE n.HauptFA IN (SELECT HauptFA FROM dbo.FaHierarchyNodes
                    WHERE Artnr = 'G-KT-A-BODEN-1320-660-V01')
GROUP BY n.HauptFA, i.MontageAbteilung, n.Arbeitsbereich ORDER BY 1, 2, 3;
```

## Offene Rueckfragen

Neu nummeriert im Rework 2026-10-06 nach Antwort 1 (alt 3 → 1, alt 4 → 2, alt 5 → 3). Seit dem Rework
2026-10-07 ist nur noch Rueckfrage 3 offen; die Nummer bleibt 3, damit sie zum Freigabe-Block passt.

3. **Bezug zu [[2026-08-06-kombinationsgeraete-montageabteilung]] (aus der Notiz; vorher 5).** Loest diese Spec
   sie teilweise mit, oder bleibt die Trennung ein eigener Schritt? *Notiz:* keine Empfehlung.
   *Spec-Agent empfiehlt:* eigener Schritt. Mit Variante A beruehrt diese Spec die Kombigeraete-Trennung
   (Trennschluessel, getrennte Dokumente, Barcode — dort Punkte 1–3) gar nicht; im Backlog-Eintrag nur als
   Querverweis vermerken, nicht als erledigt.

> [!note]- Erledigte Rueckfragen (Rework 2026-10-06)
> - **Alt 1 „Welches Feld ist Abteilung?"** — beantwortet: Variante A, vorbehaltlich P5 (Freigabe-Antwort 1).
> - **Alt 2 „Kombinationsgeraete (nur Variante B)"** — entfaellt mit Antwort 1 (dort ausdruecklich: „Frage 2
>   entfällt damit"); kein eigener Antworttext eingetragen.

> [!note]- Erledigte Rueckfragen (Rework 2026-10-07)
> - **1 (vorher 3) Summierte Ansicht** — beantwortet (Keyuser 2026-10-07): Anzeige, Gruppierschluessel bleibt
>   `Matchcode`, Filter wirkt je Position **vor** dem Zusammenzaehlen (wie der KW-Vorfilter
>   `VormontageService.cs:233-241`). Grund der Empfehlung war: ein Filter auf der Aggregat-Projektion
>   (`VormontageService.cs:259`) wuerde eine Zeile behalten, deren Summe Mengen anderer Bereiche enthaelt.
>   ~~Alternative „Teil des Summierschluessels"~~ — verworfen. ~~Kombi-Hinweis / Alternative B3~~ — verworfen
>   mit Antwort 1 (2026-10-06). Eingearbeitet in Anforderung 3, A3, AK 4A.
> - **2 (vorher 4) Server-Mode + Registrierung, Position, Sichtbarkeit, Label/Col-Key** — beantwortet (Keyuser
>   2026-10-07): Label „Arbeitsbereich", Col-Key `arbeitsbereich` (wie `ColumnDefinitions.cs:341`); Server-Mode,
>   Registrierung an allen drei Stellen, direkt nach `HauptFA` (Sicht 1) bzw. `Matchcode` (Sicht 2), Default
>   sichtbar, nicht `locked`. ~~Label „Abteilung" wie Sage, Col-Key `abteilung`~~ — verworfen.
>   ~~„Montage-Abt." wie FA-Liste (`ColumnDefinitions.cs:36`)~~ — verworfen mit Antwort 1 (2026-10-06).
>   Eingearbeitet in In-Scope, Anforderungen 2/5, A1, Schritte 4/5/7, AK 1A/3A/4A/7/8, Testszenarien.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

### Beantwortet / entfallen (2026-10-06)

1. „Abteilung" = Variante A (Knotenfeld `Arbeitsbereich`, je Strukturzeile wie Sage-Screenshot) oder Variante B (FAInfos.[Abteilung], je HauptFA)? Ergebnis P5 bitte mitnotieren → **Variante A – Knotenfeld `Arbeitsbereich` (Gerald 2026-10-06: „in der View ist die Abteilung gleich dem Arbeitsbereich“), vorbehaltlich P5. Stützend: `fallstricke.md:1118` (Arbeitsbereich = Sage `USER_OSAbteilung`, Werte wie K-02, S-01, H4-04 – dieselben Werte wie die Sage-Spalte „Abteilung“). P5-Ergebnis: noch ausstehend. Weicht P5 ab, ist die Antwort neu zu entscheiden. Frage 2 entfällt damit.**
2. (nur B) Kombinationsgeraete — Anzeige/Filter (Empfehlung: alle kommagetrennt, Treffer bei einer; Abweichung zur FA-Liste bewusst?) →

*(Eintrag 2: entfaellt mit Antwort 1 — kein Antworttext eingetragen.)*

### Offen

1. (vorher 3) Summierte Ansicht — Anzeige oder Schluessel, Filter vor/nach Aggregation (Empfehlung: Anzeige + Vorfilter je Position; ~~Kombi-Hinweis nur bei B~~ verworfen mit Antwort 1) → **Wie vorgeschlagen (Keyuser 2026-10-07): Anzeige, Filter wirkt vor dem Zusammenzählen.**
2. (vorher 4) Server-Mode + Registrierung; Position, Sichtbarkeit, Label/Col-Key (Empfehlung: nach HauptFA/Matchcode, sichtbar; Label „Abteilung" wie Sage oder „Arbeitsbereich" wie Kommissionierlisten) → **Label „Arbeitsbereich“ (Keyuser 2026-10-07); Server-Mode/Registrierung, Position und Sichtbarkeit wie vorgeschlagen.**
3. (vorher 5) Bezug Kombigeraete-Backlog (Empfehlung: eigener Schritt, nur Querverweis) → **Eigener Schritt, nur Querverweis im Kombigeraete-Backlog, nicht als erledigt markieren (Gerald 2026-10-09).**

> **FREIGABE-NACHTRAG (Gerald 2026-10-09):** Untertitel der summierten Sicht (`Views/Vormontage/Summiert.cshtml:68`)
> von „summierte Mengensicht des gewählten Arbeitsbereichs“ auf „… des gewählten VM-Bedarfs“ ändern, damit
> „Arbeitsbereich“ nur noch die neue Spalte meint. Reiter-Beschriftungen der `VMBedarf`-Reiter mitprüfen und,
> wo sie „Arbeitsbereich“ heißen, ebenso auf „VM-Bedarf“ umstellen. Geht dem Rumpf vor.
