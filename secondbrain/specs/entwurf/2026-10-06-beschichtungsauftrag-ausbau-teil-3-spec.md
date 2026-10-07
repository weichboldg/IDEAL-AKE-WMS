---
type: spec
title: "Beschichtungsauftrag-Ausbau Teil 3 — Beschichtungsauftrag als eigenes Objekt"
slug: 2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-07
source_backlog: "[[2026-10-06-beschichtungsauftrag-ausbau]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Models/Beschichtungsauftrag.cs (neu, erbt AuditableEntity — Kopf mit genau einem HauptFA, Nummer, Status, Statuszeitpunkte, Dienstleister als Freitext, RowVersion; Muster WarehouseRequisition.cs)
  - IdealAkeWms/Models/BeschichtungsauftragPosition.cs (neu, erbt AuditableEntity — Herkunftsschluessel + Snapshot; Muster WarehouseRequisitionItem.cs:5)
  - IdealAkeWms/Models/BeschichtungsauftragStatus.cs (neu, enum byte mit Luecken fuer den spaeteren Freigabe-Status; Muster WarehouseRequisitionStatus.cs)
  - IdealAkeWms/Data/ApplicationDbContext.cs (DbSets + Konfiguration: Laengen, decimal(18,4), Indizes, UNIQUE Nummer, RowVersion; Muster :1164-1189)
  - IdealAkeWms/Data/Repositories/IBeschichtungsauftragRepository.cs + BeschichtungsauftragRepository.cs (neu, ADR 0001; Registrierung in Program.cs)
  - IdealAkeWms/Services/BeschichtungsauftragWorkflow.cs (neu — Anlegen je HauptFA aus allen beschichteten Positionen, Nummernvergabe, Statuswechsel, Entwurf bearbeiten, Bemerkung, Doppelvergabe-Guard, Reklamations-Folgeauftrag; Name im Dev-Lauf frei)
  - IdealAkeWms/Migrations/<Zeitstempel>_AddBeschichtungsauftrag.cs (neu)
  - SQL/XX_AddBeschichtungsauftrag.sql (neu, XX = naechste freie Nummer in BEIDEN Zweigen zum Umsetzungszeitpunkt; heute Buendel SQL/93, main SQL/88)
  - SQL/00_FreshInstall.sql (zwei Stellen — Schema-Objekte UND MigrationId)
  - IdealAkeWms.Tests/Services/BeschichtungsauftragWorkflowTests.cs (neu — Nummernkreis, Statusautomat, Doppelvergabe, Audit-Felder, Snapshot)
  - IdealAkeWms.Tests/Repositories/ (DI-Aufloesung, Muster BomDiResolutionTests)
  - secondbrain/codebase/datenmodell.md (zwei neue Tabellen)
  - IdealAkeWms/Views/Help/Changelog.cshtml + beide AppVersion.cs
  - docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md
open_questions:
  - "Freigabe-Schritt (Notiz Punkt 6, OUT OF SCOPE): Vorsorge im Datenmodell nur ueber einen reservierten Statuswert, keine Spalten und keinen Code — bestaetigen?"
  - "Vorgemerktes (Notiz Punkt 7, OUT OF SCOPE): Positionen als eigene Zeilen mit eigener Id plus Verweis Folgeauftrag -> Ursprungsauftrag als einzige Vorsorge — bestaetigen?"
  - "Storno und Doppelvergabe: Antwort 4 erlaubt eine erneute Vergabe nur ueber Reklamation. Gibt ein stornierter Entwurf seine Positionen wieder frei (neuer Auftrag fuer denselben HauptFA moeglich)? Empfehlung: ja, Storniert sperrt nicht."
  - "Reklamation aus welchem Status: nur aus Erledigt, oder auch aus Uebergeben? Empfehlung: nur aus Erledigt."
  - "Restpositionen eines HauptFA: Was gilt fuer Positionen, die im Entwurf entfernt wurden oder nach der Anlage in Sage neu beschichtet markiert werden — frei fuer einen weiteren Auftrag desselben HauptFA (nur diese Positionen), oder nur ueber den bestehenden Auftrag? Duerfen im Entwurf Positionen (wieder) hinzugefuegt werden?"
  - "Reklamations-Kopf: Teil-4-Antwort 3 sagt 'nichts vorbelegen', Teil-4-Antwort 4 'Standard-Beschichter aus der PPS-View, ueberschreibbar besonders bei Reklamation'. Gilt 'nichts vorbelegen' nur fuer die Teileauswahl (Kopf dann aus PPS-View bzw. Ursprung vorbelegt) oder auch fuer den Kopf?"
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
Doppelvergabe-Guard, Anforderung 6). Die Summierung ist Darstellung — der gedruckte Auftrag wird summiert
(Teil-4-Antwort 6), das macht Teil 4 beim Druck. Fachlich ist Teil 1 daher nicht Voraussetzung.

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
- Status-Automat `Entwurf → Uebergeben → Erledigt`, Storno nur aus `Entwurf` (Antwort 2), Statuszeitpunkt +
  Benutzer je Uebergang.
