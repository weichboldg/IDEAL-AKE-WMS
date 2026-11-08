---
type: changelog
version: 1.93.0
date: 2026-11-08
---
# v1.93.0 — Korrigiert: Meldebestand-Mail

Bündel `feature/2026-08-07-ideal-teile-1-5`, Lauf [[2026-11-08-nachbesserungen-vor-buendel-merge-goal-lauf]]. Spec [[2026-11-08-meldebestand-mail-bestand-falsch-berechnet-spec]].
Commits `897f7a03`. Nicht gemergt (Schranke 2).

## Kernpunkte

- `StockCheckService` nutzt `IStockMovementRepository.GetStockByArticleNumbersAsync` (Regel der Web-App) statt eigenes SQL; Artikel ohne Bewegung mit Bestand 0; „Lager:“ nur Plätze mit Bestand.

## Deploy-Risiken

**Service** neu deployen (Web nur Version). `Notifications:MeldebestandEnabled` ist Default an – die nächste Mail nach dem Deploy enthält die korrigierte Liste, ggf. mehr Zeilen (Artikel ohne Bewegung; Zählabfrage in der Spec je Standort vor PROD).
