---
type: spec
title: "FA-Struktur Arbeitsgänge als Symbole — Teil 2: Status-Farben bei aktivem BDE"
slug: 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-06
source_backlog: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]"
anhaenge:
  - "[[sage-fa-struktur-arbeitsschritte.png]]"
depends_on: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs + WorkOperationRepository.cs — EINE gebuendelte Status-Abfrage fuer eine Menge WorkOperation-Ids (abgeschlossen = BdeBookingQuantity.IsFinal auf nicht stornierter Buchung, Praedikat wie BdeApiController.cs:252-256; ggf. IsReported je Rueckfrage 3; ggf. laufende Buchung je Rueckfrage 5)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs — WorkOperationSummary (Z.210) um Status erweitern; Ableitung offen/erledigt/ueberfaellig als reine, unit-testbare Funktion"
  - "IdealAkeWms/Controllers/FaHierarchyController.cs — Status-Abfrage dort, wo Teil 1 die Symbole laedt (Teil-1-Variante A: Index Z.52-133; Variante B: WorkOperations Z.139-155); BDE-Aktiv-Gate je Rueckfrage 4 (IAppSettingRepository ist bereits injiziert, Z.29); zusaetzlich die WorkOperations der angezeigten Strukturen laden, weil Teil 1 Quelle S zeigt (Antwort 1 Teil 1) — bei Variante A EINE gebuendelte Abfrage, Muster WorkOperationRepository.cs:120-128"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs — Zuordnung Kuerzel -> WorkOperation als reine, unit-testbare Funktion nach Antwort 6: Zeile mit SubFA != 0 -> eigene WorkOperation (SubOrderNumber == SubFA), sonst die des VaterFA (SubOrderNumber == VaterFA); OrderNumber == HauptFA; OperationNumber == Kuerzel case-insensitiv; kein Treffer -> Zustand 'kein BDE-Arbeitsgang'; WorkOperation ohne Kuerzel wird nicht angezeigt. Widersprueche W-1..W-4 (Fachliche Anforderungen 9) sind vor dem Dev-Lauf zu klaeren (Rueckfrage 7)"
  - "IdealAkeWms/Views/FaHierarchy/_FaWorkOperationsModal.cshtml bzw. _FaHierarchyNode.cshtml — Statusdarstellung am Symbol aus Teil 1 (Form/Icon + Text + Farbe)"
  - "IdealAkeWms/wwwroot/css/site.css — Statusfarben mit WCAG-AA-Kontrast"
  - "IdealAkeWms.Tests — Status-Ableitung (Tabellentest), Repository-Abfrage (InMemory), ToQueryString-Test falls Contains ueber viele Ids"
  - "docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md — TS-Kapitel naechste freie Nummer zum Umsetzungszeitpunkt"
