# =====================================================================
# sync-onedrive-specs.ps1
# ZWECK: Spiegelt die Spec-Freigabe in einen OneDrive-Ordner, damit
#        Kollegen OHNE GitHub-Account pruefen und freigeben koennen.
#        Richtung 1 (raus): repo specs\entwurf  -> OneDrive\entwurf
#        Richtung 2 (rein): OneDrive\freigegeben -> repo specs\freigegeben
#        Beim Einlesen wird status: im Frontmatter auf "Freigegeben"
#        gesetzt und die Entwurfskopien (repo + OneDrive) entfernt.
#        Die menschliche Geste ist NUR das Verschieben der Datei nach
#        OneDrive\freigegeben - alles andere macht dieses Skript.
# VORAUSSETZUNGEN: OneDrive-Ordner existiert und ist fuer die Kollegen
#        geteilt (Anleitung Schritt 8). PS 5.1 oder 7.
# NICHT AUTOMATISIERT:
#   - Loest KEINE OneDrive-Konfliktkopien auf (Dateien mit Geraete-
#     namen-Suffix werden uebersprungen und gemeldet - Handarbeit).
#   - Committet nichts nach git (macht der Watcher-Lauf bzw. du).
#   - Gibt nichts frei - die Geste bleibt beim Menschen.
# WARUM SO: OneDrive meldet bei .md keine Konflikte, sondern legt still
#        Zweitdateien an; deshalb Einweg-Spiegel je Richtung, Skip von
#        Konfliktkopien und "ein Schreiber pro Datei"-Disziplin.
# =====================================================================
[CmdletBinding()]
param(
    [string]$Repo = "C:\Git\IDEAL-AKE-WMS",
    [string]$OneDriveRoot = "$env:OneDrive\WMS-Freigaben"
)
$ErrorActionPreference = "Stop"

$repoEntwurf   = Join-Path $Repo "secondbrain\specs\entwurf"
$repoFrei      = Join-Path $Repo "secondbrain\specs\freigegeben"
$odEntwurf     = Join-Path $OneDriveRoot "entwurf"
$odFrei        = Join-Path $OneDriveRoot "freigegeben"

foreach ($d in @($odEntwurf, $odFrei)) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}
if (-not (Test-Path $repoEntwurf)) { Write-Error "Vault fehlt: $repoEntwurf. Erst setup-vault.ps1 ausfuehren." }

function Test-ConflictCopy([string]$Name) {
    # OneDrive-Konfliktkopien: "name-GERAETENAME.md" bzw. "name (1).md"
    return ($Name -match '-[A-Za-z0-9]+-(PC|LAPTOP|DESKTOP)[A-Za-z0-9\-]*\.md$') -or ($Name -match '\(\d+\)\.md$')
}

# --- Richtung 1: Entwuerfe rausspiegeln (nur neuere kopieren) ---------
Get-ChildItem $repoEntwurf -Filter *.md -File | ForEach-Object {
    $dest = Join-Path $odEntwurf $_.Name
    if (-not (Test-Path $dest) -or ($_.LastWriteTime -gt (Get-Item $dest).LastWriteTime)) {
        Copy-Item $_.FullName $dest -Force
        Write-Host "raus     $($_.Name)"
    }
}

# --- Richtung 2: Freigaben einlesen -----------------------------------
Get-ChildItem $odFrei -Filter *.md -File | ForEach-Object {
    if (Test-ConflictCopy $_.Name) {
        Write-Warning "KONFLIKTKOPIE uebersprungen (manuell klaeren): $($_.FullName)"
        return
    }
    $target = Join-Path $repoFrei $_.Name
    if (Test-Path $target) { return }   # schon eingelesen

    # Stabilitaets-Check: Datei muss vollstaendig synchronisiert sein
    $t0 = $_.LastWriteTime
    Start-Sleep -Seconds 3
    if ((Get-Item $_.FullName).LastWriteTime -ne $t0) {
        Write-Host "wartet   $($_.Name) (noch in Sync)"; return
    }

    $content = Get-Content $_.FullName -Raw -Encoding UTF8
    if ($content -match '(?m)^status:\s*\S+') {
        $content = $content -replace '(?m)^status:\s*\S+.*$', 'status: Freigegeben'
    } else {
        Write-Warning "Kein status:-Frontmatter in $($_.Name) - Datei uebersprungen."
        return
    }
    $content = $content -replace '(?m)^updated:\s*\S+.*$', ('updated: {0:yyyy-MM-dd}' -f (Get-Date))

    $content | Set-Content -Path $target -Encoding UTF8
    Write-Host "FREIGEGEBEN eingelesen: $($_.Name)"

    # Entwurfskopien aufraeumen (Verschieben, kein Loeschen von Inhalt:
    # die freigegebene Fassung ist ab jetzt die Wahrheit)
    $reD = Join-Path $repoEntwurf $_.Name
    $odD = Join-Path $odEntwurf  $_.Name
    if (Test-Path $reD) { Remove-Item $reD }
    if (Test-Path $odD) { Remove-Item $odD }
}

Write-Host "Sync fertig."
