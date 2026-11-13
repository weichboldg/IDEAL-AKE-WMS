---
type: spec
title: "Wareneingang Teil 3: Rahmenabrufe (EBZ) lesen, anzeigen, buchen"
slug: 2026-11-11-wareneingang-teil-3-rahmenabrufe-spec
status: InUmsetzung
created: 2026-11-13
updated: 2026-11-13
source_backlog: "[[2026-11-11-wareneingang-teil-3-rahmenabrufe]]"
depends_on: "[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]]"
verwandt: "[[2026-11-12-offene-bestellungen-mahnmail-lieferverzug-spec]], [[2026-11-12-wareneingang-barcode-lieferschein-csv-export-spec]]"
version_vorschlag: "1.99.13 (Bündel steht auf 1.99.12, IdealAkeWms/AppVersion.cs:5; Dev-Lauf prüft)"
sql_nummern: "keine Migration geplant; nächste freie Nummer wäre 147 (SQL/146_AddWareneingangBarcodeCsv.sql ist die höchste im Bündel)"
testszenario: "TS-130 (TS-129 ist die höchste in docs/TESTSZENARIEN.md; Dev-Lauf prüft)"
task: "[[2026-11-13-buendel-v1-99-13-goal-lauf]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: AKE
affected_code:
  - "IDEALAKEWMSService/Services/Sdata/EinkaufBestellungenSyncService.cs:11-14 (Record + MengeBasis), :32-37 (Where/Select/Belegart), :66-67 (zwei Abfragen), :82-87 (Empty-Guard je Belegart), :112-113 (Belegart-Prüfung EBB+EBZ), :127-143 (Mengen je Belegart), :140 (Belegkennzeichen der Zeile), :179-192 (Counts)"
  - "IdealAkeWms/Models/SagePurchaseOrderLine.cs:3-10,16,28-33 (Konstante EBZ, IstRahmenabruf, Kommentare je Belegart)"
  - "IdealAkeWms/Data/Repositories/PurchaseOrderReceiptRepository.cs:6-28 (ErfasstJeBelPos: Abzuziehen/Rest mit Zeile statt SyncedAt - EBZ zieht alles ab)"
  - "IdealAkeWms/Models/ViewModels/PurchaseOrderReceiveViewModel.cs:35 (Rest - unverändert, liest ImWmsAbzuziehen)"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.cs:41-55 (ColumnMap geliefert), :58-65 (Hinweis Rahmenabruf), :67-114 (Filter auchErfassteAbrufe + Ausgeblendet-Zähler), :129 (Abzuziehen(l))"
  - "IdealAkeWms/Models/ViewModels/PurchaseOrderListViewModel.cs:4-20 (AuchErfassteAbrufe, AusgeblendeteAbrufe)"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.Receive.cs:22 (Meldung), :163 (SageWeAktiv je Belegart über SageWareneingang.GiltFuer)"
  - "IdealAkeWms/Services/SageWareneingang.cs (neu: RahmenabrufMitSageWe-Konstante + GiltFuer)"
  - "IdealAkeWms/Views/PurchaseOrders/Index.cshtml:28 (Stand-Text), :62-67 (Filter), :111 (geliefert „–“)"
  - "IdealAkeWms/Views/PurchaseOrders/Receive.cshtml:45-56 (Kopf: Text-Badge Rahmenabruf), :86-88 (Barcode-Hinweis bei Abruf ohne Sage-WE)"
  - "IDEALAKEWMSService/Services/LieferverzugMailService.cs:73 (Rest(l))"
  - "IDEALAKEWMSService/Services/LieferverzugMailHtml.cs:114,132-133 (geliefert „–“ bei EBZ)"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs:112,262 (Text Sync:EinkaufBestellungenEnabled: EBB und EBZ)"
  - "Kommentare: IDEALAKEWMSService/Services/SageWareneingangPayload.cs:8, IDEALAKEWMSService/Program.cs:85, IdealAkeWms/Services/SyncLogger/SyncLogServices.cs:37, IdealAkeWms/Data/ApplicationDbContext.cs:1389"
  - "Tests: IDEALAKEWMSService.Tests/Services/Sdata/EinkaufBestellungenSyncTests.cs:55,60; IdealAkeWms.Tests/Repositories/SageWareneingangGrundlageTests.cs:84-108; PurchaseOrdersControllerTests, PurchaseOrdersReceiveTests, LieferverzugMailTests"
  - "docs/TESTSZENARIEN.md (TS-130, Text TS-116 Z. 11376), README.md:793, Views/Help/Index.cshtml:84,125 + Changelog.cshtml, beide AppVersion.cs, scripts/deploy/nachlauf.psd1 (eine Info)"
open_questions:
  - "1 Konflikt Entscheidung 2 und 4: Ein in Sage direkt (ohne WMS) gelieferter Abruf bleibt im WMS mit Rest > 0 und wäre nach dem Liefertermin für die Kreditor-Mail fällig - der Lieferant würde für bereits Geliefertes erinnert. Abrufe auch in der Kreditor-Mail (wie entschieden) oder nur in der Einkauf-Sammelliste? (Empfehlung: nur Einkauf-Sammelliste, bis der Einkauf die Testmails gesichtet hat)"
  - "2 Schreibtest: Reicht die Freigabe vom 2026-11-12 (Entscheidung 3), oder soll der Dev-Lauf wie in Teil 2 den fertigen Request vor dem einen POST Gerald zeigen und auf sein OK warten? (Empfehlung: vorher zeigen - dann läuft E2 interaktiv, nicht im /goal-Lauf)"
  - "3 Erfolgskriterium Schreibtest: Zählt der Test nur als 'klappt', wenn am angelegten ELL-Beleg ReferenzBelID = BelId des Abrufs ist (sonst findet das Sicherungsnetz aus Teil 2 E5 einen Abruf-Wareneingang ohne Kennung nicht)? (Empfehlung ja; sonst bleibt EBZ bei NichtNoetig)"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-13"
---

# Wareneingang Teil 3: Rahmenabrufe (EBZ) lesen, anzeigen, buchen

Backlog [[2026-11-11-wareneingang-teil-3-rahmenabrufe]] (aus Freigabe-Antwort 6 von
[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]]). Verbindlich sind die **„Entscheidungen Gerald 2026-11-12“** des Backlogs:

