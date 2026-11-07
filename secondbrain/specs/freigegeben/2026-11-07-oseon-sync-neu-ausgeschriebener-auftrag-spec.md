---
type: spec
title: OSEON-Tracking-Sync übersteht neu ausgeschriebene Aufträge (MERGE über Auftragsnummer)
slug: 2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec
status: InUmsetzung
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-11-03-oseon-sync-neu-ausgeschriebener-auftrag]]"
bug: "[[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-bug]]"
buendel: "[[2026-11-07-buendel-uebersicht]]"
depends_on: ""
version: "Patch auf main-Stand (Hotfix), im Bündel 1.74.0 entfällt"
sql_nummern: "keine (Block 108–109 reserviert, nicht benötigt)"
task: "[[2026-11-07-oseon-hotfix-main]]"
worktree: ""
branch: main
standort: AKE
affected_code:
  - "IDEALAKEWMSService/Services/OseonSyncService.cs (SyncOseonProductionOrdersAsync :147-193, EnsureWorkplacesExistBulkAsync :207-247, BulkMergeOrdersAsync :253-390, BulkMergeOperationsAsync :395-504, OseonRawRow :731-752)"
  - "IDEALAKEWMSService/Services/OseonOrderSelection.cs (neu, reiner Entscheidungs-Helper)"
  - "IDEALAKEWMSService.Tests/Services/OseonOrderSelectionTests.cs (neu)"
  - "IDEALAKEWMSService/AppVersion.cs, IdealAkeWms/AppVersion.cs, IdealAkeWms/Views/Help/Changelog.cshtml (v1.74.0)"
  - "docs/TESTSZENARIEN.md (TS-107)"
  - "Bug: [[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-bug]]"
open_questions:
  - "Auswahlregel bei mehreren OSEON-Zeilen je Auftragsnummer: höchste OseonId (Empfehlung) oder jüngste DateOfLastChange?"
  - "Nicht mehr gelieferte Arbeitsgänge eines neu ausgeschriebenen Auftrags: in diesem Fix stehen lassen (Empfehlung) oder löschen?"
  - "Eigener AKE-Hotfix aus main (v1.30.x) oder nur im Bündel v1.74.0 (Empfehlung: nur Bündel)?"
  - "Testinstanz wms-testideal@ake.at (AKENET01) hat Sync:OseonTrackingEnabled aktiv: gewollt, und auf welcher DEV-Umgebung testet QA?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-07"
---

## Ziel / Nutzen (das Warum)

Schreibt OSEON einen Auftrag neu aus (gleiche `AuftragsNr`, neue `ID`), bricht der OSEON-Tracking-Sync mit
Fehler 2601 ab. Er bleibt danach dauerhaft hängen, und die Teileverfolgung friert ein. Nach dem Fix gleicht der Sync
über das Merkmal ab, das in der Tabelle eindeutig ist (die Auftragsnummer). Er wählt bei Mehrfachlieferung
deterministisch eine Zeile aus und meldet beide Fälle sichtbar im Aktivitäts-Protokoll. Die AG-Zuordnung bleibt
dabei erhalten, nichts wird gelöscht.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Auftrags-MERGE `ON target.[OseonOrderNumber] = source.[OseonOrderNumber]`, `OseonId` wird im UPDATE mitgeschrieben.
- Deterministische Auswahl einer OSEON-Zeile je Auftragsnummer (reiner Helper, unit-getestet).
- Deduplizierung der AG-Zeilen: nur die Zeilen der ausgewählten Auftragszeile, je (Auftragsnummer, Position) eine.
- SQL-Wächter: Die MERGE-Quellen sind auch nach der DB-Kollation eindeutig.
- Meldungen: Counts `neu_ausgeschrieben`, `mehrfach_in_oseon`, `doppelt_nach_kollation` sowie Warnzeilen je Fall.

**Out-of-Scope**
- Löschen nicht mehr gelieferter Arbeitsgänge (Rückfrage 2; bis zur Antwort bleibt der Status quo: der Sync löscht nie).
- Bereinigung der Lücken aus den manuellen Löschungen vom 03.–05.11. Diese Aufträge kommen zurück, sobald sie
  in OSEON geändert werden. Ein voller Nachlauf ist Betrieb, nicht Code (siehe Deploy).
