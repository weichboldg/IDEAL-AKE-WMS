---
type: spec
title: "FA-Struktur Arbeitsgänge als Symbole — Teil 2: Status-Farben bei aktivem BDE"
slug: 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec
status: Testbereit
created: 2026-10-06
updated: 2026-10-13
source_backlog: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]"
anhaenge:
  - "[[sage-fa-struktur-arbeitsschritte.png]]"
# ueberholt (2026-10-13): Der zweite Eintrag zeigte bis 2026-10-12 auf die Backlog-Notiz
# 2026-10-09-arbeitsgang-stammdaten-vereinheitlichen, weil die Spec noch nicht existierte. Seit 2026-10-13 auf die
# Spec umgestellt (Testbereit, Buendel v1.54.0).
depends_on:
  - "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]]"
  - "[[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]"
task: "[[2026-10-13-fa-struktur-arbeitsgaenge-symbole-teil-2-umsetzung]]"
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs + WorkOperationRepository.cs — (1) EINE gebuendelte Abfrage der WorkOperations aller HauptFAs der Seite (Projektion inkl. SubOrderNumber, OperationNumber, ProductionWorkplace.BdeAktiv; Muster WorkOperationRepository.cs:120-128, Contains -> OPENJSON); (2) EINE gebuendelte Status-Abfrage fuer die aufgeloesten WorkOperation-Ids: erledigt = BdeBookingQuantity.IsFinal auf nicht stornierter Buchung (Antwort 3, Praedikat wie BdeApiController.cs:252-256), laeuft = BdeBooking EndedAt == null && !IsCancelled (Antwort 5, Muster BdeBookingRepository.cs:62)"
  - "IdealAkeWms/Services/OseonDueDateCalculator.cs — NUR wiederverwenden (Antwort 2), nicht kopieren: Soll-Termin je Arbeitsgang = Calculate(FE_Termin, Offset, IBusinessDayService, Feiertage) (OseonDueDateCalculator.cs:9-13, v1.49.0 geprueft; Werktage Mo-Fr ohne Feiertage, BusinessDayService.cs:29-50); Feiertage EINMAL je Seitenaufbau ueber IHolidayRepository.GetHolidayDatesAsync (IHolidayRepository.cs:8, Muster OseonGroupViewModelBuilder.cs:35). (ueberholt 2026-10-13: 'Benennt die Vorbedingungs-Spec den Calculator neutral um, folgt Teil 2 dem neuen Namen' — die Vorbedingung hat den Namen bewusst behalten, Out-of-Scope ihrer Spec; OseonDueDateCalculator.cs:9-13 bei v1.54.0 unveraendert)"
  - "Offset-Quelle — entschieden (Antwort 8, 2026-10-09) und umgesetzt (Vorbedingung [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]], v1.54.0): IOperationConfigRepository.GetDueDateOffsetsAsync() (Data/Repositories/OperationConfigRepository.cs:23 Interface, :67-77 Implementierung: alle Zeilen, Schluessel OperationName.Trim(), OrdinalIgnoreCase, kleinste Id gewinnt; fehlender Schluessel = kein Eintrag), gecacht in CachedOperationConfigRepository.cs:41-49 (Key opConfig:offsets, 10 min, Invalidierung nach dem Schreiben :51-62), DI Program.cs:125-126. Teil 2 trimmt den Suchschluessel selbst (Doku OperationConfigRepository.cs:21). EIN Aufruf je Seitenaufbau; ohne Eintrag Offset 0 + sichtbare Meldung 'nicht konfiguriert'. KEINE eigene Pflege, KEINE Migration in Teil 2. VERWORFEN (Antwort 8 'weder A noch B'): Variante A IOseonOperationConfigRepository.GetAllAsDictionaryAsync direkt (Interface heisst seit v1.54.0 IOperationConfigRepository, GetAllAsDictionaryAsync bleibt case-sensitiv fuer OSEON, :61-65); Variante B Feld an ProductionWorkplace"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs — reine, unit-testbare Funktionen: (a) Zuordnung Kuerzel -> WorkOperation(s) (Antwort 6 a + Antwort 7: W-2 kein Rueckfall auf den Vater, W-3 Waisen-Blatt -> kein BDE-Arbeitsgang; Waisen-Erkennung vgl. IsOrphan/OrphanVaterFa FaHierarchyTreeViewModel.cs:55-58, v1.54.0); (b) Status-Ableitung je WorkOperation und Zusammenfassung mehrerer WorkOperations gleichen Kuerzels (W-4); Zustaende kein-BDE-Arbeitsgang/erledigt/laeuft/laeuft+ueberfaellig/ueberfaellig/offen bzw. neutral bei BDE aus (Antworten 2-7, 10). Setzt auf Teil 1 auf (v1.52.0): FaHierarchyTreeNode.Arbeitsgaenge (:97), record FaArbeitsschrittSymbol(Kuerzel, Name) mit Beschriftung 'KA – Name' (:218-222), FaArbeitsschrittSymbole.Tokenize/NamenLookup/Zuordnen (:229-263) — Status als Erweiterung dieser Typen, kein zweiter Symbol-Typ"
  - "IdealAkeWms/Controllers/FaHierarchyController.cs — Index (Z.55-140, v1.54.0; Teil-1-Variante A): nach der Seiten-Paginierung (Z.86-92) und der Teil-1-Symbolzuordnung (Z.100-102) die zwei Abfragen oben + BDE-Gate global (AppSettingKeys.BdeAktiv, IAppSettingRepository ist injiziert Z.29) + Feiertage + Offset-Lookup; FE_Termin aus den bereits geladenen OrderInfos (Z.71 allOrderInfos, structure.OrderInfos), Kombigeraet = fruehester FE_Termin (Antwort 9); NEU ILogger<FaHierarchyController>, IHolidayRepository, IBusinessDayService, IOperationConfigRepository im Ctor (Z.35-53, heute ohne Logger; Teil 1 hat IProductionWorkplaceRepository ergaenzt, Z.43). VERWORFEN (Teil-1-Antwort 2): Status im Modal-Endpunkt WorkOperations (Z.146-162)"
  - "IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml — Statusdarstellung an den Symbolen aus Teil 1 (Symbol-Span Z.108: .fa-ag-symbol, tabindex 0, role img, aria-label/title = Beschriftung) — Farbe + Zeichen + aria-label/title. VERWORFEN (Teil-1-Antwort 2): _FaWorkOperationsModal.cshtml als Ort der Statusdarstellung"
  - "IdealAkeWms/wwwroot/css/site.css — Statusfarben mit WCAG-AA-Kontrast als Varianten von .fa-ag-symbol (site.css:1350-1359, neutraler Umriss-Chip ohne Fuellfarbe, Farbe laut Teil 1 bewusst fuer den Status freigehalten); Kontextzeilen-Dimmung .fa-node-row.fa-node-context opacity 0.6 (:1361) siehe Rueckfrage 11"
  - "IdealAkeWms.Tests — Status-Ableitung (Tabellentest), Zuordnung (Unit), Repository-Abfragen (InMemory), ToQueryString-Test fuer das Contains ueber HauptFAs/Ids. FaHierarchyController wird in keinem Test direkt konstruiert (Grep 'new FaHierarchyController' in IdealAkeWms.Tests: 0 Treffer, v1.54.0)"
  - "Vorbedingung aus [[2026-10-12-operationconfig-offset-leer-pruefen]] (QA-Nachlauf N-3 der Stammdaten-Spec), im SELBEN Dev-Lauf wie Teil 2 erledigen: IdealAkeWms/Controllers/OperationConfigsController.cs — Add (Z.72) und Update (Z.107) binden heute int dueDateOffsetDays ohne ModelState-Pruefung, ein POST ohne/mit leerem Offset speichert still 0 (Add :95, Update :118). Neu: int? dueDateOffsetDays; null (fehlend, leer, nicht numerisch) -> TempData WarningMessage + RedirectToAction(Index), nichts gespeichert, Update aendert auch DisplayName/Relevanz nicht. Views/OperationConfigs/Index.cshtml bleibt unveraendert (required an beiden Offset-Feldern Z.79-80, Z.149). Tests in IdealAkeWms.Tests/Controllers/OperationConfigsControllerTests.cs"
  - "docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md — TS-Kapitel naechste freie Nummer zum Umsetzungszeitpunkt (Stand 2026-10-13: hoechstes TS-87 -> naechstes TS-88); TS-87.3 um den N-3-Negativfall ergaenzen"
  - "Vorbedingung, NICHT Teil dieser Spec: IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs — Scope-Korrektur v1.47.4 (Antwort 7 W-1) ist im Buendel umgesetzt, siehe [[2026-10-07-v1-47-4-arbeitsgang-erkennung-kind-sub-fa]]"
  - "Vorbedingung, NICHT Teil dieser Spec (umgesetzt, Testbereit v1.54.0): Arbeitsgang-Stammdaten (Reiter 'Arbeitsgaenge' in den BDE-Stammdaten, Offset-Pflege, Liste 'nicht konfiguriert', Migration 20261012140548_RenameOperationConfigsAndAddAudit / SQL/95) — [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]"
  - "11 Kontrast gedimmte Kontextzeilen -> C: Dimmung in Teil 2 AA-konform umbauen, AK 8 ohne Ausnahme (Gerald 2026-10-13, Freigabe-Block Nr. 11)"
