---
type: spec
title: "Stückliste: Spalte Matchcode + Lagerplatz-Zelle mit Limit und „+n weitere“ (Zahnrad-Teil erledigt)"
slug: 2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec
status: Testbereit
created: 2026-10-16
updated: 2026-10-16
source_backlog: "[[2026-10-06-stueckliste-zahnrad-bug-matchcode]]"
bug: "[[2026-10-16-stueckliste-lagerplatz-zelle-tabellenbreite-bug]]"
task: "[[2026-10-16-stueckliste-matchcode-lagerplatz-limit-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Views/Picking/Bom.cshtml:151-184 (thead: neue th matchcode nach :155; storage-location :172 bekommt data-filterable)"
  - "IdealAkeWms/Views/Picking/Bom.cshtml:240-276 (Zellen: matchcode nach :251; Lagerplatz-Zelle :267-276 mit Limit, Badge, Volltext, data-csv-value)"
  - "IdealAkeWms/Views/Picking/Bom.cshtml:357-371 (emptyColspan +1)"
  - "IdealAkeWms/Views/Picking/Bom.cshtml:660-690 (#column-config: matchcode nach :667)"
  - "IdealAkeWms/Views/Picking/Bom.cshtml:1258-1264 (Druck-colNames: matchcode, storage-location)"
  - "IdealAkeWms/Views/Picking/Bom.cshtml:1308-1430 (Popover-Init vor `if (readOnly) return;` :1430)"
  - "IdealAkeWms/Models/ViewModels/BomViewModels.cs:37-70 (BomItemViewModel.Matchcode) und :151-168 (PrintBomItem.Matchcode)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs:231-268 (Bom: matchcode nach :251, Kommentar :239/:262 korrigieren)"
  - "IdealAkeWms/Controllers/PickingController.cs:319-439 (Bom: MatchcodeLookup ueber Ressourcenummer) und :585-666 (RenderPrintBomAsync)"
  - "IdealAkeWms/Services/ReadOnlyBomBuilder.cs:15-148 (Ctor + IArticleRepository/ILogger, Matchcode)"
  - "IdealAkeWms/Data/Repositories/StockMovementRepository.cs:466-471 (Sortierung: Hauptlagerplatz, Menge absteigend, Code)"
  - "IdealAkeWms/Views/Picking/PrintBom.cshtml:196-260 (ShowCol matchcode)"
  - "IdealAkeWms/wwwroot/css/site.css (neue Klasse fuer Badge/Popover, site.css:250-253 bleibt)"
  - "IdealAkeWms/Views/Help/Index.cshtml:1246-1335 (Abschnitt Kommissionierung/Stueckliste)"
  - "IdealAkeWms.Tests/Controllers/FaCompletionControllerTests.cs:55, FaWorklistControllerTests.cs:73, Services/ReadOnlyBomBuilderTests.cs:33 (Ctor)"
  - "IdealAkeWms.Tests/Repositories/StockMovementRepositoryTests.cs:224-244 (Sortierung)"
open_questions:
  - "Matchcode-Quelle an IDEAL: A Artikelstamm an beiden Standorten (Empfehlung) oder B Strukturknoten mit Artikelstamm als Fallback (wie Kommissionierliste/Vormontage/Beschichtung)? Vorher Pruefabfrage P1 (WMS-DB) und P2 (Sage IDEAL) laufen lassen."
  - "Spalte Lagerplatz ist heute NICHT filterbar (Bom.cshtml:172, ColumnDefinitions.cs:239/262). Fuer „Filtern nach verdecktem Platz“ wird sie filterbar gemacht — bestaetigen?"
  - "Druck (PrintBom): volle Lagerplatzliste oder ebenfalls Limit? Empfehlung: volle Liste."
  - "Lagerbestellung-Details (WarehousePicking/Details) gleich loesen? Empfehlung: nein, nicht in dieser Spec."
  - "Ausgestaltung bestaetigen: 3 sichtbare Plaetze als Konstante, Reihenfolge Hauptlagerplatz zuerst dann Menge absteigend (zentral im Repository, wirkt auch auf den Druck), Popover per Hover/Fokus/Tippen, Hauptlagerplatz im Popover als Text markiert."
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-10-16"
---

# Stückliste: Spalte Matchcode + Lagerplatz-Zelle mit Limit

> [!warning] Code-Grundlage, Reihenfolge, Version
> Alle Fundstellen sind gegen den **Bündel-Worktree** `.claude/worktrees/2026-08-07-ideal-teile-1-5` gemessen
> (Stand v1.61.0, `AppVersion.cs:5`), nicht gegen `main`. Umgesetzt wird **im Bündel**, weil `Bom.cshtml` dort stark
> umgebaut ist (hierarchische Stückliste, Backlog-Hinweis).
> - **Kein paralleler /dev im Bündel.** Der Arbeitsgang-Abgleich v1.61.0
>   ([[2026-10-16-arbeitsgang-stammdaten-abgleich-aus-sage-view-spec]]) hat Vorrang. Diese Spec startet erst, wenn dort
>   kein Lauf mehr offen ist.
> - **Versionsnummer erst bei der Umsetzung festlegen**, nach dem dann aktuellen Bündelstand (beide `AppVersion.cs`).
> - Zeilennummern vor dem Start kurz nachmessen, falls das Bündel bis dahin weitergewachsen ist.

> [!important] Freigabe-Nachtrag 2026-10-16 (geht dem Rumpf vor)
> 1. **Matchcode-Quelle = Variante B** (ersetzt Empfehlung A im Lösungsentwurf M): IDEAL-Zeilen (`FaHierarchyBomItem`)
>    zeigen `FaHierarchyNode.Matchcode`, bei leerem Wert den Artikelstamm über `MatchcodeLookup`; AKE-Zeilen (flach,
>    `BomItem`) den Artikelstamm. Umsetzung wie im Absatz „Variante B“: Property `FaHierarchyBomItem.Matchcode`, Mapping
>    `FaHierarchyBomRepository.cs:174-193`, `(bom as FaHierarchyBomItem)?.Matchcode` (getrimmt, leer = null) `?? lookup` an
>    den drei Projektionsstellen. `BomItem` bekommt **keine** Property (fallstricke §10). Grund: Stückliste und
>    Kommissionierliste/Vormontage/Beschichtung zeigen an IDEAL denselben Matchcode. AK 8 und Hilfetext entsprechend B.
> 2. **Quellbezeichnung korrigiert:** Die FA-Struktur liest seit v1.60.x die View aus `Sync:FaHierarchyListeViewName`, auf
>    IDEAL-Test `[vw_IDEAL_AKE_WMS_FAListe]` – nicht mehr `vw_IDEAL-AKE_Kommissionierung_FAListe` (die gibt es dort nicht
>    mehr). P2 daher gegen `vw_IDEAL_AKE_WMS_FAListe`. P1/P2 sind informativ, keine Vorbedingung.
> 3. Lagerplatz-Spalte filterbar; Druck volle Liste; WarehousePicking nicht; Ausgestaltung (a)–(d) wie empfohlen.
> 4. **Hinweis Hauptlagerplatz (IDEAL):** Viele IDEAL-Artikel haben keinen WMS-Hauptlagerplatz (Artikel-Sync meldet
>    „2259 ohne WMS-Hauptlagerplatz“, Sage-Codes der Form `Lager;0;0;0`). Dort greift „Hauptlagerplatz zuerst“ nicht,
>    die Sortierung fällt auf Menge absteigend zurück – gewollt, kein Fehler; TS-95.9 an einem Artikel **mit**
>    Hauptlagerplatz prüfen.
> 5. **Mitbeheben (eine Zeile):** `warehouse-order` in `ColumnDefinitions.Bom` ergänzen (Zusatzfund Out-of-Scope).
>    `SupportsReorder` und der Kommentar `ColumnDefinitions.cs:239` nur, wenn trivial; sonst melden.
> 6. Umsetzung im Bündel-Worktree **nach** v1.61.0 (Arbeitsgang-Abgleich, deployt). Version: nächste freie (v1.62.0).

## Ziel / Nutzen (das Warum)

1. **Matchcode in der Stückliste.** Der Matchcode ist im Lager der gebräuchliche Kurzname. In den FA-Listen steht er
   seit v1.38/v1.40, in der Stückliste fehlt er (Gerald 2026-10-06, erneut 2026-10-16). Kommissionierer müssen
   heute für jede Position die Artikelinfo öffnen.
2. **Lagerplatz-Zelle begrenzen.** Bei Komponenten mit vielen Bestandsplätzen wird die Spalte „Lagerplatz“ so breit,
   dass die ganze Tabelle breiter als der Bildschirm wird. Die horizontale Scrollbar steht dann erst am Ende von z. B.
   263 Zeilen (Bug [[2026-10-16-stueckliste-lagerplatz-zelle-tabellenbreite-bug]]). Entscheidung Gerald 2026-10-16:
   Zelle begrenzen, Rest per „+n weitere“ mit Popover.

## Umfang (In-Scope / Out-of-Scope)

### Stand der Teile der Quell-Notiz

| Teil | Stand | Beleg |
|---|---|---|
| Zahnrad unsichtbar | **erledigt in v1.47.3, nicht Teil dieser Spec** | siehe unten „Zahnrad-Bestätigung“ |
| Matchcode-Spalte | **In-Scope** | `Bom.cshtml:151-184` hat kein `data-col-key="matchcode"` |
| Lagerplatz viele Plätze (Nachtrag 2026-10-16) | **In-Scope**, Entscheidung Gerald 2026-10-16 | `Bom.cshtml:267-276`, `site.css:250-253` |

**Zahnrad-Bestätigung (am Code, Bündel):** Die Badges `#bomDefaultFilterBadges` und der Leerzustand `#bomKzEmptyState`
stehen jetzt **über** dem Host (`Bom.cshtml:129-133`). Der leere Host steht an `:142`, direkt davor `.table-responsive`
an `:143`. `insertGearButton` hängt unverändert an `previousElementSibling` (`column-preferences.js:367-390`) und
trifft damit den Host. Wächter: `IdealAkeWms.Tests/Views/BomZahnradHostTests.cs:31-36`. Changelog
[[2026-10-06-v1-47-3-stueckliste-zahnrad-sichtbar]], [[fallstricke]] §19. Gerald hat das Zahnrad am 2026-10-16 auf dem
Testsystem gesehen. Deckt sich mit der Sammel-Spec [[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]], Tabelle
„Stand der sechs Quellen“.

### Varianten der Stückliste (alle über `Views/Picking/Bom.cshtml`)

