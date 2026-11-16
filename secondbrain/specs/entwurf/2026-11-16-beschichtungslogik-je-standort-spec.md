---
type: spec
title: Beschichtungslogik je Standort - Quelle fuer Kennzeichen und Termin, ein Resolver, Beschichtungsauftrag auch an der AKE
slug: 2026-11-16-beschichtungslogik-je-standort-spec
status: Entwurf
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
  - IdealAkeWms/Views/Shared/_Layout.cshtml
  - IDEALAKEWMSService/Services/CoatingDetectionService.cs
  - IDEALAKEWMSService/Services/SageImportService.cs
  - IDEALAKEWMSService/Services/FaMaterializationSyncService.cs
  - IDEALAKEWMSService/Workers/SyncWorker.cs
  - scripts/deploy/nachlauf.psd1
  - docs/TESTSZENARIEN.md
  - docs/betrieb/04-konfiguration.md
open_questions:
  - "1. Ein Schluessel (Beschichtungslogik) oder zwei Schluessel (Kennzeichen-Quelle, Termin-Quelle)? Empfehlung: zwei Choice-AppSettings mit Default Automatisch."
  - "2. Soll der Server Kombinationen ablehnen, die zum Auftragsmodell nicht passen (Artikelkategorie bei hierarchisch, FA-Struktur bei flach, View-Termin an der AKE)? Empfehlung: ja."
  - "3. Soll der Kommissionier-Leitstand und die Werkbank-Arbeitsliste an der IDEAL den Termin aus der View zeigen (heute leer)? Empfehlung: ja, als bewusste Verhaltensaenderung in E4."
  - "4. Ist Start_Beschichtung fachlich derselbe Termin wie der berechnete Beschichtungstermin (Abholung zum Beschichter)? Empfehlung: Spaltenname bleibt Beschichtungstermin, Tooltip nennt die Quelle; Bestaetigung durch IDEAL-Keyuser."
  - "5. Kombigeraet (mehrere Koepfe): Termin weiter leer oder fruehester Start_Beschichtung? Empfehlung: leer lassen wie heute (TS-71.11a)."
  - "6. Termin-Quelle Berechnet an der IDEAL: Basisdatum = FE_Termin des Kopfes (wie FA-Liste heute) auch im Leitstand/Arbeitsliste? Empfehlung: ja, ueber den Resolver einheitlich."
  - "7. Aktiv-Kennung an der IDEAL: heute haengt sie an LackierteilKategorieName; mit Quelle FA-Struktur gilt sie immer als aktiv (Haken nur noch an beschichteten Zeilen). Empfehlung: ja, nach Pruefabfrage P-I1."
  - "8. AKE-Beschichtungsauftrag fachlich: Positionen = Lackierteile der WA-Stueckliste (CachedBomItems mit Lackierteil-Kategorie), Masse leer/0, Kopf manuell, Start aus dem berechneten Termin vorbelegt? Empfehlung: ja."
  - "9. AKE-WA-Nummern rein numerisch (Beschichtungsauftrag.HauptFA ist int)? Empfehlung: Pruefabfrage P-A3; alle numerisch -> int behalten, sonst neue Spalte Auftragsnummer (SQL/152)."
  - "10. Beschichtungs-Vorschlagsliste (FaHierarchyBeschichtung) an der AKE ebenfalls? Empfehlung: nein, nur Beschichtungsauftrag; Vorschlagsliste bleibt IDEAL."
  - "11. Gate-Schluessel FaHierarchyBeschichtungAktiv umbenennen? Empfehlung: nein, Key behalten, Anzeigename/Gruppe/Standort auf beide aendern."
  - "12. Soll der Lackierteil-Hook im Sage-Import das Gate Sync:CoatingDetectionEnabled respektieren (heute nur Sync:BomCacheEnabled)? Empfehlung: ja."
  - "13. Erkennungsfenster der AKE filtert IsDonePicking/IsCancelled nicht. Aendern? Empfehlung: nein in diesem Epic, als Aufgabe festhalten."
  - "14. Die beiden Schluessel auch in die Standort-Maske (/StandortEinstellungen; kennt heute keinen Choice-Typ)? Empfehlung: nein, nur /Settings; Standort-Maske bekommt einen Link."
  - "15. Gibt es an der AKE eine View-Spalte mit Beschichtungsdatum (vw_AKE_Kommissionierung_WAListe)? Empfehlung: Pruefabfrage P-A5; bis dahin ist View an der AKE nicht waehlbar."
  - "16. Zielversion v1.99.25 (naechste freie nach der reservierten 1.99.24)? Empfehlung: ja, alle Etappen unter einer Version."
