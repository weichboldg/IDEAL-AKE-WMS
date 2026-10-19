---
type: spec
title: "BDE Stufe 1 – Arbeitsgang an mehreren Werkbänken, veraltete Sage-Werkbänke kennzeichnen, Werkbank-Option „nur AG-Fertigmeldung“"
slug: 2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec
status: Testbereit
created: 2026-10-19
updated: 2026-10-19
source_backlog: "[[2026-10-19-bde-lastenheft-abgleich]]"
related_backlog: "[[2026-10-19-werkbank-katalog-veraltete-sage-arbeitsplaetze]]"
task: "[[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Models/ProductionWorkplace.cs (neu: SageMissingSince, BdeNurAgFertigmeldung)"
  - "IdealAkeWms/Data/ApplicationDbContext.cs (ProductionWorkplace-Konfiguration ~:205-216)"
  - "IdealAkeWms/Migrations/<ts>_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung.cs (neu)"
  - "SQL/97_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung.sql (neu), SQL/00_FreshInstall.sql (2 Stellen)"
  - "IDEALAKEWMSService/Services/ProductionWorkplaceSyncService.cs (Kennzeichnung nicht in Sage, Empty-Guard, Counts)"
  - "IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs (Katalog ohne gekennzeichnete, mehrere Werkbaenke -> anlegen)"
  - "IdealAkeWms/Services/WerkbankKuerzelFamilie.cs (neu, reiner Helfer)"
  - "IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs + WorkOperationRepository.cs (GetOpenByWorkplaceIdsAsync)"
  - "IdealAkeWms/Data/Repositories/IProductionWorkplaceRepository.cs + ProductionWorkplaceRepository.cs (UpdateAsync nur Pflegefelder)"
  - "IdealAkeWms/Controllers/BdeApiController.cs (GetAvailableOperations, Familie)"
  - "IdealAkeWms/Services/BdeScanResolver.cs (BuildNormalCandidatesAsync, Familie)"
  - "IdealAkeWms/Services/BdeBookingService.cs + IBdeBookingService (ReportCompletionAsync, Start-Sperre FA-c6, Familien-Pruefung FA-a9 in StartPlannedAsync :36-105 / ResumeAsync :162-240 / ReportCompletionAsync)"
  - "IdealAkeWms/Controllers/BdeTerminalController.cs (Index-ViewBag, Action ReportCompletion; StartProductionForOrder :62-68 Pruefung ProductionOrder.ProductionWorkplaceId (Antwort 12))"
  - "IdealAkeWms/Views/BdeTerminal/Index.cshtml + wwwroot/js/bde-terminal.js (Knopf Fertig melden)"
  - "IdealAkeWms/Controllers/FaHierarchyController.cs (:114-127 Werkbank-Gate ueber Familie)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs (NamenLookup ohne gekennzeichnete, :268-273)"
  - "IdealAkeWms/Controllers/ProductionWorkplacesController.cs (Create :108-120, Edit :195-211: Kuerzel nur bei DB-Wert SageArbeitsplatznummer NULL, FA-a8) + Views/ProductionWorkplaces/{Index,Edit,Create}.cshtml + ProductionWorkplaceEditViewModel"
  - "IdealAkeWms/Services/ColumnFilterSyntax.cs (:63 Register ProductionWorkplaces/Index)"
  - "Tests: WorkOperationStructureDetectionServiceTests, ProductionWorkplaceSyncServiceTests, BdeScanResolverTests, BdeApiControllerTests, BdeBookingServiceTests (inkl. FA-a9), ProductionWorkplacesControllerTests + ProductionWorkplaceRepositoryTests (FA-a8), WerkbankKuerzelFamilieTests (neu)"
  - "AppVersion.cs x2 (v1.63.0), Views/Help/Changelog.cshtml, Views/Help/Index.cshtml, README.md, docs/TESTSZENARIEN.md (TS-96)"
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: "B"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-10-19"
---

> [!info] Quellen und Code-Stand
> Backlog [[2026-10-19-bde-lastenheft-abgleich]] (Abschnitt „Plan nach Gerald“, **nur Stufe 1, Punkte a–c**) und
> [[2026-10-19-werkbank-katalog-veraltete-sage-arbeitsplaetze]] (Befund + Bestätigung Gerald). Lastenheft [[bde_ideen]]
> Nr. 12 „Rückmelden“ als Hintergrund für (c). Alle Belege: Bündel-Worktree `2026-08-07-ideal-teile-1-5`, Stand v1.62.0,
> nur gelesen. Pfade relativ zu `IdealAkeWms/` bzw. `IDEALAKEWMSService/`.
> Variantenentscheidung zu (a): **Variante B** (Gerald 2026-10-19), siehe Freigabe-Nachtrag.

> [!important] Freigabe-Nachtrag 2026-10-19 (geht dem Rumpf vor)
> Gerald hat alle 11 Rückfragen wie empfohlen beantwortet (Block „Freigabe-Antworten“). Verbindlich für den Rumpf:
> 1. **Variante B** für (a): Der AG behält eine bevorzugte Werkbank (BDE-aktive zuerst, dann kleinste Id); Terminal,
>    Resolve-Scan und FA-Baum-Gate arbeiten über die Kürzel-Familie.
> 2. **Kürzel auf beiden Wegen:** Sage-Werkbänke bleiben Sage-geführt (zweiter Arbeitsplatz mit gleichem
>    USER_ArbeitsSchritt kommt über Sage/View); an Werkbänken **ohne** Sage-Nummer ist das Kürzel im WMS pflegbar (FA-a8).
>    Ein Kürzel je Werkbank, kein N:M.
> 3. **Sicherheitslücke wird in dieser Spec mitbehoben** (ersetzt FA-a4 „keine neue Prüfung“ und den Zusatzfund zu
>    `BdeBookingService.cs:36-104`): Start (`StartPlannedAsync`), Fortsetzen (`ResumeAsync`) und `ReportCompletion`
>    nehmen einen AG nur an, wenn dessen `ProductionWorkplaceId` in der Kürzel-Familie der Terminal-Werkbank liegt.
>    Sonst Fehlermeldung, keine Buchung. Gilt auch für Werkbänke ohne Kürzel (Familie = nur die Werkbank selbst).
>    Eigenes AK und eigene Tests (manipulierter POST mit fremdem AG wird abgewiesen).
> 4. Übrige Antworten 5–11 wie im Rumpf empfohlen: gekennzeichnete Werkbänke nur melden; `BdeNurFaMeldung` gewinnt;
>    Fertigmeldung bei fremd laufender Buchung nach der Start-Regel; Start-Sperre Rüsten/Produktion, Tätigkeiten frei;
>    Gut-Menge mit FA-Sollmenge vorbelegt; `StartedAt == EndedAt`, kein neuer Buchungstyp; nur Aktivitäts-Protokoll.

## Ziel / Nutzen (das Warum)

Das IDEAL-BDE-Terminal „Entgraten Fläche“ zeigt für FA 1059463 keine Arbeitsgänge: Sub-FA 1065150 hat nur ST, EG und KA
fehlen. Ursache (vom Backlog am Code belegt, von Gerald bestätigt): Die Werkbank-Anlage kennzeichnet Sage-Arbeitsplätze, die
nicht mehr in der Quelle stehen, nie. Die alten „W1“-Werkbänke (z. B. Id 19, Sage-Nr. 2150) behalten ihr Kürzel, die
Struktur-Erkennung findet je Kürzel zwei Werkbänke und legt deshalb gar nichts an
(`IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs:120-126`). Betroffen sind 8 Kürzel, darunter EG mit 93,
KA mit 94 und VM mit 58 Aufträgen.

Stufe 1 baut **Vorhandenes** aus, statt Neues zu erfinden:

- **(b)** Veraltete Sage-Werkbänke werden gekennzeichnet und zählen nicht mehr für den Kürzel-Abgleich. Damit ist der Bug
  behoben.
- **(a)** Derselbe Arbeitsgang (dasselbe Kürzel) ist an jeder BDE-aktiven Werkbank mit diesem Kürzel stempelbar. Die Buchung
  speichert schon heute die tatsächliche Werkbank. Ein Kürzel an mehreren Werkbänken verhindert die Anlage nicht mehr.
- **(c)** Eine neue Werkbank-Option „BDE nur AG-Fertigmeldung“: Der Werker meldet einen Arbeitsgang fertig, ohne Start/Stop.
  Festgehalten werden wer, wann, wo und welche Menge (Lastenheft Nr. 12 ohne SOLL-Zeit).

AKE verhält sich unverändert, Begründung je Stelle im Abschnitt „Wirkung je Stelle“.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. (b) Kennzeichnung `SageMissingSince` an Sage-Werkbänken in `ProductionWorkplaceSyncService`: nie löschen, Empty-Guard,
   selbstheilend, Meldung im Aktivitäts-Protokoll. Dazu eine Anzeige-Spalte in Stammdaten › Werkbänke.
2. (b) Struktur-Erkennung: Der Katalog nimmt keine gekennzeichneten Werkbänke mehr. FA-Baum-Namenslookup ebenso.
3. (a) Kürzel-Familie: Terminal (`available-operations`), Scan (`/api/bde/resolve-scan`, Normal-Modus) und FA-Baum-Status
   finden offene Arbeitsgänge über das Kürzel der Werkbank. Die Struktur-Erkennung legt bei mehreren aktuellen Werkbänken je
   Kürzel an, mit einer bevorzugten Werkbank, und meldet das.
4. (c) Werkbank-Option `BdeNurAgFertigmeldung` (Default aus) mit Terminal-Knopf „Fertig melden“ samt Mengen-Eingabe. Die
   Buchung speichert Start = Ende (Dauer 0) mit End-Menge.
5. (a) Kürzel an Werkbänken ohne Sage-Nummer im WMS pflegbar (FA-a8, Antwort 1). Sage-Werkbänke bleiben Sage-geführt,
   serverseitig geschützt.
6. (a) Familien-Prüfung beim Buchen (FA-a9, Freigabe-Nachtrag Punkt 3): Start, Fortsetzen und Fertigmeldung nehmen nur AGs
   der Kürzel-Familie der Terminal-Werkbank an. Schließt die bisherige Lücke „manipulierter POST bucht jeden AG“.
7. Migration (2 Spalten), Tests, v1.63.0, Hilfe, Changelog, README, TS-96 (TS-96.1–96.10), Brain-Nachträge.

**Out-of-Scope** (Abgrenzung / Ausblick)
- **Stufe 2** des Plans (je eigene Backlog-Notiz/Spec, Reihenfolge mit Gerald): Nr. 11 Priorität am Terminal, Nr. 9
  Sammelauftrag, Nr. 2 Teileverfolgung IDEAL, Nr. 17 Transportlisten, Nr. 6 Arbeitsvorrat/Planung.
- **SOLL-Zeiten** gibt es nicht (Gerald 2026-10-19), also kein SOLL=IST, kein Fertigungsgrad in %, kein SOLL-IST-Reporting.
- N:M-Kürzel je Werkbank (Antwort 2: ein Kürzel je Werkbank), Umhängen bestehender Arbeitsgänge (durch die Kürzel-Familie unnötig, siehe (a)),
  Abbuchung vom Werkbanklager bei Fertigmeldung (Lastenheft Nr. 12, zweiter Halbsatz), Etikettendruck (Nr. 10).
- Der NurFA-Modus (`BdeNurFaMeldung`) bleibt FA-bezogen über `ProductionOrder.ProductionWorkplaceId`
  (`Services/BdeScanResolver.cs:127-157`, `Controllers/BdeApiController.cs:216-253`). Keine Kürzel-Familie dort. Buchungs-Prüfung im
  NurFA-Pfad: **Antwort 12 (Gerald 2026-10-19):** Im NurFA-Modus prüfen Start und Fortsetzen statt der Kürzel-Familie, dass `ProductionOrder.ProductionWorkplaceId` des AG gleich der Terminal-Werkbank ist (deckungsgleich mit dem NurFA-Angebot); FA ohne Werkbank → abgewiesen. NurFA wird bei IDEAL nicht aktiviert; der Pfad wird per Unit-Test abgesichert.
- MK, ZS, MO (unbekannte Kürzel): Pflege in Sage, kein Code.
- Die fehlende Filterkarte auf `Views/ProductionWorkplaces/Index.cshtml` und die fehlende Zahnrad-Registrierung von
  `ProductionWorkplaces` in `Models/ViewModels/ColumnDefinitions.cs` sind Altschuld. Nur melden, nicht mitbauen; passt zur
  offenen Backlog-Notiz `2026-10-14-spaltenauswahl-pflicht-fehlende-listen`.

## Fachliche Anforderungen

### Ist-Stand (belegt)

