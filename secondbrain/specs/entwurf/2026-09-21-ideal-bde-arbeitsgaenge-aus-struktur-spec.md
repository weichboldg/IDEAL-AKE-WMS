---
type: spec
title: "IDEAL: BDE-Arbeitsgaenge (WorkOperation) aus der Struktur + Werkbank-Routing aus Sage"
slug: 2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec
status: Entwurf
created: 2026-09-21
updated: 2026-09-21
source_backlog: "[[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur]]"
supersedes: "[[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]]"
depends_on: "[[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Models/ProductionWorkplace.cs — NEUES Feld `ArbeitsschrittCode` (string?, MaxLength 20), Sage-gefuehrt (Baustein a)"
  - "IdealAkeWms/Migrations/*_AddProductionWorkplaceArbeitsschrittCode.cs (NEU)"
  - "SQL/93_AddProductionWorkplaceArbeitsschrittCode.sql (NEU, COL_LENGTH-Guard, Nummer vorlaeufig — siehe Offene Rueckfrage 7)"
  - "SQL/00_FreshInstall.sql (Schema-Objekt + MigrationId, beide Stellen)"
  - "IDEALAKEWMSService/Services/ProductionWorkplaceCodeSyncService.cs (NEU) + IProductionWorkplaceCodeSyncService.cs (NEU) — Baustein a, Muster ADR 0014 (Sage fuehrend, Melde-statt-Anlege-Prinzip), aber umgekehrte Match-Richtung (Name -> Code statt Code -> Name)"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs — neuer Key `Sync:ProductionWorkplaceCodeSyncEnabled` (Baustein a)"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs — neue Konstante `ProductionWorkplaceCodeSync` (Baustein a)"
  - "IDEALAKEWMSService/Workers/SyncWorker.cs — neuer Gate-Block Baustein a (unabhaengig von FA-Hierarchie, kein FK-Bezug); UMBAU des bestehenden Blocks 'FA-Arbeitsgang-Erkennung (Struktur)' (Baustein b, Zielservice + Key umbenannt)"
  - "IDEALAKEWMSService/Program.cs — DI-Registrierung Baustein a (neu) + Baustein b (umbenannt)"
  - "IdealAkeWms/Views/ProductionWorkplaces/Index.cshtml, Edit.cshtml, Create.cshtml — neues, read-only dargestelltes Feld (Sage fuehrend, analog ADR-0014-Warnhinweis)"
  - "IDEALAKEWMSService/Services/FaWorkStepStructureDetectionService.cs -> UMBENANNT + UMGEBAUT zu WorkOperationStructureDetectionService.cs (Baustein b, 2c-Umbau: Ziel WorkOperation statt FaWorkStep, Katalog ProductionWorkplace.ArbeitsschrittCode statt WorkStep.Code)"
  - "IDEALAKEWMSService/Services/IFaWorkStepStructureDetectionService.cs -> UMBENANNT zu IWorkOperationStructureDetectionService.cs"
  - "IDEALAKEWMSService/Common/IUnknownWorkStepTokenState.cs -> UMBENANNT zu IUnknownArbeitsschrittTokenState.cs (gleiches HashSet-Singleton-Muster, nur Umbenennung fuer Konsistenz mit dem neuen Match-Ziel)"
  - "IDEALAKEWMSService.Tests/Services/FaWorkStepStructureDetectionServiceTests.cs -> UMBENANNT + angepasst auf WorkOperation-Zielmodell"
  - "IdealAkeWms/Models/FaWorkStep.cs — die in v1.41.0 vorgesehene Ergaenzung `FaWorkStepSources.Struktur` ENTFAELLT ersatzlos (FaWorkStep bleibt fuer IDEAL leer; AKE-Quellen `Sync`/`Manual` unveraendert)"
  - "IdealAkeWms/Services/BdeScanResolver.cs — voraussichtlich UNVERAENDERT (Design-Empfehlung F, siehe unten); Verifikations-AK statt Code-Aenderung"
  - "IdealAkeWms/Services/BdeDefaultWorkOperationService.cs — Koexistenz mit echten WorkOperations (Baustein c, abhaengig von Offener Rueckfrage 6)"
  - "IdealAkeWms.Tests/Services/BdeScanResolverTests.cs — Regressionslauf + ggf. neue Faelle mit echten, struktur-abgeleiteten WorkOperations"
  - "docs/TESTSZENARIEN.md — neues Kapitel (naechste freie Nummer TS-77, Stand TS-76 im Worktree) + TS-66-Ergaenzung (BDE-Disambiguierung mit echten AGs) + TS-76 als 'superseded, siehe TS-77' kennzeichnen"
  - "secondbrain/tests/testszenarien-index.md"
open_questions:
  - "Routing-Design: `WorkOperation.ProductionWorkplaceId` bei der Anlage (Baustein b) direkt setzen (empfohlen) oder Live-Kuerzel-Schnittmenge im Scan-Resolver (Baustein c) berechnen?"
  - "Varianten-Regel: Struktur-Basis-Kuerzel (`KA`) exakt matchen, oder Praefix-/Gruppen-Match inkl. Sage-Varianten (`KA2`/`KA4` etc.)?"
  - "Die 5 Kuerzel ohne Werkbank (MO/ZS/LOe/PR/BE): melden, Werkbank in Sage nachtragen, oder aus dem Scope ausschliessen?"
  - "Sage-Quelle fuer Baustein a: genaue Tabelle/View der Arbeitsplatz-Stammdaten (`USER_ArbeitsSchritt`) + eigener Sync-Schritt (kein bestehender Workplace-Sync vorhanden, siehe Recherche) bestaetigen?"
  - "Kuerzel->Name-Katalog: kommt aus `ProductionWorkplace.Name` (Baustein a) — kein separater WorkOperation-Katalog noetig. Bestaetigen?"
  - "Koexistenz `BdeDefaultWorkOperationService`: bei IDEAL abschalten/ersetzen sobald echte WorkOperations vorliegen, insbesondere im NurFA-Modus (der heute IMMER den Default erzeugt)?"
  - "Migrationsnummer-Koordination: SQL/93 vorgeschlagen — SQL/92 ist von [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] im selben Worktree belegt (Status Freigegeben, Stand 2026-09-21). Reihenfolge im Dev-Lauf verifizieren."
  - "Umfang/Schnitt: Epic mit 3 Etappen (a->b->c) im bestehenden Buendel-Worktree, oder 3 getrennte, einzeln mergbare Specs? Siehe 'Reihenfolge/Einordnung'."
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

> **Groessen-Hinweis vorab:** Diese Spec deckt fachlich EIN Ziel ab (Sub-FA-Arbeitsgaenge am
> BDE-Terminal buchbar), technisch aber DREI klar getrennte Bausteine mit jeweils eigenem
> Schema-/Service-/UI-Anteil. Das ist fuer einen einzelnen Dev-Lauf voraussichtlich zu gross — siehe
> „Reihenfolge/Einordnung" und Offene Rueckfrage 8 fuer die empfohlene Aufteilung.

## Ziel / Nutzen (das Warum)

Am BDE-Terminal sieht ein IDEAL-Werker heute pro Sub-FA nur einen **generischen Sammel-AG**
(`BdeDefaultWorkOperationService`, `OperationNumber "01"`, Name = `ProductionWorkplace.BdeDefaultArbeitsgang`)
statt der echten Arbeitsgaenge (KA/LS/SW/…) — weil IDEAL kein OSEON hat und daher nie echte
`WorkOperation`-Zeilen aus einem Fremdsystem-Sync bekommt.

Die Struktur liefert die echten Arbeitsschritte je Sub-FA bereits mit (`FaHierarchyNode.Arbeitsschritte`).
[[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]] (v1.41.0, Testbereit) hat diese Ableitung bereits
gebaut — aber gegen **`FaWorkStep`** (die FA-Abarbeitungsliste), nicht gegen `WorkOperation` (die
Tabelle, gegen die das BDE-Terminal tatsaechlich bucht: `BdeApiController`, `BdeScanResolver`,
`BdeBooking`). Es gibt keinen Fremdschluessel zwischen beiden Tabellen — `FaWorkStep`-Zeilen erscheinen
am Terminal **nie**. v1.41.0 zielt fuer den eigentlichen Zweck (BDE-Rueckmeldung) auf die falsche
Tabelle.

