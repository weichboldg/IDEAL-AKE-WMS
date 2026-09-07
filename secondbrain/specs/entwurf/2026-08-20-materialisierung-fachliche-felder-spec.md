---
type: spec
title: "IDEAL: Materialisierung um die fachlichen Felder erweitern (K1/K2/K3, Werkbank-Datenhoheit)"
slug: 2026-08-20-materialisierung-fachliche-felder-spec
status: Entwurf
created: 2026-09-07
updated: 2026-09-07
source_backlog: "[[2026-08-20-materialisierung-fachliche-felder]]"
depends_on: "[[2026-08-18-fa-liste-hierarchie-anzeige-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IDEALAKEWMSService/Services/FaMaterializationSyncService.cs (Anlege- UND Update-Pfad gemeinsam um K2-Felder erweitern, F1; Klassenkommentar Zeile 21-22 korrigieren, falls Workplace aus der app-verwalteten Liste faellt)"
  - "IDEALAKEWMSService/Services/FaMaterializationPlanner.cs (MaterializationSourceOrder um die entschiedenen K2-Felder erweitern — reiner Werttyp, unit-testbar, kein DB-Zugriff)"
  - "IdealAkeWms/Models/ProductionOrder.cs (neue K2-Spalten je nach Rueckfrage 3 — aktuell existiert NUR ProductionWorkplaceId aus der K2-Liste, verifiziert am Modell; Beschichtet/Matchcode/Hauptlagerplatz/Artikelgruppe/Artikeltyp/Material/EKBedarf/Fertigungmenge/Masse existieren NICHT)"
  - "IdealAkeWms/Models/ProductionWorkplace.cs + IdealAkeWms/Data/Repositories/ProductionWorkplaceRepository.cs (Lookup/ggf. Anlage per Name-Match gegen Arbeitsbereich, Rueckfrage 1)"
  - "IdealAkeWms/Data/Repositories/FaHierarchyOrderInfoRepository.cs + IFaHierarchyOrderInfoRepository.cs (neue gebuendelte Abfrage GetByHauptFaKeysAsync fuer eine Seite von Gruppen, statt Einzel-Query je HauptFA — vermeidet Fan-out ueber die Repository-Grenze)"
  - "IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs (ProductionOrderListGroup: heute NUR OrderNumber+Items, verifiziert — neue K1-Felder Kunde/Termine/Prio/AB-Nummer/Montage-Abteilung + Mehrdeutig-Flag ergaenzen)"
  - "IdealAkeWms/Controllers/ProductionOrdersController.cs (Gruppen-Kopfzeile mit K1 befuellen; MapItem/Zeilen-Termine im hierarchischen Modus auf das gruppenweite Fertigungstermin-Feld umstellen statt auf die je K1-Regel leer bleibende ProductionOrder.ProductionDate-Spalte der Zeile, siehe Abschnitt 'Termin-Kaskade'; Kunde-Filter zusaetzlich gegen FaHierarchyOrderInfo.Kunde pruefen, siehe Abschnitt 'Kunde-Filter')"
  - "IdealAkeWms/Views/ProductionOrders/Index.cshtml (Gruppen-Kopfzeile Zeile ~124-133 um K1-Felder + Mehrdeutig-Badge erweitern, aktuell nur 'HauptFA {Nr}' + Sub-FA-Zaehler)"
  - "IDEALAKEWMSService/Services/FaHierarchySyncService.cs (Kombigeraete-Mehrfachzeilen je HauptFA erkennen + einmal je Sync-Lauf protokollieren, Rueckfrage 7)"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs (kein neuer Service-Name noetig — FaMaterialization/FaHierarchy bestehen bereits; ggf. neue Counts-Keys)"
  - "SQL/91_<Name>.sql (naechste freie Nummer im Worktree, Stand pruefen; nur falls Rueckfrage 3/6 neue ProductionOrder-Spalten ergibt) + SQL/00_FreshInstall.sql an beiden Stellen"
  - "docs/TESTSZENARIEN.md"
  - "secondbrain/tests/testszenarien-index.md"
open_questions:
  - "Werkbank-Stammdaten fuer unbekannte Arbeitsbereiche: automatisch anlegen oder nur melden (Log+Sammelmail)?"
  - "Bestaetigung: Wird fuer IDEAL-Auftraege aktuell NIRGENDS manuell eine Werkbank zugewiesen? (Vorbedingung fuer Variante B/Sage-fuehrend) + Klassenkommentar-Korrektur"
  - "Welche K2-Felder ausser Werkbank/Beschichtet sollen materialisiert werden — nur angezeigte Spalten oder alle verfuegbaren als Vorrat? (jedes neue Feld = neue ProductionOrder-Spalte + Migration)"
  - "K1 ausschliesslich in der Gruppen-Kopfzeile, oder zusaetzlich je Zeile (Export/Spaltenfilter)?"
  - "Exaktes Feld-Mapping FaHierarchyOrderInfo -> Anzeige-Spalten: Neuer_PT_PPS oder FE_Termin fuer 'Fert.-Termin'? Verladetermin_Vsl fuer 'Liefertermin'?"
  - "Verhaeltnis FaHierarchyNode.Beschichtet (K2) zur bestehenden HasCoatingParts/LackierteilKategorieName-Logik (BOM-Kategorie, bereits fuer AKE aktiv) — ersetzen, ergaenzen oder unabhaengig nebeneinander?"
  - "Kombigeraete-Mehrfachzeilen (1:n FaHierarchyOrderInfo je HauptFA): Log-Eintrag im FaHierarchySyncService (Sync-Zeit, einmal je Lauf) oder bei jedem Seitenaufruf der FA-Liste?"
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