| # | Entscheidung | Umsetzung in dieser Spec |
|---|---|---|
| 1 | Abrufmenge = `MengeBasis`; P-W8 bestätigt das vorher lesend | E0 (Blocker bei Abweichung), F1 |
| 2 | Abrufe mit Rest 0 im WMS standardmäßig ausblenden, Filter „auch vollständig erfasste“; Grenze „in Sage direkt gebucht wird nicht abgezogen“ akzeptiert, kommt in die Hilfe | F3, E3 |
| 3 | Sage-WE auch für Abrufe (EBZ → ELL), wenn **ein** Schreibtest mit genau einem Abruf-Beleg am Sage-AKE-Test klappt (über DEV, Kürzel `DEV`); sonst `NichtNoetig` | F4, E2 |
| 4 | Lieferverzug-Mail auch für Abrufe, mit dem Liefertermin des Abrufs | F5 (Rückfrage 1 zum Konflikt mit 2) |
| 5 | Barcode und CSV-Übergabe gelten für Abrufe genauso | F6 |

Alle Zeilennummern unten sind im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Stand v1.99.12) neu geprüft.

## Ziel / Nutzen (das Warum)

Rahmenabrufe (Sage-Belegkennzeichen `EBZ`) erscheinen heute weder in „Offene Bestellungen“ noch in „Wareneingang zu Bestellung“: Der Abgleich liest
nur `EBB` (`EinkaufBestellungenSyncService.cs:32`, doppelt abgesichert `:112-113`). Am AKE-Test sind das 108 offene Abruf-Positionen (Teil 1, P-W4).
Das Lager kann einen Abruf-Wareneingang deshalb nur am WMS vorbei buchen. Ziel: Abrufe wie Bestellungen anzeigen, buchen und – nach einem
erfolgreichen Schreibtest – als Sage-Wareneingang an Sage übergeben; Lieferverzug-Mail, Barcode und CSV gelten mit.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- F1 Abgleich liest `EBB` **und** `EBZ`; Abrufmenge aus `MengeBasis`.
- F2 Rest-Regel für Abrufe (an der vorhandenen einen Stelle).
- F3 Anzeige: Kennzeichnung „Rahmenabruf“, „geliefert (Sage)“ = „–“, Rest-0-Abrufe ausgeblendet mit Filter, angepasste Meldungen.
- F4 Sage-WE für Abrufe hinter einer Code-Konstante, festgelegt durch den einen Schreibtest; sonst `NichtNoetig`.
- F5 Lieferverzug-Mail mit Abrufen (fällt ohne eigenen Code an; nur Anzeige „–“).
- F6 Barcode/CSV für Abrufe (ohne eigenen Code; nur Sperre, wenn Abrufe ohne Sage-WE laufen).

**Out-of-Scope**
- Etikettendruck, Charge/Seriennummer, Rechnungsprüfung (wie Teil 1/2).
- Abziehen von Abruf-Lieferungen, die in Sage direkt gebucht wurden (Entscheidung 2: akzeptierte Grenze).
- Rahmenverträge (`ERK`) selbst und Rahmen-Vorgangsmengen je Abruf.
- Ausblenden von `EBB`-Positionen mit Rest 0 (Entscheidung 2 betrifft nur Abrufe; EBB bleibt wie heute).
- IDEAL: kein SData-Endpunkt, `Sync:EinkaufBestellungenEnabled` dort aus (`ServiceSettingDefinitions.cs:112`). IDEAL bekommt den Code, ohne Wirkung.
- Weitere Sage-Schreibtests: genau **ein** POST in E2, kein weiterer (auch kein Wiederholen, keine Überlieferung, kein zweiter Abruf).

## Fachliche Anforderungen

### Belegter Ausgangsstand

- **Datenbasis:** SData `EinkaufPositionenInfo` (`EinkaufBestellungenSyncService.cs:31`), heute `where Belegkennzeichen eq 'EBB'` (`:32`), Select
  ohne `Menge`/`MengeBasis`/`VorPosId` (`:33-35`). Die Felder `Menge`, `MengeBasis`, `VorPosId` sind laut P-W1 (Teil 1) Teil der Ressource; mit
  `select` gelesen hat sie das WMS noch nie. Fallstrick: Ein falscher Feldname im `select` gibt HTTP 500 ([[fallstricke]] „Sage-SData-Lesen“).
- **Mengen heute:** `MengeBestellt/Geliefert = PositionGGBestellt/PositionGGGeliefert` (`:128-129`), `MengeOffen = bestellt − geliefert` (`:143`).
  Diese Felder sind Mengen der **Vorgangsposition**; bei EBZ gelten sie für den ganzen Rahmen, also für bis zu 18 Abrufe (Teil 1 B-1, EK013128).
  Für EBZ dürfen sie nie als bestellt/geliefert je Zeile verwendet werden.
- **Sichtbarkeit:** Die Sage-View filtert auf die offene Vorgangsposition (Teil 1 P-W4). Ein Abruf bleibt im Cache, solange der Rahmen offen ist,
  auch wenn er selbst voll geliefert ist.
- **Cache:** `SagePurchaseOrderLine.Belegkennzeichen` gibt es schon (`SagePurchaseOrderLine.cs:16`, nvarchar(3), `ApplicationDbContext.cs:1395`);
  der Abgleich schreibt heute fest die Konstante `EBB` (`EinkaufBestellungenSyncService.cs:140`). `BelPosId` ist UNIQUE (`ApplicationDbContext.cs:1410`).
- **Bezug Wareneingang → Abruf** besteht: `PurchaseOrderReceipt.BelPosId`/`BelId` werden beim Buchen aus der Cache-Zeile gesetzt
  (`PurchaseOrdersController.Receive.cs:130`); ein Absenden betrifft genau eine `BelId` (`:45`) = einen Abruf-Beleg. Keine Migration nötig.
