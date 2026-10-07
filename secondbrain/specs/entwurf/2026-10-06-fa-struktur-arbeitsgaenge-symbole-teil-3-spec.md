---
type: spec
title: "FA-Struktur Arbeitsgänge als Symbole — Teil 3: Offene Aufträge mit Arbeitsgang-Status am BDE-Terminal"
slug: 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-07
source_backlog: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]"
depends_on: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Controllers/BdeApiController.cs — GetAvailableOperations (Z.193-286), Normalmodus (Z.244-285): Auswahlmenge unveraendert (Z.245-273, ausser Rueckfrage 6), Antwort zusaetzlich je Arbeitsgang: Auftrag (ProductionOrderId, OrderNumber, SubOrderNumber, Description1), Fertigungstermin, OperationNumber, Name, Status offen/laeuft/ueberfaellig + Soll-Termin; Sortierung nach Fertigungstermin. NurFA-Zweig (Z.205-241) laut Rueckfrage 8"
  - "IdealAkeWms/Data/Repositories/IFaHierarchyOrderInfoRepository.cs — nur nutzen: GetByHauptFaKeysAsync (Z.25) fuer FE_Termin der HauptFAs (eine Abfrage; bei AKE leer)"
  - "IdealAkeWms/Services/OseonDueDateCalculator.cs + IHolidayRepository + Offset-Quelle aus Teil 2 (Teil-2-Rueckfrage 8) — nur nutzen, Soll-Termin je Arbeitsgang"
  - "Status-Ableitung aus Teil 2 (reine Funktion) — wiederverwenden, nicht duplizieren; liegt sie nach Teil 2 im FaHierarchy-ViewModel, in eine gemeinsame statische Klasse unter IdealAkeWms/Services/ ziehen"
  - "Neue reine Funktion (z. B. IdealAkeWms/Services/BdeOrderGrouping.cs oder im Controller als static): Gruppierung je ProductionOrder + Sortierung + Fertigungstermin-Regel, unit-testbar"
  - "IdealAkeWms/wwwroot/js/bde-terminal.js — renderOperationButtons (Z.442-478): je Auftrag ein Block mit Kopf (Auftrag, Bezeichnung, Fertigungstermin) und den AG-Knoepfen mit Status-Zeichen; Werte per textContent/Escaping einsetzen (heute unescaped, Z.453/Z.455); bindOperationButtonHandlers (Z.480-513) bleibt fuer data-wo-id"
  - "IdealAkeWms/Views/BdeTerminal/Index.cshtml — Karte '2. Arbeitsgang waehlen' (Z.60-75): Struktur bleibt, ggf. Ueberschrift/Hinweistext; Scan-Zeile (Z.65-73) unveraendert"
  - "IdealAkeWms/wwwroot/css/bde.css — Auftragsblock und Status-Stile (WCAG AA, Touch-Groesse wie btn-lg)"
  - "IdealAkeWms.Tests — Gruppierung/Sortierung/Termin-Regel (Unit), GetAvailableOperations (InMemory: Gruppen, Sortierung, Status, Ausschluesse unveraendert)"
  - "docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md — TS-Kapitel naechste freie Nummer zum Umsetzungszeitpunkt"
beantwortete_rueckfragen:
  - "1 Ansicht -> am BDE-Terminal je Werkbank die offenen Auftraege, Anknuepfung Schritt 'Arbeitsgang waehlen' (Views/BdeTerminal/Index.cshtml:61) mit der dortigen Logik (Keyuser 2026-10-07, Freigabe-Block Nr. 1)"
  - "2 Rolle -> bestehende BDE-Rollen, keine neue Rolle (Gerald 2026-10-07, Freigabe-Block Nr. 2)"
  - "3 Auftragsbestand -> alle Auftraege mit mindestens einem offenen Arbeitsgang an dieser Werkbank, sortiert nach naechstem Fertigungstermin (naechster zuerst), kein Zeitfenster (Gerald 2026-10-07, Freigabe-Block Nr. 3)"
  - "4 Status -> farbig mit dem Status aus Teil 2 (ueberfaellig/offen/laeuft, mit Zeichen zusaetzlich zur Farbe) (Gerald 2026-10-07, Freigabe-Block Nr. 4) -> depends_on Teil 2"
  - "5 Standort -> nicht standortspezifisch, AKE und IDEAL (Gerald 2026-10-07, Freigabe-Block Nr. 5)"