Zweiter Befund, der diese Spec erst sinnvoll macht: In den Sage-Arbeitsplatz-Stammdaten
(`USER_ArbeitsSchritt`) traegt jede Werkbank genau ihr Kuerzel, und die Arbeitsgang-**Namen** sind die
Werkbank-Bezeichnungen (`KA` = „Kanterei W1", `SW` = „Schweißerei W1" …). **Ein Arbeitsgang IST bei
IDEAL faktisch eine Werkbank.** Die Werkbank↔Arbeitsgang-Zuordnung existiert damit bereits in Sage —
keine neue manuelle Stammdatenpflege noetig, Sage bleibt fuehrend (analog
[[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]).

Diese Spec baut die Struktur-Ableitung von `FaWorkStep` auf `WorkOperation` um (2c-Umbau, kein
Zweitbau) und ergaenzt die fehlende Werkbank↔Kuerzel-Zuordnung aus Sage, damit der Werker am Terminal
tatsaechlich den passenden Arbeitsgang seines Sub-FA sieht. **v1.41.0 wird durch diese Spec
fachlich ersetzt** (superseded) — die Umsetzung dieser Spec ersetzt v1.41.0s Zielservice, bevor
v1.41.0 je in Produktion geht.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

- **Baustein (a):** `ProductionWorkplace` bekommt ein Kuerzel-Feld (`ArbeitsschrittCode`), befuellt
  durch einen neuen, eigenstaendigen Sage-Sync (Sage fuehrend, Melde-statt-Anlege-Prinzip wie ADR 0014).
- **Baustein (b):** 2c-Umbau der bestehenden Struktur-Ableitung (`FaWorkStepStructureDetectionService`
  -> `WorkOperationStructureDetectionService`): Ziel-Tabelle `WorkOperation` statt `FaWorkStep`,
  Match-Katalog `ProductionWorkplace.ArbeitsschrittCode` statt `WorkStep.Code`.
- **Baustein (c):** BDE-Terminal zeigt/bucht die neu entstehenden, echten `WorkOperation`-Zeilen.
  Nach Design-Empfehlung F (siehe unten) voraussichtlich **kein** Code-Eingriff in
  `BdeScanResolver`/`GetAvailableOperations`, sondern eine Verifikations-AK plus die Koexistenz-
  Entscheidung fuer `BdeDefaultWorkOperationService`.
- Testszenarien inkl. Negativfaellen (unbekanntes Kuerzel ohne Werkbank, leere Schnittmenge, Fertig-/
  Storno-Filter, Koexistenz Default-AG, TS-66-Anpassung).
- Brain-Pflichten: v1.41.0 als superseded kennzeichnen (Hauptcheckout, nicht durch diese Spec selbst
  ausgefuehrt, sondern als Aufgabe fuer Umsetzung/Brain-Update vermerkt).

**Out-of-Scope**

- Der **AKE-Pfad** (`FaWorkStepDetectionService`, `FaWorkStep`, `WorkStep`-Katalog `VA/VE/VK/VL/VT`)
  bleibt **vollstaendig unveraendert**.
- Kombinationsgeraete-Grenze am BDE-Terminal (bereits bekannte, akzeptierte Einschraenkung aus
  [[2026-07-29-standort-ideal-teil-8-spec]]) — unveraendert.
- Pflege der Sage-Arbeitsplatz-Stammdaten selbst (die 5 Kuerzel ohne Werkbank nachtragen) —
  Fachbereichs-/Sage-Arbeit, kein Code (siehe Offene Rueckfrage 3).
- Aenderungen an `FaHierarchyNode`, `FaHierarchySyncService`, `FaMaterializationSyncService` oder der
  Materialisierung selbst — alle drei Bausteine lesen nur.
- Aenderungen an `FaMaterializationSyncService`s bestehender Werkbank-Ableitung
  (`ProductionOrder.ProductionWorkplaceId` aus `Arbeitsbereich`, ADR 0014) — das ist eine andere,
  bereits produktiv entschiedene Zuordnung (physischer Standort des Bauteils) und bleibt unberuehrt.
  Diese Spec fuehrt eine **zweite, unabhaengige** Kuerzel-Zuordnung ein (Arbeitsgang-Routing), die
  **nicht** mit `Arbeitsbereich` verwechselt werden darf (siehe Anhang [[sage-views-ideal]]:
  `Arbeitsbereich` stammt aus `USER_OSAbteilung`, ist positionsbezogen; `Arbeitsschritte`/das neue
  Kuerzel-Feld sind ein eigenes Vokabular aus `USER_ArbeitsSchritt`).

## Fachliche Anforderungen

**Bereits entschieden (Schranke-1-Vorlage aus dem Brainstorming, nicht erneut zur Diskussion):**

1. **Ziel-Tabelle `WorkOperation`**, nicht `FaWorkStep`. `FaWorkStep` bleibt fuer den AKE-Pfad
   unveraendert.
2. **Werkbank-Routing aus Sage `USER_ArbeitsSchritt`**, nicht manuell gepflegt.
3. **2c-Umbau statt Zweitbau** — dieselbe Service-Klasse (umbenannt), derselbe Buendel-Worktree,
   v1.41.0 wird ersetzt.

**Token -> Werkbank-Zuordnung (2026-09-21, vom Menschen gegen die Sage-Arbeitsplatz-Liste
abgeglichen — massgeblich, nicht neu zu erheben):**

17 real gemeldete Kuerzel aus dem ersten Struktur-Lauf. **12 treffen eine Werkbank**
(`USER_ArbeitsSchritt` -> `Bezeichnung1`):

| Kuerzel | Werkbank |
|---|---|
| KA | Kanterei W1 |
| LS | Stanzerei W1 |
| SW | Schweißerei W1 |
| EG | Flächen entgraten |
| SÄ | Schäumerei W1 (EG) |
| VM | Elektrofertigung W1 (OG) |
| PL | Punkten Ladenbau W1 |
| PG | Punkten Gehäusebau W1 |
| BR | Berohren W1 |
| EM | Elektromontage W1 |
| ISO | Isolierung W2 |
| SL | Schleiferei W1 |

**5 Kuerzel ohne Werkbank in der Liste:** `MO`, `ZS`, `LÖ`, `PR`, `BE` — heute nicht buchbar
(Werkbank fehlt oder Liste unvollstaendig), Melde-/Zuordnungsthema (Offene Rueckfrage 3).

Quelle: vollstaendige Arbeitsplatz-Stammdaten (Spalte `USER_ArbeitsSchritt`), 2026-09-21 vom Menschen
bereitgestellt — Sync-Quelle fuer Baustein (a).

**Varianten-Problem (Offene Rueckfrage 2):** Die Struktur (`FaHierarchyNode.Arbeitsschritte`) nutzt
laut v1.41.0-Freigabe-Antwort Basis-Kuerzel (`KA`). Sage kennt daneben Werkbank-**Varianten**
(`KA2`/`KA4`, `SW1`/`SW2`/`SWB`/`SWL`, `SÄ2`/`SÄE`/`SÄO`, `VM1`/`VM2`, `PG2`, `PL2`, `EM2`, `SLH`).
Ob die Struktur ausschliesslich Basis-Kuerzel liefert oder auch Varianten, ist am Code nicht
abschliessend zu verifizieren (das haengt an den tatsaechlichen Sage-Daten) — daher offene Rueckfrage,
mit der Tendenz aus dem Backlog: exakter Basis-Kuerzel-Match = die scan-relevante Haupt-Werkbank.

## Technischer Loesungsentwurf

Referenzmuster: [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]] (`ISyncLogger` letzter
Ctor-Parameter, deutsche Counts-Keys), [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]
(Sage fuehrend + Melde-statt-Anlege-Prinzip), ADR 0008 (ServiceSettings-Katalog). Betroffene Module:
[[services]] (Sync-Services, `SyncWorker`), [[datenmodell]] (`ProductionWorkplace`, `WorkOperation`).

### Baustein (a) — `ProductionWorkplace.ArbeitsschrittCode` + eigener Sage-Sync

