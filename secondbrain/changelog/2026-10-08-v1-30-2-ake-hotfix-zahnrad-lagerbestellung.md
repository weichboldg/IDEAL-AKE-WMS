---
type: changelog
version: 1.30.2
date: 2026-10-08
---
# v1.30.2 — AKE-Hotfix: Zahnrad auf der Lagerbestellung-Detailseite sichtbar

**Gemerged** in `main` am 2026-11-08 (Merge-Commit `e386bfce`, Freigabe Gerald; danach folgt OSEON-Hotfix
[[2026-11-08-v1-30-3-ake-hotfix-oseon-sync-druck-werkbank]]). Hotfix auf Basis `main` im Worktree `feature/2026-10-08-ake-hotfix-zahnrad-lagerbestellung`, Commit `0a6fb3cd`.
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

Nur Web-Publish (Service-Version mitgezogen, keine Service-Änderung). Commits `0a6fb3cd` + `6d0e954e` (QA-Nachzug:
Details-Kommentar wortgleich zum Bündel).

## Vorwärts-Merge `main` → Bündel (gemessen mit `git merge-tree` gegen `f102fbf5`, QA 2026-10-08)

- Konfliktfrei: `Details.cshtml`, `IdealAkeWms.Tests/Views/BomZahnradHostTests.cs` (beide wortgleich).
- Konflikt in **vier** Dateien: beide `AppVersion.cs` (1.47.5 vs 1.30.2), `docs/TESTSZENARIEN.md` (nur Überschrift
  „Zusatz v1.47.5“ vs „Zusatz v1.30.2“), `Views/Help/Changelog.cshtml`.
- **Achtung Changelog:** Git stellt die v1.47.5-Karte gegen die v1.30.2-Karte; der Konfliktblock umfasst auf der
  Bündel-Seite die **gesamte** Bündel-Historie v1.47.5 … v1.31.0 (~590 Zeilen). Wer hier die Hotfix-Seite nimmt
  oder von Hand kürzt, verliert sie. **Auflösung für alle vier Dateien: Bündel-Seite behalten**
  (`git checkout --ours <datei>` beim Merge im Bündel). Optional die v1.30.2-Karte von Hand direkt über v1.30.1
  einfügen.
- Ohne diesen Hotfix wäre `main` konfliktfrei ins Bündel gegangen (Baseline-Messung der QA).
- Für die Aufgabe [[2026-10-05-vorwaerts-merge-ake-hotfix-ins-buendel]] übernehmen (dort nicht eingetragen,
  weil die Notiz fremde, uncommittete Änderungen trägt).
