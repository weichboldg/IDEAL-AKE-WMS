---
type: aufgabe
title: "Review der Anweisungen, Agenten und Befehle – Anpassung an Fable 5.2 – Modellwahl"
created: 2026-11-10
status: offen
---

# Review Anweisungen / Agenten / Befehle (2026-11-10)

Auftrag Gerald 2026-11-10. Geprüft (nur gelesen): `CLAUDE.md`, `.claude/agents/{spec-agent,qa-agent,task-scout}.md`,
`.claude/commands/{spec,review,dev,epic-stage,goal-run,merge}.md`, `.claude/settings*.json`, `secondbrain/prompts/*.md`,
`secondbrain/README.md`, [[cowork-projekt-ideal-ake-wms-anweisungen]]. Fable-5.2-Teil nach der offiziellen Anleitung
„Prompting Claude Fable 5" (platform.claude.com) und der Modellseite Fable 5.2 (Stand 2026-11-09).
Umsetzung: Änderungen an `.claude/` und `CLAUDE.md` macht Claude Code auf ausdrücklichen Auftrag; die Cowork-Anweisungen
ändert Gerald im Projekt.

## A. Fehler und Widersprüche (zuerst beheben)

| # | Befund | Beleg | Vorschlag |
|---|---|---|---|
| A1 | **Watcher-Prompts sind veraltet.** `secondbrain/prompts/epic-stage.md` fehlt der ganze Abschnitt SCHREIBZIELE (Brain nur im Hauptcheckout), die Nachtrag-Vorrangregel, die frontend-design-Pflicht und die QA-Rückkehrregel. `prompts/dev.md` fehlt die Nachtrag-Vorrangregel und der BLOCKER-Check aus „Kritische Prüfung“; `prompts/spec.md` fehlt der REWORK MODE. | `diff` commands/epic-stage.md vs prompts/epic-stage.md; prompts/dev.md Z. 6-11 vs commands/dev.md Z. 9-23 | **Eine Quelle:** `.claude/commands/*.md` nur noch als Einzeiler `@secondbrain/prompts/<x>.md` mit `$1` (wie `goal-run.md`), Inhalt nur in `secondbrain/prompts/`. Watcher und interaktiv nutzen dann denselben Text. |
| A2 | **prompts/dev.md widerspricht CLAUDE.md:** verlangt `superpowers:verification-before-completion + code-review` und `subagent-driven-development` parallel; CLAUDE.md streicht Selbstprüf-Anweisungen ausdrücklich und begrenzt Subagenten. | prompts/dev.md Z. 23-24, 28; CLAUDE.md „Skill-Workflow-Kette“ 3 und 5 | Mit A1 erledigt (commands-Fassung übernehmen). |
| A3 | **Wer setzt Testbereit?** CLAUDE.md und qa-agent: „nur der qa-agent“. dev.md Schritt 5 und epic-stage Schritt 4 lassen den Dev-Lauf Testbereit setzen. | CLAUDE.md Pipeline-Rollen; commands/dev.md Z. 49 | dev/epic/goal: „Status setzt der qa-agent; du ergänzt nur die Checkliste, falls er sie nicht geschrieben hat“. |
| A4 | **Bündel-Läufe brechen die Vorprüfung.** dev.md: „worktree/branch müssen leer sein, sonst nichts tun“. Seit dem IDEAL-Bündel muss jeder /dev-Befehl das per Zusatz aufheben. | commands/dev.md Z. 10-11 | Wie goal.md Schritt 1: „leer ODER gleich dem im Auftrag genannten Bündel-Worktree (dann dort weiterarbeiten, keinen neuen anlegen)“. |
| A5 | **merge.md kennt keine Bündel.** Ein Branch, viele Specs (IDEAL: ~40 Specs v1.31–v1.72). merge.md arbeitet je Spec, Schritt 3/4 nur für eine Spec. | commands/merge.md | Bündel-Variante: Eingabe Branch + Liste/Übersicht; alle Specs des Bündels auf Gemerged, feature-map und changelog je Spec; vor dem Merge `sync-worktree.ps1`-Stand prüfen. |
| A6 | **Veraltete Fakten in CLAUDE.md/README.** Tech-Stack nennt nur `AKESQL20`/`IDEAL_AKE_WMS` (IDEAL-Produktiv und DEV fehlen); „Web lokal: dotnet run“ statt DEV-IIS (`D:\Dev\scripts\dev-deploy.ps1`); README nennt „35 Keys“/„36-Key-Tabelle“ – der Bündel-Katalog hat 79 `new(`-Einträge in `ServiceSettingDefinitions.cs`. | CLAUDE.md Z. 6-7, 17-18; secondbrain/README.md | Umgebungstabelle (AKE-Test, IDEAL-Test, IDEAL-Produktiv, DEV AKE/IDEAL) in CLAUDE.md; Zählangaben streichen (veralten immer). |
| A7 | **settings.json:** `Bash(*)`, `PowerShell(*)`, `Write(*)` erlaubt, keine `deny`-Regeln; dazu Altpfade `AKEBDELight`, `get-shit-done`. Die harten Verbote (push, merge auf main, AKESQL20) stehen nur im Prompt. | .claude/settings.json | `deny`: `Bash(git push:*)`, `PowerShell(git push*)`, `Bash(git worktree remove:*)`, `Bash(git branch -D:*)`, Befehle mit `AKESQL20` bzw. Testserver-Namen; Altpfade entfernen. Technische Sperre statt Vertrauen. |
| A8 | **qa-agent DEV-Sicherung weicht vom Einrichtungsplan ab:** „`SData:BaseUrl` is left as is“, der Plan setzte sie leer. Buchung hängt dann allein an `SageLagerbuchungAktiv`. | qa-agent Punkt 6b; [[2026-10-29-dev-umgebung-lokal]] Schritt 3 | Bewusste Entscheidung festhalten (Sage-Lesen erlaubt) oder BaseUrl wieder leeren – zwei Schalter sind sicherer als einer. |
| A9 | **Cowork-Anweisungen kennen DEV nicht.** ABNAHME verbietet „andere Adressen“; DEV-Sites (localhost:8081/8082) sind damit tabu, obwohl sie für QA gedacht sind. | Projektanweisung TESTSYSTEME | DEV ergänzen: „nur wenn Auftrag/Protokoll sie nennt; dort sind Testdaten-Änderungen erlaubt“. |
| A10 | **Cowork: Git-Verbot und Brain-Commits.** Cowork darf nicht committen – Brain-Änderungen blieben zweimal unbemerkt liegen. Ein lesender `git worktree list` (2026-10-29) zeigte, dass „keine Git-Befehle“ auch Lesebefehle meint. | Verlauf 2026-10-29, 2026-11-06 | Regel ergänzen: „Jede eigene Brain-Änderung am Ende als Commit-Block für Claude Code mit expliziten Pfaden ausgeben“; „auch lesende Git-Befehle nicht“. |

