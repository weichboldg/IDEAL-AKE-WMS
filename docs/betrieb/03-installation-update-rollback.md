# 03 – Installation, Update und Rückfall

**Stand:** v1.99.29, 2026-11-16 · **Zielgruppe:** IT-Administratoren, die installieren oder aktualisieren · **Zweck:** Schritt für Schritt
durch Erstinstallation und Update mit dem Update-Skript, und ehrlich, was beim Zurückgehen möglich ist und was nicht.

Übersicht: [00-index.md](00-index.md). Zielsysteme: [02-systeme-umgebungen.md](02-systeme-umgebungen.md). Sicherung:
[10-backup-restore.md](10-backup-restore.md). Fehlerbilder: [07-troubleshooting.md](07-troubleshooting.md).

---

## 1. Grundregeln

1. **Reihenfolge je System: Datenbank (Schema) → Web → Dienst.** Das Update-Skript hält das selbst ein; bei Handarbeit nicht abweichen.
   (Quelle: `README.md:111-112`, `scripts/deploy/Update-IdealAkeWms.ps1:782-808`, `:855-888`)
2. **Erst Test-, dann Produktivsysteme.** Bei mehreren gewählten Systemen nimmt das Skript Nicht-Prod zuerst und Prod zuletzt; vor jedem
   Prod-System muss `AKE-PROD` bzw. `IDEAL-PROD` eingetippt werden. (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:86-87`, `:932-935`)
3. **Vor jedem Update ein DB-Backup** (Pflicht bei daten-konvertierenden Skripten und vor jedem Daten-/Einmal-Lauf). (Quelle:
   `scripts/deploy/nachlauf.psd1:218-219`, [[0004-migrations-und-sql-disziplin]])
4. **Web-App nie mit fehlenden Schema-Skripten starten.** Beim Start spielt sie fehlende Migrationen selbst ein (`Migrate()`) – ohne die
   Vorbedingungen der SQL-Skripte (Backups, Seeds, Sonderregeln). (Quelle: `IdealAkeWms/Program.cs:206`,
   `scripts/deploy/Update-IdealAkeWms.ps1:317`, `:799`, [[fallstricke]] §30 „Ein Seed, der nur im SQL-Skript steht, fehlt nach Migrate()“)
5. **Nur von Hand, als Administrator, in Windows PowerShell 5.1.** Es gibt keinen unbeaufsichtigten Modus und keinen Schalter, der
   Bestätigungen überspringt. (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:7`, `:14`, `scripts/deploy/IdealAkeWms.Deploy.psm1:19`)
6. **Sage-schreibende und außenwirksame Schalter bleiben nach dem Deploy aus**, bis sie je System freigegeben sind. Das Skript schaltet
   nichts davon ein. (Quelle: `README.md:121-131`)

---

## 2. Das Paket

Ein Paket ist der Ordner `publish\` aus dem Build: (Quelle: `README.md:71-72`, `README.md:899`, [[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]] Entscheidung)

| Teil | Inhalt |
|---|---|
| `IDEALAKEWMSWeb\` | Web-App (muss `IdealAkeWms.dll` enthalten) |
| `IDEALAKEWMSWebService\` | Dienst (muss `IDEALAKEWMSService.exe` enthalten) |
| `SQL\` | nummerierte Schema-Skripte, `Einmalig\`, `AgentJobs\`, `sage-views\` |
| `deploy\` | `Update-IdealAkeWms.ps1`, `Install-IdealAkeWms.ps1`, `IdealAkeWms.Deploy.psm1`, `nachlauf.psd1`, `ziele.psd1` |
| `publish-info.txt` | `Version:`, `Commit:`, `Branch:` … (Format `Schlüssel: Wert`) |

Das Skript prüft vor allem anderen: `publish-info.txt` vorhanden mit Version und Commit, gleiche Version/Commit in den Unterordnern,
`IdealAkeWms.dll` und `IDEALAKEWMSService.exe` vorhanden, `SQL\` vorhanden. (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:102-120`)
Aktuelles Paket: Version 1.99.23, Commit `08672fcc…`. (Quelle: `publish/publish-info.txt`)

