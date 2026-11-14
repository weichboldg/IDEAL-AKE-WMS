# 06 – Logs und Überwachung

**Stand:** v1.99.23, 2026-11-14 · **Zielgruppe:** IT-Administratoren (ohne Entwicklerwissen)

Wo IdealAkeWms (Web-App unter IIS) und der Windows-Dienst `IDEALAKEWMSService` protokollieren, was man täglich und wöchentlich ansieht
und wie typische Warnungen zu lesen sind. Fehlerbilder mit Lösung stehen in [07-troubleshooting.md](07-troubleshooting.md), lesende
SQL-Abfragen in [08-diagnose-abfragen.md](08-diagnose-abfragen.md).

Quellenangaben: Pfade relativ zum Repository (Worktree), Brain-Notizen als `[[Name]]` unter `secondbrain/`.

---

## 1. Übersicht: Wo steht was?

| Ort | Was steht drin | Wer liest es | Quelle |
|---|---|---|---|
| Web-Log `<Web>\logs\idealakewms-<Datum>.log` | Meldungen der Web-App (Information und höher), Anfragen | IT, Server-Zugriff nötig | `IdealAkeWms/appsettings.json:7-30` |
| Web-stdout-Log `<Web>\logs\stdout_*.log` | Konsolenausgabe des Prozesses, v. a. Startabbrüche | IT | `IdealAkeWms/web.config:13-14` |
| Dienst-Log `<Dienst>\logs\service-<Datum>.log` | alle Meldungen des Dienstes | IT | `IDEALAKEWMSService/appsettings.json:61-69` |
| Dienst-Log `<Dienst>\logs\sync\sync-<Datum>.log` | nur Sync-, OSEON-, enaio- und Sage-Buchungs-Teile | IT | `IDEALAKEWMSService/appsettings.json:70-93` |
| Dienst-Log `<Dienst>\logs\notifications\notifications-<Datum>.log` | Meldebestand-Prüfung und Mailversand | IT | `IDEALAKEWMSService/appsettings.json:94-117` |
| Windows-Ereignisanzeige (Anwendung, Quelle ASP.NET Core Module / IIS-W3SVC-WP) | Startfehler der Web-App, App-Pool-Recycling | IT | (Quelle: [[2026-10-15-scan-handler-hardware-scanner-spec]] Abschnitt 6; [[2026-09-08-uat-protokoll-ideal-buendel-chrome]]) |
| Aktivitäts-Protokoll in der App (`/SyncLog`, Tabelle `dbo.SyncLogs`) | jeder Lauf des Dienstes mit Ergebnis-Zählern | Admins in der App, ohne Server-Zugriff | (Quelle: [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]]) |
| Seite „Sage-Lagerbuchungen“ (`/SageBookingQueue`) | Warteschlange der Buchungen WMS → Sage | Lager-Keyuser, Admins | `IdealAkeWms/Controllers/SageBookingQueueController.cs:10-16` |
| Seite „Sage-Wareneingänge“ (`/PurchaseOrders/SageWareneingaenge`) | Wareneingänge, die an Sage übergeben werden, inkl. CSV-Stand | Lager, Einkauf, Admins | `IdealAkeWms/Controllers/PurchaseOrdersController.SageWe.cs:8-14,54` |
| Update-Protokolle `C:\<Firma>\deploy-logs\` | Mitschrift jedes Laufs des Update-Skripts, Trockenlauf-Ausgaben | IT | `scripts/deploy/Update-IdealAkeWms.ps1:94,555` |
| Fehlermail an Empfänger aus `ErrorNotification:Recipients` | Sync-Fehler des Dienstes mit Fehlertext | IT (wenn eingetragen) | `IDEALAKEWMSService/Services/SyncErrorNotifier.cs:32-45` |

`<Web>` und `<Dienst>` sind die Programmordner je System, z. B. `C:\inetpub\wwwroot\IDEALAKEWMS` und `C:\AKE\IdealAkeWmsService`
(Quelle: `scripts/deploy/ziele.psd1:9-15`). Systeme und Server: [02-systeme-umgebungen.md](02-systeme-umgebungen.md).

---

## 2. Log-Dateien (Serilog)

Beide Programme schreiben mit der Bibliothek Serilog; die Einstellungen stehen im Abschnitt `Serilog` der jeweiligen `appsettings.json`
(Quelle: `IdealAkeWms/Program.cs:20-23`, `IDEALAKEWMSService/Program.cs:26-27`).

### 2.1 Web-App

| Einstellung | Wert | Quelle |
|---|---|---|
| Mindeststufe | `Information`; Microsoft.AspNetCore, Entity Framework und System erst ab `Warning` | `IdealAkeWms/appsettings.json:8-15` |
| Datei | `logs/idealakewms-.log`, täglich neue Datei (Datum wird angehängt) | `IdealAkeWms/appsettings.json:23-24` |
| Aufbewahrung | 30 Dateien (= 30 Tage), ältere löscht Serilog selbst | `IdealAkeWms/appsettings.json:25` |
| Zeilenformat | `Datum Uhrzeit Zeitzone [Stufe] Meldung` | `IdealAkeWms/appsettings.json:26` |
| Anfrage-Protokoll | jede HTTP-Anfrage als eine Zeile | `IdealAkeWms/Program.cs:443-444` |

Der Pfad ist relativ zum Programmordner der Web-App, also `<Web>\logs\` (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:319`, verweist bei
Startfehlern auf `<Web>\logs`). Der Ordner braucht das Recht „Ändern“ für die App-Pool-Identität; das Update-Skript warnt, wenn es fehlt
(Quelle: [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]], Prüfliste Ordner). Rechte im Detail:
[09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md).