| # | Variante | Standort | Einstieg | Datenquelle | Spalten |
|---|---|---|---|---|---|
| V1 | Kommissionier-Stückliste, flach | AKE | `PickingController.Bom` (`PickingController.cs:279-489`) | Weiche `BomRepositoryMasterSwitch.cs:44-48` → `CachedBomRepository.cs:19-34` (5 min MemoryCache) → `BomRepository.cs:23-60`: BOM-Cache, sonst Live-Sage `vw_AKE_Kommissionierung_StuecklistenDB` (`:33-52`), sonst OSEON-SP (`:54-57`), sonst `KEINE_DATEN` | Basis |
| V2 | Kommissionier-Stückliste, hierarchisch, Vollstruktur | IDEAL | wie V1; nur HauptFA, Sub-FA wird umgeleitet (`PickingController.cs:287-291`) | `FaHierarchyBomRepository.cs:39-62` `BuildFullStructure` (Scope `BomScope.cs:22-23`), `DataSource = FA-HIERARCHIE` | Basis + Komm.-Ziel, Hauptlagerplatz, Sage-Pos., Ebene, Vater-Sub-FA (`Bom.cshtml:161-171`) |
| V3 | Read-only-Stückliste FA-Abarbeitungsliste | beide | `FaWorklistController.Bom` (`:344-375`) → `ReadOnlyBomBuilder.BuildAsync` | wie V1 bzw. V2/V4 je Master | ohne Picking-Spalten (`ReadOnly`, `ReadOnlyBomBuilder.cs:146`) |
| V4 | Read-only, hierarchisch, Sub-FA | IDEAL | V3 oder V5 mit einem Sub-FA | `BuildDirectChildren` (`FaHierarchyBomRepository.cs:57-59`) | hierarchisch ohne Ebene/Vater-Sub-FA (`FullStructure` false) |
| V5 | Read-only-Stückliste FA-Vervollständigung | beide | `FaCompletionController.Bom` (`:395-417`) → `ReadOnlyBomBuilder` | wie V3 | wie V3 |
| D | Druck | beide | `PickingController.PrintBom` GET/POST (`:574-583`) → `RenderPrintBomAsync` (`:585-666`) → `Views/Picking/PrintBom.cshtml` | wie V1/V2 | sichtbare Spalten der Bildschirmansicht (`visibleColumns`) |

Ein „summiert“ gibt es in der Stückliste nicht. Summiert gibt es nur in den IDEAL-Kommissionierlisten
(`FaHierarchyKommissionierListen/Summiert`), und die sind nicht Teil dieser Spec. Alle Varianten V1–V5 rendern **dieselbe** View
(`FaWorklistController.cs:374`, `FaCompletionController.cs:416`). Deshalb reicht je Änderung eine Stelle in
`Bom.cshtml`. Datenseitig sind es zwei Stellen, `PickingController.Bom` und `ReadOnlyBomBuilder`, dazu der Druck.

### In-Scope

- **M** Matchcode-Spalte in V1–V5 und im Druck D, inklusive Spaltenauswahl, Spaltenfilter und CSV.
- **L** Lagerplatz-Zelle in V1–V5: höchstens 3 Plätze, dann der Badge „+n weitere“ mit Popover. Filter und CSV
  arbeiten auf der vollen Liste. Neue Reihenfolge der Plätze: Hauptlagerplatz, dann Menge absteigend.
- Die Lagerplatz-Spalte wird filterbar (Rückfrage 2, heute ist sie nicht filterbar).

### Out-of-Scope

- Zahnrad (erledigt, siehe oben). Zentraler Toolbar-Host: Sammel-Spec, umgesetzt **nach** dem Bündel-Merge.
- Matchcode in `PrintPicking` (`PickingController.cs:668-706`), `WarehousePicking/Details` und OSEON-Teileverfolgung.
  Diese bleiben Punkt 1 von [[2026-09-15-matchcode-nachlese]]. Diese Spec deckt aus Punkt 1 nur die Stückliste mit Druck ab.
- Lagerbestellung-Details (`WarehousePicking/Details.cshtml:102`) mit eigenem Datenweg, siehe Rückfrage 4.
- `site.css:250-253` (globales `nowrap`) bleibt unverändert, siehe Lösungsentwurf L.
- **Zusatzfunde, nicht mitbauen (nur melden):** `ColumnDefinitions.Bom` kennt `warehouse-order` nicht
  (`ColumnDefinitions.cs:244-267` gegen `Bom.cshtml:688`), und `SupportsReorder: false` (`ColumnDefinitions.cs:241`)
  widerspricht `"supportsReorder": true` (`Bom.cshtml:658`). Folgenlos, weil die C#-Liste nur der viewKey-Prüfung
  dient ([[fallstricke]] §10 „Neue Spalte … braucht ZWEI Registrierungen“). Der Kommentar `ColumnDefinitions.cs:239`
  („no data-col attribute“) ist ohnehin veraltet.

## Fachliche Anforderungen

**Standorte:** M und L wirken auf **AKE und IDEAL** gleich (eine View). Nur die Quelle des Matchcodes an IDEAL ist
eine offene Entscheidung (Rückfrage 1).

### M — Matchcode

1. Die Stückliste zeigt die Spalte „Matchcode“ **direkt nach „Bezeichnung 2“**, in allen Varianten V1–V5. Bei
   Benutzern mit gespeicherter Spaltenauswahl erscheint sie ebenfalls dort und sichtbar
   (`column-preferences.js:96-110` fügt neue Spalten hinter ihrem Konfig-Nachbarn ein).
2. Der Matchcode gehört zur **Komponente** der Zeile (`Ressourcenummer`), nicht zum Kopfartikel (`Artikelnummer`).
   Diese Verwechslung ist in [[fallstricke]] §10 („Kopf-Artikel vs. Zeilen-Artikel“) belegt.
3. Kein Matchcode (Artikel nicht im Artikelstamm oder Matchcode leer): Die Zelle bleibt **leer**, ohne Platzhalter
   und ohne Fehler. So halten es alle Matchcode-Zellen (z. B. `_PickingRow.cshtml:30`, `_ProductionOrderRow.cshtml:86`),
   und der Filter `!(leer)` bzw. `(leer)` greift auf den Zelltext ([[fallstricke]] §17, „kein Platzhalter“). Die Zahl der
   Fehltreffer protokolliert `MatchcodeLookup` einmal je Aufbau (`MatchcodeLookup.cs:39-43`). Das ist dieselbe Meldung wie
   in den FA-Listen seit v1.40.0, AK 14.
4. Die Spalte lässt sich über den Spaltenfilter filtern (Suchsyntax-Kern), in der Spaltenauswahl ein- und ausblenden
   und umsortieren. Sie geht in den CSV-Export und in den Druck, wenn sie sichtbar ist.

### L — Lagerplatz

5. Die Zelle zeigt **höchstens 3** Plätze als `Code (Menge)`. Sind es mehr, folgt ein Badge **„+n weitere“** (n =
   Anzahl der verdeckten Plätze).
6. Reihenfolge überall in der Stückliste: **Hauptlagerplatz zuerst, dann Menge absteigend, bei Gleichstand Code**.
   Das gilt für sichtbare Plätze, Popover, Filtertext, CSV und Druck.
7. Der Badge öffnet ein Popover mit der **vollen** Liste, ein Platz je Zeile, der Hauptlagerplatz als Text markiert.
   Bedienung:
   - **Maus:** Hover zeigt das Popover, Wegbewegen schließt es. Ein Klick lässt es offen, bis woanders hingeklickt wird.
   - **Touch:** Tippen öffnet, Tippen außerhalb schließt.
   - **Tastatur:** Tab auf den Badge öffnet, Tab weiter oder Escape schließt.
8. Der Spaltenfilter „Lagerplatz“ trifft **jeden** Platz der Zeile, auch einen verdeckten. Die Zeile erscheint dann,
   der Treffer steht gegebenenfalls nur im Popover.
9. Der CSV-Export schreibt in die Spalte „Lagerplatz“ die **volle** Liste im heutigen Format `Code (Menge), …`, ohne
   „+n weitere“.
10. Gibt es keinen Bestand, bleibt es wie bisher bei „-“ (`Bom.cshtml:272-275`). Welche Plätze erscheinen, ändert sich nicht:
    alle Plätze mit Saldo ≠ 0 ohne Kommissionierwagen (`StockMovementRepository.cs:395`, `:425`, `:464`). Negative
    Salden bleiben sichtbar und landen durch „Menge absteigend“ hinten.
11. Druck: siehe Rückfrage 3. Die Empfehlung ist die volle Liste.

## Technischer Loesungsentwurf

Muster: Listen-View-Pattern in der **Client-Mode-Ausnahme** der Stückliste (ADR
[[0005-listen-view-pattern-mit-server-side-spaltenfilter]], [[fallstricke]] §14: unpaginiert, durch den Baum
vorgefiltert). Daten nur über Repository-Interfaces (ADR [[0001-repository-pattern-mit-decorator-fuer-caching]]).
**Frontend-Lauf: Skill `frontend-design` explizit aufrufen** (CLAUDE.md), Bootstrap-Bordmittel vor eigenem Stil.

### M — Matchcode

**Quelle (Empfehlung A, Rückfrage 1): Artikelstamm `Article.Matchcode` über den vorhandenen
`Services/MatchcodeLookup.cs`.** Muster `PickingController.cs:150-167`, am Code verifiziert: ein Batch-Lookup über
die Artikelnummern **vor** der Projektion, je Zeile `matchcodes.GetValueOrDefault(nr ?? "")`. Dasselbe Muster nutzen
`ProductionOrdersController.cs:171`, `PickingLeitstandController.cs:127`, `FaWorklistController.cs:236` und
`FaCompletionController.cs:140`.

Warum der Artikelstamm (Belege, Schreib- und Lesewege):

- **AKE hat keine andere Quelle.** `BomItem` hat kein Matchcode-Feld (`BomViewModels.cs:16-27`). Die AKE-View-Abfrage
  selektiert keinen Matchcode (`BomRepository.cs:33-45`), die OSEON-SP auch nicht (`:78-89`). `BomItem` darf **keine**
  neue Property bekommen, sonst bricht `SqlQueryRaw<BomItem>` ([[fallstricke]] §10).
- **Ein Codeweg für beide Standorte** und dieselbe Quelle wie die fünf FA-Listen ([[fallstricke]] §12). Das ist auch
  die Richtung der Nachlese (Punkt 2: Baum auf den Artikelstamm umstellen).
- Mengen und Kosten: eine Abfrage `GetMatchcodesByArticleNumbersAsync` (`ArticleRepository.cs:81-89`). Die
  `Contains`-Liste übersetzt EF 10 zu einem `OPENJSON`-Parameter ([[fallstricke]] §8, gemessen), unkritisch auch bei
  263 Positionen.

**Kann `FaHierarchyNode.Matchcode` vom Artikelstamm abweichen? Ja, das ist am Code möglich, aber nicht gemessen:**

