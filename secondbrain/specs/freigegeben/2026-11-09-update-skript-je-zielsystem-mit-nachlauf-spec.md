---
type: spec
title: Update-Skript je Zielsystem mit Nachlauf-Manifest (Bündel-Deploy ab v1.97.0)
slug: 2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec
status: Gemerged
created: 2026-11-09
updated: 2026-11-28
freigabe_von: "Gerald Weichbold (Chat Koordination, „passt“ auf Freigabe-Frage)"
freigabe_am: 2026-11-11
source_backlog: "[[2026-11-09-update-skript-je-zielsystem-mit-nachlauf]]"
task: "[[2026-11-11-buendel-deploy-update-skript]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - "scripts/deploy/Update-IdealAkeWms.ps1 (neu)"
  - "scripts/deploy/nachlauf.psd1 (neu)"
  - "scripts/deploy/ziele.psd1 (neu)"
  - "scripts/deploy/IdealAkeWms.Deploy.psm1 (neu, Test-OrdnerUndRechte)"
  - "scripts/deploy/Install-IdealAkeWms.ps1 (neu, aus secondbrain/tests/Claude outputs/Install-IdealAkeWms-IDEAL.ps1)"
  - "IdealAkeWms/AppVersion.cs, IDEALAKEWMSService/AppVersion.cs, Views/Help/Changelog.cshtml (v1.98.0)"
  - "IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs (neu, Muster OperationConfigsMigrationSqlTests)"
  - "README.md (Abschnitte 'Update bestehender Installation' Z. 66-157 und 'Update (neue Version einspielen)' Z. 796-812, Bündel-Stand)"
  - "docs/TESTSZENARIEN.md (neues Kapitel TS-125, Nummer prüft der Dev-Lauf)"
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
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
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
- ~~Zieldatei entfällt~~ – **geändert 2026-11-11:** Die Zielsysteme stehen in `scripts\deploy\ziele.psd1` (siehe „Zielsysteme“).
  Grund: An AKE läuft ein drittes System, `Test-IDEAL`, dessen Ordnernamen keinem Muster folgen. App-Pool, Site, Dienst und DB ermittelt
  das Skript weiterhin selbst.
- Neuinstallation (`Install-IdealAkeWms-IDEAL.ps1`, nicht im Repo, [[2026-11-04-ideal-produktiv-installation]]).
- Skripte vor SQL/89: alle vier Systeme stehen mindestens auf SQL/88 (Backlog-Tabelle). Das Manifest beginnt bei v1.31.0.
- Abnahme-Prüfschritte („vor der Abnahme“: TS-80.1, TS-81.1, TS-82.1, TS-83.1/83.2, P-K1, Server-Kultur v1.53.0). Sie gehören zur
  Abnahme, nicht zum Deploy, und kommen nicht ins Manifest.
- Marker-Tabelle für Einmal-Skripte: Entscheidung Gerald (Rückfrage 1); wenn ja, eigene Folge-Spec.
- Jeder Lauf gegen akenet01, idealweb01 oder AKESQL20 durch einen Agenten.

## Fachliche Anforderungen

### Grenzen (hart)
- Das Skript führt den Deploy auf den Zielsystemen nur aus, wenn ein Mensch es **auf dem Zielserver** startet. Agenten starten es nur auf
  DEV, dort auch als Volllauf (Freigabe-Antwort 9). Danach stellen sie DEV mit `dev-deploy.ps1` wieder her und setzen die 5
  AKE-DEV-Sync-Schalter wieder.
- Kein Aufruf an Sage, weder lesend noch schreibend. Das Skript liest nur `ConnectionStrings:DefaultConnection` und verbindet sich nur mit
  der WMS-DB.
- `SQL/108`, `SQL/110` und alle `SQL/Einmalig/*` laufen **nie scharf** durch das Skript. Höchstens Trockenlauf (nur bei Skripten mit
  Trockenlauf-Schalter, Literal-Prüfung siehe AK 17), sonst nur Anzeige mit fertigem Aufruf in der Nachlauf-Liste.
- Vor dem ersten DB-Kontakt und vor jedem schreibenden SQL eine ausdrückliche Bestätigung.

### Bedienung
- Ohne Parameter fragt das Skript zuerst die **Firma** (`AKE` | `IDEAL`), dann die **Systeme** als nummerierte Liste aus
  `ziele.psd1`, nur die der gewählten Firma (AKE: `Prod`, `Test`, `Test-IDEAL`; IDEAL: `Prod`, `Test`). Mehrfachauswahl ist möglich
  (z. B. `2,3`). `-Firma` und `-System <Name>[,<Name>]` überspringen die Abfragen.
- Reihenfolge bei mehreren Systemen: alle Nicht-Prod-Systeme zuerst, in Listenreihenfolge, jedes vollständig mit Nachlauf-Liste, Prod
  zuletzt. Schlägt ein System fehl, werden die folgenden nicht angefasst. Vor Prod muss der Mensch `<FIRMA>-PROD` eintippen
  (z. B. `AKE-PROD`); jede andere Eingabe beendet das Skript.

### Zielsysteme (`scripts\deploy\ziele.psd1`, Stand Gerald 2026-11-09/11)

| Firma | System | Prod? | Dienst-Ordner | Web-Ordner (IIS) | Dienst (Anzeigename, nur Info) |
|---|---|---|---|---|---|
| IDEAL | Prod | ja | `C:\IDEAL\IdealAkeWmsService` | `C:\inetpub\wwwroot\IDEALAKEWMS` | `IDEAL AKE WMS Service` |
| IDEAL | Test | nein | `C:\IDEAL\IdealAkeWmsService_Test` | `C:\inetpub\wwwroot\IDEALAKEWMS_Test` | `IDEAL AKE WMS Service TEST` |
| AKE | Prod | ja | `C:\AKE\IdealAkeWmsService` | `C:\inetpub\wwwroot\IDEALAKEWMS` | `IDEAL AKE WMS Service` |
| AKE | Test | nein | `C:\AKE\IdealAkeWmsService_Test` | `C:\inetpub\wwwroot\IDEALAKEWMS_Test` | `IDEAL AKE WMS Service TEST` |
| AKE | Test-IDEAL | nein | `C:\AKE\IDEALAKEWMSService-TestIDEAL` | `C:\inetpub\wwwroot\IdealAkeWMS-TESTIDEAL` | `IDEAL AKE WMS IDEAL-Test Service` |

- Server: IDEAL auf idealweb01, AKE auf akenet01 (alle drei Systeme, Freigabe-Antwort 7).
- `Test-IDEAL` ist eine eigene Installation auf akenet01 für den Bündel-Stand, Adresse `https://akenet01.ake.at:4445/` (Gerald 2026-11-11). Zum Vergleich AKE-Test: `https://akenet01.ake.at:4444/`. Das Skript nimmt die URL aus der IIS-Bindung; stimmt sie nicht mit `ziele.psd1` (Feld `Url`, optional) überein, warnt es. Der Manifest-Filter läuft über die
  Firma AKE. Ihre DB liest das Skript wie bei allen Systemen aus der `appsettings` des Ordners.
- Format je Eintrag: `@{ Firma='AKE'; System='Test-IDEAL'; Prod=$false; WebPfad='...'; DienstPfad='...' }`. Ein neues System ist eine
  Zeile in dieser Datei; das Skript bleibt unverändert. Doppelte Firma+System oder doppelte Pfade → Abbruch beim Laden.
- Die Ordner sind nicht Groß/Klein-empfindlich zu vergleichen (Windows).
- Das Skript verlässt sich auf keinen Dienstnamen, nur auf den Exe-Pfad. README nennt abweichend `C:\Services\IDEALAKEWMSService` und
  `IDEALAKEWMSService` (Bündel-README Z. 751, 800-809); `IDEALAKEWMSService/Program.cs:23` setzt `ServiceName = "IDEAL AKE WMS Service"`.
  Fehlt ein Ordner → Abbruch.
- Abbruch, wenn zwei gewählte Systeme dieselbe Server+DB zeigen (zusätzlich zu „Ermittlung“ Punkt 7).

`-WebPfad`/`-DienstPfad`/`-LogPfad` überschreiben die Tabelle (DEV-Test auf `D:\Dev\...`). Bei mehr als einem gewählten System sind
Pfadparameter unzulässig (Abbruch), weil sie nicht für alle gelten können.

## Technischer Lösungsentwurf

Vorbild ist `D:\Dev\scripts\dev-deploy.ps1` (nur DEV, außerhalb des Repos): Stop-All mit `WaitForStatus` und 2 s Pause für w3wp
(Z. 23-28), Abbruch mit Rückbau bei HTTP-Fehler (Z. 56), Fußzeilen-Regex `IDEAL-AKE WMS v([0-9.]+)` per `HttpClient` mit
`UseDefaultCredentials` (Z. 48-58, in PS 5.1 lauffähig). Neu gegenüber dem Vorbild: kein `dotnet publish` (das Paket kommt fertig),
kein Patchen der `web.config`, SQL-Skripte statt `Migrate()`, Ermittlung statt fester Namen.

### Paket und Parameter

```
powershell -ExecutionPolicy Bypass -File <Paket>\deploy\Update-IdealAkeWms.ps1
    [-Firma AKE|IDEAL] [-System <Name>[,<Name>]]   # Namen aus ziele.psd1, z. B. Test,Test-IDEAL
    [-Quelle <Paket>]                         # Default: $PSScriptRoot\..
    [-Komponente Web|Dienst|Beide]            # Default Beide; tauscht nur diese Dateien (Freigabe-Antwort 8)
    [-SqlOrdner <Pfad>]                       # Default: <Quelle>\SQL
    [-WebPfad <Pfad>] [-DienstPfad <Pfad>]    # nur bei einem System
    [-LogPfad <Pfad>]                         # Default: C:\<FIRMA>\deploy-logs
    [-SiteUrl <URL>] [-BackupPfad <Pfad auf dem SQL-Server>]
    [-NurPruefen]
```

