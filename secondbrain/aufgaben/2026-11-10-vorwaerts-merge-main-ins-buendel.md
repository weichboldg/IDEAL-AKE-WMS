---
typ: notiz
status: Offen
worktree: .claude/worktrees/2026-08-07-ideal-teile-1-5
branch: feature/2026-08-07-ideal-teile-1-5
version: "1.95.x"
---
# Eigener Lauf: Vorwärts-Merge main → Bündel (Hotfixes v1.30.3/v1.30.4 + Zahnrad)

Entscheidung Gerald 2026-11-10: Die Bugfixes von main kommen in einem **eigenen Lauf** ins Bündel, nicht nebenbei.

## Inhalt von main (Stand `4296dcb0`, gepusht 2026-11-10)
- Zahnrad-Hotfix Lagerbestellung (Merge `e386bfce`).
- OSEON-Hotfix v1.30.3 [[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]].
- Kommissionierfotos v1.30.4 [[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-spec]] (Merge `df54dee8`).

## Befehl
```powershell
cd C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5
git merge --no-ff main -m "merge: main (v1.30.3 OSEON, Zahnrad, v1.30.4 Kommissionierfotos) -> Buendel"
```

## Erwartete Konflikte (Probe `git merge-tree` 2026-11-10, 11 Dateien)
- Versionen `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/AppVersion.cs` – Bündelversion behalten/erhöhen.
- `Views/Help/Changelog.cshtml` – beide Einträge behalten.
- `docs/TESTSZENARIEN.md` – Kapitel 124 (Fotos) hinter 123. Die Update-Skript-Spec
  [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]] plant ebenfalls TS-124 → auf 125 umstellen.
- `Program.cs`, `.gitignore` – Foto-Ordner/`LoginRedirectMiddleware` in den Bündel-Startcode einsetzen.
- Kommissionierung (Zahnrad): `PickingController.cs`, `PickingListViewModel.cs`, `Views/Picking/Index.cshtml`,
  `Views/WarehousePicking/Details.cshtml`, `PickingControllerTests.cs` – fachlich lösen, nicht mechanisch.

## Danach
Build + Tests, qa-agent mit Sichtprüfung (Fotos TS-124, Zahnrad, OSEON) auf DEV AKE/IDEAL; Brain-Changelog und
Feature-Map nachziehen. Erst dann Bündel-Deploy (Update-Skript-Spec, offene Frage 5 erledigt sich damit).