- **Rest-Regel – wo sie wirklich liegt:** Die Regel steht seit v1.99.4/v1.99.10 **nicht** in `PurchaseOrderReceiveRow.Rest`, sondern in
  `ErfasstJeBelPos` (`PurchaseOrderReceiptRepository.cs:11-28`): `Abzuziehen(syncedAt)` (`:20`) = alle Receipts außer `Uebergeben` mit
  `SageWeAm ≤ SyncedAt`; `Rest(mengeOffen, syncedAt)` (`:24`) und `RestAus` (`:27`, nie negativ). `PurchaseOrderReceiveRow.Rest`
  (`PurchaseOrderReceiveViewModel.cs:35`) ruft nur `RestAus(MengeOffen, ImWmsAbzuziehen)` auf; `ImWmsAbzuziehen` kommt aus
  `PurchaseOrdersController.cs:129`. Zweiter Nutzer ist die Lieferverzug-Mail (`LieferverzugMailService.cs:73`). Weitere Aufrufer gibt es nicht
  (Grep `Abzuziehen|RestAus|\.Rest\(` über `*.cs`: nur diese drei plus Tests). Die Angabe des Backlogs („Rest-Regel in
  `PurchaseOrderReceiveRow.Rest`, `PurchaseOrderReceiveViewModel.cs:20`“) ist damit *überholt*.
- **Lieferverzug-Mail** filtert nicht selbst auf eine Belegart (kein Treffer `EBZ|EBB|Belegkennzeichen` in `LieferverzugPlaner.cs`/
  `LieferverzugMailService.cs`); sie nimmt alle Cache-Zeilen und den Rest aus `ErfasstJeBelPos.Rest` (`LieferverzugMailService.cs:73`).
- **Barcode/CSV** sind nicht belegartabhängig (kein Treffer `EBB|EBZ` in `WareneingangCsv*`). Der Barcode ist nur bei Sage-WE erlaubt
  (`PurchaseOrdersController.Receive.cs:86-87`).
- **Sage-WE-Payload** ist nicht EBB-spezifisch: `SourceBelegHandle = BelId` (`SageWareneingangPayload.cs:95`), `PositionHandle = BelPosId` (`:87`).
  Das Sicherungsnetz aus E5 sucht `ReferenzBelID eq <BelId>` (`SdataWareneingangLookupReader.cs:48`). Ob Sage eine Übernahme **aus einem Abruf**
  so annimmt und ob der ELL-Beleg dann `ReferenzBelID = Abruf-BelId` trägt, ist **unbelegt** → E2.

### F1 Abgleich EBB + EBZ (Entscheidung 1)
- Zwei Abfragen über `QueryAllAsync`, je eine mit `where Belegkennzeichen eq 'EBB'` und `eq 'EBZ'`. Kein `or` (am Server nicht belegt), kein `ne`
  (liefert auf Textfeldern still 0 Zeilen, [[fallstricke]] Z. 1795-1799).
- `select` um `MengeBasis` erweitert (eine gemeinsame Konstante; der Record `SdataEinkaufPositionZeile` bekommt `decimal? MengeBasis`). E0 belegt,
  dass das Feld mit `select` lesbar ist.
- Belegart-Prüfung (`:112-113`) gegen die Menge `{EBB, EBZ}`; `Belegkennzeichen` der Cache-Zeile = gelesener Wert (getrimmt, groß), nicht mehr die
  Konstante (`:140`).
- Mengen je Belegart:
  - **EBB** unverändert (`PositionGGBestellt/Geliefert`).
  - **EBZ:** `MengeBestellt = MengeOffen = MengeBasis`, `MengeGeliefert = 0`. `PositionGG*` werden für EBZ nicht verwendet. `MengeBasis = null` zählt
    wie heute als `menge_fehlt` mit Warnung (`:127`, `:165-166`), die Zeile wird mit 0 geführt (= nie buchbar, weil Rest 0).
- Einheitensperre `EinheitAbweichend` (`:132-133`), Artikel-, Kreditor- und Platzzuordnung (`:117-123`) und Termin (`:125`) unverändert für beide.
- **Empty-Guard je Belegart** (statt nur über die Summe, `:82-87`): Liefert eine der beiden Abfragen 0 Zeilen, obwohl der Cache Zeilen dieser Belegart
  hat → Warnung „<Ressource> liefert 0 offene Positionen (<Where>), Cache hat n – Cache bleibt unverändert (Empty-Guard)“, der ganze Cache bleibt
  stehen. Grund: Ein still leerer Filter (Fallstrick `ne`) würde sonst die Hälfte des Caches löschen.
- Counts zusätzlich `positionen_ebb`, `positionen_ebz`; `positionen` bleibt die Summe.

### F2 Rest-Regel für Abrufe
- **Regel:** Rest EBZ = Abrufmenge (`MengeOffen`) − Σ **aller** Receipts derselben `BelPosId`, also auch `Uebergeben`. Grund: Sage verkleinert die offene
  Menge je Abruf nie; die Ausnahme „übergeben und im Abgleich-Stand enthalten“ (EBB, F5 Teil 2) würde bei EBZ Übergebenes wieder als offen zeigen.
- **Eine Stelle:** `ErfasstJeBelPos.Abzuziehen` bekommt die Cache-Zeile statt nur `SyncedAt`:
  `Abzuziehen(SagePurchaseOrderLine l) => l.IstRahmenabruf ? Gesamt : Gesamt − Σ Uebergeben mit Am ≤ l.SyncedAt`, ebenso
  `Rest(SagePurchaseOrderLine l) => RestAus(l.MengeOffen, Abzuziehen(l))`. Die alten Überladungen mit `DateTime` entfallen, damit kein Aufrufer die
  Belegart vergessen kann. Aufrufer: `PurchaseOrdersController.cs:129`, `LieferverzugMailService.cs:73`. `PurchaseOrderReceiveRow.Rest` bleibt
  unverändert.
- `SagePurchaseOrderLine` bekommt `public const string KennzeichenRahmenabruf = "EBZ";` und `public bool IstRahmenabruf => …` (nur Getter, von EF
  nicht gemappt; keine Migration).

