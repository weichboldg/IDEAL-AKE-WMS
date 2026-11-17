---
type: spec
title: Beschichtungslogik je Standort - Quelle fuer Kennzeichen und Termin, ein Resolver, Beschichtungsauftrag auch an der AKE
slug: 2026-11-16-beschichtungslogik-je-standort-spec
status: InUmsetzung
created: 2026-11-16
updated: 2026-11-17
source_backlog: "[[2026-11-16-beschichtungslogik-je-standort]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - IdealAkeWms/Models/AppSettingDefinitions.cs
  - IdealAkeWms/Models/AppSettingKeys.cs
  - IdealAkeWms/Services/CoatingDateCalculator.cs
  - IdealAkeWms/Services/Beschichtung/ (neu, Resolver)
  - IdealAkeWms/Controllers/ProductionOrdersController.cs
  - IdealAkeWms/Controllers/PickingLeitstandController.cs
  - IdealAkeWms/Controllers/FaWorklistController.cs
  - IdealAkeWms/Views/ProductionOrders/_ProductionOrderRow.cshtml
  - IdealAkeWms/Views/ProductionOrders/Index.cshtml
  - IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml
  - IdealAkeWms/Views/PickingLeitstand/Index.cshtml
  - IdealAkeWms/Views/FaWorklist/_FaWorklistRow.cshtml
  - IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs
  - IdealAkeWms/Filters/RequireFaHierarchyBeschichtungAktivAttribute.cs
  - IdealAkeWms/Services/BeschichtungsauftragWorkflow.cs
  - IdealAkeWms/Services/BeschichtungsauftragService.cs
  - IdealAkeWms/Services/BeschichtungsauftragVergabeAnzeige.cs
  - IdealAkeWms/Controllers/FaHierarchyBeschichtungController.cs (Gate unveraendert, keine Master-Bedingung; Sammelmeldung S-2 an Index und Summiert, Nachtrag 2)
  - IdealAkeWms/Models/ViewModels/FaHierarchyBeschichtungViewModels.cs (Sammelmeldung S-2, Nachtrag 2)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Summiert.cshtml
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml
  - IdealAkeWms/Data/Repositories/IBeschichtungsauftragRepository.cs (PositionsSchluessel mit Artnr fuer die flache Quelle, Nachtrag 2)
  - IdealAkeWms/Data/Repositories/BeschichtungsauftragRepository.cs (GetVergabenAsync liest Artnr in den Schluessel, Nachtrag 2)
  - IdealAkeWms/Controllers/BeschichtungsauftraegeController.cs (SchluesselText/TrySchluessel mit Artnr und PositionText, Nachtrag 2/3)
  - IdealAkeWms/Services/BomQuantityResolver.cs (nur Aufruf, unveraendert; Mengenregel der Kommissionierung, Nachtrag 2)
  - IdealAkeWms/Models/BeschichtungsauftragPosition.cs (neue Spalte PositionText nvarchar(50) NULL, Nachtrag 3)
  - IdealAkeWms/Models/FaHierarchyNode.cs (nur [NotMapped] PositionText fuer die flache Projektion, kein Schema, Nachtrag 3)
  - IdealAkeWms/Data/ApplicationDbContext.cs (PositionText HasMaxLength(50), Nachtrag 3)
  - IdealAkeWms/Migrations/<ts>_AddBeschichtungsauftragPositionText.cs (neu, Nachtrag 3)
  - SQL/157_AddBeschichtungsauftragPositionText.sql (neu, Nummer beim Start pruefen, Nachtrag 3)
  - SQL/00_FreshInstall.sql (Spalte + MigrationId, Nachtrag 3)
  - IdealAkeWms/Services/BeschichtungsauftragWorkflow.cs (Snapshot/Kopie uebernehmen PositionText, Nachtrag 3)
  - IdealAkeWms/Services/FaHierarchyListBuilder.cs (Positionsreihenfolge zusaetzlich nach PositionText natuerlich, Nachtrag 3)
  - IdealAkeWms/Models/ViewModels/BeschichtungsauftraegeViewModels.cs (Summiere uebernimmt PositionText, Nachtrag 3)
  - IdealAkeWms/Views/Beschichtungsauftraege/Details.cshtml (Anzeige/Sortierung PositionText, Nachtrag 3)
  - IdealAkeWms/Views/Beschichtungsauftraege/Reklamation.cshtml (Anzeige/Sortierung PositionText, Nachtrag 3)
  - IdealAkeWms/Services/NaturalPositionComparer.cs (nur wiederverwendet, unveraendert, Nachtrag 3)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
  - IdealAkeWms/Views/Users/RoleOverview.cshtml
  - IdealAkeWms/Views/StandortEinstellungen/Index.cshtml (nur Hinweis-Link auf /Settings, Freigabe-Antwort 14)
  - IdealAkeWms/Views/Shared/_Layout.cshtml
  - IDEALAKEWMSService/Services/CoatingDetectionService.cs
  - IDEALAKEWMSService/Services/SageImportService.cs
  - IDEALAKEWMSService/Services/FaMaterializationSyncService.cs
  - IDEALAKEWMSService/Workers/SyncWorker.cs
  - scripts/deploy/nachlauf.psd1
  - docs/TESTSZENARIEN.md
  - docs/betrieb/04-konfiguration.md
open_questions: []
beantwortete_fragen:
  - "1 -> BEANTWORTET: zwei Choice-AppSettings, Default Automatisch; Termin-Quelle SData nur als Erweiterungspunkt vorgesehen, nicht gebaut (Freigabe-Antwort 1); Feld USER_OSBeschichtet gehoert laut Freigabe-Antwort 17 zur Kennzeichen-Quelle"
  - "2 -> BEANTWORTET: Server lehnt unpassende Kombinationen ab (Freigabe-Antwort 2)"
  - "3 -> BEANTWORTET: IDEAL-Termin auch in Leitstand/Arbeitsliste, E4, im Changelog (Freigabe-Antwort 3)"
  - "4 -> BEANTWORTET: Start_Beschichtung ist in Sage bereits berechnet und wird unveraendert uebernommen, keine WMS-Berechnung darauf; Tooltip nennt die Quelle (Freigabe-Antwort 4)"
  - "5 -> BEANTWORTET: Kombigeraet leer wie heute (Freigabe-Antwort 5)"
  - "6 -> BEANTWORTET: Berechnet an der IDEAL mit Kopf-FE_Termin (Freigabe-Antwort 6)"
  - "7 -> BEANTWORTET: Aktiv-Kennung IDEAL immer aktiv; P-I1 durch Gerald, Haltepunkt H1 jetzt vor E2 statt vor E4 (Freigabe-Antwort 7, verlegt durch Nachtrag 2 / S-1)"
  - "8 -> BEANTWORTET: AKE-Positionen = Lackierteile der WA-Stueckliste, Masse 0, Dienstleister/RAL/Retour manuell, Start aus berechnetem Termin (Freigabe-Antwort 8)"
  - "9 -> BEANTWORTET: AKE-WA-Nummern numerisch, HauptFA bleibt int, kein SQL/152; P-A3 nur als Beleg vor E5 (Freigabe-Antwort 9)"
  - "10 -> BEANTWORTET: Vorschlagsliste auch an der AKE, gleich wie IDEAL, Datenbasis WA-Stueckliste, Kreditor/Beschichter leer und manuell (Freigabe-Antwort 10)"
  - "11 -> BEANTWORTET: Key bleibt, nur Anzeigename/Gruppe/Standort (Freigabe-Antwort 11)"
  - "12 -> BEANTWORTET: Hook respektiert Sync:CoatingDetectionEnabled (Freigabe-Antwort 12)"
  - "13 -> BEANTWORTET: Erkennungsfenster nicht in diesem Epic, eigene Aufgabe (Freigabe-Antwort 13)"
  - "14 -> BEANTWORTET: nur /Settings, Standort-Maske mit Hinweis-Link (Freigabe-Antwort 14)"
  - "15 -> BEANTWORTET: View an der AKE nicht waehlbar bis P-A5; P-A5 vor E5 durch Gerald (Freigabe-Antwort 15)"
  - "16 -> BEANTWORTET: v1.99.25 nach 1.99.24 Sicherheit, Nummern beim Start pruefen (Freigabe-Antwort 16)"
  - "17 -> BEANTWORTET: USER_OSBeschichtet ist Erweiterungspunkt der Kennzeichen-Quelle, nicht der Termin-Quelle; nichts wird gebaut (Freigabe-Antwort 17, Nachtrag 2)"
  - "18 -> BEANTWORTET: Menge ueber BomQuantityResolver wie die Kommissionierung, keine eigene Formel; Haltepunkt H3 Mengen-Pruefschritt vor E5 (Freigabe-Antwort 18, Nachtrag 2)"
  - "19 -> BEANTWORTET: beschichtet wird zum berechneten Termin, vor dem Kommissionierabschluss; Cache-Fenster ohne IsDonePicking bleibt, keine eigene Aufgabe (Freigabe-Antwort 19, eingearbeitet in Nachtrag 3)"
  - "20 -> BEANTWORTET: neue Spalte PositionText als Positionsschluessel mit Artnr (Freigabe-Antwort 20); P-A6 belegt 3.812 von 4.444 nicht ganzzahlige, teils mehrstufige Positionen, 0 Doppelungen (Nachtrag 3, Auftrag Gerald 2026-11-17)"
epic: true
etappen:
  - "E1 Katalog + Resolver (ohne Verhaltensaenderung)"
  - "H1 Haltepunkt P-I1 (Gerald erhebt) vor E2 (Nachtrag 2, S-1)"
  - "E2 Web-Konsumenten lesen nur noch den Resolver (inkl. Aktiv-Kennung)"
  - "E3 Dienst schreibt das Kennzeichen nach der effektiven Quelle"
  - "E4 IDEAL-Termin im Leitstand und in der Werkbank-Arbeitsliste"
  - "H2 Haltepunkt P-A5, P-A3, P-A6 (erhoben 2026-11-17, Nachtrag 3)"
  - "H3 Haltepunkt Mengen-Pruefschritt: ein WA mit Baugruppe, Kommissionierliste gegen Sage/OSEON (Gerald), vor E5 (Nachtrag 2, B-1)"
  - "E5 Beschichtungsauftrag und Vorschlagsliste an der AKE (Gate entkoppeln, flache Datenbasis, Spalte PositionText SQL/157)"
  - "E6 Doku, Testszenarien, Hilfe, Changelog, Manifest, Brain"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: "Empfehlungen 1–16, Antworten Gerald zu 4, 9, 10"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-16"
---

## Ziel / Nutzen (das Warum)

Woher „beschichtet“ und der Beschichtungstermin kommen, ist heute je Standort hart verdrahtet und auf mehrere Stellen
verteilt: Die AKE erkennt Lackierteile ueber die OSEON-Artikelkategorie, die IDEAL ueber die FA-Struktur-View. Der Termin
wird an drei Stellen unterschiedlich ermittelt, einmal davon als kopierte Formel. Das hat Folgen:

- Die IDEAL sieht nur in der FA-Liste einen Termin, nicht im Leitstand und nicht in der Arbeitsliste.
- Die AKE kann weder die Beschichtungs-Vorschlagsliste noch den Beschichtungsauftrag nutzen.

Ziel:

1. **Eine** Standort-Einstellung legt die Quelle fest.
2. **Ein** Resolver liefert Kennzeichen, Termin und Aktiv-Kennung an alle Verbraucher.
3. Die Defaults behalten das heutige Verhalten je Standort bei.
4. Danach werden die Features standortunabhaengig: Vorschlagsliste und Beschichtungsauftrag an der AKE, der View-Termin an der IDEAL in allen drei Listen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

- Zwei Choice-AppSettings (entschieden, Freigabe-Antwort 1):
  - `BeschichtungKennzeichenQuelle`
  - `BeschichtungTerminQuelle`
- Beide mit Default `Automatisch`, der das heutige Verhalten aus dem Master `ProduktionsauftragHierarchisch` ableitet.
- Resolver `BeschichtungResolver` (Web-Projekt; der Dienst nutzt die reine Quellen-Aufloesung ueber die ProjectReference).
- Alle Verbraucher lesen nur noch den Resolver:
  - FA-Liste
  - Kommissionier-Leitstand
  - Werkbank-Arbeitsliste
  - Zeilen-Views (Aktiv-Kennung)
  - Beschichtungsauftrag-Kopfvorbelegung
- Der Dienst schreibt `HasCoatingParts` nur noch aus der effektiven Kennzeichen-Quelle. Das Klasse-D-Gate wird als Folge dieser Quelle neu eingeordnet.
- Das Audit-Loch in `SetCoatingPartsAsync` wird geschlossen.
- Der IDEAL-Termin kommt in Leitstand und Arbeitsliste (E4, Rueckfrage 3).
- Das Gate `FaHierarchyBeschichtungAktiv` wird von der IDEAL entkoppelt. Beschichtungs-Vorschlagsliste (`FaHierarchyBeschichtungController`) und Beschichtungsauftrag bekommen an der AKE eine flache Datenbasis aus der WA-Stueckliste; Bedienung gleich wie an der IDEAL (E5, Freigabe-Antworten 8 und 10).

**Out-of-Scope**

- Schreiben an Sage, Aenderungen an Sage-Views.
- Einstellungen auf Test-/Prod-Systemen setzen.
- Rollen: bleiben unveraendert, `beschichtungsauftrag` / `beschichtungsauftrag_schreiben`.
- ~~Beschichtungs-Vorschlagsliste an der AKE (Rueckfrage 10).~~ *ueberholt durch Freigabe-Antwort 10: die Vorschlagsliste kommt an der AKE (In-Scope, E5).*
- ~~Schema-Aenderung am Beschichtungsauftrag: keine, die AKE-WA-Nummern sind numerisch, `HauptFA` bleibt `int` (Freigabe-Antwort 9).~~ *ueberholt durch Freigabe-Antwort 20 (Nachtrag 3):* `HauptFA` bleibt `int` (Freigabe-Antwort 9, P-A3 = 0). Einzige Schema-Aenderung ist die neue Spalte `BeschichtungsauftragPositionen.PositionText` (In-Scope, E5, SQL/157).
- ~~Termin-Quelle `SData` (Freigabe-Antwort 1): nur als Erweiterungspunkt vorgesehen, **nicht gebaut**, kein Auswahlwert in dieser Version. Der Choice-Katalog und `ResolveQuellen` sind so geschnitten, dass ein weiterer Wert spaeter nur einen Katalogeintrag und einen Resolver-Zweig braucht. Zuordnung des Feldes `USER_OSBeschichtet` zur Achse ist offen (Rueckfrage 17 im Nachtrag).~~ *ueberholt durch Freigabe-Antwort 17 (Nachtrag 2):*
- SData-Quelle: **nicht gebaut**, kein Auswahlwert in dieser Version, kein spekulativer Code. `USER_OSBeschichtet` (Lesetest L10, `sage-sdata-benutzerfelder-lesetest.md:1073`) ist der dokumentierte Erweiterungspunkt der **Kennzeichen-Quelle** (A1), **nicht** der Termin-Quelle. Fuer die Termin-Quelle ist keine SData-Datumsquelle bekannt (`Start_Beschichtung` ueber SData nicht gefunden, L11 `:1105-1107`). Der Choice-Katalog und `ResolveQuellen` brauchen fuer einen spaeteren Wert nur einen Katalogeintrag und einen Resolver-Zweig; das ist eine Eigenschaft des Entwurfs, kein Bauauftrag.
- Erkennungsfenster-Filter (Rueckfrage 13, eigene Aufgabe).
- Der tote Code `BomCacheRepository.GetArticleNumbersWithCoatingPartsAsync` (`IdealAkeWms/Data/Repositories/BomCacheRepository.cs:50-69`, nur Test-Aufrufer). Er wird gemeldet, nicht mitgebaut.

## Fachliche Anforderungen

### Ist-Stand (belegt; Pfade relativ zum Worktree)

**Kennzeichen „beschichtet“: zwei getrennte Schreibwege, beide Ziel `ProductionOrderPickingStatus.HasCoatingParts`**

`IdealAkeWms/Models/ProductionOrderPickingStatus.cs:40-44`; Spalten mit DF-Constraint `SQL/00_FreshInstall.sql:440-441`.

AKE (flach): `IDEALAKEWMSService/Services/CoatingDetectionService.cs`

- `:40-46` Im hierarchischen Modus uebersprungen (Klasse-D-Gate ueber `IHierarchicalModeReader`).
- `:56-74` liest das AppSetting `LackierteilKategorieName` per Raw-SQL. Ist es leer, endet der Lauf ohne Schreiben.
- `:76-116` Auftragsauswahl:
  - entweder gezielt ueber `specificOrderIds`,
  - oder ueber das Fenster `IsDone=0 AND ProductionDate <= +Sync:BomCacheWeeks` mit `TOP Sync:BomCacheMaxOrders`.
  - Das Fenster filtert weder `IsDonePicking` noch `IsCancelled`.
- `:149-156` Treffer, wenn die Kette `CachedBomHeaders → CachedBomItems → Articles (Ressourcenummer) → ArticleCategories.Name = @cat` greift.
- `:173-184` DryRun schreibt nichts.
- `:247-262` Update:
  - true: `HasCoatingParts=1 WHERE HasCoatingParts=0`
  - false: `HasCoatingParts=0, IsCoatingDone=0 WHERE HasCoatingParts=1`
  - Audit: `ModifiedBy='CoatingDetection'`, `ModifiedByWindows=SYSTEM_USER`, `ModifiedAt=DateTime.Now`.

Aufrufer von `CoatingDetectionService`:

- `IDEALAKEWMSService/Workers/SyncWorker.cs:242-265`, Gate `Sync:CoatingDetectionEnabled` (Default false, `IdealAkeWms/Models/ServiceSettingDefinitions.cs:123`, `:288`).
- `IDEALAKEWMSService/Services/SageImportService.cs:198-215`, Hook fuer neue Auftraege. Er ist **nur** an `Sync:BomCacheEnabled` gebunden, **nicht** an `CoatingDetectionEnabled` (Befund, Rueckfrage 12).

Weitere Schreibwege und Quellen:

- `SageImportService.cs:171-190` legt die Statuszeile mit `HasCoatingParts=0, IsCoatingDone=0` an.
- Kategoriequelle ist `OseonSyncService.SyncArticleCategoriesToWmsAsync` (`IDEALAKEWMSService/Services/OseonSyncService.cs:685-797`, `SyncWorker.cs:99`, Gate `Sync:OseonArticleCategoryEnabled`).
- Daneben setzt `ArticlesController.cs:194` die Kategorie von Hand.

IDEAL (hierarchisch):

- `IDEALAKEWMSService/Services/SageFaStrukturReader.cs:88`, `:117` ist der **einzige** Schreiber von `FaHierarchyNode.Beschichtet`.
  - Er liest `[Beschichtet]` aus der View `Sync:FaHierarchyListeViewName` (Default `[vw_IDEAL_AKE_WMS_FAListe]`, `:25-30`; SQL in `FaHierarchySql.cs:139-163`).
  - Mapping `:181-195`: `'Ja'`, `'true'`, `-1`, `1` und jede Zahl ungleich 0 ergeben true; NULL ergibt false.
- Laut Repo-Doku ist die Spalte `smallint -1/0`, „Position bevorzugt, Beleg als Fallback“ (`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql:43`). Der CREATE-Text liegt nicht im Repo, die Herkunft ist deshalb **unbelegt** (P-I4).
- `SdataFaStrukturReader.cs:22-35` liefert **kein** `Beschichtet` und keine FAInfos (G11). Er ist heute gesperrt; wird er freigeschaltet, fehlt das Kennzeichen.
- `IDEALAKEWMSService/Services/FaMaterializationCoating.cs` (`Derive`): ein Sub-FA ist beschichtet, wenn er **selbst** beschichtet ist oder ein direktes **Blatt**-Kind (`SubFA=0`) beschichtet ist (fallstricke §20).
- `FaMaterializationSyncService.cs:199-213` gibt die Map an `ProductionOrderPickingStatusRepository.SetCoatingPartsAsync`.
  - Kein Schreiben bei DryRun (`:112-125`) und bei uebersprungenem Plan (`:93-104`).
  - Gate ist der Master in `SyncWorker.cs:357-369`.