> Altes `publish\deploy.zip` vom 13.11. 08:15 nicht verwenden. (Quelle: [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „Einmal je Server vorab“)
> **zu prüfen:** ob das heute im Paket liegende `deploy.zip` aktuell ist; das Skript braucht es nicht.

Ablage auf dem Zielserver: z. B. `C:\<FIRMA>\deploy\v<Version>\`. (Quelle: `README.md:72`)

---

## 3. Einmal je Server vorbereiten

| # | Was | Wie / Prüfung | Beleg |
|---|---|---|---|
| 1 | `sqlcmd` installiert | erst „Microsoft ODBC Driver for SQL Server“, dann „Microsoft Command Line Utilities for SQL Server“ (ODBC-Variante). Das Skript sucht im PATH und unter `C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180|170\Tools\Binn` und verlangt die Schalter `-E -b -i -C` | `scripts/deploy/Update-IdealAkeWms.ps1:124-134`, `README.md:105-106` |
| 2 | SQL-Version ≥ 2017 (für die Backup-Ordner-Prüfung) und Recht `ALTER` auf die Ziel-DB | Abfrage „P-B1“ in SSMS als ausführendes Konto (steht in der Vorher-Liste des Skripts) | `scripts/deploy/nachlauf.psd1:220-222`, [[2026-11-12-deploy-fahrplan-buendel-v1-99]] |
| 3 | Rechte des ausführenden Kontos | `db_owner`, `BACKUP DATABASE`, Lesen `msdb.sysjobs` (Rechte-Abfrage der Spec) | [[2026-11-11-buendel-deploy-update-skript]] „Vorher“ |
| 4 | Kerberos/NTLM | Ist das ausführende Konto in „Protected Users“, braucht die SQL-Instanz SPNs (sonst Fehler 18452 „nicht vertrauenswürdige Domäne“) | [[2026-11-11-buendel-deploy-update-skript]] „Befund erster -NurPruefen“ |
| 5 | Backup-Ordner auf dem **SQL-Server** mit Schreibrecht des SQL-Instanzkontos | Standard ist der Instanz-Backup-Pfad; sonst `-BackupPfad` | `scripts/deploy/Update-IdealAkeWms.ps1:471-474`, [[2026-11-12-deploy-fahrplan-buendel-v1-99]] |
| 6 | Prod: keine Mail-Ablage | `MailSettings:PickupDirectory` (Datei) bzw. Umgebungsvariable `MailSettings__PickupDirectory` am Prod-Dienst darf nicht gesetzt sein – das Skript bricht auf Prod sonst vor dem Stoppen ab | `scripts/deploy/nachlauf.psd1:158-161`, `scripts/deploy/Update-IdealAkeWms.ps1:700-701`, `:738`, [[2026-11-12-deploy-fahrplan-buendel-v1-99]] |
| 7 | Microsoft Edge auf dem Web-Server (PDF) | Skript prüft in Schritt 5a, warnt nur | `scripts/deploy/IdealAkeWms.Deploy.psm1:209-213` |

---

## 4. Erstinstallation

### 4.1 Mit dem Installationsskript `deploy\Install-IdealAkeWms.ps1`

Standortneutral (`-Firma AKE|IDEAL`), nur für eine **leere** Umgebung: Abbruch, wenn Site, App-Pool, Dienst oder nicht leere Ordner schon
existieren. (Quelle: `scripts/deploy/Install-IdealAkeWms.ps1:4`, `:208-213`)

**Vorab von Hand:** leere Datenbank anlegen (Kollation `Latin1_General_CI_AS`), SQL-Logins und -Rechte anlegen, Zertifikat in
`Cert:\LocalMachine\My` (ohne Zertifikat nur HTTP – dann geht der Kamera-Scan nicht). (Quelle: `scripts/deploy/Install-IdealAkeWms.ps1:26-30`,
`:215-223`, [[2026-11-04-ideal-produktiv-installation]] „Reihenfolge“)

Ablauf des Skripts (jeder Schritt bricht bei Fehler ab): (Quelle: `scripts/deploy/Install-IdealAkeWms.ps1:14-24`, Schritte `:114-475`)

| Schritt | Was passiert |
|---|---|
| 0 | Fragt **ein** AD-Konto für App-Pool **und** Dienst ab und prüft das Kennwort gegen das AD; fragt Pfade für Web, Dienst, DataProtection (Enter = Vorschlag) |
| 1 | Prüft Administrator, IIS-Features (Web-Server, Windows-Auth, Application Initialization), ASP.NET Core Module V2 + Hosting Bundle 10.x, Publish-Ordner, Edge |
| 2 | Optional `-SqlTeil1Ausfuehren`: Schema-Skript `<FIRMA>-Prod-1-Datenbank.sql` per `sqlcmd` |
| 3 | Web-Dateien kopieren, `appsettings.Production.json` schreiben (Verbindung, `DataProtection:KeysPath`, `Security:AdDomain`), Branding kopieren. SData-Zugang **nicht** im Skript: `/ServiceSettings` (verschlüsselt, ADR 0024) |
| 4 | IIS: Auth-Abschnitte entsperren; App-Pool (No Managed Code, AlwaysRunning, Idle 0, kein Recycling-Intervall, `loadUserProfile`), Site mit Bindungen, Preload, Windows- **und** anonyme Authentifizierung. Pool bleibt gestoppt, bis das Schema da ist |
| 5 | Dienst-Dateien kopieren, Dienst-`appsettings.json` anpassen (Verbindungen, Mail; bei `-SmtpCredential` Warnung: Kennwort im Klartext, ohne `-SmtpSsl` unverschlüsselt), Recht „Als Dienst anmelden“, Dienst registrieren (Autostart, Neustart bei Absturz) – **nicht** gestartet |
| 5b | Ordner und Rechte wie beim Update (`Test-OrdnerUndRechte`) |
| 6 | Web aufwärmen (erster Start: `Migrate()` + Startdaten), nur wenn das Schema vorhanden ist; danach `Sync:ProductionOrdersEnabled = false` in `dbo.ServiceSettings` (der Dienst liest den Schalter nur aus der DB). Ohne Web-Start: Warnung, von Hand unter `/ServiceSettings` setzen |
| 7 | Optional `-SqlTeil2Ausfuehren`: Standort-Konfiguration `<FIRMA>-Prod-2-Konfiguration.sql` (erst nach dem Web-Start) |
| 8 | Optional `-DienstStarten` – erst **nach** Teil 2 |

Danach von Hand (Skript gibt die Liste aus): **sofort** als `admin` mit dem Standard-Erstkennwort `wms-admin` anmelden – das WMS verlangt
sofort ein eigenes Kennwort (Pflichtwechsel, ADR 0027); SQL-Rechte für
Web- und Dienstkonto; erster Dienst-Zyklus mit DryRun, dann `WorkerSettings:SyncDryRun = false`; IDEAL: Backup, dann `/HierarchieUmstellung`
(Einwegtor); Benutzer und Rollen; `WindowsAuthAktiv` erst, wenn AD-Benutzer angelegt sind; FA-Abgleich nach Teil 2 unter `/ServiceSettings` einschalten; SData-Zugang unter
`/ServiceSettings`. (Quelle: `scripts/deploy/Install-IdealAkeWms.ps1`, Abschnitt „Fertig - naechste Schritte“)
Protokoll: `C:\ProgramData\IdealAkeWms\Install-<FIRMA>-<Zeit>.log`. (Quelle: `scripts/deploy/Install-IdealAkeWms.ps1:102-104`)

> **zu prüfen (wichtig):** Das Skript erwartet `<FIRMA>-Prod-1-Datenbank.sql` und `<FIRMA>-Prod-2-Konfiguration.sql` im Skriptordner
> (`-SqlSkriptOrdner`, Vorgabe `deploy\`). Diese Dateien liegen **nicht** im Repo und nicht im Paket; die IDEAL-Fassungen gibt es nur als
> Ablage `secondbrain/tests/Claude outputs/IDEAL-Prod-1-Datenbank.sql` bzw. `…-2-Konfiguration.sql`, eine AKE-Fassung gar nicht.
> (Quelle: `scripts/deploy/Install-IdealAkeWms.ps1:47`, `:229-235`, [[2026-11-04-ideal-produktiv-installation]] „Dateien“)
> Ohne diese Dateien Teil 1/2 in SSMS ausführen (Abschnitt 4.2).

> **Bei einem Fehler** bleiben bereits angelegte Teile (Site, App-Pool, Dienst, Ordner) stehen und müssen vor einem neuen Lauf entfernt
> werden. (Quelle: `scripts/deploy/Install-IdealAkeWms.ps1:499-503`)

### 4.2 Datenbank für die Erstinstallation (`SQL/00_FreshInstall.sql`)

- Legt Tabellen, Views, Indizes, Standarddaten und alle `__EFMigrationsHistory`-Einträge an (bis `20261114080950_…`). (Quelle:
  `README.md:60`, `SQL/00_FreshInstall.sql:3262-3264`)
- Enthält am Anfang `CREATE DATABASE [IDEAL_AKE_WMS]` (falls nicht vorhanden) und `USE [IDEAL_AKE_WMS]` – der DB-Name ist fest. Für einen
  anderen Namen den Kopf ersetzen (so macht es das IDEAL-Teil-1-Skript). (Quelle: `SQL/00_FreshInstall.sql:8-17`,
  [[2026-11-04-ideal-produktiv-installation]] „Festgehaltene Entscheidungen“)
- Setzt **kein** `READ_COMMITTED_SNAPSHOT` (RCSI). Für IDEAL mit hierarchischer FA-Struktur ist RCSI Pflicht; das Update-Skript prüft es
  später (`Pruefung/RCSI`). (Quelle: [[2026-11-04-ideal-produktiv-installation]] „Festgehaltene Entscheidungen“, `scripts/deploy/nachlauf.psd1:110-114`)
- Beim ersten Web-Start entstehen Benutzer `admin` (Standard-Erstkennwort `wms-admin` mit Pflichtwechsel, seit v1.99.28) und Lagerplatz
  `NAN`. (Quelle: `README.md:62-66`, `IdealAkeWms/Services/AdminSeed.cs`, `IdealAkeWms/Program.cs`)

> **Behoben in v1.99.28:** Am 04.11.2026 brach Abschnitt „15. Indexes“ mit Meldung 207 „Ungültiger Spaltenname AssignedPickerId“ ab.
> Meldung 207 entsteht beim Übersetzen des **ganzen Batches** – damals fehlten deshalb **alle** Indizes des Abschnitts samt FK, nicht nur
> die beiden falschen. Die beiden Anweisungen (Spalten liegen in `ProductionOrderPickingStatus`) sind entfernt, `WarehouseRequisitions`/
> `-Items` haben einen `OBJECT_ID`-Guard, und der Test `FreshInstallIndexSpaltenTests` prüft jede Index-Spalte gegen das EF-Modell.
> Systeme aus der Erstinstallation vom 04.11. (IDEAL-Prod): Das Update-Skript zeigt dort in der Vorprüfung `Pruefung/FreshInstallIndizes`
> (nur IDEAL-Prod, nur anzeigen) – entspricht Prüfabfrage P-2b der Spec 2026-11-14-sicherheit-anmeldung-und-installer
> ausführen; bei Treffern fehlende Indizes nachziehen (offen, siehe Spec Rückfrage 4).

---

## 5. Update mit `deploy\Update-IdealAkeWms.ps1`

### 5.1 Aufruf

```powershell
# Administrator-PowerShell (Windows PowerShell 5.1) am Zielserver
powershell -ExecutionPolicy Bypass -File C:\<FIRMA>\deploy\v<Version>\deploy\Update-IdealAkeWms.ps1 -Firma <AKE|IDEAL> -System <Name> -NurPruefen   # erst Plan ansehen
powershell -ExecutionPolicy Bypass -File C:\<FIRMA>\deploy\v<Version>\deploy\Update-IdealAkeWms.ps1 -Firma <AKE|IDEAL> -System <Name>                # dann Update
```
(Quelle: `README.md:73-74`, `scripts/deploy/Update-IdealAkeWms.ps1:17`)

| Parameter | Wirkung | Beleg |
|---|---|---|
| `-Firma`, `-System` | ohne Angabe fragt das Skript; mehrere Systeme mit Komma; leere Eingabe = Abbruch ohne Änderung | `scripts/deploy/Update-IdealAkeWms.ps1:56-82` |
| `-Komponente Web\|Dienst\|Beide` | was getauscht wird (Default `Beide`); Schema, Ordner/Rechte und Nachlauf laufen immer | `README.md:81`, `scripts/deploy/Update-IdealAkeWms.ps1:645` |
| `-NurPruefen` | Plan, Ordner/Rechte-Bericht und Nachlauf-Liste, **nichts** gestoppt, nichts geschrieben | `scripts/deploy/Update-IdealAkeWms.ps1:740-747` |
| `-BackupPfad` | Backup-Ordner auf dem SQL-Server statt Instanz-Standard | `scripts/deploy/Update-IdealAkeWms.ps1:471-474` |
| `-SiteUrl` | Startseite für die Versionsprüfung, Vorrang vor `ziele.psd1` | `scripts/deploy/Update-IdealAkeWms.ps1:204-206` |
| `-WebPfad`, `-DienstPfad` | abweichende Ordner (nur bei **einem** System) | `scripts/deploy/Update-IdealAkeWms.ps1:83-85` |
| `-Quelle`, `-SqlOrdner`, `-LogPfad` | Paket-, SQL- und Protokollordner; Protokoll-Standard `C:\<FIRMA>\deploy-logs` | `scripts/deploy/Update-IdealAkeWms.ps1:89-94` |

Das Skript **liest Sage nie** (Prüfabfragen auf Sage bleiben Hand-Schritte). (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:14`,
`scripts/deploy/nachlauf.psd1:262-263`)

