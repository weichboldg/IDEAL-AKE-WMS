#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Neuinstallation IDEAL-AKE WMS (Web-App + Windows-Dienst), standortneutral (AKE | IDEAL).

.DESCRIPTION
    Herkunft: Install-IdealAkeWms-IDEAL.ps1 (IDEAL-Produktivinstallation 2026-11-04), seit v1.98.0 standortneutral im Repo
    (Spec 2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec). Nur ASCII (PS 5.1). Ordner und Rechte prueft und stellt
    dieselbe Funktion her wie beim Update: Test-OrdnerUndRechte (IdealAkeWms.Deploy.psm1). Kein unbeaufsichtigter Modus.
    Stand: Buendel feature/2026-08-07-ideal-teile-1-5, v1.71.1. Grundlage: README.md des Buendels
    (IIS Z. 31-52, Dienst Z. 559-678, Sage-Zugangsdaten Z. 431-448, Branding Z. 481-506) und die
    Start-Logik in IdealAkeWms\Program.cs (Migrate + Seeds) bzw. IDEALAKEWMSService\Program.cs.

    Ablauf (jeder Schritt bricht bei einem Fehler ab):
      1. Pruefungen: Administrator, IIS + Windows-Authentifizierung + Application Initialization,
         ASP.NET Core Hosting Bundle 10, Publish-Ordner, Ziel noch nicht vorhanden.
      2. Optional: SQL Teil 1 (Schema) auf der leeren Datenbank (-SqlTeil1Ausfuehren).
      3. Web-Dateien kopieren, appsettings.Production.json schreiben, Branding, Ordnerrechte.
      4. IIS: Auth-Abschnitte entsperren, App-Pool (No Managed Code, AlwaysRunning, Idle 0, kein
         Recycling-Intervall), Site mit Bindungen, Preload.
      5. Dienst-Dateien kopieren, appsettings.json anpassen, Dienst registrieren (NICHT starten).
      6. Web aufwaermen (erster Start: Migrate + Seeds).
      7. Optional: SQL Teil 2 (Standort-Konfiguration, <FIRMA>-Prod-2-Konfiguration.sql) mit eingesetzten Parametern (-SqlTeil2Ausfuehren).
      8. Optional: Dienst starten (-DienstStarten) - erst NACH Teil 2!

    Was das Skript bewusst NICHT tut:
      - Datenbank anlegen (legst du leer vorab an).
      - Master "ProduktionsauftragHierarchisch" schalten (Einwegtor, Seite /HierarchieUmstellung).
      - admin-Passwort setzen (ist nach der Installation LEER - sofort aendern).
      - SQL-Logins/Rechte anlegen (siehe Hinweise am Ende der Ausgabe).

