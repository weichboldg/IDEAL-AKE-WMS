---
typ: feature
---
# IDEAL-Buendel: Code-Review-Nachlese (2026-09-08)

Vollstaendiger Code-Review des ungemergten Buendels (Teile 1–8 + FA-Struktur + BOM-Guard +
FA-Liste-Hierarchie, Branch `feature/2026-08-07-ideal-teile-1-5` gegen merge-base `2a34ff4`,
179 Dateien / ~25k Zeilen). Sechs parallele Reviewer je Block. **Gesamturteil: kein funktionaler
Blocker in den ausgelieferten Code-Pfaden** — die kritischen Invarianten (Kaskade-Atomaritaet,
Z1-Feld-Erhalt, Audit, Migrations-Guards, SQL-Injection-Abwehr, Flachmodus-Trennung,
Gruppen-Pagination) halten und sind grossteils getestet. Substanz sind ein config-abhaengiges
Risiko, Post-UAT-Toter-Code und viele **Testluecken**.

Bezug: [[2026-08-18-ideal-nachlese-restarbeiten]] (die aeltere Restarbeiten-Liste),
[[2026-08-18-fa-liste-hierarchie-anzeige-spec]], [[2026-07-29-standort-ideal-teil-7-spec]],
[[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]].

> **Lehre (Wiederholung aus der aelteren Nachlese):** Diese Liste altert wie jede andere. Vor dem
> Abarbeiten den Blick in die Dateien, nicht nur in diesen Bericht — mehrere Befunde sind
> „plausibel", nicht „sicher", und einige Ziel-Dateien liegen in der merge-base (ausserhalb des Diffs).

## Merge-Blocker (Repo-Hygiene)

- [x] **B1 — `publish.zip` (226 MB) im Branch getrackt** (Blob in Commit `549c5db`; `.gitignore`
  deckte nur `publish/`, nicht `*.zip`). **Erledigt 2026-09-09:** Loeschung committet + `publish.zip`
  und `*.zip` in `.gitignore` (Worktree-Commit `91d8d3e`). **History-Rewrite bewusst NICHT
  durchgefuehrt** — Entscheidung des Menschen; der Blob bleibt in der Historie.
  **Befund aus der Pruefung des Archivs (Meldepflicht):** das Zip enthielt vier
  `appsettings*.json` mit dem Abschnitt `ConnectionStrings` (WMS, Sage `ake`, Oseon/TRUMPF, enaio)
  sowie zwei `web.config`. **Keine Zugangsdaten darin** — alle Verbindungen laufen ueber
  `Trusted_Connection=True` (Windows-Authentifizierung), `SmtpPassword` und `Password` sind leere
  Zeichenketten. Offengelegt sind damit **Servernamen und Datenbanknamen**, keine Geheimnisse.
  Ob das einen Rewrite rechtfertigt, entscheidet der Mensch; ein Wechsel von Zugangsdaten hat hier
  mangels Zugangsdaten keinen Gegenstand.

## Hoch

- [ ] **H0 (neu, 2026-09-09, MERGE-BLOCKER) — Materialisierung legt die 1:1-Statuszeilen nicht an.**
  Im UAT gefunden: ein materialisierter Sub-FA laesst sich im Leitstand nicht freigeben
  (`HTTP 404 "PickingStatus-Zeile fehlt."`). Ursache, Auswirkung und Loesungsvorschlag stehen in
  [[2026-09-09-materialisierung-ohne-statuszeilen-bug]]. Kurz: `FaMaterializationSyncService`
  erzeugt nur `ProductionOrders`; der einzige Eager-Create der beiden Begleitzeilen liegt im
  AKE-FA-Sync und hat sie bisher **beilaeufig** mitangelegt, solange H1s Schalter faelschlich auf
  `true` stand. Mit dem korrekten `false` faellt der Zufall weg. Direkte Folge von H1 — die beiden
  gehoeren zusammen bearbeitet.

