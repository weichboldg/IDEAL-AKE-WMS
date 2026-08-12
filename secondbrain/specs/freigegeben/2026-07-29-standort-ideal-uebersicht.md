---
type: uebersicht
title: "Uebersicht: IDEAL-Standort live schalten — hierarchische Produktionsauftraege (8 Teile)"
slug: 2026-07-29-standort-ideal-uebersicht
status: InUmsetzung
created: 2026-08-06
updated: 2026-08-12
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
  - "5: Teil 5 — Vormontage-Listen, zwei Sichten (erweitert den Baustein aus Etappe 3) — ERLEDIGT df5931e (Build+Test gruen; Sicht 2 Matchcode-Aggregat; Wochenbezug bewusst offen)"
  - "6: UI-Nachtrag — Listen-Spaltenauswahl an die vier IDEAL-Listen anschliessen + ADR-0005-Ergaenzung + table-filter Sort-Fix ([[2026-08-12-listen-spaltenauswahl-spec]]) — offen"
  - "7: UI-Nachtrag — FA-Struktur Darstellung: seitenweite Tree-Table (OSEON-Stil), Kontrast-Fix, Knoten-Icons, Baum-Spaltenfilter + column-preferences ([[2026-08-12-fa-struktur-darstellung-spec]]) — offen"
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
| 1 | Struktur-Fundament `IdealFaStruktur` | [[2026-07-29-standort-ideal-teil-1-spec]] | erledigt | de4dcd8 |
| 2 | Struktur-/Baumanzeige | [[2026-07-29-standort-ideal-teil-2-spec]] | erledigt | d526a4e |
| 3 | Kommissionierlisten + gemeinsamer Baustein | [[2026-07-29-standort-ideal-teil-3-spec]] | erledigt | 9ca8141 |
| 4 | Beschichtungsauftrag | [[2026-07-29-standort-ideal-teil-4-spec]] | erledigt | 08aa610 |
| 5 | Vormontage-Listen | [[2026-07-29-standort-ideal-teil-5-spec]] | erledigt | df5931e |
| 6 | UI-Nachtrag: Listen-Spaltenauswahl (+ ADR 0005 + Sort-Fix) | [[2026-08-12-listen-spaltenauswahl-spec]] | erledigt | 37e8752 |
| 7 | UI-Nachtrag: FA-Struktur Tree-Table/Kontrast/Icons/Spaltenfilter | [[2026-08-12-fa-struktur-darstellung-spec]] | offen | |

**Epic am 2026-08-12 von `Testbereit` zurueck auf `InUmsetzung`** — zwei freigegebene UI-Nachtraege
(Etappe 6/7) zu Teil 2–5 werden im SELBEN Worktree mitgebaut und **mitgemergt** (ein Merge). Reihenfolge:
**Etappe 6 (listen-spaltenauswahl) zuerst**, dann Etappe 7 (fa-struktur, seitenweite Tree-Table). Danach
QA erneut (Build/Tests) → wieder Testbereit. Der QA-Nachweis unten (93e54c4) ist damit ueberholt und
am Etappen-Ende neu zu fuehren.

