---
type: spec
title: Sage-Buchungen – eine Gesamtsicht über alle Übergabewege
slug: 2026-11-23-sage-buchungen-gesamtsicht-spec
status: Entwurf
created: 2026-11-23
updated: 2026-11-23
source_backlog: "[[2026-11-23-sage-buchungen-gesamtsicht]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.63 (Worktree IdealAkeWms/AppVersion.cs:5 = 1.99.61, nicht 1.99.53 wie im Auftrag; 1.99.62 ist an [[2026-11-23-tabellen-streifen-hover-ci-hellblau-spec]] vergeben, 1.99.48/1.99.49 in Entwürfen reserviert. Dev-Lauf prüft erneut)"
testszenario_vorschlag: "TS-188 (docs/TESTSZENARIEN.md des Worktrees endet bei TS-186, TS-187 ist an die Tabellen-Spec vergeben, TS-171/172 in Entwürfen reserviert)"
affected_code:
  - IdealAkeWms/Services/SageBuchungenGesamtsicht.cs (neu, reine Funktion – Zeile, Anzeige-Status, Herkunft, Zuordnung je Weg)
  - IdealAkeWms/Controllers/SageBookingQueueController.cs:64-93 (Index lädt drei Quellen, Union, Spaltenfilter, Paging) – Variante A
  - IdealAkeWms/Models/ViewModels/SageBookingQueueViewModel.cs (Zeilen statt Items, Zeitraum, Anzeige-Filter) – Variante A
  - IdealAkeWms/Views/SageBookingQueue/Index.cshtml (Spalten Herkunft/Bezug/Status im Weg, Filterkarte Zeitraum + Anzeige-Status, Link zur Fachseite) – Variante A
  - IdealAkeWms/Data/Repositories/SageBookingQueueRepository.cs:77-88 + ISageBookingQueueRepository (GetForMonitoringAsync mit Zeitraum, Include PurchaseOrderReceipt)
  - IdealAkeWms/Data/Repositories/PurchaseOrderReceiptRepository.cs (neue Lesemethode für die Gesamtsicht)
  - IdealAkeWms/Data/Repositories/SageFaArbeitsgangAnlageRepository.cs (neue Lesemethode für die Gesamtsicht)
  - IdealAkeWms/Services/ColumnFilterSyntax.cs:70 (Register SageBookingQueue/Index um neue Spalten-Typen)
  - IdealAkeWms/Services/Navigation/NavigationKatalog.cs:88 (nur Titel, falls Rückfrage 6 = umbenennen; Variante B: neues NavZiel + Regel)
  - IdealAkeWms/Views/Shared/_Layout.cshtml:98-102 (Linktext, falls umbenannt; Variante B: neuer Link)
  - IdealAkeWms.Tests/Services/SageBuchungenGesamtsichtTests.cs (neu)
  - IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs (1.99.63), IdealAkeWms/Views/Help/Changelog.cshtml
  - docs/TESTSZENARIEN.md (TS-188), docs/anwender/09-einbuchen-ausbuchen-umbuchen.md, docs/anwender/18-startseite-und-menue.md
open_questions:
  - "1. Welche Maske ist mit „Sage-Buchung“ gemeint: „Sage-Lagerbuchungen“ (Lager-Menü, SageBookingQueue) oder „Sage-Wareneingänge“? Empfehlung: Sage-Lagerbuchungen – dort fehlen heute genau die Wareneingänge mit Sage-WE-Beleg."
  - "2. Variante A (Seite „Sage-Lagerbuchungen“ wird zur Gesamtsicht, Spalte Herkunft, Zeilen anderer Wege nur lesend mit Link) oder B (neue Seite „Sage-Übergaben“ als Union, bestehende Seiten bleiben unverändert)? Empfehlung A."
  - "3. FA-Arbeitsgänge (IDEAL, „Arbeitsgänge → Sage“) einbeziehen? Empfehlung ja, sichtbar nur wie die Fachseite (Admin + ProduktionsauftragHierarchisch). Vorgemerkte Stammdaten-Übergaben (SageStammdatenOutbox, es wird nichts gesendet) bleiben draußen."
  - "4. Rollen: Zeilen eines Weges nur zeigen, wenn der Benutzer dessen Fachseite sehen darf (Lagerbuchung = stock_read-Kreis, Wareneingang = zusätzlich Modul WareneingangBestellungAktiv, FA-Arbeitsgänge = Admin + hierarchisch)? Empfehlung ja (eine Regel: NavigationSicht der Fachseite)."
  - "5. Standard-Zeitraum: „Erstellt ab“ = heute − 30 Tage; nicht erledigte Zeilen (wartet, gesendet, Fehler, Klären) immer unabhängig vom Zeitraum; „von Hand in Sage“ zählt als erledigt. Einverstanden, oder anderer Zeitraum?"
  - "6. Nur Variante A: Seite und Menüpunkt in „Sage-Buchungen“ umbenennen (Route, Menü-Key und Favoriten bleiben)? Empfehlung ja."
  - "7. Wareneingänge „nur in Sage buchen“ (Chargen-/Nachweispflicht, das WMS sendet nichts) mit anzeigen, als Status „von Hand in Sage“? Empfehlung ja – sie sind Sage-relevant und stehen heute auf „Sage-Wareneingänge“."
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Gerald will an **einer** Stelle sehen, was das WMS an Sage übergibt und wo es hängt – heute muss er dafür drei Seiten kennen, und eine
davon (FA-Arbeitsgänge) zeigt Warteschlangen-Zeilen nur, solange der FA noch in der Struktur steht. Die Gesamtsicht ist **nur lesend**:
Sie bündelt den Stand, die Reparatur (Erneut senden, Klären, Bestätigen) bleibt auf der jeweiligen Fachseite mit ihren Rechten und
Sage-Prüfungen.

