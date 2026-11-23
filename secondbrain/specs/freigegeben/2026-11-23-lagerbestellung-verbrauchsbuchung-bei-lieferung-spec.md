---
type: spec
title: "Lagerbestellungen: Entnahme-Lagerplatz wählen (Vorschlag wie Kommissionierung) und Verbrauchsbuchung bei Lieferung, mit Sage-Übergabe"
slug: 2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung-spec
status: Testbereit
created: 2026-11-23
updated: 2026-11-23
source_backlog: "[[2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung]]"
depends_on: ""
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
testszenario_vorschlag: "TS-178 (TS-176 Favoriten, TS-177 Wareneingang zu Bestellung reserviert; Dev-Lauf prüft erneut)"
version_vorschlag: "1.99.53 (1.99.51 Favoriten, 1.99.52 Wareneingang zu Bestellung; Dev-Lauf prüft erneut)"
sql_vorschlag: "166 (164 BDE-Liste vergeben, 165 reserviert; Dev-Lauf prüft erneut)"
affected_code:
  - IdealAkeWms/Models/StockMovement.cs (neu WarehouseRequisitionItemId + Navigation)
  - IdealAkeWms/Data/ApplicationDbContext.cs (FK NO ACTION, Index)
  - IdealAkeWms/Migrations/<neu>_AddStockMovementWarehouseRequisitionItem.cs
  - SQL/166_AddStockMovementWarehouseRequisitionItem.sql (Nummer prüfen)
  - SQL/00_FreshInstall.sql (Schema + MigrationId)
  - IdealAkeWms/Controllers/WarehousePickingController.cs (Details GET Vorbelegung, Close/PrintAndClose buchen, Status-Guard, Ctor)
  - IdealAkeWms/Views/WarehousePicking/Details.cshtml (Select Entnahme-Platz in Spalte Lagerplatz, JS collectProgress/PrintAndClose)
  - IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs (Platzliste, Vorschlag, Hinweis, gebuchte Mengen)
  - IdealAkeWms/Data/Repositories/IStockMovementRepository.cs + StockMovementRepository.cs (Summe gebucht je Position)
  - IdealAkeWms/Controllers/PickingController.cs (QuellVorschlag :670-679 unverändert herausziehen, Aufrufer :334 und :628 umstellen)
  - IdealAkeWms/Services/QuellLagerplatzVorschlag.cs (neu, statisch, Inhalt = bisheriges PickingController.QuellVorschlag)
  - IdealAkeWms.Tests/Controllers/PickingControllerTests.cs (Test QuellVorschlag_HauptplatzVorMengeVorNan :394-403 auf die neue Klasse umstellen)
  - IdealAkeWms/Models/AppSettingKeys.cs + AppSettingDefinitions.cs (neuer Schalter)
  - IdealAkeWms.Tests (WarehousePickingController-Tests, Repository-Test, Drift-Test AppSettings)
  - docs/TESTSZENARIEN.md (TS-178), docs/anwender (Lagerbestellung Lager-Sicht), Views/Help/Changelog.cshtml, beide AppVersion.cs
open_questions: []
# Rückfragen 1–10 am 2026-11-23 von Gerald beantwortet (Block „Freigabe-Antworten“) und in den Body eingearbeitet.
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: "Gerald"
freigabe_am: "2026-11-23"
---

# Lagerbestellungen: Entnahme-Lagerplatz wählen und Verbrauchsbuchung bei Lieferung

Quelle: [[2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung]]. Keine Abhängigkeit (seit Freigabe-Antwort 1): Der Platz-Vorschlag
folgt der Kommissionierung (`PickingController.QuellVorschlag`), nicht dem Wareneingang. ~~Abhängigkeit [[2026-11-23-wareneingang-zu-bestellung-bedienung-spec]]
(Hauptlagerplatz-Vorschlag) – vorher bauen~~ *(überholt durch Antwort 1; die WE-Spec bleibt nur Bezug: dort gilt immer der Hauptlagerplatz ohne Bestandsbedingung).*
Bezug: [[2026-07-29-sage-lagerbuchungen-spec]] (Queue + `SageBookingWorker`), [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]]
(Sage-WE-Platzregel `IstSageWePlatz`), [[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]] (dasselbe FK-Muster
`WarehouseRequisitionItemId` an `BdeTeilMeldungen`, keine Lagerbuchung dort).

> [!info] Codestand
> Fundstellen beziehen sich auf den Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Stand 2026-11-23), nicht auf `main`.

## Ziel / Nutzen (das Warum)

Das Lager liefert Lagerbestellungen der Werkbänke aus und trägt dabei nur die Ist-Menge ein. Eine Lagerbewegung entsteht dabei **nicht**:
Weder `WarehousePickingController` noch `WarehouseRequisitionRepository` noch der Dienst schreiben eine `StockMovement`
(Belege unten, B1–B3). Der WMS-Bestand bleibt nach jeder Lieferung zu hoch, Sage erfährt nichts. Ziel: Beim Abschließen der Lieferung
bucht das WMS die gelieferte Menge vom gewählten Lagerplatz aus (Verbrauch) und meldet sie über die bestehende Sage-Lagerbuchung als
Entnahme. Der Platz ist wie in der Kommissionierung vorbelegt (Hauptlagerplatz mit Bestand, sonst Platz mit dem meisten Bestand).

## Befund (belegt)

| # | Befund | Fundstelle |
|---|---|---|
| B1 | Lieferung = `Close`/`PrintAndClose` auf der Lager-Detailseite; schreibt nur `QuantityPicked`, Notizen, Fehlteil-Status, Kopf-Status. Keine Lagerbewegung. | `WarehousePickingController.cs:153-223, 287-339`; `WarehouseRequisitionRepository.cs:193-238` |
| B2 | Kein Schreiber von `StockMovement` mit Lagerbestellbezug: Grep `WarehouseRequisition` in `IdealAkeWms` (ohne Migrationen) trifft weder `StockMovementRepository` noch Buchungs-Services; im Dienst nur Mail-Versand (`WarehouseRequisitionEmailService`, `SyncWorker.cs:172-176`). | Grep im Worktree |
| B3 | `StockMovement` hat keinen Bezug zu Lagerbestellungen (Felder: Artikel, Menge, Platz, FA-Text/-Id, Bewegungsart, Quell-Platz, Notiz, WE-Receipt). | `StockMovement.cs:5-60` |
| B4 | Bewegungsarten: `Einbuchung`, `Ausbuchung`, `Umbuchung`, `SageEinbuchung`, `SageAusbuchung`. Eine Art „Verbrauch“ gibt es nicht. | `MovementType.cs:3-10` |
| B5 | Sage-Übergabe: Decorator `SageBookingEnqueueingStockMovementRepository.AddAsync` reiht `Ausbuchung` ein, wenn ServiceSetting `SageLagerbuchungAktiv` an und Platz `SageBuchungErlaubt`; Fehler beim Einreihen werden geschluckt, der 15-Min-Sweep holt nach. Ausbuchung → Sage `Entnahme`, Standardtext „Abgang / Entnahme Material“. | `SageBookingEnqueueingStockMovementRepository.cs:41-80`; `SageBookingEnqueueDecision.cs:40-71`; `SageLagerbuchungPayloadBuilder.cs:47-52` |
| B6 | SData `LagerbuchungService` kennt nur `Lagerbewegungsart` Zugang=1/Entnahme=2/Umbuchung=3 und **kein** Kostenstellen-Feld; je Zeile gibt es `Referenz` und `Memo`. | [[sage-sdata-commonwawiservices]] Z. 37, 56-58 |
| B7 | `WarehouseRequisitionItem` speichert die Artikelnummer als Text, keine `ArticleId`; DUMMY-Positionen haben die Nummer `DUMMY`. | `WarehouseRequisitionItem.cs:11-12`; `Article.cs:12`; `WarehouseRequisitionsApiController.cs:130-162` |
| B8 | `QuantityPicked` ist ein absoluter Wert und wird bei jedem `Close` und beim Autosave (`SaveProgress`) überschrieben; ein teilgelieferter Auftrag kann erneut abgeschlossen werden. Eingabe nur ganzzahlig (`int?[]`, `step="1"`). | `WarehouseRequisitionRepository.cs:217-219, 289-356`; `Details.cshtml:6-7, 88-91`; [[fallstricke]] Z. 367-372 |
| B9 | `Close`/`PrintAndClose` prüfen den Kopf-Status **nicht** (auch `CloseAsync` nicht); die Detailseite liefert für Entwürfe 404, ein direkter POST auf eine stornierte oder erledigte Liste läuft aber durch. | `WarehousePickingController.cs:121-124, 153-223`; `WarehouseRequisitionRepository.cs:193-238` |
| B10 | Ausbuchung im Warenausgang: Bestandsprüfung am Platz, bei Fehlbestand nur mit `NegativeBuchungErlaubt` auf den Ausweichplatz (`NegativeBuchungLagerplatz`, Default `NAN`). | `StockMovementsController.cs:334-365`; `AppSettingDefinitions.cs:110-116` |
| B11 | Plätze mit Quelle Sage werden vom Lagerbestand-Abgleich auf den Sage-Bestand korrigiert (`SageEinbuchung`/`SageAusbuchung`), Schalter `Sync:LagerbestandEnabled` (Default aus). Eine WMS-Ausbuchung auf einem Sage-Platz ohne Sage-Entnahme würde beim nächsten Lauf zurückgebucht. | `LagerbestandSyncService.cs:153, 184, 205-207, 252`; `ServiceSettingDefinitions.cs:47, 272` |
| B12 | Die Werkbank kennt eine Sage-Kostenstelle (`ProductionWorkplace.SageKostenstelle`, aus dem Arbeitsplatz-Abgleich). | `ProductionWorkplace.cs:68-71`; `ProductionWorkplaceSyncService.cs:162, 320` |
| B13 | Bedarf der Bestellvorschläge aus Lagerbestellungen = abgeschickt: bestellt; teilgeliefert mit „Fehlteil“: bestellt minus `QuantityPicked`. Die gelieferte Menge fällt dort schon heute heraus; die neue Ausbuchung ändert diese Rechnung nicht. | `PurchaseProposalRepository.cs:106-119` |
| B14 | Quell-Vorschlag der Kommissionierung: `QuellVorschlag(locations, buchbarIds, nanId)` = Hauptlagerplatz mit Bestand > 0 unter den erlaubten Plätzen, sonst erlaubter Platz mit der höchsten Menge, sonst `NAN`. Genutzt von Auftragsseite und Auftragsdruck; Test vorhanden. Hauptlagerplatz-Kennung `StockLocationInfo.IsPrimaryStorageLocation` kommt aus `Article.PrimaryStorageLocationId` in `GetStockByArticleNumbersAsync`. ~~`QuellVorschlag` ungeeignet~~ *(überholt durch Freigabe-Antwort 1: genau diese Regel ist gewollt).* | `PickingController.cs:670-679` (Aufrufer :334, :628); `PickingControllerTests.cs:394-403`; `StockMovementRepository.cs:455-476` |
| B16 | Die Lager-Detailseite lädt den Bestand je Position heute schon über `GetStockByArticleNumbersAsync` (für den Lagerplatz-Text) – dieselben Daten, die `QuellVorschlag` braucht. | `WarehousePickingController.cs:114-119, 127-133` |
| B17 | Ist unter „schon gebucht“ senken ist erreichbar: Teilgelieferte Listen bleiben bearbeitbar (`isEditable` = abgeschickt oder teilgeliefert), das Ist-Feld ist mit dem gespeicherten `QuantityPicked` vorbelegt und frei änderbar, `CloseAsync` und `SaveProgressAsync` überschreiben absolut. | `Details.cshtml:6-7, 76-91`; `WarehouseRequisitionRepository.cs:219, 313-319` |
| B15 | Zugriff: Lager-Detailseite `[RequireLagerProcessingAccess]` (admin, stock, stock_keyuser) + `[RequireLagerbestellungAktiv]`; Ausbuchung im Warenausgang `[RequireStockAccess]` (zusätzlich picking). | `WarehousePickingController.cs:10-11`; [[controller]] Z. 63, 65 |

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:**
- F1 Entnahme-Lagerplatz je Position auf der Lager-Detailseite (`/WarehousePicking/Details/{id}`), vorbelegt nach der Regel der
  Kommissionierung (`QuellVorschlag`, dazu einmal herausgezogen, nicht kopiert).
