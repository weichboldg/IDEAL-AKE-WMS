---
type: spec
title: Sage-Buchungen – eine Seite für Lagerbuchungen und Sage-Wareneingänge
slug: 2026-11-23-sage-buchungen-gesamtsicht-spec
status: Testbereit
created: 2026-11-23
updated: 2026-11-25
source_backlog: "[[2026-11-23-sage-buchungen-gesamtsicht]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "nächste freie Bündel-Version, Stand 2026-11-24 = 1.99.64 (Worktree IdealAkeWms/AppVersion.cs:5 = 1.99.63, vergeben an SQL/151). 1.99.64 nennen auch [[2026-11-24-bedarfsmeldung-aus-lesender-stueckliste-spec]] und [[2026-11-24-bf-auftrag-baugruppen-aufloesen-und-als-fa-zeigen-spec]] – der Dev-Lauf vergibt in Baureihenfolge"
testszenario_vorschlag: "nächste freie TS-Nummer, Stand 2026-11-24 = TS-189 (docs/TESTSZENARIEN.md:13800 TS-188 = SQL/151; testszenarien-index.md:162). TS-189 nennen auch die Bedarfsmeldungs- und die BF-Auftrag-Spec – Dev-Lauf löst auf"
affected_code:
  - IdealAkeWms/Services/SageBuchungenGesamtsicht.cs (neu, reine Funktion – Anzeige-Status, Herkunft, Zeile, erste Zeile je Vorgang, Zeitraum-Regel)
  - IdealAkeWms/Controllers/SageBookingQueueController.cs:15-115 (Index lädt Lagerbuchungen + Sage-WE, Filterkarte, Spaltenfilter, Paging; Requeue mit returnUrl; Lesefilter nach Rückfrage 12)
  - IdealAkeWms/Models/ViewModels/SageBookingQueueViewModel.cs (Zeilen der Gesamtsicht, Filter, Hinweise, Rechte-Flags)
  - IdealAkeWms/Views/SageBookingQueue/Index.cshtml (Titel „Sage-Buchungen“, Filterkarte, Spalten inkl. WE-Spalten, WE-Aktionen, Zahnrad)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs:866-899 (neuer ViewConfig SageBookingQueue, WE-Spalten DefaultHidden)
  - IdealAkeWms/Data/Repositories/SageBookingQueueRepository.cs:77-88 + ISageBookingQueueRepository.cs:31 (GetForMonitoringAsync mit Zeitraum-Regel)
  - IdealAkeWms/Data/Repositories/PurchaseOrderReceiptRepository.cs:41/87-98 (GetSageWareneingaengeAsync mit Zeitraum-Regel, ohne NurInSage)
  - IdealAkeWms/Controllers/PurchaseOrdersController.SageWe.cs:61-167 (Liste → 301-Umleitung, POST-Actions mit returnUrl; SageWeListeAsync entfällt)
  - IdealAkeWms/Controllers/EinkaufPurchaseOrdersController.cs:29-34 (Umleitung, Rückfrage 12)
  - IdealAkeWms/Views/PurchaseOrders/SageWareneingaenge.cshtml + Models/ViewModels/SageWareneingaengeViewModel.cs (entfallen; SageWareneingangZeile wandert in die Gesamtsicht)
  - IdealAkeWms/Services/Navigation/NavigationKatalog.cs:88/91/162/255/258 (Titel „Sage-Buchungen“, NavZiel SageWareneingaengeLager entfällt)
  - IdealAkeWms/Views/Shared/_Layout.cshtml:101-105 (Linktext, WE-Link entfällt), :271-273 (Einkauf, Rückfrage 12)
  - IdealAkeWms/Services/ColumnFilterSyntax.cs:70/85 (Register SageBookingQueue/Index erweitert, PurchaseOrders/SageWareneingaenge entfällt)
  - IdealAkeWms/Views/ServiceSettings/Index.cshtml:62/68, Views/PurchaseOrders/Receive.cshtml:263, Views/Help/Index.cshtml:143-150/170/308-329/497, Views/Users/RoleOverview.cshtml:221-226
  - IdealAkeWms/Models/AppSettingDefinitions.cs:128, IdealAkeWms/Models/ServiceSettingDefinitions.cs:73/88/93/97/102 (nur Verwendungstexte)
  - IdealAkeWms.Tests/Services/SageBuchungenGesamtsichtTests.cs (neu), IdealAkeWms.Tests/Controllers/SageWareneingaengeListeTests.cs (umziehen), PurchaseOrdersControllerTests.cs:252-254, NavigationSichtTests.cs:292-295
  - IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs, IdealAkeWms/Views/Help/Changelog.cshtml
  - docs/TESTSZENARIEN.md (neues Kapitel + Umstellungsliste), docs/anwender/05, 09, 18, docs/betrieb/04, 05, 06, 07, 08, 10, README.md
open_questions:
  - "1. Eine Seite „Sage-Buchungen“ (heutige SageBookingQueue, Route/Menü-Key/Favoriten bleiben), übernimmt alle Funktionen beider Seiten – beantwortet 2026-11-24: ok, wie Empfehlung"
  - "2. Inhalt Einbuchung, Ausbuchung, Lagerbestellungs-Entnahme, Umbuchung (wenn Schalter an), Wareneingang mit Sage-WE; Spalte + Filter „Herkunft“ – beantwortet 2026-11-24: ok, wie Empfehlung"
  - "3. Funktionen beider Seiten, Rolle wie heute, bestehende POST-Actions, Redirect zurück mit Filtern – beantwortet 2026-11-24: ok, wie Empfehlung"
  - "4. WE-Aktionen je Sage-WE-Vorgang, Knöpfe nur an der ersten Position – beantwortet 2026-11-24: ok, wie Empfehlung"
  - "5. WE-Spalten per Zahnrad standardmäßig ausgeblendet – beantwortet 2026-11-24: ok, wie Empfehlung"
  - "6. Alte Seite Sage-Wareneingänge (Lager + Einkauf) → Umleitung, Menüpunkt entfällt, Verweise nachziehen – beantwortet 2026-11-24: ok, wie Empfehlung (Einkauf-Teil siehe Rückfrage 12)"
  - "7. Rechte: Seite stock_read, WE-Zeilen nur mit WareneingangBestellungAktiv, Aktionen Keyuser/Admin – beantwortet 2026-11-24: ok, wie Empfehlung"
  - "8. Gemeinsamer Status + Originaltext, NurInSage nicht anzeigen – beantwortet 2026-11-24: ok, wie Empfehlung"
  - "9. FA-Arbeitsgänge (IDEAL) nicht dabei – beantwortet 2026-11-24: ok, wie Empfehlung"
  - "10. Standard-Zeitraum 30 Tage, offene Zeilen immer + Hinweis – beantwortet 2026-11-24: ok, wie Empfehlung"
  - "11. „nacherfasst“-Zusatz in „Erfasst von“ – beantwortet 2026-11-24: ok, wie Empfehlung"
  - "12. NEU (Lücke aus Antwort 6 + 7): Einkauf-Benutzer ohne Lager-Recht sehen heute „Einkauf → Sage-Wareneingänge (offen)“ (RequireEinkaufAccess, kein stock_read). Eine Umleitung auf die stock_read-Seite endet für sie mit 403. Empfehlung (a): Sage-Buchungen zusätzlich für Einkauf lesbar (Rolle einkauf + EinkaufAktiv), Einkauf sieht dort nur WE-Zeilen, keine Lagerbuchungen, keine Knöpfe; Einkauf-Menüpunkt bleibt und landet über die Umleitung. Alternativen: (b) Einkauf-Ansicht bleibt unverändert, (c) Umleitung trotzdem, Einkauf braucht künftig stock_read."
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "A"
freigabe_von: Gerald
freigabe_am: 2026-11-24
---

## Ziel / Nutzen (das Warum)

Gerald will an **einer** Stelle sehen und reparieren, was das WMS an Sage übergibt. Heute braucht er dafür zwei Seiten mit eigenen Filtern
und eigenen Knöpfen: „Sage-Lagerbuchungen“ (`SageBookingQueue`) und „Sage-Wareneingänge“ (`PurchaseOrders/SageWareneingaenge`, dazu die
Einkauf-Sicht „(offen)“). Entscheidung Gerald 2026-11-24 (Antworten 1–11): Die Seiten werden **zusammengeführt, mit allen Funktionen beider
Seiten**. Die Seite „Sage-Buchungen“ ist dabei nicht mehr nur lesend: Lagerbuchungen und Sage-Wareneingänge werden dort auch repariert.

## Umfang (In-Scope / Out-of-Scope)

### Ist-Stand: alle Schreibwege WMS → Sage (gegengeprüft im Bündel-Worktree, Stand v1.99.61, Zeilen 2026-11-24 nachgeprüft)

Gesucht über `HttpMethod.*`, `new HttpRequestMessage(`, `PostAsync|PutAsync|PostAsJson|PutAsJson|PatchAsync`, SQL-Schreibbefehle auf
`KHK*` und `ExecuteNonQuery`/`ExecuteSql*` in Web und Dienst. Ergebnis: **genau drei SData-POST-Stellen, alle im Dienst, alle aus
`SageBookingWorker`**. Die Web-App schreibt nicht an Sage.

