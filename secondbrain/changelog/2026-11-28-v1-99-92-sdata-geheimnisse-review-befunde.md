---
type: changelog
version: 1.99.92
date: 2026-11-28
---
# v1.99.92 — SData-Geheimnisse und Sage-Test: Review-Befunde SD-1/2/3/4/6

[[2026-11-28-sdata-geheimnisse-review-befunde-spec]], Lauf-Notiz [[2026-11-28-buendel-review-kommissionierung-goal-lauf]]. Worktree `feature/2026-11-28-buendel-review-kommissionierung`, Commits `c9c9f888`, QA-Fix `7abbc46b`.
**Keine Migration.** Neuer Manifest-Eintrag `Pruefung/SDataKennwortAppsettingsWeb` (Feld `Ort='Web'`). TS-212. ADR [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]] Nachtrag. Gemergt in main (Merge-Commit `50952006`, 2026-11-28).

## Kernpunkte

- Verbindungstest: gespeicherte Zugangsdaten nur für die gespeicherte `SData:BaseUrl` (Schema/Host/Port, kein `@`, TLS nicht schwächer,
  http nur wenn gespeichert http) — `SdataZugangsdaten.SperreGespeicherterZugang`; Antwort per Stream auf 50.000 Zeichen gekappt.
- Freitext-Weg `/ServiceSettings`: Katalog-Keys in anderer Schreibweise abgelehnt — Groß/Klein, Leerzeichen am Ende, Vollbreite
  (QA Q-1: Kollation `Latin1_General_CI_AS` traf sonst die echte `SData:Password`-Zeile).
- Sage-Test Feldliste: `orderBy`/`include` ohne `& ? #`. `SageBookingWorker`: Lagerbuchung und Sage-WE je eigener try/catch.
- Update-Skript liest Appsettings-Prüfungen mit `Ort='Web'` aus dem Web-Ordner.

## Deploy-Risiken

- Web und Dienst. IDEAL-DEV meldet `Pruefung/SDataKennwortAppsettingsWeb` = echte Abweichung (Kennwort in `appsettings.DEV.json`) — auf Test/Prod beachten.
- Offen: TS-212.5/212.7 (erreichbares Sage), 212.11 (beide Schalter an), Prüfabfragen P-1..P-3 auf Prod. Zusatzfund Z-1: doppelte WarningMessage auf `/ServiceSettings` (vorbestehend).