---
type: spec
title: Lager und Lagerplätze im WMS erfassen und per Direkt-SQL in Sage anlegen (über die Outbox, austauschbar gegen SData)
slug: 2026-11-24-lagerplatz-anlage-wms-nach-sage-spec
status: Testbereit
created: 2026-11-24
updated: 2026-11-25
source_backlog: "[[2026-11-20-lagerplatz-anlage-wms-nach-sage]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - "Outbox-Helfer: IdealAkeWms/Data/SageStammdatenOutbox.cs:14 (TypLagerplatz), :18-27 (Vormerken, speichert nicht), :30-45 (ErledigenAsync -> Bestaetigt)"
  - "Outbox-Entität: IdealAkeWms/Models/SageStammdatenOutboxItem.cs:12-37 (EntityType, EntityKey = WMS-Id, PayloadJson nur Nachvollziehbarkeit :24-26, Status SageBookingQueueStatus :29, AttemptCount :32, LastError :36); Tabelle ApplicationDbContext.cs:1267-1282; SQL/107"
  - "Status-Enums: IdealAkeWms/Models/SageBookingQueueStatus.cs:11-17 (Offen 0, Gesendet 1, Bestaetigt 2, Fehler 3); IdealAkeWms/Models/Creditor.cs:150-156 (SageUebergabeStatus NichtNoetig/Offen/Uebergeben/Fehler), :158-172 (SageUebergabeStatusText, IstOffen = Offen|Fehler)"
  - "Schreiber der Outbox heute: IdealAkeWms/Data/Repositories/StorageLocationRepository.cs:17-27 (AddAsync merkt bei Status Offen vor, ZWEI SaveChanges je Platz: Repository.cs:33-37 + :24; fuer die Sage-Anlage NICHT verwenden, S2); IdealAkeWms/Controllers/StorageLocationsController.cs:103-120 (Create setzt Offen :111)"
  - "Transaktions-Muster fuer die neue Repository-Methode (S2): IdealAkeWms/Data/Repositories/BfAuftragRepository.cs:105-112 (BeginTransactionAsync, IsRelational-Guard fuer InMemory :107)"
  - "Abgleich-Diff, den die Uebernahme treffen muss (S6): LagerplatzSyncService.cs:195-208 (Sperr-/Massfelder, Description getrimmt :126), :301 (Abweichend = Quelle liefert Wert, WMS anders); Rundung SdataLagerplatzReader.cs:68, :77 (3 Stellen); Code-Vergleich ohne Gross/Klein :100-101"
  - "Protokoll/Mail-Muster (S7): SageFaArbeitsgangService.cs:82 (kein Lauf ohne Arbeit); SageBookingWorker.cs:272-281, :292 (Fehlertext nur bei Wechsel melden, Merker im Worker); SyncErrorNotifier.cs:28-60 (keine eigene Drosselung)"
  - "Lagerort-Liste ohne Aktiv-/Outbox-Filter (S10, nur Hinweis): IdealAkeWms/Data/Repositories/PickingAreaRepository.cs:36-37"
  - "Spaltenfilter-Typen der Liste (S8): StorageLocationsController.cs:76 (ColumnFilterTypes.For(\"StorageLocations/Index\"))"
  - "Manifest (S9): scripts/deploy/nachlauf.psd1:12-13 (Pruefung = EINE Zahl), :51 (SQL/98), :209-216 (Muster Pruefung/sage-fa-ag-sdata-zugang, Pruefung/ArtikelQuelleSData)"
  - "QA-Grenzen (Freigabe-Antwort 8): .claude/agents/qa-agent.md:84-87 (nennt SageLagerplatzAnlageAktiv nicht; 'never touch AKESQL20 ... or Sage' - fuer diese Spec durch Antwort 8 eingeschraenkt, siehe Test-Szenarien)"
  - "Leser/Erlediger der Outbox heute: IDEALAKEWMSService/Services/LagerplatzSyncService.cs:174-189 (manueller Platz mit Übergabe offen + gleicher Code -> Source Sage, Uebergeben, ErledigenAsync); sonst kein Leser, kein Worker"
  - "Liste „Offene Übergaben an Sage“: StorageLocationsController.cs:55-92 (Filter onlyUebergabeOffen :70-71, Zähler :81, ColumnMap :30-45); Views/StorageLocations/Index.cshtml:32-51 (Hinweistext „Das WMS sendet nichts an Sage“ :38-39), :103/:144 (Spalte Übergabe an Sage)"
  - "Lagerplatz-Modell: IdealAkeWms/Models/StorageLocation.cs:12-15 (Code 50), :36-38 (Source), :61-71 (SageLagerkennung, SageLagerplatzId), :78-80 (StorageAreaId), :109-117 (Übergabe-Felder), :121-130 (Validate: manuelle Codes max. 12 Zeichen); Unique-Index Code ApplicationDbContext.cs:236"
  - "Lagerort-Modell: IdealAkeWms/Models/StorageArea.cs:11-37 (Lagerkennung 10, Bezeichnung 40, Lagertechnik 10, IstDispoLager, IsActive, SageMissingSince; kein Übergabe-Status; bisher nur vom Lagerplatz-Abgleich geschrieben :7); Unique Lagerkennung ApplicationDbContext.cs:267"
  - "StorageArea-Schreiber (einzige, Suche *.cs 2026-11-24): IDEALAKEWMSService/Services/LagerplatzSyncService.cs:313-379 (SyncStorageAreasAsync: nur wenn Quelle Lagerorte liefert :319-320; Schlüssel Lagerkennung ohne Groß/Klein :325; Neuanlage :338-348; Update :349-361; MUSS GEÄNDERT WERDEN: Fehlend-Schleife :364-375 setzt jedes nicht gelieferte Lager auf SageMissingSince/IsActive=false, auch ein im WMS neu erfasstes, noch nicht übergebenes); Platz-Zuordnung :152, :217; Web liest nur (PickingAreaRepository.cs:36-37, StorageLocationRepository.cs:31-32, StorageLocationsController.cs:48-49)"
  - "Outbox ohne Typ-Einschränkung: SQL/107_AddSageStammdatenOutboxAndUebergabeStatus.sql:82 (EntityType NVARCHAR(50) NOT NULL, kein CHECK), ApplicationDbContext.cs:1271 -> neuer Typ „StorageArea“ ohne Migration"
  - "Auswahl-Einstellung im Dienst-Katalog: IdealAkeWms/Models/ServiceSettingDefinitions.cs:347 (SettingType.Choice mit Werte), :35 (Meta)"
  - "Lese-Abgleich: IDEALAKEWMSService/Services/SageLagerplatzReader.cs:18-29 (SageConnection, KHKLagerorte LEFT JOIN KHKLagerplaetze, Mandant = 1, Aktiv = -1); LagerplatzSyncService.cs:80-98 (Dubletten Kurzbezeichnung -> übersprungen), :124-126 (Code = Kurzbezeichnung, Zone = Lagerkennung), :136-167 (Neuanlage, IstBuchbar false), :191-231 (Sage-Zweig überschreibt Description/Zone), :232-238 (manueller Platz = Konflikt), :243-261 (nicht im Feed -> deaktiviert)"
  - "Sage-Verbindung (auch Schreibweg, Freigabe-Antwort 2): IDEALAKEWMSService/appsettings.json:4 (ConnectionStrings:SageConnection, Trusted_Connection = Dienstkonto); IDEALAKEWMSService/Common/ConnectionStrings.cs:17; Web hat KEINE Sage-SQL-Verbindung (IdealAkeWms/appsettings.json:2-5)"
  - "Kein bestehender Direkt-Schreibweg nach Sage: Suche nach INSERT/UPDATE KHK*, spSysTan, KHKTan in *.cs/*.sql ohne Treffer (Bündel-Worktree, 2026-11-24)"
  - "Worker-Muster: IDEALAKEWMSService/Workers/SageBookingWorker.cs:14-15 (genau eine Instanz), :136-150 (Schritt mit eigenem try), :265-292 (ProcessSageFaArbeitsgaengeAsync: Schalter nicht Safe lesen :268, Fehler nur einmal melden :272-278); Program.cs:161-177 (DI der Schritte)"
  - "Schalter-Muster: IdealAkeWms/Services/SageFaArbeitsgaenge.cs:16-21 (ToggleKey, ServiceActor); ServiceSettingDefinitions.cs:75-78 (Meta mit Standort/Warnhinweis); Web liest Schalter über IServiceSettingRepository (SageFaArbeitsgaengeController.cs:23, :121, :161)"
  - "Aktivitäts-Protokoll: IdealAkeWms/Services/SyncLogger/SyncLogServices.cs:44, :66, :108 (Muster SageFaArbeitsgaenge)"
  - "Rollen: secondbrain/codebase/controller.md:53-54 (RequireMasterDataAccess = admin, masterdata; Read = + masterdata_read)"
open_questions:
  - "1. BEANTWORTET 2026-11-24: Lager mit anlegen, wenn in Sage nicht vorhanden und am Standort möglich (Einstellung SageLagerAnlageLagertechniken), sonst nur Platz."
  - "2. BEANTWORTET 2026-11-24: bestehende SageConnection (Dienstkonto) bekommt Minimalrechte per GRANT, kein eigener Connection String."
  - "3. BEANTWORTET 2026-11-24: Rolle masterdata."
  - "4. BEANTWORTET 2026-11-24: beide Standorte, Schalter je Standort, Default aus; AKE geprüft, IDEAL-Prüfung ist Voraussetzung vor dem Einschalten an IDEAL."
  - "5. BEANTWORTET 2026-11-24: Testsystem idealakeweb01.ideal.ideal-ake.at:88 auf IDEAL_TEST_2026_05_03, Testlager legt Benjamin an."
  - "6. BEANTWORTET 2026-11-24: DPS wird nicht informiert."
  - "7. BEANTWORTET 2026-11-24: AKE-Abnahme auf DEV AKE (DEV-Dienst mit appsettings.DEV.json -> ake_TEST2026 auf AKESQL20), Sage-Testlager legt Gerald an."
  - "8. BEANTWORTET 2026-11-24 (Pruefbefund B2): QA darf die Pruef-/Abnahmeabfragen per SQL selbst ausfuehren; Auslegung siehe Test-Szenarien (Schalter auf DEV nur nach SQL-Beleg Test-DB, danach wieder aus, Produktiv nie)."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: "Antworten 1-8 + SWF; Sage-Test-SQL freigegeben (Gerald 2026-11-25); Umsetzung im nächsten goal-run"
freigabe_von: Gerald
freigabe_am: 2026-11-25
---

## Ziel / Nutzen (das Warum)

Neue Lagerplätze entstehen heute zweimal: im WMS (Stammdaten → Lagerplätze → „Neuen Lagerplatz anlegen“) und ein zweites Mal von Hand in
Sage. Seit v1.73.0 merkt das WMS einen neu angelegten Platz zwar als „Offene Übergabe an Sage“ vor (Outbox `SageStammdatenOutbox`), **sendet
aber nichts** (`SageStammdatenOutboxItem.cs:7`, Hinweistext `Index.cshtml:38-39`). Grund war G5 der SData-Spec: Es gibt keinen SData-Service für
die Lagerplatz-Anlage.

Ziel: Ein im WMS erfasster Lagerplatz wird vom Dienst **automatisch** in Sage angelegt, mit genau dem Ergebnis, das der Sage-Dialog
„Lagerplätze anlegen“ erzeugt (Anhang [[2026-11-20-WMS_Lagerplatzanlage_Sage_v2]]). Danach ist der Platz ein normaler Sage-Platz im WMS (Quelle
„Sage“, Sage-Platz-Id gesetzt, buchbar nach Sage), ohne zweite Erfassung und ohne Dublette.

**Entscheid Gerald 2026-11-24 (nicht erneut fragen):**
1. Jetzt umsetzen, mit **Direkt-SQL in Sage** nach dem Anhang (Nummernvergabe über `spSysTan` wie im Mitschnitt).
2. Liefert DPS (Wiesner, Block 5 in [[2026-11-20-dps-consulting-tag-fragen]]) später einen SData-Service, wird umgebaut. Deshalb liegt der
   Schreibweg hinter **einer** austauschbaren Stelle (Interface mit einer Implementierung, hier ausdrücklich gerechtfertigt, weil der Austausch
   angekündigt ist). Die Übergabe läuft über die bestehende Outbox.
3. Eigene **neue ADR** für den Direkt-SQL-Schreibweg (Rechte SQL-Login, Updatefestigkeit bei Sage-Versionswechsel, Support DPS, Transaktion,
   Idempotenz, Rückbau auf SData) als Teil der Umsetzung.

**Freigabe-Antworten Gerald 2026-11-24 (eingearbeitet, siehe Abschnitt am Ende):** Existiert das Lager in Sage noch nicht, legt der Dienst
auch das **Lager** an, sofern das am Standort freigegeben ist (sonst nur Plätze in vorhandenen Lagern). Geschrieben wird über die
bestehende `SageConnection` des Dienstes mit Minimalrechten. Rolle `masterdata`. **Beide Standorte**, Schalter je Standort, Default aus.
DPS wird nicht informiert. AKE-Abnahme auf **DEV AKE** gegen `ake_TEST2026`, Testlager legt Gerald an (Antwort 7). Der `qa-agent` darf die
Prüf- und Abnahmeabfragen selbst per SQL ausführen und den Schalter auf DEV nach SQL-Beleg der Test-DB einschalten (Antwort 8, Auslegung im
Abschnitt Test-Szenarien). Die Befunde der „Kritischen Prüfung (2026-11-24)“ sind eingearbeitet (Liste am Ende jenes Abschnitts).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neue Erfassung „Lagerplätze in Sage anlegen“ in Stammdaten → Lagerplätze: Lagerort wählen (aus `StorageAreas`) **oder ein neues Lager
  erfassen** (F1a), Bereich je Dimension von–bis, Buchbar/Sage-Buchung erlaubt. Das WMS legt Lager und Plätze sofort an (Status „Offen“,
  Outbox-Eintrag je Lager und je Platz), alles in **einer** DB-Transaktion (S2).
- Neuer Schritt im `SageBookingWorker`: zuerst neue Lager (eine Transaktion je Lager, Anhang 4/8.1), dann Plätze (eine Transaktion je Platz)
  per Direkt-SQL in Sage anlegen; danach den WMS-Platz übernehmen (Quelle Sage, `SageLagerplatzId`, Status „Übergeben“, Outbox „Bestätigt“).
- Austauschbare Schreibstelle `ISageLagerplatzAnlage` mit genau einer Implementierung (Direkt-SQL), zwei Methoden (Lager, Platz).
- Lagerplatz-Abgleich: ein im WMS erfasstes, noch nicht übergebenes Lager wird nicht als „nicht in Sage“ markiert (F4).
- Fehler sichtbar in der Lagerplatzliste (Filter „Offene Übergaben an Sage“, neue Spalte „Übergabe-Fehler“, Knopf „Erneut senden“, Hinweis
  für Lager mit offener/fehlerhafter Übergabe) und im Aktivitäts-Protokoll.
- Schalter `SageLagerplatzAnlageAktiv` und Auswahl `SageLagerAnlageLagertechniken` (ServiceSettings, Default aus, je Standort-DB, beide Standorte).
- Neue ADR 0036 (Nummer prüft der Dev-Lauf), Hilfe, README, Testszenario mit dem Abnahmetest aus Anhang Abschnitt 9 (Lager und Plätze).

**Out-of-Scope (Scope-Grenzen aus Anhang Abschnitt 9 und 7)**
- ~~**Lager (Lagerort) anlegen** … nicht in diesem Paket, solange Rückfrage 1 nicht anders entscheidet.~~ **Überholt** durch Freigabe-Antwort 1:
  Lager-Anlage ist In-Scope (F1a, F3a). Dafür gelten die Warenfluss-Regeln aus Anhang 4.5/2.8 fest: `WarenflussKZ` = `SWF`, die Kennzeichen
  `IstWELager`/`IstWALager` werden nur daraus abgeleitet (bei `SWF` alle 0), `IstHauptWELager`/`IstHauptWALager` (`HWE`/`HWA`) setzt das WMS nie.
  Andere Warenfluss-Arten (`WE`, `WA`) bietet das WMS nicht an.
- **Lager umbenennen** (Anhang 6, 8.3): nicht angeboten. Die Variante „Nein“ ist nicht mitgeschnitten (Anhang 6, Abschnitt 9 Punkt 5). Das WMS
  ändert `Bezeichnung` eines bestehenden Sage-Lagers nie; ein im WMS erfasstes Lager ist bis zur Übergabe nicht bearbeitbar.
- **Lagertechnik eines bestehenden Lagers ändern**: nicht angeboten (Anhang Abschnitt 9 Punkt 8, nicht untersucht). Der Platz-Befehl schreibt
  an `KHKLagerorte` nur die Zähler (`AnzahlDimension1–3`, `AnzahlLagerplaetze`, `IstPlatzverwaltet`), nie `Lagertechnik`.
- **Verwerfen eines nie übergebenen Lagers** im WMS: nicht in diesem Paket (Fehlerfall selten; Plätze lassen sich deaktivieren, F7).
  `ponytail:` Folgearbeit, falls fehlgeschlagene Lager in der Praxis stehen bleiben.
- **Löschen und Deaktivieren in Sage**: das WMS löscht nichts in Sage (Anhang 7). Ein im WMS deaktivierter Platz wird nicht gesendet bzw. bleibt
  in Sage unverändert.
- **Platzbezeichnung ändern** in Sage: nein. Nach der Übernahme gilt die Sage-Platzbezeichnung (Sage führend, wie E9).
- **BDE-Mitarbeiter** (`TypBdeMitarbeiter`, `SageStammdatenOutbox.cs:15`): nicht in diesem Paket.
- **Alt-Einträge**: Plätze, die seit v1.73.0 über „Neuen Lagerplatz anlegen“ mit Status „Offen“ entstanden sind (frei gewählter Code, kein
  Lagerort), sendet der Dienst **nicht**. Für sie gilt weiter „in Sage von Hand mit demselben Code anlegen“ (E9-Übernahme bleibt).

## Fachliche Anforderungen

**F1 Erfassung.** In Stammdaten → Lagerplätze gibt es neben „Neuen Lagerplatz anlegen“ den Knopf „Lagerplätze in Sage anlegen“ (nur bei
eingeschaltetem Schalter und Schreibrecht `RequireMasterDataAccess` = `admin`, `masterdata`, Freigabe-Antwort 3). Das Formular fragt:
- Lagerort: Auswahl aus `StorageAreas` mit `IsActive = true` und `SageMissingSince IS NULL` (Anzeige Lagerkennung + Bezeichnung + Lagertechnik;
  ein im WMS erfasstes, noch nicht übergebenes Lager mit Zusatz „(neu, wartet auf Sage)“), dazu der Eintrag „Neues Lager erfassen …“ (F1a),
  nur wenn `SageLagerAnlageLagertechniken` ≠ `Aus`. Ein Lager, dessen Lager-Outbox-Eintrag auf `Fehler` steht, erscheint als
  „<Kennung> (Fehler – erst ‚Erneut senden‘)“ und ist **nicht wählbar**; der Server lehnt einen POST mit diesem Lager ab (S10: sonst entstünden
  Plätze, die nie gesendet werden).
- Dimension 1 von–bis (ganze Zahl 1–9999), Dimension 2 von–bis, Dimension 3 von–bis (0–9999; „0–0“ = Dimension nicht genutzt; Obergrenze S3:
  hält Überlauf-/Kürzungsfehler aus Sage fern, `<Kennung 10>;` + 3 × 4 Ziffern passt in `Kurzbezeichnung` 50). Hinweis am Formular:
  „Nicht genutzte Dimensionen auf 0 lassen. Ob die Koordinaten zur Lagertechnik passen, prüft Sage beim Anlegen.“
- Buchbar, Sage-Buchung erlaubt (Vorbelegung wie bei „Neuen Lagerplatz anlegen“).

Die Lagerort-Liste ist bewusst die WMS-Tabelle `StorageAreas`: Das Web hat keine Sage-SQL-Verbindung (`IdealAkeWms/appsettings.json:2-5`).
`StorageAreas` füllt nur der Lagerplatz-Abgleich mit Quelle SData (`StorageArea.cs:7`, `LagerplatzSyncService.cs:319-320`). Ist die Liste leer,
zeigt das Formular statt der Auswahl: „Keine Sage-Lagerorte im WMS. Der Lagerplatz-Abgleich muss mit Quelle SData laufen
(Service-Einstellung Sync:Quelle:Lagerplaetze), damit Lagerorte zur Auswahl stehen.“ (Prüfschritt P-8.) Dann ist auch „Neues Lager
erfassen“ nicht angeboten: Die Eindeutigkeitsprüfung in F1a stützt sich auf `StorageAreas`; ohne den Abgleich (Quelle SData,
`LagerplatzSyncService.cs:319-320`) kennt das WMS die vorhandenen Sage-Lager nicht.

**F1a Neues Lager erfassen (Freigabe-Antwort 1, „wenn möglich“).** Wählt der Anwender „Neues Lager erfassen …“, fragt das Formular zusätzlich:
- Lagerkennung: Pflicht, getrimmt, 1–10 Zeichen (`varchar(10)`, Anhang 4.2; `StorageArea.cs:14`), nur Buchstaben A–Z/a–z, Ziffern, `-` und `_`
  (H10: Leer- und Sonderzeichen landen sonst in Code und Barcode, `BarcodeValue = Code`; schließt `;`, das Trennzeichen der Kurzbezeichnung, ein),
  noch nicht in `StorageAreas` (Vergleich ohne Groß/Klein wie der Abgleich, `LagerplatzSyncService.cs:325`; Unique-Index
  `ApplicationDbContext.cs:267`), und der Basisplatz-Code `<Lagerkennung>;0;0;0` noch nicht in `StorageLocations` (AKE-Fall `HAUPT` mit
  Kurzbezeichnung `Hauptlager;0;0;0`, Prüfergebnis P-6: eine neue Kennung `Hauptlager` ergäbe in Sage eine zweite Kurzbezeichnung
  `Hauptlager;0;0;0`, die der Abgleich als Dublette überspringt, `LagerplatzSyncService.cs:80-98`).
- Bezeichnung: Pflicht, 1–40 Zeichen (`varchar(40)`, Anhang 4.2/5.3).
- Lagertechnik: Auswahlliste aus den am Standort freigegebenen Techniken (Einstellung `SageLagerAnlageLagertechniken`, Abschnitt
  Einstellungen); Server validiert gegen dieselbe Liste. Angezeigt wird die Kennung mit der Bezeichnung aus Anhang 4.4 (das Web hat keine
  Sage-Verbindung; der Dienst prüft beim Senden gegen `KHKLagertechnikenArten`, Fehler 50001).
- Platzverwaltung: Schalter „Lagerplätze anlegen (platzverwaltet)“. Aus = nur Lager mit Basisplatz (Sage: `IstPlatzverwaltet` bleibt 0,
  Anhang 4.1); an = zusätzlich der Platzbereich wie bei einem vorhandenen Lager, `IstPlatzverwaltet = -1` entsteht wie in Sage erst durch das
  Anlegen der Plätze (Anhang 5.4). Bei `LTD0ST` ist der Schalter gesperrt mit Hinweis „Standardtechnik hat keine Dimensionen – keine Plätze“
  (Anhang 4.4).
- WarenflussKZ wird nicht abgefragt: fest `SWF` (Out-of-Scope oben).

