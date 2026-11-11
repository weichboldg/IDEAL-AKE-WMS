# Gemeinsame Funktionen fuer Update-IdealAkeWms.ps1 und Install-IdealAkeWms.ps1 (Windows PowerShell 5.1). Nur ASCII.
# Spec 2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec, Abschnitt "Ordner und Rechte"; ADR 0022.
# Rechte werden nur HINZUGEFUEGT (icacls /grant). Nie zuruecksetzen, nie Eintraege entfernen, nie Vererbung kappen, nie Besitz aendern.

function Write-Schritt([string]$t) { Write-Host ''; Write-Host "=== $t ===" -ForegroundColor Cyan }
function Write-Ok([string]$t)      { Write-Host "  OK  $t" -ForegroundColor Green }
function Write-Warnung([string]$t) { Write-Host "  !!  $t" -ForegroundColor Yellow; $script:Warnungen += $t }
function Get-Warnungen { $script:Warnungen }
function Reset-Warnungen { $script:Warnungen = @() }
$script:Warnungen = @()

function Assert-Administrator {
    $p = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    if (-not $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'ABBRUCH: Bitte in einer Administrator-PowerShell ausfuehren (Rechtsklick > Als Administrator ausfuehren).'
    }
}

# j/n-Abfrage; wiederholt bis zu einer gueltigen Antwort. Kein Schalter ueberspringt Bestaetigungen (Betrieb Gerald 2026-11-11).
# Eingabe; leere/geschlossene Eingabe (z. B. Pipe zu Ende, PS 5.1 wirft dann) = Leerstring.
function Read-Eingabe([string]$text) { try { return ([string](Read-Host $text)).Trim() } catch { return '' } }

function Read-Ja([string]$frage) {
    for ($i = 0; $i -lt 5; $i++) {
        $e = Read-Eingabe "  ?   $frage [j/n]"
        if (-not $e) { return $false }
        switch -Regex ($e.Trim().ToLower()) { '^(j|ja|y|yes)$' { return $true } '^(n|nein|no)$' { return $false } }
        Write-Host '      Bitte j oder n eingeben.'
    }
    return $false
}

function Import-Ziele([string]$pfad) {
    if (-not (Test-Path $pfad)) { throw "ABBRUCH: Zieldatei $pfad fehlt." }
    $ziele = @((Import-PowerShellDataFile $pfad).Ziele)
    $gesehen = @{}
    foreach ($z in $ziele) {
        foreach ($f in 'Firma', 'System', 'WebPfad', 'DienstPfad') { if (-not $z[$f]) { throw "ABBRUCH: ${pfad}: Eintrag ohne $f." } }
        # Doppelt je Firma (= je Server): Firma+System, Web-Pfad, Dienst-Pfad; Windows-Pfade ohne Gross/Klein
        foreach ($k in @("S|$($z.Firma)|$($z.System)", "W|$($z.Firma)|$($z.WebPfad.TrimEnd('\'))", "D|$($z.Firma)|$($z.DienstPfad.TrimEnd('\'))")) {
            $kk = $k.ToLowerInvariant()
            if ($gesehen.ContainsKey($kk)) { throw "ABBRUCH: ${pfad}: doppelter Eintrag ($($k.Substring(2)))." }
            $gesehen[$kk] = $true
        }
    }
    return $ziele
}

# ---------------------------------------------------------------------------------------------- Rechte (wirksam, nicht geraten)
$script:RechtBits = @{
    Lesen           = [int64][Security.AccessControl.FileSystemRights]::Read
    LesenAusfuehren = [int64][Security.AccessControl.FileSystemRights]::ReadAndExecute
    Aendern         = [int64][Security.AccessControl.FileSystemRights]::Modify
}
$script:RechtIcacls = @{ Lesen = 'R'; LesenAusfuehren = 'RX'; Aendern = 'M' }

function ConvertTo-Bits($rechte) {
    $b = [int64]$rechte.value__; if ($b -lt 0) { $b += 4294967296 }
    $full = [int64][Security.AccessControl.FileSystemRights]::FullControl
    if ($b -band 0x10000000) { $b = $b -bor $full }                                                                  # GENERIC_ALL
    if ($b -band 0x20000000) { $b = $b -bor [int64][Security.AccessControl.FileSystemRights]::ReadAndExecute }      # GENERIC_EXECUTE
    if ($b -band 0x40000000) { $b = $b -bor [int64][Security.AccessControl.FileSystemRights]::Write }               # GENERIC_WRITE
    if ($b -band 0x80000000) { $b = $b -bor [int64][Security.AccessControl.FileSystemRights]::Read }                # GENERIC_READ
    return $b
}

