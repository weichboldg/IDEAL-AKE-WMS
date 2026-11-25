# 04 – Konfiguration

**Stand:** v1.99.70, 2026-11-25 (Kommissionier-Nachtrag ergänzt; sonst Einstellungen bis v1.99.44 nachgezogen; Zeilenangaben auf den Katalog-Stand v1.99.43 umgestellt) · **Zielgruppe:** IT-Administratoren (ohne Entwicklerwissen) · **Zweck:** Wo welche Einstellung liegt,
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
  (Quelle: `README.md:900-901`, `scripts/deploy/Update-IdealAkeWms.ps1:845-846`)

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
  `IdealAkeWms/Models/ServiceSettingDefinitions.cs` (`.All`: rund 120 Grund-Keys (`Basis()`) plus seit v1.99.40 die 27 Sage-Benutzerfelder, Abschnitt 5.6). Ein Test schlägt fehl, wenn der Dienst einen Key liest, der dort fehlt.
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
  (Quelle: `IdealAkeWms/Models/ServiceSettingDefinitions.cs:286`, [[services]] Abschnitt „IDEAL Teil 7“)

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
| `SageLagerbuchungAktiv` | S | Manuelle Ein-/Ausbuchungen auf Plätzen mit „Sage-Buchung erlaubt“ werden an Sage gesendet | aus | beide | **Bucht in Sage.** Nur ein Dienst je Datenbank (sonst Doppelbuchung) | `IdealAkeWms/Models/ServiceSettingDefinitions.cs:297`, [[fallstricke]] §8 „Idempotenz nur bei genau EINER Worker-Instanz“ |
| `SageLagerbuchungUmbuchungAktiv` | S | zusätzlich Umbuchungen (nur zusammen mit dem Schalter oben) | aus | beide | Bucht in Sage | `ServiceSettingDefinitions.cs:317` |
| `SageWareneingangBelegAktiv` | S | Wareneingänge zu Sage-Bestellungen werden als Sage-Wareneingangsbeleg angelegt | aus | AKE (IDEAL aus) | **Beliefert Bestellungen und bucht Bestand in Sage.** Nur nach Schreibtest; Einkauf darf die Positionen danach nicht von Hand schließen | `ServiceSettingDefinitions.cs:298`, `:70-72` |
| `Sync:SageSystemKuerzel` | S | Kürzel in den Memo-Kennungen (`SM#…#`, `WE#…#`) | leer | beide | **In Produktion leer lassen.** Testsysteme, die in dieselbe Sage-Test-DB schreiben, brauchen ein eigenes Kürzel; ungültiger Wert hält den Buchungstakt an | `ServiceSettingDefinitions.cs:92-94,307`, [[services]] „v1.99.9 Sync:SageSystemKuerzel“ |
| `SData:BaseUrl`, `SData:Dataset`, `SData:Username`, `SData:Password` | S | Adresse, Mandant und Zugang der Sage-Schnittstelle | leer | beide (IDEAL erst mit SData-Endpunkt) | Falscher Benutzer = Buchungen schlagen fehl; Kennwort nie in die URL | `ServiceSettingDefinitions.cs:125-133,318-324`, `README.md:539-540` |
| `SageLagerbuchungSslZertifikatPruefen` | S | TLS-Zertifikat des Sage-Servers prüfen | an | beide | Ausschalten nur für Testsysteme | `ServiceSettingDefinitions.cs:330` |

### 5.2 Liest aus Sage und ändert WMS-Daten

