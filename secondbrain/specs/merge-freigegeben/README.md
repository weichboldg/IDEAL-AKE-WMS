# Merge-freigegeben (Schranke 2)

Spec hierher verschieben = **„manuell getestet, Merge freigegeben"**.

WICHTIG — anders als Schranke 1: Diese Geste startet KEINEN Hintergrund-Lauf.
Der Merge veraendert `main` und ist die riskanteste Operation; er darf nie aus
einem Dateiereignis heraus laufen. Deshalb loest DU ihn bewusst aus:

```powershell
cd C:\Git\IDEAL-AKE-WMS
# du siehst jeden git-Schritt:
pwsh -File scripts\approve-merge.ps1
# oder ein Agent fuehrt aus (du hast ja freigegeben):
pwsh -File scripts\approve-merge.ps1 -UseAgent
```

Das Skript merged den im Spec-Frontmatter genannten Branch nach main, prueft
Build+Tests, setzt `status: Gemerged` und legt die Spec zur Ablage nach
`freigegeben/` zurueck (dieser Ordner bleibt leer). `git push` und das
Aufraeumen des Worktrees bleiben bewusst deine Hand (Push = privates GitHub;
Worktree entfernen erst nach verifiziertem Deploy).

Der autonome Watcher stoppt weiterhin bei `Testbereit` — er merged nie.