Paket = `<worktree>\publish\` mit `IDEALAKEWMSWeb\`, `IDEALAKEWMSWebService\`, `SQL\` (Kopie von `SQL\`), `deploy\` (Kopie von
`scripts\deploy\`) und `publish-info.txt`. Format (neu, festgelegt hier; `qa-agent.md:95-96` nennt nur die Inhalte):

```
Version: 1.97.0
Commit: <HEAD, voll>
Branch: feature/2026-08-07-ideal-teile-1-5
Spec: <Slug oder Aufgabe>
Erstellt: yyyy-MM-dd HH:mm
Komponenten: IDEALAKEWMSWeb + IDEALAKEWMSWebService
```
Nachgezogen 2026-11-11 auf das Format, das der qa-agent seit v1.96.0 tatsaechlich schreibt (`<Worktree>\publish\publish-info.txt`, eine
Datei fuer beide Komponenten im Paket-Ordner). Das Skript liest `Schluessel: Wert` und toleriert zusaetzlich `Schluessel=Wert`.

Bei `-Komponente Web` wird der Dienst weder gestoppt noch getauscht, bei `Dienst` gilt das umgekehrt für das Web. Fehlt in diesem Lauf ein Schema-Skript, stoppt das Skript trotzdem beide. Schema, Ordner/Rechte und Nachlauf laufen immer. Das Skript liest `publish-info.txt` aus `<Quelle>`. Fehlt sie oder
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
7. **Abbruch**, wenn Web-DB ≠ Dienst-DB (Server+Datenbank), oder wenn zwei gewählte Systeme dieselbe Server+Datenbank zeigen.
8. **sqlcmd:** `Get-Command sqlcmd`, sonst der Pfad aus `dev-deploy.ps1:21`. Fehlt beides → Abbruch mit Installationshinweis
   (Gerald 2026-11-11: auf den Zielservern vermutlich nicht installiert).
   - Empfohlen ist die ODBC-Variante „Microsoft Command Line Utilities for SQL Server“; vorher den aktuellen „Microsoft ODBC Driver for
     SQL Server“ installieren. Gleiche Variante wie auf DEV.
   - Die Go-Variante (`winget install sqlcmd`) wird nur akzeptiert, wenn `sqlcmd -?` die Schalter `-E -b -i -C` ausweist.
   - Das Skript gibt die gefundene Variante und Version (`sqlcmd -?`, erste Zeile) im Protokoll aus.
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
| 5a | Ordner und Rechte | Prüfen und bei Bedarf herstellen laut Abschnitt „Ordner und Rechte“: fehlende Ordner anlegen, fehlende Rechte nach Bestätigung per `icacls` setzen, Ergebnis je Zeile (OK / angelegt / gesetzt / FEHLT) ins Protokoll. Ein Pflicht-Eintrag mit FEHLT → Abbruch vor dem Start (App-Pool und Dienst bleiben gestoppt). |
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

### Ordner und Rechte (Auftrag Gerald 2026-11-11)

Bisher stand das als Hand-Schritt in der Nachlauf-Liste (v1.30.4/v1.96.0: App_Data, v1.39.0: PDF-Temp, v1.49.0: Branding). Ab jetzt prüft das
Skript diese Punkte selbst, bei **jedem** Lauf, und stellt fehlende her. Installations- und Update-Skript nutzen dieselbe Prüfung (eigenes
Modul `scripts\deploy\IdealAkeWms.Deploy.psm1`, Funktion `Test-OrdnerUndRechte -Herstellen`). So prüfen beide gleich, und eine neue
Anforderung kommt nur an einer Stelle dazu.

**Identität ermitteln:**
- Web: `processModel.identityType` des ermittelten App-Pools.
  - `ApplicationPoolIdentity` → `IIS AppPool\<Pool>`
  - `SpecificUser` → `processModel.userName`
  - `NetworkService`/`LocalSystem` → Warnung
- Dienst: `Win32_Service.StartName`. `LocalSystem` → Warnung; ein virtuelles Konto `NT SERVICE\<Name>` wie auf DEV gilt als gültig.
- Beide Identitäten erscheinen in der Anzeige von „Ermittlung“ Punkt 10.

**Prüfliste.** Belege in Klammern; „Pflicht“ heißt Abbruch, wenn es nicht herstellbar ist.

| Ordner / Datei | Identität | Recht | Pflicht | Herstellen |
|---|---|---|---|---|
| Web-Ordner | App-Pool | Lesen+Ausführen | ja | nur prüfen |
| `<Web>\App_Data\Fotos\Kommissionierung` (v1.30.4/v1.96.0; ohne Schreibrecht startet die App nicht) | App-Pool | Ändern, vererbt `(OI)(CI)M` auf `App_Data` | ja | `New-Item`, `icacls` |
| `<Web>\logs` (stdout-Log, `web.config:13`) | App-Pool | Ändern | nein (Warnung) | `New-Item`, `icacls` |
| DataProtection-Schlüssel (Pfad aus `DataProtection:KeysPath` der Ziel-appsettings, sonst Install-Default `C:\ProgramData\IdealAkeWms\DataProtection-Keys`) | App-Pool | Ändern | ja, wenn der Schlüssel gesetzt ist | `New-Item`, `icacls` |
| `<Web>\Branding` (v1.49.0/v1.59.0) | App-Pool | Lesen | nein (Warnung: Ersatz-Kopf) | nur prüfen |
| `<Web>\appsettings.Production.json` (v1.71.0, SageLagerbuchung) | – | vorhanden | nein (Warnung) | nur prüfen |
| PDF-Temp `%TEMP%\IdealAkeWms-Pdf` der Pool-Identität (v1.39.0) und Microsoft Edge vorhanden | App-Pool | Ändern | nein (Warnung) | nur prüfen; Hinweis, wie es manuell geht |
| Dienst-Ordner | Dienst | Lesen+Ausführen | ja | nur prüfen |
| `<Dienst>\logs` (README „Log-Dateien“) | Dienst | Ändern | ja | `New-Item`, `icacls` |

**Regeln:**
- Die Prüfung läuft über die wirksamen Rechte: `Get-Acl` und Auflösung der Gruppenmitgliedschaft (direkte Einträge, Gruppen der Identität,
  `Users`/`Authenticated Users`, `IIS_IUSRS` für Pool-Identitäten). Unsicher → als FEHLT melden, nicht raten.
- Gesetzt wird nur **hinzufügend** (`icacls /grant`). Nie `/reset`, `/remove` oder `/inheritance:r`, nie Besitz ändern. Jede Änderung braucht
  eine Bestätigung und kommt mit der vorherigen ACL (`icacls <Ordner> /save` in den Log-Ordner) ins
  Protokoll.
- Mit `-NurPruefen`: nur Bericht, keine Änderung.
- **Betrieb (Gerald 2026-11-11):** Gerald führt Installations- und Update-Skript **von Hand in einer Administrator-PowerShell** aus. Es gibt
  keinen unbeaufsichtigten Modus und keinen Schalter, der Bestätigungen überspringt.
  - Beide Skripte beginnen mit `#Requires -RunAsAdministrator` und zusätzlich einer klaren Meldung, falls die Shell nicht erhöht ist.
  - Die Rechte-Änderungen eines Systems werden **gesammelt** angezeigt (Ordner, Identität, Recht) und mit **einer** Bestätigung gesetzt,
    nicht je Zeile.
- Ordner unter `App_Data`, `logs`, `Branding` und `wwwroot\Fotos` werden beim Kopieren nie überschrieben (Schritt 5); die Prüfung ändert
  keine Dateien darin.
- Neue Ordner- oder Rechte-Anforderungen kommen künftig über das Manifest. Das Feld `Typ = Ordner` mit Pfad, Identität, Recht und Pflicht
  ergänzt die Prüfliste, damit eine Spec mit neuer Ablage (z. B. QM-Fotos) nur einen Manifest-Eintrag braucht.

**Installationsskript:** `Install-IdealAkeWms-IDEAL.ps1` liegt bisher nicht im Repo
([[2026-11-04-ideal-produktiv-installation]]). Es kommt standortneutral als `scripts\deploy\Install-IdealAkeWms.ps1` (Abfrage Firma
wie beim Update) ins Repo und ruft nach dem Anlegen von Site und Dienst dieselbe Funktion `Test-OrdnerUndRechte -Herstellen` auf. Seine
bisherigen eigenen Rechte-Schritte (Ordnerrechte, „Als Dienst anmelden“) bleiben, soweit die Funktion sie nicht abdeckt.

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

1. Ohne `-Firma`/`-System` fragt das Skript beides ab. Die Systemliste zeigt nur die Systeme der Firma aus `ziele.psd1` (AKE 3, IDEAL 2). Ungültige Eingabe → erneute Frage. Mit beiden Parametern keine Abfrage.
2. Pfade kommen aus `ziele.psd1`; `-WebPfad`/`-DienstPfad`/`-LogPfad` überschreiben sie. Bei mehr als einem gewählten System führen
   Pfadparameter zum Abbruch vor jeder Aktion. Doppelte Einträge in `ziele.psd1` → Abbruch beim Laden.
3. Fehlt Web-Ordner, `IdealAkeWms.dll`, Dienst-Ordner oder `IDEALAKEWMSService.exe` → Abbruch mit Pfad, nichts gestoppt, keine DB
   kontaktiert.
4. Site/App-Pool werden über den physischen Pfad ermittelt; 0 oder >1 Treffer → Abbruch mit Trefferliste; geteilter App-Pool → Warnung
   mit Liste und Bestätigung.
5. Der Dienst wird über den Exe-Pfad ermittelt; 0 oder >1 Treffer → Abbruch.
6. DB Web aus `web.config`-Umgebung + `appsettings*.json`, DB Dienst aus Registry-/Maschinen-Umgebung + `appsettings*.json`; Web ≠ Dienst
   → Abbruch; zwei gewählte Systeme mit derselben Server+DB → Abbruch.
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
24. Mehrere Systeme: Nicht-Prod zuerst in Listenreihenfolge, Prod zuletzt; ein Fehler → alle folgenden unberührt; vor Prod nur mit Eingabe `<FIRMA>-PROD` weiter.
25. Manifest enthält alle Einträge der drei Tabellen oben; `NachlaufManifestDriftTests` grün; ein absichtlich entfernter Eintrag macht ihn
    rot.