epic: true
etappen:
  - "E1 Katalog + Resolver (ohne Verhaltensaenderung)"
  - "E2 Web-Konsumenten lesen nur noch den Resolver"
  - "E3 Dienst schreibt das Kennzeichen nach der effektiven Quelle"
  - "E4 IDEAL-Termin im Leitstand und in der Werkbank-Arbeitsliste"
  - "E5 Beschichtungsauftrag an der AKE (Gate entkoppeln, flache Datenbasis)"
  - "E6 Doku, Testszenarien, Hilfe, Changelog, Manifest, Brain"
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Woher „beschichtet“ und der Beschichtungstermin kommen, ist heute je Standort hart verdrahtet und auf mehrere Stellen
verteilt: Die AKE erkennt Lackierteile ueber die OSEON-Artikelkategorie, die IDEAL ueber die FA-Struktur-View. Der Termin
wird an drei Stellen unterschiedlich ermittelt, einmal davon als kopierte Formel. Das hat Folgen:

- Die IDEAL sieht nur in der FA-Liste einen Termin, nicht im Leitstand und nicht in der Arbeitsliste.
- Die AKE kann den Beschichtungsauftrag nicht nutzen.

Ziel:

1. **Eine** Standort-Einstellung legt die Quelle fest.
2. **Ein** Resolver liefert Kennzeichen, Termin und Aktiv-Kennung an alle Verbraucher.
3. Die Defaults behalten das heutige Verhalten je Standort bei.
4. Danach werden die Features standortunabhaengig: der Beschichtungsauftrag an der AKE und der richtige View-Termin an der IDEAL.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

- Zwei Choice-AppSettings, Empfehlung zu Rueckfrage 1:
  - `BeschichtungKennzeichenQuelle`
  - `BeschichtungTerminQuelle`
- Beide mit dem Wert `Automatisch`, der das heutige Verhalten aus dem Master `ProduktionsauftragHierarchisch` ableitet.
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
- Das Gate `FaHierarchyBeschichtungAktiv` wird von der IDEAL entkoppelt, und der Beschichtungsauftrag bekommt eine flache AKE-Datenbasis (E5).

**Out-of-Scope**

- Schreiben an Sage, Aenderungen an Sage-Views.
- Einstellungen auf Test-/Prod-Systemen setzen.
- Rollen: bleiben unveraendert, `beschichtungsauftrag` / `beschichtungsauftrag_schreiben`.
- Beschichtungs-Vorschlagsliste an der AKE (Rueckfrage 10).
- Erkennungsfenster-Filter (Rueckfrage 13).
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
- `CachedBomItem` hat weder Masse noch eine int-Position (`Position` ist string, `IdealAkeWms/Models/CachedBomItem.cs:17`).

### Soll

