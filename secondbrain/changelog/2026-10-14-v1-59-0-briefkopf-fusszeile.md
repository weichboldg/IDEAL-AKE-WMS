---
type: changelog
version: 1.59.0
date: 2026-10-14
---
# v1.59.0 — Briefkopf: Fußzeile nach Firmen-Word-Vorlage

[[2026-10-13-briefkopf-fusszeile-word-vorlage-spec]]. **Direkt im Bündel** `feature/2026-08-07-ideal-teile-1-5`, Start
`ead16fc4`, Commits `c2d54f35`, QA-Fix `d4108f1a`. **Keine Migration.** Umsetzung [[2026-10-14-briefkopf-fusszeile-word-vorlage-umsetzung]].

**Warum:** Die Word-Vorlage der Firma trägt Anschrift, Kontakt und Balken unten auf jeder Seite; der Briefkopf aus Teil 2
konnte nur einen Kopf auf Seite 1.

## Kernpunkte

- `BriefkopfVorlageService` löst `<footer class="briefkopf-fuss">` heraus (`Fuss`, `HatFuss`); fehlendes `</footer>` oder
  `div`-Unwucht im Fuß → Ersatz-Kopf; mehrere Bereiche → erster + Hinweis; gemeinsame Einbettungsgrenzen.
- Neuer Partial `_PrintBriefkopfFuss` einmal je Druckseite hinter den Dokumenten (Beschichtung Liste/Summiert,
  Auftragsdruck; PDFs laufen über dieselben Views). Druckregeln nur mit Fußbereich: `@page` 10/10/5/10 mm, Fußzeile
  `fixed` ≤ 17,75 mm, Polster 19,75 mm je `.fa-group`-Seite (Bauweise V3, [[fallstricke]] §27).
- „Seite X von Y" nur bei genau einem Dokument je Druck (P3 positiv).
- Muster `Branding/briefkopf.html`: Logo rechts, `NimbusSanNov-*.otf`, Fußzeile IDEAL | AKE | Web + Balken, Ablage
  `Branding\` im Programmordner. AKE Blau als Schrift nur für den AKE-Firmennamen (Freigabe 2026-10-14).
- Tests Web 2014 (+14), Service 272. TS-92 neu, TS-82/TS-84.8 nachgezogen.

## Deploy-Risiken

Nur Web (Service nur Versionsanzeige). Ohne Fußbereich in der Server-Vorlage druckt alles wie bisher — Reihenfolge
Deploy/Vorlage beliebig. **Ablage korrigiert:** `Branding\` liegt **im** Programmordner (nicht `wwwroot\`); nach jedem
Deploy prüfen, dass er noch da ist (TS-92.11). Edge-Version am Server notieren (TS-92.13).
