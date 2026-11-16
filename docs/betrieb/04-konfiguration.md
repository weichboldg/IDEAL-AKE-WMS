# 04 – Konfiguration

**Stand:** v1.99.29, 2026-11-16 (Einstellungen bis v1.99.26 nachgezogen; v1.99.28/1.99.29 ohne neue Einstellung) · **Zielgruppe:** IT-Administratoren (ohne Entwicklerwissen) · **Zweck:** Wo welche Einstellung liegt,
wer sie liest und wann eine Änderung wirkt – und welche Schalter nach außen wirken (Sage, Mails, Dateien).

Übersicht aller Kapitel: [00-index.md](00-index.md). Fremdsysteme hinter den Schaltern: [05-schnittstellen.md](05-schnittstellen.md).
Konten, Rechte und Geheimnisse: [09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md).

---

## 1. Die drei Ebenen auf einen Blick

| Ebene | Wo gespeichert | Gepflegt über | Wer liest | Wann wirkt eine Änderung |
|---|---|---|---|---|
| **1. appsettings** | Dateien `appsettings.json` (+ `appsettings.Production.json`) im Programmordner von Web und Dienst | Texteditor auf dem Server (Administrator) | Web und Dienst beim Start | Web: App-Pool recyceln; Dienst: neu starten (siehe Abschnitt 2.4) |
| **2. ServiceSettings** | Tabelle `dbo.ServiceSettings` der WMS-Datenbank | Seite `/ServiceSettings` (nur Rolle `admin`) | vor allem der Dienst, teils die Web-App | Dienst: beim **nächsten Lauf** des betroffenen Blocks, ohne Neustart |
| **3. AppSettings** | Tabelle `dbo.AppSettings` der WMS-Datenbank | Seiten `/Settings` und Standorteinstellungen (nur `admin`) | vor allem die Web-App, teils der Dienst | nach höchstens ca. 2 Minuten (Zwischenspeicher) |

Merksatz: **Verbindungen und Server-Geheimnisse stehen in den Dateien, Verhalten steht in der Datenbank.**
(Quelle: [[0008-servicesettings-db-first-mit-typisiertem-katalog]] Abschnitt „Entscheidung“; `secondbrain/README.md:49-54`)

Ausnahme seit v1.99.14: Benutzer und Kennwort der Sage-Schnittstelle (SData) liegen in den ServiceSettings, das Kennwort verschlüsselt
(Quelle: [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]]; Details in [09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md)).

---

## 2. Ebene 1 – appsettings-Dateien

### 2.1 Welche Datei gilt

- **Web:** `appsettings.json` wird zuerst gelesen, danach überschreibt `appsettings.<Umgebung>.json`. Die Umgebung setzt die `web.config`
  auf `Production`, also gilt `appsettings.Production.json`. (Quelle: `IdealAkeWms/web.config:17-19`)
- Die `appsettings.json` aus dem Paket zeigt auf `AKESQL20.ake.at` / `IDEAL_AKE_WMS`. Die Neuinstallation schreibt deshalb die echten
  Verbindungen in `appsettings.Production.json`. (Quelle: `IdealAkeWms/appsettings.json:3`, `scripts/deploy/Install-IdealAkeWms.ps1:258-278`)
- **Dienst:** `appsettings.json` im Dienst-Ordner; die Umgebung kommt aus der Dienst-Registry (`DOTNET_ENVIRONMENT`), falls gesetzt.
  (Quelle: [[fallstricke]] §30 „DB-Ermittlung braucht die Umgebung …“)
- Das Update-Skript überschreibt `appsettings*.json` und `web.config` **nie**; Änderungen an diesen Dateien bleiben über Updates erhalten.
  (Quelle: `README.md:900-901`, `scripts/deploy/Update-IdealAkeWms.ps1:838-839`)

### 2.2 Inhalt – Web-App

