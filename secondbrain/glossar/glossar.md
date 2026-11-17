---
type: glossar
updated: 2026-07-27
---
# Domaenen-Glossar

Sprachregel im Projekt: **Code und Variablen auf Englisch, UI-Texte auf Deutsch.** Dieses Glossar
uebersetzt zwischen beiden Welten. Wo ein Begriff im Code anders heisst als im UI, steht es dabei —
diese Asymmetrien sind meist bewusst.

## Auftrag und Produktion

| Begriff | Bedeutung |
|---|---|
| FA | Fertigungsauftrag (= Produktionsauftrag, Entitaet `ProductionOrder`). Historisch WA (Werkstattauftrag) — in v1.3.0 in der UI zu FA umbenannt. |
| WA | Werkstattauftrag — alter Name des FA. Lebt weiter in enaio-Feldnamen (`WaNummerTrimmed`) und im enaio-Dokumenttyp „Werkstattauftrag". |
| Sub-FA | Unterauftrag zu einer WA-Nummer. Auf der IDEAL-Linie koennen mehrere `ProductionOrders` dieselbe WA-Nummer tragen (`SubOrderNumber`). |
| Artikelnummer | **Geraete**-Artikelnummer. **Nicht** fuer Bauteil-Operationen verwenden. |
| Ressourcenummer | **Bauteil**-Artikelnummer — immer diese fuer Bauteil-Operationen. |
| Stueckliste / BOM | Bill of Materials — die Bauteile zu einem FA. Quelle: Sage-View, Fallback OSEON. |
| Baugruppe | Zusammengehoerige BOM-Teilmenge; im BOM-Baum eine Ebene. |
| Arbeitsgang | **Drei** verschiedene Dinge im Projekt: FA-Vorbau-AG (`WorkStep`), BDE-Arbeitsgang (`WorkOperation`) und OSEON-Arbeitsgang. Kontext beachten. Termin-Offsets je Kuerzel: siehe Arbeitsgang-Stammdaten. |
| Arbeitsgang-Stammdaten | Tabelle `OperationConfigs` (Klasse `OperationConfig`; bis v1.53.0 `OseonOperationConfigs`): je Arbeitsgang-Kuerzel Offset und „Fremdsystem-relevant“. Schluessel fuer OSEON-AG-Namen **und** WMS-`OperationNumber`. Pflege: BDE-Stammdaten › Arbeitsgaenge (nur Admin). [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] |
| Fremdsystem-relevant | UI-Begriff fuer `OperationConfig.IsExternalSystemRelevant` (frueher `IsOseonRelevant`). Wirkt derzeit **nur** in OSEON-Teileverfolgung und -Reporting. Das OSEON-ViewModel-Property `OseonOperationViewModel.IsOseonRelevant` heisst bewusst weiter so. |
| Bezugstermin | Termin, zu dem der Arbeitsgang-Offset zaehlt: OSEON Stanztermin (OSEON-Endtermin), IDEAL `FE_Termin`, AKE-BDE `ProductionDate`. |
| Offset (Arbeitstage) | `DueDateOffsetDays`: Arbeitstage (Mo–Fr ohne Feiertage) relativ zum Bezugstermin; kein Eintrag = 0. |
| FA-Vorbau-AG | UI-Label fuer den Vorbau-Arbeitsgang. Code/Entity/Route heissen weiterhin `WorkStep` / `/WorkSteps`. |
| VK / VL / VE / VT / VA | Die 5 statischen Vorbau-AG-Spalten im Leitstand (Filter-Keys `cooling`/`fan`/`electric`/`doors`/`superstructure`). Der Katalog ist erweiterbar, diese Spalten sind es nicht. |
| Werkbank | Produktionsarbeitsplatz (`ProductionWorkplace`) — wo gearbeitet wird. |
| Arbeitsplatz | `Workstation` — PC-/Drucker-Arbeitsplatz, admin-verwaltet. Nicht dasselbe wie Werkbank. |
| Lackierteil / Beschichtung | FAs mit zu beschichtenden Teilen (`HasCoatingParts`, `IsCoatingDone`). Erkennung ueber die Artikelkategorie in `LackierteilKategorieName`. |
| Beschichtungstermin | Berechneter Abhol-/Anliefertermin fuer die Beschichtung. Formel zentral in `CoatingDateCalculator.Compute`. |
| Kommissioniertermin | Arbeitstage vor dem Fertigungstermin (`KommissionierTage`, Default 4). |
| Vorkommissioniertermin | Tage vor dem Kommissioniertermin (`VorkommissionierTage`, Default 1). |
| Storniert | `ProductionOrder.IsCancelled` — der FA existiert in Sage nicht mehr, gesetzt von der FA-Reconciliation. Verhaelt sich wie „erledigt": raus aus allen offenen Sichten. |
| HauptFA | Der Kopf-Fertigungsauftrag einer IDEAL-Struktur (`FaHierarchyNode.HauptFA`, in `ProductionOrders` als `OrderNumber`). Alle Sub-FAs einer Struktur teilen ihn — er ist deshalb **nicht** eindeutig und taugt nicht als Zeilenschluessel. |
| Arbeitsbereich | Sage-seitige Zuordnung eines Struktur-Knotens zu einem Produktionsbereich (`FaHierarchyNode.Arbeitsbereich`, z. B. `K-02`, `S-01`). Wird bei IDEAL per Name auf die **Werkbank** abgebildet; Sage ist dabei fuehrend ([[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]). |
| Kombinationsgeraet | Ein HauptFA mit **mehreren** Auftragskoepfen (mehrere `FaHierarchyOrderInfo`-Zeilen, verschiedene Montage-Abteilungen). Kunde, Termine und Beschichter koennen je Variante abweichen — die Oberflaeche zeigt deshalb **alle** Varianten und waehlt nie still eine aus. |
| Konstruktions-Termin (KO) | `FaHierarchyOrderInfo.KO_Termin` — wann die Konstruktion fertig sein muss. **Nicht** der Kommissioniertermin; der Tooltip in der FA-Struktur behauptete das bis 2026-09-09 faelschlich. |
| Kuerzel-Familie (Werkbank) | Seit v1.63.0: alle Werkbaenke mit demselben `ArbeitsschrittCode` (getrimmt, Gross/Klein egal). Eine Werkbank sieht und bucht die offenen BDE-Arbeitsgaenge (`WorkOperation`) aller Familienmitglieder; ohne Kuerzel oder „nicht in Sage“ = nur sie selbst (AKE). Code: `WerkbankKuerzelFamilie`. [[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec]] |
| Bevorzugte Werkbank | Die Werkbank, an die die Struktur-Erkennung einen `WorkOperation` haengt, wenn ein Kuerzel an mehreren aktuellen Werkbaenken steht: BDE-aktive zuerst, dann kleinste Id (v1.63.0). Wirkt auf Tracking je Werkbank und den Namen, nicht auf das Stempeln. |
| Nicht in Sage (Werkbank) | `ProductionWorkplace.SageMissingSince`: die Sage-Nummer stand im genannten Lauf nicht mehr in der Werkbank-Quelle. Nie geloescht, zaehlt nicht fuer den Kuerzel-Abgleich, selbstheilend (v1.63.0). Nicht zu verwechseln mit `OperationConfig.SageMissingSince` (Arbeitsgang-Stammdaten) oder `ProductionOrder.SageMissingSince` (FA). |
| AG-Fertigmeldung (Werkbank-Option) | `ProductionWorkplace.BdeNurAgFertigmeldung`: am Terminal nur „Fertig melden“ mit Menge, ohne Start/Stop; Buchung mit Start = Ende (Dauer 0). Nicht zu verwechseln mit dem globalen `BdeNurFaMeldung` (FA-Ebene, mit Start/Stop), das Vorrang hat (v1.63.0). |
| FE-Termin | `FaHierarchyOrderInfo.FE_Termin` — Fertigstellungstermin. Speist bei IDEAL die Termin-Kaskade der FA-Liste (Fert.-Termin). |
| BG-Termin | Spalte der FA-Liste: der **Vorkommissioniertermin**, rueckwaerts aus dem Kommissioniertermin minus `VorkommissionierTage` gerechnet — je Werkbank ueberschreibbar (`ProductionWorkplace.OverridePrePickingDays`). Kein Rohfeld. |
| Komm. (Spalte) | Spalte der FA-Liste: der **berechnete** Kommissioniertermin (Fertigungstermin minus `KommissionierTage`), nicht der Kommissionier-**Status**. |
| Lack-T | Icon-Spalte der FA-Liste fuer `HasCoatingParts`/`IsCoatingDone`. Bei AKE aus der Artikelkategorie erkannt, bei IDEAL aus `FaHierarchyNode.Beschichtet` abgeleitet — **nie beide zugleich**, der Master entscheidet. |

## Prozesse und Module

| Begriff | Bedeutung |
|---|---|
| Kommissionierung / Picking | Bereitstellen von Bauteilen zu einem FA (Umbuchung auf einen Kommissionierwagen). |
| Kommissionierwagen | Lagerplatz mit `IsPickingTransport` — Transportmittel, kein Bestandsort. Aus Verfuegbarkeits- und Meldebestandsrechnung ausgeschlossen. |
| Kommissionierauftrag (KO-Nummer) | Beleg `PickingOrder` mit Nummer `KO-JJJJ-NNNNN` je FA und Kommissionierbereich (seit v1.88.0); Zeilen `PickingOrderLine` mit Soll/Ist, Folgeauftrag bei „teilweise“. Nicht zu verwechseln mit dem Konstruktions-Termin „KO“. [[0021-kommissionierauftrag-als-beleg-neben-der-stueckliste]] |
| Kommissionierbereich | Stammdaten `PickingArea` (Lager, Glas, Vorkommissionierung …); Regeln `PickingAreaRule` (Artikel → Kommissionierziel → Lagerort → Artikelgruppe → Standard) ordnen Stücklistenzeilen zu. |
| Leitstand | Steuerungssicht: FAs fuer die Kommissionierung freigeben, priorisieren, Picker zuweisen; zeigt zusaetzlich die VK-VA-Erledigt-Status. Eigenes Hauptmenue seit v1.14.0, Gate `LeitstandAktiv`. |
| Vorbau | Vormontage von Baugruppen vor der Endmontage. Traegt die **FA-Abarbeitungsliste** (`FaWorklist`) — arbeitsgang-zentriert ueber alle Werkbaenke, Werkbank nur als Zusatzfilter. Rolle `vorbau`. |
| FA-Vervollstaendigung | Planer-Sicht: je FA festlegen, welche Vorbau-AGs und Merkmale gebraucht werden (`FaCompletion`). Gate `FaCompletionAktiv`, Rolle `fa_completion`. Schreibt `IsSpecComplete`. |
| ALLGEMEIN | Pseudo-Reiter in der FA-Vervollstaendigung fuer die read-only FA-Zusatzinfos. Der Code `ALLGEMEIN` ist im WorkSteps-Katalog reserviert. |
| Teileverfolgung | OSEON-Auftragsverfolgung im 3-Ebenen-Baum mit Ampelsystem. Gate `TeileverfolgungAktiv`, Rolle `tracking`. |
| Bedarfsmeldung | Bedarf aus der Stueckliste an den Einkauf melden (`PartRequisition`). Gate `BestellungenAktiv`. |
| Lagerbestellung | Bestellung von Lagermaterial durch Werkbank/Produktion (`WarehouseRequisition` mit `Type = Lager`). Master-Schalter `LagerbestellungAktiv` (Default **true**). |
| Glasbestellung | Derselbe Vorgang fuer den Bestelltyp **Glas** (`Type = Glas`). Eigener Reiter, eigene Empfaengergruppe, eigene Artikelgruppen. Rolle `glasbestellung`. |
| Fehlteil | Position einer Lagerbestellung, die nicht (vollstaendig) geliefert wurde. `ShortageStatus`: `WillBeRestocked` = „Offene Fehlteile", `NoRestock` = „Wird nicht nachgeliefert". |
| Teilgeliefert | Status `PartiallyDelivered` — **kein** End-Status, die Bestellung bleibt bearbeitbar. |
| Meine Fehlteile | Werkbank-Sicht (`/MissingParts`, Default nur eigene Werkbaenke). Gegenstueck: „Lager: Fehlteile" (`/MissingPartsLager`). |
| Eingehende Listen | Lager-Worklist der eingegangenen Bestellungen (`/WarehousePicking`). |
| BDE | Betriebsdatenerfassung — Zeit- und Mengenerfassung am Terminal. Gate `BdeAktiv`. |
| BDE-Terminal | Scan-Oberflaeche fuer Operatoren (`/BdeTerminal`). |
| BDE-Cockpit | Schichtleiter-Uebersicht der laufenden Buchungen (`WHERE EndedAt IS NULL`). |
| Operator | BDE-Mitarbeiter (`BdeOperator`) — nicht identisch mit dem App-Benutzer. |
| Paused | BDE-Buchungszustand mit **gesetztem** `EndedAt`; die Fortsetzung ist eine neue Buchung mit `ParentBookingId`. |
| Auto-Pause | Automatisches Pausieren offener Buchungen am exakten Schichtende (`BdeAutoPauseWorker`). |
| Schichtkalender | Schichten + Feiertage als Basis der Auto-Pause (`BdeShift`, Gate `BdeSchichtkalenderAktiv`). |
| Artikelinfo | Artikel-Detailsicht mit Bestand und „in welchen FAs kommt der Artikel vor" . Quelle ist der BOM-Cache; bei IDEAL seit v1.36.0 die FA-Struktur (Haupt-FA als Geraet plus Sub-FA als Baugruppe). |
| Aktivitaets-Protokoll | UI-Name des Lauf-Protokolls der Hintergrund-Services. DB-Tabelle und Klassen heissen `SyncLog*`, Route `/SyncLog/Index` — bewusste Asymmetrie. |
| Meldebestand | Mindestbestand je Artikel; Farbcodierung ueber `WarningThresholdPercent` / `CriticalThresholdPercent`. |
| Hauptlagerplatz | Bevorzugter Lagerplatz eines Artikels. Aus Sage ⇒ in der App gesperrt; leer ⇒ app-editierbar. Badge ⭐ nur in der Bestandsuebersicht, sonst nur Sortierung. |
| NAN | Fallback-Lagerplatz fuer negative Buchungen (`NegativeBuchungLagerplatz`), wird bei Neuinstallation angelegt. |
| DryRun | Service-Modus, in dem Laeufe rechnen und protokollieren, aber nicht schreiben (`WorkerSettings:SyncDryRun`). |
| Reconciliation | Abgleich, der in Sage verschwundene FAs storniert (`ProductionOrderReconciler`) — mit Guard und Cap, opt-in. |

## Rollen-Keys

Definiert in `../../IdealAkeWms/Models/RoleKeys.cs`, zugewiesen ausschliesslich explizit ueber
`UserRole`. **`admin` ist Wildcard.** Konzept:
[[0006-rollenkonzept-statische-keys-mit-admin-wildcard]] · Filter-Zuordnung: [[controller]].

| Key | Konstante | Darf |
|---|---|---|
| `admin` | `RoleKeys.Admin` | Vollzugriff; ueberspringt jede Pruefung |
| `masterdata` | `MasterData` | Artikel, Lagerplaetze, Werkbaenke, Artikelkategorien, Artikelmerkmale, Empfaenger + Artikelgruppen-Zuordnungen **lesen und aendern**. Benutzer/Rollen/Settings/Logs bleiben admin-only. |
| `masterdata_read` | `MasterDataRead` | Dieselben 6 Stammdaten-Sichten **nur lesen** (seit v1.20.0). Sieht zusaetzlich die Artikelinfo-Kachel. |
| `picking` | `Picking` | Kommissionierung + vollstaendiger Lagerzugriff |
| `stock` | `Stock` | Einbuchung, Ausbuchung, Bestaende |
| `stock_keyuser` | `StockKeyUser` | Lager + Lagerplatz ausbuchen/umbuchen (en bloc) |
| `stock_read` | `StockRead` | Nur lesen: Bestaende + Bewegungshistorie (seit v1.25.0) |
| `tracking` | `Tracking` | OSEON-Auftraege + Rueckmeldungen |
| `reporting` | `Reporting` | OSEON-Reporting / Betriebsdaten-Auswertung |
| `leitstand` | `Leitstand` | FAs freigeben und priorisieren |
| `fa_completion` | `FaCompletion` | FA-Vervollstaendigung: Werkbank, Arbeitsgaenge + Merkmale je FA pflegen |
| `vorbau` | `Vorbau` | FA-Abarbeitungsliste einsehen und Vorbau-AGs abhaken (seit v1.22.0) |
| `lagerbestellung` | `Lagerbestellung` | Lagerbestellungen erfassen + eigene Fehlteile verfolgen. Eng abgegrenzt, **ohne** picking/stock (seit v1.23.0) |
| `glasbestellung` | `Glasbestellung` | Wie `lagerbestellung`, aber fuer den Bestelltyp **Glas** (seit v1.25.0) |
| `bde_user` | `BdeUser` | Terminal-Buchung: Arbeitsgaenge scannen, Status wechseln |
| `bde_shiftlead` | `BdeShiftlead` | + BDE-Stammdaten, Buchungsliste, Cockpit |
| `bde_admin` | `BdeAdmin` | + Buchungen korrigieren/stornieren, Terminals konfigurieren |
| `beschichtungsauftrag` | `Beschichtungsauftrag` | IDEAL-Beschichtungsauftraege lesen (Gate `FaHierarchyBeschichtungAktiv`) — Nachtrag 2026-11-14 |
| `beschichtungsauftrag_schreiben` | `BeschichtungsauftragSchreiben` | Beschichtungsauftraege anlegen/bearbeiten (seit v1.51.0) — Nachtrag 2026-11-14 |
| `einkauf` | `Einkauf` | Einkauf → Bestellvorschlaege (seit v1.99.20, Gate `EinkaufAktiv`; ohne Seed, Manifest `Pruefung/RolleEinkauf` legt sie an) — Nachtrag 2026-11-14 |

Fuer Endanwender gibt es zusaetzlich die hand-gepflegte Uebersicht `/Users/RoleOverview`.

## Technische Begriffe des Projekts

| Begriff | Bedeutung |
|---|---|
| Server-Mode / Client-Mode | Spaltenfilter server-seitig (`data-server-column-filter="true"`, URL `?colf_*`, wirkt bei ENTER) vs clientseitig (live, nur fuer kleine unpaginierte Listen). |
| Col-Key | Spalten-Schluessel (`data-col-key`), definiert in `ColumnDefinitions.cs`. Adressiert Filter **und** Spalten-Preferences. |
| View-Key | Schluessel, unter dem Spalten-Einstellungen je Liste und Benutzer persistiert werden (`UserViewPreference`). |
| AllCap | Obergrenze 5000 hinter der Page-Size-Wahl „Alle" (`PageSize.AllCap`); `IsCappedAtAll` triggert einen sichtbaren Hinweis. |
| Filter-Mini-Syntax | OR mit `,`, NOT mit `!` — identisch in Server- und Client-Mode. |
| Satellit | 1:1- oder 1:N-Tabelle mit App-Status neben den Sage-Master-Daten des FA. |
| Composite-Filter | Zugriffs-Attribut, das mehrere Rollen alternativ zulaesst (`RequirePickingOrLeitstandAccess`). |
| Feature-Gate / Master-Schalter | AppSetting, das ein ganzes Modul freischaltet — **kumulativ** zum Rollen-Filter, kein Ersatz. |
| Drift-Guard | Test, der fehlschlaegt, wenn zwei Quellen auseinanderlaufen (z. B. Service-Key ohne Katalog-Eintrag). |
| Manual-UAT | Manuell zu pruefender Pfad, weil er nicht InMemory-testbar ist (raw SQL, LDAP, Negotiate, Fremdsystem-Reads). |
| Quellen-Weiche (`Sync:Quelle:<Bereich>`) | Wahl der Sage-Lesequelle je Abgleich: `View` (SQL, Default/Notfall), `SData`, `Vergleich` (liest beide, schreibt View). Code `…QuelleSwitch`. [[0019-quellen-weiche-view-sdata-je-abgleich]] |
| Kreditor / Debitor | Lieferant bzw. Kunde (Sage-Kontokorrent `KtoArt` `K`/`D`). Code `Creditor`/`Debitor`, UI „Kreditoren“/„Debitoren“. Herkunft Sage oder WMS (`Source`). |
| Übergabe an Sage | Status eines im WMS angelegten Stammsatzes (`SageUebergabeStatus` NichtNoetig/Offen/Uebergeben/Fehler) + Outbox `SageStammdatenOutbox`; Senden noch nicht gebaut. |

- **Sage-Wareneingangsbeleg (ELL)** (v1.99.4): Wareneingang in Sage (Belegkennzeichen ELL), den das WMS je Buchung der Maske „Wareneingang zu
  Bestellung“ als Belegübernahme aus der Bestellung anlegt (Schalter `SageWareneingangBelegAktiv`). Ersetzt fuer diese Wareneingaenge den
  Lagerbuchung-Zugang und das Schliessen von Hand durch den Einkauf.
- **Lieferschein-Nr. (Lieferant)** (v1.99.4): Lieferschein des Lieferanten, je Buchung der Maske an jedem Wareneingang gespeichert, an Sage nur im
  Sage-Wareneingangsbeleg (`EKLieferschein`, 20 Zeichen).
- **Barcode (Lieferschein-Aufkleber)** (v1.99.11): Aufkleber, den das Lager auf den Lieferschein des Lieferanten klebt; in „Wareneingang zu
  Bestellung“ gescannt und an jedem Wareneingang des Absendens gespeichert (`PurchaseOrderReceipt.Barcode`). Er ist zugleich der Dateiname der
  **Wareneingang-CSV** `<Barcode>.csv`, die der Dienst nach der Uebergabe an Sage auf einen UNC-Pfad legt (Indexdaten fuer das DMS/enaio).
  Nicht zu verwechseln mit der Lieferschein-Nr. (Lieferant) und mit Artikel-/Lagerplatz-Barcodes (Scan-Modus `'raw'`, keine Zerlegung).
- **Rahmenabruf (EBZ)** (v1.99.15): Sage-Beleg mit `Belegkennzeichen = 'EBZ'` – ein Abruf aus einer Rahmenbestellung (je Abruf-Beleg eine
  Position, `VorPosId` zeigt auf die Rahmenposition). Steht seit v1.99.15 neben den Einzelbestellungen (EBB) in „Offene Bestellungen“, im
  Wareneingang und in den Lieferverzug-Mails. Offen = Abrufmenge (`MengeBasis`) minus alle im WMS erfassten Wareneingänge; die
  Vorgangsmengen (`PositionGGBestellt/Geliefert`) gelten je Rahmen, nicht je Abruf. Spaltenfilter-Hinweis „Rahmenabruf“.

## Ergaenzungen 2026-11-14 (Wochenend-Lauf, Bündel v1.88–v1.99.22)

Nachtrag zu Kommissionierauftraegen, Wareneingang, Lieferverzug, Einkauf und Update-Skript. Fundstellen im Bündel-Worktree
`feature/2026-08-07-ideal-teile-1-5`. Bereits oben erklaert und hier nicht wiederholt: Kommissionierauftrag (KO-Nummer), Kommissionierbereich,
Kommissionierwagen, Sage-Wareneingangsbeleg (ELL), Rahmenabruf (EBZ), Kreditor/Debitor.

| Begriff | Bedeutung | Code-Name | Fundstelle |
|---|---|---|---|
| KO-Auftrag | Kurzform fuer **Kommissionierauftrag** (Nummer `KO-JJJJ-NNNNN`, ein Kreis fuer alle Typen). Nicht der Konstruktions-Termin „KO“. | `PickingOrder.Nummer` | `IdealAkeWms/Models/PickingOrder.cs` |
| Vorkommissionierung (Bereich) | Kommissionierbereich mit Haken „Vorkommissionierung“: seine Auftraege werden mit dem Vorkommissionier-Vorlauf (`VorkommissionierTage`, je Werkbank `OverridePrePickingDays`) faellig statt mit dem Kommissionier-Vorlauf. | `PickingArea.IstVorkommissionierung`, `PrePickingDaysResolver` | `IdealAkeWms/Models/PickingArea.cs:17` |
| Abschluss „Vollständig“ | KO-Auftrag nach der Umbuchung abschliessen; nicht umgebuchte Zeilen werden `Entfallen` (z. B. Baugruppe statt Einzelteile). Status `Abgeschlossen`. | `PickingOrderWorkflow.AbschliessenAsync(id, vollstaendig: true)`, `PickingOrderStatus.Abgeschlossen` | `IdealAkeWms/Services/PickingOrderWorkflow.cs:154–159` |
| Abschluss „Teilweise“ | KO-Auftrag mit Rest abschliessen: Status `TeilweiseAbgeschlossen`; Reste (nicht umgebuchte Zeilen ganz, umgebuchte mit Ist < Soll als Differenz) gehen als `Uebertragen` in einen **Folgeauftrag**. | `PickingOrderStatus.TeilweiseAbgeschlossen`, `PickingOrderLineStatus.Uebertragen` | `IdealAkeWms/Services/PickingOrderWorkflow.cs:154–200` |
| Folgeauftrag | Neuer KO-Auftrag fuer die Reste eines „teilweise“ abgeschlossenen Auftrags: gleicher Bereich, gleiche Prio, gleicher Kommissionierer, Bezug auf den Ursprung. | `PickingOrder.UrsprungsauftragId` / `Folgeauftraege` | `IdealAkeWms/Models/PickingOrder.cs`, `PickingOrderWorkflow.cs:156` |
| Wagen (Umbuchen auf Wagen) | Gepickte Zeilen eines KO-Auftrags werden auf einen Kommissionierwagen umgebucht (Zeilenstatus `Umgebucht`), erst danach Abschluss. Wagen = Lagerplatz mit `IsPickingTransport`. | `PickingController.AuftragUmbuchen`, `PickingOrderLineStatus.Umgebucht` | `IdealAkeWms/Controllers/PickingController.cs:445` |
| Sammelfreigabe | Im Leitstand mehrere FAs auf einmal freigeben (oder zuruecknehmen), optional mit Kommissionierer; die Freigabe legt die KO-Auftraege an. | `PickingLeitstandController.BulkRelease` | `IdealAkeWms/Controllers/PickingLeitstandController.cs:429` |
| Freigabe zuruecknehmen | Storniert die noch offenen KO-Auftraege eines FA, behaelt begonnene; zurueckgenommene FAs zaehlen seit v1.99.17 ins Prio-MAX (keine doppelte Prio). | `PickingOrderWorkflow.FreigabeZuruecknehmenAsync` | `IdealAkeWms/Services/PickingOrderWorkflow.cs:250` |
| Altbestand pruefen | Kennzeichen an KO-Auftraegen offener FAs mit Auftragsmenge ≠ 1 (Einmal-Skript); solange gesetzt, legt der Workflow fuer den FA nichts an, der Leitstand bestaetigt „Altbestand geprueft“. | `PickingOrder.AltbestandPruefen`, `AltbestandGeprueftAsync` | `IdealAkeWms/Models/PickingOrder.cs`, `PickingOrderWorkflow.cs:281` |
| Lieferart (Wareneingang) | Art eines Wareneingangs zu einer Bestellposition: Vollstaendig, Teillieferung, Ueberlieferung (seit v1.99.22 eigene Spalte „Überlieferung“). | `PurchaseOrderReceiptLieferart` | `IdealAkeWms/Models/PurchaseOrderReceipt.cs:4` |
| Klären (Sage-Wareneingang) | Status eines Sage-Wareneingangs ohne automatisches Senden: moeglicher Beleg in Sage ohne passende Memo-Kennung (Sicherungsnetz Bestellung + Datum + Lieferschein) oder beim Ausschalten liegengeblieben. Weiter nur „Erneut senden“ oder „als übergeben markieren“ von Hand. | `SageWareneingangStatus.Klaeren` (= 6) | `IdealAkeWms/Models/PurchaseOrderReceipt.cs:25` |
| System-Kürzel | Kuerzel in den Memo-Kennungen an Sage (`WE#<K>#<Id>#`, `SM#<K>#<Id>#`), damit Testsysteme in derselben Sage-Testdatenbank nur ihre eigenen Belege finden. Produktion leer. | ServiceSetting `Sync:SageSystemKuerzel`, `SageSystemKuerzel` | `IdealAkeWms/Services/SageSystemKuerzel.cs:11` |
| Lieferverzug-Sammelliste | Taegliche Mail an den Einkauf mit allen verzoegerten Bestellpositionen, gruppiert nach Kreditor, mit Status der Kreditor-Mail. | `Lieferverzug:EinkaufAktiv`, `LieferverzugMailService`, `PurchaseOrderReminderArt.Einkauf` | `IDEALAKEWMSService/Services/LieferverzugMailService.cs` |
| „Zu klären“ (Lieferverzug) | Abschnitt der Sammelliste fuer faellige Positionen ueber der Altersgrenze `Lieferverzug:MaxAlterTage`; gehen nie an Lieferanten (v1.99.19). Nicht dasselbe wie der Status „Klären“ beim Sage-Wareneingang. | `ZuKlaeren` im Lieferverzug-Plan, Count `zu_klaeren` | `IDEALAKEWMSService/Services/LieferverzugPlaner.cs:56` |
| Kreditor-Mail | Erinnerung je Lieferant mit seinen verzoegerten Positionen (Schalter `Lieferverzug:KreditorAktiv`), fruehestens max(`TageNachTermin`, `KreditorTageNachTermin`) Tage nach Liefertermin; Rahmenabrufe nur mit `KreditorMitRahmenabrufen`. | `LieferverzugMailService`, Protokoll `PurchaseOrderReminders` (SQL/145) | `IDEALAKEWMSService/Services/LieferverzugMailService.cs` |
| Testbetrieb (Lieferverzug) | Default **an**: jede Kreditor-Mail geht unveraendert an die Einkauf-Empfaenger („TESTBETRIEB – waere an … gegangen“). Aus = externe Mails an Lieferanten. | ServiceSetting `Lieferverzug:Testbetrieb`, `LieferverzugEinstellungen.Testbetrieb` | `IDEALAKEWMSService/Services/LieferverzugPlaner.cs:8` |
| Lieferanten-Artikelnummer | Bestellnummer des Artikels beim Lieferanten (Spalte „Lief.-Art.-Nr.“ in Kreditor-Mail und Sammelliste), aus dem Artikel-Lieferanten-Abgleich; ohne ihn leer. | `ArticleSupplier.SageBestellnummer` | `IdealAkeWms/Models/ArticleSupplier.cs:18` |
| Bestellvorschlag | Je Nachbestellartikel berechnete Bestellmenge unter Einkauf → Bestellvorschlaege (nur Lesen + CSV): Fehlmenge aus WMS-Bestand, Sage verfuegbar, offenen Bedarfen, offen bestellt und Meldebestand; Menge aus der letzten Bestellmenge L. | `PurchaseProposalCalculator`, Route `/PurchaseProposals` | `IdealAkeWms/Services/PurchaseProposalCalculator.cs:35` |
| Letzte Bestellmenge | Menge (Basiseinheit) der juengsten Sage-Bestellung (EBB/EBZ) je Artikel und Lieferant innerhalb von 3 Jahren; Grundlage L der Bestellvorschlaege (V = (⌊F/L⌋+1)×L). | `ArticleSupplier.LetzteBestellmenge`/`LetzteBestellungAm`/`LetzteBestellungBeleg`, `LetzteBestellungRueckblickJahre` | `IdealAkeWms/Models/ArticleSupplier.cs:39`, SQL/148 |
| Fallback Sage-Bestellmenge | Gibt es in 3 Jahren bei keinem Lieferanten eine Bestellung, ersetzt die Sage-Bestellmenge des Artikels L (Badge + Hinweis „Fallback: Sage-Bestellmenge …“). Einheit unbelegt (angenommen Lagereinheit, Pruefabfrage offen). | `ArticleSageDetail.Bestellmenge`, `PurchaseProposalCalculator.HinweisFallback` | `IdealAkeWms/Services/PurchaseProposalCalculator.cs:14,42` |
| Mindestbestellmenge | Mindestmenge beim Lieferanten aus Sage; aufgerundet wird nur bei vergleichbarer Einheit **und** `UmrechnungsfaktorEK = 1`, sonst Hinweis. | `ArticleSupplier.Mindestbestellmenge` | `IdealAkeWms/Models/ArticleSupplier.cs:21` |
| Offene Bedarfe (Einkauf) | Offene WMS-Bedarfe, die die verfuegbare Menge der Bestellvorschlaege mindern: abgesendete Lagerbestellungen (teilgeliefert nur der Rest) und offene Bedarfsmeldungen. Zeile fehlt = beide, leer = keine. | AppSetting `EinkaufOffeneBedarfe`, `AppSettingKeys.EinkaufBedarfWerte` | `IdealAkeWms/Models/AppSettingKeys.cs` |
| Nachlauf-Manifest | Liste aller Deploy-Schritte je Version (Schema, Daten, Einmal, Pruefung, Einstellung, Info) mit Firma, Phase und Ausfuehrung; das Update-Skript arbeitet sie ab und gibt die Hand-Schritte als Nachlauf-Liste aus. Ausgefuehrte Daten-/Einmal-Skripte stehen in `dbo.DeployScriptRuns` (SQL/142). | `scripts/deploy/nachlauf.psd1`, `Update-IdealAkeWms.ps1` | [[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]], [[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]] |
| SData-Kennwort (Geheim) | Kennwort der Sage-Schnittstelle in den Service-Einstellungen, gespeichert als `dp1:` + DataProtection-Schutztext; die Maske zeigt nur den Status. | `SData:Password`, `SettingType.Geheim`, `SdataZugangsdaten` | [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]] |