- F2 Verbrauchsbuchung beim Abschließen (`Close`, `PrintAndClose`): WMS-`Ausbuchung` je Position über die Differenz, verknüpft mit der Position.
- F3 Sage-Übergabe über den **bestehenden** Weg (Decorator → Queue → `SageBookingWorker` → `LagerbuchungService` Entnahme), ohne Änderung
  an Dienst, Payload oder Schaltern.
- F4 Status-Guard für `Close`/`PrintAndClose` (nur abgeschickt/teilgeliefert), weil daran jetzt eine Buchung hängt (B9).
- F5 AppSetting-Schalter, Default aus.

**Out-of-Scope:**
- Neue Bewegungsart „Verbrauch“ (siehe Lösungsentwurf, warum nicht).
- Kostenstelle/Kostenträger an Sage (B6; Antwort 5: nicht notwendig).
- Änderungen an `SageBookingWorker`, `SageLagerbuchungPayloadBuilder`, `EnqueueMissingAsync` (keine nötig, Ausbuchung ist dort schon abgedeckt).
- Rückbuchung bei Storno einer schon (teil)gelieferten Liste: die Ware ist physisch an der Werkbank, die Buchung bleibt.
- Besteller-Seite (`WarehouseRequisitionsController`, API) und Druck (`WarehousePickingPrintLayout`): unverändert; die Spalte „Lagerplatz“ druckt
  weiter den Bestandstext.
- Wareneingang (Zugang vom Lieferanten) – das ist [[2026-11-23-wareneingang-zu-bestellung-bedienung-spec]].

## Fachliche Anforderungen

Alle Anforderungen gelten nur bei eingeschaltetem Schalter `LagerbestellungVerbrauchsbuchungAktiv` (F5). Aus = Seite und Abschließen exakt wie heute.

### F1 Entnahme-Lagerplatz je Position (Antworten 1, 7, 10)

- In der Spalte „Lagerplatz“ steht im Bearbeitungsmodus (`isEditable`, `Details.cshtml:6-7`) über dem bisherigen Bestandstext ein Select
  „Entnahme aus“ mit den **erlaubten Plätzen** = buchbare Plätze (dieselbe Liste wie Kommissionierung/Warenausgang:
  `GetActiveOrderedExcludingPickingTransportAsync` → aktiv, `IstBuchbar`, kein Kommissionierwagen, `StorageLocationRepository.cs:43-49`)
  **ohne** Sage-Plätze, deren Entnahme nicht an Sage ginge (Antwort 10, Regel in F3). Keine neue Spalte – damit bleiben
  `ColumnDefinitions.WarehousePickingDetails`, `#column-config` und `WarehousePickingPrintLayout.CellText` unberührt ([[fallstricke]] Z. 411-418).
- Vorbelegung (nur GET) = **dieselbe Regel wie die Kommissionierung** (Antwort 1, B14), aufgerufen mit dem Bestand der Position (B16) und den
  erlaubten Plätzen als `buchbarIds`:
  1. Hauptlagerplatz des Artikels, wenn er erlaubt ist und Bestand > 0 hat;
  2. sonst erlaubter Platz mit dem meisten Bestand;
  3. sonst `NAN` (wenn vorhanden und erlaubt – wie in der Kommissionierung), sonst leer („— wählen —“).
  Weil die Sage-Plätze ohne Sage-Übergabe schon aus `buchbarIds` fallen, wird ein solcher Platz nie vorgeschlagen; die Regel selbst bleibt
  unverändert.
- Hinweistext unter dem Select (klein, `text-body-secondary`, Farbe nie allein), im Controller aus dem Ergebnis abgeleitet (keine Änderung an
  der Regel): „Vorschlag: Hauptlagerplatz“ / „Vorschlag: meister Bestand (Hauptlagerplatz ohne Bestand)“ / „Kein Bestand – Vorschlag <NAN-Code>“
  / „Kein Vorschlag – bitte Platz wählen“.
- Positionen ohne Artikel im WMS (Nummer nicht gefunden) und DUMMY-Positionen: kein Select, Hinweis „wird nicht gebucht (kein Lagerartikel)“
  (Antwort 7).
- ~~Vorbelegung mit dem Hauptlagerplatz nach der Regel der WE-Spec (ohne Bestandsbedingung, `IstSageWePlatz`)~~ *(überholt durch Antwort 1).*
- Die Platzwahl wird **nicht** gespeichert (auch nicht vom Autosave). Was gebucht wurde, steht in der Erfolgsmeldung je Position und danach
  in der Bewegungshistorie (F2).

### F2 Verbrauchsbuchung beim Abschließen (Antworten 2, 3, 4)

- Auslöser: `Close` und `PrintAndClose`, **nach** der bestehenden Pflichtprüfung (Ist oder Fehlteil, `CloseAsync:206-215`). Autosave und
  `SaveProgress` buchen nie.
- Gebucht wird immer als **Entnahme** (WMS `Ausbuchung`, Antwort 2) die beim Abschließen eingetragene **Ist-Menge** (Antwort 3). Damit ein
  erneutes Abschließen derselben Liste (teilgeliefert, B8) nicht doppelt bucht, rechnet das WMS technisch gegen das schon Gebuchte:
- Menge je Position: `Δ = Ist (neu) − Σ schon gebuchter Ausbuchungen dieser Position` (Summe über `StockMovement.WarehouseRequisitionItemId`).
  Beim ersten Abschließen ist Σ = 0, gebucht wird also das volle Ist.
  - `Δ > 0` → eine `StockMovement` `Ausbuchung`, Menge `Δ`, Platz = gewählter Platz, `WarehouseRequisitionItemId` = Position,
    `Note` = „Lagerbestellung #<Id> Pos <n> – <Werkbank>“, `ProductionOrder` leer, Audit wie Warenausgang (`StockMovementsController.cs:369-383`).
  - `Δ = 0` → nichts (zweites Abschließen, unveränderte Menge).
  - `Δ < 0` (Ist unter schon gebucht gesenkt) → Zeile abweisen: „Pos <n>: Ist <x> kleiner als bereits ausgebucht <y> – Korrektur über
    Wareneingang/Umbuchung“, nichts gebucht, nichts abgeschlossen. Bleibt drin, weil der Fall erreichbar ist (B17); ohne die Abweisung würde
    eine Senkung still ignoriert und das Ist auf der Liste wiche von der Buchung ab.
