---
type: backlog
typ: feature
created: 2026-11-23
status: in-spec
spec: "[[2026-11-23-tabellen-streifen-hover-ci-hellblau-spec]]"
quelle: Gerald 2026-11-23 („Das mit den mehrfarbigen Zeilen bzw. Hover könnten wir in allen Tabellen einbauen“)
modul: Oberfläche übergreifend (Tabellen, site.css)
---
# Alle Tabellen: Streifen, Hover und Fokus-Zeile in CI-Blau wie im Wareneingang

## Anliegen

Was im Wareneingang zu Bestellung ([[2026-11-23-v1-99-59-wareneingang-hover-hellblau]]) gebaut wurde, soll in **allen Tabellen**
der Anwendung gelten:

- **Abwechselnd hinterlegte Zeilen** (Streifen, wie `table-striped` im Wareneingang seit v1.99.52).
- **Hover** (Zeile unter der Maus) in CI-Hellblau `--ake-light-blue` (#A1D9F7) statt Bootstrap-Grau – Grau war am Terminal kaum sichtbar.
- Wo eine Zeile Eingabefelder hat: **Fokus-Zeile** sehr hellblau `--ake-very-light-blue` (#D9F4FF) mit dunkelblauem Balken links.
- **Links** in Tabellenzeilen dunkelblau (`--bs-primary-text-emphasis`), damit sie auf Hellblau lesbar bleiben (QA-Befund B-1 v1.99.59: #0D6EFD auf #A1D9F7 nur 2,95:1).

## Vorbild im Code

- `IdealAkeWms/wwwroot/css/site.css` – Regeln `.we-positionen` (v1.99.52 Fokus-Zeile, v1.99.59 Hover/Fokus/Links).
- Wächter `ViewKeyDriftGuardTests.WareneingangPositionen_DreiQuellenGleich_Gesperrt_FokusZeile`.
- TS-177, TS-184.

## Zu klären in der Spec

- Global (alle `.table`) oder per Opt-in-Klasse? Ausnahmen: Druckansichten, Tabellen mit eigener Zeilenfärbung (Status-/Ampelfarben, `table-danger`/`table-warning`/`table-success`, Leitstand, FA-Struktur-Baum, BDE-Terminal mit eigener Gestaltung), sehr kleine Tabellen in Karten/Modals.
- Zusammenspiel mit vorhandenem `table-hover`/`table-striped` je View, mit klickbaren Zeilen und mit ausgewählten/markierten Zeilen.
- Kontrast WCAG AA für Text, `text-muted`, Links und Badges auf allen Hintergründen; Farbe nie allein Bedeutungsträger.
- Drucken/PDF darf keine Farbflächen bekommen.
- Nachweis: ein zentraler Wächter-Test statt je View.
