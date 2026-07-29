Epic-Etappenlauf: <SPEC_PATH>

Lies die Spec (epic: true) und ihre Etappen-Tabelle.

1. Worktree anlegen falls noch nicht vorhanden (scripts/new-worktree.ps1 -Slug <slug-OHNE-suffix-spec>),
   sonst dort weiterarbeiten - NICHT neu anlegen. worktree/branch + status: InUmsetzung ins
   Frontmatter, Aufgaben-Datei anlegen falls fehlt.
2. Arbeite GENAU EINE offene Etappe ab (die erste mit Status offen). Testbar umsetzen, im
   Worktree committen ("epic <slug>: Etappe N - <titel>"), Etappe in der Tabelle auf erledigt
   + Commit-Hash setzen.
3. Sind noch Etappen offen: Status bleibt InUmsetzung, beenden - die naechste Etappe kommt beim
   naechsten Aufruf.
4. Erst wenn ALLE Etappen erledigt: qa-agent (build+test gruen, Testszenarien, Deploy-Abschnitt),
   dann status: Testbereit + manuelle Test-Checkliste.

Hinweis: Den Branch haelt der Mensch mit scripts/sync-worktree.ps1 auf main-Stand - du fuehrst
diesen Sync NICHT selbst aus.

Verboten: Merge nach main, git push, Worktree loeschen, status Gemerged setzen (Schranke 2 = Mensch).
