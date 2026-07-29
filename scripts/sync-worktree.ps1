# =====================================================================
# sync-worktree.ps1
# ZWECK: Haelt einen langlebigen Epic-Worktree-Branch mit main synchron,
#        damit der Abstand (und damit das Merge-Risiko am Ende) klein
#        bleibt. Fuehrt main IN den Feature-Branch (nicht umgekehrt).
# WARUM: Ein grosses Paket, das lange in einem eigenen Worktree reift,
#        driftet sonst von main weg -> am Ende ein riesiger, riskanter
#        Merge. Regelmaessiges "main einholen" haelt den Delta klein.
# VORAUSSETZUNGEN: git; Ausfuehren im Repo-Root; Worktree existiert;
#        main lokal aktuell (ggf. vorher git fetch/pull auf main).
# NICHT AUTOMATISIERT / BEWUSST MANUELL:
#   - Konfliktaufloesung: bei Merge-Konflikt STOPPT das Skript. Ein Epic-
#     Sync-Konflikt braucht Urteilsvermoegen - den loest DU im Worktree,
#     danach committen. Das ist Absicht, kein Mangel.
#   - kein git push, kein Merge des Feature-Branch nach main.
# EMPFOHLENE FREQUENZ: 1x pro Arbeitstag am Epic, oder nach jedem groesseren
#   Merge, der auf main gelandet ist. Nicht ueber den Watcher automatisieren.
# =====================================================================
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Slug,
    [string]$Repo = "C:\Git\IDEAL-AKE-WMS"
)
$ErrorActionPreference = "Stop"
Set-Location $Repo
if (-not (Test-Path ".git")) { Write-Error "Bitte im Repo-Root ausfuehren." }

$wt = ".claude\worktrees\$Slug"
if (-not (Test-Path $wt)) { Write-Error "Worktree fehlt: $wt. Erst new-worktree.ps1 -Slug $Slug." }

# main lokal aktualisieren (fetch; fast-forward wenn moeglich)
Write-Host "Aktualisiere main ..."
git fetch origin main 2>&1 | Out-Host

# Im Worktree main einholen
Write-Host "Hole main in feature/$Slug (im Worktree $wt) ..."
Push-Location $wt
try {
    git merge --no-edit origin/main
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "MERGE-KONFLIKT beim Einholen von main. Bitte im Worktree loesen:"
        Write-Warning "  cd $wt ; <konflikte loesen> ; git add -A ; git commit"
        exit 1
    }
    Write-Host "OK - Epic-Branch ist wieder auf Stand von main."
} finally {
    Pop-Location
}