open_questions: []
beantwortete_rueckfragen:
  - "6 Zuordnung Kuerzel -> WorkOperation (Teil 1 = Quelle S) -> a) Zeile mit SubFA != 0: eigener Arbeitsgang mit gleicher OperationNumber, sonst der des VaterFA; b) Kuerzel ohne WorkOperation: anzeigen, neutral, ohne Status, Hinweis 'kein BDE-Arbeitsgang' (Text/aria-label, nicht nur Farbe); c) WorkOperation ohne Kuerzel: nicht als Symbol zeigen (Gerald 2026-10-06, Freigabe-Block Nr. 6)"
  - "1 Codierung ohne Farbe -> zusaetzlich ein Zeichen je Status: Haken = erledigt, leerer Kreis = offen, Warndreieck = ueberfaellig, eigenes Zeichen fuer 'laeuft'; Statustext per aria-label (Keyuser 2026-10-07, Freigabe-Block Nr. 1)"
  - "2 Ueberfaellig -> Berechnungslogik mit Offset-Werktagen wie OSEON-Reporting/Teileverfolgung (OseonDueDateCalculator), Bezugstermin vorerst FE_Termin (Keyuser + Nachtrag Gerald 2026-10-07, Freigabe-Block Nr. 2). Offen geblieben und ausgelagert: Offset-Pflege -> Nr. 8, Kombigeraete -> Nr. 9"
  - "3 Erledigt -> BDE-Rueckmeldung IsFinal (Keyuser 2026-10-07, Freigabe-Block Nr. 3)"
  - "4 BDE aktiv -> global UND Werkbank aktiv, sonst neutral (Keyuser 2026-10-07, Freigabe-Block Nr. 4)"
  - "5 Laufende Buchung -> eigener Zustand 'laeuft' (Keyuser 2026-10-07, Freigabe-Block Nr. 5)"
  - "7 W-1 -> Variante b: Kuerzel einer Sub-FA-Zeile gehoert nur zum eigenen Sub-FA; umgesetzt als Bugfix v1.47.4 im Buendel (Keyuser 2026-10-07, Freigabe-Block Nr. 7). Nachtrag Gerald 2026-10-09: W-2 kein Rueckfall auf den Vater -> 'kein BDE-Arbeitsgang'; W-3 Waisen-Blatt -> 'kein BDE-Arbeitsgang', keine alten Vater-Arbeitsgaenge; W-4 mehrere WorkOperations gleiches Kuerzel am selben Auftrag -> zusammengefasst (laeuft, wenn einer laeuft; erledigt nur, wenn alle erledigt; sonst offen; ueberfaellig nur fuer nicht erledigte) + Log-Hinweis"
  - "8 Offsets -> weder A noch B: vereinheitlichte Arbeitsgang-Stammdaten (OSEON + IDEAL) als Reiter 'Arbeitsgaenge' in den BDE-Stammdaten, eigene Vorbedingungs-Spec aus [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen]]; Teil 2 liest den Offset von dort; Kuerzel ohne Eintrag: Offset 0, Meldung 'nicht konfiguriert' (Gerald 2026-10-09, Freigabe-Block Nr. 8)"
  - "9 Kombigeraete -> fruehester FE_Termin der Kopfzeilen des HauptFA (Gerald 2026-10-09, Freigabe-Block Nr. 9)"
  - "10 Vorrang laeuft/ueberfaellig -> beides: Status 'laeuft' mit zusaetzlichem Hinweis 'ueberfaellig' (Text + Farbe) (Gerald 2026-10-09, Freigabe-Block Nr. 10)"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "Freigegeben wie Antworten 1-11 inkl. Nachtrag 2026-10-13: Status-Symbole im FA-Baum, Offset aus den Arbeitsgang-Stammdaten (v1.54.0), Kontextzeilen-Dimmung AA-konform ersetzt (Antwort 11 = C), leerer Offset wird abgewiesen (FA 4a/AK 19). Umsetzung direkt im Buendel nach dem Menue-Lauf."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-10-13
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> **Umsetzungsort (Entscheidung Gerald 2026-10-09):** direkt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`
> auf Zweig `feature/2026-08-07-ideal-teile-1-5` (kein eigener Worktree, ein Lauf zur Zeit, kein Warten auf
> Schranke 2 des Bündels). `worktree`/`branch` im Frontmatter bleiben leer — der Ort steht hier fest.
> Code-Grundlage ist der Bündel-Stand; Zeilenangaben ohne Zusatz stammen aus v1.47.1, die im Rework 2026-10-12
> angefassten oder neu gesetzten sind gegen **v1.49.0** geprüft (Zusatz „v1.49.0“ bzw. unverändert bestätigt).
> Version und TS-Nummer: nächste freie **im Bündel** zum Umsetzungszeitpunkt (Stand 2026-10-12: v1.49.0,
> höchstes Kapitel TS-82; Teil 1 und die Vorbedingungs-Spec liegen davor). (Vormals: „… nach dem Merge gegen
> `main` nachzuprüfen … `worktree`/`branch` setzt der Dev-Lauf“ — überholt.)
> **Nachmessung 2026-10-13 (Stand überholt den von 2026-10-12):** Bündel-Worktree HEAD `d56a340e`, **v1.54.0**
> (`IdealAkeWms/AppVersion.cs:5`); höchstes Kapitel **TS-87** (`docs/TESTSZENARIEN.md:9628`) → nächstes **TS-88**;
> höchstes Skript **`SQL/95_RenameOperationConfigsAndAddAudit.sql`**; letzte Migration
> **`20261012140548_RenameOperationConfigsAndAddAudit`**; Hauptcheckout (`main`) höchstes Skript `SQL/88_*`. Teil 1
> (v1.52.0) und die Vorbedingung (v1.54.0) sind umgesetzt und Testbereit. Teil 2 bringt keine Migration (die nächste
> freie SQL-Nummer ist für Teil 2 ohne Belang). Zeilenangaben: Was der Abgleich 2026-10-13 neu gemessen hat, trägt
> „(v1.54.0)“; die Konkordanz alter → neuer Zeilen steht im Abschnitt „Abgleich mit der Umsetzung (2026-10-13)“. Der
> Dev-Lauf sucht per Bezeichner, nicht per Zeilennummer.
>
> **Vorbedingung Arbeitsgang-Stammdaten (neu, Antwort 8, 2026-10-09):** Den Offset je Kürzel liefert die
> Vorbedingungs-Spec [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] (aus der Backlog-Notiz
> [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen]]). ~~Spec existierte am 2026-10-12 noch nicht; `depends_on`
> zeigt deshalb auf die Backlog-Notiz und ist auf die Spec umzustellen, sobald sie existiert. Teil 2 startet erst,
> wenn diese Vorbedingung im Bündel umgesetzt ist.~~ — überholt (2026-10-13): Die Vorbedingung ist im Bündel
> umgesetzt (v1.54.0, Status Testbereit), `depends_on` zeigt auf die Spec. Die Schnittstelle erfüllt FA 4
> „Schnittstelle zur Vorbedingung“: `IOperationConfigRepository.GetDueDateOffsetsAsync()`, Belege im Abgleich-Abschnitt.
> **Neu im selben Dev-Lauf:** die Folgeaufgabe [[2026-10-12-operationconfig-offset-leer-pruefen]] (leerer Offset
> serverseitig abweisen), siehe Umfang, FA 4a, AK 19.
>
> **Abhängigkeit:** setzt [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]] voraus (die
> Symbole, die hier eingefärbt werden). Teil 1 hat **Ort A** gewählt (Symbole direkt in den Baumzeilen,
> Teil-1-Antwort 2, Keyuser 2026-10-07) — die Status-Abfrage läuft daher im Seitenaufbau `Index`. (Vorher:
> „folgt aus Rückfrage 2 von Teil 1 (Ort A/B)“ — überholt.) Zusätzlich setzt diese Spec die Scope-Korrektur
> der Arbeitsgang-Erkennung **v1.47.4** voraus (Antwort 7 W-1, [[2026-10-07-v1-47-4-arbeitsgang-erkennung-kind-sub-fa]],
> FREIGABE-NACHTRAG 2026-10-07 in [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]), die mit dem
> Bündel gemergt wird.
>
> **Zeilenangaben `WorkOperationStructureDetectionService.cs`:** Die Belege unten stammen aus v1.47.1. Seit
> v1.47.4 (Bündel-Commit `abd3e16a`) sind alle Zeilen ab alter Z.92 um **+2** verschoben (z. B. Scope alt
> `:101-102` → neu `:103-104`, Auftragsfilter alt `:142-145` → neu `:144-147`, Re-Add-Sperre alt `:151-163` →
> neu `:153-165`, `OperationNumber = token` alt `:169` → neu `:171`); Z.1-91 unverändert. Neue Belege in diesem
> Rework tragen den Zusatz „(v1.47.4)“. **Nachgemessen 2026-10-13:** bei v1.54.0 sind die v1.47.4-Zeilen unverändert
> (u. a. Scope `:103-104`, Re-Add-Sperre `:153-165`, `OperationNumber = token` `:171`); Konkordanz im Abschnitt
> „Abgleich mit der Umsetzung (2026-10-13)“.
>
> Teil 1 zeigt **Quelle S** (Sage-Kürzel je Baumzeile, Teil-1-Antwort 1, 2026-10-06); daher ist
> die Zuordnung Kürzel → `WorkOperation` Pflicht — Regel aus Antwort 6, siehe „Fachliche Anforderungen“ 8/9.
> (Vorher: „ob zusätzlich eine Zuordnung nötig ist, folgt aus Rückfrage 1 von Teil 1“ — überholt.) Vorbild
> der Darstellung ist der Sage-Screenshot im Anhang (Kürzel je Baumzeile in einer Spalte).

## Ziel / Nutzen (das Warum)

Bei aktivem BDE soll an den Arbeitsgang-Symbolen aus Teil 1 der Fortschritt ablesbar sein:
**Grün = erledigt, Grau = offen, Rot = überfällig** (Anforderung Gerald,
[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]). Planung und Fertigungsleitung sehen so je Auftrag,
welche Arbeitsgänge fehlen oder hängen, ohne das BDE-Cockpit zu durchsuchen. Standort: **IDEAL**
(Arbeitsgänge aus der Struktur-Erkennung, Termine aus den IDEAL-Sage-Views).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Status je Arbeitsgang ableiten (live, nicht gespeichert) und am Symbol aus Teil 1 in den **Baumzeilen**
  (Teil-1-Antwort 2) darstellen: **erledigt / läuft / überfällig / offen** (Antworten 3, 5, 2), die
  Kombination **läuft + überfällig** (Antwort 10) plus „kein BDE-Arbeitsgang“ (Antwort 6 b, 7 W-2/W-3).
- Zusammenfassung mehrerer `WorkOperation`s gleichen Kürzels am selben Auftrag zu **einem** Symbolstatus plus
  Log-Warnung (Antwort 7 W-4).
- Statuscodierung über Farbe **und** Zeichen **und** Text (Antwort 1).
- Gate „BDE aktiv“: global **und** Werkbank (Antwort 4).
- Soll-Termin je Arbeitsgang = `FE_Termin` (Kombigerät: frühester, Antwort 9) + Offset-Werktage über den
  vorhandenen `OseonDueDateCalculator` (Antwort 2); Offset per Lookup aus den Arbeitsgang-Stammdaten der
  Vorbedingung (Antwort 8), ohne Eintrag 0 + sichtbare Meldung „nicht konfiguriert“.
- **Vorbedingung aus [[2026-10-12-operationconfig-offset-leer-pruefen]] (neu 2026-10-13, im selben Dev-Lauf):**
  `OperationConfigsController.Add`/`Update` weisen einen fehlenden/leeren Offset serverseitig ab (FA 4a, AK 19). Grund:
  Ab Teil 2 bestimmt der Offset „überfällig“; ein still gespeicherter Offset 0 verschiebt den Soll-Termin
  unbemerkt (zu früh oder zu spät rot) und verdeckt den Pflegefehler.

**Out-of-Scope**
- Kein gespeichertes Soll-Datum je Arbeitsgang (der Soll-Termin wird beim Laden berechnet, Antwort 2).
- Pflege der Offsets, Stammdaten-Reiter „Arbeitsgänge“, Liste „nicht konfiguriert“ in den Stammdaten,
  Umbenennung „Fremdsystem-relevant“ und jede dafür nötige Migration → Vorbedingungs-Spec
  ([[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]], Antwort 8; umgesetzt v1.54.0) — **einzige Ausnahme**
  die serverseitige Offset-Prüfung aus [[2026-10-12-operationconfig-offset-leer-pruefen]] (In-Scope oben, FA 4a). (Vormals „Ob für die **Offsets** eine
  Migration entsteht, entscheidet Rückfrage 8 (Variante B)“ — überholt.)
- Die Scope-Korrektur der Arbeitsgang-Erkennung (W-1) — erledigt im Bündel als v1.47.4, siehe oben.
- Modal `_FaWorkOperationsModal` bleibt ohne Status (Teil-1-Antwort 2: Modal bleibt für Details).
- Keine Änderung an BDE-Buchungslogik, Terminal, Tracking-Rückmeldung.
- BDE-Auftragsanzeige → Teil 3.

## Fachliche Anforderungen

1. **Status ist abgeleitet, nicht gespeichert.** Die Quellen können sich nachträglich ändern:
   Buchungskorrekturen setzen `IsFinal` um (`BdeBookingsController.cs:236`, `q.IsFinal = row.IsFinal`),
   Buchungen können storniert werden (`IsCancelled`), die Tracking-Rückmeldung ist umkehrbar
   (`TrackingController.cs:283-286`, `UndoReport`). Ein gespeicherter Status würde veralten.
2. **„Erledigt“ — entschieden: BDE-Rückmeldung `IsFinal` (Antwort 3, Keyuser 2026-10-07).** Erledigt heißt:
   Es existiert eine `BdeBookingQuantity` mit `IsFinal = true` auf einer nicht stornierten Buchung dieses
   Arbeitsgangs — dasselbe Prädikat wie das Terminal. `IsReported` wird **nicht** ausgewertet. Befund am Code
   (vormals „entscheidet Rückfrage 3“ — überholt):
   - Das BDE-Terminal betrachtet einen Arbeitsgang als abgeschlossen, wenn eine
     `BdeBookingQuantity` mit `IsFinal = true` auf einer **nicht stornierten** Buchung dieses Arbeitsgangs
     existiert (`BdeApiController.cs:251-257`, Ausschluss aus der Auswahl `:273`). `IsFinal = true`
     schreibt `BdeBookingService` beim Beenden mit Menge (`BdeBookingService.cs:357-361`).
   - `WorkOperation.IsReported`/`ReportedAt` werden im gesamten Code **nur** von der manuellen
     Rückmeldung im Tracking-Modul geschrieben: `TrackingController.Report` (`TrackingController.cs:254-255`,
     `ReportedAt = DateTime.UtcNow`) und `UndoReport` (`:283-284`); Anlage mit `IsReported = false`
     (`WorkOperationStructureDetectionService.cs:175`). Weder `BdeBookingService` noch ein Service-Sync noch
     ein Skript unter `SQL/` (inkl. `SQL/AgentJobs/`) setzt sie (Grep über `*.cs`/`*.sql`, Migrationen
     ausgenommen). **Die BDE-Buchung setzt `IsReported` also nicht.**
   - `IsReportable` wird bei IDEAL immer `true` gesetzt (`WorkOperationStructureDetectionService.cs:173`,
     `BdeDefaultWorkOperationService.cs:41`) und nur im Tracking gelesen (`_OperationStatusBadge.cshtml:10`,
     `Views/Tracking/*`) — als Statusquelle hier ohne Aussagekraft.
   - Achtung Zeitbasis: `ReportedAt` ist **UTC** (`TrackingController.cs:255`), `BdeBookingQuantity.ReportedAt`
     ist **Lokalzeit** (`BdeBookingService.cs:386`, `DateTime.Now`). Wird ein Zeitpunkt angezeigt oder
     verglichen, muss das berücksichtigt werden.
3. **„Läuft“ — eigener Zustand (Antwort 5, Keyuser 2026-10-07):** nicht erledigt **und** es gibt eine
   Buchung auf den Arbeitsgang mit `EndedAt == null` und `!IsCancelled` (Muster `BdeBookingRepository.cs:62`,
   `BdeApiController.cs:260-268`). Pausierte Buchungen haben `EndedAt` gesetzt ([[fallstricke]] §6) und
   zählen **nicht** als „läuft“. Ist der Soll-Termin zugleich überschritten, gilt **„läuft“ mit zusätzlichem
   Hinweis „überfällig“** (Antwort 10, 2026-10-09; vormals „Vorrang gegenüber ‚überfällig‘: Rückfrage 10“ —
   überholt), siehe FA 7/7a.
3a. **„Offen“ (grau):** nicht erledigt, nicht laufend, nicht überfällig. (Vormals „Laufende Buchung:
   Rückfrage 5“ — überholt.)
4. **„Überfällig“ (rot) — entschieden: Offset-Logik wie OSEON-Reporting/Teileverfolgung, Bezug `FE_Termin`
   (Antwort 2 + Nachtrag Gerald 2026-10-07).** Nicht erledigt **und** Soll-Termin < heute (Lokaldatum,
   nur Datumsteil). Soll-Termin = `OseonDueDateCalculator.Calculate(FE_Termin, Offset, …)`:
   - Calculator (`IdealAkeWms/Services/OseonDueDateCalculator.cs:9-13`): Offset 0 → `baseDate.Date`, sonst
     `IBusinessDayService.AddBusinessDays(baseDate, offset, holidays)`; Werktage = Mo-Fr ohne Feiertage aus
     dem übergebenen Set (`BusinessDayService.cs:29-49`), negativer Offset zählt rückwärts.
   - **Wie die bestehenden Aufrufer den Bezugstermin bekommen (belegt):** OSEON-Reporting
     `OseonReportingController.cs:74-77` — Bezug `OseonProductionOrder.DueDate` (OSEON-Endtermin), Offset aus
     `OseonOperationConfig.DueDateOffsetDays` über die AG-Konfiguration der Zeile; ohne Termin **oder** ohne
     Konfiguration wird die Zeile übersprungen (`:74`); überfällig = `calcDue < today && OseonStatus != 90`
     (`:80`). Teileverfolgung `OseonGroupViewModelBuilder.cs:43-53` (genutzt von `TrackingController`, Z.23/35)
     — Konfiguration per `opConfigs.TryGetValue(op.Name, …)`; **ohne** Konfiguration gilt der Auftragstermin
     selbst (`:52`, Offset 0). Feiertage: `IHolidayRepository.GetHolidayDatesAsync()` (`:35`) bzw.
     `GetAllAsync` (`OseonReportingController.cs:68-69`).
   - *(Befund v1.49.0, vor der Umbenennung — historisch; seit v1.54.0 heißen Klasse/Tabelle/Spalte `OperationConfig` /
     `dbo.OperationConfigs` / `IsExternalSystemRelevant`, Repository `OperationConfigRepository`, Pflege unter
     `/OperationConfigs` (`/Settings/OperationConfig` leitet per 302 dorthin), siehe Abgleich-Abschnitt.)*
     `OseonOperationConfig.DueDateOffsetDays` (`Models/OseonOperationConfig.cs:22-26`, Doku „Offset in
     Arbeitstagen relativ zum Stanztermin“) ist je `OperationName` (OSEON-AG-Kurzname, `:10-14`) gepflegt,
     Seed `Program.cs:465-493` (u. a. `EG` „Entgraten“ Offset 0, `Program.cs:471`), Pflege unter
     `/Settings/OperationConfig` (`SettingsController.cs:110-164`, Klasse `[RequireAdminAccess]` Z.10). Das
     Dictionary ist **case-sensitiv** (`OseonOperationConfigRepository.cs:46-50`, `ToDictionaryAsync` ohne
     Comparer); die „noch nicht konfiguriert“-Liste liest nur `OseonWorkOperations` (`:55-67`) und kennt
     IDEAL-Kürzel nicht. ~~**Wo die Offsets je IDEAL-Kürzel gepflegt werden und was ohne Eintrag gilt, ist offen
     — Rückfrage 8.**~~ — **entschieden (Antwort 8, 2026-10-09):** Offsets kommen aus den vereinheitlichten
     Arbeitsgang-Stammdaten der Vorbedingung; ohne Eintrag Offset 0 + Meldung „nicht konfiguriert“.
   - **Befund `OseonDueDateCalculator` (Rework 2026-10-12, v1.49.0 geprüft):**
     - *Was er berechnet:* statische Methode `Calculate(DateTime baseDate, int offsetDays, IBusinessDayService,
       HashSet<DateTime> holidays)` (`OseonDueDateCalculator.cs:9-13`) — Offset 0 → `baseDate.Date` (`:11`), sonst
       `AddBusinessDays` (`:12`). Er kennt **keine** Offset-Quelle und keine Konfiguration; der Offset ist ein
       reiner Parameter.
     - *Werktage, nicht Kalendertage:* `BusinessDayService.AddBusinessDays` (`BusinessDayService.cs:29-50`) zählt
       nur Mo-Fr, die nicht im übergebenen Feiertags-Set stehen; negative Werte zählen rückwärts (`:33`).
     - *Offset-Quelle bei den heutigen Aufrufern:* beide holen `OseonOperationConfig.DueDateOffsetDays` aus
       `OseonOperationConfigs` — OSEON-Reporting über die AG-Konfiguration der Zeile
       (`OseonReportingController.cs:74-77`, ohne Konfiguration übersprungen), Teileverfolgung über
       `GetAllAsDictionaryAsync` (`OseonGroupViewModelBuilder.cs:34`) und `TryGetValue(op.Name, …)` (`:43`), ohne
       Konfiguration Offset 0 (`:50-52`). Schlüssel ist dort der OSEON-AG-Name, der Dictionary-Vergleich ist
       case-sensitiv (`OseonOperationConfigRepository.cs:46-50`). Zusätzlich liest
       `OseonProductionOrderRepository.cs:215` den größten Offset-Betrag (Datumsfenster).
     - *Folge für Teil 2:* Teil 2 nutzt **dieselbe Rechenregel** durch Aufruf des vorhandenen Calculators
       (Werktage, Feiertage aus `IHolidayRepository.GetHolidayDatesAsync`, `IHolidayRepository.cs:8`); er
       übernimmt nur den Offset aus einer anderen Quelle. Ob der Calculator umbenannt, verschoben oder die
       OSEON-Aufrufer auf die neuen Stammdaten umgestellt werden, **entscheidet die Vorbedingungs-Spec** — Teil 2
       folgt deren Ergebnis und ändert den Calculator nicht.
       **Ergebnis der Vorbedingung (Abgleich 2026-10-13):** Der Calculator heißt weiter `OseonDueDateCalculator` und
       ist unverändert (`Services/OseonDueDateCalculator.cs:3-13`, v1.54.0; `git diff` der Vorbedingung dort leer laut
       deren QA). Die OSEON-Aufrufer lesen dieselbe, nur umbenannte Tabelle mit unverändertem Verhalten
       (`OseonGroupViewModelBuilder.cs:34-35`, `:43`, `:51`; `OseonReportingController.cs:68-69`, `:76-77`). Für Teil 2
       bleibt es beim Aufruf `OseonDueDateCalculator.Calculate(...)`.
   - **Schnittstelle zur Vorbedingung (was Teil 2 erwartet; Name und Ort legt die Vorbedingungs-Spec fest):**
     - **ein** Aufruf je Seitenaufbau, der alle Offsets liefert (gecacht zulässig), z. B.
       `Task<IReadOnlyDictionary<string, int>> GetDueDateOffsetsAsync()` an einem Repository der
       Arbeitsgang-Stammdaten — Schlüssel = Kürzel (`OperationName`), Wert = Offset in Arbeitstagen zum
       Bezugstermin;
     - Schlüsselvergleich **case-insensitiv** und getrimmt (wie die Erkennung,
       `WorkOperationStructureDetectionService.cs:80`); liefert die Vorbedingung ein case-sensitives Dictionary,
       baut Teil 2 es mit `StringComparer.OrdinalIgnoreCase` um;
     - „kein Eintrag“ ist vom Wert 0 unterscheidbar (fehlender Schlüssel) — Teil 2 braucht das für die Meldung
       „nicht konfiguriert“;
     - die Relevanz-Flag („Fremdsystem-relevant“) wertet Teil 2 **nicht** aus.
     - **Umgesetzt (Abgleich 2026-10-13, v1.54.0) — Erwartung erfüllt:** `IOperationConfigRepository`
       (`Data/Repositories/OperationConfigRepository.cs:9-32`) mit
       `Task<IReadOnlyDictionary<string, int>> GetDueDateOffsetsAsync()` (`:23`); Implementierung `:67-77` liest **alle**
       Zeilen von `dbo.OperationConfigs` (DbSet `OperationConfigs`, Klasse `Models/OperationConfig.cs:11`, erbt
       `AuditableEntity`), sortiert nach `Id`, Schlüssel `OperationName.Trim()`, Comparer `OrdinalIgnoreCase`, `TryAdd` →
       bei doppeltem normalisiertem Schlüssel gewinnt die kleinste `Id`; Wert `DueDateOffsetDays` (`OperationConfig.cs:28`,
       Doku „Offset in Arbeitstagen zum Bezugstermin … IDEAL FE-Termin“, `:24-27`). Die Relevanz-Spalte heißt jetzt
       `IsExternalSystemRelevant` (`:34`) und wird vom Lookup nicht gelesen. Fehlender Schlüssel = kein Eintrag. Der
       Aufrufer trimmt seinen Suchschlüssel selbst (Doku `:21`) — `WorkOperation.OperationNumber` ist bei IDEAL ohnehin
       das getrimmte Token (`WorkOperationStructureDetectionService.cs:112-113`, `:171`, v1.54.0), Teil 2 trimmt trotzdem.
       Decorator `CachedOperationConfigRepository.cs:41-49` (Key `opConfig:offsets`, 10 min, Invalidierung **nach** dem
       Schreiben `:51-62`), DI `Program.cs:125-126`. Der Satz „liefert die Vorbedingung ein case-sensitives Dictionary,
       baut Teil 2 es mit `StringComparer.OrdinalIgnoreCase` um“ ist damit **überholt** — kein Umbau nötig.
   - **Kürzel ohne Stammdaten-Eintrag (Antwort 8):** Offset 0 (Soll-Termin = `FE_Termin`). Sichtbare Meldung:
     **ein** Sammelhinweis oberhalb des Baums (Bootstrap `alert-warning`, `role="status"`) „Für N Arbeitsgang-Kürzel
     ist kein Termin-Offset konfiguriert (Offset 0 angenommen): KA, SW … — Pflege unter BDE-Stammdaten ›
     Arbeitsgänge.“ Nur für Kürzel, die auf der Seite einen Status tragen (BDE an, `WorkOperation` vorhanden,
     Werkbank aktiv). Eigenes ViewModel-Feld, **nicht** `TempData["WarningMessage"]` — das belegt der
     Tiefen-Cap-Hinweis (`FaHierarchyController.cs:104-110`, v1.54.0; vormals `:98-103`, v1.49.0) und würde
     überschrieben. Die dauerhafte Liste „nicht konfiguriert“ in den Stammdaten gehört zur Vorbedingung und ist
     umgesetzt (`OperationConfigRepository.GetUnconfiguredOperationsAsync`, `:82-97`: OSEON-Namen + WMS-`OperationNumber`
     ohne Eintrag, gleiche Normalisierung wie der Lookup, ohne `"01"` `:80`, `:92`). Der Pflegeort-Text passt zur
     Umsetzung: Reiter „Arbeitsgänge“ in der BDE-Stammdaten-Reiterleiste (`Views/Shared/_BdeMasterDataTabs.cshtml:32`),
     Menüeintrag „Arbeitsgänge“ im Admin-Block des Stammdaten-Menüs (`Views/Shared/_Layout.cshtml:315`), Kapitel
     TS-87 „Arbeitsgang-Stammdaten: BDE-Stammdaten › Arbeitsgänge“; erreichbar nur für Admin
     (`OperationConfigsController.cs:18`), unabhängig von `BdeAktiv`.
   - **`FE_Termin` je Knoten/HauptFA — belegt:** `FE_Termin` liegt nur an `FaHierarchyOrderInfo`
     (`Models/FaHierarchyOrderInfo.cs:32`), Schlüssel `HauptFA` (`:17`); `FaHierarchyNode` hat kein Datumsfeld
     außer `SyncedAt` (`FaHierarchyNode.cs:62`). Im `Index` sind **alle** OrderInfos bereits geladen
     (`FaHierarchyController.cs:71`, v1.54.0) und hängen je Struktur an `structure.OrderInfos`
     (`FaHierarchyTreeBuilder.cs:104`) — **keine zusätzliche Abfrage**. Jeder Knoten erbt den Termin seiner
     HauptFA; Sub-FA-eigene Termine gibt es nicht. Bei genau einer Kopfzeile ist der Termin eindeutig (Muster
     `ProductionOrdersController.cs:294-305`); bei mehreren Kopfzeilen (Kombigerät, `structure.IsAmbiguous`,
     `Index.cshtml:151-153`) ist er es nicht → ~~**Rückfrage 9**~~ **entschieden (Antwort 9, 2026-10-09): der
     früheste nicht leere `FE_Termin` der Kopfzeilen des HauptFA.** Die Kopfzeile der Baumanzeige bleibt dabei
     ohne Einzeltermin (`Index.cshtml:151-153` unverändert) — der früheste Termin wirkt nur als Bezug für
     „überfällig“ und erscheint im `aria-label`/`title` des Symbols („Soll TT.MM.JJJJ“). Ohne Kopfzeile oder wenn
     alle `FE_Termin` leer sind: nie überfällig.
   - ~~Kandidat `Neuer_PT_PPS`~~ — verworfen (Nachtrag Gerald 2026-10-07: „vorerst `FE_Termin`“).
   - (Ursprünglicher Text: „der in Rückfrage 2 festgelegte Auftragstermin liegt vor heute“ — überholt.)
4a. **Offset-Pflege: fehlender/leerer Offset wird serverseitig abgewiesen (neu 2026-10-13, Vorbedingung aus
   [[2026-10-12-operationconfig-offset-leer-pruefen]], QA-Nachlauf N-3 der Stammdaten-Spec).**
   - *Ist (v1.54.0, belegt):* `OperationConfigsController.Add(string? operationName, string? displayName,
     int dueDateOffsetDays, bool isExternalSystemRelevant)` (`OperationConfigsController.cs:72`) und
     `Update(int id, string? displayName, int dueDateOffsetDays, bool isExternalSystemRelevant)` (`:107`) binden den
     Offset als `int` ohne `ModelState`-Prüfung und schreiben ihn direkt (`:95` bzw. `:118`). Fehlt der Wert im POST
     oder ist er leer/nicht numerisch, bleibt der Parameter auf dem Default `0` (bei leerem/nicht numerischem Wert
     zusätzlich ein ModelState-Fehler, den die Action nicht prüft) → still Offset 0 gespeichert (Befund der
     Stammdaten-QA, N-3). Die View verlangt den Wert nur im Browser (`required`,
     `Views/OperationConfigs/Index.cshtml:79-80` Zeilenfeld, `:149` Anlegen) — Defektklasse „nur im Browser validiert“.
   - *Soll:* Parameter `int? dueDateOffsetDays` in `Add` und `Update`. Ist er `null`, gilt: `TempData["WarningMessage"]`
     (Text z. B. „Offset (Arbeitstage) fehlt – nichts gespeichert.“) + `RedirectToAction(nameof(Index))`, **kein**
     Repository-Schreibzugriff — bei `Update` bleiben auch `DisplayName`/`IsExternalSystemRelevant` und die
     Audit-Felder unverändert. Reihenfolge der Prüfungen: nach der Leer-/Längenprüfung des Kürzels bzw. nach
     `GetByIdAsync`/`NotFound` (bestehende Frühausstiege `:75-89`, `:109-115` bleiben). Ein gültiger Wert (auch `0` oder
     negativ) wird wie heute gespeichert. Kein `TempData["ErrorMessage"]` (CLAUDE.md: nur Success/Warning).
   - *Unverändert:* `required` im Formular, Längenprüfung, Audit-Setzung (`:97-99`, `:120-122`), Rolle
     `[RequireAdminAccess]` (`:18`), kein `[RequireBdeActive]`.
5. **Termine — Befund an der Quelle (Grundlage der Antwort 2; Bedeutung von `FE_Termin` weiter unverifiziert,
   P1):**
   - `FaHierarchyOrderInfo.KO_Termin`/`FE_Termin`/`Start_Beschichtung` (und `Neuer_PT_PPS`,
     `Verladetermin_Vsl`) sind **1:1-Kopien** der Spalten der Sage-View
     `vw_IDEAL-AKE_Kommissionierung_FAInfos` (`FaHierarchySql.cs:135-143`, Zuordnung
     `FaHierarchySyncService.cs:285-290`), keine Umrechnung.
   - Bedeutung laut View-Dokumentation (`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAInfos.sql:26-37`):
     `KO_Termin` „Konstruktions-Termin“, `FE_Termin` „Wann muss die Blechfertigung fertig sein“,
     `Start_Beschichtung` „Wann Beschichtung starten“, `Neuer_PT_PPS` „Aktueller PPS-Fertigstellungstermin“.
     Quelle der View ist `[PPSImport].[dbo].[vw_PPSData]` (Z.9); Spaltennamen/Typen sind gegen die reale View
     verifiziert (Z.13), der **View-Text selbst ist nicht im Repo** (Z.16-17, Z.50) — die Beschreibungen sind
     damit **nicht** am Datenfluss geprüft. **Unverifiziert**, siehe Prüfschritt P1.
   - Termine liegen **nur je HauptFA**, nicht je Sub-FA: `FaHierarchyNode` hat kein Datumsfeld außer
     `SyncedAt` (`FaHierarchyNode.cs:62`). Bei Kombigeräten gibt es mehrere `FaHierarchyOrderInfo`-Zeilen je
     HauptFA (`FaHierarchyOrderInfo.cs:7-9`), die Kopfzeile der Baumanzeige zeigt dann bewusst **keinen**
     Termin (`Index.cshtml:151-153`).
   - Bestehende Verwendung zur Orientierung: Die FA-Liste nutzt `FE_Termin` als Produktionsdatum
     (`ProductionOrdersController.cs:299`), die Vormontage-Listen filtern auf `FE_Termin`, die
     Kommissionierlisten auf `KO_Termin` (`VormontageService.cs:169`, `KommissionierListenService.cs:164`).
6. **Gate „BDE aktiv“ — entschieden: beide Schalter (Antwort 4, Keyuser 2026-10-07).** Status nur, wenn
   global `AppSettingKeys.BdeAktiv` (`AppSettingKeys.cs:9`; gelesen von `RequireBdeActiveAttribute.cs:24` und
   `HomeController.cs:36`) **und** die Werkbank des Arbeitsgangs `ProductionWorkplace.BdeAktiv`
   (`ProductionWorkplace.cs:21`; Terminal-Muster `BdeApiController.cs:197`) aktiv sind. Sonst: Symbol neutral
   wie Teil 1, **ohne** Statusfarbe und ohne Statuszeichen. Das globale Gate gilt je Seitenaufbau, das
   Werkbank-Gate je Arbeitsgang (eine Struktur kann gemischt sein). Der Hinweis „kein BDE-Arbeitsgang“
   (Antwort 6 b) erscheint nur bei globalem BDE an — bei BDE aus sind alle Symbole neutral wie Teil 1
   (Festlegung aus „sonst neutral“).
   **Hinweis (Rework 2026-10-12):** Laut Entscheidung 1 der Backlog-Notiz, übernommen in
   [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] (dort Prüfschritt P5)
   ist `BdeAktiv` derzeit an **keinem** Standort aktiv (Wert in der DB vom Spec-Lauf nicht geprüft). Bis BDE
   eingeschaltet wird, zeigt Teil 2 produktiv keine Status — die Abnahme läuft am Testsystem mit BDE an.
7. **Leitplanken und Codierung (Antwort 1, Keyuser 2026-10-07):** Farbe ist nie alleiniger
   Bedeutungsträger. Je Status ein **Zeichen** zusätzlich zur Farbe, Statustext per `aria-label` (zusätzlich
   `title` für Maus; `title` allein reicht nicht, Touch):

   | Status | Farbe | Zeichen | Text (`aria-label`, Kürzel vorangestellt) |
   |---|---|---|---|
   | erledigt | grün | Haken | „KA – erledigt“ |
   | läuft | eigene Farbe (Vorschlag: blau/info) | eigenes Zeichen, formverschieden von Dreieck und Kreis (Vorschlag: Sanduhr bzw. Play) | „KA – läuft“ |
   | **läuft + überfällig** (Antwort 10) | Läuft-Farbe **plus** roter Akzent (z. B. roter Rand/rotes Eck-Badge) | Läuft-Zeichen **plus** kleines Warndreieck | „KA – läuft, überfällig (Soll TT.MM.JJJJ)“ |
   | überfällig | rot | Warndreieck | „KA – überfällig (Soll TT.MM.JJJJ)“ |
   | offen | grau | leerer Kreis | „KA – offen“ |
   | kein BDE-Arbeitsgang | neutral wie Teil 1 | keins | „KA – kein BDE-Arbeitsgang“ |

   **Text-Angleichung an Teil 1 (Rework 2026-10-12):** Teil 1 setzt `aria-label`/`title` = Werkbank-Name bzw.
   Kürzel ohne eindeutigen Treffer. Teil 2 **ergänzt** diesen Text um den Status, statt ihn zu ersetzen:
   `„<Kürzel> <Name> – <Status>“` (z. B. „KA Kanten – erledigt“; ohne Namen „STO – kein BDE-Arbeitsgang“). Die
   Kurzformen in der Tabelle zeigen nur den Statusteil. „Läuft + überfällig“: keine Information geht verloren
   (Antwort 10) — beide Zeichen, beide Texte; Farbe bleibt nie alleiniger Träger.
   **Abgleich mit Teil 1 (2026-10-13):** ~~„Teil 1 setzt `aria-label`/`title` = Werkbank-Name bzw. Kürzel“ und Format
   `„<Kürzel> <Name> – <Status>“` (z. B. „KA Kanten – erledigt“)~~ — überholt. Umgesetzt hat Teil 1
   `FaArbeitsschrittSymbol.Beschriftung` = **„KA – Name“**, ohne eindeutigen Katalog-Treffer nur „KA“
   (`FaHierarchyTreeViewModel.cs:218-222`, v1.54.0; Name = `ProductionWorkplace.Name` bei genau einer Werkbank je
   `ArbeitsschrittCode`, `:241-248`), gerendert als `aria-label`/`title` (`_FaHierarchyNode.cshtml:108`). Gemäß dem
   Grundsatz oben („ergänzt, statt zu ersetzen“) gilt daher: **`„<Beschriftung> – <Status>“`**, z. B. „KA – Kanten –
   erledigt“, „KA – Kanten – läuft, überfällig (Soll 04.05.2026)“, ohne Namen „STO – kein BDE-Arbeitsgang“. Teil 1
   dedupliziert nicht (Sage 1:1, Teil-1-Ruling R1): steht ein Kürzel zweimal in einer Zeile, tragen beide Symbole
   denselben Status (gleiche Zuordnung).
   Farbe und Zeichen für „läuft“ (und die Kombination) legt der Dev-Lauf mit dem Skill `frontend-design` fest
   (Antwort 1 sagt nur „eigenes Zeichen“). Zeichen als Inline-SVG wie die bestehenden Typ-Icons (`_FaHierarchyNode.cshtml:28-54`, v1.54.0),
   keine neue Icon-Bibliothek. Kontrast WCAG AA (4,5:1 Text, 3:1 Konturen/Zeichen), auch bei Rot/Grün-Sehschwäche
   unterscheidbar (Graustufen-Test).
7a. **Vorrang der Zustände (je `WorkOperation`):** kein BDE-Arbeitsgang (ohne `WorkOperation`) → erledigt →
   läuft (mit Zusatz „überfällig“, wenn Soll-Termin < heute — Antwort 10) → überfällig → offen. (Vormals
   „{läuft, überfällig} (Reihenfolge zwischen beiden: Rückfrage 10)“ — überholt.)
7b. **Zusammenfassung mehrerer `WorkOperation`s gleichen Kürzels (Antwort 7 W-4, 2026-10-09):** Trifft die
   Zuordnung (FA 9 a) für ein Symbol **mehrere** `WorkOperation`s desselben Auftrags mit gleichem Kürzel
   (case-insensitiv), wird erst je `WorkOperation` der Zustand nach 7a bestimmt und dann zusammengefasst:
   - **läuft**, wenn mindestens eine „läuft“;
   - sonst **erledigt** nur, wenn **alle** erledigt sind;
   - sonst **offen**;
   - **„überfällig“** (bzw. der Zusatz bei „läuft“) nur, wenn das Ergebnis nicht „erledigt“ ist und der
     Soll-Termin < heute. Da alle Treffer dasselbe Kürzel und denselben Auftrag haben, gibt es genau einen
     Soll-Termin.
   - Werkbank-Gate (FA 6): zählen nur die `WorkOperation`s mit BDE-aktiver Werkbank; ist keine aktiv → neutral.
   - Weil per 7a eine erledigte `WorkOperation` nie „läuft“, schließen sich „mindestens eine läuft“ und „alle
     erledigt“ aus.
   - **Log-Hinweis:** je Seitenaufbau **eine** `LogWarning` über `ILogger<FaHierarchyController>` mit allen
     betroffenen Paaren (Auftrag `OrderNumber`/`SubOrderNumber`, Kürzel, Anzahl, `WorkOperation`-Ids) — kein
     Abbruch, keine Mail, kein SyncLog (Muster `WorkOperationRepository.cs:94-97`, v1.54.0 unverändert). Ursache bleibt die
     case-sensitive Re-Add-Sperre der Erkennung (FA 9 (iv)); deren Korrektur ist **nicht** Teil dieser Spec.
8. **Status braucht eine `WorkOperation` — Teil 1 zeigt Quelle S (Teil-1-Antwort 1).** Jede Statusquelle
   oben hängt an einer `WorkOperation`: `BdeBookingQuantity.IsFinal` über `BdeBooking.WorkOperationId`
   (`BdeApiController.cs:252-256`), `IsReported` an der `WorkOperation` selbst. Ein Sage-Kürzel aus
   `FaHierarchyNode.Arbeitsschritte` hat keinen Status.
   - ~~*Quelle W:* Status direkt je angezeigter `WorkOperation`, keine Zuordnung nötig.~~ — verworfen
     (Teil-1-Antwort 1, 2026-10-06).
   - *Quelle S (gilt):* Zuordnung Kürzel → `WorkOperation` nötig. Belegter Schlüssel: `WorkOperation` →
     `ProductionOrder` (FK `ProductionOrderId`, `ApplicationDbContext.cs:807-810`), Knoten ↔ Auftrag über
     `OrderNumber == HauptFA` und `SubOrderNumber == SubFA` (`FaHierarchyTreeViewModel.cs:176-181`), Kürzel
     ↔ `OperationNumber` (= Token, `WorkOperationStructureDetectionService.cs:169`; Vergleich dort
     case-insensitiv, `:80`, `:106`). Wegen des DirectChildren-Scopes (`:101-102`) gilt:
     - Blattzeile (`SubFA = 0`): keine eigene `WorkOperation`; Kandidat ist die des **Vater**-Sub-FA
       (`SubOrderNumber == VaterFA`).
     - Sub-FA-Zeile: Kürzel der eigenen Zeile sind als eigene `WorkOperation` erfasst **und** — weil die Zeile
       direktes Kind ihres Vaters ist — zusätzlich beim Vater-Sub-FA. Zwei Kandidaten mit eigenem Status.
     - Kein Unique-Index auf (`ProductionOrderId`, `OperationNumber`) (`ApplicationDbContext.cs:804-805`);
       Eindeutigkeit je Auftrag stellt nur die Erkennung sicher (`:151-163`). Der Default-AG `"01"`
       (`BdeDefaultWorkOperationService.cs:27-40`) hat kein Kürzel und taucht unter S nicht auf.
     - Kürzel ohne `WorkOperation`: unbekannt (`:129-132`), ausgeschlossen (`:116`), mehrdeutig (`:120-124`),
       oder Auftrag beim Erkennungslauf erledigt/storniert/fertig kommissioniert bzw. nicht materialisiert
       (`:142-145`). Dafür braucht es eine eigene Darstellung — festgelegt durch Antwort 6 b (FA 9).
   - ~~Diese Punkte entfallen, wenn Teil 1 Quelle W wählt.~~ — überholt (Teil 1 hat S gewählt).
9. **Zuordnungsregel Kürzel → `WorkOperation` (Antwort 6, Gerald 2026-10-06) — verbindlich:**
   - **a) Zuordnung:** Hat die Baumzeile selbst `SubFA ≠ 0`, gilt ihre **eigene** `WorkOperation` mit gleicher
     `OperationNumber` (`ProductionOrder.SubOrderNumber == SubFA`); sonst (Blatt, `SubFA = 0`) die des
     **VaterFA** (`ProductionOrder.SubOrderNumber == VaterFA`). Zusätzlich `ProductionOrder.OrderNumber ==
     HauptFA` (Muster `FaHierarchyTreeViewModel.cs:176-181`), `OperationNumber` case-insensitiv wie die
     Erkennung (`WorkOperationStructureDetectionService.cs:80`, `:106`, `:126`). Den Status liefert diese
     `WorkOperation` (Signal laut Rückfrage 3).
     - **W-2 (Antwort 7, 2026-10-09):** Zeile mit `SubFA ≠ 0` ohne eigene `WorkOperation` für das Kürzel (nicht
       materialisiert, bei Erkennung schon fertig o. ä.) → **kein Rückfall auf den Vater**, Symbol „kein
       BDE-Arbeitsgang“ (b).
     - **W-3 (Antwort 7, 2026-10-09):** Blatt, dessen `VaterFA` in derselben HauptFA auf **keinen** importierten
       Knoten mit `SubFA == VaterFA` zeigt (Waisen-Blatt; Strukturebene vgl. `IsOrphan`/`OrphanVaterFa`,
       `FaHierarchyTreeViewModel.cs:55-58`, v1.54.0; vormals `:53-57`, v1.49.0) → „kein BDE-Arbeitsgang“; alte `WorkOperation`s eines
       früher materialisierten Auftrags mit `SubOrderNumber == VaterFA` werden **nicht** verwendet. Die
       Zuordnungsfunktion prüft dazu, ob der Vater-Knoten in der geladenen Struktur existiert — nicht nur, ob
       ein Auftrag existiert.
     - **W-4 (Antwort 7, 2026-10-09):** liefert die Zuordnung mehrere `WorkOperation`s (gleiches Kürzel,
       derselbe Auftrag), gibt sie **alle** zurück; die Zusammenfassung regelt FA 7b.
   - **b) Kürzel ohne `WorkOperation`** (Ausschlussliste, unbekannt, mehrdeutig, Auftrag bei Erkennung schon
     fertig, kein Vater, W-2, W-3): Symbol bleibt sichtbar, **ohne Status**, neutral wie Teil 1, mit dem Hinweistext
     **„kein BDE-Arbeitsgang“** — als Text (sichtbar oder `aria-label`/visuell versteckt), **nie nur über
     Farbe**; nie grün, nie rot. Bei BDE global aus erscheint der Hinweis nicht (FA 6, aus Antwort 4 „sonst
     neutral“; vormals „hängt an Rückfrage 4“ — überholt).
   - **c) `WorkOperation` ohne Kürzel** (Default-AG `"01"`, `BdeDefaultWorkOperationService.cs:27-40`; in Sage
     nachträglich entfernte Kürzel — Erkennung löscht nie, `:149-163`): **kein** Symbol.
   - **Abgleich mit dem Erkennungs-Code** (Bündel, `IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs`):
     Die Erkennung läuft je Knoten mit `SubFA ≠ 0` (`:96-98`); ihr Scope ist die eigene Zeile plus alle Knoten
     mit `VaterFA == SubFA` (`:73-76` gruppiert **über alle Knoten** nach `VaterFA`, `:101-102`). Je Sub-FA
     zählt ein Kürzel einmal (`:106`, `:113`). Angelegt wird je `ProductionOrder` mit `SubOrderNumber == SubFA`
     (`:142-146`, `:158-179`); `SubOrderNumber` ist eindeutig (`IdealAkeWms/Data/ApplicationDbContext.cs:430`).
     - **(i) Blatt → VaterFA — stimmt.** Ein Blatt (`SubFA = 0`, nie selbst im Loop, `:98`) mit `VaterFA = X`
       steht im Scope des Knotens mit `SubFA = X` (`:102`); dessen `ProductionOrder` hat `SubOrderNumber = X`
       (`FaMaterializationSyncService.cs:146-148`). Trägt die Vater-Zeile dasselbe Kürzel selbst, zeigen Blatt
       und Vater dieselbe `WorkOperation` (je Sub-FA ein Kürzel nur einmal, `:113`) — gewollt laut Regel.
       Ausnahme: Waisen-Blatt → W-3.
     - **(ii) Sub-FA-Zeile, die Kind einer Sub-FA ist — seit v1.47.4 deckungsgleich (W-1 aufgelöst, Antwort 7).**
       Neuer Scope (v1.47.4): eigene Zeile + direkte Kinder **mit `SubFA == 0`**
       (`WorkOperationStructureDetectionService.cs:103-104` (v1.47.4), `kids.Where(k => k.SubFA == 0)`).
       Kürzel `K` einer Sub-FA-Zeile `S` (`VaterFA = P`) landet nur noch bei `S` — genau wie Regel a. Altbestand
       (`K` an `P` aus früheren Läufen) bereinigt das Einmal-Skript
       `SQL/Einmalig/2026-10-07_v1.47.4_RemoveParentWorkOperationsFromChildSubFaTokens.sql`, **gebuchte** werden nur
       gemeldet und bleiben stehen ([[2026-10-07-v1-47-4-arbeitsgang-erkennung-kind-sub-fa]]); solche Rest-
       Arbeitsgänge haben in der Ansicht kein Symbol (wie Antwort 6 c). ~~Vorher: Erkennung erfasste doppelt
       (eigene Zeile `:101` **und** Vater `:102`), Regel zeigte einfach → W-1.~~ — überholt (v1.47.4). W-2 bleibt.
     - **(iii) Wurzel und Vater ohne Sub-FA.** Wurzel = `VaterFA IS NULL` (`Models/FaHierarchyNode.cs:21`).
       Wurzel mit `SubFA ≠ 0`: im Loop (`:98`) und materialisiert (`FaMaterializationSyncService.cs:71-77`,
       `:146-148`) → eigene `WorkOperation`, Regel a passt. Wurzel mit `SubFA = 0`: kein Loop, kein Vater →
       keine `WorkOperation`; Regel a greift nicht, „sonst VaterFA“ hat keinen Vater → b („kein
       BDE-Arbeitsgang“) — passt zum Code. Ein Knoten mit `SubFA = 0` kann kein Vater sein (sein `SubFA`-Wert
       0 ist nie im Loop, `:98`); ein Kind mit `VaterFA = 0` läuft also ebenfalls in b — passt. Blatt unter
       der Wurzel: `VaterFA = Wurzel.SubFA` → Wurzel-`WorkOperation`, passt (`:102`). Ob `VaterFA = 0` bzw.
       Wurzeln mit `SubFA = 0` vorkommen: ungeprüft, Prüfschritt P3 c.
     - **(iv) Mehrfach-Treffer — möglich.** Kein Unique-Index auf (`ProductionOrderId`, `OperationNumber`)
       (`ApplicationDbContext.cs:804-805`). Innerhalb eines Laufs dedupliziert die Erkennung case-insensitiv
       (`:106`, `:126`), über Läufe hinweg sperrt sie das Re-Add aber mit einem `HashSet<(int, string)>` **ohne**
       Comparer (`:151-156`, Prüfung `:163`) — case-sensitiv. Ändert Sage die Schreibweise eines Kürzels
       (`KA` → `ka`), entsteht eine zweite `WorkOperation` am selben Auftrag. Der Default-AG `"01"` kollidiert
       nur, wenn Sage ein Kürzel `01` führt. → **W-4**.
     - Hinweis (kein Widerspruch zur Antwort, betrifft die `OrderNumber`-Bedingung aus FA 8): Die Erkennung
       matcht nur über `SubOrderNumber` (`:143`), die Anzeige zusätzlich über `OrderNumber == HauptFA`.
       `OrderNumber` wird nur beim Anlegen gesetzt (`FaMaterializationSyncService.cs:146`; keine weitere
       Zuweisung in `IDEALAKEWMSService/Services`). Wandert eine Sub-FA in Sage unter eine andere HauptFA,
       findet die Anzeige die `WorkOperation` nicht mehr (→ b). Umfang ungeprüft, P3 d.

> [!success] W-1 aufgelöst (Antwort 7, Keyuser 2026-10-07): Variante b — Kürzel einer Sub-FA-Zeile gehört nur
> zum eigenen Sub-FA. Umgesetzt als Bugfix **v1.47.4** im Bündel ([[2026-10-07-v1-47-4-arbeitsgang-erkennung-kind-sub-fa]],
> FREIGABE-NACHTRAG 2026-10-07 in [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]). Der folgende
> Kasten bleibt als Befund-Historie stehen; sein Inhalt ist **überholt**.
> Rework 2026-10-12: Der Nachtrag Gerald 2026-10-09 zu Antwort 7 ändert W-1 nicht (er betrifft nur W-2 bis
> W-4); Kürzel der Teile ohne eigenen FA (Blätter, z. B. `LS KA`) bleiben laut Antwort 7 am Vater-Sub-FA
> rückmeldbar — das ist Regel a für Blattzeilen.

> [!success] W-2, W-3, W-4 aufgelöst (Antwort 7, Nachtrag Gerald 2026-10-09)
> W-2: kein Rückfall auf den Vater → „kein BDE-Arbeitsgang“. W-3: Waisen-Blatt → „kein BDE-Arbeitsgang“, keine
> alten Vater-Arbeitsgänge. W-4: zusammengefasst (läuft, wenn einer läuft; erledigt nur, wenn alle erledigt;
> sonst offen; überfällig nur für nicht erledigte) + Log-Hinweis. Eingearbeitet in FA 7b, FA 9 a/b,
> Lösungsentwurf, AK 15/17, Testszenarien S12-S14. Die drei Kästen unten bleiben als Protokoll stehen; ihre
> Aussage „Nicht aufgelöst — Rückfrage 7“ ist **überholt**.

> [!warning] WIDERSPRUCH W-1 — Antwort 6 a „genau der Scope der Erkennung“ vs. doppelte Erfassung (überholt, v1.47.4)
> Der Erkennungs-Scope ordnet ein Kürzel einer Sub-FA-Zeile **zwei** Arbeitsgängen zu (eigener Sub-FA
> `:101` und Vater-Sub-FA `:102`); Regel a ordnet es **einem** zu (dem eigenen). Folge: Der Arbeitsgang am
> Vater-Sub-FA bekommt kein Symbol, wenn das Kürzel nur an Sub-FA-Kindern steht — auch dann nicht, wenn
> am BDE-Terminal genau auf ihn gebucht wurde: Das Terminal bietet jede offene `WorkOperation` der Werkbank
> einzeln an (`BdeApiController.cs:245`, `:272-279`), mit der Beschriftung `OrderNumber / OperationNumber —
> Name` (`:277`) — die beiden `K`-Arbeitsgänge von `S` und `P` tragen dieselbe `OrderNumber` (HauptFA) und
> sind dort nicht unterscheidbar. Der Status des Vater-Arbeitsgangs ist in der Ansicht dann unsichtbar. Regel c („WorkOperation ohne
> Kürzel → nicht zeigen“) nennt diesen Fall nicht (der Arbeitsgang **hat** ein Kürzel, nur an einer anderen
> Zeile). Nicht aufgelöst — Rückfrage 7.

> [!warning] WIDERSPRUCH W-2 — „materialisierter Sub-FA (SubFA ≠ 0)“ setzt zwei verschiedene Dinge gleich — aufgelöst (Antwort 7, 2026-10-09)
> `SubFA ≠ 0` heißt nicht, dass eine eigene `WorkOperation` existiert: Der `ProductionOrder` entsteht erst
> durch die Materialisierung (`FaMaterializationSyncService.cs:71-72`, `:141-148`), und die Erkennung legt
> nur für nicht erledigte, nicht stornierte, nicht fertig kommissionierte Aufträge an (`:142-145`). Fehlt
> die eigene `WorkOperation`, während der Vater-Sub-FA eine für dasselbe Kürzel hat (über `:102`), liefert
> Regel a wörtlich „kein BDE-Arbeitsgang“ (b), obwohl ein passender Arbeitsgang existiert. Ob dann auf den
> Vater zurückgefallen wird, sagt die Antwort nicht. Nicht aufgelöst — Rückfrage 7.
>
> **Nachtrag Rework 2026-10-07 (v1.47.4):** Seit der Scope-Korrektur trägt der Vater-Sub-FA das Kürzel einer
> Sub-FA-Kindzeile **nicht** mehr (`WorkOperationStructureDetectionService.cs:104` (v1.47.4)). Ein Rückfall auf
> den Vater träfe daher nur noch eine `WorkOperation`, die aus der **Vater-Zeile selbst oder einem Blatt-Kind**
> stammt — also nicht aus dem Kürzel dieser Zeile (gleiche Lage wie W-3). Das spricht nach Antwort 7 W-1 („gehört
> nur zum eigenen Sub-FA“) eher gegen einen Rückfall; entschieden ist es nicht.

> [!warning] WIDERSPRUCH W-3 — Waisen-Blatt (Randfall) — aufgelöst (Antwort 7, 2026-10-09)
> Zeigt `VaterFA` eines Blatts auf keinen importierten Knoten (Waisen-Pseudowurzel,
> `FaHierarchyTreeViewModel.cs:53-57`), war das Blatt-Kürzel nie im Scope einer Erkennung (kein Knoten mit
> `SubFA == VaterFA` im Loop, `:96-102`). Ein früher materialisierter `ProductionOrder` mit
> `SubOrderNumber == VaterFA` und seine alten Arbeitsgänge können aber noch existieren (Erkennung löscht nie,
> `:149-163`). Regel a („sonst VaterFA“) würde dann einen Status zeigen, der nicht aus diesem Kürzel stammt.
> Nicht aufgelöst — Rückfrage 7.

> [!warning] WIDERSPRUCH W-4 — Antwort 6 a spricht von „ihrem eigenen Arbeitsgang“ (Einzahl) — aufgelöst (Antwort 7, 2026-10-09)
> Der Code garantiert die Einzahl nicht (siehe (iv)). Für mehrere Treffer je Auftrag + Kürzel fehlt eine
> Regel (z. B. erledigt, wenn einer/alle erledigt). Nicht aufgelöst — Rückfrage 7. Umfang: P3 a.

## Technischer Loesungsentwurf

Ort: **Baumzeilen** (Teil-1-Antwort 2 = Variante A). Ablauf im `FaHierarchyController.Index`, **nach** der
Paginierung (`FaHierarchyController.cs:86-92`, v1.54.0) und nach der Teil-1-Symbolzuordnung
(`FaArbeitsschrittSymbole.Zuordnen`, `:100-102`), nur für die Strukturen der Seite:

1. Globales Gate `AppSettingKeys.BdeAktiv` lesen (`IAppSettingRepository`, Z.29). Aus → keine weiteren
   Abfragen, Symbole wie Teil 1.
2. **Eine** gebündelte `WorkOperation`-Abfrage über die HauptFA-Nummern der Seite (Projektion `Id`,
   `ProductionOrder.OrderNumber`, `ProductionOrder.SubOrderNumber`, `OperationNumber`,
   `ProductionWorkplace.BdeAktiv`; Muster `WorkOperationRepository.cs:120-128`, `Contains` → EF 10 `OPENJSON`,
   [[fallstricke]] §8, ToQueryString-Test). Sie ersetzt den Existenz-Check `GetOrderNumbersWithWorkOperationsAsync`
   (`FaHierarchyController.cs:94-98`, v1.54.0) **nicht** — der bleibt für den Modal-Knopf; Zusammenlegen ist erlaubt,
   wenn `HasWorkOperations` identisch bleibt.
3. Je Knotenzeile + Kürzel die Zuordnungsfunktion (FA 9, Antwort 6 a + 7) → eine oder mehrere
   `WorkOperation`-Ids (W-4) oder „kein BDE-Arbeitsgang“ (inkl. W-2 kein Vater-Rückfall, W-3 Waisen-Blatt).
4. **Eine** gebündelte Status-Abfrage über die aufgelösten Ids: erledigt (Prädikat identisch zum Terminal,
   `BdeApiController.cs:252-256`: `q.IsFinal && !q.BdeBooking.IsCancelled`) und läuft (`EndedAt == null &&
   !IsCancelled`, Muster `BdeBookingRepository.cs:62`) — als **eine** Repository-Methode, die beide Mengen
   liefert (zwei SQL-Statements in einer Methode sind zulässig; AK 9 zählt Abfragen je Zweck, siehe dort).
   Das Terminal bleibt unverändert (kein Refactoring außerhalb des Umfangs).
5. Feiertage einmal je Seitenaufbau (`IHolidayRepository.GetHolidayDatesAsync`, Muster
   `OseonGroupViewModelBuilder.cs:35`); Offsets einmal je Seitenaufbau über den **Offset-Lookup der
   Vorbedingung** (FA 4 „Schnittstelle zur Vorbedingung“, Antwort 8), Schlüsselvergleich case-insensitiv —
   konkret `IOperationConfigRepository.GetDueDateOffsetsAsync()` (v1.54.0, gecacht), Lookup mit
   `OperationNumber.Trim()`; `TryGetValue` false = kein Eintrag → Offset 0 + Sammelhinweis.
   ~~Quelle laut Rückfrage 8; bei Variante A `IOseonOperationConfigRepository.GetAllAsDictionaryAsync`, gecacht
   über `CachedOseonOperationConfigRepository`~~ — überholt (Antwort 8: weder A noch B).
6. Soll-Termin je Arbeitsgang: `OseonDueDateCalculator.Calculate(FE_Termin, offset, _businessDays, holidays)`
   (wiederverwenden, nicht kopieren); `FE_Termin` aus `structure.OrderInfos` (ohne Abfrage, siehe FA 4);
   Kombigerät: frühester nicht leerer `FE_Termin` (Antwort 9; vormals „laut Rückfrage 9“). Kürzel ohne
   Offset-Eintrag → Offset 0 und Aufnahme in den Sammelhinweis „nicht konfiguriert“ (FA 4).
7. Statusableitung als reine Funktion (Eingaben: hat `WorkOperation`?, Werkbank BDE-aktiv?, erledigt?,
   läuft?, Soll-Termin?, heute) → Tabellentest; Vorrang laut FA 7a (läuft + überfällig kombiniert,
   Antwort 10); Zusammenfassung mehrerer Treffer laut FA 7b als zweite reine Funktion, die Mehrfach-Treffer
   zusätzlich als Liste für die eine `LogWarning` je Seitenaufbau zurückgibt (Antwort 7 W-4).

- ~~Variante B: Termin im Modal-Endpunkt nachladen (`FaHierarchyController.cs:148`), `GetByOrderNumberWithWorkplaceAsync`
  (`:152`)~~ — verworfen (Teil-1-Antwort 2: Baum).
- ~~Offene Punkte der Zuordnungsfunktion bis zur Antwort auf Rückfrage 7: Fallback bei fehlender eigener
  `WorkOperation` (W-2), Waisen-Blatt (W-3), Mehrfach-Treffer (W-4).~~ — **entschieden (Antwort 7,
  2026-10-09)**, siehe FA 7b und FA 9 a. ~~W-1 betrifft, ob zusätzliche Symbole
  oder ein Hinweis für Vater-Arbeitsgänge nötig sind.~~ — erledigt (Antwort 7, v1.47.4).
- Neue Ctor-Abhängigkeiten von `FaHierarchyController` (heute Z.35-53, v1.54.0; vormals Z.34-41, v1.49.0):
  `IHolidayRepository`, `IBusinessDayService`, `IOperationConfigRepository` (Offset-Lookup der Vorbedingung,
  `GetDueDateOffsetsAsync`), `ILogger<FaHierarchyController>`. ~~Die Tests, die den Controller direkt konstruieren,
  ziehen mit.~~ — überholt (2026-10-13): Kein Test konstruiert `FaHierarchyController` direkt (Grep
  `new FaHierarchyController` in `IdealAkeWms.Tests`: 0 Treffer; ebenso Teil-1-QA „Geänderte Bestandstests“).
- Teil 1 lädt bereits **alle** Werkbänke (`IProductionWorkplaceRepository.GetAllOrderedAsync`,
  `FaHierarchyController.cs:101-102`). Die Werkbank-Gate-Information (`ProductionWorkplace.BdeAktiv`,
  `ProductionWorkplace.cs:21`) darf der Dev-Lauf daraus per `ProductionWorkplaceId` nehmen, statt sie in der
  `WorkOperation`-Projektion zu joinen — beides ist zulässig, die Abfragezahl (AK 9) ändert sich nicht.
- `WorkOperation`s ohne Symbol (Antwort 6 c) werden nicht dargestellt; die Warnung `UnmatchedOperations` im
  Modal bleibt unberührt.
- **frontend-design-Skill ist im Dev-Lauf Pflicht** (Views/CSS). Bootstrap-5-Farben/Icons der Anwendung,
  keine neue Icon-Bibliothek ohne ADR.
- **FA 4a (neu 2026-10-13):** `OperationConfigsController.Add`/`Update` mit `int? dueDateOffsetDays` und
  Frühausstieg bei `null` (Leiter-Sprosse 7, eine Bedingung je Action); keine View-, Repository- oder Schemaänderung.

## Migrations-/SQL-Auswirkungen

Status und Soll-Termin werden abgeleitet; `FE_Termin` wird bereits importiert (`FaHierarchySql.cs:140-141`,
Antwort 2). `SQL/AgentJobs/` unberührt.

**Entschieden (Antwort 8, 2026-10-09): Teil 2 hat keine Migration.** Kein `SQL/XX_*.sql`,
`SQL/00_FreshInstall.sql` unverändert. Jede Schema-Änderung für die Arbeitsgang-Stammdaten (z. B. Umbenennung
`IsOseonRelevant` → „Fremdsystem-relevant“, neue Tabelle/Spalten) gehört zur Vorbedingungs-Spec
([[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]) und wird dort nach ADR 0004 (OBJECT_ID-/COL_LENGTH-
Guard, FreshInstall an zwei Stellen) geführt. **Stand 2026-10-13:** dort umgesetzt als Migration
`20261012140548_RenameOperationConfigsAndAddAudit` + `SQL/95_RenameOperationConfigsAndAddAudit.sql` (Tabelle
`dbo.OperationConfigs`, Spalte `IsExternalSystemRelevant`, Audit-Spalten, `IX_OperationConfigs_OperationName`). Teil 2
liest nur und setzt voraus, dass `SQL/95` auf der Ziel-DB gelaufen ist (Deploy-Reihenfolge). Die Offset-Prüfung aus
FA 4a ist reine Controller-Logik — **keine** Schema-Änderung. Die folgenden Varianten bleiben als Historie stehen und
sind **überholt**.

~~**Abhängig von Rückfrage 8 (Offset-Pflege):**~~
- Variante A (`OseonOperationConfigs` wiederverwenden): **keine** Migration, `SQL/00_FreshInstall.sql`
  unverändert. Die IDEAL-Offsets sind Daten (Pflege über `/Settings/OperationConfig`), kein Skript.
- Variante B (neues Feld an `ProductionWorkplace`, z. B. `DueDateOffsetDays int NOT NULL DEFAULT 0`):
  Migration nach ADR 0004 — Model → `dotnet ef migrations add` → `SQL/XX_*.sql` mit Spalten-Guard
  (`COL_LENGTH('dbo.ProductionWorkplaces','DueDateOffsetDays') IS NULL`, DDL in eigenem Batch) →
  `__EFMigrationsHistory`-Insert in separatem Batch → `SQL/00_FreshInstall.sql` an **beiden** Stellen. Nicht
  daten-destruktiv. Zusätzlich prüfen, dass der Werkbank-Sync (`ProductionWorkplaceSyncService`) das Feld nicht
  überschreibt.

## Audit-Feld-Auswirkungen

Keine — reine Lesedarstellung; Teil 2 schreibt weder Stammdaten noch Bewegungsdaten. Audit-Pflichten beim
Pflegen der Offsets regelt die Vorbedingungs-Spec (umgesetzt: `OperationConfig : AuditableEntity`,
`Models/OperationConfig.cs:11`; Add setzt `CreatedAt/By/ByWindows` `OperationConfigsController.cs:97-99`, Update
`ModifiedAt/By/ByWindows` `:120-122`). **FA 4a (2026-10-13):** Der neue Abweise-Pfad schreibt nichts, also auch keine
Audit-Felder; die bestehenden Audit-Zuweisungen bleiben unverändert und werden nur bei gültigem Offset erreicht. (Überholt, Antwort 8 — bei Rückfrage 8 Variante A: `OseonOperationConfig` ist keine
`AuditableEntity` (`Models/OseonOperationConfig.cs:5`) und bekommt durch diese Spec keine Audit-Felder; bei
Variante B werden `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` der Werkbank beim Speichern wie bisher gesetzt.)

## Rollen / Zugriffsfilter

Unverändert `[RequirePickingOrTrackingOrLeitstandAccess]` (`FaHierarchyController.cs:23`). Die Ansicht
zeigt BDE-Fortschritt jetzt auch Rollen ohne BDE-Rolle (`bde_user`/`bde_shiftlead`/`bde_admin`,
`RoleKeys.cs:14-16`) — es ist nur ein Status je Arbeitsgang, keine Bediener- oder Zeitdaten. Antwort 4
schränkt das nicht ein (nur Schalter-Logik) → bleibt so. Offset-Pflege: in den BDE-Stammdaten, Recht Admin,
erreichbar unabhängig von `BdeAktiv` — Sache der Vorbedingung (Backlog-Entscheidungen 1/2, umgesetzt in
[[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]: `OperationConfigsController` Klasse `[RequireAdminAccess]`,
ohne `[RequireBdeActive]`, `OperationConfigsController.cs:18`). FA 4a ändert an Rollen und Filtern nichts. Der Sammelhinweis „nicht konfiguriert“ (FA 4) nennt den
Pflegeort als Text, verlinkt ihn aber nicht zwingend (Benutzer der FA-Struktur sind nicht unbedingt Admin).
~~Variante A unter `/Settings` nur Admin (`SettingsController.cs:10`); Variante B im Werkbank-Stamm mit dessen
bestehendem Edit-Filter.~~ — überholt (Antwort 8).

## Listen-View-Pattern (ADR 0005)

Kein neuer Listen-View. Teil 1 hat Variante A gewählt: die Status-Darstellung sitzt in derselben Spalte wie in
Teil 1 (keine weitere Registrierung in `ColumnDefinitions`); ein Statusfilter in der Filterzeile wäre neu und
ist nicht verlangt.

## Akzeptanzkriterien

1. BDE global **und** Werkbank aktiv (Antwort 4): Ein Arbeitsgang mit einer `IsFinal`-Menge auf nicht
   stornierter Buchung (Antwort 3) erscheint als **erledigt** (grün + Haken + `aria-label` „… – erledigt“).
   Eine reine Tracking-Rückmeldung (`IsReported = true` ohne `IsFinal`) macht ihn **nicht** erledigt.
2. Ein nicht erledigter Arbeitsgang, dessen Soll-Termin (`FE_Termin` + Offset in Werktagen, Feiertage
   berücksichtigt, Antwort 2) vor heute liegt, erscheint als **überfällig** (rot + Warndreieck + Text).
   Prüfwert: `FE_Termin` = Do 30.04.2026, Offset 1, 01.05. Feiertag → Soll Mo 04.05.2026 (Muster
   `OseonDueDateCalculatorTests.cs:64`); heute 05.05. → überfällig, heute 04.05. → nicht überfällig.
3. Ein nicht erledigter Arbeitsgang mit laufender Buchung (`EndedAt == null`, nicht storniert) erscheint als
   **läuft** (eigene Farbe + eigenes Zeichen + Text, Antwort 5); eine **pausierte** Buchung zählt nicht als
   läuft. Liegt der Soll-Termin zugleich vor heute, erscheint **„läuft“ mit Zusatz „überfällig“**: Läuft-Zeichen
   **und** Warndreieck, roter Akzent, `aria-label` „… – läuft, überfällig (Soll TT.MM.JJJJ)“ (Antwort 10;
   vormals „Vorrang gegenüber überfällig laut Rückfrage 10“).
3a. Alle übrigen erscheinen als **offen** (grau + leerer Kreis + Text).
4. Storno der abschließenden Buchung oder Korrektur `IsFinal → false` lässt das Symbol beim nächsten Laden
   wieder offen/überfällig werden (kein gespeicherter Status).
5. Ohne `FE_Termin` (`null`) oder ohne Kopfzeile ist ein Arbeitsgang nie überfällig.
6. BDE global aus **oder** Werkbank des Arbeitsgangs nicht BDE-aktiv: Symbol wie Teil 1, ohne Statusfarbe,
   ohne Statuszeichen. Gemischte Struktur (eine Werkbank aktiv, eine nicht) → nur die aktiven tragen Status.
7. Jeder Status ist ohne Farbwahrnehmung erkennbar (Graustufen-Screenshot: Haken/Kreis/Dreieck/Läuft-Zeichen
   unterscheidbar) und per Screenreader angesagt (`aria-label`).
8. Kontrast aller Statusvarianten ≥ WCAG AA, gemessen am Testsystem. *Offen (Rückfrage 11, 2026-10-13):* ob das
   auch für gedimmte Kontextzeilen des Spaltenfilters (`opacity: 0.6`, `site.css:1361`) gilt — bis zur Antwort ist
   AK 8 für nicht gedimmte Zeilen (normal, Hover, Hervorhebung, Filtertreffer) verbindlich.
9. Je Seitenaufbau bei BDE an: genau **eine** zusätzliche `WorkOperation`-Abfrage und **eine**
   Status-Repository-Methode (höchstens zwei SQL-Statements: erledigt, läuft), plus je **ein** Aufruf für
   Feiertage und für den Offset-Lookup der Vorbedingung (gecacht zählt nicht) — kein N+1, unabhängig von der
   Seitengröße (Query-Zählung im Dev-Lauf). Bei BDE global aus: **keine** dieser Abfragen.
10. Status-Ableitung durch Tabellentest abgedeckt (kein BDE-Arbeitsgang vor allem; erledigt vor läuft/überfällig;
    Soll-Termin heute = nicht überfällig; `FE_Termin` `null`; Werkbank nicht BDE-aktiv → neutral; läuft +
    Soll-Termin gestern → „läuft“ mit Zusatz „überfällig“ (Antwort 10); erledigt + Soll-Termin gestern →
    erledigt ohne Zusatz). (Vormals „Vorrang laut Rückfrage 10; Kombigerät-Regel laut Rückfrage 9“ — überholt.)
10a. Offset-Auflösung per Unit-Test (Antwort 8): Kürzel mit Eintrag → dessen Offset (case-insensitiv: `ka` trifft
    Eintrag `KA`); Kürzel ohne Eintrag → Offset 0 (Soll = `FE_Termin`) **und** das Kürzel steht in der Liste
    für den Sammelhinweis; Eintrag mit Wert 0 → Offset 0 **ohne** Hinweis. (Vormals „laut Rückfrage 8“.)
10b. Kombigerät (Antwort 9) per Unit-Test: HauptFA mit zwei Kopfzeilen `FE_Termin` 10.05. und 05.05. → Bezug
    05.05.; eine Kopfzeile mit `FE_Termin = null`, eine mit 07.05. → Bezug 07.05.; alle `null` → nie überfällig.
    Die Kopfzeile im Baum zeigt weiterhin keinen Einzeltermin (`Index.cshtml:151-153`).
10c. Sammelhinweis „nicht konfiguriert“ (FA 4): erscheint genau dann, wenn auf der Seite mindestens ein
    Kürzel mit Status (BDE an, `WorkOperation` vorhanden, Werkbank aktiv) keinen Offset-Eintrag hat; nennt jedes
    solche Kürzel einmal; ist als Text lesbar (nicht nur Farbe); verdrängt den Tiefen-Cap-Hinweis nicht (beide
    zugleich sichtbar).

Zuordnung Kürzel → `WorkOperation` (Teil 1 = Quelle S, Antwort 6; vormals „Nur falls Teil 1 Quelle S“):
11. Zuordnung durch Unit-Test abgedeckt (Antwort 6 a): Sub-FA-Zeile (`SubFA ≠ 0`) mit Kürzel `KA` → die
    `WorkOperation` `KA` ihres **eigenen** Auftrags, auch wenn der Vater-Sub-FA ebenfalls eine `KA` hat;
    Blattzeile (`SubFA = 0`) mit `KA` → die `KA` des Auftrags mit `SubOrderNumber == VaterFA`; Wurzel mit
    `SubFA = 0` → keine Zuordnung; Groß-/Kleinschreibung egal (`ka` trifft `KA`); fremde HauptFA mit gleicher
    Sub-FA-Nummer trifft nicht.
12. Kürzel ohne `WorkOperation` (Kürzel der Ausschlussliste, unbekanntes, mehrdeutiges, Auftrag bei Erkennung
    schon fertig) erscheint als neutrales Symbol **mit dem Text „kein BDE-Arbeitsgang“** (sichtbar oder per
    `aria-label`/visuell versteckt, per Screenreader angesagt; nicht nur Farbe) — nie grün, nie rot, nie
    überfällig, ohne Fehler (Antwort 6 b).
13. Abfragebudget wie AK 9 (eine `WorkOperation`-Abfrage + eine Status-Methode je Seitenaufbau, Query-Zählung).
14. Eine `WorkOperation` ohne passendes Kürzel in der Struktur (Default-AG `01`; Kürzel in Sage entfernt)
    erzeugt **kein** Symbol (Antwort 6 c); Unit-Test: Auftrag mit AGs `01` und `KA`, Knotenzeile `"KA"` →
    genau ein Symbol `KA`.
15. Fälle W-2 bis W-4 per Unit-Test (Antwort 7, 2026-10-09; vormals „gemäß Antwort auf Rückfrage 7 — vor dem
    Dev-Lauf festzulegen; bis dahin nicht abnahmefähig“ — überholt). (W-1 erledigt durch v1.47.4, siehe AK 16.)
    - **W-2:** Sub-FA-Zeile `S` (`SubFA ≠ 0`) mit Kürzel `KA`, `S` hat **keine** `KA`-`WorkOperation`, der
      Vater-Sub-FA `P` hat eine → Symbol `KA` an `S` neutral mit „kein BDE-Arbeitsgang“ (kein Status von `P`).
    - **W-3:** Blatt mit `VaterFA = X`, kein Knoten mit `SubFA = X` in der Struktur, aber ein Auftrag mit
      `SubOrderNumber = X` und `KA`-`WorkOperation` existiert → „kein BDE-Arbeitsgang“.
    - **W-4:** Auftrag mit zwei `WorkOperation`s `KA` und `ka`: (läuft, offen) → läuft; (erledigt, offen) → offen
      bzw. überfällig bei Soll < heute; (erledigt, erledigt) → erledigt, auch bei Soll < heute;
      (läuft, erledigt) → läuft; Soll < heute und (läuft, offen) → läuft mit Zusatz überfällig.
16. Nach v1.47.4 (Vorbedingung): Sub-FA `S` (Kürzel `VM`) unter Sub-FA `P` ohne eigenes `VM` → an Zeile `S`
    Symbol `VM` mit dem Status der `VM`-`WorkOperation` von `S`; an Zeile `P` kein `VM`-Symbol, und am Auftrag `P`
    existiert keine (ungebuchte) `VM`-`WorkOperation` mehr (Antwort 7 W-1).
17. W-4-Log: Ein Seitenaufbau mit Mehrfach-Treffern schreibt genau **eine** `LogWarning` (Inhalt: Auftrag,
    Kürzel, Anzahl, Ids), die Seite lädt ohne Fehler; ohne Mehrfach-Treffer keine Warnung (Unit-Test mit
    Fake-Logger oder Prüfung der reinen Funktion, die die Liste liefert).
18. Kein Diff an `OseonDueDateCalculator`, `BusinessDayService` und an der Offset-Pflege; Teil 2 bringt keine
    Migration (`deploy.migration: false`). **Präzisiert 2026-10-13:** „kein Diff an der Offset-Pflege“ gilt mit
    **einer** Ausnahme — `OperationConfigsController.Add`/`Update` laut FA 4a (Parameter `int?` + Abweise-Pfad).
    `IOperationConfigRepository`/`OperationConfigRepository`/`CachedOperationConfigRepository`,
    `Views/OperationConfigs/Index.cshtml`, Migration und `SQL/95` bleiben ohne Diff.
19. **Leerer/fehlender Offset wird abgewiesen (FA 4a, [[2026-10-12-operationconfig-offset-leer-pruefen]]).**
    Unit-Tests in `OperationConfigsControllerTests` (direkter Action-Aufruf = manipulierter POST):
    - `Add("KA", "Kanten", null, false)` → `RedirectToActionResult` auf `Index`, `TempData["WarningMessage"]` gesetzt,
      **kein** `AddAsync`-Aufruf (kein Eintrag `KA` im Repository/InMemory);
    - `Update(id, "Neu", null, false)` auf einem Bestandseintrag mit Offset `+3` → Warnung, Eintrag unverändert
      (Offset `+3`, alter `DisplayName`, alte Relevanz, `ModifiedAt` unverändert), **kein** `UpdateAsync`-Aufruf;
    - Gegenprobe: `Add(..., 0, ...)` und `Update(..., -1, ...)` speichern wie bisher (0 und negative Werte bleiben gültig).
    Die View bleibt unverändert: beide Offset-Felder tragen weiterhin `required`
    (`Views/OperationConfigs/Index.cshtml:79-80`, `:149`; im Browser verhindert der Pflichtfeld-Hinweis das Absenden).

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` des Bündel-Worktrees (Nummer: nächste freie im Bündel zum
Umsetzungszeitpunkt; ~~Stand 2026-10-12 höchstes TS-82, Teil 1 und Vorbedingung kommen davor~~ — überholt; Stand
2026-10-13: höchstes **TS-87** (Vorbedingung), TS-85 = Teil 1 → voraussichtlich **TS-88**), Index nachziehen.
Zusätzlich **TS-87.3** (Pflege, Vorbedingung) um den Negativfall aus S17 ergänzen — dort steht heute nur der
Browser-Weg.

- **Vorbedingungen:** IDEAL-Testsystem mit Bündel ≥ v1.54.0 (enthält v1.47.4 mit Einmal-Skript, Teil 1 v1.52.0 und
  die Vorbedingung Arbeitsgang-Stammdaten inkl. `SQL/95`; vormals „≥ v1.47.4 … und der umgesetzten Vorbedingung“),
  auf der IDEAL-DB die Seed-Kollisionen bereinigt (Prüfschritt P3 der Vorbedingungs-Spec, deren Checkliste Punkt 15:
  kollidierende OSEON-Seed-Kürzel wie `EG`/`BG` per UI auf IDEAL-Werte, IDEAL-Kürzel aus „Nicht konfigurierte
  Kürzel“ anlegen), BDE global aktiv,
  Werkbänke der Test-AGs `BdeAktiv`, Struktur mit ≥ 3 erkannten Arbeitsgängen, eine Struktur mit `FE_Termin`
  in der Vergangenheit und eine mit `FE_Termin` in der Zukunft, Offsets im Stammdaten-Reiter „Arbeitsgänge“
  gepflegt (mindestens ein Kürzel mit Offset ≠ 0 und mindestens ein Kürzel **ohne** Eintrag; vormals „laut
  Rückfrage 8“), Feiertage im WMS gepflegt (Prüfschritt P4).
- **S1 Erledigt:** Am BDE-Terminal einen AG starten und mit Abschluss beenden → Symbol grün + Haken,
  Screenreader „… – erledigt“. Gegenprobe: im Tracking nur rückmelden (ohne BDE-Abschluss) → **nicht** grün.
- **S2 Offen:** AG ohne Buchung, Soll-Termin in der Zukunft → grau + leerer Kreis.
- **S3 Überfällig:** AG ohne Abschluss, Soll-Termin (FE_Termin + Offset) gestern → rot + Warndreieck. Offset-
  Probe: Kürzel mit Offset +3, `FE_Termin` = vor 2 Werktagen → noch **nicht** überfällig; vor 4 Werktagen →
  überfällig. Wochenende/Feiertag zwischen `FE_Termin` und heute wird nicht mitgezählt.
- **S4 Storno/Korrektur:** Abschlussbuchung im BDE-Buchungs-Editor auf „nicht final“ setzen oder stornieren
  → Symbol nach Neuladen nicht mehr grün.
- **S5 Läuft:** AG am Terminal gestartet, nicht beendet → eigene Farbe + Läuft-Zeichen, „… – läuft“. Pausieren
  → nach Neuladen offen bzw. überfällig. Läuft **und** Soll-Termin überschritten → „läuft“ mit Zusatz
  „überfällig“: Läuft-Zeichen + Warndreieck, roter Akzent, Screenreader „… – läuft, überfällig (Soll …)“; im
  Graustufen-Screenshot von „läuft“ allein unterscheidbar (Antwort 10; vormals „laut Rückfrage 10“).
- **S6 BDE aus:** globalen Schalter `BdeAktiv` ausschalten → alle Symbole neutral, kein „kein BDE-Arbeitsgang“.
  Global an, aber eine Werkbank `BdeAktiv = false` → nur deren Symbole neutral, die übrigen mit Status.
- **S7 Barrierefreiheit:** Graustufen-Ansicht + Screenreader + Kontrastmessung am Fertigungsterminal.
- **S8 Blatt → Vater (Antwort 6 a):** Blattzeile mit Kürzel `KA`, deren Vater-Sub-FA den AG `KA` am Terminal
  abgeschlossen hat → Symbol `KA` an der Blattzeile grün/Erledigt-Form. (Vormals „nur Quelle S … laut
  Rückfrage 6“ — jetzt unbedingt.)
- **S9 Sub-FA-Zeile → eigener AG (Antwort 6 a):** Sub-FA `S` mit eigenem Kürzel `KA` unter Sub-FA `P`. Am
  Terminal nur den `KA`-AG von `S` abschließen → Symbol `KA` an Zeile `S` grün. ~~Gegenprobe: nur den `KA`-AG
  von `P` abschließen → Zeile `S` bleibt offen/überfällig; Verhalten an Zeile `P` laut Antwort auf
  Rückfrage 7 (W-1).~~ — überholt (Antwort 7 W-1, v1.47.4): Am Terminal gibt es für `S`s Kürzel nur noch
  **einen** `KA`-AG (den von `S`); Zeile `P` zeigt `KA` nur, wenn `P` selbst oder ein Blatt-Kind von `P` `KA`
  trägt (dann mit dem Status von `P`s eigenem `KA`-AG).
- **S10 Kürzel ohne AG (Antwort 6 b):** Knoten mit einem Kürzel der Ausschlussliste (z. B. `STO`) und einem
  ohne Werkbank (P-K2 b in Teil 1, `Werkbaenke = 0`) → beide Symbole neutral, Screenreader sagt „kein
  BDE-Arbeitsgang“, kein Grün/Rot, auch bei Termin in der Vergangenheit.
- **S11 AG ohne Kürzel (Antwort 6 c):** Sub-FA mit Default-AG `01` (NurFA-Buchung) → kein Symbol `01`.
- **S12 W-2 kein Vater-Rückfall:** Sub-FA-Zeile, deren Auftrag für ein Kürzel keinen Arbeitsgang hat (z. B.
  Auftrag war bei der Erkennung schon erledigt), während der Vater-Sub-FA denselben Kürzel-AG hat → Symbol
  neutral, „kein BDE-Arbeitsgang“; Abschluss am Vater-AG ändert das Symbol an dieser Zeile nicht.
- **S13 W-3 Waisen-Blatt:** Struktur, in der der Vater-Knoten eines Blatts fehlt (Waisen-Kennzeichnung an der
  Struktur) → Blatt-Kürzel neutral, „kein BDE-Arbeitsgang“, auch wenn ein alter Auftrag zum `VaterFA` existiert.
- **S14 W-4 Doppel-Eintrag:** (Testdaten per SQL: zweite `WorkOperation` `ka` neben `KA` am selben Auftrag, P3 a)
  → ein Symbol `KA`; einen der beiden abschließen → weiterhin offen/überfällig; beide abschließen → erledigt;
  einen starten → läuft. Serverprotokoll enthält je Seitenaufbau eine Warnung zum Doppel-Eintrag, die Seite lädt
  ohne Fehler.
- **S15 Offset nicht konfiguriert:** Kürzel ohne Stammdaten-Eintrag → Soll-Termin = `FE_Termin` (Offset 0);
  oberhalb des Baums Sammelhinweis „… kein Termin-Offset konfiguriert …“ mit dem Kürzel; nach Pflege eines
  Eintrags und Neuladen verschwindet das Kürzel aus dem Hinweis.
- **S16 Kombigerät:** HauptFA mit mehreren Kopfzeilen und verschiedenen `FE_Termin` → Soll-Termin im
  `aria-label` beruht auf dem frühesten; Kopfzeile des Baums zeigt weiterhin keinen Einzeltermin (Antwort 9).
- **S17 Offset-Pflichtfeld serverseitig (FA 4a, AK 19; neu 2026-10-13):** Als Admin Stammdaten › Arbeitsgänge
  (`/OperationConfigs`). (a) Im Browser das Offset-Feld einer Zeile leeren und „Speichern“ → Pflichtfeld-Hinweis, kein
  Absenden (unverändert). (b) Manipulierter POST: in den Entwicklertools am Offset-Feld der Zeile `KA` (Offset `+3`)
  `required` entfernen, Feld leeren, speichern → gelbe Meldung „Offset … fehlt – nichts gespeichert“; nach Neuladen
  steht `KA` weiter auf `+3`, Anzeigename/Relevanz unverändert; `SELECT DueDateOffsetDays, ModifiedAt FROM
  dbo.OperationConfigs WHERE OperationName = 'KA'` zeigt den alten Stand. (c) Dasselbe im Anlege-Formular mit neuem
  Kürzel `ZZ` → gelbe Meldung, `ZZ` erscheint nicht in der Tabelle. (d) Gegenprobe: Offset `0` bzw. `-1` speichern →
  grün, Wert übernommen.
- **Negativ:** ~~Kombigerät mit abweichenden Terminen → Verhalten laut Rückfrage 9~~ (jetzt S16); Struktur ohne
  Kopfdaten (`FaHierarchyOrderInfo` fehlt) → nie überfällig, kein Fehler; ~~Kürzel ohne Offset-Eintrag → laut
  Rückfrage 8~~ (jetzt S15), kein Fehler; Offset-Lookup der Vorbedingung liefert leer (keine Stammdaten) → alle
  Offsets 0, Sammelhinweis mit allen Kürzeln, kein Fehler; Seitengröße „Alle“ → Abfragezahl wie AK 9 (kein N+1);
  manipulierter POST ohne Offset → nichts gespeichert (S17).

## Pruefschritte fuer den Menschen (nur Sage/PPS beantwortet)

**P1 — Bedeutung der Termine** (vormals „vor Antwort auf Rückfrage 2“; Antwort 2 hat `FE_Termin` „vorerst“
gesetzt — P1 bleibt als Absicherung vor dem Dev-Lauf empfohlen, `Neuer_PT_PPS` ist nur noch Vergleichswert).
Auf der IDEAL-Sage-Instanz:

```sql
SELECT TOP 50 HauptFA, Abteilung, [Status], KO_Termin, FE_Termin, Start_Beschichtung,
       Neuer_PT_PPS, Verladetermin_Vsl
FROM dbo.[vw_IDEAL-AKE_Kommissionierung_FAInfos]
ORDER BY FE_Termin DESC;
```

Mit der PPS-Verantwortlichen abgleichen: Ist `FE_Termin` der Termin, bis zu dem **alle** Fertigungs-
Arbeitsgänge der Struktur erledigt sein müssen, oder nur die Blechfertigung? Ist `Neuer_PT_PPS` ein
verschobener Ersatz für `FE_Termin`? Zusätzlich den View-Text sichern:
`SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.[vw_IDEAL-AKE_Kommissionierung_FAInfos]'));`

**P2 — Kombigeräte mit abweichenden Terminen:**

```sql
SELECT HauptFA, COUNT(*) AS Zeilen,
       COUNT(DISTINCT FE_Termin) AS FE_Varianten,
       COUNT(DISTINCT Neuer_PT_PPS) AS PT_Varianten
FROM dbo.[vw_IDEAL-AKE_Kommissionierung_FAInfos]
GROUP BY HauptFA
HAVING COUNT(*) > 1;
```

Gibt es Treffer mit `FE_Varianten > 1`, braucht Rückfrage 9 (vormals 2) eine Regel (z. B. frühester Termin).
— Regel entschieden (Antwort 9: frühester `FE_Termin`); P2 zeigt nur noch, wie oft sie greift.

**P3 — Umfang der Widersprüche W-1 bis W-4 (vor Antwort auf Rückfrage 7).** Abfrage b) (W-1) ist **überholt**:
W-1 ist entschieden, den Altbestand behandelt das v1.47.4-Einmal-Skript samt Meldeliste
([[2026-10-07-v1-47-4-arbeitsgang-erkennung-kind-sub-fa]]). a), c), d) bleiben für W-2 bis W-4. Auf der **IDEAL-WMS-Datenbank**
(nicht Sage); vom Spec-Lauf nicht ausgeführt:

```sql
-- a) W-4: Mehrfach-Treffer je Auftrag und Kuerzel (UPPER, damit auch bei CS-Kollation)
SELECT wo.ProductionOrderId, UPPER(wo.OperationNumber) AS Kuerzel, COUNT(*) AS Anzahl
FROM dbo.WorkOperations wo
GROUP BY wo.ProductionOrderId, UPPER(wo.OperationNumber)
HAVING COUNT(*) > 1;

-- b) W-1: Arbeitsgaenge eines Sub-FA, deren Kuerzel weder in der eigenen Zeile noch in einem Blatt-Kind
--    steht (unter Antwort 6 a ohne Symbol); Spalte Gebucht zeigt, ob darauf schon gebucht wurde.
--    Enthaelt auch in Sage entfernte Kuerzel (Antwort 6 c) — die sind gewollt ohne Symbol.
SELECT po.OrderNumber, po.SubOrderNumber, wo.OperationNumber,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.BdeBookings b WHERE b.WorkOperationId = wo.Id) THEN 1 ELSE 0 END AS Gebucht
FROM dbo.WorkOperations wo
JOIN dbo.ProductionOrders po ON po.Id = wo.ProductionOrderId
JOIN dbo.FaHierarchyNodes n ON n.SubFA <> 0 AND CAST(n.SubFA AS nvarchar(100)) = po.SubOrderNumber
WHERE wo.OperationNumber <> '01'
  AND NOT EXISTS (SELECT 1 FROM STRING_SPLIT(ISNULL(n.Arbeitsschritte, ''), ' ') t
                  WHERE t.value = wo.OperationNumber)
  AND NOT EXISTS (SELECT 1 FROM dbo.FaHierarchyNodes k
                  CROSS APPLY STRING_SPLIT(ISNULL(k.Arbeitsschritte, ''), ' ') t
                  WHERE k.VaterFA = n.SubFA AND k.SubFA = 0 AND t.value = wo.OperationNumber)
ORDER BY Gebucht DESC, po.OrderNumber;

-- c) (iii): Wurzeln ohne Sub-FA und Kinder mit VaterFA = 0
SELECT SUM(CASE WHEN VaterFA IS NULL AND SubFA = 0 THEN 1 ELSE 0 END) AS WurzelnOhneSubFA,
       SUM(CASE WHEN VaterFA = 0 THEN 1 ELSE 0 END) AS KnotenMitVaterFA0
FROM dbo.FaHierarchyNodes;

-- d) Hinweis OrderNumber: Auftraege, deren OrderNumber nicht (mehr) zur HauptFA des Knotens passt
SELECT po.SubOrderNumber, po.OrderNumber, n.HauptFA
FROM dbo.ProductionOrders po
JOIN dbo.FaHierarchyNodes n ON n.SubFA <> 0 AND CAST(n.SubFA AS nvarchar(100)) = po.SubOrderNumber
WHERE po.OrderNumber <> CAST(n.HauptFA AS nvarchar(100));
```

Viele Zeilen mit `Gebucht = 1` in b) machen W-1 dringend (überholt, siehe oben); a) leer heißt, W-4 tritt heute
nicht auf (die Regel braucht es trotzdem, der Code verhindert es nicht).
Seit Antwort 7 (2026-10-09) sind W-2 bis W-4 entschieden; a), c), d) messen nur noch den Umfang (a liefert
zugleich Testdaten für S14).

**P4 — Feiertage und Offset-Tabelle auf der IDEAL-WMS-Datenbank (neu 2026-10-07, vor Rückfrage 8 / Dev-Lauf).**
Der Soll-Termin zählt Werktage ohne Feiertage aus der WMS-Tabelle; ob sie bei IDEAL gepflegt ist, kann der
Spec-Lauf nicht sehen. Ebenso, welche Einträge `OseonOperationConfigs` dort hat (Seed `Program.cs:465-493` läuft
beim Web-Start, wenn die Tabelle leer ist):

**Gültige Fassung ab 2026-10-13 (nach Deploy von `SQL/95` bzw. Bündel ≥ v1.54.0 — neue Namen `dbo.OperationConfigs`,
`IsExternalSystemRelevant`; Seed jetzt `Program.cs:470-499`):**

```sql
SELECT COUNT(*) AS FeiertageAbHeute, MIN([Date]) AS Erster, MAX([Date]) AS Letzter
FROM dbo.Holidays WHERE [Date] >= CAST(GETDATE() AS date);

SELECT OperationName, DisplayName, DueDateOffsetDays, IsExternalSystemRelevant
FROM dbo.OperationConfigs ORDER BY OperationName;

-- Kollision Seed-Namen <-> IDEAL-Kuerzel (getrimmt, case-insensitiv wie der Offset-Lookup)
SELECT c.OperationName, c.DueDateOffsetDays, wp.Name AS IdealWerkbank
FROM dbo.OperationConfigs c
JOIN dbo.ProductionWorkplaces wp
  ON UPPER(LTRIM(RTRIM(wp.ArbeitsschrittCode))) = UPPER(LTRIM(RTRIM(c.OperationName)));
```

Die zweite und dritte Abfrage decken sich mit P1/P3 „nach dem Deploy“ der Vorbedingungs-Spec
([[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]], Abschnitt Prüfschritte) — dort ausgewertet und per UI
bereinigt, hier nur Kontrolle vor der Abnahme von Teil 2. Die erste Abfrage (Feiertage) bleibt Vorbedingung der Abnahme.

**~~Alte Fassung (bis 2026-10-12)~~ — überholt (2026-10-13):** liefert nach dem Deploy von `SQL/95` „Ungültiger
Objektname 'dbo.OseonOperationConfigs'“; nur noch **vor** dem Deploy lauffähig. Als Audit-Spur unverändert:

```sql
SELECT COUNT(*) AS FeiertageAbHeute, MIN([Date]) AS Erster, MAX([Date]) AS Letzter
FROM dbo.Holidays WHERE [Date] >= CAST(GETDATE() AS date);

SELECT OperationName, DisplayName, DueDateOffsetDays, IsOseonRelevant
FROM dbo.OseonOperationConfigs ORDER BY OperationName;

-- Kollision Seed-Namen <-> IDEAL-Kuerzel (relevant fuer Rueckfrage 8 Variante A)
SELECT c.OperationName, c.DueDateOffsetDays, wp.Name AS IdealWerkbank
FROM dbo.OseonOperationConfigs c
JOIN dbo.ProductionWorkplaces wp ON LTRIM(RTRIM(wp.ArbeitsschrittCode)) = c.OperationName;
```

`FeiertageAbHeute = 0` → Werktag-Offsets ignorieren Feiertage (nur Wochenenden); vor Abnahme pflegen. Treffer in
der dritten Abfrage (z. B. `EG`) → bei Variante A gilt der Seed-Offset für das IDEAL-Kürzel, bis er geändert wird.
(Rework 2026-10-12: Variante A ist überholt, Antwort 8. Die zweite und dritte Abfrage sind jetzt Input für die
Vorbedingungs-Spec — dort ist zu klären, wie OSEON-Seed-Namen und IDEAL-Kürzel in den gemeinsamen Stammdaten
nebeneinander stehen. Die erste Abfrage bleibt Vorbedingung der Abnahme von Teil 2.)

## Etappen (nur bei epic: true)

Entfällt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Final (QA 2026-10-13, gegen den echten Diff `d56a340e..57b762a9`):** geändert sind nur `IdealAkeWms/` (Web),
`IdealAkeWms.Tests/`, `docs/` und die Versionskonstante `IDEALAKEWMSService/AppVersion.cs` (nur Versionsnummer, kein
Verhalten) — **deploy.web = true, deploy.service = false, deploy.migration = false** (kein Datei-Neuzugang unter
`*/Migrations/`, kein `SQL/`-Diff, kein Einmal-Skript). Ablauf des Menschen: Publish aus dem Worktree → Testsystem →
Test (Checkliste unten) → Merge. Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien
mit parallelen `main`-Änderungen zusammengeführt hat. Die Service-Versionskonstante 1.55.0 rechtfertigt allein kein
Service-Deploy (wird mit dem nächsten Bündel-Service-Deploy mitgenommen). Reihenfolge auf dem Testsystem: `SQL/95` der
Vorbedingung muss gelaufen sein (Teil 2 selbst bringt kein Skript), dann Web publishen.

Vorläufiger Text des Spec-Laufs (bestätigt):

- **Web-App:** ja (Repository, ViewModel, Controller `FaHierarchy.Index` inkl. Logger, `_FaHierarchyNode`,
  `Index.cshtml` für den Sammelhinweis, CSS; neu 2026-10-13: `OperationConfigsController` Add/Update, FA 4a).
- **Service:** nein (die Erkennungs-Korrektur v1.47.4 kommt mit dem Bündel, nicht mit dieser Spec).
- **Migration:** nein (Antwort 8: Pflege und etwaige Migration gehören zur Vorbedingungs-Spec). ~~außer
  Rückfrage 8 entscheidet Variante B~~ — überholt.
- **Reihenfolge:** im Bündel nach Teil 1 **und** nach der Vorbedingung Arbeitsgang-Stammdaten — beide sind seit
  2026-10-13 umgesetzt (v1.52.0 bzw. v1.54.0, Testbereit). Auf dem Testsystem muss `SQL/95` der Vorbedingung gelaufen
  sein (Migration `20261012140548_RenameOperationConfigsAndAddAudit`, deren Deploy-Abschnitt), bevor die Teil-2-Binaries
  starten — Teil 2 selbst bringt kein Skript.
- **Vor Abnahme:** Offsets im Stammdaten-Reiter „Arbeitsgänge“ pflegen (vormals „laut Rückfrage 8“),
  Seed-Kollisionen auf der IDEAL-DB bereinigen (Vorbedingung P3 / Checkliste Punkt 15), Feiertage prüfen (P4 erste
  Abfrage, gültige Fassung).
- **Publish-Befehle:**

```bash
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Abgleich mit der Umsetzung (2026-10-13)