- [ ] **H1 — OrderNumber-gekoppeltes Massen-UPDATE im AKE-Sync** ·
  `IDEALAKEWMSService/Services/SageProductionOrderSql.cs:23-34` (+ `SyncWorker.cs:43`).
  Der Produktionsauftrags-Sync (`Sync:ProductionOrdersEnabled`, Default **true**) macht
  `UPDATE ... WHERE [OrderNumber] = @OrderNumber`. Nach der Inversion ist `OrderNumber` nicht mehr
  eindeutig — der INSERT-Zweig ist schema-bewusst, der UPDATE-Zweig **nicht**. Bei `Master=true` +
  versehentlich aktivem Toggle ueberschreibt ein Lauf Quantity/Kunde/Description **aller** Sub-FAs
  einer Gruppe still. Heute nur per **Config** abgesichert (IDEAL-Deploy-Vorbedingung
  `Sync:ProductionOrdersEnabled=false`), **kein Code-Guard**. (Datei liegt in merge-base, Confidence
  plausibel.)
  → **Vor Produktiv-Deploy:** harter Code-Guard „bei `Master=true` ProductionOrders-Sync
  ueberspringen"; Test „Upsert-SQL trifft nie >1 Zeile im invertierten Schema". Deckt gleichzeitig
  die staerkste Absicherung der ganzen Inversion.
  → **Hochgestuft 2026-09-09 auf merge-nah.** Die Gegenpruefung am Code (Nachtrag in
  [[2026-09-08-uat-ergebnis-ideal-buendel-lauf-1]]) zeigt: Die Materialisierung dreht Menge,
  Artikelnummer und Bezeichnungen im selben Zyklus zurueck, **Kunde und Termine aber nicht** —
  die bleiben auf allen Sub-FAs dauerhaft die Kopfwerte. Zusaetzlich schreiben beide Syncs sich
  gegenseitig jeden Zyklus neu (Aenderungs-Bedingung greift immer), mit einem Fenster falscher
  Mengen **innerhalb** jedes Laufs. Nebenbefund: der Schalter verdeckt derzeit genau die Luecke,
  die [[2026-08-20-materialisierung-fachliche-felder-spec]] schliessen soll.

## Mittel

- [ ] **M1 — `resolve-scan` erzwingt im Normal-Modus serverseitig keine AG-Nummer** ·
  `BdeApiController.cs:71-79`, `BdeScanResolver.cs:177`. `opNumber` optional → ohne AG liefert der
  Normal-Zweig WorkOperations ueber *alle* AGs der Werkbank (falsche Granularitaet). Heute nur
  clientseitig (`parts.length<2`) abgefangen. → `nurFa==false && opNumber leer` → `BadRequest`.
