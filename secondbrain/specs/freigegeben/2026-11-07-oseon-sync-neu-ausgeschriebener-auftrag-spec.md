---
type: spec
title: OSEON-Tracking-Sync übersteht neu ausgeschriebene Aufträge (MERGE über Auftragsnummer)
slug: 2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec
status: Testbereit
created: 2026-11-07
updated: 2026-11-08
source_backlog: "[[2026-11-03-oseon-sync-neu-ausgeschriebener-auftrag]]"
bug: "[[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-bug]]"
buendel: "[[2026-11-07-buendel-uebersicht]]"
depends_on: ""
version: "1.30.3 (Hotfix direkt in main, nach Zahnrad-Merge e386bfce), im Bündel 1.74.0 entfällt"
sql_nummern: "keine (Block 108–109 reserviert, nicht benötigt)"
task: "[[2026-11-07-oseon-hotfix-main]]"
worktree: ""
branch: main
standort: AKE
affected_code:
  - "IDEALAKEWMSService/Services/OseonSyncService.cs (SyncOseonProductionOrdersAsync :147-193, EnsureWorkplacesExistBulkAsync :207-247, BulkMergeOrdersAsync :253-390, BulkMergeOperationsAsync :395-504, OseonRawRow :731-752)"
  - "IDEALAKEWMSService/Services/OseonOrderSelection.cs (neu, reiner Entscheidungs-Helper)"
  - "IDEALAKEWMSService.Tests/Services/OseonOrderSelectionTests.cs (neu)"
  - "IDEALAKEWMSService/AppVersion.cs, IdealAkeWms/AppVersion.cs, IdealAkeWms/Views/Help/Changelog.cshtml (v1.30.3)"
  - "docs/TESTSZENARIEN.md (Kapitel 107, TS-107.1–107.6, Verweis TS-14.2)"
  - "Z1: IdealAkeWms/Views/Picking/PrintBom.cshtml (@page-Rand)"
  - "Z2: IdealAkeWms/Controllers/PickingController.cs (RenderPrintBomAsync), IdealAkeWms/Views/Picking/Bom.cshtml (Druck-Klick)"
  - "Z3: IdealAkeWms/Views/Picking/Index.cshtml, IdealAkeWms/Models/ViewModels/PickingListViewModel.cs, PickingController.Index/ColumnMap"
  - "IdealAkeWms.Tests/Controllers/PickingControllerTests.cs (2 neue Tests)"
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

*QA 2026-11-07 am echten Diff (`git diff` HEAD `b8acd084`, uncommittet) bestätigt: Änderungen unter `IdealAkeWms/`
(Controller, ViewModel, 3 Views, Changelog, AppVersion) → **web: true**; unter `IDEALAKEWMSService/`
(`OseonSyncService.cs`, neu `OseonOrderSelection.cs`, AppVersion) → **service: true**; nichts unter `*/Migrations/`,
nichts unter `SQL/` → **migration: false**, kein Einmal-Skript. Gilt erst nach Behebung der QA-Befunde B1/B2
(siehe QA-Abschnitt). Nach Commit/Merge nur dann erneut aus `main` publishen, wenn der Merge geprüfte Dateien mit
parallelen `main`-Änderungen kombiniert hat (z. B. Zahnrad-Hotfix, Befund B2).*

> [!warning] Hotfix AKE produktiv — v1.30.3 direkt aus `main` (Freigabe-Nachtrag 3, B2: nach Zahnrad-Merge)
> Dienst **und** Web deployen. Der Rumpf unten nannte v1.74.0 und „Web fachlich unverändert“; beides ist durch
> die Nachträge überholt: Version **1.30.3** (enthält den Zahnrad-Hotfix v1.30.2), und Web ist durch Z1–Z3 fachlich berührt.
> Publish aus dem **Hauptcheckout** `C:\Git\IDEAL-AKE-WMS` (Zweig `main`):
> ```
> dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
> dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
> ```
> Reihenfolge: Dienst stoppen → Dienst publishen/kopieren → Dienst starten; Web (App-Pool recyceln). Keine Migration,
> kein SQL. Nach dem Web-Deploy Browser-Cache leeren bzw. Strg+F5 (geändertes Inline-JS in `Bom.cshtml`).

- **Web-App (Rumpf, überholt):** ja, nur `AppVersion.cs` und `Views/Help/Changelog.cshtml` (Versionsstand v1.74.0). Fachlich ändert
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

## Umsetzung (Dev-Lauf 2026-11-07, direkt auf `main`, v1.30.2)

