---
type: spec
title: Lager-Tools – FA als verknüpftes Feld, Prüfung, Auswahl und Link; Bestandsübersicht Artikel-Scan ohne FA
slug: 2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec
status: Testbereit
created: 2026-11-08
updated: 2026-11-08
source_backlog: "[[2026-11-07-lagerbuchungen-fa-feld-und-verlinkung]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
version: v1.87.0
task: "[[2026-11-08-buendel-2026-11-08-goal-lauf-2]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - "IdealAkeWms/Models/StockMovement.cs:20-22 (neu ProductionOrderId + Navigation LinkedProductionOrder; Text ProductionOrder bleibt)"
  - "IdealAkeWms/Data/ApplicationDbContext.cs:376-424 (FK ON DELETE SET NULL, Index)"
  - "IdealAkeWms/Migrations/<ts>_AddStockMovementProductionOrderId.cs (neu)"
  - "SQL/109_AddStockMovementProductionOrderId.sql (neu), SQL/110_BackfillStockMovementProductionOrderId.sql (neu, Trockenlauf-Default)"
  - "SQL/00_FreshInstall.sql:271-301 (Spalte+FK), :1366-1377 (Index), MigrationId-Block"
  - "IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs:55 + ProductionOrderRepository.cs:193-222 (neu FindForBookingAsync, SearchAsync wiederverwendet)"
  - "IdealAkeWms/Controllers/StockMovementsController.cs:125-138, :232-245, :336-349, :425-439, :500-513 (FK setzen, Pflicht, Warnung), Ctor :21-37"
  - "IdealAkeWms/Controllers/StockApiController.cs (neu GET fa-lookup, fa-search)"
  - "IdealAkeWms/Views/StockMovements/{Inbound,Outbound,Transfer,InboundBulk}.cshtml (FA-Feld: Prüfkarte, Auswahlliste)"
  - "IdealAkeWms/Data/Repositories/StockMovementRepository.cs:297-383 (Projektion FA-Felder), Models/ViewModels/MovementHistoryViewModel.cs:39"
  - "IdealAkeWms/Views/StockMovements/Index.cshtml:89, :134 (FA-Spalte mit Link + Status)"
  - "IdealAkeWms/Controllers/StockOverviewController.cs:42-99 + Views/StockOverview/Index.cshtml:17-18, :45 (FA-Kopf, Artikel-Scan ohne FA)"
  - "IdealAkeWms/Models/AppSettingDefinitions.cs:71-101 (neu FaPflichtBuchungsarten; QrMitFaNummer Verwendung)"
  - "IdealAkeWms/Services/ICurrentUserService.cs + Filters/RequirePickingOrTrackingOrLeitstandAccessAttribute.cs:26-29 (CanViewProductionOrdersAsync, eine Regel)"
  - "IdealAkeWms/wwwroot/js/fa-feld.js (neu, klein)"
  - "IdealAkeWms/Views/Help/Index.cshtml, Views/Help/Changelog.cshtml, README.md:247, docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md:51-57"
  - "docs/TESTSZENARIEN.md TS-115 neu, TS-101.7/101.22 anpassen"
  - "Tests: IdealAkeWms.Tests (Resolver, Controller, API, Waechter; HardwareScannerTests.Bestandsuebersicht_QrFaUebernahme_FolgtQrMitFaNummer_NichtFest wird geaendert)"
open_questions:
  - "Konflikt Altdaten-Quelle: Entscheidung 2 sagt 'FA-Nummern per Muster aus Bemerkungen ziehen'; am Code steht der FA seit jeher im eigenen Textfeld StockMovements.ProductionOrder, die Notiz (Note) schreibt nur der Sage-Bestandsabgleich. Altbestand aus dem Textfeld ProductionOrder exakt auflösen und die Notiz nur im Trockenlauf zählen? Empfehlung ja."
  - "Bestandsübersicht: Artikel-QR-Knopf übernimmt nie mehr den FA, unabhängig von QrMitFaNummer (nimmt die Bestandsübersicht-Hälfte von v1.76.0 zurück; an beiden Standorten sichtbar, weil QrMitFaNummer dort an ist)? Empfehlung ja."
  - "Pflicht-Semantik: Bei eingeschalteter FA-Pflicht muss der FA eindeutig im WMS gefunden werden; erledigt/storniert/in Sage vermisst nur Warnung, keine Sperre? Empfehlung ja."
  - "IDEAL Sub-FA: Auswahlliste bietet nur Haupt-FAs (bestehende Suche), eine exakt eingegebene oder gescannte Sub-FA-Nummer wird aber verknüpft? Empfehlung ja; Sub-FA-Auswahl entscheidet das Rückmelde-Epic."
  - "Lagerplatz ausbuchen (OutboundAll, Kommissionierwagen): nur verknüpfen, wenn der Text genau einen FA ergibt; keine Prüfkarte, keine Pflicht? Empfehlung ja."
  - "Protokoll der Altdaten-Übernahme: Ergebnismengen des Skripts SQL/110 als Datei in der Deploy-Aufgabe ablegen statt einer neuen Protokolltabelle (SQL/111 bleibt frei)? Empfehlung ja."
  - "'Buchen für FA' (FA aus gefilterter Liste zuerst wählen), Werkbank-Filter in der Auswahl und Auswertung Materialverbrauch je FA: eigener Backlog-Punkt statt in dieser Spec? Empfehlung ja."
epic: true
etappen:
  - "E1 Datenmodell, Auflösungsregel, Schreibwege (SQL/109)"
  - "E2 Altdaten-Skript mit Trockenlauf (SQL/110)"
  - "E3 Buchungsmasken: Prüfung, Auswahl, Pflicht-Einstellung"
  - "E4 Anzeige und Link: Bewegungshistorie, Bestandsübersicht, Doku, Version"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-07"
---

## Ziel / Nutzen (das Warum)

Der FA an einer Lagerbuchung ist heute ein freier Text. Er wird nicht geprüft, nirgends verlinkt und kann nicht sicher
einem FA zugeordnet werden. Das Rückmelde-Epic ([[2026-11-02-sage-pps-fa-bde-abgleich-spec]], K6) und das
Kommissionier-Epic ([[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]], Teil C) brauchen aber einen echten
Verweis auf den FA. Ziel: eine Buchung kennt **den** FA (Fremdschlüssel), der Lagermitarbeiter sieht beim Buchen, ob der
FA existiert und offen ist, und überall, wo ein FA an einer Bewegung steht, führt ein Link zum FA.

## Befund am Code (Bündel-Worktree v1.78.0, nur gelesen)

### Der FA ist schon ein eigenes Feld, aber nur Text

- `StockMovement.ProductionOrder` (`string?`, `[StringLength(500)]`, Anzeige „Fertigungsauftrag“, `StockMovement.cs:20-22`).
  DB `NVARCHAR(500) NULL` (`00_FreshInstall.sql:281`, `SQL/18_ExtendProductionOrderLength.sql:7`), Index
  `IX_StockMovements_ProductionOrder` (`00_FreshInstall.sql:1375-1377`). Kein Fremdschlüssel.
- `StockMovement.Note` (`StockMovement.cs:40-42`, Anzeige „Notiz“) ist ein **anderes** Feld.

**Die Annahme der Backlog-Notiz („FA steckt im Bemerkungs-/Notizfeld“) trifft auf Lagerbuchungen nicht zu.** Belege über
alle Schreibwege (`new StockMovement` im Anwendungscode, Grep über `IdealAkeWms`/`IDEALAKEWMSService`, ohne Tests/Doku):

| # | Schreibweg | Datei:Zeile | `ProductionOrder` | `Note` |
|---|---|---|---|---|
| 1 | Wareneingang | `StockMovementsController.cs:125-138` | `vm.ProductionOrder` (Freitext, `:130`) | – |
| 2 | Sammel-Wareneingang | `:232-245` | Kopf-FA `vm.ProductionOrder` für alle Zeilen (`:237`) | – |
| 3 | Warenausgang | `:336-349` | `vm.ProductionOrder` (`:341`) | – |
| 4 | Umbuchung | `:425-439` | `vm.ProductionOrder` (`:431`) | – |
| 5 | Lagerplatz ausbuchen | `:500-513` | Freitext `productionOrder` (`:505`); vorbelegt bei Kommissionierwagen mit `string.Join("; ", …)` (`:468-473`) | – |
| 6 | Lagerplatz umbuchen | `:581-594` | – | – |
| 7 | Kommissionier-Transfer | `PickingTransferService.cs:293-307` | `order.OrderNumber` (`:70`, `:147`); bei Wagen-Konflikt mit `forceTransfer` zusammengeführt `string.Join(";", …)` (`:78`) | – |
| 8 | Sage-Bestandsabgleich | `LagerbestandSyncService.cs:179-192`, `:247-260` | – | `"Sage-Korrektur: …"` (`:185`, `:253`) |

`Note` wird nur vom Sage-Abgleich geschrieben (Grep `Note =`/`.Note` im Anwendungscode: nur `LagerbestandSyncService.cs:185/:253`;
Leser nur `StockMovementRepository.cs:378` → `Views/StockMovements/Index.cshtml:135`). Keine Buchungsmaske hat ein Notizfeld.
Die Spalte gibt es seit `SQL/57_AddStockMovementNoteAndSageMovementTypes.sql`. Ob vorher oder per Hand (SQL) anderes in
`Note` geschrieben wurde, kann der Code nicht beantworten → Prüfabfrage P2.

Das „Bemerkung“ in `Inbound.cshtml:96` ist die Spalte der Bedarfsmeldungen-Tabelle (`req.notes`, `:163`), nicht die Buchung.

### Wer liest den FA-Text

