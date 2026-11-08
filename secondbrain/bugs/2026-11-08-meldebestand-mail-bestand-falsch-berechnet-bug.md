---
type: bug
title: Meldebestand-Mail rechnet Bestand falsch (Umbuchung regulär→regulär als Zugang, Artikel ohne Bewegung fehlen)
status: offen
severity: mittel
created: 2026-11-08
affected_code:
  - "IDEALAKEWMSService/Services/StockCheckService.cs:38-46 (CASE, erster Treffer gewinnt)"
  - "IDEALAKEWMSService/Services/StockCheckService.cs:58 (INNER JOIN StockMovements)"
  - "IDEALAKEWMSService/Services/StockCheckService.cs:63-71 (HAVING, gleiche Logik)"
source_backlog: "[[2026-11-08-meldebestand-mail-bestand-falsch-berechnet]]"
spec: "[[2026-11-08-meldebestand-mail-bestand-falsch-berechnet-spec]]"
---

## Symptom
Die Meldebestand-Mail (`NotificationWorker`) meldet zu hohe Bestände und kann deshalb ausbleiben; Artikel mit Meldebestand, aber ohne eine einzige Lagerbewegung, erscheinen nie.

## Reproduktion
Nachgestellt am 2026-11-08 auf DEV (`.` / `AKE_WMS_DEV`, nur lesend). 24 Umbuchungen regulär→regulär, z. B. Artikel 00725321: Service 355, korrekt 354. 9 Artikel mit `ReorderLevel > 0` haben keine Bewegung und fehlen ganz. Details in der Spec, Abschnitt „Nachgestellt“.

## Root Cause
- `CASE` bricht beim ersten Treffer ab: Bei `MovementType = 2` mit regulärem Ziel greift `:41` (+Qty), `:42` (-Qty Quelle) wird nie erreicht. Netto wäre 0. Dasselbe im `HAVING` `:66-67`.
- `INNER JOIN [StockMovements]` `:58` wirft Artikel ohne Bewegung heraus.

## Fix / Verweis
[[2026-11-08-meldebestand-mail-bestand-falsch-berechnet-spec]]
