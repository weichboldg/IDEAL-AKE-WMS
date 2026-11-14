# 10 – Backup und Wiederherstellung

**Stand:** v1.99.23, 2026-11-14 · **Zielgruppe:** IT-Administratoren · **Zweck:** Was je System gesichert werden muss, was das
Update-Skript selbst sichert und wie eine Wiederherstellung abläuft, ohne dabei Migrationen oder Sage-Buchungen zu verwirren.

Übersicht: [00-index.md](00-index.md). Systeme und Pfade: [02-systeme-umgebungen.md](02-systeme-umgebungen.md). Update/Rückfall:
[03-installation-update-rollback.md](03-installation-update-rollback.md). Konten und Rechte: [09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md).

---

## 1. Was gesichert werden muss

| # | Was | Wo (je System) | Warum / was ginge verloren | Im Update-Paket? | Beleg |
|---|---|---|---|---|---|
| 1 | **WMS-Datenbank** | SQL-Server des Systems (z. B. `AKESQL20.ake.at` / `IDEAL_AKE_WMS`) | alle Bewegungen, Bestände, FA-Status, Benutzer/Rollen, **AppSettings** und **ServiceSettings** (inkl. verschlüsseltem SData-Kennwort), Aktivitäts-Protokoll, Deploy-Marker | nein | `IdealAkeWms/appsettings.json:3`, `README.md:525-527`, `scripts/deploy/Update-IdealAkeWms.ps1:325-333` |
| 2 | **Kommissionierfotos** | `<Web-Ordner>\App_Data\Fotos\Kommissionierung` (bis v1.30.3/v1.95: `<Web>\wwwroot\Fotos\Kommissionierung`, wird beim Start verschoben) | Fotos liegen **nur** als Dateien vor (Name beginnt mit der FA-Nummer), nicht in der DB | nein, wird nie überschrieben | `IdealAkeWms/Services/PhotoStorage.cs:10-15`, `IdealAkeWms/Program.cs:415-419`, `IdealAkeWms/Controllers/Api/PhotoController.cs:35-38` |
| 3 | **Konfigurationsdateien** | Web-Ordner: `appsettings.json`, `appsettings.Production.json`, `web.config`; Dienst-Ordner: `appsettings.json` (+ ggf. `appsettings.<Umgebung>.json`) | Verbindungen, Mail, SData-Rückfall-Zugang, Umgebung, stdout-Log; **enthalten Kennwörter** | nein, werden nie überschrieben | `scripts/deploy/Update-IdealAkeWms.ps1:632-635`, `scripts/deploy/Install-IdealAkeWms.ps1:275-278`, `:352-372` |
| 4 | **DataProtection-Schlüssel** | `DataProtection:KeysPath` der Web-App, sonst `%ProgramData%\IdealAkeWms\DataProtection-Keys` (DEV: `D:\Dev\<FIRMA>\keys`) | ohne sie ist das in der DB gespeicherte SData-Kennwort nicht mehr entschlüsselbar (kein Sage-Aufruf mehr, Kennwort neu eingeben); angemeldete Sitzungen/Formulare werden ungültig | nein | `IdealAkeWms/Services/SageSdata/SdataZugangsdaten.cs:116-122`, `IdealAkeWms/Program.cs:36-46`, `README.md:525-534`, [[2026-10-29-dev-umgebung-lokal]] „Angelegte Objekte“ |
| 5 | **Branding** | `<Web-Ordner>\Branding\` (`briefkopf.html`, Logo, lizenzierte Schriften `NimbusSanNov-*.otf`) | Briefkopf der IDEAL-Druckdokumente; Schriften liegen nicht im Repo | nein, wird nie überschrieben | `README.md:575-583`, `scripts/deploy/Update-IdealAkeWms.ps1:635` |
| 6 | **Dienst-Umgebung** | Registry `HKLM\SYSTEM\CurrentControlSet\Services\<Dienst>`, Wert `Environment` (z. B. `DOTNET_ENVIRONMENT=DEV`) | bestimmt, welche `appsettings.<Umgebung>.json` der Dienst liest | nein | `scripts/deploy/Update-IdealAkeWms.ps1:194-198` |
| 7 | **IIS-Konfiguration** | App-Pool (Identität, AlwaysRunning, Idle 0, `loadUserProfile`), Site, Bindungen, Zertifikat | Neuaufbau nötig, wenn der Server verloren geht | nein | `scripts/deploy/Install-IdealAkeWms.ps1:300-337` |
| 8 | **Programmdateien** (optional) | Web- und Dienst-Ordner bzw. das vorherige Paket `C:\<FIRMA>\deploy\v<alt>\` | nur für einen Rückfall auf die alte Version nötig | – | `scripts/deploy/Update-IdealAkeWms.ps1:632-639` (kopiert ohne eigene Sicherung) |
| 9 | Logs (optional, Nachweis) | `<Web>\logs\` (`idealakewms-*.log`, `stdout_*`), `<Dienst>\logs\` (`service-*.log`, `sync\`, `notifications\`), `C:\<FIRMA>\deploy-logs\` | Fehlersuche, Deploy-Nachweis; die App hält selbst 30 Tagesdateien | – | `IdealAkeWms/appsettings.json:23-25`, `IdealAkeWms/web.config:13-14`, `IDEALAKEWMSService/appsettings.json:64-108`, `scripts/deploy/Update-IdealAkeWms.ps1:94` |

Hinweise:
- **Schlüssel und Kennwörter:** Sicherungen von Punkt 3 und 4 enthalten Geheimnisse bzw. das Mittel, sie zu entschlüsseln – Ablage mit
  eingeschränktem Zugriff. Werte nie in Tickets kopieren. ([09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md))
- **Regelmäßige DB-Sicherung** (Wartungsplan, Aufbewahrung, Wiederherstellungsmodell): **zu prüfen** – im Repo und im Brain nicht
  beschrieben; liegt beim SQL-Betrieb des jeweiligen Standorts.
- **Sicherung der IIS-Konfiguration:** **zu prüfen** – kein dokumentierter Ablauf; mindestens die Werte aus Kapitel
  [02-systeme-umgebungen.md](02-systeme-umgebungen.md) notieren.
- **SQL-Agent-Jobs** sind für den WMS-Import nicht mehr aktiv (archiviert bzw. nur manueller Notfall-Weg). (Quelle: [[integrationen]]
  Tabelle „Was gelesen wird“ und „Fallen dieser Integration“)
- Das Aktivitäts-Protokoll in der DB wird nach `Cleanup:AktivitaetsprotokollAufbewahrungTage` (Default 180) gelöscht. (Quelle: `README.md:950-951`)

---

## 2. Was das Update-Skript selbst sichert

| Gesichert | Wie | Beleg |
|---|---|---|
| **DB-Backup** (Schritt 2, nach Bestätigung) | `BACKUP DATABASE [<DB>] TO DISK = N'<Ordner>\<DB>_<yyyyMMdd_HHmmss>_vor_v<Version>.bak' WITH COPY_ONLY, CHECKSUM`; Ordner liegt **auf dem SQL-Server** (Instanz-Standard oder `-BackupPfad`), vorher per `sys.dm_os_file_exists` geprüft | `scripts/deploy/Update-IdealAkeWms.ps1:466-511` |
| Backup-Pflicht | Pflicht, wenn ein Haltepunkt-Trockenlauf Treffer hat (sonst Abbruch vor dem Stoppen); ohne Backup muss bestätigt werden, dass eines vorliegt; `SCHARF` und `uebernehmen` nur mit Backup **aus demselben Lauf** | `scripts/deploy/Update-IdealAkeWms.ps1:750-761`, `:512-520` |
| ACL vor Rechteänderung | `icacls … /save` nach `C:\<FIRMA>\deploy-logs\acl_<Zeit>_<Eintrag>.txt` | `scripts/deploy/IdealAkeWms.Deploy.psm1:180-183` |
| Lauf-Nachweis | Transcript, Trockenlauf-Ausgaben, Marker in `dbo.DeployScriptRuns` (mit Ausgabe und Datei-Hash) | `scripts/deploy/Update-IdealAkeWms.ps1:936-937`, `:555`, `:334-340` |

**Nicht** gesichert, aber geschützt: Konfigurationsdateien, `Branding\`, `App_Data\`, `wwwroot\Fotos\` und `logs\` werden beim Kopieren
ausgelassen; das Skript vergleicht Hashes/Dateilisten vorher und nachher und bricht bei einer Änderung ab. Fotos werden vor dem Kopieren
gezählt und nach dem Start mit der Log-Zeile „Kommissionierfotos verschoben: n, uebersprungen: m“ abgeglichen.
(Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:619-639`, `:826-839`, `:870-878`)