26. `[System.Management.Automation.Language.Parser]::ParseFile` meldet für Skript und Manifest keine Fehler; beide Dateien nur ASCII.
27. Das Skript enthält keine Verbindung zu Sage (`SageConnection`, SData-URL) – Grep leer.
28. README beide Abschnitte ersetzt; `qa-agent.md`, `spec-agent.md`, Spec-Vorlage angepasst; CLAUDE.md-Vorschlag steht in dieser Spec.
29. `dotnet build IdealAkeWms.slnx` und `dotnet test` grün.
30. Schritt 5a läuft bei jedem System. Die Prüfliste „Ordner und Rechte“ steht vollständig im Protokoll (OK / angelegt / gesetzt / FEHLT).
    Ein fehlender Pflicht-Eintrag, der sich nicht herstellen lässt → Abbruch vor dem Start.
31. Auf DEV-IDEAL mit gelöschtem `App_Data\Fotos\Kommissionierung` und entzogenem Recht: Ohne `-NurPruefen` wird beides nach Bestätigung
    hergestellt, die App startet. Mit `-NurPruefen` steht nur FEHLT im Bericht, nichts wird geändert.
32. Rechte nur hinzufügend; die vorherige ACL liegt als `icacls /save` im Log-Ordner; kein `/reset`, `/remove`, `/inheritance:r` im Skript
    (Grep leer).
33. `scripts\deploy\Install-IdealAkeWms.ps1` im Repo, standortneutral, ruft `Test-OrdnerUndRechte`; Parse-Check ohne Fehler.

## Test-Szenarien

Neues Kapitel **TS-125 — Update-Skript je Zielsystem** in `docs/TESTSZENARIEN.md` (Nummer prüft der Dev-Lauf; TS-124 ist seit v1.96.0 der Foto-Hotfix),
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
| 125.1 | Aufruf oben | ermittelt Site, App-Pool, Dienst, Umgebung DEV, DB `.`; Bestätigung „ja“; Version, History, fehlende Skripte (erwartet keine), RCSI, Spuren; Nachlauf-Liste; Protokoll in `D:\Dev\logs\deploy`; nichts gestoppt (Dienst/App-Pool-Status vorher = nachher) | Agent |
| 125.2 | Wie 124.1 ohne `-Firma`/`-System` | Abfrage beider Werte; Eingabe `XYZ` → erneute Frage | Agent (Eingabe per Pipe) oder Mensch |
| 125.3 | Falsche Firma: `-Firma AKE -System Test` ohne Pfadparameter | `C:\AKE\IdealAkeWmsService_Test` fehlt → Abbruch, keine DB kontaktiert | Agent |
| 125.4 | Pfad fehlt: `-WebPfad D:\Dev\IDEAL\WebX` | Abbruch mit Pfad | Agent |
| 125.5 | `-System Test,Test-IDEAL` mit `-WebPfad` | Abbruch vor jeder Aktion | Agent |
| 125.6 | Bestätigung „Verbindung herstellen“ verneint | Ende ohne DB-Kontakt, Protokoll vorhanden | Agent |
| 125.7 | DB nicht erreichbar: Kopie des DEV-Web-Ordners mit `appsettings.DEV.json` auf Server `.\NICHTDA` (nur in einem Testordner, nie im echten DEV-Ordner) | Abbruch mit sqlcmd-Fehler, Exit ≠ 0 | Agent |
| 125.8 | Web-DB ≠ Dienst-DB (Testordner wie 124.7) | Abbruch vor DB-Kontakt | Agent |
| 125.9 | `publish-info.txt` mit abweichendem `Commit` im Dienst-Elternordner (Testkopie) | Abbruch | Agent |
| 125.10 | Einmal-Skript wird nicht ausgeführt: Nachlauf-Liste nennt alle 4 Einmal-Skripte mit Aufruf; Protokoll enthält keinen sqlcmd-Aufruf auf `SQL\Einmalig\*` außer Trockenlauf-Einträgen; Testkopie Altbestand mit `@Trockenlauf BIT = 0` → „abgelehnt, nur Anzeige“ | wie beschrieben | Agent |
| 125.11 | Version passt nach Start nicht (Volllauf auf DEV-IDEAL, `publish-info.txt` mit `Version=9.9.9`) | App-Pool wieder gestoppt, Dienst nicht gestartet, Exit ≠ 0; danach `dev-deploy.ps1 -Standort IDEAL` zur Wiederherstellung | **Mensch** (Rückfrage 9) |
| 125.12 | Volllauf DEV-IDEAL regulär | Schema: nichts fehlt; Kopieren schont `appsettings*.json`/`web.config`/`Branding`; Web, dann Dienst; Fußzeile v1.95.0; Nachlauf-Liste | **Mensch** |
| 125.13 | Drift-Test | `NachlaufManifestDriftTests` grün; Eintrag `SQL/141` entfernt → rot | Agent |
| 125.14 | PS 5.1 | `ParseFile` ohne Fehler unter `powershell.exe` (nicht `pwsh`) | Agent |

**Negativfall Zielsystem** (Mensch, erst nach Freigabe des Bündel-Deploys): Erster echter Lauf mit `-NurPruefen` je Zielsystem; Plan
mit der Backlog-Tabelle vergleichen (AKE: 89–107, 109, 112, 113, 133, 140, 141 fehlen; IDEAL-Prod: 98–141 ohne 108/110 fehlen).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | kein Epic | – | – |

## Deploy