Ist `SageLagerAnlageLagertechniken = Aus`, zeigt das Formular statt des Eintrags den Hinweis „Lager-Anlage in Sage ist an diesem Standort nicht
freigegeben – Lagerplätze nur in vorhandenen Lagern.“ Das WMS legt beim Absenden in **einer** DB-Transaktion an (S2, Repository-Methode
siehe F2; ~~„in einem `SaveChanges`“~~ **überholt**: der Outbox-Schlüssel ist die WMS-Id, die erst nach dem Insert feststeht): das `StorageArea`
(`Lagerkennung`, `Bezeichnung`, `Lagertechnik`, `IstDispoLager = true` wie Sage `-1`, Anhang 4.2; `IsActive = true`, `SageMissingSince = null`,
Audit des Anwenders), einen Outbox-Eintrag `EntityType = 'StorageArea'` (neue Konstante `SageStammdatenOutbox.TypLagerort`, `EntityKey` =
StorageArea-Id, Payload Lagerkennung/Bezeichnung/Lagertechnik nur zur Nachvollziehbarkeit) und – bei Platzverwaltung – die Plätze nach F2 mit
`StorageAreaId` = neues Lager. Rückmeldung: „Lager <X> erfasst, wird vom Dienst in Sage angelegt; n Lagerplätze angelegt …“.

**F2 Code und Platzanlage im WMS.** Je Koordinate des kartesischen Bereichs (Reihenfolge D1 außen, D3 innen, wie Sage, Anhang 5.1) entsteht ein
WMS-Platz:
- `Code` = `BarcodeValue` = `<Lagerkennung>;<D1>;<D2>;<D3>`, immer drei Zahlen, ungenutzte als 0 (= Sage-Kurzbezeichnung, Anhang 5.2). Das ist der
  Schlüssel, über den der Lese-Abgleich Plätze zuordnet (`LagerplatzSyncService.cs:124`, `:136`).
- `Zone` = Lagerkennung (so schreibt sie der Abgleich, `LagerplatzSyncService.cs:125`), `StorageAreaId` = gewählter Lagerort, `Description` leer
  bis zur Übergabe.
- `Source = Manual`, `SageUebergabeStatus = Offen`, Outbox-Eintrag je Platz (Payload Id, Code, Lagerkennung, D1–D3; nur Nachvollziehbarkeit,
  `SageStammdatenOutboxItem.cs:24`). ~~über das bestehende `StorageLocationRepository.AddAsync`~~ **überholt (S2):** `AddAsync` speichert je
  Platz zweimal (`Repository.cs:33-37` + `StorageLocationRepository.cs:24`) und ist nicht atomar; ein Abbruch ließe Plätze ohne Outbox-Eintrag
  (nie gesendet) bzw. ein Lager ohne Outbox-Eintrag zurück, das die Fehlend-Schleife des Abgleichs deaktiviert.
- **Speichern (S2):** neue Methode `IStorageLocationRepository.AddSageAnlageAsync(StorageArea? neuesLager, IReadOnlyList<StorageLocation> plaetze)`
  – eine DB-Transaktion nach Muster `BfAuftragRepository.cs:105-112` (`BeginTransactionAsync`, Isolationsstufe Standard ReadCommitted genügt;
  bei nicht-relationalem Provider ohne Transaktion wie `:107`): Lager + Plätze hinzufügen → `SaveChanges` (Ids) → `SageStammdatenOutbox.Vormerken`
  für Lager (Typ `StorageArea`) und jeden Platz → `SaveChanges` → `Commit`. Scheitert ein Schritt (z. B. Unique-Index `Code`/`Lagerkennung` bei
  zwei gleichzeitigen Anwendern), ist nichts angelegt; das Formular zeigt „Inzwischen von jemand anderem angelegt – bitte erneut prüfen“
  (`ModelState`-Fehler, kein `ErrorMessage`). Das bestehende `AddAsync` (Create, E9) bleibt unverändert. Der Abgleich sieht ein noch nicht
  committetes Lager nicht und markiert es deshalb auch nicht als fehlend.
- Koordinaten, deren Code im WMS schon existiert (Vergleich **ohne Groß/Klein** wie der Abgleich, `LagerplatzSyncService.cs:100-101`, und wie der
  Unique-Index `ApplicationDbContext.cs:236` unter der SQL-Kollation, H3), werden übersprungen (wie Sage vorhandene Kombinationen überspringt, Anhang 5.1). Rückmeldung:
  „n Lagerplätze angelegt, m übersprungen (Code im WMS schon vorhanden: …)“ als `SuccessMessage` bzw. `WarningMessage`.
- Höchstens 200 Plätze je Absenden; mehr = Formularfehler mit der berechneten Anzahl (kurze Transaktionen, Anhang 2.6).
- **Kennzeichen „Sage-Anlage“** = `Source == Manual` und `StorageAreaId != null`. Das ist eindeutig, weil `StorageAreaId` heute nur der Abgleich
  setzt, und nur an Sage-Plätzen (`LagerplatzSyncService.cs:152`, `:217`, einzige Schreibstellen, Suche 2026-11-24). Das bestehende
  `Create` muss deshalb `StorageAreaId` aus dem POST ignorieren (Überposting, AK 3). Keine neue Spalte.

**F3 Senden (Dienst).** Bei eingeschaltetem Schalter verarbeitet der Dienst im Takt des `SageBookingWorker` **zuerst** die Lager (F3a),
**danach** alle Outbox-Einträge `EntityType = 'StorageLocation'` mit Status `Offen` (sowie hängende `Gesendet`, F5), deren Platz Sage-Anlage ist
(F2) und `IsActive = true`, in Reihenfolge der Outbox-Id. Ein Platz, dessen Lager einen Outbox-Eintrag `StorageArea` mit Status ≠ `Bestaetigt`
hat, wird in diesem Takt **nicht** gesendet und bleibt unverändert `Offen` (wartet auf das Lager; der Fehler steht am Lager, F6). Solche
wartenden Plätze zählen **nicht als Arbeit** (S7). **Kein Protokoll-Lauf je Takt ohne Arbeit** (Muster `SageFaArbeitsgangService.cs:82`):
Gibt es weder Lager- noch Platz-Kandidaten, kehrt der Schritt ohne Lauf-Eintrag zurück. Jeder Lauf mit Arbeit schreibt einmal die Info
„Sage-Datenbank: <@@SERVERNAME>/<DB_NAME()>“ (aus dem Ergebnis des ersten Sage-Befehls im Lauf; Beleg für Freigabe-Antwort 8). Je Platz:
1. Outbox-Status vor dem Sage-Aufruf auf `Gesendet`, `AttemptCount + 1`, Audit (Muster „Status vor dem Call“, `SageBookingWorker.cs:14-20`).
2. Aufruf `ISageLagerplatzAnlage.AnlegenAsync(Lagerkennung, D1, D2, D3)`. Lagerkennung aus `StorageArea.Lagerkennung`, Koordinaten aus dem Code;
   passt der Code nicht zum Muster `<Lagerkennung>;<int>;<int>;<int>` mit derselben Lagerkennung, wird nichts gesendet (fachlicher Fehler).
3. Ergebnis **angelegt** oder **in Sage vorhanden**: WMS-Platz übernehmen: `Source = Sage`, `SageLagerplatzId` = PlatzID, `SageLagerkennung` =
   Code, `Description` = Sage-Platzbezeichnung (getrimmt wie `LagerplatzSyncService.cs:126`), `SageUebergabeStatus = Uebergeben`,
   `SageUebergabeAm = jetzt`, `SageUebergabeFehler = null`, **dazu die Sage-Werte der Zeile** (S6): `SageIstGesperrt = (IstGesperrt <> 0)`,
   `SageIstGesperrtInventur = (IstGesperrtInventur <> 0)`, `SageLaenge/-Breite/-Hoehe/-Volumen/-Tragkraft` = Sage-Wert gerundet auf 3 Stellen
   wie `SdataLagerplatzReader.cs:77` – im Fall „angelegt“ wie „vorhanden“ aus dem Ergebnis-`SELECT` des Befehls. Grund: der Sage-Zweig des
   Abgleichs vergleicht diese Felder (`LagerplatzSyncService.cs:197-203`, `Abweichend` `:301`); ohne sie zählte der erste SData-Abgleich jeden
   übergebenen Platz als „aktualisiert“. Outbox `Bestaetigt` (über `SageStammdatenOutbox.ErledigenAsync` erweitert um `Gesendet`; wirkt auch für
   `BdeOperator`, harmlos, AK 38), Audit. Info im Protokoll: „Lagerplatz <Code> in Sage angelegt (PlatzID n)“ bzw. „… war in Sage schon
   vorhanden (PlatzID n) – übernommen“.
4. **Fachlicher Fehler** (Konflikt, Lager inaktiv, Koordinaten passen nicht, Text zu lang, jede nicht-technische SQL-Ausnahme …): Outbox `Fehler`
   + `LastError`, Platz `SageUebergabeStatus = Fehler` + `SageUebergabeFehler` = Klartext. Nichts in Sage geschrieben (Rollback). Warnung im
   Protokoll, **keine Mail** (sichtbar in der Liste, F6). Kein automatisches Wiederholen; der Takt läuft mit dem nächsten Eintrag weiter.
5. **Technischer Fehler** (nur die Klassen unten): Sage-Transaktion ist zurückgerollt; Outbox zurück auf `Offen`, `LastError` und
   `SageUebergabeFehler` = „Letzter Versuch fehlgeschlagen: …“, Platzstatus bleibt `Offen`. Rest des Takts wird übersprungen (alle weiteren
   Einträge scheiterten am selben Grund). **Melden nur beim Wechsel (S7)** nach Muster `SageBookingWorker.cs:272-281`/`:292`: Der Worker merkt
   sich den letzten technischen Fehlertext (`_lpTechnischGemeldet`) und übergibt ihn dem Service. Ist der Text gleich und im Takt sonst nichts
   geschehen, schreibt der Service **keinen** Lauf (Lauf wird erst beim ersten Ergebnis geöffnet) und der Worker schickt **keine** Mail; neuer oder
   geänderter Text → ein fehlgeschlagener Lauf mit dem Text und **eine** Mail über `NotifyErrorAsync` (`SyncErrorNotifier.cs:28-60` drosselt
   selbst nicht). Ein Takt ohne technischen Fehler setzt den Merker zurück.

**Fehlerklassen (S3, abschließend; Einordnung in `SqlSageLagerplatzAnlage`, als reine Funktion `IstTechnisch(SqlException)` unit-getestet).**
Technisch (Exception an den Aufrufer, Punkt 5): `SqlException.Number` = `-2` (Timeout), `1205` (Deadlock-Opfer), `1222` (Lock-Timeout),
`229`/`230`/`262` (Recht fehlt – Testschritt .l), `4060`/`18456` (Datenbank/Anmeldung), `233`/`10053`/`10054`/`10060`/`53`/`-1`
(Transport/Netz) oder `SqlException.Class >= 20`; außerdem jede Nicht-`SqlException` aus dem Verbindungsaufbau (`InvalidOperationException`,
`IOException`, `TimeoutException`). **Alles andere** – `THROW 50001–50019`, `515`, `207`, `208`, `2812`, `8152`/`2628`, `220`/`8115`, `245`,
`547`/`2627`/`2601` usw. – ist fachlich (Ergebnis `Abgelehnt` mit Meldungstext; bei `515`/`207`/`208`/`2812` mit Zusatz „Sage-Tabelle oder
-Prozedur weicht ab – Prüfabfragen P-1 bis P-3 der Spec, Anlage am Standort abschalten“). Damit blockiert ein Datenfehler an einem Eintrag nie die
Warteschlange (AK 34). ~~„Ausnahme: SQL-Fehler 515 und 207 zählen als fachlich“~~ **überholt** durch diese Liste.

**F3a Lager senden (Dienst).** Outbox-Einträge `EntityType = 'StorageArea'` mit Status `Offen` (bzw. hängend `Gesendet`), deren
`StorageArea.IsActive = true`, in Reihenfolge der Outbox-Id, gleicher Status-Automat wie F3 Punkt 1, 4, 5:
1. ~~Ist die Lagertechnik nicht (mehr) freigegeben → fachlicher Fehler, nichts gesendet.~~ **Überholt (H4):** die Freigabe gilt nur für die
   **Neuanlage**. Der Dienst übergibt `neuanlageErlaubt` = (Lagertechnik des Lagers ist in der aktuellen Stufe `SageLagerAnlageLagertechniken`);
   der Befehl wirft nur dann 50006 „Lagertechnik <T> ist für die Lager-Anlage an diesem Standort nicht freigegeben“, wenn das Lager in Sage
   **nicht** existiert. Existiert es (z. B. nach einem Konflikt, dessen Sage-Werte der Abgleich übernommen hat), gilt „vorhanden“ – sonst
   scheiterte „Erneut senden“ dauerhaft an einer nicht freigegebenen Sage-Technik.
2. Aufruf `ISageLagerplatzAnlage.LagerAnlegenAsync(Lagerkennung, Bezeichnung, Lagertechnik, neuanlageErlaubt)`.
3. Ergebnis **angelegt** oder **vorhanden** (gleiche Kennung, gleiche Bezeichnung, gleiche Lagertechnik, aktiv): Outbox `Bestaetigt`, Audit;
   `StorageArea` bleibt unverändert (Werte stimmen schon, der Abgleich gleicht später ab). Info im Protokoll „Lager <X> in Sage angelegt
   (Basisplatz PlatzID n)“ bzw. „… war in Sage schon vorhanden – übernommen“. Die wartenden Plätze laufen im selben Takt (Schritt danach).
4. **Fachlicher Fehler** (Kennung in Sage anders belegt oder inaktiv 50003, Basisplatz-Kurzbezeichnung schon vergeben 50005,
   Artikel-Zuordnungen an Plätzen dieser Kennung 50004 – über `PlatzID` gegen `KHKLagerplaetze`, P-9a –, Plätze dieser Kennung ohne Lager 50007,
   Technik/`SWF` in Sage unbekannt 50001/50002, Technik nicht freigegeben 50006, alle übrigen nicht-technischen SQL-Fehler nach F3): Outbox
   `Fehler` + `LastError`, nichts in Sage, Warnung im Protokoll. Die Plätze des Lagers bleiben `Offen` (warten, zählen nicht als Arbeit).
5. **Technischer Fehler:** wie F3 Punkt 5 (Outbox zurück auf `Offen`, Rest des Takts übersprungen, Meldung nur beim Wechsel).

**F4 Datenhoheit nach der Übergabe.** Ab der Übernahme ist der Platz ein Sage-Platz: Der Lese-Abgleich findet ihn über den Code im Sage-Zweig
(`LagerplatzSyncService.cs:191-231`) und legt **keine** Dublette an; `Zone`, `Description`, `SageLagerplatzId` und – seit S6 – die Sperr- und
Maßfelder (`:197-203`) stimmen bereits, also kein Diff (auch mit Quelle View, die diese Felder nicht liefert, `:301`).
`IstBuchbar`/`SageBuchungErlaubt` fasst der Sage-Zweig nie an (E9-Regel, SData-Spec F11). Die Bearbeitung folgt der Sage-Regel
(`StorageLocationsController.cs:147-155`). Folge wie bei E9, als Info ausgewiesen: ab dem nächsten Lauf gleicht der Lagerbestand-Abgleich den
Bestand dieses Platzes mit Sage ab. Kommt der Lese-Abgleich zwischen Sage-Commit und WMS-Speichern (F5), übernimmt er den Platz selbst über den
bestehenden E9-Zweig (`LagerplatzSyncService.cs:174-189`) – gleiches Ergebnis.

**Lager beim Rücklesen (keine Dublette, kein falsches „nicht in Sage“).** `StorageAreas` schreibt nur der Abgleich
(`LagerplatzSyncService.cs:313-379`; Web liest nur, Suche 2026-11-24). Für ein im WMS erfasstes Lager gilt:
- **Keine Dublette:** Der Abgleich sucht Lager über die Lagerkennung ohne Groß/Klein (`:325`) und aktualisiert den vorhandenen Datensatz
  (`:349-361`) statt neu anzulegen (`:338-348`); zusätzlich schützt der Unique-Index `IX_StorageAreas_Lagerkennung`
  (`ApplicationDbContext.cs:267`). Nach der Übergabe sind Bezeichnung/Lagertechnik/`IstDispoLager` gleich → kein Update.
- **Änderung nötig:** Die Fehlend-Schleife (`:364-375`) setzt heute **jedes** nicht gelieferte Lager auf `SageMissingSince = now`,
  `IsActive = false` – also auch ein WMS-Lager, dessen Übergabe noch offen ist (der Abgleich läuft unabhängig vom Sage-Buchungs-Worker). Sie
  überspringt künftig Lager mit einem Outbox-Eintrag `StorageArea` im Status ≠ `Bestaetigt`. Ohne diese Ausnahme verschwände das neue Lager
  bis zur Übergabe aus der Auswahl (F1) und F3a sendete es nicht (inaktiv).
- **Sage führend nach Übergabe:** Ab dem nächsten Abgleich überschreibt der Update-Zweig Bezeichnung/Lagertechnik aus Sage; das WMS ändert ein
  Sage-Lager nie (Out-of-Scope). Hat jemand das Lager in der Zwischenzeit in Sage mit anderen Werten angelegt, meldet F3a den Konflikt; nach dem
  nächsten Abgleich trägt das `StorageArea` die Sage-Werte, und „Erneut senden“ (F6) übernimmt es dann als „vorhanden“.

Umkehr der Datenhoheit nach [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]: Das WMS führt nur **bis zur Übergabe** (Anlage),
danach ist Sage führend. Das ist das Muster „Sage führend, Abweichung melden“: Jeder Konflikt (F5) wird gemeldet, nie still überschrieben.

**F5 Idempotenz und Doppelanlage.** Der Sage-Schreibbefehl ist selbst idempotent: In derselben Transaktion prüft er mit Sperre, ob es den Platz
schon gibt, und legt nur an, wenn nicht. Fälle:
- Platz mit gleichen Koordinaten **und** gleicher Kurzbezeichnung existiert genau einmal → „in Sage vorhanden“, PlatzID zurück, nichts geschrieben.
- Koordinaten existieren mit **anderer** Kurzbezeichnung, oder die Kurzbezeichnung existiert mit **anderen** Koordinaten (auch in einem
  **anderen** Lager des Mandanten – der WMS-Code ist mandantenweit eindeutig, `ApplicationDbContext.cs:236`), oder es gibt mehrere Treffer →
  fachlicher Fehler „Konflikt in Sage: …“ (Klartext mit PlatzID/Kurzbezeichnung), nichts geschrieben. Hintergrund: Sage hat auf
  `KHKLagerplaetze` keinen Unique-Index außer `(PlatzID, Mandant)` (Anhang 2.4); abweichende Kurzbezeichnungen gibt es real (AKE P-6:
  `HAUPT;0;0;0` heißt `Hauptlager;0;0;0`).
- **Lager:** gleiche Kennung mit gleicher Bezeichnung, Lagertechnik und `Aktiv = -1` → „vorhanden“ (Wiederholung nach Abbruch), sonst Konflikt
  (50003). Der Primärschlüssel `(Mandant, Lagerkennung)` verhindert eine Lager-Dublette ohnehin (Anhang 2.3); geprüft wird vorher, damit der
  Fehler fachlich und lesbar ist.
- Hängender `Gesendet`-Eintrag (Dienst-Abbruch nach dem Sage-Commit, vor dem WMS-Speichern): Er wird ab `Sync:SageLagerbuchungStuckMinutes`
  (vorhandener Key, `SageBookingWorker.cs:285`) einfach erneut verarbeitet; der idempotente Befehl liefert „vorhanden“ → Übernahme. Kein eigener
  Wiederherstellungs-Lookup. Bewusste Abweichung vom Hausmuster „hängend nie blind neu senden“ (`SageBookingWorker.cs:19`), weil der Befehl
  idempotent ist – in ADR 0036 begründet (H12).
- Bekannte Folge (H9, dokumentieren, nicht abfangen): Wird ein Platz deaktiviert, während sein Eintrag hängend `Gesendet` ist, verarbeitet der
  Dienst ihn nicht mehr (Filter `IsActive`). Der nächste Abgleich findet den Sage-Platz, übernimmt ihn per E9 (`LagerplatzSyncService.cs:174-189`)
  und **reaktiviert** ihn (`:225`). Hilfe und ADR nennen das.
- Restrisiko: Der Sage-Dialog sperrt nicht wie das WMS. Legt ein Anwender gleichzeitig denselben Platz in Sage an, kann eine Dublette entstehen.
  Sie wird sichtbar: der Lese-Abgleich meldet doppelte Kurzbezeichnungen als Warnung und überspringt sie (`LagerplatzSyncService.cs:80-98`).

**F6 Fehler sichtbar, nicht still.** Die Lagerplatzliste bekommt die Spalte „Übergabe-Fehler“ (`SageUebergabeFehler`, Listen-View-Pattern), im
Filter „Offene Übergaben an Sage“ je Zeile mit Status „Fehler“ den Knopf „Erneut senden“ (Outbox `Fehler → Offen`, `AttemptCount = 0`,
Platz `Fehler → Offen`, Fehlertext leer, Audit des Anwenders). Der Hinweistext `Index.cshtml:38-39` unterscheidet nach Schalter:
- Schalter aus: wie heute („Bitte in Sage mit demselben Code anlegen … Das WMS sendet nichts an Sage.“).
- Schalter an: „Über ‚Lagerplätze in Sage anlegen‘ erfasste Plätze legt der Dienst in Sage an (Status ‚Offen‘ = wartet, ‚Fehler‘ = siehe Spalte
  Übergabe-Fehler). Andere offene Plätze bitte in Sage mit demselben Code anlegen.“
Ein Platz mit Fehler bleibt im Zähler der offenen Übergaben (`IstOffen` = Offen|Fehler, `Creditor.cs:171`).

**Lager mit offener Übergabe** haben keinen eigenen Status am `StorageArea` (kein Feld, `StorageArea.cs:11-37`); der Status steht in der Outbox.
Die Lagerplatzliste zeigt deshalb über der Tabelle einen Hinweis (kein eigener Tabellen-View) je Lager-Outbox-Eintrag ≠ `Bestaetigt`:
„Lager <X> wartet auf Anlage in Sage“ (Offen/Gesendet) bzw. „Lager <X> konnte nicht in Sage angelegt werden: <LastError>“ mit Knopf „Erneut
senden“ (Status `Fehler`; Outbox `Fehler → Offen`, `AttemptCount = 0`, `LastError` leer, Audit des Anwenders; Rolle wie oben). Bei
eingeschaltetem Schalter ergänzt der Hinweistext: „Neue Lager legt der Dienst vor ihren Plätzen an; Plätze eines wartenden Lagers bleiben
‚Offen‘.“ Ein Lager mit Status `Fehler` ist für neue Plätze gesperrt (F1, S10). Folge außerhalb der Lagerplatzliste (nur Hinweis in Hilfe und
ADR, kein Code): die Lagerort-Auswahl der Kommissionierbereichs-Regeln zeigt jedes `StorageArea` ohne Aktiv-/Outbox-Filter
(`PickingAreaRepository.cs:36-37`), also auch ein wartendes oder fehlerhaftes neues Lager.

**F7 Bearbeiten eines Sage-Anlage-Platzes vor der Übergabe.** `Code`, `Description`, `Zone` sind nicht änderbar (Code ist der Schlüssel zu Sage);
`IsActive`, `IstBuchbar`, `SageBuchungErlaubt`, `Capacity`, `IsPickingTransport` schon. Das Speichern darf nicht an der 12-Zeichen-Regel für
manuelle Codes scheitern (`StorageLocation.cs:124`). Falsch erfasste Plätze: deaktivieren und neu erfassen; ein inaktiver Platz wird nicht
gesendet (F3).

## Technischer Loesungsentwurf

**Bausteine (Leiter: vorhandene Muster wiederverwenden)**

