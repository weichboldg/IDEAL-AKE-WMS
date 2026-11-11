---
type: changelog
version: 1.68.1
date: 2026-10-27
---
# v1.68.1 — Einbuchung: sichtbares Scan-Feld „Scan bereit“

Spec [[2026-10-15-scan-handler-hardware-scanner-spec]], Freigabe-Nachtrag 6. Basis `ca48b22d` (v1.68.0), Commit `69bb4fd1`.
**Keine Migration**, nur Web. TS-99.19 angepasst.

## Kernpunkte

- Befund Netum (v1.68.0): Der Text erreichte das unsichtbare Feld nicht (keine `input`-Zeile). Darum ist `#hwScanOhneFeld` jetzt ein
  sichtbares, antippbares Feld (kein `aria-hidden`, kein `opacity:0`/1 px, kein `pointer-events:none`), nur bei Schalter „Scannen ohne Feld“.
- Fokus grün „✓ Scan bereit“, sonst grau gestrichelt „Scan: tippen zum Aktivieren“; nach jedem Scan leer.
- Diagnose: jede Zeile mit `[aktiv: …]`, dazu „Scan-Feld Fokus an/aus“.

## Deploy-Risiken

- Nur Web. Ob der Netum in das sichtbare Feld schreibt, zeigt erst TS-99.19 am Gerät (Ursache in Nachtrag 6 ungeprüft).
- QA-Fix `8540c4d6`: Das Leeren nach dem Scan zieht die Basis der Text-Erkennung (`vor`) mit; sonst wurde nach Rest-Text im Feld jeder
  zweite Scan nicht erkannt. TS-99.19 o–q ergänzt. QA-Nachweis in der Spec, Status Testbereit.
