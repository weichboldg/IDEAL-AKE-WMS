# =====================================================================
# run-epic-stage.ps1
# ZWECK: Startet die naechste offene Etappe eines Epic-Worktrees.
#        Ein Epic laeuft bewusst NICHT vollautomatisch durch - du
#        stoesst jede Etappe an, siehst das Ergebnis, dann die naechste.
# WARUM MANUELL PRO ETAPPE: Ein grosses Paket am Stueck traegt Risiko;
#        etappenweise mit Blick des Menschen ist sicherer als ein
#        unbeaufsichtigter Dauerlauf ueber Stunden. Zwischen den Etappen
#        kannst du sync-worktree.ps1 laufen lassen und kurz gegenschauen.
# VORAUSSETZUNGEN: freigegebene Spec mit epic: true und Etappen-Tabelle;
#        Worktree existiert (wird beim ersten Lauf angelegt). Repo-Root.
# NICHT AUTOMATISIERT: Merge (Schranke 2), git push, sync (eigenes Skript).
# =====================================================================
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SpecPath,   # z.B. secondbrain\specs\freigegeben\2026-08-xy-spec.md
    [string]$Repo = "C:\Git\IDEAL-AKE-WMS",
    [int]$MaxTurns = 180
)
$ErrorActionPreference = "Stop"
Set-Location $Repo
if (-not (Test-Path $SpecPath)) { Write-Error "Spec nicht gefunden: $SpecPath" }

$content = Get-Content $SpecPath -Raw
if ($content -notmatch '(?im)^epic:\s*true') {
    Write-Warning "Diese Spec ist kein Epic (epic: true fehlt). Fuer normale Specs den Watcher/Standardlauf nutzen."
    exit 1
}

$AllowedTools = @(
    "Read", "Glob", "Grep", "Write", "Edit",
    "Bash(dotnet build:*)", "Bash(dotnet test:*)",
    "Bash(git status:*)", "Bash(git add:*)", "Bash(git commit:*)",
    "Bash(git log:*)", "Bash(git diff:*)", "Bash(git worktree:*)",
    "Bash(powershell -ExecutionPolicy Bypass -File scripts/new-worktree.ps1:*)",
    "Agent"
)

$prompt = @"
Epic-Etappenlauf fuer Spec: $SpecPath
Lies die Spec (epic: true) und ihre Etappen-Tabelle.
1. Worktree anlegen falls noch nicht vorhanden (scripts/new-worktree.ps1 -Slug <slug-ohne-suffix-spec>), sonst dort weiterarbeiten. worktree/branch + status: InUmsetzung ins Frontmatter, Aufgaben-Datei anlegen falls fehlt.
2. Arbeite GENAU EINE offene Etappe ab (die erste mit Status offen). Testbar umsetzen, im Worktree committen ("epic <slug>: Etappe N - <titel>"), Etappe in der Tabelle auf erledigt + Commit-Hash setzen.
3. Sind noch Etappen offen: Status bleibt InUmsetzung, beenden - die naechste Etappe kommt beim naechsten Aufruf dieses Skripts.
4. Erst wenn ALLE Etappen erledigt: qa-agent (build+test gruen, Testszenarien), dann status: Testbereit + manuelle Test-Checkliste.
Verboten: Merge nach main, git push, Worktree loeschen, status Gemerged.
"@

Write-Host "Starte naechste Epic-Etappe fuer $SpecPath ..."
& claude -p $prompt --allowedTools $AllowedTools --max-turns $MaxTurns
Write-Host "`nEtappe beendet (ExitCode=$LASTEXITCODE)."
Write-Host "Tipp: Etappen-Tabelle in der Spec pruefen. Vor der naechsten Etappe ggf.:"
Write-Host "  pwsh -File scripts\sync-worktree.ps1 -Slug <slug>"
Write-Host "Dann dieses Skript erneut aufrufen fuer die naechste Etappe."
