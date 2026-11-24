---
type: spec
title: "Sage-Übergabe: Referenz mit Kennung, WMS-Benutzer und Abteilung (Ausbuchung)"
slug: 2026-11-24-sage-buchung-referenz-benutzer-abteilung-spec
status: Entwurf
created: 2026-11-24
updated: 2026-11-24
source_backlog: "[[2026-11-24-sage-buchung-abteilung-und-benutzer]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
depends_on: "[[2026-11-23-sage-buchungen-gesamtsicht-spec]]"
version_vorschlag: "nächste freie Bündel-Version; Stand 2026-11-24 Worktree IdealAkeWms/AppVersion.cs:5 = 1.99.63, 1.99.64 nennen schon Gesamtsicht-, Bedarfsmeldungs- und BF-Auftrag-Spec – der Dev-Lauf vergibt in Baureihenfolge"
sql_vorschlag: "SQL/167 vorläufig (höchste vorhandene Datei SQL/166_AddStockMovementWarehouseRequisitionItem.sql; 164/165 fehlen im Ordner; SQL/167 nennt auch [[2026-11-24-bf-auftrag-baugruppen-aufloesen-und-als-fa-zeigen-spec]]) – Dev-Lauf prüft und nimmt die nächste freie"
testszenario_vorschlag: "nächste freie TS-Nummer; Stand 2026-11-24 testszenarien-index.md:162 = TS-188, TS-189 nennen schon drei andere Specs – Dev-Lauf vergibt"
affected_code:
  - IDEALAKEWMSService/Services/SageLagerbuchungPayload.cs:17-29 (SageLagerbuchungZeile bekommt Referenz)
  - IDEALAKEWMSService/Services/SageLagerbuchungPayloadBuilder.cs:54-74 (Referenz je Zeile; Kopf-Memo unverändert)
  - IDEALAKEWMSService/Services/SageWareneingangPayload.cs:25,100 (Kopf-Memo mit Präfix und Benutzer; Artikelmemo unverändert)
  - IDEALAKEWMSService/Services/SageBookingCorrelation.cs (neue reine Funktion Referenz(...) mit Kürzungsregel, oder eigene kleine statische Klasse)
  - IDEALAKEWMSService/Services/WareneingangCsvService.cs:164-172 (nur Vergleich – Erfasser-Regel NICHT übernommen, siehe F2)
  - IdealAkeWms/Models/StockMovement.cs (neue Spalte Abteilung)
  - IdealAkeWms/Controllers/StockMovementsController.cs:302-377 (Outbound GET/POST), :472-504 (OutboundAll GET), :522-625 (OutboundAllConfirm)
  - IdealAkeWms/Models/ViewModels/StockMovementCreateViewModel (Abteilung + Werkbank-Liste), OutboundAllViewModel (dto.)
  - IdealAkeWms/Views/StockMovements/Outbound.cshtml, OutboundAll.cshtml (Auswahlfeld Abteilung)
  - IdealAkeWms/Data/Repositories/IProductionWorkplaceRepository.cs:7 (GetAllOrderedAsync als Quelle)
  - Gesamtsicht Sage-Buchungen (nach [[2026-11-23-sage-buchungen-gesamtsicht-spec]]): Spalte Abteilung in SageBookingQueue/Index + ColumnDefinitions
  - Migration AddStockMovementAbteilung, SQL/167_AddStockMovementAbteilung.sql (vorläufig), SQL/00_FreshInstall.sql (2 Stellen)
  - IDEALAKEWMSService.Tests/Services/SageLagerbuchungPayloadBuilderTests.cs, SageWareneingangServiceTests.cs:129,157,474,488,505,539 (Kopf-Memo-Erwartungen)
open_questions:
  - "1. Benutzername mit Leerzeichen (User.Name, z. B. AD-Anzeigename „Gerald Weichbold“): Leerzeichen in der Referenz durch Punkt ersetzen (→ Gerald.Weichbold wie im Beispiel)? Empfehlung: ja, nur Leerzeichen → Punkt, sonst unverändert."
  - "2. P-4 Feldlänge: Länge von KHKLagerplatzbuchungen.Referenz (und Kopf-Memo des EK-Belegs) im Sage-Testsystem – Prüfabfrage P-4 ausführen und Wert nennen. Empfehlung Kürzungsregel: erst Abteilungswert, dann Benutzer kürzen, Kennung nie."
  - "3. P-2 Sichtbarkeit: Landet das per SData gesendete Zeilen-Feld Referenz in KHKLagerplatzbuchungen.Referenz und ist es in der Sage-Oberfläche (Lagerbuchungsprotokoll) sichtbar? Schreibtest P-2 auf dem Testsystem. Empfehlung: vor dem Bau ausführen; fällt es nicht an, Referenz statt dessen ans Kopf-Memo anhängen (wie Sage-WE)."
  - "4. Auswahlliste Abteilung: alle Werkbänke (ProductionWorkplace) oder ohne die als „nicht in Sage“ markierten (SageMissingSince gesetzt, IDEAL)? Und ist „C1“ aus dem Beispiel ein Werkbank-Name (Prüfabfrage W-1)? Empfehlung: ohne „nicht in Sage“, Anzeige und gesendeter Wert = Werkbank-Name."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

