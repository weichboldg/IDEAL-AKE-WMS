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
  - IdealAkeWms/Services/VormontageService.cs (ColumnMap :79-90, BuildEinzelteileAsync :153-180, AggregateSummiert :216-274) — neuer Formatierer + Getter + Vorfilter
  - IdealAkeWms/Models/ViewModels/FaHierarchyVormontageAggregatViewModel.cs (:16-32) — neue Eigenschaft fuer die Abteilungs-Anzeige in Sicht 2
  - IdealAkeWms/Views/FaHierarchyVormontage/Index.cshtml (colCount :6, th :75-82, Zellen :131-140, column-config :153-164)
  - IdealAkeWms/Views/FaHierarchyVormontage/Summiert.cshtml (colCount :7, th :80-82, Zellen :96-100, column-config :112-118)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (FaHierarchyVormontageEinzeln :400-415, FaHierarchyVormontageSummiert :423-433)
  - IdealAkeWms.Tests/Services/VormontageServiceTests.cs — neue Faelle
  - IdealAkeWms.Tests/Views/ (Drift-Waechter A/B/C fuer die beiden Vormontage-Views, Muster KommissionierSpaltenKonsistenzTests.cs)
  - docs/TESTSZENARIEN.md (neues Kapitel, naechste freie Nummer)
  - IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs + Views/Help/Changelog.cshtml (Versions-Bump)
open_questions:
  - "1 Kombinationsgeraete: Was zeigt die Spalte bei mehreren Abteilungen je HauptFA und wie filtert der Filter? (Notiz-Empfehlung: alle, kommagetrennt; Treffer, wenn eine passt; keine Positions-Verdopplung). Achtung Abweichung zur FA-Liste, die bei Kombigeraeten die Zeilen-Abteilung leer laesst."
  - "2 Summierte Ansicht: Abteilung nur Anzeige oder Teil des Summierschluessels — und wirkt der Abteilungs-Filter vor oder nach der Matchcode-Aggregation? (Notiz: keine Empfehlung; Spec-Agent: Anzeige + Filter VOR der Aggregation)"
  - "3 Spaltenfilter im Server-Mode (ADR 0005) und Spaltenauswahl-Registrierung (fallstricke §3) — Bestaetigung inkl. Spaltenposition und Default-Sichtbarkeit (Notiz: keine Empfehlung; Spec-Agent: Server-Mode, alle drei Stellen, sichtbar, direkt nach HauptFA bzw. Matchcode)"
  - "4 Loest diese Spec [[2026-08-06-kombinationsgeraete-montageabteilung]] teilweise mit, oder bleibt die Trennung dort ein eigener Schritt? (Notiz: keine Empfehlung; Spec-Agent: eigener Schritt, diese Spec trennt nichts)"
  - "5 (Spec-Agent, zusaetzlich) Ist mit 'Abteilung' sicher die Montage-Abteilung aus FAInfos.[Abteilung] gemeint — nicht der VMBedarf-Reiter (Arbeitsbereich) und nicht FaHierarchyNode.Arbeitsbereich?"
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

## Ziel / Nutzen (das Warum)

Die Vormontage plant ihre Arbeit nach Montage-Abteilung. Heute steht die Abteilung in Sicht 1 nur
im Gruppenkopf (`Index.cshtml:111-120`) und ist weder filterbar noch nach Sortieren/Spaltenwahl
neben der Position sichtbar; in Sicht 2 (Summiert) fehlt sie ganz. Mit einer eigenen Spalte und
einem Server-Spaltenfilter kann ein Werker die Liste auf „seine" Abteilung eingrenzen.

Gilt nur am Standort **IDEAL**: die Datenbasis (`FaHierarchyNodes`/`FaHierarchyOrderInfos`) fuellt
nur der IDEAL-Sync; die Vormontage-Listen sind hinter dem Toggle `FaHierarchyVormontageAktiv`
(Default aus, `FaHierarchyVormontageController.cs:29-30`). AKE ist nicht betroffen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Sicht 1 „Einzelne Teile" (`/FaHierarchyVormontage/Index`): neue Spalte „Abteilung" + Server-Spaltenfilter.
- Sicht 2 „Summiert" (`/FaHierarchyVormontage/Summiert`): Spalte + Filter — **Semantik haengt an
  Rueckfrage 2**.
