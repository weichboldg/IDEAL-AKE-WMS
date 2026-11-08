---
type: spec
title: Kommissionieraufträge (Kopf/Zeilen, KO-Nummernkreis, Typ, Kommissionierbereiche), 1–n je FA
slug: 2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec
status: Testbereit
created: 2026-11-07
updated: 2026-11-08
source_backlog: "[[2026-11-07-kommissionierung-auftraege-kopf-zeilen]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
depends_on: "[[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]]"
task: "[[2026-11-08-buendel-2026-11-08-goal-lauf-2]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
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
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-07"
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
| E1 | Modell `PickingArea`/`PickingAreaRule`/`PickingOrder`/`PickingOrderLine`, Enums, Migrationen → `SQL/133`, `SQL/134`, FreshInstall (2 Stellen); Nummern-Helfer aus `BeschichtungsauftragWorkflow` extrahiert (BA-Tests grün); `KommissionierbereichRegel` + Unit-Tests; Bereichs-Stammdatenmaske mit Regeln (ADR 0020). *Hängt an Antwort 1, 8, 9 – ohne Antwort: Rangfolge laut Empfehlung, Spalte Vorkommissionierung weglassen und als offen vermerken.* | erledigt | `3500c2fe` |
| E2 | `PickingOrderWorkflow.ErzeugeOffeneAsync` + `PickingOrderRestmengen` (Unit-Tests); Hooks in `ToggleRelease`/`BulkRelease`/`IndexDropdown`; Knopf „Fehlende Kommissionieraufträge erzeugen“; Auftragsliste `/Picking` lesend (Listen-View-Pattern). *Hängt an Antwort 2, 3, 7.* | erledigt | `1f089b3a` |
| E3 | Auftragsseite (Bom-Ansicht im Auftragsmodus), Ist-Menge, Umbuchung über `PickingTransferService` auf Zeilen inkl. FA-Feld Teil A, Abschluss vollständig/teilweise + Folgeauftrag, `IsDonePicking`/Statustext; `/Picking/Bom/{faId}` lesend; Altpfade stilllegen, Tests umstellen. *Hängt an Antwort 5 und an Teil A.* | erledigt | `e0666d98`, `4dc165cc` |
| E4 | Leitstand-Spalte Aufträge, Freigabe zurücknehmen, Priorität/Kommissionierer durchschreiben, Druck mit QR (ersetzt `PrintPicking`). *Hängt an Antwort 4, 6.* | erledigt | `79bb524c` |
| E5 | Einmal-Skript `SQL/Einmalig/…_Kommissionierauftraege_Altbestand.sql` mit `@Trockenlauf`; nur anlegen, **nie ausführen** (ADR 0015). *Hängt an Antwort 3, 10.* | erledigt | `ff3015c6` |
| E6 | Hilfe, Anwender-Changelog, Version 1.88.0 (Web + Service), `docs/TESTSZENARIEN.md` TS-120, Brain (Changelog, feature-map, codebase-Karte, Glossar, ADR). | erledigt | `15a8e45f` |
| E7 | **Nur nach Antwort 12 = bauen:** ServiceSetting `KommissionierungSageBuchungAktiv` (Default aus) + Weg in die Sage-Queue. Ohne Antwort oder bei „nicht in diesem Epic“: Zeile als Blocker vermerken, Epic ohne E7 abschließen. | **Blocker** (Antwort 12: nicht in diesem Epic, Sage-Schreibzugriff gesperrt) | – |

## Deploy

*Final (qa-agent, QA Reparatur 1, 2026-11-07) gegen den echten Diff `7ab2fcb5..1ed5c72c`:* 45 Dateien `IdealAkeWms/`, 1 Datei
`IDEALAKEWMSService/` (nur `AppVersion.cs` 1.88.0), neue Migration `20261107193219_AddPickingAreasAndOrders`, `SQL/133`, FreshInstall,
Einmal-Skript. `SQL/134` wird **nicht** gebraucht (Nummer an die Bündel-Übersicht zurück).

- **Web-App:** ja (Controller, Views, Workflow, Migration).
- **Service:** ja, **nur Versionsstand** (`AppVersion.cs` 1.88.0, keine Logik); E7 nicht gebaut.
- **Migration:** ja – `SQL/133_AddPickingAreasAndOrders.sql` (additiv, inkl. Seed) + Einmal-Skript Altbestand (**daten-umbauend → DB-Backup vor Deploy**).
- Nach dem Merge nur dann erneut aus `main` publishen, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammenführt (hier
  wahrscheinlich: OSEON-Hotfix Z3 in `Views/Picking/Index.cshtml`/`PickingController`).
