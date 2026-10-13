---
type: spec
title: "FA-Struktur Arbeitsgänge als Symbole — Teil 3: Offene Aufträge mit Arbeitsgang-Status am BDE-Terminal"
slug: 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec
status: Testbereit
created: 2026-10-06
updated: 2026-10-13
source_backlog: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]"
# ueberholt (2026-10-13): Der zweite Eintrag zeigte bis 2026-10-12 auf die Backlog-Notiz
# 2026-10-09-arbeitsgang-stammdaten-vereinheitlichen, weil die Spec noch nicht existierte. Seit 2026-10-13 auf die
# Spec umgestellt (Testbereit, Buendel v1.54.0).
depends_on:
  - "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]]"
  - "[[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]"
task: "[[2026-10-13-fa-struktur-arbeitsgaenge-symbole-teil-3-umsetzung]]"
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Controllers/BdeApiController.cs — GetAvailableOperations (Z.193-286, v1.49.0 geprueft), Normalmodus (Z.244-285): Auswahlmenge wie heute (Z.245-273) MIT einer Aenderung (Antwort 6 = A): activeWoIds (Z.246-249) werden in Z.273 nicht mehr ausgeschlossen, sondern als 'laeuft' markiert; myActiveWoIdSet (Z.260-270) und finishedWoIdSet (Z.252-257) bleiben ausgeschlossen. Antwort zusaetzlich je Arbeitsgang: Auftrag (ProductionOrderId, OrderNumber, SubOrderNumber, Description1), Fertigungstermin, OperationNumber, Name, Status offen/laeuft/laeuft+ueberfaellig/ueberfaellig + Soll-Termin; Sortierung nach Fertigungstermin. NurFA-Zweig (Z.205-241) UNVERAENDERT (Antwort 8). Ctor (Z.26-29) bekommt IFaHierarchyOrderInfoRepository, IHolidayRepository, IBusinessDayService und den Offset-Lookup der Vorbedingung = IOperationConfigRepository (v1.54.0). Alle genannten Zeilen 2026-10-13 gegen v1.54.0 nachgemessen: unveraendert. Folge des Ctor-Umbaus: IdealAkeWms.Tests/Controllers/BdeApiControllerTests.cs konstruiert den Controller 9x direkt (Z.278, 319, 357, 388, 418, 482, 530, 569, 594) -> mitziehen (nur Typ/Compile, Fixtures unveraendert)"
  - "IdealAkeWms/Services/BdeBookingService.cs — NUR Beleg, keine Aenderung: Startpruefung bei Start auf einen laufenden Arbeitsgang (StartPlannedAsync Z.36-64: Ruesten immer exklusiv, Produktion nur exklusiv bei BdeMehrfachBuchungProArbeitsgang = false, Kollision Z.56-63); Anzeige der Kollision am Terminal bde-terminal.js:360-363 + Views/BdeTerminal/Index.cshtml:114-117 (collisionModal)"
  - "IdealAkeWms/Data/Repositories/IFaHierarchyOrderInfoRepository.cs — nur nutzen: GetByHauptFaKeysAsync (Z.25) fuer FE_Termin der HauptFAs (eine Abfrage; bei AKE leer)"
  - "IdealAkeWms/Services/OseonDueDateCalculator.cs (Z.9-13, v1.54.0 unveraendert, Name bleibt) + IHolidayRepository.GetHolidayDatesAsync (IHolidayRepository.cs:8, gecacht CachedHolidayRepository, Program.cs:90) + Offset-Lookup der Vorbedingung [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] = IOperationConfigRepository.GetDueDateOffsetsAsync() (OperationConfigRepository.cs:23, :67-77; Decorator CachedOperationConfigRepository.cs:41-49; Lookup mit OperationNumber.Trim()) (Antwort 7: gleiche Offset-Regeln an beiden Standorten, ohne Eintrag 0) — nur nutzen, Soll-Termin je Arbeitsgang; Schnittstelle wie Teil 2 FA 4. VERWORFEN: 'Offset-Quelle aus Teil 2 (Teil-2-Rueckfrage 8)'"
  - "Status-Ableitung aus Teil 2 (reine Funktion) — wiederverwenden, nicht duplizieren; liegt sie nach Teil 2 im FaHierarchy-ViewModel, in eine gemeinsame statische Klasse unter IdealAkeWms/Services/ ziehen"
  - "Neue reine Funktion (z. B. IdealAkeWms/Services/BdeOrderGrouping.cs oder im Controller als static): Gruppierung je ProductionOrder + Sortierung + Fertigungstermin-Regel (Kombigeraet fruehester FE_Termin, AKE ProductionDate), unit-testbar"
  - "IdealAkeWms/wwwroot/js/bde-terminal.js — renderOperationButtons (Z.442-478, v1.49.0 geprueft): Normalmodus je Auftrag ein Block mit Kopf (Auftrag, Bezeichnung, Fertigungstermin) und den AG-Knoepfen mit Status-Zeichen; Werte per textContent/Escaping einsetzen (heute unescaped, Z.455); NurFA-Zweig (Z.452-453) bleibt unveraendert (Antwort 8); bindOperationButtonHandlers (Z.480-513) bleibt fuer data-wo-id"
  - "IdealAkeWms/Views/BdeTerminal/Index.cshtml — Karte '2. Arbeitsgang waehlen' (Z.60-75): Struktur bleibt, ggf. Ueberschrift/Hinweistext; Scan-Zeile (Z.65-73) unveraendert"
  - "IdealAkeWms/wwwroot/css/bde.css — Auftragsblock und Status-Stile (WCAG AA, Touch-Groesse wie btn-lg)"
  - "IdealAkeWms.Tests — Gruppierung/Sortierung/Termin-Regel (Unit), GetAvailableOperations (InMemory: Gruppen, Sortierung, Status, Ausschluesse unveraendert)"
  - "docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md — TS-Kapitel naechste freie Nummer zum Umsetzungszeitpunkt (Stand 2026-10-13: hoechstes TS-87; Teil 2 belegt voraussichtlich TS-88 -> Teil 3 voraussichtlich TS-89)"
beantwortete_rueckfragen:
  - "1 Ansicht -> am BDE-Terminal je Werkbank die offenen Auftraege, Anknuepfung Schritt 'Arbeitsgang waehlen' (Views/BdeTerminal/Index.cshtml:61) mit der dortigen Logik (Keyuser 2026-10-07, Freigabe-Block Nr. 1)"
  - "2 Rolle -> bestehende BDE-Rollen, keine neue Rolle (Gerald 2026-10-07, Freigabe-Block Nr. 2)"
  - "3 Auftragsbestand -> alle Auftraege mit mindestens einem offenen Arbeitsgang an dieser Werkbank, sortiert nach naechstem Fertigungstermin (naechster zuerst), kein Zeitfenster (Gerald 2026-10-07, Freigabe-Block Nr. 3)"
  - "4 Status -> farbig mit dem Status aus Teil 2 (ueberfaellig/offen/laeuft, mit Zeichen zusaetzlich zur Farbe) (Gerald 2026-10-07, Freigabe-Block Nr. 4) -> depends_on Teil 2"
  - "5 Standort -> nicht standortspezifisch, AKE und IDEAL (Gerald 2026-10-07, Freigabe-Block Nr. 5)"
  - "6 Laeuft am Terminal -> A: Arbeitsgaenge, die ein anderer Werker bucht, sichtbar mit 'laeuft'; Start prueft der Service wie heute (Ruesten exklusiv, Produktion nur bei BdeMehrfachBuchungProArbeitsgang); eigene laufende Buchungen bleiben ausgeblendet (Gerald 2026-10-09, Freigabe-Block Nr. 6)"
  - "7 AKE -> ProductionOrder.ProductionDate ist Fertigungstermin fuer Sortierung und Bezugstermin fuer 'ueberfaellig'; Offset aus den vereinheitlichten Arbeitsgang-Stammdaten, ohne Eintrag 0 (Gerald 2026-10-09, Freigabe-Block Nr. 7)"
  - "8 NurFA-Modus -> unveraendert: keine Termin-Regel, kein Status (Gerald 2026-10-09, Freigabe-Block Nr. 8)"
  - "9 IDEAL ohne FE_Termin -> Rueckfall auf ProductionOrder.ProductionDate wie FA-Liste; kein Offset-Hinweis am Terminal (Gerald 2026-10-12, Freigabe-Block Nr. 9)"
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "Freigegeben wie Antworten 1-9: BDE-Terminal je Werkbank, fremd gebuchte AGs sichtbar als laeuft, AKE ProductionDate als Bezugstermin, IDEAL ohne FE_Termin Rueckfall ProductionDate, NurFA unveraendert. Umsetzung direkt im Buendel NACH Teil 2."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-10-13
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> [!note] Kein Platzhalter mehr (Rework 2026-10-07)
> Die Rückfragen 1-5 sind beantwortet (Keyuser/Gerald 2026-10-07); dieser Teil ist jetzt ausspezifiziert.
> Ursprünglicher Hinweis: „Platzhalter — erst spezifizieren, wenn die Rückfragen beantwortet sind“ (überholt).
> Rework 2026-10-12: Auch die Rückfragen 6-8 sind beantwortet (Gerald 2026-10-09) und eingearbeitet; aus dem
> Code-Abgleich ist eine neue Rückfrage 9 entstanden (IDEAL-Termin ohne `FE_Termin`).

