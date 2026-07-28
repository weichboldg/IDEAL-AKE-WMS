# =====================================================================
# watch-backlog.ps1
# ZWECK: Der Motor der Pipeline. Ueberwacht ereignisgetrieben
#          secondbrain\backlog\           -> Spec-Lauf (task-scout + spec-agent)
#          secondbrain\specs\freigegeben\ -> Dev+QA-Lauf im Worktree
#        und ruft dafuer headless "claude -p" mit ENGER Tool-Allowlist
#        auf. Zusaetzlich alle $SyncMinutes ein OneDrive-Spec-Sync.
# VORAUSSETZUNGEN:
#   - Claude Code CLI installiert und EINMAL interaktiv eingeloggt (Max-Abo)
#   - PowerShell 7 empfohlen (laeuft auch unter 5.1)
#   - Rechner bleibt wach (Energieoptionen!)
# NICHT AUTOMATISIERT:
#   - Schranke 1 (Spec-Freigabe) und Schranke 2 (Merge) - bewusst Mensch.
#     Merge laeuft ueber scripts\approve-merge.ps1, NICHT ueber diesen Watcher
#     (ein Dateiereignis darf nie main veraendern).
#   - Keine Konfliktaufloesung, kein git push.
# WARUM SO:
#   - Ereignisgetrieben statt Minutentakt: claude -p zaehlt gegen dein
#     Max-Abo-Limit - der Watcher feuert nur bei echten Aenderungen.
#   - Debounce + Ruhefenster: OneDrive/Editoren schreiben mehrfach;
#     wir starten erst, wenn die Datei stabil ist.
#   - Enge --allowedTools statt --dangerously-skip-permissions:
#     fail-loud Automatisierung, kein Blanko-Scheck.
#   - Cooldown + Mutex: nie zwei Claude-Laeufe parallel, keine Event-Stuerme.
# =====================================================================
[CmdletBinding()]
param(
    [string]$Repo = "C:\Git\IDEAL-AKE-WMS",
    [string]$OneDriveRoot = "$env:OneDrive\WMS-Freigaben",
    [switch]$NoOneDrive,
    [int]$QuietSeconds = 8,
    [int]$CooldownSeconds = 120,
    [int]$SyncMinutes = 5,
    [int]$SpecMaxTurns = 30,
    [int]$DevMaxTurns = 180
)
$ErrorActionPreference = "Stop"
Set-Location $Repo

$logDir = Join-Path $Repo "scripts\logs"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory $logDir | Out-Null }
$logFile = Join-Path $logDir ("watcher-{0:yyyy-MM}.log" -f (Get-Date))

function Log([string]$msg) {
    $line = "{0:yyyy-MM-dd HH:mm:ss}  {1}" -f (Get-Date), $msg
    $line | Add-Content -Path $logFile -Encoding UTF8
    Write-Host $line
}

# --- Einzelinstanz-Schutz --------------------------------------------
$mutex = New-Object System.Threading.Mutex($false, "Global\IdealAkeWmsBrainWatcher")
if (-not $mutex.WaitOne(0)) { Write-Host "Watcher laeuft bereits - Ende."; exit 0 }

# --- Claude-Aufruf mit enger Allowlist -------------------------------
$AllowedTools = @(
    "Read", "Glob", "Grep", "Write", "Edit",
    "Bash(dotnet build:*)", "Bash(dotnet test:*)",
    "Bash(git status:*)", "Bash(git add:*)", "Bash(git commit:*)",
    "Bash(git log:*)", "Bash(git diff:*)", "Bash(git worktree:*)",
    "Bash(powershell -ExecutionPolicy Bypass -File scripts/new-worktree.ps1:*)",
    "Agent"
)  # als Array: PowerShell uebergibt jedes Element als eigenes Argument

