<#
.SYNOPSIS
    Einmalhilfe v1.99.28-Sprung (AKE-Prod 2026-11-24): gemeinsames Startkennwort fuer lokale Benutzer ohne Kennwort.
.DESCRIPTION
    -Schritt Vorher  (VOR Update-IdealAkeWms.ps1): setzt allen aktiven Benutzern ohne Kennwort und ohne WindowsUserName
                     (ausser admin) das Startkennwort. Danach besteht Pruefung/BenutzerOhneKennwort.
    -Schritt Nachher (NACH dem Update, Spalte MussKennwortAendern existiert erst dann): setzt bei allen aktiven Benutzern,
                     deren Kennwort noch das Startkennwort ist, den Pflichtwechsel (Regel P, ADR 0027).
    Hash wie IdealAkeWms.Services.PasswordService: PBKDF2-HMACSHA256, 100000 Iterationen, 16 Byte Salt + 32 Byte Hash, Base64.
    Das Startkennwort wird abgefragt, nie als Parameter uebergeben und nicht protokolliert. Laeuft unter Windows PowerShell 5.1.
.EXAMPLE
    .\Set-Startkennwort.ps1 -Schritt Vorher  -WebPfad 'C:\inetpub\wwwroot\IDEALAKEWMS'
    .\Set-Startkennwort.ps1 -Schritt Nachher -WebPfad 'C:\inetpub\wwwroot\IDEALAKEWMS'
#>
param(
    [Parameter(Mandatory)][ValidateSet('Vorher', 'Nachher')][string]$Schritt,
    [Parameter(Mandatory)][string]$WebPfad
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

function Get-Hash([string]$kennwort, [byte[]]$salt) {
    $kdf = New-Object System.Security.Cryptography.Rfc2898DeriveBytes(
        [Text.Encoding]::UTF8.GetBytes($kennwort), $salt, 100000, [Security.Cryptography.HashAlgorithmName]::SHA256)
    try { return $kdf.GetBytes(32) } finally { $kdf.Dispose() }
}
function New-KennwortHash([string]$kennwort) {
    $salt = New-Object byte[] 16
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($salt)
    return [Convert]::ToBase64String([byte[]]($salt + (Get-Hash $kennwort $salt)))
}
function Test-Kennwort([string]$gespeichert, [string]$kennwort) {
    try { $b = [Convert]::FromBase64String($gespeichert) } catch { return $false }
    if ($b.Length -ne 48) { return $false }
    $soll = $b[16..47]; $ist = Get-Hash $kennwort ([byte[]]$b[0..15])
    return -not (Compare-Object $soll $ist -SyncWindow 0)
}

# Verbindung wie die App: appsettings.Production.json vor appsettings.json
$cs = $null
foreach ($d in @('appsettings.Production.json', 'appsettings.json')) {
    $p = Join-Path $WebPfad $d
    if (-not $cs -and (Test-Path $p)) {
        $j = Get-Content $p -Raw | ConvertFrom-Json
        if ($j.PSObject.Properties['ConnectionStrings'] -and $j.ConnectionStrings.PSObject.Properties['DefaultConnection']) { $cs = $j.ConnectionStrings.DefaultConnection }
    }
}
if (-not $cs) { throw "ConnectionStrings:DefaultConnection nicht in $WebPfad\appsettings*.json gefunden." }
$b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($cs)
Write-Host "Datenbank: $($b.DataSource) / $($b.InitialCatalog)" -ForegroundColor Cyan

$sec = Read-Host 'Startkennwort' -AsSecureString
$kennwort = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec))
if ($kennwort.Length -lt 8) { throw 'Startkennwort zu kurz (mindestens 8 Zeichen).' }

$con = New-Object System.Data.SqlClient.SqlConnection($cs)
$con.Open()
try {
    $tx = $con.BeginTransaction()
    $cmd = $con.CreateCommand(); $cmd.Transaction = $tx
    if ($Schritt -eq 'Vorher') {
        $cmd.CommandText = "SELECT Id, Name FROM dbo.Users WHERE IsActive = 1 AND Name <> N'admin' AND (PasswordHash IS NULL OR PasswordHash = N'') AND WindowsUserName IS NULL ORDER BY Name"
    } else {
        $cmd.CommandText = "SELECT Id, Name, PasswordHash FROM dbo.Users WHERE IsActive = 1 AND MussKennwortAendern = 0 AND PasswordHash IS NOT NULL AND PasswordHash <> N''"
    }
    $t = New-Object System.Data.DataTable; $t.Load($cmd.ExecuteReader())
    $ziel = @($t.Rows | Where-Object { $Schritt -eq 'Vorher' -or (Test-Kennwort $_.PasswordHash $kennwort) })
    if ($ziel.Count -eq 0) { Write-Host 'Keine Benutzer betroffen. Nichts geaendert.' -ForegroundColor Yellow; $tx.Rollback(); return }

    $ziel | ForEach-Object { Write-Host "  $($_.Id)  $($_.Name)" }
    $aktion = if ($Schritt -eq 'Vorher') { 'Startkennwort setzen' } else { 'Pflichtwechsel setzen' }
    if ((Read-Host "$($ziel.Count) Benutzer: $aktion? (ja/nein)") -ne 'ja') { Write-Host 'Abgebrochen. Nichts geaendert.'; $tx.Rollback(); return }

    $windows = [Security.Principal.WindowsIdentity]::GetCurrent().Name
    foreach ($r in $ziel) {
        $u = $con.CreateCommand(); $u.Transaction = $tx
        $u.CommandText = if ($Schritt -eq 'Vorher') {
            "UPDATE dbo.Users SET PasswordHash = @h, ModifiedAt = GETDATE(), ModifiedBy = N'Set-Startkennwort', ModifiedByWindows = @w WHERE Id = @id AND (PasswordHash IS NULL OR PasswordHash = N'')"
        } else {
            "UPDATE dbo.Users SET MussKennwortAendern = 1, ModifiedAt = GETDATE(), ModifiedBy = N'Set-Startkennwort', ModifiedByWindows = @w WHERE Id = @id"
        }
        if ($Schritt -eq 'Vorher') { [void]$u.Parameters.AddWithValue('@h', (New-KennwortHash $kennwort)) }
        [void]$u.Parameters.AddWithValue('@w', $windows)
        [void]$u.Parameters.AddWithValue('@id', $r.Id)
        if ($u.ExecuteNonQuery() -ne 1) { throw "Benutzer $($r.Id) $($r.Name): nicht genau eine Zeile geaendert - alles zurueckgerollt." }
    }
    $tx.Commit()
    Write-Host "$($ziel.Count) Benutzer: $aktion - erledigt." -ForegroundColor Green
    if ($Schritt -eq 'Vorher') { Write-Host 'NACH dem Update unbedingt: -Schritt Nachher (Pflichtwechsel).' -ForegroundColor Yellow }
}
finally { $con.Dispose() }
