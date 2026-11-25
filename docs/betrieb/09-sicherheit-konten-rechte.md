# 09 – Sicherheit, Konten und Rechte

**Stand:** v1.99.29, 2026-11-16 · **Zielgruppe:** IT-Administratoren (ohne Entwicklerwissen) · **Zweck:** Unter welchen Konten Web-App und
Dienst laufen, welche Rechte sie auf SQL Server, Ordner und Freigaben brauchen, wie Anmeldung und Rollen funktionieren und wo Geheimnisse
liegen – und was nie im Klartext stehen darf.

Übersicht aller Kapitel: [00-index.md](00-index.md). Schnittstellen und deren Konten: [05-schnittstellen.md](05-schnittstellen.md).
Einstellungen: [04-konfiguration.md](04-konfiguration.md). Server und Pfade je System: [02-systeme-umgebungen.md](02-systeme-umgebungen.md).

---

## 1. Konten

| Konto | Wofür | Varianten | Quelle |
|---|---|---|---|
| **App-Pool-Identität** (Web) | Web-App in IIS (App-Pool, Standardname `IDEALAKEWMS`); greift auf WMS-DB, AD, Dateien zu | (a) `ApplicationPoolIdentity` = `IIS AppPool\<Pool>`, im Netz als **Computerkonto** `DOMÄNE\SERVER$`; (b) eigenes AD-Konto | `scripts/deploy/Install-IdealAkeWms.ps1:57,289,311-317`, `scripts/deploy/IdealAkeWms.Deploy.psm1:105-117` |
| **Dienstkonto** (`IDEALAKEWMSService`) | Abgleiche, Mails, Sage-Buchungen, CSV-Ablage | (a) `LocalSystem` (Registrierung laut README ohne Konto), im Netz als `DOMÄNE\SERVER$`; (b) AD-Konto mit Recht „Als Dienst anmelden“ (setzt das Installationsskript) | `README.md:838-850`, `scripts/deploy/Install-IdealAkeWms.ps1:371-404`, [[fallstricke]] §32 |
| **Ausführendes Konto Update-Skript** | `deploy\Update-IdealAkeWms.ps1` in einer Administrator-PowerShell am Zielserver | Windows-Administrator am Server + SQL-Rechte (Abschnitt 2.3) | `secondbrain/aufgaben/2026-11-11-buendel-deploy-update-skript.md:7-14` |
| **SData-Servicebenutzer** | Sage-Schnittstelle (lesen und schreiben) | ein Benutzer je Sage-Installation | [[sage-sdata-uebersicht]] „Zugang“, [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]] |
| **SMTP-Benutzer** (optional) | Anmeldung am Mail-Relay, meist nicht nötig | leer = anonym; gesetzt ohne `SmtpUseSsl` → Warnung im Dienst-Log und `Pruefung/SmtpBenutzer` | `IDEALAKEWMSService/Services/MailService.cs:70-86` |
| **`admin`** (WMS-Benutzer) | erster Anmelde-Benutzer, Rolle `admin` | wird beim ersten Start mit dem Standard-Erstkennwort `wms-admin` und Pflichtwechsel angelegt (seit v1.99.28; ebenso, wenn `PasswordHash` geleert wurde) – **sofort** anmelden | `IdealAkeWms/Services/AdminSeed.cs`, ADR 0027 |

