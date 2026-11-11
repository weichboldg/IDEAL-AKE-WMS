---
type: changelog
version: 1.56.0
date: 2026-10-13
---
# v1.56.0 — Menü „Beschichtung“ gebündelt + Kurzanleitungen in der Hilfe

[[2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen-spec]] (Antworten 1–4 + Nachtrag). **Direkt im Bündel**
`feature/2026-08-07-ideal-teile-1-5`, Start `627c992a` (v1.55.0), Commits `14d1600d`, `699586af`, QA-Fix `d3746373`. **Keine Migration.**
Umsetzung [[2026-10-13-menue-beschichtung-und-hilfe-kurzanleitungen-umsetzung]].

**Warum:** Seit v1.51.0 gab es zwei fast gleich benannte Top-Level-Menüpunkte („Beschichtungsauftrag“ = Vorschlagsliste,
„Beschichtungsaufträge“ = gespeicherte Aufträge) — verwechselbar. Die Hilfe bestand aus Versionsnotizen statt Anleitungen.

## Kernpunkte

- `_Layout.cshtml`: ein Dropdown „Beschichtung“ (Vorschlagsliste → `FaHierarchyBeschichtung/Summiert`, Aufträge →
  `Beschichtungsauftraege/Index`), gleiche Bedingung wie vorher, Active für beide Controller.
- Titel der Vorschlagslisten-Views „Vorschlagsliste Beschichtung (IDEAL)“; Druck/PDF unverändert „Beschichtungsauftrag …“.
- Hilfe: neue Karte „Beschichtung (IDEAL)“ (führt v1.48/v1.49/v1.51 zusammen), Kurzanleitungen für Kürzel im FA-Baum und
  CSV-Export. Quelltext-Wächter in `BeschichtungsauftraegeViewTests` (+5).
- TS-89; Menüweg in TS-81.2/81.13/84.14 nachgezogen. Tests Web 1831, Service 272.

## Deploy-Risiken

Web (Service nur Versionsanzeige, optional), keine Rollen-/Einstellungsschritte. Benutzer finden die Beschichtung jetzt unter *Beschichtung* — kurz ankündigen.

Merge-Commit: — (Bündel, nicht gemergt)