- `SetCoatingPartsAsync` (`IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs:155-186`):
  - setzt das Flag,
  - setzt `IsCoatingDone` zurueck, wenn das Flag auf false kippt (`:171-176`, „Fallstrick #11“ der alten Zaehlung),
  - setzt **nur** `ModifiedAt`, `ModifiedBy` bleibt leer (`:180-181`, Audit-Loch).

Manueller Schreibweg fuer `IsCoatingDone`:

- `PickingStatusApiController.cs:8-15`, `:42-44`, `[RequirePickingAccess]`, Route `api/picking-status/toggle`.
- Weiter ueber `SetFieldAsync` (`ProductionOrderPickingStatusRepository.cs:32-66`, mit Audit).
- Die Checkbox steht in `_PickingLeitstandRow.cshtml:104-112` und erscheint nur bei `HasCoatingParts`.

Kein aktiver AgentJob schreibt die Flags (einzige Fundstelle: `SQL/AgentJobs/_archiv/01_Import_Produktionsauftraege.sql`).

**Beschichtungstermin: drei verschiedene Wege, kein gespeichertes Feld am FA**

- `IdealAkeWms/Services/CoatingDateCalculator.cs` rechnet so:
  - kein VK-Termin → null
  - `featureActive && !hasCoatingParts` → null
  - sonst VK-Termin minus `BeschichtungTage` Arbeitstage, dann `FindPreviousPickupDay` (`BeschichtungAbholtage`).
- Aufrufer des Calculators:
  - `PickingLeitstandController.cs:173-175`
  - `FaWorklistController.cs:264-274`
  - Beide nehmen das Basisdatum aus `ProductionOrder.ProductionDate`.
- **FA-Liste** (`ProductionOrdersController.cs`):
  - Formel **inline kopiert** statt Calculator (`:209-214`).
  - Flacher Zweig `:408-425`: berechnet.
  - Hierarchischer Zweig `:217-227`: `BeschichtungTermin = HasCoatingParts ? groupCoatingStart : null`.
    - `groupCoatingStart` ist `FaHierarchyOrderInfo.Start_Beschichtung` des **einzigen** Kopfes (`:294-313`, Schluessel `int.TryParse(OrderNumber)` `:271-274`).
    - Kombigeraet (mehr als ein Kopf, `:314-329`): kein Termin.
- **Korrektur zum Backlog:** Es gibt ein Datumsfeld aus der View. `FaHierarchyOrderInfos.Start_Beschichtung` und `Beschichten_Retour` sind `DATETIME2` (`SQL/00_FreshInstall.sql:1970-1971`, Model `FaHierarchyOrderInfo.cs:33-34`).
  - Gelesen werden sie aus der View `Sync:FaHierarchyInfosViewName` (Default `[vw_IDEAL_AKE_WMS_FAInfos]`, `SageFaStrukturReader.cs:137-167`).
  - Die Repo-Doku nennt als Quelle `[PPSImport].[dbo].[vw_PPSData]`; `Start_Beschichtung` = „Wann Beschichtung starten“, `Dienstleister` = „Wer beschichtet“ (`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAInfos.sql:9`, `:31-36`). Diese Bedeutungen stammen nur aus der Anhangsbeschreibung → P-I4/P-I5.
  - Die IDEAL-FA-Liste zeigt diesen Termin seit v1.47.x schon (TS-71.11, `docs/TESTSZENARIEN.md:7648-7663`).
- **Luecke IDEAL:**
  - Sub-FAs entstehen ohne `ProductionDate` (`FaMaterializationSyncService.cs:144-156`). `ProductionDate` schreiben nur der Sage-Import (`SageImportService.cs:63`, `:138`) und die Abgleichs-SQL (`SageProductionOrderSql.cs`).
  - Folge: Leitstand und Arbeitsliste zeigen an der IDEAL **keinen** Termin. Der hierarchische Leitstand-Zweig `PickingLeitstandController.cs:194-276` reicht keinen Kopf durch.
  - Ob `Sync:ProductionOrdersEnabled` an der IDEAL aus ist, ist ungeprueft (P-I1).

**Aktiv-Kennung (Spalte und Haken sichtbar)**

- `_ProductionOrderRow.cshtml:16-20` und `_PickingLeitstandRow.cshtml:82-94`: `featureInactive = LackierteilKategorieName leer` und `showBeschicht = featureInactive || HasCoatingParts`.
- Die Controller reichen dieselbe Kennung weiter (`ProductionOrdersController.cs:139-147`, `PickingLeitstandController.cs:103-111`, `FaWorklistController.cs:200-203`).
- An der IDEAL haengt die Sichtbarkeit damit an einer AKE-Einstellung (fallstricke `:207-211`: leer = Termin fuer alle).

**Beschichtungsauftrag: komplett auf der IDEAL-Struktur**

- Gate:
  - `BeschichtungsauftraegeController.cs:28-29`: `[RequireBeschichtungsauftragAccess][RequireFaHierarchyBeschichtungAktiv]`; Schreib-Actions zusaetzlich `[RequireBeschichtungsauftragWriteAccess]`.
  - `RequireFaHierarchyBeschichtungAktivAttribute.cs:41-61`: nur `"true"`. Prueft den Master **nicht**; an der AKE einschaltbar, aber leer.
  - Menue `_Layout.cshtml:77`.
  - Key in `AppSettingDefinitions.cs:193-194` (Gruppe „IDEAL FA-Hierarchie“, Standort IDEAL).
  - Standort-Maske `StandortSettingsCatalog.cs:61`.
- Daten aus `FaHierarchyNodeRepository` / `IFaHierarchyOrderInfoRepository`:
  - `BeschichtungsauftragWorkflow.cs:88-120` (Anlage aus `GetByHauptFaAsync(int).Where(Beschichtet)`)
  - `:262-268` (Pruefung „nicht beschichtet“)
  - `:404-425` (Kopf aus FAInfos: Dienstleister, RAL, Start, Retour)
  - `:440-450` (Snapshot mit Massen)
- Schluessel sind ints:
  - `Beschichtungsauftrag.cs:20` `int HauptFA`
  - `BeschichtungsauftragPosition.cs:17-20`
  - `IBeschichtungsauftragRepository.cs:10` `PositionsSchluessel(int? VaterFA, int SubFA, int? Position)`
- `CachedBomItem` hat weder Masse noch eine int-Position (`Position` ist string, `IdealAkeWms/Models/CachedBomItem.cs:17`); `SortOrder` ist int (`:40`), `Menge` decimal (`:31`). Der Stuecklistenkopf haengt an der Artikelnummer, nicht am WA (`CachedBomHeader.cs:17`).

**Vorschlagsliste: ebenfalls komplett auf der IDEAL-Struktur** (Nachtrag 2026-11-16, belegt)

- Gate: `FaHierarchyBeschichtungController.cs:28-29` dieselben zwei Filter wie der Auftrag (`[RequireBeschichtungsauftragAccess][RequireFaHierarchyBeschichtungAktiv]`), **keine** Master-Bedingung.
- Menue: beide Eintraege (Vorschlagsliste, Auftraege) unter derselben Bedingung `hasBeschichtungsauftragAccess && faBeschichtungAktiv` (`_Layout.cshtml:153-164`).
- Daten: `BeschichtungsauftragService.BuildAsync` (`:94-111`) und `BuildSummiertAsync` (`:158-174`) lesen `_nodes.GetAllAsync()` und `_orderInfos.GetAllAsync()`; das Flag-Praedikat ist `FaHierarchyNode.Beschichtet` (`:86`). Die Aggregation `AggregateSummiert` (`:185-242`) ist DB-frei und arbeitet auf `FaHierarchyNode`.
- Vergabe-Anzeige: `BeschichtungsauftragVergabeAnzeige.cs:43` liest ebenfalls `FaHierarchyNode` mit `Beschichtet`.
- Kopfzeile je HauptFA: `_BeschichtungKopf.cshtml:10-30` zeigt `FaHierarchyOrderInfo` (Dienstleister, RAL, Start, Retour); ohne Kopfdaten den Hinweis „Keine Auftrags-Kopfdaten zu diesem HauptFA.“ (`:27-30`).
- „Aus Vorschlagsliste erstellen“ ruft `BeschichtungsauftraegeController.Create(int hauptFa, ...)` (`:139-155`) → `_workflow.AnlegenAsync(hauptFa)`.
- Spaltenkataloge tragen „(IDEAL)“ im Anzeigenamen (`ColumnDefinitions.cs:379`, `:404`).
- `FaHierarchyNode`: `HauptFA` int, `VaterFA` int?, `SubFA` int, `Position` int?, `Sollmenge`/`Breite` decimal, `Beschichtet` bool (`IdealAkeWms/Models/FaHierarchyNode.cs:19-56`).

**Mengenregel der Kommissionierung** (Nachtrag 2, belegt)

- `BomQuantityResolver.Resolve` (`IdealAkeWms/Services/BomQuantityResolver.cs:15-16`): `result.MengeIstAuftragsmenge ? item.Menge : item.Menge * order.Quantity`. Eingabe ist ein `BomQueryResult` plus `BomItem` (ViewModel, `Models/ViewModels/BomViewModels.cs:3`), **kein** `CachedBomItem`.
- Aufrufer: Kommissionierliste `PickingController.cs:355` und `:553`, Kommissionierauftrag `PickingOrderWorkflow.cs:84`, lesende Stueckliste `ReadOnlyBomBuilder.cs:108`.
- Weg an der AKE: `IBomRepository` → `BomRepositoryMasterSwitch`, flach → `CachedBomRepository` (`Program.cs:100-107`) → `BomRepository.GetBomItemsAsync`, zuerst der Cache (`BomRepository.cs:28-30`, `BomCacheRepository.GetByArticleNumberAsync` `BomCacheRepository.cs:20-47`). Erst ohne Cache liest er live die Sage-View `vw_AKE_Kommissionierung_StuecklistenDB` (`BomRepository.cs:33-52`), dann OSEON (`:55-57`).
- Der Cache liefert `new BomQueryResult(items, header.Source)` (`BomCacheRepository.cs:47`), also `MengeIstAuftragsmenge = false` (Default `BomViewModels.cs:3`). Damit gilt an der AKE `Menge × Quantity` fuer **jede** Zeile. `Resolve` kennt keinen Zweig fuer `Baugruppe`.
- IDEAL zum Vergleich: `FaHierarchyBomRepository` liefert `MengeIstAuftragsmenge: true` (`FaHierarchyBomRepository.cs:14`, `:61`). `Sollmenge` ist dort schon die Auftragsgesamtmenge.
- Ob die AKE-Stuecklistenmenge auch bei Zeilen mit `Baugruppe` je Stueck Endartikel gilt, steht nur in Sage/OSEON. Das prueft H3. Die Kommissionierung rechnet heute schon so, H3 prueft deshalb dieselbe Regel an der Kommissionierliste.

**BomCache-Fenster und Kennzeichen** (Nachtrag 2, belegt; Grundlage fuer S-2 und Rueckfrage 19)

- Das Cache-Fenster nimmt nur `IsDone = 0 AND IsCancelled = 0`, **ohne** komm-erledigte (`IsDonePicking = 1`), mit `ProductionDate <= +Sync:BomCacheWeeks` (`BomCacheSyncService.cs:412-424`) und `TOP Sync:BomCacheMaxOrders` (`:57-58`).
- Koepfe, deren Artikel kein Fensterauftrag mehr traegt, werden geloescht (`:66`, `:147`, `DeleteOrphanHeadersAsync` `:595-621`).
- Das Erkennungsfenster der Lackierteil-Erkennung nimmt dagegen nur `IsDone = 0` (`CoatingDetectionService.cs:103-110`). Ein Auftrag im Erkennungsfenster ohne Lackierteil-Treffer im Cache wird auf `HasCoatingParts = 0, IsCoatingDone = 0` gesetzt (`:162-170`, `:196-199`, `:255-262`), auch wenn sein Kopf nur wegen des Cache-Fensters fehlt.
- `CachedBomHeaders.Artikelnummer` ist eindeutig (`ApplicationDbContext.cs:1098`). `CachedBomItem.Position` ist string(50) (`CachedBomItem.cs:16-17`, `ApplicationDbContext.cs:1104`). Beim Inhaltswechsel wird `SortOrder` neu vergeben (Kritische Pruefung S-4).
- Vorhandener Schluessel „Artikelnummer + Position“ der Kommissionierung: `PickingOrderRestmengen.Schluessel(articleNumber, bomPosition)` = (Artikelnummer getrimmt und gross, Position getrimmt) (`PickingOrderRestmengen.cs:16-17`). Doppelte Schluessel in einer Stueckliste kommen vor und werden dort summiert (`:35-38`).
- Gespeicherter Beschichtungsauftrag: `BeschichtungsauftragPosition.Position` ist `int?`, `Artnr` ist string(200) (`BeschichtungsauftragPosition.cs:20`, `:23`). Der Vergabe-Schluessel `PositionsSchluessel(VaterFA, SubFA, Position)` traegt keine Artikelnummer (`IBeschichtungsauftragRepository.cs:10-14`). Er wird aus dem Speicher gelesen (`BeschichtungsauftragRepository.cs:40-42`) und als Text `VaterFA|SubFA|Position` durch Formulare gereicht (`BeschichtungsauftraegeController.cs:322-333`).

**Stuecklisten-Position als Text** (Nachtrag 3, belegt im Worktree, HEAD v1.99.33)

- Daten (P-A6, AKE-Test, 2026-11-17): 3.812 von 4.444 Lackierteil-Zeilen haben eine nicht ganzzahlige Position, teils mehrstufig (`5.2`, `51.5.2`); doppelte Schluessel Artnr + Position: 0. Damit ist `int(CachedBomItem.Position)` nicht moeglich.
- Schreiber von `CachedBomItems.Position`, alle als Text:
  - Dienst, Sage: `reader.GetValue(1).ToString()`, NULL wird `""` (`IDEALAKEWMSService/Services/BomCacheSyncService.cs:314`), Quelle `[ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]` mit `ORDER BY Artikelnummer, Position` (`:291-296`).
  - Dienst, OSEON-Fallback: `SafeString(reader, "Position")` (`:362`).
  - Schreiben per `SqlBulkCopy` in `[dbo].[CachedBomItems]`, Spalte `Position` als `string` (`:540-583`); `SortOrder` ist die Lesereihenfolge (`:552-565`). Weil die Quelle nach dem Text sortiert, ist auch `SortOrder` keine natuerliche Reihenfolge (`51.5.10` vor `51.5.2`, sofern die Quellspalte Text ist; P-A6 zeigt mehrstufige Werte, also Text).
  - Web: `BomCacheRepository.UpsertBomAsync` uebernimmt `BomItem.Position` (`IdealAkeWms/Data/Repositories/BomCacheRepository.cs:127-141`).
- Spaltenbreite: `CachedBomItem.Position` ist `[MaxLength(50)]` (`IdealAkeWms/Models/CachedBomItem.cs:16-17`, `ApplicationDbContext.cs:1106`). Laengere Quellwerte koennen nicht im Cache stehen. Dieselbe Breite hat der vorhandene Positionsschluessel der Kommissionierauftraege `BomPosition` (`ApplicationDbContext.cs:1726`, Index `:1737`).
- Der Cache hat den Text also schon; `CachedBomItem` braucht **keine** neue Spalte.
- Kein Code parst eine Stuecklisten-Position als `int` (Suche nach `int.Parse`/`int.TryParse`/`Convert.ToInt32` mit `Position` im ganzen Worktree: kein Treffer). `int(CachedBomItem.Position)` stand nur im Spec-Text (E5 ist nicht gebaut; `PositionsSchluessel` hat heute drei Komponenten, `IBeschichtungsauftragRepository.cs:10`).
- Natuerliche Sortierung gibt es schon: `IdealAkeWms/Services/NaturalPositionComparer.cs:8-34` (Split an `.`, Segmente numerisch, sonst `OrdinalIgnoreCase`, kuerzerer Pfad zuerst), getestet in `IdealAkeWms.Tests/Services/NaturalPositionComparerTests.cs`. Verwendet von Kommissionierliste (`PickingController.cs:381`, `:567`, `:684`) und lesender Stueckliste (`ReadOnlyBomBuilder.cs:126`).
- Stellen, die `BeschichtungsauftragPosition.Position` bzw. `FaHierarchyNode.Position` fuer Schluessel, Anzeige oder Reihenfolge nutzen:
  - Schluessel: `IBeschichtungsauftragRepository.cs:10-14`, `BeschichtungsauftragRepository.cs:40-42`, `BeschichtungsauftraegeController.cs:322-343` (Text und Parser, erwartet genau drei Teile `:330`), `Views/Beschichtungsauftraege/Details.cshtml:314`, `:317` (Checkbox-Wert und HTML-Id aus `SchluesselText`), `Views/FaHierarchyBeschichtung/Index.cshtml:101` (Vergabe-Badge).
  - Uebernahme beim Speichern: `BeschichtungsauftragWorkflow.cs:440-450` (`Snapshot`), `:452-462` (`Kopie`); Rueckprojektion fuer den Druck `Models/ViewModels/BeschichtungsauftraegeViewModels.cs:129-137` (`Summiere`).
  - Reihenfolge: `FaHierarchyListBuilder.cs:161` (Positionen je HauptFA-Gruppe), `BeschichtungsauftragService.cs:200` (erste Position der Summiert-Zeile), `Details.cshtml:230`, `Reklamation.cshtml:34`.
  - Anzeige: `Details.cshtml:233`, `:247` (aria-label), `:318`; `Reklamation.cshtml:38`; Meldetexte ueber `PositionsSchluessel.ToString()` (`IBeschichtungsauftragRepository.cs:14`, verwendet in `BeschichtungsauftragWorkflow.cs:110`, `:241`, `:250`, `:267-271`, `:431`, `:438`).
  - Die Vorschlagsliste zeigt keine Positionsspalte (`Views/FaHierarchyBeschichtung/Index.cshtml:91-109`, `ColumnMap` ohne `position`, `BeschichtungsauftragService.cs:71-83`).
- Index und Eindeutigkeit: `BeschichtungsauftragPositionen` hat nur einen **nicht** eindeutigen Index `(HauptFA, VaterFA, SubFA, Position)`; die Doppelvergabe prueft der Workflow (`ApplicationDbContext.cs:1787-1789`, FreshInstall `SQL/00_FreshInstall.sql:2873-2875`). `GetVergabenAsync` filtert nur nach `HauptFA` und vergleicht den Rest im Speicher (`BeschichtungsauftragRepository.cs:37-43`).
- Sortieren und Paging laufen im Speicher: Die Vorschlagsliste laedt alle Knoten (`BeschichtungsauftragService.cs:100`, `:161`), sortiert und pagt je HauptFA-Gruppe in C# (`FaHierarchyListBuilder.cs:141-161`, `BeschichtungsauftragService.cs:215-227`). Die Positionsreihenfolge beeinflusst die Seitenbildung nicht.
- AgentJobs: kein Skript unter `SQL/AgentJobs/` nennt `Beschichtungsauftrag` (Suche ohne Treffer).

### Soll

| ID | Anforderung | Begruendung / Quelle |
|---|---|---|
| A1 | **Kennzeichen-Quelle** je Standort waehlbar: `Automatisch` / `Artikelkategorie` / `FaStruktur`. `Automatisch` = `FaStruktur`, wenn der Master `ProduktionsauftragHierarchisch` an ist, sonst `Artikelkategorie`. Erweiterungspunkt (nicht gebaut): eine spaetere SData-Quelle ueber `USER_OSBeschichtet` gehoert zu **dieser** Achse. | Fokus 2a; Default = heute; Freigabe-Antwort 17 |
| A2 | **Termin-Quelle** je Standort waehlbar: `Automatisch` / `Berechnet` / `View`. `Automatisch` = `View` hierarchisch, sonst `Berechnet`. ~~Ein Wert `SData` ist vorgesehen, aber nicht Teil dieser Version (kein Auswahlwert).~~ *ueberholt durch Freigabe-Antwort 17:* kein `SData`-Wert; `USER_OSBeschichtet` ist kein Termin. | Fokus 2b; Default = heute; Freigabe-Antworten 1, 17 |
| A3 | **Ein Resolver** liefert je FA: `Beschichtet` (bool), `Termin` (DateTime?), `TerminQuelle`, `Aktiv` (Spalte/Haken relevant). Kein Verbraucher liest mehr `LackierteilKategorieName`, `BeschichtungTage`, `BeschichtungAbholtage` oder `Start_Beschichtung` selbst. | Fokus 3 |
| A4 | `Termin` bei Quelle `View` = `Start_Beschichtung` des eindeutigen Kopfes, **unveraendert uebernommen**, nur wenn `Beschichtet`. `Start_Beschichtung` ist in Sage bereits berechnet; das WMS rechnet nichts darauf (kein `CoatingDateCalculator`, keine `BeschichtungTage`, kein Abholtag). Der Calculator geht vom Vorkommissionier-Termin aus (`CoatingDateCalculator.cs:11-24`), nicht von `Start_Beschichtung`. Bei Kombigeraet oder leerem Feld: null, **nie** ein Ersatztermin (TS-71.11b). | Bestand, Freigabe-Antworten 4, 5 |
| A5 | `Termin` bei Quelle `Berechnet` = heutige Formel (`CoatingDateCalculator`), Basisdatum = Vorkommissioniertermin aus dem FA-Datum. Hierarchisch ist das FA-Datum der Kopf-`FE_Termin` (wie die FA-Liste `:199`, `:305`). | Rueckfrage 6 |
| A6 | `Aktiv`: bei Quelle `Artikelkategorie` = `LackierteilKategorieName` nicht leer (heute), bei `FaStruktur` = immer true. | Rueckfrage 7 |
| A7 | Der Dienst schreibt `HasCoatingParts` nur aus der **effektiven** Kennzeichen-Quelle: `CoatingDetectionService` laeuft nur bei `Artikelkategorie`, die Materialisierung setzt das Flag nur bei `FaStruktur`. Der Reset von `IsCoatingDone` beim Kippen auf false bleibt auf beiden Wegen. | Fokus 1/6 |
| A8 | Der Server lehnt beim Speichern Kombinationen ab, die keine Datenbasis haben: `Artikelkategorie` bei Master an, `FaStruktur` bei Master aus, `View` bei Master aus (bis P-A5). Ein ungueltiger Bestandswert wird beim Lesen wie `Automatisch` behandelt und protokolliert (ADR 0020 Punkt 2). | Rueckfrage 2/15 |
| A9 | IDEAL: FA-Liste, Leitstand und Arbeitsliste zeigen denselben Termin aus dem Resolver. | Fokus 5, Rueckfrage 3 |
| A10 | Beschichtungs-Vorschlagsliste und Beschichtungsauftrag an der AKE nutzbar, Bedienung gleich wie an der IDEAL (Liste der zu beschichtenden Teile, daraus „Aus Vorschlagsliste erstellen“). Gate `FaHierarchyBeschichtungAktiv` ohne Standortbindung (Standort „beide“), **keine** Master-Bedingung an der Vorschlagsliste. Datenbasis an der AKE = Lackierteile der WA-Stueckliste (die Kette aus `CoatingDetectionService.cs:149-156`). Kreditor/Beschichter (`Dienstleister`), RAL und Retour bleiben leer und werden manuell befuellt; Masse 0; Start aus dem berechneten Termin. `HauptFA` bleibt `int`. Rollen unveraendert. | Fokus 4, Freigabe-Antworten 8-11 |
| A11 | Alle Bestands-Szenarien TS-71.10, TS-71.10a, TS-71.11 und die Leitstand-/Arbeitslisten-Termine bleiben mit `Automatisch` unveraendert gruen. | Regression |
| A12 | Menge der AKE-Positionen = `BomQuantityResolver.Resolve` (`BomQuantityResolver.cs:15-16`), dieselbe Regel wie die Kommissionierung; keine eigene Formel. Fuer den AKE-Cache heisst das `Menge × ProductionOrder.Quantity` fuer jede Zeile, auch mit gefuellter `Baugruppe`. Vor E5 bestaetigt Gerald die Regel am Beispiel (H3). | Freigabe-Antwort 18, B-1 |
| A13 | „Offen“ in der flachen Quelle = `ProductionOrders.IsDone = 0 AND ProductionOrders.IsCancelled = 0` (Felder „Erledigt“ `ProductionOrder.cs:83-84` und „Storniert“ `:102-103`). `ProductionOrderPickingStatus.IsDonePicking` (`ProductionOrderPickingStatus.cs:47`) filtert **nicht**. | Nachtrag 2, S-3 |
| A14 | Positionsschluessel der flachen Quelle = Artikelnummer (Ressourcenummer der Zeile) + Stuecklisten-Position, nicht `SortOrder`. *Praezisiert durch Nachtrag 3:* Die Position ist der **Text** aus `CachedBomItem.Position` (z. B. `5.2`, `51.5.2`) und wird in der neuen Spalte `BeschichtungsauftragPosition.PositionText` gespeichert; `Position` (`int?`) bleibt an der AKE leer. | Nachtrag 2, S-4; Freigabe-Antwort 20, P-A6 |
| A15 | Faellt ein offener WA mit `HasCoatingParts = true` aus dem Stuecklisten-Cache, zeigt die Vorschlagsliste eine Sammelmeldung mit Anzahl und Nummern, und der Logger schreibt eine Warnung. Kein stilles Weglassen. | Nachtrag 2, S-2 |
| A16 | Positionen mit `PositionText` werden ueberall, wo Beschichtungs-Positionen geordnet werden (Vorschlagsliste je HauptFA, Summiert erste Position, Auftrag-Details, Reklamationsauswahl), **natuerlich nach Ebenen** sortiert: `5.2 < 5.10`, `51.5.2 < 51.5.10`, `51 < 51.5`. Regel = vorhandener `NaturalPositionComparer`; keine zweite Sortierregel. An der IDEAL (nur `int`-Position) bleibt die Reihenfolge unveraendert. | Nachtrag 3, Auftrag Gerald 2026-11-17 |

## Technischer Loesungsentwurf

**Ponytail-Leiter:**

- Wiederverwendet wird:
  - der `CoatingDateCalculator` (bleibt die einzige Formel),
  - die `Choice`-Infrastruktur der AppSettings (`AppSettingDefinitions.cs`, Muster `QrErstesFeld` `:109-111`, `TryGetWerte` `:233-237`),
  - die Raw-SQL-AppSetting-Lesung des Dienstes (`CoatingDetectionService.cs:58-59`),
  - das Resolver-Muster (`IdealAkeWms/Services/PrePickingDaysResolver.cs`).
- Neu ist nur der Resolver selbst. Gerald will ausdruecklich **einen**.

### Einstellungsort: AppSettings, nicht ServiceSettings

- Beide Seiten lesen den Wert: das Web (Anzeige, Termin) und der Dienst (Schreiben des Flags).
- Die fachlichen Termin-Schalter liegen schon heute in AppSettings (`BeschichtungTage`, `BeschichtungAbholtage`, `LackierteilKategorieName`, Gruppe „Termine und Beschichtung“ `AppSettingDefinitions.cs:61-69`). Der Dienst liest diese Gruppe bereits.
- ServiceSettings sind fuer Lauf-Gates und Takte da (ADR 0008).
- Die Wahl ist eine Fachentscheidung je Standort, also AppSettings (ADR 0011).

### Varianten Schluessel (Rueckfrage 1)

| | Variante 1: ein Key `Beschichtungslogik` (`Automatisch` / `OseonBerechnet` / `FaStrukturView` / `FaStrukturBerechnet`) | Variante 2 (empfohlen): zwei Keys |
|---|---|---|
| Wertebereich | Kombinationen ausgeschrieben; jede neue Quelle verdoppelt die Liste | je Achse 3 Werte |
| Fachlich | Kennzeichen und Termin haengen nicht zusammen. Die IDEAL kann `Berechnet` brauchen, wenn PPS-Termine fehlen (P-I2) | entspricht Fokus 2a/2b |
| Validierung | trivial | eine Kombinationsregel (A8) |

**Entschieden: Variante 2** (Freigabe-Antwort 1, Default `Automatisch`). Begruendung der Empfehlung: Gerald hat die Trennung in Kennzeichen- und Termin-Quelle selbst vorgegeben. Der eigentliche Schalter ist die Termin-Quelle; die Kennzeichen-Quelle ist durch das Auftragsmodell faktisch festgelegt (A8). Sie bekommt trotzdem einen eigenen Key, damit die Quelle sichtbar und dokumentiert ist und eine spaetere dritte Quelle keinen Umbau braucht.

### Resolver

Datei `IdealAkeWms/Services/Beschichtung/BeschichtungResolver.cs`. Bewusst ohne Interface, solange es nur eine Implementierung gibt; in den Tests mit echten InMemory-Repositories wie `PrePickingDaysResolver`.

1. `static BeschichtungQuellen ResolveQuellen(string? kennzeichenRaw, string? terminRaw, bool hierarchisch)`
   - Reine Funktion, `Automatisch` und ungueltige Werte werden aufgeloest.
   - Wird **auch vom Dienst** genutzt; dort ist das die einzige Stelle, die zwischen Kategorie- und Struktur-Weg entscheidet.
2. `Task<BeschichtungKontext> LadenAsync()`
   - Laedt einmal je Request: Settings, Feiertage, Abholtage, Master, aufgeloeste Quellen.
3. `BeschichtungInfo Bestimmen(BeschichtungKontext k, bool hasCoatingParts, DateTime? faDatum, FaHierarchyOrderInfo? eindeutigerKopf)`
   - Liefert `(Beschichtet, Termin, TerminQuelle, Aktiv)`.
   - `Beschichtet` ist immer `HasCoatingParts`; das Flag bleibt die eine materialisierte Wahrheit, die Quelle bestimmt nur, **wer** es schreibt.
   - `Termin` nach A4/A5: Bei `View` gibt der Resolver `eindeutigerKopf.Start_Beschichtung` 1:1 zurueck und ruft den `CoatingDateCalculator` **nicht** auf; nur bei `Berechnet` laeuft der Calculator mit dem Vorkommissionier-Termin aus `faDatum`.
   - `TerminQuelle` traegt die tatsaechlich genutzte Quelle (`Berechnet` / `View`); daraus entsteht der Tooltip-Text.

**Erweiterungspunkt SData** (Freigabe-Antwort 17, nur Doku, kein Code): Eine spaetere Quelle `USER_OSBeschichtet` waere ein weiterer Wert der **Kennzeichen-Quelle**: ein Katalogeintrag, ein Zweig in `ResolveQuellen` und ein Schreiber fuer `HasCoatingParts` nach A7. Die Termin-Quelle bleibt davon unberuehrt. Der Dev-Lauf legt dafuer weder Enum-Wert noch Platzhalter noch Interface an.

### Verbraucher

| Verbraucher | heute | danach |
|---|---|---|
| FA-Liste `ProductionOrdersController.cs:139-147`, `:209-227`, `:408-425` | inline-Formel + Kopf-Start | `Bestimmen(...)`, Kopf aus dem vorhandenen `headsByFa` (`:274-275`) |
| Leitstand `PickingLeitstandController.cs:103-111`, `:173-175` | Calculator mit `ProductionDate` | `Bestimmen(...)`. Hierarchisch: Koepfe per `GetByHauptFaKeysAsync` einmal je Seite nachladen (Muster `:271-275`) |
| Arbeitsliste `FaWorklistController.cs:200-203`, `:264-274` | Calculator mit `ProductionDate` | wie Leitstand |
| Zeilen-Views (`_ProductionOrderRow` `:16-20`, `:100-120`; `_PickingLeitstandRow` `:82-94`; `_FaWorklistRow` `:51`) | `LackierteilKategorieName` im View | `Aktiv` und `Termin` aus dem ViewModel |
| Spaltenkopf-Tooltips `ProductionOrders/Index.cshtml:106`, `PickingLeitstand/Index.cshtml:140` | Formeltext fest | Text je `TerminQuelle`: `View` = „aus der Sage-View, Feld Start_Beschichtung, unveraendert uebernommen“; `Berechnet` = Formeltext wie heute |
| Beschichtungsauftrag-Kopf `BeschichtungsauftragWorkflow.cs:404-425` | FAInfos | IDEAL unveraendert; AKE: Start aus `Bestimmen(...).Termin` (Quelle `Berechnet`), Dienstleister/RAL/Retour leer (E5) |
| Vorschlagsliste `BeschichtungsauftragService.cs:94-111`, `:158-174`; `BeschichtungsauftragVergabeAnzeige.cs:43` | `FaHierarchyNode` mit `Beschichtet` | Positionsquelle nach `ResolveQuellen(...).Kennzeichen`: `FaStruktur` wie heute, `Artikelkategorie` = flache WA-Stuecklisten-Projektion (E5). Kopfzeile an der AKE leer (`_BeschichtungKopf.cshtml:27-30`). AKE zusaetzlich: Sammelmeldung fuer gekennzeichnete WAs ohne Cache-Positionen (A15) an `Index` und `Summiert` (`FaHierarchyBeschichtungController.cs:62-89`, `:152-180`) |

Die Datumsfilter `coating-date` (`ProductionOrdersController.cs:485`, `:513`; `PickingLeitstandController.cs:695`, `:737`; `FaWorklistController.cs:417`) laufen weiter **nach** der Terminberechnung in C# (ADR 0005). Sie lesen das Ergebnis, nicht die Quelle.

### Dienst und Klasse-D-Gate

- **`CoatingDetectionService.cs:40-46`**: Statt `IHierarchicalModeReader` direkt zu fragen, prueft das Gate `ResolveQuellen(...).Kennzeichen == Artikelkategorie`.
  - Mit `Automatisch` ist das Ergebnis identisch zu heute.
  - **Einordnung:** Das Gate bleibt ein technischer Schutz. Der BomCache wird im hierarchischen Modus nicht gefuellt (`BomCacheSyncService.cs:21`, `:27` mit demselben Reader), und A8 verbietet die Kombination.
- **Kategorie-Heuristik:** `LackierteilKategorieName` bleibt der Eingabewert der Quelle `Artikelkategorie` und ist dort Pflicht. Leer bedeutet weiterhin „keine Erkennung“, wie `:64-74`.
- **`SageImportService.cs:198-215`**: Der Hook laeuft nur noch bei effektiver Quelle `Artikelkategorie` **und** `Sync:CoatingDetectionEnabled` (Rueckfrage 12).
- **`FaMaterializationSyncService.cs:199-213`**: `SetCoatingPartsAsync` wird nur bei Quelle `FaStruktur` aufgerufen. `Automatisch` mit Master an bedeutet immer `FaStruktur`, also wie heute.
- **`SetCoatingPartsAsync`** bekommt `modifiedBy` (`"FaMaterialization"`) und `ModifiedByWindows` mit (Audit).

### Vorschlagsliste und Beschichtungsauftrag an der AKE (E5)

- **Gate:**
  - Key `FaHierarchyBeschichtungAktiv` bleibt (Freigabe-Antwort 11); Anzeigename „Beschichtungsauftrag aktiv“, Gruppe „Termine und Beschichtung“, Standort beide.
  - ~~Die Vorschlagsliste `FaHierarchyBeschichtungController.cs:29` bekommt zusaetzlich die Bedingung „Master an“~~ *ueberholt durch Freigabe-Antwort 10.* Die Vorschlagsliste behaelt ihre beiden Filter (`FaHierarchyBeschichtungController.cs:28-29`) ohne Master-Bedingung; das Menue (`_Layout.cshtml:153-164`) bleibt unveraendert und zeigt an der AKE beide Eintraege, sobald Gate und Rolle passen.
- **Datenbasis flach (eine Positionsquelle fuer Liste, Vergabe-Anzeige und Workflow):**
  - Neue Lesemethode, eine Abfrage, liefert die Lackierteil-Positionen offener WAs aus `CachedBomHeaders/Items` mit der Kategorie-Kette `CoatingDetectionService.cs:149-156` (Kopf ueber `CachedBomHeaders.Artikelnummer = ProductionOrders.ArticleNumber`, wie `CoatingDetectionService.cs:90-109`, `:150-155`, `:167`). ~~Welche WAs: offene mit `HasCoatingParts = true`~~ *praezisiert durch Nachtrag 2 (S-3):* Welche WAs: `ProductionOrders.IsDone = 0 AND ProductionOrders.IsCancelled = 0 AND ProductionOrderPickingStatus.HasCoatingParts = 1` (A13). Die Felder heissen in der Oberflaeche „Erledigt“ und „Storniert“ (`ProductionOrder.cs:83-84`, `:102-103`); `IsCancelled` verhaelt sich in allen Offen-Abfragen wie `IsDone` (`ProductionOrder.cs:100-101`). `IsDonePicking` filtert **nicht**: Lackiert wird nach dem Kommissionieren (Kritische Pruefung S-3). ~~Ob das an der AKE so zutrifft und was der Cache daraus macht, ist Rueckfrage 19.~~ *entschieden durch Freigabe-Antwort 19 (Nachtrag 3):* An der AKE wird zum berechneten Termin beschichtet, nach Geralds Einschaetzung vor dem Kommissionierabschluss. Das Cache-Fenster ohne `IsDonePicking` bleibt, keine eigene Aufgabe; komm-erledigte WAs ohne Cache-Kopf zeigt die Sammelmeldung (A15). P-A7 (2026-11-17): 5 nicht kommissionierte und 858 kommissionierte gekennzeichnete WAs ohne Cache. `HasCoatingParts` bleibt die von der Erkennung gesetzte Wahrheit, damit Liste und Kennzeichen nicht auseinanderlaufen.
  - Sie projiziert jede Zeile in die vorhandene Form `FaHierarchyNode` (nur im Speicher, nicht persistiert), damit `AggregateSummiert` (`BeschichtungsauftragService.cs:185-242`), der Listen-Builder und `Snapshot` unveraendert weiterlaufen:
    `HauptFA = int(OrderNumber)` (numerisch, Freigabe-Antwort 9, P-A3 als Beleg), `HauptArtnr = ProductionOrder.ArticleNumber`, `VaterFA = null`, `SubFA = 0`, ~~`Position = CachedBomItem.SortOrder`~~ *ueberholt (S-4):* ~~`Position = int(CachedBomItem.Position)` (ganzzahlig laut P-A6, sonst Blocker, Rueckfrage 20)~~ *ueberholt durch Freigabe-Antwort 20 / P-A6 (Nachtrag 3):* `Position = null`, `PositionText = (CachedBomItem.Position ?? "").Trim()` (nie `null` in der flachen Projektion, auch nicht bei leerer Position), `Artnr = Ressourcenummer`, `Matchcode` per vorhandenem `MatchcodeLookup`, Masse = 0, `Beschichtet = true`.
    ~~`Sollmenge` nach Rueckfrage 18 (Empfehlung `Menge × ProductionOrder.Quantity`)~~ *ueberholt durch Freigabe-Antwort 18:* `Sollmenge = BomQuantityResolver.Resolve(bomResult, bomItem, order)` (`BomQuantityResolver.cs:15-16`, A12). Die Lesemethode bildet die Zeile auf `BomItem` und den Kopf auf `BomQueryResult(items, header.Source)` ab, so wie `BomCacheRepository.cs:32-47` es tut (dieselbe Abbildung wiederverwenden, nicht kopieren), und ruft dann `Resolve` auf. Keine eigene Mengenformel und kein eigener Baugruppen-Zweig.
  - ~~Der `PositionsSchluessel` bleibt damit eindeutig je Stuecklistenzeile.~~ *ueberholt (S-4):* **Positionsschluessel = Artikelnummer + Position** (A14):
    - Normalisiert wie `PickingOrderRestmengen.Schluessel` (`PickingOrderRestmengen.cs:16-17`): Artnr getrimmt und gross, Position getrimmt.
    - ~~`PositionsSchluessel` (`IBeschichtungsauftragRepository.cs:10-14`) bekommt eine optionale vierte Komponente `Artnr` (Default `null`). Nur die flache Quelle fuellt sie, auf beiden Seiten: aus dem projizierten Knoten und aus der gespeicherten Position (`BeschichtungsauftragPosition.Artnr`, `GetVergabenAsync` `BeschichtungsauftragRepository.cs:40-42`). Die Textform (`BeschichtungsauftraegeController.cs:322-333`) traegt sie als vierten Teil.~~ *ueberholt durch Nachtrag 3:* `PositionsSchluessel` bekommt **zwei** optionale Komponenten `Artnr` und `PositionText` (beide Default `null`), siehe Abschnitt „Positionstext (Nachtrag 3)“ unten.
    - An der IDEAL bleiben `Artnr = null` und `PositionText = null`. Gleichheit, Textform und alle Bestandstests bleiben dort unveraendert (AK 16).
    - ~~Welche Seite fuellt, entscheidet `ResolveQuellen(...).Kennzeichen` (eine Stelle), nicht der Aufrufer.~~ *praezisiert durch Nachtrag 3:* Welche **Positionsquelle** gelesen wird, entscheidet weiter `ResolveQuellen(...).Kennzeichen` (eine Stelle). Der Schluessel selbst traegt `Artnr`/`PositionText` genau dann, wenn `PositionText != null` ist; das setzen nur die flache Projektion und die aus ihr gespeicherten Positionen. `PositionsSchluessel.Von(...)` braucht daher keinen Quellen-Parameter.
    - Damit ist die Vergabe unabhaengig von `SortOrder`, das beim Stuecklistenwechsel neu vergeben wird (S-4).
    - Doppelte Schluessel einer Stueckliste meldet der vorhandene Hinweis `MeldeDoppelteSchluessel` (`BeschichtungsauftragWorkflow.cs:427-432`) und behandelt sie nicht still.
    - ~~Schema: keines, solange P-A6 nur ganzzahlige Positionen zeigt (`Position` bleibt `int?`).~~ *ueberholt durch P-A6 und Freigabe-Antwort 20:* Schema = neue Spalte `PositionText` (SQL/157), `Position` bleibt `int?` und wird an der AKE nicht gefuellt.
  - **Positionstext (Nachtrag 3, Freigabe-Antwort 20, P-A6):**
    - **Spalte:** `BeschichtungsauftragPosition.PositionText`, `nvarchar(50) NULL`, `[StringLength(50)]` plus `HasMaxLength(50)`. Begruendung der Laenge: Einzige Quelle ist `CachedBomItem.Position` mit `MaxLength(50)` (`CachedBomItem.cs:16-17`, `ApplicationDbContext.cs:1106`); der vorhandene Text-Positionsschluessel `PickingOrderLine.BomPosition` hat dieselbe Breite (`ApplicationDbContext.cs:1726`). Keine Pruefabfrage zur Sage-Spaltenbreite noetig: Ein laengerer Wert kann nicht im Cache stehen.
    - **Nur die flache Projektion braucht den Text im Speicher:** `FaHierarchyNode` bekommt `[NotMapped] public string? PositionText` (Praezedenz `[NotMapped]` in `Models/OperationConfig.cs:64`, `Models/ProductionOrder.cs:34`). Kein Schema an `FaHierarchyNodes`, kein Schreibweg der IDEAL-Syncs aendert sich. `CachedBomItem` bleibt unveraendert (hat den Text schon).
    - **Befuellung:** `Snapshot` (`BeschichtungsauftragWorkflow.cs:440-450`) und `Kopie` (`:452-462`) uebernehmen `PositionText` 1:1; `Summiere` (`BeschichtungsauftraegeViewModels.cs:129-137`) projiziert ihn zurueck. Keine weiteren Schreiber.
    - **Bestand (Altzeilen):** kein Backfill. Alle vorhandenen Zeilen behalten `PositionText = NULL`, das ist genau die IDEAL-Semantik (Schluessel ohne Artnr/Text). Nach dem Code (Gate `RequireFaHierarchyBeschichtungAktiv`, Datenbasis `FaHierarchyNode`, die nur hierarchisch gefuellt wird) kann es heute an einem flachen System keine gespeicherten Positionen geben; belegt wird das per P-A8 und dauerhaft per Manifest-Pruefung `Pruefung/BeschichtungPositionOhneTextFlach` (Deploy). Eine Altzeile ohne Text an einem flachen System wuerde nie als vergeben erkannt; deshalb wird sie gemeldet, nicht still behandelt.
    - **Schluessel:** `PositionsSchluessel(int? VaterFA, int SubFA, int? Position, string? Artnr = null, string? PositionText = null)`. `Von(FaHierarchyNode)` und `Von(BeschichtungsauftragPosition)` setzen bei `PositionText != null` beide Zusatzteile normalisiert ueber `PickingOrderRestmengen.Schluessel(artnr ?? "", positionText)` (`PickingOrderRestmengen.cs:16-17`: Artnr getrimmt und gross, Position getrimmt; `artnr` ist dort nicht null-sicher, `Artnr` ist am Modell nullable `BeschichtungsauftragPosition.cs:23`), sonst beide `null`. `GetVergabenAsync` (`BeschichtungsauftragRepository.cs:40-42`) liest `Artnr` und `PositionText` mit und baut den Schluessel ueber dieselbe Normalisierung. `ToString()` (`IBeschichtungsauftragRepository.cs:14`) zeigt bei `PositionText != null` „Artnr {Artnr} / Pos {PositionText}“, sonst wie heute.
    - **Textform** (`BeschichtungsauftraegeController.cs:322-343`): IDEAL unveraendert dreiteilig `VaterFA|SubFA|Position`. Flach fuenfteilig `VaterFA|SubFA|Position|Artnr|PositionText`, Teil 4 und 5 mit `Uri.EscapeDataString` kodiert (ein `|` in Artnr oder Position kann den Schluessel dann nicht zerlegen); `TrySchluessel` nimmt 3 oder 5 Teile, dekodiert mit `Uri.UnescapeDataString`, jede andere Teilzahl ist ungueltig (bestehende Warnung `:193`). Die HTML-Id in `Details.cshtml:314` bleibt `SchluesselText(...).Replace('|','-')`; die kodierte Form enthaelt kein Leerzeichen.
    - **Sortierung (A16):** eine Regel, der vorhandene `NaturalPositionComparer` (`NaturalPositionComparer.cs:8-34`), kein neuer Helfer. An jeder Reihenfolge-Stelle wird `OrderBy(Position ?? int.MaxValue)` bzw. `OrderBy(n => n.Position)` um `.ThenBy(x => x.PositionText, new NaturalPositionComparer())` vor dem bisherigen `ThenBy(SubFA)` ergaenzt: `FaHierarchyListBuilder.cs:161`, `BeschichtungsauftragService.cs:200`, `Details.cshtml:230`, `Reklamation.cshtml:34`. An der IDEAL ist `PositionText` immer `null`, die Reihenfolge bleibt identisch; an der AKE ist `Position` immer `null`, es gilt nur der Text. **Nur in C#, nicht in SQL:** Sortieren und Paging laufen im Speicher, und gepagt wird je HauptFA-Gruppe, nicht je Position (`FaHierarchyListBuilder.cs:141-161`, `BeschichtungsauftragService.cs:215-227`). Eine SQL-Sortierung vor dem Paging gibt es nicht und braucht es nicht.
    - **Anzeige:** Wo heute `Position?.ToString() ?? "—"` steht (`Details.cshtml:233`, `:247`, `:318`; `Reklamation.cshtml:38`), steht kuenftig `PositionText ?? Position?.ToString() ?? "—"` (leerer Text ebenfalls „—“). Inline, kein neuer Helfer.
    - **Index:** unveraendert. Kein Unique-Index (Begruendung `ApplicationDbContext.cs:1787-1788`: dieselbe Position steht in stornierten und neuen Auftraegen sowie in Reklamationen erneut), kein neuer Index: `GetVergabenAsync` sucht ueber `HauptFA` (fuehrende Spalte des vorhandenen Index), der Textvergleich laeuft im Speicher.
    - **Doppelte (Artnr, PositionText):** P-A6 zeigt 0. Kommen sie vor, meldet `MeldeDoppelteSchluessel` sie (Hinweis, kein stilles Zusammenfassen), wie an der IDEAL.
  - **Sammelmeldung (S-2, A15):** Dieselbe Lesemethode liefert zusaetzlich die WAs, die die Offen-Bedingung erfuellen und `HasCoatingParts = 1` tragen, aber **keine** Lackierteil-Zeile im Cache haben (Kopf fehlt, z. B. ausserhalb des Cache-Fensters `BomCacheSyncService.cs:412-424`, `:617-619`, oder Kopf ohne Lackierteil-Zeile).
    - Ist die Menge nicht leer, zeigen `Index` und `Summiert` oberhalb der Tabelle ein Bootstrap-`alert-warning` mit Icon und Text: „{N} WA mit Beschichtungskennzeichen ohne Lackierteile im Stuecklisten-Cache – nicht in der Vorschlagsliste: {Nummern}“. Hoechstens 20 Nummern, danach „…“.
    - Dazu ein `ILogger.LogWarning` mit Anzahl und Nummern.
    - Nur bei Quelle `Artikelkategorie`; an der IDEAL entfaellt die Meldung.
    - Die Meldung ist eine ViewModel-Eigenschaft, kein `TempData`, denn sie gilt fuer jeden Seitenaufruf.
  - `BeschichtungsauftragService.BuildAsync`/`BuildSummiertAsync`, `BeschichtungsauftragVergabeAnzeige.ErmittelnAsync` und der Workflow waehlen die Quelle ueber `ResolveQuellen(...).Kennzeichen`: `FaStruktur` = `FaHierarchyNode` wie heute, `Artikelkategorie` = flache Projektion.
  - Kopfdaten (`FaHierarchyOrderInfo`) gibt es an der AKE nicht; die Kopfzeile zeigt den bestehenden Hinweis (`_BeschichtungKopf.cshtml:27-30`). Kreditor/Beschichter (`Dienstleister`), RAL und Retour werden im Auftrag manuell befuellt (Freigabe-Antwort 10).
  - Spaltenkataloge: „(IDEAL)“ aus den Anzeigenamen entfernen (`ColumnDefinitions.cs:379`, `:404`); Spalten unveraendert.
- **Workflow:** `AnlegenAsync` und die Kandidaten-/Pruefstellen (`BeschichtungsauftragWorkflow.cs:88-120`, `:221-222`, `:262-268`) waehlen die Quelle ueber `ResolveQuellen(...).Kennzeichen`. Kopfvorbelegung AKE: Start = `Bestimmen(...).Termin` (Quelle `Berechnet`), Rest leer.
- **Kein Schema-Eingriff am Kopf** (Freigabe-Antwort 9): Die AKE-WA-Nummern sind numerisch, `Beschichtungsauftrag.HauptFA` bleibt `int`. ~~Sonst SQL/152 mit Spalte `Auftragsnummer`~~ *ueberholt*. P-A3 ist erhoben: 4.524 FAs, 0 nicht numerisch (2026-11-17). ~~Gleiches gilt fuer P-A6 (Stuecklisten-Positionen der Lackierteile ganzzahlig, Nachtrag 2): nicht-ganzzahlige Positionen → E5 Blocker, Rueckfrage 20.~~ *ueberholt durch Nachtrag 3:* P-A6 zeigt nicht ganzzahlige Positionen; nach Freigabe-Antwort 20 kommt die Spalte `PositionText` (SQL/157). P-A6 ist damit kein Blocker mehr.
- **Mengen-Pruefschritt vor E5 (H3, Freigabe-Antwort 18):** Gerald waehlt mit der Hilfsabfrage H3 (Abschnitt „Pruefabfragen“) einen offenen Lackier-WA mit `Quantity > 1` und Stuecklistenzeilen mit gefuellter `Baugruppe`. Er oeffnet dessen Kommissionierliste und vergleicht die Mengen der Baugruppen-Zeilen mit Sage/OSEON. Passen sie, gilt A12 so, wie sie gebaut wird. Passen sie nicht, ist die Kommissionierung selbst betroffen. Dann haelt der Lauf E5 als Blocker fest und meldet den Befund, statt eine zweite Mengenregel fuer die Beschichtung zu bauen.

## Migrations-/SQL-Auswirkungen

- ~~**E1-E6: keine Schema-Aenderung**, keine EF-Migration, kein SQL-Skript~~ *ueberholt durch Nachtrag 3:* **E1-E4 und E6: keine Schema-Aenderung.** E5 bringt genau eine additive Spalte (Freigabe-Antwort 20). `HauptFA` bleibt `int` (Freigabe-Antwort 9; die fruehere Reservierung SQL/152 ist *ueberholt* und entfaellt).
  - Die neuen AppSettings werden per Startup-Seed aus `AppSettingDefinitions.All` angelegt (Seed true, Default `Automatisch`).
  - Fuer die AppSettings keine `SQL/00_FreshInstall.sql`-Aenderung; der FreshInstall-Seed laeuft ueber denselben Startup-Seed.
  - Die flache AKE-Positionsquelle ist eine reine Lesemethode auf vorhandenen Tabellen.
  - ~~Der Positionsschluessel Artikelnummer + Position (Nachtrag 2) nutzt die vorhandenen Spalten `BeschichtungsauftragPosition.Artnr` und `Position` (`int?`). Er braucht kein Schema, solange P-A6 nur ganzzahlige Positionen zeigt (sonst Rueckfrage 20, E5 Blocker).~~ *ueberholt durch P-A6 und Freigabe-Antwort 20.*
- **E5: Spalte `PositionText` (Nachtrag 3), Reihenfolge nach ADR 0004:**
  1. Model: `BeschichtungsauftragPosition.PositionText` (`[StringLength(50)] string?`), `ApplicationDbContext` `HasMaxLength(50)` im Block `:1771-1795`. `FaHierarchyNode.PositionText` nur `[NotMapped]` (kein Schema).
  2. `dotnet ef migrations add AddBeschichtungsauftragPositionText --project IdealAkeWms`. Die Migration darf nur `AddColumn PositionText nvarchar(50) NULL` auf `BeschichtungsauftragPositionen` enthalten; taucht `FaHierarchyNodes` darin auf, fehlt das `[NotMapped]`.
  3. `SQL/157_AddBeschichtungsauftragPositionText.sql` (Nummer beim Start pruefen): Guard „richtige DB“ wie `SQL/156`, dann `IF OBJECT_ID(N'dbo.BeschichtungsauftragPositionen', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.BeschichtungsauftragPositionen', N'PositionText') IS NULL ALTER TABLE ... ADD [PositionText] NVARCHAR(50) NULL;` im eigenen Batch (`GO`). Fehlt die Tabelle, Abbruch mit Meldung „SQL/94 fehlt“ statt stillem Weiterlaufen.
  4. `__EFMigrationsHistory`-Insert in separatem Batch, nur wenn die Spalte existiert (Muster `SQL/156`).
  5. `SQL/00_FreshInstall.sql` an zwei Stellen: Spalte `[PositionText] NVARCHAR(50) NULL` im `CREATE TABLE` (`:2820-2843`, nach `[Position]`) **plus** guarded `ALTER TABLE ... ADD` fuer bestehende Tabellen; MigrationId-Insert nach dem von `SQL/156` (`:3369-3371`).
  6. `BeschichtungsauftragMigrationSqlTests` (vorhandenes Muster) um Skript 157 und die FreshInstall-Stellen ergaenzen; `NachlaufManifestDriftTests.SchemaReihenfolge` um `SQL/157`.
  - Additiv, nullable, kein Default, kein Backfill (Altzeilen = IDEAL-Semantik, siehe Loesungsentwurf „Positionstext“). Nicht daten-destruktiv; das Standard-Backup des Update-Skripts genuegt, kein Sonder-Backup.
  - **Nummernlage beim Schreiben (2026-11-17):** Im Worktree ist `SQL/156` (`156_AddSagePurchaseOrderLineAbNummerKommission.sql`, v1.99.33) die hoechste, `SQL/151` ist fuer die Sicherheit reserviert (Manifest `nachlauf.psd1:99-108` ohne 151/157). **Kollision gemeldet:** Die BF-Spec [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]] plant noch `SQL/156` sowie TS-155/156 (`:78`, `:459`); diese Nummern sind durch Offene Bestellungen (TS-155) und Wareneingang (SQL/156, TS-156) belegt. Wird die BF-Spec vor E5 gebaut, nimmt sie 157 und E5 rueckt auf die naechste freie Nummer. Diese Spec aendert die BF-Spec nicht.