| Schalter | Ebene | Wirkung | Standard | Warnhinweis | Quelle |
|---|---|---|---|---|---|
| `Sync:ProductionOrdersEnabled` | S | FA-Abgleich aus Sage | **an** | – | `ServiceSettingDefinitions.cs:262` |
| `Sync:ArticlesEnabled` | S | Artikel-Abgleich | **an** | – | `ServiceSettingDefinitions.cs:263` |
| `Sync:ProductionOrderReconcileEnabled` + `Sync:ReconcileMaxCancelPerRun` (100) | S | storniert FAs, die in Sage fehlen; über der Grenze nichts + Fehlermail | aus | Storniert FAs | `ServiceSettingDefinitions.cs:275-276` |
| `Sync:LagerbestandEnabled` + `Sync:LagerbestandNullsetzenMaxPerRun` (100) | S | korrigiert Bestände nach Sage, nullt verschwundene Paare (mit Grenze) | aus | Korrigiert/nullt Bestände | `ServiceSettingDefinitions.cs:272-274`, `:47-49` |
| `Sync:FaZusatzinfoEnabled` / `Sync:FaZusatzinfoAutoErledigtEnabled` | S | FA-Zusatzinfos; setzt verpackte/abgeholte FAs auf Komm-Erledigt | aus / an | Einweg; vorher Probelauf | `ServiceSettingDefinitions.cs:277-279`, [[fallstricke]] §2 „FA-Zusatzinfos: Auto-Erledigt …“ |
| `Sync:KreditorenEnabled`, `Sync:DebitorenEnabled` | S | Lieferanten/Kunden aus Sage (SData) | aus | Überschreibt WMS-Werte | `ServiceSettingDefinitions.cs:356-357`, `:150-151` |
| `Sync:EinkaufBestellungenEnabled` | S | offene Bestellungen (EBB/EBZ) in den WMS-Zwischenspeicher | aus | Grundlage für Wareneingang und Lieferverzug | `ServiceSettingDefinitions.cs:361` |
| `Sync:EinkaufBestellungenNachWeMinuten` (seit v1.99.31) | S | nach übergebenen Sage-Wareneingängen liest der `SageBookingWorker` die offenen Bestellungen sofort neu (Lauf „Offene Bestellungen (Sage)“, Info „Anlass: Sage-Wareneingang …“), danach höchstens alle N Minuten (0–60, 0 = aus); nur mit `Sync:EinkaufBestellungenEnabled`, nie parallel zum regulären Lauf | 2 | jeder Lauf liest alle offenen Bestellungen; in der Zeit wartet die nächste Sage-Lagerbuchung. Standort AKE (IDEAL ohne SData-Endpunkt wirkungslos) | `ServiceSettingDefinitions.cs:157,219,362`, `IDEALAKEWMSService/Workers/SageBookingWorker.cs:131-145` |
| `Sync:LetzteBestellungUhrzeitNacht` (3) / `Sync:LetzteBestellungUhrzeitMittag` (12) (seit v1.99.30) | S | Uhrzeiten (0–23) des eigenen Laufs „letzte Bestellung“ je Artikel und Lieferant (3 Jahre EBB/EBZ aus `EinkaufPositionenAllInfo`) – ab dieser Stunde einmal je Tag und Zeitfenster; vorher lief er in jedem Dienst-Zyklus mit (~5 min). Nur wirksam mit `Sync:ArtikelLieferantEnabled`. Bestellungen bei Lieferanten ohne Artikel-Lieferant-Zeile landen in `dbo.ArticleLastOrdersUnassigned` (SQL/155) | 3 / 12 | Neue Bestellungen erscheinen in den Bestellvorschlägen erst nach dem nächsten Lauf; nach Deploy einmal abwarten bzw. Dienst nach der Uhrzeit starten | `ServiceSettingDefinitions.cs:153-154,232-233,359-360` |
| `Sync:KommissionierNachtragEnabled` (an) / `Sync:KommissionierNachtragUhrzeit` (2) (seit v1.99.70) | S | Lauf „Kommissionier-Nachtrag“: einmal je Tag ab der Uhrzeit (0–23) fehlende Kommissionieraufträge für Stücklisten-Nachträge freigegebener, offener HauptFAs anlegen (dieselbe Logik wie Freigabe/Leitstand-Knopf); verpasster Termin wird nach dem Dienststart nachgeholt; im DryRun nie. Kürzt nie offene Zeilen, setzt nur den Marker „Stückliste kleiner“ | an / 2 | Legt Aufträge an, die in der Kommissionierung erscheinen; erster Dienststart nach dem Update läuft sofort einmal | `ServiceSettingDefinitions.cs` (Katalog), `IDEALAKEWMSService/Services/KommissionierNachtragService.cs` |
| `Sync:Quelle:*` (Lagerplätze, Lagerbestand, Artikel, FaStruktur, Arbeitsplätze, LagerbuchungLookup) | S | Leseweg je Abgleich: `View` / `SData` / `Vergleich` | `View` | `SData` schreibt die Stammdaten aus der Schnittstelle; Artikel/FA-Struktur teils gesperrt. FaStruktur (IDEAL, v1.99.26): `Vergleich` vergleicht alle Knotenfelder inkl. beendeter Haupt-FAs (liest dazu RealTimeData `dtsFaBelegsuche`, nur in diesem Modus); `SData` (Knoten aus SData, FAInfos aus der View) gesperrt bis P-FS3/Rückfragen 4/5, wirkt wie `View` + Warnung. SData erst nach abweichungsfreiem Vergleich umschalten. **AKE-Prod-Update (v1.99.39):** `Sync:Quelle:Artikel` von Hand auf `SData` stellen, sonst bleiben `ArticleSageDetails` leer (Sage-Spalten Bestellvorschläge, Maße Beschichtung AKE); das Update-Skript zeigt das als `Pruefung/ArtikelQuelleSData` (`scripts/deploy/nachlauf.psd1:207-210`) | `ServiceSettingDefinitions.cs:24-29,334-339`, [[0019-quellen-weiche-view-sdata-je-abgleich]] |
| `Sync:FaStrukturAufbewahrungTage` (seit v1.99.37, „FA-Struktur: Aufbewahrung verschwundener Strukturen“) | S | IDEAL: FA-Struktur-Zeilen, die Sage nicht mehr liefert, bleiben gekennzeichnet stehen (in Sage beendet/inaktiv/ungeklärt); eine ganz verschwundene Haupt-FA-Struktur entfernt der Lauf „FA-Hierarchie“, sobald alle ihre WMS-Aufträge erledigt/storniert sind, spätestens nach N Tagen (1–3650) | 90 | Nach Ablauf entfernt der Dienst verschwundene FA-Strukturen endgültig aus dem WMS | `ServiceSettingDefinitions.cs:58-59,226,284`, [[0032-fa-struktur-abgleich-mit-lebenslauf-statt-full-replace]] |
| `Sync:BlechteileFelderNachlesen` (seit v1.99.35, „Blechfertigung: Blechteile-Felder aus Sage nachlesen“) | S | IDEAL: Der Artikel-Abgleich liest, solange die Artikel über die View kommen, die Blechteile-Felder aller Sage-Lagerartikel samt Bestellmenge per SData nach und schreibt sie als Artikel-Merkmale (Grundlage der BF-Aufträge); SData-Fehler = nur Warnung. Feldnamen: Abschnitt 5.6 | aus | Liest nur aus Sage, überschreibt aber Artikel-Merkmale im WMS; braucht den SData-Zugang; aus, sobald die Artikel an IDEAL über SData kommen (P-R1) | `ServiceSettingDefinitions.cs:106-108,341`, `IdealAkeWms/Services/BfArtikelFelder.cs:11` |
| `ProduktionsauftragHierarchisch` | S | IDEAL: Struktur nach Produktionsaufträgen übernehmen | aus | **Einwegtor, nicht zurückschaltbar**; vorher Backup | `ServiceSettingDefinitions.cs:60-61,286` |
| `WorkerSettings:SyncDryRun` | S | Probelauf: rechnen und protokollieren, nichts schreiben | aus | – | `ServiceSettingDefinitions.cs:389` |

