# 05 – Schnittstellen zu Fremdsystemen

**Stand:** v1.99.80, 2026-11-25 (Grundsatz um die Direkt-SQL-Ausnahme 4b ergänzt, Konflikt 50014 mit PlatzID, Blechdicke in § 3), v1.99.43, 2026-11-19 · **Zielgruppe:** IT-Administratoren (ohne Entwicklerwissen) · **Zweck:** Je Fremdsystem: in welche Richtung
Daten fließen, welcher Teil (Web oder Dienst) zugreift, welches Konto welche Rechte braucht, wie man die Schnittstelle schaltet und was bei
einem Ausfall passiert.

Übersicht aller Kapitel: [00-index.md](00-index.md). Schalter im Detail: [04-konfiguration.md](04-konfiguration.md). Konten und Rechte:
[09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md). Server je System: [02-systeme-umgebungen.md](02-systeme-umgebungen.md).

Grundsatz: Das WMS **liest** aus den Fremdsystemen und schreibt nur in klar benannten Ausnahmen zurück – an Sage über die
SData-Schnittstelle (Lagerbuchungen, Wareneingangsbelege, Arbeitsgänge in FAs), per SQL **nur** für die Anlage von Lagern und Lagerplätzen
(seit v1.99.71, § 4b, ADR 0036; Schalter aus ausgeliefert). Jeder andere SQL-Zugriff auf Sage ist lesend. (Quelle: [[integrationen]]
Einleitung und Korrektur 2026-11-02, [[0036-lagerplatz-anlage-in-sage-per-direkt-sql-hinter-austauschbarer-schnittstelle]])

---

## 1. Übersicht

| Fremdsystem | Richtung | Teil | Weg | Schalter (Hauptschalter) |
|---|---|---|---|---|
| Sage – SQL-Views/Tabellen | lesen | Dienst (Web nur Stückliste AKE) | SQL, Windows-Anmeldung | `Sync:*Enabled` je Abgleich |
| Sage – SData | lesen | Dienst (Abgleiche), Web (Register Sage-Test) | HTTPS, Basic-Auth | `Sync:Quelle:*`, SData-Abgleiche `Sync:*Enabled` |
| Sage – SData | **schreiben** | nur Dienst | HTTPS POST | `SageLagerbuchungAktiv`, `SageWareneingangBelegAktiv`, `SageFaArbeitsgaengeAnlegenAktiv` (IDEAL, seit v1.99.38) |
| Sage – SQL (Mandanten-DB) | **schreiben** (nur Lager/Lagerplätze anlegen) | nur Dienst | SQL über `SageConnection`, Windows-Anmeldung, Objekt-GRANTs | `SageLagerplatzAnlageAktiv`, `SageLagerAnlageLagertechniken` (seit v1.99.71, § 4b) |
| OSEON / TRUMPF (AKE) | lesen | Dienst (Abgleich), Web (Stücklisten-Fallback) | SQL + gespeicherte Prozedur | `Sync:OseonTrackingEnabled`, `Sync:OseonArticleCategoryEnabled` |
| enaio – Datenbank | lesen | Dienst | SQL-View | `Sync:EnaioDmsEnabled` |
| enaio / Vault – Links | (nur Verweis) | Browser des Anwenders | HTTP-Link | `EnaioLinksAktiv`, `VaultLinksAktiv` |
| Mail (SMTP-Relay / Pickup-Ordner) | senden | Dienst | SMTP | Mail-Schalter, siehe 04 §5.3 |
| CSV-Ablage (UNC) | Dateien schreiben | Dienst | SMB-Freigabe | `WareneingangCsvAktiv` |
| WiCAM-Auftragspool (UNC, IDEAL, seit v1.99.35) | Dateien schreiben | Dienst | SMB-Freigabe | `BfCsvAktiv` (§ 8a) |
| Active Directory (LDAP) | lesen | Web | LDAP | `WindowsAuthBerechtigungsgruppe` |
| Windows-Anmeldung (Negotiate) | Anmeldung | Web (IIS) | Kerberos/NTLM | `WindowsAuthAktiv` |
| Feiertage (date.nager.at) | lesen | Dienst, Web (Import-Knopf) | HTTPS ins Internet | `Sync:FeiertagSyncEnabled` |

Allgemeines Ausfallverhalten im Dienst: Jeder Abgleich läuft für sich gekapselt; fällt ein Fremdsystem aus, laufen die anderen Blöcke weiter.
Der Fehler steht im Aktivitäts-Protokoll (`/SyncLog`) und – wenn `ErrorNotification:Enabled` an ist – in einer Fehlermail.
(Quelle: [[fallstricke]] §5 „Sync-Bloecke sind einzeln gekapselt“, `IDEALAKEWMSService/Workers/SyncWorker.cs:45`, [[services]] „Reader / Zulieferer“)
Überwachung: [06-logs-ueberwachung.md](06-logs-ueberwachung.md); typische Fehlbilder: [07-troubleshooting.md](07-troubleshooting.md).

---

## 2. Sage – SQL lesend