- **Reihenfolge je Standort:** DB-Backup → Web stoppen → `SQL/133` → Einmal-Skript im **Trockenlauf**, Zählungen ins
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
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService` (nur Versionsstand; nach dem Web starten)

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

**E0-Ergebnis (Dev, 2026-11-08, lesend, WMS-DEV-Datenbanken – Produktiv-DBs nicht abgefragt):**
- P1 IDEAL-DEV: `Kommissionieren` = NULL 474 Positionen (391 Blatt), `KOM` 148, `K-02` 55, `ISO` 27, `H2-02` 14, `LÖT` 13, `EM` 11, `H3-01` 5,
  `PG` 3, `K-01` 3, `H1-03` 1, `x` 1. AKE-DEV: keine FaHierarchyNodes.
- P2: `StorageAreas.Lagerkennung` ist bei allen bisher kommissionierten Artikeln **leer** (AKE-DEV 2.656 Artikel Gruppe 960 u. a., IDEAL-DEV
  Gruppen 030102 …) → die Bereichsregel „Lagerort“ greift auf DEV nie (Standardbereich). Kein Widerspruch zur Spec (Regel optional), aber
  Bereiche „Lagerort“ erst nach gefülltem Lagerort-Abgleich sinnvoll.
- P3 (Befund B1): AKE-DEV **6 FAs** mit Auftragsmenge ≠ 1 und Fortschritt, 12 umgebuchte Zeilen → Mengenabweichung real; Antwort 3
  (Soll hochgerechnet, Altbestand Ist = gebuchte Menge) gilt. IDEAL-DEV 0.
- P4: AKE-DEV `LeitstandAktiv=true`, `KommissionierungMitZuweisung=true`; IDEAL-DEV `LeitstandAktiv=true`, Zuweisung `false`.
- P5: AKE-DEV 15 FAs mit Fortschritt / 80 mit PickingItems / 1 freigegeben-offen; IDEAL-DEV 0 / 16 / 0.
- Keine harte Abweichung → kein Blocker.

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

1. Rangfolge der Bereichsregel → **wie empfohlen (Gerald 2026-11-07 19:17)**
2. Positionsumfang AKE/IDEAL → **wie empfohlen (Gerald 2026-11-07 19:17)**
3. Soll-Menge (B1, P3) → **wie empfohlen (Gerald 2026-11-07 19:17)**
4. Freigabe zurücknehmen → **wie empfohlen (Gerald 2026-11-07 19:17)**
5. Folgeauftrag automatisch → **wie empfohlen (Gerald 2026-11-07 19:17)**
6. Priorität/Kommissionierer durchschreiben → **wie empfohlen (Gerald 2026-11-07 19:17)**
7. Standort ohne Leitstand (P4) → **wie empfohlen (Gerald 2026-11-07 19:17)**
8. Fälligkeit Vorkommissionierung → **wie empfohlen (Gerald 2026-11-07 19:17)**
9. Bereichs-Stammdaten schlank → **wie empfohlen (Gerald 2026-11-07 19:17)**
10. Altbestand-Umfang → **wie empfohlen (Gerald 2026-11-07 19:17)**
11. Tabelle PickingItems behalten → **wie empfohlen (Gerald 2026-11-07 19:17)**
12. Sage-Etappe E7 → **wie empfohlen (Gerald 2026-11-07 19:17)**
13. Kein globaler Schalter → **wie empfohlen (Gerald 2026-11-07 19:17)**

## Umsetzung (Dev, 2026-11-08, Bündel 2026-11-08 Lauf 2) – v1.88.0

Lauf [[2026-11-08-buendel-2026-11-08-goal-lauf-2]]. Baut auf Teil A ([[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]], v1.87.0, Feld
`StockMovement.ProductionOrderId`). Build grün, Tests 506 Service / 2.526 Web (1 übersprungen) grün.

- **E1 `3500c2fe`:** Modelle `PickingArea`/`PickingAreaRule`/`PickingOrder`/`PickingOrderLine`; **eine** EF-Migration
  `20261107193219_AddPickingAreasAndOrders` → **`SQL/133_AddPickingAreasAndOrders.sql`** (inkl. Seed; `SET QUOTED_IDENTIFIER ON` wegen
  gefiltertem UNIQUE-Index Standardbereich). **Nummer 134 nicht gebraucht – an die Bündel-Übersicht zurück.** Auf AKE-/IDEAL-DEV zweimal fehlerfrei.
  Nummernvergabe in `Services/Nummernkreis.cs` aus dem BA gezogen (BA-Tests 80/80 grün). `KommissionierbereichRegel`, Stammdatenmaske
  `PickingAreasController` (Bereiche Client-Mode, Regeln Server-Mode + Paging, Wert-Auswahl aus Stamm, Server prüft).
- **E2 `1f089b3a`:** `PickingOrderWorkflow.ErzeugeOffeneAsync` (Restmengen je Schlüssel, je Bereich ein Auftrag, idempotent, Sammelmeldung);
  Hooks `ToggleRelease`, `BulkRelease` (`BulkReleaseResult.ProcessedIds`), Knopf „Fehlende Kommissionieraufträge erzeugen“ (Auswahl oder alle);
  `/Picking` = Auftragsliste (ersetzt die FA-Liste inkl. IDEAL-Gruppierung; `_PickingRow` entfernt).
- **E3 `e0666d98`, `4dc165cc`:** Auftragsseite `/Picking/Auftrag/{id}` (Stückliste im Auftragsmodus), Ist-Menge (Text, Server prüft >0 und ≤ Soll),
  `PickingTransferService.TransferOrderLinesAsync` (direkt über den Context → nie Sage; Bewegung mit FA-Text **und** `ProductionOrderId`),
  `AbschliessenAsync` (vollständig/teilweise + Folgeauftrag, `IsDonePicking`/Statustext); `/Picking/Bom/{faId}` lesend mit Auftragsliste,
  ohne Leitstand legt das Öffnen die Aufträge an. Altpfade `TogglePicked`/`TransferPicked`/`SetPickingStatus`/`PrintPicking` entfernt,
  `IPickingRepository`/`PickingRepository` gelöscht (Tabelle `PickingItems` bleibt, Antwort 11).
- **E4 `79bb524c`:** Freigabe zurücknehmen storniert Aufträge ohne Fortschritt (Rest gemeldet), Prio/Kommissionierer durchschreiben, Aufträge in der
  Leitstand-Zeile; Druck je Auftrag `AuftragDrucken` (in E3 vorgezogen).
- **E5 `ff3015c6`:** `SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql` – **nicht ausgeführt**. DEV AKE Trockenlauf: 15 FAs
  (14 abgeschlossen, 1 in Arbeit), 41 Zeilen umgebucht, 13 gepickt, B1 6 FAs; 1 freigegebener FA ohne Fortschritt (→ Nachziehen-Knopf).
  Ausführungspfad in einer Transaktion mit ROLLBACK geprüft (15 Aufträge, 54 Zeilen, danach 0).
- **E6 `15a8e45f`:** Version 1.88.0, Hilfe, Anwender-Changelog, README, Einstellungstexte (`LeitstandAktiv`, `KommissionierungMitZuweisung`,
  `KommissionierTage`), TS-120.
- **E7:** Blocker laut Antwort 12 / Freigabe-Nachtrag (Sage-Schreibzugriff) – nicht gebaut.

**Abweichungen / Präzisierungen (bitte bestätigen):**
- Druck: **Code-128-Barcode** statt QR – der vorhandene `IBarcodeService` (wie die Kommissionierlisten) kann nur Code 128; keine neue Bibliothek ohne ADR.
- Leitstand: Aufträge in der **Status-Zelle** statt eigener Spalte (keine Änderung der Spalten-/Zahnrad-Konsistenz).
- Restmengen: Ist ein Auftrag **„vollständig“** abgeschlossen, zählt eine umgebuchte Zeile mit Ist &lt; Soll als voll abgedeckt – sonst legte das
  Nachziehen die bewusst nicht gebrauchte Differenz neu an.
- Umbuchen: Artikel im WMS unbekannt → **Fehlermeldung, nichts gebucht** (vorher still übersprungen) – Test entsprechend geändert.
- FA erledigt, wenn **kein** Auftrag mehr Offen/In Arbeit ist (Teilweise abgeschlossene zählen als erledigt, ihr Rest steht im Folgeauftrag).
- **OSEON-Hotfix Z3** (`066b136f`, nur `main`) nicht in den Zweig geholt („kein Merge“); Werkbank-Spalte in der neuen Auftragsliste nativ
  (`ProductionWorkplace.Name`). Beim Sync/Merge: Konflikt in `Views/Picking/Index.cshtml`/`PickingController` – die Auftragsliste gewinnt.
- Geänderte bestehende Tests (gewollt): `PickingControllerTests` (FA-Liste → Auftragsliste), `PickingTransferServiceTests` +
  `…AggregationTests` (Zeilen statt PickingItems), `PickingLeitstandControllerTests` (Workflow-Mocks), `TableToolbarHostTests`
  (Picking/Index ohne Zählzeile „Sub-FAs“), `PickingRepositoryTests` gelöscht.

## QA v1.88.0 (qa-agent, 2026-11-07) – Ergebnis: NICHT Testbereit (Blocker)

Geprüft: Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, HEAD `15a8e45f`,
Review-Bereich `7ab2fcb5..15a8e45f` (E1–E6). Status bleibt **InUmsetzung**.

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (vorbestehend).
- `dotnet test`: IDEALAKEWMSService.Tests 506/506 bestanden, 0 übersprungen; IdealAkeWms.Tests 2.526 bestanden, 1 übersprungen, 0 Fehler (gesamt 2.527).

### 2. DEV-Deploy
- `standort: beide` aus dem Frontmatter. `dev-deploy.ps1 -Standort AKE` und `-Standort IDEAL` mit `-Quelle <worktree>`, erhöhte Shell:
  beide Exit 0, „HTTP 200, Fusszeile v1.88.0“ = AppVersion 1.88.0 (Web + Service). Migration `20261107193219_AddPickingAreasAndOrders` auf beiden DEV-DBs vorhanden.
- `SQL/133` zweiter Lauf auf AKE_WMS_DEV: Exit 0, keine PRINT-Ausgabe, 3 Bereiche – idempotent.

### 3. Code-Review (Diff `7ab2fcb5..15a8e45f`, selbst durchgeführt)
Geprüft: `PickingOrderWorkflow`, `PickingOrderRestmengen`, `KommissionierbereichRegel`, `Nummernkreis` + BA-Umbau, `PickingTransferService`,
`PickingController` (Index/Bom/Auftrag/AuftragUmbuchen/AuftragAbschliessen/AuftragDrucken), `PickingLeitstandController` (Hooks, Nachziehen,
Rücknahme, Durchschreiben), `PickingAreasController` + Views, `PickingOrderRepository`/`PickingAreaRepository`, `SQL/133`, FreshInstall
(Schema Z. 2724 ff. + MigrationId Z. 3135), Einmal-Skript, Views `Picking/Index|Bom|PrintPicking`, Leitstand-Zeile, geänderte Tests.
Defektklassen: kein `@Html.Raw`/JS-String-Einbettung neu; kein `hidden`+`d-*`; Umbuchungs-Items indiziert gebunden (`items[i].…`), keine
Positionsbindung paralleler Arrays.

| ID | Schwere | Befund | Fundstelle | Behandlung |
|---|---|---|---|---|
| **Q1** | **Blocker** | Auftragsseite `/Picking/Auftrag/{id}` liefert **HTTP 500** „The view 'Auftrag' was not found“: `BomSeiteAsync` endet mit `return View(vm);` ohne View-Namen, die Action heißt `Auftrag` → sucht `Views/Picking/Auftrag.cshtml`. Damit sind Abarbeiten, Ist-Menge, Umbuchung und Abschluss (AK 11–14, TS-120.3/.4/.5) in der laufenden Anwendung **nicht erreichbar**. Unit-Tests rufen keine View-Auflösung auf. Fix: `return View("Bom", vm);`. | `IdealAkeWms/Controllers/PickingController.cs` (`BomSeiteAsync`, Ende), DEV-Log `idealakewms-20261107_017.log` 22:09:54 | offen → Dev |
| **Q2** | Hoch | Standardbereich wechseln auf einen Bereich mit **kleinerer Id** als der bisherige Standard → **HTTP 500** `Cannot insert duplicate key row … UX_PickingAreas_IsDefault` (EF schreibt das UPDATE des neuen Standards vor dem des alten; InMemory erzwingt den gefilterten Index nicht). Auf DEV reproduziert: Lager→Glas ging, Glas→Lager 500. Ohne SQL ist der alte Standard nicht wiederherstellbar. Fix: alten Standard zuerst speichern (zwei `SaveChanges` in einer Transaktion) o. ä. | `PickingAreasController.Edit` (POST), DEV-Log 22:07:14 | offen → Dev |
| Q3 | Mittel | Abschluss nur über das Modal direkt nach einer erfolgreichen Umbuchung erreichbar. Wird es geschlossen/die Seite neu geladen, oder hat ein Auftrag keine umbuchbare Zeile mehr (z. B. Artikel im WMS unbekannt → jetzt Fehlermeldung statt Überspringen), bleibt der Auftrag dauerhaft „Offen/In Arbeit“; FA wird nie über diesen Weg „erledigt“. Ein Abschließen-Knopf auf der Auftragsseite fehlt. | `Views/Picking/Bom.cshtml` (statusModal nur in `doTransfer` success), `PickingController.AuftragAbschliessen` | offen → Dev/Gerald |
| Q4 | Mittel | `AuftraegeNachziehen(ids)` prüft serverseitig nicht, ob die gewählten FAs freigegeben, HauptFA, nicht erledigt/storniert sind – nur der Browser deaktiviert den Knopf. Ein POST mit beliebigen Ids legt Aufträge für nicht freigegebene/erledigte FAs an (`ErzeugeOffeneAsync` prüft nur SubFA). | `PickingLeitstandController.AuftraegeNachziehen`, `PickingOrderWorkflow.ErzeugeOffeneAsync` | offen → Dev |
| Q5 | Niedrig | Leere Ist-Menge wird serverseitig still zu Soll (`string.IsNullOrWhiteSpace → SollMenge`). Löscht der Kommissionierer das Feld, wird die volle Menge gebucht. Feld ist vorbelegt, daher nur bei bewusster Leerung. Besser: leer ablehnen. | `PickingTransferService.cs:62` | Hinweis |
| Q6 | Niedrig | Doppelte `PickingItemId` in der Auswahl wird nicht abgewiesen → zwei Bewegungen für eine Zeile möglich (nur per manipuliertem POST). | `PickingTransferService.TransferOrderLinesAsync` Auswahlprüfung | Hinweis |
| Q7 | Niedrig | `ColumnDefinitions.Picking` nicht nachgezogen (alte Spalten matchcode/customer/quantity, fehlen nummer/workbench/bereich; `order-number` dort locked, im View `nummer`). Spec verlangte Vier-Stellen-Konsistenz; Server prüft nur den ViewKey, daher keine Fehlfunktion, aber Drift. | `Models/ViewModels/ColumnDefinitions.cs:188-208` | offen → Dev |
| Q8 | Niedrig | Kontrast: Knopf „Zuordnungsregeln“ `btn-outline-primary` auf dunklem Kartenkopf (blau auf Navy, < 3:1). | `Views/PickingAreas/Index.cshtml:24` | offen → Dev |
| Q9 | Niedrig | Zeilenstatus „Gepickt“ wird im neuen Code nie gesetzt (nur Altbestand); „In Arbeit“ entsteht erst mit der ersten Umbuchung (Spec: erste gepickte Zeile). Druck zeigt Zeilenstatus als Enum-Text (`Uebertragen`). Bewusst? | `PickingTransferService`, `PickingController.AuftragDrucken` | an Gerald |
| Q10 | Niedrig | Abweichung AK 2: ein zweiter Standard wird nicht abgelehnt, sondern **löst den alten ab** (Hilfetext so beschrieben). Fachlich vertretbar, aber nicht in der Abweichungsliste der Umsetzung. | `PickingAreasController.Edit` | an Gerald bestätigen |
| Q11 | Hinweis | Altbestand + B1: Für FAs mit Menge ≠ 1, die offen sind, erzeugt das Nachziehen nach dem Einmal-Skript einen Rest-Auftrag über (Menge−1)×Stücklistenmenge (Altbestand-Soll = Stückmenge, neues Soll hochgerechnet). AKE-DEV: 1 solcher FA. Erwartung vor dem Produktiv-Lauf klären. | Einmal-Skript Z. 104, `PickingOrderRestmengen` | an Gerald |
| Q12 | Niedrig | Brain-Changelog `secondbrain/changelog/…v1-88-0…` und `feature-map.md`-Eintrag fehlen (E6 nennt sie). | Hauptcheckout `secondbrain/` | offen → Dev |

### 4. Beweisart je AK
| AK | Beweis | Ergebnis |
|---|---|---|
| 1 | Manual-UAT DEV (SQL/133 2. Lauf, History-Eintrag); FreshInstall nur Sichtprüfung | OK (DEV); FreshInstall auf leerer DB nicht ausgeführt |
| 2 | Unit (`Bereich_*_AK2`) + DEV | **FEHLER** (Q2); Pflicht/Eindeutig/Standard-nicht-abwählbar OK |
| 3 | Unit (`Regel_WertImStamm_Eindeutig_AK3`) + DEV Doppelregel | OK (Doppelregel-Meldung auf DEV); „Wert nicht im Stamm“ nur Unit |
| 4 | Unit | grün |
| 5 | Unit (Format/Jahr), parallele Nummer Manual-UAT | Unit grün; Parallelität NICHT MOEGLICH (zwei Sessions) |
| 6 | Unit + DEV | OK: FA 2614027 → KO-2026-00001 Lager (149 Zeilen) + KO-2026-00002 Glas (1 Zeile, Artikel-Regel), Prio 15/Kommissionierer „test“ übernommen; 2. Nachziehen „0 … 1 FA ohne offene Restmenge“ |
| 7, 8, 9 | Unit | grün; DEV nicht nachgestellt (kein Sub-FA/kein FA ohne Stückliste freigegeben, Stückliste aus Sage nicht änderbar) |
| 10 | DEV | OK: Spalten inkl. Werkbank, „Nur meine“ (leer für Gerald), „aller Kommissionierer“, Spaltenfilter Nummer und Fälligkeit, Bereichsfilter; Paging nur mit 2 Aufträgen (keine 2. Seite) |
| 11–14 | Unit (`PickingTransferServiceTests`, `PickingOrderWorkflowTests`) | **FEHLER in der Anwendung (Q1)** – Auftragsseite 500, nicht bedienbar |
| 15 | DEV | OK: `/Picking/Bom/363` lesend, Kopfkarte „Kommissionieraufträge dieses FA“ |
| 16 | Unit + DEV | OK: Rücknahme storniert KO-00001, behält KO-00002 (Zeile simuliert gepickt) mit Warnung |
| 17 | Unit + DEV (Prio) | OK: Prio 15→7 auf beide Aufträge durchgeschrieben, Audit gesetzt; Kommissionierer nur Unit |
| 18 | DEV | OK: Druck KO-2026-00002 mit Bereich, Fälligkeit, Code-128-Barcode (Abweichung QR→Code 128 dokumentiert); Scan = Spaltenfilter Nummer findet den Auftrag |
| 19 | Manual-UAT | NICHT MOEGLICH für mich (Ausführung des Einmal-Skripts – auch Trockenlauf – vom Berechtigungssystem verweigert); Dev-Nachweis Trockenlauf/ROLLBACK liegt vor; read-only: 0 Altbestand-Aufträge, 0 PickingItems ohne Artikelnummer |
| 20 | Code (Attribute) | `[RequireMasterDataReadAccess]`/`[RequireMasterDataAccess]`, `[RequireLeitstandAccess]` auf Nachziehen; Rollen-UAT NICHT MOEGLICH (nur Admin-Konto) |
| 21 | Datei-Prüfung | Version 1.88.0 beide, Changelog, Hilfe, TS-120 vorhanden; Brain-Changelog fehlt (Q12) |

### 5. Geänderte bestehende Tests
- `PickingTransferServiceTests`, `PickingTransferServiceAggregationTests`: Fixture PickingItem → PickingOrderLine – Verhalten gewollt geändert (AK 11/12).
  `TransferPickedItems_ArticleNotFound_*`: Erwartung „überspringen“ → „Fehler, nichts gebucht“ – gewollt (Melden statt still), Folge siehe Q3.
- `PickingControllerTests`: FA-Liste → Auftragsliste (AK 10). `PickingLeitstandControllerTests`: nur Ctor/Mocks (Typ-Änderung).
- `TableToolbarHostTests.Marker_StehtUeberDerZaehlzeile`: `Picking/Index` entfernt, da Zählzeile „Sub-FAs“ entfällt (AK 10, gewollt).
- `PickingRepositoryTests` gelöscht (Repository entfernt, Antwort 11). Keine Kategorie „Sonstiges“.

### 6. Checkliste CLAUDE.md / ADR 0020
- Migration + `SQL/133` idempotent (Guards, History eigener Batch), FreshInstall an beiden Stellen: ja. `SQL/AgentJobs` nicht betroffen: ja.
- Audit-Felder: in Workflow/Transfer/Bereichen gesetzt: ja. Einmal-Skript unter `SQL/Einmalig/` (ADR 0015): ja.
- Version/Changelog/Hilfe/TS-120 + Testindex: ja; Brain-Changelog/feature-map: fehlt (Q12).
- ADR 0020 Maske Kommissionierbereiche: Regeltyp Auswahl, Wert aus Stamm, Server-Prüfung, Wirkungstexte je Feld vorhanden; Server-Fehler beim Standardwechsel (Q2). Einstellungstexte KommissionierTage/LeitstandAktiv/Zuweisung nachgezogen.
- Hinweis Betrieb: `sqlcmd` ohne `-I` scheitert an UPDATEs auf `PickingAreas` (gefilterter Index, QUOTED_IDENTIFIER) – bei Hand-SQL `-I` setzen.

### 7. DEV-Testlauf (AKE http://localhost:8081, IDEAL http://localhost:8082)
| Schritt | Ergebnis | Status |
|---|---|---|
| 120.1 Bereiche: Glas als Standard | gespeichert, Lager abgelöst | OK |
| 120.1 Standard abwählen (Glas) | Meldung „Es muss immer einen Standardbereich geben …“ | OK |
| 120.1 Lager wieder Standard | HTTP 500 (Q2) | FEHLER |
| 120.1 Regel Artikel 87068574 → Glas, dann → Lager | angelegt; zweite: „ist schon dem Bereich Glas zugeordnet“ | OK |
| 120.1 Wert nicht im Stamm | nur per manipuliertem POST möglich | NICHT MOEGLICH (Unit grün) |
| 120.2 Nachziehen alle | 2 Aufträge Lager/Glas; zweiter Lauf legt nichts an | OK |
| 120.2 Bulk-Freigabe mit FA ohne Stückliste, Sub-FA (IDEAL), ohne Leitstand | nicht nachgestellt (DEV-Daten; Leitstand-Schalter nicht umgestellt) | NICHT MOEGLICH |
| 120.3/120.4/120.5 Abarbeiten, Teilmenge, Folgeauftrag | `/Picking/Auftrag/18` HTTP 500 (Q1) | FEHLER |
| 120.7 Prio ändern | Aufträge folgen | OK |
| 120.7 Freigabe zurücknehmen mit/ohne Fortschritt | storniert ohne, behält mit + Warnung | OK |
| 120.7 Kommissionierer ändern | Freigabe-/Zuweisungsdialog öffnete sich im Test nicht sichtbar | NICHT MOEGLICH |
| 120.8 Liste | Filterkarte, Spaltenfilter, Werkbank, „Nur meine“ OK; IDEAL `/Picking` lädt (leer) | OK |
| 120.9 Druck | Barcode und Kopf korrekt | OK |
| 120.10 Nummernkreis parallel | zwei gleichzeitige Sessions | NICHT MOEGLICH |
| 120.11 Altbestand | Skriptausführung verweigert (Berechtigung) | NICHT MOEGLICH |
| 120.12 Rollen | nur Admin-Konto | NICHT MOEGLICH |
| Regression TS-79/TS-80 | nicht ausgeführt (Blocker zuerst) | offen |
| Sage-Queue / FA-Feld an Bewegung (AK 12) | keine Umbuchung möglich (Q1); Unit `Teilmenge_MitKomma_…_KeinSageQueue` grün | NICHT MOEGLICH |

**DEV-Zustand danach:** AKE_WMS_DEV zurückgesetzt – Testaufträge KO-2026-00001/-00002 und Regel gelöscht, Lager wieder Standard
(per SQL, Q2), FA 2614027 wieder freigegeben (Prio 15, Kommissionierer „test“; `ModifiedBy = 'QA-Reset v1.88.0'`). Keine Lagerbewegungen,
keine SageBookingQueueItems erzeugt. Keine Schalter geändert. IDEAL_WMS_DEV unverändert (nur gelesen). Beide Sites laufen mit v1.88.0.

### Fehlerbericht
- **Was:** Auftragsseite nicht erreichbar (Q1, Blocker); Standardwechsel 500 (Q2, hoch).
- **Erster Fehler:** `InvalidOperationException: The view 'Auftrag' was not found` bei `GET /Picking/Auftrag/18`.
- **Vermutete Ursache:** `BomSeiteAsync` gibt `View(vm)` ohne View-Namen zurück; Q2: EF-Statement-Reihenfolge gegen gefilterten UNIQUE-Index.
- Nach dem Fix: Q1/Q2 beheben, Q3/Q4/Q7/Q8/Q12 mitnehmen, dann erneuter QA-Lauf (TS-120.3–120.5 auf DEV, Regression TS-79/TS-80).

### Manuelle Checkliste (offen, für den Menschen nach dem Fix)
1. TS-120.3–120.5 komplett (Umbuchung auf Kommissionierwagen inkl. Wagen-Konflikt, Teilmenge, Folgeauftrag, FA erledigt, `SageBookingQueueItems` leer, `StockMovements.ProductionOrderId` gesetzt).
2. TS-120.1 Standardwechsel in beide Richtungen.
3. TS-120.2 ohne Leitstand (Auswahlliste), Sub-FA IDEAL, FA ohne Stückliste.
4. TS-120.7 Kommissionierer ändern → Aufträge folgen.
5. TS-120.10 zwei Browser parallel freigeben.
6. TS-120.11 Altbestand auf Kopie der Produktiv-DB (Trockenlauf, Ausführung, Stichprobe, zweiter Lauf bricht ab) – Q11 vorher klären.
7. TS-120.12 Rollen `picking` / `masterdata_read`.
8. Regression TS-79, TS-80.
9. Testsystem-Lauf je Standort.

## Reparatur 1 nach QA v1.88.0 (Dev, 2026-11-08)

- **Q1 (Blocker):** `BomSeiteAsync` rendert explizit `View("Bom", vm)`; Guard-Test `Auftrag_RendertStuecklistenView_QaQ1`.
- **Q2:** Standardwechsel in zwei Speicherschritten (alten Standard aufheben + speichern, dann neuen setzen) – der gefilterte UNIQUE-Index
  `UX_PickingAreas_IsDefault` lässt nie zwei Standards zu, EF ordnet die UPDATEs nicht danach. (InMemory erzwingt den Index nicht → Manual-UAT.)
- **Q3:** Knopf „Auftrag abschließen …“ auf der Auftragsseite (öffnet denselben Dialog), auch ohne vorherige Umbuchung.
- **Q4:** `AuftraegeNachziehen` nimmt nur freigegebene, offene HauptFAs (Server), übersprungene markierte FAs werden gemeldet.
- **Q5/Q6:** leere Ist-Menge und doppelt gesendete Zeile → Fehlermeldung, nichts gebucht (Test `LeereIstMenge_UndDoppelteZeile_Abgelehnt`).
- **Q7:** `ColumnDefinitions.Picking` = Spalten der Auftragsliste. **Q8:** Knopf „Zuordnungsregeln“ `btn-light`. **Q9:** Druck zeigt Zeilenstatus als
  Text (`ZeilenStatusText`); „Gepickt“ bleibt wie bisher ein Zustand nur im Browser bis zur Umbuchung (Häkchen) – Auftrag wird mit der ersten
  Umbuchung „In Arbeit“ (Frage an Gerald, ob schon das Häkchen den Status setzen soll).
- **Q10 (Frage an Gerald):** AK 2 – ein zweiter Standard wird nicht abgewiesen, sondern löst den alten ab (eine Aktion statt zwei). Bestätigen.
- **Q11 (Deploy-Hinweis):** Nach dem Altbestand-Skript legt „Nachziehen“ für offene FAs mit Auftragsmenge ≠ 1 einen Restauftrag an (Altbestand-Soll je
  Stück, neues Soll hochgerechnet, Befund B1). AKE-DEV: 1 FA. Vor dem Produktivlauf entscheiden: Restauftrag gewollt (Mehrmenge wirklich nötig)
  oder solche FAs vorher manuell abschließen.
- **Q12:** Brain-Changelog/feature-map folgen nach Testbereit.

## QA Reparatur 1 v1.88.0 (qa-agent, 2026-11-07) – Ergebnis: Testbereit

Geprüft: Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, HEAD `1ed5c72c`, Arbeitsbaum sauber.
Review-Bereich `15a8e45f..1ed5c72c` (11 Dateien, +80/−22); Build/Tests über den ganzen Worktree. Status → **Testbereit**.

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (vorbestehend).
- `dotnet test`: IdealAkeWms.Tests **2.528 bestanden, 1 übersprungen, 0 Fehler** (gesamt 2.529, +2 neue Tests); IDEALAKEWMSService.Tests **506/506**, 0 übersprungen.

### 2. DEV-Deploy (sofort nach grünem Build)
- `standort: beide`. Erhöhte Shell (Administrator = True). `D:\Dev\scripts\dev-deploy.ps1 -Standort AKE` und `-Standort IDEAL`, `-Quelle <worktree>`:
  beide **Exit 0**, „HTTP 200, Fusszeile v1.88.0“ = `AppVersion` 1.88.0 (Web + Service). DEV-Absicherung lief je Standort.

### 3. Code-Review (Delta `15a8e45f..1ed5c72c`, selbst durchgeführt)
Geprüft: `PickingController` (BomSeiteAsync, AuftragDrucken), `PickingAreasController.Edit`, `PickingLeitstandController.AuftraegeNachziehen` +
`GetReleasedForPickingAsync` (HauptFA, freigegeben, nicht erledigt/storniert/IsDonePicking), `PickingTransferService` (Auswahlprüfung),
`ColumnDefinitions.Picking`, `PickingOrderListItem.ZeilenStatusText`, `Views/Picking/Bom.cshtml` (Abschluss-Knopf, Status-Modal, `set-status-btn`-Handler,
Umbuchungs-JS), `Views/PickingAreas/Index.cshtml`, `Views/Picking/PrintPicking.cshtml`, die zwei neuen Tests. Defektklassen: keine neue Einbettung in
`<script>` (nur `@(Model.Auftrag?.Id ?? 0)` als Zahl, vorbestehend), kein `hidden`+`d-*`, keine neue stille Ersatzwert-Logik (Q5 entfernt eine),
indizierte Bindung `items[i].…` unverändert, Regeln serverseitig (Q4–Q6).

| ID | Schwere | Befund | Fundstelle | Behandlung |
|---|---|---|---|---|
| Q1 | – | **behoben**, auf DEV belegt: `/Picking/Auftrag/19`, `/20`, `/21` rendern (HTTP 200). Guard-Test liest nur Quelltext (schwach, aber ausreichend). | `PickingController.cs:457` | erledigt |
| Q2 | – | **behoben**, auf DEV in beide Richtungen belegt (Glas Standard, dann Lager [kleinere Id] zurück – kein 500). | `PickingAreasController.Edit` | erledigt |
| Q3 | – | **behoben**, auf DEV: Folgeauftrag KO-00003 ohne Umbuchung über „Auftrag abschließen …“ → „Vollständig“ abgeschlossen. | `Bom.cshtml:473-476` | erledigt |
| Q4 | – | **behoben**, auf DEV per manipuliertem POST: nicht freigegebener FA 3046 (AKE) und Sub-FA 275 (IDEAL) → „1 markierte FA übersprungen“, 0 Aufträge. | `PickingLeitstandController.cs:504-511` | erledigt |
| Q5/Q6 | – | **behoben**, auf DEV per POST: leer → „Ist-Menge angeben.“, doppelte Zeile → „mehrfach ausgewählt“, je HTTP 400, 0 Bewegungen. | `PickingTransferService.cs:55-66` | erledigt |
| Q7, Q8 | – | behoben (Spaltenliste = View; `btn-light` auf Navy-Kopf). | `ColumnDefinitions.cs:188-204`, `PickingAreas/Index.cshtml:24` | erledigt |
| **Q13** | Niedrig | Standardwechsel speichert in **zwei** `SaveChangesAsync` ohne gemeinsame Transaktion. Scheitert der zweite Schritt, gibt es **keinen** Standardbereich (Zeilen ohne Regel hätten keinen Bereich). Wahrscheinlichkeit gering (gleiche Zeile, gleicher Request). Vorschlag aus QA v1.88.0 war „in einer Transaktion“. | `PickingAreasController.cs:74-88` | offen → Dev (nicht blockierend) |
| **Q14** | Niedrig | Q9-Reparatur ist im Druck **nicht sichtbar**: `PrintPicking.cshtml` hat keine Status-Spalte je Zeile (Pos/Artikel/Bezeichnung/Lagerplatz/Soll/Ist); `ZeilenStatusText` wird nur ins ViewModel geschrieben, nie gerendert. Mein Befund Q9 („Enum-Text im Druck“) war insoweit ungenau. AK 18 verlangt keinen Zeilenstatus → keine Fehlfunktion, aber toter Wert. | `PickingController.cs:657`, `PrintPicking.cshtml:170-190` | Gerald: Spalte gewünscht? sonst Zuweisung entfernen |
| Q15 | Hinweis | Auftragsseite (nicht lesend) zeigt unten „Zurück zur FA-Liste“ → `/ProductionOrders`; der Kommissionierer kommt von der Auftragsliste `/Picking`. Vorbestehender Knopf, im Auftragsmodus irreführend. | `Bom.cshtml:719-726` | Hinweis |
| Q16 | Hinweis | Übersprung-Meldung nennt „nicht freigegeben, erledigt oder storniert“ – eine Sub-FA wird mit derselben Begründung gemeldet. | `PickingLeitstandController.cs:511` | Hinweis |
| Q10, Q11 | – | unverändert Fragen an Gerald (zweiter Standard löst ab; Altbestand + B1 Restauftrag). | – | an Gerald |
| Q12 | Niedrig | Brain-Changelog v1.88.0 und `feature-map.md` fehlen weiter („folgen nach Testbereit“). TS-120 fehlte im Testindex – **von QA nachgetragen**. | Hauptcheckout `secondbrain/` | offen → Dev |

Vorbestehend, nicht durch diesen Zweig verursacht (nur vermerkt): Status-Modal-Kopf weiß auf `--ake-secondary` #43A6E2 ≈ 2,7:1 (unter WCAG AA, auch
für große Schrift); „Trotzdem buchen“ verkettet vorhandene FA-Texte, die schon `;` enthalten, erneut (Wagen 1: `2618052;2618052;2602108;…;2614027`) –
gleiche Logik wie `main`; Kopf-Filter `?filterText=` blendet Kindzeilen unter zugeklappten Baugruppen aus („9 Positionen“, Tabelle leer) – Spaltenfilter
nach „alle aufklappen“ funktioniert.

### 4. Beweisart je AK (Änderung gegenüber QA v1.88.0)
| AK | Beweis | Ergebnis |
|---|---|---|
| 2 | Unit + DEV | **OK** (Q2 behoben; Q10 bleibt Frage) |
| 11 | DEV (echte Model-Bindung, POST) | **OK**: Ist 0, −1, „abc“, 53 (> Soll 52), „52,5“, leer → je 400 mit Meldung, 0 Bewegungen; fremde Zeile (Glas-Auftrag) → „gehört nicht zu Auftrag“ |
| 12 | DEV (Browser + SQL) | **OK**: Bewegungen 19393/19394/19395 `MovementType=2`, `ProductionOrderId=363`, Text = FA-Liste des Wagens; `SageBookingQueueItems WHERE StockMovementId>19392` = 0 |
| 13 | DEV | **OK**: nach Glas „Vollständig“ `IsDonePicking=1`, Statustext „abgeschlossen“, FA aus der Auftragsliste verschwunden |
| 14 | DEV | **OK**: Zeile Soll 8 / Ist 3 → Folgeauftrag KO-2026-00003 (Bezug KO-00001, Bemerkung „Rest aus KO-2026-00001“, Soll 5, 148 Zeilen); FA „teilkommissioniert“, `IsDonePicking=0` |
| 18 | DEV | OK (Druck KO-00001: Kopf, Status „Teilweise abgeschlossen“, Code-128, Soll/Ist/Lagerplatz) |
| übrige | wie QA v1.88.0 | unverändert |

### 5. Geänderte bestehende Tests
Keine geändert; nur zwei neue Tests (`Auftrag_RendertStuecklistenView_QaQ1`, `LeereIstMenge_UndDoppelteZeile_Abgelehnt`).

### 6. Checkliste CLAUDE.md / ADR 0020
Delta ohne Schema-/Einstellungsänderung. Testindex TS-120 nachgetragen (QA). Brain-Changelog/feature-map offen (Q12). Deploy-Abschnitt final.

### 7. DEV-Testlauf (AKE http://localhost:8081, IDEAL http://localhost:8082)
| Schritt | Ergebnis | Status |
|---|---|---|
| Q2 / 120.1 Standard Glas, dann Lager zurück (Maske) | beide gespeichert, genau ein Standard, kein 500 | OK |
| Regel Artikel 87068574 → Glas (Maske) | angelegt (Regel-Id 2) | OK |
| Q4 Nachziehen nicht freigegebener FA 3046 (POST) | übersprungen + Meldung, 0 Aufträge | OK |
| 120.2 Nachziehen FA 363 (2614027) | KO-2026-00001 Lager (149 Z.) + KO-2026-00002 Glas (1 Z.), Prio 15, Kommissionierer „test“ | OK |
| 120.4 ungültige Ist-Mengen (POST, 8 Fälle) | alle 400, nichts gebucht | OK |
| Wagen-Konflikt Wagen 2 → Dialog „Kommissionierwagen belegt“ (2616156) → „Trotzdem buchen“ | gebucht, Text `2616156;2614027` | OK |
| Wagen-Konflikt Wagen 1 → „Abbrechen“ | keine Bewegung | OK |
| 120.4 KO-00001: Pos 2.4 Ist = Soll 4, Pos 6.16.8 Ist 3 von 8 → Umbuchen (Quelle NAN, Negativbuchung erlaubt) → „Teilweise“ | „Teilweise abgeschlossen“, Folgeauftrag KO-00003, Meldung „Rest im Folgeauftrag KO-2026-00003“ | OK |
| Q3 + 120.5 KO-00003 über „Auftrag abschließen …“ → „Vollständig“ (ohne Umbuchung) | abgeschlossen, 148 Zeilen „Entfallen“; Glas offen → FA **nicht** erledigt („teilkommissioniert“) | OK |
| 120.3 KO-00002 Glas: Ist = Soll 2 → Wagen 1 („Trotzdem buchen“) → „Vollständig“ | Bewegung mit FA-Id 363, kein Sage-Queue-Eintrag, `IsDonePicking=1`, „abgeschlossen“, `/Picking` leer | OK |
| 120.9 Druck KO-00001 | Kopf/Barcode/Soll/Ist korrekt; kein Zeilenstatus (Q14) | OK |
| Sichtprüfung Auftragsseite | Kopf „Kommissionierauftrag KO-… Bereich … Status …“ als Text (nicht nur Farbe); Ist-Feld unter dem Soll, vorbelegt, ~175 px breit; gepickte Zeile grün **und** Haken; neuer Knopf `btn-outline-primary` auf Weiß (AA ok); Folgeauftrag zeigt „Rest aus KO-…“. Modal-Kopf siehe vorbestehend | OK |
| TS-79 IDEAL: `/Picking/Bom/284` (HauptFA 1060414) | lesend, Kopfkarte „Noch keine Kommissionieraufträge“, Komm.-Ziel-Filter mit „Alle Ziele (nur kommissionier-relevant)“ | OK |
| TS-79 IDEAL: `/Picking/Bom/275` (Sub-FA) | Umleitung + „Kommissionierung erfolgt nur am HauptFA 1060414.“ | OK |
| TS-79 IDEAL: Nachziehen Sub-FA 275 (POST) | übersprungen, 0 Aufträge | OK |
| TS-80 IDEAL: `/FaHierarchyKommissionierListen/Summiert` | lädt, gruppiert je HauptFA, Summe Sollmenge | OK (Smoke; Summenvergleich/PDF am Testsystem) |
| 120.7 Kommissionierer ändern, 120.10 parallel, 120.11 Altbestand, 120.12 Rollen, 120.2 ohne Leitstand / FA ohne Stückliste | nicht nachgestellt (Schalter, zweite Session, Produktivkopie, nur Admin-Konto) | NICHT MOEGLICH |

Zwischenfall: Ein Klick auf „Gepickte Artikel umbuchen“ ohne erkannte Markierung löste den Browser-Hinweis „Keine Artikel zum Umbuchen ausgewählt“
(`alert`) aus; kein Request an den Server (Log ohne POST), Tab geschlossen, im neuen Tab wiederholt. Kein Datenzustand betroffen.

**DEV-Zustand danach (dokumentiert, nicht zurückgesetzt – dient als Beleg):**
- AKE_WMS_DEV: Regel `PickingAreaRules.Id=2` (Artikel 87068574 → Glas); Aufträge KO-2026-00001 (Teilweise abgeschlossen), -00002 (Abgeschlossen),
  -00003 (Abgeschlossen) zu FA 363/2614027; Lagerbewegungen **19393, 19394** (NAN → Wagen 2) und **19395** (NAN → Wagen 1), Benutzer Gerald Weichbold;
  FA 363 `IsDonePicking=1`, Statustext „abgeschlossen“ (vorher freigegeben, offen). Lager ist wieder Standardbereich. Keine `SageBookingQueueItems` neu.
- IDEAL_WMS_DEV: unverändert (nur gelesen, Nachziehen-POST ohne Wirkung).
- Keine Schalter geändert (`SageLagerbuchung*`, `Sync:*`, ErrorNotification, Meldebestand unberührt); Einmal-Skript nicht ausgeführt. Beide Sites laufen v1.88.0.
- Rücksetzen bei Bedarf: Bewegungen 19393–19395, Aufträge Id 19–21 samt Zeilen und Regel 2 löschen, FA 363 `IsDonePicking=0`/Statustext leer.

### Manuelle Checkliste (offen, für den Menschen)
1. Testsystem-Lauf je Standort (AKE, IDEAL) mit SQL/133 nach Deploy-Reihenfolge.
2. TS-120.3–120.5 am Testsystem mit echtem Bestand auf buchbaren Plätzen (auf DEV nur über NAN/Negativbuchung möglich).
3. TS-120.2 ohne Leitstand (Auswahlliste), FA ohne Stückliste, Bulk-Freigabe mit Sammelmeldung.
4. TS-120.7 Kommissionierer ändern → Aufträge folgen.
5. TS-120.10 zwei Browser parallel freigeben (Nummernkreis, SQL Server).
6. TS-120.11 Altbestand auf Kopie der Produktiv-DB – Q11 vorher entscheiden.
7. TS-120.12 Rollen `picking` / `masterdata_read`.
8. TS-80 vollständig am IDEAL-Testsystem (Summen, PDF/Druck nur sichtbare Spalten, TS-80.1 Einheiten-Prüfschritt).
9. Entscheidungen: Q10 (zweiter Standard löst ab), Q11 (Altbestand + B1), Q14 (Zeilenstatus im Druck), Q9 (Häkchen = „In Arbeit“?).
10. Dev-Nacharbeit, nicht blockierend: Q13 (Transaktion Standardwechsel), Q12 (Brain-Changelog, feature-map).

## Nachtrag Restbefunde nach QA Reparatur 1 (Dev, 2026-11-08)

- **Q13:** Standardwechsel beide Speicherschritte in einer Transaktion (`IPickingAreaRepository.InTransactionAsync`).
- **Q14:** Druck mit Spalte „Status“ (Zeilenstatus als Text). **Q15:** Auftragsseite „Zurück zur Kommissionierung“. **Q16:** Meldetext nennt Sub-FA.
- Q9/Q10/Q11 bleiben Fragen an Gerald.


## QA Restbefunde (qa-agent, 2026-11-07) – Delta-Commit d7180d63 (Diff 1ed5c72c..d7180d63)

**Ergebnis: Testbereit bleibt – ja.** Nichts Blockierendes.

**Build und Tests (Worktree, Stand d7180d63):** `dotnet build IdealAkeWms.slnx` 0 Fehler. `dotnet test`: IdealAkeWms.Tests 2528 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 506 / 0 / 0.

**DEV-Deploy (standort: beide):** AKE und IDEAL aus dem Worktree, jeweils Exit 0, Fußzeile v1.88.0 = AppVersion.

**Review des Deltas (C2-Anteil):** `PickingAreasController.Edit` + `IPickingAreaRepository/PickingAreaRepository.InTransactionAsync`, `Views/Picking/PrintPicking.cshtml`, `Views/Picking/Bom.cshtml`, `PickingLeitstandController.AuftraegeNachziehen`.
- Q13: beide `SaveChangesAsync` in einer `BeginTransactionAsync`; kein `EnableRetryOnFailure` im Projekt (Grep `EnableRetryOnFailure|ExecutionStrategy` leer), daher keine Konflikte mit einer Ausführungsstrategie. Bei Ausnahme im zweiten Schritt wird ohne Commit verworfen → alter Standard bleibt. InMemory-Zweig führt die Aktion ohne Transaktion aus. Der gefilterte UNIQUE-Index ist InMemory nicht beweisbar → Manual-UAT, auf DEV erledigt (unten).
- Q14: `colspan` auf 7 nachgezogen; Statustext aus `PickingOrderListItem.ZeilenStatusText` (eine Quelle).
- Q15: Knopf nur im interaktiven Zweig (`!ReadOnly`), der ReadOnly-Zweig hatte schon „Zurück zur Kommissionierung“.
- Q16: reine Textänderung.
Keine geänderten Bestandstests im Delta.

| Schritt (DEV AKE, http://localhost:8081) | Ergebnis | |
|---|---|---|
| Q13a: Bereich „Glas“ bearbeiten → Standardbereich → Speichern | „Bereich „Glas“ gespeichert.“, Liste zeigt nur „Glas“ als Standard | OK |
| Q13b: Bereich „Lager“ zurück auf Standard | gespeichert; SQL `PickingAreas`: genau ein `IsDefault=1` (Lager), Audit `ModifiedAt/By` an beiden gesetzt | OK |
| Q14: `/Picking/AuftragDrucken/19` | Spalte „Status“ mit Texten „umgebucht“ / „in Folgeauftrag“ | OK |
| Q15: `/Picking/Auftrag/20` interaktiv (Status für den Test kurz auf InArbeit gesetzt, danach auf 40 zurückgesetzt) | Knopf „Zurück zur Kommissionierung“ → `/Picking` | OK |
| Q16: Sammelmeldung mit markierter Sub-FA | nur im Code geprüft (Text „… erledigt, storniert oder Sub-FA“) | NICHT MOEGLICH ohne Aufträge anzulegen – für den Menschen |

DEV-Testdaten: Standardbereich wieder „Lager“ (Ausgangszustand); `PickingOrders.Id=20` Status 40 wiederhergestellt.

**Befunde:**
- **R-1 (Doku, nicht blockierend):** `docs/TESTSZENARIEN.md` TS-120.9 nennt im Druck nur „Soll/Ist/Lagerplatz“ – Spalte „Status“ (Q14) ergänzen; Q15/Q16 haben keine eigene Zeile (in TS-120.2/120.3 als Erwartung ergänzen). Beim nächsten Commit im Worktree mitnehmen.

**Für den Menschen offen:** 1. Leitstand: HauptFA + Sub-FA markieren → „Fehlende Kommissionieraufträge erzeugen“ → Warnung „1 markierte FA übersprungen (nicht freigegeben, erledigt, storniert oder Sub-FA).“ 2. Standardwechsel in beide Richtungen auf dem Testsystem. 3. Ausdruck eines offenen Auftrags auf Papier (Spaltenbreite mit Status). 4. Q9/Q10/Q11 bleiben Fragen an Gerald.

## Entscheidungen Q9–Q11, Q14 (Gerald 2026-11-08 07:46) – Nachbesserung im nächsten Lauf
- **Q9:** Auftrag wird „In Arbeit“ **ab der ersten Umbuchung** (wie gebaut). Zeilenstatus nach dem Kommissionieren einheitlich
  **„Gepickt“** statt „Übertragen“ anzeigen (Liste, Auftragsseite, Druck); interner Enum-Wert darf bleiben, nur Anzeigetext.
- **Q10:** **Ablehnen statt ablösen** (wie AK 2 ursprünglich): Wer einen zweiten Bereich als Standard speichert, bekommt eine
  Validierungsmeldung „Es gibt bereits einen Standardbereich (…). Zuerst dort abwählen.“; Hilfetext entsprechend ändern; Transaktion
  aus Q13 bleibt für den Wechsel über „Standard abwählen + neu setzen“.
- **Q11:** Altbestand-Migration erzeugt für offene FAs mit Auftragsmenge ≠ 1 und vorhandenem Fortschritt **keinen automatischen
  Rest-Auftrag**. Das Skript schreibt diese FAs in eine Liste (Ergebnisdatei bzw. Protokoll mit FA, Menge, Fortschritt); der Leitstand
  prüft je FA und erzeugt fehlende Aufträge bei Bedarf per Knopf. Auch „Fehlende Kommissionieraufträge erzeugen“ überspringt diese FAs,
  bis sie im Leitstand bestätigt sind (Kennzeichen am FA bzw. Auftrag „Altbestand geprüft“) – Lösung im Lauf begründen.
- **Q14:** erledigt (Spalte „Status“ im Druck, Nachtrag Dev 2026-11-08).

## Umsetzung Nachbesserung v1.89.0 (Dev 2026-11-08, Auftrag Gerald; ersetzt die Statusprüfung)

Commits `bc8be58c`, `53858c6c`. SQL/141 (erste freie nach 140).
- **Q9:** `PickingOrderListItem.ZeilenStatusText`: `Gepickt` und `Umgebucht` → „gepickt“ (Enum unverändert); `Bom.cshtml` nutzt dieselbe Funktion
  statt eines eigenen `switch`. Auslegung: Geralds „Übertragen“ = die bisherige Anzeige nach dem Kommissionieren („umgebucht“); der Enum-Wert
  `Uebertragen` (Rest im Folgeauftrag) bleibt „in Folgeauftrag“.
- **Q10:** `PickingAreasController.Uebernehmen`: zweiter Standard → „Es gibt bereits einen Standardbereich („X“). Zuerst dort abwählen.“;
  Abwählen erlaubt, danach Warnung „Es gibt jetzt keinen aktiven Standardbereich …“. Ein Speichern = eine Transaktion (`SaveChanges`); die
  Zwei-Schritt-Logik (Q2) und `IPickingAreaRepository.InTransactionAsync` (Q13) sind entfernt, weil nie zwei Standards entstehen.
  Hilfetexte Edit + Hilfe.
- **Q11:** `PickingOrders.AltbestandPruefen` (Migration `20261108071811_AddPickingOrderAltbestandPruefen`, SQL/141, FreshInstall an zwei
  Stellen). Einmal-Skript: Guard „SQL/141 fehlt“, setzt das Kennzeichen für offene FAs mit Auftragsmenge ≠ 1, Liste „Altbestand pruefen“ im
  Trockenlauf und in der Ausführung. `PickingOrderWorkflow.ErzeugeOffeneAsync` (einziger Erzeugungsweg: Freigabe, Nachziehen, Öffnen ohne
  Leitstand) überspringt solche FAs mit Meldung; `AltbestandGeprueftAsync` + Leitstand-Aktion `AltbestandGeprueft` (Badge „⚠ Altbestand
  prüfen“ + Knopf in der Statusspalte). Begründung der Lösung: Kennzeichen am Auftrag statt am FA, weil das Skript nur Aufträge anlegt und der
  Leitstand die Aufträge des FA ohnehin lädt; keine neue Tabelle.
- DEV: SQL/141 je zweimal auf AKE_WMS_DEV und IDEAL_WMS_DEV; Trockenlauf AKE-DEV: Liste = 1 FA (2616156, Menge 3, 4 umgebucht, 1 gepickt).
- Tests: `KommissionierauftraegeE1Tests` (Q10 abgelehnt/abwählen/setzen), `PickingOrderWorkflowTests` (Q9-Texte, Q11 überspringen →
  bestätigen → Rest). TS-120.1/120.9/120.13 angepasst, TS-120.14 neu. Build 0 Fehler; Tests Dienst 506, Web 2534/1.

## QA v1.89.0 (2026-11-08) – Ergebnis: Testbereit

qa-agent. Prüfbereich: Commits `bc8be58c`, `53858c6c` (Basis `aeba657b`). Build und Tests liefen über den ganzen Worktree (HEAD `2efdb023`, v1.94.0).

### 1. Build und Tests (selbst ausgeführt)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen.
- `dotnet test`: IdealAkeWms.Tests 2542 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 507 / 0 / 0.

### 2. DEV-Deploy (standort: beide)
- AKE-DEV: Der Aufrufer hat mit `dev-deploy.ps1` aus dem Worktree deployt (HEAD unverändert, Arbeitsbaum sauber). Fußzeile im QA-Lauf gemessen: v1.94.0.
- IDEAL-DEV: im QA-Lauf `dev-deploy.ps1 -Standort IDEAL -Quelle <worktree>` ausgeführt. Ergebnis: Exit 0, „HTTP 200, Fusszeile v1.94.0“, DEV-Absicherung angewendet, Dienst gestartet.
- SQL/141 auf beiden DEV-DBs: `COL_LENGTH('dbo.PickingOrders','AltbestandPruefen')` = 1, MigrationId `20261108071811_AddPickingOrderAltbestandPruefen` eingetragen.

### 3. Code-Review (selbst, in diesem Lauf)
Gelesen:
- `PickingAreasController.Edit/Uebernehmen`
- `PickingLeitstandController.AltbestandGeprueft`
- `PickingOrderWorkflow.ErzeugeOffeneAsync/AltbestandGeprueftAsync`
- `_PickingLeitstandRow.cshtml`: Das neue Formular steckt in keinem anderen `<form>`; die Formulare in `Index.cshtml` Z. 70–99 sind getrennt.
- `PickingListViewModel.ZeilenStatusText` und `Bom.cshtml`
- Migration, SQL/141, FreshInstall (Spalte Z. 2782 und History-Block), Einmal-Skript

Ergebnisse:
- **Q10:** Die Prüfung `e.IsDefault && anderer Standard` steht in `Uebernehmen`, einem Ort für Anlegen und Bearbeiten (Anlegen erzwingt ohnehin `IsDefault=false`). Die Warnung nach dem Speichern liest `alle`, das die getrackte `area` enthält. Sie zeigt also den neuen Zustand. `IPickingAreaRepository.InTransactionAsync` ist restlos entfernt.
- **Q11:** `ErzeugeOffeneAsync` ist der einzige Erzeugungsweg. Das Kennzeichen sitzt am Auftrag. Der Leitstand lädt alle Aufträge des FA (`GetByProductionOrdersAsync` ohne Statusfilter), deshalb erscheint das Badge auch bei einem abgeschlossenen Altbestand-Auftrag. Die Aktion hat `RequireLeitstandAccess`, Antiforgery und `Url.IsLocalUrl`; die Audit-Felder setzt `AuditSetzen`.
- **Skript:** Guard „SQL/141 fehlt“ vorhanden. Die Liste erscheint im Trockenlauf und in der Ausführung. Das Kennzeichen wird nur bei `FaErledigt = 0 AND MengeUngleich1 = 1` gesetzt.
- **Q9:** Der Text steht an einem Ort (`ZeilenStatusText`), `Bom.cshtml` nutzt ihn.

**Befunde: keine.** Hinweis, kein Befund: „Zweiter Klick: nichts geändert“ aus TS-120.14 ist über die Oberfläche nur mit einem veralteten Tab erreichbar, weil der Knopf nach der Bestätigung verschwindet.

### 4. Beweisart je Entscheidung
| Punkt | Beweis |
|---|---|
| Q9 Text „gepickt“ | automatisch (`PickingOrderWorkflowTests`) + DEV-Druck |
| Q10 ablehnen/abwählen/setzen | automatisch (`KommissionierauftraegeE1Tests`, InMemory) + DEV-Maske |
| Q11 Erzeugung überspringt, Bestätigung löscht Kennzeichen | automatisch (`PickingOrderWorkflowTests`) + DEV-Leitstand |
| Q11 Einmal-Skript (raw SQL) | Manual-UAT; auf AKE-DEV in einer Transaktion mit ROLLBACK gelaufen |
| SQL/141, FreshInstall | Manual-UAT (DEV-DBs geprüft) |

### 5. Geänderte bestehende Tests
- `KommissionierauftraegeE1Tests`: `Bereich_NeuerStandardLoestAltenAb_AK2` heißt jetzt `Bereich_StandardWechsel_AbwaehlenDannSetzen_Q10`; die Erwartung in `Uebernehmen` ist angepasst. Das Verhalten ist absichtlich geändert (Entscheidung Q10).
- `PickingOrderWorkflowTests`: nur ergänzt.

### 6. Checkliste CLAUDE.md / ADR 0020
- Migration und `SQL/141` (COL_LENGTH-Guard, History in eigenem Batch): ja. FreshInstall an beiden Stellen: ja.
- AgentJobs nicht betroffen. Audit-Felder: ja.
- Version 1.89.0 im Commit, Anwender-Changelog v1.89.0 und Brain-Changelog `2026-11-08-v1-89-0-kommissionierauftraege-nachbesserung`: vorhanden.
- TS-120.1/.9/.13/.14 stehen in `docs/TESTSZENARIEN.md`; die Indexzeile TS-120 ist vorhanden.
- Keine neue Einstellung, ADR 0020 entfällt. Das Einmal-Skript liegt in `SQL/Einmalig/` (ADR 0015).

### 7. DEV-Testlauf (AKE http://localhost:8081)
| Schritt | Ergebnis | |
|---|---|---|
| 120.1 Glas als zweiten Standard speichern | Feldfehler „Es gibt bereits einen Standardbereich („Lager“). Zuerst dort abwählen.“; `ModifiedAt` von Glas unverändert | OK |
| 120.1 Lager abwählen | „Bereich „Lager“ gespeichert.“ + Warnung „Es gibt jetzt keinen aktiven Standardbereich …“ | OK |
| 120.1/120.13 Lager wieder setzen | ohne Fehler; SQL: genau ein `IsDefault=1` (Lager) | OK |
| 120.9 `/Picking/AuftragDrucken/19` | Spalte Status zeigt „gepickt“ (2 Zeilen), sonst „in Folgeauftrag“; kein „umgebucht“ mehr | OK |
| 120.9 Auftragsseite interaktiv | Auftrag 20 ist abgeschlossen (ReadOnly-Zweig); nur Code geprüft (`Bom.cshtml` nutzt `ZeilenStatusText`) | NICHT MOEGLICH ohne Statusänderung, für den Menschen |
| 120.14 Skript Trockenlauf AKE_WMS_DEV | 15 FAs mit Fortschritt (14 abgeschlossen, 1 in Arbeit). Liste „Altbestand pruefen“: FA 2616156, Menge 3, 4 umgebucht, 1 gepickt. Ausgabe „TROCKENLAUF: nichts geaendert“ | OK |
| 120.14 Skript mit `@Trockenlauf = 0` in äußerer Transaktion | 15 Aufträge / 54 Zeilen angelegt. Status 20: 1 Auftrag, davon AltbestandPruefen 1 (KO-2026-00008 zu FA 2616156). Status 40: 14 Aufträge, davon 0. Nach `ROLLBACK`: 0 Aufträge mit `CreatedBy = Migration-KO-Altbestand` | OK |
| 120.14 Negativ „SQL/141 fehlt“ | Beide DEV-DBs haben die Spalte; nur Code geprüft | NICHT MOEGLICH, für den Menschen (Kopie ohne SQL/141) |
| 120.14 Leitstand: Badge und Knopf | Testdatensatz `PickingOrders.Id=21` (FA 2614027) per SQL auf `AltbestandPruefen=1` gesetzt. Der Leitstand zeigt „⚠ Altbestand prüfen“ und „Altbestand geprüft“ | OK |
| 120.14 „Altbestand geprüft“ klicken | Rücksprung auf den gefilterten Leitstand, Badge weg. SQL: `AltbestandPruefen=0`; `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` gesetzt | OK |
| 120.14 „Fehlende … erzeugen“ überspringt den FA | FA 2614027 ist erledigt; nur automatisch belegt | NICHT MOEGLICH ohne offenen Altbestand-FA, für den Menschen |

Zurückgesetzt: `PickingOrders.Id=21` wieder `AltbestandPruefen=0`, `ModifiedAt` auf 2026-11-07 22:26:44. Der Standardbereich ist wieder „Lager“ (Ausgangszustand, nur `ModifiedAt` ist neu).
Screenshot Leitstand (temporär): `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude-chrome-screenshots-cF2Dyv/screenshot-1794124543737-9.jpg`.

### Deploy (aus dem echten Diff)
- web: ja · service: ja (nur Version) · migration: ja (`20261108071811_AddPickingOrderAltbestandPruefen` / SQL/141).
- Reihenfolge:
  1. DB-Backup.
  2. Web stoppen.
  3. SQL/133 (falls noch nicht eingespielt), dann SQL/141.
  4. Einmal-Skript im Trockenlauf; Liste ins Deploy-Protokoll.
  5. Freigabe, dann `@Trockenlauf = 0`.
  6. Web und Dienst starten.
  7. Leitstand „Fehlende … erzeugen“.
  8. Je FA der Liste prüfen, „Altbestand geprüft“, erneut erzeugen.
```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
```
Nach dem Merge nur dann aus main neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen kombiniert hat.

### Manuelle Checkliste (offen, für den Menschen)
1. Testsystem: Altbestand-Skript im Trockenlauf laufen lassen und die Liste mit der Erwartung abgleichen, dann ausführen.
2. Leitstand: FA aus der Liste markieren, „Fehlende Kommissionieraufträge erzeugen“. Erwartet: Warnung „FA …: Altbestand prüfen …“, kein Auftrag angelegt.
3. „Altbestand geprüft“, dann erneut erzeugen. Erwartet: Der Rest-Auftrag entsteht.
4. Negativ: Skript auf einer Kopie ohne SQL/141. Erwartet: Abbruch „SQL/141 fehlt“.
5. Auftragsseite eines laufenden Auftrags: Zeilenstatus „gepickt“.
6. Standardwechsel Lager → Glas → Lager über die Maske (abwählen, dann setzen) auf dem Testsystem.

## Nachbesserung Abnahme 2026-11-08 (v1.95.0, Ausnahme Schranke 1 Gerald)

Quelle: Vorab-Abnahme auf DEV, [[2026-11-08-buendel-dev-v1-94-browser-testbericht]], Befund 4 (B7.2): Freigabe von FA 2629001 nicht möglich, weil kein aktiver Benutzer `IsPicker` ist –
die Auswahl war leer, „Freigeben“ gesperrt, ohne Erklärung.
- Neuer Partial `Views/PickingLeitstand/_KeinKommissioniererHinweis.cshtml` (gelb, „Kein Benutzer als Kommissionierer gekennzeichnet (Benutzer →
  Kommissionierer)“, Link `alert-link` zur Benutzerverwaltung, „nur Admin“) in allen drei Dialogen (Freigabe, Kommissionierer ändern,
  Sammel-Freigabe), nur bei leerer Liste.
- Test `AbnahmeNachbesserungV195Tests.Leitstand_KeinKommissionierer_HinweisMitLink`. TS-120.15. Commit `897c5466`.

## QA Nachbesserung Abnahme v1.95.0 (2026-11-08) – Ergebnis: Testbereit

qa-agent. Prüfbereich: `git diff 9c9046c4..897c5466` im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`.

