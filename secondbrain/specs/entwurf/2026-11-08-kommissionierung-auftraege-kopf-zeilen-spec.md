---
type: spec
title: Kommissionieraufträge (Kopf/Zeilen, KO-Nummernkreis, Typ, Kommissionierbereiche), 1–n je FA
slug: 2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec
status: Entwurf
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-11-07-kommissionierung-auftraege-kopf-zeilen]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
depends_on: "[[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]]"
task: ""
worktree: ""
branch: ""
standort: beide
version: "1.88.0"
sql_reserviert: "133–139"
testszenario: "TS-120"
affected_code:
  - IdealAkeWms/Models/PickingItem.cs
  - IdealAkeWms/Models/ProductionOrderPickingStatus.cs
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Data/Repositories/PickingRepository.cs
  - IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs
  - IdealAkeWms/Services/PickingTransferService.cs
  - IdealAkeWms/Services/IPickingTransferService.cs
  - IdealAkeWms/Services/BeschichtungsauftragWorkflow.cs
  - IdealAkeWms/Services/BomQuantityResolver.cs
  - IdealAkeWms/Services/KommissionierRelevanzFilter.cs
  - IdealAkeWms/Controllers/PickingController.cs
  - IdealAkeWms/Controllers/PickingLeitstandController.cs
  - IdealAkeWms/Views/Picking/Index.cshtml
  - IdealAkeWms/Views/Picking/Bom.cshtml
  - IdealAkeWms/Views/Picking/PrintPicking.cshtml
  - IdealAkeWms/Views/PickingLeitstand/Index.cshtml
  - IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml
  - IdealAkeWms/Models/AppSettingDefinitions.cs
  - SQL/133_AddPickingAreas.sql
  - SQL/134_AddPickingOrders.sql
  - SQL/00_FreshInstall.sql
  - SQL/Einmalig/<Datum>_Kommissionierauftraege_Altbestand.sql
open_questions:
  - "1. Rangfolge der Bereichsregel: Artikel-Ausnahme zuerst (Artikel > Kommissionierziel > Lagerort > Artikelgruppe > Standardbereich) statt zuletzt?"
  - "2. Welche Stücklistenpositionen kommen in einen Kommissionierauftrag: AKE alle Positionen wie heute (inkl. Baugruppen-Zeilen), IDEAL nur kommissionier-relevante (Kommissionieren nicht leer)?"
  - "3. Soll-Menge der Zeile: auf die Auftragsmenge hochgerechnete Menge (wie in der Stückliste angezeigt) statt der heute gebuchten Menge je Stück (Befund B1)? Prüfabfrage P3."
  - "4. Freigabe zurücknehmen: Aufträge ohne Fortschritt stornieren, Aufträge mit Fortschritt stehen lassen und warnen?"
  - "5. Abschluss „teilweise“: Folgeauftrag über die Restmengen automatisch anlegen (gleicher Bereich, Priorität, Kommissionierer)?"
  - "6. Priorität und Kommissionierer: am Auftrag gespeichert, Änderungen im Leitstand am FA schreiben auf alle offenen Aufträge des FA durch; keine Einzeländerung je Auftrag in diesem Epic?"
  - "7. Standort ohne Leitstand (LeitstandAktiv = false): Öffnen des FA über die Auswahlliste erzeugt die Aufträge (wirkt wie die Freigabe)? Prüfabfrage P4."
  - "8. Fälligkeit: Bereich mit Kennzeichen „Vorkommissionierung“ rechnet den Vorkommissioniertermin (PrePickingDaysResolver), alle anderen den Kommissioniertermin?"
  - "9. Bereichs-Stammdaten schlank: flache Liste (Unterteilung = weitere Bereiche), kein Standard-Kommissionierer/-Drucker je Bereich in diesem Epic?"
  - "10. Altbestand: nur FAs mit Fortschritt (IsPicked/IsTransferred) per Einmal-Skript überführen, offene Freigaben ohne Fortschritt per Leitstand-Knopf „Fehlende Kommissionieraufträge erzeugen“?"
  - "11. Tabelle PickingItems nach der Umstellung nur lesend behalten, Löschen als eigene spätere Aufgabe?"
  - "12. Sage-Etappe E7: nicht in diesem Epic bauen, sondern an das Rückmelde-Epic hängen (Konflikt mit Regel F9 „Umbuchung mit FA-Bezug nie nach Sage“)?"
  - "13. Kein globaler Ein/Aus-Schalter für das neue Verfahren – Umstellung mit dem Deploy (Einmal-Skript im selben Wartungsfenster)?"
epic: true
etappen:
  - "E1 Modell, Nummernkreis, Kommissionierbereiche (SQL/133, SQL/134)"
  - "E2 Erzeugen: Leitstand-Freigabe legt Aufträge je FA und Bereich an, Nachziehen-Knopf, Auftragsliste lesend"
  - "E3 Abarbeiten: Auftragsseite, Teilmengen, Umbuchung mit FA-Feld, Abschluss/Folgeauftrag, FA vollständig"
  - "E4 Leitstand, Druck mit QR, Freigabe zurücknehmen, Priorität/Kommissionierer durchschreiben"
  - "E5 Altbestand: Einmal-Skript mit Trockenlauf"
  - "E6 Abschluss: Hilfe, Changelog, TS-120, Version 1.88.0"
  - "E7 (nur nach Antwort 12) Sage-Buchung hinter Schalter, Default aus"
deploy:
  web: true
  service: false
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Kommissionieren ist heute ein Zustand der Stückliste: `PickingItem` hält je FA und Stücklistenzeile ein
`IsPicked`/`IsTransferred` (`Models/PickingItem.cs:6-41`), der FA-Status liegt 1:1 in `ProductionOrderPickingStatus`
(`Models/ProductionOrderPickingStatus.cs:5-48`). Es gibt keinen Beleg, keine Nummer, keine Teilmengen und keine Historie
„wer hat wann welche Menge in welchem Durchgang“. Geralds Auftrag: eigene **Kommissionieraufträge** (Kopf/Zeilen) mit
fortlaufender Nummer `KO-JJJJ-NNNNN`, Typ (heute FA, später Lager/Glas), **Kommissionierbereichen** und 1–n Aufträgen je
FA bis vollständig. Der Leitstand erzeugt die Aufträge bei der Freigabe; die Kommissionierer sehen Aufträge statt FAs.
Die Stückliste bleibt Datenquelle (Soll), wird aber nicht mehr selbst abgehakt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neue Tabellen `PickingAreas`, `PickingAreaRules`, `PickingOrders`, `PickingOrderLines` (E1).
- Nummernkreis `KO-JJJJ-NNNNN` mit dem vorhandenen Vergabemuster des Beschichtungsauftrags (E1).
- Erzeugung je FA und Bereich bei der Leitstand-Freigabe, Nachziehen fehlender Aufträge (E2).
- Abarbeiten auf Auftragsebene inkl. Teilmengen, Umbuchung mit FA-Feld aus Teil A, Abschluss vollständig/teilweise mit
  Folgeauftrag, FA-Abschluss daraus (E3).
- Leitstand-Anzeige, Druck mit QR, Freigabe-Rücknahme (E4).
- Überführung des `PickingItem`-Bestands (E5, Einmal-Skript nach ADR 0015, daten-umbauend).