### F3 Anzeige (Entscheidung 2)
- **Offene Bestellungen** (`PurchaseOrdersController.Index`, `Views/PurchaseOrders/Index.cshtml`):
  - Hinweis-Spalte (`Hinweis`, `:58-65`) beginnt bei EBZ mit „Rahmenabruf“ (Text, damit filterbar über `ColumnMap["hinweis"]`).
  - „geliefert (Sage)“ zeigt bei EBZ „–“ mit `title`/Tooltip „Sage liefert für Rahmenabrufe keine gelieferte Menge je Abruf“, nie „0“
    (View `:111`, `ColumnMap["geliefert"]` `:49` liefert ebenfalls „–“).
  - **Rest-0-Abrufe ausgeblendet:** EBZ-Zeilen mit `Rest(l) = 0` fallen standardmäßig weg. Filterkarte bekommt die Checkbox
    „auch im WMS vollständig erfasste Abrufe“ (`auchErfassteAbrufe`, neben „nur überfällig“ `:62-67`). Ausgeblendete werden gezählt und über der
    Tabelle gemeldet: „n Rahmenabruf(e) im WMS vollständig erfasst – ausgeblendet“ (melden statt still). Der Filter wirkt vor Spaltenfilter und
    Pagination (Reihenfolge in `Index` `:82-95`).
  - Stand-Text `:28` „nur normale Bestellungen (EBB)“ → „Bestellungen (EBB) und Rahmenabrufe (EBZ); bei Abrufen: offen = Abrufmenge, abzüglich
    im WMS erfasster Mengen“.
- **Wareneingang zu Bestellung** (`Receive`):
  - Kopf (`Receive.cshtml:45-56`): Text-Badge „Rahmenabruf“ und eine Zeile „offen = Abrufmenge; in Sage direkt gebuchte Abruf-Lieferungen zieht das WMS
    nicht ab“. Farbe nie allein (CLAUDE.md, WCAG AA).
  - Ein Abruf öffnet sich auch, wenn alle Zeilen Rest 0 haben (Überlieferung mit Bestätigung bleibt möglich, `PurchaseOrdersController.Receive.cs:69`).
  - Meldung „nur offene normale Bestellungen“ (`:22`) → „nur offene Bestellungen und Rahmenabrufe aus Sage, Stand des letzten Abgleichs“.
  - Haben ein Abruf und eine Bestellung dieselbe `BelegnummerFormatiert`, greift die vorhandene Meldung „gehört zu n Bestellungen“ (`:23`). Ob das
    vorkommt, zeigt E1 auf DEV (Abfrage im Test-Abschnitt).
- **Grenze in der Hilfe** (Entscheidung 2): `Views/Help/Index.cshtml` Abschnitt Wareneingang (`:84` „Rahmenabrufe werden (noch) nicht angezeigt“ ersetzen):
  „Rahmenabrufe: offen = Abrufmenge minus im WMS erfasste Menge. Eine Abruf-Lieferung, die in Sage direkt (ohne WMS) gebucht wurde, zieht das WMS
  nicht ab – der Abruf bleibt offen, bis der Rahmen in Sage endet. Im WMS vollständig erfasste Abrufe sind ausgeblendet (Filter).“

### F4 Sage-WE für Abrufe (Entscheidung 3)
- Neue Konstante in `IdealAkeWms/Services/SageWareneingang.cs`: `public const bool RahmenabrufMitSageWe` – Wert legt E2 fest (`true` nur nach
  erfolgreichem Schreibtest, sonst `false`). **Keine Einstellung** (Entscheidung, kein Umschalten im Betrieb vorgesehen; YAGNI).
- Eine Funktion `SageWareneingang.GiltFuer(bool schalterAn, SagePurchaseOrderLine l, bool rahmenabrufMitSageWe = RahmenabrufMitSageWe)`
  = `schalterAn && (!l.IstRahmenabruf || rahmenabrufMitSageWe)` (Parameter für die Tests beider Werte).
- `BuildReceiveAsync` setzt `vm.SageWeAktiv` damit (`PurchaseOrdersController.Receive.cs:163`; alle Zeilen einer Maske haben dieselbe `BelId`, also
  dieselbe Belegart). Damit folgen Platzregel (`:68`), Status/Vorgang beim Buchen (`:107`, `:134-136`) und Barcode-Sperre (`:86-87`) ohne weitere
  Änderung:
  - **Mit Sage-WE (Konstante true):** EBZ-Receipts bekommen `Offen` (bzw. `NurInSage` bei Charge/Nachweis) und einen `SageWeVorgang`; Platzregel gilt;
    der Dienst sendet unverändert (`SourceBelegHandle` = Abruf-BelId). E5-Wiederholung, Q-2, „Klären“, Kürzel gelten unverändert.
  - **Ohne (Konstante false):** EBZ-Receipts bekommen `NichtNoetig` → Verhalten wie Teil 1 (Lagerbuchungs-Queue bei Sage-Platz, Einkauf schließt in
    Sage von Hand). Keine Platzregel. Barcode-Feld gesperrt.
- Texte bei „Abruf ohne Sage-WE“ (Schalter an, Konstante false) unterscheiden sich vom ausgeschalteten Schalter: Barcode-Hinweis (`Receive.cshtml:86-88`)
  und Fehler (`Receive.cs:87`) „Für Rahmenabrufe gibt es keinen Sage-Wareneingang – für diese Buchung entsteht keine CSV-Datei.“ Dazu im Kopf
  „Rahmenabruf: Wareneingang geht als Lagerbuchung an Sage; der Einkauf schließt den Abruf in Sage von Hand.“

### F5 Lieferverzug-Mail (Entscheidung 4)
- Ohne eigenen Code: Abrufe stehen im Cache und laufen durch Planer und Rest-Regel (F2). Termin = `Liefertermin` der Abruf-Position, sonst
  `BelegLiefertermin` des Abruf-Belegs (`EinkaufBestellungenSyncService.cs:125`). Dass das bei EBZ der Termin **des Abrufs** ist, liest E0 mit
  (Felder vorhanden?); die fachliche Bestätigung an einem Abruf in Sage ist ein Schritt für den Menschen (TS-130.6), bis dahin **unverifiziert**.
- Einkauf-Sammelliste: „Geliefert“ bei EBZ „–“ statt „0“ (`LieferverzugMailHtml.cs:114` HTML, `:132-133` Text). Die Kreditor-Mail zeigt nur bestellt
  und offen (`:61-63`, `:80-81`) – unverändert.
- Rückfrage 1 (Konflikt mit der akzeptierten Grenze aus Entscheidung 2). Bis zur Antwort gilt Entscheidung 4 wörtlich: Abrufe in beiden Mails; der
  Testbetrieb (Default an) leitet Kreditor-Mails ohnehin an den Einkauf um.

