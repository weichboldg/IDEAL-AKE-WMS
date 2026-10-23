---
type: changelog
version: 1.67.1
date: 2026-10-23
---
# v1.67.1 — Hardware-Scanner: Fokus-Eingabe-Geräte (Netum)

Spec [[2026-10-15-scan-handler-hardware-scanner-spec]], Freigabe-Nachtrag 4. Basis `4bfa4576` (v1.67.0), Commits `eb4b28ea`, `a2651019` (qa).
**Keine Migration**, nur Web. TS-99.17, TS-99.18.

## Kernpunkte

- **Erkennung am eingefügten Text:** Der Netum AIDC hat keine Tastatur-Emulation. Er schreibt `~09<Code>` + Enter als Ganzes ins
  Feld mit Fokus, Tasten kommen höchstens als keyCode 229.
  - Neuer Kern `HwScanCore.createText`: Eine Eingabe-Folge wird gegen den Feldwert davor verglichen. Beginnt der eingefügte Text mit
    dem Präfix, wird das Feld zurückgesetzt und der Code über dieselbe `zustellen`-Kette ausgewertet wie ein Tasten-Scan.
  - Enter wird vor der 229-Ausnahme abgefangen; ohne Enter gilt die Zeitgrenze.
- **Tasten-Erkennung unverändert:** Die alte Node-Prüfung läuft gegen das neue Skript grün. Doppelauswertung ist ausgeschlossen: nur
  `isTrusted`, kein Text während eines Tasten-Scans.
- **Kamera-Fenster:** neues Feld „Handscanner“ (`inputmode="none"`, Fokus beim Öffnen). Ohne Feld mit Fokus gibt ein
  Fokus-Eingabe-Gerät nichts aus.
- **Scanner-Diagnose:** Schalter in „Mein Profil“ (localStorage, Standard aus). Die letzten 30 Ereignisse mit der Entscheidung stehen am
  Seitenende, mit Knopf „Kopieren“; nichts geht an den Server.

## Deploy-Risiken

- **Web; Dienst nur Versionsstand mitpublizieren** (AppVersion). Wirkt nur bei `HardwareScannerAktiv = true`.
- **Grenzen:**
  - Ein von Hand eingefügter Text mit Präfix gilt als Scan.
  - Eine IME-Komposition kann durch das Zurücksetzen des Feldwerts gestört werden.
  - Ob der Netum in ein Feld mit `inputmode="none"` schreibt, zeigt erst TS-99.17 c.