## Ergaenzungen 2026-11-16 (Beschichtungsquelle, v1.99.25)

| Begriff | Bedeutung |
|---|---|
| Kennzeichen-Quelle | AppSetting `BeschichtungKennzeichenQuelle`: woher „beschichtet“ kommt – `Artikelkategorie` (AKE: Kategorie `LackierteilKategorieName` über BomCache) oder `FaStruktur` (IDEAL: `FaHierarchyNode.Beschichtet`). [[0028-beschichtungsquelle-je-achse-ueber-appsettings-mit-automatisch]] |
| Termin-Quelle | AppSetting `BeschichtungTerminQuelle`: `Berechnet` (Vorkommissioniertermin minus Beschichtungstage, auf Abholtag) oder `View` (`Start_Beschichtung` aus FAInfos, unverändert). |
| Automatisch (Beschichtungsquelle) | Default beider Quellen: folgt dem Master `ProduktionsauftragHierarchisch` (flach = Artikelkategorie/Berechnet, hierarchisch = FaStruktur/View). |

## Ergaenzungen 2026-11-16 (FA-Struktur über SData, v1.99.26)

| Begriff | Bedeutung |
|---|---|
| Ressourcenliste (Vorlage) | Stückliste eines **Artikels** in Sage (Ressourcen), Vorlage für neue FAs. Nicht gleich der FA-Stückliste: ein FA kann mehr oder andere Positionen haben. Anzeige in „Stückliste live“ im Artikel-Modus. [[0029-fa-struktur-ideal-knoten-aus-sdata-fainfos-aus-view-hybrid]] |
| FA-Stückliste | Positionen eines konkreten Fertigungsauftrags (Belegpositionen). Im FA-Modus von „Stückliste live“, seit v1.99.26 auch für beendete Haupt-FAs. |
| Hybrid (FA-Struktur) | Modus SData an der IDEAL: Knoten und Knotenfelder aus SData, FAInfos aus der View. Gebaut, bis P-FS3 gesperrt. |

