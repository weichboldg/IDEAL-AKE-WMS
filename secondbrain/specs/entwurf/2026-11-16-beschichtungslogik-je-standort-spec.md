---
type: spec
title: Beschichtungslogik je Standort - Quelle fuer Kennzeichen und Termin, ein Resolver, Beschichtungsauftrag auch an der AKE
slug: 2026-11-16-beschichtungslogik-je-standort-spec
status: Freigegeben
created: 2026-11-16
updated: 2026-11-16
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
  - IdealAkeWms/Controllers/FaHierarchyBeschichtungController.cs (nur Doku-Kommentar; Gate unveraendert, keine Master-Bedingung)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Summiert.cshtml
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml
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
open_questions:
  - "17. (Nachtrag 2026-11-16, nicht blockierend) Antwort 1 nennt USER_OSBeschichtet als spaetere Termin-Quelle SData. Der Lesetest fuehrt das Feld am FA-Kopf (L10, sage-sdata-benutzerfelder-lesetest.md:1073), die Schwester-Spec fa-struktur-ideal-ueber-sdata ordnet es dem Kennzeichen Beschichtet zu (:241), Start_Beschichtung ist ueber SData noch nicht gefunden (L11 :1105-1107). Gehoert die SData-Erweiterung zur Kennzeichen-Quelle, zur Termin-Quelle oder zu beiden? Betrifft nur den dokumentierten Erweiterungspunkt, nichts wird gebaut."
  - "18. (Nachtrag 2026-11-16, betrifft E5) Sollmenge der AKE-Vorschlagsliste/-Positionen: CachedBomItem.Menge ist die Stuecklistenmenge je Artikel (Kopf je Artikelnummer, CachedBomHeader.cs:17) - so uebernehmen oder mit ProductionOrder.Quantity (ProductionOrder.cs:57) multiplizieren? Empfehlung: multiplizieren (Sollmenge des WA wie an der IDEAL); der /goal-Lauf baut die Empfehlung und vermerkt sie."
beantwortete_fragen:
  - "1 -> BEANTWORTET: zwei Choice-AppSettings, Default Automatisch; Termin-Quelle SData nur als Erweiterungspunkt vorgesehen, nicht gebaut (Freigabe-Antwort 1)"
  - "2 -> BEANTWORTET: Server lehnt unpassende Kombinationen ab (Freigabe-Antwort 2)"
  - "3 -> BEANTWORTET: IDEAL-Termin auch in Leitstand/Arbeitsliste, E4, im Changelog (Freigabe-Antwort 3)"
  - "4 -> BEANTWORTET: Start_Beschichtung ist in Sage bereits berechnet und wird unveraendert uebernommen, keine WMS-Berechnung darauf; Tooltip nennt die Quelle (Freigabe-Antwort 4)"
  - "5 -> BEANTWORTET: Kombigeraet leer wie heute (Freigabe-Antwort 5)"
  - "6 -> BEANTWORTET: Berechnet an der IDEAL mit Kopf-FE_Termin (Freigabe-Antwort 6)"
  - "7 -> BEANTWORTET: Aktiv-Kennung IDEAL immer aktiv; P-I1 vor E4 durch Gerald (Freigabe-Antwort 7)"
  - "8 -> BEANTWORTET: AKE-Positionen = Lackierteile der WA-Stueckliste, Masse 0, Dienstleister/RAL/Retour manuell, Start aus berechnetem Termin (Freigabe-Antwort 8)"
  - "9 -> BEANTWORTET: AKE-WA-Nummern numerisch, HauptFA bleibt int, kein SQL/152; P-A3 nur als Beleg vor E5 (Freigabe-Antwort 9)"
  - "10 -> BEANTWORTET: Vorschlagsliste auch an der AKE, gleich wie IDEAL, Datenbasis WA-Stueckliste, Kreditor/Beschichter leer und manuell (Freigabe-Antwort 10)"
  - "11 -> BEANTWORTET: Key bleibt, nur Anzeigename/Gruppe/Standort (Freigabe-Antwort 11)"
  - "12 -> BEANTWORTET: Hook respektiert Sync:CoatingDetectionEnabled (Freigabe-Antwort 12)"
  - "13 -> BEANTWORTET: Erkennungsfenster nicht in diesem Epic, eigene Aufgabe (Freigabe-Antwort 13)"
  - "14 -> BEANTWORTET: nur /Settings, Standort-Maske mit Hinweis-Link (Freigabe-Antwort 14)"
  - "15 -> BEANTWORTET: View an der AKE nicht waehlbar bis P-A5; P-A5 vor E5 durch Gerald (Freigabe-Antwort 15)"
  - "16 -> BEANTWORTET: v1.99.25 nach 1.99.24 Sicherheit, Nummern beim Start pruefen (Freigabe-Antwort 16)"