| Punkt | Inhalt | Quelle |
|---|---|---|
| Richtung / Zweck | Sage → WMS: Fertigungsaufträge, Artikel (inkl. Hauptlagerplatz, Meldebestand), Lagerplätze, Lagerbestand, FA-Zusatzinfos; IDEAL zusätzlich FA-Struktur, Arbeitsplätze, Arbeitsgänge, Artikel über eigene Views `vw_IDEAL_AKE_WMS_*` | [[integrationen]] „Sage – Was gelesen wird“ |
| Teil | **Dienst** über `ConnectionStrings:SageConnection` | `IDEALAKEWMSService/Services/SageImportService.cs:46`, `SageArbeitsplatzReader.cs:18`, `SageBestandReader.cs:18`, `SageLagerplatzReader.cs:18`, `SageZusatzinfoReader.cs:25`, `SageFaStrukturReader.cs:35` |
| | **Web (AKE)**: Stückliste live aus `[ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]` – über die **WMS**-Verbindung als datenbankübergreifende Abfrage, nicht über `SageConnection` | `IdealAkeWms/Data/Repositories/BomRepository.cs:44` |
| | Dienst, BOM-Cache: dieselbe View, ebenfalls datenbankübergreifend | `IDEALAKEWMSService/Services/BomCacheSyncService.cs:294` |
| | Dienst, Sicherungsnetz der Lagerbuchung: liest `KHKLagerplatzbuchungen` (Memo-Kennung), Quelle umschaltbar auf SData | `IDEALAKEWMSService/Services/SageBuchungLookupReader.cs:12-15`, [[services]] „Sage-SData-Abgleich“ |
| Server | AKE: `AKESQL20.ake.at`, Datenbank `ake`. IDEAL: eigener Sage-SQL-Server, Name je Installation → [02-systeme-umgebungen.md](02-systeme-umgebungen.md) | `IDEALAKEWMSService/appsettings.json:4`, `scripts/deploy/Install-IdealAkeWms.ps1:355` |
| Konto / Rechte | Windows-Anmeldung (`Trusted_Connection`) mit dem **Dienstkonto**; Leserecht auf die genutzten Views/KHK-Tabellen, IDEAL zusätzlich Leserecht in `PPSImport`. AKE-Web: App-Pool-Konto braucht Leserecht auf die Stücklisten-View in `ake` | `scripts/deploy/Install-IdealAkeWms.ps1:483-486`; Web-Bedarf abgeleitet aus `BomRepository.cs:44` (**zu prüfen** am AKE-Server, welche Rechte heute vergeben sind) |
| Schalter | `Sync:ProductionOrdersEnabled`, `Sync:ArticlesEnabled` (beide Standard **an**), `Sync:LagerplaetzeEnabled`, `Sync:LagerbestandEnabled`, `Sync:FaZusatzinfoEnabled`, `Sync:HierarchicalFaEnabled`, `Sync:ProductionWorkplaceSyncEnabled`, `Sync:OperationConfigSyncEnabled`, `Sync:BomCacheEnabled` | `IdealAkeWms/Models/ServiceSettingDefinitions.cs:259-381` |
| View-Namen | IDEAL je Einstellung (`Sync:ArticlesViewName`, `Sync:FaHierarchy*ViewName`, `Sync:ProductionWorkplaceSyncViewName`, `Sync:OperationConfigSyncViewName`) | [[integrationen]] Zeile „IDEAL ab v1.60.0“ |
| Bei Ausfall | Lesefehler = Lauf mit Fehler + Fehlermail; andere Blöcke laufen weiter. Fehlt eine View: FA-Struktur = Fehler + Mail, sonst Warnung und Überspringen. Leere Antworten lösen keine Löschungen aus (Leer-Schutz, Obergrenzen für Storno/Nullsetzen) | [[integrationen]] Zeile „IDEAL ab v1.60.0“, [[services]] „Sync-Bloecke“ (Caps), [[fallstricke]] §5 |
| FA-Struktur IDEAL: Abgleich statt Komplett-Ersetzen (seit v1.99.37) | Lauf „FA-Hierarchie“ gleicht je fachlichem Schlüssel ab (Ids bleiben stehen). Zeilen, die Sage nicht mehr liefert: der Dienst fragt den Zustand der betroffenen FAs in `KHKPpsFaBelege` ab (nur lesend) – beendet/inaktiv = bleibt gekennzeichnet, nicht entscheidbar = bleibt + Mail „ungeklärt verschwundene Zeilen“, in Sage nicht gefunden oder Position entfernt = wird entfernt. Schutz: leerer Read, kein einziger bisher aktiver HauptFA im Read, doppelte Schlüssel oder Fehler der Sage-Abfrage → **nichts** wird entfernt, Fehlermail. Ganz verschwundene Strukturen räumt der Dienst nach `Sync:FaStrukturAufbewahrungTage` bzw. nach Erledigt/Storno der WMS-Aufträge ab. Offen vor IDEAL-Prod: P-AB4 (Mandantenfilter der Zustandsabfrage) | `IDEALAKEWMSService/Services/FaStrukturAbgleich.cs:6-7,66-77`, `IDEALAKEWMSService/Services/FaHierarchySyncService.cs:77-84,165,168-178,209,250,296`, [[0032-fa-struktur-abgleich-mit-lebenslauf-statt-full-replace]], [[2026-11-17-v1-99-37-fa-struktur-abgleich]] |
| Prüffrage Sage | Welche SQL-Rechte hat das Dienstkonto in der Sage-Datenbank tatsächlich (nur `db_datareader` bzw. Einzel-`SELECT`)? In SSMS als Dienstkonto prüfen: `SELECT * FROM fn_my_permissions(NULL, 'DATABASE');` | – |

Hinweis: Die Skripte in `SQL/AgentJobs/` sind kein aktiver Importweg mehr (01 archiviert, 02 nur Notfall). (Quelle: [[integrationen]] „Fallen dieser Integration“)

---

## 3. Sage – SData lesend

| Punkt | Inhalt | Quelle |
|---|---|---|
| Richtung / Zweck | Sage → WMS: Stammdaten-Abgleiche ohne Direkt-SQL (Lagerplätze, Bestand, Artikel, Arbeitsplätze, wahlweise statt View), dazu nur über SData: PPS-Mitarbeiter, Stillstandsgründe, Kreditoren, Debitoren, Artikel-Lieferanten, offene Bestellungen | [[services]] „Sage-SData-Abgleich (v1.73.0)“ |
| Teil | **Dienst** (Abgleiche, Lesezugriffe vor erneutem Senden); **Web** (Register „Sage-Test“ in den Einstellungen, Verbindungstest; nur Anzeige) | `IDEALAKEWMSService/Program.cs:155-159`, `IdealAkeWms/Program.cs:166-172`, `scripts/deploy/nachlauf.psd1` Eintrag `v1.99.14-SageTestInfo` |
| Endpunkt | `GET {SData:BaseUrl}/sdata/{SData:Application}/{Vertrag}/{SData:Dataset}/…` mit `format=json` | `IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs:12,79,174` |
| Server | AKE-Test: `sagetest01.ake.at`, Port 5493, Dataset `ake_TEST2026;1`. AKE-Prod: Wert in `/ServiceSettings`. IDEAL: **offen** (eigener Sage-Server, SData-Host noch nicht bekannt) | [[sage-sdata-uebersicht]] „Zugang“ |
| Konto / Rechte | SData-Servicebenutzer, HTTP Basic. Benutzer/Kennwort in `/ServiceSettings` (`SData:Username`, `SData:Password`, verschlüsselt). Der Benutzer hat am AKE-Test keinen Zugriff auf den Vertrag `MasterData` (403) | `IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs:96,245`, [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]], [[sage-sdata-uebersicht]] Zeile 94 |
| Netzwerk | Dienst- und Web-Server brauchen HTTPS-Zugang zum SData-Port. Zeitlimit Web 20 s | `IdealAkeWms/Program.cs:167` |
| FA-Struktur IDEAL (v1.99.26) | Dienst, nur `Sync:Quelle:FaStruktur = Vergleich`: `PpsFaBelegeInfo` + `PpsFaBelegePositionenInfo` (Kanten), RealTimeData `dtsFaBelegsuche.Sage.PPS` (beendete Haupt-FAs + `USER_OS*` am FA-Kopf; lesend, nur im Vergleich bis DPS-Freigabe), `Artikel` (Stamm- und Blattfelder), `LagerplaetzeInfo`. Jede Abfrage mit `where` sendet `orderBy` (sonst HTTP 500 an der IDEAL). Web (Sage-Test > Stückliste live) liest `dtsFaBelegsuche` für beendete Haupt-FAs | `IDEALAKEWMSService/Services/Sdata/SdataFaStrukturReader.cs`, `FaStrukturQuelleSwitch.cs`, `IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs` (`BuildUrl`) |
| Schalter | `Sync:Quelle:*` (`View`/`SData`/`Vergleich`), `Sync:PpsMitarbeiterEnabled`, `Sync:StillstandsgruendeEnabled`, `Sync:KreditorenEnabled`, `Sync:DebitorenEnabled`, `Sync:ArtikelLieferantEnabled`, `Sync:EinkaufBestellungenEnabled`; TLS-Prüfung `SageLagerbuchungSslZertifikatPruefen` | `ServiceSettingDefinitions.cs:330-361` |
| Anstoß nach Sage-Wareneingang (seit v1.99.31) | Hat der `SageBookingWorker` Wareneingänge an Sage übergeben, liest er danach einmal die offenen Bestellungen (nur lesend, dieselbe Abfrage wie der reguläre Lauf; höchstens alle `Sync:EinkaufBestellungenNachWeMinuten` Minuten, Default 2). Läuft der reguläre Lauf gerade, bleibt der Anstoß offen (eine Sperre, nie zwei Komplett-Ersetzungen parallel). Lesefehler: Warnungslauf „Sage-Wareneingang“, der reguläre Lauf holt es nach | `IDEALAKEWMSService/Workers/SageBookingWorker.cs:93,142`, `IDEALAKEWMSService/Services/Sdata/EinkaufBestellungenSyncService.cs` (`TryRunExclusiveAsync`) |
| AB-Nummer / Kommission der offenen Bestellungen (seit v1.99.33) | Lauf „Offene Bestellungen“ liest zusätzlich den Bestellkopf über RealTimeData `dtsEKBelegMst.Sage.Wawi` (nur GET, Filter nur auf `BelID`, Blöcke zu 50) die Benutzerfelder `USER_AB` und `USER_Kom` (Namen einstellbar, [04 § 5.6](04-konfiguration.md)); gespeichert je Position in `SagePurchaseOrderLines.AbNummer`/`Kommission` (SQL/156), nur Anzeige, nicht in der Wareneingangs-CSV. Zu lange Werte werden nie gekürzt, sondern gezählt (`kopf_zu_lang`); ist der Kopf nicht lesbar: Warnung im Log, die Positionen werden trotzdem übernommen | `IDEALAKEWMSService/Services/Sdata/EinkaufBestellungenSyncService.cs:35,52,150,251-254,267-293`, `IdealAkeWms/Models/SagePurchaseOrderLine.cs:65` |
| Blechteile-Nachlese (IDEAL, seit v1.99.72 mit Blechdicke) | Der Artikel-Abgleich liest mit `Sync:BlechteileFelderNachlesen` die Blechfertigungs-Benutzerfelder per SData nach, seit v1.99.72 auch `USER_OSBlechdicke`. Ohne Einstellungszeile bzw. leer: Blechdicke geleert, Hinweis im Protokoll (seit v1.99.79). Scheitert die Nachlese: Warnung „Blechteile-Felder (SData-Nachlese) fehlgeschlagen: … – Artikel-Abgleich (View) bleibt gültig, Merkmale unverändert.“ | `IDEALAKEWMSService/Services/Sdata/SdataArtikelReader.cs:359-364`, `IDEALAKEWMSService/Services/BlechteileFelderNachlese.cs:48`, [04 § 5.6](04-konfiguration.md) |
| Lagerplatz-Abgleich | seit v1.99.79 verbindet der SQL-Leser (Quelle View) Lagerorte und Plätze über Mandant **und** Lagerkennung (vorher nur Lagerkennung). Eine Lagerkennung, die länger ist als im WMS erlaubt, meldet der Abgleich mit „Lagerkennung '…' ist zu lang (…), Lagerort uebersprungen.“ | `IDEALAKEWMSService/Services/SageLagerplatzReader.cs:25`, `IDEALAKEWMSService/Services/LagerplatzSyncService.cs:329-333` |
| Bei Ausfall | Lesefehler: Lauf mit Fehler + Fehlermail. Leere Antwort ändert nichts (Leer-Schutz). Kennwort nicht entschlüsselbar: **kein** Aufruf an Sage, Meldung „SData-Kennwort nicht entschlüsselbar – in den Service-Einstellungen neu eingeben“ | `README.md:805,812`, [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]] „Entscheidung“ |

