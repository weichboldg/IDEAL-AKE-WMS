#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Update IDEAL-AKE WMS (Web + Dienst) je Zielsystem, lokal auf dem Zielserver, mit Nachlauf-Liste.

.DESCRIPTION
    Spec 2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec, ADR 0022. Windows PowerShell 5.1, Administrator, nur von Hand.
    Ablauf je System: Ermittlung (Ordner, Site/App-Pool, Dienst, DB aus appsettings) -> Bestaetigung -> Vorpruefung (lesend)
    -> Backup -> Stoppen -> Schema (SQL in Manifest-Reihenfolge, je Skript Bestaetigung) -> Haltepunkt -> Kopieren (robocopy ohne
    Loeschen) -> Ordner und Rechte -> Starten (Web vor Dienst, Version pruefen) -> Nachlauf-Liste.
    Fuehrt SQL/108, SQL/110 und SQL/Einmalig/* NIE scharf aus (ADR 0015); hoechstens Trockenlauf. Kein Zugriff auf Sage.

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
    if (-not $z) { Stop-Mit "System '$s' gibt es fuer $Firma nicht in ziele.psd1 ($(($firmaZiele.System) -join ', '))." }
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
$sqlcmd = (Get-Command sqlcmd -ErrorAction SilentlyContinue | Select-Object -First 1).Source
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
    if ($dienste.Count -ne 1) { Stop-Mit "Dienst: $($dienste.Count) Dienste mit $exe statt genau einem: $(($dienste.Name) -join ', ')" }
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
function Get-Spur($e, $m) {
    if (-not $m['Spur']) { return 'unbekannt, Deploy-Protokoll pruefen' }
    try { $n = [int](Invoke-Lesen $e $m.Spur).Rows[0][0]; if ($n -gt 0) { return "Spur: $n (lief schon)" } else { return 'Spur: 0 (nicht gelaufen oder ohne Treffer)' } }
    catch { return "Spur-Abfrage fehlgeschlagen: $($_.Exception.Message)" }
}
function Test-Braucht($e, $m, $history) {
    foreach ($b in @($m['Braucht'] | Where-Object { $_ })) {
        $mid = ($manifest | Where-Object { $_.Id -eq $b }).MigrationId
        if ($history -notcontains $mid) { Stop-Mit "$($m.Id) braucht $b ($mid), das fehlt in der History." }
    }
}
function Test-Trockenlauf([string]$datei) {
    $t = Get-Content $datei -Raw
    return ($t.Contains('DECLARE @Trockenlauf BIT = 1;') -and $t -notmatch 'DECLARE\s+@Trockenlauf\s+BIT\s*=\s*0')
}
function Invoke-Trockenlauf($e, $m) {
    $datei = Join-Path $SqlOrdner $m.Datei
    if (-not (Test-Path $datei)) { Write-Warnung "$($m.Id): $datei fehlt."; return }
    if (-not (Test-Trockenlauf $datei)) { Write-Warnung "$($m.Id): Datei enthaelt nicht woertlich 'DECLARE @Trockenlauf BIT = 1;' -> abgelehnt, nur Anzeige."; return }
    if (-not (Read-Ja "$($m.Id) im TROCKENLAUF ausfuehren (Datei unveraendert, aendert nichts)?")) { return }
    $aus = Join-Path $LogPfad ('{0:yyyy-MM-dd_HHmm}_{1}_{2}_trockenlauf.txt' -f (Get-Date), $e.Name, ($m.Id -replace '[^A-Za-z0-9.-]', '_'))
    $r = Invoke-SqlDatei $e $datei $aus
    if ($r.Exit -ne 0) { Write-Warnung "$($m.Id): Trockenlauf mit Exit $($r.Exit)." }
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
    $tauschWeb = $Komponente -in 'Web', 'Beide'; $tauschDienst = $Komponente -in 'Dienst', 'Beide'
    $webGestoppt = $false; $dienstGestoppt = $false

    Write-Schritt "$($e.Name): Ermittlung"
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
    Write-Schritt '1. Vorpruefung (lesend)'
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
    $rcsi = (Invoke-Lesen $e 'SELECT is_read_committed_snapshot_on FROM sys.databases WHERE name = DB_NAME()').Rows[0][0]
    Write-Host "  RCSI: $rcsi"
    if ($Firma -eq 'IDEAL' -and -not $rcsi) { Write-Warnung 'RCSI ist an IDEAL aus (v1.31.0 verlangt is_read_committed_snapshot_on = 1).' }
    foreach ($m in $manifest | Where-Object { $_.Typ -eq 'Einmalig' -and (Test-PasstFirma $_ $e) }) { Write-Host "  $($m.Id): $(Get-Spur $e $m)" }
    $vorher = @($manifest | Where-Object { $_.Phase -eq 'Vorher' -and $_.Typ -ne 'Ordner' -and (Test-PasstFirma $_ $e) -and (Test-Neuer $_ $installiert) })
    if ($vorher) { Write-Host '  Vorher:'; $vorher | ForEach-Object { Write-Host "    [$($_.Id)] $(Format-Text $_.Text $e)" } }
    $halte = @($manifest | Where-Object { $_.Phase -eq 'Haltepunkt' -and (Test-PasstFirma $_ $e) -and (Test-Neuer $_ $installiert) } |
               Where-Object { -not ((Get-Spur $e $_) -like '*lief schon*') })
    $stopWeb = $tauschWeb -or $fehlend -or $halte; $stopDienst = $tauschDienst -or $fehlend -or $halte
    Write-Host "  Plan: Backup-Angebot; stoppen: $(if ($stopWeb) { 'App-Pool ' })$(if ($stopDienst) { 'Dienst' }); Schema: $($fehlend.Count) Skript(e); Haltepunkt: $(if ($halte) { ($halte.Id) -join ', ' } else { 'keiner' }); kopieren: $Komponente; Ordner/Rechte; starten; Nachlauf."

    if ($NurPruefen) {
        Write-Schritt '5a. Ordner und Rechte (nur Bericht)'
        [void](Test-OrdnerUndRechte -WebPfad $e.Web -DienstPfad $e.Dienst -PoolIdentitaet $e.PoolIdentitaet -DienstIdentitaet $e.DienstIdentitaet `
               -Umgebung $e.UmgebungWeb -DataProtectionPfad $e.KeysPath -ManifestEintraege $manifest -LogPfad $LogPfad)
        Show-Nachlauf $e $installiert $false
        Write-Host ''; Write-Host "  -NurPruefen: nichts gestoppt, nichts geschrieben." -ForegroundColor Green
        return 0
    }

    # ---------------------------------------------------------------------------------------- 2 Backup
    Write-Schritt '2. Backup'
    Write-Host '  Pflicht bei SQL/90, 95, 109/110, 133/Altbestand (ADR 0004).'
    if (Read-Ja "Jetzt BACKUP DATABASE [$($e.Db.Datenbank)] WITH COPY_ONLY, CHECKSUM anlegen?") {
        $ziel = $BackupPfad
        if (-not $ziel) { $ziel = [string](Invoke-Lesen $e "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(4000))").Rows[0][0] }
        # Pfad liegt auf dem SQL-Server: kein Join-Path (PS 5.1 wirft, wenn das Laufwerk hier am Web-Server fehlt)
        $bak = '{0}\{1}_{2:yyyyMMdd_HHmm}_vor_v{3}.bak' -f $ziel.TrimEnd('\'), $e.Db.Datenbank, (Get-Date), $PaketVersion
        Write-Host "  Ziel (Pfad auf dem SQL-Server $($e.Db.Server)): $bak"
        $cn = New-Object System.Data.SqlClient.SqlConnection $e.Db.Cs
        try {
            $cn.Open(); $cmd = $cn.CreateCommand(); $cmd.CommandTimeout = 0
            $cmd.CommandText = "BACKUP DATABASE [$($e.Db.Datenbank.Replace(']', ']]'))] TO DISK = N'$($bak.Replace("'", "''"))' WITH COPY_ONLY, CHECKSUM"
            [void]$cmd.ExecuteNonQuery(); Write-Ok "Backup $bak"
        } catch { Stop-Mit "Backup fehlgeschlagen: $($_.Exception.Message)" } finally { $cn.Dispose() }
    } elseif (-not (Read-Ja 'Liegt ein aktuelles Backup dieser DB vor?')) { Stop-Mit 'kein Backup.' }

    # ---------------------------------------------------------------------------------------- 3 Stoppen
    Write-Schritt '3. Stoppen'
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
    Write-Schritt '4. Schema'
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

    # ---------------------------------------------------------------------------------------- 4a Haltepunkt
    foreach ($m in $halte) {
        Write-Schritt "4a. Haltepunkt $($m.Id)"
        Test-Braucht $e $m $history
        Write-Host "  $(Format-Text $m.Text $e)"
        if ($m.Ausfuehrung -eq 'Trockenlauf') { Invoke-Trockenlauf $e $m }
        Write-Host "  Scharf (von Hand, SSMS): Kopie von $(Join-Path $SqlOrdner $m.Datei) mit @Trockenlauf = 0 gegen $($e.Db.Server)/$($e.Db.Datenbank)."
        $a = Read-Eingabe "  ?   'ausgefuehrt' eingeben, wenn das Skript scharf gelaufen ist (sonst Abbruch, Web bleibt gestoppt)"
        if (([string]$a).Trim() -ne 'ausgefuehrt') { Stop-Mit "Haltepunkt $($m.Id) nicht ausgefuehrt. Web und Dienst bleiben gestoppt." }
        Write-Host "  $(Get-Spur $e $m)"
    }

    # ---------------------------------------------------------------------------------------- 5 Kopieren
    Write-Schritt '5. Kopieren'
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
    Write-Schritt '5a. Ordner und Rechte'
    $ok = Test-OrdnerUndRechte -WebPfad $e.Web -DienstPfad $e.Dienst -PoolIdentitaet $e.PoolIdentitaet -DienstIdentitaet $e.DienstIdentitaet `
          -Umgebung $e.UmgebungWeb -DataProtectionPfad $e.KeysPath -ManifestEintraege $manifest -LogPfad $LogPfad -Herstellen
    if (-not $ok) { Stop-Mit 'Pflicht-Eintrag FEHLT (Ordner/Recht). App-Pool und Dienst bleiben gestoppt.' }

    # ---------------------------------------------------------------------------------------- 6 Starten
    Write-Schritt '6. Starten'
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
            $passt = if ($f.Version) { $f.Version -eq $PaketVersion } else { Read-Ja "Fusszeile nicht lesbar (HTTP $($f.Status), keine Windows-Anmeldung). Zeigt $($e.Url) im Browser v$PaketVersion?" }
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

function Show-Nachlauf($e, $installiert, [bool]$trockenlaeufe) {
    Write-Schritt "7. Nachlauf-Liste $($e.Name) (installiert vorher: $(if ($installiert) { "v$installiert" } else { 'unbekannt' }))"
    $liste = @($manifest | Where-Object { $_.Typ -notin 'Schema', 'Ordner' -and $_.Phase -ne 'Vorher' -and (Test-PasstFirma $_ $e) -and
                                         ($_.Typ -eq 'Einmalig' -or (Test-Neuer $_ $installiert)) })
    if (-not $liste) { Write-Host '  keine Eintraege' }
    $i = 0
    foreach ($m in $liste) {
        $i++
        Write-Host ("  {0,2}. [{1}] {2} v{3} ({4})" -f $i, $m.Typ, $m.Id, $m.Version, $m.Quelle) -ForegroundColor White
        if ($m['Voraussetzung']) { Write-Host "      Voraussetzung: $($m.Voraussetzung)" }
        if ($m.Typ -eq 'Einmalig') { Write-Host "      $(Get-Spur $e $m)" }
        if ($m['Pruefabfrage']) {
            try { (Invoke-Lesen $e $m.Pruefabfrage).Rows | ForEach-Object { Write-Host "      Pruefabfrage: $(($_.ItemArray) -join ' | ')" } }
            catch { Write-Host "      Pruefabfrage fehlgeschlagen: $($_.Exception.Message)" }
        }
        if ($m['Text']) { Write-Host "      $(Format-Text $m.Text $e)" }
        if ($trockenlaeufe -and $m.Phase -eq 'Nachher' -and $m.Ausfuehrung -eq 'Trockenlauf') { Invoke-Trockenlauf $e $m }
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
    try { $gesamt = @(Invoke-System $e)[-1] }
    catch { Write-Host ''; Write-Host $_.Exception.Message -ForegroundColor Red; if ($_.Exception.Message -notlike 'ABBRUCH:*') { Write-Host $_.ScriptStackTrace }; $gesamt = 1 }
    finally { Stop-Transcript | Out-Null; Remove-Item Env:\SQLCMDPASSWORD -ErrorAction SilentlyContinue; Write-Host "  Protokoll: $log" }
    if ($gesamt -ne 0) {
        $idx = [array]::IndexOf($ermittelt, $e)
        $rest = @(if ($idx -lt $ermittelt.Count - 1) { $ermittelt[($idx + 1)..($ermittelt.Count - 1)] })
        if ($rest) { Write-Host "  Nicht angefasst: $(($rest.Name) -join ', ')" -ForegroundColor Red }
        exit $gesamt
    }
}
exit 0