- **Bestand beim Umschalten** (keine Datenmigration, Neuberechnung durch den naechsten Lauf):
  - Wechsel der Kennzeichen-Quelle: Der neue Schreiber ueberschreibt beim naechsten Lauf. Kippt eine Zeile auf false, setzt er `IsCoatingDone` mit zurueck; dieser Verlust ist gewollt und steht im Warnhinweis.
  - Grenze `Artikelkategorie`: Neu berechnet werden nur Auftraege im Erkennungsfenster (8 Wochen / 200). Zeilen ausserhalb behalten den alten Wert, bis sie ins Fenster kommen. Das wird dokumentiert, nicht still behandelt.
  - Wechsel der Termin-Quelle: wirkt sofort beim naechsten Seitenaufruf, nichts gespeichert.
- **AgentJobs:** keine betroffen (nur `_archiv`). Fuer SQL/157 belegt: kein Skript unter `SQL/AgentJobs/` nennt `Beschichtungsauftrag`.
- **Manifest-Drift:** neue Eintraege muessen `NachlaufManifestDriftTests` bestehen.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `BeschichtungKennzeichenQuelle` (neu, Anzeige „Quelle Beschichtet-Kennzeichen“) | Choice (AppSettings, Gruppe „Termine und Beschichtung“) | `Automatisch` („nach Auftragsmodell“), `Artikelkategorie` („OSEON-Artikelkategorie ueber Stueckliste“), `FaStruktur` („Sage FA-Struktur, Spalte Beschichtet“) | `Automatisch` | Legt fest, welcher Dienst-Lauf `HasCoatingParts` schreibt. Automatisch: hierarchisch = FA-Struktur, flach = Artikelkategorie. Server lehnt `Artikelkategorie` bei Master an und `FaStruktur` bei Master aus ab. | Dienst: Lackierteil-Erkennung, FA-Materialisierung, Sage-Import-Hook; Web: Aktiv-Kennung FA-Liste/Leitstand/Arbeitsliste, Beschichtungsauftrag-Anlage | beide | „Umschalten berechnet das Kennzeichen beim naechsten Lauf neu; faellt es weg, wird ‚Beschichtung erledigt‘ zurueckgesetzt.“ |
| `BeschichtungTerminQuelle` (neu, Anzeige „Quelle Beschichtungstermin“) | Choice (AppSettings, gleiche Gruppe) | `Automatisch`, `Berechnet` („Kommissioniertermin minus Beschichtungstage, auf Abholtag“), `View` („Sage-View Start_Beschichtung am Kopf, unveraendert uebernommen“). Kein Wert `SData` in dieser Version (Freigabe-Antwort 1). | `Automatisch` | Automatisch: hierarchisch = View, flach = Berechnet. `View` uebernimmt `Start_Beschichtung` ohne WMS-Berechnung. `View` nur bei Master an, also an der AKE nicht waehlbar, bis P-A5 eine Datumsspalte belegt (Freigabe-Antwort 15). | Web: FA-Liste, Kommissionier-Leitstand, Werkbank-Arbeitsliste, Beschichtungsauftrag-Kopf (AKE) | beide | – (nur Anzeige) |
| `LackierteilKategorieName` (geaendert: Beschreibung) | MasterData (unveraendert) | Artikelkategorie | leer (unveraendert) | Zusatz: „wirkt nur bei Kennzeichen-Quelle Artikelkategorie“; `AbhaengigVon` = `BeschichtungKennzeichenQuelle` | wie heute | beide | – |
| `BeschichtungTage`, `BeschichtungAbholtage` (geaendert: Beschreibung) | unveraendert | unveraendert | unveraendert | Zusatz: „wirkt nur bei Termin-Quelle Berechnet“ | wie heute | beide | – |
| `FaHierarchyBeschichtungAktiv` (geaendert in E5) | Bool (unveraendert) | true/false | false | Schaltet Vorschlagsliste und Beschichtungsauftrag frei; Datenbasis je Kennzeichen-Quelle (AKE: WA-Stueckliste) | Menue, `FaHierarchyBeschichtungController`, `BeschichtungsauftraegeController` | **beide** (bisher IDEAL); Gruppe „Termine und Beschichtung“, Anzeige „Beschichtungsauftrag aktiv“ | – |

- Die Server-Validierung der Kombination ist eine neue Regel beim Speichern in `/Settings` und braucht den Master-Wert. Muster: Abhaengigkeits-Pruefung vorhandener Choice-Keys.
- Die Standort-Maske bleibt aussen vor und bekommt nur einen Hinweis-Link auf `/Settings` (Freigabe-Antwort 14).

## Audit-Feld-Auswirkungen

- `SetCoatingPartsAsync` (`ProductionOrderPickingStatusRepository.cs:180-181`) setzt kuenftig `ModifiedBy = "FaMaterialization"` und `ModifiedByWindows` (Dienstkonto). Heute bleibt `ModifiedBy` leer, das ist ein Verstoss gegen ADR 0003.
- `CoatingDetectionService` bleibt unveraendert (`'CoatingDetection'`, `SYSTEM_USER`).
- Beschichtungsauftrag an der AKE: dieselben Audit-Pfade wie an der IDEAL (`AuditableEntity`, `ICurrentUserService`).
- `PositionText` (Nachtrag 3) wird nur bei der Anlage einer Position gesetzt (`Snapshot`, `Kopie`, beide mit `Erstellt(p)`, `BeschichtungsauftragWorkflow.cs:448`, `:460`) und nie geaendert. Kein neuer Update-Pfad, also keine neue Modified-Pflicht. Die Migration setzt keine Werte, damit auch keine Audit-Felder.
- AppSettings-Aenderungen laufen ueber den bestehenden `/Settings`-Speicherweg mit dessen Audit.

## Akzeptanzkriterien

1. Nach dem Deploy existieren die beiden Keys mit Wert `Automatisch` an AKE und IDEAL. `/Settings` zeigt sie als Auswahlliste mit Anzeigetext, Wirkung, Verwendung, Standort und Warnhinweis.
2. Speichern eines Werts ausserhalb der Liste wird abgelehnt (Server).
3. An einem System mit Master an wird `BeschichtungKennzeichenQuelle = Artikelkategorie` beim Speichern abgelehnt (A8), an einem System mit Master aus `FaStruktur` und `View`.
4. Ein per SQL gesetzter ungueltiger Wert wird wie `Automatisch` gelesen, und eine Warnung erscheint im Protokoll.
5. AKE mit `Automatisch`: FA-Liste, Leitstand und Arbeitsliste zeigen fuer einen Lackier-FA denselben Termin wie vor dem Deploy (Unit-Test: gleiche Eingabe → gleiches Datum wie die bisherige Inline-Formel und der Calculator).
6. IDEAL mit `Automatisch`: In der FA-Liste zeigt die Zeile mit `HasCoatingParts = true` `Start_Beschichtung` des eindeutigen Kopfes **datumsgleich** (keine Verschiebung um `BeschichtungTage`, kein Abholtag). Eine Aenderung von `BeschichtungTage` oder `BeschichtungAbholtage` aendert diesen Termin nicht (Unit-Test). Der Spalten-Tooltip nennt die Quelle „Sage-View, Start_Beschichtung“. Kombigeraet und leeres Feld zeigen keinen Termin und keinen Ersatztermin (TS-71.11 unveraendert gruen).
7. IDEAL (E4): Leitstand und Arbeitsliste zeigen fuer denselben Sub-FA denselben Termin wie die FA-Liste.
8. IDEAL mit `BeschichtungTerminQuelle = Berechnet`: Die FA-Liste zeigt den berechneten Termin auf Basis des Kopf-`FE_Termin`, der Tooltip nennt „berechnet“.
9. Kein Controller und kein View liest `LackierteilKategorieName`, `BeschichtungTage`, `BeschichtungAbholtage` oder `Start_Beschichtung` fuer die Terminanzeige direkt. Waechter-Test per Quelltext-Suche nach Muster der bestehenden Coverage-Tests; einzige Fundstelle ist der Resolver. Ausgenommen ist die Kopfdaten-Anzeige der Vorschlagsliste/des Auftrags (`_BeschichtungKopf.cshtml:18`, `BeschichtungsauftragWorkflow.cs:404-425`): sie zeigt die Sage-Kopfdaten als solche, nicht den Listen-Termin.
10. Dienst: Bei effektiver Quelle `FaStruktur` schreibt `CoatingDetectionService` nichts, und das Protokoll meldet „uebersprungen: Kennzeichen-Quelle FA-Struktur“. Bei `Artikelkategorie` ruft die Materialisierung `SetCoatingPartsAsync` nicht auf.
11. Der Sage-Import-Hook ruft die Lackierteil-Erkennung nicht auf, wenn `Sync:CoatingDetectionEnabled = false` (Rueckfrage 12).
12. `SetCoatingPartsAsync` setzt `ModifiedBy = "FaMaterialization"` und `ModifiedByWindows`; der Reset von `IsCoatingDone` bleibt erhalten (TS-71.10 gruen).
13. Aktiv-Kennung IDEAL: Mit Quelle `FaStruktur` erscheinen Beschichtungs-Haken nur an Zeilen mit `HasCoatingParts = true`, unabhaengig von `LackierteilKategorieName`.
14. AKE (E5): Mit `FaHierarchyBeschichtungAktiv = true` und Rolle `beschichtungsauftrag_schreiben` laesst sich fuer einen WA mit Lackierteilen ueber „Aus Vorschlagsliste erstellen“ ein Beschichtungsauftrag anlegen. Die Positionen entsprechen den Stuecklistenzeilen der Lackierteil-Kategorie, Masse 0. Dienstleister (Kreditor/Beschichter), RAL und Retour sind leer und manuell befuellbar; Start ist mit dem berechneten Termin vorbelegt. Ein WA ohne Lackierteile ergibt den bestehenden Hinweis „keine beschichteten Positionen“.
15. AKE (E5): Die Vorschlagsliste (Menue „Beschichtung → Vorschlagsliste“, `/FaHierarchyBeschichtung/Summiert` und `/FaHierarchyBeschichtung`) ist mit Gate an und Lese- oder Schreibrolle erreichbar und zeigt je WA (HauptFA = WA-Nummer) die Lackierteile der WA-Stueckliste; die Kopfzeile zeigt „Keine Auftrags-Kopfdaten zu diesem HauptFA.“. ~~Die Vorschlagsliste ist an der AKE nicht erreichbar.~~ *ueberholt durch Freigabe-Antwort 10.*
15a. AKE (E5), Negativfaelle: Ohne Rolle oder mit Gate aus gilt fuer Vorschlagsliste und Auftrag dasselbe Verhalten wie heute (Redirect plus WarningMessage, API 404); ohne Schreibrolle fehlt „Aus Vorschlagsliste erstellen“. Ein WA ohne Lackierteile erscheint nicht in der Liste. Bereits vergebene Positionen werden wie an der IDEAL als vergeben angezeigt.
15b. AKE (E5, S-2): Ein offener WA mit `HasCoatingParts = 1` ohne Lackierteil-Zeile im Stuecklisten-Cache (Kopf geloescht oder ohne Lackierteil-Zeile) fehlt in der Liste. `Index` und `Summiert` zeigen dann die Sammelmeldung „{N} WA mit Beschichtungskennzeichen ohne Lackierteile im Stuecklisten-Cache – nicht in der Vorschlagsliste: {Nummern}“, und das Log enthaelt eine Warnung mit denselben Nummern. Ohne solche WAs gibt es keine Meldung. An der IDEAL erscheint die Meldung nie (Unit-Test plus TS-145).
15c. AKE (E5, S-3): Die flache Quelle nimmt genau die WAs mit `IsDone = 0 AND IsCancelled = 0` (A13). Ein stornierter (`IsCancelled = 1`) oder erledigter (`IsDone = 1`) WA erscheint weder in der Liste noch in der Sammelmeldung. Ein WA mit `IsDonePicking = 1` und vorhandenen Cache-Zeilen erscheint (Unit-Test).
16. IDEAL (E5): Vorschlagsliste und Beschichtungsauftrag verhalten sich unveraendert (bestehende Tests zu Teil 3/4 und Ausbau Teil 1-4 gruen).
17. Build und alle Tests gruen; je Standort × Quellwert mindestens ein Resolver-Test (Matrix unten).
18. ~~Keine EF-Migration, kein neues SQL-Skript; `Beschichtungsauftrag.HauptFA` bleibt `int` (Freigabe-Antwort 9).~~ *ueberholt durch Nachtrag 3 (Freigabe-Antwort 20):* `Beschichtungsauftrag.HauptFA` bleibt `int` (Freigabe-Antwort 9). Einzige Schema-Aenderung des Epics ist `BeschichtungsauftragPositionen.PositionText nvarchar(50) NULL` in genau einer EF-Migration und genau einem SQL-Skript (SQL/157 oder die beim Start freie Nummer); E1-E4 bleiben ohne Migration.
19. `/Settings` bietet fuer `BeschichtungTerminQuelle` genau `Automatisch`, `Berechnet`, `View` an (kein `SData`); an der AKE (Master aus) wird `View` beim Speichern abgelehnt (Freigabe-Antwort 15).
20. Menge (E5, Freigabe-Antwort 18): Die Menge jeder AKE-Position in Vorschlagsliste und neu angelegtem Auftrag ist das Ergebnis von `BomQuantityResolver.Resolve` fuer diese Stuecklistenzeile. Sie ist identisch mit der Menge derselben Zeile in der Kommissionierliste desselben WA (Unit-Test: `Menge 2,5 × Quantity 4 = 10`, auch fuer eine Zeile mit `Baugruppe`). Kein Code ausser `BomQuantityResolver` multipliziert `Menge` mit `Quantity` (Waechter per Quelltext-Suche wie AK 9).
21. Haltepunkt H3 ist vor dem E5-Commit im Abschnitt „Pruefabfragen“ eingetragen: WA-Nummer, Anzahl verglichener Baugruppen-Zeilen, Ergebnis „Kommissionierliste = Sage/OSEON“. Fehlt der Eintrag oder weicht er ab, steht E5 als Blocker in der Etappen-Tabelle.
22. Positionsschluessel (E5, S-4): An der AKE ist der Vergabe-Schluessel Artikelnummer + Position. Nach einer Stuecklistenaenderung, die nur `SortOrder` verschiebt, bleibt eine vergebene Position als vergeben markiert, und eine andere Zeile wird nicht faelschlich als vergeben gezeigt (Unit-Test mit zwei Cache-Staenden). An der IDEAL bleiben Schluessel und Textform unveraendert (Bestandstests gruen). *Praezisiert durch Nachtrag 3:* „Position“ ist an der AKE `PositionText`; der Unit-Test nutzt mehrstufige Positionen (`51.5.2`, `51.5.10`).
23. Manifest-Pruefung `Pruefung/BeschichtungQuelleKombination`: Fehlt die ServiceSettings-Zeile `ProduktionsauftragHierarchisch`, zaehlt die Abfrage wie bei Master aus (Default `false`, `ServiceSettingDefinitions.cs:222`, `IHierarchicalModeReader.cs:21`). `FaStruktur` oder `View` ohne Master-Zeile ergibt also eine Abweichung > 0. `NachlaufManifestDriftTests` gruen.
24. Spalte (E5, Nachtrag 3): Nach dem Deploy hat `dbo.BeschichtungsauftragPositionen` die Spalte `PositionText nvarchar(50) NULL`, die MigrationId steht in `__EFMigrationsHistory`, und das Skript laeuft ein zweites Mal ohne Fehler und ohne Aenderung durch (Idempotenz). Eine frische DB aus `SQL/00_FreshInstall.sql` hat dieselbe Spalte und dieselbe MigrationId. Bestandszeilen haben `PositionText = NULL`.
25. Speichern (E5, Nachtrag 3): Ein an der AKE angelegter Auftrag speichert je Position `PositionText` = getrimmter Text aus `CachedBomItem.Position` (z. B. `51.5.2`), `Position = NULL`, `Artnr` = Ressourcenummer. Eine Reklamation (`Kopie`) und „Positionen hinzufuegen“ (`Snapshot`) uebernehmen `PositionText` unveraendert. An der IDEAL bleibt `PositionText` bei neuen Positionen `NULL` (Unit-Test je Weg).
26. Natuerliche Sortierung (E5, A16): Innerhalb eines HauptFA stehen die Positionen in Vorschlagsliste, Auftrag-Details und Reklamationsauswahl in der Reihenfolge `5`, `5.2`, `5.10`, `51`, `51.5`, `51.5.2`, `51.5.10`; ein nicht numerisches Segment (z. B. `5.A`) sortiert hinter den numerischen derselben Ebene (Ordinal-Vergleich). Die Summiert-Zeile nimmt Masse und Matchcode der in dieser Reihenfolge ersten Position. An der IDEAL ist die Reihenfolge unveraendert (Bestandstests gruen). Kein Code ausser `NaturalPositionComparer` legt eine Positionsreihenfolge fuer Text-Positionen fest.
27. Textform (E5, Nachtrag 3): `SchluesselText`/`TrySchluessel` liefern fuer einen IDEAL-Schluessel unveraendert drei Teile und lesen drei Teile wie bisher. Fuer einen AKE-Schluessel liefern sie fuenf Teile mit kodierten Teilen 4 und 5; Rundreise `TrySchluessel(SchluesselText(k)) == k` gilt auch fuer Artnr oder Position mit `|`, `.`, Leerzeichen und leerem Text. Vier oder sechs Teile sind ungueltig und loesen die bestehende Warnung „ungueltiger Positionsschluessel“ aus (Unit-Test).
28. Altzeilen (E5, Nachtrag 3): Die Manifest-Pruefung `Pruefung/BeschichtungPositionOhneTextFlach` ergibt an jedem System mit Master aus 0, solange keine gespeicherte Position ohne `PositionText` existiert, und > 0 mit Hinweistext, sobald eine solche Zeile existiert (auf DEV AKE per Testzeile belegt, danach entfernt). An einem System mit Master an zaehlt sie nie.

