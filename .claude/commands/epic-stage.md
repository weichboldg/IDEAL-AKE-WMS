---
description: Genau eine offene Etappe eines Epic abarbeiten (ein Worktree, mehrere Etappen).
argument-hint: <pfad-zur-epic-spec>
---
Epic-Etappenlauf: $1

Lies die Spec (epic: true) und ihre Etappen-Tabelle.

0. SCHREIBZIELE (siehe CLAUDE.md "Das Brain wird NICHT verzweigt"): Anwendungscode, SQL/, docs/,
   Tests -> IN DEN WORKTREE. Alle secondbrain/-Aenderungen (Etappen-Tabelle, Status,
   worktree/branch, Aufgaben-Notiz, QA-Nachweis) -> IMMER in den HAUPTCHECKOUT
   C:\Git\IDEAL-AKE-WMS\secondbrain\, NIE in <worktree>\secondbrain\.
1. Worktree anlegen falls noch nicht vorhanden (scripts/new-worktree.ps1 -Slug <slug-OHNE-suffix-spec>),
   sonst dort weiterarbeiten - NICHT neu anlegen. worktree/branch + status: InUmsetzung ins
   Frontmatter, Aufgaben-Datei anlegen falls fehlt.
2. Arbeite GENAU EINE offene Etappe ab (die erste mit Status offen). Testbar umsetzen, im
   Worktree committen ("epic <slug>: Etappe N - <titel>"), Etappe in der Tabelle auf erledigt
   + Commit-Hash setzen (Spec liegt im Hauptcheckout - dort aendern und separat committen).
   **Betrifft die Etappe Views, CSS oder Oberflaechen-JavaScript: den Skill `frontend-design`
   VOR der Umsetzung explizit aufrufen** (Pflicht laut CLAUDE.md). Konsistenz vor
   Eigenstaendigkeit (Bootstrap 5, bestehende Muster), Kontrast nach WCAG AA.
3. Sind noch Etappen offen: Status bleibt InUmsetzung, beenden - die naechste Etappe kommt beim
   naechsten Aufruf.
4. Erst wenn ALLE Etappen erledigt: qa-agent (build+test gruen, Testszenarien, Deploy-Abschnitt),
   dann status: Testbereit + manuelle Test-Checkliste.

Hinweis: Den Branch haelt der Mensch mit scripts/sync-worktree.ps1 auf main-Stand - du fuehrst
diesen Sync NICHT selbst aus.

Verboten: Merge nach main, git push, Worktree loeschen, status Gemerged setzen (Schranke 2 = Mensch).