| Leser | Datei:Zeile | Art |
|---|---|---|
| Bewegungshistorie Filter + Spaltenfilter | `StockMovementRepository.cs:340-341`, `:588` | `Contains` bzw. Spaltenfilter-Syntax |
| Bewegungshistorie Anzeige | `StockMovementRepository.cs:377` → `Index.cshtml:134` | Text |
| Bestandsübersicht FA-Filter / Einbuchungs-Hinweis `api/stock/by-order` | `StockMovementRepository.cs:195-202`, `StockOverviewController.cs:49-54`, `StockApiController.cs:19-35` | `Contains` |
| Kommissionierwagen-FA-Liste | `StockMovementRepository.cs:500-522`, `PickingTransferService.cs:48`, `:73` | Distinct-Text |
| Sage-Enqueue (Umbuchung mit FA nie an Sage) | `SageBookingEnqueueingStockMovementRepository.cs:65-66`, `SageBookingEnqueueDecision.cs:64-72`, `SageBookingQueueRepository.cs:151` | „nicht leer“ |

Die `Contains`-Suche ist nötig, weil das Feld Listen („A; B“) enthalten kann (Wege 5 und 7).

### Sage-Lagerbuchungen (v1.28.0 / v1.73.0)

Der Payload enthält **keinen** FA (`SageLagerbuchungPayload.cs:10-29`, `SageLagerbuchungPayloadBuilder.cs:53-73`: Memo =
Korrelation, Standardtext fest, Zeile ohne FA). Der FA wirkt nur auf die Frage, **ob** eine Umbuchung eingereiht wird
(`hatFaBezug` = Text nicht leer). Diese Spec ändert weder Payload noch Entscheidung (siehe „Sage“ unten).

### FA-Stammdaten und Eindeutigkeit

- `ProductionOrder.OrderNumber` ist seit der Schema-Inversion **nicht** eindeutig, `SubOrderNumber` ist es
  (`ProductionOrder.cs:14-22`, `ApplicationDbContext.cs:473-474`, [[fallstricke]] „`ProductionOrder.OrderNumber` ist nach der
  Schema-Inversion NICHT mehr unique“). AKE: `SubOrderNumber == OrderNumber`. Haupt-FA-Regel nur über
  `ProductionOrder.IsHauptFa` (`:29-30`).
- Status: `IsDone` (`:84`), `IsCancelled` (`:103`), `SageMissingSince` (`:54`).
- Bestehende Suche `ProductionOrderRepository.SearchAsync` (`:203-222`): offene, nicht stornierte Haupt-FAs, Treffer auf
  Nummer/Artikel/Kunde, Limit. API `ProductionOrdersApiController.cs:19-33` ist aber nur `[RequirePickingAccess]` – die
  Rolle `stock` kommt nicht hinein ([[controller]] Z. 54, 61).
- Es gibt keine FA-Detailseite. Ziele für einen Link: FA-Liste `ProductionOrders/Index?filterOrderNumber=` (`ProductionOrdersController.cs:73-74`)
  und FA-Struktur `FaHierarchy/Index?filterHauptFa=` (`FaHierarchyController.cs:68-69`). Beide hinter
  `[RequirePickingOrTrackingOrLeitstandAccess]` (= `CanPick || CanViewTracking || CanManagePickingRelease || HasVorbauAccess`,
  `RequirePickingOrTrackingOrLeitstandAccessAttribute.cs:26-29`).
- Löschen von FAs: kein Löschweg im Anwendungscode (Grep `ProductionOrders.Remove|RemoveRange|DELETE … ProductionOrders` ohne
  Treffer außer Tests), **aber** der Rückbau-Runbook löscht Sub-FA-Zeilen (`docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md:56`) →
  Fremdschlüssel mit `ON DELETE SET NULL`.

### Überschneidung mit v1.76.0

[[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-spec]] hat die FA-Übernahme beim Artikel-QR der Bestandsübersicht an
`QrMitFaNummer` gebunden (`StockOverviewController.cs:95-97`, `StockOverview/Index.cshtml:18`). Laut P1 jener Spec ist
`QrMitFaNummer` an AKE und IDEAL an – der Artikel-Knopf setzt dort also weiterhin den FA-Filter, und der FA-Filter übersteuert
den Artikelfilter (`StockOverviewController.cs:49-54`). Diese Spec baut das nicht neu, sondern ändert nur das eine Attribut
(Rückfrage 2).

### Zusatzfund (nicht mitbauen)

EF-Modell `HasMaxLength(100)` für `StockMovement.ProductionOrder` (`ApplicationDbContext.cs:378`) gegen Attribut und DB 500
(`StockMovement.cs:20`, `00_FreshInstall.sql:281`). Folge für diese Spec: Die neue Migration darf **nur** die neue Spalte,
den FK und den Index enthalten; ein mitgeneriertes `AlterColumn` für `ProductionOrder` entfernen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope** (Entscheidung Gerald 1: alle Lager-Tools außer Kommissionierung):
- Neues Feld `StockMovements.ProductionOrderId` (FK auf `ProductionOrders`), gesetzt von Wareneingang, Sammel-Wareneingang,
  Warenausgang, Umbuchung und Lagerplatz ausbuchen (Wege 1–5). Der Text `ProductionOrder` bleibt (Eingabe, Altbestand, Listen,
  Sage-Entscheidung).
