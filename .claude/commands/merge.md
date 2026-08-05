---
description: Merge-Freigabe ausfuehren (Schranke 2 wurde vom Menschen genommen). Kein push.
argument-hint: <pfad-zur-spec-in-merge-freigegeben>
---
Merge-Freigabe (Schranke 2 wurde genommen): $1
Vom Menschen manuell getestet und zum Merge freigegeben.

1. git checkout main; git merge <branch-aus-spec-frontmatter>. Bei Konflikt STOPPEN und melden - nichts erzwingen.
2. dotnet build IdealAkeWms.slnx und dotnet test - muessen gruen sein (Nachweis nach dem Merge,
   denn erst der Merge-Commit ist der Deploy-Stand).
3. Spec-Frontmatter status: Gemerged, updated heute. Spec von specs/merge-freigegeben/ zurueck nach
   specs/freigegeben/ verschieben (Ablage), damit merge-freigegeben/ leer bleibt.
4. secondbrain/feature-map.md: Feature auf Gemerged; secondbrain/changelog/ pruefen/ergaenzen.
5. Commit auf main ("merge: <branch> -> main + brain update").
6. Gib die Deploy-Info aus der Spec aus: was muss deployt werden (deploy.web/service/migration) und
   die exakten Publish-Befehle aus dem Deploy-Abschnitt. Fuehre sie NICHT selbst aus.

Verboten: git push (macht der Mensch), Worktree entfernen, Branch loeschen (erst nach Deploy-Verifikation).
