---
type: spec
title: "FA-Struktur Arbeitsgänge als Symbole — Teil 2: Status-Farben bei aktivem BDE"
slug: 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-07
source_backlog: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]"
anhaenge:
  - "[[sage-fa-struktur-arbeitsschritte.png]]"
depends_on: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs + WorkOperationRepository.cs — (1) EINE gebuendelte Abfrage der WorkOperations aller HauptFAs der Seite (Projektion inkl. SubOrderNumber, OperationNumber, ProductionWorkplace.BdeAktiv; Muster WorkOperationRepository.cs:120-128, Contains -> OPENJSON); (2) EINE gebuendelte Status-Abfrage fuer die aufgeloesten WorkOperation-Ids: erledigt = BdeBookingQuantity.IsFinal auf nicht stornierter Buchung (Antwort 3, Praedikat wie BdeApiController.cs:252-256), laeuft = BdeBooking EndedAt == null && !IsCancelled (Antwort 5, Muster BdeBookingRepository.cs:62)"
  - "IdealAkeWms/Services/OseonDueDateCalculator.cs — NUR wiederverwenden (Antwort 2): Soll-Termin je Arbeitsgang = Calculate(FE_Termin, Offset, IBusinessDayService, Feiertage) (OseonDueDateCalculator.cs:9-13); Feiertage ueber IHolidayRepository.GetHolidayDatesAsync wie OseonGroupViewModelBuilder.cs:35"
  - "Offset-Quelle je IDEAL-Kuerzel — OFFEN (Rueckfrage 8): Variante A IOseonOperationConfigRepository.GetAllAsDictionaryAsync (keine Migration) oder Variante B neues Feld an ProductionWorkplace (Migration)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs — reine, unit-testbare Funktionen: (a) Zuordnung Kuerzel -> WorkOperation (Antwort 6 a, deckungsgleich mit dem Erkennungs-Scope seit v1.47.4); (b) Status-Ableitung kein-BDE-Arbeitsgang/erledigt/laeuft/ueberfaellig/offen bzw. neutral bei BDE aus (Antworten 2-6)"
  - "IdealAkeWms/Controllers/FaHierarchyController.cs — Index (Z.52-133; Teil-1-Variante A gewaehlt): nach der Seiten-Paginierung (Z.86-89) die zwei Abfragen oben + BDE-Gate global (AppSettingKeys.BdeAktiv, IAppSettingRepository ist injiziert Z.29); FE_Termin aus den bereits geladenen OrderInfos (Z.68, structure.OrderInfos FaHierarchyTreeBuilder.cs:104). VERWORFEN (Teil-1-Antwort 2): Status im Modal-Endpunkt WorkOperations (Z.139-155)"
  - "IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml — Statusdarstellung an den Symbolen aus Teil 1 (Farbe + Zeichen + aria-label/title). VERWORFEN (Teil-1-Antwort 2): _FaWorkOperationsModal.cshtml als Ort der Statusdarstellung"
  - "IdealAkeWms/wwwroot/css/site.css — Statusfarben mit WCAG-AA-Kontrast"
  - "IdealAkeWms.Tests — Status-Ableitung (Tabellentest), Zuordnung (Unit), Repository-Abfragen (InMemory), ToQueryString-Test fuer das Contains ueber HauptFAs/Ids"
  - "docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md — TS-Kapitel naechste freie Nummer zum Umsetzungszeitpunkt"
  - "Vorbedingung, NICHT Teil dieser Spec: IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs — Scope-Korrektur v1.47.4 (Antwort 7 W-1) ist im Buendel umgesetzt, siehe [[2026-10-07-v1-47-4-arbeitsgang-erkennung-kind-sub-fa]]"