## Test-Szenarien

**Automatisierte Tests**

`BeschichtungResolverTests`, Matrix:

| Master | Kennzeichen-Quelle | Termin-Quelle | erwartet |
|---|---|---|---|
| aus | Automatisch | Automatisch | Artikelkategorie / Berechnet |
| an | Automatisch | Automatisch | FaStruktur / View |
| aus | FaStruktur (ungueltig) | – | Automatisch plus Warnung |
| an | Artikelkategorie (ungueltig) | – | Automatisch plus Warnung |
| an | – | Berechnet | Kopf-`FE_Termin` minus Tage, Abholtag |
| an | – | View, eindeutiger Kopf | `Start_Beschichtung` datumsgleich; mit `BeschichtungTage` 0 und 10 dasselbe Ergebnis (keine WMS-Berechnung, Freigabe-Antwort 4) |
| an | – | View, Kombigeraet | null |
| an | – | View, Feld leer | null |
| aus | – | Berechnet | identisch zur alten Inline-Formel |
| aus | – | View (ungueltig an der AKE) | Automatisch = Berechnet plus Warnung |

Weitere automatisierte Tests:

- Aktiv je Quelle.
- `AppSettingDefinitionsTests` (Choice-Werte genau `Automatisch`/`Berechnet`/`View` bzw. `Automatisch`/`Artikelkategorie`/`FaStruktur`, Pflichtangaben).
- Gegen-Tests fuer die Dienst-Gates (`CoatingDetectionService` uebersprungen, Hook-Gate).
- `SetCoatingPartsAsync`-Audit.
- E5: flache Positionsquelle (InMemory): Lackierteil-Zeilen eines WA → `FaHierarchyNode`-Projektion; `AggregateSummiert` darauf; Vergabe-Anzeige; Workflow-Anlage aus der flachen Basis; WA ohne Lackierteile → keine Zeile. IDEAL-Bestandstests unveraendert.
- E5 (Nachtrag 2):
  - Menge ueber `BomQuantityResolver` gleich der Kommissionier-Menge, auch mit `Baugruppe` (AK 20).
  - Offen-Filter: `IsDone`, `IsCancelled` raus, `IsDonePicking` drin (AK 15c).
  - Sammelmeldung: gekennzeichneter WA ohne Cache-Kopf bzw. ohne Lackierteil-Zeile wird gezaehlt; IDEAL ohne Meldung (AK 15b).
  - Positionsschluessel Artnr + Position stabil gegen neue `SortOrder`; ~~`TrySchluessel`/`SchluesselText` mit und ohne vierten Teil (AK 22)~~ *ueberholt (Nachtrag 3):* `TrySchluessel`/`SchluesselText` mit drei und fuenf Teilen (AK 22, AK 27).
- E5 (Nachtrag 3, `PositionText`):
  - Projektion: `CachedBomItem.Position` `" 51.5.2 "` → `PositionText = "51.5.2"`, `Position = null`; `null`/`""` → `PositionText = ""` (nicht `null`) (AK 25).
  - `PositionsSchluessel`: gleiche Artnr in anderer Gross-/Kleinschreibung und Position mit Leerzeichen ergeben denselben Schluessel; `5.2` und `5.20` sind verschiedene Schluessel (Text, keine Zahl); IDEAL-Knoten ohne `PositionText` ergeben den Drei-Komponenten-Schluessel wie heute.
  - Vergabe: gespeicherte AKE-Position (`Artnr`, `PositionText`) wird ueber `GetVergabenAsync` als vergeben erkannt; eine Altzeile mit `PositionText = NULL` und gleicher Artnr wird an der flachen Quelle **nicht** als vergeben erkannt (dokumentiertes Verhalten, gemeldet ueber AK 28).
  - Doppelte `(Artnr, PositionText)` in einer Stueckliste: Hinweis aus `MeldeDoppelteSchluessel`, kein stilles Zusammenfassen.
  - Sortierung: Liste gemischter und tiefer Positionen (`51.5.10`, `5`, `51`, `5.A`, `51.5.2`, `5.10`, `5.2`, `51.5`, `""`) in `FaHierarchyListBuilder` und `AggregateSummiert` (erste Position) nach AK 26; IDEAL-Reihenfolge unveraendert.
  - Snapshot/Kopie/Summiere tragen `PositionText` (AK 25).
  - Migrations-SQL: `BeschichtungsauftragMigrationSqlTests`-Muster fuer `157_…sql` (Guard, `COL_LENGTH`, History-Batch) und beide FreshInstall-Stellen (AK 24).
- `NachlaufManifestDriftTests` (inkl. `SchemaReihenfolge` mit `SQL/157`).

**Manuelle Szenarien fuer `docs/TESTSZENARIEN.md`** (Pflicht des Dev-Laufs; Nummern ab TS-140, TS-139 ist reserviert):

- **TS-140 — Einstellungen Beschichtungsquelle**
  - Auswahlliste, Texte, Default `Automatisch` je Standort.
  - Ablehnung ungueltiger Kombinationen; an der AKE `View` waehlen → abgelehnt (Freigabe-Antwort 15).
  - Termin-Quelle bietet kein `SData` an.
  - Standort-Maske zeigt den Hinweis-Link auf `/Settings`.
  - Negativfall: per SQL gesetzter Fremdwert → Protokoll-Warnung, Verhalten wie Automatisch.
- **TS-141 — AKE Regression Termin**
  - Vorbedingung: Lackier-FA im Fenster.
  - FA-Liste, Leitstand und Arbeitsliste zeigen denselben Termin wie vor dem Update; Spaltenfilter `coating-date` funktioniert.
  - Negativfall: `LackierteilKategorieName` leer → Termin fuer alle (fallstricke `:207-211`).
- **TS-142 — IDEAL Termin aus der View in allen drei Listen**
  - Vorbedingung: P-I1 erhoben (Schalterstand IDEAL, Freigabe-Antwort 7).
  - Ein eindeutiger Kopf mit `Start_Beschichtung`: FA-Liste, Leitstand und Arbeitsliste zeigen genau dieses Datum (gegen P-I5 bzw. `FaHierarchyOrderInfos` pruefen); Tooltip nennt „Sage-View, Start_Beschichtung“.
  - Gegenprobe: `BeschichtungTage` in `/Settings` aendern → angezeigter Termin bleibt gleich (keine WMS-Berechnung auf `Start_Beschichtung`, Freigabe-Antwort 4); danach Wert zuruecksetzen.
  - Haken nur an Zeilen mit `HasCoatingParts = true` (Aktiv-Kennung, Freigabe-Antwort 7).
  - Negativfaelle: Kombigeraet, leeres Feld mit gesetztem `FE_Termin` (kein Ersatztermin, wie TS-71.11b).
- **TS-143 — IDEAL Termin-Quelle Berechnet**
  - Umschalten, Tooltip, Termin aus dem Kopf-`FE_Termin`.
  - Zurueckschalten auf Automatisch.
- **TS-144 — Kennzeichen-Quelle und Dienst**
  - Protokoll „uebersprungen“ je Weg.
  - Sage-Import-Hook bei `CoatingDetectionEnabled = false`.
  - Audit `ModifiedBy` in `ProductionOrderPickingStatus` nach einem Materialisierungslauf.
- **TS-145 — Vorschlagsliste und Beschichtungsauftrag an der AKE**
  - Vorbedingung: P-A3 erhoben (alle WA-Nummern numerisch), ~~P-A6 erhoben (Lackierteil-Positionen ganzzahlig)~~ *ueberholt (Nachtrag 3):* P-A6 erhoben (nicht ganzzahlige, mehrstufige Positionen, 0 Doppelungen), SQL/157 eingespielt, H3 eingetragen (Nachtrag 2), Gate an, Rolle zugewiesen, Lackier-WA mit `HasCoatingParts = true` und BomCache. Fuer die Positionsfaelle einen WA mit mehrstufigen Positionen per P-A9 waehlen.
  - Vorschlagsliste (Summiert und Liste): WA erscheint mit seinen Lackierteilen, Masse 0, Kopfzeile „Keine Auftrags-Kopfdaten zu diesem HauptFA.“; Spaltenfilter und Paging funktionieren.
  - „Aus Vorschlagsliste erstellen“: Auftrag mit den Lackierteil-Positionen, Start mit dem berechneten Termin vorbelegt, Dienstleister/RAL/Retour leer und manuell speicherbar.
  - Negativfaelle: ohne Rolle bzw. Gate aus kein Menue, Direktaufruf Redirect plus Warnung; ohne Schreibrolle kein Erstellen-Knopf; WA ohne Lackierteile nicht in der Liste; zweite Anlage desselben WA zeigt die Positionen als vergeben.
  - IDEAL-Gegenprobe: Vorschlagsliste und Auftrag wie vorher (Kopfdaten aus FAInfos).
  - **Nachtrag 2:**
    - **Mengen-Pruefschritt (H3, vor E5):** Den WA aus der Hilfsabfrage H3 nehmen (`Quantity > 1`, Zeilen mit `Baugruppe`). In der Kommissionierliste die Mengen der Baugruppen-Zeilen gegen Sage/OSEON abgleichen und das Ergebnis eintragen. Danach in der Vorschlagsliste und im daraus angelegten Auftrag pruefen, dass dieselben Zeilen dieselben Mengen wie die Kommissionierliste zeigen.
    - **Negativfall Sammelmeldung (S-2):** Ein Lackier-WA mit `HasCoatingParts = 1`, dessen Stuecklistenkopf fehlt, ist nicht in der Liste. Die Sammelmeldung nennt ihn mit Nummer, das Log hat die Warnung. Vorbereitung auf DEV: einen solchen WA per P-A7 suchen. Gibt es keinen, notieren „nicht herstellbar“; kein manuelles Loeschen im Cache.
    - **Negativfall storniert (S-3):** Ein stornierter Lackier-WA (`IsCancelled = 1`) erscheint weder in der Liste noch in der Sammelmeldung.
    - **Grenzfall Stuecklistenaenderung (S-4):** Nach der Anlage eines Auftrags aendert sich die Stueckliste des Artikels; nach dem naechsten BomCache-Lauf die Vergabe-Anzeige pruefen. Die vergebene Zeile bleibt vergeben (Schluessel Artikelnummer + Position), eine neue Zeile ist frei. Nicht herstellbar → notieren.
    - **Kopfzeile:** Die Kopfzeile bleibt „Keine Auftrags-Kopfdaten zu diesem HauptFA.“, auch wenn die Sammelmeldung erscheint.
  - **Nachtrag 3 (`PositionText`):**
    - **Schema:** Nach dem Update `SELECT COL_LENGTH('dbo.BeschichtungsauftragPositionen','PositionText')` = 100 (nvarchar(50)); MigrationId in `__EFMigrationsHistory`; Skript 157 ein zweites Mal ausfuehren → keine Aenderung, kein Fehler. Bestandszeilen `PositionText IS NULL`.
    - **Anlage mit mehrstufigen Positionen:** WA aus P-A9 (Positionen wie `51.5.2` und `51.5.10`). „Aus Vorschlagsliste erstellen“ → in den Auftrag-Details steht in der Spalte „Vater / SubFA / Pos“ der Positionstext (`— / 0 / 51.5.2`), nicht „—“; per SQL `PositionText` gefuellt, `Position` NULL.
    - **Reihenfolge:** In den Auftrag-Details (Positionsspalte) steht `51.5.2` vor `51.5.10` und `5.2` vor `5.10`; eine Ebene steht vor ihren Unterpositionen (`51.5` vor `51.5.2`). Die Vorschlagsliste (Liste) hat keine Positionsspalte; ihre Zeilen des HauptFA stehen in derselben Reihenfolge wie in den Details (Artnr-Folge vergleichen). Reklamationsauswahl eines erledigten Auftrags: dieselbe Reihenfolge.
    - **Vergabe-Anzeige:** Nach der Anlage zeigen genau die uebernommenen Zeilen das Badge „in BA-…“; eine zweite Anlage desselben WA meldet „bereits vergeben“ mit Positionstexten im Meldetext.
    - **Positionen hinzufuegen:** Im Entwurf eine Position entfernen und ueber „Positionen hinzufuegen“ wieder auswaehlen → sie kommt mit demselben Positionstext zurueck (Fuenf-Teile-Schluessel funktioniert im Formular).
    - **Negativfall Doppelung:** Gibt es per P-A6 (zweite Abfrage) einen doppelten Schluessel (Artnr, Position), erscheint bei der Anlage der Hinweis „Herkunftsschluessel … nicht eindeutig“. P-A6 zeigt 0 → notieren „nicht herstellbar“, keine Daten anlegen.
    - **Negativfall gemischt/nicht numerisch:** Gibt es laut P-A9 eine Position mit Buchstaben, steht sie hinter den numerischen Positionen derselben Ebene. Sonst „nicht herstellbar“ notieren.
    - **Negativfall Altzeile:** Update-Skript-Lauf zeigt `Pruefung/BeschichtungPositionOhneTextFlach: OK` an AKE und IDEAL. Nur DEV: eine Testposition ohne `PositionText` an einen AKE-Testauftrag haengen → die Pruefung meldet „Abweichung 1“ und die Vergabe-Anzeige zeigt die Zeile nicht als vergeben; Testzeile danach loeschen.
    - **IDEAL-Gegenprobe:** Ein neuer IDEAL-Auftrag hat `PositionText` NULL, Anzeige und Reihenfolge wie vor dem Update.

Danach `secondbrain/tests/testszenarien-index.md` nachziehen.

## Etappen (nur bei epic: true)

**Epic statt Split:**

- Alle Etappen teilen einen Kern, den Resolver und die beiden Keys. Einzeln gemergt, laege nach E1 ruhender Code ohne Verbraucher in `main`.
- Die Arbeit laeuft im vorhandenen Buendel-Worktree `feature/2026-08-07-ideal-teile-1-5` (ein Merge am Ende).
- Den Branch waehrend der Arbeit mit `scripts/sync-worktree.ps1 -Slug 2026-08-07-ideal-teile-1-5` auf main-Stand halten.
- Jede Etappe baut und testet fuer sich gruen und endet mit einem Commit. ~~Keine Etappe wartet auf eine offene Frage: Wo eine Antwort fehlt, baut der `/goal`-Lauf die empfohlene Antwort und vermerkt das (Rueckfrage 18), oder er haelt die Etappe als Blocker fest (E4 ohne P-I1-Ergebnis, E5 ohne P-A5-Ergebnis oder bei nicht-numerischen WA-Nummern in P-A3).~~ *ueberholt durch Nachtrag 2:* Keine Etappe wartet auf eine offene Frage. Fehlt ein Haltepunkt-Ergebnis, haelt der Lauf die abhaengigen Etappen als Blocker fest und macht mit den unabhaengigen weiter:
  - ohne P-I1 (H1): **E2 und E4** Blocker, weiter mit E3, H2/H3, E5 (S-1);
  - ~~ohne P-A5, bei nicht-numerischen WA-Nummern in P-A3 oder nicht-ganzzahligen Positionen in P-A6 (H2): E5 Blocker;~~ *erledigt (Nachtrag 3):* H2 ist erhoben (P-A3 = 0, P-A5 ohne Datumsspalte, P-A6 nicht ganzzahlig → Spalte `PositionText` nach Freigabe-Antwort 20);
  - ohne H3-Eintrag oder bei Mengen-Abweichung (H3): E5 Blocker.
- ~~Alle Rueckfragen 1-18 sind beantwortet (Freigabe-Antworten). Die Spalte „Abhaengig von“ nennt die Antwort, nach der gebaut wird. Rueckfrage 19 aendert nichts am Bau, nur an der Nutzbarkeit. Rueckfrage 20 greift nur, wenn P-A6 > 0 ist.~~ *ueberholt (Nachtrag 3):* Alle Rueckfragen 1-20 sind beantwortet (Freigabe-Antworten). Die Spalte „Abhaengig von“ nennt die Antwort, nach der gebaut wird. Einziger offener Haltepunkt fuer E5 ist H3.
- E5 hat eine eigene SQL-Nummer (157, beim Start pruefen) und baut mit dem Skript fuer sich gruen.

| # | Etappe | Inhalt | Abhaengig von | SQL | Status | Commit |
|---|--------|--------|---------------|-----|--------|--------|
| 1 | Katalog + Resolver | 2 Choice-Keys (Termin-Quelle ohne `SData`), Beschreibungsergaenzungen, Server-Validierung (A8, `View` an der AKE abgelehnt), `BeschichtungResolver` plus Matrix-Tests (View = `Start_Beschichtung` 1:1); noch kein Verbraucher umgestellt; kein SData-Code (Freigabe-Antwort 17) | Freigabe-Antworten 1, 2, 4, 15, 17 | – | erledigt (QA 2026-11-16 gruen) | c61b7a52 |
| H1 | **Haltepunkt P-I1** (vor E2, Nachtrag 2 / S-1) | Gerald erhebt P-I1 (Schalterstand IDEAL) und traegt das Ergebnis im Abschnitt „Pruefabfragen“ ein. Grund fuer die Lage vor E2: E2 stellt die Aktiv-Kennung der Zeilen-Views (`_ProductionOrderRow.cshtml:16-20`, `_PickingLeitstandRow.cshtml:82-94`) auf den Resolver um. Das ist die Verhaltensaenderung aus Freigabe-Antwort 7. Liegt kein Ergebnis vor, vermerkt der Lauf **E2 und E4** als Blocker und macht mit E3, H2/H3 und E5 weiter. | Gerald; Freigabe-Antwort 7 | – | erledigt (P-I1 eingetragen 2026-11-17) | |
| 2 | Web-Verbraucher | FA-Liste (Inline-Formel weg), Leitstand, Arbeitsliste, Zeilen-Views (Aktiv-Kennung), Tooltips je `TerminQuelle` auf den Resolver; Waechter-Test AK 9; Verhalten mit Automatisch identisch | E1, **H1**; Freigabe-Antwort 7 | – | erledigt (QA 2026-11-17 gruen, Teilumfang E2/E4) | 2336dced |
| 3 | Dienst | Gates ueber `ResolveQuellen`, Hook-Gate, Audit `SetCoatingPartsAsync`, Protokoll-Meldungen | E1; Freigabe-Antwort 12 | – | erledigt (QA 2026-11-16 gruen) | 318abf1f |
| 4 | IDEAL-Termin Leitstand/Arbeitsliste | Koepfe je Seite nachladen, Termin aus dem Resolver (View = `Start_Beschichtung` unveraendert); Basisdatum Berechnet = Kopf-`FE_Termin` | E2 (und damit H1); Freigabe-Antworten 3, 4, 5, 6 | – | erledigt (QA 2026-11-17 gruen, Teilumfang E2/E4) | b4972700 |
| H2 | **Haltepunkt P-A5 (+ P-A3, P-A6 als Beleg)** | Gerald erhebt P-A5 (Datumsspalte in `vw_AKE_Kommissionierung_WAListe`?), P-A3 (WA-Nummern numerisch) und P-A6 (Lackierteil-Positionen ganzzahlig) und traegt die Ergebnisse im Abschnitt „Pruefabfragen“ ein. P-A5 aendert in dieser Version nichts am Bau (`View` an der AKE bleibt gesperrt). P-A3 und P-A6 muessen 0 Abweichungen zeigen, sonst E5 Blocker (P-A6: Rueckfrage 20). *Nachtrag 3:* P-A6 > 0 ist durch Freigabe-Antwort 20 aufgeloest (Spalte `PositionText` in E5). | Gerald; Freigabe-Antworten 9, 15, 20; Nachtrag 2/3 | – | **erledigt** (erhoben 2026-11-17: P-A3 0 nicht numerisch, P-A5 keine Datumsspalte, P-A6 3.812/4.444 nicht ganzzahlig, 0 Doppelungen) | |
| H3 | **Haltepunkt Mengen-Pruefschritt** (Nachtrag 2 / B-1) | Gerald waehlt per Hilfsabfrage H3 einen WA mit Baugruppe und `Quantity > 1`. Er vergleicht in der Kommissionierliste die Mengen der Baugruppen-Zeilen mit Sage/OSEON und traegt WA, Zeilenzahl und Ergebnis im Abschnitt „Pruefabfragen“ ein (AK 21). Fehlt der Eintrag oder weicht die Menge ab, ist E5 Blocker. | Gerald; Freigabe-Antwort 18 | – | **erledigt** (WA 2602036 / S0415949, 12 Baugruppen-Zeilen, Kommissionierliste = Sage, Bestaetigung Gerald 2026-11-17 12:49) | |
| 5 | Vorschlagsliste + Beschichtungsauftrag AKE | Gate-Metadaten (beide), **keine** Master-Bedingung an der Vorschlagsliste. Flache Positionsquelle (WA-Stueckliste als `FaHierarchyNode`-Projektion) fuer `BeschichtungsauftragService` (Liste/Summiert), `BeschichtungsauftragVergabeAnzeige` und die Workflow-Weiche: offen = `IsDone = 0 AND IsCancelled = 0` (A13), Menge ueber `BomQuantityResolver` (A12), Schluessel Artnr + Position (A14), Sammelmeldung plus Logger-Warnung (A15). AKE-Kopfvorbelegung (Start berechnet, Dienstleister/RAL/Retour leer). „(IDEAL)“ aus den Spaltenkatalog-Namen. *Nachtrag 3:* Spalte `PositionText` (Model, Migration, SQL/157, FreshInstall an zwei Stellen, Manifest), `[NotMapped] FaHierarchyNode.PositionText`, Projektion `PositionText` statt `int(Position)`, `PositionsSchluessel` mit `Artnr`/`PositionText`, Textform fuenfteilig, Snapshot/Kopie/Summiere, natuerliche Sortierung mit `NaturalPositionComparer` (A16), Anzeige in Details/Reklamation, Manifest-Pruefung Altzeilen | E1, E3, H2 (erledigt), **H3**; Freigabe-Antworten 8, 9, 10, 11, 18, 19, 20 | **SQL/157** (beim Start geprueft: frei) | **erledigt** (Lauf 2026-11-17, [[2026-11-17-goal-lauf-beschichtung-e5]], v1.99.34; ~~H2/H3 offen (Lauf 2026-11-16)~~ H1–H3 erledigt) | eff0c1a4 |
| 6 | Doku und Abschluss | TS-140..145 in `docs/TESTSZENARIEN.md`, Hilfe (`Help/Index.cshtml` Beschichtung, `:1265`), `RoleOverview.cshtml` (Vorschlagsliste auch AKE/WA-Stueckliste), Standort-Maske Hinweis-Link, Anwender-Changelog (IDEAL-Termin in Leitstand/Arbeitsliste, AKE-Beschichtung), `docs/betrieb/04-konfiguration.md`, `README.md` AppSettings, Manifest-Eintraege, Versions-Bump beide `AppVersion.cs` auf v1.99.25 (nach 1.99.24 Sicherheit; beim Start pruefen), Brain (ADR 0028, fallstricke, codebase, glossar, feature-map, changelog; Aufgabe Erkennungsfenster-Filter, Rueckfrage 13). Nachtrag 2: Hilfe nennt Sammelmeldung und Mengenregel. fallstricke bekommt „BomCache-Fenster ohne `IsDonePicking`, Erkennungsfenster mit“ samt Folge fuer das Kennzeichen (Rueckfrage 19, entschieden: hingenommen), mit Warum. *Nachtrag 3:* fallstricke „AKE-Stuecklisten-Positionen sind mehrstufiger Text (`51.5.2`), nie als `int` parsen; Reihenfolge nur ueber `NaturalPositionComparer`“ mit Beleg P-A6; `codebase/datenmodell.md` Spalte `PositionText` (SQL/157); TS-145 um die Faelle aus Nachtrag 3; Manifest `SQL/157` und `Pruefung/BeschichtungPositionOhneTextFlach`; Version = naechste nach 1.99.33 beim Start (voraussichtlich 1.99.34); kein neuer ADR | E1-E5 | – | teilweise (Teil E1/E3, Version 1.99.25, Hilfe, Changelog, Manifest; Rest **Blocker** wie E2/E4/E5) | 4a635534, 3f247f11 |