| ID | Anforderung | Begruendung / Quelle |
|---|---|---|
| A1 | **Kennzeichen-Quelle** je Standort waehlbar: `Automatisch` / `Artikelkategorie` / `FaStruktur`. `Automatisch` = `FaStruktur`, wenn der Master `ProduktionsauftragHierarchisch` an ist, sonst `Artikelkategorie`. | Fokus 2a; Default = heute |
| A2 | **Termin-Quelle** je Standort waehlbar: `Automatisch` / `Berechnet` / `View`. `Automatisch` = `View` hierarchisch, sonst `Berechnet`. | Fokus 2b; Default = heute |
| A3 | **Ein Resolver** liefert je FA: `Beschichtet` (bool), `Termin` (DateTime?), `TerminQuelle`, `Aktiv` (Spalte/Haken relevant). Kein Verbraucher liest mehr `LackierteilKategorieName`, `BeschichtungTage`, `BeschichtungAbholtage` oder `Start_Beschichtung` selbst. | Fokus 3 |
| A4 | `Termin` bei Quelle `View` = `Start_Beschichtung` des eindeutigen Kopfes, nur wenn `Beschichtet`. Bei Kombigeraet oder leerem Feld: null, **nie** ein Ersatztermin (TS-71.11b). | Bestand, Rueckfrage 5 |
| A5 | `Termin` bei Quelle `Berechnet` = heutige Formel (`CoatingDateCalculator`), Basisdatum = Vorkommissioniertermin aus dem FA-Datum. Hierarchisch ist das FA-Datum der Kopf-`FE_Termin` (wie die FA-Liste `:199`, `:305`). | Rueckfrage 6 |
| A6 | `Aktiv`: bei Quelle `Artikelkategorie` = `LackierteilKategorieName` nicht leer (heute), bei `FaStruktur` = immer true. | Rueckfrage 7 |
| A7 | Der Dienst schreibt `HasCoatingParts` nur aus der **effektiven** Kennzeichen-Quelle: `CoatingDetectionService` laeuft nur bei `Artikelkategorie`, die Materialisierung setzt das Flag nur bei `FaStruktur`. Der Reset von `IsCoatingDone` beim Kippen auf false bleibt auf beiden Wegen. | Fokus 1/6 |
| A8 | Der Server lehnt beim Speichern Kombinationen ab, die keine Datenbasis haben: `Artikelkategorie` bei Master an, `FaStruktur` bei Master aus, `View` bei Master aus (bis P-A5). Ein ungueltiger Bestandswert wird beim Lesen wie `Automatisch` behandelt und protokolliert (ADR 0020 Punkt 2). | Rueckfrage 2/15 |
| A9 | IDEAL: FA-Liste, Leitstand und Arbeitsliste zeigen denselben Termin aus dem Resolver. | Fokus 5, Rueckfrage 3 |
| A10 | Beschichtungsauftrag an der AKE nutzbar. Gate `FaHierarchyBeschichtungAktiv` ohne Standortbindung (Standort „beide“). Positionen an der AKE = Lackierteile der WA-Stueckliste (die Kette aus `CoatingDetectionService.cs:149-156`). Rollen unveraendert. | Fokus 4, Rueckfragen 8-11 |
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

**Empfehlung Variante 2.** Gerald hat die Trennung in Kennzeichen- und Termin-Quelle selbst vorgegeben. Der eigentliche Schalter ist die Termin-Quelle; die Kennzeichen-Quelle ist durch das Auftragsmodell faktisch festgelegt (A8). Sie bekommt trotzdem einen eigenen Key, damit die Quelle sichtbar und dokumentiert ist und eine spaetere dritte Quelle keinen Umbau braucht.

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
   - `Termin` nach A4/A5.

### Verbraucher

| Verbraucher | heute | danach |
|---|---|---|
| FA-Liste `ProductionOrdersController.cs:139-147`, `:209-227`, `:408-425` | inline-Formel + Kopf-Start | `Bestimmen(...)`, Kopf aus dem vorhandenen `headsByFa` (`:274-275`) |
| Leitstand `PickingLeitstandController.cs:103-111`, `:173-175` | Calculator mit `ProductionDate` | `Bestimmen(...)`. Hierarchisch: Koepfe per `GetByHauptFaKeysAsync` einmal je Seite nachladen (Muster `:271-275`) |
| Arbeitsliste `FaWorklistController.cs:200-203`, `:264-274` | Calculator mit `ProductionDate` | wie Leitstand |
| Zeilen-Views (`_ProductionOrderRow` `:16-20`, `:100-120`; `_PickingLeitstandRow` `:82-94`; `_FaWorklistRow` `:51`) | `LackierteilKategorieName` im View | `Aktiv` und `Termin` aus dem ViewModel |
| Spaltenkopf-Tooltips `ProductionOrders/Index.cshtml:106`, `PickingLeitstand/Index.cshtml:140` | Formeltext fest | Text je `TerminQuelle` („aus der Sage-View Start_Beschichtung“ / Formel) |
| Beschichtungsauftrag-Kopf `BeschichtungsauftragWorkflow.cs:404-425` | FAInfos | IDEAL unveraendert; AKE: Start aus `Bestimmen(...).Termin`, Rest leer (E5) |

Die Datumsfilter `coating-date` (`ProductionOrdersController.cs:485`, `:513`; `PickingLeitstandController.cs:695`, `:737`; `FaWorklistController.cs:417`) laufen weiter **nach** der Terminberechnung in C# (ADR 0005). Sie lesen das Ergebnis, nicht die Quelle.

### Dienst und Klasse-D-Gate

