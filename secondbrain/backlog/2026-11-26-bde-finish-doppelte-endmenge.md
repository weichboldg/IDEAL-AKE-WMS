---
typ: bugfix
status: offen
created: 2026-11-26
herkunft: "Fix-Lauf v1.99.81 (Task 3), Nebenfund beim Lesen von BdeBookingService"
standort: beide
---
# BDE: Fertig an Terminal/Gruppe prüft keine vorhandene End-Menge (doppelte `IsFinal`)

Bezug: [[2026-11-26-fix-lauf-v1-99-81]], ADR 0037 (Teilmengen).

- `BdeBookingService.ReportCompletionCore` weist eine zweite End-Menge auf demselben Arbeitsgang ab (`BdeBookingQuantities.AnyAsync(q => q.IsFinal …)`,
  `IdealAkeWms/Services/BdeBookingService.cs:293`).
- `FinishAsync` (`:330`, schreibt über `FinishInternalAsync` `IsFinal = true`, `:482`) und `FinishGroupAsync` (`:382`, `q.IsFinal = entry.IsFinal`, `:419`)
  prüfen das nicht. Zwei Werker, die auf demselben AG parallel Produktion laufen haben (Mehrfachbuchung), können so zwei End-Mengen schreiben.
- Zu klären: gewollt (je Buchung eine End-Menge, Summe zählt) oder Fehler (je AG genau eine)? Prüfabfrage DEV/Prod:
  `SELECT b.WorkOperationId, COUNT(*) FROM BdeBookingQuantities q JOIN BdeBookings b ON b.Id=q.BdeBookingId WHERE q.IsFinal=1 AND b.IsCancelled=0 GROUP BY b.WorkOperationId HAVING COUNT(*)>1`.
- Nicht in v1.99.81 gebaut (Umfang).
