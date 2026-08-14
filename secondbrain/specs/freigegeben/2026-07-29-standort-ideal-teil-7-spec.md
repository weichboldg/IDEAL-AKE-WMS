---
type: spec
title: "IDEAL-Standort Teil 7 — Materialisierung nach ProductionOrders (Schema-Inversion, Einweg-Migrationstor)"
slug: 2026-07-29-standort-ideal-teil-7-spec
status: InUmsetzung
created: 2026-08-06
updated: 2026-08-14
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - IdealAkeWms/Models/ProductionOrder.cs (SubOrderNumber, ParentSubOrderNumber, SageMissingSince)
  - IdealAkeWms/Data/ApplicationDbContext.cs (Index-Umbau OrderNumber -> SubOrderNumber, sys.indexes-Guard)
  - IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs (inkl. neue mengenwertige GetAllByFaAndOperationAsync + Logging im bestehenden Einzel-Lookup bei Mehrfachtreffer)
  - IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs
  - IdealAkeWms/Controllers/ProductionOrdersController.cs
  - IdealAkeWms/Controllers/PickingLeitstandController.cs
  - IdealAkeWms/Controllers/FaWorklistController.cs
  - IdealAkeWms/Controllers/FaCompletionController.cs
  - IdealAkeWms/Controllers/TrackingController.cs
  - IdealAkeWms/Controllers/ServiceSettingsController.cs (Master nur read-only anzeigen, Link auf Umschalt-Seite)
  - IdealAkeWms/Controllers/StandortEinstellungenController.cs (Teil 6, Master nur read-only anzeigen — kein Schreib-Bedienelement)
  - IdealAkeWms/Controllers/HierarchieUmstellungController.cs (neu, dedizierte Umschalt-Seite mit Bestaetigungsdialog)
  - IdealAkeWms/Views/HierarchieUmstellung/Index.cshtml (neu)
  - IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs (adversariales Review + Fold-2-Skip bei WaNummer-Mehrfachtreffer, PFLICHT)
  - IDEALAKEWMSService/Services/ProductionOrderReconciler.cs (adversariales Review + ggf. Anpassung, PFLICHT)
  - IDEALAKEWMSService/Services/SageImportService.cs
  - IDEALAKEWMSService/Services/SageProductionOrderSql.cs
  - IdealAkeWms/Services/HierarchischeStrukturGuard.cs (neu, Domaenen-Waechter/einziger Choke-Point fuer ServiceSettings-Schreibpfade auf den Master-Key)
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs (Master ProduktionsauftragHierarchisch + Auto-Erledigt-Schalter (neu) + weitere abhaengige Schalter — Liste vor Etappe B/D verbindlich benennen)
  - IdealAkeWms/Models/SyncLogServices.cs (neuer Service-Name HierarchieUmstellung fuers Aktivitaets-Protokoll)
  - SQL/87_InvertProductionOrderHierarchy.sql (neu, naechste freie Nummer — vor Dev-Lauf pruefen; SubOrderNumber/ParentSubOrderNumber/SageMissingSince + sys.indexes-Guard fuer Index-Tausch)
  - SQL/00_FreshInstall.sql
  - SQL/AgentJobs/01_Import_Produktionsauftraege.sql (tot — loeschen bzw. nach SQL/AgentJobs/_archiv/ verschieben mit Kopfkommentar AUSSER BETRIEB seit Migrationsdatum; Ordner auf weitere tote Artefakte pruefen)
  - docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md (neu)
  - README.md (Runbook-Verlinkung)
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions: []
epic: true
etappen:
  - "A: Schema-Inversion + Migration (SubOrderNumber, ParentSubOrderNumber, SageMissingSince, sys.indexes-Guard fuer den Index-Tausch) + Backfill + FreshInstall an beiden Stellen (Schema + MigrationId) + tote AgentJob-Artefakte entfernen/archivieren + serverseitige Pruefung auf aktive SQL-Agent-Jobs als harte Deploy-Vorbedingung"
  - "B: HierarchischeStrukturGuard als einziger Choke-Point + Einwegtor ueber eine dedizierte Umschalt-Seite mit Bestaetigungsdialog (generische ServiceSettings- und Teil-6-Maske zeigen den Master nur noch read-only) + Audit-SyncLog + Runbook docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md + README-Verlinkung + explizite Benennung der abhaengigen Schalter inkl. neuem Auto-Erledigt-Schalter"
  - "C: Materialisierungs-Sync + die drei Sync-Regeln als reine, unit-getestete Planer (inkl. SageMissingSince setzen/zuruecksetzen, Sammelmeldung pro Sync-Lauf statt Einzel-Mail je FA)"
  - "D: Lookup-Haertung (OrderNumber-Sweep ueber alle identifizierten Fundstellen, 14 als Untergrenze) + mengenwertige GetAllByFaAndOperationAsync mit Logging im bestehenden Einzel-Lookup + adversariales FA-Zusatzinfos-Review inkl. dreistufiger Auto-Erledigt-Sperre + gezielte hierarchische Unit-Tests fuer den Reconcile-UPDATE-Pfad und Fold 2"
  - "E: Doku (README, Runbook falls nicht bereits in B abgeschlossen), Testszenarien, Brain-Update"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-08-12
---

## Etappen-Fortschritt (Bündel-Worktree `feature/2026-08-07-ideal-teile-1-5`)

Teil 7 läuft als Epic im **bestehenden** Bündel-Worktree (gemeinsam mit Teile 1–5 v1.31.0).
Ausführliche Umsetzungsnotizen: [[2026-07-29-standort-ideal-teil-7]] (Aufgaben).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| A | Schema-Inversion + Migration (SubOrderNumber/ParentSubOrderNumber/SageMissingSince, Index-Tausch, Backfill) + FreshInstall + tote AgentJob-Artefakte | **erledigt** | `fe7299b` |
| B | `HierarchischeStrukturGuard` (Choke-Point) + Einwegtor über Umschalt-Seite + Audit-SyncLog + Runbook | offen | — |
| C | Materialisierungs-Sync + drei Sync-Regeln (unit-getestete Planer) | offen | — |
| D | Lookup-Härtung (`OrderNumber`-Sweep) + `GetAllByFaAndOperationAsync` + adversariales FA-Zusatzinfos-Review + Auto-Erledigt-Sperre | offen | — |
| E | Doku (README/Runbook), Testszenarien, Brain-Update, Version-Bump | offen | — |

> **Kopplungs-Hinweis:** Teil 7 liegt im selben Branch wie die bereits testbereiten Teile 1–5.
> Etappe A invertiert die Kern-Tabelle `ProductionOrders` (Migration 90). Der Merge dieses Branches
> bringt damit **beides** in einem Rutsch nach `main` — bei Schranke 2 bewusst entscheiden.

## Ziel / Nutzen (das Warum)

Ab Teil 7 wird der Kern angefasst: Aus `FaHierarchyNode` (Teil 1) werden die Zeilen mit
`SubFA != 0` (plus Wurzel) als echte `ProductionOrders` materialisiert. Erst dadurch werden
Sub-FAs rueckmeldefaehig (Arbeitsgaenge, Teileverfolgung, BDE — Teil 8), weil diese Module
ausschliesslich gegen `ProductionOrders` arbeiten. Die Struktur bleibt die Quelle, `ProductionOrders`
ist abgeleitet (Transformation, kein zweiter Import) — die beiden laufen so nicht auseinander, und
die Transformation ist ohne DB testbar.

Dieser Teil ist als **Epic** zugeschnitten (siehe Etappen A–E im Frontmatter): eine
daten-konvertierende Migration der zentralsten Tabelle, ein Einweg-Migrationstor ueber mehrere
Schreibwege, ein neuer Materialisierungs-Sync mit drei Sync-Regeln und die Haertung von 14+
`OrderNumber`-Lookups sind zusammen groesser als in einem Dev-Lauf sicher schaffbar. Ein einziger
langlebiger Worktree, kein Zwischen-Merge — der Branch wird waehrend der Arbeit ueber
`scripts/sync-worktree.ps1` auf main-Stand gehalten, jede Etappe endet in einem eigenen Commit.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Schema-Inversion (`OrderNumber` nicht mehr unique, `SubOrderNumber` unique,
`ParentSubOrderNumber` nullable, `SageMissingSince` als neuer Zeitstempel), Backfill fuer
AKE-Bestandsdaten, Einweg-Migrationstor (Master-Schalter `ProduktionsauftragHierarchisch`) inkl.
eigener Umschalt-Seite, Materialisierungs-Sync (Struktur → `ProductionOrders`), die drei
Sync-Regeln (nicht loeschen bei Rueckmeldungen; Umhaengung nicht still uebernehmen; neuer Sub-FA
anlegen), Durchzug von `SubOrderNumber` durch Repositories/Controller, eine mengenwertige
Lookup-Variante (`GetAllByFaAndOperationAsync`) fuer mehrdeutig gewordene Einzel-Lookups,
adversariales Review der FA-Zusatzinfos-Kollision inkl. dreistufiger Auto-Erledigt-Sperre.

**Out-of-Scope:** die eigentliche BDE-/Rueckmelde-Logik (Teil 8); Teil 2–6 (Listen/Anzeige) bleiben
unveraendert auf `FaHierarchyNode` aufgesetzt und sind von dieser Materialisierung **nicht**
abhaengig (B5). Auch die Scan-Aufloesungs-UI bei mehrdeutiger `OrderNumber`
(`wwwroot/js/barcode-scanner.js`, Auswahl eines konkreten Sub-FA am Terminal) gehoert zu Teil 8
(Teil-8-Freigabe-Antwort 1) — Teil 7 haertet nur Schema und Lookup-Semantik im Server-Code, nicht
den Scan-Client. Die Umstellung der Aufrufer von Einzel- auf mengenwertigen Lookup ist ebenfalls
Teil 8; Teil 7 liefert nur den Baustein (siehe AK 10).

## Fachliche Anforderungen

### Schema-Inversion (unbedingt, nicht schaltbar)

- `OrderNumber` = FA-Nummer (Sage StrukturID / `HauptFA`) — **nicht mehr unique**.
- `SubOrderNumber` (neu, `NOT NULL`) = Sub-FA-Nummer (Sage BelID) — **unique**.
- `ParentSubOrderNumber` (neu, nullable) = `VaterFA` — echter Elternzeiger, weil `OrderNumber`/
  `SubOrderNumber` allein nur zwei Ebenen ausdruecken koennen (Wurzel + Kinder), aber laut B1 ist
  die Struktur mehrstufig (Sub-FA kann unter einem anderen Sub-FA haengen).
