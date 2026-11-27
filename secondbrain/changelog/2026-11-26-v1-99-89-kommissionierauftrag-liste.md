---
type: changelog
version: 1.99.89
date: 2026-11-26
---
# v1.99.89 — IDEAL-Kommissionierauftrag: Maske „Liste“ summiert mit Verteil-Buchung, summierter Druck und PDF

Epic [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-3-spec]] (Bündel 1 Teil 3, Etappen A + B), Lauf-Notiz
[[2026-11-26-buendel-1-lagerbestellung-goal-lauf]]. Worktree `feature/2026-08-07-ideal-teile-1-5`, Commits `98ea06fa` (A), `6144f36d` (B),
Doku `4ec9155d`. **Keine Migration, keine neue Einstellung** (wirkt mit `FaHierarchyKommissionierlistenAktiv`). TS-209. Paket 27. Nicht gemergt.

## Kernpunkte

- Umschalter **Stückliste | Liste** im Kommissionierauftrag (IDEAL, Kommissionierlisten an, Quelle FA-Struktur); Liste ist Standard.
- Liste summiert je Artikel und Komm.-Ziel (`KommissionierauftragListe`), BG getrennt, Rest-Hinweis (K4b), Zeilen ohne Stücklistenposition (K4c),
  eigene Spaltenauswahl `KommissionierauftragListe`; summierter Druck/PDF (`PrintAuftragListe`, POST mit sichtbaren Spalten).
- Verteil-Buchung: `PickingOrderVerteilung.Verteile` (natürliche Positionsreihenfolge, je Position bis Soll), `PickingItemIds`-Expansion in
  `TransferOrderLinesAsync`, alles oder nichts, `DbUpdateConcurrencyException` → „Auftrag wurde inzwischen geändert …“ (K12), Knopf-Sperre
  `window.wmsSperren` in beiden Masken. Sammel-Bestellknopf in der Liste ausgeblendet.

## Deploy-Risiken

- Nur Web (Dienst nur `AppVersion`). Buchung = Umbuchungen Lager → Wagen, kein Sage-Queue-Eintrag.
- K12 ohne automatisierten Test (InMemory ohne RowVersion) → TS-209.13 am Testsystem.
- QA-Testdaten DEV IDEAL: `QA-LAGER-209` (Id 232), `QA-WAGEN-209` (Id 233), gebuchte Zeilen in KO-2026-00004.
- QA-Hinweise R-1 bis R-5 → [[2026-11-26-kommissionierauftrag-liste-nachbesserungen]].