| Baustein | Ort | Muster/Vorlage |
|---|---|---|
| `ISageLagerplatzAnlage` + `SqlSageLagerplatzAnlage` | `IDEALAKEWMSService/Services/` | Reader-Interfaces (`ISageLagerplatzReader.cs:15-18`); einzige gerechtfertigte Abstraktion (Entscheid 2) |
| `SageLagerplatzAnlageService.RunOnceAsync` | `IDEALAKEWMSService/Services/` | `SageFaArbeitsgangService` (Schritt im Worker, `Program.cs:161-168`) |
| Schritt `ProcessSageLagerplatzAnlageAsync` + Merker `_lpTechnischGemeldet` | `SageBookingWorker` | eigener try-Block wie `:136-150`, Schalter nicht Safe lesen wie `:268`, Meldung nur beim Wechsel wie `:272-281`/`:292` (S7) |
| `IStorageLocationRepository.AddSageAnlageAsync` (Lager + Plätze + Outbox in einer Transaktion) | `StorageLocationRepository` | `BfAuftragRepository.cs:105-112` (S2) |
| `SyncLogServices.SageLagerplatzAnlage`, Anzeige „Lagerplätze → Sage“ | `SyncLogServices.cs` | `:44`, `:66`, `:108`; Counts `angelegt`, `vorhanden`, `fehler`, `uebersprungen`, `lager_angelegt`, `lager_vorhanden`, `lager_fehler` |
| Schalter-Konstanten `SageLagerplatzAnlage.ToggleKey`, `LagertechnikenKey`, feste Liste der fünf Techniken (Anhang 4.4, AKE P-5 gleich) + Stufen, `ServiceActor = "system:sage-lagerplatz-anlage"` | `IdealAkeWms/Services/` | `SageFaArbeitsgaenge.cs:16-21`; `// ponytail: Technikliste fest aus Anhang 4.4/P-5; neue Sage-Technik = Konstante erweitern` |
| `SageStammdatenOutbox.TypLagerort = "StorageArea"`; `ErledigenAsync` zusätzlich für Status `Gesendet` | `IdealAkeWms/Data/SageStammdatenOutbox.cs` | `:14-15`, `:30-45` |
| Fehlend-Schleife überspringt Lager mit offener Lager-Übergabe | `LagerplatzSyncService.SyncStorageAreasAsync` | `:364-375` (F4) |
| Formular `CreateInSage` (GET/POST) + ViewModel inkl. Lager-Neuanlage | `StorageLocationsController`, `Views/StorageLocations/CreateInSage.cshtml` | Create-Action `:94-120` |
| „Erneut senden“ Platz und Lager (je ein POST) | `StorageLocationsController` | `[RequireMasterDataAccess]` (Freigabe-Antwort 3), `ValidateAntiForgeryToken` |

**Schnittstelle (eine Implementierung, Austausch gegen SData angekündigt)**

```csharp
public enum SageLagerplatzAnlageArt { Angelegt, Vorhanden, Abgelehnt }
// S6: Sperr-/Masswerte der Sage-Zeile fuer die Uebernahme; SageDb = "<@@SERVERNAME>/<DB_NAME()>" fuer die Protokoll-Info (Antwort 8).
public record SageLagerplatzAnlageErgebnis(SageLagerplatzAnlageArt Art, int? PlatzId, string? Platzbezeichnung, string? Grund,
    bool? IstGesperrt = null, bool? IstGesperrtInventur = null, decimal? Laenge = null, decimal? Breite = null, decimal? Hoehe = null,
    decimal? Volumen = null, decimal? Tragkraft = null, string? SageDb = null);
public interface ISageLagerplatzAnlage
{
    // Fachliche Ablehnung = Ergebnis Abgelehnt mit Grund; technische Fehler (Klassen F3) = Exception (Aufrufer: F3 Punkt 5).
    Task<SageLagerplatzAnlageErgebnis> AnlegenAsync(string lagerkennung, int d1, int d2, int d3, CancellationToken ct);
    // F3a: PlatzId = Basisplatz, Platzbezeichnung = Lagerbezeichnung (gleiches Ergebnis-Record, kein zweites).
    // neuanlageErlaubt (H4): false = nur "vorhanden" uebernehmen, Neuanlage -> 50006.
    Task<SageLagerplatzAnlageErgebnis> LagerAnlegenAsync(string lagerkennung, string bezeichnung, string lagertechnik, bool neuanlageErlaubt,
        CancellationToken ct);
}
```

**Parameter und Sperren (S4).** Alle Parameter typisiert: `@Lager` `SqlDbType.VarChar` 10, `@Bez` `VarChar` 40, `@Technik` `VarChar` 10,
`@D1–@D3` `Int`, `@Mandant` `SmallInt`, `@NeuanlageErlaubt` `Bit` – kein `nvarchar` (unter der SQL-Kollation `SQL_Latin1_General_CP1_CI_AS`,
P-9d, verhinderte ein `nvarchar`-Vergleich Index-Seeks). Beide Befehle beginnen mit `SET DEADLOCK_PRIORITY LOW;`: Die Sperrreihenfolge des
WMS (`KHKLagerplaetze` → `KHKTan`) ist umgekehrt zu der des Sage-Dialogs (`spSysTan` → INSERT, Anhang 5.1); bei einem Deadlock soll das WMS das
Opfer sein (1205 = technisch, nächster Takt), nicht der Sage-Anwender. `UPDLOCK, HOLDLOCK` mit `… OR Kurzbezeichnung = @Kurz` kann ohne Index
auf `Kurzbezeichnung` die ganze Tabelle sperren (AKE 4595 Plätze); das ist bei je einer kurzen Transaktion je Platz hingenommen, die Indexlage
liefert P-10 als Information für die ADR. **Ergebnis erst nach `COMMIT` (S5):** Die Werte stehen in Variablen; das `SELECT` an den Client folgt
dem `COMMIT`. Ein Fehler beim `COMMIT` kann so nicht hinter einer schon gelesenen Ergebnismenge verloren gehen. Der Client liest mit
`ExecuteReaderAsync` genau eine Zeile; fehlt sie, ist das ein fachlicher Fehler „Sage lieferte kein Ergebnis“.

Die Outbox, der Status-Automat, die Übernahme in F3 Punkt 3 und die gesamte Oberfläche hängen nur an diesem Vertrag. Rückbau auf SData = neue
Implementierung registrieren, `SqlSageLagerplatzAnlage` löschen, ADR 0036 superseden.

**Sage-Schreibbefehl je Platz (eine Transaktion)** – Ableitung aus Anhang 8.2 für genau einen Platz, mit Prüfungen aus F5. Parametrisiert
(`SqlParameter`, keine Literale), `Mandant = 1` als Konstante wie im Lese-Abgleich (`SageLagerplatzReader.cs:27`,
`// ponytail: Mandant fest 1 wie die Reader, Setting erst bei zweitem Mandanten`). Fehler 50010–50019 = fachlich.

```sql
SET XACT_ABORT ON;
SET DEADLOCK_PRIORITY LOW;                                            -- S4: WMS ist Deadlock-Opfer, nicht der Sage-Dialog
DECLARE @LagerBez varchar(40), @B1 varchar(50), @B2 varchar(50), @B3 varchar(50), @Aktiv smallint, @Ebenen smallint,
        @InvUeb datetime, @InvZaehl datetime, @InvJahr smallint, @PlatzID int, @Kurz varchar(50), @Bez varchar(200), @n int,
        @Angelegt bit = 0;
BEGIN TRAN;
  SELECT @LagerBez = o.Bezeichnung, @Aktiv = o.Aktiv,
         @B1 = t.BezeichnungDimension1, @B2 = t.BezeichnungDimension2, @B3 = t.BezeichnungDimension3
  FROM KHKLagerorte o WITH (UPDLOCK, HOLDLOCK)
  JOIN KHKLagertechnikenArten t ON t.Lagertechnikkennung = o.Lagertechnik
  WHERE o.Mandant = @Mandant AND o.Lagerkennung = @Lager;
  IF @@ROWCOUNT = 0 THROW 50010, 'Lager in Sage nicht gefunden, ohne Lagertechnik oder mit unbekannter Lagertechnik', 1;   -- H5: Lagertechnik nullable
  IF @Aktiv <> -1 THROW 50013, 'Lager ist in Sage inaktiv', 1;      -- inaktive Lager liest der Abgleich nicht (SageLagerplatzReader.cs:28)
  SET @Ebenen = IIF(@B1 IS NULL,0,1) + IIF(@B2 IS NULL,0,1) + IIF(@B3 IS NULL,0,1);
  IF @Ebenen = 0 THROW 50011, 'Lagertechnik ohne Dimensionen - keine Plaetze moeglich', 1;
  IF @D1 < 1 OR (@Ebenen >= 2 AND @D2 < 1) OR (@Ebenen < 2 AND @D2 <> 0) OR (@Ebenen >= 3 AND @D3 < 1) OR (@Ebenen < 3 AND @D3 <> 0)
     THROW 50012, 'Koordinaten passen nicht zur Lagertechnik', 1;     -- Klartext im C# mit @B1..@B3 ergänzen
  SET @Kurz = CONCAT(@Lager, ';', @D1, ';', @D2, ';', @D3);
  SET @Bez  = CONCAT(@LagerBez, ';', @B1, ' ', @D1, IIF(@Ebenen >= 2, CONCAT(';', @B2, ' ', @D2), ''), IIF(@Ebenen >= 3, CONCAT(';', @B3, ' ', @D3), ''));
  IF LEN(@Bez) > 110 THROW 50015, 'Platzbezeichnung laenger als 110 Zeichen', 1;
  -- F5: vorhanden / Konflikt (kein Unique-Index in Sage, Anhang 2.4); Kurzbezeichnung mandantenweit (WMS-Code ist global eindeutig)
  SELECT @n = COUNT(*), @PlatzID = MIN(PlatzID) FROM KHKLagerplaetze WITH (UPDLOCK, HOLDLOCK)
  WHERE Mandant = @Mandant
    AND ((Lagerkennung = @Lager AND Dimension1 = @D1 AND Dimension2 = @D2 AND Dimension3 = @D3) OR Kurzbezeichnung = @Kurz);
  IF @n > 1 OR (@n = 1 AND NOT EXISTS (SELECT 1 FROM KHKLagerplaetze WHERE PlatzID = @PlatzID AND Mandant = @Mandant AND Lagerkennung = @Lager
                                        AND Dimension1 = @D1 AND Dimension2 = @D2 AND Dimension3 = @D3 AND Kurzbezeichnung = @Kurz))
     THROW 50014, 'Konflikt in Sage: Koordinaten oder Kurzbezeichnung schon anders belegt', 1;
  IF @n = 0
  BEGIN
    SELECT @InvUeb = LetztesInventurUebernahmedatum, @InvZaehl = LetztesInventurZaehldatum, @InvJahr = LetztesInventurjahr
    FROM KHKLagerplaetze WHERE Mandant = @Mandant AND Lagerkennung = @Lager AND Dimension1 = 0 AND Dimension2 = 0 AND Dimension3 = 0;
    EXEC spSysTan @Table = 'KHKLagerplaetze', @Mandant = @Mandant, @Increment = 1, @Tan = @PlatzID OUTPUT;   -- nie MAX+1 (Anhang 2.1)
    INSERT INTO KHKLagerplaetze (PlatzID, Mandant, Lagerkennung, Dimension1, Dimension2, Dimension3, Dimensionsebene, Kurzbezeichnung,
           Platzbezeichnung, IstGesperrt, Laenge, Breite, Hoehe, Volumen, Tragkraft, Status, Inventurmethode, IstGesperrtInventur,
           LetztesInventurUebernahmedatum, LetztesInventurZaehldatum, LetztesInventurjahr)
    VALUES (@PlatzID, @Mandant, @Lager, @D1, @D2, @D3, @Ebenen, @Kurz, @Bez, 0, 0, 0, 0, 0, 0, 0, 0, 0, @InvUeb, @InvZaehl, @InvJahr);
    UPDATE o SET AnzahlDimension1 = x.M1, AnzahlDimension2 = x.M2, AnzahlDimension3 = x.M3, AnzahlLagerplaetze = x.Anz, IstPlatzverwaltet = -1
    FROM KHKLagerorte o CROSS APPLY (SELECT MAX(Dimension1) M1, MAX(Dimension2) M2, MAX(Dimension3) M3, COUNT(*) Anz
                                     FROM KHKLagerplaetze p WHERE p.Mandant = o.Mandant AND p.Lagerkennung = o.Lagerkennung) x
    WHERE o.Mandant = @Mandant AND o.Lagerkennung = @Lager;          -- Zähler aus den Daten, in derselben Transaktion (Anhang 5.4, besser als Sage)
    SET @Angelegt = 1;
  END
COMMIT;
-- S5: Ergebnis erst nach dem COMMIT; S6: Sperr-/Masswerte fuer die Uebernahme; Antwort 8: Ziel-DB fuer das Protokoll
SELECT p.PlatzID, @Angelegt AS Angelegt, p.Platzbezeichnung, p.IstGesperrt, p.IstGesperrtInventur, p.Laenge, p.Breite, p.Hoehe, p.Volumen,
       p.Tragkraft, CONCAT(@@SERVERNAME, '/', DB_NAME()) AS SageDb
FROM KHKLagerplaetze p WHERE p.PlatzID = @PlatzID AND p.Mandant = @Mandant;
```

Unverändert aus dem Anhang übernommen und **nicht** abgewandelt: `KHKBoolean` `-1/0` (nie `1`), `Timestamp` nie schreiben, Inventurmethode und
-sperre fest 0, Inventurdaten vom Basisplatz, Dimensionsebene = Anzahl Dimensionen der Lagertechnik, keine Trigger beim INSERT/UPDATE (Anhang 1;
AKE belegt P-2: nur DELETE-Trigger `TRD_Lagerorte`, `TRD_Lagerplaetze`, in `ake_TEST2026` zusätzlich `TRD_Lagerplaetze_DPSBSKonsi` – für das
WMS ohne Belang, es löscht nicht; IDEAL Prüfschritt P-2), `spCreateClassHistory` entfällt (Anhang 1). An `KHKLagerplaetze` gibt es an AKE
**keine** `USER_`-Spalten (P-1). Das Referenz-SQL ist laut Anhang **nicht gegen die Datenbank getestet**; vor jedem Einschalten an einem
Produktivmandanten ist der Abnahmetest (TS, Schritt .k) am Testmandanten desselben Standorts Pflicht.

**Sage-Schreibbefehl je Lager (F3a, eine Transaktion)** – Ableitung aus Anhang 8.1 mit drei bewussten Abweichungen: (a) **keine `USER_`-Spalten**
im INSERT – sie sind mandantenspezifisch (AKE: `USER_Aussenlager`, `USER_DPSBSMDELagerInaktivBei`; IDEAL laut Anhang 4.2:
`USER_DPSBSMDELagerInaktivBei`, `USER_LagerbestandLeeren`, `USER_IstAussenlager`), der Anhang-INSERT liefe an AKE auf unbekannte Spalten; die
DB-Defaults ergeben an IDEAL dieselben Werte, die Sage schreibt (0, Anhang 4.2); was an AKE in `USER_Aussenlager` landet (Default NULL), zeigt
der Abnahmevergleich; (b) Basisplatz mit **expliziten** Nullwerten für Dimensionen/Maße/Status (= die IDEAL-Defaults aus Anhang 4.3), weil die
AKE-Defaults dieser Spalten in den Prüfergebnissen nicht ausgewiesen sind; (c) Idempotenz „vorhanden“ statt Abbruch 50003.

```sql
SET XACT_ABORT ON;
SET DEADLOCK_PRIORITY LOW;                                            -- S4
DECLARE @PlatzID int, @Bez0 varchar(40), @Tech0 varchar(10), @Aktiv0 smallint, @Kurz varchar(50) = CONCAT(@Lager, ';0;0;0'),
        @Angelegt bit = 0, @BezOut varchar(40);
BEGIN TRAN;
  SELECT @Bez0 = Bezeichnung, @Tech0 = Lagertechnik, @Aktiv0 = Aktiv
  FROM KHKLagerorte WITH (UPDLOCK, HOLDLOCK) WHERE Mandant = @Mandant AND Lagerkennung = @Lager;
  IF @@ROWCOUNT = 1
  BEGIN
    IF NOT (@Bez0 = @Bez AND @Tech0 = @Technik AND @Aktiv0 = -1)
       THROW 50003, 'Lagerkennung in Sage schon anders belegt oder inaktiv', 1;
    SELECT @PlatzID = PlatzID FROM KHKLagerplaetze
    WHERE Mandant = @Mandant AND Lagerkennung = @Lager AND Dimension1 = 0 AND Dimension2 = 0 AND Dimension3 = 0;
    SET @BezOut = @Bez0;                                                                  -- vorhanden (auch ohne Freigabe, H4)
  END
  ELSE
  BEGIN
    IF @NeuanlageErlaubt = 0
       THROW 50006, 'Lagertechnik fuer die Lager-Anlage an diesem Standort nicht freigegeben', 1;   -- H4: nur fuer die Neuanlage
    IF NOT EXISTS (SELECT 1 FROM KHKLagertechnikenArten WHERE Lagertechnikkennung = @Technik) THROW 50001, 'Lagertechnik in Sage unbekannt', 1;
    IF NOT EXISTS (SELECT 1 FROM KHKGruppen WHERE Mandant = 0 AND Typ = 40025 AND Tag = 'SWF') THROW 50002, 'WarenflussKZ SWF in Sage unbekannt', 1;
    IF EXISTS (SELECT 1 FROM KHKLagerplaetze WITH (UPDLOCK, HOLDLOCK) WHERE Mandant = @Mandant AND Kurzbezeichnung = @Kurz)
       THROW 50005, 'Kurzbezeichnung des Basisplatzes in Sage schon vergeben', 1;      -- AKE: Hauptlager;0;0;0 (P-6)
    -- S1/P-9a: KHKArtikelVariantenLagerplatz hat keine Lagerkennung, nur PlatzID -> Join ueber den Platz (Sage prueft das ebenso, Anhang 4.1)
    IF EXISTS (SELECT 1 FROM KHKArtikelVariantenLagerplatz a JOIN KHKLagerplaetze p ON p.PlatzID = a.PlatzID AND p.Mandant = a.Mandant
               WHERE p.Mandant = @Mandant AND p.Lagerkennung = @Lager)
       THROW 50004, 'Artikel-Lagerplatz-Zuordnungen zu Plaetzen dieser Lagerkennung vorhanden', 1;
    -- S1: verwaiste Plaetze derselben Kennung (ohne Lager) wuerden sonst still Teil des neuen Lagers
    IF EXISTS (SELECT 1 FROM KHKLagerplaetze WITH (UPDLOCK, HOLDLOCK) WHERE Mandant = @Mandant AND Lagerkennung = @Lager)
       THROW 50007, 'Lagerplaetze mit dieser Lagerkennung ohne Lager in Sage vorhanden', 1;
    EXEC spSysTan @Table = 'KHKLagerplaetze', @Mandant = @Mandant, @Increment = 1, @Tan = @PlatzID OUTPUT;
    INSERT INTO KHKLagerorte
      (Lagerkennung, Mandant, Bezeichnung, Lagertechnik, WarenflussKZ, Aktiv, AnzahlLagerplaetze, IstDispoLager, IstFremdlager,
       IstKommissionslager, IstSperrlager, IstWELager, IstHauptWELager, IstWALager, IstHauptWALager, MitArtikelzuordnung,
       Einlagerungsregel, Einlagerungsprioritaet, Auslagerungsregel, Auslagerungsprioritaet, Memo)
    VALUES (@Lager, @Mandant, @Bez, @Technik, 'SWF', -1, 1, -1, 0, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL, NULL);  -- SWF: alle WE/WA-Kennzeichen 0
    INSERT INTO KHKLagerplaetze (PlatzID, Mandant, Lagerkennung, Dimension1, Dimension2, Dimension3, Dimensionsebene, Kurzbezeichnung,
           Platzbezeichnung, IstGesperrt, Laenge, Breite, Hoehe, Volumen, Tragkraft, Status, Inventurmethode, IstGesperrtInventur)
    VALUES (@PlatzID, @Mandant, @Lager, 0, 0, 0, 0, @Kurz, @Bez, 0, 0, 0, 0, 0, 0, 0, 0, 0);       -- Inventurdaten NULL wie Sage (4.3)
    SELECT @Angelegt = 1, @BezOut = @Bez;
  END
COMMIT;
SELECT @PlatzID AS PlatzID, @Angelegt AS Angelegt, @BezOut AS Platzbezeichnung, CONCAT(@@SERVERNAME, '/', DB_NAME()) AS SageDb;   -- S5: nach COMMIT
```

`IstPlatzverwaltet` und `AnzahlDimension1–3` stehen nicht im INSERT (bleiben 0 wie bei Sage, Anhang 4.1); sie setzt erst der Platz-Befehl.
Vergleich `@Bez0 = @Bez` folgt der Kollation der Sage-DB: AKE `SQL_Latin1_General_CP1_CI_AS` (P-9d) → ohne Groß/Klein, wie der WMS-Abgleich;
IDEAL laut P-9 vor dem Einschalten prüfen. Die Prüfungen 50001/50002 stehen seit H4 nur im Neuanlage-Zweig (ein vorhandenes Lager braucht sie
nicht). ~~`KHKArtikelVariantenLagerplatz … WHERE … Lagerkennung = @Lager`~~ **überholt** (S1, P-9a: Spalte existiert nicht; ein Verweis auf
eine fehlende Spalte bricht den ganzen Batch mit 207 ab).

**Verbindung (Freigabe-Antwort 2).** Der Dienst schreibt, nicht das Web (Web ohne Sage-SQL-Verbindung, `IdealAkeWms/appsettings.json:2-5`),
über die **bestehende** `ConnectionStrings:SageConnection` mit `Trusted_Connection` (`IDEALAKEWMSService/appsettings.json:4`), also als
Dienstkonto (AKE `AKENB\svc_idealakewms`; IDEAL das Konto, unter dem der IDEAL-Dienst läuft – im Deploy ablesen). Kein eigener Connection
String. Der DBA vergibt je Standort-Sage-DB nur: `INSERT` auf `KHKLagerplaetze`, `INSERT`, `UPDATE` auf `KHKLagerorte`, `EXECUTE` auf
`spSysTan` (Owner `dbo` wie `KHKTan`, AKE belegt P-4 → Ownership-Chaining deckt das Schreiben in `KHKTan`); `SELECT` auf `KHKLagerorte`,
`KHKLagerplaetze`, `KHKLagertechnikenArten`, `KHKGruppen`, `KHKArtikelVariantenLagerplatz` nur, falls nicht schon über `db_datareader` vorhanden
(GRANT-Text im Deploy). Der Schalter (Default aus) bleibt die Sicherung. ~~Eigener Connection String `ConnectionStrings:SageSchreibConnection`~~
**überholt** (Freigabe-Antwort 2).

**ADR 0036 (Dev-Lauf schreibt sie in den Hauptcheckout, Nummer vorher prüfen).** Titel-Vorschlag: „Lagerplatz-Anlage in Sage per Direkt-SQL
hinter austauschbarer Schnittstelle, bis DPS einen SData-Service liefert“. Pflichtinhalte:
- Kontext: G5 (kein SData-Service), Entscheid Gerald 2026-11-24, Anhang als Referenz, bisher nur SData-Schreibwege (Lagerbuchung, Wareneingang,
  AG-Anlage).
- Rechte: bestehendes Dienstkonto der `SageConnection` mit Objekt-GRANTs (Liste oben), kein `db_datawriter`, keine Rechte auf andere Tabellen
  (Freigabe-Antwort 2; ~~eigener Login~~ überholt).