| Thema | Beleg |
|---|---|
| `WorkOperation.ProductionWorkplaceId` ist **schon nullable** (`int?`) | `Models/WorkOperation.cs:20-22` |
| Die Buchung speichert die Werkbank des Terminals, nicht die des AG | `BdeBooking.ProductionWorkplaceId` `[Required] int` (`Models/BdeBooking.cs:11-13`), gesetzt aus dem Parameter `workplaceId` (`Services/BdeBookingService.cs:338`, `:223`, `:129`) |
| Die Start-Logik prüft **nicht**, ob der AG zur Werkbank gehört (v1.62.0; wird durch FA-a9 behoben) | `StartPlannedAsync` prüft nur „Werkbank BDE-aktiv“ (`:41`), „FA verpackt“ (`:47`), Kollision (`:56-64`) und eigene Buchungen (`:67-101`) (`Services/BdeBookingService.cs:36-105`). `ResumeAsync` ebenso: BDE-Gate `:166`, Parent pausiert `:169-172`, verpackt `:177-181`, Kollision `:187-194`, eigene `:197-217`, neue Buchung mit `WorkOperationId = parent.WorkOperationId` und `ProductionWorkplaceId = workplaceId` `:220-231`. Ein nicht existierender `workOperationId` läuft bis zum Insert durch (`EnsureOrderNotPackedAsync` gibt bei fehlendem AG `null` zurück, `:436-440`) |
| Buchungs-Einstiege (alle POSTs, die eine **neue** Buchung anlegen) | `BdeTerminalController`: `StartSetup` `:71-75`, `StartProduction` `:78-82` (beide → `StartPlannedAsync`), `StartProductionForOrder` `:62-68` (NurFA → `FindOrCreateDefaultAsync` → `StartPlannedAsync`), `Resume` `:99-116` (→ `ResumeAsync`), `StartActivity` `:85-89` (→ `StartActivityAsync`, `:107-140`, ohne AG). Der Scan legt nichts an: `BdeScanResolver` hat kein `SaveChanges`/`Add` (Grep), das JS ruft danach `StartSetup`/`StartProduction` bzw. `StartProductionForOrder` (`wwwroot/js/bde-terminal.js:325-326`, `:220`, `:518`). `BdeApiController` hat keine POST-Aktion (Grep `[HttpPost`: 0 Treffer). Service: `BdeAutoPauseService.cs:50-61` pausiert nur |
| Pausierte Buchungen werden werkbank-übergreifend angeboten | `PausedBookings(operatorId)` filtert nur nach Werker (`BdeTerminalController.cs:233-254`), „Fortsetzen“ schickt die **aktuelle** Terminal-Werkbank (`bde-terminal.js:615-624`) und zeigt `data.message` an (`:640-641`) |
| NurFA-Default-AG „01“ hängt an der Werkbank des ersten Starters | Suche nur über `ProductionOrderId` + `OperationNumber == "01"` (`Services/BdeDefaultWorkOperationService.cs:27-31`), Anlage mit `ProductionWorkplaceId = workplaceId` (`:39`). Das NurFA-Angebot hängt an `ProductionOrder.ProductionWorkplaceId` (`BdeApiController.cs:220`), geschrieben u. a. von `FaCompletionController.cs:430` und `IDEALAKEWMSService/Services/OseonSyncService.cs:544` |
| Das Terminal-Angebot hängt an einer einzigen Werkbank-Id | `GetOpenByWorkplaceIdAsync`: `wo.ProductionWorkplaceId == workplaceId && !wo.IsReported` (`Data/Repositories/WorkOperationRepository.cs:59-82`), Aufruf `Controllers/BdeApiController.cs:256` |
| Der Scan (Normal-Modus) hängt an derselben Werkbank-Id | `Services/BdeScanResolver.cs:166-171` |
| Weiterer Aufrufer der Werkbank-Abfrage: Rückmeldung/Tracking | `Controllers/TrackingController.cs:222-224` (Default `excludePackedOrders=false`) |
| Das Cockpit gruppiert nach der Buchungs-Werkbank | `Controllers/BdeApiController.cs:181`, Quelle `GetActiveCockpitAsync` `EndedAt == null` (`Data/Repositories/BdeBookingRepository.cs:55-63`) |
| Das FA-Baum-Werkbank-Gate prüft die Werkbank des AG | `Controllers/FaHierarchyController.cs:125-127`, `Models/ViewModels/FaArbeitsgangStatus.cs:80-81` |
| „Erledigt“ = End-Menge (`IsFinal`) auf einer nicht stornierten Buchung | `Data/Repositories/WorkOperationRepository.cs:159-163`, Terminal `Controllers/BdeApiController.cs:263-268` |
| Struktur-Erkennung: der Katalog nimmt **alle** Werkbänke mit Kürzel, bei Mehrdeutigkeit wird nichts angelegt | `WorkOperationStructureDetectionService.cs:80-88`, `:120-126`, Meldung `:195-205`, Count `arbeitsschritt_mehrdeutig` `:235` |
| Werkbank-Anlage: legt an und zieht Name/Kürzel nach, kennzeichnet aber **nie** | `ProductionWorkplaceSyncService.cs:119-191` (kein Zweig für „im WMS, nicht in der Quelle“) |
| Werkbank-Anlage: Leer- und Fehlt-Quelle | Fehlt: Warnung + Skip (`:76-89`). Leer: kein Guard, das Upsert läuft leer durch (`:119`) |
| **Schreibwege `ArbeitsschrittCode`:** nur die Werkbank-Anlage | `ProductionWorkplaceSyncService.cs:139`, `:185`. Web-Create/Edit schreiben es nicht (`Controllers/ProductionWorkplacesController.cs:108-120`, `:199-209`). Edit zeigt es nur bei Sage-Werkbänken read-only (`Views/ProductionWorkplaces/Edit.cshtml:25-49`). Die Sage-Nummer kommt als `readonly`-Input mit dem Formular zurück (`Edit.cshtml:36`), ist also vom Client manipulierbar; Create setzt nie eine Sage-Nummer (`ProductionWorkplacesController.cs:108-120`) |
| Die Werkbank-Anlage läuft nur bei Master `ProduktionsauftragHierarchisch` | Doppel-Gate `IDEALAKEWMSService/Workers/SyncWorker.cs:377-378`, Struktur-Erkennung `:414-415` |
| **Schreibwege `WorkOperation`** (Produktivcode) | Struktur-Erkennung (`WorkOperationStructureDetectionService.cs:168-181`), NurFA-Default „01“ (`Services/BdeDefaultWorkOperationService.cs:34-48`), Tracking setzt nur `IsReported` (`Controllers/TrackingController.cs:254`, `:283`). `ExternalSource` wird nirgends geschrieben, nur gelesen (`TrackingController.cs:420`). Kein Insert in `SQL/AgentJobs/` (nur `02_Import_Artikel.sql`, deprecated, Artikel) |
| Web-Update schreibt alle Spalten | `ProductionWorkplaceRepository` überschreibt `UpdateAsync` nicht. Basis `_dbSet.Update(entity)` (`Data/Repositories/Repository.cs:40-44`), Fallstrick §24 |
| Mehrfach-Schalter | `BdeMehrfachBuchungProArbeitsgang`: Kollisionsprüfung nur bei Rüsten oder `!multiMa` (`Services/BdeBookingService.cs:51-64`, Resume `:183-194`). Nach „Beenden mit Menge“ bei `multiMa` die Liste der anderen Laufenden (`Controllers/BdeTerminalController.cs:169-191`) und der Andere-schließen-Dialog (`wwwroot/js/bde-terminal.js:334-336`, `CloseOtherBookingsOnWorkOperationAsync` `BdeBookingService.cs:305-327`) |
| `BdeNurFaMeldung` ist global und FA-bezogen, **mit** Start/Stop | Terminal-Modus aus AppSetting (`Controllers/BdeTerminalController.cs:55-57`). FA-Klick und Scan starten Produktion auf dem Default-AG „01“ (`BdeTerminalController.cs:62-68`, `bde-terminal.js:219-224`, `:309`). Beenden mit Menge wie im Normal-Modus |
| Zeitaufteilung blendet Buchungen der Dauer 0 aus | `Services/BdeTimeSplitService.cs:43` (`.Where(x => x.End > x.Start)`) |
| DB-Constraints erlauben Finished mit Start = Ende | `CK_BdeBookings_StatusEnded` (Status 3 verlangt nur `EndedAt IS NOT NULL`), `CK_BdeBookings_TypeTarget` (Produktion verlangt `WorkOperationId`), `Migrations/ApplicationDbContextModelSnapshot.cs:517-521`. Keine Prüfung `EndedAt > StartedAt` |

### (b) Veraltete Sage-Werkbänke kennzeichnen

- FA-b1: Jeder Lauf der Werkbank-Anlage mit gelesener, **nicht leerer** Quelle setzt `SageMissingSince = DateTime.Now` an jeder
  Werkbank mit `SageArbeitsplatznummer IS NOT NULL`, deren getrimmte Nummer in **keiner** Quellzeile vorkommt. Bereits
  gekennzeichnete Werkbänke bleiben unverändert, der Zeitstempel bleibt also der erste.
- FA-b2: Präsenz-Menge = getrimmte `Arbeitsplatznummer` **aller rohen Quellzeilen**, vor dem Pflicht-/Ausschluss-Filter
  (`ProductionWorkplaceSyncService.cs:126`). Eine Zeile, deren Kürzel auf der Ausschlussliste steht oder leer ist, steht
  trotzdem in Sage und darf ihre Werkbank nicht kennzeichnen. Muster: Fallstrick §5 „`sagePresentKeys` aus den rohen
  Zeilen“. Vergleich ordinal wie der bestehende Schlüssel (`:108`, nie `int.Parse`, `ProductionWorkplace.cs:30-33`).
- FA-b3: Selbstheilend. Steht eine gekennzeichnete Nummer wieder in der Quelle, wird `SageMissingSince = NULL` gesetzt und als
  „wieder in Sage“ gemeldet.
- FA-b4: **Nie löschen**, und `BdeAktiv`, Name, Kürzel und Zuordnungen bleiben unberührt (Antwort 5: nur melden, BDE nicht
  automatisch aus). Werkbänke ohne
  `SageArbeitsplatznummer` (AKE, manuell angelegte) werden nie gekennzeichnet.
- FA-b5: **Empty-Guard.** Liefert die Quelle 0 Zeilen, wird weder gekennzeichnet noch entkennzeichnet. Warnung „Sage-Quelle
  {quelle} liefert keine Zeilen — keine Kennzeichnung“ (Muster `OperationConfigSyncService.cs:129-135`). Fehlt die Quelle,
  bleibt es beim bestehenden Skip (`ProductionWorkplaceSyncService.cs:76-89`).
- FA-b6: Meldungen **nur im Aktivitäts-Protokoll, keine Mail** (Antwort 11; deutsche Counts-Keys, an das bestehende
  `FinishSuccessAsync` `:205-212` angehängt):
  `nicht_in_sage` (Stand nach dem Lauf), `nicht_in_sage_neu`, `wieder_in_sage`. Eine Detailzeile je Kategorie mit
  „Nummer Name (Kürzel)“, gekappt wie `OperationConfigSyncService.cs:36-37`, `:80-86`. **Zusatzwarnung**, wenn eine
  gekennzeichnete Werkbank `BdeAktiv = true` hat: „Werkbank {Name} ({Nr.}) ist BDE-aktiv, steht aber nicht mehr in Sage.“
- FA-b7: DryRun zählt und meldet, schreibt aber nichts (wie `:133`, `:182`).
- FA-b8: Audit an jeder Kennzeichnungsänderung: `ModifiedAt/By/ByWindows = "ProductionWorkplaceSync"` (Muster `:186`).
- FA-b9: Struktur-Erkennung: Der Katalog (`WorkOperationStructureDetectionService.cs:81-88`) nimmt nur Werkbänke mit
  `SageMissingSince == null`. Ein Kürzel, das nur noch an gekennzeichneten Werkbänken hängt, erscheint in der bestehenden
  „Unbekannt“-Meldung samt Sammelmail (`:207-224`). Das ist gewollt: Es wird gemeldet, nicht still behandelt.
- FA-b10: Der FA-Baum-Namenslookup (`Models/ViewModels/FaHierarchyTreeViewModel.cs:268-273`) übergeht gekennzeichnete
  Werkbänke. EG hat dann wieder genau einen Namen statt keinen.
- FA-b11: Stammdaten › Werkbänke: neue Spalte „Nicht in Sage seit“ (Datum `dd.MM.yyyy`, leer = aktuell), Spaltenfilter
  Server-Mode mit `data-col-key="sage-missing"`, Typ Datum an `<th>` **und** im Register `ColumnFilterSyntax.cs:63`
  (Fallstrick §26). `!(leer)` zeigt alle gekennzeichneten. Edit-Seite: Hinweis-Alert „Steht seit … nicht mehr in den
  Sage-Arbeitsplätzen; zählt nicht für den Kürzel-Abgleich.“ Text plus Badge, Farbe nie allein.

### (a) Arbeitsgang an mehreren Werkbänken – Variante B (entschieden, Antwort 3)

Umgesetzt wird **Variante B: bevorzugte Werkbank + Kürzel-Familie**. `WorkOperation.ProductionWorkplaceId` bleibt gesetzt
(bevorzugte Werkbank, Antwort 4). Eine Werkbank W sieht und bucht offene AGs aller Werkbänke **mit demselben Kürzel wie W**.
Keine Schemaänderung an `WorkOperation`, bestehende Lesestellen bleiben gültig, Altbestand an der W1-Werkbank (Id 19, Kürzel EG)
erscheint an Id 65 ohne Umhängen, AKE ohne Kürzel bleibt unverändert. Eine Regel an einer Stelle (Helfer).

**Verworfen** (Kurzbegründung, Bewertung im ursprünglichen Entwurf):
- **A — Werkbank leer** (`ProductionWorkplaceId = NULL` bei mehreren Werkbänken): FA-Baum-Gate (`FaArbeitsgangStatus.cs:80`
  verlangt `int`), Tracking je Werkbank (`TrackingController.cs:222-224`) und die Sage-Nr. im AG-Modal
  (`Views/FaHierarchy/_FaWorkOperationsModal.cshtml:60`) verlören den AG; Altbestand an W1-Werkbänken bliebe unsichtbar.
- **C — reines Kürzel-Match auf `OperationNumber`**: leitet Zugehörigkeit aus einem Freitext ab und trifft fremde AGs (NurFA „01“).

**Fachliche Regel (Variante B):**

- FA-a1 **Kürzel-Familie** einer Werkbank W: Ist `W.ArbeitsschrittCode` leer oder `W.SageMissingSince != null`, dann
  `{W.Id}` (bisheriges Verhalten). Sonst `{W.Id}` ∪ alle Werkbänke X mit `Trim(X.ArbeitsschrittCode)` gleich
  `Trim(W.ArbeitsschrittCode)`, OrdinalIgnoreCase wie die Erkennung (`WorkOperationStructureDetectionService.cs:80`). X darf
  gekennzeichnet oder BDE-inaktiv sein, denn genau so wird Altbestand an W1-Werkbänken sichtbar. Vergleich in C# über die
  geladenen Werkbänke (rund 90 Zeilen), nicht in SQL: Fallstrick §26, Kollation.
- FA-a2 Terminal-Angebot (`BdeApiController.GetAvailableOperations`, Normal-Modus): offene AGs der Familie statt der einen Id.
  Alle übrigen Filter bleiben: verpackt/abgeholt, fertig gemeldet, eigene laufende ausblenden, fremd laufende als „läuft“
  zeigen (`BdeApiController.cs:256-285`).
- FA-a3 Scan `/api/bde/resolve-scan` (Normal-Modus): `BuildNormalCandidatesAsync` mit derselben Familie (deckungsgleich zu
  FA-a2, Klassenkommentar `BdeScanResolver.cs:15-17`). `NotInScope` meint dann „an keiner Werkbank dieser Familie offen“.
  Die Terminal-Meldung (`bde-terminal.js:205`) bleibt sinngemäß richtig.
- FA-a4 Buchung: `BdeBooking.ProductionWorkplaceId` = Werkbank des Terminals, unverändert (`BdeBookingService.cs:338`, `:223`).
  ~~Keine neue Prüfung „AG gehört zur Werkbank“~~ (**überholt** durch Freigabe-Nachtrag Punkt 3): Die Zugehörigkeit prüft
  jetzt FA-a9.
- FA-a5 Struktur-Erkennung: Trifft ein Token **mehrere** aktuelle Werkbänke (nach FA-b9), wird der AG **angelegt** mit
  `ProductionWorkplaceId` = bevorzugte Werkbank: BDE-aktive zuerst, dann kleinste Id (Antwort 4). `Name` kommt von der
  bevorzugten Werkbank. Das Überspringen (`:122-126`) und die Warnung „mehrdeutig“ (`:193-205`) entfallen. Stattdessen:
  Count `arbeitsschritt_mehrere_werkbaenke` (Anzahl Kürzel) und **eine Info-Zeile je Lauf** „Kürzel an mehreren Werkbänken
  ({n}): EG (140 Entgraten Fläche*, 141 …) — Arbeitsgang angelegt an * (bevorzugt), stempelbar an allen“. Der Count-Key
  `arbeitsschritt_mehrdeutig` entfällt.
- FA-a6 Nur-hinzufügen bleibt (`:151-165`). Bestehende AGs werden nicht umgehängt (Fallstrick §16 „Umzug routet Altbestand
  nicht um“). Über die Familie ist das für das Terminal nicht mehr nötig.
- FA-a7 FA-Baum-Status: Das Werkbank-Gate (`FaHierarchyController.cs:125`) nimmt als „BDE-relevant“ jede Werkbank, die
  BDE-aktiv ist **oder** deren Kürzel an einer BDE-aktiven, nicht gekennzeichneten Werkbank steht. `FaArbeitsgangStatus` und
  `ArbeitsgangStatusRegel` bleiben **unverändert**, nur die übergebene Id-Menge wächst.
- FA-a8 **Kürzel auf beiden Wegen** (Antworten 1 und 2): Die Kürzel-Familie wirkt, sobald zwei Werkbänke dasselbe Kürzel
  tragen.
  - Sage-Werkbänke (`SageArbeitsplatznummer` gesetzt) bleiben Sage-geführt: Ein zweiter Arbeitsplatz mit gleichem
    `USER_ArbeitsSchritt` kommt über Sage/View und die Werkbank-Anlage. Im WMS read-only.
  - An Werkbänken **ohne** `SageArbeitsplatznummer` ist `ArbeitsschrittCode` in Create und Edit pflegbar (max. 20, getrimmt,
    leer = NULL). Create setzt nie eine Sage-Nummer (`ProductionWorkplacesController.cs:108-120`), dort also immer pflegbar.
  - **Serverseitig, nicht nur ausgegraut:** Edit-POST übernimmt `vm.ArbeitsschrittCode` nur, wenn der **DB-Wert**
    `existing.SageArbeitsplatznummer` NULL ist (`ProductionWorkplacesController.cs:195-211`). Maßgeblich ist nie
    `vm.SageArbeitsplatznummer`, denn die kommt als `readonly`-Input vom Client (`Edit.cshtml:36`) und ist manipulierbar.
    Zusätzlich markiert `ProductionWorkplaceRepository.UpdateAsync` das Kürzel nur bei Nicht-Sage-Werkbänken als geändert
    (Lösungsentwurf Schritt 3). Ein manipulierter POST an einer Sage-Werkbank ändert den gespeicherten Wert nicht.
  - Ein Kürzel je Werkbank, kein N:M (Antwort 2). Kein Unique auf dem Kürzel, gewollt.