### 5.2 Phasen eines Laufs

| Phase | Was passiert | Ihre Eingabe | Was ist dann gestoppt | Beleg (`Update-IdealAkeWms.ps1`) |
|---|---|---|---|---|
| **Ermittlung** | zeigt Paket, Web-/Dienst-Ordner, Site/Pool, Dienst + Konto, Umgebung, DB, URL, Mail-Ziel, sqlcmd | geteilter App-Pool? → `j`; „Verbindung herstellen (nur lesend)?“ → `j` | nichts | `:648-669` |
| **1 Vorprüfung** (lesend) | installierte Version (Fußzeile, sonst `deploy-info.txt`, sonst Frage); Migrations-History muss SQL/88 enthalten; fehlende Schema-Skripte; alle **Prüfungen**; Stand der Daten-/Einmal-Einträge (Marker); Backup-Ordner; Vorher-Hinweise; Trockenlauf der Haltepunkt-Skripte; **Plan** | ggf. Version eintippen | nichts | `:672-737` |
| | Prod-Sperre: Prüfung der Phase „Vorher“ mit Abweichung auf Prod → Abbruch, nichts gestoppt | Wert von Hand beheben, neu starten | nichts | `:700-701`, `:738` |
| **2 Backup** | `BACKUP DATABASE … WITH COPY_ONLY, CHECKSUM` auf dem SQL-Server; Pflicht, wenn ein Haltepunkt-Trockenlauf Treffer hatte | `j`; sonst „Liegt ein aktuelles Backup vor?“ → ohne Ja Abbruch | nichts | `:750-761`, `:499-511` |
| **3 Stoppen** | Dienst (max. 60 s, Prozess wird **nicht** beendet), dann App-Pool (max. 60 s, +2 s) | – | Dienst, App-Pool | `:764-780` |
| **4 Schema** | fehlende Schema-Skripte in **Manifest-Reihenfolge** per `sqlcmd -b`; nach jedem Skript Kontrolle der MigrationId. Sonderregeln: vor SQL/90 Anzeige der Agent-Jobs + Extra-Bestätigung; SQL/95 nur mit Vorgänger-Migration | je Skript `j` (Nein = Abbruch, beides bleibt gestoppt) | Dienst, App-Pool | `:783-808` |
| **4a Prüfungen und Haltepunkt** | Reparaturen der Prüfungen (nur nach Bestätigung); Haltepunkt-Skripte (z. B. Altbestand Kommissionieraufträge) | `j` / `SCHARF` | Dienst, App-Pool | `:811-822` |
| **5 Kopieren** | `robocopy /E` **ohne Löschen**; nie überschrieben: `appsettings*.json`, `web.config`, `Branding\`, `App_Data\`, `wwwroot\Fotos\`, `logs\`; danach Kontrolle, dass diese unverändert sind; Warnung, wenn die `web.config` im Paket abweicht | – | Dienst, App-Pool | `:825-839`, `:632-639` |
| **5a Ordner und Rechte** | prüft wirksame Rechte (Web-Ordner, `App_Data\Fotos\Kommissionierung`, `logs`, DataProtection, Branding, Dienst-Ordner, Dienst-Logs, `appsettings.<Umgebung>.json`, Edge); fehlende Ordner/Rechte nach **einer** Sammelbestätigung, nur hinzufügend, alte ACL ins Log | `j` | Dienst, App-Pool | `:842-845`, `IdealAkeWms.Deploy.psm1:128-215` |
| **5b Vor dem Start** | Daten-/Einmal-Einträge der Phase „VorStart“ (z. B. SQL/108, SQL/147) in einer Transaktion | `j`/`n`/`e`, dann `uebernehmen`/`verwerfen` | Dienst, App-Pool | `:848-852` |
| **6 Starten** | **Web zuerst**: App-Pool starten, Startseite abrufen (bis 300 s), Fußzeilen-Version = Paket-Version, DLL-Hash = Paket; Fotos-Verschiebung im Log abgleichen. **Dann Dienst**: starten, 30 s warten, muss laufen, DLL-Hash prüfen | ggf. „Zeigt die URL im Browser v…?“ | bei Fehler: App-Pool wieder gestoppt, Dienst nicht gestartet | `:855-888` |
| **7 Nachlauf-Liste** | Daten-Einträge der Phase „Nachher“ (z. B. SQL/110 mit Trockenlauf), Hand-Schritte (Einstellungen, Leitstand, Info), Stand aller Prüfungen, gesammelte Warnungen | je Eintrag | nichts | `:891`, `:895-926` |

### 5.3 Prüfungen (Manifest-Typ „Pruefung“)

Laufen bei **jedem** Lauf (ohne Versionsfilter), lesend; Ergebnis 0 = in Ordnung. (Quelle: `scripts/deploy/nachlauf.psd1:12-17`)

| Phase | Verhalten | Beispiele | Beleg |
|---|---|---|---|
| Vorher | in der Vorprüfung; auf Prod Abbruch vor dem Stoppen, sonst Hinweis | `Pruefung/MailPickup`, `Pruefung/BenutzerOhneKennwort` (aktive Benutzer ohne Kennwort ausser admin, v1.99.28), `Pruefung/FreshInstallIndizes` (nur IDEAL-Prod: Indizes/FK aus der Erstinstallation vom 04.11. fehlen, v1.99.28; legt nichts an) | `scripts/deploy/nachlauf.psd1:158-161` |
| Haltepunkt | nach dem Schema bei gestopptem Web und Dienst; Reparatur nur nach `j` | Kommissionierbereiche, RCSI, Rollen Beschichtungsauftrag, Rolle Einkauf, DUMMY-Artikel | `scripts/deploy/nachlauf.psd1:101-137` |
| Nachher | nur lesend, Text = Hand-Schritt | Lieferverzug-Empfänger, Wareneingang-CSV, QR-Trennzeichen, Firmenanschrift, `Sync:ArticlesViewName`, SData-Kennwort in appsettings, SMTP-Benutzer (`Pruefung/SmtpBenutzer`), doppelte Prio, nur AKE: `Sync:Quelle:Artikel` nicht auf `SData` (`Pruefung/ArtikelQuelleSData`, v1.99.39; sonst bleiben `ArticleSageDetails` leer, Hand-Schritt, keine automatische Umschaltung) | `scripts/deploy/nachlauf.psd1:138-169,178-181` |

Besonderheit RCSI: Die Reparatur (`ALTER DATABASE … SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE`) zeigt vorher alle anderen
Sitzungen auf der DB, weil sie diese beendet. Ohne Recht `ALTER` die Frage mit `n` beantworten. (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:438-444`,
`scripts/deploy/nachlauf.psd1:110-114`, `:221`) Reparaturen bekommen keinen Marker – sie werden jedes Mal neu bewertet.
(Quelle: [[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]] Entscheidung)

