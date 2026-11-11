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

## Push des Bündels (Klärung 2026-11-11, Gerald + Koordination)
- Vor dem Lauf geprüft: Worktree sauber, HEAD `a26be406` (v1.95.0).
- `origin/feature/2026-08-07-ideal-teile-1-5` (`6d81a4a3`, Push 05.10.) und das lokale Bündel sind auseinandergelaufen (307 Commits nur auf Remote,
  501 nur lokal). Ursache: Die Historie wurde am 05.10. zweimal per `fast-import` neu geschrieben (Reflog Zeilen 28–30). Gepusht wurde
  die erste Fassung, lokal ging es mit der zweiten weiter (`b877e05a`).
- Gegenprobe Gerald: `git diff --stat 6d81a4a3 b877e05a` ist leer. Remote hat also nichts Eigenes, der Inhalt ist gleich.
- Der Push des Bündels geht deshalb nur mit Force, und zwar **nach** diesem Lauf und **vor** dem Bündel-Merge nach main:
  `git push --force-with-lease=feature/2026-08-07-ideal-teile-1-5:6d81a4a3 origin feature/2026-08-07-ideal-teile-1-5`

## Sicherung vor dem Lauf (Vorgabe Gerald 2026-11-11: „es darf nichts verloren gehen“)
1. Lokale Sicherungszweige bzw. Tags: `backup/buendel-2026-11-11` auf `a26be406`, `backup/origin-buendel-2026-10-05` auf `6d81a4a3`,
   `backup/main-2026-11-11` auf `02500726`.
2. Alle drei normal pushen. Das sind neue Namen, also kein Force. Danach liegt die lokale Bündel-Arbeit (501 Commits) auf GitHub.
3. Zusätzlich eine Offline-Kopie: `git bundle create D:\Backup\IDEAL-AKE-WMS-2026-11-11.bundle --all`, dann `git bundle verify`.
4. Nach dem Merge prüfen: `a26be406` und `main` sind Vorfahren von HEAD. Außerhalb der Konfliktdateien kein Diff gegenüber
   `a26be406`, der nicht aus main kommt.
5. Die Sicherungen werden erst gelöscht, wenn das Bündel in main gemergt und deployt ist, und nur mit Freigabe von Gerald.
- 2026-11-11 07:52: In GitHub Desktop wurde versehentlich „Pull origin“ auf das Bündel gestartet (93 Konflikte gegen den alten Remote-Stand). Abgebrochen, danach geprüft: Worktree sauber, HEAD `a26be406`. **Für das Bündel bis zum Force-Push kein Pull und kein Fetch-and-Merge.**