- FA-a9 **Familien-Prüfung beim Buchen** (Freigabe-Nachtrag Punkt 3, ersetzt FA-a4 „keine neue Prüfung“):
  - `StartPlannedAsync` (`BdeBookingService.cs:36-105`, damit `StartSetup` und `StartProduction`), `ResumeAsync`
    (`:162-240`, nur wenn `parent.WorkOperationId` gesetzt ist) und `ReportCompletionAsync` (FA-c4) nehmen einen AG nur an,
    wenn `WorkOperation.ProductionWorkplaceId` in `WerkbankKuerzelFamilie.FamilieIds(Terminal-Werkbank, alle Werkbänke)`
    liegt. Werkbank ohne Kürzel oder gekennzeichnet: Familie = nur die Werkbank selbst (FA-a1).
  - Bei Verstoß: `BdeBookingResult.Invalid("Dieser Arbeitsgang gehört nicht zu dieser Werkbank.")`, **keine** Buchung, kein
    Auto-Close eigener Buchungen. Die Prüfung steht deshalb vorn (Reihenfolge siehe FA-c6), **vor** der Kollisionsprüfung und
    vor den Regeln 1–4 (`:66-101` bzw. `:197-217`), die schon schreiben.
  - AG nicht gefunden oder `ProductionWorkplaceId` NULL: ebenfalls abgewiesen (`BdeBookingResult.NotFound()` bzw. dieselbe
    Meldung). Heute liefe ein unbekannter AG bis zum Insert durch (Ist-Stand). NULL-AGs bietet das Terminal schon heute nicht an
    (`WorkOperationRepository.cs:59-82` vergleicht auf Gleichheit).
  - **Deckungsgleich mit dem Angebot:** Die Prüfung nutzt denselben Helfer wie FA-a2/FA-a3. Was das Terminal anbietet, wird
    angenommen; was es nicht anbietet, nicht.
  - Gilt standortneutral (AKE: Familie = `{W.Id}`, also AG muss an der Terminal-Werkbank hängen, wie das Angebot).
  - **Nicht** geprüft, mit Beleg: `StartActivityAsync` (`:107-140`, Tätigkeit ohne AG), `PauseAsync`, `FinishAsync`,
    `ReportPartialQuantityAsync`, `FinishGroupAsync`, `CloseOtherBookingsOnWorkOperationAsync` (`:142-327`, alle auf einer
    bestehenden Buchung, keine neue Buchung; laufende Zeit muss beendbar bleiben, Fallstrick §6 „BDE-Sperre“). Der Scan
    `/api/bde/resolve-scan` liest nur (kein `SaveChanges` in `BdeScanResolver`); gestartet wird danach über `StartSetup`/
    `StartProduction`, also geprüft. `BdeBookingsController.Edit` (`:203-246`, `[RequireBdeAdminAccess]`) ist die
    Admin-Korrektur einer bestehenden Buchung und ändert den AG nicht; bewusst ohne Prüfung. `BdeAutoPauseService` pausiert nur.
  - **Folge Fortsetzen:** Die Pausiert-Liste zeigt alle pausierten Buchungen des Werkers, werkbank-übergreifend
    (`BdeTerminalController.cs:233-254`). Fortsetzen an einer Werkbank außerhalb der Familie wird jetzt abgewiesen; die Meldung
    erscheint als Toast (`bde-terminal.js:640-641`). Der Werker setzt an einer Werkbank der Familie fort. Die Liste selbst bleibt
    unverändert (kein neues Filtern; Meldung statt stilles Ausblenden).
  - **NurFA-Pfad:** `StartProductionForOrder` (`BdeTerminalController.cs:62-68`) und das Fortsetzen eines NurFA-AG „01“ laufen
    ebenfalls durch `StartPlannedAsync`/`ResumeAsync`. Die Kürzel-Familie
    passt dort nicht (Default-AG „01“ hängt an der Werkbank des ersten Starters). **Antwort 12 (Gerald 2026-10-19):** Im NurFA-Modus prüfen Start und Fortsetzen statt der Kürzel-Familie, dass `ProductionOrder.ProductionWorkplaceId` des AG gleich der Terminal-Werkbank ist (deckungsgleich mit dem NurFA-Angebot); FA ohne Werkbank → abgewiesen. NurFA wird bei IDEAL nicht aktiviert; der Pfad wird per Unit-Test abgesichert.
    Die Prüfung läuft wie FA-a9 vor dem automatischen Schließen offener Buchungen, gleiche Meldung.

### (c) Werkbank-Option „BDE nur AG-Fertigmeldung“

- FA-c1 Neues Feld `ProductionWorkplace.BdeNurAgFertigmeldung` (bool, Default `false`), Label „Nur AG-Fertigmeldung (ohne
  Start/Stop)“, in der Karte „BDE-Einstellungen“ von Create/Edit (`Views/ProductionWorkplaces/Edit.cshtml:102-110`). Es wird
  per Formular gesetzt, kein Hidden-Feld mit gleichem Namen (Fallstrick §3).
- FA-c2 **Wirksam** = `BdeAktiv && BdeNurAgFertigmeldung && !BdeNurFaMeldung` (Antwort 6: global NurFA gewinnt). Eine
  einzige Funktion entscheidet das und wird überall benutzt (Terminal-Index, Start-Sperre, Fertigmeldung).
- FA-c3 Terminal an einer wirksamen Werkbank: dasselbe AG-Angebot wie FA-a2 (Kürzel-Familie gilt mit). Nach Klick oder Scan
  `FA,AG` gibt es statt „Rüsten starten“/„Produktion starten“ (`bde-terminal.js:308-311`) **einen** Knopf „Fertig melden“.
  Dann der vorhandene Mengen-Dialog (`promptQty`, `:374`) mit Hinweis Sollmenge; Gut ist mit der FA-Sollmenge vorbelegt
  (Antwort 9), Ausschuss 0. Nur bei der Fertigmeldung; normales Beenden bleibt unverändert leer.
- FA-c4 Neue Server-Aktion `POST /BdeTerminal/ReportCompletion` (operatorId, workOperationId, workplaceId, terminalId,
  goodQty, scrapQty), in einer Transaktion (`InTransactionAsync`, `BdeBookingService.cs:447-457`). Prüfungen der Reihe nach,
  jede mit eigener Meldung:
  1. Werkbank BDE-aktiv (`EnsureWorkplaceIsBdeActiveAsync`, `:410-416`).
  2. Option wirksam (FA-c2), sonst `Invalid("Diese Werkbank meldet nicht ohne Zeit fertig.")`.
  3. AG gehört zur Kürzel-Familie der Terminal-Werkbank (FA-a9), sonst `Invalid("Dieser Arbeitsgang gehört nicht zu dieser
     Werkbank.")` bzw. `NotFound()`.
  4. FA nicht verpackt/abgeholt (`EnsureOrderNotPackedAsync`, `:425-441`).
  5. Menge: mindestens Gut oder Ausschuss angegeben (Regel wie `FinishAsync` `:249-250`).
  6. AG nicht schon fertig gemeldet (End-Menge vorhanden, Prädikat wie `WorkOperationRepository.cs:159-163`), sonst
     `Invalid("Arbeitsgang ist bereits fertig gemeldet.")`. Das schützt gegen Doppelscan.
  7. Laufende Buchung **desselben** Werkers auf dem AG: `Invalid("Dieser Arbeitsgang läuft bei Ihnen — bitte dort
     beenden.")`.
  8. Laufende Buchung eines **anderen** Werkers auf dem AG: Regel wie beim Start (Antwort 7). Bei
     `BdeMehrfachBuchungProArbeitsgang` = aus `Collision` (Dialog vorhanden, `bde-terminal.js:360-363`). Bei an erlaubt, die
     Antwort trägt `otherActiveBookings` wie `BdeTerminalController.Finish` (`:169-191`), das JS öffnet den vorhandenen
     Andere-schließen-Dialog.
- FA-c5 **Speicherform:** eine `BdeBooking` mit `BookingType = Production`, `Status = Finished`, `StartedAt = EndedAt = now`
  (derselbe Zeitwert), `WorkOperationId`, `ProductionWorkplaceId` = Terminal-Werkbank (wo), `BdeOperatorId` (wer),
  `BdeTerminalId`. Dazu eine `BdeBookingQuantity` mit `IsFinal = true` (Menge). „Wann“ = Start = Ende. Audit über
  `SetAudit` (`:392-397`).
  **Warum diese Form:** Sie erfüllt alle drei Constraints ohne Änderung (`ApplicationDbContextModelSnapshot.cs:517-521`), sie
  braucht keinen neuen Enum-Wert, und jede bestehende „erledigt“-Abfrage greift unverändert (FA-Baum, Terminal-Ausblendung,
  NurFA-Ausschluss `BdeApiController.cs:226-229`). Ein eigener Buchungstyp hätte `CK_BdeBookings_TypeTarget` und jede
  `BookingType`-Weiche berührt (Antwort 10: `StartedAt == EndedAt` reicht, kein neuer Typ).
  **Bewusst nicht** gelten die Regeln 1–4 der Start-Logik (`:66-101`), etwa das Schließen eigener Rüst-/Tätigkeitsbuchungen.
  Die Fertigmeldung ist nie „aktiv“ (`EndedAt` gesetzt) und verletzt die Regel „eine aktive Buchung je Werker“ nicht.
- FA-c6 Start-Sperre (Antwort 8): `StartPlannedAsync` und `ResumeAsync` (nur geplante, `WorkOperationId` gesetzt) lehnen an
  einer wirksamen Werkbank mit `Invalid("An dieser Werkbank wird nur fertig gemeldet.")` ab, direkt nach dem BDE-Gate.
  Reihenfolge: BDE-Gate → (nur `ResumeAsync`: Parent laden und „pausiert“ prüfen, `:169-172`, weil erst der Parent den AG
  kennt) → Start-Sperre (FA-c6) → Familien-Prüfung (FA-a9) → bestehende Prüfungen.
  Tätigkeiten (`StartActivityAsync`) bleiben frei. Beenden, Pausieren und Mengen bleiben ohne Sperre (Prinzip Fallstrick §6
  „BDE-Sperre“: laufende Zeit muss beendbar sein).
- FA-c7 **Wirkung auf Auswertungen und Zeiten** (belegt):
  - Zeitaufteilung/effektive Dauer: Die Fertigmeldung zählt 0 und verzerrt parallele Buchungen nicht
    (`BdeTimeSplitService.cs:43`). Buchungsliste und Detail zeigen effektive Dauer 0 (`Controllers/BdeBookingsController.cs:130-151`, `:197`).
  - Tagesverlauf am Terminal: `durationMinutes = 0` (`BdeApiController.cs:165-167`).
  - Cockpit: nie sichtbar, weil `EndedAt` gesetzt ist (`BdeBookingRepository.cs:62`). Auto-Pause betrifft sie nicht
    (`IDEALAKEWMSService/Services/BdeAutoPauseService.cs:51`).
  - Erledigt-Status in FA-Baum und Terminal: sofort „erledigt“ (End-Menge).
  - **Folge:** Die IST-Zeit-Summe je AG ist an Fertigmeldungs-Werkbänken 0. Das ist richtig, solange es keine SOLL-Zeit
    gibt, und gehört auf die Hilfeseite.
- FA-c8 Abgrenzung `BdeNurFaMeldung`: global, FA-Ebene, **mit** Start/Stop über den Default-AG „01“
  (`BdeTerminalController.cs:62-68`, `BdeDefaultWorkOperationService.cs:27-48`). Die neue Option gilt je Werkbank, auf
  AG-Ebene, ohne Start/Stop. Ist NurFA an, ist die Option ohne Wirkung (FA-c2). Die Edit-Seite zeigt das als Hinweis, wenn
  NurFA global an ist.

### Wirkung je Stelle (Pflicht-Prüfung, je Standort)

| Stelle | IDEAL | AKE | Beleg |
|---|---|---|---|
| Terminal-Angebot / Scan Normal-Modus | Familie über Kürzel | Familie = `{W.Id}`, weil AKE-Werkbänke kein Kürzel tragen → **unverändert** | `ArbeitsschrittCode` schreibt nur die Werkbank-Anlage (`ProductionWorkplaceSyncService.cs:139`, `:185`), die nur bei Master an läuft (`SyncWorker.cs:377-378`). Datenstand je DB ist nicht belegbar → Prüfabfrage P5 |
| NurFA-Modus | Angebot unverändert; Buchungs-Prüfung `ProductionOrder.ProductionWorkplaceId == Terminal-Werkbank` (Antwort 12) | wie IDEAL | `BdeScanResolver.cs:127-157`, `BdeApiController.cs:216-253`, `BdeDefaultWorkOperationService.cs:27-39` |
| Buchungs-Prüfung Start/Fortsetzen/Fertigmeldung (FA-a9) | AG muss in der Kürzel-Familie liegen | Familie = `{W.Id}`: AG muss an der Terminal-Werkbank hängen, wie das heutige Angebot. **Geändert:** manipulierte POSTs und Fortsetzen einer an anderer Werkbank pausierten Buchung werden abgewiesen | `BdeBookingService.cs:36-105`, `:162-240`; Pausiert-Liste `BdeTerminalController.cs:233-254` |
| Cockpit | Buchung erscheint an der tatsächlichen Werkbank | unverändert | `BdeApiController.cs:181` |
| FA-Struktur-Symbole (`ArbeitsgangStatusRegel`) | Gate über die Familie (FA-a7), Regel selbst unverändert | FA-Baum nur bei Master/IDEAL; Gate-Menge ohne Kürzel = heutige Menge | `FaHierarchyController.cs:119-132`, `Services/ArbeitsgangStatusRegel.cs` |
| Abarbeitungsliste (`FaWorklist`) | **unberührt**: `FaWorkStep`, kein Bezug zu `WorkOperation` | unberührt | Fallstricke §2 „FA-Abarbeitungsliste“, §16 „Der BDE-Arbeitsgang ist `WorkOperation`“ |
| Rückmeldung / Tracking je Werkbank | unverändert (bevorzugte Werkbank) | unverändert | `TrackingController.cs:79-82`, `:222-224`; `GetOpenByWorkplaceIdAsync` bleibt mit gleichem Verhalten |
| OSEON-Arbeitsgänge | unberührt | unberührt: eigene Tabellen (`OseonWorkOperation`, `codebase/datenmodell.md:92`). `WorkOperation.ExternalSource` wird im WMS nie geschrieben (Grep: nur `TrackingController.cs:420` liest) | — |
| Werkbank-Anlage / Kennzeichnung | aktiv bei Master + Toggle | läuft nicht | `SyncWorker.cs:377-378` |
| Werkbank-Option (c) | Default aus | Default aus; AKE kann sie nutzen, wenn gewünscht | Migration Default 0 |

## Technischer Lösungsentwurf

Muster: Repository-Pattern (ADR 0001), Sync mit Aktivitäts-Protokoll (ADR 0010), `SageMissingSince`-Vorlage aus v1.61.0
(`Models/OperationConfig.cs:44-48`, `OperationConfigSyncService.cs:318-332`, `SQL/96`), Listen-View-Pattern (ADR 0005),
Werkbank-Datenhoheit Sage führend (ADR 0014). Ponytail: kein neuer Status-Halter, kein neuer Buchungstyp, kein neuer
Settings-Key, keine Änderung an `FaArbeitsgangStatus`/`ArbeitsgangStatusRegel`.

1. **Model + Migration:** `ProductionWorkplace` bekommt `DateTime? SageMissingSince` (Doku-Kommentar wie `OperationConfig.cs:44-47`)
   und `bool BdeNurAgFertigmeldung`. Eine Migration `AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung`.
2. **`WerkbankKuerzelFamilie`** (neu, `Services/`, statisch, rein):
   `FamilieIds(ProductionWorkplace w, IEnumerable<ProductionWorkplace> alle)` (FA-a1) und
   `BdeRelevanteIds(IEnumerable<ProductionWorkplace> alle)` (FA-a7). Trim + OrdinalIgnoreCase, eine Stelle.
