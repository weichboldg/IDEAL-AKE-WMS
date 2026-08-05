# =====================================================================
# new-worktree.ps1
# ZWECK: Legt fuer eine freigegebene Spec einen isolierten Worktree +
#        Branch an (nie direkt auf main arbeiten). Idempotent: existiert
#        der Worktree schon, passiert nichts.
#        Blendet secondbrain/ per sparse-checkout AUS dem Worktree aus:
#        Das Brain ist kein Code und wird nicht verzweigt - Brain-Notizen
#        werden IMMER im Hauptcheckout gepflegt, damit Obsidian und das
#        HOME-Dashboard waehrend der Arbeit den echten Stand zeigen.
# VORAUSSETZUNGEN: git (>= 2.25 fuer sparse-checkout); Ausfuehren im
#        Repo-Root; main lokal aktuell.
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

# --- secondbrain/ aus dem Worktree ausblenden -------------------------
# Das Brain wird NICHT verzweigt. Ohne diesen Schritt bekommt jeder Worktree
# eine eigene Vault-Kopie; Agenten schreiben dann dorthin, und das Obsidian
# des Menschen (das auf den Hauptcheckout zeigt) sieht den Fortschritt nie.
# Fehlschlag ist nicht fatal - dann greift nur noch die Regel in CLAUDE.md.
try {
    git -C $path sparse-checkout set --no-cone '/*' '!/secondbrain/' 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0 -and -not (Test-Path (Join-Path $path "secondbrain"))) {
        Write-Host "secondbrain/ ist im Worktree ausgeblendet (sparse-checkout) - Brain-Notizen im Hauptcheckout pflegen."
    } else {
        Write-Warning "sparse-checkout fuer secondbrain/ griff nicht. Brain-Notizen TROTZDEM nur im Hauptcheckout aendern, nie unter $path\secondbrain."
    }
} catch {
    Write-Warning ("sparse-checkout nicht moeglich ({0}). Brain-Notizen nur im Hauptcheckout aendern, nie unter {1}\secondbrain." -f $_.Exception.Message, $path)
}

Write-Host "Claude Code IN diesem Ordner starten. Merge + Aufraeumen bleibt manuell."