- `SageMissingSince` (neu, `datetime2 NULL`) — Zeitstempel, **nicht** Bool: wird gesetzt, sobald ein
  bereits materialisierter Sub-FA nicht mehr in der Struktur-Quelle auftaucht (Sync-Regel 2), und
  automatisch wieder auf `NULL` gesetzt, sobald der FA in einem spaeteren Sync-Lauf erneut auftaucht
  (selbstheilend). Verwendet **nicht** das bestehende `IsCancelled` — „storniert" und „verschwindet
  aus der Quelle" sind fachlich unterschiedliche Zustaende, deren Vermischung spaeter nicht mehr
  aufloesbar waere. Bei AKE (Master aus) bleibt das Feld durchgehend `NULL`.
- Hauptauftrag genau dann, wenn `OrderNumber == SubOrderNumber`.
- **Backfill:** `SubOrderNumber = OrderNumber` fuer alle Bestandszeilen (AKE bleibt damit faktisch
  eindeutig — `OrderNumber == SubOrderNumber` ist im flachen Modus eine Invariante, Verhalten
  identisch zu heute).
- Das Schema-Modell ist **nicht** schaltbar — `SubOrderNumber` kann nicht mal unique sein und mal
  nicht. Die Inversion passiert einmalig und unbedingt fuer die gesamte Tabelle, unabhaengig vom
  Master-Schalter-Zustand.

### Der Master ist ein Migrationstor, kein Betriebsschalter (EINWEG)

Master-Schalter `ProduktionsauftragHierarchisch` (bool, Default `false`) plus davon abhaengige
Betriebsschalter (Master + neuer Auto-Erledigt-Schalter + ggf. weitere — explizite Liste vor
Etappe B/D zu benennen, siehe `affected_code`):

- **Sperrbedingung datengetrieben, nicht schaltergetrieben:** gesperrt genau dann, wenn
  `EXISTS(SELECT 1 FROM ProductionOrders WHERE OrderNumber <> SubOrderNumber)`. Wer versehentlich
  umstellt und es vor dem ersten hierarchischen Import merkt, kann gefahrlos zurueck.
- **Genau ein Schreibweg in der Oberflaeche: eine dedizierte Umschalt-Seite.** Teil 7 baut eine
  eigene, kleine Seite („Umstellung auf hierarchische Produktionsauftraege",
  `HierarchieUmstellungController`/`Views/HierarchieUmstellung/Index.cshtml`) mit Erklaerung,
  Bestaetigungsdialog und dem Domaenen-Waechter dahinter. Ein Einwegtor gehoert nicht als
  Kontrollkaestchen zwischen zwanzig andere Einstellungen — weder in die generische Maske noch in
  die Standorteinstellungen. Die generische ServiceSettings-Maske **und** die Teil-6-Maske
  (`StandortEinstellungenController`) zeigen den Master ausschliesslich **read-only** (Zustand,
  Sperrstatus, seit wann) mit Link auf die Umschalt-Seite — sie bieten kein Schreib-Bedienelement
  fuer diesen Key.
- **Der Waechter (`HierarchischeStrukturGuard`) gehoert in die Service-/Domaenenschicht und ist der
  einzige Choke-Point** fuer jeden Schreibversuch auf den Master-Key — auch technische Versuche
  ueber die generische ServiceSettings-Maske oder eine kuenftige API werden abgelehnt, nicht nur
  ueber fehlende UI-Bedienelemente verhindert.
- **Beim Einschalten** (auf der Umschalt-Seite) Bestaetigung erzwingen: *„Umstellung auf
  hierarchische Produktionsauftraege. Nach dem ersten hierarchischen Import ist eine Rueckkehr zur
  flachen Struktur nicht mehr moeglich. Vorher auf einer Datenbank-Kopie testen. Wirklich
  umstellen?"*
- **Solange noch keine hierarchischen Daten existieren:** Schalter bleibt auf der Umschalt-Seite in
  beide Richtungen aenderbar, mit Hinweis, dass die Sperre mit dem ersten Import greift.
- **Sobald hierarchische Daten existieren:** Die Umschalt-Seite zeigt den Schalter schreibgeschuetzt
  mit Begruendung — *„Kann nicht mehr deaktiviert werden: es liegen Auftraege mit Sub-FA-Struktur
  vor."* Ein Aenderungsversuch ueber einen anderen Weg (generische Maske, Teil-6-Maske, kuenftige
  API) wird vom Guard abgelehnt und protokolliert.
- **Audit:** Wer den Master wann umgelegt hat, wird protokolliert (Aktivitaets-Protokoll /
  `SyncLogServices`, ADR 0010, Service-Name `HierarchieUmstellung`) — bei einer Einwegtuer gehoert
  das nachvollziehbar. Ebenso protokolliert: automatisches Abschalten eines bereits aktiven
  Auto-Erledigt-Schalters, wenn der Master umgelegt wird.