- Anlegen **je HauptFA** aus allen aktuell beschichteten Positionen dieses HauptFA (Teil-4-Antwort 2, Antwort 5),
  mit Doppelvergabe-Guard und Snapshot. ~~Anlegen aus einer Menge von Positionsschluesseln~~ *(verworfen mit
  Teil-4-Antwort 2: keine Einzelauswahl)*
- Bearbeiten im `Entwurf` (Kopf, Positionsmenge, Position entfernen) und Bemerkung in jedem Status ausser
  `Storniert` (Teil-4-Antwort 7, Antwort 8).
- Reklamations-Folgeauftrag mit Verweis auf den Ursprungsauftrag (Antwort 2) aus ausgewaehlten Positionen des
  Ursprungs (Teil-4-Antwort 3).
- Migration dreifach (ADR 0004), Repository (ADR 0001), Unit-Tests.

**Out-of-Scope**
- Jede Oberflaeche (Liste, Detail, Buttons, Druck des Auftrags) → Teil 4.
- Dienstleister aus dem Kreditoren-Stamm (Fremdschluessel) — ~~Teil 4, nach [[2026-10-06-kreditoren-stammdaten-spec]]~~
  *(ueberholt mit Teil-4-Antwort 4: Ueberschreiben als Freitext genuegt, Kreditoren-Auswahl spaeter, ausserhalb
  dieses Pakets)*. Teil 3 speichert den Dienstleister als Freitext (vorbelegt aus Sage, ueberschreibbar).
- **Freigabe-Schritt** (Notiz Punkt 6): wird **nicht** gebaut. Das Datenmodell darf ihn nur nicht verbauen
  (Rueckfrage 6).
- **Vorgemerktes** (Notiz Punkt 7: Positions-Haekchen Reklamation/Neubeschichtung, Ursprungs-HauptFA bei
  Reklamation): wird **nicht** gebaut. Nur nicht verbauen (Rueckfrage 7).
- Rueckschreiben nach Sage (Integrationsgrenze: Sage bleibt Quelle der FAInfos-Werte, das WMS schreibt dort
  nichts).
- Mail/enaio-Ablage.

## Fachliche Anforderungen

1. **Nummer** `BA-JJJJ-NNNNN` (fuenfstellig, fuehrende Nullen, Zaehler beginnt jedes Jahr bei 1), **ohne**
   Standortkuerzel — AKE und IDEAL laufen auf getrennten Servern mit eigenen Datenbanken (Antwort 1).
   Eindeutig, nie wiederverwendet (auch nicht nach Storno).
2. **Kopf:** genau **ein** `HauptFA` (Antwort 5: pro HauptFA ein Auftrag), Dienstleister (Freitext), RAL (Text),
   Start Beschichtung, Retour, Bemerkung, Status, ggf. Verweis auf Ursprungsauftrag. Vorbelegung aus
   `FaHierarchyOrderInfo` des HauptFA: Dienstleister = „Standard-Beschichter aus der PPS-View"
   (`FaHierarchyOrderInfo.Dienstleister`, `FaHierarchyOrderInfo.cs:27`), RAL (`:28`), Termine aus
   `Start_Beschichtung`/`Beschichten_Retour` (`:33-34`, Antwort 9). Dienstleister, RAL und Termine sind im
   `Entwurf` aenderbar (Teil-4-Antwort 7: Kopf nur im Entwurf; Teil-4-Antwort 4: Dienstleister am Auftrag
   ueberschreibbar, als Freitext); kein Rueckschreiben nach Sage.
   Feldbedeutung belegt: `Dienstleister`, `RAL`, `Start_Beschichtung`, `Beschichten_Retour` werden nur vom Sync
   aus der Sage-View FAInfos geschrieben (`FaHierarchySyncService.cs:282-288`, Quelle `FaHierarchySql.cs:139-140`)
   und im WMS nur gelesen (Beschichtungs-Views; `Start_Beschichtung` zusaetzlich in der FA-Liste,
   `ProductionOrdersController.cs:307`). Was sie in Sage fachlich bedeuten (Plan- oder Ist-Termin), ist am Code
   nicht ablesbar; Antwort 9 legt fest, dass sie als Beschichtungstermin bzw. „Beschichten retour" uebernommen
   werden.