open_questions:
  - "Farbe nie alleiniger Bedeutungstraeger (Pflicht-Leitplanke): WIE genau wird der Status zusaetzlich codiert — Form/Icon je Status und Text (Tooltip, Screenreader)? Empfehlung der Notiz: Form/Icon und Text zusaetzlich zur Farbe, Kontrast WCAG AA."
  - "Ueberfaellig-Definition: Es gibt kein Soll-Datum je Arbeitsgang. Welcher Auftragstermin gilt (Empfehlung der Notiz: z. B. FE_Termin ueberschritten und Arbeitsgang nicht rueckgemeldet; Kandidat laut View-Doku auch Neuer_PT_PPS)? Und welche Zeile gilt bei Kombigeraeten mit mehreren Kopfzeilen je HauptFA?"
  - "Was heisst 'erledigt' (gruen)? Am Code gibt es zwei getrennte Signale: das BDE-Terminal wertet eine Abschlussmeldung (BdeBookingQuantity.IsFinal auf nicht stornierter Buchung) aus, WorkOperation.IsReported setzt nur die manuelle Rueckmeldung im Tracking. Nur IsFinal, nur IsReported oder eines von beiden?"
  - "Was heisst 'bei aktivem BDE'? Globaler AppSetting-Schalter BdeAktiv oder je Werkbank ProductionWorkplace.BdeAktiv (oder beides)? Was zeigen die Symbole, wenn BDE nicht aktiv ist — neutral wie Teil 1?"
  - "Ein Arbeitsgang mit laufender (gestarteter, nicht abgeschlossener) Buchung: grau wie offen, oder ein eigener vierter Zustand 'laeuft'?"
  - "(Neu, Rework 2026-10-06, Freigabe-Block Nr. 7) Widersprueche zwischen Antwort 6 und dem Erkennungs-Code (Fachliche Anforderungen 9): W-1 Kuerzel einer Sub-FA-Zeile, die Kind einer Sub-FA ist, erzeugt ZWEI WorkOperations (eigene + Vater, WorkOperationStructureDetectionService.cs:101-102) — Regel a zeigt nur die eigene, die des Vaters bleibt ohne Symbol; W-2 'materialisiert' ist nicht gleich 'SubFA != 0' — Fallback auf den Vater, wenn die eigene WorkOperation fehlt?; W-3 Waisen-Blatt (VaterFA ohne importierten Knoten) trifft ggf. eine alte WorkOperation, die nicht aus diesem Kuerzel stammt; W-4 Mehrfach-Treffer je Auftrag+Kuerzel moeglich (kein Unique-Index, Re-Add-Sperre case-sensitiv) — welcher Status gilt?"
beantwortete_rueckfragen:
  - "6 Zuordnung Kuerzel -> WorkOperation (Teil 1 = Quelle S) -> a) Zeile mit SubFA != 0: eigener Arbeitsgang mit gleicher OperationNumber, sonst der des VaterFA; b) Kuerzel ohne WorkOperation: anzeigen, neutral, ohne Status, Hinweis 'kein BDE-Arbeitsgang' (Text/aria-label, nicht nur Farbe); c) WorkOperation ohne Kuerzel: nicht als Symbol zeigen (Gerald 2026-10-06, Freigabe-Block Nr. 6)"
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
> Symbole, die hier eingefärbt werden). Wo die Status-Abfrage läuft, folgt aus Rückfrage 2 von Teil 1
> (Ort A/B). Teil 1 zeigt **Quelle S** (Sage-Kürzel je Baumzeile, Teil-1-Antwort 1, 2026-10-06); daher ist
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
- Status je Arbeitsgang ableiten (live, nicht gespeichert) und am Symbol aus Teil 1 darstellen.
- Statuscodierung über Farbe **und** Form/Icon **und** Text (Rückfrage 1).
- Gate „BDE aktiv“ (Rückfrage 4).

**Out-of-Scope**
- Kein Soll-Datum je Arbeitsgang einführen (kein Datenmodell-Umbau; „überfällig“ wird aus dem
  Auftragstermin abgeleitet, Rückfrage 2).
- Keine Änderung an BDE-Buchungslogik, Terminal, Tracking-Rückmeldung.
- BDE-Auftragsanzeige → Teil 3.

## Fachliche Anforderungen

1. **Status ist abgeleitet, nicht gespeichert.** Die Quellen können sich nachträglich ändern:
   Buchungskorrekturen setzen `IsFinal` um (`BdeBookingsController.cs:236`, `q.IsFinal = row.IsFinal`),
   Buchungen können storniert werden (`IsCancelled`), die Tracking-Rückmeldung ist umkehrbar
   (`TrackingController.cs:283-286`, `UndoReport`). Ein gespeicherter Status würde veralten.