- [ ] **M2 — Erstlauf-Cutover markiert alle nicht-strukturierten Bestands-FAs als „vermisst" +
  ungedeckelte Sammelmail** · `FaMaterializationSyncService.cs:69-74,136-140`. Jede Bestands-FA ohne
  Struktur-Knoten bekommt `SageMissingSince` und wird ueber den `ErrorNotification`-Kanal gemeldet
  (keine Obergrenze, heilt nie). Cutover kann hunderte Alt-Auftraege auf einmal treffen. → Verhalten
  deckeln/dokumentieren (Erstlauf ohne Massen-Notify oder Cap „…und N weitere").
- [ ] **M3 — Atomarer Writer umgeht den Guard-Decorator; einzige Absicherung ist die Allow-List
  (untestiert)** · `StandortSettingsWriter.cs:64-83`. Schreibt ServiceSettings direkt am DbContext
  (by-design); der Master/Auto-Erledigt-Guard greift auf diesem Pfad nicht. Sicher nur, weil
  `StandortSettingsCatalog.Fields` heute keinen guarded Key enthaelt. → **Drift-Guard-Test**:
  `Fields` darf keinen `HierarchischeStrukturKeys.IsGuarded`-Key enthalten (analog
  ServiceSettingDefinitions-Drift-Test).
- [ ] **M4 — Guard-Ablehnungspfad im `ServiceSettingsController` komplett untestiert** ·
  `ServiceSettingsController.cs:78-87`. Kein Test triggert `HierarchischeStrukturGuardException`;
  der Test-Build nutzt `Mock.Of<ISyncLogger>()` ohne Setup → liefe der catch je, gaebe es eine NRE
  (Beweis, dass der Zweig nie ausgefuehrt wird). → Test: manipulierter POST bei gesperrten Daten →
  ModelState invalid + Audit-SyncLog `HierarchieUmstellung`.
- [ ] **M5 — Tote Blatt-/Anomalie-Mechanik; Default-Parameter sind eine Fussangel** ·
  `Services/FaHierarchyListBuilder.cs:91-92` (Defaults `leafOnly:true, anomalyOnNonLeaf:true`). Alle
  drei Produktiv-Caller rufen mit `false,false`; der Blatt-/Anomalie-/Banner-Pfad (`.Anomalies`) ist
  in Produktion unerreichbar und wird von keiner View gelesen — der von UAT-Etappe 8 „entfernte"
  Banner lebt als Maschinerie weiter. Kuenftiger Caller, der die Flags vergisst, bekommt still
  `leafOnly:true`. → Defaults auf `false,false`, toten Pfad entfernen oder als Legacy markieren.
  (Teil des Toter-Code-Clusters, siehe unten.)
- [ ] **M6 — Vormontage Sicht 2: moegliche Doppelzaehlung bei gleichem Matchcode auf mehreren
  Ebenen** · `Services/VormontageService.cs:245-256` (analog Kommissionier-Summiert). Summiert
  `Sollmenge`/`Fertigungmenge` ueber alle Ebenen mit gesetztem `VMBedarf`. Korrekt nur, wenn jede
  Positionszeile fachlich einen eigenen Bedarf traegt. → **Fachlich bestaetigen**, dass `VMBedarf`
  post-UAT nur je einer Ebene gesetzt ist; Test, der den Fall fixiert.
- [ ] **M7 — Vollstaendig abgekoppelter Ring ohne Wurzel/Waise wird still verworfen** ·
  `Services/FaHierarchyTreeBuilder.cs:75-87`. Besteht eine HauptFA-Gruppe nur aus einem geschlossenen
  Zyklus (kein `VaterFA==null`, keine Waise), entstehen **null Strukturen** — ohne Fehler-Badge,
  ohne Log. Widerspricht der Invariante „nicht still verworfen" (`FaHierarchyTreeViewModel.cs:59-60`).
  Bei sauberen Sage-Daten selten, aber genau der Datenfehler, den der Schutz sichtbar machen sollte.

## Niedrig / Info

- [ ] **BDE:** toter `operatorId`-Parameter (`BdeScanResolver.cs:163`); `candidate.id` vs.
  `productionOrderId`-Kopplung (`bde-terminal.js:229` — bricht still, falls die Id-Gleichheit je
  aufgehoben wird).
- [ ] **Standort/BOM:** BOM-Guard akzeptiert nur `"true"`, sonst ueberall auch `"1"/"on"` →
  theoretischer Fail-Open zum HTTP 500 bei nicht-kanonischem Master-Wert
  (`HierarchicalBomGuardRepository.cs:33`); generische `SaveSettings` speichert bei Guard-Ablehnung
  Vor-Keys trotzdem (nicht-atomar, `ServiceSettingsController.cs:45-95`).
- [ ] **FA-Struktur:** Waisen-Pseudowurzel bekommt nie „Root"-Icon (bewusst? → dokumentieren);
  eingeklappte Kombigeraet-Kopfzeile zeigt nur die erste OrderInfo bei gleicher/`null`
  MontageAbteilung; `fa-node-context` `opacity:0.6` kann Kontrast <4,5:1 druecken; In-Memory-Voll-Load
  vor Pagination.
- [ ] **FA-Liste:** Spalte `parent-sub-order-number` wird auch im Flachmodus gerendert (nur
  `defaultHidden`) — technisch „neue Spalte" (benign, aber AKE-Regression bestaetigen); grosser
  IN-Clause bei PageSize „Alle" (SQL-2100-Grenze); `table-filter.js:324` besser `:scope > tbody`.
- [ ] **Teile 3/4/5:** veraltete „SubFA=0"-XML-Doku (`FaHierarchyListGroupViewModel.cs:10-23`);
  `GroupBy` ordinal vs. `OrderBy` ignore-case (`VormontageService.cs:246/255`); Vormontage ohne
  Print-Action.
- [ ] **Toter Zweig F2 (bekannt):** `fa-liste-gruppierung.js:36-41` Auto-Expand lauscht auf nie
  gerendertes `[data-fa-liste-filter]` — nicht blockierend (auch in
  [[2026-08-18-fa-liste-hierarchie-anzeige]] als Schranke-2-Entscheidung vermerkt).

## Nachtrag aus UAT-Lauf 1 (2026-09-08, [[2026-09-08-uat-ergebnis-ideal-buendel-lauf-1]])

- [x] **U1 — JS-TypeError auf der BOM-Guard-Hinweisseite** · `Views/Picking/Bom.cshtml:41` (Zweig
  `HierarchicalUnavailable`) vs. Inline-Script `:916/:927/:972` (`getElementById(...).addEventListener`
  ohne Null-Guard) → `Cannot read properties of null` in der Konsole. Dazu zeigt die Kopfzeile die
  HauptFA-Nr. statt der Sub-FA (Z4-Konsistenz). **Umgesetzt 2026-09-08, Worktree `a8d75de`:**
  Fruehabbruch ohne `#bomTable`, Druck-Knopf auf der Hinweisseite weg, `BomViewModel.SubOrderNumber`
  (Controller + `ReadOnlyBomBuilder`), Kopfzeile Sub-FA + „HauptFA" nur bei Abweichung; Test
  `Bom_ViewModel_CarriesSubOrderNumber`; TS-68.2 Nachtrag. Re-QA: siehe
  [[2026-08-18-bom-guard-hierarchisch-spec]].
- [x] **U2 — Kaskade-Dialog blendet „mit offener Buchung" nur bei >0 ein** ·
  `Views/PickingLeitstand/Index.cshtml:461-462`. Kein Bug, aber die Abnahme kann bei 0 nicht
  entscheiden, ob der Zaehler existiert → immer anzeigen („davon 0 …"). **Umgesetzt 2026-09-08,
  Worktree `a8d75de`:** Zaehler nach Vorschau immer sichtbar (0 = `alert-secondary`, >0 =
  `alert-warning`, `role=status`); TS-69.15 Nachtrag. Re-QA: siehe
  [[2026-08-18-fa-liste-hierarchie-anzeige-spec]].
- [ ] **U3 — Toggle aus → `AccessDenied` statt Home+Warning** bei `FaCompletionAktiv`
  (`FaWorklistController.cs:82-86`, `RequireFaCompletionAccessAttribute.cs:27-28`). Vorbestehend,
  weicht von der Konvention ab; nur dokumentieren oder angleichen.
- **H1 bestaetigt sich in der Praxis:** am Testsystem stand `Sync:ProductionOrdersEnabled=true`
  neben aktiver Materialisierung — genau der Fall, gegen den nur Config schuetzt.

## Testluecken (das dominierende Thema)

**Groesste Luecke:** Der **Materialisierungs-Service ist auf DB-Ebene fast ungetestet** (nur
IsDoneBde-Erhalt). Fehlen: Create-Zweig (OrderNumber/SubOrderNumber/Parent/Audit),
Missing-Markierung + Selbstheilung, Reparent-Konflikt, Skip/DryRun, Sammelmail-Dedup (genau eine
Mail/Lauf, keine bei 0/disabled), Cutover-Massenfall (M2).

Weitere hochwertige fehlende Tests:
- [ ] `HierarchieUmstellungController`: **gar kein Test** (Enable/Disable/No-op/Ablehnung/
  Auto-Erledigt-Abschaltung).
- [ ] `Bool()` VB6-Mapping (`-1`/`1`/`Ja`/NULL) · `FaHierarchySyncService.cs:280-294` — billiger
  Realfall-Regressionstest.
- [ ] BDE: NurFA-Ausschlusszweige (operator-aktiv, final-gemeldet, verpackt) + Normal-
  SubOrderNumber-Fallback + expliziter „kein-Substring"-Regressionstest (`GRP` trifft nicht
  `GRP-1`) + Endpoint-Serialisierung `NotInScope`/`NotFound`.
- [ ] FA-Liste: Sub-vor-Haupt-Filter (OR/`Like`) ist **InMemory nicht testbar** → strikt Manual-UAT;
  In-Memory-Filterpfad im gruppierten Modus (leere Gruppen entfernen, Z4-Neuberechnung);
  Kaskade auf stornierten/erledigten Sub-FAs; Flachmodus-Markup-Snapshot.
- [ ] Teile 3/4/5: Doppelzaehlungs-Test (M6); Spaltenfilter-Mini-Syntax (`!`/Komma) auf Aggregat;
  „Print spiegelt `colf_*`"; `ResolveBereich`-Fallback; Seite jenseits der letzten Gruppe.
- [ ] FA-Struktur: abgekoppelter Zyklus (M7); Orphan-Root-Klassifikation; Diamant wird **nicht** als
  Zyklus markiert; mehrere echte Wurzeln je HauptFA.
- [ ] Standort: Katalog-Drift-Guard (M3); Guard-Ablehnung im ServiceSettingsController (M4);
  Writer-Rollback nur Manual-UAT (InMemory-Limit).

## Muster ueber alle Bloecke

1. **Post-UAT-Toter-Code** haeuft sich (Blatt/Anomalie-Maschinerie M5, `operatorId`, Auto-Expand-Zweig
   F2, veraltete Docs) → **ein gebuendelter Aufraeum-Nachtrag** sinnvoll, nicht sechs einzelne.
2. **Config-statt-Code-Guards** (H1, M3): die Inversion verlaesst sich an zwei Stellen auf
   Betriebsdisziplin statt auf Test/Guard.
3. Die **rohen-SQL-/Migrations-Pfade** sind erwartungsgemaess nur per Manual-UAT abgedeckt → gehoert
   explizit in den Deploy-Abschnitt.

## Reihenfolge / Einordnung

- **Vor dem Merge:** nur B1 (erledigt, Verifikation der History ausstehend).
- **Vor dem Produktiv-Deploy:** H1 als Code-Guard nachziehen.
- **Nach dem Merge (Test-Haertung):** Materialisierungs-Service-Suite + `HierarchieUmstellungController`
  + `Bool()` + M3-Drift-Guard sind die hoechstwertigen, billig nachzuholenden Tests.
- **Gebuendelter Aufraeum-Nachtrag:** M5 + toter Zweig F2 + `operatorId` + veraltete Docs zusammen.
- **Fachlich zu klaeren:** M6 (VMBedarf-Ebenen), plus die bereits in
  [[2026-08-18-ideal-nachlese-restarbeiten]] Abschnitt D gelisteten Punkte.