open_questions:
  - "(Freigabe-Block Nr. 6, neu 2026-10-07) Status 'laeuft' vs. heutige Auswahl: Das Terminal blendet heute jeden Arbeitsgang aus, auf dem irgendjemand eine offene Buchung hat (BdeApiController.cs:246-249, :273) — 'laeuft' kaeme in der Liste also nie vor. Sollen Arbeitsgaenge, die ein ANDERER Werker gerade bucht, kuenftig mit Status 'laeuft' sichtbar sein (Start dann ueber die bestehende Kollisionspruefung, BdeBookingService.cs:51-63), oder bleiben sie ausgeblendet?"
  - "(Freigabe-Block Nr. 7, neu 2026-10-07) AKE: Teil 2 setzt fuer IDEAL FE_Termin als Bezug. Gilt bei AKE (keine FA-Struktur-Kopfdaten) ProductionOrder.ProductionDate (Sage-Spalte 'Fertigungstermin') als Fertigungstermin fuer Sortierung UND Ueberfaellig-Bezug, und gelten fuer AKE-Arbeitsgaenge dieselben Offset-Regeln wie in Teil-2-Rueckfrage 8?"
  - "(Freigabe-Block Nr. 8, neu 2026-10-07) NurFA-Modus (AppSetting BdeNurFaMeldung): Dort zeigt das Terminal Auftraege statt Arbeitsgaenge, bereits nach ProductionDate sortiert (BdeApiController.cs:205-241). Bleibt er unveraendert, oder bekommt er dieselbe Termin-Regel/Status je Auftrag?"
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

> [!note] Kein Platzhalter mehr (Rework 2026-10-07)
> Die Rückfragen 1-5 sind beantwortet (Keyuser/Gerald 2026-10-07); dieser Teil ist jetzt ausspezifiziert.
> Ursprünglicher Hinweis: „Platzhalter — erst spezifizieren, wenn die Rückfragen beantwortet sind“ (überholt).

> **Umsetzungsort (Hinweis, keine Entscheidung):** nach Schranke 2 des IDEAL-Bündels, aus `main`
> (neuer Worktree), **nach** Teil 2. Code-Grundlage: Bündel-Stand `.claude/worktrees/2026-08-07-ideal-teile-1-5`
> (v1.47.4); alle Zeilenangaben beziehen sich darauf und sind nach dem Merge gegen `main` nachzuprüfen.
> TS-Nummer und Version: nächste freie zum Umsetzungszeitpunkt. `worktree`/`branch` setzt der Dev-Lauf.
>
> **depends_on Teil 2** (Antwort 4: „farbig mit dem Status aus Teil 2“): Teil 3 nutzt die Status-Ableitung,
> die Soll-Termin-Berechnung und die Status-Codierung (Farbe + Zeichen + `aria-label`) aus
> [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]]; Teil 2 setzt Teil 1 voraus. ~~depends_on
> Teil 1, Teil 2 nicht als Abhängigkeit~~ — überholt (Antwort 4). Die offenen Teil-2-Rückfragen 8 (Offsets) und 9
> (Kombigeräte) wirken hier mit; ihre Antworten gelten unverändert auch am Terminal.

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
  Terminal damit immer erfüllt.
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
  Fertigungstermin, Status je Arbeitsgang (offen / läuft / überfällig) mit Farbe + Zeichen + `aria-label`.
- Erweiterung der bestehenden API-Antwort und des bestehenden Renderings — **kein** neuer Endpunkt, **keine**
  neue View, **keine** neue Rolle.
- Beide Standorte (AKE, IDEAL).

**Out-of-Scope**
- Auswahlmenge der Arbeitsgänge (welche erscheinen) — unverändert, **außer** Rückfrage 6 („läuft“).
- NurFA-Modus — laut Rückfrage 8 (Arbeitsannahme: unverändert).
- Scan-Weg (`/api/bde/resolve-scan`, `BdeScanResolver`), Buchungslogik, Kollisionsprüfung, BDE-Cockpit,
  Tageshistorie, Aktive Buchungen.