| Abschnitt / Key | Wofür | Gelesen von |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | WMS-Datenbank | Web (Quelle: `IdealAkeWms/Program.cs:26-34`) |
| `ConnectionStrings:OseonConnection` | OSEON-Stücklisten-Fallback (AKE). Key muss existieren, auch leer (IDEAL) | Web (Quelle: `IdealAkeWms/Data/Repositories/BomRepository.cs:18-19`, `scripts/deploy/Install-IdealAkeWms.ps1:263`) |
| `ConnectionStrings:EnaioDmsConnection` | steht in der Datei, wird von der Web-App laut Code-Suche **nicht** gelesen (nur vom Dienst) | – (Quelle: Suche `GetConnectionString` über `IdealAkeWms/`) |
| `Serilog:*` | Protokoll-Dateien `logs/idealakewms-*.log`, 30 Tage | Web (Quelle: `IdealAkeWms/appsettings.json:7-30`) → [06-logs-ueberwachung.md](06-logs-ueberwachung.md) |
| `Security:AdDomain` | Domäne für die AD-Abfrage beim Benutzer-Anlegen; leer = aktuelle Domäne | Web (Quelle: `IdealAkeWms/Services/ActiveDirectoryService.cs:28-31`) |
| `DataProtection:KeysPath` | Ordner des Schlüsselrings; leer = `%ProgramData%\IdealAkeWms\DataProtection-Keys` | Web und Dienst (Quelle: `IdealAkeWms/Services/SageSdata/SdataZugangsdaten.cs:116-120`) |
| `PdfRender:EdgePath`, `:TimeoutSeconds`, `:MaxConcurrent` | PDF-Erzeugung über Microsoft Edge | Web (Quelle: `README.md:542-556`) |
| `SageLagerbuchung:Username/Password` | **nur Übergang:** alter Ablageort der SData-Zugangsdaten, gilt nur solange `SData:Username` leer ist | Web und Dienst (Quelle: `IdealAkeWms/Services/SageSdata/SdataZugangsdaten.cs:29-31`, `README.md:535-537`) |

### 2.3 Inhalt – Windows-Dienst

| Abschnitt / Key | Wofür | Gelesen von |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | WMS-Datenbank | Dienst (Quelle: `IDEALAKEWMSService/Program.cs:30-38`) |
| `ConnectionStrings:SageConnection` | Sage-Datenbank (Views/Tabellen, nur lesend) | Dienst (Quelle: `IDEALAKEWMSService/appsettings.json:4`, `IDEALAKEWMSService/Services/SageImportService.cs:46`) |
| `ConnectionStrings:OseonConnection` | OSEON-Datenbank (AKE) | Dienst (Quelle: `IDEALAKEWMSService/Services/OseonSyncService.cs:32`) |
| `ConnectionStrings:EnaioDmsConnection` | enaio-Datenbank | Dienst (Quelle: `IDEALAKEWMSService/Services/EnaioDmsSyncService.cs:52-53`) |
| `MailSettings:SmtpHost`, `SmtpPort`, `SmtpUseSsl`, `SmtpUsername`, `SmtpPassword`, `SenderName`, `FromAddress` | Mailversand | Dienst (Quelle: `IDEALAKEWMSService/Services/MailService.cs:31-73`) |
| `MailSettings:PickupDirectory` | **nur DEV/QA:** Mails als `.eml` in einen Ordner statt senden; hat Vorrang vor `SmtpHost` | Dienst (Quelle: `IDEALAKEWMSService/Services/MailService.cs:30-67`, [[fallstricke]] §5 „Mails pruefen ohne Versand“) |
| `DataProtection:KeysPath` | muss **denselben** Wert wie im Web haben, falls dort gesetzt (sonst kein Rückgriff auf das SData-Kennwort) | Dienst (Quelle: `README.md:529-530`) |
| `Sync:Feiertag*` | nur noch Rückfallwert des Feiertags-Abgleichs, falls die DB-Zeile fehlt | Dienst (Quelle: `IDEALAKEWMSService/Program.cs:110`, `IDEALAKEWMSService/Services/HolidaySyncService.cs:58-81`) |
| `Serilog:*` | `logs/service-*.log`, `logs/sync/`, `logs/notifications/` | Dienst (Quelle: `IDEALAKEWMSService/appsettings.json:51-119`) |