3. **Repository:** `IWorkOperationRepository.GetOpenByWorkplaceIdsAsync(IReadOnlyCollection<int> ids, bool excludePackedOrders)`.
   Prädikat `wo.ProductionWorkplaceId != null && ids.Contains(wo.ProductionWorkplaceId.Value) && !wo.IsReported`, Rest wie
   `:67-81`. `GetOpenByWorkplaceIdAsync(id, x)` delegiert mit `new[] { id }`, damit Tracking bitgleich bleibt.
   `ProductionWorkplaceRepository.UpdateAsync` überschreiben: Nur Pflegefelder gelten als geändert (Muster
   `OperationConfigRepository.UpdateAsync`, Fallstrick §24). Das sind `Hall`, `OverridePrePickingDays`, `BdeAktiv`,
   `BdeDefaultArbeitsgang`, `BdeUseCustomShiftPlan`, `BdeNurAgFertigmeldung`, Audit-Modified, dazu `Name` und
   (FA-a8) `ArbeitsschrittCode` **nur** bei Werkbänken ohne `SageArbeitsplatznummer` (DB-Wert). `SageMissingSince` und
   `SageArbeitsplatznummer` schreibt das Web nie. Im Controller (`ProductionWorkplacesController.cs:195-211`) wird
   `vm.ArbeitsschrittCode` nur bei `existing.SageArbeitsplatznummer == null` übernommen, Create (`:108-120`) übernimmt es immer.
4. **Terminal:** `BdeApiController.GetAvailableOperations` lädt die Werkbänke einmal (`IProductionWorkplaceRepository.GetAllOrderedAsync`)
   und ruft `FamilieIds`, dann `GetOpenByWorkplaceIdsAsync`. `BdeScanResolver.BuildNormalCandidatesAsync` macht dasselbe
   über `_ctx.ProductionWorkplaces`. `BdeTerminalController.Index` setzt `ViewBag.NurAgFertigmeldung` (FA-c2) und das View
   einen Hidden-Wert wie `nurFaMode`. Das JS bekommt `renderStartButtons` → `['reportCompletion']` und `handleAction` →
   `promptQty` → `post('/BdeTerminal/ReportCompletion')`, plus Erfolgsmeldung in `getSuccessMessage`.
5. **Buchung:** `IBdeBookingService.ReportCompletionAsync` (FA-c4/c5); Start-Sperre in `StartPlannedAsync`/`ResumeAsync`
   (FA-c6). Die Entscheidung „wirksam“ als eine private Methode im Service, die der Controller mit nutzt (oder statisch an
   `ProductionWorkplace`). **Familien-Prüfung (FA-a9):** ein privater Helfer
   `EnsureWorkOperationInFamilyAsync(workOperationId, workplaceId)` neben `EnsureWorkplaceIsBdeActiveAsync` (`:410-416`):
   lädt den AG (`ProductionWorkplaceId`) und die Werkbänke (`_ctx.ProductionWorkplaces`, rund 90 Zeilen), ruft
   `WerkbankKuerzelFamilie.FamilieIds` und gibt `null` (weiter) oder `NotFound`/`Invalid` zurück. Aufruf in
   `StartPlannedAsync`, `ResumeAsync` (nur bei `parent.WorkOperationId`, nach dem Parent-Check `:169-172`) und
   `ReportCompletionAsync`. Im NurFA-Modus prüft dieselbe Methode stattdessen `ProductionOrder.ProductionWorkplaceId == workplaceId` (Antwort 12).
6. **Service:** `ProductionWorkplaceSyncService.RunAsync` bekommt nach dem Upsert-Loop (`:191`) einen Kennzeichnungsschritt
   (FA-b1–b8), Empty-Guard vor dem Loop. `WorkOperationStructureDetectionService` bekommt Katalog-Filter (FA-b9) und
   Mehrfach-Zweig (FA-a5), dazu den Klassenkommentar (`:24-29`) und `IWorkOperationStructureDetectionService`-Doku anpassen.
7. **FA-Baum:** `FaHierarchyController.cs:125` → `WerkbankKuerzelFamilie.BdeRelevanteIds(workplaces)`;
   `FaArbeitsschrittSymbole.NamenLookup` filtert `SageMissingSince == null`.
8. **Stammdaten-UI:** Spalte FA-b11 (`ColumnMap` `ProductionWorkplacesController.cs:44-57` mit Getter, der wie die Zelle
   rendert, Fallstrick §3 „Universal-Filter-Pattern“). `bde`-Getter und Badge um „nur Fertigmeldung“ ergänzen. Edit/Create:
   Checkbox (c), Kürzel-Feld für Nicht-Sage-Werkbänke (FA-a8), Hinweis bei NurFA global.
   `frontend-design`-Skill ist Pflicht (CLAUDE.md), Kontrast WCAG AA.

**Rollen / Filter:** keine neuen. Terminal-Aktion erbt `[RequireBdeActive]` + `[RequireBdeUserAccess]`
(`BdeTerminalController.cs:14-15`). Werkbank-Pflege `[RequireMasterDataAccess]` (Edit/Create, `ProductionWorkplacesController.cs:82`,
`:96`, `:146`, `:180`), Liste `[RequireMasterDataReadAccess]` (`:12`). Kein Eintrag in `RoleOverview` nötig.
**Settings:** kein neuer `ServiceSettingDefinitions`- oder AppSettings-Key. (b) läuft im bestehenden Doppel-Gate, (a) ist
datengetrieben (Kürzel), (c) ist ein Werkbank-Feld.

## Migrations-/SQL-Auswirkungen

**Migration ja.** Zwei Spalten sind nötig: Die Kennzeichnung muss den Lauf überdauern (Selbstheilung, Anzeige), und die
Werkbank-Option ist ein Stammdatum je Werkbank. Beides ist additiv und nicht daten-destruktiv. Ablauf nach ADR 0004:

1. Model (`ProductionWorkplace.cs`) → `dotnet ef migrations add AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung --project IdealAkeWms`.
   Erwartet: zwei `AddColumn` ohne Index. Offline prüfen: `has-pending-model-changes` leer, `migrations script` ohne Index-DDL
   (Fallstrick §9 „Guards in beiden Pfaden“; hier kein Index, also keine Raw-SQL-Guards in der Migration nötig).
2. `SQL/97_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung.sql` (nächste freie Nummer im Bündel, höchste ist
   `SQL/96`; zum Umsetzungszeitpunkt neu prüfen). Vorlage `SQL/96`:
   - Guard `IF OBJECT_ID(N'[dbo].[ProductionWorkplaces]', N'U') IS NULL` → Warnung + `SET NOEXEC ON`.
   - `IF COL_LENGTH(N'dbo.ProductionWorkplaces', N'SageMissingSince') IS NULL ALTER TABLE … ADD [SageMissingSince] DATETIME2 NULL;` (eigener Batch)
   - `IF COL_LENGTH(N'dbo.ProductionWorkplaces', N'BdeNurAgFertigmeldung') IS NULL ALTER TABLE … ADD [BdeNurAgFertigmeldung] BIT NOT NULL CONSTRAINT [DF_ProductionWorkplaces_BdeNurAgFertigmeldung] DEFAULT 0;` (eigener Batch)
   - `__EFMigrationsHistory`-Insert mit exakter MigrationId in **separatem** Batch, `SET NOEXEC OFF`.
   - Keine Bestandsbefüllung. Die erste Kennzeichnung macht der Dienst, nachvollziehbar im Protokoll.
3. `SQL/00_FreshInstall.sql` an **zwei** Stellen: Tabelle `ProductionWorkplaces` (`:235-252`) um beide Spalten und den
   MigrationId-Block nach `:2450-2452`.
4. `SQL/AgentJobs/`: nicht betroffen (nur `02_Import_Artikel.sql`, Artikel, deprecated).

## Audit-Feld-Auswirkungen

- `ProductionWorkplace` (erbt `AuditableEntity`, `ProductionWorkplace.cs:5`): Der Dienst setzt bei Kennzeichnen und
  Entkennzeichnen `Modified*` = `ProductionWorkplaceSync` (Muster `:186`). Web-Edit setzt sie unverändert über
  `ICurrentUserService` (`ProductionWorkplacesController.cs:207-209`), auch für die neue Checkbox.
- `WorkOperation` (erbt `AuditableEntity`): Die Struktur-Erkennung setzt beim Insert unverändert `Created*` =
  `WorkOperationStructureDetection` (`:178-180`), auch im Mehrfach-Zweig.
- `BdeBooking`/`BdeBookingQuantity`: `SetAudit` mit Terminal-Benutzer (`BdeBookingService.cs:388`, `:392-397`). Die
  Fertigmeldung ist ein Insert und schreibt kein `Modified*`.

## Akzeptanzkriterien

**(b) Kennzeichnung**
1. Bei IDEAL mit Quelle `vw_IDEAL_AKE_WMS_Arbeitsgaenge` hat Werkbank Id 19 (Sage-Nr. 2150) nach einem scharfen Lauf
   `SageMissingSince` gesetzt. Id 65 (140) und die KA-Werkbank 150 bleiben `NULL`.
2. Ein zweiter Lauf ändert den Zeitstempel von Id 19 nicht. `nicht_in_sage_neu = 0`, `nicht_in_sage` ≥ 1.
3. Taucht eine gekennzeichnete Nummer wieder in der Quelle auf, ist `SageMissingSince = NULL` und `wieder_in_sage` ≥ 1, mit
   Detailzeile.
4. Eine Quelle mit 0 Zeilen kennzeichnet und entkennzeichnet nichts und schreibt die Warnung „liefert keine Zeilen“
   (Unit-Test mit Fake-Reader).
5. Eine Quellzeile, deren Kürzel auf der Ausschlussliste steht oder leer ist, schützt ihre Werkbank vor der Kennzeichnung
   (Unit-Test).
6. Es wird keine Werkbank gelöscht. `BdeAktiv`, Name und Kürzel einer gekennzeichneten Werkbank bleiben unverändert. Ist sie
   BDE-aktiv, steht die Zusatzwarnung im Protokoll. Die Kennzeichnung verschickt keine Mail (nur Aktivitäts-Protokoll).
7. Werkbänke ohne `SageArbeitsplatznummer` werden nie gekennzeichnet (Unit-Test).
8. DryRun meldet die Counts und schreibt keine Spalte (Unit-Test).
9. Stammdaten › Werkbänke zeigt „Nicht in Sage seit“. Der Spaltenfilter `!(leer)` liefert genau die gekennzeichneten,
   serverseitig vor der Pagination. Die Edit-Seite einer gekennzeichneten Werkbank zeigt den Hinweis.

**(a) Mehrere Werkbänke**
10. Nach (b) legt die Struktur-Erkennung für Sub-FA 1065150 die AGs EG (Werkbank 65) und KA (Werkbank Sage-Nr. 150) an. Der
    Count `arbeitsschritt_mehrdeutig` existiert nicht mehr.
11. Tragen zwei aktuelle Werkbänke dasselbe Kürzel, wird der AG **einmal** angelegt, an der bevorzugten Werkbank
    (BDE-aktive vor inaktiver, dann kleinere Id). `arbeitsschritt_mehrere_werkbaenke` = Anzahl dieser Kürzel, mit Info-Zeile
    (Unit-Test).
12. Ein Kürzel nur an gekennzeichneten Werkbänken erscheint als „unbekannt“ (Count + Sammelmeldung) und erzeugt keinen AG
    (Unit-Test).
13. `available-operations/{65}` liefert offene EG-AGs, auch solche, deren `ProductionWorkplaceId` eine andere Werkbank mit
    Kürzel EG ist, einschließlich gekennzeichneter (Unit-Test Repository/Controller).
14. `resolve-scan?faNumber=1059463&opNumber=EG&workplaceId=65` liefert `Exact` mit dem EG-AG von Sub-FA 1065150, gleiche
    Menge wie FA-a2 (Unit-Test `BdeScanResolverTests`).
15. Eine Buchung über eine Familien-Werkbank speichert `BdeBooking.ProductionWorkplaceId` = Terminal-Werkbank, nicht die
    bevorzugte (Unit-Test).
16. Eine Werkbank ohne Kürzel sieht genau die AGs mit ihrer eigenen Id, bitgleich zu v1.62.0 (Unit-Test). Eine gekennzeichnete
    Werkbank sieht nur ihre eigenen.
17. Der FA-Baum zeigt für EG an Sub-FA 1065150 den Status (offen/läuft/erledigt), auch wenn der AG an einer BDE-inaktiven
    Werkbank der Familie hängt. Der Symbolname EG ist gesetzt.
18. Tracking „je Werkbank“ zeigt dieselben AGs wie v1.62.0 (`GetOpenByWorkplaceIdAsync`-Tests bleiben grün).
19. An einer Nicht-Sage-Werkbank lässt sich in Create und Edit ein Kürzel speichern (getrimmt, leer = NULL); danach gehört sie
    zur Familie dieses Kürzels (FA-a8).
20. **Serverseitiger Schutz (FA-a8):** An einer Sage-Werkbank ist das Kürzel-Feld read-only. Ein manipulierter Edit-POST mit
    geändertem `ArbeitsschrittCode` – auch mit geleertem `SageArbeitsplatznummer` im Formular – lässt den gespeicherten
    `ArbeitsschrittCode` und die `SageArbeitsplatznummer` unverändert (Unit-Test Controller + Repository, DB-Wert nachgelesen).

**(a) Familien-Prüfung beim Buchen (FA-a9)**
21. **Fremder AG abgewiesen:** Ein manipulierter `POST StartProduction` bzw. `StartSetup` mit einem AG, dessen
    `ProductionWorkplaceId` außerhalb der Kürzel-Familie der Terminal-Werkbank liegt, liefert `Invalid` „Dieser Arbeitsgang
    gehört nicht zu dieser Werkbank.“. Es entsteht keine Buchung, und eine laufende eigene Rüst- oder Tätigkeitsbuchung des
    Werkers bleibt offen (kein Auto-Close) (Unit-Tests `BdeBookingServiceTests`).
22. Dasselbe für `POST Resume` einer pausierten Buchung, deren AG außerhalb der Familie liegt: abgewiesen, keine neue Buchung,
    Parent bleibt `Paused`/`AutoPaused`. Fortsetzen einer pausierten **Tätigkeit** (ohne AG) bleibt ungeprüft möglich (Unit-Tests).
23. Dasselbe für `POST ReportCompletion` an einer wirksamen Fertigmeldungs-Werkbank: abgewiesen, keine Buchung, keine Menge
    (Unit-Test).
24. **Familien-AG angenommen:** Ein AG, dessen `ProductionWorkplaceId` eine andere Werkbank mit demselben Kürzel ist
    (auch gekennzeichnet oder BDE-inaktiv), wird bei Start, Resume und ReportCompletion angenommen. Eine Werkbank ohne Kürzel
    nimmt nur AGs mit ihrer eigenen Id an. Ein nicht existierender `workOperationId` wird abgewiesen statt bis zum Insert
    durchzulaufen (Unit-Tests).

**(c) Nur AG-Fertigmeldung**
25. Default der neuen Option ist aus. Bestehende Werkbänke haben nach Migration `BdeNurAgFertigmeldung = 0`.
26. An einer wirksamen Werkbank zeigt das Terminal nach Klick oder Scan nur „Fertig melden“, kein Rüsten oder Produktion. Der
    Mengen-Dialog ist mit Gut = FA-Sollmenge und Ausschuss = 0 vorbelegt; das normale Beenden bleibt leer.
27. „Fertig melden“ mit Menge erzeugt genau eine Buchung `Production/Finished` mit `StartedAt == EndedAt`, der Werkbank des
    Terminals, dem Werker, und eine `IsFinal`-Menge. Danach ist der AG an allen Werkbänken der Familie aus dem Angebot
    verschwunden.
28. Ein zweites „Fertig melden“ desselben AG wird mit „bereits fertig gemeldet“ abgelehnt (Unit-Test).
29. Ohne Menge: Ablehnung (Unit-Test). Bei FA verpackt/abgeholt: Ablehnung mit der bestehenden Meldung.
30. Läuft der AG bei einem anderen Werker und ist `BdeMehrfachBuchungProArbeitsgang` aus, kommt `Collision` und es wird
    nichts gespeichert. Ist der Schalter an, wird gespeichert und die Antwort enthält die anderen laufenden Buchungen
    (Unit-Tests).
