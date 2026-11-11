---
type: changelog
version: 1.75.0
date: 2026-11-07
---
# v1.75.0 — Fehlteile „Nur meine Werkbänke“: Trefferzahl und Badges über alle Seiten

Spec [[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec]] (Bug [[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-bug]]),
Lauf [[2026-11-07-buendel-2026-11-07-goal-lauf]], Bündel [[2026-11-07-buendel-uebersicht]]. Commit `e3071344`. **Keine Migration**, nur Web
(Dienst nur Version). TS-108. Version 1.74.0 entfällt im Bündel (OSEON-Hotfix aus `main`).

## Kernpunkte

- `GetMissingPartsAsync` nimmt eine Werkbank-**Menge** (`IReadOnlyCollection<int>?`, null = kein Filter, leer = keine Treffer) und filtert in SQL
  vor Count/Paging (ADR 0005). Vorher: eine Werkbank, bei mehreren Nachfilter der Seite im Speicher + `total` = Zeilen der Seite.
- Liste und beide Tab-Badges nutzen dieselbe Menge; Badges zählen bei „Nur meine Werkbänke“ nur die eigenen (Gerald 2026-11-07).
- `MissingPartsLagerController` nur auf die Signatur umgestellt, Verhalten gleich.

## Deploy-Risiken

- Keine. Verhaltensänderung sichtbar: Badge-Zahlen im Modus „Nur meine Werkbänke“ werden kleiner.