epic: true
etappen:
  - "E1 Katalog + Resolver (ohne Verhaltensaenderung)"
  - "E2 Web-Konsumenten lesen nur noch den Resolver"
  - "E3 Dienst schreibt das Kennzeichen nach der effektiven Quelle"
  - "H1 Haltepunkt P-I1 (Gerald erhebt) vor E4"
  - "E4 IDEAL-Termin im Leitstand und in der Werkbank-Arbeitsliste"
  - "H2 Haltepunkt P-A5 (Gerald erhebt), P-A3 als Beleg, vor E5"
  - "E5 Beschichtungsauftrag und Vorschlagsliste an der AKE (Gate entkoppeln, flache Datenbasis, keine Migration)"
  - "E6 Doku, Testszenarien, Hilfe, Changelog, Manifest, Brain"
deploy:
  web: true
  service: true
  migration: false
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
- Schema-Aenderung am Beschichtungsauftrag: keine, die AKE-WA-Nummern sind numerisch, `HauptFA` bleibt `int` (Freigabe-Antwort 9).
- Termin-Quelle `SData` (Freigabe-Antwort 1): nur als Erweiterungspunkt vorgesehen, **nicht gebaut**, kein Auswahlwert in dieser Version. Der Choice-Katalog und `ResolveQuellen` sind so geschnitten, dass ein weiterer Wert spaeter nur einen Katalogeintrag und einen Resolver-Zweig braucht. Zuordnung des Feldes `USER_OSBeschichtet` zur Achse ist offen (Rueckfrage 17 im Nachtrag).
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

### Soll

