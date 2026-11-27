---
type: changelog
version: 1.99.17
date: 2026-11-13
---
# v1.99.17 — Kommissionier-Prio: zurückgenommene Prio zählt ins MAX, keine Doppelten bei Freigabe

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commit `73911686`. **Testbereit** (QA 2026-11-13, DEV AKE+IDEAL). Spec [[2026-11-13-doppelte-kommissionier-prio-nach-ruecknahme-spec]]
(Variante B). Nicht gemergt, nicht gepusht; /goal-Lauf [[2026-11-13-buendel-v1-99-16-goal-lauf]].

## Kernpunkte

- `ProductionOrderPickingStatusRepository.GetMaxPickingPriorityAsync` zählt offene HauptFAs mit Prio unabhängig von `IsReleasedForPicking`
  (eine Stelle, wirkt für Einzel- und Sammelfreigabe). Zurückgenommene FAs behalten ihre Prio; neue Freigaben bekommen eine Zahl darüber.
- Manuelle Doppelvergabe im Leitstand bleibt erlaubt (Freigabe-Antwort 3); Race bei gleichzeitiger Freigabe bleibt als Grenze.
- Hilfeseite „Priorisierung“/„Freigabe zurücknehmen“ neu gefasst (u. a.: Aufträge mit Fortschritt bleiben in `/Picking`).
- **Altbestand:** `Pruefung/DoppeltePrio` (Nachlauf, Firma beide, Phase Nachher, nur anzeigen); Korrektur per Hand. DEV (lesend, 2026-11-13):
  AKE_WMS_DEV 4 Prio-Werte mit Doppelten (1, 13, 17, 32), IDEAL_WMS_DEV 0. **Prod-Trefferzahlen liefert Gerald.**

## Migration / SQL

Keine.

## Deploy-Risiken

Nur Web fachlich (Dienst nur wegen der Version). Nach dem Update zeigt das Skript die Doppelten-Zahl – kein Abbruch.