### F6 Barcode und CSV (Entscheidung 5)
- Kein eigener Code: Barcode am Receipt, Ausgangszeile nach `Uebergeben`, Datei wie bei EBB. Bei „Abruf ohne Sage-WE“ gesperrt (F4), weil nie
  `Uebergeben` → nie eine CSV (gleiche Begründung wie Freigabe-Antwort 7 der CSV-Spec).

## Technischer Loesungsentwurf

Kein neuer Dienst, keine neue Tabelle, keine Einstellung, keine Migration. Muster: Listen-View-Pattern (ADR 0005) für die erweiterte Filterkarte,
Rest-Regel an einer Stelle, reine Entscheidungsfunktion im Web-Projekt (vom Dienst referenziert wie `SageSystemKuerzel`).

**Schritte** (je Schritt ein Commit im Bündel; `epic: false`, die Reihenfolge ist trotzdem verbindlich):

- **E0 P-W8 (lesend, Dev-Lauf, Blocker bei Abweichung).** SData nur `GET`, über den echten Lese-Client mit dem DEV-Ziel des AKE-DEV-Dienstes
  (`SdataLeseZielLeser`, `SageSdataReadClient.QueryAllAsync`, wie P-W9 (b) in Teil 2), gegen `sagetest01`/`ake_TEST2026`. Zugangsdaten nicht notieren.
  ```
  EinkaufPositionenInfo?format=json&orderBy=BelPosId&where=Belegkennzeichen eq 'EBZ'
    &select=BelId,BelPosId,VorPosId,BelegnummerFormatiert,Menge,MengeBasis,Mengeneinheit,MengeBasiseinheit,Lagermengeneinheit,
            PositionGGBestellt,PositionGGGeliefert,Liefertermin,BelegLiefertermin
  ```
  Auswertung (Ergebnis in diese Spec, Hauptcheckout, Abschnitt „P-W8 Ergebnis“):
  1. Anzahl Zeilen; Anzahl `MengeBasis = null`.
  2. Zeilen mit `Mengeneinheit = MengeBasiseinheit` (ohne Groß/Klein): wie viele davon `Menge ≠ MengeBasis`.
  3. Zeilen mit verschiedenen Einheiten: Anzahl, davon `Menge ≠ MengeBasis` (erwartet; nicht buchbar, `EinheitAbweichend`).
  4. Mehrere `BelPosId` je `VorPosId` (Bestätigung B-1): Anzahl `VorPosId` mit > 1 Abruf; Beispiel.
  5. Zeilen ohne `Liefertermin` und ohne `BelegLiefertermin`.
  **Blocker** (stoppen, festhalten, nichts bauen): HTTP-Fehler (z. B. 500 = Feld nicht wählbar), 0 Zeilen, `MengeBasis = null` bei einer Zeile, oder
  Punkt 2 > 0 (gleiche Einheit, aber andere Menge – widerspricht „MengeBasis = Abrufmenge in der Basiseinheit“). Punkte 3–5 sind Befunde, kein Blocker.
  Die Bestätigung in der Sage-Oberfläche („ein Abruf öffnen, `MengeBasis` = Abrufmenge“) bleibt Manual-UAT (TS-130.1), nicht Teil des Blockers.
- **E1 Lesen, Rest, Anzeige, Abruf ohne Sage-WE.** F1, F2, F3, F4 mit `RahmenabrufMitSageWe = false`, F5-Anzeige „–“, Texte/Kommentare (Liste
  `affected_code`). Ohne Sage-Wirkung mergebar: Abrufe gehen wie in Teil 1 (`NichtNoetig`). Tests:
  - `EinkaufBestellungenSyncTests`: zwei Abfragen (`:60` heute nur EBB), EBZ wird übernommen (`:55` erwartet heute „nicht übernommen“ – bewusst
    geändert), EBZ-Mengen aus `MengeBasis`, `MengeGeliefert = 0`, Empty-Guard je Belegart (beide Richtungen), Counts.
  - `ErfasstJeBelPos`: EBZ zieht `Uebergeben` ab, EBB nicht (`SageWareneingangGrundlageTests.cs:84-108` auf die neue Signatur).
  - Controller: Hinweis „Rahmenabruf“, „–“ bei geliefert, Rest-0-Abruf ausgeblendet + Zähler, mit Filter sichtbar, EBB mit Rest 0 weiter sichtbar.
  - Receive: EBZ mit Schalter an und Konstante false → `NichtNoetig`, keine Platzregel, Barcode abgewiesen mit Abruf-Text; `GiltFuer` mit beiden
    Konstantenwerten.
  - `LieferverzugMailTests`: EBZ fällig nach Rest-Regel F2, „–“ bei geliefert in der Einkauf-Liste.
