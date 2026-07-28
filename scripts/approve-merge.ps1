# =====================================================================
# approve-merge.ps1
# ZWECK: Schranke 2 als Ordner-Geste. Alles in
#        secondbrain\specs\merge-freigegeben\ gilt als "vom Menschen
#        zum Merge freigegeben". Das Skript merged den zugehoerigen
#        Feature-Branch nach main, prueft Build+Tests, setzt status:
#        Gemerged und aktualisiert Feature-Map + Changelog.
# WARUM ALS SKRIPT, NICHT ALS WATCHER-TRIGGER:
#        Der Merge veraendert main - die riskanteste Operation. Er darf
#        NIE aus einem Hintergrund-Dateiereignis heraus laufen. Deshalb
#        startest DU dieses Skript bewusst; der Ordner ist nur das
#        Freigabe-Signal, deine Anwesenheit die eigentliche Schranke.
# VORAUSSETZUNGEN:
#   - Manueller Test bestanden. Feature-Branch existiert, Spec traegt
#     worktree/branch im Frontmatter. Claude Code eingeloggt.
#   - Im Repo-Root ausfuehren.
# NICHT AUTOMATISIERT:
#   - git push (bleibt bewusst deine Hand - privates GitHub).
#   - Worktree-Entfernen (erst nach verifiziertem Deploy, siehe Ende).
#   - Konfliktaufloesung: bei Merge-Konflikt STOPPT das Skript und
#     ueberlaesst dir das Loesen.
# ABLAUF DEINER GESTE:
#   Spec von specs\freigegeben\ nach specs\merge-freigegeben\ verschieben,
#   dann dieses Skript starten. Fertig.
# =====================================================================
[CmdletBinding()]
param(
    [string]$Repo = "C:\Git\IDEAL-AKE-WMS",
    [switch]$UseAgent   # wenn gesetzt: Merge+Brain-Update via claude -p statt direkter git-Befehle
)
$ErrorActionPreference = "Stop"
Set-Location $Repo

$mergeDir = "secondbrain\specs\merge-freigegeben"
$specs = Get-ChildItem $mergeDir -Filter *.md -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -ne "README.md" }

if (-not $specs) {
    Write-Host "Nichts in $mergeDir - keine Merge-Freigabe vorhanden."
    exit 0
}

foreach ($spec in $specs) {
    Write-Host "`n=== Merge-Freigabe: $($spec.Name) ==="
    $content = Get-Content $spec.FullName -Raw

    # Branch aus dem Frontmatter lesen
    $branch = $null
    if ($content -match '(?m)^branch:\s*"?([^"\r\n]+)"?\s*$') { $branch = $Matches[1].Trim() }
    if (-not $branch) {
        Write-Warning "Kein branch: im Frontmatter von $($spec.Name) - uebersprungen."
        continue
    }
    if (-not (git branch --list $branch)) {
        Write-Warning "Branch '$branch' existiert nicht (schon gemerged/aufgeraeumt?) - uebersprungen."
        continue
    }

    if ($UseAgent) {
        # --- Variante Agent: Mensch hat freigegeben, Agent fuehrt aus ---
        $prompt = @"
Freigabe fuer Merge (Schranke 2 wurde genommen): $($spec.Name) liegt in specs/merge-freigegeben/, vom Menschen manuell getestet und freigegeben.
1. git checkout main; git merge $branch. Bei Konflikt STOPPEN und melden - nichts erzwingen.
2. dotnet build IdealAkeWms.slnx und dotnet test - muessen gruen sein (Nachweis nach dem Merge, denn erst der Merge-Commit ist der Deploy-Stand).
3. Spec-Frontmatter status: Gemerged, updated heute. Verschiebe die Spec von specs/merge-freigegeben/ zurueck nach specs/freigegeben/ (Ablage), damit merge-freigegeben/ leer bleibt.
4. secondbrain/feature-map.md: Feature auf Gemerged; secondbrain/changelog/ pruefen/ergaenzen.
5. Commit auf main ("merge: $branch -> main + brain update").
6. Gib zum Schluss die Deploy-Info aus der Spec aus: was muss deployt werden (deploy.web/service/migration) und die exakten Publish-Befehle aus dem Deploy-Abschnitt - auf main auszufuehren. Fuehre sie NICHT selbst aus.
Verboten: git push (macht der Mensch), Worktree entfernen, Branch loeschen (erst nach Deploy-Verifikation).
"@
        & claude -p $prompt `
            --allowedTools "Read" "Glob" "Grep" "Edit" `
                "Bash(git checkout:*)" "Bash(git merge:*)" "Bash(git add:*)" "Bash(git commit:*)" `
                "Bash(git status:*)" "Bash(git diff:*)" "Bash(dotnet build:*)" "Bash(dotnet test:*)" `
            --max-turns 40
    }
    else {
        # --- Variante direkt: du siehst jeden git-Schritt ---
        Write-Host "Merge $branch -> main ..."
        git checkout main
        git merge $branch
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "MERGE-KONFLIKT. Bitte manuell loesen, dann committen. Skript gestoppt."
            exit 1
        }
        Write-Host "Build + Tests auf main ..."
        dotnet build IdealAkeWms.slnx
        if ($LASTEXITCODE -ne 0) { Write-Warning "BUILD ROT nach Merge - pruefen!"; exit 1 }
        dotnet test
        if ($LASTEXITCODE -ne 0) { Write-Warning "TESTS ROT nach Merge - pruefen!"; exit 1 }

        # status: Gemerged + Ablage zuruecklegen
        $content = $content -replace '(?m)^status:\s*\S.*$', 'status: Gemerged'
        $content = $content -replace '(?m)^updated:\s*\S.*$', ('updated: {0:yyyy-MM-dd}' -f (Get-Date))
        $ablage = Join-Path "secondbrain\specs\freigegeben" $spec.Name
        $content | Set-Content -Path $ablage -Encoding UTF8
        Remove-Item $spec.FullName
        git add secondbrain/specs
        git commit -m "merge: $branch -> main"
        Write-Host "GEMERGED. Feature-Map/Changelog jetzt aktualisieren (z. B. via claude), dann 'git push' von Hand."
        # Deploy-Hinweis direkt aus der Spec ziehen
        $deployWeb     = $content -match '(?m)^\s*web:\s*true'
        $deployService = $content -match '(?m)^\s*service:\s*true'
        $deployMig     = $content -match '(?m)^\s*migration:\s*true'
        Write-Host "`n--- Deploy laut Spec ---"
        Write-Host ("  Web-App : {0}" -f ($(if($deployWeb){'JA'}else{'nein'})))
        Write-Host ("  Service : {0}" -f ($(if($deployService){'JA'}else{'nein'})))
        Write-Host ("  Migration: {0}" -f ($(if($deployMig){'JA'}else{'nein'})))
        if ($deployWeb) {
            Write-Host '  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb'
        }
        if ($deployService) {
            Write-Host '  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService'
        }
        Write-Host "  (Publish-Befehle stehen vollstaendig im Deploy-Abschnitt der Spec.)"
        Write-Host "`nWorktree erst nach Deploy-Verifikation entfernen:"
        Write-Host "  git worktree remove .claude/worktrees/<slug> ; git branch -d $branch"
    }
}