- Updatefestigkeit: Sage-Versionswechsel können Spalten, Defaults, Trigger, `spSysTan` ändern → nach jedem Sage-Update vor dem Wiedereinschalten
  P-1 bis P-3 (und P-9 bei Lager-Anlage) erneut prüfen; Schalter bis dahin aus. Neue `USER_`-Pflichtfelder ohne Default brechen den INSERT ab
  (sichtbarer fachlicher Fehler 515, kein Teilschreiben, F3 Punkt 4 und Fehlerklassen).
- Support DPS: Direktschreiben ist kein von Sage/DPS unterstützter Weg; DPS wird bewusst nicht informiert (Freigabe-Antwort 6). Abgrenzung im
  Fehlerfall: Outbox + Protokoll zeigen jeden WMS-Schreibvorgang mit PlatzID und Zeit.
- Lager-Anlage „wenn möglich“: nur für Lagertechniken, deren Lager-Anlage am Standort durch Mitschnitt bzw. Abnahmevergleich belegt ist
  (Einstellung `SageLagerAnlageLagertechniken`); WarenflussKZ fest `SWF`, `HWE`/`HWA` nie; Lagertechnik/Bezeichnung bestehender Lager nie ändern.
- Transaktion: je Lager bzw. je Platz eine Transaktion inkl. Zähler-UPDATE, `XACT_ABORT`, kurz (`spSysTan` sperrt `KHKTan` bis Commit, Anhang 2.6).
- Idempotenz: Prüfen-und-Anlegen in einer Transaktion mit `UPDLOCK, HOLDLOCK`; Wiederholung ist gefahrlos; Restrisiko gleichzeitiger
  Sage-Dialog (F5). Ausdrücklich: Abweichung vom Hausmuster „hängend nie blind neu senden“ (`SageBookingWorker.cs:19`) – erlaubt, weil der
  Befehl idempotent ist (H12).
- Sperren: `DEADLOCK_PRIORITY LOW` (WMS ist Opfer), typisierte `varchar`-Parameter, Ergebnis nach `COMMIT`; Indexlage `KHKLagerplaetze` aus P-10
  festhalten (S4, S5).
- Fehlerklassen technisch/fachlich wie F3 (S3); Meldung technischer Fehler nur beim Wechsel (S7).
- Bekannte Folgen: hängender Eintrag eines deaktivierten Platzes wird vom Abgleich übernommen und reaktiviert (H9); ein neues Lager erscheint vor
  der Übergabe schon in der Lagerort-Auswahl der Kommissionierbereichs-Regeln (`PickingAreaRepository.cs:36-37`, S10).
- Rückbau: SData-Implementierung des Interfaces, Outbox/Status/Oberfläche bleiben; ADR wird superseded.

**Zusatzfund (nicht mitbauen, melden):** `SageLagerplatzReader.cs:26` verbindet `KHKLagerorte` und `KHKLagerplaetze` nur über `Lagerkennung`, ohne
`Mandant`. Mit nur Mandant 1 ohne Wirkung (SData-Spec: `LagerplaetzeInfo where Mandant ne 1` → 0 am AKE-Test); bei einem zweiten Mandanten in
derselben DB entstünden Fremdplätze.

## Migrations-/SQL-Auswirkungen

- **Keine EF-Migration, kein `SQL/XX_*.sql`, kein FreshInstall-Nachzug** – auch mit der Lager-Anlage: Outbox (SQL/107) nimmt jeden Typ auf
  (`EntityType NVARCHAR(50)` ohne CHECK, `SQL/107_…sql:82`; `ApplicationDbContext.cs:1271`), das Lager wird als Typ `StorageArea` mit
  `EntityKey` = StorageArea-Id abgebildet; der Lager-Status lebt nur in der Outbox (kein Feld am `StorageArea` nötig, F6); `StorageAreas`
  (SQL/98) hat alle zu erfassenden Felder (`StorageArea.cs:13-34`), `WarenflussKZ` muss nicht gespeichert werden (fest `SWF`).
  Übergabe-Felder am Platz (SQL/107), `StorageAreaId` (SQL/98). Der Dev-Lauf prüft das am Diff; braucht er doch eine Spalte, gilt ADR 0004
  vollständig (Model → Migration → `SQL/XX` mit `OBJECT_ID`-Guard → History-Insert → FreshInstall an zwei Stellen) und die nächste freie
  SQL-Nummer im Bündel.
- `SQL/AgentJobs/`: nicht betroffen (keine Referenz auf `StorageLocations`/`StorageAreas`/`SageStammdatenOutbox`, Suche 2026-11-24).
- **Sage-Datenbank (Außenwirkung):** Der Dienst schreibt `KHKLagerorte` (INSERT neues Lager, UPDATE Zähler), `KHKLagerplaetze` (INSERT Basisplatz
  und Plätze) und über `spSysTan` `KHKTan`. Nicht destruktiv (nur Anlage, kein Löschen, kein Ändern bestehender Plätze oder Lagerstammdaten);
  trotzdem vor dem ersten Einschalten je Produktivmandant ein Sage-DB-Backup (Betrieb Sage).

**Prüfabfragen für den Menschen (Sage steht nicht im Code; Aussagen dazu gelten bis zur Prüfung als unbelegt).** Datei
`backlog/anhaenge/2026-11-24-pruefabfragen-lagerplatzanlage-sage.sql` (P-1 bis P-7 ohne Mandant-Filter) bzw. die Einzelabfragen unten, in SSMS
gegen die Sage-Mandanten-DB, je Standort zuerst Testmandant, vor dem Produktiv-Einschalten gegen die Produktiv-DB.

**Stand je Standort:**
- **AKE (`ake` und `ake_TEST2026` auf `AKESQL20.ake.at`): P-1, P-2, P-3, P-5, P-6, P-7 erledigt** (Prüfergebnisse 2026-11-24): keine
  `USER_`-Spalten an `KHKLagerplaetze`; Lager-INSERT spaltenseitig möglich (NOT NULL ohne Default nur `Lagerkennung`); nur DELETE-Trigger;
  `spSysTan` wie erwartet, `Tan = MAX(PlatzID)`; Techniken wie Anhang 4.4; platzverwaltete Lager `GL`, `HR`, `KonsHainzl`, `LL`, `SL`, `VT`
  (`VT` fehlt in `ake_TEST2026`); 1 abweichende Kurzbezeichnung (`HAUPT;0;0;0` = `Hauptlager;0;0;0`); keine Dubletten. **P-8, P-9 erledigt**
  (Nachtrag 2026-11-24): `KHKArtikelVariantenLagerplatz` ohne `Lagerkennung` (→ S1, Join über `PlatzID`), `SWF` vorhanden, `USER_` an
  `KHKLagerorte` ohne Pflicht, Kollation `SQL_Latin1_General_CP1_CI_AS`; die AKE-WMS-DBs stehen vor SQL/98 (keine `StorageAreas`,
  `Sync:Quelle:Lagerplaetze` nicht gesetzt). **Offen an AKE:** P-4 Nachprüfung aus dem Dienstkontext nach dem GRANT (`EXECUTE AS LOGIN`
  scheiterte an der AD-Abfrage), P-10 (Information). Für DEV AKE (Antwort 7) gilt dieselbe Test-DB `ake_TEST2026`.
- **IDEAL (`IDEAL_TEST_2026_05_03` auf `idealsql2.ideal.ideal-ake.at\SAGE100TEST`, danach Produktiv-DB):** per sqlcmd nicht geprüft (Anmeldung
  abgewiesen; Zugang bisher über Royal TS mit `IDEAL\t1_adminweg`). Für die Test-DB belegt der Anhang bereits P-2 (nur DELETE-Trigger, Anhang 1),
  P-3 (`spSysTan`-Quelltext, Anhang 3) und die `USER_`-Spalten von `KHKLagerorte` (alle mit Default, Anhang 4.2) (H1). ~~**Voraussetzung vor dem
  Dev-Lauf**: P-1, P-2, Owner-Teil P-4~~ **überholt (H1):** Die IDEAL-Prüfungen sind **kein** Freigabe- oder Dev-Lauf-Blocker; der Code hängt
  von ihnen nicht ab (`USER_`-Spalten werden nie geschrieben, abweichende Spalten/Prozeduren sind fachliche Fehler nach F3).
  **Voraussetzung vor dem Einschalten an IDEAL** (Test wie Produktiv): P-1 (insbesondere `USER_` an `KHKLagerplaetze`), P-2, **P-3** (codewirksam:
  `spSysTan`), P-4 (Owner und Rechte-Nachprüfung nach dem GRANT), P-5 bis P-9, für die Produktiv-DB alle erneut.
  Weicht das Ergebnis vom AKE-Stand ab (Trigger auf INSERT/UPDATE, anderer `spSysTan`, Owner ≠ `dbo`), bleibt der Schalter an IDEAL aus und die
  Spec braucht einen Nachtrag; `USER_`-Spalten allein ändern den Code nicht (sie werden nicht geschrieben, 515/207 = fachlicher Fehler). **Ergebnis-Regel:**
  hat `KHKLagerorte` an IDEAL eine `USER_`-Spalte `IS_NULLABLE = NO` ohne `COLUMN_DEFAULT`, bleibt `SageLagerAnlageLagertechniken = Aus`
  (nur Platz-Anlage); hat `KHKLagerplaetze` eine solche Spalte, bleibt auch `SageLagerplatzAnlageAktiv` aus und die Spec braucht einen Nachtrag.

- **P-1 Spalten/Defaults** (neue `USER_`-Pflichtfelder brechen den INSERT):
  `SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN ('KHKLagerplaetze','KHKLagerorte') ORDER BY TABLE_NAME, ORDINAL_POSITION;`
  Erwartet: jede Spalte von `KHKLagerplaetze`, die nicht im INSERT steht, ist `IS_NULLABLE = YES` oder hat `COLUMN_DEFAULT`; Längen
  Kurzbezeichnung 50, Platzbezeichnung 110 (Anhang 5.3).
- **P-2 Trigger:** `SELECT OBJECT_NAME(t.parent_id) AS Tabelle, t.name, te.type_desc, t.is_disabled FROM sys.triggers t JOIN sys.trigger_events te ON te.object_id = t.object_id WHERE t.parent_id IN (OBJECT_ID('dbo.KHKLagerplaetze'), OBJECT_ID('dbo.KHKLagerorte'));`
  Erwartet: nur `DELETE` (Anhang 1).
- **P-3 Zähler:** `SELECT p.name, TYPE_NAME(p.user_type_id) AS Typ, p.is_output FROM sys.parameters p WHERE p.object_id = OBJECT_ID('dbo.spSysTan'); SELECT Tan FROM KHKTan WHERE Tabelle = 'KHKLagerplaetze' AND Mandant = 1; SELECT MAX(PlatzID) FROM KHKLagerplaetze WHERE Mandant = 1;`
  Erwartet: Parameter `@Table`, `@Mandant`, `@Increment`, `@Tan OUTPUT`; `Tan ≥ MAX(PlatzID)`.
- **P-4 Rechte des Dienstkontos** (nach dem GRANT im Deploy; **aus dem Dienstkontext**, weil `EXECUTE AS LOGIN` an AKE an der AD-Abfrage
  scheiterte – z. B. `runas /user:<Dienstkonto> "sqlcmd -S <Server> -d <SageDB> -E -Q ..."` oder als DBA `EXECUTE AS USER = N'<Dienstkonto>'` in
  der Mandanten-DB, falls der Datenbank-Benutzer existiert):
  `SELECT SUSER_SNAME() AS Konto, HAS_PERMS_BY_NAME('dbo.KHKLagerplaetze','OBJECT','INSERT') AS InsPlatz, HAS_PERMS_BY_NAME('dbo.KHKLagerorte','OBJECT','INSERT') AS InsOrt, HAS_PERMS_BY_NAME('dbo.KHKLagerorte','OBJECT','UPDATE') AS UpdOrt, HAS_PERMS_BY_NAME('dbo.spSysTan','OBJECT','EXECUTE') AS ExecTan, HAS_PERMS_BY_NAME('dbo.KHKLagertechnikenArten','OBJECT','SELECT') AS SelTechnik, HAS_PERMS_BY_NAME('dbo.KHKGruppen','OBJECT','SELECT') AS SelGruppen, HAS_PERMS_BY_NAME('dbo.KHKArtikelVariantenLagerplatz','OBJECT','SELECT') AS SelZuordnung;`
  plus Eigentümer: `SELECT name, USER_NAME(OBJECTPROPERTY(object_id,'OwnerId')) AS Owner FROM sys.objects WHERE name IN ('spSysTan','KHKTan');`
  Erwartet: `Konto` = Dienstkonto, alle 1; gleicher Owner (AKE belegt: beide `dbo`; sonst braucht das Konto zusätzlich `UPDATE` auf `KHKTan`).
- **P-5 Lagertechniken/Lager:** `SELECT Lagertechnikkennung, BezeichnungDimension1, BezeichnungDimension2, BezeichnungDimension3 FROM KHKLagertechnikenArten; SELECT Lagerkennung, Bezeichnung, Lagertechnik, Aktiv, IstPlatzverwaltet, AnzahlLagerplaetze FROM KHKLagerorte WHERE Mandant = 1;`
  Erwartet: Techniken wie Anhang 4.4.
- **P-6 abweichende Kurzbezeichnungen** (Konfliktfälle F5): `SELECT COUNT(*) FROM KHKLagerplaetze WHERE Mandant = 1 AND Kurzbezeichnung <> CONCAT(Lagerkennung, ';', Dimension1, ';', Dimension2, ';', Dimension3);`
- **P-7 Dubletten:** `SELECT Lagerkennung, Dimension1, Dimension2, Dimension3, COUNT(*) FROM KHKLagerplaetze WHERE Mandant = 1 GROUP BY Lagerkennung, Dimension1, Dimension2, Dimension3 HAVING COUNT(*) > 1;`
- **P-8 (WMS-DB des Standorts) Lagerort-Liste:** `SELECT [Key], [Value] FROM ServiceSettings WHERE [Key] IN ('Sync:LagerplaetzeEnabled','Sync:Quelle:Lagerplaetze'); SELECT COUNT(*) FROM StorageAreas WHERE IsActive = 1 AND SageMissingSince IS NULL;`
  Erwartet: Lagerplatz-Abgleich an, Quelle `SData`, Anzahl > 0. Sonst bleibt das Formular leer (F1). Vor SQL/98 (AKE-WMS-DBs heute) scheitert
  die zweite Abfrage mit Msg 208 – das ist der erwartete Stand vor dem Bündel-Deploy; die Manifest-Prüfung (Deploy) ist deshalb an SQL/98 gebunden.
- **P-9 Lager-Anlage** (nur wenn `SageLagerAnlageLagertechniken` ≠ `Aus` werden soll):
  `SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'KHKArtikelVariantenLagerplatz' ORDER BY ORDINAL_POSITION;`
  `SELECT Tag, Bezeichnung FROM KHKGruppen WHERE Mandant = 0 AND Typ = 40025;`
  `SELECT COLUMN_NAME, IS_NULLABLE, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'KHKLagerorte' AND COLUMN_NAME LIKE 'USER[_]%';`
  `SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS Kollation;`
  Erwartet: `KHKArtikelVariantenLagerplatz` hat Spalten `PlatzID` und `Mandant` (AKE belegt P-9a; ~~„`Mandant` und `Lagerkennung`“~~ überholt –
  die Prüfung 50004 joint über `PlatzID`, S1); `SWF` vorhanden; keine `USER_`-Spalte `NO` ohne Default; Kollation `…_CI_…` (dann gelten
  Lagerkennung/Bezeichnung ohne Groß/Klein wie im WMS-Abgleich).
- **P-10 Indizes `KHKLagerplaetze`** (nur Information für ADR 0036, S4; kein Schalter hängt daran):
  `SELECT i.name, i.is_unique, c.name AS Spalte, ic.key_ordinal FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID('dbo.KHKLagerplaetze') ORDER BY i.name, ic.key_ordinal;`
  Zeigt, ob die Prüfung `Kurzbezeichnung = @Kurz` einen Index nutzen kann (sonst Scan mit Bereichssperren, hingenommen).

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `SageLagerplatzAnlageAktiv` (ServiceSetting, in `ServiceSettingDefinitions.All` + `Meta`, Drift-Guard) | Bool | Ja/Nein (Auswahl, Server validiert) | `false` | An: Web zeigt „Lagerplätze in Sage anlegen“ und „Erneut senden“; der Dienst legt erfasste Plätze in Sage an. Aus: Formular-Knopf weg, Dienst sendet nichts, offene Einträge bleiben unverändert stehen. | Web: Stammdaten → Lagerplätze; Dienst: Sage-Buchungs-Worker, Lauf „Lagerplätze → Sage“ | beide (AKE und IDEAL, Freigabe-Antwort 4); wirkt je Standort-DB, an jedem Standort einzeln einschalten | „Schreibt direkt per SQL in die Sage-Tabellen KHKLagerplaetze, KHKLagerorte und KHKTan (ohne Sage-Oberfläche, ohne SData). Angelegte Plätze und Lager löscht das WMS nicht. Nur nach bestandenem Abnahmetest am Testmandanten dieses Standorts, nach dem Rechte-GRANT und nach jedem Sage-Update erst nach den Prüfabfragen der Spec einschalten.“ |
| `SageLagerAnlageLagertechniken` (ServiceSetting, `SettingType.Choice` wie `ServiceSettingDefinitions.cs:347`, in `All` + `Meta`, Drift-Guard) | Auswahl | `Aus` = keine Lager-Anlage, nur Plätze in vorhandenen Lagern; `Mitschnitt` = `LTD0ST`, `LTD1BL` (Lager-Anlage im Sage-Mitschnitt belegt, Anhang 4.1: Test-BAB2, Test-BAB4); `Alle` = `LTD0ST`, `LTD1BL`, `LTD1UF`, `LTD2SF`, `LTD3HR` (Anhang 4.4). Server validiert auf diese drei Werte | `Aus` | Legt fest, ob und für welche Lagertechniken „Neues Lager erfassen“ angeboten wird (F1a) und welche der Dienst sendet (F3a Punkt 1). Wirkt nur bei `SageLagerplatzAnlageAktiv` = an | Web: Formular „Lagerplätze in Sage anlegen“; Dienst: Lauf „Lagerplätze → Sage“ | beide, je Standort-DB | „Legt neue Lager direkt in Sage an (KHKLagerorte + Basisplatz). `Mitschnitt` erst nach bestandenem Abnahmevergleich der Lager-Anlage am Testmandanten dieses Standorts; `Alle` erst, wenn der Abnahmevergleich auch für LTD1UF/LTD2SF/LTD3HR 0 Abweichungen zeigt. `Aus` lassen, wenn KHKLagerorte ein USER_-Pflichtfeld ohne Default hat (P-1/P-9).“ |
| ~~`ConnectionStrings:SageSchreibConnection`~~ | – | – | – | **Überholt** (Freigabe-Antwort 2): kein eigener Connection String; der Schreibweg nutzt `ConnectionStrings:SageConnection` (unverändert, Rechte per GRANT) | – | – | – |

Kein AppSetting. Die Bool-Lesart „leer = Default“ gilt wie bei den anderen Schaltern; im Dienst den Schalter **nicht** mit der Safe-Variante
lesen (`SageBookingWorker.cs:267-268`).

## Audit-Feld-Auswirkungen

- WMS-Neuanlage (F1a, F2): `CreatedAt/By/ByWindows` aus `ICurrentUserService` an Lager (`StorageArea`), Platz und Outbox (Outbox übernimmt
  die Werte vom Datensatz, `SageStammdatenOutbox.cs:24-26`).
- Dienst an Lager-Einträgen (F3a): nur die Outbox bekommt `ModifiedAt/By/ByWindows`; das `StorageArea` ändert der Dienst nicht (später der
  Abgleich mit `system:sync`, `LagerplatzSyncService.cs:357-359`). Die neue Ausnahme in der Fehlend-Schleife ändert nichts (kein Schreiben).
- Dienst (F3): `ModifiedAt = DateTime.Now` (Lokalzeit, ADR 0025), `ModifiedBy = "system:sage-lagerplatz-anlage"`, `ModifiedByWindows =
  Environment.MachineName` an Platz und Outbox, bei jeder Statusänderung (auch `Gesendet` und technischer Fehler).
- „Erneut senden“ (F6) und Bearbeiten (F7): `ModifiedAt/By/ByWindows` des Anwenders an Platz und Outbox.
- Sage-Seite: Der Mitschnitt zeigt keine Benutzerspalten in `KHKLagerplaetze`/`KHKLagerorte` (Anhang 4.2, 5.2); nachvollziehbar ist der
  WMS-Vorgang über Outbox (`CreatedBy`, `ModifiedAt`, `AttemptCount`, `LastError`) und das Aktivitäts-Protokoll mit PlatzID.

## Akzeptanzkriterien

1. Schalter aus (Default): Lagerplatzliste, „Neuen Lagerplatz anlegen“ und Hinweistext unverändert wie heute; kein Knopf „Lagerplätze in Sage
   anlegen“; `GET/POST CreateInSage` liefert eine `WarningMessage` „ausgeschaltet“ und legt nichts an; der Dienst schreibt nichts nach Sage
   (Lauf „Lagerplätze → Sage“ erscheint nicht im Protokoll).
2. Schalter an, Benutzer ohne Rolle `admin`/`masterdata` (z. B. nur `masterdata_read`): kein Knopf; direkter Aufruf von `CreateInSage` bzw.
   „Erneut senden“ (Platz und Lager) → AccessDenied.
3. Bestehendes `Create` ignoriert `StorageAreaId` aus dem POST (Test mit manipuliertem Formular: Platz hat danach `StorageAreaId = null`).
4. `CreateInSage` mit Lagerort L (Kennung `TB`), D1 1–2, D2 1–3, D3 0–0 legt 6 Plätze `TB;1;1;0` … `TB;2;3;0` an, alle `Source = Manual`,
   `StorageAreaId` = L, `Zone = TB`, `SageUebergabeStatus = Offen`, je ein Outbox-Eintrag `StorageLocation/<Id>` Status `Offen`; Reihenfolge der
   Outbox-Ids = D1 außen, D3 innen.
5. Existiert `TB;1;2;0` im WMS schon – auch als `tb;1;2;0` (H3) –, werden 5 angelegt und 1 gemeldet („übersprungen, Code im WMS schon
   vorhanden: TB;1;2;0“), keine Ausnahme beim Speichern.
6. Mehr als 200 Koordinaten oder D1 < 1 oder ein Wert > 9999 oder von > bis: Formularfehler, nichts angelegt.
7. Ohne aktive `StorageAreas`: Formular zeigt den Hinweis aus F1 statt der Auswahl, Absenden nicht möglich.
8. Dienst, Schalter an, Testmandant: für jeden Eintrag aus AK 4 entsteht in Sage genau ein Platz; WMS-Platz danach `Source = Sage`,
   `SageLagerplatzId` = Sage-PlatzID, `SageLagerkennung = Code`, `Description` = Sage-Platzbezeichnung, Status `Uebergeben`, `SageUebergabeAm`
   gesetzt; Outbox `Bestaetigt`; Protokoll „Lagerplätze → Sage“ mit Count `angelegt = 6`.
9. Sage-PlatzIDs kommen aus `spSysTan`: `KHKTan.Tan` ist nach dem Lauf um 6 höher; die IDs steigen in der Reihenfolge aus AK 4.
10. `KHKLagerorte` von `TB` nach dem Lauf: `AnzahlDimension1/2/3`, `AnzahlLagerplaetze` (inkl. Basisplatz) = MAX/COUNT aus `KHKLagerplaetze`,
    `IstPlatzverwaltet = -1`.