- Überlieferung (Ist > bestellt) bucht das volle Ist; Teillieferung bucht nur das Gelieferte; der Rest folgt beim nächsten Abschließen
  (Status teilgeliefert, B8).
- Pflicht bei `Δ > 0`: ein Platz ist gewählt und steht in der erlaubten Liste (Server prüft, nicht nur das Select), sonst Zeilenfehler.
- Bestand (Antwort 4 = Warenausgang-Regel, keine eigene Einstellung, B10): reicht der Bestand am gewählten Platz nicht, wird ohne `NegativeBuchungErlaubt`
  die Zeile abgewiesen („Pos <n>: nicht genug Bestand auf <Code>, verfügbar <x>“); mit `NegativeBuchungErlaubt` wird vom Ausweichplatz gebucht
  und eine Warnung angezeigt. Die Regel wird **einmal** gebaut und von Warenausgang und Lagerbestellung benutzt (heute inline in
  `StockMovementsController.Outbound`), damit es keine zwei Wahrheiten gibt.
- Alles oder nichts: Gibt es einen Zeilenfehler, wird **nichts** gebucht und **nicht** abgeschlossen; die Eingaben werden wie beim
  Pflichtprüfungs-Fehler als Zwischenstand gesichert (`SaveProgressAsync`, `WarehousePickingController.cs:212-219`), die Meldung nennt alle
  betroffenen Positionen (Sammelmeldung). `PrintAndClose` antwortet mit `BadRequest(new { error = … })` wie heute.
- Abschluss und Buchungen laufen in **einer** DB-Transaktion (Muster `BdeBookingsController.cs:253`: `IsRelational() ? BeginTransactionAsync : null`).
  Eine `DbUpdateConcurrencyException` am Kopf (RowVersion) rollt die Buchungen mit zurück; zwei parallele Abschlüsse buchen damit nicht doppelt.
- Erfolgsmeldung zusätzlich: „<k> Position(en) ausgebucht: Pos 1 5 Stk von LL;1;4;0, …“ und ggf. „<m> Position(en) nicht gebucht (kein
  Lagerartikel/DUMMY): …“.

### F3 Sage-Übergabe (Antworten 2, 5, 6, 10)

- Keine eigene Sage-Logik: Die `Ausbuchung` geht über `IStockMovementRepository.AddAsync` (Decorator, B5). An Sage geht sie genau dann, wenn
  `SageLagerbuchungAktiv` an und der Platz `SageBuchungErlaubt` ist – als `Entnahme`, Kennung `SM#[K#]<Id>#` im Memo, Wiederholschutz wie bisher.
- Sage-Plätze ohne Sage-Übergabe (Antwort 10): Sie stehen nicht in der Auswahl und werden nicht vorgeschlagen (F1). Hat der gewählte Platz
  (manipulierter POST oder Schalterwechsel zwischen Laden und Abschließen) die Quelle Sage (`StorageLocation.Source == "Sage"`) und
  ginge die Entnahme **nicht** an Sage (Schalter aus oder Platz ohne „Sage-Buchung erlaubt“/Sage-Referenz), wird die Zeile abgewiesen:
  „<Code> ist ein Sage-Platz ohne Sage-Buchung – der Lagerbestand-Abgleich würde die Entnahme zurückbuchen. Bitte WMS-Platz wählen oder
  Sage-Buchung am Platz einschalten.“ Grund: B11. Reine WMS-Plätze (Quelle Manual) sind immer erlaubt.
- „Entnahme“ ist die richtige Sage-Bewegung (Antworten 2 und 6). Keine Kostenstelle an Sage (Antwort 5); Werkbank und Listennummer stehen
  nur in der WMS-Notiz.
- Heute bucht das Lager diese Lieferungen in Sage **von Hand** aus (Antwort 6). Ab dem Einschalttag bucht das WMS; die Handbuchung in Sage
  endet am selben Tag, sonst wird doppelt entnommen. Das ist ein organisatorischer Schritt im Deploy (Manifest-Info), keine Code-Logik.
- ~~Ob „Entnahme“ richtig ist und ob heute von Hand entnommen wird, ist unbelegt → S-1 bis S-3~~ *(überholt durch Antwort 6; S-1 nur noch
  optionale Kontrolle).*

### F4 Status-Guard

`Close` und `PrintAndClose` buchen und schließen nur, wenn der Kopf-Status vor dem Abschließen „abgeschickt“ oder „teilgeliefert“ ist; sonst
`WarningMessage` „Liste #<Id> ist <Status> – nichts gebucht.“ bzw. `BadRequest`. (Heute fehlt der Guard ganz, B9.)

### F5 Schalter

Siehe Abschnitt Einstellungen. Ist der Schalter aus, zeigt die Seite kein Select und bucht nichts; F4 gilt immer (Guard ohne Buchung schadet nicht).

## Technischer Loesungsentwurf

Muster: Repository-Interfaces (ADR 0001), Sage-Queue-Decorator unverändert, MVC-Controller mit `TempData` (nur Success/Warning), Feature-Gate
kumulativ (ADR 0011), Einstellung nach ADR 0020.

**Warum keine neue Bewegungsart „Verbrauch“ (Leiter Sprosse 2):** `Ausbuchung` ist an rund zehn Stellen verdrahtet – Bestandssummen
(`StockMovementRepository.cs:70, 223, 426, 501, 572`), Anzeige (`:371, 643`), `PickingTransferService.cs:148`, Sage-Entscheidung
(`SageBookingEnqueueDecision.cs:20-21, 43`), Payload (`SageLagerbuchungPayloadBuilder.cs:49-51`), Queue-Anzeige (`SageBookingQueueController.cs:44`).
Eine neue Art müsste jede davon nachziehen und wäre an Sage trotzdem eine Entnahme. Erkennbar ist der Verbrauch über den FK und die Notiz.

**Datenmodell:** `StockMovement.WarehouseRequisitionItemId int?` + Navigation `WarehouseRequisitionItem?`, FK ON DELETE NO ACTION, Index.
Löschen einer Position mit Buchung ist ausgeschlossen, weil Positionen nur im Entwurf gelöscht werden (`WarehouseRequisitionsApiController.cs:310-320`
mit `CheckOwnershipAndDraft`) und im Entwurf nie gebucht wird (F4). Gleiches Muster wie `BdeTeilMeldungen.WarehouseRequisitionItemId` in der BDE-Liste-Spec.

**Repository:** `IStockMovementRepository.GetAusgebuchtJeLagerbestellPositionAsync(IReadOnlyCollection<int> itemIds)` → `Dictionary<int, decimal>`
(Σ `Quantity` der `Ausbuchung`en je Position, eine Abfrage). Geht durch den Decorator als Pass-through.

**Platz-Vorschlag (Wiederverwendung der Kommissionierung, Antwort 1):** `PickingController.QuellVorschlag` (`PickingController.cs:670-679`,
`internal static`, reine Funktion) wird **unverändert** in eine statische Klasse verschoben, z. B. `IdealAkeWms/Services/QuellLagerplatzVorschlag.cs`
mit `Waehle(IEnumerable<StockLocationInfo> locations, ISet<int> buchbarIds, int? nanId)`. `PickingController` ruft sie an beiden Stellen
(:334, :628) auf; der bestehende Test `QuellVorschlag_HauptplatzVorMengeVorNan` (`PickingControllerTests.cs:394-403`) zieht auf die neue Klasse
um und bleibt der Regeltest für beide Aufrufer. Kein Kopieren, keine neue Regel. Die Lager-Detailseite holt den Bestand je Position ohnehin
(`LagerplatzTextAsync`, `WarehousePickingController.cs:114-119`): dieselbe Liste speist Text **und** Vorschlag (eine Abfrage statt zwei;
`LagerplatzTextAsync` gibt dazu die Liste zurück oder wird um den Vorschlag erweitert – Dev entscheidet). `buchbarIds` = Ids der erlaubten
Plätze (F1: buchbar minus Sage-Plätze ohne Sage-Übergabe), `nanId` = `GetByCodeAsync("NAN")?.Id` wie in der Kommissionierung (`PickingController.cs:306-307`).
Die Detailseite fragt heute je Position einzeln ab (`WarehousePickingController.cs:127-133`); `// ponytail:`-Vermerk, Batch-Abfrage erst bei
messbar langen Listen. Die Artikel-`Id` für die Buchung kommt aus `IArticleRepository.GetByArticleNumberAsync` (fehlt sie → „kein Lagerartikel“).

~~**Hauptlagerplatz-Vorschlag (Wiederverwendung, Abhängigkeit):** gemeinsame Funktion `HauptlagerplatzVorschlag.Waehle` mit der WE-Spec~~
*(überholt durch Antwort 1: die WE-Regel – immer Hauptlagerplatz ohne Bestandsbedingung – ist eine andere Regel; keine gemeinsame Funktion, keine Abhängigkeit).*