### 5.4 Daten- und Einmal-Skripte: Trockenlauf, `SCHARF`, `uebernehmen`

Daten-/Einmal-Skripte führt das Skript **nur nach Eingabe am Zielserver** aus (ADR 0023). Drei Wege je nach Manifest-Eintrag:
(Quelle: [[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]], `scripts/deploy/nachlauf.psd1:18-21`)

**Weg A – mit Trockenlauf-Schalter** (Datei enthält genau einmal `DECLARE @Trockenlauf BIT = 1;`):
1. Trockenlauf automatisch; Ausgabe als Datei im Protokollordner; die Datei muss eine Zeile `TREFFER: n` liefern.
2. `TREFFER: 0` → „entfällt“, auf Wunsch Marker `Entfaellt`.
3. Treffer > 0 → Backup **aus diesem Lauf** nötig (sonst bietet das Skript es an); dann exakt `SCHARF` eintippen (Groß/Klein zählt).
4. Scharflauf mit `@Trockenlauf = 0` – nur im Arbeitsspeicher, die Datei bleibt unverändert – in **einer** Transaktion zusammen mit dem
   Marker, sofort übernommen.
5. Zweiter Trockenlauf muss `TREFFER: 0` zeigen.
Bei einem Haltepunkt-Eintrag beendet jedes Nein den Lauf; Web und Dienst bleiben gestoppt.
(Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:554-590`, `:294-297`)

**Weg B – Transaktion** (ohne Schalter):
1. Prüfabfrage anzeigen; Frage `j` = ausführen / `n` = später / `e` = lief schon, nur vermerken.
2. Bei `j`: Web und Dienst müssen gestoppt sein, Backup aus diesem Lauf nötig.
3. Ganze Datei in **einer** Transaktion (Aufteilung an `GO`), Ausgabe zeigen; die Transaktion hält Sperren, bis Sie entscheiden.
4. `uebernehmen` = Commit + Marker; `verwerfen` oder leer = Rollback, Eintrag bleibt offen.
Abgelehnt werden Dateien mit `sqlcmd`-Direktiven oder `ROLLBACK`. (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:592-608`, `:352-409`)