open_questions:
  - "(Freigabe-Block Nr. 7, Rest) Widersprueche Antwort 6 <-> Erkennungs-Code, W-1 beantwortet: W-2 Zeile mit SubFA != 0 ohne eigene WorkOperation fuer das Kuerzel (nicht materialisiert / bei Erkennung schon fertig) — 'kein BDE-Arbeitsgang' oder Rueckfall auf den Vater-Sub-FA?; W-3 Waisen-Blatt (VaterFA ohne importierten Knoten) — alte WorkOperation des Vater-Auftrags oder 'kein BDE-Arbeitsgang'?; W-4 mehrere WorkOperations mit gleichem Kuerzel am selben Auftrag (kein Unique-Index, Re-Add-Sperre case-sensitiv) — welcher Status gilt?"
  - "(Freigabe-Block Nr. 8, neu 2026-10-07) Offsets je IDEAL-Kuerzel: Wo werden sie gepflegt — A bestehende Tabelle OseonOperationConfigs (Settings/OperationConfig, nur Admin; keine Migration; geteilter Namensraum mit den OSEON-Seed-Eintraegen, z. B. 'EG' Offset 0) oder B neues Feld an der Werkbank (ProductionWorkplace, Kuerzel = ArbeitsschrittCode; Migration)? Und: Offset fuer ein Kuerzel ohne Eintrag = 0 (Soll-Termin = FE_Termin, Muster Teileverfolgung) oder kein Ueberfaellig-Status (Muster OSEON-Reporting)?"
  - "(Freigabe-Block Nr. 9, neu 2026-10-07; Rest aus Nr. 2) Kombigeraete mit mehreren Kopfzeilen (FaHierarchyOrderInfo) je HauptFA und ggf. unterschiedlichem FE_Termin: welcher FE_Termin gilt (z. B. fruehester), oder fuer diese Strukturen kein Ueberfaellig-Status?"
  - "(Freigabe-Block Nr. 10, neu 2026-10-07) Vorrang zwischen 'laeuft' und 'ueberfaellig': Ein Arbeitsgang mit laufender Buchung, dessen Soll-Termin vor heute liegt — 'laeuft' oder 'ueberfaellig'?"
beantwortete_rueckfragen:
  - "6 Zuordnung Kuerzel -> WorkOperation (Teil 1 = Quelle S) -> a) Zeile mit SubFA != 0: eigener Arbeitsgang mit gleicher OperationNumber, sonst der des VaterFA; b) Kuerzel ohne WorkOperation: anzeigen, neutral, ohne Status, Hinweis 'kein BDE-Arbeitsgang' (Text/aria-label, nicht nur Farbe); c) WorkOperation ohne Kuerzel: nicht als Symbol zeigen (Gerald 2026-10-06, Freigabe-Block Nr. 6)"
  - "1 Codierung ohne Farbe -> zusaetzlich ein Zeichen je Status: Haken = erledigt, leerer Kreis = offen, Warndreieck = ueberfaellig, eigenes Zeichen fuer 'laeuft'; Statustext per aria-label (Keyuser 2026-10-07, Freigabe-Block Nr. 1)"
  - "2 Ueberfaellig -> Berechnungslogik mit Offset-Werktagen wie OSEON-Reporting/Teileverfolgung (OseonDueDateCalculator), Bezugstermin vorerst FE_Termin (Keyuser + Nachtrag Gerald 2026-10-07, Freigabe-Block Nr. 2). Offen geblieben und ausgelagert: Offset-Pflege -> Nr. 8, Kombigeraete -> Nr. 9"
  - "3 Erledigt -> BDE-Rueckmeldung IsFinal (Keyuser 2026-10-07, Freigabe-Block Nr. 3)"
  - "4 BDE aktiv -> global UND Werkbank aktiv, sonst neutral (Keyuser 2026-10-07, Freigabe-Block Nr. 4)"
  - "5 Laufende Buchung -> eigener Zustand 'laeuft' (Keyuser 2026-10-07, Freigabe-Block Nr. 5)"
  - "7 W-1 -> Variante b: Kuerzel einer Sub-FA-Zeile gehoert nur zum eigenen Sub-FA; umgesetzt als Bugfix v1.47.4 im Buendel (Keyuser 2026-10-07, Freigabe-Block Nr. 7). W-2 bis W-4 weiter offen"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> **Umsetzungsort (Hinweis, keine Entscheidung):** nach Schranke 2 des IDEAL-Bündels, aus `main`