31. `POST StartProduction`/`StartSetup` und `Resume` (geplante Buchung) an einer wirksamen Werkbank werden serverseitig
    abgelehnt. `StartActivity` geht (Unit-Tests).
32. Ist `BdeNurFaMeldung` an, ist die Option ohne Wirkung. Das Terminal zeigt den NurFA-Modus, und `ReportCompletion` lehnt
    ab (Unit-Test).
33. Die Fertigmeldungs-Buchung erscheint nicht im Cockpit, zählt im Tagesverlauf und in der Buchungsliste mit Dauer 0 und
    ändert die effektive Dauer paralleler Buchungen desselben Werkers nicht (Unit-Test `BdeTimeSplitService`).

**Querschnitt**
34. AKE (Master aus, keine Kürzel): Terminal, Scan, Cockpit, Tracking, FA-Liste verhalten sich wie v1.62.0 (TS-96.9). Einzige
    gewollte Änderung: FA-a9 (fremder AG bzw. Fortsetzen an anderer Werkbank abgewiesen, TS-96.10).
35. Build grün, alle Web- und Service-Tests grün. Angepasst bzw. neu: `Detect_AmbiguousCode_NotInsertedButReported`
    (`WorkOperationStructureDetectionServiceTests.cs:234-251`, wird zum Mehrfach-Fall), Moq-Setups
    `GetOpenByWorkplaceIdAsync(…, true)` in `BdeApiControllerTests.cs:316` ff. Die bestehenden `BdeBookingServiceTests`
    bleiben grün, weil der Seed den AG an die Terminal-Werkbank hängt (`IdealAkeWms.Tests/Helpers/BdeBookingTestSeed.cs:68`,
    `:96`). Neu: FA-a9-Tests (AK 21–24) und FA-a8-Tests (AK 19–20).
36. v1.63.0 in beiden `AppVersion.cs`. Anwender-Changelog, Hilfeseite (Kürzel-Familie, Kennzeichnung, Fertigmeldung mit
    Dauer 0, Fortsetzen nur an einer Werkbank der Familie), README (Werkbank-Option), TS-96.1–96.10 in
    `docs/TESTSZENARIEN.md` und `secondbrain/tests/testszenarien-index.md`.

## Test-Szenarien

Neues Kapitel **TS-96 — BDE Stufe 1: Arbeitsgang an mehreren Werkbänken, veraltete Sage-Werkbänke, nur AG-Fertigmeldung
(v1.63.0)** in `docs/TESTSZENARIEN.md` (höchstes vorhandenes TS-95 bei `:10118`; zum Umsetzungszeitpunkt nächste freie Nummer
nehmen). Unterszenarien TS-96.1–TS-96.10. Danach `secondbrain/tests/testszenarien-index.md`. Regression: TS-77
(Struktur-Erkennung v1.44.0), TS-90 (Terminal-Status), TS-94 (Arbeitsgang-Abgleich).

**Prüfabfragen (Mensch, vor dem Test; Ergebnisse in TS-96 eintragen, abhängige Aussagen bis dahin ungeprüft)**

```sql
-- P1 (WMS, je Standort): welche Werkbank-Quelle ist eingestellt?
SELECT [Key], [Value] FROM dbo.ServiceSettings
WHERE [Key] IN ('Sync:ProductionWorkplaceSyncViewName','Sync:ProductionWorkplaceSyncEnabled',
                'Sync:WorkOperationStructureDetectionEnabled','ProduktionsauftragHierarchisch');

-- P2 (WMS IDEAL): Sage-Werkbaenke mit Kuerzel (Vorher-Bild)
SELECT Id, Name, SageArbeitsplatznummer, ArbeitsschrittCode, BdeAktiv
FROM dbo.ProductionWorkplaces WHERE SageArbeitsplatznummer IS NOT NULL
ORDER BY ArbeitsschrittCode, SageArbeitsplatznummer;

-- P3 (Sage IDEAL): was liefert die Quelle (bei View-Quelle)?
SELECT Arbeitsgangnummer, Bezeichnung, AG FROM dbo.vw_IDEAL_AKE_WMS_Arbeitsgaenge ORDER BY AG;
-- Abgleich P2 gegen P3 = Liste der Werkbaenke, die gekennzeichnet werden. Erwartung laut Gerald: mind. 2150, 2300,
-- 2550, 2900, 2700, 9000, 3002, 8000; bei 87 WMS-Werkbaenken mit Kuerzel vs. 27 View-Zeilen vermutlich weit mehr.

-- P4 (WMS IDEAL): Testdaten FA 1059463
SELECT po.SubOrderNumber, wo.Id, wo.OperationNumber, wo.ProductionWorkplaceId, wp.Name, wp.SageArbeitsplatznummer, wo.IsReported
FROM dbo.WorkOperations wo JOIN dbo.ProductionOrders po ON po.Id = wo.ProductionOrderId
LEFT JOIN dbo.ProductionWorkplaces wp ON wp.Id = wo.ProductionWorkplaceId
WHERE po.OrderNumber = '1059463' ORDER BY po.SubOrderNumber, wo.Sequence;
SELECT SubFA, VaterFA, Arbeitsschritte FROM dbo.FaHierarchyNodes WHERE HauptFA = 1059463 ORDER BY SubFA;

-- P5 (WMS AKE): Kuerzel an AKE-Werkbaenken? Erwartung 0 -> Kuerzel-Familie bei AKE leer
SELECT COUNT(*) AS MitKuerzel FROM dbo.ProductionWorkplaces
WHERE NULLIF(LTRIM(RTRIM(ArbeitsschrittCode)), '') IS NOT NULL;

-- P6 (WMS IDEAL): offene AGs an kuenftig gekennzeichneten Werkbaenken (Altbestand, wird per Familie sichtbar)
SELECT wp.SageArbeitsplatznummer, wp.ArbeitsschrittCode, COUNT(*) AS OffeneAGs
FROM dbo.WorkOperations wo JOIN dbo.ProductionWorkplaces wp ON wp.Id = wo.ProductionWorkplaceId
WHERE wo.IsReported = 0 AND wp.SageArbeitsplatznummer IN ('2150','2300','2550','2900','2700','9000','3002','8000')
GROUP BY wp.SageArbeitsplatznummer, wp.ArbeitsschrittCode;
```

**TS-96.1 Kennzeichnung veralteter Sage-Werkbänke (IDEAL)**
- Vorbedingung: IDEAL-Test, Master an, `Sync:ProductionWorkplaceSyncEnabled` an, Quelle = View (P1), P2/P3 notiert.
- Schritte: 1. DryRun-Zyklus → Aktivitäts-Protokoll `ProductionWorkplaceSync`. 2. Scharfer Zyklus. 3. Stammdaten ›
  Werkbänke, Filter „Nicht in Sage seit“ = `!(leer)`. 4. Zweiter Zyklus.
- Erwartet: 1. Counts `nicht_in_sage_neu` = Anzahl aus P2-P3, keine DB-Änderung. 2. Id 19 (2150) und die KA-W1 (2300) haben
  ein Datum, Id 65 (140) und 150 nicht. Detailzeile listet „2150 Flächen entgr beendet (EG)“. 3. Liste zeigt genau die
  gekennzeichneten. 4. `nicht_in_sage_neu = 0`, Datum unverändert.
- Negativ: a) Quelle testweise auf eine leere View/Tabelle → Warnung „liefert keine Zeilen“, keine Kennzeichnung geändert.
  b) Quelle auf einen nicht existierenden Namen → bestehender Skip „Quelle nicht vorhanden“. c) Keine Werkbank gelöscht
  (Anzahl P2 vorher = nachher). d) Eine gekennzeichnete Werkbank testweise BDE-aktiv setzen → Zusatzwarnung im Protokoll.

**TS-96.2 Struktur-Erkennung legt EG/KA an (Bug-Fix)**
- Vorbedingung: TS-96.1 erledigt, `Sync:WorkOperationStructureDetectionEnabled` an, Sub-FA 1065150 offen.
- Schritte: Zyklus abwarten → P4 → Protokoll `WorkOperationStructureDetection`.
- Erwartet: Sub-FA 1065150 hat EG (`ProductionWorkplaceId` = 65) und KA (Werkbank Sage-Nr. 150) zusätzlich zu ST. Protokoll
  ohne „Mehrdeutige Arbeitsschritt-Kuerzel“. `arbeitsschritt_mehrere_werkbaenke` = 0, solange kein Kürzel doppelt aktuell ist.
- Negativ: MK, ZS, MO stehen weiter unter „unbekannt“. ST bleibt einmal vorhanden (kein Doppel-Insert).

**TS-96.3 Terminal Werkbank 65 findet EG**
- Vorbedingung: TS-96.2, Terminal-Benutzer mit Werkbank 65 („Entgraten Fläche“, BDE aktiv), Operator vorhanden.
- Schritte: 1. Terminal öffnen, Operator scannen. 2. Liste prüfen. 3. Scan `1059463,EG`. 4. „Produktion starten“, dann
  „Beenden (mit Mengen)“.
- Erwartet: 2. Block FA 1059463 mit Sub-FA 1065150 / EG. 3. direkt gewählt (eine Sub-FA) oder Auswahl bei mehreren Sub-FAs
  mit EG. 4. Buchungsliste: Werkbank = Entgraten Fläche. FA-Baum: EG erledigt.
- Negativ: Scan `1059463,ST` an 65 → „Dieser Auftrag hat an dieser Werkbank keinen offenen Arbeitsgang.“ (ST gehört zur
  Familie Stanzen).

**TS-96.4 Derselbe Arbeitsgang an zwei Werkbänken**
- Vorbedingung: FA-a8 umgesetzt. Manuelle Werkbank „Entgraten 2 (Test)“ (ohne Sage-Nr.), Kürzel EG im WMS gepflegt, BDE aktiv, zweiter
  Terminal-Benutzer. `BdeMehrfachBuchungProArbeitsgang` aus.
- Schritte: 1. Beide Terminals zeigen den EG-AG von Sub-FA 1065150. 2. Werker A startet Produktion an „Entgraten 2“.
  3. Werker B an 65 versucht denselben AG zu starten. 4. Werker A beendet mit Menge.
- Erwartet: 1. gleicher AG an beiden. 2. Buchung mit Werkbank „Entgraten 2“, Cockpit-Kachel „Entgraten 2“ aktiv. 3. 65 zeigt
  den AG als „läuft“, Start → Kollisionsdialog „in Arbeit durch A an Entgraten 2“. 4. AG an beiden Terminals verschwunden.
- Negativ: Werkbank ohne Kürzel (z. B. eine AKE-artige manuelle Werkbank) zeigt den EG-AG nicht. Wird „Entgraten 2“ auf
  BDE aus gestellt, erscheint sie nicht mehr in der Terminal-Auswahl, 65 sieht den AG weiter.

**TS-96.5 Altbestand an einer W1-Werkbank**
- Vorbedingung: P6 zeigt ≥ 1 offenen AG an 2150 (EG). Gibt es keinen, Szenario als „nicht anwendbar“ notieren.
- Schritte: Terminal 65 öffnen.
- Erwartet: Der AG an Werkbank 19 erscheint an 65 und ist buchbar. Die Buchung trägt Werkbank 65. FA-Baum zeigt dessen
  Status.

**TS-96.6 FA-Struktur-Symbole**
- Vorbedingung: `BdeAktiv` global an, Buchung aus TS-96.3 bzw. .4.
- Schritte: FA-Struktur, FA 1059463 aufklappen.
- Erwartet: EG-Symbol mit Name „Entgraten Fläche“ und Status gemäß Buchung (läuft/erledigt), unabhängig davon, an welcher
  Familien-Werkbank gebucht wurde. KA ohne Buchung: offen bzw. überfällig.

**TS-96.7 Werkbank-Option „nur AG-Fertigmeldung“**
- Vorbedingung: Werkbank 65 (oder Test-Werkbank) Edit → „Nur AG-Fertigmeldung“ an. `BdeNurFaMeldung` aus. Offener KA- oder
  EG-AG.
- Schritte: 1. Terminal öffnen, AG antippen. 2. „Fertig melden“, Vorbelegung prüfen (Gut = Sollmenge, Ausschuss 0), bestätigen. 3. Buchungsliste und
  Tagesverlauf. 4. Cockpit. 5. FA-Baum. 6. Denselben AG noch einmal per Scan melden.
- Erwartet: 1. nur Knopf „Fertig melden“, Sollmenge-Hinweis. 2. Erfolg, AG an allen Familien-Werkbänken weg. 3. eine Buchung
  Produktion, Start = Ende, Dauer 0, Werkbank 65, Operator, Menge. 4. keine Kachel-Aktivität. 5. AG erledigt. 6. Scan →
  `NotInScope`-Meldung (AG ist nicht mehr offen), ein direkter POST → „bereits fertig gemeldet“.
- Negativ: a) Ohne Menge bestätigen → abgelehnt. b) Werker B bucht den AG laufend an einer normalen Familien-Werkbank,
  Werker A meldet an der Fertigmeldungs-Werkbank → Kollisionsdialog (Schalter aus). Mit `BdeMehrfachBuchungProArbeitsgang`
  an → gespeichert + Andere-schließen-Dialog. c) `BdeNurFaMeldung` global an → Terminal im FA-Modus mit „Produktion
  starten“, Option wirkungslos, Edit zeigt Hinweis. d) Direkter POST `StartProduction` an der Werkbank → „An dieser Werkbank
  wird nur fertig gemeldet.“ e) Tätigkeit starten geht weiter. f) FA verpackt → bestehende Sperrmeldung.

**TS-96.8 Stammdaten-Liste und Rechte**
- Schritte: Als `masterdata_read`-Benutzer Liste öffnen. Als `masterdata` Edit öffnen und Option setzen, dann Spaltenfilter
  BDE = „Fertigmeldung“.
- Erwartet: Lesen ohne Bearbeiten-Knopf, Spalte „Nicht in Sage seit“ sichtbar. Speichern setzt `ModifiedBy`. Filter trifft.
  Pagination bleibt korrekt (Filter vor Skip/Take).
- Negativ: a) Eine Sage-Werkbank bearbeiten → Kürzel/Name read-only. b) **Serverseitig (FA-a8):** Edit-Formular einer
  Sage-Werkbank in den Browser-Entwicklerwerkzeugen ändern (`readonly` am Kürzel-Feld entfernen, Kürzel auf „XX“ setzen,
  zusätzlich das Feld Sage-Nummer leeren) und speichern. Danach per P2 bzw. Edit-Seite nachlesen: `ArbeitsschrittCode` und
  `SageArbeitsplatznummer` unverändert. c) Eine Nicht-Sage-Werkbank: Kürzel „ EG “ speichern → gespeichert als „EG“; Kürzel
  leeren → NULL.

**TS-96.9 AKE-Regression**
- Vorbedingung: AKE-Test, Master aus, P5 = 0, BDE an, eine AKE-Werkbank mit offenen AGs.
- Schritte: Terminal: Liste, Scan `FA,AG`, Start/Beenden. Cockpit. Tracking › je Werkbank. Stammdaten › Werkbänke.
- Erwartet: identisch zu v1.62.0. Liste, Scan und Buchung unverändert. Neue Spalte „Nicht in Sage seit“ leer. Option aus.
  Kein Eintrag `ProductionWorkplaceSync` im Protokoll.
- Negativ: AKE-Werkbank mit Option an → Fertigmeldung funktioniert auch bei AKE (gewollt, standortneutral). FA-a9 bei AKE
  siehe TS-96.10 (Familie = nur die Werkbank selbst).

**TS-96.10 Familien-Prüfung beim Buchen (manipulierter POST, FA-a9)**
- Vorbedingung: Zwei BDE-aktive Werkbänke: A mit Kürzel EG (z. B. 65), B mit anderem oder ohne
  Kürzel (z. B. Stanzen). Offener AG X an B, offener EG-AG Y an einer anderen EG-Werkbank (z. B. Altbestand 19 oder „Entgraten 2“
  aus TS-96.4). Terminal an A, Operator angemeldet. Ids von X und Y per P4 notiert. `BdeNurFaMeldung` aus.
