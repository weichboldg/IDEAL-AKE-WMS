---
type: changelog
version: 1.99.50
date: 2026-11-23
---
# v1.99.50 — Startseite: Kachel-Symbole eindeutig

Bündel `feature/2026-08-07-ideal-teile-1-5`, Basis `0b6fff36` (v1.99.47; 1.99.48/1.99.49 in Entwürfen reserviert, nicht gebaut).
Spec [[2026-11-23-bugfix-dashboard-symbole-nachschaerfen-spec]] (Variante B), Lauf [[2026-11-23-goal-lauf-dashboard-we-lagerbestellung]].
Commit `af6eadd7`. Kein SQL, keine Migration, keine Einstellung. TS-175. Nicht gemergt.

## Kernpunkte

- 21 Kacheln mit neuem Inline-SVG-Symbol (selbstgezeichnete einfache Formen: Lagerplatz-Rahmen mit Minus/Pfeil/Ausrufezeichen/Fächern,
  Blatt mit Σ/Zeilen/Tropfen, Eingangskorb, Tacho, Regal, Uhr, Werkbank, Sanduhr, Glühbirne, Schlüssel, Blechtafel, LKW …), keine Doppelbelegung.
- Wächter `NavigationSichtTests.KachelSymbole_SindEindeutig`; Symbole weiter aus einer Quelle (`NavigationKatalog`, ADR 0034).

## Deploy-Risiken

Keine. Web + Dienst (Dienst nur Versionsnummer).
