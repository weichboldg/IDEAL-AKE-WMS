---
type: changelog
version: 1.99.46
date: 2026-11-20
---
# v1.99.46 — Startseite: Kacheln wieder mit Symbolen (Bugfix)

Bündel `feature/2026-08-07-ideal-teile-1-5`, Basis `c39a9423` (v1.99.45). Spec [[2026-11-20-bugfix-dashboard-kacheln-symbole-fehlen-spec]],
Bug [[2026-11-20-dashboard-kacheln-symbole-fehlen-bug]], Aufgabe [[2026-11-20-dev-dashboard-kacheln-symbole]]. Commit `6e91fef2`.
Kein SQL, keine Migration, keine Einstellung, keine Manifest-Einträge. TS-174. Nicht gemergt.

## Kernpunkte

- `NavZiel` bekommt `SymbolPfad` (ADR [[0034-navigation-menue-und-kacheln-aus-einer-quelle]] bleibt: eine Quelle). 46 Kacheln: 9 alte SVG-Pfade
  wörtlich aus `f1747e10` (Kommissionierung, FA-Liste, Artikelinfo, Leitstand, OSEON, Terminal, Cockpit, Buchungen, AG-Übersicht), übrige aus
  Inline-SVGs im Projekt, Plus/Minus/Doppelpfeil selbst gezeichnet (Freigabe Gerald 2026-11-20).
- Startseite rendert ein 32×32-Inline-SVG `aria-hidden="true"`, `focusable="false"`, Farbe `--ake-primary` über die bestehende Regel
  `.dashboard-card .card-text`. Kein neues CSS, keine Bibliothek, kein `Html.Raw`.
- Wächter `NavigationSichtTests.JedeKachel_HatEinSymbol` + erweiterte T4. Kachelmenge, Kategorien, Berechtigungen unverändert. [[fallstricke]] §58 Nachtrag.

## Deploy-Risiken

Gering: Web + Dienst (nur Versionsnummer). Versionsfolge: Teil 6 (Lagerplatz ausbuchen mit FA-Filter) ist für 1.99.47 vorgesehen.

## QA (2026-11-20)

QA-Fix `2f3e98cc`: `file-earmark-pdf` und `gear` rendern schon an der Quelle als volle Fläche → Kommissionierlisten und Vorschlagsliste
zeigen jetzt `file-earmark` (wie Buchungen), Einstellungen zeigt `pencil` (Spec-Ausweichregel). Status Testbereit; Publish `publish\` mit Commit `2f3e98cc`.
