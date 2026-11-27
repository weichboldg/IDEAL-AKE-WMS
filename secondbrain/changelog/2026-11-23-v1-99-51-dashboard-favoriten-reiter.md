---
type: changelog
version: 1.99.51
date: 2026-11-23
---
# v1.99.51 — Startseite: Reiter „Favoriten“

Spec [[2026-11-23-dashboard-favoriten-reiter-spec]], Lauf [[2026-11-23-goal-lauf-dashboard-we-lagerbestellung]].
Commits `4802462c`, `563adecd`. Kein SQL, keine Migration (Ablage `UserViewPreferences`, Key `Startseite.Favoriten`). Manifest: Info
`v1.99.51-StartseiteFavoriten`. TS-176. Nicht gemergt.

## Kernpunkte

- Reiter „Favoriten (n)“ / „Alle“; Standard „Favoriten“, sobald ein sichtbarer Favorit existiert. Stern je Kachel (POST + Redirect, Antiforgery).
- Nur sichtbare Kacheln (`NavigationErgebnis.Favoriten`), Katalogreihenfolge; nicht verfügbare = gelber Hinweis + „Nicht verfügbare entfernen“.
- Kein Menü-Eintrag; Admin „Ansichten zurücksetzen“ löscht auch die Favoriten. [[fallstricke]] §60.

## Deploy-Risiken

Keine. Web + Dienst (Dienst nur Versionsnummer).
