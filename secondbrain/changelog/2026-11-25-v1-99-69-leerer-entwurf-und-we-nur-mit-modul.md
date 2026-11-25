---
type: changelog
version: 1.99.69
date: 2026-11-25
---
# v1.99.69 — Leerer Beschichtungsauftrag-Entwurf speicherbar, Sage-Buchungen WE nur mit Modul

Freigabe-Nachtrag 2 zu [[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]] (QA-Befund N-1) und Nachtrag B3 zu
[[2026-11-23-sage-buchungen-gesamtsicht-spec]], Lauf [[2026-11-25-goal-lauf-beschichtung-sage-buchungen]]. Commit `eff16911`, QA in beiden Specs
(2026-11-25). Kein SQL. TS-162.30, TS-189.10 geändert. Nicht gemergt.

## Kernpunkte

- N-1: `BeschichtungsauftraegeController.Bearbeiten` bindet `mengen` mit `[Bind(Prefix = "mengen")]`. Ohne `mengen[..]`-Felder (Entwurf mit 0 Positionen)
  fiel der `Dictionary<int,…>`-Binder auf das leere Präfix zurück, las `id`, `rowVersion`, `ziel` als int-Schlüssel und setzte einen ModelState-Fehler
  unter `""` → „Eingabe nicht gespeichert: ungültiger Wert in .“. Beleg `DictionaryIntKeyBindingTests`. Siehe [[fallstricke]] §64.
- B3: `SageBookingQueueController.Index` `zeigtWe = weModul && (stockRead || Einkauf)` – Modul „Wareneingang zu Bestellung“ aus = keine WE-Zeilen,
  auch für den Einkauf.

## Migration/SQL

Keine.

## Deploy

Web. Dienst nur wegen `AppVersion.cs`. Kein Manifest-Eintrag. Zusammen mit v1.99.68 aus demselben Paket (`publish\` v1.99.69, bringt SQL/167 mit).

## Risiken

- Gleiches Binder-Muster latent in `StockMovementsController.OutboundAllConfirm` (`stueckGezeigt`) – nur gemeldet, nicht geändert.
- B3-H1: Einkauf-Menüeintrag „Sage-Wareneingänge (offen)“ bleibt bei Modul aus sichtbar und führt in eine leere Liste – Entscheid Gerald.

## Commit / Merge

`eff16911` im Bündel. **Nicht gemergt** (wartet Schranke 2).
