# FA-Zusatzinfos aus Sage — Design (v1.26.0)

**Datum:** 2026-07-22 (Rev. 2 nach adversarialem 3-Lens-Review: Sync/Datenmodell, UI/Spalten, Betrieb/Doku; Rev. 3 ergaenzt §10 Auto-Erledigt; Rev. 4 haertet §10 nach 2-Lens-Review; Rev. 5 ergaenzt §10.6 BDE-Buchungs-Sperre; Rev. 6 praezisiert §10.6 nach Code-Gegenpruefung)
**Status:** approved-design; §1–§9 implementiert (Commits `89dd95e..8f5f350`); §10 implementiert (Fold 2, Commits `e31dad7..429e803`)
**Worktree:** `.claude/worktrees/pa-zusatzinfos`, Branch `feature/pa-zusatzinfos` (Basis main `9bdbedf`)

## 0. Glossar / Naming

| Kontext | Name |
|---------|------|
| UI- und Doku-Label (Changelog, Hilfe, TESTSZENARIEN-Titel, Setting-Beschreibung) | **„FA-Zusatzinfos (Sage)"** |
| Reiter in der FA-Vervollstaendigung | **„ALLGEMEIN"** |
| Sage-View | `dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen` |
| Entity / Tabelle | `ProductionOrderExtraInfo` (englisch, Muster `ProductionOrderPickingStatus`) |
| Sync-Service / Protokoll-Name / Setting | `FaZusatzinfoSyncService` / `FaZusatzinfo` / `Sync:FaZusatzinfoEnabled` |
| Worktree/Branch (nur intern) | `pa-zusatzinfos` / `feature/pa-zusatzinfos` |

„PA-Zusatzinfos" ist nur der interne Arbeitsname — in der UI heisst alles „FA" (Konvention der App).

## 1. Ziel

Zusaetzliche FA-Informationen aus Sage (Kaeltemittel, Ventil, Ausfuehrung E/Z,
Maschine, Sage-Status) werden per Service-Sync in eine **neue Tabelle**
uebernommen und read-only in vier Views angezeigt:

1. **FA-Vervollstaendigung**: neuer erster Reiter **„ALLGEMEIN"** (read-only).
2. **FA-Abarbeitungsliste**: 5 neue Spalten, **Default eingeblendet**.
3. **FA-Liste** (`ProductionOrders/Index`): 5 neue Spalten, **Default ausgeblendet**.
4. **Leitstand** (`PickingLeitstand/Index`): 5 neue Spalten, **Default ausgeblendet**.

Die Daten sind Sage-Master und in der App **nirgends editierbar**.

## 2. Datenquelle

Sage stellt eine neue View bereit (liefert **fertige Texte**, die CASE-Logik
fuer den Status lebt in der View — nicht bei uns):

```sql
SELECT CAST([WA Nummer] AS nvarchar(100))       AS WaNummer,
       CAST(Kaeltemittel AS nvarchar(200))      AS Kaeltemittel,
       CAST(Ventil AS nvarchar(200))            AS Ventil,
       CAST([Ausfuehrung E/Z] AS nvarchar(200)) AS AusfuehrungEZ,
       CAST(Maschine AS nvarchar(200))          AS Maschine,
       CAST(Status AS nvarchar(200))            AS Status
FROM dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen
```

- Zugriff ueber die bestehende **`SageConnection`**, immer mit `dbo.`-Praefix.
- **Defensive CASTs** (Muster der bestehenden Sage-Reads): hartes Abschneiden an
  der Quelle statt Truncation-Fehler beim Write. Die 200er-Laengen beim ersten
  Lauf gegen die reale View-Definition verifizieren.
- Spaltenname ist `Ausfuehrung E/Z` („Ausfuerhung" in der Brainstorming-Antwort
  war ein bestaetigter Tippfehler).
- `[WA Nummer]` entspricht `ProductionOrders.OrderNumber` (UNIQUE-Index auf
  dieser Linie, `ApplicationDbContext.cs:384`).
- **View-Existenz-Guard (Pflicht):** Vor dem Read
  `IF OBJECT_ID('dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen','V') IS NULL` →
  Warn-Zeile „View nicht vorhanden — Sync uebersprungen" + Lauf regulaer beenden,
  **kein throw, keine Fehlermail** (analog Zeroing-Leer-Guard). Verhindert
  Mail-Spam alle 15 min, wenn das Gate auf einem System ohne View aktiviert wird.
- **IDEAL-Interop:** Auf der IDEAL-Linie (`SubOrderNumber NOT NULL + UNIQUE`)
  koennen je WA-Nummer MEHRERE `ProductionOrders`-Zeilen existieren. Der
  WMS-Match wird deshalb **mehrfach-treffer-faehig** implementiert (Upsert je
  gematchter `ProductionOrders.Id`; die Zusatzinfo gilt je WA und wird ggf. je
  Sub-FA dupliziert — der UNIQUE-FK bleibt korrekt). Auf dieser Linie ist der
  Treffer wegen des UNIQUE-Index immer genau einer.
- Referenz (nur Doku): Quelle der View ist `vw_AKE_Kommissionierung_WAListe`
  LEFT JOIN `KHKIFAuftraege ia ON v.[WA Nummer] = ia.BelID` mit
  `USER_FAStatus`/`USER_IstVerladen` → Status-Texte
  `verpackt`/`abgeholt`/`in Produktion`/`begonnen`/`unbekannt`.

## 3. Datenmodell (1:1-Satellit, Ansatz A)

Muster wie `ProductionOrderPickingStatus`/`ProductionOrderBdeStatus`
(verifiziert `ApplicationDbContext.cs:396-445`):

- **Entity `ProductionOrderExtraInfo`** (Tabelle `ProductionOrderExtraInfo`),
  erbt `AuditableEntity`:
  - `ProductionOrderId` (int, **UNIQUE-FK**, Index
    `UQ_ProductionOrderExtraInfo_ProductionOrderId`, `OnDelete(Cascade)`)
  - `Kaeltemittel`, `Ventil`, `AusfuehrungEZ`, `Maschine`, `SageStatus` —
    alle `nvarchar(200)` NULL (deutsche Property-Namen = Sage-Domaenenvokabular,
    1:1 zur View nachvollziehbar).