**Stufen in der Datei:** `[INF]` Information, `[WRN]` Warnung, `[ERR]` Fehler, `[FTL]` Absturz (Format `{Level:u3}`,
`IdealAkeWms/appsettings.json:26`).

### 2.2 stdout-Log der Web-App (bei Startfehlern)

`web.config` schaltet das stdout-Log des ASP.NET Core Module ein: `stdoutLogEnabled="true"`, Datei `.\logs\stdout`
(Quelle: `IdealAkeWms/web.config:13-14`). Hier landet, was vor dem Start von Serilog schiefgeht, z. B. ein Datenbankfehler beim Start
(belegt: `D:\Dev\AKE\Web\logs\stdout_20261108065451_60020.log` mit „Invalid object name 'dbo.OseonOperationConfigs'“, Quelle:
[[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]] Befund U1).

**zu prüfen:** Das stdout-Log hat keine eigene Aufbewahrung; ob die Dateien auf den Zielsystemen von Hand oder per Aufgabe
aufgeräumt werden, ist nicht belegt. Ob `web.config` am Ziel noch dem Repo-Stand entspricht, ist ebenfalls nicht belegt
(Quelle: [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]] Rückfrage 2).

### 2.3 Windows-Dienst

| Einstellung | Wert | Quelle |
|---|---|---|
| Mindeststufe | `Information`; Microsoft und Entity Framework erst ab `Warning` | `IDEALAKEWMSService/appsettings.json:52-58` |
| Gesamt-Log | `logs/service-.log`, täglich, 30 Dateien, mit Quellangabe `{SourceContext}` | `IDEALAKEWMSService/appsettings.json:61-69` |
| Sync-Log | `logs/sync/sync-.log` – nur `SyncWorker`, `SageImportService`, `OseonSyncService`, `EnaioDmsSyncService`, `SageBookingWorker`, `SageLagerbuchungClient` | `IDEALAKEWMSService/appsettings.json:77,84-86` |
| Mail-Log | `logs/notifications/notifications-.log` – nur `NotificationWorker`, `StockCheckService`, `MailService` | `IDEALAKEWMSService/appsettings.json:101,108-110` |
| Entwicklung | `appsettings.Development.json` setzt `Debug` (nur DEV) | `IDEALAKEWMSService/appsettings.Development.json:3-9` |

