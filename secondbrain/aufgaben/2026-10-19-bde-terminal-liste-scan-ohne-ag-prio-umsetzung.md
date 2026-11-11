---
typ: notiz
spec: "[[2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: BDE-Terminal — Liste nach Haupt-FA, Scan ohne AG, Prio

Spec [[2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-spec]] · **direkt im Bündel**, Start `b3daf566` (v1.63.0), Ziel v1.64.0,
TS-97, keine Migration. Deploy: Web. Antworten 1–3 + Freigabe-Nachtrag.

## Plan (kompakt — Details auf Datei:Zeile-Ebene in der Spec)

1. **Ordnen** — `BdeTerminalArbeitsgang` + `int? Prio`; Gruppen-Sortierung Prio (min, leer zuletzt), frühester Termin,
   OrderNumber; innerhalb wie heute. Tests: Prio, ohne Prio = heute, Kombigerät, Gleichstand. JSON `prio`.
2. **Resolver** — Schritt 3b: existierende Sub-FA ohne Familien-AG → NotInScope. Test.
3. **Mengen** — `NegativeMenge`-Guard in Pause/Finish/ReportPartial/ReportCompletion, FinishGroup ganz abweisen; `[Range]` im
   Edit-ViewModel + `asp-validation-for`/`min=0`. Tests M1–M6.
4. **FA-Baum** — `NamenLookup` mehrere → `WerkbankKuerzelFamilie.Bevorzugt`. Test.
5. **AppSetting** — `BdeTerminalMaxHauptFa` (Key, Seed, Settings-Gruppe BDE); `BdeTerminalController.Index` Master +
   MaxHauptFa als Hidden-Inputs. Test.
6. **Terminal-UI** (`frontend-design` vorher) — `#actionPanel` unter `#operationsCard` + „Gewählt“; Gruppierung/Aufklappen/
   „Weitere“; Markierung `aria-pressed` + `.bde-ag-gewaehlt`; Scan ohne AG (FA-4.2/4.3).
7. **Doku** — v1.64.0 ×2, Changelog, Hilfe (Andere Listen, Terminal, Familie), README, TS-97.

## Stand

- 2026-10-19: Umsetzung gestartet.
- 2026-10-19: feature-complete (`63bf006c`, `96e90402`, `5613e5a2`); Build 0 Fehler, 2111 Web- / 316 Service-Tests grün.
- 2026-10-19: QA: Q1 (Haken-Zeichen zerschossen), Q2 (Fokus nach Scan klappte beim nächsten Scan zu), Q3 (Markierung jenseits der
  Grenze) behoben `a9558301` → **Testbereit**. Offen nur Info Q4–Q8. Changelog [[2026-10-19-v1-64-0-bde-terminal-liste-scan-ohne-ag-prio]].
- Schranke 2 mit dem Bündel: TS-97, Manuelle Test-Checkliste in der Spec (19 Punkte).