## Umfang (In-Scope / Out-of-Scope)

### Ist-Stand: alle Schreibwege WMS → Sage (gegengeprüft im Bündel-Worktree, Stand v1.99.61)

Gesucht über `HttpMethod.*`, `new HttpRequestMessage(`, `PostAsync|PutAsync|PostAsJson|PutAsJson|PatchAsync`, SQL-Schreibbefehle auf
`KHK*` und `ExecuteNonQuery`/`ExecuteSql*` in Web und Dienst. Ergebnis: **genau drei SData-POST-Stellen, alle im Dienst, alle aus
`SageBookingWorker`**. Die Web-App schreibt nicht an Sage.

| Weg | POST (Client) | Aufruf im Dienst | Ablage (WMS-DB) | Status-Enum | Anzeige heute | Recht Anzeige / Aktion |
|---|---|---|---|---|---|---|
| Lagerbuchung (Zugang/Entnahme/Umbuchung) | `SageLagerbuchungClient.cs:45` | `SageBookingWorker.cs:443`, Protokoll `SyncLogServices.SageLagerbuchung` (`:349`) | `SageBookingQueueItems` (FK `StockMovementId`), angelegt vom Decorator `SageBookingEnqueueingStockMovementRepository.cs:65-69` und vom Nacherfassungs-Lauf `SageBookingQueueRepository.cs:140-187` | `SageBookingQueueStatus` Offen/Gesendet/Bestaetigt/Fehler (`SageBookingQueueStatus.cs:11-17`) | „Sage-Lagerbuchungen“ `SageBookingQueueController.Index` (`:64-93`), alle Einträge ohne Zeitraum (`SageBookingQueueRepository.cs:77-88`) | Klasse `[RequireStockReadAccess]` (`:15`); „Erneut senden“ `[RequireStockKeyUserAccess]` (`:97`), Knopf über `CanTransferStockAsync` (`Index.cshtml:6`) |
| Sage-Wareneingangsbeleg (ELL) | `SageWareneingangClient.cs:47` | `SageWareneingangService.cs:151`, gestartet `SageBookingWorker.cs:210`, Protokoll `SyncLogServices.SageWareneingang` (`:197`, `:225`) | `PurchaseOrderReceipts.SageWeStatus` + `SageWe*`-Felder (`PurchaseOrderReceipt.cs:51-62`), ein Beleg je `SageWeVorgang` | `SageWareneingangStatus` NichtNoetig/Offen/Gesendet/Uebergeben/Fehler/NurInSage/Klaeren (`PurchaseOrderReceipt.cs:15-26`) | „Sage-Wareneingänge“ `PurchaseOrdersController.SageWe.cs:61-117` (ohne `NichtNoetig`, Repository `PurchaseOrderReceiptRepository.cs:92`), Einkauf-Variante „(offen)“ `EinkaufPurchaseOrdersController.cs:29` | Klasse `[RequireStockReadAccess]` + `[RequireWareneingangBestellungAktiv]` (`PurchaseOrdersController.cs:17-18`); Erneut senden / Als übergeben / CSV `[RequireStockKeyUserAccess]` (`SageWe.cs:121-167`) |
| FA-Arbeitsgänge anlegen (nur IDEAL) | `SageFaArbeitsgangClient.cs:84` | `SageFaArbeitsgangService.cs:157`, gestartet `SageBookingWorker.cs:288`, Protokoll `SyncLogServices.SageFaArbeitsgaenge` (`:274`) | `SageFaArbeitsgangAnlagen` (`SageFaArbeitsgangAnlage.cs:21-40`), eindeutig je (BelId, Sage-AG) | `SageFaArbeitsgangStatus` Offen/Gesendet/Angelegt/InSageVorhanden/Fehler (`SageFaArbeitsgangAnlage.cs:4-14`); „unklar“ = Fehler mit Text-Präfix (`SageFaArbeitsgaenge.cs:40-44`) | **keine eigene Warteschlangen-Liste**: „Arbeitsgänge → Sage“ (`SageFaArbeitsgaengeController.Index`) ist eine Vorschau FA × Kürzel aus der aktiven FA-Struktur (`:77-79`, `VerschwundenSeit == null`), die den Warteschlangen-Status nur über diese Knoten einblendet (`:119-120`, Repository nur `GetByBelIdsAsync` `SageFaArbeitsgangAnlageRepository.cs:25-26`) – Zeilen verschwundener FAs sieht man nirgends | Klasse `[RequireAdminAccess]` (`:18`) + Gate `ProduktionsauftragHierarchisch` (`:67-71`); Anlegen/Bestätigen ebenfalls Admin (`:151`, `:192`) |

