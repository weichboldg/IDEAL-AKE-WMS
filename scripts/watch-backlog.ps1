# =====================================================================
# watch-backlog.ps1
# ZWECK: Der Motor der Pipeline. Ueberwacht ereignisgetrieben
#          secondbrain\backlog\           -> Spec-Lauf (task-scout + spec-agent)
#          secondbrain\specs\freigegeben\ -> Dev+QA-Lauf im Worktree
#        Zwei Modi (-Mode):
#          headless    : ruft "claude -p" selbst auf (voll autonom, Turn-Limit,
#                        kein Zusehen). Gut fuer simple Nacht-Batches.
#          interactive : MELDER-Modus. Schreibt den fertigen Auftrag nach
#                        secondbrain\inbox\ + Benachrichtigung; DU fuehrst ihn
#                        in deiner offenen Claude-Session per @ aus (voller
#                        Kontext, kein Turn-Limit, du siehst zu). EMPFOHLEN.
#        Prompts liegen als Dateien in secondbrain\prompts\ (versioniert,
#        auffindbar, in beiden Modi identisch).
#        Zusaetzlich alle $SyncMinutes ein OneDrive-Spec-Sync.
# VORAUSSETZUNGEN:
#   - Claude Code CLI installiert und EINMAL interaktiv eingeloggt (Max-Abo)
#   - PowerShell 7 empfohlen (laeuft auch unter 5.1)
#   - Rechner bleibt wach (Energieoptionen!)
# NICHT AUTOMATISIERT:
#   - Schranke 1 (Spec-Freigabe) und Schranke 2 (Merge) - bewusst Mensch.
#     Merge laeuft ueber scripts\approve-merge.ps1, NICHT ueber diesen Watcher.
#   - Epic-Specs (etappenweise via run-epic-stage.ps1), Anhaenge mit Bild/PDF.
#   - Keine Konfliktaufloesung, kein git push.
# WARUM SO:
#   - Ereignisgetrieben statt Minutentakt: claude -p zaehlt gegen dein Max-Limit.
#   - interactive-Modus loest das Turn-Limit-/Kontext-/Bild-Problem, weil DU
#     in einer offenen Session mit vollem Kontext ausfuehrst.
#   - Enge --allowedTools (nur headless) statt --dangerously-skip-permissions.
#   - Cooldown + Mutex: nie zwei headless-Laeufe parallel, keine Event-Stuerme.
# =====================================================================
[CmdletBinding()]
param(
    [ValidateSet("headless","interactive")]
    [string]$Mode = "interactive",
    [string]$Repo = "C:\Git\IDEAL-AKE-WMS",
    [string]$OneDriveRoot = "$env:OneDrive\WMS-Freigaben",
    [switch]$NoOneDrive,
    [int]$QuietSeconds = 8,
    [int]$CooldownSeconds = 120,
    [int]$SyncMinutes = 5,
    [int]$SpecMaxTurns = 30,
    [int]$DevMaxTurns = 180,
    [switch]$Notify        # Windows-Ton bei neuer Inbox-Meldung (interactive)
)
$ErrorActionPreference = "Stop"
Set-Location $Repo

$logDir   = Join-Path $Repo "scripts\logs"
$inboxDir = Join-Path $Repo "secondbrain\inbox"
$promptDir= Join-Path $Repo "secondbrain\prompts"
foreach ($d in @($logDir,$inboxDir)) { if (-not (Test-Path $d)) { New-Item -ItemType Directory $d | Out-Null } }
$logFile = Join-Path $logDir ("watcher-{0:yyyy-MM}.log" -f (Get-Date))

function Log([string]$msg) {
    $line = "{0:yyyy-MM-dd HH:mm:ss}  {1}" -f (Get-Date), $msg
    $line | Add-Content -Path $logFile -Encoding UTF8
    Write-Host $line
}

# --- Einzelinstanz-Schutz --------------------------------------------
$mutex = New-Object System.Threading.Mutex($false, "Global\IdealAkeWmsBrainWatcher")
if (-not $mutex.WaitOne(0)) { Write-Host "Watcher laeuft bereits - Ende."; exit 0 }

# --- Prompt aus Datei laden, Platzhalter ersetzen --------------------
function Get-Prompt([string]$Name, [hashtable]$Vars) {
    $p = Join-Path $promptDir $Name
    if (-not (Test-Path $p)) { throw "Prompt fehlt: $p" }
    $t = Get-Content $p -Raw
    foreach ($k in $Vars.Keys) { $t = $t.Replace($k, $Vars[$k]) }
    return $t
}

# --- headless: enge Allowlist ----------------------------------------
$AllowedTools = @(
    "Read", "Glob", "Grep", "Write", "Edit",
    "Bash(dotnet build:*)", "Bash(dotnet test:*)",
    "Bash(git status:*)", "Bash(git add:*)", "Bash(git commit:*)",
    "Bash(git log:*)", "Bash(git diff:*)", "Bash(git worktree:*)",
    "Bash(powershell -ExecutionPolicy Bypass -File scripts/new-worktree.ps1:*)",
    "Agent"
)