## Ziel / Nutzen (das Warum)

Seit Teil 7 ([[2026-07-29-standort-ideal-teil-7-spec]]) werden IDEAL-Sub-FAs als echte
`ProductionOrder`-Zeilen materialisiert — technisch funktioniert das, aber `FaMaterializationSyncService`
schreibt beim Anlegen nur **sieben** Felder (`OrderNumber`, `SubOrderNumber`, `ParentSubOrderNumber`,
`Quantity`, `ArticleNumber`, `Description1/2`) und beim Update sogar nur **vier**. Das war beim
Zuschnitt von Teil 7 richtig — Ziel war BDE-Faehigkeit, dafuer genuegen Identitaet und Menge — ist
aber ueberholt, seit [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] dieselben Auftraege in FA-Liste,
Leitstand, Arbeitsvorrat, Tracking und Picking sichtbar macht: Kunde, Werkbank, Beschichtet, BG-Termin,
Fert.-Termin, Liefertermin und Komm. bleiben leer, obwohl die Werte in der Struktur- bzw.
Auftrags-Cache-Tabelle bereits vorhanden sind (`FaHierarchyNode`, `FaHierarchyOrderInfo`, Teil 1).

Diese Spec schliesst genau diese Luecke — **nicht** durch pauschales "alles befuellen", sondern durch
eine saubere Drei-Klassen-Trennung (K1/K2/K3), weil ein Teil der leeren Spalten fachlich **nicht** in
die Sub-FA-Zeile gehoert (K1, Auftragsdaten je HauptFA), ein anderer Teil bereits app-verwaltet und
damit potenziell mit einer neuen Sage-Quelle kollisionsgefaehrdet ist (Werkbank), und ein dritter Teil
zu Recht leer bleibt (K3, WMS-eigene Felder).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:**

1. K1 — Gruppen-Kopfzeile der FA-Liste (und ggf. weiterer Ansichten aus der Anzeige-Spec) mit
   Auftragsdaten aus `FaHierarchyOrderInfo` befuellen (Kunde, Termine, Prio, AB-Nummer,
   Montage-Abteilung), inkl. der 1:n-Regel bei Kombinationsgeraeten (alle Kopfvarianten im Klartext +
   Mehrdeutig-Badge, kein stilles Auswaehlen).
2. K2 — `FaMaterializationSyncService` (Anlege- **und** Update-Pfad) um die entschiedenen Zeilenfelder
   aus `FaHierarchyNode` erweitern, Feld-fuer-Feld, kein `SetValues`.
3. Die Werkbank-Datenhoheits-Entscheidung (Variante A/B/C) inkl. der beiden IDEAL-spezifischen
   Unter-Entscheidungen (Stammdaten automatisch anlegen vs. melden; Klassenkommentar-Korrektur, falls
   `Workplace` die app-verwaltete Liste verlaesst).
4. K3 — Feststellung, dass die WMS-eigenen Felder (Komm.-Status, Lack-T) zu Recht leer bleiben; keine
   Code-Aenderung dafuer.
5. Der bestehende Kunde-Filter in `ProductionOrdersController` im hierarchischen Modus so erweitern,
   dass er trotz leerer `ProductionOrder.Customer`-Spalte (K1-Regel) weiterhin Treffer liefert.
6. Testszenarien fuer alle drei Klassen inkl. AKE-Flachmodus-Regression (bit-identisch).

**Out-of-Scope:**

- Jede Aenderung an der Gruppierungs-/Anzeige-Mechanik selbst (Chevron, Paginierung ueber Gruppen,
  Kaskaden-Fertigmeldung) — das ist [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] und bleibt
  unveraendert; diese Spec liefert nur die **Daten**, die dort in bereits vorgesehene, aber leere
  Strukturen (`ProductionOrderListGroup`, Gruppen-Kopfzeile-Markup) einlaufen.
- Kombinationsgeraete als Gruppen-Schluessel-Erweiterung auf `ProductionOrders` — laut Teil-7-Freigabe-
  Antwort 1 paketweit out of scope; diese Spec zeigt Kombigeraete nur lesend/mehrdeutig markiert an.
- BDE-Cockpit (Kartengruppierung) — eigener Backlog-Eintrag laut Anzeige-Spec, hier nicht behandelt.
- AKE — der gesamte Materialisierungspfad laeuft ausschliesslich bei Master
  `ProduktionsauftragHierarchisch = true`; bei `false` aendert sich nichts (F4).

## Fachliche Anforderungen

### K1 — Auftragsdaten aus `FaHierarchyOrderInfo` (je HauptFA, Gruppen-Kopfzeile)

