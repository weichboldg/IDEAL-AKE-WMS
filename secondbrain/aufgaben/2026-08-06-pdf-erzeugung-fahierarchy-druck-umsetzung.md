---
typ: notiz
spec: "[[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]]"
status: InUmsetzung
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: PDF-Erzeugung fuer FaHierarchy-Druckdokumente

Spec [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]] · bestehender Buendel-Worktree (Freigabe-
Antwort 1), **kein** neuer, **nicht** von `main` abgezweigt. Plan im Worktree:
`docs/superpowers/plans/2026-09-14-pdf-erzeugung-fahierarchy-druck.md` (7 Tasks). Version 1.39.0,
Testkapitel TS-74.

## Vorab-Messung am Dev-System (2026-09-14, VOR dem Plan)

Nach der Lehre aus dem widerlegten Sortier-Befund B-1 ([[2026-08-20-fehlerprotokoll-anzeige-epic-ab]]) wurde die Spec-Kommandozeile zuerst mit
`System.Diagnostics.Process` gemessen (Edge 152.0.4191.66, Registry App Paths → `C:\Program Files
(x86)\Microsoft\Edge\Application\msedge.exe`):

| Variante | Launcher-Exit | PDF da beim Exit? | PDF fertig nach | Groesse |
|---|---|---|---|---|
| `--headless` (Spec), eigenes `--user-data-dir` | 0 nach 212 ms | **nein** | 1 823 ms | 16 699 B, `%PDF-1.4` |
| `--headless=new` | 0 nach 402 ms | nein | 1 590 ms | 16 699 B |
| versionierter `152.0.4191.66\msedge.exe` | 0 nach 253 ms | nein | 1 471 ms | 16 699 B |
| `--no-sandbox` | 0 nach 198 ms | nein | 1 304 ms | 16 699 B |
| `--single-process` | 0 nach 244 ms | nein | **nie** (20 s) | — |
| `--no-startup-window` | 0 nach 388 ms | nein | **nie** (20 s) | — |
| ohne `--user-data-dir`, Edge des Nutzers laeuft | 0 nach 373 ms | nein | **nie** | — |

**Befund A1:** `msedge.exe` ist unter Windows ein Launcher — der gestartete Prozess endet sofort mit
Exit 0, Kindprozesse schreiben das PDF spaeter; nach dem Schreiben laufen keine Kinder mehr.
Spec-Schritt 4/5 („WaitForExit, dann Exit-Code pruefen") ist so nicht baubar; der Runner wartet
deshalb auf die **fertige Datei** (exklusiv oeffenbar + `%%EOF`). Timeout-Kill ueber das Startzeit-
Fenster der `msedge`-Prozesse (Kinder sind Waisen, `Kill(entireProcessTree)` greift nicht).
**Befund A2:** ein eigenes `--user-data-dir` je Lauf ist Pflicht. Weitere Abweichungen A3–A7 im Plan.

## Arbeitsstand

- 2026-09-14: Plan geschrieben und committet; Spec auf `InUmsetzung`. SDD-Ledger im Worktree unter
  `.superpowers/sdd/2026-09-14-pdf-erzeugung-fahierarchy-druck/progress.md`.
- 2026-09-14: 7 Tasks (SDD) umgesetzt, je task-reviewed; Gesamt-Review (11 Commits) — zwei
  Robustheitsbefunde behoben (Aufrufer-Abbruch killt Edge; Kandidatenermittlung darf App-Start nie
  abbrechen) und re-reviewed. Worktree-HEAD `85df349`. Build 0 Fehler; Edge-Smoke-Test erzeugt am
  Dev-System ein echtes PDF. Rulings 4–7 im SDD-Ledger
  (`.superpowers/sdd/2026-09-14-pdf-erzeugung-fahierarchy-druck/progress.md`, Worktree).
- Nebenbefunde am Dev-System (nicht Teil der Spec, in [[fallstricke]] §8 festgehalten): lokaler
  `dotnet run` der Web-App = `Migrate()` gegen AKESQL20 (verhindert, Rollback bestaetigt);
  `dotnet run` in Development scheitert in `Build()` (DbContextFactory Singleton vs. scoped Options,
  pre-existing seit `a9475e2`).
- Status → `Testbereit` durch den qa-agent (gruener Gesamtnachweis + Deploy-Abschnitt). Wartet mit
  dem ganzen Buendel auf Schranke 2. Kein Merge, kein Push.