11. Abnahmetest (TS Schritt .k): identische Plätze in zwei identischen Lagern, einmal über den Sage-Dialog, einmal über das WMS – der
    spaltenweise Vergleich liefert in beide Richtungen 0 Abweichungen für `KHKLagerplaetze` und `KHKLagerorte`.
12. Idempotenz: Existiert der Platz in Sage schon mit gleichen Koordinaten und gleicher Kurzbezeichnung, schreibt der Dienst nichts in Sage
    (`KHKTan` unverändert), übernimmt den WMS-Platz mit dieser PlatzID und protokolliert „war in Sage schon vorhanden – übernommen“.
13. Konflikt: Koordinaten in Sage mit anderer Kurzbezeichnung belegt (oder umgekehrt, oder mehrfach) → nichts geschrieben, WMS-Platz Status
    `Fehler`, Spalte „Übergabe-Fehler“ nennt PlatzID/Kurzbezeichnung, Outbox `Fehler`, Warnung im Protokoll.
14. Fachliche Ablehnungen sichtbar mit Klartext, nichts in Sage geschrieben: Lager inaktiv (50013), Koordinaten passen nicht zur Lagertechnik
    (50012, Text nennt die Dimensionen der Technik), Lagertechnik ohne Dimensionen (50011), Platzbezeichnung > 110 (50015), Lager nicht
    gefunden bzw. ohne/mit unbekannter Lagertechnik (50010, Text nennt beides, H5), Code passt nicht zum Muster.
15. Technischer Fehler (fehlendes Recht des Dienstkontos, Verbindung weg): kein Teilschreiben in Sage; Outbox wieder `Offen`, Platz `Offen` mit
    „Letzter Versuch fehlgeschlagen: …“; über 10 Takte mit gleichem Fehlertext genau **ein** fehlgeschlagener Lauf im Protokoll und genau
    **ein** `NotifyErrorAsync`-Aufruf (Unit-Test mit Mock-Notifier); ein geänderter Text erzeugt einen weiteren Lauf und eine weitere Mail; nach
    Behebung läuft der Eintrag ohne Zutun durch und der Merker ist zurückgesetzt.
16. Hängender `Gesendet`-Eintrag (simuliert: Sage-Platz angelegt, WMS-Speichern nicht erfolgt) wird nach `Sync:SageLagerbuchungStuckMinutes`
    erneut verarbeitet und als „vorhanden“ übernommen – keine zweite Sage-Zeile.
17. Rücklesen (S6, prüfbar): Nach der Übergabe tragen die Plätze `SageIstGesperrt`/`SageIstGesperrtInventur`/Maße aus der Sage-Zeile. Ein
    Lagerplatz-Abgleich danach – mit Quelle SData **und** mit Quelle View – ändert sie nicht: Prüfabfrage auf der WMS-DB
    `SELECT COUNT(*) FROM StorageLocations WHERE Code LIKE '<Kennung>;%' AND (ModifiedBy <> 'system:sage-lagerplatz-anlage' OR Source <> 'Sage')`
    liefert 0 (`Source` ist ein String, `StorageLocationSource.cs:5-6`), `COUNT(*) … GROUP BY Code HAVING COUNT(*) > 1` liefert
    0 Zeilen, keine Warnung „Konflikt: Lagerplatz <Code>“ im Abgleich-Lauf; `IstBuchbar`/`SageBuchungErlaubt` wie erfasst. Unit-Test: Abgleich
    mit DTO = Befehlswerten → `updated = 0` für diese Plätze.
18. Alt-Einträge (Status Offen, ohne `StorageAreaId`) und inaktive Sage-Anlage-Plätze sendet der Dienst nicht; ihr Outbox-Eintrag bleibt
    unverändert.
19. „Erneut senden“ bei Status `Fehler`: Platz und Outbox `Offen`, Fehlertext leer, `AttemptCount = 0`, Audit des Anwenders; der nächste Takt
    verarbeitet den Eintrag.
20. Bearbeiten eines Sage-Anlage-Platzes vor der Übergabe: Code/Bezeichnung/Zone nicht änderbar, Deaktivieren und Buchbar-Schalter speicherbar,
    kein Fehler „max. 12 Zeichen“.
21. Hinweistext der Liste unterscheidet Schalter an/aus (F6); neue Spalte „Übergabe-Fehler“ mit `data-col-key`, Server-Spaltenfilter
    (`ColumnMap`), Eintrag in `column-config`; Pagination und Filter bleiben über Seitenwechsel erhalten.
22. Regression (S8, je Standort AKE und IDEAL), jeweils prüfbar:
    (a) Abgleich mit Quelle SData: ein Sage-Lager **ohne** Lager-Outbox-Eintrag, das nicht geliefert wird, bekommt weiter `SageMissingSince` und
    `IsActive = false` (Unit-Test Fehlend-Schleife);
    (b) Schalter **an**: „Neuen Lagerplatz anlegen“ erzeugt weiter Status `Offen` + Outbox ohne `StorageAreaId`, der Dienst sendet ihn nicht (AK 18),
    die E9-Übernahme beim Abgleich (`LagerplatzSyncService.cs:174-189`) wirkt wie heute;
    (c) `SageStammdatenOutbox.ErledigenAsync` bestätigt `BdeOperator`-Einträge (`Offen`/`Fehler`) wie bisher (Unit-Test);
    (d) der neue Spalten-Key ist in `ColumnMap` **und** `ColumnFilterTypes.For("StorageLocations/Index")` (`StorageLocationsController.cs:76`)
    eingetragen, bestehende Listen-Tests grün;
    (e) bestehende `LagerplatzSdataTests` unverändert grün;
    (f) Schalter aus: Lagerplatz-Neuanlage, Lagerplatz-Abgleich, Sage-Lagerbuchung unverändert, kein Lauf „Lagerplätze → Sage“.
23. ADR 0036 liegt im Hauptcheckout mit den Pflichtinhalten aus dem Lösungsentwurf; `fallstricke.md` bekommt den Eintrag „Direkt-SQL in Sage:
    nach Sage-Update erst P-1 bis P-3 (P-9), dann Schalter an“ mit Warum.
24. `SageLagerAnlageLagertechniken = Aus`: kein Eintrag „Neues Lager erfassen“, Hinweis „Lager-Anlage … nicht freigegeben“; ein POST mit
    Lager-Feldern wird abgelehnt, nichts angelegt. `Mitschnitt`: Auswahl nur `LTD0ST`, `LTD1BL`; ein POST mit `LTD2SF` → Formularfehler.
    Ungültiger Wert in `/ServiceSettings` → Speichern abgelehnt.
25. Lager-Neuanlage im Formular: Kennung leer, > 10 Zeichen, mit `;`, Leerzeichen oder anderem Zeichen außer A–Z/a–z/0–9/`-`/`_` (H10), schon in `StorageAreas` (auch in anderer Groß/Kleinschreibung), oder
    `<Kennung>;0;0;0` schon als Platz-Code vorhanden (AKE: `Hauptlager`) → Formularfehler, nichts angelegt. Bezeichnung leer/> 40 → Fehler.
26. Gültige Lager-Neuanlage `TL-WMS` (`LTD2SF`, platzverwaltet, Reihe 1–2 × Platz 1–2): ein `StorageArea` (Audit des Anwenders, aktiv), ein
    Outbox-Eintrag `StorageArea/<Id>` `Offen`, vier Plätze mit `StorageAreaId` = neues Lager und je ein Outbox-Eintrag; alles in **einer**
    Transaktion (S2): scheitert ein Teil (Unit-Test: Ausnahme beim zweiten `SaveChanges` bzw. Unique-Verletzung am relationalen Provider), ist
    **nichts** angelegt – kein `StorageArea`, kein Platz, kein Outbox-Eintrag – und das Formular zeigt den Fehler. Ohne Platzverwaltung: nur
    `StorageArea` + Lager-Eintrag. Bei `LTD0ST` ist Platzverwaltung gesperrt. (Die InMemory-Tests prüfen den Ablauf; die echte Atomarität am
    relationalen Provider zeigt TS-192 Schritt .p per SQL: `StorageAreas`/`StorageLocations`/`SageStammdatenOutbox` gleich viele neue Zeilen.)
27. Dienst: das Lager wird vor seinen Plätzen gesendet; in Sage entstehen genau eine `KHKLagerorte`-Zeile (`WarenflussKZ = 'SWF'`, alle
    `IstWE/IstHauptWE/IstWA/IstHauptWALager = 0`, `Aktiv = -1`, `IstDispoLager = -1`, `AnzahlLagerplaetze = 1` vor den Plätzen) und der Basisplatz
    `TL-WMS;0;0;0` mit Platzbezeichnung = Lagerbezeichnung; `KHKTan` steigt um 1 für das Lager. Lager-Outbox `Bestaetigt`; danach die Plätze wie AK 8,
    `KHKLagerorte` danach mit Zählern nach AK 10 und `IstPlatzverwaltet = -1`. Protokoll `lager_angelegt = 1`.
28. Solange der Lager-Eintrag nicht `Bestaetigt` ist, sendet der Dienst keinen Platz dieses Lagers (Plätze bleiben `Offen`, unverändert).
29. Lagerplatz-Abgleich **vor** der Übergabe: das neue Lager bleibt `IsActive = true`, `SageMissingSince = null`; **nach** der Übergabe: kein
    zweites `StorageArea`, kein „aktualisiert“ für dieses Lager; der Basisplatz `TL-WMS;0;0;0` wird genauso behandelt wie die Basisplätze
    vorhandener Sage-Lager (keine Sonderbehandlung im Abgleich; neu angelegte Sage-Plätze sind nicht buchbar, `LagerplatzSyncService.cs:146`).
30. Lager-Idempotenz: existiert die Kennung in Sage mit gleicher Bezeichnung/Technik und aktiv → nichts geschrieben (`KHKTan` unverändert),
    „vorhanden – übernommen“; mit anderer Bezeichnung oder Technik oder inaktiv → Lager-Outbox `Fehler`, Hinweis über der Lagerplatzliste mit
    Klartext und „Erneut senden“; die Plätze bleiben `Offen`.
31. Lager-Fehler 50005 (Basisplatz-Kurzbezeichnung vergeben), 50004 (Artikel-Zuordnungen an Plätzen dieser Kennung, Join über `PlatzID`, S1),
    50007 (Plätze dieser Kennung ohne Lager), 50001 (Technik unbekannt), 50006 (Technik nicht freigegeben und Lager in Sage nicht vorhanden),
    515/207 (Pflicht-/fremde Spalte) → fachlicher Fehler mit Klartext, nichts in Sage, kein Wiederholen ohne „Erneut senden“. Der Lager-Befehl
    kompiliert gegen `ake_TEST2026` (kein 207 für `KHKArtikelVariantenLagerplatz`; TS-192 Schritt .q).
32. Das WMS schreibt an einem bestehenden Sage-Lager nie `Bezeichnung`, `Lagertechnik`, `WarenflussKZ` oder WE/WA-Kennzeichen (Abnahmevergleich
    und Mitschnitt/Diff des Befehls: nur Zähler-Spalten im UPDATE).
33. Platz-Konflikt mandantenweit: existiert die Kurzbezeichnung des neuen Platzes in Sage in einem **anderen** Lager → Konflikt 50014, nichts
    geschrieben.
34. Fehlerklassen (S3): `IstTechnisch` liefert `true` genau für die Liste in F3 (Unit-Test je Nummer inkl. `Class >= 20`) und `false` u. a. für
    `8152`, `2628`, `220`, `8115`, `245`, `547`, `2627`, `515`, `207`, `50001–50019`. Service-Test: Mock liefert für Eintrag A `Abgelehnt`
    (z. B. aus 8152), für B `Angelegt` → A `Fehler`, B im **selben** Takt `Uebergeben`.
35. Kein Lauf ohne Arbeit (S7): Schalter an, keine Kandidaten bzw. nur Plätze eines wartenden oder fehlerhaften Lagers → 10 Takte, 0 neue
    Läufe „Lagerplätze → Sage“ im Protokoll (Unit-Test über `ISyncLogger`-Mock; im TS per `SELECT COUNT(*) FROM SyncLogs …`).
36. Sperren und Ergebnis (S4, S5): beide Befehle beginnen mit `SET DEADLOCK_PRIORITY LOW`; alle String-Parameter `SqlDbType.VarChar` mit Länge
    (Code-Review/Unit-Test auf die `SqlParameter`-Liste); das Ergebnis-`SELECT` steht nach dem `COMMIT` (Code-Review).
37. Lager mit Lager-Outbox `Fehler`: in der Lagerort-Auswahl als „(Fehler – erst ‚Erneut senden‘)“, nicht wählbar; ein POST mit diesem Lager →
    Formularfehler, nichts angelegt (S10).
38. `ErledigenAsync` setzt zusätzlich `Gesendet` → `Bestaetigt`; `Offen`/`Fehler` wie bisher (Unit-Test, auch Typ `BdeOperator`).
39. H4: Lager-Eintrag, dessen Technik nach einem Konflikt nicht (mehr) freigegeben ist, das Lager in Sage aber mit gleichen Werten existiert →
    „Erneut senden“ führt zu „vorhanden – übernommen“ (Mock: `neuanlageErlaubt = false` wird übergeben, Ergebnis `Vorhanden`).
40. Jeder Lauf mit Arbeit enthält genau eine Info „Sage-Datenbank: <Server>/<DB>“; auf DEV AKE lautet sie auf `…/ake_TEST2026`
    (Freigabe-Antwort 8, TS-192 Vorbedingung V3).

Unit-testbar (InMemory + Mock `ISageLagerplatzAnlage`): AK 1–8 (WMS-Seite), 12–19 (Status-Automat, Übernahme, Wiederholung, Einmal-Meldung),
22 (a–e), 24–26, 28, 29 (Fehlend-Schleife, Muster `LagerplatzSdataTests.cs`), 30–31 (Status-Automat Lager mit gemocktem Ergebnis), 34, 35,
37–39. Der SQL-Befehl selbst (AK 9–11, 13–14, 27, 30–33 Sage-Seite, 36 nur per Code-Review, 40) ist **nicht** InMemory-testbar → Manual-UAT am
Testmandanten (fallstricke „Tests, EF, SQL Server“). UNIQUE auf `Code` und `Lagerkennung` erzwingt InMemory nicht: die Prüfungen in F1a/F2 sind
App-Code und werden getestet. Die Transaktion (AK 26) ist unter InMemory ein No-op (`IsRelational`-Guard) – die Atomarität belegt TS-192 .p.

## Test-Szenarien

Neues Kapitel **TS-192** in `docs/TESTSZENARIEN.md` (H2: TS-188 ist im Bündel vergeben, nächste freie 189; der Dev-Lauf prüft erneut)
„Lager und Lagerplätze im WMS erfassen und in Sage anlegen (Direkt-SQL)“, Index in `secondbrain/tests/testszenarien-index.md`.
Standort: **beide** – das Szenario läuft an jedem Standort am Testmandanten vollständig durch, bevor dort eingeschaltet wird.

**Vorbedingungen je Standort:**
- **V1 IDEAL:** WMS-Testsystem `idealakeweb01.ideal.ideal-ake.at:88` (greift auf `IDEAL_TEST_2026_05_03` auf `idealsql2\SAGE100TEST` zu,
  Freigabe-Antwort 5); vor dem Test V3 für dessen Dienst. IDEAL-Prüfung P-1 bis P-9 erledigt (Voraussetzung **vor dem Einschalten**, Abschnitt
  Migrations-/SQL-Auswirkungen). Die Sage-seitigen Testlager legt **Benjamin** an (nur IDEAL; AKE siehe V2: Gerald, in `ake_TEST2026`).
- **V2 AKE (Freigabe-Antwort 7):** **DEV AKE** – Web `http://localhost:8081`, Dienst `AKE_WMS_DEV_Service`, DB `AKE_WMS_DEV` (Instanz `.`),
  Sage-Lesung/-Schreibung über `D:\Dev\AKE\Service\appsettings.DEV.json:4` → `ake_TEST2026` auf `AKESQL20.ake.at`. Die Sage-seitigen Testlager
  (Paar 1 komplett, Paar 2 `TL-SAGE`, Inventur- und Technik-Paare) legt **Gerald** an. P-4 für das Konto des DEV-Dienstes (`sc qc
  AKE_WMS_DEV_Service` → `SERVICE_START_NAME`) nach dem GRANT, P-8 (DEV-DB), P-9 erledigt (AKE belegt).
- **V3 Beleg Sage-Ziel (harte Vorbedingung für jedes Einschalten von `SageLagerplatzAnlageAktiv` auf DEV/Testsystem, Freigabe-Antwort 8):**
  (1) Umgebung des Dienstes ermitteln (`reg query HKLM\SYSTEM\CurrentControlSet\Services\<Dienst> /v Environment` → `DOTNET_ENVIRONMENT`
  bzw. `ASPNETCORE_ENVIRONMENT`) und daraus `appsettings.<Umgebung>.json` → `ConnectionStrings:SageConnection` (Server, Database). Hintergrund
  (Kritische Prüfung B2): das `appsettings.json` des DEV-AKE-Dienstes zeigt auf Produktiv `ake`; ohne Umgebungs-Überlagerung schriebe der Dienst
  dorthin. (2) **Per SQL belegen:** `sqlcmd -S <Server aus 1> -d <Database aus 1> -E -C -Q "SELECT @@SERVERNAME AS Server, DB_NAME() AS Db"` –
  `Db` muss `ake_TEST2026` bzw. mit `IDEAL_TEST_` beginnen, **nie** `ake` oder eine IDEAL-Produktiv-DB. Ergebnis (Ausgabe) in den QA-Nachweis.
  Scheitert (1) oder (2) oder ist das Ziel nicht eindeutig eine Test-DB: Schalter bleibt aus, alle Sage-schreibenden Schritte NICHT MOEGLICH.
  (3) Nach dem ersten Takt mit Arbeit: `SELECT TOP 1 Message FROM SyncLogs WHERE Message LIKE N'Sage-Datenbank:%' ORDER BY Id DESC` (DEV-DB) muss
  dieselbe Test-DB nennen (AK 40); sonst Schalter sofort aus, Befund.
- **V4 Rechte vor .l (H6):** P-4 für das Dienstkonto auf der Test-DB ausführen. Sind `InsPlatz`/`InsOrt`/`UpdOrt`/`ExecTan` schon 1 (z. B.
  `db_datawriter`), entfällt .l (im Nachweis vermerken). Sonst GRANT erst **nach** Schritt .l (der Schritt prüft den Fehlerfall ohne Recht).
- **Paar 1 (Platz-Anlage):** in Sage zwei Lager mit identischen Eingaben, z. B. `TS-SAGE` und `TS-WMS`, Bezeichnung gleich lang, Lagertechnik
  `LTD2SF`; zusätzlich ein Paar `LTD3HR`. **Inventurdaten (H7):** in einem **vorhandenen** platzverwalteten Lager, dessen Basisplatz
  Inventurdaten trägt (finden: `SELECT o.Lagerkennung FROM KHKLagerorte o JOIN KHKLagerplaetze p ON p.Mandant = o.Mandant AND p.Lagerkennung =
  o.Lagerkennung AND p.Dimension1 = 0 AND p.Dimension2 = 0 AND p.Dimension3 = 0 WHERE o.Mandant = 1 AND o.IstPlatzverwaltet = -1 AND
  p.LetztesInventurjahr IS NOT NULL;` – IDEAL laut Anhang 5.2 z. B. `LIFT1`), je **einen** freien Platz per Sage-Dialog und per WMS anlegen
  und nur diese zwei Zeilen vergleichen (Abnahmevergleich mit `WHERE PlatzID IN (…)` statt Lagerkennung).
- **Paar 2 (Lager-Anlage):** in Sage nur `TL-SAGE` über den Lagerstamm anlegen (Bezeichnung `Testlager L`, `LTD2SF`, WarenflussKZ `SWF`, sonst
  Vorgaben); das Gegenstück `TL-WMS` mit derselben Bezeichnung legt das WMS an (Schritt .p). **Je Lagertechnik der Zielstufe ein Paar (H8):**
  `Mitschnitt` = `LTD0ST`, `LTD1BL`; `Alle` = zusätzlich `LTD1UF`, `LTD2SF`, `LTD3HR`. Eine Stufe gilt an einem Standort erst als abgenommen,
  wenn .s für **jedes** ihrer Paare 0 Abweichungen zeigt.
- Lager in `StorageAreas`: Lagerplatz-Abgleich mit Quelle SData einmal gelaufen (Mensch, siehe QA-Regel), **oder** – für den QA-Lauf – die
  Test-Lager per SQL in der DEV-DB angelegt (`INSERT StorageAreas` mit Lagerkennung/Bezeichnung/Lagertechnik/`IstDispoLager` wie in Sage, Audit
  `qa-agent`; Testdaten in der DEV-DB sind erlaubt, `qa-agent.md:86`). Benutzer A mit Rolle `masterdata`, Benutzer R nur `masterdata_read`;
  `SageLagerAnlageLagertechniken` je Schritt wie angegeben (nur am Test-/DEV-System).

**QA-Regel (Freigabe-Antwort 8 „Prüfungen gerne per SQL selbst aufrufen“ – Auslegung, so festgelegt im Auftrag Gerald 2026-11-24):**
- Der `qa-agent` **darf** die Prüf- und Abnahmeabfragen selbst per `sqlcmd … -E -C` ausführen: nur `SELECT` (P-1 bis P-10, Abnahmevergleiche
  .k/.s, `KHKTan`-Stand, Ergebniskontrollen der Schritte) gegen die Sage-**Test**-DBs `ake_TEST2026` (`AKESQL20.ake.at`) und, sofern erreichbar,
  `IDEAL_TEST_2026_05_03`. Das ist eine auf diese Spec begrenzte Ausnahme von `qa-agent.md:87` („never touch AKESQL20 … or Sage“).
- Er **darf** auf DEV (AKE `localhost:8081`, IDEAL `localhost:8082`) `SageLagerplatzAnlageAktiv` und `SageLagerAnlageLagertechniken` einschalten –
  **nur nach bestandenem V3** – und stellt **nach dem Test beide wieder auf den Default** (`false` bzw. `Aus`), Beleg per `SELECT [Key],[Value]
  FROM ServiceSettings WHERE [Key] IN (N'SageLagerplatzAnlageAktiv', N'SageLagerAnlageLagertechniken')` im Nachweis. In Sage schreibt dabei nur der
  DEV-Dienst (die Software unter Test).
- Er **darf nicht:** Abfragen oder Schalter gegen Produktiv (`ake`, IDEAL-Produktiv) – Produktiv nie durch QA; Schreibanweisungen per sqlcmd in
  Sage (kein INSERT/UPDATE/DELETE, kein GRANT, nicht den Befehl aus .v); `Sync:*Enabled` einschalten (`qa-agent.md:84-85` gilt weiter).
- Daraus folgt: **Nur Mensch** (für QA NICHT MOEGLICH) sind die Sage-Dialog-Schritte (.e, Sage-Teil von .h/.j/.r/.u, Anlage der Paare), der
  Lagerplatz-Abgleich (.f, Abgleich-Teil von .p/.r/.u; braucht `Sync:LagerplaetzeEnabled`), der GRANT (.l zweiter Teil) und .v (DBA). Alle
  übrigen Schritte darf QA nach V3 ausführen. Am IDEAL-Testsystem `idealakeweb01:88` schaltet nur der Mensch.
- Empfehlung (außerhalb des Repos, Mensch): `SageLagerplatzAnlageAktiv = false` in das DEV-Sicherheits-SQL von `D:\Dev\scripts\dev-deploy.ps1`
  aufnehmen, damit ein erneuter DEV-Deploy den Schalter sicher zurücksetzt.