> (neuer Worktree). Code-Grundlage dieser Spec ist der Bündel-Stand
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.47.1); alle Zeilenangaben beziehen sich darauf und
> sind nach dem Merge gegen `main` nachzuprüfen. TS-Nummer und Version: nächste freie zum
> Umsetzungszeitpunkt. `worktree`/`branch` setzt der Dev-Lauf.
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
> Rework tragen den Zusatz „(v1.47.4)“.
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
  (Teil-1-Antwort 2) darstellen: **erledigt / läuft / überfällig / offen** (Antworten 3, 5, 2) plus
  „kein BDE-Arbeitsgang“ (Antwort 6 b).
- Statuscodierung über Farbe **und** Zeichen **und** Text (Antwort 1).
- Gate „BDE aktiv“: global **und** Werkbank (Antwort 4).
- Soll-Termin je Arbeitsgang = `FE_Termin` + Offset-Werktage über den vorhandenen `OseonDueDateCalculator`
  (Antwort 2).

**Out-of-Scope**
- Kein gespeichertes Soll-Datum je Arbeitsgang (der Soll-Termin wird beim Laden berechnet, Antwort 2).
  Ob für die **Offsets** eine Migration entsteht, entscheidet Rückfrage 8 (Variante B).
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
   zählen **nicht** als „läuft“. Vorrang gegenüber „überfällig“: Rückfrage 10.
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
   - `OseonOperationConfig.DueDateOffsetDays` (`Models/OseonOperationConfig.cs:22-26`, Doku „Offset in
     Arbeitstagen relativ zum Stanztermin“) ist je `OperationName` (OSEON-AG-Kurzname, `:10-14`) gepflegt,
     Seed `Program.cs:465-493` (u. a. `EG` „Entgraten“ Offset 0, `Program.cs:471`), Pflege unter
     `/Settings/OperationConfig` (`SettingsController.cs:110-164`, Klasse `[RequireAdminAccess]` Z.10). Das
     Dictionary ist **case-sensitiv** (`OseonOperationConfigRepository.cs:46-50`, `ToDictionaryAsync` ohne
     Comparer); die „noch nicht konfiguriert“-Liste liest nur `OseonWorkOperations` (`:55-67`) und kennt
     IDEAL-Kürzel nicht. **Wo die Offsets je IDEAL-Kürzel gepflegt werden und was ohne Eintrag gilt, ist offen
     — Rückfrage 8.**
   - **`FE_Termin` je Knoten/HauptFA — belegt:** `FE_Termin` liegt nur an `FaHierarchyOrderInfo`
     (`Models/FaHierarchyOrderInfo.cs:32`), Schlüssel `HauptFA` (`:17`); `FaHierarchyNode` hat kein Datumsfeld
     außer `SyncedAt` (`FaHierarchyNode.cs:62`). Im `Index` sind **alle** OrderInfos bereits geladen
     (`FaHierarchyController.cs:68`) und hängen je Struktur an `structure.OrderInfos`
     (`FaHierarchyTreeBuilder.cs:104`) — **keine zusätzliche Abfrage**. Jeder Knoten erbt den Termin seiner
     HauptFA; Sub-FA-eigene Termine gibt es nicht. Bei genau einer Kopfzeile ist der Termin eindeutig (Muster
     `ProductionOrdersController.cs:294-305`); bei mehreren Kopfzeilen (Kombigerät, `structure.IsAmbiguous`,
     `Index.cshtml:149-151`) ist er es nicht → **Rückfrage 9**. Ohne Kopfzeile oder mit `FE_Termin = null`:
     nie überfällig.
   - ~~Kandidat `Neuer_PT_PPS`~~ — verworfen (Nachtrag Gerald 2026-10-07: „vorerst `FE_Termin`“).
   - (Ursprünglicher Text: „der in Rückfrage 2 festgelegte Auftragstermin liegt vor heute“ — überholt.)
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
     Termin (`Index.cshtml:149-151`).
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
7. **Leitplanken und Codierung (Antwort 1, Keyuser 2026-10-07):** Farbe ist nie alleiniger
   Bedeutungsträger. Je Status ein **Zeichen** zusätzlich zur Farbe, Statustext per `aria-label` (zusätzlich
   `title` für Maus; `title` allein reicht nicht, Touch):

   | Status | Farbe | Zeichen | Text (`aria-label`, Kürzel vorangestellt) |
   |---|---|---|---|
   | erledigt | grün | Haken | „KA – erledigt“ |
   | läuft | eigene Farbe (Vorschlag: blau/info) | eigenes Zeichen, formverschieden von Dreieck und Kreis (Vorschlag: Sanduhr bzw. Play) | „KA – läuft“ |
   | überfällig | rot | Warndreieck | „KA – überfällig (Soll TT.MM.JJJJ)“ |
   | offen | grau | leerer Kreis | „KA – offen“ |
   | kein BDE-Arbeitsgang | neutral wie Teil 1 | keins | „KA – kein BDE-Arbeitsgang“ |

   Farbe und Zeichen für „läuft“ legt der Dev-Lauf mit dem Skill `frontend-design` fest (Antwort 1 sagt nur
   „eigenes Zeichen“). Zeichen als Inline-SVG wie die bestehenden Typ-Icons (`_FaHierarchyNode.cshtml:27-53`),
   keine neue Icon-Bibliothek. Kontrast WCAG AA (4,5:1 Text, 3:1 Konturen/Zeichen), auch bei Rot/Grün-Sehschwäche
   unterscheidbar (Graustufen-Test).
