---
type: spec
title: "Beschichtungsauftrag-Ausbau Teil 3 — Beschichtungsauftrag als eigenes Objekt"
slug: 2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-06
source_backlog: "[[2026-10-06-beschichtungsauftrag-ausbau]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Models/Beschichtungsauftrag.cs (neu, erbt AuditableEntity — Kopf, Nummer, Status, Statuszeitpunkte, RowVersion; Muster WarehouseRequisition.cs)
  - IdealAkeWms/Models/BeschichtungsauftragPosition.cs (neu, erbt AuditableEntity — Herkunftsschluessel + Snapshot; Muster WarehouseRequisitionItem.cs:5)
  - IdealAkeWms/Models/BeschichtungsauftragStatus.cs (neu, enum byte mit Luecken fuer den spaeteren Freigabe-Status; Muster WarehouseRequisitionStatus.cs)
  - IdealAkeWms/Data/ApplicationDbContext.cs (DbSets + Konfiguration: Laengen, decimal(18,4), Indizes, UNIQUE Nummer, RowVersion; Muster :1164-1189)
  - IdealAkeWms/Data/Repositories/IBeschichtungsauftragRepository.cs + BeschichtungsauftragRepository.cs (neu, ADR 0001; Registrierung in Program.cs)
  - IdealAkeWms/Services/BeschichtungsauftragWorkflow.cs (neu — Anlegen aus Positionsschluesseln, Nummernvergabe, Statuswechsel, Doppelvergabe-Guard, ggf. Reklamations-Folgeauftrag; Name im Dev-Lauf frei)
  - IdealAkeWms/Migrations/<Zeitstempel>_AddBeschichtungsauftrag.cs (neu)
  - SQL/XX_AddBeschichtungsauftrag.sql (neu, XX = naechste freie Nummer in BEIDEN Zweigen zum Umsetzungszeitpunkt; heute Buendel SQL/93, main SQL/88)
  - SQL/00_FreshInstall.sql (zwei Stellen — Schema-Objekte UND MigrationId)
  - IdealAkeWms.Tests/Services/BeschichtungsauftragWorkflowTests.cs (neu — Nummernkreis, Statusautomat, Doppelvergabe, Audit-Felder, Snapshot)
  - IdealAkeWms.Tests/Repositories/ (DI-Aufloesung, Muster BomDiResolutionTests)
  - secondbrain/codebase/datenmodell.md (zwei neue Tabellen)
  - IdealAkeWms/Views/Help/Changelog.cshtml + beide AppVersion.cs
  - docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md
open_questions:
  - "Nummernformat (Notiz Punkt 3): z. B. BA-JJJJ-laufend, je Standort getrennt? Empfehlung: BA-JJJJ-NNNNN, Jahreswechsel startet bei 1; Trennung je Standort ergibt sich aus der eigenen Datenbank je Installation (bitte bestaetigen)."
  - "Status-Automat (Notiz Punkt 4): Anfangsstatus, Uebergaenge, Storno? Reklamation als Status oder als Folgeauftrag mit Verweis? Empfehlung: Entwurf -> Uebergeben -> Erledigt, Storno nur aus Entwurf; Reklamation = neuer Folgeauftrag mit Verweis auf den Ursprungsauftrag."
  - "Wer darf Auftraege anlegen und Status setzen (Notiz Punkt 4, Edit-Split nach ADR 0006)? Bestehende Rolle beschichtungsauftrag ist reine Lese-Ebene. Neue Schreibrolle oder bestehende Rolle zur Schreibrolle machen?"
  - "Doppelvergabe (Notiz Punkt 5): Positionen, die schon in einem Auftrag stecken, in der Vorschlagsliste ausblenden oder markieren? Welche Status sperren eine Position?"
  - "Kombigeraete und Kopf (Notiz Punkt 8): Darf ein Auftrag Positionen mehrerer HauptFA enthalten, und wie wird der Kopf (Dienstleister, RAL) bei mehrdeutigem Sage-Kopf bestimmt?"
  - "Freigabe-Schritt (Notiz Punkt 6, OUT OF SCOPE): Vorsorge im Datenmodell nur ueber einen reservierten Statuswert, keine Spalten und keinen Code — bestaetigen?"
  - "Vorgemerktes (Notiz Punkt 7, OUT OF SCOPE): Positionen als eigene Zeilen mit eigener Id plus Verweis Folgeauftrag -> Ursprungsauftrag als einzige Vorsorge — bestaetigen?"
  - "Snapshot: Werden Positionsdaten (Menge, Masse, Artnr) bei der Anlage eingefroren, und was gilt, wenn Sage danach aendert oder die Position aus der FAListe verschwindet? Welche Menge wird uebernommen (Sollmenge) und ist sie im Auftrag aenderbar?"
  - "Termine im Kopf: Start_Beschichtung/Beschichten_Retour aus Sage als Vorbelegung uebernehmen und im WMS aenderbar (ohne Rueckschreiben nach Sage), oder eigene WMS-Termine?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