| Schritt | Aktion | Erwartet |
|---|---|---|
| .a | Schalter aus, Lagerplatzliste öffnen | wie heute, kein Knopf „Lagerplätze in Sage anlegen“, Hinweistext „Das WMS sendet nichts an Sage“ |
| .b | Schalter an; Benutzer R | kein Knopf; URL `/StorageLocations/CreateInSage` → Zugriff verweigert |
| .c | Benutzer A: Lagerort `TS-WMS`, D1 1–3, D2 1–2, D3 0–0 | 6 Plätze `TS-WMS;1;1;0` …, Status Offen, Outbox 6 Zeilen Status 0 |
| .d | Dienst-Takt abwarten (≤ 1 min) | alle 6 „Übergeben“, Quelle Sage, Spalte Übergabe-Fehler leer; Protokoll „Lagerplätze → Sage“ angelegt = 6 und Info „Sage-Datenbank: …/ake_TEST2026“ (V3 Teil 3); `SageIstGesperrt`/Maße der 6 Plätze gesetzt (S6) |
| .d2 | Schalter an, nichts zu erfassen, 10 Takte (≈ 200 s) warten | `SELECT COUNT(*) FROM SyncLogs WHERE Service = N'<Wert von SyncLogServices.SageLagerplatzAnlage>' AND Timestamp >= '<Schrittbeginn>'` = 0 (AK 35; Spalten `SyncLog.cs:7-29`) |
| .e | In Sage `TS-SAGE` dieselben Plätze über den Dialog „Lagerplätze anlegen“ anlegen (Mensch) | – |
| .f | Lagerplatz-Abgleich starten (Mensch), einmal mit Quelle SData, einmal mit Quelle View | `TS-SAGE`-Plätze neu im WMS; für `TS-WMS` liefert die Prüfabfrage aus AK 17 0, keine Dublette, keine Konflikt-Warnung |
| .g | Denselben Bereich für `TS-WMS` nochmals erfassen | alle 6 übersprungen („Code im WMS schon vorhanden“) |
| .h | In Sage `TS-WMS;3;3;0` von Hand anlegen, dann im WMS D1 3–3, D2 3–3 erfassen | Dienst: „war in Sage schon vorhanden – übernommen“, `KHKTan` unverändert |
| .i | D1 1–1, D2 1–1, D3 1–1 für `TS-WMS` (2-dimensional) | Status Fehler, Text „Koordinaten passen nicht zur Lagertechnik (Reihe, Platz)“, nichts in Sage |
| .j | Lager `TS-WMS` in Sage kurz inaktiv setzen, Platz `TS-WMS;3;1;0` erfassen; dann Lager wieder aktiv, „Erneut senden“ | erst Fehler „Lager ist in Sage inaktiv“, nichts in Sage; nach „Erneut senden“ Offen → nächster Takt Übergeben. Fehler-Platz aus .i deaktivieren: wird nie gesendet, Zähler offener Übergaben sinkt |
| .k | **Abnahmevergleich** (SQL unten) | beide Richtungen 0 Zeilen für `KHKLagerplaetze` und `KHKLagerorte` |
| .l | **Zuerst, vor dem GRANT** (Reihenfolge siehe V4; entfällt, wenn das Konto schon Schreibrecht hat): Schalter an, zwei Plätze erfassen, Dienstkonto noch ohne Schreibrecht, mindestens 10 Takte warten | beide Plätze Offen, „Letzter Versuch fehlgeschlagen: …“ (Rechte-Fehler 229), über die 10 Takte genau **ein** fehlgeschlagener Lauf im Protokoll (und höchstens eine Fehlermail, falls Mails an sind), nichts in Sage (`KHKTan` unverändert); DBA vergibt GRANT (Mensch) → nächster Takt läuft durch. Danach .c ff. |
| .m | Dienst nach Sage-Commit hart beenden (Debug-Haltepunkt bzw. Prozess beenden), neu starten | nach Stuck-Minuten „vorhanden – übernommen“, keine zweite Sage-Zeile |
| .n | Schalter aus am jeweils anderen Standort (bzw. vor dem Einschalten): Lagerplatz anlegen, Lagerplatz-Abgleich, Ein-/Ausbuchung | unverändert |
| .o | `SageLagerAnlageLagertechniken = Aus` | kein „Neues Lager erfassen“, Hinweis „nicht freigegeben“; auf `Mitschnitt`: nur `LTD0ST`/`LTD1BL` wählbar |
| .p | Neues Lager `TL-WMS`, Bezeichnung `Testlager L`, `LTD2SF` (Stufe `Alle`), platzverwaltet, Reihe 1–2 × Platz 1–3; gleichzeitig Lagerplatz-Abgleich starten, bevor der Dienst sendet (Abgleich-Teil Mensch) | Lager im Auswahlfeld „(neu, wartet auf Sage)“, Hinweis „Lager TL-WMS wartet …“; der Abgleich setzt es **nicht** auf „nicht in Sage“; 6 Plätze Offen. Atomarität (S2) per SQL auf der WMS-DB: 1 neue `StorageAreas`-Zeile, 6 neue `StorageLocations` mit `StorageAreaId` = neues Lager, 7 neue `SageStammdatenOutbox`-Zeilen (1 × `StorageArea`, 6 × `StorageLocation`), alle `Status = 0` |
| .q | Dienst-Takt abwarten | Protokoll `lager_angelegt = 1`, dann `angelegt = 6`; in Sage `TL-WMS` mit Basisplatz `TL-WMS;0;0;0`; WMS-Plätze Übergeben; Hinweis weg; kein Fehler 207 (S1: Lager-Befehl kompiliert gegen die Test-DB, AK 31) |
| .r | In Sage für `TL-SAGE` dieselben Plätze über den Dialog anlegen; Lagerplatz-Abgleich | keine zweite `StorageArea` `TL-WMS`, kein Konflikt |
| .s | **Abnahmevergleich Lager** (SQL unten mit `@A = 'TL-SAGE'`, `@B = 'TL-WMS'`) für `KHKLagerorte` und `KHKLagerplaetze` | beide Richtungen 0 Zeilen; Abweichung (z. B. AKE `USER_Aussenlager` NULL vs. Sage-Wert) = Lager-Anlage an diesem Standort bleibt `Aus`, Befund in die Spec |
| .t | Neues Lager mit Kennung eines vorhandenen Sage-Lagers in anderer Schreibweise (z. B. `ts-sage`) sowie (AKE) Kennung `Hauptlager` | Formularfehler „Lagerkennung schon vorhanden“ bzw. „Platz-Code Hauptlager;0;0;0 schon vorhanden“, nichts angelegt |
| .u | Lager-Konflikt: im WMS `TL-KON` erfassen (Dienst-Schalter kurz aus), in Sage `TL-KON` mit anderer Bezeichnung anlegen (Mensch), Schalter an | Lager-Outbox Fehler, Hinweis „konnte nicht in Sage angelegt werden: … anders belegt“, Plätze Offen; im Formular erscheint `TL-KON` als „(Fehler – erst ‚Erneut senden‘)“ und ist nicht wählbar (S10, AK 37); Lagerplatz-Abgleich starten (Mensch; übernimmt Sage-Bezeichnung), „Erneut senden“ → „vorhanden – übernommen“, Plätze laufen durch |
| .v | (AKE, `ake_TEST2026`) Konfliktfall aus P-6 (`HAUPT;0;0;0` = `Hauptlager;0;0;0`): der DBA führt den Lager-Befehl in SSMS in einer Transaktion mit `ROLLBACK` und `@Lager = 'Hauptlager'`, `@NeuanlageErlaubt = 1` aus (Mensch) | `THROW 50005` „Kurzbezeichnung des Basisplatzes in Sage schon vergeben“ (der WMS-Weg ist schon durch .t gesperrt) |
| .w | Abschluss: Schalter auf DEV/Testsystem zurück (`SageLagerplatzAnlageAktiv = false`, `SageLagerAnlageLagertechniken = Aus`) und per SQL belegen; Testlager-Liste (alle `TS-*`, `TL-*`, Inventur-Paar-Plätze) an den Sage-Anwender zum Aufräumen geben (H11: das WMS löscht nie) | Schalter Default; Aufräum-Liste im Nachweis |

**Abnahmevergleich (Schritt .k, Anhang 9)** – gegen die Sage-Test-DB; Kennung und Bezeichnung werden in den Texten ersetzt, PlatzID,
Lagerkennung und Timestamp ausgenommen. Alle übrigen Spalten (auch `USER_`) kommen aus `INFORMATION_SCHEMA`:

```sql
DECLARE @A varchar(10) = 'TS-SAGE', @B varchar(10) = 'TS-WMS';
DECLARE @BezA varchar(40) = (SELECT Bezeichnung FROM KHKLagerorte WHERE Mandant = 1 AND Lagerkennung = @A),
        @BezB varchar(40) = (SELECT Bezeichnung FROM KHKLagerorte WHERE Mandant = 1 AND Lagerkennung = @B);
DECLARE @cols nvarchar(max) = (SELECT STRING_AGG(QUOTENAME(COLUMN_NAME), ',') FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'KHKLagerplaetze' AND COLUMN_NAME NOT IN ('PlatzID','Lagerkennung','Timestamp','Kurzbezeichnung','Platzbezeichnung'));
DECLARE @sql nvarchar(max) = N'
SELECT ' + @cols + N', REPLACE(Kurzbezeichnung, @A, ''#'') K, REPLACE(Platzbezeichnung, @BezA, ''#'') P FROM KHKLagerplaetze WHERE Mandant = 1 AND Lagerkennung = @A
EXCEPT
SELECT ' + @cols + N', REPLACE(Kurzbezeichnung, @B, ''#''), REPLACE(Platzbezeichnung, @BezB, ''#'') FROM KHKLagerplaetze WHERE Mandant = 1 AND Lagerkennung = @B;';
EXEC sp_executesql @sql, N'@A varchar(10), @B varchar(10), @BezA varchar(40), @BezB varchar(40)', @A, @B, @BezA, @BezB;
-- danach mit vertauschten @A/@B und @BezA/@BezB; dasselbe Muster für KHKLagerorte (ausgenommen Lagerkennung, Bezeichnung, Timestamp)
```

Für `KHKLagerorte` (Schritt .s) dasselbe Muster mit `TABLE_NAME = 'KHKLagerorte'`, ausgenommen `Lagerkennung`, `Timestamp`; `Bezeichnung` ist
im Paar gleich und wird mitverglichen. Für `KHKLagerplaetze` in Schritt .s ist `@BezA = @BezB` (gleiche Lagerbezeichnung).

**Negativfälle** zusammengefasst: Schalter aus, Lager-Anlage nicht freigegeben, fehlendes Recht (Rolle und Dienstkonto), leere Lagerort-Liste,
> 200 Koordinaten oder > 9999, WMS-Dublette (Platz auch in anderer Schreibweise, Lagerkennung, Basisplatz-Code), unzulässige Zeichen in der
Lagerkennung, Sage-Dublette/Konflikt (Platz mandantenweit, Lager), inaktives Lager, falsche Koordinaten, technischer Fehler (nur einmal gemeldet),
Abbruch nach Commit, Alt-Einträge ohne Lagerort, inaktiver Platz, Abgleich vor Übergabe, Lager mit Fehler nicht wählbar, Sage-Ziel nicht
belegbar (V3 → Schalter bleibt aus).

## Etappen (nur bei epic: true)

Kein Epic, **kein Split** (Entscheid Gerald 2026-11-24; Vorschlag S11 „zwei Teile“ bewusst abgelehnt): ein Dev-Lauf ist bei ~25–30 Dateien
machbar, und der Lager-Teil ist im Betrieb über `SageLagerAnlageLagertechniken = Aus` (Default) vollständig abschaltbar – Teil 1 „nur Plätze“
ist damit ohne zweiten Merge nutzbar. Damit der Lauf trotzdem in sauberen Schritten vorankommt, gilt die Commit-Reihenfolge **Plätze zuerst,
Lager danach** (jeder Schritt baut und testet grün):
1. Web-Erfassung Plätze (F1 ohne F1a, F2 mit `AddSageAnlageAsync`) + Überposting-Schutz + Bearbeiten (F7).
2. Interface + Dienst-Schritt Plätze (F3, Fehlerklassen, Merker S7) mit Mock-Tests.
3. `SqlSageLagerplatzAnlage.AnlegenAsync` (Platz-SQL).
4. Liste/„Erneut senden“ Platz/Hinweistexte (F6), Spalte „Übergabe-Fehler“.
5. Lager-Anlage: Einstellung `SageLagerAnlageLagertechniken`, Formular F1a, F3a, Lager-SQL, Fehlend-Schleife im Abgleich, Lager-Hinweis und
   „Erneut senden“ Lager, Sperre fehlerhafter Lager (S10).
6. Doku, ADR 0036, `fallstricke.md`, Testszenario TS-192, Manifest.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

**Final (QA 2026-11-25, aus dem echten Diff `6e426113..5b6fa696`, 37 Dateien):** `deploy.web = ja` (IdealAkeWms/), `deploy.service = ja`
(IDEALAKEWMSService/), `deploy.migration = nein` (keine Datei unter `*/Migrations/`, kein `SQL/`-Skript). Publish fertig in
`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (v1.99.71, Commit `47a23d32`, beide Komponenten + `SQL\` + `deploy\`,
`publish-info.txt`); beide Komponenten müssen deployt werden. Ablauf Mensch: Publish aus dem Worktree → Testsystem → Test → Merge. Nach dem
Merge nur dann neu aus `main` publishen, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat:
`dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und
`dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`.
Reihenfolge Produktiv: Deploy Web + Dienst mit beiden Schaltern **aus** → DBA-GRANT (unten) → P-4 aus dem Dienstkontext → Sage-DB-Backup →
erst dann einschalten. Die folgenden Punkte (Vorfassung des Spec-Agenten) sind geprüft und gelten weiter.

- **Web-App:** ja (Formular, Liste, Hinweistexte, „Erneut senden“, Hilfe)
- **Service:** ja (Worker-Schritt, Schreibstelle, Fehlend-Schleife im Lagerplatz-Abgleich); `appsettings.json` des Dienstes **unverändert**
  (kein neuer Connection String, Freigabe-Antwort 2)
- **Migration:** nein (Begründung im Abschnitt Migrations-/SQL-Auswirkungen)
- **DBA-Schritt je Standort und je Sage-DB (Test, dann Produktiv)** – Rechte für das Dienstkonto der `SageConnection` (AKE `AKENB\svc_idealakewms`;
  IDEAL: Konto des IDEAL-Dienstes, aus den Dienst-Eigenschaften ablesen; Testsystem `idealakeweb01:88`: dessen Dienstkonto; **DEV AKE**
  (Abnahme, Antwort 7): Konto von `AKE_WMS_DEV_Service`, `sc qc AKE_WMS_DEV_Service` → `SERVICE_START_NAME`, GRANT nur auf `ake_TEST2026`):
  ```sql
  USE [<SageMandantenDB>];   -- AKE: ake_TEST2026 (DEV AKE), dann ake; IDEAL: IDEAL_TEST_2026_05_03, dann Produktiv-DB
  -- Datenbank-Benutzer muss existieren (heute liest der Dienst schon; sonst: CREATE USER [<Konto>] FOR LOGIN [<Konto>];)
  GRANT INSERT ON dbo.KHKLagerplaetze TO [<Konto>];
  GRANT INSERT, UPDATE ON dbo.KHKLagerorte TO [<Konto>];
  GRANT EXECUTE ON dbo.spSysTan TO [<Konto>];   -- KHKTan ueber Ownership-Chaining (beide dbo, AKE P-4)
  -- nur falls P-4 SELECT = 0 zeigt (kein db_datareader):
  GRANT SELECT ON dbo.KHKLagerorte TO [<Konto>];  GRANT SELECT ON dbo.KHKLagerplaetze TO [<Konto>];
  GRANT SELECT ON dbo.KHKLagertechnikenArten TO [<Konto>];  GRANT SELECT ON dbo.KHKGruppen TO [<Konto>];
  GRANT SELECT ON dbo.KHKArtikelVariantenLagerplatz TO [<Konto>];   -- Join 50004 ueber PlatzID (S1, P-9a)
  ```
  Nachprüfung P-4 **aus dem Dienstkontext**. Am Testmandanten den GRANT erst nach Testschritt .l (entfällt laut V4, wenn schon Schreibrecht).
- **Reihenfolge je Standort:** Publish aus dem Worktree → Test-/DEV-System (Schalter aus; IDEAL `idealakeweb01.ideal.ideal-ake.at:88` auf
  `IDEAL_TEST_2026_05_03`, AKE **DEV AKE** auf `ake_TEST2026`, Freigabe-Antwort 7) → **V3 Beleg Sage-Ziel per SQL** (harte Vorbedingung, TS-192)
  → Prüfabfragen (IDEAL: P-1 bis P-9 vor dem Einschalten; AKE: P-4 nach GRANT, P-10 Information) → Schalter am Test-/DEV-System an,
  Testschritt .l → GRANT → Testszenario inkl. aller Abnahmevergleiche → Schalter zurück auf Default (.w) → Merge (Schranke 2). Produktiv:
  Deploy mit beiden Einstellungen **aus**; Einschalten je Standort erst nach P-1 bis P-7 (P-9) gegen die Produktiv-Sage-DB, GRANT + P-4 und
  Sage-DB-Backup. `SageLagerAnlageLagertechniken` nur auf die Stufe stellen, deren Abnahmevergleich am Testmandanten desselben Standorts 0
  Abweichungen zeigte (je Paar seiner Techniken, H8). ~~DPS-Abstimmung~~ entfällt (Freigabe-Antwort 6). Testlager in den Sage-Test-DBs räumt
  ein Sage-Anwender nach der Abnahme auf (H11, Liste aus TS-192 .w).
- **QA auf DEV (Freigabe-Antwort 8, Auslegung):** Der `qa-agent` führt nach dem DEV-Deploy die SELECT-Prüfungen gegen die Sage-Test-DBs selbst
  aus und schaltet `SageLagerplatzAnlageAktiv`/`SageLagerAnlageLagertechniken` auf DEV **nur nach V3** ein (SQL-Beleg: Sage-Verbindung des
  DEV-Dienstes zeigt auf `ake_TEST2026` bzw. `IDEAL_TEST_*`, nie `ake`/Produktiv); nach dem Test beide zurück auf Default und per SQL belegt.
  Produktiv nie durch QA; Sage-Dialog-, Abgleich-, GRANT- und DBA-Schritte bleiben beim Menschen (Liste in TS-192 „QA-Regel“).
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023; Texte nur ASCII, Drift-Test `NachlaufManifestDriftTests.cs:304-314`;
  Version = Version des Dev-Laufs):
  - `Id='v<X>-SageLagerplatzRechte'`, Typ `Info`, Firma `beide`, Phase `Vorher`, Ausfuehrung `anzeigen`, Text: „Lager-/Lagerplatz-Anlage in
    Sage: DBA vergibt dem Dienstkonto der SageConnection auf der Sage-Mandanten-DB GRANT INSERT KHKLagerplaetze, INSERT+UPDATE KHKLagerorte,
    EXECUTE spSysTan (SELECT nur falls noetig); Nachpruefung P-4 aus dem Dienstkontext; Pruefabfragen der Spec
    2026-11-24-lagerplatz-anlage-wms-nach-sage-spec (IDEAL: P-1 bis P-9 Pflicht vor dem Einschalten).“
    (~~`v<X>-SageLagerplatzSchreibLogin`~~ überholt.)
  - `Id='v<X>-SageLagerplatzAnlageAktiv'`, Typ `Einstellung`, Firma `beide`, Phase `Nachher`, Ausfuehrung `anzeigen`, Text: „SageLagerplatzAnlageAktiv
    bleibt aus. Einschalten je Standort erst nach Abnahmetest am Testmandanten, Rechte-GRANT, Pruefabfragen gegen die Produktiv-Sage-DB und
    Sage-DB-Backup.“
  - `Id='v<X>-SageLagerAnlageLagertechniken'`, Typ `Einstellung`, Firma `beide`, Phase `Nachher`, Ausfuehrung `anzeigen`, Text:
    „SageLagerAnlageLagertechniken bleibt Aus. Mitschnitt bzw. Alle erst nach Abnahmevergleich der Lager-Anlage am Testmandanten dieses Standorts;
    Aus lassen bei USER_-Pflichtfeld ohne Default in KHKLagerorte.“
  - ~~`Pruefung/LagerorteFuerSageAnlage` … zeigt drei Werte~~ **überholt (S9):** eine `Pruefung` liefert **eine Zahl** (0 = in Ordnung,
    `nachlauf.psd1:12-13`) und darf vor SQL/98 nicht mit Msg 208 scheitern (AKE-WMS-DBs ohne `StorageAreas`, P-8). Neu, Muster
    `Pruefung/sage-fa-ag-sdata-zugang` (`nachlauf.psd1:209-212`):
    `Id='Pruefung/LagerorteFuerSageAnlage'; Version='<X>'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'; Braucht=@('SQL/98')`,
    `Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''SageLagerplatzAnlageAktiv'' AND LOWER(LTRIM(RTRIM([Value]))) = N''true'') AND (NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Sync:Quelle:Lagerplaetze'' AND [Value] COLLATE Latin1_General_BIN = N''SData'') OR NOT EXISTS (SELECT 1 FROM dbo.StorageAreas WHERE IsActive = 1 AND SageMissingSince IS NULL)) THEN 1 ELSE 0 END'`,
    `Text='Lagerplatz-Anlage in Sage ist an, aber Sync:Quelle:Lagerplaetze steht nicht auf SData oder es gibt keine aktiven Lagerorte: das Formular Lagerplaetze in Sage anlegen bietet keinen Lagerort an (/ServiceSettings > Quelle Lagerplaetze = SData, Lagerplatz-Abgleich laufen lassen).'`,
    `Quelle='<Changelog-Slug des Dev-Laufs>'`. Mit `Braucht=@('SQL/98')` läuft die Prüfung erst, wenn `StorageAreas` existiert. `[Value]` mit
    `COLLATE Latin1_General_BIN` wie `nachlauf.psd1:214` (exakte Schreibweise `SData`).
  - Keine Schema-, Daten- oder Einmal-Einträge.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Offene Rueckfragen

1. ~~**Umfang Lager**~~ – **beantwortet 2026-11-24** (Freigabe-Antwort 1): Lager mit anlegen, wenn in Sage nicht vorhanden und am Standort
   möglich; eingearbeitet in F1a, F3a, F4, Einstellung `SageLagerAnlageLagertechniken`.
2. ~~**Schreib-Login**~~ – **beantwortet** (Freigabe-Antwort 2): bestehende `SageConnection`, Minimalrechte per GRANT (Deploy).
3. ~~**Rolle**~~ – **beantwortet** (Freigabe-Antwort 3): `masterdata`.
4. ~~**Standort**~~ – **beantwortet** (Freigabe-Antwort 4): beide; AKE-Prüfergebnisse eingearbeitet, IDEAL-Prüfung ist Voraussetzung vor dem
   Einschalten an IDEAL (kein Annahme-Stand).
5. ~~**Testsystem**~~ – **beantwortet** (Freigabe-Antwort 5): `idealakeweb01.ideal.ideal-ake.at:88` auf `IDEAL_TEST_2026_05_03`, Testlager
   legt Benjamin an.