> **Umsetzungsort (Entscheidung Gerald 2026-10-09):** direkt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`
> auf Zweig `feature/2026-08-07-ideal-teile-1-5` (kein eigener Worktree, ein Lauf zur Zeit, kein Warten auf
> Schranke 2 des Bündels), **nach** Teil 2. `worktree`/`branch` im Frontmatter bleiben leer — der Ort steht hier
> fest. Code-Grundlage ist der Bündel-Stand; Zeilenangaben ohne Zusatz stammen aus v1.47.4, die im Rework
> 2026-10-12 angefassten (`BdeApiController.cs`, `BdeBookingService.cs`, `bde-terminal.js`,
> `Views/BdeTerminal/Index.cshtml`, `ProductionOrdersController.cs`, `BdeBookingRepository.cs`,
> `WorkOperationRepository.cs`) sind gegen **v1.49.0** geprüft und unverändert. Version und TS-Nummer: nächste
> freie **im Bündel** zum Umsetzungszeitpunkt (Stand 2026-10-12: v1.49.0, höchstes Kapitel TS-82; Teil 1,
> Vorbedingung und Teil 2 liegen davor). (Vormals: „… nach dem Merge gegen `main` nachzuprüfen …
> `worktree`/`branch` setzt der Dev-Lauf“ — überholt.)
> **Nachmessung 2026-10-13 (überholt den Stand 2026-10-12):** Bündel-Worktree HEAD `d56a340e`, **v1.54.0**
> (`IdealAkeWms/AppVersion.cs:5`); höchstes Kapitel **TS-87** (`docs/TESTSZENARIEN.md:9628`), höchstes Skript
> **`SQL/95_RenameOperationConfigsAndAddAudit.sql`**, letzte Migration **`20261012140548_RenameOperationConfigsAndAddAudit`**;
> Hauptcheckout (`main`) höchstes Skript `SQL/88_*`. Teil 1 (v1.52.0) und die Vorbedingung (v1.54.0) sind umgesetzt
> (Testbereit), Teil 2 noch nicht. Die im Rework 2026-10-12 gegen v1.49.0 geprüften Zeilen (`BdeApiController.cs`,
> `BdeBookingService.cs`, `bde-terminal.js`, `Views/BdeTerminal/Index.cshtml`, `WorkOperationRepository.cs`,
> `BdeBookingRepository.cs`, `ProductionOrdersController.cs`) sind bei v1.54.0 **unverändert** (nachgemessen per
> Bezeichner; Abschnitt „Abgleich mit der Umsetzung (2026-10-13)“).
>
> **depends_on Teil 2** (Antwort 4: „farbig mit dem Status aus Teil 2“): Teil 3 nutzt die Status-Ableitung,
> die Soll-Termin-Berechnung und die Status-Codierung (Farbe + Zeichen + `aria-label`) aus
> [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]]; Teil 2 setzt Teil 1 voraus. ~~depends_on
> Teil 1, Teil 2 nicht als Abhängigkeit~~ — überholt (Antwort 4). ~~Die offenen Teil-2-Rückfragen 8 (Offsets) und 9
> (Kombigeräte) wirken hier mit~~ — beide beantwortet (2026-10-09): Offsets aus den Arbeitsgang-Stammdaten,
> Kombigerät = frühester `FE_Termin`; beide Regeln gelten auch am Terminal.
>
> **depends_on Vorbedingung Arbeitsgang-Stammdaten** (Antwort 7, 2026-10-09): Offsets an beiden Standorten aus
> [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] (aus der Backlog-Notiz
> [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen]]). ~~Spec existierte am 2026-10-12 noch nicht; `depends_on`
> zeigt auf die Backlog-Notiz und ist auf die Spec umzustellen, sobald sie existiert.~~ — überholt (2026-10-13): Spec
> umgesetzt (v1.54.0, Testbereit), `depends_on` umgestellt. Über Teil 2 ist sie ohnehin Vorbedingung; der direkte
> Eintrag macht die AKE-Nutzung sichtbar. Die serverseitige Offset-Prüfung aus
> [[2026-10-12-operationconfig-offset-leer-pruefen]] erledigt Teil 2 (dort FA 4a) — sie wirkt auch für Teil 3.

## Ziel / Nutzen (das Warum)

Am BDE-Terminal sieht der Werker heute je Werkbank nur eine flache Knopfreihe „Auftrag / Kürzel — Name“ in
Auftragsnummern-Reihenfolge. Künftig soll er **je Auftrag** sehen, welche Arbeitsgänge an seiner Werkbank
offen sind, **welcher Auftrag als nächster fällig ist** (Sortierung nach Fertigungstermin) und **welcher
Arbeitsgang überfällig ist oder gerade läuft** (Status aus Teil 2) — ohne Zeitfenster, angescannt wird ohnehin
(Antworten 1-4, [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]). Gilt an **beiden** Standorten (Antwort 5):
bei IDEAL legt die Struktur-Erkennung die Arbeitsgänge an ([[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]),
bei AKE kommen sie aus dem bestehenden Bestand (siehe Prüfschritt P-T1).

### Heutige Logik am Anknüpfungspunkt (belegt, Bündel v1.47.4)

- **View:** Karte „2. Arbeitsgang wählen“ (`Views/BdeTerminal/Index.cshtml:60-75`), Container
  `#operationButtons` (`:63`), darunter Scan „FA-Nr,AG-Nr“ (`:65-69`) mit Disambiguierung `#scanSelection`
  (`:70-73`). Die Karte erscheint nach dem Operator-Scan (`style="display:none"`, `:60`).
- **Controller/Seite:** `BdeTerminalController.Index` (`BdeTerminalController.cs:36-60`) liefert nur Terminal,
  Werkbank und `NurFaMode` (`:55-57`); Klasse `[RequireBdeActive]` + `[RequireBdeUserAccess]` (`:14-15`).
- **JS:** `loadAvailableOperations()` (`wwwroot/js/bde-terminal.js:434-440`) ruft
  `GET /api/bde/available-operations/{workplaceId}?operatorId=` und `renderOperationButtons` (`:442-478`) baut je
  Eintrag einen Knopf `btn-outline-success btn-lg bde-op-btn` mit `data-wo-id` (`:455`); das Label wird
  **ungeescaped** in `innerHTML` gesetzt (`:453`, `:455`). Klick → `currentWorkOp` + Aktionsknöpfe (`:496-499`).
- **API:** `BdeApiController.GetAvailableOperations` (`BdeApiController.cs:193-286`), Klasse `[RequireBdeActive]`,
  `[RequireBdeUserAccess]` (`:11-14`):
  - Werkbank nicht `BdeAktiv` → leere Liste (`:196-200`).
  - **NurFA-Modus** (`AppSettingKeys.BdeNurFaMeldung`, `:202-205`): offene `ProductionOrder`s mit
    `ProductionWorkplaceId == workplaceId`, nicht erledigt/storniert/verpackt/abgeholt, ohne `IsFinal`, ohne
    eigene laufende Buchung, **sortiert nach `ProductionDate`** (`:207-241`).
  - **Normalmodus:** `WorkOperationRepository.GetOpenByWorkplaceIdAsync(workplaceId, excludePackedOrders: true)`
    (`:245`; Repository `WorkOperationRepository.cs:59-82`: Werkbank, `!IsReported`, ohne verpackt/abgeholt,
    sortiert `OrderNumber`, `Sequence`). Danach ausgeschlossen: Arbeitsgänge mit **irgendeiner** offenen
    Buchung (`GetActiveCockpitAsync` = `EndedAt == null && !IsCancelled`, `BdeBookingRepository.cs:55-64`;
    `BdeApiController.cs:246-249`), mit `IsFinal`-Menge (`:252-257`), mit eigener laufender Buchung
    (`:260-270`); Filter `:272-273`. Label `"{OrderNumber} / {OperationNumber} — {Name}"` (`:277`).
- **Gates:** Das Terminal ist nur bei globalem `BdeAktiv` erreichbar (`RequireBdeActiveAttribute`) und listet
  nur für `BdeAktiv`-Werkbänke (`:197`) — das Gate aus Teil-2-Antwort 4 (global **und** Werkbank) ist am
  Terminal damit immer erfüllt. Laut Entscheidung 1 der Backlog-Notiz, übernommen in
  [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] (dort Prüfschritt P5), ist
  `BdeAktiv` derzeit an keinem Standort an (DB-Wert vom Spec-Lauf nicht geprüft) — produktiv wirkt Teil 3 erst,
  wenn BDE eingeschaltet wird; Abnahme am Testsystem.
- **Startprüfung bei einem laufenden Arbeitsgang (belegt, v1.49.0):** `BdeBookingService.StartPlannedAsync`
  (`BdeBookingService.cs:36-64`) liest `BdeMehrfachBuchungProArbeitsgang` (`:51`) und sucht bei Rüsten **immer**,
  bei Produktion nur wenn die Einstellung aus ist, eine offene Buchung eines **anderen** Werkers auf derselben
  `WorkOperation` (`:56-63`) → `BdeBookingResult.Collision`. Das Terminal zeigt das als Modal „AG ist bereits in
  Arbeit durch … an … seit …“ (`bde-terminal.js:360-363`, `Views/BdeTerminal/Index.cshtml:114-117`). Diese
  Prüfung bleibt unverändert (Antwort 6).
- **Fertigungstermin heute (belegt):** AKE `ProductionOrder.ProductionDate` = Sage-Spalte `[Fertigungstermin]`
  aus `vw_AKE_Kommissionierung_WAListe` (`IDEALAKEWMSService/Services/SageImportService.cs:55-57`). IDEAL: die
  FA-Liste nimmt `FE_Termin` der HauptFA als Produktionsdatum, wenn genau eine Kopfzeile existiert, sonst
  `ProductionOrder.ProductionDate` (`ProductionOrdersController.cs:199`, `:294-305`, Kombigerät `:314-329`).
  Die Materialisierung setzt `ProductionDate` **nicht** (kein Treffer für `ProductionDate` in
  `FaMaterializationSyncService.cs`; einzige Schreibstelle im Service ist `SageProductionOrderSql.cs:14-41` des
  flachen Sage-Syncs).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Normalmodus der Arbeitsgang-Auswahl am BDE-Terminal: Gruppierung je Auftrag, Sortierung nach
  Fertigungstermin, Status je Arbeitsgang (offen / läuft / läuft + überfällig / überfällig) mit Farbe + Zeichen
  + `aria-label`.
- Arbeitsgänge mit laufender Buchung eines **anderen** Werkers bleiben sichtbar mit „läuft“ (Antwort 6 = A).
- Erweiterung der bestehenden API-Antwort und des bestehenden Renderings — **kein** neuer Endpunkt, **keine**
  neue View, **keine** neue Rolle.
- Beide Standorte (AKE, IDEAL).

**Out-of-Scope**
- Auswahlmenge der Arbeitsgänge (welche erscheinen) — unverändert, **außer** Antwort 6 („läuft“ sichtbar).
- NurFA-Modus — **unverändert**: keine Termin-Regel, kein Status, Sortierung wie heute nach `ProductionDate`
  (Antwort 8, 2026-10-09; vormals „laut Rückfrage 8 (Arbeitsannahme: unverändert)“).
- Scan-Weg (`/api/bde/resolve-scan`, `BdeScanResolver`), Buchungslogik, Kollisionsprüfung, BDE-Cockpit,
  Tageshistorie, Aktive Buchungen.
- Offset-Pflege, Liste „nicht konfiguriert“ → Vorbedingungs-Spec. Am Terminal erscheint **kein** Hinweis zu
  fehlenden Offsets (Antwort 7 nennt nur „ohne Eintrag 0“; der Werker kann daran nichts ändern); gemeldet wird
  zentral in der Liste „nicht konfiguriert“ der Stammdaten (Backlog-Entscheidung 3 der Vorbedingung) und im
  Sammelhinweis von Teil 2.
- Status „erledigt“: erledigte Arbeitsgänge stehen heute nicht in der Liste (`BdeApiController.cs:252-257`,
  `:273`) und bleiben draußen (Antwort 3: „offener Arbeitsgang“; Antwort 4 nennt nur
  überfällig/offen/läuft).
- Arbeitsgänge **anderer** Werkbänke desselben Auftrags (Antwort 3: „an dieser Werkbank“).
- Datenmodell, Offsets-Pflege ~~(kommt aus Teil 2)~~ — überholt (2026-10-13): Pflege kommt aus der Vorbedingung
  [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]] (umgesetzt), die Offset-Pflichtprüfung aus Teil 2 FA 4a.

## Fachliche Anforderungen