> **Umsetzungsort (Hinweis, keine Entscheidung):** nach Schranke 2 des IDEAL-Buendels, aus main (neuer
> Worktree). SQL-Nummer, TS-Nummer und Version: naechste freie zum Umsetzungszeitpunkt (SQL in **beiden**
> Zweigen pruefen — heute Buendel `SQL/93`, main `SQL/88`). Code-Belege beziehen sich auf den Buendel-Stand
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.47.1).

Teil 3 von 4 — Uebersicht: [[2026-10-06-beschichtungsauftrag-ausbau-uebersicht]]. Standort: **IDEAL**.

**Keine Abhaengigkeit zu Teil 1:** Teil 3 speichert Positionen **je Strukturknoten** (das braucht der
Doppelvergabe-Guard, Anforderung 6). Die Summierung ist Darstellung — ob der gedruckte Auftrag summiert
wird, entscheidet Teil 4 (dort Rueckfrage 6). Fachlich ist Teil 1 daher nicht Voraussetzung.

**Schnitt:** Teil 3 ist der **Backend-Baustein ohne Oberflaeche** (Modell, Migration, Repository,
Workflow-Service, Tests). Er ist eigenstaendig mergbar („dunkel": nichts ist fuer Anwender sichtbar) und
ueber Unit-Tests plus das SQL-Skript abnehmbar. Alle Bedienelemente kommen in Teil 4.

## Ziel / Nutzen (das Warum)

Heute ist der Beschichtungsauftrag eine gefilterte Ansicht auf die Sage-Cache-Tabellen — kein gespeichertes
Objekt (`FaHierarchyBeschichtungController.cs:55-136`: nur Lese-Actions). Es gibt keine Nummer, keinen
Status und keine Spur, was wann an welchen Beschichter ging. Teil 3 fuehrt das Objekt ein: Nummernkreis,
Kopf, Positionen mit Bezug auf HauptFA/Strukturknoten, Ablauf-Status mit Audit.

**Warum Positionen nicht per Fremdschluessel auf `FaHierarchyNode` zeigen duerfen:** Der Sync loescht bei
jedem Lauf **beide** Cache-Tabellen und fuegt neu ein (`FaHierarchySyncService.cs:131-136`,
`DELETE FROM dbo.FaHierarchyNodes` / `dbo.FaHierarchyOrderInfos` + `AddRangeAsync`). Die `Id` einer Zeile
ist damit nach dem naechsten Sync eine andere; ein FK wuerde den Sync brechen oder ins Leere zeigen.
Ausserdem importiert der Sync nur Knoten, deren HauptFA in der FAInfos-View steht (`FaHierarchySql.cs:129-131`)
— verschwindet ein HauptFA aus der View, verschwinden seine Knoten aus dem Cache. Ein erledigter
Beschichtungsauftrag muss trotzdem lesbar bleiben. Deshalb: **Herkunftsschluessel + Snapshot** je Position.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Entitaeten `Beschichtungsauftrag` (Kopf) und `BeschichtungsauftragPosition`, Status-Enum.
- Nummernkreis mit Eindeutigkeit unter Parallelitaet.
- Status-Automat mit erlaubten Uebergaengen laut Antwort 2, Statuszeitpunkt + Benutzer je Uebergang.
- Anlegen aus einer Menge von Positionsschluesseln (aktuell `Beschichtet == true`), mit Doppelvergabe-Guard
  und Snapshot.
- Reklamations-Folgeauftrag mit Verweis auf den Ursprungsauftrag — falls Antwort 2 so entscheidet.
- Migration dreifach (ADR 0004), Repository (ADR 0001), Unit-Tests.

**Out-of-Scope**
- Jede Oberflaeche (Liste, Detail, Buttons, Druck des Auftrags) → Teil 4.
- Dienstleister aus dem Kreditoren-Stamm (Fremdschluessel) → Teil 4, nach [[2026-10-06-kreditoren-stammdaten-spec]].
  Teil 3 speichert den Dienstleister als Text-Snapshot.
- **Freigabe-Schritt** (Notiz Punkt 6): wird **nicht** gebaut. Das Datenmodell darf ihn nur nicht verbauen
  (Rueckfrage 6).
- **Vorgemerktes** (Notiz Punkt 7: Positions-Haekchen Reklamation/Neubeschichtung, Ursprungs-HauptFA bei
  Reklamation): wird **nicht** gebaut. Nur nicht verbauen (Rueckfrage 7).
- Rueckschreiben nach Sage (Integrationsgrenze: Sage bleibt Quelle der FAInfos-Werte, das WMS schreibt dort
  nichts).
- Mail/enaio-Ablage.

## Fachliche Anforderungen

1. **Nummer** laut Antwort 1, eindeutig, nie wiederverwendet (auch nicht nach Storno).
2. **Kopf:** Dienstleister (Text), RAL (Text), Start Beschichtung, Retour (Termine laut Antwort 9), Bemerkung,
   Status, ggf. Verweis auf Ursprungsauftrag. Vorbelegung aus `FaHierarchyOrderInfo` der beteiligten HauptFA.
   Feldbedeutung belegt: `Dienstleister`, `RAL`, `Start_Beschichtung`, `Beschichten_Retour` werden nur vom Sync
   aus der Sage-View FAInfos geschrieben (`FaHierarchySyncService.cs:282-288`, Quelle `FaHierarchySql.cs:139-140`)
   und im WMS nur gelesen (Beschichtungs-Views; `Start_Beschichtung` zusaetzlich in der FA-Liste,
   `ProductionOrdersController.cs:307`). Was sie in Sage fachlich bedeuten (Plan- oder Ist-Termin), ist am Code
   nicht ablesbar — Rueckfrage 9.
3. **Position:** Herkunftsschluessel `HauptFA`, `VaterFA`, `SubFA`, `Position` (die Struktur-Felder aus
   `FaHierarchyNode.cs:19-28`) + Snapshot `HauptArtnr`, `Artnr`, `Matchcode`, Menge, `Breite`, `Hoehe`, `Tiefe`.
   **Unverifiziert:** dass dieser Schluessel eine FAListe-Zeile eindeutig bestimmt — weder das Modell
   (`ApplicationDbContext.cs:1032-1033`, nur nicht-eindeutige Indizes auf HauptFA und VaterFA) noch der Sync erzwingen das.
   Pruefabfrage in den Test-Szenarien; davon haengt der Doppelvergabe-Guard ab.
4. **Kopf-Eindeutigkeit** bei Kombigeraeten und bei mehreren HauptFA laut Antwort 5. Nie still eine
   Kopfvariante waehlen (Grundsatz aus [[2026-07-29-standort-ideal-teil-4-spec]], Antwort zu B-2).
5. **Statuswechsel** nur entlang der erlaubten Uebergaenge (Antwort 2); jeder Wechsel setzt Zeitpunkt und
   Benutzer des Uebergangs sowie die Audit-Felder. Unerlaubter Wechsel → Fehler mit Klartext, kein stiller
   No-op.
6. **Doppelvergabe:** Eine Position (Herkunftsschluessel) darf nicht gleichzeitig in zwei Auftraegen mit
   sperrendem Status stecken (Antwort 4). Der Workflow lehnt das Anlegen ab und nennt die betroffenen
   Positionen samt Auftragsnummer — nicht still ueberspringen.
7. **Anlegen prueft gegen den aktuellen Cache:** Jeder uebergebene Schluessel muss im Cache existieren und
   `Beschichtet == true` sein; sonst Ablehnung mit Liste der Abweichungen (Cache kann sich seit dem
   Seitenaufbau durch einen Sync geaendert haben).
8. **Snapshot-Regel** laut Antwort 8.
9. **Zugriff:** Teil 3 hat keine Actions. Die Schreibrolle (Antwort 3) wird hier festgelegt und in Teil 4
   als Action-Level-Filter umgesetzt; Class-Level bleiben `[RequireBeschichtungsauftragAccess]` +
   `[RequireFaHierarchyBeschichtungAktiv]` (`FaHierarchyBeschichtungController.cs:27-28`).

## Technischer Loesungsentwurf

**Modell (Vorschlag, Feldliste nach den Antworten finalisieren):**

`Beschichtungsauftrag : AuditableEntity`
- `Nummer` (string, max 20, Pflicht, UNIQUE), `Jahr` (int), `LaufendeNummer` (int), UNIQUE (`Jahr`, `LaufendeNummer`)
- `Status` (`BeschichtungsauftragStatus`, byte)
- `Dienstleister` (string?, 200) — Text-Snapshot; Teil 4 ergaenzt den Kreditor-Fremdschluessel
- `Ral` (string?, 200), `StartBeschichtung` (DateTime?), `BeschichtenRetour` (DateTime?)
- `Bemerkung` (string?, 1000)
- `UrsprungsauftragId` (int?, Selbstverweis, `OnDelete Restrict`) — nur bei Antwort 2 = Folgeauftrag
- Statuszeitpunkte je Uebergang: `UebergebenAt`/`UebergebenByUserId`, `ErledigtAt`/`ErledigtByUserId`,
  ggf. `StorniertAt`/`StorniertByUserId` — Muster `WarehouseRequisition.cs:21-28`, User-Id aus
  `ICurrentUserService.GetCurrentAppUserId()` (`ICurrentUserService.cs:7`)
- `RowVersion` (`[Timestamp]`, Muster `WarehouseRequisition.cs:43-44`) gegen parallele Statuswechsel
- `Positionen` (Collection)

`BeschichtungsauftragPosition : AuditableEntity` (Muster `WarehouseRequisitionItem.cs:5`)
- `BeschichtungsauftragId` (FK, Cascade)
- `HauptFA` (int), `VaterFA` (int?), `SubFA` (int), `Position` (int?) — Herkunftsschluessel
- `HauptArtnr`, `Artnr`, `Matchcode` (string?, 200), `Menge` (decimal 18,4), `Breite`/`Hoehe`/`Tiefe`
  (decimal 18,4) — Typen wie `ApplicationDbContext.cs:1010-1030`
- Index (`HauptFA`, `VaterFA`, `SubFA`, `Position`) **nicht** UNIQUE: dieselbe Position darf in einem
  stornierten Auftrag und einem neuen stehen bzw. im Reklamations-Folgeauftrag erneut vorkommen. Die
  Doppelvergabe pruefen deshalb der Workflow (statusabhaengig) — und Tests muessen das am App-Layer
  nachweisen, weil EF InMemory UNIQUE-Indizes nicht erzwingt.

`BeschichtungsauftragStatus : byte` — Werte mit Luecken, z. B. `Entwurf = 10`, `Uebergeben = 30`,
`Erledigt = 40`, `Storniert = 90` (falls Antwort 2 Storno erlaubt). **Wert 20 bleibt fuer „Freigegeben"
reserviert und wird nicht angelegt** — nur ein Kommentar im Enum (Rueckfrage 6).

**Nummernvergabe:** `MAX(LaufendeNummer) + 1` fuer das laufende Jahr, Speichern; scheitert das Insert am
UNIQUE-Index (paralleles Anlegen), neu lesen und erneut versuchen (wenige Versuche, danach sichtbarer
Fehler). Muster fuer „UNIQUE-Index entscheidet, Verlierer faengt `DbUpdateException`":
`SageBookingQueueRepository.cs:163-173`. Im Repo gibt es keinen bestehenden Nummernkreis und keine
SQL-`SEQUENCE` (Grep `nummernkreis|NumberRange|CREATE SEQUENCE|HasSequence|NEXT VALUE FOR` in
`*.cs`/`*.sql`/`*.cshtml`: kein Treffer) — daher bewusst die einfachste tragende Loesung statt einer
Sequence je Jahr.

**Workflow-Service** (scoped, ueber `IBeschichtungsauftragRepository`, `IFaHierarchyNodeRepository`,
`IFaHierarchyOrderInfoRepository`, `ICurrentUserService`):
- `AnlegenAsync(positionsSchluessel, kopfVorgaben)` → prueft Anforderungen 4/6/7, bildet Snapshot, vergibt
  Nummer, setzt `Entwurf`, `CreatedAt/By/ByWindows`; alles in einer Transaktion.
- `StatusWechselnAsync(id, zielStatus, rowVersion)` → prueft Uebergang, setzt Statuszeitpunkt +
  `ModifiedAt/By/ByWindows`; Concurrency-Konflikt → verstaendliche Meldung.
- `ReklamationAnlegenAsync(ursprungId, …)` → nur bei Antwort 2 = Folgeauftrag: neuer Auftrag `Entwurf`
  mit `UrsprungsauftragId`, Positionen als Kopie (Doppelvergabe-Regel fuer Reklamationen laut Antwort 4).
- Ergebnisse als Ergebnisobjekt mit Klartext-Fehlerliste (Controller in Teil 4 uebersetzt in
  `ModelState`/`TempData["WarningMessage"]`; TempData kennt kein `ErrorMessage`).

**Kein** Aktivitaets-Protokoll (ADR 0010 gilt fuer Hintergrund-Services); Nachvollziehbarkeit kommt aus den
Audit- und Statusfeldern. Kein Service-Anteil.

## Migrations-/SQL-Auswirkungen

Neue Tabellen `dbo.Beschichtungsauftraege` und `dbo.BeschichtungsauftragPositionen` (Namen im Dev-Lauf final).
Reihenfolge nach ADR 0004 / CLAUDE.md:
1. Model + DbContext-Konfiguration.
2. `dotnet ef migrations add AddBeschichtungsauftrag --project IdealAkeWms`.
3. `SQL/XX_AddBeschichtungsauftrag.sql`, idempotent: `IF OBJECT_ID(N'dbo.Beschichtungsauftraege', N'U') IS NULL`
   bzw. je Tabelle; jede DDL in eigenem Batch (`GO`); Indizes/Constraints mit eigener Existenzpruefung
   (`sys.indexes`). XX = naechste freie Nummer in **beiden** Zweigen zum Umsetzungszeitpunkt.
4. `__EFMigrationsHistory`-Insert in separatem Batch, mit Existenzpruefung auf die `MigrationId`.
5. `SQL/00_FreshInstall.sql` an **zwei** Stellen: Tabellen/Indizes **und** `MigrationId`.
- Nicht daten-destruktiv (nur neue Tabellen) — kein Pflicht-Backup-Hinweis ueber das uebliche Mass hinaus.
- `SQL/AgentJobs/` unberuehrt (Grep `beschicht|FaHierarchy` in `SQL/AgentJobs`: kein Treffer).
- Fallstrick Buendel: Migrationen 90/91 liessen sich auf der Ziel-DB nicht per `Migrate()` anwenden
  (`fallstricke.md` §8, Hinweis in [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]], Deploy). Fuer diese
  Migration im Dev-Lauf pruefen, ob der automatische `Migrate()`-Aufruf ausreicht oder das SQL-Skript der
  Deploy-Weg ist; im Deploy-Abschnitt festhalten.