Belegte Nicht-Wege (damit niemand sie später als fehlend sucht):

- **Vorgemerkte Stammdaten-Übergaben** (Lagerplätze, BDE-Mitarbeiter) in `SageStammdatenOutbox` werden **nicht gesendet**: kein Worker,
  kein Client (`SageStammdatenOutbox.cs:9-10`, `SageStammdatenOutboxItem.cs:6-11`; einzige Schreiber `StorageLocationRepository.cs:22`,
  `BdeOperatorRepository.cs:32/44`, `LagerplatzSyncService.cs:183` – nur Vormerken/Erledigen in der WMS-DB).
- **Beschichtungsauftrag, BF-Auftrag, Kreditoren, Sage-Benutzerfelder** schreiben nicht an Sage: Kein `HttpMethod.Post/Put/Patch` außer den
  drei Clients; die Web-SData-Zugriffe sind GET (`SageSdataReadClient.cs:97`, `:248`, `:280`; `ServiceSettingsController.cs:199`). BF und
  Beschichtung erzeugen Mails/CSV (`BfAuftragEmailService.cs:59`), keine Sage-Buchung.
- **Kein SQL-Schreiben in die Sage-DB:** keine Treffer für `UPDATE|INSERT INTO|MERGE … KHK*`; die `ExecuteNonQuery`-Stellen im
  Sage-Import laufen auf der WMS-Verbindung (`SageImportService.cs:137`, `:218`, `:306`, `:343`).
- **Kommissionierung geht nie an Sage:** Kommissionier-Transfers sind Umbuchungen mit FA-Text (`PickingTransferService.cs:216-218`), und
  Umbuchungen mit FA-Bezug schließen Decorator und Nacherfassung aus (`SageBookingEnqueueDecision.cs:74-75`,
  `SageBookingQueueRepository.cs:152`). Die Backlog-Idee „Herkunft Kommissionierung“ entfällt damit.
- **Wareneingang ohne Sage-WE** (`SageWeStatus = NichtNoetig`) steht schon heute in „Sage-Lagerbuchungen“ (als Einbuchung in der Queue);
  mit Sage-WE ist er bewusst ausgeschlossen (`SageBookingEnqueueDecision.cs:70`, `SageBookingQueueRepository.cs:154`). Die
  Backlog-Aussage dazu stimmt.

**In-Scope**
- Eine lesende Liste über die drei Wege (Zeile = ein Queue-Eintrag, ein Wareneingang mit `SageWeStatus <> NichtNoetig`, eine AG-Anlage).
- Gemeinsamer Anzeige-Status (nur Zuordnung, die Enums bleiben unverändert).
- Spalte „Herkunft“ aus vorhandenen Feldern abgeleitet, Spalte „Bezug“, Link zur Fachseite.
- Listen-View-Pflichten (ADR 0005), Rechte je Weg (ADR 0006), Navigation über `NavigationKatalog` (ADR 0034).

**Out-of-Scope**
- Neue Schreibaktionen in der Gesamtsicht für Wareneingang und FA-Arbeitsgänge (Erneut senden, Als übergeben, Bestätigen, CSV bleiben auf
  ihren Seiten). Bei Variante A bleibt „Erneut senden“ für Lagerbuchungs-Zeilen, weil die Seite deren Fachseite ist.
- `SageStammdatenOutbox` (sendet nichts; aufnehmen erst, wenn es einen Sende-Worker gibt).
- Umbau der Status-Enums, Umbau der Fachseiten (Variante B lässt sie unverändert; Variante A ändert nur die Lagerbuchungs-Seite).
- Live-Lesen in Sage (die Gesamtsicht zeigt nur den WMS-Stand der Warteschlangen).

## Fachliche Anforderungen

**F1 Quellen und Zeilen.** Die Liste zeigt je Weg:
- Lagerbuchung: jeden `SageBookingQueueItem`.
- Sage-Wareneingang: jeden `PurchaseOrderReceipt` mit `SageWeStatus <> NichtNoetig` (wie `PurchaseOrderReceiptRepository.cs:92`), eine Zeile je
  Position (wie die Fachseite; ein ELL-Beleg umfasst alle Positionen eines `SageWeVorgang`).
- FA-Arbeitsgang: jede `SageFaArbeitsgangAnlage`, **auch** wenn der FA nicht mehr in der aktiven Struktur steht.

**F2 Anzeige-Status** (Auswahlliste im Filter; die Spalte „Status im Weg“ zeigt zusätzlich den Originaltext der Fachseite):

