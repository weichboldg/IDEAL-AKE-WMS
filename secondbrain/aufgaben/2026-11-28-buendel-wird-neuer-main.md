---
typ: notiz
status: Erledigt
branch: main
version: "1.99.89"
---
# Bündel 1.99.89 wird neuer main (ohne Merge)

Entscheidung Gerald 2026-11-25/27: Das Bündel `feature/2026-08-07-ideal-teile-1-5` wird der neue `main`. **Kein Merge**, alter Stand bleibt gesichert und baubar. 1.30.4 ist seit 2026-11-24 nicht mehr im Einsatz, alle Systeme laufen auf 1.99.xx.

## Ergebnis (2026-11-28)
- `main` = `origin/main` = `524d1346` — ein Commit mit **einem** Parent (Bündel `4ec9155d`).
- Baum: Code, `SQL/`, `docs/`, `scripts/`, `Branding/`, `.gitignore` byte-gleich aus dem Bündel; `secondbrain/`, `.claude/`, `CLAUDE.md` byte-gleich aus dem alten main (`f1499c1b`). Geprüft per `git diff` (0 Abweichungen je Seite).
- Force-Push mit `--force-with-lease` gegen `21430eb3` (alter `origin/main`).
- Build grün, Tests 848/848 (Service) + 3853/3854 (Web, 1 übersprungen) auf dem byte-gleichen Bündel-Code.

## Sicherungen des alten Stands
- Tag `v1.30.4` → `f1499c1b` (letzter alter main, Code 1.30.4 + Brain bis 2026-11-27).
- Branch `save/main-v1.30.4` → `e5e5e948` (gepusht), Worktree `.claude/worktrees/save-main-v1.30.4`, Build grün.
- Tag `v1.99.89` → `4ec9155d` (Bündel vor der Übernahme).
- Dateikopie des ganzen Ordners inkl. Worktrees: `D:\Backup\IDEAL-AKE-WMS_2026-11-25_vor-main-umstellung` (robocopy 0 Fehler).

## Hinweise
- Warum Brain aus main: Das Bündel hat seit der Abzweigung `1753c79e` keinen Commit in `secondbrain/`, `.claude/` oder `CLAUDE.md` — alle Brain-Arbeit lag nur in main (Regel „Brain wird nicht verzweigt“).
- `secondbrain_alt/` (Junctions auf `docs/` und `SQL/`) zeigt nach der Umstellung viele geänderte/untrackte Dateien: Die Junctions spiegeln jetzt den neuen Code-Stand, der eingecheckte Baum ist ein alter Schnappschuss. Nicht angefasst.
- Worktrees/Branches nicht aufgeräumt (Freigabe nötig).

## Nachzug Specs und Spec-Lauf (Gerald 2026-11-28: „alles ändern“)
- Alle Specs mit Status `Testbereit`, deren Code jetzt in main liegt (123: 112 mit Bündel-worktree/-branch, dazu Teile 1–5 v1.31, FA-Struktur-Symbole 1–3, Arbeitsgang-Stammdaten, Menü Beschichtung, OSEON-Hotfix v1.30.3), auf `Gemerged`; worktree/branch bleiben als Historie stehen. Feature-Map: alle `**Testbereit**` → `**Gemerged**` + Hinweis oben.
- Specs `InUmsetzung` (2) und Entwürfe (5) mit Bündel als Ziel: `worktree`/`branch` geleert — beim nächsten Lauf neuer Worktree von main (Teilstand liegt schon in main).
- Künftig: je Lauf ein neuer Worktree von main statt des Bündel-Worktrees.
- Spec-Lauf liest Code im Hauptcheckout (main): `secondbrain/prompts/spec-lauf-backlog.md`, `scripts/local/spec-lauf-backlog.ps1` (lokal), `CLAUDE.md` (Aufgabenname, Mo–Fr 08:10/13:10, Log `secondbrain/aufgaben/logs/`).
- Doppelte, deaktivierte Windows-Aufgabe „WMS Spec-Lauf Backlog“ (lief im Bündel-Worktree) gelöscht; XML-Sicherung `D:\Backup\2026-11-28_Aufgabe_WMS-Spec-Lauf-Backlog_alt.xml`. Aktiv bleibt „IDEAL-AKE WMS Spec-Lauf Backlog“.
- Bündel-Übersichten `2026-11-07-buendel-uebersicht` / `2026-11-08-buendel-uebersicht` unverändert (Historie).
