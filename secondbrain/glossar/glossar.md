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
| Arbeitsgang | **Drei** verschiedene Dinge im Projekt: FA-Vorbau-AG (`WorkStep`), BDE-Arbeitsgang (`WorkOperation`) und OSEON-Arbeitsgang. Kontext beachten. |
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
| FE-Termin | `FaHierarchyOrderInfo.FE_Termin` — Fertigstellungstermin. Speist bei IDEAL die Termin-Kaskade der FA-Liste (Fert.-Termin). |
| BG-Termin | Spalte der FA-Liste: der **Vorkommissioniertermin**, rueckwaerts aus dem Kommissioniertermin minus `VorkommissionierTage` gerechnet — je Werkbank ueberschreibbar (`ProductionWorkplace.OverridePrePickingDays`). Kein Rohfeld. |
| Komm. (Spalte) | Spalte der FA-Liste: der **berechnete** Kommissioniertermin (Fertigungstermin minus `KommissionierTage`), nicht der Kommissionier-**Status**. |
| Lack-T | Icon-Spalte der FA-Liste fuer `HasCoatingParts`/`IsCoatingDone`. Bei AKE aus der Artikelkategorie erkannt, bei IDEAL aus `FaHierarchyNode.Beschichtet` abgeleitet — **nie beide zugleich**, der Master entscheidet. |

## Prozesse und Module

| Begriff | Bedeutung |
|---|---|
| Kommissionierung / Picking | Bereitstellen von Bauteilen zu einem FA (Umbuchung auf einen Kommissionierwagen). |
| Kommissionierwagen | Lagerplatz mit `IsPickingTransport` — Transportmittel, kein Bestandsort. Aus Verfuegbarkeits- und Meldebestandsrechnung ausgeschlossen. |
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
