---
type: changelog
version: 1.66.0
date: 2026-10-21
---
# v1.66.0 — Hardware-Barcodescanner: Tastatur-Scan mit Präfix

Spec [[2026-10-15-scan-handler-hardware-scanner-spec]], Umsetzung [[2026-10-15-scan-handler-hardware-scanner-umsetzung]].
**Direkt im Bündel** (Freigabe-Nachtrag 2026-10-21), Basis `c27f3b6d` (v1.65.0), Commits `cb49f262`, `789d0a78` (qa). **Keine Migration**, kein SQL.

## Kernpunkte

- Neu `wwwroot/js/hardware-scanner.js`: Präfix-Variante B (`HardwareScannerPraefix`, Default `~09`), Ende per Enter/Tab oder
  Zeitgrenze (`HardwareScannerMaxZeichenabstandMs`, Default 50). Kein AIM, keine Gerätelogik; Testgerät Netum AIDC.
- Ziel-Regel (a) Fokus im Scan-Ziel > (b) freies Textfeld > (d) einziges Ziel > Hinweis. Die 14 Kamera-Scan-Seiten registrieren ihre Ziele
  automatisch über `initScanner`/`initTextInputScanner`; ausgewertet wird über dieselbe `processScannedValue`-Kette wie der Kamera-Scan.
- BDE-Terminal: nur Markup `data-hw-scan-ziel`; nach der Anmeldung landet der Scan in „FA-Nr scannen“, Enter löst die v1.64.0-Auflösung aus.
- Standort-Hauptschalter `HardwareScannerAktiv` (Default aus) in den Standorteinstellungen, Geräte-Abschalter in „Mein Profil“
  (`localStorage`). Kein Benutzerfeld.
- Standorteinstellungen: leeres Int-Feld bleibt unverändert, statt das Speichern der ganzen Maske zu blockieren.

## Deploy-Risiken

Nur Web. Ohne Einschalten keine Wirkung (Skript wird nicht ausgeliefert). Nach dem Einschalten wird an jedem Gerät ein getipptes `~`
bis zu 50 ms zurückgehalten; IME-Eingaben (Bildschirmtastatur) werden nie abgefangen — sendet ein Scanner über die IME, wirkt nur das
Einfügen ins fokussierte Feld. `~` kann auf manchen Layouts eine Tottaste sein (TS-99.1 b, Präfix dann umstellen). Manual-UAT TS-99.