- Eine Auflösungsregel Text → FA (C#, ein Ort), dieselbe Regel im Altdaten-Skript.
- Buchungsmasken 1–4: Prüfkarte (Nummer, Bezeichnung, Status, Link), Warnung bei unbekannt/mehrdeutig/erledigt/storniert,
  Auswahlliste offener FAs, Pflicht je Buchungsart über neue Einstellung (Default aus).
- Anzeige mit Link: Bewegungshistorie (FA-Spalte), Bestandsübersicht (FA-Kopf bei FA-Filter).
- Bestandsübersicht: Artikel-QR-Knopf ohne FA-Übernahme (Rückfrage 2).
- Altdaten: einmalige Übernahme mit Trockenlauf und Zählung (SQL/110).

**Out-of-Scope:**
- Kommissionierung (`PickingTransferService`, Weg 7) – setzt das Feld erst im Kommissionier-Epic (siehe Abhängigkeiten).
- Sage-Payload und Sage-Enqueue-Regel; jede schreibende Sage-Funktion (spätere Stufe, Rückmelde-Epic).
- `Note` und Sage-Abgleich (Weg 8). Lagerplatz umbuchen (Weg 6, ohne FA).
- FA-Filter der Bestandsübersicht/Historie bleiben Text-Suche (`Contains`), sonst fielen Altbestand und Listen heraus.
- „Buchen für FA“, Werkbank-Filter, Auswertung Materialverbrauch je FA (Rückfrage 7).
- EF-Längendrift `ProductionOrder` (Zusatzfund).

## Fachliche Anforderungen

1. **Auflösung (eine Regel).** Eingabe getrimmt. Leer → kein FA. Enthält `;` oder `,` → „Liste“, kein Verweis. Sonst:
   (a) genau ein FA mit `SubOrderNumber` = Eingabe → dieser; (b) sonst FAs mit `OrderNumber` = Eingabe **und** `IsHauptFa`:
   genau einer → dieser, mehrere → „mehrdeutig“; (c) keiner → „unbekannt“. AKE trifft immer (a). IDEAL: Haupt-FA-Nummer
   trifft (a) bzw. (b), Sub-FA-Nummer (BelID) trifft (a).
2. **Speichern.** Jede Buchung der Wege 1–5 speichert den eingegebenen Text unverändert in `ProductionOrder` und das
   Ergebnis der Auflösung in `ProductionOrderId` (oder `NULL`). Der Server löst auf, nicht der Browser.
3. **Prüfen beim Buchen (Masken 1–4).** Sobald das FA-Feld gefüllt ist (Tippen mit Verzögerung, Verlassen, Scan, QR-Feld 3),
   zeigt eine Karte unter dem Feld: FA-Nummer (bei IDEAL-Sub-FA zusätzlich Haupt-FA), Bezeichnung 1, Artikelnummer, Status
   als **Text** („offen“, „erledigt“, „storniert“, „in Sage vermisst seit …“), Link (Anforderung 6). Unbekannt, mehrdeutig, Liste,
   erledigt, storniert, vermisst → gelbe Warnkarte mit Text; Buchen bleibt möglich, solange keine Pflicht greift.
4. **Rückmeldung nach dem Speichern.** War der FA nicht eindeutig auflösbar, erledigt oder storniert, kommt zusätzlich zur
   Erfolgsmeldung `TempData["WarningMessage"]` mit Nummer und Grund („FA 4711 ist im WMS unbekannt – Buchung ohne Verknüpfung
   gespeichert.“). Kein stilles Weglassen.
5. **Pflicht (Einstellung `FaPflichtBuchungsarten`).** Für jede dort gewählte Buchungsart gilt: ohne eindeutig aufgelösten FA
   wird nicht gebucht (`ModelState`-Fehler am FA-Feld, Maske bleibt gefüllt). Status erledigt/storniert/vermisst → nur Warnung
   (Rückfrage 3). Sammel-Wareneingang: Prüfung einmal für den Kopf-FA, vor der ersten Zeile (keine Teilbuchung). Default leer =
   keine Pflicht = Verhalten wie heute.
6. **Link.** Ziel: Master `ProduktionsauftragHierarchisch` an → FA-Struktur `/FaHierarchy?filterHauptFa={OrderNumber}`, sonst
   FA-Liste `/ProductionOrders?filterOrderNumber={OrderNumber}`. Nur für Benutzer mit FA-Leserecht (Regel des Filters
   `RequirePickingOrTrackingOrLeitstandAccess`); alle anderen sehen Nummer + Status ohne Link. Link öffnet im selben Tab.
7. **Auswahl.** Das FA-Feld bleibt ein Textfeld (Scan, Freitext) und bekommt eine Vorschlagsliste offener FAs (bestehende Suche:
   Nummer, Artikel, Kunde; Haupt-FAs; max. 20). Auswahl füllt die Nummer.
8. **Bewegungshistorie.** FA-Spalte: verknüpft → Nummer als Link + Status-Text (klein); nicht verknüpft → Text wie heute, mit
   Hinweis „nicht verknüpft“ als `title`. Filter und Spaltenfilter unverändert auf dem Text.
9. **Bestandsübersicht.** Bei gesetztem FA-Filter steht über der Tabelle ein FA-Kopf (wie Prüfkarte, Anforderung 3). Unbekannt →
   Hinweis „FA im WMS unbekannt – angezeigt werden Buchungen mit diesem Text“. Artikel-QR-Knopf übernimmt keinen FA mehr und
   leert den FA-Filter beim Artikel-Scan (Rückfrage 2); FA-Knopf unverändert.
10. **Altdaten (Entscheidung Gerald 2, Quelle laut Rückfrage 1).** Einmalig für alle Bewegungen mit `ProductionOrderId IS NULL`
    und nicht leerem Text: Regel 1 anwenden; nur bei eindeutigem Treffer `ProductionOrderId` setzen; Text und `Note` bleiben
    unverändert. Ergebnis je Klasse „übernommen / mehrdeutig / Liste / unbekannt“, vorher Trockenlauf mit Zählung. Wiederholbar
    (idempotent), z. B. nach dem Kommissionier-Epic.

## Technischer Loesungsentwurf

ponytail-Leiter: Feld und Masken existieren (Rung 2); neu sind nur FK, eine Repository-Methode, zwei API-Aktionen, ein kleines
Skript und ein Partial.

**Datenmodell (E1).** `StockMovement`: `public int? ProductionOrderId { get; set; }` und Navigation
`public ProductionOrder? LinkedProductionOrder { get; set; }` – der Name `ProductionOrder` ist durch den Text belegt und bleibt.
`ApplicationDbContext` im StockMovement-Block (`:376-424`): `HasOne(e => e.LinkedProductionOrder).WithMany().HasForeignKey(e =>
e.ProductionOrderId).OnDelete(DeleteBehavior.SetNull)`, Index `IX_StockMovements_ProductionOrderId`. Muster wie
`PartRequisition` (`:1038-1054`).

**Auflösung (E1).** `IProductionOrderRepository.FindForBookingAsync(string? text)` → `record FaAufloesung(FaAufloesungStatus Status,
ProductionOrder? Order)` mit `Status ∈ {Leer, Gefunden, Liste, Mehrdeutig, Unbekannt}`; Regel exakt wie Anforderung 1, Haupt-FA nur
über `ProductionOrder.IsHauptFa`. `AsNoTracking`. Das ist der einzige C#-Ort der Regel; SQL/110 spiegelt sie (Kommentar mit Verweis,
`ponytail:`-Hinweis „Regel doppelt C#/SQL, Altdaten einmalig“).

**Schreibwege (E1, E3).** `StockMovementsController` bekommt `IProductionOrderRepository` (Ctor, bestehende Controller-Tests
anpassen). Eine private Hilfe `ResolveFaAsync(string? text, string buchungsart)` liefert `(int? id, string? warnung, string? pflichtFehler)`;
die fünf `new StockMovement`-Stellen setzen `ProductionOrderId`. E1 setzt nur den FK; Pflicht und Warnungen kommen in E3.
`PickingTransferService` bleibt unberührt.

**API (E3)** in `StockApiController` (`[RequireStockOrPickingOrTrackingAccess]`, bestehend, deckt `stock` ab):
- `GET api/stock/fa-lookup?nr=` → `{ status, id, orderNumber, subOrderNumber, description, articleNumber, isDone, isCancelled,
  sageMissingSince, link }` (`link` null ohne FA-Leserecht).
- `GET api/stock/fa-search?q=` → Liste `{ orderNumber, text }` aus `SearchAsync(q, 20)`.

**Masken (E3).** `Inbound/Outbound/Transfer/InboundBulk.cshtml`: Textfeld bleibt (`asp-for="ProductionOrder"`), dazu
`list="faVorschlaege"` + `<datalist>` (natives Mittel, keine neue Bibliothek), Karte `<div id="faPruefung" role="status"
aria-live="polite">`. Neues `wwwroot/js/fa-feld.js` (eine Funktion `initFaFeld(inputId, kartenId)`): debounced `fa-search` füllt das
`datalist`, `fa-lookup` rendert die Karte (Text per `textContent`, kein HTML aus Daten). Reagiert auf `input`, `change` und das
Setzen durch `barcode-scanner.js` (`clearAndFillFaNumber`, `barcode-scanner.js:396-403` – dort `dispatchEvent(new Event('change'))`
ergänzen, falls nicht vorhanden; nur diese eine Stelle anfassen). Der bestehende FA-Lagerhinweis der Einbuchung
(`Inbound.cshtml:179-300`) bleibt daneben unverändert. Sammel-Wareneingang: nur Kopffeld.

**Pflicht (E3).** `AppSettingDefinitions`, Gruppe „Lager und Buchungen“: neuer `SettingType.MultiChoice`-Eintrag
`FaPflichtBuchungsarten` (Muster `BeschichtungAbholtage`, `AppSettingDefinitions.cs:63-66`), Konstante in `AppSettingKeys`. Werte
`Wareneingang`, `Sammel-Wareneingang`, `Warenausgang`, `Umbuchung`. Lesen im POST der vier Masken; Validierung über den
bestehenden `SettingValidator` beim Speichern in `/Settings` (`SettingsController.cs:101-116`). Seed nicht nötig (Katalog zeigt die
Einstellung auch ohne Zeile, `SettingsController.cs:48-57`).

**Anzeige (E4).**
- Neue Methode `ICurrentUserService.CanViewProductionOrdersAsync()` = die Oder-Regel aus `RequirePickingOrTrackingOrLeitstandAccessAttribute.cs:26-29`;
  der Filter ruft sie statt der vier Einzelaufrufe (eine Regel, kein neues Recht, keine Rollenänderung).
- Partial `Views/Shared/_FaLink.cshtml` (Muster `_VaultLink`) mit `FaLinkViewModel(text, orderNumber, subOrderNumber, isDone,
  isCancelled, sageMissingSince, linkErlaubt, hierarchisch)`; rendert Link oder Text + Status-Text. Die Seite liest Recht und
  Master einmal (Master wie `ProductionOrdersController.cs:241-243`).
- `GetMovementHistoryAsync` (`StockMovementRepository.cs:360-379`): Projektion um `ProductionOrderId`, `LinkedProductionOrder.OrderNumber/
  SubOrderNumber/IsDone/IsCancelled/SageMissingSince` erweitern (`MovementHistoryItem`, `MovementHistoryViewModel.cs:39`).
- `StockOverviewController.Index`: bei FA-Filter `FindForBookingAsync(filterProductionOrder)` → FA-Kopf im ViewModel.
  `ViewBag.QrMitFaNummer`-Zeile (`:95-97`) entfällt; `Index.cshtml:18` → `data-qr-fa-enabled="false"`, `data-fa-target` **bleibt**
  (leert den FA-Filter beim Artikel-Scan, `barcode-scanner.js:400-401`).

**frontend-design:** Views, CSS und JS ändern sich → Skill `frontend-design` explizit aufrufen. Leitplanken: Bootstrap-Karten/-Alerts
wie der vorhandene `faStorageHint`; Status nie nur über Farbe (Text ist Pflicht); Kontrast WCAG AA; Bedienbarkeit am Terminal
(Karte unter dem Feld, keine Popover).

**Rollen.** Keine neue Rolle, kein neuer Filter. Masken: `[RequireStockAccess]` bzw. `[RequireStockKeyUserAccess]` unverändert;
Historie/Bestandsübersicht `[RequireStockReadAccess]` unverändert; neue API-Aktionen unter dem Class-Filter von `StockApiController`.
`stock_read` sieht Links nur mit FA-Leserecht. `controller.md`/`RoleOverview.cshtml` unberührt (nur Hinweis „FA-Link in Lager-Tools
folgt dem FA-Leserecht“ in `controller.md` beim Brain-Update).

**Listen-View-Pattern.** Keine neue Tabelle. Bewegungshistorie behält Pagination und Server-Spaltenfilter
(`data-col-key="production-order"` filtert weiter den Text); Bestandsübersicht unverändert.

**Sage.** Payload unverändert (kein FA-Feld). `hatFaBezug` bleibt am Text (`SageBookingEnqueueingStockMovementRepository.cs:66`,
`SageBookingQueueRepository.cs:151`) – keine Verhaltensänderung durch das neue Feld. Indirekte Wirkung nur über die Pflicht:
Wer „Umbuchung“ zur Pflicht macht, gibt jeder Umbuchung einen FA; solche Umbuchungen gehen nach Spec F9 nie an Sage, auch wenn
`SageLagerbuchungUmbuchungAktiv` an ist. Warnhinweis in der Einstellung. Schreibende Sage-Aufrufe mit FA (Material-Rückmeldung)
sind eine spätere Stufe im Rückmelde-Epic, hinter eigener Einstellung mit Default aus; nichts davon hier.

## Migrations-/SQL-Auswirkungen

Reservierte Nummern laut Bündel: **109–111**. Genutzt: 109, 110. 111 bleibt frei (nur nötig, falls Rückfrage 6 eine Protokolltabelle
verlangt). Dev-Lauf prüft die Nummern gegen den Worktree-Stand (zuletzt vorhanden: `SQL/107_…`; 108 vergibt das Bündel anderweitig).

**SQL/109_AddStockMovementProductionOrderId.sql** (Model → `dotnet ef migrations add AddStockMovementProductionOrderId` → Skript):
- Batch 1: `IF COL_LENGTH('dbo.StockMovements','ProductionOrderId') IS NULL ALTER TABLE dbo.StockMovements ADD ProductionOrderId INT NULL;` `GO`
- Batch 2: `IF OBJECT_ID('dbo.FK_StockMovements_ProductionOrders_ProductionOrderId','F') IS NULL ALTER TABLE … ADD CONSTRAINT … FOREIGN KEY
  (ProductionOrderId) REFERENCES dbo.ProductionOrders(Id) ON DELETE SET NULL;` `GO` (Name = EF-Konvention, mit der Migration abgleichen)
- Batch 3: `IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_StockMovements_ProductionOrderId' AND object_id=OBJECT_ID('dbo.StockMovements'))
  CREATE NONCLUSTERED INDEX …;` `GO`
- Batch 4: `__EFMigrationsHistory`-Insert (Guard auf `MigrationId`).
- Nicht destruktiv, keine Datenänderung. Migration nur mit diesen drei Objekten (Zusatzfund `AlterColumn`).

**SQL/00_FreshInstall.sql an zwei Stellen:** Spalte + FK im `CREATE TABLE StockMovements` (`:275-298`; FK nach `ProductionOrders`
nur, wenn die Tabelle dort vorher angelegt wird – sonst FK im Index-/Constraint-Block nachziehen) bzw. Index bei `:1366-1377`;
**und** die `MigrationId` im History-Block.

**SQL/110_BackfillStockMovementProductionOrderId.sql** (Datenskript, Teil des Deploys, Default ungefährlich):
- `DECLARE @Trockenlauf BIT = 1;` Kandidaten: `ProductionOrderId IS NULL AND NULLIF(LTRIM(RTRIM(ProductionOrder)),'') IS NOT NULL`.
- Klassifizierung nach Anforderung 1 in eine `#temp` (Liste: `LIKE '%;%' OR LIKE '%,%'`; Haupt-FA: `SubOrderNumber = '' OR
  SubOrderNumber = OrderNumber`).
- Ausgabe immer: Zählung je Klasse und Bewegungsart; Detail je distinct Text mit Klasse, Anzahl, ggf. FA-Id; zusätzlich Zählung
  `Note` nach Anfang (Erwartung: nur `Sage-Korrektur:`), keine Übernahme aus `Note`.
- Nur bei `@Trockenlauf = 0`: `UPDATE … SET ProductionOrderId = …, ModifiedAt = SYSDATETIME(), ModifiedBy = N'SQL110-FA-Zuordnung',
  ModifiedByWindows = SUSER_SNAME()` nur für Klasse „übernommen“, in einer Transaktion, danach dieselbe Zählung erneut.
- Idempotent (nur `NULL`-Zeilen). Nicht InMemory-testbar → Manual-UAT auf DEV (TS-115.9/115.10).
- Dateiname mit fortlaufender Nummer, weil es zum Deploy dieser Version gehört; Alternative `SQL/Einmalig/` nur, wenn der Mensch das
  so will.

**SQL/AgentJobs:** nicht betroffen (aktiv nur `02_Import_Artikel.sql`; kein Job schreibt `StockMovements`, Grep).
**Rückbau-Runbook:** `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md` Schritt 1 um den Satz ergänzen, dass `StockMovements.ProductionOrderId`
per `ON DELETE SET NULL` geleert wird (Text bleibt).
**Service:** teilt das EF-Modell (`LagerbestandSyncService` schreibt `StockMovements`) → nach SQL/109 neu publizieren, sonst Spalten-Drift.

**Prüfabfragen (Mensch, WMS-DB je Standort, vor Freigabe – beantworten Rückfrage 1 und den Umfang der Altdaten):**

```sql
-- P1: Formen der FA-Texte
SELECT MovementType, COUNT(*) AS mitFa,
       SUM(CASE WHEN ProductionOrder LIKE '%;%' OR ProductionOrder LIKE '%,%' THEN 1 ELSE 0 END) AS liste
FROM dbo.StockMovements
WHERE NULLIF(LTRIM(RTRIM(ProductionOrder)), '') IS NOT NULL
GROUP BY MovementType;
SELECT TOP 30 ProductionOrder, COUNT(*) AS n FROM dbo.StockMovements
WHERE NULLIF(LTRIM(RTRIM(ProductionOrder)), '') IS NOT NULL GROUP BY ProductionOrder ORDER BY n DESC;

-- P2: Was steht in der Notiz?
SELECT LEFT(Note, 15) AS Anfang, COUNT(*) AS n FROM dbo.StockMovements
WHERE Note IS NOT NULL GROUP BY LEFT(Note, 15) ORDER BY n DESC;
```

**E0-Ergebnis (Dev, 2026-11-08, lesend, WMS-DEV-Datenbanken `AKE_WMS_DEV`/`IDEAL_WMS_DEV` – Produktiv-DBs nicht abgefragt):**
- P1 AKE-DEV: FA-Text bei Einbuchung 35, Ausbuchung 9, Umbuchung 43 Bewegungen; davon 22 Listen (alle Umbuchungen, z. B.
  `2618052;2618052;2602108;2602011` aus dem Kommissionierwagen); dazu Freitext-Müll (`asdf`, `Asldjflkasjdlkfj`) → Klasse „unbekannt“.
  IDEAL-DEV: 1 Bewegung mit FA (`1065141`).
- P2: `Note` nur „Sage-Korrektur:“ (AKE-DEV 19.227, IDEAL-DEV 9.094) → Annahme bestätigt, keine Übernahme aus der Notiz.
- Keine Abweichung von der Spec-Annahme → kein Blocker. Produktiv P1/P2 bleiben Deploy-Schritt für Gerald.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `FaPflichtBuchungsarten` (neu, Anzeigename „FA-Pflicht bei Buchungen“, Gruppe „Lager und Buchungen“) | Mehrfachauswahl (`MultiChoice`) | `Wareneingang`, `Sammel-Wareneingang`, `Warenausgang`, `Umbuchung` (feste Liste, Server prüft beim Speichern); leer erlaubt | leer = keine Pflicht (Leser bei fehlender Zeile: leer; kein Seed) | Für die gewählten Buchungsarten wird nur gebucht, wenn der FA eindeutig im WMS gefunden wird; sonst Fehler am FA-Feld. Erledigt/storniert = Warnung. | Wareneingang, Sammel-Wareneingang, Warenausgang, Umbuchung | beide (je Standort-DB eigener Wert) | „Mit ‚Umbuchung‘ trägt jede Umbuchung einen FA – solche Umbuchungen werden nie an Sage gemeldet, auch wenn ‚WMS-Umbuchungen an Sage‘ an ist.“ |
| `QrMitFaNummer` (bestehend, nur Text) | Bool | `true`/`false` | `false` | unverändert | „Wareneingang, Warenausgang, Umbuchung“ – Bestandsübersicht entfällt (Rückfrage 2) | beide | – |

## Audit-Feld-Auswirkungen

- Neue Bewegungen: `Created*` wie bisher (`StockMovementsController.cs:135-137` u. a.); `ProductionOrderId` wird beim Anlegen gesetzt,
  kein Update-Pfad.
- Altdaten-Skript: setzt bei jedem geänderten Satz `ModifiedAt`, `ModifiedBy = 'SQL110-FA-Zuordnung'`, `ModifiedByWindows = SUSER_SNAME()`
  (ADR 0003).
- `ON DELETE SET NULL` (nur Runbook-Rückbau) schreibt keine Audit-Felder – bewusst, Text bleibt als Spur.
- `AppSetting` erbt nicht `AuditableEntity`; Speichern über `/Settings` wie bisher.

## Akzeptanzkriterien

1. Nach SQL/109 hat `dbo.StockMovements` die Spalte `ProductionOrderId INT NULL`, den FK mit `ON DELETE SET NULL` und den Index; zweimal
   ausgeführt ohne Fehler; `dotnet ef migrations has-pending-model-changes` leer; die Migration enthält kein `AlterColumn`.
2. `FindForBookingAsync`: AKE-FA `1065141` → Gefunden; IDEAL Haupt-FA-Nummer → Gefunden (Haupt-FA); IDEAL Sub-FA-Nummer → Gefunden
   (diese Sub-FA); `"1065141; 1065142"` → Liste; zwei Haupt-FA-Zeilen gleicher `OrderNumber` ohne `SubOrderNumber`-Treffer → Mehrdeutig;
   `"9999999"` → Unbekannt; `"  1065141 "` → Gefunden; leer/null → Leer (Unit-Tests).
3. Wareneingang, Sammel-Wareneingang, Warenausgang, Umbuchung und Lagerplatz ausbuchen speichern Text unverändert und
   `ProductionOrderId` = Ergebnis der Regel (Controller-Tests je Weg).
4. Unbekannter FA ohne Pflicht: Buchung gespeichert, `ProductionOrderId` NULL, Erfolgs- **und** Warnmeldung mit FA-Nummer.
5. Erledigter oder stornierter FA: Buchung gespeichert und verknüpft, Warnmeldung nennt den Status.
6. `FaPflichtBuchungsarten = Warenausgang`: Warenausgang mit unbekanntem FA oder leerem FA wird abgewiesen (Fehler am FA-Feld, nichts
   gebucht); Wareneingang mit unbekanntem FA bucht weiter.
7. Sammel-Wareneingang mit Pflicht und unbekanntem Kopf-FA: keine einzige Zeile gebucht.
8. `/Settings` zeigt „FA-Pflicht bei Buchungen“ als Mehrfachauswahl mit vier Werten und Warnhinweis; ein manipulierter Wert
   (`Kommissionierung`) wird beim Speichern abgewiesen, nichts gespeichert.
9. Maske: FA eintippen, scannen (QR-Feld 3 bei `QrMitFaNummer`) oder aus der Vorschlagsliste wählen → Karte mit Nummer, Bezeichnung,
   Status als Text und (mit FA-Leserecht) Link; unbekannt → gelbe Karte mit Text „im WMS unbekannt“.
10. `api/stock/fa-lookup` und `fa-search` antworten für `stock`; ohne Lager-/Picking-/Tracking-Recht Zugriff verweigert; `link` ist
    null ohne FA-Leserecht.
11. Bewegungshistorie: verknüpfte Zeile zeigt FA als Link + Status-Text; nicht verknüpfte Zeile zeigt den Text wie bisher; FA-Filter
    und Spaltenfilter liefern dieselben Zeilen wie vor dem Deploy.
12. Link-Ziel: AKE (Master aus) → FA-Liste gefiltert auf die Nummer; IDEAL (Master an) → FA-Struktur gefiltert auf den Haupt-FA.
    Benutzer nur mit `stock`/`stock_read` sieht keinen Link.
13. Bestandsübersicht mit FA-Filter zeigt den FA-Kopf; unbekannter FA → Hinweistext, Tabelle wie bisher.
14. Bestandsübersicht, `QrMitFaNummer` an: Artikel-QR `102044;;1065141;09` setzt nur den Artikelfilter, FA-Filter wird geleert; FA-Knopf
    mit demselben QR setzt FA `1065141` (nur bei Antwort 2 = ja).
15. SQL/110 mit `@Trockenlauf = 1` ändert nichts und liefert die Zählung je Klasse; mit `0` setzt es nur Klasse „übernommen“, zweiter Lauf
    ändert nichts mehr; Text und `Note` aller Zeilen unverändert (Vorher/Nachher-Checksumme).
16. Sage: Payload-Tests unverändert grün; eine Umbuchung mit FA wird weiterhin nicht eingereiht, eine Ausbuchung mit verknüpftem FA
    weiterhin wie bisher.
17. `RequirePickingOrTrackingOrLeitstandAccess` verhält sich unverändert (bestehende Filter-Tests grün) und nutzt
    `CanViewProductionOrdersAsync`.
18. `dotnet build IdealAkeWms.slnx` und `dotnet test` grün; Version 1.83.0 in beiden `AppVersion.cs`; Hilfe, README, Changelog, TS-115.

## Test-Szenarien

Neues Kapitel **TS-115 „Lager-Tools: FA-Verknüpfung, Prüfung, Link“** in `docs/TESTSZENARIEN.md`, danach
`secondbrain/tests/testszenarien-index.md`. Vorbedingungen: DEV AKE und IDEAL v1.83.0, SQL/109+110 gelaufen; je Standort ein offener,
ein erledigter, ein stornierter FA; IDEAL zusätzlich eine Sub-FA-Nummer; Benutzer A (`stock`), B (`stock` + `picking`), C (`stock_read`).

- **115.1** Wareneingang, B, offener FA eintippen → Karte „offen“ + Link; buchen → Historie zeigt Link.
- **115.2** Wie 115.1 per Artikel-QR mit FA (Feld 3) und per Vorschlagsliste.
- **115.3** Unbekannter FA → gelbe Karte; buchen → Erfolg + Warnung; Historie zeigt Text ohne Link („nicht verknüpft“).
- **115.4** Erledigter / stornierter FA → Warnkarte mit Status, Buchung verknüpft.
- **115.5** Pflicht `Warenausgang`: Warenausgang ohne FA und mit unbekanntem FA → abgewiesen; mit offenem FA → gebucht. Wareneingang
  ohne FA → gebucht. Sammel-Wareneingang mit Pflicht + unbekanntem Kopf-FA → nichts gebucht.
- **115.6** Umbuchung mit FA bei `SageLagerbuchungAktiv` + `SageLagerbuchungUmbuchungAktiv` an → kein Queue-Eintrag (wie bisher);
  Warenausgang mit FA auf Sage-Platz → Queue-Eintrag wie bisher.
- **115.7** IDEAL: Haupt-FA-Nummer und Sub-FA-Nummer → beide verknüpft, Link führt auf FA-Struktur des Haupt-FA. AKE: Link auf FA-Liste.
- **115.8** Benutzer A und C: Nummer + Status, kein Link; Benutzer B: Link.
- **115.9** SQL/110 Trockenlauf auf Kopie/DEV: Zählung notieren, nichts geändert (Checksumme).
- **115.10** SQL/110 scharf: Klasse „übernommen“ verknüpft, Listen/Unbekannte nicht; zweiter Lauf 0 Änderungen; Historie zeigt Altbuchungen
  verlinkt.
- **115.11** Bestandsübersicht: FA-Filter → FA-Kopf; Artikel-QR mit FA → nur Artikelfilter (Antwort 2); FA-Knopf → FA-Filter.
- **115.12 (Negativ)** Lagerplatz ausbuchen auf Kommissionierwagen mit zwei FAs → Text „A; B“, nicht verknüpft, keine Warnung nötig;
  manipulierter `/Settings`-Wert abgewiesen; `api/stock/fa-lookup` ohne Recht → Zugriff verweigert.

Anpassen: TS-101.7 und TS-101.22 (Bestandsübersicht Artikel-QR setzt keinen FA mehr), Hinweis in TS-101.25.

## Etappen (nur bei epic: true)

**Warum Epic:** vier Schichten (Schema + Altdaten, Schreibwege, Masken-UI, Anzeige) mit einer Migration und einem Datenskript; als ein
Lauf zu groß für ein Turn-Limit, als Split nicht sinnvoll, weil erst alle Teile zusammen den Nutzen bringen (das Feld allein ist
unsichtbar). Ein Worktree, ein Merge, **eine Version 1.83.0** (Bump in E4). Branch während der Arbeit mit
`scripts/sync-worktree.ps1 -Slug <slug>` auf `main`-Stand halten. Jede Etappe baut und testet grün für sich.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | Datenmodell + Migration + SQL/109 + FreshInstall (2 Stellen); `FindForBookingAsync` mit Unit-Tests; FK in Wegen 1–5 setzen (ohne UI); Controller-Tests | erledigt | `f38e82ac` |
| 2 | SQL/110 Altdaten-Skript (Trockenlauf-Default, Zählung, idempotent); DEV-Trockenlauf je Standort. **Abhängig von Antwort 1** (Quelle Textfeld vs. Notiz) und Antwort 6 (Protokoll); bei offener Antwort: Skript nach Empfehlung bauen und als Blocker vermerken | erledigt | `9a435e81` |
| 3 | API `fa-lookup`/`fa-search`; `fa-feld.js`; Prüfkarte + Vorschlagsliste in Masken 1–4; Warnungen; Einstellung `FaPflichtBuchungsarten` + Pflichtprüfung; Tests | erledigt | `96f4dd9b` |
| 4 | `CanViewProductionOrdersAsync` + Filter-Umbau; `_FaLink`; Historie + Bestandsübersicht-Kopf; Bestandsübersicht Artikel-Scan ohne FA (**nur bei Antwort 2 = ja**, sonst Zeile überspringen); Hilfe, README, Runbook, TS-115, Changelog, Version 1.83.0 | erledigt | `7ab2fcb5` |

## Deploy

**Final (QA 2026-11-07 gegen den Diff `405e9af5..7ab2fcb5`):** `deploy.web` = true (`IdealAkeWms/` Controller, Views, JS, Einstellung,
Migration), `deploy.service` = true (`IDEALAKEWMSService/AppVersion.cs` 1.87.0; gemeinsames EF-Modell `StockMovement`, keine Service-Logik),
`deploy.migration` = true (`Migrations/20261107190419_AddStockMovementProductionOrderId.cs`, `SQL/109`, Datenskript `SQL/110`).
Ablauf des Menschen: Publish aus dem Worktree → Testsystem → Test → Merge. Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge
getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.

- **Web-App:** ja (Controller, Views, JS, Einstellungen).
- **Service:** ja – gemeinsames EF-Modell (`StockMovement`), Versionsstand 1.87.0; keine Service-Logik geändert.
- **Migration:** ja – SQL/109 (Schema, nicht destruktiv), SQL/110 (Daten, Default Trockenlauf).
- **Reihenfolge:** DB-Backup (wegen SQL/110 scharf) → SQL/109 → Web und Dienst tauschen → SQL/110 Trockenlauf, Ergebnis in der
  Deploy-Aufgabe ablegen → nach Sichtung SQL/110 mit `@Trockenlauf = 0` → Ergebnis ablegen. Kein Dienst-Stopp nötig außer für den Tausch.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- Nach dem Deploy keine Einstellung nötig (Pflicht Default aus). Anwender-Info: Bestandsübersicht-Artikel-Scan setzt keinen FA mehr
  (falls Antwort 2 = ja).

## Abhängigkeiten

- **Baut auf:** v1.76.0 [[2026-11-07-bestandsuebersicht-qr-fa-uebernahme-fest-spec]] (ändert nur deren Attribut, kein Doppelbau),
  v1.77.0 Einstellungs-Katalog (`AppSettingDefinitions`, `MultiChoice`, `SettingValidator`).
- **Wird genutzt von:** [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]] (Teil C). Das Feld ist dafür so geschnitten:
  `ProductionOrderId` sitzt an der einzelnen Bewegung (eine Artikelzeile → ein FA), der Text bleibt für Listen. Kommissionier-
  Entnahmen setzen ihn direkt aus dem Auftrag (`PickingTransferService.DoTransferAsync`, `:151-307`: `ProductionOrderId = productionOrderId`
  der Herkunftszeile; bei Sammelkommissionierung je Zeile der Herkunfts-FA, nicht die zusammengeführte Liste von `:78`). Danach
  SQL/110 erneut laufen lassen, um Kommissionier-Altbuchungen nachzuziehen.