In Sage soll man an jeder Buchung, die vom WMS kommt, erkennen können, **woher** sie kommt (Kennung `SM#…#`/`WE#…#`), **wer** sie
im WMS ausgelöst hat und bei Ausbuchungen **für welche Abteilung** (Werkbank) entnommen wurde. Heute schickt das WMS nur die
Korrelation; Benutzer und Abteilung fehlen (Befund in [[2026-11-24-sage-buchung-abteilung-und-benutzer]]).
Festgelegt sind die Richtung (Entscheidung Gerald 2026-11-24 in der Backlog-Notiz) und die Antworten vom 2026-11-24 (siehe „Festlegungen“).

## Festlegungen (Gerald 2026-11-24, über Koordination)

- **Format:** `WMS: <Kennung> <Benutzer>[ Abteilung:<Wert>]`, z. B. `WMS: SM#…# Gerald.Weichbold Abteilung:C1`. Die Abteilung steht nur da, wenn eine gewählt wurde.
- **Lagerbuchung:** Die Referenz kommt ins Zeilen-Feld `Referenz` des `LagerbuchungService`. Das Kopf-Memo mit der Korrelation bleibt unverändert.
- **Sage-WE:** Die Referenz kommt ins **Memo-Feld**, und zwar ins Kopf-Memo (Begründung in F3). Die Korrelation bleibt darin erhalten.
- **Benutzer:** der **WMS-Benutzername**, Beleg in F2.
- **Abteilung:** **derzeit nur bei Ausbuchung**, und zwar nur in den Masken „Ausbuchung“ und „Lagerplatz ausbuchen“. Ohne Abteilung bleiben
  die Lagerbestellungs-Entnahme, die Umbuchung und der Wareneingang (F4).
- `DPSMDELagerbuchung` wird nicht angefasst, P-1 entfällt für diesen Schritt (Backlog-Entscheidung).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Referenz an jeder Lagerbuchung (Zugang, Entnahme, Umbuchung – alles, was `SageLagerbuchungPayloadBuilder.Build` baut) im Zeilen-Feld `Referenz`.
- Benutzer im Kopf-Memo des Sage-WE-Belegs (`StandardBelegService`).
- Neues optionales Auswahlfeld „Abteilung“ (Werkbänke) in „Ausbuchung“ und „Lagerplatz ausbuchen“. Es wird in der neuen nullable Spalte
  `StockMovements.Abteilung` gespeichert.
- Spalte „Abteilung“ in der Gesamtsicht „Sage-Buchungen“ (nach deren Bau).

**Out-of-Scope**
- Kopf-`Bearbeiter` (P-3 ungeklärt; die Entscheidung lautet „zumindest Referenz“), Zeilen-`Memo`, `DPSMDELagerbuchung`, eine DPS-Erweiterung.
- Abteilung bei Lagerbestellungs-Entnahme, Umbuchung, Einbuchung und Wareneingang.
- Abteilung in der Lagerbewegungs-Liste `StockMovements/Index` (nicht verlangt; Folgewunsch möglich).
- Nachträgliches Ändern bereits gesendeter Sage-Buchungen.

## Fachliche Anforderungen

**F1 Referenz (eine Funktion für beide Wege)**
- `WMS: ` + Kennung + ` ` + Benutzer + (Abteilung gesetzt ? ` Abteilung:` + Abteilung : ``).
- Kennung = die Kennung, mit der die Buchung gesendet wird bzw. wurde. Für die Lagerbuchung ist das `SageBookingCorrelation.Kennung(item, kuerzel)`
  (`SageBookingCorrelation.cs:22-23`; gespeichert in `SageBookingQueueItem.SageKennung` beim Senden, `SageBookingQueueRepository.cs:94`).
  Für den Sage-WE ist es `SageWareneingang.Kennung(erste, kuerzel)` (`SageWareneingangPayload.cs:100`). Damit erzeugen Requeue, Recovery und
  Nacherfassung dieselbe Kennung.