## Deploy

*Vorlaeufig, der QA-Lauf bestaetigt gegen den echten Diff.*

- **Web-App:** ja (Settings-Katalog, Resolver, Controller, Views, Vorschlagsliste, Beschichtungsauftrag)
- **Service:** ja (Gates, Hook, Audit). Web und Dienst gemeinsam deployen, weil beide den Katalog teilen.
- ~~**Migration:** nein, kein SQL-Skript (Freigabe-Antwort 9; die fruehere bedingte SQL/152 ist *ueberholt*).~~ *ueberholt durch Nachtrag 3:* **Migration: ja, mit E5** — `SQL/157_AddBeschichtungsauftragPositionText.sql`, MigrationId `<ts>_AddBeschichtungsauftragPositionText`, additiv (`PositionText nvarchar(50) NULL`), nicht daten-destruktiv, kein Backfill. Das Standard-Backup des Update-Skripts vor dem Schema genuegt; kein Sonder-Backup, kein Dienst-Sonderhalt. Die fruehere bedingte SQL/152 bleibt *ueberholt*. E1-E4 allein brauchen keine Migration.
- **Version:** E1/E3 kamen mit 1.99.25, E2/E4 mit 1.99.32. E5 kommt mit der naechsten freien Version nach dem Stand beim Start (heute 1.99.33 → voraussichtlich **1.99.34**). Jeder neue Manifest-Eintrag traegt **diese** Auslieferungsversion, nicht 1.99.25: Das Update-Skript zeigt Eintraege nur bei `Version > installiert` (`Update-IdealAkeWms.ps1:268`, `Test-Neuer`, QA-Befund R-1).
- **Manifest-Eintraege** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023; ~~Version v1.99.25 laut Freigabe-Antwort 16, beim Start pruefen~~ *praezisiert (Nachtrag 3):* vorhandene Eintraege mit ihrer Version, neue E5-Eintraege mit der Auslieferungsversion `<v>` von E5):

| Id | Version | Typ | Firma | Phase | Ausfuehrung | Text / Pruefung |
|---|---|---|---|---|---|---|
| `Pruefung/BeschichtungQuelleKombination` | 1.99.25 | Pruefung | beide | Haltepunkt | anzeigen | Pruefung (Zeilenzahl > 0 = Befund, siehe unten). Text: „Beschichtungsquelle passt nicht zum Auftragsmodell, wirkt wie Automatisch; unter /Settings korrigieren.“ |
| `v1.99.25-Beschichtungsquelle-Info` | 1.99.25 | Info | beide | Nachher | anzeigen | „Neue Einstellungen Quelle Beschichtet-Kennzeichen / Quelle Beschichtungstermin stehen auf Automatisch, das Verhalten ist unveraendert. IDEAL: Leitstand und Werkbank-Arbeitsliste zeigen jetzt den Beschichtungstermin aus der Sage-View.“ |
| ~~`v1.99.25-Beschichtungsauftrag-AKE`~~ *ueberholt (Nachtrag 3, R-1):* `v<v>-Beschichtungsauftrag-AKE` | ~~1.99.25~~ `<v>` (Auslieferung E5, voraussichtlich 1.99.34) | Einstellung | AKE | Nachher | anzeigen | „Optional: Beschichtungsauftrag aktiv (FaHierarchyBeschichtungAktiv) unter /Settings einschalten; dann erscheinen Vorschlagsliste (Lackierteile der WA-Stueckliste) und Auftraege im Menue Beschichtung. Rollen beschichtungsauftrag / beschichtungsauftrag_schreiben zuweisen (Anlage: Pruefung/RollenBeschichtungsauftrag). Dienstleister/RAL/Retour werden am Auftrag von Hand gepflegt.“ |
| `SQL/157` (Nachtrag 3; Nummer beim Start pruefen) | `<v>` | Schema | beide | Schema | automatisch | `Datei='157_AddBeschichtungsauftragPositionText.sql'; MigrationId='<ts>_AddBeschichtungsauftragPositionText'; Braucht=@('SQL/94'); Voraussetzung='additiv, Spalte nvarchar(50) NULL, kein Backfill'; Quelle='<changelog-slug>'`. Im Schema-Block **nach** `SQL/156` (Deploy-Reihenfolge, nie Dateisortierung). |
| `Pruefung/BeschichtungPositionOhneTextFlach` (Nachtrag 3) | `<v>` | Pruefung | beide | Nachher | anzeigen | Pruefung siehe unten. Text: „Beschichtungsauftrag-Positionen ohne Positionstext an einem System mit flachem Auftragsmodell: die Vorschlagsliste erkennt sie nicht als vergeben. Positionen pruefen (nur moeglich, wenn vor v<v> Auftraege ohne FA-Struktur angelegt wurden).“ |

~~`SQL/152` (nur falls Rueckfrage 9 = Spalte), Schema, `152_AddBeschichtungsauftragAuftragsnummer.sql`~~ *ueberholt durch Freigabe-Antwort 9: kein Schema-Eintrag.*

Pruefabfrage zu `Pruefung/BeschichtungQuelleKombination`:

*Fruehere Fassung, ueberholt durch Nachtrag 2 (S-5).* Fehlte die ServiceSettings-Zeile, lieferte der `CROSS JOIN` 0 Zeilen, und die Pruefung meldete still OK:

```sql
-- UEBERHOLT, nicht verwenden
SELECT COUNT(*) FROM dbo.AppSettings a CROSS JOIN (SELECT LOWER(LTRIM(RTRIM([Value]))) m FROM dbo.ServiceSettings WHERE [Key]=N''ProduktionsauftragHierarchisch'') s WHERE (a.[Key]=N''BeschichtungKennzeichenQuelle'' AND ((a.[Value]=N''Artikelkategorie'' AND s.m=N''true'') OR (a.[Value]=N''FaStruktur'' AND s.m<>N''true''))) OR (a.[Key]=N''BeschichtungTerminQuelle'' AND a.[Value]=N''View'' AND s.m<>N''true'')
```

Gueltige Fassung (Schreibweise wie im `.psd1`, Hochkommas verdoppelt). Die Master-Zeile wird als Skalar mit `COALESCE(..., 'false')` gelesen. Das ist derselbe Default wie im Katalog (`ServiceSettingDefinitions.cs:222`) und im Dienst (`IHierarchicalModeReader.cs:21`, `GetBoolSafeAsync(..., false)`):

```sql
SELECT COUNT(*) FROM dbo.AppSettings a CROSS JOIN (SELECT COALESCE((SELECT LOWER(LTRIM(RTRIM([Value]))) FROM dbo.ServiceSettings WHERE [Key]=N''ProduktionsauftragHierarchisch''), N''false'') m) s WHERE (a.[Key]=N''BeschichtungKennzeichenQuelle'' AND ((a.[Value]=N''Artikelkategorie'' AND s.m=N''true'') OR (a.[Value]=N''FaStruktur'' AND s.m<>N''true''))) OR (a.[Key]=N''BeschichtungTerminQuelle'' AND a.[Value]=N''View'' AND s.m<>N''true'')
```

**Verhalten im Update-Skript** (`Ausfuehrung = anzeigen`, belegt in `scripts/deploy/Update-IdealAkeWms.ps1:419-434`, Manifest-Kopf `nachlauf.psd1:12-17`):

- Die Pruefung laeuft bei **jedem** Lauf, ohne Versionsfilter (`nachlauf.psd1:12`). Phase Haltepunkt bedeutet: nach dem Schema, bei gestopptem Web und Dienst (`:14`). Die Nachlauf-Liste zeigt sie erneut mit dem Stand danach (`:17`).
- Ergebnis 0: eine Zeile „Pruefung Pruefung/BeschichtungQuelleKombination: OK“ (`Update-IdealAkeWms.ps1:432`). Ansonsten still: kein Abbruch, keine Rueckfrage.
- Ergebnis > 0: eine gelbe Zeile „Abweichung n – Text“ (`:433`) und eine Warnung „von Hand: Text“ (`:434`). Es gibt keine Reparatur, keinen Abbruch, Web und Dienst laufen normal weiter. Der Text ist der Hinweis an den Menschen („unter /Settings korrigieren“).
- Abfragefehler: Warnung „Fehler: …“ (`:422`, `:431`), kein Abbruch.
- Beim ersten Deploy gibt es die beiden Keys erst nach dem Web-Start (Startup-Seed). Am Haltepunkt ergibt die Pruefung deshalb 0 und meldet OK. Massgeblich ist der Stand in der Nachlauf-Liste.

Kein Einmal- oder Daten-Skript, also auch keine Trockenlauf-Pflicht.

Pruefabfrage zu `Pruefung/BeschichtungPositionOhneTextFlach` (Nachtrag 3; Schreibweise wie im `.psd1`, Master-Default wie AK 23):

```sql
SELECT COUNT(*) FROM dbo.BeschichtungsauftragPositionen p WHERE p.PositionText IS NULL AND COALESCE((SELECT LOWER(LTRIM(RTRIM([Value]))) FROM dbo.ServiceSettings WHERE [Key]=N''ProduktionsauftragHierarchisch''), N''false'') <> N''true''
```

- Phase Nachher: Die Spalte existiert dann sicher (Schema lief vorher im selben Lauf). Ergebnis 0 = „OK“, > 0 = Hinweiszeile plus Warnung, keine Reparatur, kein Abbruch (Verhalten wie oben, `Update-IdealAkeWms.ps1:419-434`).
- Erwartung nach Code und P-A8: 0 an allen Systemen. Ein Treffer heisst: An einem flachen System gibt es Positionen aus der Zeit vor E5; das ist eine Rueckfrage an Gerald, keine automatische Korrektur.