**Wirkungslos in der Datei:** Die Blöcke `Sync:*` (außer Feiertage), `WorkerSettings:*` und `ErrorNotification:*` in der Dienst-`appsettings.json`
werden laut Code-Suche nicht mehr aus der Datei gelesen; maßgeblich sind die gleichnamigen ServiceSettings in der Datenbank.
(Quelle: `IDEALAKEWMSService/Workers/SyncWorker.cs:31-32,43`, `IDEALAKEWMSService/Services/SyncErrorNotifier.cs:32`; Suche nach
`Configuration[...]`/`GetSection`/`GetValue` über beide Projekte findet nur die in 2.2/2.3 genannten Keys)

### 2.4 Wann wirkt eine Dateiänderung?

- **Verbindungen, DataProtection, Serilog:** werden beim Start eingerichtet (Quelle: `IdealAkeWms/Program.cs:21-47`,
  `IDEALAKEWMSService/Program.cs:26-38`). → Web: App-Pool recyceln, Dienst: `Restart-Service IDEALAKEWMSService`.
- Eine Änderung an der `web.config` startet die Web-App in IIS selbst neu (IIS-Standardverhalten; **zu prüfen** am Zielsystem, nicht im Code belegt).
- `MailSettings:*` liest der Dienst bei jedem Versand neu aus der Konfiguration (Quelle: `IDEALAKEWMSService/Services/MailService.cs:31-73`).
  Ob eine Dateiänderung ohne Neustart greift, hängt vom Nachladen der Datei ab – **zu prüfen**; sicher ist: Dienst neu starten.

---

## 3. Ebene 2 – ServiceSettings (`/ServiceSettings`)

### 3.1 Prinzip „DB-first“

- **Katalog:** Alle Keys mit Typ, Standard, Kategorie, Anzeigename, Verwendung, Standort und Warnhinweis stehen in
  `IdealAkeWms/Models/ServiceSettingDefinitions.cs` (`.All`, rund 100 Keys). Ein Test schlägt fehl, wenn der Dienst einen Key liest, der dort fehlt.
  (Quelle: `IdealAkeWms/Models/ServiceSettingDefinitions.cs:15-21`, [[0008-servicesettings-db-first-mit-typisiertem-katalog]])
- **Anlage:** Jeder Start der Web-App legt fehlende Zeilen mit dem Katalog-Standard an; vorhandene Werte bleiben unberührt.
  (Quelle: `IdealAkeWms/Program.cs:312-327`)
- **DB gewinnt:** Der Dienst liest jeden Wert bei **jedem** Zugriff direkt aus der Tabelle, ohne Zwischenspeicher.
  (Quelle: `IDEALAKEWMSService/Common/ServiceSettings.cs:9-13`)
- **Bei DB-Fehler** nehmen die meisten Lesestellen den Standard aus dem Code (nicht aus der Datei), damit der Dienst weiterläuft.
  (Quelle: `IDEALAKEWMSService/Common/ServiceSettings.cs:45-77`)
- **Pflege:** nur Rolle `admin` (Quelle: `IdealAkeWms/Controllers/ServiceSettingsController.cs:19`). Feste Wertebereiche als Auswahl,
  der Server weist ungültige Werte ab (Quelle: [[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]]).
- **Gesperrt:** `ProduktionsauftragHierarchisch` ist ein Einwegtor und in der Maske nur lesbar; Umschalten nur über `/HierarchieUmstellung`.
  (Quelle: `IdealAkeWms/Models/ServiceSettingDefinitions.cs:222`, [[services]] Abschnitt „IDEAL Teil 7“)

### 3.2 Wann wirkt eine Änderung?