- **Benutzer und Abteilung kommen immer aus der Bewegung bzw. dem Receipt, nie aus dem Queue-Eintrag.** Die Nacherfassung legt Queue-Einträge
  mit `CreatedBy = system:sage-booking` an (`SageBookingQueueRepository.cs:169`), die Bewegung behält aber ihren Benutzer.
- Leer (Benutzer leer bzw. nur Leerraum): Der Teil entfällt, es entsteht kein doppeltes Leerzeichen.
- Kürzungsregel (Länge `ReferenzMaxLaenge` = Wert aus P-4, als Konstante im Code, keine Einstellung): Zuerst wird der Abteilungswert gekürzt
  (ist er leer, entfällt das Wort `Abteilung:` ganz), danach der Benutzer. **Kennung und Präfix werden nie gekürzt.** Die längste Kennung ist
  `SM#<5 Zeichen>#<int>#` = 20 Zeichen, mit Präfix 25.
  Ist `ReferenzMaxLaenge` < 25, ist das ein Dev-Lauf-Blocker und wird gemeldet.

**F2 Benutzer = WMS-Benutzername**
- Der WMS-Benutzername ist `User.Name` (`Models/User.cs:7-10`, Pflicht, max. 200). Ein Feld `Username` gibt es am `User` nicht. Bei der Anmeldung
  wird `user.Name` in die Session geschrieben (`AccountController.cs:150`), und `ICurrentUserService.GetDisplayName()` liefert ihn
  (`CurrentUserService.cs:43-47`).
- Jede Bewegung speichert ihn beim Anlegen als `StockMovement.CreatedBy = GetDisplayName()`, belegt an allen Ein-, Aus- und Umbuchungswegen:
  `StockMovementsController.cs:162,274,367,460,606,697`, `WarehousePickingController.cs:425`, `PurchaseOrdersController.Receive.cs:131,153`
  (Bewegung) und `:166` (Receipt). Der Wert steht damit **an der Buchungszeile selbst**, und der Dienst lädt die Bewegung ohnehin
  (`SageBookingQueueRepository.cs:45-48`). Ein Include von `User` ist nicht nötig. Weil der Wert ein Schnappschuss ist, bleibt er
  auch nach einer Umbenennung des Benutzers stabil, und ein Requeue sendet denselben Text.
- Lagerbuchung: `StockMovement.CreatedBy`. Sage-WE: `PurchaseOrderReceipt.CreatedBy` des ersten Receipts der Gruppe. Das ist derselbe Wert wie
  `CreatedBy` der Bewegung, beide kommen aus `name` (`PurchaseOrdersController.Receive.cs:131,153,166`).
- Fallback ohne Sitzung (`GetDisplayName()` liefert dann den Windows- bzw. `SYSTEM`-Namen, `CurrentUserService.cs:49-52`): Der Wert wird so
  gesendet, wie er gespeichert ist.
- Bewusst **nicht** übernommen: die CSV-Erfasser-Regel `ExtractSam(User.WindowsUserName) ?? User.Name` (`WareneingangCsvService.cs:172`).
  Gerald hat „WMS-Benutzername“ festgelegt, nicht den Windows-Login.
- Leerzeichen → Punkt: nach Rückfrage 1.

**F3 Sage-WE: Referenz im Kopf-Memo**
- Das StandardBelegService-Positionsschema hat **kein** Feld `Referenz`. Belegt ist das in [[sage-sdata-commonwawiservices]] §StandardBelegPosition,
  das Feld `Artikelmemo` gibt es, `Referenz` nicht. Am Kopf gibt es `Memo`, `Referenznummer`, `Referenzzeichen` und `Bearbeiter`
  (§Belegkopf-Felder, Zeile 29).
- Gerald hat „Memo-Feld“ festgelegt. In Frage kommen zwei Felder:
  - Kopf-`Memo` = heute genau die Kennung `WE#…#` (`SageWareneingangPayload.cs:100`).
  - Positions-`Artikelmemo` = `[Eingabe | ]WE#…# [LS …] Platz …` (`:63-68`, `:89`).