- Spaltenregistrierung an allen drei Stellen (`<th data-col-key>`, `#column-config`,
  `ColumnDefinitions`) + Drift-Waechter-Test fuer beide Vormontage-Views.
- Automatisierte Tests, Testszenarien-Kapitel, Versions-Bump, Changelog (Anwender + Brain).

**Out-of-Scope**
- Echte Trennung von Kombinationsgeraeten nach Abteilung (Positions-Split, getrennte Dokumente,
  eindeutiger Barcode) — bleibt [[2026-08-06-kombinationsgeraete-montageabteilung]] (siehe Rueckfrage 4).
- Vormontage-Druckansicht/PDF — es gibt keine (`Views/FaHierarchyVormontage/` enthaelt nur
  `Index.cshtml` und `Summiert.cshtml`); eigener Backlog [[2026-09-14-vormontage-druckansicht]].
- Kopfzeilen-Darstellung im Gruppenkopf (`Index.cshtml:111-120`) bleibt unveraendert.
- Hilfeseite: einen Vormontage-Abschnitt gibt es in `Views/Help/` nicht (nur Changelog-Treffer);
  wird hier nicht neu angelegt, nur der Changelog-Eintrag.
- Nebenbefund, nicht Teil dieser Spec: `ColumnDefinitions.FaHierarchyVormontageEinzeln` sagt
  `SupportsSortDefault: false` (`ColumnDefinitions.cs:402`), das Inline-`#view-config` derselben View
  sagt `"supportsSortDefault": true` (`Index.cshtml:151`). Nicht angefasst.

## Fachliche Anforderungen

Belegte Ausgangslage (Buendel-Stand):

- **Quelle der Abteilung.** `FaHierarchyOrderInfo.MontageAbteilung` (`Models/FaHierarchyOrderInfo.cs:21`).
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
- **Nicht am Strukturknoten.** `FaHierarchyNode` hat kein Abteilungsfeld (Import-SELECT
  `FaHierarchySql.cs:121-127` enthaelt keine Abteilung; `MontageAbteilung` kommt in keinem
  `FaHierarchyNode`-Code vor).
- **Fan-out-Verbot.** Kopfdaten werden nie per Join an Positionen gehaengt; Sicht 1 ordnet sie per
  Dictionary je `HauptFA` zu (`FaHierarchyListBuilder.cs:151-163`).

Anforderungen:

1. **Sicht 1:** Jede Positionszeile zeigt in einer neuen Spalte „Abteilung" die Montage-Abteilung(en)
   ihres `HauptFA`. Darstellung bei mehreren Abteilungen: **gemaess Rueckfrage 1** (Empfehlung:
   alle, distinct, sortiert, kommagetrennt). Positionsanzahl bleibt 1:1 (keine Verdopplung).
2. **Sicht 1 Filter:** Server-Spaltenfilter `colf_abteilung` mit der bestehenden Mini-Syntax
   (Komma = ODER, `!` = NICHT, Teilstring, case-insensitiv — `ColumnFilterHelper.cs:45-60,71-93`).
   Kombigeraet-Verhalten **gemaess Rueckfrage 1** (Empfehlung: Treffer, wenn eine der Abteilungen passt).
3. **Sicht 2:** Spalte + Filter **gemaess Rueckfrage 2**.
4. **HauptFA ohne Abteilung** (keine OrderInfo-Zeile oder alle `MontageAbteilung` NULL/leer): Zelle
   **leer** — wie die FA-Liste (`ProductionOrdersController.cs:235`, Zeilenfeld bleibt `null`).
   Der Gruppenkopf zeigt weiterhin „—" (`Index.cshtml:114`), unveraendert.