### 1. Build und Tests (selbst ausgeführt, Worktree HEAD `897c5466`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler (28 Warnungen, Bestand).
- `dotnet test`: IdealAkeWms.Tests 2571 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 507 / 0 / 0.
- DEV nicht neu deployt (Auftrag: DEV steht bereits auf v1.95.0, `dev-deploy.ps1` würde die fünf AKE-Sync-Schalter abschalten). Fußzeile gemessen: AKE `localhost:8081` „IDEAL-AKE WMS v1.95.0“; IDEAL `localhost:8082` zeigt den Stand v1.95.0 (Hilfe-Text aus diesem Commit sichtbar).
- Sync-Schalter AKE-DEV am Ende unverändert **true**: `Sync:ArticlesEnabled`, `Sync:ArtikelLieferantEnabled`, `Sync:KreditorenEnabled`, `Sync:DebitorenEnabled`, `Sync:EinkaufBestellungenEnabled`.

### 2. Code-Review (Diff `9c9046c4..897c5466`, 13 Dateien, selbst gelesen)
Geprüft: `ColumnDefinitions.PurchaseOrders` gegen `th[data-col-key]` und `#column-config` (13 Schlüssel, gleiche Reihenfolge, 13 `td` je Zeile), Muster Kreditoren (`Views/Creditors/Index.cshtml:104-200`), `column-preferences.js` (Reset per `DELETE` ohne `confirm`), `_FaLink.cshtml` und `StockApiController.FaLinkAsync` gegen `ProductionOrderRepository.cs:141-142` (`!showDone` blendet `IsDone`, `IsCancelled`, `IsDonePicking` aus), alle Nutzer des Partials (`StockMovements/Index.cshtml:140`, `StockOverview/Index.cshtml:83`), `PickingLeitstand/Index.cshtml:254-353` (`activePickers` nur im Block `PickerAssignmentEnabled`, `ViewBag.ActivePickers` aus `UserRepository.GetActivePickersAsync` = `IsActive && IsPicker`), Hilfe-Stellen, `AppSettingDefinitions.cs:99-100`. JSON-Blöcke statisch, kein `@Html.Raw`; keine `hidden`/`d-*`-Kombination, kein stiller Fallback, keine Parallel-Arrays, keine reine Client-Validierung.