| Anzeige | Lagerbuchung (`SageBookingQueueStatus`) | Wareneingang (`SageWareneingangStatus`) | FA-Arbeitsgang (`SageFaArbeitsgangStatus`) | erledigt? |
|---|---|---|---|---|
| wartet | Offen | Offen | Offen | nein |
| gesendet (Antwort offen) | Gesendet | Gesendet | Gesendet | nein |
| übergeben | Bestaetigt | Uebergeben | Angelegt, InSageVorhanden | ja |
| Fehler | Fehler | Fehler | Fehler (ohne „Antwort unklar“) | nein |
| klären | – | Klaeren | Fehler mit `SageFaArbeitsgaenge.IstUnklar` (`SageFaArbeitsgaenge.cs:43`) | nein |
| von Hand in Sage | – | NurInSage | – | ja (siehe Rückfrage 5/7) |

Originaltexte: Lagerbuchung `SageBookingQueueController.StatusText` (`:32-39`), Wareneingang `PurchaseOrdersController.SageWeStatusText`
(`SageWe.cs:17-27`), FA-Arbeitsgang die Texte aus `SageFaArbeitsgaenge.QueueStatus` (`:126-139`). Badge-Farben wie heute
(`Index.cshtml:71-77`), „klären“ gelb mit dunkler Schrift; der Text steht immer im Badge (Farbe nie allein).

**F3 Herkunft** (aus gespeicherten Feldern, Reihenfolge = Vorrang, nicht aus Namen geraten):

| Herkunft | Regel | Beleg |
|---|---|---|
| Wareneingang (Sage-WE) | Zeile aus `PurchaseOrderReceipts` | F1 |
| Wareneingang zu Bestellung | Queue-Zeile, `StockMovement.PurchaseOrderReceipt != null` (dann `NichtNoetig`) | `StockMovement.cs:59`, `PurchaseOrderReceipt.cs:36` |
| Lagerbestellung | Queue-Zeile, `StockMovement.WarehouseRequisitionItemId != null` | `StockMovement.cs:64` (v1.99.53) |
| Einbuchung / Ausbuchung / Umbuchung | Queue-Zeile, sonst nach `MovementType`; Ausbuchung mit FA-Text als „Ausbuchung (FA)“ | `StockMovement.cs:22`, `:31` |
| FA-Arbeitsgang | Zeile aus `SageFaArbeitsgangAnlagen` | F1 |

Ob eine Ein-/Ausbuchung aus der Einzel-, Mehrfach- oder Lagerplatz-Maske kam, speichert das WMS nicht – die Herkunft wird darum nicht
feiner. Eine nachträglich eingereihte Buchung (`CreatedBy = system:sage-booking`, `SageBookingQueueRepository.cs:12`, `:169`) bekommt den
Zusatz „nacherfasst“ in der Spalte „Erfasst von“ (der Wert steht schon dort).

**F4 Spalten:** Herkunft, Erstellt, Status (Anzeige), Status im Weg, Bezug (Artikel bzw. „Bestellung <Belegnummer> Pos <BelPosId>“ bzw.
„FA <BelId> AG <SageArbeitsgangnummer> (<Kürzel>)“), Menge, Lagerplatz, Sage-Beleg/Kennung (Queue `SageKennung`, WE `SageWeBelegnummer`
sonst `SageWeKennung`, FA leer), Übergeben am (Queue `ConfirmedAt`, WE `SageWeAm`, FA `SageAm`), Versuche (`AttemptCount` /
`SageWeVersuche` / `Versuche`), Fehler (`LastError` / `SageWeFehler` / `Fehler`), Erfasst von (`CreatedBy`), Aktion.
Leere Zellen bleiben leer (FA-Zeilen haben keine Menge/keinen Platz).

**F5 Aktion je Zeile:** Link „zur Fachseite“:
- Wareneingang → `PurchaseOrders/SageWareneingaenge?bestellung=<Belegnummer>` (Parameter vorhanden, `SageWe.cs:61`).
- FA-Arbeitsgang → `SageFaArbeitsgaenge/Index?fa=<BelId>` (`SageFaArbeitsgaengeController.cs:64`); steht der FA nicht mehr in der Struktur,
  Hinweis „FA nicht mehr in der FA-Struktur“ statt eines Links ins Leere.
- Lagerbuchung → Variante A: unverändert „Erneut senden“ bei Fehler (gleiche Rechte wie heute); Variante B: Link
  `SageBookingQueue/Index?colf_id=<Id>` (Spaltenfilter-Präfix `ColumnFilterHelper.cs:12`).

Keine weitere Schreibaktion.

**F6 Rechte je Weg** (Rückfrage 4): Zeilen eines Weges erscheinen nur, wenn `NavigationSicht` die Fachseite zeigt –
Lagerbuchung `NavZiele.SageLagerbuchungen` (stock_read-Kreis, `NavigationKatalog.cs:255`), Wareneingang `SageWareneingaengeLager`
(`WareneingangBestellungAktiv` && stock_read, `:258`), FA-Arbeitsgang `ArbeitsgaengeSage` (Admin && hierarchisch, `:287`). Damit sieht
niemand in der Gesamtsicht mehr als auf den Fachseiten und kein Link führt auf 403.

