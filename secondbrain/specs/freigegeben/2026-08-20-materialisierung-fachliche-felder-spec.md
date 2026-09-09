---
type: spec
title: "IDEAL: Materialisierung um die fachlichen Felder erweitern (K1/K2/K3, Werkbank-Datenhoheit)"
slug: 2026-08-20-materialisierung-fachliche-felder-spec
status: InUmsetzung
created: 2026-09-07
updated: 2026-09-09
source_backlog: "[[2026-08-20-materialisierung-fachliche-felder]]"
depends_on: "[[2026-08-18-fa-liste-hierarchie-anzeige-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IDEALAKEWMSService/Services/FaMaterializationSyncService.cs (Anlege- UND Update-Pfad gemeinsam um K2-Felder erweitern, F1; Werkbank-Schreiblogik; Eager-Create der ProductionOrderPickingStatus-Zeile + HasCoatingParts ueber SetCoatingPartsAsync setzen, F5/F6; Klassenkommentar Zeile 21-22 ZWEIMAL korrigieren — Workplace UND PickingStatus.HasCoatingParts fallen aus der app-verwalteten Liste)"
  - "IDEALAKEWMSService/Services/FaMaterializationPlanner.cs (MaterializationSourceOrder um Arbeitsbereich (string?) und ein bereits abgeleitetes HasCoatingParts (bool) erweitern — Ableitung 'selbst oder direktes Kind Beschichtet' als eigener reiner Helper im Service, S8-Muster, Planner bleibt DB-frei)"
  - "IdealAkeWms/Models/ProductionOrder.cs (KEINE neue Spalte mehr, Stand 2026-09-09 — Werkbank nutzt bestehendes ProductionWorkplaceId, HasCoatingParts liegt bereits auf ProductionOrderPickingStatus)"
  - "IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs + Interface (Nachtrag 2026-09-09: Eager-Create-Methode fuer fehlende PickingStatus-Zeilen; bestehende SetCoatingPartsAsync fuer die HasCoatingParts/IsCoatingDone-Kaskade wiederverwenden, F5/F6)"
  - "IdealAkeWms/Models/ProductionWorkplace.cs + IdealAkeWms/Data/Repositories/ProductionWorkplaceRepository.cs (Lookup per Name-Match gegen Arbeitsbereich, case-insensitiv+getrimmt, GetByNamesAsync statt Einzel-Lookup, S2; NUR melden bei unbekanntem/mehrdeutigem Treffer, Rueckfrage 1 — kein automatisches Anlegen)"
  - "IdealAkeWms/Data/Repositories/FaHierarchyOrderInfoRepository.cs + IFaHierarchyOrderInfoRepository.cs (neue gebuendelte Abfrage GetByHauptFaKeysAsync fuer eine Seite von Gruppen, statt Einzel-Query je HauptFA — vermeidet Fan-out ueber die Repository-Grenze)"
  - "IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs (BuildLeitstandQuery: Kunde-Freitextfilter UND server-seitiger Kunde-Spaltenfilter im hierarchischen Modus zusaetzlich per Subquery gegen FaHierarchyOrderInfo.Kunde pruefen, gleiche Join-Logik fuer beide Filterwege, S3/Kunde-Filter-Entscheidung 2026-09-09; betrifft auch PickingLeitstandController, der denselben Filter nutzt)"
  - "IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs (ProductionOrderListGroup: heute NUR OrderNumber+Items, verifiziert — neue K1-Felder Kunde/Konstruktions-Termin (KO_Termin, Nachtrag 2026-09-09)/Fert.-Termin/Liefertermin/Prio/AB-Nummer/Montage-Abteilung + Mehrdeutig-Flag ergaenzen)"
  - "IdealAkeWms/Controllers/ProductionOrdersController.cs (Gruppen-Kopfzeile mit K1 befuellen inkl. Konstruktions-Termin; MapItem/Zeilen-Termine im hierarchischen Modus auf das gruppenweite Fertigungstermin-Feld umstellen statt auf die je K1-Regel leer bleibende ProductionOrder.ProductionDate-Spalte der Zeile, siehe Abschnitt 'Termin-Kaskade'; Beschichtungstermin aus Start_Beschichtung statt Kaskade bei eindeutigem HauptFA + HasCoatingParts, Rueckfrage-6-Nachtrag; Kunde-Filter zusaetzlich gegen FaHierarchyOrderInfo.Kunde pruefen, siehe Abschnitt 'Kunde-Filter'). PickingLeitstandController.cs und FaWorklistController.cs werden NICHT geaendert (View-Scope-Entscheidung: nur FA-Liste) — Beschichtungstermin bleibt dort ueber CoatingDateCalculator, liefert fuer IDEAL weiterhin NULL, dokumentierte Einschraenkung."
  - "IdealAkeWms/Views/ProductionOrders/Index.cshtml (Gruppen-Kopfzeile Zeile ~124-133 um K1-Felder inkl. Konstruktions-Termin + Mehrdeutig-Badge erweitern, aktuell nur 'HauptFA {Nr}' + Sub-FA-Zaehler)"
  - "IDEALAKEWMSService/Services/FaHierarchySyncService.cs (Kombigeraete-Mehrfachzeilen je HauptFA erkennen + einmal je Sync-Lauf protokollieren, Rueckfrage 7)"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs (kein neuer Service-Name noetig — FaMaterialization/FaHierarchy bestehen bereits; neue Counts-Keys: arbeitsbereich_unbekannt, werkbank_abweichung, hauptfa_mehrdeutig)"
  - "Views/FaHierarchy/Index.cshtml:159 (Randfund, NICHT Teil dieser Spec: Tooltip 'Kommissionier-Termin' fuer KO_Termin war fachlich falsch — korrigiert auf 'Konstruktions-Termin', bereits im Worktree editiert, noch nicht committet)"
  - "docs/TESTSZENARIEN.md"
  - "secondbrain/tests/testszenarien-index.md"