| | `FaHierarchyNode.Matchcode` (Struktur) | `Article.Matchcode` (Artikelstamm) |
|---|---|---|
| Schreibwege | nur `FaHierarchySyncService.cs:238` (Service) aus der Sage-View `vw_IDEAL-AKE_Kommissionierung_FAListe` (`FaHierarchySql.cs:141`) | (1) `SageImportService.cs:461-498` (Service, beide Standorte): Legacy-Select `KHKArtikel.Matchcode` (`SageArticleSql.cs:20`, `:36`) oder, bei gesetztem `Sync:ArticlesViewName`, die View `vw_IDEAL_AKE_WMS_Artikel` (`SageArticleSql.cs:61-73`, `SageImportService.cs:384-386`); (2) **Handpflege im WMS** `ArticlesController.cs:182` (Artikel bearbeiten) |
| Herkunft laut Doku | „Position bevorzugt, KHKArtikel als Fallback“ (`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql:29`). Der echte View-Text liegt **nicht** im Repo (`:57` TODO) | Artikelstamm bzw. Artikel-View; deren Matchcode-Definition liegt in Sage |
| Leser | Kommissionierlisten (`KommissionierListenService.cs:60`, `:184`), Vormontage (`VormontageService.cs:85`, `:247`), Beschichtung (`BeschichtungsauftragService.cs:77`, `BeschichtungsauftragWorkflow.cs:216-224`), FA-Struktur-Baum (`_FaHierarchyNode.cshtml:93`) | FA-Listen über `MatchcodeLookup`, Artikelliste/-info, Fehlteile/Bestand-Suche |

Abweichungsquellen:

- **(a) Positions-Matchcode in Sage.** Liegt an der Position ein eigener Matchcode vor, gewinnt dieser in der
  Struktur-View. Der Code rechnet damit: `VormontageService.cs:214-215` „gleicher Artnr mit verschiedenen Matchcodes
  ergibt bewusst mehrere Zeilen“. Ob es solche Positionen gibt, ist **unbelegt**, siehe P1/P2.
- **(b) Handpflege im WMS** (`ArticlesController.cs:182`). Der nächste Artikel-Sync überschreibt den Wert nur, wenn
  Sage den Artikel liefert und der Wert abweicht (`SageImportService.cs:471`, `:482`). Für Artikel außerhalb der
  Sage-Menge bleibt die Handpflege dauerhaft.
- **(c) Zeitversatz** zwischen den beiden Syncs.
- **(d) An IDEAL ist die Artikelquelle umschaltbar** (`Sync:ArticlesViewName`). Wie die Artikel-View den Matchcode
  bildet, steht nur in Sage.

**Folge an IDEAL bei Variante A:** Für dieselbe Position kann die Stückliste einen anderen Matchcode zeigen als
Kommissionierliste, Vormontage oder Beschichtung, die den Strukturknoten lesen. Das ist die gleiche bewusste
Inkonsistenz wie in [[fallstricke]] §12 (Baum gegen Listen). Außerdem fehlen Matchcodes für Komponenten, die nicht in
`Articles` stehen ([[fallstricke]] §12 „gefilterte Projektion“).

**Variante B (Alternative):** An IDEAL zuerst `FaHierarchyNode.Matchcode`, sonst der Artikelstamm. Aufwand: eine
Property `FaHierarchyBomItem.Matchcode` (`FaHierarchyBomItem.cs:10-29`, die Ableitung ist dafür vorgesehen, §10), eine
Zeile im Mapping `FaHierarchyBomRepository.cs:174-193` und `(bom as FaHierarchyBomItem)?.Matchcode ?? lookup` an den
drei Projektionsstellen. Vorteil: an IDEAL gleich wie die Kommissionierliste. Nachteil: zwei Quellen in einer Spalte,
und Stückliste und FA-Listen können sich bei (a) unterscheiden.

**Empfehlung: A.** Zeigt P1 Abweichungen > 0, dann B. Die Entscheidung trifft Gerald (Rückfrage 1). Prüfabfragen
**vor** der Freigabe:

```sql
-- P1 (WMS-DB IDEAL_AKE_WMS, IDEAL-Test): Struktur-Matchcode gegen Artikelstamm, Abdeckung des Artikelstamms
SELECT COUNT(*) AS Positionen,
       SUM(CASE WHEN a.Id IS NULL THEN 1 ELSE 0 END) AS OhneArtikelstamm,
       SUM(CASE WHEN a.Id IS NOT NULL
                 AND ISNULL(LTRIM(RTRIM(n.Matchcode)),'') <> ISNULL(LTRIM(RTRIM(a.Matchcode)),'')
                THEN 1 ELSE 0 END) AS MatchcodeAbweichend
FROM dbo.FaHierarchyNodes n
LEFT JOIN dbo.Articles a ON a.ArticleNumber = n.Artnr
WHERE n.Artnr IS NOT NULL AND n.Artnr <> '';

-- P1b: Beispiele der Abweichungen
SELECT TOP 50 n.HauptFA, n.Artnr, n.Matchcode AS StrukturMatchcode, a.Matchcode AS ArtikelMatchcode
FROM dbo.FaHierarchyNodes n
JOIN dbo.Articles a ON a.ArticleNumber = n.Artnr
WHERE ISNULL(LTRIM(RTRIM(n.Matchcode)),'') <> ISNULL(LTRIM(RTRIM(a.Matchcode)),'');

-- P2 (Sage-DB IDEAL): Hat die Position einen eigenen Matchcode, und wie bildet die View ihn?
EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAListe';
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'KHKPpsFaBelegePositionen' AND COLUMN_NAME LIKE '%atchcode%';
```

Grenze von P1: Die WMS-DB vergleicht case-insensitiv (Kollation). Reine Groß-/Kleinschreibungs-Unterschiede zählt
die Abfrage deshalb nicht, für die Anzeige sind sie unerheblich.

**Umsetzung (A):**

1. `BomItemViewModel.Matchcode` (`BomViewModels.cs:37-70`) und `PrintBomItem.Matchcode` (`:151-168`), beide `string?`.
2. `PickingController.Bom`: nach `articleNumbers` (`PickingController.cs:335`)
   `var matchcodes = await MatchcodeLookup.BuildAsync(_articleRepository, articleNumbers, _logger, "Stueckliste");`
   `_articleRepository` (`:62`) und `_logger` (`:69`) sind vorhanden. In der Projektion (`:407-436`)
   `Matchcode = matchcodes.GetValueOrDefault(bom.Ressourcenummer ?? "")`.
3. `RenderPrintBomAsync` (`:598-641`): derselbe Lookup über dieselbe `articleNumbers`-Liste, `Matchcode` im
   `PrintBomItem`.
4. `ReadOnlyBomBuilder` (`ReadOnlyBomBuilder.cs:23-35`): Konstruktor um `IArticleRepository` und
   `ILogger<ReadOnlyBomBuilder>` erweitern. Beide sind im DI vorhanden, der Builder ist scoped (`Program.cs:161`).
   Lookup nach `:73`, Zuweisung in `:91-115`. Die drei Testkonstruktionen ziehen mit:
   `FaCompletionControllerTests.cs:55`, `FaWorklistControllerTests.cs:73`, `ReadOnlyBomBuilderTests.cs:33`.
5. `Bom.cshtml`:
   - `<th data-filterable data-col-key="matchcode">Matchcode</th>` nach `:155`
   - `<td>@item.Matchcode</td>` nach `:251`
   - `{ "key": "matchcode", "label": "Matchcode", "locked": false, "defaultWidth": null },` nach `:667`, gleicher
     Platz und ohne Gate
   - `emptyColspan` Basis `12`/`10` → `13`/`11` (`:359-367`), Kommentar mitziehen
   - Druck-`colNames` (`:1258-1264`) um `'matchcode': 'Matchcode'` ergänzen ([[fallstricke]] §14 „col-key-Schlüssel“)
6. `ColumnDefinitions.Bom`: `new ColumnDef("matchcode", "Matchcode", Locked: false)` nach `:251`. Das ist die zweite
   Pflicht-Registrierung ([[fallstricke]] §3, §10).
7. `PrintBom.cshtml`: `ShowCol("matchcode")` in Zählung (`:196-210`), Kopf (`:216-228`) und Zelle (`:250-260`), je nach
   `description2`.
8. **Filtertyp:** Text. Kein Eintrag in `ColumnFilterTypes.Lists` nötig, `"Picking/Bom"` kennt nur `quantity`
   (`ColumnFilterSyntax.cs:58`). **Prüfschritt P-2 (Kollation je Spalte) entfällt:** Er gilt für SQL-Filter
   ([[fallstricke]] §26). Die Stückliste filtert im Browser über `ColumnFilterSyntax.matches` auf dem Zelltext
   (`Bom.cshtml:848-851`, `table-filter.js:324`), case-insensitiv durch den Kern.
9. CSV: Der neue `<th>` hat `data-col-key`, also läuft `CsvExportCoverageTests` ([[fallstricke]] §23) durch. Den Wert
   liefert der Zelltext (`csv-export.js:93`).

### L — Lagerplatz-Zelle

**Woher Hauptlagerplatz und Menge kommen (belegt):** `StockLocationInfo` (`BomViewModels.cs:29-35`) entsteht in
`StockMovementRepository.GetStockByArticleNumbersAsync` (`:385-473`):

- `Quantity` ist der Saldo je Artikel und Platz aus den Lagerbewegungen: Einbuchung + SageEinbuchung + Umbuchung-Ziel
  − Ausbuchung − SageAusbuchung − Umbuchung-Quelle, ohne Kommissionierwagen (`:391-439`), nur ≠ 0 (`:464`).
- `IsPrimaryStorageLocation` ist wahr, wenn der Platz `Article.PrimaryStorageLocationId` ist (`:442-447`, `:459-461`).
  Das ist der **WMS-Hauptlagerplatz** am Artikel: aus Sage `KHKLagerplaetze.Kurzbezeichnung` über den Artikel-Sync
  (`SageImportService.cs:500-528`), sonst von Hand gepflegt ([[fallstricke]] §7 „Lock-Semantik“).
- **Nicht verwechseln** mit der IDEAL-Spalte „Hauptlagerplatz“ (`Bom.cshtml:164`, `:259`). Diese kommt aus der
  Struktur-View (`FaHierarchyBomRepository.cs:187`, `vw_…FAListe.sql:36`) und ist ein Text, kein Lagerplatz-FK. Die
  Sortierung nutzt **nur** `IsPrimaryStorageLocation`.