2. **„Erledigt“ — Befund am Code (entscheidet Rückfrage 3):**
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
3. **„Offen“ (grau):** nicht erledigt und nicht überfällig. Laufende Buchung: Rückfrage 5.
4. **„Überfällig“ (rot):** nicht erledigt **und** der in Rückfrage 2 festgelegte Auftragstermin liegt vor
   heute (Lokaldatum, nur Datumsteil — die Sage-Spalten sind `date`, siehe unten).
5. **Termine — Befund an der Quelle (entscheidet Rückfrage 2):**
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
6. **Gate „BDE aktiv“ (Rückfrage 4) — Befund:** Es gibt zwei Schalter. Global `AppSettingKeys.BdeAktiv`
   (`AppSettingKeys.cs:9`; gelesen von `RequireBdeActiveAttribute.cs:24` und `HomeController.cs:36`) und je
   Werkbank `ProductionWorkplace.BdeAktiv` (`ProductionWorkplace.cs:21`; das Terminal bietet nur Arbeitsgänge
   BDE-aktiver Werkbänke an, `BdeApiController.cs:197`). Ohne aktives BDE: Symbole wie in Teil 1 (neutral) —
   vorbehaltlich Rückfrage 4.
7. **Leitplanken (verbindlich):** Farbe ist nie alleiniger Bedeutungsträger — jeder Status hat zusätzlich
   eine eigene Form/ein Icon **und** einen Text (sichtbar oder `aria-label`/visuell versteckt, nicht nur
   `title`, der an Touch-Terminals nicht erscheint). Kontrast WCAG AA (4,5:1 Text, 3:1 Konturen/Icons), auch
   bei Rot/Grün-Sehschwäche unterscheidbar. Die genaue Codierung legt Rückfrage 1 fest.
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
     Farbe**; nie grün, nie rot. Ob der Hinweis auch bei BDE aus erscheint, hängt an Rückfrage 4.
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
     - **(ii) Sub-FA-Zeile, die Kind einer Sub-FA ist — Erkennung erfasst doppelt, Regel zeigt einfach.**
       Kürzel `K` einer Sub-FA-Zeile `S` (`VaterFA = P`) landet als `WorkOperation` bei `S` (eigene Zeile,
       `:101`) **und** bei `P` (direktes Kind, `:102`). Regel a ordnet die Zeile `S` der eigenen zu — für die
       Zeile `S` passt das zum Code. Die `WorkOperation` `K` an `P` erscheint aber nur, wenn `P`s eigene Zeile
       oder ein **Blatt**-Kind von `P` ebenfalls `K` trägt; sonst hat sie kein Symbol. → **W-1**, **W-2**.
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

> [!warning] WIDERSPRUCH W-1 — Antwort 6 a „genau der Scope der Erkennung“ vs. doppelte Erfassung
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

- **Eine gebündelte Abfrage** je Ladevorgang über die Ids der angezeigten Arbeitsgänge — kein N+1. Prädikat
  „erledigt“ identisch zum Terminal (`BdeApiController.cs:252-256`):
  `BdeBookingQuantities.Where(q => q.IsFinal && !q.BdeBooking.IsCancelled && ids.Contains(q.BdeBooking.WorkOperationId))`.
  Wiederverwendung statt Neuerfindung: dieselbe Bedingung als Repository-Methode; das Terminal selbst
  bleibt unverändert (kein Refactoring außerhalb des Umfangs).
- Laufende Buchung (falls Rückfrage 5 einen eigenen Zustand will): `BdeBookings` mit `EndedAt == null`
  und `!IsCancelled` (Muster `BdeApiController.cs:260-268`; „pausiert“ hat `EndedAt` gesetzt,
  [[fallstricke]] §6 „`Paused` hat `EndedAt` gesetzt“).
- Termin: aus den bereits geladenen `FaHierarchyOrderInfo` der Struktur (Variante A: im Index ohnehin
  geladen, `FaHierarchyController.cs:68`; Variante B: der Modal-Endpunkt baut den Baum heute **ohne**
  OrderInfos, `FaHierarchyController.cs:148` — dort ist der Kopf der einen HauptFA nachzuladen, z. B. über
  das vorhandene `IFaHierarchyOrderInfoRepository`, `:27`).