| Fall | Wirkung | Quelle |
|---|---|---|
| Schalter `Sync:*Enabled`, Caps, Quellen | beim nächsten Durchlauf des `SyncWorker` (Standard alle 15 min) | `IDEALAKEWMSService/Workers/SyncWorker.cs:31,552` |
| `WorkerSettings:SyncIntervalMinutes` | erst **nach** Ablauf der laufenden Wartezeit | `IDEALAKEWMSService/Workers/SyncWorker.cs:31,552` |
| Sage-Buchungen (`SageLagerbuchungAktiv`, `SageWareneingangBelegAktiv`, CSV) | im nächsten Takt des `SageBookingWorker` (Standard 20 s, mindestens 5 s) | `IDEALAKEWMSService/Workers/SageBookingWorker.cs:60-75,105` |
| `SageLagerbuchungSslZertifikatPruefen` im Dienst | ohne Neustart (wird beim Verbindungsaufbau gelesen); die Warnung im Start-Log erscheint aber nur beim Dienststart | `IDEALAKEWMSService/Program.cs:117-137`, `IDEALAKEWMSService/Workers/SageBookingWorker.cs:55-56` |
| Derselbe Schalter in der Web-App | erst für neue Verbindungen (bis ca. 2 min oder App-Pool-Recycle) | `IdealAkeWms/Program.cs:163-165` |
| SData-Benutzer/Kennwort | ohne Neustart, kein Recycle | [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]] „Konsequenzen“ |

> **Nach jedem Deploy:** `/ServiceSettings` einmal durchgehen. Werte aus der Dienst-`appsettings.json` greifen nicht; jeder gewünschte
> Abgleich muss hier eingeschaltet sein. (Quelle: [[services]] „Deploy-Pflicht“, [[0008-servicesettings-db-first-mit-typisiertem-katalog]] „Konsequenzen“)

---

## 4. Ebene 3 – AppSettings (`/Settings`)

- **Katalog:** `IdealAkeWms/Models/AppSettingDefinitions.cs` (Typ, Standard, Gruppe, Wirkung, Verwendung, Standort, Warnhinweis).
  Die Maske `/Settings` zeigt alles daraus; `README.md` Abschnitt „AppSettings“ ist ein Auszug. (Quelle: `README.md:447-448`)
- **Anlage:** Der Web-Start legt nur Keys mit `Seed` an; Keys ohne Seed bleiben ohne Zeile, dann gilt der Standard des Lesers.
  (Quelle: `IdealAkeWms/Program.cs:296-310`, `IdealAkeWms/Models/AppSettingDefinitions.cs:5-10`)
- **Pflege:** `/Settings` und `/StandortEinstellungen`, nur `admin`. (Quelle: `IdealAkeWms/Controllers/SettingsController.cs:11`, [[controller]] Zeile „RequireAdminAccess“)
- **Fachmodule sind standardmäßig aus** (Opt-in); Ausnahme `LagerbestellungAktiv` (Standard an, nur `false` sperrt).
  (Quelle: [[0011-feature-toggles-ueber-appsettings]] „Entscheidung“)
- **Lesart beachten:** Die meisten Schalter wirken nur bei exakt `true`; `1` oder `true ` gelten als **aus**. Altwerte zeigt beim Start der
  Lauf „Einstellungsprüfung“ im Aktivitäts-Protokoll. (Quelle: `README.md:450-455`, `IdealAkeWms/Program.cs:384-405`)
- **Wann wirkt eine Änderung:** Web und Dienst halten AppSettings bis zu **2 Minuten** im Speicher. Speichern in der Web-App leert den
  Speicher nur im Web-Prozess; der Dienst sieht die Änderung spätestens nach 2 Minuten.
  (Quelle: `IdealAkeWms/Data/Repositories/CachedSettingRepository.cs:10,38-42`, `IDEALAKEWMSService/Program.cs:44-45`)

---

## 5. Betriebskritische Schalter

Spalte „Ebene“: S = ServiceSettings (`/ServiceSettings`), A = AppSettings (`/Settings`). Standort laut Katalog. Vollständige Liste:
Katalog-Dateien bzw. die Masken selbst.

### 5.1 Schreibt an Sage (Außenwirkung)