Vorbedingung: Zweig `main` (HEAD `5425a44e`), Arbeitsverzeichnis **nicht** sauber (nur `secondbrain/`, `.claude/`,
`CLAUDE.md`). Auf Geralds Anweisung („1 — und wir können alle nicht committeten im secondbrain auch committen")
wurden die offenen `secondbrain/`-Änderungen vorab committet (`36a7ed96`). `.claude/*` und `CLAUDE.md` bleiben
uncommittet und sind nicht Teil des Hotfixes. Kein Code-Commit (Auftrag: kein Commit ohne Rückfrage).

### OSEON (Rumpf)

| Punkt | Fundstelle |
|---|---|
| Helper `OseonOrderSelection.Select` (höchste ID je Nummer, `OrdinalIgnoreCase`; AGs nur aus gewählter Zeile, je (Nummer, Position) einmal, Gleichstand: jüngste `LastStatusReport`, dann höchste `AgaStatus`; `Multiples`) | `IDEALAKEWMSService/Services/OseonOrderSelection.cs` (neu), `OseonRawRow` dort als `internal sealed` |
| Aufruf statt `GroupBy`, Mehrfach-Warnungen vor dem MERGE (auch im DryRun) | `OseonSyncService.cs:148-160` |
| Counts `neu`, `aktualisiert`, `neu_ausgeschrieben`, `mehrfach_in_oseon`, `doppelt_nach_kollation` (DryRun, leer, Erfolg) | `OseonSyncService.cs:217` (`Counts`) |
| Auftrags-MERGE `ON target.[OseonOrderNumber]`, `target.[OseonId] = source.[OseonId]` im UPDATE | `OseonSyncService.cs:377` ff. |
| `OUTPUT $action, deleted.[OseonId], inserted.[OseonId], inserted.[OseonOrderNumber]` → `neu_ausgeschrieben` + Warnzeile „Auftrag {Nr}: OSEON-ID {alt} → {neu} (neu ausgeschrieben)" (Referenz = Nummer) | `OseonSyncService.cs:404` ff. |
| SQL-Wächter Aufträge `ROW_NUMBER() OVER (PARTITION BY OseonOrderNumber ORDER BY OseonId DESC) … rn = 1` + Zählung | `OseonSyncService.cs:360-361` |
| SQL-Wächter AGs `PARTITION BY OseonOrderNumber, PositionNumber ORDER BY LastStatusReportInOseon DESC, OseonStatus DESC` + Zählung | `OseonSyncService.cs:525-527` |
| Nicht gelieferte AGs bleiben stehen (kein DELETE, wie bisher) | AG-MERGE ohne `WHEN NOT MATCHED BY SOURCE` |
| Schema unverändert, `OseonId`-Index bleibt nicht eindeutig | kein Diff unter `IdealAkeWms/Migrations/`, `SQL/` |

### Zusatzfixes (Nachtrag 2)

- **Z1 — welche Druckansicht:** es gibt genau eine Stücklisten-Druckansicht, `Views/Picking/PrintBom.cshtml`
  (Aufrufer: `PickingController.PrintBom`/`PrintBomPost`, aus `Picking/Bom` und dem Read-only-Modus von
  FA-Abarbeitungsliste/FA-Vervollständigung, die dieselbe View `~/Views/Picking/Bom.cshtml` rendern —
  `FaWorklistController.cs:315`, `FaCompletionController.cs:324`). **Fix:** `@page { margin: 28mm 8mm 8mm 8mm; }`
  statt `8mm` (`PrintBom.cshtml:155`). Bildschirmansicht unverändert.
- **Z2 — Ursache:** Der Druck-Klick (`Bom.cshtml`) schickte die sichtbaren Positionen nur bei Teilfilterung und
  der Server behandelte sie als **Menge** (`HashSet`) und sortierte den Druck danach **immer** fest nach Position
  (`.OrderBy(v => v.Position, new NaturalPositionComparer())`, `PickingController.cs` `RenderPrintBomAsync`). Die
  clientseitige Sortierung (`table-filter.js` `sortTable`, ordnet nur die DOM-Zeilen um) kam also bei **keiner**
  Spalte im Druck an. Bei „Bezeichnung 1“ fällt es am deutlichsten auf, weil diese Reihenfolge am weitesten von
  der Positionsfolge abweicht. Die Aussage „nur bei Bezeichnung 1“ ließ sich am Code nicht bestätigen; betroffen
  waren alle Spalten. **Fix:** Client schickt die Liste auch bei aktiver Sortierung (`th[data-sort-dir]`,
  `Bom.cshtml:990-991`), in DOM-Reihenfolge. Server übernimmt die Reihenfolge der Liste (`Dictionary` Position →
  Index, `PickingController.cs:549`). Ohne Liste wie bisher alles nach Position. Test
  `PickingControllerTests.PrintBom_VisiblePositions_KeepClientOrder`.
- **Z3 — welche Maske:** Kandidaten mit „Kommissionierung“ im Menü (`_Layout.cshtml:116,125`) sind der Leitstand
  (`PickingLeitstand/Index`, hat die Spalte schon: `data-col-key="workbench"` `:112`), die Lager-Kommissionierung
  (`WarehousePicking/Index`, hat „Werkbank“ `:64`) und die Picker-Liste `Picking/Index` (Menüpunkt
  „Kommissionierung“). Nur Letztere hat keine Werkbank-Spalte, also ist sie gemeint. **Fix:** Spalte nach „Kunde“
  (`Index.cshtml:45`, Zelle, `column-config` `:103`), `PickingListItem.WorkplaceName` aus
  `o.ProductionWorkplace?.Name` (`PickingController.cs:143`, `Include` in
  `ProductionOrderPickingStatusRepository.cs:178,195`), server-seitiger Spaltenfilter `ColumnMap["workbench"]`
  (`PickingController.cs:78`). Default sichtbar auch bei gespeicherter Spaltenauswahl
  (`column-preferences.js:103-108`: neue Keys `visible = !defaultHidden`). Test
  `PickingControllerTests.Index_WorkbenchColumn_FilledFromWorkplaceAndFilterable`.

### Version, Doku

- `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs`: 1.30.1 → **1.30.2**; `Views/Help/Changelog.cshtml`.
- `docs/TESTSZENARIEN.md`: Kapitel 107 (TS-107.1–107.6; 107.4–107.6 = Z1–Z3), Verweis aus TS-14.2, Inhaltsverzeichnis.
  Nummer bündelgleich, in `main` folgt 107 auf 56.

### Beweis Build + Tests (Dev-Lauf, Hauptcheckout)

```
dotnet build IdealAkeWms.slnx   → Der Buildvorgang wurde erfolgreich ausgeführt. 25 Warnung(en), 0 Fehler
                                   (keine Warnung in geänderten Dateien; --no-incremental geprüft)
dotnet test                     → IdealAkeWms.Tests:        Fehler 0, erfolgreich 1109, übersprungen 1, gesamt 1110
                                   IDEALAKEWMSService.Tests: Fehler 0, erfolgreich 202,  übersprungen 0, gesamt 202
```
Die drei bestehenden `OseonSyncServiceTests` (Fehlerpfad-Lebenszyklus) sind darin grün (AK 9).

### Prüfabfrage IDEAL-Testinstanz (Freigabe-Antwort 4) — Ergebnis: Gerald

```sql
SELECT [Key],[Value] FROM dbo.ServiceSettings WHERE [Key] LIKE 'Sync:Oseon%';   -- WMS-DB der IDEAL-Testinstanz (AKENET01)
```
Ergebnis: _offen (Gerald)_. Erwartung: `Sync:OseonTrackingEnabled = false`, sofern die Instanz nicht bewusst als AKE-Ersatz läuft.

## QA (qa-agent 2026-11-07) — NICHT Testbereit

Geprüft im Hauptcheckout `C:\Git\IDEAL-AKE-WMS`, Zweig `main`, HEAD `b8acd084`, Hotfix uncommittet (`git diff` +
2 neue Dateien). `.claude/*`, `CLAUDE.md`, `secondbrain*` nicht Gegenstand. Status bleibt **InUmsetzung**:
zwei offene Befunde (B1, B2) und der DEV-Deploy war nicht möglich (Shell nicht erhöht).

### 1. Build + Tests (eigener Lauf)

```
dotnet build IdealAkeWms.slnx  → 0 Fehler, 25 Warnungen (NU190x Paket-Advisories, CS8602 TrackingController.cs:261 — keine in geänderten Dateien)
dotnet test --no-build         → IDEALAKEWMSService.Tests: Fehler 0, erfolgreich 202, übersprungen 0, gesamt 202
                                 IdealAkeWms.Tests:        Fehler 0, erfolgreich 1109, übersprungen 1, gesamt 1110
```

### 2. Code-Review (selbst, in diesem Lauf)

Geprüft: `OseonSyncService.cs` (Lesen/Delta `:51-140`, Auswahl `:148-205`, `Counts`, Auftrags-MERGE inkl. Temp-Tabelle,
Wächter, OUTPUT-Leser, AG-MERGE), `OseonOrderSelection.cs`, `OseonOrderSelectionTests.cs`, `PickingController.cs`
(ColumnMap, Index, `RenderPrintBomAsync`), `Bom.cshtml` Druck-Klick `:943-995` + `table-filter.js` `sortTable`/
`data-sort-dir` (`:229-243`, `:314-357`, `:570-574`), `PrintBom.cshtml` `@page`, `Picking/Index.cshtml`,
`column-preferences.js:102-110`, Repo-Include `ProductionOrderPickingStatusRepository.cs:178,195`, Changelog,
TESTSZENARIEN Kap. 107, Versionen anderer offener AKE-Hotfix-Zweige.

| # | Schwere | Befund | Fundstelle | Behandlung |
|---|---|---|---|---|
| **B1** | Mittel | **Die Regel „höchste ID gewinnt“ gilt nur innerhalb des gelesenen Deltas, nicht gegen den gespeicherten Stand.**<br>Ablauf:<br>1. Der Delta-Read filtert je `pa`-Zeile mit `pa.DateOfLastChange > @DeltaDate`.<br>2. Nach der Neuausschreibung ändert OSEON nur noch die **alte** Zeile. Das ist genau der Fall aus Rückfrage 1: Stornieren oder Fertigmelden schiebt deren `DateOfLastChange` nach vorn.<br>3. Im Delta kommt nur ID A an. `Select` wählt A.<br>4. Der MERGE `ON OseonOrderNumber` schlüsselt B → A **zurück**, überschreibt Status und Mengen mit dem alten (stornierten) Auftrag und meldet fälschlich „OSEON-ID B → A (neu ausgeschrieben)“.<br>5. Beim nächsten Delta mit B kippt es wieder zurück.<br>Das widerspricht Freigabe-Antwort 1 („höchste ID gewinnt“) und AK 2 („in jedem Lauf dieselbe Zeile“). Es gibt keinen Absturz, aber falsche Daten und eine falsche Warnung. Die Unit-Tests sehen das nicht, weil sie nur eine einzelne Zeilenmenge prüfen. | `OseonSyncService.cs`: Delta-Filter in `oseonSql` (`AND pa.DateOfLastChange > @DeltaDate`), MERGE `WHEN MATCHED THEN` ohne ID-Bedingung | **offen → Dev.**<br>Vorschlag (kleinste Änderung): `WHEN MATCHED AND source.[OseonId] >= target.[OseonId] THEN UPDATE …`. Übersprungene ältere Zeilen zählen und melden, z. B. Count `aelter_ignoriert` plus Warnzeile, sonst bleibt der Fall still.<br>Zu bedenken: Wird nur die alte Zeile übersprungen, rückt `MAX(LastChangedInOseon)` dafür nicht vor. Sie wird dann bei jedem Lauf neu gelesen; das ist harmlos, aber laut. Alternative: im Zweig „älter“ nur `LastChangedInOseon` fortschreiben.<br>Testschritt siehe Checkliste Punkt 3. |
| **B2** | Mittel | **Versionskollision v1.30.2.** Die Nummer ist schon an den AKE-Hotfix „Zahnrad Lagerbestellung“ vergeben: Zweig `feature/2026-10-08-ake-hotfix-zahnrad-lagerbestellung`, Commits `0a6fb3cd` und `6d0e954e`, nicht in `main`, Brain-Changelog [[2026-10-08-v1-30-2-ake-hotfix-zahnrad-lagerbestellung]].<br>Jener Zweig ändert dieselben Dateien: beide `AppVersion.cs`, `Changelog.cshtml`, `docs/TESTSZENARIEN.md`. Folge: Merge-Konflikte und zwei verschiedene Stände mit derselben Fußzeilen-Version.<br>Ob der Zahnrad-Hotfix schon auf AKE produktiv läuft, lässt sich hier nicht feststellen. | `IdealAkeWms/AppVersion.cs:5`, `IDEALAKEWMSService/AppVersion.cs:5`; `git log main..feature/2026-10-08-ake-hotfix-zahnrad-lagerbestellung` | **offen → Gerald entscheidet:**<br>(a) Zahnrad-Hotfix zuerst mergen, dieser Hotfix wird **v1.30.3**, oder<br>(b) dieser Hotfix bleibt v1.30.2, der Zahnrad-Hotfix wird umnummeriert.<br>Danach Changelog, TS-Kopf und Spec-Frontmatter nachziehen. |
| H1 | Hinweis | `AppVersion.Date` bleibt in beiden Dateien `"2026-08-03"`. Der Zahnrad-Hotfix hat das Datum mitgezogen. | `AppVersion.cs:6` | Dev erledigt das zusammen mit B2. |
| H2 | Hinweis | Z2: Die Aussage „nur Bezeichnung 1 betroffen“ stimmte nicht, betroffen waren alle Spalten. Ursache und Fix sind bestätigt.<br>Vorbestehend und nicht Teil des Fixes: `sortTable` vergleicht numerisch per `parseFloat`, wenn **beide** Zellen mit einer Zahl beginnen, sonst per `localeCompare`. Bei gemischten Bezeichnungen („10 mm …“, „9 mm …“, „Anker“) ist dieser Vergleich nicht transitiv. Die Sortierung nach Bezeichnung 1 kann deshalb schon in der Ansicht seltsam aussehen, und der Druck folgt ihr jetzt auch dann. | `wwwroot/js/table-filter.js:334-348` | Nur melden. Kommt aus TS-107.5 eine Rückmeldung, wird daraus eine eigene Aufgabe. |
| H3 | Hinweis | Auch eine gespeicherte Standard-Sortierung des Nutzers setzt `data-sort-dir` (`triggerSort`). Sie zählt damit als „Sortierung aktiv“, und der Druck folgt der Ansicht. Das entspricht Z2. | `table-filter.js:248-252,570-574`, `Bom.cshtml:990-991` | Ok, keine Aktion. |

Ohne Befund geprüft:
- MERGE `ON target.[OseonOrderNumber]`, `target.[OseonId] = source.[OseonId]` im UPDATE.
- `OUTPUT $action, deleted.[OseonId], inserted.[OseonId], inserted.[OseonOrderNumber]`: Bei INSERT wird vor `GetInt64` per `continue` abgebrochen. `OseonId` ist `bigint`/`long` (`Models/OseonProductionOrder.cs:7`).
- ROW_NUMBER-Wächter in beiden Quellen. Die Zählung läuft nach BulkCopy und vor dem MERGE über dieselbe `PARTITION`-Zeichenkette. Die Sortierung im SQL entspricht der in C#: höchste ID bzw. `LastStatusReport DESC` (NULL zuletzt, in C# `MinValue`), dann Status DESC.
- Counts mit deutschen Schlüsseln in allen drei Pfaden (DryRun, leer, Erfolg). Warnzeilen mit Referenz.
- Audit-Felder in UPDATE/INSERT unverändert vorhanden.
- Kein Schema-Diff. Der `OseonId`-Index bleibt unberührt.
- Z1 `@page { margin: 28mm 8mm 8mm 8mm }`, nur im Druck-Media-Block.
- Z3: Spalte, Zelle, `column-config` und `ColumnMap["workbench"]` greifen ineinander. Die Tabelle läuft im Server-Mode (`Index.cshtml:37`). `WorkplaceName` kommt aus `Include` in beiden Repo-Pfaden. Bei gespeicherter Auswahl bleibt die Spalte sichtbar (`column-preferences.js:106`).
- Defektklassen: kein `@Html.Raw` in `<script>`, kein `hidden`+`d-*`, keine Parallel-Arrays.
- `?? DateTime.MinValue` dient nur der Sortierung und ist kein Fallback, der einen fehlenden Wert zu einem echten Wert macht.
- Keine neue Einstellung, daher ADR 0020 nicht betroffen.

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweis | Status |
|---|---|---|
| 1 Neuausschreibung (Erfolg, gleiche Id, neue OseonId, ModifiedBy, Count und Warnzeile) | Raw SQL plus C#-OUTPUT-Leser, also **Manual-UAT**.<br>Teilbeleg QA: MERGE-SQL wortgleich auf DEV-DB `AKE_WMS_DEV` in einer Transaktion mit ROLLBACK (siehe 6.). Ergebnis: `UPDATE 1662131 → 900001662131`, gleiche `Id 227964`, `ModifiedBy=IDEALAKEWMSService`, eine Zeile je Nummer, kein Fehler 2601.<br>Count und Warnzeile (C#-Pfad) sind **nicht** belegt. | offen (Mensch, TS-107.1) |
| 2 AGs bleiben an derselben Id | Simulation: AG-Anzahl vorher und nachher 1, an Id 227964. Der echte Lauf ist **Manual-UAT**. B1 betrifft die Hälfte „in jedem Lauf dieselbe Zeile“. | offen |
| 3 Mehrfachlieferung | Unit-Test `Select_MultipleIds_PicksHighest_InBothOrders` | grün, gilt aber nur innerhalb eines Lesevorgangs (B1) |
| 4 AG-Dedupe | Unit-Tests `Select_Operations_OnlyFromSelectedRow_OncePerPosition` und `Select_DuplicatePosition_LatestStatusReportWins_ThenHighestStatus` | grün |
| 5 Groß/Klein | Unit-Test `Select_CaseInsensitiveNumber_IsOneOrder`.<br>SQL-Seite: DB- und tempdb-Kollation sind auf DEV `Latin1_General_CI_AS`; der Wächter hat das Duplikat gezählt (`doppelt_nach_kollation 1`, `ops_doppelt 1`). Die Simulationsnummer `1662066-65` hat keine Buchstaben; der Groß/Klein-Teil ist damit nur per Unit-Test belegt. | grün / Simulation |
| 6 Normalfall, drei Counts = 0 | Der Auswahlteil ist per Unit-Test `Select_NormalCase_NoMultiples` belegt. Die Counts im Protokoll sind **Manual-UAT** (TS-107.2). | offen |
| 7 MAX(LastChangedInOseon) rückt vor | Simulation: `LastChangedInOseon` der Zeile wurde übernommen. Der echte Delta-Lauf ist **Manual-UAT**. | offen |
| 8 keine Migration, kein SQL | `git status` und `git diff --stat HEAD -- IdealAkeWms/Migrations SQL` sind leer. | bewiesen |
| 9 Build/Tests grün inkl. 3 `OseonSyncServiceTests` | eigener Lauf oben | bewiesen |
| Z1 Druckrand | Browser-Druckvorschau, **Manual-UAT** (TS-107.4) | offen |
| Z2 Druck-Sortierung | Server: `PrintBom_VisiblePositions_KeepClientOrder`. Client-JS (`data-sort-dir`, DOM-Reihenfolge) ist **Manual-UAT** (TS-107.5). | Server grün, Client offen |
| Z3 Werkbank-Spalte | Server-Filter: `Index_WorkbenchColumn_FilledFromWorkplaceAndFilterable`. Spaltenauswahl und Default sichtbar sind **Manual-UAT** (TS-107.6). | Server grün, Browser offen |

### 4. Geänderte bestehende Tests

- `IdealAkeWms.Tests/Controllers/PickingControllerTests.cs` `Setup(...)`: Die optionalen Parameter `orders`, `bom` und `stock` (Default wie bisher `Mock.Of`) wurden ergänzt. Das erweitert nur die Fixture, das Verhalten der bestehenden Tests bleibt gleich. Kein Befund.
- Sonst wurde kein bestehender Test geändert. Die `OseonSyncServiceTests` sind unverändert.

### 5. Checkliste CLAUDE.md

| Punkt | Stand |
|---|---|
| Migration/SQL/FreshInstall/AgentJobs | entfällt |
| Audit-Felder | entfällt (vorhanden) |
| Version beide `AppVersion.cs` | ja, aber Kollision siehe **B2** |
| Anwender-Changelog | ja |
| Brain-Changelog | **fehlt**. Nach B2 mit der endgültigen Nummer anlegen. |
| Feature-Map | **kein Eintrag** (`grep oseon-sync-neu secondbrain/feature-map.md` leer) |
| Bug-Record | steht noch auf `status: offen`. Richtig bis zum Merge, danach auf `behoben` setzen. |
| TESTSZENARIEN Kap. 107 + Index | ja (`testszenarien-index.md:125`). Für B1 sollte ein Schritt ergänzt werden. |
| ADR 0020 | entfällt |

### 6. DEV-Deploy und Testlauf

- **Standort:** `AKE` (Frontmatter). Freigabe-Antwort 4 nennt „AKE-Test“. Der qa-agent darf aber nur die lokale DEV-Umgebung (`http://localhost:8081`, DB `AKE_WMS_DEV`) benutzen und nie Testserver. TS-107 auf dem echten AKE-Test-Server bleibt deshalb beim Menschen.
- **Deploy:** **nicht ausgeführt.** Die Shell ist nicht erhöht: `net session` → NOT_ELEVATED, PowerShell `IsInRole(Administrator)` → `False`. `dev-deploy.ps1` muss den Dienst stoppen, IIS steuern und publishen. QA-Punkt 6 gilt damit als **fehlgeschlagen**.
- Hinweis für einen späteren Lauf: `AKE_WMS_DEV` steht auf Migration `20261106202615_AddSageStammdatenOutboxAndUebergabeStatus`, also auf Bündel-Stand. `main` v1.30.x ist älter als dieses DB-Schema. Ein Deploy aus `main` auf diese DB ist nur bedingt aussagekräftig.
- Auf DEV gilt `Sync:OseonTrackingEnabled = false` (Abfrage `dbo.ServiceSettings`), und QA darf den Schalter nicht einschalten. TS-107.1/107.2 laufen dort daher auch nach einem Deploy nicht als echter Sync.

| Schritt | Ausgeführt | Ergebnis | Bewertung |
|---|---|---|---|
| TS-107.1 Neuausschreibung (echter Sync) | nein | Sync-Schalter auf DEV aus (harte Grenze), kein Deploy | NICHT MÖGLICH |
| TS-107.1 Ersatz: MERGE-SQL als Simulation | ja, `sqlcmd -S . -E -C -d AKE_WMS_DEV`, BEGIN TRAN … ROLLBACK | Ausgangslage: Auftrag `1662066-65` (Id 227964, OseonId 1662131, 1 AG).<br>Temp-Quelle: dieselbe Nummer mit `OseonId+900000000000`, dazu ein Kleinschreib-Duplikat mit niedrigerer ID.<br>Auftrags-MERGE: Wächter `doppelt_nach_kollation 1`, OUTPUT `UPDATE 1662131 900001662131 1662066-65`. Danach Id 227964 mit `OseonId 900001662131`, `ModifiedBy IDEALAKEWMSService`, AGs 1, eine Zeile je Nummer, `LastChangedInOseon` übernommen.<br>AG-MERGE mit Duplikat: `ops_doppelt 1`, `UPDATE`, AGs weiterhin 1.<br>Nach ROLLBACK wieder OseonId 1662131. | OK (nur SQL-Teil) |
| TS-107.2 Normallauf | nein | wie 107.1 | NICHT MÖGLICH |
| TS-107.3 Altstand 2601 | nein | optional | NICHT MÖGLICH |
| TS-107.4 Druckrand (Z1) | nein | kein Deploy | NICHT MÖGLICH |
| TS-107.5 Druck-Sortierung (Z2) | nein | kein Deploy | NICHT MÖGLICH |
| TS-107.6 Werkbank-Spalte (Z3) | nein | kein Deploy | NICHT MÖGLICH |

### Prüfabfrage IDEAL-Testinstanz (Freigabe-Antwort 4)

```sql
SELECT [Key],[Value] FROM dbo.ServiceSettings WHERE [Key] LIKE 'Sync:Oseon%';   -- WMS-DB der IDEAL-Testinstanz (AKENET01)
```
Ergebnis: **Gerald 2026-11-08: `Sync:OseonTrackingEnabled` ist auf der IDEAL-Testinstanz aus** – wie erwartet, kein Handlungsbedarf.
Zum Vergleich der lokale DEV-Stand `AKE_WMS_DEV`: `Sync:OseonArticleCategoryEnabled=false`, `Sync:OseonTrackingEnabled=false`.

### Was bis Testbereit fehlt

1. Dev behebt B1. Danach einen Unit- oder SQL-Simulationsschritt ergänzen: nur die alte ID kommt im Delta, die gespeicherte ID bleibt.
2. Gerald entscheidet B2 (Versionsnummer), Dev zieht nach: AppVersion inkl. Date, Changelog, TS-Kopf, Frontmatter `version`, Brain-Changelog, Feature-Map.
3. Erneuter qa-agent-Lauf in einer **erhöhten** Shell. Dann DEV-Deploy AKE und TS-107.4–107.6 im Browser.

### Manuelle Testcheckliste (Mensch, nach Behebung)

1. Auf dem AKE-Test (Dienst und Web aus `main`): TS-107.1 Neuausschreibung. Prüfen: Erfolg, gleiche Id, Count `neu_ausgeschrieben=1`, Warnzeile, AGs nicht weniger, `MAX(LastChangedInOseon)` rückt vor.
2. TS-107.2 Normallauf: alle drei neuen Counts stehen auf 0.
3. Zu B1, nach dem Fix: `UPDATE dbo.OseonProductionOrders SET OseonId = OseonId + 1 WHERE OseonOrderNumber='X'` (gespeicherte ID höher als OSEON). Nächster Lauf mit X im Delta: Die gespeicherte ID wird **nicht** zurückgeschrieben, und es erscheint eine Meldung.
4. TS-107.3 (optional): Altstand mit v1.30.1 → Fehler 2601.
5. TS-107.4 Druckvorschau: oben 28 mm Rand, auch auf den Folgeseiten.
6. TS-107.5: Nach Bezeichnung 1 auf- und absteigend sortieren, nach Menge sortieren, Filter mit Sortierung kombinieren. Ohne Sortierung wird nach Position gedruckt. Das gilt auch aus FA-Abarbeitungsliste und FA-Vervollständigung.
7. TS-107.6: Spalte Werkbank anzeigen, Server-Filter `A1` über alle Seiten, Spaltenauswahl prüfen, Benutzer mit gespeicherter Auswahl sieht die Spalte.
8. Prüfabfrage der IDEAL-Testinstanz ausführen und das Ergebnis hier eintragen.

## Dev-Nachlauf zu den QA-Befunden (2026-11-07)

- **B1 behoben.** Vor dem Auftrags-MERGE ermittelt eine Abfrage die Nummern, für die das Delta eine **kleinere**
  OSEON-ID liefert als gespeichert. Der MERGE aktualisiert nur noch `WHEN MATCHED AND source.[OseonId] >= target.[OseonId]`
  (`OseonSyncService.cs`, `BulkMergeOrdersAsync`). Übersprungene Fälle stehen im neuen Count `aeltere_id_uebersprungen`,
  dazu je Fall eine Warnzeile „Auftrag {Nr}: OSEON liefert ältere ID {a}, gespeichert ist {b} — nicht übernommen
  (höchste ID gewinnt)“ mit der Nummer als Referenz.
  - AGs einer übersprungenen alten Zeile werden weiter abgeglichen. Das ist unkritisch, weil OSEON die AGs über die
    `AuftragsNr` verknüpft und nicht über `pa.ID` (`OseonSyncService.cs:94`).
  - Bekannte Grenze: Die Regel setzt aufsteigende OSEON-IDs voraus (Freigabe-Antwort 1). Eine Umnummerierung auf
    eine kleinere ID würde nie übernommen, sondern nur gemeldet.
  - Bekannte Grenze: Liefert das Delta nur die alte Zeile, rückt `MAX(LastChangedInOseon)` dafür nicht vor. Die
    Zeile wird gelesen und gemeldet, bis andere Änderungen das Fenster vorschieben. Ein Hänger entsteht dabei nicht,
    weil der Lauf mit Erfolg endet.
  - **Beweis auf DEV** (`Server=.`, `AKE_WMS_DEV`, Transaktion mit ROLLBACK, Skript `b1_sim.sql` im Scratchpad).
    - 1662066-65 (gespeichert 1662131, geliefert 1662130 mit Status 95): erkannt als `aeltere_id`, kein UPDATE,
      Status bleibt 90.
    - 1662066-64 (geliefert +900000000000): `UPDATE 1662130 → 900001662130`, eine Zeile je Nummer.
    - Nach dem ROLLBACK ist der Ausgangszustand wieder hergestellt.
  - Neuer Testschritt **TS-107.2b** in `docs/TESTSZENARIEN.md`; TS-107.2 nennt den neuen Count.
  - Build: 0 Fehler, 24 Warnungen. Tests: Web 1109/1 übersprungen, Dienst 202, alle grün.
- **B2 offen — Entscheidung Gerald.** Version 1.30.2 trägt bereits der ungemergte Zweig
  `feature/2026-10-08-ake-hotfix-zahnrad-lagerbestellung` (`0a6fb3cd`, `6d0e954e`, Changelog
  [[2026-10-08-v1-30-2-ake-hotfix-zahnrad-lagerbestellung]]). Version, `AppVersion.Date` (H1), Changelogs und
  feature-map ziehe ich nach der Entscheidung nach.
- **B2 entschieden (Gerald 2026-11-07): Zahnrad-Hotfix zuerst in `main` mergen**, danach wird dieser Hotfix **v1.30.3**
  (Version beider `AppVersion.cs` + `Date`, Anwender-Changelog, Kapitel-107-Kopf, Brain-Changelog, feature-map).
  Bis zum Zahnrad-Merge bleibt der Stand unverändert und uncommittet im Hauptcheckout. Danach folgt ein neuer QA-Lauf in
  erhöhter Shell (DEV-Deploy + Browser-Schritte).

## Dev-Nachlauf v1.30.3 (2026-11-08, Auftrag Gerald)

- Zahnrad-Hotfix gemergt: `e386bfce` (`--no-ff`). Konflikte wie erwartet nur in beiden `AppVersion.cs` (Date) und
  `Views/Help/Changelog.cshtml` (beide v1.30.2-Karten untereinander); `docs/TESTSZENARIEN.md` lief automatisch.
  Merge-Stand: Build 0 Fehler, Tests Dienst 202, Web 1111/1 übersprungen.
- v1.30.3: beide `AppVersion.cs` (1.30.3, Date 2026-11-08), Changelog-Karte v1.30.3 (08.11.2026) über der Zahnrad-Karte
  v1.30.2, Kapitel-107-Kopf + Inhaltsverzeichnis-Anker + TS-107.1-Vorbedingung + Fußzeile + Verweis TS-14.2, Code-Kommentare
  „Hotfix v1.30.3“. Brain-Changelog [[2026-11-08-v1-30-3-ake-hotfix-oseon-sync-druck-werkbank]].
- Build 0 Fehler (25 Warnungen, Bestand), Tests Dienst 202, Web 1111/1 übersprungen, alle grün.
- Prüfabfrage IDEAL-Testinstanz beantwortet (siehe oben): kein Handlungsbedarf.

## QA Lauf 2 (2026-11-08) — Code ohne Befund, NICHT Testbereit (DEV-Deploy gescheitert, Umgebung)

Geprüft im Hauptcheckout `C:\Git\IDEAL-AKE-WMS`, Zweig `main`, HEAD `e386bfce`, Shell erhöht (`net session` OK).
Prüfbereich: WIP-Commit `066b136f` (OSEON-Hotfix + Z1–Z3; liegt auf `main` **vor** `3d0dddfe`, daher nicht in
`3d0dddfe..HEAD`), Merge `e386bfce` (Zahnrad `0a6fb3cd`, `6d0e954e`) und die uncommitteten v1.30.3-Änderungen
(`git diff -- IdealAkeWms IDEALAKEWMSService IdealAkeWms.Tests docs`: 10 Dateien, nur Version, Datum, Kommentare, Kap.-107-Kopf).
Status bleibt **InUmsetzung**: Der vorgeschriebene DEV-Deploy (`dev-deploy.ps1`) endete mit Exit 1. Ursache ist die
DEV-Umgebung, nicht der Code (U1). Ersatzweise lief v1.30.3 isoliert gegen eine Kopie der AKE-Test-DB; dort sind
TS-107.5/107.6 im Browser **OK**.

### 1. Build + Tests (eigener Lauf)

```
dotnet build IdealAkeWms.slnx  → 0 Fehler, 24 Warnungen (NU190x-Advisories, Bestand)
dotnet test --no-build         → IDEALAKEWMSService.Tests: Fehler 0, erfolgreich  202, übersprungen 0, gesamt  202
                                 IdealAkeWms.Tests:        Fehler 0, erfolgreich 1111, übersprungen 1, gesamt 1112
```

### 2. Code-Review (selbst, in diesem Lauf)

Geprüft: `OseonSyncService.cs` (Delta `:51-63`, Auswahl/Meldungen `:147-210`, `Counts`, Temp-Tabelle `:322-335`,
Älter-Abfrage + MERGE `:366-458`, AG-Wächter + AG-MERGE `:555-600`), `OseonOrderSelection.cs`, `PickingController`
(ColumnMap `:78`, `WorkplaceName` `:143`, `RenderPrintBomAsync` `:516-551`), `Bom.cshtml:984-992`, `PrintBom.cshtml:152-156`,
`Picking/Index.cshtml`, Changelog, TESTSZENARIEN Kap. 107, Merge-Ergebnis.

**B1-Fix — ältere ID wird nicht zurückgeschrieben: bestätigt.**
- `WHEN MATCHED AND source.[OseonId] >= target.[OseonId]`: Eine per `ON` passende, aber ältere Zeile fällt in **keinen**
  Zweig. Es gibt kein UPDATE, und `WHEN NOT MATCHED` greift nicht, weil `ON` passt. Also kein Rückschlüsseln, kein INSERT, kein 2601.
- Die Älter-Abfrage nutzt dieselbe entdoppelte Quelle wie der MERGE; Count `aeltere_id_uebersprungen` plus Warnzeile mit Referenz.
- **Eigene SQL-Simulation auf `AKE_WMS_DEV`** (SQL wortgleich zum Code, `BEGIN TRAN … ROLLBACK`, Skript `qa2_b1.sql` im Scratchpad):
  - `1662066-65`: gespeichert `1662131`, geliefert `1662130` mit Status 95. Die Älter-Abfrage meldet `1662066-65|1662130|1662131`,
    der MERGE gibt dafür keine OUTPUT-Zeile aus, der Datensatz bleibt unverändert (OseonId 1662131, Status 90, ModifiedAt NULL).
  - `1662066-64`: geliefert `+900000000000` → `UPDATE 1662130 → 900001662130`, gleiche Id 227963, `ModifiedBy=IDEALAKEWMSService`.
  - Neue Nummer `QA2-NEU-01` → `INSERT`. Nach dem ROLLBACK: 0 Zeilen `QA2-NEU-01`.

**B2 — Versionen und Changelog: konsistent.**
- Beide `AppVersion.cs`: `1.30.3` / `2026-11-08`.
- `Changelog.cshtml`: Karte v1.30.3 (08.11.2026, `:12`) über der Zahnrad-Karte v1.30.2 (08.10.2026, `:34`). Die Zahnrad-Karte ist
  zeichengleich zu `6d0e954e` (Diff leer).
- `docs/TESTSZENARIEN.md`: Kap.-107-Kopf, Inhaltsverzeichnis-Anker, TS-107.1-Vorbedingung, Verweis TS-14.2 und Fußzeile auf v1.30.3;
  der Zahnrad-Abschnitt „Zusatz v1.30.2“ (`:3880`) ist vorhanden.

**Merge hat nichts verloren: bestätigt.** `git diff 6d0e954e HEAD -- IdealAkeWms/Views/WarehousePicking/Details.cshtml
IdealAkeWms.Tests/Views/BomZahnradHostTests.cs` ist leer. `BomZahnradHostTests` laufen in den 1111 grünen Web-Tests mit.

**Defektklassen:** kein `@Html.Raw` in `<script>`, kein `hidden`+`d-*`, kein stiller `?? 0`-Fallback (nur
`?? DateTime.MinValue` als Sortierschlüssel), keine Parallel-Arrays (Z2 ist eine geordnete Komma-Liste, `TryAdd` gegen Duplikate),
keine nur clientseitige Regel (Z2: Server sortiert nach der Liste, ohne Liste nach Position). Die Auswahlregel „höchste ID“ läuft in
C# (`OseonOrderSelection`) und SQL (`ROW_NUMBER … OseonId DESC`, `>=`) gleich. Keine neue Einstellung, ADR 0020 entfällt.

| # | Art | Befund | Fundstelle | Behandlung |
|---|---|---|---|---|
| U1 | **Umgebung, blockiert** | `dev-deploy.ps1 -Standort AKE -Quelle C:\Git\IDEAL-AKE-WMS` → Exit 1, „Startseite HTTP 500 - Site gestoppt, Dienst nicht gestartet“.<br>`AKE_WMS_DEV` steht auf Bündel-Schema (letzte Migration `20261107193219_AddPickingAreasAndOrders`); `dbo.OseonOperationConfigs` fehlt, weil das Bündel sie umbenannt hat.<br>`main` bricht beim Seed ab: `Program.cs:401`, `Invalid object name 'dbo.OseonOperationConfigs'`. | `D:\Dev\AKE\Web\logs\stdout_20261108065451_60020.log` | **Mensch:** `AKE_WMS_DEV` aus `D:\akewms.bak` neu herstellen (danach Absicherung) oder den Ersatzlauf (unten) als DEV-Nachweis akzeptieren.<br>Site `AKE_WMS_DEV` und `AKE_WMS_DEV_Service` sind jetzt **gestoppt**. |
| U2 | Umgebung | `dev-deploy.ps1` publisht in den bestehenden Ordner, und `dotnet publish` überschreibt nur ältere Dateien.<br>Beim Wechsel Bündel → `main` bleiben neuere Bündel-Dateien in `wwwroot` liegen: `D:\Dev\AKE\Web\wwwroot\js\site.js` hat 7475 B vom 21.10. statt 5295 B.<br>Das Static-Asset-Manifest nennt aber die `main`-Länge. Der Browser bekommt deshalb abgeschnittenes JS (`SyntaxError: Unexpected end of input` in `site.js`, `column-preferences.js`, `table-filter.js`). | `D:\Dev\scripts\dev-deploy.ps1` (publish-Schleife) | Vorschlag: Zielordner vor dem Publish leeren (außer `appsettings.DEV.json`, `logs`). Nicht Teil des Zweigs. |
| H1 | Hinweis Doku | TS-107.2b: Auftrag Y wird nur gelesen, wenn er im Delta-Fenster liegt. Diese Vorbedingung fehlt; nach TS-107.1 ist `MAX(LastChangedInOseon)` wieder aktuell. | `docs/TESTSZENARIEN.md:6386` | Beim Test einen Y wählen, der in OSEON gerade geändert wurde, oder `LastChangedInOseon` wie in TS-107.1 zurücksetzen. Doku-Ergänzung optional. |
| H2 | Hinweis | Das Anwender-Changelog v1.30.3 nennt die Warnungen „neu ausgeschrieben“ und „mehrfach in OSEON“, nicht aber „ältere ID nicht übernommen“. | `Changelog.cshtml:17-21` | optional |
| H3 | Hinweis, vorbestehend | Ergibt ein Spaltenfilter in `Picking/Index` 0 Treffer, zeigt die View nur „Aktuell sind keine Auftraege …“ ohne Tabelle und Filterzeile. Der Filter lässt sich dann in der Maske nicht mehr löschen, nur über Menü oder URL. Gilt für alle Spalten; mit Werkbank ist der Fall aber leichter erreichbar. | `Views/Picking/Index.cshtml:22-31` | Nur melden, ggf. eigene Aufgabe. |

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweis | Status |
|---|---|---|
| 1 Neuausschreibung | SQL-Simulation (Lauf 1 und 2): UPDATE, gleiche Id, `ModifiedBy`. Count und Warnzeile (C#-Pfad, echter Sync): **Manual-UAT** TS-107.1 | offen (Mensch) |
| 2 AGs an derselben Id; „in jedem Lauf dieselbe Zeile“ | B1 per SQL-Simulation belegt (ältere ID bleibt). Echter Lauf: **Manual-UAT** TS-107.1/107.2b | offen (Mensch) |
| 3 Mehrfachlieferung | Unit-Test `Select_MultipleIds_PicksHighest_InBothOrders` | grün |
| 4 AG-Dedupe | Unit-Tests `Select_Operations_OnlyFromSelectedRow_OncePerPosition`, `Select_DuplicatePosition_LatestStatusReportWins_ThenHighestStatus` | grün |
| 5 Groß/Klein | Unit-Test `Select_CaseInsensitiveNumber_IsOneOrder`; SQL-Wächter in Lauf 1 | grün |
| 6 Normalfall, Counts = 0 | Auswahl per Unit-Test. Protokoll-Counts: **Manual-UAT** TS-107.2 | offen (Mensch) |
| 7 MAX(LastChangedInOseon) rückt vor | **Manual-UAT** TS-107.1 | offen (Mensch) |
| 8 keine Migration, kein SQL | `git show --stat 066b136f`, `git diff --stat`: nichts unter `*/Migrations/` oder `SQL/` | bewiesen |
| 9 Build/Tests inkl. 3 `OseonSyncServiceTests` | eigener Lauf oben | bewiesen |
| Z1 Druckrand | Quelle `PrintBom.cshtml:155` `margin: 28mm 8mm 8mm 8mm`. Die Druckvorschau ist ein Browser-Dialog → **Manual-UAT** TS-107.4 | offen (Mensch) |
| Z2 Druck-Sortierung | Server: `PrintBom_VisiblePositions_KeepClientOrder`. Client: Browser-Ersatzlauf OK (siehe 6.) | belegt (Ersatzumgebung) |
| Z3 Werkbank-Spalte | Server: `Index_WorkbenchColumn_FilledFromWorkplaceAndFilterable`. Browser-Ersatzlauf OK (siehe 6.) | belegt (Ersatzumgebung) |

### 4. Geänderte bestehende Tests

Gegenüber Lauf 1 unverändert: nur `PickingControllerTests.Setup(...)` ist um optionale Parameter erweitert (Fixture, kein
Verhaltenswechsel). In den uncommitteten Änderungen ändern sich dort nur zwei Kommentare (v1.30.2 → v1.30.3). Der Merge bringt
`BomZahnradHostTests` neu mit, unverändert gegenüber dem Zahnrad-Zweig. Kein Befund.

### 5. Checkliste CLAUDE.md

- Migration, SQL, FreshInstall, AgentJobs: entfällt.
- Audit-Felder: unverändert, das UPDATE setzt `Modified*`.
- Version in beiden `AppVersion.cs` plus Datum: ok.
- Anwender-Changelog: ok.
- Brain-Changelog [[2026-11-08-v1-30-3-ake-hotfix-oseon-sync-druck-werkbank]]: vorhanden.
- feature-map-Zeile: vorhanden (`feature-map.md:88`, InUmsetzung).
- TESTSZENARIEN Kap. 107 und Index: ok.
- ADR 0020: entfällt.

### 6. DEV-Deploy und Testlauf

- **Standort:** `AKE` (Frontmatter).
- **Deploy (Pflichtweg):** `powershell -ExecutionPolicy Bypass -File D:\Dev\scripts\dev-deploy.ps1 -Standort AKE -Quelle C:\Git\IDEAL-AKE-WMS`
  - Publish von Web und Dienst ok, dann „Startseite HTTP 500 - Site gestoppt, Dienst nicht gestartet.“, **Exit 1** (U1). Die Fußzeile war nicht prüfbar.
  - **QA-Punkt 6 ist damit formal fehlgeschlagen.**
  - Site `AKE_WMS_DEV` und Dienst `AKE_WMS_DEV_Service` sind gestoppt. In `D:\Dev\AKE\Web` liegen v1.30.3-DLLs mit teils veralteten Bündel-`wwwroot`-Dateien (U2).
- **Ersatzlauf** (nur lokal; an IIS, AKESQL20 und den Testservern nichts geändert):
  - `D:\akewms.bak` als **neue** DB `AKE_WMS_QA_MAIN` wiederhergestellt (Dateien unter `D:\Dev\QA\`). Das Schema entspricht `main`
    (letzte Migration `20260806120650_AllowMultipleDummyRequisitionItems`).
  - DEV-Absicherung angewendet (`dev-absicherung-ake.sql` mit `USE [AKE_WMS_QA_MAIN]`), Ergebnis `NichtFalse = 0`.
  - Web v1.30.3 sauber nach `D:\Dev\QA\Web` publisht, `appsettings.DEV.json` auf `AKE_WMS_QA_MAIN`, Kestrel auf `http://localhost:5182`.
  - Unter Kestrel geht die Windows-Anmeldung nicht. Deshalb gibt es den Testbenutzer `QA-Agent` ohne Passwort mit Rolle admin, nur in `AKE_WMS_QA_MAIN`.
  - **Fußzeile `© 2026 IDEAL-AKE WMS v1.30.3`** = AppVersion. Der Dienst wurde im Ersatzlauf nicht gestartet (Sync-Schalter aus, kein Nutzen).

| Schritt | Ausgeführt | Ergebnis | Bewertung |
|---|---|---|---|
| TS-107.1 Neuausschreibung (echter Sync) | nein | braucht `Sync:OseonTrackingEnabled=true` (harte Grenze) | NICHT MÖGLICH |
| TS-107.1/107.2b Ersatz: MERGE-SQL | ja, `AKE_WMS_DEV`, ROLLBACK | siehe 2. (B1): ältere ID bleibt, neue ID wird übernommen, Insert ok | OK (nur SQL-Teil) |
| TS-107.2 Normallauf | nein | wie 107.1 | NICHT MÖGLICH |
| TS-107.3 Altstand 2601 | nein | optional | NICHT MÖGLICH |
| TS-107.4 Druckrand | teilweise | Ausgelieferte View hat `@page margin: 28mm 8mm 8mm 8mm`. Die Druckvorschau ist ein Browser-Dialog und wurde nicht ausgelöst. | NICHT MÖGLICH (Mensch) |
| TS-107.5 Sortierung „Bezeichnung 1“ ▲ → Drucken | ja | Druck mit 150 Positionen, Reihenfolge = Ansicht (6.9.6 Abdeckprofi Lampe, 9.1, 10.2, 7.6.2 …, Ende 4.15 Zylinderschraube) | OK |
| TS-107.5 Filter Pos „15“ + Sortierung „Bezeichnung 2“ ▲ → Drucken | ja | Druck „Filter: Spalte position=15“, 13 Positionen, Reihenfolge = Ansicht (15.9, 15.10, 15.12, 15.13, 15, 15.8, 15.1 …) | OK |
| TS-107.5 Negativfall ohne Sortierung und Filter | ja | Druck nach Position (2.1, 2.2, 2.3, 2.4 …) | OK |
| TS-107.5 aus FA-Abarbeitungsliste/FA-Vervollständigung | nein | gleiche View, gleicher Code, nicht separat geklickt | offen (Mensch) |
| TS-107.6 Spalte „Werkbank“ nach „Kunde“, gefüllt | ja | FA 2614027 → `D2` | OK |
| TS-107.6 Spaltenfilter serverseitig | ja | `A1` → 0 Treffer (URL `colf_workbench=a1`), `d2` → 1 Treffer (Groß/Klein egal). Mehrere Seiten mangels Daten nicht prüfbar (nur 1 freigegebene FA). | OK (eine Seite) |
| TS-107.6 Spaltenauswahl | ja | „Werkbank“ steht in „Spalten konfigurieren“; Abwählen blendet sie aus, die Einstellung wird gespeichert (`UserViewPreferences`: `workbench visible:false`) | OK |
| TS-107.6 gespeicherte Altauswahl ohne `workbench` | ja | `SettingsJson` ohne `workbench` und mit `customer visible:false` gesetzt → Kunde ausgeblendet, Werkbank sichtbar | OK |

**Aufräumen (offen, Mensch):**
- Die Ersatzlauf-Prozesse `dotnet IdealAkeWms.dll` auf `localhost:5181` und `localhost:5182` sind inzwischen beendet
  (Exit 255, nicht durch QA gestoppt).
- DB `AKE_WMS_QA_MAIN` und Ordner `D:\Dev\QA\` können nach Belieben entfernt werden.

### Was bis Testbereit fehlt

1. U1 auflösen, auf einem von zwei Wegen:
   - `AKE_WMS_DEV` auf `main`-Schema bringen (Restore `D:\akewms.bak`, dann `dev-absicherung-ake.sql`). Danach `dev-deploy.ps1 -Standort AKE`
     erneut ausführen, am besten nach dem U2-Fix oder mit geleertem `D:\Dev\AKE\Web\wwwroot`, und die Fußzeile v1.30.3 prüfen → Testbereit.
   - Oder Gerald akzeptiert den Ersatzlauf als DEV-Nachweis.
2. Danach steht die AKE-DEV-Umgebung auf `main`. Ein späterer Bündel-Deploy nach AKE migriert sie wieder vorwärts.

### Manuelle Testcheckliste (Mensch)

1. AKE-Test (Dienst + Web v1.30.3 aus `main`), TS-107.1: Erfolg, gleiche Id, `neu_ausgeschrieben=1`, Warnzeile, AGs nicht weniger, `MAX(LastChangedInOseon)` rückt vor.
2. TS-107.2: alle vier neuen Counts stehen auf 0.
3. TS-107.2b mit einem Auftrag Y **im Delta-Fenster** (H1): ID bleibt, `aeltere_id_uebersprungen=1`, Warnzeile; danach Y zurücksetzen.
4. TS-107.3 (optional): Altstand v1.30.1 → Fehler 2601.
5. TS-107.4 Druckvorschau: oben ca. 28 mm Rand, auch auf Folgeseiten, nichts abgeschnitten.
6. TS-107.5: zusätzlich aus FA-Abarbeitungsliste und FA-Vervollständigung drucken.
7. TS-107.6: Filter über mehrere Seiten prüfen (Testsystem mit mehr freigegebenen FAs).
8. Zahnrad v1.30.2 (TESTSZENARIEN „Zusatz v1.30.2“, `:3880`) im selben Testlauf mitprüfen.

## Entscheidung Gerald 2026-11-08 – Ersatznachweis QA Lauf 2 akzeptiert

Wie beim Foto-Hotfix: Code-Review ohne Befund (B1-Fix, B2-Versionen/Changelog, Merge `e386bfce` vollständig) plus Browser-Ersatzlauf auf der
DB-Kopie `AKE_WMS_QA_MAIN` (main-Schema, Kestrel, v1.30.3: TS-107.5/107.6 OK) gelten als DEV-Nachweis. Befund U1 (AKE_WMS_DEV mit
Bündel-Schema, main startet dort nicht) ist ein Umgebungsbefund, kein Code-Befund. **Status: Testbereit** (Commit v1.30.3 `1c7fc333` auf `main`).
Offen für den Menschen am Testsystem: TS-107.1/107.2/107.2b (brauchen `Sync:OseonTrackingEnabled`), TS-107.4 Druckvorschau, Druck aus
FA-Abarbeitungsliste/FA-Vervollständigung. Hinweise H1/H2 aus Lauf 2 nicht blockierend.
