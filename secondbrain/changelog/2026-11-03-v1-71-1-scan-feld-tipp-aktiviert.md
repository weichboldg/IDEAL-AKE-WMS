---
type: changelog
version: 1.71.1
date: 2026-11-03
---
# v1.71.1 — Einbuchung: ein Tipp aktiviert das Scan-Feld (Freigabe-Nachtrag 7)

Spec [[2026-10-15-scan-handler-hardware-scanner-spec]] (Abschnitt „Nachbesserung v1.71.x“), Umsetzung
[[2026-10-15-scan-handler-hardware-scanner-umsetzung]]. Basis `51c3203d` (v1.71.0), Commit `9ca87f7c`. **Keine Migration**, nur Web
(Dienst nur Version). TS-99.19 a7–h7.

## Kernpunkte

- Befund Netum (Gerald 2026-11-03): grünes Feld nach Skript-Fokus nahm keinen Scan an; erst Tipp auf ein Formularfeld, dann aufs Scan-Feld ging.
- `#hwScanOhneFeld` ohne `inputmode` im Markup, autocorrect/autocapitalize off, spellcheck false, `form-control-lg`, volle Kartenbreite.
- `pointerdown` = Benutzergeste; nur danach nach ~300 ms `inputmode="none"` + grün „✓ Scan bereit“; blur entfernt `inputmode`, grau.
  Skript-Fokus (`parken`) bleibt, zeigt grau „Zum Scannen hier tippen“.
- Diagnose „Scan-Feld Fokus an (durch Tipp|per Skript), inputmode=…“.
- Fallstrick: [[fallstricke]] §29.

## Deploy-Risiken

- Keine Migration, kein Setting. Wirkt nur mit Geräte-Schalter „Scannen ohne Feld“; am PC und mit Schalter aus wie v1.71.0.
- Am Gerät offen: ob Chrome die Tastatur beim Umschalten einklappt (TS-99.19 b7); bleibt sie stehen, mit Zurück schließen (laut Nachtrag hinnehmbar).

## QA (2026-11-03)

- QA-Fix `6969620a`: Fokusring im Zustand 2 (nur Skript-Fokus bzw. Tab) jetzt als Outline `#495057` mit Abstand, ca. 8:1. Vorher war es ein
  halbtransparenter Ring mit ca. 1,8:1, und das war der einzige Unterschied zu Zustand 1. Der Wächter prüft jetzt, dass `inputmode="none"`
  genau einmal und nur in `nachTipp` gesetzt wird.
- Build/Tests grün (Web 2254 + 1 übersprungen, Dienst 328, Node 57). DEV AKE + IDEAL auf v1.71.1. Status Testbereit, wartet Schranke 2 (Netum).