---

## 4. Sage – SData schreibend

**Einziger Schreibweg an Sage.** Nur der Dienst schreibt, und nur, wenn der jeweilige Schalter an ist.

| Punkt | Lagerbuchung | Wareneingangsbeleg | Quelle |
|---|---|---|---|
| Zweck | Manuelle Ein-/Ausbuchungen (optional Umbuchungen) an Sage melden | Wareneingang zu einer Sage-Bestellung als Sage-Beleg anlegen (Bestellung → Lieferschein) | [[integrationen]] „Sage-Lagerbuchung“, „Sage-Wareneingangsbeleg“ |
| Ablauf | Web legt beim Buchen einen Eintrag in `SageBookingQueueItems` an; der Dienst (`SageBookingWorker`) sendet | Web merkt beim Buchen vor; der Dienst sendet im selben Takt | [[integrationen]], [[services]] „Sage-Wareneingang“ |
| Endpunkt | `POST …/$service/LagerbuchungService` | `POST …/$service/StandardBelegService` | [[integrationen]] Zeilen 198 und 233 |
| Konto | derselbe SData-Servicebenutzer wie beim Lesen | dito | `ServiceSettingDefinitions.cs:125-133` |
| Schalter | `SageLagerbuchungAktiv` **und** Lagerplatz-Kennzeichen „Sage-Buchung erlaubt“; Umbuchungen zusätzlich `SageLagerbuchungUmbuchungAktiv` | `SageWareneingangBelegAktiv` (unabhängig vom Lagerbuchungs-Schalter) | `IDEALAKEWMSService/Workers/SageBookingWorker.cs:67-75`, [[integrationen]] „Enqueue“ |
| Takt | alle 20 s (`Sync:SageLagerbuchungIntervalSeconds`, mindestens 5 s) | dito | `SageBookingWorker.cs:60-62` |
| Inhalt (ab 1.99.68) | Kopf-Memo `IdealAkeWms Lagerbuchung SM#…#` unverändert; je Zeile `Referenz` = `SM#…# Abteilung Benutzer` (Sage varchar(31), am Ende gekürzt, Kennung nie) und Zeilen-`Memo` = `WMS: SM#…# Benutzer[ Abteilung:…]`. Benutzer/Abteilung aus der Bewegung (`StockMovements.CreatedBy`/`.Abteilung`), auch bei Nacherfassung und erneutem Senden | Kopf-Memo `WMS: WE#…# Benutzer` (vorher nur `WE#…#`), Positionsmemo unverändert | `IDEALAKEWMSService/Services/SageReferenz.cs`, `SageLagerbuchungPayloadBuilder.cs`, `SageWareneingangPayload.cs` |
| Schutz vor Doppelbuchung | Kennung `SM#…#` im Sage-Memo; vor jedem erneuten Senden Suche in Sage. Gilt nur bei **genau einem** laufenden Dienst je WMS-Datenbank | Kennung `WE#…#`; vor erneutem Senden Suche in Sage | [[fallstricke]] §8 „Idempotenz nur bei genau EINER Worker-Instanz“, §31 |
| Bei Ausfall | Einträge bleiben offen bzw. „Fehler“; automatische Wiederholung bis `Sync:SageLagerbuchungMaxRetries` (5); „Gesendet“ ohne Antwort gilt nach 10 min als hängend und wird über die Suche geklärt; schlägt die Suche fehl, wird **nicht** neu gesendet. Ab 50 Fehlern je Lauf Fehlermail | Fehler bleibt stehen; Wiederholung nur nach `Sync:SageWareneingangMaxWiederholungen` mit vorheriger Suche; hängend → „Ergebnis unklar“ (von Hand klären). Bei ausgeschaltetem Schalter gehen Liegengebliebene auf „Klären“, nie automatisch nachsenden | `ServiceSettingDefinitions.cs:303-305,325-327`, `README.md:813-815`, [[services]] „Sage-Wareneingang“, [[fallstricke]] §31 |
| Testsysteme | `Sync:SageSystemKuerzel` setzen, wenn mehrere WMS in dieselbe Sage-Test-DB schreiben; Produktion leer | dito | [[fallstricke]] §31 „Memo-Kennungen …“ |

