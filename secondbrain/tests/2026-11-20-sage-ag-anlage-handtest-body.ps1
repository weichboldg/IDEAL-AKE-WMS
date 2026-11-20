# Handtest Sage-AG-Anlage mit vollstaendigem Fehler-Body – Claude Code 2026-11-20, Windows PowerShell 5.1
# Spec [[2026-11-20-bugfix-sage-ag-anlage-json-rumpf-spec]]. Freigabe Gerald: NUR FA 1065144 / Sage-AG 162, hoechstens 3 POSTs (A, B, C),
# nach dem ersten HTTP 2xx keine weiteren POSTs (Sperrdatei). Kennwort nur ueber Get-Credential, nie gespeichert/ausgegeben.
#
#   powershell -ExecutionPolicy Bypass -File <dieses Skript> -Variante A      # Enums als Bezeichnung
#   powershell -ExecutionPolicy Bypass -File <dieses Skript> -Variante B      # Enums als Zahl 4/2
#   powershell -ExecutionPolicy Bypass -File <dieses Skript> -Variante C -Rumpf <json-datei>
#   powershell -ExecutionPolicy Bypass -File <dieses Skript> -Variante Lesen  # nur GET: Positionen von FA 1065144
param(
    [Parameter(Mandatory)][ValidateSet('A', 'B', 'C', 'Lesen')][string]$Variante,
    [string]$Rumpf
)
$ErrorActionPreference = 'Stop'
$basis  = 'https://idealsage100tes.ideal.ideal-ake.at:5493/sdata/ol'
$post   = "$basis/CommonWawiServices/IDEAL;1/`$service/FertigungsauftragService"
$lesen  = "$basis/CommonWawiServicesResources/IDEAL;1/PpsFaBelegePositionenInfo?where=BelID%20eq%201065144&orderBy=BelPosID&count=200&format=json"
$sperre = Join-Path $PSScriptRoot '2026-11-20-sage-ag-anlage-handtest-ERFOLG.txt'
$log    = Join-Path $PSScriptRoot '2026-11-20-sage-ag-anlage-handtest-protokoll.txt'

$body = switch ($Variante) {
    'A' { '{"ServiceOption":"AddOrChangePositionArbeitsgang","PositionenArbeitsgang":[{"Handle":1065144,"PositionHandle":0,"RessourceTyp":"Arbeitsgang","RessourceNummer":"162","Menge":1,"Bearbeiter":"M00153"}]}' }
    'B' { '{"ServiceOption":4,"PositionenArbeitsgang":[{"Handle":1065144,"PositionHandle":0,"RessourceTyp":2,"RessourceNummer":"162","Menge":1,"Bearbeiter":"M00153"}]}' }
    'C' { if (-not $Rumpf) { throw 'Variante C braucht -Rumpf <json-datei>.' }; [IO.File]::ReadAllText((Resolve-Path $Rumpf)) }
    default { $null }
}
if ($body) {
    if (Test-Path $sperre) { throw "Gesperrt: ein POST war bereits erfolgreich ($sperre). Keine weiteren Schreibaufrufe." }
    if ($body -notmatch '1065144' -or $body -notmatch '"162"') { throw 'Rumpf betrifft nicht FA 1065144 / AG 162 - abgebrochen.' }
}

$cred = Get-Credential -UserName 'sage' -Message 'SData-Zugang Sage IDEAL (Prod) - wird nicht gespeichert'
$auth = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(('{0}:{1}' -f $cred.UserName, $cred.GetNetworkCredential().Password)))
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
[Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

function Senden([string]$methode, [string]$url, [string]$json) {
    $req = [Net.HttpWebRequest]::Create($url)
    $req.Method = $methode; $req.Timeout = 60000
    $req.Headers['Authorization'] = $auth
    if ($json) {
        $bytes = [Text.Encoding]::UTF8.GetBytes($json)
        $req.ContentType = 'application/json; charset=utf-8'   # wie StringContent(json, UTF8, "application/json") im Dienst
        $req.ContentLength = $bytes.Length
        $s = $req.GetRequestStream(); $s.Write($bytes, 0, $bytes.Length); $s.Close()
    }
    try { $resp = $req.GetResponse() } catch [Net.WebException] { $resp = $_.Exception.Response; if (-not $resp) { throw } }
    $text = (New-Object IO.StreamReader($resp.GetResponseStream(), [Text.Encoding]::UTF8)).ReadToEnd()
    $code = [int]$resp.StatusCode; $resp.Close()
    return @{ Code = $code; Text = $text }
}

try {
    $zeit = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    if ($body) {
        Write-Host "=== Variante $Variante  POST $post"; Write-Host "Rumpf: $body"
        $r = Senden 'POST' $post $body
        if ($r.Code -ge 200 -and $r.Code -lt 300) { Set-Content -Path $sperre -Value "$zeit Variante $Variante HTTP $($r.Code)" -Encoding UTF8 }
    } else {
        Write-Host "=== Lesen  GET $lesen"
        $r = Senden 'GET' $lesen $null
    }
    Write-Host "HTTP $($r.Code)"; Write-Host $r.Text
    Add-Content -Path $log -Encoding UTF8 -Value "`r`n=== $zeit Variante $Variante`r`nRumpf: $body`r`nHTTP $($r.Code)`r`n$($r.Text)"
} finally {
    [Net.ServicePointManager]::ServerCertificateValidationCallback = $null
}