## B. Lücken (fehlende Hinweise)

- **B1 spec-agent darf die DEV-Datenbanken lesen.** Viele Prüfabfragen (P7/P8/P9 im Etikett-Lauf) betreffen nur die WMS-DB. Mit
  `sqlcmd -S . -E -C -d IDEAL_WMS_DEV` (nur SELECT) könnte der spec-agent sie selbst beantworten – gekennzeichnet als „DEV-Kopie
  vom <Datum>“. Sage bleibt Prüfabfrage für Gerald. Dazu Bash in die Tools des spec-agent und eine Lese-Grenze.
- **B2 Spec-Länge.** Die letzte Spec hat 756 Zeilen und 10 Rückfragen. Vorschlag: Kopf mit „Kurzfassung (max. 10 Zeilen)“ und
  „Entscheidungen auf einen Blick“; Rückfragen nur, wo Code/DEV-DB nicht antworten können; Zielgröße nennen.
- **B3 review.md:** „Eine Prüfung ohne einen einzigen Befund ist verdächtig“ fördert erfundene Befunde. Besser: „Nur belegte
  Befunde. Keine Befunde ist ein gültiges Ergebnis, dann Liste, was geprüft wurde.“
- **B4 Standort in Befehlen:** /dev, /goal-run: Bezug auf `standort` im Frontmatter fehlt (qa-agent nutzt ihn); in dev.md erwähnen.
- **B5 Fallstricke als Gedächtnis:** fallstricke.md hat 1739 Zeilen. Regel für neue Einträge: eine Zeile Zusammenfassung oben,
  „Warum“, Beleg; falsche Einträge streichen statt ergänzen (passt zur Fable-Empfehlung „memory system“).
- **B6 Watcher:** `claude -p` ohne `--model`; `--max-turns 180` (Dev) kann Fable-Läufe abschneiden. Modell und Effort je Job setzen.

## C. Anpassungen für Fable 5.2 (laut Anthropic-Anleitung)

1. **Weniger Vorschrift, mehr Begründung.** „Skills für frühere Modelle sind oft zu vorschreibend und können die Qualität
   senken.“ → ponytail-Block in CLAUDE.md (≈40 Zeilen) auf 5 Zeilen kürzen; „PFLICHT“, „nicht verhandelbar“, Großschreibung
   abbauen; je Regel ein Satz Warum (Fable nutzt den Grund).
