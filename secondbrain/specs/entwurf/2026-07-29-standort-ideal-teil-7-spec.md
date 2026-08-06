---
type: spec
title: "IDEAL-Standort Teil 7 — Materialisierung nach ProductionOrders (Schema-Inversion, Einweg-Migrationstor)"
slug: 2026-07-29-standort-ideal-teil-7-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-06
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Models/ProductionOrder.cs
  - IdealAkeWms/Data/ApplicationDbContext.cs (Index-Umbau OrderNumber -> SubOrderNumber)
  - IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs
  - IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs
  - IdealAkeWms/Controllers/ProductionOrdersController.cs
  - IdealAkeWms/Controllers/PickingLeitstandController.cs
  - IdealAkeWms/Controllers/FaWorklistController.cs
  - IdealAkeWms/Controllers/FaCompletionController.cs
  - IdealAkeWms/Controllers/TrackingController.cs
  - IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs (adversariales Review + ggf. Anpassung, PFLICHT)
  - IDEALAKEWMSService/Services/ProductionOrderReconciler.cs (adversariales Review + ggf. Anpassung, PFLICHT)
  - IDEALAKEWMSService/Services/SageImportService.cs
  - IDEALAKEWMSService/Services/SageProductionOrderSql.cs
  - IdealAkeWms/wwwroot/js/barcode-scanner.js (QR-/Scan-Lookup, Index 2 = BelID)
  - IdealAkeWms/Services/HierarchischeStrukturGuard.cs (neu, Domaenen-Waechter des Einweg-Tors)
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs (Master + 3 abhaengige Schalter)
  - SQL/87_InvertProductionOrderHierarchy.sql (neu, naechste freie Nummer — vor Dev-Lauf pruefen)
  - SQL/00_FreshInstall.sql
  - SQL/AgentJobs/01_Import_Produktionsauftraege.sql (Review, ob Folge-MERGEs betroffen)
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "B3/B-4: Wo lebt Montage-Abteilung im ProductionOrders-Modell? OrderNumber = HauptFA ist bei Kombinationsgeraeten selbst als GRUPPEN-Schluessel mehrdeutig (zwei logische Auftraege, gleiche OrderNumber) — braucht ProductionOrders eine eigene MontageAbteilung-Spalte und einen zusammengesetzten Gruppen-Schluessel OrderNumber+MontageAbteilung?"
  - "Vollstaendigkeit der adversarialen FA-Zusatzinfos-Kollisionspruefung: alle Single-/First-Lookups auf OrderNumber in den 14 identifizierten Fundstellen (siehe technischer Loesungsentwurf) sind vor der Freigabe einzeln durchzugehen — diese Spec listet sie, bewertet sie aber nicht abschliessend"
  - "Exakte Sperrbedingung-Formel: EXISTS(...) laut Notiz eindeutig, aber WANN wird sie geprueft (bei jedem Request? gecacht? bei jedem Schreibversuch auf den Setting-Key)? Performance-Impact auf jeden Zugriffspfad, der den Master liest"
  - "Rueckweg-Doku: wo genau wird die 'bewusste Datenbereinigung ausserhalb der Anwendung' dokumentiert (README? eigenes Runbook?) — noch offen"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Ab Teil 7 wird der Kern angefasst: Aus `FaHierarchyNode` (Teil 1) werden die Zeilen mit
`SubFA != 0` (plus Wurzel) als echte `ProductionOrders` materialisiert. Erst dadurch werden
Sub-FAs rueckmeldefaehig (Arbeitsgaenge, Teileverfolgung, BDE — Teil 8), weil diese Module
ausschliesslich gegen `ProductionOrders` arbeiten. Die Struktur bleibt die Quelle, `ProductionOrders`
ist abgeleitet (Transformation, kein zweiter Import) — die beiden laufen so nicht auseinander, und
die Transformation ist ohne DB testbar.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Schema-Inversion (`OrderNumber` nicht mehr unique, `SubOrderNumber` unique,
`ParentSubOrderNumber` nullable), Backfill fuer AKE-Bestandsdaten, Einweg-Migrationstor
(Master-Schalter `ProduktionsauftragHierarchisch`), Materialisierungs-Sync (Struktur →
`ProductionOrders`), die drei Sync-Regeln (nicht loeschen bei Rueckmeldungen; Umhaengung nicht
still uebernehmen; neuer Sub-FA anlegen), Durchzug von `SubOrderNumber` durch
Repositories/Controller/Scan-Lookups, adversariales Review der FA-Zusatzinfos-Kollision.

