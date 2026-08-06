---
type: uebersicht
title: "Uebersicht: IDEAL-Standort live schalten — hierarchische Produktionsauftraege (8 Teile)"
slug: 2026-07-29-standort-ideal-uebersicht
status: Entwurf
created: 2026-08-06
updated: 2026-08-06
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
---

## Zweck

Die Backlog-Notiz `[[2026-07-29-Standort-IDEAL]]` beschreibt die Live-Schaltung eines zweiten
Standorts (**IDEAL**, eigenes Deployment, gemeinsamer Codestamm mit **AKE**) samt Umstellung auf
**hierarchische Produktionsauftraege** (Haupt-FA → Sub-FA → Sub-Sub-FA, echter Elternzeiger). Das
Paket ist sehr gross und teils noch nicht entscheidungsreif — deshalb `split: true` mit acht
Teil-Specs, davon Teil 8 als `epic: true`.

**Zur Nummerierung:** Die Ideen-Notiz enthaelt (Stand vor der „Kritischen Pruefung / Aufbereitung
2026-08-06") **drei widerspruechliche Alt-Nummernschemata** — die Zerlegungstabelle
(„Zerlegung & Reihenfolge") nennt Materialisierung=Teil 7 / BDE=Teil 8, der B5-Fliesstext
nennt an mehreren Stellen „Teil 2 (BDE)" / „Teil 1/2", und zusaetzlich tauchen „Teil 1b"
(Stueckliste) und „Teil 1c" (Standorteinstellungen) auf. **Diese Uebersicht verwendet
ausschliesslich EIN Schema — die Zerlegungstabelle der Notiz ist die konsolidierte Wahrheit.**
Die Stueckliste ist laut Befund B4 kein eigener Teil, sondern eine zweite Projektion **von**
Teil 1 (ein Lesepfad, zwei Projektionen); „Teil 1c" (Standorteinstellungen) ist Teil 6.

## Teile in risiko-aufsteigender Reihenfolge

| # | Teil-Spec | Kern (`ProductionOrders`) beruehrt | Zweck |
|---|---|---|---|
| 1 | [[2026-07-29-standort-ideal-teil-1-spec]] | nein | Struktur-Fundament `IdealFaStruktur` (+ `IdealFaInfo`): Import aus den IDEAL-Sage-Views `FAListe`/`FAInfos`, mehrstufige Projektion (`HauptFA`/`VaterFA`/`SubFA`/`Position`), eigene Domain, Repository + Cache-Decorator, konfigurierbare View-Namen. Fundament fuer Teil 2–5. |
| 2 | [[2026-07-29-standort-ideal-teil-2-spec]] | nein | Struktur-/Baumanzeige: rekursive Darstellung von `IdealFaStruktur` (nicht zweistufig — B1 hat den alten 2-Ebenen-Entwurf ueberholt). Riskantester Anzeigeteil (Paging ueber Gruppen, Phantom-Header, Auto-Expand). |
| 3 | [[2026-07-29-standort-ideal-teil-3-spec]] | nein | Kommissionierlisten: Filter `Kommissionieren`, gruppiert nach `HauptFA` (+ Montage-Abteilung), Barcode `HauptFA`, Druck. |
| 4 | [[2026-07-29-standort-ideal-teil-4-spec]] | nein | Beschichtungsauftrag: Filter `Beschichtet = -1`, Druckdokument mit Dienstleister-Kopf. |
| 5 | [[2026-07-29-standort-ideal-teil-5-spec]] | nein | Vormontage-Listen: Filter `VMBedarf`, drei Sichten, Isolierfraesen-Export. |
| 6 | [[2026-07-29-standort-ideal-teil-6-spec]] | nein | Standorteinstellungen-Maske: buendelt Firmenname/Adresse/Mandant/View-Namen/Schalter statt Einzelpflege in der generischen Settings-Oberflaeche. |
| 7 | [[2026-07-29-standort-ideal-teil-7-spec]] | **ja** | Materialisierung nach `ProductionOrders`: Schema-Inversion (`OrderNumber` nicht mehr unique, `SubOrderNumber` unique, `ParentSubOrderNumber`), Einweg-Migrationstor, Sync-Regeln (nicht loeschen / Umhaengung nicht still uebernehmen), FA-Zusatzinfos-Kollision. |
| 8 | [[2026-07-29-standort-ideal-teil-8-spec]] | **ja** | Sub-FA-Rueckmeldung / BDE (`epic: true`). Nach Teil 7 sind Sub-FAs echte `ProductionOrders` — Arbeitsgaenge/Teileverfolgung/Rueckmeldung greifen grundsaetzlich unveraendert, muessen aber gegen die Nicht-Eindeutigkeit von `OrderNumber` gehaertet werden. |

**Abhaengigkeiten (`depends_on`):** Teil 2–6 haengen nur an Teil 1 (lesen `IdealFaStruktur`/
`IdealFaInfo`, kein Schema-Umbau am Kern — Entscheidung B5). Teil 7 haengt an Teil 1 (liest die
Struktur-Tabelle als Quelle der Transformation, siehe dortiger Abschnitt „Synchronisation"). Teil 8
haengt an Teil 7 (braucht echte `SubOrderNumber`-`ProductionOrders`). Teil 2–6 sind **untereinander**
unabhaengig und in beliebiger Reihenfolge lieferbar.

**Alternative Reihenfolge laut Notiz:** Muss Rueckmeldefaehigkeit von Tag eins stehen, koennen 7/8
vorgezogen werden — Teil 1 bleibt trotzdem das Fundament (Architektur aendert sich nicht, nur die
Lieferreihenfolge).

## Querschnitts-Hinweise fuer die Freigabe (Schranke 1)

- **Migrationsnummern:** main ist nach v1.28.0 (Sage-Lagerbuchungen) bei `SQL/83`; die noch in
  `entwurf/` liegenden WmsBugs-Teil-7-Specs belegen bereits **`SQL/84`** und **`SQL/85`**
  (Entwuerfe, noch nicht gemergt). Die IDEAL-Migrationen (Teil 1: `IdealFaStruktur`/`IdealFaInfo`;
  Teil 7: Schema-Inversion) sind daher **ab `SQL/86`** zu planen — **vor dem jeweiligen Dev-Lauf
  erneut pruefen**, ob die Nummer noch frei ist (mehrere Teams koennten parallel umsetzen).
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

1. →
2. →
3. →

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