- **Wird genutzt von:** Rückmelde-Epic [[2026-11-02-sage-pps-fa-bde-abgleich-spec]] (K6, Material-RM je BelID = `SubOrderNumber` des
  verknüpften FA).
- **Bündel:** [[2026-11-08-buendel-uebersicht]] Teil A, letzte Spec des gemeinsamen Dev-Laufs; SQL 109–111.

## Offene Rueckfragen

1. **Konflikt Altdaten-Quelle.** Entscheidung 2 sagt „FA-Nummern per Muster aus Bemerkungen ziehen“. Am Code steht der FA bei allen
   Buchungsmasken seit jeher im eigenen Textfeld `StockMovements.ProductionOrder` (Wege 1–5, 7); die Notiz `Note` schreibt nur der
   Sage-Bestandsabgleich (`LagerbestandSyncService.cs:185`, `:253`). Altbestand aus dem Textfeld exakt auflösen (Regel 1) und die Notiz
   nur im Trockenlauf zählen (P2)? **Empfehlung: ja**; Muster-Parsing der Notiz nur, falls P2 Notizen ohne „Sage-Korrektur:“ zeigt.
2. **Bestandsübersicht Artikel-Scan ohne FA.** Artikel-QR-Knopf übernimmt nie mehr den FA, unabhängig von `QrMitFaNummer` (nimmt die
   Bestandsübersicht-Hälfte von v1.76.0 zurück; an beiden Standorten sichtbar, weil `QrMitFaNummer` an ist)? **Empfehlung: ja** – der FA
   übersteuert dort den Artikelfilter, für den FA gibt es den eigenen Knopf.