**Prüffragen Sage (für den Menschen):**
- Ist der `LagerbuchungService` am **produktiven** AKE-Sage inzwischen lizenziert? Stand 2026-11-03: nur am Testsystem, in Produktion nicht
  lizenziert. (Quelle: [[sage-sdata-uebersicht]] Zeile 67)
- IDEAL: Gibt es einen SData-Endpunkt und eine Lizenz? Bis dahin alle Sage-Schreibschalter an IDEAL aus.
  (Quelle: `ServiceSettingDefinitions.cs:70-72`, `secondbrain/aufgaben/2026-11-12-deploy-fahrplan-buendel-v1-99.md:91`)

Vor dem Einschalten von `SageWareneingangBelegAktiv` an einem AKE-System: Wareneingangs-Zielplätze im WMS buchbar und „Sage-Buchung erlaubt“;
auf AKE Test-IDEAL war das bei keinem Platz gesetzt. (Quelle: `secondbrain/aufgaben/2026-11-12-deploy-fahrplan-buendel-v1-99.md:58-60`)

### 4a. Sage – Arbeitsgänge in Fertigungsaufträgen anlegen (IDEAL, seit v1.99.38)

Dritter Schreibweg an Sage, eigener Schalter. Bis zum Schreibtest TS-159.x (Feldnamen P-1..P-4 unbelegt) bleibt er **aus**.

| Punkt | Inhalt | Quelle |
|---|---|---|
| Zweck | Fehlende Arbeitsgänge (Sage-Arbeitsgangnummer aus den Arbeitsgang-Stammdaten) an einem Sage-FA anlegen | [[2026-11-17-v1-99-38-arbeitsgaenge-in-sage-fa-anlegen]] |
| Ablauf | Web: Stammdaten → Arbeitsgänge → Sage, Knopf reiht je FA und Arbeitsgang einen Eintrag in `SageFaArbeitsgangAnlagen` ein (SQL/160). Dienst (`SageBookingWorker`) liest **vor jedem Senden** den FA in Sage (nur GET): Arbeitsgang schon da = „In Sage vorhanden“ ohne Senden; FA fehlt/beendet/begonnen = Fehler ohne Senden; Lesefehler = nichts senden | `IDEALAKEWMSService/Services/SageFaArbeitsgangService.cs:37-41`, `IdealAkeWms/Controllers/SageFaArbeitsgaengeController.cs` |
| Endpunkt | `POST …/$service/FertigungsauftragService`, `ServiceOption` 4 (Arbeitsgang-Position anlegen/ändern), je Arbeitsgang ein Aufruf; Menge = Fertigungsmenge, `Bearbeiter` = Einstellung; **kein** Einlasten (Option 5) | `IDEALAKEWMSService/Services/SageFaArbeitsgangClient.cs:9,15-16,25-42` |
| Konto | derselbe SData-Servicebenutzer; zusätzlich `Sync:SageFaArbeitsgaengeBearbeiter` (leer = nichts gesendet, Meldung „Sage-Bearbeiter für AG-Anlage fehlt“) | `IdealAkeWms/Services/SageFaArbeitsgaenge.cs:16-45`, [04 § 5.4](04-konfiguration.md) |
| Erfolg | Eine 2xx-Antwort allein genügt nicht (seit v1.99.39): der Dienst liest den FA nach; „Angelegt“ erst, wenn die Sage-Arbeitsgangnummer an einer AG-Position (`RessourceNummer`) steht. Kein Treffer bzw. Lesefehler beim Nachlesen = „Antwort unklar“ | `SageFaArbeitsgangService.cs:196`, [[2026-11-17-v1-99-39-nachbesserung-abend]] |
| Bei Ausfall | Fehler: automatische Wiederholung bis `Sync:SageFaArbeitsgaengeMaxWiederholungen` (3) im Abstand `…WiederholAbstandMinuten` (15). „Antwort unklar“ wird **nie** automatisch wiederholt (seit v1.99.42): in Sage prüfen, dann „In Sage vorhanden – bestätigen“ (nur Admin, schreibt nichts an Sage, geht auch bei ausgeschaltetem Schalter) oder erneut einreihen. Protokoll „Sage-FA-Arbeitsgänge“, Fehlermail | `IdealAkeWms/Services/SageFaArbeitsgaenge.cs:16-45`, [[2026-11-18-v1-99-42-nachbesserungen]], [[fallstricke]] §49 |
| Update-Skript | `Pruefung/sage-fa-ag-sdata-zugang` (IDEAL): Schalter an, aber Bearbeiter oder `SData:BaseUrl` leer | `scripts/deploy/nachlauf.psd1:203-206` |

### 4b. Sage – Lager und Lagerplätze per Direkt-SQL anlegen (seit v1.99.71)

Einziger Weg, auf dem das WMS **direkt** in die Sage-Mandanten-DB schreibt (Entscheid ADR 0036: Sage-Standard-SData bietet keine
Lagerplatz-Anlage). Beide Einstellungen werden **aus** ausgeliefert.