- Status „erledigt“: erledigte Arbeitsgänge stehen heute nicht in der Liste (`BdeApiController.cs:252-257`,
  `:273`) und bleiben draußen (Antwort 3: „offener Arbeitsgang“; Antwort 4 nennt nur
  überfällig/offen/läuft).
- Arbeitsgänge **anderer** Werkbänke desselben Auftrags (Antwort 3: „an dieser Werkbank“).
- Datenmodell, Offsets-Pflege (kommt aus Teil 2).

## Fachliche Anforderungen

1. **Bestand (Antwort 3):** Ein Auftrag erscheint, wenn er mindestens einen Arbeitsgang hat, den das Terminal
   heute an dieser Werkbank anbietet (Auswahlmenge wie `BdeApiController.cs:245-273`). Kein Zeitfenster, keine
   Paginierung (Bestand heute ebenfalls ungepaged).
2. **Auftrag = `ProductionOrder`** des Arbeitsgangs (`WorkOperation.ProductionOrderId`). Kopf: `OrderNumber`,
   bei abweichender `SubOrderNumber` zusätzlich diese (IDEAL: HauptFA / Sub-FA — nötig, weil mehrere Sub-FAs
   derselben HauptFA an einer Werkbank sonst gleich aussehen, vgl. W-1 in Teil 2), `Description1`,
   Fertigungstermin (TT.MM.JJJJ bzw. „ohne Termin“).
3. **Fertigungstermin je Auftrag** (gleiche Regel wie die FA-Liste, `ProductionOrdersController.cs:199`,
   `:294-313`): genau eine `FaHierarchyOrderInfo`-Zeile zur HauptFA (`int.TryParse(OrderNumber)` →
   `HauptFA`, Muster `:271-275`) → deren `FE_Termin`; sonst `ProductionOrder.ProductionDate`. Bei AKE gibt es
   keine Kopfzeilen → immer `ProductionDate` (Bestätigung Rückfrage 7). Kombigerät (mehrere Kopfzeilen):
   Regel aus Teil-2-Rückfrage 9; bis dahin wie FA-Liste (`ProductionDate`, bei IDEAL meist leer → „ohne Termin“).
4. **Sortierung (Antwort 3):** Aufträge aufsteigend nach Fertigungstermin (vergangene/überfällige oben, je
   weiter in der Zukunft desto weiter unten), Aufträge **ohne** Termin am Ende; Gleichstand nach `OrderNumber`,
   dann `SubOrderNumber`. Innerhalb eines Auftrags die Arbeitsgänge nach `Sequence` (wie heute).
5. **Status je Arbeitsgang (Antwort 4, Logik aus Teil 2):**
   - **läuft:** offene Buchung (`EndedAt == null && !IsCancelled`) eines anderen Werkers — nur wenn Rückfrage 6
     sie sichtbar macht; die Menge liegt bereits vor (`activeWoIds`, `BdeApiController.cs:246-249`), keine neue
     Abfrage.
   - **überfällig:** Soll-Termin < heute; Soll-Termin = `OseonDueDateCalculator.Calculate(Fertigungstermin,
     Offset(OperationNumber), …)` (Teil 2 FA 4; Offset-Quelle/Default laut Teil-2-Rückfrage 8; AKE laut
     Rückfrage 7). Vorrang läuft/überfällig laut Teil-2-Rückfrage 10.
   - **offen:** sonst.
   - Ohne Fertigungstermin: nie überfällig.
6. **Codierung (Teil-2-Antwort 1):** gleiche Farben, Zeichen und `aria-label`-Texte wie Teil 2 FA 7
   (Warndreieck / leerer Kreis / Läuft-Zeichen); sichtbar am Knopf: `OperationNumber` und `Name`. Farbe nie
   allein; Kontrast WCAG AA; Knopfgröße wie heute (`btn-lg`, Touch).
7. **Bedienung unverändert:** Klick auf einen Arbeitsgang-Knopf startet denselben Ablauf wie heute
   (`data-wo-id` → `currentWorkOp`, `bde-terminal.js:496-499`). Ungeplante Tätigkeiten und Leermeldungen wie
   heute (`:461-474`).
8. **Sicherheit:** Auftrags- und Artikeltexte (Sage-Daten) werden escaped bzw. per `textContent` gesetzt;
   die bestehende ungeescapte Label-Verkettung (`:453`, `:455`) wird dabei für den Normalmodus ersetzt.

