---
type: spec
title: Sage-100-Lagerbuchungen ueber SData-API (Material Zugang/Entnahme, Queue + Windows-Service)
slug: 2026-07-29-sage-lagerbuchungen-spec
status: Testbereit
created: 2026-07-29
updated: 2026-08-03
source_backlog: "[[2026-07-29-Postman-Lagerbuchungen]]"
task: "[[2026-07-29-sage-lagerbuchungen-umsetzung]]"
worktree: ".claude/worktrees/2026-07-29-sage-lagerbuchungen"
branch: "feature/2026-07-29-sage-lagerbuchungen"
affected_code:
  - IdealAkeWms/Models/StorageLocation.cs (neue Felder SageBuchungErlaubt, SageLagerkennung, SageLagerplatzId)
  - IdealAkeWms/Models/SageBookingQueueItem.cs (neu)
  - IdealAkeWms/Models/SageBookingQueueStatus.cs (neu)
  - IdealAkeWms/Data/Repositories/ISageBookingQueueRepository.cs (neu)
  - IdealAkeWms/Data/Repositories/SageBookingQueueRepository.cs (neu)
  - IdealAkeWms/Data/Repositories/IStockMovementRepository.cs / StockMovementRepository.cs (Decorator-Anschlusspunkt)
  - IdealAkeWms/Data/Repositories/SageBookingEnqueueingStockMovementRepository.cs (neu, Decorator)
  - IdealAkeWms/Data/ApplicationDbContext.cs (neuer DbSet<SageBookingQueueItem>)
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs (neue Keys SageLagerbuchungAktiv, SData-URL/Mandant, Intervall, Retry-Cap)
  - IdealAkeWms/Program.cs (Decorator-Registrierung IStockMovementRepository)
  - IdealAkeWms/Controllers/StorageLocationsController.cs (Edit/Create fuer SageBuchungErlaubt)
  - IdealAkeWms/Views/StorageLocations/Index.cshtml, Create.cshtml, Edit.cshtml (neue Spalte/Feld)
  - IdealAkeWms/Services/SyncLogger/SyncLogServices.cs (neuer Service-Name SageLagerbuchung)
  - IDEALAKEWMSService/Services/ISageLagerbuchungClient.cs (neu)
  - IDEALAKEWMSService/Services/SageLagerbuchungClient.cs (neu, typed HttpClient)
  - IDEALAKEWMSService/Services/SageLagerbuchungPayloadBuilder.cs (neu, reine Mapping-Logik)
  - IDEALAKEWMSService/Workers/SageBookingWorker.cs (neu, eigener BackgroundService mit Kurztakt)
  - IDEALAKEWMSService/Program.cs (DI-Registrierung, AddHttpClient, neuer Worker)
  - IDEALAKEWMSService/appsettings.json, appsettings.Development.json (Basic-Auth-Credentials, appsettings-only)
  - SQL/82_AddStorageLocationSageLagerbuchung.sql (neu)
  - SQL/83_AddSageBookingQueue.sql (neu)
  - SQL/00_FreshInstall.sql (beide Migrationen nachziehen: Schema + MigrationId)
  - IdealAkeWms.Tests/Services/SageLagerbuchungPayloadBuilderTests.cs (neu)
  - IDEALAKEWMSService.Tests/Services/SageBookingWorkerTests.cs (neu, Invariante ohne DB)
  - docs/TESTSZENARIEN.md (neues Kapitel Sage-Lagerbuchungen)
  - secondbrain/tests/testszenarien-index.md
  - secondbrain/codebase/integrationen.md, services.md, datenmodell.md (additiv nach Umsetzung)
  - Views/Help/Changelog.cshtml, IdealAkeWms/AppVersion.cs, IDEALAKEWMSService/AppVersion.cs (Versions-Bump)
open_questions:
  - "Buchungs-Auslöser-Scope: nur manuelle Ein-/Ausbuchung oder auch Umbuchung, kommissionierungsgetrieben?"
  - "Fast-live-Kadenz: Poll-Intervall in Sekunden bestätigen"
  - "Idempotenz/Timeout-Handling: reicht Statuswechsel offen→gesendet oder braucht es eine Korrelations-Id?"
  - Artikelnummer-Abgleich WMS Article.ArticleNumber == Sage Artikelnummer bestätigen
  - Mandant/dataset-Wert je Instanz (AKE vs. IDEAL) klären
  - "Fehler-/Retry-Politik: max. Versuche, Backoff, Fehlermail-Schwelle, manuelle Requeue-UI?"
  - Sage-Spalte für den numerischen LagerplatzId (SageLagerplatzReader liest sie heute nicht)
  - "Zone vs. neues Feld SageLagerkennung: Redundanz, da Zone die Lagerkennung heute schon speichert"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-08-03
---

## Ziel / Nutzen (das Warum)

Sage 100 stellt eine REST-Schnittstelle (SData, Common Wawi Services) fuer Lagerbuchungen bereit.
Heute laeuft die Bestandsintegration zwischen WMS und Sage **nur in eine Richtung**: Sage ist
fuehrend, `LagerbestandSyncService` korrigiert den WMS-Bestand nach Sage
(`IDEALAKEWMSService/Services/LagerbestandSyncService.cs:171-190`, `MovementType.SageEinbuchung`/
`SageAusbuchung`). Bucht ein Lagermitarbeiter im WMS eine Ein- oder Ausbuchung
(`StockMovementsController.Inbound`/`Outbound`), bleibt das in Sage **unsichtbar**, bis der naechste
Sage-eigene Bestandsabgleich (durch einen Menschen in Sage selbst) diesen Unterschied manuell
nachzieht — es gibt keinen automatisierten Weg vom WMS zurueck zu Sage.

Ziel dieser Spec ist die **Gegenrichtung**: manuelle Material-Zugaenge/-Entnahmen, die im WMS
gebucht werden, sollen automatisiert und zeitnah ("fast live") an Sage uebermittelt werden, damit
Sage den tatsaechlichen Lagerbestand ohne manuellen Zwischenschritt kennt. Schritt 1 ist bewusst
klein geschnitten: nur Material (kein BDE-Rueckmeldekanal), nur Zugang/Entnahme (keine
Seriennummern/Chargen), asynchron ueber eine Queue mit Status-Tracking (Robustheit,
Nachvollziehbarkeit, kein blindes Doppelbuchen bei einem Sage-Timeout).

## Ist-Zustand (verifiziert)

**Bestandsbuchungen im WMS** laufen ueber `StockMovement` (`IdealAkeWms/Models/StockMovement.cs`),
erbt `AuditableEntity`, mit `MovementType`-Enum (`IdealAkeWms/Models/MovementType.cs:3-10`):
`Einbuchung=0`, `Ausbuchung=1`, `Umbuchung=2`, `SageEinbuchung=3`, `SageAusbuchung=4`. Die letzten
beiden sind **ausschliesslich** Sage-Korrekturbuchungen und werden **nie** ueber das Repository
angelegt, sondern per direktem `_ctx.StockMovements.Add(...)` in
`IDEALAKEWMSService/Services/LagerbestandSyncService.cs:173-186` (Delta-Korrektur) und `:241-254`
(Nullsetzen verwaister Paare) — sie umgehen `IStockMovementRepository` vollstaendig.

**Manuelle Einbuchung/Ausbuchung** laufen **ausschliesslich** ueber
`IStockMovementRepository.AddAsync(...)`:
- `StockMovementsController.Inbound(POST)` (`IdealAkeWms/Controllers/StockMovementsController.cs:122-137`)
  → `MovementType.Einbuchung`.
- `StockMovementsController.Outbound(POST)` (`:221-236`) → `MovementType.Ausbuchung`.
- `StockMovementsController.OutboundAllConfirm` (`:385-399`, Bulk-Ausbuchung eines Lagerplatzes)
  → `MovementType.Ausbuchung`.
- `StockMovementsController.Transfer(POST)` (`:310-326`) und `LocationTransferConfirm`
  (`:466-480`) → **beide** `MovementType.Umbuchung`, ebenfalls ueber das Repository.

**Kommissionierungs-getriebene Umbuchungen** (`IdealAkeWms/Services/PickingTransferService.cs:293-300`,
`MovementType.Umbuchung`) legen `StockMovement` dagegen **direkt per `_context.StockMovements.Add(...)`**
an, **nicht** ueber `IStockMovementRepository`. Es gibt **keine** Codestelle, die eine
Kommissionierung als `Einbuchung`/`Ausbuchung` bucht — Kommissionierung erzeugt ausschliesslich
`Umbuchung` (Lagerplatz → Kommissionierwagen), und das nur per direktem DbContext-Zugriff.

**Konsequenz fuer den Hook-Punkt:** Ein Abfangen auf Ebene `IStockMovementRepository.AddAsync`
erfasst **genau** die manuellen Einbuchungen/Ausbuchungen (Inbound/Outbound/OutboundAllConfirm) —
und **automatisch nicht** die Sage-Korrekturbuchungen (die umgehen das Repository ohnehin) **und
nicht** die kommissionierungs-getriebenen Umbuchungen aus `PickingTransferService` (die umgehen das
Repository ebenfalls). Die beiden manuellen Umbuchungs-Actions (`Transfer`, `LocationTransferConfirm`)
liefen dagegen **wohl** durchs Repository und wuerden mitgefangen, wenn Umbuchung in Scope waere —
das ist Teil der offenen Rueckfrage 1.

**`StorageLocation`** (`IdealAkeWms/Models/StorageLocation.cs`) hat heute **kein** Flag „Sage-Buchung
erlaubt". Vorhandene Flags: `IsPickingTransport` (Z. 34, Kommissionierwagen), `IsActive` (Z. 41,
Sage-controlled seit dem Lagerplatz-Sync), `IstBuchbar` (Z. 44, user-controlled „darf gebucht
werden" — siehe `secondbrain/architektur/fallstricke.md` Abschnitt „`IsActive` vs `IstBuchbar`"),
`Source` (Z. 38, `Sage`/`Manual`). Migrations-Vorbild fuer ein neues Bool-Flag:
`SQL/58_AddStorageLocationIstBuchbar.sql`.

**Wichtiger Fund — Lagerkennung wird bereits persistiert, aber unter `Zone`:**
`IDEALAKEWMSService/Services/LagerplatzSyncService.cs:109` liest `dto.Lagerkennung` (aus
`SageLagerplatzReader.cs:24,40`, Sage-Spalte `KHKLagerorte.Lagerkennung`) und schreibt sie **bereits
heute** in `StorageLocation.Zone` (Anlage `:125`, Update `:151`). `Zone` ist die UI-Spalte
„Bereich/Zone" (`Views/StorageLocations/Index.cshtml:68,87`, Spaltenfilter-Key `"zone"` in
`StorageLocationsController.cs:34`) — ein frei editierbares Textfeld, **auch** fuer manuelle
(Nicht-Sage-)Lagerplaetze genutzt (`StorageLocationsController.cs:138`, Edit-POST uebernimmt
`location.Zone` fuer `Source == Manual`). Die Aussage der Aufgabenstellung, die Lagerkennung werde
„nicht persistiert", stimmt also nur fuer den **numerischen** Sage-`LagerplatzId` — die Kennung
selbst liegt bereits in `Zone`. Details und die daraus resultierende Design-Frage (neues,
dediziertes Feld vs. Wiederverwendung von `Zone`) siehe offene Rueckfrage 8.

**Der numerische Sage-`LagerplatzId`** (von der SData-API fuer `HerkunftLagerplatzId`/
`ZielLagerplatzId` benoetigt, verschieden von `Kurzbezeichnung`/`Code`) wird **an keiner Stelle**
gelesen: `SageLagerplatzReader.GetAllActiveAsync` (`IDEALAKEWMSService/Services/SageLagerplatzReader.cs:23-29`)
selektiert nur `Lagerkennung`, `Kurzbezeichnung`, `Platzbezeichnung` — keine ID-Spalte. Welche
Sage-Spalte diesen Wert liefert, ist unbekannt (siehe offene Rueckfrage 7).