3. **Pflicht-Semantik.** Pflicht = FA eindeutig im WMS gefunden; erledigt/storniert/in Sage vermisst nur Warnung, keine Sperre?
   **Empfehlung: ja** (Nachbuchungen auf erledigte FAs kommen vor).
4. **IDEAL Sub-FA.** Vorschlagsliste nur Haupt-FAs (bestehende Suche), eine exakt eingegebene/gescannte Sub-FA-Nummer wird verknüpft?
   **Empfehlung: ja**; ob Sub-FAs auswählbar sein müssen, entscheidet das Rückmelde-Epic.
5. **Lagerplatz ausbuchen** (Kommissionierwagen): nur verknüpfen, wenn der Text genau einen FA ergibt; keine Prüfkarte, keine Pflicht?
   **Empfehlung: ja**.
6. **Protokoll Altdaten.** Ergebnismengen von SQL/110 (Trockenlauf und scharf) als Datei in der Deploy-Aufgabe ablegen statt neuer
   Protokolltabelle? **Empfehlung: ja** (SQL/111 bleibt frei).
7. **Folgepunkte.** „Buchen für FA“ (FA zuerst aus gefilterter Liste), Werkbank-/Status-Filter in der Auswahl und Auswertung
   Materialverbrauch je FA als eigener Backlog-Punkt? **Empfehlung: ja**, nicht in dieser Spec.

