---
type: spec
title: "Matchcode im Artikelstamm (Article.Matchcode) — hausweit suchbar"
slug: 2026-09-13-matchcode-artikelstamm-spec
status: Entwurf
created: 2026-09-15
updated: 2026-09-15
source_backlog: "[[2026-09-13-matchcode-artikelstamm-kommissionierung-hauptfa]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Models/Article.cs (neue Spalte `Matchcode` (string?, `[StringLength(200)]`), analog `Description`/`ArticleNumber`)"
  - "IdealAkeWms/Models/ProductionOrder.cs (Spalte `Matchcode` WIEDER ENTFERNEN — Zeilen 43-45 im Worktree-Stand, `[StringLength(200)] [Display(Name=\"Matchcode\")] public string? Matchcode`)"
  - "IdealAkeWms/Migrations/20260910081155_AddProductionOrderMatchcode.cs + .Designer.cs (ENTFERNEN, bevorzugt via `dotnet ef migrations remove --project IdealAkeWms`, sofern zum Umsetzungszeitpunkt noch die letzte Migration) + ApplicationDbContextModelSnapshot.cs (Matchcode-Property dort verschwindet automatisch mit der Remove-Operation)"
  - "SQL/91_AddProductionOrderMatchcode.sql (ENTFERNEN, nie deployt) → NEUE Datei SQL/91_AddArticleMatchcode.sql (COL_LENGTH-Guard auf `dbo.Articles`, siehe Migrations-Abschnitt) — Nummer 91 wird durch den Rückbau wieder frei"
  - "SQL/00_FreshInstall.sql (ZWEI Korrekturen: 1. `ProductionOrders`-CREATE TABLE-Block, Zeile ~269 `[Matchcode] NVARCHAR(200) NULL` entfernen; 2. `Articles`-CREATE-TABLE-Block um `[Matchcode] NVARCHAR(200) NULL` ergänzen; 3. `__EFMigrationsHistory`-Insert-Block: Zeile `20260910081155_AddProductionOrderMatchcode` entfernen, neue MigrationId für `AddArticleMatchcode` einfügen)"
  - "IDEALAKEWMSService/Services/FaMaterializationSyncService.cs (Zeilen ~176 `Matchcode = n.Matchcode,` im Anlege-Zweig UND ~200 `o.Matchcode = sn.Matchcode;` im Update-Zweig ENTFERNEN — `ProductionOrder.Matchcode` existiert nach dem Rückbau nicht mehr)"
  - "IDEALAKEWMSService/Services/FaMaterializationPlanner.cs (`MaterializationSourceOrder`-Record: `Matchcode`-Property entfernen, falls dort separat deklariert — am Diff zu `461d3e5`/`cbabdd9` zu prüfen)"
  - "IDEALAKEWMSService/Services/SageImportService.cs (`SyncArticlesAsync`, Zeilen 382-421: `sageSql` UNION-Query um eine Matchcode-Spalte aus `KHKArtikel` erweitern, SOBALD Rückfrage 4 geklärt ist — realer Sage-Spaltenname unbekannt, siehe Offene Rückfrage 4; Upsert-SQL Zeilen 480-515: `[Matchcode]` in INSERT- und UPDATE-Zweig sowie in die `ISNULL(...)!=ISNULL(...)`-Änderungserkennung Zeilen 494-501 aufnehmen — sonst wird eine reine Matchcode-Änderung nicht erkannt)"
  - "IdealAkeWms/Data/Repositories/ArticleRepository.cs (`SearchAsync` Zeilen 22-23, `GetPaginatedAsync`-Freitext Zeilen 40-41, `ApplyArticleColumnFilter` Zeilen 62-85 — je um `a.Matchcode` ergänzen; neuer `\"matchcode\"`-Case im Column-Filter-Switch, null-sicher wie `\"description\"`)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (Artikelstammliste-`ViewConfig` — genauer viewKey am Diff zu verifizieren — neue `ColumnDef(\"matchcode\", \"Matchcode\", Locked: false)` neben `article-number`/`description`, analog dem bereits bestehenden Muster in den 5 FA-Zeilen-`ViewConfig`s)"
  - "IdealAkeWms/Views/Articles/Index.cshtml + Info.cshtml + Edit.cshtml (`Matchcode`-Spalte/-Feld analog `Description`; `#column-config` + `<th data-col-key=\"matchcode\">` in Index.cshtml — Fallstricke §3-Pflicht)"
  - "IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs (`ProjectLeitstandRows`, Zeile 178 `o.Matchcode` → korrelierte Subquery/Join auf `Article` per `ArticleNumber`, z. B. `_context.Set<Article>().Where(a => a.ArticleNumber == o.ArticleNumber).Select(a => a.Matchcode).FirstOrDefault()`; `ApplyLeitstandColumnFilter` `\"matchcode\"`-Case Zeile 308 auf denselben berechneten Ausdruck umstellen, `BuildExtraInfoOrContains`-Selector entsprechend anpassen oder durch einen neuen, äquivalenten Helper ersetzen; `SearchAsync` Zeile 195ff. auf dieselbe Quelle prüfen)"
  - "IdealAkeWms/Controllers/PickingController.cs (Zeile 84 Client-Filter-Dictionary + Zeile 159 `Matchcode = o.Matchcode` in der Projektion — auf denselben Article-Join umstellen; `PickingListViewModel`-Feld bleibt bestehen, nur die Quelle wechselt)"
  - "IdealAkeWms/Controllers/FaCompletionController.cs (Zeile ~91 `o.ArticleNumber`-Freitextfilter um denselben Article-Matchcode-Ausdruck als ODER-Bedingung erweitern)"
  - "IdealAkeWms/Data/Repositories/StockMovementRepository.cs (Zeilen 29-30, 89-90, 322-323 Freitext + 558-561 Spaltenfilter: `sm.Article.ArticleNumber`/`sm.Article.Description` → zusätzlich `sm.Article.Matchcode`, Article ist hier bereits per Navigation geladen, keine neue Query-Struktur nötig)"
  - "IdealAkeWms/Data/Repositories/PartRequisitionRepository.cs (Zeilen 80-81: `r.ArticleNumber`/`r.ArticleDescription` sind Snapshot-Felder auf `PartRequisition`, KEIN Live-Join zu `Article` — Matchcode-Suche braucht einen zusätzlichen Join/Subquery auf `Article.ArticleNumber`, kein neues Snapshot-Feld)"
  - "IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs (Zeilen 400-462, `ApplyMissingPartsTextFilter`: `WarehouseRequisitionItem.ArticleNumber`/`ArticleDescription` sind ebenfalls Snapshot-Felder — gleiches Muster wie `PartRequisitionRepository`, Join/Subquery auf `Article.ArticleNumber`)"
  - "IdealAkeWms/Controllers/ArticlesController.cs (`Info`-Action, Zeile 218-228 — Verhalten je nach Antwort auf Rückfrage 2 anzupassen: aktuell ausschliesslich exakter `GetByArticleNumberAsync`-Treffer)"
  - "IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs, IDEALAKEWMSService.Tests/Services/FaMaterializationSyncServiceTests.cs (bestehende `ColumnFilter_Matchcode_*`/Materialisierungs-Tests auf die neue Quelle umstellen, neue Tests für `ArticleRepository`/`ProductionOrderRepository` mit gejointem Matchcode ergänzen)"
  - "docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md"
open_questions:
  - "Article.Matchcode als eigene Spalte oder Ersatz für Article.Description? (Backlog-Frage 4)"
  - "QR-Scan in der Artikelinfo: kann der gescannte Code auch ein Matchcode sein, welche Vorrang-Reihenfolge? (Backlog-Frage 4b)"
  - "Verhältnis Article.Matchcode (neu, artikelbezogen) zu FaHierarchyNode.Matchcode (bestehend, knoten-/auftragsbezogen) — gilt die neue Spalte für beide Standorte gleich? (Backlog-Frage 2)"
  - "Liefert die AKE-Sage-Quelle (SageImportService.SyncArticlesAsync, KHKArtikel) den Matchcode bereits unter einem bekannten Spaltennamen? (Backlog-Frage 1)"
  - "LIKE-Latenzmessung auf den ~108.818 Article-Zeilen noch nicht durchgeführt (Production-Reads-Schutz) — vor Umsetzungsende nachzuholen"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> **Scope-Grenze (hart):** Diese Spec behandelt ausschliesslich den Matchcode. Das ursprünglich in