5. **Spaltenauswahl:** Spalte ist ueber das Zahnrad ein-/ausblendbar und umsortierbar; Einstellung
   ueberlebt einen Reload (Registrierung, Rueckfrage 3).
6. Keine Aenderung an Zugriff, Toggle, Reitern (`VMBedarf`), KW-Filter, Paging-Einheiten.

## Technischer Loesungsentwurf

Muster: Listen-View-Pattern ([[0005-listen-view-pattern-mit-server-side-spaltenfilter]]),
Universal-Filter-Pattern „Getter liefert gerenderten Text" ([[fallstricke]] §3). Keine neue
Abhaengigkeit, kein Repository-Aufruf mehr als heute — beide Sichten laden `allInfos` bereits
(`VormontageService.cs:162,195`).

**1. Ein Formatierer, zwei Verwender (Zelle = Getter).**
`VormontageService` bekommt eine statische Funktion, z. B.
`public static string? FormatAbteilungen(IEnumerable<FaHierarchyOrderInfo> infos)`:
nicht-leere `MontageAbteilung` trimmen, distinct (Vergleichsart → Rueckfrage 1 / P1), sortiert,
mit `", "` verbinden; keine → `null`. Die View ruft sie mit `group.OrderInfos` auf, der Getter mit
denselben Zeilen — beide stammen aus `allInfos` (`FaHierarchyListBuilder.cs:152-163` bzw. dem neuen
Lookup), damit Zelltext und Filtertext identisch sind.

**2. Sicht 1 — Getter mit Request-Lookup.** `ColumnMap` ist `static readonly` und kennt nur den
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

**3. Sicht 2 — abhaengig von Rueckfrage 2.** Empfohlene Variante (Spec-Agent):
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

Alternative (falls Rueckfrage 2 = „Teil des Summierschluessels"): Gruppierung nach
`(Matchcode, FormatAbteilungen(HauptFA))`. Ein Kombigeraet landet dann in einer eigenen Zeile
„A, B" (keine Doppelzaehlung), derselbe Matchcode erscheint aber in bis zu drei Zeilen (A / B /
„A, B"). Filter dann auf der Projektion wie die anderen Spalten (`AggregatColumnMap`).

**4. Views.**
- `Index.cshtml`: `<th data-filterable data-col-key="abteilung">Abteilung</th>` an der in Rueckfrage 3
  bestaetigten Position, Zelle `@VormontageService.FormatAbteilungen(group.OrderInfos)`,
  `colCount` 8 → 9 (`:6`), Eintrag im `#column-config` (`:153-164`).
- `Summiert.cshtml`: analog, `colCount` 3 → 4 (`:7`), `#column-config` (`:112-118`), ggf. Hinweis-Alert.
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
-- P4: Wertevorrat Abteilung vs. VMBedarf-Reiter (zu Rueckfrage 5)
SELECT DISTINCT MontageAbteilung FROM dbo.FaHierarchyOrderInfos ORDER BY 1;
SELECT DISTINCT VMBedarf FROM dbo.FaHierarchyNodes WHERE VMBedarf IS NOT NULL AND VMBedarf <> '' ORDER BY 1;
```

Bis P1–P4 gelaufen sind, gelten als **unverifiziert**: Haeufigkeit von Kombigeraeten, ob
Doppel-Abteilungen je `HauptFA` vorkommen, ob Kommas in Werten vorkommen. Liefert P3 Treffer, ist
der Komma-Trenner der Anzeige nicht eindeutig (dann Trenner `" / "` erwaegen — Filter-Token-Trenner
bleibt `,`, `ColumnFilterHelper.cs:54`).

## Migrations-/SQL-Auswirkungen

Keine. Kein neues Feld, keine Migration, kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql` unveraendert,
keine AgentJobs betroffen. `MontageAbteilung` existiert bereits (`SQL/89_AddFaHierarchy.sql:62`,
`SQL/00_FreshInstall.sql:1807`). Der Sync bleibt unveraendert.

## Audit-Feld-Auswirkungen

Keine. Reine Lesefunktion; `FaHierarchyOrderInfo` ist bewusst keine `AuditableEntity`
(Cache-Tabelle, `FaHierarchyOrderInfo.cs:10`) und wird hier nicht geschrieben.

**Rollen/Zugriff:** unveraendert — Class-Level `[RequireVorbauAccess]` + Feature-Gate
`[RequireFaHierarchyVormontageAktiv]` (`FaHierarchyVormontageController.cs:29-30`). Kein neuer Filter,
keine neue Rolle, `RoleOverview` unberuehrt.

## Akzeptanzkriterien

AK 4–6 gelten fuer die **empfohlene** Variante; bei anderer Antwort auf Rueckfrage 1/2 werden sie im
Rework angepasst.

1. Sicht 1 zeigt die Spalte „Abteilung"; fuer einen `HauptFA` mit genau einer OrderInfo-Zeile steht
   deren `MontageAbteilung` in **jeder** Positionszeile der Gruppe.
2. Fuer einen `HauptFA` ohne nicht-leere `MontageAbteilung` ist die Zelle leer; der Gruppenkopf zeigt
   weiterhin „—".
3. Sicht 1, Filter `colf_abteilung=<Wert>`: nur Gruppen, deren Abteilungstext den Wert (Teilstring,
   case-insensitiv) enthaelt; `TotalCount` der Pagination zaehlt die verbleibenden Gruppen.
4. Kombigeraet (zwei OrderInfo-Zeilen, Abteilungen A und B): die Zelle zeigt „A, B" (sortiert,
   distinct); die Positionsanzahl der Gruppe ist identisch mit dem Stand ohne die neue Spalte; Filter
   „A" und Filter „B" zeigen die Gruppe jeweils, Filter „!A" blendet sie aus.