**Felder:** Kunde, BG-Termin/Fert.-Termin/Liefertermin (Termin-Kaskade siehe unten), Prio, AB-Nummer
(`ABNr`), Montage-Abteilung.

- Diese Werte haengen am **HauptFA**, nicht am einzelnen Sub-FA. Sie werden **nicht** nach
  `ProductionOrders` materialisiert (keine neue Spalte, keine Migration fuer K1) — die Gruppen-
  Kopfzeile liest sie direkt aus `FaHierarchyOrderInfo`, ueber eine **gebuendelte** Abfrage je
  angezeigter Seite von HauptFA-Gruppen (`GetByHauptFaKeysAsync`, neu), nicht per Einzel-Query je
  Gruppe (Fan-out-Vermeidung, paketweite Regel; `IFaHierarchyOrderInfoRepository.GetByHauptFaAsync`
  existiert bereits fuer den Einzelfall aus `/FaHierarchy`, wird hier um eine mengenwertige Variante
  ergaenzt, analog dem `GetAllByFaAndOperationAsync`-Vorbild aus Teil 7).
- `ProductionOrderListGroup` (aktuell **nur** `OrderNumber` + `Items`, verifiziert am Code) bekommt die
  K1-Felder als neue Properties.
- **1:n-Regel (Kombinationsgeraete):** Liefert `FaHierarchyOrderInfo` mehr als eine Zeile fuer ein
  `HauptFA` (verschiedene `MontageAbteilung`), zeigt die Gruppen-Kopfzeile **alle** Kopfvarianten im
  Klartext (z. B. je Zeile: Montage-Abteilung, Kunde, Termine) **plus** einen „mehrdeutig"-Badge —
  **nicht** stillschweigend eine Zeile auswaehlen. Ein Log-Eintrag markiert den Fall (Ort siehe
  Rueckfrage 7).