7a. **Vorrang der Zustände:** kein BDE-Arbeitsgang (ohne `WorkOperation`) → erledigt → {läuft, überfällig}
   (Reihenfolge zwischen beiden: Rückfrage 10) → offen.
8. **Status braucht eine `WorkOperation` — Teil 1 zeigt Quelle S (Teil-1-Antwort 1).** Jede Statusquelle
   oben hängt an einer `WorkOperation`: `BdeBookingQuantity.IsFinal` über `BdeBooking.WorkOperationId`
   (`BdeApiController.cs:252-256`), `IsReported` an der `WorkOperation` selbst. Ein Sage-Kürzel aus
   `FaHierarchyNode.Arbeitsschritte` hat keinen Status.
   - ~~*Quelle W:* Status direkt je angezeigter `WorkOperation`, keine Zuordnung nötig.~~ — verworfen
     (Teil-1-Antwort 1, 2026-10-06).
   - *Quelle S (gilt):* Zuordnung Kürzel → `WorkOperation` nötig. Belegter Schlüssel: `WorkOperation` →
     `ProductionOrder` (FK `ProductionOrderId`, `ApplicationDbContext.cs:807-810`), Knoten ↔ Auftrag über
     `OrderNumber == HauptFA` und `SubOrderNumber == SubFA` (`FaHierarchyTreeViewModel.cs:171-176`), Kürzel
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
     HauptFA` (Muster `FaHierarchyTreeViewModel.cs:171-176`), `OperationNumber` case-insensitiv wie die
     Erkennung (`WorkOperationStructureDetectionService.cs:80`, `:106`, `:126`). Den Status liefert diese
     `WorkOperation` (Signal laut Rückfrage 3).
   - **b) Kürzel ohne `WorkOperation`** (Ausschlussliste, unbekannt, mehrdeutig, Auftrag bei Erkennung schon
     fertig, kein Vater): Symbol bleibt sichtbar, **ohne Status**, neutral wie Teil 1, mit dem Hinweistext
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

> [!warning] WIDERSPRUCH W-2 — „materialisierter Sub-FA (SubFA ≠ 0)“ setzt zwei verschiedene Dinge gleich
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

> [!warning] WIDERSPRUCH W-3 — Waisen-Blatt (Randfall)
> Zeigt `VaterFA` eines Blatts auf keinen importierten Knoten (Waisen-Pseudowurzel,
> `FaHierarchyTreeViewModel.cs:53-57`), war das Blatt-Kürzel nie im Scope einer Erkennung (kein Knoten mit
> `SubFA == VaterFA` im Loop, `:96-102`). Ein früher materialisierter `ProductionOrder` mit
> `SubOrderNumber == VaterFA` und seine alten Arbeitsgänge können aber noch existieren (Erkennung löscht nie,
> `:149-163`). Regel a („sonst VaterFA“) würde dann einen Status zeigen, der nicht aus diesem Kürzel stammt.
> Nicht aufgelöst — Rückfrage 7.

> [!warning] WIDERSPRUCH W-4 — Antwort 6 a spricht von „ihrem eigenen Arbeitsgang“ (Einzahl)
> Der Code garantiert die Einzahl nicht (siehe (iv)). Für mehrere Treffer je Auftrag + Kürzel fehlt eine
> Regel (z. B. erledigt, wenn einer/alle erledigt). Nicht aufgelöst — Rückfrage 7. Umfang: P3 a.

## Technischer Loesungsentwurf

Ort: **Baumzeilen** (Teil-1-Antwort 2 = Variante A). Ablauf im `FaHierarchyController.Index`, **nach** der
Paginierung (`FaHierarchyController.cs:86-89`), nur für die Strukturen der Seite:

1. Globales Gate `AppSettingKeys.BdeAktiv` lesen (`IAppSettingRepository`, Z.29). Aus → keine weiteren
   Abfragen, Symbole wie Teil 1.
2. **Eine** gebündelte `WorkOperation`-Abfrage über die HauptFA-Nummern der Seite (Projektion `Id`,
   `ProductionOrder.OrderNumber`, `ProductionOrder.SubOrderNumber`, `OperationNumber`,
   `ProductionWorkplace.BdeAktiv`; Muster `WorkOperationRepository.cs:120-128`, `Contains` → EF 10 `OPENJSON`,
   [[fallstricke]] §8, ToQueryString-Test). Sie ersetzt den Existenz-Check `GetOrderNumbersWithWorkOperationsAsync`
   (`FaHierarchyController.cs:91-95`) **nicht** — der bleibt für den Modal-Knopf; Zusammenlegen ist erlaubt,
   wenn `HasWorkOperations` identisch bleibt.
3. Je Knotenzeile + Kürzel die Zuordnungsfunktion (FA 9, Antwort 6 a) → `WorkOperation`-Id oder „kein
   BDE-Arbeitsgang“.
4. **Eine** gebündelte Status-Abfrage über die aufgelösten Ids: erledigt (Prädikat identisch zum Terminal,
   `BdeApiController.cs:252-256`: `q.IsFinal && !q.BdeBooking.IsCancelled`) und läuft (`EndedAt == null &&
   !IsCancelled`, Muster `BdeBookingRepository.cs:62`) — als **eine** Repository-Methode, die beide Mengen
   liefert (zwei SQL-Statements in einer Methode sind zulässig; AK 9 zählt Abfragen je Zweck, siehe dort).
   Das Terminal bleibt unverändert (kein Refactoring außerhalb des Umfangs).
5. Feiertage einmal je Seitenaufbau (`IHolidayRepository.GetHolidayDatesAsync`, Muster
   `OseonGroupViewModelBuilder.cs:35`); Offsets einmal je Seitenaufbau (Quelle laut Rückfrage 8; bei Variante A
   `IOseonOperationConfigRepository.GetAllAsDictionaryAsync`, gecacht über `CachedOseonOperationConfigRepository`,
   Lookup **case-insensitiv** nachbauen, weil das Dictionary es nicht ist).
6. Soll-Termin je Arbeitsgang: `OseonDueDateCalculator.Calculate(FE_Termin, offset, _businessDays, holidays)`
   (wiederverwenden, nicht kopieren); `FE_Termin` aus `structure.OrderInfos` (ohne Abfrage, siehe FA 4);
   Kombigerät laut Rückfrage 9.
7. Statusableitung als reine Funktion (Eingaben: hat `WorkOperation`?, Werkbank BDE-aktiv?, erledigt?,
   läuft?, Soll-Termin?, heute) → Tabellentest; Vorrang laut FA 7a.

- ~~Variante B: Termin im Modal-Endpunkt nachladen (`FaHierarchyController.cs:148`), `GetByOrderNumberWithWorkplaceAsync`
  (`:152`)~~ — verworfen (Teil-1-Antwort 2: Baum).
- Offene Punkte der Zuordnungsfunktion bis zur Antwort auf Rückfrage 7: Fallback bei fehlender eigener
  `WorkOperation` (W-2), Waisen-Blatt (W-3), Mehrfach-Treffer (W-4). ~~W-1 betrifft, ob zusätzliche Symbole
  oder ein Hinweis für Vater-Arbeitsgänge nötig sind.~~ — erledigt (Antwort 7, v1.47.4).
- `WorkOperation`s ohne Symbol (Antwort 6 c) werden nicht dargestellt; die Warnung `UnmatchedOperations` im
  Modal bleibt unberührt.
- **frontend-design-Skill ist im Dev-Lauf Pflicht** (Views/CSS). Bootstrap-5-Farben/Icons der Anwendung,
  keine neue Icon-Bibliothek ohne ADR.

## Migrations-/SQL-Auswirkungen

Status und Soll-Termin werden abgeleitet; `FE_Termin` wird bereits importiert (`FaHierarchySql.cs:140-141`,
Antwort 2). `SQL/AgentJobs/` unberührt.

**Abhängig von Rückfrage 8 (Offset-Pflege):**
- Variante A (`OseonOperationConfigs` wiederverwenden): **keine** Migration, `SQL/00_FreshInstall.sql`
  unverändert. Die IDEAL-Offsets sind Daten (Pflege über `/Settings/OperationConfig`), kein Skript.
- Variante B (neues Feld an `ProductionWorkplace`, z. B. `DueDateOffsetDays int NOT NULL DEFAULT 0`):
  Migration nach ADR 0004 — Model → `dotnet ef migrations add` → `SQL/XX_*.sql` mit Spalten-Guard
  (`COL_LENGTH('dbo.ProductionWorkplaces','DueDateOffsetDays') IS NULL`, DDL in eigenem Batch) →
  `__EFMigrationsHistory`-Insert in separatem Batch → `SQL/00_FreshInstall.sql` an **beiden** Stellen. Nicht
  daten-destruktiv. Zusätzlich prüfen, dass der Werkbank-Sync (`ProductionWorkplaceSyncService`) das Feld nicht
  überschreibt.

## Audit-Feld-Auswirkungen

Keine — reine Lesedarstellung. (Bei Rückfrage 8 Variante A: `OseonOperationConfig` ist keine
`AuditableEntity` (`Models/OseonOperationConfig.cs:5`) und bekommt durch diese Spec keine Audit-Felder; bei
Variante B werden `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` der Werkbank beim Speichern wie bisher gesetzt.)

## Rollen / Zugriffsfilter

Unverändert `[RequirePickingOrTrackingOrLeitstandAccess]` (`FaHierarchyController.cs:23`). Die Ansicht
zeigt BDE-Fortschritt jetzt auch Rollen ohne BDE-Rolle (`bde_user`/`bde_shiftlead`/`bde_admin`,
`RoleKeys.cs:14-16`) — es ist nur ein Status je Arbeitsgang, keine Bediener- oder Zeitdaten. Antwort 4
schränkt das nicht ein (nur Schalter-Logik) → bleibt so. Offset-Pflege: Variante A unter `/Settings` nur Admin
(`SettingsController.cs:10`); Variante B im Werkbank-Stamm mit dessen bestehendem Edit-Filter.

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
   läuft. Vorrang gegenüber überfällig laut Rückfrage 10.
3a. Alle übrigen erscheinen als **offen** (grau + leerer Kreis + Text).
4. Storno der abschließenden Buchung oder Korrektur `IsFinal → false` lässt das Symbol beim nächsten Laden
   wieder offen/überfällig werden (kein gespeicherter Status).
5. Ohne `FE_Termin` (`null`) oder ohne Kopfzeile ist ein Arbeitsgang nie überfällig.
6. BDE global aus **oder** Werkbank des Arbeitsgangs nicht BDE-aktiv: Symbol wie Teil 1, ohne Statusfarbe,
   ohne Statuszeichen. Gemischte Struktur (eine Werkbank aktiv, eine nicht) → nur die aktiven tragen Status.
7. Jeder Status ist ohne Farbwahrnehmung erkennbar (Graustufen-Screenshot: Haken/Kreis/Dreieck/Läuft-Zeichen
   unterscheidbar) und per Screenreader angesagt (`aria-label`).
8. Kontrast aller Statusvarianten ≥ WCAG AA, gemessen am Testsystem.
9. Je Seitenaufbau bei BDE an: genau **eine** zusätzliche `WorkOperation`-Abfrage und **eine**
   Status-Repository-Methode (höchstens zwei SQL-Statements: erledigt, läuft), plus je **eine** Abfrage für
   Feiertage und Offsets (gecacht zählt nicht) — kein N+1, unabhängig von der Seitengröße (Query-Zählung im
   Dev-Lauf). Bei BDE global aus: **keine** dieser Abfragen.
10. Status-Ableitung durch Tabellentest abgedeckt (kein BDE-Arbeitsgang vor allem; erledigt vor läuft/überfällig;
    Soll-Termin heute = nicht überfällig; `FE_Termin` `null`; Werkbank nicht BDE-aktiv → neutral; Vorrang
    läuft/überfällig laut Rückfrage 10; Kombigerät-Regel laut Rückfrage 9).
10a. Offset-Auflösung laut Rückfrage 8 per Unit-Test: Kürzel mit Eintrag → dessen Offset (case-insensitiv:
    `ka` trifft Eintrag `KA`); Kürzel ohne Eintrag → Verhalten laut Rückfrage 8.

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
15. Verhalten in den Fällen W-2 bis W-4 (FA 9) gemäß Antwort auf Rückfrage 7 — **vor** dem Dev-Lauf
    festzulegen; bis dahin nicht abnahmefähig. (W-1 erledigt durch v1.47.4, siehe AK 16.)
16. Nach v1.47.4 (Vorbedingung): Sub-FA `S` (Kürzel `VM`) unter Sub-FA `P` ohne eigenes `VM` → an Zeile `S`
    Symbol `VM` mit dem Status der `VM`-`WorkOperation` von `S`; an Zeile `P` kein `VM`-Symbol, und am Auftrag `P`
    existiert keine (ungebuchte) `VM`-`WorkOperation` mehr (Antwort 7 W-1).

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Nummer: nächste freie zum Umsetzungszeitpunkt), Index nachziehen.

- **Vorbedingungen:** IDEAL-Testsystem mit Bündel ≥ v1.47.4 (Einmal-Skript ausgeführt), BDE global aktiv,
  Werkbänke der Test-AGs `BdeAktiv`, Struktur mit ≥ 3 erkannten Arbeitsgängen, eine Struktur mit `FE_Termin`
  in der Vergangenheit und eine mit `FE_Termin` in der Zukunft, Offsets laut Rückfrage 8 gepflegt (mindestens
  ein Kürzel mit Offset ≠ 0), Feiertage im WMS gepflegt (Prüfschritt P4).
- **S1 Erledigt:** Am BDE-Terminal einen AG starten und mit Abschluss beenden → Symbol grün + Haken,
  Screenreader „… – erledigt“. Gegenprobe: im Tracking nur rückmelden (ohne BDE-Abschluss) → **nicht** grün.
- **S2 Offen:** AG ohne Buchung, Soll-Termin in der Zukunft → grau + leerer Kreis.
- **S3 Überfällig:** AG ohne Abschluss, Soll-Termin (FE_Termin + Offset) gestern → rot + Warndreieck. Offset-
  Probe: Kürzel mit Offset +3, `FE_Termin` = vor 2 Werktagen → noch **nicht** überfällig; vor 4 Werktagen →
  überfällig. Wochenende/Feiertag zwischen `FE_Termin` und heute wird nicht mitgezählt.
- **S4 Storno/Korrektur:** Abschlussbuchung im BDE-Buchungs-Editor auf „nicht final“ setzen oder stornieren
  → Symbol nach Neuladen nicht mehr grün.
- **S5 Läuft:** AG am Terminal gestartet, nicht beendet → eigene Farbe + Läuft-Zeichen, „… – läuft“. Pausieren
  → nach Neuladen offen bzw. überfällig. Läuft **und** Soll-Termin überschritten → laut Rückfrage 10.
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
- **Negativ:** Kombigerät mit abweichenden Terminen → Verhalten laut Rückfrage 9; Struktur ohne Kopfdaten
  (`FaHierarchyOrderInfo` fehlt) → nie überfällig, kein Fehler; Kürzel ohne Offset-Eintrag → laut Rückfrage 8,
  kein Fehler; Seitengröße „Alle“ → Abfragezahl wie AK 9 (kein N+1).

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

**P4 — Feiertage und Offset-Tabelle auf der IDEAL-WMS-Datenbank (neu 2026-10-07, vor Rückfrage 8 / Dev-Lauf).**
Der Soll-Termin zählt Werktage ohne Feiertage aus der WMS-Tabelle; ob sie bei IDEAL gepflegt ist, kann der
Spec-Lauf nicht sehen. Ebenso, welche Einträge `OseonOperationConfigs` dort hat (Seed `Program.cs:465-493` läuft
beim Web-Start, wenn die Tabelle leer ist):

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

## Etappen (nur bei epic: true)

Entfällt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Vorläufig — der QA-Lauf bestätigt gegen den echten Diff.**

- **Web-App:** ja (Repository, ViewModel, Controller `FaHierarchy.Index`, `_FaHierarchyNode`, CSS).
- **Service:** nein (die Erkennungs-Korrektur v1.47.4 kommt mit dem Bündel, nicht mit dieser Spec).
- **Migration:** nein — **außer** Rückfrage 8 entscheidet Variante B (dann `deploy.migration: true`, Skript
  `SQL/XX_*.sql` vor dem Web-Deploy, FreshInstall an zwei Stellen).
- **Vor Abnahme:** Offsets laut Rückfrage 8 pflegen, Feiertage prüfen (P4).
- **Publish-Befehle:**

```bash
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

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
7. **Widersprüche Antwort 6 ↔ Erkennungs-Code** (neu, Rework 2026-10-06; Befund und Belege „Fachliche
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
8. **Offsets je IDEAL-Kürzel — wo gepflegt, und was ohne Eintrag?** (neu 2026-10-07; Antwort 2 nennt es
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
9. **Kombigeräte — welcher `FE_Termin`?** (neu 2026-10-07; unbeantworteter Rest aus Nr. 2) Mehrere
   `FaHierarchyOrderInfo` je HauptFA (P2). Frühester `FE_Termin`, spätester, oder für diese Strukturen kein
   Überfällig-Status? Die FA-Liste zeigt bei Kombigeräten bewusst keinen Einzeltermin
   (`ProductionOrdersController.cs:314-329`). Die Spec wählt nicht.
10. **Vorrang „läuft“ vs. „überfällig“** (neu 2026-10-07; aus Antworten 2 und 5) Ein Arbeitsgang mit laufender
    Buchung, dessen Soll-Termin vor heute liegt: „läuft“ (zeigt, dass gearbeitet wird) oder „überfällig“ (zeigt
    die Terminlage)? Die Spec wählt nicht.

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
7. Widersprüche Antwort 6 ↔ Code — W-1 Vater-AG eines Sub-FA-Kind-Kürzels sichtbar machen?; W-2 Rückfall auf den Vater, wenn die eigene WorkOperation fehlt?; W-3 Waisen-Blatt: alte Vater-WorkOperation oder „kein BDE-Arbeitsgang“?; W-4 Mehrfach-Treffer: welcher Status gilt? → **W-1: Variante b (Keyuser 2026-10-07) – ein Kürzel auf einer Sub-FA-Zeile gehört nur zum eigenen Sub-FA, nicht zusätzlich zum Vater (Beispiel `VM` Mittelholmblende nicht am Korpus). Kürzel der Teile ohne eigenen FA (z. B. `LS KA`) bleiben am Vater-Sub-FA rückmeldbar. Folge: Korrektur der Erkennung im Bündel (`WorkOperationStructureDetectionService.cs:100-102`), eigener Bugfix vor dem Bündel-Merge. W-2 bis W-4: noch offen (IT).**
8. Offsets je IDEAL-Kürzel — A (OseonOperationConfigs wiederverwenden) oder B (Feld an der Werkbank, Migration); Kürzel ohne Eintrag: Offset 0 oder kein Überfällig-Status? →
9. Kombigeräte (mehrere Kopfzeilen je HauptFA) — frühester FE_Termin, spätester, oder kein Überfällig-Status? →
10. Vorrang bei laufender Buchung und überschrittenem Soll-Termin — „läuft“ oder „überfällig“? →