## Audit-Feld-Auswirkungen

- Beide Entitaeten erben `AuditableEntity` (`AuditableEntity.cs:3-12`, ADR 0003).
- Anlage: `CreatedAt`, `CreatedBy` (`GetDisplayName()`), `CreatedByWindows` (`GetWindowsUserName()`).
- **Jeder** Update (Kopf-Aenderung, Statuswechsel, Positionsaenderung): `ModifiedAt`, `ModifiedBy`,
  `ModifiedByWindows`. Statuswechsel setzen zusaetzlich `<Status>At` + `<Status>ByUserId`.
- Grenze, bewusst benannt: `AuditableEntity` haelt nur die **letzte** Aenderung. Eine lueckenlose
  Statushistorie ergibt sich nur, weil bei der Empfehlung jeder Status hoechstens einmal erreicht wird
  (Reklamation = neuer Auftrag). Entscheidet Antwort 2 „Reklamation als Status desselben Auftrags", wiederholen
  sich Uebergaenge — dann braucht es eine eigene Historientabelle (neue Entitaet, gleiche Migrationspflichten).

## Akzeptanzkriterien

1. Migration und `SQL/XX_*.sql` legen beide Tabellen an; das Skript laeuft zweimal hintereinander ohne
   Fehler (idempotent); `SQL/00_FreshInstall.sql` enthaelt Tabellen **und** `MigrationId`.