**Controller `WarehousePickingController`:**
- Ctor: zusätzlich `IArticleRepository`, `IStorageLocationRepository`, `IAppSettingRepository`, `IServiceSettingRepository` und der
  `ApplicationDbContext` nur für die Transaktion (wie `BdeBookingsController`). Tests mit Ctor-Aufruf ziehen mit („bewusst geändert“).
- `Details` (GET): bei Schalter an Platzliste (erlaubte Plätze), Vorschlag je Position über `QuellLagerplatzVorschlag.Waehle`, bereits gebuchte Menge je Position (Anzeige „davon ausgebucht: y“ im
  Read-only-Modus) ins ViewModel.
- `Close`/`PrintAndClose`: neuer Parameter `int?[]? storageLocationIds` (Index-gleich zu `itemIds`; leerer Select bindet als `null` am selben
  Index, `NullableIntArrayBindingTests`); bei abweichender Länge wie heute `InvalidQuantityMessage`. Ablauf: Status-Guard → Pflichtprüfung →
  Δ und Zeilenprüfung (Platz, Sage-Platz-Regel, Bestand) → Transaktion { `CloseAsync`; je Δ > 0 `AddAsync` } → Commit. Gemeinsamer privater
  Helfer für beide Actions (heute schon doppelt implementiert, keine dritte Kopie).
- Ausweichplatz-/Bestandsregel aus `StockMovementsController.Outbound` (`:334-365`) in eine gemeinsame Methode ziehen (Ort: Dev entscheidet,
  z. B. statischer Helfer neben `SageWareneingang`), Warenausgang nutzt sie unverändert weiter (Regressionstest).

**View `Details.cshtml`:** Select + Hinweis in der Lagerplatz-Zelle (nur bei Schalter an und `isEditable`); `collectProgress`/PrintAndClose-JS
(`:285-299`) schickt `storageLocationIds` mit (SaveProgress ignoriert sie). Skill `frontend-design` Pflicht; Kontrast AA.

## Migrations-/SQL-Auswirkungen

Schema-Änderung nach ADR 0004, additiv, nicht daten-destruktiv:
1. Model → `dotnet ef migrations add AddStockMovementWarehouseRequisitionItem`.
2. `SQL/166_AddStockMovementWarehouseRequisitionItem.sql` (Nummer im Dev-Lauf gegen Worktree und Entwürfe prüfen; Stand 2026-11-23 höchste
   Datei `SQL/163`, 164 an die BDE-Liste-Spec vergeben, 165 reserviert):
   - Batch 1: `IF COL_LENGTH('dbo.StockMovements','WarehouseRequisitionItemId') IS NULL ALTER TABLE … ADD WarehouseRequisitionItemId int NULL;`
   - Batch 2: FK `FK_StockMovements_WarehouseRequisitionItems_WarehouseRequisitionItemId` (ON DELETE NO ACTION) mit `OBJECT_ID`-Guard;
     Index `IX_StockMovements_WarehouseRequisitionItemId` mit `sys.indexes`-Guard.
   - Eigener Batch: `__EFMigrationsHistory`-Insert mit `IF NOT EXISTS`.
3. `SQL/00_FreshInstall.sql` an **beiden** Stellen (Spalte/FK/Index in `StockMovements` und `MigrationId`).
4. `SQL/AgentJobs/`: nicht betroffen (Grep `StockMovements` dort ohne Treffer).

Keine Datenmigration: Altlieferungen bleiben ungebucht (kein Nachbuchen – das würde heutige Bestände verfälschen).

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `LagerbestellungVerbrauchsbuchungAktiv` (Anzeigename „Lagerbestellung: Lieferung ausbuchen“, Gruppe Bestellungen, abhängig von `LagerbestellungAktiv`) | Bool (Auswahl an/aus, `B(...)` in `AppSettingDefinitions`, Seed nein) | true / false | false | An = Lager-Detailseite zeigt je Position „Entnahme aus“ (Vorschlag wie Kommissionierung); beim Abschließen wird die gelieferte Menge vom gewählten Platz ausgebucht. Aus = wie bisher, keine Buchung. | Lager → Lagerbestellungen/Glasbestellungen → Details (Abschließen, Drucken und Abschließen) | beide – normale AppSetting je Installation, keine Standort-Sonderlogik (Antwort 9). Ob die Entnahme an Sage geht, entscheidet allein die bestehende Sage-Lagerbuchung (B5). | „An = jede Lieferung einer Lager-/Glasbestellung bucht Bestand aus. Mit eingeschalteter Sage-Lagerbuchung (Service-Einstellung „Lagerbuchungen an Sage senden“) und Platz mit „Sage-Buchung erlaubt“ geht die Entnahme an Sage – die Handbuchung in Sage muss am Einschalttag enden, sonst wird doppelt entnommen.“ |

Einschalten entscheidet Gerald nach dem Deploy (Antwort 9).

Bestehende Einstellungen nur gelesen, nicht geändert: `SageLagerbuchungAktiv` (ServiceSetting), `NegativeBuchungErlaubt`, `NegativeBuchungLagerplatz`
(deren Feld „Verwendung“ um „Lagerbestellung (Lieferung)“ ergänzen), `LagerbestellungAktiv`.

## Audit-Feld-Auswirkungen

- Neue `StockMovement`: `CreatedAt/By/ByWindows`, `UserId`, `WindowsUser`, `Timestamp` aus `ICurrentUserService` wie im Warenausgang.
- `WarehouseRequisitionItem`/`WarehouseRequisition`: unverändert über `CloseAsync` (`ModifiedAt/By/ByWindows`, `WarehouseRequisitionRepository.cs:226-235`).
- `SageBookingQueueItem`: unverändert über den Decorator.

## Rollen und Zugriff

Keine neue Rolle, kein neuer Filter (Antwort 8): Buchen darf, wer die Lager-Detailseite bearbeiten darf
(`[RequireLagerProcessingAccess]`: admin, stock, stock_keyuser – eine Teilmenge von `[RequireStockAccess]`, das die normale Ausbuchung erlaubt).
Feature-Gates kumulativ: `[RequireLagerbestellungAktiv]` (Klasse) + neuer Schalter (nur Verhalten, kein Zugriffsfilter).
`controller.md` und `RoleOverview.cshtml` bleiben unberührt.

## Listen-View-Pattern

Keine neue Tabellenansicht. Die Detailtabelle bekommt keine neue Spalte (Select in der bestehenden Zelle „Lagerplatz“); Spaltenfilter,
Zahnrad und Druck bleiben gleich.

## Prüfabfragen

> [!note] Stand nach Freigabe-Antworten
> Keine dieser Abfragen ist Freigabe- oder Einschalt-Voraussetzung (Antwort 6: heute Handbuchung in Sage, Entnahme ist richtig). S-1 bleibt als
> **optionale Kontrolle** nach dem ersten Sage-Test (178.6). S-2 und S-3 sind überholt und bleiben nur als Nachweis stehen. P-4 dient der
> Einschätzung des Umfangs vor dem Einschalten.

**S-1 (optional; Sage AKE-Test, nur SELECT, Mensch) – als welche Sage-Bewegungsart landet eine WMS-Entnahme?** Spalten `Memo`, `Bewegungsart`,
`Bewegungsdatum` belegt in [[2026-07-29-sage-lagerbuchungen-spec]] Z. 1022-1038; ob die Tabelle eine Spalte `Mandant` hat, ist unbelegt (ggf.
`AND Mandant = 1` ergänzen):
```sql
SELECT Bewegungsart, COUNT(*) AS Anzahl, MIN(Bewegungsdatum) AS Von, MAX(Bewegungsdatum) AS Bis
FROM dbo.KHKLagerplatzbuchungen
WHERE Memo LIKE '%SM#%'
GROUP BY Bewegungsart
ORDER BY Anzahl DESC;
```

**S-2 (überholt durch Antwort 6; Sage, nur SELECT, Mensch) – gibt es eine eigene Verbrauchs-Bewegungsart?** Tabellenname unbelegt (SData `LagerbewegungsartenInfo` liefert
404, [[sage-sdata-wms-schnittstellenkatalog]] Z. 19), daher zuerst suchen, dann lesen:
```sql
SELECT name FROM sys.tables WHERE name LIKE '%ewegungsart%' ORDER BY name;
-- danach: SELECT * FROM dbo.<gefundene Tabelle> ORDER BY 1;
```
Ergebnis entscheidet Rückfrage 6: Gibt es „Verbrauch“ und ist sie über `LagerbuchungService` (nur Zugang/Entnahme/Umbuchung, B6) nicht
wählbar, bleibt es bei Entnahme oder es braucht einen anderen Sage-Service (dann eigene Spec).

**S-3 (überholt durch Antwort 6 – ja, heute Handbuchung; Sage produktiv AKE, nur SELECT, Mensch) – wird heute von Hand ausgebucht (Gefahr doppelte Entnahme)?**
```sql
SELECT Bewegungsart, COUNT(*) AS Anzahl
FROM dbo.KHKLagerplatzbuchungen
WHERE Bewegungsdatum >= DATEADD(day, -30, CAST(GETDATE() AS date))
GROUP BY Bewegungsart
ORDER BY Anzahl DESC;
```
Dazu die WMS-Seite (P-4) und der Abgleich mit dem Lager: Entsprechen manuelle Entnahmen in Sage etwa der Zahl gelieferter Positionen?