Weitere Abgleiche (OSEON, enaio, Lagerplätze, BOM-Cache, Werkbänke, Arbeitsgänge, Feiertage, PPS-Mitarbeiter, Stillstandsgründe,
Artikel-Lieferanten): Standard aus, Katalog `ServiceSettingDefinitions.cs:259-381`, Beschreibung [[services]] Abschnitte „Sync-Services“ und
„Sage-SData-Abgleich“.
Geändert: `Sync:OperationConfigSyncEnabled` („Arbeitsgang-Abgleich“) füllt seit v1.99.32 zusätzlich die Sage-Arbeitsgangnummer der
Arbeitsgang-Stammdaten (ein Handwert in `/OperationConfigs` hat Vorrang; SQL/153). Warnhinweis an `Sync:ProductionWorkplaceSyncViewName`:
die View `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]` liefert Arbeits**gang**nummern, keine Arbeitsplätze – erst nach Prüfabfrage P3 umstellen.
(Quelle: `ServiceSettingDefinitions.cs:290,292`, Manifest-Eintrag `v1.99.27-ArbeitsgangNummerPruefen` in `scripts/deploy/nachlauf.psd1:537`)

### 5.3 Versendet Mails

| Schalter | Ebene | Wirkung | Standard | Warnhinweis | Quelle |
|---|---|---|---|---|---|
| `ErrorNotification:Enabled` + `:Recipients` | S | Fehlermails bei Sync-Fehlern | aus | Versendet Mails | `ServiceSettingDefinitions.cs:392-393` |
| `Notifications:MeldebestandEnabled` + `:Recipients` | S | Meldebestand-Mail | **an** | Versendet Mails | `ServiceSettingDefinitions.cs:396-398` |
| `Notifications:AppBaseUrl` | S | Basis-Adresse für Links in Mails | leer | je System richtig setzen (DEV zeigte auf ein anderes System) | `ServiceSettingDefinitions.cs:399`, `secondbrain/aufgaben/2026-11-12-deploy-fahrplan-buendel-v1-99.md:66-67` |
| `Sync:PartRequisitionEmailEnabled`, `Sync:WarehouseRequisitionEmailEnabled` | S | Mails für Bedarfsmeldungen bzw. Lager-/Glasbestellungen | aus | Versendet Mails | `ServiceSettingDefinitions.cs:269-270`, `:44-45` |
| `Lieferverzug:MailAktiv` | S | Hauptschalter Lieferverzug-Mails (Mo–Fr ab `Lieferverzug:Uhrzeit`) | aus | Versendet Mails, ohne Testbetrieb auch an Lieferanten | `ServiceSettingDefinitions.cs:402`, `:183` |
| `Lieferverzug:EinkaufAktiv` + `:EinkaufEmpfaenger` | S | tägliche Sammelliste an den Einkauf | an / leer | Versendet Mails | `ServiceSettingDefinitions.cs:408-410` |
| `Lieferverzug:KreditorAktiv` | S | Erinnerung an Lieferanten | aus | **Externe Mails, nicht rückholbar** | `ServiceSettingDefinitions.cs:411`, `:192` |
| `Lieferverzug:Testbetrieb` | S | Kreditor-Mails gehen an den Einkauf statt an Lieferanten | **an** | Ausschalten = ab nächstem Versandtag extern. Vorher Mail-Relay und Absender mit der IT klären | `ServiceSettingDefinitions.cs:412`, `:193` |
| `Lieferverzug:KreditorMaxJeLauf` | S | Grenze echter Kreditor-Mails je Lauf (alle oder keine) | 30 | erste Welle prüfen | `ServiceSettingDefinitions.cs:414`, [[fallstricke]] §37 „Erste Welle Kreditor-Mails“ |
| `Lieferverzug:KreditorAbsender` | S | Absender der Kreditor-Mail; leer = `MailSettings:FromAddress` | leer | Relay muss Adresse für externe Empfänger zulassen | `ServiceSettingDefinitions.cs:415`, `:196` |
| `Sync:BfAuftragEmailEnabled` (seit v1.99.35) | S | Bestell-Mail je abgeschicktem BF-Auftrag an die Empfängergruppe `BfEmpfaengergruppeId` (nur beim Abschicken, nicht bei Freigabe/Fertig/Storno) | aus | Versendet Mails; beim Einschalten gehen offene Mails nach | `ServiceSettingDefinitions.cs:122,353`, `IDEALAKEWMSService/Workers/SyncWorker.cs:189-193` |
| `MailSettings:PickupDirectory` (Datei) | Datei | Mails nur als Datei ablegen | nicht gesetzt | **In Produktion nie setzen** – dann geht keine Mail raus, das Protokoll meldet trotzdem „gesendet“ | [[fallstricke]] §5 „Mails pruefen ohne Versand“, `scripts/deploy/nachlauf.psd1:235-238` |