| ID | Anforderung | Begruendung / Quelle |
|---|---|---|
| A1 | **Kennzeichen-Quelle** je Standort waehlbar: `Automatisch` / `Artikelkategorie` / `FaStruktur`. `Automatisch` = `FaStruktur`, wenn der Master `ProduktionsauftragHierarchisch` an ist, sonst `Artikelkategorie`. | Fokus 2a; Default = heute |
| A2 | **Termin-Quelle** je Standort waehlbar: `Automatisch` / `Berechnet` / `View`. `Automatisch` = `View` hierarchisch, sonst `Berechnet`. Ein Wert `SData` ist vorgesehen, aber nicht Teil dieser Version (kein Auswahlwert). | Fokus 2b; Default = heute; Freigabe-Antwort 1 |
| A3 | **Ein Resolver** liefert je FA: `Beschichtet` (bool), `Termin` (DateTime?), `TerminQuelle`, `Aktiv` (Spalte/Haken relevant). Kein Verbraucher liest mehr `LackierteilKategorieName`, `BeschichtungTage`, `BeschichtungAbholtage` oder `Start_Beschichtung` selbst. | Fokus 3 |
| A4 | `Termin` bei Quelle `View` = `Start_Beschichtung` des eindeutigen Kopfes, **unveraendert uebernommen**, nur wenn `Beschichtet`. `Start_Beschichtung` ist in Sage bereits berechnet; das WMS rechnet nichts darauf (kein `CoatingDateCalculator`, keine `BeschichtungTage`, kein Abholtag). Der Calculator geht vom Vorkommissionier-Termin aus (`CoatingDateCalculator.cs:11-24`), nicht von `Start_Beschichtung`. Bei Kombigeraet oder leerem Feld: null, **nie** ein Ersatztermin (TS-71.11b). | Bestand, Freigabe-Antworten 4, 5 |
| A5 | `Termin` bei Quelle `Berechnet` = heutige Formel (`CoatingDateCalculator`), Basisdatum = Vorkommissioniertermin aus dem FA-Datum. Hierarchisch ist das FA-Datum der Kopf-`FE_Termin` (wie die FA-Liste `:199`, `:305`). | Rueckfrage 6 |
| A6 | `Aktiv`: bei Quelle `Artikelkategorie` = `LackierteilKategorieName` nicht leer (heute), bei `FaStruktur` = immer true. | Rueckfrage 7 |
| A7 | Der Dienst schreibt `HasCoatingParts` nur aus der **effektiven** Kennzeichen-Quelle: `CoatingDetectionService` laeuft nur bei `Artikelkategorie`, die Materialisierung setzt das Flag nur bei `FaStruktur`. Der Reset von `IsCoatingDone` beim Kippen auf false bleibt auf beiden Wegen. | Fokus 1/6 |
| A8 | Der Server lehnt beim Speichern Kombinationen ab, die keine Datenbasis haben: `Artikelkategorie` bei Master an, `FaStruktur` bei Master aus, `View` bei Master aus (bis P-A5). Ein ungueltiger Bestandswert wird beim Lesen wie `Automatisch` behandelt und protokolliert (ADR 0020 Punkt 2). | Rueckfrage 2/15 |
| A9 | IDEAL: FA-Liste, Leitstand und Arbeitsliste zeigen denselben Termin aus dem Resolver. | Fokus 5, Rueckfrage 3 |
| A10 | Beschichtungs-Vorschlagsliste und Beschichtungsauftrag an der AKE nutzbar, Bedienung gleich wie an der IDEAL (Liste der zu beschichtenden Teile, daraus „Aus Vorschlagsliste erstellen“). Gate `FaHierarchyBeschichtungAktiv` ohne Standortbindung (Standort „beide“), **keine** Master-Bedingung an der Vorschlagsliste. Datenbasis an der AKE = Lackierteile der WA-Stueckliste (die Kette aus `CoatingDetectionService.cs:149-156`). Kreditor/Beschichter (`Dienstleister`), RAL und Retour bleiben leer und werden manuell befuellt; Masse 0; Start aus dem berechneten Termin. `HauptFA` bleibt `int`. Rollen unveraendert. | Fokus 4, Freigabe-Antworten 8-11 |
| A11 | Alle Bestands-Szenarien TS-71.10, TS-71.10a, TS-71.11 und die Leitstand-/Arbeitslisten-Termine bleiben mit `Automatisch` unveraendert gruen. | Regression |

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

### Verbraucher