2. Anlegen aus gueltigen Schluesseln erzeugt einen Auftrag im Status `Entwurf` mit Nummer im Format laut
   Antwort 1, Positionen mit Snapshot-Werten und gesetzten `Created*`-Feldern.
3. Zwei Anlagen im selben Jahr erhalten aufeinanderfolgende Nummern; im neuen Jahr beginnt die Folge laut
   Antwort 1 neu.
4. Ein Insert, der am UNIQUE-Index der Nummer scheitert, wird mit neuer Nummer wiederholt; erschoepfte
   Versuche enden mit Klartext-Fehler (Unit-Test ueber simulierten Konflikt; echter Parallelfall Manual-UAT
   bzw. SQL-Test).
5. Erlaubte Uebergaenge laut Antwort 2 gelingen und setzen Statuszeitpunkt, Statusbenutzer und
   `Modified*`; jeder andere Uebergang wird mit Klartext abgelehnt und aendert nichts.
6. Eine Position, die in einem Auftrag mit sperrendem Status steckt (Antwort 4), kann nicht in einen zweiten
   Auftrag uebernommen werden; die Ablehnung nennt Position und Auftragsnummer.
7. Schluessel, die im Cache fehlen oder nicht mehr `Beschichtet == true` sind, fuehren zur Ablehnung mit
   Abweichungsliste.