- **Empfehlung und Festlegung in dieser Spec: Kopf-Memo** = `WMS: WE#…# <Benutzer>`. Gründe:
  - Die Wiedererkennung sucht mit `Memo like '%<Kennung>%'` (`SdataWareneingangLookupReader.cs:44-45`). Treffer mit **anderen**
    WMS-Kennungen erkennt `FremdeKennung` per Regex `WE#(?:[A-Z][A-Z0-9]{0,4}#)?\d+#` (`:54-55`, genutzt in `SageWareneingangService.cs:191-198`).
    Präfix und Benutzer enthalten kein `WE#…#`, deshalb finden beide Prüfungen weiterhin genau den eigenen Beleg. Das gilt auch für Belege,
    die vor der Änderung mit dem nackten `WE#…#` gesendet wurden.
  - Der Kopf ist **einmal je Beleg** da, und ein Beleg ist ein Absenden eines Benutzers (eine Gruppe = ein `SageWeVorgang`,
    `SageWareneingangPayload.cs:57-58`). Den Benutzer je Position zu wiederholen bringt nichts.
  - Das Artikelmemo ist schon voll (Eingabe des Lagers bis 200 Zeichen, `PurchaseOrderReceiveViewModel.cs:39`, plus Kennung, LS und Platz)
    und bleibt **unverändert**.
  - Dass ein vom WMS gesendetes Kopf-Memo in Sage ankommt, ist schon belegt (Memo-Lookup v1.99.4/v1.99.9, Schreibtests 2027-398675/398676).
    Für diesen Weg ist kein neuer Schreibtest P-2 nötig, nur die Länge (P-4).
- Der Sage-WE trägt keine Abteilung (Festlegung 3).

**F4 Abteilung bei Ausbuchung**
- Alle Stellen, die eine `MovementType.Ausbuchung` schreiben (Grep `MovementType = ` über `IdealAkeWms/` und `new StockMovement` über das Repo;
  der Dienst schreibt nur `SageEinbuchung`/`SageAusbuchung` in `LagerbestandSyncService.cs:179,247`, die nie an Sage gehen):

  | Maske | Code | Abteilung |
  |---|---|---|
  | Ausbuchung (Einzel) `StockMovements/Outbound` | `StockMovementsController.cs:302-377`, Schreiben `:355-369` | **ja** |
  | Lagerplatz ausbuchen `StockMovements/OutboundAll` | `:472-504`, Schreiben `:594-608` (alle Zeilen erhalten dieselbe Abteilung) | **ja** |
  | Lagerbestellung/Glasbestellung abschließen (Entnahme) | `WarehousePickingController.cs:409-428` | nein (Festlegung 3) |

  Eine Mehrfach-Ausbuchungsmaske gibt es nicht. `InboundBulk` schreibt nur Einbuchungen (`StockMovementsController.cs:269`).
- Das Feld ist optional, Beschriftung „Abteilung“, erster Eintrag leer („– keine –“). Die Werte sind die Werkbänke aus
  `IProductionWorkplaceRepository.GetAllOrderedAsync()` (`IProductionWorkplaceRepository.cs:7`; Werkbank = `ProductionWorkplace`, Glossar Zeile 29),
  angezeigt und gespeichert wird `Name` (max. 200, `ProductionWorkplace.cs:7-10`). Den Filter „nicht in Sage“ regelt Rückfrage 4.
- Der Server prüft beim Speichern, ob der Wert leer ist oder einem Namen der Liste entspricht (Vergleich ohne Groß/Klein, getrimmt). Bei einem
  unbekannten Wert gibt es einen `ModelState`-Fehler „Unbekannte Abteilung“ (Outbound) bzw. eine `WarningMessage`, und es wird nichts gebucht
  (OutboundAllConfirm, Muster `Zurueck(...)`, `:550-554`).
- Gespeichert wird der Name als Schnappschuss in `StockMovement.Abteilung`. Leer = `NULL`, und dann wird nichts gesendet.
- Gesendet wird die Abteilung nur in der Lagerbuchungs-Referenz, und zwar wenn die Bewegung an Sage geht (unverändert: `SageLagerbuchungAktiv` + Platz-Flag).
  Bewegungen auf WMS-only-Plätzen speichern die Abteilung trotzdem.

**F5 Gesamtsicht**
- „Erfasst von“ zeigt den Benutzer schon (Gesamtsicht F6 = `StockMovement.CreatedBy`, [[2026-11-23-sage-buchungen-gesamtsicht-spec]] Zeile 188).
  Dazu kommt eine Spalte `abteilung` „Abteilung“: bei Lagerbuchungen `StockMovement.Abteilung`, bei Sage-WE leer. Standard: ausgeblendet über das
  Zahnrad (wie die WE-Zusatzspalten), mit Server-Spaltenfilter. Der Eintrag kommt in den ViewConfig `SageBookingQueue` (`ColumnDefinitions`).