**Out-of-Scope (vorgemerkt, nicht bauen)**
- **Sammelkommissionierung** mehrerer FAs in einem Auftrag. Das Modell lässt sie zu: jede Zeile trägt ihre Herkunft
  (`ProductionOrderId` + Stücklistenschlüssel) selbst, `PickingOrders.ProductionOrderId` ist nullable.
- **Typ Lager-/Glasbestellung** (Umstellung `WarehousePicking` auf Kommissionieraufträge) – eigenes Folge-Epic. Der Typ
  ist als Spalte angelegt, im Code gibt es nur `FA = 1`.
- Schreibende Sage-Aufrufe (E7 nur nach Antwort 12, hinter Schalter, Default aus).
- IDEAL-Kommissionierlisten `/FaHierarchyKommissionierListen` (lesende Lagerlisten, `KommissionierListenService`) bleiben
  unverändert.
- Löschen der Tabelle `PickingItems` (Rückfrage 11).

## Fachliche Anforderungen

### Ist-Stand (am Code belegt)

| Thema | Befund | Beleg |
|---|---|---|
| Anlage der Fortschrittszeilen | Beim **Öffnen** der Stückliste, einmal je FA: existiert irgendeine Zeile, wird nichts nachgelegt (spätere Stücklistenänderungen kommen nie an). | `PickingRepository.cs:25-49`, Aufruf `PickingController.cs:322-325` |
| Schlüssel | `(BomArticleNumber = Ressourcenummer, BomPosition)`; IDEAL-Position ist der erzeugte Pfad, Tiebreak bewusst stabil gehalten. | `PickingController.cs:330-333`, `FaHierarchyBomRepository.cs:77-79`, `:143-147` |
| Menge | `PickingItem.Quantity = bom.Menge` (roh). Die Stückliste **zeigt** dagegen `BomQuantityResolver.Resolve` (AKE: × `order.Quantity`). | `PickingRepository.cs:41`, `PickingController.cs:417`, `BomQuantityResolver.cs:7-16` |
| Teilmengen | Nicht möglich: Auswahl kennt nur Id/Lagerplatz/Baugruppe, gebucht wird immer `item.Quantity`. | `IPickingTransferService.cs:3-8`, `PickingTransferService.cs:293-307` |
| Lagerbewegung | `MovementType.Umbuchung` Quell-Lagerplatz → Ziel (Kommissionierwagen/Werkbank), FA als **Text** in `StockMovement.ProductionOrder`, bei Wagen-Konflikt mit `;` zusammengeführt. | `PickingTransferService.cs:69-79`, `:293-307`; `StockMovement.cs:20-22` |
| Sage | Kommissionier-Umbuchungen gehen **nie** nach Sage: am Decorator vorbei geschrieben, die Nacherfassung schließt „Umbuchung mit FA-Bezug“ aus. | `PickingTransferService.cs:293`, `SageBookingQueueRepository.cs:149-151`, [[fallstricke]] Z. 1719 |
| FA erledigt (Kommissionierung) | `IsDonePicking` hat vier Schreiber: `PickingController.ToggleDone` (:261-277), `SetPickingStatus("abgeschlossen")` (:547-567), `/api/picking-status/toggle` (`ProductionOrderPickingStatusRepository.cs:9`, `:56`), Dienst `FaZusatzinfoSyncService.cs:217`, `:225`. | wie genannt |
| Statustext | Nach der Umbuchung wählt der Kommissionierer „teilkommissioniert“/„abgeschlossen“ → `PickingStatus`-Text. | `Views/Picking/Bom.cshtml:503-506`, `:1575-1594` |
| Freigabe | `ToggleRelease` (:347-404) und `BulkRelease` (:409-455) in `PickingLeitstandController`; nur HauptFA (v1.46.0, :353-359); Kommissionierer-Pflicht bei `KommissionierungMitZuweisung` (:372-380). | wie genannt |
| Kommissionier-Liste | Liste freigegebener HauptFAs, Prio-sortiert, ohne Leitstand nur Auswahlliste (`IndexDropdown`). IDEAL gruppiert nach HauptFA. | `PickingController.cs:118-256`, `ProductionOrderPickingStatusRepository.cs:174-201` |
| Fotos | Je FA im Dateisystem (`Fotos/Kommissionierung/<OrderNumber>_*`). | `Api/PhotoController.cs:23-39` |
| Druck | `PrintPicking` druckt die gepickten Zeilen des FA, ohne Nummer/QR. | `PickingController.cs:672-710` |
| Nummernkreis | Nur der Beschichtungsauftrag hat einen: `MAX+1` je Jahr, UNIQUE-Index entscheidet, bis 3 Versuche, Serializable-Transaktion. Lagerbestellungen haben **keine** Nummer, nur `#Id`. | `BeschichtungsauftragWorkflow.cs:35-36`, `:48`, `:377-410`; `BeschichtungsauftragRepository.cs:27-30`, `:76-84`; `ApplicationDbContext.cs:1509-1511`; `Views/WarehousePicking/Details.cshtml:11` |
| AKE-Glas | `HasGlass` ist ein **manuelles** FA-Häkchen, keine Artikeleigenschaft – taugt nicht als Bereichsregel je Zeile. | `_PickingLeitstandRow.cshtml:118-119`, `ProductionOrderPickingStatusRepository.cs:56` |
| IDEAL-Ziel | `FaHierarchyNode.Kommissionieren` (Sage-View `vw_IDEAL-AKE_Kommissionierung_FAListe`). Die Kommissionierlisten filtern seit Nachtrag 2026-08-13 **nicht** mehr auf `SubFA = 0` – der Backlog-Hinweis „nur Blattebene“ ist überholt. | `FaHierarchyNode.cs:39`, `KommissionierListenService.cs:90-93`, `:109`, `KommissionierRelevanzFilter.cs:12` |

**Befund B1 (Zusatzfund, nicht hier beheben, aber für die Soll-Menge maßgeblich):** An AKE bucht die Kommissionierung je
Zeile die Stücklistenmenge **je Stück**, angezeigt wird die Menge × Auftragsmenge. Bei FAs mit `Quantity <> 1` weichen
Anzeige und Buchung ab (`PickingRepository.cs:41` + `PickingTransferService.cs:296` gegen `BomQuantityResolver.cs:16`).
Umfang am Bestand: Prüfabfrage P3. → Rückfrage 3.

**Befund B2 (Zusatzfund):** `PickingRepository.MarkAsTransferredAsync` (:102-115), `PickingTransferService.DoTransferAsync`
(:309-310) und das Zurücksetzen in `ApplyPickingSelectionsAsync` (:126-133) setzen keine `Modified*`-Felder. Der neue Code
setzt sie; der Altcode wird in E3 stillgelegt.

**Befund B3 (Zusatzfund):** FK `PickingItems → ProductionOrders` ist im Modell `Restrict` (`ApplicationDbContext.cs:826-829`),
in `SQL/00_FreshInstall.sql:841` `ON DELETE CASCADE`. Nicht in diesem Epic angleichen (Tabelle wird stillgelegt), als
Aufgabe melden.

### Soll

**Kommissionierbereiche** (Stammdaten `PickingAreas`)
- Felder: Name, Kürzel, Reihenfolge, Aktiv, Standard (genau ein aktiver Standardbereich), Kennzeichen „Vorkommissionierung“
  (nur falls Antwort 8 = ja). Seed: **Lager** (Standard, `L`), **Glas** (`G`), **Vorkommissionierung** (`VK`).