- Statusableitung als reine Funktion (Eingaben: hat `WorkOperation`?, erledigt?, läuft?, Termin, heute) →
  Tabellentest. Ohne `WorkOperation` ist das Ergebnis immer „kein BDE-Arbeitsgang“ (Antwort 6 b), vor jeder
  Termin-Prüfung.
- Weil Teil 1 Quelle S zeigt (vormals „Nur Teil-1-Quelle S“ — jetzt unbedingt): vor der Status-Abfrage die
  `WorkOperation`s der angezeigten Strukturen laden (Variante A: **eine** gebündelte Abfrage über die
  HauptFA-Nummern, Muster `WorkOperationRepository.cs:120-128`; Variante B: vorhandenes
  `GetByOrderNumberWithWorkplaceAsync`, `FaHierarchyController.cs:152`) und je Knotenzeile + Kürzel per
  Zuordnungsfunktion (FA 9, Antwort 6 a) auflösen. Die Status-Abfrage bleibt dieselbe, nur über die
  aufgelösten Ids. `WorkOperation`s ohne Symbol (Antwort 6 c) werden nicht dargestellt; die bestehende
  Warnung `UnmatchedOperations` im Modal (Sub-FA nicht im Baum) bleibt unberührt.
- Offene Punkte der Zuordnungsfunktion bis zur Antwort auf Rückfrage 7: Fallback bei fehlender eigener
  `WorkOperation` (W-2), Waisen-Blatt (W-3), Mehrfach-Treffer (W-4); W-1 betrifft, ob zusätzliche Symbole
  oder ein Hinweis für Vater-Arbeitsgänge nötig sind.
- Variante A (Baum): Contains über viele Ids → EF 10 `OPENJSON` mit einem Parameter
  ([[fallstricke]] §8), per ToQueryString-Test belegen.
- **frontend-design-Skill ist im Dev-Lauf Pflicht** (Views/CSS). Bootstrap-5-Farben/Icons der Anwendung,
  keine neue Icon-Bibliothek ohne ADR.

## Migrations-/SQL-Auswirkungen

Keine (Status wird abgeleitet). Keine Migration, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt.
Sollte Rückfrage 2 einen Termin verlangen, der heute nicht importiert wird, entstünde eine Migration —
das wäre eine Umfangsänderung und ist dann neu zu spezifizieren. Alle Kandidaten (`KO_Termin`, `FE_Termin`,
`Start_Beschichtung`, `Neuer_PT_PPS`, `Verladetermin_Vsl`) werden bereits importiert
(`FaHierarchySql.cs:140-141`).

## Audit-Feld-Auswirkungen

Keine — reine Lesedarstellung.

## Rollen / Zugriffsfilter

Unverändert `[RequirePickingOrTrackingOrLeitstandAccess]` (`FaHierarchyController.cs:23`). Die Ansicht
zeigt BDE-Fortschritt jetzt auch Rollen ohne BDE-Rolle (`bde_user`/`bde_shiftlead`/`bde_admin`,
`RoleKeys.cs:14-16`) — es ist nur ein Status je Arbeitsgang, keine Bediener- oder Zeitdaten. Falls das nicht
gewollt ist, gehört es in die Antwort zu Rückfrage 4.

## Listen-View-Pattern (ADR 0005)

Kein neuer Listen-View. Falls Teil 1 Variante A gewählt ist: die Status-Spalte ist dieselbe Spalte wie in
Teil 1 (keine weitere Registrierung); ein Statusfilter in der Filterzeile wäre neu und ist nicht verlangt.

## Akzeptanzkriterien

1. BDE aktiv (laut Rückfrage 4): Ein Arbeitsgang mit einer `IsFinal`-Menge auf nicht stornierter Buchung
   (bzw. dem in Rückfrage 3 festgelegten Signal) erscheint als **erledigt** (grün + Erledigt-Form + Text).
