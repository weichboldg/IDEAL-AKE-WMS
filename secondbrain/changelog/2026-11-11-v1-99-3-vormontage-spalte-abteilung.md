---
type: changelog
version: 1.99.3
date: 2026-11-11
---
# v1.99.3 — Vormontage-Listen (IDEAL): Spalte „Abteilung“ je HauptFA

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commit `d2d4ed46`. Spec [[2026-11-11-vormontage-spalte-abteilung-hauptfa-spec]]. Nicht gemergt,
nicht gepusht.

## Kernpunkte

- Neue Spalte „Abteilung“ (`montage-abteilung`, Quelle `FaHierarchyOrderInfo.MontageAbteilung` wie im Gruppenkopf) in „Einzelne Teile“ und
  „Summiert“, direkt nach „Arbeitsbereich“, Zahnrad, Server-Spaltenfilter.
- Kombigeraet (mehrere Abteilungen je HauptFA) zeigt „Kombigerät“; Summiert listet die Abteilungen der beitragenden HauptFA sortiert.

## Deploy-Risiken

Keine Migration, keine Einstellung. Nur Web (Dienst nur Versionsnummer).