| Punkt | Inhalt | Quelle |
|---|---|---|
| Zweck | Im WMS erfasste Lagerplätze (Code `Lager;D1;D2;D3`) und fehlende Lager in Sage anlegen; danach sind es normale Sage-Plätze (Quelle „Sage“) | Spec `2026-11-24-lagerplatz-anlage-wms-nach-sage` |
| Ablauf | Web: *Stammdaten → Lagerplätze → Lagerplätze in Sage anlegen* legt Plätze (und ggf. das Lager) im WMS an und reiht je Datensatz einen Eintrag in `SageStammdatenOutbox` ein (Typ `StorageArea` bzw. `StorageLocation`). Dienst (`SageBookingWorker`, Schritt „Lagerplätze → Sage“): erst Lager, dann deren Plätze; Status vor dem Aufruf `Gesendet` | `IdealAkeWms/Controllers/StorageLocationsController.cs` (`CreateInSage`), `IDEALAKEWMSService/Services/SageLagerplatzAnlageService.cs` |
| SQL | je Platz bzw. Lager **eine** Transaktion mit `XACT_ABORT`, `DEADLOCK_PRIORITY LOW`, Prüfen und Anlegen unter `UPDLOCK, HOLDLOCK`; Nummern über `spSysTan`; Ergebnis erst nach dem `COMMIT`. Idempotent: ein schon vorhandener Platz bzw. ein vorhandenes Lager wird nur übernommen | `IDEALAKEWMSService/Services/SqlSageLagerplatzAnlage.cs` |
| Konto | Dienstkonto der bestehenden `SageConnection` (kein neuer Connection String, `appsettings.json` unverändert) mit Objekt-GRANTs (unten) | [09 § 2.1](09-sicherheit-konten-rechte.md) |
| Fehler | fachlich (Konflikt, Lager fehlt, Koordinaten passen nicht …) = Eintrag `Fehler`, Grund in der Spalte „Übergabe-Fehler“, keine Mail, Knopf „Erneut senden“. Seit v1.99.79 nennt der Konflikt (SQL-Fehler 50014) die belegenden Sage-Plätze: „Konflikt in Sage: Koordinaten oder Kurzbezeichnung schon anders belegt (PlatzID <n> <Lager> = <Kurzbezeichnung>, …)“ – den genannten Platz in Sage prüfen, nicht einfach erneut senden. Der Text nutzt `STRING_AGG … WITHIN GROUP` (SQL Server ab 2017; Version des Sage-Servers IDEAL noch zu prüfen: `SELECT @@VERSION`). Technisch (Timeout, Deadlock, fehlendes Recht, Netz) = Eintrag bleibt `Offen` mit „Letzter Versuch fehlgeschlagen: …“, der nächste Takt versucht es erneut; Fehlermail **nur bei neuem Fehlertext**. Hängend `Gesendet` länger als `Sync:SageLagerbuchungStuckMinutes` = wird erneut gesendet (der Befehl ist idempotent) | `SqlSageLagerplatzAnlage.cs` (`IstTechnisch`), `SageBookingWorker.cs` (`MeldenBeimWechsel`) |
| Protokoll | Dienst „Lagerplätze → Sage“ (`SageLagerplatzAnlage`); je Lauf die Info `Sage-Datenbank: <Server>/<DB>`; Zähler `angelegt`, `vorhanden`, `fehler`, `uebersprungen`, `lager_angelegt`, `lager_vorhanden`, `lager_fehler` | `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs` |
| Update-Skript | Info `v1.99.71-SageLagerplatzRechte` (Vorher), Einstellungen `v1.99.71-SageLagerplatzAnlageAktiv`, `v1.99.71-SageLagerAnlageLagertechniken`, Prüfung `Pruefung/LagerorteFuerSageAnlage` (Schalter an, aber Quelle Lagerplätze nicht SData oder keine aktiven Lagerorte) | `scripts/deploy/nachlauf.psd1` |

**Rechte (DBA, je Standort und je Sage-DB: erst Test, dann Produktiv).** Konto: AKE `AKENB\svc_idealakewms`; IDEAL: Konto des IDEAL-Dienstes
(Dienst-Eigenschaften); Testsystem `idealakeweb01:88`: dessen Dienstkonto; DEV AKE: `sc qc AKE_WMS_DEV_Service` → `SERVICE_START_NAME`, GRANT nur
auf `ake_TEST2026`.

```sql
USE [<SageMandantenDB>];   -- AKE: ake_TEST2026 (DEV AKE), dann ake; IDEAL: IDEAL_TEST_2026_05_03, dann Produktiv-DB
-- Datenbank-Benutzer muss existieren (heute liest der Dienst schon; sonst: CREATE USER [<Konto>] FOR LOGIN [<Konto>];)
GRANT INSERT ON dbo.KHKLagerplaetze TO [<Konto>];
GRANT INSERT, UPDATE ON dbo.KHKLagerorte TO [<Konto>];
GRANT EXECUTE ON dbo.spSysTan TO [<Konto>];   -- KHKTan ueber Ownership-Chaining (beide dbo, AKE P-4)
-- nur falls P-4 SELECT = 0 zeigt (kein db_datareader):
GRANT SELECT ON dbo.KHKLagerorte TO [<Konto>];  GRANT SELECT ON dbo.KHKLagerplaetze TO [<Konto>];
GRANT SELECT ON dbo.KHKLagertechnikenArten TO [<Konto>];  GRANT SELECT ON dbo.KHKGruppen TO [<Konto>];
GRANT SELECT ON dbo.KHKArtikelVariantenLagerplatz TO [<Konto>];
```

Nachprüfung P-4 **aus dem Dienstkontext**. Die Prüfabfragen P-1 bis P-10 stehen in der Spec (Abschnitt „Migrations-/SQL-Auswirkungen“) und in
`secondbrain/backlog/anhaenge/2026-11-24-pruefabfragen-lagerplatzanlage-sage.sql`; sie sind reine `SELECT`.

**Einschalt-Checkliste je Standort** (erst Test-/DEV-System, dann Produktiv):

- [ ] Deploy mit beiden Einstellungen **aus** (Nachlauf-Liste bestätigt).
- [ ] Ziel-DB belegen: mit der Verbindung des Dienstes `SELECT @@SERVERNAME AS Server, DB_NAME() AS Db` – Test/DEV muss `ake_TEST2026` bzw.
      `IDEAL_TEST_*` zeigen, nie `ake` bzw. die IDEAL-Produktiv-DB.
- [ ] Prüfabfragen: **IDEAL** P-1 bis P-9 vor dem Einschalten (Pflicht, insbesondere `USER_`-Pflichtfelder in `KHKLagerplaetze`/`KHKLagerorte`);
      **AKE** P-1 bis P-7 sind am 24.11.2026 belegt, nach dem GRANT P-4, P-10 zur Information.
- [ ] GRANT wie oben, danach P-4 aus dem Dienstkontext.
- [ ] `Sync:Quelle:Lagerplaetze` = `SData` und Lagerplatz-Abgleich gelaufen (sonst bietet das Formular keinen Lagerort an; Prüfung
      `Pruefung/LagerorteFuerSageAnlage`).
- [ ] Produktiv zusätzlich: Sage-DB-Backup, Abnahmetest am Testmandanten desselben Standorts bestanden.
- [ ] `SageLagerplatzAnlageAktiv` an; nach dem ersten Takt zeigt
      `SELECT TOP 1 Message FROM SyncLogs WHERE Message LIKE N'Sage-Datenbank:%' ORDER BY Id DESC` die erwartete Sage-DB.
- [ ] `SageLagerAnlageLagertechniken` nur auf die Stufe stellen, deren Abnahmevergleich am Testmandanten 0 Abweichungen zeigte.
- [ ] Nach jedem Sage-Update: P-1 bis P-3 (P-9) erneut prüfen, Schalter bis dahin aus.

Diagnose: [08 A-8.7](08-diagnose-abfragen.md).

---

## 5. OSEON / TRUMPF (nur AKE)