- **Reihenfolge:** Diese Spec wird **nach** der Gesamtsicht gebaut (`depends_on`). Würde sie vorher gebaut, landete die Spalte in einer Seite,
  die die Gesamtsicht ohnehin neu baut.

## Technischer Loesungsentwurf

- **Referenz-Funktion:** eine reine statische Funktion `SageReferenz.Build(string kennung, string? benutzer, string? abteilung)` mit
  `MaxLaenge`-Konstante und Kürzung nach F1. Sie liegt neben `SageBookingCorrelation` im Dienst (`IDEALAKEWMSService/Services/`). Ein Interface
  gibt es nicht, testbar wie `SageBookingCorrelationTests`.
- **Lagerbuchung:** `SageLagerbuchungZeile` bekommt `[JsonPropertyName("Referenz")] public string Referenz { get; set; } = string.Empty;`.
  `SageLagerbuchungPayloadBuilder.Build` setzt `Referenz = SageReferenz.Build(marker ?? Marker(movement.Id), movement.CreatedBy, movement.Abteilung)`.
  Das Kopf-`Memo` (`:71`) bleibt **unverändert**. Der Lookup `KHKLagerplatzbuchungen WHERE Memo LIKE` (`SageBuchungLookupReader.cs:15`) bzw.
  `Memo like` (`SdataBuchungLookupReader.cs:34`) sucht nur im Memo und ist nicht betroffen.
- **Sage-WE:** `SageWareneingangPayloadBuilder.Build` setzt `Memo = SageReferenz.Build(SageWareneingang.Kennung(erste, kuerzel), erste.CreatedBy, null)`.
  `PositionsMemo` bleibt. Die Länge kommt aus P-4b; ist sie kleiner als die Referenz, wird der Benutzer gekürzt, nie die Kennung.
  Die Wiedererkennung ist unabhängig vom gesendeten Memo-Text. Die Suchkennung wird eigens berechnet (`SageWareneingangService.cs:105-106`
  → `PruefenAsync`), und auch die gespeicherte Kennung (`:147`) entsteht unabhängig davon. `payload.Memo` steht nur in der Warnmeldung `:165`,
  dort erscheint künftig die lange Referenz. Das ist unschädlich.
- **Requeue/Recovery/Nacherfassung:** Es gibt nur einen Build-Aufruf je Weg (`SageBookingWorker.cs:405`, `SageWareneingangService.cs:136`), und alle
  Eingaben sind gespeicherte Felder. Jeder erneute Versand erzeugt deshalb dieselbe Referenz. Eine Änderung an Worker oder Queue ist nicht nötig.
- **Abteilung UI:** Die ViewModels `StockMovementCreateViewModel` (Outbound) und `OutboundAllViewModel` bekommen `string? Abteilung` und
  `List<ProductionWorkplace> Werkbaenke`. Gefüllt werden sie in GET und in jedem Re-Render-Zweig von `Outbound` POST (`:320-349`).
  `OutboundAllConfirm` bekommt den Parameter `string? abteilung`. Dass das Formular auf beiden Masken dasselbe ist, regelt ein kleines
  `<select class="form-select">`. Für zwei Stellen lohnt kein neues Partial.
- **Gesamtsicht:** Spalte nach F5, Muster der Gesamtsicht-Spalten.
- Kein neuer Controller, keine neue Rolle, kein neuer Dienst.

## Migrations-/SQL-Auswirkungen

- Model `StockMovement.Abteilung` (`[StringLength(200)] public string? Abteilung`, Display „Abteilung“) → `dotnet ef migrations add AddStockMovementAbteilung`.
- `SQL/167_AddStockMovementAbteilung.sql` (Nummer vorläufig, siehe `sql_vorschlag`): Die DDL steht in einem eigenen Batch mit Guard
  `IF COL_LENGTH('dbo.StockMovements','Abteilung') IS NULL ALTER TABLE dbo.StockMovements ADD Abteilung NVARCHAR(200) NULL;`. Danach folgt in einem
  separaten Batch ein `__EFMigrationsHistory`-Insert mit `IF NOT EXISTS`.
- `SQL/00_FreshInstall.sql` wird an **zwei** Stellen nachgezogen: die Spalte in `CREATE TABLE StockMovements` und die `MigrationId`.
- Additiv und nullable, nicht daten-destruktiv, kein Backfill. `SQL/AgentJobs/` ist nicht betroffen (kein Pflichtfeld, kein MERGE auf StockMovements).
  Der Dev-Lauf bestätigt das per Grep.

## Einstellungen