- Schritte (POSTs über die Browser-Konsole am Terminal A, mit dem Antiforgery-Token der Seite):
  1. Werker startet eine Tätigkeit (läuft). Dann `POST /BdeTerminal/StartProduction` mit `workOperationId = X`,
     `workplaceId = A`. 2. Dasselbe mit `StartSetup`. 3. An Werkbank B einen AG von B starten und pausieren, Terminal auf A
     umschalten, in der Pausiert-Liste „Fortsetzen“. 4. Werkbank A auf „Nur AG-Fertigmeldung“, `POST
     /BdeTerminal/ReportCompletion` mit `workOperationId = X`, `goodQty = 1`. 5. `POST StartProduction` mit
     `workOperationId = Y` an A (Option wieder aus). 6. `POST StartProduction` mit einer nicht existierenden `workOperationId`.
- Erwartet: 1. und 2. Antwort `Invalid` „Dieser Arbeitsgang gehört nicht zu dieser Werkbank.“; keine neue Buchung
  (Buchungsliste), die Tätigkeit läuft weiter. 3. Toast mit derselben Meldung; Buchung bleibt pausiert; an B fortsetzen geht.
  4. Abgewiesen, keine Buchung, keine Menge. 5. Angenommen, Buchung mit Werkbank A. 6. Abgewiesen ohne Serverfehler.
- Negativ: a) AKE (Master aus, keine Kürzel): AG an einer anderen AKE-Werkbank per POST starten → abgewiesen; AG der eigenen
  Werkbank → angenommen. b) Pausierte Tätigkeit an B, an A fortsetzen → geht (keine AG-Prüfung). c) NurFA-Modus: nur als
  Unit-Test (NurFA wird bei IDEAL nicht aktiviert): FA der Terminal-Werkbank → angenommen, auch nach Umhängen des FA mit „01“
  an alter Werkbank; FA einer anderen Werkbank → abgewiesen; FA ohne Werkbank → abgewiesen.

## Etappen (nur bei epic: true)

Entfällt (`epic: false`). Empfohlene Commit-Reihenfolge im Worktree: (1) Migration + Model + SQL/97 + FreshInstall,
(2) Service (b) + Erkennung (a), (3) Web (a) Familie Terminal/Scan/FA-Baum + Familien-Prüfung FA-a9 im Buchungsservice,
(4) Web (c) Fertigmeldung + Stammdaten-UI inkl. Kürzel-Pflege FA-a8, (5) Doku/Version/TS.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

> [!success] Final (qa-agent 2026-10-19, gegen den echten Diff `d75680f4..b3daf566`)

- **Web-App:** ja — `IdealAkeWms/` geändert (Buchungsservice, Terminal-Controller/-View/-JS, Scan-Resolver, BdeApi, FA-Baum,
  Stammdaten Werkbänke, Repository, Hilfe/Changelog, AppVersion).
- **Service:** ja — `IDEALAKEWMSService/` geändert (`ProductionWorkplaceSyncService`, `WorkOperationStructureDetectionService`,
  AppVersion); der Dienst nutzt zudem den neuen Web-Helfer `WerkbankKuerzelFamilie` (Projektreferenz).
- **Migration:** ja — neu `IdealAkeWms/Migrations/20261019092105_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung.cs`,
  Skript `SQL/97_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung.sql` (additiv, 2× ADD COLUMN, nicht destruktiv,
  idempotent). `SQL/AgentJobs/` unberührt. Kein Einmal-Skript (`SQL/Einmalig/`).
- **Reihenfolge (Testsystem, danach Produktion je Standort):**
  1. DB-Backup (Routine; die Migration ist additiv).
  2. `SQL/97_…sql` ausführen (Ausgabe: zwei „Spalte … hinzugefügt“ + „Migration … eingetragen“).
  3. Dienst `IDEALAKEWMSService` stoppen.
  4. Web und Dienst publishen (Befehle unten, im Worktree), IIS-App neu starten.
  5. Dienst starten.
  6. **IDEAL vor dem Scharfschalten:** P1–P3 und P6 ausführen, dann einen DryRun-Zyklus der Werkbank-Anlage; `nicht_in_sage_neu`
     mit P2 minus P3 abgleichen. Quelle noch `KHKPpsArbeitsplaetze` (P1)? Dann werden nur dort nicht mehr aktive Arbeitsplätze
     gekennzeichnet.
- **Verhaltensänderung an allen Terminals (beide Standorte, FA-a9):** Fortsetzen einer an einer fremden Werkbank pausierten
  Buchung wird abgewiesen. Werker/Schichtleitung vorher informieren.
- **Risiko:** Viele gekennzeichnete Werkbänke → deren Kürzel ohne aktuelle Werkbank erscheinen als „unbekannt“ (Sammelmail). Gewollt,
  selbstheilend.