**QA-Nachweis (2026-08-10, Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`,
Branch `feature/2026-08-07-ideal-teile-1-5`, gepruefter Commit `93e54c4`):** `dotnet build
IdealAkeWms.slnx` gruen (0 Fehler, 9 vorbestehende NuGet-/Nullable-Warnungen). `dotnet test`
gruen: **IdealAkeWms.Tests 1131 bestanden, 1 vorbestehend uebersprungen, 0 Fehler** (Gesamt 1132);
**IDEALAKEWMSService.Tests 219 bestanden, 0 Fehler**. `docs/TESTSZENARIEN.md` (Worktree) traegt die
neuen Kapitel 57–61 (TS-57.0–57.11, TS-58.1–58.10, TS-59.1–59.14, TS-60.1–60.13, TS-61.1–61.20);
`secondbrain/tests/testszenarien-index.md` (Hauptcheckout) um Zeilen 57–61 ergaenzt. Status auf
**Testbereit** gesetzt — Schranke 2 (Manual-UAT + Merge) ist Sache des Menschen.

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

## QA-Abnahme Teile 1–5 (2026-08-10) — status: Testbereit

Diese QA-Runde prueft den **Gesamtstand** aller fuenf Etappen im Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch `feature/2026-08-07-ideal-teile-1-5`,
gepruefter Commit `93e54c4`, Version-Bump bereits enthalten: v1.30.0 → **v1.31.0**). Teil 6, 7, 8
sind **nicht** Teil dieses Buendels und bleiben `status: Freigegeben`/eigene Epics.

### Build- und Test-Beweis

```
dotnet build IdealAkeWms.slnx
  Der Buildvorgang wurde erfolgreich ausgeführt. 0 Fehler, 9 vorbestehende Warnungen
  (NU1902 MailKit/MimeKit, 1x CS8602 TrackingController — beide nicht epic-bezogen).

dotnet test
  IdealAkeWms.Tests:        Fehler: 0, erfolgreich: 1131, uebersprungen: 1, gesamt: 1132
  IDEALAKEWMSService.Tests: Fehler: 0, erfolgreich:  219, uebersprungen: 0, gesamt:  219
```

Beide Suiten gruen. Der eine uebersprungene Test (`ProductionOrderEagerCreateAgentJobTests.
EagerCreate_...`) ist vorbestehend und epic-unabhaengig.

### CLAUDE.md-Checkliste (verifiziert)

- **Migration + SQL/XX mit OBJECT_ID-Guard:** `SQL/89_AddFaHierarchy.sql` — beide neuen Tabellen
  (`FaHierarchyNodes`, `FaHierarchyOrderInfos`) unter `OBJECT_ID(...) IS NULL`-Guard, DDL in
  eigenem Batch, `__EFMigrationsHistory`-Insert (`20260807105825_AddFaHierarchy`) in separatem
  Batch, idempotent (mehrfach ausfuehrbar). Migrationsnummer korrekt: main stand nach dem
  wms-bugs-Merge bei `SQL/88`, der Epic-Branch zweigt **nach** diesem Merge ab (Merge-Base
  `2a34ff4` ist Nachfahre von `7b111f1` = wms-bugs-Merge) — `89` ist tatsaechlich frei, keine
  Kollision.
- **`SQL/00_FreshInstall.sql` an beiden Stellen:** Schema-Block „17g. FaHierarchyNodes +
  FaHierarchyOrderInfos" vorhanden, `__EFMigrationsHistory`-Zeile fuer die Migration ebenfalls
  ergaenzt (Zeile 2287/2288).
- **Audit-Felder:** bewusst **nicht** angewandt — `FaHierarchyNodes`/`FaHierarchyOrderInfos` sind
  reine, vom `FaHierarchySyncService` per Full-Refresh befuellte Cache-Tabellen (Praezedenzfall
  `CachedBomHeaders`), kein `AuditableEntity`. Nachvollziehbarkeit ueber `SyncedAt` + SyncLog. Fuer
  die neuen Web-Controller (reine Lesepfade) entfaellt Audit ohnehin.
- **Version-Bump + Changelog:** `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs`
  beide auf `1.31.0`/`2026-08-10`. `Views/Help/Changelog.cshtml` traegt den v1.31.0-Block mit
  explizitem Hinweis „betrifft ausschliesslich IDEAL, standardmaessig ausgeschaltet".
- **`docs/TESTSZENARIEN.md` aktualisiert:** Kapitel 57 (Teil 1, TS-57.0–57.11), TS-58 (Teil 2,
  58.1–58.10), TS-59 (Teil 3, 59.1–59.14), TS-60 (Teil 4, 60.1–60.13), TS-61 (Teil 5, 61.1–61.20)
  — alle im Worktree vorhanden.
- **`secondbrain/tests/testszenarien-index.md` nachgezogen** (Hauptcheckout, diese QA-Runde):
  Zeilen 57–61 im Abschnitt „Nach Release" ergaenzt + Eintrag in „Kapitel mit besonderem Gewicht"
  (Teile 1–5 sind ohne produktivnahe IDEAL-Daten nicht vollstaendig verifizierbar).
- **Neue Rolle `beschichtungsauftrag` (Teil 4) an allen drei Pflichtstellen:** `RoleKeys.
  Beschichtungsauftrag`, `RequireBeschichtungsauftragAccessAttribute` +
  `ICurrentUserService.HasBeschichtungsauftragAccessAsync`, `Views/Users/RoleOverview.cshtml`
  (verifiziert).
- **Service-Settings-Drift-Guard:** 3 neue Keys (`Sync:HierarchicalFaEnabled`,
  `Sync:FaHierarchyListeViewName`, `Sync:FaHierarchyInfosViewName`) in
  `ServiceSettingDefinitions.All` + zugehoerige InlineData im Drift-Guard-Test.
- **`ISyncLogger` als letzter Ctor-Parameter + `SyncLogServices.All`:** `FaHierarchySyncService`
  erhaelt `ISyncLogger` als letzten Parameter; `SyncLogServices.FaHierarchy` registriert.
- **Nicht in dieser QA-Runde nachgezogen (offen fuer die naechste Brain-Pflege-Runde, ausserhalb des
  hier beauftragten Umfangs):** `secondbrain/codebase/controller.md` (neue Controller/Filter/
  Toggles), `secondbrain/feature-map.md`, `secondbrain/changelog/` (Brain-Changelog-Eintrag). Die
  Epic-Abschluss-Checkliste in `secondbrain/aufgaben/2026-08-07-ideal-teile-1-5.md` fuehrt diese
  Punkte weiter.

### Code-Review-Befund (Selbst-Review des Gesamtdiffs main...HEAD, 69 Dateien)

**Wichtiger, nicht blockierender Fund:** `FaHierarchySql.BuildCountSql`/`BuildNodeSelectSql`/
`BuildInfosSelectSql` (IDEALAKEWMSService/Services/FaHierarchySql.cs) hardcoden das Praefix
`FROM dbo.{quotedView}`. `ValidateAndQuote` akzeptiert aber laut eigenem XML-Doc und
`ServiceSettingDefinitions`-Beschreibung auch ein **schema-qualifiziertes** Format
(`[Schema].[Name]`, z. B. `dbo.vw_Foo` → `[dbo].[vw_Foo]`). Wird der View-Name **mit** Schema
konfiguriert, entsteht `FROM dbo.[dbo].[vw_Foo]` — ein fehlerhafter Drei-Teile-Bezeichner, der zur
Laufzeit fehlschlaegt (kein Sicherheitsproblem — Whitelist/QUOTENAME halten, aber ein
Funktionsfehler). Der Default-Wert (`[vw_IDEAL-AKE_Kommissionierung_FAListe]`, ein Segment, kein
Schema) ist davon **nicht** betroffen und funktioniert. **Betriebsempfehlung:** Beim Setzen von
`Sync:FaHierarchyListeViewName`/`Sync:FaHierarchyInfosViewName` am IDEAL-Zielsystem **immer nur den
reinen Objektnamen ohne Schema-Praefix** eintragen (`[ViewName]` oder `ViewName`, **kein**
`dbo.ViewName`/`[dbo].[ViewName]`) — dokumentiert hier, weil es sonst niemand vor dem ersten
Fehlschlag am Zielsystem merkt. Kein Blocker fuer Testbereit (Toggle Default aus, betrifft nur eine
Konfigurationsvariante); vor Produktivgang entweder als bekannte Einschraenkung akzeptieren oder in
einem kleinen Folge-Fix das hartcodierte `dbo.`-Praefix nur anwenden, wenn `ValidateAndQuote` kein
Schema-Segment geliefert hat.

Uebrige Stichproben ohne Befund: DI-Registrierungen (Web `Program.cs`: Repository+Decorator+
Builder+Services+`IBarcodeService`; Service `Program.cs`+`SyncWorker.cs`: `IFaHierarchySyncService`
im eigenen `RunResilientAsync`-Block), alle vier neuen Controller tragen Class-Level-Access-Filter
+ (wo vorgesehen) Toggle-Attribut, Baustein-Naht (`FaHierarchyListBuilder.Build` mit
`leafOnly`/`anomalyOnNonLeaf`-Trailing-Defaults) hat einen expliziten Regressionstest fuer den
Teil-3-Default.

### Deploy

**Betroffene Komponenten (aus dem realen Diff, nicht der urspruenglichen Spec-Schaetzung):**
`deploy.web = true`, `deploy.service = true`, `deploy.migration = true` — alle drei zutreffend
(Web: 4 neue Controller + Views + Repositories; Service: neuer `FaHierarchySyncService`-Sync-Block
in `SyncWorker`; Migration: `SQL/89_AddFaHierarchy.sql` + EF `20260807105825_AddFaHierarchy`, rein
additiv, kein Backup-Zwang — zwei neue, leere Tabellen).

**Publish FROM THE WORKTREE** (Mensch-Fluss: publish → Testsystem → Test → dann Merge):

```powershell
cd C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
```

Nach dem Merge (Schranke 2) nur dann erneut aus `main` publizieren, wenn der Merge tatsaechlich
getestete Dateien mit parallelen main-Aenderungen zusammengefuehrt hat (sonst reicht der bereits
getestete Worktree-Stand).

**Migrations-Reihenfolge:** additive Migration, keine Datenkonvertierung — `dotnet ef database
update` (oder `SQL/89_AddFaHierarchy.sql` manuell) **vor** dem ersten Service-Neustart, damit
`FaHierarchySyncService` beim ersten Lauf nicht gegen fehlende Tabellen laeuft. Kein Service-Stop
zwingend noetig ausserhalb des normalen Deploy-Fensters (additiv, keine Downtime-Anforderung); ein
DB-Backup ist **nicht** zwingend vorgeschrieben (rein additiv, zwei neue leere Tabellen, kein
Datenverlustrisiko) — trotzdem ueblicher Vorsicht halber vor jedem Produktions-Deploy empfohlen.

**Betriebshinweise (vor dem ersten produktiven Sync-Lauf am IDEAL-System zwingend zu setzen):**
- Alle fuenf Feature-Toggles stehen **default aus** und muessen am IDEAL-Zielsystem bewusst aktiviert
  werden: `Sync:HierarchicalFaEnabled` (ServiceSettings), `FaHierarchyKommissionierlistenAktiv`,
  `FaHierarchyBeschichtungAktiv`, `FaHierarchyVormontageAktiv` (alle drei AppSettings) — plus die
  reine Anzeige-Route `/FaHierarchy` (Teil 2, kein eigener Toggle, nur Access-Filter).
- `SageConnection` (Connection-String fuer den Lesezugriff auf die IDEAL-Sage-DB) muss in
  `appsettings` gesetzt sein.
- Die konfigurierbaren View-Namen `Sync:FaHierarchyListeViewName`/`Sync:FaHierarchyInfosViewName`
  muessen gesetzt werden — **ohne Schema-Praefix** (siehe Code-Review-Befund oben), Default
  `[vw_IDEAL-AKE_Kommissionierung_FAListe]`/`[vw_IDEAL-AKE_Kommissionierung_FAInfos]`.
- **RCSI ist AN** am Zielsystem (vorausgesetzt, laut Teil-1-Entscheidung) → der gebaute Primaerweg
  (DELETE beider Tabellen + Neubefuellung in **einer** Transaktion) ist der einzige gebaute Weg;
  **kein** `GRANT ALTER` noetig, kein Staging/Swap. Vor dem ersten Produktivlauf trotzdem einmalig
  `SELECT is_read_committed_snapshot_on FROM sys.databases WHERE name = DB_NAME()` gegen die
  IDEAL-DB gegenpruefen (TS-57.0) — bei `0` ist der gebaute Weg **nicht** einsetzbar (Fallback wurde
  bewusst nicht gebaut).
- Die zwei Sage-View-DDL-Dateien (`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql`,
  `..._FAInfos.sql`) sind nur „Struktur laut Anhang"/TODO-Platzhalter — sie legen am Zielsystem
  **nichts** an. Die realen Views muessen am IDEAL-Sage-System bereits existieren bzw. vor
  Produktivgang final abgestimmt werden.

### Manuelle Test-Checkliste fuer Schranke 2 (Mensch, am IDEAL-Testsystem)

Vorbedingung fuer alle Punkte: das IDEAL-Testsystem war zum Zeitpunkt dieser QA-Runde **leer** —
produktivnahe Daten sind die wichtigste Vorbedingung des gesamten Pakets (siehe Abschnitt
„Querschnitts-Hinweise" oben). Reihenfolge folgt der Abhaengigkeit (Teil 1 zuerst, Sync muss
gelaufen sein, bevor Teil 2–5 sinnvoll pruefbar sind).

1. **RCSI-Check + Migration:** `SELECT is_read_committed_snapshot_on FROM sys.databases WHERE
   name = DB_NAME()` gegen die IDEAL-DB → muss `1` liefern (sonst Fallback-Entscheidung noetig,
   siehe Deploy-Abschnitt). `SQL/89_AddFaHierarchy.sql` (oder `dotnet ef database update`)
   ausfuehren, `FaHierarchyNodes`/`FaHierarchyOrderInfos` existieren leer.
2. **Konfiguration setzen:** `SageConnection`, `Sync:FaHierarchyListeViewName`/
   `Sync:FaHierarchyInfosViewName` (ohne Schema-Praefix), `Sync:HierarchicalFaEnabled = true`.
3. **Teil 1 — Erstimport (TS-57.1–57.6):** Service-Lauf ausloesen, Aktivitaets-Protokoll
   kontrollieren (Counts plausibel, kein Fehlschlag). Eine bekannte Mehrstufigkeits-Struktur
   (Haupt-FA → Sub-FA → Sub-Sub-FA) am Datenbestand identifizieren und die `VaterFA`-Kette
   nachvollziehen. Eine Kombigeraet-Struktur (mehrere `MontageAbteilung`) auf
   Verdopplungsfreiheit pruefen (TS-57.4).
4. **Teil 1 — Full-Refresh ohne Blocking (TS-57.5):** waehrend eines zweiten Laufs parallel
   `/FaHierarchy` oder eine Kommissionierliste aufrufen — erwartet: sofortige Antwort mit dem
   alten Stand, kein Timeout.
5. **Teil 1 — Negativfaelle (TS-57.8/57.9):** ungueltigen View-Namen (Semikolon/Homoglyph) setzen
   → Lauf schlaegt kontrolliert fehl, keine SQL-Ausfuehrung gegen Sage, Fehlermail kommt an; danach
   korrigieren.
6. **Teil 2 — Baumanzeige (TS-58.1–58.9):** `/FaHierarchy` aufrufen (nur per URL, kein Nav-Link),
   mehrstufige Struktur vollstaendig aufklappen, Struktur-Pagination + Struktur-Filter pruefen,
   Kombigeraet-Kopf als „mehrdeutig" pruefen, Zugriff ohne passende Rolle verweigert.
7. **Teil 3 — Kommissionierlisten (TS-59.8–59.13), Toggle `FaHierarchyKommissionierlistenAktiv =
   true`:** Grunddarstellung + Ziel-Dropdown, Spaltenfilter + Gruppen-Paging, Druck spiegelt
   Bildschirm (ein Ausdruck je HauptFA, Barcode = HauptFA).
   **BLOCKER-Punkt (a): TS-59.12 Mengenabgleich —** an einer bekannten mehrstufigen Struktur
   manuell gegenrechnen, ob „nur Blattebene `SubFA = 0`" tatsaechlich die richtige
   Kommissionier-Menge liefert (die Blatt-nur-Regel ist eine Arbeitsannahme, der Anhang stuetzt sie
   nicht explizit) — Anomalie-Banner dabei beobachten (sollte bei sauberen Daten leer bleiben).
8. **Teil 4 — Beschichtungsauftrag (TS-60.8–60.12), Toggle `FaHierarchyBeschichtungAktiv = true`,
   Rolle `beschichtungsauftrag`:** Grunddarstellung (beschichtete Positionen **aller** Ebenen, kein
   Blattfilter), Druck mit Dienstleister-Kopf, Kombigeraet-Kopf mehrdeutig markiert.
   **Offener Punkt (c):** Dienstleister-Layout/Corporate-Design ist noch nicht mit dem
   Fachbereich abgestimmt — vor Produktivgang UI/Layout gegenpruefen.
9. **Teil 5 — Vormontage-Listen (TS-61.13–61.19), Toggle `FaHierarchyVormontageAktiv = true`,
   Rolle `vorbau`:** Sicht 1 (Einzelteile, gruppiert) + Sicht 2 (Matchcode-Aggregat, zwei Summen)
   gegen dieselbe Struktur pruefen (keine Doppelzaehlung ueber Ebenen), Reiter-Wechsel je
   `VMBedarf`-Bereich.
   **Offener Punkt (d):** der Wochenbezug-Filter (`Neuer_PT_PPS`) ist **bewusst nicht** gebaut
   (offene Rueckfrage) — `Neuer_PT_PPS` erscheint hoechstens als Kopf-Anzeige, nie als
   Mengen-Einschraenkung; mit dem Fachbereich klaeren, ob das fuer den Produktivstart ausreicht.
10. **Alle Teile — Anomalie-/Datenqualitaets-Pruefung (b, e):** SubFA=0-Blattannahme (Punkt 7)
    UND Node-Dezimalspalten (`Sollmenge`/`Fertigungmenge`/`Breite`/`Hoehe`/`Tiefe`, laut Sage NOT
    NULL, WMS mappt `NULL → 0`) gegen die echten IDEAL-Daten gegenpruefen — Blaetter ohne Masse
    duerfen nicht faelschlich als „0" interpretiert werden, wenn die Sage-Seite tatsaechlich anders
    befuellt.
11. **Regression AKE (alle Teile):** bestehende AKE-Testszenarien (FA-Liste, Kommissionierung, BDE,
    Beschichtungslogik `CoatingDetectionService`) unveraendert; `ProductionOrders` byte-identisch
    unveraendert; alle Toggles auf `false` zurueckgesetzt → System verhaelt sich exakt wie vor
    diesem Epic.
12. **Konfigurations-Regressionstest View-Name:** wie im Code-Review-Befund beschrieben —
    **nicht** `dbo.ViewName` als View-Namen konfigurieren (fuehrt zu einem fehlerhaften
    Drei-Teile-Bezeichner); nur den reinen Objektnamen eintragen.

Nach erfolgreichem Durchlauf: Merge (Schranke 2, ausschliesslich durch den Menschen; dieser QA-Lauf
merged nicht, pusht nicht, loescht den Worktree nicht).