keine. Begründung: Die Referenz ist ein Zusatztext ohne eigene Schaltwirkung. Gesendet wird ohnehin nur unter den bestehenden Schaltern
(`SageLagerbuchungAktiv`, Platz-Flag `SageBuchungErlaubt`, `WareneingangBestellungAktiv`). Die Feldlänge ist ein Sage-Fakt (P-4) und keine Wahl,
deshalb steht sie als Konstante im Code.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

- `StockMovement.Abteilung` wird nur beim Anlegen gesetzt, zusammen mit den bestehenden `CreatedAt/CreatedBy/CreatedByWindows`
  (`StockMovementsController.cs:366-368`, `:605-607`). Einen Update-Weg gibt es nicht, `Modified*` ist nicht betroffen.
- Queue- und Receipt-Audit sind unverändert. Die Referenz wird nicht gespeichert, sie wird bei jedem Versand neu berechnet.

## Akzeptanzkriterien

1. Eine Ausbuchung auf einem Sage-Platz ohne Abteilung sendet im Zeilen-Feld `Referenz` `WMS: SM#[K#]<id># <Benutzer>`. Das Kopf-`Memo` ist
   byte-gleich zu vorher (`IdealAkeWms Lagerbuchung SM#…#`).
2. Eine Ausbuchung mit Abteilung „X“ sendet `… <Benutzer> Abteilung:X`. `StockMovements.Abteilung` = „X“.
3. Einbuchung und Umbuchung senden die Referenz ohne Abteilungsteil.
4. `<Benutzer>` = `StockMovement.CreatedBy` (= WMS-Benutzername `User.Name` zum Buchungszeitpunkt), die Leerzeichen-Regel steht in Rückfrage 1.
   Auch eine **nacherfasste** Buchung (Queue `CreatedBy = system:sage-booking`) sendet den Benutzer der Bewegung.
5. Requeue nach Fehler und Recovery eines hängenden Eintrags senden dieselbe Referenz wie der erste Versuch (Unit-Test: zweimal Build mit
   gespeicherter `SageKennung` → gleicher Text).
6. Der Memo-Lookup findet eine gesendete Buchung weiterhin (bestehende `SageBookingCorrelationTests` grün, kein Requeue-Doppelversand).
7. Ein Sage-WE-Beleg hat das Kopf-Memo `WMS: WE#[K#]<id># <Benutzer>`. Das Positions-Artikelmemo ist unverändert. Die Wiedererkennung
   (`NachMemoAsync`, `FremdeKennung`) findet Belege mit neuem und altem Kopf-Memo; `FremdeKennung(neuesMemo, eigeneKennung)` = false.
8. Ist die Referenz länger als `MaxLaenge`, wird zuerst die Abteilung gekürzt, dann der Benutzer. Die Kennung ist immer vollständig (Unit-Tests
   mit 200-Zeichen-Abteilung und -Benutzer).
9. In „Ausbuchung“ und „Lagerplatz ausbuchen“ gibt es das Auswahlfeld „Abteilung“ (leer + Werkbänke). Leer gelassen → `Abteilung = NULL`.
10. Ein manipulierter POST mit unbekannter Abteilung wird abgewiesen. Es entsteht keine Bewegung, und die Meldung nennt den Grund.
11. Lagerplatz ausbuchen mit Abteilung: Alle erzeugten Bewegungen tragen dieselbe Abteilung.
12. Lagerbestellungs-Abschluss, Umbuchung, Einbuchung und Wareneingang haben kein Abteilungsfeld. Ihre Bewegungen haben `Abteilung = NULL`.
13. Die Gesamtsicht hat die Spalte „Abteilung“ (per Zahnrad), mit Spaltenfilter, bei WE-Zeilen leer.
14. SQL/167 läuft zweimal fehlerfrei (idempotent). FreshInstall enthält Spalte und MigrationId.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md`, Nummer vom Dev-Lauf (vorläufig nächste freie nach TS-189), „Sage-Übergabe: Referenz mit Benutzer
und Abteilung“, plus Eintrag in `secondbrain/tests/testszenarien-index.md`:
- **.1** Ausbuchung (DEV AKE, Sage-Platz, `SageLagerbuchungAktiv` an) ohne Abteilung → Gesamtsicht bestätigt; Sage-Test: P-2-Abfrage zeigt `Referenz`
  = `WMS: SM#…# <Benutzer>`, Memo unverändert.