## Freigabe-Nachtrag 2026-11-07 19:17 (Gerald, geht dem Rumpf vor)
- Alle Rückfragen **wie empfohlen** (Antworten unten), Abweichungen nur wo dort vermerkt.
- **Etappe E0 vor E1:** Alle Prüfabfragen dieser Spec, die lesend am AKE-Test möglich sind (SData nur GET mit `orderBy`; Sage-SQL bzw.
  WMS-DB nur SELECT), führt der Dev selbst aus und trägt die Ergebnisse ein. **Weicht ein Ergebnis von der Spec-Annahme ab: Blocker melden,
  nicht raten** (Muster B1 vom 07.11.). Prüfabfragen, die IDEAL-Systeme oder Menschen brauchen, bleiben offen; betroffene Teile hinter
  Schalter/Standort aus.
- **Etappen, die an Sage schreiben, bleiben blockiert und aus** (laut Bündel-Übersicht). Keine Schreibtests an Sage in diesem Lauf.
- Worktree laut Lauf-Auftrag (Empfehlung Ü1: nach Merge des Bündels frisch von `main`).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Altdaten aus dem Textfeld `ProductionOrder` statt aus der Notiz (P2 nur zählen)? (Empfehlung ja) → **wie empfohlen (Gerald 2026-11-07 19:17)**
2. Bestandsübersicht: Artikel-QR-Knopf nie mehr FA, unabhängig von `QrMitFaNummer`? (Empfehlung ja) → **wie empfohlen (Gerald 2026-11-07 19:17)**
3. Pflicht = eindeutig gefunden; erledigt/storniert/vermisst nur Warnung? (Empfehlung ja) → **wie empfohlen (Gerald 2026-11-07 19:17)**
4. IDEAL: Vorschläge nur Haupt-FAs, exakte Sub-FA-Nummer wird verknüpft? (Empfehlung ja) → **wie empfohlen (Gerald 2026-11-07 19:17)**
5. Lagerplatz ausbuchen: nur eindeutiger Text verknüpft, keine Prüfkarte/Pflicht? (Empfehlung ja) → **wie empfohlen (Gerald 2026-11-07 19:17)**
6. Protokoll SQL/110 als Datei in der Deploy-Aufgabe, keine neue Tabelle? (Empfehlung ja) → **wie empfohlen (Gerald 2026-11-07 19:17)**
7. „Buchen für FA“, Werkbank-Filter, Auswertung je FA als eigener Backlog-Punkt? (Empfehlung ja) → **wie empfohlen (Gerald 2026-11-07 19:17)**

## Umsetzung (Dev, 2026-11-08, Bündel 2026-11-08 Lauf 2) – v1.87.0

Lauf [[2026-11-08-buendel-2026-11-08-goal-lauf-2]]. **Version 1.87.0** (laut Bündel-Übersicht/Lauf-Notiz, nicht 1.83.0 wie im Rumpf).
SQL/109 (Schema) + SQL/110 (Daten); 111 nicht genutzt (Antwort 6).

- **E1 `f38e82ac`:** `StockMovement.ProductionOrderId` + Navigation `LinkedProductionOrder` (FK SetNull, Index); Migration
  `20261107190419_AddStockMovementProductionOrderId` nur mit Spalte/FK/Index (kein `AlterColumn`, `has-pending-model-changes` leer);
  `SQL/109` auf AKE-/IDEAL-DEV zweimal fehlerfrei; FreshInstall an zwei Stellen (Spalte im `CREATE TABLE`, FK + Index im Index-Block, weil
  `ProductionOrders` später angelegt wird; `MigrationId`). `IProductionOrderRepository.FindForBookingAsync` → `FaAufloesung`; Wege 1–5 setzen den FK.
- **E2 `9a435e81`:** `SQL/110` (Trockenlauf-Default). DEV AKE Trockenlauf: übernommen 38 (10 Einb./7 Ausb./21 Umb.), Liste 22, unbekannt 27,
  Notiz nur „Sage-Korrektur:“ (19.227); scharf 38 verknüpft, zweiter Lauf 0, Checksumme Text/Notiz gleich. IDEAL-DEV Trockenlauf: 1 übernommen (nicht scharf).
- **E3 `96f4dd9b`:** `api/stock/fa-lookup`/`fa-search` (Class-Filter `RequireStockOrPickingOrTrackingAccess`), `wwwroot/js/fa-feld.js` (Karte per
  `textContent`, `<datalist>`; `barcode-scanner.js` feuert `change` bereits – nicht angefasst), Karte in Inbound/InboundBulk/Outbound/Transfer;
  `Services/FaBuchungsText.cs` (Status-Text, Warnungen, Pflicht-Fehler); Einstellung `FaPflichtBuchungsarten` (MultiChoice, Gruppe „Lager und Buchungen“, kein Seed).
- **E4 `7ab2fcb5`:** `_FaLink` + `FaLinkViewModel`; Historie (Projektion `LinkedFa`), Bestandsübersicht FA-Kopf; Artikel-QR der Bestandsübersicht
  `data-qr-fa-enabled="false"` (Antwort 2), `ViewBag.QrMitFaNummer` dort entfernt; Hilfe, README, Runbook, Changelog, TS-115, TS-101.7/22/25 angepasst.
- **Abweichung (bewusst):** `CanViewProductionOrdersAsync` ist eine **Erweiterungsmethode** auf `ICurrentUserService` (`FaBuchungsText`), nicht
  Interface-Member – eine Regel, der Filter ruft sie, Mocks/Fakes bleiben unverändert (bestehende Filter-Tests grün ohne Änderung).
- **Geänderte bestehende Tests (gewollt, Antwort 2):** `StockOverviewControllerTests` (QrMitFaNummer-ViewBag → FA-Kopf/kein FA),
  `HardwareScannerTests.Bestandsuebersicht_QrFaUebernahme_*` (→ `_Nie_v187`); `StockMovementsControllerBulkInboundTests` (Ctor-Parameter);
  `AppSettingDefinitionsTests` 54 → 55.
- Build grün, Tests 506 Service / 2.499 Web (1 übersprungen) grün.

## QA v1.87.0 (qa-agent, 2026-11-07, Bündel 2026-11-08 Lauf 2)