3. **Position:** Herkunftsschluessel `HauptFA`, `VaterFA`, `SubFA`, `Position` (die Struktur-Felder aus
   `FaHierarchyNode.cs:19-28`) + Snapshot `HauptArtnr`, `Artnr`, `Matchcode`, Menge, `Breite`, `Hoehe`, `Tiefe`.
   **Unverifiziert:** dass dieser Schluessel eine FAListe-Zeile eindeutig bestimmt — weder das Modell
   (`ApplicationDbContext.cs:1032-1033`, nur nicht-eindeutige Indizes auf HauptFA und VaterFA) noch der Sync erzwingen das.
   Pruefabfrage in den Test-Szenarien; davon haengt der Doppelvergabe-Guard ab.
4. **Kopf-Eindeutigkeit** (Antwort 5): Ein Auftrag enthaelt nur Positionen **eines** HauptFA; ein Aufruf mit
   Positionen mehrerer HauptFA wird abgelehnt. Kombigeraet (mehrere `FaHierarchyOrderInfo`-Zeilen je HauptFA,
   `FaHierarchyOrderInfo.cs:7-9`): je Kopffeld wird nur vorbelegt, wenn alle Kopfzeilen denselben Wert
   liefern; sonst bleibt das Feld leer und das Anlege-Ergebnis traegt den Hinweis „Kopf aus Sage mehrdeutig
   (n Kopfzeilen)" mit den Varianten. Nie still eine Kopfvariante waehlen (Grundsatz aus
   [[2026-07-29-standort-ideal-teil-4-spec]], Antwort zu B-2). ~~mehrere HauptFA je Auftrag~~ *(verworfen mit
   Antwort 5)*
5. **Statuswechsel** nur entlang `Entwurf → Uebergeben → Erledigt` und `Entwurf → Storniert` (Antwort 2);
   keine Rueckwaertsschritte. Jeder Wechsel setzt Zeitpunkt und Benutzer des Uebergangs sowie die Audit-Felder.
   Unerlaubter Wechsel → Fehler mit Klartext, kein stiller No-op.
6. **Doppelvergabe** (Antwort 4): Eine Position (Herkunftsschluessel) darf nur ueber eine Reklamation erneut in
   einen Auftrag; sonst ist Doppelvergabe ausgeschlossen. Sperrend sind alle Status ausser `Storniert` — ob
   `Storniert` freigibt, ist Rueckfrage 10 (bis zur Antwort: Empfehlung „gibt frei"). Ausnahme: ein
   Reklamations-Folgeauftrag darf Positionen seines Ursprungsauftrags enthalten. Der Workflow lehnt ab und nennt
   die betroffenen Positionen samt Auftragsnummer — nicht still ueberspringen.
7. **Anlegen je HauptFA prueft gegen den aktuellen Cache:** Der Workflow liest beim Anlegen alle Knoten des
   HauptFA mit `Beschichtet == true` frisch aus dem Cache (nicht aus dem Seitenaufbau). Kein Treffer →
   Ablehnung mit Klartext. Sind einzelne dieser Positionen bereits vergeben, gilt Rueckfrage 12; bis zur
   Antwort lehnt der Workflow mit der Liste der vergebenen Positionen ab (sicherste Variante).
8. **Snapshot-Regel** (Antwort 8): Positionsdaten werden bei der Anlage eingefroren; Menge = `Sollmenge`;
   die Menge ist im `Entwurf` aenderbar; keine automatische Nachfuehrung bei spaeteren Sage-Aenderungen.
9. **Zugriff** (Antwort 3): neue Berechtigungsgruppe „Beschichtungsaufträge" als **Schreibrolle** (ein Recht
   fuer alle Schreibvorgaenge); die bestehende Rolle `beschichtungsauftrag` (`RoleKeys.cs:24`) bleibt reine
   Lese-Ebene. Teil 3 hat keine Actions; Rolle, Filter und die drei Pflichtstellen setzt Teil 4 um.
   Class-Level bleiben `[RequireBeschichtungsauftragAccess]` + `[RequireFaHierarchyBeschichtungAktiv]`
   (`FaHierarchyBeschichtungController.cs:27-28`). Hinweis fuer Teil 4: der Anzeigename der Leserolle lautet
   heute „Beschichtungsauftrag (IDEAL)" (`RoleOverview.cshtml:194`) — der neue Name „Beschichtungsaufträge"
   muss in der Rollenuebersicht klar als Schreibrecht erkennbar sein.