1. **Bestand (Antwort 3 + 6):** Ein Auftrag erscheint, wenn er mindestens einen Arbeitsgang hat, den das
   Terminal an dieser Werkbank anbietet: Auswahlmenge wie `BdeApiController.cs:245-273`, **mit einer
   Änderung (Antwort 6 = A):** Arbeitsgänge in `activeWoIds` (offene Buchung irgendeines Werkers, `:246-249`)
   werden nicht mehr ausgeschlossen, sondern als „läuft“ markiert. Weiter ausgeschlossen bleiben: `IsReported`
   und verpackt/abgeholt (Repository `WorkOperationRepository.cs:59-82`), `IsFinal` (`:252-257`) und eigene
   laufende Buchungen des angemeldeten Werkers (`myActiveWoIdSet`, `:260-270` — stehen unter „Aktuelle
   Buchungen“). Ohne `operatorId` ist `myActiveWoIdSet` leer (`:269`); dann erscheinen alle laufenden als
   „läuft“. Kein Zeitfenster, keine Paginierung (Bestand heute ebenfalls ungepaged).
2. **Auftrag = `ProductionOrder`** des Arbeitsgangs (`WorkOperation.ProductionOrderId`). Kopf: `OrderNumber`,
   bei abweichender `SubOrderNumber` zusätzlich diese (IDEAL: HauptFA / Sub-FA — nötig, weil mehrere Sub-FAs
   derselben HauptFA an einer Werkbank sonst gleich aussehen, vgl. W-1 in Teil 2), `Description1`,
   Fertigungstermin (TT.MM.JJJJ bzw. „ohne Termin“).
3. **Fertigungstermin je Auftrag** (= Sortierschlüssel **und** Bezugstermin für „überfällig“):
   - **Kopfzeilen vorhanden (IDEAL):** `FaHierarchyOrderInfo`-Zeilen zur HauptFA (`int.TryParse(OrderNumber)` →
     `HauptFA`, Muster `ProductionOrdersController.cs:271-275`, v1.49.0) → der **früheste nicht leere
     `FE_Termin`** — bei genau einer Kopfzeile deren `FE_Termin` (wie FA-Liste `:294-305`), bei Kombigeräten der
     früheste (Teil-2-Antwort 9, gleiche Regel wie Teil 2 FA 4). (Vormals „Kombigerät: Regel aus
     Teil-2-Rückfrage 9; bis dahin wie FA-Liste“ — überholt.)
   - **Keine Kopfzeile (AKE, und IDEAL-Aufträge ohne Kopfdaten):** `ProductionOrder.ProductionDate` (Antwort 7,
     2026-10-09: bei AKE Fertigungstermin für Sortierung und Bezugstermin für „überfällig“; Quelle Sage-Spalte
     `[Fertigungstermin]`, `SageImportService.cs:55`).
   - **Kopfzeilen vorhanden, aber alle `FE_Termin` leer:** `ProductionDate` (Rückfall wie FA-Liste
     `ProductionOrdersController.cs:199`, `groupProductionDate ?? o.ProductionDate`) — **entschieden, Antwort 9
     (Gerald 2026-10-12)**. Bewusste Abweichung: Teil 2 zeigt in diesem Fall in der FA-Struktur „ohne Termin“.
   - Ergibt sich kein Datum → „ohne Termin“.
   - Die Regel unterscheidet die Standorte über die **Daten** (Kopfzeilen gibt es nur bei IDEAL), nicht über den
     Master-Schalter `ProduktionsauftragHierarchisch`.
4. **Sortierung (Antwort 3):** Aufträge aufsteigend nach Fertigungstermin (vergangene/überfällige oben, je
   weiter in der Zukunft desto weiter unten), Aufträge **ohne** Termin am Ende; Gleichstand nach `OrderNumber`,
   dann `SubOrderNumber`. Innerhalb eines Auftrags die Arbeitsgänge nach `Sequence` (wie heute).
5. **Status je Arbeitsgang (Antwort 4, Logik aus Teil 2 — dieselbe reine Funktion, Teil 2 FA 7a):**
   - **läuft:** offene Buchung (`EndedAt == null && !IsCancelled`) eines anderen Werkers (Antwort 6 = A); die
     Menge liegt bereits vor (`activeWoIds`, `BdeApiController.cs:246-249`; Prädikat
     `BdeBookingRepository.cs:62` = dasselbe wie Teil 2 FA 3), keine neue Abfrage. Pausierte Buchungen
     (`EndedAt` gesetzt) zählen nicht.
   - **läuft + überfällig:** läuft **und** Soll-Termin < heute → „läuft“ mit Zusatz „überfällig“ (Teil-2-Antwort
     10; vormals „Vorrang läuft/überfällig laut Teil-2-Rückfrage 10“ — überholt).
   - **überfällig:** nicht laufend, Soll-Termin < heute (Lokaldatum, nur Datumsteil); Soll-Termin =
     `OseonDueDateCalculator.Calculate(Fertigungstermin, Offset(OperationNumber), _businessDays, holidays)`
     (Werktage Mo-Fr ohne Feiertage, Teil 2 FA 4). Offset aus dem Lookup der Vorbedingung, case-insensitiv,
     **ohne Eintrag 0** — an beiden Standorten gleich (Antwort 7; vormals „Offset-Quelle/Default laut
     Teil-2-Rückfrage 8; AKE laut Rückfrage 7“ — überholt). Umgesetzt (v1.54.0):
     `IOperationConfigRepository.GetDueDateOffsetsAsync()` liefert ein `OrdinalIgnoreCase`-Dictionary über alle Zeilen
     von `dbo.OperationConfigs` (`OperationConfigRepository.cs:67-77`); Lookup mit `OperationNumber.Trim()`, fehlender
     Schlüssel = Offset 0. Jede Site hat ihre eigene DB; auf der AKE-DB teilen sich OSEON-AG-Namen und AKE-BDE-Kürzel
     dieselbe Tabelle (ein Offset je Kürzel, Bezugstermin je Fremdsystem laut Doku `Models/OperationConfig.cs:24-27`) —
     Kollisionen zeigt Prüfschritt P-T1.
   - **offen:** sonst.
   - Ohne Fertigungstermin: nie überfällig (auch nicht bei „läuft“).
   - **Nicht am Terminal:** „erledigt“ (ausgeschlossen, siehe Umfang) und „kein BDE-Arbeitsgang“ (das Terminal
     listet nur vorhandene `WorkOperation`s). Die **W-4-Zusammenfassung** aus Teil 2 FA 7b gilt hier **nicht**:
     Jede `WorkOperation` ist am Terminal ein eigener Knopf (wie heute) und trägt ihren eigenen Status.
   - BDE-Gate aus Teil 2 FA 6 ist am Terminal immer erfüllt (siehe „Gates“).
6. **Codierung (Teil-2-Antwort 1 + 10):** gleiche Farben, Zeichen und `aria-label`-Texte wie Teil 2 FA 7
   (Warndreieck / leerer Kreis / Läuft-Zeichen / Läuft-Zeichen + Warndreieck mit rotem Akzent); Text im Format
   von Teil 2 ~~`„<Kürzel> <Name> – <Status>“`, z. B. „KA Kanten – läuft, überfällig (Soll 04.05.2026)“~~ — überholt
   (2026-10-13, Abgleich A3): Teil 1 hat „KA – Name“ umgesetzt, Teil 2 hängt den Status an. Am Terminal daher
   `„<OperationNumber> – <Name> – <Status>“` mit `WorkOperation.Name` (= Werkbank-Name beim Anlegen kopiert,
   `WorkOperationStructureDetectionService.cs:172`), z. B. „KA – Kanten – läuft, überfällig (Soll 04.05.2026)“. Sichtbar am
   Knopf: `OperationNumber` und `Name`, dazu das Statuszeichen; der Statustext steht zusätzlich als visuell
   versteckter Text bzw. `aria-label` (Touch: `title` erscheint nicht). Farbe nie allein; Kontrast WCAG AA;
   Knopfgröße wie heute (`btn-lg`, Touch).
7. **Bedienung unverändert:** Klick auf einen Arbeitsgang-Knopf startet denselben Ablauf wie heute
   (`data-wo-id` → `currentWorkOp`, `bde-terminal.js:496-499`). Ungeplante Tätigkeiten und Leermeldungen wie
   heute (`:461-474`). Auch ein „läuft“-Knopf ist wählbar; ob der Start gelingt, entscheidet die bestehende
   Startprüfung (Rüsten: Kollisions-Modal; Produktion: Kollisions-Modal nur bei
   `BdeMehrfachBuchungProArbeitsgang` = aus, sonst zweite Buchung) — keine zusätzliche Sperre im Terminal
   (Antwort 6).
7a. **NurFA-Modus (Antwort 8):** Ist `BdeNurFaMeldung` an, bleibt der Zweig `BdeApiController.cs:205-241` und das
   Rendering `bde-terminal.js:452-453` byte-gleich: keine Gruppierung, keine Termin-Regel, kein Status.
8. **Sicherheit:** Auftrags- und Artikeltexte (Sage-Daten) werden escaped bzw. per `textContent` gesetzt;
   die bestehende ungeescapte Label-Verkettung (`:453`, `:455`) wird dabei für den Normalmodus ersetzt.

## Technischer Loesungsentwurf

Leiter: vorhandener Endpunkt, vorhandene Mengen, vorhandener Calculator — nur Anreicherung, Sortierung und
Rendering sind neu.

- **API (`GetAvailableOperations`, Normalmodus):** Auswahlmenge wie heute, nur `activeWoIds` wird im Filter
  `:273` nicht mehr ausgeschlossen, sondern je Arbeitsgang als `isRunning` mitgegeben (Antwort 6). Zusätzlich
  je Seitenaufruf:
  1. `IFaHierarchyOrderInfoRepository.GetByHauptFaKeysAsync` über die numerischen `OrderNumber`s der Treffer
     (eine Abfrage; bei AKE leer, kein Gate nötig).
  2. Feiertage (`IHolidayRepository.GetHolidayDatesAsync`) und Offsets über den Lookup der Vorbedingung
     (Schnittstelle wie Teil 2 FA 4, gecacht zulässig) — je ein Aufruf. (Vormals „Quelle laut
     Teil-2-Rückfrage 8; bei Variante A gecacht“ — überholt.)
  3. Reine Funktion: Fertigungstermin je Auftrag (FA 3) → Soll-Termin je Arbeitsgang → Status (Teil-2-Funktion,
     unverändert wiederverwendet) → Gruppierung + Sortierung (FA 4).
  - Der NurFA-Zweig (`:205-241`) wird nicht angefasst (Antwort 8).
  - Antwortform rückwärtskompatibel erweitern: `productive` bleibt eine Liste, jedes Element bekommt
    zusätzlich `productionOrderId`, `orderNumber`, `subOrderNumber`, `description`, `productionDate`,
    `operationNumber`, `operationName`, `status`, `dueDate`; Reihenfolge = Sortierung FA 4. Das JS gruppiert
    nach `productionOrderId` (Reihenfolge bleibt erhalten). `label` bleibt für Bestandsaufrufer.
  - Abhängigkeiten neu im Ctor (`BdeApiController.cs:26-29`, v1.49.0; v1.54.0 unverändert): `IFaHierarchyOrderInfoRepository`,
    `IHolidayRepository`, `IBusinessDayService`, der Offset-Lookup der Vorbedingung (`IOperationConfigRepository`,
    DI `Program.cs:125-126`). Die 9 direkten Konstruktionen in `BdeApiControllerTests.cs` ziehen mit. ~~ggf.
    `IOseonOperationConfigRepository` (Teil-2-Rückfrage 8)~~ — überholt (Teil-2-Antwort 8).
  - Antwortfelder `status` (Werte `open`/`running`/`overdue`) und `isOverdue` (bool, auch bei `running`) statt
    eines kombinierten Werts — so bleibt „läuft + überfällig“ ohne Sonderwert darstellbar; `dueDate` als
    `yyyy-MM-dd` oder `null`.