### 5.4 Schreibt Dateien (CSV-Ablage)

| Schalter | Ebene | Wirkung | Standard | Standort | Warnhinweis | Quelle |
|---|---|---|---|---|---|---|
| `WareneingangCsvAktiv` | S | schreibt je an Sage übergebenem Wareneingang mit Barcode eine `<Barcode>.csv` | aus | AKE | Nur zusammen mit `SageWareneingangBelegAktiv`; beim Einschalten werden alle wartenden Vorgänge sofort geschrieben | `ServiceSettingDefinitions.cs:96-98,311-312` |
| `WareneingangCsvPfad` | S | Zielordner, nur UNC (`\\server\freigabe\ordner`) | leer = nichts | AKE | Pfad mit dem Betreiber des Lesers (enaio) abstimmen; Dienstkonto braucht „Ändern“ | `ServiceSettingDefinitions.cs:99-100,313-314` |
| `WareneingangBarcodeAktiv` | A | Barcode-Feld im Wareneingang (Voraussetzung für die CSV) | aus | AKE | – | `IdealAkeWms/Models/AppSettingDefinitions.cs:126`, `README.md:510` |
| `BfCsvAktiv` (seit v1.99.35) | S | schreibt je freigegebenem BF-Auftrag eine `<BF-Nummer>.csv` in den WiCAM-Auftragspool – **WiCAM legt daraus Aufträge an** | aus | IDEAL | Beim Einschalten werden alle wartenden freigegebenen Aufträge geschrieben. Aufbau seit v1.99.42 exakt nach der echten WiCAM-Datei `BF00000002.csv` (vorher Platzhalter), Details [05 § 8a](05-schnittstellen.md); Dienstkonto braucht Schreib- und Änderungsrecht auf den Pool | `ServiceSettingDefinitions.cs:109-111,342`, `IdealAkeWms/Services/BfCsv.cs:15-16` |
| `BfCsvPfad` | S | WiCAM-Pool, nur UNC; geplant `\\idealcam.ideal.ideal-ake.at\fertigung\u\pn\apools\` | leer = nichts | IDEAL | **In Test/DEV nie den echten Pool eintragen.** Dienstkonto braucht „Ändern“ (anlegen, umbenennen, `.tmp` löschen); Update-Skript prüft `Pruefung/BfCsvPfad` | `ServiceSettingDefinitions.cs:112-113,343-344`, `scripts/deploy/nachlauf.psd1:267-270` |
| `Sync:BfCsvMaxWiederholungen` / `Sync:BfCsvWiederholAbstandMinuten` | S | automatische Wiederholung nach Schreibfehler (0–20 / 1–1440 min); Datei vorhanden = nie automatisch | 3 / 15 | IDEAL | – | `ServiceSettingDefinitions.cs:114-115,345-346` |
| `SageFaArbeitsgaengeAnlegenAktiv` (seit v1.99.38) | S | Seite *Arbeitsgänge → Sage* zeigt „In Sage anlegen“; der `SageBookingWorker` legt eingereihte Arbeitsgänge als AG-Positionen im Sage-FA an (FertigungsauftragService, ServiceOption 4) | aus | IDEAL | **Schreibt in Sage-FAs (Einlastung/Kosten), angelegte Positionen löscht das WMS nicht.** Erst nach Schreibtest TS-159.x; Feldnamen bis dahin unbelegt (Spec P-1..P-4). Seit v1.99.39 gilt eine Anlage erst als „angelegt“, wenn das Nachlesen des FA die Sage-AG-Nummer findet; seit v1.99.42 wird „Antwort unklar“ nie automatisch wiederholt (Ablauf [05 § 4a](05-schnittstellen.md)) | `ServiceSettingDefinitions.cs:75-78,300`, `IDEALAKEWMSService/Workers/SageBookingWorker.cs:140,265` |
| `Sync:SageFaArbeitsgaengeBearbeiter` | S | Bearbeiter an jeder angelegten AG-Position | leer = nichts senden (Fehler) | IDEAL | gültiger Sage-Mitarbeiter (ValidateMitarbeiter, P-4) | `ServiceSettingDefinitions.cs:79-81,301-302` |
| `Sync:SageFaArbeitsgaengeMaxWiederholungen` / `Sync:SageFaArbeitsgaengeWiederholAbstandMinuten` | S | automatische Wiederholung nach Fehler mit Sage-Prüfung davor (0–10 / 5–1440 min); „Antwort unklar“ nie automatisch | 3 / 15 | IDEAL | – | `ServiceSettingDefinitions.cs:82-87,215-216,303-304` |
| `BfCsvCreateNewOrder`, `BfCsvOrderPriority`, `BfCsvOrderRelease`, `BfCsvRemark1..3` | S | Kopfzeilen der WiCAM-CSV (YES/NO, 1–9, YES/NO, `*HEADER_LINE_17..19`) | Spec-Vorlage | IDEAL | `ORDER_RELEASE = NO` sperrt die Teile im A-Pool; die Remarks füllt PN beim Import | `ServiceSettingDefinitions.cs:116-121,224,347-352` |

### 5.5 Fachmodule und Anmeldung (Feature-Toggles, AppSettings)

| Schalter | Wirkung | Standard | Hinweis | Quelle |
|---|---|---|---|---|
| `WindowsAuthAktiv` | automatische Anmeldung mit dem Windows-Konto | aus | erst einschalten, wenn AD-Benutzer angelegt sind | `AppSettingDefinitions.cs:261-262`, `scripts/deploy/Install-IdealAkeWms.ps1:492` |
| `WindowsAuthBerechtigungsgruppe` | AD-Gruppe für „AD-Benutzer anlegen“ | leer | – | `AppSettingDefinitions.cs:263-264` |
| `LeitstandAktiv`, `BdeAktiv`, `TeileverfolgungAktiv`, `FaCompletionAktiv`, `BestellungenAktiv` | Module ein | aus | zusätzlich Rolle nötig. Menü *Stammdaten → Empfänger* (Empfängergruppen) erscheint seit v1.99.44, wenn `BestellungenAktiv`, `LagerbestellungAktiv` (nicht „false“) **oder** `BlechfertigungAktiv` an ist; *Artikelgruppen-Zuordnung* nur mit `BestellungenAktiv`. Seit v1.99.45 stehen alle Menü- und Kachelregeln an einer Stelle (`Services/Navigation/NavigationKatalog.cs`, `NavigationSicht`); Startseite zeigt genau die Menüziele mit Kategorie; *OSEON AG-Übersicht* nur mit `TeileverfolgungAktiv` | `AppSettingDefinitions.cs:206,214,234,210,158` |
| `LagerbestellungAktiv` | Modul Lagerbestellungen | **an** | nur `false` sperrt | `AppSettingDefinitions.cs:160`, [[fallstricke]] §7 „Feature-Toggle mit Default true …“ |
| `LagerbestellungVerbrauchsbuchungAktiv` („Lagerbestellung: Lieferung ausbuchen“, seit v1.99.53) | Abschließen einer Lager-/Glasbestellung bucht die Ist-Menge (bei erneutem Abschluss die Differenz) als Ausbuchung vom Platz „Entnahme aus“; Sage-Plätze nur mit Sage-Buchung | aus | nur mit `LagerbestellungAktiv`; Bestandsregel wie Warenausgang (`NegativeBuchungErlaubt`/`NegativeBuchungLagerplatz`). **Am Einschalttag endet die Handbuchung dieser Lieferungen in Sage** (sonst doppelte Entnahme); Manifest `Einstellung/LagerbestellungVerbrauchsbuchungAktiv` | `AppSettingDefinitions.cs`, `WarehousePickingController.AbschliessenAsync` |
| `WareneingangBestellungAktiv` | Menü „Offene Bestellungen“ / „Wareneingang zu Bestellung“ | aus | braucht `Sync:EinkaufBestellungenEnabled` | `AppSettingDefinitions.cs:119`, `README.md:508` |
| `EinkaufAktiv` | Einkauf → Bestellvorschläge, seit v1.99.31 auch Einkauf → Offene Bestellungen und Sage-Wareneingänge (offen; seit v1.99.66 Umleitung auf Sage-Buchungen) | aus | erst nach den SData-Abgleichen | `AppSettingDefinitions.cs:176-178`, `README.md:511` |
| `OffeneBestellungenVorschauWochen` | Vorbelegung des Spaltenfilters Liefertermin (Mo der aktuellen KW bis So der N-ten KW, 1–26), Lager und Einkauf; seit v1.99.31 statt „heute + N Wochen“ | 2 | nur Anzeige | `AppSettingDefinitions.cs:136-138` |
| `EinkaufOffeneBedarfe` („Einkauf – offene Bedarfe einrechnen“) | welche offenen WMS-Bedarfe (Lagerbestellungen, Bedarfsmeldungen) die verfügbare Menge mindern – seit v1.99.30 in den Bestellvorschlägen **und** in der Artikelinfo (Verfügbarer Bestand) | `Lagerbestellungen,Bedarfsmeldungen` | nur mit `EinkaufAktiv`; leer erlaubt (nichts einrechnen) | `AppSettingDefinitions.cs:179-183`, Manifest `v1.99.30-ArtikelinfoBedarfe` (`scripts/deploy/nachlauf.psd1:516`) |
| `FaPflichtBuchungsarten` | FA-Pflicht je Buchungsart | leer | mit `Umbuchung` geht keine Umbuchung mehr an Sage | `AppSettingDefinitions.cs:130-135`, `README.md:514` |
| `FaHierarchyKommissionierlistenAktiv`, `FaHierarchyVormontageAktiv` | IDEAL-Module | aus | – | `IdealAkeWms/Models/AppSettingDefinitions.cs:245-246,255-256` |
| `FaHierarchyKommissionierlisteNurFreigegebene` (seit v1.99.43, „Kommissionierliste nur nach Leitstand-Freigabe“) | IDEAL: ein HauptFA erscheint in Kommissionierliste, Summiert, Druck und PDF erst nach „zur Kommissionierung freigegeben“ im Leitstand; aus = sofort nach der Übernahme aus Sage | **an** (Seed) | **Beim Einschalten verschwinden alle noch nicht freigegebenen Aufträge aus der Liste**; braucht `LeitstandAktiv`. Update-Skript zeigt `Pruefung/KommissionierlisteNichtFreigegeben` und `Pruefung/KommissionierlisteLeitstandAus` | `AppSettingDefinitions.cs:251-254`, `scripts/deploy/nachlauf.psd1:142-149` |
| `FaHierarchyKommissionierlisteVorfilterZiele` (seit v1.99.43, „Kommissionierliste: Standard-Ziele“) | IDEAL: Vorauswahl der Kommissionierziele (Mehrfachauswahl aus den Stammdaten Kommissionierziel), solange ein Benutzer keinen eigenen Zielfilter gemerkt hat; leer = alle Ziele | `K-01,K-02` (Seed) | nur Anzeige; `Pruefung/KommissionierlisteVorfilterZiele` meldet, wenn K-01/K-02 in der FA-Struktur nicht vorkommen | `AppSettingDefinitions.cs:247-250`, `scripts/deploy/nachlauf.psd1:150-153` |
| `FaHierarchyBeschichtungAktiv` | Beschichtungsauftragsmodul (Vorschlagsliste und Aufträge) an **beiden** Standorten: IDEAL aus der FA-Struktur, AKE (seit v1.99.34) aus der WA-Stückliste | aus (Seed) | Anzeigename „Beschichtungsauftrag aktiv“, Gruppe „Termine und Beschichtung“, Standort beide; zusätzlich Rolle `beschichtungsauftrag_*`; abgeschaltet meldet die App „Das Beschichtungsauftragsmodul ist deaktiviert“ | `IdealAkeWms/Models/AppSettingDefinitions.cs:101-103`, `IdealAkeWms/Filters/RequireFaHierarchyBeschichtungAktivAttribute.cs` |
| `BlechfertigungAktiv` (seit v1.99.35) | Menü „Blechfertigung“ (BF-Warenkorb, BF-Aufträge) | aus | zusätzlich Rolle Lagerbestellung **oder** `schachtler`; Empfängergruppe `BfEmpfaengergruppeId` Pflicht zum Abschicken (`Pruefung/BfEmpfaengergruppe`); Termine `BfSchachtelterminArbeitstage` (1) / `BfFeTerminArbeitstage` (10) in Arbeitstagen ohne Feiertage; `BfStatuswerte` (EILT, steht vorne in der WiCAM-Bemerkung). Warnhinweise im Katalog: `BlechfertigungAktiv` erst nach Empfängergruppe, Rolle `schachtler`, Feiertagen und Dienst einschalten; `BfEmpfaengergruppeId` **sendet Mails** an die Gruppe (über `Sync:BfAuftragEmailEnabled`); die beiden Termin-Werte (0–30 / 0–60) **wirken auf die Termine in WiCAM**. Standort IDEAL (AKE bleibt aus) | `AppSettingDefinitions.cs:186-203` |
| `BeschichtungKennzeichenQuelle` („Quelle Beschichtet-Kennzeichen“) | wer das Lack-Kennzeichen schreibt: `Artikelkategorie` (Lackierteil-Erkennung) oder `FaStruktur` (FA-Materialisierung); der andere Lauf meldet „übersprungen“ | `Automatisch` (flach = Artikelkategorie, hierarchisch = FaStruktur) | unpassend zum Master `ProduktionsauftragHierarchisch` = Speichern abgelehnt, sonst wirkt wie Automatisch mit Protokoll-Warnung; Umschalten setzt bei wegfallendem Kennzeichen „Beschichtung erledigt“ zurück; Update-Skript prüft `Pruefung/BeschichtungQuelleKombination` | `IdealAkeWms/Models/AppSettingDefinitions.cs`, `IdealAkeWms/Services/Beschichtung/BeschichtungResolver.cs` |
| `BeschichtungTerminQuelle` („Quelle Beschichtungstermin“) | `Berechnet` oder `View` (`Start_Beschichtung`, nur hierarchisch) | `Automatisch` (flach = Berechnet, hierarchisch = View) | wirkt in FA-Liste, Leitstand und Arbeitsliste (seit v1.99.32; v1.99.25–v1.99.31 noch ohne Wirkung); `View` an der AKE nicht speicherbar; hierarchisch nimmt `Berechnet` den Kopf-`FE_Termin` | wie oben |
| `Sync:CoatingDetectionEnabled` (ServiceSetting) | steuert seit v1.99.25 auch die Lackierteil-Erkennung im Sage-Import-Hook | aus | aus = kein Lack-Kennzeichen für neue Aufträge aus dem Import | `IDEALAKEWMSService/Services/SageImportService.cs` |
| `VaultLinksAktiv` / `VaultLinkVorlage`, `EnaioLinksAktiv` / `EnaioLinkVorlage` | Links zu Vault-Zeichnungen und enaio-Dokumenten | an (Seed), AKE-Adressen | IDEAL ohne Vault: ausschalten | `AppSettingDefinitions.cs:273-282`, `README.md:504-507` |

### 5.6 Sage-Benutzerfelder (`Sync:Userfeld:*`, seit v1.99.40)

Jedes Sage-Benutzerfeld (`USER_*`), das das WMS liest, hat eine eigene ServiceSetting `Sync:Userfeld:<Abgleich>:<Standardname>`.
Nur lesend gegenüber Sage; keine Außenwirkung. (Quelle: `IdealAkeWms/Services/SageSdata/SageUserfelder.cs:11-16`,
[[0033-sage-benutzerfelder-als-einstellung]])

- **Wert:** Sage-Feldname ohne Tabellen-Präfix, Typ Text, Standard = bisheriger Feldname (AKE liest damit unverändert). Leer = Feld wird
  nicht angefragt, die Anzeige zeigt „nicht konfiguriert“. Der Server lässt beim Speichern nur leer oder `USER_` + Buchstaben/Ziffern/
  Unterstrich zu; ein trotzdem ungültiger Wert gilt wie leer, mit Hinweis. (Quelle: `SageUserfelder.cs:13-23`, `ServiceSettingDefinitions.cs:251-257`)
- **Warnhinweis je Feld:** „Name muss in Sage existieren: ein falscher Name lässt den Lauf mit ‚Sage kennt das Feld nicht' scheitern.“
  (Quelle: `ServiceSettingDefinitions.cs:251`)
- **Kategorie in `/ServiceSettings`:** „Sage-Benutzerfelder: <Abgleich>“. (Quelle: `SageUserfelder.cs:30-36`)
- **Anlage:** Der Web-Start legt die Zeilen wie alle Katalog-Keys mit dem Standard an. (Quelle: `IdealAkeWms/Program.cs:311-323`)
- **IDEAL nach dem Update:** `Sync:Userfeld:Kreditoren:USER_EKMindestbestellwert` **leer** setzen (das Feld gibt es in Sage IDEAL nicht, sonst
  bricht der Kreditoren-Abgleich ab); ebenso Artikel `USER_LagerplatzID`/`USER_Mindestbestand`, falls Sage IDEAL sie nicht kennt – die Meldung
  im Aktivitäts-Protokoll nennt die Einstellung. (Quelle: Manifest `v1.99.40-Userfeld-EKMindestbestellwert-IDEAL`,
  `scripts/deploy/nachlauf.psd1:567-569`)
- **HTTP 404 einer SData-Resource:** eine Meldung, danach ist der Schritt 24 h pausiert; die Sperre liegt im Speicher, ein Dienst-Neustart hebt
  sie auf. (Quelle: [[2026-11-18-v1-99-40-sage-userfelder-als-einstellung]] „Deploy-Risiken“; Meldungstext
  `IdealAkeWms/Services/SageSdata/SageSdataQueryModels.cs:25-27`)

| Kategorie | Standardnamen (= letzter Teil des Keys) | Standort laut Katalog | Gelesen von | Quelle |
|---|---|---|---|---|
| Kreditoren | `USER_EKMindestbestellwert` | AKE | Kreditoren-Abgleich (SData Kontokorrent) → „Mindestbestellwert (Sage)“ | `SageUserfelder.cs:43` |
| Debitoren | `USER_KundenNameFA` | – | Debitoren-Abgleich (SData Adressen) → „Kundenname FA (Sage)“ | `SageUserfelder.cs:44` |
| Artikel | `USER_LagerplatzID`, `USER_Mindestbestand` | AKE | Artikel-Abgleich über SData → Artikel-Sage-Details; Mindestbestand auch Spalte „Mindestbestand (Sage)“ der Bestellvorschläge | `SageUserfelder.cs:45-46` |
| Offene Bestellungen | `USER_AB`, `USER_Kom` | – | Kopf der offenen Bestellungen (RealTimeData, Präfix `KHKEKBelege_`) → AB-Nummer, Kommission | `SageUserfelder.cs:47-48` |
| Arbeitsplätze | `USER_ArbeitsSchritt` | – | Werkbank-Anlage und Arbeitsgang-Abgleich aus `KHKPpsArbeitsplaetze` (nicht bei View-Quelle) | `SageUserfelder.cs:49` |
| Blechfertigung (7) | `USER_OSIstLagerartikel`, `USER_OSArtikeltyp`, `USER_OSMaterial`, `USER_OSMaterialnummerPN`, `USER_OSFertigungsInfoPN`, `USER_OSArbeitsschritte`, `USER_OSAbteilung` | IDEAL | Nachlese des Artikel-Abgleichs (`Sync:BlechteileFelderNachlesen`), gespeichert als Artikel-Merkmal unter dem Standardnamen | `SageUserfelder.cs:39,50-56` |
| FA-Struktur (13) | `USER_OSBemerkungPN`, `USER_OSFertigungsInfoPN`, `USER_OSKommissionieren`, `USER_OSAbteilung`, `USER_OSArbeitsschritte`, `USER_OSArtikeltyp`, `USER_OSMaterial`, `USER_OSVMBedarf`, `USER_OSBeschichtet`, `USER_OSBreite`, `USER_OSHoehe`, `USER_OSTiefe`, `USER_OSEKBedarf` | IDEAL | FA-Struktur über SData/Vergleich: Feld am FA-Kopf (`dtsFaBelegsuche`, Präfix `KHKPpsFaBelege_`) und, falls vorhanden, am Artikel | `SageUserfelder.cs:38,57-69` |

---

## 6. Checkliste nach einem Deploy

1. Dienst-`appsettings.json` und Web-`appsettings.Production.json`: Verbindungen zeigen auf das richtige System; `PickupDirectory` in Produktion
   nicht gesetzt (auch nicht als Umgebungsvariable `MailSettings__PickupDirectory`). (Quelle: `secondbrain/aufgaben/2026-11-12-deploy-fahrplan-buendel-v1-99.md:34`)
2. `/ServiceSettings` durchgehen: gewünschte Abgleiche an, `WorkerSettings:SyncDryRun` bewusst gesetzt, `Sync:SageSystemKuerzel` je System
   (Produktion leer), SData-Zugang „gesetzt“.
3. `/Settings`: Module wie am Standort vorgesehen; IDEAL ab v1.99.43: `LeitstandAktiv` an, solange `FaHierarchyKommissionierlisteNurFreigegebene` an ist.
   IDEAL ab v1.99.40: Sage-Benutzerfelder prüfen (Abschnitt 5.6).
4. Aktivitäts-Protokoll: kein neuer Lauf „Einstellungsprüfung“ mit Befunden. (Quelle: `README.md:450-455`)

Wo die Logs liegen und wie man Läufe überwacht: [06-logs-ueberwachung.md](06-logs-ueberwachung.md). Typische Fehlbilder:
[07-troubleshooting.md](07-troubleshooting.md). Lesende Abfragen auf die Einstellungstabellen: [08-diagnose-abfragen.md](08-diagnose-abfragen.md).
Installation und Update: [03-installation-update-rollback.md](03-installation-update-rollback.md). Systeme und Pfade:
[02-systeme-umgebungen.md](02-systeme-umgebungen.md). Architektur: [01-architektur-technologie.md](01-architektur-technologie.md).
Sicherung: [10-backup-restore.md](10-backup-restore.md).