- Zuordnungsregeln `PickingAreaRules` (Bereich, Regeltyp, Wert); ein (Regeltyp, Wert) gehört genau einem Bereich.
  Regeltypen: `Artikel` (Artikelnummer), `Kommissionierziel` (IDEAL: Wert von `Kommissionieren`), `Lagerort`
  (`StorageAreas.Lagerkennung` des Hauptlagerplatzes des Artikels), `Artikelgruppe` (`Articles.ArticleGroup`).
- Auflösung je Zeile in fester Rangfolge (Vorschlag, Rückfrage 1): **Artikel → Kommissionierziel → Lagerort → Artikelgruppe
  → Standardbereich**. Trifft eine Regel auf einen inaktiven Bereich, gilt der Standardbereich; die Erzeugung meldet das
  (Melden statt still behandeln).
- IDEAL: die Bereiche bilden die heutigen `Kommissionieren`-Ziele ab (je Ziel eine Regel). Welche Ziele vorkommen: P1.
  AKE: Regeln über Artikelgruppe/Lagerort; welche vorkommen: P2.

**Kommissionierauftrag** (`PickingOrders`)
- Nummer `KO-JJJJ-NNNNN`, ein gemeinsamer Kreis für alle Typen (Typ als Feld, Backlog-Vorschlag), Vergabe wie BA.
- Typ `FA = 1` (2 Lagerbestellung, 3 Glasbestellung nur als Kommentar reserviert, wie BA-Status 20).
- Herkunft: `ProductionOrderId` (gesetzt, solange der Auftrag genau einen FA umfasst – immer in diesem Epic).
- Bereich, Status, Priorität, Kommissionierer (Id + Name wie `ProductionOrderPickingStatus`), Bemerkung, Folgeauftrag-Bezug
  `UrsprungsauftragId` (Muster BA, `ApplicationDbContext.cs:1516-1519`), Abschlusszeitpunkt/-person, `RowVersion`.
- Status (byte, Muster BA): `Offen = 10`, `InArbeit = 20` (erste Zeile gepickt), `TeilweiseAbgeschlossen = 30`,
  `Abgeschlossen = 40`, `Storniert = 90`. Kein „Entwurf“: der Leitstand legt bei der Freigabe an, ein Entwurf hätte keinen
  Bearbeiter.
- Fälligkeit wird **nicht** gespeichert, sondern wie heute aus dem FA gerechnet (`PickingController.cs:180-184`; Rückfrage 8
  für Vorkommissionierung).

**Zeile** (`PickingOrderLines`)
- Herkunft je Zeile: `ProductionOrderId`, `ArticleNumber` (= Ressourcenummer, [[fallstricke]] „Artikelnummer vs
  Ressourcenummer“), `BomPosition` – derselbe Schlüssel wie `PickingItem` (Fortsetzbarkeit, stabiler IDEAL-Pfad).
- `IsBaugruppe`, `SollMenge`, `IstMenge`, Von-Lagerplatz, Status, Pick-Zeitpunkt/-Person, Umbuchungszeitpunkt,
  `StockMovementId` (die erzeugte Lagerbewegung), `RowVersion`.
- Zeilenstatus: `Offen = 0`, `Gepickt = 10`, `Umgebucht = 20`, `Entfallen = 30` (Auftrag „vollständig“ abgeschlossen, Zeile
  nicht gebraucht – heutige Entscheidung des Kommissionierers, z. B. Baugruppe statt Einzelteile, [[fallstricke]]
  „Baugruppen-Picking bucht NUR die Baugruppe“), `Uebertragen = 40` (Rest in den Folgeauftrag übergeben).
- Der Bereich der Zeile ist der Bereich des Kopfs (ein Auftrag je FA und Bereich). Eine eigene Spalte je Zeile kommt erst
  mit Sammelkommissionierung/Umbereichen.

**Erzeugung (Leitstand)**
- Bei Freigabe (`ToggleRelease`, `BulkRelease`) ruft der Leitstand nach dem Setzen der Freigabe **eine** Workflow-Funktion
  „offene Restmengen erzeugen“ je FA auf:
  1. Stückliste wie die Kommissionierseite laden (`IBomRepository.GetBomItemsAsync(BomKey.ForOrder(order), BomScopes.ForOrder(order))`).
  2. Relevante Positionen wählen (Rückfrage 2: AKE alle, IDEAL `KommissionierRelevanzFilter.IsRelevant`).
  3. Soll je Position (Rückfrage 3: `BomQuantityResolver.Resolve`).
  4. Abgedeckt je Schlüssel = Summe über alle nicht stornierten Aufträge des FA: `Umgebucht` → `IstMenge`;
     `Offen`/`Gepickt`/`Entfallen` → `SollMenge`; `Uebertragen` → 0 (steht im Folgeauftrag).
  5. Rest > 0 → Zeile; Bereich je Zeile auflösen; **je Bereich ein Auftrag** mit Nummer, Priorität und Kommissionierer aus
     `ProductionOrderPickingStatus`.
- Idempotent: zweimal freigeben oder Nachziehen erzeugt nichts doppelt. Neue Stücklistenpositionen nach der Freigabe kommen
  über das Nachziehen an (behebt die Einmal-Initialisierung `PickingRepository.cs:28-31`).
- Leere oder fehlende Stückliste: Freigabe gilt trotzdem, Sammelmeldung „FA … ohne Stückliste – kein Kommissionierauftrag“.
- Knopf im Leitstand **„Fehlende Kommissionieraufträge erzeugen“** (eine Zeile oder Auswahl, Leitstand-Recht): ruft dieselbe
  Funktion für freigegebene, offene HauptFAs; Ergebnis als Sammelmeldung (n Aufträge, m FAs ohne Stückliste, k ohne Rest).
- Ohne Leitstand (Rückfrage 7): das Öffnen über `IndexDropdown` ruft dieselbe Funktion.

**Abarbeiten (Kommissionierer)**
- `/Picking` zeigt **Kommissionieraufträge** (Nummer, FA, Werkbank, Bereich, Priorität, Fälligkeit, Status, Kommissionierer)
  statt FAs; Filter Bereich, Status, „Nur meine“ (`KommissionierungMitZuweisung`). Standard: Status Offen/InArbeit, FA nicht
  erledigt (`IsDone || IsDonePicking`, [[fallstricke]] „IsDone vs IsDonePicking“) und nicht storniert. Die IDEAL-Gruppierung
  nach HauptFA entfällt (Aufträge gibt es nur am HauptFA). **Werkbank-Spalte übernimmt Z3 des OSEON-Hotfix** (siehe Deploy).
- Auftragsseite `/Picking/Auftrag/{id}`: die bestehende Stücklistenseite (`Bom.cshtml`) im Auftragsmodus – nur die Zeilen
  dieses Auftrags, Kopf mit Nummer/Bereich/Status. Scan, Spaltenfilter, Lagerplatz-Vorschlag (`PickingController.cs:374-406`)
  und Wagen-Konfliktprüfung bleiben.
- Je Zeile Ist-Menge eingebbar (Default = Soll), > 0 und ≤ Soll (Server prüft). Mehrmenge ist kein Fall dieses Epics.
- Umbuchen: wie heute `Umbuchung` Quelle → Ziel mit Bestandsprüfung/NAN-Fallback (`PickingTransferService.cs:189-288`),
  gebucht wird `IstMenge`; die Zeile bekommt `StockMovementId`; die Bewegung bekommt **das FA-Feld aus Teil A** und weiter
  den Text in `ProductionOrder` (damit die Sage-Ausschlussregel `SageBookingQueueRepository.cs:149-151` greift).
