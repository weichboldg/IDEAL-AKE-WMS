---
typ: notiz
spec: "[[2026-10-15-scan-handler-hardware-scanner-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Scan-Handler für Hardware-Barcodescanner (Präfix-Variante B)

Spec [[2026-10-15-scan-handler-hardware-scanner-spec]], **direkt im Bündel** (Freigabe-Nachtrag 2026-10-21). Start `c27f3b6d`
(v1.65.0), Ziel **v1.66.0**, **TS-99**. Keine Migration. Deploy: nur Web.

## Plan (kompakt)

**Global:** Variante B (Literal-Präfix aus `HardwareScannerPraefix`, Default `~09`, Ende Enter/Tab/Zeitgrenze). Kein AIM, keine
Gerätelogik. `processScannedValue`, das Kamera-Modal und `bde-terminal.js` bleiben unverändert.

1. **Kern + DOM** `wwwroot/js/hardware-scanner.js`:
   - `HwScanCore.create({prefix, maxGapMs})` mit `key(k,t)` → `{action: pass|hold|swallow, replay?, scan?:{code, endedWithEnter}}`
     und `tick(t)` (Zeitgrenze). Dualer Export.
   - DOM: Capture-`keydown` auf `window`. Ausnahmen: nur echte Ereignisse (`isTrusted`), IME/`229` geht durch, Passwortfeld nie,
     Kürzel ohne AltGr gehen durch.
   - Es feuert `wms:hardware-scan`. Danach gilt Regel (b) freies Feld (Einfügen, `input`/`change`, synthetisches Enter), dann
     (d) einziges `[data-hw-scan-ziel]` (sichtbar), sonst ein Hinweis.
   - Gerät aus: `localStorage['wms.hardwareScanner.geraet']==='aus'`, wird bei jeder Taste gelesen.
   - Check: `IdealAkeWms.Tests/Js/hardware-scanner.check.js` und `HardwareScannerTests` (SkippableFact, Muster
     `ColumnFilterSyntaxTests`).
2. **Register** `barcode-scanner.js`:
   - `initScanner`/`initTextInputScanner` tragen sich in `_hwScanTargets` ein.
   - Der Listener wählt (a) Fokus im Ziel, Knopf, `.input-group` oder offenem Select2. Danach (d), wenn kein freies Feld fokussiert
     ist: genau 1 Ziel, sonst `detail.ambiguous`.
   - Er ruft die globale `processScannedValue`.
3. **BDE:** `scanOperator` und `scanFaAg` tragen `data-hw-scan-ziel`, sonst nichts. Nach der Anmeldung ist nur `scanFaAg` sichtbar,
   also greift (d): Feld füllen, dann Enter, dann die bestehende Auflösung aus v1.64.0.
4. **Layout:** `HardwareScannerAktiv=="true"`, dann body-data-Attribute und das Skript vor den Seiten-Skripten.
5. **Einstellungen:**
   - 3 Keys in `AppSettingKeys` und die Gruppe „Hardware-Scanner“ im `StandortSettingsCatalog`.
   - Leeres Int-Feld bleibt unverändert, statt den ganzen Speichervorgang zu blockieren: Ohne Seed fehlt die Zeile, die Maske
     schickt dann `""`.
6. **Profil:** Abschnitt „Dieses Gerät“ mit form-switch (localStorage), nur sichtbar, wenn der Standort-Schalter an ist (body-Attribut).
7. **Doku:** v1.66.0 ×2, Changelog, Hilfe (Scanner-Abschnitt), README-AppSettings, TS-99.

## Review-Fokus

Shift zwischen Scanner-Zeichen (Großbuchstaben) darf den Scan nicht abbrechen · AltGr-`~` (ctrl+alt) ist kein Kürzel · langsames `~`
erscheint vollständig · synthetisches Enter wird vom eigenen Listener nicht erneut verarbeitet · Tottaste `~` (TS-99).

## Nachbesserung v1.67.1 (Freigabe-Nachtrag 4, 2026-10-23)

Plan (kompakt):
1. **Kern:** `createText`/`eingefuegt` mit Node-Prüfungen.
2. **DOM:**
   - `beforeinput`/`focusin` merken den Feldwert, `input` (Capture) setzt bei Präfix zurück und stellt zu.
   - Enter vor der 229-Ausnahme abfangen.
3. **Kamera-Fenster:** Feld „Handscanner“ (`inputmode="none"`, Fokus); `hwScanKameraZiel` greift auch, wenn der Fokus in diesem
   Feld liegt.
4. **Diagnose:** Profil-Schalter und Karte vor dem Seitenfuß.
5. **Doku:** Hilfe, Hinweistext, TS-99.17/18, v1.67.1 ×2, Changelog.

Review-Fokus:
- Select2-Suche sieht den Präfix nie.
- Das Enter-Suffix schickt kein Formular ab.
- Keine Doppelauswertung bei Tasten-Scanner + `input`.
- Passwort nie protokolliert.
- Eine Hand-Eingabe von `~09` bleibt Text.