| Schalter | Ebene | Wirkung | Standard | Standort | Warnhinweis | Quelle |
|---|---|---|---|---|---|---|
| `SageLagerbuchungAktiv` | S | Manuelle Ein-/Ausbuchungen auf Plätzen mit „Sage-Buchung erlaubt“ werden an Sage gesendet | aus | beide | **Bucht in Sage.** Nur ein Dienst je Datenbank (sonst Doppelbuchung) | `IdealAkeWms/Models/ServiceSettingDefinitions.cs:233`, [[fallstricke]] §8 „Idempotenz nur bei genau EINER Worker-Instanz“ |
| `SageLagerbuchungUmbuchungAktiv` | S | zusätzlich Umbuchungen (nur zusammen mit dem Schalter oben) | aus | beide | Bucht in Sage | `ServiceSettingDefinitions.cs:247` |
| `SageWareneingangBelegAktiv` | S | Wareneingänge zu Sage-Bestellungen werden als Sage-Wareneingangsbeleg angelegt | aus | AKE (IDEAL aus) | **Beliefert Bestellungen und bucht Bestand in Sage.** Nur nach Schreibtest; Einkauf darf die Positionen danach nicht von Hand schließen | `ServiceSettingDefinitions.cs:234`, `:69-71` |
| `Sync:SageSystemKuerzel` | S | Kürzel in den Memo-Kennungen (`SM#…#`, `WE#…#`) | leer | beide | **In Produktion leer lassen.** Testsysteme, die in dieselbe Sage-Test-DB schreiben, brauchen ein eigenes Kürzel; ungültiger Wert hält den Buchungstakt an | `ServiceSettingDefinitions.cs:77-79,237`, [[services]] „v1.99.9 Sync:SageSystemKuerzel“ |
| `SData:BaseUrl`, `SData:Dataset`, `SData:Username`, `SData:Password` | S | Adresse, Mandant und Zugang der Sage-Schnittstelle | leer | beide (IDEAL erst mit SData-Endpunkt) | Falscher Benutzer = Buchungen schlagen fehl; Kennwort nie in die URL | `ServiceSettingDefinitions.cs:90-97,248-254`, `README.md:539-540` |
| `SageLagerbuchungSslZertifikatPruefen` | S | TLS-Zertifikat des Sage-Servers prüfen | an | beide | Ausschalten nur für Testsysteme | `ServiceSettingDefinitions.cs:260` |

### 5.2 Liest aus Sage und ändert WMS-Daten