- Abschluss nach der Umbuchung (ersetzt die Knöpfe `Bom.cshtml:503-506`):
  - **„Vollständig“** → `Abgeschlossen`, offene Zeilen `Entfallen`.
  - **„Teilweise“** → `TeilweiseAbgeschlossen`, offene Reste `Uebertragen`, Folgeauftrag über die Restmengen (Rückfrage 5).
- **FA vollständig kommissioniert**, wenn alle nicht stornierten Aufträge des FA `Abgeschlossen` sind und kein Auftrag offen
  ist → `SetIsDonePickingAsync(true)` und Statustext „abgeschlossen“; sonst Statustext „teilkommissioniert“ (dieselben
  Repository-Methoden wie `PickingController.cs:553-564`). `IsDonePicking` bleibt **gespeichert** (vier weitere Schreiber,
  viele Leser im Dienst) – es wird gesetzt, nicht berechnet.
- `/Picking/Bom/{faId}` wird lesend (Muster `ReadOnlyBomBuilder`) und listet die Aufträge des FA mit Status als Links.
- Fotos bleiben am FA (Dateiname `OrderNumber`), die Auftragsseite zeigt sie wie heute.

**Leitstand / Druck (E4)**
- Leitstand-Zeile zeigt die Aufträge des FA (Nummer, Bereich, Status) als Links.
- Freigabe zurücknehmen (Rückfrage 4): Aufträge ohne gepickte/umgebuchte Zeile → `Storniert`; Aufträge mit Fortschritt
  bleiben, Warnung nennt sie.
- Priorität/Kommissionierer (Rückfrage 6): `SetPriority`/`ChangeAssignedPicker` (`PickingLeitstandController.cs:460-490`)
  schreiben zusätzlich auf alle Aufträge des FA mit Status Offen/InArbeit.
- Druck je Auftrag (ersetzt `PrintPicking`): Kopf mit Nummer, FA, Bereich, Fälligkeit, **QR mit der Auftragsnummer**
  (`IBarcodeService`, wie die Kommissionierlisten), Zeilen mit Soll/Ist/Lagerplatz. Scan der Nummer im Suchfeld der
  Auftragsliste findet den Auftrag (Spaltenfilter Nummer).

## Technischer Loesungsentwurf

- **Muster:** Repository-Interface `IPickingOrderRepository` ohne Cache-Decorator (wie `BeschichtungsauftragRepository.cs:11-13`:
  Vergabe braucht den aktuellen Stand); einziger Schreibweg `Services/PickingOrderWorkflow.cs` (Muster
  `BeschichtungsauftragWorkflow`), reine, DB-freie Teile unit-testbar:
  - `KommissionierbereichRegel.Resolve(...)` (Rangfolge, inaktiv → Standard + Meldung),
  - `PickingOrderRestmengen.Berechne(sollJeSchluessel, vorhandeneZeilen)` (Schritt 4 oben),
  - Status-Übergänge als erlaubte Paare (Muster `BeschichtungsauftragWorkflow.cs:52-54`).
- **Nummernkreis wiederverwenden:** Die Vergabe `MitNummernVersuchenAsync`/`NummerVergebenUndSpeichernAsync`/`IstDeadlock`
  (`BeschichtungsauftragWorkflow.cs:377-410`) wird in einen kleinen gemeinsamen Helfer gezogen (Präfix, `GetMax`, Speichern
  als Parameter; Serializable-Rahmen `BeschichtungsauftragRepository.cs:76-84` als Vorlage) und vom BA **und** vom KO
  benutzt. Die BA-Tests sind der Wächter, dass die Extraktion verhaltensgleich ist. Fallback, falls die Extraktion BA-Risiko
  zeigt: Muster kopieren, `ponytail:`-Kommentar mit Verweis.
- **Freigabe-Hooks:** `PickingLeitstandController.ToggleRelease` nach `SetReleaseAsync` (:390-400), `BulkRelease` nach
  `SetReleaseBatchAsync` (:437-438, nur für `Processed`-FAs), `PickingController.Index` im Pfad `IndexDropdown` (:124-125, Rückfrage 7).
  `BulkRelease` lädt je FA die Stückliste – bei großen Auswahlen spürbar; Sammelmeldung statt Abbruch je FA.
- **Umbuchung:** `PickingTransferService` arbeitet auf `PickingOrderLines` statt `PickingItems`; Wagen-Konflikt
  (:45-79) unverändert; Schreiben weiter **direkt** über den Context (nicht über `IStockMovementRepository.AddAsync`), damit
  sich am Sage-Verhalten nichts ändert. Transaktion wie heute (:250-334).
- **Altpfade stilllegen (E3):** `TogglePicked`, `TransferPicked` (FA-basiert), `PrintPicking`, `InitializePickingAsync` werden
  durch die Auftragsvarianten ersetzt; `IPickingRepository` bleibt nur lesend für das Altbestand-Skript-Umfeld bzw. entfällt,
  wenn ungenutzt (Rückfrage 11). Tests `PickingTransferServiceTests`, `PickingTransferServiceAggregationTests`,
  `PickingControllerTests` ziehen mit.
- **Listen-View-Pattern (ADR 0005)** für `/Picking` (Auftragsliste): `PageSize.Resolve` + `PaginationState` + `_Pagination`,
  Filterkarte (Bereich, Status, Nur meine), Server-Spaltenfilter (`data-server-column-filter="true"`, alle `<th>` mit
  `data-col-key`, `ColumnFilterHelper.ReadFromQuery`), Fälligkeit **nach** der Terminberechnung filtern, Register
  `ColumnFilterTypes.For("Picking/Index")` anpassen. Referenz `ProductionOrdersController.Index`.
  Bereichs-Stammdaten: kleine, unpaginierte Liste → Client-Mode zulässig; Regelliste (Artikel-Ausnahmen können viele werden)
  → Server-Mode mit Paging.
- **Spaltenauswahl/Zahnrad:** neue ViewKeys in `ColumnDefinitions` + `#column-config` (Vier-Stellen-Konsistenz,
  `KommissionierSpaltenKonsistenzTests` als Vorbild).
- **Frontend:** Skill `frontend-design` Pflicht (Views/JS ändern sich); Statusanzeige nie nur über Farbe (Text + Badge).
- Brain-Karte nach Abschluss: [[datenmodell]], [[controller]], [[module]], [[feature-map]], [[glossar]] (Begriffe
  Kommissionierauftrag, Kommissionierbereich), neuer Fallstrick „Kommissionier-Umbuchung bleibt am Sage-Decorator vorbei“
  bleibt gültig; ADR „Kommissionierauftrag als Beleg neben der Stückliste“ (Satelliten-Gedanke ADR 0009 fortgeschrieben).

## Migrations-/SQL-Auswirkungen

Reservierter Block **133–139** (Bündel-Übersicht; der Dev-Lauf prüft die nächste freie Nummer erneut). Ablauf je
Schema-Änderung nach ADR 0004: Model → `dotnet ef migrations add` → idempotentes `SQL/XX` mit `OBJECT_ID`-Guard (DDL je
Batch) → `__EFMigrationsHistory`-Insert im eigenen Batch → `SQL/00_FreshInstall.sql` **an zwei Stellen** (Schema + `MigrationId`).