Der Dienst setzt sein Arbeitsverzeichnis auf den Programmordner; die Logs liegen deshalb unter `<Dienst>\logs\` und nicht unter
`C:\Windows\System32` (Quelle: `IDEALAKEWMSService/Program.cs:9-11`). Die README nennt dieselbe Aufteilung
(Quelle: `README.md:915-924`).

> Die Mindeststufe und die Pfade stehen in der `appsettings.json` am Zielsystem. Das Update-Skript überschreibt diese Datei nicht
> (Quelle: [[fallstricke]] §30 „Kopieren nie mit /MIR“). Wer dort etwas ändert, ändert es nur für dieses System. Konfiguration
> allgemein: [04-konfiguration.md](04-konfiguration.md).

---

## 3. Windows-Ereignisanzeige und IIS

| Was | Wann ansehen | Beleg |
|---|---|---|
| Ereignisanzeige → Windows-Protokolle → Anwendung, Quelle „ASP.NET Core Module“ (ANCM) | Startseite liefert HTTP 500 direkt nach Deploy/Neustart | IDEAL-DEV: Ursache stand im Ereignisprotokoll (ANCM): `FormatException: A duplicate key 'SageLagerbuchung:Username'` in einer appsettings-Datei (Quelle: [[2026-10-15-scan-handler-hardware-scanner-spec]] Abschnitt 6) |
| Quelle „IIS-W3SVC-WP“ | sporadische 503 / App-Pool-Recycling | als Prüfschritt empfohlen (Quelle: [[2026-09-08-uat-protokoll-ideal-buendel-chrome]]; [[2026-09-08-uat-ergebnis-ideal-buendel-lauf-1]]) |
| IIS-Protokoll mit `sc-status` **und** `sc-substatus` | um 500/503 genauer einzuordnen | (Quelle: [[2026-09-08-uat-ergebnis-ideal-buendel-lauf-1]]) |

Die Web-App läuft „in-process“ im IIS-Arbeitsprozess (Quelle: `IdealAkeWms/web.config:15`). Eine 503 erzeugt die Anwendung selbst nicht;
sie kann nur aus IIS/ANCM kommen (Quelle: [[2026-09-08-uat-ergebnis-ideal-buendel-lauf-1]]). Empfohlene App-Pool-Werte (Idle Timeout 0,
AlwaysRunning) stehen in `README.md:34-52`.

**zu prüfen:** Speicherort der IIS-Protokolle auf idealweb01/akenet01 (Standard `%SystemDrive%\inetpub\logs\LogFiles` ist Windows-Standard,
im Projekt nicht belegt).

**HTTP 500.30** ist die Fehlerseite des ASP.NET Core Module, wenn die App beim Start abbricht (Microsoft-Standardverhalten, im Projekt
nur als „Startseite HTTP 500“ belegt). Die Web-App greift beim Start auf die Datenbank zu (Migrationen, Seeds; Quelle:
`IdealAkeWms/Program.cs:200-206`) – ist die DB nicht erreichbar, startet sie nicht. Vorgehen: [07-troubleshooting.md](07-troubleshooting.md).

---

## 4. Aktivitäts-Protokoll in der App (`/SyncLog`)

### 4.1 Zweck und Zugang

- Menü **Stammdaten → Aktivitäts-Protokoll**, nur Rolle `admin` (Quelle: `IdealAkeWms/Controllers/SyncLogController.cs:11`;
  `README.md:360-364`).
- Jeder Hintergrund-Lauf des Dienstes schreibt hier Beginn, Warnungen/Fehler und Ende mit Zählern. Die Einträge werden mit einer
  eigenen DB-Verbindung geschrieben und bleiben auch dann stehen, wenn der Lauf selbst zurückgerollt wird
  (Quelle: [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]]).
- Filter über die Adresse: `/SyncLog?service=<Name>&level=<Stufe>&reference=<Text>` plus Spaltenfilter
  (Quelle: `IdealAkeWms/Controllers/SyncLogController.cs:27-31`). Beispiel aus den Tests: `/SyncLog?service=ArtikelLieferant`
  (Quelle: [[2026-11-08-buendel-dev-v1-94-browser-testbericht]]).
- Tabelle `dbo.SyncLogs` mit `Timestamp`, `Service`, `Level`, `Message` (max. 1000 Zeichen), `Reference`
  (Quelle: `SQL/00_FreshInstall.sql:1193-1201`).
- Zeit = **Ortszeit** des schreibenden Rechners, ohne Umrechnung angezeigt (Quelle: `IdealAkeWms/Services/SyncLogger/SyncRun.cs:85-87`;
  [[0025-zeitstempel-in-lokalzeit-des-schreibenden-rechners]]).

### 4.2 So liest man einen Lauf

| Meldung | Stufe | Bedeutung | Quelle |
|---|---|---|---|
| `Run gestartet` | Info | Lauf beginnt | `IdealAkeWms/Services/SyncLogger/SyncLogger.cs:20-21` |
| `Run erfolgreich beendet — neu=…, aktualisiert=…` | Info | Lauf fertig, Zähler je Ergebnis | `IdealAkeWms/Services/SyncLogger/SyncRun.cs:40,98-105` |
| `Run fehlgeschlagen: <Text> — …` | Error | Lauf abgebrochen | `IdealAkeWms/Services/SyncLogger/SyncRun.cs:51-53` |
| `Run wurde unerwartet beendet (kein FinishXxx-Aufruf)` | Warning | Lauf endete ohne Abschluss (z. B. Dienst gestoppt während des Laufs) | `IdealAkeWms/Services/SyncLogger/SyncRun.cs:56-62` |

Zähler-Namen sind deutsch und wiederkehrend: `neu`, `aktualisiert`, `uebersprungen`, `fehler`, dazu fachliche wie `storniert`,
`reaktiviert`, `nullgesetzt` (Quelle: [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]]). Detailzeilen sind je Lauf begrenzt
(typisch 100), die Zähler zählen alles (ebd.).

### 4.3 Dienst-Namen (Auswahl)

Die vollständige Liste steht in `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs:53-64`, die deutschen Anzeigenamen in Zeile 68-106.
Für die tägliche Prüfung wichtig:

| Technischer Name | Anzeige | Quelle |
|---|---|---|
| `ProductionOrder`, `ProductionOrderReconciliation` | Produktionsaufträge-Sync, FA-Reconciliation | `SyncLogServices.cs:80-81` |
| `Article` | Artikel-Sync | `SyncLogServices.cs:82` |
| `Lagerplatz`, `Lagerbestand` | Lagerplatz-Sync, Lagerbestand-Sync | `SyncLogServices.cs:70-71` |
| `SageLagerbuchung` | Sage-Lagerbuchung (schreibt nach Sage) | `SyncLogServices.cs:84` |
| `SageWareneingang` | Sage-Wareneingang (schreibt nach Sage) | `SyncLogServices.cs:98` |
| `WareneingangCsv` | Wareneingang-CSV (Datei auf Netzfreigabe) | `SyncLogServices.cs:99` |
| `EinkaufBestellungen`, `Kreditoren`, `ArtikelLieferant` | SData-Abgleiche (lesend) | `SyncLogServices.cs:94-97` |
| `LieferverzugMail`, `PartRequisitionEmail`, `WarehouseRequisitionEmail` | Mailversand | `SyncLogServices.cs:100-102` |
| `OseonTracking`, `OseonArticleCategories`, `EnaioDms` | Fremdsysteme (AKE) | `SyncLogServices.cs:73-76` |
| `CleanupAktivitaetsprotokoll` | Bereinigung des Protokolls | `SyncLogServices.cs:104` |
| `Einstellungspruefung` | Prüfung der Einstellungen beim Web-Start | `SyncLogServices.cs:105`; `IdealAkeWms/Program.cs:381-398` |

### 4.4 Aufbewahrung

Der `CleanupWorker` des Dienstes löscht einmal täglich Einträge, die älter sind als `Cleanup:AktivitaetsprotokollAufbewahrungTage`
(Standard 180, `0` = nie löschen); er schreibt dazu einen eigenen Lauf `CleanupAktivitaetsprotokoll` (Quelle: `README.md:363`,
`README.md:950-951`). Gepflegt unter `/ServiceSettings` (Quelle: [[services]] Katalog). Auf akenet01 stand der Wert am 20.10.2026 auf 90,
auf idealweb01 auf 180 (Quelle: [[2026-10-20-ake-testsystem-bestandsaufnahme]] Abschnitt 2.2).

---

## 5. Sage-Lagerbuchungen (`/SageBookingQueue`)

- Menü **Lager → Sage-Lagerbuchungen** (Quelle: `IdealAkeWms/Views/Shared/_Layout.cshtml:125-127`). Lesen mit Lager-Lesezugriff,
  „Erneut senden“ nur Lager-Keyuser (Quelle: `IdealAkeWms/Controllers/SageBookingQueueController.cs:10-15`).
- Status je Eintrag: `Offen` (0), `Gesendet` (1), `Bestaetigt` (2), `Fehler` (3) (Quelle: `IdealAkeWms/Models/SageBookingQueueStatus.cs:13-16`;
  `SQL/00_FreshInstall.sql:321`).
- Der Dienst arbeitet die Warteschlange im Sekunden-Takt ab (`Sync:SageLagerbuchungIntervalSeconds`, Standard 20). Hängende `Gesendet`
  werden nach `Sync:SageLagerbuchungStuckMinutes` (10) per Suche in Sage geklärt; ab `Sync:SageLagerbuchungMaxErrorsPerRun` (50) Fehlern
  in einem Lauf geht eine Fehlermail raus (Quelle: [[services]] Abschnitt SageBookingWorker; `IDEALAKEWMSService/Workers/SageBookingWorker.cs:183-187,322-327`).
- „Erneut senden“ prüft vorher per Memo-Suche in Sage, ob die Buchung schon da ist (kein Doppelbuchen)
  (Quelle: [[2026-10-20-ake-testsystem-bestandsaufnahme]] Abschnitt Sage-Lagerbuchungen).
- Gelbes Banner „TLS-Zertifikatsprüfung deaktiviert … nur für Testsysteme“, wenn `SageLagerbuchungSslZertifikatPruefen` aus ist
  (ebd.; [[services]]). Auf einem Produktivsystem darf das Banner nicht erscheinen.

**Bewertung:** Einträge in `Fehler` sind immer ein Arbeitsauftrag; ein `Gesendet`, das länger als die Hänge-Zeit steht, ebenfalls.
Am AKE-Test lagen am 20.10.2026 sechs alte Fehler (SSL, 401, 400, 500) vom 03.08. (Quelle: [[2026-10-20-ake-testsystem-bestandsaufnahme]]).

## 6. Sage-Wareneingänge (`/PurchaseOrders/SageWareneingaenge`)

- Menü **Lager → Sage-Wareneingänge**, nur sichtbar, wenn der Wareneingang zu Bestellungen aktiv ist (Quelle:
  `IdealAkeWms/Views/Shared/_Layout.cshtml:128-131`). Aktionen „Erneut senden“, „Als übergeben markieren“, „CSV erneut schreiben“ nur
  Lager-Keyuser (Quelle: `IdealAkeWms/Controllers/PurchaseOrdersController.SageWe.cs:101-141`).
- Status (Spalte `SageWeStatus`): `NichtNoetig` 0, `Offen` 1, `Gesendet` 2, `Uebergeben` 3, `Fehler` 4, `NurInSage` 5, `Klaeren` 6
  (Quelle: `IdealAkeWms/Models/PurchaseOrderReceipt.cs:15-26`).
- **Fehler** wiederholt der Dienst selbst (Standard 3 Versuche, Abstand 15 min) und sucht vor jedem Versuch den Beleg in Sage
  (Quelle: `scripts/deploy/nachlauf.psd1:319-321`). **Klären** = möglicherweise schon ein Beleg in Sage ohne passende WMS-Kennung, oder beim
  Ausschalten des Sage-Wareneingangs liegen geblieben; es wird nie automatisch nachgeschickt (Quelle: `scripts/deploy/nachlauf.psd1:322-324`;
  [[fallstricke]] §31).
- Spalte **CSV** zeigt, ob die Datei für die Netzfreigabe geschrieben ist (Quelle: `scripts/deploy/nachlauf.psd1:337-339`).
- Ein erfolgreicher Durchlauf am AKE-Test-IDEAL (13.11.2026): Status „offen (wird gesendet)“ → nach ca. 20 s „an Sage übergeben“,
  Sage-Beleg, CSV „geschrieben“; im Aktivitäts-Protokoll `Sage-Wareneingang … gesendet 1, uebergeben 1, fehler 0, unklar 0`
  (Quelle: [[2026-11-13-ake-regression-akenet-4445-browser-testbericht]] Fortsetzung 5).

Schnittstellen im Detail: [05-schnittstellen.md](05-schnittstellen.md).

---

## 7. Mails an die IT

| Mail | Auslöser | Empfänger | Quelle |
|---|---|---|---|
| `[IDEAL-AKE-WMS] Sync-Fehler: <Schritt>` mit Zeit, Maschine, Version, Fehlertext | jeder Sync-Fehler des Dienstes, Fehler-Cap der Sage-Lagerbuchung | `ErrorNotification:Recipients` (kommagetrennt), nur wenn `ErrorNotification:Enabled = true` | `IDEALAKEWMSService/Services/SyncErrorNotifier.cs:32-56`; `IDEALAKEWMSService/Workers/SageBookingWorker.cs:322-327` |

- Beide Werte liegen in `/ServiceSettings` (DB), nicht in der Datei; Standard: aus, keine Empfänger (Quelle: [[services]] Katalog,
  `ErrorNotification:*`; `README.md:928-933`).
- Die Fehlermail gilt **nur für den Dienst**. Fehler der Web-App (z. B. HTTP 500) erzeugen keine Mail – sie stehen nur im Web-Log
  (Quelle: [[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec]] QA-Befund F3).
- Ist `MailSettings:PickupDirectory` gesetzt, gehen **alle** Dienst-Mails als `.eml` in einen Ordner statt per SMTP; auf Prod darf der Wert
  nicht gesetzt sein, das Update-Skript bricht dort ab (Quelle: `scripts/deploy/nachlauf.psd1:158-161`; `README.md` Tabelle Dienst-Konfiguration).
- **zu prüfen:** Wer an AKE und IDEAL als Empfänger in `ErrorNotification:Recipients` steht und ob `ErrorNotification:Enabled` auf den
  Produktivsystemen an ist. Im Brain ist nur der Default (aus) belegt.

---

## 8. Was prüfen – täglich und wöchentlich

### Täglich (ca. 10 Minuten)

| Nr | Prüfung | Wo | Erwartung | Quelle |
|---|---|---|---|---|
| T1 | Laufen die Syncs? Letzter `Run erfolgreich beendet` je wichtigem Dienst nicht älter als ein Takt (Standard 15 min) | `/SyncLog?service=ProductionOrder`, `…=Article`, `…=Lagerbestand` oder Abfrage 08 §2.1 | aktuelle Einträge | `README.md:935`; [08-diagnose-abfragen.md](08-diagnose-abfragen.md) |
| T2 | Fehler im Aktivitäts-Protokoll seit gestern | `/SyncLog?level=Error` oder 08 §2.2 | keine neuen; bekannte OSEON-Timeouts/Deadlocks einordnen (§9) | [[2026-10-20-ake-testsystem-bestandsaufnahme]] 2.3 |
| T3 | Sage-Lagerbuchungen in `Fehler` oder hängend in `Gesendet` | `/SageBookingQueue`, Status „Fehler“ | 0 | §5 |
| T4 | Sage-Wareneingänge in `Fehler` / `Klären`, CSV mit Fehler | `/PurchaseOrders/SageWareneingaenge` | 0 offen zu klären | §6 |
| T5 | `[ERR]`/`[FTL]` im Web- und Dienst-Log des Tages | `<Web>\logs`, `<Dienst>\logs` | nur bekannte (§9) | Prüfmuster aus [[2026-10-30-sage-sdata-fa-ansicht-spec]] QA („ohne `[ERR]`/`[FTL]`“) |
| T6 | Dienst läuft | Dienste-Konsole, Dienst `IDEALAKEWMSService` (Name je System, siehe 02) | „Wird ausgeführt“ | `scripts/deploy/Install-IdealAkeWms.ps1:56` |

### Wöchentlich

| Nr | Prüfung | Quelle |
|---|---|---|
| W1 | Freier Platz der Log-Ordner; stdout-Logs (keine automatische Aufbewahrung, §2.2) | `IdealAkeWms/web.config:13-14` |
| W2 | Bereinigung lief: Eintrag `CleanupAktivitaetsprotokoll` der letzten 7 Tage | `README.md:363` |
| W3 | Einstellungsprüfung nach jedem Web-Neustart ansehen (`/SyncLog?service=Einstellungspruefung`) | `scripts/deploy/nachlauf.psd1:282-283` |
| W4 | Doppel-Prüfungen und Kommissionier-Abfragen aus 08 §5/§6 (sollen leer sein) | `docs/TESTSZENARIEN.md` TS-138.4g/138.5b |
| W5 | Nach jedem Update: Protokoll in `C:\<Firma>\deploy-logs\` ablegen/prüfen, Nachlauf-Liste abgearbeitet | `scripts/deploy/Update-IdealAkeWms.ps1:94`; [03-installation-update-rollback.md](03-installation-update-rollback.md) |

**zu prüfen:** Ob es heute eine automatische Überwachung (Monitoring-Werkzeug, Dienst-Wiederanlauf-Alarm) gibt. Belegt ist nur die
Wiederanlauf-Regel des Dienstes (dreimal Neustart nach 60 s, `scripts/deploy/Install-IdealAkeWms.ps1:406`).

---

## 9. Typische Warnungen und was sie bedeuten

| Meldung (Ausschnitt) | Wo | Bedeutung | Handlungsbedarf | Quelle |
|---|---|---|---|---|
| OSEON-Tracking / -Artikelkategorie: Ausführungstimeout, Deadlock | Aktivitäts-Protokoll, Error | bekannte, wiederkehrende Fehler beim Lesen aus OSEON | nur bei Häufung | [[2026-10-20-ake-testsystem-bestandsaufnahme]] 2.3; [[teil-1]] BS16 |
| `View dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen nicht vorhanden — Sync übersprungen` | FaZusatzinfo, Warning | Sage-View fehlt am System | klären, ob gebraucht | [[2026-10-20-ake-testsystem-bestandsaufnahme]] 2.3; [[2026-11-12-deploy-fahrplan-buendel-v1-99]] |
| `hauptlagerplatz_fehlt=…`, Lagerbestand `übersprungen` („Artikel … nicht im WMS“) | Article / Lagerbestand | Stammdatenlücken in Sage, kein Fehler des WMS | nein | [[2026-10-20-ake-testsystem-bestandsaufnahme]] 2.3 |
| `Keine aktiven Empfänger` / `ohne_empfaenger=…` | PartRequisitionEmail | Bedarfsmeldung ohne Empfänger | Fachbereich | ebd. |
| `Sage-Wareneingang ist aus: … auf „Klären“ gesetzt, nicht gesendet.` | SageWareneingang | Schalter aus, Liegengebliebenes wartet auf Hand-Entscheid | Lager/Einkauf: §6 | [[teil-1]] BS16; [[fallstricke]] §31 |
| `Beleg … in Sage gefunden, nicht erneut gesendet.` | SageWareneingang | Sicherungsnetz hat Doppelbeleg verhindert | nein | [[teil-1]] BS16 |
| `Der Netzwerkname wurde nicht gefunden.` | Wareneingang-CSV | UNC-Pfad falsch/nicht erreichbar | ja, siehe 07 | [[teil-1]] BS16 |
| `Datei existiert bereits – nicht überschrieben.` | Wareneingang-CSV | Datei mit gleichem Barcode liegt schon da; wird nie ersetzt | prüfen | [[teil-1]] BS16; [[fallstricke]] §32 |
| `LagerbestellungAktiv: „1“ ist nicht erlaubt – erlaubt: an oder aus` | Einstellungspruefung | ungültiger Altwert in den Einstellungen | Maske einmal speichern | [[teil-1]] BS16; `scripts/deploy/nachlauf.psd1:284-285` |
| `Run wurde unerwartet beendet (kein FinishXxx-Aufruf)` | beliebiger Dienst, Warning | Lauf ohne Abschluss, z. B. Dienst-Stopp | bei Wiederholung prüfen | `IdealAkeWms/Services/SyncLogger/SyncRun.cs:60-61`; [[teil-1]] BS16 |
| `Zugangsdaten aus appsettings …` | Sage-Lagerbuchung u. a. | SData-Zugang kommt noch aus der Datei (Übergang) | Zugang in `/ServiceSettings` eintragen | `README.md:535-538` |
| `SData-Kennwort nicht entschlüsselbar – in den Service-Einstellungen neu eingeben` | Sage-Läufe, Error | Schlüsselring fehlt/anders; nichts geht an Sage | ja, siehe 07 | `README.md:532-534`; [[fallstricke]] §33 |
| `Run fehlgeschlagen: Ungültiger Objektname "dbo.PurchaseOrderReceipts".` | Sage-Lagerbuchung | Dienst lief neuer als das DB-Schema (während eines Updates) | nur, wenn nach dem Update weiter | [[2026-11-13-ake-regression-akenet-4445-browser-testbericht]] Fortsetzung 5 |
| `SyncLog write failed for …` | Dienst-/Web-Log, Warning | Protokollzeile konnte nicht in die DB geschrieben werden (z. B. Meldung > 1000 Zeichen) | ja, DB/Länge prüfen | `IdealAkeWms/Services/SyncLogger/SyncRun.cs:91-94`; [[fallstricke]] §31 |
| `Kommissionierfotos verschoben: n, uebersprungen: m.` | Web-Log, Info, erster Start ab v1.96.0 | Fotos aus `wwwroot` nach `App_Data` umgezogen | bei `m > 0` Rest prüfen | `IdealAkeWms/Services/PhotoStorage.cs:60`; `scripts/deploy/nachlauf.psd1:300-302` |
| **`[ERR]` zu UNIQUE-Verletzung (2601/2627) oder Deadlock (1205) im Web-Log** | Web-Log | **Seit v1.99.23 gewollt abgefangen** (Doppel-Absenden, Gleichstand); Entity Framework schreibt den Fehler trotzdem als `[ERR]`. Anwender sahen „bereits gebucht“ bzw. es wurde wiederholt | kein Handlungsbedarf, nur bei Häufung | [[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec]] QA-Befund F3; `IdealAkeWms/Controllers/PurchaseOrdersController.Receive.cs:162-168` |

---

## Verwandte Kapitel

[00-index.md](00-index.md) · [01-architektur-technologie.md](01-architektur-technologie.md) · [02-systeme-umgebungen.md](02-systeme-umgebungen.md) ·
[03-installation-update-rollback.md](03-installation-update-rollback.md) · [04-konfiguration.md](04-konfiguration.md) ·
[05-schnittstellen.md](05-schnittstellen.md) · [07-troubleshooting.md](07-troubleshooting.md) · [08-diagnose-abfragen.md](08-diagnose-abfragen.md) ·
[09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md) · [10-backup-restore.md](10-backup-restore.md)