| Weg | POST (Client) | Aufruf im Dienst | Ablage (WMS-DB) | Status-Enum | Anzeige heute | Recht Anzeige / Aktion |
|---|---|---|---|---|---|---|
| Lagerbuchung (Zugang/Entnahme/Umbuchung) | `SageLagerbuchungClient.cs:45` | `SageBookingWorker.cs:443`, Protokoll `SyncLogServices.SageLagerbuchung` (`:349`) | `SageBookingQueueItems` (FK `StockMovementId`), angelegt vom Decorator `SageBookingEnqueueingStockMovementRepository.cs:65-69` (`CreatedBy` = Akteur der Buchung, `SageBookingQueueRepository.cs:35`) und vom Nacherfassungs-Lauf `SageBookingQueueRepository.cs:140-187` (`CreatedBy = system:sage-booking`, `:169`) | `SageBookingQueueStatus` Offen/Gesendet/Bestaetigt/Fehler (`SageBookingQueueStatus.cs:11-17`) | „Sage-Lagerbuchungen“ `SageBookingQueueController.Index` (`:64-93`), alle Einträge ohne Zeitraum (`SageBookingQueueRepository.cs:77-88`) | Klasse `[RequireStockReadAccess]` (`:15`); „Erneut senden“ `[RequireStockKeyUserAccess]` (`:97`), Knopf über `CanTransferStockAsync` (`Index.cshtml:6`) |
| Sage-Wareneingangsbeleg (ELL) | `SageWareneingangClient.cs:47` | `SageWareneingangService.cs:151`, gestartet `SageBookingWorker.cs:210`, Protokoll `SyncLogServices.SageWareneingang` | `PurchaseOrderReceipts.SageWeStatus` + `SageWe*`-Felder (`PurchaseOrderReceipt.cs:51-62`), ein Beleg je `SageWeVorgang` (`:52-53`) | `SageWareneingangStatus` NichtNoetig/Offen/Gesendet/Uebergeben/Fehler/NurInSage/Klaeren (`PurchaseOrderReceipt.cs:15-26`) | „Sage-Wareneingänge“ `PurchaseOrdersController.SageWe.cs:61-117` (ohne `NichtNoetig`, `PurchaseOrderReceiptRepository.cs:92`), Einkauf-Variante „(offen)“ `EinkaufPurchaseOrdersController.cs:29-34` (`OffeneSageWeStatus`, `SageWe.cs:55-59`, enthält NurInSage) | Lager: Klasse `[RequireStockReadAccess]` + `[RequireWareneingangBestellungAktiv]` (`PurchaseOrdersController.cs:17-18`); Einkauf: `[RequireEinkaufAccess]` + `[RequireEinkaufAktiv]` (`EinkaufPurchaseOrdersController.cs:14-15`), **ohne Lager-Recht**; Erneut senden / Als übergeben / CSV `[RequireStockKeyUserAccess]` (`SageWe.cs:121-167`) |
| FA-Arbeitsgänge anlegen (nur IDEAL) | `SageFaArbeitsgangClient.cs:84` | `SageFaArbeitsgangService.cs:157` | `SageFaArbeitsgangAnlagen` | `SageFaArbeitsgangStatus` | „Arbeitsgänge → Sage“ | Admin + `ProduktionsauftragHierarchisch` – **nicht Teil der Seite (Antwort 9)** |

Belegte Nicht-Wege (unverändert gültig):

- **Vorgemerkte Stammdaten-Übergaben** in `SageStammdatenOutbox` werden **nicht gesendet**: kein Worker, kein Client
  (`SageStammdatenOutbox.cs:9-10`, `SageStammdatenOutboxItem.cs:6-11`).
- **Beschichtungsauftrag, BF-Auftrag, Kreditoren, Sage-Benutzerfelder** schreiben nicht an Sage (Web-SData-Zugriffe sind GET,
  `SageSdataReadClient.cs:97`, `:248`, `:280`; BF/Beschichtung erzeugen Mails/CSV, `BfAuftragEmailService.cs:59`).
- **Kein SQL-Schreiben in die Sage-DB** (die `ExecuteNonQuery`-Stellen im Sage-Import laufen auf der WMS-Verbindung,
  `SageImportService.cs:137`, `:218`, `:306`, `:343`).
- **Kommissionierung geht nie an Sage:** Umbuchungen mit FA-Bezug schließen Decorator und Nacherfassung aus
  (`SageBookingEnqueueDecision.cs:69`, `:74-75`; `SageBookingQueueRepository.cs:152`).
- **Wareneingang ohne Sage-WE** (`SageWeStatus = NichtNoetig`) steht als Einbuchung in der Queue; mit Sage-WE (auch NurInSage) nie
  (`SageBookingEnqueueDecision.cs:70`, `SageBookingQueueRepository.cs:154`). Es gibt also keine Doppelzeile zwischen den beiden Quellen.

**In-Scope**
- Die Seite `SageBookingQueue/Index` heißt „Sage-Buchungen“ und zeigt Lagerbuchungen (Queue) und Sage-Wareneingänge
  (`SageWeStatus` ∉ {NichtNoetig, NurInSage}) in einer Liste, mit allen Filtern, Hinweisen und Knöpfen beider bisherigen Seiten.
- Alte Listen-Routen (Lager und Einkauf) leiten um; Menüpunkt „Sage-Wareneingänge“ entfällt; alle Verweise werden nachgezogen.
- Listen-View-Pflichten (ADR 0005), Zahnrad (ColumnDefinitions), Navigation über `NavigationKatalog` (ADR 0034).

**Out-of-Scope**
- FA-Arbeitsgänge (Antwort 9), `SageStammdatenOutbox` (sendet nichts).
- Neue Schreibaktionen: es werden nur die bestehenden POST-Actions wiederverwendet (Antwort 3).
- Umbau der Status-Enums, des Dienstes oder der Sende-Logik; Live-Lesen in Sage.
- „Nur in Sage buchen“-Wareneingänge (Antwort 8). Folge, nur zur Kenntnis: Sie stehen danach in keiner Liste mehr, auch nicht im Einkauf
  „nur offene“ (bisher enthalten, `SageWe.cs:58`). Sichtbar bleiben sie als Zähler „Sage-Wareneingang: n× Status“ je Position in den Offenen
  Bestellungen (`PurchaseOrderReceiptRepository.cs:18`, `:84`; `PurchaseOrderListViewModel.cs:42`).

## Fachliche Anforderungen

**F1 Quellen und Zeilen.**
- Lagerbuchung: jeder `SageBookingQueueItem`, eine Zeile.
- Sage-Wareneingang: jeder `PurchaseOrderReceipt` mit `SageWeStatus` ∉ {NichtNoetig, NurInSage}, **eine Zeile je Position** (Antwort 4).
  Ein Vorgang (`SageWeVorgang`) ist ein ELL-Beleg mit allen Positionen.

**F2 Gemeinsamer Status** (Antwort 8; Auswahlliste „Status“ in der Filterkarte, Spalte „Status im Weg“ zeigt den Originaltext):

| Anzeige (`SageBuchungAnzeige`) | Lagerbuchung | Sage-Wareneingang | offen? | Badge |
|---|---|---|---|---|
| wartet (`Wartet`) | Offen | Offen | ja | `text-bg-secondary` |
| gesendet (`Gesendet`) | Gesendet | Gesendet | ja | `text-bg-info` |
| übergeben (`Uebergeben`) | Bestaetigt | Uebergeben | nein | `text-bg-success` |
| Fehler (`Fehler`) | Fehler | Fehler | ja | `text-bg-danger` |
| klären (`Klaeren`) | – | Klaeren | ja | `text-bg-warning` |
| – (nicht angezeigt) | – | NichtNoetig, NurInSage | – | – |

Originaltexte unverändert: `SageBookingQueueController.StatusText` (`:32-39`) und `PurchaseOrdersController.SageWeStatusText` (`SageWe.cs:17-27`).
Der Text steht immer im Badge (Farbe nie allein); `text-bg-*` setzt die Schriftfarbe passend (Kontrast WCAG AA).

**F3 Herkunft** (Antwort 2; Spalte „Herkunft“ + Auswahlliste „Herkunft“ in der Filterkarte, Reihenfolge = Vorrang, aus gespeicherten Feldern):

| Herkunft (`SageBuchungHerkunft`) | Regel | Beleg |
|---|---|---|
| Wareneingang | Zeile aus `PurchaseOrderReceipts` (Sage-WE) | F1 |
| Lagerbestellung | Queue-Zeile, `StockMovement.WarehouseRequisitionItemId != null` | `StockMovement.cs:64` |
| Einbuchung / Ausbuchung / Umbuchung | Queue-Zeile nach `StockMovement.MovementType` | `StockMovement.cs:22`, `SageBookingEnqueueDecision.cs:40-52` |

Umbuchungen kommen nur bei eingeschaltetem `SageLagerbuchungUmbuchungAktiv` in die Queue (`SageBookingEnqueueDecision.cs:68`); die Seite
zeigt einfach, was in der Queue steht. Ein Wareneingang **ohne** Sage-WE ist eine Queue-Einbuchung und erscheint als „Einbuchung“ (wie heute).

> [!note] Überholt (Entwurf 2026-11-23)
> Die Herkunftswerte „Wareneingang zu Bestellung“ (Queue-Zeile mit `PurchaseOrderReceipt`), „Ausbuchung (FA)“ und „FA-Arbeitsgang“ entfallen
> mit Antwort 2 und 9.

**F4 Spalten** (Reihenfolge; „Zahnrad“ = `ColumnDefinitions`, Antwort 5):

