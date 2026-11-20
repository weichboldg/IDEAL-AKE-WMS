# Handtest Sage-AG-Anlage mit vollstaendigem Fehler-Body – Claude Code 2026-11-20, Windows PowerShell 5.1
# Spec [[2026-11-20-bugfix-sage-ag-anlage-json-rumpf-spec]]. Freigabe Gerald: NUR FA 1065144 / Sage-AG 162, hoechstens 3 POSTs (A, B, C),
# nach dem ersten HTTP 2xx keine weiteren POSTs (Sperrdatei). Kennwort nur ueber Get-Credential, nie gespeichert/ausgegeben.
#
#   powershell -ExecutionPolicy Bypass -File <dieses Skript> -Variante A      # Enums als Bezeichnung
#   powershell -ExecutionPolicy Bypass -File <dieses Skript> -Variante B      # Enums als Zahl 4/2
#   powershell -ExecutionPolicy Bypass -File <dieses Skript> -Variante C -Rumpf <json-datei>
#   powershell -ExecutionPolicy Bypass -File <dieses Skript> -Variante Lesen  # nur GET: Positionen von FA 1065144
#   AKE-Test (Freigabe Gerald 2026-11-20, nur FA 835):  ... -System AKE -Variante Lesen   bzw.   -System AKE -Variante A -Ag <Nr>
param(
    [Parameter(Mandatory)][ValidateSet('A', 'B', 'C', 'Lesen')][string]$Variante,
    [string]$Rumpf,
    [ValidateSet('IDEAL', 'AKE')][string]$System = 'IDEAL',
    [string]$Ag = '162'
)
$ErrorActionPreference = 'Stop'
if ($System -eq 'AKE') { $basis = 'https://sagetest01.ake.at:5493/sdata/ol'; $ds = 'ake_TEST2026;1'; $fa = 835; $bearb = '' }
else { $basis = 'https://idealsage100tes.ideal.ideal-ake.at:5493/sdata/ol'; $ds = 'IDEAL;1'; $fa = 1065144; $bearb = 'M00153'; $Ag = '162' }
$post   = "$basis/CommonWawiServices/$ds/`$service/FertigungsauftragService"
$lesen  = "$basis/CommonWawiServicesResources/$ds/PpsFaBelegePositionenInfo?where=BelID%20eq%20$fa&orderBy=BelPosID&count=200&format=json"
$sperre = Join-Path $PSScriptRoot ("2026-11-20-sage-ag-anlage-handtest-ERFOLG" + $(if ($System -eq 'AKE') { '-AKE' } else { '' }) + '.txt')
$log    = Join-Path $PSScriptRoot '2026-11-20-sage-ag-anlage-handtest-protokoll.txt'

$body = switch ($Variante) {
    'A' { '{"ServiceOption":"AddOrChangePositionArbeitsgang","PositionenArbeitsgang":[{"Handle":' + $fa + ',"PositionHandle":0,"RessourceTyp":"Arbeitsgang","RessourceNummer":"' + $Ag + '","Menge":1,"Bearbeiter":"' + $bearb + '"}]}' }
    'B' { '{"ServiceOption":4,"PositionenArbeitsgang":[{"Handle":' + $fa + ',"PositionHandle":0,"RessourceTyp":2,"RessourceNummer":"' + $Ag + '","Menge":1,"Bearbeiter":"' + $bearb + '"}]}' }
    'C' { if (-not $Rumpf) { throw 'Variante C braucht -Rumpf <json-datei>.' }; [IO.File]::ReadAllText((Resolve-Path $Rumpf)) }
    default { $null }
}
if ($body) {
    if (Test-Path $sperre) { throw "Gesperrt: ein POST war bereits erfolgreich ($sperre). Keine weiteren Schreibaufrufe." }
    if ($body -notmatch "`"Handle`":$fa," -or $body -notmatch "`"$Ag`"") { throw "Rumpf betrifft nicht FA $fa / AG $Ag - abgebrochen." }
}

$cred = Get-Credential -UserName 'sage' -Message "SData-Zugang Sage $System ($ds) - wird nicht gespeichert"
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
    Add-Content -Path $log -Encoding UTF8 -Value "`r`n=== $zeit $System Variante $Variante`r`nRumpf: $body`r`nHTTP $($r.Code)`r`n$($r.Text)"
} finally {
    [Net.ServicePointManager]::ServerCertificateValidationCallback = $null
}