- **Publish-Befehle** (im Worktree):
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  - `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- Gilt im Buendel: DEV AKE nie mit dem main-Stand deployen.

### Pruefabfragen (nur lesend, vom Menschen auszufuehren; Ergebnisse vor Schranke 1 hier eintragen)
> **Ergebnisse 2026-11-17 (Koordination, IDEALSQL2, [[2026-11-17-pruefabfragen-gerald]]):** P-I1 PROD (`IDEAL_AKE_WMS`): FaHierarchyBeschichtungAktiv false, LackierteilKategorieName leer, ProduktionsauftragHierarchisch/ProductionOrders/BomCache/HierarchicalFa false, CoatingDetection false, Views FAListe/FAInfos, Quelle FaStruktur View. TEST (`IDEAL_AKE_WMS_TEST`): BeschichtungAktiv true, Hierarchisch/ProductionOrders/BomCache/HierarchicalFa true, CoatingDetection false, Kategorie leer. BeschichtungTage 10, Abholtage Di,Do (beide). AKE-Abfragen offen.
> **Ergebnisse AKE 2026-11-17 11:52–11:56 (Koordination, AKESQL20, WMS-DB von 4445 = `IDEAL_AKE_WMS_TESTIDEAL` (SageSystemKuerzel ATI), Sage `ake_TEST2026`, nur SELECT; [[2026-11-17-pruefabfragen-gerald]]):** P-A2 Kategorie „Lackierteile“ 6.926 Artikel. **P-A3** 4.524 FAs, **0 nicht numerisch** (int bleibt). P-A4 offene FAs: HasCoatingParts=1 → 161 (nicht kommissioniert, 156 mit Cache) + 1.200 (kommissioniert, 342 mit Cache); =0 → 343 + 2.657 – die Lackierteil-Erkennung arbeitet. **P-A7** gekennzeichnete WAs ohne Stücklisten-Cache: 5 nicht kommissioniert (Bsp. 2644248), 858 kommissioniert (Bsp. 2602035). Cache-Köpfe 367. **P-A6: 3.812 von 4.444 Lackierteil-Zeilen haben eine nicht ganzzahlige Position** (z. B. 5.2, 7.5, 51.5.2, 51.6.3 – mehrstufig), doppelte Schlüssel Artnr+Position 0 → Rückfrage 20 greift: Spalte `PositionText` nötig (Antwort 20), E5 bleibt bis zum Spec-Nachtrag Blocker. **P-A5** `vw_AKE_Kommissionierung_WAListe` (ake_TEST2026 und ake, 8 Spalten): Datumsspalten nur `Fertigungstermin`, `Liefertermin` – **kein Beschichtungsdatum** (View an der AKE bleibt gesperrt). **H3-Kandidaten** (Quantity 2, Zeilen mit Baugruppe): 2602036/2602043/2602053/2602057 (S0415949, 81 Zeilen), 2602038/2602045/2602055 (S0415967, 176), 2602037/2602044/2602054 (S0415966, 173) – Vergleich Kommissionierliste ↔ Sage/OSEON durch Gerald offen.

> **Nachtrag 3 (2026-11-17, Auftrag Gerald) – Haltepunkt-Stand:**
> - **P-A6** (AKE-Test `IDEAL_AKE_WMS_TESTIDEAL`, 2026-11-17): 3.812 von 4.444 Lackierteil-Stuecklistenzeilen mit nicht ganzzahliger, teils mehrstufiger Position (`5.2`, `51.5.2`), doppelte Schluessel Artnr + Position: 0. Nach Freigabe-Antwort 20 kommt die Spalte `PositionText` (E5, SQL/157); P-A6 ist damit kein Blocker mehr. Hinweis zur Zaehlung: `TRY_CAST('' AS int)` ergibt 0, leere Positionen zaehlen in P-A6 also als „ganzzahlig“; P-A9 zaehlt sie getrennt.
> - **P-A3** (0 nicht numerisch) und **P-A5** (keine Beschichtungs-Datumsspalte in `vw_AKE_Kommissionierung_WAListe`) sind eingetragen (oben).
> - **H2 ist damit erledigt** (P-A3, P-A5, P-A6 erhoben).
> - **H3 ist weiter offen:** Kandidaten-WAs stehen oben, der Mengenvergleich Kommissionierliste ↔ Sage/OSEON durch Gerald fehlt. E5 bleibt bis zum H3-Eintrag Blocker (AK 21).
>
> Neue Pruefabfragen (nur lesend), beide **nicht** blockierend:
>
> - **P-A8** Gespeicherte Beschichtungs-Positionen je System (vor dem Deploy von E5; Erwartung an AKE Test/Prod und IDEAL Prod mit Master aus: 0). Ein Treffer an einem System mit Master aus heisst, dass dort Positionen ohne `PositionText` entstehen wuerden, die die flache Quelle nicht als vergeben erkennt:
>
>   ```sql
>   SELECT [Value] master FROM dbo.ServiceSettings WHERE [Key]='ProduktionsauftragHierarchisch';
>   SELECT COUNT(*) positionen, COUNT(DISTINCT p.BeschichtungsauftragId) auftraege FROM dbo.BeschichtungsauftragPositionen p;
>   ```
>
> **Ergebnis P-A8/P-A9 2026-11-17 12:42 (Koordination, AKESQL20 `IDEAL_AKE_WMS_TESTIDEAL` = 4445, nur SELECT):** P-A8 Master `ProduktionsauftragHierarchisch` = false, `BeschichtungsauftragPositionen` 0 Zeilen / 0 Aufträge (erwartet). `IDEAL_AKE_WMS` (AKE-Prod-Kopie auf AKESQL20, Migration 07.08.) hat die Tabelle noch nicht – dort P-A8 erst nach dem Update. P-A9: leere Positionen 0, nicht Ziffer/Punkt 0, **max. Länge 15**, **max. 4 Punkte (5 Ebenen)**, z. B. S1402262 / 87032384 `10.140.40.10.10`, `10.140.40.20.40`, `20.150.40.10.10` (WA zum Artikel per `ProductionOrders.ArticleNumber`).
>
> **H3 Mengen-Prüfschritt 2026-11-17 12:47 (Koordination auf Auftrag Gerald, AKESQL20, nur SELECT):** WA **2602036** (Artikel S0415949, `USER_FASollmenge` = 2), **12 Zeilen mit Baugruppe auf 3. Ebene** (Positionen 150.30.10 … 160.20.20) verglichen. Ergebnis: **Kommissionierliste = Sage** – WMS-Menge je Stück = Sage-Menge in der Baugruppe × Menge der Baugruppe im übergeordneten Artikel (z. B. 160.20.10: 1 × 2 = 2; 150.30.10: 2 × 1 = 2), Kommissionierliste = × 2 (WA-Stückzahl). Grund: `vw_AKE_Kommissionierung_StuecklistenDB` multipliziert rekursiv (`Mengenprodukt = r.Mengenprodukt * p.Menge`), der Cache-Wert ist also Menge je Endartikel; `BomQuantityResolver` × Quantity ist damit richtig (A12 bestätigt). Sage selbst führt am WA keine aufgelöste Materialliste (`KHKIFAuftraegeStuecklisten` hat für 2602036 nur 1 Zeile, den Endartikel) – Vergleichsbasis ist die Sage-Stückliste `KHKPpsRessourcenPositionen`. OSEON nicht verglichen. **Bestätigung Gerald 2026-11-17 12:49: „passt“ – H3 erledigt, E5 frei.**
>
> - **P-A9** TS-Vorbereitung (AKE, WMS-DB): WAs mit tiefen Positionen, leere und nicht numerische Positionen, groesste Laenge:
>
>   ```sql
>   SELECT TOP 20 h.Artikelnummer, i.Ressourcenummer, i.Position, LEN(i.Position)-LEN(REPLACE(i.Position,'.','')) punkte
>   FROM dbo.CachedBomHeaders h JOIN dbo.CachedBomItems i ON i.CachedBomHeaderId=h.Id JOIN dbo.Articles a ON a.ArticleNumber=i.Ressourcenummer JOIN dbo.ArticleCategories c ON c.Id=a.ArticleCategoryId
>   WHERE c.Name=(SELECT [Value] FROM dbo.AppSettings WHERE [Key]='LackierteilKategorieName')
>   ORDER BY punkte DESC, h.Artikelnummer;
>   SELECT SUM(CASE WHEN LEN(LTRIM(RTRIM(ISNULL(i.Position,''))))=0 THEN 1 ELSE 0 END) leer, SUM(CASE WHEN i.Position LIKE '%[^0-9.]%' THEN 1 ELSE 0 END) mit_nichtziffer, MAX(LEN(i.Position)) max_laenge
>   FROM dbo.CachedBomItems i JOIN dbo.Articles a ON a.ArticleNumber=i.Ressourcenummer JOIN dbo.ArticleCategories c ON c.Id=a.ArticleCategoryId
>   WHERE c.Name=(SELECT [Value] FROM dbo.AppSettings WHERE [Key]='LackierteilKategorieName');
>   ```
>
>   Die WA-Nummer zum Artikel liefert `SELECT OrderNumber FROM dbo.ProductionOrders WHERE ArticleNumber=N'<Artikelnummer>' AND IsDone=0 AND IsCancelled=0`.

**IDEAL – WMS-Datenbank**

- **P-I1** Schalterstand:

  ```sql
  SELECT 'App' Ort,[Key],[Value] FROM dbo.AppSettings WHERE [Key] IN ('LackierteilKategorieName','FaHierarchyBeschichtungAktiv','BeschichtungTage','BeschichtungAbholtage')
  UNION ALL SELECT 'Svc',[Key],[Value] FROM dbo.ServiceSettings WHERE [Key] IN ('ProduktionsauftragHierarchisch','Sync:CoatingDetectionEnabled','Sync:ProductionOrdersEnabled','Sync:BomCacheEnabled','Sync:HierarchicalFaEnabled','Sync:FaHierarchyListeViewName','Sync:FaHierarchyInfosViewName','Sync:Quelle:FaStruktur');
  ```

- **P-I2** Fuellgrad Kopf-Termine gegen das Lack-Flag:

  ```sql
  SELECT COUNT(DISTINCT po.OrderNumber) HauptFAs_mit_Lack, SUM(CASE WHEN oi.Start_Beschichtung IS NULL THEN 1 ELSE 0 END) ohne_Start, SUM(CASE WHEN oi.Dienstleister IS NULL THEN 1 ELSE 0 END) ohne_Dienstleister
  FROM dbo.ProductionOrders po JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId=po.Id AND ps.HasCoatingParts=1
  LEFT JOIN dbo.FaHierarchyOrderInfos oi ON oi.HauptFA=TRY_CAST(po.OrderNumber AS int) WHERE po.IsDone=0;
  ```

- **P-I3** Flag-Bestand und Audit-Luecke:

  ```sql
  SELECT HasCoatingParts, IsCoatingDone, ModifiedBy, COUNT(*) n FROM dbo.ProductionOrderPickingStatus GROUP BY HasCoatingParts, IsCoatingDone, ModifiedBy ORDER BY n DESC;
  ```

**IDEAL – Sage-Datenbank** (nur SELECT)

- **P-I4** Herkunft der Spalten:

  ```sql
  SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.vw_IDEAL_AKE_WMS_FAListe')); SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.vw_IDEAL_AKE_WMS_FAInfos'));
  ```

  View-Namen gegen P-I1 abgleichen. Ziel: woraus `Beschichtet` und `Start_Beschichtung` wirklich stammen (Rueckfrage 4).

- **P-I5** Werte:

  ```sql
  SELECT Beschichtet, COUNT(*) FROM dbo.vw_IDEAL_AKE_WMS_FAListe GROUP BY Beschichtet;
  SELECT TOP 50 HauptFA, Start_Beschichtung, Beschichten_Retour, Dienstleister, RAL, FE_Termin FROM dbo.vw_IDEAL_AKE_WMS_FAInfos WHERE Start_Beschichtung IS NOT NULL ORDER BY Start_Beschichtung DESC;
  ```

  Ziel: Liegt `Start_Beschichtung` plausibel vor `FE_Termin`?

**AKE – WMS-Datenbank**

- **P-A1** Schalterstand: wie P-I1.
- **P-A2** Kategorie vorhanden:

  ```sql
  SELECT c.Name, COUNT(a.Id) Artikel FROM dbo.ArticleCategories c LEFT JOIN dbo.Articles a ON a.ArticleCategoryId=c.Id WHERE c.Name=(SELECT [Value] FROM dbo.AppSettings WHERE [Key]='LackierteilKategorieName') GROUP BY c.Name;
  ```

- **P-A3** WA-Nummern numerisch (Rueckfrage 9):

  ```sql
  SELECT COUNT(*) gesamt, SUM(CASE WHEN TRY_CAST(OrderNumber AS int) IS NULL THEN 1 ELSE 0 END) nicht_numerisch FROM dbo.ProductionOrders;
  ```

- **P-A4** Flag-Bestand und BomCache-Abdeckung (korrigiert in Nachtrag 2, S-6):

  Die fruehere Fassung ist *ueberholt*. Sie nutzte `h.ArticleNumber`; die Spalte heisst `Artikelnummer` (`CachedBomHeader.cs:17`, `CoatingDetectionService.cs:150`, `:155`). Ausserdem stand eine `EXISTS`-Unterabfrage in `SUM`, und SQL Server lehnt Unterabfragen in Aggregaten ab. Gueltige Fassung, Join-Schluessel `h.Artikelnummer`, ohne Duplikate, weil `Artikelnummer` eindeutig ist (`ApplicationDbContext.cs:1098`):

  ```sql
  SELECT ps.HasCoatingParts, ps.IsCoatingDone, ps.IsDonePicking, COUNT(*) n, SUM(CASE WHEN h.Id IS NULL THEN 0 ELSE 1 END) mit_BomCache
  FROM dbo.ProductionOrders po JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId=po.Id
  LEFT JOIN dbo.CachedBomHeaders h ON h.Artikelnummer=po.ArticleNumber
  WHERE po.IsDone=0 AND po.IsCancelled=0
  GROUP BY ps.HasCoatingParts, ps.IsCoatingDone, ps.IsDonePicking;
  ```

- **P-A6** Stuecklisten-Positionen der Lackierteile ganzzahlig? (Nachtrag 2, S-4, vor E5 in H2; Rueckfrage 20)

  ```sql
  SELECT COUNT(*) zeilen, SUM(CASE WHEN TRY_CAST(i.Position AS int) IS NULL THEN 1 ELSE 0 END) nicht_ganzzahlig
  FROM dbo.CachedBomItems i JOIN dbo.Articles a ON a.ArticleNumber=i.Ressourcenummer JOIN dbo.ArticleCategories c ON c.Id=a.ArticleCategoryId
  WHERE c.Name=(SELECT [Value] FROM dbo.AppSettings WHERE [Key]='LackierteilKategorieName');
  SELECT h.Artikelnummer, i.Ressourcenummer, i.Position, COUNT(*) n
  FROM dbo.CachedBomHeaders h JOIN dbo.CachedBomItems i ON i.CachedBomHeaderId=h.Id JOIN dbo.Articles a ON a.ArticleNumber=i.Ressourcenummer JOIN dbo.ArticleCategories c ON c.Id=a.ArticleCategoryId
  WHERE c.Name=(SELECT [Value] FROM dbo.AppSettings WHERE [Key]='LackierteilKategorieName')
  GROUP BY h.Artikelnummer, i.Ressourcenummer, i.Position HAVING COUNT(*)>1;
  ```

  Erwartet: `nicht_ganzzahlig = 0`, sonst E5 Blocker und Rueckfrage 20. Die zweite Abfrage zeigt doppelte Schluessel Artikelnummer + Position. Treffer sind kein Blocker; die Anlage meldet sie (`BeschichtungsauftragWorkflow.cs:427-432`).

- **P-A7** Gekennzeichnete offene WAs ohne Stuecklisten-Cache (Nachtrag 2, S-2; Grundlage fuer Sammelmeldung und Rueckfrage 19):

  ```sql
  SELECT ps.IsDonePicking, COUNT(*) n, MIN(po.OrderNumber) beispiel
  FROM dbo.ProductionOrders po JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId=po.Id
  LEFT JOIN dbo.CachedBomHeaders h ON h.Artikelnummer=po.ArticleNumber
  WHERE po.IsDone=0 AND po.IsCancelled=0 AND ps.HasCoatingParts=1 AND h.Id IS NULL
  GROUP BY ps.IsDonePicking;
  ```

- **H3** Hilfsabfrage Mengen-Pruefschritt (Nachtrag 2, B-1; Ergebnis des Vergleichs hier eintragen: WA, Anzahl Baugruppen-Zeilen, „Kommissionierliste = Sage/OSEON“ ja/nein):

  ```sql
  SELECT TOP 20 po.OrderNumber, po.ArticleNumber, po.Quantity, COUNT(*) zeilen_mit_baugruppe
  FROM dbo.ProductionOrders po JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId=po.Id AND ps.HasCoatingParts=1
  JOIN dbo.CachedBomHeaders h ON h.Artikelnummer=po.ArticleNumber JOIN dbo.CachedBomItems i ON i.CachedBomHeaderId=h.Id
  WHERE po.IsDone=0 AND po.IsCancelled=0 AND po.Quantity>1 AND LEN(LTRIM(RTRIM(ISNULL(i.Baugruppe,N''))))>0
  GROUP BY po.OrderNumber, po.ArticleNumber, po.Quantity ORDER BY zeilen_mit_baugruppe DESC;
  ```

  Pruefschritt: Die Kommissionierliste des gewaehlten WA zeigt `Menge × Quantity` (`PickingController.cs:355`, `BomQuantityResolver.cs:15-16`). Ausnahme: Eine Zeile mit Kommissionierauftrag zeigt dessen `SollMenge`, bei einem Folgeauftrag die Restmenge (`:354-355`). Deshalb einen WA ohne Folgeauftrag waehlen. Diese Mengen fuer die Baugruppen-Zeilen gegen den Sage- bzw. OSEON-Bedarf desselben WA halten. Was in Sage/OSEON als richtig gilt, entscheidet Gerald; die Spec nimmt es nicht an.

  **Aussage Gerald 2026-11-16 (Dev-Lauf-Auftrag):** „B-1 sollte passen.“ Der Pruefschritt H3 (WA, Zeilenzahl, Ergebnis) bleibt offen.

**AKE – Sage-Datenbank**

- **P-A5** Beschichtungsdatum in der WA-Liste? (Rueckfrage 15)

  ```sql
  SELECT c.name, t.name typ FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id WHERE c.object_id=OBJECT_ID('dbo.vw_AKE_Kommissionierung_WAListe') ORDER BY c.column_id;
  ```

## Nachtrag 2026-11-16: Freigabe-Antworten in den Rumpf gezogen

Freigabe-Antworten 1, 4, 9, 10 (Gerald) und die uebrigen („passt“ = Empfehlung) sind in den Rumpf uebernommen. Die Abschnitte „Offene Rueckfragen“, „Freigabe-Antworten“ und „Pruefabfragen“ sind unveraendert (Audit-Trail).

Geaendert:

- **Frontmatter:** `open_questions` enthaelt nur noch die neuen Rueckfragen 17 und 18; 1-16 stehen als `beantwortete_fragen`. `etappen` um die Haltepunkte H1 (P-I1) und H2 (P-A5, P-A3) ergaenzt, E5 um die Vorschlagsliste. `affected_code` um Vorschlagsliste (`FaHierarchyBeschichtungController.cs`, `BeschichtungsauftragService.cs`, `BeschichtungsauftragVergabeAnzeige.cs`, `Views/FaHierarchyBeschichtung/Index.cshtml`, `Summiert.cshtml`, `ColumnDefinitions.cs`), `RoleOverview.cshtml` und `StandortEinstellungen/Index.cshtml` ergaenzt.
- **Antwort 1:** In-Scope „entschieden“; Termin-Quelle `SData` als Erweiterungspunkt unter Out-of-Scope, kein Auswahlwert (A2, Einstellungen, AK 19).
- **Antwort 4:** A4, Resolver, Verbraucher-Tooltip, Einstellungen, AK 6, Matrix-Test und TS-142: `Start_Beschichtung` wird 1:1 uebernommen, kein `CoatingDateCalculator` darauf; Tooltip nennt die Quelle.
- **Antwort 9:** Alle SQL/152-Stellen (Loesungsentwurf E5, Migrations-/SQL, Etappe 5, Deploy, Manifest) als *ueberholt* markiert bzw. auf „keine Migration“ gesetzt; AK 18. P-A3 nur noch Beleg (H2).
- **Antwort 10:** Out-of-Scope-Zeile und Master-Bedingung als *ueberholt* markiert; Ist-Stand Vorschlagsliste (belegt) ergaenzt; A10, Loesungsentwurf E5 (flache Positionsquelle fuer Liste, Vergabe-Anzeige, Workflow), Verbraucher-Tabelle, Einstellungen `FaHierarchyBeschichtungAktiv`, AK 14/15/15a/16, TS-145, Manifest-Text AKE.
- **Antworten 7, 15:** Haltepunkte H1/H2 in der Etappen-Tabelle; Vorbedingungen in TS-142/TS-145; AK 19 und TS-140 fuer `View` an der AKE.
- **Antwort 14:** Hinweis-Link der Standort-Maske in Einstellungen, TS-140 und Etappe 6.
- **Antwort 16:** Version v1.99.25 nach 1.99.24 in Deploy und Etappe 6.
- **AK 9 praezisiert:** Die Kopfdaten-Anzeige (`_BeschichtungKopf.cshtml:18`) liest `Start_Beschichtung` weiter direkt und ist vom Waechter-Test ausgenommen; sonst schluege der Waechter an einer Stelle an, die kein Listen-Termin ist.

Neue Rueckfragen (nicht blockierend, auch im Frontmatter):

17. **SData-Erweiterung, welche Achse?** Antwort 1 nennt `USER_OSBeschichtet` (Lesetest L11) als spaetere *Termin*-Quelle. Belegt ist: Das Feld steht im Lesetest unter L10 am FA-Kopf (`sage-sdata-benutzerfelder-lesetest.md:1073`), die Schwester-Spec [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]] ordnet es dem Kennzeichen `Beschichtet` zu (`:241`), und `Start_Beschichtung` ist ueber SData noch nicht gefunden (L11, `:1105-1107`). Die Bedeutung des Feldes wird hier nicht aus dem Namen abgeleitet. Gehoert die Erweiterung zur Kennzeichen-Quelle, zur Termin-Quelle oder zu beiden? Betrifft nur den dokumentierten Erweiterungspunkt; gebaut wird nichts.
18. **Sollmenge der AKE-Positionen:** `CachedBomItem.Menge` (`CachedBomItem.cs:31`) ist die Stuecklistenmenge je Artikel, der Kopf haengt an der Artikelnummer (`CachedBomHeader.cs:17`). Uebernehmen oder mit `ProductionOrder.Quantity` (`ProductionOrder.cs:57`) multiplizieren? Empfehlung: multiplizieren (Sollmenge des WA wie an der IDEAL). Der `/goal`-Lauf baut die Empfehlung und vermerkt sie.

Hinweis zur Pruefabfrage P-A4 (Abschnitt bleibt unveraendert): `CachedBomHeaders` hat keine Spalte `ArticleNumber`, sondern `Artikelnummer` (`CachedBomHeader.cs:17`, `CoatingDetectionService.cs:150`, `:155`). Vor dem Ausfuehren `h.ArticleNumber` durch `h.Artikelnummer` ersetzen.

## Offene Rueckfragen

1. Ein Schluessel `Beschichtungslogik` oder zwei Schluessel (Kennzeichen-Quelle, Termin-Quelle)?
   - Empfehlung: zwei Choice-AppSettings mit Default `Automatisch` (Begruendung im Varianten-Vergleich).
2. Soll der Server Kombinationen ablehnen, die zum Auftragsmodell nicht passen (`Artikelkategorie` bei hierarchisch, `FaStruktur` bei flach, `View` an der AKE)?
   - Empfehlung: ja. Ohne Datenbasis waere die Spalte still leer.
3. Sollen Kommissionier-Leitstand und Werkbank-Arbeitsliste an der IDEAL den Termin aus der View zeigen? Heute ist er dort leer.
   - Empfehlung: ja (E4), als sichtbare Verhaltensaenderung im Changelog.
4. Ist `Start_Beschichtung` („Wann Beschichtung starten“) fachlich derselbe Termin wie der berechnete Beschichtungstermin (Abholung zum Beschichter)?
   - Empfehlung: Spaltenname bleibt, Tooltip nennt die Quelle.
   - Bestaetigung durch den IDEAL-Keyuser, Herkunft per P-I4.
5. Kombigeraet (mehrere Koepfe): Termin weiter leer oder fruehester `Start_Beschichtung`?
   - Empfehlung: leer wie heute (TS-71.11 Negativfall a).
6. Termin-Quelle `Berechnet` an der IDEAL: Soll das Basisdatum der Kopf-`FE_Termin` sein, wie heute in der FA-Liste, und so auch in Leitstand und Arbeitsliste gelten?
   - Empfehlung: ja, einheitlich ueber den Resolver.
7. Aktiv-Kennung an der IDEAL: Heute haengt sie an `LackierteilKategorieName`. Mit Quelle `FaStruktur` gilt sie immer als aktiv, Haken erscheinen dann nur noch an beschichteten Zeilen.
   - Empfehlung: ja. Auswirkung vorher per P-I1 pruefen; ist der Wert dort leer, sieht die IDEAL heute an allen Zeilen einen Haken.
8. AKE-Beschichtungsauftrag fachlich, Vorschlag:
   - Positionen = Lackierteile der WA-Stueckliste
   - Masse 0
   - Dienstleister, RAL und Retour manuell
   - Start aus dem berechneten Termin vorbelegt
   - Empfehlung: ja.
9. Sind die AKE-WA-Nummern rein numerisch? `Beschichtungsauftrag.HauptFA` ist int.
   - Empfehlung: P-A3 ausfuehren. Alle numerisch → int behalten, sonst neue Spalte `Auftragsnummer` (SQL/152).
10. Soll die Beschichtungs-Vorschlagsliste auch an der AKE kommen?
    - Empfehlung: nein, sie bleibt hinter dem Master.
11. Soll der Gate-Schluessel `FaHierarchyBeschichtungAktiv` umbenannt werden?
    - Empfehlung: nein. Key behalten, nur Anzeigename, Gruppe und Standort aendern (kein Datenumzug, kein Manifest-Schritt).
12. Soll der Lackierteil-Hook im Sage-Import auch `Sync:CoatingDetectionEnabled` respektieren? Heute haengt er nur an `Sync:BomCacheEnabled`.
    - Empfehlung: ja.
13. Das AKE-Erkennungsfenster filtert `IsDonePicking` und `IsCancelled` nicht. Aendern?
    - Empfehlung: nein in diesem Epic, als Aufgabe festhalten.
14. Sollen die beiden Keys auch in die Standort-Maske `/StandortEinstellungen`? Die Maske kennt heute keinen Choice-Typ.
    - Empfehlung: nein, nur `/Settings`; die Standort-Maske bekommt einen Hinweis-Link.
15. Gibt es an der AKE eine View-Spalte mit Beschichtungsdatum?
    - Empfehlung: P-A5 ausfuehren; bis dahin ist `View` an der AKE nicht waehlbar.
16. Zielversion v1.99.25, die naechste freie nach der reservierten 1.99.24?
    - Empfehlung: ja, alle Etappen unter einer Version.
17. SData-Erweiterung, welche Achse? (Wortlaut im Nachtrag 2026-11-16) → **beantwortet** durch Freigabe-Antwort 17, eingearbeitet in Nachtrag 2.
18. Sollmenge der AKE-Positionen (Wortlaut im Nachtrag 2026-11-16) → **beantwortet** durch Freigabe-Antwort 18, eingearbeitet in Nachtrag 2.
19. **Beschichtet die AKE vor oder nach dem Kommissionierabschluss?** (Nachtrag 2, nicht blockierend fuer den Bau)
    - Komm-erledigte WAs (`IsDonePicking = 1`) liegen nicht im BomCache-Fenster (`BomCacheSyncService.cs:412-424`). Ihr Kopf wird geloescht, wenn kein anderer Fensterauftrag denselben Artikel traegt (`:66`, `:147`, `:617-619`).
    - Die Lackierteil-Erkennung prueft sie weiter (`CoatingDetectionService.cs:103-110`). Ohne Cache-Treffer setzt sie `HasCoatingParts = 0` und `IsCoatingDone = 0` (`:162-170`, `:255-262`).
    - Folge fuer die Vorschlagsliste: Solche WAs fehlen, entweder sichtbar in der Sammelmeldung (Kopf weg, Flag noch 1) oder ganz (Flag schon 0). Das Zuruecksetzen des Flags ist bestehendes Verhalten der Erkennung, nicht neu durch diese Spec.
    - Wird nach dem Kommissionierabschluss beschichtet: Fenster anpassen (eigene Aufgabe wie Rueckfrage 13) oder hinnehmen? P-A7 liefert die Groessenordnung.
    - Keine Empfehlung, das ist eine Ablauffrage der AKE-Fertigung.
20. **Positionen nicht ganzzahlig?** (Nachtrag 2, greift nur bei P-A6 > 0)
    - Der Schluessel Artikelnummer + Position passt ohne Schema nur, wenn die Lackierteil-Positionen ganzzahlig sind (`BeschichtungsauftragPosition.Position` ist `int?`, `:20`).
    - Bei Punkt-Positionen widerspricht die Entscheidung S-4 der Freigabe-Antwort 9 (kein Schema): neue Spalte `PositionText` (SQL-Nummer neu vergeben) oder ein anderer Schluessel?
    - Bis zur Antwort ist E5 Blocker.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Ein Key oder zwei Keys → **Gerald 2026-11-16 15:40 „passt“ (Empfehlung): zwei Choice-AppSettings, Default „Automatisch“. Spätere Termin-Quelle „SData“ (`USER_OSBeschichtet`, Lesetest L11) nur vorsehen, nicht bauen.**
2. Kombinationen serverseitig ablehnen → **Gerald 2026-11-16 15:40 „passt“ (Empfehlung): ja.**
3. IDEAL-Termin auch in Leitstand/Arbeitsliste → **Gerald 2026-11-16 15:40 „passt“ (Empfehlung): ja (E4), im Changelog.**
4. Start_Beschichtung = Beschichtungstermin → **Gerald 2026-11-16 15:38: Start_Beschichtung ist bereits berechnet und wird unverändert übernommen (Termin-Quelle „View“ an der IDEAL, keine WMS-Berechnung darauf). Gerald fragte, ob man stattdessen im WMS „auf 0 setzen“ und die Berechnung weiterverwenden könne – Koordination: nicht nötig, der Rechner geht vom Vorkommissionier-Termin aus (`CoatingDateCalculator.cs`), nicht von Start_Beschichtung; die Quelle „View“ liefert den Wert direkt über denselben Resolver.**
5. Kombigeraet → **Gerald 2026-11-16 15:40 „passt“ (Empfehlung): leer wie heute.**
6. Berechnet an der IDEAL mit Kopf-FE_Termin → **Gerald 2026-11-16 15:40 „passt“ (Empfehlung): ja.**
7. Aktiv-Kennung IDEAL immer aktiv → **Gerald 2026-11-16 15:40 „passt“ (Empfehlung): ja; P-I1 vor E4 erheben (Gerald).**
8. AKE-Beschichtungsauftrag Positionen/Kopf → **Gerald 2026-11-16 15:40 „passt“ (Empfehlung): ja (Lackierteile der WA-Stückliste, Maße 0, Dienstleister/RAL/Retour manuell, Start aus berechnetem Termin).**
9. AKE-WA-Nummern numerisch (P-A3) → **Gerald 2026-11-16: ja (numerisch) → `HauptFA` int bleibt, kein SQL/152. P-A3 bestätigt es vor E5 (Beleg).**
10. Vorschlagsliste an der AKE → **Gerald 2026-11-16: ja, gleich wie an der IDEAL (Liste der zu beschichtenden Teile, `FaHierarchyBeschichtungController`, daraus Beschichtungsauftrag); Kreditor/Beschichter bleibt leer und wird manuell befüllt. Damit entfällt die Master-Bedingung aus dem Lösungsentwurf (Z. 309, AK 15) – Datenbasis an der AKE = flache Struktur/WA-Stückliste wie Rückfrage 8.**
11. Gate-Key umbenennen → **Gerald 2026-11-16 15:40 „passt“ (Empfehlung): nein, nur Anzeigename/Gruppe/Standort.**
12. Sage-Import-Hook an CoatingDetectionEnabled → **Gerald 2026-11-16 15:40 „passt“ (Empfehlung): ja.**
13. Erkennungsfenster-Filter → **Gerald 2026-11-16 15:40 „passt“ (Empfehlung): nicht in diesem Epic, eigene Aufgabe.**
14. Standort-Maske → **Gerald 2026-11-16 15:40 „passt“ (Empfehlung): nein, nur /Settings mit Hinweis-Link.**
15. AKE-View-Datum (P-A5) → **Gerald 2026-11-16 15:40 „passt“ (Empfehlung): bis P-A5 ist „View“ an der AKE nicht wählbar; P-A5 vor E5 erheben (Gerald).**
16. Version v1.99.25 → **Gerald 2026-11-16 15:40 „passt“ (Empfehlung): ja, Reihenfolge 1.99.24 Sicherheit, 1.99.25 Beschichtung; Nummern beim Start prüfen.**
17. SData-Erweiterung, welche Achse → **Gerald 2026-11-16: „passt“ (Koordinations-Empfehlung): `USER_OSBeschichtet` ist Erweiterungspunkt der Kennzeichen-Quelle, NICHT der Termin-Quelle. Nur dokumentieren, nichts bauen (kein spekulativer Code).**
18. Sollmenge der AKE-Positionen → **Gerald 2026-11-16: „passt“ (Koordinations-Empfehlung): Menge über `BomQuantityResolver` (`BomQuantityResolver.cs:15-16`), dieselbe Regel wie die Kommissionierung, keine eigene Formel. Neuer Haltepunkt vor E5: Prüfschritt „ein WA mit Baugruppe: Kommissionierliste vs. Sage/OSEON“ (Mensch vergleicht die Mengen), in Etappen-Tabelle, AK und TS-145.**
19. AKE beschichtet vor oder nach Kommissionierabschluss (Cache-Fenster ohne IsDonePicking) → **Gerald 2026-11-16 17:28: Beschichtet wird zum berechneten Termin, „dieser ist glaube ich vorher“ (vor dem Kommissionierabschluss) → Cache-Fenster ohne IsDonePicking bleibt, keine eigene Aufgabe; P-A7 bestätigt die Größenordnung.**
20. Lackierteil-Positionen nicht ganzzahlig (nur falls P-A6 > 0): Schema-Spalte oder anderer Schlüssel → **Gerald 2026-11-16 17:28: Punkt-Positionen kommen vor („z. B. 1.20 soweit ich weiß“). Koordination: dann neue Spalte `PositionText` (SQL nächste freie, nach 153) als Schlüssel mit Artikelnummer – P-A6 bestätigt; E5 bleibt bis dahin Blocker.**

## Kritische Pruefung (2026-11-16)

Geprueft nach dem Nachtrag (Commit `9f999e5c`) gegen den Bündel-Worktree `feature/2026-08-07-ideal-teile-1-5`. Antworten 1–16 sind vollstaendig und stehen jetzt im Rumpf; SQL/152 und die Master-Sperre sind konsistent als *ueberholt* markiert. Die Befunde betreffen E2/E5.

### BLOCKER

- **B-1 Sollmenge der AKE-Positionen ist offen, geht aber nach aussen (Rueckfrage 18).** Der Beschichtungsauftrag geht an den Beschichter. Eine falsche Menge ist dort ein echter Fehler, keine Anzeigefrage. Zwei Lesarten sind moeglich: `CachedBomItem.Menge` (`IdealAkeWms/Models/CachedBomItem.cs:31`) ist die Menge je Stuecklistenartikel, und die Stueckliste haengt am Artikel, nicht am WA (`CachedBomHeader.cs:17`). Dazu kommt `Baugruppe` (`CachedBomItem.cs:20`, gefuellt in `BomCacheSyncService.cs:292`, `:315`). Ist die Stueckliste mehrstufig abgeflacht, gilt `Menge` womoeglich je Baugruppe und nicht je Endprodukt. Dann waere `Menge × Quantity` fuer Unterpositionen falsch. „Der /goal-Lauf baut die Empfehlung“ reicht hier nicht.
  **Frage an Gerald:** Ist `Menge` in der AKE-Stueckliste je 1 Stueck Endartikel, auch bei Zeilen mit gefuellter `Baugruppe`? Dann gilt `Menge × ProductionOrder.Quantity`. Bitte an einem Beispiel-WA gegen Sage/OSEON gegenpruefen.

### SOLLTE

- **S-1 Haltepunkt H1 sitzt an der falschen Etappe.** Antwort 7 verlangt P-I1 vor der Aenderung der Aktiv-Kennung, weil die IDEAL heute bei leerem `LackierteilKategorieName` an allen Zeilen einen Haken sieht. Diese Aenderung baut aber **E2**: Die Zeilen-Views `_ProductionOrderRow.cshtml:16-20` und `_PickingLeitstandRow.cshtml:82-94` werden dort auf `Aktiv` aus dem Resolver umgestellt (Etappen-Tabelle E2, „Abhaengig von: Freigabe-Antwort 7“). E4 bringt nur den View-Termin in Leitstand und Arbeitsliste. Damit ist die Verhaltensaenderung schon gebaut, bevor H1 greift.
  **Vorschlag:** H1 vor E2 legen. Oder E2 baut mit `Aktiv` = heutige Logik, und der Wechsel auf „FaStruktur = immer aktiv“ zieht nach E4 hinter H1 um. Gerald entscheidet, die Spec nennt danach eine Stelle.
- **S-2 Stille Luecke: WA mit `HasCoatingParts = true`, aber ohne BomCache.** Die flache Quelle liest die Positionen aus `CachedBomHeaders/Items`. Der BomCache loescht Koepfe ausserhalb des aktuellen Fensters (`BomCacheSyncService.cs:600-621`, `DELETE … WHERE Artikelnummer NOT IN (…)`). Die Erkennung setzt Flags ausserhalb des Fensters nicht zurueck (Spec, Migrations-Abschnitt). Folge: Ein Lackier-WA kann gekennzeichnet sein und trotzdem ohne jeden Hinweis aus der Vorschlagsliste fallen. Das widerspricht der Hausregel „melden statt still behandeln“.
  **Vorschlag:** In der Vorschlagsliste ein Banner oder eine Sammelmeldung „N WA mit Beschichtungskennzeichen ohne Stuecklisten-Cache“ plus Logger-Warnung. Dazu ein AK (15b) und ein Negativfall in TS-145.
- **S-3 „Offene WAs“ der flachen Quelle sind nicht definiert** (Loesungsentwurf E5, „offene mit `HasCoatingParts = true`“). Unklar bleibt, ob `IsDone`, `IsCancelled` und `IsDonePicking` ausgeschlossen werden. `ProductionOrder.cs:101` sagt: `IsCancelled` verhaelt sich in allen Offen-Queries wie `IsDone`. Das Erkennungsfenster filtert beides nicht (Rueckfrage 13).
  **Vorschlag:** In E5 und AK 15 festschreiben: `IsDone = 0 AND IsCancelled = 0`. `IsDonePicking` bleibt bewusst drin (lackiert wird nach dem Kommissionieren). Dazu ein Negativfall „stornierter WA erscheint nicht“.
- **S-4 Der Positionsschluessel `Position = CachedBomItem.SortOrder` ist bei Stuecklistenaenderungen nicht stabil.** Aendert sich der Inhalt, loescht der BomCache alle Items eines Kopfes und legt sie neu an (`BomCacheSyncService.cs:518-521`, `SortOrder` neu vergeben `:550`, `:582`). Nach einer Stuecklistenaenderung zeigt die Vergabe-Anzeige (`BeschichtungsauftragVergabeAnzeige.cs:43`, Schluessel `PositionsSchluessel(VaterFA, SubFA, Position)`) deshalb womoeglich die falsche Zeile als vergeben.
  **Vorschlag:** Den Grenzfall dokumentieren und einen Negativfall in TS-145 aufnehmen: Stueckliste aendert sich nach der Anlage, dann die Vergabe-Anzeige pruefen. Mindestens sollte eine Warnung erscheinen, wenn Artnr der Position und Artnr des Snapshots nicht zusammenpassen. Eine stabilere Ersatzloesung, z.B. ein Schluessel ueber `Ressourcenummer`, gibt es nur mit Schema-Aenderung. Das bleibt deshalb bewusst ausserhalb dieser Version.
- **S-5 Die Pruefabfrage `Pruefung/BeschichtungQuelleKombination` schweigt, wenn die ServiceSettings-Zeile fehlt.** Der `CROSS JOIN` auf `ServiceSettings WHERE Key = 'ProduktionsauftragHierarchisch'` liefert 0 Zeilen, wenn der Key nicht angelegt ist. Dann meldet die Pruefung nie einen Befund, auch wenn `FaStruktur` an der AKE steht.
  **Vorschlag:** `LEFT JOIN` bzw. `COALESCE(s.m, 'false')`, passend zum Default des Masters, und im Manifest-Test pruefen.
- **S-6 P-A4 hat einen falschen Spaltennamen** (`h.ArticleNumber` statt `h.Artikelnummer`, Hinweis im Nachtrag). Laut Auftrag bleibt der Abschnitt unveraendert. Gerald muss die Abfrage aber vor dem Ausfuehren korrigieren, sonst bricht sie ab.

### HINWEIS

- **H-1 Rueckfrage 17 (SData-Achse) blockiert nicht**, nichts davon wird gebaut. Antwort 1 und die Lesetest-Notiz widersprechen sich aber. Das sollte in der SData-Schwester-Spec [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]] geklaert werden, nicht hier.
- **H-2 Die AKE-Annahme `HauptFA = int(OrderNumber)` ist eindeutig.** Im flachen Modus gilt `SubOrderNumber == OrderNumber` (`ProductionOrder.cs:15-17`, `:30`), mit Unique-Index auf `SubOrderNumber` (`ApplicationDbContext.cs:493`). Eine WA-Nummer entspricht also genau einem Auftrag, eine Kollision mehrerer Auftraege unter einem `HauptFA` gibt es nicht. P-A3 bleibt der Beleg fuer „numerisch“.
- **H-3 Die Vorschlagsliste laedt alle Positionen in den Speicher** (`BeschichtungsauftragService.cs:98-99`, `GetAllAsync`). An der AKE ist die Menge durch das BomCache-Fenster begrenzt (`Sync:BomCacheMaxOrders`, Default 200), unkritisch.
- **H-4 Groesse:** rund 30 Dateien in 3 Schichten (Katalog/Resolver, Web, Dienst) plus Doku. Als Epic mit sechs Etappen ist das schaffbar. Das Buendel ist aber schon sehr gross (bis v1.99.24), das Merge-Risiko waechst mit jeder Etappe.
- **H-5 Status/Ablage:** `status: Freigegeben` und `freigabe_*` hat der Mensch gesetzt, die Datei liegt aber noch in `specs/entwurf/`. Das Verschieben nach `specs/freigegeben/` bleibt die Geste des Menschen.

**NACHBESSERUNG NOETIG: Sollmenge der AKE-Positionen klaeren (B-1) und H1 vor die Aktiv-Kennungs-Aenderung in E2 legen (S-1).**

## Nachtrag 2 2026-11-16: Review-Befunde eingearbeitet

Grundlage sind Geralds Entscheidungen zur Kritischen Pruefung und zu den Rueckfragen 17 und 18. Die Abschnitte „Kritische Pruefung (2026-11-16)“, „Nachtrag 2026-11-16“ und die Freigabe-Antworten 1-16 sind unveraendert (Audit-Trail). `status` und `freigabe_*` sind unveraendert. Belege stammen aus dem Worktree `feature/2026-08-07-ideal-teile-1-5`.

| Befund / Entscheidung | eingearbeitet in |
|---|---|
| **B-1 / Rueckfrage 18** Menge ueber `BomQuantityResolver` | Ist-Stand „Mengenregel der Kommissionierung“ (belegt: `BomQuantityResolver.cs:15-16`; Aufrufer `PickingController.cs:355`, `:553`, `PickingOrderWorkflow.cs:84`, `ReadOnlyBomBuilder.cs:108`; AKE-Weg `Program.cs:100-107` → `BomRepository.cs:28-30` → `BomCacheRepository.cs:47`, `MengeIstAuftragsmenge = false`); Soll A12; Loesungsentwurf E5 (`Sollmenge = Resolve(...)`, Abbildung wie `BomCacheRepository.cs:32-47`, alter Text *ueberholt*); neuer Haltepunkt **H3** (Frontmatter `etappen`, Etappen-Tabelle, Loesungsentwurf „Mengen-Pruefschritt“); AK 20, AK 21; automatisierte Tests; TS-145 „Mengen-Pruefschritt“; Pruefabfragen „H3“; Freigabe-Antwort 18 |
| **S-1** H1 vor E2 | Frontmatter `etappen`; Etappen-Tabelle (H1 vor E2, E2 haengt an H1, E4 ueber E2); Etappen-Text zu den Blockern (*ueberholt* markiert): ohne P-I1 sind E2 und E4 Blocker |
| **S-2** Sammelmeldung | Soll A15; Verbraucher-Tabelle (Vorschlagsliste); Loesungsentwurf E5 „Sammelmeldung“ (Text, max. 20 Nummern, `LogWarning`, ViewModel statt TempData, nur bei Quelle `Artikelkategorie`); AK 15b; automatisierte Tests; TS-145 Negativfall; Pruefabfrage P-A7; `affected_code` (Controller, ViewModels) |
| **S-3 / SOLLTE** „offen“ | Soll A13 (`IsDone = 0 AND IsCancelled = 0`, `ProductionOrder.cs:83-84`, `:100-103`; `IsDonePicking` `ProductionOrderPickingStatus.cs:47` filtert nicht); Loesungsentwurf E5 (alter Text *praezisiert*); AK 15c; TS-145 Negativfall storniert; Etappe 5 |
| **S-4 / SOLLTE** Schluessel Artikelnummer + Position | Ist-Stand (`PickingOrderRestmengen.cs:16-17`, `BeschichtungsauftragPosition.cs:20`, `:23`, `IBeschichtungsauftragRepository.cs:10-14`, `BeschichtungsauftragRepository.cs:40-42`, `BeschichtungsauftraegeController.cs:322-333`); Soll A14; Loesungsentwurf E5 (Projektion `Position = int(CachedBomItem.Position)` statt `SortOrder`, *ueberholt* markiert; optionale `Artnr`-Komponente nur in der flachen Quelle); Migrations-Abschnitt; AK 22; TS-145 Grenzfall; Pruefabfrage P-A6; neue Rueckfrage 20; `affected_code` |
| **S-5 / SOLLTE** Manifest-Pruefung | Deploy: Abfrage mit `COALESCE(..., 'false')` (Default `ServiceSettingDefinitions.cs:222`, `IHierarchicalModeReader.cs:21`), alte Fassung als *ueberholt* stehen gelassen; Verhalten im Update-Skript ausgeschrieben (`Update-IdealAkeWms.ps1:419-434`, `nachlauf.psd1:12-17`): bei 0 still „OK“, bei > 0 Hinweiszeile plus Warnung, keine Reparatur, kein Abbruch; AK 23 |
| **S-6 / SOLLTE** P-A4 | Pruefabfrage P-A4 neu: Join `h.Artikelnummer`. Zusaetzlich ersetzt `LEFT JOIN` die `EXISTS`-Unterabfrage in `SUM`, die SQL Server ablehnt. `IsCancelled = 0` und `IsDonePicking` als Gruppe ergaenzt |
| **H-1 / Rueckfrage 17** SData-Achse | Out-of-Scope (alter Text *ueberholt*), Soll A1/A2, Resolver-Abschnitt „Erweiterungspunkt SData“ (nur Doku, kein Code), Etappe 1, Freigabe-Antwort 17 |
| H-2 bis H-5 | keine Aenderung noetig (H-2 bestaetigt, H-3 unkritisch, H-4 Groesse bekannt, H-5 Ablage bleibt Geste des Menschen) |

Rueckfragen 17 und 18 sind beantwortet (Frontmatter `beantwortete_fragen`, Abschnitt „Offene Rueckfragen“, Freigabe-Antworten 17/18).

**Neu beim Belegen gefunden** (als Rueckfragen, nicht selbst entschieden):

- **Rueckfrage 19:** Das BomCache-Fenster schliesst komm-erledigte WAs aus (`BomCacheSyncService.cs:412-424`), das Erkennungsfenster nicht (`CoatingDetectionService.cs:103-110`). Komm-erledigte Lackier-WAs verlieren dadurch Cache-Kopf und ggf. Kennzeichen. Die Entscheidung „IsDonePicking bleibt drin“ (S-3) greift also nur, solange der Kopf noch im Cache steht. Den Bau aendert das nicht; die Sammelmeldung macht den Fall „Kopf weg, Flag noch 1“ sichtbar.
- **Rueckfrage 20:** Der Schluessel Artikelnummer + Position passt nur bei ganzzahligen Positionen ohne Schema (`BeschichtungsauftragPosition.Position` `int?`), `CachedBomItem.Position` ist string. P-A6 klaert das vor E5. Bei Treffern kollidieren S-4 und Freigabe-Antwort 9.

## QA-Nachweis Teilumfang E1/E3/E6 (qa-agent, 2026-11-16)

**Ergebnis: QA Teilumfang E1/E3/E6: gruen.** Status bleibt `InUmsetzung`.

Begruendung der Status-Entscheidung:

- Das Epic ist nur teilweise gebaut. E2, E4 und E5 sind blockiert (H1, H2, H3 fehlen).
- Offen sind die AK 5-9, 13-16 und 20-22.
- `Testbereit` oeffnet Schranke 2 (Test und Merge) fuer die ganze Spec. Das wuerde einen Abschluss anzeigen, den es nicht gibt.
- Der gebaute Teil ist trotzdem in sich lauffaehig: Beide Keys stehen auf `Automatisch`, das Verhalten ist unveraendert, die Termin-Quelle ist als „noch ohne Wirkung“ dokumentiert. Er darf mit dem Buendel auf DEV und Test.
- Vor einem Test- oder Prod-Deploy muss Befund Q-1 erledigt oder ausdruecklich hingenommen sein.

### Geprueft

- Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`.
- Commit-Bereich `8bfd8c73..4a635534` mit drei Commits:
  - `c61b7a52` E1
  - `318abf1f` E3
  - `4a635534` E6, Teil