Gemessen gegen den Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, HEAD `d56a340e`, **v1.54.0**, nach
Umsetzung von Teil 1 ([[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]], v1.52.0, Testbereit) und der
Vorbedingung ([[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]], v1.54.0, Testbereit nach QA-Nachlauf).
Nichts davon wurde still angepasst; die Folgen sind oben im Rumpf eingearbeitet und dort als „2026-10-13“ markiert.

| # | Annahme der Spec | Umsetzung (Beleg, v1.54.0) | Folge |
|---|---|---|---|
| A1 | Vorbedingungs-Spec existiert noch nicht; `depends_on` auf die Backlog-Notiz | Spec existiert, Status Testbereit, umgesetzt in v1.54.0 (Commits `d4bb711a`, `5ac5ea16`, `b5123529`, Fix `d56a340e`) | `depends_on` + Verweise auf [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] umgestellt; Hinweise „Spec folgt“ als überholt markiert |
| A2 | FA 4: ein Lookup Kürzel → Offset, case-insensitiv + getrimmt, „kein Eintrag“ ≠ 0, Relevanz unberücksichtigt; ggf. Umbau eines case-sensitiven Dictionary | `IOperationConfigRepository.GetDueDateOffsetsAsync()` (`OperationConfigRepository.cs:23`, `:67-77`): alle Zeilen, `OperationName.Trim()`, `OrdinalIgnoreCase`, kleinste `Id` gewinnt; Decorator `CachedOperationConfigRepository.cs:41-49` | Erwartung erfüllt, **kein Umbau**; Teil 2 trimmt den Suchschlüssel selbst (Doku `:21`) |
| A3 | Calculator wird von der Vorbedingung evtl. umbenannt | Name und Code unverändert (`Services/OseonDueDateCalculator.cs:3-13`), in der Vorbedingung Out-of-Scope | Teil 2 ruft `OseonDueDateCalculator.Calculate` (affected_code, FA 4 Ergebnis) |
| A4 | Offset-Pflege ist fertig und nicht Teil von Teil 2 (AK 18 „kein Diff an der Offset-Pflege“) | `OperationConfigsController.Add`/`Update` binden `int dueDateOffsetDays` ohne Prüfung (`:72`, `:107`) → leerer Offset per manipuliertem POST = still 0 (Vorbedingungs-QA-Nachlauf N-3, Aufgabe [[2026-10-12-operationconfig-offset-leer-pruefen]]) | Auf Auftrag der Koordination (2026-10-13) als Vorbedingung im selben Dev-Lauf: FA 4a, AK 19, S17, AK 18 präzisiert. Die Freigabe von Teil 2 bestätigt diese Umfangserweiterung |
| A5 | Teil-2-P4: Seed-Kollision OSEON ↔ IDEAL-Kürzel war Input für die Vorbedingung | Gemeinsamer Namensraum bleibt; Seed-Code unverändert (`Program.cs:471`, Vorbedingungs-Antwort 3), Bereinigung per UI nach deren P3 | Abnahme-Vorbedingung von Teil 2 (Testszenarien, Deploy „Vor Abnahme“) |
| A6 | P4 fragt `dbo.OseonOperationConfigs` / `IsOseonRelevant` ab | Tabelle/Spalte umbenannt (`Models/OperationConfig.cs:11`, `:34`; `SQL/95`) | P4 neue Fassung, alte als überholt markiert (sonst „Ungültiger Objektname“) |
| A7 | FA 7: Teil 1 setzt `aria-label` = Werkbank-Name bzw. Kürzel; Teil-2-Format „KA Kanten – erledigt“ | `FaArbeitsschrittSymbol.Beschriftung` = „KA – Name“ bzw. „KA“ (`FaHierarchyTreeViewModel.cs:218-222`), gerendert `_FaHierarchyNode.cshtml:108` | Format „`<Beschriftung>` – `<Status>`“ (FA 7, Abgleich-Absatz); folgt dem schon entschiedenen Grundsatz „ergänzen statt ersetzen“, keine Rückfrage |
| A8 | Ein Symbol je Kürzel und Zeile (implizit) | Teil 1 dedupliziert nicht (Ruling R1) | Gleiches Kürzel doppelt in einer Zeile → zwei Symbole mit gleichem Status; keine Regeländerung |
| A9 | Symbol aus Teil 1 neutral, Farbe für den Status frei | Bestätigt: `.fa-ag-symbol` ohne Füllfarbe (`site.css:1350-1359`). **Aber:** Kontextzeilen werden mit `opacity: 0.6` gedimmt (`site.css:1361`), Teil-1-QA M1 offen („Entscheidung beim Menschen“) | Statusfarben fallen in gedimmten Zeilen unter WCAG AA → **Rückfrage 11**; AK 8 bis dahin für nicht gedimmte Zeilen |
| A10 | „Die Tests, die den Controller direkt konstruieren, ziehen mit“ | Kein Test konstruiert `FaHierarchyController` (Grep: 0 Treffer) | Satz überholt |
| A11 | `FaHierarchyController`-Zeilen v1.49.0 | Teil 1 hat Ctor (`IProductionWorkplaceRepository`, `:43`) und Index (`Zuordnen`, `:100-102`) erweitert; Werkbänke werden schon einmal geladen (`:101-102`) | Zeilen aktualisiert; `BdeAktiv` darf aus der geladenen Werkbank-Liste kommen (Lösungsentwurf), Abfragebudget AK 9 unverändert |
| A12 | Pflegeort-Text „BDE-Stammdaten › Arbeitsgänge“ | Reiter `_BdeMasterDataTabs.cshtml:32`, Menü `_Layout.cshtml:315`, TS-87-Titel identisch; nur Admin (`OperationConfigsController.cs:18`) | Text bleibt; Benutzer der FA-Struktur ohne Admin sehen den Text, nicht den Link (wie geplant) |
| A13 | Liste „nicht konfiguriert“ kennt IDEAL-Kürzel | `GetUnconfiguredOperationsAsync` (`:82-97`): OSEON + WMS-`OperationNumber`, gleiche Normalisierung, ohne `"01"` | Konsistent mit dem Sammelhinweis von Teil 2 |
| A14 | Stand v1.49.0, TS-82 | v1.54.0, TS-87, `SQL/95`, Migration `20261012140548_…`; `main` `SQL/88` | Hinweise aktualisiert; Teil 2 → voraussichtlich TS-88 |