.EXAMPLE
    # Das AD-Konto fuer App-Pool und Dienst (ein Konto fuer beide) fragt das Skript interaktiv ab.
    .\Install-IdealAkeWms.ps1 -Firma IDEAL `
        -PublishWeb 'D:\Deploy\IDEALAKEWMSWeb' -PublishService 'D:\Deploy\IDEALAKEWMSWebService' `
        -SqlServer 'IDEALSQL01' -SageServer 'IDEALSQL01' -SageDatabase 'IDEAL' `
        -HostName 'wms.ideal.ideal-ake.at' -CertThumbprint 'AB12...' `
        -SmtpHost 'mail.ideal-ake.at' -FromAddress 'wms@ideal-ake.at' -FehlermailEmpfaenger 'it@ideal-ake.at' `
        -BrandingQuelle 'D:\Deploy\Branding' -SqlTeil1Ausfuehren -SqlTeil2Ausfuehren
#>
[CmdletBinding()]
param(
    [ValidateSet('AKE', 'IDEAL')] [string] $Firma,       # leer = Abfrage
    # --- Quellen ------------------------------------------------------------------------------------------
    [Parameter(Mandatory)] [string] $PublishWeb,        # Ergebnis: dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o ...
    [Parameter(Mandatory)] [string] $PublishService,    # Ergebnis: dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o ...
    [string] $SqlSkriptOrdner = $PSScriptRoot,          # enthaelt <FIRMA>-Prod-1-Datenbank.sql und <FIRMA>-Prod-2-Konfiguration.sql
    [string] $BrandingQuelle,                            # Ordner mit briefkopf.html (+ NimbusSanNov-*.otf); optional

    # --- Ziele --------------------------------------------------------------------------------------------
    [string] $WebPfad       = 'C:\inetpub\IDEALAKEWMS\Web',
    [string] $DienstPfad    = 'C:\Services\IDEALAKEWMSService',
    [string] $DataProtectionPfad = 'C:\ProgramData\IdealAkeWms\DataProtection-Keys',
    [string] $SiteName      = 'IDEAL AKE WMS',
    [string] $AppPoolName   = 'IDEALAKEWMS',
    [string] $DienstName    = 'IDEALAKEWMSService',     # wie README Z. 655

    # --- Bindungen ----------------------------------------------------------------------------------------
    [Parameter(Mandatory)] [string] $HostName,           # z. B. wms.ideal.ideal-ake.at
    [int]    $HttpsPort = 443,
    [string] $CertThumbprint,                            # Zertifikat in Cert:\LocalMachine\My; leer = nur HTTP (Kamera-Scan braucht HTTPS!)
    [int]    $HttpPort  = 0,                              # 0 = keine HTTP-Bindung

    # --- Datenbanken --------------------------------------------------------------------------------------
    [Parameter(Mandatory)] [string] $SqlServer,          # WMS-Datenbankserver (Instanz)
    [string] $Datenbank = 'IDEAL_AKE_WMS',               # die IDEAL-SQL-Skripte pruefen genau diesen Namen
    [Parameter(Mandatory)] [string] $SageServer,
    [Parameter(Mandatory)] [string] $SageDatabase,       # Sage-Mandant des Standorts (Views vw_IDEAL_AKE_WMS_*)

    # --- Konten -------------------------------------------------------------------------------------------
    [pscredential] $AppPoolCredential,                   # AD-Konto fuer App-Pool UND Dienst; leer = interaktive Abfrage
    [pscredential] $ServiceCredential,                   # veraltet: wird auf $AppPoolCredential gesetzt
    [string] $AdDomain = '',                             # Security:AdDomain, leer = aktuelle Domaene

    # --- Mail / Sage-Lagerbuchung -------------------------------------------------------------------------
    [string] $SmtpHost = '',
    [int]    $SmtpPort = 25,
    [switch] $SmtpSsl,
    [pscredential] $SmtpCredential,
    [string] $FromAddress = 'wms@ideal-ake.at',
    [string] $FehlermailEmpfaenger = '',
    [pscredential] $SageLagerbuchungCredential,          # SData-Servicebenutzer; leer = Sage-Lagerbuchung bleibt unkonfiguriert

    # --- Ablauf -------------------------------------------------------------------------------------------
    [switch] $SqlTeil1Ausfuehren,
    [switch] $SqlTeil2Ausfuehren,
    [switch] $DienstStarten,
    [switch] $IisFeaturesInstallieren,
    [switch] $FirewallRegel
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'IdealAkeWms.Deploy.psm1') -Force -DisableNameChecking
Assert-Administrator   # zusaetzlich zu #Requires: klare Meldung
while (-not $Firma) {
    $e = (Read-Host '  ?   Firma (AKE | IDEAL)').Trim().ToUpper()
    if ($e -in 'AKE', 'IDEAL') { $Firma = $e } else { Write-Host '      Ungueltig.' }
}

$logDir = 'C:\ProgramData\IdealAkeWms'
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
Start-Transcript -Path (Join-Path $logDir ("Install-{0}-{1:yyyyMMdd-HHmmss}.log" -f $Firma, (Get-Date))) | Out-Null

function Schritt([string]$t) { Write-Host ''; Write-Host "=== $t ===" -ForegroundColor Cyan }
function Ok([string]$t)      { Write-Host "  OK  $t" -ForegroundColor Green }
function Warn([string]$t)    { Write-Host "  !!  $t" -ForegroundColor Yellow; $script:Warnungen += $t }
function Fehler([string]$t)  { throw "ABBRUCH: $t" }
$script:Warnungen = @()

try {
    # =================================================================================================
    Schritt '1. Pruefungen'
    # =================================================================================================
    $id = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    if (-not $id.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { Fehler 'Bitte als Administrator ausfuehren.' }
    Ok 'Administrator'

    # =================================================================================================
    Schritt '0. Konten und Kennwoerter abfragen'
    # =================================================================================================
    Add-Type -AssemblyName System.DirectoryServices.AccountManagement
    function Pruefe-Domaenenkonto([pscredential]$cred) {
        # DOMAENE\benutzer oder benutzer@domaene; prueft das Kennwort gegen das AD (kein Anmelden am System)
        $u = $cred.UserName
        $domain = $null; $name = $u
        if ($u -match '^(?<d>[^\\]+)\\(?<n>.+)$') { $domain = $Matches.d; $name = $Matches.n }
        elseif ($u -match '^(?<n>[^@]+)@(?<d>.+)$') { $domain = $Matches.d; $name = $Matches.n }
        else { return 'Bitte als DOMAENE\benutzer oder benutzer@domaene eingeben.' }
        try {
            $ctx = New-Object System.DirectoryServices.AccountManagement.PrincipalContext('Domain', $domain)
            if ($ctx.ValidateCredentials($name, $cred.GetNetworkCredential().Password)) { return $null }
            return 'Benutzer oder Kennwort falsch.'
        } catch { return "AD nicht erreichbar ($($_.Exception.Message))." }
    }
    function Lies-Domaenenkonto([string]$zweck) {
        for ($i = 1; $i -le 3; $i++) {
            $c = Get-Credential -Message "$zweck (DOMAENE\benutzer)"
            if (-not $c) { Fehler "Abfrage fuer '$zweck' abgebrochen." }
            $f = Pruefe-Domaenenkonto $c
            if (-not $f) { Ok "${zweck}: $($c.UserName) geprueft"; return $c }
            Write-Host "  !!  $f" -ForegroundColor Yellow
        }
        Fehler "Konto fuer '$zweck' nach 3 Versuchen nicht bestaetigt."
    }

    # Ein AD-Konto fuer App-Pool UND Dienst (Vorgabe Gerald 2026-11-04)
    if (-not $AppPoolCredential -and $ServiceCredential) { $AppPoolCredential = $ServiceCredential }
    if (-not $AppPoolCredential) {
        Write-Host '  AD-Konto fuer IIS-App-Pool und Windows-Dienst eingeben (dasselbe Konto fuer beide).'
        $AppPoolCredential = Lies-Domaenenkonto 'AD-Konto App-Pool + Dienst'
    } else {
        $f = Pruefe-Domaenenkonto $AppPoolCredential
        if ($f) { Fehler "AD-Konto $($AppPoolCredential.UserName): $f" }
        Ok "AD-Konto $($AppPoolCredential.UserName) geprueft"
    }
    $ServiceCredential = $AppPoolCredential
    Ok "App-Pool und Dienst laufen als $($AppPoolCredential.UserName)"

    # Installationspfade abfragen (Vorgabe Gerald 2026-11-04); Enter = Standardwert, per Parameter uebergeben = keine Abfrage
    function Lies-Pfad([string]$name, [string]$standard) {
        $e = Read-Host "  $name [$standard]"
        if ([string]::IsNullOrWhiteSpace($e)) { return $standard }
        $e = $e.Trim().Trim('"').TrimEnd('\')
        if (-not [IO.Path]::IsPathRooted($e)) { Fehler "${name}: bitte absoluten Pfad angeben (eingegeben: $e)." }
        return $e
    }
    Write-Host '  Installationspfade (Enter = Vorschlag in Klammern uebernehmen):'
    if (-not $PSBoundParameters.ContainsKey('WebPfad'))            { $WebPfad            = Lies-Pfad 'Web-App (IIS-Site)' $WebPfad }
    if (-not $PSBoundParameters.ContainsKey('DienstPfad'))         { $DienstPfad         = Lies-Pfad 'Windows-Dienst' $DienstPfad }
    if (-not $PSBoundParameters.ContainsKey('DataProtectionPfad')) { $DataProtectionPfad = Lies-Pfad 'DataProtection-Schluessel' $DataProtectionPfad }
    if ($WebPfad -ieq $DienstPfad) { Fehler 'Web-App und Dienst brauchen getrennte Ordner.' }
    Ok "Web: $WebPfad"
    Ok "Dienst: $DienstPfad"
    Ok "DataProtection: $DataProtectionPfad"

    if ($Firma -eq 'IDEAL' -and ($SqlTeil1Ausfuehren -or $SqlTeil2Ausfuehren) -and $Datenbank -ne 'IDEAL_AKE_WMS') {
        Fehler "Datenbankname '$Datenbank': Die SQL-Skripte (und 00_FreshInstall.sql) sind auf IDEAL_AKE_WMS festgelegt."
    }

    $features = 'Web-Server', 'Web-Windows-Auth', 'Web-AppInit', 'Web-Mgmt-Console'
    if (Get-Command Get-WindowsFeature -ErrorAction SilentlyContinue) {
        $fehlend = $features | Where-Object { -not (Get-WindowsFeature $_).Installed }
        if ($fehlend) {
            if ($IisFeaturesInstallieren) { Install-WindowsFeature -Name $fehlend | Out-Null; Ok "Installiert: $($fehlend -join ', ')" }
            else { Fehler "Windows-Features fehlen: $($fehlend -join ', '). Mit -IisFeaturesInstallieren nachinstallieren." }
        }
        Ok 'IIS, Windows-Authentifizierung, Application Initialization'
    } else { Warn 'Get-WindowsFeature nicht verfuegbar (kein Windows Server?) - IIS-Features bitte von Hand pruefen.' }

    Import-Module WebAdministration
    if (-not (Get-WebGlobalModule -Name 'AspNetCoreModuleV2' -ErrorAction SilentlyContinue)) {
        Fehler 'ASP.NET Core Module V2 fehlt - ASP.NET Core Hosting Bundle 10.x installieren (nach IIS), dann iisreset.'
    }
    $aspnet = Get-ChildItem "$env:ProgramFiles\dotnet\shared\Microsoft.AspNetCore.App" -Directory -ErrorAction SilentlyContinue |
              Where-Object Name -like '10.*'
    if (-not $aspnet) { Fehler 'Microsoft.AspNetCore.App 10.x nicht gefunden - Hosting Bundle 10.x installieren.' }
    Ok "Hosting Bundle (ASP.NET Core $($aspnet.Name -join ', '))"

    if (-not (Test-Path (Join-Path $PublishWeb 'IdealAkeWms.dll'))) { Fehler "IdealAkeWms.dll nicht in $PublishWeb." }
    if (-not (Test-Path (Join-Path $PublishWeb 'web.config')))      { Fehler "web.config nicht in $PublishWeb." }
    $svcExe = Join-Path $PublishService 'IDEALAKEWMSService.exe'
    if (-not (Test-Path $svcExe)) { Fehler "IDEALAKEWMSService.exe nicht in $PublishService." }
    $ver = (Get-Item (Join-Path $PublishWeb 'IdealAkeWms.dll')).VersionInfo.ProductVersion
    Ok "Publish-Ordner vorhanden (Web-Version laut Datei: $ver)"

    if (Test-Path "IIS:\Sites\$SiteName")     { Fehler "IIS-Site '$SiteName' existiert bereits - dieses Skript ist nur fuer die Neuinstallation." }
    if (Test-Path "IIS:\AppPools\$AppPoolName") { Fehler "App-Pool '$AppPoolName' existiert bereits." }
    if (Get-Service -Name $DienstName -ErrorAction SilentlyContinue) { Fehler "Dienst '$DienstName' existiert bereits." }
    if ((Test-Path $WebPfad) -and (Get-ChildItem $WebPfad -Force | Select-Object -First 1)) { Fehler "$WebPfad ist nicht leer." }
    if ((Test-Path $DienstPfad) -and (Get-ChildItem $DienstPfad -Force | Select-Object -First 1)) { Fehler "$DienstPfad ist nicht leer." }
    Ok 'Ziel frei (Site, App-Pool, Dienst, Ordner)'

    if ($CertThumbprint) {
        $CertThumbprint = ($CertThumbprint -replace '\s', '').ToUpper()
        if (-not (Test-Path "Cert:\LocalMachine\My\$CertThumbprint")) { Fehler "Zertifikat $CertThumbprint nicht in LocalMachine\My." }
        Ok 'Zertifikat gefunden'
    } else {
        # SSL ist keine Pflicht (Vorgabe Gerald 2026-11-04): ohne Zertifikat nur HTTP, Standard Port 80
        if ($HttpPort -eq 0) { $HttpPort = 80 }
        Warn "Kein Zertifikat: nur HTTP auf Port $HttpPort. Kamera-Scan (Barcode/QR) funktioniert nur ueber HTTPS (README Z. 243); HTTPS-Bindung spaeter in IIS ergaenzen."
    }

    $edge = @("${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe", "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe") |
            Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($edge) { Ok "Edge gefunden (PDF-Erzeugung): $edge" } else { Warn 'Microsoft Edge fehlt - der PDF-Knopf der IDEAL-Druckdokumente bleibt ausgeblendet (README Z. 28).' }

    $sql1 = Join-Path $SqlSkriptOrdner "$Firma-Prod-1-Datenbank.sql"
    $sql2 = Join-Path $SqlSkriptOrdner "$Firma-Prod-2-Konfiguration.sql"
    if (($SqlTeil1Ausfuehren -or $SqlTeil2Ausfuehren) -and -not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        Fehler 'sqlcmd nicht gefunden - SQL-Skripte in SSMS ausfuehren oder sqlcmd installieren.'
    }
    if ($SqlTeil1Ausfuehren -and -not (Test-Path $sql1)) { Fehler "$sql1 fehlt." }
    if ($SqlTeil2Ausfuehren -and -not (Test-Path $sql2)) { Fehler "$sql2 fehlt." }

    $baseUrl = if ($CertThumbprint) { if ($HttpsPort -eq 443) { "https://$HostName" } else { "https://${HostName}:$HttpsPort" } }
               else { if ($HttpPort -eq 80) { "http://$HostName" } else { "http://${HostName}:$HttpPort" } }

    function Invoke-SqlDatei([string]$datei) {
        & sqlcmd -S $SqlServer -E -b -f 65001 -I -i $datei
        if ($LASTEXITCODE -ne 0) { Fehler "sqlcmd meldet Fehler ($LASTEXITCODE) in $datei - Ausgabe oben lesen." }
    }

    # =================================================================================================
    Schritt '2. Datenbank - Teil 1 (Schema)'
    # =================================================================================================
    if ($SqlTeil1Ausfuehren) { Invoke-SqlDatei $sql1; Ok 'Schema angelegt' }
    else { Write-Host '  --  uebersprungen (in SSMS ausfuehren, BEVOR die Web-App das erste Mal startet)' }

    # =================================================================================================
    Schritt '3. Web-App: Dateien, Konfiguration, Rechte'
    # =================================================================================================
    New-Item -ItemType Directory -Path $WebPfad -Force | Out-Null
    & robocopy $PublishWeb $WebPfad /E /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { Fehler "robocopy Web ($LASTEXITCODE)." }
    Ok "Kopiert nach $WebPfad"

    $cnWms  = "Server=$SqlServer;Database=$Datenbank;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
    $webCfg = [ordered]@{
        ConnectionStrings = [ordered]@{
            DefaultConnection  = $cnWms
            OseonConnection    = ''          # OSEON gibt es nur bei AKE; Schluessel muss existieren (BomRepository)
            EnaioDmsConnection = ''          # enaio-Sync bei IDEAL aus
        }
        Security       = [ordered]@{ AdDomain = $AdDomain }
        DataProtection = [ordered]@{ KeysPath = $DataProtectionPfad }
    }
    if ($SageLagerbuchungCredential) {
        $webCfg.SageLagerbuchung = [ordered]@{
            Username = $SageLagerbuchungCredential.UserName
            Password = $SageLagerbuchungCredential.GetNetworkCredential().Password
        }
    }
    # appsettings.Production.json ueberschreibt die Repo-appsettings.json (die auf AKESQL20 zeigt!) -
    # die web.config setzt ASPNETCORE_ENVIRONMENT=Production (README Z. 431-448).
    $webCfg | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $WebPfad 'appsettings.Production.json') -Encoding UTF8
    Ok 'appsettings.Production.json geschrieben (Verbindungen, DataProtection, AdDomain)'

    if ($BrandingQuelle) {
        if (-not (Test-Path (Join-Path $BrandingQuelle 'briefkopf.html'))) { Fehler "briefkopf.html nicht in $BrandingQuelle." }
        $brand = Join-Path $WebPfad 'Branding'
        New-Item -ItemType Directory -Path $brand -Force | Out-Null
        Copy-Item -Path (Join-Path $BrandingQuelle '*') -Destination $brand -Recurse -Force
        if (-not (Get-ChildItem $brand -Filter 'NimbusSanNov-*.otf')) { Warn 'Schriften NimbusSanNov-*.otf fehlen im Branding-Ordner (README Z. 490 ff.).' }
        Ok "Branding nach $brand"
    } else { Warn 'Kein Branding-Ordner: Briefkopf faellt auf den Ersatzkopf aus Firmenname/Anschrift zurueck.' }

    $poolPrincipal = if ($AppPoolCredential) { $AppPoolCredential.UserName } else { "IIS AppPool\$AppPoolName" }

    # =================================================================================================
    Schritt '4. IIS: App-Pool und Site'
    # =================================================================================================
    $appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
    foreach ($sec in 'windowsAuthentication', 'anonymousAuthentication') {
        & $appcmd unlock config "-section:system.webServer/security/authentication/$sec" | Out-Null
    }
    Ok 'Auth-Abschnitte auf Server-Ebene entsperrt (sonst 500.19 durch die web.config)'

    New-WebAppPool -Name $AppPoolName | Out-Null
    $pp = "IIS:\AppPools\$AppPoolName"
    # Pool bleibt gestoppt, bis das Schema aus Teil 1 da ist: sonst legt der erste Start per Migrate()
    # ein Schema OHNE FreshInstall-Daten und ohne RCSI an, und Teil 1 bricht danach ab ("nicht leer").
    Set-ItemProperty $pp -Name autoStart -Value $false
    Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    Set-ItemProperty $pp -Name managedRuntimeVersion -Value ''                     # No Managed Code
    Set-ItemProperty $pp -Name startMode -Value 'AlwaysRunning'
    Set-ItemProperty $pp -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)
    Set-ItemProperty $pp -Name recycling.periodicRestart.time -Value ([TimeSpan]::Zero)
    Set-ItemProperty $pp -Name processModel.loadUserProfile -Value $true          # %TEMP% fuer die PDF-Erzeugung
    if ($AppPoolCredential) {
        Set-ItemProperty $pp -Name processModel -Value @{
            identityType = 3; userName = $AppPoolCredential.UserName; password = $AppPoolCredential.GetNetworkCredential().Password }
        Ok "App-Pool '$AppPoolName' mit Konto $($AppPoolCredential.UserName)"
    } else {
        Warn "App-Pool laeuft als ApplicationPoolIdentity -> SQL-Login fuer das Computerkonto ($env:USERDOMAIN\$env:COMPUTERNAME`$) noetig."
        Ok "App-Pool '$AppPoolName' (ApplicationPoolIdentity)"
    }

    # Rechte erst nach dem Anlegen des Pools (die virtuelle Identitaet existiert vorher nicht). Lesen Web-Ordner setzt nur dieses Skript;
    # logs, App_Data\Fotos und DataProtection stellt Schritt 5b mit Test-OrdnerUndRechte her (gleiche Pruefung wie beim Update).
    & icacls $WebPfad /grant "${poolPrincipal}:(OI)(CI)RX" /T /Q | Out-Null
    Ok "Recht Lesen+Ausfuehren Web-Ordner fuer $poolPrincipal"

    if ($CertThumbprint) {
        New-Website -Name $SiteName -PhysicalPath $WebPfad -ApplicationPool $AppPoolName `
            -Port $HttpsPort -HostHeader $HostName -Ssl -SslFlags 1 | Out-Null
        (Get-WebBinding -Name $SiteName -Protocol https).AddSslCertificate($CertThumbprint, 'My')
        if ($HttpPort -gt 0) { New-WebBinding -Name $SiteName -Protocol http -Port $HttpPort -HostHeader $HostName }
    } else {
        New-Website -Name $SiteName -PhysicalPath $WebPfad -ApplicationPool $AppPoolName -Port $HttpPort -HostHeader $HostName | Out-Null
    }
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationDefaults.preloadEnabled -Value $true
    Set-WebConfigurationProperty -PSPath 'IIS:\' -Location $SiteName -Filter 'system.webServer/security/authentication/windowsAuthentication' -Name enabled -Value $true
    Set-WebConfigurationProperty -PSPath 'IIS:\' -Location $SiteName -Filter 'system.webServer/security/authentication/anonymousAuthentication' -Name enabled -Value $true
    if ((Get-WebAppPoolState -Name $AppPoolName).Value -ne 'Stopped') { Stop-WebAppPool -Name $AppPoolName }
    Ok "Site '$SiteName' ($baseUrl), Preload an, Windows- und anonyme Authentifizierung an"

    if ($FirewallRegel) {
        $ports = @(); if ($CertThumbprint) { $ports += $HttpsPort }; if ($HttpPort -gt 0) { $ports += $HttpPort }
        New-NetFirewallRule -DisplayName "IDEAL AKE WMS ($($ports -join ','))" -Direction Inbound -Protocol TCP -LocalPort $ports -Action Allow | Out-Null
        Ok 'Firewall-Regel angelegt'
    }

    # =================================================================================================
    Schritt '5. Windows-Dienst: Dateien, Konfiguration, Registrierung'
    # =================================================================================================
    New-Item -ItemType Directory -Path $DienstPfad -Force | Out-Null
    & robocopy $PublishService $DienstPfad /E /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { Fehler "robocopy Dienst ($LASTEXITCODE)." }

    $svcJsonPfad = Join-Path $DienstPfad 'appsettings.json'
    $svc = Get-Content $svcJsonPfad -Raw -Encoding UTF8 | ConvertFrom-Json
    $svc.ConnectionStrings.DefaultConnection  = "Server=$SqlServer;Database=$Datenbank;Trusted_Connection=True;TrustServerCertificate=True;"
    $svc.ConnectionStrings.SageConnection     = "Server=$SageServer;Database=$SageDatabase;Trusted_Connection=True;TrustServerCertificate=True;"
    $svc.ConnectionStrings.OseonConnection    = ''
    $svc.ConnectionStrings.EnaioDmsConnection = ''
    $svc.MailSettings.SmtpHost    = $SmtpHost
    $svc.MailSettings.SmtpPort    = $SmtpPort
    $svc.MailSettings.SmtpUseSsl  = [bool]$SmtpSsl
    $svc.MailSettings.FromAddress = $FromAddress
    if ($SmtpCredential) {
        $svc.MailSettings.SmtpUsername = $SmtpCredential.UserName
        $svc.MailSettings.SmtpPassword = $SmtpCredential.GetNetworkCredential().Password
    }
    if ($SageLagerbuchungCredential) {
        $svc.SageLagerbuchung.Username = $SageLagerbuchungCredential.UserName
        $svc.SageLagerbuchung.Password = $SageLagerbuchungCredential.GetNetworkCredential().Password
    }
    # Nur Default-Referenz (die DB gewinnt), aber gegen Fehlstart abgesichert:
    $svc.Sync.ProductionOrdersEnabled = $false
    $svc | ConvertTo-Json -Depth 30 | Set-Content -Path $svcJsonPfad -Encoding UTF8
    Ok 'appsettings.json des Dienstes angepasst (Verbindungen, Mail; ProductionOrdersEnabled=false)'

    $svcPrincipal = if ($ServiceCredential) { $ServiceCredential.UserName } else { 'NT AUTHORITY\SYSTEM' }
    & icacls $DienstPfad /grant "${svcPrincipal}:(OI)(CI)M" /T /Q | Out-Null     # logs\ relativ zur EXE

    $np = @{
        Name           = $DienstName
        DisplayName    = 'IDEAL AKE WMS Service'
        BinaryPathName = "`"$(Join-Path $DienstPfad 'IDEALAKEWMSService.exe')`""
        StartupType    = 'Automatic'
        Description    = 'Hintergrundprozesse fuer IDEAL AKE WMS (Sync + Benachrichtigungen)'
    }
    if ($ServiceCredential) {
        # "Als Dienst anmelden" - New-Service vergibt das Recht nicht selbst (sonst Fehler 1069 beim Start)
        $sid = (New-Object Security.Principal.NTAccount($ServiceCredential.UserName)).Translate([Security.Principal.SecurityIdentifier]).Value
        $cfg = Join-Path $env:TEMP 'idealakewms-secpol.inf'
        $db  = Join-Path $env:TEMP 'idealakewms-secpol.sdb'
        & secedit /export /cfg $cfg /areas USER_RIGHTS /quiet | Out-Null
        $zeilen = Get-Content $cfg -Encoding Unicode
        $idx = [Array]::FindIndex([string[]]$zeilen, [Predicate[string]]{ param($z) $z -like 'SeServiceLogonRight*' })
        if ($idx -ge 0 -and $zeilen[$idx] -notmatch [regex]::Escape("*$sid")) { $zeilen[$idx] += ",*$sid" }
        elseif ($idx -lt 0) {
            $pos = [Array]::IndexOf($zeilen, '[Privilege Rights]')
            $liste = [Collections.Generic.List[string]]$zeilen; $liste.Insert($pos + 1, "SeServiceLogonRight = *$sid"); $zeilen = $liste.ToArray()
        }
        Set-Content -Path $cfg -Value $zeilen -Encoding Unicode
        & secedit /configure /db $db /cfg $cfg /areas USER_RIGHTS /quiet | Out-Null
        Remove-Item $cfg, $db -Force -ErrorAction SilentlyContinue
        Ok "Recht 'Als Dienst anmelden' fuer $($ServiceCredential.UserName)"
        $np.Credential = $ServiceCredential
    }
    else { Warn "Dienst laeuft als LocalSystem -> SQL-Login fuer das Computerkonto noetig (Sage-DB lesend inkl. PPSImport)." }
    New-Service @np | Out-Null
    & sc.exe failure $DienstName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
    Ok "Dienst '$DienstName' registriert (Autostart, Neustart bei Absturz) - NICHT gestartet"

    # =================================================================================================
    Schritt '5b. Ordner und Rechte (Test-OrdnerUndRechte, wie Update-IdealAkeWms.ps1 Schritt 5a)'
    # =================================================================================================
    $manifestDatei = Join-Path $PSScriptRoot 'nachlauf.psd1'
    $manifest = if (Test-Path $manifestDatei) { @((Import-PowerShellDataFile $manifestDatei).Eintraege) } else { @() }
    $ordnerOk = Test-OrdnerUndRechte -WebPfad $WebPfad -DienstPfad $DienstPfad -PoolIdentitaet $poolPrincipal -DienstIdentitaet $svcPrincipal `
                -DataProtectionPfad $DataProtectionPfad -ManifestEintraege $manifest -LogPfad $logDir -Herstellen
    if (-not $ordnerOk) { Fehler 'Ordner/Rechte: Pflicht-Eintrag FEHLT (Liste oben).' }

    # =================================================================================================
    Schritt '6. Web-App aufwaermen (erster Start: Migrate + Seeds)'
    # =================================================================================================
    function Get-SchemaStand {
        $cn = New-Object System.Data.SqlClient.SqlConnection "Server=$SqlServer;Database=$Datenbank;Integrated Security=true;TrustServerCertificate=true"
        try {
            $cn.Open()
            $cmd = $cn.CreateCommand()
            $cmd.CommandText = "SELECT CASE WHEN OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL THEN -1 ELSE (SELECT COUNT(*) FROM dbo.__EFMigrationsHistory) END"
            [int]$cmd.ExecuteScalar()
        } finally { $cn.Dispose() }
    }
    try { $migrationen = Get-SchemaStand }
    catch { Warn "Schema-Pruefung nicht moeglich ($($_.Exception.Message))."; $migrationen = -2 }
    $script:WebGestartet = $false
    if ($migrationen -lt 75) {
        Warn "Schema fehlt oder ist unvollstaendig ($migrationen Migrationen statt 75). App-Pool bleibt GESTOPPT. Erst Teil 1 ausfuehren, dann: Set-ItemProperty 'IIS:\AppPools\$AppPoolName' -Name autoStart -Value `$true; Start-WebAppPool '$AppPoolName'; Seite aufrufen."
    } else {
        Ok "Schema vorhanden ($migrationen Migrationen)"
        Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name autoStart -Value $true
        Start-WebAppPool -Name $AppPoolName
        Start-Sleep -Seconds 5
        try {
            $r = Invoke-WebRequest -Uri "$baseUrl/" -UseDefaultCredentials -UseBasicParsing -TimeoutSec 180 -MaximumRedirection 5
            Ok "Antwort $($r.StatusCode) von $baseUrl"; $script:WebGestartet = $true
        } catch {
            $code = $null
            if ($_.Exception.PSObject.Properties['Response'] -and $_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
            if ($code -in 401, 403) { Ok "App antwortet ($code - Anmeldung verlangt, Start ist erfolgt)"; $script:WebGestartet = $true }
            else { Warn "Aufwaermen fehlgeschlagen: $($_.Exception.Message). Seite im Browser oeffnen und $WebPfad\logs pruefen (stdout_*.log, idealakewms-*.log)." }
        }
    }

    # =================================================================================================
    Schritt '7. Datenbank - Teil 2 (Standort-Konfiguration)'
    # =================================================================================================
    if ($SqlTeil2Ausfuehren -and -not $script:WebGestartet) {
        Warn 'Teil 2 uebersprungen: Die Web-App ist noch nicht gestartet (Teil 2 prueft das und bricht sonst ab).'
    } elseif ($SqlTeil2Ausfuehren) {
        $tmp = Join-Path $env:TEMP "$Firma-Prod-2-Konfiguration.ausgefuellt.sql"
        $txt = Get-Content $sql2 -Raw -Encoding UTF8
        $txt = $txt.Replace('<https://wms.ideal-ake.at>', $baseUrl).
                    Replace('<it@ideal-ake.at>', $FehlermailEmpfaenger).
                    Replace('<C:\inetpub\IDEALAKEWMS\Web\Branding\briefkopf.html>', (Join-Path $WebPfad 'Branding\briefkopf.html'))
        if ($txt -match "<IDEAL-AKE-WMS_Users>") { Warn 'WindowsAuthBerechtigungsgruppe ist noch Platzhalter - in /Settings setzen, bevor WindowsAuthAktiv an geht.' }
        [IO.File]::WriteAllText($tmp, $txt, (New-Object Text.UTF8Encoding($true)))
        Invoke-SqlDatei $tmp
        Ok "$Firma-Konfiguration geschrieben"
    } else { Write-Host '  --  uebersprungen (in SSMS ausfuehren, Parameter oben im Skript ersetzen)' }

    # =================================================================================================
    Schritt '8. Dienst'
    # =================================================================================================
    if ($DienstStarten) {
        if (-not $SqlTeil2Ausfuehren) { Warn 'Dienst wird gestartet, obwohl Teil 2 nicht in diesem Lauf ausgefuehrt wurde - vorher sicherstellen, dass Teil 2 gelaufen ist!' }
        Start-Service -Name $DienstName
        Ok 'Dienst gestartet - erster Zyklus nach ca. 1 Minute, danach alle 15 Minuten (/SyncLog)'
    } else { Write-Host "  --  nicht gestartet. Nach Teil 2:  Start-Service -Name '$DienstName'" }

    # =================================================================================================
    Schritt 'Fertig - naechste Schritte'
    # =================================================================================================
    @"
  1. $baseUrl oeffnen, als admin anmelden (Passwort LEER) und sofort ein Passwort setzen (Mein Profil).
  2. SQL-Rechte (falls noch nicht): Web-Konto '$poolPrincipal' und Dienst-Konto '$svcPrincipal'
       - WMS-DB [$Datenbank]: db_datareader, db_datawriter, EXECUTE (Web zusaetzlich db_ddladmin nur,
         falls kuenftige Updates per Migrate() laufen sollen)
       - Sage-DB [$SageDatabase] (nur Dienst): lesen auf die Views vw_IDEAL_AKE_WMS_* und KHK-Tabellen,
         zusaetzlich Leserecht in PPSImport (UAT IDEAL 2026-08-11).
  3. Dienst starten (falls nicht geschehen). Erster Zyklus laeuft mit DryRun (Teil 2): /SyncLog pruefen,
     dann /ServiceSettings -> WorkerSettings:SyncDryRun = false.
  4. Nach erfolgreichem Struktur-Import: Backup, dann /HierarchieUmstellung (Einwegtor).
     Danach Werkbank-Anlage und Arbeitsgang-Erkennung mit DryRun (Abschnitt "Danach" in Teil 2).
  5. Benutzer anlegen, Rollen vergeben (beschichtungsauftrag / _schreiben sind angelegt).
  6. WindowsAuthAktiv erst einschalten, wenn die ersten AD-Benutzer angelegt sind.
"@ | Write-Host
    if ($script:Warnungen) {
        Write-Host ''; Write-Host 'Warnungen:' -ForegroundColor Yellow
        $script:Warnungen | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    }
}
catch {
    Write-Host ''
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host 'Bereits angelegte Teile bleiben bestehen (Site/App-Pool/Dienst/Ordner) - vor einem neuen Lauf entfernen.' -ForegroundColor Red
    exit 1
}
finally {
    Stop-Transcript | Out-Null
}