**`Article.ArticleNumber`** (`IdealAkeWms/Models/Article.cs:7-10`) ist der Sage-Schlüssel der
`Articles`-Stammdatentabelle, befuellt aus der **UNION** von `KHKPpsRessourcenPositionen.Ressourcenummer`
und `KHKArtikel.Artikelnummer` (`IDEALAKEWMSService/Services/SageImportService.cs:382-421`) — der
bekannte Artikelnummer-vs-Ressourcenummer-Fallstrick (`secondbrain/architektur/fallstricke.md`
Abschnitt 1) betrifft ausschliesslich **BOM-Zeilen** (`vw_AKE_Kommissionierung_StuecklistenDB`, wo
beide Felder nebeneinanderstehen). `StockMovement.ArticleId` referenziert dagegen direkt `Article`
(Stammdaten), nicht eine BOM-Zeile — dort existiert die Verwechslungsgefahr strukturell nicht.
Trotzdem bleibt die 1:1-Uebereinstimmung `Article.ArticleNumber == Sage-`Artikelnummer`" eine
Annahme, die am Testsystem zu verifizieren ist (offene Rueckfrage 4).

**ServiceSettings** (ADR `0008-servicesettings-db-first-mit-typisiertem-katalog`): Katalog
`IdealAkeWms/Models/ServiceSettingDefinitions.cs:16-71` (`.All`, 36 Keys, Typ Bool/Int/String je
Eintrag), Record `IdealAkeWms/Models/ServiceSettingDefinition.cs`, Service-seitiger Reader
`IDEALAKEWMSService/Common/ServiceSettings.cs` (`GetBoolSafeAsync`/`GetIntSafeAsync`/
`GetValueSafeAsync`, fangen transiente DB-Fehler ab). Drift-Guard-Test
`ServiceSettingDefinitionsTests.All_ContainsDocumentedServiceReadKey` erzwingt einen Katalog-Eintrag
je gelesenem Key. Ausnahmen bleiben appsettings-only: `ConnectionStrings:*`, `MailSettings:*`,
`Security:AdDomain` (ADR 0008) — **kein** `IDataProtector`/DPAPI im Projekt, `PasswordService.cs`
hasht nur (PBKDF2, einwegig, fuer Basic-Auth-Credentials unbrauchbar).

**Typed-HttpClient-Vorbild:** `IDEALAKEWMSService/Program.cs:74-80` registriert
`AddHttpClient<IHolidaySyncService, HolidaySyncService>` mit `BaseAddress` + `Timeout=30s`; Konsum
in `HolidaySyncService.cs:98-99` (`_http.GetFromJsonAsync<...>`). Das ist ein reines
**GET**-Vorbild — POST-JSON mit Basic-Auth-Header und eigenem Retry/Idempotenz-Handling sind fuer
diese Integration **neu** (kein Polly im Projekt).

**Worker-Architektur:** `SyncWorker` (`IDEALAKEWMSService/Workers/SyncWorker.cs`) ist ein
`BackgroundService` mit `while(!stoppingToken.IsCancellationRequested) { ...; await
Task.Delay(TimeSpan.FromMinutes(intervalMinutes)); }`, Bloecke einzeln resilient gekapselt
(`RunResilientAsync`). Alle heutigen Worker-Takte sind **minutenskaliert**
(`WorkerSettings:SyncIntervalMinutes` Default 15). Es gibt bereits **drei** unabhaengige
`BackgroundService`-Registrierungen (`SyncWorker`, `NotificationWorker`, `CleanupWorker`,
`IDEALAKEWMSService/Program.cs:83-85`) — ein "fast-live"-Sekundentakt hat **kein** Vorbild in
diesem Takt-Schema.

**`ISyncLogger`** (ADR `0010-aktivitaets-protokoll-mit-isolierten-dbcontexts`):
`IdealAkeWms/Services/SyncLogger/` (`ISyncLogger.BeginRunAsync` → `ISyncRun.LogInfoAsync`/
`LogWarningAsync`/`LogErrorAsync` → `FinishSuccessAsync`/`FinishFailedAsync`), Singleton mit
`IDbContextFactory<ApplicationDbContext>` (Protokoll ueberlebt Rollback der Sync-Transaktion).
Service-Namen sind Konstanten in `SyncLogServices.cs:10-31` — ein neuer Name (`SageLagerbuchung`)
muss dort ergaenzt werden, sonst ist der Lauf im Aktivitaets-Protokoll-Filter nicht waehlbar.
Konvention: `ISyncLogger` letzter Ctor-Parameter, Connection-String-Validierung **innerhalb** des
try-Blocks nach `BeginRunAsync`.

**Repository/DI:** `IStockMovementRepository` ist im Web-Projekt **ohne** Decorator registriert
(`IdealAkeWms/Program.cs:69`: `AddScoped<IStockMovementRepository, StockMovementRepository>()`).
Das bestehende Decorator-Muster (ADR `0001-repository-pattern-mit-decorator-fuer-caching`) zeigt
den Umbau: konkrete Klasse separat registrieren, Interface auf den Decorator zeigen lassen (Vorbild
`AppSettingRepository`/`CachedSettingRepository`, `IdealAkeWms/Program.cs:76-77`).

**Rollen fuer Lagerbuchungen** (`secondbrain/codebase/controller.md:60-63`): `[RequireStockAccess]`
(admin, stock, stock_keyuser, picking — Schreib-Actions Ein-/Aus-/Umbuchung),
`[RequireStockReadAccess]` (zusaetzlich stock_read — Lese-Zugriff Bestand/Historie),
`[RequireStockKeyUserAccess]` (admin, stock_keyuser, picking — Lagerplatz ausbuchen/umbuchen en
bloc). `StorageLocationsController` ist `[RequireMasterDataReadAccess]` class-level,
`[RequireMasterDataAccess]` auf den Edit-Actions (`StorageLocationsController.cs:11,77,85,101,113`).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:**
- Neues Flag `StorageLocation.SageBuchungErlaubt` (pro Lagerplatz, user-controlled, kumulativ zum
  globalen Toggle) + zwei neue Sage-Referenzfelder (`SageLagerkennung`, `SageLagerplatzId`),
  befuellt durch eine Erweiterung von `LagerplatzSyncService`/`SageLagerplatzReader`.
- Neuer globaler ServiceSetting-Toggle `SageLagerbuchungAktiv` (Default `false`) sowie
  Konfigurationswerte fuer SData-Basis-URL, Mandant (`dataset`), Poll-Intervall und Retry-Cap —
  alle DB-first ueber `ServiceSettingDefinitions.All`.
- Basic-Auth-Credentials (User + Passwort) **appsettings-only** im Service-Projekt (ADR 0008).
- Neue Queue-Tabelle `SageBookingQueueItems` mit Status-Automat
  offen → gesendet → bestaetigt/fehler, FK auf `StockMovement`.
- Erzeugung eines Queue-Eintrags fuer **manuelle** `Einbuchung`/`Ausbuchung`
  (`StockMovementsController.Inbound`/`Outbound`/`OutboundAllConfirm`), gated durch **beide**
  Bedingungen: globaler Toggle **und** `StorageLocation.SageBuchungErlaubt` am beteiligten
  Lagerplatz. **Nie** fuer `SageEinbuchung`/`SageAusbuchung` (Feedback-Loop-Schutz — siehe Ist-Zustand:
  diese umgehen das Repository ohnehin strukturell, zusaetzlich expliziter Typ-Check als
  Verteidigung in der Tiefe).
- Neuer, eigener `BackgroundService` (`SageBookingWorker`) im Windows-Service mit kurzem Poll-Takt,
  der offene Queue-Eintraege an Sage sendet und den Status fortschreibt — inkl. `ISyncLogger`-Lauf,
  Fehlermail bei Cap-Ueberschreitung.
- `ISageLagerbuchungClient` (typed HttpClient) + `SageLagerbuchungPayloadBuilder` (reine, testbare
  Mapping-Logik StockMovement/Article/StorageLocation → SData-JSON).
- Migrations-/SQL-Artefakte nach ADR 0004 fuer beide Schema-Aenderungen.
- Testszenarien-Kapitel, Brain-Updates (nach Freigabe/Umsetzung durch den Dev-Lauf).

**Out-of-Scope (Step 1, explizit laut Backlog):**
- Serien- und Chargenverwaltung (`Seriennummern`/`Chargen`-Arrays werden **leer** mitgeschickt,
  analog zum Postman-Sample) — spaeterer Step.
- BDE-Rueckmeldung an Sage — eigener, separater Kanal, eigene (noch ausstehende)
  Schnittstellendoku.
- `Auspraegung`/Artikelvarianten (`AuspraegungHandle` wird fix `0` gesendet).
- Umbuchung (`MovementType.Umbuchung`, Wert `3` in der SData-Enum) — **ENTSCHIEDEN out** (Schranke 1,
  Antwort B1: „Umbuchung herauslassen", 2026-08-03). Der Decorator-Filter bleibt bei
  `Einbuchung`/`Ausbuchung`; die manuellen Umbuchungs-Actions (`Transfer`, `LocationTransferConfirm`)
  erzeugen **keinen** Queue-Eintrag. Umbuchung wird — falls je gewuenscht — ein sauber definierter
  spaeterer Step (inkl. beider Lagerplatz-Enden, Gating-Regel, Behandlung same-Lagerkennung-Moves).
- Ruecklesen/Abgleich der Sage-Stammdaten `GET Adressen`/`GET Artikel`/`$schema` als eigener Sync —
  dient hier nur der manuellen Verifikation der offenen Rueckfragen, kein Feature dieser Spec.
- Aenderung von `LagerbestandSyncService` (Sage→WMS-Korrektur) — bleibt unangetastet, ist die
  Gegenrichtung.

## Fachliche Anforderungen

1. Ein Admin kann pro Lagerplatz (`StorageLocationsController` Create/Edit) festlegen, ob
   Buchungen dieses Platzes an Sage gemeldet werden duerfen (`SageBuchungErlaubt`), unabhaengig vom
   bestehenden `IstBuchbar`-Flag (das bleibt „darf im WMS gebucht werden", `SageBuchungErlaubt` ist
   „darf zusaetzlich an Sage gemeldet werden").
2. Ein globaler Schalter (`SageLagerbuchungAktiv`, ServiceSettings) schaltet die gesamte Integration
   stumm — ohne ihn wird **nichts** in die Queue geschrieben, unabhaengig vom Lagerplatz-Flag
   (kumulative UND-Verknuepfung, nicht ODER).
3. Wird eine manuelle Einbuchung oder Ausbuchung gespeichert **und** sind beide Bedingungen (2) und
   (1) fuer den beteiligten Lagerplatz erfuellt, entsteht **automatisch und ohne weiteres Zutun des
   Anwenders** ein Queue-Eintrag mit Status `offen`.
4. Der Windows-Service verarbeitet offene Queue-Eintraege in kurzer Taktfrequenz (Sekunden- statt
   Minutenbereich, siehe offene Rueckfrage 2) und sendet sie einzeln an
   `POST {{sdata_base_url}}/{{sdata_servicecontract}}/{{dataset}}/$service/LagerbuchungService`.
5. `Einbuchung` wird als `Lagerbewegungsart: "Zugang"` mit gesetztem Ziel-Lagerplatz (leerer
   Herkunft) gesendet; `Ausbuchung` als `"Entnahme"` mit gesetztem Herkunfts-Lagerplatz (leeres
   Ziel) — exakt spiegelbildlich zu den beiden Postman-Beispielen.
6. `Artikelnummer` im Payload ist `Article.ArticleNumber` des gebuchten Artikels; `MengeLager` ist
   `StockMovement.Quantity`; `Seriennummern`/`Chargen` werden als leere Arrays mitgeschickt
   (Schema-Konformitaet ohne fachlichen Inhalt).
7. Jede Buchung ist nachvollziehbar: wer (WindowsUser/AppUser aus dem urspruenglichen
   `StockMovement`), wann (Timestamp der Buchung **und** Zeitpunkt des Sendens/Bestaetigens), was
   (Artikel/Menge/Lagerplatz) **und** die rohe Sage-Antwort (Erfolg oder Fehlertext) sind am
   Queue-Eintrag ablesbar.
8. Ein Fehler beim Senden (Netzwerk, Sage-Fehlerantwort, Timeout) darf **nie** zu einer
   automatischen Doppelbuchung fuehren — der Status bleibt nachvollziehbar `fehler` bis zur
   naechsten (manuellen oder automatischen, siehe Rueckfrage 6) Wiederholung.
9. `SageEinbuchung`/`SageAusbuchung` (Sage→WMS-Korrekturen) erzeugen **nie** einen Queue-Eintrag —
   das waere ein Feedback-Loop (Sage korrigiert WMS, WMS meldet die Korrektur an Sage zurueck).

## Technischer Loesungsentwurf

**Muster:** Repository-Pattern + Decorator (ADR 0001) fuer den Enqueue-Hook, DB-first
ServiceSettings (ADR 0008) fuer Konfiguration/Toggle, Aktivitaets-Protokoll (ADR 0010) fuer den
Sende-Lauf, Migrations-Dreiklang (ADR 0004) fuer beide Schema-Aenderungen. Kein neues
Architektur-Muster — Anwendung dreier bestehender Muster auf eine neue, ausgehende Integration.

### 1. Stammdaten-Erweiterung `StorageLocation`

Neue Properties (Migration `AddStorageLocationSageLagerbuchung`):
```csharp
[Display(Name = "Sage-Buchung erlaubt")]
public bool SageBuchungErlaubt { get; set; }        // Default false, user-controlled

[StringLength(50)]
public string? SageLagerkennung { get; set; }        // KORRIGIERT (Schranke 1, s.u.): volle Sage-Kurzbezeichnung inkl. Ebenen, z.B. "LL;1;4;0" — identisch zu StorageLocation.Code fuer Sage-Plaetze

public int? SageLagerplatzId { get; set; }            // Sage-interne numerische Id = KHKLagerplaetze.PlatzID
```

> **KORREKTUR nach Schranke-1-Antwort B3 (2026-08-03):** Die urspruengliche Annahme „Kennung ohne
> Ebenen-Suffix + hartes `;0;0;0`" ist **falsch**. Der Screenshot zeigt: die adressierbare
> Kurzbezeichnung eines realen Platzes ist `LL;1;4;0` (Format `Lagerkennung;Reihe;Platz;Ebene`,
> **Semikolon**), und `LL;0;0;0` ist ein **anderer** Datensatz (die Lager-Wurzel). Diese volle
> Kurzbezeichnung liegt bereits in `StorageLocation.Code` (`LagerplatzSyncService.cs:85,91`,
> `code = dto.Kurzbezeichnung`). Der Payload muss diese **volle** Zeichenkette senden, nicht
> `<bare-Lagerkennung>;0;0;0`. Details siehe konsolidierten Abschnitt am Ende.
`SageBuchungErlaubt` ist **kumulativ** zu `IstBuchbar` und **unabhaengig** von `IsActive`/`Source` —
ein Admin kann es theoretisch auch fuer einen manuellen (Nicht-Sage-)Lagerplatz setzen, was aber
ins Leere liefe, weil `SageLagerkennung`/`SageLagerplatzId` dort nie befuellt werden (kein
Sage-Sync fuer manuelle Plaetze). Der Sende-Pfad muss das defensiv abfangen (Akzeptanzkriterium 9).

`LagerplatzSyncService.RunAsync` (`IDEALAKEWMSService/Services/LagerplatzSyncService.cs`) wird um
das Schreiben von `SageLagerkennung` (aus `dto.Lagerkennung`, **zusaetzlich** zum bestehenden
`Zone`-Schreiben — nicht als Ersatz, siehe offene Rueckfrage 8) und `SageLagerplatzId` (neue Spalte
in `SageLagerplatzDto`, sobald die Sage-Quellspalte geklaert ist, siehe offene Rueckfrage 7)
erweitert; `SageLagerplatzReader.GetAllActiveAsync` (`IDEALAKEWMSService/Services/SageLagerplatzReader.cs:23-29`)
muss die zusaetzliche Spalte selektieren.

### 2. Queue-Tabelle `SageBookingQueueItem`

```csharp
public class SageBookingQueueItem : AuditableEntity
{
    public int StockMovementId { get; set; }
    public StockMovement StockMovement { get; set; } = null!;

