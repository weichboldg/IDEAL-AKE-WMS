# Agenten, Prompts & Betrieb — Referenz

Stand: 2026-07-29. Was WO liegt, welcher Befehl was tut, die zwei Betriebs-
Modi, und wie die groesseren Anforderungen (split / epic / Anhaenge) laufen.

Die zwei manuellen Schranken bleiben immer: Spec-Freigabe (Schranke 1) und
Merge (Schranke 2), beide als Ordner-Geste — siehe Abschnitt 7 und 8.

---

## 1. Die drei Subagenten (liegen im Repo)

`.claude/agents/` — versioniert, in jeder Session verfuegbar.

| Datei | Zweck | Input (Brain) | Output | Modell | Eskalation |
|---|---|---|---|---|---|
| `task-scout.md` | Backlog-Dateien ohne Spec finden | `backlog/`, `specs/*` | Meldung (read-only) | haiku | `ESCALATE: vault missing` |
| `spec-agent.md` | Anforderung → Spec(s); split/epic/bug/anhaenge | `architektur/`, `codebase/`, `glossar/`, Backlog + Anhaenge | Spec(s) in `specs/entwurf/`, ggf. Bug in `bugs/` | sonnet | Blocker als erste offene Rueckfrage |
| `qa-agent.md` | Beweis vor „Testbereit"; Deploy-Abschnitt finalisieren | Spec, `docs/TESTSZENARIEN.md` | `status: Testbereit` + Evidenz | sonnet | nach 2 Fix-Versuchen `ESCALATE` |

Pruefen: `claude` starten, `/agents` — sie erscheinen unter „project agents".

---

## 2. Die Prompt-Bibliothek (`secondbrain/prompts/`)

Alle Pipeline-Auftraege liegen als versionierte Dateien — auffindbar, nicht in
fluechtigen Chats verstreut, und in beiden Modi identisch verwendet:

| Prompt | Zweck |
|---|---|
| `spec.md` | Backlog → Spec(s) |
| `dev.md` | freigegebene Spec → Umsetzung + QA → Testbereit |
| `epic-stage.md` | naechste Etappe eines Epic |
| `merge.md` | Merge-Freigabe (Schranke 2) |

Platzhalter `<BACKLOG_PATH>` / `<SPEC_PATH>` — der Watcher ersetzt sie; interaktiv
den echten Pfad einsetzen oder nach der `@`-Referenz nennen.

---

## 3. Zwei Betriebs-Modi (der Kern)

Der Watcher hat einen `-Mode`-Schalter:

| Modus | Was der Watcher tut | Ausfuehrung | Wann |
|---|---|---|---|
| **interactive** (Standard) | schreibt den Auftrag nach `secondbrain/inbox/` + optional Ton | **DU** in offener Session per `@` | tagsueber, Zusehen, grosse/kontextreiche Aufgaben, Anhaenge |
| **headless** | ruft `claude -p` selbst mit enger Allowlist | autonom, Turn-Limit | simple, klar umrissene Nacht-Batches |

**interactive** loest die headless-Schwaechen (Turn-Limit, wenig Kontext, Bilder
unzuverlaessig), weil DU in einer offenen Session mit vollem Kontext ausfuehrst.
Der einzige Handgriff: ein `@`-Tippen pro Auftrag.