10. **Bearbeiten** (Teil-4-Antwort 7): Kopf und Positionen (Menge aendern, Position entfernen) nur im
    `Entwurf`; danach nur Statuswechsel und Bemerkung. Ob Positionen im Entwurf hinzugefuegt werden duerfen und
    ob entfernte Positionen frei werden, ist Rueckfrage 12.
11. **Reklamation** (Antwort 2, Teil-4-Antwort 3): Folgeauftrag mit `UrsprungsauftragId`, aus `Erledigt`
    (ob auch aus `Uebergeben`: Rueckfrage 11). Die Positionen waehlt der Anwender aus den Positionen des
    Ursprungs; **nichts** wird vorausgewaehlt; leere Auswahl → Ablehnung. Snapshot-Werte kommen aus den
    Ursprungspositionen (nicht aus dem Cache — der HauptFA kann dort schon fehlen). Der „Vermerk am
    Ursprungsauftrag" ergibt sich aus dem Selbstverweis (Ursprung kennt seine Folgeauftraege ueber
    `UrsprungsauftragId`); keine eigene Spalte, der Ursprung behaelt seinen Status `Erledigt`.
    Kopf-Vorbelegung des Folgeauftrags: Rueckfrage 13 (bis zur Antwort wie beim Anlegen aus dem Cache des
    HauptFA; fehlt der HauptFA dort, bleibt der Kopf leer mit Hinweis).

## Technischer Loesungsentwurf

**Modell (nach den Antworten 1, 2, 5, 9 und Teil-4-Antwort 4):**

`Beschichtungsauftrag : AuditableEntity`
- `Nummer` (string, max 20, Pflicht, UNIQUE), `Jahr` (int), `LaufendeNummer` (int), UNIQUE (`Jahr`, `LaufendeNummer`)
- `HauptFA` (int, Pflicht, Index **nicht** UNIQUE — Reklamations-Folgeauftraege und stornierte Entwuerfe teilen
  den HauptFA) — Antwort 5; auch Grundlage fuer Liste/Filter in Teil 4
- `Status` (`BeschichtungsauftragStatus`, byte)
- `Dienstleister` (string?, 200 wie `ApplicationDbContext.cs:1044`) — Freitext, vorbelegt aus Sage, im Entwurf
  ueberschreibbar. ~~Teil 4 ergaenzt den Kreditor-Fremdschluessel~~ *(verworfen mit Teil-4-Antwort 4)*
- `Ral` (string?, 200 wie `:1045`), `StartBeschichtung` (DateTime?), `BeschichtenRetour` (DateTime?)
- `Bemerkung` (string?, 1000)
- `UrsprungsauftragId` (int?, Selbstverweis, `OnDelete Restrict`) — Reklamations-Folgeauftrag (Antwort 2)
- Statuszeitpunkte je Uebergang: `UebergebenAt`/`UebergebenByUserId`, `ErledigtAt`/`ErledigtByUserId`,
  `StorniertAt`/`StorniertByUserId` — Muster `WarehouseRequisition.cs:21-28`, User-Id aus
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
`Erledigt = 40`, `Storniert = 90` (Antwort 2). **Wert 20 bleibt fuer „Freigegeben"
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
- `AnlegenAsync(hauptFa)` → liest die beschichteten Knoten und die Kopfzeilen des HauptFA frisch aus dem
  Cache, prueft Anforderungen 4/6/7, bildet Snapshot, belegt den Kopf vor, vergibt Nummer, setzt `Entwurf`,
  `CreatedAt/By/ByWindows`; alles in einer Transaktion. ~~`AnlegenAsync(positionsSchluessel, kopfVorgaben)`~~
  *(verworfen mit Teil-4-Antwort 2)*