**F7 Zeitraum und Filter:** Filterkarte mit „Erstellt ab“ (Datum, Default nach Rückfrage 5: heute − 30 Tage), „Erstellt bis“ (leer) und
Anzeige-Status (Auswahlliste F2, „Alle“). Nicht erledigte Zeilen (F2) erscheinen unabhängig vom Zeitraum; darüber ein Hinweis
„n nicht erledigte Übergaben, davon m älter als der Zeitraum“, wenn m > 0 (melden statt still).

## Technischer Loesungsentwurf

### Gemeinsamer Kern (beide Varianten)

`IdealAkeWms/Services/SageBuchungenGesamtsicht.cs` (statisch, DB-frei, wie `SageFaArbeitsgaenge.BuildVorschau`):
- `enum SageBuchungAnzeige { Wartet, Gesendet, Uebergeben, Fehler, Klaeren, VonHandInSage }` – nur Anzeige, keine DB-Spalte.
- `record Zeile(...)` mit den Feldern aus F4, dazu `Weg`, `Herkunft`, `Anzeige`, `IstErledigt`, Link-Ziel.
- `Anzeige(SageBookingQueueStatus)`, `Anzeige(SageWareneingangStatus)`, `Anzeige(SageFaArbeitsgangAnlage)` – drei kleine `switch` nach F2.
- `Herkunft(StockMovement)` nach F3.
- `Build(queue, receipts, anlagen, kuerzel, sichtbar: (lager, we, fa))` → `List<Zeile>`, sortiert nach Erstellt absteigend.

Datenzugriff nur über Repositories (ADR 0001), je Quelle **eine** Abfrage mit derselben Zeitraum-Regel
`CreatedAt >= ab || !erledigt` (F7). Nicht berechtigte Quellen werden gar nicht gelesen:
- `ISageBookingQueueRepository.GetForMonitoringAsync(DateTime? ab)`: wie heute (`SageBookingQueueRepository.cs:77-88`), zusätzlich
  `Include(StockMovement.PurchaseOrderReceipt)` und `SourceStorageLocation`. Der bisherige Status-Parameter entfällt (Filter läuft über
  die Anzeige); einziger Aufrufer ist `SageBookingQueueController.cs:71`.
- `IPurchaseOrderReceiptRepository.GetFuerGesamtsichtAsync(DateTime? ab)`: `SageWeStatus <> NichtNoetig`, Includes wie
  `GetSageWareneingaengeAsync` (`:89-92`).
- `ISageFaArbeitsgangAnlageRepository.GetFuerGesamtsichtAsync(DateTime? ab)`: `AsNoTracking`, keine Struktur-Abhängigkeit. Für den Hinweis
  „FA nicht mehr in der FA-Struktur“ eine `SubFA`-Menge aus `IFaHierarchyNodeRepository.GetAllAsync()` (wie
  `SageFaArbeitsgaengeController.cs:77`), nur wenn FA-Zeilen sichtbar sind.

Memo-Kennung der WE-Zeilen über `SageWareneingang.Kennung` wie die Fachseite (`SageWe.cs:32`, `:77-79`), nicht neu berechnen.

Listen-View-Pattern (ADR 0005, Referenz `ProductionOrdersController.Index`):
- `PageSize.Resolve` + `PaginationState` + `_Pagination`, Seitenparameter heißt `page`, State als Model-Property (Fallstrick 50:
  `PaginationSeiteKlemmenFilter` greift nur so).
- Server-Spaltenfilter `data-server-column-filter="true"`, jedes `<th>` mit `data-col-key`, `ColumnFilterHelper.ReadFromQuery` →
  `ColumnFilterHelper.Apply` über die **ganze Union**, danach `Skip/Take`. ColumnMap liefert den gerenderten Zelltext (Datum
  `dd.MM.yyyy HH:mm` wie `SageBookingQueueController.cs:57-59`).
- Typen im Register `ColumnFilterSyntax.cs` (Zahl: Menge, Versuche; Datum: Erstellt, Übergeben am) – P-2 (Kollation je Spalte) entfällt, weil
  in C# gefiltert wird.
- Filterkarte wie die bestehenden Listen (GET-Formular, Zeitraum + Anzeige-Status, „Zurücksetzen“).

### Variante A – „Sage-Lagerbuchungen“ wird die Gesamtsicht (Empfehlung)

- `SageBookingQueueController.Index(SageBuchungAnzeige? anzeige, DateTime? ab, DateTime? bis, int page, int? pageSize)` lädt die drei
  Quellen (F6 über `NavigationSicht.GetAsync()`, scoped, schon in DI), baut die Union und filtert/paged wie oben. `Requeue` (`:95-115`)
  bleibt unverändert.
- View: neue Spalten Herkunft, Status im Weg, Bezug, Sage-Beleg, Erfasst von; Spalte „Bewegung“ entfällt in „Herkunft“; Fehler-/Antwort-Detail
  wie heute (`Index.cshtml:89-106`). Einleitungstext nennt die drei Wege und dass Wareneingang/FA nur auf ihrer Seite repariert werden.