Geprüft: Commit-Bereich `405e9af5..7ab2fcb5` (E1 `f38e82ac`, E2 `9a435e81`, E3 `96f4dd9b`, E4 `7ab2fcb5`), Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5` HEAD `7ab2fcb5`, sauber. Maßgeblich: Freigabe-Nachtrag + Antworten (alle wie empfohlen), Version 1.87.0
(AK 18 nennt im Rumpf 1.83.0, laut [[2026-11-08-buendel-2026-11-08-goal-lauf-2]] gilt 1.87.0; Frontmatter `version` auf v1.87.0 gesetzt).

### 1. Build und Tests (selbst ausgeführt)

- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (vorbestehend).
- `dotnet test`: **IDEALAKEWMSService.Tests 506 bestanden / 0 übersprungen / 0 Fehler**; **IdealAkeWms.Tests 2.499 bestanden / 1 übersprungen / 0 Fehler** (2.500).
- `dotnet ef migrations has-pending-model-changes`: „No changes have been made to the model since the last migration.“ Migration ohne `AlterColumn`.

### 2. DEV-Deploy (automatisch nach grünem Build)

Standort laut Frontmatter `beide`, elevated Shell, `dev-deploy.ps1 -Quelle <Worktree>`:
- AKE: Exit 0, „HTTP 200, Fusszeile v1.87.0“, DEV-Absicherung angewandt, Dienst gestartet → http://localhost:8081
- IDEAL: Exit 0, „HTTP 200, Fusszeile v1.87.0“, DEV-Absicherung angewandt, Dienst gestartet → http://localhost:8082

DB-Stand danach (beide): FK `FK_StockMovements_ProductionOrders_ProductionOrderId` `SET_NULL`, Index `IX_StockMovements_ProductionOrderId`,
History `20261107190419_AddStockMovementProductionOrderId`. Master `ProduktionsauftragHierarchisch`: AKE false, IDEAL true. `QrMitFaNummer` beide true.
`FaPflichtBuchungsarten`: keine Zeile (Default).

### 3. Code-Review (selbst, in diesem Lauf)

Gelesen: gesamter Diff ohne Designer-Datei, dazu `StockMovementsController` (Inbound/InboundBulk/Outbound/Transfer/OutboundAllConfirm im Zusammenhang),
`ProductionOrder.IsHauptFa`, `ProductionOrderRepository.SearchAsync`, `RequireStockOrPickingOrTrackingAccess`, `CurrentUserService.CanAccessStock*`,
die vier Masken, `_FaLink`, `fa-feld.js`, SQL/109, SQL/110, FreshInstall-Stellen, geänderte Tests.

Gezielt geprüfte Fehlerklassen: Werte in `<script>` – keine (fa-feld.js baut DOM per `textContent`, keine Razor-Werte im Skript); `hidden` + `d-*` – nicht
vorhanden; stille Rückfälle – `fa.Order?.Id` = null heißt fachlich „nicht verknüpft“ und wird immer gemeldet (Warnung bzw. Pflicht-Fehler), kein `?? 0`;
parallele Arrays – nicht betroffen; Server-Durchsetzung – Pflicht prüft der Server (`FaPruefenAsync` vor `ModelState.IsValid` in allen vier POSTs,
Sammel vor der ersten Zeile), die Karte ist nur Vorschau; eine Regel an mehreren Orten – Auflösung C# (`FindForBookingAsync`) + SQL/110 bewusst doppelt
(Spec, `ponytail:`-Kommentar); Status-Text, Warnstatus und FA-Leserecht je an einem Ort (`FaBuchungsText`), der Filter nutzt `CanViewProductionOrdersAsync`.

| ID | Schwere | Befund | Fundstelle | Behandlung |
|---|---|---|---|---|
| Q-1 | niedrig | Prüfkarte sagt bei unbekannt/mehrdeutig/Liste immer „Buchen ist möglich …“, auch wenn für die Buchungsart die Pflicht greift. DEV: Warenausgang mit Pflicht + `9999999` → Server-Fehler am Feld korrekt, darunter weiter „Buchen ist möglich“. Widerspricht Anf. 3 („solange keine Pflicht greift“) im Text, nicht in der Wirkung. | `IdealAkeWms/wwwroot/js/fa-feld.js` Funktion `zeigen`, Zweig `else` | offen, nicht blockierend (Server sperrt). Vorschlag: Maske gibt die Pflicht als `data-`Attribut mit, Satz dann „FA-Pflicht: ohne gefundenen FA wird nicht gebucht.“ |
| Q-2 | mittel | Kontrast Link in gelber Karte: Bootstrap-Linkfarbe `#0d6efd` auf `alert-warning` `#fff3cd` ≈ 4,1:1 < 4,5:1 (kleiner Text „FA anzeigen“, Klasse `small`; ebenso FA-Nummer-Link im FA-Kopf der Bestandsübersicht bei Warnstatus). Unterstrichen und mit Text, also nicht nur Farbe; Wert knapp unter AA. Auf weißer Karte (`alert-light`) ≈ 4,5:1. | `fa-feld.js` (`el('a', 'small', 'FA anzeigen')`), `Views/Shared/_FaLink.cshtml` (Link) im FA-Kopf `Views/StockOverview/Index.cshtml` | offen, nicht blockierend; Empfehlung vor dem Merge: Klasse `alert-link` an Links innerhalb `.alert` (Alert-Textfarbe, ≥ 7:1). |
| Q-3 | Hinweis | `letzte` wird vor dem Fetch gesetzt; bei HTTP-Fehler (nicht `ok`) bleibt die Karte unverändert und derselbe Wert wird erst nach einer Änderung erneut geprüft (bei Netzfehler kommt der Hinweistext). | `fa-feld.js` Funktion `pruefen` | offen, kosmetisch (Server prüft beim Speichern). |
| Q-4 | Hinweis | C# `Trim()` entfernt auch Tab/Zeilenumbruch, SQL/110 `LTRIM/RTRIM` nur Leerzeichen – theoretische Drift der doppelten Regel. | `ProductionOrderRepository.FindForBookingAsync`, `SQL/110` Kandidaten-SELECT | akzeptiert (Altdaten; DEV-Detail zeigt keine solchen Texte). |
| Q-5 | Hinweis (Brain) | `secondbrain/tests/testszenarien-index.md` hatte keinen TS-115-Eintrag. | Hauptcheckout | **von QA nachgezogen** (Zeile 115). |
| Z-1 | Zusatzfund, nicht diese Spec | Waagerechte Seiten-Scrollleiste auf allen Seiten bei 1520 px Fensterbreite (Navigationsleiste breiter als Fenster), auch FA-Struktur/Bewegungshistorie. | Layout/Navbar | nicht mitgebaut; zur Kenntnis. |

Keine blockierenden Befunde.

### 4. Beweisart je Akzeptanzkriterium

| AK | Beweis | Ergebnis |
|---|---|---|
| 1 | DEV (SQL Server): SQL/109 erneut auf AKE-DEV Exit 0; FK `SET_NULL` + Index + History auf beiden DEV-DBs; `has-pending-model-changes` leer; kein `AlterColumn` | OK |
| 2 | automatisiert `FindForBooking_Regel_AK2` (9 Fälle) + `FindForBooking_ZweiHauptFaZeilenOhneSubTreffer_Mehrdeutig_AK2`; DEV-API: AKE `2638001` gefunden, IDEAL Haupt `1060414` → Id 284, Sub `1067220` → Id 309, Liste → `liste` | OK (InMemory + DEV) |
| 3 | automatisiert `Schreibwege1bis5_SetzenFkUndBehaltenText_AK3`, `LagerplatzAusbuchen_WagenListe_NichtVerknuepft_Antwort5`; DEV: Ids 19390–19392 Text unverändert, FK NULL/15/3046 | OK |
| 4 | automatisiert `UnbekannterFa_OhnePflicht_GebuchtMitWarnung_AK4`; DEV Wareneingang `9999999`: Erfolg + Warnung, FK NULL | OK |
| 5 | automatisiert `ErledigterFa_GebuchtVerknuepftWarnungNenntStatus_AK5`; DEV Wareneingang `2602098` (erledigt): Warnung „ist erledigt – … verknüpft“, FK 15 | OK |
| 6 | automatisiert `Pflicht_Warenausgang_UnbekanntUndLeerAbgewiesen_WareneingangBuchtWeiter_AK6`; DEV (Pflicht per SQL): Warenausgang `9999999` abgewiesen mit Feldfehler, Maske gefüllt; `2638001` gebucht (FK 3046) | OK; „leerer FA“ nur automatisiert |
| 7 | automatisiert `Pflicht_SammelWareneingang_UnbekannterKopfFa_NichtsGebucht_AK7`; DEV Sammel mit Kopf-FA `8888888`: Feldfehler, 0 Zeilen | OK |
| 8 | automatisiert `Einstellung_FaPflicht_MultiChoice_ManipulierterWertAbgewiesen_AK8` (Validator); DEV `/Settings`: Mehrfachauswahl 4 Werte, Beschreibung, Verwendung, Standard, Standort, Warnhinweis sichtbar | Darstellung OK; manipulierter POST über echte Modellbindung = **Manual-UAT** |
| 9 | Browser-JS. DEV: Tippen → Karte offen/erledigt/unbekannt (Wareneingang, Ausbuchung, Sammel; Umbuchung IDEAL Sub-FA „1067220 (Haupt-FA 1060414)“); `fa-search` liefert Treffer | Tippen OK; QR-Scan Feld 3 und Auswahl aus `<datalist>` = **Manual-UAT** |
| 10 | automatisiert `Api_FaLookup_LinkNurMitLeserecht_ZielJeMaster_AK10_AK12`, `Api_KlasseBleibtHinterLagerPickingTrackingFilter_AK10`; DEV: ohne Anmeldung → Umleitung Login/AccessDenied | Rollen A/C real = **Manual-UAT** |
| 11 | automatisiert `Historie_…_AK11`; DEV-Historie: `2638001` Link + „offen“, `2602098` Link + „Achtung: erledigt“, `9999999` Text | OK |
| 12 | DEV: AKE-Link `/ProductionOrders?filterOrderNumber=…`; IDEAL-Link `/FaHierarchy?filterHauptFa=1060414` angeklickt, Struktur gefiltert | OK; „nur stock/stock_read ohne Link“ = **Manual-UAT** (115.8) |
| 13 | automatisiert `Index_FaFilter_FaKopf_AK13`; DEV: FA-Kopf `2602098` (Link, „Achtung: erledigt“, Bezeichnung, Artikel), `9999999` Hinweistext, Tabelle unverändert | OK |
| 14 | Quelltext-Wächter `Index_ArtikelQr_UebernimmtNieFa_AuchMitQrMitFaNummer_AK14`, `HardwareScannerTests.Bestandsuebersicht_QrFaUebernahme_Nie_v187` | Scan im Browser = **Manual-UAT** (115.11) |
| 15 | DEV AKE: SQL/110 Trockenlauf Exit 0, „TROCKENLAUF: nichts geaendert“, Checksumme `148833457` und verknüpft 40 vorher = nachher; scharfer Lauf + zweiter Lauf 0 laut E2 | Trockenlauf OK; scharf IDEAL/Testsystem/Produktiv = Deploy-Schritt Mensch |
| 16 | Sage-Payload/Enqueue-Dateien nicht im Diff; Sage-Tests grün; DEV: 0 neue `SageBookingQueueItems` (Schalter aus) | Wirkung mit Schaltern an = **Manual-UAT Testsystem** (115.6) |
| 17 | bestehende `RequirePickingOrTrackingOrLeitstandAccessFilterTests` unverändert grün; Filter ruft `CanViewProductionOrdersAsync` | OK |
| 18 | Build/Tests grün; `AppVersion` Web + Dienst 1.87.0; Hilfe, README, Changelog, TS-115 im Diff | OK (1.87.0 statt 1.83.0) |