| Verbraucher | heute | danach |
|---|---|---|
| FA-Liste `ProductionOrdersController.cs:139-147`, `:209-227`, `:408-425` | inline-Formel + Kopf-Start | `Bestimmen(...)`, Kopf aus dem vorhandenen `headsByFa` (`:274-275`) |
| Leitstand `PickingLeitstandController.cs:103-111`, `:173-175` | Calculator mit `ProductionDate` | `Bestimmen(...)`. Hierarchisch: Koepfe per `GetByHauptFaKeysAsync` einmal je Seite nachladen (Muster `:271-275`) |
| Arbeitsliste `FaWorklistController.cs:200-203`, `:264-274` | Calculator mit `ProductionDate` | wie Leitstand |
| Zeilen-Views (`_ProductionOrderRow` `:16-20`, `:100-120`; `_PickingLeitstandRow` `:82-94`; `_FaWorklistRow` `:51`) | `LackierteilKategorieName` im View | `Aktiv` und `Termin` aus dem ViewModel |
| Spaltenkopf-Tooltips `ProductionOrders/Index.cshtml:106`, `PickingLeitstand/Index.cshtml:140` | Formeltext fest | Text je `TerminQuelle`: `View` = „aus der Sage-View, Feld Start_Beschichtung, unveraendert uebernommen“; `Berechnet` = Formeltext wie heute |
| Beschichtungsauftrag-Kopf `BeschichtungsauftragWorkflow.cs:404-425` | FAInfos | IDEAL unveraendert; AKE: Start aus `Bestimmen(...).Termin` (Quelle `Berechnet`), Dienstleister/RAL/Retour leer (E5) |
| Vorschlagsliste `BeschichtungsauftragService.cs:94-111`, `:158-174`; `BeschichtungsauftragVergabeAnzeige.cs:43` | `FaHierarchyNode` mit `Beschichtet` | Positionsquelle nach `ResolveQuellen(...).Kennzeichen`: `FaStruktur` wie heute, `Artikelkategorie` = flache WA-Stuecklisten-Projektion (E5). Kopfzeile an der AKE leer (`_BeschichtungKopf.cshtml:27-30`) |

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
  - Neue Lesemethode, eine Abfrage, liefert die Lackierteil-Positionen offener WAs aus `CachedBomHeaders/Items` mit der Kategorie-Kette `CoatingDetectionService.cs:149-156` (Kopf ueber `CachedBomHeaders.Artikelnummer = ProductionOrders.ArticleNumber`, wie `CoatingDetectionService.cs:90-109`, `:150-155`, `:167`). Welche WAs: offene mit `HasCoatingParts = true` (die von der Erkennung gesetzte Wahrheit), damit Liste und Kennzeichen nicht auseinanderlaufen.
  - Sie projiziert jede Zeile in die vorhandene Form `FaHierarchyNode` (nur im Speicher, nicht persistiert), damit `AggregateSummiert` (`BeschichtungsauftragService.cs:185-242`), der Listen-Builder, `PositionsSchluessel` und `Snapshot` unveraendert weiterlaufen:
    `HauptFA = int(OrderNumber)` (numerisch, Freigabe-Antwort 9, P-A3 als Beleg), `HauptArtnr = ProductionOrder.ArticleNumber`, `VaterFA = null`, `SubFA = 0`, `Position = CachedBomItem.SortOrder`, `Artnr = Ressourcenummer`, `Matchcode` per vorhandenem `MatchcodeLookup`, `Sollmenge` nach Rueckfrage 18 (Empfehlung `Menge × ProductionOrder.Quantity`), Masse = 0, `Beschichtet = true`.
  - Der `PositionsSchluessel` bleibt damit eindeutig je Stuecklistenzeile.
  - `BeschichtungsauftragService.BuildAsync`/`BuildSummiertAsync`, `BeschichtungsauftragVergabeAnzeige.ErmittelnAsync` und der Workflow waehlen die Quelle ueber `ResolveQuellen(...).Kennzeichen`: `FaStruktur` = `FaHierarchyNode` wie heute, `Artikelkategorie` = flache Projektion.
  - Kopfdaten (`FaHierarchyOrderInfo`) gibt es an der AKE nicht; die Kopfzeile zeigt den bestehenden Hinweis (`_BeschichtungKopf.cshtml:27-30`). Kreditor/Beschichter (`Dienstleister`), RAL und Retour werden im Auftrag manuell befuellt (Freigabe-Antwort 10).
  - Spaltenkataloge: „(IDEAL)“ aus den Anzeigenamen entfernen (`ColumnDefinitions.cs:379`, `:404`); Spalten unveraendert.
- **Workflow:** `AnlegenAsync` und die Kandidaten-/Pruefstellen (`BeschichtungsauftragWorkflow.cs:88-120`, `:221-222`, `:262-268`) waehlen die Quelle ueber `ResolveQuellen(...).Kennzeichen`. Kopfvorbelegung AKE: Start = `Bestimmen(...).Termin` (Quelle `Berechnet`), Rest leer.
- **Kein Schema-Eingriff** (Freigabe-Antwort 9): Die AKE-WA-Nummern sind numerisch, `Beschichtungsauftrag.HauptFA` bleibt `int`. ~~Sonst SQL/152 mit Spalte `Auftragsnummer`~~ *ueberholt*. P-A3 wird vor E5 nur als bestaetigender Beleg erhoben; zeigt sie wider Erwarten nicht-numerische Nummern, haelt der Lauf E5 als Blocker fest, statt ein Schema zu erfinden.

## Migrations-/SQL-Auswirkungen

- **E1-E6: keine Schema-Aenderung**, keine EF-Migration, kein SQL-Skript (Freigabe-Antwort 9: `HauptFA` bleibt `int`; die fruehere Reservierung SQL/152 ist *ueberholt* und entfaellt).
  - Die neuen AppSettings werden per Startup-Seed aus `AppSettingDefinitions.All` angelegt (Seed true, Default `Automatisch`).
  - Keine `SQL/00_FreshInstall.sql`-Aenderung; der FreshInstall-Seed laeuft ueber denselben Startup-Seed.
  - Die flache AKE-Positionsquelle ist eine reine Lesemethode auf vorhandenen Tabellen.
