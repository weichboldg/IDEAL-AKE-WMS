---
type: uebersicht
title: "Uebersicht: IDEAL-Standort live schalten — hierarchische Produktionsauftraege (8 Teile)"
slug: 2026-07-29-standort-ideal-uebersicht
status: InUmsetzung
created: 2026-08-06
updated: 2026-08-07
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
epic: true
task: "[[2026-08-07-ideal-teile-1-5]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
etappen:
  - "1: Teil 1 — Struktur-Fundament FaHierarchyNode/OrderInfo (Import FAListe+FAInfos, Domain, Repository, Cache, View-DDL) — ERLEDIGT de4dcd8 (Build+Test gruen)"
  - "2: Teil 2 — Struktur-/Baumanzeige (rekursiv, Tiefen-Cap + Zyklenschutz) — ERLEDIGT d526a4e (Build+Test gruen)"
  - "3: Teil 3 — Kommissionierlisten INKL. gemeinsamem Listen-/Druck-Baustein (Referenzimplementierung) — ERLEDIGT 9ca8141 (Build+Test gruen; Baustein FaHierarchyListBuilder)"
  - "4: Teil 4 — Beschichtungsauftrag (erweitert den Baustein aus Etappe 3, ohne PDF) — ERLEDIGT 08aa610 (Build+Test gruen; Baustein generalisiert leafOnly/anomaly opt-in)"
  - "5: Teil 5 — Vormontage-Listen, zwei Sichten (erweitert den Baustein aus Etappe 3)"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-08-07
---

## Zweck

Die Backlog-Notiz `[[2026-07-29-Standort-IDEAL]]` beschreibt die Live-Schaltung eines zweiten
Standorts (**IDEAL**, eigenes Deployment, gemeinsamer Codestamm mit **AKE**) samt Umstellung auf
**hierarchische Produktionsauftraege** (Haupt-FA → Sub-FA → Sub-Sub-FA, echter Elternzeiger). Das
Paket ist sehr gross und teils noch nicht entscheidungsreif — deshalb `split: true` mit acht
Teil-Specs, davon **Teil 7 und Teil 8 als `epic: true`** (Teil 7 seit der Nachbesserung 2026-08-07).