**Modell:** `ProductionWorkplace` bekommt `[MaxLength(20)] public string? ArbeitsschrittCode { get; set; }`
(Display „Arbeitsschritt-Kuerzel (Sage)"). Kein Unique-Index — analog zur bestehenden Entscheidung bei
`ProductionWorkplace.Name` (ADR 0014, S. „Betrachtete Optionen"): Ambiguitaet wird **gemeldet**, nicht
per DB-Constraint verhindert, weil ein Sync-Lauf besser erklaeren kann, was mehrdeutig ist, als eine
Exception es koennte.

**Sync, neuer eigenstaendiger Service (`ProductionWorkplaceCodeSyncService`):** Es existiert heute
**kein** bestehender Sync-Schritt, der `ProductionWorkplace`-Stammdaten aus Sage befuellt (verifiziert
per Grep — `ProductionWorkplace` wird im Service-Projekt nur **gelesen**, z. B. von
`FaMaterializationSyncService`, `OseonSyncService`, `BdeAutoPauseService`). Ein neuer, eigener Service
ist daher die einzige Option, keine „Erweiterung eines Bestandssyncs" (Offene Rueckfrage 4 fragt nur
noch nach der genauen Sage-Quelle, nicht mehr nach dem Ob).

Ablauf, gespiegelt an `FaMaterializationSyncService`s Werkbank-Ableitung (ADR 0014), aber in
umgekehrter Match-Richtung (dort: Position -> `Arbeitsbereich` -> `ProductionWorkplace.Name`; hier:
Sage-Arbeitsplatz-Stammsatz -> `Bezeichnung1` -> `ProductionWorkplace.Name` -> Kuerzel schreiben):

1. Sage-Arbeitsplatz-Stammdaten lesen (genaue Tabelle/View: Offene Rueckfrage 4).
2. Je Zeile: `ProductionWorkplace` mit exakt (case-insensitiv, getrimmt) passendem `Name` suchen.
   - **Kein Treffer** -> melden, nichts schreiben (keine Werkbank automatisch anlegen — exakt ADR 0014).
   - **Mehrdeutig** (zwei Werkbaenke mit demselben Namen) -> melden, nichts schreiben.
   - **Ein Treffer**, Kuerzel weicht vom Bestandswert ab -> **schreiben** (Sage fuehrend) UND melden
     (alter/neuer Wert), exakt wie ADR 0014s Umgang mit `ProductionWorkplaceId`-Abweichungen.
3. Sammelmeldung + Sammelmail nur bei Aenderung der Menge unbekannter/mehrdeutiger Namen (S1-Muster,
   analog `IUnknownWorkplaceState`).
4. Eigener `SyncLogServices`-Eintrag (`ProductionWorkplaceCodeSync`), eigener Toggle
   (`Sync:ProductionWorkplaceCodeSyncEnabled`, Default `false`).

**Reihenfolge im `SyncWorker`:** kein FK-Bezug zu FA-Hierarchie/-Materialisierung — der Block kann an
beliebiger Stelle stehen, sollte aber **vor** Baustein (b) laufen, damit der Katalog beim
WorkOperation-Aufbau moeglichst aktuell ist (sonst ein Tag Verzoegerung, kein Fehler — Nur-hinzufuegen-
Semantik faengt das beim naechsten Lauf ohnehin ab).

**UI:** `/ProductionWorkplaces` (Index/Edit/Create) zeigt das Feld **read-only** an (Sage fuehrend —
eine manuelle Eingabe wuerde beim naechsten Lauf ueberschrieben, derselbe Fallstrick wie ADR 0014s
Konsequenz „ueberlebt hoechstens 15 Minuten"; das gehoert auf die Hilfeseite). Bestehende Liste bleibt
Listen-View-Pattern-konform (ADR 0005): die neue Spalte in `Index.cshtml` bekommt einen Spaltenfilter
wie die uebrigen Spalten der Tabelle, keine Sonderbehandlung.

### Baustein (b) — 2c-Umbau: `WorkOperationStructureDetectionService`

Der bestehende `FaWorkStepStructureDetectionService` (v1.41.0, Design D des Ursprungs-Specs) wird
**umgebaut, nicht neu gebaut** — Ctor-Signatur, Full-Refresh-Cache, DirectChildren-Scope-Logik
(eigene Zeile + direkte Kinder aus `childrenByParent`), Nur-hinzufuegen-Semantik, Fertig-/Storno-Filter
und Melde-statt-Anlege-Prinzip bleiben **strukturell identisch** — nur Zieltabelle und Katalog
wechseln:

| | v1.41.0 (FaWorkStep, wird ersetzt) | Diese Spec (WorkOperation) |
|---|---|---|
| Ziel-Tabelle | `FaWorkStep` | `WorkOperation` |
| Match-Katalog | `WorkStep.Code` | `ProductionWorkplace.ArbeitsschrittCode` (Baustein a) |
| Neu angelegtes Feld „Name" | `WorkStep.Name` (referenziert) | `ProductionWorkplace.Name` (kopiert in `WorkOperation.Name`) |
| Eindeutigkeits-Schluessel je Sub-FA | `(ProductionOrderId, WorkStepId)` | `(ProductionOrderId, OperationNumber)` |
| Fremdschluessel-Zwang | Pflicht-FK auf `WorkStep` (Zwei-Lauf-Ablauf) | **kein FK-Zwang** — `WorkOperation.OperationNumber`/`Name` sind Plain-Strings, kein Katalog-FK |

**Wichtige Vereinfachung gegenueber v1.41.0:** `WorkOperation` hat **keinen** Fremdschluessel auf
einen Arbeitsgang-Katalog (verifiziert `Models/WorkOperation.cs`: `OperationNumber`/`Name` sind reine
Strings). Der bisherige „Zwei-Lauf-Ablauf" (erst Katalogeintrag anlegen, dann Ableitung) entfaellt
damit fuer Baustein (b) **strukturell** — es gibt nichts Zweites zu pflegen, sobald Baustein (a) den
Namen kennt. Ein Token bleibt weiterhin „unbekannt" (gemeldet, kein Insert), solange **kein**
`ProductionWorkplace.ArbeitsschrittCode` dazu existiert — der Zwei-Lauf-Gedanke lebt weiter, nur ist
der Katalog jetzt `ProductionWorkplace` (Baustein a) statt `WorkStep` (Offene Rueckfrage 5 bittet um
Bestaetigung dieser Vereinfachung).

**Neu angelegte `WorkOperation`-Felder je Kandidat (Token, Sub-FA):**

- `ProductionOrderId` — wie v1.41.0, gebuendelte Abfrage ueber `SubOrderNumber`, gleicher Fertig-/
  Storno-Filter (`!IsDone && !IsCancelled && !(PickingStatus?.IsDonePicking == true)`).
- `OperationNumber` = das Kuerzel selbst (z. B. `"KA"`) — kurz, menschenlesbar, kollisionsfrei mit dem
  Default-AG (der hartcodiert `"01"` verwendet, `BdeDefaultWorkOperationService.cs:34`).
- `Name` = `ProductionWorkplace.Name` des per Kuerzel gematchten Datensatzes (z. B. „Kanterei W1").
- `ProductionWorkplaceId` = **Id** desselben Datensatzes — siehe Design-Empfehlung F unten
  (Offene Rueckfrage 1).
- `Sequence` = Position des Tokens innerhalb der Arbeitsschritte-Zeichenkette der Quelle (0-basiert,
  Sage-Reihenfolge bleibt erhalten); Fallback `1` falls nicht ermittelbar. Beeinflusst nur die
  Sortierung mehrerer Kandidaten am Terminal (`BdeScanResolver`/`GetOpenByWorkplaceIdAsync` sortieren
  `.ThenBy(Sequence)`), kein hartes Kriterium.
- `IsReportable = true` (treibt BDE-Buchungen, analog `BdeDefaultWorkOperationService`).
- `IsExternalSystem = false`, `IsReported = false`.
- `CreatedAt/CreatedBy/CreatedByWindows = "WorkOperationStructureDetection"` (Service-Name als Autor,
  ADR 0003).

**Unbekannte Kuerzel** (kein `ProductionWorkplace.ArbeitsschrittCode`-Treffer): melden, nicht anlegen —
identisches Sammelmeldungs-/Sammelmail-Muster wie v1.41.0 (`IUnknownArbeitsschrittTokenState`,
S1-Muster: Mail nur bei Aenderung der Token-**Menge**).

**Umbenennung (Konsistenz statt Restbestand):** `FaWorkStepStructureDetectionService` ->
`WorkOperationStructureDetectionService` (+ Interface, Tests, `IUnknownWorkStepTokenState` ->
`IUnknownArbeitsschrittTokenState`, `ServiceSettings`-Key, `SyncLogServices`-Konstante). Begruendung:
Ein Service, der `WorkOperation` befuellt, aber „FaWorkStep" im Namen traegt, ist eine irrefuehrende
Altlast fuer jeden kuenftigen Leser (Sprachregel: Code-Namen sollen die Wahrheit sagen). Risiko gering:
keiner der umbenannten Keys/Konstanten ist bisher in einer produktiven Datenbank gesetzt/geseedet
(v1.41.0 ist nur Testbereit, nie deployt) — kein Migrations-/Datenverlust-Thema, reines Rename.

**`FaWorkStep.Source = "Struktur"`-Ergaenzung aus v1.41.0 entfaellt ersatzlos** — IDEAL-Sub-FAs
bekommen ihre Arbeitsgaenge kuenftig ausschliesslich ueber `WorkOperation`, nicht mehr zusaetzlich
ueber `FaWorkStep`. `FaWorkStep` bleibt fuer IDEAL dauerhaft leer (kein Fehler, einfach ungenutzt fuer
diesen Standort), fuer AKE unveraendert aktiv.

### Baustein (c) — BDE-Terminal: Design-Empfehlung F statt Live-Schnittmenge

Der Backlog skizziert Baustein (c) als Umbau von `BdeScanResolver`/`GetAvailableOperations` auf eine
**zur Scan-Zeit berechnete** Kuerzel-Schnittmenge (Werkbank-Kuerzel × Sub-FA-Kuerzel-Menge). Die
Code-Recherche zeigt einen einfacheren, bereits vorhandenen Weg:

**Befund:** `BdeScanResolver.BuildNormalCandidatesAsync` UND `WorkOperationRepository.GetOpenByWorkplaceIdAsync`
UND `BdeApiController.GetAvailableOperations` (Normal-Modus) filtern **bereits heute** ausschliesslich
ueber `WorkOperation.ProductionWorkplaceId == workplaceId` (verifiziert, je eine Fundstelle). Setzt
Baustein (b) `ProductionWorkplaceId` beim Anlegen direkt auf die Id der gematchten Werkbank (siehe
oben), routen **alle drei Stellen automatisch korrekt** — ohne eine einzige Code-Aenderung an diesem
Pfad. Die „Kuerzel-Schnittmenge" aus dem Backlog wird dann **einmalig bei der Anlage** aufgeloest
(Baustein b) statt **bei jedem Scan neu** (Baustein c) — dieselbe fachliche Schnittmenge, aber als
Fremdschluessel materialisiert statt als Laufzeit-Join. Ponytail-Sprosse 2 („gibt es das schon?"):
Ja — der Filter existiert, nur die Daten fehlten bisher.

**Konsequenz:** `BdeScanResolver` bleibt voraussichtlich **unveraendert**. Aus dem Backlog-„Baustein
(c)" wird eine **Verifikations-Akzeptanzkriterium** (WorkOperations aus Baustein b erscheinen am
Terminal der richtigen Werkbank, ohne Resolver-Aenderung) statt eines Umbaus. Einzige echte offene
Code-Frage bleibt die **Koexistenz mit `BdeDefaultWorkOperationService`** (Offene Rueckfrage 6):

- **NurFA-Modus** bucht heute *immer* ueber `FindOrCreateDefaultAsync` (generischer „01"-AG) —
  `StartProductionForOrder` kennt die echten, struktur-abgeleiteten `WorkOperation`s des Sub-FA gar
  nicht. Ob der NurFA-Modus fuer IDEAL kuenftig die echten AGs anbieten soll (Modus-Wechsel-Frage,
  nicht diese Spec allein) oder der generische Default bewusst bestehen bleibt (z. B. weil der
  Default-Name organisch mit einem echten Werkbank-Namen kollidieren *koennte*, siehe `Name`-basierter
  Existenz-Check in `FindOrCreateDefaultAsync`), ist eine fachliche Entscheidung.
- **Normal-Modus** zeigt/bucht ab Baustein (b) automatisch die echten AGs — der Default-Service wird
  dort gar nicht aufgerufen (er haengt ausschliesslich an `StartProductionForOrder`, NurFA-Pfad).

Diese Empfehlung ist eine **Abweichung von der Backlog-Formulierung** ( „Terminal zeigt die
Schnittmenge") und wird daher ausdruecklich als Offene Rueckfrage 1 zur Bestaetigung vorgelegt, nicht
stillschweigend umgesetzt.

## Migrations-/SQL-Auswirkungen

**Genau eine Migration, aus Baustein (a):** neues nullable Feld
`ProductionWorkplace.ArbeitsschrittCode` (`NVARCHAR(20) NULL`).

- Model -> `dotnet ef migrations add AddProductionWorkplaceArbeitsschrittCode --project IdealAkeWms`
  -> `SQL/93_AddProductionWorkplaceArbeitsschrittCode.sql` mit `COL_LENGTH`-Guard (ALTER TABLE ... ADD,
  analog `SQL/87_AddUserDefaultFilterBomDescription1.sql`), DDL-Batch + separater
  `__EFMigrationsHistory`-Insert-Batch (ADR 0004).
- `SQL/00_FreshInstall.sql` an **beiden** Stellen (Schema-Objekt + `MigrationId`).
- **Nummern-Kollisionsgefahr:** `SQL/92_AddUserDefaultFilterBomKommissionierziel.sql` ist bereits von
  [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] im **selben** Worktree/Branch belegt
  (Status `Freigegeben`, noch nicht umgesetzt, Stand 2026-09-21). `93` ist der naechste freie Wert nach
  dem aktuellen Stand (`91_AddArticleMatchcode.sql` + das reservierte `92`) — im Dev-Lauf gegen den
  dann tatsaechlichen Worktree-Stand verifizieren (Offene Rueckfrage 7), **nicht** blind `92` vergeben.
- **Nicht daten-destruktiv** (neue nullable Spalte, kein Datenverlust, kein Backup-Zwang ueber das
  ueblichen Mass hinaus).

**Bausteine (b) und (c) bringen keine Migration:** `WorkOperation` und `FaWorkStep` sind bestehende
Tabellen; Baustein (b) schreibt nur neue Zeilen mit vorhandenen Spalten, Baustein (c) aendert
voraussichtlich keinen Code. Die `ServiceSettings`-Keys (neu bzw. umbenannt) sind reine
Katalog-/Laufzeit-Konfiguration (ADR 0008, DB-first) — kein Schema-Impact, aber der
`ServiceSettingDefinitions`-Drift-Guard-Test muss den neuen **und** den umbenannten Key kennen.

## Audit-Feld-Auswirkungen

- `ProductionWorkplace` erbt `AuditableEntity`. Der neue Sync (Baustein a) setzt bei jeder
  Aktualisierung `ModifiedAt/ModifiedBy/ModifiedByWindows = "ProductionWorkplaceCodeSync"` (Service-
  Name als Autor, ADR 0003) — **nur** wenn tatsaechlich geschrieben wird (kein Touch bei unveraendertem
  Kuerzel).
- `WorkOperation` erbt `AuditableEntity`. Baustein (b) setzt bei jeder neu angelegten Zeile
  `CreatedAt/CreatedBy/CreatedByWindows = "WorkOperationStructureDetection"` (analog v1.41.0-Vorbild).
  Der Service **aktualisiert nie** bestehende `WorkOperation`-Zeilen (reine Nur-hinzufuegen-Semantik) —
  `ModifiedAt/ModifiedBy` sind fuer diesen Pfad nicht relevant.
- Baustein (c) fuehrt keine neue Entitaet ein (Design-Empfehlung F) — falls die Koexistenz-Entscheidung
  (Offene Rueckfrage 6) doch eine neue Statuszeile braucht, erbt sie `AuditableEntity`.

## Betroffene Rollen / Zugriffsfilter

Keine Aenderung. Baustein (a)/(b) laufen ausschliesslich im Hintergrund (Windows-Service, kein neuer
Web-Endpoint). `/ProductionWorkplaces` behaelt seinen bestehenden `RequireXxxAccess`-Filter — nur ein
zusaetzliches, read-only dargestelltes Feld. Das BDE-Terminal behaelt seine bestehenden
`RequireBdeActive`/`RequireBdeUserAccess`/`RequireBdeShiftleadAccess`-Filter unveraendert; diese Spec
liefert nur zusaetzliche, korrekt geroutete Datenzeilen in bereits geschuetzte, bestehende Ansichten.

## Listen-View-Pattern-Pflichten (ADR 0005)

- `/ProductionWorkplaces`-Index (Baustein a): bestehende, bereits paginierte Liste bekommt eine neue
  Spalte (`ArbeitsschrittCode`) — Spaltenfilter dafuer ergaenzen, damit die Tabelle ADR-0005-konform
  bleibt (kein Sonderfall „nur eine Spalte").
- Sonst keine neue Tabellen-Ansicht. `WorkOperation`-Zeilen aus Baustein (b) erscheinen in bereits
  bestehenden, zugriffsgeschuetzten Ansichten (BDE-Terminal-Listen, `WorkOperationRepository`-basierte
  Uebersichten) ohne View-Aenderung.

## Akzeptanzkriterien

1. **Baustein a — Treffer.** Sage-Arbeitsplatz-Stammsatz mit `Bezeichnung1 = "Kanterei W1"` und
   Kuerzel `KA` matcht exakt (case-insensitiv, getrimmt) eine `ProductionWorkplace`-Zeile mit
   `Name = "Kanterei W1"` -> `ArbeitsschrittCode = "KA"` wird geschrieben, Audit gesetzt.
2. **Baustein a — kein Treffer.** Kein `ProductionWorkplace` mit passendem Namen -> keine Schreibung,
   Sammelmeldung im SyncLog, Sammelmail nur bei Mengenaenderung gegenueber dem letzten Lauf.
3. **Baustein a — Mehrdeutigkeit.** Zwei `ProductionWorkplace`-Zeilen mit identischem Namen -> keine
   Schreibung, Meldung „mehrdeutig".
4. **Baustein a — Abweichung.** Ein bereits gesetztes `ArbeitsschrittCode` weicht vom neuen Sage-Wert
   ab -> wird ueberschrieben (Sage fuehrend) UND gemeldet (alter/neuer Wert).
5. **Baustein b — Struktur-Ableitung korrekt.** Sub-FA mit Token `KA` in seiner DirectChildren-Token-
   Menge, `ProductionWorkplace` mit `ArbeitsschrittCode = "KA"` existiert -> genau eine neue
   `WorkOperation`-Zeile mit `OperationNumber = "KA"`, `Name` = Werkbank-Name, `ProductionWorkplaceId`
   = deren Id.
6. **Baustein b — unbekanntes Kuerzel.** Token ohne passenden `ArbeitsschrittCode` -> keine
   `WorkOperation`-Zeile, Sammelmeldung (Token-Liste + Anzahl betroffener Sub-FAs).
7. **Baustein b — Nur-hinzufuegen.** Eine bereits existierende `WorkOperation` mit gleichem
   `(ProductionOrderId, OperationNumber)` wird nie erneut angelegt oder veraendert — auch nicht, wenn
   sie manuell geloescht/deaktiviert wurde (sofern eine entsprechende App-Semantik existiert).
8. **Baustein b — DirectChildren, keine Doppelzaehlung.** Ein Token, das ausschliesslich auf einem
   Enkelknoten steht, erzeugt **keine** `WorkOperation` fuer den Grossvater-Sub-FA (identische Logik
   wie v1.41.0 AK 5).
9. **Baustein b — Fertig-/Storno-Filter.** Ein `ProductionOrder` mit `IsDone`/`IsCancelled`/
   `PickingStatus.IsDonePicking = true` bekommt keine neue `WorkOperation`, auch bei bekanntem Token.
10. **Baustein c — Verifikation ohne Resolver-Aenderung.** An einer Werkbank mit
    `ArbeitsschrittCode = "KA"` zeigt `GetAvailableOperations`/`BdeScanResolver` (Normal-Modus) fuer
    einen Sub-FA mit `WorkOperation.OperationNumber = "KA"` genau diesen Arbeitsgang — ohne dass
    `BdeScanResolver.cs` fuer diese Spec geaendert wurde (Nachweis: Diff zeigt keine Aenderung an
    dieser Datei, sofern Offene Rueckfrage 1 mit „Option Empfehlung F" beantwortet wird).
11. **AKE unveraendert.** `FaWorkStepDetectionService`, `FaWorkStep`, `WorkStep`-Katalog `VA/VE/VK/VL/VT`
    funktionieren nach dieser Spec identisch wie vorher; kein SyncLog-Eintrag der neuen/umbenannten
    Services bei Master `false`.
12. **Beide Gates greifen.** Baustein (b) laeuft nur bei `ProduktionsauftragHierarchisch = true` UND
    dem (umbenannten) Toggle `true` (Doppel-Gate im `SyncWorker`, `GetBoolSafeAsync` fuer beide).
13. **Audit korrekt.** Neue `WorkOperation`-Zeilen tragen `CreatedBy/CreatedByWindows =
    "WorkOperationStructureDetection"`; aktualisierte `ProductionWorkplace`-Zeilen tragen
    `ModifiedBy/ModifiedByWindows = "ProductionWorkplaceCodeSync"`.
14. **Migration idempotent.** `SQL/9X_AddProductionWorkplaceArbeitsschrittCode.sql` laeuft auf einer
    bestehenden Datenbank fehlerfrei und zweimal hintereinander ohne Aenderung beim zweiten Lauf.

## Test-Szenarien

Neues Kapitel „IDEAL — Arbeitsgaenge (WorkOperation) aus der Struktur + Werkbank-Routing" in
`docs/TESTSZENARIEN.md`, **naechste freie Nummer TS-77** (Stand TS-76 im Worktree — Dev-Lauf gegen den
dann aktuellen Stand bestaetigen):

- **TS-77.1 Baustein a — Grundfall (AK 1).** Sage-Quelle liefert `KA -> "Kanterei W1"`, WMS hat eine
  `ProductionWorkplace` „Kanterei W1" ohne Kuerzel. Sync-Lauf. Erwartung: `ArbeitsschrittCode = "KA"`.
- **TS-77.2 Baustein a — unbekannter Name (AK 2).** Sage-Quelle liefert einen Namen ohne
  WMS-Gegenstueck. Erwartung: keine Schreibung, Sammelmeldung; Mail nur beim ersten Lauf mit dieser
  Menge.
- **TS-77.3 Baustein a — Mehrdeutigkeit (AK 3).** Zwei `ProductionWorkplace` mit identischem Namen.
  Erwartung: keine Schreibung, Meldung „mehrdeutig".
- **TS-77.4 Baustein b — Grundfall (AK 5).** Sub-FA mit Token `KA`, `ProductionWorkplace` mit
  `ArbeitsschrittCode = "KA"` existiert. Erwartung: genau eine `WorkOperation` mit korrektem
  `ProductionWorkplaceId`.
- **TS-77.5 Baustein b — unbekanntes Kuerzel (AK 6).** Token ohne Katalog-Treffer. Erwartung: keine
  `WorkOperation`, Sammelmeldung, keine Exception.
- **TS-77.6 Baustein b — DirectChildren-Gegenprobe (AK 8).** Wie TS-76.2 (v1.41.0), aber Zielentitaet
  `WorkOperation` statt `FaWorkStep`.
- **TS-77.7 Baustein c — Terminal zeigt den echten AG (AK 10).** An einer Werkbank mit passendem
  `ArbeitsschrittCode` erscheint der struktur-abgeleitete Arbeitsgang eines gescannten Sub-FA im
  Normal-Modus, ohne manuelle Auswahl eines Default-AG.
- **TS-77.8 Baustein c — Koexistenz Default-AG (Offene Rueckfrage 6, Ergebnis-abhaengig).** NurFA-Modus
  mit vorhandenem echtem `WorkOperation`: Verhalten je nach Antwort auf Rueckfrage 6 dokumentieren.
- **TS-77.9 Regressionslauf TS-66.** Bestehende BDE-Disambiguierungs-Szenarien
  ([[2026-07-29-standort-ideal-teil-8-spec]], TS-66) laufen mit echten, struktur-abgeleiteten
  `WorkOperation`s statt nur dem Default-AG unveraendert durch (insbesondere: mehrere Sub-FAs
  derselben `OrderNumber`, Auswahlliste, NurFA-Fix).
- **TS-77.10 AKE-Regression (AK 11).** Master `false`: `FaWorkStepDetectionService` unveraendert aktiv,
  kein Lauf der neuen/umbenannten Services.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen; TS-76 (v1.41.0) im Index als
„ersetzt durch TS-77" kennzeichnen statt zu loeschen (Nachvollziehbarkeit der Historie).

Alle drei Bausteine sind **nicht InMemory-vollstaendig-testbar** (Sage-Quellzugriff, Zeitpunkt-
abhaengige Sync-Reihenfolge) — Manual-UAT auf der befuellten IDEAL-Testinstanz ist Pflicht, analog dem
bereits dokumentierten Muster bei v1.41.0 (H3 dort).

## Reihenfolge / Einordnung

- **Im Buendel-Zweig**, wie alle IDEAL-Bausteine seit August (`feature/2026-08-07-ideal-teile-1-5`) —
  kein neuer Worktree.
- **Ersetzt** [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]] (v1.41.0) fachlich vollstaendig;
  deren Status/Changelog-Eintrag ist bei der Umsetzung als „superseded" zu kennzeichnen (Brain-Aufgabe,
  nicht Teil dieser Spec-Datei selbst — die v1.41.0-Spec wird hier bewusst NICHT umgeschrieben).
- **Setzt voraus:** [[2026-08-20-materialisierung-fachliche-felder-spec]] (materialisierte
  `ProductionOrder`-Zeilen als Ziel von Baustein b) — bereits Testbereit im selben Worktree.
- **Reihenfolge der drei Bausteine:** a (Katalog) vor b (Ableitung) vor c (Terminal-Verifikation) —
  b braucht a's Katalog, c braucht b's Datenzeilen zum Testen. a und b koennen technisch im selben
  Sync-Zyklus laufen (kein harter FK-Zwang, nur Aktualitaets-Verzoegerung bei falscher Reihenfolge).

**Groessen-Einschaetzung (ehrlich, fuer die Etappen-/Split-Entscheidung des Menschen):**

Drei fachlich getrennte, technisch aber ineinandergreifende Bausteine mit jeweils eigenem Schema-
(a), Service- (a, b) und Verifikations-Anteil (c) plus einer Umbenennungs-/Supersede-Aktion (b) sind
fuer einen einzelnen Dev-Lauf voraussichtlich zu viel Aenderungsflaeche fuer sauberes, buildbares
Zwischenhalten. Zwei plausible Schnitte, beide dem Menschen zur Entscheidung vorgelegt
(Offene Rueckfrage 8):

- **Epic mit 3 Etappen** (a -> b -> c) im bestehenden Buendel-Worktree, je Etappe ein eigenstaendiger,
  buildbarer Commit, QA/Merge erst nach allen drei Etappen — analog
  [[2026-07-29-standort-ideal-teil-8-spec]]s Etappen-Schnitt. Vorteil: ein Merge, passt zum bereits
  laufenden Buendel-Rhythmus dieses Worktrees.
- **3 getrennte, einzeln mergbare Specs** (analog `split`), jede mit eigenem `depends_on` auf die
  vorige. Vorteil: Baustein (a) allein ist risikoarm und schnell test-/mergbar (reine
  Stammdaten-Ergaenzung); Baustein (b) traegt das groesste Risiko (Rename + Zieltabellen-Wechsel) und
  profitiert von einem eigenen, fokussierten Test-/Freigabezyklus, ohne auf (c) zu warten.

Diese Spec liefert bewusst **beide Optionen als gleichwertig plausibel** — die Entscheidung haengt an
Praeferenzen (ein Merge vs. schnelleres Teil-Feedback), die nur der Mensch treffen kann.

## Brain-Pflichten bei Umsetzung (Merkliste fuer den Dev-Lauf)

- Version-Bump in **beiden** `AppVersion.cs` — naechste freie Nummer nach `1.41.0` im Worktree
  (vermutlich `1.42.0` je Baustein oder Etappe, im Dev-Lauf zu bestaetigen).
- Anwender-Changelog `Views/Help/Changelog.cshtml` + Brain-Changelog
  `secondbrain/changelog/YYYY-MM-DD-vX-Y-Z-*.md`.
- `secondbrain/feature-map.md` — IDEAL-Abschnitt aktualisieren, v1.41.0-Zeile als superseded markieren.
- `secondbrain/specs/freigegeben/2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec.md` — im
  **Hauptcheckout** (nicht Worktree) einen Hinweis „superseded durch
  [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]" ergaenzen (additiv, Datei nicht
  umschreiben).
- `secondbrain/codebase/services.md` — Sync-Services-Tabelle: neuen Eintrag `ProductionWorkplaceCodeSyncService`,
  umbenannten Eintrag `WorkOperationStructureDetectionService` (ersetzt die bestehende
  `FaWorkStepStructureDetectionService`-Zeile), `ServiceSettings`-Katalogtabelle nachziehen.
- `secondbrain/architektur/fallstricke.md` — Eintrag: zwei unabhaengige Kuerzel-/Zuordnungs-Vokabulare
  bei IDEAL (`Arbeitsbereich` aus `USER_OSAbteilung` fuer `ProductionOrder.ProductionWorkplaceId`,
  ADR 0014, physischer Standort **einer Position**; `Arbeitsschritte`/`ArbeitsschrittCode` aus
  `USER_ArbeitsSchritt` fuer `WorkOperation.ProductionWorkplaceId`, Arbeitsgang-**Routing**) — leicht
  verwechselbar, unterschiedliche Sage-Quellspalte, unterschiedliches Zielfeld.
- Hilfeseite (`Views/Help/`) — Hinweis, dass `ProductionWorkplace.ArbeitsschrittCode` Sage-gefuehrt ist
  (manuelle Aenderung ueberlebt nicht den naechsten Sync-Lauf).
- `secondbrain/tests/testszenarien-index.md` — TS-77-Zeile, TS-76 als superseded markieren.

## Deploy

**Provisorisch (Dev-Lauf bestaetigt gegen den echten Diff):**

- **Web-App: ja.** `IdealAkeWms/Models/ProductionWorkplace.cs`, `ServiceSettingDefinitions.cs`,
  `SyncLogServices.cs`, `Views/ProductionWorkplaces/*.cshtml`, ggf. `BdeDefaultWorkOperationService.cs`
  (Offene Rueckfrage 6), Migration.
- **Service: ja.** Neuer Service (Baustein a), umbenannter/umgebauter Service (Baustein b),
  `SyncWorker.cs`, `Program.cs` (DI, neu + umbenannt).
- **Migration: ja** — genau eine, aus Baustein (a) (siehe „Migrations-/SQL-Auswirkungen"). Reihenfolge:
  DB-Migration vor Service-Neustart (Standardablauf), kein Daten-Backup-Zwang ueber das uebliche Mass
  hinaus (nicht destruktiv).
- **Zwei-Lauf-aehnlicher Ablauf, aber ohne harten FK-Zwang (anders als v1.41.0):** Deploy -> Baustein-
  a-Sync einmal mit `DryRun` beobachten (welche Werkbaenke matchen, welche nicht) -> scharf schalten ->
  Baustein-b-Sync beobachten (welche Sub-FAs bekommen AGs, welche Token bleiben unbekannt) -> scharf
  schalten. Kein Katalog-Pflegeschritt zwischen a und b noetig (anders als v1.41.0s `/WorkSteps`-Pflege) —
  das ist die unter „Technischer Loesungsentwurf" beschriebene strukturelle Vereinfachung.
- **Publish-Befehle (aus dem Worktree):**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
  *Nach dem Merge* nur erneut aus `main` publishen, falls der Merge tatsaechlich getestete Dateien mit
  parallelen `main`-Aenderungen zusammengefuehrt hat.

## Offene Rueckfragen

1. **Routing-Design (Baustein c).** `WorkOperation.ProductionWorkplaceId` bei der Anlage (Baustein b)
   direkt setzen — **Empfehlung F**, macht `BdeScanResolver` voraussichtlich unveraendert — oder die
   im Backlog skizzierte **Live-Kuerzel-Schnittmenge** zur Scan-Zeit berechnen? Empfehlung F spart
   einen kompletten Umbau eines produktiv genutzten Pfads und nutzt einen bereits vorhandenen Filter.
2. **Varianten-Regel.** Struktur nutzt Basis-Kuerzel (`KA`); Sage hat Werkbank-Varianten (`KA2`/`KA4`,
   `SW1`/`SW2`/`SWB`/`SWL`, `SÄ2`/`SÄE`/`SÄO`, `VM1`/`VM2`, `PG2`, `PL2`, `EM2`, `SLH`). Exakt `KA` oder
   Praefix-/Gruppen-Match? (Backlog-Tendenz: exakter Basis-Kuerzel-Match.)
3. **5 unbekannte Kuerzel** (`MO`/`ZS`/`LÖ`/`PR`/`BE`): melden, Werkbank in Sage nachtragen, oder aus
   dem Scope ausschliessen?
4. **Sage-Quelle Baustein a.** Genaue Tabelle/View der Arbeitsplatz-Stammdaten (`USER_ArbeitsSchritt`)
   + Zugriffsweg bestaetigen. Ein eigener, neuer Sync-Schritt ist die einzige Option (kein bestehender
   Workplace-Sync vorhanden) — das ist nicht mehr offen, nur die konkrete Quellangabe.
5. **Kuerzel->Name-Katalog.** Bestaetigung, dass **kein** separater WorkOperation-Katalog noetig ist —
   der Name kommt aus `ProductionWorkplace.Name` (Baustein a). Technisch durch das Fehlen eines
   Katalog-FK an `WorkOperation` begruendet, aber fachlich zu bestaetigen.
6. **Koexistenz `BdeDefaultWorkOperationService`.** Bei IDEAL abschalten/ersetzen, sobald echte
   `WorkOperation`s vorliegen — insbesondere im NurFA-Modus, der heute **immer** den generischen
   Default erzeugt und die echten, struktur-abgeleiteten AGs nie anbietet?
7. **Migrationsnummer-Koordination.** `SQL/93` vorgeschlagen — `SQL/92` ist von
   [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] im selben Worktree belegt (Status
   Freigegeben, Stand 2026-09-21). Im Dev-Lauf gegen den dann aktuellen Stand verifizieren, nicht
   blind uebernehmen.
8. **Umfang/Schnitt.** Epic mit 3 Etappen (a -> b -> c, ein Merge) oder 3 getrennte, einzeln mergbare
   Specs (schnelleres Teil-Feedback, hoeheres Koordinationsaufwand)? Siehe „Reihenfolge/Einordnung".

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → **Empfehlung F — `ProductionWorkplaceId` bei der Anlage setzen.** Keine Live-Schnittmenge.
   Die Begruendung der Spec traegt: Der Filter existiert an drei Stellen bereits, und
   `BdeScanResolver` ist ein **produktiv genutzter, auch von AKE durchlaufener Pfad** — ihn fuer eine
   Ersparnis umzubauen, die es nicht gibt, waere das falsche Risiko.
   **Eine Konsequenz von F, die die Spec nicht benennt und die dokumentiert gehoert:** F
   **materialisiert** das Routing zum Anlagezeitpunkt. Weil Baustein (b) Nur-hinzufuegen ist, wird eine
   bestehende `WorkOperation` **nie** nachgezogen. Ordnet Sage ein Kuerzel spaeter einer anderen
   Werkbank zu, routen **bereits angelegte, offene** Arbeitsgaenge weiter zur **alten** Werkbank; nur
   neue gehen zur neuen. Bei einer Live-Schnittmenge wirkte die Aenderung sofort.
   **Vertretbar**, weil die Zuordnung Kuerzel→Werkbank Stammdaten sind und sich selten aendern — und
   weil Baustein (a) jede Abweichung **meldet** (AK 4). Die Meldung ist damit das Signal, bestehende
   offene Arbeitsgaenge zu pruefen. **Als bekannte Eigenschaft in den Rumpf, nicht als Fehler.**

2. → **Exakter Basis-Kuerzel-Match. Kein Praefix.**
   Ein Praefix-Match waere **mehrdeutig**: `KA` traefe `KA2` **und** `KA4` — welche Werkbank? Genau
   die Mehrdeutigkeit, die das ganze Paket sonst meldet statt aufloest. Die Struktur liefert
   Basis-Kuerzel; exakt passt.
   **Folge, im UAT zu beobachten:** Werkbaenke mit Varianten-Kuerzel (`KA2`, `SW1` …) bekommen **keine**
   struktur-abgeleiteten Arbeitsgaenge — nur die Basis-Werkbank. Ob Varianten **alternative Werkbaenke
   fuer denselben Arbeitsgang** sind (dann muessten sie `KA`-Arbeit auch sehen) oder **eigene
   Arbeitsgaenge** (dann ist exakt richtig), ist eine Fachfrage. Exakt ist der sichere Start; eine
   Zuordnung Variante→Basis waere eine spaetere, eigene Erweiterung — mit Beleg aus dem Betrieb.

3. → **Melden — und im Code NICHT ausschliessen.** Der Melde-Mechanismus heilt sich selbst: Sobald die
   Werkbank in Sage steht, greift Baustein (a) beim naechsten Lauf, und Baustein (b) legt die
   Arbeitsgaenge an — **ohne Code-Aenderung**. Ein Ausschluss im Code muesste dagegen spaeter wieder
   ausgebaut werden.
   **Fachlich je Kuerzel zu pruefen** (keine Code-Aufgabe): Fehlt die Werkbank nur in Sage →
   nachtragen. Ist es ein Arbeitsgang **ohne** scannbare Werkbank (etwa extern) → bleibt er zu Recht
   unbuchbar, und die Meldung ist dann dauerhaft erklaert.

4. → **Quelle: `KHKPpsArbeitsplaetze`** (am 2026-09-21 vom Menschen bereitgestellt). Relevante Spalten:
   `Arbeitsplatznummer`, `Mandant`, `Bezeichnung1`, `Aktiv`, `USER_ArbeitsSchritt`.

   **Drei Pflicht-Filter, sonst liest der Sync falsch** — **ENTSCHIEDEN 2026-09-21:**
   - **`Mandant = 1`** — gilt bei beiden Standorten immer. **Als Parameter in den
     IDEAL-Standorteinstellungen** (Teil 6, [[2026-08-03-standorteinstellungen-maske]]) hinterlegt,
     Standardwert `1` — sichtbar, dokumentiert und ohne Deploy aenderbar (Entscheidung 2026-09-21;
     ersetzt den frueheren Vorschlag einer Konstante im Service).
   - **`Aktiv = -1`** — Sage-Konvention wie bei `KHKArtikel`. Ein stillgelegter Arbeitsplatz mit gleicher
     `Bezeichnung1` wie ein aktiver erzeugte sonst eine falsche Mehrdeutigkeit.
   - **`USER_ArbeitsSchritt` gefuellt** — Arbeitsplaetze ohne Kuerzel ueberspringen, nicht melden.

   **Doppelte Kuerzel — eine Luecke in Baustein (b):** Tragen **zwei aktive** Arbeitsplaetze dasselbe
   Kuerzel, weiss Baustein (b) nicht, welche `ProductionWorkplaceId` er setzen soll. Die Spec regelt
   Mehrdeutigkeit heute nur fuer **Namen** (Baustein a), nicht fuer **Kuerzel** (Baustein b).
   **Verbindlich:** Trifft ein Token mehrere Werkbaenke, wird **keine** `WorkOperation` angelegt, sondern
   „mehrdeutig" gemeldet — gleiches Muster wie ueberall. Vorab pruefbar:
   ```sql
   SELECT USER_ArbeitsSchritt, COUNT(*) AS Anzahl
   FROM KHKPpsArbeitsplaetze
   WHERE Aktiv = -1 AND ISNULL(USER_ArbeitsSchritt, '') <> '' -- plus Mandant-Filter
   GROUP BY USER_ArbeitsSchritt HAVING COUNT(*) > 1;
   ```

   ### NEUE BLOCKER-FRAGE: Wird `ProductionWorkplace.Name` gegen ZWEI verschiedene Vokabulare gematcht?

   `ProductionWorkplace` fuehrt **keinen** Sage-Schluessel — `Name` ist das einzige Verknuepfungsfeld
   (am Code geprueft). Und es wird damit **zweimal** gematcht, gegen **unterschiedliche** Sage-Quellen:
   - **ADR 0014 / Materialisierung:** `Name` ↔ **`Arbeitsbereich`** aus `USER_OSAbteilung` — Werte wie
     `K-02`, `S-01`, `H4-04`.
   - **Diese Spec, Baustein (a):** `Name` ↔ **`Bezeichnung1`** aus `KHKPpsArbeitsplaetze` — Werte wie
     „Kanterei W1", „Schweisserei W1".

   **Ein und dasselbe Feld kann nicht beide Konventionen gleichzeitig erfuellen.** Heissen die
   WMS-Werkbaenke `K-02`, findet Baustein (a) **keinen einzigen** Treffer und meldet jede Werkbank als
   unbekannt. Heissen sie „Kanterei W1", laeuft umgekehrt die ADR-0014-Zuordnung ins Leere. Das waere
   ein Sync, der sauber durchlaeuft und nichts tut.

   Die Spec hat den Unterschied der beiden Vokabulare selbst erkannt (Out-of-Scope, fallstricke-Merkliste)
   — aber nicht, dass **beide auf dasselbe Zielfeld** zielen.

   **VOR der Umsetzung zu pruefen:**
   ```sql
   -- WMS: wie heissen die Werkbaenke heute?
   SELECT Name FROM ProductionWorkplaces ORDER BY Name;
   -- Sage: wie heissen die Arbeitsplaetze?
   SELECT Bezeichnung1, USER_ArbeitsSchritt FROM KHKPpsArbeitsplaetze
   WHERE Aktiv = -1 ORDER BY Bezeichnung1;   -- plus Mandant-Filter
   ```
   **Moegliche Ergebnisse:**
   - **Die Namen ueberschneiden sich** (etwa weil es zwei Arten Werkbank-Eintraege gibt: Bereiche wie
     `K-02` und Arbeitsplaetze wie „Kanterei W1") → Entwurf traegt, aber die Koexistenz gehoert benannt.
   - **Sie ueberschneiden sich nicht** → der Name-Match traegt nicht. Dann ist der robuste Weg ein
     **eigener Schluessel**: `ProductionWorkplace.SageArbeitsplatznummer` aus
     `KHKPpsArbeitsplaetze.Arbeitsplatznummer`, einmalig zugeordnet, danach umbenennungssicher. Das
     waere eine zweite Spalte in derselben Migration — kein neuer Baustein.

   **Hinweis, bewusst ausserhalb des Umfangs:** Die Tabelle fuehrt eine Spalte **`BdeTerminalId`**. Moeglich,
   dass Sage selbst schon eine Zuordnung Arbeitsplatz→BDE-Terminal kennt. Nicht Teil dieser Spec — aber
   vor einem kuenftigen Terminal-Umbau wert, sie anzusehen, statt eine zweite Zuordnung zu erfinden.

   ### Stand 2026-09-21: Bei IDEAL gibt es noch KEINE `ProductionWorkplaces`

   **Folge, die bekannt sein muss:** Ohne Werkbaenke findet Baustein (a) keinen Treffer und meldet jeden
   Sage-Arbeitsplatz als unbekannt; Baustein (b) findet kein Kuerzel und legt keine Arbeitsgaenge an.
   **Die Kette liefert nichts, bis die Werkbaenke existieren.** Die Materialisierung braucht sie
   ebenfalls (Werkbank-Zuordnung aus dem Arbeitsbereich) — dort ist das Anlegen vor dem Deploy bereits
   Vorbedingung.
   **Die leere Tabelle ist zugleich die Chance:** Die Namenskonvention laesst sich jetzt ohne Altlast
   festlegen.

   **OFFEN — vor der Umsetzung vom Menschen zu entscheiden (fachlich, nicht am Code ablesbar):**
   Ist eine IDEAL-Werkbank ein **Arbeitsbereich** (`K-02`, Quelle `USER_OSAbteilung`, Ziel der
   Materialisierung) oder ein **Arbeitsplatz** („Kanterei W1", Quelle `KHKPpsArbeitsplaetze`, Ziel dieser
   Spec)?
   - **Dieselben Dinge mit zwei Namen** → einmal anlegen; `Name` traegt einen der beiden, der andere
     bekommt einen eigenen Schluessel (`SageArbeitsplatznummer`).
   - **Verschiedene Dinge** (ein Bereich enthaelt mehrere Arbeitsplaetze) → Materialisierung und
     Arbeitsgaenge zeigen auf **unterschiedliche Ebenen**; dann gehoeren sie nicht beide an
     `ProductionWorkplace.Name`.
   **AKE:** Laut Mensch keine Ueberschneidungen zu erwarten.

   ### Loesungsweg ueber die Standorteinstellungen — aber mit dem RICHTIGEN Parameter

   **Vorschlag des Menschen (2026-09-21):** die Frage als Parameter in den IDEAL-Standorteinstellungen
   hinterlegbar machen. Das traegt — **aber nur mit dem richtigen Parameter:**

   **NICHT: ein Schalter „Werkbank = Arbeitsbereich oder Arbeitsplatz".** Beide Abgleiche zielen auf
   dasselbe Feld `ProductionWorkplace.Name`. Ein solcher Schalter waehlte nur aus, **welcher** der beiden
   ins Leere laeuft — und Werkbaenke, die unter der einen Einstellung angelegt wurden, truegen nach
   einem Umschalten falsche Namen. Er verdeckt den Konflikt, statt ihn zu loesen.

   **SONDERN: „Match-Spalte fuer Baustein (a)"** — gegen welche Spalte von `KHKPpsArbeitsplaetze` wird
   `ProductionWorkplace.Name` abgeglichen: `Bezeichnung1`, `Matchcode` oder `Arbeitsplatznummer`.
   **Das kann den Konflikt tatsaechlich aufloesen:** Traegt eine dieser Spalten dieselben Werte wie der
   `Arbeitsbereich` (`K-02`, `S-01`, `H4-04` …), dann heisst die Werkbank `K-02`, die Materialisierung
   (ADR 0014) findet sie ueber den Arbeitsbereich, und Baustein (a) findet sie **ebenfalls** — nur ueber
   die gewaehlte Sage-Spalte. **Ein Name, beide Abgleiche, keine zweite Schluesselspalte.**

   **Pruefbar vor der Umsetzung:**
   ```sql
   SELECT Arbeitsplatznummer, Matchcode, Bezeichnung1, USER_ArbeitsSchritt
   FROM KHKPpsArbeitsplaetze
   WHERE Mandant = 1 AND Aktiv = -1
   ORDER BY Arbeitsplatznummer;
   ```
   - **Eine Spalte traegt Werte wie `K-02`** → sie wird die Match-Spalte, Standardwert des Parameters.
     Konflikt geloest, **Werkbaenke werden nach dem Arbeitsbereich benannt.**
   - **Keine Spalte passt** → der Parameter hilft nicht; dann bleibt die zweite Schluesselspalte
     `SageArbeitsplatznummer` der Weg (siehe oben).

   **Warum der Parameter trotzdem bleibt, auch wenn die Pruefung eindeutig ist:** Die Zuordnung ist eine
   Stammdaten-Konvention, die sich aendern kann, ohne dass jemand den Code anfasst. Die Einstellung macht
   sie sichtbar und ohne Deploy korrigierbar — dieselbe Begruendung wie fuer den Mandanten.
   **Eine Pflicht dazu:** Die Hilfeseite muss sagen, dass ein Wechsel der Match-Spalte **bestehende**
   Werkbank-Zuordnungen nicht umschreibt — er wirkt erst beim naechsten Sync-Lauf, und Werkbaenke mit
   unpassendem Namen fallen dann in die Meldung.

   ### ERGEBNIS DER ABFRAGE (2026-09-21) — die Vermutung traegt NICHT

   `KHKPpsArbeitsplaetze` (Mandant 1, aktiv) liefert 66 Arbeitsplaetze. **Keine Spalte traegt
   Arbeitsbereichs-Werte wie `K-02`:** `Arbeitsplatznummer` ist vierstellig numerisch (`2300`),
   `Matchcode` ist nahezu identisch mit `Bezeichnung1` („Kanterei W1"), `USER_ArbeitsSchritt` traegt das
   Kuerzel (`KA`). **Der Parameter „Match-Spalte" ueberbrueckt die beiden Vokabulare nicht.**

   **Damit ist die Grundfrage nicht mehr zu umgehen:** Der `Arbeitsbereich` (`K-02`, `S-01`, `H4-04`,
   Sage-Feld `USER_OSAbteilung`) und der **Sage-PPS-Arbeitsplatz** (`2300` / „Kanterei W1" / `KA`, Tabelle
   `KHKPpsArbeitsplaetze`) sind **zwei Vokabulare** — beide aus Sage, aber ohne gemeinsamen Wert.
   `ProductionWorkplace.Name` kann nicht beide tragen.
   *Korrektur 2026-09-21:* Eine fruehere Fassung dieses Absatzes deutete `USER_OSAbteilung` als
   **OSEON**-Abteilung. **Das war falsch — IDEAL hat kein OSEON** (steht bereits im Ziel-Abschnitt dieser
   Spec). Der Schluss kam aus dem Feldnamen (`OS`), nicht aus den Daten.

   **Offen — moeglicherweise gibt es die Bruecke doch:** Weil **beide Felder aus Sage** stammen, ist denkbar,
   dass Sage intern eine Zuordnung Arbeitsbereich→Arbeitsplatz kennt (eine Stammdaten-Tabelle, ein
   Feld am Arbeitsplatz). **Vor einer eigenen Zuordnungstabelle im WMS pruefen**, ob es die in Sage
   schon gibt — dieselbe Leitlinie wie ueberall (ponytail, Sprosse 2).

   **Entscheidung des Menschen noetig — praktisch gefragt:** An welcher Stelle steht das
   **BDE-Terminal**, an dem der Werker scannt — an einem **Arbeitsplatz** wie „Kanterei W1" oder an
   einem **Bereich** wie `K-02`? Das Vokabular, das den Terminal-Standort benennt, ist das, was eine
   WMS-Werkbank bei IDEAL sein muss.
   - **Arbeitsplatz** → die Werkbaenke werden aus `KHKPpsArbeitsplaetze` angelegt; die
     Materialisierung (ADR 0014, Werkbank aus dem **Arbeitsbereich**) braucht dann eine Zuordnung
     Arbeitsbereich→Arbeitsplatz, oder ihre Werkbank-Ableitung ist neu zu bewerten.
   - **Bereich** → ADR 0014 bleibt, aber das BDE-Routing (diese Spec) braucht eine Zuordnung
     Arbeitsplatz→Bereich, weil die Kuerzel an den Arbeitsplaetzen haengen.
   Beides ist machbar. **Nicht machbar ist, beide ueber denselben Namen zu verknuepfen.**
   Hintergrund: ADR 0014 entstand mit der Annahme „Werkbank = Arbeitsbereich aus der FA-Struktur" —
   bevor die Arbeitsplatz-Stammdaten vorlagen. Diese Annahme gehoert jetzt ueberprueft, nicht nur
   ergaenzt.

   **Unabhaengig von der Antwort: Abgleich ueber `Arbeitsplatznummer`, NICHT ueber einen Namen.**
   Die Namensspalten sind fuer einen Abgleich ungeeignet — am Datensatz belegt:
   - `Matchcode` und `Bezeichnung1` **weichen ab** (2150: „Flaechen entgraten (X)" vs. „Flaechen entgr
     beendet"; 2550: „Punkten Ladenbau W1" vs. „…W1 (EKP)"; 2950; 0000).
   - **Doppelte Leerzeichen** („Schaeumerei  W1 (EG)", „Endmontage SB  W1 (OG)").
   - Eine Umbenennung in Sage braeche die Verknuepfung.
   `Arbeitsplatznummer` ist dagegen **eindeutig, stabil und sauber**. Damit wird
   **`ProductionWorkplace.SageArbeitsplatznummer`** zur Pflichtspalte neben `ArbeitsschrittCode` — dieselbe
   Migration. Der Parameter „Match-Spalte" **entfaellt** (es gibt nur einen tauglichen Schluessel).

   ### Datenbefunde aus der Liste — fuer den Sync verbindlich

   - **`"EG "` mit Leerzeichen** (Arbeitsplatz 2150). Ohne `TRIM` matcht das Struktur-Token `EG` nie.
     **Kuerzel beim Lesen trimmen — Pflicht**, sonst faellt `EG` still in die Unbekannt-Meldung.
   - **Keine doppelten Kuerzel** unter den aktiven Arbeitsplaetzen. Die Mehrdeutigkeitsregel fuer
     Baustein (b) bleibt als Absicherung, tritt heute aber nicht ein.
   - **Pseudo-Arbeitsplaetze** stehen mit `Aktiv = -1` in der Liste: **Storno** (`STO`), **Gestoppt**
     (`XXX`), **Reserve** (`x01`), dazu Verkauf, Lager, Verladung, externe Beschichter, Reklamation
     (`RM-*`). Sie stoeren nicht, **solange kein Struktur-Token auf sie zeigt** — ein Arbeitsgang
     „Storno" am Terminal waere aber offensichtlich falsch. **Zu klaeren:** Sollen Kuerzel wie `STO`/`XXX`
     vom Routing ausgeschlossen werden, oder koennen sie in der Struktur ohnehin nicht vorkommen?
   - **Ausgelaufene Arbeitsplaetze** mit Markierung `(X)` bzw. „beendet" sind noch aktiv — **darunter
     `EG`**, eines der zwoelf gematchten Kuerzel. Wird `EG`-Arbeit auf einen beendeten Arbeitsplatz
     geroutet? Fachlich zu pruefen.
   - **Korrektur der Zuordnungstabelle oben:** `VM` ist **„Vormontage W1"** (3002), nicht
     „Elektrofertigung W1 (OG)" — das ist `VM9` (3001). Der Sync liest das Kuerzel aus der Quelle und
     ist davon nicht betroffen; die Tabelle ist aber als Dokumentation falsch und gehoert korrigiert.
   - **Die fuenf unbekannten Kuerzel** (`MO`/`ZS`/`LÖ`/`PR`/`BE`) stehen **nicht** in der Liste — bestaetigt.

   ### AUFLOESUNG (2026-09-21) — drei verschiedene Begriffe, vom Menschen geklaert

   | Begriff | Bedeutung | Quelle |
   |---|---|---|
   | **Arbeitsbereich** (`K-02`, `S-01`) | **Zielort** — wohin das Teil kommt, wenn alle Arbeitsschritte erledigt sind, bzw. allgemein sein Bestimmungsort. Begrifflich aus **Lagerorten** abgeleitet. | `USER_OSAbteilung` |
   | **Arbeitsschritt** (`SW`, `LS`, `KA`) | **Arbeitsgang**, der am Teil erfolgen muss | `FaHierarchyNode.Arbeitsschritte` |
   | **Werkbank** (fuers BDE) | **Arbeitsplatz**, an dem gearbeitet wird | `KHKPpsArbeitsplaetze` |

   **Arbeitsbereich und Arbeitsschritt stehen in KEINER hierarchischen Beziehung.**

   **Damit ist entschieden: Eine IDEAL-Werkbank im WMS ist der Sage-Arbeitsplatz.** BDE-Terminals sollen
   an den meisten Arbeitsplaetzen stehen.
   - **Verknuepfung ueber `SageArbeitsplatznummer`** (aus `KHKPpsArbeitsplaetze.Arbeitsplatznummer`) —
     nicht ueber den Namen (siehe Datenbefunde oben).
   - **`Name` = `Bezeichnung1`** — fuer die **Anzeige** (Entscheidung 2026-09-21), kein Abgleichsschluessel.
   - **`ArbeitsschrittCode` = `USER_ArbeitsSchritt`** (getrimmt), Sage-gefuehrt, read-only.
   - **Einrichtung — der Sync LEGT DIE WERKBAENKE AN (Entscheidung 2026-09-21):** Nummer, Name und
     Kuerzel werden **im DB-Abgleich** gesetzt, nicht von Hand. Das heisst zwingend: Baustein (a) legt
     fehlende Werkbaenke **selbst an** — ohne Namensabgleich gaebe es sonst keinen Weg, eine Nummer der
     richtigen Werkbank zuzuordnen.
     **Damit kehrt sich `melden statt anlegen` fuer DIESE Tabelle um — bewusst und begruendet:** Bei
     ADR 0014 kamen die Werte aus **freiem Text an Auftragspositionen** (Tippfehler, Altlasten).
     `KHKPpsArbeitsplaetze` ist dagegen die **gepflegte Stammdatentabelle** von Sage. Wer daraus
     anlegt, uebernimmt eine Liste, die jemand bewusst fuehrt. **Sage ist fuehrend** — fuer Nummer,
     Name und Kuerzel.
     **Bestehende Werkbaenke** werden ueber die Nummer wiedergefunden; aendern sich Name oder Kuerzel in
     Sage, zieht der Sync sie nach und **meldet** die Abweichung (ADR-0014-Muster).
   - **OFFEN — welche Sage-Zeilen werden zu WMS-Werkbaenken?** Die Tabelle enthaelt auch
     **Pseudo-Arbeitsplaetze** (Storno, Gestoppt, Reserve, Verkauf, Lager, Verladung, externe
     Beschichter, Reklamation). Sie als „Werkbank" anzulegen, machte `/ProductionWorkplaces`
     unuebersichtlich und boete sie am Terminal zur Auswahl an. Drei Wege:
     - **Alle anlegen, BDE-Sichtbarkeit ueber das bestehende `BdeAktiv`** (Standard `false`, der Mensch
       schaltet echte Werkbaenke frei). Nutzt ein vorhandenes Feld, aber die Pseudo-Eintraege stehen in
       der Werkbank-Liste.
     - **Nur Kuerzel anlegen, die in der Struktur vorkommen** (`FaHierarchyNode.Arbeitsschritte`).
       Datengetrieben — Storno und Gestoppt tauchen dort nie auf, es entstehen genau die benoetigten
       Werkbaenke. **Nachteil:** Eine Werkbank entsteht erst, wenn ein Auftrag sie braucht; ein Terminal
       laesst sich nicht vorab fuer sie einrichten.
     - **Ausschlussliste in den Standorteinstellungen** (Kuerzel wie `STO`, `XXX`, `x01`).
     *Einschaetzung:* der zweite Weg ist der sauberste, sofern die Terminal-Einrichtung warten kann.
     Zu entscheiden.
   - **Folge fuer die UI:** In `/ProductionWorkplaces` sind Nummer, Name und Kuerzel **read-only**, weil
     Sage-gefuehrt. Hinweis auf der Hilfeseite: Aenderungen gehoeren nach Sage, nicht ins WMS.
   - **Der Arbeitsbereich zielt NICHT auf `ProductionWorkplace`.** Damit entfaellt der
     Namenskonflikt dieser Spec vollstaendig. **Aber:** ADR 0014 tut genau das — siehe unten.

   **Neue Anforderung an das Terminal (Baustein c ist damit NICHT leer):**
   Anfangs wird **ein Terminal mehrere Werkbaenke** bedienen. Der Werker **waehlt die Werkbank** und sieht
   dann nur deren Auftraege. **Zu pruefen:** Kann das BDE-Terminal heute zwischen mehreren Werkbaenken
   waehlen, oder ist es fest an eine gebunden? Ist die Auswahl vorhanden, bleibt Empfehlung F
   unveraendert gueltig (gefiltert wird ohnehin nach `ProductionWorkplaceId`). Fehlt sie, ist die
   Werkbank-Auswahl am Terminal **echte Arbeit in Baustein (c)** — und die Zwei-Etappen-Einschaetzung
   (Antwort 8) ist zu ueberpruefen.

   ### KONSEQUENZ FUER ADR 0014 — ausserhalb dieser Spec, aber vor ihrer Umsetzung zu klaeren

   [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]] und die Materialisierung
   ([[2026-08-20-materialisierung-fachliche-felder-spec]], v1.37) leiten
   `ProductionOrder.ProductionWorkplaceId` — die **Werkbank** — aus dem **Arbeitsbereich** ab. Das beruht
   auf der Annahme vom August: *„Werkbank = Arbeitsbereich aus der FA-Struktur"*. **Diese Annahme ist
   jetzt widerlegt:** Der Arbeitsbereich ist ein **Zielort**, keine Werkbank.
   **Heute noch folgenlos** — bei IDEAL gibt es keine Werkbaenke, also findet die Ableitung nichts und
   schreibt nichts. **Sobald die Werkbaenke angelegt sind**, meldet sie jeden Arbeitsbereich als
   „unbekannte Werkbank" (die Namen passen nicht) — oder, falls jemand eine Werkbank `K-02` nennt,
   schreibt sie einen **Zielort in das Werkbank-Feld**.
   **Eigene Notiz:** [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort]]. **Reihenfolge:** vor dem Anlegen
   der IDEAL-Werkbaenke klaeren.

   **Vorschlag (Bestaetigung ausstehend): Baustein (a) ebenfalls an den Master haengen.** Sein einziger
   Abnehmer ist Baustein (b), und der laeuft nur bei IDEAL. Ungegated laese (a) bei AKE die
   AKE-Arbeitsplaetze, faende keine passenden Werkbaenke und meldete alles als unbekannt — reines
   Rauschen. Doppel-Gate wie bei (b): Master **und** eigener Toggle.

5. → **BESTAETIGT — kein separater Katalog.** `WorkOperation` hat keinen Katalog-FK, der Name kommt
   aus `ProductionWorkplace.Name`. Das ist sauberer als v1.41 und beseitigt den Zwei-Lauf-Ablauf.
   **Dieselbe Materialisierungs-Eigenschaft wie bei Antwort 1:** Der Name wird beim Anlegen
   **kopiert**. Wird eine Werkbank spaeter umbenannt, behalten bestehende Arbeitsgaenge den alten
   Namen. Unkritisch, aber in den Rumpf.

6. → **NurFA bleibt beim generischen Default — das ist die Definition des Modus. Aber der
   Existenz-Check muss repariert werden.**
   „Nur FA" heisst: auf den Auftrag buchen, **ohne** Arbeitsgang zu waehlen. Dort echte Arbeitsgaenge
   anzubieten, widerspraeche dem Zweck. Normal-Modus zeigt die echten automatisch (die Spec hat
   verifiziert, dass der Default dort nicht aufgerufen wird). **Kein Koexistenz-Konflikt zwischen den
   Modi** — es sind zwei bewusste Wege.
   **Der eigentliche Fund steckt in einem Nebensatz der Spec:** `FindOrCreateDefaultAsync` prueft die
   Existenz ueber den **Namen**. Echte Arbeitsgaenge tragen `Name = ProductionWorkplace.Name`, der
   Default `Name = BdeDefaultArbeitsgang`. Stimmen beide ueberein — was jemand bei der Pflege leicht
   so setzt —, **findet der Default-Service den echten Arbeitsgang und bucht darauf**, statt den
   Default anzulegen. Eine NurFA-Buchung landete still auf einem echten Arbeitsgang.
   **Verbindlich:** Der Existenz-Check geht ueber `OperationNumber = "01"`, nicht ueber den Namen. Echte
   Arbeitsgaenge nutzen das Kuerzel als `OperationNumber` — die Nummer trennt sauber, der Name nicht.
   Eine Zeile Code, aber sie gehoert **mit einem Test** in den Umfang.

7. → **Naechste freie Nummer zum Umsetzungszeitpunkt.** Die Kommissionierziel-Spec ist freigegeben und
   laeuft voraussichtlich zuerst — dann ist `93` richtig. Laeuft diese Spec zuerst, nimmt sie `92`.
   **Gegen den tatsaechlichen Worktree-Stand pruefen, nicht die Nummer aus der Spec uebernehmen.**

8. → **Epic mit ZWEI Etappen im Buendel-Worktree — nicht drei Specs.**
   **Der Vorteil getrennter Specs existiert hier nicht:** „Einzeln mergbar" setzt voraus, dass gemergt
   werden kann. Im Buendel-Modell merged **nichts**, bevor Schranke 2 fuer das ganze Buendel faellt.
   Schnelleres Teil-Feedback gibt es also nicht — nur mehr Koordination.
   **Zwei statt drei Etappen**, weil Baustein (c) durch Antwort 1 und 6 fast leer wird
   (Verifikations-AK plus der Existenz-Check-Fix):
   - **Etappe A — Baustein (a):** `ArbeitsschrittCode` + Sage-Sync + Migration. Risikoarm, in sich
     abgeschlossen. **STOPP + melden.**
   - **Etappe B — Bausteine (b) + (c):** Umbau auf `WorkOperation`, Umbenennung,
     Terminal-Verifikation, Existenz-Check-Fix aus Antwort 6.
   Etappe B ist die riskante (Umbenennung, Zieltabellen-Wechsel) — deshalb der Halt davor.

**Zur Abnahme von v1.41:** Die dortige Manual-Checkliste ist mit dieser Spec **gegenstandslos** —
v1.41 schreibt in die falsche Tabelle. **Nicht separat abnehmen**, sondern mit TS-77 dieser Spec.

## Kritische Pruefung (2026-09-22)

> Anwalt-des-Teufels-Durchsicht **vor** dem Dev-Lauf. Die acht Freigabe-Antworten — vor allem der
> lange Antwort-4-Nachtrag mit seiner „AUFLOESUNG" — haben den Entwurf **erheblich umgebaut**, aber
> **nichts davon steht im Rumpf**. Der Rumpf (Fachliche Anforderungen, Baustein a, Migration,
> `affected_code`, AK, Testszenarien, Deploy) beschreibt durchgaengig noch das **alte** Design. Das
> ist der teure Riss, vor dem gewarnt wurde. `freigabe_entscheidung` ist ausserdem noch leer. Mehrere
> BLOCKER, ein paar SOLLTE, Hinweise. Empfehlung am Ende.

### BLOCKER — Rumpf widerspricht den getroffenen Entscheidungen

**B1 — „Anlegen" vs. „melden" (direkt die Frage des Menschen).** **Nein, es steht nicht in
`affected_code`/AK.** Rumpf: „Melde-statt-Anlege-Prinzip wie ADR 0014" (Z.95), „**keine** Werkbank
automatisch anlegen — exakt ADR 0014" (Z.198), AK 1-4 + TS-77.1-3 beschreiben Melden. **Entschieden**
(Antwort 4, Z.784-792): Baustein (a) **legt die Werkbaenke selbst an** (Nummer, Name, Kuerzel im
DB-Abgleich); „melden statt anlegen kehrt sich fuer DIESE Tabelle um" (gepflegte Stammdaten
`KHKPpsArbeitsplaetze`, anders als die Freitext-Arbeitsbereiche). → Fachliche Anforderung, Baustein-a-
Beschreibung (Z.177-216), AK 1-4, TS-77.1-3, Deploy-„DryRun beobachten"-Text und `affected_code`
muessen auf **Anlege-Semantik** umgeschrieben werden. Die ganze „exakt ADR 0014"-Begruendung ist jetzt
das Gegenteil.

**B2 — Match ueber `SageArbeitsplatznummer`, nicht ueber `Name`; zweite Migrationsspalte
(direkt die Frage des Menschen).** **Nein, der Rumpf matcht noch ueber `Name`.** Rumpf: Match
`Bezeichnung1 → ProductionWorkplace.Name` (Z.194/197), Migration mit **nur** `ArbeitsschrittCode`
(Z.314-315), AK 1 name-basiert. **Entschieden** (Antwort 4, Z.739-747, 780-782): Verknuepfung ueber
**`ProductionWorkplace.SageArbeitsplatznummer`** (aus `KHKPpsArbeitsplaetze.Arbeitsplatznummer`) —
Pflicht-**zweite Spalte in derselben Migration**; `Name = Bezeichnung1` nur zur **Anzeige**; der
Parameter „Match-Spalte" **entfaellt** (am Datensatz belegt: Namen weichen ab/haben Doppel-Leerzeichen,
66 Arbeitsplaetze, keine Spalte traegt `K-02`-Werte). → Migrations-Abschnitt (Z.312-328),
`affected_code` (nur eine Spalte gelistet), Baustein-a-Ablauf und AK 1 sind falsch. Zusaetzlich Pflicht
laut Antwort: **Kuerzel beim Lesen `TRIM`en** (`"EG "` mit Leerzeichen, Z.751) — sonst faellt `EG`
still in die Unbekannt-Meldung.

**B3 — Der Existenz-Check-Fix (Antwort 6) fehlt in `affected_code`/AK, obwohl er ein echter Bug ist.**
Entschieden (Antwort 6, Z.847-860): `BdeDefaultWorkOperationService.FindOrCreateDefaultAsync` prueft die
Existenz heute ueber den **Namen** — stimmt `BdeDefaultArbeitsgang` mit einem echten Werkbank-Namen
ueberein, bucht eine NurFA-Buchung **still auf einen echten Arbeitsgang**. Fix: Existenz-Check ueber
`OperationNumber = "01"`, **mit Test**. Der Rumpf fuehrt Baustein (c) noch als „voraussichtlich kein
Code-Eingriff" (Z.100-102, 294-296); dieser konkrete, entschiedene Code-Fix + Test gehoert in
`affected_code`, In-Scope und als AK.

**B4 — Etappen-Entscheidung nicht im Frontmatter.** Entschieden (Antwort 8): **Epic mit ZWEI Etappen**
(A = Baustein a; B = Bausteine b+c, mit STOPP vor B). Frontmatter: `epic: false`, `etappen: []`; der
Abschnitt „Reihenfolge/Einordnung" bietet noch **beide** Optionen als gleichwertig an (Z.459-477). →
`epic: true` + `etappen`-Tabelle setzen, Groessen-Abschnitt auf die getroffene 2-Etappen-Entscheidung
reduzieren.

**B5 — Noch GENUINE offene Entscheidungen, die den Umfang von Baustein (a)/(c) bestimmen — nicht nur
Rumpf-Nachzug.** Diese sind in Antwort 4 ausdruecklich als „zu entscheiden/zu pruefen" offen:
- **Welche Sage-Zeilen werden ueberhaupt WMS-Werkbaenke?** (Z.795-808) — Pseudo-Arbeitsplaetze
  (`STO`/`XXX`/`x01`, Verkauf, Lager, externe Beschichter …) stehen aktiv in der Liste. Drei Wege
  angeboten („alle + `BdeAktiv`", „nur in der Struktur vorkommende Kuerzel", „Ausschlussliste"),
  Entscheidung offen. **Das ist die Kernfrage der neuen Anlege-Semantik (B1)** — ohne sie kann
  Baustein (a) nicht gebaut werden.
- **Kann das BDE-Terminal heute zwischen mehreren Werkbaenken WAEHLEN?** (Z.814-820) — anfangs bedient
  ein Terminal mehrere Werkbaenke, der Werker waehlt. Fehlt die Auswahl, ist Baustein (c) **echte
  Arbeit**, und die „Baustein c fast leer"-Annahme (Z.100-102) samt Zwei-Etappen-Schnitt kippt. **Am
  Code zu klaeren, bevor der Umfang steht.**
- **`STO`/`XXX`-Kuerzel vom Routing ausschliessen?** (Z.755-759) und **`EG` auf einem „beendeten"
  Arbeitsplatz** (Z.760-762) — offen.
→ Diese drei muss der Mensch entscheiden bzw. am Code klaeren lassen, **bevor** Baustein (a)/(c)
umgesetzt werden — sie aendern, was gebaut wird, nicht nur wie der Rumpf klingt.

### SOLLTE

**S1 — Baustein (a) ans Master-Gate haengen (Antwort 4, Z.836-839, „Bestaetigung ausstehend").**
Sonst laeuft der Sync bei AKE ins Leere und meldet alle AKE-Arbeitsplaetze als unbekannt (Rauschen).
Doppel-Gate wie Baustein (b). Bestaetigen und in Baustein-a-Text + AK aufnehmen; im Rumpf steht noch
„kann an beliebiger Stelle stehen" (Z.207).

**S2 — Kuerzel-Mehrdeutigkeit in Baustein (b) als AK.** Antwort 4 (Z.599-609): tragen zwei aktive
Arbeitsplaetze dasselbe Kuerzel, legt Baustein (b) **keine** `WorkOperation` an, sondern meldet
„mehrdeutig". Heute regelt die Spec Mehrdeutigkeit nur fuer **Namen** in Baustein (a). Als AK + Test
ergaenzen (auch wenn aktuell keine Dubletten existieren — Absicherung).

**S3 — Selbst-markierte „in den Rumpf"-Eigenschaften einarbeiten.** Antwort 1 (Routing wird bei der
Anlage **materialisiert**; spaetere Sage-Umzuege ziehen offene Arbeitsgaenge **nicht** nach — nur
Meldung) und Antwort 5 (Werkbank-**Name** wird beim Anlegen **kopiert**, Umbenennung schlaegt nicht
durch) sind bekannte Eigenschaften, die beide Antworten ausdruecklich „in den Rumpf" verlangen.

**S4 — Doku-Korrektur + veraltete Token-Tabelle.** Antwort 4 (Z.763-765): `VM` ist „Vormontage W1"
(3002), nicht „Elektrofertigung W1 (OG)" (= `VM9`, 3001). Die Token→Werkbank-Tabelle im Rumpf
(Z.142-155) traegt den falschen `VM`-Namen und ist gegen die 66-Zeilen-Abfrage zu verifizieren.

### HINWEIS

**H1 — `freigabe_entscheidung` ist leer**, obwohl die acht Antworten ausgefuellt sind. Nach der
Rumpf-Nachbesserung gehoert die Kernentscheidung dort hinein (Anlegen aus `KHKPpsArbeitsplaetze` ueber
`SageArbeitsplatznummer`, Epic/2 Etappen, Empfehlung F) — Setzen bleibt die Geste des Menschen.

**H2 — Baustein (a) hat jetzt eine deutlich groessere Wirkflaeche.** Aus „ein Kuerzel-Feld befuellen"
ist „eine Sage-gefuehrte Stammdatentabelle mit **Anlage** neuer Zeilen" geworden. Das beruehrt
`/ProductionWorkplaces` (Nummer/Name/Kuerzel read-only), das `BdeAktiv`-Feld als Sichtbarkeits-Gate und
die Frage, was mit bestehenden, von Hand angelegten Werkbaenken passiert. Im neu zu schreibenden
Baustein-a-Text sauber fassen.

**H3 — Praktische Empfehlung: Baustein (a) neu drafteln statt flicken.** Die Divergenz ist so gross
(Anlege- statt Melde-Semantik, zweite Schluesselspalte, Master-Gate, offene Zeilen-Auswahl), dass ein
gezielter Neu-Entwurf von Fachlicher Anforderung, Baustein (a), Migration, betroffenen AK und
Frontmatter sauberer ist als stueckweises Nachziehen — nachdem B5 entschieden ist.

### Empfehlung

**NACHBESSERUNG NOETIG:** Der Rumpf ist gegenueber den getroffenen Entscheidungen grundlegend veraltet
(B1 Anlegen, B2 `SageArbeitsplatznummer`+2. Spalte+Trim, B3 Existenz-Check-Fix, B4 Epic/2 Etappen) und
**drei fachliche Entscheidungen sind noch offen** (B5: Zeilen-Auswahl/Pseudo-Arbeitsplaetze,
Terminal-Werkbank-Auswahl, Storno-Kuerzel), die den Umfang bestimmen. Erst B5 entscheiden, dann Baustein
(a) + Migration + AK neu fassen (H3), dann freigeben.