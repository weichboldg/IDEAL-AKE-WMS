---
type: changelog
version: 1.30.2
date: 2026-10-08
---
# v1.30.2 — AKE-Hotfix: Zahnrad auf der Lagerbestellung-Detailseite sichtbar

Hotfix auf Basis `main` im Worktree `feature/2026-10-08-ake-hotfix-zahnrad-lagerbestellung`, Commit `0a6fb3cd`.
Auftrag Gerald 2026-10-08 („am main auch den Zahnrad-Fehler beheben“) — entscheidet die offene Frage in
[[2026-10-06-lagerbestellung-zahnrad-unsichtbar]] zugunsten eines eigenen AKE-Hotfix. **Keine Migration.**

## Kernpunkte

- Ursache seit v1.30.1 (`f653f8a0`): `#incomplete-alert` (`d-none`) direkt vor `.table-responsive`; das Zahnrad hing
  darin (unsichtbar) und wurde beim Einblenden per `textContent` gelöscht. Regel: [[fallstricke]] §19.
- Fix in `Views/WarehousePicking/Details.cshtml` identisch zum Bündel-Fix
  [[2026-10-08-v1-47-5-zahnrad-lagerbestellung-lackflag-kind-sub-fa]]: leerer Toolbar-Host als direkter Vorgänger
  der Tabelle, Hinweis darüber.
- Wächter `IdealAkeWms.Tests/Views/BomZahnradHostTests.cs` **wortgleich** aus dem Bündel (enthält auch den
  Bom-Wächter, auf `main` grün) — damit der spätere Vorwärts-Merge `main` → Bündel diese Datei konfliktfrei
  übernimmt. TS-18.10 um den Zahnrad-Zusatz ergänzt.

## Deploy-Risiken

Nur Web-Publish (Service-Version mitgezogen, keine Service-Änderung). Beim Vorwärts-Merge ins Bündel sind
Konflikte in `AppVersion.cs`, `Help/Changelog.cshtml`, `Details.cshtml` (Kommentarzeile) und TS-18.10 zu erwarten
— jeweils beide Seiten behalten bzw. Bündel-Version nehmen.