| Begriff | Bedeutung |
|---|---|
| Aktiv-Kennung (Beschichtung) | Ob die Liste „Lack“-Haken und Termin nur für beschichtete Zeilen zeigt: Kennzeichen-Quelle FaStruktur = immer aktiv; Artikelkategorie = aktiv, wenn `LackierteilKategorieName` gesetzt ist; nicht aktiv = Termin für alle Zeilen (heutiges PROD-IDEAL-/AKE-Verhalten ohne Kategorie). `BeschichtungResolver.Aktiv`. |

| Begriff | Bedeutung |
|---|---|
| Teileliste (BDE) | Terminal-Ansicht der Blechfertigung (IDEAL): Teile und Baugruppen einer Werkbank aus der FA-Struktur, je Kürzel; Melden statt AG-Fertigmeldung. Option `ProductionWorkplaces.BdeTeileliste`. v1.99.32 |
| Teilmeldung | Gemeldete Menge je fachlichem Schlüssel (HauptFA, VaterFA, SubFA, Position, Artnr, Kürzel) in `BdeTeilMeldungen`; geht nicht an Sage. |
| Waise (Teilmeldung) | Teilmeldung, deren Schlüssel nach einem Struktur-Sync nicht mehr in der Struktur vorkommt – Hinweis mit Link, keine Löschung. |
| Rücknahmefrist | Bis wann ein Werker die eigene Teilmeldung zurücknehmen darf: Schichtende, ohne Schichtkalender Ende des Meldetags. |

| Begriff | Bedeutung |
|---|---|
| Sage-Arbeitsgangnummer (Sage-AG) | `KHKPpsArbeitsgaenge.Arbeitsgangnummer` (varchar 31) je Kürzel, in `OperationConfigs.SageArbeitsgangnummer`; **kein** Sage-Arbeitsplatz. v1.99.27 |
| Handwert (Sage-AG) | `OperationConfigs.SageArbeitsgangnummerManuell` – in `/OperationConfigs` gepflegt, hat Vorrang vor dem Sage-Wert; der Sync fasst ihn nie an. |