**Nicht** gesichert: Programmdateien (werden überschrieben) und DataProtection-Schlüssel. (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:632-639`)

---

## 3. Wiederherstellung

### 3.1 Grundregel

**Datenbankstand und Programmversion gehören zusammen.** Die Web-App spielt beim Start jede fehlende Migration selbst ein. Wird eine ältere
DB zurückgespielt und die neue Web-App gestartet, laufen alle späteren Migrationen ohne die Vorbedingungen der SQL-Skripte (Seeds,
Sonderregeln, Prüfungen). (Quelle: `IdealAkeWms/Program.cs:206`, `scripts/deploy/Update-IdealAkeWms.ps1:317`, [[fallstricke]] §30)
→ Nach einem Restore entweder die passenden alten Programmdateien wiederherstellen **oder** vor dem ersten Start das Update-Skript laufen
lassen.

### 3.2 Datenbank auf demselben System wiederherstellen

1. **Dienst stoppen, dann App-Pool stoppen** (keine Schreibzugriffe, kein `Migrate()`). (Quelle: Reihenfolge wie `scripts/deploy/Update-IdealAkeWms.ps1:764-780`)
2. Passendes Backup wählen (z. B. `<DB>_<Zeit>_vor_v<Version>.bak` aus dem Update-Lauf). Restore mit den Mitteln des SQL-Betriebs
   (SSMS bzw. `RESTORE DATABASE … FROM DISK = … WITH REPLACE`). **zu prüfen:** Standort-Verfahren (Logsicherungen, Zeitpunkt-Restore).
3. Kontrolle nach dem Restore (nur lesend):
   - Datenbank-Benutzer der Konten von App-Pool und Dienst vorhanden und zugeordnet? Nach einem Restore aus einer anderen Umgebung bleiben
     Benutzer ohne passendes Login zurück. (Quelle: [[2026-10-29-dev-umgebung-lokal]] „Nachtrag 2026-10-30 (Dienstkonto)“, verwaiste Benutzer)
   - Letzte Einträge in `__EFMigrationsHistory` mit der installierten Programmversion vergleichen. Abfragen:
     [08-diagnose-abfragen.md](08-diagnose-abfragen.md).
4. **Programmstand angleichen:**
   - Backup stammt aus **derselben** Version wie die Programmdateien → weiter mit Schritt 5.
   - Backup ist **älter** (z. B. „vor_v1.99.23“) und die neuen Programmdateien sind installiert → entweder alte Programmdateien
     zurückkopieren, oder das Update-Skript für dieses System erneut ausführen (spielt die Schema-Skripte in richtiger Reihenfolge ein).
     Web **nicht** vorher starten. (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:782-808`)
