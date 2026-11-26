---
type: spec
title: "Kommissionierauftrag: Maske umschaltbar Stückliste / Liste (summiert je Artikel, Buchung verteilt auf Positionen), Standard „Liste“ bei aktiven Kommissionierlisten"
slug: 2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-3-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste]]"
uebersicht: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-uebersicht]]"
depends_on: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: IDEAL
affected_code:
  - IdealAkeWms/Controllers/PickingController.cs (Auftrag Z. 252-258 Parameter ansicht + [FromServices] IFaHierarchyOrderInfoRepository; BomSeiteAsync Z. 260-442 Summierung bei Liste; AuftragUmbuchen Z. 444-474 unveraendert)
  - IdealAkeWms/Services/KommissionierauftragListe.cs (NEU, rein/DB-frei - Summenzeilen je Artikel+Ziel+BG aus den Zeilen-ViewModels)
  - IdealAkeWms/Services/PickingOrderVerteilung.cs (NEU, rein/DB-frei - Ist-Menge einer Summenzeile in Positionsreihenfolge auf offene PickingOrderLines verteilen)
  - IdealAkeWms/Services/IPickingTransferService.cs (PickingSelectionItem + PickingItemIds)
  - IdealAkeWms/Services/PickingTransferService.cs (TransferOrderLinesAsync Z. 53-73 - Summenzeilen vor der Pruefung expandieren; Buchung Z. 191-241 unveraendert)
  - IdealAkeWms/Models/FaHierarchyBomItem.cs + IdealAkeWms/Data/Repositories/FaHierarchyBomRepository.cs (Z. 174-194 - Arbeitsbereich durchreichen)
  - IdealAkeWms/Models/ViewModels/BomViewModels.cs (IstListe, KannListe, Summenzeilen, Arbeitsbereich, Abteilung)
  - IdealAkeWms/Views/Picking/Bom.cshtml (Umschalter im Auftragskopf Z. 47-57; Liste-Tabelle; collectPickedItems Z. 1601-1618; Druck-Reihenfolge Z. 1404-1420)
  - IdealAkeWms/Views/Picking/_AuftragListeTabelle.cshtml (NEU, Partial - thead/tbody/column-config der Liste)
  - IdealAkeWms/Controllers/PickingController.cs (AuftragDrucken GET/POST Z. 602-613 + RenderAuftragDruckenAsync Z. 615-671 - Parameter ansicht/visibleColumns/pdf, Zweig summierter Druck + PDF ueber [FromServices] IRazorViewRenderer/IPdfRenderService; Auftrag: [FromServices] PdfRenderStatus fuer den Knopf)
  - IdealAkeWms/Views/Picking/PrintAuftragListe.cshtml (NEU - Aufbau/Stil wie FaHierarchyKommissionierListen/PrintSummiert.cshtml, Spalten per ShowCol)
  - IdealAkeWms/wwwroot/js/print-visible-columns.js (unveraendert wiederverwendet, data-print-link am Druckknopf der Liste)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (neue ViewConfig KommissionierauftragListe neben Bom Z. 260 + Lookup Z. 1061)
  - IdealAkeWms/Models/AppSettingDefinitions.cs (Wirkungstext FaHierarchyKommissionierlistenAktiv)
  - IdealAkeWms.Tests (KommissionierauftragListeTests, PickingOrderVerteilungTests, PickingTransferServiceTests, PickingControllerTests, ViewKeyDriftGuardTests)
  - IdealAkeWms/Views/Help/* (Kommissionierung), Changelog
  - scripts/deploy/nachlauf.psd1
  - docs/TESTSZENARIEN.md
open_questions:
  - "1 (beantwortet 2026-11-26) Spaltensatz der Maske „Liste“: Komm.-Ziel, Hauptlagerplatz, Position, Artikelnummer, Matchcode, Bezeichnung, Menge Soll, Menge Ist, Lagerplatz/Quell-Lagerplatz, Abteilung (ausblendbar) - einverstanden, oder fehlt eine Spalte der Kommissionierliste?"
  - "2 (beantwortet 2026-11-26, abweichend: summiert) Zeilen der Liste: je Auftragszeile (wie Stückliste, nötig für Haken/Ist/Umbuchen) statt summiert je Artikel wie die Kommissionierliste - einverstanden?"
  - "3 (beantwortet 2026-11-26) Sortierung der Liste: Komm.-Ziel, dann Hauptlagerplatz, dann Position (Laufweg) - einverstanden?"
  - "4 (beantwortet 2026-11-26) Baugruppen-Zeilen in der flachen Liste: zeigen (mit Kennzeichen „BG“, ohne Einrücken), wenn die Auftragszeile eine ist - einverstanden?"
  - "5 (beantwortet 2026-11-26) Gewählte Maske merken: nein (Umschalter wirkt per URL, jeder Aufruf startet mit dem Standard) - einverstanden?"
  - "6 (beantwortet 2026-11-26) Soll die Kommissionierliste (/FaHierarchyKommissionierListen) je HauptFA auf die zugehörigen Kommissionieraufträge verlinken? Empfehlung: nicht in diesem Teil."
  - "7 (beantwortet 2026-11-26: ja) Schnitt: ein Epic in einem Lauf mit Etappe A (Umschalter + summierte Anzeige, noch ohne Buchung aus der Liste) und Etappe B (Verteil-Buchung) als zwei Commits, ein Merge - einverstanden?"
  - "8 (beantwortet 2026-11-26: weglassen) Bestellen (Bedarfsmeldung) und Lagerbestellung in der Liste: Empfehlung nicht anbieten (Spalten und Sammelknopf nur in der Stückliste) - einverstanden?"
  - "9 (beantwortet 2026-11-26, abweichend: summiert wie die Liste, Aussehen wie PDF-Druck der Kommissionierliste, eingeblendete Spalten) „Auftrag drucken“ aus der Liste: Druck bleibt je Position (PrintPicking), nur die Reihenfolge folgt der Liste; ein summierter Auftragsdruck wäre ein eigener Eintrag - einverstanden?"
  - "10 (beantwortet 2026-11-26: zusätzlich PDF-Knopf wie PdfSummiert) Summierter Auftragsdruck: nur Bildschirm-/Browserdruck (wie heute „Auftrag drucken“) oder zusätzlich ein PDF-Download-Knopf wie „PDF“ der Kommissionierliste (PdfSummiert)? Empfehlung: nur Browserdruck."
epic: true
etappen:
  - "A Umschalter + summierte Anzeige (Spalten, Sortierung, BG, Arbeitsbereich/Abteilung) + summierter Auftragsdruck aus der Liste - Liste noch ohne Haken/Ist/Quell-Lagerplatz"
  - "B Verteil-Buchung (PickingOrderVerteilung, PickingItemIds, Service-Expansion, Skript collectPickedItems)"
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

An der IDEAL arbeiten die Kommissionierer mit der Kommissionierliste (flach, nach Ziel und Lagerplatz), gebucht wird aber über die
Kommissionieraufträge (KO-…), die nur die Stücklisten-Maske (Baum) haben. Wer einen Auftrag wie KO-2026-00004 öffnet, soll zwischen
Maske 1 „Stückliste“ und Maske 2 „Liste“ wechseln können; mit aktiven Kommissionierlisten ist „Liste“ der Standard. Die Liste ist
wie die Kommissionierliste **je Artikel summiert** (Antwort 2); gebucht wird weiter je Position (`PickingOrderLine`): eine Ist-Menge
der Summenzeile wird in Positionsreihenfolge auf die Auftragszeilen verteilt. Haken, Soll/Ist, Umbuchung auf den Kommissionierwagen,
Drucken/Abschließen und Fotos gelten in beiden Masken.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

- `/Picking/Auftrag/{id}`: Umschalter „Stückliste | Liste“ im Auftragskopf, Parameter `ansicht=stueckliste|liste`.
- Maske „Liste“: dieselbe Seite (`Bom.cshtml`), Tabelle neu als Partial, **eine Zeile je Artikel + Komm.-Ziel** (Summe der
  Auftragszeilen), eigene Spaltenauswahl, Sortierung nach Laufweg, Bedienelemente der Auftragsseite.
- Verteil-Buchung: Ist-Menge einer Summenzeile → offene Auftragszeilen in Positionsreihenfolge, jede bis zu ihrem Soll; Buchung je
  Zeile über den vorhandenen Umbuchungsweg (`AuftragUmbuchen` → `PickingTransferService.TransferOrderLinesAsync`).
- `Arbeitsbereich` aus der FA-Struktur bis in die Auftragsseite durchreichen (heute nicht in `FaHierarchyBomItem`).
- „Auftrag drucken“ aus der Liste druckt **summiert** (Antwort 9): Summenzeilen in der Reihenfolge der Liste, nur die in der Liste
  eingeblendeten Spalten, Aufbau und Aussehen wie der PDF-Druck der Kommissionierliste (`PrintSummiert.cshtml`). Aus der Maske
  „Stückliste“ bleibt der Druck je Position wie heute (`PrintPicking`).
- Standard „Liste“, wenn `FaHierarchyKommissionierlistenAktiv = true` und die Stückliste aus der FA-Struktur kommt.

**Out-of-Scope**

- `/Picking/Bom/{id}` (Stückliste ohne Auftrag), AKE-Ablauf, Maske „Stückliste“ der Auftragsseite (bleibt je Zeile wie heute).
- Kommissionierliste `/FaHierarchyKommissionierListen` (Liste, Summiert, Druck) bleibt **unverändert**, kein Link von dort zum
  KO-Auftrag (Antwort 6 / Nachfrage zu 6). Die Liste-Maske ist eine Neu-Implementierung am KO-Auftrag.
- Neuer Endpunkt (der summierte Druck ist ein Zweig von `AuftragDrucken`), neue Rolle, neue Einstellung, Migration.
- Bestellen/Lagerbestellung in der Liste (Antwort 8: weglassen; nur in der Stückliste).
- ~~Summierter Auftragsdruck; `AuftragDrucken` (v1.97.0) bleibt je Position, nur die Reihenfolge kommt aus der Liste (Rückfrage 9).~~
  *Überholt durch Antwort 9 – siehe In-Scope.*
- ~~PDF-Download des Auftragsdrucks (Rückfrage 10, Empfehlung: nur Browserdruck).~~ *Überholt durch Antwort 10: der PDF-Knopf
  ist In-Scope (K11), aber nur in der Maske „Liste“.*
- ~~Neue Buchungslogik~~ – *überholt durch Antwort 2 (summiert): die Verteilung ist neu, die Buchung je Zeile bleibt.*

## Fachliche Anforderungen

### Ist-Stand (belegt; Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, AppVersion 1.99.81; nachgeprüft v1.99.84)

- `Auftrag(int id, string? filterText)` lädt den Auftrag mit Zeilen und ruft `BomSeiteAsync(order, auftrag, filterText)`
  (`Controllers/PickingController.cs:252-258`). `BomSeiteAsync` (`:260-442`) filtert die Stücklisten-Positionen auf die Auftragszeilen
  (Schlüssel `PickingOrderRestmengen.Schluessel(ArticleNumber, BomPosition)`, `:288-293`), füllt `BomItemViewModel` (Kommissionieren,
  Hauptlagerplatz, Ebene, VaterSubFa, SagePosition, Lagerplätze, IstMenge, Quell-Lagerplatz-Vorschlag), sortiert mit
  `NaturalPositionComparer`, setzt `ReadOnly` (Status nicht Offen/InArbeit), `Hierarchical` (Quelle FaHierarchie) und
  `TargetStorageLocations` (Kommissionierwagen) und rendert `View("Bom", vm)`.
- `Views/Picking/Bom.cshtml` ist die eine Maske für Stückliste und Auftrag: Auftragskopf `:47-57`; Tabelle `id="bomTable"`
  `data-view-key="Bom"` (`:191`); Baum über `data-parent-pos`, `style="display:none"` für Kinder und Einrückung
  `TreeLevel*24` (`:248-254`); Haken `.picking-checkbox` (`:260-263`); Ist-Menge `.ist-menge` nur mit Auftrag (`:205-208`, `:307-309`);
  Quell-Lagerplatz `.source-location-select` (`:361`); Umbuchungskarte mit Wagen-Auswahl, Scan, „Gepickte Artikel umbuchen“,
  „Auftrag drucken“, „Auftrag abschließen …“ (`:457-496`); Fotos (`:498-518`, `initPhotoUpload` `:1727-1731`, Fotos hängen am FA);
  Baum-Skript `:975-1007`, `:1235`; Umbuchungs-Skript liest angehakte `.picking-checkbox` und `.ist-menge` (`:1603-1639`);
  Spaltenkatalog `:752-779`.
- Aktionen: `AuftragUmbuchen` (`:444-474`), `AuftragAbschliessen` (`:476-489`), `AuftragDrucken` GET/POST (`:604-613`). Zugriff
  `[RequirePickingAccess]` je Aktion (Klasse ohne Attribut).
- ViewConfig `Bom` (`Models/ViewModels/ColumnDefinitions.cs:241`, `SupportsReorder: true`), Lookup `:1024`. *(v1.99.84: `:260`,
  Lookup `:1061`.)*
- **Buchungsweg (v1.99.84, Grundlage der Verteilung):** `PickingSelectionItem` = `PickingItemId`, `SourceStorageLocationId`,
  `IsBaugruppe`, `IstMenge` als Text (`Services/IPickingTransferService.cs:5-12`). `TransferOrderLinesAsync`
  (`Services/PickingTransferService.cs:36-108`) prüft alles vor der ersten Buchung: Auftrag Offen/InArbeit (`:48-49`), keine Zeile doppelt
  (`:55-56`), Zeile gehört zum Auftrag (`:59-60`), Zeilenstatus Offen/Gepickt (`:61-62`), Ist-Menge Pflicht, Zahl, `> 0` und `≤ SollMenge`
  (`:64-69`), Quell-Lagerplatz Pflicht (`:70-71`). Gebucht wird in **einer** Transaktion je Zeile eine `Umbuchung` (`:191-241`); der
  Bestand je (Artikel, Quelle) wird nach jeder Zeile abgezogen (`:208` „doppelter Artikel am selben Platz“), bei Unterdeckung und
  `NegativeBuchungErlaubt` weicht die einzelne Zeile auf den Negativ-Lagerplatz aus (`:199-207`). Die Zeile wird danach `Umgebucht`
  mit `IstMenge`, `SourceStorageLocationId`, `StockMovement` und Audit-Feldern (`:228-239`) – eine Zeile kann also nur **einmal**
  gebucht werden (eine `StockMovementId` je Zeile, `Models/PickingOrder.cs:100`). `PickingOrderLine` trägt `RowVersion`
  (`Models/PickingOrder.cs:103`).
- **Teilmengen heute:** Eine mit `Ist < Soll` umgebuchte Zeile ist `Umgebucht` und nicht mehr buchbar
  (`PickingController.cs:376`, `PickingTransferService.cs:61-62`); ihr Rest zählt nach `PickingOrderRestmengen.Abgedeckt`
  (`Services/PickingOrderRestmengen.cs:19-27`) als offen und geht beim teilweisen Abschluss in den Folgeauftrag
  (`PickingController.cs:476-489` → `PickingOrderWorkflow.AbschliessenAsync`, `Services/PickingOrderWorkflow.cs:178-212`: „teilweise“ →
  nicht umgebuchte Zeilen ganz und umgebuchte mit Ist < Soll als Rest in den Folgeauftrag, Zeilen `Uebertragen`; „vollständig“ →
  nicht umgebuchte Zeilen `Entfallen`). Nicht gebuchte Zeilen bleiben bis dahin `Offen`.
- **Quell-Lagerplatz offener Zeilen:** `PickingOrderLine.SourceStorageLocationId` wird nur in `PickingTransferService.cs:230` geschrieben,
  im selben Schritt wie `Status = Umgebucht` (Suche nach Schreibzugriffen in `IdealAkeWms`, `IDEALAKEWMSService`, `SQL/`: keine
  weiteren). Offene Zeilen haben also keinen gespeicherten Quell-Platz; die Seite zeigt den Vorschlag
  `QuellLagerplatzVorschlag.Waehle` je **Artikel** (`PickingController.cs:334-343`) – für alle Zeilen eines Artikels derselbe Wert.
- **Haken:** rein lokal, nichts wird gespeichert (`Bom.cshtml:1581-1593`); `Gepickt` wird im Code nirgends gesetzt, nur gelesen
  (`PickingController.cs:372`, `PickingTransferService.cs:61`). Der Artikel-Scan hakt alle Zeilen mit passendem
  `data-article-number` an (`Bom.cshtml:846-880`).
- **Umbuchungs-Skript:** `collectPickedItems` liest je angehaktem `.picking-checkbox` `data-id`, `data-is-baugruppe`, das
  `.source-location-select` und das `.ist-menge` derselben Zeile (`Bom.cshtml:1601-1618`) und sendet `items[i].*` als Formularfelder
  (`:1633-1640`). Der Druck sammelt `data-position` aller `tr[data-position]` als `reihenfolge` (`:1404-1420`);
  `RenderAuftragDruckenAsync` druckt immer alle Zeilen je Position in dieser Reihenfolge (`PickingController.cs:615-671`).
- **PDF-Druck der Kommissionierliste (Vorlage für den summierten Auftragsdruck):** Der Knopf „PDF“ der Summiert-Ansicht ruft
  `PdfSummiert` (`Controllers/FaHierarchyKommissionierListenController.cs:333-361`); er rendert **dieselbe View `PrintSummiert`** per
  `_viewRenderer.RenderToStringAsync(…, "PrintSummiert", vm)` und `IPdfRenderService` zu PDF (`:357-359`). Der Bildschirmdruck
  `PrintSummiert` (`:308-328`) nutzt dieselbe View – PDF und Browserdruck sehen also gleich aus; das Layout steht in
  `Views/FaHierarchyKommissionierListen/PrintSummiert.cshtml` (eigenes HTML ohne Layout `:4`, Stilblock `:13-30`, Banner
  „Summierte Ansicht“ `:49-52`, Kopf mit Barcode `:61-69`, `table.positions` `:71-93`). Die nicht summierte Liste hat den Zwilling
  `Pdf`/`Print.cshtml` (`:270-296`).
- **Muster „Druck/PDF nur sichtbare Spalten“ (v1.47.0):** `wwwroot/js/print-visible-columns.js` hängt beim Klick auf
  `a[data-print-link="<ViewKey>"]` die sichtbaren `th[data-col-key]` von `table[data-view-key="<ViewKey>"]` als `visibleColumns` an
  die URL (`:9-24`); der Server löst über `PrintVisibleColumns.ResolveAsync` auf (`Services/PrintVisibleColumns.cs:20-36`: Parameter
  gewinnt, sonst gespeicherte `UserViewPreference`, Locked-Spalten immer, leer = alle), die View blendet mit
  `ShowCol(key)` ein (`PrintSummiert.cshtml:6`). Spaltenreihenfolge im Druck ist fest in der View verdrahtet
  (`PrintVisibleColumns.cs:14-15`). Seit v1.48.0 nutzt auch der Beschichtungsauftrag das Muster (`:11-12`).
  `PickingController` hat weder `IUserViewPreferenceRepository` noch einen Barcode-Dienst im Konstruktor; `AuftragDrucken` holt
  `IBarcodeService` per `[FromServices]` (`PickingController.cs:604`, Barcode der Auftragsnummer `:647`).
- **Druck-Knopf der Auftragsseite heute:** Link `#btnAuftragDrucken` → `AuftragDrucken` GET (`Bom.cshtml:488`); bei aktiver
  Spaltensortierung baut das Skript ein POST-Formular mit `reihenfolge` und `action = getAttribute('href')` (`:1404-1426`).
  `SortiereZeilen` ordnet Zeilen nach dieser Positionsliste, fehlende am Ende (`PickingController.cs:674-683`).
- **Bestellen/Lagerbestellung** hängen an `.picking-checkbox:checked` + `.order-single-btn` je Zeile mit `data-position`/`data-quantity`
  (`Bom.cshtml:382-433`, `:1850-1909`).
- **Summierung Kommissionierliste:** `KommissionierListenService.AggregateSummiert` (`Services/KommissionierListenService.cs:226-295`)
  summiert `FaHierarchyNode` je `(HauptFA, Artnr getrimmt, Kommissionieren getrimmt)`, nur `Sollmenge`, Artikeleigenschaften per
  `g.First()` (`:250-269`), sortiert HauptFA → Artnr → Ziel (`:270-272`). Direkt wiederverwendbar ist sie nicht (Eingabe sind
  Strukturknoten ohne Auftragszeilen, keine Ist/Status/Lagerplatzauswahl) – übernommen werden Schlüssel und Muster.
  Mehrwertige Texte fasst `VormontageService.FormatTexte` zusammen (`Services/VormontageService.cs:113-116`, genutzt von der
  Vormontage-Summiert-Ansicht `:344`).
- **Arbeitsbereich:** steht am Strukturknoten (`Models/FaHierarchyNode.cs:45`), **nicht** in `FaHierarchyBomItem`
  (`Models/FaHierarchyBomItem.cs:10-32`); die Abbildung Knoten → BOM-Zeile liegt in `FaHierarchyBomRepository.cs:174-194`.
- **Abteilung:** je HauptFA aus den Kopfzeilen: `KommissionierListenService.AbteilungJeHauptFa` (`:94-96`) über
  `IFaHierarchyOrderInfoRepository.GetByHauptFaAsync(int)` (`Data/Repositories/IFaHierarchyOrderInfoRepository.cs:16`). HauptFA =
  `int.Parse(order.OrderNumber)` wie in `FaHierarchyBomRepository.cs:67`. `PickingController` hat das Repository nicht im Konstruktor;
  `[FromServices]` an der Action ist im Controller schon Muster (`AuftragDrucken`, `PickingController.cs:604`).
- Settings liest der Controller über `_settingRepository.GetValueAsync(AppSettingKeys.…)` mit `"true"`-Vergleich
  (`PickingController.cs:239-240`).
- Kommissionierliste: `FaHierarchyKommissionierListenController` (`[RequireLagerProcessingAccess]` +
  `[RequireFaHierarchyKommissionierlistenAktiv]`), Spalten `ColumnDefinitions.FaHierarchyKommissionierListen` (`:334-354`): HauptFA,
  Haupt-Artnr., Artnr., Matchcode, Sollmenge, Hauptlagerplatz, Lagerplätze, Kommissionieren (Ziel), Arbeitsbereich, Montage-Abteilung,
  ausgeblendet Artikeltyp/beschichtet/Material. Die Liste summiert Knoten, sie hat keine Auftragszeilen und keine Buchung.
- Einstellung `FaHierarchyKommissionierlistenAktiv` (`AppSettingKeys.cs:68`, Definition `AppSettingDefinitions.cs:258-259`: Bool,
  Default `false`, Gruppe IDEAL, „Schaltet das Kommissionierlisten-Modul ein.“, Standort IDEAL, Seed). Gelesen von
  `RequireFaHierarchyKommissionierlistenAktivAttribute.cs:45`, `StandortSettingsCatalog.cs:60`, `NavigationKatalog.cs:242`.

### Soll

| ID | Anforderung |
|---|---|
| K1 | Auftragskopf der Auftragsseite zeigt einen Umschalter (Bootstrap `btn-group`, zwei Links) „Stückliste“ / „Liste“; aktive Maske hervorgehoben **und** mit `aria-current`. Filtertext bleibt beim Wechsel erhalten. |
| K2 | Standard ohne `ansicht`: „Liste“, wenn `FaHierarchyKommissionierlistenAktiv = true` **und** `Hierarchical` (Quelle FaHierarchie); sonst „Stückliste“. Ungültiger Wert = Standard. |
| K3 | Ist die Kommissionierliste aus oder die Quelle nicht FaHierarchie, gibt es keinen Umschalter; `ansicht=liste` wird ignoriert (AKE unverändert). |
| K4 | ~~Maske „Liste“: gleiche Zeilen wie die Stückliste des Auftrags (je Auftragszeile), flach: kein Einrücken, kein Chevron, keine versteckten Kinder; Baugruppen-Zeilen mit Kennzeichen „BG“ (Text). Sortierung Komm.-Ziel → Hauptlagerplatz → Position (natürliche Sortierung, leere zuletzt).~~ *Überholt durch Antwort 2 (summiert) – neu:* Maske „Liste“: **eine Summenzeile je (Ressourcenummer getrimmt/groß, Komm.-Ziel getrimmt, BG-Kennzeichen)** über die Zeilen des Auftrags (Schlüssel wie `AggregateSummiert` ohne HauptFA, da ein Auftrag zu genau einem FA gehört; BG im Schlüssel, damit `IsBaugruppe` je Zeile eindeutig bleibt). Flach: kein Einrücken, kein Chevron, keine versteckten Kinder; Baugruppen-Zeilen mit Kennzeichen „BG“ (Text, Antwort 4). Sortierung Komm.-Ziel → Hauptlagerplatz → erste Position der Summenzeile (natürliche Sortierung) → Artikelnummer; leere Werte zuletzt (Antwort 3; bewusst nicht die Artnr-Sortierung der Summiert-Ansicht `KommissionierListenService.cs:270-272`). |
| K4a | Werte einer Summenzeile: **Menge Soll** = Σ `SollMenge` aller Zeilen der Gruppe; **Pos.** = alle Positionen, natürliche Reihenfolge, „, “-getrennt; **Menge Ist** = Eingabefeld, vorbelegt mit Σ Soll der offenen Zeilen (Offen/Gepickt), daneben „bereits gebucht x“ = Σ `IstMenge` der umgebuchten Zeilen, wenn es welche gibt; Artikeleigenschaften (Matchcode, Bezeichnung 1/2, Artikelgruppe, Beschaffung, Kategorie, Hauptlagerplatz, Lagerplatz) aus der ersten Zeile (je Artikel konstant, Muster `AggregateSummiert` `:264-268`); **Arbeitsbereich**, Sage-Pos., Vater-Sub-FA, Ebene, Baugruppe = verschiedene Werte über `VormontageService.FormatTexte`. **Abteilung** = `AbteilungJeHauptFa` des HauptFA (für alle Zeilen gleich). |
| K4b | Zeilenzustand: mindestens eine offene Zeile → Summenzeile bearbeitbar (Haken, Ist, Quell-Lagerplatz); alle Zeilen umgebucht/entfallen/übertragen → Statustext wie heute (`ZeilenStatusText`, bei gemischtem Status „teilweise umgebucht (n von m Pos.)“), keine Eingaben. |
| K5 | ~~Spalten der Liste … ausgeblendet: Bezeichnung 2, Abteilung, Vater-Sub-FA, Sage-Position.~~ *Überholt durch Antwort 1 + Nachfrage zu 1 – neu:* eigener View-Key `KommissionierauftragListe`, Spaltenauswahl/Umsortieren über das Zahnrad wie überall. **Sichtbar:** Haken, Komm.-Ziel, Hauptlagerplatz, Pos., Artnr. (Ressourcenummer), Matchcode, Bezeichnung 1, Menge Soll, Menge Ist, Lagerplatz, Quell-Lagerplatz, Arbeitsbereich, Abteilung, Bezeichnung 2, Artikelgruppe, Beschaffung, Kategorie. **Über das Zahnrad (Standard ausgeblendet):** Baugruppe, Sage-Pos., Ebene, Vater-Sub-FA (letzte zwei nur bei voller Struktur, wie `Bom.cshtml:218-222`). Keine Spalten Bestellen/Lagerbestellung und kein Sammel-Bestellknopf in der Liste (Antwort 8). |
| K6 | In der Liste funktionieren: Haken je Summenzeile, Ist-Menge, Quell-Lagerplatz, Artikel-Scan (hakt die Summenzeile an), Umbuchung auf Kommissionierwagen (Auswahl + Scan, Konflikt-Dialog „trotzdem buchen“), „Auftrag drucken“ (summiert, K11), „Auftrag abschließen“, Fotos, Filterfeld, Schreibschutz bei Status ≠ Offen/InArbeit. |
| K7 | Umschalten verliert nichts Gebuchtes (alles liegt in `PickingOrderLine`); nicht gespeicherte Haken/Ist-Eingaben gehen wie bei jedem Seitenwechsel verloren – der Umschalter fragt nach, wenn Haken gesetzt sind (vorhandenes `confirm`-Muster). Gewählte Maske wird nicht gemerkt (Antwort 5). |
| K8 | **Verteil-Buchung** (Antwort 2 + Nachfrage zu 2): Haken an einer Summenzeile = Buchung der eingegebenen Ist-Menge auf die **offenen** Zeilen der Gruppe (Offen/Gepickt), sortiert nach `BomPosition` (`NaturalPositionComparer`), jede Zeile bis zu ihrem `SollMenge`, bis die Ist-Menge aufgebraucht ist. Unterdeckung: die zuletzt erreichte Zeile wird mit dem Rest teilgebucht (`Umgebucht`, Ist < Soll), nicht erreichte Zeilen bleiben `Offen` (beides geht beim teilweisen Abschluss wie heute in den Folgeauftrag). Überdeckung (Ist > Σ offenes Soll): abgelehnt mit Meldung, nichts gebucht – folgt aus „jede bis zu ihrem Soll“ und der vorhandenen Regel Ist ≤ Soll (`PickingTransferService.cs:68-69`). Ist ≤ 0 oder keine Zahl: abgelehnt wie heute. Gebucht wird je Zeile über die unveränderte Buchungsschleife (eine Bewegung je Zeile, eine Transaktion für die ganze Auswahl, Bestandsabzug je Zeile). |
| K9 | Quell-Lagerplatz einer Summenzeile: ein Auswahlfeld, vorbelegt mit dem Vorschlag je Artikel (offene Zeilen haben keinen gespeicherten Platz, siehe Ist-Stand); alle verteilten Zeilen der Summenzeile bekommen diesen Platz. Reicht der Bestand nicht und ist Negativbuchung erlaubt, weicht wie heute je Zeile der Negativ-Lagerplatz ein (`PickingTransferService.cs:199-207`). |
| K11 | **Summierter Auftragsdruck** (Antwort 9 + Nachfragen Gerald zum Druck): „Auftrag drucken“ aus der Liste druckt die Summenzeilen des KO-Auftrags – **alle** Summenzeilen des Auftrags, auch weggefilterte (wie der heutige Auftragsdruck, `Bom.cshtml:1405-1406`), in der Zeilenreihenfolge der Liste (inkl. Spaltensortierung). Spalten = die zur Klickzeit in der Liste eingeblendeten (Muster v1.47.0 „nur sichtbare Spalten“), ohne Haken; Menge Ist = gebuchte Summe, Quell-Lagerplatz = Text (gespeicherter Platz bzw. Vorschlag wie `PickingController.cs:629-632`). Aussehen wie der PDF-Druck der Kommissionierliste (`PrintSummiert.cshtml`: Stil, Banner „Summierte Ansicht“, Kopf mit Barcode, `table.positions`); Kopf mit KO-Nummer + Barcode, FA, Bereich, Status, Fälligkeit wie `PrintPicking`. Aus der Maske „Stückliste“ bleibt der Druck je Position unverändert. **PDF-Knopf** (Antwort 10) neben „Auftrag drucken“ in der Liste: lädt dasselbe Dokument als PDF (dieselbe View, dieselben Summenzeilen, Reihenfolge und sichtbaren Spalten), Muster `PdfSummiert`; nur sichtbar, wenn der PDF-Dienst verfügbar ist (`PdfRenderStatus.IsAvailable`, Muster `Summiert.cshtml:106`); Fehler beim Rendern → Warnmeldung auf der Auftragsseite (Liste), kein leeres PDF. |
| K10 | Filterfeld (`filterText`) wirkt in der Liste auf die Auftragszeilen **vor** dem Summieren (`PickingController.cs:384-393` bleibt vor der Summierung); eine Summenzeile zeigt und bucht dann nur die passenden Zeilen. |

## Technischer Loesungsentwurf

Ponytail-Leiter: kein neuer Controller, kein neuer Endpunkt, kein neues JS-Modul, keine neue Buchungsschleife. Die Seite bleibt
`Bom.cshtml` (Kopf, Umbuchungskarte, Modals, Fotos, Skripte gelten für beide Masken); nur die Tabelle der Liste ist neu (Partial).
Die einzige neue Logik sind zwei reine, unit-getestete Funktionen: Summieren und Verteilen.

**Etappe A – Umschalter und summierte Anzeige**

1. **Controller** `Auftrag(int id, string? filterText, string? ansicht, [FromServices] IFaHierarchyOrderInfoRepository orderInfos)`
   (Muster `[FromServices]` wie `PickingController.cs:604`, kein Konstruktor-Umbau). `BomSeiteAsync` bekommt `ansicht` und
   `orderInfos`; nach dem Bau von `vm`: `vm.KannListe = vm.Hierarchical && (await _settingRepository.GetValueAsync(
   AppSettingKeys.FaHierarchyKommissionierlistenAktiv))?.Equals("true", OrdinalIgnoreCase) == true` (Muster `:239-240`);
   `vm.IstListe = vm.KannListe && ansicht != "stueckliste"`. Bei `IstListe`: `vm.Summenzeilen = KommissionierauftragListe.Summiere(
   viewItems, zeileByKey, abteilung)` mit `abteilung = KommissionierListenService.AbteilungJeHauptFa(await orderInfos.GetByHauptFaAsync(
   hauptFa)).GetValueOrDefault(hauptFa)` (HauptFA per `int.TryParse(order.OrderNumber)`, sonst `null`). Die Zeilen-Items und ihr
   Textfilter bleiben unverändert davor (K10).
2. **Neu `Services/KommissionierauftragListe.cs`** (static, DB-frei): `Summiere(...)` gruppiert die Zeilen-ViewModels mit
   Auftragszeile nach K4-Schlüssel, füllt K4a/K4b und sortiert nach K4. Ergebnis je Summenzeile u. a.: `OffeneZeilenIds`
   (Positionsreihenfolge), `Positionen` (alle, für Anzeige und Druck-Reihenfolge), `SollSumme`, `OffenSoll`, `GebuchtIst`,
   `IsBaugruppe`, `QuellVorschlagId`, Artikeleigenschaften, `Arbeitsbereich`, `Abteilung`, `StatusText`.
3. **Arbeitsbereich durchreichen:** `FaHierarchyBomItem.Arbeitsbereich` (neu) in `FaHierarchyBomRepository.cs:174-194` aus
   `node.Arbeitsbereich` setzen, `BomItemViewModel.Arbeitsbereich` in `PickingController.cs:345-379` übernehmen. Keine Migration (Spalte
   existiert am Knoten).
4. **ViewModel** (`BomViewModels.cs`): `IstListe`, `KannListe`, `Summenzeilen`, `BomItemViewModel.Arbeitsbereich`.
5. **View:** Umschalter im Auftragskopf (`Bom.cshtml:47-57`, `btn-group` mit zwei Links, `aria-current`, `filterText` mitnehmen,
   `confirm` bei gesetzten Haken). Bei `IstListe` rendert `Bom.cshtml` statt `#bomTable` das Partial `_AuftragListeTabelle.cshtml`
   (eigenes `<table id="bomTable" data-view-key="KommissionierauftragListe">` – gleiche Tabellen-Id, damit Filter-, Spalten-, Scan- und
   Druck-Skript sie finden –, eigene `view-config`/`column-config`, kein `data-parent-pos`, kein Chevron, kein Einrücken, BG-Badge).
   In Etappe A ohne Haken/Ist-Eingabe/Quell-Lagerplatz (Spalten leer, Hinweis „Buchen über Stückliste“ nur bis Etappe B).
   Baum-Skript läuft ohne `data-parent-pos`/`.bom-toggle` ins Leere – beim Dev-Lauf prüfen, sonst mit `if (!istListe)` umgehen.
6. **ColumnDefinitions:** `ViewConfig KommissionierauftragListe` (`SupportsReorder: true`, `SupportsSortDefault: false` wie `Bom`
   `:260`) + Lookup (`:1061`) + `ViewKeyDriftGuardTests`.
6a. **Summierter Auftragsdruck (K11, gehört zu A – reine Anzeige):** kein neuer Endpunkt. `AuftragDrucken` GET/POST
   (`PickingController.cs:602-613`) bekommen `string? ansicht` und `string? visibleColumns`; `RenderAuftragDruckenAsync` verzweigt bei
   `ansicht == "liste"` (und `KannListe` wie K3, sonst Druck je Position): Items über dieselbe Aufbereitung wie die Seite – den
   Item-Teil von `BomSeiteAsync` (`:283-382`) als private Methode herausziehen und von Seite und Druck nutzen, dann
   `KommissionierauftragListe.Summiere`; Reihenfolge: `reihenfolge` (Positionsliste, wie heute) auf die Summenzeilen anwenden
   (Summenzeile steht an der Stelle ihrer ersten genannten Position, fehlende am Ende – Muster `SortiereZeilen` `:674-683`).
   Spalten: `PrintVisibleColumns.ResolveAsync(visibleColumns, "KommissionierauftragListe", ColumnDefinitions.KommissionierauftragListe.Columns,
   …)` mit `[FromServices] IUserViewPreferenceRepository`. Neue View `Views/Picking/PrintAuftragListe.cshtml`: Aufbau und Stilblock 1:1
   aus `PrintSummiert.cshtml:1-93` übernommen (eine Gruppe = der Auftrag; Banner „Summierte Ansicht“; Kopf `PrintSummiert`-Stil mit
   KO-Nummer, Barcode `IBarcodeService.EncodeCode128Base64(auftrag.Nummer)`, FA, Bereich, Status, Fälligkeit aus der vorhandenen
   Berechnung `:634-642`), Spalten per `ShowCol` in der Standard-Reihenfolge der Liste. `PrintSummiert.cshtml` selbst bleibt unverändert
   (Antwort 6: bestehende Kommissionierliste nicht anfassen) – der kopierte Stilblock bekommt einen `ponytail:`-Kommentar
   („Stil aus PrintSummiert kopiert; gemeinsames Partial, wenn beide sich ändern“).
   Im Partial: Druckknopf `data-print-link="KommissionierauftragListe"` (+ `print-visible-columns.js` einbinden) und `ansicht=liste` in
   der href; das Sortier-POST-Skript (`Bom.cshtml:1404-1426`) übernimmt `action = getAttribute('href')` schon mit Query – im Dev-Lauf
   prüfen, dass `visibleColumns` auch im POST-Fall ankommt (Reihenfolge der beiden Klick-Handler), sonst als Formularfeld setzen; bei
   Summenzeilen alle Positionen aus `data-positionen` in die `reihenfolge` übernehmen.
6b. **PDF-Knopf (Antwort 10, Muster `PdfSummiert` `FaHierarchyKommissionierListenController.cs:333-366`):** kein neuer Endpunkt –
   `AuftragDrucken` GET/POST bekommen zusätzlich `bool pdf = false`. Bei `pdf && ansicht == "liste"`: dasselbe ViewModel wie 6a,
   `html = await viewRenderer.RenderToStringAsync(ControllerContext, "PrintAuftragListe", vm)`,
   `pdfRenderService.RenderHtmlToPdfAsync(html, HttpContext.RequestAborted)`, `File(…, "application/pdf",
   PdfFileNameBuilder.Build("Kommissionierauftrag", auftrag.Nummer, DateTime.Now))` (String-Überladung `Services/PdfFileNameBuilder.cs:13`);
   `IRazorViewRenderer` und `IPdfRenderService` per `[FromServices]` (Controller-Konstruktor bleibt). `PdfRenderException` →
   `TempData["WarningMessage"] = ex.UserMessage` und Redirect auf `Auftrag/{id}?ansicht=liste` (Muster `:361-366`). Knopf im Partial nur
   bei `vm.PdfVerfuegbar` (= `PdfRenderStatus.IsAvailable`, per `[FromServices]` in `Auftrag` gelesen), mit
   `data-print-link="KommissionierauftragListe"`; bei aktiver Sortierung denselben POST-Weg wie „Auftrag drucken“ mit `pdf=true` nehmen
   (die Positionsliste nicht in die GET-Query – Hotfix v1.30.1 HTTP 404.15, `PickingController.cs:495-497`).
7. **Tests A:** `KommissionierauftragListeTests` (Schlüssel inkl. Groß/Klein/Leerzeichen, Summen, gemischter Status, Sortierung mit
   leeren Werten, BG getrennt, mehrwertiger Arbeitsbereich, Reihenfolge nach Positionsliste), `PickingControllerTests` (Standard je
   Einstellung, `ansicht`-Parameter, ungültiger Wert, AKE/flach ohne Umschalter; `AuftragDrucken` mit `ansicht=liste` → View
   `PrintAuftragListe` mit Summenzeilen und `VisibleColumns` aus dem Parameter, ohne `ansicht` → `PrintPicking` unverändert; `pdf=true` mit gemocktem `IPdfRenderService` → `FileResult` `application/pdf`,
   `PdfRenderException` → Redirect mit Warnung). Die echte PDF-Erzeugung ist nicht InMemory-testbar → Manual-UAT (x.7a).

**Etappe B – Verteil-Buchung**

8. **Neu `Services/PickingOrderVerteilung.cs`** (static, DB-frei): `Verteile(IEnumerable<PickingOrderLine> zeilen, decimal ist)` →
   Liste `(Zeile, Menge)` nur mit Menge > 0, Zeilen nach `BomPosition` (`NaturalPositionComparer`), jede bis `SollMenge`; wirft
   `InvalidOperationException` bei `ist <= 0` oder `ist > Σ SollMenge` („Ist-Menge x größer als offenes Soll y“).
9. **`PickingSelectionItem.PickingItemIds`** (`List<int>?`, neu): gesetzt = Summenzeile, dann ist `PickingItemId` leer.
   `TransferOrderLinesAsync` expandiert solche Items **vor** der vorhandenen Prüfung (`PickingTransferService.cs:53`): Zeilen des Auftrags zu
   den Ids holen (fremde Id → vorhandene Meldung „gehört nicht zu Auftrag“), nicht offene Zeilen → vorhandene Meldung „bereits
   umgebucht … bitte Seite neu laden“ (Stand des Servers zählt, nicht der Seite), Ist-Text mit `TryMenge` parsen, `Verteile`, je Anteil
   ein `PickingSelectionItem` mit `PickingItemId`, gleichem `SourceStorageLocationId`, `IsBaugruppe` und `IstMenge` als Invariant-Text.
   Danach läuft die bestehende Prüfung und Buchung unverändert (Doppelauswahl-Prüfung `:55-56` greift auch über Summenzeilen).
10. **Skript:** `collectPickedItems` (`Bom.cshtml:1601-1618`): hat der Haken `data-ids`, `PickingItemIds` statt `PickingItemId` senden
    (`items[i].PickingItemIds[j]`). ~~Druck-Reihenfolge (`:1404-1420`): bei Summenzeilen alle Positionen aus `data-positionen` in
    Reihenfolge anhängen.~~ *Nach Antwort 9 in Etappe A (Schritt 6a).* Der Rest (Konflikt-Dialog, Status-Modal, Scan) bleibt.
11. **Partial:** Haken `.picking-checkbox` mit `data-ids` (offene Zeilen) und `data-is-baugruppe`, `.ist-menge` (vorbelegt `OffenSoll`,
    `aria-label` mit Artikel), `.source-location-select` (Vorschlag je Artikel), nur wenn `!ReadOnly` und offene Zeilen.
12. **Tests B:** `PickingOrderVerteilungTests` (genau, Unterdeckung mitten in der zweiten Zeile, Überdeckung, 0/negativ,
    Positionsreihenfolge „10, 9, 10.1“ natürlich), `PickingTransferServiceTests` (Summenzeile bucht n Bewegungen in einer Transaktion,
    Bestand über zwei Zeilen am selben Platz, eine Zeile inzwischen umgebucht → Abbruch ohne Buchung, gemischt Summen- und
    Einzelzeile, Doppel-Id über Summen- und Einzelzeile → Abbruch).
13. Frontend: Skill `frontend-design` ausdrücklich aufrufen. Umschalter groß genug für Tablet-Bedienung, Kontrast AA, aktive Maske
    nicht nur über Farbe; BG-Kennzeichen als Text.

#### Fassung vor den Freigabe-Antworten (überholt durch Antwort 2 „summiert“ und Antwort 1 Spalten)

Ponytail-Leiter: keine neue View, kein neuer Controller, kein neues JS-Modul. Eine Maske = dieselbe `Bom.cshtml` mit einem Schalter.

1. **Controller:** `Auftrag(int id, string? filterText, string? ansicht)`. `BomSeiteAsync` bekommt `ansicht`; nach dem Bau von `vm`:
   `vm.KannListe = vm.Hierarchical && await _appSettings.GetBoolAsync(FaHierarchyKommissionierlistenAktiv)` (vorhandenen Lesezugriff
   des Controllers nutzen, sonst `IAppSettingRepository` wie im Attribut `:45`); `vm.Ansicht = KannListe && ansicht != "stueckliste" ? Liste : Stueckliste`.
   Bei `Liste`: `Items` mit `OrderBy(Kommissionieren leer zuletzt).ThenBy(Hauptlagerplatz leer zuletzt).ThenBy(Position, NaturalPositionComparer)`,
   `TreeLevel`/Kindbeziehung für die Darstellung ignorieren.
2. **ViewModel** (`BomViewModels.cs`): `Ansicht` (enum oder bool `IstListe`), `KannListe`. Abteilung je Zeile: nur wenn Rückfrage 1
   sie will – Quelle `KommissionierListenService.AbteilungJeHauptFa` (`Services/KommissionierListenService.cs:94-96`) bzw. der HauptFA
   des Auftrags (eine Abteilung je Auftrag).
3. **View `Bom.cshtml`:** Umschalter im Kopf (`:47-57`). Bei `IstListe`: `data-view-key="KommissionierauftragListe"`, kein
   `data-parent-pos`, kein `display:none`, kein Einrück-Padding, kein Chevron; zweiter `column-config`-Block für die Liste. Die Zellen
   bleiben dieselben Partials/Markups (gleiche Klassen `.picking-checkbox`, `.ist-menge`, `.source-location-select`), damit das
   Umbuchungs- und Druck-Skript unverändert greift. Baum-Skript läuft ohne `data-parent-pos` ins Leere (prüfen, sonst `if`).
4. **ColumnDefinitions:** `ViewConfig KommissionierauftragListe` (`SupportsReorder: true`) + Lookup + `ViewKeyDriftGuardTests`.
5. Frontend: Skill `frontend-design` ausdrücklich aufrufen. Umschalter groß genug für Tablet-Bedienung, Kontrast AA, aktive Maske
   nicht nur über Farbe.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein Schema. Spaltenauswahl je Benutzer liegt in der vorhandenen Präferenz-Tabelle unter dem neuen View-Key (kein
Seed nötig, Default aus `column-config`). `SQL/00_FreshInstall.sql`, `SQL/AgentJobs/` unberührt.

Prüfabfrage (WMS-DB je Standort, nur lesen) – bestimmt, wo der Standard „Liste“ greift:

```sql
SELECT [Key], [Value] FROM dbo.AppSettings
WHERE [Key] IN (N'FaHierarchyKommissionierlistenAktiv', N'LeitstandAktiv');
-- Master hierarchisch steht in den ServiceSettings:
SELECT [Key], [Value] FROM dbo.ServiceSettings WHERE [Key] = N'ProduktionsauftragHierarchisch';
```

Tabellen-/Spaltennamen beim Ausführen gegen `SQL/00_FreshInstall.sql` prüfen (nicht aus dem Namen abgeleitet: AppSettings-Zugriff
über `AppSettingKeys`, ServiceSettings über `ServiceSettingDefinitions`).

## Einstellungen

Keine neue oder geänderte Einstellung. Gelesen wird die vorhandene:

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `FaHierarchyKommissionierlistenAktiv` (unverändert) | Bool | true/false (Auswahl) | false | zusätzlich: Standard-Maske „Liste“ (summiert je Artikel) am Kommissionierauftrag und Umschalter sichtbar | Menü, Kommissionierlisten, **neu** Kommissionierauftrag | IDEAL | keine Außenwirkung (Umbuchungen bleiben WMS-intern) |

Die Wirkungsbeschreibung in `AppSettingDefinitions.cs:258-259` („Menü, Kommissionierlisten“) um „Kommissionierauftrag: Standard-Maske Liste“
ergänzen (ADR 0020).

## Audit-Feld-Auswirkungen

~~Keine neuen Schreibwege.~~ *Präzisiert nach Antwort 2:* kein neuer Schreibweg, aber ein neuer Eingang in den vorhandenen: Die
Verteilung erzeugt nur Auswahl-Items, geschrieben wird ausschließlich in der unveränderten Buchungsschleife
(`PickingTransferService.cs:191-249`), die je Zeile `ModifiedAt/ModifiedBy/ModifiedByWindows`, `PickedAt/By/ByWindows`, `TransferredAt` und
an der Bewegung `CreatedAt/CreatedBy/CreatedByWindows` setzt (`:210-239`) und den Auftragskopf beim Übergang Offen → InArbeit
auditiert (`:243-249`). Abschließen und Fotos unverändert. Kommissionier-Umbuchungen gehen weiter nicht an Sage
(`PickingTransferService.cs:14-15`).

## Akzeptanzkriterien

1. IDEAL, Kommissionierlisten an: `/Picking/Auftrag/{id}` öffnet in „Liste“; Umschalter sichtbar, „Liste“ aktiv (hervorgehoben und `aria-current`).
2. Klick „Stückliste“: Baumansicht wie heute (Einrücken, Chevrons, je Zeile); Klick „Liste“ zurück; Filtertext bleibt.
3. Liste flach, **eine Zeile je Artikel + Komm.-Ziel**; Menge Soll = Summe der Auftragszeilen, Pos. listet alle Positionen; sortiert Komm.-Ziel → Hauptlagerplatz → erste Position → Artikel; Baugruppen mit „BG“, nicht eingerückt.
4. Sichtbare Spalten genau: Haken, Komm.-Ziel, Hauptlagerplatz, Pos., Artnr., Matchcode, Bezeichnung 1, Menge Soll, Menge Ist, Lagerplatz, Quell-Lagerplatz, Arbeitsbereich, Abteilung, Bezeichnung 2, Artikelgruppe, Beschaffung, Kategorie; Baugruppe, Sage-Pos. (und bei voller Struktur Ebene, Vater-Sub-FA) über das Zahnrad einblendbar.
5. Verteilung genau: Artikel mit zwei offenen Zeilen (Pos. 10 Soll 3, Pos. 20 Soll 2), Ist 5 → beide `Umgebucht` mit Ist 3 und 2, zwei Bewegungen.
6. Unterdeckung: dieselbe Lage, Ist 4 → Pos. 10 Ist 3 `Umgebucht`, Pos. 20 Ist 1 `Umgebucht` (teilgeliefert); Ist 2 → Pos. 10 Ist 2 `Umgebucht`, Pos. 20 bleibt `Offen`. Teilweiser Abschluss legt die Reste wie heute im Folgeauftrag an.
7. Überdeckung: Ist 6 → Meldung „größer als offenes Soll“, nichts gebucht (keine Bewegung, kein Zeilenstatus geändert).
8. Summenzeile mit bereits umgebuchter Zeile: verteilt nur auf die offenen; Anzeige „bereits gebucht x“; alle Zeilen erledigt → keine Eingaben, Statustext.
9. Zeile inzwischen von anderer Seite umgebucht → Buchung der ganzen Auswahl abgelehnt („bitte Seite neu laden“), nichts gebucht.
10. Wagen-Konflikt aus der Liste: Konflikt-Dialog wie aus der Stückliste, „trotzdem buchen“ bucht alle verteilten Zeilen.
11. Artikel-Scan in der Liste hakt die Summenzeile an; „Auftrag abschließen“ und Foto hochladen funktionieren aus der Liste.
11a. „Auftrag drucken“ aus der Liste: ein Dokument im Aufbau des PDF-Drucks der Kommissionierliste (Banner „Summierte Ansicht“, Kopf mit Barcode – hier KO-Nummer –, Tabelle im selben Stil), Inhalt = alle Summenzeilen des Auftrags in der Reihenfolge der Liste (auch nach Spaltensortierung), Spalten = genau die in der Liste eingeblendeten ohne Haken (Zahnrad: z. B. Kategorie ausblenden → fehlt im Druck; Sage-Pos. einblenden → erscheint).
11b. „Auftrag drucken“ aus der Stückliste: unverändert je Position (`PrintPicking`); dort kein PDF-Knopf.
11c. PDF-Knopf in der Liste (nur bei verfügbarem PDF-Dienst): lädt eine PDF-Datei „Kommissionierauftrag-<KO-Nummer>-…“ mit demselben Inhalt, derselben Reihenfolge und denselben Spalten wie 11a; ist der PDF-Dienst nicht verfügbar, fehlt der Knopf; schlägt das Rendern fehl, steht eine Warnmeldung auf der Auftragsseite (Liste).
12. Auftrag im Status Abgeschlossen: Liste schreibgeschützt (keine Haken/Eingaben), wie die Stückliste.
13. Spaltenauswahl der Liste ist unabhängig von der der Stückliste (eigener View-Key).
14. Kommissionierlisten aus (oder AKE): kein Umschalter, `?ansicht=liste` zeigt die Stückliste.
15. Haken gesetzt, Umschalter geklickt → Rückfrage; Abbrechen bleibt auf der Seite.
16. `/FaHierarchyKommissionierListen` (Liste, Summiert, Druck) unverändert, kein neuer Link.
17. Tests grün: `KommissionierauftragListeTests`, `PickingOrderVerteilungTests`, `PickingTransferServiceTests` (Summenzeile), `PickingControllerTests` (Standard je Einstellung, `ansicht`, ungültiger Wert), `ViewKeyDriftGuardTests`; Build grün.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Nummer beim Dev-Start prüfen; nach Teil 2), Verweise auf das KO-Auftrags-Kapitel
(Spec [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]]).

- **Vorbedingung:** IDEAL-Testsystem, `FaHierarchyKommissionierlistenAktiv = true`, freigegebener HauptFA mit offenem
  Kommissionierauftrag (≥ 5 Zeilen, verschiedene Ziele/Lagerplätze, eine Baugruppe, **mindestens ein Artikel an zwei Positionen
  mit gleichem Ziel**), Bestand am Quell-Lagerplatz, aktiver Kommissionierwagen, Benutzer mit Picking-Recht. Soll-Mengen der
  Doppel-Positionen vor dem Test notieren (Beispiel unten: 3 und 2).
- **x.1** Auftrag öffnen → Liste (AK 1), umschalten (AK 2), Summierung/Sortierung/BG (AK 3), Spaltensatz und Zahnrad (AK 4).
- **x.2** Verteilung genau (AK 5): Ist = Summe, umbuchen; in der Stückliste beide Positionen `Umgebucht`, Bestandsbewegungen prüfen.
- **x.3** Unterdeckung (AK 6) an einem zweiten Auftrag/Artikel: Ist 4 bzw. 2, danach „Auftrag abschließen – teilweise“ → Folgeauftrag
  enthält die Reste.
- **x.4** Überdeckung (AK 7): Ist > Summe → Meldung, nichts gebucht.
- **x.5** Teilweise gebuchte Summenzeile (AK 8): eine Position in der Stückliste umbuchen, zur Liste wechseln, Rest buchen.
- **x.6** Zwei Browser-Fenster, dieselbe Zeile in einem umbuchen, im anderen die Summenzeile buchen → Ablehnung (AK 9).
- **x.7** Wagen mit fremdem FA → Konflikt-Dialog, „trotzdem buchen“ (AK 10); Scan, Foto, Abschließen (AK 11).
- **x.7a** Druck aus der Liste (AK 11a): Kommissionierliste → Summiert → „PDF“ eines HauptFA öffnen als Vergleich; im KO-Auftrag
  (Liste) per Zahnrad eine Spalte aus- und eine einblenden, nach Hauptlagerplatz sortieren, Filtertext setzen, „Auftrag drucken“ →
  gleiches Aussehen wie das PDF, alle Summenzeilen trotz Filter, Reihenfolge und Spalten wie in der Liste. Umschalten auf Stückliste,
  „Auftrag drucken“ → Druck je Position wie bisher (AK 11b). Zurück zur Liste, PDF-Knopf → PDF-Datei mit gleichem Inhalt,
  Reihenfolge und Spalten wie der Browserdruck, neben das Kommissionierlisten-PDF legen (AK 11c).
- **Negativ x.7b:** PDF-Dienst aus/nicht verfügbar (DEV: wie bei der Kommissionierliste) → kein PDF-Knopf; Fehler beim Rendern →
  Warnmeldung, Auftragsseite bleibt in der Liste.
- **x.8** Abgeschlossenen Auftrag öffnen (AK 12); Spaltenauswahl Liste vs. Stückliste (AK 13); Umschalter mit gesetzten Haken (AK 15).
- **Negativ:** Einstellung aus bzw. AKE-Test: kein Umschalter, `?ansicht=liste` wirkungslos (AK 14); `/FaHierarchyKommissionierListen`
  unverändert (AK 16); Ist 0, „abc“ → Meldung, nichts gebucht.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| A | Umschalter, summierte Anzeige, summierter Auftragsdruck und PDF-Knopf (Schritte 1–7 inkl. 6a, 6b): Spalten, Sortierung, BG, Arbeitsbereich/Abteilung, ViewConfig, `PrintAuftragListe`, PDF über `IPdfRenderService`; Liste noch ohne Haken/Ist/Quell-Lagerplatz. Build + Tests grün, keine Migration. AK 1–4, 11a–11c, 12–14, 16. | offen | – |
| B | Verteil-Buchung (Schritte 8–12): `PickingOrderVerteilung`, `PickingItemIds`, Expansion in `TransferOrderLinesAsync`, Skript + Partial-Eingaben. Build + Tests grün, keine Migration. AK 5–11, 15, 17. Hängt an keiner offenen Rückfrage. | offen | – |

*Schnitt bestätigt (Antwort 7):* ein Lauf, ein Worktree (Bündel `2026-08-07-ideal-teile-1-5`), ein Merge; Etappen sind nur
Commits. Branch während der Arbeit mit `scripts/sync-worktree.ps1` aktuell halten. Etappe A allein ist nicht auslieferungsfähig (Standard
„Liste“ ohne Buchung) – deshalb Etappen statt getrennter Teil-Specs.

## Deploy

*Vorläufig, der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja
- **Service:** nein
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):

| Id | Version | Typ | Firma | Phase | Ausführung | Text |
|---|---|---|---|---|---|---|
| `v<Version>-KoAuftragListe` | (Dev-Lauf) | Info | IDEAL | Nachher | anzeigen | „Kommissionieraufträge öffnen bei aktiven Kommissionierlisten in der Maske „Liste“: je Artikel summiert, die Ist-Menge wird in Positionsreihenfolge auf die Positionen verteilt (bei zu wenig bleibt die letzte erreichte Position teilgeliefert). „Auftrag drucken“ aus der Liste druckt summiert mit den eingeblendeten Spalten. Umschalten auf „Stückliste“ oben im Auftrag.“ |

Keine SQL-Skripte, kein Daten-/Einmal-Skript, keine Prüfung (nichts SQL-prüfbar Neues), keine Einstellung zu schalten.

- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree).

## Offene Rueckfragen

1. **Spaltensatz „Liste“.** **Empfehlung:** sichtbar Haken, Komm.-Ziel, Hauptlagerplatz, Position, Artikelnummer, Matchcode,
   Bezeichnung 1, Menge Soll, Menge Ist, Lagerplatz, Quell-Lagerplatz; ausgeblendet Bezeichnung 2, Abteilung, Vater-Sub-FA,
   Sage-Position. Haupt-Artnr. und HauptFA der Kommissionierliste entfallen (ein Auftrag gehört zu genau einem FA, steht im Kopf).
2. **Zeilen je Auftragszeile statt summiert.** Die Kommissionierliste summiert je Artikel; Haken, Ist-Menge und Umbuchung arbeiten aber
   je `PickingOrderLine`. **Empfehlung:** je Auftragszeile. Summieren hieße, eine Ist-Menge auf mehrere Zeilen zu verteilen – neue
   Buchungslogik.
3. **Sortierung.** **Empfehlung:** Komm.-Ziel → Hauptlagerplatz → Position (Laufweg wie Kommissionierliste); über das Filterfeld
   und die Spaltensortierung weiter änderbar.
4. **Baugruppen.** **Empfehlung:** Baugruppen-Zeilen zeigen, die der Auftrag enthält, mit Kennzeichen „BG“, ohne Einrücken.
5. **Maske merken.** **Empfehlung:** nein; jeder Aufruf startet mit dem Standard, der Wechsel ist ein Klick. Merken je Benutzer
   wäre eine neue Präferenz.
6. **Link aus der Kommissionierliste zu den KO-Aufträgen.** **Empfehlung:** nicht in diesem Teil (eigener Backlog-Eintrag bei Bedarf);
   der Weg in den Auftrag bleibt Leitstand bzw. Kommissionierung → Aufträge.

*Rückfragen 1–6 sind beantwortet (siehe Freigabe-Antworten und Nachfragen 2026-11-26) und in Rumpf, AK und Entwurf übernommen. Neu nach
der Einarbeitung:*

7. **Schnitt (Größe).** Die summierte Liste mit Verteil-Buchung ist für einen Dev-Lauf groß (zwei neue reine Funktionen, Service-Eingang,
   Partial, Skript, Arbeitsbereich durch das BOM-Modell). **Empfehlung:** ein Epic in einem Lauf mit Etappe A (Anzeige) und Etappe B
   (Verteil-Buchung) als zwei Commits, ein Merge (Tabelle „Etappen“). Getrennte Teil-Specs nicht, weil A allein den Standard „Liste“
   ohne Buchung ausliefern würde.
8. **Bestellen / Lagerbestellung in der Liste.** Beide Knöpfe arbeiten je Position (`data-position`, `data-quantity`,
   `Bom.cshtml:382-433`, `:1850-1909`); in der Summenzeile wäre offen, welche Position und welche Menge gilt. **Empfehlung:** in der Liste
   nicht anbieten, dafür auf „Stückliste“ umschalten.
9. **„Auftrag drucken“ aus der Liste.** **Empfehlung:** Druck bleibt je Position (`PrintPicking`, `PickingController.cs:615-671`), nur
   die Reihenfolge folgt der Liste; ein summierter Auftragsdruck wäre ein eigener Backlog-Eintrag.

*Rückfragen 7–9 sind beantwortet (Freigabe-Antworten Runde 2; zu 9 abweichend: summierter Druck wie der PDF-Druck der
Kommissionierliste mit den eingeblendeten Spalten) und in Rumpf, AK, Entwurf und Etappen übernommen. Neu:*

10. **PDF-Knopf am Auftragsdruck.** Die Kommissionierliste bietet neben dem Browserdruck einen PDF-Download (`PdfSummiert`,
    `FaHierarchyKommissionierListenController.cs:333-361`, über `IPdfRenderService`). Der Auftragsdruck öffnet heute nur die
    Druckansicht im neuen Tab (`Bom.cshtml:488`). **Empfehlung:** nur Browserdruck (gleiches Aussehen, PDF über den Druckdialog
    möglich); ein PDF-Knopf wäre ein zweiter Weg mit `IPdfRenderService`/`PdfRenderStatus` und eigener Fehlerbehandlung.
    *Beantwortet (Runde 3): zusätzlich PDF-Knopf wie bei der Kommissionierliste – übernommen in K11, Schritt 6b, AK 11c, x.7a/x.7b.*

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Spaltensatz der Maske „Liste“ wie Empfehlung? → **welche spalten haben wir noch? Arbeitsbereich zb,... wenn mehr Spalten verfügbar über zahnrad ein/ausblendbar** (Gerald 2026-11-26, Chat)
2. Zeilen je Auftragszeile (nicht summiert)? → **nein, in der liste ist es summiert, sollten wir dann auch so schaffen können. Die buchung in der db darf auf positionsebene passieren** (Gerald 2026-11-26, Chat)
3. Sortierung Komm.-Ziel → Hauptlagerplatz → Position? → **gleiche sortierung wie gehabt** (Gerald 2026-11-26, Chat)
4. Baugruppen mit „BG“, ohne Einrücken? → **ja. Woe gehabt** (Gerald 2026-11-26, Chat)
5. Maske nicht merken? → **wie ghabt. Nein** (Gerald 2026-11-26, Chat)
6. Link Kommissionierliste → KO-Aufträge nicht in diesem Teil? → **siehe Nachfragen 2026-11-26 zu 6** (Gerald, Chat)

**Nachfragen 2026-11-26 (Gerald, Chat):**
- zu 1 (Spalten): zusätzlich sichtbar „Arbeitsbereich, Abteilung, Bezeichnung 2 / Artikelgruppe, Beschaffung / Kategorie“; alle
  übrigen Spalten über das Zahnrad ein-/ausblendbar.
- zu 2 (Ist-Menge in der summierten Liste): „Positionsfolge, wenn Restmenge sein sollte, ist diese Position nur teilgeliefert“ —
  Verteilung je Artikel auf die `PickingOrderLine`s in Positionsreihenfolge, jede bis zu ihrem Soll; die letzte erreichte Position
  bleibt bei Unterdeckung teilgeliefert.
- 6 (Link Kommissionierliste ↔ KO-Auftrag): „Eigentlich umgekehrt, die neue Kommissionierliste wird vom KO-Auftrag geöffnet, der User
  kann umschalten zwischen der neuen Liste und der bestehenden Kommissionierung. Für den ersten Implementierungsversuch wirklich die
  bestehende Kommissionierliste bestehen lassen und neu implementieren.“ — kein Link aus `/FaHierarchyKommissionierListen`; diese
  bleibt unverändert, die Liste-Maske wird am KO-Auftrag neu gebaut.

## Freigabe-Antworten Runde 2 (Rückfragen 7–9, nach Einarbeitung 2026-11-26)

7. Schnitt als Epic mit Etappe A (Anzeige) und B (Verteil-Buchung), ein Lauf, ein Merge? → **Ja, Epic A+B, ein Lauf, ein Merge** (Gerald 2026-11-26, Chat)
8. Bestellen/Lagerbestellung in der Liste nicht anbieten (nur Stückliste)? → **Ja, Bestellen/Lagerbestellung in der Liste weglassen** (Gerald 2026-11-26, Chat)
9. „Auftrag drucken“ bleibt je Position, Reihenfolge aus der Liste? → **Nein – „Auftrag drucken“ aus der Liste druckt summiert (wie die Liste)** (Gerald 2026-11-26, Chat)

## Freigabe-Antworten Runde 3 (Rückfrage 10)

10. Summierter Auftragsdruck nur als Browserdruck, ohne eigenen PDF-Knopf? → **Nein – zusätzlich eigener PDF-Knopf wie bei der Kommissionierliste (`PdfSummiert`-Muster)** (Gerald 2026-11-26, Chat)

**Nachfragen 2026-11-26 (Gerald, Chat) zum Druck (Antwort 9):** „Der Druck soll gleich ausschauen wie der PDF-Druck auf der
derzeitigen Kommissionierliste“ und „Halt um die Zeilen erweitert wie wir eingeblendet haben“ (gemeint: die eingeblendeten Spalten)
→ K11.

## Kritische Pruefung (2026-11-26)

Geprüft gegen Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (`AppVersion` 1.99.84). Antworten Runde 1–3 und
Nachfragen (Gerald, Chat 2026-11-26) sind vollständig; Antworten 2/9/10 weichen von der Empfehlung ab und sind in Rumpf, K-Tabelle, AK,
Entwurf und Etappen nachgezogen. Die Ist-Stand-Belege stimmen in den Stichproben (`PickingTransferService.cs:48-73`, `:188-260`,
`PickingController.cs:252-442`, `:602-683`, `PickingOrderWorkflow.cs:178-245`, `print-visible-columns.js:9-24`,
`ColumnDefinitions.cs:260`/`:1061`, `PdfFileNameBuilder.cs:13`, `PrintVisibleColumns.cs:20-22`).

### BLOCKER

- **B-1 Rest einer teilgebuchten Position ist in diesem Auftrag nicht mehr buchbar – gewollt?** Eine Zeile kann nur einmal gebucht
  werden (`PickingTransferService.cs:61-62`, `:228-231`). Beispiel AK 6 „Ist 2“: Pos. 10 (Soll 3) wird mit 2 `Umgebucht`, Pos. 20 (Soll 2)
  bleibt `Offen`. Die Summenzeile zeigt danach Soll 5, „bereits gebucht 2“, vorbelegt 2 – kommt später die fehlende Menge (3), lehnt
  die Verteilung sie ab („größer als offenes Soll 2“), obwohl rechnerisch 3 offen sind; der Rest 1 von Pos. 10 entsteht erst beim
  teilweisen Abschluss im Folgeauftrag (`PickingOrderWorkflow.cs:207-209`). Bei Buchung in mehreren Schritten „verliert“ die Summenzeile
  also scheinbar Menge. **Frage an Gerald:** (a) so akzeptiert – dann zeigt die Summenzeile sichtbar „Rest x aus Pos. 10 geht beim
  Abschluss in den Folgeauftrag“ (Melden statt still), AK 6/8 und x.3/x.5 nennen diesen Zustand; oder (b) bei Unterdeckung die
  angebrochene Zeile **nicht** teilbuchen, nur volle Zeilen buchen (widerspricht der Nachfrage zu 2 „nur teilgeliefert“); oder
  (c) Nachbuchung auf eine umgebuchte Zeile erlauben (zweite Bewegung je Zeile – Datenmodell-Änderung, eine `StockMovementId` je Zeile
  `Models/PickingOrder.cs:100`, eigener Teil)? Empfehlung: (a).

### SOLLTE

- **S-1 Gleichzeitiger Zugriff endet als HTTP 500.** `PickingOrderLine.RowVersion` ist Concurrency-Token (`ApplicationDbContext.cs:1767ff`,
  `IsRowVersion`), zwei gleichzeitige Buchungen (Doppelklick, zwei Geräte) buchen also nie doppelt – aber die
  `DbUpdateConcurrencyException` wird weder im Service (`PickingTransferService.cs:256-261` wirft weiter) noch in `AuftragUmbuchen`
  (`PickingController.cs:469` fängt nur `InvalidOperationException`) behandelt → Fehlerseite; das Skript zeigt `alert(xhr.responseText)`
  mit HTML (`Bom.cshtml:1664-1666`). Der Umbuchen-Knopf hat keine Sperre (anders als der Abschluss mit `window.wmsSperren`). Mit
  Summenzeilen bucht ein Klick mehrere Zeilen, das Risiko steigt. Vorschlag: in `TransferOrderLinesAsync` `DbUpdateConcurrencyException`
  → `InvalidOperationException` „Auftrag wurde inzwischen geändert – nichts gebucht, bitte Seite neu laden“ (Muster
  `WarehousePickingController.cs:402`, `BfAuftragWorkflow.cs:389`), Submit über `wmsSperren` sperren; AK 9 um den echten Gleichstand
  ergänzen (Manual-UAT, nicht InMemory-testbar).
- **S-2 Meldungstext AK 9 ≠ „vorhandene Meldung“.** Schritt 9 nennt „vorhandene Meldung ‚bereits umgebucht … bitte Seite neu laden‘“, der
  Code sagt „… ist bereits umgebucht oder abgeschlossen.“ (`PickingTransferService.cs:62`, ohne „neu laden“). Vorschlag: in der Expansion
  eigener Text mit „bitte Seite neu laden“, AK 9 auf genau diesen Text.
- **S-3 Standardfilter und Filter-Skript der Stückliste für die Liste nicht entschieden.** `Bom.cshtml:1098-1320` belegt aus dem
  Benutzerprofil still Beschaffung/Artikelgruppe/Bezeichnung 1/Komm.-Ziel vor (Spaltenschlüssel `procurement`, `article-group`,
  `description1`, `kommissionieren`), ersetzt Komm.-Ziel durch ein Select2-Mehrfach-Dropdown mit „Alle Ziele“, zeigt Badges und einen
  Leerzustand und klappt Baum-Äste auf. Wird „Liste“ Standard, fallen diese Filter je nach Schlüsseln im Partial still weg oder wirken
  unerwartet auf Summenzeilen. Vorschlag: Partial nutzt dieselben `data-col-key`; Standardfilter, Badges, Komm.-Ziel-Dropdown und
  Leerzustand gelten auch in der Liste (AK + Testschritt in x.1); `expandAncestorsOfMatching`/`updateBomVisibility` ohne
  `data-parent-pos` ausdrücklich prüfen.
- **S-4 `visibleColumns` im POST-Fall als Regel festlegen statt „prüfen“.** `print-visible-columns.js:11-23` ändert `a.href` im eigenen
  Klick-Handler, das Sortier-Skript liest `getAttribute('href')` (`Bom.cshtml:1418`); wer zuerst läuft, hängt von der Registrierreihenfolge
  ab. Vorschlag: im POST-Weg (Druck und PDF) `visibleColumns` selbst aus dem `thead` lesen und als Formularfeld senden – Muster des
  `PrintBom`-Handlers (`Bom.cshtml:1398`, `addField('visibleColumns', …)`).
- **S-5 Summierter Druck verliert Zeilen, die der heutige Druck zeigt.** `RenderAuftragDruckenAsync` druckt heute **alle**
  `auftrag.Zeilen` (`PickingController.cs:654`); Schritt 6a nimmt die Items aus `BomSeiteAsync`, das nur Stücklistenpositionen mit
  Auftragszeile kennt (`:288-293`). Zeilen ohne passende Stücklistenposition (Stückliste geschrumpft, „Stückliste kleiner“) fehlen dann
  in Liste und summiertem Druck still. Vorschlag: solche Zeilen als eigene Summenzeilen (ohne Ziel) mitführen oder per Banner „x Zeilen
  ohne Stücklistenposition – siehe Stückliste“ in Liste und Druck melden; AK ergänzen.
- **S-6 Mehrwertige Werte im Druck.** K11 nennt „Quell-Lagerplatz = Text“ (Einzahl). Eine Summenzeile kann Zeilen mit verschiedenen
  gespeicherten Quellen haben (Negativ-Ausweichplatz je Zeile, `PickingTransferService.cs:199-204`, K9). Vorschlag: Quell-Lagerplatz (und
  Status) im Druck über `VormontageService.FormatTexte` wie die übrigen mehrwertigen Felder (K4a).
- **S-7 Test „eine Transaktion“ ist InMemory nicht prüfbar** (InMemory ohne Transaktion, `PickingTransferService.cs:188-189`). Vorschlag
  für `PickingTransferServiceTests`: prüfen, dass bei Fehler in der zweiten verteilten Zeile (Bestand reicht nur für die erste,
  Negativbuchung aus) **keine** Bewegung und kein Zeilenstatus gespeichert ist.
- **S-8 Regressions-AK für die gemeinsam genutzte View fehlt.** `Bom.cshtml` trägt auch `/Picking/Bom/{id}` (FA-Stückliste, Vorbau,
  FA-Vervollständigung, `PrintBom`). AK 2/11b decken nur die Auftragsseite. Vorschlag: AK „`/Picking/Bom/{id}` inkl. Druck/Lagerbestellung
  sowie Auftragsseite in ‚Stückliste‘ unverändert (Baum, Filter-Badges, Bestellen)“ und je ein Testschritt.

### HINWEIS

- **H-1** `NaturalPositionComparer` sortiert `null` **zuerst** (`Services/NaturalPositionComparer.cs:13`); K4 verlangt „leere zuletzt“ –
  `Summiere` muss das selbst regeln; `Verteile` bucht Zeilen ohne Position zuerst (bei FaHierarchie praktisch ausgeschlossen).
- **H-2** `zeileByKey` nimmt je (Artnr, Pos.) die erste Zeile (`PickingController.cs:288-290`); zwei Stücklisten-Items mit gleichem
  Schlüssel ergäben doppeltes Soll und dieselbe Id zweimal (→ „mehrfach ausgewählt“, `PickingTransferService.cs:55-56`). Im
  FaHierarchie-Pfad macht `Unique` die Positionen eindeutig (`FaHierarchyBomRepository.cs:92`, `:144`) – billige Absicherung: in
  `Summiere` je `PickingItemId` deduplizieren, ein Testfall.
- **H-3** Rückfrage 3 begründet die Sortierung mit „Laufweg wie Kommissionierliste“ – nicht belegt: die Liste sortiert über den
  Struktur-Builder je HauptFA/Position, Summiert nach Artnr → Ziel (`KommissionierListenService.cs:270-272`). „Gleiche Sortierung wie
  gehabt“ ist im Stil der Antworten 4/5 („wie gehabt“ = einverstanden) als Zustimmung zur Empfehlung gelesen – plausibel, aber gedeutet.
- **H-4** Bestandsmeldung nennt „Benötigt“ je verteilter Zeile, nicht die Summe (`PickingTransferService.cs:202-203`); das
  Negativ-Ausweichen geschieht ganzzeilig, nicht anteilig – in x.2/x.3 erwähnen.
- **H-5** Doppelausführung nacheinander ist sicher: die zweite Anfrage liest frisch und scheitert am Zeilenstatus (`:61-62`); Abschluss
  gegen Umbuchung ist über `RowVersion` der Zeilen geschützt (`PickingOrderWorkflow.cs:199-205` ändert die offenen Zeilen) – Folge nur S-1.
- **H-6** `depends_on` Teil 2 (Leitstand) ohne gemeinsame Dateien laut `affected_code` – nur Reihenfolge im Bündel.
- **H-7** Größe: rund 15 Dateien plus der große Skriptteil von `Bom.cshtml`; als Epic A/B in einem Lauf machbar, Hauptrisiko ist das
  geteilte Seiten-Skript (S-3, S-8).
- **H-8** Deploy (Web ja, Service/Migration nein, Info-Manifest) plausibel. Testbuchungen erzeugen echte WMS-Bewegungen auf dem
  IDEAL-Testsystem (keine Sage-Wirkung, `PickingTransferService.cs:14-15`) – in der Vorbedingung erwähnen.

**NACHBESSERUNG NOETIG: B-1 (Rest teilgebuchter Position bei schrittweiser Buchung) mit Gerald klären; S-1, S-3, S-5 in Rumpf und AK übernehmen.**

**Antwort Gerald 2026-11-26 (Chat) zu B-1 der Kritischen Prüfung:** „Akzeptieren, Rest anzeigen“ – teilgebuchte Position zeigt ihren
Rest sichtbar; Nachbuchen in diesem Auftrag nicht möglich, Rest geht beim Teilabschluss in den Folgeauftrag (Variante a).
