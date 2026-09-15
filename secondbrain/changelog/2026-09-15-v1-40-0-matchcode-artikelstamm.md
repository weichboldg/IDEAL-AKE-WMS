---
type: changelog
version: 1.40.0
date: 2026-09-15
---
# v1.40.0 — Matchcode im Artikelstamm (Article.Matchcode), hausweit suchbar

Umsetzung der freigegebenen Spec [[2026-09-13-matchcode-artikelstamm-spec]] im **Bündel-**Worktree
`feature/2026-08-07-ideal-teile-1-5`. Umsetzungsnotiz [[2026-09-13-matchcode-artikelstamm-umsetzung]],
Plan `docs/superpowers/plans/2026-09-15-matchcode-artikelstamm.md` (8 Tasks, SDD).

**Warum:** Der Matchcode (Typenkurzbezeichnung) ist **artikelbezogen** — für denselben Artikel in jedem
Auftrag gleich. Er gehört an `Article`, nicht an `ProductionOrder`; nur am Artikelstamm wird er in JEDER
Suche sichtbar, die heute Artikelnummer oder Bezeichnung findet. Diese Spec **widerruft den Ort** der in
v1.38.0 gebauten (Testbereit, nie gemergten) `ProductionOrder.Matchcode`-Spalte, **nicht** die Anzeige/
Suchbarkeit.

## Umgesetzt

- **Neue Spalte `Article.Matchcode`** (`NVARCHAR(200)`, Migration `20260915094646_AddArticleMatchcode`),
  additiv/idempotent. Sauberer Rückbau der `ProductionOrder.Matchcode`-Spalte: **Reihenfolge verbindlich
  remove → add** (`AddProductionOrderMatchcode` entfernt statt per Gegen-Migration abgebaut, weil nie
  deployt).
- **Fünf FA-Zeilen-Listen** (`ProductionOrders`, `PickingLeitstand`, `FaCompletion`, `FaWorklist`,
  `Picking`) behalten Spalte + Filter, gespeist jetzt aus `Article` per Equi-Join über `ArticleNumber`
  (Batch-Lookup `Services/MatchcodeLookup.cs`, eine Abfrage je Aufbau). Der FA-Listen-Spaltenfilter wurde
  zum **C#-Postfilter** (`FaListComputedColumnKeys`), weil `BuildExtraInfoOrContains` keine korrelierte
  Subquery aufnehmen kann (siehe [[fallstricke]] §11).
- **Hausweit suchbar** (acht Fundstellen): Artikelstammliste (Freitext + Spaltenfilter),
  Bestandsübersicht/Bewegungshistorie, Fehlteile/Lager-/Glasbestellung, Bedarfsmeldungen,
  FA-Fertigmeldung-Freitext, plus die fünf FA-Listen. Bei Snapshot-Feldern (PartRequisition,
  WarehouseRequisitionItem) über korrelierte Subquery auf `Article`, **kein** neues Snapshot-Feld
  (Matchcode bleibt „eine Quelle, immer aktuell").
- **Artikelinfo** zeigt den Matchcode und findet einen Artikel per **exaktem** Matchcode-Fallback, wenn
  die Artikelnummer nicht trifft (Scan exakt mit Vorrang; Teilstring nur für die getippte Suche).
- **Sage-Artikel-Sync** liest `KHKArtikel.Matchcode` (`SageImportService.SyncArticlesAsync`, beide
  UNION-Zweige, `MAX` je Artikel, `CAST … nvarchar(200)`), inkl. Änderungserkennung und Audit-Feldern.
- **Melden statt still** (AK 14): jeder erweiterte Listenaufbau protokolliert **eine** Zahl — wie viele
  FA-Zeilen über den Join keinen Artikelstamm-Treffer finden. So macht der erste echte Lauf die
  Coverage-Lücke (gefertigte Endgeräte evtl. nicht in `Articles`) sichtbar, statt sie vorauszusetzen.
- **AK 15:** Artikel-Typeahead (`/api/articles/search`) feuert erst ab **3 Zeichen** (+ ~300 ms
  Entprellung) — ein Full Scan über 108.818 Zeilen pro Buchstabe ist ausgeschlossen.

## Bewusst außen vor

- **OSEON Teileverfolgung** und **BOM-Komponentenebene** zeigen unverändert keinen Matchcode
  (eigene Datenpipelines ohne Article-Join) — Folge-Arbeit, [[2026-09-15-matchcode-nachlese]].
- **„Kommissionierung nur auf HauptFA"** ist eine eigene Spec ([[2026-09-13-kommissionierung-nur-hauptfa]]).

## Deploy

- **Web: ja · Service: ja · Migration: ja.** DB-Backup vor der Migration. Migrationen des Bündels
  (inkl. `20260915094646_AddArticleMatchcode`) über SQL-Skript + History-Insert, **nicht** per `Migrate()`
  ([[fallstricke]] §8). Nach dem Deploy einen Artikel-Sync-Lauf abwarten, bevor der Matchcode erscheint
  (Zwei-Lauf-Muster). Bei AKE füllt sich die Spalte erst mit dem Sync-Query-Ausbau, der mit diesem
  Bündel ausgeliefert wird.
- **Merge-Commit:** offen (Schranke 2, Mensch).

## Nachweis

Build 0 Fehler; genaue Testzahlen im Spec-QA-Abschnitt (qa-agent). Testszenarien **TS-75.1–75.9**
([[testszenarien-index]] → `docs/TESTSZENARIEN.md`). Gemessene Spec-Korrekturen (A1 Postfilter,
A6 keine Articles-ViewConfig) in der Umsetzungsnotiz.

## Zugehörig

Spec [[2026-09-13-matchcode-artikelstamm-spec]] · Umsetzung [[2026-09-13-matchcode-artikelstamm-umsetzung]]
· [[fallstricke]] §11 · Vorgänger [[2026-09-10-fa-liste-ausbau-matchcode-spec]] (v1.38.0, Ort widerrufen)
· Bündel [[2026-08-07-ideal-teile-1-5]].
