---
type: changelog
version: 1.99.77
date: 2026-11-25
---
# v1.99.77 — BDE-Seite FA: Meine Buchungen, Dialoge, Scan, Tätigkeit

Spec [[2026-11-25-bde-masken-vereinheitlichung-spec]], Etappe 4b. Commit `d61ea22c` (Worktree `feature/2026-08-07-ideal-teile-1-5`).
**Keine Migration**, keine neue Einstellung. Web und Dienst (Dienst nur Versionsnummer). TS-198. Nicht gemergt.

## Kernpunkte

- Reiter FA: „Meine Buchungen“ (laufend/pausiert) mit Pause, Teilfertig, Fertig, Rüsten fertig, Produktion, Beenden, Fortsetzen; Schalter „Zeiten und Details“.
- Abfrage an einem Ort `BdeKopfBuchungen` (auch Terminal-JSON `ActiveBookings`/`PausedBookings`).
- Dialoge als gemeinsame Partial `_BdeDialoge` (Terminal + FA-Sicht); keine Browser-Dialoge.
- Scan-Feld „FA scannen“ (Handscanner-Ziel), `resolve-scan?faSicht=true` ohne `BdeNurFaMeldung`; Markierung + Blättern zur Zeile.
- Tätigkeit starten; „Fertig melden“ je Zeile an Werkbänken mit nur AG-Fertigmeldung.
- Buchungen über die bestehenden `BdeTerminal`-JSON-Aktionen (Kollision serverseitig).
- Manifest: Info `v1.99.77-BdeFaSichtBuchungenInfo`.

## Deploy-Risiken

- Keine DB-Änderung. Browser-Cache am Terminal ggf. leeren (neues JS/CSS). Altes Terminal unverändert in Betrieb.