**Weg C – anzeigen:** Hand-Schritt; das Skript fragt danach „Wurde … schon ausgeführt?“ und vermerkt `VonHand`.
(Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:547-551`)

**Marker `dbo.DeployScriptRuns`** (ab SQL/142): Ein Eintrag mit Marker wird **nie wieder** angeboten. Gespeichert werden u. a. Manifest-Id,
Datei, SHA-256, Modus (`Scharf`, `Transaktion`, `Entfaellt`, `Spur`, `VonHand`), Treffer, Ausgabe, Paket-Version/Commit, Zeit, ausführendes
Konto. Alte Läufe ohne Marker erkennt das Skript über „Spur“-Abfragen und bietet an, sie zu vermerken. Soll ein Skript bewusst ein zweites
Mal laufen, braucht es einen neuen Manifest-Eintrag. (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:325-347`, `:534-544`,
`scripts/deploy/nachlauf.psd1:19-20`) Abfragen dazu: [08-diagnose-abfragen.md](08-diagnose-abfragen.md).

### 5.5 Protokolle eines Laufs

| Datei | Ort | Beleg |
|---|---|---|
| Gesamtprotokoll (Transcript) `<yyyy-MM-dd_HHmm>_<FIRMA>-<System>_v<Version>.log` | `C:\<FIRMA>\deploy-logs` | `scripts/deploy/Update-IdealAkeWms.ps1:94`, `:936-937` |
| Trockenlauf-Ausgaben `…_trockenlauf.txt` | gleicher Ordner | `scripts/deploy/Update-IdealAkeWms.ps1:555` |
| ACL-Sicherung vor Rechteänderung `acl_<Zeit>_<Eintrag>.txt` | gleicher Ordner | `scripts/deploy/IdealAkeWms.Deploy.psm1:180-183` |
| `deploy-info.txt` (Kopie von `publish-info.txt`) | Web- und Dienst-Ordner | `scripts/deploy/Update-IdealAkeWms.ps1:836-837` |