**P-4 (WMS-DB AKE- und IDEAL-DEV bzw. produktiv, nur SELECT) – Umfang und Sonderfälle der letzten 30 Tage:**
```sql
SELECT r.Type,
       COUNT(DISTINCT r.Id) AS Listen,
       COUNT(*) AS Positionen,
       SUM(CASE WHEN i.QuantityPicked > 0 THEN 1 ELSE 0 END) AS GeliefertePositionen,
       SUM(CASE WHEN i.ArticleNumber = 'DUMMY' THEN 1 ELSE 0 END) AS Dummy,
       SUM(CASE WHEN a.Id IS NULL THEN 1 ELSE 0 END) AS OhneArtikel,
       SUM(CASE WHEN a.Id IS NOT NULL AND a.PrimaryStorageLocationId IS NULL THEN 1 ELSE 0 END) AS OhneHauptlagerplatz,
       SUM(CASE WHEN h.Source = 'Sage' AND h.SageBuchungErlaubt = 0 THEN 1 ELSE 0 END) AS HauptplatzSageOhneSageBuchung
FROM dbo.WarehouseRequisitionItems i
JOIN dbo.WarehouseRequisitions r ON r.Id = i.WarehouseRequisitionId
LEFT JOIN dbo.Articles a ON a.ArticleNumber = i.ArticleNumber
LEFT JOIN dbo.StorageLocations h ON h.Id = a.PrimaryStorageLocationId
WHERE r.ClosedAt >= DATEADD(day, -30, CAST(GETDATE() AS date))
GROUP BY r.Type;
```

## Akzeptanzkriterien

1. Schalter aus: Detailseite, Abschließen und Drucken+Abschließen verhalten sich wie vor dem Update; keine `StockMovement` entsteht.
2. Schalter an, Artikel mit Bestand > 0 auf seinem (erlaubten) Hauptlagerplatz: Select „Entnahme aus“ ist mit dem Hauptlagerplatz vorbelegt,
   Hinweis „Vorschlag: Hauptlagerplatz“ – auch wenn ein anderer Platz mehr Bestand hat.
3. Hauptlagerplatz ohne Bestand (oder keiner), Bestand auf anderen erlaubten Plätzen: vorbelegt ist der Platz mit dem meisten Bestand; ganz
   ohne Bestand: `NAN` (falls vorhanden), sonst „— wählen —“; jeweils mit Hinweis. Für dieselben Bestandsdaten liefert die Kommissionierung
   denselben Platz (eine Regel, `QuellLagerplatzVorschlag.Waehle`); Auftragsseite und Auftragsdruck der Kommissionierung schlagen unverändert vor (Regression).
4. Ein Sage-Platz, dessen Entnahme nicht an Sage ginge (`SageLagerbuchungAktiv` aus oder Platz ohne „Sage-Buchung erlaubt“/Sage-Referenz),
   steht weder in der Auswahl noch wird er vorgeschlagen – auch wenn er Hauptlagerplatz mit Bestand ist; ein Sage-Platz mit Sage-Übergabe schon.
5. DUMMY-Position und Position, deren Artikelnummer nicht im Artikelstamm steht: kein Select, Hinweis „wird nicht gebucht“; Abschließen
   gelingt, die Erfolgsmeldung nennt sie als nicht gebucht.
6. Abschließen mit Ist 5 auf Platz X: genau eine `Ausbuchung` 5 auf X mit `WarehouseRequisitionItemId` der Position und Notiz
   „Lagerbestellung #<Id> Pos <n> – <Werkbank>“; Bestand auf X sinkt um 5.
7. Teillieferung (Ist 3 von 5, Fehlteil): Ausbuchung 3, Status teilgeliefert; zweites Abschließen mit Ist 5: weitere Ausbuchung 2, Status
   erledigt; drittes Abschließen ohne Änderung: keine weitere Buchung.
8. Überlieferung (Ist 7 bei bestellt 5): Ausbuchung 7.
9. Teilgelieferte Liste, Ist unter schon gebucht gesenkt (erreichbar, B17): Meldung mit Position, nichts gebucht, nichts abgeschlossen,
   Eingaben als Zwischenstand gesichert.
10. Ist > 0 ohne gewählten Platz oder mit einem Platz außerhalb der Liste (manipulierter POST): Zeilenfehler, nichts gebucht.
11. Bestand reicht nicht, `NegativeBuchungErlaubt` aus: Zeilenfehler mit verfügbarem Bestand, nichts gebucht; an: Buchung vom Ausweichplatz
    plus Warnung. Warenausgang verhält sich unverändert (Regression).
12. Mehrere fehlerhafte Zeilen: eine Meldung nennt alle; keine Position wird gebucht (alles oder nichts).
13. Sage-Platz ohne Sage-Übergabe gewählt (Quelle Sage, Schalter oder Platz-Flag aus): Zeilenfehler nach F3; reiner WMS-Platz bucht ohne Sage.
14. `SageLagerbuchungAktiv` an, Platz mit „Sage-Buchung erlaubt“: Queue-Eintrag zur Ausbuchung; nach dem Worker-Lauf `Bestaetigt`, in Sage eine
    Entnahme mit Memo `SM#…#` (AKE-Test).
15. Zwei gleichzeitige Abschlüsse derselben Liste: einer bekommt „inzwischen geändert“, es entstehen keine doppelten Ausbuchungen.
16. POST `Close`/`PrintAndClose` auf eine stornierte, erledigte oder Entwurfs-Liste: Warnung bzw. `BadRequest`, keine Buchung, keine Änderung.
17. Drucken und Abschließen bucht wie Abschließen (gleiche Fälle 6–13), Fehler kommen als JSON-Fehlertext.
18. Autosave/`SaveProgress` bucht nie.
19. Bestellvorschläge: Bedarf aus Lagerbestellungen bleibt rechnerisch unverändert (B13).
20. Glasbestellung: wird genauso gebucht wie eine Lagerbestellung (Antwort 7).
21. Nicht berechtigter Benutzer (nur `lagerbestellung`/`picking`) erreicht die Lager-Detailseite weiterhin nicht (Regression, kein neuer Weg).
22. Einstellung erscheint unter /Settings als Auswahl an/aus mit Anzeigename, Wirkung, Verwendung, Standort und Warnhinweis; Drift-Test grün.

## Test-Szenarien

Automatisiert (xUnit, EF-InMemory/Moq):
- Δ-Rechnung (erste, zweite, unveränderte Lieferung, Senkung, Überlieferung) über `GetAusgebuchtJeLagerbestellPositionAsync`.
- Controller: Schalter aus = keine `AddAsync`; Status-Guard; Platz-Pflicht und erlaubte Liste; DUMMY/ohne Artikel; Sage-Platz-Regel; alles oder
  nichts bei mehreren Fehlern; `PrintAndClose` JSON-Fehler; Array-Länge `storageLocationIds`.
- Vorschlagsregel: bestehender Test `QuellVorschlag_HauptplatzVorMengeVorNan` (`PickingControllerTests.cs:394-403`) auf
  `QuellLagerplatzVorschlag.Waehle` umgestellt, Inhalt unverändert; dazu ein Controller-Test, dass die Lager-Detailseite einen Sage-Platz ohne
  Sage-Übergabe nicht in `buchbarIds` gibt (AK 4).
- Bestandsregel: Warenausgang und Lagerbestellung nutzen dieselbe Methode (Regression Warenausgang).
- Hinweis: Transaktion und RowVersion sind InMemory nicht prüfbar ([[fallstricke]] „Tests, EF, SQL Server“) → Manual-UAT 178.8.

Manuell – `docs/TESTSZENARIEN.md` neues Kapitel **TS-178** „Lagerbestellung: Lieferung ausbuchen (Verbrauch) mit Sage-Übergabe“, Verweis im
Lagerbestellungs-Kapitel; `secondbrain/tests/testszenarien-index.md` nachziehen. Skizze:

| Nr | Vorbedingung / Schritte | Erwartung |
|---|---|---|
| 178.1 | Schalter aus; Lagerbestellung abschließen | wie bisher, keine Bewegung (AK 1) |
| 178.2 | Schalter an; Benutzer `stock`; Liste mit 5 Positionen: Artikel mit Bestand auf Hauptlagerplatz (und mehr auf einem Nebenplatz), Artikel mit leerem Hauptlagerplatz und Bestand anderswo, Artikel ganz ohne Bestand, DUMMY, unbekannte Nummer; Vergleich mit der Kommissionierung für einen der Artikel | Vorbelegung/Hinweise nach AK 2, 3, 5; gleicher Platz wie in der Kommissionierung |
| 178.3 | Ist 5 auf vorgeschlagenem Platz abschließen; Bewegungshistorie und Bestandsübersicht prüfen | AK 6 |
| 178.4 | Teillieferung + zweites Abschließen + drittes ohne Änderung | AK 7 |
| 178.5 | Negativ: Ist senken; kein Platz; Bestand zu klein (Minus-Schalter aus/an) | AK 9, 10, 11 |
| 178.6 | AKE-Test, `SageLagerbuchungAktiv` an, System-Kürzel gesetzt: Abschließen auf Sage-Platz mit Sage-Buchung; Sage-Queue und Sage prüfen | AK 14 |
| 178.7 | Artikel, dessen Hauptlagerplatz ein Sage-Platz ohne Sage-Buchung ist (Bestand > 0): Vorbelegung prüfen; Platz per manipuliertem POST trotzdem senden | AK 4, AK 13 |
| 178.8 | Zwei Browser, gleiche Liste, fast gleichzeitig abschließen | AK 15 |
| 178.9 | Drucken und Abschließen mit einem Fehlerfall und einem Gutfall | AK 17 |
| 178.10 | Glasbestellung abschließen | AK 20 |
| 178.11 | IDEAL-DEV: Schalter an, Abschließen (ServiceSetting `SageLagerbuchungAktiv` wie dort eingestellt) | WMS-Ausbuchung; Queue-Eintrag genau dann, wenn `SageLagerbuchungAktiv` an und Platz Sage-buchbar – keine IDEAL-Sonderlogik (Antwort 9) |