**Konkordanz der Zeilenangaben** (Rumpf und Kästen zitieren teils ältere Stände; der Dev-Lauf sucht per Bezeichner):

| Datei | alt | v1.54.0 |
|---|---|---|
| `IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs` | v1.47.1-Zeilen ab Z.92 | **+2** (wie v1.47.4, seither unverändert): Scope `:103-104`, Dedupe `:108`/`:115`, Ausschluss `:118`, mehrdeutig `:122-126`, Map-Comparer `:128`, unbekannt `:133`, Auftragsfilter `:144-147`, Re-Add-Sperre `:153-165`, `OperationNumber = token` `:171`, `IsReportable` `:175`, `IsReported = false` `:177`; Z.1-91 unverändert (Katalog `:80`) |
| `IdealAkeWms/Data/ApplicationDbContext.cs` | `:804-805` (Indizes `WorkOperation`), `:807-810` (FK), `:430` (`SubOrderNumber` unique) | `:806-807`, `:809-812`, `:432` |
| `IdealAkeWms/Controllers/FaHierarchyController.cs` | Ctor `:34-41`, Index `:52-133`, OrderInfos `:68`, Paginierung `:86-89`, Existenz-Check `:91-95`, Tiefen-Cap `:98-103`, Modal `:139-155` | Ctor `:35-53`, Index `:55-140`, `:71`, `:86-92`, `:94-98`, `:104-110`, `:146-162` |
| `IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs` | `IsOrphan` `:53-57`, Zuordnungsmuster `:171-176` | `:55-58`, `:176-181` |
| `IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml` | Typ-Icons `:27-53` | `:28-54`; Symbol-Span `:108` |
| `IdealAkeWms/Views/FaHierarchy/Index.cshtml` | Kombigerät ohne Einzeltermin `:149-151` | `:151-153` |
| `OseonOperationConfigRepository.cs` / `SettingsController.cs:110-164` / `Models/OseonOperationConfig.cs` | Befund v1.49.0 | entfallen bzw. umbenannt: `OperationConfigRepository.cs`, `OperationConfigsController.cs`, `Models/OperationConfig.cs` |
| unverändert gemessen | `WorkOperationRepository.cs:59-82`, `:94-97`, `:120-128`; `BdeApiController.cs:197`, `:246-273`; `BdeBookingRepository.cs:62`; `OseonDueDateCalculator.cs:9-13`; `ProductionWorkplace.cs:21`; `RequireBdeActiveAttribute.cs:24`; `IHolidayRepository.cs:8`; `FaHierarchyTreeBuilder.cs:104`; `FaMaterializationSyncService.cs:146-148` | — |