- **E2 Schreibtest (der eine freigegebene Sage-Schreibvorgang).** Abhängigkeit: E1 auf AKE-DEV deployt, Rückfrage 2 und 3 beantwortet.
  1. Code: `RahmenabrufMitSageWe = true` (eigener Commit), Tests für den true-Pfad (EBZ → `Offen` + Vorgang + Platzregel). Auf AKE-DEV deployen.
  2. DEV-Zustand: `Sync:SageSystemKuerzel = DEV`, `SageLagerbuchungAktiv` aus, **`Sync:SageWareneingangMaxWiederholungen = 0`** (sonst könnte E5 nach
     einem Fehler selbst ein zweites Mal senden), `SageWareneingangBelegAktiv` erst unmittelbar vor dem Senden an. Abgleich einmal laufen lassen.
  3. Auswahl (lesend, DEV-Cache): ein EBZ-Abruf mit Rest > 0, `ArticleId` gesetzt, nicht `EinheitAbweichend`, ohne Charge/Nachweis, Ziel-Platz mit
     `SageWareneingang.IstSageWePlatz`. Festhalten: `BelId`, `BelPosId`, `VorPosId`, `MengeBasis`, vorher gelesenes `PositionGGGeliefert` der Vorgangsposition.
  4. Buchen über die DEV-Maske: **eine** Position, Menge 1 (bzw. kleinste sinnvolle Teilmenge), Lieferschein `WMS-TEST-EBZ-1`. Erwarteter Request wie
     im Teil-2-Schreibtest, mit `SourceBelegHandle = <Abruf-BelId>`, `PositionHandle = <Abruf-BelPosId>`, Memo `WE#DEV#<id>#`. Request (ohne Zugangsdaten)
     vor dem Senden festhalten (Rückfrage 2).
  5. Genau **ein** Sende-Lauf, danach `SageWareneingangBelegAktiv` sofort aus.
  6. Prüfen (nur lesend, SData `GET`): Antwort `ResultNumber 200`, `BelegnummerFormatiert`; `EinkaufBelegeInfo … where Belegkennzeichen eq 'ELL' and Memo
     like '%WE#DEV#<id>#%'` (kodiert wie im Dienst) → genau 1 Treffer, `ReferenzBelID` notieren; Vorgangsposition `PositionGGGeliefert` um die Menge
     gestiegen; WMS: Receipt `Uebergeben` mit Belegnummer, kein Eintrag in `SageBookingQueueItems`, nach erneutem Abgleich Rest = Abrufmenge − Menge.
  7. **Klappt** = Punkt 6 vollständig positiv (inkl. `ReferenzBelID = Abruf-BelId`, Rückfrage 3) → Konstante bleibt `true`.
     **Klappt nicht** (Sage-Fehler, kein Beleg, falscher Bezug) → kein zweiter Versuch; lesend prüfen, ob doch ein Beleg entstand; Ergebnis eintragen;
     Konstante zurück auf `false` (Commit). Ein Beleg mit falschem Bezug wird **nicht** vom Agenten korrigiert; Rückbau entscheidet Gerald.
  8. Ergebnis in Abschnitt „Schreibtest-Ergebnis Rahmenabruf“ dieser Spec (Hauptcheckout).
- **E3 Abschluss.** Beide `AppVersion.cs`, Anwender-Changelog, Hilfe (F3-Grenze, F4 je nach E2-Ergebnis, Lieferverzug `:125` „Rahmenabrufe nie“
  ersetzen), README `:793`, `nachlauf.psd1`, TS-130, Text TS-116 (`docs/TESTSZENARIEN.md:11376` „nur EBB“).

**Verworfen:** eine Einstellung „Rahmenabrufe mit Sage-WE“ (kein Bedarf zum Umschalten genannt; ADR-0020-Aufwand ohne Nutzen); eine Spalte
`Belegkennzeichen` am Receipt (Rest und Status entscheiden über die Cache-Zeile; die Liste „Sage-Wareneingänge“ braucht keine Kennzeichnung).

Oberfläche: Skill `frontend-design` explizit aufrufen; Bootstrap 5, vorhandene Badges/Filterkarte, Kontrast WCAG AA, Text statt Farbe.

## Migrations-/SQL-Auswirkungen

- **Keine Migration, kein SQL-Skript, kein FreshInstall-Nachtrag.** `Belegkennzeichen` (nvarchar(3)) besteht seit `SQL/112`; `IstRahmenabruf` und die
  Konstanten sind nicht gemappt. Nächste freie Nummer wäre **147** (`SQL/146_AddWareneingangBarcodeCsv.sql` ist die höchste; Glob `SQL/14*.sql`,
  `SQL/1[5-9]*.sql` geprüft). Braucht der Dev-Lauf wider Erwarten eine Schema-Änderung, nimmt er 147 und zieht Spec und Manifest nach (ADR 0004).
