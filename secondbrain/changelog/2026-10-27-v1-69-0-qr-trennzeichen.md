---
type: changelog
version: 1.69.0
date: 2026-10-27
---
# v1.69.0 — QR-Trennzeichen wählbar, Auswahllisten in /Settings

Spec [[2026-10-23-qr-code-matchcode-statt-artikelnummer-spec]], Freigabe-Nachtrag 2 vom 2026-10-27. Basis `8540c4d6` (v1.68.1), Commit `2c1e3a6d`.
**Keine Migration**, nur Web (Dienst nur Version). TS-101.15–101.21.

## Kernpunkte

- Allgemeiner Mechanismus: `AppSettingKeys.FesteWerte` (Key → erlaubte Werte) speist das Dropdown in `/Settings` (Gruppen und „Sonstige“)
  und die Prüfung in `SaveSettings`. `QrErstesFeld` ist jetzt Auswahlliste; ein ungültiger gespeicherter Wert bleibt als „ungültig“ sichtbar.
- Neue Einstellung `QrTrennzeichen` (`;` Standard | `,`), Laufzeit-Seed, body-Attribut `data-qr-trennzeichen` nur bei `,`.
- Alle QR-Zerlegungen (`processScannedValue`, Matchcode-Wrapper, `HwScanCore.zuordnung`) nutzen das eingestellte Zeichen.

## Deploy-Risiken

- Nur Web. Nach dem Deploy `QrTrennzeichen = ;` (Seed) = bisheriges Verhalten.
- Bei `,` werden Etiketten mit `;` nicht mehr zerlegt (gewollt, genau ein Zeichen). Lagerplatz-Codes mit Komma vorher prüfen (TS-101).
