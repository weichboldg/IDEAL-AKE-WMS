---
type: aufgabe
status: in-arbeit
created: 2026-11-20
spec: "[[2026-08-05-wms-bugs-improvements-teil-6-spec]]"
---
# Dev-Lauf: Lagerplatz ausbuchen mit FA-Filter (v1.99.47, TS-173)

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig `feature/2026-08-07-ideal-teile-1-5`, Basis v1.99.46.

## Stand
- [x] FaBuchungsText.Nummern/PruefeZeile/IstGemischt + Repository GetProductionOrdersByArticleAtLocationAsync + Tests (`29577a3a`)
- [x] Controller GET/POST (Serverprüfung) + Tests (`29577a3a`)
- [x] View/JS (Spalte, Badges, Modal, Enter, Knopf-Sperre), Version, Changelog, TS-173 + TS-2.4, Hilfe, docs/anwender 01/09, docs/betrieb, Manifest (`d28a6488`)
- [x] QA (qa-agent) DEV AKE + IDEAL: **Testbereit** 2026-11-22. QA-Fix `0b6fff36`: Q-1 BLOCKER `data-fa` als rohes JSON im Attribut
      (Modal öffnete nie) → `.ToString()`, Wächter `JsonImAttributWaechterTests`, [[fallstricke]] §59; Q-2 Platzcode im `confirm()` per
      `Json.Serialize`; Foto 09-05 neu. Erster QA-Lauf 2026-11-20 am Wochenlimit abgebrochen, am 2026-11-22 fortgesetzt.
- [x] Paket-ZIP `paket12-v1.99.47.zip`, 236 905 105 Byte, SHA-256 `AE32A1F3F4AFE427FA867605802A2844878EDF5A8196E40EC6A7CA3EA15BF605` (Publish-Commit `0b6fff36`)
  - `\\AKENET01.ake.at\Installsourcen`: Größe + SHA-256 gleich ✔
  - `\\IDEALWEB01.ideal.ideal-ake.at\InstallSourcen`: Größe + SHA-256 gleich ✔ (Hash nachgeprüft 2026-11-22 nach Freigabe Gerald; erster Lauf war wegen Speichermangel abgebrochen)
  - lokales `publish\` geleert 2026-11-22
- [x] Brain: Changelog [[2026-11-20-v1-99-47-lagerplatz-ausbuchen-fa-filter]], feature-map, fallstricke §59, Testindex

## Manual-UAT (aus QA)
- leeres Feld mit `confirm()` (173.8 Teil 2, AK 17, 173.14, TS-2.4 Schritt 7, 173.16 leer/Enter/OK)
- 173.9/AK 22 Sage-Queue auf dem Testsystem; 173.7 Zwei-Tab (7a/7b) + Manipulation; 173.13 mit echtem AKE-Satz
- IDEAL mit echter Struktur, Einstieg über Konflikt-Dialog; Info an Lagermitarbeiter. DEV-Testdaten QA173-* bleiben stehen.

## Confluence (für spätere Übernahme)
- Anwender Kapitel 09 „Einbuchen, Ausbuchen, Umbuchen“, Abschnitt 5 Lagerplatz ausbuchen (Text + Foto 09-05), Stand 1.99.47
- Anwender Kapitel 01 „Kommissionierung“, Fenster „Kommissionierwagen belegt“ (Knopf Lagerplatz ausbuchen), Stand 1.99.47
- Anwender README „Stand und was noch folgt“ (Zeile 1.99.47)
- Betrieb 00-index (Stand, Zeile 1.99.47)