5. App-Pool starten, dann Dienst starten. Fußzeile, `/SyncLog`, `logs\` prüfen ([06-logs-ueberwachung.md](06-logs-ueberwachung.md)).
6. **Sage abgleichen:** Lagerbuchungen und Wareneingangsbelege, die nach dem Backup an Sage gingen, sind in Sage, aber nicht mehr im
   WMS. **zu prüfen** im Einzelfall mit dem Fachbereich (Liste „Sage-Lagerbuchungen“ / „Sage-Wareneingänge“, Sage-Belege mit Memo
   `SM#…#`/`WE#…#`). (Quelle: `README.md:726-729`, [[integrationen]] „Idempotenz“)
7. Deploy-Marker: Ein Restore auf einen Stand vor einem Daten-/Einmal-Lauf entfernt dessen Marker; der nächste Update-Lauf bietet ihn
   erneut an. Vor `SCHARF`/`uebernehmen` die Zählung lesen. (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:325-333`, `:534-544`)

### 3.3 Datenbank auf ein anderes System kopieren (z. B. Prod-Kopie für Test)

Zusätzlich zu 3.2:
- **Vor dem ersten Dienst-Start** Sage-schreibende und Mail-Schalter ausschalten, sonst bucht bzw. mailt das Testsystem mit den
  Produktivwerten: `SageLagerbuchungAktiv`, `SageLagerbuchungUmbuchungAktiv`, `SageWareneingangBelegAktiv`, `WareneingangCsvAktiv`,
  `Lieferverzug:MailAktiv`, `ErrorNotification:Enabled`, `Notifications:MeldebestandEnabled`, Mail-Syncs. So wird DEV abgesichert.
  (Quelle: [[2026-10-29-dev-umgebung-lokal]] Ablauf Schritt 3, `README.md:121-131`)
- **System-Kürzel** `Sync:SageSystemKuerzel` setzen (`DEV`, `AT`, `ATI`, `IT`), wenn das System in eine gemeinsame Sage-Testdatenbank
  schreibt; in Produktion leer. (Quelle: `README.md:129`, `README.md:730-734`)
- **SData-Kennwort neu eingeben:** auf einem anderen System ist es mit dessen Schlüsselring nicht entschlüsselbar („SData-Kennwort nicht
  entschlüsselbar – in den Service-Einstellungen neu eingeben“). (Quelle: `README.md:532-534`)
- `Notifications:AppBaseUrl` auf das neue System stellen (Links in Mails). (Quelle: [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „AKE Test-IDEAL“, letzter Punkt)
- Niemals zwei Systeme (zwei Dienste) auf **dieselbe** DB zeigen lassen: der Doppelbuchungs-Schutz gilt nur für genau eine Dienst-Instanz.
  Das Update-Skript bricht ab, wenn zwei gewählte Systeme dieselbe DB haben. (Quelle: [[fallstricke]] §8 „Idempotenz nur bei genau EINER
  Worker-Instanz“, `scripts/deploy/Update-IdealAkeWms.ps1:229-234`)

### 3.4 Kommissionierfotos wiederherstellen

1. Dateien zurück nach `<Web>\App_Data\Fotos\Kommissionierung` kopieren (Dateinamen unverändert, sie beginnen mit der FA-Nummer).
   (Quelle: `IdealAkeWms/Services/PhotoStorage.cs:10-11`, `IdealAkeWms/Controllers/Api/PhotoController.cs:35-38`)
2. App-Pool-Identität braucht „Ändern“ auf `App_Data` (vererbt) – ohne Schreibrecht startet die App nicht. Der nächste Update-Lauf prüft
   und setzt es in Schritt 5a. (Quelle: `scripts/deploy/nachlauf.psd1:208-211`)

### 3.5 DataProtection-Schlüssel wiederherstellen

1. Ordner an den konfigurierten Pfad zurückkopieren (Wert `DataProtection:KeysPath` der Web-App; der Dienst braucht **denselben** Wert in
   seiner `appsettings.json`). (Quelle: `README.md:529-531`, `IdealAkeWms/Services/SageSdata/SdataZugangsdaten.cs:116-122`)
2. Rechte: App-Pool „Ändern“, Dienstkonto „Lesen“. (Quelle: `scripts/deploy/IdealAkeWms.Deploy.psm1:153`, `scripts/deploy/nachlauf.psd1:212-215`)
3. Sind die Schlüssel verloren: SData-Kennwort unter `/ServiceSettings` neu eingeben; Anwender müssen sich ggf. neu anmelden. Der Dienst
   legt selbst keine Schlüssel an. (Quelle: `README.md:529-534`, `IdealAkeWms/Services/SageSdata/SdataZugangsdaten.cs:134-135`)

### 3.6 Konfiguration und Branding wiederherstellen

- `appsettings*.json` und `web.config` zurück in Web- bzw. Dienst-Ordner; danach App-Pool bzw. Dienst neu starten. **zu prüfen:** ob
  eine Änderung der `appsettings` ohne Neustart greift (im Code nicht belegt; die DB-Einstellungen unter `/ServiceSettings` werden je Lauf
  gelesen). (Quelle: `README.md:928-933`)
- `Branding\` zurück in den Web-Ordner, Leserecht für die App-Pool-Identität; Pfad in „Standorteinstellungen → Firmendaten“ muss passen.
  Fehlt die Datei, druckt die App mit Ersatz-Kopf weiter. (Quelle: `README.md:578-590`)

### 3.7 Server neu aufbauen

Neuinstallation mit dem Installationsskript in einer leeren Umgebung ([03-installation-update-rollback.md](03-installation-update-rollback.md)
Abschnitt 4), danach **statt** Teil 1/2 die gesicherte DB zurückspielen und Konfiguration, Schlüssel, Fotos und Branding aus Abschnitt 1
zurückkopieren; Programmversion passend zur DB wählen (3.1). **zu prüfen:** Dieser Ablauf ist nicht erprobt und nicht dokumentiert; das
Installationsskript verlangt eine leere DB nur für Teil 1 (`-SqlTeil1Ausfuehren`). (Quelle: `scripts/deploy/Install-IdealAkeWms.ps1:208-213`, `:246-249`)

---

## 4. DEV-Umgebung

DEV-Datenbanken werden aus Kopien der Test-DBs wiederhergestellt: `D:\akewms.bak`, `D:\idealwms.bak`. Ein DEV-Deploy hebt die DEV-DB auf
den Migrationsstand des Zweigs; bei Problemen Restore aus diesen Dateien. Nach jedem Restore die DEV-Absicherung (Sage-Buchung, Syncs,
Mails aus) vor dem ersten Start ausführen. (Quelle: [[0018-qa-agent-deployt-und-testet-auf-lokaler-dev-umgebung]] „Folgen“,
[[2026-10-29-dev-umgebung-lokal]] Ablauf Schritte 2–3)
