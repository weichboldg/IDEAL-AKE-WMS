---
type: changelog
version: 1.99.37
date: 2026-11-17
---
# v1.99.37 — FA-Struktur: Abgleich mit Lebenslauf statt Full-Replace

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commits `7a1f7301` `94e327e7` `026d0d32` `354344d8` `8ea8e98d` `2abad047`+`b1f83b1a` `0963a0a5` `7fa7603d`+`96049d0d`. Spec [[2026-11-17-fa-struktur-abgleich-statt-full-replace-spec]] (Epic E1–E8, E0 teilweise),
ADR [[0032-fa-struktur-abgleich-mit-lebenslauf-statt-full-replace]], Fallstrick 48. Goal-Lauf [[2026-11-17-goal-lauf-sammel-abend]]. Nicht gemergt.

## Kernpunkte

- Lebenslauf-Felder an drei Tabellen, UNIQUE-Schlüssel, Abgleich statt Löschen/Neuanlage; Ids bleiben stabil.
- Verschwundene Zeilen: Sage-Zustand aus `KHKPpsFaBelege` → beendet/inaktiv behalten, unklar behalten + Mail, nicht gefunden entfernen.
- Wächter: leerer Read, alle HauptFA fehlen, doppelter Schlüssel, Sage-Abfragefehler → nichts entfernen, Mail.
- Aufräumen nach erledigten/stornierten WMS-Aufträgen oder `Sync:FaStrukturAufbewahrungTage` (90).
- Schild „in Sage beendet/inaktiv/ungeklärt“ in FA-Struktur, Kommissionierliste, Vormontage, Beschichtung (Liste + Summiert), BDE-Teileliste
  (dort nicht meldbar). Materialisierung/Erkennung nur aktive Knoten. R4: Artikeltyp vom Artikel (`FelderSetzen`).

## Deploy-Risiken

- **SQL/159** (additiv, Dienst vorher stoppen): bricht vor dem UNIQUE-Index ab, wenn der Bestand Dubletten hat → Vorher-Prüfung
  `Pruefung/FaStrukturSchluesselDoppelt`. Manifest: `v1.99.37-FaStrukturAufbewahrungTage`, `v1.99.37-FaStrukturBeendetSichtbar`.
- **Vor Prod IDEAL:** Mandant-Filter der Sage-Abfrage (P-AB4) offen; P-AB3/5/6/8 und P-AB1/P-AB2 auf dem IDEAL-Server offen.
- AKE: Tabellen leer, keine sichtbare Änderung. Tests: TS-158.