- Navigation: `ProductionOrder.ExtraInfo` (`WithOne`, wie PickingStatus).
- **Migration 81** (`AddProductionOrderExtraInfo`) +
  `SQL/81_AddProductionOrderExtraInfo.sql` (OBJECT_ID-Guard, idempotent,
  History-Insert in separatem Batch) + `SQL/00_FreshInstall.sql`-Spiegelung
  (BEIDE Stellen: Schema + MigrationId). **Rein additiv** — kein Backup-Zwang.
- **Kein AgentJob-Eager-Create:** Anders als PickingStatus/BdeStatus bekommt
  `ProductionOrderExtraInfo` bewusst KEINEN Folge-MERGE im AgentJob
  `SQL/AgentJobs/01_*` — Zeilen entstehen ausschliesslich im neuen Sync.

## 4. Sync

**Architektur (EF-Write, Reader-Seam — Muster `LagerbestandSyncService`):**

- **`ISageZusatzinfoReader`** (raw SQL NUR fuer den Sage-Read inkl.
  View-Existenz-Guard; im Test durch `FakeSageZusatzinfoReader` ersetzt).
  Rueckgabe: `record SageZusatzinfoReadResult(bool ViewExists,
  IReadOnlyList<SageZusatzinfoRow> Rows)`; `SageZusatzinfoRow` = Record mit
  den 6 Textfeldern aus §2.
- **`FaZusatzinfoSyncService : IFaZusatzinfoSyncService`** mit
  `Task<SyncResult> SyncAsync(bool dryRun, CancellationToken ct)` —
  schreibt per **EF Core ueber den shared `ApplicationDbContext`**
  (CLAUDE.md-Konvention; KEIN raw-SQL-Write). Damit ist der gesamte
  Entscheidungs- UND Schreibpfad InMemory-testbar — kein Deko-Helper.
- **DI:** `AddScoped<ISageZusatzinfoReader, SageZusatzinfoReader>()` +
  `AddScoped<IFaZusatzinfoSyncService, FaZusatzinfoSyncService>()` in
  `IDEALAKEWMSService/Program.cs`.

**Ablauf je Lauf:**