- **Publish-Befehle** (im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Nach dem Merge:** nur dann erneut aus `main` publishen, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen
  zusammengeführt hat.

## Offene Rueckfragen

**Rückfragen 1–11: keine mehr offen, beantwortet 2026-10-19** (Gerald, Block „Freigabe-Antworten“ und Freigabe-Nachtrag).
Die Fragen bleiben unten als Historie stehen; ihre „Empfehlung“-Formulierungen sind durch die Antworten ersetzt.

**Neu aus der Code-Prüfung im Rework 2026-10-19 – beantwortet 2026-10-19 (Antwort 12, Block „Freigabe-Antworten Rework“):**

12. **Familien-Prüfung im NurFA-Pfad.** Freigabe-Nachtrag Punkt 3 verlangt die Prüfung in `StartPlannedAsync` und
    `ResumeAsync`. Durch genau diese Methoden läuft auch der NurFA-Start (`BdeTerminalController.cs:62-68` →
    `FindOrCreateDefaultAsync` → `StartProductionAsync`). Der Default-AG „01“ wird je FA nur einmal angelegt und hängt an der
    Werkbank des **ersten** Starters (`BdeDefaultWorkOperationService.cs:27-39`); das NurFA-Angebot hängt dagegen an
    `ProductionOrder.ProductionWorkplaceId` (`BdeApiController.cs:220`), das umgehängt werden kann
    (`FaCompletionController.cs:430`, `IDEALAKEWMSService/Services/OseonSyncService.cs:544`). Wörtlich angewandt weist die
    Familien-Prüfung NurFA-Start und -Fortsetzen ab, sobald ein FA nach dem ersten Start an eine Werkbank außerhalb der Familie
    umgehängt wurde – im Widerspruch zum Out-of-Scope „NurFA unverändert“. Gleichzeitig schützt sie dort nichts: Ein
    manipulierter NurFA-POST mit fremdem FA legt den „01“-AG, falls noch keiner existiert, an der Terminal-Werkbank an
    (`:34-39`) und bestünde die Prüfung.
    **Empfehlung:** Im NurFA-Modus (`BdeNurFaMeldung` an) statt der Kürzel-Familie prüfen, dass
    `ProductionOrder.ProductionWorkplaceId` des AG gleich der Terminal-Werkbank ist – deckungsgleich mit dem NurFA-Angebot,
    wie FA-a9 im Normal-Modus. Alternative: NurFA-Pfad von der Prüfung ausnehmen (Lücke bleibt dort offen).

**Historie (beantwortet 2026-10-19):**

1. **Wie bekommt eine zweite Werkbank dasselbe Kürzel?** Heute schreibt nur die Werkbank-Anlage `ArbeitsschrittCode`
   (`ProductionWorkplaceSyncService.cs:139`, `:185`). Im WMS ist das Feld read-only bzw. bei manuellen Werkbänken gar nicht
   vorhanden (`Edit.cshtml:25-57`). Die View liefert je Kürzel genau eine Nummer. Ohne Antwort hat (a) bei IDEAL keine
   sichtbare Wirkung außer der Mehrdeutigkeits-Absicherung. **Empfehlung:** Kürzel an Werkbänken **ohne** Sage-Nummer im WMS
   pflegbar machen (FA-a8). Sage-Werkbänke bleiben Sage-geführt (ADR 0014). Alternative: weitere Arbeitsplätze mit gleichem
   AG in Sage/View pflegen, dann ohne WMS-Pflege.
2. **Ein Kürzel je Werkbank oder N:M?** **Empfehlung:** eins je Werkbank. Das Feld existiert, die Familie ist eine
   Gleichheitsprüfung, keine neue Tabelle, keine Migration dafür, und die Sage-Quelle kennt auch nur ein Kürzel je
   Arbeitsplatz (`USER_ArbeitsSchritt`). N:M (Muster `ProductionWorkplaceWorkStep`) erst, wenn eine konkrete Werkbank
   nachweislich zwei Arbeitsgänge stempeln soll.
3. **Variante A, B oder C** (Tabelle in „(a)“)? **Empfehlung: B**: keine Schemaänderung an `WorkOperation`, bestehende
   Lesestellen bleiben gültig, Altbestand ohne Umhängen sichtbar, AKE ohne Kürzel unverändert.
4. **Bevorzugte Werkbank** bei mehreren aktuellen Werkbänken je Kürzel: BDE-aktive zuerst, dann kleinste Id? **Empfehlung:**
   ja. Sie wirkt nur auf Tracking je Werkbank, die Sage-Nr. im AG-Modal und den Namen. Stempeln geht an allen.
5. **Gekennzeichnete, aber BDE-aktive Werkbank:** nur melden oder BDE automatisch aus? **Empfehlung:** nur melden. Ein
   Sync, der Stammdaten-Schalter umlegt, wäre still wirkender Datenverlust (ADR 0014, Z1). Laufende Buchungen müssen beendbar
   bleiben.
6. **Vorrang `BdeNurFaMeldung` (global) vor der Werkbank-Option?** **Empfehlung:** ja, NurFA gewinnt. Sonst entstünde ein
   vierter Terminal-Modus, „FA ohne Zeit“, der nicht verlangt ist.
7. **Fertigmeldung bei fremd laufender Buchung:** Regel wie beim Start (Kollision bei `BdeMehrfachBuchungProArbeitsgang` aus,
   sonst erlaubt + Andere-schließen-Dialog)? **Empfehlung:** ja. Eine Regel, vorhandene Dialoge.
8. **Start-Sperre an Fertigmeldungs-Werkbänken:** Rüsten/Produktion serverseitig sperren, Tätigkeiten weiter anbieten?
   **Empfehlung:** ja/ja. Ohne Sperre entstünden über alte Browser-Tabs Zeitbuchungen an einer „zeitlosen“ Werkbank.
   Tätigkeiten sind kein Arbeitsgang.
9. **Mengen-Vorbelegung:** Gut = FA-Sollmenge vorbelegen? **Empfehlung:** ja, nur bei der Fertigmeldung. Das kommt dem
   Lastenheft „nur einmal scannen“ am nächsten (ein Scan + Bestätigen). Normales Beenden bleibt unverändert leer.
10. **Kennzeichen „zeitlos“:** reicht `StartedAt == EndedAt`, oder ein eigener Buchungstyp? **Empfehlung:** reicht. Ein
    neuer Typ berührt `CK_BdeBookings_TypeTarget` und jede `BookingType`-Weiche. Unterscheidbar bleibt es über Start = Ende
    und die Werkbank-Option.
11. **Mail bei neu gekennzeichneten Werkbänken** oder nur Aktivitäts-Protokoll? **Empfehlung:** nur Protokoll, wie der
    Arbeitsgang-Abgleich v1.61.0. Die Folgemeldung „unbekannte Kürzel“ verschickt ohnehin schon eine Sammelmail.

> [!note] Zusatzfunde (nur melden, nicht mitbauen)
> - ~~Die Start-Logik prüft nicht, ob der AG zur Werkbank des Terminals gehört (`BdeBookingService.cs:36-104`). Ein manipulierter
>   POST bucht jeden AG an jeder BDE-aktiven Werkbank. Das war schon vor dieser Spec so; eine eigene Entscheidung nötig.~~
>   **Erledigt durch FA-a9** (Freigabe-Nachtrag Punkt 3, 2026-10-19); NurFA-Pfad über Antwort 12.
> - Rework 2026-10-19: Mit FA-a9 wird „Fortsetzen“ einer an einer fremden Werkbank pausierten Buchung abgewiesen, die
>   Pausiert-Liste zeigt sie aber weiter (`BdeTerminalController.cs:233-254`). Gewollt als Meldung statt stilles Ausblenden;
>   ein Filter der Liste nach Familie wäre eine eigene Entscheidung.
> - Stammdaten › Werkbänke: keine Filterkarte, keine Zahnrad-Registrierung (siehe Out-of-Scope).
> - Das Backlog [[2026-10-19-werkbank-katalog-veraltete-sage-arbeitsplaetze]] ist `typ: bug`. Ein Bug-Record unter
>   `secondbrain/bugs/` wurde in diesem Lauf auf Anweisung („nur die Spec-Datei“) **nicht** angelegt.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Zweite Werkbank mit gleichem Kürzel: Kürzel an Nicht-Sage-Werkbänken im WMS pflegbar? → **Beide Wege: Sage-Werkbänke bleiben Sage-geführt (weitere Arbeitsplätze mit gleichem USER_ArbeitsSchritt in Sage/View); an Werkbänken ohne Sage-Nummer ist das Kürzel im WMS pflegbar (FA-a8).** (Gerald 2026-10-19)
2. Ein Kürzel je Werkbank (statt N:M)? → **Ja, ein Kürzel je Werkbank; N:M erst bei belegtem Bedarf.** (Gerald 2026-10-19)
3. Variante A / B / C (zusätzlich `freigabe_entscheidung` setzen)? → **Variante B (bevorzugte Werkbank + Kürzel-Familie).** (Gerald 2026-10-19)
4. Bevorzugte Werkbank: BDE-aktive zuerst, dann kleinste Id? → **Ja.** (Gerald 2026-10-19)
5. Gekennzeichnete BDE-aktive Werkbank: nur melden? → **Ja, nur melden, BDE nicht automatisch aus.** (Gerald 2026-10-19)
6. `BdeNurFaMeldung` gewinnt über die Werkbank-Option? → **Ja.** (Gerald 2026-10-19)
7. Fertigmeldung bei fremd laufender Buchung: Regel wie beim Start? → **Ja, gleiche Regel wie beim Start.** (Gerald 2026-10-19)
8. Start-Sperre Rüsten/Produktion an Fertigmeldungs-Werkbank, Tätigkeiten frei? → **Ja.** (Gerald 2026-10-19)
9. Gut-Menge mit FA-Sollmenge vorbelegen? → **Ja.** (Gerald 2026-10-19)
10. `StartedAt == EndedAt` als Kennzeichen, kein neuer Buchungstyp? → **Ja.** (Gerald 2026-10-19)
11. Nur Aktivitäts-Protokoll, keine Mail bei neu gekennzeichneten Werkbänken? → **Ja, nur Aktivitäts-Protokoll.** (Gerald 2026-10-19)

## Freigabe-Antworten Rework (neue Rückfrage aus der Code-Prüfung 2026-10-19)

12. Familien-Prüfung im NurFA-Pfad: statt Kürzel-Familie `ProductionOrder.ProductionWorkplaceId == Terminal-Werkbank` prüfen (Empfehlung) oder NurFA-Pfad ausnehmen? → **Wie empfohlen: im NurFA-Modus `ProductionOrder.ProductionWorkplaceId == Terminal-Werkbank` prüfen. Hinweis: NurFA soll generell nicht verwendet werden und wird nicht aktiviert.** (Gerald 2026-10-19)

## Umsetzung (2026-10-19)

Direkt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Start `d75680f4` (v1.62.0, Arbeitsbaum sauber),
Ziel **v1.63.0**, **TS-96.1–96.10**, Migration `20261019092105_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung` /
`SQL/97`. Variante B, Antworten 1–12, Freigabe-Nachtrag. Plan: [[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-umsetzung]].

| # | Inhalt | Commit |
|---|---|---|
| 1 | Model (`SageMissingSince`, `BdeNurAgFertigmeldung`), EF-Migration (2× AddColumn, kein Index), `SQL/97` (COL_LENGTH-Guards, History separat), `SQL/00_FreshInstall.sql` Tabelle + MigrationId (mit COL_LENGTH-Bedingung) | `4416aac8` |
| 2–4 | `WerkbankKuerzelFamilie` (FamilieIds, BdeRelevanteIds, Bevorzugt, NurAgFertigmeldungWirksam); Werkbank-Anlage: Kennzeichnung aus rohen Quellzeilen, Empty-Guard, selbstheilend, Counts `nicht_in_sage`/`nicht_in_sage_neu`/`wieder_in_sage`, Warnung BDE-aktiv; Struktur-Erkennung: Katalog ohne gekennzeichnete, mehrere → bevorzugte Werkbank + Count `arbeitsschritt_mehrere_werkbaenke` + EINE Info-Zeile | `13681983` |
| 5 | `GetOpenByWorkplaceIdsAsync` (alte Methode delegiert, Tracking bitgleich), Terminal-Angebot + Resolve-Scan über Familie, FA-Baum-Gate `BdeRelevanteIds`, `NamenLookup` ohne gekennzeichnete | `27fdec11` |
| 6 | `BdeBookingService`: `EnsureStartAllowedAsync` (FA-c6) + `EnsureWorkOperationInFamilyAsync` (FA-a9, NurFA: `ProductionOrder.ProductionWorkplaceId`) vor jedem Schreiben in `StartPlannedAsync`/`ResumeAsync` (nur mit AG); `ReportCompletionAsync` (Reihenfolge FA-c4 1–8); `BdeTerminalController.ReportCompletion` + gemeinsamer Helfer `OtherActiveBookingsAsync` (auch von `Finish` genutzt); Terminal-View/JS „Fertig melden“ mit Sollmengen-Vorbelegung | `8b51af5a` |
| 7 | FA-a8: Controller übernimmt Kürzel nur bei DB-Wert `SageArbeitsplatznummer == null`, `ProductionWorkplaceRepository.UpdateAsync` nur Pflegefelder (Kürzel nur ohne gespeicherte Sage-Nr.); Create/Edit Kürzel-Feld + Option + NurFA-Hinweis (Partials), Index Spalte „Nicht in Sage seit“ (`data-date-filter`, Register `sage-missing` D) + Badge „nur Fertigmeldung“ | `3efba9a9` |
| 8 | Hilfe, Changelog, README (SQL/97, Werkbänke), AppVersion 1.63.0 ×2, TS-96 | `501008f6` |

**Nachweis (vor QA):**
- `dotnet build IdealAkeWms.slnx` → **0 Fehler**, 28 Warnungen (keine in geänderten Produktivdateien; eine in `BdeTerminalAuftraegeTests.cs:23`, Bestand).
- `dotnet test` → Web **2085 bestanden, 1 übersprungen**, 0 Fehler; Service **316 bestanden**, 0 Fehler.
- `dotnet ef migrations has-pending-model-changes` → „No changes have been made to the model since the last migration.“
- `node --check wwwroot/js/bde-terminal.js` → ok.
- Neue/angepasste Tests: `WerkbankKuerzelFamilieTests` (11), `ProductionWorkplaceSyncServiceTests` (+6, AK 1–8), `WorkOperationStructureDetectionServiceTests`
  (`Detect_AmbiguousCode_NotInsertedButReported` → `Detect_CodeOnTwoWorkplaces_InsertedOnceAtPreferred_Reported` + 3, AK 10–12),
  `BdeScanResolverTests` (+2, AK 14/16), `BdeApiControllerTests` (Moq-Setups auf `GetOpenByWorkplaceIdsAsync`, +2 Familie AK 13/16),
  `WorkOperationRepositoryExtendedTests` (+1, AK 13/18), `FaArbeitsschrittSymboleTests` (+1, AK 17), `BdeBookingServiceStufe1Tests` (18, AK 15/21–24/27–33, Antwort 12),
  `BdeTerminalControllerTests.ReportCompletion_MultiMa_*` (AK 30), `ProductionWorkplacesControllerTests` (+4, AK 9/19/20/25),
  `ProductionWorkplaceRepositoryTests` (+2, Fallstrick §24/FA-a8). Bestehende `BdeBookingServiceTests` unverändert grün.
- Kontrast (WCAG-Formel, Bootstrap 5.3): „Fertig melden“ Weiß auf `#198754` **4,53:1**; Badge „Nicht in Sage“ `#212529` auf `#ffc107` **9,46:1**;
  Badge „nur Fertigmeldung“ `#212529` auf `#f8f9fa` 14,6:1 (mit Rand). Bedeutung immer per Text, Farbe zusätzlich.

**Abweichungen / Entscheidungen im Lauf (mit Begründung):**
- Unbekannter AG im Buchungsweg → `Invalid` mit derselben Meldung statt `NotFound()` (Spec erlaubt „bzw. dieselbe Meldung“; `NotFound` trägt keinen Text, das Terminal zeigte nichts).
- `ReportCompletion` weist auch Gut = 0 **und** Ausschuss = 0 ab, nicht nur „beide leer“: das Terminal-JS sendet ein leeres Feld als 0.
- Außerhalb `affected_code`: zwei Partials `Views/ProductionWorkplaces/_ArbeitsschrittCodeField.cshtml` / `_BdeNurAgFertigmeldungField.cshtml`
  (Create und Edit teilen die Felder), `targetQuantity` im JSON von `available-operations` (Vorbelegung Sollmenge, Antwort 9),
  `IWorkOperationStructureDetectionService`-Doku, Beschreibung von `Sync:WorkOperationStructureDetectionEnabled` in `ServiceSettingDefinitions`
  (Text „mehrdeutige gemeldet“ stimmte nicht mehr; Länge ≤ 500 per Drift-Test).
- `Name` bleibt Pflegefeld in `UpdateAsync` (wie im Lösungsentwurf); an Sage-Werkbänken ist er im Formular read-only und wird vom Sync überschrieben.

**Befunde (nur gemeldet, nicht gebaut):**
- NurFA-Pfad: `StartProductionForOrder` ruft `FindOrCreateDefaultAsync` **vor** der Prüfung (Antwort 12). Ein manipulierter POST mit fremdem FA
  legt dort ggf. einen Default-AG „01“ an der Terminal-Werkbank an (ohne Buchung, abgewiesen). NurFA wird bei IDEAL nicht aktiviert; Fix wäre die
  Prüfung im Controller vor `FindOrCreateDefaultAsync`.
- `NamenLookup` (FA-Baum-Symbolname) liefert weiterhin keinen Namen, wenn ein Kürzel an **mehreren aktuellen** Werkbänken steht (z. B. EG an 65 und
  „Entgraten 2“ aus TS-96.4). Spec verlangt nur das Ausblenden gekennzeichneter (FA-b10); Name der bevorzugten Werkbank wäre eine eigene Entscheidung.
- README-Tabelle der SQL-Skripte führt `SQL/96` nicht (Altschuld v1.61.0); `SQL/97` ist eingetragen.

## QA (2026-10-19, qa-agent)

Unabhängiger Lauf im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Diff `d75680f4..501008f6` (Umsetzung) plus
QA-Commit `b3daf566`. Review selbst im Lauf durchgeführt, nicht delegiert.

> [!note] Ablage
> Die Spec liegt noch unter `specs/entwurf/`, obwohl Schranke 1 genommen ist. Nicht verschoben (Anweisung), nur gemeldet.

### 1. Build und Tests (eigene Ausführung)

| Lauf | Ergebnis |
|---|---|
| `dotnet build IdealAkeWms.slnx` @ `501008f6` | 0 Fehler, 25 Warnungen |
| `dotnet test` @ `501008f6` | Web **2085 bestanden, 1 übersprungen, 0 Fehler** (2086); Service **316 bestanden, 0 Fehler** |
| `dotnet build IdealAkeWms.slnx` @ `b3daf566` (nach QA-Fix) | 0 Fehler, 28 Warnungen; einzige `.cs`-Warnung `BdeTerminalAuftraegeTests.cs:23` CS8601 (Bestand), Rest außerhalb `.cs` |
| `dotnet test` @ `b3daf566` | Web **2088 bestanden, 1 übersprungen, 0 Fehler** (2089, +3 QA-Tests); Service **316 bestanden, 0 Fehler** |

Zwischenstand: Der erste Lauf des neuen QA-Tests `StartProductionForOrder_NurFa_OrderOfTerminalWorkplace_Started` scheiterte an der
Test-Vorbedingung (kein Default-Arbeitsgang konfiguriert, `BdeDefaultWorkOperationService.cs:67`); im Test `BdeDefaultArbeitsgang`
gesetzt, danach grün. Kein Produktivcode betroffen.

### 2. Code-Review — Befunde und Behandlung

| # | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| Q1 | mittel | `Controllers/BdeTerminalController.cs:65-70` (vor Fix) | `StartProductionForOrder` rief `FindOrCreateDefaultAsync` **vor** jeder Prüfung. Über den gemeldeten NurFA-Fall hinaus: Im **Normal-Modus** (IDEAL) legte ein manipulierter POST mit beliebiger `productionOrderId` einen AG „01“ an der **Terminal-Werkbank** an; dieser AG bestand danach die Familien-Prüfung (FA-a9), die Buchung lief durch. Damit war die mit FA-a9 geschlossene Lücke über diesen Endpunkt offen, und der fremde FA bekam einen zusätzlichen Arbeitsgang, der danach im Terminal-Angebot erscheint. Eine unbekannte `productionOrderId` führte zudem zum Serverfehler (FK). | **Behoben** `b3daf566`: Prüfung vor dem Anlegen, deckungsgleich mit dem Angebot (nur NurFA-Modus, nur `ProductionOrder.ProductionWorkplaceId == workplaceId`, Antwort 12), sonst `InvalidState` „Dieser Arbeitsgang gehört nicht zu dieser Werkbank.“ Tests `StartProductionForOrder_NotOffered_Rejected_NoDefaultAgCreated` (NurFA + fremder FA; Normal-Modus) und `StartProductionForOrder_NurFa_OrderOfTerminalWorkplace_Started`. TS-96.10 Negativ d ergänzt. Die Service-Prüfung in `EnsureWorkOperationInFamilyAsync` bleibt. |
| Q2 | niedrig | `IdealAkeWms/AppVersion.cs:6`, `IDEALAKEWMSService/AppVersion.cs:6` | `Date` stand noch auf `2026-10-16` (v1.62.0), Version schon 1.63.0; der Dienst loggt beides beim Start (`SyncWorker.cs:27`). | **Behoben** `b3daf566`: `2026-10-19`. |
| Q3 | niedrig | `docs/TESTSZENARIEN.md`, Dokument-Fußzeile | Stand-Zeile endete bei TS-95. | **Behoben** `b3daf566`. |
| Q4 | niedrig | `Services/BdeBookingService.cs` `ReportCompletionAsync` (`(goodQty ?? 0) == 0 && …`) | Negative Mengen werden serverseitig nicht abgewiesen; `min="0"` steht nur im Browser (`Views/BdeTerminal/Index.cshtml:116-117`). Gleiches Verhalten wie Bestand `FinishAsync`/`PauseAsync`/`ReportPartialQuantityAsync`; FA-c4 Punkt 5 verlangt nur „Regel wie FinishAsync“. | **Nicht behoben**, gemeldet: eine Server-Regel „Menge ≥ 0“ gehört dann einheitlich an alle Mengen-Einstiege (eigene Entscheidung, Kandidat für [[2026-10-14-buendel-fixlauf-vor-merge]]). |
| Q5 | niedrig | `Services/BdeBookingService.cs` `ReportCompletionAsync`, Prüfung „bereits fertig gemeldet“ | Prüfung ohne Sperre oder Unique-Index: zwei **gleichzeitige** Fertigmeldungen desselben AG an zwei Terminals können beide durchlaufen (Read Committed). Der Doppelscan am selben Terminal (nacheinander) ist abgedeckt (AK 28). | **Nicht behoben**, gemeldet (Wahrscheinlichkeit gering; Lösung wäre eine Schemaentscheidung, z. B. gefilterter Unique-Index). |
| Q6 | info | `IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs`, Info-Zeile Schritt 7 | Bei einer Werkbank ohne Sage-Nr. (FA-a8, z. B. „Entgraten 2“) beginnt der Eintrag mit einem Leerzeichen („( Entgraten 2*)“). Kosmetisch. | Nicht behoben. |
| Q7 | info | `ProductionWorkplacesController` Edit-POST / `ProductionWorkplaceRepository.UpdateAsync` vs. `ProductionWorkplaceEditViewModel.IstSageGefuehrt` | View entscheidet „Sage-geführt“ über `IsNullOrWhiteSpace`, Controller/Repository über `== null`. Eine Sage-Nr. `""` zeigte das Kürzel-Feld editierbar und verwürfe die Eingabe still. Nicht erreichbar: der einzige Schreibweg (`ProductionWorkplaceSyncService.cs`, Upsert-Schleife) schreibt nur getrimmte, nicht leere Nummern; das Web schreibt die Sage-Nr. nie. | Nicht behoben (unerreichbar). |
| Q8 | info | Testabdeckung AK 5 | Getestet ist nur die Quellzeile **ohne Kürzel**, nicht die mit Kürzel auf der Ausschlussliste. Derselbe Codepfad (Präsenz aus allen `sageRows` vor dem Pflicht-/Ausschluss-Filter), daher am Code belegt. | Akzeptiert. |

Bestätigte Befunde der Umsetzung (unverändert offen, nur gemeldet): `NamenLookup` ohne Namen bei einem Kürzel an mehreren **aktuellen**
Werkbänken; README-SQL-Tabelle ohne `SQL/96`. Der Umsetzungs-Befund „NurFA legt Default-AG vor der Prüfung an“ ist durch Q1 erledigt.

**Geprüft ohne Befund** (mit Fundstelle):
- **FA-a9 vor jedem Schreiben:** `StartPlannedAsync`: BDE-Gate → `EnsureStartAllowedAsync` → `EnsureWorkOperationInFamilyAsync` →
  verpackt → Kollision → Regeln 1–4 (erstes `FinishAndSaveAsync` erst danach), `BdeBookingService.cs:41-55`. `ResumeAsync`: Gate →
  Parent laden/pausiert → (nur mit `WorkOperationId`) Sperre → Familie → verpackt, alles vor `activeOwn`/`FinishAndSaveAsync` und vor
  `parent.Status = Resumed`. `ReportCompletionAsync`: Reihenfolge FA-c4 1–8 exakt, Insert erst danach, in `InTransactionAsync`.
  Unbekannter AG → `Invalid` mit Meldung (Abweichung begründet).
- **NurFA-Zweig (Antwort 12):** `EnsureWorkOperationInFamilyAsync` prüft bei `BdeNurFaMeldung` `ProductionOrder.ProductionWorkplaceId ==
  workplaceId`; FA ohne Werkbank → abgewiesen. Test `NurFa_ChecksOrderWorkplace_NotAgWorkplace`.
- **FA-a8 serverseitig am DB-Wert:** Controller-Bedingung `existing.SageArbeitsplatznummer == null` auf der geladenen Entität, nie
  `vm.SageArbeitsplatznummer`; `UpdateAsync` setzt `IsModified` nur für Pflegefelder, Kürzel nur bei `OriginalValue` der Sage-Nr. `null`;
  `SageMissingSince`/`SageArbeitsplatznummer` nie. Einziger Aufrufer von `ProductionWorkplaceRepository.UpdateAsync` ist dieser Edit-POST
  (Grep über alle Controller mit `IProductionWorkplaceRepository`), kein Caching-Decorator (`Program.cs:66`). Fallstrick §24 erfüllt.
- **Empty-Guard + Präsenz aus rohen Zeilen:** `sageRows.Count == 0` → Warnung, keine (Ent-)Kennzeichnung; Präsenz-Menge aus allen
  `sageRows` vor dem Filter, ordinal, getrimmt; nur Werkbänke mit Sage-Nr.; DryRun schreibt nichts; Audit `ProductionWorkplaceSync`;
  keine Aufnahme in `problems` → keine Mail. Counts deutsch (`nicht_in_sage`, `nicht_in_sage_neu`, `wieder_in_sage`,
  `arbeitsschritt_mehrere_werkbaenke`), `arbeitsschritt_mehrdeutig` entfernt.
- **AKE-Unverändertheit:** `FamilieIds` liefert ohne Kürzel `{w.Id}`; `GetOpenByWorkplaceIdAsync` delegiert mit `new[] { id }`
  (Tracking bitgleich); `BdeRelevanteIds` ohne Kürzel = Menge der BDE-aktiven (wie vorher); Scan-Resolver fällt bei unbekannter
  Werkbank auf `{workplaceId}` zurück.
- **Pausiert-Liste bewusst nicht gefiltert:** `BdeTerminalController.PausedBookings` im Diff unverändert; die Abweisung erscheint als
  Toast über `post()` → `InvalidState` (`bde-terminal.js`).
- **Eine Regel an einer Stelle:** Familie/Bevorzugt/Wirksam nur in `WerkbankKuerzelFamilie` (Terminal, Scan, Buchung, FA-Baum,
  Struktur-Erkennung); kein SQL-Vergleich der Kürzel (Fallstrick §26 Kollation). `NamenLookup` behält seine bestehende Gruppierung
  (gleiches Trim/IgnoreCase).
- **Spaltenfilter-Register:** `<th data-date-filter data-col-key="sage-missing">` und Register `("sage-missing", D)`
  (`ColumnFilterSyntax.cs:63`) stimmen überein; `ColumnMap`-Getter rendert wie die Zelle (`dd.MM.yyyy`), `bde`-Getter mit
  „nur Fertigmeldung“ wie das Badge; Filter vor `Skip/Take`.
- **Read/Edit-Split, TempData:** keine neuen Rollen/Filter; `ReportCompletion` erbt `[RequireBdeActive]` + `[RequireBdeUserAccess]`
  und hat `[ValidateAntiForgeryToken]`; nur `TempData["SuccessMessage"]`.
- **Defektklassen:** keine Werte in `<script>` (Hidden-Inputs, `nurAgFertigmeldung` als festes `"true"/"false"`); kein `hidden` mit
  `d-*`; Leerfeld → 0 im Mengen-Dialog ist Bestand, der Server weist 0/0 ab (Abweichung begründet); keine parallelen Arrays; Kürzel-
  und Start-Regeln serverseitig. Der Werkbank-Wechsel am Terminal lädt die Seite neu (`bde-terminal.js:582-583`), der Modus-Wert
  gehört daher immer zur aktuellen Werkbank.
- **Kontrast:** Badge `bg-warning text-dark`, Badge `bg-light text-dark border`, Knopf `btn-success` (Weiß auf #198754, 4,53:1),
  jeweils mit Text, Farbe nie allein. Werte der Umsetzung nachvollzogen; Sichtprüfung im Browser ist Manual-UAT.
- **Migration/SQL:** Migration 2× `AddColumn`, kein Index; `SQL/97` mit `OBJECT_ID`-Guard + `NOEXEC`, `COL_LENGTH`-Guards je DDL in
  eigenem Batch, History-Insert separat, exakte MigrationId `20261019092105_…`, ProductVersion 10.0.2; `SQL/00_FreshInstall.sql` an
  beiden Stellen (Tabelle und MigrationId-Block mit COL_LENGTH-Bedingung); `SQL/AgentJobs/` nicht betroffen.

### 3. Beweisart je Akzeptanzkriterium

**Unit** = automatisierter Test beweist es; **UAT** = Manual-UAT, zählt nicht als grün (Sage-/View-Read, SQL Server, Browser-JS,
echte Model-Binding, Echtdaten).

| AK | Beweis | Test / Szenario |
|---|---|---|
| 1 | Logik Unit, Ergebnis **UAT** (Sage-View-Read, Echtdaten Id 19/65/150) | `Run_NichtMehrInQuelle_…`; TS-96.1 |
| 2 | Unit + UAT | `Run_ZweiterLauf_ZeitstempelBleibt_NeuNull`; TS-96.1 Schritt 4 |
| 3 | Unit | `Run_WiederInQuelle_KennzeichnungAufgehoben` |
| 4 | Unit | `Run_LeereQuelle_KeineKennzeichnung_Warnung`; TS-96.1 Negativ a |
| 5 | Unit (Variante ohne Kürzel), Ausschluss-Variante per Code-Review (Q8) | `Run_QuellzeileOhneKuerzel_SchuetztIhreWerkbank` |
| 6 | Unit; „keine Mail“ Code-Review + UAT | `Run_NichtMehrInQuelle_…`; TS-96.1 Negativ c–e |
| 7 | Unit | `Run_NichtMehrInQuelle_…` (AKE-Werkbank) |
| 8 | Unit | `Run_DryRun_ZaehltKennzeichnungOhneSchreiben` |
| 9 | Filter Unit; Edit-Hinweis und Darstellung **UAT** | `Index_SageMissingFilter_NotEmpty_…_AK9`; TS-96.1/96.8 |
| 10 | Logik Unit, Sub-FA 1065150 **UAT** | `Detect_MarkedWorkplaceIgnored_SingleCurrentMatches`; TS-96.2 |
| 11 | Unit | `Detect_CodeOnTwoWorkplaces_InsertedOnceAtPreferred_Reported`, `Detect_CodeOnTwoInactiveWorkplaces_SmallestIdPreferred` |
| 12 | Unit | `Detect_CodeOnlyAtMarkedWorkplace_ReportedUnknown_NoInsert` |
| 13 | Unit (InMemory); SQL-Übersetzung von `Contains` **UAT** | `GetAvailableOperations_QueriesKuerzelFamilie`, `GetOpenByWorkplaceIdsAsync_…`, `Normal_Familie_…`; TS-96.3/96.5 |
| 14 | Unit (InMemory) + UAT Echtdaten | `Normal_Familie_FindsAgAtOtherWorkplaceWithSameCode_IncludingMarked`; TS-96.3 |
| 15 | Unit | `Familie_AgOfSameCodeWorkplace_Accepted_BookingStoresTerminalWorkplace_AK15_AK24` |
| 16 | Unit | `Normal_MarkedOrCodelessWorkplace_SeesOnlyOwn`, `GetAvailableOperations_QueriesKuerzelFamilie(null)`, `FamilieIds_NoCode_OnlySelf_Ake` |
| 17 | Helfer Unit; Controller-Gate und Symbol **UAT** | `BdeRelevanteIds_…`, `NamenLookup_GekennzeichneteWerkbankZaehltNicht`; TS-96.6 |
| 18 | Unit (Bestandstests `GetOpenByWorkplaceIdAsync` unverändert grün) | `WorkOperationRepositoryExtendedTests` |
| 19 | Unit (Controller direkt, InMemory); echte Model-Binding **UAT** | `Edit_NonSageWorkplace_…_AK19`, `Create_SavesCodeAndOption_…`; TS-96.8 c |
| 20 | Unit (Controller + Repository, InMemory); `IsModified` gegen SQL Server und manipuliertes Formular **UAT** | `Edit_SageWorkplace_ManipulatedPost_…_AK20`, `UpdateAsync_WritesOnlyEditableFields_…`; TS-96.8 b |
| 21 | Unit | `Start_ForeignAg_Rejected_NoBooking_RunningActivityStaysOpen_AK21`; TS-96.10 |
| 22 | Unit | `Resume_ForeignAg_Rejected_ParentStaysPaused_ActivityResumable_AK22` |
| 23 | Unit | `ReportCompletion_ForeignAg_Rejected_NoBookingNoQuantity_AK23` |
| 24 | Unit | `Familie_…_AK15_AK24`, `UnknownWorkOperation_Rejected_AK24`, `OwnWorkplaceWithoutCode_StillAccepted_Ake`; Q1-Tests |
| 25 | Unit (Create-Default); DB-Default nach SQL/97 **UAT** | `Create_SavesCodeAndOption_DefaultOff_AK19_AK25`; Checkliste Punkt 1 |
| 26 | **UAT** (Browser-JS) | TS-96.7 Schritte 1–2 |
| 27 | Unit (Speicherform); „an allen Familien-Werkbänken weg“ UAT | `ReportCompletion_StoresFinishedProductionWithZeroDuration_…_AK27`; TS-96.7 |
| 28 | Unit | `ReportCompletion_SecondTime_AlreadyReported_AK28` |
| 29 | Unit | `ReportCompletion_WithoutQuantity_Rejected_AK29`, `ReportCompletion_PackedOrder_Rejected_AK29` |
| 30 | Unit | `ReportCompletion_OtherOperatorRunning_FollowsStartRule_AK30`, `ReportCompletion_MultiMa_SavedAndIncludesOtherActiveBookings` |
| 31 | Unit | `StartAndResume_BlockedAtFertigmeldungWorkplace_ActivityAllowed_AK31` |
| 32 | Service Unit; Terminal-Modus **UAT** | `NurFaGlobal_OptionWithoutEffect_…_AK32`; TS-96.7 Negativ c |
| 33 | Zeitaufteilung Unit; Cockpit/Tagesverlauf/Buchungsliste **UAT** | `ReportCompletion_ZeroDuration_DoesNotChangeParallelEffectiveDuration_AK33`; TS-96.7 |
| 34 | **UAT** | TS-96.9, TS-96.10 Negativ a |
| 35 | Build-/Testausgabe oben | — |
| 36 | Code-Review (beide `AppVersion.cs` 1.63.0, Changelog, Hilfe, README, TS-96, Index) | — |

### 4. Geänderte Bestandstests

| Test | Änderung | Einstufung |
|---|---|---|
| `WorkOperationStructureDetectionServiceTests.Detect_AmbiguousCode_NotInsertedButReported` → `Detect_CodeOnTwoWorkplaces_InsertedOnceAtPreferred_Reported` | Erwartung „kein Insert, Warnung mehrdeutig“ → „ein Insert an der bevorzugten Werkbank, Info-Zeile, Count `arbeitsschritt_mehrere_werkbaenke`“; Fixture: zweite Werkbank `"ka "` + `BdeAktiv` | **Verhalten durch die Spec gewollt geändert** (FA-a5, AK 10/11). Die Fixture-Änderung prüft Trim/IgnoreCase und „BDE-aktive zuerst“, sie ist nicht bis grün angepasst. |
| `BdeApiControllerTests` (6 Moq-Setups) | `GetOpenByWorkplaceIdAsync(id, true)` → `GetOpenByWorkplaceIdsAsync(c => c.Contains(id), true)` | **Typ-/API-Änderung** (Controller ruft die neue Methode), Erwartungen unverändert |
| `BdeTerminalControllerTests.CreateTerminalController` (QA) | optionaler Parameter `nurFa` (Default wie bisher `"false"`) | **Typ-only** |

Alle anderen Bestandsdateien nur ergänzt (keine Löschzeilen laut `git diff --numstat`); `BdeBookingServiceTests` unverändert grün.

### 5. CLAUDE.md-Checkliste

- [x] Migration + `SQL/97` mit Guards; FreshInstall an beiden Stellen; `SQL/AgentJobs/` nicht betroffen; kein Einmal-Skript.
- [x] Audit: Dienst `ProductionWorkplaceSync` bei (Ent-)Kennzeichnung; Web-Edit über `ICurrentUserService`; Fertigmeldung `SetAudit`.
- [x] Version 1.63.0 in beiden `AppVersion.cs` (Datum durch QA korrigiert), Anwender-Changelog, Brain-Changelog
      [[2026-10-19-v1-63-0-bde-stufe-1-werkbaenke-ag-fertigmeldung]], feature-map-Eintrag vorhanden.
- [x] README (SQL/97, Werkbänke), Hilfeseite (Familie, Kennzeichnung, Fertigmeldung Dauer 0, Fortsetzen nur in der Familie).
- [x] `docs/TESTSZENARIEN.md` TS-96.1–96.10 (Worktree) und `secondbrain/tests/testszenarien-index.md` Zeile 96 (Hauptcheckout).
- [x] Aktivitäts-Protokoll: deutsche Counts-Keys; keine neuen Service-Keys (`ServiceSettingDefinitions` nur Beschreibungstext, Drift-Test grün).
- [x] Keine neue Rolle, kein neuer Filter.

**Status-Entscheidung:** Build und alle Tests grün, Review-Befunde Q1–Q3 behoben (`b3daf566`), Q4–Q8 gemeldet und eingeordnet
→ **Testbereit**. Schranke 2 (manueller Test, Merge) liegt beim Menschen.

## Manuelle Test-Checkliste (Schranke 2)

Vor dem Test: Deploy-Reihenfolge oben (Backup → SQL/97 → Dienst stoppen → Publish Web + Dienst → Dienst starten); Prüfabfragen
P1–P6 ausführen und in TS-96 notieren.

1. **SQL/97** auf dem Testsystem ausführen und ein zweites Mal laufen lassen → keine Änderung; `__EFMigrationsHistory` enthält
   `20261019092105_…` genau einmal; alle Werkbänke `BdeNurAgFertigmeldung = 0` (AK 25).
2. **TS-96.1** (IDEAL): DryRun → Counts gegen P2 minus P3; scharfer Lauf → Id 19/2300 gekennzeichnet, 65/150 nicht; zweiter Lauf
   `nicht_in_sage_neu = 0`; Negativ a–e (leere Quelle, fehlende Quelle, keine Löschung, BDE-aktiv-Warnung, keine Mail).
3. **TS-96.2:** Struktur-Erkennung legt für Sub-FA 1065150 EG (65) und KA (150) an; kein `arbeitsschritt_mehrdeutig`.
4. **TS-96.3:** Terminal 65 zeigt EG, Scan `1059463,EG` wählt ihn, Buchung trägt Werkbank 65; Scan `1059463,ST` → Meldung.
5. **TS-96.4:** manuelle Werkbank „Entgraten 2 (Test)“ mit Kürzel EG → beide Terminals zeigen den AG; Kollisionsdialog; beim nächsten
   Lauf der Struktur-Erkennung Info-Zeile „Kürzel an mehreren Werkbänken“ im Protokoll.
6. **TS-96.5:** Altbestand an Werkbank 19 erscheint an 65 und ist buchbar (oder laut P6 „nicht anwendbar“).
7. **TS-96.6:** FA-Struktur, FA 1059463: EG mit Name „Entgraten Fläche“ und Status gemäß Buchung.
8. **TS-96.7:** Option „Nur AG-Fertigmeldung“ → nur grüner Knopf „Fertig melden“, Vorbelegung Gut = Sollmenge / Ausschuss 0, Buchung
   Start = Ende, Cockpit leer, FA-Baum erledigt; Negativ a–g (insbesondere d: direkter `StartProduction`-POST abgewiesen).
9. **TS-96.8:** Liste mit `masterdata_read`; Filter „Nicht in Sage seit“ `!(leer)` und BDE `Fertigmeldung`; **manipuliertes
   Edit-Formular** einer Sage-Werkbank (readonly entfernen, Kürzel „XX“, Sage-Nr. leeren) → per P2 unverändert; Nicht-Sage-Werkbank
   „ EG “ → „EG“, leer → NULL; 21 Zeichen → Validierung.
10. **TS-96.9 (AKE):** Terminal, Scan, Cockpit, Tracking, Stammdaten wie v1.62.0; kein Protokolleintrag `ProductionWorkplaceSync`.
11. **TS-96.10:** manipulierte POSTs aus der Browser-Konsole (StartProduction/StartSetup mit fremdem AG, Fortsetzen an fremder
    Werkbank, ReportCompletion mit fremdem AG, Familien-AG angenommen, unbekannte Id ohne Serverfehler) und **Negativ d (QA-Fix):**
    `POST /BdeTerminal/StartProductionForOrder` im Normal-Modus mit beliebiger `productionOrderId` → `InvalidState`, P4 zeigt
    keinen neuen AG „01“.
12. **Kontrast und Bedienung im Browser** am Terminal-Gerät: grüner Knopf „Fertig melden“, Badges „Nicht in Sage“ und „nur
    Fertigmeldung“ lesbar (WCAG AA), Bedeutung auch ohne Farbe erkennbar.
13. **Regression:** TS-77 (Struktur-Erkennung), TS-90 (Terminal-Status), TS-94 (Arbeitsgang-Abgleich) stichprobenartig.
14. **Werker/Schichtleitung informieren** (Fortsetzen nur in der Familie), danach Merge-Freigabe.