**Ergebnis:** Teil 2 ist bis auf **Rückfrage 11** (Kontrast in gedimmten Kontextzeilen) freigabereif.

## Offene Rueckfragen

> [!success] Fragen 1-5 beantwortet (Keyuser 2026-10-07; Nr. 2 mit Nachtrag Gerald), Frage 7 teilweise (W-1)
> Übernommen in „Fachliche Anforderungen“ 2-7a, Lösungsentwurf, AK 1-10a/16, Testszenarien S1-S6/S9. Die Fragen
> sind in `beantwortete_rueckfragen` verschoben und bleiben unten als Audit-Trail stehen. Offen: Nr. 7 (nur noch
> W-2 bis W-4), neu Nr. 8 (Offset-Pflege, aus Antwort 2 „Offen für die Spec“), Nr. 9 (Kombigeräte, Rest aus
> Nr. 2), Nr. 10 (Vorrang läuft/überfällig, aus Antworten 2 + 5). Die Nummerierung entspricht dem Freigabe-Block.

1. ~~**Codierung ohne Farbe — wie genau?**~~ — **beantwortet (Antwort 1)** (Notiz-Punkt 3) „Farbe nie alleiniger Bedeutungsträger“ ist
   Pflicht (CLAUDE.md, Fertigungsterminals). Empfehlung der Notiz: Status zusätzlich über Form/Icon und
   Text (Tooltip, Screenreader), Kontrast WCAG AA. Zu entscheiden: welche Form/welches Icon je Status
   (z. B. Haken / leerer Kreis / Warndreieck), und ob der Statustext sichtbar am Symbol steht oder nur
   per `aria-label` (Hinweis: `title` erscheint an Touch-Terminals nicht).