# --- Auftrag ausfuehren (headless) ODER melden (interactive) ---------
function Dispatch([string]$Prompt, [string]$Tag, [int]$MaxTurns) {
    if ($Mode -eq "headless") {
        Log "CLAUDE START [$Tag] (headless, max-turns=$MaxTurns)"
        try {
            & claude -p $Prompt --allowedTools $AllowedTools --max-turns $MaxTurns 2>&1 |
                ForEach-Object { $_ | Add-Content -Path $logFile -Encoding UTF8 }
            Log "CLAUDE ENDE  [$Tag] ExitCode=$LASTEXITCODE"
        } catch {
            Log ("CLAUDE FEHLER [{0}]: {1}" -f $Tag, $_.Exception.Message)
        }
    }
    else {
        # interactive: Auftrag in die Inbox schreiben, Mensch fuehrt per @ aus
        $stamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
        $file  = Join-Path $inboxDir ("{0}_{1}.md" -f $stamp, $Tag)
        @"
<!-- Auftrag vom Watcher ($Tag). In deiner offenen Claude-Session ausfuehren mit:
     @secondbrain/inbox/$(Split-Path $file -Leaf)
     Danach diese Datei loeschen (oder nach secondbrain/inbox/erledigt/ verschieben). -->

$Prompt
"@ | Set-Content -Path $file -Encoding UTF8
        Log "INBOX [$Tag]: $file  ->  in offener Session ausfuehren: @secondbrain/inbox/$(Split-Path $file -Leaf)"
        if ($Notify) { [console]::beep(880,300) }
    }
}

function Test-Stable([string]$Path) {
    if (-not (Test-Path $Path)) { return $false }
    $t0 = (Get-Item $Path).LastWriteTime
    Start-Sleep -Seconds $QuietSeconds
    if (-not (Test-Path $Path)) { return $false }
    return ((Get-Item $Path).LastWriteTime -eq $t0)
}

# --- FileSystemWatcher ------------------------------------------------
$fsw = New-Object System.IO.FileSystemWatcher
$fsw.Path = (Join-Path $Repo "secondbrain")
$fsw.Filter = "*.md"
$fsw.IncludeSubdirectories = $true
$fsw.NotifyFilter = [System.IO.NotifyFilters]"FileName, LastWrite"
Register-ObjectEvent $fsw Created -SourceIdentifier BrainCreated | Out-Null
Register-ObjectEvent $fsw Renamed -SourceIdentifier BrainRenamed | Out-Null
$fsw.EnableRaisingEvents = $true

Log "Watcher gestartet. Mode=$Mode Repo=$Repo OneDrive=$(if($NoOneDrive){'aus'}else{$OneDriveRoot})"
if ($Mode -eq "interactive") { Log "MELDER-Modus: Auftraege landen in secondbrain\inbox\ - in offener Claude-Session per @ ausfuehren." }
$lastClaudeRun = [DateTime]::MinValue
$lastSync      = [DateTime]::MinValue
$seen          = @{}

try {
    while ($true) {
        if (-not $NoOneDrive -and ((Get-Date) - $lastSync).TotalMinutes -ge $SyncMinutes) {
            try {
                & (Join-Path $Repo "scripts\sync-onedrive-specs.ps1") -Repo $Repo -OneDriveRoot $OneDriveRoot 2>&1 |
                    ForEach-Object { $_ | Add-Content -Path $logFile -Encoding UTF8 }
            } catch { Log ("SYNC FEHLER: " + $_.Exception.Message) }
            $lastSync = Get-Date
        }

        $ev = Wait-Event -Timeout 60
        if (-not $ev) { continue }
        $path = $ev.SourceEventArgs.FullPath
        Remove-Event -EventIdentifier $ev.EventIdentifier

        if ($seen.ContainsKey($path) -and ((Get-Date) - $seen[$path]).TotalSeconds -lt 120) { continue }
        $seen[$path] = Get-Date

        if ($path -notmatch '\.md$') { continue }
        if ($path -match '\\_templates\\|\\\.obsidian\\|\\anhaenge\\|\\prompts\\|\\inbox\\|\\ideen\\|README\.md$|HOME\.md$') { continue }

        # Cooldown nur im headless-Modus (interactive schreibt nur Dateien)
        if ($Mode -eq "headless" -and ((Get-Date) - $lastClaudeRun).TotalSeconds -lt $CooldownSeconds) {
            Log "Cooldown aktiv - warte: $path"
            Start-Sleep -Seconds $CooldownSeconds
        }
        if (-not (Test-Stable $path)) { Log "instabil/verschwunden: $path"; continue }

        if ($path -like "*\secondbrain\backlog\*") {
            Log "Backlog-Ereignis: $path"
            $bl = Get-Content $path -Raw -ErrorAction SilentlyContinue
            if ($Mode -eq "headless" -and $bl -match '(?im)^anhaenge:' -and $bl -match '(?i)\.(png|jpg|jpeg|gif|bmp|webp|pdf)') {
                Log "INTERAKTIV NOETIG: $path hat Bild-/PDF-Anhaenge. headless uebersprungen - interaktiv spezifizieren."
                continue
            }
            $lastClaudeRun = Get-Date
            Dispatch (Get-Prompt "spec.md" @{ "<BACKLOG_PATH>" = $path }) "SPEC" $SpecMaxTurns
        }
        elseif ($path -like "*\secondbrain\specs\freigegeben\*") {
            Log "Freigabe-Ereignis: $path"
            $sp = Get-Content $path -Raw -ErrorAction SilentlyContinue
            if ($sp -match '(?im)^epic:\s*true') {
                Log "EPIC erkannt: $path - Etappen via: pwsh -File scripts\run-epic-stage.ps1 -SpecPath '$path'"
                continue
            }
            $lastClaudeRun = Get-Date
            Dispatch (Get-Prompt "dev.md" @{ "<SPEC_PATH>" = $path }) "DEV" $DevMaxTurns
        }
    }
}
finally {
    Unregister-Event BrainCreated -ErrorAction SilentlyContinue
    Unregister-Event BrainRenamed -ErrorAction SilentlyContinue
    $fsw.Dispose(); $mutex.ReleaseMutex()
    Log "Watcher beendet."
}
