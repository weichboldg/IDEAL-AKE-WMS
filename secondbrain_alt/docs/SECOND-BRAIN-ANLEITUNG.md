# Second Brain + Agenten-Pipeline — Schritt-für-Schritt-Anleitung

Repo: `C:\Git\IDEAL-AKE-WMS` · Stand: 2026-07-29
Jeder Schritt hat ein **Warum** — das System soll verstanden, nicht nur ausgeführt werden.

> Diese Datei ist der **Aufbau-Pfad** (einmalige Einrichtung, Schritte 0–10).
> Für den täglichen Betrieb, die zwei Modi (interactive/headless), die
> Prompt-Bibliothek und split/epic/Anhaenge siehe `docs/AGENTEN-UND-TASKS.md`.
> Beide Schranken sind Ordner-Gesten (Spec-Freigabe → `specs/freigegeben/`,
> Merge → `specs/merge-freigegeben/`).

**Reihenfolge im Überblick:**

| # | Schritt | Ergebnis |
|---|---|---|
| 0 | Voraussetzungen prüfen | Werkzeuge bereit |
| 1 | Paket einspielen | Skripte + Agenten im Repo |
| 2 | Alten Vault ablösen, `.gitignore` bereinigen | sauberer Neustart |
| 3 | `setup-vault.ps1` ausführen | neuer Vault steht |
| 4 | Obsidian installieren + einrichten | du kannst nachsehen |
| 5 | CLAUDE.md-Diät + ADR-Nacherfassung | Brain hat Substanz, CLAUDE.md wird Constitution |
| 6 | Manueller End-to-End-Durchlauf | Kette bewiesen |
| 7 | Watcher im Vordergrund testen | Motor bewiesen |
| 8 | OneDrive-Freigabe für Kollegen | Schranke 1 teamfähig |
| 9 | Task registrieren | Dauerbetrieb |
| 10 | Guardrails schärfen | sicher autonom |

> **Warum diese Reihenfolge?** Brain vor Agenten (kein Agent ohne Kontext), manuell vor automatisch (Automatisierung einer kaputten Kette potenziert nur Fehler), Guardrails vor Dauerbetrieb.

---

## Schritt 0 — Voraussetzungen prüfen

```powershell
git --version
dotnet --version          # .NET 10 SDK
claude --version          # Claude Code CLI; einmal interaktiv "claude" starten und einloggen (Max-Abo)
$PSVersionTable.PSVersion # 5.1 vorhanden; pwsh 7 empfohlen: winget install Microsoft.PowerShell
```

**Warum:** Die headless-Läufe (`claude -p`) benutzen dein bestehendes interaktives Login. Ohne einmaliges Einloggen läuft nichts autonom.

**Wichtig (Stand Juli 2026):** `claude -p` zählt gegen dein Max-Abo-Limit (die angekündigte Trennung auf ein separates Agent-Credit wurde am 15.06.2026 pausiert). Deshalb ist die ganze Pipeline **ereignisgetrieben** statt im Minutentakt gebaut. Fremd-Orchestrierer (claude-flow/Ruflo etc.) sind seit 04.04.2026 mit Abo-Logins gesperrt — nur API-Key (teuer). Unser Motor bleibt darum PowerShell + Task Scheduler.

## Schritt 1 — Paket einspielen

ZIP nach `C:\Git\IDEAL-AKE-WMS` entpacken (Ordnerstruktur passt exakt):

```
scripts\setup-vault.ps1            # Vault-Struktur + Templates anlegen
scripts\new-worktree.ps1           # Worktree je Spec
scripts\sync-worktree.ps1          # Epic-Branch auf main-Stand halten
scripts\sync-onedrive-specs.ps1    # Kollegen-Freigabe via OneDrive
scripts\watch-backlog.ps1          # der Motor (Watcher; -Mode interactive|headless)
scripts\run-epic-stage.ps1         # naechste Etappe eines Epic
scripts\approve-merge.ps1          # Schranke 2: Merge-Freigabe ausfuehren
scripts\register-watcher-task.ps1  # Dauerbetrieb via Task Scheduler
.claude\agents\task-scout.md       # Erkennung (haiku, read-only)
.claude\agents\spec-agent.md       # Anforderung -> Spec (sonnet)
.claude\agents\qa-agent.md         # Beweis vor "Testbereit" (sonnet)
secondbrain\prompts\*.md           # versionierte Auftraege (spec/dev/epic-stage/merge)
docs\SECOND-BRAIN-ANLEITUNG.md     # dieses Dokument
docs\AGENTEN-UND-TASKS.md          # Betriebs-/Agenten-Referenz
```