**Sortierung (ponytail: einmal, zentral):** In `StockMovementRepository.cs:469-470`
`.OrderBy(i => i.IsPrimaryStorageLocation ? 0 : 1).ThenByDescending(i => i.Quantity).ThenBy(i => i.Code)`.
Die Regel „Hauptlagerplatz zuerst“ liegt laut [[fallstricke]] §7 bereits zentral dort. Dieselbe Regel steht schon
wörtlich in `MissingPartsLagerController.cs:60-63`. Wen das betrifft (alle Aufrufer geprüft):

| Aufrufer | Wirkung |
|---|---|
| `PickingController.Bom` (`:336`), `ReadOnlyBomBuilder` (`:74`), `RenderPrintBomAsync` (`:604`) | gewollt: neue Reihenfolge auf Bildschirm und Druck |
| `PickingController.Bom` Auto-Vorschlag (`:379-392`) | keine, sortiert selbst (`FirstOrDefault(IsPrimary)`, `OrderByDescending(Quantity)`) |
| `PickingApiController.SearchSourceLocations` (`:38-66`) | keine, sortiert selbst |
| `MissingPartsLagerController` (`:53-63`) | keine, sortiert selbst |

Test `StockMovementRepositoryTests.cs:224-244` bleibt grün (Primär zuerst). Neuer Fall: drei Plätze ohne Primär mit
Mengen 1/5/3 ergeben die Reihenfolge 5, 3, 1, bei Gleichstand nach Code.

**Zelle (Razor, `Bom.cshtml:267-276`):**

- Konstante `MaxSichtbareLagerplaetze = 3`, im View-Kopf oder als `const` am `BomItemViewModel`.
  `ponytail:`-Kommentar: Konstante, AppSetting erst bei belegtem Bedarf.
- Sichtbar ist `StockLocations.Take(3)` im heutigen Format. Bei mehr Plätzen folgt
  `<a tabindex="0" role="button" class="badge … bom-lagerplatz-mehr" data-bs-toggle="popover" aria-label="Alle {n} Lagerplätze anzeigen">+{n−3} weitere</a>`.
  Bootstrap verlangt für „schließen beim nächsten Klick“ ein `<a>` mit `tabindex` plattformübergreifend, ein `<button>`
  reicht dort nicht (iOS fokussiert Buttons beim Tippen nicht).
- **Volltext für Filter und Sortierung:** In derselben Zelle steht die volle Liste in einem `d-none`-Element, z. B. ein
  `<ul class="d-none bom-lagerplatz-alle">` mit einem `<li>` je Platz und dem Text „Hauptlagerplatz“ am Primärplatz.
  Begründung: Filter (`table-filter.js:324`, `Bom.cshtml:899`), Sortierung (`table-filter.js:386`) und die
  Leerzustand-/Aufklapp-Prüfungen (`Bom.cshtml:1142`, `:1166`) lesen alle `cell.textContent`. `textContent` enthält
  auch versteckte Elemente. So trifft der Filter ohne eine einzige Änderung am gemeinsamen Filter-JS.
  - Verworfene Alternative: ein neues `data-filter-value` im Kern. Das wären mindestens 4 Lesestellen in zwei Dateien,
    dazu Wirkung auf alle Listen. Ponytail-Leiter Stufe 2: vorhandenen Mechanismus nutzen.
  - Bekannte Grenze: Der Badge-Text „+n weitere“ ist ebenfalls Zelltext. Ein Filter auf „weitere“ trifft also Zeilen
    mit mehr als 3 Plätzen. Das ist folgenlos und wird nicht behandelt.
- **CSV:** `data-csv-value` am `<td>` mit der vollen Liste im heutigen Format (`Code (Menge), …`). `csv-export.js:80`
  liest das Attribut vor dem Zelltext. Ohne Bestand fehlt das Attribut, CSV schreibt wie heute „-“.
- `<th data-col-key="storage-location">` bekommt `data-filterable` (Rückfrage 2). `ColumnDefinitions.cs:262` „Not
  filterable“ wird korrigiert, und `colNames` (`Bom.cshtml:1258-1264`) bekommt `'storage-location': 'Lagerplatz'`.

**Popover (wiederverwenden):** Im Code gibt es genau eine Popover-Verwendung: `column-filter-syntax.js:220-223`
(`new bootstrap.Popover(…, { trigger: 'click', … })` + Escape schließt). Es gibt **keine** globale
Popover-Initialisierung (Grep `data-bs-toggle="popover"` in Views: kein Treffer), und kein „+n weitere“-Muster
(Grep „weitere“ in Views: nur Fließtext). Bootstrap Popover samt Popper ist über `bootstrap.bundle.min.js` überall
geladen (`_Layout.cshtml:413`). Deshalb im Seitenskript von `Bom.cshtml`, **vor** `if (readOnly) return;` (`:1430`),
damit Abarbeitungsliste und FA-Vervollständigung es auch bekommen:

- je `#bomTable .bom-lagerplatz-mehr` ein `new bootstrap.Popover(el, { trigger: 'hover focus', container: 'body',
  html: true, placement: 'auto', title: 'Lagerplätze (n)', content: <Inhalt der .bom-lagerplatz-alle derselben Zelle>,
  customClass: 'bom-lagerplatz-popover' })`. Den Inhalt per Element übergeben oder den Standard-`sanitize` lassen. Ein
  `sanitize: false` wie in `column-filter-syntax.js:222` ist hier **nicht** nötig, die Liste ist Razor-kodierter
  Servertext.
- `container: 'body'`: `.table-responsive` beschneidet sonst das Popover. Dieselbe Begründung steht bei Select2
  (`Bom.cshtml:719` „verhindert Clipping in <td>“).
- `trigger: 'hover focus'`: Hover deckt die Maus ab, Fokus deckt Tastatur und Tippen ab (Tippen fokussiert das `<a tabindex>`).
  Tippen oder Klicken außerhalb nimmt den Fokus und schließt. Escape schließt alle offenen Popover (Muster
  `column-filter-syntax.js:223`). Wird eine Zeile weggeklappt, wandert der Fokus nicht automatisch. Das ist akzeptiert,
  Escape oder ein Klick daneben schließen.
- `site.css`: Klasse `.bom-lagerplatz-popover .popover-body { max-height: 50vh; overflow-y: auto; }`. Lange Listen
  scrollen im Popover, nicht auf der Seite. Der Popover hängt am `body`, `site.css:250-253` (nur `.table-responsive`)
  greift dort also nicht.

**Barrierefreiheit (Pflicht, nicht kürzen):**

- Der Badge-Text „+n weitere“ trägt die Bedeutung, nicht die Farbe. `aria-label` nennt die Gesamtzahl. Bootstrap setzt
  beim Öffnen `aria-describedby`, damit liest ein Screenreader die Liste vor. Das versteckte `d-none`-Element liest er
  nicht doppelt.
- Kontrast WCAG AA messen und im QA-Nachweis festhalten. Badge-Text gegen Badge-Grund mindestens 4,5:1, Fokusrahmen
  gegen Zeilengrund mindestens 3:1 in normaler, gestreifter, `table-success`- und `table-secondary`-Zeile. Vorschlag
  `text-bg-secondary` (Weiß auf `#6c757d` ≈ 4,7:1). Den Projektwert `#495057` aus `site.css:1387-1391` nur nehmen,
  wenn die Messung es verlangt.
- Trefferfläche mindestens 24 × 24 CSS-px (WCAG 2.2, 2.5.8), z. B. per Padding in derselben Klasse. Die Terminals in
  der Fertigung werden mit Handschuh bedient.
- Fokus sichtbar, kein `outline: none`.

**`site.css:250-253` (`nowrap !important`) einordnen:** Die Regel bleibt. Sie gilt für alle Tabellen in
`.table-responsive`. Sie zu lockern hätte Folgen für alle Listen (laut Sammel-Spec 42 Views mit `filterable-table`),
und mit dem Limit wird sie für diese Zelle
unschädlich: höchstens 3 × `Code (Menge)` + Badge haben eine begrenzte Breite. Der Umbruch-Vorschlag der Koordination
ist durch Geralds Entscheidung **überholt**.

**Wechselwirkung mit der Sammel-Spec [[2026-10-14-spaltenauswahl-zahnrad-sammel-spec]]:**

- Die Sammel-Spec ist freigegeben, wird aber erst **nach** dem Bündel-Merge auf einem neuen Zweig ab `main`
  umgesetzt (Freigabe-Nachtrag dort). Diese Spec läuft **vorher im Bündel**. Die Reihenfolge ist damit fest: erst
  diese Spec, dann die Sammel-Spec.
- Berührungspunkt `Bom.cshtml`: Die Sammel-Spec ändert nur den Host `:135-142` (Marker `data-table-toolbar`,
  Kommentar), ersetzt `BomZahnradHostTests` und fasst `column-preferences.js` an. Diese Spec ändert `thead`/`tbody`,
  `#column-config` und das Seitenskript. **Zwischen Host und `.table-responsive` kommt nichts hinzu**, das Popover hängt
  am `body`. Folge für die Sammel-Spec: nur verschobene Zeilennummern. Das deckt ihr „Zeilennummern gegen `main`
  nachmessen“ bereits ab.
- Paket B der Sammel-Spec (`colspan`-Zeilen überspringen) betrifft die Leerzustandszeile der Stückliste
  (`Bom.cshtml:368-370`). Deren `colspan` wächst hier um 1. Konflikt: keiner.
- Die Sammel-Spec plant ihr Testkapitel als „TS-93“. Im Bündel ist TS-93 inzwischen vergeben (Sage-Views). Ihr Dev-Lauf
  misst die Nummer ohnehin neu. Nur ein Hinweis, keine Änderung hier.
- Die Sammel-Spec hat Matchcode an [[2026-09-15-matchcode-nachlese]] Punkt 1 verwiesen (Rückfrage 3 dort). **Diese Spec
  ist diese Ausgliederung** für den Stücklisten-Teil von Punkt 1.

## Migrations-/SQL-Auswirkungen

**Keine Migration, kein `SQL/XX`-Skript, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt.** Belege:

- `Article.Matchcode` existiert seit Migration `AddArticleMatchcode` (`Migrations/20260915094646_AddArticleMatchcode.cs`,
  `SQL/91_AddArticleMatchcode.sql`).
- Neue Properties nur an ViewModels: `BomItemViewModel` und `PrintBomItem` (`Models/ViewModels/BomViewModels.cs`).
  Das sind keine Entities im `ApplicationDbContext`.
- Bei Variante B kommt die Property an `FaHierarchyBomItem`. Das ist kein Entity, sondern eine Ableitung von `BomItem`,
  und sie wird nur von `FaHierarchyBomRepository` befüllt (`:174-193`). Die AKE-Abfrage `SqlQueryRaw<BomItem>` bleibt
  unberührt ([[fallstricke]] §10).