- **.2** Wie .1 mit Abteilung → `… Abteilung:<Werkbank>`; die Gesamtsicht-Spalte zeigt sie.
- **.3** Lagerplatz ausbuchen mit Abteilung, 3 Artikel → 3 Bewegungen, alle mit Abteilung, 3 Sage-Buchungen mit gleicher Abteilung.
- **.4** Einbuchung und Umbuchung → Referenz ohne Abteilung.
- **.5** Requeue: Sage-Adresse falsch → Fehler; korrigieren, „Erneut senden“ → dieselbe Referenz, keine Doppelbuchung.
- **.6** Nacherfassung: Queue-Eintrag einer Bewegung löschen (DEV), Worker-Lauf → Referenz mit dem Benutzer der Bewegung, nicht `system:sage-booking`.
- **.7** Sage-WE (DEV IDEAL bzw. wo `WareneingangBestellungAktiv` an): Kopf-Memo im EK-Beleg = `WMS: WE#…# <Benutzer>`; erneut senden eines Fehler-Vorgangs
  → Wiedererkennung greift.
- **.8** Lagerbestellung abschließen → Entnahme ohne Abteilungsfeld, Referenz ohne Abteilung.
- **Negativ .9** POST mit erfundener Abteilung (DevTools) → abgewiesen, keine Bewegung.
- **Negativ .10** WMS-only-Platz mit Abteilung → gespeichert, nichts an Sage.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | kein Epic | – | – |

## Deploy

**Vorläufig – der QA-Lauf bestätigt den Abschnitt gegen den echten Diff.**

- **Web-App:** ja (Masken Ausbuchung/Lagerplatz ausbuchen, Gesamtsicht-Spalte, Model).
- **Service:** ja (Payload-Builder Lagerbuchung und Sage-WE). Das EF-Modell ist geteilt, deshalb startet der Dienst **nach** dem Schema-Schritt.
- **Migration:** ja, additiv (`StockMovements.Abteilung NULL`), nicht destruktiv.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):
  - Id `SQL167-StockMovementAbteilung`, Version = vergebene Bündel-Version, Typ `Schema`, Firma `beide`, Phase `Schema`, Ausführung `automatisch`,
    Text „Lagerbewegungen: Spalte Abteilung (Ausbuchung, geht als Referenz an Sage)“. Die Reihenfolge folgt der Deploy-Reihenfolge der
    Schema-Einträge, nicht der Dateisortierung.
  - Id `Info-SageReferenz`, Typ `Info`, Firma `beide`, Phase `Nachher`, Ausführung –, Text „Sage-Lagerbuchungen tragen ab jetzt im Feld Referenz
    ‚WMS: Kennung Benutzer [Abteilung:…]‘, Sage-Wareneingänge im Kopf-Memo; bei Ausbuchungen kann eine Abteilung gewählt werden.“
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Prüfabfragen (Mensch – Sage-Testsystem bzw. WMS-DEV, nicht angenommen)

- **P-4 Feldlängen** (Sage-DB AKE-Test und IDEAL-Test):
  ```sql
  SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE (TABLE_NAME = 'KHKLagerplatzbuchungen' AND COLUMN_NAME IN ('Referenz','Memo'))
     OR (TABLE_NAME IN ('KHKEKBelege','KHKEKVorgaenge') AND COLUMN_NAME = 'Memo');
  ```
  Ergebnis → `SageReferenz.MaxLaenge` (Lagerbuchung) bzw. Grenze Kopf-Memo (WE). `-1` = MAX → keine Kürzung nötig.
  **Unverifiziert**, bis die Abfrage gelaufen ist: Hängt das Kopf-Memo des EK-Belegs an `KHKEKBelege`? Den Tabellennamen bestätigt der Mensch.
- **P-2 Referenz kommt an und ist sichtbar** (Schreibtest auf dem Testsystem nach dem Bau auf DEV oder vorab per Postman mit `"Referenz":"WMS: SM#TEST#1# Test"`):
  ```sql
  SELECT TOP 5 BuchungsID, Bewegungsart, Artikelnummer, Memo, Referenz, Bewegungsdatum
  FROM dbo.KHKLagerplatzbuchungen
  WHERE Memo LIKE '%IdealAkeWms Lagerbuchung%'
  ORDER BY BuchungsID DESC;
  ```
  Zusätzlich in der Sage-Oberfläche (Lagerbuchungsprotokoll) prüfen, ob „Referenz“ angezeigt wird. Achtung: Sage füllt `Referenz` bei eigenen
  Buchungen mit Belegbezügen (Beispiel „2007-200001“, [[2026-07-29-sage-lagerbuchungen-spec]] Zeile 1023f.). Ob ein gesendeter Wert überschrieben
  wird, zeigt erst P-2.