**Zur Nummerierung:** Die Ideen-Notiz enthaelt (Stand vor der „Kritischen Pruefung / Aufbereitung
2026-08-06") **drei widerspruechliche Alt-Nummernschemata** — die Zerlegungstabelle
(„Zerlegung & Reihenfolge") nennt Materialisierung=Teil 7 / BDE=Teil 8, der B5-Fliesstext
nennt an mehreren Stellen „Teil 2 (BDE)" / „Teil 1/2", und zusaetzlich tauchen „Teil 1b"
(Stueckliste) und „Teil 1c" (Standorteinstellungen) auf. **Diese Uebersicht verwendet
ausschliesslich EIN Schema — die Zerlegungstabelle der Notiz ist die konsolidierte Wahrheit.**
Die Stueckliste ist laut Befund B4 kein eigener Teil, sondern eine zweite Projektion **von**
Teil 1 (ein Lesepfad, zwei Projektionen); „Teil 1c" (Standorteinstellungen) ist Teil 6.

## Etappen (epic: true — Buendel Teile 1–5)

Die Teile 1–5 werden in **einem** langlebigen Worktree als Etappen gebaut und **einmal** gemergt.
Begruendung: (a) Der gemeinsame Listen-/Druck-Baustein entsteht so aus **drei echten Verbrauchern**
statt aus einer Vermutung — keine Refaktorierung einer bereits gemergten Komponente. (b) Schranke 2
haengt fuer alle fuenf am selben leeren Testsystem; fuenf getrennte Merges hiessen fuenf Testrunden
auf dieselben fehlenden Daten. (c) Die Teile 1–5 fassen weder `ProductionOrders` noch das Schema an,
alles additiv und hinter Schaltern mit Default `false` — das Buendel-Risiko ist gering.

**Teil 6, 7 und 8 sind NICHT Teil dieses Buendels.** Teil 7 (Schema-Inversion, Einwegtor) und Teil 8
(BDE) bleiben strikt getrennte, eigene Epics — eine daten-konvertierende Migration der zentralsten
Tabelle gehoert nie in denselben Merge wie Kommissionierlisten.

| # | Etappe | Detail-Spec | Status | Commit |
|---|--------|-------------|--------|--------|
| 1 | Struktur-Fundament `IdealFaStruktur` | [[2026-07-29-standort-ideal-teil-1-spec]] | offen | |
| 2 | Struktur-/Baumanzeige | [[2026-07-29-standort-ideal-teil-2-spec]] | offen | |
| 3 | Kommissionierlisten + gemeinsamer Baustein | [[2026-07-29-standort-ideal-teil-3-spec]] | offen | |
| 4 | Beschichtungsauftrag | [[2026-07-29-standort-ideal-teil-4-spec]] | offen | |
| 5 | Vormontage-Listen | [[2026-07-29-standort-ideal-teil-5-spec]] | offen | |

**Regeln fuer den Etappenlauf:**
- Jede Etappe endet in einem **eigenstaendigen, buildbaren Commit** — `dotnet build` und
  `dotnet test` gruen, bevor die naechste beginnt. Keine Etappe baut auf einem roten Stand auf.
- Die **Detail-Spec der Etappe ist massgeblich** fuer Umfang, Akzeptanzkriterien und Loesungsentwurf.
  Diese Uebersicht steuert nur Reihenfolge und Querschnitts-Regeln.
- **Etappe 3 schneidet den gemeinsamen Baustein** (Filter als Parameter, Kopf-Abfrage je `HauptFA`,
  Gruppierung, Paginierung ueber Gruppen, Druck-Scaffold). Etappe 4 und 5 **erweitern** ihn — sie
  duerfen ihn nicht duplizieren. Zeigt sich in Etappe 4/5, dass die Naht falsch liegt, wird sie
  **im selben Zweig** korrigiert; genau dafuer ist gebuendelt worden.
- Zwischen den Etappen `scripts/sync-worktree.ps1 -Slug <slug>` laufen lassen, damit der Zweig nicht
  von `main` wegdriftet.
- **QA und Merge (Schranke 2) erst, wenn alle fuenf Etappen fertig sind.** Kein Zwischen-Merge.

## Teile in risiko-aufsteigender Reihenfolge

| # | Teil-Spec | Kern (`ProductionOrders`) beruehrt | Zweck |
|---|---|---|---|
| 1 | [[2026-07-29-standort-ideal-teil-1-spec]] | nein | Struktur-Fundament `FaHierarchyNode` (+ `FaHierarchyOrderInfo`): Import aus den IDEAL-Sage-Views `FAListe`/`FAInfos`, mehrstufige Projektion (`HauptFA`/`VaterFA`/`SubFA`/`Position`), eigene Domain, Repository + Cache-Decorator, konfigurierbare View-Namen. Fundament fuer Teil 2–5. |
| 2 | [[2026-07-29-standort-ideal-teil-2-spec]] | nein | Struktur-/Baumanzeige: rekursive Darstellung von `FaHierarchyNode` (nicht zweistufig — B1 hat den alten 2-Ebenen-Entwurf ueberholt). Riskantester Anzeigeteil (Paging ueber Gruppen, Phantom-Header, Auto-Expand). |
| 3 | [[2026-07-29-standort-ideal-teil-3-spec]] | nein | Kommissionierlisten: Filter `Kommissionieren`, gruppiert nach `HauptFA` (+ Montage-Abteilung), Barcode `HauptFA`, Druck. |
| 4 | [[2026-07-29-standort-ideal-teil-4-spec]] | nein | Beschichtungsauftrag: Filter `Beschichtet = -1`, Druckdokument mit Dienstleister-Kopf. |
| 5 | [[2026-07-29-standort-ideal-teil-5-spec]] | nein | Vormontage-Listen: Filter `VMBedarf`, drei Sichten, Isolierfraesen-Export. |
| 6 | [[2026-07-29-standort-ideal-teil-6-spec]] | nein | Standorteinstellungen-Maske: buendelt Firmenname/Adresse/Mandant/View-Namen/Schalter statt Einzelpflege in der generischen Settings-Oberflaeche. |
| 7 | [[2026-07-29-standort-ideal-teil-7-spec]] | **ja** | Materialisierung nach `ProductionOrders`: Schema-Inversion (`OrderNumber` nicht mehr unique, `SubOrderNumber` unique, `ParentSubOrderNumber`), Einweg-Migrationstor, Sync-Regeln (nicht loeschen / Umhaengung nicht still uebernehmen), FA-Zusatzinfos-Kollision. |
| 8 | [[2026-07-29-standort-ideal-teil-8-spec]] | **ja** | Sub-FA-Rueckmeldung / BDE (`epic: true`). Nach Teil 7 sind Sub-FAs echte `ProductionOrders` — Arbeitsgaenge/Teileverfolgung/Rueckmeldung greifen grundsaetzlich unveraendert, muessen aber gegen die Nicht-Eindeutigkeit von `OrderNumber` gehaertet werden. |

**Abhaengigkeiten (`depends_on`):** Teil 2–5 haengen nur an Teil 1 (lesen `FaHierarchyNode`/
`FaHierarchyOrderInfo`, kein Schema-Umbau am Kern — Entscheidung B5) und sind **untereinander**
unabhaengig, in beliebiger Reihenfolge lieferbar. **Ausnahme Teil 6** (seit Nachbesserung
2026-08-07): haengt zusaetzlich an Teil 3 (gemeinsamer Firmendaten-Key-Definitionsort in
`AppSettingKeys.cs`) und Teil 7 (die Master-Anzeige braucht Guard + gecachten Sperrzustand). Teil 7
haengt an Teil 1 (liest die Struktur-Tabelle als Quelle der Transformation). Teil 8 haengt an Teil 7
(braucht echte `SubOrderNumber`-`ProductionOrders` + die mengenwertige
`GetAllByFaAndOperationAsync`-Variante).

**Alternative Reihenfolge laut Notiz:** Muss Rueckmeldefaehigkeit von Tag eins stehen, koennen 7/8
vorgezogen werden — Teil 1 bleibt trotzdem das Fundament (Architektur aendert sich nicht, nur die
Lieferreihenfolge).

## Querschnitts-Hinweise fuer die Freigabe (Schranke 1)

- **Migrationsnummern:** main ist nach v1.28.0 (Sage-Lagerbuchungen) bei `SQL/83`; die WmsBugs-Batches
  (v1.29.0/v1.30.0, Testbereit in eigenen Worktrees, noch nicht gemergt) belegen **`SQL/84`**,
  **`SQL/85`** und Seed **`SQL/86`**. Die IDEAL-Migrationen (Teil 1: `FaHierarchyNode`/
  `FaHierarchyOrderInfo`; Teil 7: Schema-Inversion) sind daher voraussichtlich **ab `SQL/87`** zu
  planen — die `86`-Referenzen in den Teil-Specs sind Platzhalter; **vor dem jeweiligen Dev-Lauf
  erneut gegen den dann gemergten Stand pruefen**, welche Nummer wirklich frei ist.
- **Das Einweg-Migrationstor** (Master-Schalter `ProduktionsauftragHierarchisch`, datengetrieben
  gesperrt sobald `EXISTS(ProductionOrders WHERE OrderNumber <> SubOrderNumber)`, Waechter in der
  Domaenenschicht, Pflicht-Audit) ist in Teil 7 exakt aus der Notiz uebernommen — siehe dortiger
  Abschnitt „Migrations-/SQL-Auswirkungen".
- **FA-Zusatzinfos-Kollision** (v1.26.0, `FaZusatzinfoSyncService` + FA-Reconciliation +
  Scan-/QR-Lookups) ist das groesste technische Risiko des gesamten Pakets — als
  Pflicht-Adversarial-Review in Teil 7 verankert (H-2: die Schreibseite von
  `FaZusatzinfoSyncService` ist seit v1.26.0 bereits mehrfachtreffer-faehig, das Restrisiko liegt in
  der FA-Reconciliation und in Single-/First-Lookups).
- **Regel fuer alle Teile ab 7:** eindeutige Lookups → `SubOrderNumber`; Gruppen-Lookups (alle
  Sub-FAs einer Haupt-FA) → `OrderNumber`.
- **Harte Akzeptanzbedingung fuer JEDEN Teil:** Bei `ProduktionsauftragHierarchisch = false`
  verhaelt sich das System exakt wie heute (AKE unveraendert) — nach jedem Teil-Merge nachweisbar.
- **Namens-Konvention (Schranke-1-Hinweis, in allen Teilen umgesetzt):** **kein Standort ("Ideal")
  in neuen Code-Bezeichnern** — konzeptbasiert `FaHierarchy*` / `HierarchicalFa` (z. B.
  `FaHierarchyNode`, `FaHierarchyOrderInfo`, `Sync:HierarchicalFaEnabled`,
  `FaHierarchyKommissionierListenController`). Unveraendert bleiben: Projektname `IdealAkeWms`,
  Prosa-Verweise auf den Standort IDEAL, die Sage-View-Namen `vw_IDEAL-AKE_*` und die Spec-Slugs.
  Vollstaendige Zuordnungstabelle in [[2026-07-29-standort-ideal-teil-1-spec]] („Umsetzungsnotiz —
  Namens-Konvention").

## Uebergreifende offene Rueckfragen

1. **Anhang-Pfad kaputt.** Der `anhaenge:`-Eintrag im Frontmatter der Backlog-Notiz zeigt auf
   `anhaenge/2026-07-29-standort-ideal/sage-views-ideal.md` — der tatsaechliche Ordner heisst
   (Datums-Tippfehler **und** Gross-/Kleinschreibung abweichend)
   `anhaenge/2026-0-29-Standort-IDEAL/sage-views-ideal.md`. Diese Spec-Runde hat den Anhang ueber
   den **korrekten Ist-Pfad** gelesen (vom Auftraggeber explizit vorgegeben) und vollstaendig
   ausgewertet — der Anhang ist NICHT „nicht verlaesslich lesbar". Trotzdem muss der Pfad vor dem
   Verschieben der Notiz nach `backlog/` repariert werden (Ordner umbenennen ODER Frontmatter auf
   den Ist-Pfad setzen — eine kanonische Schreibweise festlegen), sonst liest ein kuenftiger
   automatisierter Lauf (ohne die hier gegebene Pfad-Korrektur) den Anhang tatsaechlich nicht.
2. **Toggle-Heimat und Toggle-Abhaengigkeit vs. Entscheidung B5.** Die Notiz zeichnet einen
   Schalterbaum, in dem `ProduktionsauftragBaumAnzeige` / `TermineAusPpsView` /
   `StuecklisteAusIdealView` **unter** dem Master `ProduktionsauftragHierarchisch` haengen
   („nur wirksam wenn Master an"). Die spaeter im selben Dokument getroffene Entscheidung B5 sagt
   aber explizit, dass Teil 2–5 (wozu die Baumanzeige gehoert) **nichts aus dem Kern beruehren** und
   **vor** der Schema-Inversion lieferbar sind. Beides gleichzeitig ist widerspruechlich: entweder
   haengt die Baumanzeige vom (noch nicht existenten) Master ab — dann waere sie nicht vor Teil 7
   lieferbar — oder sie ist unabhaengig, wie B5 es verlangt. **Empfehlung dieser Spec-Runde:**
   B5-Linie folgen — alle Teil-1-5-Schalter sind vom Master **unabhaengig**; der Schalterbaum in der
   Notiz ist ein ueberholter Stand von vor der B5-Ergaenzung. Zusaetzlich: sollen die rein
   web-seitigen Anzeige-Schalter (Baumanzeige, Stueckliste-Quelle) als `AppSettings` (ADR 0011,
   fachlicher Feature-Toggle der Web-App) statt als `ServiceSettings` (ADR 0008, Service-Verhalten)
   gefuehrt werden? Die Notiz schlaegt pauschal ServiceSettings vor („Null Zusatzaufwand"); die
   bestehende Architektur-Trennung spricht für AppSettings bei reinen UI-Schaltern und
   ServiceSettings nur dort, wo der Windows-Service selbst etwas synchronisiert (View-Namen,
   Import-Enable, der Master als Sync-Gate der Materialisierung in Teil 7).
3. **Standorteinstellungen-Maske-Notiz existiert (noch) nicht.** Die Referenz
   `[[2026-08-03-standorteinstellungen-maske]]` (Teil 6) ist in `secondbrain/ideen/` nicht
   auffindbar. Teil 6 ist deshalb nur grob spezifiziert und muss nachgezogen werden, sobald diese
   Notiz existiert oder ihr Inhalt anderweitig vorliegt.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → **Anhang-Pfad: kanonisch ist der Slug-Stil, der Ordner wird umbenannt.**
   Verbindlich: `secondbrain/ideen/anhaenge/2026-07-29-standort-ideal/sage-views-ideal.md`
   (Kleinschreibung, korrigiertes Datum). Der `anhaenge:`-Eintrag im Frontmatter der Ideen-Notiz
   zeigt bereits dorthin und bleibt unveraendert; der **Ordner** wird auf diese Schreibweise
   gebracht — nicht umgekehrt, damit alle Anhang-Pfade dieselbe Konvention haben wie die
   Spec-Slugs. Beim Verschieben der Notiz nach `backlog/` wandert er nach
   `backlog/anhaenge/2026-07-29-standort-ideal/`.

2. → **B5-Linie gilt; der Schalterbaum der Ideen-Notiz ist ueberholt. Und: UI-Schalter in
   AppSettings, Service-Schalter in ServiceSettings.**
   - **Abhaengigkeit:** Die Schalter der Teile 1–6 sind **unabhaengig** vom Master
     `ProduktionsauftragHierarchisch`. Der Schalterbaum in der Ideen-Notiz
     ("nur wirksam wenn Master an") stammt aus der Zeit **vor** Befund B5 und ist damit ein
     ueberholter Stand — die Empfehlung dieser Spec-Runde ist bestaetigt. Der Master ist
     ausschliesslich das Sync-Gate der Materialisierung in Teil 7.
     *Folgearbeit:* Der Schalterbaum in der Ideen-Notiz ist entsprechend zu korrigieren, damit
     kein kuenftiger Lauf den alten Stand liest.
   - **Heimat:** Der Vorschlag dieser Spec-Runde wird uebernommen, weil er der bestehenden
     Architektur-Trennung folgt statt sie aufzuweichen:
     - **AppSettings (ADR 0011)** — reine Web-/Anzeige-Schalter: `ProduktionsauftragBaumAnzeige`,
       Stueckliste-Quelle, die Feature-Toggles der Listen (Teil 3/4/5).
     - **ServiceSettings (ADR 0008)** — alles, was der Windows-Service selbst tut:
       `Sync:HierarchicalFaEnabled`, die beiden View-Namen, sowie der Master als Sync-Gate in
       Teil 7.
     Die Formulierung "pauschal ServiceSettings, Null Zusatzaufwand" in der Ideen-Notiz ist damit
     ueberholt.

3. → **Die Notiz existiert — sie liegt in `backlog/`, nicht in `ideen/`.**
   Pfad: `secondbrain/backlog/2026-08-03-standorteinstellungen-maske.md`. Sie enthaelt bereits den
   Vermerk, dass sie **nicht separat zu spezifizieren** ist, sondern als Teil 6 dieses Pakets
   gefuehrt wird. Teil 6 ist daher nachzuziehen, sobald der Inhalt eingearbeitet ist — kein
   Blocker fuer die Teile 1–5.

### Ergaenzende Querschnitts-Entscheidungen (2026-08-06)

- **Teil 3 ist Referenzimplementierung fuer die Listen-/Druck-Mechanik.** Teil 3/4/5 machen
  strukturell dasselbe (Flag-Filter auf `FaHierarchyNode`, Kopf aus `FaHierarchyOrderInfo` je
  `HauptFA`, Gruppierung, Druck-ViewModel). Teil 3 schneidet den Baustein bewusst wiederverwendbar;
  **Teil 4 und Teil 5 erweitern ihn, statt ihn zu duplizieren**. `depends_on` von Teil 4 und 5
  entsprechend um die Teil-3-Spec ergaenzt.
- **Kombinationsgeraete sind in allen Teilen out of scope** — keine Trennung nach
  `MontageAbteilung`, keine Sonderlogik. **Aber:** Der 1:n-Fall `HauptFA` -> FAInfos muss trotzdem
  behandelt werden (kein Fan-out-Join; Kopfdaten in eigener Abfrage; bei mehreren Kopfzeilen alle
  im Klartext auffuehren, Dokument als mehrdeutig kennzeichnen, Log-Eintrag). Fachliche Behandlung
  als Backlog-Nachtrag: [[2026-08-06-kombinationsgeraete-montageabteilung]].
- **Seiteneinheit ist durchgaengig die Gruppe (`HauptFA`), nicht die Zeile** (Teil 2/3/4/5). Eine
  Gruppe wird nie ueber Seiten getrennt, `TotalCount` zaehlt Gruppen. Damit entfallen
  Phantom-Header und Zeilenzahl-Schaetzung im gesamten Paket.
- **PDF-Erzeugung ist ein eigener Querschnitts-Baustein**, nicht Teil 4:
  [[2026-08-06-pdf-erzeugung-fahierarchy-druck]]. Teil 3/4/5 setzen darauf auf.
### Querschnitts-Regel: Das Web verschickt keine Mails (2026-08-07)

Befund aus der Teil-2-Pruefung, am Code verifiziert: **Im Web-Projekt existiert kein Mailversand.**
Ein Grep ueber alle Mail-Primitiven liefert null Treffer; `ISyncErrorNotifier` ist Service-only.
Die Fehlermail-Zusagen in Teil 1 und 2 haengen damit an Infrastruktur, die es nicht gibt.

**Entscheidung — gilt fuer alle Teile:**
- **Das Web verschickt keine Mails.** Nicht, weil es aufwendig waere, sondern weil es falsch waere:
  SMTP im Request-Pfad koppelt die Antwortzeit an die Verfuegbarkeit des Mailservers, verteilt
  Zugangsdaten in die Web-Schicht, und ADR 0010 zieht die Grenze ohnehin bei Hintergrund-Diensten.
- **Web-seitige Funde** gehen an `ILogger` **plus ein sichtbares Signal in der Oberflaeche**
  (Banner/Hinweis an der betroffenen Liste). Bei einem Anzeige- oder Datenproblem ist das wirksamer
  als eine Mail, die niemand mit der Ansicht verbindet.
- **Was gemailt werden muss, wird im Service erkannt.** Der hat `ISyncErrorNotifier` bereits. Ein
  Tiefen-/Zyklenverstoss oder eine Datenanomalie ist ein **Datenproblem** — der Sync kann es
  genauso feststellen wie die Anzeige, und dort gehoert die Meldung hin.
- **Kein SyncLog aus dem Web.** `SyncLog`/ADR 0010 ist fuer Hintergrund-Dienste; Web-Lesefunktionen
  (Teil 2–6) protokollieren ueber `ILogger`. Das loest zugleich den SyncLog-Widerspruch in Teil 4.

**Folge fuer die Specs:** Alle Formulierungen „Fehlermail" in Web-Kontexten (Teil 1 Web-Anteil,
Teil 2 Tiefen-/Zyklen-Cap, Teil 4 Mehrdeutigkeit, Teil 5 Anomalie-Diagnose) sind auf
„`ILogger` + Oberflaechen-Hinweis" umzuschreiben; wo eine Mail fachlich gebraucht wird, wandert die
Erkennung in den Service (Teil 1 Sync, Teil 7 Materialisierung).

- **Testbarkeit — kritischer Pfad des Pakets:** Das IDEAL-Testsystem ist derzeit **leer**. Ohne
  produktivnahe IDEAL-Daten kann Schranke 2 fuer die Teile 1–5 faktisch nicht gruen werden. Das
  Befuellen des Testsystems ist damit die wichtigste Vorbedingung des gesamten Vorhabens —
  wichtiger als jede offene Spec-Frage. In den Deploy-/Test-Abschnitten der Teile vermerkt.

## Referenzen

- **Anhang [[sage-views-ideal]]** (`secondbrain/ideen/anhaenge/2026-0-29-Standort-IDEAL/sage-views-ideal.md`)
  — massgebliche View-/Spaltengrundlage.
- **[[2026-07-28-ideal-anpassungen-neu-nachbilden-spec]]** — historischer Vorentwurf (Basis
  ~v1.12.0, Branch `feature/ideal-anpassungen-v1`, geloescht). Design-Entscheidungen D1/D2/D3
  darin sind durch B1/B5 dieser Notiz **ueberholt** (u. a. „zweistufig", zwei Auftragstabellen
  vermengt). Nicht als Spezifikationsgrundlage verwenden — bleibt vorerst unangetastet in
  `entwurf/`, da das Archivieren ausserhalb des Auftrags dieser Spec-Runde liegt (Hinweis fuer die
  naechste Pflege-Runde).
- **[[2026-07-27-ideal-anpassungen-neu-nachbilden]]** — Backlog-Alteintrag, durch diese Notiz
  abgeloest.

## Vollstaendigkeits-/Reifegrad-Pruefung (2026-08-07) — Gate vor Entwicklungsstart

Methode: Ist-Zustands-Pruefung aller acht Teil-Specs plus die beiden bereits eingearbeiteten
Kritische-Pruefung-Durchgaenge (06-Erstreview + 07-Zweitreview, je Teil committet). Ein dritter
Achtfach-Agenten-Review wurde **bewusst nicht** gefahren: Alle acht Dateien sind seit den
07-Review-Commits **unveraendert** — ein erneuter Lauf wuerde die bereits dokumentierten Befunde
identisch reproduzieren. Das Gate fasst deshalb den Stand zusammen, statt Aufwand zu duplizieren.

**Gesamturteil: KEINE der acht Specs ist derzeit freigabereif.** Zwei Klassen von Luecken.

### Klasse 1 — querschnittlich (betrifft ALLE): Antworten entschieden, aber NICHT in Rumpf/Frontmatter eingearbeitet

Der Mensch hat starke, ueberwiegend am Code verifizierte Entscheidungen getroffen — sie leben aber
nur in den „## Antworten"-/„=>"-Bloecken; Rumpf, Akzeptanzkriterien, `open_questions` und Frontmatter
stehen noch auf dem alten Stand. Ein `dev`-Lauf wuerde den veralteten Body bauen. Konkret offen je Teil:

| Teil | Antworten da? | Wichtigste Rumpf-/Frontmatter-Diskrepanz |
|---|---|---|
| 1 | ja | Bindestrich-Whitelist-Fix + EXISTS/Staging in AK ziehen; `open_questions` (2) trimmen |
| 2 | **nur teils** | „Baum in scope" im Rumpf ok, aber **5 neue Freigabe-Antworten LEER** (s. Klasse 2) |
| 3 | **nein** | Kern-Frage Doppelzaehlung unbeantwortet (s. Klasse 2); Rumpf-`open_questions`=1 offen |
| 4 | ja | `depends_on` zeigt auf Backlog-Notiz statt Spec; SyncLog→ILogger; Render-Weg |
| 5 | ja | Rumpf 100 % stale (drei Sichten+Export statt zwei; AK2 „Artikel" vs. Antwort „Matchcode") |
| 6 | ja | `depends_on` nur Teil 1 (Antwort: +7 +3); Master read-only vs. Body-„schreibbar" |
| 7 | ja | **`epic: false` → muss `true`** (sonst falsches Tooling-Routing) + Etappen A–E; 4 `open_questions` leeren; `barcode-scanner.js`/„Index 2 = BelID" streichen; `SageMissingSince` in Migration |
| 8 | ja (`epic:true`) | **zwei widerspruechliche Etappen-Tabellen**; `affected_code` falsch (barcode-scanner.js statt WorkOperationRepository/bde-terminal.js); `open_questions` (3) offen |

→ Behebbar durch eine Nachbesserungs-Runde „Antworten → Rumpf" (analog Teil 1–4 nach Runde 1).

### Klasse 2 — echte offene Entscheidungen (brauchen den Menschen, nicht nur Nachziehen)

1. **teil-3 (BLOCKER):** Kommissioniert IDEAL **nur auf Blattebene `SubFA = 0`**, oder auch
   fremdbezogene Baugruppen (`SubFA <> 0`)? Der Anhang stuetzt die Blatt-nur-Regel **nicht** (filtert
   allein nach `Kommissionieren`). Ohne Antwort ist die Doppelzaehlungs-Regel geraten.
2. **teil-2 (BLOCKER):** Die 5 neuen Freigabe-Antworten sind **leer** — v. a. die **Web-Mail-Frage**:
   das Web-Projekt hat **keinen** Mail-Versand (`ISyncErrorNotifier` lebt nur im Service). Wie wird die
   Fehlermail bei Tiefen-Cap-/Zyklen-Abbruch zur Web-Request-Zeit verdrahtet?
3. **teil-6 ↔ teil-7 (BLOCKER):** Master-Schalter **read-only** in der Teil-6-Maske (teil-6-Antwort)
   vs. teil-7 fuehrt die Teil-6-Maske als **gesicherten Schreibweg** (AK + Test). Eins muss weichen.
4. **teil-7 ↔ teil-8 (BLOCKER):** teil-8 baut auf `GetAllByFaAndOperationAsync` (mengenwertig) aus
   teil-7 — teil-7-AK ist aber **binaer** und sagt die Variante nicht zu. Zuordnung festlegen.
5. **teil-1 (BLOCKER, Foundation):** Whitelist-Regex `^[A-Za-z0-9_]+$` verwirft den **eigenen
   Default-View-Namen** `vw_IDEAL-AKE_Kommissionierung_FAListe` (Bindestrich) → Sync liefe nie. Fixen.
6. **teil-7 (SOLLTE→BLOCKER Deploy):** `SageMissingSince` (entschieden) in **keiner** Migrationsstufe;
   AgentJob-Serverabschaltung als **harte Deploy-Vorbedingung** wieder aufnehmen (Datei-Loeschen
   entfernt keinen auf `AKESQL20` deployten Job).
7. **teil-5 (SOLLTE):** Wochenbezug-Semantik `Neuer_PT_PPS` (Filter vs. Spalte, Wochengrenze, welcher
   Termin bei Kombigeraet) offen.

### Kritischer Pfad zum Entwicklungsstart

1. **Teil 1 zuerst reif machen** — es ist das Fundament, Teil 2–8 haengen daran. Nachbesserung
   (Bindestrich-Whitelist-Fix + Antworten→Rumpf) → Freigabe → Entwicklung Teil 1. Erst danach faechert
   der Rest auf.
2. **Teil 7 als Epic** (`epic: true`, Etappen A–E) — der Kern, erst nach Teil 1; daten-invasive
   Migration, Backup/Runbook.
3. **Parallel vom Menschen beantworten lassen:** die 4 offenen Entscheidungen (teil-3 Doppelzaehlung,
   teil-2 Web-Mail + 4 weitere, teil-6↔7 Master, teil-7↔8 GetAll).
4. Teil 2–6 (Anzeige/Listen) sind nach Teil 1 untereinander unabhaengig lieferbar (B5).

**Empfehlung: NACHBESSERUNG NOETIG (alle 8) — Entscheidungen sind ueberwiegend da und code-verifiziert,
aber (a) in keinem Rumpf eingearbeitet und (b) 4 echte Entscheidungen offen (teil-2/3 unbeantwortet,
teil-6↔7 + teil-7↔8 Widersprueche). Fruehester echter Dev-Start: Teil 1 nach seiner Nachbesserung +
Freigabe.**