# SIDs der Identitaet: eigene SID, Gruppen (wenn aufloesbar), Jeder/Authentifizierte Benutzer/Benutzer, IIS_IUSRS bei Pool-Identitaeten.
# Nicht aufloesbar -> $null (Aufrufer meldet FEHLT, nicht raten).
function Get-IdentitaetSids([string]$identitaet) {
    try { $sid = (New-Object Security.Principal.NTAccount($identitaet)).Translate([Security.Principal.SecurityIdentifier]).Value }
    catch { return $null }
    $s = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($x in @($sid, 'S-1-1-0', 'S-1-5-11', 'S-1-5-32-545')) { [void]$s.Add($x) }
    if ($identitaet -like 'IIS AppPool\*') { [void]$s.Add('S-1-5-32-568') }
    elseif ($sid -in 'S-1-5-18', 'S-1-5-19', 'S-1-5-20') { if ($sid -eq 'S-1-5-18') { [void]$s.Add('S-1-5-32-544') } }
    elseif ($identitaet -notlike 'NT SERVICE\*') {
        try { $wi = New-Object Security.Principal.WindowsIdentity($identitaet); foreach ($g in $wi.Groups) { [void]$s.Add($g.Value) } }
        catch { }   # Gruppen nicht aufloesbar: nur direkte und allgemeine Eintraege zaehlen (fehlt etwas -> FEHLT)
    }
    return , $s
}

function Test-Recht([string]$pfad, [string]$identitaet, [string]$recht) {
    if (-not (Test-Path -LiteralPath $pfad)) { return $false }
    $sids = Get-IdentitaetSids $identitaet
    if (-not $sids) { return $false }
    $need = $script:RechtBits[$recht]; $allow = [int64]0; $deny = [int64]0
    foreach ($r in (Get-Acl -LiteralPath $pfad).Access) {
        if ($r.PropagationFlags -band [Security.AccessControl.PropagationFlags]::InheritOnly) { continue }
        try { $rsid = $r.IdentityReference.Translate([Security.Principal.SecurityIdentifier]).Value } catch { continue }
        if (-not $sids.Contains($rsid)) { continue }
        $bits = ConvertTo-Bits $r.FileSystemRights
        if ($r.AccessControlType -eq 'Allow') { $allow = $allow -bor $bits } else { $deny = $deny -bor $bits }
    }
    return ((($allow -band $need) -eq $need) -and (($deny -band $need) -eq 0))
}

# Identitaet des App-Pools: ApplicationPoolIdentity -> IIS AppPool\<Pool>, SpecificUser -> userName, sonst Konto mit Warnung.
function Get-PoolIdentitaet([string]$pool) {
    Import-Module WebAdministration -ErrorAction Stop
    $pm = (Get-Item "IIS:\AppPools\$pool").processModel
    $typ = [string]$pm.identityType
    switch ($typ) {
        'ApplicationPoolIdentity' { return "IIS AppPool\$pool" }
        'SpecificUser'            { return [string]$pm.userName }
        'NetworkService'          { Write-Warnung "App-Pool $pool laeuft als NetworkService."; return 'NT AUTHORITY\NETWORK SERVICE' }
        'LocalSystem'             { Write-Warnung "App-Pool $pool laeuft als LocalSystem."; return 'NT AUTHORITY\SYSTEM' }
        'LocalService'            { Write-Warnung "App-Pool $pool laeuft als LocalService."; return 'NT AUTHORITY\LOCAL SERVICE' }
        default                   { throw "ABBRUCH: Identitaet des App-Pools $pool unbekannt ($typ)." }
    }
}

function Get-DienstIdentitaet([string]$startName) {
    if (-not $startName -or $startName -ieq 'LocalSystem') { Write-Warnung 'Dienst laeuft als LocalSystem.'; return 'NT AUTHORITY\SYSTEM' }
    if ($startName -like '.\*') { return "$env:COMPUTERNAME\$($startName.Substring(2))" }
    return $startName   # auch virtuelles Konto NT SERVICE\<Name> (DEV) ist gueltig
}