- **`CoatingDetectionService.cs:40-46`**: Statt `IHierarchicalModeReader` direkt zu fragen, prueft das Gate `ResolveQuellen(...).Kennzeichen == Artikelkategorie`.
  - Mit `Automatisch` ist das Ergebnis identisch zu heute.
  - **Einordnung:** Das Gate bleibt ein technischer Schutz. Der BomCache wird im hierarchischen Modus nicht gefuellt (`BomCacheSyncService.cs:21`, `:27` mit demselben Reader), und A8 verbietet die Kombination.
- **Kategorie-Heuristik:** `LackierteilKategorieName` bleibt der Eingabewert der Quelle `Artikelkategorie` und ist dort Pflicht. Leer bedeutet weiterhin „keine Erkennung“, wie `:64-74`.
- **`SageImportService.cs:198-215`**: Der Hook laeuft nur noch bei effektiver Quelle `Artikelkategorie` **und** `Sync:CoatingDetectionEnabled` (Rueckfrage 12).
- **`FaMaterializationSyncService.cs:199-213`**: `SetCoatingPartsAsync` wird nur bei Quelle `FaStruktur` aufgerufen. `Automatisch` mit Master an bedeutet immer `FaStruktur`, also wie heute.
- **`SetCoatingPartsAsync`** bekommt `modifiedBy` (`"FaMaterialization"`) und `ModifiedByWindows` mit (Audit).

### Beschichtungsauftrag an der AKE (E5)

- **Gate:**
  - Key `FaHierarchyBeschichtungAktiv` bleibt (Rueckfrage 11); Anzeigename „Beschichtungsauftrag aktiv“, Gruppe „Termine und Beschichtung“, Standort beide.
  - Die Vorschlagsliste `FaHierarchyBeschichtungController.cs:29` bekommt zusaetzlich die Bedingung „Master an“, damit sie an der AKE nicht leer erreichbar ist (Rueckfrage 10).
- **Datenbasis flach:**
  - Neue Lesemethode, eine Abfrage, liefert die Lackierteil-Positionen eines WA aus `CachedBomHeaders/Items` mit der Kategorie-Kette `CoatingDetectionService.cs:149-156`.
  - Snapshot: `HauptFA = int(OrderNumber)` (P-A3), `VaterFA = null`, `SubFA = 0`, `Position = CachedBomItem.SortOrder`, `Artnr = Ressourcenummer`, `Matchcode` per vorhandenem `MatchcodeLookup`, `Menge`, Masse = 0.
  - Der `PositionsSchluessel` bleibt damit eindeutig je Stuecklistenzeile.
- **Workflow:** `AnlegenAsync` und die Kandidaten-/Pruefstellen (`:88-120`, `:221-222`, `:262-268`) waehlen die Quelle ueber `ResolveQuellen(...).Kennzeichen`.
- **Kein Schema-Eingriff, wenn P-A3 nur numerische WA-Nummern zeigt.** Sonst: SQL/152 mit neuer Spalte `Auftragsnummer NVARCHAR(100) NULL` an `Beschichtungsauftraege` (Rueckfrage 9). Die Etappe wartet dann auf diese Antwort.

## Migrations-/SQL-Auswirkungen

- **E1-E4: keine Schema-Aenderung**, keine EF-Migration.
  - Die neuen AppSettings werden per Startup-Seed aus `AppSettingDefinitions.All` angelegt (Seed true, Default `Automatisch`).
  - Kein SQL-Skript, keine `SQL/00_FreshInstall.sql`-Aenderung; der FreshInstall-Seed laeuft ueber denselben Startup-Seed.
