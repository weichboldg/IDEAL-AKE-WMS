---
type: aufgabe
title: Umsetzung WMS Bugs & Improvements Teil 1-3 (ein Worktree, sequentiell)
status: Gemerged
created: 2026-08-05
spec: "[[2026-08-05-wms-bugs-improvements-teil-1-spec]]"
worktree: ".claude/worktrees/2026-08-05-wms-bugs-improvements-teil-1-2-3"
branch: "feature/2026-08-05-wms-bugs-improvements-teil-1-2-3"
---

# Umsetzung WMS Bugs & Improvements Teil 1-3

Drei unabhaengige, freigegebene Teil-Specs sequentiell im **gemeinsamen** Worktree
`feature/2026-08-05-wms-bugs-improvements-teil-1-2-3` (Auftrag des Menschen: gleicher Worktree).
Alle drei sind Web-only, keine Migration.

- [[2026-08-05-wms-bugs-improvements-teil-1-spec]] — FA-Hinweis auf Ist-Bestand (Bugfix)
- [[2026-08-05-wms-bugs-improvements-teil-2-spec]] — Mehrfachartikel-Einbuchung (Feature)
- [[2026-08-05-wms-bugs-improvements-teil-3-spec]] — WA-Scan-Button Bewegungshistorie (Feature)

## Reihenfolge & Datei-Overlap
Teil 2 und Teil 3 fassen **beide** `wwwroot/js/barcode-scanner.js` an → sequentiell, gleicher
Worktree ist zwingend. Reihenfolge: 1 → 2 → 3.

## Teil 1 — GetStockByProductionOrderAsync onlyActualStock
- [ ] `IStockMovementRepository.GetStockByProductionOrderAsync(string, bool onlyActualStock = true)`
- [ ] Repository: Kandidaten via FA-Tag; `true` = Ist-Bestand ueber alle Bewegungen, nur > 0;
      `false` = bit-identisch heute (FA-Netto-Summe)
- [ ] Aggregationsregel NICHT ein 3. Mal duplizieren (gemeinsamer Helper)
- [ ] `StockOverviewController.Index` ruft explizit `onlyActualStock: false`
- [ ] `StockApiController` bleibt Default `true`
- [ ] Repository-Tests: true-Pfad (AK1-3), false-Regression (AK6)

## Teil 2 — InboundBulk
- [ ] `StockMovementBulkInboundViewModel` (+ Line)
- [ ] Action `InboundBulk` GET/POST, `[RequireStockAccess]`
- [ ] Buchung je Zeile ueber `IStockMovementRepository.AddAsync` (Sage-Decorator! kein DbContext-Bypass)
- [ ] Validierung ALLER Zeilen vor erstem AddAsync (keine Teilbuchung), Eingaben-Erhalt bei Fehler
- [ ] View mit Kopf (Lagerplatz+FA) + dynamischer Zeilenliste, Menge 1 default
- [ ] Scan sequenziell, gleicher Artikel → Menge+1, Toggle „neue Zeile erzwingen"
- [x] Navigation/Link zur neuen Seite

## Teil 3 — WA-Scan-Button
- [ ] `id=filterProductionOrder` + Scan-Button in `StockMovements/Index.cshtml`
- [ ] barcode-scanner.js + html5-qrcode LOKAL einbinden
- [ ] `initTextInputScanner` + onScanned-Callback `split(/[-_]/)[0]`
- [ ] kein Auto-Submit

## Abschluss
- [x] docs/TESTSZENARIEN.md (TS-2.22–2.25) + secondbrain/tests/testszenarien-index.md
- [x] Version-Bump v1.29.0 (beide AppVersion.cs) + Changelog.cshtml + Brain-Changelog
      [[2026-08-05-v1-29-0-wms-bugs-teil-1-2-3]]
- [x] Code-Review (kein BLOCKER; 2 Nachbesserungen umgesetzt), QA gruen, alle 3 Specs Testbereit
- [x] feature-map.md, Folge-Backlog [[2026-08-05-bulk-einbuchung-bedarfsmeldungen-fulfillment]]

## Ergebnis (Testbereit, 2026-08-05)
Alle drei Teile umgesetzt, `dotnet build` 0 Fehler, `dotnet test` Web 1074 passed/1 skipped +
Service 195 passed. Offen: **Schranke 2** (Mensch: Manual-UAT je Teil, dann Merge). Worktree/Branch
bleiben bis zur expliziten Freigabe stehen. **Nebenfund:** veraltete Duplikate der drei Specs in
`secondbrain/specs/entwurf/` (status Entwurf) — sollten aufgeraeumt werden (Wikilink-Kollision).