## Etappen (nur bei epic: true)

Nicht anwendbar (epic: false). Commit-Reihenfolge innerhalb der Spec: Schema (SQL/166 + Migration) → Repository + Regeln (Vorschlag, Bestand) →
Controller/View → Doku.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

> [!note] Vom QA-Lauf 2026-11-23 bestätigt – siehe „QA-Nachweis → Deploy (final)“ am Ende (Dienst ja, Migration SQL/166, Paket = Bündel v1.99.53).

- **Web-App:** ja (Model, Migration, Controller, View, AppSetting, Hilfe/Changelog, AppVersion).
- **Service:** nein fachlich (Worker/Payload unverändert). Das Model `StockMovement` ist im Dienst mitgenutzt (gemeinsames Projekt) → Dienst
  mit derselben Version publishen, damit EF-Model und Schema übereinstimmen; Versionsgleichstand `IDEALAKEWMSService/AppVersion.cs`.
- **Migration:** ja, additiv (SQL/166 vor dem Web-Start; Dienst und Web gestoppt wie üblich). Kein Datenverlust, kein Backup-Sonderfall.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):

| Id | Version | Typ | Firma | Phase | Ausführung | Text |
|---|---|---|---|---|---|---|
| `SQL/166` | 1.99.53 | Schema | beide | Schema | automatisch | `166_AddStockMovementWarehouseRequisitionItem.sql`: Spalte `StockMovements.WarehouseRequisitionItemId` (NULL) + FK + Index, MigrationId der neuen Migration (Reihenfolge nach 164/165) |
| `Einstellung/LagerbestellungVerbrauchsbuchungAktiv` | 1.99.53 | Einstellung | beide | Nachher | anzeigen | „Lagerbestellung: Lieferung ausbuchen“ ist nach dem Update aus (Default). Einschalten unter /Settings entscheidet Gerald nach dem Deploy, je Installation. Am Einschalttag endet die Handbuchung dieser Lieferungen in Sage (sonst doppelte Entnahme). |
| `Info/Lagerbestellung-Entnahmeplatz` | 1.99.53 | Info | beide | Nachher | anzeigen | Nur bei eingeschaltetem Schalter: Lager wählt je Position „Entnahme aus“ (Vorschlag wie Kommissionierung: Hauptlagerplatz mit Bestand, sonst Platz mit dem meisten Bestand); Abschließen bucht die Ist-Menge als Entnahme aus. **Ab dem Einschalttag diese Lieferungen in Sage nicht mehr von Hand ausbuchen** – das WMS übergibt die Entnahme (sonst doppelte Entnahme). |

- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Standort:** DEV AKE (mit Sage-Test, 178.6) und DEV IDEAL (178.11).
- **Nummern:** Version 1.99.53, TS-178, SQL/166 – alle im Dev-Lauf neu prüfen. Fallstrick (B11: WMS-Ausbuchung auf Sage-Platz ohne Sage-Übergabe
  wird vom Lagerbestand-Abgleich zurückgebucht) als nächste freie Nummer (60 Favoriten, 61 WE vorgesehen → 62). Kein ADR nötig.

## Offene Rueckfragen

> [!success] Alle zehn Rückfragen am 2026-11-23 beantwortet (Block „Freigabe-Antworten“) und in den Body eingearbeitet. Die Fragen unten
> bleiben als Bezug der Antwort-Nummern stehen; ihre Empfehlungen zu 1, 3 und 9 (Vorschlagsregel, Mengenformulierung, Standort-Sonderlogik) sind durch die Antworten überholt. Keine neuen Rückfragen.

1. **(Fachbereich: Lager) Was heißt „Lager auswählen“?** *Empfehlung:* Entnahme-**Lagerplatz** je Position (der Lagerort ergibt sich aus dem
   Platz, `StorageLocation.StorageAreaId`), vorbelegt mit dem Hauptlagerplatz; kein eigenes Lagerort-Feld und keine Auswahl durch den Besteller.
2. **(Fachbereich: Lager/Buchhaltung) Verbrauch = nur Abgang?** *Empfehlung:* ja – die Ware liegt im Lager und geht an die Werkbank: eine
   Ausbuchung vom gewählten Platz, an Sage als Entnahme. Zugang + sofortiger Abgang wäre nur sinnvoll, wenn Ware am Lager vorbei direkt
   vom Lieferanten an die Werkbank geht; das ist der Wareneingang, nicht die Lagerbestellung.
3. **(Fachbereich: Lager) Zeitpunkt und Menge.** *Empfehlung:* beim Abschließen bzw. Drucken+Abschließen, Menge = Ist minus schon gebucht;
   Überlieferung bucht das volle Ist; Ist unter schon gebucht senken wird abgewiesen (Korrektur über Wareneingang/Umbuchung).
4. **Bestand reicht nicht.** *Empfehlung:* dieselbe Regel wie Warenausgang (`NegativeBuchungErlaubt` → Ausweichplatz, sonst Zeile abweisen),
   einmal gebaut für beide.
5. **(Fachbereich: Buchhaltung) Kostenstelle.** `LagerbuchungService` hat kein Kostenstellen-Feld (B6); die Werkbank hat eine Sage-Kostenstelle
   (B12). *Empfehlung:* in diesem Schritt keine Kostenstelle an Sage; Werkbank und Bestellnummer stehen in der WMS-Notiz. Optional später die
   Kostenstelle in `Referenz` der Sage-Buchung (Freitext, Wirkung in Sage unbelegt).
6. **(Fachbereich: Lager/Sage) Doppelte Entnahme und Sage-Bewegungsart.** Bucht das Lager diese Lieferungen heute in Sage von Hand aus (S-3)?
   *Empfehlung:* ab Einschalten nicht mehr – das WMS übernimmt. Und: Ist „Entnahme“ (wie Warenausgang) richtig, oder braucht Sage eine eigene
   Verbrauchs-Bewegungsart (S-1, S-2)? Bis zur Antwort bleibt der Schalter aus.
7. **Glas und DUMMY.** *Empfehlung:* Glasbestellungen wie Lagerbestellungen buchen (gleiche Seite, gleicher Ablauf); DUMMY-Positionen und
   Positionen ohne Artikel nie buchen, nur Hinweis.
8. **Rolle.** *Empfehlung:* keine neue Rolle; buchen darf, wer die Lager-Detailseite bearbeiten darf (admin, stock, stock_keyuser).
9. **Standort und Einschalten.** *Empfehlung:* Schalter je Standort, Default aus; AKE nach Klärung von 6 mit Sage-Übergabe; IDEAL nur
   WMS-Buchung, solange dort kein SData-Endpunkt eingetragen ist. Wann und wo einschalten?
10. **Sage-Plätze ohne Sage-Übergabe.** Einen Platz mit Quelle Sage nur zulassen bzw. vorschlagen, wenn die Entnahme auch an Sage geht
    (Schalter an + „Sage-Buchung erlaubt“ + Sage-Referenz)? *Empfehlung:* ja – sonst bucht der Lagerbestand-Abgleich (falls an) die Entnahme
    zurück (B11). Analog Rückfrage 3 der WE-Spec; reine WMS-Plätze immer erlaubt.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

<!--
  Von der Koordination eingetragen, wortgetreu, je Antwort:
  <Nr>. <Name>, <yyyy-mm-dd>, [<Jira-Schluessel> Kommentar](<Link>): "<Antwort>"