- Navigation: kein neues `NavZiel`, Regel unverändert (`NavigationKatalog.cs:255`). Nur bei Rückfrage 6 = ja: `Titel` von
  `NavZiele.SageLagerbuchungen` auf „Sage-Buchungen“ (`:88`) und Linktext `_Layout.cshtml:101`; Key, Route und Favoriten bleiben.
- TLS-Banner (`Index.cshtml:16-29`) bleibt.
- Alte Lesezeichen `?status=Fehler` binden auf `anzeige` nicht mehr → Liste „Alle“ (kein Fehler). Bewusst kein Altparameter (YAGNI).

Für A spricht (Ponytail-Leiter, Sprosse 2):
- Geralds Wort „Maske Sage-Buchung“ zeigt auf eine bestehende Seite.
- Kein neuer Controller, keine neue Route, kein neues `NavZiel`, kein neuer Menüeintrag.
- Lagerbuchungs-Zeilen behalten „Erneut senden“ am selben Ort.
- Die einzige Seite ohne Zeitraum (`GetForMonitoringAsync` lädt heute alles) bekommt nebenbei einen.

Dagegen spricht:
- Die Lagerbuchungs-Seite ändert ihr Aussehen; wer nur Lagerbuchungen will, filtert die Spalte Herkunft.
- Seitentitel und Inhalt passen nur mit der Umbenennung.

### Variante B – neue Seite „Sage-Übergaben“

- Neuer `SageUebergabenController` (Klasse `[RequireStockReadAccess]` **oder** Admin – der Admin ist im stock_read-Kreis enthalten,
  `CurrentUserService.cs:104-105`), Index wie oben, keine POST-Action. Neues ViewModel und neue View.
- Navigation nach Fallstrick 58:
  - `NavZiel("SageUebergaben", "Sage-Übergaben", "SageUebergaben", "Index")` ohne Kategorie (nur Menü wie die beiden Sage-Protokolle).
  - Regel `"SageUebergaben" => stockRead`.
  - Link im Lager-Block `_Layout.cshtml:97-106`.
  - Wächter `NavigationSichtTests` greifen automatisch.
- Bestehende Seiten unverändert; der Lagerbuchungs-Link geht über `?colf_id=`.

Für B: Die Fachseiten bleiben unberührt, die Gesamtsicht kann nichts kaputt machen. Dagegen: Ein weiterer Controller, eine weitere View
und ein weiterer Menüpunkt neben „Sage-Lagerbuchungen“, und die beiden Seiten zeigen dieselben Lagerbuchungen doppelt.

**Empfehlung: A.** Weniger Code und kein dritter Sage-Menüpunkt. Die Sicherheitsfrage „bleibt die Fachseite heil?“ beantwortet A ebenso, weil
die Schreibaktion `Requeue` und ihre Rechte unverändert bleiben.

## Migrations-/SQL-Auswirkungen

Keine. Alle angezeigten Felder existieren:
- `SageBookingQueueItems`, `PurchaseOrderReceipts.SageWe*`, `SageFaArbeitsgangAnlagen`.
- `StockMovements.WarehouseRequisitionItemId` seit SQL/166.

Keine neue Spalte, kein Index. `IX_SageFaArbeitsgangAnlagen_Status` (`ApplicationDbContext.cs:1905`) und der Queue-Status-Index tragen die
Regel „nicht erledigt“. Die Zeitraum-Bedingung auf `CreatedAt` läuft ohne Index; die Tabellen sind klein bzw. werden heute schon ganz
gelesen (`SageBookingQueueRepository.cs:87`). Falls die QA auf DEV eine spürbare Ladezeit misst, wird das als Befund gemeldet, nicht
vorab indiziert. Kein FreshInstall-Eintrag, kein `SQL/AgentJobs`-Bezug.

## Einstellungen

keine. Der Standard-Zeitraum (Rückfrage 5) ist eine feste Konstante im Controller (`// ponytail: feste 30 Tage, Einstellung erst bei
Bedarf`), keine AppSetting.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine: Die Seite schreibt nichts. Bei A bleibt `Requeue` unverändert (`SageBookingQueueRepository.RequeueAsync` setzt `ModifiedAt/By/ByWindows`,
`:127-138`). Angezeigt werden die vorhandenen `CreatedBy`-Werte (Service-Akteure `system:sage-booking`, `system:sage-fa-arbeitsgaenge` als
solche erkennbar).

## Akzeptanzkriterien

1. Die Liste zeigt Zeilen aus allen drei Wegen (Lagerbuchung, Sage-Wareneingang, FA-Arbeitsgang), je Zeile Herkunft und Anzeige-Status nach
   F2/F3. Ein Wareneingang mit Sage-WE erscheint, einer mit `NichtNoetig` erscheint nur als Lagerbuchung (Herkunft „Wareneingang zu Bestellung“).