> derselben Backlog-Notiz mitgeführte Thema „Kommissionierung nur auf HauptFA" ist ausgelagert nach
> [[2026-09-13-kommissionierung-nur-hauptfa]] und **nicht** Bestandteil dieser Spec — eigener Takt,
> eigene Spec, eigene Freigabe.

> **Verhältnis zu [[2026-09-10-fa-liste-ausbau-matchcode-spec]] (Status `Testbereit`, im selben
> Worktree bereits UMGESETZT):** Jene Spec hat `ProductionOrder.Matchcode` bereits gebaut — Modell,
> Migration `91`, Materialisierung, Anzeige in 5 FA-Zeilen-Listen (`ProductionOrders`,
> `PickingLeitstand`, `FaCompletion`, `FaWorklist`, `Picking`), Spaltenfilter (`F7`-Regel, keine
> `EF.Functions.Like`). **Diese Spec widerruft ausdrücklich den Ort der Spalte, NICHT die
> Anzeige/Suchbarkeits-Funktion selbst.** Die 5 Listen behalten ihre Matchcode-Spalte und ihren
> Matchcode-Spaltenfilter — nur die Datenquelle wechselt von `ProductionOrder.Matchcode` auf
> `Article.Matchcode` (per Join über `ArticleNumber`). Die Spec vom 2026-09-10 bleibt als Dokument
> unverändert stehen (Protokollcharakter); der hier beschriebene Rückbau ist Teil **dieser** Spec.
>
> **Wichtiger Recherchebefund (dieser Lauf, 2026-09-15, am Worktree-Code verifiziert):** Die
> `ProductionOrder.Matchcode`-Migration (`20260910081155_AddProductionOrderMatchcode`,
> `SQL/91_AddProductionOrderMatchcode.sql`) ist **nie gemergt und nie deployt** — der Worktree
> wartet weiterhin auf Schranke 2 fürs gesamte Bündel. Der Rückbau ist damit ein **sauberer
> Rückbau im Worktree selbst** (Migration entfernen, nicht per Gegen-Migration abbauen) — es gibt
> keine Produktions-Zeile, die je `Matchcode` auf `ProductionOrders` getragen hat.

## Ziel / Nutzen (das Warum)

Der Matchcode (Typenkurzbezeichnung, Beispiel `G-KT-FRR-760-1500--1320B-180-V01`) ist am
2026-09-13 als **artikelbezogenes** Merkmal bestätigt: für denselben Artikel ist er in jedem
Auftrag gleich. Er gehört damit fachlich an `Article`, nicht an `ProductionOrder` — nur am
Artikelstamm wird er in JEDER Suche sichtbar, die heute Artikelnummer oder Bezeichnung findet
(Artikelinfo, Bestandsansichten, Fehlteile, Bestellungen), nicht nur in der FA-Liste. Eine Kopie je
`ProductionOrder` wäre für eine reine Artikelsuche unsichtbar.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:**

1. Neue Spalte `Article.Matchcode` (string?, `NVARCHAR(200)`), analog `ArticleNumber`/`Description`.
2. Rückbau der bereits gebauten `ProductionOrder.Matchcode`-Spalte (Migration, Materialisierung) —
   die 5 FA-Zeilen-Listen behalten ihre Matchcode-Spalte/-Filter, aber gespeist aus `Article` per
   Join über `ArticleNumber` statt aus einer eigenen materialisierten Kopie.
3. Erhebung + Umsetzung: Matchcode-Suchbarkeit in allen Fundstellen, die heute Artikelnummer oder
   Bezeichnung durchsuchen (Erhebung (a) unten) — Artikelstammliste, Artikelinfo (Anzeige),
   Bestandsübersicht/Bewegungshistorie, Fehlteile, Lager-/Glasbestellung, Bedarfsmeldungen,
   Artikel-Suchfelder (Typeahead).
4. Erweiterung des AKE-Artikel-Sync (`SageImportService.SyncArticlesAsync`) um den Matchcode,
   sobald Rückfrage 4 geklärt ist; bis dahin bleibt die Spalte bei AKE leer, null-sicherer
   Lesepfad Pflicht (kein Fehler, kein stiller Sonderfall).
5. Anzeige des Matchcodes in `Views/Articles/Index.cshtml`/`Info.cshtml`/`Edit.cshtml`.

**Out-of-Scope:**

- **„Kommissionierung nur auf HauptFA"** — eigene Spec [[2026-09-13-kommissionierung-nur-hauptfa]].
- **OSEON Teileverfolgung** (`OseonProductionOrder`/`OseonGroupViewModelBuilder`) — eigene
  Datenpipeline ohne bestehenden Article-Join (siehe Erhebung (a)); Folge-Arbeit, eigener
  Backlog-Punkt (analog zur BOM-Komponentenebene, die die Schwester-Spec bereits ausgelagert hat).
- **BOM-Komponentenebene** (`Picking/Bom.cshtml`, `PrintBom`, `PrintPicking`,
  `WarehousePicking/Details`) — bereits von der Schwester-Spec (2026-09-10, Antwort 7) bewusst
  ausgeschlossen; diese Spec ändert daran nichts.
- **Freigabe-Kaskade, HauptFA-als-Zeile, Kopfdaten-je-Zeile** — Bausteine der Schwester-Spec, von
  dieser Spec nicht berührt.

## Fachliche Anforderungen

- `Article.Matchcode` ist **eigene Spalte** neben `Description` (Empfehlung zu Rückfrage 1) — sie
  ersetzt kein bestehendes Feld, da Bezeichnung (Freitext-Artikelname) und Matchcode (strukturierter
  Typenschlüssel) fachlich verschiedene Dinge sind und in der IDEAL-Sicht (`FaHierarchyNode`) bereits
  heute nebeneinander geführt werden (`Bezeichnung1`/`Bezeichnung2` UND `Matchcode` als getrennte
  Spalten der Sage-View, siehe `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql` Zeilen
  29-31).
- **Teilstring-Suche (`Contains`/`LIKE '%...%'`), nicht Exakttreffer** — wie bei Artikelnummer/
  Bezeichnung heute schon. Kein neuer Suchmodus, dieselbe Mini-Syntax (`ColumnFilterHelper`).
- **Null-sicherer Lesepfad überall:** Ein leerer Matchcode (AKE, bis die Sync-Erweiterung greift)
  liefert bei einer Matchcode-Suche korrekt **kein Treffer**, nie eine Exception, nie eine leere
  Gesamtliste ohne erkennbaren Grund (F7-Regel, unverändert aus der Schwester-Spec übernommen).
- **Melden statt still behandeln:** Wo eine Fundstelle NICHT erweitert wird (OSEON, BOM-Komponenten),
  steht das explizit in Out-of-Scope mit Begründung — keine stillschweigend vergessene Stelle.
- **Audit-Felder:** jede Schreiboperation auf `Article` (Sync UND manuelle Bearbeitung über
  `/Articles/Edit`) setzt weiterhin `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` — unverändert,
  nur ein zusätzliches Feld in der bestehenden Zuweisung.

## Erhebung (a) — Suchen auf Artikelnummer/Bezeichnung

Verifiziert am Code im Worktree `2026-08-07-ideal-teile-1-5` (Grep auf `ArticleNumber`/
`Description` in Kombination mit `Contains`/`EF.Functions.Like`). Über die im Backlog genannten
Kandidaten hinaus zusätzlich gefunden: `PartRequisitionRepository` (Bedarfsmeldungen) und
`FaCompletionController` (eigener Freitextfilter neben der Spaltenfilter-Erhebung der
Schwester-Spec).