- `OseonProductionOrderRepository.GetByOseonIdAsync` (`IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs:56-59`)
  hat außer Tests keinen Aufrufer (Grep `GetByOseonIdAsync` im Worktree: nur Interface, Implementierung und
  `OseonProductionOrderRepositoryTests.cs:72,84`). Es bleibt unverändert.
- Schema: Der Index auf `OseonId` bleibt nicht eindeutig. Ein eindeutiger Index würde bei einer OSEON-Umnummerierung
  denselben Fehlertyp an anderer Stelle erzeugen.

## Fachliche Anforderungen

1. Ein Auftrag mit bekannter Auftragsnummer und neuer OSEON-ID wird **aktualisiert**, nicht neu angelegt.
   Der WMS-Datensatz behält seine `Id`, also bleiben die Arbeitsgänge über `OseonProductionOrderId` angehängt.
2. Liefert OSEON zu einer Auftragsnummer mehrere Auftragszeilen, gewinnt in jedem Lauf dieselbe Zeile
   (Vorschlag: höchste `ID`, siehe Rückfrage 1). Die Auswahl hängt nicht von der Lesereihenfolge ab.
3. Melden statt still behandeln (Aktivitäts-Protokoll, Dienst `OseonTracking`):
   - `neu_ausgeschrieben`: Anzahl der Aufträge, deren `OseonId` sich im UPDATE geändert hat. Je Fall eine
     Warnzeile „Auftrag {Nr}: OSEON-ID {alt} → {neu} (neu ausgeschrieben)“ mit Referenz = Auftragsnummer.
   - `mehrfach_in_oseon`: Anzahl der Auftragsnummern mit mehr als einer OSEON-Zeile. Je Fall eine Warnzeile
     „Auftrag {Nr}: OSEON liefert {n} Datensätze (IDs …), übernommen {gewählt}“.
   - `doppelt_nach_kollation`: Zeilen, die der SQL-Wächter verworfen hat (in C# verschieden, nach DB-Kollation gleich).
     Ist der Wert größer als 0, folgt eine Warnzeile. Erwartet wird 0. Ein Treffer ist ein Datenbefund und kein
     Normalbetrieb.
4. Der Lauf endet mit Erfolg, und `MAX(LastChangedInOseon)` rückt vor. Der Delta-Hänger aus dem Bug ist damit beseitigt.

## Technischer Loesungsentwurf

**Befund im Bündel-Worktree** (der Backlog stützte sich auf `main` v1.30.1; der Code ist hier identisch):

| Stelle | Fundstelle |
|---|---|
| OSEON-Read ohne `ORDER BY`, AG-Join über `AuftragsNr` (nicht über `pa.ID`) | `OseonSyncService.cs:72-101`, Join `:94` |
| Gruppierung ordinal über die getrimmte Nummer | `:148-151` (Trim beim Lesen `:120`) |
| Zufällige Zeile je Gruppe | `:274` (`group.First()`), dasselbe für Werkbänke `:211` |
| MERGE-Schlüssel `OseonId` | `:344` |
| Fehlerstelle des Stacks | `:377` (Lese-Loop über `OUTPUT $action`) |
| AG-Zeilen ungefiltert aus allen Rohzeilen | `:399-401` |
| AG-MERGE über die Auftragsnummer | `:476-478` |
| Eindeutig ist die Nummer | `ApplicationDbContext.cs:922`, Snapshot `:2526-2527`, `SQL/00_FreshInstall.sql:938` |
| `OseonId` nur normaler Index | `ApplicationDbContext.cs:924`, FreshInstall `:940` |
| AG eindeutig je (Auftrag, Position), FK Cascade | `ApplicationDbContext.cs:950,957-960`, FreshInstall `:971-975` |
| Kein weiterer FK auf `OseonProductionOrders`/`OseonWorkOperations` | Snapshot: einziges `HasOne("IdealAkeWms.Models.Oseon…")` ist `:5115`; FreshInstall: einziges `REFERENCES [dbo].[OseonProductionOrders]` ist `:972` |

**Widerspruch Teil 8 aufgelöst.** [[2026-07-29-standort-ideal-teil-8-spec]] `:199` urteilt „`OseonSyncService` ist
nicht eindeutigkeitsannehmend“. Das Urteil betrifft dort ausdrücklich `ProductionOrders.OrderNumber`, also die
Frage, ob der Sync bei nicht mehr eindeutiger Sage-FA-Nummer (IDEAL) falsch zuordnet. **In diesem Rahmen stimmt es:**
Der einzige Schreibpfad auf `ProductionOrders` ist der mengenbasierte UPDATE-JOIN (`OseonSyncService.cs:542-557`).
**Als allgemeine Aussage über den Sync stimmt es nicht.** Auf der Spiegeltabelle `OseonProductionOrders` nimmt der
Sync an, dass OSEON zu einer Auftragsnummer genau eine stabile ID liefert: Der Abgleich läuft über `OseonId`
(`:344`), eindeutig ist aber `OseonOrderNumber` (`ApplicationDbContext.cs:922`). Genau das löst den Fehler 2601 aus.
Die Teil-8-Spec liegt in `freigegeben/` und wird hier nicht geändert. Der Dev-Lauf ergänzt dort einen Hinweis zur
Einschränkung (siehe Brain-Update).

**Muster:** Entscheidung in einem reinen Helper, Ausführung im raw-SQL-Pfad (`secondbrain/codebase/services.md`,
„Reine Entscheidungs-Helper“; Vorbilder `ProductionOrderReconciler`, `LagerbestandZeroingPlanner`).

1. **`OseonRawRow`** (`:731`) wird von `private class` zu einer `internal sealed class` auf Namespace-Ebene.
   `InternalsVisibleTo` für `IDEALAKEWMSService.Tests` ist vorhanden (`IDEALAKEWMSService.csproj:24`).
2. **Neu `OseonOrderSelection`** (`internal static class`, eine Methode `Select(IReadOnlyList<OseonRawRow> rows)`).
   Rückgabe:
   - `Orders`: je Auftragsnummer genau eine Zeile.
     - Gruppierung über die getrimmte Nummer mit `StringComparer.OrdinalIgnoreCase`. Grund: Die DB-Kollation ist laut
       Prüfschritt P-2 CI, siehe [[fallstricke]] „Groß/Klein in SQL macht die Kollation“.
     - Auswahl in der Gruppe: höchste `OseonId` (Rückfrage 1).
   - `Operations`:
     - nur Zeilen mit `OseonId == gewählte ID` und nicht leerer `PositionNumber`,
     - dedupliziert je (Auftragsnummer, Position) mit `OrdinalIgnoreCase`,
     - bei Gleichstand gewinnt die jüngste `LastStatusReport`, danach die höchste `AgaStatus`.
   - `Multiples`: Liste (Nummer, alle IDs aufsteigend, gewählte ID) für die Meldung.
3. **`SyncOseonProductionOrdersAsync`** ersetzt `orderGroups` (`:148-151`) durch das Ergebnis des Helpers.
   - `EnsureWorkplacesExistBulkAsync` liest `WorkplaceName` aus der gewählten Zeile statt aus `g.First()` (`:211`).
   - `BulkMergeOrdersAsync` füllt die DataTable aus `Orders` (statt `:272-290`).
   - `BulkMergeOperationsAsync` füllt sie aus `Operations` (statt `:399-430`).
   - Die Mehrfach-Warnungen werden vor dem MERGE geschrieben (`run.LogWarningAsync(message, reference: nummer)`,
     `ISyncRun.cs:12`). Im DryRun werden die Counts mit gemeldet.
4. **Auftrags-MERGE** (`:337-371`):
   - `ON target.[OseonOrderNumber] = source.[OseonOrderNumber]`, im `WHEN MATCHED` zusätzlich `target.[OseonId] = source.[OseonId]`.
   - `OUTPUT $action, deleted.[OseonId], inserted.[OseonId], inserted.[OseonOrderNumber]`.
   - Die C#-Schleife (`:377-382`) zählt `UPDATE` mit `deleted.OseonId <> inserted.OseonId` als `neu_ausgeschrieben` und
     schreibt je Fall die Warnzeile. Die Zählung `neu`/`aktualisiert` bleibt wie bisher.
5. **SQL-Wächter** in beiden MERGE-Quellen:
   - Auftrags-MERGE:
     `SELECT … FROM (SELECT t.*, ROW_NUMBER() OVER (PARTITION BY t.[OseonOrderNumber] ORDER BY t.[OseonId] DESC) AS rn FROM #TmpOseonOrders t) … WHERE rn = 1`.
   - AG-MERGE: dieselbe Form mit `PARTITION BY t.[OseonOrderNumber], t.[PositionNumber]`.
   - Vor jedem MERGE ermittelt eine Abfrage `SELECT COUNT(*) - COUNT(DISTINCT …)` auf der Temp-Tabelle die Anzahl
     verworfener Zeilen. Die Summe geht in `doppelt_nach_kollation`.
   - Begründung: Die C#-Gruppierung bildet die DB-Kollation nur nach. Zeichen mit Gewicht 0 in älteren Kollationen
     (Fall (b) im Backlog-Verlauf) sieht nur SQL.
   - Die Temp-Spalten bleiben ohne explizite `COLLATE`-Klausel. Der bestehende Join
     `opo.[OseonOrderNumber] = t.[OseonOrderNumber]` (`:476`) läuft produktiv ohne Kollationskonflikt (Fehler 468).
     Tempdb- und DB-Kollation sind also verträglich, und der Wächter vergleicht nach derselben Regel wie der Index.
6. **Counts** in `FinishSuccessAsync` (`:187-191`, DryRun `:157-161`, leer `:167-171`): `neu`, `aktualisiert`,
   `neu_ausgeschrieben`, `mehrfach_in_oseon`, `doppelt_nach_kollation`. Das sind deutsche Schlüssel mit
   Unterstrich wie `uebersprungen_hierarchisch` (`BomCacheSyncService.cs:46`).

Unverändert bleiben: der Delta-Filter (`:51-63`), die OSEON-Abfrage (`:72-101`), `SyncWorkplacesToProductionOrdersAsync`
und `SyncArticleCategoriesToWmsAsync`.

## Migrations-/SQL-Auswirkungen

**Keine.** Es gibt keine Modelländerung, keine EF-Migration und kein `SQL/1xx_*.sql`, und `SQL/00_FreshInstall.sql`
bleibt unberührt. Der reservierte Block 108–109 wird nicht gebraucht und kann im Bündel weitergegeben werden.
`SQL/AgentJobs/` ist nicht betroffen, denn der OSEON-Sync läuft nur im C#-Service (`SyncWorker.cs:115-136`).

## Einstellungen

keine. Der bestehende Schalter `Sync:OseonTrackingEnabled` (`SyncWorker.cs:115`, Default `false`) bleibt unverändert.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

- Das UPDATE im Auftrags-MERGE setzt weiterhin `ModifiedAt = GETUTCDATE()`, `ModifiedBy = 'IDEALAKEWMSService'` und
  `ModifiedByWindows = SYSTEM_USER` (`:358-360`). Das Umschlüsseln der `OseonId` läuft über diesen Zweig und ist
  damit auditiert.
- INSERT setzt `Created*` (`:365,369`), der AG-MERGE setzt Audit-Felder wie bisher (`:486-488,492,495`).
  Hier ändert sich nichts.

## Akzeptanzkriterien

1. **Neuausschreibung:** Ein WMS-Auftrag trägt Nummer X mit `OseonId` A, OSEON liefert X mit B. Dann gilt nach dem
   nächsten Lauf:
   - Status Erfolg, kein Fehler 2601,
   - derselbe Datensatz (`Id` unverändert) hat `OseonId = B` und `ModifiedBy = 'IDEALAKEWMSService'`,
   - Count `neu_ausgeschrieben = 1` und eine Warnzeile mit X, A und B.
2. Die Arbeitsgänge aus AK 1 hängen nach dem Lauf an derselben `OseonProductionOrderId`. Ihre Anzahl ist
   nicht kleiner als vorher.
3. **Mehrfachlieferung (Unit-Test):** Für Zeilen mit X/ID 100 und X/ID 200 (in beiden Reihenfolgen) wählt
   `OseonOrderSelection.Select` immer 200 und meldet X in `Multiples` mit [100, 200].
4. **AG-Dedupe (Unit-Test):** Zwei Auftragszeilen X/100 und X/200 tragen je die Positionen 10 und 20. `Operations`
   enthält dann genau zwei Zeilen (10, 20), beide aus ID 200.
5. **Groß/Klein (Unit-Test):** `ab-01` und `AB-01` ergeben einen Auftrag. Gewählt wird die Zeile mit der höheren ID.
6. **Normalfall:** Ein Lauf ohne solche Fälle liefert dieselben `neu`/`aktualisiert`-Zahlen wie vorher.
   `neu_ausgeschrieben`, `mehrfach_in_oseon` und `doppelt_nach_kollation` stehen auf 0.
7. Nach dem Lauf aus AK 1 ist `MAX(LastChangedInOseon)` mindestens so groß wie die `DateOfLastChange` von X.
   Der Delta-Hänger tritt also nicht mehr auf.
8. Es gibt keine Datei unter `IdealAkeWms/Migrations/` und kein neues `SQL/*.sql` im Diff.
9. `dotnet build IdealAkeWms.slnx` und `dotnet test` sind grün. Die drei bestehenden `OseonSyncServiceTests`
   (Lebenszyklus über den Fehlerpfad) bleiben grün.

## Test-Szenarien

Neues Kapitel **TS-107 — OSEON-Tracking-Sync: neu ausgeschriebener Auftrag (v1.74.0)** in `docs/TESTSZENARIEN.md`.
Die Nummer ist vorläufig, die Bündel-Übersicht kann sie verschieben. Dazu kommt ein Verweis aus TS-14.2.
Index: `secondbrain/tests/testszenarien-index.md`.

- **TS-107.1 Neuausschreibung simulieren** (nur WMS-**Test**-DB, AKE).
  - Vorbedingungen:
    - Service mit v1.74.0, `Sync:OseonTrackingEnabled = true`.
    - Einmalig `UPDATE dbo.OseonProductionOrders SET LastChangedInOseon = DATEADD(day, -1, LastChangedInOseon)`.
      Damit liest der nächste Lauf alle OSEON-Änderungen des letzten Tages erneut.
    - Einen Auftrag X wählen, der in OSEON in den letzten 24 h geändert wurde.
    - Vorher P3 aus dem Backlog notieren (Id, OseonId, Anzahl AGs).
  - Schritte:
    1. `UPDATE dbo.OseonProductionOrders SET OseonId = OseonId + 900000000000 WHERE OseonOrderNumber = 'X'`.
    2. Den Sync abwarten oder den Service neu starten.
  - Erwartet: AK 1, 2 und 7. In `/SyncLog` (Filter OseonTracking) steht die Warnzeile mit beiden IDs.
- **TS-107.2 Normallauf.** Ein weiterer Lauf ohne Eingriff ergibt Erfolg, die drei neuen Counts stehen auf 0 (AK 6).
- **TS-107.3 Negativfall Altstand.** Mit v1.73.0 und Schritt 1 aus TS-107.1 tritt Fehler 2601 auf. Das ist optional
  und belegt den Bug.
- **Mehrfachlieferung und Kollationsduplikat** lassen sich ohne Schreibzugriff auf OSEON nicht manuell herstellen.
  Sie sind durch Unit-Tests (AK 3–5) abgedeckt. Liefert die Prüfabfrage P2 (Backlog) einen echten Fall im
  Delta-Fenster, wird dieser zusätzlich manuell geprüft: Warnzeile `mehrfach_in_oseon` und kein Fehler.

## Etappen (nur bei epic: true)

Entfällt (`epic: false`). Ein Commit im Bündel-Worktree, Priorität 1 im Bündel.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vorläufig; der QA-Lauf bestätigt die Angaben am echten Diff.*

- **Web-App:** ja, nur `AppVersion.cs` und `Views/Help/Changelog.cshtml` (Versionsstand v1.74.0). Fachlich ändert
  sich nichts.
- **Service:** ja, das ist der Kern des Fixes (`OseonSyncService`, neuer Helper).
- **Migration:** nein.
- **Reihenfolge:** Service stoppen, publishen, starten. Der erste Lauf nach dem Deploy erledigt einen hängenden
  Delta-Zustand von selbst, denn derselbe Auftrag wird jetzt aktualisiert statt eingefügt.
- **Betriebshinweis (Test-DB, optional):** Die am 03.–05.11. gelöschten Positionen von `1617675-%` kommen erst wieder,
  wenn sie in OSEON geändert werden. Wer sie sofort braucht, setzt `LastChangedInOseon` wie in TS-107.1 zurück.
- **Publish-Befehle** (im Worktree):
  ```
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  ```

**Brain-Update durch den Dev-Lauf** (Hauptcheckout):
- `fallstricke.md`, Abschnitt 1: neuer Eintrag „MERGE-Schlüssel = eindeutiger Index; OSEON schreibt Aufträge neu aus“
  mit Begründung.
- `codebase/services.md`: `OseonOrderSelection` in die Tabelle „Reine Entscheidungs-Helper“ aufnehmen.
- `codebase/integrationen.md`, OSEON-Abschnitt: Abgleich über die Auftragsnummer.
- In [[2026-07-29-standort-ideal-teil-8-spec]] bei `:199` einen Hinweis setzen: Das Urteil gilt nur für
  `ProductionOrders.OrderNumber`. Ergänzen, nicht umschreiben.
- Bug-Record auf `behoben` setzen, changelog `2026-11-…-v1-74-0-…`, feature-map, Testindex.

## Offene Rueckfragen

1. **Auswahlregel bei mehreren OSEON-Zeilen je Auftragsnummer.** Die Koordination schlug „jüngste
   `DateOfLastChange`, dann höchste `ID`“ vor. Empfehlung: **höchste `ID`**. Eine Neuausschreibung ist ein neuer
   Datensatz, und das Stornieren oder Fertigmelden des alten kann dessen `DateOfLastChange` *nach* die Anlage des
   neuen schieben. Dann würde der alte gewinnen. Voraussetzung ist, dass OSEON-IDs aufsteigend vergeben werden.
   Das steht nur in OSEON. Prüfabfrage (OSEON-DB):
   ```sql
   SELECT AuftragsNr, ID, Status, DateOfLastChange
   FROM ProduktionsAuftrag
   WHERE AuftragsNr IN (SELECT AuftragsNr FROM ProduktionsAuftrag WHERE KundenAuftragsNr IS NOT NULL
                        GROUP BY AuftragsNr HAVING COUNT(*) > 1)
   ORDER BY AuftragsNr, ID;
   ```
   Erwartet: Je Nummer hat die höchste ID den aktuellen Status, die niedrigeren sind 90/95 oder älter.
2. **Nicht mehr gelieferte Arbeitsgänge eines neu ausgeschriebenen Auftrags.** Empfehlung: **in diesem Fix stehen
   lassen**, das ist der Status quo, und der Sync löscht nie. Der AG-Join läuft über `AuftragsNr`, nicht über die
   Auftrags-ID (`OseonSyncService.cs:94`). Alte und neue Auftragszeile teilen sich also vermutlich dieselben AGs, und
   verwaiste Positionen entstehen nur, wenn OSEON beim Neuausschreiben AG-Positionen entfernt. Unverifiziert,
   Prüfabfrage (OSEON-DB):
   ```sql
   SELECT aga.AuftragsNr, aga.ApPositionsNr, aga.Status, aga.LetzteStatusMeldung
   FROM ProduktionsAga aga WHERE aga.AuftragsNr = '1617675-06' ORDER BY aga.ApPositionsNr;
   -- plus Spaltenliste: verweist ProduktionsAga auch auf ProduktionsAuftrag.ID?
   SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('ProduktionsAga') ORDER BY column_id;
   ```
   Zeigt sich Bedarf, wird eine Folgeaufgabe angelegt („nur bei `neu_ausgeschrieben` die nicht gelieferten Positionen
   dieses Auftrags entfernen und zählen“).
3. **Auslieferungsweg.** Der Backlog sah einen AKE-Hotfix aus `main` vor (v1.30.x), der Auftrag vom 07.11. sieht das
   Bündel mit v1.74.0 vor. Empfehlung: **nur Bündel**. Die AKE-Produktion ist laut Backlog-Verlauf (07.11. 08:55)
   nicht betroffen, und für den Notfall gibt es das Umschlüssel-Skript. Bis zum Bündel-Merge ist die Produktion aber
   gegen eine echte Neuausschreibung ungeschützt.
4. **Testinstanz mit OSEON-Sync.** Die Fehlermail kam von `wms-testideal@ake.at` (AKENET01). Ist dort
   `Sync:OseonTrackingEnabled = true` gewollt, obwohl IDEAL kein OSEON hat? Auf welcher DEV-Umgebung testet QA
   TS-107? Empfehlung: **QA testet auf AKE-Test**, und die IDEAL-Testinstanz schaltet den Schalter aus, wenn sie
   nicht bewusst als AKE-Ersatz läuft. Prüfabfrage (WMS-DB der Testinstanz):
   ```sql
   SELECT [Key], [Value] FROM dbo.ServiceSettings WHERE [Key] LIKE 'Sync:Oseon%';
   ```
   (Tabelle `dbo.ServiceSettings`, Schlüssel `[Key]`: `SQL/00_FreshInstall.sql:1086-1093`.)

## Freigabe-Nachtrag 2026-11-07 (geht dem Rumpf vor)

- Auslieferung als **AKE-Hotfix aus `main`**: neuer Worktree von `main` (`scripts/new-worktree.ps1`), Patch-Version auf den aktuellen `main`-Stand, eigener Merge durch den Menschen (Schranke 2). Nicht im Bündel-Worktree umsetzen; das Bündel holt den Fix über `scripts/sync-worktree.ps1` (Mensch). Die Bündel-Versionen 1.75.0 ff. bleiben, 1.74.0 entfällt im Bündel.

## Freigabe-Nachtrag 3 — direkt in main (Gerald 2026-11-07 16:18, geht Nachtrag 1 vor)

- „gleich im main umsetzen“: **kein** neuer Worktree. Umsetzung im Hauptcheckout auf `main`. Frontmatter `worktree: ""`,
  `branch: main`. Patch-Version, kein SQL, Bündel holt den Fix später über `scripts/sync-worktree.ps1` (Mensch).
  Koordination hatte den Worktree empfohlen (AKE-Produktion liefert aus `main` aus); Entscheidung bei Gerald.

## Freigabe-Nachtrag 2 — Zusatzfixes im selben Hotfix (Gerald 2026-11-07 16:16, geht dem Rumpf vor)

Gerald ordnet an, im selben AKE-Hotfix drei kleine Web-Änderungen mitzuliefern (Wortlaut Gerald in Anführung):

- **Z1 Stücklisten-Druck 2 cm tiefer:** „der druck der stückliste soll um 2 cm nach unten versetzt werden - sprich oben mehr
  frei“. Oberer Rand der Stücklisten-Druckansicht +2 cm; sonst unverändert.
- **Z2 Sortierung im Stücklisten-Druck:** „die sortierreihenfolge bei der stückliste soll beim druck berücksichtigt werden
  (bei bezeichnung 1 funktioniert das nicht)“. Der Druck übernimmt die in der Ansicht gewählte Sortierung, für alle
  sortierbaren Spalten, nachweislich auch „Bezeichnung 1“.
- **Z3 Werkbank-Spalte in der Kommissionierung:** „In der Kommissionierung-Maske auch die Werkbank Spalte anzeigen“.
  Gilt die Spaltenauswahl je Benutzer, wird die Spalte dort wählbar; Default sichtbar.

Offen für den Dev-Lauf, mit Fundstelle zu klären: welche Druckansicht und welche Kommissionierungs-Maske gemeint sind (bei
mehreren Kandidaten nachfragen, nicht raten), Ursache von Z2. Je Fix ein TS-Schritt im Kapitel TS-107 oder als Unterpunkt
der bestehenden Stücklisten-/Kommissionierungs-Kapitel. Deploy: Web ist damit fachlich berührt.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. Auswahlregel mehrere OSEON-Zeilen (höchste ID vs. jüngste DateOfLastChange) → **wie empfohlen (Gerald 2026-11-07): höchste ID**
2. Nicht mehr gelieferte AGs bei Neuausschreibung stehen lassen oder löschen → **wie empfohlen (Gerald 2026-11-07): stehen lassen**
3. Auslieferung nur im Bündel v1.74.0 oder zusätzlich AKE-Hotfix aus main → **Gerald 2026-11-07: zusätzlich als AKE-Hotfix aus `main`, sofort** – eigener Worktree von `main`, Patch-Version auf den `main`-Stand (nicht 1.74.0); das Bündel erhält den Fix über `scripts/sync-worktree.ps1` (Mensch). Siehe Freigabe-Nachtrag.
4. OSEON-Sync auf der IDEAL-Testinstanz gewollt / QA-Testumgebung → **wie empfohlen (Gerald 2026-11-07): QA auf dem AKE-Test; Prüfabfrage `ServiceSettings` der IDEAL-Testinstanz im Testbericht festhalten**