2. Jede Status-Kombination der drei Enums landet auf genau dem Anzeige-Status aus F2 (Unit-Test über alle Enum-Werte, neue Enum-Werte
   brechen den Test).
3. Eine FA-Arbeitsgang-Fehlerzeile mit Text „Antwort unklar …“ zeigt „klären“, ohne diesen Text „Fehler“.
4. Herkunft: Lagerbestellungs-Ausbuchung (`WarehouseRequisitionItemId` gesetzt) = „Lagerbestellung“; Ausbuchung mit FA-Text = „Ausbuchung (FA)“;
   Umbuchung = „Umbuchung“.
5. Benutzer nur mit `stock_read`, Modul `WareneingangBestellungAktiv` aus: sieht nur Lagerbuchungs-Zeilen; Modul an: zusätzlich Wareneingänge;
   FA-Zeilen nur Admin bei `ProduktionsauftragHierarchisch = true`. Kein Link der Seite führt auf 403.
6. Spaltenfilter (z. B. Herkunft = „Wareneingang“, Fehler enthält „Timeout“) filtern die ganze Union vor dem Paging; Trefferzahl und Seiten
   stimmen (Seite 2 bei wenigen Treffern leitet auf die letzte Seite, Fallstrick 50).
7. Default-Zeitraum: Zeilen älter als 30 Tage und erledigt sind ausgeblendet. Ältere nicht erledigte (wartet/gesendet/Fehler/klären) sind
   sichtbar, und der Hinweis nennt ihre Anzahl.
8. Wareneingangs- und FA-Zeilen haben keine Schreib-Schaltfläche, nur den Link zur Fachseite. Der Link öffnet die Fachseite gefiltert auf
   Bestellung bzw. FA. FA nicht mehr in der Struktur → Hinweistext statt Link.
9. (A) „Erneut senden“ an Lagerbuchungs-Fehlerzeilen funktioniert unverändert (Recht `stock_keyuser`/`picking`/Admin).
   (B) Die bestehenden Seiten sind unverändert.
10. Menü: (A) ein Eintrag, Titel nach Rückfrage 6. (B) neuer Eintrag „Sage-Übergaben“ im Lager-Menü, Sichtbarkeit aus `NavigationSicht`,
    `NavigationSichtTests` grün.
11. Build und alle Tests grün; neue Tests `SageBuchungenGesamtsichtTests` (Zuordnung, Herkunft, Rechte-Ausschluss, Zeitraum-Regel).

## Test-Szenarien

Neues Kapitel **TS-188 „Sage-Buchungen: Gesamtsicht über alle Übergabewege (v1.99.63)“** in `docs/TESTSZENARIEN.md` (Worktree), danach eine
Zeile in `secondbrain/tests/testszenarien-index.md`. Querverweis auf das Kapitel der Sage-Lagerbuchungen und TS-178 (Lagerbestellung).

- **Vorbedingungen:** DEV AKE und DEV IDEAL mit Stand 1.99.63. Je ein Queue-Eintrag Bestätigt und Fehler (vorhandene Testdaten oder
  `SageLagerbuchungAktiv` auf DEV). Ein Wareneingang mit Sage-WE (`Offen`/`Uebergeben`) und einer „nur in Sage“. IDEAL: eine
  `SageFaArbeitsgangAnlagen`-Zeile (Angelegt) und eine „Antwort unklar“. Benutzer: admin, einer nur `stock_read`, einer `stock_keyuser`.
- **188.1** admin, Seite öffnen: Zeilen aller Wege, Herkunft und Status korrekt; DB-Gegenprobe
  `SELECT Status, COUNT(*) FROM SageBookingQueueItems GROUP BY Status` usw.
- **188.2** Filter Anzeige „Fehler“ und „klären“: Zuordnung wie F2; FA-„Antwort unklar“ unter „klären“.
- **188.3** Spaltenfilter Herkunft = `Wareneingang` + Seite 2: Paging über die gefilterte Union, Zähler stimmt.
- **188.4** Zeitraum: erledigte Zeile mit `CreatedAt` älter 30 Tage (DEV-Daten oder per SQL auf DEV zurückdatiert) ausgeblendet. Fehlerzeile
  gleichen Alters sichtbar, Hinweis mit Anzahl.
- **188.5** Link Wareneingang → „Sage-Wareneingänge“ gefiltert auf die Bestellung; Link FA → „Arbeitsgänge → Sage“ mit `fa=`.
- **188.6** (A) „Erneut senden“ an Lagerbuchungs-Fehlerzeile mit `stock_keyuser`: Erfolgsmeldung, Status „wartet“. Mit `stock_read`: kein
  Knopf; POST per Hand ergibt 403.
- **Negativ 188.7** `stock_read` bei Modul Wareneingang aus: keine WE-Zeilen, kein Link auf 403.
- **Negativ 188.8** AKE (Master hierarchisch aus) bzw. Nicht-Admin auf IDEAL: keine FA-Zeilen.
- **Negativ 188.9** WE `NichtNoetig`: erscheint nicht doppelt (nur als Lagerbuchung).
- **Negativ 188.10** Leere Datenlage: „Keine Einträge.“, Pagination 0.
- **188.11** Kontrast und Bedienung: Badges mit Text, „klären“ gelb mit dunkler Schrift (≥ 4,5:1), 390 px Breite mit genau einem
  Scrollbalken der Tabelle.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