| Key | Kopf | Lagerbuchung | Sage-Wareneingang | Standard |
|---|---|---|---|---|
| `id` | # | Queue-`Id` | leer | sichtbar |
| `herkunft` | Herkunft | F3 | „Wareneingang“ | sichtbar, gesperrt |
| `status` | Status | Anzeige F2 | Anzeige F2 | sichtbar, gesperrt |
| `status-weg` | Status im Weg | `StatusText` | `SageWeStatusText` | sichtbar |
| `created-at` | Erstellt | `CreatedAt` | `Receipt.CreatedAt` | sichtbar |
| `bestellung` | Bestellung | leer | `Belegnummer` | sichtbar |
| `pos` | Pos.-ID | leer | `BelPosId` | sichtbar |
| `article` | Artikel | `StockMovement.Article.ArticleNumber` | dto. über `Receipt.StockMovement` | sichtbar |
| `quantity` | Menge | `StockMovement.Quantity` | `Receipt.Quantity` (wie `SageWe.cs:41`) | sichtbar |
| `storage-location` | Lagerplatz | `StockMovement.StorageLocation.Code` | dto. | sichtbar |
| `sagebeleg` | Sage-Beleg | leer | `SageWeBelegnummer` | sichtbar |
| `sent-at` | Gesendet | `SentAt` | leer | sichtbar |
| `confirmed-at` | Übergeben am | `ConfirmedAt` | `SageWeAm` | sichtbar |
| `attempts` | Vers. | `AttemptCount` | `SageWeVersuche` | sichtbar |
| `error` | Fehler | `LastError` + „Sage-Antwort“-Details wie heute (`Index.cshtml:89-106`) | `SageWeFehler` (pre-wrap wie `SageWareneingaenge.cshtml:123`) | sichtbar |
| `erfasst-von` | Erfasst von | F6 | `Receipt.CreatedBy` | sichtbar |
| `lieferschein` | Lieferschein | leer | `Lieferschein` | **ausgeblendet** |
| `barcode` | Barcode | leer | `Barcode` | **ausgeblendet** |
| `memo-position` | Positionsmemo | leer | `StockMovement.Note` | **ausgeblendet** |
| `memo` | Memo | leer | Memo-Kennung je Vorgang (`SageWe.cs:77-79`) | **ausgeblendet** |
| `csv` | CSV | leer | `WareneingangCsv.StatusText` (`SageWe.cs:93`) | **ausgeblendet** |
| `actions` | – | F5 | F5 | sichtbar, gesperrt |

Die Spalte „Bewegung“ (`movement-type`) geht in „Herkunft“ auf. Leere Zellen bleiben leer.

**F5 Aktionen** (Antwort 3, 4, 7; Rechte wie heute):
- Lagerbuchung, Status Fehler: „Erneut senden“ → `SageBookingQueue/Requeue` (`:95-115`), Knopf bei `CanTransferStockAsync` (wie `Index.cshtml:6`).
- Sage-Wareneingang, **je Vorgang einmal**, an der ersten Zeile des Vorgangs auf der angezeigten Seite (Reihenfolge der Liste):
  - Status Fehler/Klären: „Erneut senden“ → `PurchaseOrders/SageWeErneutSenden` und „Als übergeben markieren“ mit Pflichtfeld Belegnummer
    (max. `SagePurchaseOrderLine.BelegnummerMaxLaenge`) → `PurchaseOrders/SageWeAlsUebergeben`, Texte/Tooltips wie `SageWareneingaenge.cshtml:126-137`.
  - CSV-Fehler: „CSV erneut schreiben“ → `PurchaseOrders/CsvErneutSchreiben` mit dem Hinweistext wie `SageWareneingaenge.cshtml:139-147`.
  - Knöpfe nur, wenn `CanTransferStockAsync` **und** `WareneingangBestellungAktiv` an (wie `SageWe.cs:101-109`; die POST-Actions liegen hinter dem
    Klassen-Gate von `PurchaseOrdersController`, `PurchaseOrdersController.cs:17-18`).
  - Übrige Zeilen desselben Vorgangs auf der Seite zeigen in der Aktionsspalte „gehört zu Vorgang <Memo-Kennung>“ (kleiner, gedämpfter Text;
    die Kennung ist die lesbare Form des Vorgangs, `SageWareneingang.Kennung`).
  - Warum „auf der angezeigten Seite“ statt „kleinste Id“: Bei Spaltenfilter oder Seitenwechsel kann die erste Position fehlen; dann gäbe es
    keinen Knopf. Die Aktion gilt ohnehin für den ganzen Vorgang (`PurchaseOrderReceiptRepository.cs:102-103`, `:117-118`).
- Alle Formulare senden `returnUrl` = aktuelle URL (Filter, Seite). Die POST-Actions leiten bei `Url.IsLocalUrl(returnUrl)` dorthin zurück, sonst
  auf `SageBookingQueue/Index` (WE-Actions mit `herkunft=Wareneingang`). Muster `BdeTeilelisteController.cs:184`. Meldungen über
  `TempData["SuccessMessage"]`/`["WarningMessage"]` unverändert.

**F6 „Erfasst von“** (Antwort 11): Lagerbuchung = `StockMovement.CreatedBy` (wer gebucht hat); hat die Queue-Zeile
`CreatedBy = SageBookingQueueRepository.ServiceActor` (`:12`, Nacherfassung `:169`) und die Bewegung einen Akteur, folgt der Zusatz
„(nacherfasst)“. Ohne Akteur an der Bewegung steht der Service-Akteur da (wie `:35`), ohne Zusatz. Wareneingang = `Receipt.CreatedBy`.

**F7 Rechte** (Antwort 7, Rückfrage 12):
- Seite: `stock_read`-Kreis (`CurrentUserService.cs:104-105`), Klassenfilter bleibt `[RequireStockReadAccess]` – bei Rückfrage 12 (a) zusätzlich
  Einkauf, siehe Lösungsentwurf.
- Lagerbuchungs-Zeilen: `stock_read`-Kreis.
- WE-Zeilen: nur bei `WareneingangBestellungAktiv` = `true` (gleiche Lese-Semantik wie `NavigationKatalog.cs:243`) und `stock_read`;
  bei Rückfrage 12 (a) zusätzlich für Einkauf (`EinkaufAktiv` + Rolle einkauf, ohne Modulbedingung wie heute, `EinkaufPurchaseOrdersController.cs:10`).
- Aktionen: wie F5 (Keyuser/Admin über `RequireStockKeyUserAccess`).
- Wer keine WE-Zeilen sehen darf, sieht auch die Auswahl „Herkunft = Wareneingang“, die WE-Spalten, die WE-Hinweise und die Filter
  „Bestellung“ nicht.

**F8 Filterkarte und Zeitraum** (Antwort 10; GET-Formular wie `SageWareneingaenge.cshtml:52-81`):
- „Herkunft“ (Auswahlliste F3, „Alle“), „Status“ (Auswahlliste F2, „Alle“), „nur offene“ (Haken: wartet/gesendet/Fehler/klären),
  „Bestellung“ (Text, enthält – wie `PurchaseOrderReceiptRepository.cs:94`; gesetzt ⇒ nur WE-Zeilen), „von“/„bis“ (Datum, Erstellt).
- Ohne „von“ gilt heute − 30 Tage; das Feld zeigt den wirksamen Wert. „Zurücksetzen“ = Standard.
- Offene Zeilen erscheinen **immer**, unabhängig von „von“/„bis“. Liegen offene Zeilen außerhalb des Zeitraums, steht über der Tabelle
  „n offene Buchungen außerhalb des Zeitraums werden mit angezeigt.“ (melden statt still). Herkunft/Status/Bestellung/Spaltenfilter gelten
  auch für diese Zeilen.
- Query-Parameter: `herkunft`, `status`, `nurOffene`, `bestellung`, `von`, `bis`, `page`, `pageSize`, `colf_*`. `status` bindet an
  `SageBuchungAnzeige`; die Werte heißen so, dass alte Lesezeichen `?status=Fehler|Gesendet` und WE-Werte `Klaeren|Uebergeben` weiter binden;
  `?status=Offen|Bestaetigt` bindet nicht und zeigt „Alle“ (kein Fehler).

**F9 Hinweise und Kopf** (alles aus beiden Seiten bleibt):
- TLS-Banner (`Index.cshtml:16-29`) wie heute.
- Q-2-Hinweis „Sage-Wareneingang ist aus …“ (`SageWe.cs:110`, `SageWareneingang.AusHinweis`) und CSV-Hinweise (`SageWe.cs:111`) – nur wenn
  WE-Zeilen sichtbar.
- Knopf „Wareneingang zu Bestellung“ (`SageWareneingaenge.cshtml:17-20`) – nur wenn WE-Zeilen sichtbar und `CanAccessStockAsync`.
- Einleitungstext: beide Texte (`Index.cshtml:31-35`, `SageWareneingaenge.cshtml:42-46`) zusammengefasst; Leerzeile „Keine Einträge.“

**F10 Alte Seiten** (Antwort 6):
- `GET PurchaseOrders/SageWareneingaenge` → 301 auf `SageBookingQueue/Index?herkunft=Wareneingang` und reicht `bestellung`, `von`, `bis` weiter;
  `status` wird über die F2-Zuordnung übersetzt (Offen → Wartet; NichtNoetig/NurInSage → weggelassen). Muster „Abgeloeste Routen“
  ([[controller]]; `ProductionOrdersController.cs:63-72`, `RedirectToActionPermanent`).
- `GET EinkaufPurchaseOrders/SageWareneingaenge` → 301 auf dieselbe Seite mit zusätzlich `nurOffene=true` (Ergebnis für Einkauf-Benutzer
  hängt an Rückfrage 12).
- Menü: Lager-Eintrag „Sage-Wareneingänge“ entfällt. In-App-Favoriten können darauf nie gezeigt haben: Das Ziel hat keine Kategorie
  (`NavigationKatalog.cs:91`), Favoriten zeigen nur Kacheln (`:195`); ein gespeicherter Key erscheint künftig als „… (nicht mehr vorhandene
  Funktion)“ (`:197`). Browser-Lesezeichen landen über die Umleitung.

## Technischer Loesungsentwurf

**Kern `IdealAkeWms/Services/SageBuchungenGesamtsicht.cs`** (statisch, DB-frei, Muster `SageFaArbeitsgaenge.BuildVorschau`):
- `enum SageBuchungAnzeige { Wartet, Gesendet, Uebergeben, Fehler, Klaeren }`, `enum SageBuchungHerkunft { Einbuchung, Ausbuchung, Umbuchung,
  Lagerbestellung, Wareneingang }` – nur Anzeige, keine DB-Spalte.
