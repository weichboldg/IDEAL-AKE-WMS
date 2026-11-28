---
type: changelog
version: 1.99.93
date: 2026-11-28
---
# v1.99.93 — Update-Skript: Neustart nach Abbruch, Transaktionsschutz, Marker-Lokalzeit, Lieferverzug-Hinweis

[[2026-11-28-update-skript-review-befunde-spec]] (UP-1..UP-4), Lauf-Notiz [[2026-11-28-buendel-review-kommissionierung-goal-lauf]]. Worktree `feature/2026-11-28-buendel-review-kommissionierung`, Commit `ff681409`, QA-Fix `a83d8756` (nach Server-COMMIT: `@@TRANCOUNT 0`, Rollback wirft nicht → „ggf. festgeschrieben“ bei `@@TRANCOUNT 0`). Paket 28.
**Keine Migration, kein SQL** (176 bleibt frei). TS-213, überholt TS-126.22. Nicht gemergt.

## Kernpunkte

- UP-1: `deploy-info.txt` erst nach Schritt 7; Schritt 1 nimmt die kleinere Version aus Fußzeile und `deploy-info.txt`; Daten-Einträge wie
  Einmalig immer fällig (Marker/Spur entscheiden) — `SQL/110` macht je System einmal einen Trockenlauf bis „entfällt“.
- UP-2: `Test-SqlFuerTransaktion` lehnt `USE` und unpaariges `COMMIT` ab; `Invoke-SqlTransaktion` prüft nach jedem Batch
  `@@TRANCOUNT = 1` und `DB_NAME()`; „ggf. festgeschrieben“ bei `@@TRANCOUNT 0` oder gescheitertem Rollback nach verletzter Transaktion (nicht bei XACT_ABORT).
- UP-3: `DeployScriptRuns.ExecutedAt` und `Set-Startkennwort` `ModifiedAt` = `Get-Date` (ADR [[0025-zeitstempel-in-lokalzeit-des-schreibenden-rechners]]).
- UP-4: `Pruefung/LieferverzugSammellisteTage` entfällt; `v1.99.93-LieferverzugSammellisteTage` (Einstellung, AKE) zeigt den Wert; Text
  `v1.99.19-LieferverzugSchwellen` „AKE = 1, IDEAL Standard 3“.

## Deploy-Risiken

- Die neue Logik wirkt erst ab dem Lauf **mit** diesem Paket (Skript reist im Paket). Erster Lauf je System zeigt Daten-Einträge, die vorher „nicht fällig“ waren, als offen (Trockenlauf).
- Altzeilen `DeployScriptRuns` bleiben in SQL-Server-Zeit.