**Vorläufig** – der QA-Lauf bestätigt es am echten Diff.

- **Web-App:** ja (Controller, View, Repositories, Navigation/Layout falls umbenannt, Changelog, Version 1.99.63 in beiden `AppVersion.cs`).
- **Service:** nein (kein Dienst-Code; `IDEALAKEWMSService/AppVersion.cs` wird nur hochgezählt – Dienst-Deploy nur, wenn das Bündel ihn
  ohnehin mitnimmt).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine Schema-, Daten-, Einmal-, Prüf- oder Einstellungs-Einträge. Nur bei
  Rückfrage 6 = umbenennen (Variante A) bzw. bei Variante B ein Info-Eintrag:
  - Id `v1.99.63-SageBuchungenInfo`, Version 1.99.63, Typ Info, Firma beide, Phase Nachher, Ausführung anzeigen.
  - Text A: „Lager → ‚Sage-Lagerbuchungen‘ heißt jetzt ‚Sage-Buchungen‘ und zeigt alle Übergaben an Sage (Lagerbuchungen, Sage-Wareneingänge,
    bei IDEAL Arbeitsgänge). Reparieren weiter auf der jeweiligen Seite.“
  - Text B: „Neuer Menüpunkt Lager → ‚Sage-Übergaben‘: alle Übergaben an Sage in einer Liste, nur lesend.“
- **Publish-Befehle** (im Worktree):
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- **Doku (DoD):** `docs/anwender/09-einbuchen-ausbuchen-umbuchen.md` (Abschnitt Sage-Lagerbuchungen), `docs/anwender/18-startseite-und-menue.md`
  (Menütext), Foto der Seite per Playwright gegen DEV (nur ansehen).

## Offene Rueckfragen

1. **Welche Maske ist gemeint?** „Sage-Lagerbuchungen“ (Lager-Menü, `SageBookingQueue`) oder „Sage-Wareneingänge“ (`PurchaseOrders/
   SageWareneingaenge`)? Empfehlung: **Sage-Lagerbuchungen**. Dort fehlen heute genau die Wareneingänge mit Sage-WE; „Sage-Wareneingänge“
   zeigt nur einen Weg.
2. **Variante A oder B?** Empfehlung **A** (Begründung im Lösungsentwurf: vorhandene Seite, kein neuer Controller/Menüpunkt, Lagerbuchungs-
   Reparatur bleibt am Ort).
3. **FA-Arbeitsgänge (IDEAL) einbeziehen?** Empfehlung **ja**, sichtbar nur wie die Fachseite (Admin + `ProduktionsauftragHierarchisch`).
   Nebennutzen: Warteschlangen-Zeilen verschwundener FAs werden zum ersten Mal sichtbar. `SageStammdatenOutbox` bleibt draußen (sendet nichts).
4. **Rollen:** Zeilen eines Weges nur, wenn der Benutzer dessen Fachseite sehen darf (Lagerbuchung = stock_read-Kreis, Wareneingang =
   zusätzlich Modul `WareneingangBestellungAktiv`, FA = Admin + hierarchisch)? Empfehlung **ja**. Alternative wäre „alle Zeilen für den
   stock_read-Kreis“; dann führte der FA-Link für Nicht-Admins auf 403.
5. **Standard-Zeitraum:** „Erstellt ab“ = heute − 30 Tage. Nicht erledigte Zeilen (wartet, gesendet, Fehler, klären) immer, unabhängig vom
   Zeitraum. „Von Hand in Sage“ zählt als erledigt, sonst sammeln sich diese Zeilen ewig an (das WMS ändert den Status nie). Einverstanden
   oder anderer Zeitraum?
6. **(nur A) Umbenennen** von Seite und Menüpunkt in „Sage-Buchungen“? Route, Menü-Key und gespeicherte Favoriten bleiben. Empfehlung **ja**.
7. **Wareneingänge „nur in Sage buchen“** (Chargen-/Nachweispflicht, `PurchaseOrdersController.Receive.cs:163`, das WMS sendet nichts) als
   Status „von Hand in Sage“ mitzeigen? Empfehlung **ja**: Sie sind Sage-relevant und stehen heute schon auf „Sage-Wareneingänge“.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Welche Maske (Sage-Lagerbuchungen / Sage-Wareneingänge)? →
2. Variante A oder B? →
3. FA-Arbeitsgänge (IDEAL) einbeziehen? →
4. Rollen: Zeilen je Weg nur mit Recht auf die Fachseite? →
5. Standard-Zeitraum 30 Tage, nicht erledigte immer, „von Hand in Sage“ = erledigt? →
6. (nur A) Umbenennen in „Sage-Buchungen“? →
7. Wareneingänge „nur in Sage buchen“ mitzeigen? →