### 5. Geänderte bestehende Tests

| Test | Einordnung |
|---|---|
| `StockMovementsControllerBulkInboundTests` (Mock + Ctor-Parameter) | nur Typ/Ctor (Compile-Fix); Mock liefert „Unbekannt“ = Verhalten ohne Pflicht wie bisher |
| `AppSettingDefinitionsTests` 54 → 55 | gewollt (neue Einstellung, AK 8) |
| `StockOverviewControllerTests` (QrMitFaNummer-Theory ersetzt durch AK13/AK14) | Verhalten gewollt geändert (Antwort 2 / AK 14) |
| `HardwareScannerTests.Bestandsuebersicht_QrFaUebernahme_FolgtQrMitFaNummer_NichtFest` → `…_Nie_v187` | Verhalten gewollt geändert (Antwort 2 / AK 14) |

Keine Einordnung „sonstige“.

### 6. CLAUDE.md-Checkliste / ADR 0020

- Migration + `SQL/109` (Guards je Batch, History separat) ✓; FreshInstall an beiden Stellen (Spalte im `CREATE TABLE`, FK+Index im Index-Block, `MigrationId` mit Spalten-Guard) ✓; `SQL/AgentJobs` nicht betroffen ✓.
- Audit: neue Bewegungen wie bisher; SQL/110 setzt `ModifiedAt/By/ByWindows` (DEV: `ModifiedBy = SQL110-FA-Zuordnung`) ✓.
- Version beide `AppVersion.cs` 1.87.0, Anwender-Changelog, Hilfe, README, Runbook ✓; `docs/TESTSZENARIEN.md` TS-115 + 101.7/101.22 ✓; Index von QA nachgezogen (Q-5).
- Brain-Changelog/feature-map: Bündel-Abschluss, nicht Teil dieses QA-Laufs.
- ADR 0020 `FaPflichtBuchungsarten`: feste Auswahl (MultiChoice), Server-Validierung (`SettingValidator`, Test AK8), Anzeigename/Wirkung/Verwendung/Default/Standort/Warnhinweis vorhanden und auf DEV sichtbar ✓. `QrMitFaNummer`: nur Verwendungstext ✓.

### 7. DEV-Testlauf TS-115 (AKE-DEV 8081, IDEAL-DEV 8082, Windows-Anmeldung Admin)

| Schritt | Ausgeführt | Ergebnis | Status |
|---|---|---|---|
| 115.1 | AKE Wareneingang `2638001` getippt | Karte „FA 2638001 / UKW 1-1-E R290 / Artikel EU253691 / Status: offen / FA anzeigen“; Buchung mit diesem FA als Warenausgang (115.5), Historie Link + „offen“ | OK |
| 115.2 | `api/stock/fa-search?q=26380` | Treffer „2638001 – EU253691 – UKW 1-1-E R290 – Kunde“ | OK (API); QR-Scan und Auswahl im `<datalist>` NICHT MÖGLICH (Kamera/natives Dropdown) |
| 115.3 | AKE Wareneingang `9999999` gebucht (Id 19390) | gelbe Karte „Achtung: … unbekannt“; Erfolg + Warnung; FK NULL; Historie Text ohne Link | OK |
| 115.4 | `2602098` (erledigt) gebucht (Id 19391); `2630279` (storniert) per API | Karte „Achtung: Status erledigt“, Warnung nach Buchung, FK 15; storniert: `statusText` „storniert“, `warnStatus` true | OK |
| 115.5 | Pflicht `Warenausgang,Sammel-Wareneingang` per SQL; Warenausgang `9999999`, dann `2638001` (Id 19392); Sammel mit Kopf `8888888`; Pflicht danach gelöscht | abgewiesen mit Feldfehler; offen gebucht (FK 3046); Sammel: Feldfehler, 0 Zeilen; Einstellungszeile wieder entfernt (Ausgangszustand) | OK (Befund Q-1 Kartentext) |
| 115.6 | – | – | NICHT MÖGLICH (Sage-Schalter auf DEV verboten; nur Testsystem) |
| 115.7 | IDEAL `fa-lookup` Haupt `1060414` und Sub `1067220`; Umbuchungsmaske Sub-FA; Link angeklickt | beide gefunden; Karte „FA 1067220 (Haupt-FA 1060414)“; Link → FA-Struktur `filterHauptFa=1060414` | OK (Karte/Link; IDEAL-Buchung selbst nicht ausgeführt) |
| 115.8 | – | nur Admin-Benutzer verfügbar | NICHT MÖGLICH (Benutzer A/C) |
| 115.9 | SQL/110 Trockenlauf AKE-DEV | Liste 22, unbekannt 28 (inkl. QA-Buchung `9999999`), übernommen 0 (bereits verknüpft), Notiz nur „Sage-Korrektur:“ 19.227; Checksumme/Anzahl unverändert | OK |
| 115.10 | Nachweis E2 (AKE-DEV 38 verknüpft, zweiter Lauf 0) | DEV jetzt 40 verknüpft = 38 + 2 QA-Buchungen | OK laut E2; IDEAL-DEV bewusst nur Trockenlauf |
| 115.11 | FA-Filter `2602098` und `9999999` | FA-Kopf bzw. Hinweistext, Tabelle unverändert | OK; Artikel-QR/FA-Knopf per Kamera NICHT MÖGLICH |
| 115.12 | API ohne Anmeldung | Umleitung auf Login (AccessDenied) | teilweise; Wagen „A; B“ nur Unit-Test (Antwort 5), manipulierter `/Settings`-POST und Rolle ohne Recht NICHT MÖGLICH |

Sichtprüfung Kontrast: Karten-Text `alert-warning` (#664d03 auf #fff3cd) und „Achtung:“-Präfix ≥ 7:1, Status nie nur Farbe (Text „offen/erledigt/storniert“
+ „Achtung:“); Historie „offen“ `text-body-secondary` auf Weiß ausreichend; einzige Abweichung Q-2.

**DEV-Testbuchungen (AKE_WMS_DEV, bleiben als Testdaten stehen):** `StockMovements` Id 19390 (Einbuchung 1 × 00000905 auf „Test“, FA-Text `9999999`, FK NULL),
19391 (Einbuchung, `2602098`, FK 15), 19392 (Ausbuchung 1 × 00000905 von „Test“, `2638001`, FK 3046) – Bestand 00000905 auf „Test“ netto +1.
Keine Sage-Queue-Einträge. `FaPflichtBuchungsarten` wieder ohne Zeile. Auf IDEAL_WMS_DEV keine Buchungen.

### 8. Manuelle Checkliste für den Menschen (offen)

1. Q-2 entscheiden (Link-Kontrast in gelber Karte, `alert-link`) – Empfehlung: vor dem Merge beheben.
2. Q-1 entscheiden (Kartentext bei aktiver Pflicht).
3. TS-115.2: FA per Artikel-QR (Feld 3) an Kamera/Netum und per Vorschlagsliste wählen.
4. TS-115.8: Benutzer A (`stock`) und C (`stock_read`) – Nummer + Status ohne Link; B mit Link.
5. TS-115.11: Bestandsübersicht Artikel-QR `…;;<FA>;…` über den Artikel-Knopf (nur Artikelfilter, FA leer) und über den FA-Knopf.
6. TS-115.12: `/Settings` mit manipuliertem Wert `Kommissionierung` (DevTools) → abgewiesen; `api/stock/fa-lookup` als Benutzer ohne Lager-/Picking-/Tracking-Recht; Kommissionierwagen mit zwei FAs ausbuchen.
7. TS-115.6 **nur Testsystem**: Umbuchung mit FA bei `SageLagerbuchungAktiv` + `SageLagerbuchungUmbuchungAktiv` → kein Queue-Eintrag; Warenausgang mit FA auf Sage-Platz → Queue-Eintrag.
8. Testsystem-Lauf je Standort: Backup → SQL/109 → Web + Dienst publizieren → SQL/110 Trockenlauf (Ergebnis in die Deploy-Aufgabe, Antwort 6) → nach Sichtung `@Trockenlauf = 0` → zweiter Lauf 0.
9. IDEAL: eine echte Buchung mit Haupt- und Sub-FA (TS-115.7) inkl. Historie-Link auf die FA-Struktur.
10. Produktiv-Prüfabfragen P1/P2 je Standort vor dem scharfen SQL/110.