1. `BeginRunAsync(SyncLogServices.FaZusatzinfo)` (neue Konstante + `All`).
2. Reader: View-Guard → fehlt: Warn-Zeile + regulaeres Lauf-Ende (Count
   `uebersprungen=0`, Message „View nicht vorhanden"); sonst alle Zeilen lesen.
3. **Einmaliger Set-Read** der FA-Zuordnung (kein Zeile-fuer-Zeile-Roundtrip):
   `ProductionOrders` mit `OrderNumber IN (gelesene WA-Nummern)` inkl.
   vorhandener `ExtraInfo`-Satelliten laden → Dictionary
   `OrderNumber → List<ProductionOrder>` (mehrfach-treffer-faehig, §2).
4. Je View-Zeile / je gematchter FA:
   - kein FA-Treffer → **uebersprungen** (Count; keine Warn-Zeile je WA —
     alte/erledigte WAs sind normal),
   - kein Satellit → **neu** (Insert),
   - Satellit vorhanden + mindestens 1 Feld geaendert → **aktualisiert**
     (Update; haelt `ModifiedAt` aussagekraeftig),
   - unveraendert → kein Write, kein Count.
5. `FinishSuccessAsync` mit Counts `gelesen` / `neu` / `aktualisiert` /
   `uebersprungen` (+ `[DryRun]`-Suffix).

**Semantik & Konventionen:**

- **DryRun:** voller Plan inkl. echter Would-be-Counts (Reads erlaubt,
  `SaveChangesAsync` wird uebersprungen) — Reconciler-Muster, NICHT das
  Artikel-Muster (das bricht nach dem Read ab und meldet Nullen).
- **Audit-Felder (EF-Pfad, Muster `FaWorkStepDetectionService`):**
  `CreatedBy = CreatedByWindows = "FaZusatzinfoSync"` (Modified analog),
  Zeitstempel `DateTime.Now` (Lokalzeit — konsistent mit SyncLog-Konvention).
- **Kein Loeschen:** Verschwindet ein WA aus der View, bleibt der letzte
  bekannte Stand stehen (Info bleibt fuer erledigte FAs sichtbar). Kein Cap
  noetig (keine destruktive Operation).
- **Fehlerpfad:** Exception → `run.LogErrorAsync` + `FinishFailedAsync` +
  rethrow (→ `RunResilientAsync` → optionale Fehlermail). Der View-fehlt-Fall
  ist bewusst KEIN Fehlerpfad (§2 Guard).

**Einbindung `SyncWorker`:**

- Eigener Block **direkt NACH dem FA-Import-Block**, Gate
  `ServiceSettings.GetBoolSafeAsync("Sync:FaZusatzinfoEnabled", default false)`,
  gekapselt in `RunResilientAsync`.
- **Resolve INNERHALB des gegateten Blocks** (`scope.ServiceProvider
  .GetRequiredService<IFaZusatzinfoSyncService>()` erst nach dem Gate-Check) —
  sonst werfen die bestehenden `SyncWorkerTests` (Mock-Provider kennt nur
  `ISageImportService`) und der aeussere catch killt den Restzyklus.
- **Akzeptiertes Verhalten:** Schlaegt der FA-Import fehl (oder ist er
  deaktiviert), laeuft der Zusatzinfo-Sync trotzdem — gegen den alten FA-Stand;
  neue WAs zaehlen dann als `uebersprungen` und heilen sich im Folgezyklus.
  (Erhoehte Skip-Counts nach FA-Import-Fehlern sind KEIN Bug.)
- **Katalog:** `ServiceSettingDefinitions.All` + `InlineData` im Drift-Guard:
  `Sync:FaZusatzinfoEnabled`, Bool, Default `false`, Kategorie „Sync",
  Beschreibung „FA-Zusatzinfos (Sage): Kaeltemittel/Ventil/Ausfuehrung/
  Maschine/Status je FA synchronisieren".

## 5. UI

### 5.1 FA-Vervollstaendigung — Reiter „ALLGEMEIN"

- Neuer **erster** Reiter „ALLGEMEIN" als Pseudo-Tab (NICHT in `Model.Tabs`):
  eigenes `<li>` VOR der `Model.Tabs`-Schleife, ohne ✓/●-Indikator
  (der basiert auf `IsSpecComplete`, das es hier nicht gibt).
- **View-Verzweigung:** `if (Model.ActiveTab == "ALLGEMEIN") { read-only Pane }`
  laeuft VOR dem bestehenden `active == null`-Alert-Zweig — sonst frisst der
  „keine AGs aktiv"-Alert den ALLGEMEIN-Inhalt bei FAs MIT AG-Reitern.
- **Controller (`FaCompletionController.Edit`):** `ALLGEMEIN` als gueltigen
  Tab-Wert zulassen, Vergleich **`OrdinalIgnoreCase`**. **Default-aktiv bleibt
  der erste FA-Vorbau-AG-Reiter** (User-Entscheid); FA ganz ohne AG-Reiter →
  ALLGEMEIN ist aktiver (einziger) Reiter, der bisherige „+ FA-Vorbau-AG
  hinzufuegen"-Hinweis erscheint zusaetzlich unter dem ALLGEMEIN-Pane.
- **Kollisionsschutz:** Code `ALLGEMEIN` wird im `WorkStepsController`-CRUD
  als **reserviert** abgelehnt (App-Layer-Validierung neben dem
  Duplicate-Check, case-insensitiv, InMemory-testbar).
- **Ladepfad:** `GetByIdAsync` laedt keine Navigationen (generisches
  `FindAsync`) — neue gezielte Repo-Methode
  `IProductionOrderRepository.GetExtraInfoAsync(int productionOrderId)`
  (ein Read auf den Satelliten), ViewModel bekommt die 5 nullable Strings.
- Inhalt: read-only `dl` (Kaeltemittel, Ventil, Ausfuehrung E/Z, Maschine,
  Sage-Status), KEINE Eingabefelder/POSTs. Ohne Satellit: Hinweis
  „Noch keine Zusatzinformationen aus Sage vorhanden."
- **Tab-Erhalt-Nit (mitfixen):** `SetWorkplace`/`RemoveWorkStep` redirecten
  heute ohne `tab` — hidden `tab`-Input ergaenzen, damit man nach einer
  Werkbank-Aenderung auf ALLGEMEIN bleibt.

### 5.2 FA-Abarbeitungsliste (`FaWorklist/Index`)

- Datenpfad: `GetAllOrderedAsync` bekommt `.Include(o => o.ExtraInfo)`
  (Nebenwirkung: auch `FaCompletion/Index` laedt den 1:1-LEFT-JOIN mit —
  akzeptiert, unkritisch). `FaWorklistRow` + Mapping um die 5 Felder erweitern.
- 5 neue Spalten, Col-Keys `kaeltemittel`, `ventil`, `ausfuehrung`,
  `maschine`, `sage-status`; Header „Kaeltemittel", „Ventil",
  „Ausfuehrung E/Z", „Maschine", „Sage-Status". **Default eingeblendet.**
- Server-seitige Spaltenfilter: `BuildColumnMap` um 5 Getter erweitern
  (gerenderter Zellentext, leer = ""), Filter laeuft wie bisher in-Memory VOR
  der Pagination.
- `#column-config`-JSON + `<th data-col-key>` erweitern; **colspan-Konstante**
  `columnCount` (heute `10 + Attribute + 1`) auf `15 + …` anpassen.
- **Bestehenden Prefs-Bug mitfixen:** `ColumnDefinitions.GetByViewKey` kennt
  „FaWorklist" nicht → die Prefs-API antwortet 400 und Zahnrad-Einstellungen
  gehen bei jedem Reload verloren. Fix: ViewConfig „FaWorklist" registrieren
  (Validierung prueft nur den ViewKey; die dynamischen `attr-{id}`-Spalten
  stoeren nicht). Ohne diesen Fix ist „ausblendbar" nur bis zum Reload wahr.

### 5.3 FA-Liste (`ProductionOrders/Index`) + Leitstand (`PickingLeitstand/Index`)

- **Datenpfad (Korrektur aus Review):** BEIDE Views laden ueber
  `GetForLeitstandAsync` → **Projektion** in das Record `LeitstandOrderRow` —
  ein `.Include()` waere dort wirkungslos. Stattdessen: `LeitstandOrderRow` um
  die 5 nullable Strings erweitern und in der Projektion
  `o.ExtraInfo != null ? o.ExtraInfo.Kaeltemittel : null` etc. selektieren
  (bedient FA-Liste UND Leitstand in einem Zug).
- **Spaltenfilter (Korrektur aus Review):** Die 5 Keys kommen in den
  **SQL-Filter-Switch** (`ApplyLeitstandColumnFilter`) — analog der
  bestehenden Text-Keys (Komma-OR/`!`-NOT-Semantik, `Contains`-basiert und
  InMemory-testbar, KEIN `EF.Functions.Like`), mit Null-Guards auf
  `o.ExtraInfo`. Vorteil: SQL-Pagination bleibt erhalten (kein
  Force-Full-Load — die Werte sind Rohstrings, keine berechneten Termine).
  Unbekannte Keys wuerden sonst im Switch-Default STILL ignoriert.
- 5 Spalten in beiden Views: `<th data-col-key>` + beide inline
  `#column-config`-JSONs + `ColumnDefinitions.cs` (beide View-Keys) —
  **Drei-Stellen-Sync** (Record + 2 inline JSONs) als Fallstrick beachten.
  Empty-Row-`colspan`-Konstanten beider Views anpassen.
- **Default ausgeblendet — per-Key-Fallback (Korrektur aus Review):**
  `ColumnDef` bekommt ein `DefaultHidden`-Flag (+ `defaultHidden` im JSON).
  In `column-preferences.js` verwenden `buildDefaultSettings()` UND
  `mergeWithDefaults()` als Fallback `visible: !c.defaultHidden` — heute steht
  dort hart `true`, wodurch neue Spalten bei JEDEM User mit gespeicherten
  Prefs sichtbar wuerden (Gegenteil des Ziels). Gespeicherte Einstellungen
  gewinnen weiterhin **je Spalte** (wer je explizit ein-/ausgeblendet hat,
  behaelt das).
- Hinweis (bestehendes Verhalten, dokumentieren): Bei ausgeblendeter Spalte
  ist auch deren Filterzelle versteckt — ein per URL geteilter
  `?colf_kaeltemittel=…` filtert dann unsichtbar.

## 6. Tests

- **Sync end-to-end InMemory** (`FaZusatzinfoSyncServiceTests` mit
  `FakeSageZusatzinfoReader` + `FakeSyncLogger`): neu / aktualisiert /
  unveraendert / kein-FA-Treffer (`uebersprungen`) / mehrere FAs je WA
  (IDEAL-Fall) / DryRun (echte Counts, kein Write) / View-fehlt-Guard
  (Warn + regulaeres Ende, kein Throw) / Fehlerpfad (FinishFailed + rethrow).
- **SyncWorker:** Fail-Safe-Invariante (Gate default false → Block laeuft
  nicht, kein Resolve, kein Crash) analog bestehender `SyncWorkerTests`.
- **Drift-Guard:** neuer Key in `ServiceSettingDefinitionsTests`.
- **Controller:** `FaCompletionController.Edit` (`?tab=ALLGEMEIN` aktiv +
  Felder im ViewModel; Default-Tab unveraendert; FA ohne AGs → ALLGEMEIN);
  `WorkStepsController` (Code „ALLGEMEIN"/„allgemein" reserviert → abgelehnt);
  FaWorklist/ProductionOrders/PickingLeitstand-Spaltenfilter auf die neuen
  Keys (inkl. Null-ExtraInfo-Zeilen); Prefs-API akzeptiert ViewKey
  „FaWorklist" (kein 400 mehr).
- **JS (`defaultHidden`-Fallback) ist nicht unit-getestet** → Manual-UAT
  (Kap. 55, insb. Bestands-User mit gespeicherten Prefs).

## 7. Deploy / Cutover (Reihenfolge zwingend)

1. **Sage-View verifizieren** (`SELECT TOP 1 …` am Zielsystem; Spaltennamen +
   Laengen gegen §2 pruefen).
2. **Web deployen** → `Database.Migrate()` legt Tabelle an, Katalog-Seed macht
   `Sync:FaZusatzinfoEnabled` in `/ServiceSettings` sichtbar (Default aus).
3. **Windows-Service publishen** (Gate ist aus → noch kein Lauf).
4. `WorkerSettings:SyncDryRun = true` + Gate aktivieren → einen Zyklus
   abwarten, Aktivitaets-Protokoll pruefen (`gelesen/neu/…` plausibel;
   **explizit `erledigt-gesetzt` kontrollieren — Erstlauf-Aufraeum-Zahl!**
   Bei `erledigt-kandidaten > Cap`: Cap temporaer erhoehen oder in Etappen
   scharfschalten).
5. DryRun aus → scharf.

Der View-Guard (§2) haelt den Fall „Gate an, View fehlt" mail-frei; die
Reihenfolge Web-vor-Service verhindert „Tabelle fehlt"-Fehler (Gate ist ohnehin
erst nach dem Web-Seed schaltbar).