| Fundstelle (Datei:Zeile) | Heute durchsuchte Spalten | Matchcode ergänzen? |
|---|---|---|
| `ArticleRepository.SearchAsync` (ArticleRepository.cs:22-23) — Typeahead über `ArticlesApiController`, u. a. Lager-/Glasbestellung, Bedarfsmeldung | `ArticleNumber`, `Description` | **Ja** — reiner Freitext, gleicher Ort wie Bezeichnung |
| `ArticleRepository.GetPaginatedAsync` (ArticleRepository.cs:40-41) — Artikelstammliste-Freitextsuche | `ArticleNumber`, `Description` | **Ja** |
| `ArticleRepository.ApplyArticleColumnFilter` (ArticleRepository.cs:62-85) — Artikelstammliste-Spaltenfilter | `article-number`, `description`, `unit`, `article-group`, `category` | **Ja** — eigener `"matchcode"`-Case, analog `"description"` |
| `ArticlesController.Info` (ArticlesController.cs:218-228) — QR-Scan/Direktaufruf | `ArticleNumber` (exakt) | **Rückfrage 2** — heute ausschliesslich Exakttreffer auf ArticleNumber, keine Ambiguitäts-Auflösung vorhanden |
| `StockMovementRepository` Freitext (StockMovementRepository.cs:29-30, 89-90, 322-323) — StockOverview + Bewegungshistorie | `sm.Article.ArticleNumber`, `sm.Article.Description` (bereits über Navigation `Article` geladen) | **Ja** — trivial, `Article` schon im Join |
| `StockMovementRepository` Spaltenfilter (StockMovementRepository.cs:558-561) | dieselben zwei Spalten | **Ja** |
| `PartRequisitionRepository` (PartRequisitionRepository.cs:80-81) — Bedarfsmeldungen-Suche | `ArticleNumber`, `ArticleDescription` (**Snapshot-Felder auf `PartRequisition`**, kein Live-Join) | **Ja** — braucht zusätzlichen Join/Subquery auf `Article.ArticleNumber`, kein neues Snapshot-Feld; nicht im Backlog namentlich genannt, aber dieselbe Anforderung |
| `WarehouseRequisitionRepository.ApplyMissingPartsTextFilter` (WarehouseRequisitionRepository.cs:400-462) — Fehlteile (`MissingParts`/`MissingPartsLager`) + Lager-/Glasbestellungs-Item-Ansichten | `WarehouseRequisitionItem.ArticleNumber`/`ArticleDescription` (**ebenfalls Snapshot**, kein Live-Join) | **Ja** — gleiches Muster wie PartRequisition |
| `FaCompletionController` Freitextfilter (FaCompletionController.cs:~88-92) | `ProductionOrder.ArticleNumber` | **Ja** — gehört zur selben FA-Zeilen-Ebene wie die 5 bereits gebauten Matchcode-Listen |
| `ProductionOrderRepository`/`PickingController` (5 FA-Zeilen-Listen, s. Rückbau-Abschnitt) | bereits `Matchcode`-fähig (Schwester-Spec) | **Bleibt Ja** — nur Datenquelle wechselt (Rückbau) |
| `OseonProductionOrderRepository`/`OseonGroupViewModelBuilder` (OseonProductionOrderRepository.cs:74-90, 133-134, 165-186) — OSEON Teileverfolgung | `OseonProductionOrder.ArticleNumber`/`Description1`/`Description2` (eigene Datenpipeline, **kein** bestehender Join zu `Article`) | **Nein, out of scope** — eigene Sync-Pipeline, ein Join wäre eine zusätzliche, ungeprüfte Kopplung; als Folge-Arbeit vermerken |
| `Views/Picking/Bom.cshtml`, `PrintBom.cshtml`, `PrintPicking.cshtml`, `WarehousePicking/Details.cshtml` — BOM-Komponentenebene | `Description1`/`Description2`/`description` auf `BomItem`/`FaHierarchyBomItem` | **Nein, out of scope** — von der Schwester-Spec (Antwort 7) bereits entschieden ausgeschlossen |

**Fazit:** Acht Fundstellen werden erweitert (davon fünf bereits von der Schwester-Spec vorbereitet,
drei neu: Artikelstammliste, Bestandsansichten, Bedarfsmeldungen/Fehlteile/Bestellungen). Zwei
bleiben bewusst aussen vor (OSEON, BOM-Komponentenebene) mit dokumentierter Begründung. Eine
Fundstelle (QR-Scan) hängt an einer offenen Rückfrage.

## Erhebung (b) — Größe des Artikelstamms + LIKE-Kosten (gemessen)

Am 2026-09-13 gegen die Produktions-DB `IDEAL_AKE_WMS` (`AKESQL20.ake.at`) gemessen:

- **`Articles`-Zeilen: 108.818** — widerlegt die Backlog-Annahme „mehrere tausend Zeilen" deutlich.
- `ArticleNumber` NVARCHAR(100), `Description` NVARCHAR(500) — deckt sich mit den Model-Attributen
  (`[StringLength(100)]`/`[StringLength(500)]`). (Klarstellung: die gemessenen `sys.columns.max_length`-
  Werte 200 bzw. 1000 sind **Byte**-Längen; NVARCHAR belegt 2 Byte je Zeichen, also 100 bzw. 500
  Zeichen — **keine** Diskrepanz zwischen Attribut und Spalte.) Für `Matchcode` ist damit
  `[StringLength(200)]` → `NVARCHAR(200)` eine bewusste, großzügige Wahl (Beispielwert ~30 Zeichen).
- Indizes: PK (`Id`, clustered), UQ auf `ArticleNumber`, NC auf `ArticleCategoryId`, NC auf
  `PrimaryStorageLocationId`. **Kein Index auf `Description`.** Eine `LIKE '%...%'`-Suche mit
  führender Wildcard kann keinen dieser Indizes nutzen → Full Scan über ~109k Zeilen.
- **Wichtige Randbeobachtung:** Dieses Full-Scan-Risiko ist **nicht neu** — die heutige
  `ArticleNumber`/`Description`-Freitextsuche (`ArticleRepository.SearchAsync`/`GetPaginatedAsync`)
  läuft schon jetzt, im Produktivbetrieb, als Full Scan über dieselben ~109k Zeilen (kein Index auf
  `Description`, `ArticleNumber` hilft bei `Contains` mit führendem `%` ebenfalls nicht). SQL Server
  liest bei einem Scan JEDE Zeile ohnehin einmal und wertet dabei alle ODER-verknüpften Prädikate in
  einem Durchgang aus — eine dritte `OR`-Bedingung (`Matchcode`) erzeugt **keinen zusätzlichen
  Scan-Durchlauf**, nur zusätzliche CPU-Arbeit pro bereits gelesener Zeile. Das Risiko ist also
  strukturell dasselbe wie heute, nicht ein neues.
- **Exakte Latenzmessung des bestehenden `LIKE '%...%'`-Scans steht noch aus** (Production-Reads-
  Schutz hat die Messung blockiert) — offener Messschritt vor/bei Umsetzungsende (Offene Rückfrage 5).

**Empfohlene Option (Ponytail — einfachste tragfähige Lösung zuerst):** `Matchcode` als weiteres
`OR`-Glied in denselben bestehenden `Contains`/`LIKE`-Ausdrücken führen, **ohne** neue Infrastruktur
(kein Volltextindex, keine computed column). Begründung: Das Full-Scan-Verhalten existiert bereits
heute produktiv für zwei Spalten auf derselben Tabelle; eine dritte Spalte ändert die
Scan-Charakteristik nicht grundsätzlich. Ein Volltextindex oder eine persistente computed column
wäre zusätzliche Infrastruktur (SQL-Server-Feature-Aktivierung bzw. Schema-Erweiterung) für ein
Risiko, das noch nicht einmal am bestehenden Zwei-Spalten-Fall gemessen wurde — das wäre
Over-Engineering vor dem Befund. **Bedingung:** Die ausstehende Messung (Rückfrage 5) muss VOR
Abschluss des Dev-Laufs nachgeholt werden; zeigt sie ein echtes Problem (spürbare Latenz beim
bestehenden Zwei-Spalten-Scan), ist das ein Fund für eine eigene Folge-Aufgabe (Indexierung), nicht
für einen Vorgriff in dieser Spec.