2. ~~**„Überfällig“ — welcher Termin?**~~ — **beantwortet (Antwort 2: Offset-Logik, Bezug `FE_Termin`; Rest →
   Nr. 8 und Nr. 9)** (Notiz-Punkt 4) Es gibt kein Soll-Datum je Arbeitsgang. Empfehlung
   der Notiz: z. B. Auftragstermin (`FE_Termin`?) überschritten und Arbeitsgang nicht rückgemeldet; welcher
   Termin gilt, entscheidet Gerald. Befund: Kandidaten sind `FE_Termin` („Blechfertigung fertig“ laut
   View-Doku) und `Neuer_PT_PPS` („aktueller PPS-Fertigstellungstermin“) — beide unverifiziert, siehe P1.
   Teilfrage: Welche Zeile gilt bei Kombigeräten mit mehreren Kopfzeilen je HauptFA (siehe P2)?
3. ~~**„Erledigt“ — welches Signal?**~~ — **beantwortet (Antwort 3: `IsFinal`)** (neu, aus dem Code-Befund) BDE-Abschluss (`IsFinal`, wie das Terminal)
   oder manuelle Tracking-Rückmeldung (`IsReported`) oder eines von beiden? Empfehlung der Spec: `IsFinal`,
   weil die Anforderung „bei aktivem BDE“ lautet und das Terminal genau dieses Signal nutzt; die
   Entscheidung bleibt beim Menschen.