## 8. Doku & Version

- **Version v1.26.0** (Web + Service `AppVersion`), Changelog-Card
  „FA-Zusatzinfos (Sage)".
- **Hilfeseite:** FA-Vervollstaendigung (Reiter ALLGEMEIN), FA-Abarbeitungsliste
  (5 Spalten, Default sichtbar), **FA-Liste + Leitstand** (5 Spalten, Default
  versteckt, per Zahnrad einblendbar — sonst findet sie niemand),
  Aktivitaets-Protokoll-Filterliste (`FaZusatzinfo`), Service-Konfig-Hinweis
  (`Sync:FaZusatzinfoEnabled` aktivieren).
- **CLAUDE.md:** Service-Konfig-Tabelle (neuer Key) + Fallstrick-Eintrag
  (Satellit ohne Loeschen, Opt-in-Gate, View-Guard, LeitstandOrderRow-Projektion,
  defaultHidden-Mechanik). **README:** AppSettings-/Service-Tabelle UND die
  Prosa-Sync-Aufzaehlung im Windows-Service-Abschnitt. **PROJECT_STATUS.md:**
  Eintrag v1.26.0.
- **TESTSZENARIEN Kapitel 55** + Index-Zeile + „Stand:"-Zeile im Kopf
  hochziehen. UAT-Faelle: Gate aus→an, DryRun, View fehlt (Warn-Skip, keine
  Mail), WA ohne FA (`uebersprungen`), FA ohne Zusatzinfo (Hinweis im Reiter,
  leere Zellen), Update-Fall (Sage-Aenderung → `aktualisiert` + ModifiedAt),
  Gate an→aus (letzter Stand bleibt), **Bestands-User mit gespeicherten
  Spalten-Prefs sieht die neuen Spalten in FA-Liste/Leitstand NICHT** (und in
  der Abarbeitungsliste SCHON), Reiter ALLGEMEIN inkl. reserviertem
  WorkStep-Code.
- **Junction-Fallstrick (Worktree):** `secondbrain/docs` + `secondbrain/sql`
  sind im Worktree ECHTE Kopien (keine Junctions). Jede Doku-/SQL-Aenderung in
  BEIDEN Pfad-Sichten identisch committen (inkl. dieser Spec selbst); nach dem
  Merge in main den Hash-Check wiederholen
  (`git ls-files -s <A> <B>` — gleiche Blob-Hashes).