6. ~~**DPS**~~ – **beantwortet** (Freigabe-Antwort 6): nein.
7. ~~**AKE-Abnahme**~~ – **beantwortet 2026-11-24** (Freigabe-Antwort 7, Prüfbefund B1): DEV AKE (`AKE_WMS_DEV_Service`,
   `appsettings.DEV.json` → `ake_TEST2026`), Testlager legt Gerald an; eingearbeitet in TS-192 V2/V3 und Deploy.
8. ~~**QA und Sage-Schreibschritte**~~ – **beantwortet 2026-11-24** (Freigabe-Antwort 8, Prüfbefund B2): QA darf die Prüfungen per SQL selbst
   aufrufen; Auslegung (SELECT gegen Test-DBs, Schalter auf DEV nur nach SQL-Beleg der Test-DB, danach wieder aus, Produktiv nie) in TS-192
   „QA-Regel“ und Deploy.

Keine offenen Rückfragen.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

Antworten Gerald im Chat 2026-11-24 (wörtlich übernommen):

1. Umfang Lager (nur Plätze oder auch Lager anlegen) → **wenn Lager noch nicht vorhanden, Lager auch – wenn möglich. Sonst nur Lagerplatz.**
2. Schreib-Login (eigener SageSchreibConnection oder SageConnection erweitern) → **Schreibrechte ok**
3. Rolle (masterdata oder admin) → **masterdata ok**
4. Standort (nur IDEAL oder auch AKE) → **beide; Prüfabfragen selbst machen** (Ergebnisse AKE siehe „Prüfergebnisse 2026-11-24“)
5. Testsystem am Testmandanten und Anlage der Testlager → **derzeit greift idealakeweb01.ideal.ideal-ake.at:88 auf den Testmandanten zu; Testlager legt Benjamin an**
6. DPS informieren/Bestätigung vor Produktiv → **nein**

Bestätigung Gerald im Chat 2026-11-24 zur Auslegung von Antwort 1: neue Lager nur mit WarenflussKZ `SWF`, `WE`/`WA` nicht anbieten → **ja**

7. AKE-Abnahme (Rückfrage 7 / Prüfbefund B1) → **AKE-Abnahme auf DEV AKE, Testlager legt Gerald an.**
8. QA und Sage-Schreibschritte (Prüfbefund B2) → **Prüfungen gerne per SQL selbst aufrufen.** (Gerald im Chat 2026-11-24, wörtlich)

### Prüfergebnisse 2026-11-24 (Claude, sqlcmd, nur SELECT, Datei `backlog/anhaenge/2026-11-24-pruefabfragen-lagerplatzanlage-sage.sql` = P-1 bis P-7 ohne Mandant-Filter)

**AKE** – `AKESQL20.ake.at`, DB `ake` (Produktiv) und `ake_TEST2026`, Login `akenb\gerald.weichbold`:
- **P-1:** `KHKLagerplaetze` 22 Spalten, alle außer `PlatzID`/`Mandant`/`IstGesperrt`/`IstGesperrtInventur` nullable, die vier NOT NULL haben Default `(0)`; **keine `USER_`-Spalten**.
  Längen `Kurzbezeichnung` 50, `Platzbezeichnung` 110, `Lagerkennung` varchar(10) – wie Anhang 5.3. `KHKLagerorte` 30 Spalten; NOT NULL ohne Default nur
  `Lagerkennung`; `USER_Aussenlager` (NULL) und `USER_DPSBSMDELagerInaktivBei` (Default 0) beide nullable → Lager-INSERT aus Sicht der Spalten möglich.
  Schema `ake` = `ake_TEST2026` (Diff leer bis auf einen Trigger).
- **P-2:** nur DELETE-Trigger: `TRD_Lagerorte`, `TRD_Lagerplaetze`; in `ake_TEST2026` zusätzlich `TRD_Lagerplaetze_DPSBSKonsi` (DELETE). Kein INSERT/UPDATE-Trigger.
- **P-3:** `spSysTan(@Table char, @Mandant int, @Increment int, @Tan int OUTPUT)` wie erwartet; `KHKTan` `KHKLagerplaetze` Mandant 1 = 4595 = `MAX(PlatzID)` (Produktiv).
- **P-4:** Owner `spSysTan`/`KHKTan` beide `dbo`. Schreibrechte (`db_datawriter`) heute nur `sysadm` und `akenb\gerald.berger`; das Dienstkonto
  `AKENB\svc_idealakewms` ließ sich per `EXECUTE AS LOGIN` nicht prüfen (Fehlercode 0x5, AD-Abfrage verweigert) → **Rechte vergibt der DBA; Nachprüfung
  mit dem Dienstkonto selbst** (z. B. `HAS_PERMS_BY_NAME` aus dem Dienst-Kontext oder SSMS „Ausführen als“).
- **P-5:** Lagertechniken `LTD0ST` (0 Dim.), `LTD1BL`, `LTD1UF` (1), `LTD2SF` (2), `LTD3HR` (3) – wie Anhang 4.4. 37 Lager in Mandant 1, platzverwaltet
  (`IstPlatzverwaltet = -1`): `GL`, `HR`, `KonsHainzl`, `LL`, `SL`, `VT` (nur Produktiv; `VT` fehlt in `ake_TEST2026`).
- **P-6:** 1 abweichende Kurzbezeichnung: `HAUPT;0;0;0` heißt `Hauptlager;0;0;0` (beide DBs) → Konfliktfall F5 real vorhanden.
- **P-7:** keine Dubletten.

**IDEAL** – `idealsql2.ideal.ideal-ake.at\SAGE100TEST`, DB `IDEAL_TEST_2026_05_03`: **nicht geprüft.** Anmeldung `AKENB\gerald.weichbold` abgewiesen
(„Fehler bei der Anmeldung“); bisher lief das nur über Royal TS mit `IDEAL\t1_adminweg`. Dieselbe Datei dort ausführen (Mensch) – offen
vor dem Dev-Lauf, mindestens P-1 (`USER_`-Pflichtfelder sind mandantenspezifisch, Anhang 4.2), P-2 und P-4.

**Nachtrag AKE P-8/P-9 (2026-11-24, gleiche Anmeldung, nur SELECT):**
- **P-9a:** `KHKArtikelVariantenLagerplatz` hat die Spalten `Artikelnummer`, `AuspraegungID`, `PlatzID`, `Mandant`, `MengeLager`, `Timestamp` –
  **keine `Lagerkennung`**. Die Prüfung 50004 muss über `PlatzID` gegen `KHKLagerplaetze` joinen (beide DBs gleich).
- **P-9b:** `KHKGruppen` Typ 40025 (Mandant 0): `SWF`, `WE`, `HWE`, `WA`, `HWA` vorhanden.
- **P-9c:** `USER_`-Spalten `KHKLagerorte`: `USER_Aussenlager` (nullable, Default NULL), `USER_DPSBSMDELagerInaktivBei` (nullable, Default 0) – keine Pflicht.
- **P-9d:** Kollation `SQL_Latin1_General_CP1_CI_AS` (ohne Groß/Klein) in `ake` und `ake_TEST2026`.
- **P-8 (WMS-DBs auf AKESQL20, `IDEAL_AKE_WMS` und `IDEAL_AKE_WMS_Test`):** `Sync:LagerplaetzeEnabled = true`, **`Sync:Quelle:Lagerplaetze` nicht gesetzt**,
  **Tabelle `StorageAreas` existiert nicht** (Msg 208) – beide DBs stehen vor SQL/98. Folge: An AKE ist die Lagerort-Auswahl (F1) erst nach dem
  Bündel-Deploy (SQL/98 ff.) **und** mit `Sync:Quelle:Lagerplaetze = SData` gefüllt; vorher greift der Leer-Hinweis aus F1. Im Deploy/Manifest als
  Voraussetzung je Standort führen.

## Kritische Pruefung (2026-11-24)

Anwalt des Teufels, nur lesend: Spec, Backlog, Anhang [[2026-11-20-WMS_Lagerplatzanlage_Sage_v2]], ADR 0004/0010/0014/0020, fallstricke, Code im
Bündel-Worktree `2026-08-07-ideal-teile-1-5` (HEAD `9dbc6ef2`, v1.99.63). Keine Datenbank abgefragt. Zeilenangaben ohne Pfad = diese Spec.

**Stichprobe Belege (15, alle bestätigt):** `SageStammdatenOutbox.cs:14,18-27,30-45`; `StorageLocationRepository.cs:17-27`;
`StorageLocationsController.cs:30-45,55-92,103-120,147-155`; `StorageLocation.cs:121-130`; `LagerplatzSyncService.cs:80-98,124-126,146,152,174-189,217,243-261,313-379`
(inkl. `:319-320`, `:325`, `:364-375`); `SageLagerplatzReader.cs:23-29` (Mandant 1, Join ohne Mandant); `SQL/107…sql:82` (kein CHECK, auch sonst
keiner in der Datei); `IdealAkeWms/appsettings.json:2-5` (keine SageConnection); `SageBookingWorker.cs:14-15,136-150,265-292` (`:268`, `:272-278`,
`:285`); `ServiceSettingDefinitions.cs:35,75-78,347`; `SageFaArbeitsgaenge.cs:16-21`; `SyncLogServices.cs:44,66,108`; `controller.md:53-54`.
Negativaussagen geprüft: `StorageAreaId` wird nur an `LagerplatzSyncService.cs:152`/`:217` geschrieben (Suche in *.cs, *.sql, *.ps1, *.psd1,
*.cshtml, *.js; Model-Binding im `Create`-POST ist der einzige weitere Weg, AK 3 deckt ihn); `StorageAreas` schreibt nur der Abgleich;
Outbox-Erlediger für `StorageLocation` nur E9 (`BdeOperatorRepository.cs:32,44` nutzt `ErledigenAsync` für den Typ `BdeOperator` – die Erweiterung
um `Gesendet` wirkt dort mit, harmlos). `@Bez varchar(200)` im Platz-Befehl ist unkritisch (Kürzung bei der Zuweisung, danach greift
`LEN > 110`). `AnzahlDimension1–3` existieren (Anhang 4.2 Zeile 89; AKE P-1 „30 Spalten“). Mandant fest 1 ist konsistent mit den Readern.

### BLOCKER

**B1 – Rückfrage 7 unbeantwortet (Zeile 40, 724-727).** Laut Anleitung im Freigabe-Block (Zeile 734-737) ist die Spec erst startklar, wenn jede
Rückfrage beantwortet ist. Inhaltlich ist 7 nur Voraussetzung **vor dem Einschalten an AKE** (Zeile 726), nicht für den Dev-Lauf. Befund am Rande
(keine Antwort): die lokale DEV-AKE-Dienstkonfiguration zeigt auf die Sage-Test-DB (`D:\Dev\AKE\Service\appsettings.DEV.json:4` →
`ake_TEST2026`), ihr `appsettings.json:4` dagegen auf Produktiv `ake`.
→ **Frage an Gerald:** Rückfrage 7 jetzt beantworten – oder ausdrücklich festhalten „Freigabe ohne 7, AKE-Abnahme später, AKE bleibt bis dahin aus“?

**B2 – Ein automatischer QA-Lauf kann echte Sage-Daten schreiben.** Der `qa-agent` deployt nach grünem Build selbständig auf DEV und fährt die
Testszenarien (`.claude/agents/qa-agent.md:62-87`). Seine harten Grenzen (`:84-87`) nennen `SageLagerbuchungAktiv`, `Sync:*Enabled`, Mails –
**nicht** `SageLagerplatzAnlageAktiv`. Die DEV-Dienste lesen Sage aus den Test-DBs (`D:\Dev\AKE\Service\appsettings.DEV.json:4`,
`D:\Dev\IDEAL\Service\appsettings.DEV.json:4`), ohne Umgebungs-Überlagerung aus Produktiv `ake` (`appsettings.json:4` beider DEV-Dienste).
Die Schritte .c–.v schreiben in Sage (Lager, Plätze, `KHKTan`), das WMS löscht nie.
→ **Frage an Gerald:** Darf der QA-Lauf auf DEV `SageLagerplatzAnlageAktiv` einschalten (dann schreibt er in `ake_TEST2026` bzw.
`IDEAL_TEST_2026_05_03`), oder sind alle Sage-schreibenden Schritte (.c–.f, .h–.m, .p–.v) „nur Mensch“ (für QA NICHT MOEGLICH) und der Schalter
kommt in die Grenzliste bzw. das DEV-Sicherheits-SQL? Die Antwort gehört in die Abschnitte Test-Szenarien und Deploy.

### SOLLTE

**S1 – Der Lager-Befehl referenziert eine nicht existierende Spalte (Zeile 373 vs. Prüfergebnis P-9a, Zeile 773-774).** Rumpf, F3a Punkt 4
(Zeile 194) und AK 31 sind nach P-9a nicht nachgezogen: `KHKArtikelVariantenLagerplatz` hat keine `Lagerkennung`. Eine fehlende Spalte einer
vorhandenen Tabelle ist ein **Kompilierfehler des ganzen Batches** (207) → jede Lager-Anlage scheitert, und F3 Punkt 5 stuft 207 als fachlich mit
dem irreführenden Text „Sage-Tabelle hat abweichende Pflicht-/Spalten … P-1“ ein. Nicht InMemory-testbar, fällt erst in TS .q auf.
Vorschlag (eine Prüfung statt Join): 50004 ersetzen durch
`IF EXISTS (SELECT 1 FROM KHKLagerplaetze WITH (UPDLOCK, HOLDLOCK) WHERE Mandant = @Mandant AND Lagerkennung = @Lager) THROW 50004, 'Plaetze oder Artikel-Zuordnungen zu dieser Lagerkennung vorhanden', 1;`
– Zuordnungen hängen über `PlatzID` an Plätzen; verwaiste Plätze mit derselben Kennung würden sonst still Teil des neuen Lagers (der Befehl prüft
heute nur die Kurzbezeichnung, Zeile 371). Alternativ wörtlich nach P-9a:
`… FROM KHKArtikelVariantenLagerplatz a JOIN KHKLagerplaetze p ON p.PlatzID = a.PlatzID AND p.Mandant = a.Mandant WHERE p.Mandant = @Mandant AND p.Lagerkennung = @Lager`.
Rumpf, F3a, AK 31 und GRANT-Liste (Zeile 397, 684) entsprechend nachziehen.

**S2 – „Ein SaveChanges“ (F1a Zeile 144, AK 26) widerspricht dem genannten Weg über `AddAsync` (F2 Zeile 156).** `StorageLocationRepository.AddAsync`
speichert je Platz **zweimal** (`Repository.cs:33-37` + `StorageLocationRepository.cs:23`); der Outbox-Schlüssel braucht die Id erst nach dem
Insert (Kommentar `StorageLocationRepository.cs:14-15`). 200 Plätze = 400 Speichervorgänge, nicht atomar. Folgen: Ein Abbruch mittendrin
hinterlässt Plätze ohne Outbox-Eintrag (der Dienst liest nur die Outbox → nie gesendet, sehen aber wie Sage-Anlage-Plätze aus). Wird das
`StorageArea` vor seinem Outbox-Eintrag gespeichert und läuft der Abgleich dazwischen, setzt die Fehlend-Schleife
(`LagerplatzSyncService.cs:364-375`) das neue Lager auf inaktiv → F3a sendet es nie.
Vorschlag: eine DB-Transaktion um Lager + Plätze + Outbox (Muster `BeginTransactionAsync` in `BfAuftragRepository`/`PickingOrderRepository`),
eigene Repository-Methode statt `AddAsync` je Platz. AK 26 auf „in einer Transaktion; scheitert ein Teil, ist nichts angelegt“ ändern.

**S3 – Ein „technischer“ Fehler blockiert die Warteschlange dauerhaft (F3 Punkt 5, Zeile 178-183).** Technisch = alles außer 515/207 und
50001–50019. Damit wiederholen sich Datenfehler wie 8152/2628 (Kürzung), 220/8115 (Überlauf), 245 (Konvertierung), 547/2627 in jedem Takt, und
„Rest des Takts übersprungen“ hält **alle** folgenden Einträge an.
Vorschlag: Liste umdrehen – technisch nur Verbindung/Timeout (`-2`, Transportfehler, `SqlException.Class >= 20`), 1205 (Deadlock), 1222
(Lock-Timeout), 229/230/262 (Recht, nötig für TS .l), 4060/18456 (Anmeldung); jede andere `SqlException` = fachlich mit Meldungstext. Dazu die
Koordinaten im Formular nach oben begrenzen (z. B. ≤ 9999; der Typ von `Dimension1–3` steht im P-1-Ergebnis nicht). Neues AK: „SQL-Fehler 8152 an
Eintrag A → A `Fehler`, Eintrag B im selben Takt angelegt“.

**S4 – Sperren gegen den Sage-Dialog (Zeile 302, 315-317, 360, 371).** `UPDLOCK, HOLDLOCK` mit `… OR Kurzbezeichnung = @Kurz` ist ohne Index auf
`Kurzbezeichnung` ein Scan mit Range-Sperren über die ganze Tabelle (AKE 4595 Plätze, nahe der Eskalationsschwelle 5000). Reihenfolge WMS:
`KHKLagerplaetze` → `KHKTan`; Sage-Dialog: `KHKTan` (`spSysTan`) → INSERT `KHKLagerplaetze` (Anhang 5.1) – ein Deadlock-Zyklus, dessen Opfer der
Sage-Anwender sein kann. Werden die Parameter als `nvarchar` gesendet, verhindert die SQL-Kollation (`SQL_Latin1_General_CP1_CI_AS`, P-9d)
zusätzlich Index-Seeks.
Vorschlag: `SET DEADLOCK_PRIORITY LOW;` an den Anfang beider Befehle (das WMS ist das Opfer, 1205 = technisch, nächster Takt); `SqlParameter` mit
`SqlDbType.VarChar` und Länge (10/40/50); Prüfabfrage P-10 (Indizes auf `KHKLagerplaetze` aus `sys.indexes`/`sys.index_columns`) als Information
für die ADR.

**S5 – Ergebnis-`SELECT` vor `COMMIT` (Zeile 335-336, 367, 384).** Liest der Client die erste Ergebnismenge und schließt den Reader, kann ein Fehler
einer späteren Anweisung (hier das `COMMIT`) je nach Lesemuster unbemerkt bleiben – das WMS hielte einen zurückgerollten Platz für „angelegt“.
Vorschlag: Ergebnis in Variablen, `COMMIT`, **danach** `SELECT @PlatzID, …` (oder Output-Parameter + `ExecuteNonQueryAsync`).

**S6 – AK 17 / TS .f („weder … aktualisiert“) ist mit Quelle SData nicht erfüllbar.** Die Übernahme (F3 Punkt 3) setzt nur Quelle, Platz-Id,
Kennung, Beschreibung. Der Sage-Zweig vergleicht aber auch `SageIstGesperrt`, `SageIstGesperrtInventur`, Maße, Volumen, Tragkraft
(`LagerplatzSyncService.cs:195-203`; `Abweichend` `:301` = „SData liefert einen Wert, das WMS hat null“) → der erste Abgleich zählt jeden
übergebenen Platz als „aktualisiert“.
Vorschlag: Die Übernahme setzt die bekannten Befehlswerte mit (`SageIstGesperrt = false`, `SageIstGesperrtInventur = false`, Maße 0; im Fall
„vorhanden“ weglassen) – oder AK 17/TS .f auf „höchstens einmal aktualisiert, keine Dublette, kein Konflikt“ ändern. Entscheidung im Rumpf.

**S7 – Protokoll- und Mailflut ungeregelt.** Es fehlt „kein Lauf-Eintrag je Takt ohne Arbeit“ (Muster `SageFaArbeitsgangService.cs:82`). Plätze eines
wartenden oder fehlerhaften Lagers bleiben `Offen` (F3 Zeile 167-168) und wären in jedem 20-s-Takt Kandidaten → ein Lauf „uebersprungen“ alle 20 s.
`NotifyErrorAsync` hat keine Drosselung (`SyncErrorNotifier.cs:28-60`); der Verweis auf `SageBookingWorker.cs:272-278` betrifft den Zugangsfehler,
nicht Einträge – bei einem technischen Fehler (TS .l) käme eine Mail je Takt.
Vorschlag: (a) wartende Plätze zählen nicht als Arbeit; (b) gleicher technischer Fehlertext → kein neuer Lauf, keine Mail (Merker im Worker wie
`_agZugangGemeldet`), Mail nur beim Wechsel; (c) AK: „Schalter an, nichts zu tun → 10 Takte, 0 neue Läufe im Protokoll“.

**S8 – Fehlende „Bestandsverhalten unverändert“-AKs.** AK 22 ist zu weich („unverändert“). Prüfbare Fassung:
(a) Abgleich mit Quelle SData: ein Sage-Lager **ohne** Outbox-Eintrag, das nicht geliefert wird, bekommt weiter `SageMissingSince`;
(b) Schalter an: „Neuen Lagerplatz anlegen“ erzeugt weiter Status `Offen` + Outbox ohne `StorageAreaId`, wird nicht gesendet, E9-Übernahme beim
Abgleich wie heute (`LagerplatzSyncService.cs:174-189`); (c) `ErledigenAsync` bestätigt `BdeOperator`-Einträge wie bisher; (d) neuer
Spalten-Key auch in `ColumnFilterTypes.For("StorageLocations/Index")` (`StorageLocationsController.cs:76`), bestehende Listen-Tests grün;
(e) bestehende `LagerplatzSdataTests` unverändert grün.

**S9 – Die Manifest-Prüfung passt nicht zur Konvention (Zeile 706-707).** `Typ Pruefung` braucht **eine Zahl** (0 = in Ordnung,
`nachlauf.psd1:12-13`); „zeigt drei Werte“ ist keine. Und P-8 belegt: die AKE-WMS-DBs haben `StorageAreas` noch nicht (Zeile 778-781) → Msg 208,
wenn die Prüfung vor SQL/98 läuft. Vorschlag: `Braucht=@('SQL/98')`, Pruefung
`SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''SageLagerplatzAnlageAktiv'' AND LOWER(LTRIM(RTRIM([Value]))) = N''true'') AND (NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Sync:Quelle:Lagerplaetze'' AND [Value] COLLATE Latin1_General_BIN = N''SData'') OR NOT EXISTS (SELECT 1 FROM dbo.StorageAreas WHERE IsActive = 1 AND SageMissingSince IS NULL)) THEN 1 ELSE 0 END`
(Muster `Pruefung/ArtikelQuelleSData`, `nachlauf.psd1:213-216`), Text nur ASCII.

**S10 – Ein Lager mit dauerhaftem Fehler ist eine Sackgasse.** „Verwerfen“ ist Out-of-Scope (Zeile 100); das Lager steht weiter mit „(neu, wartet
auf Sage)“ in der Auswahl (F1 Zeile 114), Anwender legen weitere Plätze an, die nie gesendet werden; es erscheint auch als Lagerort in den
Kommissionierbereichs-Regeln (`PickingAreaRepository.cs:35-36`, kein Aktiv-/Outbox-Filter). Vorschlag: Lager mit Outbox `Fehler` in F1 als
„(Fehler – erst ‚Erneut senden‘)“ zeigen und für neue Plätze sperren; die Kommissionierbereichs-Folge in Hilfe/ADR nennen.

**S11 – Größe.** Grob 25–30 Dateien: Web (Controller mit 4 Actions, ViewModel, neue View, `Index.cshtml` Spalte/Hinweise/Knöpfe, Edit-Regel F7,
Überposting, Repository mit Transaktion), Dienst (Interface, SQL-Implementierung mit 2 Befehlen, Service mit 2 Status-Automaten, Worker-Schritt, DI,
Fehlend-Schleife), Katalog (2 Keys + Meta + Konstanten), Tests, ADR, fallstricke, Hilfe, README, Changelog, TESTSZENARIEN, Manifest,
Anwenderdoku mit Fotos. In einem Lauf machbar, aber der Lager-Teil trägt S1, S10, IDEAL-P-9 und AKE ohne `StorageAreas`. Vorschlag: zwei Teile –
**Teil 1 Plätze in vorhandenen Lagern** (F1 ohne F1a, F2, F3, F5–F7), **Teil 2 Lager** (F1a, F3a, F4-Ausnahme, Einstellung
`SageLagerAnlageLagertechniken`, AK 24–33). Teil 1 ist allein nutzbar.