**Termin-Kaskade — kritischer, am Code verifizierter Befund:** Die heutigen Spalten „BG-Termin",
„Komm." und „Beschicht." in `Views/ProductionOrders/Index.cshtml` sind **keine** Rohfelder, sondern
werden in `ProductionOrdersController.MapItem` aus `o.ProductionDate` (**Fert.-Termin**) rueckwaerts
berechnet — `Komm.` = Fert.-Termin minus `KommissionierTage`, `BG-Termin` = Komm. minus
`VorkommissionierTage` (**werkbank-spezifisch ueberschreibbar**, `ProductionWorkplace.
OverridePrePickingDays`), `Beschicht." = BG-Termin minus `BeschichtungTage`. Nach der K1-Regel bleibt
`ProductionOrder.ProductionDate` je Sub-FA-Zeile **leer** (nicht materialisiert) — ohne Anpassung
wuerden `Komm.`/`BG-Termin`/`Beschicht.` fuer **alle** IDEAL-Zeilen leer bleiben, obwohl das
zugrunde liegende Fertigungstermin-Datum in `FaHierarchyOrderInfo` fuer die ganze Gruppe bekannt ist.

**Loesung dieser Spec:** Im hierarchischen Modus liest `MapItem` das gruppenweite Fertigungstermin-
Feld aus `FaHierarchyOrderInfo` (Mapping siehe Rueckfrage 5) statt `o.ProductionDate` der Zeile, wendet
aber weiterhin die **zeilen-eigene** Werkbank-Override (`OverridePrePickingDays`, K2) auf die
Rueckwaerts-Berechnung an. Ergebnis: Zwei Sub-FAs derselben Gruppe an unterschiedlichen Werkbaenken
zeigen denselben Fert.-Termin/Liefertermin (K1, aus der Kopfzeile), aber ggf. unterschiedliche
BG-Termine (weil `VorkommissionierTage` je Werkbank abweicht) — das ist **kein** Fehler, sondern
korrekt abgeleitet aus K1 (gemeinsam) + K2 (Werkbank-spezifisch). `ProductionOrder.ProductionDate`/
`DeliveryDate` bleiben dabei auf der DB-Zeile weiterhin `NULL` (K1-Regel bleibt gewahrt) — die
Anreicherung passiert ausschliesslich im Anzeige-ViewModel, nicht in der DB.

**Kunde-Filter:** `ProductionOrdersController` hat bereits einen bestehenden Freitextfilter
`filterCustomer`, der gegen `ProductionOrder.Customer` sucht. Nach K1 bleibt diese Spalte fuer
IDEAL-Sub-FAs leer — der Filter liefert im hierarchischen Modus sonst **keine** Treffer mehr. Diese
Spec erweitert den Filter im hierarchischen Modus so, dass er zusaetzlich gegen `FaHierarchyOrderInfo.
Kunde` (Join ueber `HauptFA = OrderNumber`) sucht. AKE (Master `false`) bleibt unveraendert
(nur `ProductionOrder.Customer`).

### K2 — Zeilendaten aus `FaHierarchyNode` (je Sub-FA), in die Materialisierung aufnehmen

**Gesichert im Scope (aus dem Befund):** `Arbeitsbereich` -> `ProductionOrder.ProductionWorkplaceId`
(Werkbank-Zuordnung, siehe eigener Abschnitt unten), `Beschichtet` -> neue Spalte auf `ProductionOrder`
(Name TBD im Dev-Lauf, z. B. `IsCoated`), sofern Rueckfrage 6 nicht anders entscheidet.

**Zu pruefen (Rueckfrage 3):** `Matchcode`, `Hauptlagerplatz`, `Artikelgruppe`, `Artikeltyp`,
`Material`, `EKBedarf`, `Fertigungmenge`, Masse (`Breite`/`Hoehe`/`Tiefe`). **Keines** dieser Felder
existiert heute auf `ProductionOrder` (verifiziert am Modell) — jedes zusaetzlich aufgenommene Feld
bedeutet eine neue Spalte **und** eine Migration. Diese Spec nimmt daher **keine** Vorfestlegung vor,
welche der geprueften Felder tatsaechlich uebernommen werden — das haengt an Rueckfrage 3 und daran,
welche Spalten eine der sechs betroffenen Ansichten (Anzeige-Spec) tatsaechlich anzeigt.

**F1 — Update-Pfad muss mitwachsen (PFLICHT).** Jedes neu aufgenommene K2-Feld wird **in derselben
Aenderung** sowohl im Anlege- als auch im Update-Zweig von `FaMaterializationSyncService.RunAsync`
gesetzt (heute: Anlegen 7 Felder, Update nur 4 — dieser Bestandsunterschied ist die Ursache des
gesamten Befunds und darf sich nicht wiederholen).

**F2 — Bestandsauftraege erst nach einem Sync-Lauf gefuellt.** Die bereits materialisierten 130
Sub-FAs bekommen die neuen K2-Felder **nicht** rueckwirkend durch die Migration, sondern erst durch
den naechsten reguraeren Materialisierungs-Lauf (Update-Zweig). Nach dem Deploy einen Lauf abwarten
und die Anzeige gegenpruefen — nicht annehmen, dass sie sofort korrekt ist.

**F3 — Z1 gilt unveraendert.** Kein `SetValues`, kein Entitaets-Ersatz — jedes Feld wird **einzeln**
in `FaMaterializationSyncService` gesetzt (Muster: bestehende `Quantity`/`ArticleNumber`/
`Description1/2`-Zeilen im Anlege- und Update-Block). Die app-verwaltete Liste
(`IsDone`/`PickingStatus`/`BdeStatus`/`Storno`/`ExtraInfo`, aktuell **inklusive** `Workplace`) bleibt
tabu, **ausser** fuer das Feld, das die Werkbank-Entscheidung unten ausdruecklich davon ausnimmt.

**F4 — AKE unveraendert.** Der gesamte Materialisierungspfad laeuft nur bei Master `true`; bei
`false` aendert sich nichts an `ProductionOrders`, auch nicht an neu hinzugefuegten Spalten (bleiben
fuer AKE-Zeilen `NULL`, kein Befuellungsversuch).

### K3 — WMS-eigene Felder, zu Recht leer

Komm.-Status (`PickingStatus`/`IsDonePicking`) und Lack-T (`HasCoatingParts`/`IsCoatingDone`, siehe
auch Rueckfrage 6) entstehen erst durch Arbeit im WMS selbst. **Kein Handlungsbedarf**, keine
Code-Aenderung — diese Spec haelt das nur fest, damit niemand versehentlich versucht, sie ebenfalls
aus Sage zu befuellen.

### Werkbank-Datenhoheit (K2-Sonderfall, Schranke-1-relevant)

Der Klassenkommentar von `FaMaterializationSyncService` (Zeile 21-22) fuehrt `Workplace` ausdruecklich
unter den bei Updates **nie ueberschriebenen** Feldern (mit `IsDone`, `PickingStatus`, `BdeStatus`,
`Storno`, `ExtraInfo`). Soll die Werkbank kuenftig automatisch aus `Arbeitsbereich` kommen, kollidiert
das direkt mit dieser bewussten Z1-Regel. Drei Varianten, **nicht** vom Spec-Agent entschieden:

| Variante | Bedeutung | Bewertung |
|---|---|---|
| **A — nur beim Anlegen** | Sage setzt den Startwert; danach gehoert das Feld dem WMS, eine spaetere Sage-Aenderung kommt nicht mehr an. | Sicher gegen stillen Datenverlust, aber Sage-Aenderungen bleiben unsichtbar. |
| **B — bei jedem Lauf (Sage fuehrend)** | Eine manuelle WMS-Zuweisung wird beim naechsten Sync-Lauf **still** ueberschrieben. | Bricht Z1 — genau die Klasse stiller Datenverluste, gegen die das Paket abgesichert wurde, **ausser** eine Meldung bei Abweichung macht den Uberschreibvorgang sichtbar (siehe unten). |
| **C — eigenes Feld** | Sage-Arbeitsbereich getrennt von der WMS-Werkbank; beide sichtbar, keine Kollision. | Sauberste Trennung, aber zwei Felder mit aehnlicher Bedeutung in der UI. |

**Empfehlung dieser Spec: A oder C.** Variante B ist nur vertretbar, wenn zusaetzlich (a) niemand fuer
IDEAL-Auftraege manuell eine Werkbank zuweist (Rueckfrage 2) **und** (b) jede Ueberschreibung eines
abweichenden Werts gemeldet wird (Log-Eintrag, analog dem bestehenden
`SendMissingDigestAsync`-Muster) — so faellt sofort auf, wenn doch jemand manuell zuweist.

**IDEAL-Teilentscheidung (2026-08-20, aus dem Backlog uebernommen):** Die Werkbank **soll**
automatisch aus `Arbeitsbereich` kommen. Damit zerfaellt die Entscheidung in zwei konkrete, noch
offene Teile:

**(a) Werkbank-Stammdaten (Rueckfrage 1).** Existieren `K-02`, `S-01`, `H4-04` etc. bereits als
`ProductionWorkplace`-Zeilen? Zwei Wege:
- **Automatisch anlegen** — der Sync legt fehlende Werkbaenke selbst an (Name-Match gegen
  `ProductionWorkplace.Name`). Risiko: uebernimmt unkontrolliert Tippfehler/Altlasten aus Sage in
  die Werkbank-Stammdaten.
- **Nur melden** (empfohlen bei ueberschaubarer, stabiler Zahl von Arbeitsbereichen) — der Sync
  **meldet** unbekannte Arbeitsbereiche per Log + Sammelmail (analog dem bestehenden
  `SendMissingDigestAsync`-Muster fuer vermisste FAs), ein Mensch pflegt sie einmal manuell an.

**(b) Datenhoheit bei Updates = Variante B (aus dem Backlog uebernommen).** Sage ist fuehrend; eine
manuelle Werkbank-Zuweisung an einem IDEAL-Auftrag wird beim naechsten Sync-Lauf (max. 15 Minuten)
**ueberschrieben**. Voraussetzung (Rueckfrage 2): Fuer IDEAL-Auftraege weist aktuell niemand manuell
zu. Absicherungen, die diese Spec **verbindlich** macht:
- `Workplace` faellt aus der app-verwalteten Liste des `FaMaterializationSyncService` heraus — der
  Klassenkommentar (Zeile 21-22) wird entsprechend korrigiert, sonst widersprechen sich Kommentar und
  Verhalten (F3-Verletzung durch Unterlassung).
- Der Sync **meldet** (Log-Eintrag, eigener Counts-Key im bestehenden `FaMaterialization`-SyncLog),
  wenn er bei einem bereits gesetzten `ProductionWorkplaceId` einen abweichenden Wert schreibt — so
  wird eine versehentliche manuelle Zuweisung sichtbar, statt spurlos zu verschwinden.

## Technischer Loesungsentwurf

- **`FaMaterializationPlanner`** (rein, unit-testbar, kein DB-Zugriff): `MaterializationSourceOrder`
  um die entschiedenen K2-Felder erweitern (Werttyp-Record, Muster wie heute).
- **`FaMaterializationSyncService.RunAsync`**: im Anlege-Block (heute Zeilen ~117-133) **und** im
  Update-Block (heute Zeilen ~136-157) dieselben neuen Felder ergaenzen — Werkbank-Lookup
  (`ProductionWorkplaceRepository`, Name-Match gegen `Arbeitsbereich`, ggf. Anlage oder Meldung je
  Rueckfrage 1) sitzt in diesem Service, nicht im reinen Planner (DB-Zugriff).
- **`FaHierarchyOrderInfoRepository`**: neue `GetByHauptFaKeysAsync(IEnumerable<int> hauptFaKeys)` —
  eine Abfrage je angezeigter Seite von Gruppen, `WHERE HauptFA IN (...)`, kein Join in die
  Sub-FA-Zeilenabfrage hinein (Fan-out-Vermeidung).
- **`ProductionOrdersController.Index`**: nach dem Laden der Gruppen (`GetForLeitstandGroupedAsync`,
  bereits vorhanden aus der Anzeige-Spec) die HauptFA-Schluessel sammeln, `GetByHauptFaKeysAsync`
  aufrufen, je Gruppe die K1-Felder in `ProductionOrderListGroup` setzen (inkl. Mehrdeutig-Flag bei
  >1 Zeile je HauptFA); `MapItem` bekommt das gruppenweite Fertigungstermin-Datum als Parameter statt
  `o.ProductionDate` zu verwenden, wenn hierarchisch (siehe Termin-Kaskade oben).
- **`ProductionOrderListGroup`** (VM): neue Properties `Customer`, `ProductionDate`, `DeliveryDate`,
  `Prio`, `AbNummer`, `MontageAbteilung`, `IsAmbiguous` (bzw. eine Liste von Kopfvarianten bei
  Mehrdeutigkeit — exakte Form Dev-Lauf-Entscheidung).
- **`FaHierarchySyncService`**: nach dem Full-Refresh pruefen, ob ein `HauptFA` mehr als eine
  `FaHierarchyOrderInfo`-Zeile hat; falls ja, Log-Eintrag mit `HauptFA` + Anzahl (einmal je Sync-Lauf,
  nicht je Seitenaufruf — siehe Rueckfrage 7).
- **`Views/ProductionOrders/Index.cshtml`**: Gruppen-Kopfzeile (aktuell nur „HauptFA {Nr}" +
  Sub-FA-Zaehler) um Kunde/Termine/Prio/AB-Nummer/Montage-Abteilung sowie den Mehrdeutig-Badge
  erweitern; bei Master `false` unveraendert (kein Gruppen-Rendering, F4).

## Migrations-/SQL-Auswirkungen

**K1 — keine Migration.** Alle K1-Felder werden ausschliesslich lesend aus der bereits vorhandenen
Cache-Tabelle `FaHierarchyOrderInfo` angezeigt, nicht materialisiert.

**K2 — mindestens eine Migration, Umfang haengt an Rueckfrage 3/6.** Sicher betroffen:
- **Beschichtet** (sofern nicht durch Rueckfrage 6 anders geloest): neue Spalte auf `ProductionOrder`,
  `bit NOT NULL DEFAULT 0` oder nullable `bool?` je nach Semantik-Entscheidung im Dev-Lauf.
- **Werkbank:** `ProductionOrder.ProductionWorkplaceId` existiert bereits (Teil 6/vorher) — **keine**
  neue Spalte, nur eine neue Schreiblogik im Sync. Falls Rueckfrage 1 „automatisch anlegen" ergibt,
  betrifft das nur neue `ProductionWorkplace`-Zeilen (bestehende Tabelle, kein Schema-Impact).
- **Weitere K2-Felder** (Matchcode, Hauptlagerplatz, Artikelgruppe, Artikeltyp, Material, EKBedarf,
  Fertigungmenge, Masse): **falls** Rueckfrage 3 sie in den Scope aufnimmt, je Feld eine neue Spalte.

Migrationsdisziplin wie in jedem Teil: Model -> `dotnet ef migrations add <Name>` -> idempotentes
`SQL/91_<Name>.sql` (naechste freie Nummer im Worktree, vor dem Dev-Lauf erneut pruefen) mit
`OBJECT_ID`/`COL_LENGTH`-Guards, additive Spalten (nullable oder mit Default, **nicht**
daten-destruktiv) -> `SQL/00_FreshInstall.sql` an **beiden** Stellen (Schema + `MigrationId`).
Kein Backfill-Bedarf ueber die naechste Materialisierungs-Runde hinaus (F2) — bestehende Zeilen
bekommen die Werte durch den naechsten Update-Lauf, nicht durch die Migration selbst.

## Audit-Feld-Auswirkungen

`ProductionOrder` bleibt `AuditableEntity`. Jede vom Sync geschriebene Zeile (Anlegen **und** Update)
setzt weiterhin `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` auf den Service-Namen
(`IDEALAKEWMSService`, bestehendes Muster) — unveraendert durch diese Spec, nur die Anzahl der
gesetzten Fachfelder waechst. Neue Melde-Log-Eintraege (Werkbank-Ueberschreibung, unbekannter
Arbeitsbereich, Kombigeraet-Mehrfachzeile) laufen ueber das bestehende Aktivitaets-Protokoll
(`ISyncLogger`, Service `FaMaterialization` bzw. `FaHierarchy`), kein neues Audit-Feld auf der
Entitaet selbst.

## Betroffene Rollen / Zugriffsfilter

Keine Aenderung. Die betroffenen Ansichten behalten ihre bestehenden `RequireXxxAccess`-Filter aus
[[2026-08-18-fa-liste-hierarchie-anzeige-spec]] unveraendert — diese Spec liefert nur zusaetzliche
Daten in bereits zugriffsgeschuetzte Ansichten hinein, keine neue Seite, keine neue Aktion.

## Listen-View-Pattern-Pflichten (ADR 0005)

Keine neue Tabellen-Ansicht. Die K1-Kopfzeilenfelder sind **nicht** eigenstaendig sortier-/
spaltenfilterbar (sie stehen in der Gruppen-Kopfzeile, nicht in einer regulaeren `<th data-col-key>`
-Spalte) — falls Rueckfrage 4 ergibt, dass K1 zusaetzlich je Zeile erscheinen soll, gelten dafuer die
vollen ADR-0005-Pflichten (eigener `data-col-key`, Server-Spaltenfilter, `ColumnDefinitions.cs`-Eintrag)
wie fuer jede andere Spalte. Die neuen K2-Spalten auf `ProductionOrder` (Beschichtet etc.), sofern sie
in einer der sechs Listen angezeigt werden sollen, bekommen jeweils einen eigenen `ColumnDef`-Eintrag
analog zum bereits etablierten `parent-sub-order-number`-Muster aus der Anzeige-Spec.

## Akzeptanzkriterien

1. **K1 in der Kopfzeile.** Bei Master `true` zeigt die Gruppen-Kopfzeile der FA-Liste je HauptFA
   Kunde, Fert.-Termin, Liefertermin, Prio, AB-Nummer und Montage-Abteilung aus `FaHierarchyOrderInfo`
   — ohne dass diese Werte auf den einzelnen Sub-FA-Zeilen dupliziert oder in `ProductionOrders`
   materialisiert werden.
2. **Kombigeraet-Mehrdeutigkeit.** Liefert `FaHierarchyOrderInfo` fuer ein `HauptFA` mehr als eine
   Zeile, zeigt die Gruppen-Kopfzeile **alle** Kopfvarianten im Klartext plus einen „mehrdeutig"-Badge;
   es wird **nicht** stillschweigend eine Variante ausgewaehlt.
3. **Termin-Kaskade korrekt.** Zwei Sub-FAs derselben Gruppe an unterschiedlichen Werkbaenken mit
   unterschiedlicher `OverridePrePickingDays` zeigen denselben Fert.-Termin/Liefertermin (aus der
   Kopfzeile), aber ggf. unterschiedliche BG-Termine — `ProductionOrder.ProductionDate`/`DeliveryDate`
   bleiben dabei auf der DB-Zeile `NULL`.
4. **Kunde-Filter funktioniert weiterhin.** Eine Freitextsuche nach einem Kundennamen liefert im
   hierarchischen Modus weiterhin Treffer, obwohl `ProductionOrder.Customer` je Sub-FA-Zeile leer ist
   (Suche zusaetzlich gegen `FaHierarchyOrderInfo.Kunde`).
5. **F1 — Update-Pfad vollstaendig.** Jedes neu materialisierte K2-Feld wird sowohl im Anlege- als
   auch im Update-Zweig von `FaMaterializationSyncService` gesetzt — nachgewiesen durch einen Unit-
   Test, der ein bereits existierendes `ProductionOrder` mit geaenderten Quellwerten durch den
   Update-Zweig laufen laesst und alle neuen Felder aktualisiert vorfindet.
6. **F2 — Bestandsauftraege werden nachgezogen.** Nach einem Materialisierungs-Lauf gegen die
   bestehenden 130 IDEAL-Sub-FAs zeigen zuvor leere K2-Spalten (z. B. Werkbank) befuellte Werte, ohne
   dass ein manueller Eingriff noetig war.
7. **F3 — kein SetValues, kein Entitaets-Ersatz.** Code-Review bestaetigt, dass jedes neue Feld
   einzeln zugewiesen wird (kein `SetValues`/kein `_ctx.Update(entity)`-Muster).
8. **F4 — AKE unveraendert.** Bei Master `false` ist `ProductionOrders` byte-identisch zum
   Vor-Zustand dieser Spec; keine neue Spalte wird fuer AKE-Zeilen befuellt.
9. **Werkbank-Entscheidung nachweisbar umgesetzt** gemaess der Schranke-1-Antwort (A/B/C) — bei
   Variante B zusaetzlich: eine simulierte manuelle Werkbank-Zuweisung wird beim naechsten Sync-Lauf
   ueberschrieben **und** dabei protokolliert (Log-Eintrag mit altem/neuem Wert); der Klassenkommentar
   in `FaMaterializationSyncService` nennt `Workplace` nicht mehr unter den app-verwalteten Feldern.
10. **Unbekannter Arbeitsbereich behandelt gemaess Rueckfrage-1-Antwort** — entweder wird eine neue
    `ProductionWorkplace`-Zeile automatisch angelegt (nachweisbar per Name-Match), oder der Sync meldet
    den unbekannten Arbeitsbereich per Log + Sammelmail, ohne eine Werkbank zuzuweisen (kein Absturz,
    kein stiller Datenverlust in beiden Faellen).
11. **K3 unveraendert.** Komm.-Status und Lack-T bleiben durch diese Spec unangetastet — Regressionstest
    zeigt keine Verhaltensaenderung an `PickingStatus`/`BdeStatus`/`HasCoatingParts`.

## Test-Szenarien

Neues Kapitel „IDEAL — Materialisierung: Fachliche Felder (K1/K2/K3)" in `docs/TESTSZENARIEN.md`:

- **K1-Kopfzeile:** HauptFA mit bekannten `FaHierarchyOrderInfo`-Werten oeffnen → Kunde/Termine/Prio/
  AB-Nummer/Montage-Abteilung erscheinen einmal in der Kopfzeile, auf keiner Sub-FA-Zeile dupliziert.
- **Kombigeraet:** Testdaten mit zwei `FaHierarchyOrderInfo`-Zeilen zum selben `HauptFA` (verschiedene
  `MontageAbteilung`) → beide Kopfvarianten sichtbar, „mehrdeutig"-Badge, Log-Eintrag vorhanden.
- **Termin-Kaskade:** zwei Sub-FAs derselben Gruppe, unterschiedliche Werkbaenke mit unterschiedlicher
  `OverridePrePickingDays` → gleicher Fert.-Termin/Liefertermin, unterschiedlicher BG-Termin.
- **Kunde-Filter-Regression:** Freitextsuche nach Kundennamen im hierarchischen Modus → liefert
  Treffer trotz leerer `ProductionOrder.Customer`-Spalte je Zeile.
- **F1/F2 — Update-Pfad + Bestandsnachzug:** Materialisierungs-Lauf gegen bereits existierende
  Sub-FAs mit vorher leeren K2-Feldern → nach dem Lauf befuellt, keine manuelle Nacharbeit.
- **Werkbank-Datenhoheit (je nach Entscheidung):** Variante A — WMS-Zuweisung bleibt nach erneutem
  Sync-Lauf erhalten. Variante B — WMS-Zuweisung wird beim naechsten Lauf ueberschrieben, Log-Eintrag
  vorhanden. Variante C — beide Felder (Sage-Arbeitsbereich, WMS-Werkbank) unabhaengig sichtbar.
- **Unbekannter Arbeitsbereich:** Testdaten mit einem `Arbeitsbereich`-Wert ohne passende
  `ProductionWorkplace`-Zeile → je nach Entscheidung entweder neue Werkbank automatisch angelegt oder
  Sammelmail/Log-Eintrag, kein Absturz.
- **F4 — AKE-Regression:** Master `false`, bestehendes AKE-Testszenario der FA-Liste unveraendert
  gruen; keine neue Spalte sichtbar befuellt.
- **K3-Regression:** Komm.-Status/Lack-T verhalten sich nach dieser Aenderung identisch zu vorher.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

- **Web-App:** ja — `ProductionOrdersController`, `Views/ProductionOrders/Index.cshtml`,
  `FaHierarchyOrderInfoRepository`, ViewModel-Erweiterung.
- **Service:** ja — `FaMaterializationSyncService`, `FaMaterializationPlanner`,
  `FaHierarchySyncService` (Kombigeraet-Log).
- **Migration:** ja, Umfang haengt an Rueckfrage 3/6 (mindestens `Beschichtet`, sofern nicht anders
  geloest) — additiv, nicht daten-destruktiv, kein DB-Backup-Zwang wie bei Teil 7 (keine
  Kern-Tabellen-Inversion), aber Standard-Vorsicht (Backup vor jedem Produktions-Deploy) gilt
  unveraendert.
- **Kontext:** Diese Spec setzt auf Code auf, der aktuell **ausschliesslich** im noch nicht gemergten
  Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch
  `feature/2026-08-07-ideal-teile-1-5`) existiert (`FaMaterializationSyncService`,
  `ProductionOrderListGroup`, `SubOrderNumber` u. a.) — Umsetzung nur in **demselben** Worktree
  sinnvoll. Vor Umsetzungsbeginn `scripts/sync-worktree.ps1` laufen lassen.
- **Publish-Befehle (im Worktree):**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```

