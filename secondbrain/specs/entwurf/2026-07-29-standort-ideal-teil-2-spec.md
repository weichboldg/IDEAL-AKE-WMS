---
type: spec
title: "IDEAL-Standort Teil 2 — Struktur-/FA-Baumanzeige (rekursiv, Seiteneinheit Struktur)"
slug: 2026-07-29-standort-ideal-teil-2-spec
status: Entwurf
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/FaHierarchyController.cs (neu)
  - IdealAkeWms/Services/FaHierarchyTreeBuilder.cs (neu — Rekursion, Tiefen-Cap, Visited-Set, Waisen-Erkennung, analog ReadOnlyBomBuilder)
  - IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs (neu — Seite aus Strukturen, je Struktur rekursiver Knotenbaum + Kopfdaten)
  - IdealAkeWms/Views/FaHierarchy/Index.cshtml (neu)
  - IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml (neu — rekursives Partial je Knoten)
  - IdealAkeWms/Models/AppSettingKeys.cs (neu: Key fuer Tiefen-Cap, Default 500 — provisorisch, siehe offene Rueckfrage 5)
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Kombigeraet-Kopfdaten in der Strukturkopfzeile: ein HauptFA mit mehreren FaHierarchyOrderInfo-Zeilen (mehrere MontageAbteilung-Werte) — alle Kopfzeilen im Klartext auffuehren + als mehrdeutig kennzeichnen (analog Uebersicht-Regel und [[2026-08-06-kombinationsgeraete-montageabteilung]]) oder eigene Darstellungsregel fuer diese Baumansicht?"
  - "Integrationsluecke Fehlermail: ISyncErrorNotifier lebt ausschliesslich im Namespace IDEALAKEWMSService.Services (Windows-Service-Projekt) und ist von einem Web-Controller/-Service (IdealAkeWms) nicht direkt injizierbar. Der Tiefen-Cap-/Zyklen-Abbruch passiert aber beim Rendern einer Web-Seite (Request-Zeit), nicht in einem periodischen Sync-Lauf. Wie wird die geforderte Fehlermail technisch verdrahtet — eigener Web-seitiger Mail-Mechanismus, gemeinsame Abstraktion fuer beide Projekte, oder wird die Zyklen-/Tiefenpruefung stattdessen in FaHierarchySyncService (Teil 1, hat ISyncErrorNotifier bereits verdrahtet) vorgelagert und nur das Ergebnis (Fehler-Flag je Struktur) an die Web-Anzeige durchgereicht?"
  - "Aktivitaets-/SyncLog-Eintrag bei Tiefen-Cap-/Zyklen-Abbruch: ISyncLogger/SyncLog (ADR 0010) ist auf periodische Sync-Laeufe mit eigenem, isoliertem DbContext zugeschnitten. Ein Verstoss tritt hier aber pro Web-Request auf. Reicht ein Serilog-Log-Eintrag (kein SyncLog-Lauf), oder wird ein eigener, minimaler SyncLog-Eintrag je Verstoss erzeugt?"
  - "Default-Aufklapp-Zustand und Kopplung des Auto-Expand: Ist die Baumanzeige beim ersten Laden vollstaendig aufgeklappt oder collapsed mit Expand/Collapse wie beim BOM-Tree (ReadOnlyBomBuilder/Views/Picking/Bom.cshtml)? Ist Auto-Expand bei Filtertreffer an das bestehende User-Setting RecursiveFilterSearch gekoppelt (dann muesste es fuer FaHierarchy-Anwender ebenfalls gelten) oder ein eigener, immer aktiver Mechanismus fuer diese Ansicht?"
  - "Konfigurations-Heimat des Tiefen-Caps: AppSettings (ADR 0011, mit oder ohne Seed-Zeile) analog zu den uebrigen reinen Web-Anzeige-Schaltern der Uebersicht, oder eine hartkodierte Konstante im Code (kein DB-Zugriff noetig, aber dann nicht ohne Deploy aenderbar — widerspraeche 'konfigurierbar' aus der B-3-Antwort)?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
created: 2026-08-06
updated: 2026-08-06
---

## Ziel / Nutzen (das Warum)

Teil 1 legt die mehrstufige Struktur (`FaHierarchyNode`/`FaHierarchyOrderInfo`) in der DB ab; Teil 2
macht sie fuer Anwender **erstmals sichtbar** — als **rekursive Baum-/Strukturanzeige**
(Haupt-FA → Sub-FA → Sub-Sub-FA → ..., mehrstufig, **nicht** zweistufig, B1) samt der zugehoerigen
`FaHierarchyOrderInfo`-Kopfdaten (Kunde, Termine, Status) je Struktur. Ohne diesen Teil bleiben die
importierten Daten unsichtbar.