function Invoke-Claude([string]$Prompt, [string]$Tag, [int]$MaxTurns) {
    Log "CLAUDE START [$Tag] (max-turns=$MaxTurns)"
    try {
        # Ausgabe mitloggen; Fehler brechen den Watcher nicht ab
        & claude -p $Prompt --allowedTools $AllowedTools --max-turns $MaxTurns 2>&1 |
            ForEach-Object { $_ | Add-Content -Path $logFile -Encoding UTF8 }
        Log "CLAUDE ENDE  [$Tag] ExitCode=$LASTEXITCODE"
    } catch {
        Log ("CLAUDE FEHLER [{0}]: {1}" -f $Tag, $_.Exception.Message)
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

Log "Watcher gestartet. Repo=$Repo OneDrive=$(if($NoOneDrive){'aus'}else{$OneDriveRoot})"
$lastClaudeRun = [DateTime]::MinValue
$lastSync      = [DateTime]::MinValue
$seen          = @{}   # Pfad -> letzte Verarbeitung (Event-Dedupe)

try {
    while ($true) {
        # --- periodischer OneDrive-Sync (bringt Freigaben ins Repo,
        #     die Datei-Ankunft in specs\freigegeben feuert dann den FSW)
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

        # Dedupe: gleiche Datei max. alle 2 Minuten
        if ($seen.ContainsKey($path) -and ((Get-Date) - $seen[$path]).TotalSeconds -lt 120) { continue }
        $seen[$path] = Get-Date

        if ($path -notmatch '\.md$') { continue }
        if ($path -match '\\_templates\\|\\\.obsidian\\|README\.md$|HOME\.md$') { continue }

        # globaler Cooldown zwischen Claude-Laeufen
        if (((Get-Date) - $lastClaudeRun).TotalSeconds -lt $CooldownSeconds) {
            Log "Cooldown aktiv - Event vorgemerkt: $path"
            Start-Sleep -Seconds $CooldownSeconds
        }
        if (-not (Test-Stable $path)) { Log "instabil/verschwunden: $path"; continue }

        if ($path -like "*\secondbrain\backlog\*") {
            Log "Backlog-Ereignis: $path"
            $lastClaudeRun = Get-Date
            Invoke-Claude @"
Neue Backlog-Datei erkannt: $path
1. Nutze den Subagenten task-scout, um zu pruefen, welche Backlog-Dateien noch keine Spec haben (Idempotenz - nicht doppelt spezifizieren).
2. Fuer jede gefundene Datei: Nutze den Subagenten spec-agent, um eine vollstaendige Spec nach secondbrain/specs/entwurf/ zu schreiben (Template secondbrain/_templates/spec.md, status: Entwurf).
3. Committe NUR die neuen/geaenderten Dateien unter secondbrain/ mit Message "spec: <slug> (Entwurf)".
Verboten: Dateien nach specs/freigegeben verschieben, Anwendungscode aendern, mergen, main anfassen.
"@ "SPEC" $SpecMaxTurns
        }
        elseif ($path -like "*\secondbrain\specs\freigegeben\*") {
            Log "Freigabe-Ereignis: $path"
            $lastClaudeRun = Get-Date
            Invoke-Claude @"
Freigegebene Spec erkannt: $path (Schranke 1 wurde genommen).
Pruefe zuerst: Frontmatter muss status: Freigegeben haben UND worktree/branch muessen leer sein (sonst laeuft die Umsetzung schon - dann NICHTS tun und beenden).
Lies den Abschnitt 'Freigabe-Antworten' der Spec als verbindlichen Auftrag: er beantwortet die offenen Rueckfragen. Ist eine Rueckfrage dort unbeantwortet (nur Pfeil, keine Antwort) ODER ist es eine Varianten-Spec ohne gesetztes freigabe.entscheidung im Frontmatter: NICHT umsetzen, Status auf Entwurf zuruecksetzen, Grund in die Spec schreiben, beenden.
Dann:
1. Worktree anlegen: powershell -ExecutionPolicy Bypass -File scripts/new-worktree.ps1 -Slug <slug-aus-spec>. Trage worktree + branch ins Spec-Frontmatter ein, status: InUmsetzung, und lege eine Aufgaben-Datei in secondbrain/aufgaben/ an.
2. Setze IM WORKTREE um, gemaess CLAUDE.md-Workflow: superpowers:writing-plans, dann subagent-driven-development (unabhaengige Tasks parallel via dispatching-parallel-agents). Committe Zwischenstaende regelmaessig im Worktree ("wip: <slug>"), damit bei einem Abbruch nichts verloren geht.
3. PFLICHT vor der QA-Phase: committe den vollstaendigen Arbeitsstand im Worktree ("wip: <slug> feature-complete").
4. Qualitaet: superpowers:verification-before-completion + code-review. dotnet build und dotnet test muessen gruen sein - Ausgaben als Beweis in die Spec uebernehmen. docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md ergaenzen. Nutze dafuer den Subagenten qa-agent.
5. NUR bei Erfolg: Spec-Frontmatter status: Testbereit + Checkliste fuer den manuellen Test ans Spec-Ende. Commit im Worktree.
Wenn du merkst, dass die Aufgabe zu gross fuer das Turn-Limit ist: committe den Stand als "wip: <slug>" und schreibe in die Spec, wo du stehst - NICHT unvollstaendig auf Testbereit setzen.
Verboten: Merge nach main, git push, Worktree loeschen, status Gemerged setzen (alles Schranke 2 = Mensch).
"@ "DEV" $DevMaxTurns
        }
    }
}
finally {
    Unregister-Event BrainCreated -ErrorAction SilentlyContinue
    Unregister-Event BrainRenamed -ErrorAction SilentlyContinue
    $fsw.Dispose(); $mutex.ReleaseMutex()
    Log "Watcher beendet."
}