- **Rueckweg** existiert nur als bewusste Datenbereinigung ausserhalb der Anwendung — dokumentiert
  in `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md` (Vorbedingungen inkl. Backup, betroffene Tabellen,
  Bereinigungsschritt, ehrliche Warnung zum Datenverlust), verlinkt aus `README.md` und referenziert
  in der Ablehnungs-Fehlermeldung des Guards („Rueckbau nur ueber das Runbook, siehe
  docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md"). Formulierung durchgaengig **„nicht ueber die Anwendung
  umkehrbar"**, nicht „unmoeglich".

### Synchronisation Struktur → ProductionOrders (drei Sync-Regeln, als Akzeptanzkriterien, nicht Randnotiz)

Die Struktur-Tabelle (`FaHierarchyNode`) ist ein Cache und darf jederzeit komplett neu aufgebaut
werden (Teil 1). Bei den materialisierten `ProductionOrders` gilt das **nicht** — dort haengen
Rueckmeldungen dran:

1. **Neuer Sub-FA taucht auf** → anlegen.
2. **FA verschwindet aus der Struktur** → **NICHT loeschen**, unabhaengig davon, ob bereits
   Rueckmeldungen existieren — beide Faelle (mit und ohne Rueckmeldungen) werden gleich behandelt,
   damit es keine Fallunterscheidung braucht, die spaeter vergessen wird. `SageMissingSince` wird
   gesetzt (Zeitstempel „nicht mehr in Sage"); taucht der FA in einem spaeteren Sync-Lauf wieder
   auf, wird das Feld auf `NULL` zurueckgesetzt (selbstheilend). Zusaetzlich: Eintrag im
   Aktivitaets-Log **und** — nur fuer neu als fehlend erkannte FAs, die **nicht** bereits als
   erledigt bekannt waren — eine **Sammelmeldung pro Sync-Lauf** per Mail (Liste der neu markierten
   FAs), **keine** Einzel-Mail je FA (sonst erzeugt jeder planmaessig aus der Sicht verschwindende
   fertige Auftrag eine taegliche Flut, die niemand mehr liest). Vor dem ersten echten Datenlauf in
   Etappe C zu pruefen: behaelt die IDEAL-Quelle fertige Auftraege oder blendet sie sie aus — danach
   die Melde-Regel schaerfen.
3. **Sub-FA haengt unter einem anderen Vater (Umhaengung)** → darf fachlich **nicht** vorkommen —
   als Invariante behandeln, nicht als Annahme: Der Sync erkennt eine geaenderte `VaterFA` bei
   bereits materialisiertem Sub-FA, **aendert sie nicht stillschweigend**, sondern protokolliert
   und meldet den Fall zur Klaerung.

### B3-Folge: Montage-Abteilung bleibt ausserhalb von ProductionOrders (ENTSCHIEDEN)

`OrderNumber = HauptFA` ist bei Kombinationsgeraeten selbst als **Gruppen**-Schluessel mehrdeutig
(zwei logische Auftraege teilen sich dieselbe `OrderNumber`). **Entschieden (Freigabe-Antwort 1):**
`ProductionOrders` erhaelt **keine** eigene `MontageAbteilung`-Spalte und **keinen**
zusammengesetzten Gruppen-Schluessel — der Gruppen-Schluessel bleibt `OrderNumber`.

Grund: Kombinationsgeraete sind paketweit (ueber alle Teile) bewusst **out of scope**. Teil 1
behandelt sie beim Struktur-Import wie normale Auftraege (Schranke-1-Antwort 1) und fuehrt
`MontageAbteilung` nur **informativ** auf `FaHierarchyOrderInfo` (auftrags-, nicht
positionsbezogen). Ein zusammengesetzter Schluessel `OrderNumber + MontageAbteilung` waere in den
Positionsdaten nicht befuellbar und damit nicht sinnvoll bildbar — ein zusaetzliches Feld auf der
Kern-Tabelle `ProductionOrders` waere zudem eine Kern-Tabellen-Aenderung fuer einen Fall, den wir
bewusst nicht behandeln. Nachzuholen ist das gemeinsam mit
[[2026-08-06-kombinationsgeraete-montageabteilung]], wo der fehlende Positionsschluessel als Wurzel
benannt ist.

### FA-Zusatzinfos-Kollision (groesstes technisches Risiko, PFLICHT-Review)

Seit v1.26.0 matcht `FaZusatzinfoSyncService` `[WA Nummer]` auf `ProductionOrders.OrderNumber` und
verlaesst sich auf Eindeutigkeit. Nach der Inversion ist die Spalte **nicht mehr eindeutig** — ein
`OrderNumber`-Lookup liefert im hierarchischen Modus mehrere Zeilen. Zu pruefen und je Fundstelle
explizit zu entscheiden:

- `FaZusatzinfoSyncService`: Die Schreibseite ist mehrfachtreffer-**faehig** (kein Absturz,
  `GroupBy OrderNumber`, Upsert je Id) seit v1.26.0 — **aber** `Fold 2` (Auto-Erledigt) wendet den
  Komm-Erledigt-Status **je gematchter Zeile** an (`WaNummer`→`OrderNumber`-Match,
  `foreach (var order in matches)`). Im hierarchischen Modus teilen HauptFA und alle Sub-FAs
  dieselbe `OrderNumber` — ohne Sperre wuerde ein „verpackt/abgeholt" am HauptFA den Status der
  **ganzen Gruppe** still auf erledigt setzen. Deshalb **dreistufige Auto-Erledigt-Sperre** (AK 11):
  1. **Datengetrieben (Pflicht):** `Fold 2` wird uebersprungen, sobald der `WaNummer`-Match **mehr
     als eine Zeile** liefert — mit Eintrag im Aktivitaets-/SyncLog (`WaNummer` + Trefferzahl).
     Greift unabhaengig von jedem Schalter und ist die eigentliche Sicherung.
  2. **Schalter:** Der Auto-Erledigt-Schalter laesst sich nicht einschalten, solange hierarchische
     Daten existieren (dieselbe `EXISTS`-Bedingung wie das Einwegtor); ist er bereits an, wird er
     beim Umlegen des Masters automatisch abgeschaltet und das protokolliert.
  3. **Test:** Struktur mit zwei Sub-FAs derselben `OrderNumber` — ein „verpackt" am HauptFA darf
     den Komm-Erledigt-Status der Geschwister **nicht** veraendern.
  AKE bleibt unberuehrt (Match dort immer = 1, `Fold 2` laeuft unveraendert).
- `ProductionOrderReconciler` (FA-Reconciliation, `OrderNumber`-basiertes UPDATE/Stornieren) — neben
  `Fold 2` der zweite der beiden riskantesten UPDATE-Pfade; beide bekommen gezielte hierarchische
  Unit-Tests (AK 12).
- Alle weiteren `OrderNumber`-basierten Single-/First-Lookups im heutigen `main` — bei der
  Code-Recherche fuer diese Spec wurden **mindestens 14 Dateien** mit `OrderNumber`-Vergleich/
  -Lookup identifiziert (`ApplicationDbContext`, `ProductionOrderRepository`,
  `ProductionOrdersController`, `PickingLeitstandController`, `FaWorklistController`,
  `FaCompletionController`, `TrackingController`, `WorkOperationRepository`,
  `ProductionOrderPickingStatusRepository`, `EnaioDmsDocumentRepository`,
  `OseonProductionOrderRepository` u. a.) — **14 ist eine Untergrenze, keine abgeschlossene
  Menge**: das adversariale Review **sweept** den Code, statt eine feste Liste abzuticken, und
  nimmt weitere gefundene Stellen auf. Jede identifizierte Fundstelle bekommt ein eigenes,
  schriftliches Urteil — „unkritisch (Gruppen-Lookup, bleibt `OrderNumber`)" oder „kritisch, auf
  `SubOrderNumber`/`GetAllByFaAndOperationAsync` umgestellt" — mit Begruendung (AK 8).
- **Regel:** eindeutige Lookups → `SubOrderNumber`; Gruppen-Lookups (alle Sub-FAs einer Haupt-FA)
  → `OrderNumber`. Wo ein Eindeutigkeits-Lookup im hierarchischen Modus mehrdeutig werden kann,
  stellt Teil 7 zusaetzlich eine **mengenwertige Variante** bereit (z. B.
  `GetAllByFaAndOperationAsync`) und protokolliert im bestehenden Einzel-Lookup, wenn dieser mehr
  als eine Zeile faende — **ohne das aufrufende Verhalten zu aendern** (AK 10). Die Umstellung der
  Aufrufer auf die mengenwertige Variante ist Aufgabe von Teil 8; Teil 7 bleibt damit fuer sich
  mergebar.

**Vor der Freigabe dieses Teils ist zwingend ein adversariales `/review` auf diese Spec
durchzufuehren** (nicht nur die Spec-Erstellung selbst) — explizit mit Fokus FA-Zusatzinfos-
Kollision, wie in der Backlog-Notiz gefordert.

## Technischer Loesungsentwurf

- `ApplicationDbContext`: `HasIndex(e => e.OrderNumber).IsUnique()` (aktuell Zeile 415) wird
  entfernt/durch einen nicht-eindeutigen Index ersetzt; neuer `HasIndex(e =>
  e.SubOrderNumber).IsUnique()`.
- `ProductionOrder.cs`: neue Properties `SubOrderNumber` (`string`, `[Required]`,
  `[StringLength(100)]`), `ParentSubOrderNumber` (`string?`, `[StringLength(100)]`) und
  `SageMissingSince` (`DateTime?`).
- Materialisierungs-Sync (neuer Service oder Erweiterung von `SageImportService`, TBD im Dev-Lauf):
  liest `FaHierarchyNode` (Teil 1) als Quelle, wendet die drei Sync-Regeln an, schreibt/aktualisiert
  `ProductionOrders`. Laeuft **nur** bei `ProduktionsauftragHierarchisch = true` — bei `false`
  bleibt der bestehende `SageImportService`-Pfad (AKE) unveraendert die einzige Quelle.
- `HierarchischeStrukturGuard` (neuer Domaenen-Service): kapselt die
  `EXISTS(...)`-Sperrbedingung, wird von **jedem** Schreibpfad auf den Master-Key aufgerufen — der
  einzige sanktionierte UI-Schreibpfad ist die neue Umschalt-Seite, der Guard sitzt zusaetzlich als
  Choke-Point am generischen ServiceSettings-Schreibpfad (S7-6/S7-7).
- `HierarchieUmstellungController` + `Views/HierarchieUmstellung/Index.cshtml` (neu): dedizierte
  Seite mit Erklaerung, Zustand, Bestaetigungsdialog beim Einschalten, Sperr-Anzeige mit Begruendung.
  `ServiceSettingsController` und `StandortEinstellungenController` (Teil 6) zeigen den Master nur
  noch lesend mit Link hierher.
- `ProductionOrderRepository`: neue mengenwertige `GetAllByFaAndOperationAsync` (oder passender
  Name je nach bestehender Methode); der bestehende Einzel-Lookup protokolliert (nicht bricht ab),
  wenn er mehr als eine Zeile faende.
- `FaZusatzinfoSyncService`: `Fold 2` uebersprungen bei `WaNummer`-Mehrfachtreffer + SyncLog-Eintrag;
  Auto-Erledigt-Schalter als neuer `ServiceSetting`, geschuetzt durch dieselbe `EXISTS`-Bedingung.

## Migrations-/SQL-Auswirkungen

1. Model → `dotnet ef migrations add InvertProductionOrderHierarchy` (aktueller Timestamp,
   `SubOrderNumber`, `ParentSubOrderNumber`, `SageMissingSince`) → idempotentes
   `SQL/87_InvertProductionOrderHierarchy.sql` mit `OBJECT_ID`/`COL_LENGTH`-Guards fuer die neuen
   Spalten **und** einem `sys.indexes`-Guard fuer den Index-Tausch (die Spalten-Guards greifen nicht
   fuer Indizes, H7-1/H7-3). DDL/Daten-Schritte in **eigenen Batches**, in dieser Reihenfolge:
   1. Spalten additiv, nullable hinzufuegen (`SubOrderNumber`, `ParentSubOrderNumber`,
      `SageMissingSince`).
   2. Backfill-UPDATE `SubOrderNumber = OrderNumber` wo `NULL`.
   3. `SubOrderNumber` auf `NOT NULL` setzen.
   4. Alten Unique-Index auf `OrderNumber` droppen (`sys.indexes`-Guard) und neuen Unique-Index auf
      `SubOrderNumber` anlegen (`sys.indexes`-Guard).
   5. `__EFMigrationsHistory`-Insert in separatem Batch.
2. `SQL/00_FreshInstall.sql` an beiden Stellen nachziehen (Schema-Objekte inkl. neuer Spalten und
   Index + `MigrationId`).
3. **Daten-konvertierend, nicht destruktiv** (Backfill fuellt neue Spalten, loescht nichts) —
   trotzdem als „Kern-Tabelle betroffen, DB-Backup vor Deploy **zwingend**" markieren, weil
   `ProductionOrders` die zentralste Tabelle des Systems ist.
4. `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` ist **tot**: produktiv laeuft ausschliesslich
   `SageImportService` (C#, verifiziert — schreibt `SubOrderNumber = OrderNumber`, sobald die Spalte
   existiert, ueber `SyncWorker`/DI live). Die Datei wird geloescht oder nach
   `SQL/AgentJobs/_archiv/` verschoben, mit Kopfkommentar „AUSSER BETRIEB seit
   <Migrationsdatum>, ersetzt durch `SageImportService` — nicht mehr ausgefuehrt". Der gesamte
   Ordner `SQL/AgentJobs/` wird auf weitere tote Artefakte durchsucht und gleich mitbehandelt. In
   `secondbrain/codebase/` wird vermerkt, dass der Produktionsauftrags-Import ausschliesslich ueber
   `SageImportService` laeuft.
5. **Das Loeschen der Repo-Datei entfernt keinen auf `AKESQL20` eingerichteten SQL-Agent-Job** —
   „die Datei ist tot" und „es ist nichts geplant" sind zwei verschiedene Aussagen. Deshalb
   **verbindlich vor der Migration** (siehe Deploy-Abschnitt) die serverseitige Pruefung, ob ein
   aktiver Job `ProductionOrders` schreibt; falls ja, im selben Wartungsfenster deaktivieren.
6. **Vor dem Dev-Lauf erneut pruefen, ob `SQL/87` noch frei ist.**

## Audit-Feld-Auswirkungen

`ProductionOrder` bleibt `AuditableEntity` — bestehende Audit-Felder unveraendert; `SageMissingSince`
ist ein reines Fachfeld, kein Audit-Feld. Neu: das Umlegen des Master-Schalters (und ein
automatisches Abschalten des Auto-Erledigt-Schalters dabei) wird als eigener Audit-Eintrag im
Aktivitaets-Protokoll erfasst (wer, wann) — kein Feld auf `ProductionOrder` selbst, sondern ein
`SyncLog`-Eintrag des Service-Namens `HierarchieUmstellung` (in `SyncLogServices.All` zu ergaenzen).
Ebenso protokolliert: jeder `Fold 2`-Skip in `FaZusatzinfoSyncService` bei `WaNummer`-Mehrfachtreffer
(eigener Counts-Key im bestehenden Sync-Log dieses Service).

## Akzeptanzkriterien

1. Bei `ProduktionsauftragHierarchisch = false` ist `ProductionOrders` byte-identisch zum
   Vor-Zustand (Backfill hat `SubOrderNumber = OrderNumber` gesetzt, `SageMissingSince` bleibt
   `NULL`, kein Verhaltensunterschied in keinem bestehenden Controller/Repository).
2. Ist der Master `true` und existieren bereits Zeilen mit `OrderNumber <> SubOrderNumber`: ein
   Deaktivierungsversuch auf der Umschalt-Seite ist abgelehnt und protokolliert; die generische
   ServiceSettings-Maske und die Teil-6-Maske bieten fuer den Master **kein** Schreib-Bedienelement
   an (nur Anzeige, Link auf die Umschalt-Seite); ein technischer Schreibversuch ueber den
   generischen ServiceSettings-Weg wird vom Guard ebenfalls abgelehnt und protokolliert.
3. Ist der Master noch nie `true` gewesen ODER `true` ohne hierarchische Daten, bleibt der Schalter
   auf der Umschalt-Seite in beide Richtungen aenderbar.
4. Einschalten des Masters auf der Umschalt-Seite erzwingt die Bestaetigungs-Dialogformulierung aus
   der Notiz (wortgleich oder sinngemaess, Kernaussage „nicht mehr moeglich, vorher auf Kopie
   testen" muss enthalten sein).
5. **Regel 1 (Sync):** ein neuer Sub-FA in der Struktur wird als neuer `ProductionOrder` angelegt.
6. **Regel 2 (Sync):** ein aus der Struktur verschwundener Sub-FA — mit oder ohne vorhandene
   Rueckmeldung — wird **nicht** geloescht, erhaelt `SageMissingSince` (Zeitstempel), ist damit in
   der Oberflaeche als „nicht mehr in Sage" erkennbar, und `SageMissingSince` wird auf `NULL`
   zurueckgesetzt, sobald der FA in einem spaeteren Sync-Lauf wieder auftaucht. Meldung erfolgt als
   **eine** Sammelmail pro Sync-Lauf (nicht je FA) und nur fuer unerwartet verschwundene FAs.
7. **Regel 3 (Sync):** eine erkannte Umhaengung (`ParentSubOrderNumber` weicht vom zuvor
   gespeicherten Wert ab) wird **nicht** automatisch uebernommen, sondern protokolliert und als zu
   klaerender Fall markiert.
8. Jede identifizierte `OrderNumber`-Fundstelle (14 als Untergrenze, kein abgeschlossener Katalog)
   ist im Zuge des adversarialen Reviews einzeln als „unkritisch (Gruppen-Lookup, bleibt
   `OrderNumber`)" oder „kritisch, auf `SubOrderNumber`/`GetAllByFaAndOperationAsync` umgestellt"
   dokumentiert — kein Fundort bleibt unbewertet, weitere im Review gefundene Stellen werden
   ergaenzt.
9. **AKE-Verhalten (Master aus) unveraendert — harte Akzeptanzbedingung fuer jeden Teil.**
10. Wo ein Eindeutigkeits-Lookup im hierarchischen Modus mehrdeutig werden kann, stellt Teil 7 eine
    mengenwertige Variante (`GetAllByFaAndOperationAsync`) bereit und protokolliert im bestehenden
    Einzel-Lookup einen Mehrfachtreffer, **ohne** das aufrufende Verhalten zu aendern (die
    Umstellung der Aufrufer ist Teil 8).
11. Dreistufige Auto-Erledigt-Sperre nachgewiesen: (a) `Fold 2`-Skip + SyncLog-Eintrag bei
    `WaNummer`-Match > 1 Zeile; (b) Auto-Erledigt-Schalter nicht einschaltbar, solange hierarchische
    Daten existieren (dieselbe `EXISTS`-Bedingung), automatisches Abschalten + Protokollierung beim
    Umlegen des Masters; (c) Testfall zwei Sub-FAs derselben `OrderNumber` — ein „verpackt" am
    HauptFA veraendert den Komm-Erledigt-Status der Geschwister nicht.
12. `HierarchischeStrukturGuard` (`EXISTS`-Bedingung) und die drei Sync-Regeln sind als reine,
    unit-getestete Planer umgesetzt (Muster `ProductionOrderReconciler`), plus gezielte
    hierarchische Unit-Tests fuer die zwei riskantesten UPDATE-Pfade (Reconcile-`WHERE OrderNumber`
    und FA-Zusatzinfo-`Fold 2`).
13. Vor der Migration ist die serverseitige Pruefung „kein aktiver SQL-Agent-Job schreibt
    `ProductionOrders` auf `AKESQL20`" durchgefuehrt und das Ergebnis dokumentiert (Deploy-
    Vorbedingung, siehe Deploy-Abschnitt).
14. Der Index-Tausch (`OrderNumber` unique entfernen / `SubOrderNumber` unique anlegen) erfolgt mit
    einem `sys.indexes`-Guard, nicht nur `OBJECT_ID`/`COL_LENGTH` — das Migrationsskript ist bei
    wiederholtem Lauf idempotent.
15. `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md` existiert, ist aus `README.md` verlinkt und wird in der
    Ablehnungs-Fehlermeldung des Guards referenziert.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 7 — Materialisierung / Einweg-Migrationstor":

- **Backfill-Regression:** vor/nach der Migration auf einer AKE-Testkopie —
  `ProductionOrders`-Zeilenzahl, alle bestehenden Listen/Filter identisch, `SageMissingSince`
  durchgehend `NULL`.
- **Vor-Migration-Check (Deploy-Vorbedingung):** die `sysjobs`-Abfrage aus dem Deploy-Abschnitt auf
  einer produktionsnahen Kopie ausfuehren, Ergebnis dokumentieren.
- **Master einschalten ohne Daten:** auf der Umschalt-Seite erscheint der Bestaetigungsdialog,
  Umschalten gelingt, Schalter bleibt danach (ohne hierarchische Daten) noch rueckgaengig machbar.
- **Master sperren:** nach einem hierarchischen Materialisierungs-Lauf (Testdaten mit
  `OrderNumber <> SubOrderNumber`) ist der Schalter auf der Umschalt-Seite schreibgeschuetzt; ein
  Schreibversuch ueber die generische ServiceSettings-Maske wird vom Guard abgelehnt und
  protokolliert; zusaetzlicher Test, dass **weder** die generische **noch** die Teil-6-Maske ein
  Schreib-Bedienelement fuer den Master anbieten (ersetzt den bisherigen Test „Deaktivierung ueber
  Teil-6-Maske abgelehnt" — Teil 6 kann den Master gar nicht schreiben).
- **Sync-Regel 2:** ein Sub-FA (mit und getrennt ohne vorhandene Rueckmeldung) verschwindet aus der
  Quelle → `SageMissingSince` gesetzt statt Loeschung, Rueckmeldedaten bleiben abrufbar; taucht der
  FA wieder auf → `SageMissingSince` wird `NULL`; Sammelmail enthaelt genau die neu markierten,
  unerwarteten FAs, keine Einzel-Mail je FA.
- **Sync-Regel 3:** simulierte Umhaengung (`VaterFA` geaendert) → Protokoll-Eintrag, keine
  stille Uebernahme.
- **FA-Zusatzinfos-Kollision / Auto-Erledigt-Sperre:** Struktur mit zwei Sub-FAs derselben
  `OrderNumber` — `FaZusatzinfoSyncService` ueberspringt `Fold 2` mit SyncLog-Eintrag, der
  Auto-Erledigt-Schalter laesst sich nicht einschalten (bzw. schaltet sich beim Umlegen des Masters
  automatisch ab), `ProductionOrderReconciler` behandelt beide Sub-FAs korrekt (kein falscher
  Treffer, kein Datenverlust).
- **GetAllByFaAndOperationAsync:** Mehrfachtreffer im bestehenden Einzel-Lookup fuehrt zu einem
  Log-Eintrag, das Terminal-/Aufrufer-Verhalten bleibt unveraendert (Teil 7 aendert es nicht).
- **Idempotenz Migrationsskript:** `SQL/87_InvertProductionOrderHierarchy.sql` zweimal gegen dieselbe
  Datenbank ausgefuehrt bricht nicht (Spalten- **und** Index-Guards greifen).

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

- **Web-App:** ja (neue Umschalt-Seite, read-only Master-Anzeige in generischer und Teil-6-Maske).
- **Service:** ja (Materialisierungs-Sync, angepasster `ProductionOrderReconciler`/
  `FaZusatzinfoSyncService`).
- **Migration:** ja, **daten-konvertierend** — **DB-Backup vor Deploy zwingend** (Kern-Tabelle
  `ProductionOrders`).
- **Harte Vorbedingung vor der Migration:** serverseitig pruefen, ob ein aktiver SQL-Agent-Job
  `ProductionOrders` beschreibt:
  ```sql
  SELECT j.name, j.enabled, s.command
  FROM msdb.dbo.sysjobs j
  JOIN msdb.dbo.sysjobsteps s ON s.job_id = j.job_id
  WHERE s.command LIKE '%ProductionOrders%';
  ```
  Gefundene aktive Jobs werden **deaktiviert oder entfernt**, bevor die Migration laeuft — sonst
  feuert ein vergessener Job nach der Inversion in die `NOT NULL`-Spalte. Das Loeschen/Archivieren
  der Repo-Datei `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` beseitigt nur die Irrefuehrung im
  Code, nicht ein moegliches Betriebsrisiko auf dem SQL-Server.
- **Reihenfolge:** DB-Backup → serverseitige Agent-Job-Pruefung (s. o.) → **Windows-Service
  stoppen** → Migration → Service-Neustart → Web-Deploy. Der Master bleibt nach dem Deploy
  **default aus** (`false`) — die eigentliche Umstellung ist ein bewusster, spaeterer manueller
  Schritt am Zielsystem, nicht Teil des Deploys selbst.
- **Irreversibilitaet beachten (Etappe A):** Etappe A vollzieht bereits die Schema-Inversion selbst
  (Spalten, Backfill, Index-Tausch) unabhaengig vom Master-Zustand. Wird das Epic nach Etappe A
  abgebrochen, ist die DB-Struktur bereits invertiert — das ist inhaerent (kein Fehler), aber im
  Runbook und in der Deploy-Planung zu benennen.
- **Publish-Befehle:**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
  (provisorisch, vom Dev-Lauf gegen den tatsaechlichen Diff zu bestaetigen).

## Offene Rueckfragen

1. B3/B-4 — wo lebt Montage-Abteilung in `ProductionOrders`? Eigene Spalte + zusammengesetzter
   Gruppen-Schluessel `OrderNumber + MontageAbteilung`, oder reicht ein anderer Mechanismus?
   Haengt an der Teil-1-Rueckfrage 1.
2. Vollstaendigkeit des adversarialen Reviews der 14 identifizierten `OrderNumber`-Fundstellen —
   diese Spec listet sie, entscheidet aber nicht einzeln (das ist Aufgabe des geforderten
   `/review`-Laufs).
3. Exaktes Timing/Caching der `EXISTS(...)`-Sperrbedingungs-Pruefung (Performance-Impact auf jeden
   Zugriffspfad, der den Master-Wert liest).
4. Wo wird der dokumentierte „Rueckweg ausserhalb der Anwendung" (Datenbereinigung) fest verankert
   (README, eigenes Runbook)?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → **Keine Montage-Abteilung in `ProductionOrders`. Gruppen-Schluessel bleibt `OrderNumber`.**
   Konsistent zur paketweiten Entscheidung, dass Kombinationsgeraete in allen Teilen **out of
   scope** sind: Es gibt keinen Trennschluessel auf Positionsebene, also laesst sich auch kein
   zusammengesetzter Schluessel `OrderNumber + MontageAbteilung` sinnvoll bilden — er waere in den
   Positionsdaten nicht befuellbar. Die Montage-Abteilung bleibt dort, wo sie hergehoert: auf der
   auftragsbezogenen Struktur-Seite (`FaHierarchyOrderInfo`), als Information.
   Ein zusaetzliches Feld in `ProductionOrders` waere zudem eine Kern-Tabellen-Aenderung fuer einen
   Fall, den wir bewusst nicht behandeln. Nachzuholen ist das gemeinsam mit dem Backlog-Eintrag
   [[2026-08-06-kombinationsgeraete-montageabteilung]] — dort ist der fehlende Positionsschluessel
   als Wurzel benannt.

2. → **Der adversariale `/review`-Lauf ist harte Freigabe-Vorbedingung, kein Nice-to-have.**
   Diese Spec listet die 14 `OrderNumber`-Fundstellen bewusst nur auf; die Einzelbewertung ist
   Aufgabe des Reviews. Verbindlich:
   - `/review` auf diese Spec **vor** der Freigabe; ohne abgeschlossenen Lauf wird Teil 7 nicht
     nach `freigegeben/` verschoben.
   - Jede der 14 Fundstellen bekommt ein **eigenes schriftliches Urteil** —
     `SubOrderNumber` (eindeutig) / `OrderNumber` (Gruppe) / unveraendert korrekt, jeweils mit
     Begruendung. **Kein Fundort bleibt unbewertet** (das ist bereits AK 8).
   - Findet das Review weitere Fundstellen, werden sie aufgenommen — die Liste ist eine Untergrenze,
     keine abgeschlossene Menge.

3. → **Sperrbedingung: live nur auf dem Schreibpfad, gecacht fuer die Anzeige.**
   Das `EXISTS(SELECT 1 FROM ProductionOrders WHERE OrderNumber <> SubOrderNumber)` darf **nicht**
   bei jedem Lesen des Master-Werts laufen — der Wert wird potenziell in jedem Request gelesen, das
   waere eine Abfrage pro Zugriffspfad.
   - **Schreibpfad (Aenderungsversuch am Master):** Pruefung **live** in derselben Transaktion wie
     die Aenderung. Nur hier zaehlt Aktualitaet, und hier ist eine Abfrage voellig unkritisch
     (passiert einmal im Leben des Systems).
   - **Anzeige (Schalter gesperrt darstellen, Hinweistext):** **gecachter** Zustand, aufgefrischt
     nach jedem Materialisierungs-Lauf und beim Start. Eine kurzzeitig veraltete Anzeige ist
     harmlos — der Schreibpfad laesst nichts durch.
   - Der Cache-Refresh gehoert an das Ende des Materialisierungs-Syncs, nicht an einen Timer.

4. → **Eigenes Runbook unter `docs/`, verlinkt aus README und dieser Spec.**
   Der "Rueckweg ausserhalb der Anwendung" (Datenbereinigung nach dem Umlegen des Einwegtors) darf
   nicht nur als Satz in einer Spec stehen — gesucht wird er im Ernstfall Monate spaeter, von
   jemandem unter Druck.
   Verbindlich: `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md` mit Vorbedingungen (Backup!),
   den betroffenen Tabellen, dem Bereinigungsschritt und einer ehrlichen Warnung, was dabei
   verloren geht. Aus `README.md` verlinken und in der Fehlermeldung des Waechters referenzieren
   ("Rueckbau nur ueber das Runbook, siehe docs/..."). Formulierung durchgaengig **"nicht ueber die
   Anwendung umkehrbar"**, nicht "unmoeglich".

## Kritische Pruefung (2026-08-06)

Anwalt-des-Teufels-Durchsicht **nach** ausgefuellten Freigabe-Antworten (1-4), gegengelesen: diese
Spec, Teil 1 + Teil 8 (Abhaengigkeiten), die Ideen-Notiz inkl. ihrer eigenen Kritischen Pruefung,
ADR 0003/0004/0006/0010, `fallstricke.md` — und der **echte main-Code**:
`ApplicationDbContext.cs` (Zeile 415 verifiziert), `ProductionOrderReconciler.cs`,
`SageImportService.cs`, `SageProductionOrderSql.cs`, `FaZusatzinfoSyncService.cs`,
`SQL/AgentJobs/01_Import_Produktionsauftraege.sql`, `barcode-scanner.js`. Das Migrationstor ist
konzeptionell exzellent — aber es gibt einen produktionsgefaehrdenden Code-Befund, ein
Groessen-Problem und mehrere Konsistenzluecken, die eine Freigabe **jetzt** verhindern.

### BLOCKER — vor der Freigabe zu klaeren

**B7-1 — Der AKE-Import-AgentJob bricht nach der Migration (NOT-NULL-Verletzung), nicht nur der
MERGE-Key.** `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` fuegt neue FAs per
`INSERT (OrderNumber, Quantity, …)` ein (Zeilen 92-97) — **ohne `SubOrderNumber`**. Die
Schema-Inversion macht `SubOrderNumber` **unbedingt** (auch bei AKE, „nicht schaltbar") `NOT NULL
UNIQUE`. Ab dem ersten neuen AKE-FA nach der Migration schlaegt der AgentJob-INSERT fehl — der
Sage-Import auf der **Produktivlinie AKE** steht still. Die Spec (Migrations-Punkt 4) framed das als
„MERGE mit dem neuen `SubOrderNumber`-Upsert-Key kollidiert" und verweist auf einen bereits
vorbereiteten Fallstrick — dieser ist aber in `SageProductionOrderSql.BuildUpsert` (C#) vorbereitet,
**nicht** im AgentJob-SQL. Zu entscheiden/zu fixen, bevor Teil 7 laeuft:
- **Welcher Import-Pfad ist an AKE live?** Der SQL-Agent-Job (raw SQL) oder `SageImportService` (C#,
  der `SubOrderNumber=OrderNumber` bereits schreibt)? Die Auto-Memory sagt fuer BomCache „raw-SQL
  ist Produktion" — trifft das auch auf den ProductionOrder-Import zu, ist B7-1 ein harter
  Deploy-Blocker.
- Ist der AgentJob live: seinen INSERT auf `SubOrderNumber = OrderNumber` spiegeln (analog
  `SageProductionOrderSql`) **und** ihn im selben Wartungsfenster deployen — die Deploy-Reihenfolge
  in dieser Spec nennt das nicht.
- Deploy-Reihenfolge ergaenzen: den geplanten **SQL-Agent-Job waehrend des Migrations-Fensters
  deaktivieren**, sonst feuert er mitten in die Migration und schlaegt fehl.
- Folge fuer AK 1/AK 9: „`ProductionOrders` byte-identisch / AKE-Verhalten unveraendert" ist
  **erst dann** erfuellt, wenn der AgentJob mitgezogen ist. Solange nicht: die harte
  Regressionsgarantie ist **verletzt**, nicht nur theoretisch.

**B7-2 — Groesse: das ist ein Epic, kein Ein-Dev-Lauf (`epic: false` ist falsch gesetzt).**
In-Scope buendelt: (1) daten-konvertierende Migration der zentralsten Tabelle, (2)
`HierarchischeStrukturGuard` + Einwegtor mit Dialogen ueber mehrere Schreibwege, (3) neuer
Materialisierungs-Sync mit drei Sync-Regeln, (4) Haertung von 14+ `OrderNumber`-Lookups, (5)
adversariales FA-Zusatzinfos-Review mit ggf. Code-Aenderung, (6) AgentJob-Umbau (B7-1), (7)
Runbook, (8) gecachter Anzeige-Zustand + Refresh, (9) Audit-SyncLog-Service, (10) FreshInstall.
Das ist **umfangreicher als Teil 8**, der bewusst `epic: true` mit sechs Etappen ist. Ein einziger
Dev-Lauf produziert einen riesigen, schwer reviewbaren PR auf der Kern-Tabelle mit hohem
Rollback-Risiko. **Entweder** `epic: true` mit Etappen (z. B. A Schema+Migration+AgentJob / B
Guard+Einwegtor / C Materialisierungs-Sync+3 Regeln / D Lookup-Haertung+FA-Zusatzinfos-Review / E
Runbook+Doku) **oder** eine harte, begruendete Rechtfertigung, warum das in einem Lauf sicher
schaffbar ist. So wie jetzt spezifiziert: nicht schaffbar/nicht sicher.

**B7-3 — Der Spec-Text widerspricht seiner eigenen maßgeblichen Freigabe-Antwort 1 (B3).**
Freigabe-Antwort 1 **entscheidet** B3 klar: keine `MontageAbteilung` in `ProductionOrders`,
Gruppen-Schluessel bleibt `OrderNumber`, Kombinationsgeraete paketweit out of scope (Backlog
[[2026-08-06-kombinationsgeraete-montageabteilung]] — **existiert**, Link ok). Aber der **Body**
(§„B3-Folge (NICHT abschliessend entschieden)") **und** das Frontmatter-Feld `open_questions[0]`
fuehren B3 weiterhin als **offen/unentschieden**. Ein Dev, der den Body liest, sieht „nicht
entschieden" und raet erneut. Vor Freigabe: Body-Abschnitt auf die getroffene Entscheidung
umschreiben und **alle vier** `open_questions` streichen — sie sind durch Antworten 1-4
vollstaendig beantwortet (das Feld steuert das HOME-Dashboard; stehen sie drin, gilt die Spec
faelschlich als offen).

### SOLLTE — macht den Dev-Lauf sicherer

**S7-1 — Die Entlastung der FA-Zusatzinfos-Schreibseite ist sachlich falsch.** Die Spec sagt, das
„Restrisiko liegt im UPDATE-Pfad der FA-Reconciliation, **nicht** in dieser Klasse". Verifiziert am
Code: `FaZusatzinfoSyncService` matcht `WaNummer` auf `OrderNumber` und wendet **je gematchter
Zeile** nicht nur den Upsert, sondern **Fold 2 (Auto-Erledigt)** an (Zeilen 165-172, 192-231). Im
hierarchischen Modus teilen HauptFA **und alle Sub-FAs** dieselbe `OrderNumber` → ein einziges
„verpackt/abgeholt" am HauptFA setzt den Komm-Erledigt-Status der **gesamten Gruppe** (jeder Sub-FA)
still auf erledigt. „Mehrfachtreffer-faehig" heisst hier nur „stuerzt nicht ab", **nicht**
„fachlich korrekt". Diese Klasse gehoert ausdruecklich ins adversariale Review (mit
hierarchischem Testfall), nicht in die Entlastung.

**S7-2 — Sync-Regel 2 ist zweifach unterspezifiziert.** (a) „Status setzen (nicht mehr in Sage)":
**welches Feld?** `ProductionOrder` kennt heute nur `IsDone`/`IsCancelled`. Wird `IsCancelled`
wiederverwendet oder ein neues Feld angelegt? Ein neues Feld = **weitere Migrations-Spalte**, die im
Migrations-Plan fehlt. (b) Der **komplementaere Fall** fehlt ganz: Sub-FA verschwindet aus der
Struktur **ohne** Rueckmeldungen — loeschen, behalten, markieren? Beide Punkte vor der Umsetzung
festlegen, sonst ratet der Dev.

**S7-3 — Keine einzige automatisierte-Test-Anforderung.** Alle Test-Szenarien sind Manual-UAT.
`HierarchischeStrukturGuard` (EXISTS-Bedingung) und die drei Sync-Regeln sind reine Entscheidungs-
logik — genau das Muster, das `ProductionOrderReconciler` als unit-testbaren Baustein umsetzt. AK
ergaenzen: Guard-Bedingung + die drei Sync-Regeln als reine, unit-getestete Planer; **plus**
gezielte hierarchische Tests fuer die zwei riskantesten UPDATE-Pfade (Reconcile-`WHERE OrderNumber`
und FA-Zusatzinfo-Fold-2). Sonst ist die „Build+Tests gruen"-Mindestbedingung ohne Aussage fuer den
neuralgischen Teil.

**S7-4 — `affected_code` ist gegen die verbindlichen Antworten unvollstaendig.** Antwort 4 macht
`docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md` **und** die README-Verlinkung verbindlich — beide fehlen in
`affected_code` (nur `docs/TESTSZENARIEN.md` steht drin). Ergaenzen. Zusaetzlich: die
serverseitige Scan-Aufloesung (mehrdeutige `OrderNumber` → Auswahl) gehoert laut Teil-8-Freigabe-
Antwort 1 **zu Teil 8**; `barcode-scanner.js` steht aber in Teil-7-`affected_code`. Klarstellen,
dass Teil 7 nur Schema/Lookup-Semantik anfasst und die Aufloesungs-UI Teil 8 gehoert — sonst
Doppel-Ownership.

**S7-5 — „14 Fundstellen" ist eine Unterschaetzung; AK 8 liest sich als abgeschlossene Menge.**
Breite Grep-Suche zeigt `OrderNumber` in deutlich mehr als 14 Quelldateien — u. a.
`BdeBookingService`, `BdeApiController`, `PickingController`, `PartRequisitionsController`,
`BdeBookingsController`, `ArticlesController`, `ReadOnlyBomBuilder` —, die nicht in `affected_code`
stehen (einige sind Teil 8/BDE). Antwort 2 haelt korrekt fest „Untergrenze, keine abgeschlossene
Menge" — aber AK 8 formuliert „**jeder der 14** … dokumentiert" und liest sich als fixe Zahl. AK 8
umformulieren: „alle identifizierten Fundstellen; die Zahl 14 ist eine Untergrenze, das Review
**sweept**, tickt keine feste Liste ab."

**S7-6 — Guard-Choke-Point unverifiziert.** Der Waechter soll „ueber jeden Schreibweg" greifen.
Der einzige heutige Schreibweg ist `ServiceSettingsController` (verifiziert vorhanden); die
Teil-6-Maske existiert noch nicht. Vor der Umsetzung sicherstellen, dass es einen **einzigen
Service-Layer-Choke-Point** fuer ServiceSettings-Writes gibt, an dem der Guard sitzt — kann der
Key an mehreren Stellen ohne gemeinsame Naht geschrieben werden, leckt die Einweg-Garantie.

### HINWEIS

**H7-1 — Idempotenz-Guard des Index-Umbaus.** Der Tausch „`OrderNumber` unique entfernen /
`SubOrderNumber` unique anlegen" braucht einen **`sys.indexes`-Guard**, nicht nur
`OBJECT_ID`/`COL_LENGTH` (die Spalten-Guards greifen fuer die Spalten, nicht fuer die Indizes).

**H7-2 — Frontmatter/Anzeige.** Nach Aufloesung von B7-3 sind `open_questions` zu leeren; solange
sie stehen, zeigt das HOME-Dashboard Teil 7 als offen.

**H7-3 — Staerken (beibehalten).** Datengetriebenes Einwegtor
(`EXISTS(OrderNumber<>SubOrderNumber)`), Waechter in der Domaenenschicht, Antwort-3-Trennung
live-Schreibpfad vs. gecachte Anzeige (Performance sauber geloest), Backup-Pflicht, verbindliches
Runbook, korrekte Identifikation des Reconcile-UPDATE-Pfads als groesstes Risiko — alles
vorbildlich. Der referenzierte Backlog-Eintrag existiert (verifiziert). Die Loecher sind ein
Code-Blocker (B7-1), ein Zuschnitt-Problem (B7-2) und Konsistenz-/Vollstaendigkeits-Nachbesserungen
— keine Neukonzeption.

NACHBESSERUNG NOETIG: AgentJob-INSERT bricht die AKE-Produktivlinie nach der Migration (B7-1);
Zuschnitt ist Epic-gross, aber `epic: false` (B7-2); Spec-Body + `open_questions` widersprechen der
maßgeblichen Freigabe-Antwort 1 zu B3 (B7-3).

## Antworten auf die Kritische Pruefung (2026-08-06)

**Zu B7-1 — ENTWARNUNG: Den SQL-Agent-Job gibt es nicht mehr. Produktiv laeuft ausschliesslich
`SageImportService` (C#).** Damit ist B7-1 **kein Deploy-Blocker**: Der C#-Pfad schreibt
`SubOrderNumber = OrderNumber` bereits, die AKE-Produktivlinie bricht nach der Migration nicht.

**Aber die Datei ist eine Landmine und muss weg.** `SQL/AgentJobs/01_Import_Produktionsauftraege.sql`
liegt weiterhin im Repo und beschreibt einen Import, den es nicht mehr gibt. Sie hat eine sorgfaeltige
Pruefung in die Irre gefuehrt — ein Dev-Lauf oder ein kuenftiger Agent wird denselben Fehlschluss
ziehen, und beim naechsten Mal faellt er vielleicht nicht auf. Verbindlich fuer Etappe A:
- Datei **loeschen** oder nach `SQL/AgentJobs/_archiv/` verschieben **mit Kopfkommentar**
  ("AUSSER BETRIEB seit <Datum>, ersetzt durch SageImportService — nicht mehr ausgefuehrt").
- Den gesamten Ordner `SQL/AgentJobs/` auf weitere tote Artefakte durchsehen und gleich mit
  behandeln.
- In `secondbrain/codebase/` vermerken, dass der Produktionsauftrags-Import ausschliesslich ueber
  `SageImportService` laeuft — damit kuenftige Laeufe nicht wieder raten muessen.
Die Deploy-Reihenfolge braucht entsprechend **keine** Agent-Job-Deaktivierung; stattdessen den
**Windows-Service** waehrend des Migrationsfensters stoppen (steht bereits so drin).

**Zu B7-2 — `epic: true` mit fuenf Etappen. [ENTSCHIEDEN]**
Frontmatter auf `epic: true` und Etappen-Tabelle ergaenzen:
| # | Etappe |
|---|---|
| A | Schema-Inversion + Migration + Backfill + FreshInstall + tote AgentJob-Artefakte |
| B | `HierarchischeStrukturGuard` + Einwegtor (Schreibpfad live, Anzeige gecacht) + Runbook |
| C | Materialisierungs-Sync + die drei Sync-Regeln (als unit-testbare Planer) |
| D | Lookup-Haertung (`OrderNumber`-Sweep) + adversariales FA-Zusatzinfos-Review |
| E | Doku, README-Verlinkung, Testszenarien, Brain-Update |
Ein langlebiger Worktree, kein Zwischen-Merge, `sync-worktree.ps1` haelt den Branch auf main-Stand.

**Zu B7-3 — Body und `open_questions` werden auf die Antworten gezogen. [Nachbesserung]**
Der Abschnitt „B3-Folge (NICHT abschliessend entschieden)" wird auf die getroffene Entscheidung
umgeschrieben (keine `MontageAbteilung` in `ProductionOrders`, Gruppen-Schluessel bleibt
`OrderNumber`). **Alle vier `open_questions` werden geleert** — sie sind durch die Antworten 1-4
vollstaendig beantwortet und steuern sonst das HOME-Dashboard falsch.

**Zu S7-1 — Befund akzeptiert. Auto-Erledigt wird fuer hierarchische Auftraege GESPERRT.**
Die Entlastung dieser Klasse war falsch — danke fuer den Code-Nachweis. Auflösung: Auto-Erledigt ist
ein **eigener Schalter** und soll bei hierarchischen Auftraegen **derzeit gar nicht aktivierbar**
sein. Drei Ebenen, absichtlich redundant:
1. **Datengetrieben (Pflicht):** `FaZusatzinfoSyncService` Fold 2 wird uebersprungen, sobald der
   `WaNummer`-Match **mehr als eine Zeile** liefert — mit Eintrag im Aktivitaets-/SyncLog
   (`WaNummer` + Trefferzahl). Das greift unabhaengig von jedem Schalter und ist die eigentliche
   Sicherung.
2. **Schalter:** Der Auto-Erledigt-Schalter laesst sich nicht einschalten, solange hierarchische
   Daten existieren (dieselbe `EXISTS`-Bedingung wie das Einwegtor); ist er bereits an, wird er
   beim Umlegen des Masters abgeschaltet und das protokolliert.
3. **Test:** Ein gezielter Testfall — Struktur mit zwei Sub-FAs derselben `OrderNumber`, ein
   „verpackt" am HauptFA darf den Komm-Erledigt-Status der Geschwister **nicht** veraendern.
Die Klasse wandert damit aus der Entlastung in Etappe D (adversariales Review).

**Zu S7-2a — Neues Feld, aber als Zeitstempel: `SageMissingSince` (datetime2 NULL).**
`IsCancelled` wird **nicht** wiederverwendet — „storniert" und „verschwindet aus der Quelle" sind
fachlich verschieden, und die Vermischung waere spaeter nicht mehr aufloesbar.
Warum ein Zeitstempel statt eines Bool (`SageCancelled`): Er beantwortet zusaetzlich **seit wann**,
traegt damit Auswertungen („seit X Tagen verschwunden") und ist **selbstheilend** — taucht der FA
wieder auf, wird das Feld auf `NULL` gesetzt, statt dass ein Bool haengenbleibt. Der Name sagt,
was es ist (fehlt in der Quelle), nicht, was man vermutet (storniert).
Die Spalte ist eine **zusaetzliche Migrationsspalte** und gehoert in den Migrations-Plan von
Etappe A — dort fehlt sie bisher.

**Zu S7-2b — Verschwundene Sub-FAs OHNE Rueckmeldungen: ebenfalls markieren, nie loeschen.**
Gleiche Behandlung wie mit Rueckmeldungen (`SageMissingSince` setzen), zusaetzlich Eintrag im
Aktivitaetslog und Benachrichtigung per Mail. **Nie loeschen** — die Regel ist damit einheitlich
und braucht keine Fallunterscheidung.

> **Warnung zur Mail — vor der Umsetzung zu klaeren:** Enthaelt die IDEAL-View nur **aktive**
> Auftraege, verschwindet **jeder fertige Auftrag** planmaessig aus der Quelle. Eine Mail je
> verschwundenem FA erzeugte dann taeglich eine Flut, und nach zwei Wochen liest sie niemand mehr —
> genau dann, wenn die eine wichtige Meldung darin steht.
> Vorgabe: **eine Sammelmeldung pro Sync-Lauf** (Liste der neu als fehlend markierten FAs), nicht
> eine Mail je FA. Und **nur melden, was unerwartet ist** — ein FA, der als erledigt bekannt ist
> und verschwindet, wird protokolliert, aber nicht gemailt. Beim ersten echten Datenlauf pruefen,
> ob die View fertige Auftraege behaelt oder ausblendet; die Filterregel danach schaerfen.

**Zu S7-3 bis S7-6 und H7-1/H7-2 — alle uebernommen, ohne Einschraenkung:**
- **S7-3:** Guard-Bedingung und die drei Sync-Regeln als **reine, unit-getestete Planer** (Muster
  `ProductionOrderReconciler`), plus gezielte hierarchische Tests fuer die zwei riskantesten
  UPDATE-Pfade (Reconcile-`WHERE OrderNumber`, FA-Zusatzinfo-Fold-2). Als AK aufnehmen.
- **S7-4:** `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md` und `README.md` in `affected_code`;
  `barcode-scanner.js` **entfernen** — die Scan-Aufloesungs-UI gehoert laut Teil-8-Antwort 1 zu
  Teil 8. Teil 7 fasst nur Schema und Lookup-Semantik an.
- **S7-5:** AK 8 umformulieren — „**alle identifizierten** Fundstellen; 14 ist eine Untergrenze,
  das Review **sweept**, es tickt keine feste Liste ab".
- **S7-6:** Vor Etappe B pruefen, dass es **einen einzigen Service-Layer-Choke-Point** fuer
  ServiceSettings-Writes gibt. Existiert er nicht, wird er in Etappe B **geschaffen** — ohne
  gemeinsame Naht leckt die Einweg-Garantie, und Teil 6 wuerde spaeter einen zweiten Weg aufmachen.
- **H7-1:** Index-Tausch mit `sys.indexes`-Guard, nicht nur `OBJECT_ID`/`COL_LENGTH`.
- **H7-2:** erledigt mit B7-3 (`open_questions` leeren).

## Entscheidungen zu den Rest-Blockern (2026-08-07)

**Master-Umschaltung: eigene Seite in Teil 7. Ueberall sonst nur lesend. [ENTSCHEIDUNG]**
Aufloesung des Widerspruchs zwischen der Teil-6-Antwort („Maske read-only") und dem Teil-7-Rumpf
(„Teil-6-Maske als gesicherter Schreibweg"): **Keins von beiden gewinnt — beide werden ersetzt.**
- **Teil 7 baut eine eigene, kleine Umschalt-Seite** („Umstellung auf hierarchische
  Produktionsauftraege") mit Erklaerung, Bestaetigungsdialog und dem Domaenen-Waechter dahinter.
  Ein Einwegtor gehoert nicht als Kontrollkaestchen zwischen zwanzig andere Einstellungen — weder
  in die generische Maske noch in die Standorteinstellungen.
- **Generische ServiceSettings-Maske UND Teil-6-Maske zeigen den Master ausschliesslich
  read-only** (Zustand, Sperrstatus, seit wann) mit Link auf diese Seite. Der Waechter in der
  Domaenenschicht bleibt die eigentliche Sicherung und lehnt jeden anderen Schreibweg ab.
- **Testszenario anpassen:** Der Guard-Test laeuft gegen die generische Maske (Schreibversuch wird
  abgelehnt und protokolliert) **plus** ein Test, dass weder generische noch Teil-6-Maske ein
  Schreib-Bedienelement fuer den Master anbieten. Der bisherige Test „Deaktivierung ueber
  Teil-6-Maske abgelehnt" entfaellt in dieser Form.

**Liefergrenze zu Teil 8: Teil 7 sagt die mengenwertige Variante ausdruecklich zu. [ENTSCHEIDUNG]**
AK 8 ist heute binaer formuliert (Urteil je Fundstelle) und verspricht `GetAllByFaAndOperationAsync`
nicht — Teil 8 baut aber darauf auf. **Zusaetzliches AK in Teil 7:**
> Wo ein Eindeutigkeits-Lookup im hierarchischen Modus mehrdeutig werden kann, stellt Teil 7 eine
> **mengenwertige Variante** bereit (z. B. `GetAllByFaAndOperationAsync`) und protokolliert im
> bestehenden Einzel-Lookup, wenn er mehr als eine Zeile faende — **ohne das aufrufende Verhalten
> zu aendern**. Die Umstellung der Aufrufer auf die mengenwertige Variante gehoert zu Teil 8.

Damit bleibt Teil 7 fuer sich mergebar (das Terminal laeuft unveraendert weiter) und Teil 8 findet
die Naht vor, statt sie in fremdem Gebiet zu schlagen.

**AgentJob: serverseitige Pruefung kommt als harte Deploy-Vorbedingung ZURUECK. [KORREKTUR]**
Der Einwand ist berechtigt, mein Streichen war voreilig: **Eine `.sql`-Datei zu loeschen entfernt
keinen auf AKESQL20 eingerichteten Job.** „Die Datei ist tot" und „es ist nichts geplant" sind zwei
verschiedene Aussagen — belegt ist nur die erste.
Verbindlich **vor** der Migration pruefen:
```sql
SELECT j.name, j.enabled, s.command
FROM msdb.dbo.sysjobs j
JOIN msdb.dbo.sysjobsteps s ON s.job_id = j.job_id
WHERE s.command LIKE '%ProductionOrders%';
```
Gefundene aktive Jobs, die `ProductionOrders` schreiben, werden **deaktiviert oder entfernt**, bevor
die Migration laeuft — sonst feuert ein vergessener Job nach der Inversion in die `NOT NULL`-Spalte.
Die Datei-Bereinigung im Repo bleibt zusaetzlich bestehen: Sie beseitigt die Irrefuehrung, nicht das
Betriebsrisiko.

**`SageMissingSince` in den Migrationsplan aufnehmen. [NACHTRAG]**
Die in S7-2a entschiedene Spalte (`datetime2 NULL`) fehlt bisher in **jeder** Migrationsstufe. Sie
gehoert in Etappe A, zusammen mit der Schema-Inversion — idempotent per `COL_LENGTH`-Guard, plus
`00_FreshInstall.sql`.

**Frontmatter-Korrekturen (mechanisch, mit der Nachbesserung):** `epic: true`, Etappen A–E
eintragen, alle vier `open_questions` leeren, `barcode-scanner.js` aus `affected_code` entfernen und
die am Code widerlegte Behauptung „QR traegt an Index 2 die BelID" streichen.

## Kritische Pruefung (2026-08-07)

Zweiter Anwalt-des-Teufels-Durchgang, Kern-Teil, **nach** dem Antwortblock „Antworten auf die
Kritische Pruefung (2026-08-06)". Gegengelesen: dieser Antwortblock gegen den Spec-Rumpf und das
Frontmatter, Teil-1- und Teil-8-Spec (inkl. deren Antwortbloecke), Ideen-Notiz, ADR 0003/0004/0010,
`fallstricke.md`, sowie der **echte main-Code**: `SageImportService.cs`, `SageProductionOrderSql.cs`,
`SQL/AgentJobs/01_Import_Produktionsauftraege.sql`, `FaZusatzinfoSyncService.cs`,
`ProductionOrderReconciler.cs`, `ApplicationDbContext.cs` (Zeile 415 verifiziert),
`barcode-scanner.js`, `SyncWorker.cs`. Die **Sachentscheidungen** im Antwortblock sind ganz
ueberwiegend richtig und am Code belegbar — aber **keine einzige davon ist in den Spec-Rumpf oder
das Frontmatter eingearbeitet**. Eine Freigabe jetzt gaebe dem Dev ein Dokument in die Hand, dessen
Koerper der eigenen Entscheidungslage widerspricht.

### BLOCKER — vor der Freigabe zu klaeren

**B7-4 — Rumpf und Frontmatter sind durchgaengig stale; die Antworten leben nur im Anhang.**
Der Antwortblock trifft die Entscheidungen, aber der maßgebliche Teil der Spec (Frontmatter +
Body) ist unveraendert der Vor-Antwort-Stand. Konkret nicht nachgezogen:
- **`epic: false` (Zeile 40) + `etappen: []` (Zeile 41)** — obwohl B7-2 „`epic: true` mit fuenf
  Etappen [ENTSCHIEDEN]" sagt. Das Frontmatter steuert das Tooling: `epic: false` routet den Lauf
  auf den `dev`-Skill („Nicht fuer Epics"), nicht auf `epic-stage`. Die Etappen-Tabelle A–E steht
  nur im Fliesstext des Antwortblocks, nicht im `etappen:`-Feld.
- **`open_questions` (Zeilen 35–39) noch alle vier vorhanden** — B7-3 sagt „Alle vier werden
  geleert". Solange sie stehen, zeigt das HOME-Dashboard Teil 7 als offen (das war H7-2).
- **§„B3-Folge (NICHT abschliessend entschieden)" (Zeilen 132–143) unveraendert** — B7-3 sagt, der
  Abschnitt werde auf die getroffene Entscheidung (keine `MontageAbteilung`, Gruppen-Schluessel
  bleibt `OrderNumber`) umgeschrieben. Ein Dev, der den Body liest, sieht „nicht entschieden" und
  raet erneut — exakt der Fehler, den B7-3 verhindern wollte.
- **`barcode-scanner.js` steht noch in `affected_code` (Zeile 27)** mit dem Zusatz
  „Index 2 = BelID" — obwohl S7-4 (dieser Spec) **und** die Teil-8-Antwort dessen Entfernung
  verlangen. Zusaetzlich steht die **am Code widerlegte Behauptung** noch im Body (Zeilen 165–166:
  „QR traegt an Index 2 die BelID = kuenftig `SubOrderNumber`"). Verifiziert in
  `barcode-scanner.js` Zeile 289: der Kommentar lautet woertlich „FA-Nummer aus QR extrahieren
  (Index 2)", der Client behandelt Index 2 als HauptFA/OrderNumber, **nie** als BelID. Die falsche
  Aussage muss gestrichen werden (so verlangt es die Teil-8-Antwort ausdruecklich fuer Teil 7).
- **`docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md` und `README.md` fehlen in `affected_code`** — obwohl
  Antwort 4 + S7-4 sie verbindlich machen.
Fazit: Der Antwortblock ist gut, aber er ist noch nicht die Spec. Vor Freigabe muessen alle
Antworten in Frontmatter + Body **eingearbeitet** sein — sonst ist die Freigabe eine Freigabe des
alten, widerspruechlichen Standes.

**B7-5 — `SageMissingSince` ist entschieden (S7-2a), aber in keiner ADR-0004-Stufe abgebildet.**
Der Antwortblock legt eine neue Spalte `SageMissingSince` (`datetime2 NULL`) fest und schreibt
selbst: „gehoert in den Migrations-Plan von Etappe A — dort fehlt sie bisher." Verifiziert: Sie
fehlt in **jeder** Stufe der Pflicht-Kette:
- nicht in `affected_code` (kein `ProductionOrder.cs`-Property genannt),
- nicht im Abschnitt „Migrations-/SQL-Auswirkungen" (Zeilen 190–204 kennen nur die Inversion),
- nicht in `SQL/00_FreshInstall.sql`-Nachzug,
- nicht in Sync-Regel 2 / AK 6 („nicht mehr in Sage"-Status — das Feld wird nirgends benannt),
- kein AK, das das Setzen/Zuruecksetzen (`NULL` bei Wiederauftauchen, „selbstheilend" laut S7-2a)
  prueft.
Nach ADR 0004 ist das Model → Migration → idempotentes SQL (`sys.columns`/`COL_LENGTH`-Guard) →
FreshInstall an **zwei** Stellen, plus Verankerung in Sync-Regel 2 und AK. Bis das im Body steht,
ist Etappe A unvollstaendig spezifiziert und der Dev muss die zweite Migrationsspalte erraten.

### SOLLTE — macht den Dev-Lauf sicherer

**S7-7 — B7-1-Entwarnung ist im Kern verifiziert, aber die entfernte Schutzmaßnahme ist
gefaehrlich.** Verifiziert und **korrekt**: `SageProductionOrderSql.BuildUpsert(true)` schreibt
`SubOrderNumber` im INSERT-Zweig (Spalten- + Werte-Liste), `SageImportService` setzt
`SubOrderNumber = OrderNumber`, sobald `COL_LENGTH('dbo.ProductionOrders','SubOrderNumber')`
vorhanden ist (Zeilen 106–117), und dieser C#-Pfad ist ueber `SyncWorker` (Zeile 40) + DI
(`Program.cs` Zeile 51) nachweislich **live**. Der reine NOT-NULL-Bruch aus B7-1 ist damit
entschaerft — die Entwarnung trifft insoweit zu. **Aber:** Die Behauptung „produktiv laeuft
**ausschliesslich** SageImportService, den Agent-Job gibt es nicht mehr" ist aus dem Repo **nicht**
verifizierbar — die Datei `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` existiert weiter, ihr
INSERT (Zeilen 92–97) fuehrt `SubOrderNumber` **nicht**, und die Auto-Memory dokumentiert fuer
BomCache den **umgekehrten** Praezedenzfall („raw-SQL ist Produktion, EF-Methode tot"). Der
Antwortblock hat daraufhin die „Agent-Job-Deaktivierung" aus der Deploy-Reihenfolge **gestrichen**.
Das Loeschen der Repo-Datei (die vorgesehene Maßnahme) unscheduled keinen auf `AKESQL20`
deployten SQL-Agent-Job. Vorgabe vor Freigabe: die **serverseitige Pruefung** „kein SQL-Agent-Job
fuer den ProductionOrder-Import ist auf `AKESQL20` aktiviert" als **harte Deploy-Vorbedingung**
wieder aufnehmen (nicht ersetzen durch das bloße Loeschen der Datei) — und falls doch einer laeuft,
im Migrationsfenster deaktivieren.

**S7-8 — S7-1-Befund am Code bestaetigt; die dreistufige Sperre ist schluessig, aber die
Schalter-Landschaft ist nicht abzaehlbar.** Verifiziert: `FaZusatzinfoSyncService` liest
`orders` per `waNumbers.Contains(o.OrderNumber)` (Zeile 92), gruppiert nach `OrderNumber` (Zeilen
95–97) und wendet Fold 2 in `foreach (var order in matches)` (Zeile 116) **je gematchter Zeile** an
(Done-Check Zeilen 165–172). Im hierarchischen Modus teilen HauptFA + alle Sub-FAs dieselbe
`OrderNumber` → ein „verpackt/abgeholt" am HauptFA setzt `IsDonePicking` der **ganzen Gruppe**. Der
Befund S7-1 ist also real, die Entlastung war falsch, der Antwortblock akzeptiert das korrekt. Die
dreistufige Sperre (datengetriebener Fold-2-Skip bei Match > 1 + Schalter + Test) ist in sich
widerspruchsfrei und AKE-sicher (bei AKE ist Match immer = 1, Fold 2 laeuft unveraendert). **Offen:**
Der neue „Auto-Erledigt-Schalter" (Stufe 2) ist ein zusaetzlicher `ServiceSetting` — `affected_code`
Zeile 29 nennt aber nur „Master + 3 abhaengige Schalter", und **welche** drei das sind, wird in
dieser Spec nirgends aufgezaehlt (verwiesen auf „Uebersichts-Rueckfrage 2"). Ist der Auto-Erledigt-
Schalter einer der drei oder ein vierter? Vor Etappe B/D die Schalter-Liste **explizit** benennen,
sonst rät der Dev die Schalter-Inventur.

**S7-9 — Regressionsgarantie AKE haengt an B7-5 und der noch fehlenden Test-Verankerung.** AK 1/AK 9
(„byte-identisch / AKE-Verhalten unveraendert") sind erst pruefbar, wenn (a) `SageMissingSince` als
`NULL`-Spalte ohne Verhaltensaenderung nachgewiesen ist (B7-5) und (b) die von S7-3 zugesagten
Unit-Tests fuer den Reconcile-`WHERE OrderNumber`-Pfad und Fold-2 tatsaechlich als AK stehen —
derzeit steht S7-3 nur im Antwortblock, nicht als AK im Body. `ProductionOrderReconciler` ist
verifiziert ein reiner Planer auf `OrderNumber`-Listen (Zeilen 51/57) — genau der unit-testbare
Baustein, den S7-3 meint; die Testpflicht muss aber in die AK-Liste, nicht in den Anhang.

### HINWEIS

**H7-3 — H7-1 (`sys.indexes`-Guard) noch nicht im Body.** Der Migrationsabschnitt (Zeilen 193–195)
nennt weiterhin nur `OBJECT_ID`/`COL_LENGTH`-Guards; der im Antwortblock zugesagte
`sys.indexes`-Guard fuer den Index-Tausch fehlt im eigentlichen Plan.

**H7-4 — Etappen-Zuschnitt A–E plausibel, Einweg-Charakter beachten.** Fuenf Etappen in einem
langlebigen Worktree ohne Zwischen-Merge (wie Teil 8) sind fuer diesen Umfang angemessen. Bewusst
sein: Etappe A vollzieht die **irreversible** Schema-Inversion; wird das Epic nach A abgebrochen,
ist die DB bereits invertiert (inhaerent, kein Fehler — aber im Runbook/Deploy zu benennen).

**H7-5 — Staerken (beibehalten).** Der referenzierte Backlog
[[2026-08-06-kombinationsgeraete-montageabteilung]] existiert (verifiziert). Die Sachentscheidungen
des Antwortblocks (Auto-Erledigt-Sperre, `SageMissingSince` als selbstheilender Zeitstempel statt
Bool, Sammelmail statt Flut, Guard-Choke-Point, `barcode-scanner.js`-Entfernung) sind fachlich
richtig und teils vorbildlich begruendet. Die Nachbesserung ist reine **Einarbeitung** der bereits
getroffenen Entscheidungen in Frontmatter + Body, kein neues Konzept.

NACHBESSERUNG NOETIG: Antworten sind sachlich gut, aber nicht in die Spec eingearbeitet — Frontmatter
+ Body sind stale (`epic: false`, 4 `open_questions`, `barcode-scanner.js` mit widerlegter
„Index 2 = BelID", B3-Folge weiter „unentschieden") (B7-4); `SageMissingSince` fehlt in jeder
ADR-0004-Stufe (B7-5); die serverseitige Agent-Job-Pruefung wurde als Deploy-Schutz entfernt,
obwohl die „ausschliesslich C#"-Annahme aus dem Repo nicht verifizierbar ist (S7-7).

### Nachbesserung 2 (2026-08-07)

Beide Antwortbloecke (2026-08-06 und 2026-08-07) sind jetzt vollstaendig in Frontmatter, Rumpf und
Akzeptanzkriterien eingearbeitet, nicht nur als Anhang dokumentiert:

- **B7-4:** `epic: true` + Etappen A–E stehen im Frontmatter (`etappen:`-Feld, nicht nur Fliesstext);
  alle vier `open_questions` sind geleert; „B3-Folge" ist auf die getroffene Entscheidung (keine
  `MontageAbteilung`, Gruppen-Schluessel bleibt `OrderNumber`, ENTSCHIEDEN) umgeschrieben;
  `barcode-scanner.js` ist aus `affected_code` und aus dem Rumpf entfernt, die am Code widerlegte
  Behauptung „Index 2 = BelID" gestrichen (Out-of-Scope-Absatz verweist stattdessen auf Teil 8);
  `docs/RUNBOOK-FA-HIERARCHIE-RUECKBAU.md` und `README.md` stehen in `affected_code`.
- **B7-5:** `SageMissingSince` (`datetime2 NULL`) ist durchgaengig verankert — als Property in
  `affected_code`/Technischer Loesungsentwurf, als eigene Spalte in der Migrations-Reihenfolge von
  Etappe A (`COL_LENGTH`-Guard, `00_FreshInstall.sql` an beiden Stellen), in Sync-Regel 2 (Setzen
  bei Verschwinden, `NULL` bei Wiederauftauchen) und in AK 6/AK 14.
- **S7-7 (AgentJob):** die serverseitige `sysjobs`-Pruefung ist als harte Deploy-Vorbedingung im
  Deploy-Abschnitt **und** als AK 13 wieder aufgenommen — zusaetzlich, nicht anstelle der
  Datei-Archivierung (Migrationsabschnitt Punkt 4/5).
- **H7-3 (sys.indexes-Guard):** im Migrationsabschnitt (Reihenfolge-Schritt 4) und in AK 14 verankert.
- **H7-4 (Irreversibilitaet Etappe A):** als eigener Absatz im Deploy-Abschnitt benannt.
- **Master-Schreibwege (Entscheidung 2026-08-07):** die dedizierte Umschalt-Seite
  (`HierarchieUmstellungController`) ist der einzige UI-Schreibweg; generische ServiceSettings- und
  Teil-6-Maske sind im Rumpf, in `affected_code` und in AK 2 als ausschliesslich read-only
  spezifiziert. Das Testszenario „Deaktivierung ueber Teil-6-Maske abgelehnt" ist entsprechend durch
  „weder generische noch Teil-6-Maske bieten ein Schreib-Bedienelement" ersetzt.
- **Liefergrenze zu Teil 8 (`GetAllByFaAndOperationAsync`):** als eigenes AK 10 mit Logging-Pflicht
  im bestehenden Einzel-Lookup aufgenommen, Umstellung der Aufrufer bleibt ausdruecklich Teil 8.
- **S7-1/Auto-Erledigt-Sperre:** die dreistufige Sperre ist als AK 11 sowie im Abschnitt
  „FA-Zusatzinfos-Kollision" ausformuliert.

**Verbleibender, nicht blockierender Klaerungspunkt (S7-8):** Welche Schalter genau die „weiteren
abhaengigen Schalter" neben Master und dem neuen Auto-Erledigt-Schalter sind, ist in dieser Spec
weiterhin nicht abschliessend aufgezaehlt — das war nicht Gegenstand der bisherigen Freigabe-
Antworten. `affected_code` markiert das ausdruecklich als vor Etappe B/D zu klaerende Inventur-
Aufgabe, kein Freigabe-Blocker. Ebenso als Hinweis, nicht als Blocker uebernommen: ob die
IDEAL-Sage-Quelle fertige Auftraege behaelt oder ausblendet, wird erst am ersten echten Datenlauf in
Etappe C geprueft (siehe Sync-Regel 2); die Melde-Regel ist bis dahin bewusst konservativ
(Sammelmeldung, nur Unerwartetes) angelegt.