- **JS (`renderOperationButtons`):** Normalmodus: je Auftrag ein Block (Kopf FA 2 + Knopfreihe), Knöpfe mit
  Status-Zeichen und `aria-label`; per DOM-API/`textContent` statt String-Verkettung. NurFA- und
  Aktivitäts-Zweig unverändert. `bindOperationButtonHandlers` bleibt (Selektor `#operationButtons .bde-op-btn`).
- **CSS (`bde.css`):** Auftragsblock, Status-Varianten; Stil-Bausteine aus Teil 2 wiederverwenden, keine neue
  UI-/Icon-Bibliothek. **frontend-design-Skill ist im Dev-Lauf Pflicht.**
- **Antwort 6 = A (entschieden 2026-10-09; vormals „Rückfrage 6 = sichtbar“ als Bedingung):** `activeWoIds` nicht
  mehr ausschließen, sondern als „läuft“ markieren; eigene laufende Buchungen (`myActiveWoIdSet`) bleiben
  ausgeschlossen (stehen unter „Aktuelle Buchungen“). Start auf einen laufenden Arbeitsgang prüft der Service
  wie heute (`BdeBookingService.cs:51-63`, v1.49.0: Rüsten immer exklusiv, Produktion nur bei
  `BdeMehrfachBuchungProArbeitsgang`); Anzeige der Kollision `bde-terminal.js:360-363`.

## Migrations-/SQL-Auswirkungen