**Out-of-Scope:** die eigentliche BDE-/Rueckmelde-Logik (Teil 8); Teil 2–6 (Listen/Anzeige) bleiben
unveraendert auf `FaHierarchyNode` aufgesetzt und sind von dieser Materialisierung **nicht**
abhaengig (B5).

## Fachliche Anforderungen

### Schema-Inversion (unbedingt, nicht schaltbar)

- `OrderNumber` = FA-Nummer (Sage StrukturID / `HauptFA`) — **nicht mehr unique**.
- `SubOrderNumber` (neu, `NOT NULL`) = Sub-FA-Nummer (Sage BelID) — **unique**.
- `ParentSubOrderNumber` (neu, nullable) = `VaterFA` — echter Elternzeiger, weil `OrderNumber`/
  `SubOrderNumber` allein nur zwei Ebenen ausdruecken koennen (Wurzel + Kinder), aber laut B1 ist
  die Struktur mehrstufig (Sub-FA kann unter einem anderen Sub-FA haengen).
- Hauptauftrag genau dann, wenn `OrderNumber == SubOrderNumber`.
- **Backfill:** `SubOrderNumber = OrderNumber` fuer alle Bestandszeilen (AKE bleibt damit faktisch
  eindeutig — `OrderNumber == SubOrderNumber` ist im flachen Modus eine Invariante, Verhalten
  identisch zu heute).
- Das Schema-Modell ist **nicht** schaltbar — `SubOrderNumber` kann nicht mal unique sein und mal
  nicht. Die Inversion passiert einmalig und unbedingt fuer die gesamte Tabelle, unabhaengig vom
  Master-Schalter-Zustand.

### Der Master ist ein Migrationstor, kein Betriebsschalter (EINWEG) — exakt aus der Notiz uebernommen

Master-Schalter `ProduktionsauftragHierarchisch` (bool, Default `false`) plus drei davon
abhaengige Betriebsschalter (siehe Uebersichts-Rueckfrage 2 zur Abhaengigkeits-/Heimatfrage dieser
drei — hier nur der Master selbst behandelt):

- **Sperrbedingung datengetrieben, nicht schaltergetrieben:** gesperrt genau dann, wenn
  `EXISTS(SELECT 1 FROM ProductionOrders WHERE OrderNumber <> SubOrderNumber)`. Wer versehentlich
  umstellt und es vor dem ersten hierarchischen Import merkt, kann gefahrlos zurueck.
- **Der Waechter gehoert in die Service-/Domaenenschicht, nicht in die Maske.** Eine zentrale
  Uebergangspruefung (`HierarchischeStrukturGuard`) greift ueber jeden Schreibweg (generische
  `ServiceSettings`-Maske, Teil-6-Standorteinstellungen-Maske, kuenftige API) — die Dialoge sind
  nur die Umgangsform darueber.
- **Beim Einschalten** Bestaetigung erzwingen: *„Umstellung auf hierarchische
  Produktionsauftraege. Nach dem ersten hierarchischen Import ist eine Rueckkehr zur flachen
  Struktur nicht mehr moeglich. Vorher auf einer Datenbank-Kopie testen. Wirklich umstellen?"*
- **Solange noch keine hierarchischen Daten existieren:** Schalter bleibt aenderbar, mit Hinweis,
  dass die Sperre mit dem ersten Import greift.
- **Sobald hierarchische Daten existieren:** Schalter wird schreibgeschuetzt angezeigt mit
  Begruendung — *„Kann nicht mehr deaktiviert werden: es liegen Auftraege mit Sub-FA-Struktur
  vor."* Ein Aenderungsversuch ueber einen anderen Weg wird abgelehnt und protokolliert.
- **Audit:** Wer den Master wann umgelegt hat, wird protokolliert (Aktivitaets-Protokoll /
  `SyncLogServices`, ADR 0010) — bei einer Einwegtuer gehoert das nachvollziehbar.
- **Rueckweg** existiert nur als bewusste Datenbereinigung ausserhalb der Anwendung (dokumentiert,
  nicht per Klick). Ehrlich benannt: „nicht ueber die Anwendung umkehrbar", nicht „physikalisch
  unmoeglich".