- `Anzeige(SageBookingQueueStatus)` → `SageBuchungAnzeige`, `Anzeige(SageWareneingangStatus)` → `SageBuchungAnzeige?` (null = nicht anzeigen).
- `Herkunft(StockMovement)`, `IstOffen(SageBuchungAnzeige)`, `ErfasstVon(SageBookingQueueItem)` (F6).
- `record Zeile(...)` mit den Feldern aus F4 plus `Queue` (nullable), `We` (die bestehende `SageWareneingangZeile` mit Receipt/Memo/Csv/CsvFehler,
  aus `SageWareneingaengeViewModel.cs:23` hierher verschoben) und `ZeigtWeAktionen`.
- `Build(queue, weZeilen)` → Liste nach `CreatedAt` absteigend, dann `Id`; `MarkiereErsteJeVorgang(seite)` setzt `ZeigtWeAktionen` nach dem Paging.

**Repositories** (ADR 0001; je Quelle eine Abfrage, Regel `(CreatedAt >= von && CreatedAt < bis+1) || offen`):
- `ISageBookingQueueRepository.GetForMonitoringAsync(DateTime von, DateTime? bis)` statt `(SageBookingQueueStatus?)`; einziger Aufrufer
  `SageBookingQueueController.cs:71` (geprüft per Suche in allen `*.cs`). Includes wie heute (`:80-81`); `WarehouseRequisitionItemId` ist ein
  Skalar am `StockMovement`, kein Include nötig.
- `IPurchaseOrderReceiptRepository.GetSageWareneingaengeAsync(string? bestellung, DateTime von, DateTime? bis)`: zusätzlich ohne `NurInSage`, der
  Status-Parameter entfällt (Filter läuft über F2); einziger Aufrufer `SageWe.cs:75`.
- Die Memo/CSV-Aufbereitung aus `SageWeListeAsync` (`SageWe.cs:77-96`, inkl. `GetCsvExporteAsync`, `SageSystemKuerzel`, Schalter) zieht in den
  Index der Gesamtsicht um, unverändert; `SageWeListeAsync` entfällt.

**`SageBookingQueueController.Index(SageBuchungHerkunft? herkunft, SageBuchungAnzeige? status, bool nurOffene, string? bestellung,
DateTime? von, DateTime? bis, int page = 1, int? pageSize = null)`**:
- Rechte F7 über `ICurrentUserService` + `IAppSettingRepository` (neue Ctor-Abhängigkeiten: `IPurchaseOrderReceiptRepository`,
  `IAppSettingRepository`). Nicht sichtbare Quellen werden nicht gelesen; `herkunft=Wareneingang` lädt die Queue nicht, `bestellung` ebenso.
- Union bauen → Filterkarte (Herkunft, Status, nurOffene) → `ColumnFilterHelper.Apply` über die ganze Union mit `ColumnMap` (gerenderter Zelltext,
  Datum `dd.MM.yyyy HH:mm` wie `:57-59`) → `Skip/Take` → `MarkiereErsteJeVorgang`.
- `PageSize.Resolve` + `PaginationState` als Model-Property, Seitenparameter `page` (Fallstrick 50).
- `Requeue(int id, string? returnUrl)` sonst unverändert (`:95-115`).
- `const int StandardTage = 30; // ponytail: feste 30 Tage, Einstellung erst bei Bedarf`.

**`PurchaseOrdersController.SageWe.cs`**: `SageWareneingaenge(...)` wird Umleitung (F10); `SageWeErneutSenden`, `SageWeAlsUebergeben`,
`CsvErneutSchreiben` bekommen `string? returnUrl` und ersetzen die fünf `RedirectToAction(nameof(SageWareneingaenge))` (`:129`, `:134`, `:148`,
`:153`, `:166`) durch einen gemeinsamen Rücksprung (F5). Logik, Q-2-Sperre und Meldungstexte bleiben. `OffeneSageWeStatus` (`:55-59`) entfällt,
wenn kein Aufrufer bleibt (Suche: nur `SageWe.cs:76`, `SageWareneingaenge.cshtml:8`).

**View `Views/SageBookingQueue/Index.cshtml`**:
- Titel/`ViewData["Title"]`/`h2` „Sage-Buchungen“.
- Zahnrad: `data-table-toolbar`-Host, `<table data-view-key="SageBookingQueue" data-server-column-filter="true">`, jedes `<th>` mit
  `data-col-key`, `#view-config` + `#column-config` (WE-Spalten `"defaultHidden": true`); Muster `Articles/Index.cshtml:140-152`,
  `Receive.cshtml:114-118`, `:268-279`.
- WE-Spalten-Köpfe, WE-Filter und WE-Hinweise nur, wenn WE-Zeilen sichtbar (F7) – dann auch nicht im `#column-config`.
- Formulare der WE-Aktionen mit `asp-controller="PurchaseOrders"` (Wächter `PurchaseOrdersControllerTests.cs:252-254` zieht auf diese View um).

**`ColumnDefinitions`**: `ViewConfig SageBookingQueue` („Sage-Buchungen“, `SupportsReorder: false`, `SupportsSortDefault: false`) mit den Keys
aus F4, gesperrt `herkunft`, `status`, `actions`, `DefaultHidden` für die fünf WE-Spalten; Eintrag in `GetByViewKey` (`:866-899`). Drift-Wächter
`ViewKeyDriftGuardTests.ViewKey_Registriert` greift automatisch.

**`ColumnFilterSyntax.cs`**: Register `SageBookingQueue/Index` (`:70`) um `pos` (N) erweitern; `confirmed-at` bleibt D. Eintrag
`PurchaseOrders/SageWareneingaenge` (`:85`) entfällt. P-2 (Kollation) entfällt, gefiltert wird in C#.

**Navigation (Fallstrick 58, ADR 0034)**: `NavZiele.SageLagerbuchungen` Titel → „Sage-Buchungen“ (`:88`, Key bleibt); `SageWareneingaengeLager`
aus `NavZiele` (`:91`), `Alle` (`:162`) und Regel (`:258`) entfernen; `_Layout.cshtml:101` Linktext, `:103-106` Block entfernen.
`NavigationSichtTests.cs:295` erwartet künftig „Sage-Buchungen“. Einkauf-Eintrag (`:131`, `_Layout.cshtml:271-273`) nach Rückfrage 12.

**Rückfrage 12 (a), falls so beantwortet**: der Klassenfilter `[RequireStockReadAccess]` (`SageBookingQueueController.cs:15`) wandert an die
Actions – `Index` bekommt den neuen kombinierten Filter `RequireStockReadOrEinkaufAccess` (stock_read-Kreis oder einkauf + `EinkaufAktiv`),
`Requeue` behält `[RequireStockKeyUserAccess]` (`:97`) und bekommt `[RequireStockReadAccess]`; Einkauf-Benutzer ohne `stock_read` sehen nur WE-Zeilen, nie Knöpfe. Neuer Filter =
drei Stellen (Attribut, [[controller]], `Views/Users/RoleOverview.cshtml`). Der Einkauf-Menüpunkt bleibt auf
`EinkaufPurchaseOrders/SageWareneingaenge` und landet über die Umleitung.

> [!note] Überholt (Entwurf 2026-11-23)
> Der Entwurf vom 2026-11-23 sah eine **nur lesende** Gesamtsicht über drei Wege (inkl. FA-Arbeitsgänge) mit Link zur jeweiligen Fachseite vor;
> „Sage-Wareneingänge“ und „Arbeitsgänge → Sage“ sollten unverändert bleiben. Mit Antwort 1, 3, 6 und 9 ist das ersetzt: zusammengeführte
> Seite mit allen Funktionen, ohne FA-Arbeitsgänge, alte WE-Seite wird Umleitung.
>
> **Variante B – verworfen:** eigene neue Seite „Sage-Übergaben“ (`SageUebergabenController`, nur lesend, eigener Menüpunkt), bestehende
> Seiten unverändert. Verworfen, weil sie einen dritten Sage-Menüpunkt und doppelte Lagerbuchungs-Zeilen bringt; gewählt ist A
> (`freigabe_entscheidung: A`).

## Migrations-/SQL-Auswirkungen

Keine. Alle Felder existieren (`SageBookingQueueItems`, `PurchaseOrderReceipts.SageWe*`, `StockMovements.WarehouseRequisitionItemId` seit SQL/166).
Kein Index: Die Queue wird heute schon ganz gelesen (`SageBookingQueueRepository.cs:87`); misst die QA auf DEV eine spürbare Ladezeit, ist das ein
Befund, kein Vorab-Index. Kein FreshInstall-Eintrag, kein `SQL/AgentJobs`-Bezug.

## Einstellungen

Keine neue oder geänderte Einstellung. Nur die **Verwendungstexte** nennen künftig „Sage-Buchungen“ statt „Sage-Wareneingänge“:
`AppSettingDefinitions.cs:128` (`WareneingangBarcodeAktiv`), `ServiceSettingDefinitions.cs:73`, `:88`, `:93`, `:97`, `:102`. Wert, Default,
Wertebereich und Wirkung bleiben. Der Standard-Zeitraum ist eine Konstante (F8), keine Einstellung.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine neuen Schreibwege. Die wiederverwendeten Actions setzen `ModifiedAt/By/ByWindows` schon: `RequeueAsync`
(`SageBookingQueueRepository.cs:127-138`), `SageWeErneutSendenAsync`/`SageWeAlsUebergebenAsync` (`PurchaseOrderReceiptRepository.cs:109`, `:126`),
`CsvErneutSchreibenAsync` (Aufruf `SageWe.cs:163` mit Anzeige- und Windows-Name).

## Akzeptanzkriterien