| Punkt | Inhalt | Quelle |
|---|---|---|
| Richtung / Zweck | OSEON → WMS: Kundenaufträge, Subaufträge, Arbeitsgänge (Teileverfolgung), Artikelkategorien; Stückliste als Rückfall, wenn Sage keine liefert | [[integrationen]] „OSEON / TRUMPF“ |
| Teil | Dienst (Abgleich, BOM-Cache); Web (Stücklisten-Rückfall über die Prozedur `sp_AKE_Kommissionierung_OseonStuecklistenDB`) | `IDEALAKEWMSService/Services/OseonSyncService.cs:32`, `IDEALAKEWMSService/Services/BomCacheSyncService.cs:338`, `IdealAkeWms/Data/Repositories/BomRepository.cs:18,69-71` |
| Server | `aketrumpf01.ake.at\TRUMPFSQL2`, Datenbank `T1000_V01_V001` | `IdealAkeWms/appsettings.json:4`, `IDEALAKEWMSService/appsettings.json:5` |
| Konto / Rechte | Windows-Anmeldung. Dienstkonto: Leserecht auf die genutzten Tabellen; App-Pool-Konto: Ausführen der Prozedur. **Zu prüfen** am OSEON-Server, welche Rechte vergeben sind | Bedarf abgeleitet aus den Fundstellen oben |
| Schreiben | Keiner. Der frühere Schalter `OseonRueckmeldungAktiv` wurde in v1.79.0 entfernt (kein Leser) | `IdealAkeWms/Models/AppSettingDefinitions.cs:182` |
| Schalter | `Sync:OseonTrackingEnabled`, `Sync:OseonArticleCategoryEnabled` (Standard aus); Modul `TeileverfolgungAktiv` | `ServiceSettingDefinitions.cs:265-266`, [[integrationen]] „Fallen“ |
| IDEAL | `OseonConnection` bleibt leer, der Key muss aber vorhanden sein | `scripts/deploy/Install-IdealAkeWms.ps1:263` |
| Bei Ausfall | Abgleich: Fehler im Lauf, andere Blöcke laufen weiter. Stückliste: liefert Sage nichts und ist OSEON nicht erreichbar, fängt die Web-App den Verbindungsfehler nicht ab – Folge vermutlich Fehlerseite (**zu prüfen**) | `IdealAkeWms/Data/Repositories/BomRepository.cs:53-59,62-67` |

---

## 6. enaio (DMS) und Vault – Datenbank-Abgleich und Links

| Punkt | enaio-Abgleich | enaio-Links | Vault-Links | Quelle |
|---|---|---|---|---|
| Richtung / Zweck | enaio → WMS: Werkstattaufträge/Zeichnungen je FA (`EnaioDmsDocuments`, voller Abgleich ohne Delta) | Symbol an der FA-Nummer öffnet den enaio-Viewer | Link an der Artikelnummer öffnet die Vault-Suche | [[integrationen]] „enaio“, `README.md:504-507` |
| Teil | Dienst | Browser des Anwenders | Browser des Anwenders | – |
| Endpunkt | View `[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]` (Einstellung `Sync:EnaioDmsViewName`) über `EnaioDmsConnection` (`AKESQL20.ake.at`, DB `enaio`) | Vorlage `EnaioLinkVorlage` (Standard `http://akeosapp.ake.at/oscontentviewer/viewer/{ObjektId}/…`) | Vorlage `VaultLinkVorlage` (Standard `http://akevault24.ake.at/AutodeskTC/…`) | `ServiceSettingDefinitions.cs:268`, `IDEALAKEWMSService/appsettings.json:6`, `AppSettingDefinitions.cs:215,220` |
| Konto / Rechte | Dienstkonto: Leserecht auf die View | Anwender braucht Zugang zum enaio-Viewer (Server greift nicht zu) | Anwender braucht Zugang zum Vault | – |
| Schalter | `Sync:EnaioDmsEnabled` (aus) | `EnaioLinksAktiv` (an) – unabhängig vom Abgleich | `VaultLinksAktiv` (an); IDEAL ohne Vault ausschalten | `ServiceSettingDefinitions.cs:267`, `README.md:504-507` |
| Bei Ausfall | Lauf mit Fehler; vorhandene Verknüpfungen bleiben | Link führt im Browser ins Leere; WMS unberührt | dito | [[fallstricke]] §5 |

IDEAL: `EnaioDmsConnection` leer, enaio-Abgleich aus. (Quelle: `scripts/deploy/Install-IdealAkeWms.ps1:264,357`)

---

## 7. Mail – SMTP-Relay bzw. Pickup-Ordner

| Punkt | Inhalt | Quelle |
|---|---|---|
| Zweck | Fehlermails, Meldebestand, Bedarfsmeldungen, Lager-/Glasbestellungen, Lieferverzug (Einkauf-Sammelliste, Lieferanten-Erinnerung), Sammelmeldungen der IDEAL-Abgleiche | [[integrationen]] „SMTP“, [[services]] „Lieferverzug-Mails“ |
| Teil | nur **Dienst** (`MailService`) | `IDEALAKEWMSService/Program.cs:100` |
| Endpunkt | `MailSettings:SmtpHost`/`SmtpPort` (Standard 25) in der Dienst-`appsettings.json`; Absender `MailSettings:FromAddress` | `IDEALAKEWMSService/appsettings.json:8-16`, `IDEALAKEWMSService/Services/MailService.cs:32,39-40,70` |
| Verschlüsselung | `SmtpUseSsl = true` → TLS ab Verbindungsbeginn; sonst **ohne** Verschlüsselung (kein STARTTLS) | `IDEALAKEWMSService/Services/MailService.cs:78-79` |
| Konto | optional SMTP-Benutzer (`SmtpUsername`/`SmtpPassword`, Datei) – meist anonymes Relay. Für Lieferanten-Mails muss das Relay **externe Empfänger** und den Absender `Lieferverzug:KreditorAbsender` zulassen (IT klären) | `MailService.cs:72-82`, `ServiceSettingDefinitions.cs:196` |
| Pickup-Ordner | `MailSettings:PickupDirectory` (nur DEV/QA): Mails als `.eml` ablegen statt senden; Dienstkonto braucht Schreibrecht auf den Ordner (legt ihn selbst an) | `MailService.cs:30-67` |
| Schalter | siehe [04-konfiguration.md](04-konfiguration.md) §5.3 | – |
| Bei Ausfall | `SmtpHost` leer → Mail übersprungen, Warnung im Log. Relay nicht erreichbar → Fehler im Log (`logs/notifications/`), der aufrufende Lauf meldet „nicht gesendet“; die Fehlermail selbst wirft nie | `MailService.cs:33-37,90-94`, `IDEALAKEWMSService/Services/LieferverzugMailService.cs:136,164`, [[services]] „Reader / Zulieferer“ |
| Achtung Produktion | Ist `PickupDirectory` gesetzt, geht **nichts** raus, das Protokoll zeigt trotzdem „gesendet“. Das Update-Skript prüft das (`Pruefung/MailPickup`) | [[fallstricke]] §5 „Mails pruefen ohne Versand“, `scripts/deploy/nachlauf.psd1:235-238` |

---

## 8. CSV-Ablage auf UNC-Freigabe (Wareneingang, AKE)