open_questions: []
beantwortete_rueckfragen:
  - "Werkbank-Stammdaten fuer unbekannte Arbeitsbereiche: automatisch anlegen oder nur melden (Log+Sammelmail)?"
  - "Bestaetigung: Wird fuer IDEAL-Auftraege aktuell NIRGENDS manuell eine Werkbank zugewiesen? (Vorbedingung fuer Variante B/Sage-fuehrend) + Klassenkommentar-Korrektur"
  - "Welche K2-Felder ausser Werkbank/Beschichtet sollen materialisiert werden — nur angezeigte Spalten oder alle verfuegbaren als Vorrat? (jedes neue Feld = neue ProductionOrder-Spalte + Migration)"
  - "K1 ausschliesslich in der Gruppen-Kopfzeile, oder zusaetzlich je Zeile (Export/Spaltenfilter)?"
  - "Exaktes Feld-Mapping FaHierarchyOrderInfo -> Anzeige-Spalten: Neuer_PT_PPS oder FE_Termin fuer 'Fert.-Termin'? Verladetermin_Vsl fuer 'Liefertermin'?"
  - "Verhaeltnis FaHierarchyNode.Beschichtet (K2) zur bestehenden HasCoatingParts/LackierteilKategorieName-Logik (BOM-Kategorie, bereits fuer AKE aktiv) — ersetzen, ergaenzen oder unabhaengig nebeneinander?"
  - "Kombigeraete-Mehrfachzeilen (1:n FaHierarchyOrderInfo je HauptFA): Log-Eintrag im FaHierarchySyncService (Sync-Zeit, einmal je Lauf) oder bei jedem Seitenaufruf der FA-Liste?"
  - "(2026-09-09, Kritische Pruefung Runde 2) HasCoatingParts/Beschichtungstermin in dieser Spec belassen oder in Folge-Spec auslagern? -> in dieser Spec belassen, Quelle FaHierarchyNode.Beschichtet + FaHierarchyOrderInfo.Start_Beschichtung"
  - "(2026-09-09) Umfang der Ansichten: nur FA-Liste oder alle sechs? -> nur FA-Liste (Referenz-Etappe)"
  - "(2026-09-09) Werkbank-Abweichungsmeldung: unscharf oder Quellwert mitspeichern? -> unscharf, kein neues Feld"
  - "(2026-09-09) Kunde-Spaltenfilter im hierarchischen Modus: Join-Fix oder Spalte ausblenden? -> gleiche Join-Logik wie Freitextfilter"
  - "(2026-09-09) KO_Termin-Bedeutung: Kommissionier- oder Konstruktions-Termin? -> Konstruktions-Termin (Fachbereich); zusaetzlich als eigenes K1-Feld aufgenommen; Label-Fehler in Views/FaHierarchy/Index.cshtml korrigiert"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: "Werkbank-Datenhoheit: Variante B (Sage fuehrend) mit Abweichungs-Meldung als Umschaltpunkt auf C. Zusatz 2026-09-09: HasCoatingParts (aus FaHierarchyNode.Beschichtet) + Beschichtungstermin (aus Start_Beschichtung) bleiben in Scope; Umfang nur FA-Liste (/ProductionOrders); Werkbank-Abweichungsmeldung unscharf ohne neues Feld; Kunde-Spaltenfilter per gleicher Join-Logik wie Freitextfilter; KO_Termin = Konstruktions-Termin, zusaetzliches K1-Feld."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-09-09
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> **Verzahnung (2026-09-08)** mit [[2026-09-08-bom-schnittstellen-bridge-hierarchisch]]
> (Stueckliste ueber die Repository-Schnittstelle). Dort wurde entschieden: die AKE-Heuristiken
> `CoatingDetection`/`FaWorkStepDetection` werden fuer hierarchische Auftraege **hart abgeschaltet**;
> `HasCoatingParts` kommt daher **hier** aus der Materialisierung (K2: „Sub-FA selbst oder ein
> direktes Kind `Beschichtet`") — das beantwortet Rueckfrage 6 in Richtung **ersetzen**. Zusaetzlich
> zu Rueckfrage 5: `CoatingDateCalculator.Compute(vorkommissionierTermin, …)` rechnet aus
> `ProductionDate` (bei IDEAL NULL) → der **Beschichtungstermin** muss fuer IDEAL aus FAInfos
> `Start_Beschichtung` kommen (K1), sonst erzeugt das Flag allein keinen Termin.
>
> **Stand 2026-09-09:** Zwischen dieser Notiz und heute gab es einen Umweg — die
> Freigabe-Antwort 6 und die erste Reaktion auf die Kritische Pruefung („ANTWORTEN … 2026-09-08")
> hatten `HasCoatingParts`/Beschichtungstermin komplett aus dieser Spec geworfen. Das ist mit der
> finalen Fachbereichs-Entscheidung (siehe „ANTWORTEN auf die Kritische Pruefung (2026-09-09)")
> **zurueckgenommen** — diese urspruengliche Verzahnungs-Notiz gilt wieder, siehe K2.

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
   Auftragsdaten aus `FaHierarchyOrderInfo` befuellen (Kunde, Termine — inkl. **Konstruktions-Termin**
   `KO_Termin`, siehe Nachtrag 2026-09-09 —, Prio, AB-Nummer, Montage-Abteilung), inkl. der 1:n-Regel
   bei Kombinationsgeraeten (alle Kopfvarianten im Klartext + Mehrdeutig-Badge, kein stilles
   Auswaehlen).
2. K2 — `FaMaterializationSyncService` (Anlege- **und** Update-Pfad) um die entschiedenen Zeilenfelder
   aus `FaHierarchyNode` erweitern, Feld-fuer-Feld, kein `SetValues`.
3. Die Werkbank-Datenhoheits-Entscheidung — **final Variante B** (Sage fuehrend, unscharfe
   Abweichungsmeldung, kein neues Feld) — inkl. der Rueckfrage-1-Antwort (melden statt automatisch
   anlegen) und der Klassenkommentar-Korrektur, weil `Workplace` die app-verwaltete Liste verlaesst.
4. **HasCoatingParts-Ableitung + Beschichtungstermin (Nachtrag 2026-09-09, nur FA-Liste).**
   `PickingStatus.HasCoatingParts` wird aus `FaHierarchyNode.Beschichtet` (Sub-FA selbst oder direktes
   Kind) abgeleitet und ersetzt fuer IDEAL die AKE-Heuristik; der Beschichtungstermin kommt aus
   `FaHierarchyOrderInfo.Start_Beschichtung` (K1) und wird nur in `/ProductionOrders` angezeigt.
   Das ist die **zweite** Z1-Ausnahme neben `Workplace` (F5) und braucht Eager-Create der
   `PickingStatus`-Zeile (F6). K3 reduziert sich damit auf Komm.-Status
   (`PickingStatus.IsDonePicking`/`BdeStatus`), der weiterhin zu Recht leer bleibt — keine
   Code-Aenderung dafuer.
5. Der bestehende Kunde-Filter in `ProductionOrdersController` im hierarchischen Modus so erweitern,
   dass er trotz leerer `ProductionOrder.Customer`-Spalte (K1-Regel) weiterhin Treffer liefert —
   **sowohl** der Freitextfilter **als auch** der serverseitige Kunde-Spaltenfilter (gleiche
   HauptFA-Join-Logik, betrifft auch `PickingLeitstandController`).
6. Testszenarien fuer alle Klassen inkl. AKE-Flachmodus-Regression (bit-identisch).

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

**Felder:** Kunde, Konstruktions-Termin (`KO_Termin`, Nachtrag 2026-09-09), BG-Termin/Fert.-Termin/
Liefertermin (Termin-Kaskade siehe unten), Prio, AB-Nummer (`ABNr`), Montage-Abteilung.

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
(Werkbank-Zuordnung, siehe eigener Abschnitt unten).

**Nachtrag (2026-09-09, Fachbereichs-Entscheidung): `Beschichtet` UND der Beschichtungstermin werden
doch aufgenommen — kein Aufschub in eine Folge-Spec.** Damit revidiert diese Spec die vorherige
Zwischen-Antwort („HasCoatingParts komplett raus"); Grund ist die bereits **freigegebene**
BOM-Bridge-Spec ([[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]]), die diese
Ersatzquelle ausdruecklich **dieser** Spec zuweist (Design H). **Keine neue `ProductionOrder`-Spalte**
fuer ein rohes `Beschichtet` — stattdessen die bereits bestehende Ableitung auf der Satelliten-Tabelle:

- **`HasCoatingParts`** (auf `ProductionOrderPickingStatus`, ADR 0009) ← „Sub-FA selbst oder ein
  direktes Kind ist `FaHierarchyNode.Beschichtet`" (Stueckliste/Struktur-Quelle, wie in Rueckfrage 6
  urspruenglich entschieden: **ersetzt** die AKE-Heuristik `CoatingDetectionService` fuer IDEAL, nie
  beide nebeneinander).
- **Beschichtungstermin** (Anzeige, nur FA-Liste, siehe Werkbank-analoge View-Scope-Entscheidung) ←
  `FaHierarchyOrderInfo.Start_Beschichtung` (K1, je HauptFA), angewendet auf jede Sub-FA-Zeile mit
  `HasCoatingParts = true` — **nicht** ueber `CoatingDateCalculator`/die Vorkommissionier-Kaskade
  (die liefert fuer IDEAL ohnehin `NULL`, da `ProductionDate` leer bleibt, K1-Regel).
- **Kombigeraet-Regel (aus S7 uebernommen):** Ist das `HauptFA` mehrdeutig (mehrere
  `FaHierarchyOrderInfo`-Zeilen mit ggf. abweichendem `Start_Beschichtung`/`Dienstleister`/`RAL`),
  zeigt **keine** Sub-FA-Zeile dieser Gruppe einen Beschichtungstermin — kein stilles Auswaehlen
  einer Variante (dieselbe Regel wie bei den uebrigen K1-Feldern, siehe Kombigeraete-Notiz
  [[2026-08-06-kombinationsgeraete-montageabteilung]]: Dienstleister/RAL koennen bei Kombigeraeten
  fachlich unterschiedlich sein — ein falsch gewaehlter Kopf schickt Teile zum falschen Beschichter).
- **View-Scope-Konsequenz (aus der Umfangs-Entscheidung „nur FA-Liste" abgeleitet):** Nur
  `ProductionOrdersController`/`Views/ProductionOrders/Index.cshtml` zeigen den Beschichtungstermin
  aus `Start_Beschichtung`. `PickingLeitstandController` und `FaWorklistController` bleiben
  **unveraendert** bei ihrer bestehenden `CoatingDateCalculator`-Berechnung, die fuer IDEAL `NULL`
  liefert — bekannte Einschraenkung, konsistent mit B5. Das `HasCoatingParts`-Flag selbst (Lack-T-Icon)
  erscheint dagegen **automatisch** in allen sechs Ansichten, sobald es geschrieben ist — es ist ein
  persistiertes Feld auf der Satelliten-Tabelle, keine View-spezifische Berechnung.

**F5 — zweite Z1-Ausnahme (PFLICHT, siehe F3).** `ProductionOrderPickingStatus.HasCoatingParts`
faellt zusaetzlich zu `Workplace` aus der „nie ueberschreiben"-Regel des Materialisierungs-Syncs
heraus. **Alle anderen** `PickingStatus`-Felder (`IsDonePicking`, `PickingPriority`,
`AssignedPicker`, `HasGlass`, `HasExternalPurchase`, `IsReleasedForPicking`) bleiben tabu. Kippt die
Ableitung von `true` auf `false`, wird `IsCoatingDone` **mitzurueckgesetzt** (Fallstrick #11) — dafuer
die bestehende `ProductionOrderPickingStatusRepository.SetCoatingPartsAsync`-Methode wiederverwenden
(kein rohes Feld-Setzen, F3 bleibt durch Nutzung der etablierten Methode gewahrt). Der Klassenkommentar
von `FaMaterializationSyncService` wird ein **zweites Mal** korrigiert (nach der Workplace-Korrektur):
`PickingStatus` steht dort weiterhin als app-verwaltet, **ausser** fuer `HasCoatingParts`.

**F6 — Eager-Create der `PickingStatus`-Zeile (PFLICHT, sonst schreibt F5 ins Leere).** Anders als
`SageImportService` (AKE, legt `PickingStatus`/`BdeStatus` eager per Folge-MERGE an) erzeugt die
IDEAL-Materialisierung heute **keine** `ProductionOrderPickingStatus`-Zeile. Der Sync muss beim
Anlegen einer neuen `ProductionOrder`-Zeile **und** — fuer die bereits materialisierten Bestands-Sub-FAs
— im Update-Zweig, falls die Zeile noch fehlt, eine `PickingStatus`-Zeile eager anlegen, bevor
`HasCoatingParts` gesetzt werden kann. Ohne diesen Schritt bleibt die Ableitung fuer alle Bestandszeilen
wirkungslos (F1/F2-Konsequenz: derselbe Fehler wie beim urspruenglichen Anlegen/Update-Unterschied
darf sich hier nicht wiederholen).

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
tabu, **ausser** fuer die beiden Felder, die diese Spec ausdruecklich davon ausnimmt: `Workplace`
(Werkbank-Entscheidung unten) und `PickingStatus.HasCoatingParts` (F5 oben).

**F4 — AKE unveraendert.** Der gesamte Materialisierungspfad laeuft nur bei Master `true`; bei
`false` aendert sich nichts an `ProductionOrders`, auch nicht an neu hinzugefuegten Spalten (bleiben
fuer AKE-Zeilen `NULL`, kein Befuellungsversuch).

### K3 — WMS-eigene Felder, zu Recht leer

**Nachtrag (2026-09-09): Lack-T (`HasCoatingParts`/`IsCoatingDone`) ist ab dieser Spec KEIN K3-Feld
mehr** — es wird jetzt aktiv aus `FaHierarchyNode.Beschichtet` abgeleitet (siehe K2, F5/F6 oben).
Was als K3 uebrig bleibt: **Komm.-Status** (`PickingStatus.IsDonePicking`, `BdeStatus.IsDoneBde`)
entsteht weiterhin erst durch Arbeit im WMS selbst (Kommissionierung/BDE abschliessen). **Kein
Handlungsbedarf**, keine Code-Aenderung dafuer — diese Spec haelt nur fest, dass diese beiden
Felder **nicht** ebenfalls aus Sage befuellt werden.

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
- **`ProductionOrderListGroup`** (VM): neue Properties `Customer`, `KonstruktionsTermin` (`KO_Termin`,
  Nachtrag 2026-09-09), `ProductionDate`, `DeliveryDate`, `Prio`, `AbNummer`, `MontageAbteilung`,
  `IsAmbiguous` (bzw. eine Liste von Kopfvarianten bei Mehrdeutigkeit — exakte Form
  Dev-Lauf-Entscheidung).
- **`FaMaterializationSyncService`/`FaMaterializationPlanner`** (Nachtrag 2026-09-09, F5/F6): Anlege-
  **und** Update-Zweig legen bei fehlender `ProductionOrderPickingStatus`-Zeile diese eager an, leiten
  `HasCoatingParts` aus den Kindknoten des Sub-FA ab (eigener reiner Helper, S8-Muster — der Planner
  selbst bleibt DB-frei) und schreiben sie ueber `ProductionOrderPickingStatusRepository.
  SetCoatingPartsAsync` (IsCoatingDone-Reset inklusive). Klassenkommentar zweite Korrektur (F5).
- **`FaHierarchySyncService`**: nach dem Full-Refresh pruefen, ob ein `HauptFA` mehr als eine
  `FaHierarchyOrderInfo`-Zeile hat; falls ja, Log-Eintrag mit `HauptFA` + Anzahl (einmal je Sync-Lauf,
  nicht je Seitenaufruf — siehe Rueckfrage 7).
- **`Views/ProductionOrders/Index.cshtml`**: Gruppen-Kopfzeile (aktuell nur „HauptFA {Nr}" +
  Sub-FA-Zaehler) um Kunde/Termine/Prio/AB-Nummer/Montage-Abteilung sowie den Mehrdeutig-Badge
  erweitern; bei Master `false` unveraendert (kein Gruppen-Rendering, F4).

## Migrations-/SQL-Auswirkungen

**K1 — keine Migration.** Alle K1-Felder (inkl. `KO_Termin`/Konstruktions-Termin) werden
ausschliesslich lesend aus der bereits vorhandenen Cache-Tabelle `FaHierarchyOrderInfo` angezeigt,
nicht materialisiert.

**K2 — KEINE Migration mehr noetig (Stand 2026-09-09).** Alle drei K2-Punkte betreffen bereits
bestehende Tabellen/Spalten:
- **Werkbank:** `ProductionOrder.ProductionWorkplaceId` existiert bereits (Teil 6/vorher) — **keine**
  neue Spalte, nur eine neue Schreiblogik im Sync. „Melden statt automatisch anlegen" (Rueckfrage 1)
  betrifft ohnehin nur neue `ProductionWorkplace`-**Zeilen** (bestehende Tabelle, kein Schema-Impact).
- **`HasCoatingParts`** liegt bereits auf `ProductionOrderPickingStatus` (ADR 0009) — **kein** rohes
  `Beschichtet`-Feld auf `ProductionOrder`, nur eine neue Ableitungslogik + Eager-Create der Zeile
  (F5/F6), beides ohne Schema-Aenderung.
- **Weitere K2-Felder** (Matchcode, Hauptlagerplatz, Artikelgruppe, Artikeltyp, Material, EKBedarf,
  Fertigungmenge, Masse): laut Rueckfrage-3-Antwort **nicht** aufgenommen — kein Migrationsbedarf.

`deploy.migration: false` im Frontmatter ist damit final (nicht nur vorlaeufig). Falls ein
kuenftiger Dev-Lauf doch eine Spalte braucht, gilt unveraendert: Model -> `dotnet ef migrations add
<Name>` -> idempotentes `SQL/91_<Name>.sql` mit `OBJECT_ID`/`COL_LENGTH`-Guards -> `SQL/00_
FreshInstall.sql` an **beiden** Stellen (Schema + `MigrationId`).
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
   Kunde, Konstruktions-Termin (`KO_Termin`), Fert.-Termin, Liefertermin, Prio, AB-Nummer und
   Montage-Abteilung aus `FaHierarchyOrderInfo` — ohne dass diese Werte auf den einzelnen
   Sub-FA-Zeilen dupliziert oder in `ProductionOrders` materialisiert werden.
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
11. **K3 unveraendert (eingeschraenkt auf Komm.-Status).** `PickingStatus.IsDonePicking`/`BdeStatus`
    bleiben durch diese Spec unangetastet — Regressionstest zeigt keine Verhaltensaenderung. **Lack-T
    (`HasCoatingParts`/`IsCoatingDone`) ist ab 2026-09-09 kein K3-Feld mehr**, siehe AK 12/13.
12. **HasCoatingParts-Ableitung (K2, Nachtrag 2026-09-09).** Nach einem Materialisierungs-Lauf zeigt
    jeder Sub-FA, der selbst oder dessen direktes Kind `Beschichtet = true` ist,
    `PickingStatus.HasCoatingParts = true` — unabhaengig davon, ob die Zeile vorher bereits
    materialisiert war (Eager-Create der `PickingStatus`-Zeile, F6) oder neu angelegt wird. Kippt die
    Ableitung von `true` auf `false`, ist `IsCoatingDone` danach ebenfalls `false` (Fallstrick #11).
    Die AKE-Heuristik (`CoatingDetectionService`) bleibt fuer IDEAL-Zeilen inaktiv (Design H,
    BOM-Bridge-Spec) — nie beide Quellen gleichzeitig.
13. **Beschichtungstermin (K1→Anzeige, nur FA-Liste).** Bei eindeutigem `HauptFA` mit
    `HasCoatingParts = true` zeigt die Zeile in `/ProductionOrders` den Termin aus
    `FaHierarchyOrderInfo.Start_Beschichtung`. Bei mehrdeutigem `HauptFA` (Kombigeraet) zeigt **keine**
    Zeile dieser Gruppe einen Beschichtungstermin (keine stille Auswahl). `PickingLeitstand` und
    `FaWorklist` zeigen fuer IDEAL weiterhin keinen Beschichtungstermin (bestehende
    `CoatingDateCalculator`-Berechnung liefert dort `NULL`) — dokumentierte Einschraenkung, keine
    Regression.

## Test-Szenarien

Neues Kapitel „IDEAL — Materialisierung: Fachliche Felder (K1/K2/K3)" in `docs/TESTSZENARIEN.md`:

- **K1-Kopfzeile:** HauptFA mit bekannten `FaHierarchyOrderInfo`-Werten oeffnen → Kunde/
  Konstruktions-Termin/Fert.-Termin/Liefertermin/Prio/AB-Nummer/Montage-Abteilung erscheinen einmal
  in der Kopfzeile, auf keiner Sub-FA-Zeile dupliziert.
- **Kombigeraet:** Testdaten mit zwei `FaHierarchyOrderInfo`-Zeilen zum selben `HauptFA` (verschiedene
  `MontageAbteilung`) → beide Kopfvarianten sichtbar, „mehrdeutig"-Badge, Log-Eintrag vorhanden.
- **Termin-Kaskade:** zwei Sub-FAs derselben Gruppe, unterschiedliche Werkbaenke mit unterschiedlicher
  `OverridePrePickingDays` → gleicher Fert.-Termin/Liefertermin, unterschiedlicher BG-Termin.
- **Kunde-Filter-Regression:** Freitextsuche nach Kundennamen im hierarchischen Modus → liefert
  Treffer trotz leerer `ProductionOrder.Customer`-Spalte je Zeile.
- **Kunde-Spaltenfilter-Regression (2026-09-09):** Serverseitiger Spaltenfilter „Kunde" (nicht der
  Freitextfilter) im hierarchischen Modus → liefert ebenfalls Treffer ueber denselben
  `FaHierarchyOrderInfo`-Join; identischer Test fuer `PickingLeitstandController`, der denselben
  Filter nutzt.
- **HasCoatingParts-Ableitung (2026-09-09):** Sub-FA mit direktem Kind `Beschichtet = true` →
  `PickingStatus.HasCoatingParts = true` nach dem Lauf, Eager-Create der `PickingStatus`-Zeile
  nachgewiesen fuer eine zuvor ohne Zeile materialisierte Bestands-FA. Negativfall: Ableitung kippt
  von `true` auf `false` → `IsCoatingDone` wird mitzurueckgesetzt.
- **Beschichtungstermin (2026-09-09):** Eindeutiges `HauptFA` mit `HasCoatingParts = true` und
  gesetztem `Start_Beschichtung` → Termin erscheint in `/ProductionOrders`. Negativfall: mehrdeutiges
  `HauptFA` (Kombigeraet, verschiedene `Start_Beschichtung`/`Dienstleister`/`RAL`) → **kein**
  Beschichtungstermin auf den Zeilen dieser Gruppe. Regressionscheck: `PickingLeitstand`/`FaWorklist`
  zeigen fuer dieselbe FA weiterhin keinen Beschichtungstermin (View-Scope-Grenze).
- **F1/F2 — Update-Pfad + Bestandsnachzug:** Materialisierungs-Lauf gegen bereits existierende
  Sub-FAs mit vorher leeren K2-Feldern → nach dem Lauf befuellt, keine manuelle Nacharbeit.
- **Werkbank-Datenhoheit (Variante B, final 2026-09-09):** Eine simulierte manuelle Werkbank-Zuweisung
  wird beim naechsten Sync-Lauf ueberschrieben; die Meldung lautet unscharf „Werkbank weicht vom
  Quellwert ab" (kein `SourceWorkplaceName`-Feld, keine Behauptung „manuell"), Log-Eintrag mit
  altem/neuem Wert vorhanden.
- **Unbekannter Arbeitsbereich:** Testdaten mit einem `Arbeitsbereich`-Wert ohne passende
  `ProductionWorkplace`-Zeile → je nach Entscheidung entweder neue Werkbank automatisch angelegt oder
  Sammelmail/Log-Eintrag, kein Absturz.
- **F4 — AKE-Regression:** Master `false`, bestehendes AKE-Testszenario der FA-Liste unveraendert
  gruen; keine neue Spalte sichtbar befuellt.
- **K3-Regression:** Komm.-Status (`IsDonePicking`/`BdeStatus`) verhaelt sich nach dieser Aenderung
  identisch zu vorher. (Lack-T/`HasCoatingParts` ist ab 2026-09-09 kein K3-Feld mehr — dafuer gelten
  die eigenen Szenarien oben, nicht dieses Regressionsszenario.)

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

1. → **MELDEN, nicht automatisch anlegen.**
   Stammdaten aus einer Fremdquelle automatisch zu erzeugen fuellt die Werkbank-Liste mit allem, was
   in Sage steht — auch mit Tippfehlern und Altlasten, und ohne dass es jemand entschieden hat.
   Die Zahl der Arbeitsbereiche ist klein und stabil (im Testbestand `K-02`, `S-01`, `H1-03`,
   `H2-02`, `H4-04`); das Nachpflegen faellt **einmalig** an, danach kommt selten etwas dazu.
   **Damit das Melden nicht zur Sackgasse wird:** Die Meldung muss das Nachpflegen trivial machen —
   Liste der unbekannten Arbeitsbereiche **mit Anzahl betroffener Auftraege**, im Log und in der
   Sammelmeldung des Sync-Laufs. Nicht „unbekannter Wert" je Zeile, sondern eine Liste je Lauf.

   **Vorher zu pruefen — das aendert den Umfang:** Ist `ProductionOrder.Workplace` ein
   **String-Feld** oder ein **Fremdschluessel** auf `ProductionWorkplace`?

   **NACHTRAG (2026-09-08): Es ist ein FREMDSCHLUESSEL.** Das `affected_code` dieser Spec haelt
   verifiziert fest: *„aktuell existiert NUR `ProductionWorkplaceId` aus der K2-Liste"*. Damit gilt
   der zweite Fall — **die Zuweisung kann nicht stattfinden, solange der Stammsatz fehlt.**

   **Konsequenz fuer den Ablauf, die dokumentiert gehoert:** Der erste Materialisierungs-Lauf nach
   dieser Umsetzung wird die unbekannten Arbeitsbereiche **melden**, aber die Werkbank-Spalte
   **bleibt leer**. Erst nachdem ein Mensch die fuenf Arbeitsplaetze angelegt hat, fuellt der
   **naechste** Lauf sie. Das ist ein **Zwei-Lauf-Ablauf** — er ist in Ordnung, muss aber in den
   Deploy-Abschnitt, sonst haelt beim ersten Lauf jemand die Umsetzung fuer kaputt.
   Alternativ koennen die Arbeitsplaetze **vor** dem Deploy angelegt werden (Namen sind aus
   `FaHierarchyNode.Arbeitsbereich` bekannt) — dann greift die Zuweisung sofort. **Das ist der
   sauberere Weg** und gehoert als Deploy-Vorbedingung empfohlen.
   - **String:** Die Zuweisung funktioniert auch fuer unbekannte Werte; die Spalte ist sofort
     gefuellt, nur werkbank-basierte Filter/Gruppierungen zeigen einen Wert ohne Stammsatz. Dann
     ist „melden" folgenlos gut.
   - **Fremdschluessel:** Die Zuweisung **kann nicht stattfinden**, solange der Stammsatz fehlt —
     dann bleibt die Spalte trotz Materialisierung leer, bis jemand nachpflegt. Das gehoert dann
     ausdruecklich in die Deploy-Vorbedingung, sonst wundert sich beim ersten Lauf jemand.

2. → **Variante B (Sage fuehrend) — mit Meldung bei Abweichung und einem definierten Umschaltpunkt.**
   Heute ist B unbedenklich: Die Werkbank-Spalte ist fuer IDEAL-Auftraege **leer**, also hat noch
   niemand manuell zugewiesen — es gibt nichts zu ueberschreiben.
   **Das Risiko liegt in der Zukunft:** Sobald die Spalte gefuellt ist, koennte jemand umdisponieren
   wollen (bei AKE macht das vermutlich der Leitstand). Deshalb:
   - Der Sync **meldet**, wenn er einen `Workplace` ueberschreibt, der vom Quellwert abweicht —
     also wenn jemand manuell zugewiesen hat.
   - **Diese Meldung ist der Umschaltpunkt:** Schlaegt sie an, ist damit belegt, dass manuell
     disponiert wird, und die Entscheidung wandert auf **Variante C** (Sage-Arbeitsbereich und
     WMS-Werkbank als getrennte Felder). Vorher waere C Vorratsbau.
   So faengt man klein an, ohne sich zu verbauen — und der Wechsel haengt an einem Beleg statt an
   einer Vermutung.
   **Mitzuziehen:** `Workplace` faellt damit aus der app-verwalteten Liste des
   `FaMaterializationSyncService`; der Klassenkommentar dort ist zu korrigieren, sonst sagt der Code
   das eine und die Dokumentation das andere.

3. → **NUR die tatsaechlich angezeigten Felder. Kein Vorrat.**
   Jedes zusaetzliche Feld ist eine Spalte plus Migration — und ein Feld, das niemand ansieht, faellt
   nicht auf, wenn es falsch befuellt ist. Genau diese Klasse stiller Fehler zieht sich durch das
   ganze Paket.
   **Minimalsatz K2:** `Arbeitsbereich` → Werkbank, `Beschichtet`.
   **Vorher zu klaeren:** Was bedeutet die Spalte **„Komm."** in der FA-Liste — das
   **Kommissionier-Ziel** aus `FaHierarchyNode` (dann K2, gehoert dazu) oder den
   **Kommissionier-Status** des WMS (dann K3, bleibt zu Recht leer)? Der Spaltenkopf traegt ein
   Sortier-Zeichen, was eher auf einen Status hindeutet — aber das ist geraten und muss am Code
   nachgesehen werden.
   `Matchcode`, `Hauptlagerplatz`, `Artikelgruppe`, `Artikeltyp`, `Material`, `EKBedarf`,
   `Fertigungmenge`, Masse: **nicht** materialisieren, solange keine Liste sie zeigt. Wenn spaeter
   eine Anzeige entsteht, kommt das Feld mit ihr — dann ist auch klar, wozu.

4. → **Kopfzeile fuer die ANZEIGE — aber Filter und Export brauchen einen eigenen Weg.**
   Kunde, Termine, Prio, AB-Nummer und Montage-Abteilung haengen am HauptFA; sie in 39 Zeilen zu
   wiederholen ist Redundanz. **Keine Materialisierung je Zeile.**
   **Aber die Rueckfrage trifft einen echten Punkt:** Die FA-Liste hat in der Filterkarte bereits
   ein **Kunde**-Feld. Ein Wert, der nur in der Gruppen-Kopfzeile steht, ist fuer einen
   zeilenbasierten Filter unsichtbar — der Filter liefe ins Leere, ohne Fehlermeldung.
   **Vorgabe:** Der Kunde-Filter (und jeder kuenftige Filter auf einem Kopfdatum) arbeitet
   **server-seitig ueber einen Join auf `FaHierarchyOrderInfo` je `HauptFA`** und schraenkt
   **Gruppen** ein — nicht ueber eine duplizierte Zeilenspalte. Das ist derselbe Weg wie bei den
   Kopfdaten selbst: eigene Abfrage je HauptFA, **kein Fan-out-Join** auf die Positionen.
   **Export:** nimmt die Kopfdaten je Gruppe mit (einmal je Gruppe, nicht je Zeile). Falls ein
   flacher Export je Zeile gebraucht wird, ist das ein eigener Umfang — hier bewusst nicht.

5. → **Vorschlag mit Pflicht zur Bestaetigung am ersten Datenlauf:**
   | Anzeige-Spalte | Vorschlag | Begruendung |
   |---|---|---|
   | Fert.-Termin | `FE_Termin` | Fertigstellung; erscheint bereits so benannt in der FA-Struktur-Kopfzeile |
   | Liefertermin | `Verladetermin_Vsl` | Verladung = Auslieferung |
   | BG-Termin | **vermutlich ohne Entsprechung** | in `FaHierarchyOrderInfo` kein passendes Feld erkennbar |
   **Fuer `BG-Termin` gilt die F7-Regel:** Eine Spalte ohne befuellbare Quelle wird im hierarchischen
   Modus **ausgeblendet**, nicht leer stehen gelassen — eine sichtbar leere Spalte, auf die ein
   Filter oder eine Sortierung wirkt, liefert sonst eine leere Liste ohne erkennbaren Grund.
   **`Neuer_PT_PPS`** (aktueller PPS-Produktionstermin) ist **nicht** dasselbe wie `FE_Termin` und
   gehoert nur dann als eigene Spalte dazu, wenn der Fachbereich ihn in der FA-Liste braucht — offen
   und mit demselben Fachbereich zu klaeren wie die Vormontage-Filterfrage.
   **Bestaetigung:** an einem HauptFA die drei Termine gegen das PPS/Sage-Bild vergleichen. Das
   Mapping steht und faellt mit einer Sichtpruefung, nicht mit der Feldbenennung.

6. → **ERSETZT — nicht ergaenzt, und nie beide gleichzeitig.**
   Fuer IDEAL ist `FaHierarchyNode.Beschichtet` (aus Sage) die Wahrheit. Die AKE-Heuristik
   (BOM-Kategorie-Matching ueber `LackierteilKategorieName` in `CoatingDetectionService`) wird fuer
   hierarchische Auftraege **hart abgeschaltet** — das ist bereits als Klasse-D-Gate in
   [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]] (Design H) festgelegt.
   Aufgabenteilung zwischen den beiden Specs:
   - **BOM-Bridge:** schaltet die Heuristik ab (Gates).
   - **Diese Spec:** liefert die Ersatzquelle — `HasCoatingParts` fuer einen Sub-FA aus
     „er selbst oder eines seiner direkten Kinder ist `Beschichtet`".
   Fuer AKE bleibt alles unveraendert. **Beide Logiken laufen nie nebeneinander** — der Master
   entscheidet, welche gilt.
   **Zeitliche Luecke bewusst benennen:** Zwischen der BOM-Bridge (Heuristik aus) und dieser Spec
   (Ersatz da) bleibt `HasCoatingParts` fuer IDEAL leer. Das ist gewollt — leer ist besser als
   falsch — aber es faellt jemandem auf, und dann soll die Erklaerung auffindbar sein.
   **Der Beschichtungstermin** kommt bei IDEAL aus FAInfos `Start_Beschichtung`, nicht aus
   `CoatingDateCalculator` (der rechnet aus dem Vorkommissionier-Termin, und der ist bei IDEAL NULL).

7. → **Im `FaHierarchySyncService`, einmal je Sync-Lauf.**
   Nicht bei jedem Seitenaufruf: Ein mehrdeutiger `HauptFA` ist eine **Eigenschaft der Daten**, keine
   der Anzeige — sie aendert sich zwischen zwei Seitenaufrufen nicht, und ein Log-Eintrag je Aufruf
   erzeugt Rauschen, das die Meldung entwertet. Damit folgt sie demselben Muster wie die uebrigen
   Invarianten dieses Pakets (Umhaeng-Konflikt, vermisste FAs).
   **Die Kennzeichnung in der Oberflaeche bleibt** — sie ist Anzeige und gehoert dorthin; nur der
   **Log-Eintrag** wandert in den Sync.
   Form wie bei den vermissten FAs: **eine Sammelmeldung je Lauf** mit der Liste der betroffenen
   HauptFAs, nicht ein Eintrag je Fall.

## ANTWORTEN auf die Kritische Pruefung (2026-09-08)

### Zu B1 — Blocker berechtigt. **`HasCoatingParts` und `Beschichtet` fallen KOMPLETT aus dieser Spec.**

> **UEBERHOLT (2026-09-09):** Diese Antwort wurde durch eine explizite Fachbereichs-Entscheidung
> zurueckgenommen — `HasCoatingParts`-Ableitung **und** Beschichtungstermin sind wieder **in
> Scope** (siehe K2 oben und „ANTWORTEN auf die Kritische Pruefung (2026-09-09)"). Bleibt als
> historischer Beleg stehen, ist aber **nicht mehr gueltig**.

Meine Antwort 6 hat ueberreicht. Die Kette, die der Reviewer am Code belegt, ist zwingend:
`HasCoatingParts` liegt auf `ProductionOrderPickingStatus`; `PickingStatus` steht in der
Z1-Liste der app-verwalteten Felder; und fuer IDEAL legt die Materialisierung **gar keine
PickingStatus-Zeile an** — ein Schreibpfad dorthin schriebe ins Leere. Dazu die Kopplung an
`IsCoatingDone` beim Kippen auf `false` (Fallstrick #11).

**Und das entscheidende Argument ist meine eigene Antwort 3:** Keine der sechs Ansichten zeigt ein
rohes `Beschichtet`. Nach der Regel „nur tatsaechlich angezeigte Felder" gibt es fuer die neue
Spalte keinen Grund.

**Verbindlich:**
- **Kein neues Feld `Beschichtet` an `ProductionOrder`.** `deploy.migration` faellt auf `false`,
  `SQL/91_*.sql` entfaellt aus `affected_code`.
- **Diese Spec schreibt `HasCoatingParts` NICHT.** K3 und AK 11 bleiben unveraendert gueltig:
  Lack-T wird nicht angetastet, der Regressionstest beweist keine Aenderung. Der Widerspruch ist
  damit zugunsten des Spec-Koerpers aufgeloest, nicht zugunsten meiner Antwort.
- **Folge, die benannt gehoert:** Fuer IDEAL bleiben `Beschicht.` und `Lack-T` **leer**. Die
  BOM-Bridge hat die AKE-Heuristik abgeschaltet (Design H), eine Ersatzquelle entsteht hier
  **nicht**. Das ist gewollt — leer ist besser als falsch — aber es ist eine **offene fachliche
  Luecke**, keine Erledigung.
- **Eigene Backlog-Notiz anlegen:** „Beschichtung fuer IDEAL" — mit dem vollstaendigen Bild, das
  dieser Review geliefert hat: fehlende PickingStatus-Zeile, zweite Z1-Ausnahme,
  `IsCoatingDone`-Kopplung, und die drei Controller, die den Beschichtungstermin rechnen.

### Zu B2 — Antwort 5 war falsch: **BG-Termin wird NICHT ausgeblendet.**

Ich hatte „keine Entsprechung in FAInfos" geschlossen und daraus F7 (ausblenden) abgeleitet. Der
Reviewer zeigt am Code: **BG-Termin ist eine Ableitung aus Fert.-Termin** — sobald der befuellt ist,
entsteht er von selbst. Ihn auszublenden hiesse, einen Wert wegzuwerfen, den man geschenkt bekommt.
S4 („F7-Ausblenden waere Neubau") erledigt sich damit ebenfalls.

### Zu B3 — **`KO_Termin` wird uebernommen, NICHT gerechnet.**

Der schwerwiegendste der drei Termin-Punkte. IDEAL liefert den Kommissionier-Termin **explizit**
(die Struktur-Kopfzeile zeigt ihn bereits als „KO-Termin"); die Kaskade wuerde ihn stattdessen als
`FE_Termin − 4 AT` schaetzen. **Das ist exakt die Klasse „Heuristik statt Wahrheit", die Design H
gerade abgeschaltet hat** — sie hier wieder einzufuehren waere widersinnig.

**Mapping verbindlich:**
| Anzeige | Quelle |
|---|---|
| Fert.-Termin | `FE_Termin` |
| **Komm.-Termin** | **`KO_Termin` aus `FaHierarchyOrderInfo` — nicht die Kaskade** |
| BG-Termin | Ableitung aus Fert.-Termin (bestehende Logik, unveraendert) |
| Liefertermin | `Verladetermin_Vsl` |

**Beschichtungstermin/`Start_Beschichtung`: RAUS aus dieser Spec** — konsistent zu B1. Er gehoert
zur selben Beschichtungs-Baustelle, wird von drei Controllern gerechnet und steht in keinem
Umfangspunkt. Die Ankuendigung im Kopf und in Antwort 6 ist zu streichen und in die neue
Beschichtungs-Notiz zu uebernehmen. **Das gesamte Beschichtungsthema verlaesst diese Spec
geschlossen.**

Bestaetigung des Mappings am ersten Datenlauf bleibt Pflicht (Sichtvergleich an einem HauptFA).

### Zu B4 — Der Umschaltpunkt bleibt, aber er ist bewusst UNSCHARF.

Der Einwand trifft: Der Sync sieht nur „Ist-Werkbank ≠ Arbeitsbereich" — eine Sage-seitige
Umplanung erzeugt dieselbe Meldung wie eine manuelle Zuweisung. Meine Antwort 2 hat daraus
faelschlich einen **Beweis** gemacht.

**Korrigiert:**
- Die Meldung heisst **„Werkbank weicht vom Quellwert ab"**, nicht „manuell ueberschrieben". Der
  Wortlaut darf nicht behaupten, was er nicht weiss.
- Der Umschaltpunkt auf Variante C ist eine **menschliche Beurteilung nach Sichtung der Meldungen**,
  kein Automatismus.
- **Kein zusaetzliches Feld** fuer den zuletzt materialisierten Quellwert — das waere bereits
  Variante C samt Migration, und die haben wir mit B1 gerade beseitigt. Zeigt die Sichtung, dass
  die Meldungen ueberwiegend Sage-Umplanungen sind, ist **das** der Anlass, C mit ihrem zweiten
  Feld zu bauen.

### Zu B5 — Umfang scharf: **NUR die FA-Liste (`/ProductionOrders`).**

Kein Epic. Begruendung: Es ist genau das Muster, das in diesem Paket zweimal funktioniert hat —
Etappe A baute `ProductionOrders` als Referenz, wurde angesehen, und erst dann replizierte Etappe B
auf die uebrigen fuenf.

- **In-Scope:** K1-Gruppen-Kopfzeile und Werkbank **nur** in `/ProductionOrders`.
- **Out-of-Scope, ausdruecklich benannt:** Leitstand, Tracking, Picking, Arbeitsvorrat, FaCompletion
  zeigen Kunde/Termine je Zeile und bleiben fuer IDEAL **leer**. Das ist eine **bekannte
  Einschraenkung**, kein Versehen — als solche in die Spec und in eine Folge-Notiz.
- Nach der Sichtpruefung der Referenz entscheidet der Mensch, ob und wie repliziert wird.

### Zu S1 — 96 Mails am Tag: uebernommen, mit konkreter Regel.

**Log: bei jedem Lauf. Mail: nur bei Aenderung.** Der Sync merkt sich die Menge der unbekannten
Arbeitsbereiche des letzten Laufs (in-memory genuegt — nach einem Neustart einmal zu viel zu melden
ist harmlos); die Sammelmail geht nur raus, wenn **neue** Werte hinzugekommen sind. Sonst entwertet
sich die Meldung binnen eines Tages selbst, und dann wird auch die eine wichtige uebersehen.

### Zu S5 und H4 — uebernommen.

**S5:** Der Zwei-Lauf-Ablauf aus Antwort 1 gehoert in den Deploy-Abschnitt — mit der **Empfehlung,
die fuenf Arbeitsplaetze vor dem Deploy anzulegen** (Namen sind aus `FaHierarchyNode.Arbeitsbereich`
bekannt), dann greift die Zuweisung sofort und der Zwei-Lauf-Fall tritt gar nicht ein.
**H4:** Antwort 4 beschreibt Export-Verhalten fuer einen Export, den es nicht gibt — der Absatz ist
zu streichen. Die Kunde-Filter-Vorgabe (Join auf `FaHierarchyOrderInfo`, Gruppen einschraenken)
bleibt unveraendert gueltig.

### Zu H1 — mein Fehler, korrigiert.

Das Frontmatter stand auf `Freigegeben`, obwohl die Datei in `entwurf/` liegt und der Review
Nachbesserung verlangt. **Zurueckgesetzt auf `Entwurf`.** Die Freigabe erfolgt erst, wenn B1–B5 im
Spec-Koerper eingearbeitet sind.

## Kritische Pruefung (2026-09-09)

Anwalt des Teufels vor Schranke 1. Code-Belege gegen Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5` @ `22d31ae` (BOM-Bridge v1.36.0 inkl. Klasse-D-Gates
ist dort bereits umgesetzt, `HierarchicalModeGateTests` gruen). Gelesen: Spec + Freigabe-Antworten,
Backlog, [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]] (Design H, QA-Nachweis),
[[2026-08-18-fa-liste-hierarchie-anzeige-spec]] (Z1, sechs Ansichten), Teil-7-Spec, ADR 0003/0005/
0009/0010/0013, fallstricke §5/§9/§10, Glossar, `codebase/services.md`.

**Vorab — was am Code geklaert ist (keine Rueckfrage mehr noetig):**
- Antwort 3 fragt, was „Komm." bedeutet: Es ist **weder K2 noch K3**, sondern ein **berechnetes
  Datum** (`picking-date` = Fert.-Termin minus `KommissionierTage`, `ProductionOrdersController.cs:137-138`,
  Tooltip „Fertigungstermin - N Arbeitstage"). Es faellt aus der Termin-Kaskade heraus, sobald
  Fert.-Termin bekannt ist. „Lack-T" (`coating-part`) ist ein **Icon** aus `HasCoatingParts`/`IsCoatingDone`
  (`_ProductionOrderRow.cshtml:95-104`), kein Datum.
- `ProductionOrder` hat **kein** `string Workplace`, nur FK `ProductionWorkplaceId` + Navigation
  (`ProductionOrder.cs:67-68`) — Antwort-1-Nachtrag stimmt. `ProductionWorkplace.Name` hat **keinen
  Unique-Index** (`ApplicationDbContext.cs:745-756`, `SQL/22_AddProductionWorkplaces.sql`), es gibt
  **keine** `GetByNameAsync`-Methode, **kein** Werkbank-Seed; einziger Anlege-by-Name-Weg ist
  `OseonSyncService.cs:219-230` (AKE).
- `HasCoatingParts`/`IsCoatingDone` liegen **nicht** auf `ProductionOrder`, sondern auf der
  Satelliten-Tabelle `ProductionOrderPickingStatus` (`:41/:44`, ADR 0009). Geschrieben heute nur von
  `CoatingDetectionService.BulkUpdateCoatingFlagAsync` (`:222-274`, `UPDATE` nur bestehender Zeilen)
  und `ProductionOrderPickingStatusRepository.SetCoatingPartsAsync` (`:141-160`, mit
  `IsCoatingDone`-Kaskaden-Reset, Fallstrick #11).
- `FaMaterializationSyncService` laedt `PickingStatus` per `Include` (`:66`), schreibt es aber nie;
  `MaterializationSourceOrder` hat genau drei String-Felder (`FaMaterializationPlanner.cs:7-8`).
- Naechste freie Migrationsnummer im Worktree ist tatsaechlich **91** (`SQL/90_...` ist die hoechste).
- `FaHierarchyOrderInfo.HauptFA` ist ein **nicht-uniquer** Index (`ApplicationDbContext.cs:1040`) —
  1:n bestaetigt. Felder: `KO_Termin`, `FE_Termin`, `Start_Beschichtung`, `Beschichten_Retour`,
  `Neuer_PT_PPS`, `Verladetermin_Vsl`, `Prio`, `ABNr`, `Kunde`, `MontageAbteilung`, `Dienstleister`, `RAL`.
- Die FA-Struktur-Kopfzeile (`Views/FaHierarchy/Index.cshtml:157-164`) zeigt `KO_Termin` als
  **„Kommissionier-Termin"** und `FE_Termin` als „Fertig-Termin". `Verladetermin_Vsl`,
  `Start_Beschichtung`, `Prio` werden dort **nirgends** gerendert.

### BLOCKER

**B1 — `HasCoatingParts`: Antwort 6 widerspricht K3, AK 11 und F3.** Spec-Abschnitt K3 erklaert
Lack-T (`HasCoatingParts`) fuer „zu Recht leer, keine Code-Aenderung"; AK 11 fordert einen
Regressionstest „keine Verhaltensaenderung an `HasCoatingParts`". Antwort 6 (und die BOM-Bridge-Spec,
Design H Z. 447-456, Out-of-Scope Z. 150-152) weisen **genau dieser Spec** die Schreibquelle zu:
`HasCoatingParts` ← „Sub-FA selbst oder ein direktes Kind `Beschichtet`". Dazu kommen drei Folgen,
die im Spec-Koerper fehlen:
(a) `HasCoatingParts` liegt auf `ProductionOrderPickingStatus` — und `PickingStatus` steht in der
app-verwalteten Z1-Liste (`FaMaterializationSyncService.cs:21-22`). Der Sync muesste also eine
**zweite Z1-Ausnahme** neben `Workplace` bekommen (F3 nennt nur eine). (b) Wer legt die
`PickingStatus`-Zeile fuer IDEAL-Auftraege an? AKE erzeugt sie eager im `SageImportService`; die
Materialisierung erzeugt sie **nicht**; `CoatingDetection` updatet nur bestehende Zeilen. Ohne
Eager-Create schreibt der Sync ins Leere. (c) Kippt das Flag auf `false`, muss `IsCoatingDone`
mit zurueckgesetzt werden (Fallstrick #11) — Semantik in der Spec nicht erwaehnt.
**Und:** Antwort 3 nennt als Minimalsatz `Beschichtet` als **neue `ProductionOrder`-Spalte** — aber
**keine der sechs Ansichten zeigt ein rohes `Beschichtet`**; sie zeigen Lack-T (= `HasCoatingParts`)
und Beschicht.-Datum. Nach Antwort 3s eigener Regel („nur angezeigte Felder") gibt es fuer eine neue
Spalte keinen Grund; die Ableitung nach `HasCoatingParts` genuegt. Dann faellt die **Migration
komplett weg** (`migration: true` im Frontmatter waere falsch), sofern B3 keine neue Spalte bringt.
→ *Frage an den Menschen:* (1) K3/AK 11 streichen und `HasCoatingParts`-Ableitung als In-Scope-Punkt
mit eigener AK + Z1-Ausnahme aufnehmen — ja? (2) Rohes `Beschichtet` als eigene Spalte **oder** nur
die Ableitung? (Empfehlung: nur Ableitung, keine Migration.) (3) Eager-Create der `PickingStatus`-
Zeile im Materialisierungs-Sync — ja, oder Ableitung in einem eigenen Schritt nach dem Anlegen?

**B2 — Termine: Antwort 5 widerspricht der Termin-Kaskade, AK 3 und dem Testszenario.** Antwort 5
sagt „BG-Termin vermutlich ohne Entsprechung → ausblenden (F7)". Der Spec-Abschnitt „Termin-Kaskade"
und AK 3 sagen das Gegenteil: BG-Termin wird aus Fert.-Termin **berechnet** und ist gerade der Wert,
der je Werkbank (`OverridePrePickingDays`) **abweicht** — das ist der Kern von AK 3 und des
Testszenarios „Termin-Kaskade". Am Code stimmt die Kaskade (`:137-146`): sobald Fert.-Termin gesetzt
ist, entstehen Komm., BG-Termin und Beschicht. automatisch. Ausblenden waere also nicht F7
(„Spalte ohne Quelle"), sondern Wegwerfen einer vorhandenen Ableitung.
Zweiter Riss: IDEAL liefert **`KO_Termin` explizit** (Struktur-Kopfzeile: „Kommissionier-Termin"),
Antwort 5 erwaehnt ihn nicht — die Kaskade wuerde Komm. stattdessen als `FE_Termin - 4 AT` **errechnen**
und damit vom expliziten Sage-Wert abweichen. Das ist dieselbe Klasse „Heuristik statt expliziter
Wahrheit", die Design H der BOM-Bridge fuer IDEAL gerade abgeschaltet hat.
→ *Frage:* (1) BG-Termin: Kaskade (Spec-Text, AK 3) **oder** ausblenden (Antwort 5)? (2) Komm.:
aus `KO_Termin` (explizit, K1) oder errechnet aus `FE_Termin`? Empfehlung: Komm. ← `KO_Termin`,
BG-Termin ← `KO_Termin - effectivePrePickingDays`, Fert.-Termin ← `FE_Termin`, Liefertermin ←
`Verladetermin_Vsl`; ist `KO_Termin` NULL, Fallback auf die Kaskade und **kennzeichnen**.
(3) Sichtpruefung am ersten Datenlauf bleibt Pflicht — als AK formulieren (siehe S6).

**B3 — Beschichtungstermin aus `Start_Beschichtung`: angekuendigt, aber nirgends im Umfang.** Der
Verzahnungsblock (Kopf) und Antwort 6 sagen: „Beschichtungstermin kommt bei IDEAL aus FAInfos
`Start_Beschichtung`, nicht aus `CoatingDateCalculator`". In-Scope-Liste, `affected_code`,
Termin-Kaskade („Beschicht. = BG-Termin minus `BeschichtungTage`"), AKs und Testszenarien enthalten
davon **nichts** — die Kaskade im Spec-Text wuerde Beschicht. weiterhin errechnen. Zudem rechnen
**drei** Controller den Beschichtungstermin: `ProductionOrdersController` (inline, `:141-146`),
`PickingLeitstandController.cs:155-157` und `FaWorklistController.cs:259-261` (beide ueber
`CoatingDateCalculator.Compute`). Und: `Start_Beschichtung` haengt am **HauptFA** (K1), Beschicht.
wird aber **je Zeile** nur bei `HasCoatingParts` gezeigt — die Regel „Kopfwert auf Zeilen mit Flag"
steht nirgends. Bei mehrdeutigem HauptFA (Kombigeraet) koennen die Varianten **verschiedene**
`Start_Beschichtung`/`Dienstleister`/`RAL` haben — genau der Fall, den
[[2026-08-06-kombinationsgeraete-montageabteilung]] als schwerwiegend beschreibt.
→ *Frage:* Ist der Beschichtungstermin aus `Start_Beschichtung` **in dieser Spec** drin (dann: alle
drei Controller, Regel fuer mehrdeutige HauptFAs = keine Zeilen-Termine + Badge, eigene AK) — oder
ausdruecklich **raus** (dann: Verzahnungsblock/Antwort 6 korrigieren und Folge-Backlog anlegen,
und die Kaskade rechnet Beschicht. bis dahin wie heute)?

**B4 — Umschaltpunkt B→C ist so nicht belegbar.** Antwort 2 macht die Abweichungs-Meldung zum Beweis
fuer manuelle Disposition („schlaegt sie an, ist belegt, dass manuell disponiert wird"). Der Sync
kann aber nur `ProductionWorkplace.Name` (Ist) gegen `Arbeitsbereich` (Quelle) vergleichen. Eine
**Sage-seitige** Aenderung des Arbeitsbereichs an einem laufenden Auftrag erzeugt exakt dieselbe
Abweichung — die Meldung unterscheidet nicht zwischen „Mensch hat umdisponiert" und „Sage hat
umgeplant". `FaHierarchyNode` ist Full-Refresh je Lauf (`FaHierarchySyncService.cs:129-138`), der
vorige Quellwert ist nach dem Lauf weg. Zur echten Unterscheidung braeuchte es den **zuletzt
materialisierten Quellwert** je Zeile (z. B. `ProductionOrder.SourceWorkplaceName`, nvarchar(200)
NULL → Migration; ironischerweise ist das das zweite Feld aus Variante C) — dann gilt: Ist ≠ letzter
Quellwert **und** Quelle = letzter Quellwert → manuell; Quelle ≠ letzter Quellwert → Sage-Aenderung.
→ *Frage:* (a) Quellwert mitspeichern (Migration, praezise Meldung) **oder** (b) Meldung bewusst
unscharf lassen („abweichend — manuell oder Sage-Aenderung") und den Umschaltpunkt nach
menschlicher Sichtung statt automatisch setzen? Beides ist vertretbar; die Spec muss es sagen, weil
AK 9 heute „Log-Eintrag mit altem/neuem Wert" als Beweis fuer B verlangt.

**B5 — Umfang: welche Ansichten?** In-Scope 1 sagt „Gruppen-Kopfzeile der FA-Liste (und **ggf.**
weiterer Ansichten)". `affected_code` nennt nur `ProductionOrdersController`/`Index.cshtml`. Am Code
zeigen aber auch `PickingLeitstand` (Kunde, Werkbank, alle fuenf Termine), `Tracking` (Kunde,
Werkbank, Fertigungstermin), `Picking` (Kunde, Komm.-Termin) und `FaWorklist` (Werkbank, vier
Termine) je Zeile — fuer IDEAL heute alle leer. Werkbank (K2) fuellt sich dort automatisch mit;
Kunde und Termine (K1) **nicht**, solange nur `ProductionOrdersController.MapItem` umgebaut wird.
Ein Anwender sieht dann in der FA-Liste Termine, im Leitstand daneben keine.
Dazu AK 1 vs. AK 3: AK 1 fordert „nicht auf den Sub-FA-Zeilen dupliziert", AK 3 verlangt, dass Zeilen
denselben Fert.-Termin/Liefertermin **zeigen**. Gemeint ist offenbar „nicht in der DB", aber so ist
es nicht formuliert — und fuer Kunde ist offen, ob die leere Zeilenspalte `customer` im hierarchischen
Modus **ausgeblendet** (F7 — dafuer gibt es heute **keinen** Mechanismus, `Hierarchical` blendet keine
Spalte aus, Index.cshtml `:77-102`/`:171-195` identisch in beiden Modi) oder mit dem Kopfwert
befuellt wird.
→ *Frage:* (1) Nur FA-Liste (dann explizit als Out-of-Scope: die fuenf anderen Ansichten zeigen
K1 weiterhin leer, Folge-Backlog) — **oder** alle sechs? Bei „alle sechs" ist das ein Epic-Kandidat
(siehe G1). (2) Zeilen-Darstellung von K1: Termine ja (Kaskade braucht sie), Kunde ja/nein/ausblenden?

### SOLLTE

**S1 — Sammelmeldung „unbekannte Arbeitsbereiche" spammt ohne Zustand.** Antwort 1 will Log **und**
Sammelmail je Lauf. Das Vorbild `SendMissingDigestAsync` meldet nur **neu** Vermisste, weil der
Planner ueber `SageMissingSince` weiss, was schon gemeldet ist (`FaMaterializationPlanner.cs:79-84`).
Fuer unbekannte Arbeitsbereiche gibt es keinen solchen Zustand — bei 15-Minuten-Takt kaemen bis zur
Nachpflege **96 identische Mails pro Tag**. Vorschlag: jeder Lauf schreibt Warning + Counts-Key
`arbeitsbereich_unbekannt` (Anzahl Auftraege) + Liste `Name (n Auftraege)` ins Aktivitaets-Protokoll;
**Mail nur**, wenn sich die Menge der unbekannten Namen gegenueber dem letzten Lauf aendert (einfachster
Zustand: die Namen der letzten Meldung im Singleton-Reader oder als ServiceSetting-Wert halten) —
oder ganz ohne Mail, weil das Protokoll in der UI sichtbar ist. Als Entscheidung in Antwort 1
nachtragen.

**S2 — Name-Match ohne Eindeutigkeit definieren.** Kein Unique-Index auf `ProductionWorkplace.Name`,
keine Lookup-Methode. Festlegen: Vergleich case-insensitiv + getrimmt; bei **mehreren** Treffern
keine Zuweisung + eigener Meldefall (nicht der erste Treffer); neue Methode
`GetByNamesAsync(IEnumerable<string>)` (eine Abfrage je Lauf, kein N+1). `affected_code` Zeile zu
`ProductionWorkplaceRepository` entsprechend praezisieren.

**S3 — Kunde-Filter: Praezisierung statt Join.** Antwort 4 spricht von „Join auf `FaHierarchyOrderInfo`
je HauptFA". Am Code sitzt `filterCustomer` in `BuildLeitstandQuery` (`ProductionOrderRepository.cs:109-110`,
Zeilenebene, vor der Gruppen-Paginierung `:68-73`) und wird **auch** vom `PickingLeitstandController`
(`:181-182`) genutzt — die Reparatur im Repository hilft also zwei Ansichten. Umsetzbar ohne Fan-out
als **Subquery**: `o.OrderNumber IN (SELECT CAST(HauptFA AS nvarchar) FROM FaHierarchyOrderInfos WHERE
Kunde LIKE …)` (Typbruch int↔string beachten, Muster aus der Materialisierung). Ausserdem: der
**Spaltenfilter** `customer` (`ColumnFilterHelper`, serverseitig) hat dasselbe Problem — Antwort 4
sagt „jeder kuenftige Filter", dieser existiert schon. Entweder mitziehen oder Spalte im
hierarchischen Modus ausblenden (haengt an B5).

**S4 — F7-Ausblende-Mechanismus ist Neubau, nicht Konfiguration.** Wo immer die Spec „im hierarchischen
Modus ausgeblendet" sagt (Antwort 5, ggf. B5), braucht es Code: `ColumnDefinitions.ProductionOrders`
**und** das Inline-`column-config`-JSON (fallstricke §10: zwei Registrierungen) plus eine Flag-Logik,
die es heute nicht gibt. In `affected_code` aufnehmen oder die Ausblende-Faelle streichen.

**S5 — Deploy-Abschnitt nachziehen, wie Antwort 1 es selbst verlangt.** Der Zwei-Lauf-Ablauf und die
empfohlene Vorbedingung „fuenf Werkbaenke (`K-02`, `S-01`, `H1-03`, `H2-02`, `H4-04`) **vor** dem
Deploy anlegen" stehen nur in der Antwort, nicht im Deploy-Abschnitt. Ebenso fehlt dort: „nach dem
Deploy einen Sync-Lauf (≤ 15 min) abwarten, dann pruefen" (F2) und der Hinweis, dass die
Werkbank-Namen aus Sage exakt (Gross/Klein, Leerzeichen) uebernommen werden muessen. Ausserdem das
Antwort-1-Relikt: die beiden Aufzaehlungspunkte „String:/Fremdschluessel:" **nach** dem Nachtrag
sind ueberholt und verwirren den Dev-Lauf — streichen.

**S6 — AKs schaerfen.** AK 6 nennt „130 Sub-FAs" — Zahl aendert sich; „alle bereits materialisierten
Zeilen". AK 8 „byte-identisch" — pruefbar als „bestehende Web-/Service-Tests unveraendert gruen +
kein Schreibzugriff auf `ProductionOrders` bei Master `false` (Unit-Test mit Master `false` → Sync
schreibt 0 Zeilen)". Neue AK fuer Antwort 5: „Sichtpruefung: an einem HauptFA stimmen Komm./Fert./
Liefer mit dem Sage-Bild ueberein — Ergebnis mit HauptFA-Nummer im QA-Nachweis". Neue AK fuer die
Kombigeraet-Meldung: Counts-Key-Name (`hauptfa_mehrdeutig`) + Sammelmeldung mit HauptFA-Liste.

**S7 — Mehrdeutig-Darstellung wiederverwenden.** `Views/FaHierarchy/Index.cshtml:151-181` hat das
Muster schon (Badges nur bei eindeutigem Kopf, sonst Untertabelle aller Varianten + Badge
„mehrdeutig (Kombigeraet)"). Als Referenz in den Loesungsentwurf; bei mehrdeutigem HauptFA gilt fuer
die Zeilen-Termine: **keine** (nicht die erste Variante) — explizit hinschreiben.

**S8 — Planner-Zuschnitt.** `MaterializationSourceOrder` bekommt `Arbeitsbereich` (string?) und ein
**bereits abgeleitetes** `HasCoatingParts` (bool) — die Ableitung „selbst oder direktes Kind"
braucht alle Knoten (`VaterFA = SubFA`), die der Service kennt, nicht der reine Planner. So bleibt der
Planner DB-frei und unit-testbar; die Ableitung bekommt einen eigenen reinen Helper + Tests.

### HINWEIS

**H1 — Frontmatter sagt `status: Freigegeben`, Datei liegt in `specs/entwurf/`.** Das HOME-Dashboard
und die Pipeline lesen den Ordner. Nach der Nachbesserung gehoert beides zusammen umgestellt
(Menschen-Geste). Der Verweis im `/review`-Aufruf zeigte schon auf `freigegeben/`.
**Update 2026-09-09:** `status: Freigegeben` + `freigabe_von`/`freigabe_am` sind inhaltlich gesetzt
(alle Blocker B1–B5 entschieden, siehe „ANTWORTEN … (2026-09-09)"). Die Datei liegt bewusst noch in
`specs/entwurf/` — der Mensch verschiebt sie selbst nach `specs/freigegeben/`.

**H2 — Worktree-Wahl.** Frontmatter bindet die Umsetzung an das Buendel
`feature/2026-08-07-ideal-teile-1-5`, das seit v1.31 waechst und mit v1.36.0 auf Schranke 2 wartet.
Jede weitere Etappe dort verschiebt den Merge und vergroessert die UAT-Flaeche. Alternative: Merge
zuerst, dann eigener Worktree aus `main` (so hatte es die BOM-Bridge-Spec vorgesehen, bevor sie doch
im Buendel lief). Bewusst entscheiden, nicht per Default.

**H3 — „Zeitliche Luecke" (Antwort 6) existiert nur bei getrenntem Deploy.** Die Gates sind im
Buendel bereits aktiv (`56e8faa`); wird diese Spec im selben Buendel umgesetzt, gibt es beim Deploy
keine Phase mit leerem `HasCoatingParts`. Bei getrenntem Deploy: Erklaerung auf der Hilfeseite.

**H4 — Antwort 4 „Export nimmt Kopfdaten mit"** beschreibt Verhalten fuer ein Feature, das es nicht
gibt (kein CSV/Excel-Export in der App, 0 Treffer). Streichen oder als „falls spaeter" markieren.

**H5 — Backlog-Verweis falsch.** Der Backlog verweist fuer die Z1-Regel auf
[[2026-07-29-standort-ideal-teil-7-spec]]; Z1 steht in [[2026-08-18-fa-liste-hierarchie-anzeige-spec]]
(§Z1, H1). Fuer den Dev-Lauf die richtige Quelle nennen.

**H6 — F7 ist keine paketweite Regel.** Sie stammt aus der inzwischen superseded Alt-Spec
[[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]] (§F7) und wurde in keine freigegebene Spec
uebernommen. Wer sich darauf beruft, sollte sie hier einmal ausformulieren.

**H7 — Glossar-Luecken**: Arbeitsbereich, Lack-T, Komm., BG-Termin, Kombinationsgeraet, HauptFA fehlen
im Glossar; „Artikelinfo … aus dem BOM-Cache" ist seit v1.36.0 ueberholt. Brain-Pflicht im Dev-Lauf.

**H8 — Datenhoheit ohne ADR.** „Sage fuehrend fuer die Werkbank bei IDEAL, Meldung als Umschaltpunkt"
ist eine Architekturentscheidung ohne ADR (0009 kommt am naechsten). ADR-0014-Kandidat.

**H9 — Antwort-2-Annahme pruefen, nicht glauben.** „Werkbank-Spalte ist fuer IDEAL leer" ist
plausibel (Sync schreibt sie nie), aber der Leitstand kann zuweisen. Vor dem Deploy per SQL belegen:
`SELECT COUNT(*) FROM ProductionOrders WHERE ProductionWorkplaceId IS NOT NULL` bei Master `true`.

**H10 — Antwort 5, Begruendung „erscheint bereits so benannt"**: Die Struktur-Kopfzeile nennt
`FE_Termin` „FE-Termin"/„Fertig-Termin", nicht „Fert.-Termin" — passt inhaltlich, Wortlaut stimmt
nicht. `Neuer_PT_PPS` wird nirgends angezeigt; wer ihn braucht, ist offen (Fachbereich).

### Groesse (G1)

Web: Controller, Repository (+Interface, neue Methode, Kunde-Subquery), ViewModel, `Index.cshtml`,
ggf. `ColumnDefinitions` + column-config, ggf. `PickingStatus`-Schreibweg, ggf. zwei weitere
Controller (B3). Service: `FaMaterializationSyncService`, `Planner`, `FaHierarchySyncService`,
`ProductionWorkplaceRepository`, ggf. Migration 91 (B1/B4). Tests in beiden Suiten, TESTSZENARIEN,
Hilfeseite, zwei `AppVersion.cs`, Changelog ×2, feature-map, ggf. ADR. **~18-25 Dateien, drei
Schichten.** Bei „nur FA-Liste, keine Migration" gerade noch ein Dev-Lauf; bei B3 = ja oder B5 =
„alle sechs" ein **Epic** mit drei Etappen: A K2-Service (Werkbank + `HasCoatingParts` + Meldungen),
B K1-FA-Liste (Kopfzeile, Termine, Kunde-Filter), C uebrige Ansichten/Beschichtungstermin.

### Deploy und Test

`deploy.migration: true` haengt an B1/B4 — nach deren Entscheidung korrigieren. Manuelle
Test-Checkliste ist als Szenario-Liste vorhanden, aber ohne Vorbedingungen (Werkbaenke angelegt?
Master `true`? welcher HauptFA mit Kombigeraet?) und ohne Negativfaelle (unbekannter Arbeitsbereich
mit doppeltem Namen; HauptFA ohne `FaHierarchyOrderInfo`-Zeile; `KO_Termin`/`FE_Termin` NULL) —
fuer `docs/TESTSZENARIEN.md` im Dev-Lauf ausformulieren. Produktiv entstehen **echte Daten**
(Werkbank-Zuweisungen, `HasCoatingParts`-Flags, Log-Eintraege, Mails) ab dem ersten Sync-Lauf nach
dem Deploy — im Deploy-Abschnitt benennen.

**Empfehlung: NACHBESSERUNG NOETIG — B1 (HasCoatingParts vs. K3/AK 11, Migration ja/nein), B2/B3
(Termin-Regeln widersprechen sich, `KO_Termin`/`Start_Beschichtung` ungeklaert) und B5 (Umfang
Ansichten) muessen vor dem Dev-Lauf entschieden sein.**

> **Update 2026-09-09 (nach Fachbereichs-Entscheidung, siehe „ANTWORTEN … (2026-09-09)" unten):**
> B1, B2/B3, B4 und B5 sind entschieden. Die Groessenschaetzung oben („Bei `nur FA-Liste, keine
> Migration` gerade noch ein Dev-Lauf") **gilt weiterhin fuer die Migration** (K2 braucht keine
> Spalte mehr), waechst aber um die HasCoatingParts-/Beschichtungstermin-Arbeit (F5/F6, zweiter
> Controller-Berührungspunkt bleibt aber aus, da View-Scope = nur FA-Liste). Kein Epic — ein
> (groesserer) Dev-Lauf. Migration bleibt `false`.

## ANTWORTEN auf die Kritische Pruefung (2026-09-09)

### Zu B2/B3 — Feld-Mapping final geklaert, inkl. Korrektur einer falschen Code-Annahme.

Die Kritische Pruefung nahm an, `KO_Termin` sei der explizite Kommissionier-Termin (gestuetzt auf
den Tooltip „Kommissionier-Termin" in `Views/FaHierarchy/Index.cshtml:159`) und empfahl, ihn als
Komm.-Termin zu uebernehmen statt ihn zu berechnen. **Das ist fachlich falsch.** Der Fachbereich
bestaetigt: `KO_Termin` ist der **Konstruktions-Termin** (wann die Konstruktion fertig sein muss),
nicht der Kommissionier-Termin. Der Tooltip in `Views/FaHierarchy/Index.cshtml` war schlicht
falsch beschriftet — **behoben** (Zeile 159: „Kommissionier-Termin" → „Konstruktions-Termin"), ein
Ein-Zeilen-Fix, unabhaengig von dieser Spec, aber im selben Review aufgefallen (noch nicht
committet, siehe Deploy-Hinweis unten).

**Damit gilt fuer die Termin-Mapping-Frage:**

| Anzeige | Quelle | Status |
|---|---|---|
| Fert.-Termin | `FE_Termin` | bestaetigt |
| Komm.-Termin | **weiterhin berechnet** (Fert.-Termin − `KommissionierTage`, bestehende Kaskade) | `FaHierarchyOrderInfo` hat fuer IDEAL keinen echten Kommissionier-Termin — `KO_Termin` ist kein Ersatz dafuer |
| BG-Termin | Ableitung aus Komm.-Termin, unveraenderte Kaskade (`OverridePrePickingDays` bleibt werkbank-spezifisch) | bestaetigt |
| Liefertermin | `Verladetermin_Vsl` | bestaetigt |

**Neu aufgenommen (2026-09-09, Fachbereichs-Entscheidung):** `KO_Termin` (Konstruktions-Termin)
wird **zusaetzlich** als eigenes K1-Feld in der Gruppen-Kopfzeile angezeigt — neben Kunde,
Fert.-Termin, Liefertermin, Prio, AB-Nummer, Montage-Abteilung. Wie die uebrigen K1-Felder: nur
Anzeige aus `FaHierarchyOrderInfo`, **keine Materialisierung**, keine neue `ProductionOrder`-Spalte,
keine Migration. Betrifft `ProductionOrderListGroup` (neue Property), die Gruppen-Kopfzeile in
`Views/ProductionOrders/Index.cshtml`, AK 1 und das K1-Testszenario (bereits im Spec-Koerper
nachgezogen). Bei Kombigeraet-Mehrdeutigkeit gilt dieselbe Klartext-plus-Badge-Regel wie fuer die
uebrigen K1-Felder (siehe 1:n-Regel oben).

### Zu B1 — Entschieden: HasCoatingParts + Beschichtungstermin bleiben IN dieser Spec.

Fachbereichs-Entscheidung (2026-09-09): `HasCoatingParts` **kann von der Stueckliste `Beschichtet`
uebernommen werden** — das ist das Feld, das aus IDEAL/Sage tatsaechlich geliefert wird. Der
Beschichtungstermin (`Start_Beschichtung`) wird ebenfalls uebernommen. Das nimmt die vorherige
Zwischen-Antwort („komplett raus") zurueck und stellt die urspruengliche Verzahnungs-Notiz (Kopf
dieser Spec) wieder her — konsistent mit der bereits **freigegebenen** BOM-Bridge-Spec, die dieser
Spec genau diese Ersatzquelle zuweist.

**Damit sind die drei B1-Unterfragen beantwortet:**
1. K3 wird auf Komm.-Status eingeschraenkt, `HasCoatingParts`-Ableitung wird In-Scope-Punkt mit
   eigener AK (12/13) und eigener Z1-Ausnahme (F5) — **ja**, siehe K2/K3 oben.
2. **Nur die Ableitung**, kein rohes `Beschichtet`-Feld auf `ProductionOrder` — Empfehlung der
   Kritischen Pruefung uebernommen, spart die Migration.
3. **Eager-Create der `PickingStatus`-Zeile im Materialisierungs-Sync** (F6) — direkt im
   Anlege-/Update-Zweig, nicht als separater Nachlaufschritt, analog dem `SageImportService`-Muster
   fuer AKE.

### Zu B4 — Entschieden: Meldung bleibt unscharf, kein neues Feld.

Die Abweichungsmeldung „Werkbank weicht vom Quellwert ab" reicht; der Wechsel zu Variante C bleibt
eine menschliche Entscheidung nach Log-Sichtung. Kein `SourceWorkplaceName`, keine zusaetzliche
Migration — Option (b) aus der Kritischen Pruefung.

### Zu B5 — Entschieden: Nur FA-Liste, Kunde-Filter (Freitext + Spaltenfilter) per Join.

Umfang bleibt auf `/ProductionOrders` beschraenkt (Referenz-Etappe, wie im Paket zweimal etabliert);
die fuenf uebrigen Ansichten zeigen K1 fuer IDEAL weiterhin leer — bekannte Einschraenkung, eigene
Folge-Notiz. Der bestehende serverseitige Kunde-**Spaltenfilter** (nicht nur der Freitextfilter,
S3) bekommt dieselbe `FaHierarchyOrderInfo`-Join-Behandlung — kein neuer Ausblende-Mechanismus (S4
entfaellt damit, F7 wird nicht gebraucht).

**Damit sind B1, B2/B3, B4 und B5 vollstaendig entschieden.** Diese Spec ist inhaltlich
freigabefaehig; offen bleibt nur das formale Verschieben nach `specs/freigegeben/` inkl.
`status: Freigegeben` und `freigabe_von`/`freigabe_am` (H1) — das ist ein bewusster, expliziter
Schritt des Menschen, kein Automatismus dieses Reviews.