- HEAD `4a635534731efd967a1430b281b38f1566f0ee08`, `AppVersion` 1.99.25 in Web und Dienst.

### 1. Build und Tests

- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (alle Bestand: CS8601, NU190x, CS8321).
- `dotnet test`:

| Testprojekt | bestanden | uebersprungen | Fehler | gesamt |
|---|---|---|---|---|
| `IDEALAKEWMSService.Tests` | 631 | 0 | 0 | 631 |
| `IdealAkeWms.Tests` | 2913 | 1 | 0 | 2914 |

### 2. Code-Review (eigenstaendig, in diesem Lauf)

**Geprueft:**

- der ganze Diff `8bfd8c73..4a635534` (32 Dateien)
- der Code rund um die Aenderungen: `SettingsController.SaveSettings` (`Gilt`, Reihenfolge der Pruefung), `ServiceSettings.GetBoolSafeAsync`, `SyncWorker.cs:242-265` (Lackierteile) und `:356-368` (Materialisierung), DI in `IDEALAKEWMSService/Program.cs:49`, `:75`, `_SettingField.cshtml` (`AbhaengigVon`-Anzeige), `ArticleSageDetailsWriter.cs:28` (Audit-Konvention)

**Bekannte Fehlerklassen:**

- Kein `<script>`-Einbetten, kein `hidden` auf `d-*`, keine parallelen Arrays.
- Kein stiller Fallback auf einen Wert: ungueltig wirkt wie `Automatisch`, mit Warnung im Protokoll, siehe aber Q-4.
- Die A8-Regel prueft der Server (`SettingsController.cs:129-137`) mit derselben Funktion wie der Dienst (`ResolveQuellen`).
- Die zweite Umsetzung derselben Regel ist die Manifest-SQL (`nachlauf.psd1:170-173`). Sie weicht bei Fremdwerten ab, siehe Q-3.

| Nr | Schwere | Befund | Fundstelle | Behandlung |
|---|---|---|---|---|
| Q-1 | **Mittel** | **Sage-Import-Hook: Die Verhaltensaenderung hat keine Deploy-Pruefung.** Bisher lief die Lackierteil-Erkennung im Hook immer, sobald `Sync:BomCacheEnabled` an war. Jetzt braucht sie zusaetzlich `Sync:CoatingDetectionEnabled` (Default `false`, `ServiceSettingDefinitions.cs:288`). Das ist gewollt (Freigabe-Antwort 12). Steht der Schalter an einem flachen System aber aus, bekommen neue WAs **still** kein Lack-Flag mehr: kein Haken im Leitstand, kein Termin. Gemeldet wird das nur als Serilog-Info (`SageImportService.cs:174-178`), nicht im Aktivitaets-Protokoll. `fallstricke.md` §39 verlangt „Vor dem Deploy je System pruefen“, das Manifest erzwingt es aber nicht; der Info-Eintrag `v1.99.25-Beschichtungsquelle-Info` nennt es nur als Satz. Beleg Testsystem AKE: Schalter **AN** (`2026-10-20-ake-testsystem-bestandsaufnahme.md:114`). Prod-Stand unbekannt (AKESQL20 nicht abgefragt). Nach „melden statt still behandeln“ ist kein Laufzeit-Banner noetig, denn die Regel ist bewusst. Die Aenderung muss aber beim Deploy **sichtbar gemeldet** werden. | `SageImportService.cs:224-228`, `scripts/deploy/nachlauf.psd1` (fehlt) | **offen, vor Test-/Prod-Deploy.** Vorschlag: Manifest-Eintrag `Pruefung/LackierteilErkennungImportHook`, Version 1.99.25, Firma beide, Phase Haltepunkt, `anzeigen`. Er zaehlt: Master aus (`COALESCE(...,'false')` wie AK 23), `Sync:BomCacheEnabled = true`, `Sync:CoatingDetectionEnabled <> true` (fehlende Zeile = false). Text: „Lackierteil-Erkennung nach dem Sage-Import laeuft seit v1.99.25 nur mit Sync:CoatingDetectionEnabled; ohne den Schalter bekommen neue WAs kein Lack-Kennzeichen. Unter /ServiceSettings einschalten oder bewusst hinnehmen.“ |
| Q-2 | Niedrig | **Falscher Abhaengigkeits-Text** an `LackierteilKategorieName`: „Wirkt nur, wenn „Quelle Beschichtet-Kennzeichen“ an ist.“ Der Text ist fuer Bool-Schalter gebaut, die Quelle ist aber ein Choice. Auf DEV AKE und IDEAL sichtbar. | `Views/Shared/_SettingField.cshtml:178`, Ausloeser `AppSettingDefinitions.cs:329-330` | offen. Entweder den Text fuer Choice-Eltern anpassen (z. B. „Wirkt nur bei Quelle Beschichtet-Kennzeichen = Artikelkategorie“) oder `abhaengig` weglassen; die Beschreibung sagt es schon. |
| Q-3 | Niedrig | **Manifest-Pruefung erkennt Fremdwerte nicht.** Ein per SQL gesetztes `SData` ergibt 0 (DEV AKE gemessen); Resolver und `/Settings` melden den Wert. Damit zwei Fassungen derselben Regel. | `nachlauf.psd1:170-173` | offen. Zaehlen, wenn `[Value] NOT IN ('Automatisch','Artikelkategorie','FaStruktur')` bzw. `('Automatisch','Berechnet','View')`, oder bewusst hinnehmen (`/Settings` markiert den Fremdwert schon beim Laden). |
| Q-4 | Niedrig | `GetBeschichtungQuellenAsync` schluckt jeden DB-Fehler ohne Log und wirkt dann wie `Automatisch`. Das Muster entspricht `GetBoolSafeAsync`, und der Fallback ist das heutige Verhalten. Es fehlt aber jede Spur. | `IDEALAKEWMSService/Common/IHierarchicalModeReader.cs:48` | offen, hinnehmbar. Optional ein `LogWarning`. |
| Q-5 | Niedrig | Uneinheitliche Audit-Form: `ModifiedByWindows = Environment.UserName`, auf DEV gemessen `svc_idealakewms`. Andere Dienst-Schreiber verwenden `DOMAIN\User` (`ArticleSageDetailsWriter.cs:28`), die Lackierteil-Erkennung `SYSTEM_USER`. | `FaMaterializationSyncService.cs:226` | offen, hinnehmbar (ADR 0003 erfuellt, nur die Form weicht ab). |
| Q-6 | Info | `AppVersion.Date` steht in beiden Dateien noch auf `2026-11-13`, der Changelog auf 16.11.2026. | `IdealAkeWms/AppVersion.cs:6`, `IDEALAKEWMSService/AppVersion.cs:6` | offen, trivial. |
| Q-7 | Info | Die Kennzeichen-Quelle hat unter A8 keinen erreichbaren Effekt: Jeder Wert ungleich `Automatisch` ist entweder gleich dem Automatisch-Ergebnis oder wird abgelehnt. Den Zweig „Lack-Flag nicht geschrieben“ (`FaMaterializationSyncService.cs:213-215`) erreicht man nur, wenn das Lesen des Masters fehlschlaegt. Das ist so entworfen (Spec „Varianten Schluessel“). | – | hingenommen (Spec-Entscheidung). |
| Q-8 | Info | Folgende E6-Teile fehlen noch, weil sie zu E2/E5 gehoeren oder Brain sind: Standort-Maske Hinweis-Link (Freigabe-Antwort 14, in TS-140 als zurueckgestellt vermerkt), `RoleOverview`, Brain-Changelog v1.99.25 (`secondbrain/changelog/`, nicht vorhanden), Eintrag in `feature-map.md`. Vorhanden sind ADR 0028, `fallstricke` §39 und `codebase/services.md`. Testindex: TS-140/144 **fehlten**; QA hat die Zeile in `testszenarien-index.md` ergaenzt. | – | Testindex erledigt (QA); Rest offen bis E6-Abschluss. |
| Q-9 | Info | `publish/deploy.zip` (07:31, vor E1) liegt veraltet im Publish-Ordner. | `<worktree>/publish/deploy.zip` | nicht geloescht; der Mensch entscheidet. Massgeblich sind `publish/deploy/` und `publish-info.txt`. |

**Einschaetzung zum Implementierer-Befund** (Hook ohne `CoatingDetectionEnabled`): bestaetigt, Schwere **Mittel**, siehe Q-1. Kein Blocker fuer DEV. Fuer Test und Prod gehoert er vor den Deploy, als Manifest-Pruefung oder als ausdrueckliche Hinnahme durch Gerald.

### 3. Nachweisart je AK (gebauter Umfang)

| AK | Nachweis | Art |
|---|---|---|
| 1 | `AppSettingDefinitionsTests.BeschichtungQuellen_AlsChoice_OhneSData_AK1_AK19`, Seed-Test; DEV: beide Keys `Automatisch` an AKE und IDEAL, Auswahl, Texte und Warnhinweis sichtbar | automatisiert + DEV |
| 2 | `SettingsControllerBeschichtungQuelleTests.Save_WertAusserhalbDerListe_Abgelehnt_AK2`; DEV 140.9 | automatisiert + DEV (echtes Binding) |
| 3 | `Save_UnpassendeKombination_NichtsGespeichert`; DEV 140.4, 140.5, 140.6 | automatisiert + DEV |
| 4 | `BeschichtungResolverTests.ResolveQuellen_UngueltigOderUnpassend_*`; DEV 144.8: Warnung im Aktivitaets-Protokoll. Die Web-Seite liest erst mit E2. | automatisiert + DEV (Dienst) |
| 10 | `HierarchicalModeGateTests`, `FaMaterializationCoatingWriteTests.RunAsync_QuelleArtikelkategorie_*` (Stub). Das „uebersprungen“ der Lackierteil-Erkennung ist auf DEV nicht beobachtbar (`Sync:CoatingDetectionEnabled` darf nicht an). | automatisiert; **Manual-UAT** fuer den echten Lauf |
| 11 | `SageImportHookGateTests` (interne Methode). Den echten Import-Hook gegen Sage gibt es auf DEV nicht. | automatisiert; **Manual-UAT** |
| 12 | `ProductionOrderPickingStatusRepositoryTests`, `FaMaterializationCoatingWriteTests.RunAsync_SetztAuditFelder_ADR0003`; DEV 144.3 (echter Dienstlauf, SQL Server) | automatisiert + DEV |
| 17 | alle Tests gruen; Matrix in `BeschichtungResolverTests` | automatisiert |
| 18 | Diff ohne Migration und ohne SQL-Skript | Diff |
| 19 | `AppSettingDefinitionsTests`; DEV 140.2 (Optionen genau Automatisch/Berechnet/View), 140.5 | automatisiert + DEV |
| 23 | `NachlaufManifestDriftTests`; Pruefungs-SQL auf beiden DEV-DBs ausgefuehrt (gueltig 0, unpassend 1) | automatisiert + DEV |
| 5-9, 13-16, 20-22 | nicht gebaut (E2/E4/E5 blockiert) | – |

### 4. Geaenderte Bestandstests

| Test | Einordnung |
|---|---|
| `BereinigungenV179Tests`, `OperationConfigsControllerTests`, `SettingsControllerKatalogTests`, `SettingsControllerQrErstesFeldTests` | nur Typ (neuer Ctor-Parameter `IServiceSettingRepository`) |
| `FaMaterializationSyncServiceTests`, `StubHierarchicalModeReader` | nur Typ (neue Abhaengigkeit, neue Interface-Methode) |
| `ProductionOrderPickingStatusRepositoryTests` | Signatur plus gewollte Zusatzpruefung Audit (AK 12) |
| `AppSettingDefinitionsTests` (Anzahl 58 → 60, Seed 36 → 38) | gewollt (AK 1) |
| `HierarchicalModeGateTests` (Counts-Key `uebersprungen_hierarchisch` → `uebersprungen_quelle_fastruktur`) | gewollt (AK 10). Der alte Key gilt in `BomCacheSyncService.cs:46` und `FaWorkStepDetectionService.cs:43` weiter, dort korrekt. |

Kein Befund „sonstiges“.

### 5. Checkliste CLAUDE.md und ADR 0020

**CLAUDE.md:**

- Migration: keine (korrekt, AK 18). FreshInstall: nicht betroffen (Seed beim Start). AgentJobs: nicht betroffen.
- Audit-Felder: geschlossen (AK 12).
- Version 1.99.25 in beiden `AppVersion.cs`. Anwender-Changelog vorhanden, Brain-Changelog fehlt (Q-8).
- `docs/TESTSZENARIEN.md`: TS-140 und TS-144 vorhanden, TS-141 bis 143 Platzhalter. Testindex von QA ergaenzt.
- README und `docs/betrieb/04` gepflegt.

**ADR 0020** (beide neuen Choice-Keys):

- Auswahlliste: ja
- Server-Validierung (Wert und Kombination): ja
- Anzeigename, Wirkung, Verwendung, Default, Standort: ja
- Warnhinweis an der Kennzeichen-Quelle: ja; die Termin-Quelle wirkt nur auf die Anzeige.
- Geaenderte Beschreibungen (`BeschichtungTage`, `BeschichtungAbholtage`, `LackierteilKategorieName`, `Sync:CoatingDetectionEnabled`): vorhanden. Q-2 betrifft den Abhaengigkeits-Text.

**Manifest:** `Pruefung/BeschichtungQuelleKombination` und `v1.99.25-Beschichtungsquelle-Info` sind vorhanden. `v1.99.25-Beschichtungsauftrag-AKE` gehoert zu E5 und fehlt zu Recht. Es fehlt die Pruefung zu Q-1.

### 6. DEV-Deploy und Testlauf

**Standort:** `beide` laut Frontmatter.

**Deploy:**

- Erhoehte Shell, `D:\Dev\scripts\dev-deploy.ps1 -Quelle <worktree>`, je Standort Web und Dienst.
- AKE: „HTTP 200, Fusszeile v1.99.25“, „Fertig: AKE v1.99.25 auf http://localhost:8081/“, Exit 0.
- IDEAL: „HTTP 200, Fusszeile v1.99.25“, „Fertig: IDEAL v1.99.25 auf http://localhost:8082/“, Exit 0.
- Dienste `AKE_WMS_DEV_Service` und `IDEAL_WMS_DEV_Service` laufen. Die Service-Logs vom 16.11. enthalten keine ERR/FTL-Eintraege.
- DEV-Absicherung nach dem Deploy (SQL): alle `Sync:*Enabled` (BomCache, CoatingDetection, ProductionOrders, HierarchicalFa) `false`, `SageLagerbuchungAktiv` `false`, `ErrorNotification:Enabled` `false`.
- Seed: Beide Keys stehen nach dem Start an beiden DEV-DBs auf `Automatisch`. Master AKE `false`, IDEAL `true`.

**TS-140** (Browser ueber claude-in-chrome, Daten ueber `sqlcmd -S . -E -C`):

| Nr | gemacht | Ergebnis | Status |
|---|---|---|---|
| 140.1 | `/Settings` AKE und IDEAL, Gruppe „Termine und Beschichtung“ | zwei Auswahlfelder auf „Automatisch (nach Auftragsmodell)“; Beschreibung, Verwendung, Standard und Standort „beide“ sichtbar; Warnhinweis „Umschalten berechnet das Kennzeichen …“ an der Kennzeichen-Quelle | OK |
| 140.2 | Optionen der Termin-Quelle gelesen (IDEAL) | genau `Automatisch`, `Berechnet`, `View`; kein `SData` | OK |
| 140.3 | Texte `BeschichtungTage`, `BeschichtungAbholtage`, `LackierteilKategorieName` | Zusaetze „Wirkt nur bei Termin-Quelle „Berechnet““ bzw. „… Kennzeichen-Quelle „Artikelkategorie““ vorhanden; der Abhaengigkeits-Hinweis lautet aber „… an ist.“ | OK mit Befund Q-2 |
| 140.4 | AKE: Kennzeichen `FaStruktur` speichern | gelbe Meldung „Nicht gespeichert: „Quelle Beschichtet-Kennzeichen“: „FaStruktur“ passt nicht zum Auftragsmodell (flach, …) – wirkt wie „Automatisch“ (Artikelkategorie) …“; DB unveraendert | OK |
| 140.5 | AKE: Termin `View` speichern | abgelehnt, Meldung „… „View“ passt nicht zum Auftragsmodell (flach …) … (Berechnet)“ | OK |
| 140.6 | IDEAL: Kennzeichen `Artikelkategorie` speichern | abgelehnt, „(hierarchisch, ProduktionsauftragHierarchisch an) … (FaStruktur)“ | OK |
| 140.7 | AKE `Artikelkategorie` + `Berechnet`; IDEAL `FaStruktur` + `View` speichern | beide gespeichert (gruene Meldung, DB geprueft); danach per SQL auf `Automatisch` zurueckgesetzt. Die IDEAL-Variante `FaStruktur` + `Berechnet` wurde nicht einzeln gespeichert; die Resolver-Matrix deckt sie ab. | OK |
| 140.8 | AKE per SQL `FaStruktur`, dann `/Settings` unveraendert speichern | abgelehnt mit derselben Meldung (Bestandswert sperrt Speichern). Die Dienst-Warnung der Lackierteil-Erkennung ist auf DEV nicht ausloesbar (Lauf nur mit `Sync:CoatingDetectionEnabled`). | OK (Web) / NICHT MOEGLICH (Dienst-Teil) |
| 140.9 | AKE per SQL `SData` | `/Settings` zeigt beim Laden „„SData“ (ungueltig – bitte waehlen)“ und eine Meldung „… ist nicht erlaubt“ | OK |
| 140.10 | Pruefungs-SQL des Manifests direkt auf den DEV-DBs (statt Update-Skript-Trockenlauf) | AKE gueltig 0, `FaStruktur` 1, `SData` **0**; IDEAL `FaStruktur`+`View` 0, `Artikelkategorie` 1 | OK mit Befund Q-3; Lauf ueber das Update-Skript NICHT MOEGLICH (bleibt fuer den Menschen) |

**TS-144:**

| Nr | gemacht | Ergebnis | Status |
|---|---|---|---|
| 144.1 | – | Die Lackierteil-Erkennung laeuft nur mit `Sync:CoatingDetectionEnabled` (`SyncWorker.cs:242`). Auf DEV gesperrt. | NICHT MOEGLICH |
| 144.2 | Dienstlaeufe „FA-Materialisierung“ IDEAL auf v1.99.25 (16:40, 16:55; Gate ist nur der Master) | `lackflag_gesetzt=12`, Laeufe erfolgreich | OK |
| 144.3 | Testdaten: Status-Zeile `ProductionOrderId=286` per SQL auf `HasCoatingParts=0`, naechsten Lauf abgewartet | Zeile 286: `HasCoatingParts=1`, `ModifiedAt 2026-11-16 16:55:01`, `ModifiedBy=FaMaterialization`, `ModifiedByWindows=svc_idealakewms`; vorher NULL/NULL | OK (Form siehe Q-5) |
| 144.4 | – | braucht `Sync:CoatingDetectionEnabled=true` | NICHT MOEGLICH |
| 144.5 | – | braucht Sage-Import mit neuem WA plus `Sync:BomCacheEnabled`/`ProductionOrdersEnabled` | NICHT MOEGLICH |
| 144.6 | – | wie 144.5 plus `CoatingDetectionEnabled` | NICHT MOEGLICH |
| 144.7 | – | die Materialisierung laeuft nur bei Master an; bei Master an ist die effektive Quelle immer FaStruktur (Q-7) | NICHT MOEGLICH (nicht herstellbar) |
| 144.8 | IDEAL per SQL `BeschichtungKennzeichenQuelle=Artikelkategorie`, Materialisierungslauf 16:55 | SyncLogs 14298580 Warning „„Quelle Beschichtet-Kennzeichen“: „Artikelkategorie“ passt nicht zum Auftragsmodell (hierarchisch …) – wirkt wie „Automatisch“ (FaStruktur).“; Flag trotzdem geschrieben (Zeile 286). Wert danach auf `Automatisch` zurueckgesetzt. Teil „Lackierteile“ NICHT MOEGLICH (Sync-Gate). | OK (Materialisierung) / NICHT MOEGLICH (Lackierteile) |

**DEV-Endstand:** Beide Keys stehen an AKE und IDEAL wieder auf `Automatisch`. Die Testzeile 286 ist durch den Dienst wieder korrekt (`HasCoatingParts=1`).

### 7. Publish

- Ziel: `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\`
  - `IDEALAKEWMSWeb` und `IDEALAKEWMSWebService`: vorher geleert, `dotnet publish -c Release` fehlerfrei
  - `SQL\`, `deploy\`
- `publish-info.txt`:
  - Version: 1.99.25
  - Commit: 4a635534731efd967a1430b281b38f1566f0ee08
  - Branch: feature/2026-08-07-ideal-teile-1-5
  - Spec: 2026-11-16-beschichtungslogik-je-standort-spec
  - Erstellt: 2026-11-16 16:55
- Hinweis: Im Publish-Root liegt eine alte `deploy.zip` (Q-9).

### Deploy (aus dem echten Diff, Teilumfang)

**Komponenten:**

- `deploy.web`: ja (`IdealAkeWms/`: Katalog, Resolver, SettingsController, Repository, Hilfe, Changelog)
- `deploy.service`: ja (`IDEALAKEWMSService/`: Reader, Lackierteil-Erkennung, Materialisierung, Import-Hook)
- `deploy.migration`: nein (keine Datei unter `*/Migrations/`, kein SQL-Skript)
- Web und Dienst gemeinsam deployen (gemeinsamer Katalog und Repository-Signatur).

**Paket und Reihenfolge:**

- Paket: `<worktree>\publish`, v1.99.25. Ablauf beim Menschen: Publish → Testsystem → Test → Merge.
- Reihenfolge: Update-Skript wie ueblich. Es ist keine Migration und kein Einmal-Skript dabei.
- Die Pruefung `BeschichtungQuelleKombination` zeigt am Haltepunkt beim Erst-Deploy OK; die Keys entstehen erst beim Web-Start.
- **Vor dem Deploy an Test und Prod:** Q-1 klaeren (`Sync:CoatingDetectionEnabled` je flachem System).

Nach dem Merge nur dann neu aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen main-Aenderungen zusammengefuehrt hat:

- `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

### Manuelle Checkliste (offen fuer den Menschen)

1. Q-1 entscheiden: Manifest-Pruefung zum Import-Hook ergaenzen lassen oder hinnehmen. Je flachem System (AKE Test, AKE Prod) `Sync:CoatingDetectionEnabled` pruefen, wo `Sync:BomCacheEnabled` an ist.
2. Q-2 (Abhaengigkeits-Text) und Q-3 (Fremdwerte im Manifest) entscheiden.
3. TS-140.8 Dienst-Teil und TS-144.1/144.4/144.8 (Lackierteile) am Testsystem AKE mit `Sync:CoatingDetectionEnabled=true`: Protokoll-Warnung bzw. unveraenderte Zaehler.
4. TS-144.5/144.6 am Testsystem AKE: Sage-Import mit neuem WA, Schalter aus bzw. an; Dienst-Log „Lackierteil-Erkennung uebersprungen“ bzw. Lack-Flag gesetzt.
5. TS-140.10 ueber das Update-Skript (Trockenlauf) mit Fremdwert: „Abweichung 1“, nach Korrektur „OK“.
6. Testsystem IDEAL: TS-144.2/144.3 nach dem Deploy (Audit `FaMaterialization` an geaenderten Zeilen).
7. Danach H1, H2 und H3 erheben, damit E2, E4 und E5 gebaut werden koennen. Erst nach dem vollstaendigen Epic folgt der QA-Lauf fuer `Testbereit`.

### Nachtrag Lauf 2026-11-16: QA-Befunde behoben (`3f247f11`)

- **Q-1:** neue Manifest-Prüfung `Pruefung/LackierteilErkennungImportHook` (Nachher, anzeigen), TS-144.9.
- **Q-2:** Hinweis bei Abhängigkeit von einer Auswahl lautet „Wirkt nur bei passender Auswahl in „…““ (`Settings/Index.cshtml`, `_SettingField.cshtml`); `AbhaengigVon` bleibt laut Einstellungen-Tabelle.
- **Q-3:** `Pruefung/BeschichtungQuelleKombination` zählt auch Fremdwerte (TS-140.10 ergänzt).
- **Q-4:** Lesefehler der Quellen wird als Warnung geloggt (`IHierarchicalModeReader.cs`).
- **Q-5:** `ModifiedByWindows` = `IDEALAKEWMSService` wie die anderen Schreiber der Materialisierung.
- **Q-6:** `AppVersion.Date` 2026-11-16.
- Q-7..Q-9 Info, nicht geändert. Build 0 Fehler; Tests Web 2913/0/1, Dienst 631/0/0. Nicht erneut auf DEV deployt (Publish am Ende des Laufs).

## QA-Nachweis Teilumfang E2/E4 (qa-agent, 2026-11-17)

**Ergebnis: QA Teilumfang E2/E4: gruen.** Status bleibt `InUmsetzung`.

Begruendung der Status-Entscheidung:

- E5 ist weiter blockiert (H2: P-A5/P-A3/P-A6 der AKE offen; H3 offen; Rueckfragen 19/20).
- Damit bleiben AK 14, 15, 15a-c, 16 und 20-22 offen. `Testbereit` wuerde Schranke 2 fuer das ganze Epic oeffnen.
- Der gebaute Teil E1-E4 ist in sich lauffaehig. Er darf mit dem Buendel auf DEV und Test, braucht vor dem Publish aber den Versions-Bump (R-2).

### Geprueft

- Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`.
- Commit-Bereich `d3a00101..b4972700` (22 Dateien, +465/-93):
  - `2336dced` E2 Web-Verbraucher
  - `b4972700` E4 IDEAL-Termin Leitstand/Arbeitsliste
- HEAD `b4972700b3b742979af08ef38b1ac829069a4a37`. `AppVersion` 1.99.31 in Web und Dienst, unveraendert (R-2).
- Vorbedingung H1: P-I1 steht im Abschnitt „Pruefabfragen“. PROD IDEAL hat den Master **aus**, TEST und DEV IDEAL haben ihn an.

### 1. Build und Tests

- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (alle Bestand: CS8601, NU190x, CS8321).
- `dotnet test`:

| Testprojekt | bestanden | uebersprungen | Fehler | gesamt |
|---|---|---|---|---|
| `IDEALAKEWMSService.Tests` | 682 | 0 | 0 | 682 |
| `IdealAkeWms.Tests` | 3024 | 1 | 0 | 3025 |

### 2. Code-Review (eigenstaendig, in diesem Lauf)

**Geprueft:** der ganze Diff `d3a00101..b4972700` und der Code darum:

- `ProductionOrdersController.MapItem`:
  - `effectiveProductionDate` = Kopf-`FE_Termin`
  - Kombigeraet-Zweig `:282-313`
  - `groupHead` nur bei genau einem Kopf
- `PickingLeitstandController.Index`:
  - hierarchischer Zweig mit Memory-Filtern `:200-250`
  - `coating-date` nach der Termin-Berechnung `:709`, `:751`
- `FaWorklistController.Index`:
  - Gate `FaCompletionAktiv`
  - Bestand vor Filter und Pagination
  - `coating-date` `:430`
- `CoatingDateCalculator.Compute`: null-sicher bei fehlendem VK-Termin
- `CachedSettingRepository`: 2 Minuten Cache
- `FaHierarchyOrderInfoRepository.GetByHauptFaKeysAsync`: eine Abfrage
- Manifest-Filter `Update-IdealAkeWms.ps1:268` (`Test-Neuer`)
- Zeilen-Views, Spaltenkopf-Tooltips, Hilfe, Changelog, README, `docs/betrieb/04`

**Hausregeln und bekannte Fehlerklassen:**

- **Ausgabe im HTML:** Kein `<script>`-Einbetten; der Tooltip steht Razor-kodiert im `title`-Attribut. Kein `hidden` auf `d-*`, keine parallelen Arrays, keine neue Client-Validierung.
- **Kein stiller Fallback:**
  - Ein ungueltiger Quellwert wirkt wie Automatisch und wird je Request geloggt (`BeschichtungResolver.cs:76`).
  - Kombigeraet und leerer `Start_Beschichtung` ergeben null, keinen Ersatztermin.
- **Eine Regel an einer Stelle:**
  - Termin, Aktiv-Kennung und Tooltip kommen nur aus `BeschichtungResolver`.
  - `LadenAsync` ist die einzige Web-Stelle, die `LackierteilKategorieName`, `BeschichtungTage` und `BeschichtungAbholtage` liest. Der Waechter-Test sichert das ab.
  - Leitstand und Arbeitsliste teilen sich den Kopf-Lookup `EindeutigeKoepfeAsync`.
- **Gleiche Berechnet-Basis in drei Listen:** Die FA-Liste rechnet ueber `effectiveProductionDate`, Leitstand und Arbeitsliste ueber `head.FE_Termin`. Der VK-Termin ist ueberall derselbe; auf DEV gemessen identisch (143.2).
- **Read/Edit-Split:** keine neue Action, kein neuer Filter, Rollen unveraendert.
- **Listen-Muster:**
  - Pagination, Spaltenfilter und Server-Mode unveraendert, kein neues Take/Cap.
  - Der Kopf-Lookup laeuft je Seite. Bei Memory-Filtern laeuft er ueber die ungepaginierte Gruppenmenge, wie FA-Liste und Matchcode-Lookup.
- **WCAG:** keine Farb- oder Layout-Aenderung. Rot-Markierung und Haken haengen weiter an `showBeschicht`.

**Implementierer-Notizen, bewertet:**

- *Kein AppVersion-Bump:* fuer den Code hingenommen, als Prozess-Befund R-2 festgehalten.
- *Manifest-Info-Text geaendert statt neuem Eintrag:* Befund R-1.
- *Leitstand und Arbeitsliste aendern nur den Beschichtungstermin:* korrekt.
  - E4 verlangt nur den Termin aus dem Resolver. A5 legt nur die Basis des Berechnet-Termins fest.
  - Die K-/VK-Spalten bleiben im hierarchischen Leitstand leer (DB-`ProductionDate` NULL), wie vorher (143.5).
- *`BeschichtungTerminQuelleWaechterTests`:* traegt AK 9.
  - Er sucht in `Controllers/*.cs` und `Views/*.cshtml`.
  - Ausnahmen: `_BeschichtungKopf`, `_BeschichtungKopfDruck` und `Help/`. Sie zeigen Sage-Kopfdaten bzw. Hilfetext. `_BeschichtungKopfDruck` fehlt in der Ausnahme-Liste von AK 9, ist aber dieselbe Kopfdaten-Anzeige.
  - Ein zweiter Test prueft, dass die Ausnahmen noch existieren.
  - `Services/` wird nicht gescannt; AK 9 verlangt das nicht.

| Nr | Schwere | Befund | Fundstelle | Behandlung |
|---|---|---|---|---|
| R-1 | Niedrig | **Der neue Verhaltenshinweis haengt am alten Manifest-Eintrag `v1.99.25-Beschichtungsquelle-Info`.** Der Text ist neu: IDEAL hierarchisch zeigt `Start_Beschichtung` in Leitstand und Arbeitsliste, Haken nur an gekennzeichneten Auftraegen. Das Update-Skript zeigt den Eintrag aber nur bei `Version > installiert` (`Test-Neuer`). Ein System, das schon auf 1.99.25 bis 1.99.31 steht, sieht den Hinweis beim Update auf 1.99.32 nicht. Es gibt keinen Beleg, dass ein Test- oder Prod-System schon ≥ 1.99.25 hat. Heute ist das daher ohne Wirkung. | `scripts/deploy/nachlauf.psd1:401-403`, `scripts/deploy/Update-IdealAkeWms.ps1:268` | offen. Beim Bump auf 1.99.32 einen eigenen Info-Eintrag anlegen (`v1.99.32-Beschichtungstermin-Listen`, Firma IDEAL bzw. beide, Nachher, anzeigen) oder bewusst hinnehmen, solange kein Zielsystem ≥ 1.99.25 hat. |
| R-2 | Niedrig (Prozess) | **E2/E4 laufen ohne Versions-Bump mit der Fusszeile 1.99.31**, also mit demselben Wert wie der Stand `d3a00101`. Die Fusszeile aus `dev-deploy.ps1` belegt den neuen Stand daher nicht; Beleg sind der Zeitstempel der DLL und das gemessene Verhalten. Laut Laufnotiz [[2026-11-17-goal-lauf-beschichtung-bde-mapping]] kommt der Bump auf 1.99.32 mit BDE. Ein Publish mit 1.99.31 und E2/E4 waere ein zweites Paket mit derselben Version wie der Publish `d3a00101`; das Update-Skript koennte die beiden nicht unterscheiden. Der Anwender-Changelog fuehrt die Aenderung als „Nachtrag (naechste Version nach v1.99.31)“ im Block v1.99.25. | `IdealAkeWms/AppVersion.cs:5`, `IDEALAKEWMSService/AppVersion.cs:5`, `Views/Help/Changelog.cshtml:151-155` | offen, **Bedingung vor dem Publish:** AppVersion 1.99.32 in beiden Dateien. Im Changelog 1.99.32 die IDEAL-Listenaenderung nennen oder auf den Nachtrag verweisen. |
| R-3 | Info | `LadenAsync` loggt die Quellen-Warnung bei **jedem** Seitenaufruf von FA-Liste, Leitstand und Arbeitsliste, solange ein Fremdwert in der DB steht. Das ist gewollt (ADR 0020 Punkt 2), kann das Web-Log aber fuellen. | `IdealAkeWms/Services/Beschichtung/BeschichtungResolver.cs:76` | hingenommen; `/Settings` und die Manifest-Pruefung melden den Wert ohnehin. |
| R-4 | Info | Die Spalte „Beschicht.“ der Arbeitsliste hat keinen Quellen-Tooltip. Die Spec verlangt ihn nur fuer FA-Liste und Leitstand (Verbraucher-Tabelle, TS-142.3). Die Hilfe sagt allgemein: „der Tooltip der Spalte nennt die Quelle“. | `IdealAkeWms/Views/FaWorklist/Index.cshtml:111` | hingenommen (Spec-Umfang); optional nachziehen. |
| R-5 | Info | `deploy-info.txt` ist in `D:\Dev\AKE\Web` (1.99.16) und `D:\Dev\IDEAL\Web` (1.98.1) veraltet. `dev-deploy.ps1` schreibt die Datei nicht. Betrifft nur die DEV-Umgebung, nicht den Zweig. | `D:\Dev\<Standort>\Web\deploy-info.txt` | gemeldet, nicht geaendert. |

Keine Befunde der Schwere Mittel oder Hoch. Kein Anwendungscode geaendert.

### 3. Nachweisart je AK (Teilumfang E2/E4)

| AK | Nachweis | Art |
|---|---|---|
| 5 | `ProductionOrdersControllerTests.Index_Flat_OhneKategorie_AktivFalse_TerminFuerAlle_AK5`, Bestandstests Leitstand/Arbeitsliste; DEV AKE 141.1-141.5: Termin = Handrechnung der alten Formel in allen drei Listen | automatisiert + DEV |
| 6 | `Index_Hierarchical_Automatisch_ViewTooltip_UndAktiv_AK6_AK13`, `BeschichtungResolverTests` (View datumsgleich, Tage 0/10, Kombigeraet, leer); DEV 142.1, 142.3-142.6 | automatisiert + DEV |
| 7 | `PickingLeitstandControllerTests.Index_Hierarchical_*_AK7`, `FaWorklistControllerTests.Index_Hierarchical_*_AK7`; DEV 142.1, 143.2 (alle drei Listen gleich) | automatisiert + DEV |
| 8 | `Index_Hierarchical_TerminQuelleBerechnet_AusKopfFeTermin_TooltipBerechnet_AK8`, dazu die Tests fuer Leitstand und Arbeitsliste; DEV 143.1-143.4 | automatisiert + DEV |
| 9 | `BeschichtungTerminQuelleWaechterTests` (Quelltext-Waechter) | automatisiert |
| 13 | `Index_Hierarchical_Automatisch_ViewTooltip_UndAktiv_AK6_AK13`, `BeschichtungResolverTests` (Aktiv je Quelle); DEV 142.2 | automatisiert + DEV |
| 4 (Web-Teil) | `BeschichtungResolverTests.LadenAsync_FremdwertWirktWieAutomatisch_UndLoggtWarnung_AK4` (Mock-Logger) | automatisiert; auf DEV nicht erneut erzeugt |
| 17 | alle Tests gruen | automatisiert |
| 18 | Diff ohne Migration, ohne SQL-Skript | Diff |
| 14-16, 20-22 | nicht gebaut (E5 blockiert) | – |

Die echte Razor-Ausgabe (Tooltip, Aktiv-Kennung, leere Zelle) ist mit InMemory und Mock nicht beweisbar. Sie ist hier ueber das DEV-HTML belegt, abgerufen mit Windows-Anmeldung.

### 4. Geaenderte Bestandstests

| Test | Einordnung |
|---|---|
| `FaWorklistControllerTests.Build` | nur Typ (neuer Ctor-Parameter `IFaHierarchyOrderInfoRepository`) |
| `PickingLeitstandControllerTests` (Feld, Ctor-Aufruf, Default-Setup `GetByHauptFaKeysAsync` → leer) | nur Typ bzw. Grund-Setup fuer die neue Abhaengigkeit; keine Erwartung geaendert |
| `BeschichtungResolverTests`, `ProductionOrdersControllerTests` | nur neue Tests, keine Bestands-Erwartung geaendert |

Kein Befund „sonstiges“.

### 5. Checkliste CLAUDE.md und ADR 0020

- **Migration, FreshInstall, AgentJobs:** nicht betroffen (AK 18).
- **Audit:** kein neuer Schreibweg.
- **Version:** **kein Bump** (R-2). Der Anwender-Changelog hat einen Nachtrag im Block v1.99.25. Den Brain-Changelog fuehrt die Koordination.
- **`docs/TESTSZENARIEN.md`:** TS-141 bis 143 ausgeschrieben, mit automatisierten Belegen; Schlussvermerk aktualisiert. Den Testindex hat QA nachgezogen.
- **README, `docs/betrieb/04-konfiguration.md`, Hilfe:** nachgezogen. Der Hinweis „noch ohne Wirkung“ ist weg; DEV `/Settings` zeigt ihn an beiden Standorten nicht mehr.
- **ADR 0020:** keine neue Einstellung.
  - Geaendert sind nur die Verwendungstexte der beiden Quellen-Keys; auf DEV sichtbar.
  - Auswahlliste und Server-Validierung unveraendert (Regression 140.R).
- **Manifest:**
  - Text des vorhandenen Info-Eintrags angepasst (R-1).
  - Kein SQL und kein Hand-Schritt neu. `NachlaufManifestDriftTests` gruen.

### 6. DEV-Deploy und Testlauf

**Standort:** `beide` laut Frontmatter.

**Deploy:**

- Ablauf: erhoehte Shell, `D:\Dev\scripts\dev-deploy.ps1 -Standort <AKE|IDEAL> -Quelle <worktree>`, Web und Dienst, nach gruenem Build und gruenen Tests.
- AKE: „HTTP 200, Fusszeile v1.99.31“, „Fertig: AKE v1.99.31 auf http://localhost:8081/“, Exit 0.
- IDEAL: „HTTP 200, Fusszeile v1.99.31“, „Fertig: IDEAL v1.99.31 auf http://localhost:8082/“, Exit 0.
- **Fusszeile und Stand:**
  - Fusszeile = `AppVersion` des Zweigs (1.99.31). Weil die Version gleich geblieben ist (R-2), belegt die Fusszeile allein den Stand nicht.
  - Beleg: `IdealAkeWms.dll` in `D:\Dev\AKE\Web` und `D:\Dev\IDEAL\Web` traegt den Zeitstempel 2026-11-17 08:38:08. Das ist nach dem Commit `b4972700` (08:35:49).
  - Das neue Verhalten ist gemessen (Tooltip-Text, IDEAL-Leitstand mit `Start_Beschichtung`).
- **DEV-Absicherung nach dem Deploy (SQL):** an beiden DEV-DBs `false`:
  - `Sync:BomCacheEnabled`, `Sync:CoatingDetectionEnabled`, `Sync:HierarchicalFaEnabled`, `Sync:ProductionOrdersEnabled`
  - `SageLagerbuchungAktiv`, `ErrorNotification:Enabled`
  - Master: AKE `false`, IDEAL `true`.
- **Logs:** Dienst-Logs `service-20261117.log` AKE und IDEAL ohne ERR/FTL. Aktivitaets-Protokoll ab 08:30 ohne Warnung und ohne Fehler.

**Methode:**

- Seiten mit Windows-Anmeldung abgerufen (`Invoke-WebRequest -UseDefaultCredentials`). Zellen und `title` aus dem gerenderten HTML gelesen.
- `/Settings` im Browser bedient (claude-in-chrome, eigener Tab).
- Daten per `sqlcmd -S . -E -C`.
- Erwartungswerte von Hand gerechnet, mit den DEV-Feiertagen 26.10. und 01.11.2026.

**TS-141 (DEV AKE, Master aus, Kategorie „Lackierteile“):** Lackier-FA `2645001` (Id 3047, Fert. 03.11.2026), FA ohne Lack `2614027`.

| Nr | gemacht | Ergebnis | Status |
|---|---|---|---|
| 141.1 | FA-Liste, Leitstand, Arbeitsliste (AG 1) fuer 2645001 | ueberall **08.10.2026**. Handrechnung der alten Formel: K 28.10., VK 27.10., minus 10 AT = 12.10., letzter Abholtag Do 08.10. Einen Wert von vor dem Update gab es nicht; die Rechnung der unveraenderten Formel ersetzt ihn. | OK |
| 141.2 | Tooltip „Beschicht.“ in FA-Liste und Leitstand | „Berechnet: Kommissioniertermin - 10 Arbeitstage, auf den letzten Abholtag“ | OK |
| 141.3 | `colf_coating-date=08.10.2026` in FA-Liste und Leitstand | 2 von 12 Eintraegen (2645001, 2630007), beide 08.10.2026 | OK |
| 141.4 | FA ohne Lack 2614027 in der FA-Liste | Zelle „Beschicht.“ leer | OK |
| 141.5 | `LackierteilKategorieName` per SQL leer, Cache-Ablauf abgewartet | 2614027 zeigt in FA-Liste und Leitstand 03.11.2026. FA ohne Lack 2614167 zeigt in der Arbeitsliste 13.10.2026; die Handrechnung stimmt. Danach auf „Lackierteile“ zurueckgesetzt (SQL belegt). | OK |

**TS-142 (DEV IDEAL, Master an, Automatisch):**

- HauptFA `1060414` mit einem Kopf (Id 14448, FE 14.07.2026, `Start_Beschichtung` 17.07.2026).
- Sub-FA mit Kennzeichen `1067190`, ohne Kennzeichen `1067191`.
- Die Arbeitsliste ist auf DEV IDEAL gesperrt (`FaCompletionAktiv=false`, keine `FaWorkSteps`). Fuer den Test wurde sie voruebergehend eingeschaltet, mit zwei `FaWorkSteps`-Testzeilen (`Source='QA-Test'`, AG 1, PO 286 und 275).

| Nr | gemacht | Ergebnis | Status |
|---|---|---|---|
| 142.1 | 1067190 in FA-Liste, Leitstand, Arbeitsliste | ueberall **17.07.2026** = `Start_Beschichtung`. Vor E4 zeigte der hierarchische Leitstand keinen Termin. | OK |
| 142.2 | 1067191 (ohne Kennzeichen, Kategorie leer) | kein Termin, kein Haken in allen drei Listen | OK |
| 142.3 | Tooltip in FA-Liste und Leitstand | „Aus der Sage-View, Feld Start_Beschichtung, unverändert übernommen“ | OK |
| 142.4 | `BeschichtungTage` in `/Settings` von 10 auf 3 gespeichert | alle drei Listen weiter 17.07.2026; danach in `/Settings` auf 10 zurueck | OK |
| 142.5 | Testkopf `QA-KOMBI` fuer 1060414 per SQL angelegt (zweiter Kopf) | kein Termin in FA-Liste, Leitstand und Arbeitsliste, unter View und unter Berechnet. Testkopf danach geloescht. | OK |
| 142.6 | `Start_Beschichtung` von Kopf 14448 per SQL NULL (`FE_Termin` bleibt) | kein Termin und kein Ersatztermin in allen drei Listen; danach auf 17.07.2026 zurueck | OK |
| 142.7 | Leitstand `colf_coating-date=17.07.2026` bzw. `18.07.2026` | Gruppe 1060414 mit den 12 gekennzeichneten Sub-FA bzw. leer | OK |

**TS-143 (DEV IDEAL, Berechnet):**

| Nr | gemacht | Ergebnis | Status |
|---|---|---|---|
| 143.1 | `/Settings` „Quelle Beschichtungstermin“ = Berechnet gespeichert | „Einstellungen gespeichert.“; DB `Berechnet` | OK |
| 143.2 | 1067190 in drei Listen (`BeschichtungTage` zu diesem Zeitpunkt 3) | ueberall **02.07.2026**, nicht 17.07. Handrechnung: FE 14.07., K 08.07., VK 07.07., minus 3 AT = Do 02.07. | OK |
| 143.3 | Tooltip | „Berechnet: Kommissioniertermin - 3 Arbeitstage …“ bzw. „- 10 …“ | OK |
| 143.4 | `BeschichtungTage` in `/Settings` von 3 auf 10 | ueberall **23.06.2026** (Handrechnung: minus 10 AT = Di 23.06.) | OK |
| 143.5 | Leitstand K/VK von 1067190 | leer wie unter View, also unveraendert (DB-Datum NULL) | OK |
| 143.6 | zurueck auf Automatisch | ueberall wieder 17.07.2026, Tooltip View | OK |

**Regression TS-140 und TS-144:**

| Nr | gemacht | Ergebnis | Status |
|---|---|---|---|
| 140.R | `/Settings` AKE und IDEAL gelesen | Kennzeichen-Quelle genau Automatisch/Artikelkategorie/FaStruktur, Termin-Quelle genau Automatisch/Berechnet/View, beide auf Automatisch. Neue Verwendungstexte sichtbar, „noch ohne Wirkung“ nicht mehr vorhanden. | OK |
| 144.2R | Aktivitaets-Protokoll IDEAL nach dem Deploy | FaMaterialization 08:38:50: „Run erfolgreich beendet … lackflag_gesetzt=12“ | OK |
| 144.3R | PO 286 per SQL auf `HasCoatingParts=0`, naechsten Materialisierungslauf abgewartet | Lauf 08:53:50 (SyncLogs 14298803, `lackflag_gesetzt=12`) setzt PO 286 wieder auf `HasCoatingParts=1`. Audit: `ModifiedAt` 2026-11-17 08:53:50, `ModifiedBy=FaMaterialization`, `ModifiedByWindows=IDEALAKEWMSService` (vorher `svc_idealakewms`). DEV-Endzustand damit wiederhergestellt. | OK |

**DEV-Endstand (SQL belegt):**

- AKE: `LackierteilKategorieName=Lackierteile`, beide Quellen Automatisch.
- IDEAL: `BeschichtungTage=10`, `BeschichtungTerminQuelle=Automatisch`, `BeschichtungKennzeichenQuelle=Automatisch`, `FaCompletionAktiv=false`, `LackierteilKategorieName` leer.
- `FaWorkSteps` wieder 0 Zeilen. Kopf 14448 hat wieder `Start_Beschichtung` 17.07.2026, der Testkopf `QA-KOMBI` ist geloescht.
- Nichts an Sage, keine Mail, kein Test- oder Prod-System.

### 7. Publish

Nicht in diesem QA-Lauf. Laut Auftrag folgt der Publish am Ende des `/goal`-Laufs, nach dem Versions-Bump auf 1.99.32 (Bedingung R-2). Den Publish-Ordner `<worktree>\publish` hat QA nicht angefasst; dort liegt weiter der Stand `d3a00101` (1.99.31).

### Deploy (aus dem echten Diff, Teilumfang E2/E4)

- `deploy.web`: ja (`IdealAkeWms/`: drei Controller, Resolver, ViewModels, Views, Hilfe, Changelog, Katalogtexte).
- `deploy.service`:
  - im Diff `d3a00101..b4972700` **nein**, keine Datei unter `IDEALAKEWMSService/`
  - fuer das Epic weiter ja (E3); Web und Dienst gehen ohnehin gemeinsam mit dem Buendel
- `deploy.migration`: nein.
- Manifest: nur der Text von `v1.99.25-Beschichtungsquelle-Info` geaendert (R-1). Keine SQL-Datei, kein neuer Hand-Schritt.
- Wirkung je System:
  - AKE und PROD IDEAL (Master aus laut P-I1): Verhalten unveraendert.
  - TEST IDEAL (Master an): Leitstand und Arbeitsliste zeigen neu `Start_Beschichtung`. Haken und Termin stehen in allen drei Listen nur an Auftraegen mit Kennzeichen.
- Ablauf beim Menschen: Publish aus dem Worktree (nach dem Bump) → Testsystem → Test → Merge.
- Nach dem Merge nur dann neu aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen main-Aenderungen zusammengefuehrt hat:
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  - `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

### Manuelle Checkliste (offen fuer den Menschen, Teilumfang E2/E4)

1. Vor dem Publish: AppVersion 1.99.32 in Web und Dienst pruefen (R-2). Ueber R-1 entscheiden: eigener Info-Eintrag 1.99.32 oder Hinnahme.
2. Testsystem IDEAL (Master an) nach dem Deploy: TS-142.1 bis 142.3 mit einem echten HauptFA gegen `FaHierarchyOrderInfos`. Falls es ein echtes Kombigeraet gibt, auch 142.5.
3. Testsystem IDEAL: TS-142.2 bestaetigen. An Sub-FA ohne Kennzeichen verschwindet der Haken; das ist die gewollte Aenderung aus Freigabe-Antwort 7. Bitte mit den Anwendern abstimmen.
4. Testsystem AKE: TS-141.1 mit einem vor dem Update notierten Lackier-FA. DEV hatte keinen Vorher-Wert, dort ist nur die Handrechnung belegt.
5. Optional TS-143 am Testsystem IDEAL, danach zurueck auf Automatisch.
6. Weiter offen aus dem Teilumfang E1/E3/E6: Punkte 3 bis 6 der Checkliste vom 2026-11-16 (Lackierteil-Erkennung und Import-Hook mit echtem Dienst, Update-Skript-Pruefungen).
7. H2 (P-A5/P-A3/P-A6 der AKE) und H3 erheben, damit E5 gebaut werden kann. Erst danach folgt der QA-Lauf fuer `Testbereit`.

## Nachtrag 3 2026-11-17: P-A6 und Freigabe-Antworten 19/20 eingearbeitet (`PositionText`)

Grundlage ist Geralds Auftrag vom 2026-11-17: „P-A6 an AKE-Test (`IDEAL_AKE_WMS_TESTIDEAL`, 2026-11-17) zeigt 3.812 von 4.444 Lackierteil-Stuecklistenzeilen mit nicht ganzzahliger, teils mehrstufiger Position (5.2, 51.5.2), keine Doppelungen. Gemaess Freigabe-Antwort 20 neue Spalte PositionText als Positionsschluessel (mit Artnr) einfuehren …“, dazu Freigabe-Antwort 19. Abschnitte „Offene Rueckfragen“, „Freigabe-Antworten“, „Kritische Pruefung“, die Nachtraege 1/2, die vorhandenen Pruefabfragen und beide QA-Nachweise sind unveraendert (Audit-Trail); ergaenzt wurde nur ein datierter Stand-Block in „Pruefabfragen“. `status` und `freigabe_*` unveraendert. Belege aus dem Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, HEAD mit `AppVersion` 1.99.33.

| Entscheidung / Befund | eingearbeitet in |
|---|---|
| **Freigabe-Antwort 20 / P-A6** Spalte `PositionText` | Frontmatter (`open_questions` leer, 19/20 unter `beantwortete_fragen`, `etappen` H2/E5, `deploy.migration: true`, `affected_code`); Out-of-Scope (*ueberholt*); Ist-Stand „Stuecklisten-Position als Text“ (Schreiber, Breite, keine int-Parser, vorhandene Sortierung, alle Nutzstellen, Index, In-Memory-Paging); Soll A14 praezisiert, neu A16; Loesungsentwurf E5 (Projektion, Schluessel, Abschnitt „Positionstext“, Schema-Satz *ueberholt*); Migrations-/SQL-Abschnitt; Audit; AK 18 *ueberholt*, AK 22 praezisiert, neu AK 24-28; automatisierte Tests; TS-145; Etappen-Text und -Tabelle; Deploy und Manifest |
| **Freigabe-Antwort 19** Cache-Fenster bleibt | Loesungsentwurf E5 (Offen-Definition, *entschieden*), Etappe 6 (fallstricke-Eintrag „hingenommen“), Frontmatter |
| **H2** | erledigt (P-A3 0, P-A5 keine Datumsspalte, P-A6 durch Antwort 20 aufgeloest); Pruefabfragen-Stand, Etappen-Tabelle |
| **H3** | weiter offen, einziger Blocker fuer E5 (AK 21) |
| **R-1** (Versionsfilter des Update-Skripts) | Deploy: neue E5-Eintraege tragen die Auslieferungsversion `<v>`; `v1.99.25-Beschichtungsauftrag-AKE` → `v<v>-Beschichtungsauftrag-AKE` |

**Nummern (beim Start des Dev-Laufs erneut pruefen):**

- SQL: **157** (`157_AddBeschichtungsauftragPositionText.sql`). Hoechste im Worktree ist 156 (Wareneingang, v1.99.33), 151 ist fuer die Sicherheit reserviert.
- Kollision gemeldet, nicht bearbeitet: Die BF-Spec [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]] plant noch SQL/156 und TS-155/156; alle drei sind vergeben. Wer zuerst baut, nimmt 157.
- Testszenario: **TS-145** (von dieser Spec reserviert, in `docs/TESTSZENARIEN.md` noch frei; dort folgen TS-144 und TS-146). Kein neues TS.
- Version: naechste nach 1.99.33, voraussichtlich **1.99.34**.
- ADR: **keiner**. `PositionText` ist eine Spalte nach dem vorhandenen Muster `PickingOrderLine.BomPosition` (Text, 50) und die Sortierung nutzt den vorhandenen `NaturalPositionComparer`; es entsteht keine neue Grundsatzentscheidung. Das Dauerwissen („Positionen sind Text, nie als int parsen“) geht nach `fallstricke.md` (Etappe 6).

**Bei der Belegpruefung gefunden (keine Widersprueche zur Entscheidung, nur Praezisierungen):**

- `int(CachedBomItem.Position)` existierte nur im Spec-Text. Im Code parst keine Stelle eine Stuecklisten-Position als `int`, und E5 ist nicht gebaut (`PositionsSchluessel` hat drei Komponenten, `IBeschichtungsauftragRepository.cs:10`).
- Die Ist-Stand-Zeile „`CachedBomItem.Position` ist string(50) (… `ApplicationDbContext.cs:1104`)“ verweist auf die Zeile davor; die Konfiguration steht in `:1106`. Inhalt stimmt.
- `SortOrder` ist auch ohne Stuecklistenwechsel keine natuerliche Reihenfolge: Der Dienst liest die Sage-View mit `ORDER BY Artikelnummer, Position` und vergibt `SortOrder` in dieser Lesereihenfolge (`BomCacheSyncService.cs:296`, `:552-565`). Bei einer Text-Position steht `51.5.10` damit vor `51.5.2`. Das stuetzt A16 (Sortierung ueber `NaturalPositionComparer`, nicht ueber `SortOrder`).
- `PickingOrderRestmengen.Schluessel` ist fuer `articleNumber = null` nicht sicher (`PickingOrderRestmengen.cs:17`, `articleNumber.Trim()`); `BeschichtungsauftragPosition.Artnr` ist nullable. Die Spec schreibt deshalb `artnr ?? ""` vor.
- Die bisherige Textform zerlegt den Schluessel an `|` und erwartet genau drei Teile (`BeschichtungsauftraegeController.cs:329-330`). Ein freier Text als Teil haette einen Trenner im Inhalt nicht ueberlebt; deshalb sind Teil 4 und 5 kodiert (Standardbibliothek `Uri.EscapeDataString`), statt eine Pruefabfrage nach `|` in Sage-Daten zu verlangen.

**Offene Rueckfragen nach Nachtrag 3:** keine. Offen ist nur der Haltepunkt **H3** (Mengenvergleich durch Gerald). P-A8 und P-A9 sind Belege bzw. TS-Vorbereitung und blockieren nichts; zeigt P-A8 an einem System mit Master aus Positionen, wird daraus eine neue Rueckfrage (Umgang mit Altzeilen ohne `PositionText`).