- **Bestand beim Umschalten** (keine Datenmigration, Neuberechnung durch den naechsten Lauf):
  - Wechsel der Kennzeichen-Quelle: Der neue Schreiber ueberschreibt beim naechsten Lauf. Kippt eine Zeile auf false, setzt er `IsCoatingDone` mit zurueck; dieser Verlust ist gewollt und steht im Warnhinweis.
  - Grenze `Artikelkategorie`: Neu berechnet werden nur Auftraege im Erkennungsfenster (8 Wochen / 200). Zeilen ausserhalb behalten den alten Wert, bis sie ins Fenster kommen. Das wird dokumentiert, nicht still behandelt.
  - Wechsel der Termin-Quelle: wirkt sofort beim naechsten Seitenaufruf, nichts gespeichert.
- **AgentJobs:** keine betroffen (nur `_archiv`).
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
16. IDEAL (E5): Vorschlagsliste und Beschichtungsauftrag verhalten sich unveraendert (bestehende Tests zu Teil 3/4 und Ausbau Teil 1-4 gruen).
17. Build und alle Tests gruen; je Standort × Quellwert mindestens ein Resolver-Test (Matrix unten).
18. Keine EF-Migration, kein neues SQL-Skript; `Beschichtungsauftrag.HauptFA` bleibt `int` (Freigabe-Antwort 9).
19. `/Settings` bietet fuer `BeschichtungTerminQuelle` genau `Automatisch`, `Berechnet`, `View` an (kein `SData`); an der AKE (Master aus) wird `View` beim Speichern abgelehnt (Freigabe-Antwort 15).

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
- `NachlaufManifestDriftTests`.

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
  - Vorbedingung: P-A3 erhoben (alle WA-Nummern numerisch), Gate an, Rolle zugewiesen, Lackier-WA mit `HasCoatingParts = true` und BomCache.
  - Vorschlagsliste (Summiert und Liste): WA erscheint mit seinen Lackierteilen, Masse 0, Kopfzeile „Keine Auftrags-Kopfdaten zu diesem HauptFA.“; Spaltenfilter und Paging funktionieren.
  - „Aus Vorschlagsliste erstellen“: Auftrag mit den Lackierteil-Positionen, Start mit dem berechneten Termin vorbelegt, Dienstleister/RAL/Retour leer und manuell speicherbar.
  - Negativfaelle: ohne Rolle bzw. Gate aus kein Menue, Direktaufruf Redirect plus Warnung; ohne Schreibrolle kein Erstellen-Knopf; WA ohne Lackierteile nicht in der Liste; zweite Anlage desselben WA zeigt die Positionen als vergeben.
  - IDEAL-Gegenprobe: Vorschlagsliste und Auftrag wie vorher (Kopfdaten aus FAInfos).

Danach `secondbrain/tests/testszenarien-index.md` nachziehen.

## Etappen (nur bei epic: true)

**Epic statt Split:**

- Alle Etappen teilen einen Kern, den Resolver und die beiden Keys. Einzeln gemergt, laege nach E1 ruhender Code ohne Verbraucher in `main`.
- Die Arbeit laeuft im vorhandenen Buendel-Worktree `feature/2026-08-07-ideal-teile-1-5` (ein Merge am Ende).
- Den Branch waehrend der Arbeit mit `scripts/sync-worktree.ps1 -Slug 2026-08-07-ideal-teile-1-5` auf main-Stand halten.
- Jede Etappe baut und testet fuer sich gruen und endet mit einem Commit. Keine Etappe wartet auf eine offene Frage: Wo eine Antwort fehlt, baut der `/goal`-Lauf die empfohlene Antwort und vermerkt das (Rueckfrage 18), oder er haelt die Etappe als Blocker fest (E4 ohne P-I1-Ergebnis, E5 ohne P-A5-Ergebnis oder bei nicht-numerischen WA-Nummern in P-A3).
- Alle Rueckfragen 1-16 sind beantwortet (Freigabe-Antworten); die Spalte „Abhaengig von“ nennt die Antwort, nach der gebaut wird.

