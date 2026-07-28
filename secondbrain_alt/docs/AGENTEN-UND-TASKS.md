# Agenten & geplante Aufgaben — Anlage-Referenz

Stand: 2026-07-28. Was WO liegt, welcher BEFEHL was tut, und wie du die zwei
autonomen Trigger (Backlog-Erkennung, Freigabe-Erkennung) als Dauerbetrieb anlegst.
Die zwei manuellen Schranken (Spec-Freigabe, Merge) sind beide Ordner-Gesten —
siehe Abschnitt 6 und 7.

---

## 1. Die drei Subagenten (liegen bereits im Repo)

`.claude/agents/` — versioniert, in jeder Session verfügbar. Nichts anzulegen,
nur zur Übersicht:

| Datei | Zweck | Input (Brain) | Output (Brain) | Modell | Abbruch/Eskalation |
|---|---|---|---|---|---|
| `task-scout.md` | Findet Backlog-Dateien ohne Spec | `backlog/`, `specs/*` | nur Meldung (read-only) | haiku | `ESCALATE: vault missing` |
| `spec-agent.md` | Anforderung → vollständige Spec | `architektur/`, `codebase/`, `glossar/`, Backlog-Datei | 1 Datei in `specs/entwurf/` | sonnet | Blocker als erste offene Rückfrage |
| `qa-agent.md` | Beweis vor „Testbereit" | Spec, `docs/TESTSZENARIEN.md` | `status: Testbereit` + Evidenz | sonnet | nach 2 Fix-Versuchen `ESCALATE` |

