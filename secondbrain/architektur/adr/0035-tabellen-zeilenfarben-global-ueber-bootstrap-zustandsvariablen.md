---
type: adr
id: 0035
title: Tabellen-Zeilenfarben global über Bootstrap-Zustandsvariablen mit Ausnahmeklasse table-plain
status: accepted
date: 2026-11-24
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-23-tabellen-streifen-hover-ci-hellblau-spec]] (v1.99.62, TS-187, keine Migration), Freigabe Gerald 2026-11-23 Variante A.
> Baut auf Fallstrick 61 auf. Bündel `feature/2026-08-07-ideal-teile-1-5`, Commit `7fbdd2b7`.

## Kontext und Problem

- Streifen/Hover waren je View per `table-striped`/`table-hover` geregelt (26 Tabellen ungestreift, Hover Bootstrap-Grau, am Terminal kaum sichtbar).
- Die globale Regel `.table tbody tr:hover { background-color }` war wirkungslos: Bootstrap 5.3 malt jede Zelle deckend, der `tr`-Grund liegt darunter.
- Im Wareneingang gab es seit v1.99.52/59 eine eigene Fokus-Zeile + CI-Hover nur für diese Tabelle (`.we-positionen`).

## Entscheidung

- **Global auf `.table`** in `site.css`: Streifen (`--bs-table-bg-type`), Hover CI-Hellblau (`--bs-table-hover-bg` = `--ake-light-blue`, ohne
  `@media (hover: hover)`), Fokus-Zeile über eigene Variable `--wms-table-focus-bg` (`--ake-very-light-blue`) + 4-px-Balken, ausgelöst per `:has()`
  nur von Wertfeldern (Kästchen nur `:focus-visible`, Links/Knöpfe nie). Links, Farbtexte (`text-*` → `-text-emphasis`) und Vault/enaio-Symbole in
  `tbody` dunkler. `@media print` setzt Streifen/Zustand zurück.
- **Nur Zustandsvariablen, nie `background` am `td`**; `--bs-table-active-bg` wird nie umgesetzt (Gruppenköpfe `tr.table-active`).
  Kontextklassen am `tr` behalten ihre eigenen Varianten; `--wms-table-focus-bg` fällt dort auf ihren Aktiv-Ton zurück.
- **Ausnahme `table-plain`** (Opt-out) für Tabellen mit eigener Zellfärbung: `#faTree`, `fa-head-table`, `#trackingTree`, `#oseonTree`, Artikelinfo-Schlüssel-Wert.
- **Wächter** `TabellenZeilenfarbeTests`: Regeln nur mit Variablen, `--bs-table-active-bg` unberührt, keine Kontextklasse am `<table>`, Ausnahmeliste exakt.

## Folgen

- Neue Tabellen bekommen das Verhalten automatisch; wer es nicht will, braucht `table-plain` **und** eine bewusste Teständerung.
- `table-striped`/`table-hover` im Markup sind redundant, aber harmlos (kein 73-Dateien-Diff).
- Ohne `:has()` (Browser vor Chrome/Edge 105, Firefox 121) fehlt nur die Fokus-Zeile.
- Abgelehnt: Opt-in-Klasse je View (Variante B) – ~95 Markup-Änderungen und Drift bei jeder neuen View.