| Nr | Inhalt | Etappe | Art |
|---|---|---|---|
| 133 | `AddPickingAreas`: `PickingAreas` (UNIQUE Name, UNIQUE Kürzel, gefilterter UNIQUE-Index `IsDefault = 1`), `PickingAreaRules` (FK Cascade, UNIQUE `RuleType`+`Value`), Seed Lager/Glas/Vorkommissionierung (`IF NOT EXISTS`) | E1 | additiv |
| 134 | `AddPickingOrders`: `PickingOrders` (UNIQUE `Nummer`, UNIQUE `Jahr`+`LaufendeNummer`, Index `ProductionOrderId`, `Status`, `PickingAreaId`; FK FA Restrict, Bereich Restrict, Kommissionierer SetNull, Ursprung Restrict, `RowVersion`), `PickingOrderLines` (FK Kopf Cascade, FA Restrict, Lagerplatz SetNull, `StockMovementId` Restrict; Index `ProductionOrderId`+`ArticleNumber`+`BomPosition` **nicht** unique) | E1 | additiv |
| 135 | nur falls E3/E4 doch Schema brauchen (z. B. Kennzeichen Vorkommissionierung nach Antwort 8, sonst schon in 133) | E3/E4 | additiv |
| 136–139 | frei; nicht gebrauchte Nummern gibt der Lauf in der Bündel-Übersicht zurück | – | – |

- **Altbestand (E5): `SQL/Einmalig/<Datum>_Kommissionierauftraege_Altbestand.sql`** nach ADR 0015 (keine Nummer, kein
  History-/FreshInstall-Eintrag), **daten-umbauend → DB-Backup vor Deploy**. Pflichtinhalt:
  - Warnkopf „NICHT ERNEUT AUSFUEHREN“ + Abbruch, wenn schon Aufträge mit `CreatedBy = 'Migration-KO-Altbestand'` existieren.
  - `DECLARE @Trockenlauf bit = 1` – im Trockenlauf nur Zählungen: FAs mit Fortschritt, davon erledigt/offen, Zeilen je
    Zielstatus, FAs mit `Quantity <> 1` (B1), freigegebene offene HauptFAs **ohne** Fortschritt (→ Nachziehen-Knopf).
  - Ausführung (Rückfrage 10): je FA mit mindestens einer Zeile `IsPicked = 1 OR IsTransferred = 1` **ein** Auftrag im
    Standardbereich, Typ FA, Nummer `KO-<Jahr>-NNNNN` fortlaufend ab `MAX+1` des Jahres (`ROW_NUMBER()` nach FA-Freigabe),
    Bemerkung „Altbestand vor v1.88.0“. Zeilen nur aus berührten `PickingItems`: `IsTransferred` → `Umgebucht`
    (`IstMenge = Quantity`, `StockMovementId` NULL – nicht sicher zuordenbar), `IsPicked` ohne Umbuchung → `Gepickt`;
    `SollMenge = Quantity`. Kopfstatus: alle umgebucht oder FA erledigt → `Abgeschlossen`, sonst `InArbeit`. Priorität/
    Kommissionierer aus `ProductionOrderPickingStatus`. Audit: `CreatedAt = GETDATE()`, `CreatedBy = 'Migration-KO-Altbestand'`,
    `CreatedByWindows = SYSTEM_USER`. `PickingItems` bleiben unverändert.
  - `@@ROWCOUNT` je Schritt; Ergebnis im Deploy-Protokoll der Spec je System (System, Datum, Zählung).
- **`SQL/AgentJobs/`**: aktiv liegt nur `02_Import_Artikel.sql`; es berührt weder `PickingItems` noch
  `ProductionOrderPickingStatus` (Treffer nur in `_archiv/01_Import_Produktionsauftraege.sql`) → **keine Änderung**. Der
  Dienst-MERGE auf `ProductionOrderPickingStatus` (`SageImportService.cs:173-174`) bleibt unberührt, da diese Tabelle keine
  Spalte bekommt.
- InMemory erzwingt die UNIQUE-Indizes nicht → App-Guard + Wiederholversuch wie BA; Nummernkollision zusätzlich in
  Manual-UAT (zwei parallele Freigaben).

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `KommissionierungMitZuweisung` (bestehend, nur Verwendungstext in `AppSettingDefinitions` nachziehen) | Bool | an/aus | unverändert | Freigabe verlangt einen Kommissionierer; „Nur meine“ filtert jetzt **Aufträge** nach Kommissionierer | Leitstand-Freigabe, Kommissionier-Auftragsliste | beide | – |
| `LeitstandAktiv` (bestehend, Verwendungstext) | Bool | an/aus | unverändert | aus: Auswahlliste; Öffnen erzeugt die Aufträge (Rückfrage 7) | Kommissionierung | beide | – |
| `KommissionierTage` (bestehend, Verwendungstext) | Int | wie heute | 4 | zusätzlich Fälligkeit der Aufträge | Auftragsliste, Druck | beide | – |
| `KommissionierungSageBuchungAktiv` (neu, **nur E7**, ServiceSetting in `ServiceSettingDefinitions.All`) | Bool | Auswahl an/aus | **aus** | an: Kommissionier-Umbuchungen gehen den Weg der Sage-Lagerbuchungen (Inhalt hängt an Antwort 12) | Kommissionierung, Dienst `SageBookingWorker` | beide | „Sendet Kommissionier-Buchungen an Sage. Nur einschalten, wenn Lagerplätze in Sage freigegeben und die Rückmeldung abgestimmt ist.“ |

**Kuratierte Maske „Kommissionierbereiche“** (Stammdaten, ADR 0020): Name (Pflicht, eindeutig, max. 100), Kürzel (Pflicht,
eindeutig, max. 10), Reihenfolge (Zahl ≥ 0), Aktiv, Standard (genau ein aktiver Bereich; den letzten Standard nicht
abwählbar/deaktivierbar), ggf. Vorkommissionierung (Antwort 8). Regeln: **Regeltyp = Auswahlliste** (4 feste Werte),
**Wert = Auswahl aus Stammdaten** je Typ: Artikel aus dem Artikelstamm (Suche), Kommissionierziel aus den vorkommenden
`FaHierarchyNodes.Kommissionieren`-Werten (Liste wie `KommissionierListenService.cs:254-255`), Lagerort aus `StorageAreas`,
Artikelgruppe aus den vorkommenden `Articles.ArticleGroup`. Der Server prüft beim Speichern Typ, Wert-Existenz und
Eindeutigkeit (Typ, Wert). Je Feld Anzeigename und Wirkungstext in der Maske. Kein Außenwirkung → kein Warnhinweis.
Kein globaler Ein/Aus-Schalter für das Verfahren (Rückfrage 13).

## Audit-Feld-Auswirkungen

- `PickingAreas`, `PickingAreaRules`, `PickingOrders`, `PickingOrderLines` erben `AuditableEntity` (ADR 0003); jede
  Änderung setzt `ModifiedAt/By/ByWindows` aus `ICurrentUserService` – auch Statuswechsel, Durchschreiben von Priorität/
  Kommissionierer, Stornieren bei Freigabe-Rücknahme und Zeilen-Pick/Umbuchung (anders als der Altcode, Befund B2).
- Zeilen tragen zusätzlich die fachlichen Felder Pick-Person/-Zeitpunkt und Umbuchungszeitpunkt (Historie „wer, wann, welche
  Menge“); Kopf trägt Abschluss-Person/-Zeitpunkt.