## AM CODE VERIFIZIERT — Artikel-Sync aus Sage

`IDEALAKEWMSService/Services/SageImportService.SyncArticlesAsync` (Zeilen 369-421) liest Artikel aus
der `SageConnection` (AKE-Instanz, `Database=ake`) über eine `UNION`-Query gegen
`KHKPpsRessourcenPositionen`, `KHKArtikel`, `KHKArtikelvarianten`, `KHKLagerplaetze`. Die
selektierten Spalten sind `ArticleNumber` (`Ressourcenummer`/`Artikelnummer`), `Description`
(`Bezeichnung1`), `Unit` (`Lagermengeneinheit`), `ArticleGroup` (`Artikelgruppe`), `ReorderLevel`
(`Meldebestand`), `PrimaryStorageLocation` (`Kurzbezeichnung`). **Kein Matchcode-Feld wird
selektiert.**

Zum Vergleich: Die IDEAL-seitige Sage-View `vw_IDEAL-AKE_Kommissionierung_FAListe` (eigene,
IDEAL-eigene Sage-Instanz, dokumentiert in `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql`
Zeile 29) liefert einen Matchcode bereits — laut Dokumentationskopf „Position bevorzugt, KHKArtikel
als Fallback". Das bestätigt, dass **ein Matchcode-Konzept in `KHKArtikel` (Sage-Stammdaten)
existiert** — vermutlich ein Standard-Sage-Feld gleichen Namens (`Matchcode` ist ein gängiger
ERP-Fachbegriff). Der **exakte Spaltenname in der AKE-Instanz von `KHKArtikel`** ist am Code jedoch
nicht verifizierbar (die CREATE-VIEW-/Tabellendefinition liegt in der Sage-DB, nicht im Repo) und
eine Live-Abfrage der Produktions-Sage-DB war im Rahmen dieses Spec-Laufs nicht vorgesehen.
**→ Offene Rückfrage 4.**

Liefert die Quelle den Matchcode nicht direkt unter einem bekannten Namen, ist die Erweiterung der
`sageSql`-Query um `CAST(a.<Spaltenname> AS nvarchar(200)) AS Matchcode` (beide `UNION`-Zweige) eine
**eigene, hausinterne Aufgabe** — analog zur bereits an anderer Stelle zugesagten AKE-View-Erweiterung
für die FA-Liste. Bis dahin bleibt `Article.Matchcode` bei AKE **leer, kein Fehler** (F7-Regel).

## Rückbau der `ProductionOrder.Matchcode`-Spalte — Ist-Stand und Vorgehen

**Ist-Stand im Worktree, verifiziert (2026-09-15):**

- Modell: `ProductionOrder.cs` Zeilen 43-45, `[StringLength(200)] public string? Matchcode`.
- Migration `20260910081155_AddProductionOrderMatchcode` (.cs + .Designer.cs) + Eintrag in
  `ApplicationDbContextModelSnapshot.cs`.
- `SQL/91_AddProductionOrderMatchcode.sql` — additiv, `COL_LENGTH`-Guard, **nie deployt**.
- `SQL/00_FreshInstall.sql` — Spalte im `ProductionOrders`-CREATE-Block (Zeile 269) UND
  `__EFMigrationsHistory`-Insert (Zeile 2300-2301).
- Materialisierung: `FaMaterializationSyncService.RunAsync` setzt `Matchcode = n.Matchcode` beim
  Anlegen (Zeile 176) und `o.Matchcode = sn.Matchcode` beim Update (Zeile 200) — Quelle ist
  `FaHierarchyNode.Matchcode` (selbst aus der IDEAL-Sage-View, s. o.).
- Anzeige + Filter in **fünf** `ColumnDefinitions`-`ViewConfig`s (`ProductionOrders`,
  `PickingLeitstand`, `FaCompletion`, `FaWorklist`, `Picking`) — alle mit dem Kommentar
  „IDEAL-Matchcode (Task 3)", `Locked: false`, sichtbar per Default.
- Projektion: `ProductionOrderRepository.ProjectLeitstandRows` (Zeile 178, `LeitstandOrderRow`-Record)
  und separat `PickingController.Index` (Zeile 159, eigene Projektion, nicht über
  `LeitstandOrderRow`).
- Filter: `ProductionOrderRepository.ApplyLeitstandColumnFilter`, Case `"matchcode"` (Zeile 308),
  über `BuildExtraInfoOrContains(o => o.Matchcode, ...)` — bewusst **ohne** `EF.Functions.Like`
  (InMemory-Testbarkeits-Grund, siehe Kommentar dort).

**Abwägung (a)/(c) sauberer Rückbau vs. (b) denormalisierte Kopie:**

Der Backlog hat (a)/(c) faktisch entschieden. Der Code-Befund bestätigt das: Der Join
`ProductionOrder.ArticleNumber = Article.ArticleNumber` ist ein **Equi-Join über eine eindeutige,
indizierte Spalte** (`Article.ArticleNumber` trägt laut Erhebung (b) einen `UNIQUE`-Index) — technisch
grundverschieden vom **korrelierten Subquery-Join gegen `FaHierarchyOrderInfo`**, den die
Schwester-Spec wegen echter Performance-/Korrektheitsbedenken (mehrdeutige Kombigeräte-Köpfe, keine
eindeutige Spalte) durch einen C#-Postfilter ersetzt hat. Ein Index-Seek gegen ~130 paginierte
FA-Zeilen kostet nichts Nennenswertes. **Variante (b) (denormalisierte Kopie) wird verworfen:** Sie
würde exakt die Doppelpflege wiederherstellen, die der Backlog vermeiden wollte, ohne einen
gemessenen Performance-Grund dafür zu haben — Ponytail-Prinzip: keine Kopie ohne Beleg, dass der Join
zu teuer ist.

**Empfohlenes Vorgehen (sauberer Rückbau, da nie deployt):**

1. `dotnet ef migrations remove --project IdealAkeWms` — **vorausgesetzt**,
   `AddProductionOrderMatchcode` ist zum Umsetzungszeitpunkt noch die letzte Migration
   (`dotnet ef migrations list` vorab prüfen). Das entfernt Migration + Designer + bereinigt den
   Model-Snapshot in einem offiziellen, sauberen Schritt — kein Hand-Edit am Snapshot nötig.
2. `SQL/91_AddProductionOrderMatchcode.sql` löschen.
3. `SQL/00_FreshInstall.sql`: `[Matchcode]`-Zeile aus dem `ProductionOrders`-Block entfernen, den
   `__EFMigrationsHistory`-Insert für `20260910081155_AddProductionOrderMatchcode` entfernen.
4. `FaMaterializationSyncService.cs`: beide `Matchcode =`-Zeilen entfernen;
   `FaMaterializationPlanner.MaterializationSourceOrder` um das `Matchcode`-Feld kürzen, falls dort
   separat deklariert.