1. Menü Lager zeigt „Sage-Buchungen“ (kein Eintrag „Sage-Wareneingänge“ mehr); die Seite unter `/SageBookingQueue` heißt „Sage-Buchungen“.
2. Die Liste zeigt Queue-Zeilen und Sage-WE-Positionen gemeinsam; ein Wareneingang mit `NichtNoetig` erscheint nur einmal (als „Einbuchung“),
   einer mit `NurInSage` gar nicht.
3. Jeder Wert beider Status-Enums landet auf genau dem Anzeige-Status aus F2 (Unit-Test über alle Enum-Werte; ein neuer Enum-Wert bricht den Test).
4. Herkunft: Lagerbestellungs-Entnahme = „Lagerbestellung“, Umbuchung = „Umbuchung“, Sage-WE = „Wareneingang“; Filter „Herkunft“ schränkt genau so ein.
5. Lagerbuchung Fehler + Keyuser: „Erneut senden“ reiht ein, Rücksprung auf dieselbe gefilterte Seite (Filter und Seite bleiben).
6. Sage-WE-Vorgang mit 3 Positionen im Status Fehler/Klären: drei Zeilen; Knöpfe „Erneut senden“ und „Als übergeben markieren“ nur an der
   ersten Zeile auf der Seite, die anderen zeigen „gehört zu Vorgang WE#…#“. Ein Klick wirkt auf alle drei Positionen.
7. „Als übergeben markieren“ ohne bzw. mit 46 Zeichen Belegnummer: abgelehnt mit dem bisherigen Hinweis; mit gültiger Nummer: alle Positionen
   „übergeben“, Rücksprung mit Filtern.
8. CSV-Fehler: „CSV erneut schreiben“ mit Hinweistext an der ersten Zeile des Vorgangs; wirkt wie bisher.
9. Spalten Lieferschein, Barcode, Positionsmemo, Memo, CSV sind beim ersten Aufruf ausgeblendet, über das Zahnrad einblendbar, die Wahl bleibt
   gespeichert; bei Lagerbuchungen leer.
10. Rechte: `stock_read` ohne Keyuser sieht die Seite ohne Knöpfe, POST per Hand ergibt 403. `WareneingangBestellungAktiv` aus: keine WE-Zeilen,
    keine WE-Spalten/-Filter/-Hinweise. Einkauf-Benutzer: Verhalten nach Rückfrage 12.
11. Zeitraum: Ohne „von“ sind erledigte Zeilen älter als 30 Tage ausgeblendet; offene ältere (wartet/gesendet/Fehler/klären) sind sichtbar und der
    Hinweis nennt ihre Anzahl.
12. Nacherfasste Lagerbuchung zeigt in „Erfasst von“ den buchenden Benutzer mit „(nacherfasst)“.
13. `/PurchaseOrders/SageWareneingaenge?status=Fehler&bestellung=X` leitet (301) auf Sage-Buchungen mit Herkunft Wareneingang, Status Fehler,
    Bestellung X; `/EinkaufPurchaseOrders/SageWareneingaenge` zusätzlich mit „nur offene“. Die Links aus `/ServiceSettings` führen direkt auf
    Sage-Buchungen (Herkunft Wareneingang).
14. Spaltenfilter und Filterkarte filtern die ganze Union vor dem Paging; Trefferzahl stimmt; Seite außerhalb der Treffer wird geklemmt (Fallstrick 50).
15. Q-2-Hinweis, CSV-Hinweise, TLS-Banner und Knopf „Wareneingang zu Bestellung“ erscheinen unter denselben Bedingungen wie bisher.
16. Build und alle Tests grün; `SageBuchungenGesamtsichtTests` (Zuordnung, Herkunft, erste Zeile je Vorgang, Zeitraum-Regel, nacherfasst),
    umgezogene `SageWareneingaengeListeTests`, `ViewKeyDriftGuardTests`, `NavigationSichtTests`.

## Test-Szenarien

Neues Kapitel **TS-189 (vorläufig, Nummer prüft der Dev-Lauf) „Sage-Buchungen: Lagerbuchungen und Sage-Wareneingänge auf einer Seite“** in
`docs/TESTSZENARIEN.md` (Worktree), danach Zeile in `secondbrain/tests/testszenarien-index.md`. Kein Agent schreibt an Sage: Testdaten über vorhandene
DEV-Daten bzw. per SQL auf DEV gesetzte Status; „Erneut senden“ nur bei ausgeschaltetem Dienst oder Sage-Test (Gerald).

- **Vorbedingungen:** DEV AKE (WE-Zeilen, `WareneingangBestellungAktiv` an) und DEV IDEAL (nur Lagerbuchungen). Je eine Queue-Zeile Bestätigt
  und Fehler, eine nacherfasste, eine Lagerbestellungs-Entnahme; ein Sage-WE-Vorgang mit 3 Positionen auf Fehler, einer auf Klären, einer mit
  CSV-Fehler, einer `NurInSage`, einer `NichtNoetig`. Benutzer: admin, `stock_read`, `stock_keyuser`, Einkauf ohne Lager-Recht.
- **189.1** admin öffnet Lager → Sage-Buchungen: Titel, beide Herkünfte, Status nach F2, DB-Gegenprobe
  `SELECT Status, COUNT(*) FROM SageBookingQueueItems GROUP BY Status; SELECT SageWeStatus, COUNT(*) FROM PurchaseOrderReceipts GROUP BY SageWeStatus`.
- **189.2** Filter Herkunft, Status, nur offene, Bestellung, von/bis einzeln und kombiniert; Spaltenfilter Fehler enthält „Timeout“ + Seite 2.
- **189.3** Zeitraum-Standard: erledigte Zeile > 30 Tage (per SQL auf DEV zurückdatiert) fehlt, offene gleichen Alters da, Hinweis mit Anzahl.
- **189.4** Vorgang mit 3 Positionen: Knöpfe nur an der ersten Zeile, Rest „gehört zu Vorgang …“; Spaltenfilter auf die 2. Position: Knöpfe dort.
- **189.5** Keyuser: Lagerbuchung „Erneut senden“, WE „Als übergeben markieren“ (leer, 46 Zeichen, gültig), „CSV erneut schreiben“; jeweils
  Meldung + Rücksprung mit allen Filtern.
- **189.6** Zahnrad: WE-Spalten aus/ein, nach Neuladen gespeichert; Lagerbuchungs-Zeilen dort leer.
- **189.7** Nacherfasste Zeile: „Erfasst von“ mit „(nacherfasst)“.
- **189.8** Umleitungen: alte Lager-URL mit `status=Klaeren&bestellung=…`, alte Einkauf-URL, Links in `/ServiceSettings`, Hinweis in „Wareneingang zu
  Bestellung“ nennt „Sage-Buchungen“.
- **Negativ 189.9** `stock_read`: keine Knöpfe; POST `SageWeErneutSenden` per Werkzeug → 403.
- **Negativ 189.10** `WareneingangBestellungAktiv` aus (DEV IDEAL): keine WE-Zeilen, keine Auswahl „Wareneingang“, keine WE-Spalten im Zahnrad.
- **Negativ 189.11** `NurInSage` und `NichtNoetig` erscheinen nicht als WE-Zeile; `NichtNoetig` nur als Einbuchung.
- **Negativ 189.12** Einkauf ohne Lager-Recht: Menü Einkauf → Sage-Wareneingänge (offen) – Ergebnis nach Rückfrage 12 (bei (a): nur WE-Zeilen,
  „nur offene“ gesetzt, keine Knöpfe, keine Lagerbuchungen).
- **Negativ 189.13** Leere Datenlage: „Keine Einträge.“, Pagination 0.
- **189.14** Kontrast/Bedienung: Badges mit Text, `text-bg-warning`/`text-bg-info` lesbar (≥ 4,5:1), 390 px Breite mit genau einem Scrollbalken.

**Umstellungsliste bestehender Szenarien** (Pfad „Lager → Sage-Wareneingänge“ / „Sage-Lagerbuchungen“ → „Lager → Sage-Buchungen“, ggf. „Herkunft
Wareneingang“; Fundstellen `docs/TESTSZENARIEN.md` im Worktree): Kap. 56 Überschrift/Schritte (`:6308`), `:679`, 116.5 (`:11412`), `:11421`
(Testname), 116.23 (`:11437`), **116.24 (`:11438`) als überholt durch TS-189 markieren** (Menüblock mit zwei Einträgen gibt es nicht mehr), 116.26
(`:11440`), `:11443`, 129.10 (`:11740`), 132.3.2 (`:12007`), 137.2 (`:12268`), `:12286` (Testname), 153.8 (`:12817`), `:12885`, `:12900`, `:12908`
(Testname `SageWareneingaengeListeTests.Einkauf_NurOffene_*` → neuer Name), 178.6 (`:13634`). Reine Sage-Begriffe („Sage-WE auf DEV aus“,
`:12297`, `:12569`) bleiben.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