| Schalter | Ebene | Wirkung | Standard | Warnhinweis | Quelle |
|---|---|---|---|---|---|
| `Sync:ProductionOrdersEnabled` | S | FA-Abgleich aus Sage | **an** | – | `ServiceSettingDefinitions.cs:199` |
| `Sync:ArticlesEnabled` | S | Artikel-Abgleich | **an** | – | `ServiceSettingDefinitions.cs:200` |
| `Sync:ProductionOrderReconcileEnabled` + `Sync:ReconcileMaxCancelPerRun` (100) | S | storniert FAs, die in Sage fehlen; über der Grenze nichts + Fehlermail | aus | Storniert FAs | `ServiceSettingDefinitions.cs:212-213` |
| `Sync:LagerbestandEnabled` + `Sync:LagerbestandNullsetzenMaxPerRun` (100) | S | korrigiert Bestände nach Sage, nullt verschwundene Paare (mit Grenze) | aus | Korrigiert/nullt Bestände | `ServiceSettingDefinitions.cs:209-211`, `:46` |
| `Sync:FaZusatzinfoEnabled` / `Sync:FaZusatzinfoAutoErledigtEnabled` | S | FA-Zusatzinfos; setzt verpackte/abgeholte FAs auf Komm-Erledigt | aus / an | Einweg; vorher Probelauf | `ServiceSettingDefinitions.cs:214-216`, [[fallstricke]] §2 „FA-Zusatzinfos: Auto-Erledigt …“ |
| `Sync:KreditorenEnabled`, `Sync:DebitorenEnabled` | S | Lieferanten/Kunden aus Sage (SData) | aus | Überschreibt WMS-Werte | `ServiceSettingDefinitions.cs:273-274`, `:114-115` |
| `Sync:EinkaufBestellungenEnabled` | S | offene Bestellungen (EBB/EBZ) in den WMS-Zwischenspeicher | aus | Grundlage für Wareneingang und Lieferverzug | `ServiceSettingDefinitions.cs:276` |
| `Sync:Quelle:*` (Lagerplätze, Lagerbestand, Artikel, FaStruktur, Arbeitsplätze, LagerbuchungLookup) | S | Leseweg je Abgleich: `View` / `SData` / `Vergleich` | `View` | `SData` schreibt die Stammdaten aus der Schnittstelle; Artikel/FA-Struktur teils gesperrt. FaStruktur (IDEAL, v1.99.26): `Vergleich` vergleicht alle Knotenfelder inkl. beendeter Haupt-FAs (liest dazu RealTimeData `dtsFaBelegsuche`, nur in diesem Modus); `SData` (Knoten aus SData, FAInfos aus der View) gesperrt bis P-FS3/Rückfragen 4/5, wirkt wie `View` + Warnung. SData erst nach abweichungsfreiem Vergleich umschalten | `ServiceSettingDefinitions.cs:24-29,264-269`, [[0019-quellen-weiche-view-sdata-je-abgleich]] |
| `ProduktionsauftragHierarchisch` | S | IDEAL: Struktur nach Produktionsaufträgen übernehmen | aus | **Einwegtor, nicht zurückschaltbar**; vorher Backup | `ServiceSettingDefinitions.cs:57-58,222` |
| `WorkerSettings:SyncDryRun` | S | Probelauf: rechnen und protokollieren, nichts schreiben | aus | – | `ServiceSettingDefinitions.cs:303` |

Weitere Abgleiche (OSEON, enaio, Lagerplätze, BOM-Cache, Werkbänke, Arbeitsgänge, Feiertage, PPS-Mitarbeiter, Stillstandsgründe,
Artikel-Lieferanten): Standard aus, Katalog `ServiceSettingDefinitions.cs:199-298`, Beschreibung [[services]] Abschnitte „Sync-Services“ und
„Sage-SData-Abgleich“.

### 5.3 Versendet Mails

| Schalter | Ebene | Wirkung | Standard | Warnhinweis | Quelle |
|---|---|---|---|---|---|
| `ErrorNotification:Enabled` + `:Recipients` | S | Fehlermails bei Sync-Fehlern | aus | Versendet Mails | `ServiceSettingDefinitions.cs:306-307` |
| `Notifications:MeldebestandEnabled` + `:Recipients` | S | Meldebestand-Mail | **an** | Versendet Mails | `ServiceSettingDefinitions.cs:310-312` |
| `Notifications:AppBaseUrl` | S | Basis-Adresse für Links in Mails | leer | je System richtig setzen (DEV zeigte auf ein anderes System) | `ServiceSettingDefinitions.cs:313`, `secondbrain/aufgaben/2026-11-12-deploy-fahrplan-buendel-v1-99.md:66-67` |
| `Sync:PartRequisitionEmailEnabled`, `Sync:WarehouseRequisitionEmailEnabled` | S | Mails für Bedarfsmeldungen bzw. Lager-/Glasbestellungen | aus | Versendet Mails | `ServiceSettingDefinitions.cs:206-207`, `:43-44` |
| `Lieferverzug:MailAktiv` | S | Hauptschalter Lieferverzug-Mails (Mo–Fr ab `Lieferverzug:Uhrzeit`) | aus | Versendet Mails, ohne Testbetrieb auch an Lieferanten | `ServiceSettingDefinitions.cs:316`, `:141` |
| `Lieferverzug:EinkaufAktiv` + `:EinkaufEmpfaenger` | S | tägliche Sammelliste an den Einkauf | an / leer | Versendet Mails | `ServiceSettingDefinitions.cs:322-324` |
| `Lieferverzug:KreditorAktiv` | S | Erinnerung an Lieferanten | aus | **Externe Mails, nicht rückholbar** | `ServiceSettingDefinitions.cs:325`, `:150` |
| `Lieferverzug:Testbetrieb` | S | Kreditor-Mails gehen an den Einkauf statt an Lieferanten | **an** | Ausschalten = ab nächstem Versandtag extern. Vorher Mail-Relay und Absender mit der IT klären | `ServiceSettingDefinitions.cs:326`, `:151` |
| `Lieferverzug:KreditorMaxJeLauf` | S | Grenze echter Kreditor-Mails je Lauf (alle oder keine) | 30 | erste Welle prüfen | `ServiceSettingDefinitions.cs:328`, [[fallstricke]] §37 „Erste Welle Kreditor-Mails“ |
| `Lieferverzug:KreditorAbsender` | S | Absender der Kreditor-Mail; leer = `MailSettings:FromAddress` | leer | Relay muss Adresse für externe Empfänger zulassen | `ServiceSettingDefinitions.cs:329`, `:154` |
| `MailSettings:PickupDirectory` (Datei) | Datei | Mails nur als Datei ablegen | nicht gesetzt | **In Produktion nie setzen** – dann geht keine Mail raus, das Protokoll meldet trotzdem „gesendet“ | [[fallstricke]] §5 „Mails pruefen ohne Versand“, `scripts/deploy/nachlauf.psd1:158-161` |