    public SageBookingQueueStatus Status { get; set; } = SageBookingQueueStatus.Offen;
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
    public string? SageResponseRaw { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

public enum SageBookingQueueStatus { Offen = 0, Gesendet = 1, Bestaetigt = 2, Fehler = 3 }
```
`AuditableEntity` liefert `CreatedAt`/`CreatedBy`/`CreatedByWindows` (beim Enqueue, aus dem
urspruenglichen `StockMovement`-Kontext bzw. `"system:sync"` analog `LagerplatzSyncService.cs:15`)
und `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` (bei jedem Statuswechsel, Service schreibt einen
Service-Namen, analog `SyncUser`-Konstanten in den bestehenden Sync-Services). Repository
`ISageBookingQueueRepository`/`SageBookingQueueRepository` (Repository-Pattern, kein raw SQL):
`EnqueueAsync(StockMovement)`, `GetOpenBatchAsync(int max)`, `MarkSentAsync`, `MarkConfirmedAsync`,
`MarkFailedAsync`.

### 3. Enqueue-Hook: Decorator auf `IStockMovementRepository`

`SageBookingEnqueueingStockMovementRepository : IStockMovementRepository` umschliesst die
konkrete `StockMovementRepository` (Vorbild `CachedSettingRepository`,
`IdealAkeWms/Program.cs:76-77`) und ueberschreibt **nur** `AddAsync`:
```csharp
public override async Task<StockMovement> AddAsync(StockMovement entity)
{
    var saved = await _inner.AddAsync(entity);
    if (saved.MovementType is MovementType.Einbuchung or MovementType.Ausbuchung)
        await _enqueueDecision.MaybeEnqueueAsync(saved); // prueft globalen Toggle + SageBuchungErlaubt
    return saved;
}
```
Registrierung: `IdealAkeWms/Program.cs:69` von
`AddScoped<IStockMovementRepository, StockMovementRepository>()` auf das Decorator-Paar umstellen
(konkrete Klasse separat registrieren, Interface zeigt auf den Decorator). Dieser Hook fasst
**automatisch nur** die manuellen Einbuchungen/Ausbuchungen (siehe Ist-Zustand) — Sage-Korrekturen
und kommissionierungs-getriebene Umbuchungen bleiben unberuehrt, weil sie das Repository gar nicht
durchlaufen. Zusaetzlich ein expliziter `MovementType`-Filter im Decorator selbst als zweite,
unabhaengige Sicherung (Verteidigung in der Tiefe, falls sich ein Aufrufer in Zukunft aendert).
Die Entscheidungslogik (`IsEnabled = globalToggle && location.SageBuchungErlaubt`) liegt in einem
eigenen, reinen Helper (analog `PrePickingDaysResolver`/`ProductionOrderReconciler` — pruefbar ohne
DB-Zugriff mit gemocktem Repository).

### 4. Sende-Pfad: neuer Worker im Windows-Service

`SageBookingWorker : BackgroundService` (eigene Registrierung neben `SyncWorker`,
`NotificationWorker`, `CleanupWorker`, `IDEALAKEWMSService/Program.cs:83-85`) mit eigenem, kurzem
Takt (`Sync:SageLagerbuchungIntervalSeconds`, Sekunden statt Minuten — siehe offene Rueckfrage 2).
Ein eigener Worker statt Integration in den bestehenden `SyncWorker`-Minutentakt, weil (a) alle
bestehenden Sync-Bloecke minutenskaliert sind und ein Umbau des gemeinsamen Takts alle anderen
Bloecke mitreissen wuerde, und (b) die bestehende Kapselung „Sync-Bloecke sind einzeln gekapselt"
(`secondbrain/architektur/fallstricke.md` Abschnitt 5) genau fuer diese Isolation steht — ein
Sage-Ausfall bei den Lagerbuchungen darf die anderen Sync-Bloecke nicht verlangsamen und umgekehrt.
Ablauf je Tick:
1. `ServiceSettings.GetBoolSafeAsync(config, "SageLagerbuchungAktiv", false)` — Gate.
2. `ISyncLogger.BeginRunAsync(SyncLogServices.SageLagerbuchung)`.
3. `ISageBookingQueueRepository.GetOpenBatchAsync(maxBatchSize)` — offene Eintraege laden.
4. Je Eintrag: Status **vor** dem HTTP-Call auf `Gesendet` setzen und speichern (Idempotenz-Baustein,
   siehe offene Rueckfrage 3), dann `ISageLagerbuchungClient.SendAsync(payload)` aufrufen.
5. Erfolg → `Bestaetigt` + rohe Antwort; Fehler/Timeout → `Fehler` + `LastError` +
   `AttemptCount++`; Cap-Ueberschreitung (zu viele Fehler je Lauf) → `ISyncErrorNotifier` (Vorbild
   `LagerbestandSyncService.cs:219-227`).
6. `FinishSuccessAsync`/`FinishFailedAsync` mit deutschen Counts-Keys (`gesendet`, `bestaetigt`,
   `fehler`, `uebersprungen`).

### 5. `ISageLagerbuchungClient` + `SageLagerbuchungPayloadBuilder`

Typed HttpClient (Vorbild `IDEALAKEWMSService/Program.cs:74-80`, `AddHttpClient<ISageLagerbuchungClient,
SageLagerbuchungClient>`), `BaseAddress` aus ServiceSettings (`SData:BaseUrl`) **zur Laufzeit** pro
Request gesetzt (nicht beim DI-Setup, da DB-first — analog zum Options-Pattern in
`HolidaySyncService.cs:64-81`, wo DB-Werte die `IOptions`-Defaults zur Laufzeit ueberschreiben,
lokale Variablen statt Mutation der Singleton-Options-Instanz), Basic-Auth-Header aus
`IConfiguration` (appsettings, `SageLagerbuchung:Username`/`SageLagerbuchung:Password`), Timeout
30s (Vorbild). `SageLagerbuchungPayloadBuilder` baut das JSON rein funktional aus
`StockMovement`+`Article`+`StorageLocation`(n) — **kein** HTTP, dadurch ohne DB/Netzwerk
unit-testbar:
```json
{
  "Memo": "IdealAkeWms StockMovement #<Id>",
  "Standardtext": "Zugang Material" | "Abgang / Entnahme Material",
  "Lagerbuchungen": [{
    "Lagerbewegungsart": "Zugang" | "Entnahme",
    "Artikelnummer": "<Article.ArticleNumber>",
    "AuspraegungHandle": 0,
    "HerkunftLagerkennung": "" | "<StorageLocation.Code, z.B. LL;1;4;0>",   // KORRIGIERT B3: volle Kurzbezeichnung, KEIN ";0;0;0"-Suffix
    "HerkunftLagerplatzId": 0 | <SageLagerplatzId = PlatzID>,
    "ZielLagerkennung": "<StorageLocation.Code, z.B. LL;1;4;0>" | "",       // KORRIGIERT B3
    "ZielLagerplatzId": <SageLagerplatzId = PlatzID> | 0,
    "MengeLager": <Quantity>,
    "Seriennummern": [{ "Seriennummer": "" }],
    "Chargen": [{ "Charge": "", "Menge": 0.0, "Verfallsdatum": null }]
  }]
}
```
Die `Memo`-Einbettung der WMS-internen `StockMovement.Id` ist ein pragmatischer
Korrelations-Mechanismus (die Postman-Collection dokumentiert kein dediziertes Idempotenz-Feld) —
ob Sage `Memo` in der Antwort spiegelt oder anderweitig abfragbar macht, ist am Testsystem zu
pruefen (offene Rueckfrage 3).

### 6. Konfiguration

Neue `ServiceSettingDefinitions.All`-Eintraege (Kategorie z. B. `"SageLagerbuchung"`):
`SageLagerbuchungAktiv` (Bool, `false`), `SData:BaseUrl` (String, leer), `SData:Dataset` (String,
leer, Mandant), `Sync:SageLagerbuchungIntervalSeconds` (Int, Vorschlag `20`),
`Sync:SageLagerbuchungMaxRetries` (Int, Vorschlag `5`, siehe Rueckfrage 6). Neuer appsettings-Block
in `IDEALAKEWMSService/appsettings.json` (Vorbild `MailSettings`-Block, Z. 8-16):
```json
"SageLagerbuchung": { "Username": "", "Password": "" }
```
appsettings-only nach ADR 0008 (Geheimnis, nicht UI-editierbar) — muss am Server **einmalig** manuell
ergaenzt werden (wie `ConnectionStrings`/`MailSettings` heute schon).

#### Nachtrag (2026-08-03): TLS-Zertifikatspruefung schaltbar

Der Sage-Testserver (`sagetest01.ake.at`) hat derzeit kein gueltiges Zertifikat (`PartialChain`,
interne PKI noch nicht fertig). Analog zu Postmans „Enable SSL certificate verification" gibt es
einen expliziten Schalter:

- **ServiceSetting** `SageLagerbuchungSslZertifikatPruefen` (Bool, **Default `true`**, Kategorie
  „Sage-Lagerbuchung"). **Positiver** Schluesselname. **Fail-safe:** fehlt der Wert oder ist er
  nicht parsebar, wird **geprueft** (nicht fail-open). Reine Logik in
  `IdealAkeWms/Services/SageTlsPolicy.cs` (`ShouldVerifyCertificate` = true, sofern nicht explizit
  `false` parsebar — bewusst **nicht** `ServiceSettings.GetBoolAsync`, dessen Semantik fail-open waere).
- **Wirkung ausschliesslich** auf den typisierten `ISageLagerbuchungClient` via
  `ConfigurePrimaryHttpMessageHandler` (`HttpClientHandler.ServerCertificateCustomValidationCallback`).
  **Kein** `ServicePointManager`, keine globale Aenderung, **keine** Auswirkung auf
  OSEON/enaio/HolidaySync oder sonstige Clients.
- **Laufzeit-Gueltigkeit (wichtig):** Der Primary Handler wird bei der DI-Registrierung erzeugt und
  gepoolt. Der Wert wird deshalb **nicht** einmalig beim Registrieren gelesen, sondern **innerhalb des
  Validation-Callbacks** aus der aktuellen Konfiguration (`ServiceSettings.GetValueSafeAsync` ueber den
  im Closure gehaltenen `IServiceProvider`/`IConfiguration`). Eine Aenderung greift damit **ohne
  Dienst-Neustart** (spaetestens beim naechsten TLS-Handshake / neuer Verbindung; bestehende gepoolte
  Verbindungen laufen aus).
- **Sichtbarkeit bei `false`:** (a) `SageBookingWorker` loggt beim Start eine Warnung
  („TLS-Zertifikatspruefung fuer Sage-Lagerbuchungen ist DEAKTIVIERT - nur fuer Testsysteme
  zulaessig"); (b) `/ServiceSettings` zeigt am Eintrag einen Warnhinweis; (c) die Monitoring-Liste
  `/SageBookingQueue` zeigt ein Warn-Banner oben. Kein stilles Kaestchen.
- **Test:** `SageTlsPolicyTests` sichert die Invariante (Default `true`; fehlender/ungueltiger Wert →
  `true`; nur explizit `false` → `false`; Katalog-Default = `true`).

### 7. Optionale Monitoring-UI

Empfehlung: minimale Read-only-Liste `/SageBookingQueue` (Listen-View-Pattern, ADR 0005) unter
`[RequireStockReadAccess]`, mit einer „erneut senden"-Aktion fuer `Fehler`-Eintraege unter
`[RequireStockKeyUserAccess]` (bestehende Rollen wiederverwenden statt eine neue anzulegen — analog
zur bestehenden Rollenaufteilung fuer Lagerbuchungen). Ob diese UI in Step 1 gebraucht wird, haengt
an offener Rueckfrage 6 (manuelle Requeue-Faehigkeit gewuenscht?).

## Migrations-/SQL-Auswirkungen

Zwei Schema-Aenderungen, beide nach dem Migrations-Dreiklang (ADR 0004, naechste freie SQL-Nummern
nach `81_AddProductionOrderExtraInfo.sql`):

1. **`AddStorageLocationSageLagerbuchung`**: `StorageLocations` erhaelt `SageBuchungErlaubt BIT NOT
   NULL DEFAULT 0`, `SageLagerkennung NVARCHAR(50) NULL`, `SageLagerplatzId INT NULL`.
   `SQL/82_AddStorageLocationSageLagerbuchung.sql` mit `OBJECT_ID`/`COL_LENGTH`-Guard (`IF
   COL_LENGTH('dbo.StorageLocations', 'SageBuchungErlaubt') IS NULL BEGIN ALTER TABLE ... END`,
   DDL in eigenem Batch), danach `__EFMigrationsHistory`-Insert in separatem Batch.
2. **`AddSageBookingQueue`**: neue Tabelle `SageBookingQueueItems` (FK `StockMovementId` →
   `StockMovements.Id`, Index auf `Status` fuer den Worker-Read-Pfad).
   `SQL/83_AddSageBookingQueue.sql` mit `OBJECT_ID`-Guard, Tabellen-DDL in eigenem Batch (SQL
   Server parst Batches vollstaendig vorab — Referenz auf eine im selben Batch neu erstellte Tabelle
   scheitert sonst, siehe `secondbrain/architektur/fallstricke.md` Abschnitt 8), danach
   `__EFMigrationsHistory`-Insert separat.
3. **`SQL/00_FreshInstall.sql`** an **beiden** Stellen je Migration nachziehen: (a) Schema-Objekte
   im konsolidierten Schema (`StorageLocations`-Spalten + neue `SageBookingQueueItems`-Tabelle), (b)
   beide `MigrationId`s im `__EFMigrationsHistory`-INSERT-Block.
4. **`SQL/AgentJobs/`**: **nicht betroffen** — verifiziert, es existieren nur
   `01_Import_Produktionsauftraege.sql` und `02_Import_Artikel.sql`; der Lagerplatz-Sync laeuft als
   C#-Service (`LagerplatzSyncService`), nicht als SQL-Agent-Job.
5. Keine Migration ist daten-destruktiv (nur additive Spalten + neue Tabelle) — kein DB-Backup vor
   Deploy im Sinne von ADR 0004 zwingend, aber wie bei jedem Produktiv-Deploy empfohlen.

## Audit-Feld-Auswirkungen

`StorageLocation` bleibt `AuditableEntity`; die neuen Felder folgen dem bestehenden Speicherpfad
(`ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` beim Edit, `StorageLocationsController.Edit(POST)`
Zeilen 146-148, und beim Sage-Sync-Update, `LagerplatzSyncService.cs:155-157`). `SageBookingQueueItem`
ist ebenfalls `AuditableEntity`: `CreatedAt`/`CreatedBy`/`CreatedByWindows` beim Enqueue (aus dem
Web-Request-Kontext via `ICurrentUserService`, analog zu `StockMovement` selbst),
`ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` bei **jedem** Statuswechsel im Service — der Service
schreibt seinen Service-Namen (Konvention `"system:sage-booking"` o. ae., analog `SyncUser`-Konstanten
in `LagerplatzSyncService.cs:15`/`LagerbestandSyncService.cs:14`), **nie** `DateTime.UtcNow`
(Timestamp-Konvention Lokalzeit, `secondbrain/architektur/fallstricke.md` Abschnitt 5).

## Auswirkung auf Rollen/Zugriffsfilter

`StorageLocationsController` bleibt `[RequireMasterDataReadAccess]`/`[RequireMasterDataAccess]`
(Read/Edit-Split unveraendert) — `SageBuchungErlaubt` ist ein normales Stammdatenfeld wie
`IstBuchbar`, keine neue Rolle noetig. Fuer eine optionale Monitoring-UI (Abschnitt „Technischer
Loesungsentwurf" Punkt 7) Wiederverwendung von `[RequireStockReadAccess]` (Lesen) und
`[RequireStockKeyUserAccess]` (Requeue-Aktion) — **keine** neue Rolle, keine Aenderung an
`secondbrain/codebase/controller.md` oder `Views/Users/RoleOverview.cshtml` noetig, sofern die
Rueckfragen das bestaetigen.

## Listen-View-Pattern-Pflichten

**`StorageLocations/Index`** (bestehende Liste, `Views/StorageLocations/Index.cshtml`): neue Spalte
„Sage-Buchung" mit `data-col-key="sage-buchung"` (Pflicht-Pattern, ADR 0005) und passendem
`ColumnMap`-Eintrag in `StorageLocationsController.cs:30-41` (Getter liefert den **gerenderten**
Text „Ja"/„Nein", analog `["bookable"]` Zeile 38); `colCount`-Berechnung (Zeile 11) um 1 erhoehen.
Diese Liste ist bereits Pattern-konform (Pagination, serverseitiger Spaltenfilter ueber
`ColumnFilterHelper.Apply` **vor** Pagination, `StorageLocationsController.cs:60-61`) — keine
strukturelle Aenderung, nur eine zusaetzliche Spalte.

**Neue `/SageBookingQueue`-Liste** (falls laut Rueckfrage 6 gewuenscht): volle Pattern-Pflicht nach
ADR 0005 — `PageSize.Resolve` + `PaginationState` + `_Pagination`-Partial, Filterkarte
(Status/Zeitraum/Artikel), Server-Mode-Spaltenfilter (`data-server-column-filter="true"`, jedes
`<th>` mit `data-col-key`, `ColumnFilterHelper.ReadFromQuery`), Datumsspalten (`CreatedAt`,
`SentAt`, `ConfirmedAt`) **nach** etwaiger Berechnung in C# gefiltert.

## Auswirkung auf Hintergrund-Services

Neuer, vierter `BackgroundService` (`SageBookingWorker`) neben `SyncWorker`, `NotificationWorker`,
`CleanupWorker` (`IDEALAKEWMSService/Program.cs:83-85`) — eigener Kurztakt, eigene
`ISyncLogger`-Laeufe (neuer Konstantenname in `SyncLogServices.cs`), eigene Fehlermail-Schwelle
(`ISyncErrorNotifier`, Vorbild `LagerbestandSyncService.cs:219-227`). Bestehende Worker/Sync-Bloecke
bleiben unangetastet — insbesondere `LagerbestandSyncService` (Gegenrichtung) und
`LagerplatzSyncService` (dort nur additive Erweiterung um zwei Felder, keine Verhaltensaenderung
am bestehenden Sync-Ablauf).

## Akzeptanzkriterien

1. Ein Admin kann in `StorageLocations/Edit` das Feld „Sage-Buchung erlaubt" pro Lagerplatz setzen
   und speichern; die Liste zeigt eine neue, filterbare Spalte mit „Ja"/„Nein".
2. Ist `SageLagerbuchungAktiv` (ServiceSettings) `false`, entsteht **bei keiner** Buchung ein
   Queue-Eintrag — unabhaengig vom Lagerplatz-Flag.
3. Ist `SageLagerbuchungAktiv` `true`, aber `SageBuchungErlaubt` am beteiligten Lagerplatz `false`,
   entsteht **kein** Queue-Eintrag fuer diese Buchung.
4. Sind beide Bedingungen erfuellt, entsteht bei einer manuellen Einbuchung
   (`StockMovementsController.Inbound`) automatisch ein Queue-Eintrag mit Status `offen`, FK auf den
   korrekten `StockMovement`.
5. Dasselbe gilt fuer eine manuelle Ausbuchung (`Outbound`) und die Bulk-Ausbuchung
   (`OutboundAllConfirm`) je betroffenem Artikel.
6. Eine Sage-Korrekturbuchung (`SageEinbuchung`/`SageAusbuchung`, ausgeloest durch
   `LagerbestandSyncService`) erzeugt **nie** einen Queue-Eintrag — unabhaengig von den beiden
   Toggle-Zustaenden (Feedback-Loop-Schutz, unit-testbar durch direkten Aufruf des
   Enqueue-Entscheidungs-Helpers mit einer `SageEinbuchung`-Instanz).
7. Der an Sage gesendete Payload einer Einbuchung enthaelt `Lagerbewegungsart: "Zugang"`, das Ziel
   (Lagerkennung+LagerplatzId) gesetzt, die Herkunft leer/`0` — isoliert unit-getestet im
   `SageLagerbuchungPayloadBuilder` ohne HTTP/DB.
8. Der Payload einer Ausbuchung enthaelt spiegelbildlich `"Entnahme"`, Herkunft gesetzt, Ziel
   leer/`0` — ebenfalls isoliert unit-getestet.
9. Ist `SageBuchungErlaubt=true`, aber `SageLagerkennung`/`SageLagerplatzId` des Lagerplatzes
   `null` (z. B. versehentlich auf einem manuellen Lagerplatz gesetzt), wird der Queue-Eintrag mit
   Status `Fehler` und einer sprechenden Fehlermeldung markiert statt eine ungueltige Anfrage zu
   senden oder den Worker abstuerzen zu lassen.
10. Nach erfolgreichem Senden wechselt der Status **vor** dem HTTP-Call auf `gesendet` (nicht erst
    danach) — ein simulierter Timeout nach dem Senden fuehrt beim naechsten Worker-Tick **nicht**
    zu einer zweiten automatischen Sende-Anfrage fuer denselben Eintrag.
11. Der `SageBookingWorker` protokolliert jeden Lauf im Aktivitaets-Protokoll (`SyncLogServices`
    kennt den neuen Namen, deutsche Counts-Keys `gesendet`/`bestaetigt`/`fehler`).
12. `dotnet build` und `dotnet test` sind gruen; `SageLagerbuchungPayloadBuilderTests` und der
    Enqueue-Entscheidungs-Helper sind ohne DB/HTTP unit-getestet.
13. Migration + `SQL/82_*`/`SQL/83_*` sind idempotent gegen eine bereits migrierte DB einspielbar
    (zweimaliges Ausfuehren ohne Fehler); `SQL/00_FreshInstall.sql` erzeugt beide Schema-Aenderungen
    und beide `MigrationId`s.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (naechste freie Kapitelnummer, Themenbereich
„Integrationen/Sage") — Index in `secondbrain/tests/testszenarien-index.md` nachziehen. Skizze
(bei Freigabe vollstaendig auszuformulieren mit Vorbedingungen/Schritten/erwartetem
Verhalten/Negativfall je Testszenarien-Pflicht):

- **TS-X.1 — Globaler Toggle aus, Lagerplatz-Flag an: keine Buchung.** Vorbedingung:
  `SageLagerbuchungAktiv=false`, Ziel-Lagerplatz `SageBuchungErlaubt=true`. Schritt: manuelle
  Einbuchung durchfuehren. Erwartung: kein `SageBookingQueueItem` entsteht (DB-Check oder
  Monitoring-Liste leer).
- **TS-X.2 — Globaler Toggle an, Lagerplatz-Flag aus: keine Buchung.** Spiegelbildlich zu X.1.
- **TS-X.3 — Beide an: Queue-Eintrag entsteht, Status offen → gesendet → bestaetigt.**
  Vorbedingung: Sage-Testsystem erreichbar, gueltige Credentials in `appsettings.json`. Schritt:
  Einbuchung durchfuehren, Worker-Tick abwarten (Poll-Intervall). Erwartung: Queue-Eintrag
  durchlaeuft `offen → gesendet → bestaetigt` innerhalb der erwarteten Latenz; Buchung ist im
  Sage-Testsystem sichtbar (**echte Buchung am Testsystem**, Manual-UAT, da HTTP/SData nicht
  InMemory-testbar — `secondbrain/architektur/fallstricke.md` Abschnitt 8 „Raw-SQL-/Fremdsystem-Pfade
  sind nicht InMemory-testbar").
- **TS-X.4 — Ausbuchung spiegelbildlich.** Wie X.3, mit `"Entnahme"`, Herkunft/Ziel vertauscht.
- **TS-X.5 — Sage-Korrektur erzeugt keinen Ping-Pong.** Vorbedingung: `LagerbestandSyncService`
  aktiv und erzeugt eine `SageEinbuchung`/`SageAusbuchung`. Erwartung: **kein** Queue-Eintrag
  entsteht dafuer, auch wenn beide Toggles aktiv sind.
- **TS-X.6 — Fehlerhafte Konfiguration (Lagerkennung fehlt).** Vorbedingung: `SageBuchungErlaubt=true`
  an einem Lagerplatz ohne `SageLagerkennung`/`SageLagerplatzId`. Erwartung: Queue-Eintrag landet auf
  `Fehler` mit sprechender Meldung, Worker laeuft weiter (kein Absturz), Folgeeintraege werden
  weiterhin verarbeitet.
- **TS-X.7 — Timeout-Verhalten.** Vorbedingung: Sage-Testsystem simuliert einen Timeout (z. B.
  Netzwerk kurzzeitig kappen). Erwartung: Status bleibt bei `gesendet`/`fehler`, **kein** zweiter
  automatischer Sende-Versuch fuer denselben Eintrag ohne expliziten Retry (gemaess Rueckfrage 6).
- **TS-X.8 — Aktivitaets-Protokoll.** Schritte: `/SyncLog/Index` nach einem Lauf oeffnen. Erwartung:
  Lauf „SageLagerbuchung" sichtbar mit Counts `gesendet`/`bestaetigt`/`fehler`.

## Deploy

**QA-Finalisierung (2026-08-03):** Diff `3eca9ea..HEAD` bestaetigt web=true, service=true,
migration=true — Web (`IdealAkeWms/*`), Service (`IDEALAKEWMSService/*`) und zwei neue Migrationen
(`IdealAkeWms/Migrations/20260803104055_AddStorageLocationSageLagerbuchung`,
`20260803112322_AddSageBookingQueue`) sind alle drei betroffen, wie im Frontmatter bereits
vorgemerkt.

- **Web-App:** ja — neues Stammdatenfeld + Spalte (`StorageLocationsController`/`Views`), neue
  Queue-Repository-Registrierung + Decorator, neue Monitoring-View `/SageBookingQueue` inkl. Requeue.
- **Service:** ja — neuer `SageBookingWorker`, neuer `ISageLagerbuchungClient`, DI-Registrierung in
  `IDEALAKEWMSService/Program.cs`, neuer appsettings-Block `SageLagerbuchung:Username`/`Password`.
- **Migration:** ja — zwei additive Migrationen (`SQL/82_*`, `SQL/83_*`, inkl. UNIQUE-Index auf
  `SageBookingQueueItems.StockMovementId` gegen Doppel-Enqueue), kein DB-Backup zwingend (nicht
  daten-destruktiv), aber wie ueblich vor einem Produktiv-Deploy empfohlen.

**Publish-Befehle (aus dem Worktree, VOR dem Merge — Fluss: Publish aus dem Worktree → Testsystem →
Test → dann Merge):**
```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
```
Beide Komponenten sind betroffen — beide Befehle ausfuehren. Hinweis: **nach dem Merge** nur dann
erneut aus `main` publishen, wenn der Merge tatsaechlich getestete Dateien mit parallelen
main-Aenderungen zusammengefuehrt hat (Konflikt-Merge/Nicht-Fast-Forward); bei einem sauberen
Fast-Forward-Merge ist der bereits im Worktree gebaute/getestete Stand identisch mit main.

- **Reihenfolge:** DB-Migration zuerst (additiv, unkritisch fuer den laufenden Betrieb, `SQL/82_*`
  dann `SQL/83_*`) → Web-App neu deployen → Service stoppen, Binaries + `appsettings.json`-Ergaenzung
  (`SageLagerbuchung`-Block mit echten Credentials) einspielen, Service starten.
- **appsettings-Secret am Server:** `IDEALAKEWMSService/appsettings.json` (bzw. die
  Produktions-Overlay-Datei) muss am Zielserver **manuell** um den `SageLagerbuchung`-Block ergaenzt
  werden — analog zu `ConnectionStrings`/`MailSettings` heute, das geschieht **nicht** automatisch
  durch das Deploy-Skript.
- **ServiceSettings nach Deploy aktivieren:** `SageLagerbuchungAktiv` ist per Default `false` — nach
  dem Deploy muss ein Mensch unter `/ServiceSettings` den Toggle **und** die
  `SData:BaseUrl`/`SData:Dataset`-Werte setzen sowie je gewuenschtem Lagerplatz
  `SageBuchungErlaubt` aktivieren (ADR 0008: DB gewinnt, appsettings-Defaults greifen nicht mehr;
  `docs/TESTSZENARIEN.md` Kap. 51 „Nach jedem Deploy `/ServiceSettings` durchgehen").
- **Ein-Instanz-Voraussetzung (H5):** genau **ein** laufender `SageBookingWorker` — kein
  Doppel-Deploy/Failover auf derselben Queue, sonst Doppelbuchung (der Idempotenz-Baustein schuetzt
  nur bei einer einzigen Instanz).
- **Empfehlung:** vor der produktiven Aktivierung mit einem einzelnen, unkritischen Testartikel und
  einem klar identifizierbaren Testlagerplatz am Sage-Testsystem beginnen (siehe TS-56.3/56.4), erst
  danach breiter ausrollen. Detaillierte Deploy-Checkliste zusaetzlich in
  `secondbrain/aufgaben/2026-08-03-deploy-v1-28-0-sage-lagerbuchungen.md`.

## Offene Rueckfragen

1. **Buchungs-Auslöser-Scope.** Sollen ausschliesslich die manuellen Einbuchungen/Ausbuchungen
   (`StockMovementsController.Inbound`/`Outbound`/`OutboundAllConfirm`) einen Queue-Eintrag
   erzeugen, oder soll `Umbuchung` (Wert `3` in der SData-Enum) in Step 1 mitgenommen werden? Der
   Ist-Zustand zeigt: kommissionierungs-getriebene Umbuchungen (`PickingTransferService`) umgehen
   das Repository ohnehin strukturell und wuerden ein Decorator-basiertes Enqueue **nicht**
   erreichen — nur die beiden manuellen Umbuchungs-Actions (`Transfer`, `LocationTransferConfirm`)
   liefen technisch durch. Sollten diese trotzdem ausgeschlossen bleiben (Empfehlung dieser Spec:
   ja, „nur Material Zugang/Entnahme" laut Backlog), oder ist eine inkonsistente Teilabdeckung von
   Umbuchungen (nur manuell, nie kommissionierungs-getrieben) fachlich unerwuenscht und daher
   `SageEinbuchung`/`SageAusbuchung`-Schutz reicht als einzige Ausnahme? **Feedback-Loop-Schutz fuer
   `SageEinbuchung`/`SageAusbuchung` ist keine Frage, sondern harte Regel (Akzeptanzkriterium 6).**
2. **Fast-live-Kadenz.** Poll-Intervall des `SageBookingWorker` in Sekunden bestaetigen (Vorschlag
   dieser Spec: 15-30s, ServiceSetting `Sync:SageLagerbuchungIntervalSeconds`) — Abwaegung DB-Last
   (haeufigeres Polling) gegen Latenz („fast live").
3. **Idempotenz/Timeout-Handling.** Reicht der Statuswechsel `offen → gesendet` **vor** dem
   HTTP-Call plus manuelle Fehler-Sichtung (Vorschlag dieser Spec), oder verlangt Sage eine
   client-seitige Korrelations-/Idempotenz-Id, um eine Doppelbuchung bei einem Antwort-Timeout
   sicher auszuschliessen? Das Antwortformat des `LagerbuchungService` ist aus der Postman-Collection
   nicht belegt (nur der Request ist dokumentiert) — am Sage-Testsystem zu verifizieren, bevor die
   Idempotenz-Strategie final feststeht.
4. **Artikelnummer-Abgleich.** Bestaetigen, dass `Article.ArticleNumber` (WMS-Stammdaten) exakt der
   Sage-`Artikelnummer` im SData-Payload entspricht. Strukturelle Einschaetzung dieser Spec: ja,
   weil `StockMovement.ArticleId` direkt auf `Article` (nicht auf eine BOM-Zeile) verweist und der
   bekannte Artikelnummer-/Ressourcenummer-Fallstrick nur BOM-Zeilen betrifft — dennoch ist ein
   Abgleich per `GET Artikel` am Testsystem sinnvoll, bevor produktiv gebucht wird.
5. **Mandant/`dataset`-Wert je Instanz.** Konkreten Wert (bzw. wie er je Standort AKE/IDEAL
   ermittelt/gepflegt wird) klaeren — die Sample-Collection liefert ihn leer.
6. **Fehler-/Retry-Politik.** Maximale Sende-Versuche, Backoff-Strategie, ab wann eine Fehlermail
   ausgeloest wird (`ISyncErrorNotifier`), und ob fehlerhafte Buchungen ueber eine UI manuell
   requeue-bar sein sollen (dann ist die in „Technischer Loesungsentwurf" Punkt 7 skizzierte
   Monitoring-Liste Teil des Umfangs, sonst nicht).
7. **Sage-Spalte fuer den numerischen `LagerplatzId`.** `SageLagerplatzReader.GetAllActiveAsync`
   (`IDEALAKEWMSService/Services/SageLagerplatzReader.cs:23-29`) liest heute **keine** ID-Spalte,
   nur `Lagerkennung`/`Kurzbezeichnung`/`Platzbezeichnung`. Welche Sage-Spalte
   (`KHKLagerorte`/`KHKLagerplaetze`) liefert den numerischen `LagerplatzId`, den die SData-API fuer
   `HerkunftLagerplatzId`/`ZielLagerplatzId` erwartet? Ohne diese Spalte lassen sich die neuen
   Felder nicht befuellen — bitte am Sage-Testsystem-Schema (oder per `$schema`-Abruf) klaeren.
8. **`Zone` vs. neues Feld `SageLagerkennung` — Redundanz.** `LagerplatzSyncService.cs:109,125,151`
   persistiert die Sage-Lagerkennung **bereits heute** in `StorageLocation.Zone` (frei editierbares
   UI-Feld „Bereich/Zone", auch fuer manuelle Lagerplaetze genutzt). Diese Spec schlaegt laut
   Vorgabe ein **zusaetzliches, dediziertes** Feld `SageLagerkennung` vor (sauberer, entkoppelter
   Payload-Datenpfad, keine Abhaengigkeit von einem frei editierbaren Anzeigefeld). Bestaetigen:
   soll es wirklich ein zweites Feld geben (Redundanz fuer Sage-Lagerplaetze, `Zone` bleibt
   unangetastet), oder soll stattdessen direkt `Zone` fuer den SData-Payload verwendet werden
   (Risiko: `Zone` ist frei editierbar, keine Formatgarantie, und ein Admin koennte es fuer einen
   Sage-Lagerplatz versehentlich ueberschreiben und damit die Sage-Buchung stillschweigend
   verfaelschen)?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →Umbuchung auch gleich mitnehmen. 
   es gibt am Lagerplatz normalerweise bereits ein FLAG istBuchbar oder so. 
   Ein zusätzliches FLAG SageSync pro Lagerort bzw. ein Flag nur WMS Buchungslager. 
2. →ok
3. →ist mir nicht bekannt
4. →sollte so sein, ja
5. →Mandant ist derzeit pro Standort gleich - getrennte eigene Systeme. 
   eventuell neue Settingsmaske - Standorteinstellungen, Firmenname, Adresse, Mandant, Fertigungsauftragslogik, für zukünftige anpassungen 
6. →ja, requeue sinnvoll.
7. →hier hast du einen Screenshot mit der übersicht möglicher spalten. ![[Pasted image 20260803094629.png]]
8. →Zone lassen und SageLagerkennung 

## Kritische Pruefung (2026-08-03)

Anwalt-des-Teufels-Durchsicht vor Schranke 1. Geprueft gegen Spec, Backlog
`2026-07-29-Postman-Lagerbuchungen`, Screenshot zu Frage 7 und den echten Code
(`StockMovement.cs`, `StockMovementsController.cs`, `SageLagerplatzReader.cs`,
`LagerplatzSyncService.cs`). Es gibt substanzielle Befunde — der Entwurf ist gut recherchiert,
aber **drei der acht Antworten reissen den Entwurf auf**, weil sie ihm widersprechen oder eine
offene, geschaeftskritische Frage offen lassen.

### BLOCKER — vor der Freigabe zu klaeren

**B1 — Antwort 1 („Umbuchung auch gleich mitnehmen") widerspricht dem gesamten Loesungsentwurf.**
Der komplette Entwurf ist auf `Einbuchung`/`Ausbuchung` gebaut: Out-of-Scope nennt Umbuchung
explizit „out", der Payload kennt nur `Zugang`/`Entnahme`, die Akzeptanzkriterien 4/5/7/8 nur
Ein-/Ausbuchung, und der Decorator-Filter (`saved.MovementType is Einbuchung or Ausbuchung`,
Abschnitt 3) schliesst Umbuchung aktiv aus. Die Antwort dreht das um, ohne dass Entwurfstext,
Payload oder Kriterien angepasst waeren. Konkrete, im Code verifizierte Konsequenzen:
- **Inkonsistente Teilabdeckung.** Die beiden *manuellen* Umbuchungen (`Transfer`
  `StockMovementsController.cs:326`, `LocationTransferConfirm` `:480`) laufen durchs Repository und
  wuerden vom Decorator erfasst. Die *kommissionierungs-getriebenen* Umbuchungen
  (`PickingTransferService`, direkter `_context.Add`) laufen **nicht** durchs Repository und wuerden
  **nie** an Sage gemeldet. Ergebnis: Sage sieht manuelle Umlagerungen, aber nie die aus der
  Kommissionierung — genau die Inkonsistenz, vor der die Spec in Rueckfrage 1 selbst warnt.
- **Kein Payload fuer Umbuchung.** Ein `Umbuchung`-`StockMovement` traegt **beide** Enden
  (`StorageLocationId` = Ziel *und* `SourceStorageLocationId` = Herkunft, `StockMovement.cs:18,38`).
  Der Entwurf definiert aber nur „Ziel gesetzt / Herkunft leer" (Zugang) bzw. umgekehrt (Entnahme).
  Welchen `Lagerbewegungsart`-String (SData-Enum-Wert 3) eine Umbuchung sendet und dass **beide**
  Lagerplaetze gefuellt werden, steht nirgends.
- **Gating unklar.** „`SageBuchungErlaubt` am beteiligten Lagerplatz" ist bei zwei Lagerplaetzen
  mehrdeutig: Herkunft, Ziel oder beide? Und ein Sonderfall: der Negativ-Lagerplatz-Auto-Transfer
  (`StockMovementsController.cs:300-305`) erzeugt eine WMS-interne Umbuchung, die bei Sage vermutlich
  Rauschen waere.
- **Fachlich fraglich.** Eine Umlagerung zwischen zwei Plaetzen **derselben** Sage-Lagerkennung
  (z. B. `GL:1:1:12` → `GL:1:1:13`, beide Lagerkennung „GL") aendert den Sage-Bestand auf
  Lager-Ebene nicht — sie an Sage zu melden, kann falsch/doppelt sein.

  **Frage an den Menschen:** Soll Umbuchung wirklich in Step 1? Wenn ja, bitte entscheiden:
  (a) akzeptiert, dass nur *manuelle* Umbuchungen bei Sage ankommen, kommissionierungs-getriebene
  nie? (b) Umbuchungs-Payload = beide Enden gesetzt, welcher `Lagerbewegungsart`-Wert? (c) welches
  Ende gated das Enqueue? (d) werden Umlagerungen innerhalb derselben Lagerkennung unterdrueckt?
  **Empfehlung:** Umbuchung wie im Original-Entwurf **aus Step 1 herauslassen** und als klar
  definierten Step 2 nachziehen — sonst geht ein halbgares, inkonsistentes Umbuchungs-Verhalten
  produktiv.
	=>ANTWORT: Umubuchung herauslassen.
	
**B2 — Idempotenz/Doppelbuchung ist ungeloest (Antwort 3: „ist mir nicht bekannt") und Antwort 6
(„requeue sinnvoll") verschaerft genau diesen Fall.** Das Backlog nennt das selbst
geschaeftskritisch („muss die Spec loesen"). Der Entwurfs-Baustein „Status **vor** dem HTTP-Call auf
`Gesendet`" verhindert nur, dass der *automatische* Worker im naechsten Tick erneut sendet. Er
verhindert **nicht** die Doppelbuchung im Timeout-Fall: bucht Sage erfolgreich, kommt aber die
Antwort nicht zurueck, geht der Eintrag auf `Fehler` — und ein (jetzt gewuenschtes) manuelles
Requeue sendet dieselbe Buchung ein **zweites** Mal. Zusaetzlich fehlt eine Erholung fuer den
Crash-Fall: stirbt der Service zwischen „`Gesendet` gesetzt" und Antwort, bleibt der Eintrag fuer
immer auf `Gesendet` haengen (wird nie erneut geladen, nie bestaetigt). Ob Sage ueber `Memo` oder
eine Korrelations-Id nachtraeglich abfragbar macht, „ob schon gebucht", ist laut Entwurf selbst
unbelegt (Antwortformat des `LagerbuchungService` nicht dokumentiert).

  **Frage an den Menschen / Auftrag an den Dev-Lauf:** Die Idempotenz-Garantie laesst sich nicht
  entwerfen, solange das Sage-Antwort-/Dedup-Verhalten unbekannt ist. Bitte festlegen, dass der
  Dev-Lauf **zuerst am Sage-Testsystem verifiziert**, ob eine Buchung ueber `Memo`/Korrelation
  auffindbar ist, **bevor** der Sende-/Requeue-Pfad final gebaut wird; und die Requeue-Prozedur als
  „im Sage pruefen, dass keine Buchung existiert, erst dann erneut senden" definieren (nie blindes
  Auto-/Manuell-Resend nach Timeout). Ohne diese Klaerung ist der geschaeftskritischste Teil der
  Integration nicht spezifiziert.
=>ANTWORT: es gibt memo möglichkeit im Job 
![[Pasted image 20260803102037.png]]

**B3 — Der Payload hartkodiert das Ebenen-Suffix `;0;0;0`, aber die realen Lager haben Ebenen.**
Der Screenshot zu Frage 7 zeigt: `PlatzID` (aus `KHKLagerplaetze`, per Lagerplatz eindeutig)
loest Frage 7, aber er zeigt auch, dass reale Plaetze `Kurzbezeichnung` „GL:1:1:12" mit den Ebenen
`1:1:12` und Lagerkennung „GL" haben. Der Entwurf sendet
`HerkunftLagerkennung`/`ZielLagerkennung = "<SageLagerkennung>;0;0;0"`, also fuer GL „GL;0;0;0" —
waehrend der Platz real die Ebenen `1:1:12` und eine spezifische `PlatzID` hat. Ob Sage die
`LagerplatzId` als fuehrend nimmt (und das Ebenen-Suffix ignoriert) oder das echte „GL;1;1;12"
erwartet, ist unbekannt. Falsch gefuellt bucht die Integration auf den **falschen Platz bzw. das
ganze Lager** — geschaeftskritisch.

  **Frage an den Menschen:** Ist am Sage-Testsystem die `LagerplatzId` allein fuehrend (dann ist
  `;0;0;0` egal), oder muss die Lagerkennung die echten Ebenen tragen? Cheap jetzt zu klaeren, teuer
  erst im UAT (TS-X.3) zu entdecken.
ANTWORT hier ein beispiel zum LL (Lagerlift)
![[Pasted image 20260803102655.png]]

### SOLLTE — macht den Dev-Lauf sicherer

**S1 — Monitoring-UI ist durch Antwort 6 nicht mehr „optional", sondern In-Scope.** „ja, requeue
sinnvoll" macht die in Punkt 7 skizzierte Liste `/SageBookingQueue` (Listen-View-Pattern + Requeue-
Aktion) verbindlich. Bitte In-Scope-Liste, `affected_code`-Frontmatter (Controller, Index-View,
ViewModel, Pagination) und ein Akzeptanzkriterium fuer den Requeue-Pfad ergaenzen — inkl. der
Doppelbuchungs-Absicherung aus B2.

**S2 — Groesse: mit Umbuchung (B1) + Requeue-UI (S1) ist das kein „mittleres Feature" mehr.** Grob
~30 betroffene Dateien ueber Web + Service + 2 Migrationen + FreshInstall (je 2 Stellen) + Tests +
Doku. Das ist ein sehr voller einzelner Dev-Lauf. **Vorschlag:** Schnitt in Step 1a
(Zugang/Entnahme + read-only Monitoring, ohne Requeue) und Step 1b (Umbuchung + Requeue), oder
zumindest eine bewusste Reihenfolge innerhalb des Laufs mit eigener QA je Teil.

**S3 — Recovery fuer haengende `Gesendet`-Eintraege fehlt** (Service-Restart mitten im Senden, siehe
B2). Bitte eine Regel + Akzeptanzkriterium + Testszenario ergaenzen: `Gesendet`-Eintraege aelter als
X werden **nicht** automatisch neu gesendet, sondern zur manuellen Sage-Pruefung in der
Monitoring-Liste sichtbar gemacht.

**S4 — Flag-Semantik/Benennung aus Antwort 1 bestaetigen.** Die Antwort schwankt zwischen „ein
zusaetzliches FLAG SageSync" (positiver Opt-in, = Entwurf `SageBuchungErlaubt`, Default false) und
„ein Flag nur WMS Buchungslager" (inverse Semantik: Platz, der **nicht** an Sage geht). Das sind
gegensaetzliche Defaults. Bitte bestaetigen, dass es beim positiven Opt-in `SageBuchungErlaubt`
(Default false, kumulativ zum globalen Toggle) bleibt.

**S5 — Antwort 5 birgt Scope-Creep.** Die Idee „neue Settingsmaske Standorteinstellungen
(Firmenname, Adresse, Mandant, FA-Logik)" ist ein eigenes, groesseres Vorhaben. Bitte im Spec-Text
explizit als **out-of-scope** markieren; fuer diese Spec bleibt `SData:Dataset` ein einfacher
ServiceSetting. Die Standorteinstellungs-Idee als eigene Backlog-Notiz festhalten, damit der
Dev-Lauf sie nicht mitbaut.

### HINWEIS — Beobachtung ohne Handlungszwang

**H1 — Gute Nachricht zu Frage 7:** Der Match-Schluessel existiert bereits sauber. Der Sync matcht
Sage↔WMS ueber `Kurzbezeichnung` → `StorageLocation.Code` (`LagerplatzSyncService.cs:108,120,124`),
und `Kurzbezeichnung` ist im Screenshot pro Platz eindeutig („GL:1:1:12"). `lp.PlatzID` gehoert zur
selben Zeile — die neuen Felder lassen sich also sauber pro Platz befuellen, sobald
`SageLagerplatzReader` `lp.PlatzID` zusaetzlich selektiert. Achtung: die `PlatzID`-Eindeutigkeit
haengt am Mandanten (`lo.Mandant = 1` im Reader) — bei getrennten Systemen je Standort unkritisch.

**H2 — AK9 wird real getroffen:** `LagerplatzSyncService` befuellt die neuen Felder nur fuer
`Source == Sage`-Plaetze; manuelle Plaetze bleiben `null`. Der defensive `Fehler`-Pfad (AK9) ist
also kein Papiertiger, sondern der Normalfall fuer faelschlich gesetztes `SageBuchungErlaubt` auf
manuellen Plaetzen — gut, dass er getestet wird.

**H3 — Frontmatter-Housekeeping:** `open_questions` listet weiterhin alle 8 Fragen als offen und
`freigabe`-Block ist leer; das ist bis zur Freigabe ok, sollte aber beim Uebergang nach
`freigegeben/` mit den Antworten/Entscheidungen abgeglichen werden (Traceability).

### Empfehlung

**NACHBESSERUNG NOETIG: Umbuchungs-Scope (B1) und Idempotenz/Doppelbuchung (B2) sind ungeklaert und
teils widerspruechlich — beide sind geschaeftskritisch und muessen vor dem Dev-Lauf entschieden
werden.**

---

## Kritische Pruefung — 2. Durchgang (2026-08-03)

Zweiter Anwalt-des-Teufels-Durchgang. Die **Freigabe-Antworten sind unveraendert** seit dem ersten
Durchgang — **B1, B2 und B3 oben stehen unveraendert** und sind weiterhin die entscheidenden
Blocker. Dieser Durchgang ging tiefer in den **Implementierungs-Entwurf** (Decorator, Transaktions-
Grenzen, Repository-Basisklasse) und foerderte Risiken zutage, die der erste Durchgang nicht
abgedeckt hat. Verifiziert an `Repository.cs`, `StockMovementRepository.cs`,
`IStockMovementRepository.cs`, `CachedSettingRepository.cs`, `SQL/`-Verzeichnis.

### BLOCKER-nah (starkes SOLLTE) — vor dem Dev-Lauf loesen

**B4 — Der Enqueue ist nicht transaktional mit der Buchung, und ein Enqueue-Fehler reisst die
bereits gespeicherte WMS-Buchung mit.** Verifiziert: `Repository<T>.AddAsync` ruft
`SaveChangesAsync()` **sofort** (`Repository.cs:35-37`). Der `StockMovement` ist also bereits
**committed**, wenn der Decorator danach `MaybeEnqueueAsync(saved)` ausfuehrt — das ist eine zweite,
getrennte Transaktion. Zwei konkrete Loecher:
- **Stiller Verlust:** Crasht der Prozess (oder wirft der Enqueue) **zwischen** den beiden
  SaveChanges, ist der `StockMovement` persistiert, aber es entsteht **kein** Queue-Eintrag. Die
  Buchung erreicht Sage **nie**, und **nichts** meldet den Verlust. Fuer das erklaerte Ziel „Sage
  kennt den echten Bestand" ist das ein stiller Datenverlust im geschaeftskritischen Pfad.
- **Fehlerhafte Buchungs-Rueckmeldung:** Wirft `MaybeEnqueueAsync` (z. B. transienter DB-Fehler beim
  Nachladen der `StorageLocation`), propagiert die Exception durch den Decorator zum Controller —
  obwohl die WMS-Buchung **schon gespeichert** ist. Der Anwender sieht „Buchung fehlgeschlagen" und
  bucht evtl. erneut → **doppelte WMS-Buchung**. Der Sketch (`await
  _enqueueDecision.MaybeEnqueueAsync(saved);`, Abschnitt 3) hat kein try/catch.

  **Vorschlag (keine Menschen-Entscheidung noetig, aber im Entwurf zu fixieren):** entweder (a) den
  Enqueue in **dieselbe** SaveChanges/Transaktion wie den `StockMovement` ziehen (echte Atomaritaet
  — erfordert, den direkten `SaveChanges` in `AddAsync` fuer diesen Pfad zu umgehen), oder (b) den
  Enqueue-Fehler im Decorator **fangen + protokollieren, nie werfen** und zusaetzlich einen
  **Reconciliation-Sweep** im Worker vorsehen (Ein-/Ausbuchungen auf Sage-Plaetzen ohne
  Queue-Eintrag nachtraeglich einreihen). Akzeptanzkriterium ergaenzen: „Ein Enqueue-Fehler laesst
  die WMS-Buchung weder scheitern noch verloren gehen."

### SOLLTE — macht den Dev-Lauf sicherer

**S6 — Der Decorator-Sketch mischt zwei Bauweisen und ist als Vorlage so nicht baubar.** Verifiziert:
`StockMovementRepository : Repository<StockMovement>, IStockMovementRepository`, `AddAsync` ist
`virtual` (`Repository.cs:33`); `IStockMovementRepository` erbt `IRepository<T>` **plus** sieben
eigene Methoden (`IStockMovementRepository.cs:6-44`). Der Sketch schreibt `public override async Task
AddAsync` (= **Subclassing**), die Prosa sagt aber „umschliesst die konkrete `StockMovementRepository`
(Vorbild `CachedSettingRepository`)" — und `CachedSettingRepository` ist ein **Kompositions**-Decorator
**ohne** `override` (`CachedSettingRepository.cs:6-17`). Das sind zwei unvereinbare Ansaetze:
- **Komposition** (wie das zitierte Vorbild): `IStockMovementRepository` implementieren, `_inner`
  umschliessen, **alle ~15 Methoden** an `_inner` delegieren, **kein** `override`.
- **Subclassing**: `: StockMovementRepository`, nur `override AddAsync`, alles andere geerbt — passt
  zum `override` im Sketch und ist **deutlich** weniger Boilerplate (eine Methode statt fuenfzehn).

  **Vorschlag:** eine Bauweise festlegen — Subclassing ist hier klar guenstiger. Dann die
  `CachedSettingRepository`-Referenz als „Vorbild nur fuer die **DI-Registrierung**, nicht fuer die
  **Klassenstruktur**" kennzeichnen, sonst delegiert der Dev unnoetig 15 Methoden von Hand (mit dem
  Risiko, beim naechsten Interface-Zuwachs eine zu vergessen).

### HINWEIS — Beobachtung ohne Handlungszwang

**H4 — Migrationsnummern 82/83 sind auf `main` aktuell frei** (hoechste ist
`81_AddProductionOrderExtraInfo.sql`) — die Spec-Annahme stimmt heute. Aber laut Brain sind Branches
in Arbeit (OverridePrePickingDays v1.27, IDEAL-Anpassungen-Rebuild). Unmittelbar vor dem Dev-Lauf
pruefen, dass keine parallele Arbeit 82/83 belegt hat, sonst kollidieren die
`__EFMigrationsHistory`-Inserts und `SQL/00_FreshInstall.sql`.

**H5 — Ein-Instanz-Annahme unausgesprochen.** Der Idempotenz-Baustein („Status auf `Gesendet` vor dem
Call") schuetzt nur bei **genau einer** laufenden Service-Instanz. Bei zwei Hosts (Failover,
versehentlicher Doppel-Deploy) auf derselben Queue senden beide → Doppelbuchung. Kurz als
Voraussetzung festhalten: „genau eine `SageBookingWorker`-Instanz".

### Empfehlung (2. Durchgang)

**NACHBESSERUNG NOETIG: unveraendert wegen B1/B2/B3; zusaetzlich B4 (nicht-transaktionaler Enqueue
mit stillem Verlust bzw. Doppelbuchungs-Pfad) im Entwurf schliessen, bevor gebaut wird.**

---

## Kritische Pruefung — 3. Durchgang (2026-08-03)

Dritter Durchgang. **Die Freigabe-Antworten sind weiterhin unveraendert** — alle bisherigen Blocker
(**B1 Umbuchung-Widerspruch, B2 Idempotenz „ist mir nicht bekannt", B3 `;0;0;0`-Payload, B4
nicht-transaktionaler Enqueue**) stehen **unveraendert** und sind die offenen Punkte. Dieser
Durchgang hat den **Regressionspfad** des Decorator-Umbaus geprueft (Blast-Radius, Bestandstests).
Ergebnis ehrlich: **ein neuer Befund, eine Entwarnung** — mehr gibt der aktuelle Stand ohne
geaenderte Antworten nicht her.

### SOLLTE

**S7 — Es fehlt ein Regressions-Akzeptanzkriterium „Bestandsverhalten unveraendert".** Verifiziert:
**zwoelf** Consumer injizieren `IStockMovementRepository` (`StockMovementsController`,
`PickingController`, `WarehousePickingController`, `StockOverviewController`, `StockApiController`,
`PickingApiController`, `WarehouseRequisitionsApiController`, `ArticlesController`,
`MissingPartsLagerController`, `ReadOnlyBomBuilder`, `PickingTransferService`, +Registrierung in
`Program.cs`). Der DI-Umbau leitet **alle** durch den Decorator. Die 13 Akzeptanzkriterien pruefen,
dass bei Toggle aus **kein Queue-Eintrag** entsteht (AK2/AK3), aber **keines** sichert zu, dass die
**Buchung selbst** und alle uebrigen Repository-Methoden fuer diese zwoelf Aufrufer **exakt
unveraendert** bleiben. Das ist genau die Zusicherung, die der Decorator-Umbau braucht.
**Vorschlag:** ein hartes Kriterium ergaenzen — „Bei `SageLagerbuchungAktiv=false` ist das Verhalten
aller `IStockMovementRepository`-Aufrufer **bit-identisch** zum Ist-Zustand (Buchung, Bestand,
Historie, Kommissionierung); der Decorator ist fuer alle Methoden ausser `AddAsync` ein reiner
Pass-through." Das schaerft zugleich S6 (bei Komposition darf keine der ~15 Methoden vergessen
werden — sonst bricht sie fuer bis zu zwoelf Aufrufer).

### HINWEIS

**H6 — Entwarnung Test-Regression:** Die bestehenden Repository-Tests konstruieren `new
StockMovementRepository(ctx)` **direkt** (`StockMovementRepositoryTests.cs:71,86,102,126,138`, u. a.),
nicht ueber DI. Der DI-Umbau auf den Decorator laesst diese Tests also unberuehrt — das
Regressions-Risiko liegt **im Laufzeit-Verhalten** (S7), nicht in den Unit-Tests. Die neuen Tests
(`SageLagerbuchungPayloadBuilderTests`, Enqueue-Helper) sind davon unabhaengig.

### Empfehlung (3. Durchgang)

**NACHBESSERUNG NOETIG — unveraendert.** Die Entscheidung liegt jetzt beim Menschen: **B1/B2/B3**
sind Menschen-Entscheidungen (Umbuchung-Scope, Idempotenz-Strategie am Sage-Testsystem,
Payload-Ebenenformat), **B4** ist eine Entwurfs-Korrektur. Solange die vier offen sind, aendern
weitere Re-Reviews ohne geaenderte Antworten nichts — die drei Durchgaenge decken den pruefbaren
Stand ab. Nach dem Beantworten von B1/B2/B3 lohnt ein gezielter vierter Blick **nur** auf die dann
angepassten Stellen.

---

## Schranke-1-Antworten — konsolidiert und geprueft (2026-08-03)

Der Mensch hat die Blocker im Kritik-Abschnitt beantwortet (inline `=>ANTWORT`-Zeilen + zwei
Screenshots). Hier sauber zusammengefuehrt, gegen die Screenshots und den Code geprueft, mit der
jeweiligen **Konsequenz fuer die Umsetzung**. Die rohen Antworten oben bleiben als Provenienz stehen.

### B1 — Umbuchung: **RAUS.** ✅ geloest
Antwort: „Umbuchung herauslassen." Damit steht der Original-Scope wieder: nur `Einbuchung`/
`Ausbuchung`, Decorator-Filter `is Einbuchung or Ausbuchung`. Out-of-Scope-Abschnitt entsprechend
verschaerft (kein „vorbehaltlich" mehr). Kein Umbuchungs-Payload, keine Gating-Mehrdeutigkeit mehr.
**Nichts weiter zu tun.**

### B2 — Idempotenz/Doppelbuchung: Memo ist im Sage-Job vorhanden → Lookup statt Blind-Resend. ✅ Mechanismus geklaert, Detail fuer den Dev-Lauf
Antwort + Screenshot (`Pasted image 20260803102037.png`): Es gibt die Sage-Tabellen
`KHKLagerplatzbuchungen` (+ `KHKLagerplatzbuchungenJobs`) mit den Spalten **`Memo`**, **`Referenz`**
(z. B. „2007-200001"), `Status`, `Bewegungsart`, `Artikelnummer`, `Bewegungsdatum`. **Konsequenz:**
Die Idempotenz laesst sich sauber loesen — der `SageBookingWorker` bettet eine eindeutige Korrelation
(z. B. `StockMovement.Id`) in `Memo` **und** setzt sie fuer den Timeout-/Requeue-Fall als
Lookup-Schluessel ein:
- Vor einem Requeue (oder beim Aufraeumen haengender `Gesendet`-Eintraege, S3): **erst per Read gegen
  `KHKLagerplatzbuchungen` pruefen**, ob zu dieser Korrelation bereits eine Buchung existiert. Wenn ja
  → Eintrag auf `Bestaetigt` setzen (nicht erneut senden). Wenn nein → senden erlaubt.
- Der Read geht ueber die bereits vorhandene `SageConnection` (Raw-SQL, wie `SageLagerplatzReader`/
  `SageImportService`) — **kein** neuer Zugriffsweg noetig.
- **Dev-Lauf-Auftrag (am Testsystem zu fixieren):** exakte Spalte fuer die Korrelation waehlen
  (`Memo` frei setzbar? oder `Referenz`?) und das reale Antwortformat des `LagerbuchungService`
  bestaetigen. Damit ist B2 **kein Blocker mehr**, sondern eine umsetzbare, verifizierbare Strategie.
- **Achtung Bewegungsart:** im persistierten Datensatz steht `Bewegungsart` als Code (`EA`), nicht als
  „Zugang"/„Entnahme" — das betrifft nur den Lese-Abgleich, nicht den SData-POST (der die Klartext-
  Bewegungsart aus dem Postman-Sample sendet).

### B3 — Payload-Kennung: `;0;0;0` war **falsch**; volle Kurzbezeichnung nutzen. ✅ geloest + Spec korrigiert
Antwort + Screenshot (`Pasted image 20260803102655.png`, Lager „LL | Lagerlift"): die
Kurzbezeichnung eines realen Platzes ist **`LL;1;4;0`** (Format `Lagerkennung;Reihe;Platz;Ebene`,
Semikolon-getrennt) — und `LL;0;0;0` ist die **Lager-Wurzel**, ein **anderer** Platz. Verifiziert im
Code: diese volle Kurzbezeichnung liegt bereits in `StorageLocation.Code`
(`LagerplatzSyncService.cs:85` Dictionary-Key `Code`, `:91` `code = dto.Kurzbezeichnung`).
**Konsequenz (in Abschnitt 1 + 5 bereits eingearbeitet):**
- `HerkunftLagerkennung`/`ZielLagerkennung` = **`StorageLocation.Code`** (volle Kurzbezeichnung,
  z. B. `LL;1;4;0`) — **kein** hartes `;0;0;0`.
- `HerkunftLagerplatzId`/`ZielLagerplatzId` = `SageLagerplatzId` (= `KHKLagerplaetze.PlatzID`,
  Frage 7). Beide Felder zeigen konsistent auf **denselben** Platz — robust, egal ob Sage die Kennung
  oder die PlatzId als fuehrend behandelt.
- **Feld-Klarstellung:** `SageLagerkennung` soll die **volle** Kurzbezeichnung tragen (= `Code`), nicht
  die bare Lagerkennung („LL"). Damit ist der Payload-Pfad entkoppelt vom frei editierbaren `Zone`
  (Antwort 8: „Zone lassen und SageLagerkennung"). Alternativ kann der Payload direkt `Code` lesen und
  `SageLagerkennung` entfaellt — **eine** von beiden Varianten der Dev-Lauf waehlen (Empfehlung: direkt
  `Code` verwenden, `SageLagerkennung` nur wenn ein von `Code` entkoppelter Wert fachlich gewollt ist).

### B4 — Nicht-transaktionaler Enqueue: als Entwurfs-Korrektur akzeptiert (keine Menschen-Frage)
Bleibt umzusetzen (kein menschlicher Input noetig): Enqueue-Fehler im Decorator **fangen +
protokollieren, nie werfen**; zusaetzlich Reconciliation-Sweep im Worker (Ein-/Ausbuchungen auf
Sage-Plaetzen ohne Queue-Eintrag nachtraeglich einreihen). Akzeptanzkriterium „Enqueue-Fehler laesst
die WMS-Buchung weder scheitern noch verloren gehen" ergaenzen. Passt gut zum B2-Lookup: der Sweep
kann dieselbe Korrelations-Pruefung nutzen.

### Restpunkte — Status
- **S1 (Monitoring-/Requeue-UI):** durch Antwort 6 („requeue sinnvoll") **In-Scope**. In-Scope-Liste,
  `affected_code` und ein Requeue-Akzeptanzkriterium (mit B2-Lookup) sind beim Dev-Lauf zu ergaenzen.
- **S2 (Groesse):** mit Umbuchung **raus** (B1) wieder knapper — bleibt aber mit Requeue-UI ein
  grosser Einzel-Lauf. Empfehlung: read-only Monitoring + Requeue zusammen halten, aber QA in zwei
  Etappen (Sende-Pfad zuerst gruen, dann Requeue).
- **S3 (haengende `Gesendet`):** durch B2 jetzt loesbar — Recovery = B2-Lookup, kein Blind-Resend.
- **S4 (Flag-Benennung):** Antwort 1 blieb hier vage („FLAG SageSync" bzw. „nur WMS Buchungslager").
  **Offen gebliebene Praezisierung** — Vorschlag: beim positiven Opt-in `SageBuchungErlaubt`
  (Default false) bleiben; bitte kurz bestaetigen, sonst faellt die Default-Richtung dem Dev-Lauf zu.
  =>ANTWORT (2026-08-03): **`SageBuchungErlaubt`** — positives Opt-in, **Default `false`**, kumulativ
  zum globalen Toggle `SageLagerbuchungAktiv` (UND-Verknuepfung, nicht ODER). Es wird **kein**
  invertiertes Flag („nur WMS Buchungslager") gebaut: reine WMS-Lagerplaetze (`Source == Manual`)
  haben ohnehin nie `SageLagerkennung`/`SageLagerplatzId` und laufen damit strukturell in den
  definierten Fehlerpfad (Akzeptanzkriterium 9) statt an Sage zu senden. Damit bleiben Benennung,
  Default und Semantik exakt wie im Loesungsentwurf (Abschnitt 1) und in den Akzeptanzkriterien
  2-4 beschrieben — **keine Anpassung des Entwurfs noetig.**
- **S5 (Standorteinstellungs-Maske, Antwort 5):** als **out-of-scope dieser Spec** gefuehrt;
  `SData:Dataset` bleibt ein einfacher ServiceSetting. Idee als eigene Backlog-Notiz festhalten.
- **S6 (Decorator-Bauweise):** Subclassing + `override AddAsync` festlegen (eine Methode statt ~15).
- **S7 (Regressions-AK):** „bei Toggle aus bit-identisches Verhalten" als hartes Kriterium ergaenzen.
- **H3 (Frontmatter):** `open_questions` noch als offen gelistet; beim Uebergang nach `freigegeben/`
  mit diesen Entscheidungen abgleichen.

### Empfehlung (nach Schranke-1-Antworten)

**Alle geschaeftskritischen Blocker sind geloest:** B1 (Umbuchung raus), B2 (Idempotenz per
Memo-Lookup gegen `KHKLagerplatzbuchungen`), B3 (volle Kurzbezeichnung statt `;0;0;0`, im Spec-Text
korrigiert), S4 (Flag bleibt `SageBuchungErlaubt`, positives Opt-in, Default `false`).

**BEREIT ZUR FREIGABE.** B4/S1/S3/S6/S7 wandern als **Umsetzungs-Auftraege in den Dev-Lauf** (kein
weiterer menschlicher Input noetig):
- **B4** Enqueue-Fehler fangen + protokollieren, nie werfen; Reconciliation-Sweep im Worker.
- **S1** Monitoring-Liste `/SageBookingQueue` inkl. Requeue ist In-Scope (Antwort 6).
- **S3** haengende `Gesendet`-Eintraege ueber den B2-Lookup aufloesen, nie blind neu senden.
- **S6** Decorator als **Subclassing** (`: StockMovementRepository`, nur `override AddAsync`).
- **S7** hartes Regressions-Kriterium „bei Toggle aus bit-identisches Verhalten" ergaenzen.

Den Freigabe-Block im Frontmatter fuellt weiterhin der Mensch — ich habe ihn bewusst nicht gesetzt.

---

## QA-Abnahme (2026-08-03) — Status: Testbereit

Durchgefuehrt im Worktree `.claude/worktrees/2026-07-29-sage-lagerbuchungen`,
Branch `feature/2026-07-29-sage-lagerbuchungen`, Diff `3eca9ea..6386f05`.

### Beweis: Build

```
> dotnet build IdealAkeWms.slnx -c Debug
Der Buildvorgang wurde erfolgreich ausgeführt.
    9 Warnung(en)   (bestehende NU1902-Advisories MailKit/MimeKit + 1 CS8602 in TrackingController,
                     beide nicht Teil dieser Aenderung)
    0 Fehler(en)
```

### Beweis: Tests

```
> dotnet test IdealAkeWms.slnx -c Debug
IdealAkeWms.Tests.dll        : Fehler: 0, erfolgreich: 1043, übersprungen: 1, gesamt: 1044
IDEALAKEWMSService.Tests.dll : Fehler: 0, erfolgreich:  186, übersprungen: 0, gesamt:  186
```
Der eine uebersprungene Test (`ProductionOrderEagerCreateAgentJobTests...`) ist ein bestehender,
umgebungsabhaengiger Integrationstest, unabhaengig von dieser Spec.

### Akzeptanzkriterien-Abdeckung (automatisiert vs. Manual-UAT)

| AK | Abdeckung |
|---|---|
| AK1 (Edit/Liste/Filter) | Manual-UAT (View) |
| AK2/AK3 (Toggle-Gating) | `SageBookingEnqueueDecisionTests`, `SageBookingEnqueueingStockMovementRepositoryTests` (InMemory) |
| AK4/AK5 (Enqueue bei Inbound/Outbound/OutboundAllConfirm) | Decorator-Test deckt `AddAsync`-Pfad ab; Controller-Actions selbst Manual-UAT |
| AK6 (Feedback-Loop-Schutz SageEinbuchung/-Ausbuchung) | `SageBookingEnqueueDecisionTests.ShouldEnqueue_NonBookableType_AlwaysFalse...`, Decorator-Test |
| AK7/AK8 (Payload spiegelbildlich Zugang/Entnahme) | `SageLagerbuchungPayloadBuilderTests` (isoliert, ohne HTTP/DB) |
| AK9 (fehlende Sage-Referenz → Fehler statt Absturz) | `SageLagerbuchungPayloadBuilderTests.Build_MissingSageLagerkennung/PlatzId_Throws...`; Worker faengt `SageBookingPayloadException` ab (Code-Review, TS-56.6 Manual-UAT) |
| AK10 (Status vor HTTP-Call auf Gesendet) | Code-verifiziert (`SageBookingWorker.cs:186` `MarkSentAsync` vor `:189` `client.SendAsync`); Timeout-Fall selbst nicht InMemory-testbar → TS-56.7 Manual-UAT |
| AK11 (Aktivitaets-Protokoll) | TS-56.10 Manual-UAT (kein SyncLog-InMemory-Vorbild) |
| AK12 (Build/Test gruen) | siehe oben, erfuellt |
| AK13 (Migrationen idempotent, FreshInstall an beiden Stellen) | siehe unten |
| B4 (Enqueue-Fehler faellt Buchung nicht) | Code-verifiziert: try/catch in `SageBookingEnqueueingStockMovementRepository.AddAsync`, nie wirft; Reconciliation-Sweep `EnqueueMissingAsync` |
| S6 (Decorator = Subclassing) | Code-verifiziert: `class SageBookingEnqueueingStockMovementRepository : StockMovementRepository`, nur `override AddAsync` |
| S7 (Pass-through bei Toggle aus) | `AddAsync_ToggleOff_CreatesNoQueueItem_ButStillSavesMovement` (InMemory) |
| Idempotenz-Guard (Doppel-Enqueue) | `EnqueueAsync_SameMovementTwice_CreatesOnlyOneItem` + UNIQUE-Index `IX_SageBookingQueueItems_StockMovementId` (SQL/83 + FreshInstall) |

Alle Luecken (HTTP/SData-Sende-Pfad, Sage-Memo-Lookup gegen echtes Sage-Testsystem,
Aktivitaets-Protokoll-Anzeige) sind explizit als Manual-UAT in `docs/TESTSZENARIEN.md` Kapitel 56
(TS-56.1–56.11) und `secondbrain/tests/testszenarien-index.md` (Zeile 81/93) gefuehrt — konsistent
mit `secondbrain/architektur/fallstricke.md` „Raw-SQL-/Fremdsystem-Pfade sind nicht
InMemory-testbar".

### Migrations-/SQL-Konsistenz (verifiziert)

- `IdealAkeWms/Migrations/20260803112322_AddSageBookingQueue.cs`: `CreateIndex(... "IX_SageBookingQueueItems_StockMovementId", ..., unique: true)`.
- `SQL/83_AddSageBookingQueue.sql`: `CREATE UNIQUE NONCLUSTERED INDEX [IX_SageBookingQueueItems_StockMovementId]`, `OBJECT_ID`-Guard, DDL/Index/History je eigener Batch, MigrationId `20260803112322_AddSageBookingQueue`.
- `SQL/00_FreshInstall.sql`: Zeilen 121-123 (Spalten `StorageLocations`), 198-222 (Tabelle `SageBookingQueueItems`), 1082-1085 (beide Indizes inkl. UNIQUE), 2182-2185 (beide `MigrationId`s im `__EFMigrationsHistory`-Block) — Schema **und** MigrationId an beiden Pflichtstellen nachgezogen.
- `SQL/82_AddStorageLocationSageLagerbuchung.sql`: `COL_LENGTH`-Guard je Spalte, MigrationId `20260803104055_AddStorageLocationSageLagerbuchung`.

### Code-Review (durchgefuehrt als QA-Agent, kein separater Subagent-Dispatch verfuegbar in dieser Session)

Gezielt gegen die eigenen Blocker/Sollte-Punkte der Kritischen Pruefungen gegengelesen:
- B4 (nicht-transaktionaler Enqueue): geloest — try/catch faengt, wirft nie, Reconciliation-Sweep
  als Netz.
- S6 (Decorator-Bauweise): Subclassing wie festgelegt, kein 15-Methoden-Delegations-Boilerplate.
- S7 (Pass-through-Regression): durch expliziten Test abgesichert.
- DI-Registrierung: Web registriert den Decorator auf `IStockMovementRepository`; Service registriert
  weiterhin die plain `StockMovementRepository` (dort werden nie manuelle Buchungen erzeugt — korrekt,
  keine Doppel-Registrierung).
- Migrations-Dreiklang und Audit-Felder wie oben verifiziert.
- Keine kritischen oder wichtigen Befunde offen; keine Code-Aenderung durch die QA-Abnahme noetig.

### Entscheidung

**Testbereit.** Build und Tests gruen, alle testbaren Akzeptanzkriterien abgedeckt, die
Nicht-InMemory-testbaren Teile sauber als Manual-UAT in `docs/TESTSZENARIEN.md` Kapitel 56
dokumentiert, Migrations-/SQL-Konsistenz verifiziert, Deploy-Abschnitt aus dem echten Diff
finalisiert.

## Manuelle Test-Checkliste (Schranke 2 — vor dem Merge, am Sage-Testsystem)

**Vorbedingung einmalig:** Migration 82+83 eingespielt, `IDEALAKEWMSService/appsettings.json` →
`SageLagerbuchung:Username/Password` gesetzt, `/ServiceSettings` → `SData:BaseUrl`/`SData:Dataset`
gesetzt (Toggle `SageLagerbuchungAktiv` zunaechst **aus**), Lagerplatz-Sync mind. einmal gelaufen.

1. **Toggle-Gating (TS-56.1/56.2):** Mit `SageLagerbuchungAktiv=false` und einem Lagerplatz mit
   `SageBuchungErlaubt=true` eine manuelle Einbuchung durchfuehren → `/SageBookingQueue` bleibt leer.
   Danach Toggle an, Lagerplatz-Flag aus → wieder kein Eintrag. Erst mit **beiden** an entsteht ein
   Eintrag mit Status „Offen".
2. **Echte Zugangs-Buchung (TS-56.3):** Einbuchung auf einem Sage-freigegebenen Testlagerplatz mit
   Testartikel durchfuehren, einen Worker-Tick abwarten. Erwartung: `/SageBookingQueue`-Eintrag
   durchlaeuft Offen → Gesendet → Bestaetigt; die Buchung ist im Sage-Testsystem als **Zugang** auf
   dem korrekten Platz sichtbar (Kurzbezeichnung inkl. Ebenen, z. B. „LL;1;4;0", nicht die
   Lager-Wurzel „LL;0;0;0").
3. **Echte Entnahme-Buchung (TS-56.4):** Spiegelbildlich mit einer Ausbuchung — Sage zeigt
   **Entnahme**, Herkunft/Ziel vertauscht gegenueber Schritt 2.
4. **Timeout-/Doppelbuchungs-Schutz (TS-56.7):** Sende-Timeout simulieren (z. B. Netzwerk kurz
   kappen waehrend eines Sende-Versuchs). Erwartung: Eintrag bleibt auf „Gesendet"; beim naechsten
   Worker-Tick **kein** zweiter automatischer Sende-Versuch — der Recovery-Pfad prueft per
   Sage-Memo-Lookup, ob die Buchung schon existiert, und setzt ggf. auf „Bestaetigt" statt erneut zu
   senden.
5. **Requeue eines Fehler-Eintrags (TS-56.8):** Einen Eintrag mit Status „Fehler" ueber
   „Erneut senden" in `/SageBookingQueue` (Rolle `stock_keyuser`) requeuen. Erwartung: Status wird
   „Offen"; der Worker prueft vor dem erneuten Senden per Memo-Lookup — existiert die Buchung in
   Sage bereits, landet der Eintrag direkt auf „Bestaetigt" statt ein zweites Mal zu senden.
6. **Fehlerpfad Konfigurationsfehler (TS-56.6):** `SageBuchungErlaubt=true` auf einem **manuellen**
   Lagerplatz (ohne `SageLagerkennung`/`SageLagerplatzId`) setzen, dort einbuchen. Erwartung:
   Queue-Eintrag landet auf „Fehler" mit sprechender Meldung, der Worker laeuft weiter (kein
   Absturz, kein haengenbleiben), Folgeeintraege werden normal weiterverarbeitet.
7. **Toggle-Aus-Regression (TS-56.11, S7):** Bei `SageLagerbuchungAktiv=false` stichprobenartig
   Ein-/Ausbuchung, Bestand, Historie und eine Kommissionierung durchspielen — Verhalten muss
   **exakt** wie vor diesem Update sein (der Decorator ist reiner Pass-through, wenn der Toggle
   aus ist).
8. **Aktivitaets-Protokoll (TS-56.10):** Nach den vorigen Schritten `/SyncLog` oeffnen. Erwartung:
   Lauf „SageLagerbuchung" sichtbar mit Counts `gesendet`/`bestaetigt`/`fehler`/`nacherfasst`/
   `uebersprungen`.

**Dev-Lauf-Verifikationen am Testsystem (parallel zu den obigen Schritten zu bestaetigen):**
9. **Memo vs. Referenz:** Pruefen, ob der Sage-Memo-Lookup (`SageBuchungLookupReader`) tatsaechlich
   gegen die richtige Spalte (`Memo`) matcht, oder ob `Referenz` die verlaesslichere Korrelation
   waere — ggf. eine Zeile in `IDEALAKEWMSService/Services/SageBuchungLookupReader.cs` anpassen.
10. **PlatzID-Quelle:** Bestaetigen, dass `KHKLagerplaetze.PlatzID` tatsaechlich der numerische Wert
    ist, den die SData-API als `Herkunft-/ZielLagerplatzId` erwartet (H1-Annahme).
11. **Artikelnummer-Abgleich:** Stichprobenartig `Article.ArticleNumber` gegen die Sage-Artikelnummer
    im `GET Artikel`-Response vergleichen (Frage 4-Annahme, strukturell erwartet identisch).
12. **TLS-Schalter am Testsystem (2026-08-03):** Da `sagetest01.ake.at` derzeit kein gueltiges
    Zertifikat hat, fuer den UAT `SageLagerbuchungSslZertifikatPruefen=false` setzen und pruefen, dass
    (a) die Buchung durchlaeuft, (b) Worker-Start-Log **und** `/ServiceSettings` **und**
    `/SageBookingQueue` den Warnhinweis zeigen, (c) ein Umschalten auf `true` **ohne Dienst-Neustart**
    wieder greift (naechster Handshake schlaegt bei ungueltigem Zertifikat fehl).
13. **Vor Produktivgang:** pruefen, dass `SageLagerbuchungSslZertifikatPruefen` auf **`true`** steht.

Ein-Instanz-Voraussetzung beachten (siehe Deploy-Abschnitt): waehrend des Manual-UAT darf nur
**eine** `SageBookingWorker`-Instanz laufen.

## Re-Verifikation QA (2026-08-03, nach Commit fd3bec8 "TLS-Zertifikatspruefung schaltbar")

Re-Pruefung des Nachtrags `fd3bec8` (Diff `cf0e799..HEAD`, 14 geaenderte Dateien) gegen die zuvor
mit `cf0e799` bestaetigte Testbereit-Basis.

### Beweis: Build

```
> dotnet build IdealAkeWms.slnx -c Debug
Der Buildvorgang wurde erfolgreich ausgeführt.
    9 Warnung(en)   (bestehende NU1902-Advisories MailKit/MimeKit + 1 CS8602 in TrackingController,
                     unveraendert gegenueber der cf0e799-Basis, nicht Teil dieser Ergaenzung)
    0 Fehler(en)
```

### Beweis: Tests

```
> dotnet test IdealAkeWms.slnx -c Debug
IdealAkeWms.Tests.dll        : Fehler: 0, erfolgreich: 1056, übersprungen: 1, gesamt: 1057
IDEALAKEWMSService.Tests.dll : Fehler: 0, erfolgreich:  186, übersprungen: 0, gesamt:  186
```
Gezielter Re-Lauf `--filter "FullyQualifiedName~ServiceSettingDefinitions|FullyQualifiedName~SageTlsPolicy"`:
55/55 gruen (Drift-Guard-Katalogtest + alle 4 `SageTlsPolicyTests`-Theorien).

### Inhaltliche Pruefung der Ergaenzung (a-f)

- **(a) Fail-safe-Semantik:** `SageTlsPolicy.ShouldVerifyCertificate` — `!bool.TryParse(...) || verify`.
  Fehlend/leer/nicht-parsebar (`null`, `""`, `"yes"`, `"1"`, `"tru"`) → `true` (geprueft); nur ein
  explizit parsebares `false` deaktiviert. Bewusst nicht `ServiceSettings.GetBoolAsync` (waere
  fail-open). **Korrekt.**
- **(b) Wirkung nur auf Sage-Client:** `ConfigurePrimaryHttpMessageHandler` haengt ausschliesslich
  am `AddHttpClient<ISageLagerbuchungClient, SageLagerbuchungClient>(...)`-Builder in
  `IDEALAKEWMSService/Program.cs`. Kein `ServicePointManager`, keine globale Handler-Aenderung,
  kein anderer Client betroffen. **Korrekt.**
- **(c) Laufzeit-Lesen im Callback:** `ServerCertificateCustomValidationCallback` ruft
  `ServiceSettings.GetValueSafeAsync(...)` **bei jedem Handshake** auf (kein Capture eines
  einmalig gelesenen Werts beim `ConfigurePrimaryHttpMessageHandler`-Setup); `ServiceSettings.*`
  liest laut eigenem Klassenkommentar bewusst ungecacht direkt aus der DB. **Korrekt**, Aenderung
  greift ohne Dienst-Neustart.
- **(d) Warnungen an allen drei Stellen:** `SageBookingWorker.ExecuteAsync` loggt beim Start
  (`LogWarning(SageTlsPolicy.DisabledWarning)`, wenn deaktiviert); `Views/ServiceSettings/Index.cshtml`
  zeigt am Eintrag ein Warn-Alert; `Views/SageBookingQueue/Index.cshtml` zeigt ein Banner oben, wenn
  `SslCheckDisabled` (Controller liest den Key live via `IServiceSettingRepository`). Alle drei
  vorhanden. **Korrekt.**
- **(e) Testabdeckung der Invarianten:** `SageTlsPolicyTests` deckt fehlend/leer/whitespace/nicht-
  parsebar → `true`, explizites `false` (inkl. Gross-/Kleinschreibung, Leerzeichen) → `false`,
  explizites `true` → `true`, sowie den Katalog-Default (`ServiceSettingDefinitions.All` enthaelt den
  Key mit `DefaultValue == "true"` und dieser Default erfuellt selbst die Invariante). **Korrekt.**
- **(f) "Vor Produktivgang auf true" in der Checkliste:** Vorhanden — Spec-Manual-Checkliste Punkt 13
  ("Vor Produktivgang: pruefen, dass `SageLagerbuchungSslZertifikatPruefen` auf `true` steht"),
  zusaetzlich in README.md, `secondbrain/aufgaben/2026-08-03-deploy-v1-28-0-sage-lagerbuchungen.md`
  und `secondbrain/changelog/2026-08-03-v1-28-0-sage-lagerbuchungen.md`. **Korrekt.**

### Drift-Guard/Katalog

`SageLagerbuchungSslZertifikatPruefen` ist in `ServiceSettingDefinitions.All` eingetragen
(Kategorie „Sage-Lagerbuchung", Bool, Default `"true"`). `ServiceSettingDefinitionsTests` (Teil des
Gesamtlaufs, 1056/1056 gruen) zeigt keinen Drift.

### Gefundene Luecke — NICHT bestaetigt

**`docs/TESTSZENARIEN.md` Kapitel 56 und `secondbrain/tests/testszenarien-index.md` (Zeile 81) wurden
durch Commit `fd3bec8` NICHT aktualisiert** (`git diff cf0e799..HEAD -- docs/TESTSZENARIEN.md
secondbrain/tests/testszenarien-index.md` ist leer). Die Ergaenzung ist zwar an sechs anderen
Stellen sauber dokumentiert (Spec-Nachtrag Abschnitt 6, Spec-Manual-Checkliste Punkt 12/13, README,
Brain-Changelog, `codebase/services.md`, Deploy-Aufgabe) — CLAUDE.md verlangt aber explizit
„Testszenarien-Pflicht ... **nicht verhandelbar**": jedes Feature braucht ein synchronisiertes
`docs/TESTSZENARIEN.md` als „Single Source of Truth der Abnahme", danach den Index-Nachzug. Ein
Tester, der ausschliesslich Kapitel 56 (TS-56.1–56.11) folgt, ohne die Spec zu lesen, erfaehrt dort
nichts vom TLS-Schalter — relevant, weil `sagetest01.ake.at` aktuell ein ungueltiges Zertifikat hat
und TS-56.3/56.4 (echte Buchung am Testsystem) sonst an einem TLS-Handshake-Fehler scheitern, ohne
dass die Ursache dokumentiert ist.

**Konkret fehlend:**
1. Ein bis zwei neue TS-Eintraege in Kapitel 56 (z. B. TS-56.12/56.13), die den bereits in der
   Spec-Checkliste (Punkt 12/13) beschriebenen TLS-Schalter-Ablauf im offiziellen TS-Format
   (Vorbedingung/Schritt/Erwartung) abbilden — oder mindestens ein Vorbedingungs-Hinweis bei
   TS-56.3/56.4, dass am aktuellen Testsystem vorher `SageLagerbuchungSslZertifikatPruefen=false`
   zu setzen ist.
2. `secondbrain/tests/testszenarien-index.md` Zeile 81 entsprechend nachziehen (TS-Nummernbereich
   und/oder Hinweistext).

**Ursache:** vermutlich Umfangs-Annahme des Umsetzungs-Agenten, dass die Spec-Manual-Checkliste
(Punkt 12/13) fuer diesen kleinen Nachtrag ausreicht — reicht nach dem harten CLAUDE.md-Wortlaut
aber nicht, da `docs/TESTSZENARIEN.md` explizit als separates Pflichtdokument benannt ist.

**Kein Code-/Logik-Defekt.** Build, Tests, TLS-Policy-Logik, Warnhinweise und Drift-Guard sind
vollstaendig verifiziert und korrekt (siehe oben a-f). Status bleibt daher **InUmsetzung** bis die
beiden obigen Punkte nachgezogen sind; danach ist eine erneute (voraussichtlich sehr kurze)
QA-Runde ausreichend, um wieder auf Testbereit zu setzen — kein neuer vollstaendiger Review-Zyklus
noetig.

**ESCALATE:** nein (erster Fund, kein Fixversuch unternommen — Nachbesserung ist eine reine
Dokumentationsergaenzung durch den Umsetzungs-Agenten, kein QA-Retry-Fall).

### Re-Re-Verifikation QA (2026-08-03, nach Commit 2afb77f "TS-56.12/56.13 nachgezogen")

Luecke geschlossen. `git diff 54c1a38..2afb77f` betrifft ausschliesslich `docs/TESTSZENARIEN.md`
(+20 Zeilen: Vorbedingungs-Hinweis TLS am Testsystem + TS-56.12 mit a/b/c inkl. Negativfall
fail-safe + TS-56.13 Vor-Produktivgang-Check) und `secondbrain/tests/testszenarien-index.md`
(Zeile 56 auf TS-56.1–56.13 erweitert, TLS-Schalter explizit benannt). Keine Code-Datei
veraendert — die inhaltliche Pruefung (a-f) aus der vorigen Runde bleibt unveraendert gueltig.

**Beweis Build:**
```
> dotnet build IdealAkeWms.slnx -c Debug
Der Buildvorgang wurde erfolgreich ausgeführt.
    9 Warnung(en)  (unveraendert: NU1902 MailKit/MimeKit + 1 CS8602 TrackingController)
    0 Fehler(en)
```

**Beweis Tests:**
```
> dotnet test IdealAkeWms.slnx -c Debug
IdealAkeWms.Tests.dll        : Fehler: 0, erfolgreich: 1056, übersprungen: 1, gesamt: 1057
IDEALAKEWMSService.Tests.dll : Fehler: 0, erfolgreich:  186, übersprungen: 0, gesamt:  186
```
Identisch zur vorigen Runde (nur Doku geaendert, keine Testverschiebung).

**Kapitel-56-Pruefung:** Vorbedingungen-Block nennt jetzt den TLS-Workaround fuer TS-56.3/56.4/
56.7/56.8 explizit; TS-56.12 deckt Aus/Sichtbarkeit-an-3-Stellen/An-ohne-Neustart + Negativfall
fail-safe ab; TS-56.13 deckt „vor Produktivgang auf true" ab. Index-Zeile 56 verweist korrekt auf
TS-56.1–56.13 und nennt den TLS-Schalter. Alle vier Punkte aus dem Gap-Report erfuellt.

**Entscheidung: Testbereit.** Build gruen, Tests gruen (1056+186, 1 uebersprungen wie durchgehend),
inhaltliche Ergaenzung (a-f) weiterhin korrekt, Testszenarien-Pflicht jetzt vollstaendig erfuellt
(Spec + docs/TESTSZENARIEN.md + Index synchron). Status zurueck auf Testbereit.