- `ProductionOrderPickingStatus`-Schreibwege laufen über die bestehenden Repository-Methoden (setzen Audit bereits).
- Einmal-Skript: `CreatedBy = 'Migration-KO-Altbestand'`, `CreatedByWindows = SYSTEM_USER`.

## Akzeptanzkriterien

1. Die vier Tabellen existieren nach `SQL/133`+`134` auf einer bestehenden DB und nach `00_FreshInstall.sql` auf einer leeren;
   ein zweiter Lauf beider Skripte ändert nichts; `__EFMigrationsHistory` enthält beide Migrationen.
2. Seed: Lager (Standard), Glas, Vorkommissionierung sind angelegt; ein zweiter aktiver Standardbereich lässt sich nicht
   speichern (Server-Meldung).
3. Eine Regel (Typ, Wert) lässt sich keinem zweiten Bereich zuordnen; ein Wert, der im jeweiligen Stamm nicht existiert, wird
   abgelehnt.
4. Bereichsauflösung (Unit-Test): Artikel-Regel schlägt Kommissionierziel, das schlägt Lagerort, das schlägt Artikelgruppe;
   ohne Treffer Standardbereich; Regel auf inaktivem Bereich → Standardbereich + Meldung (Reihenfolge laut Antwort 1).
5. Nummern: zwei Aufträge im selben Jahr bekommen `KO-JJJJ-00001`, `-00002`; im neuen Jahr beginnt die Zählung bei 00001;
   zwei parallele Freigaben erzeugen keine doppelte Nummer (SQL Server, Manual-UAT). Die BA-Tests bleiben grün.
6. Freigabe eines HauptFA mit Positionen aus zwei Bereichen erzeugt genau zwei Aufträge (Typ FA, Status Offen, Priorität und
   Kommissionierer wie am FA); erneute Freigabe bzw. „Fehlende Kommissionieraufträge erzeugen“ erzeugt keinen weiteren.
7. Sub-FA-Freigabe bleibt gesperrt (v1.46.0) und erzeugt keinen Auftrag.
8. FA ohne Stückliste: Freigabe gelingt, Warnung nennt den FA, kein Auftrag.
9. Neue Stücklistenposition nach der Freigabe erscheint nach „Fehlende Kommissionieraufträge erzeugen“ als neuer Auftrag im
   passenden Bereich; bereits erfasste Positionen werden nicht doppelt angelegt.
10. `/Picking` listet Aufträge (Nummer, FA, Werkbank, Bereich, Priorität, Fälligkeit, Status, Kommissionierer) mit Paging,
    Filterkarte und Server-Spaltenfilter; „Nur meine“ zeigt nur Aufträge des angemeldeten Kommissionierers; Aufträge
    erledigter oder stornierter FAs erscheinen standardmäßig nicht.
11. Auftragsseite zeigt nur die Zeilen des Auftrags; Ist-Menge 0, negativ oder > Soll wird serverseitig abgelehnt.
12. Umbuchen bucht je Zeile `IstMenge` als `Umbuchung` vom Quell- auf den Zielplatz, setzt `StockMovementId`, das FA-Feld aus
    Teil A und den Text `ProductionOrder`; die Bewegung landet **nicht** in `SageBookingQueueItems`.
13. Abschluss „Vollständig“ setzt offene Zeilen auf `Entfallen`; sind danach alle Aufträge des FA abgeschlossen, ist
    `IsDonePicking = true` und der Statustext „abgeschlossen“; der FA verschwindet aus den offenen Listen.
14. Abschluss „Teilweise“ mit einer Zeile Soll 10 / Ist 4 erzeugt einen Folgeauftrag (neue Nummer, Bezug auf den Ursprung)
    mit Soll 6; `IsDonePicking` bleibt false, Statustext „teilkommissioniert“.
15. `/Picking/Bom/{faId}` ist nur lesend und verlinkt die Aufträge des FA.
16. Freigabe zurücknehmen storniert Aufträge ohne Fortschritt; Aufträge mit gepickter/umgebuchter Zeile bleiben und werden in
    der Warnung genannt (laut Antwort 4).
17. Prioritäts-/Kommissionierer-Änderung am FA im Leitstand ändert alle offenen/in Arbeit befindlichen Aufträge des FA
    (laut Antwort 6).
18. Druck eines Auftrags zeigt Nummer, FA, Bereich, Fälligkeit und einen scanbaren QR mit der Nummer; der Scan in der
    Auftragsliste findet den Auftrag.
19. Einmal-Skript: Trockenlauf ändert nichts und liefert die Zählungen; Ausführung legt je FA mit Fortschritt genau einen
    Altbestand-Auftrag an; ein zweiter Lauf bricht mit Meldung ab; `PickingItems` unverändert.
20. Rollen: Kommissionierer (`picking`) arbeiten Aufträge ab, können aber weder freigeben noch Bereiche pflegen; Leitstand
    (`leitstand`) erzeugt/storniert; Bereichspflege nur `masterdata`/`admin`, Ansicht auch `masterdata_read`.
21. Version 1.88.0 in beiden `AppVersion.cs`, Anwender-Changelog, Hilfeseite (Abschnitt Kommissionieraufträge und Bereiche),
    `docs/TESTSZENARIEN.md` TS-120.

## Test-Szenarien

Neues Kapitel **TS-120 „Kommissionieraufträge“** in `docs/TESTSZENARIEN.md`, danach `secondbrain/tests/testszenarien-index.md`.
Je Standort (AKE flach, IDEAL hierarchisch) durchspielen.

- **120.1 Stammdaten:** Bereiche anlegen/ändern, zweiter Standard (negativ), Regel doppelt (negativ), Wert nicht im Stamm
  (negativ), Standardbereich deaktivieren (negativ).
- **120.2 Erzeugen:** HauptFA mit Glas- und Lagerpositionen freigeben → zwei Aufträge; erneut freigeben → keine neuen;
  Bulk-Freigabe mit einem FA ohne Stückliste → Sammelmeldung; Sub-FA (IDEAL) → gesperrt; ohne Leitstand über die Auswahlliste.
- **120.3 Abarbeiten vollständig:** Zeilen picken, Ist = Soll, auf Kommissionierwagen umbuchen, „Vollständig“ → Bewegung mit
  FA-Feld, kein Sage-Queue-Eintrag (`SELECT * FROM SageBookingQueueItems WHERE StockMovementId = …` leer), FA erledigt.
- **120.4 Teilmenge/Folgeauftrag:** Ist < Soll, „Teilweise“ → Folgeauftrag mit Rest; diesen abschließen → FA erledigt.
- **120.5 Bereichsübergreifend:** Lager-Auftrag fertig, Glas-Auftrag offen → FA **nicht** erledigt.
- **120.6 Stücklistenänderung:** Position nach Freigabe ergänzen (Testsystem), Nachziehen → neuer Auftrag nur mit dieser Position.
- **120.7 Leitstand:** Priorität/Kommissionierer ändern → Aufträge folgen; Freigabe zurücknehmen mit und ohne Fortschritt.
- **120.8 Liste:** Paging, Filterkarte, Spaltenfilter inkl. Fälligkeit, „Nur meine“, Werkbank-Spalte.
- **120.9 Druck/QR:** Druck, QR scannen.
- **120.10 Nummernkreis parallel:** zwei Browser geben gleichzeitig frei → unterschiedliche Nummern.
- **120.11 Altbestand:** Kopie der Produktiv-DB: Trockenlauf (Zählungen notieren), Ausführung, Stichprobe 3 FAs (erledigt,
  teilweise, nur gepickt), zweiter Lauf bricht ab.
