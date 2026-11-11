---
type: spec
title: Update-Skript je Zielsystem mit Nachlauf-Manifest (Bündel-Deploy v1.95.0)
slug: 2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec
status: Entwurf
created: 2026-11-09
updated: 2026-11-09
source_backlog: "[[2026-11-09-update-skript-je-zielsystem-mit-nachlauf]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - "scripts/deploy/Update-IdealAkeWms.ps1 (neu)"
  - "scripts/deploy/nachlauf.psd1 (neu)"
  - "IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs (neu, Muster OperationConfigsMigrationSqlTests)"
  - "README.md (Abschnitte 'Update bestehender Installation' Z. 66-157 und 'Update (neue Version einspielen)' Z. 796-812, Bündel-Stand)"
  - "docs/TESTSZENARIEN.md (neues Kapitel TS-124, Nummer prüft der Dev-Lauf)"
  - ".claude/agents/qa-agent.md (Punkt 7: Format publish-info.txt, SQL\\ und deploy\\ ins Paket)"
  - ".claude/agents/spec-agent.md (DEPLOY SECTION: Pflicht Manifest-Einträge)"
  - "secondbrain/_templates/spec.md (Deploy-Abschnitt: Manifest-Einträge)"
  - "CLAUDE.md (nur Vorschlag für die Checkliste, Änderung durch Gerald)"
open_questions:
  - "1. Marker-Tabelle für gelaufene Einmal-Skripte (Nachtrag zu ADR 0015): ja oder nein? Für/Wider im Abschnitt Offene Rückfragen."
  - "2. web.config am Ziel: nicht überschreiben und Unterschied melden (Empfehlung) oder aus dem Paket überschreiben?"
  - "3. Haltepunkt Altbestand-Einmal-Skript (v1.88.0): nur 'ausgeführt' oder 'Abbruch, Web bleibt gestoppt' (Empfehlung) oder zusätzlich 'überspringen'?"
  - "4. Stand der Einmal-Skripte je System (nur das Deploy-Protokoll weiß es): Hotfix-1.30.1 an AKE-Prod/AKE-Test/IDEAL-Test, v1.47.4 und 00er-Bereinigung an IDEAL-Test, v1.46.0-Rücksetzen an IDEAL-Test; IDEAL-Prod: entfallen Hotfix-1.30.1 und v1.47.4 wegen Neuinstallation?"
  - "5. Hotfix v1.30.4 (Fotos nach App_Data) liegt nur in main, nicht im Bündel: vor dem Bündel-Deploy ins Bündel holen (Empfehlung)?"
  - "6. Rolle 'beschichtungsauftrag' (Lesen, v1.31.0, kein Seed) an AKE ebenfalls anlegen? Backlog nennt nur 'beschichtungsauftrag_schreiben'."
  - "7. Zielsysteme bestätigen: AKE-Prod auf akenet01 mit C:\\AKE\\... (Annahme Backlog Z. 93), Test-Systeme mit eigener DB, sqlcmd und SQL-Rechte des ausführenden Kontos auf beiden Servern."
  - "8. -PublishWeb/-PublishDienst als Quellpfade der Publish-Ordner gedeutet. Oder waren Schalter 'nur Web'/'nur Dienst' gemeint?"
  - "9. Negativfälle, die einen echten Start brauchen (Version passt nach Start nicht): Gerald fährt sie einmal von Hand auf DEV-IDEAL, der Agent nur -NurPruefen. Einverstanden?"
  - "10. Änderungen an .claude/agents/*.md und secondbrain/_templates/spec.md direkt im Hauptcheckout (wie qa-agent Punkt 7 am 09.11.), CLAUDE.md nur als Vorschlag: einverstanden?"
epic: false
etappen: []
deploy:
  web: false
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Der Bündel-Deploy v1.95.0 muss auf vier Systeme (AKE-Prod, AKE-Test, IDEAL-Prod, IDEAL-Test). Jedes System steht auf einem anderen
Stand (Backlog-Tabelle „Was beim Bündel-Deploy nachzuführen ist“), und zwischen v1.31.0 und v1.95.0 liegen 25 Schema-Skripte, 2
Datenskripte, 4 Einmal-Skripte und rund 30 Hand-Schritte, die heute nur verstreut in über 60 Changelogs stehen. Die README-Update-Tabelle
endet bei SQL/80 (plus 95 und 97, `README.md` des Bündels Z. 152-154), die Dienst-Anleitung kennt nur `C:\Services\IDEALAKEWMSService`
(Z. 796-812). Ein Skript, das je Zielsystem selbst ermittelt, was fehlt, und danach eine gefilterte Nachlauf-Liste ausgibt, ersetzt das
fehleranfällige Zusammensuchen. Es führt nichts Einmaliges selbst aus (ADR 0015), sondern macht die Hand-Schritte vollständig und in
richtiger Reihenfolge sichtbar.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. `scripts\deploy\Update-IdealAkeWms.ps1` – Update je Zielsystem, lokal auf dem Zielserver, Windows PowerShell 5.1, als Administrator.
2. `scripts\deploy\nachlauf.psd1` – Manifest aller Skripte und Schritte v1.31.0–v1.95.0 (Tabelle unten), gelesen per
   `Import-PowerShellDataFile` (in PS 5.1 vorhanden).