- `EntwurfBearbeitenAsync(id, kopf, positionsMengen, entfernteIds, rowVersion)` → nur im `Entwurf`; setzt
  `ModifiedAt/By/ByWindows` an Kopf und geaenderten Positionen.
- `BemerkungAendernAsync(id, text, rowVersion)` → in jedem Status ausser `Storniert`.
- `StatusWechselnAsync(id, zielStatus, rowVersion)` → prueft Uebergang, setzt Statuszeitpunkt +
  `ModifiedAt/By/ByWindows`; Concurrency-Konflikt → verstaendliche Meldung.
- Abfrage fuer Teil 4: „Herkunftsschluessel → Auftragsnummer(n)" je HauptFA-Menge (Vergabe-Markierung in der
  Vorschlagsliste), im Repository, ohne N+1.
- `ReklamationAnlegenAsync(ursprungId, positionIds)` → neuer Auftrag `Entwurf` mit `UrsprungsauftragId` und
  demselben `HauptFA`, Positionen = Kopie der ausgewaehlten Ursprungspositionen (Doppelvergabe-Ausnahme,
  Anforderung 6); Kopf laut Rueckfrage 13.
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
  Statushistorie ergibt sich, weil jeder Status hoechstens einmal erreicht wird (Antwort 2: keine
  Rueckwaertsschritte, Reklamation = neuer Auftrag) — keine Historientabelle. ~~Entscheidet Antwort 2
  „Reklamation als Status desselben Auftrags", wiederholen sich Uebergaenge — dann braucht es eine eigene
  Historientabelle.~~ *(entfaellt mit Antwort 2)* Aenderungen an Kopf/Menge im Entwurf sind nur als letzte
  Aenderung nachvollziehbar (`Modified*`) — fuer den Entwurf ausreichend, weil erst ab `Uebergeben` gedruckt
  wird (Teil-4-Antwort 8).

## Akzeptanzkriterien

1. Migration und `SQL/XX_*.sql` legen beide Tabellen an; das Skript laeuft zweimal hintereinander ohne
   Fehler (idempotent); `SQL/00_FreshInstall.sql` enthaelt Tabellen **und** `MigrationId`.
2. Anlegen fuer einen HauptFA erzeugt einen Auftrag im Status `Entwurf` mit Nummer `BA-JJJJ-NNNNN`, `HauptFA`
   gesetzt, **allen** beschichteten Positionen dieses HauptFA mit Snapshot-Werten (Menge = `Sollmenge`),
   vorbelegtem Kopf (Dienstleister, RAL, Start, Retour aus `FaHierarchyOrderInfo`) und gesetzten
   `Created*`-Feldern.
3. Zwei Anlagen im selben Jahr erhalten aufeinanderfolgende Nummern (`BA-2026-00001`, `BA-2026-00002`); im
   neuen Jahr beginnt die Folge bei `00001`; die Nummer enthaelt kein Standortkuerzel.
4. Ein Insert, der am UNIQUE-Index der Nummer scheitert, wird mit neuer Nummer wiederholt; erschoepfte
   Versuche enden mit Klartext-Fehler (Unit-Test ueber simulierten Konflikt; echter Parallelfall Manual-UAT
   bzw. SQL-Test).
5. `Entwurf → Uebergeben`, `Uebergeben → Erledigt` und `Entwurf → Storniert` gelingen und setzen
   Statuszeitpunkt, Statusbenutzer und `Modified*`; jeder andere Uebergang (z. B. `Uebergeben → Storniert`,
   `Erledigt → Entwurf`) wird mit Klartext abgelehnt und aendert nichts.
6. Eine Position, die in einem nicht stornierten Auftrag steckt, kann nicht in einen zweiten Auftrag
   uebernommen werden (Ausnahme: Reklamations-Folgeauftrag des Ursprungs); die Ablehnung nennt Position und
   Auftragsnummer. Verhalten nach Storno laut Rueckfrage 10.
7. Anlegen fuer einen HauptFA ohne beschichtete Positionen im Cache wird mit Klartext abgelehnt; Positionen
   werden beim Anlegen frisch aus dem Cache gelesen.