- **W-1 Werkbank-Namen** (WMS-DEV AKE und IDEAL): `SELECT Id, Name, SageArbeitsplatznummer, SageMissingSince FROM dbo.ProductionWorkplaces ORDER BY Name;`
  Damit ist zu sehen, ob „C1“ ein Werkbank-Name ist (Rückfrage 4).

## Offene Rueckfragen

1. Benutzername mit Leerzeichen (`User.Name`, bei AD-Benutzern der AD-Anzeigename, `UsersController.cs:221`): Leerzeichen in der Referenz durch
   Punkt ersetzen (→ `Gerald.Weichbold` wie im Beispiel)? **Empfehlung:** ja, nur Leerzeichen → Punkt, sonst unverändert (Groß/Klein bleibt).
2. P-4 Feldlänge `Referenz` (Lagerbuchung) und Kopf-Memo (EK-Beleg): Abfrage oben ausführen und Wert nennen. **Empfehlung** Kürzungsregel wie F1
   (erst Abteilung, dann Benutzer, Kennung nie). Ohne Wert kann der Dev-Lauf die Konstante nicht setzen.
3. P-2: Landet `Referenz` in `KHKLagerplatzbuchungen.Referenz` und ist sie in Sage sichtbar? **Empfehlung:** vor dem Bau per Postman prüfen. Kommt sie
   nicht an oder wird sie überschrieben, hängt man die Referenz statt dessen an das Kopf-Memo der Lagerbuchung an
   (`IdealAkeWms Lagerbuchung SM#…# | <Benutzer> Abteilung:…`, Lookup bleibt `%SM#…#%`).
4. Auswahlliste Abteilung: alle Werkbänke oder ohne „nicht in Sage“ (`SageMissingSince` gesetzt, nur IDEAL)? Ist „C1“ ein Werkbank-Name (W-1)?
   **Empfehlung:** ohne „nicht in Sage“. Angezeigt und gesendet wird `ProductionWorkplace.Name`.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Leerzeichen im Benutzernamen → Punkt? → **Nein. Benutzername genau so, wie der User im WMS angelegt ist (`User.Name`), kein Punkt (Gerald 2026-11-24).**
2. P-4 Feldlänge Referenz / Kopf-Memo EK-Beleg (Wert) → **Geprüft 2026-11-24 per Schema-Abfrage (nur lesend, `INFORMATION_SCHEMA.COLUMNS`, Sage-DB `ake` auf AKESQL20):
   `KHKLagerplatzbuchungen.Referenz varchar(31)`, `KHKLagerplatzbuchungen.Memo varchar(max)`, `KHKLagerplatzbuchungenJobs.Memo varchar(max)`
   (+ `Benutzer`), `KHKEKBelege.Memo varchar(max)`, `KHKEKBelege.Bearbeiter varchar(128)`. `DPSMDELagerbuchung.Abteilung varchar(10)`
   (DPS-MDE-Tabelle, Spalten ID, Tan, MA, Artikel, Menge, HLP, ZLP, …, OLBuchungsID – wird vom WMS nicht beschrieben).**
3. P-2 Referenz landet sichtbar in Sage? → **Ja (Beleg Gerald 2026-11-24, Sage-Testsystem, Lagerbuchungsprotokoll 23.11.2026, Job 592161,
   EM, EK019956): Spalte „Referenz“ zeigt `IdealAkeWms Lagerbuchung SM#ATI` = genau 31 Zeichen. Sage füllt `Referenz` heute selbst aus dem
   Kopf-Memo und schneidet bei 31 ab. Die Spalte „Abteilung“ derselben Liste kommt aus der DPS-MDE-Welt (`DPSMDELagerbuchung`, 10 Zeichen).**
4. Auswahlliste Abteilung (ohne „nicht in Sage“?) / „C1“ Werkbank-Name? → **ok, wie Empfehlung (Gerald 2026-11-24).**
5. NEU (aus 2/3): `Referenz` hat nur **31 Zeichen** – `WMS: SM#ATI#12345# Gerald Weichbold Abteilung:C1` passt nicht. Vorschlag:
   `Referenz` = **`<Abteilung> <Benutzer>`** gekürzt auf 31 (Abteilung zuerst, max. 10 wie DPS), voller Text
   `WMS: SM#…# <Benutzer> Abteilung:<x>` ins **Zeilen-`Memo`** (varchar(max)); die Kennung bleibt im Kopf-Memo (Wiedererkennung). Sage-WE:
   voller Text ins Kopf-Memo (varchar(max), keine Kürzung nötig). →
