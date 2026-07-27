# =====================================================================
# register-watcher-task.ps1
# ZWECK: Registriert watch-backlog.ps1 als geplanten Task, der bei der
#        Anmeldung startet und bei Absturz neu startet. Idempotent
#        (-Force ueberschreibt eine bestehende Registrierung).
# VORAUSSETZUNGEN: Admin-PowerShell; pwsh 7 empfohlen (Fallback 5.1);
#        Claude Code CLI eingeloggt; Rechner-Energieoptionen auf
#        "nie schlafen" (Task laeuft nur bei angemeldetem, wachem User).
# NICHT AUTOMATISIERT:
#   - Speichert KEIN Claude-Login (einmal interaktiv "claude" starten).
#   - Aendert keine Energieoptionen (manuell: powercfg / Systemsteuerung).
#   - Erstellt keinen OneDrive-Share (Anleitung Schritt 8).
# WARUM SO: Trigger "AtLogon" + Interactive-Principal, weil claude die
#        Anmeldedaten aus deinem Benutzerprofil braucht; RestartCount
#        faengt Abstuerze ab; ExecutionTimeLimit=0 = Dauerbetrieb.
# =====================================================================
#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [string]$Repo = "C:\Git\IDEAL-AKE-WMS",
    [string]$TaskName = "IdealAkeWms-BrainWatcher"
)
$ErrorActionPreference = "Stop"

$shell = Get-Command pwsh -ErrorAction SilentlyContinue
if (-not $shell) { $shell = Get-Command powershell }
$exe = $shell.Source

$action = New-ScheduledTaskAction -Execute $exe `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$Repo\scripts\watch-backlog.ps1`" -Repo `"$Repo`"" `
    -WorkingDirectory $Repo

$trigger  = New-ScheduledTaskTrigger -AtLogOn -User $env:USERNAME
$settings = New-ScheduledTaskSettingsSet `
    -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 5) `
    -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

$principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Limited

Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger `
    -Settings $settings -Principal $principal -Force | Out-Null

Write-Host "Task '$TaskName' registriert (Start bei Anmeldung, Auto-Restart)."
Write-Host "Sofort starten: Start-ScheduledTask -TaskName '$TaskName'"
Write-Host "Log ansehen:    Get-Content '$Repo\scripts\logs\watcher-*.log' -Tail 50 -Wait"