### 5.6 Schritt für Schritt: ein System aktualisieren

1. Paket auf den Zielserver kopieren, `publish-info.txt` prüfen (Version). (Quelle: [[2026-11-11-buendel-deploy-update-skript]] „Vorher“)
2. Fahrplan des Releases lesen (Sonderpunkte je System, z. B. System-Kürzel auf Testsystemen, Prüfabfrage vor v1.99.17).
   (Quelle: [[2026-11-12-deploy-fahrplan-buendel-v1-99]])
3. Wartungsfenster: niemand kommissioniert/bucht (Web und Dienst werden gestoppt). (Quelle: [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „AKE-Test“)
4. `-NurPruefen` ausführen; Plan lesen: fehlende Schema-Skripte, Prüfungen ≠ 0, offene Daten-/Einmal-Einträge, Backup-Ordner, Warnungen.
5. Ohne `-NurPruefen` ausführen, Fragen wie in 5.2 beantworten. Backup anlegen lassen.
6. Bei `SCHARF`/`uebernehmen`: die angezeigte Zählung bzw. Ausgabe lesen, erst dann bestätigen.
7. Nach dem Start: Fußzeile im Browser prüfen; Nachlauf-Liste abarbeiten (Hand-Schritte, Einstellungen speichern, Rollen zuweisen).
8. Aktivitäts-Protokoll `/SyncLog` und Logs ansehen ([06-logs-ueberwachung.md](06-logs-ueberwachung.md)).
9. Protokolldatei des Laufs aufbewahren.

### 5.7 Abbruch und Weitermachen

Nach jedem unerwarteten Fehler zeigt das Skript den Live-Zustand (App-Pool, Site, Dienst, letzter Schritt, fehlende MigrationIds) und den
Weg weiter. (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:303-323`)

| Abbruch in | Zustand | Weiter |
|---|---|---|
| Ermittlung, 1, 2 | nichts gestoppt, nichts geändert | Ursache beheben, neu starten |
| 3, 4, 4a | Web und/oder Dienst gestoppt | **nicht von Hand starten** (der Web-Start spielte fehlende Migrationen ohne Vorbedingungen ein); Ursache beheben, Skript neu starten – es setzt bei Fehlendem fort |
| 5, 5a, 5b | Dateien ggf. teilweise kopiert, beides gestoppt | Skript neu starten (kopiert erneut) |
| 6 | App-Pool gestoppt bzw. Dienst nicht gestartet | `<Web>\logs` bzw. `<Dienst>\logs` prüfen, Skript neu starten |
| danach | Web und Dienst laufen | offene Nachlauf-Einträge: Skript neu starten; Marker verhindern Doppelläufe |

(Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:315-321`) Bei mehreren Systemen werden nach einem Fehler die folgenden nicht angefasst.
(Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:945-949`) Häufige Ursachen: [07-troubleshooting.md](07-troubleshooting.md).

---

## 6. Rückfall (Rollback): was geht, was nicht

**Grundsatz: Das Datenbankschema geht nur vorwärts.** Das Update-Skript kennt keinen Rückwärtsweg; es gibt keine Rückbau-Skripte im Paket,
und die Web-App migriert beim Start nur vorwärts. (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:1-14` – kein Rückbau-Schritt;
`IdealAkeWms/Program.cs:206`) Einzelne Migrationen sind zudem daten-konvertierend; ihr Rückweg verliert Detail-Status.
(Quelle: `README.md:221-222`, [[0004-migrations-und-sql-disziplin]] Entscheidung, letzter Absatz)

| Situation | Möglich? | Wie | Beleg |
|---|---|---|---|
| Abbruch **vor** Schritt 3 | ja, nichts zu tun | Ursache beheben, neu starten | `scripts/deploy/Update-IdealAkeWms.ps1:316` |
| Abbruch **während** Schema/Kopieren | vorwärts | Skript erneut laufen lassen (idempotente Skripte, Marker) | `scripts/deploy/Update-IdealAkeWms.ps1:317-318`, [[0004-migrations-und-sql-disziplin]] Konsequenzen |
| Daten-Lauf in Weg B nicht gewollt | ja, **vor** `uebernehmen` | `verwerfen` (Rollback) | `scripts/deploy/Update-IdealAkeWms.ps1:395-397` |
| Daten-Lauf nach `SCHARF`/`uebernehmen` rückgängig | nur über DB-Restore | Backup aus diesem Lauf zurückspielen | `scripts/deploy/Update-IdealAkeWms.ps1:512-520`, [10-backup-restore.md](10-backup-restore.md) |
| Neue Version macht Probleme, Schema eingespielt | **nicht** durch Zurückkopieren alter Dateien allein | DB-Backup von vor dem Update **und** alte Programmdateien zurück; beides zusammen. **zu prüfen:** ob ältere Binärdateien gegen ein neueres Schema überhaupt laufen – im Code nicht abgesichert | siehe unten |
| Fachliche Funktion abschalten statt Rückfall | ja | Schalter unter `/Settings` bzw. `/ServiceSettings` aus; Sage-Quellen zurück auf `View` | `README.md:768`, `README.md:1031-1032` |
| IDEAL: hierarchische FA-Struktur zurück | **nicht über die Anwendung** | nur bewusste Datenbereinigung laut Runbook, DB-Backup zwingend | [[0012-fa-hierarchie-einweg-migrationstor]] Punkt 2, `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md`, `README.md:443` |
| Rücksprung auf eine Version < v1.96.0 | eingeschränkt | Kommissionierfotos werden dann nicht mehr angezeigt (liegen seit v1.96.0 in `App_Data`) | [[2026-11-11-v1-96-0-vorwaerts-merge-main-hotfixes]] „Deploy-Risiken“ |

Wichtig für einen echten Rückfall:
- **Das Update-Skript sichert die Programmdateien nicht.** Es kopiert ohne Löschen über die alten Dateien und legt nur ein DB-Backup an.
  (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:632-639`, `:499-511`) Wer zurückfallen können will, sichert Web- und Dienst-Ordner vor dem
  Update selbst ([10-backup-restore.md](10-backup-restore.md)). Alternativ liegt das vorherige Paket noch unter `C:\<FIRMA>\deploy\v<alt>\`.
- **Nie nur die DB zurückspielen und die neue Web-App starten:** Die neue Web-App spielt dann alle Migrationen per `Migrate()` erneut ein –
  ohne Sonderregeln, Seeds und Prüfungen. Nach einem DB-Restore entweder die alten Programmdateien wiederherstellen oder das Update-Skript
  erneut laufen lassen. (Quelle: `IdealAkeWms/Program.cs:206`, `scripts/deploy/Update-IdealAkeWms.ps1:317`)
- **Sage lässt sich nicht mit zurückspielen.** Was der Dienst nach dem Backup an Sage gemeldet hat (Lagerbuchungen, Wareneingangsbelege),
  bleibt in Sage. **zu prüfen** je Fall: nach einem WMS-Restore fehlen diese Vorgänge im WMS, die Doppelbuchungs-Sperre arbeitet über die
  Sage-Memo-Kennung. (Quelle: `README.md:726-729`, [[integrationen]] „Idempotenz“)
- Ein Restore entfernt auch Marker aus `dbo.DeployScriptRuns`; der nächste Lauf bietet die Einträge erneut an (Spur-Abfragen erkennen
  bereits gelaufene Daten). (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:325-333`, `:539-544`)

---

## 7. Deinstallation (Dienst)

```powershell
Stop-Service   -Name "<Dienstname>" -Force
Remove-Service -Name "<Dienstname>"
Remove-Item    -Path "<Dienst-Ordner>" -Recurse -Force
```
(Quelle: `README.md:905-911`; Dienstname und Ordner je System siehe [02-systeme-umgebungen.md](02-systeme-umgebungen.md).)
Vorher die Konfigurationsdateien und Logs sichern ([10-backup-restore.md](10-backup-restore.md)). IIS-Site, App-Pool, Web-Ordner und
Datenbank: **zu prüfen** – kein dokumentierter Ablauf.

---

## 8. Kurz-Checkliste Update

- [ ] Paket kopiert, `publish-info.txt` = erwartete Version
- [ ] Fahrplan/Changelog des Releases gelesen, Sonderpunkte je System notiert
- [ ] `sqlcmd`, SQL-Rechte, Backup-Ordner geprüft (einmal je Server)
- [ ] Prod: kein `PickupDirectory`
- [ ] Programmordner gesichert (falls Rückfall gewünscht)
- [ ] `-NurPruefen` gelesen
- [ ] Volllauf im Wartungsfenster, Backup angelegt
- [ ] Fußzeile = neue Version, Dienst läuft
- [ ] Nachlauf-Liste abgearbeitet, Sage-/Mail-Schalter bewusst entschieden
- [ ] Protokoll `C:\<FIRMA>\deploy-logs\…` abgelegt