### 5.4 Schreibt Dateien (CSV-Ablage)

| Schalter | Ebene | Wirkung | Standard | Standort | Warnhinweis | Quelle |
|---|---|---|---|---|---|---|
| `WareneingangCsvAktiv` | S | schreibt je an Sage übergebenem Wareneingang mit Barcode eine `<Barcode>.csv` | aus | AKE | Nur zusammen mit `SageWareneingangBelegAktiv`; beim Einschalten werden alle wartenden Vorgänge sofort geschrieben | `ServiceSettingDefinitions.cs:81-83,241` |
| `WareneingangCsvPfad` | S | Zielordner, nur UNC (`\\server\freigabe\ordner`) | leer = nichts | AKE | Pfad mit dem Betreiber des Lesers (enaio) abstimmen; Dienstkonto braucht „Ändern“ | `ServiceSettingDefinitions.cs:84-85,243` |
| `WareneingangBarcodeAktiv` | A | Barcode-Feld im Wareneingang (Voraussetzung für die CSV) | aus | AKE | – | `IdealAkeWms/Models/AppSettingDefinitions.cs:92`, `README.md:510` |

### 5.5 Fachmodule und Anmeldung (Feature-Toggles, AppSettings)

| Schalter | Wirkung | Standard | Hinweis | Quelle |
|---|---|---|---|---|
| `WindowsAuthAktiv` | automatische Anmeldung mit dem Windows-Konto | aus | erst einschalten, wenn AD-Benutzer angelegt sind | `AppSettingDefinitions.cs:201`, `scripts/deploy/Install-IdealAkeWms.ps1:492` |
| `WindowsAuthBerechtigungsgruppe` | AD-Gruppe für „AD-Benutzer anlegen“ | leer | – | `AppSettingDefinitions.cs:203` |
| `LeitstandAktiv`, `BdeAktiv`, `TeileverfolgungAktiv`, `FaCompletionAktiv`, `BestellungenAktiv` | Module ein | aus | zusätzlich Rolle nötig | `AppSettingDefinitions.cs:124,152,156,160,180` |
| `LagerbestellungAktiv` | Modul Lagerbestellungen | **an** | nur `false` sperrt | `AppSettingDefinitions.cs:126`, [[fallstricke]] §7 „Feature-Toggle mit Default true …“ |
| `WareneingangBestellungAktiv` | Menü „Offene Bestellungen“ / „Wareneingang zu Bestellung“ | aus | braucht `Sync:EinkaufBestellungenEnabled` | `AppSettingDefinitions.cs:85`, `README.md:508` |
| `EinkaufAktiv` | Einkauf → Bestellvorschläge | aus | erst nach den SData-Abgleichen | `AppSettingDefinitions.cs:142`, `README.md:511` |
| `FaPflichtBuchungsarten` | FA-Pflicht je Buchungsart | leer | mit `Umbuchung` geht keine Umbuchung mehr an Sage | `AppSettingDefinitions.cs:96`, `README.md:514` |
| `FaHierarchyKommissionierlistenAktiv`, `FaHierarchyBeschichtungAktiv`, `FaHierarchyVormontageAktiv` | IDEAL-Module | aus | – | `AppSettingDefinitions.cs:191-195` |
| `BeschichtungKennzeichenQuelle` („Quelle Beschichtet-Kennzeichen“) | wer das Lack-Kennzeichen schreibt: `Artikelkategorie` (Lackierteil-Erkennung) oder `FaStruktur` (FA-Materialisierung); der andere Lauf meldet „übersprungen“ | `Automatisch` (flach = Artikelkategorie, hierarchisch = FaStruktur) | unpassend zum Master `ProduktionsauftragHierarchisch` = Speichern abgelehnt, sonst wirkt wie Automatisch mit Protokoll-Warnung; Umschalten setzt bei wegfallendem Kennzeichen „Beschichtung erledigt“ zurück; Update-Skript prüft `Pruefung/BeschichtungQuelleKombination` | `IdealAkeWms/Models/AppSettingDefinitions.cs`, `IdealAkeWms/Services/Beschichtung/BeschichtungResolver.cs` |
| `BeschichtungTerminQuelle` („Quelle Beschichtungstermin“) | `Berechnet` oder `View` (`Start_Beschichtung`, nur hierarchisch) | `Automatisch` (flach = Berechnet, hierarchisch = View) | v1.99.25: noch ohne Wirkung in den Listen (Folgeversion); `View` an der AKE nicht speicherbar | wie oben |
| `Sync:CoatingDetectionEnabled` (ServiceSetting) | steuert seit v1.99.25 auch die Lackierteil-Erkennung im Sage-Import-Hook | aus | aus = kein Lack-Kennzeichen für neue Aufträge aus dem Import | `IDEALAKEWMSService/Services/SageImportService.cs` |
| `VaultLinksAktiv` / `VaultLinkVorlage`, `EnaioLinksAktiv` / `EnaioLinkVorlage` | Links zu Vault-Zeichnungen und enaio-Dokumenten | an (Seed), AKE-Adressen | IDEAL ohne Vault: ausschalten | `AppSettingDefinitions.cs:213-220`, `README.md:504-507` |