- **120.12 Rollen:** `picking` ohne Leitstand-Knöpfe, `masterdata_read` ohne Speichern.
- Regressionsprüfung: TS-79 (Kommissionierung nur am HauptFA), TS-80 (Kommissionierliste summiert, unverändert), Wagen-Konflikt.

## Etappen (nur bei epic: true)

Ein Worktree, ein Merge am Ende; je Etappe ein Commit, Build + Tests grün. Branch während der Arbeit mit
`scripts/sync-worktree.ps1 -Slug <slug>` auf `main`-Stand halten (OSEON-Hotfix, siehe Deploy). Der Dev-Lauf startet erst,
wenn Teil A ([[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]]) im Worktree committed ist (E3 braucht das FA-Feld).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| E1 | Modell `PickingArea`/`PickingAreaRule`/`PickingOrder`/`PickingOrderLine`, Enums, Migrationen → `SQL/133`, `SQL/134`, FreshInstall (2 Stellen); Nummern-Helfer aus `BeschichtungsauftragWorkflow` extrahiert (BA-Tests grün); `KommissionierbereichRegel` + Unit-Tests; Bereichs-Stammdatenmaske mit Regeln (ADR 0020). *Hängt an Antwort 1, 8, 9 – ohne Antwort: Rangfolge laut Empfehlung, Spalte Vorkommissionierung weglassen und als offen vermerken.* | offen | |
| E2 | `PickingOrderWorkflow.ErzeugeOffeneAsync` + `PickingOrderRestmengen` (Unit-Tests); Hooks in `ToggleRelease`/`BulkRelease`/`IndexDropdown`; Knopf „Fehlende Kommissionieraufträge erzeugen“; Auftragsliste `/Picking` lesend (Listen-View-Pattern). *Hängt an Antwort 2, 3, 7.* | offen | |
| E3 | Auftragsseite (Bom-Ansicht im Auftragsmodus), Ist-Menge, Umbuchung über `PickingTransferService` auf Zeilen inkl. FA-Feld Teil A, Abschluss vollständig/teilweise + Folgeauftrag, `IsDonePicking`/Statustext; `/Picking/Bom/{faId}` lesend; Altpfade stilllegen, Tests umstellen. *Hängt an Antwort 5 und an Teil A.* | offen | |
| E4 | Leitstand-Spalte Aufträge, Freigabe zurücknehmen, Priorität/Kommissionierer durchschreiben, Druck mit QR (ersetzt `PrintPicking`). *Hängt an Antwort 4, 6.* | offen | |
| E5 | Einmal-Skript `SQL/Einmalig/…_Kommissionierauftraege_Altbestand.sql` mit `@Trockenlauf`; nur anlegen, **nie ausführen** (ADR 0015). *Hängt an Antwort 3, 10.* | offen | |
| E6 | Hilfe, Anwender-Changelog, Version 1.88.0 (Web + Service), `docs/TESTSZENARIEN.md` TS-120, Brain (Changelog, feature-map, codebase-Karte, Glossar, ADR). | offen | |
| E7 | **Nur nach Antwort 12 = bauen:** ServiceSetting `KommissionierungSageBuchungAktiv` (Default aus) + Weg in die Sage-Queue. Ohne Antwort oder bei „nicht in diesem Epic“: Zeile als Blocker vermerken, Epic ohne E7 abschließen. | offen | |

## Deploy

*Vorläufig – der qa-agent bestätigt gegen den echten Diff.*

- **Web-App:** ja (Controller, Views, Workflow, Migrationen).
- **Service:** nein (E1–E6). Nur mit E7 ja (neuer Katalog-Key).
- **Migration:** ja – `SQL/133`, `SQL/134` (additiv) + Einmal-Skript Altbestand (**daten-umbauend → DB-Backup vor Deploy**).
- **Reihenfolge je Standort:** DB-Backup → Web stoppen → `SQL/133`, `SQL/134` → Einmal-Skript im **Trockenlauf**, Zählungen ins
  Deploy-Protokoll → Freigabe der Zählung durch Gerald → Einmal-Skript ausführen → Web deployen/starten → im Leitstand
  „Fehlende Kommissionieraufträge erzeugen“ für alle freigegebenen offenen FAs → Stichprobe TS-120.11. Zwischen Skript und
  Start darf niemand kommissionieren (Wartungsfenster).
- **Konfliktstelle beim Merge/Sync:** Im Hauptcheckout läuft der OSEON-Hotfix ([[2026-11-07-oseon-hotfix-main]],
  [[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]]) mit **Z3 „Spalte Werkbank in der Kommissionierungs-Maske“**:
  `Views/Picking/Index.cshtml`, `PickingListItem.WorkplaceName`, `ColumnMap["workbench"]` in `PickingController`, dazu Z1/Z2 in
  `PrintBom.cshtml`/`Bom.cshtml`/`RenderPrintBomAsync`. Hier **nicht** nachbauen; beim `sync-worktree.ps1` die Werkbank-Spalte
  in die neue Auftragsliste übernehmen (AK 10) und Z2 (Druckreihenfolge der sichtbaren Positionen) im Auftragsmodus erhalten.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (Service nur mit E7: `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`)

### Prüfabfragen (WMS-DB, nur lesend, je Standort vom Menschen auszuführen)

Aussagen, die davon abhängen (Bereichsregeln je Standort, Umfang B1, Altbestand-Umfang), sind bis dahin **ungeprüft**.

```sql
-- P1 (IDEAL) heute genutzte Kommissionier-Ziele; bewusst OHNE SubFA = 0 (KommissionierListenService.cs:90-93)
SELECT LTRIM(RTRIM(Kommissionieren)) AS Ziel, COUNT(*) AS Positionen,
       SUM(CASE WHEN SubFA = 0 THEN 1 ELSE 0 END) AS davonBlatt, COUNT(DISTINCT HauptFA) AS HauptFAs
FROM dbo.FaHierarchyNodes GROUP BY LTRIM(RTRIM(Kommissionieren)) ORDER BY Positionen DESC;

-- P2 (AKE, auch IDEAL) Artikelgruppe/Lagerort der bisher kommissionierten Artikel
SELECT a.ArticleGroup, sa.Lagerkennung, COUNT(DISTINCT a.Id) AS Artikel
FROM dbo.PickingItems p
JOIN dbo.Articles a ON a.ArticleNumber = p.BomArticleNumber
LEFT JOIN dbo.StorageLocations sl ON sl.Id = a.PrimaryStorageLocationId
LEFT JOIN dbo.StorageAreas sa ON sa.Id = sl.StorageAreaId
GROUP BY a.ArticleGroup, sa.Lagerkennung ORDER BY Artikel DESC;

-- P3 Befund B1: FAs mit Fortschritt und Auftragsmenge <> 1
SELECT COUNT(DISTINCT po.Id) AS FAs, SUM(CASE WHEN p.IsTransferred = 1 THEN 1 ELSE 0 END) AS umgebuchteZeilen
FROM dbo.PickingItems p JOIN dbo.ProductionOrders po ON po.Id = p.ProductionOrderId
WHERE po.Quantity <> 1 AND (p.IsPicked = 1 OR p.IsTransferred = 1);

-- P4 Leitstand/Zuweisung je Standort
SELECT [Key], [Value] FROM dbo.AppSettings WHERE [Key] IN ('LeitstandAktiv', 'KommissionierungMitZuweisung');

-- P5 Altbestand-Umfang
SELECT
  (SELECT COUNT(DISTINCT ProductionOrderId) FROM dbo.PickingItems WHERE IsPicked = 1 OR IsTransferred = 1) AS FAsMitFortschritt,
  (SELECT COUNT(DISTINCT ProductionOrderId) FROM dbo.PickingItems) AS FAsMitPickingItems,
  (SELECT COUNT(*) FROM dbo.ProductionOrderPickingStatus ps JOIN dbo.ProductionOrders po ON po.Id = ps.ProductionOrderId
     WHERE ps.IsReleasedForPicking = 1 AND ps.IsDonePicking = 0 AND po.IsDone = 0 AND po.IsCancelled = 0) AS FreigegebenOffen;
```