### HINWEIS

- **H1 Die IDEAL-Prüfabfragen sind kein Freigabe-Blocker.** Für `IDEAL_TEST_2026_05_03` belegt der Anhang bereits P-2 (nur DELETE-Trigger, Anhang
  Zeile 23), P-3 (`spSysTan`-Quelltext, Zeile 51-53) und die `USER_`-Spalten von `KHKLagerorte` (Zeile 102-104, alle mit Default). Offen bleiben
  `USER_` an `KHKLagerplaetze`, der Owner (P-4) und die Produktiv-DB – Voraussetzung **vor dem Einschalten**. Zeile 448-450 und 770 („vor dem
  Dev-Lauf“) überzeichnen das; zugleich fehlt dort P-3, obwohl gerade `spSysTan` codewirksam ist.
- **H2** `docs/TESTSZENARIEN.md` im Bündel enthält schon TS-188 – „höchste TS-187“ (Zeile 588) ist überholt.
- **H3** F2 „Code schon vorhanden“ ohne Groß/Klein vergleichen (Abgleich `OrdinalIgnoreCase`, `LagerplatzSyncService.cs:100-101`; Unique-Index
  `ApplicationDbContext.cs:236`), sonst Ausnahme beim Speichern statt Meldung.
- **H4** F3a Punkt 1 prüft die Technik-Freigabe vor dem Fall „vorhanden“: Übernimmt der Abgleich nach einem Konflikt eine nicht freigegebene
  Sage-Technik, scheitert „Erneut senden“ dauerhaft. Freigabe nur für die Neuanlage prüfen.
- **H5** `KHKLagerorte.Lagertechnik` ist nullable (Anhang 4.2 Zeile 85): der `JOIN` im Platz-Befehl liefert dann 50010 „Lager … nicht gefunden“ –
  Text um „oder ohne Lagertechnik“ ergänzen.
- **H6** TS .l setzt voraus, dass das Dienstkonto des Testsystems noch **kein** Schreibrecht hat. Hat es eines (z. B. `db_datawriter` auf der Test-DB),
  entfällt .l – das gehört in die Checkliste, sonst Rückfrage mitten im Test.
- **H7** Paar 1 „ein Lager mit Basisplatz-Inventurdaten ≠ NULL“ (Zeile 600): wie für **beide** Lager gleiche Inventurdaten entstehen, steht nirgends.
  Vorschlag: in einem vorhandenen Lager mit Inventurdaten (Anhang 5.2: LIFT1) je einen Platz per Sage-Dialog und per WMS anlegen und nur diese
  zwei Zeilen vergleichen.
- **H8** Stufe `Alle` verlangt 0 Abweichungen auch für `LTD1UF`/`LTD3HR`-Lager (Einstellung Zeile 490), das Testszenario legt nur `LTD2SF`/`LTD1BL`
  an – `Alle` wäre nach eigener Regel nie zulässig. Paare ergänzen oder Stufe streichen (YAGNI).
- **H9** Ein hängender `Gesendet`-Eintrag, dessen Platz inzwischen deaktiviert wurde, wird nie mehr verarbeitet (Filter `IsActive`, F3); der Abgleich
  übernimmt ihn per E9 und **reaktiviert** ihn (`LagerplatzSyncService.cs:225`). Als bekannte Folge dokumentieren.
- **H10** Lagerkennung in F1a auf unkritische Zeichen begrenzen (Buchstaben, Ziffern, `-`, `_`): Leer- und Sonderzeichen landen in Code und
  Barcode (`BarcodeValue = Code`).
- **H11** Test- und Abnahmelager bleiben in den Sage-Test-DBs stehen (das WMS löscht nie). Aufräumen durch einen Sage-Anwender gehört in die
  Checkliste.
- **H12** Abweichung vom Hausmuster „hängend nie blind neu senden“ (`SageBookingWorker.cs:19`): hier bewusst erneut senden, weil der Befehl
  idempotent ist – in ADR 0036 ausdrücklich begründen.
- **H13** Zwei Dienst-Instanzen gegen dieselbe WMS-DB (Deploy-Überlappung) oder zwei WMS gegen dieselbe Sage-Test-DB fängt die Prüfung mit Sperre
  in Sage ab („vorhanden“) – geprüft, kein Befund.

NACHBESSERUNG NOETIG: B1/B2 brauchen Geralds Antwort; der Lager-Befehl referenziert eine nicht existierende Spalte (S1), die Erfassung ist nicht atomar (S2).

Eingearbeitet 2026-11-24: B1 (Antwort 7), B2 (Antwort 8, als Auslegung), S1 (Join über PlatzID + 50007 für verwaiste Plätze), S2, S3, S4 (P-10 nur Information), S5, S6 (Variante „Übernahme setzt Sage-Werte“), S7, S8, S9, S10, H1–H12; H13 ohne Handlungsbedarf; S11 bewusst abgelehnt (kein Split, Entscheid Gerald; Etappen-Reihenfolge Plätze vor Lager).

## Umsetzung (2026-11-25)

- Worktree-Commit `5b6fa696` (v1.99.71), kein SQL, keine Migration. TS-Nummer **TS-192** (statt TS-189, vergeben). ADR [[0036-lagerplatz-anlage-in-sage-per-direkt-sql-hinter-austauschbarer-schnittstelle]], Fallstrick §66.
- Build 0 Fehler, 28 Warnungen (Baseline 30); Tests Web 3586 grün / 1 übersprungen (vorher 3551), Dienst 847 grün (vorher 801).
- Korrektur Testlager: AKE legt **Gerald** in `ake_TEST2026` an, Benjamin nur IDEAL.
- Kein SQL gegen Sage ausgeführt, keine Einstellung umgeschaltet; Schreibweg nur über Fakes und SQL-Text-Tests belegt. Live-Test = qa-agent (TS-192, V1–V4).
- Abweichungen/Funde:
  - Atomarität der Web-Anlage (AK 26) ist InMemory nicht prüfbar (keine Transaktion); belegt per DbUpdateException-Test, live in TS-192.
  - Manifest-Eintrag `Pruefung/LagerorteFuerSageAnlage` mit `Firma = 'beide'`, braucht SQL/98.
  - Mandant fest 1 in `SqlSageLagerplatzAnlage` (`ponytail:`-Kommentar), wie der Lese-Abgleich.
  - Zusatzfund bestätigt, nicht gebaut: `SageLagerplatzReader.cs:26` verbindet ohne `Mandant`.

## QA-Nachweis (qa-agent, 2026-11-25)

Geprüft: Implementierungs-Commit `5b6fa696` (Bereich `6e426113..5b6fa696`), Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`.
QA-Commit im Worktree: `47a23d32` (nur `docs/anwender/19-lagerplaetze-in-sage-anlegen.md` + Fotos 19-01..19-05, Knopftext angeglichen).

### 1. Build und Tests (selbst gemessen)

- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 32 Warnungen (inkrementell; keine neue Warnung in den geänderten Dateien gefunden).
- `dotnet test`: IdealAkeWms.Tests 3586 bestanden / 1 übersprungen / 0 fehlgeschlagen (3587); IDEALAKEWMSService.Tests 847 / 0 / 0.

### 2. Code-Review (selbst, synchron in diesem Lauf)

Untersucht: `SqlSageLagerplatzAnlage` (Platz-/Lager-SQL, Parameter, Fehlerklassen), `SageLagerplatzAnlageService`, `SageBookingWorker`
(Schritt + Melde-Merker), Fehlend-Schleife `LagerplatzSyncService`, `StorageLocationsController` (CreateInSage, Edit F7, Create, Erneut
senden), `StorageLocationRepository.AddSageAnlageAsync`/`ErneutSendenAsync`, `SageStammdatenOutbox`, `SageLagerplatzAnlage` (Code-Format),
`ServiceSettingDefinitions`, Views `CreateInSage`/`Index`/`Edit`, `nachlauf.psd1`, Tests. Gegen `ake_TEST2026` nur per SELECT: alle im SQL
genannten Spalten existieren (kein 207-Risiko), keine NOT-NULL-Spalte ohne Default fehlt im INSERT, nur DELETE-Trigger (P-2), `spSysTan`/`KHKTan`
gehören `dbo` (P-4), `spSysTan` liefert kein Resultset (der Reader liest das Ergebnis-SELECT nach COMMIT richtig), P-5/P-9/P-10 wie in der Spec.

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| N1 | niedrig | Abbruch beim Dienst-Stopp während `ExecuteReaderAsync`: SqlClient wirft `SqlException` „Operation cancelled by user“ (Nummer 0, Klasse 11); `IstTechnisch` stuft sie fachlich ein → Eintrag geht auf **Fehler** statt Offen und braucht „Erneut senden“ (danach „vorhanden – übernommen“; kein Datenverlust, `XACT_ABORT` rollt zurück). Fix-Vorschlag in beiden catch-Blöcken: `catch (SqlException ex) when (!ct.IsCancellationRequested && !IstTechnisch(ex))`. | Nicht gefixt (kein Datenschaden, manuell behebbar); Mensch entscheidet, Beobachtung in Testschritt .m. |
| N2 | niedrig | AK 13/F5 verlangen, dass der Konflikttext PlatzID/Kurzbezeichnung nennt; `THROW 50014` ist statischer Text (wie im Spec-SQL) → Spalte „Übergabe-Fehler“ nennt die belegende PlatzID nicht. Vorschlag: Meldung per `CONCAT` in eine Variable, `THROW 50014, @msg, 1`. | Nicht gefixt (ändert das nur live prüfbare Sage-SQL); AK 13 Textteil offen für Manual-UAT. |
| H1 | Hinweis | Rennen im Abgleich: Abgleich liest den Feed vor der Sage-Anlage, der Worker bestätigt dazwischen, die Fehlend-Schleife markiert das neue Lager als fehlend; der nächste Abgleich reaktiviert es (Update-Zweig setzt IsActive/SageMissingSince zurück). Selbstheilend, Plätze nicht betroffen. | Akzeptiert. |
| H2 | Hinweis | Unerwartete Ausnahme (z. B. DB-Fehler bei SaveChanges) → fehlgeschlagener Lauf + Worker-Mail je Takt; gleiches Muster wie die übrigen Worker-Schritte. | Akzeptiert. |

Ohne Befund geprüft: Parametrisierung (typisierte VarChar/Int/SmallInt/Bit, keine String-Verkettung → keine Injection); Transaktion mit
`XACT_ABORT ON` (rollt auch bei Attention/Timeout zurück), `DEADLOCK_PRIORITY LOW`, 1205/1222 technisch; `spSysTan` in der Transaktion;
Idempotenz bei Doppelausführung (UPDLOCK/HOLDLOCK auf `KHKLagerorte` serialisiert, „vorhanden“/Konflikt-Logik, Gesendet-Hänger nach
Stuck-Minuten); Fehlend-Schleife überspringt nur nicht bestätigte StorageArea-Outbox; Mandant fest 1 mit `ponytail:`-Kommentar; Script-Blöcke
ohne Serverwerte; `hidden` ohne `d-*`-Konflikt; keine stillen Fallbacks; keine positionsgebundenen Arrays; alle Regeln serverseitig
(live belegt, siehe 6); Code-Format an einer Stelle (`SageLagerplatzAnlage`) für Web und Dienst; Überposting-Schutz in Create.

### 3. Beweisart je AK

- **Automatisiert:** AK 1–8 (Web-Seite), 12, 15–19, 22a–e, 24, 25, 26 (nur Ablauf/DbUpdateException), 28, 29, 30–31 (Status-Automat, SQL-Text), 34, 35, 37–39.
- **Manual-UAT (nicht grün gezählt):** AK 9–11, 13/14/27/30–33 (Sage-Seite), 36 (Code-Review bestätigt DEADLOCK LOW/VarChar/SELECT nach COMMIT, Laufzeit offen), 40 (V3 Teil 3), echte Atomarität von 26 (auf DEV per SQL gesehen: 1 Lager + 6 Plätze + 7 Outbox in einem Absenden, siehe .p).

### 4. Geänderte bestehende Tests

- `SageUebergabeTests.cs` (2 Zeilen), `StorageLocationsControllerTests.cs` (6 Zeilen): nur neues Ctor-Argument → Typ-/Compile-Anpassung.
- `LagerplatzSdataTests.cs`: nur ein neuer Test angehängt, kein bestehender geändert. Kategorie „sonstige“: keine.

### 5. Checkliste / ADR 0020

Keine Migration nötig (kein Schema); beide `AppVersion.cs` 1.99.71, `Changelog.cshtml` + Brain-Changelog vorhanden; ADR 0036, fallstricke §66;
TS-192 in `docs/TESTSZENARIEN.md`, Index-Eintrag vorhanden; README/Hilfe/`docs/anwender`+`docs/betrieb` nachgezogen. Manifest:
`Pruefung/LagerorteFuerSageAnlage`, `v1.99.71-SageLagerplatzRechte`, `-SageLagerplatzAnlageAktiv`, `-SageLagerAnlageLagertechniken` vorhanden.
ADR 0020: beide Einstellungen mit Anzeigename/Wirkung/Verwendung/Standort/Default/Warnhinweis; `SageLagerAnlageLagertechniken` als Choice mit
`Werte` (Aus/Mitschnitt/Alle), Server-Validierung über `SettingValidator` (Choice). Kein Befund.

### 6. DEV-Deploy und TS-192

Deploy (erhöhte Shell, Quelle Worktree, Web + Dienst): **AKE** „Fertig: AKE v1.99.71 auf http://localhost:8081/“, Exit 0, Fußzeile v1.99.71;
**IDEAL** „Fertig: IDEAL v1.99.71 auf http://localhost:8082/“, Exit 0.

Vorbedingungen Live-Teil:
- **V3 belegt (Teil 1+2):** `AKE_WMS_DEV_Service` läuft als `svc_idealakewms@ake.at`, `DOTNET_ENVIRONMENT=DEV`; `D:\Dev\AKE\Service\appsettings.DEV.json:4`
  SageConnection `Server=AKESQL20.ake.at;Database=ake_TEST2026`; `SELECT @@SERVERNAME, DB_NAME()` = `AKESQL20` / `ake_TEST2026`. Teil 3 offen (kein Lauf).
- **V1/V2 fehlt:** in `ake_TEST2026` keine Testlager `TS-WMS`/`TS-SAGE`/`TL-SAGE`/`TL-*` (nur `TEST`, `TEST1`, beide inaktiv).
- **V4/GRANT fehlt:** `sys.database_permissions` zeigt keinen GRANT für `svc_idealakewms`; kein gleichnamiger DB-Benutzer (evtl. implizit über
  eine Gruppe, unbelegt); nur `EXECUTE spSysTan` an `public`.
- ⇒ **Live-Schreibtest gegen Sage NICHT MOEGLICH.** Kein Schreibzugriff auf Sage; `ake_TEST2026` nach dem Lauf unverändert
  (`KHKTan` KHKLagerplaetze/Mandant 1 = 4378 = MAX(PlatzID), 0 Lager/Plätze `QA%`). **In Sage-Test angelegte Datensätze: keine.**

Vorgehen WMS-Seite (Auslegung QA): `AKE_WMS_DEV_Service` **gestoppt** (Stopped belegt), damit nichts nach Sage gehen kann; QA-Lager `QA-LP`
(LTD2SF), `QA-HR` (LTD3HR) per INSERT in `AKE_WMS_DEV.StorageAreas` (Audit `qa-agent`); Schalter nur in `AKE_WMS_DEV` an; Browser per Playwright
(Edge headless, Windows-Anmeldung); Fehlerzustände für .j/.u per UPDATE in der DEV-DB gesetzt. Danach Schalter zurück, QA-Testdaten (4 Lager,
12 Plätze, 14 Outbox-Zeilen) aus `AKE_WMS_DEV` entfernt, damit ein späterer Live-Lauf sie nicht nach Sage schreibt; Dienst wieder gestartet (Running).

| Schritt | Ergebnis | Status |
|---|---|---|
| .a | AKE + IDEAL: kein Knopf, Spalte Übergabe-Fehler da, Hinweis „Das WMS sendet nichts an Sage“, Direktaufruf → „ist ausgeschaltet … nichts angelegt“ | OK |
| .b | Benutzer R: nur ein Windows-Konto verfügbar; Filter `RequireMasterDataAccess` im Code + Tests | NICHT MOEGLICH (Browser) |
| .c | QA-LP D1 1–3, D2 1–2, D3 0 → „6 Lagerplätze angelegt“, 6 Plätze `QA-LP;1;1;0`… Status Offen, 6 Outbox Status 0 | OK |
| .d, .d2, .e, .f, .h, .i, .k, .l, .m, .q, .r, .s, .v | brauchen den Sage-Schreibweg (V1/V2/GRANT fehlen) bzw. Sage-Dialog/Abgleich/DBA (Mensch) | NICHT MOEGLICH |
| .g | gleicher Bereich nochmals → „0 angelegt, 6 übersprungen (Code im WMS schon vorhanden: …)“ | OK |
| .j (WMS-Teil) | Fehler-Platz zeigt Status Fehler + Text in Spalte, „Erneut senden“ → „Erneut eingereiht“, Platz Offen, Outbox Status 0/Versuche 0; zweites Senden auf Offen-Platz → „Kein Übergabe-Fehler … gefunden“; POST ohne Antiforgery → 400 | OK |
| .n | Schalter aus: Liste/„Neuen Lagerplatz anlegen“ unverändert (Buchung/Abgleich nicht geprüft) | OK (teilweise) |
| .o | `Aus`: keine Option „Neues Lager“, Hinweis „nicht freigegeben“, POST `Lagerort=neu` serverseitig abgewiesen; `Mitschnitt`: nur LTD0ST/LTD1BL, POST LTD2SF abgewiesen; keine StorageArea angelegt | OK |
| .p (WMS-Teil) | QA-NEU LTD2SF platzverwaltet 1–2 × 1–3 → „Lager QA-NEU erfasst …; 6 Lagerplätze angelegt“; Auswahl „(neu, wartet auf Sage)“, Hinweis „Lager QA-NEU wartet …“; DB: 1 StorageArea, 6 Plätze mit StorageAreaId = neues Lager, 7 Outbox (1 × StorageArea, 6 × StorageLocation) Status 0 | OK |
| .t | `qa-lp` → „Lagerkennung qa-lp schon vorhanden“; `QA LP`, `QA;X`, leer → Zeichenregel; Bezeichnung 41 Zeichen, Technik LTD9XX, LTD0ST mit Koordinaten serverseitig abgewiesen; LTD0ST sperrt „platzverwaltet“ im Formular | OK |
| .u (WMS-Teil) | Lager QA-NEU auf Fehler gesetzt → roter Hinweis mit Grund + „Erneut senden“, Auswahl „(Fehler – erst ‚Erneut senden‘)“ disabled, POST auf das Lager abgewiesen; „Erneut senden“ Lager → Outbox Offen | OK |
| AK 6 | D1 0, D1 10000 → „ganze Zahlen von 1 bis 9999“; von > bis → Meldung; 220 Plätze → „höchstens 200“ (serverseitig per POST belegt) | OK |
| .w | `SageLagerplatzAnlageAktiv = false`, `SageLagerAnlageLagertechniken = Aus` in AKE_WMS_DEV und IDEAL_WMS_DEV per SELECT belegt; Aufräumliste Sage: leer | OK |

Beobachtung (kein Befund): Eine 11-stellige Kennung im Browser wird von `maxlength=10` still auf 10 Zeichen gekürzt (`ABCDEFGHIJ` wurde
angelegt); serverseitig wird 11 Zeichen abgewiesen. Testdaten wieder entfernt.

Fotos (DEV AKE, Fehlerzustände in der DEV-DB gesetzt, kein Live-Lauf): `docs/anwender/bilder/19-01-knopf-lagerplaetze-in-sage-anlegen.jpg`,
`19-02-formular-bereich.jpg`, `19-03-formular-neues-lager.jpg`, `19-04-liste-uebergabe-fehler.jpg`, `19-05-hinweis-lager-wartet.jpg` (wartend + Fehler).

### 7. Publish

`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` – `IDEALAKEWMSWeb` + `IDEALAKEWMSWebService` (Ziele vorher geleert,
Exit 0/0), `SQL\`, `deploy\`, `publish-info.txt` (Version 1.99.71, Commit `47a23d32061e9bb630066456d281ae55382c5109`, Erstellt 2026-11-25 18:21).
Nachlauf-Einträge der Spec vorhanden.

### Manuelle Testliste (offen für den Menschen)

1. Gerald legt in `ake_TEST2026` die Testlager-Paare an (`TS-SAGE`/`TS-WMS` LTD2SF + LTD3HR-Paar, `TL-SAGE`, Technik-Paare je Stufe, Inventur-Paar).
2. Diese Lager per Lagerplatz-Abgleich (Quelle SData) nach `AKE_WMS_DEV` holen (oder INSERT wie im QA-Lauf).
3. V4: P-4 für `svc_idealakewms` auf `ake_TEST2026`; .l (ohne Recht, 10 Takte, ein fehlgeschlagener Lauf) → DBA-GRANT laut Deploy-Abschnitt.
4. Schalter an, .c/.d mit V3 Teil 3 (`SyncLogs` „Sage-Datenbank: …ake_TEST2026“), .d2 (10 Takte ohne Arbeit = 0 Läufe).
5. .e/.f (Sage-Dialog, Abgleich SData und View), .g, .h (`KHKTan` unverändert), .i, .j, .k Abnahmevergleich 0/0.
6. .m (Dienst nach Sage-Commit hart beenden), dabei N1 beobachten (Stopp während laufendem Befehl → Fehler statt Offen?).
7. .n Buchung/Abgleich mit Schalter aus.
8. Lager-Anlage .o–.s je Stufe (Mitschnitt: LTD0ST/LTD1BL, Alle: +LTD1UF/LTD2SF/LTD3HR), .s Abnahmevergleich je Paar 0/0, sonst Stufe bleibt `Aus`.
9. .u Lager-Konflikt, .v DBA-Fall `Hauptlager` (THROW 50005); AK 13: prüfen, ob der Konflikttext reicht (N2).
10. .b mit einem Benutzer nur `masterdata_read`.
11. .w: beide Schalter zurück, Aufräumliste der Sage-Testlager an den Sage-Anwender.
12. IDEAL: Testsystem `idealakeweb01:88` auf `IDEAL_TEST_2026_05_03`, P-1..P-9, V3, Benjamin legt Testlager an, TS-192 komplett.
13. Empfehlung: `SageLagerplatzAnlageAktiv = false` und `SageLagerAnlageLagertechniken = Aus` ins DEV-Sicherheits-SQL von `D:\Dev\scripts\dev-deploy.ps1` aufnehmen.
14. Testsystem-Lauf mit dem Publish-Paket, danach Merge (Schranke 2).

**Schlusslauf v1.99.79 (2026-11-25):** QA bestanden (F-5/F-8 Review OK; F-6 Konflikt-SELECT auf ake_TEST2026 nennt PlatzID 1; F-7 live NICHT MOEGLICH ohne Sage-Schreibschalter; Hinweis H-C STRING_AGG-Version am IDEAL-Sage-Server) - Nachweis [[2026-11-25-kommission-lagerplatz-bde-goal-lauf]], Abschnitt „QA Schlusslauf v1.99.79“.