## 9. Nicht-Ziele

- Kein Editieren der Zusatzinfos in der App (Sage ist Master).
- Kein Loeschen/Reconciliation der Satellit-Zeilen; kein AgentJob-MERGE.
- Keine neuen Rollen/Zugriffsaenderungen (Sichtbarkeit folgt den bestehenden
  View-Berechtigungen); `RoleOverview` unveraendert.
- Keine Anzeige in weiteren Views (Picking, Bestand, BDE) — YAGNI.

## 10. Erweiterung (Fold 2): Auto-Erledigt bei Sage-Status „verpackt"/„abgeholt"

**User-Entscheidungen (Brainstorming 2026-07-22):** Flag = `PickingStatus.IsDonePicking`
(App-Komm-Erledigt); Einweg (Erledigt bleibt bei Status-Rueckfall); KEIN eigener
Schalter (Automatik laeuft immer mit `Sync:FaZusatzinfoEnabled`); gefaltet in
v1.26.0 auf `feature/pa-zusatzinfos` (kein Schema-Change, keine Migration,
kein Versions-Bump).

### 10.1 Logik (im bestehenden `FaZusatzinfoSyncService`, gleicher Lauf)

- **Trigger je gematchter FA-Zeile, UNABHAENGIG vom Feld-Diff:** Der Done-Check
  laeuft innerhalb `foreach (var order in matches)` **NACH der bestehenden
  if/else-if-Upsert-Kette** (nach FaZusatzinfoSyncService.cs:137) — die Kette
  hat kein fruehes `continue`, der Check greift also auch im
  unveraendert-Zweig. Statusquelle ist **`row.Status`** (der frische View-Wert),
  NICHT `info.SageStatus` (im Insert-Zweig existiert `info` noch nicht, im
  DryRun waere es veraltet).
- **Bedingung:** Sage-Status (getrimmt, `OrdinalIgnoreCase`) ist `verpackt`
  ODER `abgeholt` UND FA offen nach App-Konvention (Tripel):
  `!IsDone && !IsCancelled && !PickingStatus.IsDonePicking`. Sage-`IsDone`
  wird NIE beschrieben; stornierte FAs werden uebersprungen (sonst kaeme eine
  via Reconciliation reaktivierte FA mit klebendem `IsDonePicking` dauerhaft
  versteckt zurueck — ist sie weiterhin abgeholt, setzt sie der naechste Lauf).
- **Aktion:** `PickingStatus.IsDonePicking = true` + Audit
  (`ModifiedBy/ModifiedByWindows = "FaZusatzinfoSync"`, `ModifiedAt = DateTime.Now`).
  Damit verschwindet die FA aus allen offenen Sichten (FA-Liste, Komm-Worklist,
  FA-Vervollstaendigung, FA-Abarbeitungsliste, BomCache/Detection) — identisch
  zum manuellen Abschliessen-Button. **Auch teilkommissionierte FAs werden
  geschlossen** (bereits auf den Wagen gebuchte Teile bleiben dort stehen, wie
  beim manuellen Abschluss) — bewusste Semantik.
- **Umsetzung DIREKT am getrackten Objekt** (`order.PickingStatus`), NICHT
  ueber `IProductionOrderPickingStatusRepository.SetIsDonePickingAsync` — die
  Repo-Methode ruft sofort `SaveChangesAsync` (DryRun-Leck + Save je FA statt
  Batch).
- **Fehlende `PickingStatus`-Zeile** (Altbestand-Randfall): Zeile im
  Service-Kontext anlegen (`_ctx.ProductionOrderPickingStatuses.Add`, Muster
  `SetFieldAsync`-Eager-Create; Pflichtfelder `CreatedBy`/`CreatedByWindows` =
  `"FaZusatzinfoSync"`, `CreatedAt = DateTime.Now`), `IsDonePicking = true`.
- **DryRun (Guard-VOR-Mutation, wie der Upsert-Teil des Service):** `if (!dryRun)`
  liegt UM die Mutation bzw. das `Add`; Kandidaten-Erfassung/Counts laufen
  AUSSERHALB des Guards. Im DryRun wird KEINE getrackte Entity mutiert (der
  Service laeuft im zyklusweiten Scope — eine mutierte Entity koennte ein
  spaeterer `SaveChangesAsync` desselben Scopes mitflushen). Der irrefuehrende
  Service-Docblock („nur SaveChanges wird uebersprungen") wird im selben Fold
  korrigiert.
- **Set-Read-Erweiterung:** der bestehende Dictionary-Load laedt zusaetzlich
  `.Include(o => o.PickingStatus)` (ein LEFT JOIN mehr, kein zweiter Roundtrip).
- **Mehrfach-Treffer (IDEAL-Linie):** Pruefung/Aktion je gematchter FA-Zeile.
  Konsequenz: der WA-granulare Sage-Status schliesst ALLE Sub-FAs der WA (eine
  WA → N Counts + N Detailzeilen) — bewusste Semantik.
- **Einweg + Ping-Pong-Semantik (ehrlich dokumentiert):** Verlaesst der Status
  verpackt/abgeholt wieder, bleibt Erledigt bestehen (kein Herkunfts-Marker,
  kein Auto-Reopen). Manuelles Wieder-Oeffnen geht ueber FA-Liste/Leitstand →
  „Erledigte anzeigen" → Erledigt-Toggle (`PickingController.ToggleDone`,
  Rolle picking/admin; kein Bulk-Reopen) — **haelt aber nur, bis der naechste
  Lauf denselben Sage-Status sieht**: solange Sage weiter verpackt/abgeholt
  meldet, schliesst die Automatik erneut (bewusst, kein Suppress-Marker —
  dauerhaftes Offenhalten erfordert einen Sage-Statuswechsel).