8. Kopfvorbelegung bei Kombigeraet bzw. mehreren HauptFA folgt Antwort 5; nie wird still eine Variante
   gewaehlt.
9. Nach einem simulierten Sync (Cache geloescht und neu befuellt, andere Ids) ist ein bestehender Auftrag
   vollstaendig lesbar und seine Positionen unveraendert.
10. Paralleler Statuswechsel auf denselben Auftrag fuehrt beim Verlierer zu einer Concurrency-Meldung, nicht
    zu einem stillen Ueberschreiben (RowVersion; Manual-UAT, InMemory prueft RowVersion nicht).
11. Das Enum enthaelt keinen „Freigegeben"-Wert; Wert 20 ist frei und kommentiert.
12. Keine Oberflaeche aendert sich (Liste/Druck/PDF des Beschichtungsauftrags wie vorher, Regression TS-60/TS-74).

## Test-Szenarien

Neues Kapitel (naechste freie Nummer), Schwerpunkt Datenbank und Workflow; Bedienung folgt in Teil 4:
- **Pruefschritt Schluessel-Eindeutigkeit (vor Abnahme, Ergebnis im Kapitel notieren):** Sage-DB IDEAL
  ```sql
  SELECT [HauptFA], [VaterFA], [SubFA], [Position], COUNT(*) AS Anzahl
  FROM [vw_IDEAL-AKE_Kommissionierung_FAListe]
  WHERE [Beschichtet] <> 0
  GROUP BY [HauptFA], [VaterFA], [SubFA], [Position]
  HAVING COUNT(*) > 1;
  ```
  und gegen den WMS-Cache dieselbe Abfrage auf `dbo.FaHierarchyNodes` mit `WHERE Beschichtet = 1`. Leer ⇒
  Schluessel traegt. Treffer ⇒ melden; dann gehoert ein weiteres Feld (z. B. `Artnr`) in den Schluessel.