2. Ein nicht erledigter Arbeitsgang, dessen maßgeblicher Auftragstermin (Rückfrage 2) vor heute liegt,
   erscheint als **überfällig** (rot + eigene Form + Text).
3. Alle übrigen erscheinen als **offen** (grau + eigene Form + Text); laufende gemäß Rückfrage 5.
4. Storno der abschließenden Buchung oder Korrektur `IsFinal → false` lässt das Symbol beim nächsten Laden
   wieder offen/überfällig werden (kein gespeicherter Status).
5. Ohne Auftragstermin (`null`) ist ein Arbeitsgang nie überfällig.
6. BDE nicht aktiv: Symbole wie Teil 1, keine Statusfarben.
7. Jeder Status ist ohne Farbwahrnehmung erkennbar (Graustufen-Screenshot) und per Screenreader angesagt.
8. Kontrast aller Statusvarianten ≥ WCAG AA, gemessen am Testsystem.
9. Je Ladevorgang genau **eine** zusätzliche Status-Abfrage (Query-Zählung im Dev-Lauf).
10. Status-Ableitung durch Tabellentest abgedeckt (erledigt vor überfällig; Termin heute = nicht überfällig;
    Termin `null`; Kombigerät-Regel aus Rückfrage 2).

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
13. Weiterhin genau **eine** zusätzliche Status-Abfrage plus höchstens **eine** `WorkOperation`-Abfrage je
    Ladevorgang (Query-Zählung).
14. Eine `WorkOperation` ohne passendes Kürzel in der Struktur (Default-AG `01`; Kürzel in Sage entfernt)
    erzeugt **kein** Symbol (Antwort 6 c); Unit-Test: Auftrag mit AGs `01` und `KA`, Knotenzeile `"KA"` →
    genau ein Symbol `KA`.
15. Verhalten in den Fällen W-1 bis W-4 (FA 9) gemäß Antwort auf Rückfrage 7 — **vor** dem Dev-Lauf
    festzulegen; bis dahin nicht abnahmefähig.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Nummer: nächste freie zum Umsetzungszeitpunkt), Index nachziehen.

- **Vorbedingungen:** IDEAL-Testsystem, BDE aktiv (laut Rückfrage 4), Werkbank `BdeAktiv`, Struktur mit
  ≥ 3 erkannten Arbeitsgängen, Kopfzeile mit Termin in der Vergangenheit und eine mit Termin in der Zukunft.
- **S1 Erledigt:** Am BDE-Terminal einen AG starten und mit Abschluss beenden → Symbol grün/Erledigt-Form.
- **S2 Offen:** AG ohne Buchung, Termin in der Zukunft → grau.
- **S3 Überfällig:** AG ohne Abschluss, Termin gestern → rot/Überfällig-Form.
- **S4 Storno/Korrektur:** Abschlussbuchung im BDE-Buchungs-Editor auf „nicht final“ setzen oder stornieren
  → Symbol nach Neuladen nicht mehr grün.
- **S5 Laufend:** AG gestartet, nicht beendet → Darstellung laut Rückfrage 5.
- **S6 BDE aus:** BDE deaktivieren → neutrale Symbole.
- **S7 Barrierefreiheit:** Graustufen-Ansicht + Screenreader + Kontrastmessung am Fertigungsterminal.
- **S8 Blatt → Vater (Antwort 6 a):** Blattzeile mit Kürzel `KA`, deren Vater-Sub-FA den AG `KA` am Terminal
  abgeschlossen hat → Symbol `KA` an der Blattzeile grün/Erledigt-Form. (Vormals „nur Quelle S … laut
  Rückfrage 6“ — jetzt unbedingt.)