## Technischer Loesungsentwurf

Leiter: vorhandener Endpunkt, vorhandene Mengen, vorhandener Calculator — nur Anreicherung, Sortierung und
Rendering sind neu.

- **API (`GetAvailableOperations`, Normalmodus):** Auswahlmenge unverändert. Zusätzlich je Seitenaufruf:
  1. `IFaHierarchyOrderInfoRepository.GetByHauptFaKeysAsync` über die numerischen `OrderNumber`s der Treffer
     (eine Abfrage; bei AKE leer, kein Gate nötig).
  2. Feiertage (`IHolidayRepository.GetHolidayDatesAsync`) und Offsets (Quelle laut Teil-2-Rückfrage 8; bei
     Variante A gecacht) — je eine Abfrage.
  3. Reine Funktion: Fertigungstermin je Auftrag (FA 3) → Soll-Termin je Arbeitsgang → Status (Teil-2-Funktion)
     → Gruppierung + Sortierung (FA 4).
  - Antwortform rückwärtskompatibel erweitern: `productive` bleibt eine Liste, jedes Element bekommt
    zusätzlich `productionOrderId`, `orderNumber`, `subOrderNumber`, `description`, `productionDate`,
    `operationNumber`, `operationName`, `status`, `dueDate`; Reihenfolge = Sortierung FA 4. Das JS gruppiert
    nach `productionOrderId` (Reihenfolge bleibt erhalten). `label` bleibt für Bestandsaufrufer.
  - Abhängigkeiten neu im Ctor: `IFaHierarchyOrderInfoRepository`, `IHolidayRepository`, `IBusinessDayService`,
    ggf. `IOseonOperationConfigRepository` (Teil-2-Rückfrage 8).
- **JS (`renderOperationButtons`):** Normalmodus: je Auftrag ein Block (Kopf FA 2 + Knopfreihe), Knöpfe mit
  Status-Zeichen und `aria-label`; per DOM-API/`textContent` statt String-Verkettung. NurFA- und
  Aktivitäts-Zweig unverändert. `bindOperationButtonHandlers` bleibt (Selektor `#operationButtons .bde-op-btn`).
- **CSS (`bde.css`):** Auftragsblock, Status-Varianten; Stil-Bausteine aus Teil 2 wiederverwenden, keine neue
  UI-/Icon-Bibliothek. **frontend-design-Skill ist im Dev-Lauf Pflicht.**
- **Rückfrage 6 = sichtbar:** `activeWoIds` nicht mehr ausschließen, sondern als „läuft“ markieren; eigene
  laufende Buchungen (`myActiveWoIdSet`) bleiben ausgeschlossen (stehen unter „Aktuelle Buchungen“). Start auf
  einen laufenden Arbeitsgang prüft der Service wie heute (`BdeBookingService.cs:51-63`: Rüsten immer exklusiv,
  Produktion nur bei `BdeMehrfachBuchungProArbeitsgang`).

## Migrations-/SQL-Auswirkungen

Keine eigene. Keine Migration, kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt.
Falls Teil-2-Rückfrage 8 Variante B (Feld an der Werkbank) wählt, bringt Teil 2 die Migration; Teil 3 liest
das Feld nur.

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
2. Es erscheinen genau die Arbeitsgänge, die das Terminal heute anbietet (Ausschlüsse `IsReported`,
   verpackt/abgeholt, `IsFinal`, eigene laufende Buchung unverändert) — Ausnahme laut Rückfrage 6 für
   Arbeitsgänge mit laufender Buchung eines anderen Werkers. Ein Auftrag ohne solchen Arbeitsgang erscheint nicht.
3. Aufträge sind aufsteigend nach Fertigungstermin sortiert, ohne Termin am Ende; Gleichstand nach
   `OrderNumber`, `SubOrderNumber`; innerhalb nach `Sequence` (Unit-Test der reinen Funktion).
4. Fertigungstermin-Regel per Unit-Test: IDEAL-Auftrag mit genau einer Kopfzeile → `FE_Termin`; ohne Kopfzeile
   → `ProductionDate`; AKE-Auftrag (keine Kopfzeile) → `ProductionDate`; Kombigerät laut Teil-2-Rückfrage 9.