**Final (QA 2026-11-11, aus dem echten Diff `9db618c0..550f10d5`):**
- **Web-App:** ja (nur `AppVersion.cs` 1.98.0 + Changelog-Karte) · **Service:** ja (nur `AppVersion.cs` 1.98.0) · **Migration:** nein
  (keine Datei unter `*/Migrations/`, kein neues SQL). Der eigentliche Inhalt sind die Deploy-Skripte im Paket (`deploy\`).
- **Paket:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` = v1.98.0, Commit `550f10d5`
  (`IDEALAKEWMSWeb\`, `IDEALAKEWMSWebService\`, `SQL\`, `deploy\`, `publish-info.txt` im neuen Format).
- Ablauf Mensch: Paket auf den Zielserver kopieren (z. B. `C:\<FIRMA>\deploy\v1.98.0\`) -> je System zuerst `deploy\Update-IdealAkeWms.ps1
  -NurPruefen`, Plan prüfen -> Volllauf (Backup, Stoppen, Schema, Haltepunkt, Kopieren, Ordner/Rechte, Start Web vor Dienst, Nachlauf-Liste)
  -> testen -> Merge. Reihenfolge bei Schema/Einmal-Skripten steuert das Skript (Backup vor Stoppen; Schema; Haltepunkt Altbestand; Start;
  danach SQL/110-Trockenlauf und Hand-Schritte laut Nachlauf-Liste).
- Nach dem Merge nur dann neu aus `main` publishen, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat:
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService` (plus `SQL\`, `deploy\`,
  `publish-info.txt` laut `qa-agent.md` Punkt 7).

**Final Nachtrag v1.98.1 (QA 2026-11-11, aus dem echten Diff `550f10d5..e1ecc0c0`):**
- **Web-App:** ja (nur `AppVersion.cs` 1.98.1 + Changelog-Karte) · **Service:** ja (nur `AppVersion.cs` 1.98.1) · **Migration:** nein
  (keine Datei unter `*/Migrations/`, kein SQL). Inhalt: `deploy\Update-IdealAkeWms.ps1`, `deploy\IdealAkeWms.Deploy.psm1`, `deploy\ziele.psd1`.
- **Paket:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` = **v1.98.1, Commit `e1ecc0c0`** (ersetzt das
  v1.98.0-Paket; `IDEALAKEWMSWeb\`, `IDEALAKEWMSWebService\`, `SQL\`, `deploy\`, `publish-info.txt`).
- Ablauf unverändert wie oben (Paket nach `C:\<FIRMA>\deploy\v1.98.1\`, je System zuerst `-NurPruefen`). Kein neuer Manifest-Eintrag nötig
  (kein SQL, keine Einstellung, kein Hand-Schritt).
- Nach dem Merge nur dann neu aus `main` publishen, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.

**Vorläufig** (Spec-Agent, überholt durch „Final“ oben):
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
9. **Volllauf auf DEV:** Der Auftrag erlaubt dem Agenten nur `-NurPruefen`. TS-125.11/.12 brauchen einen echten Start. **Empfehlung:**
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

## Nachtrag 2026-11-11 (Vorwärts-Merge main → Bündel, v1.96.0)

Quelle: [[2026-11-10-vorwaerts-merge-main-ins-buendel]], Auftrag Gerald 2026-11-11. Antwort- und Rückfrageabschnitte bleiben unverändert.
- Der Hotfix v1.30.4 (Kommissionierfotos in `App_Data`) ist seit Merge-Commit `02a0a173` im Bündel (zusammen mit v1.30.2 Zahnrad und
  v1.30.3 OSEON/Druck/Werkbank). Bündelversion jetzt **v1.96.0**. Rückfrage 5 ist damit durch den Auftrag erledigt; die Antwortzeile
  füllt Gerald.
- Testszenarien: Kapitel **TS-124 = Kommissionierfotos** (aus main). Diese Spec nutzt deshalb **TS-125** (alle Verweise umgestellt).
- **Manifest-Eintrag (Typ Vorher, Firma beide, ab v1.96.0)** – vor dem Kopieren/Start der neuen Web-Version je Site:
  1. `mkdir "<SiteRoot>\App_Data\Fotos\Kommissionierung"` und `icacls "<SiteRoot>\App_Data" /grant "IIS AppPool\<PoolName>:(OI)(CI)M"`
     (Identität aus dem ermittelten App-Pool; ohne Schreibrecht startet die App nicht).
  2. Dateien in `<SiteRoot>\wwwroot\Fotos\Kommissionierung` zählen und im Protokoll festhalten.
  3. Nach dem Web-Start im Log „Kommissionierfotos verschoben: n, uebersprungen: m“ suchen; n + m muss der Zählung entsprechen. m > 0:
     Warnung je Datei, Rest-Dateien vergleicht der Mensch und entfernt sie von Hand (Nachlauf-Liste, Typ Info).
  Hat ein System v1.30.4 schon (Ordner `App_Data\Fotos\Kommissionierung` vorhanden, Recht gesetzt), entfallen 1 und die Verschiebung
  (n = 0). Das Kopieren schont `App_Data\` ohnehin (Ablauf Schritt 5).
- Rücksprung auf eine Bündelversion < v1.96.0 ist nach diesem Deploy nicht mehr fotofest (alte Versionen lesen nur `wwwroot`); Rückbau
  wie im Deploy-Abschnitt von [[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-spec]].

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Marker-Tabelle Einmal-Skripte (ADR-0015-Nachtrag) ja/nein → **Ja, aber als eigene Folge-Spec nach dem Bündel-Deploy. Bis dahin „unbekannt, Deploy-Protokoll prüfen“.** (Gerald 2026-11-11)
2. web.config am Ziel nicht überschreiben, Unterschied melden → **Ja: nie überschreiben, nur warnen.**
3. Haltepunkt Altbestand ohne „überspringen“ → **Ja, kein Überspringen. Das Skript wartet, bis das Einmal-Skript ausgeführt ist (Trockenlauf, Freigabe, scharf); sonst Abbruch bei gestopptem Web. Gilt auch für IDEAL, dort mit 0 Treffern.**
4. Stand der Einmal-Skripte je System (Hotfix-1.30.1, v1.47.4, 00er, v1.46.0-Rücksetzen; IDEAL-Prod entfallen?) → **Gerald 2026-11-11: auf keinem System gelaufen. Daraus folgt (Koordination, zur Bestätigung durch Gerald):**
   - **Hotfix-1.30.1 entfällt überall und kommt nicht ins Manifest.** Das Skript setzt Ist-Mengen 0 offener Lagerbestellungen auf NULL. Seit v1.30.1 ist eine getippte 0 eine bewusste Bestätigung (ADR 0015, Kontext), deshalb würde ein Lauf heute echte Eingaben löschen.
   - **v1.47.4 (Vater-Arbeitsgänge) nur IDEAL-Test**, mit dem Bündel-Deploy: nach dem Dienst-Deploy laut Skriptkopf; die Meldeliste geht an den Fachbereich. Bei IDEAL-Prod entfällt es, weil v1.71.1 neu installiert wurde.
   - **00er-Bereinigung nur IDEAL-Test**, und nur wenn P7 Treffer zeigt: zuerst `@Loeschen = 0` (Liste), dann entscheidet Gerald. Bei IDEAL-Prod entfällt sie.
   - **v1.46.0 Sub-FA-Rücksetzen entfällt.** Das war nur eine Testvorbereitung am Testsystem, das SQL steht nur in der Spec.
   - Das Manifest trägt für diese Einträge eine feste Zielzuordnung statt „unbekannt“.
5. v1.30.4 vor dem Bündel-Deploy ins Bündel holen → **Erledigt: v1.96.0, Merge-Commit `02a0a173` (2026-11-11).**
6. Rolle `beschichtungsauftrag` (Lesen) an AKE anlegen → **Ja, beide Rollen anlegen (`beschichtungsauftrag` und `beschichtungsauftrag_schreiben`).**
7. Zielsysteme/DB/sqlcmd/Rechte bestätigt → **AKE-Prod und AKE-Test laufen beide auf akenet01, Ordner wie in der Tabelle „Zielsysteme“. sqlcmd und SQL-Rechte noch ungeprüft: Gerald prüft vor dem ersten Lauf (`Get-Command sqlcmd` und die Rechte-Abfrage aus Rückfrage 7).**
8. `-PublishWeb`/`-PublishDienst` = Quellpfade oder Schalter → **Schalter. Ein Parameter `-Komponente Web|Dienst|Beide` (Default `Beide`) wählt, was getauscht wird. Bei „nur Web“ bleibt der Dienst unberührt und umgekehrt. Schema-, Ordner- und Rechte-Schritte laufen trotzdem. Den Quellpfad gibt `-Quelle` (Default: Paket-Ordner neben dem Skript). `-PublishWeb`/`-PublishDienst` entfallen.**
9. Volllauf DEV (TS-125.11/.12) durch Gerald → **Nein: Der Agent fährt den Volllauf auf DEV (AKE und IDEAL) selbst. Danach `dev-deploy.ps1` zur Wiederherstellung und die 5 AKE-DEV-Sync-Schalter wieder setzen. Zielsysteme (akenet01, idealweb01, AKESQL20) bleiben tabu.**
10. Agenten-/Vorlagen-Änderungen im Hauptcheckout, CLAUDE.md nur Vorschlag → **Ja.**

## Umsetzung (Dev-Lauf 2026-11-11)

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Commits `1f3d2ce4` (feature-complete) und `1c689a66` (Korrekturen aus den
DEV-Läufen). Version **1.98.0** laut Auftrag (Web + Dienst, Changelog-Karte „Update-Skript je Zielsystem (Betrieb)“). Aufgabe
[[2026-11-11-buendel-deploy-update-skript]].

**Geliefert:** `scripts/deploy/Update-IdealAkeWms.ps1`, `nachlauf.psd1` (72 Einträge: 25 Schema, Altbestand als Haltepunkt, SQL/108, SQL/110,
v1.47.4 und 00er nur `System = Test` der Firma IDEAL, 1 Ordner-Eintrag v1.96.0, Vorher-/Nachher-Einträge inkl. v1.96.0 und v1.97.0),
`ziele.psd1` (5 Systeme, `Url` für AKE-Test 4444 und Test-IDEAL 4445), `IdealAkeWms.Deploy.psm1` (`Test-OrdnerUndRechte`, `Read-Ja`,
`Import-Ziele`), `Install-IdealAkeWms.ps1` (standortneutral, ASCII, ruft `Test-OrdnerUndRechte -Herstellen` als Schritt 5b),
`IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs` (8 Tests), README (beide Update-Abschnitte), `docs/TESTSZENARIEN.md` Kapitel TS-125.
Hauptcheckout: `.claude/agents/qa-agent.md` Punkt 7 (Paket mit `SQL\` + `deploy\`, publish-info-Format, Manifest-Prüfung),
`.claude/agents/spec-agent.md` (Manifest-Pflicht im DEPLOY SECTION), `_templates/spec.md` (Zeile Manifest-Einträge), ADR
[[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]], [[fallstricke]] §30, feature-map, testszenarien-index, Changelog
[[2026-11-11-v1-98-0-update-skript-je-zielsystem]]. **CLAUDE.md** unverändert; Vorschlag für die Checkliste steht oben unter
„Pflicht Manifest nachtragen“.

**Entscheidungen im Dev-Lauf (im Rahmen der Spec, zur Kenntnis):**
- Doppelte Pfade in `ziele.psd1` werden **je Firma** geprüft: IDEAL-Prod und AKE-Prod haben denselben Web-Pfad auf verschiedenen Servern.
- Prüfliste „appsettings.Production.json“ prüft `appsettings.<Umgebung der web.config>.json` (auf DEV `appsettings.DEV.json`); am
  Zielsystem ist das `Production`.
- Lesende Abfragen (History, RCSI, Spuren, Prüfabfragen, sysjobs) laufen über `System.Data.SqlClient` mit der Verbindung aus der
  appsettings; Skripte (Schema, Trockenlauf) über `sqlcmd -b -I -f 65001`. Ein nicht erreichbarer Server bricht daher mit dem
  SqlClient-Fehler ab (TS-125.7), nicht mit einem sqlcmd-Fehler.
- Ein abgelehntes Schema-Skript bricht sofort ab (Web und Dienst bleiben gestoppt) statt die übrigen weiterlaufen zu lassen – gleiche
  Wirkung wie AK 16, ohne Folgeskripte auf unvollständigem Stand.
- Haltepunkt nur, wenn die installierte Version < 1.88.0 oder unbekannt ist **und** die Spur 0 zeigt; sonst liefe er bei jedem Deploy erneut
  (an IDEAL bleibt die Spur nach dem Lauf 0, Freigabe-Antwort 3).
- Kein Assembly-Versionsvergleich der DLLs (die Projekte setzen keine `<Version>`); Nachweis ist Hash-Gleichheit + Fußzeile.
- `Install-IdealAkeWms.ps1`: Schalter `-OhneAbfrage` entfernt (kein unbeaufsichtigter Modus), SQL-Dateien `<FIRMA>-Prod-1/2-*.sql`,
  Foto-Ordner jetzt `App_Data` über die Funktion (vorher `wwwroot\Fotos`).

**Nachweis Build/Test** (Worktree, 2026-11-11): `dotnet build IdealAkeWms.slnx` 0 Fehler; `dotnet test`: Web **2601/2602** bestanden
(1 übersprungen, wie bisher), Dienst **512/512**; `NachlaufManifestDriftTests` 8/8. `ParseFile` unter `powershell.exe` 5.1: 0 Fehler für alle
5 Dateien in `scripts/deploy`; alle ASCII (Drift-Test).

**Nachweis DEV** (Paket `<Worktree>\publish\` v1.98.0, Protokolle `D:\Dev\logs\deploy\`):

| TS | Ergebnis |
|---|---|
| 125.1 | `-NurPruefen` IDEAL: Site/Pool `IDEAL_WMS_DEV`, Dienst `IDEAL_WMS_DEV_Service`, Umgebung DEV/DEV, DB `./IDEAL_WMS_DEV`, Fußzeile v1.97.0, fehlend keine, RCSI 0 (Warnung), Spuren, Ordner/Rechte-Bericht, Nachlauf (3 Einmal-Einträge), Exit 0, Pool/Dienst vorher = nachher |
| 125.2 | `XYZ` → „Ungueltig“, Systemliste IDEAL 2 Einträge, `9` → „Ungueltig“, `Test` angenommen |
| 125.3 | `-Firma AKE -System Test` → `ABBRUCH: C:\inetpub\wwwroot\IDEALAKEWMS_Test fehlt (AKE-Test).` Exit 1 |
| 125.4 | `ABBRUCH: D:\Dev\IDEAL\WebX fehlt` Exit 1 |
| 125.5 | `ABBRUCH: -WebPfad/-DienstPfad/-SiteUrl gelten nur fuer EIN System.` Exit 1 |
| 125.6 | „Ende ohne DB-Kontakt.“, Protokoll vorhanden, Exit 2 |
| 125.7 | Test-Anwendung + nie gestarteter Testdienst auf `D:\Dev\deploytest` (danach entfernt): `ABBRUCH: DB .\NICHTDA/IDEAL_WMS_DEV nicht lesbar: … error: 26`, Exit 1 |
| 125.8 | `ABBRUCH: Web-DB ./IDEAL_WMS_ANDERS ist nicht die Dienst-DB ./IDEAL_WMS_DEV.` vor DB-Kontakt |
| 125.9 | Dienst-`publish-info.txt` mit `Commit: 0000000` → Abbruch „passt nicht zu“; ohne `publish-info.txt` → Abbruch |
| 125.10 | Nachlauf nennt Altbestand, v1.47.4, 00er (Hotfix-1.30.1 nicht); Haltepunkt mit Testkopie `@Trockenlauf BIT = 0` → „abgelehnt, nur Anzeige“, Antwort ≠ `ausgefuehrt` → `ABBRUCH … Web und Dienst bleiben gestoppt` (Pool/Dienst Stopped); mit echter Datei Trockenlauf ausgeführt („TROCKENLAUF: nichts geaendert“, Ausgabe-Datei im Log-Ordner); kein `sqlcmd` auf `Einmalig` außer Trockenlauf |
| 125.11 | Paketkopie `Version: 9.9.9`: `ABBRUCH: Fusszeile v1.98.0 statt v9.9.9. App-Pool gestoppt, Dienst nicht gestartet.` Exit 1 |
| 125.12 | Volllauf IDEAL (Backup echt: `IDEAL_WMS_DEV_20261111_1047_vor_v1.98.0.bak` im SQL-Standard-Backup-Ordner) und AKE (Backup abgelehnt + „liegt vor“): Stoppen, Schema „nichts fehlt“, web.config-Warnung mit beiden Fassungen (Ziel DEV bleibt), robocopy 334/93 Dateien, Schutzprüfung gleich, Ordner/Rechte OK, Fußzeile v1.98.0, Dienst läuft, Exit 0 |
| 125.13 | Drift-Test grün; `Manifest_EntfernterEintrag_MachtWaechterRot` entfernt `SQL/141` und erwartet beide Fehlmeldungen |
| 125.14 | `ParseFile` unter `powershell.exe` ohne Fehler |
| 125.15 (AK 31) | IDEAL: Fotos-Ordner umbenannt + an `App_Data` Vererbung testweise gekappt → `-NurPruefen` „FEHLT“, nichts geändert; Volllauf → eine Bestätigung, `vorherige ACL: …acl_20261111_104908_v1.96.0-FotosOrdner.txt`, „gesetzt“ bzw. im ersten Lauf „angelegt“, App startet. Testzustand danach zurückgesetzt (`icacls … /reset /T`, Testfoto zurück) |

Danach `dev-deploy.ps1 -Standort IDEAL` und `-Standort AKE -Quelle <Bündel-Worktree>` (beide v1.98.0) und die 5 AKE-DEV-Sync-Schalter
auf `true` (geprüft). Testartefakt: Backup-Datei `C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup\IDEAL_WMS_DEV_20261111_1047_vor_v1.98.0.bak` (≈ 4,5 GB) liegt noch – löschen nach Belieben.

**Befunde/Zusatzfunde (nicht mitgebaut):**
- DEV-IDEAL hat RCSI = 0 (Warnung des Skripts); an IDEAL-Prod/Test vor dem Deploy prüfen.
- `Branding\` fehlt auf DEV (Warnung, Ersatz-Kopf) – erwartbar.
- Install-Skript wurde nicht ausgeführt (nur Parse-Check, AK 33); ein echter Lauf braucht eine leere Maschine.

## QA-Nachweis (qa-agent, 2026-11-11)

Geprüft: Commits `1f3d2ce4`, `1c689a66` (Diff `9db618c0..1c689a66`) im Bündel-Worktree; QA-Fix `550f10d5`. Review selbst in diesem Lauf
durchgeführt (kein Unteragent).

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler (28 Warnungen, bestehend).
- `dotnet test`: **IdealAkeWms.Tests 2601 bestanden / 1 übersprungen / 0 Fehler (2602)**; **IDEALAKEWMSService.Tests 512 / 0 / 0 (512)**.
- Nach dem QA-Fix (nur `.ps1`, kein C#): `NachlaufManifestDriftTests` 8/8; `ParseFile` unter `powershell.exe` 5.1.26100: 0 Fehler in allen
  5 Dateien von `scripts/deploy`; Grep auf Nicht-ASCII leer; Grep `/reset|/remove|/inheritance:r|SageConnection|SData` in Update-Skript
  und Modul leer.

### 2. Code-Review (Befunde und Behandlung)
| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| F1 | Mittel | Schritt 2 Backup: `Join-Path <InstanceDefaultBackupPath> ...` wirft unter PS 5.1, wenn das Laufwerk des **SQL-Servers** am Web-Server fehlt (gemessen: `Join-Path 'Q:\MSSQL\Backup' 'a.bak'` -> „Laufwerk Q nicht vorhanden“). An AKE (AKESQL20) und IDEAL (eigener SQL-Server) wahrscheinlich; der Lauf bräche mit Rohfehler ab (vor dem Stoppen, nichts geändert, aber kein Backup). | **Behoben** `550f10d5` (String statt `Join-Path`). DEV: `-BackupPfad Q:\NichtDa` -> `ABBRUCH: Backup fehlgeschlagen: … Cannot open backup device … Operating system error 3`, Exit 1, Pool/Dienst danach Started/Running (zugleich AK 11 Negativfall). |
| F2 | Niedrig | `Invoke-SqlDatei`: unter `ErrorActionPreference=Stop` wirft PS 5.1 bei stderr von sqlcmd (Verbindungsfehler) sofort (gemessen mit `-S .\NICHTDA`); SQL-Fehler (Msg …) gehen auf stdout und waren korrekt. Folge: Abbruch ohne „ABBRUCH … bleiben gestoppt“-Meldung, Ausgabe unvollständig. | **Behoben** `550f10d5` (lokal `Continue`, Exit-Code entscheidet). |
| F3 | Mittel | Schritt 6 ruft die URL aus der IIS-Bindung ab; https-Bindung ohne Host-Header -> `https://localhost:<Port>` -> Zertifikatsname passt nicht -> Ausnahme -> **App-Pool gestoppt nach Kopieren und Schema** (spec-konform „HTTP-Fehler -> Abbruch“, aber erst spät sichtbar). Betrifft vermutlich AKE-Test (4444) und Test-IDEAL (4445). Die Vorprüfung schwieg bei HTTP-Status ≠ 2xx. | **Teilweise behoben** `550f10d5`: Vorprüfung warnt jetzt bei nicht lesbarer Startseite (auch `-NurPruefen`). DEV mit `-SiteUrl https://localhost:8082/`: `!! Startseite … nicht lesbar: Fehler bei Handshake … Schritt 6 bricht dann nach dem Kopieren ab … ggf. -SiteUrl`. **Offen (Checkliste 2).** Folgeidee (nicht gebaut): bei Bindung ohne Host-Header die `Url` aus `ziele.psd1` nehmen. |
| F4 | Niedrig | Abbrüche in der Ermittlung (vor jeder Aktion: Ordner fehlt, Pfadparameter bei mehreren Systemen, Web-DB ≠ Dienst-DB) schreiben kein Protokoll; das Transcript startet erst je System. | Offen, nicht gebaut (nichts geändert zu dem Zeitpunkt; Konsole zeigt den Grund). |
| F5 | Info | `-LogPfad` ist bei mehreren Systemen erlaubt (Spec: alle Pfadparameter unzulässig). Sinnvoll, Protokollnamen enthalten das System. | Akzeptiert, nicht geändert. |
| F6 | Niedrig | `deploy-info.txt` wird in Schritt 5 geschrieben (vor der Versionsprüfung in 6) und von `dev-deploy.ps1`/Hand-Updates nicht erneuert -> veralteter Fallback, wenn die Fußzeile nicht lesbar ist. Gemessen: DEV-IDEAL hatte nach TS-125.11 `Version: 9.9.9` darin („Installiert: v9.9.9 (Quelle: deploy-info.txt)“). Die Quelle wird angezeigt. | Offen; DEV-Artefakt durch QA-Volllauf überschrieben (jetzt 1.98.0/`550f10d5`). |
| F7 | Niedrig | `Stop-Service` wartet in PS 5.1 selbst unbegrenzt; der 60-s-Timeout (AK 12) greift erst danach. Ein hängender Dienst blockiert das Skript, Abbruch nur per Strg+C. | Offen, nicht gebaut. |
| F8 | Info | `Install-IdealAkeWms.ps1`: Warnungen aus `Test-OrdnerUndRechte` landen in der Modul-Liste, nicht in der Abschluss-Liste „Warnungen“ des Installers (inline sichtbar). | Offen, nicht gebaut. |

Geprüft ohne Befund: Never-scharf-Regel (einziger `Invoke-SqlDatei`-Weg außer Schema ist `Invoke-Trockenlauf` mit Literal-Prüfung und
Ablehnung von `= 0`; SQL/108, v1.47.4, 00er nur `anzeigen`; Grep aller DEV-Protokolle 2026-11-11: einziger sqlcmd auf `Einmalig\` =
Altbestand-Trockenlauf); Rechte nur `icacls /grant` + `/save`; kein Sage-Zugriff (nur `ConnectionStrings:DefaultConnection`, Grep + Test);
Stop/Start/Abbruch-Pfade (Schema-, Haltepunkt-, 5a-Abbruch: beide gestoppt; Schritt-6-Webfehler: Pool gestoppt, Dienst nicht gestartet);
PS-5.1-Kompatibilität (StrictMode 2, `Import-PowerShellDataFile`, Member-Enumeration auf Hashtable-Arrays gemessen, WebAdministration-Objekte
im DEV-Lauf); Manifest gegen Spec-Tabellen und Freigabe-Antwort 4 (25 Schema in Deploy-Reihenfolge; Hotfix-1.30.1 bewusst nicht enthalten;
v1.47.4 + 00er nur `Firma IDEAL, System Test`; kein v1.46.0-Eintrag; v1.51.0 nennt beide Rollen; v1.96.0 Ordner/Zählen/Rest/Dienst und
v1.97.0 vorhanden; 72 Einträge). Fehlerklassen der QA-Liste (Script-Einbettung, `hidden`+`d-*`, stille Fallbacks, Parallel-Arrays,
Browser-only-Validierung): nicht berührt; „eine Regel an mehreren Orten“: Ordner/Rechte nur im Modul, Trockenlauf-Literal in Skript und
Drift-Test gleich.

### 3. Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1 | DEV (TS-125.2) + Code |
| 2 | DEV (125.5); doppelte `ziele.psd1`-Einträge nur Code-Review (`Import-Ziele`); `-LogPfad` siehe F5 |
| 3 | DEV (125.3, 125.4) |
| 4 | DEV positiv (genau 1 Treffer); 0/>1 Treffer und geteilter Pool nur Code-Review |
| 5 | DEV positiv; 0/>1 nur Code-Review |
| 6 | DEV (125.8); zwei Systeme mit gleicher DB nur Code-Review |
| 7 | DEV (125.9) |
| 8 | DEV (125.6) |
| 9 | DEV (125.1, QA-Wiederholung aus dem Paket) |
| 10 | DEV (125.1 QA: Pool/Dienst vorher = nachher Started/Running, Exit 0) |
| 11 | DEV (QA: Backup-Fehler -> Abbruch vor dem Stoppen; echtes Backup im Dev-Lauf) |
| 12 | DEV positiv; Timeout nur Code-Review (F7) |
| 13 | **Manual-UAT** (auf DEV fehlt kein Schema-Skript) – erster Volllauf AKE-Test |
| 14 | **Manual-UAT** (AKE-Test: SQL/90 fehlt dort) + Code-Review |
| 15 | **Manual-UAT** + Code-Review |
| 16 | Code-Review (auf DEV ohne Schema-Lücke nicht provozierbar) |
| 17 | Test (`Manifest_DatenUndEinmalSkripte_NieAutomatisch`) + DEV (125.10) + Log-Grep |
| 18 | DEV (125.12 und QA-Volllauf: Schutzstand gleich, `web.config` DEV bleibt); robocopy ≥ 8 nur Code-Review |
| 19 | DEV (125.11, 125.12) |
| 20 | DEV positiv; Negativfall nur Code-Review |
| 21 | DEV (125.10) |
| 22 | DEV (125.1: installiert 1.98.0 -> nur Einmal-Einträge mit Spur); Versionsfilter mit älterem Stand: Dev-Lauf AKE 125.12 + Code |
| 23 | DEV (Protokolle auch bei Abbruch: 125.6, QA-Backup-Abbruch); Ermittlungs-Abbrüche ohne Protokoll (F4) |
| 24 | Code-Review; **Manual-UAT** beim ersten Mehrsystem-Lauf |
| 25 | Test (8/8, inkl. `Manifest_EntfernterEintrag_MachtWaechterRot`) |
| 26 | QA: `ParseFile` 0 Fehler unter `powershell.exe` 5.1 + ASCII-Test |
| 27 | Test + Grep |
| 28 | Review: README beide Abschnitte, `qa-agent.md`/`spec-agent.md`/Vorlage je mit `nachlauf.psd1`, CLAUDE.md-Vorschlag in dieser Spec |
| 29 | QA Build/Test oben |
| 30 | DEV (QA-Volllauf: vollständige Liste OK/WARNUNG im Protokoll) |
| 31 | DEV (125.15 Dev-Lauf) |
| 32 | Test + Grep; `icacls /save` belegt in 125.15 |
| 33 | Parse + Review; Ausführung **Manual-UAT** (braucht leere Maschine) |

### 4. Geänderte bestehende Tests
Keine. Einzige Testdatei im Diff ist neu (`IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs`).

### 5. CLAUDE.md-Checkliste / ADR 0020
Keine Migration, kein SQL, kein FreshInstall-Bedarf, keine Audit-Felder. Version 1.98.0 in beiden `AppVersion.cs`, Anwender-Changelog
(Karte v1.98.0), Brain-Changelog [[2026-11-11-v1-98-0-update-skript-je-zielsystem]], ADR [[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]],
[[fallstricke]] §30, TS-125 in `docs/TESTSZENARIEN.md` und im Testindex. ADR 0020: keine neue oder geänderte Einstellung.

### 6. DEV-Deploy und Testlauf
- `standort: beide`. `dev-deploy.ps1 -Standort IDEAL` und `-Standort AKE -Quelle <Bündel-Worktree>` (erhöht, Exit 0): beide **HTTP 200,
  Fußzeile v1.98.0**. Danach die 5 AKE-DEV-Sync-Schalter auf `true` gesetzt und geprüft.
- Update-Skript aus dem Paket (`publish\deploy\`, v1.98.0/`550f10d5`) gegen DEV-IDEAL:

| Schritt | Ergebnis | Status |
|---|---|---|
| 125.1 `-NurPruefen` | Site/Pool/Dienst/DB `./IDEAL_WMS_DEV` ermittelt, Fußzeile v1.98.0, fehlend keine, RCSI-Warnung, Spuren, Ordner/Rechte-Bericht, Nachlauf 3 Einmal-Einträge, Exit 0, Pool/Dienst vorher = nachher | OK |
| F3 `-SiteUrl https://localhost:8082/ -NurPruefen` | Warnung „Startseite … nicht lesbar … Schritt 6 bricht dann … ab“, Exit 0 | OK |
| AK 11 `-BackupPfad Q:\NichtDa` | Abbruch vor dem Stoppen, Exit 1, Pool/Dienst laufen | OK |
| 125.12 Volllauf (Backup abgelehnt, „liegt vor“ j) | Stoppen, Schema nichts fehlt, web.config-Warnung (Ziel DEV bleibt), robocopy, Schutzstand gleich, Ordner/Rechte OK (Branding WARNUNG), Fußzeile v1.98.0, Dienst läuft, Exit 0 | OK |
| 125.2–125.11, 125.13–125.15 | aus dem Dev-Lauf übernommen (Tabelle oben), nicht wiederholt | OK (Dev-Lauf) |
| 125.16 erster `-NurPruefen` je Zielsystem | Zielsysteme für Agenten tabu | NICHT MOEGLICH |

- Danach DEV-IDEAL mit `dev-deploy.ps1 -Standort IDEAL` wiederhergestellt (Exit 0, v1.98.0).
- **Stand DEV am Ende:** AKE und IDEAL v1.98.0, App-Pools `AKE_WMS_DEV`/`IDEAL_WMS_DEV` Started, Dienste Running, AKE-Sync-Schalter
  `Sync:ArticlesEnabled`, `…ArtikelLieferantEnabled`, `…KreditorenEnabled`, `…DebitorenEnabled`, `…EinkaufBestellungenEnabled` = true.
- Browser-Schritte: keine (keine Oberflächenänderung außer der Changelog-Karte, siehe Checkliste 6).

### 7. Publish
`<Worktree>\publish\` neu erzeugt (Zielordner vorher geleert): `IDEALAKEWMSWeb\`, `IDEALAKEWMSWebService\`, `SQL\`, `deploy\`,
`publish-info.txt` (`Version: 1.98.0`, `Commit: 550f10d5bca66b4d1a41ed650546ec8ea0debec7`, `Branch: feature/2026-08-07-ideal-teile-1-5`,
`Spec: 2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec`, `Erstellt: 2026-11-11 11:00`, `Komponenten: …`). Genau dieses Paket hat
das Update-Skript auf DEV-IDEAL eingespielt (125.12 oben).

### Manuelle Test-Checkliste (Mensch, Schranke 2)
1. Vor dem ersten Lauf je Server: `Get-Command sqlcmd` und die Rechte-Abfrage aus Rückfrage 7 (db_owner, BACKUP, msdb-Leserecht).
2. Je Zielsystem zuerst `-NurPruefen` (AKE-Test, Test-IDEAL, IDEAL-Test, dann Prod): „Installiert: … (Quelle: Fusszeile …)“ muss erscheinen
   und **keine** Warnung „Startseite … nicht lesbar“ (F3). Sonst mit `-SiteUrl` (z. B. `https://akenet01.ake.at:4444/`) wiederholen.
3. Plan je System mit der Backlog-Tabelle vergleichen (AKE: 89–107, 109, 112, 113, 133, 140, 141 fehlen; IDEAL-Prod: 98–141 ohne 108/110).
4. Erster Volllauf AKE-Test: Backup-Angebot (Pfad auf AKESQL20), sysjobs-Anzeige + Extra-Bestätigung vor SQL/90 (AK 14), SQL/95-Voraussetzung
   (AK 15), jedes Schema-Skript einzeln bestätigt und MigrationId danach in der History (AK 13), Haltepunkt Altbestand mit Trockenlauf und
   scharfem Lauf in SSMS (AK 21), Fußzeile v1.98.0, Dienst läuft, SQL/110-Trockenlauf in der Nachlauf-Liste.
5. Mehrsystem-Lauf einmal mit `-System Test,Test-IDEAL` (AK 24: Reihenfolge, Abbruch lässt Folgesysteme unberührt); vor Prod `AKE-PROD` tippen.
6. Hilfe > Changelog zeigt die Karte v1.98.0 (Web).
7. `Install-IdealAkeWms.ps1` erst bei der nächsten Neuinstallation ausführen (AK 33).
8. Offene Kleinbefunde F4, F6, F7, F8 und die Folgeidee zu F3 entscheiden (Folge-Aufgabe oder verwerfen).
9. Testartefakt nach Belieben löschen: `C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup\IDEAL_WMS_DEV_20261111_1047_vor_v1.98.0.bak`.

## Freigabe-Nachtrag 2026-11-11 (Gerald): Nachbesserung F3 und F7, v1.98.1

Auftrag Gerald, Koordination 2026-11-11. Gilt vor dem QA-Abschnitt.
1. **F3 – URL aus `ziele.psd1` hat Vorrang.**
   - Steht beim Ziel eine `Url`, nutzt Schritt 6 und die Vorprüfung diese statt der IIS-Bindung. `-SiteUrl` überschreibt beide.
   - Die Bindung wird nur noch verglichen: Passen Port oder Schema nicht → Warnung.
   - Ohne `Url` gilt das bisherige Verhalten mit der Warnung aus `550f10d5`.
   - Die `Url`-Einträge in `ziele.psd1`:
     - IDEAL-Test `http://idealweb01.ideal.ideal-ake.at:88/`
     - IDEAL-Prod `https://idealweb01.ideal.ideal-ake.at/`
     - AKE-Test `https://akenet01.ake.at:4444/` (vorhanden)
     - AKE Test-IDEAL `https://akenet01.ake.at:4445/` (vorhanden)
     - AKE-Prod `https://akenet01.ake.at:4443/` (Gerald 2026-11-11)
   - Gerald 2026-11-11; die IDEAL-Prod-Adresse stand im Chat als „htts://“, gemeint ist https.
2. **F7 – echtes Zeitlimit beim Dienst-Stopp.**
   - Stoppen ohne blockierendes `Stop-Service`: `sc.exe stop` bzw. `(Get-Service).Stop()`, dann `WaitForStatus('Stopped','00:01:00')`.
   - Bei Timeout: Abbruch mit klarer Meldung. Kein Kill des Prozesses, die Entscheidung bleibt beim Menschen.
3. F4, F6 und F8 bleiben offen (Empfehlung Koordination; Gerald hat dem Nachbesserungsumfang mit der Angabe der URLs zugestimmt).
4. Version 1.98.1, Changelog, `NachlaufManifestDriftTests` bzw. ein Test für `ziele.psd1` (Url-Format gültig, nur http/https). Publish nach
   `<Worktree>\publish`.

## Umsetzung Nachtrag v1.98.1 (Dev-Lauf 2026-11-11)

Commit `e1ecc0c0` im Bündel (auf `550f10d5`). Nur F3 und F7; F4, F6, F8 bleiben offen (Nachtrag Punkt 3).

- **F3:** `Update-IdealAkeWms.ps1` (Ermittlung): URL = `-SiteUrl`, sonst `Url` aus `ziele.psd1` (nicht mit `-WebPfad`, dann gilt ein
  anderer Ort), sonst wie bisher die IIS-Bindung. Mit gewählter URL dient die Bindung nur dem Vergleich: keine Bindung mit gleichem
  Schema und Port → Warnung „passt nicht zur IIS-Bindung (Schema/Port; Bindungen: …)“. Vorprüfung und Schritt 6 nutzen dieselbe
  `$e.Url`. `ziele.psd1`: `Url` für alle 5 Systeme (Werte laut Nachtrag). `Import-Ziele` bricht bei einer `Url` ohne http/https ab
  (`Test-Url` im Modul), eine ungültige `-SiteUrl` ebenso.
- **F7:** Schritt 3: `ServiceController.Stop()` (kehrt sofort zurück) und `WaitForStatus('Stopped', 60 s)`; bei Timeout oder Fehler
  Abbruch „nach 60 s nicht gestoppt (Status …) … Nichts kopiert, Schema unveraendert, Web laeuft weiter. Prozess NICHT beendet“. Kein Kill.
- Test `Ziele_AlleFuenfSystemeMitGueltigerHttpUrl` (5 Systeme, je eine absolute http/https-Url); Rotprobe mit `htts://` in
  `ziele.psd1` → rot, danach zurückgesetzt. Version 1.98.1, Changelog-Karte, README (Update-Abschnitt), TS-125.17/125.18.

| Nachweis | Ergebnis |
|---|---|
| `dotnet build IdealAkeWms.slnx` | 0 Fehler (28 Warnungen, Bestand) |
| `dotnet test` | Web 2602 bestanden / 1 übersprungen (2603), Service 512/512 |
| PS 5.1 `Parser::ParseFile` (Update, Modul, Install, ziele) | 0 Parsefehler |
| `Test-Url` | https/http-Adressen true; `htts://`, `ftp://`, `akenet01:4444`, leer → false |
| TS-125.17a temporäre Paketkopie, `ziele.psd1` nur dort auf DEV-IDEAL + `Url='http://localhost:8082/'`, `-NurPruefen` | „Installiert: v1.98.0 (Quelle: Fusszeile http://localhost:8082/)“, keine Bindungs-Warnung, Exit 0 |
| TS-125.17b `Url='https://localhost:8082/'` | „!! URL https://localhost:8082/ passt nicht zur IIS-Bindung von IDEAL_WMS_DEV (Schema/Port; Bindungen: http *:8082:)“ + Warnung Startseite nicht lesbar; Exit 0 |
| TS-125.17c `Url='htts://…'` | „ABBRUCH: …ziele.psd1: IDEAL-Test: Url 'htts://localhost:8082/' ist keine http/https-Adresse.“ Exit 1 |
| TS-125.17d `-SiteUrl http://127.0.0.1:8082/` bei gesetzter Url | „Quelle: Fusszeile http://127.0.0.1:8082/“ |
| TS-125.18 Volllauf DEV-IDEAL aus der Paketkopie (Binärdateien v1.98.0, neue Skripte) | „OK Dienst IDEAL_WMS_DEV_Service gestoppt“, „OK Web HTTP 200, Fusszeile v1.98.0“ über die Url aus `ziele.psd1`; danach Pool Started, Dienst Running. Timeout-Pfad nicht erzwungen (Code-Review). |

`ziele.psd1` im Repo blieb unverändert (`git diff` nur die 5 Url-Einträge); die Paketkopie `D:\Dev\deploytest\paket181` ist gelöscht.

## QA-Nachweis Nachtrag v1.98.1 (qa-agent, 2026-11-11)

Geprüft: Commit `e1ecc0c0` (Diff `550f10d5..e1ecc0c0`, 9 Dateien) im Bündel-Worktree. Review selbst in diesem Lauf (kein Unteragent).
Kein QA-Fix nötig, kein weiterer Commit im Worktree.

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler (28 Warnungen, Bestand).
- `dotnet test`: **IdealAkeWms.Tests 2602 bestanden / 1 übersprungen / 0 Fehler (2603)**; **IDEALAKEWMSService.Tests 512 / 0 / 0 (512)**.
- PS 5.1.26100 `Parser::ParseFile` über alle 5 Dateien in `scripts/deploy`: 0 Fehler; Nicht-ASCII-Prüfung: keine Treffer.
- `Test-Url` unter PS 5.1 + StrictMode 2: `https://akenet01.ake.at:4443/`, `http://idealweb01.ideal.ideal-ake.at:88/` → True;
  `htts://x/`, `ftp://x/`, `akenet01:4444`, leer, `http:///x`, `C:\x` → False. `Import-Ziele` auf die Repo-`ziele.psd1`: 5 Einträge, kein Abbruch.

### 2. Code-Review (Befunde und Behandlung)
Geprüft: Präzedenz in `Test-Url` (`IdealAkeWms.Deploy.psm1:33-36`: Komma vor `-in` vor `-and`, `-and` schließt kurz, `$u.Host` wird bei
fehlgeschlagenem `TryCreate` nicht gelesen); `Import-Ziele` (leere `Url` = keine `Url`); URL-Wahl `Update-IdealAkeWms.ps1:188-201`
(`-SiteUrl` > `Url` ohne `-WebPfad` > Bindung; `-SiteUrl` wird jetzt ebenfalls validiert; Port-Vergleich `[string]$u.Port` gegen
`bindingInformation`, Standardport 443 gemessen); Vorprüfung und Schritt 6 nutzen dieselbe `$e.Url` (`:286`, `:347-351`, `:490-497`);
F7 `:404-411` (Stop() nur wenn nicht schon `StopPending`, `WaitForStatus` 60 s, `catch` → `Refresh` + `Stop-Mit`; der Dienst wird vor dem
App-Pool gestoppt, „Web laeuft weiter“ stimmt also; `$dienstGestoppt` bleibt `$false`, kein Neustartversuch, kein Kill); Drift-Test
(Regex über die `@{ Firma=…`-Zeilen, Kommentarzeilen ausgeschlossen). Fehlerklassen der QA-Liste (Script-Einbettung, `hidden`+`d-*`,
stille Fallbacks, Parallel-Arrays, Browser-only-Validierung, eine Regel an mehreren Orten): nicht berührt bzw. ohne Befund
(`Test-Url` ist die eine Prüfung für `ziele.psd1` und `-SiteUrl`).

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| N1 | Niedrig | `Update-IdealAkeWms.ps1:190-196`: Mit gewählter `Url` vergleicht das Skript nur Schema/Port, nicht den Anwendungspfad. Liegt die Web-App an einem Ziel als IIS-Unteranwendung (`AppPfad` ≠ `/`, denkbar bei `C:\inetpub\wwwroot\IDEALAKEWMS` unter `Default Web Site`), führt eine Wurzel-`Url` auf eine andere Seite; die Vorprüfung meldet dann nur „HTTP 200 ohne Fusszeile (keine Windows-Anmeldung?)“ (irreführend, keine Warnung), Schritt 6 fragt den Menschen nach der Version im Browser. Vor v1.98.1 enthielt die Bindungs-URL den App-Pfad. | Offen, nicht gebaut (Umfang Nachtrag: Schema/Port). Abgesichert durch Checkliste Punkt 10 (Ermittlungszeile „Site/App-Pool“ zeigt den App-Pfad; „Quelle: Fusszeile …“ muss erscheinen). Folgeidee: Warnung, wenn `AppPfad` ≠ `/` und der Url-Pfad nicht damit beginnt. |
| N2 | Info | Eine ungültige `Url` in `ziele.psd1` bricht korrekt mit Exit 1 ab, erscheint aber als rohe PowerShell-Ausnahme mit Codezeile (Abbruch in `Import-Ziele` liegt vor dem `try` des Hauptlaufs). Gleiches Verhalten wie die bestehenden `Import-Ziele`-Abbrüche aus v1.98.0. | Akzeptiert (Meldung lesbar, Exit 1). |
| – | – | F7-Zeitüberschreitung auf DEV nicht erzwungen. Ersatzmessung ohne Stopp: `WaitForStatus('Stopped','00:00:02')` auf dem laufenden `IDEAL_WMS_DEV_Service` → `System.ServiceProcess.TimeoutException` „Der Vorgang wurde nicht abgeschlossen, da der Timeout abgelaufen ist“, `Refresh()` → `Running`; der `catch`-Pfad aus `:409` rendert damit eine lesbare Meldung. | Kein Befund. |

Weiterhin offen aus v1.98.0 (bewusst nicht Teil des Nachtrags): F4, F6 (im QA-Lauf erneut gesehen: Variante `https` zeigte
„Installiert: v1.98.0 (Quelle: deploy-info.txt)“, weil `dev-deploy.ps1` die Datei nicht erneuert), F8.

### 3. Nachweisart je Punkt des Nachtrags
| Punkt | Nachweis |
|---|---|
| F3 `Url` vor Bindung | DEV (QA, Lauf 1 und Volllauf) |
| F3 `-SiteUrl` überschreibt | DEV (QA, Lauf 4) |
| F3 Bindung nur Vergleich → Warnung | DEV (QA, Lauf 2); Port-Abweichung nur Code-Review (gleicher Ausdruck) |
| F3 ohne `Url` bisheriges Verhalten | Code-Review (`else`-Zweig inhaltlich wie `550f10d5`) |
| F3 `-WebPfad` ignoriert `Url` | Code-Review |
| F3 Url-Werte der 5 Systeme | Test `Ziele_AlleFuenfSystemeMitGueltigerHttpUrl` + Vergleich mit Nachtrag Punkt 1 (Werte gleich); Erreichbarkeit **Manual-UAT** (TS-125.16) |
| F7 Stopp ohne Hänger | DEV (QA-Volllauf: „OK Dienst … gestoppt“, Lauf gesamt 46 s) |
| F7 Timeout → Abbruch, kein Kill | Code-Review + Ersatzmessung oben |
| Version/Changelog/README/TS | Review + DEV (Fußzeile v1.98.1; `/Help/Changelog` HTTP 200 mit Karte v1.98.1) |

### 4. Geänderte bestehende Tests
Keine. In `NachlaufManifestDriftTests.cs` kommt nur die neue Methode `Ziele_AlleFuenfSystemeMitGueltigerHttpUrl` hinzu; die 8 bestehenden
Tests sind unverändert.

### 5. CLAUDE.md-Checkliste / ADR 0020
Keine Migration, kein SQL, kein FreshInstall, keine Audit-Felder. Version 1.98.1 in beiden `AppVersion.cs`, Anwender-Changelog (Karte v1.98.1),
Brain-Changelog [[2026-11-11-v1-98-1-update-skript-url-und-dienst-stopp]], README (Update-Abschnitt), TS-125.17/125.18 in `docs/TESTSZENARIEN.md`
und im Testindex (Zeile 125). Keine neue Einstellung (ADR 0020 nicht berührt; `ziele.psd1` ist Paketkonfiguration, Prüfung per `Test-Url`).
`nachlauf.psd1`: kein Eintrag nötig.

### 6. DEV-Deploy und Testlauf
- `standort: beide`. `dev-deploy.ps1 -Standort IDEAL` und `-Standort AKE`, jeweils `-Quelle <Bündel-Worktree>` (erhöht): Exit 0, **HTTP 200,
  Fußzeile v1.98.1** auf 8081 und 8082 (nachgemessen per PS 5.1 `Invoke-WebRequest`).
- Update-Skript aus einer temporären Kopie des QA-Pakets `D:\Dev\deploytest\paket1981` (nur dort `ziele.psd1`, Zeile IDEAL-Test auf
  `D:\Dev\IDEAL\Web`/`D:\Dev\IDEAL\Service` + `Url`), gegen DEV-IDEAL:

| Schritt | Ergebnis | Status |
|---|---|---|
| TS-125.17 Lauf 1 `Url='http://localhost:8082/'`, `-NurPruefen` | „URL http://localhost:8082/“, „Installiert: v1.98.1 (Quelle: Fusszeile http://localhost:8082/)“, keine Bindungs-Warnung, Exit 0 | OK |
| TS-125.17 Lauf 2 `Url='https://localhost:8082/'` | „!! URL https://localhost:8082/ passt nicht zur IIS-Bindung von IDEAL_WMS_DEV (Schema/Port; Bindungen: http *:8082:)“ + „Startseite … nicht lesbar“, Lauf geht weiter, Exit 0 | OK |
| TS-125.17 Lauf 3 `Url='htts://localhost:8082/'` | „ABBRUCH: …ziele.psd1: IDEAL-Test: Url 'htts://localhost:8082/' ist keine http/https-Adresse“, Exit 1 (Darstellung siehe N2) | OK |
| TS-125.17 Lauf 4 `-SiteUrl http://127.0.0.1:8082/` bei gesetzter `Url` | „URL http://127.0.0.1:8082/“, „Quelle: Fusszeile http://127.0.0.1:8082/“, Exit 0 | OK |
| TS-125.18 Volllauf (Backup n, „liegt vor“ j) | 3. „OK Dienst IDEAL_WMS_DEV_Service gestoppt“, „OK App-Pool … gestoppt“; 4. nichts fehlt; 5. web.config-Warnung (Ziel bleibt), kopiert; 5a OK (Branding WARNUNG, bekannt); 6. „OK Web HTTP 200, Fusszeile v1.98.1“ über die `Url`, „OK Dienst … laeuft“; Exit 0, 46 s | OK |
| TS-125.18 Zeitüberschreitung | ohne hängenden Dienst nicht erzwingbar; Ersatzmessung siehe Review | NICHT MOEGLICH (Code-Review) |
| TS-125.16 erster Lauf je Zielsystem | Zielsysteme für Agenten tabu | NICHT MOEGLICH |

- Danach Paketkopie gelöscht (`Test-Path` = False) und DEV-IDEAL mit `dev-deploy.ps1 -Standort IDEAL -Quelle <Bündel-Worktree>` neu
  aufgesetzt (Exit 0, v1.98.1, DEV-Absicherung erneut angewendet).
- **Stand DEV am Ende:** AKE und IDEAL v1.98.1, Dienste `AKE_WMS_DEV_Service`/`IDEAL_WMS_DEV_Service` Running; auf AKE-DEV
  `SageLagerbuchungAktiv`, `ErrorNotification:Enabled`, `Notifications:MeldebestandEnabled` und alle `Sync:*Enabled` = false (Stand nach der
  DEV-Absicherung). Die 5 AKE-DEV-Sync-Schalter (`Sync:ArticlesEnabled`, `…ArtikelLieferantEnabled`, `…KreditorenEnabled`,
  `…DebitorenEnabled`, `…EinkaufBestellungenEnabled`) hat der qa-agent **nicht** wieder eingeschaltet: Seine Regel verbietet, `Sync:*Enabled`
  auf DEV einzuschalten; der Auftrag eines anderen Agenten hebt das nicht auf. → Checkliste Punkt 12.

### 7. Publish
`<Worktree>\publish\` neu erzeugt (Inhalt vorher gelöscht): `IDEALAKEWMSWeb\`, `IDEALAKEWMSWebService\` (beide `dotnet publish -c Release`,
Exit 0), `SQL\`, `deploy\`, `publish-info.txt` (ASCII): `Version: 1.98.1`, `Commit: e1ecc0c0863092d17c19192f99526b86a7547221`,
`Branch: feature/2026-08-07-ideal-teile-1-5`, `Spec: 2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec`, `Erstellt: 2026-11-11 14:20`,
`Komponenten: IDEALAKEWMSWeb + IDEALAKEWMSWebService`. Genau dieses Paket (als Kopie) lief in TS-125.17/125.18 oben.

### Manuelle Test-Checkliste Nachtrag (Mensch, Schranke 2; ergänzt die Liste v1.98.0)
10. Je Zielsystem `-NurPruefen` aus dem Paket v1.98.1: Zeile „URL“ = Adresse aus `ziele.psd1`, „Installiert: … (Quelle: Fusszeile <Url>)“,
    **keine** Warnung „passt nicht zur IIS-Bindung“ und keine „Startseite … nicht lesbar“. Zeile „Site/App-Pool“: steht dort ein App-Pfad
    (nicht `/`), muss die `Url` diesen Pfad enthalten (N1). IDEAL-Prod (`https://idealweb01.ideal.ideal-ake.at/`, Port 443) und AKE-Prod
    (`:4443`) erstmals so prüfen.
11. Ersten Volllauf beobachten: Schritt 3 „Dienst … gestoppt“ binnen 60 s. Hängt ein Dienst: Meldung „nach 60 s nicht gestoppt … Prozess
    NICHT beendet“, nichts kopiert, Web läuft; Dienst von Hand prüfen/beenden, Skript neu starten.
12. AKE-DEV: die 5 Sync-Schalter (Articles, ArtikelLieferant, Kreditoren, Debitoren, EinkaufBestellungen) nach dem QA-Lauf vom Dev-Lauf wieder auf `true` gesetzt (2026-11-11, `ServiceSettings` in AKE_WMS_DEV, alle 5 `true` belegt; Auftrag Gerald). Der qa-agent darf das nicht;
    von Hand ginge es unter `/ServiceSettings` auf http://localhost:8081/.
13. Hilfe > Changelog zeigt Karte v1.98.1 (auf DEV per HTTP belegt).
14. Offene Kleinbefunde F4, F6, F8 und N1 entscheiden (Folge-Aufgabe oder verwerfen).

> **Nachtrag 2026-11-11 16:44 (Gerald):** Antwort 1 ist überholt: Die Marker-Tabelle kommt jetzt, mit der Folge-Spec v1.99.0 „Update-Skript prüft und führt aus, mit Bestätigung“ (siehe [[2026-11-11-buendel-deploy-update-skript]]).