5. Sicht 2 zeigt je Aggregatzeile die Abteilungen aller beitragenden `HauptFA`; Filter „A" reduziert
   die Summen auf Positionen von `HauptFA` mit Abteilung A (Vorfilter vor der Aggregation),
   `TotalCount` zaehlt Aggregatzeilen nach Filter.
6. Sicht 2: Enthaelt das gefilterte Ergebnis ein Kombigeraet, erscheint ein sichtbarer Hinweis mit der
   Anzahl betroffener `HauptFA`; ohne Kombigeraet erscheint kein Hinweis.
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
- Formatierer: null/leer ignoriert, trim, distinct, sortiert, „A, B"; keine → `null`.
- Sicht 1 Getter: Filter auf Abteilung behaelt/entfernt ganze Gruppen; `TotalGroupCount` korrekt.
- Sicht 1 Kombigeraet: Positionsanzahl 1:1, Filter „A"/„B" treffen, „!A" entfernt.
- Sicht 2 Vorfilter: zwei `HauptFA` gleicher Matchcode, verschiedene Abteilungen → Filter reduziert
  die Summe auf einen; Abteilungs-Anzeige der Aggregatzeile korrekt; Kombi-Hinweis-Zaehler.
- Drift-Waechter A/B/C fuer `Index.cshtml` und `Summiert.cshtml`.

