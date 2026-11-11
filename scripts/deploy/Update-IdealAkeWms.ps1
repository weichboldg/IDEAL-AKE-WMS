#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Update IDEAL-AKE WMS (Web + Dienst) je Zielsystem, lokal auf dem Zielserver, mit Nachlauf-Liste.

.DESCRIPTION
    Spec 2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec, ADR 0022. Windows PowerShell 5.1, Administrator, nur von Hand.
    Ablauf je System: Ermittlung (Ordner, Site/App-Pool, Dienst, DB aus appsettings) -> Bestaetigung -> Vorpruefung (lesend, inkl.
    Pruefungen und Marker) -> Backup -> Stoppen -> Schema (SQL in Manifest-Reihenfolge, je Skript Bestaetigung) -> Pruefungen und
    Haltepunkt -> Kopieren (robocopy ohne Loeschen) -> Ordner und Rechte -> Transaktion vor dem Start -> Starten (Web vor Dienst,
    Version pruefen) -> Nachlauf-Liste.
    v1.99.0 (Spec 2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung, ADR 0023): Pruefungen reparieren nur nach
    Bestaetigung; Daten-/Einmal-Skripte laufen nur nach Eingabe am Zielserver (Trockenlauf -> SCHARF mit Backup aus diesem Lauf bzw.
    Transaktion -> uebernehmen/verwerfen); Marker in dbo.DeployScriptRuns. Kein unbeaufsichtigter Modus. Kein Zugriff auf Sage.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File C:\AKE\deploy\v1.98.0\deploy\Update-IdealAkeWms.ps1 -Firma AKE -System Test -NurPruefen
#>
[CmdletBinding()]
param(
    [ValidateSet('AKE', 'IDEAL')][string]$Firma,
    [string[]]$System,
    [string]$Quelle,
    [ValidateSet('Web', 'Dienst', 'Beide')][string]$Komponente = 'Beide',
    [string]$SqlOrdner,
    [string]$WebPfad,
    [string]$DienstPfad,
    [string]$LogPfad,
    [string]$SiteUrl,
    [string]$BackupPfad,
    [switch]$NurPruefen
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2
Import-Module (Join-Path $PSScriptRoot 'IdealAkeWms.Deploy.psm1') -Force -DisableNameChecking
Assert-Administrator
Import-Module WebAdministration
Add-Type -AssemblyName System.Net.Http
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$VorManifestMigration = '20260806120650_AllowMultipleDummyRequisitionItems'   # SQL/88: alle Systeme stehen mindestens hier

function Stop-Mit([string]$t) { throw "ABBRUCH: $t" }
# Fehler ausserhalb eines Systemlaufs (Paket, sqlcmd, ziele.psd1, Ermittlung): lesbar statt roher Ausnahme; Stack nur bei unerwarteten Fehlern
trap {
    Write-Host ''; Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.Exception.Message -notlike 'ABBRUCH:*') { Write-Host $_.ScriptStackTrace }
    Write-Host '  Nichts gestoppt, nichts geaendert.' -ForegroundColor Red
    exit 1
}

# ================================================================================================ Eingaben
$ziele = Import-Ziele (Join-Path $PSScriptRoot 'ziele.psd1')
$manifest = @((Import-PowerShellDataFile (Join-Path $PSScriptRoot 'nachlauf.psd1')).Eintraege)

while (-not $Firma) {
    $e = (Read-Eingabe '  ?   Firma (AKE | IDEAL)').ToUpper()
    if ($e -in 'AKE', 'IDEAL') { $Firma = $e } else { Write-Host '      Ungueltig.' }
}
$firmaZiele = @($ziele | Where-Object { $_.Firma -eq $Firma })
if ($System) { $System = @($System -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }) }
while (-not $System) {
    for ($i = 0; $i -lt $firmaZiele.Count; $i++) { Write-Host ("      {0}  {1}{2}" -f ($i + 1), $firmaZiele[$i].System, $(if ($firmaZiele[$i].Prod) { '  (PROD)' } else { '' })) }
    $e = Read-Eingabe '  ?   Systeme (Nummern oder Namen, mehrere mit Komma)'
    $wahl = @(); $gueltig = $true
    foreach ($t in @($e -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })) {
        $n = 0
        if ([int]::TryParse($t, [ref]$n) -and $n -ge 1 -and $n -le $firmaZiele.Count) { $wahl += $firmaZiele[$n - 1].System }
        elseif ($firmaZiele | Where-Object { $_.System -ieq $t }) { $wahl += ($firmaZiele | Where-Object { $_.System -ieq $t }).System }
        else { $gueltig = $false }
    }
    if ($gueltig -and $wahl.Count -gt 0) { $System = @($wahl | Select-Object -Unique) } else { Write-Host '      Ungueltig.' }
}
$gewaehlt = @()
foreach ($s in $System) {
    $z = $firmaZiele | Where-Object { $_.System -ieq $s }
    if (-not $z) { Stop-Mit "System '$s' gibt es fuer $Firma nicht in ziele.psd1 ($(@($firmaZiele | ForEach-Object { $_.System }) -join ', '))." }
    $gewaehlt += $z
}
if ($gewaehlt.Count -gt 1 -and ($WebPfad -or $DienstPfad -or $SiteUrl)) {
    Stop-Mit '-WebPfad/-DienstPfad/-SiteUrl gelten nur fuer EIN System.'
}
# Reihenfolge: Nicht-Prod in Listenreihenfolge, Prod zuletzt
$gewaehlt = @($gewaehlt | Where-Object { -not $_.Prod }) + @($gewaehlt | Where-Object { $_.Prod })

if (-not $Quelle) { $Quelle = Split-Path $PSScriptRoot -Parent }
$Quelle = (Resolve-Path $Quelle).Path
if (-not $SqlOrdner) { $SqlOrdner = Join-Path $Quelle 'SQL' }
if (-not (Test-Path $SqlOrdner)) { Stop-Mit "SQL-Ordner $SqlOrdner fehlt." }
$SqlOrdner = (Resolve-Path $SqlOrdner).Path
if (-not $LogPfad) { $LogPfad = "C:\$Firma\deploy-logs" }

# ================================================================================================ Paket
function Read-Info([string]$datei) {
    $h = @{}
    foreach ($z in Get-Content $datei) { if ($z -match '^\s*([^:=]+?)\s*[:=]\s*(.*?)\s*$') { $h[$Matches[1]] = $Matches[2] } }
    return $h
}
$infoDatei = Join-Path $Quelle 'publish-info.txt'
if (-not (Test-Path $infoDatei)) { Stop-Mit "$infoDatei fehlt (Paket unvollstaendig)." }
$info = Read-Info $infoDatei
if (-not $info['Version'] -or -not $info['Commit']) { Stop-Mit "$infoDatei ohne Version oder Commit." }
foreach ($k in 'IDEALAKEWMSWeb', 'IDEALAKEWMSWebService') {
    $kd = Join-Path $Quelle "$k\publish-info.txt"
    if (Test-Path $kd) {
        $ki = Read-Info $kd
        if ($ki['Version'] -ne $info['Version'] -or $ki['Commit'] -ne $info['Commit']) {
            Stop-Mit "$kd (v$($ki['Version']) $($ki['Commit'])) passt nicht zu $infoDatei (v$($info['Version']) $($info['Commit']))."
        }
    }
}
$PaketVersion = $info['Version']
$paketWeb = Join-Path $Quelle 'IDEALAKEWMSWeb'
$paketDienst = Join-Path $Quelle 'IDEALAKEWMSWebService'
if (-not (Test-Path (Join-Path $paketWeb 'IdealAkeWms.dll'))) { Stop-Mit "IdealAkeWms.dll fehlt in $paketWeb." }
if (-not (Test-Path (Join-Path $paketDienst 'IDEALAKEWMSService.exe'))) { Stop-Mit "IDEALAKEWMSService.exe fehlt in $paketDienst." }
if (-not (Test-Path $SqlOrdner)) { Stop-Mit "SQL-Ordner $SqlOrdner fehlt." }

# ================================================================================================ sqlcmd
# StrictMode 2: .Source auf $null (sqlcmd nicht im PATH) wirft - ExpandProperty liefert dann $null ohne Fehler
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source
if (-not $sqlcmd) {
    $sqlcmd = @('C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\SQLCMD.EXE',
                'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE') | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $sqlcmd) {
    Stop-Mit ('sqlcmd nicht gefunden. Installieren: zuerst "Microsoft ODBC Driver for SQL Server" (aktuell), dann ' +
              '"Microsoft Command Line Utilities for SQL Server" (ODBC-Variante, wie DEV). Die Go-Variante (winget install sqlcmd) nur, wenn sqlcmd -? die Schalter -E -b -i -C zeigt.')
}
$sqlHilfe = (& $sqlcmd '-?' 2>&1 | Out-String)
foreach ($sw in '-E', '-b', '-i', '-C') { if ($sqlHilfe -notmatch [regex]::Escape($sw) + '\b') { Stop-Mit "sqlcmd ($sqlcmd) kennt den Schalter $sw nicht." } }
$sqlcmdVersion = (($sqlHilfe -split "`r?`n" | Where-Object { $_.Trim() }) | Select-Object -First 2) -join ' / '