8. Kombigeraet: Kopffelder mit abweichenden Werten in den Kopfzeilen bleiben leer, das Ergebnis meldet „Kopf
   aus Sage mehrdeutig" mit den Varianten; nie wird still eine Variante gewaehlt.
8a. Ein Aufruf mit Positionen mehrerer HauptFA in einem Auftrag ist nicht moeglich bzw. wird abgelehnt.
8b. Kopf, Menge und Positionsentfernung aendern sich nur im `Entwurf`; in `Uebergeben`/`Erledigt` wird nur die
    Bemerkung angenommen, alles andere mit Klartext abgelehnt; jede Aenderung setzt `Modified*`.
8c. Reklamation aus `Erledigt` mit ausgewaehlten Positionen erzeugt einen `Entwurf` mit `UrsprungsauftragId`,
    demselben `HauptFA` und genau diesen Positionen; leere Auswahl wird abgelehnt; der Ursprung zeigt den
    Folgeauftrag ueber den Selbstverweis und bleibt `Erledigt`.
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
- Unit-Tests: Nummernkreis inkl. Jahreswechsel und Format `BA-JJJJ-NNNNN`, Uebergangsmatrix (alle erlaubten
  und eine Auswahl verbotener Uebergaenge), Doppelvergabe inkl. Reklamations-Ausnahme, Anlegen je HauptFA
  (alle Positionen, leerer HauptFA, Kombigeraet-Kopf), Bearbeiten nur im Entwurf, Bemerkung danach,
  Reklamation (Auswahl, leere Auswahl, Status des Ursprungs), Audit-Felder, Snapshot nach simuliertem Sync.
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

*Stand 2026-10-07: beantwortet 1–5, 8, 9 (eingearbeitet); offen 6, 7 und neu 10–13.*

1. **Nummernformat (Notiz Punkt 3):** z. B. `BA-JJJJ-laufend`, je Standort getrennt?
   *Beantwortet mit Antwort 1: `BA-JJJJ-NNNNN`, eigene Datenbank je Standort, kein Kuerzel.*
   *Empfehlung:* `BA-JJJJ-NNNNN` (fuenfstellig, fuehrende Nullen), Zaehler beginnt jedes Jahr bei 1.
   Trennung je Standort ohne Standortkennung in der Nummer — **unter der Annahme**, dass AKE und IDEAL je
   eine eigene Datenbank haben (Hinweis darauf: der Master-Schalter `ProduktionsauftragHierarchisch` ist ein
   Wert je Datenbank, `StandortSettingsCatalog.cs:61`). Das ist am Code nicht beweisbar — bitte bestaetigen.
   Teilen sich beide Standorte eine Datenbank, braucht die Nummer ein Standortkuerzel.