### interactive-Ablauf
```powershell
# Watcher als Melder starten (Ton bei neuem Auftrag):
cd C:\Git\IDEAL-AKE-WMS
pwsh -File scripts\watch-backlog.ps1 -Mode interactive -Notify -NoOneDrive
```
Neue Backlog-/Freigabe-Ereignisse landen als fertiger Auftrag in
`secondbrain\inbox\`. In deiner OFFENEN `claude`-Session:
```
@secondbrain/inbox/<datei>.md
```
Claude fuehrt ihn aus, du siehst zu. Danach die Inbox-Datei loeschen.

### headless-Ablauf
```powershell
pwsh -File scripts\watch-backlog.ps1 -Mode headless -NoOneDrive
```
Der Watcher startet `claude -p` selbst. Bild-/PDF-Anhaenge und Epic-Specs
werden dabei bewusst NICHT headless verarbeitet (Log-Meldung „interaktiv noetig").

---

## 4. Manuelle Einzel-Laeufe (ohne Watcher)

Zum Testen oder gezielt. Interaktiv (empfohlen) — in offener `claude`-Session:
```
@secondbrain/prompts/spec.md    (dann Backlog-Pfad nennen)
@secondbrain/prompts/dev.md     (dann Spec-Pfad nennen)
```
Oder headless als Einzelaufruf:
```powershell
claude -p "$(Get-Content secondbrain\prompts\spec.md -Raw)" --allowedTools "Read" "Glob" "Grep" "Write" "Agent" --max-turns 30
```

---

## 5. Grosse Anforderungen: split / epic / Anhaenge

Steuerung ueber das Backlog-Frontmatter (Details: `secondbrain/backlog/README.md`):

- **`split: true`** — mehrere einzeln mergbare Teil-Specs (`-teil-N-spec`) +
  Uebersicht. Jede laeuft einzeln durch die Pipeline. **Bevorzugt**, haelt den
  Abstand zu main klein.
- **`epic: true`** — EIN langlebiger Worktree, mehrere Etappen (je ein Commit),
  ein Merge am Ende. Fuer unteilbare/bewusst am Stueck gebaute Pakete. Laeuft
  bewusst NICHT ueber den Watcher:
  ```powershell
  # pro Etappe:
  pwsh -File scripts\run-epic-stage.ps1 -SpecPath secondbrain\specs\freigegeben\<spec>.md
  # dazwischen den Branch auf main-Stand halten:
  pwsh -File scripts\sync-worktree.ps1 -Slug <slug>
  ```
- **`anhaenge:`** — Input-Dateien pro Backlog in `backlog/anhaenge/<slug>/`.
  Bild/PDF nur interaktiv verlaesslich; der Watcher meldet solche Notizen im
  headless-Modus als „interaktiv noetig".

Faustregel: im Zweifel **split**; **epic** nur wenn wirklich unteilbar.

---

## 6. Dauerbetrieb registrieren

```powershell
# Admin-PowerShell (Standard = interactive; fuer headless -Mode im Skript anpassen):
pwsh -File C:\Git\IDEAL-AKE-WMS\scripts\register-watcher-task.ps1
Start-ScheduledTask -TaskName "IdealAkeWms-BrainWatcher"
powercfg /change standby-timeout-ac 0   # Rechner darf nicht schlafen
```
Steuern:
```powershell
Get-Content C:\Git\IDEAL-AKE-WMS\scripts\logs\watcher-*.log -Tail 50 -Wait
Stop-ScheduledTask  -TaskName "IdealAkeWms-BrainWatcher"
Start-ScheduledTask -TaskName "IdealAkeWms-BrainWatcher"
Unregister-ScheduledTask -TaskName "IdealAkeWms-BrainWatcher" -Confirm:$false
```
Genau EIN Task traegt die ganze Autonomie. Die zwei Schranken bleiben manuell.

> Hinweis: `register-watcher-task.ps1` startet den Watcher im Standard-Modus
> (interactive). Willst du den Dauer-Task headless, ergaenze `-Mode headless`
> im Argument des Task-Skripts.

---

## 7. Freigabe einer Spec (Schranke 1)

1. Rueckfragen im Abschnitt **„Freigabe-Antworten"** der Spec beantworten
   (Obsidian, je Frage in **fett** hinter dem Pfeil).
2. Bei Varianten-Specs `freigabe.entscheidung: A` (oder B) im Frontmatter.
3. `status: Freigegeben` setzen.
4. Datei von `specs\entwurf\` nach `specs\freigegeben\` verschieben.
5. Committen. Watcher/Dev-Lauf uebernimmt (bzw. meldet in die Inbox).

Ohne beantwortete Fragen bzw. ohne `freigabe.entscheidung` setzt der Dev-Lauf
die Spec zurueck auf `Entwurf` — die Sicherung.

---

## 8. Merge freigeben (Schranke 2)

Ordner-Geste, aber KEIN Hintergrund-Lauf (Merge veraendert main). Du loest aus:

1. Manuellen Test bestehen (Checkliste am Spec-Ende).
2. Spec von `specs\freigegeben\` nach `specs\merge-freigegeben\` verschieben.
3. Merge ausloesen:
```powershell
cd C:\Git\IDEAL-AKE-WMS
pwsh -File scripts\approve-merge.ps1           # du siehst jeden git-Schritt
pwsh -File scripts\approve-merge.ps1 -UseAgent # ein Agent fuehrt aus (du hast freigegeben)
```
Merged nach main, prueft Build+Tests auf main, setzt `status: Gemerged`, legt
die Spec zur Ablage nach `freigegeben/` zurueck, zeigt die Deploy-Info. `git push`
und Worktree-Aufraeumen (nach Deploy-Verifikation) bleiben deine Hand.

Der Watcher fasst `git merge` nie an; er stoppt immer bei `Testbereit`.

---

## 9. Was du tatsaechlich anlegst

| Was | Wie oft | Wie |
|---|---|---|
| Subagenten, Prompts | schon da | — |
| Watcher als Dauer-Task | einmal | Abschnitt 6 |
| Spec-Freigabe | pro Feature | Abschnitt 7 (Ordner-Geste) |
| Merge-Freigabe | pro Feature | Abschnitt 8 (Ordner-Geste) |
| Epic-Etappen / Worktree-Sync | pro Epic | Abschnitt 5 |