4. ~~**„Bei aktivem BDE“ — welcher Schalter?**~~ — **beantwortet (Antwort 4: beide)** (neu, aus dem Code-Befund) Globales `BdeAktiv` (AppSetting),
   `BdeAktiv` der Werkbank des Arbeitsgangs, oder beides? Und: neutrale Symbole bei BDE aus — richtig?
5. ~~**Laufende Buchung**~~ — **beantwortet (Antwort 5: eigener Zustand „läuft“)** (neu, aus dem Code-Befund): grau wie „offen“ oder ein vierter Zustand „läuft“
   (eigene Form/Text)?
> [!success] Frage 6 beantwortet (Antwort 6, Gerald 2026-10-06)
> Regel a/b/c übernommen in „Fachliche Anforderungen“ 9, AK 11-14, Testszenarien S8-S11. Die Frage ist in den
> Frontmatter-Schlüssel `beantwortete_rueckfragen` verschoben und bleibt unten als Audit-Trail stehen. Der
> Code-Abgleich ergab vier Widersprüche (W-1 bis W-4) → neue Rückfrage 7.

6. ~~**Nur falls Teil 1 Rückfrage 1 = S (Sage-Kürzel): Zuordnung Kürzel → `WorkOperation`**~~ —
   **beantwortet (Antwort 6)** (neu, aus dem
   Sage-Vorbild; Befund „Fachliche Anforderungen“ 8). Status gibt es nur über `WorkOperation`
   (`IsFinal`/`IsReported`). Zu entscheiden:
   - a) Welche `WorkOperation` gilt für ein Kürzel an einer **Sub-FA-Zeile** — die eigene (Sub-FA der Zeile)
     oder die beim Vater erfasste (DirectChildren-Scope)? Für **Blattzeilen** gibt es nur den Vater.
   - b) Darstellung für Kürzel **ohne** `WorkOperation` (unbekannt, ausgeschlossen, mehrdeutig, Auftrag
     erledigt/nicht materialisiert): neutral wie Teil 1, eigener Zustand „nicht erfasst“ (eigene Form + Text),
     oder ausblenden?
   - c) `WorkOperation` **ohne** Kürzel (Default-AG `01`, in Sage nachträglich entfernte Kürzel): weglassen
     oder zusätzlich als Symbol zeigen?
   Entfällt bei Quelle W.
> [!success] Fragen 7 (Rest W-2 bis W-4), 8, 9, 10 eingearbeitet (Antworten Gerald 2026-10-09; Rework 2026-10-12)
> Nr. 7 → FA 7b, FA 9 a/b, AK 15/17, S12-S14; Nr. 8 → FA 4 (Befund Calculator, Schnittstelle, Meldung „nicht
> konfiguriert“), Vorbedingung + `depends_on`, Migration/Deploy, AK 10a/10c/18, S15; Nr. 9 → FA 4, AK 10b, S16;
> Nr. 10 → FA 3/7/7a, AK 3/10, S5. In `beantwortete_rueckfragen` verschoben, Fragetext unten als Audit-Trail.
> Teil 2 hat **keine** offene Rückfrage mehr.

7. ~~**Widersprüche Antwort 6 ↔ Erkennungs-Code**~~ — **eingearbeitet (Antwort 7, 2026-10-09)** (neu, Rework 2026-10-06; Befund und Belege „Fachliche
   Anforderungen“ 9, Umfang per P3). Antwort 6 a nennt die Regel „genau den Scope der Erkennung“; der Code
   weicht in vier Fällen ab. Zu entscheiden je Fall:
   - ~~**W-1** Kürzel einer Sub-FA-Zeile `S` unter Sub-FA `P` ist als `WorkOperation` an `S` **und** an `P`
     erfasst; die Regel zeigt nur die an `S`. Soll der `P`-Arbeitsgang irgendwo sichtbar werden (z. B. an der
     Zeile `P` zusätzlich, als Hinweis), oder bleibt er bewusst ohne Symbol?~~ — **beantwortet (Antwort 7:
     Variante b, Erkennung korrigiert in v1.47.4)**
   - **W-2** Zeile mit `SubFA ≠ 0`, aber ohne eigene `WorkOperation` für das Kürzel (nicht materialisiert,
     bei Erkennung schon fertig): „kein BDE-Arbeitsgang“ wörtlich nach Regel a, oder Rückfall auf den
     Vater-Sub-FA wie bei Blättern?
   - **W-3** Waisen-Blatt (VaterFA ohne importierten Knoten): alte `WorkOperation` des Vater-Auftrags
     verwenden oder „kein BDE-Arbeitsgang“?
   - **W-4** Mehrere `WorkOperation`s mit gleichem Kürzel am selben Auftrag: welcher Status gilt (z. B.
     erledigt, wenn einer erledigt / wenn alle erledigt)?
   Die Spec wählt nicht. (Nachtrag 2026-10-07: zu W-2 siehe den Nachtrag im W-2-Kasten — seit v1.47.4 träfe ein
   Rückfall auf den Vater nie das Kürzel dieser Zeile.)
8. ~~**Offsets je IDEAL-Kürzel — wo gepflegt, und was ohne Eintrag?**~~ — **eingearbeitet (Antwort 8,
   2026-10-09: weder A noch B, Vorbedingungs-Spec)** (neu 2026-10-07; Antwort 2 nennt es
   „Offen für die Spec“; Befund FA 4)
   - **A — bestehende Tabelle `OseonOperationConfigs`** (Kürzel als `OperationName`, Pflege unter
     `/Settings/OperationConfig`, nur Admin, `SettingsController.cs:10`, `:110-164`): keine Migration, kein neuer
     Pflege-Screen. Nachteile: geteilter Namensraum mit den OSEON-Seed-Einträgen (`Program.cs:467-481`, z. B.
     `EG` Offset 0 — `EG` kommt auch im IDEAL-Vorbild vor), Feldtext „relativ zum Stanztermin“
     (`OseonOperationConfig.cs:22-23`) und „noch nicht konfiguriert“-Liste (nur OSEON-Namen,
     `OseonOperationConfigRepository.cs:55-67`) passen nicht zu IDEAL; `IsOseonRelevant` ohne Bedeutung.
   - **B — neues Feld an der Werkbank** (`ProductionWorkplace`, Kürzel = `ArbeitsschrittCode`, ein Kürzel trifft
     genau eine Werkbank, sonst gibt es keinen Arbeitsgang): fachlich sauber getrennt, aber Migration + Pflege im
     Werkbank-Stamm; der Werkbank-Sync darf es nicht überschreiben.
   - Teilfrage: Kürzel **ohne** Eintrag → Offset 0 (Soll = `FE_Termin`, wie Teileverfolgung
     `OseonGroupViewModelBuilder.cs:50-52`) oder kein Überfällig-Status (wie OSEON-Reporting, Zeile übersprungen,
     `OseonReportingController.cs:74`)?
   - Empfehlung der Spec: **A** mit Default **Offset 0** — kleinster Eingriff (Leiter-Sprosse 2), die Werte sind
     Daten; die Kollision ist per P4 prüfbar und durch Pflege behebbar. Entscheidung beim Menschen.
9. ~~**Kombigeräte — welcher `FE_Termin`?**~~ — **eingearbeitet (Antwort 9, 2026-10-09: frühester)** (neu 2026-10-07; unbeantworteter Rest aus Nr. 2) Mehrere
   `FaHierarchyOrderInfo` je HauptFA (P2). Frühester `FE_Termin`, spätester, oder für diese Strukturen kein
   Überfällig-Status? Die FA-Liste zeigt bei Kombigeräten bewusst keinen Einzeltermin
   (`ProductionOrdersController.cs:314-329`). Die Spec wählt nicht.
10. ~~**Vorrang „läuft“ vs. „überfällig“**~~ — **eingearbeitet (Antwort 10, 2026-10-09: beides kombiniert)** (neu 2026-10-07; aus Antworten 2 und 5) Ein Arbeitsgang mit laufender
    Buchung, dessen Soll-Termin vor heute liegt: „läuft“ (zeigt, dass gearbeitet wird) oder „überfällig“ (zeigt
    die Terminlage)? Die Spec wählt nicht.

> [!question] Neu aus dem Abgleich 2026-10-13: Rückfrage 11
> Der Abgleich mit Teil 1 (Abschnitt „Abgleich mit der Umsetzung“, A9) ergab eine Entscheidung, die der Code nicht
> trifft. Die Aussage „Teil 2 hat **keine** offene Rückfrage mehr“ im Kasten oben ist damit **überholt** — offen ist
> nur Nr. 11.

11. **Kontrast der Statusfarben in gedimmten Kontextzeilen** (neu 2026-10-13, Widerspruch AK 8 ↔ Teil-1-Umsetzung).
    Der Baum-Spaltenfilter dimmt die Vorfahren-Zeilen eines Treffers mit `.fa-node-row.fa-node-context { opacity: 0.6; }`
    (`site.css:1361`, gesetzt in `fa-hierarchy-tree.js`). Die Teil-1-QA hat das als **M1** gemessen (Chip-Text 4,2:1,
    Kontur 3,0:1 bzw. 2,7:1 auf Hover — unter WCAG AA) und die Entscheidung beim Menschen gelassen; sie ist offen
    (Teil-1-Spec, QA-Nachlauf „M1 nicht behoben“). Teil 2 legt Statusfarben auf diese Symbole; Rot, Grün und Grau
    verlieren in gedimmten Zeilen ebenso Kontrast. AK 8 („Kontrast aller Statusvarianten ≥ WCAG AA“) ist für diese
    Zeilen so nicht erfüllbar, ohne die Dimmung anzufassen. Optionen:
    - **A** AK 8 gilt für nicht gedimmte Zeilen (normal, Hover, Hervorhebung, Filtertreffer); gedimmte Kontextzeilen
      bleiben wie Teil-1-M1 (vorbestehend, betrifft alle Zellen gleich). Die Bedeutung bleibt über Zeichen und
      `aria-label` erhalten, die Kontextzeile ist bewusst „Nicht-Treffer“. Kein Zusatzcode.
    - **B** Teil 2 nimmt die Statussymbole von der Dimmung aus (z. B. eigene Regel für `.fa-node-context .fa-ag-symbol`),
      damit Statusfarben überall AA erreichen. Wenig CSS, aber die Kontextzeile wirkt dann uneinheitlich (Text gedimmt,
      Symbole voll).
    - **C** Die Dimmung grundsätzlich auf AA bringen (alle Zellen) — eigene Folgeaufgabe, nicht in Teil 2.
    Empfehlung der Spec: **A** (kleinster Eingriff; Farbe ist ohnehin nie alleiniger Träger, FA 7), bei Bedarf C als
    getrennte Aufgabe. Entscheidung beim Menschen.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Codierung ohne Farbe — Form/Icon je Status, Statustext sichtbar oder nur aria-label? → **Zusätzlich ein Zeichen je Status – ja (Keyuser 2026-10-07): Haken = erledigt, leerer Kreis = offen, Warndreieck = überfällig, eigenes Zeichen für „läuft“; Statustext per aria-label.**
2. Überfällig — welcher Termin (FE_Termin / Neuer_PT_PPS / anderer), Regel bei Kombigeräten? → **Berechnungslogik mit Offset-Tagen wie im OSEON-Reporting / der Teileverfolgung (Keyuser 2026-10-07). Am Code: `Services/OseonDueDateCalculator.cs` (Bezugstermin + Offset in Werktagen) mit `OseonOperationConfig.DueDateOffsetDays` („Offset in Arbeitstagen relativ zum Stanztermin“). Offen für die Spec: Bezugstermin für IDEAL (statt Stanztermin) und wo die Offsets je IDEAL-Kürzel gepflegt werden.** **Nachtrag (Gerald 2026-10-07): Bezugstermin ist vorerst `FE_Termin`.**
3. Erledigt — IsFinal (BDE), IsReported (Tracking) oder eines von beiden? → **BDE-Rückmeldung (IsFinal) (Keyuser 2026-10-07).**
4. BDE aktiv — globaler Schalter, Werkbank-Schalter oder beides; neutral bei BDE aus? → **Beide müssen aktiv sein (global und Werkbank) (Keyuser 2026-10-07); sonst neutral.**
5. Laufende Buchung — grau oder eigener Zustand „läuft“? → **Eigener Zustand „läuft“ wie vorgeschlagen (Keyuser 2026-10-07).**
6. Nur bei Teil-1-Quelle S: Zuordnung Kürzel → WorkOperation (Sub-FA-Zeile eigene oder Vater), Darstellung Kürzel ohne WorkOperation (neutral / „nicht erfasst“ / ausblenden), WorkOperation ohne Kürzel (weglassen / zeigen)? → **(Teil 1: Quelle S, Gerald 2026-10-06.) a) Zuordnung: Ist die Zeile selbst ein materialisierter Sub-FA (SubFA ≠ 0), gilt ihr eigener Arbeitsgang mit gleicher OperationNumber; sonst der des Vater-Sub-FA (VaterFA) – genau der Scope der Erkennung. b) Kürzel ohne WorkOperation (Ausschlussliste, unbekannt, mehrdeutig, Auftrag bei Erkennung schon fertig): anzeigen, ohne Status, neutral, mit Hinweis „kein BDE-Arbeitsgang“ (Text/aria-label, nicht nur Farbe). c) WorkOperation ohne Kürzel (Default `01`, in Sage entfernte Kürzel): nicht als Symbol zeigen.**
7. Widersprüche Antwort 6 ↔ Code — W-1 Vater-AG eines Sub-FA-Kind-Kürzels sichtbar machen?; W-2 Rückfall auf den Vater, wenn die eigene WorkOperation fehlt?; W-3 Waisen-Blatt: alte Vater-WorkOperation oder „kein BDE-Arbeitsgang“?; W-4 Mehrfach-Treffer: welcher Status gilt? → **W-1: Variante b (Keyuser 2026-10-07) – ein Kürzel auf einer Sub-FA-Zeile gehört nur zum eigenen Sub-FA, nicht zusätzlich zum Vater (Beispiel `VM` Mittelholmblende nicht am Korpus). Kürzel der Teile ohne eigenen FA (z. B. `LS KA`) bleiben am Vater-Sub-FA rückmeldbar. Folge: Korrektur der Erkennung im Bündel (`WorkOperationStructureDetectionService.cs:100-102`), eigener Bugfix vor dem Bündel-Merge. W-2 bis W-4: noch offen (IT).** **Nachtrag (Gerald 2026-10-09): W-2 – kein Rückfall auf den Vater, Symbol „kein BDE-Arbeitsgang“. W-3 – Waisen-Blatt: „kein BDE-Arbeitsgang“, keine alten Vater-Arbeitsgänge. W-4 – mehrere WorkOperations gleiches Kürzel am selben Auftrag: zusammengefasst („läuft“, wenn einer läuft; „erledigt“ nur, wenn alle erledigt; sonst „offen“; „überfällig“ nur für nicht erledigte) + Log-Hinweis auf den Doppel-Eintrag.**
8. Offsets je IDEAL-Kürzel — A (OseonOperationConfigs wiederverwenden) oder B (Feld an der Werkbank, Migration); Kürzel ohne Eintrag: Offset 0 oder kein Überfällig-Status? → **Weder A noch B (Gerald 2026-10-09): Arbeitsgang-Stammdaten für OSEON und IDEAL vereinheitlichen, als Reiter „Arbeitsgänge“ in den BDE-Stammdaten; „OSEON-relevant“ → „Fremdsystem-relevant“; der Abgleich prüft gegen diese Stammdaten. Eigene Vorbedingungs-Spec aus [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen]]; Teil 2 liest den Offset von dort. Kürzel ohne Eintrag: Arbeitsgang wird angelegt, Offset 0, Meldung „nicht konfiguriert“ (Gerald 2026-10-09).**
9. Kombigeräte (mehrere Kopfzeilen je HauptFA) — frühester FE_Termin, spätester, oder kein Überfällig-Status? → **Frühester `FE_Termin` der Kopfzeilen des HauptFA (Gerald 2026-10-09).**
10. Vorrang bei laufender Buchung und überschrittenem Soll-Termin — „läuft“ oder „überfällig“? → **Beides (Gerald 2026-10-09): Status „läuft“ mit zusätzlichem Hinweis „überfällig“ (Text + Farbe), keine Information geht verloren.**
11. Kontrast der Statusfarben in gedimmten Kontextzeilen (opacity 0.6) — A AK 8 nur für nicht gedimmte Zeilen (Empfehlung), B Statussymbole von der Dimmung ausnehmen, oder C Dimmung als eigene Folgeaufgabe auf AA bringen? → **C – Dimmung in Teil 2 mitbeheben (Gerald 2026-10-13, abweichend von der Empfehlung A).** Kontextzeilen nicht mehr per `opacity` dimmen, sondern mit einer anderen Kennzeichnung (z. B. Hintergrundton/Kursiv), bei der Text, Kuerzel und Statusfarben in ALLEN Zeilen WCAG AA erreichen; AK 8 gilt dann ohne Ausnahme. Erledigt zugleich [[2026-10-12-fa-baum-vorfahrenzeilen-kontrast]]. Umfang-Bestaetigung: FA 4a / AK 19 (leerer Offset wird abgewiesen) gehoert zu Teil 2 (Gerald 2026-10-13).**

> **FREIGABE-NACHTRAG (Gerald 2026-10-13, Antwort 11 = C):** Die Kontextzeilen-Dimmung (`site.css:1361`, `opacity: 0.6`)
> wird in diesem Lauf ersetzt durch eine Kennzeichnung ohne Deckkraft-Reduktion (Vorschlag: dezenter Hintergrundton
> und/oder Kursivschrift des Zeilentexts), bei der Text, Arbeitsgang-Kuerzel und alle Statusvarianten in normalen,
> Hover-, hervorgehobenen und Kontextzeilen WCAG AA erreichen. AK 8 gilt fuer alle Zeilen; Kontrastwerte im Bericht
> nennen. Zusaetzlich gehoert FA 4a / AK 19 (leerer Offset abweisen) zum Umfang. Geht dem Rumpf vor.

## Umsetzung (2026-10-13, Dev-Lauf)