- Die Sortierung ist LINQ in-memory nach `ToListAsync` (`StockMovementRepository.cs:449-472`), ohne Schemabezug.
- Der Dev-Lauf bestätigt das mit `dotnet ef migrations has-pending-model-changes` (leer).

## Audit-Feld-Auswirkungen

Keine. Es entsteht kein Schreibweg. `PickingController.Bom` schreibt wie bisher nur in
`InitializePickingAsync` (`:322-325`, unverändert). Lookup und Sortierung lesen nur. Die Handpflege des Matchcodes
(`ArticlesController.cs:176-194`) bleibt unverändert und setzt `ModifiedAt` usw. wie bisher.

## Rollen / Zugriffsfilter

Keine neue Rolle, kein neuer Filter, Gates unverändert:

- `PickingController.Bom`: `[RequirePickingAccess]` (`:279`)
- `FaWorklistController` Klasse: `[RequireVorbauAccess]` (`:20`), dazu Gate `FaCompletionAktiv` (`:347-352`)
- `FaCompletionController` Klasse: `[RequireFaCompletionAccess]` (`:20`)
- `PrintBom`: `[RequirePickingOrVorbauOrFaCompletionAccess]` (`:575`, `:580`)

Matchcode und Bestandsplätze sehen dieselben Rollen schon heute: den Matchcode in den FA-Listen, die Plätze in der
Zelle. Es werden keine neuen Daten freigelegt. `controller.md` und `RoleOverview` bleiben unverändert.

**Listen-View-Pattern:** keine neue Tabellen-View. Die Stückliste bleibt die dokumentierte Client-Mode-Ausnahme
(unpaginiert, [[fallstricke]] §14). Die Pflichten für die neue bzw. neu filterbare Spalte: `data-col-key`, Eintrag in
`#column-config` **und** `ColumnDefinitions.Bom` (zwei Registrierungen, §10), Spaltenfilter über den Kern, CSV-fähig.
Pagination entfällt, wie bisher.

## Akzeptanzkriterien

**M — Matchcode**

1. In V1–V5 steht die Spalte „Matchcode“ direkt rechts von „Bezeichnung 2“, sichtbar per Standard. Auch Benutzer mit
   gespeicherter Spaltenauswahl sehen sie dort nach dem Deploy.
2. Der angezeigte Matchcode ist der des Artikels in der Spalte „Ressourcenummer“ derselben Zeile, nicht der des
   Kopfartikels. Ein Unit-Test mit Kopf ≠ Komponente sichert das ab.
3. Ein Artikel ohne Artikelstamm-Eintrag oder mit leerem Matchcode ergibt eine leere Zelle. Keine Exception, kein
   Platzhalter. Das Log zeigt eine Zeile „Matchcode-Join: x von y … (Stueckliste)“ je Aufbau.
4. Spaltenfilter „Matchcode“: ein Teilstring findet die Zeile, `(leer)` findet leere Zellen, `!(leer)` blendet sie
   aus, Groß-/Kleinschreibung ist egal.
5. Das Zahnrad listet „Matchcode“. Ausblenden, Umordnen und Neuladen behalten den Zustand. Die Prefs-API antwortet
   nicht mit 400.
6. Der CSV-Export enthält die Spalte „Matchcode“, wenn sie sichtbar ist, und lässt sie weg, wenn sie ausgeblendet ist.
7. Der Druck (`PrintBom`, POST aus der Stückliste) zeigt die Spalte „Matchcode“, wenn sie am Bildschirm sichtbar ist.
   Der GET-Druck ohne `visibleColumns` zeigt sie immer. Der Filterhinweis im Druckkopf nennt „Matchcode“ als Klartext.
8. An IDEAL entspricht die Quelle der Antwort auf Rückfrage 1: bei A der Artikelstamm, bei B der Strukturknoten mit
   Artikelstamm als Fallback.

**L — Lagerplatz**

9. Eine Zeile mit ≤ 3 Bestandsplätzen sieht wie bisher aus (alle Plätze, kein Badge). Nur die Reihenfolge folgt AK 11.
10. Eine Zeile mit > 3 Plätzen zeigt genau 3 Plätze und den Badge „+n weitere“ mit n = Anzahl − 3.
11. Reihenfolge: Hauptlagerplatz (sofern Bestand) zuerst, dann Menge absteigend, bei Gleichstand Code aufsteigend. Ein
    Repository-Test sichert das ab. Druck und CSV haben dieselbe Reihenfolge.
12. Das Popover zeigt alle Plätze, einen je Zeile, den Hauptlagerplatz mit dem Text „Hauptlagerplatz“. Es öffnet per
    Hover und schließt beim Wegbewegen. Es öffnet per Klick und bleibt bis zum Klick daneben. Es öffnet per Tippen auf
    Tablet/Handy und schließt beim Tippen daneben. Es öffnet per Tab-Fokus und schließt mit Escape oder Tab weiter.
    Es wird nicht vom Tabellenrand abgeschnitten.
13. Lange Listen scrollen im Popover, die Seite wächst nicht.
14. Die Spalte „Lagerplatz“ hat ein Filterfeld (nach Bestätigung Rückfrage 2). Ein Filter auf einen **verdeckten**
    Platz (Position > 3) zeigt die Zeile an.
15. CSV-Spalte „Lagerplatz“ = volle Liste `Code (Menge), …` ohne „+n weitere“.
16. Ohne Bestand steht weiter „-“. Kommissionierwagen erscheinen weiter nicht.
17. Badge-Kontrast ≥ 4,5:1 und Fokusrahmen ≥ 3:1 gemessen, auf allen vier Zeilengründen. Trefferfläche ≥ 24 × 24 px.
    Bedeutung über Text.
18. Bei FA 1059463 (IDEAL-Test, 263 Positionen) ist die Tabelle ohne den Lagerplatz-Effekt nicht breiter als nötig. Die
    Spalte „Lagerplatz“ ist höchstens so breit wie 3 Plätze + Badge.

**Allgemein**

19. Zahnrad weiter sichtbar, auch bei Leerzustand und Standardfilter-Badges (Regression TS-79.20). `BomZahnradHostTests`
    bleibt grün.
20. Bestandsverhalten unverändert: Baum auf/zu, rekursive Suche, Standardfilter-Badges, Komm.-Ziel-Dropdown,
    Picking-Checkboxen, Quell-Lagerplatz-Select2, Lagerbestellung/Bedarfsmeldung, Druck mit sichtbaren Positionen.
21. Build und alle Tests (Web + Service) grün. Version in beiden `AppVersion.cs` (Nummer bei der Umsetzung festlegen),
    Anwender-Changelog `Views/Help/Changelog.cshtml`, Hilfeseite gepflegt.

## Test-Szenarien

Neues Kapitel **TS-95 — Stückliste: Matchcode und Lagerplatz-Limit** in `docs/TESTSZENARIEN.md` des Bündels.
Gemessen: letztes Kapitel ist TS-94 (`docs/TESTSZENARIEN.md:10041`), kein Spec im Brain reserviert TS-95 (Grep
`TS-9[5-9]` in `secondbrain/`: kein Treffer). Danach `secondbrain/tests/testszenarien-index.md` nachziehen. CRLF-Prüfung
vor dem Commit beachten ([[fallstricke]] §21).
Regressionen: Kapitel 5 Stückliste (`:1901`), TS-70 (BOM-Bridge), TS-79.20 (Zahnrad), TS-86 (CSV), TS-91 (Suchsyntax).

**Vorbedingungen allgemein:** Testsysteme IDEAL (`ProduktionsauftragHierarchisch` = an) und AKE (aus). Benutzer mit
Rolle picking, einer mit vorbau bei `FaCompletionAktiv` = an, einer mit fa_completion. Ein Benutzer **mit** und einer
**ohne** gespeicherte Spaltenauswahl „Bom“. Edge am PC und ein Android- oder iOS-Tablet. Eine Komponente mit > 3
Bestandsplätzen. Finden auf der WMS-DB (Näherung über Bewegungen, Saldo ≠ 0 im Bildschirm prüfen):

```sql
SELECT TOP 20 a.ArticleNumber, COUNT(DISTINCT sm.StorageLocationId) AS Plaetze
FROM dbo.StockMovements sm JOIN dbo.Articles a ON a.Id = sm.ArticleId
GROUP BY a.ArticleNumber HAVING COUNT(DISTINCT sm.StorageLocationId) > 3
ORDER BY Plaetze DESC;
```

Diese Komponente muss in der geprüften Stückliste vorkommen. Notfalls auf dem Testsystem per Einbuchung auf 5
Plätze verteilen.

- **95.1 Matchcode sichtbar (IDEAL).** FA 1059463 → Stückliste (`Picking/Bom/131`). *Erwartet:* Spalte „Matchcode“
  rechts von „Bezeichnung 2“. Stichprobe 3 Zeilen: der Wert entspricht Artikelinfo → Matchcode der Ressourcenummer.
  *Negativ:* Der Wert ist der Matchcode des Kopfartikels (Titelzeile) → Fehler.
- **95.2 Matchcode AKE.** Stückliste eines AKE-FA (Quelle-Badge SAGE, wenn vorhanden auch OSEON). *Erwartet:* wie 95.1.
- **95.3 Read-only-Varianten.** Abarbeitungsliste → Stückliste, FA-Vervollständigung → Stückliste. An IDEAL zusätzlich
  einen Sub-FA (V4). *Erwartet:* Matchcode-Spalte da, Popover funktioniert, keine Picking-Spalten.
- **95.4 Fehlender Matchcode.** Komponente ohne Artikelstamm-Eintrag oder mit leerem Matchcode (P1 liefert Kandidaten).
  *Erwartet:* leere Zelle, Seite lädt. Filter `(leer)` zeigt sie, `!(leer)` blendet sie aus. Im Log steht die Zeile
  „Matchcode-Join … (Stueckliste)“.
- **95.5 Matchcode filtern.** Teil eines Matchcodes in Klein- statt Großschrift eingeben. *Erwartet:* Treffer.
  Mehrfachwert `a, b` und Ausschluss `!x` wie in TS-91.
- **95.6 Spaltenauswahl.** Benutzer **mit** gespeicherter Auswahl öffnet die Stückliste. *Erwartet:* Matchcode sichtbar
  hinter „Bezeichnung 2“. Über das Zahnrad ausblenden, F5: bleibt ausgeblendet. Nach vorne ziehen, F5: bleibt.
  DevTools → Netzwerk: PUT `/api/user-view-preferences/Bom` mit 200/204, nicht 400.
- **95.7 CSV.** CSV mit sichtbarem Matchcode. *Erwartet:* Spalte vorhanden. Lagerplatz-Spalte mit **voller** Liste bei
  der Komponente mit > 3 Plätzen, ohne „weitere“. Matchcode ausblenden → CSV ohne die Spalte.