# ================================================================================================ Ermittlung (nichts aendern)
function Get-Umgebung([string]$webConfig) {
    [xml]$x = Get-Content $webConfig -Raw
    $n = $x.SelectSingleNode("//environmentVariable[@name='ASPNETCORE_ENVIRONMENT']")
    if ($n) { return $n.value } else { return 'Production' }
}
function Get-Einstellung([string]$ordner, [string]$umgebung, [string]$abschnitt, [string]$schluessel) {
    $wert = $null
    foreach ($d in @('appsettings.json', "appsettings.$umgebung.json")) {
        $p = Join-Path $ordner $d
        if (-not (Test-Path $p)) { continue }
        try { $j = Get-Content $p -Raw -Encoding UTF8 | ConvertFrom-Json } catch { Stop-Mit "$p ist kein gueltiges JSON ($($_.Exception.Message))." }
        if ($j.PSObject.Properties[$abschnitt] -and $j.$abschnitt.PSObject.Properties[$schluessel] -and $j.$abschnitt.$schluessel) { $wert = [string]$j.$abschnitt.$schluessel }
    }
    return $wert
}
function Get-Db([string]$cs, [string]$wo) {
    if (-not $cs) { Stop-Mit "ConnectionStrings:DefaultConnection fehlt ($wo)." }
    try { $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($cs) } catch { Stop-Mit "Verbindungszeichenfolge ($wo) nicht lesbar: $($_.Exception.Message)" }
    return [pscustomobject]@{ Server = $b.DataSource; Datenbank = $b.InitialCatalog; Windows = $b.IntegratedSecurity; Benutzer = $b.UserID
                              Passwort = $b.Password; Trust = $b.TrustServerCertificate; Cs = $cs }
}
function Get-PfadNorm([string]$p) { return [Environment]::ExpandEnvironmentVariables($p).TrimEnd('\').ToLowerInvariant() }

function Get-Ermittlung($ziel) {
    $web = if ($WebPfad) { $WebPfad } else { $ziel.WebPfad }
    $dienst = if ($DienstPfad) { $DienstPfad } else { $ziel.DienstPfad }
    foreach ($p in @($web, (Join-Path $web 'IdealAkeWms.dll'), $dienst, (Join-Path $dienst 'IDEALAKEWMSService.exe'))) {
        if (-not (Test-Path $p)) { Stop-Mit "$p fehlt ($Firma-$($ziel.System))." }
    }
    # Site/Anwendung ueber den physischen Pfad
    $treffer = @()
    foreach ($s in Get-Website) {
        if ((Get-PfadNorm $s.physicalPath) -eq (Get-PfadNorm $web)) { $treffer += [pscustomobject]@{ Site = $s.name; Pfad = '/'; Pool = $s.applicationPool } }
    }
    foreach ($a in Get-WebApplication) {
        if ((Get-PfadNorm $a.PhysicalPath) -eq (Get-PfadNorm $web)) {
            $site = [regex]::Match($a.ItemXPath, "@name='([^']+)'").Groups[1].Value
            $treffer += [pscustomobject]@{ Site = $site; Pfad = $a.path; Pool = $a.applicationPool }
        }
    }
    if ($treffer.Count -ne 1) { Stop-Mit "IIS: $($treffer.Count) Sites/Anwendungen mit Pfad $web statt genau einer: $(($treffer | ForEach-Object { "$($_.Site)$($_.Pfad)" }) -join ', ')" }
    $t = $treffer[0]
    $geteilt = @(@(Get-Website | Where-Object { $_.applicationPool -eq $t.Pool } | ForEach-Object { $_.name }) +
                 @(Get-WebApplication | Where-Object { $_.applicationPool -eq $t.Pool } | ForEach-Object { $_.path }))
    # Dienst ueber den Exe-Pfad (kein Name verlassen)
    $exe = Get-PfadNorm (Join-Path $dienst 'IDEALAKEWMSService.exe')
    $dienste = @(Get-CimInstance Win32_Service | Where-Object {
        $pn = ([string]$_.PathName).Trim()
        if ($pn.StartsWith('"')) { $pn = $pn.Substring(1, $pn.IndexOf('"', 1) - 1) } elseif ($pn -match '^(.+?\.exe)') { $pn = $Matches[1] }
        (Get-PfadNorm $pn) -eq $exe })
    if ($dienste.Count -ne 1) { Stop-Mit "Dienst: $($dienste.Count) Dienste mit $exe statt genau einem: $(@($dienste | ForEach-Object { $_.Name }) -join ', ')" }
    $d = $dienste[0]
    # Umgebung + DB
    $uWeb = Get-Umgebung (Join-Path $web 'web.config')
    $envReg = @((Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Services\$($d.Name)" -ErrorAction SilentlyContinue).PSObject.Properties |
                Where-Object Name -eq 'Environment' | ForEach-Object { $_.Value })
    $uDienst = ($envReg | Where-Object { $_ -match '^DOTNET_ENVIRONMENT=' } | ForEach-Object { $_.Split('=', 2)[1] } | Select-Object -First 1)
    if (-not $uDienst) { $uDienst = [Environment]::GetEnvironmentVariable('DOTNET_ENVIRONMENT', 'Machine') }
    if (-not $uDienst) { $uDienst = 'Production' }
    $dbWeb = Get-Db (Get-Einstellung $web $uWeb 'ConnectionStrings' 'DefaultConnection') "Web $web, Umgebung $uWeb"
    $dbDienst = Get-Db (Get-Einstellung $dienst $uDienst 'ConnectionStrings' 'DefaultConnection') "Dienst $dienst, Umgebung $uDienst"
    if ($dbWeb.Server -ne $dbDienst.Server -or $dbWeb.Datenbank -ne $dbDienst.Datenbank) {
        Stop-Mit "Web-DB $($dbWeb.Server)/$($dbWeb.Datenbank) ist nicht die Dienst-DB $($dbDienst.Server)/$($dbDienst.Datenbank)."
    }
    # URL: -SiteUrl vor Url aus ziele.psd1 (nicht bei -WebPfad) vor IIS-Bindung (https vor http); sonst dient die Bindung nur dem Vergleich
    $bindungen = @(Get-WebBinding -Name $t.Site | Where-Object { $_.protocol -in 'https', 'http' } | Sort-Object { if ($_.protocol -eq 'https') { 0 } else { 1 } })
    $url = if ($SiteUrl) { $SiteUrl } elseif ($ziel['Url'] -and -not $WebPfad) { $ziel.Url } else { $null }
    if ($url) {
        if (-not (Test-Url $url)) { Stop-Mit "URL '$url' ist keine http/https-Adresse." }
        $u = [uri]$url
        if ($bindungen.Count -and -not @($bindungen | Where-Object { $_.protocol -eq $u.Scheme -and $_.bindingInformation.Split(':')[1] -eq [string]$u.Port }).Count) {
            Write-Warnung "URL $url passt nicht zur IIS-Bindung von $($t.Site) (Schema/Port; Bindungen: $(($bindungen | ForEach-Object { "$($_.protocol) $($_.bindingInformation)" }) -join ', '))."
        }
    } else {
        if (-not $bindungen.Count) { Stop-Mit "Site $($t.Site) ohne http/https-Bindung; -SiteUrl angeben." }
        $teile = $bindungen[0].bindingInformation.Split(':'); $hostName = if ($teile[2]) { $teile[2] } else { 'localhost' }
        $url = "$($bindungen[0].protocol)://${hostName}:$($teile[1])$($t.Pfad.TrimEnd('/'))/"
    }
    return [pscustomobject]@{
        Ziel = $ziel; Name = "$Firma-$($ziel.System)"; Web = $web; Dienst = $dienst; Site = $t.Site; AppPfad = $t.Pfad; Pool = $t.Pool
        PoolGeteilt = $geteilt; DienstName = $d.Name; DienstAnzeige = $d.DisplayName; DienstKonto = $d.StartName
        UmgebungWeb = $uWeb; UmgebungDienst = $uDienst; Db = $dbWeb; Url = $url
        PoolIdentitaet = (Get-PoolIdentitaet $t.Pool); DienstIdentitaet = (Get-DienstIdentitaet $d.StartName)
        KeysPath = (Get-Einstellung $web $uWeb 'DataProtection' 'KeysPath')
    }
}

$ermittelt = @($gewaehlt | ForEach-Object { Get-Ermittlung $_ })
$dbKeys = @{}
foreach ($e in $ermittelt) {
    $k = "$($e.Db.Server)|$($e.Db.Datenbank)".ToLowerInvariant()
    if ($dbKeys.ContainsKey($k)) { Stop-Mit "$($e.Name) und $($dbKeys[$k]) zeigen auf dieselbe DB $($e.Db.Server)/$($e.Db.Datenbank)." }
    $dbKeys[$k] = $e.Name
}

# ================================================================================================ Hilfen je System
function Invoke-Lesen($e, [string]$sql) {
    if ($sql -match '\b(INSERT|UPDATE|DELETE|MERGE|DROP|ALTER|CREATE|EXEC|EXECUTE|TRUNCATE|GRANT)\b') { Stop-Mit "Abfrage ist nicht lesend: $sql" }
    $cn = New-Object System.Data.SqlClient.SqlConnection $e.Db.Cs
    try {
        $cn.Open(); $cmd = $cn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 120
        $t = New-Object System.Data.DataTable; $t.Load($cmd.ExecuteReader()); return , $t
    } finally { $cn.Dispose() }
}
function Get-SqlcmdArgs($e) {
    $a = @('-S', $e.Db.Server, '-d', $e.Db.Datenbank)
    if ($e.Db.Windows) { $a += '-E' } else { $a += @('-U', $e.Db.Benutzer); $env:SQLCMDPASSWORD = $e.Db.Passwort }
    if ($e.Db.Trust) { $a += '-C' }
    return $a + @('-b', '-I', '-f', '65001')
}
function Invoke-SqlDatei($e, [string]$datei, [string]$ausgabe) {
    $a = (Get-SqlcmdArgs $e) + @('-i', $datei)
    Write-Host "  >   sqlcmd $(($a | Where-Object { $_ -ne $e.Db.Passwort }) -join ' ')"
    # PS 5.1: stderr (z. B. Verbindungsfehler) wuerde unter ErrorActionPreference=Stop sofort werfen; Ergebnis entscheidet der Exit-Code
    $ErrorActionPreference = 'Continue'
    $out = @(& $sqlcmd @a 2>&1 | ForEach-Object { [string]$_ })
    $code = $LASTEXITCODE
    $out | ForEach-Object { Write-Host "      $_" }
    if ($ausgabe) { $out | Set-Content -Path $ausgabe -Encoding UTF8; Write-Host "      Ausgabe: $ausgabe" }
    return [pscustomobject]@{ Exit = $code; Text = ($out -join "`n") }
}
function Get-History($e) { return @((Invoke-Lesen $e 'SELECT MigrationId FROM dbo.__EFMigrationsHistory').Rows | ForEach-Object { $_.MigrationId }) }
function Test-PasstFirma($m, $e) {
    if ($m.Firma -ne 'beide' -and $m.Firma -ne $Firma) { return $false }
    if ($m['System'] -and -not (@($m.System) -contains $e.Ziel.System)) { return $false }
    return $true
}
function Test-Neuer($m, $installiert) { return (-not $installiert) -or ([version]$m.Version -gt $installiert) -or ($m.Version -eq '0.0.0') }
function Format-Text([string]$t, $e) {
    $sc = 'sqlcmd ' + ((Get-SqlcmdArgs $e) -join ' ')
    return $t.Replace('{Sqlcmd}', $sc).Replace('{Server}', $e.Db.Server).Replace('{DB}', $e.Db.Datenbank).Replace('{SqlOrdner}', $SqlOrdner).
              Replace('{LogPfad}', $LogPfad).Replace('{Web}', $e.Web).Replace('{Dienst}', $e.Dienst)
}
function Get-SpurZahl($e, $m) {
    if (-not $m['Spur']) { return $null }
    try { return [int](Invoke-Lesen $e $m.Spur).Rows[0][0] } catch { Write-Host "      Spur-Abfrage fehlgeschlagen: $($_.Exception.GetBaseException().Message)"; return $null }
}
function Get-Spur($e, $m) {
    $n = Get-SpurZahl $e $m
    if ($null -eq $n) { return 'Spur: keine bzw. nicht lesbar' }
    if ($n -gt 0) { return "Spur: $n (lief schon)" } else { return 'Spur: 0 (nicht gelaufen oder ohne Treffer)' }
}
function Get-MigrationIdVon([string]$id) {
    $x = @($manifest | Where-Object { $_.Id -eq $id })
    if ($x.Count -ne 1 -or -not $x[0]['MigrationId']) { Stop-Mit "nachlauf.psd1: Braucht verweist auf '$id', dafuer gibt es keinen Schema-Eintrag." }
    return $x[0].MigrationId
}
function Get-BrauchtFehlend($m, $history) {
    return @(@($m['Braucht'] | Where-Object { $_ }) | Where-Object { $history -notcontains (Get-MigrationIdVon $_) })
}
function Test-Braucht($e, $m, $history) {
    foreach ($b in (Get-BrauchtFehlend $m $history)) { Stop-Mit "$($m.Id) braucht $b ($(Get-MigrationIdVon $b)), das fehlt in der History." }
}
function Test-Trockenlauf([string]$datei) {
    $t = Get-Content $datei -Raw
    return ($t.Contains('DECLARE @Trockenlauf BIT = 1;') -and $t -notmatch 'DECLARE\s+@Trockenlauf\s+BIT\s*=\s*0')
}
# ================================================================================================ v1.99.0: Schritt, Zustand, Marker
# Spec 2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung, ADR 0023.
function Start-Schritt([string]$nr, [string]$t) { $script:SchrittNr = $nr; $script:Schritt = $t; Write-Schritt $t }

# Bugfix 4: nach jedem unerwarteten Fehler Live-Zustand von Web und Dienst, letzter Schritt und Weg zum Weitermachen
function Show-Zustand($e) {
    Write-Host ''; Write-Host '  Zustand jetzt (live gelesen):' -ForegroundColor Yellow
    try { Write-Host "    App-Pool $($e.Pool): $((Get-WebAppPoolState $e.Pool).Value);  Site $($e.Site): $((Get-Website $e.Site).state)" -ForegroundColor Yellow }
    catch { Write-Host "    App-Pool/Site nicht lesbar: $($_.Exception.Message)" -ForegroundColor Yellow }
    try { Write-Host "    Dienst $($e.DienstName): $((Get-Service $e.DienstName).Status)" -ForegroundColor Yellow }
    catch { Write-Host "    Dienst nicht lesbar: $($_.Exception.Message)" -ForegroundColor Yellow }
    Write-Host "    Letzter Schritt: $script:Schritt" -ForegroundColor Yellow
    try {
        $h = Get-History $e
        $f = @($manifest | Where-Object { $_.Typ -eq 'Schema' -and $h -notcontains $_.MigrationId } | ForEach-Object { $_.MigrationId })
        Write-Host "    Fehlende MigrationIds: $(if ($f) { $f -join ', ' } else { 'keine' })" -ForegroundColor Yellow
    } catch { }
    $weg = switch -Regex ($script:SchrittNr) {
        '^(Ermittlung|1|2)$' { 'Nichts gestoppt, nichts geaendert. Ursache beheben, Skript neu starten.' }
        '^(3|4|4a)$' { 'Web und/oder Dienst gestoppt. NICHT von Hand starten (der Web-Start spielte fehlende Migrationen per Migrate() ohne Vorbedingungen ein). Ursache beheben, Skript neu starten: es setzt bei fehlenden Schema-Skripten, Pruefungen und Haltepunkten fort.' }
        '^(5|5a|5b)$' { 'Dateien ggf. teilweise kopiert, Web und Dienst gestoppt. Skript neu starten (kopiert erneut, bietet offene Eintraege erneut an).' }
        '^6$' { "App-Pool gestoppt bzw. Dienst nicht gestartet (siehe oben). Log $($e.Web)\logs bzw. $($e.Dienst)\logs pruefen, Skript neu starten." }
        default { 'Web und Dienst laufen. Offene Nachlauf-Eintraege: Skript neu starten; die Marker verhindern Doppellaeufe.' }
    }
    Write-Host "    Weiter: $weg" -ForegroundColor Yellow
}

function Test-MarkerTabelle($e) {
    try { return [int](Invoke-Lesen $e "SELECT CASE WHEN OBJECT_ID(N'dbo.DeployScriptRuns', N'U') IS NULL THEN 0 ELSE 1 END").Rows[0][0] -eq 1 } catch { return $false }
}
function Get-Marker($e, $m) {
    if (-not (Test-MarkerTabelle $e)) { return $null }
    $t = Invoke-Lesen $e ("SELECT TOP 1 Mode, ExecutedAt, Hits FROM dbo.DeployScriptRuns WHERE ManifestId = N'" + $m.Id.Replace("'", "''") + "'")
    if (-not $t.Rows.Count) { return $null }
    return 'erledigt am {0:dd.MM.yyyy HH:mm} ({1}{2})' -f $t.Rows[0].ExecutedAt, $t.Rows[0].Mode, $(if ($t.Rows[0].Hits -isnot [DBNull]) { ", Treffer $($t.Rows[0].Hits)" })
}
function Set-MarkerBefehl($cmd, $m, [string]$mode, $hits, $sha, $ausgabe, $notiz) {
    $cmd.CommandText = 'INSERT INTO dbo.DeployScriptRuns (ManifestId, FileName, FileSha256, Mode, Hits, Output, PackageVersion, PackageCommit, ExecutedAt, ExecutedBy, Note) ' +
                       'VALUES (@Id, @Datei, @Sha, @Mode, @Hits, @Ausgabe, @Version, @Commit, SYSDATETIME(), LEFT(SUSER_SNAME(), 200), @Notiz)'
    $werte = [ordered]@{ '@Id' = $m.Id; '@Datei' = $m['Datei']; '@Sha' = $sha; '@Mode' = $mode; '@Hits' = $hits; '@Ausgabe' = $ausgabe
                         '@Version' = $PaketVersion; '@Commit' = $info['Commit']; '@Notiz' = $notiz }
    foreach ($k in $werte.Keys) { [void]$cmd.Parameters.AddWithValue($k, $(if ($null -eq $werte[$k]) { [DBNull]::Value } else { $werte[$k] })) }
}
# Marker ohne Skriptlauf (Entfaellt, Spur, VonHand); Fehler (z. B. Tabelle fehlt, UNIQUE) nur als Warnung
function Write-Marker($e, $m, [string]$mode, $hits, $notiz) {
    $cn = New-Object System.Data.SqlClient.SqlConnection $e.Db.Cs
    try { $cn.Open(); $cmd = $cn.CreateCommand(); Set-MarkerBefehl $cmd $m $mode $hits $null $null $notiz; [void]$cmd.ExecuteNonQuery(); Write-Ok "$($m.Id): Marker $mode" }
    catch { Write-Warnung "$($m.Id): Marker $mode nicht geschrieben: $($_.Exception.GetBaseException().Message)" }
    finally { $cn.Dispose() }
}

# ================================================================================================ v1.99.0: SQL in einer Transaktion
# SqlClient statt sqlcmd: nur so bleibt die Transaktion ueber die Eingabe uebernehmen/verwerfen offen und der Marker steht in derselben
# Transaktion wie die Datenaenderung (Spec, Technische Loesung). GO-Aufteilung wie Migration 20260512120355_AddProductionOrderSplit.
function Test-SqlFuerTransaktion([string]$text) {
    if ($text -match '(?m)^\s*:' -or $text.Contains('$(')) { return 'sqlcmd-Direktive (Zeile mit : oder $( )' }
    if ($text -match '(?i)\bROLLBACK\b') { return 'ROLLBACK (beendete die aeussere Transaktion samt Marker)' }
    return $null
}
function Split-SqlBatches([string]$text) { return @([regex]::Split($text, '(?im)^\s*GO\s*$') | Where-Object { $_.Trim() }) }
function Get-Treffer([string]$text) { $x = [regex]::Match($text, '(?m)^\s*TREFFER: (\d+)\s*$'); if ($x.Success) { return [int]$x.Groups[1].Value } else { return $null } }

# $marker: $null (Reparatur) oder @{ M = Eintrag; Mode = ...; Hits = ...; Sha = ... }; $abschluss: Commit (sofort) | Frage (uebernehmen/verwerfen)
function Invoke-SqlTransaktion($e, [string]$name, [string]$text, $marker, [string]$abschluss) {
    $grund = Test-SqlFuerTransaktion $text
    if ($grund) { Write-Warnung "${name}: abgelehnt - $grund. Nichts ausgefuehrt."; return [pscustomobject]@{ Uebernommen = $false; Text = '' } }
    $batches = @(Split-SqlBatches $text)   # @(): ein einzelner Batch kaeme sonst als String zurueck (StrictMode: kein .Count)
    $ausgabe = New-Object System.Collections.Generic.List[string]
    $cn = New-Object System.Data.SqlClient.SqlConnection $e.Db.Cs
    $cn.add_InfoMessage([System.Data.SqlClient.SqlInfoMessageEventHandler]({ param($s, $a) foreach ($er in $a.Errors) { $ausgabe.Add($er.Message); Write-Host "      $($er.Message)" } }.GetNewClosure()))
    $tx = $null; $nr = 0; $uebernommen = $false
    try {
        $cn.Open(); $tx = $cn.BeginTransaction(); $start = Get-Date
        Write-Host "  >   $name in EINER Transaktion ($($batches.Count) Batch(es)), $($e.Db.Server)/$($e.Db.Datenbank)"
        foreach ($b in $batches) {
            $nr++
            $cmd = $cn.CreateCommand(); $cmd.Transaction = $tx; $cmd.CommandText = $b; $cmd.CommandTimeout = 0
            $r = $cmd.ExecuteReader()
            try {
                while (-not $r.IsClosed -and $r.FieldCount -gt 0) {
                    $t = New-Object System.Data.DataTable; $t.Load($r)
                    $spalten = @($t.Columns | ForEach-Object { $_.ColumnName })
                    $tab = if ($t.Rows.Count) { ($t.Rows | Select-Object $spalten | Format-Table -AutoSize | Out-String -Width 250).TrimEnd() } else { "(keine Zeilen: $($spalten -join ', '))" }
                    $tab -split "`r?`n" | Where-Object { $_.Trim() } | ForEach-Object { $ausgabe.Add($_); Write-Host "      $_" }
                }
            } finally { $r.Dispose() }
        }
        if ($marker) {
            $cmd = $cn.CreateCommand(); $cmd.Transaction = $tx
            Set-MarkerBefehl $cmd $marker.M $marker.Mode $marker.Hits $marker.Sha ($ausgabe -join "`n") $null
            [void]$cmd.ExecuteNonQuery(); $nr = 'Marker'
        }
        if ($abschluss -eq 'Commit') { $tx.Commit(); $uebernommen = $true }
        else {
            while ($true) {
                $sek = [int]((Get-Date) - $start).TotalSeconds
                Write-Host "  Transaktion offen seit $sek s: Sperren bestehen bis zur Entscheidung (Web und Dienst gestoppt)." -ForegroundColor Yellow
                $a = Read-Eingabe "  ?   'uebernehmen' (Commit) oder 'verwerfen' (Rollback; leer = verwerfen)"
                if ($a -ceq 'uebernehmen') { $tx.Commit(); $uebernommen = $true; break }
                if (-not $a -or $a -ceq 'verwerfen') { $tx.Rollback(); break }
                Write-Host '      Bitte uebernehmen oder verwerfen eingeben.'
            }
        }
    } catch {
        $msg = $_.Exception.GetBaseException().Message
        if ($tx -and -not $uebernommen) { try { $tx.Rollback() } catch { } }   # XACT_ABORT/Fehler: Server hat ggf. schon zurueckgerollt
        Write-Warnung "${name}: Fehler in Batch $nr - zurueckgerollt, kein Marker, DB unveraendert: $msg"
        return [pscustomobject]@{ Uebernommen = $false; Text = ($ausgabe -join "`n"); Fehler = $msg }
    } finally { $cn.Dispose() }
    if ($uebernommen) { Write-Ok "${name}: uebernommen (Commit)$(if ($marker) { ", Marker $($marker.Mode)" })" } else { Write-Warnung "${name}: verworfen (Rollback), kein Marker." }
    return [pscustomobject]@{ Uebernommen = $uebernommen; Text = ($ausgabe -join "`n") }
}

# ================================================================================================ v1.99.0: Pruefungen
function Get-PruefStand($e, $m, $history) {
    if (Get-BrauchtFehlend $m $history) { return 'nach Schema' }
    try { return [int](Invoke-Lesen $e $m.Pruefung).Rows[0][0] } catch { return "Fehler: $($_.Exception.GetBaseException().Message)" }
}
function Format-Pruefstand($st) { if ($st -is [int]) { if ($st -eq 0) { 'OK' } else { "Abweichung $st" } } else { [string]$st } }
function Assert-Gestoppt($e, [string]$was) {
    $p = [string](Get-WebAppPoolState $e.Pool).Value; $d = [string](Get-Service $e.DienstName).Status
    if ($p -ne 'Stopped' -or $d -ne 'Stopped') { Stop-Mit "$was nur bei gestopptem Web und Dienst (App-Pool $p, Dienst $d)." }
}
function Invoke-Pruefung($e, $m, $history) {
    $st = Get-PruefStand $e $m $history
    if ($st -isnot [int]) { Write-Warnung "Pruefung $($m.Id): $st"; return }
    if ($st -eq 0) { Write-Ok "Pruefung $($m.Id): OK"; return }
    Write-Host "  Pruefung $($m.Id): Abweichung $st - $(Format-Text $m.Text $e)" -ForegroundColor Yellow
    if ($m.Ausfuehrung -ne 'Reparatur') { Write-Warnung "Pruefung $($m.Id): Abweichung $st, von Hand: $(Format-Text $m.Text $e)"; return }
    if ($m['Datei']) { Write-Host "  Reparatur: sqlcmd -i $(Join-Path $SqlOrdner $m.Datei) (idempotent)" }
    else { Write-Host "  Reparatur ($(if ($m['OhneTransaktion']) { 'ohne Transaktion' } else { 'in einer Transaktion' })):"; $m.Reparatur -split "`r?`n" | ForEach-Object { Write-Host "      $_" } }
    $frage = 'Reparatur ausfuehren?'
    if ($m['OhneTransaktion']) {
        [System.Data.SqlClient.SqlConnection]::ClearAllPools()   # eigene gepoolte Sitzungen nicht mitzaehlen und nicht abschiessen lassen
        $sitz = Invoke-Lesen $e 'SELECT session_id, login_name, host_name, program_name FROM sys.dm_exec_sessions WHERE database_id = DB_ID() AND session_id <> @@SPID'
        Write-Host "  Andere Sitzungen auf $($e.Db.Datenbank): $($sitz.Rows.Count)"
        $sitz.Rows | ForEach-Object { Write-Host "      $($_.session_id)  $($_.login_name)  $($_.host_name)  $($_.program_name)" }
        $frage = "Reparatur ausfuehren? WITH ROLLBACK IMMEDIATE beendet die $($sitz.Rows.Count) Sitzung(en) oben (offene Transaktionen werden zurueckgerollt)"
    }
    if (-not (Read-Ja $frage)) { Write-Warnung "Pruefung $($m.Id): Reparatur abgelehnt, Abweichung $st bleibt."; return }
    Assert-Gestoppt $e "Reparatur $($m.Id)"
    if ($m['Datei']) {
        $r = Invoke-SqlDatei $e (Join-Path $SqlOrdner $m.Datei) $null
        if ($r.Exit -ne 0) { Stop-Mit "Reparatur $($m.Id) fehlgeschlagen (Exit $($r.Exit)). Web und Dienst bleiben gestoppt." }
    } elseif ($m['OhneTransaktion']) {
        # ALTER DATABASE lehnt SQL Server in einer Benutzertransaktion ab
        $cn = New-Object System.Data.SqlClient.SqlConnection $e.Db.Cs
        try { $cn.Open(); $cmd = $cn.CreateCommand(); $cmd.CommandTimeout = 0; $cmd.CommandText = $m.Reparatur; [void]$cmd.ExecuteNonQuery() }
        catch { Stop-Mit "Reparatur $($m.Id) fehlgeschlagen: $($_.Exception.GetBaseException().Message). Web und Dienst bleiben gestoppt." }
        finally { $cn.Dispose(); [System.Data.SqlClient.SqlConnection]::ClearAllPools() }
    } else {
        $r = Invoke-SqlTransaktion $e "Reparatur $($m.Id)" $m.Reparatur $null 'Commit'
        if (-not $r.Uebernommen) { Stop-Mit "Reparatur $($m.Id) fehlgeschlagen. Web und Dienst bleiben gestoppt." }
    }
    $nach = Get-PruefStand $e $m $history
    if (-not ($nach -is [int] -and $nach -eq 0)) { Stop-Mit "Pruefung $($m.Id) nach der Reparatur: $(Format-Pruefstand $nach). Web und Dienst bleiben gestoppt." }
    Write-Ok "Pruefung $($m.Id): repariert (vorher $st, jetzt 0)"
}

# ================================================================================================ v1.99.0: Backup (Bugfix 3)
function Test-BackupOrdner($e, [string]$pfad) {
    # '1' = Ordner da, '0' = fehlt, sonst Text (nicht pruefbar: Version < 2017 oder Recht)
    try { return [string][int](Invoke-Lesen $e ("SELECT file_is_a_directory FROM sys.dm_os_file_exists(N'" + $pfad.Replace("'", "''") + "')")).Rows[0][0] }
    catch { return "nicht pruefbar: $($_.Exception.GetBaseException().Message)" }
}
function Get-BackupStandard($e) {
    if ($BackupPfad) { return $BackupPfad.TrimEnd('\') }
    return ([string](Invoke-Lesen $e "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(4000))").Rows[0][0]).TrimEnd('\')
}
function Show-BackupPfad($e) {
    $p = Get-BackupStandard $e
    if (-not $p) { Write-Host "  Backup-Ordner: Standardpfad leer - Schritt 2 fragt nach einem Ordner auf $($e.Db.Server)."; return }
    $r = Test-BackupOrdner $e $p
    $txt = switch ($r) { '1' { 'vorhanden' } '0' { 'FEHLT - Schritt 2 fragt nach einem anderen Ordner' } default { "$r - Schritt 2 fragt nach" } }
    Write-Host "  Backup-Ordner auf $($e.Db.Server): $p ($txt)"
}
function Get-BackupOrdner($e) {
    $pfad = Get-BackupStandard $e
    while ($true) {
        if (-not $pfad) {
            $pfad = (Read-Eingabe "  ?   Backup-Ordner auf dem SQL-Server $($e.Db.Server) (leer = Abbruch)").TrimEnd('\')
            if (-not $pfad) { Stop-Mit 'kein Backup-Ordner angegeben.' }
        }
        $r = Test-BackupOrdner $e $pfad
        if ($r -eq '1') { return $pfad }
        if ($r -eq '0') { Write-Host "  !!  Ordner $pfad fehlt auf $($e.Db.Server)." -ForegroundColor Yellow; $pfad = ''; continue }
        Write-Host "  !!  Backup-Ordner $pfad $r" -ForegroundColor Yellow
        if (Read-Ja 'Trotzdem diesen Ordner verwenden?') { return $pfad }
        $pfad = ''
    }
}
function Invoke-Backup($e) {
    $ziel = Get-BackupOrdner $e
    # Pfad liegt auf dem SQL-Server: kein Join-Path (PS 5.1 wirft, wenn das Laufwerk hier am Web-Server fehlt)
    $bak = '{0}\{1}_{2:yyyyMMdd_HHmmss}_vor_v{3}.bak' -f $ziel, $e.Db.Datenbank, (Get-Date), $PaketVersion
    Write-Host "  Ziel (Pfad auf dem SQL-Server $($e.Db.Server)): $bak"
    $cn = New-Object System.Data.SqlClient.SqlConnection $e.Db.Cs
    try {
        $cn.Open(); $cmd = $cn.CreateCommand(); $cmd.CommandTimeout = 0
        $cmd.CommandText = "BACKUP DATABASE [$($e.Db.Datenbank.Replace(']', ']]'))] TO DISK = N'$($bak.Replace("'", "''"))' WITH COPY_ONLY, CHECKSUM"
        [void]$cmd.ExecuteNonQuery(); Write-Ok "Backup $bak"; $script:BackupDatei = $bak
    } catch { Stop-Mit "Backup fehlgeschlagen: $($_.Exception.Message)" } finally { $cn.Dispose() }
}
# Backup aus DIESEM Lauf (Fluss A Pflicht; Fluss B auch "liegt vor" aus Schritt 2)
function Test-BackupFuerLauf($e, [bool]$liegtVorReicht) {
    if ($script:BackupDatei -or ($liegtVorReicht -and $script:BackupLiegtVor)) { return $true }
    Write-Host '  Kein Backup aus diesem Lauf.'
    if (Read-Ja "Jetzt BACKUP DATABASE [$($e.Db.Datenbank)] WITH COPY_ONLY, CHECKSUM anlegen?") { Invoke-Backup $e }
    return [bool]$script:BackupDatei
}

# ================================================================================================ v1.99.0: Daten-/Einmal-Eintraege
function Get-OffeneEintraege($e, [string]$phase, $installiert) {
    return @($manifest | Where-Object { $_.Typ -in 'Daten', 'Einmalig' -and $_.Phase -eq $phase -and (Test-PasstFirma $_ $e) -and
                                        ($_.Typ -eq 'Einmalig' -or (Test-Neuer $_ $installiert)) } |
             Where-Object { -not (Get-Marker $e $_) -and -not ((Get-SpurZahl $e $_) -gt 0) })
}
function Show-Pruefabfrage($e, $m) {
    try { (Invoke-Lesen $e $m.Pruefabfrage).Rows | ForEach-Object { Write-Host "      Pruefabfrage: $(($_.ItemArray) -join ' | ')" } }
    catch { Write-Host "      Pruefabfrage fehlgeschlagen: $($_.Exception.Message)" }
}
# Marker zuerst, dann Spur (Altstand), dann Fluss A (Trockenlauf) / B (Transaktion) / C (anzeigen)
function Invoke-Eintrag($e, $m, [bool]$halt) {
    $erl = Get-Marker $e $m
    if ($erl) { Write-Host "      $erl"; return }
    if ($m['Voraussetzung']) { Write-Host "      Voraussetzung: $($m.Voraussetzung)" }
    if ($m['Text']) { Write-Host "      $(Format-Text $m.Text $e)" }
    $spur = Get-SpurZahl $e $m
    if ($spur -gt 0) {
        Write-Host "      Spur zeigt: lief schon ($spur)."
        if (Read-Ja "$($m.Id) als gelaufen vermerken (Marker Spur)?") { Write-Marker $e $m 'Spur' $spur $null }
        return
    }
    switch ($m.Ausfuehrung) {
        'Trockenlauf' { Invoke-FlussA $e $m $halt }
        'Transaktion' { Invoke-FlussB $e $m }
        default {
            if (Read-Ja "Wurde $($m.Id) auf diesem System schon ausgefuehrt?") { Write-Marker $e $m 'VonHand' $null 'von Hand erfasst' }
            else { Write-Warnung "$($m.Id): offen (von Hand, siehe Text)." }
        }
    }
}
function Invoke-Trockenlauf($e, $m, [string]$datei) {
    $aus = Join-Path $LogPfad ('{0:yyyy-MM-dd_HHmmss}_{1}_{2}_trockenlauf.txt' -f (Get-Date), $e.Name, ($m.Id -replace '[^A-Za-z0-9.-]', '_'))
    Write-Host '  Trockenlauf (Datei unveraendert, @Trockenlauf = 1):'
    $r = Invoke-SqlDatei $e $datei $aus
    return [pscustomobject]@{ Exit = $r.Exit; Treffer = (Get-Treffer $r.Text) }
}
# Fluss A: Trockenlauf automatisch; 0 -> entfaellt; > 0 -> Backup aus diesem Lauf + SCHARF -> Scharflauf mit Marker in EINER Transaktion
function Invoke-FlussA($e, $m, [bool]$halt) {
    $datei = Join-Path $SqlOrdner $m.Datei
    $nein = { param($t) if ($halt) { Stop-Mit "Haltepunkt $($m.Id): $t Web und Dienst bleiben gestoppt." } else { Write-Warnung "$($m.Id): $t Eintrag bleibt offen." } }
    if (-not (Test-Path $datei)) { & $nein "$datei fehlt."; return }
    if (-not (Test-Trockenlauf $datei)) { & $nein "Datei enthaelt nicht woertlich 'DECLARE @Trockenlauf BIT = 1;' -> nur Anzeige."; return }
    $t = Invoke-Trockenlauf $e $m $datei
    if ($t.Exit -ne 0 -or $null -eq $t.Treffer) { & $nein "Trockenlauf Exit $($t.Exit)$(if ($null -eq $t.Treffer) { ', keine Zeile TREFFER' }) -> kein Scharflauf."; return }
    if ($t.Treffer -eq 0) {
        Write-Ok "$($m.Id): entfaellt (TREFFER: 0)"
        if (Read-Ja "$($m.Id) als 'entfaellt' vermerken?") { Write-Marker $e $m 'Entfaellt' 0 $null }
        return
    }
    Write-Host "  TREFFER: $($t.Treffer)" -ForegroundColor Yellow
    if (-not (Test-BackupFuerLauf $e $false)) { & $nein 'kein Backup aus diesem Lauf -> kein Scharflauf.'; return }
    $a = Read-Eingabe "  ?   $($m.Id): $($t.Treffer) Treffer. Zum Ausfuehren SCHARF eintippen (anderes = nicht ausfuehren)"
    if ($a -cne 'SCHARF') { & $nein 'nicht SCHARF bestaetigt.'; return }
    $hash = (Get-FileHash $datei -Algorithm SHA256).Hash
    $text = [IO.File]::ReadAllText($datei, [Text.Encoding]::UTF8)
    $n = ([regex]::Matches($text, [regex]::Escape('DECLARE @Trockenlauf BIT = 1;'))).Count
    if ($n -ne 1) { & $nein "Literal 'DECLARE @Trockenlauf BIT = 1;' $n-mal statt genau einmal -> kein Scharflauf."; return }
    # nur im Arbeitsspeicher: die Datei bleibt unveraendert (Hash vorher = nachher)
    $r = Invoke-SqlTransaktion $e "$($m.Id) SCHARF" $text.Replace('DECLARE @Trockenlauf BIT = 1;', 'DECLARE @Trockenlauf BIT = 0;') @{ M = $m; Mode = 'Scharf'; Hits = $t.Treffer; Sha = $hash } 'Commit'
    if ((Get-FileHash $datei -Algorithm SHA256).Hash -ne $hash) { Write-Warnung "$($m.Id): Datei-Hash nach dem Lauf veraendert." }
    if (-not $r.Uebernommen) { & $nein 'Scharflauf fehlgeschlagen (zurueckgerollt).'; return }
    $t2 = Invoke-Trockenlauf $e $m $datei
    if ($t2.Treffer -ne 0) { Write-Warnung "$($m.Id): zweiter Trockenlauf TREFFER: $($t2.Treffer) (erwartet 0)." } else { Write-Ok "$($m.Id): zweiter Trockenlauf TREFFER: 0" }
}
# Fluss B: ohne Trockenlauf-Schalter; ganze Datei in EINER Transaktion, Ausgabe zeigen, dann uebernehmen/verwerfen
function Invoke-FlussB($e, $m) {
    if ($m['Pruefabfrage']) { Show-Pruefabfrage $e $m }
    while ($true) {
        $a = (Read-Eingabe "  ?   $($m.Id) jetzt in einer Transaktion ausfuehren? [j = ausfuehren / n = spaeter / e = lief schon, nur vermerken]").ToLower()
        if ($a -in 'j', 'n', 'e', '') { break }
        Write-Host '      Bitte j, n oder e eingeben.'
    }
    if ($a -eq 'e') { Write-Marker $e $m 'VonHand' $null 'von Hand erfasst'; return }
    if ($a -ne 'j') { Write-Warnung "$($m.Id): offen (spaeter)."; return }
    Assert-Gestoppt $e "$($m.Id) (Phase $($m.Phase))"
    if (-not (Test-BackupFuerLauf $e $true)) { Write-Warnung "$($m.Id): kein Backup -> nicht ausgefuehrt, Eintrag bleibt offen."; return }
    $datei = Join-Path $SqlOrdner $m.Datei
    $text = [IO.File]::ReadAllText($datei, [Text.Encoding]::UTF8)
    $r = Invoke-SqlTransaktion $e $m.Id $text @{ M = $m; Mode = 'Transaktion'; Hits = $null; Sha = (Get-FileHash $datei -Algorithm SHA256).Hash } 'Frage'
    if ($r.Uebernommen -and $m['Pruefabfrage']) { Show-Pruefabfrage $e $m }
    if (-not $r.Uebernommen) { Write-Host "      $($m.Id): Eintrag bleibt offen." }
}
function Get-FooterVersion($e, [int]$sekunden) {
    $h = New-Object System.Net.Http.HttpClientHandler; $h.UseDefaultCredentials = $true; $h.CookieContainer = New-Object System.Net.CookieContainer
    $c = New-Object System.Net.Http.HttpClient($h); $c.Timeout = [TimeSpan]::FromSeconds($sekunden)
    $c.DefaultRequestHeaders.UserAgent.ParseAdd('Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/130.0')
    try {
        $r = $c.GetAsync($e.Url).GetAwaiter().GetResult()
        $body = $r.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        return [pscustomobject]@{ Status = [int]$r.StatusCode; Ok = $r.IsSuccessStatusCode; Version = [regex]::Match($body, 'IDEAL-AKE WMS v([0-9.]+)').Groups[1].Value }
    } finally { $c.Dispose() }
}
function Get-Schutzstand($e) {
    # appsettings*.json, web.config (Hash) und geschuetzte Ordner (Dateiliste) im Ziel
    $zeilen = @()
    foreach ($o in @($e.Web, $e.Dienst)) {
        $zeilen += @(Get-ChildItem $o -File | Where-Object { $_.Name -like 'appsettings*.json' -or $_.Name -eq 'web.config' } |
                     ForEach-Object { "$($_.FullName)|$((Get-FileHash $_.FullName).Hash)" })
    }
    foreach ($o in @('Branding', 'App_Data', 'wwwroot\Fotos')) {
        $p = Join-Path $e.Web $o
        if (Test-Path $p) { $zeilen += @(Get-ChildItem $p -File -Recurse | ForEach-Object { "$($_.FullName)|$($_.Length)|$($_.LastWriteTimeUtc.Ticks)" }) }
    }
    return ($zeilen | Sort-Object) -join "`n"
}
function Invoke-Robocopy([string]$von, [string]$nach) {
    $a = @($von, $nach, '/E', '/R:2', '/W:5', '/NP', '/NFL', '/NDL', '/NJH',
           '/XF', 'appsettings*.json', 'web.config', 'publish-info.txt', 'deploy-info.txt',
           '/XD', (Join-Path $von 'Branding'), (Join-Path $von 'App_Data'), (Join-Path $von 'wwwroot\Fotos'), (Join-Path $von 'logs'))
    Write-Host "  >   robocopy $($a -join ' ')"
    & robocopy @a | ForEach-Object { if ($_.Trim()) { Write-Host "      $_" } }
    if ($LASTEXITCODE -ge 8) { Stop-Mit "robocopy $von -> $nach mit Exit $LASTEXITCODE." }
}

# ================================================================================================ Ablauf je System
function Invoke-System($e) {
    Reset-Warnungen
    $script:BackupDatei = $null; $script:BackupLiegtVor = $false
    $tauschWeb = $Komponente -in 'Web', 'Beide'; $tauschDienst = $Komponente -in 'Dienst', 'Beide'
    $webGestoppt = $false; $dienstGestoppt = $false

    Start-Schritt 'Ermittlung' "$($e.Name): Ermittlung"
    @(
        "Paket          v$PaketVersion  Commit $($info['Commit'])  Zweig $($info['Branch'])  ($Quelle)"
        "Komponente     $Komponente"
        "Web-Ordner     $($e.Web)"
        "Site/App-Pool  $($e.Site)$(if ($e.AppPfad -ne '/') { $e.AppPfad }) / $($e.Pool)  (Identitaet $($e.PoolIdentitaet))"
        "Dienst         $($e.DienstName) ($($e.DienstAnzeige))  Konto $($e.DienstKonto)  Ordner $($e.Dienst)"
        "Umgebung       Web $($e.UmgebungWeb), Dienst $($e.UmgebungDienst)"
        "DB             $($e.Db.Server) / $($e.Db.Datenbank)  ($(if ($e.Db.Windows) { 'Windows-Anmeldung' } else { "SQL-Login $($e.Db.Benutzer)" }))"
        "URL            $($e.Url)"
        "sqlcmd         $sqlcmd  ($sqlcmdVersion)"
        "SQL-Ordner     $SqlOrdner"
        "Protokoll      $LogPfad"
    ) | ForEach-Object { Write-Host "  $_" }
    if ($e.PoolGeteilt.Count -gt 1) {
        Write-Warnung "App-Pool $($e.Pool) wird auch genutzt von: $($e.PoolGeteilt -join ', ')"
        if (-not (Read-Ja 'Trotzdem weiter (der App-Pool wird gestoppt)?')) { Stop-Mit 'geteilter App-Pool nicht bestaetigt.' }
    }
    if (-not (Read-Ja "Verbindung zu $($e.Db.Server)/$($e.Db.Datenbank) herstellen (nur lesend)?")) {
        Write-Host '  Ende ohne DB-Kontakt.'; return 2
    }

    # ---------------------------------------------------------------------------------------- 1 Vorpruefung
    Start-Schritt '1' '1. Vorpruefung (lesend)'
    $installiert = $null; $quelleVersion = ''
    # Schritt 6 ruft dieselbe URL ab: was hier scheitert, laesst spaeter den Start abbrechen -> als Warnung zeigen (auch bei -NurPruefen)
    try {
        $f = Get-FooterVersion $e 60
        if ($f.Version) { $installiert = [version]$f.Version; $quelleVersion = "Fusszeile $($e.Url)" }
        elseif (-not $f.Ok) { Write-Warnung "Startseite $($e.Url): HTTP $($f.Status). Schritt 6 bricht dann nach dem Kopieren ab (App-Pool gestoppt) - URL pruefen, ggf. -SiteUrl." }
        else { Write-Host "      Startseite $($e.Url): HTTP $($f.Status) ohne Fusszeile (keine Windows-Anmeldung?) - Schritt 6 fragt dann nach der Version im Browser." }
    }
    catch { Write-Warnung "Startseite $($e.Url) nicht lesbar: $($_.Exception.GetBaseException().Message). Schritt 6 bricht dann nach dem Kopieren ab (App-Pool gestoppt) - URL/Zertifikat pruefen, ggf. -SiteUrl." }
    if (-not $installiert -and (Test-Path (Join-Path $e.Web 'deploy-info.txt'))) {
        $v = (Read-Info (Join-Path $e.Web 'deploy-info.txt'))['Version']; if ($v) { $installiert = [version]$v; $quelleVersion = 'deploy-info.txt' }
    }
    if (-not $installiert) {
        $v = (Read-Eingabe '  ?   Installierte Version (z. B. 1.95.0; leer = unbekannt)')
        if ($v) { $installiert = [version]$v; $quelleVersion = 'Eingabe' } else { $quelleVersion = 'unbekannt (alle Eintraege)' }
    }
    Write-Host "  Installiert: $(if ($installiert) { "v$installiert" } else { '?' })  (Quelle: $quelleVersion)"
    try { $history = Get-History $e } catch { Stop-Mit "DB $($e.Db.Server)/$($e.Db.Datenbank) nicht lesbar: $($_.Exception.GetBaseException().Message)" }
    if ($history -notcontains $VorManifestMigration) { Stop-Mit "History ohne $VorManifestMigration (SQL/88): System aelter als das Manifest, Update von Hand laut README." }
    $schema = @($manifest | Where-Object { $_.Typ -eq 'Schema' })
    $fehlend = @($schema | Where-Object { $history -notcontains $_.MigrationId })
    Write-Host "  Fehlende Schema-Skripte (Manifest-Reihenfolge): $(if ($fehlend) { ($fehlend | ForEach-Object { $_.Id }) -join ', ' } else { 'keine' })"
    $pruefungen = @($manifest | Where-Object { $_.Typ -eq 'Pruefung' -and (Test-PasstFirma $_ $e) })
    $pruefStand = @{}
    if ($pruefungen) { Write-Host '  Pruefungen (0 = in Ordnung):' }
    foreach ($m in $pruefungen) {
        $st = Get-PruefStand $e $m $history; $pruefStand[$m.Id] = $st
        $hinweis = if ($st -isnot [int] -or $st -eq 0) { '' } elseif ($m.Ausfuehrung -eq 'Reparatur') { if ($m.Phase -eq 'Haltepunkt') { ' - wird in 4a nach Bestaetigung repariert' } else { '' } } else { " - von Hand: $(Format-Text $m.Text $e)" }
        Write-Host "    [$($m.Id)] $(Format-Pruefstand $st)$hinweis"
    }
    if (-not (Test-MarkerTabelle $e)) { Write-Host '  Marker-Tabelle fehlt (kommt mit SQL/142) - Daten-/Einmal-Eintraege ohne Marker-Stand.' }
    foreach ($m in $manifest | Where-Object { $_.Typ -in 'Daten', 'Einmalig' -and (Test-PasstFirma $_ $e) }) {
        $erl = Get-Marker $e $m
        Write-Host "  $($m.Id): $(if ($erl) { $erl } else { "offen, $(Get-Spur $e $m)" })"
    }
    Show-BackupPfad $e
    $vorher = @($manifest | Where-Object { $_.Phase -eq 'Vorher' -and $_.Typ -ne 'Ordner' -and (Test-PasstFirma $_ $e) -and (Test-Neuer $_ $installiert) })
    if ($vorher) { Write-Host '  Vorher:'; $vorher | ForEach-Object { Write-Host "    [$($_.Id)] $(Format-Text $_.Text $e)" } }
    # Haltepunkt- und VorStart-Eintraege ohne Marker und ohne Spur > 0 verlangen gestopptes Web und Dienst
    $halte = @(Get-OffeneEintraege $e 'Haltepunkt' $installiert)
    $vorStart = @(Get-OffeneEintraege $e 'VorStart' $installiert)
    $reparatur = @($pruefungen | Where-Object { $_.Phase -eq 'Haltepunkt' -and $_.Ausfuehrung -eq 'Reparatur' -and
                                               ("$($pruefStand[$_.Id])" -eq 'nach Schema' -or ($pruefStand[$_.Id] -is [int] -and $pruefStand[$_.Id] -gt 0)) })
    $alleStoppen = [bool]($fehlend -or $halte -or $vorStart -or $reparatur)
    $stopWeb = $tauschWeb -or $alleStoppen; $stopDienst = $tauschDienst -or $alleStoppen
    Write-Host ("  Plan: Backup-Angebot; stoppen: $(if ($stopWeb) { 'App-Pool ' })$(if ($stopDienst) { 'Dienst' }); Schema: $($fehlend.Count) Skript(e); " +
                "Reparaturen: $(if ($reparatur) { @($reparatur | ForEach-Object { $_.Id }) -join ', ' } else { 'keine' }); " +
                "Haltepunkt: $(if ($halte) { @($halte | ForEach-Object { $_.Id }) -join ', ' } else { 'keiner' }); kopieren: $Komponente; Ordner/Rechte; " +
                "vor dem Start: $(if ($vorStart) { @($vorStart | ForEach-Object { $_.Id }) -join ', ' } else { 'nichts' }); starten; Nachlauf.")

    if ($NurPruefen) {
        Write-Schritt '5a. Ordner und Rechte (nur Bericht)'
        [void](Test-OrdnerUndRechte -WebPfad $e.Web -DienstPfad $e.Dienst -PoolIdentitaet $e.PoolIdentitaet -DienstIdentitaet $e.DienstIdentitaet `
               -Umgebung $e.UmgebungWeb -DataProtectionPfad $e.KeysPath -ManifestEintraege $manifest -LogPfad $LogPfad)
        Show-Nachlauf $e $installiert $false
        Write-Host ''; Write-Host "  -NurPruefen: nichts gestoppt, nichts geschrieben." -ForegroundColor Green
        return 0
    }

    # ---------------------------------------------------------------------------------------- 2 Backup
    Start-Schritt '2' '2. Backup'
    Write-Host '  Pflicht bei SQL/90, 95, 109/110, 133/Altbestand und vor jedem Scharf-/Transaktionslauf (ADR 0004, ADR 0023).'
    if (Read-Ja "Jetzt BACKUP DATABASE [$($e.Db.Datenbank)] WITH COPY_ONLY, CHECKSUM anlegen?") { Invoke-Backup $e }
    elseif (Read-Ja 'Liegt ein aktuelles Backup dieser DB vor?') { $script:BackupLiegtVor = $true }
    else { Stop-Mit 'kein Backup.' }

    # ---------------------------------------------------------------------------------------- 3 Stoppen
    Start-Schritt '3' '3. Stoppen'
    if ($stopDienst) {
        $s = Get-Service $e.DienstName
        # Stop() kehrt sofort zurueck (Stop-Service wartet in PS 5.1 ohne Limit); 60 s Limit, bei Timeout kein Kill - der Mensch entscheidet
        if ($s.Status -ne 'Stopped') {
            try { if ($s.Status -ne 'StopPending') { $s.Stop() }; $s.WaitForStatus('Stopped', '00:01:00') }
            catch { $s.Refresh(); Stop-Mit "Dienst $($e.DienstName) nach 60 s nicht gestoppt (Status $($s.Status)): $($_.Exception.GetBaseException().Message) Nichts kopiert, Schema unveraendert, Web laeuft weiter. Prozess NICHT beendet: Dienst pruefen bzw. von Hand beenden, dann Skript neu starten." }
        }
        $dienstGestoppt = $true; Write-Ok "Dienst $($e.DienstName) gestoppt"
    }
    if ($stopWeb) {
        if ((Get-WebAppPoolState $e.Pool).Value -ne 'Stopped') { Stop-WebAppPool $e.Pool }
        $bis = (Get-Date).AddSeconds(60)
        while ((Get-WebAppPoolState $e.Pool).Value -ne 'Stopped') { if ((Get-Date) -gt $bis) { Stop-Mit "App-Pool $($e.Pool) nicht gestoppt (Timeout)." }; Start-Sleep 1 }
        Start-Sleep 2   # w3wp gibt die DLLs frei
        $webGestoppt = $true; Write-Ok "App-Pool $($e.Pool) gestoppt"
    }

    # ---------------------------------------------------------------------------------------- 4 Schema
    Start-Schritt '4' '4. Schema'
    foreach ($m in $fehlend) {
        Test-Braucht $e $m $history
        $datei = Join-Path $SqlOrdner $m.Datei
        if (-not (Test-Path $datei)) { Stop-Mit "$datei fehlt." }
        Write-Host "  $($m.Id) $($m.Datei) (v$($m.Version))$(if ($m['Voraussetzung']) { " - $($m.Voraussetzung)" })"
        if ($m.Id -eq 'SQL/90') {
            try {
                $jobs = Invoke-Lesen $e "SELECT j.name, j.enabled, s.step_name FROM msdb.dbo.sysjobs j JOIN msdb.dbo.sysjobsteps s ON s.job_id = j.job_id WHERE s.command LIKE N'%ProductionOrders%'"
                Write-Host "  Agent-Jobs mit ProductionOrders: $($jobs.Rows.Count)"; $jobs.Rows | ForEach-Object { Write-Host "    $($_.name) (enabled=$($_.enabled)) $($_.step_name)" }
            } catch { Write-Warnung "sysjobs nicht lesbar ($($_.Exception.Message)) - von Hand pruefen." }
            if (-not (Read-Ja 'Backup liegt vor, kein Agent-Job schreibt ProductionOrders?')) { Stop-Mit 'SQL/90 nicht bestaetigt. Web und Dienst bleiben gestoppt.' }
        }
        if ($m.Id -eq 'SQL/95' -and $history -notcontains '20260330142035_AddOseonOperationConfig') {
            Stop-Mit 'SQL/95 braucht 20260330142035_AddOseonOperationConfig in der History (P1/R10). Web und Dienst bleiben gestoppt.'
        }
        if (-not (Read-Ja "$($m.Id) ausfuehren?")) { Stop-Mit "$($m.Id) abgelehnt: Ohne alle Schema-Skripte wuerde der Web-Start db.Database.Migrate() ausloesen (Program.cs) und EF die Migration ohne Vorbedingungen einspielen. Web und Dienst bleiben gestoppt." }
        $r = Invoke-SqlDatei $e $datei $null
        if ($r.Exit -ne 0 -or ($m.Id -eq 'SQL/95' -and $r.Text -match 'ABBRUCH')) { Stop-Mit "$($m.Id) fehlgeschlagen (Exit $($r.Exit)). Web und Dienst bleiben gestoppt." }
        $history = Get-History $e
        if ($history -notcontains $m.MigrationId) { Stop-Mit "$($m.MigrationId) fehlt nach $($m.Id) in der History." }
        Write-Ok "$($m.Id) -> $($m.MigrationId)"
    }
    $rest = @($schema | Where-Object { $history -notcontains $_.MigrationId })
    if ($rest) { Stop-Mit "MigrationId fehlt: $(($rest.MigrationId) -join ', '). Kein Kopieren/Start, sonst Migrate() ohne Vorbedingungen." }
    if (-not $fehlend) { Write-Ok 'nichts fehlt' }

    # ---------------------------------------------------------------------------------------- 4a Pruefungen und Haltepunkt
    Start-Schritt '4a' '4a. Pruefungen und Haltepunkt'
    foreach ($m in @($pruefungen | Where-Object { $_.Phase -eq 'Haltepunkt' })) { Invoke-Pruefung $e $m $history }
    foreach ($m in @($manifest | Where-Object { $_.Typ -in 'Daten', 'Einmalig' -and $_.Phase -eq 'Haltepunkt' -and (Test-PasstFirma $_ $e) -and
                                     ($_.Typ -eq 'Einmalig' -or (Test-Neuer $_ $installiert)) })) {
        Write-Host ''; Write-Host "  Haltepunkt $($m.Id)" -ForegroundColor White
        Test-Braucht $e $m $history
        Invoke-Eintrag $e $m $true
    }

    # ---------------------------------------------------------------------------------------- 5 Kopieren
    Start-Schritt '5' '5. Kopieren'
    $fotosAlt = Join-Path $e.Web 'wwwroot\Fotos\Kommissionierung'
    $fotoZahl = if (Test-Path $fotosAlt) { @(Get-ChildItem $fotosAlt -File).Count } else { 0 }
    Write-Host "  Kommissionierfotos in wwwroot\Fotos\Kommissionierung: $fotoZahl"
    $wcPaket = Join-Path $paketWeb 'web.config'; $wcZiel = Join-Path $e.Web 'web.config'
    if ($tauschWeb -and (Test-Path $wcPaket) -and (Get-Content $wcPaket -Raw) -ne (Get-Content $wcZiel -Raw)) {
        Write-Warnung 'web.config im Paket weicht vom Ziel ab - Ziel bleibt unveraendert (Freigabe-Antwort 2). Beide Fassungen:'
        Write-Host '  --- Paket'; Get-Content $wcPaket | ForEach-Object { Write-Host "    $_" }
        Write-Host '  --- Ziel'; Get-Content $wcZiel | ForEach-Object { Write-Host "    $_" }
    }
    $schutzVor = Get-Schutzstand $e
    if ($tauschWeb) { Invoke-Robocopy $paketWeb $e.Web; Copy-Item $infoDatei (Join-Path $e.Web 'deploy-info.txt') -Force }
    if ($tauschDienst) { Invoke-Robocopy $paketDienst $e.Dienst; Copy-Item $infoDatei (Join-Path $e.Dienst 'deploy-info.txt') -Force }
    if ((Get-Schutzstand $e) -ne $schutzVor) { Stop-Mit 'appsettings*.json, web.config, Branding, App_Data oder wwwroot\Fotos im Ziel veraendert. Web und Dienst bleiben gestoppt.' }
    Write-Ok "kopiert ($Komponente); appsettings*.json, web.config, Branding, App_Data, wwwroot\Fotos, logs unveraendert"

    # ---------------------------------------------------------------------------------------- 5a Ordner und Rechte
    Start-Schritt '5a' '5a. Ordner und Rechte'
    $ok = Test-OrdnerUndRechte -WebPfad $e.Web -DienstPfad $e.Dienst -PoolIdentitaet $e.PoolIdentitaet -DienstIdentitaet $e.DienstIdentitaet `
          -Umgebung $e.UmgebungWeb -DataProtectionPfad $e.KeysPath -ManifestEintraege $manifest -LogPfad $LogPfad -Herstellen
    if (-not $ok) { Stop-Mit 'Pflicht-Eintrag FEHLT (Ordner/Recht). App-Pool und Dienst bleiben gestoppt.' }

    # ---------------------------------------------------------------------------------------- 5b Vor dem Start (Freigabe-Antwort 1)
    $vor = @($manifest | Where-Object { $_.Typ -in 'Daten', 'Einmalig' -and $_.Phase -eq 'VorStart' -and (Test-PasstFirma $_ $e) -and
                                        ($_.Typ -eq 'Einmalig' -or (Test-Neuer $_ $installiert)) })
    if ($vor) {
        Start-Schritt '5b' '5b. Vor dem Start (Web und Dienst gestoppt)'
        foreach ($m in $vor) { Write-Host ''; Write-Host "  $($m.Id)" -ForegroundColor White; Invoke-Eintrag $e $m $false }
    }

    # ---------------------------------------------------------------------------------------- 6 Starten
    Start-Schritt '6' '6. Starten'
    if ($webGestoppt) {
        $start = Get-Date
        Start-WebAppPool $e.Pool
        if ((Get-Website $e.Site).state -ne 'Started') { Start-Website $e.Site }
        try { $f = Get-FooterVersion $e 300 } catch { Stop-WebAppPool $e.Pool; Stop-Mit "Startseite nicht erreichbar ($($_.Exception.GetBaseException().Message)). App-Pool gestoppt, Dienst nicht gestartet." }
        if (-not $f.Ok) { Stop-WebAppPool $e.Pool; Stop-Mit "Startseite HTTP $($f.Status). App-Pool gestoppt, Dienst nicht gestartet; $($e.Web)\logs pruefen." }
        if ($tauschWeb) {
            if ((Get-FileHash (Join-Path $e.Web 'IdealAkeWms.dll')).Hash -ne (Get-FileHash (Join-Path $paketWeb 'IdealAkeWms.dll')).Hash) {
                Stop-WebAppPool $e.Pool; Stop-Mit 'IdealAkeWms.dll im Ziel ist nicht die des Pakets. App-Pool gestoppt, Dienst nicht gestartet.'
            }
            $passt = if ($f.Version) { $f.Version -eq $PaketVersion } else { Read-Ja "Fusszeile nicht lesbar (HTTP $($f.Status), keine Windows-Anmeldung). Zeigt $($e.Url) im Browser v${PaketVersion}?" }
            if (-not $passt) { Stop-WebAppPool $e.Pool; Stop-Mit "Fusszeile v$($f.Version) statt v$PaketVersion. App-Pool gestoppt, Dienst nicht gestartet." }
        }
        Write-Ok "Web HTTP $($f.Status), Fusszeile v$($f.Version)"
        if ($fotoZahl -gt 0 -or (Test-Path (Join-Path $e.Web 'App_Data\Fotos\Kommissionierung'))) {
            $zeile = Get-ChildItem (Join-Path $e.Web 'logs') -Filter 'idealakewms-*.log' -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -ge $start } |
                     Select-String -Pattern 'Kommissionierfotos verschoben: (\d+), uebersprungen: (\d+)' | Select-Object -Last 1
            if ($zeile) {
                $n = [int]$zeile.Matches[0].Groups[1].Value; $u = [int]$zeile.Matches[0].Groups[2].Value
                Write-Host "  Log: Kommissionierfotos verschoben: $n, uebersprungen: $u (gezaehlt vorher: $fotoZahl)"
                if ($n + $u -ne $fotoZahl -or $u -gt 0) { Write-Warnung "Fotos: verschoben $n + uebersprungen $u <> gezaehlt $fotoZahl oder Rest-Dateien - Nachlauf v1.96.0-FotosRest." }
            } elseif ($fotoZahl -gt 0) { Write-Warnung "Log-Zeile 'Kommissionierfotos verschoben' nicht gefunden ($fotoZahl Fotos gezaehlt) - logs pruefen." }
        }
    }
    if ($dienstGestoppt) {
        Start-Service $e.DienstName
        Start-Sleep 30
        if ((Get-Service $e.DienstName).Status -ne 'Running') { Stop-Mit "Dienst $($e.DienstName) laeuft nach 30 s nicht. $($e.Dienst)\logs\service-*.log pruefen." }
        if ($tauschDienst -and (Get-FileHash (Join-Path $e.Dienst 'IDEALAKEWMSService.dll')).Hash -ne (Get-FileHash (Join-Path $paketDienst 'IDEALAKEWMSService.dll')).Hash) {
            Stop-Mit 'IDEALAKEWMSService.dll im Ziel ist nicht die des Pakets.'
        }
        Write-Ok "Dienst $($e.DienstName) laeuft"
    }

    # ---------------------------------------------------------------------------------------- 7 Nachlauf
    Show-Nachlauf $e $installiert $true
    return 0
}

function Show-Nachlauf($e, $installiert, [bool]$ausfuehren) {
    if ($ausfuehren) { Start-Schritt '7' "7. Nachlauf-Liste $($e.Name) (installiert vorher: $(if ($installiert) { "v$installiert" } else { 'unbekannt' }))" }
    else { Write-Schritt "7. Nachlauf-Liste $($e.Name) (nur Bericht; installiert: $(if ($installiert) { "v$installiert" } else { 'unbekannt' }))" }
    # Nachlauf-Liste: Einstellung, Leitstand, Info, Daten-/Einmal-Eintraege; SQL-Pruefbares steht im Block "Pruefungen" (Typ Pruefung)
    $liste = @($manifest | Where-Object { $_.Typ -notin 'Schema', 'Ordner', 'Pruefung' -and $_.Phase -ne 'Vorher' -and (Test-PasstFirma $_ $e) -and
                                         ($_.Typ -eq 'Einmalig' -or (Test-Neuer $_ $installiert)) })
    if (-not $liste) { Write-Host '  keine Eintraege' }
    $i = 0
    foreach ($m in $liste) {
        $i++
        Write-Host ("  {0,2}. [{1}] {2} v{3} ({4})" -f $i, $m.Typ, $m.Id, $m.Version, $m.Quelle) -ForegroundColor White
        if ($m.Typ -in 'Daten', 'Einmalig') {
            if ($ausfuehren -and $m.Phase -eq 'Nachher') { Invoke-Eintrag $e $m $false; continue }
            $erl = Get-Marker $e $m
            Write-Host "      $(if ($erl) { $erl } else { "offen ($($m.Phase), $($m.Ausfuehrung)), $(Get-Spur $e $m)" })"
            if ($erl) { continue }
        }
        if ($m['Voraussetzung']) { Write-Host "      Voraussetzung: $($m.Voraussetzung)" }
        if ($m['Pruefabfrage']) { Show-Pruefabfrage $e $m }
        if ($m['Text']) { Write-Host "      $(Format-Text $m.Text $e)" }
    }
    $pr = @($manifest | Where-Object { $_.Typ -eq 'Pruefung' -and (Test-PasstFirma $_ $e) })
    if ($pr) {
        Write-Host ''; Write-Host '  Pruefungen (Stand jetzt, 0 = in Ordnung):' -ForegroundColor White
        $history = @(); try { $history = Get-History $e } catch { }
        foreach ($m in $pr) {
            $st = Get-PruefStand $e $m $history
            Write-Host "    [$($m.Id)] $(Format-Pruefstand $st)$(if ($st -isnot [int] -or $st -ne 0) { " - $(Format-Text $m.Text $e)" })" -ForegroundColor $(if ($st -is [int] -and $st -eq 0) { 'Gray' } else { 'Yellow' })
        }
    }
    $w = @(Get-Warnungen)
    if ($w) { Write-Host ''; Write-Host '  Warnungen:' -ForegroundColor Yellow; $w | ForEach-Object { Write-Host "    - $_" -ForegroundColor Yellow } }
}

# ================================================================================================ Hauptlauf
New-Item -ItemType Directory -Path $LogPfad -Force | Out-Null
$gesamt = 0
foreach ($e in $ermittelt) {
    if ($e.Ziel.Prod) {
        $p = Read-Eingabe "  ?   Produktivsystem $($e.Name): zum Fortfahren $($Firma.ToUpper())-PROD eintippen"
        if ($p -cne "$($Firma.ToUpper())-PROD") { Write-Host '  Ende: Produktivsystem nicht bestaetigt.' -ForegroundColor Red; exit 1 }
    }
    $log = Join-Path $LogPfad ('{0:yyyy-MM-dd_HHmm}_{1}-{2}_v{3}.log' -f (Get-Date), $Firma, $e.Ziel.System, $PaketVersion)
    Start-Transcript -Path $log -Append | Out-Null
    $script:Schritt = 'Ermittlung'; $script:SchrittNr = 'Ermittlung'
    try { $gesamt = @(Invoke-System $e)[-1] }
    catch {
        Write-Host ''; Write-Host $_.Exception.Message -ForegroundColor Red; if ($_.Exception.Message -notlike 'ABBRUCH:*') { Write-Host $_.ScriptStackTrace }
        Show-Zustand $e; $gesamt = 1
    }
    finally { Stop-Transcript | Out-Null; Remove-Item Env:\SQLCMDPASSWORD -ErrorAction SilentlyContinue; Write-Host "  Protokoll: $log" }
    if ($gesamt -ne 0) {
        $idx = [array]::IndexOf($ermittelt, $e)
        $rest = @(if ($idx -lt $ermittelt.Count - 1) { $ermittelt[($idx + 1)..($ermittelt.Count - 1)] })
        if ($rest) { Write-Host "  Nicht angefasst: $(($rest.Name) -join ', ')" -ForegroundColor Red }
        exit $gesamt
    }
}
exit 0