**Final (QA 2026-11-25, aus dem echten Diff `0725fc3e^..0725fc3e` + Doku-Commit `2c20cfea`):**
- **Web-App: ja** (Controller, Filter `RequireStockReadOrEinkaufAccess`, Repositories, View, Navigation, Hilfe, Changelog).
- **Service: nein** (unter `IDEALAKEWMSService/` nur `AppVersion.cs` 1.99.66; mitliefern schadet nicht, das Paket enthält beide).
- **Migration: nein** (keine Datei unter `*/Migrations/`, kein `SQL/`).
- **Manifest:** `v1.99.66-SageBuchungenInfo` (Info, beide, Nachher, anzeigen) steht in `scripts/deploy/nachlauf.psd1` – geprüft.
- **Publish erledigt:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (Web + Dienst, `SQL\`, `deploy\`,
  `publish-info.txt` Version 1.99.66, Commit `2c20cfeae977c52341c12d7ca06dd79bf67019df`). Ablauf Mensch: publish → Testsystem → testen → Merge.
- Reihenfolge: keine DB-Schritte; Web-App tauschen, danach Info-Schritt des Manifests (Lager/Einkauf informieren).
- Nach dem Merge nur dann neu aus `main` publishen, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat:
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`.

**Vorläufig (Spec-Agent)** – durch den Abschnitt oben ersetzt.

- **Web-App:** ja (Controller, View, Repositories, Navigation/Layout, Hilfe, Changelog, Version in beiden `AppVersion.cs`).
- **Service:** nein (kein Dienst-Code; `IDEALAKEWMSService/AppVersion.cs` wird nur hochgezählt).
- **Migration:** nein.
- **Manifest-Eintrag** (`scripts/deploy/nachlauf.psd1`), sonst keine Schema-, Daten-, Einmal-, Prüf- oder Einstellungs-Einträge:
  - Id `v<Version>-SageBuchungenInfo`, Version = vergebene Version, Typ `Info`, Firma `beide`, Phase `Nachher`, Ausführung `anzeigen`.
  - Text: „Lager/Einkauf informieren: ‚Sage-Lagerbuchungen‘ heißt jetzt ‚Sage-Buchungen‘ und enthält auch die Sage-Wareneingänge mit allen Knöpfen
    (Erneut senden, Als übergeben markieren, CSV erneut schreiben). Der Menüpunkt ‚Sage-Wareneingänge‘ entfällt, alte Links leiten um.
    ‚Nur in Sage buchen‘ steht nicht mehr in der Liste.“
  - Der alte Eintrag `nachlauf.psd1:454` (v1.99.7) bleibt als Historie.
- **Publish** (im Worktree): `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`.
- **Doku/Hilfe – Umstellungsliste (DoD):**
  - `docs/anwender/05-wareneingang-zu-bestellung.md`: Rechte-Tabelle `:19-22`, Abschnitt Liste `:150-165` mit **neuem Foto** statt
    `bilder/05-05-sage-wareneingaenge.jpg` (`:153`), Bekannte Effekte `:195`.
  - `docs/anwender/09-einbuchen-ausbuchen-umbuchen.md:196`, `docs/anwender/18-startseite-und-menue.md:70` (Menütext) + Foto der Seite per Playwright gegen DEV (nur ansehen).
  - `docs/betrieb/04-konfiguration.md:160`, `:218`; `05-schnittstellen.md:175`; `06-logs-ueberwachung.md:24-25`, §5 `:167-177` und §6 `:184-190`
    zusammenführen, Tabelle `:229-230`; `07-troubleshooting.md:51`, `:53`, `:70`; `08-diagnose-abfragen.md:203`, `:251`, `:268`; `10-backup-restore.md:83`.
    `docs/betrieb/AGENTS.md:76` bleibt (Begriff, keine Seite).
  - `README.md:534`, `:757`, `:767`, `:796` (und `:1052` prüfen).
  - App: `Views/Help/Index.cshtml:143-150`, `:170`, `:308-329`, `:497`; `Views/Users/RoleOverview.cshtml:221-226`; `Views/ServiceSettings/Index.cshtml:62`, `:68`;
    `Views/PurchaseOrders/Receive.cshtml:263`. `Views/Help/Changelog.cshtml` nur neuer Eintrag (Historie bleibt).
  - Brain (Hauptcheckout, Dev-Lauf): [[controller]] (Abgeloeste Routen, Rechte), `codebase/module.md`/`feature-map.md`, Testindex.

## Offene Rueckfragen

Fragen 1–11: Liste Gerald 2026-11-24, beantwortet „ok, wie Empfehlungen“ (siehe Freigabe-Antworten). Sie ersetzen die Fragen 1–7 des Entwurfs
vom 2026-11-23 (unten eingeklappt als Historie).

1. Eine Seite „Sage-Buchungen“ (heutige SageBookingQueue, Route/Menü-Key/Favoriten bleiben), übernimmt alle Funktionen beider Seiten.
2. Inhalt: Einbuchung, Ausbuchung, Lagerbestellungs-Entnahme, Umbuchung (wenn Schalter an), Wareneingang mit Sage-WE; Spalte + Filter „Herkunft“.
3. Funktionen: Lagerbuchungen „Erneut senden“ wie heute; WE-Zeilen „Erneut senden“, „Als übergeben markieren“ (mit Belegnummer), „CSV erneut
   schreiben“ wie heute auf der WE-Seite; Rolle wie heute (RequireStockKeyUserAccess). Bestehende POST-Actions wiederverwenden, Redirect zurück auf
   die Gesamtsicht mit Filtern.
4. WE-Aktionen gelten je Sage-WE-Vorgang: eine Zeile je Position, Knöpfe nur an der ersten Position des Vorgangs, übrige Zeilen „gehört zu Vorgang …“.
5. WE-Spalten (Lieferschein, Barcode, Positionsmemo, Memo, CSV) bleiben, per Zahnrad (ColumnDefinitions) standardmäßig ausgeblendet; bei
   Lagerbuchungen leer.
6. Alte Seite „Sage-Wareneingänge“ (Lager + Einkauf) → Umleitung auf Sage-Buchungen mit Filter Herkunft = Wareneingang (Einkauf: + „nur offene“),
   Muster „Abgelöste Routen“ (controller.md); Menüpunkt/Kachel „Sage-Wareneingänge“ entfällt aus NavigationKatalog (ADR 0034, Fallstrick 58),
   Favoriten darauf landen über die Umleitung; Links aus Receive.cshtml u. a. nachziehen (alle Verweise belegen).
7. Rechte: Seite = stock_read; WE-Zeilen nur bei aktivem Modul WareneingangBestellungAktiv; Aktionen Keyuser/Admin.
8. Gemeinsamer Status wartet/gesendet/übergeben/Fehler/klären + Originaltext; „nur in Sage buchen“ (NurInSage) NICHT anzeigen.
9. FA-Arbeitsgänge (IDEAL) nicht dabei.
10. Standard-Zeitraum 30 Tage, offene Zeilen (wartet/gesendet/Fehler/klären) immer + Hinweis mit Anzahl älterer offener.
11. „nacherfasst“-Zusatz in „Erfasst von“.
12. **NEU – Einkauf ohne Lager-Recht (Lücke zwischen Antwort 6 und 7).** Die Einkauf-Sicht läuft heute unter `[RequireEinkaufAccess]` +
    `[RequireEinkaufAktiv]` und ausdrücklich **ohne** Lager-Recht (`EinkaufPurchaseOrdersController.cs:9-15`; Rolle einkauf ist nicht im
    stock_read-Kreis, `CurrentUserService.cs:104-105` vs. `:113-114`). Nach Antwort 6 leitet sie auf Sage-Buchungen um, nach Antwort 7 ist die
    Seite stock_read – ein Einkauf-Benutzer ohne Lager-Recht bekäme 403. Optionen:
    - **(a) Empfehlung:** Sage-Buchungen zusätzlich für Einkauf lesbar (Rolle einkauf + `EinkaufAktiv`, neuer kombinierter Lesefilter, drei Stellen).
      Einkauf ohne Lager-Recht sieht nur WE-Zeilen (ohne Modulbedingung, wie heute), keine Lagerbuchungen, keine Knöpfe. Der Einkauf-Menüpunkt
      „Sage-Wareneingänge (offen)“ bleibt und landet über die Umleitung mit „nur offene“. Hält „eine Seite“ und nimmt niemandem etwas weg.
    - (b) Einkauf-Ansicht bleibt unverändert (nur die Lager-Seite wird zusammengeführt) – weniger Code, aber zwei Listen bleiben.
    - (c) Umleitung wie beschlossen; Einkauf-Benutzer brauchen künftig `stock_read` (Rollenpflege), Einkauf-Menüpunkt entfällt.

> [!faq]- Historie: Fragen 1–7 des Entwurfs vom 2026-11-23 (ersetzt durch die Liste vom 2026-11-24)
> 1. Welche Maske ist gemeint: „Sage-Lagerbuchungen“ oder „Sage-Wareneingänge“? Empfehlung Sage-Lagerbuchungen.
> 2. Variante A (Sage-Lagerbuchungen wird lesende Gesamtsicht) oder B (neue Seite „Sage-Übergaben“)? Empfehlung A.
> 3. FA-Arbeitsgänge (IDEAL) einbeziehen? Empfehlung ja.
> 4. Rollen: Zeilen je Weg nur mit Recht auf die Fachseite? Empfehlung ja.
> 5. Standard-Zeitraum 30 Tage, nicht erledigte immer, „von Hand in Sage“ = erledigt?
> 6. (nur A) Umbenennen in „Sage-Buchungen“? Empfehlung ja.
> 7. Wareneingänge „nur in Sage buchen“ mitzeigen? Empfehlung ja.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

Antwort Gerald 2026-11-24 zu 1–11: „ok, wie Empfehlungen“.

1. Eine Seite „Sage-Buchungen“ (heutige SageBookingQueue, Route/Menü-Key/Favoriten bleiben), übernimmt alle Funktionen beider Seiten. → **ok, wie Empfehlungen (Gerald 2026-11-24)**
2. Inhalt: Einbuchung, Ausbuchung, Lagerbestellungs-Entnahme, Umbuchung (wenn Schalter an), Wareneingang mit Sage-WE; Spalte + Filter „Herkunft“. → **ok, wie Empfehlungen (Gerald 2026-11-24)**
3. Funktionen: Lagerbuchungen „Erneut senden“ wie heute; WE-Zeilen „Erneut senden“, „Als übergeben markieren“ (mit Belegnummer), „CSV erneut schreiben“ wie heute auf der WE-Seite; Rolle wie heute (RequireStockKeyUserAccess). Bestehende POST-Actions wiederverwenden, Redirect zurück auf die Gesamtsicht mit Filtern. → **ok, wie Empfehlungen (Gerald 2026-11-24)**
4. WE-Aktionen gelten je Sage-WE-Vorgang: eine Zeile je Position, Knöpfe nur an der ersten Position des Vorgangs, übrige Zeilen „gehört zu Vorgang …“. → **ok, wie Empfehlungen (Gerald 2026-11-24)**
5. WE-Spalten (Lieferschein, Barcode, Positionsmemo, Memo, CSV) bleiben, per Zahnrad (ColumnDefinitions) standardmäßig ausgeblendet; bei Lagerbuchungen leer. → **ok, wie Empfehlungen (Gerald 2026-11-24)**
6. Alte Seite „Sage-Wareneingänge“ (Lager + Einkauf) → Umleitung auf Sage-Buchungen mit Filter Herkunft = Wareneingang (Einkauf: + „nur offene“), Muster „Abgelöste Routen“ (controller.md); Menüpunkt/Kachel „Sage-Wareneingänge“ entfällt aus NavigationKatalog (ADR 0034, Fallstrick 58), Favoriten darauf landen über die Umleitung; Links aus Receive.cshtml u. a. nachziehen (alle Verweise belegen). → **ok, wie Empfehlungen (Gerald 2026-11-24)**
7. Rechte: Seite = stock_read; WE-Zeilen nur bei aktivem Modul WareneingangBestellungAktiv; Aktionen Keyuser/Admin. → **ok, wie Empfehlungen (Gerald 2026-11-24)**
8. Gemeinsamer Status wartet/gesendet/übergeben/Fehler/klären + Originaltext; „nur in Sage buchen“ (NurInSage) NICHT anzeigen. → **ok, wie Empfehlungen (Gerald 2026-11-24)**
9. FA-Arbeitsgänge (IDEAL) nicht dabei. → **ok, wie Empfehlungen (Gerald 2026-11-24)**
10. Standard-Zeitraum 30 Tage, offene Zeilen (wartet/gesendet/Fehler/klären) immer + Hinweis mit Anzahl älterer offener. → **ok, wie Empfehlungen (Gerald 2026-11-24)**
11. „nacherfasst“-Zusatz in „Erfasst von“. → **ok, wie Empfehlungen (Gerald 2026-11-24)**
12. Einkauf ohne Lager-Recht: (a) Sage-Buchungen auch für Einkauf lesbar (nur WE-Zeilen, keine Knöpfe), (b) Einkauf-Ansicht bleibt, (c) Einkauf braucht stock_read? → **(a), Gerald 2026-11-24: „12a ja, passt“. Festlegungen des Spec-Agents (Knöpfe an der ersten Zeile des Vorgangs auf der angezeigten Seite; NurInSage in keiner Liste mehr; Umleitung betrifft nur Browser-Lesezeichen) ebenfalls bestätigt.**

## QA-Nachweis (qa-agent, 2026-11-25)

Geprüft: Umsetzung `0725fc3e` (v1.99.66) im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch
`feature/2026-08-07-ideal-teile-1-5`. QA-Commit im Worktree: `2c20cfea` (Foto 05-05 neu + Bildunterschrift + Stand in `docs/anwender/README.md`).
Brain (Hauptcheckout): [[controller]] um `RequireStockReadOrEinkaufAccess` und die beiden 301-Umleitungen ergänzt (Befund B1).

### 1. Build und Tests

- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 30 Warnungen (bestehend).
- `dotnet test`: `IdealAkeWms.Tests` 3519 bestanden / 1 übersprungen / 0 Fehler (3520); `IDEALAKEWMSService.Tests` 790 / 0 / 0.

### 2. Code-Review `0725fc3e^..0725fc3e` (selbst, in diesem Lauf)

Angesehen: `SageBookingQueueController` (Index, Requeue, `Zurueck`), `PurchaseOrdersController.SageWe.cs` (Umleitung, drei POST-Actions),
`EinkaufPurchaseOrdersController`, neuer Filter `RequireStockReadOrEinkaufAccessAttribute`, beide Repository-Abfragen (Zeitraum-Regel),
`SageBuchungenGesamtsicht`, ViewModel, `Views/SageBookingQueue/Index.cshtml`, `ColumnDefinitions`, `ColumnFilterSyntax`, `NavigationKatalog`,
`_Layout`, `ServiceSettings`/`Receive`/`RoleOverview`, Manifest, Tests.

- **Rollen/Read-Edit-Split:** Index `[RequireStockReadOrEinkaufAccess]` (Action), Requeue `[RequireStockReadAccess]` + `[RequireStockKeyUserAccess]`;
  WE-POSTs weiter hinter dem Klassen-Gate des `PurchaseOrdersController` (stock_read + `WareneingangBestellungAktiv`) + `[RequireStockKeyUserAccess]`.
  Knöpfe: `CanRequeue = stockRead && CanTransfer`, `CanWeAktionen = CanRequeue && Modul` – Einkauf ohne Lager-Recht sieht nie Knöpfe. OK.
- **Paging/Spaltenfilter (ADR 0005):** Filterkarte und `ColumnFilterHelper.Apply` über die ganze Union, danach `Skip/Take`; `MarkiereErsteJeVorgang`
  nach dem Paging; Seitenklemmung zentral über `PaginationSeiteKlemmenFilter` (Fallstrick 50, auf DEV bestätigt). OK.
- **returnUrl/Open-Redirect:** `Zurueck` nutzt `Url.IsLocalUrl` + `LocalRedirect` (doppelt abgesichert), sonst `RedirectToAction`. Test
  `Aktion_Ruecksprung_LokaleReturnUrl_SonstSageBuchungen` mit fremder URL. OK.
- **301:** `RedirectToActionPermanent`, Status über `SageBuchungenGesamtsicht.Anzeige` übersetzt; die Einkauf-Instanz `_listen` ohne ControllerContext
  ist unkritisch (UrlHelper entsteht beim Ausführen) – auf DEV bestätigt.
- Script-Blöcke: `#column-config` über `@Json.Serialize`, `#view-config` statisch – OK. Kein `hidden` + `d-*`. Keine stillen `?? 0`-Fallbacks
  (`Queue?.… ?? We!.…` ist Quellenwahl). Keine Parallel-Arrays. Belegnummer-Länge serverseitig geprüft (nicht nur `maxlength`).
- Eine Regel an mehreren Stellen: Zeitraum-Regel in beiden Repositories (LINQ) und `ImZeitraum` (nur für den Hinweis-Zähler), Kommentare verweisen
  aufeinander; „offen“ = nicht Bestätigt/Übergeben überall gleich. Die Queue-Abfrage ist InMemory nicht getestet, auf DEV aber bestätigt.

**Befunde**

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| B1 | mittel | Neuer Filter `RequireStockReadOrEinkaufAccess` fehlte in [[controller]] (Drei-Stellen-Regel; Attribut und `RoleOverview` waren da). | **Behoben durch QA** im Hauptcheckout (Filtertabelle + Abschnitt „Abgeloeste Routen“). |
| B2 | niedrig | Einleitungstext: zwischen den beiden `<text>`-Blöcken fehlt ein Leerzeichen („…(kein Doppelbuchen).Wareneingänge gehen …“), `Views/SageBookingQueue/Index.cshtml`, Einleitungs-`<p>`. | Offen, kosmetisch – beim nächsten Anfassen der View; kein Blocker. |
| B3 | niedrig (Hinweis) | Admin bzw. Rolle einkauf sieht bei `EinkaufAktiv` = true WE-Zeilen auch mit `WareneingangBestellungAktiv` = aus (ODER-Regel F7/Rückfrage 12a; Knöpfe dann aus). AK 10 („Modul aus: keine WE-Zeilen“) gilt damit nur für Benutzer ohne Einkauf-Lesekreis. | Spec-konform (F7 „ohne Modulbedingung, wie heute“), akzeptiert; Checkliste Punkt 5. |
| B4 | Hinweis (DEV) | Per SQL auf „Fehler“ gesetzte Sage-WE-Zeilen stellt der Dienst bei ausgeschaltetem Sage-Wareneingang binnen Sekunden auf „klären“ (Q-2, `system:sage-wareneingang`). Kein Fehler; 189.4 wurde mit „klären“ geprüft. | Keine Aktion. |
| B5 | Hinweis (Lauf) | Brain-Changelog `secondbrain/changelog/…v1-99-66…` und Eintrag in `feature-map.md` fehlen noch (ebenso für v1.99.65). | Gehört zum Lauf-Abschluss (goal-Lauf), nicht zur QA. |

**Bewertung der Abweichungen des Umsetzers**
1. Einkauf sieht WE-Zeilen schon mit `EinkaufAktiv` ohne Modul – entspricht F7 + Rückfrage 12a („ohne Modulbedingung, wie heute“); Knöpfe korrekt am Modul. **Akzeptiert** (siehe B3).
2. `?herkunft=Wareneingang` ohne WE-Recht wird ignoriert → Lagerbuchungen. Unschädlich (die alte Lager-URL liegt ohnehin hinter dem Modul-Gate), in TS-189.10 so beschrieben. **Akzeptiert.**
3. Foto 05-05 alt – **erledigt durch QA** (`2c20cfea`, headless Edge gegen DEV AKE, nur angesehen).
4. Testdateiname `SageWareneingaengeListeTests.cs` bleibt – Testindex und TS verweisen darauf. **Akzeptiert.**
5. `docs/betrieb/04` ohne eigenen Stempel – Stand steht zentral in `docs/betrieb/00-index.md` (im Diff). **Akzeptiert.**

### 3. Nachweisart je AK

| AK | Nachweis |
|---|---|
| 1 Menü/Titel | `NavigationSichtTests` (Titel) + DEV OK |
| 2 Union, NichtNoetig/NurInSage | `SageWareneingaengeListeTests.Liste_OhneNichtNoetig…`, `Build_WeOhneAnzeige…` + DEV OK |
| 3 Status-Zuordnung | automatisiert (`Anzeige_*_JederWert`, neuer Enum-Wert bricht) |
| 4 Herkunft | `Herkunft_Lagerbestellung_VorBewegungsart` + DEV OK |
| 5 Requeue Lagerbuchung | Rücksprung automatisiert (an der WE-Action); Requeue selbst **Manual-UAT** (auf DEV nicht ausgelöst, kein Senden) |
| 6 3 Positionen, Knöpfe erste Zeile | `ErsteZeileJeVorgang_TraegtDieKnoepfe…` + DEV OK (mit „klären“) |
| 7 Als übergeben (leer/46/gültig) | automatisiert (`AlsUebergeben_*`) + DEV „gültig“ OK |
| 8 CSV erneut schreiben | DEV OK (Manual) |
| 9 Zahnrad | Manual, DEV OK |
| 10 Rechte | `Einkauf_OhneLagerRecht…`, `ModulAus_KeineWeZeilen…` automatisiert; DEV IDEAL Modul aus OK; `stock_read` ohne Keyuser + POST verweigert **Manual-UAT offen** |
| 11 Zeitraum | `Zeitraum_ErledigteAelterAls30TageWeg…` (WE-Abfrage) + DEV OK (Queue-Abfrage nur DEV) |
| 12 nacherfasst | `ErfasstVon_Nacherfasst…` + DEV OK |
| 13 Umleitungen 301 | `Umleitung_AlteLagerUrl_301…` (Permanent) + DEV Ziel-URL OK |
| 14 Filter vor Paging, Klemmen | DEV OK (Manual; InMemory teilweise) |
| 15 Hinweise | DEV: TLS-Banner, Q-2-Hinweis, Knopf „Wareneingang zu Bestellung“ OK; CSV-Hinweise nicht prüfbar (CSV auf DEV aus) |
| 16 Build/Tests | grün (s. o.) |

### 4. Geänderte bestehende Tests

- `PurchaseOrdersControllerTests` (Wächter liest jetzt `Views/SageBookingQueue/Index.cshtml`) – Verhalten absichtlich geändert (F5, View umgezogen).
- `NavigationSichtTests` (Titel „Sage-Buchungen“) – absichtlich geändert (AK 1).
- `SageWareneingaengeListeTests`: Aufrufe auf `SageBookingQueueController.Index` bzw. den `returnUrl`-Parameter umgestellt (Signatur-Anpassung);
  `Einkauf_NurOffene_…_v1_99_31_F2a` ersetzt durch `Einkauf_OhneLagerRecht…_Rueckfrage12` – absichtlich geändert (Rückfrage 12a, NurInSage nicht mehr
  gelistet, AK 2/10). Keine Fixture „grün gebogen“.

### 5. Checkliste CLAUDE.md / ADR 0020

Keine Migration, kein SQL, kein FreshInstall/AgentJobs-Bezug. Audit-Felder: nur bestehende Schreibwege (DEV: `ModifiedBy`/`ModifiedByWindows` gesetzt).
Version 1.99.66 in beiden `AppVersion.cs`, Anwender-Changelog ergänzt. TS-189 in `docs/TESTSZENARIEN.md`, Indexzeile 189 vorhanden.
Einstellungen: nur Verwendungstexte geändert, keine neue/geänderte Einstellung – ADR 0020 nicht berührt. Brain-Changelog/feature-map: B5.

### 6. DEV-Deploy und Testlauf

`standort: beide` (Frontmatter). Elevated Shell, `dev-deploy.ps1 -Quelle <Worktree>` je Standort, Exit 0:
- AKE: „HTTP 200, Fusszeile v1.99.66 … Fertig: AKE v1.99.66 auf http://localhost:8081/“
- IDEAL: „HTTP 200, Fusszeile v1.99.66 … Fertig: IDEAL v1.99.66 auf http://localhost:8082/“

Testdaten (DEV AKE, per SQL, danach zurückgesetzt): Receipts 37–39 ein Vorgang (Fehler → vom Dienst „klären“), 20 „klären“, 19 `NurInSage`,
CSV-Export 3 auf Fehler, Queue 14 als Lagerbestellung (WarehouseRequisitionItemId 155), 15 Bestätigt im Zeitraum, 16 nacherfasst; Queue 1–6 (Fehler,
August) = offene außerhalb des Zeitraums. Erwartung vorher gerechnet, Ergebnis deckungsgleich.

| Schritt | Ergebnis | |
|---|---|---|
| 189.1 | Titel „Sage-Buchungen“, Menü Lager ohne „Sage-Wareneingänge“, 16 Zeilen beider Herkünfte (9 Queue + 7 WE), Badges/„Status im Weg“ passend zur DB-Abfrage | OK |
| 189.2 | Herkunft WE 7, Lagerbestellung 1; Status Fehler 6 / klären 4; nur offene 10; Bestellung „168715“ 2 (nur WE); von=bis=12.11. 12 Zeilen + Hinweis 10; Spaltenfilter Fehler „Timeout“ 3; `page=3` → auf Seite 1 geklemmt | OK |
| 189.3 | Ohne „von“: Feld 26.10.2026, erledigte Queue 7–13 (Sept.) fehlen, offene 1–6 (Aug.) da, Hinweis „6 offene Buchungen außerhalb …“ | OK |
| 189.4 | Vorgang WE#37#: Knöpfe nur an 406229, Rest „gehört zu Vorgang WE#37#“; `colf_pos=406230` → 1 Treffer mit Knöpfen | OK |
| 189.5 | „Als übergeben markieren“ WE#20# mit `QA-189-TEST` → Meldung „1 Position(en) … markiert“, DB Status 3 + Beleg + Modified*, Rücksprung auf dieselbe URL inkl. `colf_pos`; „CSV erneut schreiben“ → Meldung, Export Status 3→1, Rücksprung OK. Leer/46 Zeichen nur automatisiert | OK |
| 189.5 | Lagerbuchung „Erneut senden“, WE „Erneut senden“ | **NICHT MOEGLICH** (Auftrag: keine Requeue-/Sende-Aktion) |
| 189.6 | Zahnrad: fünf WE-Spalten standardmäßig aus, Herkunft/Status gesperrt; Lieferschein ein → nach Neuladen sichtbar, wieder aus | OK |
| 189.7 | Queue 16: „Gerald Weichbold (nacherfasst)“ | OK |
| 189.8 | `/PurchaseOrders/SageWareneingaenge?status=Klaeren&bestellung=168965&von=2026-11-01` → `/SageBookingQueue?herkunft=Wareneingang&status=Klaeren&bestellung=168965&von=2026-11-01`; Einkauf-URL → `?herkunft=Wareneingang&nurOffene=True`; `/ServiceSettings` „Zu den Sage-Buchungen“; Receive-Hinweis im Diff. 301-Code nur per Unit-Test | OK |
| 189.9 | `stock_read`-Benutzer | **NICHT MOEGLICH** (kein zweiter Test-Login in diesem Lauf) |
| 189.10 | DEV IDEAL, `WareneingangBestellungAktiv` kurz auf false (danach wieder true): keine Auswahl „Wareneingang“, kein Feld Bestellung, keine WE-Spalten; `?herkunft=Wareneingang` → Lagerbuchungen | OK |
| 189.11 | `NurInSage` (19) und `NichtNoetig` nicht als WE-Zeile | OK |
| 189.12 | Einkauf ohne Lager-Recht | **NICHT MOEGLICH** (kein Einkauf-Test-Login); automatisiert über `Einkauf_OhneLagerRecht…` |
| 189.13 | Herkunft WE + Status Fehler bzw. IDEAL: „Keine Einträge.“, Pagination „Keine Eintraege“ | OK |
| 189.14 | Badges mit Text (Bild 05-05) OK; 390 px | **NICHT MOEGLICH** (Headless-Fenster rendert nicht unter ~500 px, Chrome-Screenshot reagierte nicht) |

### 7. Publish

`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` (Zielordner vorher geleert): `IDEALAKEWMSWeb`, `IDEALAKEWMSWebService`, `SQL`,
`deploy`, `publish-info.txt` (Version 1.99.66, Commit `2c20cfeae977c52341c12d7ca06dd79bf67019df`). Manifest-Eintrag geprüft.

### Manuelle Test-Checkliste (Mensch, Schranke 2)

1. 189.5 Lagerbuchung „Erneut senden“ mit Filter (z. B. Status Fehler, Seite 2) – Rücksprung mit Filtern; nur bei ausgeschaltetem Dienst oder gegen Sage-Test.
2. 189.5 WE „Erneut senden“ (Q-2-Sperre bei ausgeschaltetem Sage-WE, sonst Einreihen) – nur mit Sage-Test.
3. 189.9 als `stock_read` ohne Keyuser: Liste ohne Knöpfe; POST `SageWeErneutSenden`/`Requeue` per Werkzeug → Zugriff verweigert, nichts geändert.
4. 189.12 als Einkauf ohne Lager-Recht: Einkauf → Sage-Wareneingänge (offen) → nur WE-Zeilen, „nur offene“, keine Knöpfe, kein „Wareneingang zu Bestellung“.
5. 189.10 zusätzlich als admin bei `EinkaufAktiv` = true und Modul aus (B3: WE-Zeilen sichtbar, ohne Knöpfe – so gewollt?).
6. 189.14 bei 390 px Breite: genau ein Scrollbalken, keine Seiten-Querscrollleiste.
7. CSV-Hinweise (AK 15) mit eingeschalteter CSV-Übergabe auf dem Testsystem.
8. Testsystem-Lauf mit dem Paket aus `publish\` (Web tauschen, Manifest-Info), danach Merge.
