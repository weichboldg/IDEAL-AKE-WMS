---
type: changelog
version: 1.97.0
date: 2026-11-11
---
# v1.97.0 — Auftrag drucken in Sortierreihenfolge

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commit `9db618c0`. Auftrag Gerald 2026-11-11 (aus QA v1.96.0 H-4), Nachtrag in
[[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]]. Nicht gemergt, nicht gepusht.

## Kernpunkte

- „Auftrag drucken“ auf der Auftragsseite: bei aktiver Sortierung POST mit der Positionsliste in Anzeige-Reihenfolge (Muster Stücklisten-Druck
  v1.30.3); ohne Sortierung GET wie bisher nach Position. Gedruckt werden immer alle Zeilen des Auftrags, Spaltenfilter wirken nicht.
- `PickingController.SortiereZeilen` (Rest ohne Listeneintrag am Ende nach Position), Test `AuftragDrucken_SortiereZeilen_NachAnsicht`, TS-120.16.

## Deploy-Risiken

Nur Web fachlich (Dienst nur Version). Keine Migration. DEV: nach `dev-deploy.ps1` die 5 AKE-DEV-Sync-Schalter wieder setzen (2026-11-11 erledigt).

## QA

qa-agent 2026-11-11 Testbereit, keine Befunde. Hinweise: H-1 Positionen mit Rand-Leerzeichen landen am Ende (kein Fall auf DEV); H-2 bei
eingeklappter Baumansicht druckt der sortierte Auftrag Kindzeilen flach nach ihrem eigenen Sortierwert (wie der Stücklisten-Druck) – Bewertung
Gerald. TS-120.16 auf IDEAL nicht möglich (keine Kommissionieraufträge in IDEAL_WMS_DEV).