2. **Schlanke Steuerung statt Aufzählungen.** Ein kurzer Satz wirkt wie eine Liste. Kandidaten: Teile der Skill-Workflow-Kette,
   Wiederholungen der Schreibziele in jedem Befehl (einmal in CLAUDE.md, Befehle verweisen).
3. **Überbau verhindern, Hausregel schützen.** Anthropic-Baustein „keine Features/Abstraktionen über den Auftrag hinaus …“
   übernehmen, aber mit Ausnahme: „Melden statt still behandeln“ (Banner, Invarianten-Warnungen) bleibt Pflicht – sonst
   kollidiert „keine Fallbacks/Validierung für unmögliche Fälle“ mit der Projektregel.
4. **Fortschritt belegen.** Baustein „Before reporting progress, audit each claim against a tool result …“ in dev/goal/qa – deckt
   sich mit „Belegen statt deuten“.
5. **Grenzen ausdrücklich.** Fable handelt gelegentlich ungefragt (z. B. Sicherungs-Branches). In dev/goal: keine zusätzlichen
   Branches/Worktrees/Backups, keine Änderungen außerhalb der Spec.
6. **Autonomie im Watcher (headless).** Baustein „You are operating autonomously … End your turn only when the task is complete
   or you are blocked“ – verhindert Abbrüche mit „Ich werde jetzt …“.
7. **Subagenten neu regeln.** Fable delegiert zuverlässiger. Erlauben: parallele Such-/Leseaufträge, unabhängige Teilaufgaben.
   Unverändert: Code-Review des qa-agent synchron und im eigenen Lauf; ein frischer Prüf-Kontext ist laut Anthropic besser als
   Selbstkritik – genau das ist der qa-agent.
8. **Schlussbericht lesbar.** Baustein „Lead with the outcome … complete sentences, no arrow chains“ für Berichte an Gerald.
9. **Handeln statt überplanen.** „When you have enough information to act, act … give a recommendation, not an exhaustive survey“
   – für spec-agent (weniger Rückfragen) und Koordination.
10. **Nicht verlangen, Denkwege auszuschreiben** (löst bei Fable Refusals aus). Geprüft: kommt in den Dateien nicht vor.
11. **Effort statt Länge.** `high` Standard; `xhigh` für spec-agent/Review; `medium` für Routine (task-scout, Abnahme).
12. **Lange Läufe:** Timeouts/`--max-turns` anheben; keine Kontext-Restanzeigen einbauen.

## D. Modellwahl (Vorschlag)

| Zweck | Modell / Effort | Grund |
|---|---|---|
| Spec schreiben (spec-agent), /review | **Fable 5.2**, high/xhigh | Fehler in Specs kosten am meisten; Fable: Mehrdeutigkeit, Codesuche, Bug-Recall |
| /dev Einzelspec | **Opus 5.5**, high | Anthropic: „strongest daily driver“, halber Preis von Fable |
| /goal-Bündel, Epics | **Fable 5.2**, high | lange autonome Läufe, Instruktionstreue über Stunden |
| qa-agent | **Fable 5.2** (Review), sonst Opus 5.5 | anderes Modell als die Umsetzung = echte zweite Sicht |
| task-scout | **Haiku 5.5** (bleibt) | reine Dateisuche |
| Koordination (Cowork) | **Opus 5.5**; Fable für große Analysen | viele kurze Runden; Fable nur bei Bedarf |
| Browser-Abnahme | **Sonnet 5.5**, high | schnell, günstig, gute Bildauswertung; Opus bei unklaren Fällen |
| Merge | Opus 5.5 / Sonnet 5.5 | mechanisch, Konflikte selten |

Preise (je 1 Mio. Token Ein-/Ausgabe): Fable 5.2 8/40 $, Opus 5.5 4/20 $, Sonnet 5.5 2/10 $, Haiku 5.5 ab 0,10/0,50 $.
Fable läuft in Pro/Team-Plänen bis zu 50 % des Wochenkontingents. Welche Modelle in Geralds Claude-Code-Plan verfügbar
sind und ob Agent-Frontmatter `effort` unterstützt: ungeprüft.

## Reihenfolge

1. A1–A4 (eine Quelle für Prompts, Testbereit-Zuständigkeit, Bündel-Vorprüfung) – ein Claude-Code-Lauf, nur `.claude/` und
   `secondbrain/prompts/`.
2. A7 (deny-Regeln), A6 (Fakten).
3. C-Bausteine und D (Modelle in Agent-Frontmatter / Watcher).
4. A9/A10 in den Cowork-Projektanweisungen (Gerald).
5. A5 (Bündel-Merge) vor dem IDEAL-Bündel-Merge.