5. **Anzeige/Filter NICHT entfernen** — nur die Datenquelle in `ProjectLeitstandRows`,
   `ApplyLeitstandColumnFilter` (Case `"matchcode"`) und `PickingController.Index` von
   `o.Matchcode` auf einen Article-Join/-Subquery umstellen (Details siehe „Technischer
   Lösungsentwurf"). `ColumnDefinitions.cs`, die `#column-config`-Blöcke und die Row-Partials
   bleiben unverändert — sie kennen nur den Spaltenschlüssel `matchcode`, nicht seine Quelle.
6. Bestehende Tests, die `ProductionOrder.Matchcode` direkt setzen/prüfen
   (`ProductionOrderRepositoryTests.cs`, `FaMaterializationSyncServiceTests.cs`), auf die neue
   Quelle umstellen.

## Technischer Lösungsentwurf

**Muster:** Repository-Pattern (ADR 0001), Listen-View-Pattern mit Server-Spaltenfilter (ADR 0005),
Audit-Felder (ADR 0003).

- **`Article.Matchcode`:** neue nullable `string`-Property, `[StringLength(200)]`, kein
  `[Required]` (AKE bleibt zunächst leer). Keine neue Rolle, keine neue Zugriffsprüfung — dieselben
  Rollen wie die übrigen fünf Stammdaten-Sichten (`masterdata`/`masterdata_read`).
- **Sync (`SageImportService.SyncArticlesAsync`):** Erweiterung der `sageSql`-`UNION`-Query um eine
  Matchcode-Spalte aus `KHKArtikel` (Spaltenname per Rückfrage 4 zu klären), Aufnahme in Upsert-SQL
  (INSERT-Liste, UPDATE-SET-Liste, Änderungserkennungs-`ISNULL(...)!=ISNULL(...)`-Kette) — exakt
  dasselbe Muster wie die fünf bestehenden Spalten dort. Solange Rückfrage 4 offen ist, bleibt dieser
  Umsetzungsschritt zurückgestellt; Spalte + Suche funktionieren auch mit durchgehend `NULL`
  (null-sicherer Lesepfad).
- **Suche/Filter (Erhebung a):** Wo `Article` bereits geladen ist (`ArticleRepository`,
  `StockMovementRepository` über `sm.Article`), reine Erweiterung der bestehenden `Contains`/
  `EF.Functions.Like`-Ausdrücke um `Matchcode` — keine neue Query-Struktur. Wo heute ein
  **Snapshot-Feld** durchsucht wird (`PartRequisition.ArticleNumber`/`ArticleDescription`,
  `WarehouseRequisitionItem.ArticleNumber`/`ArticleDescription`), wird die Matchcode-Suche über
  einen zusätzlichen Join/eine korrelierte Subquery auf `Article.ArticleNumber` realisiert (gleiche
  Index-Seek-Charakteristik wie beim FA-Listen-Rückbau) — es wird **kein** neues Snapshot-Feld
  eingeführt, weil das den Matchcode zum Bestellzeitpunkt einfrieren und die „eine Quelle, immer
  aktuell"-Anforderung des Backlogs unterlaufen würde.
- **FA-Zeilen-Listen (Rückbau):** In `ProductionOrderRepository.ProjectLeitstandRows` und
  `PickingController.Index` ersetzt eine korrelierte Subquery
  `_context.Set<Article>().Where(a => a.ArticleNumber == o.ArticleNumber).Select(a => a.Matchcode).FirstOrDefault()`
  den bisherigen Feldzugriff `o.Matchcode`. Das ist mit EF Core InMemory testbar (reine
  `IQueryable`-Komposition, kein `EF.Functions.Like`) und übersetzt sich serverseitig in einen
  Index-Seek gegen den `UNIQUE`-Index auf `Article.ArticleNumber`. Der `"matchcode"`-Spaltenfilter-Case
  in `ApplyLeitstandColumnFilter` wird auf denselben Ausdruck umgestellt (der bestehende
  `BuildExtraInfoOrContains`-Helper akzeptiert einen beliebigen
  `Expression<Func<ProductionOrder, string?>>`-Selector — eine korrelierte Subquery ist ein gültiger
  Selector-Ausdruck).
- **Artikelstammliste:** `ColumnDefinitions.cs` bekommt eine neue `ColumnDef("matchcode", ...)` in
  der Artikel-`ViewConfig`, `Views/Articles/Index.cshtml` bekommt Spalte + `#column-config`-Eintrag +
  `<th data-col-key="matchcode">` (Fallstricke §3-Pflicht: beide Stellen).
- **QR-Scan (`ArticlesController.Info`):** bleibt bis zur Klärung von Rückfrage 2 unverändert
  (exakter `ArticleNumber`-Treffer) — keine Vorgriffs-Implementierung einer Ambiguitäts-Auflösung
  ohne Entscheidung.

## Migrations-/SQL-Auswirkungen

**Zwei Migrationen in einer Umsetzung:**

1. **Rückbau** (kein neuer Migrationseintrag, sondern Entfernen des bestehenden, nie deployten
   Eintrags `20260910081155_AddProductionOrderMatchcode`) — siehe Abschnitt „Rückbau" oben.
2. **Neu:** `dotnet ef migrations add AddArticleMatchcode --project IdealAkeWms` — additive Spalte
   `Article.Matchcode` (NVARCHAR(200), NULL). Idempotentes `SQL/91_AddArticleMatchcode.sql` mit
   `COL_LENGTH('dbo.Articles', 'Matchcode') IS NULL`-Guard (DDL in eigenem Batch,
   `__EFMigrationsHistory`-Insert in separatem Batch) — Muster 1:1 aus
   `SQL/91_AddProductionOrderMatchcode.sql` übernehmbar, nur Tabelle/Migrationsname geändert.
   `SQL/00_FreshInstall.sql` an beiden Stellen (Schema-Objekt im `Articles`-Block,
   `MigrationId` im Insert-Block).

**Nummer:** Da die bisherige Migration 91 (`ProductionOrder.Matchcode`) im selben Zug zurückgebaut
wird und **nie deployt** war, wird die SQL-Nummer **91 wieder frei** — `SQL/91_AddArticleMatchcode.sql`
ist die konsistente Wahl. Falls zwischen Spec und Umsetzung ein anderer Branch bereits eine 91er-
Datei gemergt hat (Restrisiko jedes offenen Bündels, wie in der Schwester-Spec vermerkt), am
Umsetzungszeitpunkt neu zählen (`92`).

**Kein Backfill-Zwang:** Bestandszeilen bleiben `NULL` und füllen sich beim nächsten Artikel-Sync-
Lauf (IDEAL sofort, sobald die Sage-View-Zuordnung geprüft ist; AKE erst nach der
Sync-Query-Erweiterung, Rückfrage 4). Keine daten-destruktive Migration.

## Audit-Feld-Auswirkungen

`Article` erbt `AuditableEntity` (bestätigt am Modell). Der Sage-Sync setzt bei jedem Schreiben
`ModifiedAt = GETUTCDATE()`, `ModifiedBy = 'IDEALAKEWMSService'`, `ModifiedByWindows = SYSTEM_USER`
(bestehendes Muster, Upsert-SQL Zeilen 490-492) — Matchcode reiht sich in die bestehende
Änderungserkennung und Zuweisung ein, keine neue Logik. Manuelle Bearbeitung über `/Articles/Edit`
(Rolle `masterdata`) setzt Audit-Felder über `ICurrentUserService` wie bei den übrigen Feldern
bereits etabliert — vom Dev-Lauf am tatsächlichen `ArticlesController.Edit`-POST zu bestätigen, nicht
neu zu bauen.

## Akzeptanzkriterien

1. `Article` hat eine Spalte `Matchcode` (nullable, `NVARCHAR(200)`); Migration additiv, idempotent
   (zweifacher Lauf des SQL-Skripts ändert nichts am zweiten Mal).
2. Die bereits gebaute `ProductionOrder.Matchcode`-Spalte, ihre Migration und die zwei
   Materialisierungs-Schreibstellen existieren nach dem Rückbau nicht mehr im Code; die fünf
   FA-Zeilen-Listen zeigen weiterhin korrekt den Matchcode je Zeile (Regressionstest gegen den
   vor-Rückbau-Stand, gleiche Werte für dieselben Testdaten).
3. Matchcode-Spaltenfilter in den fünf FA-Zeilen-Listen funktioniert unverändert (Teilstring, Mini-
   Syntax OR/NOT) — jetzt gespeist über den Article-Join statt der entfernten Spalte.
4. Artikelstammliste (`/Articles`) zeigt eine Matchcode-Spalte, filterbar (eigene Spaltenfilter-Case)
   und Teil der Freitextsuche zusammen mit Artikelnummer/Bezeichnung.
5. Artikelinfo (`/Articles/Info`) zeigt den Matchcode des gefundenen Artikels, sofern vorhanden.
6. Bestandsübersicht (`/StockOverview`) und Bewegungshistorie (`/StockMovements`) finden einen
   Artikel über eine Matchcode-Teilstring-Eingabe in Freitextsuche UND Spaltenfilter.
7. Fehlteile (`/MissingParts`, `/MissingPartsLager`) und Lager-/Glasbestellungs-Item-Ansichten finden
   eine Position über eine Matchcode-Teilstring-Eingabe.
8. Bedarfsmeldungen (`/PartRequisitions`) finden einen Bedarf über eine Matchcode-Teilstring-Eingabe.
9. Ein leerer Matchcode (AKE vor Sync-Erweiterung) führt in JEDER erweiterten Suche zu „kein Treffer
   auf dieser Spalte", nie zu einem Fehler, nie zu einer unerwartet leeren Gesamtliste.
10. AKE-Regression: Solange die Sync-Query nicht erweitert ist, bleibt `Article.Matchcode` bei allen
    AKE-Artikeln `NULL` — keine Exception im Sync-Lauf, keine Änderung an den übrigen synchronisierten
    Feldern.
11. Der Artikel-Sync (sobald Rückfrage 4 geklärt und die Query erweitert ist) schreibt Matchcode-
    Änderungen inkl. korrekt gesetzter Audit-Felder (`ModifiedAt`/`ModifiedBy`/`ModifiedByWindows`).
12. OSEON Teileverfolgung und BOM-Komponentenebene zeigen unverändert **keinen** Matchcode — das ist
    laut Out-of-Scope kein Fehler, sondern Folge-Arbeit, und wird im UAT-Vermerk als solches benannt.
13. `dotnet build` + `dotnet test` (Web und Service) grün.

## Test-Szenarien

Nächste freie TS-Nummer am Worktree-Stand prüfen (zuletzt `TS-74.x`, voraussichtlich `TS-75`).
Skizze der Szenarien, `docs/TESTSZENARIEN.md` durch den Dev-Lauf vollständig auszuformulieren
(Vorbedingungen/Schritte/Erwartung/Negativfall je Szenario):

1. Artikelstammliste: Freitextsuche und Spaltenfilter auf Matchcode-Teilstring finden den
   erwarteten Artikel, ein Nicht-Treffer-String liefert eine leere Liste (kein Fehler).
2. Artikelinfo: Aufruf mit gültiger Artikelnummer zeigt den Matchcode; Artikel ohne Matchcode zeigt
   das Feld leer/„—", kein Fehler.
3. Bestandsübersicht + Bewegungshistorie: Matchcode-Teilstring in Freitext und Spaltenfilter liefert
   dieselben Treffer wie eine Suche nach der zugehörigen Artikelnummer.
4. Fehlteile + Lagerbestellung/Glasbestellung: Matchcode-Teilstring findet die erwartete Position.
5. Bedarfsmeldungen: Matchcode-Teilstring findet den erwarteten Bedarf.
6. FA-Listen-Regression (5 Listen): Matchcode-Anzeige und -Filter liefern nach dem Rückbau exakt
   dieselben Ergebnisse wie vor dem Rückbau (gleiche Testdaten, gleiche Treffer/Reihenfolge).
7. AKE-Artikel ohne Matchcode: Matchcode-Suche liefert korrekt „kein Treffer", keine Exception, alle
   übrigen Suchwege (Artikelnummer/Bezeichnung) bleiben unverändert funktionsfähig.
8. Migrations-Idempotenz: `SQL/91_AddArticleMatchcode.sql` zweimal gegen eine Testdatenbank
   ausgeführt ändert beim zweiten Lauf nichts (kein Fehler, kein Duplikat).
9. Sage-Sync (sobald Query erweitert): ein geänderter Matchcode in der Sage-Quelle wird beim
   nächsten Lauf übernommen, inkl. korrekt gesetzter Audit-Felder.

## Deploy

- **Web-App:** ja (Repositories, Controller, Views, ViewModels betroffen).
- **Service:** ja (`FaMaterializationSyncService` — Rückbau zweier Zeilen; `SageImportService`,
  sobald Rückfrage 4 geklärt ist).
- **Migration:** ja (Rückbau `AddProductionOrderMatchcode` + neu `AddArticleMatchcode`).
- **Reihenfolge-Hinweis:** Diese Spec läuft im selben Bündel-Worktree wie die Schwester-Spec und
  alle anderen offenen IDEAL-Bausteine — ein gemeinsamer Merge (Schranke 2), kein Zwischen-Merge.
  Vor dem Deploy: DB-Backup (Standardregel bei Migrationen), danach Migrations-SQL ausführen, dann
  Service + Web neu starten, danach einen Artikel-Sync-Lauf abwarten, bevor der Matchcode bei IDEAL
  in den Listen erscheint (bekanntes Zwei-Lauf-Muster aus der Materialisierungs-Spec).
- **Publish-Befehle** (im Worktree, nach Merge-Test ggf. vom Repo-Root):
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```

## Offene Rückfragen

1. **Verhältnis zu `Article.Description`:** eigene Spalte `Matchcode` (Empfehlung dieser Spec, siehe
   „Fachliche Anforderungen") oder ersetzt der Matchcode ein bestehendes Feld?
2. **QR-Scan in der Artikelinfo:** Kann der gescannte Code auch ein Matchcode sein, oder trägt er
   immer die Artikelnummer? Falls beides möglich ist: welche Vorrang-Reihenfolge (erst das
   Eindeutige = Artikelnummer, dann das Mehrdeutige = Matchcode-Teilstring), analog der
   FA-Scan-Auflösung (`BdeScanResolver`)?
3. **Gilt die neue Spalte für beide Standorte gleich, und wie verhält sie sich zum bestehenden
   `FaHierarchyNode.Matchcode`** (auftrags-/knotenbezogen, IDEAL-spezifisch)? Ist Letzterer künftig
   nur noch die Sync-Quelle für `Article.Matchcode`, oder bleiben beide unabhängig nebeneinander
   bestehen (z. B. weil ein Knoten in Einzelfällen vom Artikelstamm abweichen könnte)?
4. **Liefert die AKE-Sage-Quelle (`KHKArtikel`, `SageImportService.SyncArticlesAsync`) den Matchcode
   bereits unter einem bekannten Spaltennamen?** Am Code nicht abschliessend entscheidbar (reale
   `KHKArtikel`-Struktur liegt in der Sage-DB, nicht im Repo); die IDEAL-Sage-View verwendet
   „KHKArtikel als Fallback" für ihren Matchcode, was auf ein existierendes Sage-Feld hindeutet, aber
   den genauen Namen nicht bestätigt.
5. **LIKE-Latenzmessung** auf den ~108.818 `Article`-Zeilen (bestehender Zwei-Spalten-Scan
   `ArticleNumber`/`Description`) ist noch nicht durchgeführt (Production-Reads-Schutz hat die
   Messung im Rahmen dieses Spec-Laufs blockiert) — vor Abschluss der Umsetzung nachzuholen, um die
   Empfehlung in Erhebung (b) zu bestätigen oder zu widerlegen.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → **Eigene Spalte.** `Bezeichnung1` und `Matchcode` sind fachlich verschieden: die eine ist
   beschreibender Text, die andere ein **Typenschluessel**. Ein Ersatz wuerde Information
   vernichten, und jede Ansicht, die heute die Bezeichnung zeigt, zeigte ploetzlich etwas anderes.

2. → **Scan: EXAKT und mit Vorrang. Suche: Teilstring.** Die beiden Wege sind ausdruecklich
   verschieden.
   - **QR-Scan:** erst **Artikelnummer exakt**, dann **Matchcode exakt**, sonst „nicht gefunden".
     Dieselbe Vorrang-Reihenfolge wie beim FA-Scan: zuerst das Eindeutigere.
   - **Kein Teilstring-Treffer beim Scannen.** Ein Scan liefert einen vollstaendigen Code; ihn
     unscharf aufzuloesen erzeugte ueberraschende Treffer, bei denen der Werker nicht merkt, dass
     er den falschen Artikel vor sich hat. Teilstring gilt nur fuer die **getippte** Suche.

3. → **Kein Abgleich zwischen beiden — sie stammen ohnehin aus derselben Quelle.**
   `Article.Matchcode` kommt aus `KHKArtikel.Matchcode`; `FaHierarchyNode.Matchcode` kommt aus der
   IDEAL-Struktur-View, die ihrerseits auf demselben Sage-Artikelstamm aufsetzt.
   - **`Article.Matchcode` ist die Wahrheit** fuer alles Artikelbezogene (Suche, Artikelinfo,
     Bestand, Bestellungen, die fuenf FA-Listen ueber den Equi-Join).
   - **`FaHierarchyNode.Matchcode` bleibt unveraendert** und bedient weiterhin die
     FA-Struktur-Baumansicht, die direkt aus der Strukturtabelle liest.
   - **Keine Synchronisierung, kein Nachziehen, kein Vorrang-Regelwerk** zwischen beiden — das
     waere Doppelpflege ohne Gegenwert.
   **Beide Standorte:** Jeder Mandant hat seinen eigenen `KHKArtikel`; der Artikel-Sync holt den
   Matchcode je Standort aus der eigenen Quelle. Kein Sonderweg fuer IDEAL.

4. → **BEANTWORTET — ja, Sage liefert ihn.** Die erweiterte Abfrage liegt vor und fuehrt
   `CAST(a.Matchcode AS nvarchar(500)) AS Matchcode` in **beiden** Zweigen (Ressourcen-Positionen
   und Artikelstamm), aggregiert als `MAX(Matchcode)`.
   Damit ist die als „hausinterne Folgeaufgabe" vermerkte View-/Query-Erweiterung **bereits
   erledigt** — `SageImportService.SyncArticlesAsync` uebernimmt genau diese Abfrage.
   **Die Null-Sicherheit bleibt trotzdem Pflicht:** `Matchcode` ist in Sage nicht
   zwingend gefuellt. Leere Werte duerfen Suche und Anzeige nicht brechen.
   *Randnotiz — Feldlaenge, mit Pruefschritt (2026-09-13):* Quelle und Zielspalte werden auf
   **`nvarchar(200)`** angeglichen.
   **Achtung, Fehlerklasse:** `CAST(... AS nvarchar(200))` kuerzt **stillschweigend**. Waere der
   `CAST` groesser als die Spalte, schluege der Insert **laut** fehl — die Angleichung tauscht also
   einen sichtbaren Fehler gegen einen unsichtbaren.
   **Deshalb vor der Umsetzung einmal messen statt abwaegen:**
   ```sql
   SELECT MAX(LEN(Matchcode)) AS MaxLen, COUNT(*) AS Gefuellt
   FROM [dbo].[KHKArtikel] WHERE Matchcode IS NOT NULL AND Matchcode <> '';

   SELECT c.max_length/2 AS Zeichen FROM sys.columns c
   WHERE c.object_id = OBJECT_ID('dbo.KHKArtikel') AND c.name = 'Matchcode';
   ```
   Ist die Sage-Spalte kuerzer als 200 (bei einem Matchcode-Feld wahrscheinlich), ist das Thema
   erledigt. Liegt der laengste Wert nahe an 200, **groesser dimensionieren** — eine breitere
   `nvarchar`-Spalte kostet in SQL Server nichts, sie ist variabel lang.

5. → **Nicht gegen die Produktionsdatenbank messen. Die Entscheidung haengt nicht daran — aber eine
   andere Frage schon.**
   Die Argumentation der Spec traegt: Der Full Scan existiert heute bereits fuer zwei Spalten, ein
   drittes `OR`-Glied erzeugt keinen zusaetzlichen Durchlauf. **Schlichter `LIKE`, kein
   Volltextindex** — bestaetigt. Der Schutz auf Produktions-Lesezugriffe besteht aus gutem Grund;
   ihn fuer eine Zahl zu oeffnen, die die Entscheidung nicht aendert, waere der falsche Handel.
   Messen zur Umsetzungszeit am Testsystem genuegt.

   **ABER — 108.818 Artikel machen eine Frage wichtig, die bisher nicht gestellt wurde:**
   **Sucht die Oberflaeche bei jedem Tastendruck oder erst beim Absenden?**
   - **Beim Absenden** → ein Scan je Suchvorgang, unkritisch.
   - **Bei jedem Tastendruck** → ein Full Scan ueber 108.818 Zeilen **pro Buchstabe**. Wer
     „FRR-760" tippt, loest sieben Scans aus, der erste davon auf ein einziges Zeichen.
   **Zu pruefen und, falls live gesucht wird, verbindlich:**
   - **Mindestens 3 Zeichen**, bevor gesucht wird.
   - **Entprellung** (~300 ms), damit waehrend des Tippens nicht jeder Anschlag feuert.
   Beides ist billig und verhindert genau den Fall, der aus einer vertretbaren Abfrage eine
   spuerbar traege Oberflaeche macht.

## Kritische Pruefung (2026-09-15)

Anwalt des Teufels, vor der Freigabe. Freigabe-Antworten 1–5 gelesen; am Worktree-Code
`2026-08-07-ideal-teile-1-5` gegengeprueft: Migrationsreihenfolge, `SageImportService.
SyncArticlesAsync` (inkl. Filter), `FaHierarchySql.cs` (Matchcode-Herkunft), `SageConnection`-
Nutzung beider Sync-Dienste. Die Antworten sind inhaltlich stark und schliessen die
Wert-/Quellfrage (3), die Scan-Semantik (2) und die Sage-Verfuegbarkeit (4) sauber. **Ein
tragender Punkt bleibt jedoch ungemessen, und zwei verbindliche Zusagen stehen nur in den
Antworten, noch nicht im pruefbaren Rumpf.**

### BLOCKER

- **B-1 — Coverage-Luecke IDEAL: die fuenf FA-Listen koennten ihren Matchcode nach dem Rueckbau
  STILL verlieren; AK #2 ist bis zur Messung ungedeckt.** Antwort 3 entscheidet „`Article.Matchcode`
  ist die Wahrheit … die fuenf FA-Listen ueber den Equi-Join" und begruendet das mit „gleiche
  Quelle". Das loest die **Wert**-Frage (gut), aber **nicht die Existenz-Frage**: Der Join
  `ProductionOrder.ArticleNumber = Article.ArticleNumber` liefert nur dann einen Matchcode, wenn die
  FA-Zeilen-Artikelnummer ueberhaupt in `Articles` steht. `Articles` wird von
  `SageImportService.SyncArticlesAsync` aber **gefiltert** befuellt: nur Artikel, die in
  `KHKPpsRessourcenPositionen` vorkommen (Zweig 1) **oder** `IstBestellartikel = -1 AND Aktiv = -1`
  sind (Zweig 2, `SageImportService.cs:391-395/406-409`). **Gefertigte HauptFA-/SubFA-Artikel
  (Baugruppen, Endprodukte) sind typischerweise keine Bestellartikel** und muessen nicht als
  Ressourcenposition auftauchen — sie koennen durch **beide** Filter fallen und in `Articles`
  fehlen. Heute hat jede FA-Zeile ihren Matchcode aus `FaHierarchyNode.Matchcode` (fuer JEDEN
  Knoten vorhanden); nach dem Rueckbau haengt er am Join. Fehlt der Artikel → `NULL` → **leerer
  Matchcode genau bei IDEAL**, wo er essentiell ist. Das verletzt AK #2 („gleiche Werte") UND die
  Hausregel „melden statt still behandeln". Die 108.818-Zeilen-Zahl belegt NICHT, dass die
  FA-Artikel dabei sind.
  *Billige Messung, die vor dem Dev-Lauf entscheidet (am Testsystem, kein Prod-Zwang):*
  ```sql
  -- Wie viele materialisierte FA-Zeilen-Artikel fehlen im Artikelstamm?
  SELECT COUNT(*) AS FaZeilenGesamt,
         SUM(CASE WHEN a.ArticleNumber IS NULL THEN 1 ELSE 0 END) AS OhneArtikelstamm
  FROM ProductionOrders po
  LEFT JOIN Articles a ON a.ArticleNumber = po.ArticleNumber;
  ```
  Ist `OhneArtikelstamm > 0`, ist Variante (a)/(c) **nicht regressionsfrei** und braucht eine
  bewusste Entscheidung: Artikel-Sync-Filter um die fehlenden FA-Artikel erweitern, ODER doch
  Variante (b) (denormalisierte Kopie, die die Spec derzeit verwirft), ODER IDEAL behaelt seine
  `FaHierarchyNode`-Quelle fuer die Listen und nur Artikelstamm/AKE bekommt `Article.Matchcode`.
  *Frage an den Menschen:* Wurde diese Coverage geprueft? Wenn nein — vor der Freigabe messen.

### SOLLTE

- **S-1 — Antwort 5 (Mindestlaenge + Entprellung) verbindlich in Rumpf und Akzeptanzkriterien
  ziehen.** Die Zusage „mind. 3 Zeichen, ~300 ms Entprellung, falls live gesucht wird" lebt nur in
  der Antwort. Der Dev-Lauf liest zwar die Antworten als Auftrag, aber es fehlt (a) das
  **Code-Verifikat**, OB die Artikel-Typeahead-Suche (`ArticlesApiController` → `ArticleRepository.
  SearchAsync`, genutzt u. a. von Lager-/Glasbestellung und Bedarfsmeldung) tatsaechlich je
  Tastendruck feuert, und (b) ein **pruefbares Akzeptanzkriterium** dafuer. Vorschlag: neues AK
  „Live-Artikelsuche feuert erst ab 3 Zeichen und entprellt ~300 ms; per-Tastendruck-Full-Scan ist
  ausgeschlossen" + expliziter Verifikationsschritt im Loesungsentwurf.
- **S-2 — Widerspruch in Antwort 4 aufloesen: `CAST(... AS nvarchar(500))` vs. Zielspalte
  `nvarchar(200)`.** Der in Antwort 4 gezeigte Query-Ausschnitt castet auf `nvarchar(500)`, die
  Entscheidung direkt darunter gleicht Quelle und Ziel aber auf **`nvarchar(200)`** an
  (`Article.Matchcode`, Migration, Model `[StringLength(200)]`). Ein 500er-Wert in eine 200er-Spalte
  laeuft in genau die „laut fehlschlagen"-Falle, die die Antwort selbst benennt. Vor der Umsetzung:
  die in Antwort 4 vorgesehene `LEN`-Messung an `KHKArtikel.Matchcode` ausfuehren, dann Ziel- UND
  CAST-Laenge einheitlich setzen (>= gemessenem Maximum). Im Rumpf ist derzeit durchgaengig 200
  angenommen — die 500 im Antwort-Snippet muss angeglichen werden, damit der Dev-Lauf nicht die
  falsche Zahl uebernimmt.
- **S-3 — Rueckbau- und Neu-Migration als geordneten Ablauf festschreiben.** `dotnet ef migrations
  remove` funktioniert nur, solange `AddProductionOrderMatchcode` die **letzte** Migration ist
  (heute im Worktree bestaetigt, H-1). Reihenfolge daher verbindlich: **zuerst** entfernen,
  **danach** `AddArticleMatchcode` hinzufuegen. Als nummerierten Schritt in „Migrations-/SQL-
  Auswirkungen".
- **S-4 — AK #2 als echten Vorher/Nachher-Vergleich formulieren (ist zugleich der B-1-Nachweis).**
  Statt „gleiche Werte" behaupten: vor dem Rueckbau die Ist-Matchcodes der fuenf Listen fuer einen
  realen IDEAL-Auftrag festhalten, nach dem Rueckbau bit-vergleichen — **und explizit zaehlen,
  welche Zeilen von gefuellt auf leer kippen**. Kippt eine, ist B-1 eingetreten.

### HINWEIS

- **H-1** — `ef migrations remove`-Vorbedingung ist erfuellt: `AddProductionOrderMatchcode` ist die
  letzte Migration im Worktree (verifiziert). Der „sauberer Rueckbau statt Gegen-Migration"-Ansatz
  traegt.
- **H-2** — **Bewusste Inkonsistenz Baumansicht ↔ Listen, dokumentieren.** Antwort 3 laesst
  `FaHierarchyNode.Matchcode` (FA-Struktur-Baum, `/FaHierarchy`) unveraendert und macht
  `Article.Matchcode` zur Quelle der fuenf Listen. Weicht der positionsbevorzugte View-Matchcode je
  vom Artikelstamm-Matchcode ab (die IDEAL-View-Doku sagt „Position bevorzugt, KHKArtikel als
  Fallback"), zeigen **Baum und Listen fuer denselben Knoten dann unterschiedliche Matchcodes**.
  Das ist durch Antwort 3 gedeckt (Artikel gewinnt in den Listen), aber es gehoert als
  „so gewollt, kein Fehler" in den UAT-Vermerk — sonst wird es beim ersten Nebeneinander gemeldet.
- **H-3 — Groesse.** ~18 Dateien ueber Model/Migration/Service/Repository/Controller/View/Tests plus
  Rueckbau. Grenzwertig, aber als **ein** kohaerentes Thema mit klarer Commit-Folge (1. Rueckbau,
  2. `Article.Matchcode` + Sync, 3. Suchstellen, 4. Anzeige/Tests) in einem Dev-Lauf machbar — kein
  Split/Epic zwingend. Weicht B-1 die Variante auf, neu bewerten.
- **H-4** — Die frueher vermutete NVARCHAR-Diskrepanz war ein Byte/Zeichen-Lesefehler und ist
  bereits korrigiert; kein offener Punkt.

### Drei wichtigste Punkte

1. **B-1: IDEAL koennte seinen Matchcode nach dem Rueckbau still verlieren.** Die fuenf Listen
   ziehen ihn heute aus `FaHierarchyNode` (jeder Knoten hat ihn), kuenftig aus `Article` (nur
   gefiltert befuellt — gefertigte FA-Artikel fehlen evtl.). **Vor der Freigabe die eine
   `LEFT JOIN`-Zaehl-Query laufen lassen.** Das kann die gewaehlte Variante kippen.
2. **S-1: Die Entprellungs-/Mindestlaengen-Zusage aus Antwort 5 muss in Rumpf + Akzeptanzkriterium**
   — sonst geht die 108k-Zeilen-Live-Suche zwischen den Antworten verloren.
3. **S-2: Antwort 4 castet auf 500, die Zielspalte ist 200** — vor der Umsetzung die
   `KHKArtikel.Matchcode`-Laenge messen und beide Zahlen angleichen.

**NACHBESSERUNG NOETIG:** Die Coverage IDEAL-Artikel → `Articles` (B-1) ist ungemessen und kann die
gewaehlte „ein Ort, alles ueber Join"-Variante kippen; zusaetzlich muessen die verbindlichen Zusagen
aus Antwort 4 (Feldlaenge) und 5 (Entprellung/Mindestlaenge) in den pruefbaren Spec-Rumpf.