Danach committen: `git add scripts .claude/agents docs/SECOND-BRAIN-ANLEITUNG.md && git commit -m "chore: second-brain pipeline scaffolding"`

**Warum `.claude/agents/` im Repo:** Subagenten sind Projektwissen wie Code — versioniert, für jede Session identisch verfügbar. (Aktuelles Format: Markdown mit Frontmatter `name`/`description`/`tools`/`model`; der frühere `/agents`-Wizard existiert nicht mehr, gepflegt wird per Datei.)

## Schritt 2 — Alten Vault ablösen + `.gitignore` bereinigen

1. **Obsidian schließen** (falls offen).
2. In `.gitignore` diese vier Hardlink-Zeilen **löschen** (der neue Vault arbeitet ohne Hardlink-Konstrukt):
   ```
   secondbrain/CLAUDE.md
   secondbrain/README.md
   secondbrain/PROJECT_STATUS.md
   secondbrain/ANALYSIS.md
   ```
   Die `.obsidian/workspace*`- und `.trash`-Zeilen bleiben — die sind richtig.
3. Den alten Vault musst du nicht selbst anfassen — `setup-vault.ps1` erkennt ihn und **benennt ihn nach `secondbrain_alt\` um** (nichts wird gelöscht).

**Warum umbenennen statt löschen:** Reversibel. Du hast entschieden „leerer Neustart, keine Übernahme" — aber endgültiges `git rm -r secondbrain_alt` machst du erst nach eigener Sichtung, in einem eigenen Commit.

## Schritt 3 — Vault anlegen

```powershell
cd C:\Git\IDEAL-AKE-WMS
powershell -ExecutionPolicy Bypass -File scripts\setup-vault.ps1
git add -A ; git commit -m "feat: second brain vault (fresh start)"
```

Das Skript ist **idempotent**: mehrfach ausführbar, überschreibt nie bestehende Dateien. Es legt an: Struktur (`architektur/adr`, `codebase/`, `glossar/`, `backlog/`, `specs/entwurf|freigegeben`, `aufgaben/`, `bugs/`, `tests/`, `changelog/`, `_templates/`), alle Templates (Spec/Aufgabe/ADR/Bug mit `status:`-Frontmatter), README mit den Pipeline-Regeln, HOME.md-Dashboard (Dataview), Glossar-Grundstock, Feature-Map- und Testszenarien-Index-Gerüst.

**Warum Vault = Ordner im Repo (statt Symlinks oder MCP-Bridge):** Agenten lesen/schreiben mit ihren nativen Datei-Tools — kein Zusatzdienst, keine laufende Obsidian-Instanz nötig, alles versioniert und atomar mit Branches. Die Obsidian-MCP-Bridge (Local-REST-API-Plugin) wäre nur sinnvoll, wenn Obsidian der Schreibkanal wäre — ist es nicht.

**Warum `status:` im Frontmatter UND Ordner:** Der Ordner ist die menschliche Geste (im Explorer machbar, auch für Kollegen ohne Obsidian), das Frontmatter die maschinenlesbare Wahrheit. Doppelt hält bei OneDrive-Latenz besser.

## Schritt 4 — Obsidian installieren + einrichten

1. **Installieren:** `winget install Obsidian.Obsidian` (oder Download von obsidian.md).
2. **Vault öffnen:** Obsidian starten → „Open folder as vault" → `C:\Git\IDEAL-AKE-WMS\secondbrain` wählen. Beim Start dem Vault vertrauen.
3. **Core-Plugins:** Settings → Core plugins: *Templates* aktivieren (Template folder: `_templates`), außerdem Backlinks, Outline, Tag pane.
4. **Community-Plugins — genau diese drei, in dieser Reihenfolge, einzeln testen:**
   - **Dataview** — treibt das `HOME.md`-Dashboard (Pipeline-Übersicht, offene Bugs, letzte ADRs). Settings: Standard reicht.
   - **Templater** — Datums-/Slug-Platzhalter beim Anlegen neuer Notizen. Settings → Template folder: `_templates`.
   - **Kanban** — optional, als *Ansicht* für den Backlog. **Regel:** Der Kanban ist Lesehilfe; die Wahrheit über den Status steht IMMER im Frontmatter der Spec/Aufgabe.
5. **Nicht installieren:** Obsidian Git (kollidiert mit Code-Commits — Notizen committest du mit dem normalen git-Workflow), Auto-Formatter/Linter, Auto-Note-Mover (verrauschen Diffs und kollidieren mit Agenten-Schreibvorgängen). Excalidraw nur, wenn du Skizzen willst — `.excalidraw`-Dateien sind kein Agenten-Futter. Faustregel: unter ~15 Plugins, Community-Plugins haben vollen Vault-Zugriff und führen JavaScript aus — nur Populäres, Gepflegtes.

**Warum so wenig Plugins:** Agenten arbeiten mit reinem Markdown + YAML. Jedes Plugin, das Dateien automatisch umschreibt, ist eine Fehlerquelle zwischen Mensch, Agent und git.

`.gitignore` deckt `workspace*.json` schon ab — der Rest von `.obsidian/` (Plugins, Hotkeys) wird mitversioniert, damit ein zweiter Rechner identisch aussieht.

## Schritt 5 — CLAUDE.md-Diät + einmalige ADR-Nacherfassung

Deine CLAUDE.md hat 381 Zeilen (~95 KB): Rollen-Tabellen, Zugriffsschutz-Matrix, Auth-Details, Fallstricke. Das ist Gold — aber am falschen Ort: Es kostet in **jeder** Session Kontext-Budget, und lange CLAUDE.md-Dateien werden nachweislich schlechter befolgt (Context Rot). Ziel: **CLAUDE.md unter ~150 Zeilen als Constitution**, Substanz ins Brain.

**5a — Nacherfassung + Migration in einer Claude-Code-Session (interaktiv, im Repo-Root):**

> Einmalige Nacherfassung ins Second Brain. Lies CLAUDE.md, PROJECT_STATUS.md, ANALYSIS.md, HANDOFF.md, nice_to_know.txt und überfliege docs/superpowers/specs/.
> 1. **ADRs:** Erfasse die bestehenden Architektur-Entscheidungen als MADR-ADRs in secondbrain/architektur/adr/ (Template _templates/adr.md, status: accepted, Vermerk „nachträglich erfasst"). Mindestens: Repository+Decorator-Pattern (CachedBomRepository), Dual-Auth/WindowsAutoLoginMiddleware-Design, AuditableEntity-Konzept, Migrations-/SQL-Disziplin (OBJECT_ID-Guards, FreshInstall), Listen-View-Pattern inkl. Server-Side-Spaltenfilter, Rollenkonzept (statische Keys, Admin-Wildcard, Wegfall Role.AdGroup), BOM-Quelle Sage-View mit OSEON-Fallback, DB-first ServiceSettings.
> 2. **Codebase-Karte:** Befülle secondbrain/codebase/{module,controller,services,datenmodell,integrationen}.md als navigierbare Karte mit Datei-Verweisen — kein Code-Dump. Die Zugriffsschutz-Tabelle aus CLAUDE.md gehört nach controller.md.
> 3. **Glossar:** Erweitere secondbrain/glossar/glossar.md um alle Rollen-Keys und Domänenbegriffe (BDE, Leitstand, Vorbau, Lagerbestellung/Glasbestellung, Teileverfolgung …).
> 4. **Fallstricke:** Übertrage „Bekannte Fallstricke" aus CLAUDE.md nach secondbrain/architektur/fallstricke.md, je Eintrag mit dem Warum.
> 5. **Feature-Map:** Erstbefüllung von secondbrain/feature-map.md aus PROJECT_STATUS.md + den Spec-Titeln in docs/superpowers/specs/ (Status: Gemerged für alles in v1.25.0).
> 6. **Testszenarien-Index:** secondbrain/tests/testszenarien-index.md aus den Kapiteln von docs/TESTSZENARIEN.md befüllen.
> Danach: Schreibe CLAUDE.md neu als schlanke Constitution (<150 Zeilen): Projektzweck (1 Satz), Tech-Stack-Zeile, Build/Test-Befehle oben, die harten Regeln (Migrations-/SQL-Disziplin, Audit-Felder, Muster-Pflichten, Worktree-Zwang, Sprachregel, Checkliste), Skill-Workflow-Kette, Brain-first-Direktive („Vor jeder Aufgabe secondbrain/ lesen — Einstieg secondbrain/README.md; keine Aufgabe ist fertig ohne Brain-Update") und Verweise ins Brain statt eingebetteter Tabellen. Lege die alte Fassung als docs/CLAUDE-full-backup-2026-07.md ab. Nichts löschen, nur verschieben und verweisen.

**Warum „nachträglich erfasste" ADRs mit heutigem Datum:** Ehrlichkeit im Format — ab jetzt gilt „nur noch vorwärts": jede neue Entscheidung ein neuer ADR, alte werden nie umgeschrieben, nur superseded.

**5b — Review:** Danach in Obsidian durchklicken (HOME.md-Dashboard!), stichprobenartig gegen den Code prüfen, committen. Das ist dein „Menschen schreiben"-Moment: Was der Agent nacherfasst hat, kuratierst du einmal.

## Schritt 6 — Manueller End-to-End-Durchlauf (der wichtigste Schritt)

**Warum:** Erst die Kette von Hand grün bekommen, dann den Motor dranhängen. So lernst du jede Stufe kennen und weißt später, wo es klemmt.

1. **Anforderung ablegen** — kleine, echte Aufgabe, z. B. `secondbrain\backlog\2026-07-28-beispiel.md`:
   > Auf der Lagerbestand-Übersicht soll die Spalte Lagerplatz einen Tooltip mit der Lagerplatz-Beschreibung zeigen.
2. **Spec-Lauf manuell** (im Repo-Root):
   ```powershell
   claude -p "Nutze task-scout, dann spec-agent: erstelle fuer alle unverarbeiteten Backlog-Dateien vollstaendige Specs in secondbrain/specs/entwurf/ (status: Entwurf). Nichts freigeben, keinen Code aendern." --allowedTools "Read" "Glob" "Grep" "Write" "Agent" --max-turns 30
   ```
3. **Schranke 1 von Hand:** Spec lesen (Obsidian oder Editor), offene Rückfragen ggf. direkt in der Spec beantworten, dann Datei nach `specs\freigegeben\` **verschieben** und im Frontmatter `status: Freigegeben` setzen. Commit.
4. **Dev+QA-Lauf manuell:**
   ```powershell
   claude -p "Freigegebene Spec secondbrain/specs/freigegeben/<datei>.md umsetzen: Worktree via scripts/new-worktree.ps1, Umsetzung gemaess CLAUDE.md-Workflow im Worktree, dann qa-agent (dotnet build + dotnet test muessen gruen sein, Beweis in die Spec, TESTSZENARIEN.md ergaenzen). Bei Erfolg status: Testbereit. Kein Merge, kein Push, main nicht anfassen." --allowedTools "Read" "Glob" "Grep" "Write" "Edit" "Agent" "Bash(dotnet build:*)" "Bash(dotnet test:*)" "Bash(git status:*)" "Bash(git add:*)" "Bash(git commit:*)" "Bash(git diff:*)" "Bash(git worktree:*)" "Bash(powershell -ExecutionPolicy Bypass -File scripts/new-worktree.ps1:*)" --max-turns 60
   ```
5. **Schranke 2 — Merge-Freigabe (Ordner-Geste):** Manuelle Test-Checkliste aus der Spec abarbeiten (`dotnet run` bzw. lokaler IIS Express). Wenn gut: Spec von `specs\freigegeben\` nach `specs\merge-freigegeben\` verschieben und `pwsh -File scripts\approve-merge.ps1 -UseAgent` starten (merged nach main, prüft Build+Tests, setzt `status: Gemerged`, aktualisiert Feature-Map + Changelog). `git push` und Worktree-Aufräumen bleiben deine Hand — Worktree erst nach verifiziertem Deploy. Siehe `docs/AGENTEN-UND-TASKS.md` Abschnitt 7.

**Erst wenn dieser Durchlauf sauber war, weiter zu Schritt 7.**

## Schritt 7 — Watcher im Vordergrund testen

```powershell
cd C:\Git\IDEAL-AKE-WMS
# interactive (Standard): meldet Auftraege in secondbrain\inbox\, du fuehrst per @ aus
pwsh -File scripts\watch-backlog.ps1 -Mode interactive -Notify -NoOneDrive
# oder headless: Watcher startet claude -p selbst
pwsh -File scripts\watch-backlog.ps1 -Mode headless -NoOneDrive
```

Im **interactive**-Modus (empfohlen) legst du eine Backlog-Datei ab, der Watcher schreibt den fertigen Auftrag nach `secondbrain\inbox\` und piept; in deiner offenen `claude`-Session tippst du `@secondbrain/inbox/<datei>.md`. Im **headless**-Modus startet der Watcher `claude -p` selbst. Beides loggt nach `scripts\logs\watcher-YYYY-MM.log`. Abbruch: `Ctrl+C`.

**Warum zwei Modi:** headless ist voll autonom, hat aber Turn-Limit, wenig Kontext und liest Bilder unzuverlaessig — gut fuer simple Nacht-Batches. interactive verbindet autonome *Erkennung* mit interaktiver *Ausfuehrung* (voller Kontext, kein Limit, du siehst zu), zum Preis eines `@`-Tastendrucks. Details: `docs/AGENTEN-UND-TASKS.md` Abschnitt 3.

**Warum ereignisgetrieben statt Polling:** Jeder `claude -p`-Lauf kostet Abo-Kontingent. Der Watcher feuert nur bei echten Dateiereignissen, hat Cooldown (headless), Event-Dedupe und einen Mutex gegen Doppelstart. Das umgeht auch den bekannten Windows-Bug verwaister headless-`claude`-Prozesse beim naiven Minutentakt-Scheduling.

**Warum enge `--allowedTools` statt `--dangerously-skip-permissions`:** Deine `settings.json` erlaubt interaktiv `Bash(*)` — okay, wenn du danebensitzt. Headless gilt: nur Lesen/Schreiben, `dotnet build/test`, eng begrenzte git-Kommandos, Worktree-Skript. Kein `git push`, kein `rm`, kein Zugriff auf Secrets. Fail-loud: Was nicht erlaubt ist, schlägt fehl und steht im Log.

## Schritt 8 — OneDrive-Freigabe für die Kollegen (Schranke 1 teamfähig)

1. In deinem OneDrive einen Ordner `WMS-Freigaben` anlegen und für die 1–2 Kollegen **freigeben** (Bearbeiten-Rechte). Unterordner `entwurf` und `freigegeben` legt das Sync-Skript selbst an.
2. Einmal testen: `pwsh -File scripts\sync-onedrive-specs.ps1`
3. Ablauf für Kollegen (eine Zeile Schulung): *„Spec in `entwurf` lesen — wenn okay, Datei nach `freigegeben` verschieben. Fertig."* Das Skript (läuft alle 5 min im Watcher) holt die Datei ins Repo, setzt `status: Freigegeben`, räumt die Entwurfskopien weg — und die Ankunft im Repo-Ordner triggert automatisch den Dev+QA-Lauf.

**Warum nur der Spec-Ordner in OneDrive, nie das Repo:** OneDrive darf `.git`, Worktrees und Build-Artefakte niemals anfassen. **Warum Konfliktkopien übersprungen werden:** OneDrive meldet bei gleichzeitiger `.md`-Bearbeitung keinen Konflikt, sondern legt still eine Zweitdatei mit Gerätenamen-Suffix an — das Skript verarbeitet solche Dateien nicht, sondern warnt. Disziplin: ein Schreiber pro Datei; Kollegen kommentieren am Spec-Ende oder geben frei, sie editieren nicht parallel.

## Schritt 9 — Dauerbetrieb registrieren

```powershell
# Admin-PowerShell:
pwsh -File C:\Git\IDEAL-AKE-WMS\scripts\register-watcher-task.ps1
Start-ScheduledTask -TaskName "IdealAkeWms-BrainWatcher"
Get-Content C:\Git\IDEAL-AKE-WMS\scripts\logs\watcher-*.log -Tail 50 -Wait
```

Plus einmalig Energieoptionen: Ruhezustand aus (`powercfg /change standby-timeout-ac 0`).

**Warum „bei Anmeldung" statt „bei Systemstart":** `claude` braucht dein Benutzerprofil (Login-Token). Der Task läuft interaktiv in deiner Session, startet bei Absturz bis zu 3× neu, ohne Zeitlimit. Der Rechner läuft bei dir ohnehin durchgehend — nur angemeldet und wach muss er sein.

## Schritt 10 — Guardrails schärfen (empfohlen)

Ergänze in `.claude/settings.json` einen **deny**-Block (deny schlägt allow, gilt auch headless und für Subagenten):

```json
"permissions": {
  "deny": [
    "Bash(git push:*)",
    "Bash(rm -rf:*)",
    "Bash(git merge:*)",
    "Bash(git worktree remove:*)",
    "Read(./**/appsettings*Production*.json)",
    "Read(./**/.env*)"
  ],
  "allow": [ "...deine bestehenden Eintraege..." ]
}
```

**Warum trotz privatem Repo:** Prompt-Injection-Risiko ist bei euch klein — privates Repo, keine fremden Beiträge, alle Inhalte stammen von dir/den Kollegen. Aber Autonomie-Risiko bleibt: Ein Agent kann sich verrennen. Defense-in-depth = enge Allowlist (Watcher) + Deny-Liste (settings) + Worktree-Isolation (git als Rollback) + `--max-turns` (Budget-Deckel) + zwei menschliche Schranken. `git push` bleibt bewusst dir vorbehalten — das private GitHub-Repo bekommt nur, was du gemergt hast.

---

## Betrieb im Alltag (Soll-Zustand)

1. Du (oder ein Kollege) wirfst eine formlose Anforderung in `secondbrain\backlog\`.
2. Watcher → Spec-Agent → Spec liegt in `specs\entwurf\` (und via Sync in OneDrive).
3. **Schranke 1:** Du oder Kollege verschiebt die Datei nach `freigegeben`.
4. Watcher → Worktree → Umsetzung (Skill-Workflow) → QA (Build+Tests grün, Testszenarien) → `status: Testbereit` + manuelle Test-Checkliste.
5. **Schranke 2 (Ordner-Geste):** Du testest manuell; wenn gut, verschiebst du die Spec nach `specs\merge-freigegeben\` und startest `approve-merge.ps1`. Der merged, prüft Build+Tests auf main, setzt `Gemerged`, aktualisiert Feature-Map + Changelog. `git push` + Worktree-Aufräumen (nach Deploy-Verifikation) bleiben deine Hand.

Dein manueller Aufwand: zwei Ordner-Gesten und ein manueller Test. Alles andere: Brain-first, mit Beweis statt Behauptung. Der Watcher merged nie selbst — er stoppt immer bei `Testbereit`.

## Troubleshooting

| Symptom | Ursache/Abhilfe |
|---|---|
| Watcher startet Claude nie | Log prüfen; `claude --version` im selben Benutzerkontext; Mutex? (läuft schon eine Instanz) |
| `claude -p` bricht mit Rate-Limit ab | Max-Abo-Fenster erschöpft — Lauf wiederholt sich beim nächsten Ereignis; ggf. `-CooldownSeconds` erhöhen, `model: haiku` im task-scout belassen |
| Spec wird doppelt erzeugt | `source_backlog` im Frontmatter fehlt/falsch — task-scout erkennt Verarbeitung darüber |
| Freigabe kommt nicht an | OneDrive-Sync-Latenz (Skript wartet auf stabile Datei) oder Konfliktkopie (Warnung im Log, manuell klären) |
| Task läuft nach Reboot nicht | Du warst nicht angemeldet — Trigger ist AtLogOn; anmelden genügt |
| `approve-merge.ps1`: „Kein branch im Frontmatter" | Die Spec trägt kein `branch:` — vom Dev-Lauf nicht gesetzt; Branch von Hand ins Frontmatter eintragen |
| Merge-Konflikt beim Approve | Skript stoppt bewusst; Konflikt manuell lösen, committen, dann Spec-Status selbst auf `Gemerged` setzen |
| Verwaiste claude-Prozesse | `Get-Process claude* \| Stop-Process`; tritt mit dem Watcher-Design normalerweise nicht auf |