| Punkt | Inhalt | Quelle |
|---|---|---|
| Richtung / Zweck | WMS → Netzfreigabe: je an Sage übergebenem Wareneingang mit Barcode eine Datei `<Barcode>.csv`; ein eigener enaio-Import liest und räumt die Dateien selbst auf | [[integrationen]] „Wareneingang-CSV“ |
| Teil | **Dienst**, Schritt im `SageBookingWorker` nach dem Sage-Wareneingang | `IDEALAKEWMSService/Workers/SageBookingWorker.cs:89-93` |
| Endpunkt | `WareneingangCsvPfad`, nur UNC `\\server\freigabe[\ordner]`; der Server prüft nur die Form, nicht die Erreichbarkeit | `ServiceSettingDefinitions.cs:313-314`, [[integrationen]] „Wareneingang-CSV“ |
| Konto / Rechte | Dienstkonto. Läuft es als `LocalSystem`, greift es als **Computerkonto `DOMÄNE\SERVER$`** zu. Recht „Ändern“ auf Freigabe **und** NTFS (anlegen, schreiben, umbenennen, eigene `.tmp` löschen) | [[fallstricke]] §32 „Dienstkonto LocalSystem …“, `scripts/deploy/nachlauf.psd1:334-337` |
| Schreibweise | erst `<Barcode>.tmp`, dann Umbenennen auf `.csv` (nie überschreiben); höchstens 10 Dateien je Takt. Seit v1.99.33 (Sammellieferschein, mehrere Wareneingänge mit demselben Barcode): erste Datei `<Barcode>.csv`, weitere `<Barcode>_1.csv`, `_2` …; belegte Namen (Datenbank oder Ordner) werden übersprungen. Das Update-Skript prüft Kollisionen (`Pruefung/WareneingangCsvKollision`, AKE). Prod erst nach Bestätigung des enaio-Imports (E-1) | `IDEALAKEWMSService/Services/WareneingangCsvService.cs:16-18`, `scripts/deploy/nachlauf.psd1:219-222`, [[2026-11-17-v1-99-33-wareneingang-sammellieferschein-bestellkopf]] | [[services]] „Wareneingang-CSV“ |
| Schalter | `WareneingangCsvAktiv` **und** `SageWareneingangBelegAktiv` **und** Pfad gesetzt | [[services]] „Wareneingang-CSV“ |
| Bei Ausfall | Schreibfehler (Netz, Recht) → Wiederholung bis `Sync:WareneingangCsvMaxWiederholungen` (3) im Abstand `…WiederholAbstandMinuten` (15); Namenskollision oder ungültiger Barcode nie automatisch; Status je Vorgang in „Sage-Wareneingänge“ (Spalte CSV); Fehler stören die Sage-Buchungen nicht | [[services]] „Wareneingang-CSV“, `SageBookingWorker.cs:87-103` |
| Prüffrage | Freigabe und Format (unten) mit dem Betreiber des enaio-Imports **vor dem Einschalten** abgestimmt? (Hand-Schritte P-C1/P-C2). Seit v1.99.29 ohne Kopfzeile: ein Import, der eine Kopfzeile überspringt, verliert sonst die einzige Datenzeile | `secondbrain/aufgaben/2026-11-12-deploy-fahrplan-buendel-v1-99.md:97` |

**Dateiformat (seit v1.99.29):**

| Merkmal | Wert | Quelle |
|---|---|---|
| Zeilen | **genau eine Datenzeile, keine Kopfzeile**; enaio ordnet nach Position zu | `IDEALAKEWMSService/Services/WareneingangCsvService.cs:131-145` |
| Kodierung | UTF-8 **mit BOM**, Zeilenende CRLF | `IDEALAKEWMSService/Services/WareneingangCsvService.cs:118` |
| Trennzeichen | `;`; Felder mit `;`, `"` oder Zeilenumbruch in Anführungszeichen (inneres `"` verdoppelt); leere Werte leer | `WareneingangCsvService.cs:138-139` |
| Felder (9, in dieser Reihenfolge) | 1 Barcode · 2 Kreditornummer · 3 Kreditorname · 4 Bestellnummer · 5 Lieferschein · 6 Sage-Belegnummer (WE) · 7 **Erfasser** · 8 Datum (Erfassung im WMS) · 9 übergeben am (an Sage) | `WareneingangCsvService.cs:143-144` |
| Datumsformat | `yyyy-MM-dd HH:mm`, kulturunabhängig | `WareneingangCsvService.cs:140` |
| Erfasser | Windows-Kontoname (**sAMAccountName**) des buchenden WMS-Benutzers **ohne Domäne** (z. B. `jmuster`); hat der Benutzer kein Windows-Konto (lokaler WMS-Benutzer): sein **WMS-Name**; ohne Benutzer an der Buchung: Ersteller des Wareneingangs. Nicht die Windows-Sitzung des Rechners | `WareneingangCsvService.cs:134-142`, `IdealAkeWms/Services/WindowsAccountHelper.cs:7` |

Bis v1.99.28 stand eine Kopfzeile in der Datei und als Erfasser der WMS-Anzeigename. Dateien, die vor dem Update geschrieben wurden, behalten
dieses alte Format. Mails, Druck und Sage-Belege sind von der Änderung nicht betroffen.

---

## 8a. WiCAM-Auftragspool – BF-Aufträge (IDEAL, seit v1.99.35)

| Punkt | Inhalt | Quelle |
|---|---|---|
| Richtung / Zweck | WMS → WiCAM: je freigegebenem BF-Auftrag eine Datei `<BF-Nummer>.csv` (z. B. `BF00000042.csv`); WiCAM legt daraus einen Auftrag an. Kein Sage-Bezug | Spec `2026-11-13-blechfertigung-bf-auftraege-wicam-csv` F4 |
| Teil | **Dienst**, eigener Schritt im `SageBookingWorker` (nicht an den Sage-Wareneingang gebunden) | `IDEALAKEWMSService/Workers/SageBookingWorker.cs:120-134,236-255`, `IDEALAKEWMSService/Services/BfCsvService.cs` |
| Endpunkt | `BfCsvPfad`, nur UNC; geplant `\\idealcam.ideal.ideal-ake.at\fertigung\u\pn\apools\` | `ServiceSettingDefinitions.cs:343-344` |
| Konto / Rechte | Dienstkonto (bei `LocalSystem` das Computerkonto `DOMÄNE\SERVER$`): „Ändern“ auf Freigabe und NTFS | `scripts/deploy/nachlauf.psd1` (`v1.99.35-BfPoolRecht`) |
| Schreibweise | erst `<BF-Nummer>.tmp`, dann Umbenennen auf `.csv`, nie überschreiben; höchstens 10 Dateien je Takt | `BfCsvService.cs:91-110` |
| Schalter | `BfCsvAktiv` **und** Pfad gesetzt; die Web-Oberfläche braucht `BlechfertigungAktiv` | – |
| Bei Ausfall | Schreibfehler → Wiederholung bis `Sync:BfCsvMaxWiederholungen` (3) im Abstand `Sync:BfCsvWiederholAbstandMinuten` (15); Datei existiert schon = Fehler **ohne** automatische Wiederholung („CSV erneut schreiben“ nach Klärung in WiCAM); Status je Auftrag in der Spalte CSV der BF-Aufträge, Protokoll „Blechfertigung: WiCAM-CSV“ | `BfCsvService.cs:32-87` |

**Dateiformat (seit v1.99.42 exakt nach der echten WiCAM-Datei `BF00000002.csv`; vorher Platzhalter):**

| Merkmal | Wert | Quelle |
|---|---|---|
| Kodierung | **Windows-1252** ohne BOM, CRLF; ein nicht darstellbares Zeichen wird gemeldet, nie still ersetzt | `IdealAkeWms/Services/BfCsv.cs:48-52,72-78` |
| Spalten | jede Zeile genau 13 Felder (12 `;`), leere Felder aufgefüllt; keine Anführungszeichen | `BfCsv.cs:113-121` |
| Zeilen 1–9 (Kopf) | Spalte A `PART_START`; danach Schlüssel in Spalte B, Wert in C: `CREATE_NEW_ORDER`, `ORDER_NUMBER` (BF-Nummer), `ORDER_REMARK_3`, `ORDER_REMARK_1`, `ORDER_REMARK_2`, `ORDER_PRIORITY`, `ORDER_RELEASE`; Spalte A `PART_END`. Werte aus den Einstellungen `BfCsv*` ([04 § 5.4](04-konfiguration.md)) | `BfCsv.cs:115-130` |
| Zeile 10 / 12 | leer | `BfCsv.cs:115-117` |
| Zeile 11 | `ORDER_NC_FILE`, `ORDER_MATERIAL_NO`, `ORDER_AMOUNT`, `ORDER_REMARK`, `ORDER_POSITION`, `ORDER_REMARK_2`, `ORDER_REMARK_4`, `ORDER_DATE`, `ORDER_REMARK_5`, `ORDER_REMARK_1` | `BfCsv.cs:132` |
| Zeile 13 | deutsche Überschrift: Matchcode, Mat.Nr., Menge, Bemerkung, Lagerplatz, Hauptlagerplatz, Besteller, Schachteltermin, FE-Termin, Arbeitsschritte | `BfCsv.cs:115-117` |
| ab Zeile 14 (je übergebener Position, aufsteigend) | Matchcode (TRIM/CLEAN/UPPER) · Materialnummer (nur ganzzahlig, sonst ist die Freigabe gesperrt) · Menge (ganzzahlig bzw. Dezimalkomma) · Bemerkung `[Status - ][FertigungsInfoPN - ]dd.MM[ - Position bei BG]` · Lagerplatz (voll, `;` → `,`) · Hauptlagerplatz (Teil vor dem ersten Trenner) · Besteller-**Kürzel** (Benutzerverwaltung, SQL/162) · Schachteltermin `dd.MM.yyyy` · FE-Termin `yyyy-MM-dd` · Arbeitsschritte (SQL/163). Keine Füllzeilen | `BfCsv.cs:59-64,88-91,136-140` |
| Trenner | `;`; `;`, `"` und Zeilenumbruch in einem Wert sperren die Freigabe | `BfCsv.cs:72`, `IdealAkeWms/Services/BfAuftragWorkflow.cs:239-257` |
| Nicht übergeben | gestrichene Positionen und Positionen ohne Materialnummer (Hinweis bei Freigabe) | `BfAuftragWorkflow.cs:203-236` |