- **E5: reserviert SQL/152**, nur falls Rueckfrage 9 die Spalte `Auftragsnummer` verlangt. Dann:
  1. Model
  2. `dotnet ef migrations add AddBeschichtungsauftragAuftragsnummer`
  3. `SQL/152_AddBeschichtungsauftragAuftragsnummer.sql` mit Guard `IF COL_LENGTH('dbo.Beschichtungsauftraege','Auftragsnummer') IS NULL` (DDL eigener Batch)
  4. `__EFMigrationsHistory`-Insert im separaten Batch
  5. FreshInstall an **beiden** Stellen
  6. Nicht daten-destruktiv.
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
| `BeschichtungTerminQuelle` (neu, Anzeige „Quelle Beschichtungstermin“) | Choice (AppSettings, gleiche Gruppe) | `Automatisch`, `Berechnet` („Kommissioniertermin minus Beschichtungstage, auf Abholtag“), `View` („Sage-View Start_Beschichtung am Kopf“) | `Automatisch` | Automatisch: hierarchisch = View, flach = Berechnet. `View` nur bei Master an (bis P-A5). | Web: FA-Liste, Kommissionier-Leitstand, Werkbank-Arbeitsliste, Beschichtungsauftrag-Kopf (AKE) | beide | – (nur Anzeige) |
| `LackierteilKategorieName` (geaendert: Beschreibung) | MasterData (unveraendert) | Artikelkategorie | leer (unveraendert) | Zusatz: „wirkt nur bei Kennzeichen-Quelle Artikelkategorie“; `AbhaengigVon` = `BeschichtungKennzeichenQuelle` | wie heute | beide | – |
| `BeschichtungTage`, `BeschichtungAbholtage` (geaendert: Beschreibung) | unveraendert | unveraendert | unveraendert | Zusatz: „wirkt nur bei Termin-Quelle Berechnet“ | wie heute | beide | – |
| `FaHierarchyBeschichtungAktiv` (geaendert in E5) | Bool (unveraendert) | true/false | false | Schaltet das Modul Beschichtungsauftrag frei; Datenbasis je Kennzeichen-Quelle | Menue, `BeschichtungsauftraegeController` | **beide** (bisher IDEAL); Gruppe „Termine und Beschichtung“, Anzeige „Beschichtungsauftrag aktiv“ | – |

- Die Server-Validierung der Kombination ist eine neue Regel beim Speichern in `/Settings` und braucht den Master-Wert. Muster: Abhaengigkeits-Pruefung vorhandener Choice-Keys.
- Die Standort-Maske bleibt aussen vor (Rueckfrage 14).

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
6. IDEAL mit `Automatisch`: In der FA-Liste zeigt die Zeile mit `HasCoatingParts = true` `Start_Beschichtung` des eindeutigen Kopfes. Kombigeraet und leeres Feld zeigen keinen Termin und keinen Ersatztermin (TS-71.11 unveraendert gruen).
7. IDEAL (E4): Leitstand und Arbeitsliste zeigen fuer denselben Sub-FA denselben Termin wie die FA-Liste.
8. IDEAL mit `BeschichtungTerminQuelle = Berechnet`: Die FA-Liste zeigt den berechneten Termin auf Basis des Kopf-`FE_Termin`, der Tooltip nennt „berechnet“.
9. Kein Controller und kein View liest `LackierteilKategorieName`, `BeschichtungTage`, `BeschichtungAbholtage` oder `Start_Beschichtung` fuer die Terminanzeige direkt. Waechter-Test per Quelltext-Suche nach Muster der bestehenden Coverage-Tests; einzige Fundstelle ist der Resolver.
10. Dienst: Bei effektiver Quelle `FaStruktur` schreibt `CoatingDetectionService` nichts, und das Protokoll meldet „uebersprungen: Kennzeichen-Quelle FA-Struktur“. Bei `Artikelkategorie` ruft die Materialisierung `SetCoatingPartsAsync` nicht auf.
11. Der Sage-Import-Hook ruft die Lackierteil-Erkennung nicht auf, wenn `Sync:CoatingDetectionEnabled = false` (Rueckfrage 12).
12. `SetCoatingPartsAsync` setzt `ModifiedBy = "FaMaterialization"` und `ModifiedByWindows`; der Reset von `IsCoatingDone` bleibt erhalten (TS-71.10 gruen).
13. Aktiv-Kennung IDEAL: Mit Quelle `FaStruktur` erscheinen Beschichtungs-Haken nur an Zeilen mit `HasCoatingParts = true`, unabhaengig von `LackierteilKategorieName`.
14. AKE (E5): Mit `FaHierarchyBeschichtungAktiv = true` und Rolle `beschichtungsauftrag_schreiben` laesst sich fuer einen WA mit Lackierteilen ein Beschichtungsauftrag anlegen. Die Positionen entsprechen den Stuecklistenzeilen der Lackierteil-Kategorie. Ein WA ohne Lackierteile ergibt den bestehenden Hinweis „keine beschichteten Positionen“.
15. AKE (E5): Ohne Rolle oder mit Gate aus gilt dasselbe Verhalten wie heute (Redirect plus WarningMessage, API 404). Die Vorschlagsliste ist an der AKE nicht erreichbar.
16. IDEAL (E5): Der Beschichtungsauftrag verhaelt sich unveraendert (bestehende Tests zu Teil 3/4 gruen).
17. Build und alle Tests gruen; je Standort × Quellwert mindestens ein Resolver-Test (Matrix unten).

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
| an | – | View, Kombigeraet | null |
| an | – | View, Feld leer | null |
| aus | – | Berechnet | identisch zur alten Inline-Formel |