- **S9 Sub-FA-Zeile → eigener AG (Antwort 6 a):** Sub-FA `S` mit eigenem Kürzel `KA` unter Sub-FA `P`. Am
  Terminal nur den `KA`-AG von `S` abschließen → Symbol `KA` an Zeile `S` grün. Gegenprobe: nur den `KA`-AG
  von `P` abschließen → Zeile `S` bleibt offen/überfällig; Verhalten an Zeile `P` laut Antwort auf
  Rückfrage 7 (W-1).
- **S10 Kürzel ohne AG (Antwort 6 b):** Knoten mit einem Kürzel der Ausschlussliste (z. B. `STO`) und einem
  ohne Werkbank (P-K2 b in Teil 1, `Werkbaenke = 0`) → beide Symbole neutral, Screenreader sagt „kein
  BDE-Arbeitsgang“, kein Grün/Rot, auch bei Termin in der Vergangenheit.
- **S11 AG ohne Kürzel (Antwort 6 c):** Sub-FA mit Default-AG `01` (NurFA-Buchung) → kein Symbol `01`.
- **Negativ:** Kombigerät mit abweichenden Terminen → Verhalten laut Rückfrage 2; Struktur ohne Kopfdaten
  (`FaHierarchyOrderInfo` fehlt) → nie überfällig, kein Fehler.

## Pruefschritte fuer den Menschen (nur Sage/PPS beantwortet)

**P1 — Bedeutung der Termine (vor Antwort auf Rückfrage 2).** Auf der IDEAL-Sage-Instanz:

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

Gibt es Treffer mit `FE_Varianten > 1`, braucht Rückfrage 2 eine Regel (z. B. frühester Termin).

**P3 — Umfang der Widersprüche W-1 bis W-4 (vor Antwort auf Rückfrage 7).** Auf der **IDEAL-WMS-Datenbank**
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

Viele Zeilen mit `Gebucht = 1` in b) machen W-1 dringend; a) leer heißt, W-4 tritt heute nicht auf (die
Regel braucht es trotzdem, der Code verhindert es nicht).

## Etappen (nur bei epic: true)

Entfällt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Vorläufig — der QA-Lauf bestätigt gegen den echten Diff.**

- **Web-App:** ja (Repository, ViewModel, Controller, View, CSS).
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:**

