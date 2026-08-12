---
description: Freigegebene Spec im Worktree umsetzen inkl. QA bis Testbereit. Nicht fuer Epics.
argument-hint: <pfad-zur-freigegebenen-spec>
---
Freigegebene Spec umsetzen (Schranke 1 wurde genommen): $1

Diese Spec ist KEIN Epic (Epics laufen ueber /epic-stage bzw. run-epic-stage.ps1).

Pruefe zuerst:
- Frontmatter status muss Freigegeben sein UND worktree/branch muessen leer sein
  (sonst laeuft die Umsetzung schon -> NICHTS tun, beenden).
- Lies den Abschnitt 'Freigabe-Antworten' als verbindlichen Auftrag: er beantwortet
  die offenen Rueckfragen. Ist eine Rueckfrage unbeantwortet (nur Pfeil, keine Antwort)
  ODER ist es eine Varianten-Spec ohne gesetztes freigabe_entscheidung:
  NICHT umsetzen, Status auf Entwurf zuruecksetzen, Grund in die Spec schreiben, beenden.
- Gibt es einen Abschnitt 'Kritische Pruefung' mit offenen BLOCKER-Befunden, die weder
  ausgeraeumt noch in den Freigabe-Antworten beantwortet sind: NICHT umsetzen, Grund in
  die Spec schreiben, beenden. (Ein BLOCKER, der nachweislich geklaert wurde, ist ok.)

Dann:
0. SCHREIBZIELE (wichtig, siehe CLAUDE.md "Das Brain wird NICHT verzweigt"):
   - Anwendungscode, SQL/, docs/, Tests, Versions-Bump -> IN DEN WORKTREE.
   - Alle secondbrain/-Aenderungen (Spec-Status, worktree/branch, QA-Nachweis, Deploy-Abschnitt,
     Aufgaben-Notiz, codebase-Karte, Testindex, changelog) -> IMMER in den HAUPTCHECKOUT
     C:\Git\IDEAL-AKE-WMS\secondbrain\, NIE in <worktree>\secondbrain\.
   Der Worktree hat per sparse-checkout normalerweise gar kein secondbrain/. Ist dort doch eines
   sichtbar: nicht hineinschreiben, Fund melden.
1. Worktree anlegen: powershell -ExecutionPolicy Bypass -File scripts/new-worktree.ps1 -Slug <slug-OHNE-suffix-spec>
   (Spec 2026-07-28-foo-spec -> Slug 2026-07-28-foo). worktree + branch ins Spec-Frontmatter,
   status: InUmsetzung, Aufgaben-Datei in secondbrain/aufgaben/ anlegen.
2. IM WORKTREE umsetzen, gemaess CLAUDE.md-Workflow: superpowers:writing-plans, dann
   subagent-driven-development (unabhaengige Tasks parallel via dispatching-parallel-agents).
   **Betrifft die Aufgabe Views, CSS oder Oberflaechen-JavaScript: den Skill `frontend-design`
   VOR der Umsetzung explizit aufrufen** (Pflicht laut CLAUDE.md; nicht auf automatische
   Ausloesung verlassen). Dabei gilt Konsistenz vor Eigenstaendigkeit (Bootstrap 5, bestehende
   Muster) und Kontrast nach WCAG AA.
   Zwischenstaende regelmaessig committen ("wip: <slug>").
3. PFLICHT vor der QA-Phase: vollstaendigen Stand committen ("wip: <slug> feature-complete").
4. Qualitaet: superpowers:verification-before-completion + code-review. dotnet build und
   dotnet test muessen gruen sein - Ausgaben als Beweis in die Spec. docs/TESTSZENARIEN.md +
   secondbrain/tests/testszenarien-index.md ergaenzen. Nutze den Subagenten qa-agent.
   Der qa-agent finalisiert auch den Deploy-Abschnitt (web/service/migration + Publish-Befehle).
5. NUR bei Erfolg: status: Testbereit + manuelle Test-Checkliste ans Spec-Ende.
   Code im Worktree committen; die secondbrain/-Aenderungen SEPARAT im Hauptcheckout committen
   ("brain: <slug> testbereit") - zwei Commits in zwei Baeumen, das ist beabsichtigt.

Wenn die Aufgabe zu gross fuers Turn-Limit ist: Stand als "wip: <slug>" committen und in die
Spec schreiben, wo du stehst - NICHT unvollstaendig auf Testbereit setzen. (Zu gross generell?
-> Kandidat fuer split oder epic, Hinweis in die Spec.)

Verboten: Merge nach main, git push, Worktree loeschen, status Gemerged setzen (Schranke 2 = Mensch).