### 10.2 Sicherheits-Cap (Muster Reconcile/Nullsetzen)

Ein View-Defekt (Status-Spalte liefert flaechendeckend „abgeholt") wuerde
sonst in EINEM Lauf alle offenen FAs schliessen — still, einweg, ohne
Bulk-Reopen. Deshalb Cap nach dem etablierten Muster:

- Neuer Service-Key **`Sync:FaZusatzinfoAutoDoneMaxPerRun`** (Int, Default
  **100**) in `ServiceSettingDefinitions.All` + Drift-Guard-`InlineData`.
  KEIN Ein/Aus-Schalter — nur ein Sicherheitslimit (User-Entscheid „kein
  eigener Schalter" bleibt gewahrt).
- **Zweiphasig:** Kandidaten waehrend der Schleife in einem
  `HashSet<int>` (Order-Ids) sammeln (dedupliziert zugleich hypothetische
  View-Duplikate → kein DryRun-Doppelcount), Writes erst nach der Schleife:
  - Kandidaten `<= Cap` → setzen (bzw. DryRun: nur zaehlen).
  - Kandidaten `> Cap` → **KEIN Erledigt-Write** (der Upsert-Teil laeuft
    normal weiter), Warn-Zeile „Auto-Erledigt uebersprungen: N Kandidaten >
    Cap M — moeglicher View-Defekt", `ISyncErrorNotifier.NotifyAsync`,
    Counts `erledigt-kandidaten=N` + `erledigt-gesetzt=0`, **kein throw**.
- **Cap-Wert als Parameter:** `SyncWorker` liest den Cap via
  `ServiceSettings.GetIntSafeAsync` und reicht ihn als Parameter an
  `SyncAsync(dryRun, autoDoneMaxPerRun, ct)` (Muster
  `ActivityLogCleanupService.RunAsync(retentionDays, …)` — haelt den Service
  ohne DB-Settings unit-testbar).
- **DI:** `ISyncErrorNotifier` neu in den Service injizieren (Position VOR
  `ILogger`; `ISyncLogger` bleibt letzter Parameter — v1.15.2-Konvention).

### 10.3 Protokoll & Transparenz

- Neuer Count **`erledigt-gesetzt`** — Zaehler VOR dem try deklarieren und in
  ALLE DREI Counts-Dictionaries aufnehmen (View-fehlt-Guard `=0`,
  FinishSuccess, FinishFailed im catch). Im DryRun echte Would-be-Zahl +
  `[DryRun]`-Suffix. Bei Cap-Skip zusaetzlich `erledigt-kandidaten`.
- Je gesetzter FA eine Info-Detailzeile
  „FA <Nr> auf erledigt gesetzt (Sage-Status: <status>)" mit
  `reference: OrderNumber` (im FakeSyncLogger assertierbar) —
  **Detailzeilen gecappt auf 100/Lauf** (Muster Hauptlagerplatz-Warnzeilen),
  der Count zaehlt weiterhin alle.
- **Erstlauf-Effekt (gewollt, dokumentieren):** Beim ersten Scharfschalten
  schliessen ALLE in der App noch offenen FAs, die in Sage bereits
  verpackt/abgeholt sind (Aufraeum-Effekt). PFLICHT-Ablauf: DryRun fahren,
  `erledigt-gesetzt`-Zahl kontrollieren; ist sie `> Cap`, Cap temporaer
  erhoehen ODER in Etappen scharfschalten.
- **UX-Transparenz (ohne neues UI, YAGNI):** Erkennbar ist die Automatik ueber
  die einblendbare Spalte „Sage-Status" (verpackt/abgeholt) in FA-Liste/
  Leitstand („Erledigte anzeigen") und das Aktivitaets-Protokoll (admin-only).
  Hilfe-Hinweis deshalb AUCH im Kommissionierungs-/Leitstand-Abschnitt
  („FA verschwindet automatisch, wenn Sage verpackt/abgeholt meldet — Grund
  via Spalte Sage-Status einblendbar"). Kein sichtbarer Herkunfts-Marker
  (Nicht-Ziel).

### 10.4 Tests (InMemory, TDD — Erweiterung `FaZusatzinfoSyncServiceTests`)

Neuer Seed-Helper `SeedPickingStatus(orderId, isDonePicking)` noetig.

1. Status `abgeholt` + FA offen → `IsDonePicking = true`, Audit gesetzt,
   Count `erledigt-gesetzt = 1`, Info-Zeile mit `reference = OrderNumber`.
2. Status `verpackt` → dito.
3. FA bereits `IsDonePicking` → kein Write, kein Count.
4. FA bereits Sage-`IsDone` → kein Write, kein Count.
5. FA `IsCancelled` → kein Write, kein Count.
6. Anderer Status (`in Produktion`) → nichts.
7. Rueckfall-Fall: `IsDonePicking = true` + Status jetzt `in Produktion` →
   bleibt erledigt.
8. **Ping-Pong:** FA manuell wieder geoeffnet (`IsDonePicking = false`),
   Status weiterhin `abgeholt` → wird erneut geschlossen.
9. DryRun: Count gesetzt, DB unveraendert (weder Flag noch neue Zeile).
10. `PickingStatus`-Zeile fehlt → wird mit `IsDonePicking = true` angelegt
    (Audit-Felder `FaZusatzinfoSync`).
11. Case/Trim: `" Abgeholt "` → greift.
12. Unveraendert-Zweig: ExtraInfo identisch, Status abgeholt, FA offen →
    erledigt.
13. Mehrfach-Treffer-Mischfall: eine WA → 2 FAs, eine bereits erledigt, eine
    offen → genau EINE gesetzt, Count = 1.
14. **Cap:** Cap+1 offene Kandidaten → kein Write, Warn-Zeile, Notifier-Fake
    verifiziert, `erledigt-kandidaten` gezaehlt, `erledigt-gesetzt = 0`.

### 10.5 Doku (Fold 2)

- **Katalog-Beschreibung `Sync:FaZusatzinfoEnabled` erweitern** (die einzige
  Stelle, die der Admin beim Aktivieren sieht): „… synchronisieren. ACHTUNG:
  setzt FAs mit Sage-Status verpackt/abgeholt automatisch auf Komm-Erledigt
  (vorher DryRun pruefen)." + neuer Cap-Key mit eigener Beschreibung.
- Changelog: neuer Bullet in der v1.26.0-Card.
- Hilfe: Zusatzinfo-Kontext + Service-Hinweis (Hilfe:320) + **zusaetzlich
  Kommissionierungs-/Leitstand-Abschnitt** (§10.3 UX-Transparenz).
- TESTSZENARIEN Kap. 55: (a) **TS-55.1 umbauen** — Pflicht-Vorschritt „DryRun
  aktivieren, `erledigt-gesetzt` kontrollieren, erst dann scharf"
  (Reconcile-Muster); (b) Kopf-Hinweis + Count-Liste um `erledigt-gesetzt`/
  `erledigt-kandidaten` + Detailzeilen-Cap; (c) TS-55.7 ergaenzen („Gate aus →
  Automatik stoppt ebenfalls"); (d) neue TS-55.10 (Automatik + Erstlauf-DryRun
  + Teilkomm-Fall) und TS-55.11 (Einweg/bereits-erledigt/anderer Status/
  Ping-Pong nach manuellem Wieder-Oeffnen); (e) Recovery-Hinweis mit SQL
  (`UPDATE ProductionOrderPickingStatus SET IsDonePicking = 0 WHERE
  ModifiedBy = 'FaZusatzinfoSync' AND ModifiedAt >= '<Zeitfenster>'`) fuer den
  Fehlerfall.
- Deploy §7 Schritt 4 ergaenzen: DryRun-Kontrolle prueft explizit
  `erledigt-gesetzt` (Erstlauf-Aufraeum-Zahl!).
- **BDE-Sperre (§10.6):** Hilfe-BDE-Abschnitt („Keine neuen Buchungen mehr,
  sobald die FA in Sage verpackt/abgeholt ist — laufende Buchungen koennen
  normal beendet werden"); TESTSZENARIEN neue TS-55.12 (Start/Resume gesperrt
  + Terminal-Meldung + Listen-Hygiene) und TS-55.13 (laufende Buchung bleibt
  abschliessbar, ungeplante Taetigkeit weiter moeglich); CLAUDE.md-Fallstrick
  um die Sperre ergaenzen (Guard in Start+Resume, NICHT in Beenden-Pfaden;
  `IsDoneBde` blockt nichts und bleibt unbeschrieben).
- CLAUDE.md: Service-Konfig-Tabelle (Beschreibung + Cap-Key) + Fallstrick-
  Eintrag FA-Zusatzinfos um Auto-Erledigt ergaenzen (IsDonePicking-Tripel-
  Bedingung, Einweg + Ping-Pong, Cap, Count `erledigt-gesetzt`); README-
  Formulierung „read-only 1:1-Satellit" praezisieren (der Sync schreibt jetzt
  auch `PickingStatus.IsDonePicking`); PROJECT_STATUS-Eintrag.
- Spec/Plan-Zwillinge unter `secondbrain/docs` mitziehen (Worktree ohne
  Junctions).

### 10.6 BDE-Buchungs-Sperre bei „verpackt"/„abgeholt" (Rev. 5, User-Anmerkung)

Ist die FA in Sage bereits verpackt oder abgeholt (= ausgeliefert), sind am
BDE-Terminal **keine neuen Buchungen** mehr moeglich. Befund: `IsDoneBde` ist
heute reines Anzeige-/Toggle-Flag (blockt nirgends) — deshalb ein expliziter
Guard statt eines Flags.

**Gemeinsame Status-Wahrheit (DRY):** Neuer Helper im Web-Projekt
(z. B. `Services/FaZusatzinfoStatus.cs`): Konstanten `Verpackt = "verpackt"` /
`Abgeholt = "abgeholt"` + `static bool IstVerpacktOderAbgeholt(string? s)`
(Trim + `OrdinalIgnoreCase`). Wird von BEIDEN Wirkungen genutzt: Auto-Erledigt
(§10.1, Service-Projekt referenziert Web) und BDE-Sperre. EF-Listen-Filter
(unten) nutzen die Konstanten inline (`.Trim().ToLower()`-Vergleich,
InMemory-kompatibel — statischer Helper ist nicht EF-uebersetzbar).

**Zentraler Guard (`BdeBookingService`):**

- In `StartPlannedAsync` DIREKT NACH dem Werkbank-Gate (`EnsureWorkplaceIsBdeActiveAsync`,
  BdeBookingService.cs:41): Lookup per FK-Kette
  `_ctx.WorkOperations.Where(w => w.Id == workOperationId)
  .Select(w => new { w.ProductionOrder.OrderNumber,
  SageStatus = w.ProductionOrder.ExtraInfo != null ?
  w.ProductionOrder.ExtraInfo.SageStatus : null })` — ein gezielter Read.
  Bei Treffer → `BdeBookingResult.Invalid("FA <Nr> ist bereits <status> —
  keine BDE-Buchung mehr moeglich.")` (bestehendes Invalid-/InvalidState-Muster).
- **Gleicher Guard in `ResumeAsync`** (Fortsetzen erzeugt laut BDE-Semantik
  eine NEUE Buchung mit `ParentBookingId`). Platzierung: **NACH dem
  Parent-Load + Paused-Check** (die Methode hat keinen workOperationId-Param —
  die Id kommt aus `parent.WorkOperationId` und ist **nullable**), Guard nur
  `if (parent.WorkOperationId.HasValue)` — pausierte **Activity-Buchungen**
  (WorkOperationId NULL, real erreichbar ueber das Paused-Panel) duerfen NICHT
  geblockt werden (Semantik 3).
- Deckt damit ALLE Wege: AG-Scan (der Scan-Pfad `GetByFaAndOperationAsync`
  filtert heute gar nicht), Buttons, NurFA-Modus (`StartProductionForOrder`
  muendet via Default-WorkOperation ebenfalls in `StartPlannedAsync`).
- **Semantik (User-approved):** (1) Neue Buchungen + Fortsetzen gesperrt.
  (2) LAUFENDE Buchungen bleiben unangetastet — Fertigmelden/Pausieren/
  Beenden/Mengen-Erfassung gehen weiter (kein gestrandeter Operator).
  (3) Ungeplante Taetigkeiten (`StartActivityAsync`, werkbank-bezogen, ohne
  FA) bleiben moeglich.
- Keine Zusatzinfo-Daten (Sync aus / View fehlt / kein Satellit) → KEINE
  Sperre (Verhalten wie bisher). Kein eigener Schalter, kein Cap (reiner
  Lese-Check).

**Terminal-UX:**

- **JS-Bugfix (mitliefern):** `bde-terminal.js` `post()` verschluckt heute
  `InvalidState`-Antworten stillschweigend (betrifft auch die bestehende
  Werkbank-Gate-Meldung!). Neuer Zweig: `outcome === 'InvalidState'` →
  `showToast(json.message || 'Aktion nicht moeglich', 'danger')`.
  **KEIN catch-all-else** — `GroupFinishRequired` laeuft als Nicht-Success
  durch `post()` und wird vom Aufrufer behandelt. Der Resume-Pfad nutzt
  `post()` nicht (eigener fetch, zeigt Meldungen bereits an). Dass auch
  `NotFound`/`Exception` verschluckt werden, ist ein bekannter Alt-Zustand
  und bleibt ausserhalb dieses Folds (YAGNI).
- **Listen-Hygiene:** Die Terminal-Auswahllisten filtern gesperrte FAs mit
  aus, damit sie nicht erst beim Start scheitern: (a) NurFA-FA-Liste
  (`BdeApiController` available-orders-Query) und (b) offene AGs je Werkbank —
  ACHTUNG: `WorkOperationRepository.GetOpenByWorkplaceIdAsync` ist SHARED mit
  `TrackingController.ByWorkplace` (altes Tracking-Modul). Deshalb **optionaler
  Parameter** `GetOpenByWorkplaceIdAsync(workplaceId, excludePackedOrders =
  false)` — Default false erhaelt das Tracking-Verhalten, NUR der BDE-Aufrufer
  setzt `true`; Filter laeuft IN der EF-Query (nicht nachgelagert in-Memory —
  die Methode laedt `ExtraInfo` heute nicht).
  **EF-Prädikat ausgeschrieben (Null-Guards Pflicht, InMemory-Semantik):**
  `po.ExtraInfo == null || po.ExtraInfo.SageStatus == null ||
  (po.ExtraInfo.SageStatus.Trim().ToLower() != "verpackt" &&
  po.ExtraInfo.SageStatus.Trim().ToLower() != "abgeholt")`.
  Der Scan-Lookup-Endpoint bleibt unveraendert (der Start-Guard liefert die
  sprechende Meldung).
- **NurFA-Randfall (bewusst akzeptiert):** `StartProductionForOrder` legt via
  `FindOrCreateDefaultAsync` ggf. eine Default-WorkOperation an, BEVOR der
  Guard ablehnt — die Zeile bleibt stehen. Unschaedlich: idempotent (naechster
  Versuch findet sie wieder), vorbestehendes Muster bei allen Ablehnungen
  (Werkbank-Gate/Collision), und durch die Listen-Hygiene ist der Pfad nur
  ueber eine veraltete Terminal-Seite erreichbar. Kein Pre-Check (YAGNI).

**Tests (InMemory, TDD — `BdeBookingServiceTests` + API-/Repo-Tests):**

1. Start mit FA `verpackt` → `InvalidState` + Meldung enthaelt FA-Nummer und
   Status; keine Buchung angelegt.
2. Start mit FA `abgeholt` → dito. Case/Trim (`" Verpackt "`) → greift.
3. Start mit anderem Status (`in Produktion`) → Buchung startet normal.
4. Start ohne ExtraInfo-Satellit → startet normal.
5. Resume einer pausierten Buchung, FA inzwischen `abgeholt` → `InvalidState`,
   keine neue Buchung.
6. Laufende Buchung einer inzwischen `verpackt`-FA: Finish/Pause funktionieren
   weiter (kein Guard in den Beenden-Pfaden).
7. Ungeplante Taetigkeit an einer Werkbank startet trotz gesperrter FA.
8. NurFA-Liste + offene-AGs-Liste enthalten gesperrte FA nicht mehr
   (ExtraInfo-los + anderer Status bleiben enthalten); Tracking-Aufrufer von
   `GetOpenByWorkplaceIdAsync` (Default-Parameter) bleibt ungefiltert.
9. Setup-Transition: laufendes Ruesten + FA wird `verpackt` →
   `StartProduction` = `InvalidState`, das Setup bleibt Running (Guard liegt
   VOR der Auto-Close-Transition — kein gestrandeter Operator).
10. Resume einer pausierten Activity-Buchung (WorkOperationId NULL) bleibt
    trotz Sperre irgendwelcher FAs moeglich.

### 10.7 Nicht-Ziele (Fold 2)

- Kein Auto-Reopen, kein Herkunfts-Marker, kein Suppress-Marker.
- Kein Schreiben von Sage-`IsDone` oder `BdeStatus.IsDoneBde` (die BDE-Sperre
  laeuft als Guard auf `SageStatus`, nicht ueber ein Flag).
- Kein Guard in den Beenden-/Pausieren-/Mengen-Pfaden laufender Buchungen.
- Kein eigener Ein/Aus-Schalter (nur der Sicherheits-Cap fuers Auto-Erledigt),
  kein eigener Protokoll-Service-Name (Counts im bestehenden
  `FaZusatzinfo`-Lauf; die BDE-Sperre protokolliert nicht — sie ist ein
  Request-Guard, kein Sync).
- Kein Bulk-Reopen-UI.