Direkt im Bündel `feature/2026-08-07-ideal-teile-1-5`, Start `d56a340e` (v1.54.0 — der Menü-Lauf lief vorher nicht).
Plan `docs/superpowers/plans/2026-10-13-fa-struktur-arbeitsgaenge-symbole-teil-2.md`. Commits `539be27f`, `e33cdc83`,
`7690d281`, `6d85810b`, `99a3b09e` (feature-complete). Version **1.55.0**, TS-88 + TS-87.3-Negativfall. Keine Migration.
Aufgabe [[2026-10-13-fa-struktur-arbeitsgaenge-symbole-teil-2-umsetzung]].

**Code-Einstieg:** `WorkOperationRepository.GetForFaStatusAsync` / `GetBdeStatusAsync` (records `FaStatusWorkOperation`,
`FaBdeStatus` in `IWorkOperationRepository.cs`); reine Funktion `FaArbeitsgangStatus.Zuordnen`
(`Models/ViewModels/FaArbeitsgangStatus.cs`, enum `FaAgStatus`); `FaArbeitsschrittSymbol` um `Status`, `Soll`, `StatusText`,
`AriaLabel` erweitert (kein zweiter Typ); `FaHierarchyController.Index` (BDE-Gate, eine `LogWarning` je Seitenaufbau);
`FaHierarchyTreeViewModel.NichtKonfigurierteKuerzel` + `alert-warning role=status` in `Index.cshtml`; Status-Chips +
Inline-SVG (`@functions` in `_FaHierarchyNode.cshtml`); `site.css` Status-Varianten + Kontextzeile kursiv/Randstreifen.

**Rulings (Entscheidungen im Lauf, ohne Rückfrage):**
- R1: Die Status-Methode erhält die Ids **aller** WorkOperations der Seiten-HauptFAs mit BDE-aktiver Werkbank (Obermenge der
  aufgelösten Ids) — gleiche Abfragezahl (AK 9), keine zweite Zuordnungsrunde. Kosten bei Fehler: etwas größere Id-Liste
  (OPENJSON, ein Parameter).
- R2: Werkbank-Gate aus der in Teil 1 schon geladenen Werkbank-Liste (`ProductionWorkplace.BdeAktiv`); `ProductionWorkplaceId
  = null` gilt als nicht aktiv → neutral.
- R3: „Kein BDE-Arbeitsgang“ ohne sichtbares Zeichen (neutral wie Teil 1), Text nur in `aria-label`/`title` (FA 9 b erlaubt
  das). Soll-Datum steht nur bei den überfälligen Varianten im Text (Tabelle FA 7).
- R4: Sammelhinweis nennt jedes Kürzel einmal, alle Kürzel mit Status (auch erledigte) ohne Offset-Eintrag (FA 4
  „Kürzel, die einen Status tragen“).
- R5: W-4-Liste dedupliziert je Paar (Blatt und Vater zeigen dieselbe WorkOperation) → genau eine Warnung je Seitenaufbau.
- R6: „Läuft“ = Sanduhr (Spec-Vorschlag; Play wäre ein Dreieck wie „überfällig“). Läuft + überfällig = Läuft-Chip mit 2px
  rotem Rand + zweites Zeichen Warndreieck.
- R7 (Antwort 11 = C): Kontextzeile `font-style: italic` + `box-shadow: inset 4px 0 0 #495057` an der ersten Zelle statt
  `opacity: 0.6`; Textfarbe unverändert.

**Kontrastwerte (WCAG 2.x, rechnerisch, Bootstrap 5.3.3-Farben):** Chip-Text gegen Chip-Grund: erledigt `#0a3622`/`#d1e7dd`
10,35:1 · läuft `#052c65`/`#cfe2ff` 10,28:1 · überfällig `#58151c`/`#f8d7da` 10,22:1 · offen `#212529`/`#e9ecef` 13,01:1 ·
neutral 15,43:1. Zeichen/Rand gegen Chip: 4,97 / 4,90 / 4,86 / 6,90:1; roter Akzent `#b02a37` auf Läuft-Grund 4,94:1. Rand
gegen Zeilengründe (weiß / Hover `#f5f5f5` / Hervorhebung `#d9f4ff` / Filtertreffer `#fff3cd`): ≥ 5,62:1. Da jeder Chip
einen eigenen Grund hat, gelten die Chip-Werte in allen Zeilenzuständen inkl. Kontext. Zeilentext `#4a494a` in Kontextzeilen
(keine Deckkraft mehr): 8,96 / 8,22 / 7,83 / 8,09:1; Randstreifen `#495057`: 8,18 / 7,50 / 7,14 / 7,38:1. Messung am
Testsystem bleibt Teil der Abnahme (TS-88.10).

**Abfragebudget (AK 9/13), am Code belegt:** BDE an → `GetForFaStatusAsync` (1 Statement), `GetBdeStatusAsync` (2), Feiertage
(gecacht), Offsets (gecacht); BDE aus → keine davon (`if (bdeAktiv && …)`). OPENJSON per ToQueryString-Test. Eine
Query-Zählung gegen SQL Server ist nicht automatisiert (Controller nicht InMemory-testbar) → TS-88.11.

**Beweis:** `dotnet build IdealAkeWms.slnx` → 0 Fehler; `dotnet test` → Web 1826 bestanden / 1 übersprungen, Service 272
bestanden. Neue Tests: `FaArbeitsgangStatusTests` (25 Fälle), `WorkOperationRepositoryFaStructureTests` (+3),
`OperationConfigsControllerTests` (+3).

## QA (2026-10-13, qa-agent)

Geprüft im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig `feature/2026-08-07-ideal-teile-1-5`,
Bereich `d56a340e..99a3b09e` (5 Dev-Commits) + QA-Commit `57b762a9` (nur `docs/TESTSZENARIEN.md`). Web-App nicht gestartet,
keine DB berührt. **Ergebnis: Testbereit** — keine Critical/Important-Befunde.

### 1. Build und Tests (HEAD `99a3b09e`; der QA-Commit ändert nur Doku)

- `dotnet build IdealAkeWms.slnx` → **0 Fehler**, 12 Warnungen (vorbestehend, u. a. CS8321 `FaWorklist/Index.cshtml`).
- `dotnet test` → **IdealAkeWms.Tests: 1826 bestanden / 1 übersprungen / 0 fehlgeschlagen (1827)**;
  **IDEALAKEWMSService.Tests: 272 bestanden / 0 / 0 (272)**.
- Neu: `FaArbeitsgangStatusTests` (14 Methoden, 25 Fälle inkl. Theory-Zeilen), `WorkOperationRepositoryFaStructureTests` +3,
  `OperationConfigsControllerTests` +3.

### 2. Code-Review (selbst durchgeführt, in diesem Lauf)

Gelesen: kompletter Diff `d56a340e..99a3b09e` (19 Dateien) und Umfeld — `FaHierarchyController.Index` (Filter/Paginierung),
`FaHierarchyTreeBuilder` (Waisen-Strukturen `:83-107`), `FaHierarchyStructure`, `RequireBdeActiveAttribute.cs:24` (gleiches
`BdeAktiv`-Parsing), `OperationConfigRepository.GetDueDateOffsetsAsync` + Decorator (Comparer bleibt über den Cache
erhalten), `FaMaterializationSyncService.cs:146-148` (`SubOrderNumber = SubFA.ToString()` → Schlüssel passt), DI
`Program.cs:90/126/151` (alle drei neuen Ctor-Abhängigkeiten registriert), Bootstrap 5.3.3, `site.css`
Zeilenhintergründe `:1344-1348`.

Geprüfte Punkte:
- **Zuordnung FA 9 a / W-2 / W-3 / W-4** (`FaArbeitsgangStatus.cs`): `SubFA ≠ 0` → eigener Auftrag, kein Vater-Rückfall (W-2
  ok); Blatt → `VaterFA` nur, wenn ein Knoten mit `SubFA == VaterFA` in **derselben** Struktur existiert (W-3 ok; Waisen sind
  eigene Strukturen mit echtem Wurzelknoten, `FaHierarchyTreeBuilder.cs:86`; der Server-Filter wählt nur Strukturen und
  entfernt nie Knoten); `VaterFA` null/0 → kein BDE-Arbeitsgang; Schlüssel zusätzlich `OrderNumber == HauptFA` (fremde HauptFA
  trifft nicht); Kürzel getrimmt + `ToUpperInvariant` auf beiden Seiten. W-4: alle Treffer, Zusammenfassung FA 7a/7b korrekt
  (erledigt hat je WorkOperation Vorrang vor läuft; „alle erledigt“ nur über BDE-aktive Treffer).
- **Gate FA 6:** global `BdeAktiv` identisch zum Filter geparst; bei aus → keine der vier Abfragen (`if (bdeAktiv && …)`),
  Status bleibt `null` → Markup wie Teil 1. Werkbank-Gate über die schon geladene Werkbank-Liste; `ProductionWorkplaceId =
  null` → neutral (R2).
- **Soll/Offset FA 4:** `OseonDueDateCalculator.Calculate` aufgerufen, nicht kopiert; frühester nicht leerer `FE_Termin`
  (`Min` über `DateTime?` ignoriert `null`; alle leer → `null` → nie überfällig); Offset-Lookup mit getrimmtem Kürzel gegen
  das `OrdinalIgnoreCase`-Dictionary; fehlend → 0 **und** Sammelhinweis (kein stilles `?? 0`); Vergleich
  `Soll.Date < heute.Date`.
- **Abfragebudget AK 9:** bei BDE an `GetForFaStatusAsync` (1 Statement, OPENJSON), `GetBdeStatusAsync` (2 Statements,
  OPENJSON), Feiertage (`CachedHolidayRepository`), Offsets (gecacht) — kein DB-Zugriff in Schleifen.
- **aria-label:** `AriaLabel = "<Beschriftung> – <Status>"`, ohne Status unverändert Teil 1; Soll nur in den überfälligen
  Varianten (R3). `role="img"` + `aria-label`/`title`, SVGs `aria-hidden`.
- **`@Html.Raw(agSvg)`:** nur Konstanten aus `@functions` (keine Daten) — unbedenklich. Keine Werte in `<script>`-Blöcken,
  kein `hidden` + `d-*`, keine parallelen Arrays.
- **Kontrast AK 8 / Antwort 11:** Werte des Dev-Laufs rechnerisch nachgemessen (WCAG-Formel): Chip-Text 10,22–13,01:1,
  Zeichen/Rand gegen Chip 4,86–6,90:1, roter Akzent auf Läuft-Grund 4,94:1, Zeichenfarben gegen Zeilengründe ≥ 5,62:1,
  Zeilentext `#4A494A` auf weiß/`#fff3cd` 8,96/8,09:1, Streifen `#495057` auf `#d9f4ff` 7,14:1 — **bestätigt**. `opacity` an
  `.fa-node-context` vollständig entfernt (Grep `site.css`, `fa-hierarchy-tree.js`, `Views/FaHierarchy`: kein Rest).
- **FA 4a / AK 19:** `int?` + Frühausstieg nach Längen- bzw. `NotFound`-Prüfung, vor jedem Schreibzugriff; Update ändert bei
  `null` nichts (auch nicht DisplayName/Relevanz/Audit). Leer/nicht numerisch/Überlauf binden zu `null` → abgewiesen. Nur
  `WarningMessage`.
- **AK 18:** `git diff d56a340e..HEAD` über `*Migrations*`, `SQL/`, `OseonDueDateCalculator.cs`, `BusinessDayService.cs`,
  `OperationConfigRepository.cs`, `CachedOperationConfigRepository.cs`, `Views/OperationConfigs/`,
  `IDEALAKEWMSService/Services/` → **leer**. Keine Migration.

**Befunde:**

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| M1 | Minor | `docs/TESTSZENARIEN.md` TS-88 deckte die Spec-Szenarien S8 (Blatt → Vater), S9 (Sub-FA eigener AG / AK 16), S11 (Default-AG `01` ohne Symbol) und den Negativfall „Stammdaten leer“ nicht ab. | **Behoben** in QA-Commit `57b762a9`: TS-88.12 (a–d); Index auf „TS-88.1 – 88.12“ nachgezogen. LF geprüft (`git show HEAD:docs/TESTSZENARIEN.md` ohne CR). |
| M2 | Minor | Erledigt-/Läuft-Prädikat steht jetzt zweimal (Terminal `BdeApiController.cs:252-256` und `WorkOperationRepository.BdeStatusQueries`) — Drift-Risiko „eine Regel an mehreren Orten“. | Akzeptiert: Spec verlangt „Terminal bleibt unverändert“; Kommentar am Repository verweist auf die Quelle; Repository-Test sichert IsFinal/Storno/Pausiert. |
| M3 | Minor | W-4-Mehrfach-Liste wird vor dem Werkbank-Gate befüllt — ein Doppel-Eintrag an einer nicht BDE-aktiven Werkbank erzeugt ebenfalls die Log-Warnung. | Akzeptiert: der Log-Hinweis meldet den Datenbefund (Ursache Re-Add-Sperre) unabhängig vom Gate; keine UI-Wirkung. |
| M4 | Minor | `AppVersion.Date` in beiden `AppVersion.cs` bleibt `2026-10-12`, der Changelog nennt 13.10.2026. | Nicht geändert: `AppVersion.Date` wird nirgends gelesen (Grep), die Bündel-Bumps 1.52–1.54 haben es ebenso nicht gepflegt. Beim Bündel-Merge glattziehen. **Nachtrag Dev-Lauf 2026-10-13:** behoben in `627c992a` (beide auf `2026-10-13`). |
| M5 | Minor | FA 4 („Soll TT.MM.JJJJ“ im `aria-label`) ↔ R3 (Soll nur bei überfälligen Varianten). | Akzeptiert: R3 folgt der spezielleren FA-7-Tabelle; der Kombigerät-Bezug ist nur an überfälligen Symbolen prüfbar (TS-88.8 mit Terminen in der Vergangenheit erfüllt das). |
| M6 | Minor | `Add_OhneOffset`/`Update_OhneOffset` prüfen den Rückgabetyp `RedirectToActionResult` nicht (AK 19 nennt ihn). | Akzeptiert: Pfad trivial (`return RedirectToAction(nameof(Index))`), im Diff sichtbar; Manual-UAT TS-87.3. |

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweis | Anmerkung |
|---|---|---|
| 1 | Unit (`Status_Tabelle_AK10`, Repo-Test IsFinal/Storno) + **Manual-UAT** TS-88.1 | `IsReported` wird im Diff nirgends gelesen |
| 2 | Unit (Prüfwert 30.04./Offset 1/01.05. → 04.05.; heute 04./05.05.) | Feiertage aus der DB: Manual-UAT TS-88.3 |
| 3 / 3a | Unit (Status-Tabelle, Repo-Test pausiert/storniert) + **Manual-UAT** TS-88.2/88.3 | Darstellung (Farbe/Zeichen) nur manuell |
| 4 | Unit (Repo-Prädikat Storno) + **Manual-UAT** TS-88.4 | |
| 5 | Unit (`OhneFeTermin_OderOhneKopfzeile…`) | |
| 6 | Unit (Werkbank-Gate) + **Manual-UAT** TS-88.5 | globales Gate im Controller nicht automatisiert |
| 7 | **Manual-UAT** TS-88.10 | Textteil per Unit (`Texte_AriaLabel…`) |
| 8 | **Manual-UAT** TS-88.10 | rechnerisch bestätigt, Messung am Testsystem offen |
| 9 / 13 | Code-Lesung + ToQueryString-Test (OPENJSON, 1 Parameter) + **Manual-UAT** TS-88.11 | Query-Zählung gegen SQL Server nicht automatisiert |
| 10, 10a, 10b | Unit | |
| 10c | Unit (Liste) + **Manual-UAT** TS-88.6 (Anzeige, beide Hinweise zugleich) | |
| 11, 12, 14, 15 | Unit | real zusätzlich TS-88.7/88.12 |
| 16 | **Manual-UAT** TS-88.12 b | hängt an v1.47.4-Daten (Erkennung + Einmal-Skript) |
| 17 | Unit (reine Liste: ein Eintrag je Paar) + **Manual-UAT** TS-88.9 | Logger-Aufruf selbst nicht automatisiert |
| 18 | `git diff` (leer, s. o.) | |
| 19 | Unit (direkter Action-Aufruf) + **Manual-UAT** TS-87.3 | echtes ASP.NET-Model-Binding (leer → `null`) nur manuell |

Grün gezählt werden nur die Unit-/Diff-Beweise; alle als Manual-UAT markierten Teile sind bis zur Abnahme offen.

### 4. Geänderte Bestandstests

Keine. `git diff --numstat` über die Testprojekte: nur Hinzufügungen (40/0, 235/0 neu, 70/0); kein bestehender Test und keine
Fixture verändert.

### 5. CLAUDE.md-Checkliste

- Migration / `SQL/XX` / FreshInstall / AgentJobs: entfällt (kein Schema-Diff, AK 18 belegt). Kein Einmal-Skript.
- Audit-Felder: Teil 2 schreibt nichts; der FA-4a-Abweisepfad schreibt nichts; bestehende Audit-Zuweisungen unverändert.
- Version 1.55.0 in **beiden** `AppVersion.cs` ✔, Anwender-Changelog `Views/Help/Changelog.cshtml` ✔, Hilfeseite
  `Views/Help/Index.cshtml` ✔. Brain-Changelog/feature-map: Sache des Aufrufers (nicht Teil des Worktree-Diffs).
- `docs/TESTSZENARIEN.md`: TS-88.1–88.12 + TS-87.3-Negativfall ✔ (LF); [[testszenarien-index]] Zeile 88 ✔.
- Neue Rolle/neuer Filter: keine.

### Manuelle Test-Checkliste (Testsystem, Mensch)

Vorbedingungen: Bündel v1.55.0 aus dem Worktree publiziert (nur Web); `SQL/95` gelaufen; `BdeAktiv = true` (nur
Testsystem); Seed-Kollisionen bereinigt (Vorbedingung P3); Offsets: `KA` = 1, `SW` ohne Eintrag; Feiertage gepflegt;
Werkbänke `KA`/`SW` BDE-aktiv, `PG` nicht.

1. `/FaHierarchy` öffnen → Seite lädt ohne Fehler; gelber Hinweis „Für 1 Arbeitsgang-Kürzel … SW“ (TS-88.6).
2. Am BDE-Terminal `KA` einer Sub-FA mit Endmeldung beenden → Neuladen: `KA` grün mit Haken, Tab → „KA – <Werkbank> – erledigt“ (TS-88.1). Gegenprobe nur Tracking-Rückmeldung → nicht grün.
3. Buchung auf `SW` starten → blau mit Sanduhr „… – läuft“; pausieren → grau bzw. rot (TS-88.2).
4. Struktur mit FE-Termin in der Vergangenheit: offenes Kürzel rot mit Dreieck „… – überfällig (Soll …)“; laufendes → blau mit rotem Rand, Sanduhr **und** Dreieck (TS-88.3). Struktur ohne FE-Termin → nie rot.
5. Endmeldung stornieren bzw. `IsFinal` per Korrektur zurücknehmen → nach Neuladen nicht mehr grün (TS-88.4).
6. `PG` (Werkbank nicht BDE-aktiv) neutral; `BdeAktiv = false` → alles neutral, kein gelber Hinweis, kein „kein BDE-Arbeitsgang“ (TS-88.5); danach wieder einschalten.
7. `STO`, Kürzel einer nicht materialisierten Sub-FA, Blatt in einer Waisen-Struktur → neutral, Tab-Text „… – kein BDE-Arbeitsgang“ (TS-88.7).
8. Blatt → Vater-AG, Sub-FA → eigener AG, Default-AG `01` ohne Symbol (TS-88.12 a–c).
9. Kombigerät (zwei Kopfzeilen, verschiedene FE-Termine in der Vergangenheit) → Soll im Tab-Text nach dem frühesten; Kopfzeile ohne Einzeltermin (TS-88.8).
10. Per SQL zweite `WorkOperation` `ka` neben `KA` → ein Symbol, zusammengefasster Status; Serverlog genau **eine** Warnung je Aufruf (TS-88.9).
11. Graustufen-Screenshot + Spaltenfilter `ka`: Pfadzeilen kursiv mit grauem Streifen, nicht blass; Kontrast in DevTools in normaler, Hover-, hervorgehobener, Treffer- und Kontextzeile (TS-88.10).
12. Seitengröße „Alle“ mit SQL-Profiler: eine `WorkOperations`-Abfrage + zwei Status-Abfragen je Aufruf (TS-88.11).
13. `/OperationConfigs` als Admin: `required` per DevTools entfernen, Offset leeren, speichern (Zeile `KA` **und** Anlegen `ZZ`) → gelbe Meldung, nichts gespeichert, `KA` bleibt `+3`/alter Name/`ModifiedAt`; `0` und `-1` speichern weiterhin (TS-87.3, S17).