- **95.8 Druck.** Mit sichtbarem Matchcode drucken. *Erwartet:* Spalte im Druck, Lagerplatz laut Antwort 3 (Empfehlung
  volle Liste, umgebrochen). Filter auf Matchcode setzen und drucken: der Kopf zeigt „Matchcode=…“ als Klartext.
- **95.9 Limit.** Zeile der Komponente mit > 3 Plätzen. *Erwartet:* 3 Plätze, Badge „+n weitere“ mit richtigem n. Der
  erste Platz ist der Hauptlagerplatz, sofern dort Bestand liegt (Artikel bearbeiten → Hauptlagerplatz prüfen). Danach
  absteigende Menge. Zeile mit ≤ 3 Plätzen ohne Badge.
- **95.10 Popover Maus.** Hover über den Badge: das Popover zeigt alle Plätze, den Hauptlagerplatz markiert. Wegbewegen
  schließt. Klick: bleibt offen, Klick daneben schließt. Badge in der letzten Tabellenspalte und in der letzten Zeile:
  das Popover wird nicht abgeschnitten.
- **95.11 Popover Touch.** Tablet: auf den Badge tippen öffnet, daneben tippen schließt. Zweimal hintereinander auf
  verschiedene Badges tippen: nur ein Popover offen. *Negativ:* Popover lässt sich nicht schließen oder öffnet nicht →
  Fehler.
- **95.12 Popover Tastatur.** Mit Tab zum Badge: öffnet. Escape schließt. Tab weiter schließt. Mit Screenreader (NVDA
  oder Sprachausgabe) wird „Alle n Lagerplätze anzeigen“ und die Liste angesagt.
- **95.13 Filtern nach verdecktem Platz.** Code eines Platzes an Position ≥ 4 der Liste in den Lagerplatz-Filter.
  *Erwartet:* die Zeile ist sichtbar, Badge → der Platz steht im Popover. Bei aktiver „rekursiver Suche“ erscheint
  auch eine Zeile im zugeklappten Ast. Ohne rekursive Suche gilt der Baum-Vorrang ([[fallstricke]] §3 „BOM-Baum hat
  Vorrang“). *Negativ:* Filter auf einen nicht vorhandenen Code → keine Zeile.
- **95.14 Viele Positionen.** FA 1059463 (263 Positionen) → „Alle aufklappen“. *Erwartet:* Die Lagerplatz-Spalte ist
  nicht breiter als 3 Plätze + Badge. Die horizontale Scrollbar ist nur noch nötig, wenn die übrigen Spalten den
  Bildschirm sprengen. Die Seite reagiert flüssig beim Hovern über mehrere Badges.
- **95.15 Kontrast.** Badge in normaler, gestreifter, grüner (gepickt) und grauer (umgebucht) Zeile messen (z. B. mit
  dem DevTools-Kontrastwert). *Erwartet:* Text ≥ 4,5:1, Fokusrahmen ≥ 3:1.
- **95.16 Regression.** TS-79.20 (Zahnrad bei Leerzustand), Baum auf/zu, Komm.-Ziel-Dropdown, Picking-Checkbox +
  Umbuchen, Quell-Lagerplatz-Select2. *Erwartet:* unverändert.

## Etappen (nur bei epic: true)

Entfällt, flache Spec. Ein Dev-Lauf, empfohlene Commit-Reihenfolge: M (Daten + View + Druck + Tests) → L (Sortierung +
Zelle + Popover + CSS + Tests) → Doku/Version.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Final (QA 2026-10-16, gegen den echten Diff `8773ba4f..d75680f4`).*

- **Web-App:** ja. Geändert unter `IdealAkeWms/`: `PickingController.cs`, `FaHierarchyBomRepository.cs`,
  `StockMovementRepository.cs`, `FaHierarchyBomItem.cs`, `BomViewModels.cs`, `ColumnDefinitions.cs`, `ReadOnlyBomBuilder.cs`,
  `Views/Picking/Bom.cshtml`, `Views/Picking/PrintBom.cshtml`, `Views/Help/{Index,Changelog}.cshtml`, `wwwroot/css/site.css`,
  `AppVersion.cs`.
- **Service:** nein. Unter `IDEALAKEWMSService/` änderte sich nur `AppVersion.cs` (1.61.0 → 1.62.0), kein Verhalten.
  Ein Publish des Dienstes ist nur nötig, wenn die angezeigte Dienst-Version mitziehen soll.
- **Migration:** nein. Keine Datei unter `*/Migrations/`, kein `SQL/`-Skript, kein Einmal-Skript (`git diff --stat`).
- **Publish-Befehl** (im Bündel-Worktree, mit dem Bündel):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- Ablauf: Publish aus dem Worktree → Testsystem → TS-95 → Merge. Nach dem Merge nur dann erneut aus `main` publishen,
  wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.
- Wirkt an **beiden** Standorten. Ausrollen mit dem Bündel (ein Merge, Schranke 2). Den Browser-Cache deckt
  `asp-append-version` ab, auch für `site.css` (`_Layout.cshtml:11`).

**Brain-Update-Liste (Dev-/QA-Lauf, im Hauptcheckout):**

- [[fallstricke]] additiv:
  - §12 Nachtrag „Stückliste liest `Article.Matchcode` über `Ressourcenummer`“ (bzw. Variante B mit Begründung), P1-Ergebnis
  - §3/§14 Nachtrag „Volltext für Filter per `d-none` in der Zelle, CSV per `data-csv-value`. Filter, Sortierung und
    Leerzustand lesen `textContent`“
  - §7 Satz zur neuen Sortierung „Hauptlagerplatz, Menge absteigend, Code“
- `codebase/` (module/controller): `ReadOnlyBomBuilder` mit `IArticleRepository`. Stückliste nutzt `MatchcodeLookup`.
- [[feature-map]] (Stückliste: Matchcode, Lagerplatz-Limit), Changelog-Eintrag `secondbrain/changelog/`,
  [[testszenarien-index]] (TS-95).
- [[2026-09-15-matchcode-nachlese]]: Punkt 1 Teil „Stückliste/PrintBom“ als erledigt mit Verweis markieren (überholt
  kennzeichnen, nicht löschen). Offen bleiben PrintPicking, WarehousePicking/Details, OSEON.
- Bug [[2026-10-16-stueckliste-lagerplatz-zelle-tabellenbreite-bug]] auf `behoben` mit Version.

**Hilfeseite (`Views/Help/Index.cshtml`, Abschnitt Kommissionierung `:1246-1335`), konkrete Inhalte:**

- Spalte **Matchcode** in der Stückliste: woher sie kommt („Artikelstamm“, bzw. bei B „FA-Struktur, sonst
  Artikelstamm“), leer heißt „kein Matchcode im Artikelstamm“, filterbar wie andere Spalten.
- Spalte **Lagerplatz**: höchstens 3 Plätze, Hauptlagerplatz zuerst, dann nach Menge. „+n weitere“ per Mauszeiger,
  Antippen oder Tab öffnet die volle Liste, Escape schließt. Der Filter findet auch verdeckte Plätze. CSV und Druck
  enthalten die volle Liste (Druck laut Antwort 3).
- Satz `:1335` (Kommissionierwagen) bleibt gültig.

## Offene Rueckfragen

1. **Matchcode-Quelle an IDEAL: A oder B?**
   - **A** (Empfehlung, Vorschlag der Koordination): Artikelstamm an beiden Standorten, ein Codeweg, gleich wie die
     FA-Listen.
   - **B**: An IDEAL der Matchcode aus der FA-Struktur (Position), sonst der Artikelstamm. Gleich wie
     Kommissionierliste, Vormontage und Beschichtung.

   Am Code können beide abweichen: Positions-Matchcode in Sage („Position bevorzugt“), Handpflege im WMS, Zeitversatz.
   Gemessen ist das nicht. **Bitte vorher P1 (WMS-DB) und P2 (Sage IDEAL) aus dem Lösungsentwurf laufen lassen.**
   Empfehlung: A, wenn `MatchcodeAbweichend` = 0. Sonst B, damit Stückliste und Kommissionierliste denselben Kurznamen
   zeigen. Zeigt P1 viele `OhneArtikelstamm`, spricht das zusätzlich für B ([[fallstricke]] §12, Nachlese Punkt 3).
2. **Lagerplatz-Spalte filterbar machen?** Die Entscheidung vom 2026-10-16 setzt voraus, dass der Spaltenfilter
   „weiter“ auf der vollen Liste arbeitet. Die Spalte hat heute aber **kein** Filterfeld (`Bom.cshtml:172` ohne
   `data-filterable`, `ColumnDefinitions.cs:239`, `:262` „Not filterable“). **Empfehlung: ja**, sonst gibt es kein
   „Filtern nach verdecktem Platz“. Kosten: ein Attribut. Nebenwirkung: Die Spalte wird per Kopfklick sortierbar, wie
   alle filterbaren Spalten (`table-filter.js:243-267`).
3. **Druck (`PrintBom`): volle Lagerplatzliste oder ebenfalls Limit?** Papier hat kein Popover. Die Druckzellen brechen
   um (`PrintBom.cshtml:99-103` ohne `nowrap`, Tabelle `width: 100%` `:84-88`). Die Breite ist dort also kein Problem,
   es wird nur höher. **Empfehlung: volle Liste**, in der neuen Reihenfolge. Ein Limit würde auf dem Arbeitspapier
   Plätze unterschlagen.
4. **Lagerbestellung-Details (`WarehousePicking/Details`) gleich lösen?** Dort steht die Liste als `<small>`-Text
   (`Details.cshtml:102`). Sie kommt aus einem anderen Datenweg (`GetCurrentStockAsync`, nur Bestand > 0,
   `WarehousePickingController.cs:120-122`, `:367-369`). Der Druck spiegelt sie über `WarehousePickingPrintLayout.CellText`
   (`:78`), wo drei Stellen synchron bleiben müssen ([[fallstricke]] §3). Eine Lagerbestellung hat meist wenige
   Positionen, das 263-Zeilen-Problem fehlt dort. **Empfehlung: nein, nicht in dieser Spec.** Bei Bedarf als eigene
   Backlog-Notiz.