## Abhängigkeiten

- **Teil A** [[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]] (SQL 109–111): liefert das strukturierte FA-Feld an
  `StockMovement`. Kommissionier-Entnahmen (E3) setzen es. Der genaue Feldname steht in Teil A – der Dev-Lauf liest ihn dort.
  Teil A schließt die Kommissionierung aus seiner eigenen Maskenumstellung aus (Backlog-Entscheidung 1) – diese Spec ist der
  Ort dafür. Die Sage-Ausschlussregel „Umbuchung mit FA-Bezug“ (`SageBookingQueueRepository.cs:149-151`,
  `SageBookingEnqueueDecision.IstUmbuchungMitFaBezug`) muss nach Teil A weiter greifen; stellt Teil A sie auf das neue Feld um,
  ist das für Kommissionier-Umbuchungen gleichwertig, weil beide Felder gesetzt werden.
- **OSEON-Hotfix auf `main`** (Z3 Werkbank-Spalte) – Konfliktstelle, siehe Deploy.
- **Rückmelde-Epic** [[2026-11-02-sage-pps-fa-bde-abgleich-spec]]: Materialentnahme mit FA-Bezug nach Sage – Ziel für E7.
- Vorbilder: [[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]] (Nummernkreis, Status), [[2026-09-25-kommissionierung-nur-hauptfa-spec]]
  (nur HauptFA, Relevanzfilter), [[2026-09-25-kommissionierliste-summierung-pdf-spec]] (Listen unverändert).
- Bündel: [[2026-11-08-buendel-uebersicht]] (Teil C, eigener Dev-Lauf, Version 1.88.0, SQL 133–139, TS-120).
- Rollen: **keine neue Rolle, kein neuer Filter.** Genutzt werden `[RequirePickingAccess]` (Abarbeiten),
  `[RequireLeitstandAccess]` (Erzeugen/Stornieren/Prio), `[RequirePickingOrLeitstandAccess]` (Leitstand-Klasse),
  `[RequireMasterDataReadAccess]`/`[RequireMasterDataAccess]` (Bereiche) – [[controller]] Z. 51-71. Braucht der Dev-Lauf doch
  einen, dann an **drei** Stellen: Attribut, `secondbrain/codebase/controller.md`, `Views/Users/RoleOverview.cshtml`.

## Offene Rueckfragen

1. **Rangfolge der Bereichsregel.** Gerald nannte Kommissionieren → Lagerort → Artikelgruppe → Artikel-Ausnahme. Eine
   Ausnahme, die nicht gewinnt, wirkt nie. *Empfehlung:* Artikel → Kommissionierziel → Lagerort → Artikelgruppe → Standardbereich.
2. **Positionsumfang.** Heute legt AKE für **alle** Stücklistenpositionen Fortschrittszeilen an (`PickingRepository.cs:34-46`),
   IDEAL zeigt per Standardfilter nur Positionen mit Kommissionierziel. *Empfehlung:* AKE alle Positionen (inkl.
   Baugruppen-Zeilen, der Kommissionierer entscheidet wie heute), IDEAL nur `KommissionierRelevanzFilter.IsRelevant`.
3. **Soll-Menge (Befund B1).** *Empfehlung:* Soll = hochgerechnete Menge wie in der Stückliste angezeigt
   (`BomQuantityResolver`); Altbestand-Ist = tatsächlich gebuchte Menge. Vorher P3 ausführen; liefert P3 FAs, B1 als eigenen
   Bug aufnehmen.
4. **Freigabe zurücknehmen.** *Empfehlung:* Aufträge ohne Fortschritt stornieren, Aufträge mit Fortschritt bleiben + Warnung.
5. **Folgeauftrag bei „teilweise“.** *Empfehlung:* automatisch anlegen (gleicher Bereich, Priorität, Kommissionierer, Bezug
   auf den Ursprung) – sonst fällt der Rest still durch.
6. **Priorität/Kommissionierer.** *Empfehlung:* am Auftrag gespeichert (Liste je Auftrag), Leitstand-Änderung am FA schreibt
   auf offene Aufträge durch; Einzeländerung je Auftrag erst bei Bedarf.
7. **Standort ohne Leitstand.** Erst P4 ausführen. *Empfehlung:* Öffnen über die Auswahlliste erzeugt die Aufträge.
8. **Fälligkeit Vorkommissionierung.** *Empfehlung:* Kennzeichen „Vorkommissionierung“ am Bereich (eine Spalte); solche
   Aufträge rechnen über `PrePickingDaysResolver` ([[fallstricke]] „OverridePrePickingDays“), alle anderen wie heute.
9. **Bereichs-Stammdaten.** *Empfehlung:* flach (Unterteilung = weitere Bereiche wie „Vorkomm. Kupfer“), kein
   Standard-Kommissionierer/-Drucker je Bereich in diesem Epic.
10. **Altbestand-Umfang.** *Empfehlung:* nur FAs mit Fortschritt per Einmal-Skript (ein Altbestand-Auftrag im
    Standardbereich); offene Freigaben ohne Fortschritt erzeugt der Leitstand-Knopf mit Bereichen.
11. **Tabelle `PickingItems`.** *Empfehlung:* nach der Umstellung nur lesend behalten (Nachweis), Löschen als Aufgabe nach
    einer Abnahmeperiode; FK-Drift B3 dann mit erledigen.
12. **Sage-Etappe E7.** Heute schließt Regel F9 Umbuchungen mit FA-Bezug von Sage aus (`SageBookingQueueRepository.cs:149-151`);
    fachlich wäre die Materialentnahme mit FA-Bezug das Ziel. *Empfehlung:* E7 nicht in diesem Epic, an das Rückmelde-Epic
    hängen; Schalter erst dort.
13. **Globaler Schalter.** *Empfehlung:* keiner – Doppelpfad (alt/neu) in Stückliste und Umbuchung wäre teurer als die
    Umstellung im Wartungsfenster; Absicherung über Backup, Trockenlauf und TS-120 auf dem Testsystem.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Rangfolge der Bereichsregel →
2. Positionsumfang AKE/IDEAL →
3. Soll-Menge (B1, P3) →
4. Freigabe zurücknehmen →
5. Folgeauftrag automatisch →
6. Priorität/Kommissionierer durchschreiben →
7. Standort ohne Leitstand (P4) →
8. Fälligkeit Vorkommissionierung →
9. Bereichs-Stammdaten schlank →
10. Altbestand-Umfang →
11. Tabelle PickingItems behalten →
12. Sage-Etappe E7 →
13. Kein globaler Schalter →