**Befunde (Befund 4): keine.**
**Hinweis H-1 (kein Befund, Entscheidung Gerald):** Der Text „Kein Benutzer als Kommissionierer gekennzeichnet“ erscheint auch, wenn ein Benutzer `IsPicker` hat, aber inaktiv ist (`UserRepository.GetActivePickersAsync`: `IsActive && IsPicker`). Auf AKE-DEV ist genau das der Fall (Id 11 `gerald.berger`, `IsPicker=1`, `IsActive=0`). Wer dem Link folgt, findet den Haken dort schon gesetzt. Präziser: „Kein aktiver Benutzer als Kommissionierer gekennzeichnet“.

### 3. Beweisart
| Punkt | Beweis |
|---|---|
| Partial in allen drei Dialogen, nur bei leerer Liste | automatisch: `AbnahmeNachbesserungV195Tests.Leitstand_KeinKommissionierer_HinweisMitLink` (Quelltext, 3 Fundstellen) |
| Sichtbarkeit, Freigeben gesperrt, Kontrast | Manual-UAT, auf DEV erledigt |
| Gegenprobe „mit Kommissionierer kein Hinweis“ | Manual-UAT, offen (DEV-Daten laut Auftrag nicht geändert) |

### 4. Geänderte Bestandstests
Keine.