| # | Etappe | Inhalt | Abhaengig von | SQL | Status | Commit |
|---|--------|--------|---------------|-----|--------|--------|
| 1 | Katalog + Resolver | 2 Choice-Keys (Termin-Quelle ohne `SData`), Beschreibungsergaenzungen, Server-Validierung (A8, `View` an der AKE abgelehnt), `BeschichtungResolver` plus Matrix-Tests (View = `Start_Beschichtung` 1:1); noch kein Verbraucher umgestellt | Freigabe-Antworten 1, 2, 4, 15 | – | offen | |
| 2 | Web-Verbraucher | FA-Liste (Inline-Formel weg), Leitstand, Arbeitsliste, Zeilen-Views, Tooltips je `TerminQuelle` auf den Resolver; Waechter-Test AK 9; Verhalten mit Automatisch identisch | E1; Freigabe-Antwort 7 | – | offen | |
| 3 | Dienst | Gates ueber `ResolveQuellen`, Hook-Gate, Audit `SetCoatingPartsAsync`, Protokoll-Meldungen | E1; Freigabe-Antwort 12 | – | offen | |
| H1 | **Haltepunkt P-I1** | Gerald erhebt P-I1 (Schalterstand IDEAL) und traegt das Ergebnis im Abschnitt „Pruefabfragen“ ein. Liegt kein Ergebnis vor, vermerkt der Lauf E4 als Blocker und macht mit E5 weiter. | Gerald; Freigabe-Antwort 7 | – | offen | |
| 4 | IDEAL-Termin Leitstand/Arbeitsliste | Koepfe je Seite nachladen, Termin aus dem Resolver (View = `Start_Beschichtung` unveraendert); Basisdatum Berechnet = Kopf-`FE_Termin` | E2, H1; Freigabe-Antworten 3, 4, 5, 6 | – | offen | |
| H2 | **Haltepunkt P-A5 (+ P-A3 als Beleg)** | Gerald erhebt P-A5 (Datumsspalte in `vw_AKE_Kommissionierung_WAListe`?) und P-A3 (WA-Nummern numerisch) und traegt die Ergebnisse im Abschnitt „Pruefabfragen“ ein. P-A5 aendert in dieser Version nichts am Bau (`View` an der AKE bleibt gesperrt); P-A3 muss 0 nicht-numerische Nummern zeigen, sonst E5 Blocker. | Gerald; Freigabe-Antworten 9, 15 | – | offen | |
| 5 | Vorschlagsliste + Beschichtungsauftrag AKE | Gate-Metadaten (beide), **keine** Master-Bedingung an der Vorschlagsliste; flache Positionsquelle (WA-Stueckliste als `FaHierarchyNode`-Projektion) fuer `BeschichtungsauftragService` (Liste/Summiert), `BeschichtungsauftragVergabeAnzeige` und Workflow-Weiche; AKE-Kopfvorbelegung (Start berechnet, Dienstleister/RAL/Retour leer); „(IDEAL)“ aus den Spaltenkatalog-Namen | E1, E3, H2; Freigabe-Antworten 8, 9, 10, 11; Rueckfrage 18 (Empfehlung baubar) | – (keine Migration) | offen | |
| 6 | Doku und Abschluss | TS-140..145 in `docs/TESTSZENARIEN.md`, Hilfe (`Help/Index.cshtml` Beschichtung, `:1265`), `RoleOverview.cshtml` (Vorschlagsliste auch AKE/WA-Stueckliste), Standort-Maske Hinweis-Link, Anwender-Changelog (IDEAL-Termin in Leitstand/Arbeitsliste, AKE-Beschichtung), `docs/betrieb/04-konfiguration.md`, `README.md` AppSettings, Manifest-Eintraege, Versions-Bump beide `AppVersion.cs` auf v1.99.25 (nach 1.99.24 Sicherheit; beim Start pruefen), Brain (ADR 0028, fallstricke, codebase, glossar, feature-map, changelog; Aufgabe Erkennungsfenster-Filter, Rueckfrage 13) | E1-E5 | – | offen | |

## Deploy

*Vorlaeufig, der QA-Lauf bestaetigt gegen den echten Diff.*