## Offene Rueckfragen

1. **Werkbank-Stammdaten:** Existieren `K-02`, `S-01`, `H4-04` etc. bereits als
   `ProductionWorkplace`-Zeilen? Soll der Sync unbekannte Arbeitsbereiche **automatisch anlegen**
   (Risiko: uebernimmt Tippfehler/Altlasten aus Sage) oder nur **melden** (Log + Sammelmail, ein
   Mensch pflegt einmalig nach)?
2. **Bestaetigung Werkbank-Datenhoheit:** Wird fuer IDEAL-Auftraege aktuell **nirgends** manuell eine
   Werkbank zugewiesen? Nur dann ist Variante B (Sage fuehrend, stilles-aber-gemeldetes
   Ueberschreiben) vertretbar. Falls doch manuell zugewiesen wird: Variante A oder C statt B?
3. **K2-Feldumfang:** Sollen ausser Werkbank und Beschichtet weitere Felder (Matchcode,
   Hauptlagerplatz, Artikelgruppe, Artikeltyp, Material, EKBedarf, Fertigungmenge, Masse) materialisiert
   werden — nur die tatsaechlich in einer Liste angezeigten, oder als Vorrat alle verfuegbaren? Jedes
   zusaetzliche Feld bedeutet eine neue Spalte und Migration.