**Manuell am IDEAL-Testsystem** (Vorbedingung: Toggle `FaHierarchyVormontageAktiv = true`, Benutzer mit
`vorbau`/`admin`, produktivnahe Daten; P1 liefert mindestens ein Kombigeraet, sonst Kombi-Faelle als
„nicht pruefbar" protokollieren):
1. Sicht 1 Grunddarstellung: Spalte „Abteilung" vorhanden, Wert = Abteilung im Gruppenkopf.
2. Spaltenfilter eintippen + ENTER: nur passende Gruppen, Seitenzahl passt; Filter entfernen.
3. Kombigeraet aus P1: Zelle zeigt beide Abteilungen, keine doppelten Positionen; Filter je Abteilung
   und Negation pruefen.
4. Sicht 2: Spalte vorhanden; Filter auf eine Abteilung → Summen sinken plausibel; Kombi-Hinweis
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

1. **Kombinationsgeraete (aus der Notiz).** Was zeigt die Spalte, wenn ein `HauptFA` mehrere
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
2. **Summierte Ansicht (aus der Notiz).** Abteilung nur Anzeige oder Teil des Summierschluessels?
   *Notiz:* keine Empfehlung. *Spec-Agent empfiehlt:* Anzeige, Gruppierschluessel bleibt `Matchcode`,
   der Abteilungs-Filter wirkt **vor** der Aggregation (wie der KW-Filter), plus sichtbarer Hinweis,
   wenn Kombigeraete in der Summe stecken (deren Mengen zaehlen voll in jeder ihrer Abteilungen).
   Grund: ein Filter auf der Aggregat-Projektion (heutiger Mechanismus, `VormontageService.cs:259`)
   wuerde eine Zeile behalten, deren Summe Mengen anderer Abteilungen enthaelt — fuer den Werker
   irrefuehrend. Alternative „Teil des Schluessels": siehe Loesungsentwurf Punkt 3.
3. **Server-Mode + Registrierung (aus der Notiz).** Spaltenfilter im Server-Mode (ADR 0005) und
   Registrierung an allen Stellen ([[fallstricke]] §3). *Notiz:* keine Empfehlung (ist Pflicht laut
   ADR 0005). *Spec-Agent:* bestaetigen; zusaetzlich zu entscheiden: Spaltenposition (Vorschlag:
   direkt nach `HauptFA` in Sicht 1, direkt nach `Matchcode` in Sicht 2), Default sichtbar (nicht
   `defaultHidden`), nicht `locked`, Label „Abteilung" (oder „Montage-Abt." wie in der FA-Liste,
   `ColumnDefinitions.cs:36`?).
4. **Bezug zu [[2026-08-06-kombinationsgeraete-montageabteilung]] (aus der Notiz).** Loest diese Spec
   sie teilweise mit, oder bleibt die Trennung ein eigener Schritt? *Notiz:* keine Empfehlung.
   *Spec-Agent empfiehlt:* eigener Schritt. Diese Spec macht die Abteilung nur sichtbar und filterbar;
   Trennschluessel auf Positionsebene, getrennte Dokumente und eindeutiger Barcode (dort Punkte 1–3)
   bleiben offen. Allenfalls Punkt 4 („Anzeige in den Listen") wird fuer die Vormontage teilweise
   beruehrt — im Backlog-Eintrag als Querverweis vermerken, nicht als erledigt.
5. **Begriff „Abteilung" (zusaetzlich, Spec-Agent).** Ist sicher die Montage-Abteilung aus
   FAInfos.`[Abteilung]` gemeint? Die Vormontage-Listen zeigen bereits den Arbeitsbereich
   `VMBedarf` als Reiter und Spalte (`Index.cshtml:81`), und `FaHierarchyNode` hat ein Feld
   `Arbeitsbereich` (`FaHierarchySql.cs:126`). Die Notiz setzt Montage-Abteilung voraus; der Code kann
   die fachliche Absicht nicht klaeren. Pruefabfrage P4 zeigt beide Wertevorraete nebeneinander.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Kombinationsgeraete — Anzeige/Filter (Empfehlung: alle kommagetrennt, Treffer bei einer; Abweichung zur FA-Liste bewusst?) →
2. Summierte Ansicht — Anzeige oder Schluessel, Filter vor/nach Aggregation (Empfehlung: Anzeige + Vorfilter + Kombi-Hinweis) →
3. Server-Mode + Registrierung; Position, Sichtbarkeit, Label (Empfehlung: nach HauptFA/Matchcode, sichtbar, „Abteilung") →
4. Bezug Kombigeraete-Backlog (Empfehlung: eigener Schritt, nur Querverweis) →
5. „Abteilung" = FAInfos.[Abteilung] (Montage-Abteilung)? →
