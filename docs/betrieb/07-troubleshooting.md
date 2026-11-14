# 07 – Troubleshooting

**Stand:** v1.99.23, 2026-11-14 · **Zielgruppe:** IT-Administratoren (ohne Entwicklerwissen)

Fehlerbilder, die im Projekt tatsächlich aufgetreten oder ausdrücklich dokumentiert sind – je Fall Ursache, Prüfung, Lösung und Quelle.
Was nicht belegt ist, steht als **zu prüfen** da. Log-Orte: [06-logs-ueberwachung.md](06-logs-ueberwachung.md); lesende SQL-Abfragen
(„A-…“): [08-diagnose-abfragen.md](08-diagnose-abfragen.md).

Quellenangaben: Pfade relativ zum Repository (Worktree), Brain-Notizen als `[[Name]]` unter `secondbrain/`.

> **Grundregel:** Erst messen, dann ändern. Auf Produktivsystemen keine Einstellungen „zum Testen“ umschalten – Sage-Schalter schreiben
> echte Belege (`README.md` Tabelle Dienst-Konfiguration, Zeile „Sage-Lagerbuchung / Sage-Wareneingang (**schreibt nach Sage**)“).

---

## 1. Start, IIS, Datenbank

| Symptom | Ursache | Prüfung | Lösung | Quelle |
|---|---|---|---|---|
| Startseite **HTTP 500 / 500.30** direkt nach Deploy oder Neustart | Startabbruch der Web-App. Belegt: (a) doppelter Schlüssel in einer `appsettings*.json` (`FormatException: A duplicate key 'SageLagerbuchung:Username'`); (b) Datenbankobjekt fehlt beim Start (`Invalid object name 'dbo.OseonOperationConfigs'`, DB auf anderem Schema-Stand) | Ereignisanzeige → Anwendung, Quelle ASP.NET Core Module; `<Web>\logs\stdout_*.log`; `<Web>\logs\idealakewms-*.log` | (a) JSON-Datei sichern, doppelten Abschnitt entfernen; (b) DB-Stand und Paket angleichen (A-1.1) | (a) [[2026-10-15-scan-handler-hardware-scanner-spec]] Abschnitt 6; (b) [[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]] Befund U1 |
| HTTP 500.30, **SQL nicht erreichbar** | Die Web-App führt beim Start Migrationen und Seeds aus (`db.Database.Migrate()`); ohne DB-Verbindung startet sie nicht | stdout-Log; Verbindungszeichenfolge `ConnectionStrings:DefaultConnection` am Ziel; SQL-Server/Instanz erreichbar; App-Pool-Konto hat DB-Login | Verbindung/Login herstellen, App-Pool neu starten. **zu prüfen:** genaue Fehlerseite (500.30) ist Microsoft-Standard, im Projekt nur „HTTP 500“ belegt | `IdealAkeWms/Program.cs:200-206`; `IdealAkeWms/appsettings.json:3` |
| Nach abgebrochenem Update: Web/Dienst gestoppt | Update-Skript hat in Schritt 3/4 (Schema) abgebrochen | Bildschirmausgabe „Zustand jetzt“, Protokoll `C:\<Firma>\deploy-logs\` | **Nicht von Hand starten** – der Web-Start würde fehlende Migrationen ohne Vorbedingungen einspielen. Ursache beheben, Skript neu starten | `scripts/deploy/Update-IdealAkeWms.ps1:316-320` |
| Fehlerseite beim nächsten Formular-Absenden (z. B. Abmelden) nach App-Pool-Recycling | Schlüssel für Formular-Tokens (DataProtection) nicht dauerhaft gespeichert | Ordner aus `DataProtection:KeysPath` bzw. `%ProgramData%\IdealAkeWms\DataProtection-Keys` vorhanden, App-Pool hat „Ändern“ | Ordner/Recht herstellen (macht das Update-Skript in Schritt 5a) | `IdealAkeWms/Program.cs:35-46`; [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]] Prüfliste Ordner |
| Sporadisch **HTTP 503** | Kommt nie aus der Anwendung, nur aus IIS/ANCM (Recycling, Rapid-Fail-Protection, volle Warteschlange) | Ereignisanzeige Quelle IIS-W3SVC-WP; IIS-Log `sc-status`/`sc-substatus` | App-Pool-Einstellungen laut README (Idle Timeout 0, AlwaysRunning) | [[2026-09-08-uat-ergebnis-ideal-buendel-lauf-1]]; `README.md:34-52` |
| App startet nicht, Kommissionierfotos | Ab v1.30.4/v1.96.0 liegen Fotos in `<Web>\App_Data`; ohne Schreibrecht startet die App nicht | Recht „Ändern“ des App-Pools auf `<Web>\App_Data` | Update-Skript stellt es her (Pflicht-Ordner) | `scripts/deploy/nachlauf.psd1:208-211` |
| Dienst-Lauf: „Die von der Anmeldung angeforderte …-Datenbank kann nicht geöffnet werden … AKENB\svc_idealakewms“ | Dienstkonto hat keinen Zugriff auf die in der Verbindung genannte (Sage-)Datenbank | Aktivitäts-Protokoll des Laufs (z. B. `/SyncLog?service=Article`); Verbindungszeichenfolge in der Dienst-`appsettings*.json` | DB-Login/Benutzer für das Dienstkonto anlegen oder Verbindung korrigieren. Belegt nur auf IDEAL-DEV | [[2026-11-08-buendel-dev-v1-94-browser-testbericht]] Befund 6 |

## 2. Anmeldung (Windows-Auth, SQL-Anmeldung)

| Symptom | Ursache | Prüfung | Lösung | Quelle |
|---|---|---|---|---|
| Kein automatisches Anmelden mit Windows | Schalter `WindowsAuthAktiv` aus (Standard aus); oder kein Benutzer mit passendem `WindowsUserName`; oder kein Windows-Desktop-Browser (Handys/Tablets/Mac bekommen bewusst das Formular) | `/Settings` → `WindowsAuthAktiv`; Benutzerstamm (A-7.3); Gerät | Schalter setzen, AD-Benutzer anlegen; „Mit Windows anmelden“ erzwingt die Anmeldung | `README.md:353-358`; `docs/TESTSZENARIEN.md` TS-40.1/40.2 |
| Nicht-Domänen-Geräte kommen gar nicht mehr auf die Seite | Anonyme Authentifizierung in IIS abgeschaltet | IIS → Site → Authentifizierung: **Windows und Anonym** müssen an sein | Beide aktivieren (steht so in `web.config`) | `IdealAkeWms/web.config:5-9`; [[fallstricke]] §4 „AddNegotiate() ist unter IIS in-process falsch“ |
| Update-Skript bricht beim ersten DB-Kontakt ab: „Die Anmeldung stammt aus einer nicht vertrauenswürdigen Domäne …“ (SQL 18452) | Ausführendes Konto ist Mitglied von **Protected Users** (NTLM gesperrt) **und** die benannte SQL-Instanz hat keinen SPN (Kerberos geht nicht) | `whoami /groups`; `setspn -Q MSSQLSvc/<host>*`; nach Verbindung A-1.5 (`auth_scheme`) | Empfohlen: SPNs `MSSQLSvc/<FQDN>:<Instanz>` und `:<Port>` auf das Instanzkonto, `klist purge`, Test mit `auth_scheme = KERBEROS`. Behelf (IDEAL-Test 11.11.): Konto aus Protected Users nehmen → `NTLM` | [[2026-11-11-buendel-deploy-update-skript]] Befund IDEAL-Test; [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „Einmal je Server“ |
| Wie oben, zusätzlich LDAP-Fehler 0x31 | RDP-Sitzung zu alt, Kerberos-Tickets abgelaufen | Anmeldezeit der Sitzung | Ab- und wieder anmelden | [[2026-11-11-buendel-deploy-update-skript]] |

**zu prüfen (offen laut Brain):** SPNs für `IdealSQL2\SAGE100TEST` (Port 54723) und ob das Konto danach wieder in Protected Users kommt;
für AKE: Konto und SPN von AKESQL20 (Quelle: [[2026-11-12-deploy-fahrplan-buendel-v1-99]]). Konten und Rechte: [09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md).

## 3. Sage-Schnittstelle (SData), Lagerbuchungen, Wareneingang

| Symptom | Ursache | Prüfung | Lösung | Quelle |
|---|---|---|---|---|
| Sage-Läufe: „SData-Kennwort nicht entschlüsselbar – in den Service-Einstellungen neu eingeben“; nichts geht an Sage | Dienst findet den Schlüsselring der Web-App nicht oder nutzt einen anderen (DB kopiert, Schlüsselordner gelöscht, Wert per SQL im Klartext eingetragen) | `DataProtection:KeysPath` in Web **und** Dienst gleich; Dienstkonto hat Leserecht auf den Ordner | Pfad angleichen / Recht setzen (Update-Skript 5a); sonst Kennwort unter `/ServiceSettings` → Sage-Lagerbuchung neu eingeben | `README.md:529-534`; [[fallstricke]] §33; `scripts/deploy/nachlauf.psd1:212-215` |
| Warnung je Lauf „Zugangsdaten aus appsettings …“ | `SData:Username` in `/ServiceSettings` leer → Rückfall auf Klartext in der Datei (Übergang) | Update-Skript-Prüfung `Pruefung/SDataKennwortAppsettings` | Zugang in `/ServiceSettings` eintragen, Lauf „Sage-Lagerbuchung“ prüfen, dann Einträge aus **beiden** Dateien entfernen (Dienst-`appsettings.json` und Web-`appsettings.Production.json`). Die Prüfung sieht nur die Dienst-Datei – Web-Datei von Hand ansehen | `README.md:535-538`; `scripts/deploy/nachlauf.psd1:162-165`; [[2026-11-14-review-befunde-sdata]] SD-4 |
| Register Sage-Test: „Sage-SData nicht konfiguriert“ | `SData:BaseUrl` oder `SData:Dataset` leer | `/ServiceSettings` Gruppe Sage-Lagerbuchung | Werte setzen (IDEAL erst mit SData-Endpunkt) | `README.md:539-540`; [[2026-11-13-ake-regression-akenet-4445-browser-testbericht]] (4445 anfangs leer) |
| Sage-Lagerbuchung in Status **Fehler** | Sage hat abgelehnt (belegte Beispiele: SSL, 401, 400, 500) | `/SageBookingQueue`, Spalte Fehler (A-3.2) | Ursache klären, dann „Erneut senden“ (prüft vorher per Memo-Suche, kein Doppelbuchen) | [[2026-10-20-ake-testsystem-bestandsaufnahme]] Sage-Lagerbuchungen |
| Gelbes Banner „TLS-Zertifikatsprüfung deaktiviert“ | `SageLagerbuchungSslZertifikatPruefen` aus | `/ServiceSettings` | Auf Prod einschalten; aus nur für Testsysteme | [[services]] TLS-Schalter |
| Sage-Wareneingang **Fehler** | Sage-Schreibfehler | `/PurchaseOrders/SageWareneingaenge`, Spalten Versuche/Fehler (A-4.2) | Dienst wiederholt selbst (Standard 3 Versuche, 15 min Abstand, jeweils mit Suche in Sage); danach in Sage prüfen und „Erneut senden“ | `scripts/deploy/nachlauf.psd1:319-324` |
| Sage-Wareneingang **Klären** | Möglicher Beleg in Sage ohne WMS-Kennung, oder liegen geblieben, weil der Sage-Wareneingang ausgeschaltet wurde | wie oben; in Sage nach Bestellung/Datum suchen | In Sage prüfen, dann „Als übergeben markieren“ (Belegnummer) **oder** „Erneut senden“. Wird nie automatisch nachgeschickt | `scripts/deploy/nachlauf.psd1:322-324`; [[fallstricke]] §31 |
| Einkauf schließt Wareneingang in Sage von Hand → Doppelbestand | Seit v1.99.4 macht das WMS den Sage-Beleg | — | Einkauf informieren: Status „An Sage übergeben“ nicht von Hand schließen | `scripts/deploy/nachlauf.psd1:313-315`; `scripts/deploy/nachlauf.psd1:238-239` |
| Wareneingang: kein Ziel-Lagerplatz wählbar bzw. „Sage-Platz … ist im WMS nicht buchbar“ | Mit `SageWareneingangBelegAktiv` sind nur Sage-Plätze mit „Sage-Buchung erlaubt“ buchbar; auf 4445 hatte keiner der 853 buchbaren Plätze diese Kennung | Lagerplatz-Stammdaten | Zielplätze buchbar machen und „Sage-Buchung erlaubt“ setzen; vorher klären, ob das zusätzliche Lagerbuchungen auslöst (**zu prüfen**, laut Brain ungeprüft) | [[2026-11-12-deploy-fahrplan-buendel-v1-99]] AKE Test-IDEAL; `scripts/deploy/nachlauf.psd1:307-309` |
| „Barcode nicht möglich: Der Sage-Wareneingang ist aus …“, obwohl gerade eingeschaltet | Jemand anderes hat `/ServiceSettings` mit altem Stand gespeichert und den Schalter zurückgesetzt | `/ServiceSettings` neu laden und nachlesen | Absprechen, wer Einstellungen pflegt; neu setzen | [[2026-11-13-ake-regression-akenet-4445-browser-testbericht]] Fortsetzung „Sage-WE + CSV“ |
| Sage-Lagerbuchung: „Run fehlgeschlagen: Ungültiger Objektname "dbo.PurchaseOrderReceipts"“ | Dienst lief mit neuer Version gegen alte DB (während eines Updates) | Zeitpunkt vs. Update-Protokoll; A-1.1 | Verschwindet nach vollständigem Schema; sonst Schema nachziehen | [[2026-11-13-ake-regression-akenet-4445-browser-testbericht]] Fortsetzung 5 |

Schnittstellen und Schalter: [05-schnittstellen.md](05-schnittstellen.md), [04-konfiguration.md](04-konfiguration.md).

## 4. Wareneingang-CSV auf Netzfreigabe (nur AKE)

| Symptom | Ursache | Prüfung | Lösung | Quelle |
|---|---|---|---|---|
| CSV-Fehler „Der Netzwerkname wurde nicht gefunden.“ | UNC-Pfad falsch oder Server/Freigabe nicht erreichbar | `/ServiceSettings` → `WareneingangCsvPfad` (UNC mit Backslashes); vom Dienst-Server aus erreichbar | Pfad korrigieren; „CSV erneut schreiben“ | [[teil-1]] BS16; `scripts/deploy/nachlauf.psd1:334-336` |
| Jede Datei endet mit „Zugriff verweigert“ | Dienst läuft als `LocalSystem` und greift als **Computerkonto** `DOMÄNE\SERVER$` zu; Rechte nur für einen Benutzer reichen nicht | Dienstkonto (P-C1); Rechte auf Freigabe **und** NTFS (P-C2) | Recht „Ändern“ für das tatsächliche Konto auf Freigabe und NTFS | [[fallstricke]] §32; `scripts/deploy/nachlauf.psd1:334-336` |
| „Datei existiert bereits – nicht überschrieben.“ | Datei mit diesem Barcode liegt schon dort; das WMS ersetzt nie eine Datei | Zielordner | Mit dem Leser (enaio) klären, ob verarbeitet; Datei nicht blind löschen | [[fallstricke]] §32; [[teil-1]] BS16 |
| CSV aktiv, aber es entsteht keine Datei | `WareneingangCsvPfad` leer oder `SageWareneingangBelegAktiv` aus; CSV entsteht nur aus Status „Übergeben“ | Update-Skript-Prüfung `Pruefung/WareneingangCsv` (A-4.3) | Pfad setzen / Sage-WE einschalten (nur nach Abnahme) | `scripts/deploy/nachlauf.psd1:142-145`; [[fallstricke]] §32 |

## 5. Zahlen, Kultur, Zeitstempel, Filter

| Symptom | Ursache | Prüfung | Lösung | Quelle |
|---|---|---|---|---|
| Tausendertrenner als Leerzeichen („5 500,000“ statt „5.500,000“), CSV-Zahlen als Text | Bis v1.99.22 keine feste Kultur; Windows 11/ICU `de-AT` nutzt ein geschütztes Leerzeichen | Version der Web-App (Fußzeile) | Seit **v1.99.23** feste Kultur `de-DE` in der Web-App; CSV ohne Tausendertrenner. Der **Dienst** hat keine feste Kultur | `IdealAkeWms/Program.cs:11-16`; [[2026-11-12-ake-regression-dev-v1-99-browser-testbericht]] BV4; [[fallstricke]] §38 |
| Nach Update auf v1.99.23 werden Dezimal-Eingaben anders gelesen | Feste Kultur ändert auf einem Server mit anderer Kultur das Lesen von `decimal`-Feldern | Test: `2,5` in einer Mengen-Maske speichern | Erwartet seit v1.99.23: gespeichert 2,5 (TS-138.8b). akenet01 ist mit Punkt belegt, IDEAL soll `de-DE` sein | [[2026-11-14-v1-99-23-schutz-doppel-absenden]] Deploy-Risiken; `docs/TESTSZENARIEN.md` TS-138.8b |
| Spaltenfilter `GL;0;0;0` liefert viele fremde Plätze | `;` bedeutet im Filter „oder“ | — | Wert in Anführungszeichen setzen: `"GL;0;0;0"` (seit v1.99.13) | [[2026-11-13-ake-regression-akenet-4445-browser-testbericht]] B1.3; `docs/TESTSZENARIEN.md:11749-11759` |
| Zeitstempel 1–2 h zu früh (z. B. `/Users`, `/Articles` „Erstellt am“, Teileverfolgung, Bedarfsmeldung) | Alte Zeilen von vor v1.99.18 wurden teils in UTC geschrieben; Anzeigen rechnen nicht um. **Altdaten werden bewusst nicht korrigiert** | Datum der Zeile vor/nach dem Update auf v1.99.18 | Keine – neue Zeilen stehen in Ortszeit. Bekannte Rest-Stellen: OSEON-Stammdaten mit SQL-Server-Uhr (LZ-1), Marker des Update-Skripts mit SQL-Server-Uhr (UP-3) | [[0025-zeitstempel-in-lokalzeit-des-schreibenden-rechners]]; [[2026-11-14-review-befunde-lokalzeit]]; [[2026-11-14-review-befunde-update-skript]] UP-3 |

## 6. Bedienung: Doppel-Absenden, veraltete Seiten, Kommissionierung

| Symptom | Ursache | Prüfung | Lösung | Quelle |
|---|---|---|---|---|
| „Die Seite ist veraltet – bitte neu laden. Es wurde nichts gebucht/geändert.“ | Seit v1.99.23 senden Wareneingang und Leitstand eine Absicht mit; Seiten von **vor** dem Update haben sie nicht | — | Seite neu laden (auch am Handgerät); nichts wurde gebucht | `IdealAkeWms/Controllers/PurchaseOrdersController.Receive.cs:175`; `IdealAkeWms/Controllers/PickingLeitstandController.cs:382`; `scripts/deploy/nachlauf.psd1:370-372` |
| „Diese Eingabe wurde bereits gebucht – nichts doppelt gebucht.“ / „FA … ist bereits freigegeben – nichts geändert.“ / „Arbeitsgang ist bereits fertig gemeldet.“ | Gewollte Abwehr einer Doppel-Anfrage (Doppeltipp, Netz-Wiederholung, zweites Gerät) | Stand der Bestellung/des FA ansehen; A-5.x | Keine; im Web-Log stehen dazu `[ERR]`-Zeilen (abgefangene UNIQUE-/Deadlock-Fehler) – kein Handlungsbedarf | [[0026-schreibaktionen-senden-die-absicht-der-server-prueft-am-schreibort]]; `docs/TESTSZENARIEN.md` TS-138; [[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec]] QA F3 |
| Zwei gleichzeitige Rücknahmen → HTTP 500 | Bewusstes Restfenster (RowVersion-Konflikt, K-7) | — | Seite neu laden, Stand prüfen | [[0026-schreibaktionen-senden-die-absicht-der-server-prueft-am-schreibort]] Restfenster |
| FA freigegeben, aber ohne Kommissionierauftrag | Vor v1.99.23: Deadlock bei paralleler Freigabe endete als HTTP 500 nach gespeicherter Freigabe (N-2) | A-6.3 | Leitstand: „Fehlende Kommissionieraufträge für alle freigegebenen FAs erzeugen“ | [[01-kommissionierung]] TS-120.10; `scripts/deploy/nachlauf.psd1:290-291` |
| Sammelfreigabe → HTTP 500 („Invalid object name …StuecklistenDB“) | Stückliste nicht lesbar während der Freigabe; Freigabe ist schon gespeichert (offener Befund N-3, belegt auf AKE-DEV) | A-6.3 | wie oben „Fehlende Kommissionieraufträge erzeugen“; Sage-Stücklistensicht prüfen | [[01-kommissionierung]] N-3 |
| App legt keine Kommissionieraufträge an | Kein (aktiver Standard-)Kommissionierbereich; der Seed stand nur im SQL-Skript, nicht in der EF-Migration | A-6.1 | Update-Skript-Prüfung `Pruefung/Kommissionierbereiche` repariert (SQL/133 erneut, idempotent); Standard unter Stammdaten → Kommissionierbereiche | `scripts/deploy/nachlauf.psd1:101-109`; [[2026-11-11-buendel-deploy-update-skript]] 16:24 |
| Zwei offene FAs mit gleicher Kommissionier-Prio | Altbestand vor v1.99.17 | A-6.4 (`Pruefung/DoppeltePrio`) | Im Leitstand per Prio-Eingabe korrigieren; bewusst gleiche Prio ist erlaubt | `scripts/deploy/nachlauf.psd1:166-169`; [[fallstricke]] §35 |
| Lagerbestellung lehnt unbekannte EK-Nummern ab | DUMMY-Artikel fehlt | A-7.4 | `Pruefung/DummyArtikel` repariert (SQL/86 erneut) | `scripts/deploy/nachlauf.psd1:133-137` |
| Menüpunkt/Seite fehlt trotz Recht (Beschichtungsaufträge, Bestellvorschläge) | Rollen `beschichtungsauftrag`, `beschichtungsauftrag_schreiben`, `einkauf` gibt es nicht (kein Seed) | A-7.1 | Update-Skript legt sie nach Bestätigung an; **Zuweisen** bleibt Hand-Schritt unter `/Roles` | `scripts/deploy/nachlauf.psd1:115-132` |

## 7. Mails

| Symptom | Ursache | Prüfung | Lösung | Quelle |
|---|---|---|---|---|
| Keine Dienst-Mails (Lieferverzug, Meldebestand, Fehler, Bestellung) | `MailSettings:PickupDirectory` gesetzt → alle Mails als `.eml` in den Ordner | Dienst-`appsettings*.json`; Umgebungsvariable `MailSettings__PickupDirectory` am Dienst (`reg query`) | Auf Prod Wert entfernen (Update-Skript bricht dort sonst ab) | `scripts/deploy/nachlauf.psd1:158-161`; [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „Einmal je Server“ |
| Lieferverzug an, aber keine Mail | Kein Einkauf-Empfänger eingetragen | A-2.5 (`Pruefung/LieferverzugEmpfaenger`) | `/ServiceSettings` → Lieferverzug-Mail → Einkauf-Empfänger | `scripts/deploy/nachlauf.psd1:138-141` |
| Keine Fehlermail bei Sync-Fehlern | `ErrorNotification:Enabled` aus (Standard) oder keine Empfänger | `/ServiceSettings` Gruppe Fehlermail | Einschalten, Empfänger eintragen | `IDEALAKEWMSService/Services/SyncErrorNotifier.cs:32-43` |
| Externe Empfänger (Lieferanten) bekommen nichts | SMTP-Relay für externe Adressen nicht geklärt (P-M4) | mit Mail-Admin | Relay und Absender klären, erst dann Testbetrieb ausschalten | `scripts/deploy/nachlauf.psd1:331-333`; `README.md` Tabelle „Lieferverzug-Mail“ |

## 8. Update-Skript

| Symptom | Ursache | Prüfung | Lösung | Quelle |
|---|---|---|---|---|
| Altbestand-Trockenlauf bricht ab: „Kein aktiver Standard-Kommissionierbereich (SQL/133-Seed)“ | System bekam die Migration über den Web-Start, ohne Seed | A-6.1 | Seit v1.99.0 repariert die Prüfung `Pruefung/Kommissionierbereiche` nach Bestätigung; danach Trockenlauf erneut | [[2026-11-11-buendel-deploy-update-skript]] 16:24/16:41; `scripts/deploy/nachlauf.psd1:101-105` |
| Altbestand-Trockenlauf meldet `TREFFER: n > 0` | Kommissionier-Altbestand ist zu überführen | Protokoll-Datei `…_trockenlauf.txt` im Log-Ordner | `SCHARF` eintippen (Backup aus diesem Lauf Pflicht); ohne SCHARF Abbruch, Web und Dienst bleiben gestoppt. `TREFFER: 0` = entfällt | `scripts/deploy/nachlauf.psd1:172-177`; `scripts/deploy/Update-IdealAkeWms.ps1:555-589` |
| Warnung „zweiter Trockenlauf TREFFER: (leer)“ | Fehler am Skript, nicht an den Daten (behoben v1.99.16) | Version des Pakets | Ab v1.99.16 nicht mehr; sonst Ausgabe im Log-Ordner prüfen | [[2026-11-12-deploy-fahrplan-buendel-v1-99]] AKE Test-IDEAL; [[fallstricke]] §30 v1.99.16 |
| „Dienst … nach 60 s nicht gestoppt“ | Der Dienst beendet sich nicht innerhalb des Limits; das Skript beendet den Prozess **nicht** gewaltsam | Dienst-Status; Dienst-Log | Nichts kopiert, Schema unverändert, Web läuft. Dienst prüfen bzw. von Hand beenden, Skript neu starten | `scripts/deploy/Update-IdealAkeWms.ps1:767-770`; [[2026-11-11-v1-98-1-update-skript-url-und-dienst-stopp]] |
| Abbruch in Schritt 2: Sicherungspfad existiert nicht (OS-Fehler 3) | Standard-Backup-Pfad der Instanz fehlt auf dem SQL-Server | A-1.4 (`sys.dm_os_file_exists`) | `-BackupPfad <Ordner auf dem SQL-Server>` mitgeben (IDEAL: `E:\SQLBackup\WMS`) | [[2026-11-11-buendel-deploy-update-skript]] 16:09; [[2026-11-12-deploy-fahrplan-buendel-v1-99]] |
| „sqlcmd nicht gefunden“ | Werkzeug fehlt | — | „Microsoft ODBC Driver for SQL Server“ und sqlcmd installieren (idealweb01 erledigt, akenet01 offen laut Brain) | `scripts/deploy/Update-IdealAkeWms.ps1:130`; [[2026-11-12-deploy-fahrplan-buendel-v1-99]] |
| Prüfung RCSI meldet Abweichung | `READ_COMMITTED_SNAPSHOT` aus, FA-Struktur-Import (IDEAL) braucht es an | A-1.6 | Reparatur per `ALTER DATABASE` (bei gestopptem Web/Dienst); braucht Recht `ALTER` (P-B1, A-1.4) – fehlt es, mit `n` antworten, sonst Abbruch | `scripts/deploy/nachlauf.psd1:110-114,220-222` |
| Nach Abbruch in Schritt 5/6 und Neustart fehlen Daten-Skripte (z. B. SQL/110) in der Nachlauf-Liste | Bekannter Skript-Befund UP-1: installierte Version gilt schon als neu | A-1.3 (Marker) gegen Manifest | Daten-Einträge von Hand nachprüfen; **offen** bis zur Korrektur | [[2026-11-14-review-befunde-update-skript]] UP-1 |

Ablauf und Rollback: [03-installation-update-rollback.md](03-installation-update-rollback.md); Sicherung: [10-backup-restore.md](10-backup-restore.md).

---

## 9. Eskalationsweg

**zu prüfen, wer** – im Brain ist keine formale Eskalationsliste hinterlegt. Belegt sind nur Rollen in den Abläufen:

| Stufe | Wann | Wer (laut Brain, zu bestätigen) | Beleg |
|---|---|---|---|
| 1 | Bedienung, Meldungen „bereits …“, „Seite veraltet“ | Keyuser des Bereichs (Lager, Einkauf; Einkauf-Keyuser „Till“ genannt) | `scripts/deploy/nachlauf.psd1:350` |
| 2 | Server, IIS, Dienst, Rechte, SPN, Protected Users, Freigaben | IT des Standorts (AKE bzw. IDEAL) | [[2026-11-11-buendel-deploy-update-skript]] |
| 3 | Anwendungsfehler, Update-Skript, Daten-Skripte, Einstellungen mit Außenwirkung | Entwicklung/Betreuung WMS (Gerald Weichbold laut Deploy-Fahrplan) | [[2026-11-12-deploy-fahrplan-buendel-v1-99]] |
| 4 | Sage-Belege, SData-Endpunkt/Lizenz | Sage-Verantwortliche / Sage-Partner – **zu prüfen** | — |

Für eine Meldung immer mitgeben: System (z. B. AKE-Test, akenet01:4444), Uhrzeit, Version (Fußzeile), Ausschnitt aus Web-/Dienst-Log und
Aktivitäts-Protokoll, Ergebnis der passenden Abfrage aus [08-diagnose-abfragen.md](08-diagnose-abfragen.md).

---

## Verwandte Kapitel

[00-index.md](00-index.md) · [01-architektur-technologie.md](01-architektur-technologie.md) · [02-systeme-umgebungen.md](02-systeme-umgebungen.md) ·
[03-installation-update-rollback.md](03-installation-update-rollback.md) · [04-konfiguration.md](04-konfiguration.md) ·
[05-schnittstellen.md](05-schnittstellen.md) · [06-logs-ueberwachung.md](06-logs-ueberwachung.md) · [08-diagnose-abfragen.md](08-diagnose-abfragen.md) ·
[09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md) · [10-backup-restore.md](10-backup-restore.md)