-->

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. „Lager auswählen“ = Entnahme-Lagerplatz je Position, Vorschlag Hauptlagerplatz, kein Lagerort-Feld? → **ja – „ähnlich wie bei der Kommissionierung ein Lager auswählen lassen, Hauptlagerplatz vorschlagen“ (Gerald 2026-11-23). Vorschlagsregel = gleiche Logik wie Kommissionierung (`PickingController` Quell-Vorschlag: Hauptlagerplatz mit Bestand > 0, sonst Platz mit dem meisten Bestand) – Gerald 2026-11-23; im Wareneingang dagegen immer der Hauptlagerplatz**
2. Verbrauch = nur Abgang (Ausbuchung, an Sage Entnahme)? → **ja – immer Entnahme: Lagerbestellungen gehen vom Lager an die Werkbank (Gerald)**
3. Buchen beim Abschließen, Ist minus schon gebucht, Überlieferung voll, Senken abweisen? → **Die Ist-Menge wird beim Abschließen als Entnahme gebucht (Gerald). Schutz gegen Doppelbuchung bei erneutem Abschließen bleibt technisch drin**
4. Fehlbestand: Regel wie Warenausgang (Minus-Schalter/Ausweichplatz)? → **ja – dieselbe Regel wie Warenausgang (NegativeBuchungErlaubt + Ausweich-Lagerplatz), keine eigene Einstellung (Gerald 2026-11-23)**
5. Keine Kostenstelle an Sage in diesem Schritt? → **ja, keine Kostenstelle notwendig**
6. Heute manuelle Entnahme in Sage? Entnahme richtig oder eigene Bewegungsart (S-1 bis S-3)? → **ja, heute wird von Hand in Sage ausgebucht; Entnahme ist richtig (Antwort 2). Folge: ab Einschalten bucht das WMS – die Handbuchung in Sage endet am selben Tag (sonst doppelte Entnahme)**
7. Glas wie Lager buchen, DUMMY/ohne Artikel nie? → **ja**
8. Keine neue Rolle (admin, stock, stock_keyuser)? → **ja**
9. Schalter je Standort, Default aus – wo und wann einschalten? → **allgemeine Einstellung unter /Settings (Gerald). Klarstellung: AppSettings gelten ohnehin je Installation (AKE und IDEAL haben je eine eigene) – keine Sonderlogik je Standort bauen. Einschalten entscheidet Gerald nach Deploy**
10. Sage-Plätze nur mit Sage-Übergabe zulassen/vorschlagen? → **ja**

## QA-Nachweis (qa-agent, 2026-11-23)

Bündel-QA im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Bereich `0b6fff36..a93bd910`
(vier Specs v1.99.50–v1.99.53 aus [[2026-11-23-goal-lauf-dashboard-we-lagerbestellung]]). Die QA prüft je Spec den eigenen Commit-Bereich, Build und Tests decken den ganzen Worktree ab.

### 1. Build und Tests

- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 30 Warnungen (alle bekannt, u. a. CS8620 in `BfAuftragWorkflowTests`, NU1903 Pkcs).
- `dotnet test` vor dem QA-Fix: IdealAkeWms.Tests 3472 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 790 / 0 / 0.
- Nach dem QA-Fix `a93bd910`: IdealAkeWms.Tests **3473 bestanden / 1 übersprungen / 0 Fehler**, IDEALAKEWMSService.Tests **790 / 0 / 0**.

### 6. DEV-Deploy

- `powershell -ExecutionPolicy Bypass -File D:\Dev\scripts\dev-deploy.ps1 -Standort AKE|IDEAL -Quelle <worktree>` in einer erhöhten Shell, Web und Dienst,
  zweimal (nach Build-Grün `f7b26ebc`, nach dem QA-Fix `a93bd910`). Ausgabe jeweils „HTTP 200, Fusszeile v1.99.53 … Fertig: AKE v1.99.53“ bzw. „IDEAL v1.99.53“, Exit 0.
- AKE http://localhost:8081 und IDEAL http://localhost:8082 zeigen in der Fußzeile **v1.99.53** = `AppVersion` des Branches.
- Beide DEV-DBs: letzte Migration `20261123124036_AddStockMovementWarehouseRequisitionItem`, Spalte `StockMovements.WarehouseRequisitionItemId` vorhanden.
- DEV-Sicherheit geprüft: ServiceSetting `SageLagerbuchungAktiv` = false auf AKE_WMS_DEV und IDEAL_WMS_DEV, kein `Sync:*`, keine Mail eingeschaltet.
  `SData:BaseUrl` zeigt auf AKE DEV auf sagetest01, auf IDEAL DEV auf idealsage2. Deshalb wurde keine Sage-Übergabe ausgelöst.

### 7. Publish

