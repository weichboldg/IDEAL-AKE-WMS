---
typ: notiz
spec: "[[2026-09-13-matchcode-artikelstamm-spec]]"
status: InUmsetzung
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Matchcode im Artikelstamm (Article.Matchcode) — hausweit suchbar

Spec [[2026-09-13-matchcode-artikelstamm-spec]] · bestehender Bündel-Worktree (kein neuer, nicht von
`main` abgezweigt). Plan im Worktree:
`docs/superpowers/plans/2026-09-15-matchcode-artikelstamm.md`. Version 1.40.0, Testkapitel TS-75.

Der Matchcode ist artikelbezogen (je Artikel in jedem Auftrag gleich) → gehört an `Article`, nicht an
`ProductionOrder`. Diese Spec **widerruft den Ort** der in v1.38.0 gebauten (Testbereit, nie gemergten)
`ProductionOrder.Matchcode`-Spalte, **nicht** die Anzeige/Suchbarkeit: die fünf FA-Zeilen-Listen behalten
Spalte + Filter, gespeist künftig aus `Article` per Equi-Join über `ArticleNumber`.

## Vorab am Worktree-Code verifiziert (2026-09-15, vor dem Plan)

- **`AddProductionOrderMatchcode` (`20260910081155`) ist die letzte Migration** → `ef migrations remove`
  trägt (S-3/H-1). Reihenfolge verbindlich: **erst remove (ProductionOrder), dann add (Article)**.
- **`Article.Matchcode` existiert noch nicht** (`Article.cs`); `ProductionOrder.Matchcode` = Zeilen 43-45.
- **Fünf Matchcode-Quellstellen** in den Listen-Projektionen: `ProductionOrdersController` (:151),
  `ProductionOrderRepository.ProjectLeitstandRows` (:178, PickingLeitstand), `FaCompletionController`
  (:125), `FaWorklistController` (:225), `PickingController` (:159, in-memory). Plus Server-Spaltenfilter
  `ProductionOrderRepository.ApplyLeitstandColumnFilter` Case `"matchcode"` (:308).
- **Spec-Ungenauigkeit (A1):** Spec (Zeile 318-321) nimmt an, `BuildExtraInfoOrContains` akzeptiere einen
  beliebigen Selektor für die korrelierte Article-Subquery. **Falsch** — der Helper castet den Body zu
  `MemberExpression` (:421) und zieht dessen `.Expression` als Null-Guard. Eine Subquery ist ein
  `MethodCallExpression`. **Lösung:** Matchcode-Spaltenfilter der FA-Liste geht in die bestehende
  C#-Postfilter-Maschinerie (`FaListComputedColumnKeys` = customer/prio/ab-nummer/montage-abteilung,
  Schwester-Spec Task 6) — Matchcode wird ein berechneter Wert wie Kunde im hierarchischen Modus, kein
  SQL-Prädikat mehr. Die Client-Filter-Dicts (`i => i.Matchcode`) in FaCompletion/FaWorklist/Picking/
  ProductionOrders bleiben unverändert, sobald die Projektion `item.Matchcode` aus dem Article-Join füllt.
- **AK 15 ist real (feuert je Tastendruck):** Artikel-Typeahead über Select2 `_Select2ArticlePartial`
  (`minimumInputLength: 0`, delay 300), `StockMovements/InboundBulk` (minLen 0), `WarehouseRequisitions/
  Edit` (custom fetch) → auf `minimumInputLength: 3` + ~300 ms setzen. `_Select2ProductionOrderPartial`
  ist Auftrags-, keine Artikelsuche — unberührt.
- **AK 16:** `Article.Matchcode` `NVARCHAR(200)`, `CAST(... AS nvarchar(200))` — **beide 200 identisch**.
  Freigabe-Antwort 4 zeigt `nvarchar(500)` → das ist Begründung, nicht Auftrag; AK 16 (200) gilt (Nutzer-
  Vorgabe: „Antwort-Abschnitte sind Begründung, nicht Auftrag").
- **AK 14:** Fehltreffer-Zähler (Article-Join ohne Treffer) je Listenaufbau protokollieren — eine Zahl je
  Aufbau. Batch-Dict-Ansatz (`GetMatchcodesByArticleNumbersAsync`) deckt Anzeige, in-memory-Fall und
  Zählung zugleich ab.
- **BLOCKER B-1** der Kritischen Prüfung (2026-09-15) ist durch die Freigabe-Entscheidung ausgeräumt:
  nicht vorab messen, sondern **melden statt still** (AK 2 Vorher/Nachher-Zählung Erwartung null + AK 14
  Log). Freigabe kam nach der Prüfung.

## Arbeitsstand

- 2026-09-15: Spec entwurf→freigegeben gezogen (Rename), Status `InUmsetzung`. Plan folgt.
- 2026-09-15: 8 Tasks (SDD) umgesetzt, je task-reviewed; Gesamt-Review (9 Commits, `9e90024..7fa71c1`)
  **MERGE-READY**, keine CRITICAL/MAJOR. Worktree-HEAD `7fa71c1`. Build 0 Fehler.
  Gemessene Spec-Korrekturen: **A1** (`BuildExtraInfoOrContains` nimmt keine Subquery → Matchcode-
  Spaltenfilter als C#-Postfilter, [[fallstricke]] §12), **A6** (Articles-Liste hat keine
  ColumnDefinitions-ViewConfig, nur `<th data-col-key>` — Spec affected_code:22 ungenau).
  Implementer-Abweichungen (alle im Review bestätigt): Matchcode-Filter-Fix auf Flach-Zweig +
  PickingLeitstand ausgeweitet (sonst stiller Filter-No-op); geteilter `Services/MatchcodeLookup.cs`
  (7 Call-Sites); `WarehouseRequisition` per-Token-Query + `Union` statt nested `.Any()` (InMemory);
  `EF.Functions.Like`-Pfade via `ToQueryString()`-Test ([[fallstricke]] §8/§12).
- **AK 16:** `CAST … nvarchar(200)` = Zielspalte 200 (das `nvarchar(500)` der Freigabe-Antwort war
  Begründung, überholt — Nutzer-Vorgabe „Antworten = Begründung").
- **BLOCKER B-1** (Coverage IDEAL) durch Freigabe-Entscheidung ausgeräumt (melden statt still:
  AK 2 Vorher/Nachher + AK 14 Fehltreffer-Zähler); der erste echte Lauf beantwortet die Frage.
- Nachlese (bewusst verschoben): [[2026-09-15-matchcode-nachlese]] (OSEON/BOM-Matchcode, FA-Baum auf
  Artikelstamm, `Articles`-Sync um gefertigte Artikel erweitern falls AK-14-Zähler > 0).
- Status → `Testbereit` durch den qa-agent (grüner Gesamtnachweis + Deploy). Wartet mit dem ganzen
  Bündel auf Schranke 2. Kein Merge, kein Push.