### 5. Checkliste
Keine Migration, keine Einstellung. TS-120.15 in `docs/TESTSZENARIEN.md`; Testindex Zeile 120 durch QA nachgezogen.

### 6. DEV-Testlauf AKE (`KommissionierungMitZuweisung=true`, `LeitstandAktiv=true`, kein aktiver Kommissionierer)
| Schritt | Ergebnis | |
|---|---|---|
| 120.15 Freigabe FA 2629001 (Knopf öffnet nur den Dialog) | gelbe Karte mit Text und Link „Zur Benutzerverwaltung“ (`href=/Users`), Auswahl leer, „Freigeben“ gesperrt; mit Abbrechen geschlossen | OK |
| 120.15 Sammel-Freigabe (1 markiert, „Markierte freigeben“) | gleiche Karte unter „1 Auftraege werden freigegeben“, „Freigeben“ gesperrt; mit Esc geschlossen | OK |
| 120.15 „Kommissionierer ändern“ | AKE-DEV hat keinen offenen, freigegebenen FA mit Kommissionierer; der Dialog lässt sich nicht öffnen. Im DOM steht die Karte im Dialog `changePickerModal` (Text + Link `/Users`). | Sichtprüfung NICHT MÖGLICH (Daten), Markup OK |
| Kontrast Link in der gelben Karte | `.alert-link` = `--bs-warning-text-emphasis` #664d03 auf `--bs-warning-bg-subtle` #fff3cd (Bootstrap 5.3.3, kein Override in `site.css`): **7,21:1** (≥ 4,5:1); fett und unterstrichen, Farbe nicht alleiniger Träger | OK |
| Keine Freigabe ausgelöst | Web-Log: kein `POST /PickingLeitstand…` während des Laufs (letzter um 08:55, vor dem Lauf) | OK |

Screenshots: `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude-chrome-screenshots-cF2Dyv/screenshot-1794133797239-41.jpg` (Freigabe-Dialog), `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude-chrome-screenshots-cF2Dyv/screenshot-1794133831507-42.png` (Zoom Karte), `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude-chrome-screenshots-cF2Dyv/screenshot-1794133860198-43.jpg` (Sammel-Freigabe).

### Deploy (aus dem echten Diff `9c9046c4..897c5466`)
web: ja · service: ja (nur Version) · migration: nein.
```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
```
Nach dem Merge nur dann aus main neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen kombiniert hat.

### Manuelle Checkliste (offen)
1. „Kommissionierer ändern“ bei leerer Liste am Bildschirm (braucht einen freigegebenen FA mit Kommissionierer, danach keinen aktiven Kommissionierer).
2. Gegenprobe: ein aktiver Kommissionierer – kein Hinweis in allen drei Dialogen.
3. Entscheidung H-1 (Text „kein aktiver Benutzer“).
4. Testsystem AKE und IDEAL: TS-120.15.