5. **Ausgestaltung bestätigen** (Geralds Entscheidung sagt „in der Spec zu bestätigen“):
   - **(a)** 3 sichtbare Plätze als **Konstante**. Ein AppSetting bräuchte Seed, `/Settings` und README und hat keinen
     belegten Bedarf (ADR [[0011-feature-toggles-ueber-appsettings]]).
   - **(b)** Reihenfolge Hauptlagerplatz, dann Menge absteigend, dann Code, **zentral im Repository**. Sie wirkt damit
     auch auf den Druck, andere Aufrufer sortieren selbst.
   - **(c)** Popover per Hover, Klick/Tippen und Tab-Fokus, Schließen per Wegbewegen, Klick/Tippen daneben oder Escape.
   - **(d)** Im Popover den Hauptlagerplatz als Text „Hauptlagerplatz“ kennzeichnen.

   **Empfehlung: alle vier ja.**

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Matchcode-Quelle an IDEAL: A (Artikelstamm) oder B (Struktur, Fallback Artikelstamm)? P1/P2-Ergebnis → **B (Gerald 2026-10-16, Empfehlung Koordination): an IDEAL `FaHierarchyNode.Matchcode`, leer -> Artikelstamm; AKE Artikelstamm. P1/P2 nur noch informativ, keine Vorbedingung.**
2. Lagerplatz-Spalte filterbar machen → **Ja (Gerald 2026-10-16).**
3. Druck PrintBom: volle Lagerplatzliste oder Limit → **Volle Liste, neue Reihenfolge (Gerald 2026-10-16).**
4. Lagerbestellung-Details gleich lösen → **Nein, nicht in dieser Spec (Gerald 2026-10-16).**
5. Ausgestaltung (a) Konstante 3, (b) Sortierung zentral im Repository, (c) Hover/Klick/Tippen/Tab, (d) Hauptlagerplatz im Popover markiert → **Alle vier ja (Gerald 2026-10-16).**

## Umsetzung (2026-10-16)

Direkt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Start `8773ba4f` (v1.61.0, Arbeitsbaum sauber,
kein offener Lauf), Ziel **v1.62.0**, **TS-95**, keine Migration. Matchcode-Quelle **Variante B** (Freigabe-Nachtrag 1).

| # | Inhalt | Commit |
|---|---|---|
| M | `FaHierarchyBomItem.Matchcode` + Mapping (getrimmt, leer = null); `BomItemViewModel`/`PrintBomItem.Matchcode`; `(bom as FaHierarchyBomItem)?.Matchcode ?? MatchcodeLookup(Ressourcenummer)` in `PickingController.Bom`, `RenderPrintBomAsync`, `ReadOnlyBomBuilder` (Ctor + 3 Testkonstruktionen); `Bom.cshtml` th/td/#column-config/emptyColspan/colNames; `ColumnDefinitions.Bom` matchcode + warehouse-order + `SupportsReorder: true` + Kommentar (Wert wird nirgends gelesen → trivial, Nachtrag 5); `PrintBom.cshtml` ShowCol | `c5d9b2b7` |
| L | Sortierung zentral (Hauptlagerplatz, Menge absteigend, Code); Zelle max. 3 + Badge „+n weitere“ (`<a tabindex="0" role="button">`, Popover `hover focus`, `container: 'body'`, Escape, Init vor `if (readOnly) return;`), volle Liste als `d-none`-`<ul>` (Hauptlagerplatz benannt), `data-csv-value`, `data-filterable`, colNames `storage-location`; `site.css` (24×24, Fokus, scrollender Popover-Body); Tests | `c9bc382f`, `41c3c14e` |
| Doku | Hilfe, Changelog, AppVersion 1.62.0 (Web + Dienst), TS-95 | `6a452892` |

**Nachweis (vor QA):**
- `dotnet build IdealAkeWms.slnx` → 0 Fehler, 24 Warnungen.
- `dotnet test` → Web **2042 bestanden, 1 übersprungen**, 0 Fehler; Service **307 bestanden**.
- `dotnet ef migrations has-pending-model-changes` → „No changes have been made to the model since the last migration.“
- Neue Tests: `ReadOnlyBomBuilderTests` (Kopf ≠ Komponente, fehlender Matchcode leer; Variante B Struktur vor Artikelstamm,
  leer → Fallback), `FaHierarchyBomRepositoryTests.Map_Matchcode_Getrimmt_LeerWirdNull`,
  `StockMovementRepositoryTests.GetStockByArticleNumbers_HauptlagerplatzDannMengeAbsteigendDannCode`, `BomLagerplatzZelleTests`.
  `BomZahnradHostTests` und CSV-Abdeckung grün; zwischen Host und `.table-responsive` nichts eingefügt.
- **AK 17 gemessen (WCAG-Formel, Bootstrap 5.3.3):** Badge-Text Weiß auf `#6c757d` **4,69:1**; Badge-Fläche gegen Zeilengrund
  4,69 / 4,19 / 3,61 / 3,65 : 1 (weiß / gestreift `#f2f2f2` / `table-success #d1e7dd` / `table-secondary #e2e3e5`, ≥ 3:1);
  Fokusrahmen `#212529` 15,4 / 13,8 / 11,9 / 12,0 : 1; Trefferfläche per CSS `min-height`/`min-width` 24 px. Sichtprobe im Browser = TS-95.15.

**Hinweis zum Lauf:** `c9bc382f` wurde mit einem roten Wächtertest committet (die Befehlskette prüfte nur die grep-Ausgabe); Ursache:
`IndexOf("if (readOnly) return;")` traf einen Kommentar. Behoben in `41c3c14e` (Regex auf die Anweisung), danach alles grün.
Code-Vergleich `ThenBy(i => i.Code)` unverändert (kein Ordinal-Wechsel).

## QA (2026-10-16, qa-agent)

Geprüft im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Diff `8773ba4f..6a452892`, danach QA-Commits
`9177db47` (Fix) und `d75680f4` (TS-95.16). Review synchron in diesem Lauf, nichts delegiert.

### 1. Build und Tests (nach dem QA-Fix)

| Schritt | Ergebnis |
|---|---|
| `dotnet build IdealAkeWms.slnx` | 0 Fehler, 28 Warnungen (Bestand, z. B. CS8601 in `BdeTerminalAuftraegeTests.cs:23`) |
| `dotnet test` – `IdealAkeWms.Tests` | **2043 bestanden, 1 übersprungen, 0 Fehler** (vor dem Fix 2042/1/0, +1 neuer Wächter) |
| `dotnet test` – `IDEALAKEWMSService.Tests` | **307 bestanden, 0 übersprungen, 0 Fehler** |

### 2. Code-Review – Befunde

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| W-1 | wichtig | **Positionsbindung verschoben:** Der Bulk-Bestell-Fallback in `Bom.cshtml` (vor dem Fix `:1788-1795`) las Zellen per festem Index `cells[4]`/`cells[6]`/`cells[8]`. Durch die neue Matchcode-Spalte an Index 6 las „Menge“ den Matchcode (`parseFloat` → `NaN`) und „Artikelgruppe“ die Beschaffung. Trifft angehakte **Baugruppen**-Zeilen (sie haben keinen `.order-single-btn`) bei „Ausgewählte bestellen“. Bei Zahnrad-Umordnung war der Pfad schon vorher fragil. | **Behoben** in `9177db47`: Zugriff über die vorhandene `bomColKeyToIndex` (`Bom.cshtml:874`). Neuer Quelltext-Wächter `BomLagerplatzZelleTests.Zellzugriff_NurPerColKey_KeinFesterIndex` (Regex `cells\[\d` darf nicht vorkommen). TS-95.16 ergänzt (`d75680f4`). |
| K-1 | klein | Die Matchcode-Projektion in `PickingController.Bom` (`:416`) und `RenderPrintBomAsync` (`:631`) hat keinen eigenen Unit-Test; abgesichert ist nur `ReadOnlyBomBuilder`. Der Ausdruck ist an allen drei Stellen wörtlich gleich. | Offen, akzeptiert: Manual-UAT TS-95.1/95.2/95.8. |
| K-2 | klein | Eine Regel an drei Stellen: `(bom as FaHierarchyBomItem)?.Matchcode ?? matchcodes.GetValueOrDefault(...)` steht dreimal (Spec-Vorgabe Variante B). Driftrisiko gering, solange die drei Stellen gleich bleiben. | Hinweis, kein Umbau. |
| K-3 | klein | `MatchcodeLookup` meldet an IDEAL auch Zeilen „ohne Artikelstamm-Treffer“, die einen Struktur-Matchcode haben. Die Zahl stimmt (Artikelstamm-Fehltreffer), sagt aber nicht „leere Zelle“. | Hinweis, kein Fix. |
| K-4 | klein | Badge-Text „+n weitere“ und „Hauptlagerplatz“ gehen in den Filter-/Sortiertext der Lagerplatz-Zelle ein. | In der Spec als bekannte Grenze akzeptiert. |

**Geprüft ohne Befund:**
- Variante B an allen drei Projektionsstellen, Quelle `Ressourcenummer` (nicht `Artikelnummer`). `FaHierarchyBomItem.Matchcode`
  wird getrimmt, leer wird `null` (`FaHierarchyBomRepository.cs:193`). `BomItem` hat keine neue Property (fallstricke §10).
- Zwei Registrierungen: `#column-config` (`Bom.cshtml:685`) und `ColumnDefinitions.Bom` (`:253`). `warehouse-order` ist
  ergänzt. `SupportsReorder: true` wird außer in der Record-Definition nirgends gelesen (Grep über `*.cs`, `*.cshtml`, `*.js`).
- Sortierung zentral (`StockMovementRepository.cs:470-473`). Alle Aufrufer von `GetStockByArticleNumbersAsync` geprüft:
  `PickingApiController.cs:38` und `MissingPartsLagerController.cs:53` sortieren selbst. Der Auto-Vorschlag
  (`PickingController.cs:379ff`) wählt per `FirstOrDefault(IsPrimary)` bzw. `OrderByDescending(Quantity)`. Keine
  Wirkungsänderung dort.
- Lagerplatz-Zelle: `Take(3)`, Badge mit n = Anzahl − 3, `d-none`-Volltext, `data-csv-value` (Razor lässt das Attribut bei
  `null` weg → CSV „-“), `data-filterable`, `colNames['storage-location']`. Werte sind Razor-kodiert. Das Popover bekommt ein
  geklontes DOM-Element. Bootstrap 5.3.3 hängt es bei `html: true` an, ohne HTML-Strings zu parsen. Der Titel ist ein
  Zahlenwert (sanitize aktiv). **Kein XSS.** Kein `@Html.Raw`, keine Werte in `<script>`-Strings.
- Popover-Init (`Bom.cshtml:1442ff`) steht vor der Anweisung `if (readOnly) return;` (`:1477`). Dazu `container: 'body'`,
  `trigger: 'hover focus'` und Escape-Handler.
- CSS (`site.css:1408-1427`): 24 × 24 px über `min-height`/`min-width`, `:focus-visible` mit Outline 2 px `#212529`, kein
  `outline: none`.