Das Installationsskript schlägt **ein** AD-Konto für App-Pool **und** Dienst vor (Quelle: `scripts/deploy/Install-IdealAkeWms.ps1:71-72`).
Welches Konto auf welchem System tatsächlich läuft, ist im Brain nur teilweise festgehalten: Im Protokoll AKE Test-IDEAL steht das Konto
`akenb\svc_idealakewms` (Quelle: `secondbrain/aufgaben/2026-11-12-deploy-fahrplan-buendel-v1-99.md:53`) – ob als App-Pool, Dienst oder
ausführendes Konto: **zu prüfen**. Das Update-Skript liest die tatsächlichen Identitäten selbst aus und zeigt sie an
(Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:220-222`). Prüfen von Hand:

```powershell
# Dienstkonto (nur lesen)
Get-CimInstance Win32_Service -Filter "Name='IDEALAKEWMSService'" | Select-Object Name, StartName
# App-Pool-Identitaet (nur lesen)
Import-Module WebAdministration; (Get-Item IIS:\AppPools\IDEALAKEWMS).processModel | Select-Object identityType, userName
```

---

## 2. SQL-Server-Rechte

Alle Verbindungen nutzen **Windows-Anmeldung** (`Trusted_Connection`); in den Dateien steht kein SQL-Kennwort.
(Quelle: `IdealAkeWms/appsettings.json:3-5`, `IDEALAKEWMSService/appsettings.json:3-6`)

### 2.1 Laufzeit

| Konto | Datenbank | Benötigt | Quelle |
|---|---|---|---|
| App-Pool | WMS (`IDEAL_AKE_WMS` bzw. Test-DB) | `db_datareader`, `db_datawriter`, `EXECUTE` | `scripts/deploy/Install-IdealAkeWms.ps1:482-484` |
| App-Pool | WMS | zusätzlich `db_ddladmin`, **nur** wenn Schema-Änderungen per App-Start (`Migrate()`) eingespielt werden sollen – die Web-App ruft `Migrate()` bei **jedem** Start auf | `Install-IdealAkeWms.ps1:483-484`, `IdealAkeWms/Program.cs:206` |
| App-Pool (AKE) | Sage `ake` | Lesen auf `vw_AKE_Kommissionierung_StuecklistenDB` (datenbankübergreifend über die WMS-Verbindung) | `IdealAkeWms/Data/Repositories/BomRepository.cs:44` – **zu prüfen**, ob vergeben |
| App-Pool (AKE) | OSEON `T1000_V01_V001` | Ausführen von `sp_AKE_Kommissionierung_OseonStuecklistenDB` | `BomRepository.cs:69-71` – **zu prüfen** |
| Dienst | WMS | `db_datareader`, `db_datawriter`, `EXECUTE` | `Install-IdealAkeWms.ps1:482-483` |
| Dienst | Sage-Mandant | Lesen auf Views `vw_IDEAL_AKE_WMS_*` bzw. AKE-Views und KHK-Tabellen; IDEAL zusätzlich Lesen in `PPSImport` | `Install-IdealAkeWms.ps1:485-486`, [[integrationen]] „Sage“ |
| Dienst (AKE) | OSEON, enaio | Lesen auf die genutzten Tabellen/Views | [[integrationen]] „OSEON“, „enaio“ |
| Dienst | Sage-Mandant (seit v1.99.71, erst vor dem Einschalten der Lagerplatz-Anlage) | `INSERT` auf `KHKLagerplaetze`, `INSERT, UPDATE` auf `KHKLagerorte`, `EXECUTE` auf `spSysTan` (SELECT nur falls kein `db_datareader`) | [05 § 4b](05-schnittstellen.md), ADR 0036 |

Läuft ein Konto als `ApplicationPoolIdentity` oder `LocalSystem`, braucht der SQL Server einen Login für das **Computerkonto**
`DOMÄNE\SERVER$`. (Quelle: `Install-IdealAkeWms.ps1:316,404`)

**Nie** schreibend auf Sage-Datenbanken berechtigen: das WMS schreibt an Sage nur über SData. (Quelle: [[integrationen]] Korrektur 2026-11-02)

**Ausnahme seit v1.99.71:** nur die drei Objekt-Rechte der Lager-/Lagerplatz-Anlage oben (keine Rolle `db_datawriter`, kein `db_owner`);
alle anderen Schreibwege an Sage bleiben SData. (Quelle: ADR 0036, Spec `2026-11-24-lagerplatz-anlage-wms-nach-sage`)

### 2.2 Update und Installation

| Bedarf | Quelle |
|---|---|
| ausführendes Konto: `db_owner` auf der Ziel-DB, Recht `BACKUP DATABASE`, Lesen in `msdb.sysjobs`; `ALTER` auf jede Ziel-DB; SQL Server ≥ 2017 | `secondbrain/aufgaben/2026-11-11-buendel-deploy-update-skript.md:14`, `secondbrain/aufgaben/2026-11-12-deploy-fahrplan-buendel-v1-99.md:28` |
| Backup-Ordner auf dem SQL-Server mit Schreibrecht für das **Instanzkonto** | `secondbrain/aufgaben/2026-11-12-deploy-fahrplan-buendel-v1-99.md:32` → [10-backup-restore.md](10-backup-restore.md) |
| `sqlcmd` auf dem Web-/Dienstserver | `secondbrain/aufgaben/2026-11-11-buendel-deploy-update-skript.md:13` |

### 2.3 Diagnose

Für Diagnose nur ein **lesendes** SQL-Konto verwenden ([08-diagnose-abfragen.md](08-diagnose-abfragen.md)). (Quelle: `docs/betrieb/AGENTS.md` §2)

---

## 3. Ordner- und Freigaberechte

Das Update-Skript prüft diese Liste bei jedem Lauf und setzt fehlende Rechte nur **hinzu** (`icacls /grant`, nach Bestätigung, vorherige
ACL wird gesichert). (Quelle: `scripts/deploy/IdealAkeWms.Deploy.psm1:3,127-160,180-186`)

| Ordner | Konto | Recht | Pflicht | Quelle |
|---|---|---|---|---|
| Web-Programmordner | App-Pool | Lesen + Ausführen | ja | `IdealAkeWms.Deploy.psm1:144` |
| `<Web>\logs` (Serilog `idealakewms-*.log` und `stdout`) | App-Pool | Ändern | nein (Logs fehlen sonst) | `IdealAkeWms.Deploy.psm1:152`, `IdealAkeWms/appsettings.json:23`, `IdealAkeWms/web.config:13-14` |
| `<Web>\App_Data` (Fotos unter `App_Data\Fotos\Kommissionierung`) | App-Pool | Ändern | **ja** – ohne Schreibrecht startet die App nicht | `scripts/deploy/nachlauf.psd1:208-210`, `IdealAkeWms/Services/PhotoStorage.cs:5,11` |
| DataProtection-Schlüssel (`%ProgramData%\IdealAkeWms\DataProtection-Keys` oder `DataProtection:KeysPath`) | App-Pool | Ändern (legt Schlüssel an) | ja, wenn Pfad gesetzt | `IdealAkeWms.Deploy.psm1:153`, `IdealAkeWms/Program.cs:42-46` |
| dito | Dienst | **Lesen** (legt nie Schlüssel an) | ja | `scripts/deploy/nachlauf.psd1:212-214`, [[fallstricke]] §33 |
| `<Web>\Branding` (Briefkopf) | App-Pool | Lesen | nein (Ersatz-Briefkopf) | `IdealAkeWms.Deploy.psm1:154` |
| `%TEMP%\IdealAkeWms-Pdf` im Profil des App-Pool-Kontos | App-Pool | Schreiben; App-Pool `loadUserProfile = true` | nein (PDF-Knopf fehlt sonst) | `IdealAkeWms.Deploy.psm1:213`, `scripts/deploy/Install-IdealAkeWms.ps1:310` |
| Dienst-Programmordner | Dienst | Lesen + Ausführen | ja | `IdealAkeWms.Deploy.psm1:155` |
| `<Dienst>\logs` | Dienst | Ändern | ja | `IdealAkeWms.Deploy.psm1:156` |
| UNC-Ziel `WareneingangCsvPfad` (AKE) | Dienst (bei `LocalSystem`: `DOMÄNE\SERVER$`) | „Ändern“ auf **Freigabe und NTFS** | wenn CSV an | [[fallstricke]] §32, `scripts/deploy/nachlauf.psd1:334-337` |
| `MailSettings:PickupDirectory` (nur DEV/QA) | Dienst | Schreiben (Ordner wird angelegt) | – | `IDEALAKEWMSService/Services/MailService.cs:60-63` |

Der Ordner `App_Data` wird von IIS nicht ausgeliefert. (Quelle: `IdealAkeWms/Services/PhotoStorage.cs:5`)

---

## 4. Anmeldung: Windows-Auth, AD, Negotiate

- **Zwei Wege, eine Autorisierung:** Formular-Login und Windows-SSO führen beide in dieselbe App-Sitzung; Rechte prüft die App nur über die
  Sitzung und ihre Rollen – nie über AD-Gruppen. (Quelle: [[0002-dual-auth-session-login-plus-windows-sso]] „Entscheidung“)
- **IIS:** Hosting in-process, **Windows- und anonyme** Authentifizierung beide an. Wird „anonym“ abgeschaltet, sind Geräte ohne Domäne
  ausgesperrt. Die Auth-Abschnitte müssen auf Server-Ebene entsperrt sein (sonst Fehler 500.19).
  (Quelle: `IdealAkeWms/web.config:5-10`, [[fallstricke]] §4 „AddNegotiate() ist unter IIS in-process falsch“, `scripts/deploy/Install-IdealAkeWms.ps1:295-297`)
- **Schalter:** `WindowsAuthAktiv` (aus). Zuordnung Windows-Konto → WMS-Benutzer über das Feld `WindowsUserName` (SAM-Name, ohne Beachtung
  von Groß/Klein). (Quelle: [[0002-dual-auth-session-login-plus-windows-sso]] „Entscheidung“)
- **AD-Abfrage** nur beim Anlegen von Benutzern (Gruppe `WindowsAuthBerechtigungsgruppe`, Domäne `Security:AdDomain`).
  (Quelle: `IdealAkeWms/Services/ActiveDirectoryService.cs:22-31`)
- **Protected Users / SPN:** Mitglieder der AD-Gruppe *Protected Users* können kein NTLM. Dann ist Kerberos nötig, und Kerberos braucht einen
  SPN für den Zieldienst. Belegt ist der Fall für die **SQL-Anmeldung** des Update-Kontos am IDEAL-Test: Fehler 18452 „nicht vertrauenswürdige
  Domäne“, weil für die benannte Instanz `IdealSQL2\SAGE100TEST` kein SPN existierte; das Konto wurde vorerst aus *Protected Users* genommen.
  Offen: SPNs `MSSQLSvc/IdealSQL2.IDEAL.IDEAL-AKE.AT:SAGE100TEST` und `…:<Port>` setzen, dann das Konto wieder aufnehmen; an AKE Konto und
  SPN von `AKESQL20` prüfen. (Quelle: `secondbrain/aufgaben/2026-11-11-buendel-deploy-update-skript.md:28-45`,
  `secondbrain/aufgaben/2026-11-12-deploy-fahrplan-buendel-v1-99.md:29-31`)
- **SPN für die Web-Seite** (`HTTP/<Hostname>`, bei App-Pool mit AD-Konto auf dieses Konto): im Brain nicht dokumentiert – **zu prüfen**, bevor
  Benutzer aus *Protected Users* per SSO arbeiten sollen.
- **Abgelaufene Tickets** einer alten RDP-Sitzung führten am IDEAL-Test zu LDAP-Fehler 0x31; Ab- und Anmelden half.
  (Quelle: `secondbrain/aufgaben/2026-11-11-buendel-deploy-update-skript.md:36`)
- **Sitzung:** Cookie `IdealAkeWms.Session`, `HttpOnly`, 8 Stunden Leerlauf. (Quelle: `IdealAkeWms/Program.cs:54-61`)
- **Formular-Anmeldung nur mit Kennwort (seit v1.99.28, Regel R):** ohne gespeicherten `PasswordHash` meldet das Formular „Für diesen Benutzer ist
  kein Kennwort hinterlegt …“ und legt keine Sitzung an; Windows-SSO bleibt unverändert. (Quelle: `IdealAkeWms/Controllers/AccountController.cs:59-62`,
  ADR `0027`)
- **Erstkennwort mit Pflichtwechsel (Stand v1.99.29 inkl. QA-Nachbesserung B-2/B-3):** `/Users/Create` und `/Users/Edit` – Kennwort eintragen
  oder „Erstkennwort erzeugen“ (12 Zeichen, **einmal** in der Erfolgsmeldung, nicht gespeichert/geloggt). Der Haken `MussKennwortAendern` ist beim
  Öffnen **nicht** vorbelegt, das Formular setzt ihn, sobald ein Kennwort eingegeben/erzeugt wird; für Gerätekonten abwählen. **Ohne
  gespeichertes Kennwort wird der Haken nie gespeichert** (Erfolgsmeldung mit Hinweis). Der `/Users`-Hinweis „offenes Erstkennwort“ zählt nur
  aktive Benutzer **mit** Kennwort und Haken. Der Haken fällt weg nach dem Pflichtwechsel oder einem selbst gesetzten Kennwort in *Mein Profil*.
  (Quelle: `IdealAkeWms/Controllers/UsersController.cs:80-84,333-349`, `IdealAkeWms/Controllers/AccountController.cs:137,270`;
  Anwender-Anleitung `docs/anwender/13-anmeldung-und-benutzer.md`)

Fehlerbilder rund um Anmeldung (400 bei POST, Button ohne Wirkung): [07-troubleshooting.md](07-troubleshooting.md), Hintergrund [[fallstricke]] §4.

---

## 5. Rollenkonzept der App

- **Feste Rollen-Keys** im Code (`IdealAkeWms/Models/RoleKeys.cs:5-29`): `admin`, `masterdata`, `masterdata_read`, `picking`, `stock`,
  `stock_keyuser`, `stock_read`, `tracking`, `reporting`, `leitstand`, `fa_completion`, `vorbau`, `lagerbestellung`, `glasbestellung`,
  `bde_user`, `bde_shiftlead`, `bde_admin`, `beschichtungsauftrag`, `beschichtungsauftrag_schreiben`, `einkauf`.
- **`admin` ist Wildcard:** jede Berechtigungsprüfung lässt `admin` zu. (Quelle: `IdealAkeWms/Services/CurrentUserService.cs:83-111`,
  [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]])
- **Nur für `admin`:** Benutzer, Rollen, Arbeitsplätze, Einstellungen (`/Settings`), Service-Einstellungen (`/ServiceSettings`),
  Standorteinstellungen, Aktivitäts-Protokoll, BDE-Schichtkalender, Arbeitsgänge, Sage-Test. (Quelle: [[controller]] Zeile „RequireAdminAccess“,
  `IdealAkeWms/Controllers/ServiceSettingsController.cs:19`, `SettingsController.cs:11`, `UsersController.cs:10`, `RolesController.cs:10`)
- **Vergabe:** ausschließlich **pro Benutzer** unter Benutzer → Bearbeiten (Rollen ankreuzen); keine Ableitung aus AD-Gruppen (seit v1.23.0).
  Rollen selbst unter `/Roles`. Übersicht für Anwender: `/Users/RoleOverview` (von Hand gepflegt).
  (Quelle: `IdealAkeWms/Controllers/UsersController.cs:141-143,356`, [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]] „Entscheidung“)
- **Wer legt die Rollen an:** 13 Systemrollen beim Web-Start (Quelle: `IdealAkeWms/Program.cs:224-238`); `vorbau` u. a. per SQL-Skript
  (Quelle: `SQL/00_FreshInstall.sql:1584-1585`); `einkauf` und die Beschichtungs-Rollen per Update-Skript-Prüfung bzw. von Hand unter `/Roles`
  (Quelle: [[glossar]] Zeile `einkauf`, `README.md:498`).
- **Module zusätzlich per Schalter:** Ein Modul braucht Rolle **und** eingeschalteten AppSetting-Schalter (kumulativ).
  (Quelle: [[0011-feature-toggles-ueber-appsettings]] „Entscheidung“)
- Vollständige Zuordnung Filter → Rollen → Seiten: [[controller]] Abschnitt „Zugriffsschutz“; Bedeutung je Rolle: [[glossar]] „Rollen-Keys“,
  `README.md:384-405`.

---

## 6. Geheimnisse – wo sie liegen

| Geheimnis | Ablage | Schutz | Quelle |
|---|---|---|---|
| SQL-Zugang | keiner – Windows-Anmeldung des Kontos | – | `IdealAkeWms/appsettings.json:3-5` |
| **SData-Kennwort** | `/ServiceSettings` → `SData:Password`, gespeichert als `dp1:<Schutztext>` | verschlüsselt mit ASP.NET-Core-DataProtection (Schlüsselring der Web-App); Maske zeigt nur „gesetzt / nicht gesetzt / nicht entschlüsselbar“, kein Log, keine API-Ausgabe | [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]] „Entscheidung“, `IdealAkeWms/Services/SageSdata/SdataZugangsdaten.cs:36-61` |
| SData-Kennwort (Übergang) | `SageLagerbuchung:Password` in Dienst-`appsettings.json` und Web-`appsettings.Production.json` | **Klartext** – nur solange `SData:Username` leer; nach Umstellung aus beiden Dateien entfernen (Prüfung `Pruefung/SDataKennwortAppsettings`) | `README.md:535-537`, `scripts/deploy/nachlauf.psd1:162-165` |
| SMTP-Kennwort (falls genutzt) | `MailSettings:SmtpPassword` in der Dienst-`appsettings.json` | Klartext in der Datei; Schutz nur über NTFS-Rechte | [[0008-servicesettings-db-first-mit-typisiertem-katalog]] „Ausnahmen“, `IDEALAKEWMSService/appsettings.json:13` |
| Kennwort eines AD-Kontos für App-Pool/Dienst | IIS-Konfiguration bzw. Dienststeuerung von Windows | von Windows verwaltet; das Installationsskript übergibt es nur | `scripts/deploy/Install-IdealAkeWms.ps1:311-313,402` |
| **DataProtection-Schlüsselring** | `%ProgramData%\IdealAkeWms\DataProtection-Keys` oder `DataProtection:KeysPath` | **unverschlüsselt** im Dateisystem (bewusst begrenztes Schutzziel). Schützt Sitzungs-/Formular-Token und das SData-Kennwort | `IdealAkeWms/Program.cs:36-46`, [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]] „Negativ / Risiken“ |
| Kennwörter lokaler WMS-Benutzer | Tabelle `Users`, nur Hash (PBKDF2/HMAC-SHA256) | nicht umkehrbar | `IdealAkeWms/Services/PasswordService.cs:8-14` |

**Schlüsselring-Folgen:**
- Gelöscht oder fremd (z. B. DB auf ein anderes System kopiert): SData-Kennwort „nicht entschlüsselbar“ → der Dienst sendet **nichts** an Sage,
  bis es neu eingegeben ist; kein stiller Rückfall auf die Datei. (Quelle: [[fallstricke]] §33, [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]])
- Ohne dauerhaften Schlüsselring erzeugt der App-Pool bei jedem Recycle neue Schlüssel → Formular-POSTs (z. B. Abmelden) scheitern mit 400.
  (Quelle: `IdealAkeWms/Program.cs:36-41`, [[fallstricke]] §4 „Logout ohne ValidateAntiForgeryToken“)
- Der Dienst muss denselben Pfad lesen wie das Web (`DataProtection:KeysPath` in beiden Dateien gleich, falls gesetzt).
  (Quelle: `README.md:529-530`)
- Sicherung des Schlüsselrings: [10-backup-restore.md](10-backup-restore.md). Wer Lesezugriff auf Schlüsselordner **und** Datenbank hat, kann
  das SData-Kennwort entschlüsseln – Ordner- und Backup-Rechte entsprechend eng halten.

---

## 7. Was nie im Klartext stehen darf

- Kein Kennwort in `SData:BaseUrl` (Form `benutzer:passwort@` wird abgelehnt). (Quelle: `README.md:539-540`)
- Kein SData-Kennwort per SQL direkt in `ServiceSettings` eintragen – ein Wert ohne `dp1:` gilt als „nicht entschlüsselbar“; Eingabe nur über
  die Maske. (Quelle: `README.md:532-534`)
- Keine Geheimnisse ins Brain, in Tickets, Chats oder dieses Handbuch; nur Platzhalter wie `<Kennwort>` und der Ablageort.
  (Quelle: [[sage-sdata-uebersicht]] „Zugang“, `docs/betrieb/AGENTS.md` §6)
- Nach der Umstellung auf `/ServiceSettings`: `SageLagerbuchung:Password` aus **beiden** appsettings-Dateien entfernen. (Quelle: `README.md:535-537`)
- Keine SQL-Logins mit Kennwort in Verbindungszeichenfolgen; Windows-Anmeldung beibehalten. (Quelle: `IdealAkeWms/appsettings.json:3-5`)

---

## 8. Bekannte Sicherheitsrisiken (Stand Code v1.99.28)

| Nr. | Befund | Fundstelle | Empfehlung |
|---|---|---|---|
| R1 | ~~admin ohne Kennwort~~ **seit v1.99.28 entschärft:** admin bekommt das Standard-Erstkennwort `wms-admin` mit Pflichtwechsel. Restrisiko: bis zur ersten Anmeldung kennt jeder mit Betriebshandbuch das Kennwort | `IdealAkeWms/Services/AdminSeed.cs`, ADR 0027 | direkt nach Installation bzw. Rückweg als admin anmelden; `/Users` zeigt offene Erstkennwörter |
| R2 | ~~Benutzer ohne Kennwort melden sich mit leerem Kennwort an~~ **behoben in v1.99.28:** das Formular meldet nur mit gespeichertem Kennwort an (Ausnahme nur `Development`, mit Hinweis). Vom Admin vergebene Kennwörter können einen Pflichtwechsel tragen | `IdealAkeWms/Controllers/AccountController.cs` (Login), ADR 0027 | vor dem Update `Pruefung/BenutzerOhneKennwort` beachten (sperrt Prod) |
| R3 | DataProtection-Schlüsselring liegt unverschlüsselt im Dateisystem | [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]] „Negativ / Risiken“ | Ordnerrechte eng; Folgeaufgabe DPAPI laut ADR |
| R4 | Mails ohne Verschlüsselung, wenn `SmtpUseSsl = false` (kein STARTTLS); ein gesetzter SMTP-Benutzer würde unverschlüsselt übertragen. Seit v1.99.28 Warnung im Dienst-Log beim Senden und `Pruefung/SmtpBenutzer` | `IDEALAKEWMSService/Services/MailService.cs:75-86` | anonymes internes Relay oder `SmtpUseSsl = true` |
| R5 | Das Installationsskript schreibt ein angegebenes SMTP-Kennwort im Klartext in die Dienst-`appsettings.json` (mit Warnung). `-SageLagerbuchungCredential` gibt es seit v1.99.28 nicht mehr | `scripts/deploy/Install-IdealAkeWms.ps1` Schritt 5 | SData-Zugang in `/ServiceSettings` eintragen; SMTP anonym bevorzugen |
| R6 | TLS-Prüfung zum Sage-Server abschaltbar (`SageLagerbuchungSslZertifikatPruefen = false`) | `IdealAkeWms/Models/ServiceSettingDefinitions.cs:260` | nur auf Testsystemen; Warnung erscheint in `/ServiceSettings` und im Start-Log |
| R7 | Testsysteme mit reiner HTTP-Bindung (z. B. IDEAL-Test Port 88): Sitzungs-Cookie und Formulardaten gehen unverschlüsselt; seit v1.99.28 tragen Sitzungs- und Antiforgery-Cookie bei HTTPS das `Secure`-Attribut (`SameAsRequest`), bei HTTP weiter nicht | `scripts/deploy/ziele.psd1:4`, `IdealAkeWms/Program.cs` (`AddSession`, `AddAntiforgery`) | Prod nur HTTPS (ist laut Zielliste so); Test nach Möglichkeit ebenfalls |
| R8 | `MailSettings:PickupDirectory` in Produktion gesetzt = keine Mail geht raus, Protokoll meldet trotzdem „gesendet“ (kein Sicherheits-, aber ein stilles Betriebsrisiko) | [[fallstricke]] §5 „Mails pruefen ohne Versand“ | Prüfung `Pruefung/MailPickup` des Update-Skripts beachten |

Klartext-Kennwörter in den geprüften Konfigurationsdateien: keine. `IdealAkeWms/appsettings*.json`, `IDEALAKEWMSService/appsettings*.json`
und die Kopien unter `publish/` enthalten nur leere Kennwort-Felder (Quelle: Durchsicht dieser Dateien am 2026-11-14; produktive Dateien auf
den Servern sind nicht im Repository – **zu prüfen** mit der Prüfung `Pruefung/SDataKennwortAppsettings` des Update-Skripts).

Weiter: Installation [03-installation-update-rollback.md](03-installation-update-rollback.md) · Logs [06-logs-ueberwachung.md](06-logs-ueberwachung.md) ·
Architektur [01-architektur-technologie.md](01-architektur-technologie.md).
