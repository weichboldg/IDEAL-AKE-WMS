---
type: backlog
title: Lagerplatz ausbuchen — FA-Spalte in der Bestandstabelle + funktionsfähige FA-Ausbuchung
status: neu
created: 2026-08-06
herkunft: "Offenes Thema beim Merge von WMS Bugs & Improvements (2026-08-06); entspricht dem zurückgestellten [[2026-08-05-wms-bugs-improvements-teil-6-spec]]"
---

# Lagerplatz ausbuchen — FA-Spalte + FA-Ausbuchung

Beim Merge der WMS-Bugs-&-Improvements-Reihe (v1.29.0 + v1.30.0) bewusst **nicht** mitgemergt:
**Teil 6** war zurückgestellt (Freigabe-Antworten offen). Dieses Thema bleibt offen und wird hier
als Backlog-Aufgabe festgehalten (Auftrag des Menschen beim Merge).

## Worum es geht
Auf der Seite **„Lagerplatz ausbuchen"** (`/StockMovements/OutboundAll`):
1. **FA-Spalte** in der Bestandstabelle anzeigen (welcher Fertigungsauftrag steckt hinter dem
   Bestand je Artikel/Lagerplatz).
2. **FA-Ausbuchung aktivieren:** das vorhandene FA-Textfeld wirkt heute **nicht** als Filter —
   `OutboundAllConfirm` bucht immer den **gesamten** Lagerplatz-Bestand aus, unabhängig vom
   eingetragenen FA-Wert. Das FA-Feld soll tatsächlich die auszubuchenden Artikel eingrenzen.

## Offene Kern-Entscheidung (VOR Umsetzung klären — Schranke 1)
- **Variante A** (Textfeld als echter Filter): nur die zur eingegebenen FA gehörenden Artikel
  ausbuchen.
- **Variante B** (Checkbox/Auswahl je Zeile): Anwender wählt manuell die auszubuchenden Zeilen.
Ohne diese Wahl ist der Fix nicht eindeutig spezifizierbar. Weitere offene Punkte (mehrdeutige FA
je Zeile, Konsistenz mit dem Ist-Bestand-Verhalten aus Teil 1) siehe Detail-Spec.

## Referenzen
- Detail-Entwurf (zurückgestellt, alle Code-Referenzen + offene Fragen):
  [[2026-08-05-wms-bugs-improvements-teil-6-spec]] (liegt wieder in `specs/entwurf/`).
- Cross-Feature-Merkposten: `OutboundAll` erzeugt `Ausbuchung`en über
  `IStockMovementRepository.AddAsync` — seit v1.28.0 hängt daran der Sage-Lagerbuchungs-Enqueue.
  Ändert die FA-Filterung die Menge der ausgebuchten Zeilen, ändert sich die Menge der
  Sage-Meldungen (Repository-Pfad nicht umgehen).

**Nächster Schritt:** Kern-Entscheidung (A vs. B) durch den Menschen, dann normale
`/spec` → `/review` → `/dev`-Kette über die Teil-6-Spec.