Weitere automatisierte Tests:

- Aktiv je Quelle.
- `AppSettingDefinitionsTests` (Choice-Werte, Pflichtangaben).
- Gegen-Tests fuer die Dienst-Gates (`CoatingDetectionService` uebersprungen, Hook-Gate).
- `SetCoatingPartsAsync`-Audit.
- E5: Workflow-Anlage aus der flachen Basis (InMemory), IDEAL-Bestandstests unveraendert.
- `NachlaufManifestDriftTests`.

**Manuelle Szenarien fuer `docs/TESTSZENARIEN.md`** (Pflicht des Dev-Laufs; Nummern ab TS-140, TS-139 ist reserviert):

- **TS-140 — Einstellungen Beschichtungsquelle**
  - Auswahlliste, Texte, Default `Automatisch` je Standort.
  - Ablehnung ungueltiger Kombinationen.
  - Negativfall: per SQL gesetzter Fremdwert → Protokoll-Warnung, Verhalten wie Automatisch.
- **TS-141 — AKE Regression Termin**
  - Vorbedingung: Lackier-FA im Fenster.
  - FA-Liste, Leitstand und Arbeitsliste zeigen denselben Termin wie vor dem Update; Spaltenfilter `coating-date` funktioniert.
  - Negativfall: `LackierteilKategorieName` leer → Termin fuer alle (fallstricke `:207-211`).
- **TS-142 — IDEAL Termin aus der View in allen drei Listen**
  - Ein eindeutiger Kopf mit `Start_Beschichtung`.
  - Negativfaelle: Kombigeraet, leeres Feld mit gesetztem `FE_Termin` (kein Ersatztermin, wie TS-71.11b).
- **TS-143 — IDEAL Termin-Quelle Berechnet**
  - Umschalten, Tooltip, Termin aus dem Kopf-`FE_Termin`.
  - Zurueckschalten auf Automatisch.
- **TS-144 — Kennzeichen-Quelle und Dienst**
  - Protokoll „uebersprungen“ je Weg.
  - Sage-Import-Hook bei `CoatingDetectionEnabled = false`.
  - Audit `ModifiedBy` in `ProductionOrderPickingStatus` nach einem Materialisierungslauf.
- **TS-145 — Beschichtungsauftrag an der AKE**
  - Anlage, Positionen, Kopf-Vorbelegung Start.
  - Rollen-Negativfaelle, Gate aus, Vorschlagsliste nicht erreichbar.
  - IDEAL-Gegenprobe.

Danach `secondbrain/tests/testszenarien-index.md` nachziehen.

## Etappen (nur bei epic: true)

**Epic statt Split:**

- Alle Etappen teilen einen Kern, den Resolver und die beiden Keys. Einzeln gemergt, laege nach E1 ruhender Code ohne Verbraucher in `main`.
- Die Arbeit laeuft im vorhandenen Buendel-Worktree `feature/2026-08-07-ideal-teile-1-5` (ein Merge am Ende).
- Den Branch waehrend der Arbeit mit `scripts/sync-worktree.ps1 -Slug 2026-08-07-ideal-teile-1-5` auf main-Stand halten.
- Jede Etappe baut und testet fuer sich gruen und endet mit einem Commit. Keine Etappe wartet auf eine offene Frage: Wo eine Antwort fehlt, baut der `/goal`-Lauf die empfohlene Antwort und vermerkt das, oder er haelt die Etappe als Blocker fest (E5 bei Rueckfrage 9).