- **Pruefschritt View-Filter (Sage):** `EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAInfos';` — nach
  welchem Kriterium faellt ein HauptFA aus der View (und damit aus dem Cache)? Bestimmt, wie oft Auftraege
  auf nicht mehr vorhandene Knoten zeigen.
- SQL-Skript zweimal auf Test-DB ausfuehren; FreshInstall auf leerer DB.
- Unit-Tests: Nummernkreis inkl. Jahreswechsel, Uebergangsmatrix, Doppelvergabe, Audit-Felder, Snapshot nach
  simuliertem Sync.
- Manual-UAT Concurrency (zwei Sessions, gleicher Auftrag) — sobald Teil 4 Bedienelemente liefert, dort.

## Etappen (nur bei epic: true)

Nicht zutreffend (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | — | — | — |

## Deploy

*Vorlaeufig — der QA-Lauf bestaetigt gegen den echten Diff.*

- **Web-App:** ja (Modell, DbContext, Repository, Service, DI).
- **Service:** nein (nur Versions-Bump; der Service liest das gemeinsame Modell, nutzt die neuen Tabellen
  aber nicht — im Dev-Lauf am Diff bestaetigen).
- **Migration:** ja — `SQL/XX_AddBeschichtungsauftrag.sql` + History-Insert **vor** dem IIS-Recycle der neuen
  Web-Binaries; kein Service-Stop noetig. Ob `Migrate()` beim Start ausreicht: im Dev-Lauf klaeren (siehe
  Migrationsabschnitt).
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

1. **Nummernformat (Notiz Punkt 3):** z. B. `BA-JJJJ-laufend`, je Standort getrennt?
   *Empfehlung:* `BA-JJJJ-NNNNN` (fuenfstellig, fuehrende Nullen), Zaehler beginnt jedes Jahr bei 1.
   Trennung je Standort ohne Standortkennung in der Nummer — **unter der Annahme**, dass AKE und IDEAL je
   eine eigene Datenbank haben (Hinweis darauf: der Master-Schalter `ProduktionsauftragHierarchisch` ist ein
   Wert je Datenbank, `StandortSettingsCatalog.cs:61`). Das ist am Code nicht beweisbar — bitte bestaetigen.
   Teilen sich beide Standorte eine Datenbank, braucht die Nummer ein Standortkuerzel.
2. **Status-Automat (Notiz Punkt 4):** Anfangsstatus, dann uebergeben → erledigt; Reklamation als Status des
   Auftrags oder als neuer Folgeauftrag mit Verweis? Welche Uebergaenge sind erlaubt? Gibt es Storno?
   *Empfehlung der Koordination:* Folgeauftrag mit Verweis auf den Ursprungsauftrag (bereitet „urspruenglicher
   HauptFA ersichtlich" vor). *Ergaenzung:* `Entwurf → Uebergeben → Erledigt`; `Storniert` nur aus `Entwurf`;
   keine Rueckwaertsschritte (Korrektur = Storno + neuer Auftrag); Reklamation nur aus `Erledigt` (ggf. auch
   aus `Uebergeben`?). Mit Folgeauftrag reichen Statuszeitpunkt-Spalten als Audit (siehe Audit-Abschnitt).
3. **Wer darf anlegen und Status setzen (Notiz Punkt 4, Pflicht Edit-Split ADR 0006):** Die Rolle
   `beschichtungsauftrag` ist bewusst reine Lese-Ebene (`RequireBeschichtungsauftragAccessAttribute.cs:7-12`,
   `CurrentUserService.cs:137-138`). Optionen: (a) neue Schreibrolle (z. B. `beschichtungsauftrag_edit`),
   bestehende bleibt Lesen — Muster `masterdata`/`masterdata_read` (`RoleKeys.cs:6-7`); (b) bestehende Rolle
   wird Schreibrolle, neue Leserolle. Getrennte Rechte je Uebergang (z. B. nur Lager setzt „Erledigt")?
   *Empfehlung:* (a), ein Recht fuer alle Schreibvorgaenge. Neue Rolle = drei Stellen (Attribut,
   `secondbrain/codebase/controller.md`, `Views/Users/RoleOverview.cshtml`) — umgesetzt in Teil 4.
4. **Doppelvergabe (Notiz Punkt 5):** Positionen, die schon in einem Auftrag stecken — in der Vorschlagsliste
   ausblenden oder markieren? Welche Status sperren?
   *Empfehlung:* sperrend sind `Entwurf`, `Uebergeben`, `Erledigt`; `Storniert` gibt frei. Reklamations-
   Folgeauftraege duerfen Positionen eines `Erledigt`-Ursprungs erneut enthalten. In der Vorschlagsliste
   **markieren** (mit Auftragsnummer) statt ausblenden — sonst sieht niemand, warum ein Teil fehlt (Umsetzung
   Teil 4).
5. **Kombigeraete / mehrere HauptFA (Notiz Punkt 8):** Bei mehreren FAInfos-Zeilen je HauptFA sind mehrere
   Dienstleister/RAL moeglich ([[2026-08-06-kombinationsgeraete-montageabteilung]]). Darf ein Auftrag
   Positionen mehrerer HauptFA enthalten, und wie entsteht der Kopf?
   *Empfehlung:* mehrere HauptFA erlaubt (eine Fahrt zum Beschichter), aber **ein** Dienstleister und **ein**
   RAL je Auftrag. Vorbelegung nur, wenn alle beteiligten Kopfzeilen dasselbe Paar liefern; sonst bleibt der
   Kopf leer, der Anwender waehlt, und der Auftrag zeigt sichtbar „Kopf aus Sage mehrdeutig". Positionen mit
   abweichendem Sage-RAL im selben Auftrag → Warnung, nicht still.
6. **Freigabe-Schritt (Notiz Punkt 6) — OUT OF SCOPE:** wird nicht gebaut. Vorsorge nur: Statuswert 20 im
   Enum reserviert (Kommentar, kein Enum-Mitglied), keine Spalten, kein Code. Der spaetere Schritt ergaenzt
   Enum-Wert, `FreigegebenAt/ByUserId` und den Uebergang — additiv, ohne Umbau. Bestaetigen? (Die Frage „wer
   prueft das Dokument vor dem Versand" aus [[2026-09-17-druck-briefkopf-logo-adresse]] gehoert dorthin.)
7. **Vorgemerktes (Notiz Punkt 7) — OUT OF SCOPE:** Positions-Haekchen Reklamation/Neubeschichtung und
   „urspruenglicher HauptFA ersichtlich" werden nicht gebaut. Vorsorge nur: Positionen sind eigene Zeilen mit
   eigener Id (ein Haekchen ist spaeter eine additive Spalte) und der Folgeauftrag verweist auf den Ursprung
   (dessen Positionen tragen `HauptFA`). Bestaetigen?
8. **Snapshot und Menge:** Werden Positionsdaten bei der Anlage eingefroren, und was gilt bei spaeteren
   Sage-Aenderungen oder wenn die Position aus der FAListe verschwindet? Welche Menge (die Liste zeigt heute
   `Sollmenge`, `Index.cshtml:111`; `Fertigungmenge` existiert ebenfalls, `FaHierarchyNode.cs:47`) — und darf
   sie im Auftrag geaendert werden (Teilmenge)?
   *Empfehlung:* Snapshot bei Anlage gilt; keine automatische Nachfuehrung. `Sollmenge` uebernehmen, im
   Status `Entwurf` aenderbar. Abweichungshinweis (Sage-Wert geaendert/Position weg) erst bei Bedarf als
   eigener Punkt.
9. **Termine im Kopf:** `Start_Beschichtung`/`Beschichten_Retour` kommen aus Sage (Bedeutung dort nicht am Code
   belegbar). Als Vorbelegung uebernehmen und im WMS aenderbar (ohne Rueckschreiben nach Sage) — oder eigene
   WMS-Termine (z. B. Uebergabe-/Rueckgabe-Soll) unabhaengig von Sage?
   *Empfehlung:* Vorbelegung aus Sage, im Entwurf aenderbar, kein Rueckschreiben.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Nummernformat / Standort →
2. Status-Automat / Reklamation / Storno →
3. Schreibrolle / Edit-Split →
4. Doppelvergabe / sperrende Status →
5. Mehrere HauptFA / Kopf bei Kombigeraet →
6. Freigabe-Vorsorge bestaetigen →
7. Vorgemerktes-Vorsorge bestaetigen →
8. Snapshot / Menge →
9. Termine im Kopf →