`<worktree>\publish\` geleert und neu erstellt: `IDEALAKEWMSWeb` + `IDEALAKEWMSWebService` (`dotnet publish -c Release`, beide Exit 0), `SQL\` → `publish\SQL\`
(inkl. `166_…`), `scripts\deploy\` → `publish\deploy\` (inkl. `Update-IdealAkeWms.ps1`), `publish-info.txt`: Version 1.99.53, Commit
`a93bd91039e22bae194422e11e253d1cd6cbab78`, Branch, vier Spec-Slugs, Erstellt 2026-11-23 14:12, Komponenten IDEALAKEWMSWeb + IDEALAKEWMSWebService.
Paket-ZIP/Shares macht der Koordinator.

### Bündel-weite geänderte Bestandstests (Punkt 4)

| Test | Einordnung |
|---|---|
| `PickingControllerTests` | nur Typ: Quell-Vorschlag nach `QuellLagerplatzVorschlag.Waehle` verschoben (Lagerbestellung) |
| `PurchaseOrdersControllerTests`, `SageWareneingaengeListeTests` | nur Typ: Ctor um `IArticleRepository` (WE) |
| `PurchaseOrdersReceiveTests` | Ctor + erwartete Meldung bewusst geändert (WE F1/AK 5, Mehrdeutigkeit) |
| `WarehousePickingControllerTests` | Ctor + `GetByIdAsync`-Setup wegen Status-Guard F4 (Lagerbestellung AK 16, bewusst) |
| `NachlaufManifestDriftTests` | + SQL/166 (Lagerbestellung) |
| `CsvExportCoverageTests` | bewusst: Sortiertext `data-filter-text` (Lagerbestellung Abweichung 6) |
| `AppSettingDefinitionsTests` | Anzahl 67 → 68 (neue Einstellung Lagerbestellung) |

Keine Änderung der Klasse „sonstiges“: Keine Fixture wurde angepasst, nur damit ein Test grün wird.
### Abweichungen aus der Umsetzung (Lauf-Notiz, von QA geprüft und akzeptiert)

1. Die Prüfreihenfolge je Position ist angepasst (Ist < gebucht → Platz fehlt/fremd → Sage-Platz-Regel → Bestand). Alle Fehler landen in einer Sammelmeldung, AK 12 bleibt erfüllt.
2. PrintAndClose sichert die Eingaben bei einem Fehler wie Close (Zwischenstand, bucht nie).
3. Der Hinweistext lautet „Lieferung wird ausgebucht. Beim Abschließen wird die Ist-Menge je Position vom Platz unter Entnahme aus ausgebucht …“.
4. Glas: Die Notiz lautet „Glasbestellung #… Pos n – <Werkbank>“.
5. Ist ist `int?`: leer heißt keine Angabe, es gibt keinen stillen Ersatz durch 0.
6. Spaltenfilter und Sortierung „Lagerplatz“ nutzen `data-filter-text` in `table-filter.js` (`cellText`), damit die Select-Optionen nicht mitgefiltert werden.
7. Der Status-Guard (F4) gilt auch bei ausgeschaltetem Schalter, eine erledigte oder stornierte Liste lässt sich nie erneut abschließen. Das ist eine bewusste Verschärfung.

### 2. Code-Review (Commit `f7b26ebc`, selbst durchgeführt)

Geprüft:
- `WarehousePickingController`: `AbschliessenAsync` für Close/PrintAndClose, Status-Guard, eine Transaktion
  (`IsRelational() ? BeginTransactionAsync : null`), Δ = Ist − bereits gebucht, `EntnahmeErlaubt`/`VorschlagHinweis`, `Eingaben`, `BestandAsync`.
- `QuellLagerplatzVorschlag.Waehle`: aus PickingController verschoben, eine Regel.
- `AusbuchungBestandsregel.PruefeAsync`: auch von `StockMovementsController.Outbound` genutzt.
- `StockMovementRepository`: Summe je Position.
- Sage-Decorator `SageBookingEnqueueingStockMovementRepository`: gleicher `ApplicationDbContext`, dadurch in der Transaktion.
- `Details.cshtml`: Zeilen ohne Select senden ein verstecktes leeres `storageLocationIds`, damit beim positionsweisen Binden keine Zeile verrutscht; der Server prüft die Array-Länge.
- `table-filter.js` und `AppSettingDefinitions`.
- Migration + `SQL/166`: Guards, DDL in eigenem Batch, History-Insert getrennt.
- `00_FreshInstall.sql` (Schema + MigrationId) und `nachlauf.psd1`.

| Nr | Befund | Behandlung |
|---|---|---|
| F-QA-1 | Die Bestandsprüfung lief je Position einzeln. Zwei Positionen mit demselben Artikel vom selben Platz bestanden die Prüfung je für sich und hätten den Platz gemeinsam ohne `NegativeBuchungErlaubt` ins Minus gebucht. | **behoben** in `a93bd910` `qa(v1.99.53)`: Bereits geplante Mengen (gleicher Artikel und Platz) zählen in `PruefeAsync` mit, die Meldung nennt den verbleibenden Bestand. Test `GleicherArtikelInZweiPositionen_BestandZaehltGemeinsam`. |
| H-1 | Abfragen je Position (Bestand, Summe gebucht): N Abfragen je Liste. Bei Listengrößen um 10–30 Positionen unkritisch. | offen als Hinweis, kein Handlungsbedarf |
| H-2 | Transaktion und RowVersion bei gleichzeitigen Abschlüssen sind InMemory nicht belegbar. | Manual-UAT TS-178.8 |

Geprüft ohne Befund:
- Keine Werte in `<script>` außer `@Url.Action`.
- Kein `hidden` zusammen mit `d-*` (die Meldung schaltet über `d-none`).
- Kein `?? 0`.
- Alle Regeln liegen serverseitig, das JS prüft nur die Vollständigkeit.
- Die Bestandsregel gibt es einmal (`AusbuchungBestandsregel`), den Quell-Vorschlag einmal (`QuellLagerplatzVorschlag`).

### 3. Nachweisart je AK

| AK | Nachweis |
|---|---|
| 1 | automatisch + DEV AKE (Schalter aus: kein Hinweis, keine Auswahl) |
| 2, 3, 4, 5 | automatisch (`WarehousePickingVerbrauchsbuchungTests`, `PickingControllerTests`); auf DEV gesehen: 3 (meister Bestand), 4 (Sage-Plätze fehlen in der Auswahl), 5 (DUMMY) |
| 6, 7, 9, 10 (kein Platz) | automatisch **und** DEV AKE (siehe 6c) |
| 8, 10 (fremder Platz), 11, 12, 13, 16, 17, 18, 20 | automatisch (InMemory). Browser-Teil (manipulierter POST, Druck, Glas): Manual-UAT |
| 14 | **Manual-UAT** (Sage-Übergabe auf DEV bewusst nicht ausgelöst) |
| 15 | **Manual-UAT** (Transaktion/RowVersion) |
| 19, 21 | Regression, Bestandscode unverändert. Manual-UAT |
| 22 | automatisch (`AppSettingDefinitionsTests`) + DEV /Settings: Checkbox „Lagerbestellung: Lieferung ausbuchen“, Verwendung und Warnhinweis sichtbar |

### 5. Checkliste

- Migration `20261123124036_AddStockMovementWarehouseRequisitionItem`, `SQL/166_AddStockMovementWarehouseRequisitionItem.sql` (idempotent, Guards) und
  `00_FreshInstall.sql` an beiden Stellen: ja.
- `SQL/AgentJobs`: nicht betroffen, die neue Spalte ist nullable.
- Audit: `StockMovement` mit `CreatedBy`/`CreatedByWindows` (auf DEV „Gerald Weichbold“).
- Version 1.99.53 in beiden `AppVersion.cs`, Changelog v1.99.53, TS-178, Testindex Zeile 178.
- Manifest: `SQL/166`, `Einstellung/LagerbestellungVerbrauchsbuchungAktiv` und `Info/Lagerbestellung-Entnahmeplatz` sind eingetragen.
- **ADR 0020** für `LagerbestellungVerbrauchsbuchungAktiv`: Auswahl an/aus (`B(...)`), Server-Validierung über den Bool-Typ, Anzeigename, Wirkung,
  Verwendung, Default false, Standort beide, Warnhinweis vorhanden, abhängig von `LagerbestellungAktiv`. Das Feld „Verwendung“ von `NegativeBuchungErlaubt` ist ergänzt.
- Brain-Nachzug (fallstricke §62, controller.md, services.md, datenmodell.md, feature-map, Changelog) macht der Koordinator.

### 6c. Testlauf auf DEV

Der Schalter `LagerbestellungVerbrauchsbuchungAktiv` war nur auf DEV vorübergehend an. Danach wurde die Zeile wieder gelöscht, es gilt der Default aus.

Testbestand per SQL (Notiz „QA TS-178 Testbestand“):
- AKE: Artikel 750 +10 auf `1;1;1` und +20 auf `Test`; Artikel 3453 +10 auf `1;1;1`; Position 80 auf bestellt 5 gesetzt.
- IDEAL: Testliste #1 (QA-Agent) mit Artikel 101001, +10 auf `NAN`.

| Schritt | Ergebnis | Status |
|---|---|---|
| AKE #57 öffnen | blauer Hinweis; Pos 1 „Vorschlag: Platz mit dem meisten Bestand“ mit Bestandsliste; DUMMY „wird nicht gebucht (kein Lagerartikel)“; Sage-Plätze (`LL;1;14;0`, `HR;1;2;4`) nicht in der Auswahl | OK |
| #57 Ist 3, Platz „— wählen —“, abschließen | „Nicht abgeschlossen – nichts gebucht: Pos 1: kein Entnahme-…“, Status bleibt Abgeschickt, Ist gesichert (3/1), 0 Bewegungen | OK |
| #57 mit Platz `Test` abschließen | Status Erledigt (3); genau eine Ausbuchung 3 von `Test`, `WarehouseRequisitionItemId` 78, Notiz „Lagerbestellung #57 Pos 1 - A1“, Benutzer Gerald Weichbold; DUMMY ohne Bewegung; kein Queue-Eintrag | OK |
| AKE #53 Ist 3 + Fehlteil | Status Teilgeliefert (5), Ausbuchung 3 von `1;1;1` | OK |
| #53 erneut öffnen | „bereits ausgebucht“ unter dem Vorschlag, Bestand `1;1;1 (7,000)` | OK |
| #53 Ist auf 2 senken | „Nicht abgeschlossen – nichts gebucht: Pos 1: Ist 2 kleiner …“, keine Bewegung | OK |
| #53 Ist 5 | Status Erledigt; zweite Ausbuchung **2** (Differenz); Meldung „Liste #53 abgeschlossen. 1 Position(en) ausgebucht: Pos 1 2 …“ | OK |
| IDEAL #1 Ist 2 (178.11) | Status Erledigt, Ausbuchung 2 von `NAN`, kein Queue-Eintrag (`SageLagerbuchungAktiv` aus) | OK |
| Schalter aus (AKE #54) | kein Hinweis, keine Auswahl „Entnahme aus“ | OK |
| 178.6 Sage-Entnahme (AKE-Test) | `SData:BaseUrl` zeigt auf eine echte Sage-Instanz, deshalb wurde der Sage-Teil bewusst nicht ausgelöst | NICHT MOEGLICH, Manual-UAT |
| 178.7 manipulierter POST, 178.9 Drucken und Abschließen (öffnet einen Tab, `alert` im Fehlerpfad), 178.10 Glas, `NegativeBuchungErlaubt` | nicht im Browser ausgeführt (automatisch abgedeckt) | NICHT MOEGLICH, Manual-UAT |
| 178.8 gleichzeitiges Abschließen | — | Manual-UAT |

### Deploy (final, aus dem echten Diff)

- **Komponenten:**
  - Web: ja.
  - Dienst: ja, wegen der Versionsnummer **und** des gemeinsam genutzten Models `StockMovement` (das EF-Model muss zum Schema passen).
  - Migration: **ja**, `20261123124036_AddStockMovementWarehouseRequisitionItem` / `SQL/166`, additiv (nullable Spalte + FK + Index).
- **Reihenfolge:**
  1. DB-Backup (Standard).
  2. Dienst und Web stoppen.
  3. `SQL/166` ausführen (bzw. das Update-Skript über das Manifest).
  4. Web starten.
  5. Dienst starten.

  Der Schalter bleibt aus. Das Einschalten entscheidet Gerald je Installation. Am Einschalttag endet die Handbuchung in Sage.
- **Paket:** `<worktree>\publish` mit **v1.99.53**, Commit `a93bd910`, Web + Dienst + `SQL\` + `deploy\`.
- **Ablauf:** Publish aus dem Worktree → Testsystem → testen → Merge. Nach dem Merge nur dann neu aus main publishen, wenn der Merge getestete
  Dateien mit parallelen main-Änderungen zusammengeführt hat.

### Manuelle Testcheckliste (offen für den Menschen)

1. Auf dem Testsystem: Das Update-Skript führt `SQL/166` aus, die Fußzeile zeigt v1.99.53, und der Schalter ist aus (178.1).
2. TS-178.2–178.5 mit den Testartikeln (a)–(d), dazu `NegativeBuchungErlaubt` an und aus und den Warenausgang als Regression.
3. TS-178.6 auf dem AKE-Test mit Sage-Test:
   - `SageLagerbuchungAktiv` an.
   - Platz mit „Sage-Buchung erlaubt“.
   - Queue → Bestätigt → Entnahme mit `SM#…#` in Sage (AK 14).
4. TS-178.7: manipulierter POST mit einem Sage-Platz ohne Sage-Buchung (DevTools).
5. **TS-178.8:** zwei Browser schließen gleichzeitig ab. Es entsteht nur eine Ausbuchung, der zweite bekommt „inzwischen geaendert“.
6. TS-178.9 „Drucken und Abschließen“ mit Fehlerfall und Gutfall, dazu TS-178.10 Glasbestellung.
7. Regressionen:
   - Bestellvorschläge: Der Bedarf aus Lagerbestellungen bleibt unverändert (AK 19).
   - Ein Benutzer nur mit `lagerbestellung`/`picking` erreicht die Detailseite nicht (AK 21).
8. Vor dem produktiven Einschalten: Die Handbuchung in Sage endet, und der Lager-Hinweis aus dem Manifest ist kommuniziert.