5. Jeder Arbeitsgang-Knopf zeigt Kürzel + Name und den Status offen / läuft / überfällig mit Farbe **und**
   Zeichen **und** `aria-label` (z. B. „KA Kanten – überfällig (Soll 04.05.2026)“); Graustufen-Screenshot
   unterscheidbar; Kontrast ≥ WCAG AA am Terminal gemessen.
6. Überfällig genau dann, wenn Soll-Termin (Fertigungstermin + Offset in Werktagen) < heute; Fertigungstermin
   heute → nicht überfällig; ohne Fertigungstermin nie überfällig.
7. Klick auf einen Arbeitsgang-Knopf verhält sich wie heute (Aktionsknöpfe Rüsten/Produktion erscheinen,
   Buchung startet); Scan „FA-Nr,AG-Nr“ und Disambiguierung unverändert.
8. NurFA-Modus laut Rückfrage 8 (Arbeitsannahme: Ausgabe und Sortierung unverändert).
9. Benutzer ohne BDE-Rolle → `GET /api/bde/available-operations/{id}` verweigert (wie heute); BDE global aus →
   Terminal nicht erreichbar (wie heute); Werkbank nicht BDE-aktiv → leere Liste (wie heute).
10. Abfragebudget: gegenüber heute höchstens **drei** zusätzliche Abfragen je Aufruf (Kopfzeilen, Feiertage,
    Offsets; gecachte zählen nicht), unabhängig von der Zahl der Aufträge (Query-Zählung im Dev-Lauf).
11. Ein `Description1` mit `<b>x</b>` erscheint als Text, nicht als HTML.
12. AKE-Testsystem: Liste funktioniert ohne FA-Struktur-Daten, sortiert nach `ProductionDate`.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Nummer: nächste freie zum Umsetzungszeitpunkt), Index in
`secondbrain/tests/testszenarien-index.md` nachziehen. Bestehende BDE-Terminal-Szenarien auf die geänderte
Darstellung prüfen.

- **Vorbedingungen:** BDE global aktiv, Werkbank `BdeAktiv`, Terminal für den Testbenutzer konfiguriert,
  Benutzer mit `bde_user`; an der Werkbank ≥ 3 Aufträge mit offenen Arbeitsgängen: einer mit Fertigungstermin
  in der Vergangenheit, einer heute + 5 Werktage, einer ohne Termin; Teil 2 und dessen Offsets/Feiertage wie
  dort beschrieben. IDEAL-Testsystem **und** AKE-Testsystem.
- **T1 Gruppierung + Sortierung:** Operator scannen → Aufträge als Blöcke, Reihenfolge: vergangener Termin,
  dann der künftige, dann „ohne Termin“; je Block die Arbeitsgänge dieser Werkbank in Sequenz-Reihenfolge.
- **T2 Status überfällig/offen:** Auftrag mit vergangenem Termin → Arbeitsgang rot + Warndreieck,
  Screenreader „… – überfällig (Soll …)“; künftiger → grau + leerer Kreis.
- **T3 Status läuft:** Zweiter Werker startet einen Arbeitsgang dieser Werkbank → beim ersten Werker nach
  Neuladen der Liste laut Rückfrage 6 (sichtbar als „läuft“ bzw. ausgeblendet); Start darauf → bestehende
  Kollisionsmeldung bzw. Mehrfachbuchung laut Einstellung.
- **T4 Bedienung:** Arbeitsgang antippen → Rüsten/Produktion wie bisher; Abschluss mit Menge → Arbeitsgang
  verschwindet aus der Liste, Auftrag verschwindet, wenn es sein letzter war.
- **T5 IDEAL-Termin:** HauptFA mit genau einer Kopfzeile → angezeigter Termin = `FE_Termin` aus
  `/FaHierarchy`; Kombigerät → laut Teil-2-Rückfrage 9.
- **T6 Viele Aufträge:** Werkbank mit vielen offenen Arbeitsgängen (≥ 100) → Liste lädt ohne spürbare
  Verzögerung, Blöcke umbrechen, kein horizontales Scrollen.
- **T7 AKE:** AKE-Testsystem, gleiche Schritte T1/T2 → Sortierung und Überfällig nach `ProductionDate`
  (Bestätigung Rückfrage 7).
