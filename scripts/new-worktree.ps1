# =====================================================================
# new-worktree.ps1
# ZWECK: Legt fuer eine freigegebene Spec einen isolierten Worktree +
#        Branch an (nie direkt auf main arbeiten). Idempotent: existiert
#        der Worktree schon, passiert nichts.
# VORAUSSETZUNGEN: git; Ausfuehren im Repo-Root; main lokal aktuell.
# NICHT AUTOMATISIERT: Merge nach main (Schranke 2), Branch-/Worktree-
#        Aufraeumen (bewusst erst nach verifiziertem Deploy, siehe
#        PROJECT_STATUS-Praxis), kein Commit.
# =====================================================================
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Slug,
    [string]$BaseBranch = "main"
)
$ErrorActionPreference = "Stop"

if (-not (Test-Path ".git")) { Write-Error "Bitte im Repo-Root ausfuehren." }

$branch = "feature/$Slug"
$path   = ".claude\worktrees\$Slug"

if (Test-Path $path) {
    Write-Host "Worktree existiert bereits: $path (Branch $branch) - nichts zu tun."
    exit 0
}

# Branch schon vorhanden (z.B. nach Abbruch)? Dann ohne -b anhaengen.
$branchExists = (git branch --list $branch) -ne $null -and (git branch --list $branch).Trim() -ne ""
if ($branchExists) {
    git worktree add $path $branch
} else {
    git worktree add $path -b $branch $BaseBranch
}

Write-Host "Worktree angelegt: $path auf Branch $branch (Basis: $BaseBranch)."
Write-Host "Claude Code IN diesem Ordner starten. Merge + Aufraeumen bleibt manuell."