```bash
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rueckfragen

1. **Codierung ohne Farbe — wie genau?** (Notiz-Punkt 3) „Farbe nie alleiniger Bedeutungsträger“ ist
   Pflicht (CLAUDE.md, Fertigungsterminals). Empfehlung der Notiz: Status zusätzlich über Form/Icon und
   Text (Tooltip, Screenreader), Kontrast WCAG AA. Zu entscheiden: welche Form/welches Icon je Status
   (z. B. Haken / leerer Kreis / Warndreieck), und ob der Statustext sichtbar am Symbol steht oder nur
   per `aria-label` (Hinweis: `title` erscheint an Touch-Terminals nicht).
2. **„Überfällig“ — welcher Termin?** (Notiz-Punkt 4) Es gibt kein Soll-Datum je Arbeitsgang. Empfehlung
   der Notiz: z. B. Auftragstermin (`FE_Termin`?) überschritten und Arbeitsgang nicht rückgemeldet; welcher
   Termin gilt, entscheidet Gerald. Befund: Kandidaten sind `FE_Termin` („Blechfertigung fertig“ laut
   View-Doku) und `Neuer_PT_PPS` („aktueller PPS-Fertigstellungstermin“) — beide unverifiziert, siehe P1.
   Teilfrage: Welche Zeile gilt bei Kombigeräten mit mehreren Kopfzeilen je HauptFA (siehe P2)?
3. **„Erledigt“ — welches Signal?** (neu, aus dem Code-Befund) BDE-Abschluss (`IsFinal`, wie das Terminal)
   oder manuelle Tracking-Rückmeldung (`IsReported`) oder eines von beiden? Empfehlung der Spec: `IsFinal`,
   weil die Anforderung „bei aktivem BDE“ lautet und das Terminal genau dieses Signal nutzt; die
   Entscheidung bleibt beim Menschen.
4. **„Bei aktivem BDE“ — welcher Schalter?** (neu, aus dem Code-Befund) Globales `BdeAktiv` (AppSetting),
   `BdeAktiv` der Werkbank des Arbeitsgangs, oder beides? Und: neutrale Symbole bei BDE aus — richtig?
5. **Laufende Buchung** (neu, aus dem Code-Befund): grau wie „offen“ oder ein vierter Zustand „läuft“
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
   - **W-1** Kürzel einer Sub-FA-Zeile `S` unter Sub-FA `P` ist als `WorkOperation` an `S` **und** an `P`
     erfasst; die Regel zeigt nur die an `S`. Soll der `P`-Arbeitsgang irgendwo sichtbar werden (z. B. an der
     Zeile `P` zusätzlich, als Hinweis), oder bleibt er bewusst ohne Symbol?
   - **W-2** Zeile mit `SubFA ≠ 0`, aber ohne eigene `WorkOperation` für das Kürzel (nicht materialisiert,
     bei Erkennung schon fertig): „kein BDE-Arbeitsgang“ wörtlich nach Regel a, oder Rückfall auf den
     Vater-Sub-FA wie bei Blättern?
   - **W-3** Waisen-Blatt (VaterFA ohne importierten Knoten): alte `WorkOperation` des Vater-Auftrags
     verwenden oder „kein BDE-Arbeitsgang“?
   - **W-4** Mehrere `WorkOperation`s mit gleichem Kürzel am selben Auftrag: welcher Status gilt (z. B.
     erledigt, wenn einer erledigt / wenn alle erledigt)?
   Die Spec wählt nicht.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Codierung ohne Farbe — Form/Icon je Status, Statustext sichtbar oder nur aria-label? →
2. Überfällig — welcher Termin (FE_Termin / Neuer_PT_PPS / anderer), Regel bei Kombigeräten? →
3. Erledigt — IsFinal (BDE), IsReported (Tracking) oder eines von beiden? →
4. BDE aktiv — globaler Schalter, Werkbank-Schalter oder beides; neutral bei BDE aus? →
5. Laufende Buchung — grau oder eigener Zustand „läuft“? →
6. Nur bei Teil-1-Quelle S: Zuordnung Kürzel → WorkOperation (Sub-FA-Zeile eigene oder Vater), Darstellung Kürzel ohne WorkOperation (neutral / „nicht erfasst“ / ausblenden), WorkOperation ohne Kürzel (weglassen / zeigen)? → **(Teil 1: Quelle S, Gerald 2026-10-06.) a) Zuordnung: Ist die Zeile selbst ein materialisierter Sub-FA (SubFA ≠ 0), gilt ihr eigener Arbeitsgang mit gleicher OperationNumber; sonst der des Vater-Sub-FA (VaterFA) – genau der Scope der Erkennung. b) Kürzel ohne WorkOperation (Ausschlussliste, unbekannt, mehrdeutig, Auftrag bei Erkennung schon fertig): anzeigen, ohne Status, neutral, mit Hinweis „kein BDE-Arbeitsgang“ (Text/aria-label, nicht nur Farbe). c) WorkOperation ohne Kürzel (Default `01`, in Sage entfernte Kürzel): nicht als Symbol zeigen.**
7. Widersprüche Antwort 6 ↔ Code — W-1 Vater-AG eines Sub-FA-Kind-Kürzels sichtbar machen?; W-2 Rückfall auf den Vater, wenn die eigene WorkOperation fehlt?; W-3 Waisen-Blatt: alte Vater-WorkOperation oder „kein BDE-Arbeitsgang“?; W-4 Mehrfach-Treffer: welcher Status gilt? →