---

## 6. Checkliste nach einem Deploy

1. Dienst-`appsettings.json` und Web-`appsettings.Production.json`: Verbindungen zeigen auf das richtige System; `PickupDirectory` in Produktion
   nicht gesetzt (auch nicht als Umgebungsvariable `MailSettings__PickupDirectory`). (Quelle: `secondbrain/aufgaben/2026-11-12-deploy-fahrplan-buendel-v1-99.md:34`)
2. `/ServiceSettings` durchgehen: gewünschte Abgleiche an, `WorkerSettings:SyncDryRun` bewusst gesetzt, `Sync:SageSystemKuerzel` je System
   (Produktion leer), SData-Zugang „gesetzt“.
3. `/Settings`: Module wie am Standort vorgesehen.
4. Aktivitäts-Protokoll: kein neuer Lauf „Einstellungsprüfung“ mit Befunden. (Quelle: `README.md:450-455`)

Wo die Logs liegen und wie man Läufe überwacht: [06-logs-ueberwachung.md](06-logs-ueberwachung.md). Typische Fehlbilder:
[07-troubleshooting.md](07-troubleshooting.md). Lesende Abfragen auf die Einstellungstabellen: [08-diagnose-abfragen.md](08-diagnose-abfragen.md).
Installation und Update: [03-installation-update-rollback.md](03-installation-update-rollback.md). Systeme und Pfade:
[02-systeme-umgebungen.md](02-systeme-umgebungen.md). Architektur: [01-architektur-technologie.md](01-architektur-technologie.md).
Sicherung: [10-backup-restore.md](10-backup-restore.md).
