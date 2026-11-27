---
type: backlog
typ: feature
status: in-spec
spec: "[[2026-11-23-sage-buchungen-gesamtsicht-spec]]"
created: 2026-11-23
quelle: Gerald, Chat 2026-11-23 („können wir jede Sage-Buchung in der Maske Sage-Buchung anzeigen? Nicht nur die von Wareneingang?“)
modul: Sage-Übergabe / Monitoring
---
# Sage-Buchungen: eine Gesamtsicht über alle Übergabewege

Wortlaut: *Können wir jede Sage-Buchung in der Maske „Sage-Buchung“ anzeigen, nicht nur die vom Wareneingang?*

## Ist-Stand (geprüft 2026-11-23, Bündel-Worktree `2026-08-07-ideal-teile-1-5`)

Das WMS schreibt auf **drei getrennten Wegen** an Sage. Jeder Weg hat eine eigene Ablage und eine eigene Anzeige (oder gar keine):

| Weg | Was | Ablage | Anzeige heute |
|---|---|---|---|
| Lagerbuchung | Ein-/Ausbuchung, Umbuchung (nur mit Schalter), ab v1.99.53 auch Lagerbestellungs-Entnahme | `SageBookingQueueItems` (Decorator `SageBookingEnqueueingStockMovementRepository.AddAsync`) → `SageBookingWorker` → `SageLagerbuchungClient` | Menü „Sage-Lagerbuchungen“ (`SageBookingQueueController.Index`) – **alle** Queue-Einträge, kein Filter nach Herkunft (`SageBookingQueueRepository.GetForMonitoringAsync`) |
| Sage-Wareneingang (WE-Beleg) | Wareneingang zu Bestellung mit Sage-WE | `PurchaseOrderReceipts.SageWeStatus` → `SageWareneingangService` → `SageWareneingangClient` | eigene Seite `SageWareneingaenge` (`PurchaseOrdersController.SageWe.cs:61`, `EinkaufPurchaseOrdersController.cs:29`) |
| FA-Arbeitsgänge (IDEAL) | Arbeitsgänge aus dem WMS im Sage-FA anlegen | `SageFaArbeitsgangService` → `SageFaArbeitsgangClient` | keine eigene Liste gefunden – in der Spec prüfen |

Wichtig: Eine WE-Buchung mit Sage-WE-Beleg kommt **bewusst nicht** in die Lagerbuchungs-Queue (`SageBookingEnqueueDecision.ShouldEnqueue`: `!hatSageWareneingang`), sonst wäre sie in Sage doppelt. „Sage-Lagerbuchungen“ zeigt also schon jede Lagerbuchung, aber genau die Wareneingänge mit Sage-WE fehlen dort.

## Für die Spec klären

- Was meint „Maske Sage-Buchung“: die Seite „Sage-Lagerbuchungen“ oder „Sage-Wareneingänge“? Rückfrage an Gerald.
- Variante A: In „Sage-Lagerbuchungen“ eine Spalte „Herkunft“ (Einbuchung, Ausbuchung, Umbuchung, Lagerbestellung, Kommissionierung …) und **nur lesend** Zeilen aus `PurchaseOrderReceipts` und aus den FA-Arbeitsgängen dazu, mit Link auf die jeweilige Fachseite (Erneut senden / Klären bleibt dort).
- Variante B: Eine eigene Übersichtsseite „Sage-Übergaben“ als Union der drei Quellen; die bestehenden Seiten bleiben, wie sie sind.
- Statuswerte vereinheitlichen? (Queue: Offen/Gesendet/Bestätigt/Fehler; WE: eigene `SageWareneingangStatus`) – nur zuordnen, nicht umbauen.
- Paging und Spaltenfilter über die Union serverseitig (ADR 0005); Rollen: wer sieht welche Quelle?
- Fehlt ein Sage-Schreibweg in dieser Tabelle? In der Spec vollständig belegen (alle SData-POST-Stellen im Dienst).