4. **K1-Reichweite:** Bleiben Kunde/Termine/Prio/AB-Nummer/Montage-Abteilung ausschliesslich in der
   Gruppen-Kopfzeile, oder wird zusaetzlich eine Zeilen-Darstellung erwartet (z. B. fuer Export oder
   Spaltenfilter)?
5. **Feld-Mapping Termine:** Welches `FaHierarchyOrderInfo`-Feld entspricht der Anzeige-Spalte
   „Fert.-Termin" — `Neuer_PT_PPS` (aktueller PPS-Fertigstellungstermin) oder `FE_Termin`
   (Blechfertigung fertig)? Entspricht „Liefertermin" `Verladetermin_Vsl`? Am ersten echten
   Datenlauf zu bestaetigen (analog Teil-7-Mapping-Vorbehalt S7-2).
6. **Beschichtet vs. bestehende Lack-Logik:** `FaHierarchyNode.Beschichtet` (K2, aus Sage) trifft auf
   die bereits vorhandene `HasCoatingParts`/`IsCoatingDone`-Logik (BOM-Kategorie
   `LackierteilKategorieName`, aktuell fuer AKE aktiv). Ersetzt das eine das andere fuer IDEAL,
   ergaenzen sich beide, oder bleiben sie unabhaengig nebeneinander bestehen?
7. **Ort der Kombigeraet-Meldung:** Der geforderte Log-Eintrag bei mehrdeutigem `HauptFA`
   (mehrere `FaHierarchyOrderInfo`-Zeilen) — im bestehenden `FaHierarchySyncService` (einmal je
   Sync-Lauf, empfohlen) oder bei jedem Seitenaufruf der FA-Liste (Risiko: Log-Spam)?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen
  (hier: Werkbank-Datenhoheit A/B/C, siehe Rueckfrage 1/2).
-->

1. →
2. →
3. →
4. →
5. →
6. →
7. →
