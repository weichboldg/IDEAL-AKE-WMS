---
typ: notiz
spec: "[[2026-10-14-ideal-sage-views-umstellung-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: IDEAL Sage-Zugriffe auf die neuen WMS-Views (v1.60.0)

Spec [[2026-10-14-ideal-sage-views-umstellung-spec]] · **direkt im Bündel**, Start `d4108f1a`. Version v1.59.0 → v1.60.0, TS-93.

## Plan

1. Gemeinsamer Existenz-Helfer (`OBJECT_ID`), FA-Defaults/Fallback + Existenzprüfung (Fehler + Mail).
2. Arbeitsplätze: Key `Sync:ProductionWorkplaceSyncViewName` (Default `KHKPpsArbeitsplaetze`), Reader mit Quelle + Existenz, Skip ohne Mail.
3. Artikel: Key `Sync:ArticlesViewName` (leer = alter Select zeichengleich), reine SQL-Auswahlfunktion, Skip ohne Mail.
4. Katalog, Drift-Guard, Standort-Maske; Tests.
5. Einmal-Skript `SQL/Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql` (nur anlegen).
6. Doku: README, Changelog, AppVersion, TS-93, fallstricke §16, services/integrationen.

## Stand

- 2026-10-15: Start `d4108f1a`, Commits `b53b99dd` (Service, Katalog, Tests), `2524bb03` (Einmal-Skript, Doku, v1.60.0). Build 0 Fehler, Web 2016 / Service 281 gruen. Changelog [[2026-10-15-v1-60-0-ideal-sage-views-umstellung]], [[fallstricke]] §16, TS-93.
- 2026-10-15: QA gruen → **Testbereit**, keine Code-Aenderung durch QA, Minor M-1..M-7 in der Spec.
- 2026-10-15: **Nachbesserung v1.60.1** `63524941`: Artikel-View liefert WMS-Spaltennamen (P4), `BuildViewSql` umgestellt, Hash-Waechter fuer LegacySql, TS-93.4b. Changelog [[2026-10-15-v1-60-1-artikel-view-wms-spalten]].
- Offen fuer die Abnahme (Gerald): Pruefabfragen P1–P5b auf Sage (Test + live), P6/P7 auf der WMS-DB, Umstellung per Einstellung, Einmal-Skript nur IDEAL (Liste bestaetigen, dann loeschen).
