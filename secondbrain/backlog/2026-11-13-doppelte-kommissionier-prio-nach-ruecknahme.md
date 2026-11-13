---
typ: backlog
status: offen
created: 2026-11-13
herkunft: "[[2026-11-12-befunde-ake-regression-dev-spec]]"
standort: beide
---
# Doppelte Kommissionier-Prio nach „Freigabe zurücknehmen“

Abgespalten aus [[2026-11-12-befunde-ake-regression-dev-spec]] (Freigabe-Antwort 5, Gerald 2026-11-13: eigener Backlog-Eintrag).

**Befund (Spec-Agent, Bündel v1.99.12, nicht neu gemessen):** Eine neue Prio wird als `MAX + 1` über freigegebene, nicht erledigte,
nicht stornierte HauptFAs mit Prio vergeben (`Controllers/PickingLeitstandController.cs:396-401` →
`Data/Repositories/ProductionOrderPickingStatusRepository.cs:211-222`; Sammelfreigabe `:242-244`, `:270`). Beim Zurücknehmen der
Freigabe bleibt die Prio stehen (`SetReleaseAsync` `:76-86`, Sammel `:278-282`), zählt aber nicht mehr ins MAX, weil der FA nicht
mehr freigegeben ist. Wird er wieder freigegeben, behält er seine alte Prio, während inzwischen ein anderer FA dieselbe Zahl bekommen
haben kann. Im DEV-Lauf 2026-11-12: 2614167 und 2614168 beide 32.

**Zu klären:** Soll die Prio beim Zurücknehmen gelöscht werden, soll MAX auch zurückgenommene FAs mit Prio zählen, oder sind doppelte
Prios fachlich in Ordnung (Sortierung dann zusätzlich nach Freigabezeitpunkt)?