- **T8 Barrierefreiheit:** Graustufen + Screenreader + Kontrastmessung am Fertigungsterminal.
- **Negativ:** Benutzer ohne BDE-Rolle → Zugriff verweigert; Werkbank ohne offene Arbeitsgänge → „Keine
  offenen Arbeitsgänge an dieser Werkbank.“; Auftragstext mit HTML-Zeichen → als Text angezeigt; NurFA-Modus
  eingeschaltet → laut Rückfrage 8 unverändert; Scan eines Auftrags, der nicht in der Liste steht → bestehende
  Meldung des Scan-Wegs.

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
des Normalmodus wären dort ohne Wirkung.

**P-T2 — `ProductionDate` bei IDEAL:** Auf der **IDEAL-WMS-Datenbank**, ob materialisierte Aufträge ein
`ProductionDate` tragen (Fallback der Termin-Regel):

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

**Vorläufig — der QA-Lauf bestätigt gegen den echten Diff.** (Vormals „Vorläufig, ohne Grundlage“ — überholt.)

- **Web-App:** ja (`BdeApiController`, `bde-terminal.js`, `bde.css`, ggf. `Views/BdeTerminal/Index.cshtml`,
  gemeinsame Status-Funktion). Beide Standorte (AKE und IDEAL) erhalten dieselbe Web-Version.
- **Service:** nein.
- **Migration:** nein (eine etwaige Offset-Migration gehört zu Teil 2, Rückfrage 8 B).
- **Reihenfolge:** nach Teil 2 (depends_on). Vor Abnahme P-T1 (AKE) und P-T2 (IDEAL).
- **Publish-Befehle:**

```bash
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

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
6. **„Läuft“ widerspricht der heutigen Auswahl** (neu 2026-10-07, Widerspruch Antwort 4 ↔ Code). Antwort 4
   verlangt den Status „läuft“; das Terminal blendet aber jeden Arbeitsgang mit offener Buchung **irgendeines**
   Werkers aus (`BdeApiController.cs:246-249`, Filter `:273`) — „läuft“ käme in der Liste nie vor. Optionen:
   **A** Arbeitsgänge, die ein anderer Werker bucht, künftig sichtbar mit „läuft“ (Start prüft der Service wie
   heute: Rüsten immer exklusiv, Produktion nur bei `BdeMehrfachBuchungProArbeitsgang`,
   `BdeBookingService.cs:51-63`); **B** weiter ausblenden (dann entfällt „läuft“ am Terminal). Eigene laufende
   Buchungen bleiben in beiden Fällen ausgeblendet (`:260-270`). Empfehlung der Spec: A — sonst ist Antwort 4
   für „läuft“ wirkungslos; die Kollisionsprüfung besteht schon. Entscheidung beim Menschen.
7. **AKE: Fertigungstermin und Überfällig-Bezug** (neu 2026-10-07; Antwort 5 ↔ Teil-2-Nachtrag „FE_Termin“).
   Bei AKE gibt es keine `FaHierarchyOrderInfo`; der Sage-„Fertigungstermin“ steht in
   `ProductionOrder.ProductionDate` (`SageImportService.cs:55`). Gilt er bei AKE als Fertigungstermin für die
   Sortierung **und** als Bezug für „überfällig“? Und gelten für AKE-Arbeitsgänge die Offset-Regeln aus
   Teil-2-Rückfrage 8 (inkl. Default ohne Eintrag)? Welche AKE-Arbeitsgänge es gibt, zeigt P-T1.
8. **NurFA-Modus** (neu 2026-10-07). Ist `BdeNurFaMeldung` an, zeigt das Terminal Aufträge statt Arbeitsgänge,
   bereits nach `ProductionDate` sortiert (`BdeApiController.cs:205-241`), ohne Status. Bleibt das unverändert
   (Arbeitsannahme der Spec, kleinster Eingriff), oder bekommt der NurFA-Modus dieselbe Termin-Regel (FA 3) und
   einen Status je Auftrag?

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
6. „Läuft“ am Terminal — A Arbeitsgänge anderer Werker sichtbar als „läuft“ oder B weiter ausblenden? →
7. AKE — ProductionOrder.ProductionDate als Fertigungstermin für Sortierung und Überfällig-Bezug; Offset-Regeln wie Teil-2-Rückfrage 8? →
8. NurFA-Modus — unverändert lassen oder Termin-Regel + Status je Auftrag? →