### Synchronisation Struktur → ProductionOrders (drei Sync-Regeln, als Akzeptanzkriterien, nicht Randnotiz)

Die Struktur-Tabelle (`FaHierarchyNode`) ist ein Cache und darf jederzeit komplett neu aufgebaut
werden (Teil 1). Bei den materialisierten `ProductionOrders` gilt das **nicht** — dort haengen
Rueckmeldungen dran:

1. **Neuer Sub-FA taucht auf** → anlegen.
2. **FA verschwindet aus der Struktur, hat aber schon Rueckmeldungen** → **NICHT loeschen.**
   Status setzen („nicht mehr in Sage") und in der Oberflaeche kenntlich machen. Ein Sync, der
   loeschen darf, vernichtet Buchungsdaten.
3. **Sub-FA haengt unter einem anderen Vater (Umhaengung)** → darf fachlich **nicht** vorkommen —
   als Invariante behandeln, nicht als Annahme: Der Sync erkennt eine geaenderte `VaterFA` bei
   bereits materialisiertem Sub-FA, **aendert sie nicht stillschweigend**, sondern protokolliert
   und meldet den Fall zur Klaerung.

### B3-Folge (NICHT abschliessend entschieden — siehe offene Rueckfrage 1)

`OrderNumber = HauptFA` ist bei Kombinationsgeraeten selbst als **Gruppen**-Schluessel mehrdeutig
(zwei logische Auftraege teilen sich dieselbe `OrderNumber`). Diese Spec **listet** das Problem
und die moeglichen Loesungsrichtungen (eigene `MontageAbteilung`-Spalte auf `ProductionOrder` +
zusammengesetzter Gruppen-Schluessel), entscheidet es aber **nicht** — es ist bewusst hierher
verlagert: **Teil 1 behandelt Kombinationsgeraete beim Struktur-Import wie normale Auftraege**
(Schranke-1-Antwort 1) und fuehrt `MontageAbteilung` **nur informativ** auf `FaHierarchyOrderInfo`
(nicht auf `FaHierarchyNode`). Damit steht als Eingangslage fest: Die Montage-Abteilung ist
**auftrags-, nicht positionsbezogen** verfuegbar — die Materialisierung muss daher entscheiden, ob
`ProductionOrders` eine eigene `MontageAbteilung`-Spalte und einen zusammengesetzten Gruppen-
Schluessel `OrderNumber + MontageAbteilung` erhaelt.

### FA-Zusatzinfos-Kollision (groesstes technisches Risiko, PFLICHT-Review)

Seit v1.26.0 matcht `FaZusatzinfoSyncService` `[WA Nummer]` auf `ProductionOrders.OrderNumber` und
verlaesst sich auf Eindeutigkeit. Nach der Inversion ist die Spalte **nicht mehr eindeutig** — ein
`OrderNumber`-Lookup liefert im hierarchischen Modus mehrere Zeilen. Zu pruefen und je Fundstelle
explizit zu entscheiden:

- `FaZusatzinfoSyncService` (Schreibseite bereits mehrfachtreffer-faehig seit v1.26.0 — `GroupBy
  OrderNumber`, Upsert je Id; **Restrisiko liegt im UPDATE-Pfad der FA-Reconciliation**, nicht in
  dieser Klasse).
- `ProductionOrderReconciler` (FA-Reconciliation, `OrderNumber`-basiertes UPDATE/Stornieren).
- Alle weiteren `OrderNumber`-basierten Single-/First-Lookups im heutigen `main` — bei der
  Code-Recherche fuer diese Spec wurden **14 Dateien** mit `OrderNumber`-Vergleich/-Lookup
  identifiziert (`ApplicationDbContext`, `ProductionOrderRepository`,
  `ProductionOrdersController`, `PickingLeitstandController`, `FaWorklistController`,
  `FaCompletionController`, `TrackingController`, `WorkOperationRepository`,
  `ProductionOrderPickingStatusRepository`, `EnaioDmsDocumentRepository`,
  `OseonProductionOrderRepository` u. a.) — **jede einzelne** ist vor der Freigabe dieses Teils
  daraufhin zu pruefen, ob sie eine Eindeutigkeitsannahme trifft, die im hierarchischen Modus
  bricht (siehe offene Rueckfrage 2).
- Scan-/QR-Lookups (`barcode-scanner.js`): QR traegt an Index 2 die BelID = kuenftig
  `SubOrderNumber` — muss auf den neuen eindeutigen Schluessel umgestellt werden, wo Eindeutigkeit
  gebraucht wird (siehe B2/B-5-Regel unten).
- **Regel:** eindeutige Lookups → `SubOrderNumber`; Gruppen-Lookups (alle Sub-FAs einer Haupt-FA)
  → `OrderNumber`.

**Vor der Freigabe dieses Teils ist zwingend ein adversariales `/review` auf diese Spec
durchzufuehren** (nicht nur die Spec-Erstellung selbst) — explizit mit Fokus FA-Zusatzinfos-
Kollision, wie in der Backlog-Notiz gefordert.

## Technischer Loesungsentwurf

- `ApplicationDbContext`: `HasIndex(e => e.OrderNumber).IsUnique()` (aktuell Zeile 415) wird
  entfernt/durch einen nicht-eindeutigen Index ersetzt; neuer `HasIndex(e =>
  e.SubOrderNumber).IsUnique()`.
- `ProductionOrder.cs`: neue Properties `SubOrderNumber` (`string`, `[Required]`,
  `[StringLength(100)]`) und `ParentSubOrderNumber` (`string?`, `[StringLength(100)]`).
- Materialisierungs-Sync (neuer Service oder Erweiterung von `SageImportService`, TBD im Dev-Lauf):
  liest `FaHierarchyNode` (Teil 1) als Quelle, wendet die drei Sync-Regeln an, schreibt/aktualisiert
  `ProductionOrders`. Laeuft **nur** bei `ProduktionsauftragHierarchisch = true` — bei `false`
  bleibt der bestehende `SageImportService`-Pfad (AKE) unveraendert die einzige Quelle.
- `HierarchischeStrukturGuard` (neuer Domaenen-Service): kapselt die
  `EXISTS(...)`-Sperrbedingung, wird von **jedem** Schreibpfad auf den Master-Key aufgerufen
  (generische ServiceSettings-Maske, Teil-6-Maske, kuenftige API).

## Migrations-/SQL-Auswirkungen

1. Model → `dotnet ef migrations add InvertProductionOrderHierarchy` (aktueller Timestamp) →
   idempotentes `SQL/87_InvertProductionOrderHierarchy.sql` mit `OBJECT_ID`/`COL_LENGTH`-Guards,
   DDL in eigenem Batch, Backfill-UPDATE (`SubOrderNumber = OrderNumber` wo NULL) **vor** dem
   Setzen von `NOT NULL`/`UNIQUE`, `__EFMigrationsHistory`-Insert in separatem Batch.
2. `SQL/00_FreshInstall.sql` an beiden Stellen nachziehen (Schema + `MigrationId`).
3. **Daten-konvertierend, nicht destruktiv** (Backfill fuellt eine neue Spalte, loescht nichts) —
   trotzdem als „Kern-Tabelle betroffen, DB-Backup vor Deploy" markieren, weil `ProductionOrders`
   die zentralste Tabelle des Systems ist.
4. `SQL/AgentJobs/01_Import_Produktionsauftraege.sql`: Review, ob der bestehende MERGE-Job mit
   dem neuen `SubOrderNumber`-Upsert-Key kollidiert (Fallstrick „`SubOrderNumber` nur schreiben,
   wenn die Spalte existiert" ist bereits vorbereitet, aber die Merge-Logik selbst muss auf den
   neuen Key umgestellt werden) — im **selben Wartungsfenster**.
5. **Vor dem Dev-Lauf erneut pruefen, ob `SQL/87` noch frei ist.**

## Audit-Feld-Auswirkungen

`ProductionOrder` bleibt `AuditableEntity` — bestehende Audit-Felder unveraendert. Neu:
das Umlegen des Master-Schalters wird als eigener Audit-Eintrag im Aktivitaets-Protokoll erfasst
(wer, wann) — kein Feld auf `ProductionOrder` selbst, sondern ein `SyncLog`-Eintrag eines eigenen
„Service"-Namens (z. B. `HierarchieUmstellung`, in `SyncLogServices.All` zu ergaenzen).

## Akzeptanzkriterien

1. Bei `ProduktionsauftragHierarchisch = false` ist `ProductionOrders` byte-identisch zum
   Vor-Zustand (Backfill hat `SubOrderNumber = OrderNumber` gesetzt, kein Verhaltensunterschied in
   keinem bestehenden Controller/Repository).
2. Ist der Master `true` und existieren bereits Zeilen mit `OrderNumber <> SubOrderNumber`, ist ein
   Deaktivierungsversuch über **jeden** Schreibweg (generische Maske, Teil-6-Maske) abgelehnt und
   protokolliert.
3. Ist der Master noch nie `true` gewesen ODER `true` ohne hierarchische Daten, bleibt der Schalter
   in beide Richtungen aenderbar.
4. Einschalten des Masters erzwingt die Bestaetigungs-Dialogformulierung aus der Notiz (wortgleich
   oder sinngemaess, Kernaussage „nicht mehr moeglich, vorher auf Kopie testen" muss enthalten
   sein).
5. **Regel 1 (Sync):** ein neuer Sub-FA in der Struktur wird als neuer `ProductionOrder` angelegt.
6. **Regel 2 (Sync):** ein aus der Struktur verschwundener, aber bereits ruckgemeldeter Sub-FA wird
   **nicht** geloescht, sondern erhaelt einen sichtbaren „nicht mehr in Sage"-Status.
7. **Regel 3 (Sync):** eine erkannte Umhaengung (`ParentSubOrderNumber` weicht vom zuvor
   gespeicherten Wert ab) wird **nicht** automatisch uebernommen, sondern protokolliert und als zu
   klaerender Fall markiert.
8. Jeder der 14 identifizierten `OrderNumber`-Fundstellen ist im Zuge des adversarialen Reviews
   einzeln als „unkritisch (Gruppen-Lookup, bleibt OrderNumber)" oder „kritisch, auf
   SubOrderNumber umgestellt" dokumentiert — kein Fundort bleibt unbewertet.
9. AKE-Verhalten (Master aus) unveraendert — harte Akzeptanzbedingung fuer jeden Teil.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 7 — Materialisierung / Einweg-Migrationstor":

- **Backfill-Regression:** vor/nach der Migration auf einer AKE-Testkopie —
  `ProductionOrders`-Zeilenzahl, alle bestehenden Listen/Filter identisch.
- **Master einschalten ohne Daten:** Bestaetigungsdialog erscheint, Umschalten gelingt, Schalter
  bleibt danach (ohne hierarchische Daten) noch rueckgaengig machbar.
- **Master sperren:** nach einem hierarchischen Materialisierungs-Lauf (Testdaten mit
  `OrderNumber <> SubOrderNumber`) ist der Schalter schreibgeschuetzt — Versuch ueber die
  generische Maske UND (sobald vorhanden) die Teil-6-Maske beide abgelehnt und protokolliert.
- **Sync-Regel 2:** ein Sub-FA mit vorhandener Rueckmeldung verschwindet aus der Quelle → Status
  „nicht mehr in Sage" statt Loeschung, Rueckmeldedaten bleiben abrufbar.
- **Sync-Regel 3:** simulierte Umhaengung (`VaterFA` geaendert) → Protokoll-Eintrag, keine
  stille Uebernahme.
- **FA-Zusatzinfos-Kollision:** Struktur mit zwei Sub-FAs derselben `OrderNumber` — FA-Reconciliation
  und `FaZusatzinfoSyncService` behandeln beide korrekt (kein falscher Treffer, kein Datenverlust).

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

- **Web-App:** ja.
- **Service:** ja (Materialisierungs-Sync, ggf. angepasster `ProductionOrderReconciler`/
  `FaZusatzinfoSyncService`).
- **Migration:** ja, **daten-konvertierend** — **DB-Backup vor Deploy zwingend** (Kern-Tabelle
  `ProductionOrders`).
- **Reihenfolge:** DB-Backup → Migration (Service gestoppt) → Service-Neustart → Web-Deploy. Der
  Master bleibt nach dem Deploy **default aus** (`false`) — die eigentliche Umstellung ist ein
  bewusster, spaeterer manueller Schritt am Zielsystem, nicht Teil des Deploys selbst.
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

1. →
2. →
3. →
4. →