**Korrektur einer vorherigen fehlerhaften Ueberarbeitung.** Eine vorangegangene Runde hatte diesen
Spec-Rumpf faelschlich auf „flache Liste zuerst, Baum als spaeterer Folge-Schritt" umgeschrieben.
Das widerspricht der massgeblichen Entscheidung des Menschen in der „Kritischen Pruefung" am Ende
dieser Datei: die dortige **B-1-„ANTWORT"** stellt ausdruecklich fest, dass der rekursive Baum
**IN SCOPE bleibt**, kein Split in Teil 2a/2b erfolgt, und dass die fruehere Freigabe-Antwort 4
(„hierarchische Darstellung im naechsten Step") **ueberholt und nicht mehr massgeblich** ist. Diese
Ueberarbeitung setzt das um und nimmt die vorherige flache Richtung zurueck (Details siehe
„### Nachbesserung (2026-08-06)" am Ende dieser Datei).

**Teil 2 beruehrt weder `ProductionOrders` noch den Master-Schalter
`ProduktionsauftragHierarchisch`.** Er liest ausschliesslich die eigenen, additiven Teil-1-Tabellen.
Damit ist er **unabhaengig von Teil 7** (Materialisierung) und **vor** Teil 7 lieferbar — der
Schalterbaum-Widerspruch der Ideen-Notiz (Toggle nur wirksam bei Master an) ist bereits auf
Uebersichts-Ebene aufgeloest (B5-Linie, siehe [[2026-07-29-standort-ideal-uebersicht]]).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (diese Runde):**
- Erster erreichbarer IDEAL-Controller `FaHierarchyController` mit Action `Index`: **rekursive
  Baumdarstellung** aller `FaHierarchyNode`-Strukturen, gruppiert nach `HauptFA`
  (Wurzel = `VaterFA IS NULL`), mit **allen** Ebenen der `VaterFA`/`SubFA`-Kette (Sub-FA,
  Sub-Sub-FA, ...) — nicht zweistufig (B1). Jede Struktur wird um ihre zugehoerigen
  `FaHierarchyOrderInfo`-Kopfdaten (Kunde, `KO_Termin`, `FE_Termin`, `Status`, ...) im Strukturkopf
  ergaenzt.
- **Pagination mit Seiteneinheit = Struktur, nicht Zeile (B-1/B-4).** `PageSize.Resolve`/
  `PaginationState`/`_Pagination`-Partial wie in jeder anderen Liste, aber `TotalCount` zaehlt
  **Strukturen** (Wurzeln + Waisen-Pseudowurzeln, siehe unten), nicht Node-Zeilen. Eine Struktur
  wird **nie** ueber zwei Seiten getrennt und **immer vollstaendig** dargestellt (alle Ebenen bis
  zum Tiefen-Cap) — kein Paging innerhalb einer Struktur, kein Phantom-Header, keine
  Zeilenzahl-Schaetzung (entfaellt strukturell, siehe Technischer Loesungsentwurf und
  [[2026-07-29-standort-ideal-uebersicht]], Abschnitt „Ergaenzende Querschnitts-Entscheidungen").
- **Filterkarte, server-seitig auf Struktur-Ebene.** Eine Struktur qualifiziert sich fuer die
  gefilterte Seite, wenn ihr Kopf (`HauptFA`, `Kunde`, `Status`, Termine) **oder** irgendein Knoten
  der Struktur (`Artnr`, `Bezeichnung1/2`, ...) auf den Filtertext passt — die Filterung entscheidet
  **welche Strukturen** erscheinen, **nie**, welche einzelnen Knoten **innerhalb** einer gezeigten
  Struktur sichtbar sind (das wuerde die `VaterFA`/`SubFA`-Kette zerreissen und Kinder verwaisen
  lassen, siehe Fachliche Anforderungen).
- **Client-seitige Baum-Interaktion innerhalb einer angezeigten Struktur** (Expand/Collapse je
  Knoten, Auto-Expand des Pfads zu einem client-seitigen Filtertreffer) — dies ist die in ADR 0005
  **dokumentierte Ausnahme** vom Server-Mode-Spaltenfilter-Pattern (BOM-Tree-Praezedenzfall,
  `ReadOnlyBomBuilder`/`Views/Picking/Bom.cshtml`), nicht ein Verstoss dagegen. Details und offene
  Feinheiten siehe Technischer Loesungsentwurf und offene Rueckfrage 4.
- **Tiefen-Cap (konfigurierbar, Default 500) UND Visited-Set-Zyklenschutz je Struktur (B-3).**
  Verletzung: Aufbau **dieser einen** Struktur bricht ab, uebrige Strukturen bleiben normal
  sichtbar; Protokoll- **und** Fehlermail-Pflicht (technische Verdrahtung ist offene Rueckfrage 2/3,
  siehe unten — die fachliche Anforderung selbst ist nicht offen). Die betroffene Struktur wird als
  **fehlerhaft markiert**, nicht leer dargestellt und nicht still verworfen.
- **Waisen-Behandlung.** Ein Knoten, dessen `VaterFA` auf keinen in `FaHierarchyNode` importierten
  Parent zeigt, verschwindet **nicht** still aus der Anzeige — er wird als eigene, deutlich als
  „verwaist" markierte Pseudo-Wurzel-Struktur gerendert und zaehlt als eigene Struktur fuer
  Pagination/`TotalCount`.
- Class-Level-Access-Filter `[RequirePickingOrTrackingOrLeitstandAccess]` — **derselbe** Filter, der
  heute `ProductionOrdersController` traegt (ADR 0006, B-2). Reine Read-View ⇒ kein Edit-Split
  noetig.

**Out-of-Scope:**
- `ProductionOrders`/AKE unveraendert (harte Bedingung, gilt weiterhin); keine Rueckmeldefunktion
  (Teil 8); keine Kommissionier-/Beschichtungs-/Vormontage-spezifischen Filter (Teil 3–5, eigene
  Views).
- Materialisierung nach `ProductionOrders` (Teil 7) — inklusive der materialisierungsseitigen
  Behandlung von Kombinationsgeraeten.
- Druck/PDF-Ausgabe der Struktur — eigener Querschnitts-Baustein laut Uebersicht
  ([[2026-08-06-pdf-erzeugung-fahierarchy-druck]]), nicht Teil 2.
- Ein zweiter, „flacher" Anzeige-Modus fuer `FaHierarchyController` — diese Runde liefert
  ausschliesslich die Baumdarstellung; ein etwaiger Umschalter zwischen Baum- und Flach-Ansicht
  waere ein eigener, spaeterer Backlog-Punkt und ist nicht Gegenstand dieser Spec.

## Fachliche Anforderungen

- Datenquelle: `IFaHierarchyNodeRepository` (Teil 1, Cache-Decorator, 5 min) fuer die
  Positionszeilen, `IFaHierarchyOrderInfoRepository` (Teil 1) fuer die Kopfdaten je `HauptFA` — Join
  im Anwendungscode, **kein** raw SQL im Controller.
- **Mehrstufigkeit (B1).** Der Baum wird ueber `VaterFA`/`SubFA` rekursiv aufgebaut: ein Knoten mit
  `VaterFA IS NULL` ist eine Wurzel; seine Kinder sind alle Knoten, deren `VaterFA` dem `SubFA`
  dieses Knotens entspricht (innerhalb desselben `HauptFA`); das setzt sich fort, bis ein Knoten
  `SubFA = 0` (Blatt) hat oder keine Kinder mehr gefunden werden. Keine Begrenzung auf zwei Ebenen.
- **Zyklenschutz UND Tiefen-Cap je Struktur (B-3, verbindlich).**
  - Tiefen-Cap: konfigurierbar, **Default 500** — bewusst hoch, weil die reale Tiefe noch nicht
    bekannt ist; reine Reissleine, keine fachliche Grenze.
  - Zyklenschutz zusaetzlich: **Visited-Set je Struktur** (z. B. besuchte `SubFA`-Werte innerhalb
    des aktuellen Traversierungspfads) — ein Zyklus ist nicht dasselbe wie grosse Tiefe und wird vom
    Cap nur zufaellig und zu spaet erkannt.
  - Verhalten bei Verletzung (beide Faelle): Aufbau **dieser einen** Struktur bricht ab, die
    uebrigen Strukturen der Seite werden normal und vollstaendig angezeigt. Kein Abbruch der ganzen
    Seite.
  - Meldung: Eintrag im Aktivitaets-/SyncLog **und** Fehlermail, mit `HauptFA` und Abbruchstelle
    (technische Verdrahtung — welcher Mechanismus vom Web-Request aus erreichbar ist — siehe offene
    Rueckfragen 2/3; die fachliche Anforderung selbst steht fest).
  - Anzeige: Die betroffene Struktur wird als fehlerhaft gekennzeichnet, nicht leer dargestellt.
- **Waisen-Behandlung (verwandt mit AK aus Teil 1, hier fuer die Baumdarstellung konkretisiert).**
  Ein Knoten, dessen `VaterFA` auf keinen anderen Knoten (`SubFA`-Wert) in `FaHierarchyNode`
  verweist, wird als eigene Pseudo-Wurzel behandelt: er (und seine ggf. vorhandenen Kinder) wird als
  eigenstaendige, deutlich markierte Struktur gerendert (z. B. Badge/Hinweistext „Verwaist —
  `VaterFA` <Wert> nicht gefunden"), nicht als Teil einer anderen Struktur und nicht stillschweigend
  ausgelassen.
- **Server-Filter vs. Baum-Integritaet (loest den scheinbaren ADR-0005-Widerspruch, S-1).** Ein
  echter server-seitiger Zeilen-Spaltenfilter, der einzelne, nicht passende `FaHierarchyNode`-Zeilen
  aus einer bereits zur Anzeige qualifizierten Struktur entfernt, wuerde deren Kinder verwaisen
  lassen — das widerspraeche der eigenen Waisen-Anforderung oben. Deshalb gilt fuer diese
  Baumansicht die in ADR 0005 **dokumentierte BOM-Tree-Ausnahme**: server-seitig wird nur auf
  Struktur-Ebene gefiltert (welche Strukturen erscheinen), innerhalb einer angezeigten Struktur ist
  die Filterung client-seitig (Hervorhebung/Sichtbarkeit einzelner Zeilen ohne die Baumstruktur zu
  zerstoeren), analog zum bestehenden `ReadOnlyBomBuilder`/`Views/Picking/Bom.cshtml`-Muster.
- Zugriffsschutz: Class-Level `[RequirePickingOrTrackingOrLeitstandAccess]` auf
  `FaHierarchyController` — derselbe Filter, der heute `ProductionOrdersController.Index` (slim
  FA-Liste) schuetzt (ADR 0006; B-2 konkret ausgelegt: **bestehenden** Filter wiederverwenden, nicht
  filterlos lassen). Reine Read-View ⇒ kein Edit-Split noetig.
- Kopfdaten-Anzeige: Ein `HauptFA` mit **mehreren** `FaHierarchyOrderInfo`-Zeilen (Kombigeraet,
  mehrere `MontageAbteilung`-Werte) ist ein bekannter, aus Teil 1 uebernommener Fall — genaue
  Darstellung im Strukturkopf ist offene Rueckfrage 1.

## Technischer Loesungsentwurf

- **`FaHierarchyTreeBuilder`** (neuer Service, analog `ReadOnlyBomBuilder`): laedt alle
  `FaHierarchyNode`-Zeilen ueber `IFaHierarchyNodeRepository.GetAllAsync()` (Cache-Decorator, Teil
  1) und gruppiert sie nach `HauptFA`. Innerhalb jeder Gruppe:
  1. Wurzel(n) ermitteln (`VaterFA IS NULL`).
  2. Knoten, deren `VaterFA` auf keinen `SubFA`-Wert **innerhalb** der Gesamtmenge verweist, als
     eigene Waisen-Pseudowurzeln markieren (siehe Fachliche Anforderungen).
  3. Je Wurzel/Pseudowurzel rekursiv Kinder anhaengen (`child.VaterFA == parent.SubFA`), dabei ein
     Visited-Set (besuchte `SubFA`-Werte im aktuellen Pfad) und einen Tiefenzaehler mitfuehren;
     Ueberschreitung des Caps oder ein bereits besuchter `SubFA` im Pfad bricht **nur** diese
     Struktur ab und markiert sie als fehlerhaft (siehe Fachliche Anforderungen).
  4. `IFaHierarchyOrderInfoRepository` liefert die Kopfdaten je `HauptFA` fuer die auf der Seite
     gebauten Strukturen (kein Fan-out auf Node-Ebene).
- **Struktur-Filter (server-seitig):** Vor dem Bau des Baums wird pro `HauptFA`-Gruppe geprueft, ob
  der Filtertext auf Kopf **oder** irgendeinen Knoten passt; nur qualifizierende Gruppen werden zu
  vollstaendigen Baeumen aufgebaut und paginiert — unnoetiger Rekursionsaufwand fuer nicht
  angezeigte Strukturen entfaellt dadurch.
- **Pagination:** `PageSize.Resolve`/`PaginationState` wie im Referenz-Pattern
  (`ProductionOrdersController.Index`, ADR 0005), aber angewandt auf die Liste der **qualifizierten
  Strukturen** (nicht Zeilen); `TotalCount` = Anzahl qualifizierter Strukturen inkl.
  Waisen-Pseudowurzeln. Default-Seitengroesse 25 Strukturen (wie jede andere Liste), `PageSize.AllCap`
  fuer „Alle" mit sichtbarem Cap-Hinweis analog `IsCappedAtAll`.
- **View:** `Views/FaHierarchy/Index.cshtml` (Ordnername passend zu `FaHierarchyController`) rendert
  je Struktur einen Kopfbereich (Kopfdaten aus `FaHierarchyOrderInfo`, Waisen-/Fehler-Badge) und
  darunter das rekursive Partial `Views/FaHierarchy/_FaHierarchyNode.cshtml` (ruft sich fuer jedes
  Kind selbst auf, analog zu bestehenden rekursiven Partial-Mustern), gerendert mit
  `data-node-id`/`data-parent-id`-Attributen (Knotenidentitaet ueber `SubFA` innerhalb der Struktur,
  **nicht** ueber eine Punkt-Positions-Zeichenkette wie beim BOM-Tree, da `FaHierarchyNode` keine
  vergleichbare hierarchische Positions-Notation fuehrt).
- **Client-seitige Baum-Interaktion:** Expand/Collapse je Knoten (JS-`expandedState`-Map analog
  `Views/Picking/Bom.cshtml`, aber keyed auf `data-node-id` statt Positions-String), „Alle
  aufklappen"/„Alle zuklappen"-Buttons. Auto-Expand bei client-seitigem Spaltenfiltertreffer: ein
  Treffer in einem eingeklappten Ast macht dessen Vorfahren-Kette sichtbar (analog
  `recursiveFilterSearch`/`RecursiveFilterSearch`-Mechanik des BOM-Tree) — ob dies an dasselbe
  User-Setting gekoppelt wird oder ein eigener, immer aktiver Mechanismus fuer diese Ansicht ist, ist
  offene Rueckfrage 4.
- Filterkarte fuer die globale Struktur-Suche (`HauptFA`, `Kunde`, plus Knoten-Attribute wie `Artnr`,
  `Bezeichnung1/2`) ueber der Strukturliste.
- Sichtbare Knoten-Attribute (`HauptArtnr`, `Artnr`, `Matchcode`, `Bezeichnung1/2`, `Kommissionieren`,
  `Arbeitsbereich`, `Sollmenge`, `Fertigungmenge`, `Beschichtet`, ...) werden je Knoten dargestellt;
  ein client-seitiger Filter auf diesen Spalten hebt Treffer hervor/klappt sie auf, entfernt aber
  **keine** Knoten aus der DOM-Struktur (Baum-Integritaet, siehe Fachliche Anforderungen).
  `KO_Termin`/`FE_Termin` sind laut Teil 1 **gespeicherte**, nicht berechnete Werte aus der
  PPS-View — sie werden im Strukturkopf angezeigt, nicht als Knoten-Spalte.

## Migrations-/SQL-Auswirkungen

Keine Schema-Migration. Reine Lesefunktion auf den in Teil 1 angelegten Tabellen. Falls der
Tiefen-Cap als `AppSettings`-Key gefuehrt wird (ADR 0011, siehe offene Rueckfrage 5), ist dafuer
**keine** DB-Migration noetig — `AppSettings` ist eine generische Key/Value-Tabelle; offen ist nur,
ob eine Default-Zeile geseedet wird oder der Code ohne vorhandene Zeile auf `500` defaultet (Teil
der offenen Rueckfrage 5, kein Blocker fuer die Spec-Freigabe an sich).

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten mit Audit-Pflicht. Reine Anzeige.

## Akzeptanzkriterien

1. Eine Struktur wird **rekursiv und mehrstufig** dargestellt (Haupt-FA → Sub-FA → Sub-Sub-FA →
   ...), nicht nur zweistufig — verifiziert an einer Teststruktur mit mindestens einer
   Sub-Sub-FA-Ebene (B1).
2. **Pagination-Seiteneinheit ist die Struktur, nicht die Zeile.** `TotalCount` zaehlt Strukturen
   (inkl. Waisen-Pseudowurzeln); eine Struktur erscheint **immer vollstaendig** auf genau einer
   Seite und wird **nie** ueber zwei Seiten getrennt (loest den fruehen Widerspruch alte-AK-1
   „alle Zeilen" zugunsten „Seiteneinheit = Struktur" auf, B-1/B-4).
3. Ein Benutzer **ohne** `picking`/`tracking`/`leitstand`/`admin`-Rolle kann
   `FaHierarchyController.Index` **nicht** aufrufen (403/Redirect wie beim bestehenden Muster von
   `ProductionOrdersController`, B-2).
4. **Tiefen-Cap (Default 500) und Zyklenschutz (Visited-Set) greifen je Struktur unabhaengig
   voneinander.** Bei Ueberschreitung/Zyklus bricht **nur** der Aufbau der betroffenen Struktur ab;
   alle uebrigen Strukturen der Seite bleiben normal und vollstaendig sichtbar; die betroffene
   Struktur wird als **fehlerhaft markiert**, nicht leer dargestellt, nicht still entfernt (B-3).
5. Ein `FaHierarchyNode`-Datensatz, dessen `VaterFA` auf keinen importierten Parent zeigt, erscheint
   als eigene, deutlich als „verwaist" markierte Pseudo-Wurzel-Struktur — kein stilles Verschwinden.
6. Jede Struktur zeigt im Kopf die zugehoerigen `FaHierarchyOrderInfo`-Daten (mindestens `Kunde`,
   `KO_Termin`, `FE_Termin`, `Status`) fuer ihren `HauptFA`.
7. Die Filterkarte wirkt server-seitig auf **Struktur-Ebene**: eine Struktur erscheint auf der Seite,
   wenn Kopf oder mindestens ein Knoten passt; `TotalCount`/Pagination beziehen sich auf die
   gefilterte Strukturmenge. Innerhalb einer angezeigten Struktur werden **keine** Knoten durch den
   Filter aus der Anzeige entfernt (Baum-Integritaet, S-1/ADR-0005-Ausnahme).
8. `ProductionOrders` (Schema, Zeilenzahl, Verhalten aller bestehenden Controller) bleibt **byte-
   identisch unveraendert** — AKE-Verhalten unangetastet (harte Akzeptanzbedingung fuer jeden Teil).

## Test-Szenarien

Neues Kapitel „IDEAL Teil 2 — FA-Baumanzeige" in `docs/TESTSZENARIEN.md`:

- **Grunddarstellung mehrstufig:** Eine bekannte Struktur mit Sub-Sub-FA wird rekursiv vollstaendig
  dargestellt (alle Ebenen sichtbar/aufklappbar).
- **Struktur-Pagination:** Seitenwechsel 25/50/100/Alle funktioniert auf Struktur-Ebene; keine
  Struktur wird ueber zwei Seiten getrennt; `TotalCount` entspricht der Anzahl Strukturen.
- **Zyklen-/Tiefen-Test:** Testdatensatz mit kuenstlichem Zyklus bzw. einer Tiefe > Cap praeparieren
  (Testsystem) — erwartet: nur diese eine Struktur wird als fehlerhaft markiert, alle uebrigen
  Strukturen bleiben normal sichtbar, Protokoll-/Mail-Mechanismus greift (Details je nach Antwort
  auf offene Rueckfrage 2/3).
- **Waisen-Test:** Testdatensatz mit `VaterFA` auf einen nicht existierenden Parent erscheint als
  eigene, markierte Pseudo-Wurzel-Struktur.
- **Struktur-Filter:** Filtertext, der nur auf einen Knoten tief in einer Struktur passt, laesst die
  ganze Struktur erscheinen (nicht nur den Treffer-Knoten); eine Struktur ohne jeden Treffer
  verschwindet komplett von der Seite.
- **Zugriffstest:** Benutzer ohne `picking`/`tracking`/`leitstand`/`admin`-Rolle wird abgewiesen;
  Benutzer mit einer der genannten Rollen sieht die Baumanzeige.
- **Regressionsfall:** bestehende AKE-FA-Liste (`ProductionOrdersController`) bleibt unveraendert.

Nach Klaerung der offenen Rueckfragen (insbesondere 1 Kombigeraet-Kopfdaten, 2/3 Fehlermail-/Log-
Verdrahtung, 4 Auto-Expand-Default) zu praezisieren.

## Deploy

- **Web-App:** ja (neuer Controller/View/Service, keine Service-/Migrationsaenderung).
- **Service:** nein.
- **Migration:** nein (vorbehaltlich der Klaerung zur AppSettings-Seed-Zeile fuer den Tiefen-Cap,
  offene Rueckfrage 5 — auch im Seed-Fall reine Daten-, keine Schema-Aenderung).
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch, vom Dev-Lauf zu bestaetigen).

## Offene Rueckfragen

1. Kombigeraet-Kopfdaten in der Strukturkopfzeile: ein `HauptFA` mit mehreren
   `FaHierarchyOrderInfo`-Zeilen (mehrere `MontageAbteilung`-Werte) — alle Kopfzeilen im Klartext
   auffuehren + als mehrdeutig kennzeichnen (analog Uebersicht-Regel und
   [[2026-08-06-kombinationsgeraete-montageabteilung]]) oder eigene Darstellungsregel fuer diese
   Baumansicht?
2. Integrationsluecke Fehlermail: `ISyncErrorNotifier` lebt ausschliesslich im Namespace
   `IDEALAKEWMSService.Services` (Windows-Service-Projekt) und ist von einem Web-Controller/-Service
   (`IdealAkeWms`) nicht direkt injizierbar. Der Tiefen-Cap-/Zyklen-Abbruch passiert aber beim
   Rendern einer Web-Seite (Request-Zeit), nicht in einem periodischen Sync-Lauf. Wie wird die
   geforderte Fehlermail technisch verdrahtet — eigener Web-seitiger Mail-Mechanismus, gemeinsame
   Abstraktion fuer beide Projekte, oder wird die Zyklen-/Tiefenpruefung stattdessen in
   `FaHierarchySyncService` (Teil 1, hat `ISyncErrorNotifier` bereits verdrahtet) vorgelagert und nur
   das Ergebnis (Fehler-Flag je Struktur) an die Web-Anzeige durchgereicht?
3. Aktivitaets-/SyncLog-Eintrag bei Tiefen-Cap-/Zyklen-Abbruch: `ISyncLogger`/`SyncLog` (ADR 0010)
   ist auf periodische Sync-Laeufe mit eigenem, isoliertem DbContext zugeschnitten. Ein Verstoss
   tritt hier aber pro Web-Request auf. Reicht ein Serilog-Log-Eintrag (kein SyncLog-Lauf), oder wird
   ein eigener, minimaler SyncLog-Eintrag je Verstoss erzeugt?
4. Default-Aufklapp-Zustand und Kopplung des Auto-Expand: Ist die Baumanzeige beim ersten Laden
   vollstaendig aufgeklappt oder collapsed mit Expand/Collapse wie beim BOM-Tree
   (`ReadOnlyBomBuilder`/`Views/Picking/Bom.cshtml`)? Ist Auto-Expand bei Filtertreffer an das
   bestehende User-Setting `RecursiveFilterSearch` gekoppelt (dann muesste es fuer
   FaHierarchy-Anwender ebenfalls gelten) oder ein eigener, immer aktiver Mechanismus fuer diese
   Ansicht?
5. Konfigurations-Heimat des Tiefen-Caps: `AppSettings` (ADR 0011, mit oder ohne Seed-Zeile) analog
   zu den uebrigen reinen Web-Anzeige-Schaltern der Uebersicht, oder eine hartkodierte Konstante im
   Code (kein DB-Zugriff noetig, aber dann nicht ohne Deploy aenderbar — widerspraeche
   „konfigurierbar" aus der B-3-Antwort)?

## Freigabe-Antworten zu den neuen Rueckfragen (Mensch fuellt aus — Schranke 1)

1. →
2. →
3. →
4. →
5. →

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →wenn hierarchisch dargestellt, alle zur hierarchie gehörenden aufträge anzeigen, bei flacher liste kann gern nach 25 gewechselt werden
2. →ProduktionsauftragBaumAnzeige betrifft eher die appsettings als den service
3. →keine neue rollen, alles wie gehabt
4. →hierarchische darstellung kann im nächsten step erfolgen

## Kritische Pruefung (2026-08-06)

Anwalt-des-Teufels-Durchsicht VOR der Freigabe. Gegengelesen: diese Teil-2-Spec, die Teil-1-Spec
(Datenquelle `FaHierarchyNode`/`FaHierarchyOrderInfo`), die Uebersichts-Spec, die Ideen-Notiz inkl.
„Kritische Pruefung", der Anhang [[sage-views-ideal]], ADR 0005 (Listen-View) und ADR 0006 (Rollen)
sowie der reale Code (`ReadOnlyBomBuilder`/`BomViewModels` als einzige bestehende „Baum"-Sicht,
`RoleKeys`, `AppSettingKeys`, `controller.md`).

**Kern-Beobachtung vorweg:** Die Freigabe-Antworten wurden eingetragen, **aber der Spec-Rumpf
(In-Scope, Technischer Loesungsentwurf, Akzeptanzkriterien, Offene Rueckfragen) ist unveraendert
stale**. Die AKs und die vier „Offenen Rueckfragen" beschreiben noch den Zustand VOR den Antworten.
Ein `/dev`-Lauf auf diesem Text baut gegen einen widerspruechlichen Auftrag.

### BLOCKER — vor der Freigabe/dem Dev-Lauf zu klaeren

**B-1 — Freigabe-Antwort 4 stellt den GESAMTEN In-Scope in Frage (teuerster Fehler).**
Die In-Scope-, Anforderungs- und AK-Bloecke drehen sich zentral um den **rekursiven Baum**
(Paging ueber Gruppen, Phantom-Header, Auto-Expand) — den die Spec selbst als „riskantesten Teil"
bezeichnet. Antwort 4 sagt aber woertlich: „**hierarchische darstellung kann im naechsten step
erfolgen**". Das deutet auf ein **Descoping der Baumdarstellung** hin: zuerst die flache Liste,
Baum spaeter. Zusaetzlich kollidiert Antwort 1 („bei flacher liste kann gern nach 25 gewechselt
werden") mit AK 1, die die flache Liste als „**alle** `FaHierarchyNode`-Zeilen" definiert (alles
vs. paginiert 25). Bevor irgendein Code entsteht, muss entschieden werden: liefert Teil 2 jetzt
**nur die flache, paginierte Liste** (und der Baum wird eigener Folge-Teil), oder den vollen Baum?
Ohne diese Entscheidung baut der Dev-Lauf mit hoher Wahrscheinlichkeit genau die aufwaendige
rekursive Paging-Mechanik, die der Mensch gerade verschoben hat.
=> **ANTWORT (2026-08-06): Kein Descoping — der rekursive Baum bleibt IN SCOPE.** Kein Split in
   Teil 2a/2b; S-4 ist damit abgelehnt. Die fruehere Freigabe-Antwort 4 ("hierarchische
   Darstellung im naechsten Step") ist hiermit **ueberholt** und nicht mehr massgeblich.
   Zur Paging-Kollision mit Antwort 1: siehe Antwort zu B-4 — die Seiteneinheit ist die
   **Struktur**, nicht die Zeile. AK 1 ("alle `FaHierarchyNode`-Zeilen") ist entsprechend zu
   korrigieren.

**B-2 — Erste erreichbare IDEAL-Route ohne benannten Access-Filter.**
Teil 1 hatte bewusst keine Route; Teil 2 fuegt die **erste** hinzu. Antwort 3 („keine neue Rollen,
alles wie gehabt") ist **kein Filtername**. ADR 0006 verlangt einen Class-Level-Read-Filter; ein
Controller **ohne** `RequireXxxAccess`-Attribut ist fuer **jeden eingeloggten Benutzer** erreichbar
— das ist eine stille Zugriffsentscheidung, kein „wie gehabt". Der konkrete bestehende Filter muss
benannt werden (Kandidat analog zur slim FA-Liste: `[RequirePickingOrTrackingOrLeitstandAccess]`
auf `ProductionOrdersController`; oder `picking`/`vorbau`). Read-only-View ⇒ Read-Filter genuegt,
kein Edit-Split noetig — aber der Name gehoert in die Spec, nicht in den Dev-Lauf.
=> **ANTWORT (2026-08-06): Bestehendes Berechtigungssystem 1:1 uebernehmen — keine neuen Rollen,
   keine neuen Filter.** Konkret: Der neue `FaHierarchyController` bekommt **denselben**
   Class-Level-Read-Filter, den heute die bestehende FA-/Produktionsauftrags-Liste traegt. Der
   Dev-Lauf liest den Filternamen aus dem Class-Level-Attribut von `ProductionOrdersController`
   und setzt ihn identisch. Read-only-View ⇒ Read-Filter genuegt, kein Edit-Split; ADR 0006 damit
   erfuellt.
   **Ein Controller ohne Filter ist keine zulaessige Auslegung von "wie gehabt".** Sollte sich
   beim Nachsehen zeigen, dass dort kein Class-Level-Filter existiert, ist das ein Fund und
   zurueckzumelden — nicht stillschweigend zu uebergehen.

**B-3 — Rekursion ohne Zyklen-/Tiefenschutz = Produktions-Endlosschleife.**
Die Anforderung lautet „rekursiv bis keine weiteren Kinder". Es gibt **keinen** Visited-Set-, kein
Max-Tiefen-Guard. Eine defekte `VaterFA`-Kette (Zyklus, oder `VaterFA` zeigt im Kreis) fuehrt zu
Stack-Overflow/Hänger — und die Ideen-Notiz behandelt genau eine geaenderte/kaputte `VaterFA`
(Umhaengung) explizit als **real moegliche Invarianz-Verletzung, nicht als Annahme**. Wichtig:
**es gibt keinen wiederverwendbaren Praezedenzfall** — die einzige bestehende „Baum"-Sicht
(`ReadOnlyBomBuilder`) leitet die Ebene aus der **Positions-Zeichenkette** (Anzahl Punkte in „15.1")
ab, **nicht** aus einer Zeiger-Rekursion; sie kann also gar nicht zyklen-sicher sein. Zyklenschutz +
Tiefen-Cap muessen als AK spezifiziert werden.
=> **ANTWORT (2026-08-06): Tiefen-Cap UND Zyklenschutz — beides spezifizieren.**
   - **Tiefen-Cap:** konfigurierbar, **Default 500**. Bewusst hoch gewaehlt, weil die reale Tiefe
     noch nicht bekannt ist und gross sein kann. Der Cap ist eine Reissleine, keine fachliche
     Grenze.
   - **Zyklenschutz zusaetzlich:** Visited-Set je Struktur. Ein Zyklus ist NICHT dasselbe wie
     grosse Tiefe — der Cap faengt ihn nur zufaellig und viel zu spaet ab.
   - **Verhalten bei Verletzung (beide Faelle):** Der Aufbau **dieser einen** Struktur bricht ab,
     die uebrigen Strukturen werden normal angezeigt. Kein Abbruch der ganzen Seite.
   - **Meldung:** Eintrag im Aktivitaets-/SyncLog **und** Fehlermail (`ISyncErrorNotifier`), mit
     HauptFA und Abbruchstelle. Der Fall ist eine Invarianz-Verletzung (vgl. Umhaengung in der
     Ideen-Notiz) und muss auffallen, nicht still abgeschnitten werden.
   - **Anzeige:** Die betroffene Struktur wird als fehlerhaft gekennzeichnet, nicht leer
     dargestellt.
**B-4 — Die Kern-Mechanik ist Prosa, kein Spec.**
Der Technische Loesungsentwurf gibt selbst zu: Paging ueber Gruppen, Phantom-Header bei
abgeschnittenen Strukturen, Zeilenzahl-Schaetzung und Auto-Expand-Algorithmus sind „**bewusst noch
nicht im Detail ausgearbeitet**" (Offene Rueckfrage 1). Damit ist der eigentliche Inhalt von Teil 2
nicht implementierbar. Antwort 1 loest das nur teilweise (im Baum „alle zur Hierarchie gehoerenden
Auftraege" — was faktisch **kein Paging innerhalb einer Struktur** bedeutet, aber die Frage nach
einer Obergrenze fuer die Zahl der Strukturen pro Seite offen laesst). Solange dieser Teil offen
ist, ist die Spec fuer den Baum-Anteil **nicht freigabereif** (fuer die flache Liste ggf. schon —
siehe B-1/S-4).
=> **ANTWORT (2026-08-06): Die Seiteneinheit ist die STRUKTUR, nicht die Zeile.** Die genaue
   Tiefenstruktur ist noch nicht greifbar, deshalb die einfachste tragfaehige Regel: Eine Struktur
   wird **immer vollstaendig** dargestellt — alle Knoten, die zum Haupt-FA gehoeren, im
   schlimmsten Fall also alle.
   **Damit entfaellt der gesamte komplexe Anteil:** kein Paging innerhalb einer Struktur, keine
   Phantom-Header, keine Zeilenzahl-Schaetzung, kein Auto-Expand ueber Seitengrenzen. Paginiert
   wird ueber die **Anzahl der Strukturen je Seite** (Vorschlag 25, wie die uebrigen Listen).
   Der Loesungsentwurf ist an dieser Stelle also zu **vereinfachen**, nicht auszuarbeiten — B-4
   und der Grossteil von S-4 erledigen sich damit.
### SOLLTE — macht den Dev-Lauf sicherer

**S-1 — Widerspruch zu ADR 0005 (Baum ist dort ausdruecklich Ausnahme).**
Die Spec behauptet „Spaltenfilter (ADR 0005) gelten weiterhin serverseitig". ADR 0005 listet
hierarchische Baumdarstellungen (namentlich **BOM-Tree**) aber als **begruendete Ausnahme** vom
Pattern. Server-Mode-Spaltenfilter sind mit Rekursion **und** Paging-ueber-Gruppen kaum vereinbar.
Entscheiden: Client-Mode (wie BOM-Tree) oder echter Server-Mode? Zusatz: die
Datums-in-C#-nach-Termin-Berechnung-Regel passt nicht, weil IDEAL-Termine **gespeichert** aus der
PPS-View kommen (`FE_Termin` etc.), nicht berechnet werden — und diese Felder liegen auf
`FaHierarchyOrderInfo`, das die Spec gar nicht liest (siehe S-2).

**S-2 — `FaHierarchyOrderInfo` fehlt in der Anzeige komplett.**
Der Baum/die Liste liest ausschliesslich `FaHierarchyNode` (nur `IFaHierarchyNodeRepository` im
Technischen Loesungsentwurf). Woher kommen die Auftrags-Kopfdaten (Kunde, Termine, Status), die den
ganzen INNER-JOIN-Sinn aus Teil 1 tragen? Entweder sind sie Out-of-Scope (dann explizit sagen) oder
`IFaHierarchyOrderInfoRepository` + Join gehoeren in den Loesungsentwurf.

**S-3 — Verwaiste Knoten verschwinden lautlos (verletzt AK 4).**
Baut man den Baum aus den `VaterFA IS NULL`-Wurzeln, wird eine Zeile, deren `VaterFA` auf einen
**nicht importierten** Sub-FA zeigt (z. B. weil dessen Struktur durch die INNER-JOIN-Regel oder
einen Teil-Import wegfiel), von **keiner** Wurzel erreicht → sie faellt still aus der Anzeige. AK 4
verspricht „keine stillen Caps/Datenverluste", aber nichts erkennt Waisen. Orphan-Detection als AK
ergaenzen.

**S-4 — Zu gross fuer einen sauberen Dev-Lauf; Split empfohlen.**
Rekursiver Baum + Gruppen-Paging + Phantom-Header + Auto-Expand + flacher Dritt-Zustand +
Spaltenfilter + neuer Controller/View/JS + AppSetting-Seed ist zu viel auf einmal — und der
riskanteste Teil ist unspezifiziert. Deckt sich mit Antwort 4. Empfehlung: **Teil 2a** (flache,
paginierte Liste — sofort lieferbar) / **Teil 2b** (rekursiver Baum — naechster Step).

**S-5 — AppSetting-Seed nicht in `affected_code`.**
Die Migrations-Sektion nennt einen „reinen Daten-Seed (`AppSettings`-Tabelle)", aber `affected_code`
listet nur `AppSettingKeys.cs` — **kein** `SQL/xx`-Seed-Skript und **kein** `00_FreshInstall.sql`.
Klaeren: braucht `ProduktionsauftragBaumAnzeige` eine DB-Seed-Zeile (dann Skript benennen) oder
defaultet der Key im Code ohne Zeile (dann so sagen)?

### HINWEIS

**H-1 — Der markierte „teuerste Fehler" (Toggle-Baum vs. B5) ist faktisch aufgeloest — aber nur
durch die Architektur, nicht durch eine ausdrueckliche Aussage.** Teil 2 liest `FaHierarchyNode`
(von Teil 1s `Sync:HierarchicalFaEnabled` befuellt) und beruehrt **weder** `ProductionOrders` **noch**
den Master `ProduktionsauftragHierarchisch`. Damit ist die Baumanzeige **unabhaengig vom
(noch nicht existenten) Master** und tatsaechlich VOR Teil 7 lieferbar — genau wie B5 verlangt und
die Uebersicht (Offene Rueckfrage 2) bestaetigt. Der Schalterbaum der Ideen-Notiz
(„nur wirksam wenn Master an") ist ueberholt. **Ein Satz in der Spec, der das explizit festhaelt,
verhindert, dass ein Dev den Toggle faelschlich an den Master haengt.**

**H-2 — View-Ordner-Mismatch.** `affected_code` nennt `Views/FaHierarchyNode/Index.cshtml`, der
Controller heisst aber `FaHierarchyController` → MVC sucht in `Views/FaHierarchy/`. Angleichen.

**H-3 — Toggle-Namensfamilie.** `ProduktionsauftragBaumAnzeige` (deutsch, `ProduktionsauftragHierarchisch`-
Familie) passt zur bestehenden `AppSettingKeys`-Konvention (deutsche PascalCase-Keys), weicht aber
von Teil 1s englischen Konzeptnamen (`Sync:HierarchicalFaEnabled`) ab. Bewusst so waehlen und den
Key in `AppSettingKeys.cs` eintragen (Datei ist in `affected_code`, gut).

**H-4 — Spec-Rumpf an die Freigabe-Antworten angleichen.** Die vier „Offenen Rueckfragen" und die
AKs muessen nach den Antworten neu geschrieben werden (Antwort 2 ⇒ AppSettings ist entschieden;
Antwort 1 ⇒ Paging-Regel; Antwort 3 ⇒ Filtername; Antwort 4 ⇒ Scope). Aktuell steht der
Vor-Antwort-Zustand — Verwechslungsgefahr im Dev-Lauf.

NACHBESSERUNG NOETIG: Scope nach Antwort 4 klaeren (flache Liste jetzt vs. Baum spaeter, B-1),
Access-Filter benennen (B-2), Zyklen-/Tiefenschutz spezifizieren (B-3) und die Paging-/Auto-Expand-
Mechanik ausarbeiten oder den Baum-Anteil abspalten (B-4). Die Architektur (eigene Tabelle,
Master-Unabhaengigkeit) traegt — die Luecken sind eine Scope- und drei Design-Entscheidungen.

### Nachbesserung (2026-08-06)

**Ruecknahme der vorherigen (fehlerhaften) Ueberarbeitung.** Eine vorangegangene Runde hatte diesen
Abschnitt so geschrieben, dass der Spec-Rumpf auf „flache, paginierte Liste zuerst, Baum als
spaeterer Folge-Schritt" umgestellt wurde. Das war falsch: Es behandelte die **urspruengliche**
Freigabe-Antwort 4 als massgeblich und ignorierte, dass die **B-1-„ANTWORT" innerhalb dieser
Kritischen Pruefung selbst** — datiert auf denselben Tag, spezifisch zu genau diesem Konflikt
formuliert und woertlich „**Kein Descoping — der rekursive Baum bleibt IN SCOPE** ... Die fruehere
Freigabe-Antwort 4 ... ist hiermit **ueberholt und nicht mehr massgeblich**" — die spaetere und
spezifischere Entscheidung ist. Diese Ueberarbeitung nimmt die flache Richtung vollstaendig zurueck
und schreibt den Spec-Rumpf erneut, diesmal korrekt auf Basis der B-1-„ANTWORT". Die vormals als
„offene Rueckfrage 2" gefuehrte Kollision zwischen Freigabe-Antwort 4 und der B-1-„ANTWORT" gilt
damit als **entschieden** (B-1-„ANTWORT" hat Vorrang) und wurde aus den Offenen Rueckfragen
entfernt.

**B-1 (Scope) — jetzt korrekt umgesetzt.** Ziel/Nutzen, Umfang, Fachliche Anforderungen, Technischer
Loesungsentwurf, Akzeptanzkriterien, Test-Szenarien und Offene Rueckfragen beschreiben durchgehend
die **rekursive, mehrstufige Baumdarstellung** als In-Scope dieser Runde. Kein Split in Teil 2a/2b.
Die AK-1-Paging-Frage ist über B-4 geloest: Seiteneinheit ist die **Struktur**, nicht die Zeile —
eine Struktur wird nie über Seiten getrennt und immer vollstaendig dargestellt; `TotalCount` zaehlt
Strukturen. Damit ist auch der scheinbare Widerspruch zwischen der alten AK 1 („alle Zeilen") und
Freigabe-Antwort 1 („bei flacher Liste 25") aufgeloest, ohne dass es noch einen flachen Modus gaebe:
Antwort 1s erster Halbsatz („bei hierarchischer Darstellung alle zur Hierarchie gehoerenden
Auftraege anzeigen") ist die tatsaechlich einschlaegige Vorgabe fuer diese Runde.

**B-2 (Access-Filter) — unveraendert behoben.** Realer Filtername aus dem Code verifiziert:
`[RequirePickingOrTrackingOrLeitstandAccess]` (Class-Level-Attribut auf `ProductionOrdersController`,
Datei `IdealAkeWms/Controllers/ProductionOrdersController.cs`, auch in
`secondbrain/codebase/controller.md` dokumentiert). Der neue `FaHierarchyController` erhaelt in
Fachliche Anforderungen/Technischer Loesungsentwurf denselben Filter. Kein Fund zu melden — der
Referenz-Controller hat einen Class-Level-Filter.

**B-3 (Zyklenschutz) — jetzt als tatsaechliche In-Scope-Anforderung uebernommen, mit einer neuen,
ehrlich benannten Luecke.** Weil der Baum in dieser Runde wirklich gebaut wird (nicht mehr
verschoben), greift der Zyklenschutz jetzt **technisch**, nicht mehr nur als Vormerkung fuer
spaeter: Visited-Set + Tiefen-Cap (Default 500) sind in „Fachliche Anforderungen“ und „Technischer
Loesungsentwurf" konkret als Bestandteil von `FaHierarchyTreeBuilder` beschrieben, inkl.
Abbruchverhalten je Struktur (nicht ganze Seite) und Fehlerkennzeichnung statt leerer Darstellung.
**Neu erkannt bei dieser Korrektur:** Die von der B-3-„ANTWORT" geforderte Fehlermail
(`ISyncErrorNotifier`) und der SyncLog-Eintrag sind Mechanismen, die im echten Code ausschliesslich
im Windows-Service-Projekt existieren (`ISyncErrorNotifier` im Namespace
`IDEALAKEWMSService.Services`; `ISyncLogger`/`SyncLog` sind auf periodische, isolierte Sync-Laeufe
zugeschnitten, ADR 0010) — der Tiefen-Cap-/Zyklen-Abbruch passiert hier aber **zur Web-Request-Zeit**
beim Rendern einer Seite. Das ist eine echte Integrationsluecke, die die urspruengliche B-3-„ANTWORT"
nicht adressiert (sie wurde offenbar im Kontext eines Sync-Laufs formuliert). Diese Luecke wird
**nicht** stillschweigend geloest, sondern als neue offene Rueckfragen 2 und 3 in die Spec
aufgenommen (Web-seitiger Mail-Mechanismus vs. Vorverlagerung der Pruefung in
`FaHierarchySyncService` vs. reines Serilog-Log).

**B-4 (Paging-Mechanik) — jetzt korrekt als Baum-Vereinfachung umgesetzt.** Die B-4-„ANTWORT"
(„Seiteneinheit ist die STRUKTUR, nicht die Zeile ... eine Struktur wird immer vollstaendig
dargestellt") ist jetzt direkt im Technischen Loesungsentwurf und in AK 1/2 umgesetzt: Eine Struktur
wird beim Rendern **immer vollstaendig** aufgebaut (bis zum Tiefen-Cap), paginiert wird ausschliesslich
ueber die Anzahl der Strukturen je Seite. **Damit entfaellt Phantom-Header strukturell** — er war nur
noetig, wenn eine Struktur ueber Seiten getrennt werden koennte, und genau das schliesst die
B-4-Regel aus. Das deckt sich mit der inzwischen getroffenen Querschnitts-Entscheidung der Uebersicht
(„Seiteneinheit ist durchgaengig die Gruppe (`HauptFA`), nicht die Zeile ... Damit entfallen
Phantom-Header und Zeilenzahl-Schaetzung im gesamten Paket", siehe
[[2026-07-29-standort-ideal-uebersicht]]). Auto-Expand bleibt demgegenueber ein **echtes**, von der
Paging-Frage unabhaengiges UI-Thema (Sichtbarkeit eingeklappter Aeste bei einem client-seitigen
Filtertreffer **innerhalb** einer bereits vollstaendig geladenen Struktur) und wird konkret anhand
des bestehenden `ReadOnlyBomBuilder`/`RecursiveFilterSearch`-Praezedenzfalls beschrieben; der exakte
Default-Zustand und die Kopplung an das bestehende User-Setting bleiben offene Rueckfrage 4.

**S-1 (ADR-0005-Ausnahme) — jetzt korrekt benannt statt faelschlich verworfen.** Anders als die
zurueckgenommene vorherige Ueberarbeitung behauptete („die Ausnahme ist fuer Teil 2 nicht
einschlaegig"), gilt fuer eine echte Baumdarstellung genau das Gegenteil: Server-Mode-Spaltenfilter
auf Node-Ebene sind mit der Baum-Integritaet nicht vereinbar (ein entfernter Knoten wuerde seine
Kinder verwaisen lassen). Die Spec benennt deshalb jetzt explizit die **dokumentierte
BOM-Tree-Ausnahme** aus ADR 0005: server-seitige Filterung/Pagination nur auf **Struktur-Ebene**
(Filterkarte + Pagination, Standard-ADR-0005-Pattern), node-interne Filterung **client-seitig**
(Hervorhebung/Sichtbarkeit, keine Entfernung aus dem DOM), analog `ReadOnlyBomBuilder`/
`Views/Picking/Bom.cshtml`.

**S-2 (`FaHierarchyOrderInfo` fehlte) — weiterhin behoben.** `IFaHierarchyOrderInfoRepository` ist
Teil des Loesungsentwurfs; Kopfdaten (`Kunde`, `KO_Termin`, `FE_Termin`, `Status`, ...) werden je
Struktur im Kopf angezeigt (AK 6). Die verbleibende Detailfrage — mehrere Kopfzeilen je `HauptFA`
bei Kombigeraeten — bleibt offene Rueckfrage 1.

**S-3 (verwaiste Knoten) — jetzt fuer die Baumdarstellung selbst geloest (nicht mehr nur
„verschoben").** Weil der Baum tatsaechlich ueber eine Wurzel-Traversal (`VaterFA IS NULL` +
rekursiv `SubFA`) aufgebaut wird, ist die Waisen-Frage hier real: Ein Knoten mit kaputter
`VaterFA`-Kette wuerde ohne Gegenmassnahme von keiner Wurzel erreicht und still verschwinden. Die
Spec definiert deshalb jetzt ausdruecklich eine **Waisen-Pseudowurzel**: ein solcher Knoten wird als
eigene, markierte Struktur gerendert und zaehlt als eigene Struktur fuer Pagination/`TotalCount`
(AK 5).

**S-4 (Split empfohlen) — nicht uebernommen; explizit durch B-1 abgelehnt.** Die vorherige
(zurueckgenommene) Ueberarbeitung hatte behauptet, „genau der empfohlene Split ist jetzt der Stand
dieser Spec". Das war falsch: Die B-1-„ANTWORT" lehnt S-4 **ausdruecklich ab** ("S-4 ist damit
abgelehnt"). Diese Korrektur haelt sich daran — es gibt weiterhin nur eine Teil-2-Datei, und sie
beschreibt ausschliesslich die Baumdarstellung, nicht einen flachen Zwischenschritt.

**S-5 (AppSetting-Seed nicht in `affected_code`) — jetzt anders begruendet offen, nicht mehr
„erledigt durch Scope-Aenderung".** Weil der Tiefen-Cap laut B-3 **konfigurierbar** sein muss und die
Uebersicht reine Web-Anzeige-Schalter in `AppSettings` verortet, ist `AppSettingKeys.cs` wieder Teil
von `affected_code` (fuer den Tiefen-Cap-Key). Ob dafuer eine Seed-Zeile noetig ist oder der Code
ohne vorhandene DB-Zeile auf `500` defaultet, ist neu als offene Rueckfrage 5 aufgenommen — anders
als beim alten `ProduktionsauftragBaumAnzeige`-Toggle (der fuer **diese** Spec nicht gebraucht wird,
da es keinen flachen Alternativmodus mehr gibt, siehe H-3 unten).

**H-1 (Master-Unabhaengigkeit) — unveraendert behoben.** Absatz in „Ziel / Nutzen": Teil 2 beruehrt
weder `ProductionOrders` noch den Master `ProduktionsauftragHierarchisch` und ist vor Teil 7
lieferbar (Verweis auf die B5-Linie der Uebersicht).

**H-2 (View-Ordner-Mismatch) — unveraendert behoben.** `affected_code` und Technischer
Loesungsentwurf nennen `Views/FaHierarchy/Index.cshtml` (passend zu `FaHierarchyController`).

**H-3 (Toggle-Namensfamilie) — bleibt fuer diese Spec nicht einschlaegig, aus einem anderen Grund
als zuvor.** Die zurueckgenommene Ueberarbeitung hatte den Toggle verschoben, weil sie einen
zukuenftigen zweiten (flachen) Anzeige-Modus vorsah. Diese Korrektur verschiebt ihn aus einem
anderen Grund: `FaHierarchyController` liefert in dieser Spec **ausschliesslich** die Baumansicht,
es gibt keinen Modus, zwischen dem `ProduktionsauftragBaumAnzeige` umschalten wuerde. Sollte dieser
Toggle spaeter gebraucht werden (z. B. wenn `ProductionOrders` nach Teil 7 selbst wahlweise
hierarchisch angezeigt werden soll), ist das ein eigener, spaeterer Backlog-Punkt — Heimat-Frage
dafuer ist auf Uebersichts-Ebene bereits geklaert (`AppSettings`, ADR 0011).

**H-4 (Spec-Rumpf an Freigabe-Antworten/B-1..B-4 angleichen) — mit dieser Korrektur erneut
hergestellt.** Diese Ueberarbeitung schreibt Ziel/Nutzen, Umfang, Fachliche Anforderungen,
Technischer Loesungsentwurf, Akzeptanzkriterien, Test-Szenarien, Deploy und Offene Rueckfragen
durchgehend so, dass sie widerspruchsfrei zur B-1-„ANTWORT" (Baum in Scope) sind — nicht mehr zur
inzwischen ueberholten urspruenglichen Freigabe-Antwort 4.

**Verbleibend offen (Schranke 1):**
1. Kombigeraet-Kopfdaten-Darstellung in der Strukturkopfzeile (offene Rueckfrage 1).
2. Technische Verdrahtung der Fehlermail bei Tiefen-Cap-/Zyklen-Abbruch — Integrationsluecke
   Web- vs. Service-Projekt, neu erkannt bei dieser Korrektur (offene Rueckfrage 2).
3. Aktivitaets-/SyncLog-Eintrag bei einem Verstoss, der zur Web-Request-Zeit auftritt statt in einem
   periodischen Sync-Lauf (offene Rueckfrage 3).
4. Default-Aufklapp-Zustand und Kopplung des Auto-Expand an `RecursiveFilterSearch` (offene
   Rueckfrage 4).
5. Konfigurations-Heimat des Tiefen-Caps — `AppSettings` mit/ohne Seed-Zeile vs. hartkodierte
   Konstante (offene Rueckfrage 5).

## Kritische Pruefung (2026-08-07)

Zweiter Anwalt-des-Teufels-Durchgang. Gegengelesen: diese Spec **vollstaendig**, die Teil-1-Spec
(Datenquelle + `ISyncErrorNotifier`-Verdrahtung), ADR 0005 (Baum-Ausnahme), 0006 (Rollen), 0010
(Aktivitaets-Protokoll), 0011 (AppSettings). Am **echten Code** verifiziert:
`ProductionOrdersController.cs`, `ISyncErrorNotifier.cs`, `ReadOnlyBomBuilder`/`Bom.cshtml`,
`RecursiveFilterSearch` (User-Setting) sowie ein Grep ueber das **gesamte** Web-Projekt
`IdealAkeWms/` nach jeglicher Mail-Primitive.

**Zuerst das Bestaetigte (kein Nachbesserungsgrund):**
- **B-2/06 (Access-Filter) stimmt am echten Code.** `ProductionOrdersController.cs` traegt auf
  Class-Level tatsaechlich `[RequirePickingOrTrackingOrLeitstandAccess]` (Zeile 12/13). Die
  Uebernahme desselben Filters auf `FaHierarchyController` ist korrekt; Read-only ⇒ kein Edit-Split.
- **Rumpf ist durchgehend „Baum in scope".** Keine flache-Liste-Reste ausserhalb des verbatim
  06er-Abschnitts. Out-of-Scope (Zeile 110-112) schliesst einen flachen Modus explizit aus.
- **AK 2 (Seiteneinheit = Struktur), AK 4 (Cap + Visited-Set, Abbruch nur dieser Struktur,
  „fehlerhaft markiert" statt leer), AK 5 (Waisen-Pseudowurzel)** sind sauber und pruefbar
  formuliert. Der Phantom-Header entfaellt strukturell — korrekt.
- **ADR 0005** listet den BOM-Tree ausdruecklich als Ausnahme; die client-seitige Node-Filterung ist
  damit regelkonform, nicht ein Verstoss. Der `RecursiveFilterSearch`-Praezedenzfall existiert real
  (User-Setting, in `User.cs`/`Bom.cshtml`).

### BLOCKER

**B-1 (07) — Die 5 „neuen" Freigabe-Antworten sind auf der Platte LEER; die Kern-Praemisse dieses
Durchgangs trifft auf den Datei-Ist-Zustand nicht zu.** Der Abschnitt „## Freigabe-Antworten zu den
neuen Rueckfragen (Mensch fuellt aus — Schranke 1)" (Zeilen 306-312) enthaelt fuenf **blanke** `→`
ohne jeden Text. Es gibt in dieser Datei **keine** erweiterten Antworten auf die Rueckfragen 1-5.
Damit sind — entgegen der Annahme, der Mensch habe nachgeliefert — **alle fuenf neuen offenen
Punkte unbeantwortet**: Kombigeraet-Kopfdaten (1), Fehlermail-Verdrahtung (2), SyncLog-at-request
(3), Auto-Expand-Kopplung (4), Tiefen-Cap-Heimat (5). Ein `/dev`-Lauf haette fuer jeden dieser
Punkte keine Entscheidungsgrundlage. Solange dieser Block leer ist, ist die Spec **nicht
freigabereif** — unabhaengig von der Qualitaet des Rumpfes. (Falls der Mensch die Antworten an
anderer Stelle/uncommitted gegeben hat: sie sind in der zu pruefenden Datei nicht vorhanden — das
allein ist der Nachbesserungsgrund.)

**B-2 (07) — Die Fehlermail-Integrationsluecke ist NICHT geloest, sondern nur in eine unbeantwortete
Rueckfrage verschoben — und im Web-Projekt fehlt jede Mail-Primitive.** Verifiziert:
`ISyncErrorNotifier` existiert ausschliesslich im Namespace `IDEALAKEWMSService.Services`
(`ISyncErrorNotifier.cs`, `NotifyAsync(string, Exception, ct)`, „Wirft NIE") und wird nur von
Service-Klassen injiziert. Ein Grep ueber das **gesamte** Web-Projekt `IdealAkeWms/` nach
`IEmailService`/`IEmailSender`/`SmtpClient`/`MailMessage`/`SendMailAsync` liefert **null Treffer** —
das Web-Projekt hat heute gar keinen Mail-Versand. Konsequenzen, die die Spec nicht zieht:
- Die als In-Scope-Anforderung deklarierte „Fehlermail-Pflicht" (Zeile 92/132) ist mit
  vorhandenen Web-Bausteinen **nicht** erfuellbar; jede der drei in Rueckfrage 2 skizzierten
  Optionen ist ein echter Architektur-Eingriff, keine Verdrahtung.
- **Keine der Optionen ist folgenlos fuer bereits geschriebene Nachbar-Specs bzw. die eigene
  Deploy-Deklaration.** Die architektonisch sauberste Option („Zyklen-/Tiefenpruefung in
  `FaHierarchySyncService` vorverlagern") kollidiert damit, dass Teil 1 den Sync **bewusst als
  reinen Zeilen-Import ohne jede Baum-Traversierung** spezifiziert — Vorverlagerung erzwingt dort
  Traversierung **plus** ein persistiertes Fehler-Flag je Struktur (neue Spalte = Migration). Das
  widerspricht direkt der Deploy-Deklaration dieser Spec (`service: false`, `migration: false`,
  Zeilen 28-31/269-272) und wuerde die freigegebene Teil-1-Spec nachtraeglich aendern. Diese
  Wechselwirkung ist in der Spec nirgends benannt.
- **Es gibt fuer die Meldung (SyncLog + Fehlermail) kein einziges Akzeptanzkriterium.** AK 4 endet
  bei „fehlerhaft markiert" (UI). Die Melde-Pflicht steht nur in „Fachliche Anforderungen" mit dem
  Zusatz „technische Verdrahtung … siehe offene Rueckfragen 2/3". Das Test-Szenario „Zyklen-/Tiefen-
  Test" macht den Mechanismus selbst von der Antwort abhaengig („Details je nach Antwort auf offene
  Rueckfrage 2/3"). Damit ist die geforderte Meldung derzeit **weder spezifiziert noch abnehmbar** —
  auch die SyncLog-Frage (ADR 0010 ist auf isolierte periodische Laeufe zugeschnitten, hier tritt
  der Verstoss pro Web-Request auf) ist ungeloest.

Fazit B-2: nur verschoben, nicht geloest. Die 06er-Erkenntnis (Integrationsluecke) besteht
unveraendert fort und ist zusaetzlich durch das komplette Fehlen einer Web-Mail-Primitive
verschaerft.

### SOLLTE

**S-1 (07) — „Tiefen-Cap konfigurierbar" (fest zugesagt) vs. Rueckfrage 5 (koennte hartkodierte
Konstante werden) ist ein offener Selbstwiderspruch.** In-Scope (Zeile 89) und AK 4 (Zeile 228)
nennen den Cap „konfigurierbar (Default 500)"; `affected_code` (Zeile 17) setzt bereits einen
`AppSettingKeys`-Eintrag voraus. Rueckfrage 5 laesst aber ausdruecklich die hartkodierte Konstante
offen — die „nicht ohne Deploy aenderbar" waere und „konfigurierbar" damit **verletzt**. Solange
Rueckfrage 5 offen ist, ist AK 4 nicht deterministisch umsetzbar (und `affected_code` ggf. falsch).

**S-2 (07) — Auto-Expand ist als festes In-Scope-Feature zugesagt, haengt aber an der unbeantworteten
Rueckfrage 4 und hat kein Akzeptanzkriterium.** In-Scope (Zeile 84-88) fuehrt „Auto-Expand des Pfads
zu einem client-seitigen Filtertreffer" als geliefertes Feature. Rueckfrage 4 laesst offen, ob es an
das User-Setting `RecursiveFilterSearch` gekoppelt wird — ist es gekoppelt und der Anwender hat das
Setting **aus**, greift das zugesagte In-Scope-Feature **still nicht**. Zusaetzlich existiert fuer
Expand/Collapse und Auto-Expand **kein** Akzeptanzkriterium (AK 1-8 erwaehnen sie nicht). Entweder
Kopplung entscheiden und ein AK ergaenzen, oder Auto-Expand aus dem festen In-Scope in die von
Rueckfrage 4 abhaengige Menge verschieben.

### HINWEIS

**H-1 (07) — AK 6 ist fuer den Kombigeraet-Fall unterbestimmt (Rueckfrage 1 offen).** AK 6 verlangt
„die zugehoerigen `FaHierarchyOrderInfo`-Daten … fuer ihren `HauptFA`" im Singular, waehrend ein
Kombigeraet laut Teil 1 **mehrere** `FaHierarchyOrderInfo`-Zeilen (mehrere `MontageAbteilung`) je
`HauptFA` traegt. Wie mehrere Kopfzeilen dargestellt werden, ist Rueckfrage 1 — bis zur Antwort ist
AK 6 fuer diesen (real existierenden) Fall nicht abnehmbar.

**H-2 (07) — Deploy-Metadaten sind an Rueckfrage 2/5 gekoppelt und derzeit nur unter der optimistischsten
Annahme korrekt.** `deploy.service: false`/`migration: false` gilt nur, wenn Rueckfrage 2 **nicht**
zur Vorverlagerung in den Sync fuehrt (siehe B-2) und Rueckfrage 5 keine Seed-Zeile verlangt. Beide
Aufloesungen koennen die Deklaration kippen — beim Beantworten mitpruefen.

NACHBESSERUNG NOETIG: Die 5 neuen Freigabe-Antworten fehlen komplett auf der Platte (B-1); die
Fehlermail-/SyncLog-Integrationsluecke ist nur verschoben, nicht geloest, und im Web-Projekt fehlt
jede Mail-Primitive (B-2, inkl. Wechselwirkung mit Teil-1-Spec und den eigenen Deploy-Metadaten);
„konfigurierbarer" Tiefen-Cap widerspricht der noch offenen Konstanten-Option (S-1); Auto-Expand ist
zugesagt, aber an eine offene Frage gekoppelt und ohne AK (S-2).