Keine eigene. Keine Migration, kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt.
Eine etwaige Migration für die Offsets bringt die Vorbedingungs-Spec
([[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]; umgesetzt als `20261012140548_RenameOperationConfigsAndAddAudit`
/ `SQL/95`); Teil 3 liest nur und setzt voraus, dass `SQL/95` auf der jeweiligen Site-DB (AKE **und** IDEAL) gelaufen ist. ~~Falls Teil-2-Rückfrage 8
Variante B (Feld an der Werkbank) wählt, bringt Teil 2 die Migration~~ — überholt (Teil-2-Antwort 8).

## Audit-Feld-Auswirkungen

Keine — reine Lesedarstellung, es wird nichts geschrieben.

## Rollen / Zugriffsfilter

Unverändert (Antwort 2): `[RequireBdeActive]` + `[RequireBdeUserAccess]` auf `BdeApiController`
(`BdeApiController.cs:11-14`) und `BdeTerminalController` (`BdeTerminalController.cs:14-15`); zugelassen
admin, bde_user, bde_shiftlead, bde_admin ([[controller]] Z.73). Kein neuer Filter, keine Änderung an
`controller.md`/`RoleOverview`.

## Listen-View-Pattern (ADR 0005)

Kein Listen-View: Die Anzeige ist eine Auswahl-Knopfliste in einer Terminal-Karte, keine Tabelle. Keine
Pagination (Antwort 3 „kein Zeitfenster“, Bestand heute ebenfalls ungepaged), keine Spaltenfilter. Begründung
und Lastgrenze prüft Testszenario T6.

## Akzeptanzkriterien

1. Nach dem Operator-Scan zeigt „2. Arbeitsgang wählen“ im Normalmodus die angebotenen Arbeitsgänge
   **gruppiert je Auftrag**; Kopf = `OrderNumber` (+ `SubOrderNumber`, wenn abweichend), `Description1`,
   Fertigungstermin oder „ohne Termin“.
2. Es erscheinen die Arbeitsgänge, die das Terminal heute anbietet (Ausschlüsse `IsReported`,
   verpackt/abgeholt, `IsFinal`, eigene laufende Buchung unverändert), **plus** Arbeitsgänge mit laufender
   Buchung eines anderen Werkers, markiert als „läuft“ (Antwort 6 = A; vormals „Ausnahme laut Rückfrage 6“). Ein
   Auftrag ohne solchen Arbeitsgang erscheint nicht. InMemory-Test: Arbeitsgang mit offener Buchung von Werker B
   erscheint bei Werker A mit `status = running`, bei Werker B gar nicht.
3. Aufträge sind aufsteigend nach Fertigungstermin sortiert, ohne Termin am Ende; Gleichstand nach
   `OrderNumber`, `SubOrderNumber`; innerhalb nach `Sequence` (Unit-Test der reinen Funktion).
4. Fertigungstermin-Regel per Unit-Test: IDEAL-Auftrag mit genau einer Kopfzeile → deren `FE_Termin`;
   Kombigerät mit `FE_Termin` 10.05. und 05.05. → 05.05. (Teil-2-Antwort 9); ohne Kopfzeile → `ProductionDate`;
   AKE-Auftrag (keine Kopfzeile) → `ProductionDate` (Antwort 7); Kopfzeilen nur mit leerem `FE_Termin` →
   `ProductionDate` (Antwort 9); kein Datum → „ohne Termin“, am Ende sortiert.
5. Jeder Arbeitsgang-Knopf zeigt Kürzel + Name und den Status offen / läuft / läuft + überfällig / überfällig mit
   Farbe **und** Zeichen **und** `aria-label` (z. B. „KA – Kanten – überfällig (Soll 04.05.2026)“, „KA – Kanten –
   läuft, überfällig (Soll 04.05.2026)“; Format angeglichen 2026-10-13, vormals „KA Kanten – …“); Graustufen-Screenshot unterscheidbar (auch „läuft“ vs. „läuft +
   überfällig“); Kontrast ≥ WCAG AA am Terminal gemessen.
6. Überfällig genau dann, wenn Soll-Termin (Fertigungstermin + Offset in Werktagen aus den Arbeitsgang-
   Stammdaten, ohne Eintrag 0) < heute; Fertigungstermin heute → nicht überfällig; ohne Fertigungstermin nie
   überfällig. Gleicher Prüfwert wie Teil 2 AK 2 (FE_Termin/ProductionDate Do 30.04.2026, Offset 1, 01.05.
   Feiertag → Soll Mo 04.05.2026). Unit-Test: `OperationNumber` `ka` trifft Stammdaten-Eintrag `KA`; AKE-AG `01`
   ohne Eintrag → Soll = `ProductionDate`.
7. Klick auf einen Arbeitsgang-Knopf verhält sich wie heute (Aktionsknöpfe Rüsten/Produktion erscheinen,
   Buchung startet); Scan „FA-Nr,AG-Nr“ und Disambiguierung unverändert. Klick auf einen „läuft“-Knopf und
   „Rüsten“ → bestehendes Kollisions-Modal („AG ist bereits in Arbeit durch …“); „Produktion“ → Kollisions-Modal
   bei `BdeMehrfachBuchungProArbeitsgang` = aus, sonst zweite Buchung (Antwort 6).
8. NurFA-Modus unverändert (Antwort 8): bei `BdeNurFaMeldung` = an liefert die API dieselbe Ausgabe in derselben
   Reihenfolge wie vor der Änderung (`id`, `label`, `type = "fa"`, sortiert nach `ProductionDate`), ohne Status
   und ohne Gruppierung — InMemory-Vergleichstest.
9. Benutzer ohne BDE-Rolle → `GET /api/bde/available-operations/{id}` verweigert (wie heute); BDE global aus →
   Terminal nicht erreichbar (wie heute); Werkbank nicht BDE-aktiv → leere Liste (wie heute).
10. Abfragebudget: gegenüber heute höchstens **drei** zusätzliche Abfragen je Aufruf (Kopfzeilen, Feiertage,
    Offset-Lookup; gecachte zählen nicht), unabhängig von der Zahl der Aufträge (Query-Zählung im Dev-Lauf). Die
    Menge „läuft“ kommt aus der bestehenden `activeWoIds`-Abfrage (keine zusätzliche).
11. Ein `Description1` mit `<b>x</b>` erscheint als Text, nicht als HTML.
12. AKE-Testsystem: Liste funktioniert ohne FA-Struktur-Daten, sortiert nach `ProductionDate`; „überfällig“
    bezieht sich auf `ProductionDate` + Offset (Antwort 7).
13. Status-Ableitung ist **dieselbe** Funktion wie in Teil 2 (kein Duplikat; Code-Review/Grep belegt einen
    einzigen Ort); am Terminal erscheinen weder „erledigt“ noch „kein BDE-Arbeitsgang“; zwei `WorkOperation`s
    gleichen Kürzels am selben Auftrag sind zwei Knöpfe mit je eigenem Status (keine W-4-Zusammenfassung).
14. Am Terminal erscheint kein Hinweis zu fehlenden Offsets; ein Kürzel ohne Stammdaten-Eintrag rechnet mit
    Offset 0 ohne Fehler.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` des Bündel-Worktrees (Nummer: nächste freie im Bündel zum
Umsetzungszeitpunkt; ~~Stand 2026-10-12 höchstes TS-82, Teil 1, Vorbedingung und Teil 2 kommen davor~~ — überholt;
Stand 2026-10-13: höchstes TS-87 (Teil 1 = TS-85, Vorbedingung = TS-87), Teil 2 voraussichtlich TS-88 → Teil 3
voraussichtlich **TS-89**), Index in
`secondbrain/tests/testszenarien-index.md` nachziehen. Bestehende BDE-Terminal-Szenarien (Kapitel 8-10, 15,
TS-66) auf die geänderte Darstellung prüfen — insbesondere Stellen, die erwarten, dass ein von einem anderen
Werker gebuchter Arbeitsgang **nicht** in der Liste steht (gilt nach Antwort 6 nicht mehr).

- **Vorbedingungen:** BDE global aktiv, Werkbank `BdeAktiv`, Terminal für den Testbenutzer konfiguriert,
  zwei Benutzer mit `bde_user` (Werker A, B); an der Werkbank ≥ 3 Aufträge mit offenen Arbeitsgängen: einer mit
  Fertigungstermin in der Vergangenheit, einer heute + 5 Werktage, einer ohne Termin; Vorbedingung
  Arbeitsgang-Stammdaten umgesetzt (Bündel ≥ v1.54.0, `SQL/95` auf der jeweiligen DB gelaufen), Offsets dort gepflegt
  (ein Kürzel mit Offset ≠ 0, eines ohne Eintrag), auf IDEAL Seed-Kollisionen bereinigt (Vorbedingung P3), Feiertage
  gepflegt; Teil 2 umgesetzt. IDEAL-Testsystem **und** AKE-Testsystem.
- **T1 Gruppierung + Sortierung:** Operator scannen → Aufträge als Blöcke, Reihenfolge: vergangener Termin,
  dann der künftige, dann „ohne Termin“; je Block die Arbeitsgänge dieser Werkbank in Sequenz-Reihenfolge.
- **T2 Status überfällig/offen:** Auftrag mit vergangenem Termin → Arbeitsgang rot + Warndreieck,
  Screenreader „… – überfällig (Soll …)“; künftiger → grau + leerer Kreis.
- **T3 Status läuft (Antwort 6 = A):** Werker B startet einen Arbeitsgang dieser Werkbank → bei Werker A nach
  Neuladen der Liste sichtbar mit Läuft-Zeichen, Screenreader „… – läuft“; bei Werker B steht er nicht in der
  Liste, sondern unter „Aktuelle Buchungen“. Werker A wählt ihn und „Rüsten“ → Kollisions-Modal „AG ist bereits
  in Arbeit durch B …“; „Produktion“ → Kollisions-Modal bei `BdeMehrfachBuchungProArbeitsgang` = aus, sonst
  startet eine zweite Buchung. Werker B pausiert → bei A nach Neuladen „offen“ bzw. „überfällig“.
- **T3a Läuft + überfällig (Teil-2-Antwort 10):** Arbeitsgang mit vergangenem Soll-Termin, von Werker B gestartet
  → bei A Läuft-Zeichen **und** Warndreieck, roter Akzent, Screenreader „… – läuft, überfällig (Soll …)“.
- **T4 Bedienung:** Arbeitsgang antippen → Rüsten/Produktion wie bisher; Abschluss mit Menge → Arbeitsgang
  verschwindet aus der Liste, Auftrag verschwindet, wenn es sein letzter war.
- **T5 IDEAL-Termin:** HauptFA mit genau einer Kopfzeile → angezeigter Termin = `FE_Termin` aus
  `/FaHierarchy`; Kombigerät → frühester `FE_Termin` der Kopfzeilen (Teil-2-Antwort 9; vormals „laut
  Teil-2-Rückfrage 9“); HauptFA mit Kopfzeile ohne `FE_Termin` → ~~laut Rückfrage 9~~ angezeigter Termin =
  `ProductionOrder.ProductionDate` (Antwort 9, Gerald 2026-10-12; korrigiert 2026-10-13), „überfällig“ bezogen auf
  dieses Datum + Offset; ohne `ProductionDate` „ohne Termin“, am Ende. Gegenprobe in `/FaHierarchy`: dieselbe
  Struktur zeigt dort nie „überfällig“ (Teil 2, bewusst abweichend).
- **T6 Viele Aufträge:** Werkbank mit vielen offenen Arbeitsgängen (≥ 100) → Liste lädt ohne spürbare
  Verzögerung, Blöcke umbrechen, kein horizontales Scrollen.
- **T7 AKE:** AKE-Testsystem, gleiche Schritte T1/T2 → Sortierung und Überfällig nach `ProductionDate` +
  Offset aus den Arbeitsgang-Stammdaten, ohne Eintrag 0 (Antwort 7; vormals „Bestätigung Rückfrage 7“). Hat
  AKE nur den Default-AG `01` bzw. NurFA an (P-T1), wirkt nur der NurFA-Zweig → unverändert prüfen (T9).
- **T9 NurFA unverändert (Antwort 8):** `BdeNurFaMeldung` einschalten → Liste „Produktionsaufträge“ wie vor der
  Änderung (Knöpfe „FA — Bezeichnung“, nach `ProductionDate` sortiert), keine Auftragsblöcke, keine
  Statuszeichen; FA-Start wie bisher.
- **T8 Barrierefreiheit:** Graustufen + Screenreader + Kontrastmessung am Fertigungsterminal.
- **Negativ:** Benutzer ohne BDE-Rolle → Zugriff verweigert; Werkbank ohne offene Arbeitsgänge → „Keine
  offenen Arbeitsgänge an dieser Werkbank.“; Auftragstext mit HTML-Zeichen → als Text angezeigt; NurFA-Modus
  eingeschaltet → unverändert (T9, Antwort 8); Kürzel ohne Offset-Eintrag → Soll = Fertigungstermin, kein
  Hinweis am Terminal, kein Fehler; Scan eines Auftrags, der nicht in der Liste steht → bestehende Meldung des
  Scan-Wegs.

## Pruefschritte fuer den Menschen (ungeprüft)

**P-T1 — Welche `WorkOperation`s gibt es bei AKE?** Im Repo legt außer der IDEAL-Struktur-Erkennung nur
`BdeDefaultWorkOperationService` Arbeitsgänge an (Default `"01"` im NurFA-Modus,
`BdeDefaultWorkOperationService.cs:27-47`); weitere Quellen sind im Code nicht sichtbar. Auf der
**AKE-WMS-Datenbank**:

```sql
SELECT wo.OperationNumber, COUNT(*) AS Anzahl, MIN(wo.CreatedBy) AS AngelegtVon,
       SUM(CASE WHEN wo.IsReported = 0 THEN 1 ELSE 0 END) AS Offen
FROM dbo.WorkOperations wo
GROUP BY wo.OperationNumber ORDER BY Anzahl DESC;

SELECT [Key], [Value] FROM dbo.AppSettings WHERE [Key] IN ('BdeAktiv', 'BdeNurFaMeldung');
```

Nur `01` bzw. `BdeNurFaMeldung = true` → bei AKE greift der NurFA-Zweig (Rückfrage 8), die Auftragsgruppen
des Normalmodus wären dort ohne Wirkung. (Rework 2026-10-12: Antwort 8 hält den NurFA-Modus unverändert — dann
ändert Teil 3 an der AKE sichtbar nichts. Gibt es nur `01` im Normalmodus, rechnet `01` ohne Stammdaten-Eintrag
mit Offset 0; ~~ob `01` in der Liste „nicht konfiguriert“ der Vorbedingung auftauchen soll, ist dort zu klären.~~ —
entschieden und umgesetzt (Vorbedingungs-Antwort 7, 2026-10-12): `01` erscheint **nie** in „nicht konfiguriert“
(`OperationConfigRepository.cs:80` `DefaultOperationNumber`, Filter `:92`); wer für `01` einen Offset will, legt ihn
manuell an — der Lookup findet ihn.)

**Ergänzung 2026-10-13 — Kollision OSEON-AG-Namen ↔ AKE-BDE-Kürzel:** Auf der AKE-DB stehen in `dbo.OperationConfigs`
die OSEON-AG-Namen (Offset relativ zum OSEON-Stanztermin). Trägt eine AKE-`WorkOperation` dieselbe `OperationNumber`
(getrimmt, case-insensitiv), rechnet das Terminal deren Offset auf `ProductionDate` — ein Wert für zwei Bezugstermine
(so von der Vorbedingung gewollt: ein Offset je Kürzel, Doku `Models/OperationConfig.cs:24-27`). Ob das auf der AKE-DB
vorkommt, zeigt die Abfrage P3 „nach dem Deploy“ der Vorbedingungs-Spec, auf der **AKE-WMS-DB** ausgeführt:

```sql
SELECT c.OperationName, c.DueDateOffsetDays, wo.Anzahl
FROM dbo.OperationConfigs c
JOIN (SELECT OperationNumber, COUNT(*) AS Anzahl FROM dbo.WorkOperations GROUP BY OperationNumber) wo
  ON UPPER(LTRIM(RTRIM(wo.OperationNumber))) = UPPER(LTRIM(RTRIM(c.OperationName)));
```

Leer (zu erwarten, wenn AKE nur `01` hat) → kein Thema. Treffer → vor der AKE-Abnahme dem Menschen melden
(unverifiziert, ob es fachlich passt; die Spec entscheidet das nicht).

**P-T2 — `ProductionDate` bei IDEAL:** Auf der **IDEAL-WMS-Datenbank**, ob materialisierte Aufträge ein
`ProductionDate` tragen (Fallback der Termin-Regel; bestimmt, wie viel Rückfrage 9 praktisch ausmacht — im
Service schreibt nur der flache Sage-Sync `ProductionDate`, `SageImportService.cs:55`,
`SageProductionOrderSql.cs:14-41`, v1.49.0):

```sql
SELECT COUNT(*) AS Auftraege, SUM(CASE WHEN ProductionDate IS NULL THEN 1 ELSE 0 END) AS OhneProductionDate
FROM dbo.ProductionOrders WHERE IsDone = 0 AND IsCancelled = 0;
```

## Etappen (nur bei epic: true)

Entfällt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Final (QA-Lauf 2026-10-13, gegen den echten Diff `d3746373..d4f326ef`):** Geändert nur `IdealAkeWms/` (Controller,
Services, ViewModels, Views, wwwroot), `IdealAkeWms.Tests/`, `docs/`, und `IDEALAKEWMSService/AppVersion.cs` (nur
Versions-Bump 1.57.0, keine Logik). Keine Datei unter `*/Migrations/`, keine unter `SQL/` → **web: ja, service: nein
(Version-Bump allein erzwingt kein Service-Deploy), migration: nein.** Ablauf: Publish aus dem Worktree → Testsystem → Test →
Merge. Nach dem Merge nur dann erneut aus `main` publishen, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen
zusammengeführt hat. (Vormals „Vorläufig — der QA-Lauf bestätigt gegen den echten Diff“ — überholt.)

- **Web-App:** ja (`BdeApiController`, `bde-terminal.js`, `bde.css`, ggf. `Views/BdeTerminal/Index.cshtml`,
  gemeinsame Status-Funktion). Beide Standorte (AKE und IDEAL) erhalten dieselbe Web-Version.
- **Service:** nein.
- **Migration:** nein (eine etwaige Offset-Migration gehört zur Vorbedingungs-Spec; ~~zu Teil 2, Rückfrage 8 B~~ —
  überholt).
- **Reihenfolge:** im Bündel nach Teil 2 und damit nach der Vorbedingung (depends_on; Vorbedingung seit 2026-10-13
  umgesetzt, v1.54.0). Vor Abnahme P-T1 (AKE, inkl. Kollisionsabfrage) und P-T2 (IDEAL), Offsets in den
  Arbeitsgang-Stammdaten gepflegt. `SQL/95` der Vorbedingung muss auf **beiden** Site-DBs gelaufen sein, bevor ein
  Stand mit Teil 3 startet (Teil 3 liest `dbo.OperationConfigs`).
- **AKE-Hinweis:** Teil 3 wird im Bündel-Zweig umgesetzt; an der AKE wirkt er erst, wenn dort ein Stand mit
  diesem Bündel deployt ist. Die AKE-Abnahme (T7/T9) braucht daher ein AKE-Testsystem mit Bündel-Stand.
- **Publish-Befehle:**

```bash
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Abgleich mit der Umsetzung (2026-10-13)

Gemessen gegen den Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, HEAD `d56a340e`, **v1.54.0**, nach
Umsetzung von Teil 1 ([[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]], v1.52.0) und der Vorbedingung
([[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]], v1.54.0). Teil 2 ist noch nicht umgesetzt; seine
Spec wurde am selben Tag nachgezogen ([[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]], Abschnitt
gleichen Namens).

| # | Annahme der Spec | Umsetzung (Beleg, v1.54.0) | Folge |
|---|---|---|---|
| A1 | Vorbedingungs-Spec existiert noch nicht; `depends_on` auf die Backlog-Notiz | Spec existiert, Testbereit, umgesetzt in v1.54.0 | `depends_on` + Verweise umgestellt, Hinweise als überholt markiert |
| A2 | Offset-Lookup „wie Teil 2 FA 4“, Calculator evtl. umbenannt | `IOperationConfigRepository.GetDueDateOffsetsAsync()` (`OperationConfigRepository.cs:23`, `:67-77`), gecacht (`CachedOperationConfigRepository.cs:41-49`); `OseonDueDateCalculator` unverändert (`:3-13`) | Ctor bekommt `IOperationConfigRepository`; Lookup mit `OperationNumber.Trim()`; Calculator-Name bleibt |
| A3 | `aria-label`-Format „KA Kanten – läuft …“ (aus Teil 2) | Teil 1 hat „KA – Name“ umgesetzt (`FaHierarchyTreeViewModel.cs:218-222`); Teil 2 hängt den Status an | Terminal-Format „`<OperationNumber>` – `<Name>` – `<Status>`“ (FA 6, AK 5) — Angleichung, keine neue Entscheidung |
| A4 | AKE: Offset aus den vereinheitlichten Stammdaten, ohne Eintrag 0 (Antwort 7) | Eine Tabelle je Site-DB für OSEON-Namen **und** WMS-Kürzel, ein Offset je Kürzel (`Models/OperationConfig.cs:14-15`, `:24-27`); Seed der 12 OSEON-Namen bei leerer Tabelle (`Program.cs:471`) | Kollision OSEON-Name ↔ AKE-BDE-Kürzel möglich → Prüfabfrage in P-T1 ergänzt; Treffer werden gemeldet, nicht still entschieden |
| A5 | P-T1: Ob `01` in „nicht konfiguriert“ erscheint, klärt die Vorbedingung | Nie (`OperationConfigRepository.cs:80`, `:92`, Vorbedingungs-Antwort 7) | P-T1-Satz als entschieden markiert |
| A6 | Offset-Pflege ohne Server-Prüfung (nicht betrachtet) | Leerer Offset per manipuliertem POST = still 0 (`OperationConfigsController.cs:72`, `:107`; Aufgabe [[2026-10-12-operationconfig-offset-leer-pruefen]]) | Erledigt Teil 2 (FA 4a, AK 19) vor Teil 3 — kein Umfang in Teil 3 |
| A7 | Ctor-Erweiterung `BdeApiController` ohne Testfolgen genannt | `BdeApiControllerTests.cs` konstruiert den Controller 9× direkt (Z.278 … 594) | In affected_code/Lösungsentwurf aufgenommen (Compile-Nachzug, keine Fixture-Änderung) |
| A8 | Code-Anker v1.49.0 | Unverändert bei v1.54.0: `BdeApiController.cs:11-14`, `:26-29`, `:193-286` (u. a. `:197`, `:202-205`, `:245-273`, `:277`); `bde-terminal.js:360-363`, `:434`, `:442`, `:453`, `:455`, `:480-483`; `Views/BdeTerminal/Index.cshtml:61`, `:63`, `:114`; `BdeBookingService.cs:36`, `:51`, `:54-63`; `WorkOperationRepository.cs:59-82`; `BdeBookingRepository.cs:62`; `ProductionOrdersController.cs:199`, `:272`, `:299`, `:305`; `IFaHierarchyOrderInfoRepository.cs:25` | Keine Zeilenkorrektur nötig |
| A9 | Rückfrage 9 beantwortet (Antwort 9 = a), aber T5 sagte noch „laut Rückfrage 9“; Übersicht zählte 1 offene Rückfrage | — (Spec-intern) | T5 auf Antwort 9 korrigiert; Übersicht nachgezogen |
| A10 | Stand v1.49.0, TS-82 | v1.54.0, TS-87, `SQL/95`, Migration `20261012140548_…`; `main` `SQL/88` | Hinweise aktualisiert; Teil 3 voraussichtlich TS-89 |

**Ergebnis:** Kein Widerspruch, der eine Entscheidung des Menschen braucht — **keine offene Rückfrage, Teil 3 ist
freigabereif.** Die Freigabe setzt voraus, dass Teil 2 freigegeben und vor Teil 3 umgesetzt wird (depends_on).

## Offene Rueckfragen

Aus Notiz-Punkt 5 („Was genau fehlt heute im BDE — welche Ansicht, welche Rolle, welcher
Auftragsbestand?“) und den daraus folgenden Unterfragen. Die Notiz gibt hierzu keine Empfehlung.

> [!success] Fragen 1-5 beantwortet (Keyuser/Gerald 2026-10-07)
> Übernommen in Ziel, Umfang, Fachliche Anforderungen, Lösungsentwurf, AK, Testszenarien, Deploy; Frage 4 stellt
> `depends_on` auf Teil 2 um. In `beantwortete_rueckfragen` verschoben, Text unten als Audit-Trail. Aus dem
> Code-Abgleich neu: Nr. 6-8.

1. ~~**Ansicht:** Was genau fehlt heute im BDE — wo sollen die Aufträge inkl. Arbeitsgänge erscheinen
   (BDE-Terminal je Werkbank, BDE-Cockpit, eine neue Auftragsübersicht)?~~ — **beantwortet (Antwort 1)**
2. ~~**Rolle:** Für wen ist die Anzeige (`bde_user`, `bde_shiftlead`, `bde_admin`, oder auch
   Planung/Leitstand)?~~ — **beantwortet (Antwort 2)**
3. ~~**Auftragsbestand:** Welche Aufträge — alle offenen HauptFAs, nur die der gewählten Werkbank, ein
   Termin-Zeitfenster, nur freigegebene?~~ — **beantwortet (Antwort 3)**
4. ~~**Status:** Arbeitsgänge mit Status-Farben aus Teil 2 oder nur als Symbole aus Teil 1? (bestimmt die
   Abhängigkeit von Teil 2)~~ — **beantwortet (Antwort 4)**
5. ~~**Standort:** nur IDEAL oder auch AKE?~~ — **beantwortet (Antwort 5)**
> [!success] Fragen 6-8 eingearbeitet (Gerald 2026-10-09; Rework 2026-10-12)
> Nr. 6 = A → Umfang, FA 1/5/7, Lösungsentwurf, AK 2/7, T3; Nr. 7 → FA 3/5, `depends_on` Vorbedingung, AK 4/6/12,
> T7; Nr. 8 → Umfang, FA 7a, AK 8, T9. In `beantwortete_rueckfragen` verschoben, Fragetext unten als Audit-Trail.
> Neu aus dem Code-Abgleich: Nr. 9 (IDEAL-Termin, wenn Kopfzeilen keinen `FE_Termin` tragen) — 1 offene Rückfrage.

6. ~~**„Läuft“ widerspricht der heutigen Auswahl**~~ — **eingearbeitet (Antwort 6, 2026-10-09: A)** (neu 2026-10-07, Widerspruch Antwort 4 ↔ Code). Antwort 4
   verlangt den Status „läuft“; das Terminal blendet aber jeden Arbeitsgang mit offener Buchung **irgendeines**
   Werkers aus (`BdeApiController.cs:246-249`, Filter `:273`) — „läuft“ käme in der Liste nie vor. Optionen:
   **A** Arbeitsgänge, die ein anderer Werker bucht, künftig sichtbar mit „läuft“ (Start prüft der Service wie
   heute: Rüsten immer exklusiv, Produktion nur bei `BdeMehrfachBuchungProArbeitsgang`,
   `BdeBookingService.cs:51-63`); **B** weiter ausblenden (dann entfällt „läuft“ am Terminal). Eigene laufende
   Buchungen bleiben in beiden Fällen ausgeblendet (`:260-270`). Empfehlung der Spec: A — sonst ist Antwort 4
   für „läuft“ wirkungslos; die Kollisionsprüfung besteht schon. Entscheidung beim Menschen.
7. ~~**AKE: Fertigungstermin und Überfällig-Bezug**~~ — **eingearbeitet (Antwort 7, 2026-10-09)** (neu 2026-10-07; Antwort 5 ↔ Teil-2-Nachtrag „FE_Termin“).
   Bei AKE gibt es keine `FaHierarchyOrderInfo`; der Sage-„Fertigungstermin“ steht in
   `ProductionOrder.ProductionDate` (`SageImportService.cs:55`). Gilt er bei AKE als Fertigungstermin für die
   Sortierung **und** als Bezug für „überfällig“? Und gelten für AKE-Arbeitsgänge die Offset-Regeln aus
   Teil-2-Rückfrage 8 (inkl. Default ohne Eintrag)? Welche AKE-Arbeitsgänge es gibt, zeigt P-T1.
8. ~~**NurFA-Modus**~~ — **eingearbeitet (Antwort 8, 2026-10-09: unverändert)** (neu 2026-10-07). Ist `BdeNurFaMeldung` an, zeigt das Terminal Aufträge statt Arbeitsgänge,
   bereits nach `ProductionDate` sortiert (`BdeApiController.cs:205-241`), ohne Status. Bleibt das unverändert
   (Arbeitsannahme der Spec, kleinster Eingriff), oder bekommt der NurFA-Modus dieselbe Termin-Regel (FA 3) und
   einen Status je Auftrag?
9. **IDEAL: Kopfzeilen ohne `FE_Termin`** (neu 2026-10-12, aus dem Abgleich Teil 2 ↔ Teil 3). Teil 2 zeigt in der
   FA-Struktur bei leerem `FE_Termin` „nie überfällig“ (kein anderes Datum vorhanden). Teil 3 übernimmt für den
   Terminal-Termin die Regel der FA-Liste, die in diesem Fall auf `ProductionOrder.ProductionDate` zurückfällt
   (`ProductionOrdersController.cs:199`, `groupProductionDate ?? o.ProductionDate`, v1.49.0). Gilt am Terminal
   für einen IDEAL-Auftrag, dessen Kopfzeile(n) keinen `FE_Termin` tragen:
   - **a** `ProductionDate` als Fertigungs- und Bezugstermin (Arbeitsannahme der Spec, wie FA-Liste), oder
   - **b** „ohne Termin“ (am Ende sortiert, nie überfällig — wie Teil 2)?
   Praktische Wirkung hängt an P-T2 (die Materialisierung setzt `ProductionDate` nicht).
   *Beantwortet mit Antwort 9 (Gerald 2026-10-12): a – `ProductionDate`.*

> [!success] Stand 2026-10-13: keine offene Rückfrage
> Nr. 9 ist beantwortet und eingearbeitet (FA 3, AK 4, T5 — T5 am 2026-10-13 nachgezogen). Der Abgleich mit der
> Umsetzung von Teil 1 und der Vorbedingung (Abschnitt „Abgleich mit der Umsetzung (2026-10-13)“) ergab **keine** neue
> Rückfrage. Die Angabe „1 offene Rückfrage“ im Kasten zu den Fragen 6-8 ist **überholt**. `open_questions` bleibt leer.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Ansicht — wo im BDE? → **Am BDE-Terminal je Werkbank bzw. Arbeitsgang die offenen Aufträge anzeigen; anknüpfen beim Schritt „Arbeitsgang wählen“ (`Views/BdeTerminal/Index.cshtml:61`), wo es schon eine Logik gibt (Keyuser 2026-10-07).**
2. Rolle — für wen? → **Bestehende BDE-Rollen, keine neue Rolle (Gerald 2026-10-07).**
3. Auftragsbestand — welche Aufträge? → **Alle Aufträge mit mindestens einem offenen Arbeitsgang an dieser Werkbank, sortiert nach dem nächsten Fertigungstermin (nächster zuerst); je weiter in der Zukunft, desto weiter unten – kein Zeitfenster, der Werker scannt den Auftrag ohnehin an (Gerald 2026-10-07).**
4. Status — mit Farben aus Teil 2 oder nur Symbole aus Teil 1? → **Farbig mit dem Status aus Teil 2 (überfällig/offen/läuft, mit Zeichen zusätzlich zur Farbe) (Gerald 2026-10-07).**
5. Standort — nur IDEAL oder auch AKE? → **Nicht standortspezifisch – gilt auf beiden Systemen (AKE und IDEAL) (Gerald 2026-10-07).**
6. „Läuft“ am Terminal — A Arbeitsgänge anderer Werker sichtbar als „läuft“ oder B weiter ausblenden? → **A (Gerald 2026-10-09): Arbeitsgänge, die ein anderer Werker bucht, bleiben sichtbar mit „läuft“; Start prüft der Service wie heute (Rüsten exklusiv, Produktion nur bei `BdeMehrfachBuchungProArbeitsgang`). Eigene laufende Buchungen bleiben ausgeblendet.**
7. AKE — ProductionOrder.ProductionDate als Fertigungstermin für Sortierung und Überfällig-Bezug; Offset-Regeln wie Teil-2-Rückfrage 8? → **Ja, gleiche Regeln (Gerald 2026-10-09): `ProductionOrder.ProductionDate` ist bei AKE Fertigungstermin für Sortierung und Bezugstermin für „überfällig“; Offset aus den vereinheitlichten Arbeitsgang-Stammdaten ([[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen]]), ohne Eintrag 0.**
8. NurFA-Modus — unverändert lassen oder Termin-Regel + Status je Auftrag? → **Unverändert (Gerald 2026-10-09): keine Termin-Regel, kein Status im NurFA-Modus.**
9. IDEAL-Auftrag mit Kopfzeile(n) ohne FE_Termin — a ProductionDate (wie FA-Liste) oder b „ohne Termin“ (wie Teil 2)? → **a – Rueckfall auf `ProductionOrder.ProductionDate` wie FA-Liste (Gerald 2026-10-12, abweichend von der Koordinations-Empfehlung). Folge: Terminal kann bei diesen Auftraegen „ueberfaellig“ zeigen, waehrend die FA-Struktur (Teil 2) „ohne Termin“ zeigt – bewusst so. Spec-Entscheidung „kein Offset-Hinweis am Terminal“ (AK 14) bestaetigt (Gerald 2026-10-12).**

## Umsetzung (2026-10-13, Dev-Lauf)

Direkt im Bündel `feature/2026-08-07-ideal-teile-1-5`, Start `d3746373` (v1.56.0; Worktree sauber, keine offenen wip-Commits).
Plan `docs/superpowers/plans/2026-10-13-fa-struktur-arbeitsgaenge-symbole-teil-3.md`. Commits `4c5d4e66` (Regel/Darstellung,
Terminal-Funktion, API, Tests), `d4f326ef` (feature-complete: JS/CSS/View, v1.57.0, Hilfe, Changelog, TS-90). **Version 1.57.0,
TS-90.** Keine Migration. Aufgabe [[2026-10-13-fa-struktur-arbeitsgaenge-symbole-teil-3-umsetzung]].

**Code-Einstieg:**
- `Services/ArbeitsgangStatusRegel.cs`: `ArbeitsgangStatusRegel.Ableiten` (Vorrang FA 7a) und `.Soll` (Offset-Lookup getrimmt,
  ohne Eintrag 0) — **einziger Ort** der Status-Regel (AK 13). Teil 2 (`FaArbeitsgangStatus.Zuordnen`) ruft sie jetzt ebenfalls
  (Refactor ohne Verhaltensänderung, 25 Teil-2-Tabellentests unverändert grün).
- Gleiche Datei: `BdeTerminalAuftraege.Fertigungstermin` (FA 3: frühester nicht leerer FE_Termin der Kopfzeilen zur HauptFA =
  `OrderNumber`, sonst `ProductionDate`) und `.Ordnen` (Status je WorkOperation ohne W-4, Sortierung FA 4).
- `FaAgStatusDarstellung.Fuer` (`Models/ViewModels/FaArbeitsgangStatus.cs`): CSS-Klasse + SVG-Zeichen, aus dem Teil-2-Partial
  herausgezogen, dieselbe Quelle für FA-Baum und Terminal (`<template>`-Vorlagen `#bdeAgZeichen` in `Views/BdeTerminal/Index.cshtml`).
- `BdeApiController.GetAvailableOperations`, Normalmodus: `activeWoIds` nicht mehr ausgeschlossen, sondern `running` (Antwort 6);
  `myActiveWoIdSet`/`finishedWoIdSet` unverändert ausgeschlossen. Antwort je Element zusätzlich `productionOrderId`,
  `orderNumber`, `subOrderNumber`, `description`, `productionDate`, `operationNumber`, `operationName`, `status`
  (`open`/`running`/`overdue`), `isOverdue`, `dueDate`, `statusClass`, `ariaLabel`; `id`/`label`/`type` bleiben. NurFA-Zweig ohne Diff.
- `bde-terminal.js` `renderOrderBlocks` (escapeHtml für alle Sage-Texte); NurFA-Schleife unverändert (nur in `if (nurFaMode)`
  gefasst). `bde.css` `.bde-auftrag*`, `.bde-ag-btn` (Bootstrap-Knopfvariablen aus der Teil-2-Palette). `site.css`: Teil-2-Palette
  als CSS-Variablen `--fa-ag-fg/-bg/-bd` (Werte unverändert).

**Rulings:**
- R1: Statuswerte `status` + `isOverdue` (statt Kombi-Wert) wie Lösungsentwurf; zusätzlich `statusClass`/`ariaLabel` vom Server,
  damit Zuordnung Status → Klasse/Zeichen/Text nur in C# lebt (kein JS-Duplikat).
- R2: Sortierung nach dem **Datumsteil** des Termins (Uhrzeiten in `ProductionDate` sollen gleiche Tage nicht trennen), danach
  OrderNumber/SubOrderNumber ordinal, dann `ProductionOrderId` (Blöcke zusammenhängend) und `Sequence`.
- R3: Kopfzeilen/Feiertage/Offsets werden nur abgefragt, wenn das Angebot nicht leer ist (Budget AK 10: höchstens +3).
- R4: Palette als CSS-Variablen statt Kopie der Hex-Werte in `bde.css` — eine Quelle für FA-Baum und Terminal.
- R5: `BdeApiControllerTests` hatte **zehn** direkte Konstruktionen (neun Tests + Helfer `CreateController`), alle nachgezogen
  (nur zusätzliche Argumente, Fixtures unverändert).

**Kontrast:** dieselben Farbpaare wie v1.55.0 (Text ≥ 10,22:1, Zeichen/Rand ≥ 4,86:1, roter Rand auf Läuft-Grund 4,94:1); Knöpfe
setzen Hover-/Aktiv-Variablen auf dieselben Werte (kein Bootstrap-Grün beim Antippen). Auftragsblock: Text auf Kartengrund wie
bisher, Randstreifen `#495057` (8,18:1 auf weiß).

**Bestands-TS geprüft:** Kapitel 8–10, 15 und TS-66 enthalten keine Erwartung „von anderem Werker gebuchter Arbeitsgang fehlt in der
Liste“ (Suche nach „ausgeblendet/verschwindet/nicht in der Liste“) — kein Nachzug nötig.

**Beweis:** `dotnet build IdealAkeWms.slnx` → 0 Fehler; `dotnet test` → Web 1845 bestanden / 1 übersprungen (+14), Service 272.
Neu: `BdeTerminalAuftraegeTests` (11), `BdeApiControllerTests` (+3: Fremdbuchung sichtbar/eigene aus, Termin+überfällig aus
ProductionDate+Stammdaten, NurFA-Ausgabe nur `id/label/type` in ProductionDate-Reihenfolge).

## QA-Nachweis (2026-10-13, qa-agent)

Geprüft im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig `feature/2026-08-07-ideal-teile-1-5`,
Bereich `d3746373..d4f326ef` (2 Commits, 16 Dateien, +575/−58). Web-App nicht gestartet (Auto-Migration gegen AKESQL20).
**Ergebnis: Testbereit** — Build und Tests grün, keine Critical/Important-Befunde, keine Fix-Commits.

### 1. Build und Tests (selbst ausgeführt)

- `dotnet build IdealAkeWms.slnx` → **0 Fehler**, 13 Warnungen (u. a. CS8601 in `BdeTerminalAuftraegeTests.cs:23`, kosmetisch).
- `dotnet test`:
  - `IdealAkeWms.Tests`: **1845 bestanden / 1 übersprungen / 0 fehlgeschlagen** (gesamt 1846).
  - `IDEALAKEWMSService.Tests`: **272 bestanden / 0 übersprungen / 0 fehlgeschlagen**.

### 2. Code-Review (selbst, in diesem Lauf)

Gelesen: kompletter Diff plus Umfeld — `BdeApiController.GetAvailableOperations` (ganze Methode inkl. NurFA-Zweig),
`Services/ArbeitsgangStatusRegel.cs` (neu), `Models/ViewModels/FaArbeitsgangStatus.cs` (Teil-2-Refactor + `FaAgStatusDarstellung`),
`FaHierarchyTreeViewModel.cs:227-246` (`StatusText`/`AriaLabel`), `_FaHierarchyNode.cshtml`, `Views/BdeTerminal/Index.cshtml`,
`bde-terminal.js` (`renderOperationButtons`, `renderOrderBlocks`, `escapeHtml` `:279-283`), `bde.css`, `site.css`,
`FaHierarchyOrderInfoRepository.GetByHauptFaKeysAsync` (leere Schlüsselliste → keine Abfrage), `CachedHolidayRepository`,
`Program.cs:90/109/126/151` (DI aller vier neuen Ctor-Abhängigkeiten vorhanden), `WorkOperation.cs:13/18` (nicht nullable),
`WorkOperationRepository` (`Include(ProductionOrder)`), Help/Changelog, `docs/TESTSZENARIEN.md`.

Geprüfte Punkte:
- **Antwort 6:** `angebot` schließt nur noch `finishedWoIdSet` und `myActiveWoIdSet` aus; `activeWoIds` geht als „läuft“ in die Regel
  (`BdeApiController.cs`, Normalmodus). `BdeBookingService.cs` nicht im Diff → Startprüfung unverändert. Test
  `GetAvailableOperations_FremdeLaufendeBuchung_SichtbarAlsLaeuft_EigeneAusgeblendet_AK2`: A sieht `running`, B sieht ihn nicht.
- **NurFA (AK 8):** API-Zweig ohne Diff-Hunk; JS: alte Schleife zeilengleich, nur in `if (nurFaMode) { … } else { renderOrderBlocks }`
  gefasst → funktional byte-gleich. Test `…NurFa_AusgabeUnveraendert_OhneStatus_AK8` prüft Feldmenge `id/label/type` + Reihenfolge.
- **Termin-Regel FA 3:** `BdeTerminalAuftraege.Fertigungstermin` = `Min()` über `FE_Termin?` der Kopfzeilen zur HauptFA (ignoriert null;
  keine/alle leer → null) `?? ProductionDate`; ohne Datum → „ohne Termin“. Sortierung FA 4: ohne Termin zuletzt, Datumsteil, OrderNumber,
  SubOrderNumber (ordinal), ProductionOrderId (Blöcke zusammenhängend), Sequence. Status inkl. läuft+überfällig über
  `ArbeitsgangStatusRegel.Ableiten`; Offset ohne Eintrag 0 (`Soll`; `out ohneEintrag` am Terminal verworfen → **kein Hinweis**, AK 14).
- **AK 13 (ein Ort):** Status-Ableitung nur in `ArbeitsgangStatusRegel.Ableiten` (Grep `FaAgStatus.LaeuftUeberfaellig`: sonst nur
  Mapping/Darstellung/Text, keine Regel). Teil-2-Refactor äquivalent: `kuerzel` war schon getrimmt (`sym.Kuerzel.Trim()`), Vorrang
  erledigt → läuft → überfällig → offen identisch (alle erledigt ⇒ kein „nicht erledigt + läuft“). `FaArbeitsgangStatusTests.cs`:
  `git diff d3746373..HEAD` leer, Tests grün.
- **AK 11 Escaping:** alle Sage-Texte (`orderNumber`, `subOrderNumber`, `description`, `operationNumber`, `operationName`) sowie
  `ariaLabel`/`statusClass` über `escapeHtml` (auch `"`/`'`, attribut-sicher); unescaped nur `op.id` (int) und der aus `yyyy-MM-dd`
  gebildete Termin. `@Html.Raw` nur für die konstanten SVG-Strings aus `FaAgStatusDarstellung`. Kein Wert in einem `<script>`-Block.
- **`hidden` + `d-*`:** `#bdeAgZeichen` trägt `hidden` ohne `d-*`-Klasse — unkritisch (Inhalt sind inerte `<template>`s).
- **CSS:** site.css-Palette als Variablen mit identischen Werten (je Status fg/bg/bd 1:1 gegen die alten `color`/`background-color`/
  `border-color`/`.fa-ag-sign`-Farben verglichen); Zeichenfarbe `var(--fa-ag-bd, currentColor)` = vorher je Status gesetzte Farbe;
  `.fa-ag-laeuft-ueberfaellig { border … #b02a37 }` steht weiter nach der Sammelregel → FA-Baum gleich. `bde.css .bde-ag-btn` setzt
  `--bs-btn-*`, `-hover-*`, `-active-*` auf dieselben Werte (kein Bootstrap-Grün beim Antippen); läuft+überfällig 3px roter Rand inkl.
  Hover/Aktiv. Knopfhöhe `.bde-terminal .btn-lg { min-height: 60px }` (`bde.css:1`) → TS-90.8 stimmt.
- **Abfragebudget AK 10:** nur bei nicht leerem Angebot drei Aufrufe — Kopfzeilen (1 Abfrage, entfällt ohne numerische OrderNumber),
  Feiertage (`CachedHolidayRepository`), Offsets (`CachedOperationConfigRepository`); „läuft“ aus der bestehenden `activeWoIds`-Menge.
  ≤ +3 per Code belegt, nicht gemessen.
- **Keine Migration/SQL:** kein Pfad unter `*/Migrations/` oder `SQL/` im Diff. Rollen/Gates: Klassenattribute ohne Diff.
- Stille Rückfälle „Offset ohne Eintrag = 0“ und „kein FE_Termin → ProductionDate“ sind spec-gewollt (Antwort 7/9); gemeldet wird
  zentral (Stammdaten „nicht konfiguriert“, Teil 2). Keine parallelen Arrays/Model-Binding, keine Nur-Client-Validierung (Lesesicht).

**Befunde:**

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| M1 | Minor | AKE-`OrderNumber`s sind ggf. numerisch → die Kopfzeilen-Abfrage läuft auch an der AKE; trüge die AKE-DB wider Erwarten `FaHierarchyOrderInfos`-Zeilen, ginge deren `FE_Termin` vor `ProductionDate` (Regel unterscheidet über Daten, wie FA 3 verlangt). | Kein Code-Fix (spec-konform). Prüfabfrage in Checkliste Schritt 1. |
| M2 | Minor | `label` wird im Normalmodus weiter serverseitig unescaped gebaut (Rückwärtskompatibilität); das JS rendert es im Normalmodus nicht mehr. | Kein Fix; Hinweis für künftige Aufrufer. |
| M3 | Minor | AK 10 nur per Code-Review belegt, keine Query-Zählung im Test. | Manual-UAT TS-90.10 (SQL-Profiler). |
| M4 | Minor | Build-Warnung CS8601 in `BdeTerminalAuftraegeTests.cs:23` (nullable `sub`). | Kosmetisch, offen gelassen. |

Keine Critical-, keine Important-Befunde.

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweis |
|---|---|
| 1 Gruppierung/Kopf | API-Felder + Reihenfolge automatisch; Rendering = **Manual-UAT** TS-90.1 |
| 2 Bestand + fremd „läuft“ | Automatisch (`…FremdeLaufendeBuchung…_AK2`; bestehende Ausschluss-Tests grün) |
| 3 Sortierung | Automatisch (`Sortierung_TerminAufsteigend_…`) |
| 4 Termin-Regel | Automatisch (`Termin_EineKopfzeile_…`, `Termin_OhneKopfzeile_Ake_…`) |
| 5 Farbe+Zeichen+aria-label, Graustufen, Kontrast | `ariaLabel`-Format automatisch (`AriaLabel_KuerzelNameStatus_AK5`); Darstellung/Kontrast = **Manual-UAT** TS-90.8 |
| 6 Überfällig/Soll | Automatisch (`Status_SollTerminPlusOffsetWerktage_Feiertag_AK6`, `…AusProductionDateUndStammdaten_AK6_AK12`) |
| 7 Klick/Kollision | **Manual-UAT** TS-90.3/.5 (Browser-JS + Buchungsablauf) |
| 8 NurFA unverändert | API automatisch (`…NurFa_AusgabeUnveraendert…_AK8`); JS = Code-Review + **Manual-UAT** TS-90.9 |
| 9 Zugriff/Gates | Attribute ohne Diff; bestehender Test „Werkbank inaktiv → leer“; Rest **Manual-UAT** TS-90.11 |
| 10 Abfragebudget | Code-Review; **Manual-UAT** TS-90.10 |
| 11 Escaping | Code-Review; **Manual-UAT** TS-90.11 (Browser) |
| 12 AKE | Logik automatisch (AKE-Fall ohne Kopfzeile); echte AKE-DB = **Manual-UAT** TS-90.7 |
| 13 Ein Ort, kein W-4 | Automatisch (`Regel_ErledigtVorLaeuft_EinzigerOrt`, `ZweiArbeitsgaengeGleichesKuerzel_…_AK13`, Teil-2-Tests unverändert grün) + Grep |
| 14 Kein Offset-Hinweis, Offset 0 | Automatisch (`Status_OhneOffsetEintrag_…_AK14`); „kein Hinweis“ per Code-Review |

### 4. Geänderte bestehende Tests

- `IdealAkeWms.Tests/Controllers/BdeApiControllerTests.cs`: Helfer `CreateController` + 9 direkte Konstruktionen — **nur Typ/Compile**
  (vier zusätzliche Ctor-Argumente), keine Fixture- oder Assertion-Änderung. Neu angehängt: 3 Tests + Helfer `Ctrl`/`Productive`.
- `FaArbeitsgangStatusTests.cs`: **unverändert** (leerer Diff). Keine Kategorie „Sonstiges“.

### 5. CLAUDE.md-Checkliste

- Migration/SQL/FreshInstall/AgentJobs: entfällt. Audit-Felder: entfällt (Lesesicht). Neue Rolle/Filter: keine.
- Version 1.57.0 in **beiden** `AppVersion.cs`; Anwender-Changelog v1.57.0; Hilfe: bestehender BDE-Terminal-Schritt „Arbeitsgang
  waehlen“ ergänzt (`Views/Help/Index.cshtml:1066`), nicht gedoppelt.
- `docs/TESTSZENARIEN.md`: TS-90.1–90.11 inkl. Vorab P-T1 (mit AKE-Kollisionsabfrage), P-T2, Vorbedingung `BdeAktiv = true`;
  `git show HEAD:docs/TESTSZENARIEN.md` enthält **0 CR** (LF). Index `secondbrain/tests/testszenarien-index.md:116` hat Zeile 90.
- Brain: Changelog [[2026-10-13-v1-57-0-bde-terminal-arbeitsgang-status]], `feature-map.md:782` vorhanden.

## Manuelle Test-Checkliste (Schranke 2)

1. **Vorab DB-Prüfungen** (kein UI): P-T1 auf der AKE-WMS-DB, P-T1-Kollision (`OperationConfigs` ↔ `WorkOperations`; Treffer melden),
   P-T2 auf der IDEAL-WMS-DB. Zusätzlich (Befund M1) auf der AKE-WMS-DB: `SELECT COUNT(*) FROM dbo.FaHierarchyOrderInfos;` — erwartet 0;
   sonst melden.
2. Publish aus dem Worktree (nur Web, Befehl im Abschnitt Deploy); Testsystem mit `SQL/95` gelaufen, `BdeAktiv = true`
   (freigabepflichtig), Werkbank BDE-aktiv, Offsets gepflegt (ein Kürzel ≠ 0, eines ohne Eintrag), Feiertage gepflegt.
3. TS-90.1 Blöcke je Auftrag; Reihenfolge vergangener Termin → künftiger → „ohne Termin“; je Block Sequenz-Reihenfolge.
4. TS-90.2 Überfällig rot/Dreieck, offen grau/Kreis; Kürzel ohne Offset-Eintrag: Soll = Termin, kein Hinweis.
5. TS-90.3 Werker B startet → bei A blau/Sanduhr „läuft“, bei B unter „Aktuelle Buchungen“; A: Rüsten → Kollisions-Modal; Produktion →
   Modal bzw. zweite Buchung je `BdeMehrfachBuchungProArbeitsgang`; B pausiert → bei A wieder offen/überfällig.
6. TS-90.4 Läuft + überfällig: Sanduhr + Dreieck, roter Rand.
7. TS-90.5 Bedienung, Scan „FA-Nr,AG-Nr“, Sub-FA-Auswahl unverändert; letzter AG fertig → Auftrag verschwindet.
8. TS-90.6 IDEAL-Termin: eine Kopfzeile, Kombigerät (frühester), Kopfzeile ohne FE_Termin → ProductionDate; Gegenprobe `/FaHierarchy`.
9. TS-90.7 AKE-Testsystem (bzw. TS-90.9 bei NurFA).
10. TS-90.8 Graustufen, Screenreader, Kontrast am Terminal, auch beim Antippen (keine grüne Bootstrap-Farbe).
11. TS-90.9 NurFA an → Liste wie vor v1.57.0.
12. TS-90.10 ≥ 100 AGs: Ladezeit, Umbruch; SQL-Profiler ≤ 3 Zusatzabfragen.
13. TS-90.11 Negativfälle inkl. `<b>x</b>` in der Bezeichnung → als Text.
14. Regression FA-Baum (`/FaHierarchy`, TS-88): Statusfarben/-zeichen unverändert (site.css auf Variablen umgestellt).