---

## 9. Active Directory (LDAP)

| Punkt | Inhalt | Quelle |
|---|---|---|
| Zweck | **Nur** beim Anlegen von AD-Benutzern: Mitglieder der Gruppe `WindowsAuthBerechtigungsgruppe` auflisten, E-Mail übernehmen. Keine Rechtevergabe über AD | [[integrationen]] „Active Directory“, [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]] |
| Teil | Web (`ActiveDirectoryService`, Seite Benutzer → AD-Benutzer anlegen) | `IdealAkeWms/Services/ActiveDirectoryService.cs:22-37` |
| Endpunkt | Domäne aus `Security:AdDomain` (leer = Domäne des Servers), Abfrage über `System.DirectoryServices.AccountManagement` | `ActiveDirectoryService.cs:28,43-46` |
| Konto | App-Pool-Identität (bei `ApplicationPoolIdentity` das Computerkonto); braucht normales Leserecht im AD | **zu prüfen**, im Code nicht weiter belegt |
| Schalter | Gruppe leer → keine Abfrage, leere Liste | `ActiveDirectoryService.cs:24-26` |
| Bei Ausfall | Fehler wird abgefangen: Warnung im Log, Picker zeigt eine leere Liste; Anmeldung bestehender Benutzer ist nicht betroffen | `ActiveDirectoryService.cs:33-37` |

---

## 10. Windows-Anmeldung (IIS, Negotiate)

| Punkt | Inhalt | Quelle |
|---|---|---|
| Zweck | Domänen-Benutzer am Windows-Arbeitsplatz ohne Formular anmelden; die Windows-Identität wird auf `WindowsUserName` eines WMS-Benutzers abgebildet und in eine App-Sitzung überführt | [[0002-dual-auth-session-login-plus-windows-sso]] „Entscheidung“ |
| Teil | Web unter IIS in-process; Windows- **und** anonyme Authentifizierung aktiv | `IdealAkeWms/web.config:5-10`, [[fallstricke]] §4 „AddNegotiate() ist unter IIS in-process falsch“ |
| Ablauf | Nur Windows-Desktop-Browser bekommen die stille Anfrage; Handys/Tablets landen direkt am Formular. Knopf „Mit Windows anmelden“ erzwingt sie | [[0002-dual-auth-session-login-plus-windows-sso]] „Entscheidung“ |
| Schalter | `WindowsAuthAktiv` (Standard aus) | `AppSettingDefinitions.cs:201` |
| Kerberos/NTLM | IIS verhandelt Kerberos oder NTLM. Mitglieder der AD-Gruppe **Protected Users** dürfen kein NTLM – dann braucht es Kerberos und damit passende SPNs. Am IDEAL-Test fiel das für die SQL-Anmeldung des Update-Kontos auf (kein SPN für die benannte Instanz) | `secondbrain/aufgaben/2026-11-11-buendel-deploy-update-skript.md:31-45` |
| SPN Web | Für die Web-Seite (`HTTP/<Hostname>`) ist im Brain nichts festgehalten. **Zu prüfen**, wenn Benutzer aus Protected Users per SSO angemeldet werden sollen: `setspn -Q HTTP/<Hostname>` und App-Pool-Konto | – |
| Bei Ausfall | Jeder Fehler und jeder Nicht-Treffer fällt zum Anmeldeformular durch; kein Zugriff ohne gültige Anmeldung | [[0002-dual-auth-session-login-plus-windows-sso]] „Entscheidung“ und „Konsequenzen“ |
| Test | Echter Handshake nur am Zielsystem prüfbar (`docs/TESTSZENARIEN.md` Kap. 40) | [[fallstricke]] §4 „Windows-Auth und AD-LDAP sind nicht InMemory-testbar“ |

---

## 11. Feiertage (date.nager.at)

| Punkt | Inhalt | Quelle |
|---|---|---|
| Zweck | Feiertage für Arbeitstags-Rechnung und BDE-Schichtkalender | [[integrationen]] „Feiertage“ |
| Teil / Endpunkt | Dienst: `https://date.nager.at/` (Land `Sync:FeiertagCountryCode`, Region `Sync:FeiertagRegion`); Web: Import-Knopf in den Einstellungen, fest Land `AT` | `IDEALAKEWMSService/Program.cs:111-115`, `IdealAkeWms/Services/HolidayImportService.cs:20` |
| Netzwerk | Ausgehendes HTTPS ins Internet vom Server (ggf. Proxy – **zu prüfen**) | – |
| Schalter | `Sync:FeiertagSyncEnabled` (aus) | `ServiceSettingDefinitions.cs:292` |
| Bei Ausfall | Lauf mit Fehler; vorhandene Feiertage bleiben | [[fallstricke]] §5 |

---

Weiter: Abfragen zum Prüfen der Warteschlangen und Läufe [08-diagnose-abfragen.md](08-diagnose-abfragen.md) · Installation/Update
[03-installation-update-rollback.md](03-installation-update-rollback.md) · Architektur [01-architektur-technologie.md](01-architektur-technologie.md) ·
Sicherung [10-backup-restore.md](10-backup-restore.md).
