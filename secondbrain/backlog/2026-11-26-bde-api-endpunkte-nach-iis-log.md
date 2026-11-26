---
typ: aufraeumen
status: offen
created: 2026-11-26
herkunft: "Besprechung 2026-11-26, Entscheid 15 (F-29 A)"
standort: beide
---
# BDE: `/api/bde`-Endpunkte nach 30 Tagen IIS-Log ohne Aufruf entfernen (F-29)

Bezug: [[2026-11-26-loesungsvorschlaege-bde-ui]] (F-29), [[2026-11-26-besprechung-0830-kritische-pruefung]] (Block 4, Entscheid 15).

- v1.99.81: die verwaisten GETs `BdeTerminal/ActiveBookings` und `BdeTerminal/PausedBookings` sind gelöscht.
- Offen: `BdeApiController` (`/api/bde/...`, u. a. StartProductionForOrder) hat keine Wächter wie der `BdeTerminalController`. Nach dem Deploy
  von 1.99.81 an beiden Standorten 30 Tage IIS-Log auswerten (Prüfung für den Menschen: IIS-Log-Ordner der WMS-Site nach `/api/bde/` durchsuchen,
  Aufrufe je Pfad zählen). Ohne Aufruf: Endpunkte entfernen (TS-90.11 anpassen); mit Aufruf: Wächter nachziehen.