- Host `Bom.cshtml:142` steht direkt vor `.table-responsive` `:143`, dazwischen nichts. `BomZahnradHostTests` ist grün.
- `emptyColspan` 13/11 nachgezählt (13 Spalten ohne ReadOnly: pick-control, Pos., Baugruppe, Ressourcennr., Bez. 1, Bez. 2, Matchcode,
  Menge, Beschaffung, Artikelgruppe, Kategorie, Lagerplatz, Quell-Lagerplatz). `PrintBom`: `ShowCol("matchcode")` an allen drei
  Stellen. Lagerplatz zeigt die volle Liste (`PickingController.cs:639`).
- DI: `ReadOnlyBomBuilder` ist scoped (`Program.cs:161`), `IArticleRepository` ist registriert (`:68`), `ILogger<T>` ist Standard.
- Hilfe, Changelog, beide `AppVersion.cs` 1.62.0, TS-95 in `docs/TESTSZENARIEN.md`, Index-Zeile 95 vorhanden.
- Defektklassen: `hidden` + `d-*` kommt im Diff nicht vor (`#bomKzEmptyState` ist Bestand; die globale `[hidden]`-Regel steht
  in `site.css:20`). Keine stillen Fallbacks außer dem gewollten „kein Matchcode = leer“ (AK 3). Kein neuer Schreibweg,
  daher keine Server-Validierung nötig.
- **WCAG nachgerechnet (WCAG-Formel):** Weiß auf `#6c757d` = **4,69:1** (L = 0,174). Fokusrahmen `#212529` auf
  `table-success #d1e7dd` = **11,9:1**. Die übrigen Zeilengründe sind heller oder gleich, also ≥ 11,9:1. Die Werte des
  Dev-Nachweises sind bestätigt.

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweisart | Beleg |
|---|---|---|
| 1 Spalte nach Bez. 2, gespeicherte Auswahl | Quelltext + **Manual-UAT** | `Bom.cshtml:156/253/685`; Einfügen bei gespeicherter Auswahl → TS-95.6 |
| 2 Komponente statt Kopf | Unit-Test (ReadOnly-Pfad) + Manual-UAT (Picking/Druck) | `ReadOnlyBomBuilderTests.BuildAsync_Matchcode_IstDerDerKomponente_NichtDesKopfes_FehltLeer`; TS-95.1/95.2 |
| 3 leer, kein Fehler, Log | Unit-Test (leer) + Manual-UAT (Log) | gleicher Test; Log-Zeile TS-95.4 |
| 4 Filter Matchcode | **Manual-UAT** (Browser-JS) | TS-95.5 |
| 5 Zahnrad/Prefs-API | **Manual-UAT** | TS-95.6 (die Zwei-Registrierung ist Quelltext, die 400-Prüfung braucht den echten Request) |
| 6 CSV Matchcode | Quelltext-Wächter (`CsvExportCoverageTests`) + **Manual-UAT** | TS-95.7 |
| 7 Druck Matchcode + Filterhinweis | **Manual-UAT** | TS-95.8 |
| 8 Variante B | Unit-Test | `ReadOnlyBomBuilderTests.BuildAsync_Hierarchisch_StrukturMatchcodeVorArtikelstamm_LeerFaelltZurueck`, `FaHierarchyBomRepositoryTests.Map_Matchcode_Getrimmt_LeerWirdNull`; Echtdaten IDEAL → TS-95.1 |
| 9 ≤ 3 Plätze unverändert | **Manual-UAT** | TS-95.9 |
| 10 genau 3 + „+n weitere“ | Quelltext-Wächter + **Manual-UAT** | `BomLagerplatzZelleTests.Zelle_FilterbarMitVolltextUndCsvWert`; TS-95.9 |
| 11 Reihenfolge | Unit-Test (InMemory, LINQ in-memory nach `ToListAsync` → aussagekräftig) | `StockMovementRepositoryTests.GetStockByArticleNumbers_HauptlagerplatzDannMengeAbsteigendDannCode`; Druck/CSV TS-95.7/95.8 |
| 12 Popover-Bedienung | Quelltext-Wächter (Init-Ort, Trigger) + **Manual-UAT** | `BomLagerplatzZelleTests.PopoverInit_…`; TS-95.10–95.12 |
| 13 Scroll im Popover | **Manual-UAT** | TS-95.10 |
| 14 Filter verdeckter Platz | Quelltext-Wächter (`d-none`-Volltext, `data-filterable`) + **Manual-UAT** | TS-95.13 |
| 15 CSV volle Liste | Quelltext-Wächter + **Manual-UAT** | TS-95.7 |
| 16 „-“, keine Wagen | Bestandstests Repository + **Manual-UAT** | TS-95.16 |
| 17 Kontrast/Trefferfläche | Rechnung (oben) + **Manual-UAT** | TS-95.15 |
| 18 Breite bei 263 Positionen | **Manual-UAT** | TS-95.14 |
| 19 Zahnrad | Quelltext-Wächter + **Manual-UAT** | `BomZahnradHostTests`; TS-95.16 |
| 20 Bestandsverhalten | **Manual-UAT** (+ W-1-Wächter) | TS-95.16 |
| 21 Build/Tests/Version/Doku | automatisiert + Sichtprüfung | Abschnitt 1; `AppVersion.cs` ×2, Changelog, Hilfe |

Grün gezählt sind nur die Unit-Test-Zeilen. Alle **Manual-UAT**-Zeilen sind offen, bis TS-95 abgenommen ist.

### 4. Geänderte Bestandstests

| Test | Änderung | Klasse |
|---|---|---|
| `FaCompletionControllerTests.cs:55` | Ctor `ReadOnlyBomBuilder` + `ArticleRepository`, `NullLogger` | nur Typ (Compile-Fix) |
| `FaWorklistControllerTests.cs:73` | ebenso | nur Typ (Compile-Fix) |
| `ReadOnlyBomBuilderTests.cs:33` (Helper `Build`) | ebenso; dazu 2 neue Tests | nur Typ (Compile-Fix) + neue Tests |
| `StockMovementRepositoryTests.cs` | bestehender Test `:224-244` unverändert, 1 neuer Test | nur Ergänzung |
| `FaHierarchyBomRepositoryTests.cs` | 1 neuer Test, Bestand unverändert | nur Ergänzung |
| `BomLagerplatzZelleTests.cs` | neu (Dev), +1 Wächter (QA) | neu |

Keine Fixture wurde angepasst, bis ein Test grün wurde. Kein Befund der Klasse „Sonstiges“.

### 5. Checkliste CLAUDE.md

Migration/SQL/FreshInstall/AgentJobs: entfällt (kein Schema). Audit-Felder: entfällt (kein Schreibweg). Version beide
`AppVersion.cs` 1.62.0 ✓, Anwender-Changelog ✓, Hilfe ✓, `docs/TESTSZENARIEN.md` TS-95 ✓, Index Zeile 95 ✓. Neue
Rolle/Filter: keine. **Offen für den Brain-Lauf (nicht QA):** Brain-Changelog `secondbrain/changelog/`, [[feature-map]],
[[fallstricke]]-Nachträge (§3/§14 Volltext per `d-none` + `data-csv-value`, §7 neue Sortierung, §12 Variante B; neu: feste
`cells[n]`-Indizes in Bom.cshtml verboten, W-1), [[2026-09-15-matchcode-nachlese]] Punkt 1 teilweise erledigt, Bug-Notiz auf
behoben.

## Manuelle Test-Checkliste (Schranke 2)

1. Web aus dem Worktree publishen (siehe Deploy) und auf das Testsystem bringen. Version 1.62.0 im Fuß prüfen.
2. IDEAL, FA 1059463 → Stückliste: Matchcode rechts von „Bezeichnung 2“, 3 Zeilen gegen FA-Struktur/Artikelinfo der
   **Ressourcenummer** prüfen (TS-95.1).
3. AKE-FA → Stückliste: Matchcode aus dem Artikelstamm (TS-95.2).
4. Abarbeitungsliste und FA-Vervollständigung → Stückliste, an IDEAL auch ein Sub-FA: Matchcode da, Popover geht (TS-95.3).
5. Komponente ohne Matchcode: Zelle leer, `(leer)`/`!(leer)` wirken, Log-Zeile „Matchcode-Join … (Stueckliste)“ (TS-95.4).
6. Matchcode filtern in Kleinschrift, `a, b`, `!x` (TS-95.5).
7. Benutzer mit gespeicherter Spaltenauswahl: Matchcode sichtbar; ausblenden/umordnen + F5 bleibt; PUT
   `/api/user-view-preferences/Bom` ≠ 400 (TS-95.6).
8. CSV: Matchcode-Spalte da bzw. weg; Lagerplatz volle Liste ohne „weitere“ (TS-95.7).
9. Druck: Matchcode-Spalte, Lagerplatz volle Liste in neuer Reihenfolge, Filterhinweis „Matchcode=…“/„Lagerplatz=…“ (TS-95.8).
10. Komponente mit > 3 Plätzen **und** WMS-Hauptlagerplatz: 3 Plätze + „+n weitere“, Hauptlagerplatz zuerst, dann Menge
    absteigend (TS-95.9).
11. Popover per Maus (Hover, Klick bleibt, Klick daneben schließt), nicht abgeschnitten in letzter Spalte/Zeile, lange Liste
    scrollt (TS-95.10).
12. Popover per Tablet-Tippen, zwei Badges nacheinander → nur eins offen (TS-95.11).
13. Popover per Tab, Fokusrahmen sichtbar, Escape und Tab weiter schließen, Screenreader-Ansage (TS-95.12).
14. Lagerplatz-Filter auf einen verdeckten Platz (Position ≥ 4): Zeile erscheint; Negativ: unbekannter Code → keine Zeile
    (TS-95.13).
15. FA 1059463 „Alle aufklappen“: Lagerplatz-Spalte kompakt, Hover flüssig (TS-95.14).
16. Kontrast im Browser messen auf normaler, gestreifter, grüner, grauer Zeile (TS-95.15).
17. Regression: „-“ ohne Bestand, keine Kommissionierwagen, Zahnrad bei Leerzustand (TS-79.20), Baum auf/zu,
    Komm.-Ziel-Dropdown, Picking-Checkbox + Umbuchen, Quell-Lagerplatz-Select2 (TS-95.16).
18. **QA-Fix W-1:** Bestellen aktiv, eine Baugruppen-Zeile + eine Bauteil-Zeile anhaken → „Ausgewählte bestellen“: Bezeichnung,
    Menge (keine `NaN`) und Artikelgruppe stimmen. Dasselbe nach Umordnen der Spalten per Zahnrad (TS-95.16).
- Browser-Abnahme 2026-10-21: [[2026-10-21-ideal-v1-59-bis-v1-64-browser-testbericht]] - 45 OK, 2 FEHLER, 1 nicht pruefbar, 0 Protokoll passt nicht