Prüfen, dass Claude sie sieht:
```powershell
claude
# in der Session:
/agents
```
Sie erscheinen unter „project agents". Delegiert werden sie automatisch über ihre
`description` — oder du nennst sie explizit im Prompt („nutze den spec-agent").

---

## 2. Manuelle Befehle (Schritt 6 — der End-to-End-Testlauf)

Diese brauchst du zum Kennenlernen der Kette, BEVOR der Motor läuft.

### 2a. Spec-Lauf (Backlog → Entwurf)
```powershell
cd C:\Git\IDEAL-AKE-WMS
claude -p "Nutze task-scout, dann spec-agent: erstelle fuer alle unverarbeiteten Backlog-Dateien vollstaendige Specs in secondbrain/specs/entwurf/ (status: Entwurf). Nichts freigeben, keinen Code aendern." --allowedTools "Read" "Glob" "Grep" "Write" "Agent" --max-turns 30
```

### 2b. Dev+QA-Lauf (Freigegeben → Testbereit)
```powershell
cd C:\Git\IDEAL-AKE-WMS
claude -p "Freigegebene Spec secondbrain/specs/freigegeben/<DATEINAME>.md umsetzen. Lies zuerst den Abschnitt 'Freigabe-Antworten' als Auftrag; ist eine Frage unbeantwortet oder fehlt bei Varianten-Specs freigabe.entscheidung, NICHT umsetzen. Sonst: Worktree via scripts/new-worktree.ps1, Umsetzung gemaess CLAUDE.md-Workflow im Worktree, dann qa-agent (dotnet build + dotnet test muessen gruen sein, Beweis in die Spec, TESTSZENARIEN.md ergaenzen). Bei Erfolg status: Testbereit. Kein Merge, kein Push, main nicht anfassen." --allowedTools "Read" "Glob" "Grep" "Write" "Edit" "Agent" "Bash(dotnet build:*)" "Bash(dotnet test:*)" "Bash(git status:*)" "Bash(git add:*)" "Bash(git commit:*)" "Bash(git diff:*)" "Bash(git worktree:*)" "Bash(powershell -ExecutionPolicy Bypass -File scripts/new-worktree.ps1:*)" --max-turns 60
```

> `<DATEINAME>` durch den echten Spec-Dateinamen ersetzen. Erst wenn 2a+2b von
> Hand sauber liefen, weiter zum Motor (Abschnitt 3). Hinweis: der Watcher nutzt
> getrennte Turn-Limits (Spec 30, Dev 180); der manuelle 2b-Aufruf oben nutzt 60
> — fuer groessere Aufgaben `--max-turns` hochsetzen oder besser interaktiv
> (`claude` im Worktree) fortsetzen, dort gilt kein Limit.

---

## 3. Der autonome Motor (Watcher) — beide Trigger in einem

Statt zwei getrennter geplanter Tasks läuft EIN ereignisgetriebener Watcher,
der beide Quellen überwacht: neue Backlog-Datei → 2a, neue freigegebene Spec → 2b.
Das ist sparsamer (feuert nur bei echten Ereignissen, schont dein Max-Kontingent)
und robuster als ein Minutentakt-Scheduler.

### 3a. Erst im Vordergrund testen (zusehen!)
```powershell
cd C:\Git\IDEAL-AKE-WMS
pwsh -File scripts\watch-backlog.ps1 -NoOneDrive
```
Dann in einem zweiten Fenster eine Backlog-Datei ablegen und im Watcher-Log
(`scripts\logs\watcher-*.log`) zusehen. Abbruch mit `Ctrl+C`. `-NoOneDrive`
schaltet den Kollegen-Sync ab, solange du allein testest.

### 3b. Als Dauerbetrieb registrieren (Admin-PowerShell)
```powershell
pwsh -File C:\Git\IDEAL-AKE-WMS\scripts\register-watcher-task.ps1
Start-ScheduledTask -TaskName "IdealAkeWms-BrainWatcher"
```
Das legt EINEN geplanten Task an: Start bei Anmeldung, Auto-Restart bei Absturz,
kein Zeitlimit. Er startet den Watcher, der beide Trigger bedient — du legst also
NICHT zwei Aufgaben im Taskplaner an, sondern diese eine.

Einmalig noch Energieoptionen (Rechner darf nicht schlafen):
```powershell
powercfg /change standby-timeout-ac 0
```

### 3c. Betrieb beobachten / steuern
```powershell
# Live-Log:
Get-Content C:\Git\IDEAL-AKE-WMS\scripts\logs\watcher-*.log -Tail 50 -Wait
# Status:
Get-ScheduledTask -TaskName "IdealAkeWms-BrainWatcher"
# Stoppen / wieder starten:
Stop-ScheduledTask  -TaskName "IdealAkeWms-BrainWatcher"
Start-ScheduledTask -TaskName "IdealAkeWms-BrainWatcher"
# Ganz entfernen:
Unregister-ScheduledTask -TaskName "IdealAkeWms-BrainWatcher" -Confirm:$false
```

---

## 4. OneDrive-Sync für die Kollegen-Freigabe (Schritt 8, optional zuschaltbar)

Läuft im Watcher automatisch alle 5 min mit (sofern NICHT mit `-NoOneDrive`
gestartet). Einmal manuell testen:
```powershell
pwsh -File C:\Git\IDEAL-AKE-WMS\scripts\sync-onedrive-specs.ps1
```
Voraussetzung: OneDrive-Ordner `WMS-Freigaben` existiert und ist für die Kollegen
freigegeben. Der Watcher im Dauerbetrieb ruft dieses Skript selbst auf — kein
eigener Task nötig.

---

## 5. Zusammengefasst: was du tatsächlich anlegst

| Was | Wie oft | Befehl |
|---|---|---|
| Subagenten | schon da | — (nur `/agents` zum Prüfen) |
| Spec-Lauf manuell | zum Testen | Abschnitt 2a |
| Dev+QA-Lauf manuell | zum Testen | Abschnitt 2b |
| **Watcher als Dauer-Task** | **einmal** | Abschnitt 3b |
| OneDrive-Sync | im Watcher enthalten | — (3b deckt es ab) |

Genau **ein** geplanter Task (`IdealAkeWms-BrainWatcher`) trägt die ganze
Autonomie. Die zwei Schranken (Spec-Freigabe, Merge) bleiben manuell.

---

## 6. Freigabe einer Spec (Schranke 1) — die Geste in voller Form

1. Offene Rückfragen im Abschnitt **„Freigabe-Antworten"** der Spec beantworten
   (in Obsidian, je Frage in **fett** hinter dem Pfeil).
2. Bei Varianten-Specs zusätzlich im Frontmatter `freigabe.entscheidung: A`
   (oder B) setzen.
3. `status: Freigegeben` setzen.
4. Datei von `specs\entwurf\` nach `specs\freigegeben\` verschieben.
5. Committen. Der Watcher (oder dein manueller 2b-Lauf) übernimmt ab hier.

Ohne beantwortete Fragen bzw. ohne `freigabe.entscheidung` setzt der Dev-Lauf
die Spec zurück auf `Entwurf` und rührt keinen Code an — das ist die Sicherung.

---

## 7. Merge freigeben (Schranke 2) — die zweite Ordner-Geste

Symmetrisch zu Schranke 1, aber mit einem bewussten Unterschied: Die Geste
startet KEINEN Hintergrund-Lauf. Der Merge verändert `main` und ist die
riskanteste Operation — er darf nie aus einem Dateiereignis kommen. Deshalb
löst DU ihn bewusst aus; der Ordner ist nur das Signal, deine Anwesenheit die
Schranke.

1. Manuellen Test bestehen (Checkliste am Spec-Ende).
2. Spec von `specs\freigegeben\` nach `specs\merge-freigegeben\` verschieben.
3. Merge auslösen — zwei Modi:
```powershell
cd C:\Git\IDEAL-AKE-WMS
# du siehst jeden git-Schritt selbst:
pwsh -File scripts\approve-merge.ps1
# oder ein Agent fuehrt aus (du hast ja freigegeben):
pwsh -File scripts\approve-merge.ps1 -UseAgent
```

Das Skript liest den Branch aus dem Spec-Frontmatter, merged nach main, prüft
Build+Tests **auf main** (erst der Merge-Commit ist der Deploy-Stand), setzt
`status: Gemerged` und legt die Spec zur Ablage nach `freigegeben/` zurück
(`merge-freigegeben/` bleibt leer). `git push` und das Aufräumen des Worktrees
bleiben bewusst deine Hand — Push wegen des privaten GitHub, Worktree erst nach
verifiziertem Deploy.

Der autonome Watcher fasst `git merge` nie an; er stoppt immer bei `Testbereit`.