- `SQL/AgentJobs/*`: nicht betroffen (der Cache wird nur vom Dienst geschrieben).
- Daten: Der erste Abgleich nach dem Deploy füllt EBZ-Zeilen in `SagePurchaseOrderLines` (Vollersatz, `EinkaufBestellungenSyncService.cs:171-175`).

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Sync:EinkaufBestellungenEnabled` (ServiceSetting, vorhanden) – **nur Beschreibungstext geändert** | Bool | unverändert | false (unverändert) | Text `ServiceSettingDefinitions.cs:262`: „… normale Bestellungen (EBB) und Rahmenabrufe (EBZ) …“ | Meta `:112` unverändert | AKE (unverändert) | – |

Sonst keine neue oder geänderte Einstellung. `RahmenabrufMitSageWe` ist bewusst eine Code-Konstante (F4).

## Audit-Feld-Auswirkungen

- `SagePurchaseOrderLine` ist Cache ohne `AuditableEntity` (`SagePurchaseOrderLine.cs:6`), unverändert.
- EBZ-Wareneingänge entstehen im selben Insert wie heute (`Created*`, `PurchaseOrdersController.Receive.cs:137`); Statuswechsel durch Dienst und
  Hand-Aktionen setzen `Modified*` wie in Teil 2. Kein neuer Schreibweg.

## Rollen und Zugriff

Unverändert: Lesen `RequireStockReadAccess`, Buchen `RequireStockAccess`, Feature-Gate `RequireWareneingangBestellungAktiv` kumulativ
(`PurchaseOrdersController.cs:15-16`). Keine neue Rolle, kein neuer Filter.

## Akzeptanzkriterien

1. (E0) Abschnitt „P-W8 Ergebnis“ steht in der Spec mit den Zählungen 1–5; kein Blocker-Kriterium verletzt (sonst ist E1 nicht begonnen).
2. (E1) Der Abgleich ruft `EinkaufPositionenInfo` genau zweimal auf (`eq 'EBB'`, `eq 'EBZ'`), übernimmt beide Belegarten und keine andere; das Protokoll
   zeigt `positionen_ebb` und `positionen_ebz`.
3. (E1) Eine EBZ-Zeile hat `Belegkennzeichen = EBZ`, `MengeBestellt = MengeOffen = MengeBasis`, `MengeGeliefert = 0`; `PositionGG*` wirken nicht.
4. (E1) Liefert eine Abfrage 0 Zeilen, während der Cache Zeilen dieser Belegart hat, bleibt der ganze Cache unverändert und das Protokoll zeigt die Warnung.
5. (E1) Rest: Abruf 10, Receipts 3 (davon 1 `Uebergeben` mit `SageWeAm ≤ SyncedAt`) → Rest 7, auch nach erneutem Abgleich; dieselbe Konstellation bei
   EBB → Rest wie bisher (übergebene Menge nicht abgezogen). Maske und Lieferverzug-Mail liefern denselben Wert.
6. (E1) „Offene Bestellungen“: EBZ-Zeile zeigt „Rahmenabruf“ in der Hinweis-Spalte (Spaltenfilter „Rahmenabruf“ findet sie), „–“ bei geliefert.
7. (E1) Ein Abruf mit Rest 0 fehlt standardmäßig, die Meldung nennt die Zahl ausgeblendeter Abrufe; mit „auch im WMS vollständig erfasste Abrufe“ ist er
   da. Eine EBB-Zeile mit Rest 0 ist in beiden Fällen sichtbar.
8. (E1) Scan/Eingabe der Belegnummer eines Abrufs öffnet ihn; kein Treffer zeigt „… nur offene Bestellungen und Rahmenabrufe …“. Der Kopf zeigt das
   Text-Badge „Rahmenabruf“.
9. (E1, Konstante false) Abruf mit `SageWareneingangBelegAktiv = true`: Receipt `NichtNoetig`, kein `SageWeVorgang`, jeder aktive Platz wählbar, Barcode
   abgewiesen mit dem Abruf-Text; EBB im selben Zustand unverändert (Status `Offen`, Platzregel).
10. (E1) Abweichende Einheit bei einem Abruf ist nicht buchbar (Text in der Zeile, wie EBB).
11. (E1) Lieferverzug: ein überfälliger Abruf mit Rest > 0 steht in der Einkauf-Liste, „Geliefert“ = „–“; Rest 0 nie.
12. (E2) Abschnitt „Schreibtest-Ergebnis Rahmenabruf“ ist eingetragen: genau ein POST (Dienst-Log), Ergebnis, `ReferenzBelID`, Wert der Konstante danach.
    Kein weiterer `StandardBelegService`-POST im Dienst-Log.
13. (E2, nur wenn „klappt“) Konstante `true`: Abruf mit Schalter an → `Offen` + `SageWeVorgang`, Platzregel greift; Barcode erlaubt. Unit-Tests für beide
    Werte über den Parameter von `GiltFuer`.
14. (E3) Hilfe nennt die Grenze „in Sage direkt gebuchte Abruf-Lieferungen werden nicht abgezogen“ und den Rest-0-Filter; „Rahmenabrufe werden (noch)
    nicht angezeigt“ und „Rahmenabrufe nie“ sind ersetzt.
15. Build 0 Fehler, alle Tests grün; bewusst geänderte Bestandstests (`EinkaufBestellungenSyncTests` EBZ, `SageWareneingangGrundlageTests` Signatur)
    im QA-Nachweis begründet.

## Test-Szenarien

Neues Kapitel **TS-130 „Rahmenabrufe (EBZ) in Offene Bestellungen und Wareneingang“** in `docs/TESTSZENARIEN.md` (Nummer prüft der Dev-Lauf), danach
`secondbrain/tests/testszenarien-index.md`. Sage-Schreibregel: Nur E2 schreibt (ein POST, Dev-Lauf); alle anderen Szenarien schreiben nicht nach Sage,
ausgenommen TS-130.5, das nur Gerald bzw. der Einkauf am AKE-Test ausführt.

- **TS-130.1 Abrufe lesen und anzeigen.** Vorbedingung AKE-DEV, `Sync:EinkaufBestellungenEnabled` an, ein Abgleich. Erwartet: Protokoll `positionen_ebz` > 0;
  Spaltenfilter Hinweis „Rahmenabruf“ zeigt Abrufe; ein Abruf aus EK013128 (bzw. einem Rahmen mit mehreren Abrufen) mit Sage vergleichen: bestellt/offen =
  Abrufmenge (Basiseinheit), geliefert „–“, Termin = Abruf-Termin. Prüfabfrage DEV:
  `SELECT Belegkennzeichen, COUNT(*) FROM SagePurchaseOrderLines GROUP BY Belegkennzeichen;` und
  `SELECT Belegnummer FROM SagePurchaseOrderLines GROUP BY Belegnummer HAVING COUNT(DISTINCT BelId) > 1;` (gleiche Nummer EBB/EBZ? Treffer → Meldung
  „gehört zu n Bestellungen“ prüfen). Negativ: Abruf mit abweichender Einheit ist nicht buchbar.
- **TS-130.2 Abruf buchen ohne Sage-WE.** `SageWareneingangBelegAktiv` aus. Abruf 10: 3 buchen → Rest 7; erneuter Abgleich → weiter 7; Rest 7 buchen →
  Rest 0, Abruf in der Liste ausgeblendet, Meldung „1 Rahmenabruf … ausgeblendet“; Filter an → sichtbar. Negativ: 8 auf Rest 7 ohne Häkchen → abgewiesen.
  Queue: `SELECT * FROM SageBookingQueueItems WHERE StockMovementId = <id>` → Eintrag bei Sage-Platz (Verhalten Teil 1).
- **TS-130.3 Abruf bei eingeschaltetem Sage-WE, Konstante false** (nur wenn E2 nicht klappt): Receipt `NichtNoetig`, keine Platzregel, Barcode-Feld
  gesperrt mit Abruf-Text; EBB im selben Zustand: Platzregel und `Offen`.
- **TS-130.4 Abruf mit Sage-WE, Konstante true** (nur wenn E2 klappt; ohne Senden): Schalter an, Dienst gestoppt; Abruf auf Sage-Platz buchen →
  `Offen` + Vorgang; Nicht-Sage-Platz → Zeilenfehler; Barcode erlaubt. Danach Eintrag per SQL zurücksetzen, Schalter aus.
- **TS-130.5 Sage-WE für einen Abruf am AKE-Test (nur Gerald/Einkauf, schreibt nach Sage).** Teilmenge buchen → ELL-Beleg in Sage mit Lieferschein, Abruf
  in Sage beliefert, WMS-Rest korrekt nach Abgleich, CSV bei Barcode (TS-129).
- **TS-130.6 Lieferverzug mit Abruf.** Abruf mit Termin vor heute − `TageNachTermin`, Rest > 0, Testbetrieb an: Einkauf-Liste enthält ihn, „Geliefert“ „–“;
  Kreditor-Mail je nach Rückfrage 1. Fachlich: Termin in Sage am Abruf gegenprüfen. Negativ: voll erfasster Abruf (Rest 0) fehlt.
- **TS-130.7 Hilfe.** Hilfeseite Abschnitt Wareneingang und Lieferverzug: Grenze und Filter beschrieben.

## Etappen (nur bei epic: true)

Entfällt (`epic: false`). Die verbindliche Reihenfolge E0 → E1 → E2 → E3 steht im technischen Entwurf; E2 hängt an den Rückfragen 2 und 3 und an
AKE-DEV. Im /goal-Lauf: E0, E1, E3 laufen durch; E2 als Blocker festhalten, falls die Rückfragen offen sind (E1 + E3 sind ohne E2 mergebar,
Abrufe dann mit `NichtNoetig`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

*Vorläufig – der qa-agent bestätigt gegen den echten Diff.*

- **Web-App:** ja (Controller, Views, Repository-Regel, Hilfe, Changelog, Settings-Text).
- **Service:** ja (Abgleich `EinkaufBestellungenSyncService`, Lieferverzug-Mail; der Dienst referenziert das Web-Projekt für `ErfasstJeBelPos`/`SageWareneingang`).
  Web und Dienst gemeinsam deployen.
- **Migration:** nein.
- **Reihenfolge:** Backup (Standard), Update-Skript, Web, Dienst, einen Abgleich „Offene Bestellungen (Sage)“ laufen lassen, Einkauf informieren.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023), Block „Nachher“, ASCII:
  - `@{ Id='v<Version>-Rahmenabrufe'; Version='<Version>'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Einkauf und Lager informieren: Rahmenabrufe (EBZ) stehen jetzt in Offene Bestellungen und Wareneingang. Offen = Abrufmenge minus im WMS erfasst; in Sage direkt gebuchte Abruf-Lieferungen zieht das WMS nicht ab. Abrufe sind auch in den Lieferverzug-Mails. <je E2: Abrufe gehen als Sage-Wareneingang | Abrufe gehen als Lagerbuchung, Einkauf schliesst in Sage von Hand>.'; Quelle='<changelog-slug>' }`
  - Keine Schema-, Daten-, Einmal- oder Pruefung-Einträge (kein SQL, kein SQL-prüfbarer Zustand, keine neue Einstellung).
- **Publish-Befehle (im Worktree):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Standort:** AKE. IDEAL bekommt den Code ohne Wirkung (Abgleich dort aus).

Brain-Pflichten (Hauptcheckout): [[fallstricke]] „EBZ-Vorgangsmengen gelten je Rahmen, nicht je Abruf; Rest EBZ zieht alle WMS-Wareneingänge ab“ (mit
Warum); [[glossar]] „Rahmenabruf (EBZ)“; [[feature-map]], [[services]] (Abgleich EBB+EBZ, Counts), [[datenmodell]] (`Belegkennzeichen` jetzt EBB/EBZ),
Brain-Changelog, Testindex.

## Offene Rueckfragen

1. **Konflikt Entscheidung 2 ↔ 4 (Kreditor-Mail für Abrufe):** Ein Abruf, der in Sage direkt (ohne WMS) geliefert wurde, bleibt im WMS mit Rest > 0
   (akzeptierte Grenze). Nach dem Liefertermin wird er fällig, und der Lieferant bekäme eine Erinnerung für bereits Geliefertes. Sollen Abrufe auch in
   der **Kreditor-Mail** stehen (wie Entscheidung 4 wörtlich) oder nur in der **Einkauf-Sammelliste**?
   *Empfehlung:* nur Einkauf-Sammelliste (eine Bedingung im Planer), bis der Einkauf die Testmails gesichtet hat; danach neu entscheiden.
2. **Schreibtest-Ablauf:** Reicht die Freigabe vom 2026-11-12 (Entscheidung 3), oder zeigt der Dev-Lauf wie in Teil 2 den fertigen Request vor dem einen
   POST und wartet auf Geralds OK?
   *Empfehlung:* vorher zeigen. Dann läuft E2 interaktiv und nicht im /goal-Lauf; E0/E1/E3 bleiben davon unabhängig.
3. **Erfolgskriterium Schreibtest:** Gilt der Test nur als „klappt“, wenn der ELL-Beleg `ReferenzBelID = BelId des Abrufs` trägt? Sonst findet das
   Sicherungsnetz aus Teil 2 E5 (`SdataWareneingangLookupReader.cs:48`) einen Abruf-Wareneingang ohne Kennung nicht, und ein Wiederholen könnte einen
   zweiten Beleg anlegen.
   *Empfehlung:* ja; sonst bleibt EBZ bei `NichtNoetig`.

## Freigabe-Nachtrag 2026-11-13 08:39 (Gerald, geht dem Rumpf vor)
- Gemeinsamer /goal-Lauf mit [[2026-11-12-befunde-ake-regression-dev-spec]], [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]] und
  [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]] auf dem Bündel (v1.99.12). **Versions- und TS-Nummern vergibt der Lauf in
  Lauf-Reihenfolge neu** (die Nummern in den Specs sind Vorschläge; WE Teil 3 und Bugfix schlagen beide TS-130 vor). SQL ab 147.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Abrufe in der Kreditor-Mail oder nur in der Einkauf-Sammelliste? → **Beide – Abrufe in Einkauf-Sammelliste und Kreditor-Mail, keine Planer-Bedingung. Begründung: Keyuser Till (13.11.) will Rahmenabrufe erinnert haben; im Testbetrieb gehen Kreditor-Mails nur an den Einkauf, der so sieht, ob Abrufe falsch erinnert werden. Vor dem Echtbetrieb klärt die Koordination mit Till, ob Abruf-Lieferungen direkt in Sage gebucht werden; nur dann folgt die Bedingung „nur Sammelliste“ als Nachtrag (Gerald 2026-11-13).**
2. Schreibtest: Request vorher zeigen und auf OK warten, oder Freigabe vom 12.11. reicht? → **Request vorher zeigen und auf Geralds OK warten; E2 läuft interaktiv, nicht im /goal-Lauf – der Lauf setzt E0, E1, E3 um und hält vor E2 an (Gerald 2026-11-13).**
3. Schreibtest „klappt“ nur mit `ReferenzBelID` = Abruf-BelId? → **Ja; sonst bleibt EBZ bei NichtNoetig (Gerald 2026-11-13).**