- **Web-App:** ja (Settings-Katalog, Resolver, Controller, Views, Vorschlagsliste, Beschichtungsauftrag)
- **Service:** ja (Gates, Hook, Audit). Web und Dienst gemeinsam deployen, weil beide den Katalog teilen.
- **Migration:** nein, kein SQL-Skript (Freigabe-Antwort 9; die fruehere bedingte SQL/152 ist *ueberholt*).
- **Manifest-Eintraege** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023; Version v1.99.25 laut Freigabe-Antwort 16, beim Start pruefen):

| Id | Version | Typ | Firma | Phase | Ausfuehrung | Text / Pruefung |
|---|---|---|---|---|---|---|
| `Pruefung/BeschichtungQuelleKombination` | 1.99.25 | Pruefung | beide | Haltepunkt | anzeigen | Pruefung (Zeilenzahl > 0 = Befund, siehe unten). Text: „Beschichtungsquelle passt nicht zum Auftragsmodell, wirkt wie Automatisch; unter /Settings korrigieren.“ |
| `v1.99.25-Beschichtungsquelle-Info` | 1.99.25 | Info | beide | Nachher | anzeigen | „Neue Einstellungen Quelle Beschichtet-Kennzeichen / Quelle Beschichtungstermin stehen auf Automatisch, das Verhalten ist unveraendert. IDEAL: Leitstand und Werkbank-Arbeitsliste zeigen jetzt den Beschichtungstermin aus der Sage-View.“ |
| `v1.99.25-Beschichtungsauftrag-AKE` | 1.99.25 | Einstellung | AKE | Nachher | anzeigen | „Optional: Beschichtungsauftrag aktiv (FaHierarchyBeschichtungAktiv) unter /Settings einschalten; dann erscheinen Vorschlagsliste (Lackierteile der WA-Stueckliste) und Auftraege im Menue Beschichtung. Rollen beschichtungsauftrag / beschichtungsauftrag_schreiben zuweisen (Anlage: Pruefung/RollenBeschichtungsauftrag). Dienstleister/RAL/Retour werden am Auftrag von Hand gepflegt.“ |

~~`SQL/152` (nur falls Rueckfrage 9 = Spalte), Schema, `152_AddBeschichtungsauftragAuftragsnummer.sql`~~ *ueberholt durch Freigabe-Antwort 9: kein Schema-Eintrag.*

Pruefabfrage zu `Pruefung/BeschichtungQuelleKombination`:

```sql
SELECT COUNT(*) FROM dbo.AppSettings a CROSS JOIN (SELECT LOWER(LTRIM(RTRIM([Value]))) m FROM dbo.ServiceSettings WHERE [Key]=N''ProduktionsauftragHierarchisch'') s WHERE (a.[Key]=N''BeschichtungKennzeichenQuelle'' AND ((a.[Value]=N''Artikelkategorie'' AND s.m=N''true'') OR (a.[Value]=N''FaStruktur'' AND s.m<>N''true''))) OR (a.[Key]=N''BeschichtungTerminQuelle'' AND a.[Value]=N''View'' AND s.m<>N''true'')
```

Kein Einmal- oder Daten-Skript, also auch keine Trockenlauf-Pflicht.

- **Publish-Befehle** (im Worktree):
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  - `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- Gilt im Buendel: DEV AKE nie mit dem main-Stand deployen.

### Pruefabfragen (nur lesend, vom Menschen auszufuehren; Ergebnisse vor Schranke 1 hier eintragen)

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

- **P-A4** Flag-Bestand und BomCache-Abdeckung:

  ```sql
  SELECT ps.HasCoatingParts, ps.IsCoatingDone, COUNT(*) n, SUM(CASE WHEN EXISTS (SELECT 1 FROM dbo.CachedBomHeaders h WHERE h.ArticleNumber=po.ArticleNumber) THEN 1 ELSE 0 END) mit_BomCache
  FROM dbo.ProductionOrders po JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId=po.Id WHERE po.IsDone=0 GROUP BY ps.HasCoatingParts, ps.IsCoatingDone;
  ```

  Den Join-Schluessel von `CachedBomHeaders` vor dem Lauf gegen `CoatingDetectionService.cs:149-156` abgleichen und ggf. anpassen.

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