| # | Etappe | Inhalt | Abhaengig von | SQL | Status | Commit |
|---|--------|--------|---------------|-----|--------|--------|
| 1 | Katalog + Resolver | 2 Choice-Keys, Beschreibungsergaenzungen, Server-Validierung (A8), `BeschichtungResolver` plus Matrix-Tests; noch kein Verbraucher umgestellt | Rueckfragen 1, 2 (Empfehlung baubar) | – | offen | |
| 2 | Web-Verbraucher | FA-Liste (Inline-Formel weg), Leitstand, Arbeitsliste, Zeilen-Views, Tooltips auf den Resolver; Waechter-Test AK 9; Verhalten mit Automatisch identisch | E1; Rueckfrage 7 | – | offen | |
| 3 | Dienst | Gates ueber `ResolveQuellen`, Hook-Gate, Audit `SetCoatingPartsAsync`, Protokoll-Meldungen | E1; Rueckfrage 12 | – | offen | |
| 4 | IDEAL-Termin Leitstand/Arbeitsliste | Koepfe je Seite nachladen, Termin aus dem Resolver; Basisdatum Berechnet = Kopf-`FE_Termin` | E2; Rueckfragen 3, 5, 6 | – | offen | |
| 5 | Beschichtungsauftrag AKE | Gate-Metadaten (beide), Vorschlagsliste Master-Gate, flache Positionsquelle, Workflow-Weiche, AKE-Kopfvorbelegung | E1, E3; Rueckfragen 8, 9, 10, 11; **P-A3** | 152 (nur bei Rueckfrage 9 = Spalte) | offen | |
| 6 | Doku und Abschluss | TS-140..145 in `docs/TESTSZENARIEN.md`, Hilfe (`Help/Index.cshtml` Beschichtung, `:1265`), Anwender-Changelog, `docs/betrieb/04-konfiguration.md`, `README.md` AppSettings, Manifest-Eintraege, Versions-Bump beide `AppVersion.cs`, Brain (ADR 0028, fallstricke, codebase, glossar, feature-map, changelog) | E1-E5 | – | offen | |

## Deploy

*Vorlaeufig, der QA-Lauf bestaetigt gegen den echten Diff.*

- **Web-App:** ja (Settings-Katalog, Resolver, Controller, Views, Beschichtungsauftrag)
- **Service:** ja (Gates, Hook, Audit). Web und Dienst gemeinsam deployen, weil beide den Katalog teilen.
- **Migration:** nein. Ausnahme: E5 mit SQL/152, falls Rueckfrage 9 die Spalte verlangt. Dann Schema-Phase vor dem Start, nicht destruktiv.
- **Manifest-Eintraege** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023; Version v1.99.25 als Annahme):

| Id | Version | Typ | Firma | Phase | Ausfuehrung | Text / Pruefung |
|---|---|---|---|---|---|---|
| `Pruefung/BeschichtungQuelleKombination` | 1.99.25 | Pruefung | beide | Haltepunkt | anzeigen | Pruefung (Zeilenzahl > 0 = Befund, siehe unten). Text: „Beschichtungsquelle passt nicht zum Auftragsmodell, wirkt wie Automatisch; unter /Settings korrigieren.“ |
| `v1.99.25-Beschichtungsquelle-Info` | 1.99.25 | Info | beide | Nachher | anzeigen | „Neue Einstellungen Quelle Beschichtet-Kennzeichen / Quelle Beschichtungstermin stehen auf Automatisch, das Verhalten ist unveraendert. IDEAL: Leitstand und Werkbank-Arbeitsliste zeigen jetzt den Beschichtungstermin aus der Sage-View.“ |
| `v1.99.25-Beschichtungsauftrag-AKE` | 1.99.25 | Einstellung | AKE | Nachher | anzeigen | „Optional: Beschichtungsauftrag aktiv (FaHierarchyBeschichtungAktiv) unter /Settings einschalten, Rollen beschichtungsauftrag / beschichtungsauftrag_schreiben zuweisen (Anlage: Pruefung/RollenBeschichtungsauftrag).“ |
| `SQL/152` (nur falls Rueckfrage 9 = Spalte) | 1.99.25 | Schema | beide | Schema | automatisch | `152_AddBeschichtungsauftragAuftragsnummer.sql` |

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

1. Ein Key oder zwei Keys →
2. Kombinationen serverseitig ablehnen →
3. IDEAL-Termin auch in Leitstand/Arbeitsliste →
4. Start_Beschichtung = Beschichtungstermin →
5. Kombigeraet →
6. Berechnet an der IDEAL mit Kopf-FE_Termin →
7. Aktiv-Kennung IDEAL immer aktiv →
8. AKE-Beschichtungsauftrag Positionen/Kopf →
9. AKE-WA-Nummern numerisch (P-A3) →
10. Vorschlagsliste an der AKE →
11. Gate-Key umbenennen →
12. Sage-Import-Hook an CoatingDetectionEnabled →
13. Erkennungsfenster-Filter →
14. Standort-Maske →
15. AKE-View-Datum (P-A5) →
16. Version v1.99.25 →