3. Drift-Test `NachlaufManifestDriftTests` – macht die Pflicht „Manifest nachtragen“ maschinell prüfbar.
4. Paket: `qa-agent.md` Punkt 7 legt zusätzlich `SQL\` und `scripts\deploy\` in `<worktree>\publish\` und schreibt `publish-info.txt` in
   festem Format (siehe Lösungsentwurf). Damit ist `publish\` das vollständige Deploy-Paket, das der Mensch auf den Server kopiert.
5. README: beide Update-Abschnitte durch Verweis auf Skript und Manifest ersetzen.
6. Pflicht „Manifest nachtragen“ in `spec-agent.md` und der Spec-Vorlage, Vorschlag für die CLAUDE.md-Checkliste.
7. ADR 0022 „Update je Zielsystem per Skript mit Nachlauf-Manifest“ (Brain, Hauptcheckout), `fallstricke.md`-Eintrag (siehe Abschnitt
   Brain unten).

**Out-of-Scope**
- Publish nach jedem Build: steht bereits in `.claude/agents/qa-agent.md:90-97` (Auftrag Gerald 2026-11-09). Hier nur das Format von
  `publish-info.txt` und der Paketinhalt (Punkt 4 oben).
- Zieldatei `deploy/ziele.psd1` aus dem Backlog-Vorschlag: entfällt. Die Pfade folgen einem festen Muster, App-Pool, Site, Dienst und
  DB ermittelt das Skript selbst (Backlog „Zielsysteme“). Hinzufügen, wenn ein System vom Muster abweicht.
- Neuinstallation (`Install-IdealAkeWms-IDEAL.ps1`, nicht im Repo, [[2026-11-04-ideal-produktiv-installation]]).
- Skripte vor SQL/89: alle vier Systeme stehen mindestens auf SQL/88 (Backlog-Tabelle). Das Manifest beginnt bei v1.31.0.
- Abnahme-Prüfschritte („vor der Abnahme“: TS-80.1, TS-81.1, TS-82.1, TS-83.1/83.2, P-K1, Server-Kultur v1.53.0). Sie gehören zur
  Abnahme, nicht zum Deploy, und kommen nicht ins Manifest.
- Marker-Tabelle für Einmal-Skripte: Entscheidung Gerald (Rückfrage 1); wenn ja, eigene Folge-Spec.
- Jeder Lauf gegen akenet01, idealweb01 oder AKESQL20 durch einen Agenten.

## Fachliche Anforderungen

### Grenzen (hart)
- Das Skript führt den Deploy nur aus, wenn ein Mensch es **auf dem Zielserver** startet. Agenten starten es nur auf DEV und nur mit
  `-NurPruefen`.
- Kein Aufruf an Sage, weder lesend noch schreibend. Das Skript liest nur `ConnectionStrings:DefaultConnection` und verbindet sich nur mit
  der WMS-DB.
- `SQL/108`, `SQL/110` und alle `SQL/Einmalig/*` laufen **nie scharf** durch das Skript. Höchstens Trockenlauf (nur bei Skripten mit
  Trockenlauf-Schalter, Literal-Prüfung siehe AK 17), sonst nur Anzeige mit fertigem Aufruf in der Nachlauf-Liste.
- Vor dem ersten DB-Kontakt und vor jedem schreibenden SQL eine ausdrückliche Bestätigung.

### Bedienung
- Ohne Parameter fragt das Skript **Firma** (`AKE` | `IDEAL`) und **System** (`Prod` | `Test` | `beide`). `-Firma` und `-System`
  überspringen die jeweilige Abfrage.
- Bei `beide`: zuerst Test, vollständig mit Nachlauf-Liste. Schlägt Test fehl, wird Prod nicht angefasst. Vor Prod muss der Mensch
  `<FIRMA>-PROD` eintippen (z. B. `AKE-PROD`); jede andere Eingabe beendet das Skript.

### Zielsysteme (Muster, abgeglichen mit Backlog „Zielsysteme“)

| Firma / System | Dienst-Ordner | Web-Ordner (IIS) | Dienst (Anzeigename, nur Info) |
|---|---|---|---|
| IDEAL Prod | `C:\IDEAL\IdealAkeWmsService` | `C:\inetpub\wwwroot\IDEALAKEWMS` | `IDEAL AKE WMS Service` |
| IDEAL Test | `C:\IDEAL\IdealAkeWmsService_Test` | `C:\inetpub\wwwroot\IDEALAKEWMS_Test` | `IDEAL AKE WMS Service TEST` |
| AKE Prod | `C:\AKE\IdealAkeWmsService` | `C:\inetpub\wwwroot\IDEALAKEWMS` | `IDEAL AKE WMS Service` |
| AKE Test | `C:\AKE\IdealAkeWmsService_Test` | `C:\inetpub\wwwroot\IDEALAKEWMS_Test` | `IDEAL AKE WMS Service TEST` |

Muster: Dienst `C:\<FIRMA>\IdealAkeWmsService[_Test]`, Web `C:\inetpub\wwwroot\IDEALAKEWMS[_Test]`. Das deckt alle vier Zeilen der
Backlog-Tabelle ohne Abweichung. **Abweichungen an anderer Stelle:** README nennt `C:\Services\IDEALAKEWMSService` und Dienstname
`IDEALAKEWMSService` (Bündel-README Z. 751, 800-809); `IDEALAKEWMSService/Program.cs:23` setzt `ServiceName = "IDEAL AKE WMS Service"`
für beide Dienste. Das Skript verlässt sich deshalb auf keinen Namen, nur auf den Exe-Pfad. Belegt ist nur idealweb01 (Screenshots); AKE
„gleich aufgebaut“ und AKE-Prod auf akenet01 sind Annahmen (Backlog Z. 93) → Rückfrage 7. Das Skript bricht ab, wenn ein Ordner fehlt.

`-WebPfad`/`-DienstPfad`/`-LogPfad` überschreiben das Muster (DEV-Test auf `D:\Dev\...`). Mit `-System beide` sind Pfadparameter
unzulässig (Abbruch), weil sie nicht beiden Systemen gelten können.

## Technischer Lösungsentwurf

Vorbild ist `D:\Dev\scripts\dev-deploy.ps1` (nur DEV, außerhalb des Repos): Stop-All mit `WaitForStatus` und 2 s Pause für w3wp
(Z. 23-28), Abbruch mit Rückbau bei HTTP-Fehler (Z. 56), Fußzeilen-Regex `IDEAL-AKE WMS v([0-9.]+)` per `HttpClient` mit
`UseDefaultCredentials` (Z. 48-58, in PS 5.1 lauffähig). Neu gegenüber dem Vorbild: kein `dotnet publish` (das Paket kommt fertig),
kein Patchen der `web.config`, SQL-Skripte statt `Migrate()`, Ermittlung statt fester Namen.

### Paket und Parameter

```
powershell -ExecutionPolicy Bypass -File <Paket>\deploy\Update-IdealAkeWms.ps1
    [-Firma AKE|IDEAL] [-System Prod|Test|beide]
    [-Quelle <Paket>]                         # Default: $PSScriptRoot\..
    [-PublishWeb <Pfad>] [-PublishDienst <Pfad>]  # Default: <Quelle>\IDEALAKEWMSWeb bzw. \IDEALAKEWMSWebService (Rückfrage 8)
    [-SqlOrdner <Pfad>]                       # Default: <Quelle>\SQL
    [-WebPfad <Pfad>] [-DienstPfad <Pfad>]    # nur bei einem System
    [-LogPfad <Pfad>]                         # Default: C:\<FIRMA>\deploy-logs
    [-SiteUrl <URL>] [-BackupPfad <Pfad auf dem SQL-Server>]
    [-NurPruefen]
```

Paket = `<worktree>\publish\` mit `IDEALAKEWMSWeb\`, `IDEALAKEWMSWebService\`, `SQL\` (Kopie von `SQL\`), `deploy\` (Kopie von
`scripts\deploy\`) und `publish-info.txt`. Format (neu, festgelegt hier; `qa-agent.md:95-96` nennt nur die Inhalte):

```
Version=1.95.0
Commit=<HEAD, kurz>
Branch=feature/2026-08-07-ideal-teile-1-5
Spec=<Slug>
Zeit=yyyy-MM-dd HH:mm
```

Das Skript liest `publish-info.txt` aus dem Elternordner von `-PublishWeb` und von `-PublishDienst`. Fehlt eine der Dateien oder
unterscheiden sich `Version`/`Commit` → Abbruch (Backlog „Publish nach jedem Build“).

**Dateien nur ASCII** (Umlaute als ae/oe/ue, wie `dev-deploy.ps1`): PS 5.1 liest eine `.ps1`/`.psd1` ohne BOM als ANSI, Umlaute würden
zerstört; das Datei-Werkzeug der Agenten löst zudem `\uXXXX` auf ([[fallstricke]] §23).

### Ermittlung (vor jeder Änderung)
1. **Ordner:** Web-Ordner mit `IdealAkeWms.dll`, Dienst-Ordner mit `IDEALAKEWMSService.exe`, sonst Abbruch.
2. **Site/App-Pool:** Modul `WebAdministration`; alle Sites (`Get-Website`) und Anwendungen (`Get-WebApplication`), deren
   `physicalPath` (Umgebungsvariablen expandiert, Groß/Klein egal, ohne abschließenden `\`) gleich dem Web-Ordner ist. Genau ein Treffer,
   sonst Abbruch mit Trefferliste. Nutzen weitere Anwendungen denselben App-Pool → Warnung mit Liste und Bestätigung.
3. **Dienst:** `Get-CimInstance Win32_Service`, `PathName` (Anführungszeichen entfernt) gleich `<Dienst-Ordner>\IDEALAKEWMSService.exe`.
   Genau ein Treffer, sonst Abbruch.
4. **DB Web:** Umgebung aus `ASPNETCORE_ENVIRONMENT` in der Ziel-`web.config` (Repo-Stand `Production`, `IdealAkeWms/web.config:18`;
   DEV patcht auf `DEV`, `dev-deploy.ps1:40-43`), dann `appsettings.json` überlagert von `appsettings.<Umgebung>.json`
   (`ConvertFrom-Json`; scheitert das Parsen, z. B. an Kommentaren → Abbruch mit Dateiname).
5. **DB Dienst:** Umgebung aus Registry `HKLM:\SYSTEM\CurrentControlSet\Services\<Dienstname>` Wert `Environment`
   (`DOTNET_ENVIRONMENT=...`), sonst Maschinen-Variable `DOTNET_ENVIRONMENT`, sonst `Production` (Dienst nutzt
   `Host.CreateApplicationBuilder`, `IDEALAKEWMSService/Program.cs:19`); dann wie 4 im Dienst-Ordner.
6. Connection-String-Zerlegung mit `System.Data.SqlClient.SqlConnectionStringBuilder` (.NET Framework, in PS 5.1 vorhanden): Server,
   Datenbank, Windows-Anmeldung (`-E`) oder SQL-Login (`-U`, Passwort über `$env:SQLCMDPASSWORD`, nie auf der Kommandozeile),
   `TrustServerCertificate` → `-C`.
7. **Abbruch**, wenn Web-DB ≠ Dienst-DB (Server+Datenbank), oder bei `beide`, wenn Test und Prod dieselbe Server+Datenbank zeigen.
8. **sqlcmd:** `Get-Command sqlcmd`, sonst der Pfad aus `dev-deploy.ps1:21`; fehlt beides → Abbruch.
9. **Site-URL:** aus der ersten Bindung (https vor http; Host-Header, sonst `localhost`), `-SiteUrl` überschreibt. TLS 1.2 einschalten
   (`[Net.ServicePointManager]::SecurityProtocol`).
10. **Anzeige** aller Werte (Paket-Version/Commit/Zweig, Ordner, Site, App-Pool, Dienstname, Umgebung, Server/DB Web und Dienst, URL),
    dann Bestätigung „Verbindung zu `<Server>/<DB>` herstellen (nur lesend)?“. Erst danach der erste DB-Kontakt.

### Ablauf je System

| # | Schritt | Inhalt |
|---|---|---|
| 1 | Vorprüfung (lesend) | Installierte Version: Fußzeile der laufenden Site (nur nach Windows-Anmeldung sichtbar, `_Layout.cshtml:447`); sonst `deploy-info.txt` im Web-Ordner (Schritt 5 legt sie ab); sonst Eingabe, leer = unbekannt (dann alle Einträge). `SELECT MigrationId FROM dbo.__EFMigrationsHistory` → fehlende Schema-Einträge des Manifests in Manifest-Reihenfolge. RCSI (`is_read_committed_snapshot_on`) anzeigen, bei IDEAL `0` = Warnung (v1.31.0, Z. 124-125). Spur-Abfragen der Einmal-Skripte. Phase-„Vorher“-Einträge anzeigen. Mit `-NurPruefen`: Plan + Nachlauf-Liste ausgeben, Protokoll schreiben, Ende (Exit 0). |
| 2 | Backup | Hinweis (Pflicht bei 90, 95, 109/110, 133/Einmal, ADR 0004). Angebot `BACKUP DATABASE [<DB>] TO DISK = N'<BackupPfad>\<DB>_<Zeit>_vor_v<Version>.bak' WITH COPY_ONLY, CHECKSUM` nach Bestätigung; Pfad liegt auf dem **SQL-Server** (bei AKE ist das AKESQL20, nicht der Web-Server), Default `SERVERPROPERTY('InstanceDefaultBackupPath')`. Fehler → Abbruch. Ablehnen = Mensch bestätigt „Backup liegt vor“. |
| 3 | Stoppen | Dienst `Stop-Service` + `WaitForStatus('Stopped','00:01:00')`, App-Pool `Stop-WebAppPool` + Warten auf `Stopped`, 2 s Pause (w3wp gibt DLLs frei, `dev-deploy.ps1:27`). Timeout → Abbruch. |
| 4 | Schema | Fehlende Schema-Einträge in Manifest-Reihenfolge, **nie** nach Dateisortierung (dreistellige Namen sortieren vor `11_`, ADR 0004 Nachtrag G17). Je Skript Bestätigung, dann `sqlcmd -S -d (-E\|-U) [-C] -b -i <Datei>`, Ausgabe ins Protokoll. Exit ≠ 0 → Abbruch, Web und Dienst bleiben gestoppt. Danach MigrationId in der History prüfen. Sonderregeln 90 und 95 siehe unten. **Fehlt am Ende eine MigrationId** (abgelehnt oder Fehler), endet das Skript hier: Kopieren und Start würden `db.Database.Migrate()` auslösen (`IdealAkeWms/Program.cs:202`), und EF spielte die Migration ohne die Vorbedingungen ein ([[fallstricke]] §8 „Lokaler `dotnet run` …“). |
| 4a | Haltepunkt | Einträge mit `Phase = Haltepunkt` (heute nur Altbestand v1.88.0): Trockenlauf anbieten (AK 17), Ausgabe ins Protokoll, dann Anzeige des scharfen Aufrufs. Weiter nur mit „ausgeführt“, sonst Abbruch bei gestopptem Web (Rückfrage 3). |
| 5 | Kopieren | `robocopy <Paket> <Ziel> /E /R:2 /W:5` **ohne** `/MIR`/`/PURGE`; ausgeschlossen: Dateien `appsettings*.json`, `web.config` (Rückfrage 2), Ordner `Branding`, `App_Data`, `wwwroot\Fotos`, `logs`. robocopy-Exit ≥ 8 → Abbruch. Hash (`Get-FileHash`) aller `appsettings*.json` vor/nach gleich, sonst Abbruch. `deploy-info.txt` (Kopie von `publish-info.txt`) in Web- und Dienst-Ordner. Hat das Ziel `App_Data\Fotos` → Warnung (Rückfrage 5). |
| 6 | Starten | App-Pool starten, Startseite abrufen (`HttpClient`, Timeout 5 min wie Vorbild). `IdealAkeWms.dll` im Ziel hash-gleich mit dem Paket; Fußzeile (falls lesbar) = Paket-Version; Fußzeile nicht lesbar → Mensch bestätigt die Version im Browser. HTTP-Fehler oder Abweichung → App-Pool stoppen, Dienst **nicht** starten, Exit ≠ 0 (wie `dev-deploy.ps1:56`). Dann Dienst starten, `IDEALAKEWMSService.dll` hash-gleich, nach 30 s `Running`, sonst Abbruch mit Hinweis auf `logs\service-*.log`. Reihenfolge Web vor Dienst: der Web-Start legt die neuen Setting-Keys an (v1.60.0 Z. 27). |
| 7 | Nachlauf | Gefilterte Liste (unten) mit eingesetzten Werten ausgeben; Trockenlauf-Einträge der Phase „Nachher“ (heute SQL/110) nach Bestätigung im Trockenlauf ausführen. Protokoll `<LogPfad>\yyyy-MM-dd_HHmm_<FIRMA>-<SYSTEM>_v<Version>.log` per `Start-Transcript` von Anfang an, sqlcmd-Ausgaben angehängt; wird auch bei Abbruch geschrieben (`try/finally`). |

### Sonderregeln (belegt)
- **SQL/90** (daten-konvertierend, Kern-Tabelle `ProductionOrders`): vorher lesend
  `SELECT j.name, j.enabled, s.step_name FROM msdb.dbo.sysjobs j JOIN msdb.dbo.sysjobsteps s ON s.job_id = j.job_id WHERE s.command LIKE N'%ProductionOrders%'`
  anzeigen (fehlt das Leserecht auf `msdb` → Hinweis „von Hand prüfen“), Dienst gestoppt (Schritt 3), Extra-Bestätigung „Backup liegt
  vor, kein Agent-Job schreibt ProductionOrders“. Belege: `SQL/90:9-12`, v1.32.0 Z. 68-70.
- **SQL/95** (Umbenennung `OseonOperationConfigs` → `OperationConfigs`): nur wenn `20260330142035_AddOseonOperationConfig` in der
  History steht (P1/R10, v1.54.0 Z. 32), sonst Abbruch. App-Pool ist gestoppt (Schritt 3; alte Binaries fragen die alte Tabelle ab,
  `SQL/95:8-9`). Die Datei läuft vollständig mit `-b`; steht `ABBRUCH` in der Ausgabe oder ist der Exit ≠ 0 → Abbruch (`SQL/95:12-27`,
  v1.54.0 Z. 30-31).
- **SQL/96** braucht 95 (v1.61.0 Z. 29); **SQL/110** braucht 109 (`SQL/110:12, 19-23`); **Altbestand** braucht 133 und 141
  (Skriptkopf Z. 24, 37-46). Die Manifest-Reihenfolge erfüllt das; das Skript prüft `Voraussetzung` trotzdem vor dem Lauf.

### Manifest `scripts\deploy\nachlauf.psd1`

Pfad festgelegt: `scripts\deploy\nachlauf.psd1` neben dem Skript (im Paket `deploy\nachlauf.psd1`). Aufbau:

```powershell
@{ Eintraege = @(
  @{ Id='SQL/90'; Version='1.32.0'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
     Datei='90_InvertProductionOrderHierarchy.sql'; MigrationId='20260814105526_InvertProductionOrderHierarchy'
     Voraussetzung='Backup; sysjobs; Dienst gestoppt'; Text='...'; Quelle='2026-08-17-v1-32-0-ideal-teil-7' }
  # ...
)}
```

| Feld | Werte | Bedeutung |
|---|---|---|
| `Id` | `SQL/<Nr>`, `Einmalig/<Datum>`, `v<Version>-<Kurz>` | eindeutig |
| `Version` | `x.y.z` | eingeführt mit; Nicht-Einmalig-Einträge erscheinen nur, wenn `Version` > installierte Version (unbekannt = alle) |
| `Typ` | `Schema` \| `Daten` \| `Einmalig` \| `Einstellung` \| `Leitstand` \| `Info` | wie Backlog |
| `Firma` | `AKE` \| `IDEAL` \| `beide` | Filter |
| `Phase` | `Vorher` \| `Schema` \| `Haltepunkt` \| `Nachher` | wann das Skript den Eintrag zeigt oder ausführt |
| `Ausfuehrung` | `automatisch` (nur `Schema`) \| `Trockenlauf` \| `anzeigen` | was das Skript selbst tun darf |
| `Datei`, `MigrationId` | | bei SQL; `MigrationId` nur bei `Schema` |
| `Voraussetzung` | Text | wird angezeigt, bei SQL zusätzlich geprüft |
| `Spur` | optional, lesendes SQL, liefert >0 = „lief schon“ | nur bei Einmal-Skripten mit Spur |
| `Text` | Anweisung | mit Platzhaltern `{Server}` `{DB}` `{SqlOrdner}` `{LogPfad}` für den fertigen Aufruf |
| `Quelle` | Changelog-Dateiname | Beleg |

`Einmalig`-Einträge werden **nie** nach Version gefiltert (die Version sagt nichts darüber, ob das Skript lief), sondern immer mit
Spur-Ergebnis bzw. „unbekannt, Deploy-Protokoll prüfen“ gezeigt.

**Schema-Einträge** (alle `Firma = beide`, `Phase = Schema`, `Ausfuehrung = automatisch`; Reihenfolge = Deploy-Liste Backlog, ADR 0004
Nachtrag). „Nur AKE“ im Backlog heißt: an IDEAL stehen 89–97 schon in der History (IDEAL-Prod: FreshInstall mit 75 Migrationen bis
`20261019092105_…`, [[2026-11-04-ideal-produktiv-installation]] Z. 18). Die History-Prüfung erledigt das, ein Firma-Filter wäre falsch.
MigrationIds belegt aus dem History-Insert jeder Datei und 1:1 abgeglichen mit den 25 `[Migration("…")]` nach
`20260806120650_AllowMultipleDummyRequisitionItems` in `IdealAkeWms/Migrations/*.Designer.cs`.

| # | Datei | MigrationId | Version | Voraussetzung / Hinweis | Quelle |
|---|---|---|---|---|---|
| 1 | `89_AddFaHierarchy.sql` | `20260807105825_AddFaHierarchy` | 1.31.0 | additiv | 2026-08-10-v1-31-0-ideal-teile-1-5 |
| 2 | `90_InvertProductionOrderHierarchy.sql` | `20260814105526_InvertProductionOrderHierarchy` | 1.32.0 | Sonderregel 90 | 2026-08-17-v1-32-0-ideal-teil-7 |
| 3 | `91_AddArticleMatchcode.sql` | `20260915094646_AddArticleMatchcode` | 1.40.0 | ersetzt die nie deployte `ProductionOrder.Matchcode` aus v1.38.0 | 2026-09-15-v1-40-0-matchcode-artikelstamm |
| 4 | `92_AddUserDefaultFilterBomKommissionierziel.sql` | `20260921131329_AddUserDefaultFilterBomKommissionierziel` | 1.42.0 | additiv | 2026-09-21-v1-42-0-stueckliste-kommissionierziel-filter |
| 5 | `93_AddProductionWorkplaceSageFields.sql` | `20260922083259_AddProductionWorkplaceSageFields` | 1.44.0 | additiv | 2026-09-22-v1-44-0-ideal-bde-arbeitsgaenge-werkbank-anlage |
| 6 | `94_AddBeschichtungsauftrag.sql` | `20261012073551_AddBeschichtungsauftrag` | 1.50.0 | additiv | 2026-10-12-v1-50-0-beschichtungsauftrag-objekt |
| 7 | `95_RenameOperationConfigsAndAddAudit.sql` | `20261012140548_RenameOperationConfigsAndAddAudit` | 1.54.0 | Sonderregel 95 | 2026-10-12-v1-54-0-arbeitsgang-stammdaten |
| 8 | `96_AddOperationConfigSourceAndSageMissingSince.sql` | `20261016090641_AddOperationConfigSourceAndSageMissingSince` | 1.61.0 | braucht 95 | 2026-10-16-v1-61-0-arbeitsgang-abgleich-sage-view |
| 9 | `97_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung.sql` | `20261019092105_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung` | 1.63.0 | additiv | 2026-10-19-v1-63-0-bde-stufe-1-werkbaenke-ag-fertigmeldung |
| 10 | `98_AddStorageAreasAndStorageLocationSageFields.sql` | `20261106181707_AddStorageAreasAndStorageLocationSageFields` | 1.73.0 | 98–107 additiv | 2026-11-06-v1-73-0-sage-sdata-stammdaten-sync |
| 11 | `99_AddArticleSageDetails.sql` | `20261106184123_AddArticleSageDetails` | 1.73.0 | | dto. |
| 12 | `100_AddArticleAttributeValueTextValue.sql` | `20261106185149_AddArticleAttributeValueTextValue` | 1.73.0 | | dto. |
| 13 | `101_AddProductionOrderPositions.sql` | `20261106191438_AddProductionOrderPositions` | 1.73.0 | | dto. |
| 14 | `102_AddProductionWorkplaceSdataFields.sql` | `20261106192342_AddProductionWorkplaceSdataFields` | 1.73.0 | | dto. |
| 15 | `103_AddSagePpsMitarbeiterAndStillstandsgruende.sql` | `20261106193321_AddSagePpsMitarbeiterAndStillstandsgruende` | 1.73.0 | | dto. |
| 16 | `104_AddCreditors.sql` | `20261106195956_AddCreditors` | 1.73.0 | | dto. |
| 17 | `105_AddDebitors.sql` | `20261106200713_AddDebitors` | 1.73.0 | | dto. |
| 18 | `106_AddArticleSuppliers.sql` | `20261106201439_AddArticleSuppliers` | 1.73.0 | | dto. |
| 19 | `107_AddSageStammdatenOutboxAndUebergabeStatus.sql` | `20261106202615_AddSageStammdatenOutboxAndUebergabeStatus` | 1.73.0 | | dto. |
| 20 | `109_AddStockMovementProductionOrderId.sql` | `20261107190419_AddStockMovementProductionOrderId` | 1.87.0 | Backup | 2026-11-08-v1-87-0-lagerbuchungen-fa-feld |
| 21 | `112_AddSagePurchaseOrderLines.sql` | `20261107172542_AddSagePurchaseOrderLines` | 1.84.0 | | 2026-11-08-v1-84-0-wareneingang-aus-sage-bestellungen |
| 22 | `113_AddPurchaseOrderReceipts.sql` | `20261107173341_AddPurchaseOrderReceipts` | 1.84.0 | | dto. |
| 23 | `133_AddPickingAreasAndOrders.sql` | `20261107193219_AddPickingAreasAndOrders` | 1.88.0 | additiv, Seed; Backup | 2026-11-08-v1-88-0-kommissionierauftraege |
| 24 | `140_AddArtikelMengeneinheiten.sql` | `20261107182445_AddArtikelMengeneinheiten` | 1.85.0 | | 2026-11-08-v1-85-0-artikel-lieferanten-nachbesserung |
| 25 | `141_AddPickingOrderAltbestandPruefen.sql` | `20261108071811_AddPickingOrderAltbestandPruefen` | 1.89.0 | vor dem Altbestand-Skript | 2026-11-08-v1-89-0-kommissionierauftraege-nachbesserung |

Nummernlücken 108 (Daten), 110 (Daten), 111, 114–132, 134–139 sind gewollt (feste SQL-Blöcke mit Lücken, Freigabe Ü2,
[[2026-11-08-buendel-uebersicht]] Z. 47-49, 93). Die MigrationIds folgen nicht der Deploy-Reihenfolge (109 ist jünger als 112/113/140);
für die idempotenten Skripte ohne Abhängigkeit untereinander ist das ohne Wirkung, die Deploy-Liste gilt.

**Daten- und Einmal-Einträge**

| Id | Version | Typ | Firma | Phase | Ausführung | Inhalt / genauer Aufruf | Spur | Quelle |
|---|---|---|---|---|---|---|---|---|
| `Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand` | 1.88.0 | Einmalig | beide | Haltepunkt | Trockenlauf | Web gestoppt, 133+141 da. Trockenlauf = Datei unverändert (`DECLARE @Trockenlauf BIT = 1;`, Z. 33; Trockenlauf endet mit `RETURN`, Z. 92-96). Zählung und Liste „Altbestand prüfen“ ins Protokoll, Gerald gibt frei, scharf: Kopie mit `@Trockenlauf = 0` in SSMS, Wartungsfenster bis zum Leitstand-Schritt. | `SELECT COUNT(*) FROM dbo.PickingOrders WHERE CreatedBy = N'Migration-KO-Altbestand'` (nur wenn Tabelle existiert; das Skript selbst bricht dann ab, Z. 47-51) | 2026-11-08-v1-88-0-kommissionierauftraege Z. 22-24; 2026-11-08-v1-89-0-… Z. 19 |
| `SQL/108` | 1.79.0 | Daten | beide | Nachher | anzeigen | Vorher P-4 lesend (Skript führt die beiden SELECTs aus dem Dateikopf aus, Z. 7 und 27), dann von Hand `sqlcmd … -b -i "{SqlOrdner}\108_RemoveAppSettingsRueckmeldungOhneLeser.sql"`. Löscht 3 AppSettings-Zeilen ohne Leser, idempotent. | – | 2026-11-08-v1-79-0-… Z. 9, 22; 2026-11-08-v1-94-0-… Z. 18 |
| `SQL/110` | 1.87.0 | Daten | beide | Nachher | Trockenlauf | Nach Web- und Dienst-Start (gemeinsames EF-Modell). Trockenlauf = Datei unverändert (`DECLARE @Trockenlauf BIT = 1;`, Z. 17; `RETURN` Z. 67-70). Ergebnis als Datei ins Protokoll; scharf von Hand mit `@Trockenlauf = 0`; zweiter Trockenlauf muss 0 liefern. | – | 2026-11-08-v1-87-0-lagerbuchungen-fa-feld Z. 22; `SQL/110:9-12` |
| `Einmalig/2026-09-28_Hotfix-1.30.1_ResetAutosaveZeroQuantityPicked` | 1.30.1 | Einmalig | beide | Nachher | anzeigen | **Kein Trockenlauf-Schalter** (UPDATE nach Zählung, Z. 35-43) → nie durch das Skript. Nach Web-Deploy, ruhiges Fenster, danach Lager-Tabs neu laden. Nur wenn auf dem System nicht gelaufen (Rückfrage 4). | keine | 2026-10-05-v1-47-1-… Z. 22-24, 33-34; Skriptkopf Z. 11, 25-30 |
| `Einmalig/2026-10-07_v1.47.4_RemoveParentWorkOperationsFromChildSubFaTokens` | 1.47.4 | Einmalig | IDEAL | Nachher | anzeigen | **Kein Trockenlauf-Schalter** (DELETE in Transaktion, Z. 76-77). Nach Dienst-Deploy ≥ v1.47.4. Nur wenn nicht gelaufen (Rückfrage 4). | keine | 2026-10-07-v1-47-4-… Z. 20-27 |
| `Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel` | 1.60.0 | Einmalig | IDEAL | Nachher | anzeigen | Listenmodus `@Loeschen = 0` (Z. 51) braucht vorher `Sync:ArticlesViewName`, einen Artikel-Lauf, P7 und die `@Behalten`-Liste (Z. 22-35) → nicht automatisierbar. Entfällt an IDEAL-Prod ([[2026-11-04-ideal-produktiv-installation]] Z. 45-46). | `SELECT CASE WHEN OBJECT_ID(N'dbo.Articles_Bereinigung00er_Sicherung') IS NULL THEN 0 ELSE 1 END` (fehlt nach Schritt 9 des Skriptkopfs wieder → nur Hinweis) | 2026-10-15-v1-60-0-… Z. 22, 28-29 |

**Vorher-, Einstellungs-, Leitstand- und Info-Einträge** (alle `Ausfuehrung = anzeigen`; Spalte „Firma“ wie Changelog)

| Id | Version | Typ | Firma | Phase | Inhalt | Quelle |
|---|---|---|---|---|---|---|
| `v0-Backup` | 0.0.0 (immer) | Info | beide | Vorher | DB-Backup; Skript-Schritt 2 | ADR 0004; v1.32.0 Z. 68; v1.87.0 Z. 22; v1.88.0 Z. 22 |
| `v1.32.0-sysjobs` | 1.32.0 | Info | beide | Vorher | Agent-Jobs gegen `ProductionOrders` prüfen; Skript führt die Abfrage vor SQL/90 aus | 2026-08-17-v1-32-0-ideal-teil-7 Z. 69 |
| `v1.39.0-Edge` | 1.39.0 | Info | beide | Vorher | Microsoft Edge auf dem Web-Server, ausführbar unter der App-Pool-Identität, `%TEMP%\IdealAkeWms-Pdf` beschreibbar; Edge-Version notieren | 2026-09-14-v1-39-0-… Z. 57-58; 2026-10-14-v1-59-0-… Z. 30 |
| `v1.49.0-Branding` | 1.49.0 | Info | beide | Vorher | Ordner `Branding\` im Programmordner (nicht `wwwroot\`), Leserecht App-Pool, Einstellung `BriefkopfVorlagePfad`; Skript prüft `Test-Path`, kopiert ihn nie | 2026-10-09-v1-49-0-… Z. 33-35; 2026-10-14-v1-59-0-… Z. 29-30 |
| `v1.49.0-Anschrift` | 1.49.0 | Einstellung | IDEAL | Nachher | Firmenanschrift korrigieren („38, 4840“ → „28, 4810“) | 2026-10-09-v1-49-0-… Z. 35 |
| `v1.58.0-P123` | 1.58.0 | Info | beide | Vorher | P-1 gespeicherte Standardfilter, P-2 Kollation DB und je Spalte (Pflicht), P-3 Server-Kultur; Abfragen in der Suchsyntax-Spec | 2026-10-13-v1-58-0-… Z. 29; 2026-10-13-v1-58-1-… Z. 28-29 |
| `v1.61.0-P5` | 1.61.0 | Info | beide | Vorher | P5 je Standort (Arbeitsgang-Abgleich) | 2026-10-16-v1-61-0-… Z. 29 |
| `v1.71.0-AppsettingsProd` | 1.71.0 | Info | beide | Vorher | `appsettings.Production.json` mit Abschnitt `SageLagerbuchung` im Web-Ordner vorhanden; Skript prüft Existenz, überschreibt nie | 2026-10-30-v1-71-0-… Z. 22-23 |
| `v1.84.0-Einkauf` | 1.84.0 | Info | AKE | Vorher | Einkauf informieren: kein WE-Beleg mit Lagerbuchung in Sage (Doppelbestand) | 2026-11-08-v1-84-0-… Z. 23 |
| `v1.93.0-Zaehlung` | 1.93.0 | Info | beide | Vorher | Zählabfrage Meldebestand je Standort vor PROD (Spec) | 2026-11-08-v1-93-0-… Z. 17 |
| `v1.31.0-IDEAL-Schalter` | 1.31.0 | Einstellung | IDEAL | Nachher | `Sync:HierarchicalFaEnabled`, `FaHierarchyKommissionierlistenAktiv`/`…BeschichtungAktiv`/`…VormontageAktiv`; `SageConnection`; View-Namen ohne Schema-Präfix; RCSI = 1 | 2026-08-10-v1-31-0-… Z. 116-125 |
| `v1.40.0-Matchcode` | 1.40.0 | Info | beide | Nachher | Matchcode erscheint erst nach dem nächsten Artikel-Lauf | 2026-09-15-v1-40-0-… Z. 54-56 |
| `v1.44.0-Werkbank` | 1.44.0 | Einstellung | IDEAL | Nachher | DryRun; erst `Sync:ProductionWorkplaceSyncEnabled`, dann `Sync:WorkOperationStructureDetectionEnabled` | 2026-09-22-v1-44-0-… Z. 59-61 |
| `v1.51.0-Rolle` | 1.51.0 | Einstellung | AKE | Nachher | Rolle `beschichtungsauftrag_schreiben` unter `/Roles` anlegen und zuweisen (kein Seed); IDEAL hat sie aus Installation Teil 2 | 2026-10-12-v1-51-0-… Z. 32-33; Backlog |
| `v1.56.0-Info` | 1.56.0 | Info | beide | Nachher | Anwender-Info: Beschichtung im Menü | 2026-10-13-v1-56-0-… Z. 26 |
| `v1.57.0-PT1` | 1.57.0 | Info | AKE | Nachher | P-T1 inkl. Kollisionsabfrage vor der AKE-Abnahme | 2026-10-13-v1-57-0-… Z. 28-29 |
| `v1.58.1-Info` | 1.58.1 | Info | beide | Nachher | Anwender-Info: „oder“ = `;`, Fehlteile `!a;b` | 2026-10-13-v1-58-1-… Z. 30 |
| `v1.60.0-IDEAL-Views` | 1.60.0 | Einstellung | IDEAL | Nachher | P1–P5b auf Sage, `Sync:ArticlesViewName` setzen und nicht mehr leeren | 2026-10-15-v1-60-0-… Z. 27-29 |
| `v1.61.0-IDEAL-Altzeilen` | 1.61.0 | Info | IDEAL | Nachher | alte OperationConfig-Zeilen erst nach dem ersten Lauf löschen | 2026-10-16-v1-61-0-… Z. 30 |
| `v1.63.0-IDEAL-DryRun` | 1.63.0 | Info | IDEAL | Nachher | P1–P3/P6 + DryRun vor dem Scharfschalten | 2026-10-19-v1-63-0-… Z. 32-33 |
| `v1.63.0-Info` | 1.63.0 | Info | beide | Nachher | Werker-Info: Fortsetzen an fremder Werkbank wird abgewiesen | 2026-10-19-v1-63-0-… Z. 33-34 |
| `v1.64.0-MaxHauptFa` | 1.64.0 | Einstellung | beide | Nachher | `/Settings` › BDE: `BdeTerminalMaxHauptFa = 5` (wirkt laut README Z. 420 nur IDEAL); AKE: Prüfabfrage Q4 | 2026-10-19-v1-64-0-… Z. 26-27 |
| `v1.67.0-IDEAL-Vault` | 1.67.0 | Einstellung | IDEAL | Nachher | „Vault-Links anzeigen“ aus; enaio entscheidet Gerald | 2026-10-22-v1-67-0-… Z. 26 |
| `v1.68.0-IDEAL-Qr` | 1.68.0 | Einstellung | IDEAL | Nachher | `QrErstesFeld = Matchcode` erst nach TS-101 | 2026-10-27-v1-68-0-… Z. 21 |
| `v1.69.0-Trennzeichen` | 1.69.0 | Info | beide | Nachher | bei `QrTrennzeichen = ,` vorher Lagerplatz-Codes mit Komma prüfen | 2026-10-27-v1-69-0-… Z. 21 |
| `v1.71.0-IDEAL-SData` | 1.71.0 | Einstellung | IDEAL | Nachher | SData-FA-Ansicht aus (kein SData-Host) | 2026-10-30-v1-71-0-… Z. 24 |
| `v1.72.0-Rollendrucker` | 1.72.0 | Einstellung | beide | Nachher | Lagerplatz-Etikett Rollendrucker nur bei Bedarf einschalten | 2026-11-06-v1-72-0-… Z. 23 |
| `v1.73.0-SData-Stamm` | 1.73.0 | Einstellung | beide | Nachher | `Sync:KreditorenEnabled`, `Sync:DebitorenEnabled`, `Sync:ArtikelLieferantEnabled` ([[services]] Z. 337-338); IDEAL erst mit SData-Endpunkt und Lizenz, erste Läufe als `Vergleich` | 2026-11-06-v1-73-0-… Z. 36; Backlog |
| `v1.77.0-Pruefung` | 1.77.0 | Info | beide | Nachher | Aktivitäts-Protokoll „Einstellungsprüfung“ prüfen (P-E1a–c) | 2026-11-07-v1-77-0-… Z. 30 |
| `v1.79.0-Speichern` | 1.79.0 | Einstellung | beide | Nachher | Einstellungsmasken je Standort einmal speichern (F4; Altwert `1` wird `false`, Liste im QA-Abschnitt der Spec) | 2026-11-08-v1-79-0-… Z. 16, 23-24 |
| `v1.84.0-Wareneingang` | 1.84.0 | Einstellung | AKE | Nachher | `Sync:EinkaufBestellungenEnabled` an ([[services]] Z. 339), P-W6 prüfen, erst dann `WareneingangBestellungAktiv`; IDEAL aus bis SData-Endpunkt | 2026-11-08-v1-84-0-… Z. 24; [[2026-11-08-buendel-uebersicht]] Z. 146 |
| `v1.85.0-Einheiten` | 1.85.0 | Info | beide | Nachher | Mengeneinheiten erst nach Artikel- und Artikel-Lieferanten-Lauf; Q-5 offen | 2026-11-08-v1-85-0-… Z. 21-22 |
| `v1.88.0-Leitstand` | 1.88.0 | Leitstand | beide | Nachher | „Fehlende Kommissionieraufträge für alle freigegebenen FAs erzeugen“ – direkt nach dem Web-Start, vorher niemand kommissionieren | 2026-11-08-v1-88-0-… Z. 22-23; Altbestand-Kopf Z. 26, 28 |
| `v1.89.0-Altbestand` | 1.89.0 | Leitstand | beide | Nachher | Liste „Altbestand prüfen“ je FA abarbeiten, „Altbestand geprüft“, erneut erzeugen; Stichprobe TS-120.11 | 2026-11-08-v1-89-0-… Z. 15; Altbestand-Kopf Z. 27 |
| `v1.90.0-F5` | 1.90.0 | Info | beide | Nachher | Strg+F5 an den Geräten (`fa-feld.js`) | 2026-11-08-v1-90-0-… Z. 18 |
| `v1.92.0-Final` | 1.92.0 | Info | beide | Nachher | Prüfabfrage früher verlorene Final-Kennzeichen (lesend, Spec) | 2026-11-08-v1-92-0-… Z. 18 |
| `v1.93.0-Mail` | 1.93.0 | Info | beide | Nachher | erste Meldebestand-Mail nach dem Deploy ist lang (gewollt) | 2026-11-08-v1-93-0-… Z. 17 |

Versionen ohne Deploy-Schritt (Changelog „Nur Web/Publish“, „Keine“): 1.33–1.37 (1.37-Vorbedingung „Werkbänke vorher anlegen“ ist durch
den Rückbau v1.43.0 überholt, 2026-09-22-v1-43-0-… Z. 63), 1.41 (durch v1.44.0 ersetzt, nie deployt), 1.43, 1.45, 1.47.2/.3/.5, 1.52,
1.53, 1.55, 1.59 (außer Branding), 1.60.1/.2 (nur IDEAL-Prüfabfragen vor dem View-Umstellen, mit 1.60.0 abgedeckt), 1.62, 1.64.1–1.67.2,
1.68.1, 1.70, 1.71.1, 1.75, 1.76, 1.78, 1.80, 1.81, 1.86, 1.91, 1.94 (außer SQL/108-Nachtrag), 1.95. Die Lücken im Manifest stehen unten
unter „Manifest-Lücken“.

### Drift-Test `IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs`
Muster `OperationConfigsMigrationSqlTests` (Repo-Root-Suche Z. 20-26, `File.ReadAllText`). Prüft als Text (kein PowerShell im Test):
- jede `[Migration("…")]`-Id nach `20260806120650_AllowMultipleDummyRequisitionItems` steht als `MigrationId` im Manifest;
- jede `SQL/<Nr>_*.sql` mit Nr ≥ 89 und jede `SQL/Einmalig/*.sql` steht als `Datei` im Manifest;
- jede `Datei` im Manifest existiert;
- die `MigrationId` eines Eintrags kommt in seiner Datei vor;
- die Schema-Einträge stehen in der Reihenfolge der Tabelle oben (Liste im Test).
Damit fällt ein vergessener Manifest-Eintrag im nächsten Dev-Lauf auf, nicht erst am Zielserver.

### Pflicht „Manifest nachtragen“
- `.claude/agents/spec-agent.md`, Abschnitt `DEPLOY SECTION` (Z. 113 ff.): je neuem SQL-Skript, Einmal-Skript oder Hand-Schritt den
  geplanten Manifest-Eintrag (Id, Typ, Firma, Phase, Ausführung, Text) im Deploy-Abschnitt der Spec nennen.
- `secondbrain/_templates/spec.md`, Abschnitt `## Deploy`: Zeile „**Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):“.
- `.claude/agents/qa-agent.md` Punkt 7: Paketinhalt und `publish-info.txt`-Format (oben); bei Spec mit Deploy-Schritt prüfen, dass der
  Eintrag im Manifest steht (der Drift-Test deckt SQL ab, Hand-Schritte nicht).
- **Vorschlag** CLAUDE.md-Checkliste (Gerald ändert selbst): „- [ ] Deploy-Schritt (SQL, Einmal-Skript, Einstellung, Leitstand,
  Anwender-Info) in `scripts/deploy/nachlauf.psd1` nachgetragen?“

### README (Bündel)
- „Update bestehender Installation“ (Z. 66-157): Einleitung ersetzen durch Verweis auf `scripts\deploy\Update-IdealAkeWms.ps1` und
  `scripts\deploy\nachlauf.psd1`; Tabelle 01–88 bleibt als historische Liste („bis SQL/88; ab SQL/89 gilt das Manifest“), Zeilen 95 und
  97 entfallen.
- „Update (neue Version einspielen)“ (Z. 796-812): ersetzen durch Aufruf-Beispiel, Paket-Beschreibung und Hinweis „appsettings*.json,
  web.config, Branding\, App_Data\, wwwroot\Fotos\ werden nie überschrieben“.

## Migrations-/SQL-Auswirkungen

Keine neue Migration, kein neues SQL-Skript, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/*` unverändert. Das Skript spielt die
vorhandenen Skripte ein; ihre OBJECT_ID-/COL_LENGTH-Guards und History-Inserts bleiben die Idempotenz-Grundlage (ADR 0004). Nicht
idempotent im Sinne der Reihe sind nur 108 (idempotent, aber löschend), 110 und die Einmal-Skripte (ADR 0015) – die führt das Skript nie
scharf aus.

## Einstellungen

keine. Das Skript liest nur `ConnectionStrings:DefaultConnection` und `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT`; es legt keine
AppSettings/ServiceSettings an und ändert keine. Skript-Parameter sind keine Einstellungen im Sinne von ADR 0020.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Das Skript schreibt keine fachlichen Daten. Die eingespielten Skripte bringen ihre Audit-Behandlung selbst mit (z. B. `SQL/95`
B8 `CreatedBy = 'system'`, `SQL/110` Kopf Z. 10, Altbestand `@Wer`). Rollen/Zugriffsfilter: keine Änderung. Listen-View-Pattern: keine
neue Tabellenansicht.

## Akzeptanzkriterien

1. Ohne `-Firma`/`-System` fragt das Skript beides ab; ungültige Eingabe → erneute Frage. Mit beiden Parametern keine Abfrage.
2. Pfade folgen dem Muster der Tabelle „Zielsysteme“; `-WebPfad`/`-DienstPfad`/`-LogPfad` überschreiben es; mit `-System beide` führen
   Pfadparameter zum Abbruch vor jeder Aktion.
3. Fehlt Web-Ordner, `IdealAkeWms.dll`, Dienst-Ordner oder `IDEALAKEWMSService.exe` → Abbruch mit Pfad, nichts gestoppt, keine DB
   kontaktiert.
4. Site/App-Pool werden über den physischen Pfad ermittelt; 0 oder >1 Treffer → Abbruch mit Trefferliste; geteilter App-Pool → Warnung
   mit Liste und Bestätigung.
5. Der Dienst wird über den Exe-Pfad ermittelt; 0 oder >1 Treffer → Abbruch.
6. DB Web aus `web.config`-Umgebung + `appsettings*.json`, DB Dienst aus Registry-/Maschinen-Umgebung + `appsettings*.json`; Web ≠ Dienst
   → Abbruch; bei `beide` Test-DB = Prod-DB → Abbruch.
7. `publish-info.txt` fehlt oder Web/Dienst unterscheiden sich in `Version`/`Commit` → Abbruch.
8. Alle ermittelten Werte werden angezeigt; der erste DB-Kontakt erfolgt erst nach Bestätigung; „nein“ → Ende ohne DB-Kontakt,
   Protokoll geschrieben.
9. Vorprüfung zeigt installierte Version (Quelle genannt), fehlende Schema-Skripte in Manifest-Reihenfolge, RCSI, Spur-Ergebnisse der
   Einmal-Skripte und die Vorher-Einträge.
10. `-NurPruefen`: kein Dienst/App-Pool gestoppt, kein schreibendes SQL, keine Datei im Ziel geändert; Plan und Nachlauf-Liste
    ausgegeben, Protokoll geschrieben, Exit 0.
11. Backup-Angebot mit `COPY_ONLY`; Fehler beim Backup → Abbruch vor dem Stoppen.
12. Dienst und App-Pool werden gestoppt und erst nach `Stopped` geht es weiter; Timeout → Abbruch.
13. Jedes Schema-Skript nur nach Bestätigung, mit `sqlcmd -b`; Exit ≠ 0 → Abbruch bei gestopptem Web und Dienst; nach jedem Lauf steht
    die MigrationId in der History.
14. Vor SQL/90: sysjobs-Abfrage angezeigt (oder Hinweis bei fehlendem Recht) und Extra-Bestätigung.
15. SQL/95 nur, wenn `20260330142035_AddOseonOperationConfig` in der History steht; `ABBRUCH` in der Ausgabe → Abbruch.
16. Fehlt nach Phase 4 eine MigrationId des Manifests, kopiert und startet das Skript nichts (Exit ≠ 0, Meldung nennt `Migrate()`).
17. `SQL/108`, `SQL/110` und `SQL/Einmalig/*` werden nie scharf ausgeführt. Ein Trockenlauf läuft nur bei `Ausfuehrung = Trockenlauf`,
    nach Bestätigung, und nur wenn die Datei die Zeile `DECLARE @Trockenlauf BIT = 1;` wörtlich enthält; sonst nur Anzeige. Eine
    Testkopie mit `= 0` wird abgelehnt.
18. Kopieren mit `robocopy /E` ohne Löschen; `appsettings*.json`, `web.config`, `Branding\`, `App_Data\`, `wwwroot\Fotos\`, `logs\` im
    Ziel unverändert (Hash bzw. Inhalt gleich); robocopy-Exit ≥ 8 → Abbruch.
19. Nach dem Web-Start: `IdealAkeWms.dll` hash-gleich mit dem Paket und Fußzeile = Paket-Version (oder Bestätigung durch den Menschen);
    sonst App-Pool gestoppt, Dienst nicht gestartet, Exit ≠ 0.
20. Dienst startet nach dem Web, `IDEALAKEWMSService.dll` hash-gleich, nach 30 s `Running`; sonst Abbruch mit Log-Hinweis.
21. Haltepunkt-Einträge erscheinen nach dem Schema und vor dem Kopieren; weiter nur mit „ausgeführt“ (Ausgang laut Rückfrage 3).
22. Nachlauf-Liste: gefiltert nach Firma und Version > installiert (unbekannt = alle); Einmal-Einträge immer, mit Spur-Ergebnis oder
    „unbekannt, Deploy-Protokoll prüfen“; Aufrufe mit eingesetztem Server, DB und Pfad.
23. Protokoll unter `<LogPfad>\yyyy-MM-dd_HHmm_<FIRMA>-<SYSTEM>_v<Version>.log` mit sqlcmd-Ausgaben, auch bei Abbruch.
24. `beide`: Test zuerst; Fehler bei Test → Prod unberührt; vor Prod nur mit Eingabe `<FIRMA>-PROD` weiter.
25. Manifest enthält alle Einträge der drei Tabellen oben; `NachlaufManifestDriftTests` grün; ein absichtlich entfernter Eintrag macht ihn
    rot.
26. `[System.Management.Automation.Language.Parser]::ParseFile` meldet für Skript und Manifest keine Fehler; beide Dateien nur ASCII.
27. Das Skript enthält keine Verbindung zu Sage (`SageConnection`, SData-URL) – Grep leer.
28. README beide Abschnitte ersetzt; `qa-agent.md`, `spec-agent.md`, Spec-Vorlage angepasst; CLAUDE.md-Vorschlag steht in dieser Spec.
29. `dotnet build IdealAkeWms.slnx` und `dotnet test` grün.

## Test-Szenarien

Neues Kapitel **TS-124 — Update-Skript je Zielsystem** in `docs/TESTSZENARIEN.md` (Nummer prüft der Dev-Lauf; letzte im Bündel TS-123),
danach `secondbrain/tests/testszenarien-index.md`.

**Vorbedingungen:** DEV-IDEAL (`D:\Dev\IDEAL\Web`, `D:\Dev\IDEAL\Service`, Site/App-Pool `IDEAL_WMS_DEV`, Dienst `IDEAL_WMS_DEV_Service`,
`dev-deploy.ps1:16-19`), DB lokal `.`; Paket = `<worktree>\publish\` mit `publish-info.txt`, `SQL\`, `deploy\`. **Nie** gegen akenet01,
idealweb01 oder AKESQL20. Vor jedem Lauf zeigt das Skript Server/DB – Agent bricht ab, wenn dort nicht `.` bzw. `localhost` steht.

Aufruf (Agent, nur so):
```
powershell -ExecutionPolicy Bypass -File <worktree>\scripts\deploy\Update-IdealAkeWms.ps1 -Firma IDEAL -System Test `
  -WebPfad D:\Dev\IDEAL\Web -DienstPfad D:\Dev\IDEAL\Service -LogPfad D:\Dev\logs\deploy `
  -Quelle <worktree>\publish -SqlOrdner <worktree>\SQL -NurPruefen
```

| Nr | Szenario | Erwartet | Wer |
|---|---|---|---|
| 124.1 | Aufruf oben | ermittelt Site, App-Pool, Dienst, Umgebung DEV, DB `.`; Bestätigung „ja“; Version, History, fehlende Skripte (erwartet keine), RCSI, Spuren; Nachlauf-Liste; Protokoll in `D:\Dev\logs\deploy`; nichts gestoppt (Dienst/App-Pool-Status vorher = nachher) | Agent |
| 124.2 | Wie 124.1 ohne `-Firma`/`-System` | Abfrage beider Werte; Eingabe `XYZ` → erneute Frage | Agent (Eingabe per Pipe) oder Mensch |
| 124.3 | Falsche Firma: `-Firma AKE -System Test` ohne Pfadparameter | `C:\AKE\IdealAkeWmsService_Test` fehlt → Abbruch, keine DB kontaktiert | Agent |
| 124.4 | Pfad fehlt: `-WebPfad D:\Dev\IDEAL\WebX` | Abbruch mit Pfad | Agent |
| 124.5 | `-System beide` mit `-WebPfad` | Abbruch vor jeder Aktion | Agent |
| 124.6 | Bestätigung „Verbindung herstellen“ verneint | Ende ohne DB-Kontakt, Protokoll vorhanden | Agent |
| 124.7 | DB nicht erreichbar: Kopie des DEV-Web-Ordners mit `appsettings.DEV.json` auf Server `.\NICHTDA` (nur in einem Testordner, nie im echten DEV-Ordner) | Abbruch mit sqlcmd-Fehler, Exit ≠ 0 | Agent |
| 124.8 | Web-DB ≠ Dienst-DB (Testordner wie 124.7) | Abbruch vor DB-Kontakt | Agent |
| 124.9 | `publish-info.txt` mit abweichendem `Commit` im Dienst-Elternordner (Testkopie) | Abbruch | Agent |
| 124.10 | Einmal-Skript wird nicht ausgeführt: Nachlauf-Liste nennt alle 4 Einmal-Skripte mit Aufruf; Protokoll enthält keinen sqlcmd-Aufruf auf `SQL\Einmalig\*` außer Trockenlauf-Einträgen; Testkopie Altbestand mit `@Trockenlauf BIT = 0` → „abgelehnt, nur Anzeige“ | wie beschrieben | Agent |
| 124.11 | Version passt nach Start nicht (Volllauf auf DEV-IDEAL, `publish-info.txt` mit `Version=9.9.9`) | App-Pool wieder gestoppt, Dienst nicht gestartet, Exit ≠ 0; danach `dev-deploy.ps1 -Standort IDEAL` zur Wiederherstellung | **Mensch** (Rückfrage 9) |
| 124.12 | Volllauf DEV-IDEAL regulär | Schema: nichts fehlt; Kopieren schont `appsettings*.json`/`web.config`/`Branding`; Web, dann Dienst; Fußzeile v1.95.0; Nachlauf-Liste | **Mensch** |
| 124.13 | Drift-Test | `NachlaufManifestDriftTests` grün; Eintrag `SQL/141` entfernt → rot | Agent |
| 124.14 | PS 5.1 | `ParseFile` ohne Fehler unter `powershell.exe` (nicht `pwsh`) | Agent |

**Negativfall Zielsystem** (Mensch, erst nach Freigabe des Bündel-Deploys): Erster echter Lauf mit `-NurPruefen` je Zielsystem; Plan
mit der Backlog-Tabelle vergleichen (AKE: 89–107, 109, 112, 113, 133, 140, 141 fehlen; IDEAL-Prod: 98–141 ohne 108/110 fehlen).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | kein Epic | – | – |

## Deploy

**Vorläufig** (QA bestätigt am echten Diff):
- **Web-App:** nein · **Service:** nein · **Migration:** nein. Keine Änderung an Anwendungscode, daher kein Versions-Bump in
  `AppVersion.cs` und kein Anwender-Changelog; Nachweis im QA-Abschnitt dieser Spec, Eintrag im Bündel-Changelog des nächsten
  Versionsschritts.
- Auslieferung: Skript und Manifest kommen über das Paket (`<worktree>\publish\deploy\`) auf den Server; der Mensch kopiert `publish\`
  z. B. nach `C:\<FIRMA>\deploy\v1.95.0\` und startet dort.
- Erster Einsatz: Bündel-Deploy v1.95.0, je System zuerst `-NurPruefen`, Plan prüfen, dann Volllauf.
- **Manifest-Einträge:** dieses Spec liefert das Manifest selbst.
- **Publish-Befehle:** unverändert laut `qa-agent.md` Punkt 7 (plus Kopie von `SQL\` und `scripts\deploy\` ins Paket).

**Brain** (Hauptcheckout): ADR 0022 „Update je Zielsystem per Skript mit Nachlauf-Manifest“ (Entscheidung: Schema automatisch mit
Bestätigung, Daten/Einmalig nie scharf, Manifest-Pflicht, Drift-Test); `fallstricke.md`: „Kopieren nie mit `/MIR`; Fotos und
Konfiguration liegen im Programmordner“, „DB-Ermittlung braucht die Umgebung aus `web.config` bzw. Registry – sonst liest ein Lauf auf DEV
die Produktiv-Verbindung aus `appsettings.json`“; `feature-map.md`; Aufgabe für den Bündel-Deploy mit Protokoll je System.

## Offene Rueckfragen

1. **Marker-Tabelle für gelaufene Einmal-Skripte (Nachtrag zu ADR 0015)?**
   - Für: Der Nachweis liegt in derselben DB und stimmt deshalb auch nach einem Restore; das Skript kann gelaufene Skripte sicher
     ausblenden statt „unbekannt“ zu zeigen; Personal- oder Rechnerwechsel verlieren nichts; inzwischen 4 Einmal- und 2 Datenskripte, mit
     C2 und den Rückmelde-Epics kommen weitere.
   - Wider: Schema für Betriebsdaten (Migration + SQL + FreshInstall, ADR 0004); jedes Einmal-Skript muss den Marker selbst in seiner
     Transaktion schreiben, die vier vorhandenen müssten nachgerüstet werden; der Altstand (was lief vor dem Marker?) bleibt trotzdem
     Handarbeit.
   - Ohne Marker trägt dieser Entwurf die **Spur-Abfrage** je Skript, wo eine eindeutige Spur existiert (Altbestand, 00er), sonst
     „unbekannt, Deploy-Protokoll prüfen“.
   - **Empfehlung:** Ja, als eigene kleine Folge-Spec nach dem Bündel-Deploy (Tabelle `DeployEinmalLaeufe` mit Skript, Zeit, Wer,
     Ergebnis; die Skripte schreiben sie selbst). Den Bündel-Deploy nicht daran hängen.
2. **`web.config` am Ziel:** Das Repo setzt `ASPNETCORE_ENVIRONMENT=Production` und `stdoutLogEnabled="true"`
   (`IdealAkeWms/web.config:13, 18`); ob die Installation oder ein Mensch sie am Ziel angepasst hat, ist nicht belegt. **Empfehlung:**
   nicht überschreiben, bei inhaltlichem Unterschied Paket ↔ Ziel eine Warnung mit beiden Fassungen im Protokoll.
3. **Haltepunkt Altbestand (v1.88.0):** Startet das Web ohne Überführung, legen Freigabe und „Öffnen“ schon Aufträge an (C2 Antwort 7,
   [[2026-11-08-buendel-uebersicht]] Z. 173); das Einmal-Skript prüft danach nur seine eigene Spur (Z. 47-51). **Empfehlung:** kein
   „überspringen“ – nur „ausgeführt“ oder „Abbruch, Web bleibt gestoppt“.
4. **Stand der Einmal-Skripte je System** (steht nur im Deploy-Protokoll, ADR 0015): Hotfix-1.30.1 an AKE-Prod, AKE-Test, IDEAL-Test
   gelaufen? v1.47.4 und 00er-Bereinigung an IDEAL-Test? Das Rücksetzen freigegebener Sub-FAs aus v1.46.0 (SQL nur in der Spec,
   2026-09-25-v1-46-0-… Z. 45-46) an IDEAL-Test? IDEAL-Prod: Hotfix-1.30.1 und v1.47.4 entfallen nach meiner Lesart, weil die
   Neuinstallation mit Bündel-Code v1.71.1 lief (enthält v1.47.1 und v1.47.4) – bitte bestätigen. Die Backlog-Liste nennt Hotfix-1.30.1
   nicht.
5. **Hotfix v1.30.4** (Fotos nach `App_Data`, Commit `29a2bfe2`) liegt nur in `main`; das Bündel legt Fotos weiter unter
   `wwwroot\Fotos\Kommissionierung` ab (`IdealAkeWms/Program.cs:396` im Bündel). Läuft v1.30.4 vor dem Bündel auf einem System, baut der
   Bündel-Deploy die Sicherung zurück. **Empfehlung:** v1.30.4 vor dem Bündel-Deploy per Vorwärts-Merge ins Bündel; das Skript warnt, wenn
   das Ziel `App_Data\Fotos` hat.
6. **Rolle `beschichtungsauftrag`** (Lesen, v1.31.0 Z. 62-64, kein Seed): an AKE ebenfalls anlegen? Der Backlog nennt nur
   `beschichtungsauftrag_schreiben` (schließt Lesen ein, v1.51.0 Z. 25).
7. **Zielsysteme bestätigen:** AKE-Prod auf akenet01 mit `C:\AKE\IdealAkeWmsService` und `C:\inetpub\wwwroot\IDEALAKEWMS` (Annahme,
   Backlog Z. 93); Test-Systeme mit eigener DB (das Skript bricht ab, wenn nicht). Prüfschritte auf beiden Servern:
   `Get-Command sqlcmd`; und in SSMS als das ausführende Konto:
   `SELECT IS_ROLEMEMBER('db_owner') AS DbOwner, HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'BACKUP DATABASE') AS Backup, HAS_PERMS_BY_NAME('msdb.dbo.sysjobs', 'OBJECT', 'SELECT') AS SysJobs;`
8. **`-PublishWeb`/`-PublishDienst`:** im Entwurf als Quellpfade der Publish-Ordner gedeutet (überschreiben den Default aus dem Paket).
   Waren Schalter „nur Web“/„nur Dienst“ gemeint?
9. **Volllauf auf DEV:** Der Auftrag erlaubt dem Agenten nur `-NurPruefen`. TS-124.11/.12 brauchen einen echten Start. **Empfehlung:**
   Gerald fährt beide einmal von Hand auf DEV-IDEAL, danach `dev-deploy.ps1` zur Wiederherstellung.
10. **Ablageort der Agenten-/Vorlagen-Änderungen:** `.claude/agents/*.md` und `secondbrain/_templates/spec.md` direkt im Hauptcheckout
    (wirken sofort; so wurde `qa-agent.md` Punkt 7 am 09.11. geändert), Skript/Manifest/Test/README im Bündel-Worktree; CLAUDE.md nur als
    Vorschlag. Einverstanden?

### Manifest-Lücken (nicht geraten, offen)
- Kein Changelog für v1.82.0 und v1.83.0 (v1.74.0 entfällt belegt, [[2026-11-07-buendel-2026-11-07-goal-lauf]] Z. 27). Laut
  [[2026-11-08-buendel-uebersicht]] Z. 39-40 war 1.82.0 nur bei Codeänderung vergeben und A5 wurde 1.87.0; ob 1.82/1.83 je ausgeliefert
  wurden, ist nicht belegt.
- v1.38.0-Changelog nennt `SQL/91_AddProductionOrderMatchcode.sql` (Z. 64); die Datei heißt `91_AddArticleMatchcode.sql` (v1.40.0 hat den
  Ort widerrufen, Z. 20-23). Das Manifest folgt der Datei.
- Prüfabfragen stehen nur in Specs, nicht als Datei: P-1/P-2/P-3 (v1.58), P5 (v1.61), P-T1 (v1.57), Q4 (v1.64), P-E1a–c (v1.77), P-W6
  (v1.84), Q-5 (v1.85), Final-Kennzeichen (v1.92), Meldebestand-Zählung (v1.93). Der Dev-Lauf übernimmt den Abfragetext ins Manifest, wo er
  in der Spec eindeutig steht, sonst bleibt der Verweis.
- Hotfix-1.30.1-Einmal-Skript fehlt in der Backlog-Liste; Stand je System unbekannt (Rückfrage 4).
- v1.46.0: Rücksetzen freigegebener Sub-FAs am Testsystem, SQL nur in der Spec; Stand unbekannt (Rückfrage 4).
- v1.30.4 nicht im Bündel (Rückfrage 5).
- Rolle `beschichtungsauftrag` an AKE (Rückfrage 6).
- `BdeTerminalMaxHauptFa`: Changelog ohne Standort, README Z. 420 „nur IDEAL“ – Manifest „beide“ mit Hinweis.
- IDEAL-Test: Version „≥ v1.67“, Migration unbekannt (Backlog-Tabelle) – das Skript ermittelt beides; ob die IDEAL-Aktivierungsschritte
  v1.31–v1.67 dort vollständig liefen, ist nicht belegt.
- v1.49.0 Anschrift-Korrektur: ob erledigt, unbekannt.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Marker-Tabelle Einmal-Skripte (ADR-0015-Nachtrag) ja/nein →
2. web.config am Ziel nicht überschreiben, Unterschied melden →
3. Haltepunkt Altbestand ohne „überspringen“ →
4. Stand der Einmal-Skripte je System (Hotfix-1.30.1, v1.47.4, 00er, v1.46.0-Rücksetzen; IDEAL-Prod entfallen?) →
5. v1.30.4 vor dem Bündel-Deploy ins Bündel holen →
6. Rolle `beschichtungsauftrag` (Lesen) an AKE anlegen →
7. Zielsysteme/DB/sqlcmd/Rechte bestätigt →
8. `-PublishWeb`/`-PublishDienst` = Quellpfade oder Schalter →
9. Volllauf DEV (TS-124.11/.12) durch Gerald →
10. Agenten-/Vorlagen-Änderungen im Hauptcheckout, CLAUDE.md nur Vorschlag →