2. **Status-Automat (Notiz Punkt 4):** Anfangsstatus, dann uebergeben → erledigt; Reklamation als Status des
   Auftrags oder als neuer Folgeauftrag mit Verweis? Welche Uebergaenge sind erlaubt? Gibt es Storno?
   *Beantwortet mit Antwort 2: wie vorgeschlagen (der Klammerteil „ggf. auch aus Uebergeben?" → Rueckfrage 11).*
   *Empfehlung der Koordination:* Folgeauftrag mit Verweis auf den Ursprungsauftrag (bereitet „urspruenglicher
   HauptFA ersichtlich" vor). *Ergaenzung:* `Entwurf → Uebergeben → Erledigt`; `Storniert` nur aus `Entwurf`;
   keine Rueckwaertsschritte (Korrektur = Storno + neuer Auftrag); Reklamation nur aus `Erledigt` (ggf. auch
   aus `Uebergeben`?). Mit Folgeauftrag reichen Statuszeitpunkt-Spalten als Audit (siehe Audit-Abschnitt).
3. **Wer darf anlegen und Status setzen (Notiz Punkt 4, Pflicht Edit-Split ADR 0006):** Die Rolle
   `beschichtungsauftrag` ist bewusst reine Lese-Ebene (`RequireBeschichtungsauftragAccessAttribute.cs:7-12`,
   `CurrentUserService.cs:137-138`). Optionen: (a) neue Schreibrolle (z. B. `beschichtungsauftrag_edit`),
   bestehende bleibt Lesen — Muster `masterdata`/`masterdata_read` (`RoleKeys.cs:6-7`); (b) bestehende Rolle
   wird Schreibrolle, neue Leserolle. Getrennte Rechte je Uebergang (z. B. nur Lager setzt „Erledigt")?
   *Beantwortet mit Antwort 3: Variante (a), neue Gruppe „Beschichtungsaufträge", bestehende bleibt Lesen.*
   *Empfehlung:* (a), ein Recht fuer alle Schreibvorgaenge. Neue Rolle = drei Stellen (Attribut,
   `secondbrain/codebase/controller.md`, `Views/Users/RoleOverview.cshtml`) — umgesetzt in Teil 4.
4. **Doppelvergabe (Notiz Punkt 5):** Positionen, die schon in einem Auftrag stecken — in der Vorschlagsliste
   ausblenden oder markieren? Welche Status sperren?
   *Beantwortet mit Antwort 4: erneute Vergabe nur ueber Reklamation (Storno-Fall → Rueckfrage 10; Darstellung
   → Teil 4).*
   *Empfehlung:* sperrend sind `Entwurf`, `Uebergeben`, `Erledigt`; `Storniert` gibt frei. Reklamations-
   Folgeauftraege duerfen Positionen eines `Erledigt`-Ursprungs erneut enthalten. In der Vorschlagsliste
   **markieren** (mit Auftragsnummer) statt ausblenden — sonst sieht niemand, warum ein Teil fehlt (Umsetzung
   Teil 4).
5. **Kombigeraete / mehrere HauptFA (Notiz Punkt 8):** Bei mehreren FAInfos-Zeilen je HauptFA sind mehrere
   Dienstleister/RAL moeglich ([[2026-08-06-kombinationsgeraete-montageabteilung]]). Darf ein Auftrag
   Positionen mehrerer HauptFA enthalten, und wie entsteht der Kopf?
   *Beantwortet mit Antwort 5: nein, pro HauptFA ein Auftrag; Kombigeraete bleiben mehrdeutig gekennzeichnet.*
   *Empfehlung:* ~~mehrere HauptFA erlaubt (eine Fahrt zum Beschichter), aber **ein** Dienstleister und **ein**
   RAL je Auftrag.~~ *(verworfen mit Antwort 5)* Vorbelegung nur, wenn alle beteiligten Kopfzeilen dasselbe
   liefern; sonst bleibt der Kopf leer, der Anwender waehlt, und der Auftrag zeigt sichtbar „Kopf aus Sage
   mehrdeutig". ~~Positionen mit abweichendem Sage-RAL im selben Auftrag → Warnung, nicht still.~~ *(entfaellt
   mit Antwort 5 — ein HauptFA je Auftrag)*
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
   *Beantwortet mit Antwort 8: wie vorgeschlagen.*
   *Empfehlung:* Snapshot bei Anlage gilt; keine automatische Nachfuehrung. `Sollmenge` uebernehmen, im
   Status `Entwurf` aenderbar. Abweichungshinweis (Sage-Wert geaendert/Position weg) erst bei Bedarf als
   eigener Punkt.
9. **Termine im Kopf:** `Start_Beschichtung`/`Beschichten_Retour` kommen aus Sage (Bedeutung dort nicht am Code
   belegbar). Als Vorbelegung uebernehmen und im WMS aenderbar (ohne Rueckschreiben nach Sage) — oder eigene
   WMS-Termine (z. B. Uebergabe-/Rueckgabe-Soll) unabhaengig von Sage?
   *Empfehlung:* Vorbelegung aus Sage, im Entwurf aenderbar, kein Rueckschreiben.
   *Beantwortet mit Antwort 9 (Uebernahme aus `Start_Beschichtung`/`Beschichten_Retour`); die dort offene
   Aenderbarkeit im Entwurf ist durch Teil-4-Antwort 7 entschieden („Kopf und Positionen nur im Entwurf").*
10. **Storno und Doppelvergabe (neu 2026-10-07):** Antwort 4 erlaubt eine erneute Vergabe nur ueber eine
    Reklamation. Ein stornierter Entwurf (Storno nur im Entwurf, Antwort 2) ging nie an den Beschichter — gibt er
    seine Positionen frei, sodass ein neuer Auftrag fuer denselben HauptFA moeglich ist?
    *Empfehlung:* ja, `Storniert` sperrt nicht (sonst waeren die Positionen nach einem Fehlanlage-Storno nur noch
    ueber eine Reklamation vergebbar, die einen `Erledigt`-Ursprung braucht).
11. **Reklamation aus welchem Status (neu 2026-10-07):** Die Empfehlung zu Rueckfrage 2 nannte „nur aus
    Erledigt (ggf. auch aus Uebergeben?)"; Antwort 2 „wie vorgeschlagen" klaert den Klammerteil nicht.
    *Empfehlung:* nur aus `Erledigt`.
12. **Restpositionen eines HauptFA (neu 2026-10-07):** Teil-4-Antwort 2 uebernimmt automatisch alle Positionen
    des HauptFA und erlaubt Bearbeiten im Entwurf; Antwort 5 sagt „pro HauptFA ein Auftrag". Was gilt fuer
    Positionen, die im Entwurf entfernt wurden oder nach der Anlage in Sage neu als beschichtet erscheinen —
    frei fuer einen weiteren Auftrag desselben HauptFA (nur diese Positionen) oder nur ueber den bestehenden
    Auftrag? Duerfen im Entwurf Positionen (wieder) hinzugefuegt werden?
    *Empfehlung:* entfernte und neue Positionen sind frei; ein weiterer Auftrag fuer denselben HauptFA nimmt nur
    freie Positionen; im Entwurf kein Hinzufuegen (kein Positions-Picker noetig).
13. **Reklamations-Kopf (neu 2026-10-07):** Teil-4-Antwort 3 sagt „Teile auswaehlen, nichts vorbelegen",
    Teil-4-Antwort 4 „Standard-Beschichter aus der PPS-View, am Auftrag ueberschreibbar — besonders bei
    Reklamation". Gilt „nichts vorbelegen" nur fuer die Teileauswahl (Kopf dann aus PPS-View vorbelegt, Fallback
    Ursprungskopf) oder auch fuer den Kopf?
    *Empfehlung:* nur fuer die Teileauswahl; Kopf aus der PPS-View, fehlt der HauptFA im Cache, aus dem Ursprung.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Nummernformat / Standort → **`BA-JJJJ-NNNNN` ja (Keyuser 2026-10-07). Standortfrage (eigene Datenbank je Standort) offen bei Gerald.** **Nachtrag (Gerald 2026-10-07): AKE und IDEAL laufen auf zwei getrennten Servern mit eigenen Datenbanken – Nummer ohne Standortkürzel.**
2. Status-Automat / Reklamation / Storno → **Wie vorgeschlagen (Keyuser 2026-10-07): Entwurf → Übergeben → Erledigt, Storno nur im Entwurf, Reklamation als Folgeauftrag mit Verweis.**
3. Schreibrolle / Edit-Split → **Neue Berechtigungsgruppe „Beschichtungsaufträge“ (Schreibrolle) (Keyuser 2026-10-07); bestehende Rolle bleibt Lesen.**
4. Doppelvergabe / sperrende Status → **Eine Position darf nur über eine Reklamation erneut in einen Auftrag; sonst ist Doppelvergabe ausgeschlossen (Keyuser 2026-10-07). Darstellung in der Vorschlagsliste: siehe Teil 4.**
5. Mehrere HauptFA / Kopf bei Kombigeraet → **Nein – pro HauptFA ein Auftrag (Keyuser 2026-10-07) (abweichend vom Vorschlag). Kopf (Dienstleister/RAL) damit je HauptFA; Kombigeräte bleiben mehrdeutig gekennzeichnet.**
6. Freigabe-Vorsorge bestaetigen →
7. Vorgemerktes-Vorsorge bestaetigen →
8. Snapshot / Menge → **Wie vorgeschlagen (Keyuser 2026-10-07): Snapshot bei Anlage, Sollmenge, im Entwurf änderbar, keine automatische Nachführung.**
9. Termine im Kopf → **Beschichtungstermin und „Beschichten retour“ aus den vorhandenen Terminen übernehmen (Keyuser 2026-10-07) – am Code `FaHierarchyOrderInfo.Start_Beschichtung` / `Beschichten_Retour` (Z. 33-34, aus der PPS-View). Änderbarkeit im Entwurf: Vorschlag, nicht ausdrücklich beantwortet.**
10. Storno gibt Positionen frei →
11. Reklamation aus welchem Status →
12. Restpositionen eines HauptFA / Hinzufuegen im Entwurf →
13. Reklamations-Kopf vorbelegen →