# Pruefliste "Ordner und Rechte" + Ordner-Eintraege des Manifests. Liefert $true, wenn kein Pflicht-Eintrag FEHLT.
# -Herstellen: fehlende Ordner anlegen und fehlende Rechte nach EINER gesammelten Bestaetigung setzen (vorherige ACL per icacls /save).
function Test-OrdnerUndRechte {
    param(
        [Parameter(Mandatory)][string]$WebPfad,
        [Parameter(Mandatory)][string]$DienstPfad,
        [Parameter(Mandatory)][string]$PoolIdentitaet,
        [Parameter(Mandatory)][string]$DienstIdentitaet,
        [string]$Umgebung = 'Production',
        [string]$DataProtectionPfad,
        [array]$ManifestEintraege = @(),
        [string]$LogPfad,
        [switch]$Herstellen
    )
    $ident = @{ Web = $PoolIdentitaet; Dienst = $DienstIdentitaet }
    $dpGesetzt = [bool]$DataProtectionPfad
    if (-not $dpGesetzt) { $DataProtectionPfad = Join-Path $env:ProgramData 'IdealAkeWms\DataProtection-Keys' }
    $liste = @(
        @{ Name = 'Web-Ordner'; Pfad = $WebPfad; Identitaet = 'Web'; Recht = 'LesenAusfuehren'; Pflicht = $true; Herstellen = $false }
    )
    foreach ($m in $ManifestEintraege | Where-Object { $_.Typ -eq 'Ordner' }) {
        $p = $m.Pfad.Replace('{Web}', $WebPfad).Replace('{Dienst}', $DienstPfad)
        $ra = if ($m.RechtAuf) { $m.RechtAuf.Replace('{Web}', $WebPfad).Replace('{Dienst}', $DienstPfad) } else { $p }
        $liste += @{ Name = $m.Id; Pfad = $p; RechtAuf = $ra; Identitaet = $m.Identitaet; Recht = $m.Recht; Pflicht = [bool]$m.Pflicht; Herstellen = [bool]$m.Herstellen }
    }
    $liste += @(
        @{ Name = 'Web-Logs (stdout)'; Pfad = (Join-Path $WebPfad 'logs'); Identitaet = 'Web'; Recht = 'Aendern'; Pflicht = $false; Herstellen = $true }
        @{ Name = 'DataProtection-Schluessel'; Pfad = $DataProtectionPfad; Identitaet = 'Web'; Recht = 'Aendern'; Pflicht = $dpGesetzt; Herstellen = $true }
        @{ Name = 'Branding'; Pfad = (Join-Path $WebPfad 'Branding'); Identitaet = 'Web'; Recht = 'Lesen'; Pflicht = $false; Herstellen = $false; Hinweis = 'Briefkopf faellt auf den Ersatz-Kopf zurueck' }
        @{ Name = 'Dienst-Ordner'; Pfad = $DienstPfad; Identitaet = 'Dienst'; Recht = 'LesenAusfuehren'; Pflicht = $true; Herstellen = $false }
        @{ Name = 'Dienst-Logs'; Pfad = (Join-Path $DienstPfad 'logs'); Identitaet = 'Dienst'; Recht = 'Aendern'; Pflicht = $true; Herstellen = $true }
    )

    $ergebnis = @()
    foreach ($e in $liste) {
        $wer = $ident[$e.Identitaet]
        $da = Test-Path -LiteralPath $e.Pfad
        $ok = $da -and (Test-Recht $e.Pfad $wer $e.Recht)
        $ergebnis += [pscustomobject]@{ Eintrag = $e; Wer = $wer; Da = $da; Status = $(if ($ok) { 'OK' } else { 'FEHLT' }) }
    }

    $zuTun = @($ergebnis | Where-Object { $_.Status -eq 'FEHLT' -and $_.Eintrag.Herstellen })
    if ($Herstellen -and $zuTun.Count -gt 0) {
        Write-Host '  Herzustellen (nur hinzufuegend):'
        foreach ($z in $zuTun) {
            $ra = if ($z.Eintrag.RechtAuf) { $z.Eintrag.RechtAuf } else { $z.Eintrag.Pfad }
            Write-Host ("    {0}{1}  ->  {2}:{3} auf {4}" -f $(if (-not $z.Da) { 'anlegen ' } else { '' }), $z.Eintrag.Pfad, $z.Wer, $script:RechtIcacls[$z.Eintrag.Recht], $ra)
        }
        if (Read-Ja 'Diese Ordner anlegen und Rechte setzen?') {
            foreach ($z in $zuTun) {
                $e = $z.Eintrag
                $ra = if ($e.RechtAuf) { $e.RechtAuf } else { $e.Pfad }
                if (-not $z.Da) { New-Item -ItemType Directory -Path $e.Pfad -Force | Out-Null }
                if (-not (Test-Recht $e.Pfad $z.Wer $e.Recht)) {
                    if ($LogPfad) {
                        $sicher = Join-Path $LogPfad ('acl_{0:yyyyMMdd_HHmmss}_{1}.txt' -f (Get-Date), ($e.Name -replace '[^A-Za-z0-9.-]', '_'))
                        & icacls $ra /save $sicher /Q | Out-Null
                        Write-Host "    vorherige ACL: $sicher"
                    }
                    $out = & icacls $ra /grant "$($z.Wer):(OI)(CI)$($script:RechtIcacls[$e.Recht])" 2>&1
                    if ($LASTEXITCODE -ne 0) { Write-Host "    icacls: $out" }
                }
                $neu = (Test-Path -LiteralPath $e.Pfad) -and (Test-Recht $e.Pfad $z.Wer $e.Recht)
                $z.Status = if (-not $neu) { 'FEHLT' } elseif (-not $z.Da) { 'angelegt' } else { 'gesetzt' }
            }
        }
    }

    $allesOk = $true
    Write-Host ('  {0,-10} {1,-28} {2,-16} {3}' -f 'Status', 'Eintrag', 'Recht', 'Pfad / Identitaet')
    foreach ($r in $ergebnis) {
        $st = $r.Status
        if ($st -eq 'FEHLT' -and -not $r.Eintrag.Pflicht) { $st = 'WARNUNG' }
        if ($st -eq 'FEHLT') { $allesOk = $false }
        $farbe = switch ($st) { 'FEHLT' { 'Red' } 'WARNUNG' { 'Yellow' } default { 'Green' } }
        Write-Host ('  {0,-10} {1,-28} {2,-16} {3}  ({4})' -f $st, $r.Eintrag.Name, $r.Eintrag.Recht, $r.Eintrag.Pfad, $r.Wer) -ForegroundColor $farbe
        if ($st -eq 'WARNUNG') { Write-Warnung "$($r.Eintrag.Name): $($r.Eintrag.Pfad) fehlt oder ohne Recht $($r.Eintrag.Recht) fuer $($r.Wer)$(if ($r.Eintrag.Hinweis) { " ($($r.Eintrag.Hinweis))" })." }
    }

    # Nur pruefen, nie herstellen
    $apj = Join-Path $WebPfad "appsettings.$Umgebung.json"
    if (Test-Path $apj) { Write-Host ('  {0,-10} {1,-28} {2,-16} {3}' -f 'OK', "appsettings.$Umgebung.json", 'vorhanden', $apj) -ForegroundColor Green }
    else { Write-Warnung "appsettings.$Umgebung.json fehlt im Web-Ordner (v1.71.0 SageLagerbuchung, Verbindungen)." }
    $edge = @("${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe", "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe") |
            Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($edge) { Write-Host ('  {0,-10} {1,-28} {2,-16} {3} (v{4})' -f 'OK', 'Microsoft Edge (PDF)', 'vorhanden', $edge, (Get-Item $edge).VersionInfo.ProductVersion) -ForegroundColor Green }
    else { Write-Warnung 'Microsoft Edge fehlt: PDF-Knopf der Druckdokumente bleibt ausgeblendet (v1.39.0).' }
    Write-Host "  Hinweis PDF-Temp: %TEMP%\IdealAkeWms-Pdf der Identitaet $PoolIdentitaet muss beschreibbar sein (App-Pool loadUserProfile = true). Pruefen: Druck > PDF im Browser; sonst icacls auf den TEMP-Ordner des Profils."
    return $allesOk
}

Export-ModuleMember -Function Write-Schritt, Write-Ok, Write-Warnung, Get-Warnungen, Reset-Warnungen, Assert-Administrator, Read-Eingabe, Read-Ja,
    Import-Ziele, Test-Recht, Get-PoolIdentitaet, Get-DienstIdentitaet, Test-OrdnerUndRechte
