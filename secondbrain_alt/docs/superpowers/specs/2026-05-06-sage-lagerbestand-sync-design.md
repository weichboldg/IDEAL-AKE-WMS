# Design — Sage Lagerbestand-Sync (Phase 2)

**Datum:** 2026-05-06
**Branch:** `feature/sage-lagerbestand-sync` (wird beim Plan-Start angelegt)
**Status:** Spec, freigegeben
**Vorgaenger:** Phase 1 (Lagerplatz-Stammdaten-Sync, v1.9.0) — abgeschlossen, baut darauf auf

## Ziel und Scope

Sage-Lagerbestand pro (Artikel, Lagerplatz) periodisch ins WMS spiegeln. Bei Abweichung wird eine **Korrektur-Buchung** als `StockMovement` mit neuen MovementType-Werten `SageEinbuchung` / `SageAusbuchung` erzeugt. WMS-Bewegungshistorie bleibt vollstaendig erhalten — Sage-Korrekturen sind eigene Eintraege in der Historie, nicht ein Replace bestehender Daten.

Sage ist **Master fuer den absoluten Bestand**. WMS ist **Master fuer die Bewegungs-Geschichte**. Die zwei Welten koexistieren ueber Korrektur-Buchungen, die das Delta ausgleichen.

**Explizit nicht in Phase 2:**
- Auto-Anlegen von Articles, die in Sage existieren aber im WMS nicht (separate Pipeline).
- Korrektur auf Lagerplaetzen mit `Source=Manual` oder ohne Lagerplatz im WMS — diese Tupel werden geskippt + geloggt.
- Auto-zero von WMS-Bestand fuer (Artikel, Lagerplatz)-Tupel, die Sage nicht zurueckgibt — konservativ ignoriert.
- Retention/Cleanup der entstehenden Korrektur-Buchungen (Phase-3-Kandidat).
- Tolerance-Schwellen oder Sage-Wert-Rundung — wir vergleichen exakte decimal-Werte.

## Datenmodell

### `MovementType` Enum-Erweiterung

```csharp
public enum MovementType
{
    Einbuchung = 0,
    Ausbuchung = 1,
    Umbuchung = 2,
    SageEinbuchung = 3,    // Sage-Korrektur Plus: WMS war zu niedrig
    SageAusbuchung = 4     // Sage-Korrektur Minus: WMS war zu hoch
}
```

`Quantity` bleibt positiv (existing `[Range(0.001, double.MaxValue)]`). Vorzeichen kommt aus dem MovementType, wie heute.

### `StockMovement.Note`

Neues optionales Feld:

```csharp
[StringLength(500)]
[Display(Name = "Notiz")]
public string? Note { get; set; }
```

- Default `null` fuer alle bestehenden Bewegungen.
- Fuer `SageEinbuchung`/`SageAusbuchung` wird er gefuellt mit `"Sage-Korrektur: WMS={alt}, Sage={neu}, Diff={delta}"` (mit Vorzeichen).
- Generischer als nur fuer Sage gedacht — kuenftige Auto-Buchungen koennen ihn ebenfalls nutzen.

### Migration `57_AddStockMovementNoteAndSageMovementTypes.sql`

- `ALTER TABLE dbo.StockMovements ADD Note NVARCHAR(500) NULL` mit `COL_LENGTH`-Guard.
- Keine Schema-Aenderung an der `MovementType`-Spalte (bleibt `int`, neue Werte 3/4 sind ohne DB-Migration nutzbar).
- `__EFMigrationsHistory`-Insert mit Equality-Match (Pattern aus SQL/56).
- Plus EF-Migration `AddStockMovementNoteAndSageMovementTypes`.
- `00_FreshInstall.sql` aktualisieren: `StockMovements`-Tabelle bekommt `Note NVARCHAR(500) NULL`.

## Sync-Service-Architektur

### Komponenten im `IDEALAKEWMSService`

```
ILagerbestandSyncService.RunAsync(CancellationToken)
  +-- ISageBestandReader.GetAllAsync(CancellationToken) -> List<SageBestandDto>
  +-- IArticleRepository (lookup ArticleNumber -> Id)
  +-- IStorageLocationRepository (lookup Code -> { Id, Source, IsActive })
  +-- IStockMovementRepository.GetCurrentStockByArticleAndLocationAsync()  // NEU
       -> Dictionary<(int ArticleId, int StorageLocationId), decimal>
  +-- ApplicationDbContext.StockMovements (Insert)
  +-- ISyncLogRepository (Logging)
```

`ISageBestandReader` ist die Mock-Grenze fuer Tests. Echte Implementierung kapselt das raw-SQL gegen Sage. Der Tests-Service injiziert einen `FakeSageBestandReader`.

### Sage-Query

```sql
SELECT
    A.Artikelnummer,
    LP.Kurzbezeichnung AS Lagerplatz,
    SUM(LB.Bestand) AS Bestand
FROM [dbo].[KHKArtikel] AS A
LEFT JOIN [dbo].[KHKArtikelVarianten] AS AV ON A.Artikelnummer = AV.Artikelnummer
LEFT JOIN [dbo].[KHKLagerplatzbestaende] AS LB ON A.Artikelnummer = LB.Artikelnummer
LEFT JOIN [dbo].[KHKLagerplaetze] AS LP ON LB.PlatzID = LP.PlatzID
WHERE A.Mandant = 1
  AND LB.Bestand IS NOT NULL
GROUP BY A.Artikelnummer, LP.Kurzbezeichnung, LB.Lagerkennung
```

Wir uebernehmen die User-Query fast 1:1 — **ohne** das `CONVERT(int, ...)` aus der urspruenglichen Vorlage. Sage-Bestand wird als decimal gelesen (echte Werte, sub-int-Korrekturen werden ehrlich abgebildet).

**Annahme:** `KHKLagerplatzbestaende.Bestand` ist `decimal`/`numeric`. Defensiver Lese-Code: `Convert.ToDecimal(reader.GetValue(idx))` toleriert auch `money`/`float`/`numeric`. Bei der Implementation gegen das echte Schema verifizieren.

### DTO

```csharp
public record SageBestandDto(string? Artikelnummer, string? Lagerplatz, decimal? Bestand);
```

Alle Felder nullable, weil Sage-LEFT-JOINs und Aggregations theoretisch null liefern koennen.

### Sync-Algorithmus

```
1. SageBestandReader.GetAllAsync() -> List<SageBestandDto> sageRows

2. Sage-Duplicate-Detection (analog Phase 1):
     dupGroups = sageRows
       .Where(r => !string.IsNullOrWhiteSpace(r.Lagerplatz))
       .GroupBy((r.Artikelnummer, r.Lagerplatz), case-insensitive)
       .Where(g => g.Count() > 1)
     Pro dup-Gruppe:
       SyncLog.Warning $"Sage liefert (Artikel '{Artikelnummer}', Lagerplatz '{Lagerplatz}') mehrfach.
                        Tupel uebersprungen."
     Filter sageRows: ALLE Vorkommen der Dup-Tupel raus.

3. Pre-Loading:
     articleByNumber = Dictionary<string, int> (alle WMS-Articles, OrdinalIgnoreCase)
     locationByCode  = Dictionary<string, (int Id, string Source, bool IsActive)> (alle WMS-StorageLocations)
     wmsStock        = Dictionary<(int, int), decimal> (per neuer Repo-Methode)

4. counters: int Tuples=0, CorrectionsPlus=0, CorrectionsMinus=0, NoChange=0, Skipped=0, Errors=0

5. Pro Sage-Row dto:
     Tuples++

     a) Validate fields
        - dto.Artikelnummer leer/null -> Skipped++, SyncLog.Warning "Artikel leer", continue
        - dto.Lagerplatz leer/null    -> Skipped++, SyncLog.Warning "Lagerplatz leer", continue
        - dto.Bestand == null         -> behandle als 0 (Sage-LEFT-JOIN-Artefakt)

     b) Article lookup
        articleId = articleByNumber.GetValueOrDefault(Artikelnummer)
        if (articleId == 0) {
          Skipped++, SyncLog.Warning $"Artikel {Artikelnummer} nicht im WMS, uebersprungen", continue
        }

     c) Location lookup
        if (!locationByCode.TryGetValue(Lagerplatz, out var loc)) {
          Skipped++, SyncLog.Warning $"Lagerplatz {Lagerplatz} nicht im WMS, uebersprungen", continue
        }
        if (loc.Source != "Sage") {
          Skipped++, SyncLog.Warning $"Lagerplatz {Lagerplatz} ist Manual-Quelle, uebersprungen", continue
        }
        if (!loc.IsActive) {
          Skipped++, SyncLog.Warning $"Lagerplatz {Lagerplatz} ist deaktiviert, uebersprungen", continue
        }

     d) Diff-Berechnung
        wmsBestand = wmsStock.GetValueOrDefault((articleId, loc.Id), 0m)
        sageBestand = dto.Bestand ?? 0m
        delta = sageBestand - wmsBestand
        if (delta == 0m) { NoChange++; continue }

     e) Korrektur-Insert
        if (dryRun) { /* nur loggen, kein Insert */ }
        else {
          var movementType = delta > 0 ? SageEinbuchung : SageAusbuchung
          var quantity = Math.Abs(delta)
          ctx.StockMovements.Add(new StockMovement {
            ArticleId = articleId,
            StorageLocationId = loc.Id,
            Quantity = quantity,
            MovementType = movementType,
            Note = $"Sage-Korrektur: WMS={wmsBestand}, Sage={sageBestand}, Diff={delta:+#;-#;0}",
            Timestamp = DateTime.Now,
            UserId = null,
            WindowsUser = "system:sync",
            CreatedAt = DateTime.Now,
            CreatedBy = "system:sync",
            CreatedByWindows = Environment.MachineName
          })
        }
        if (delta > 0) CorrectionsPlus++ else CorrectionsMinus++

6. ctx.SaveChangesAsync()  // batch-save am Ende, nicht pro Insert

7. Summen-Log:
     msg = $"Sync OK: {Tuples} Tupel, {CorrectionsPlus} Plus, {CorrectionsMinus} Minus, "
         + $"{NoChange} ohne Aenderung, {Skipped} uebersprungen, {Errors} Fehler"
     if (dryRun) msg = "[DryRun] " + msg
     SyncLog.Info(msg, Service="Lagerbestand")
```

**DryRun-Modus:** `WorkerSettings:SyncDryRun=true` aktiviert. In diesem Modus werden alle Korrekturen nur in den SyncLog geschrieben, nicht in die DB. Empfohlen fuer den Erst-Lauf nach Aktivierung (siehe Operational Notes).

**Sage-Connection-Fehler:** Try/Catch um `_reader.GetAllAsync(ct)` (analog Phase 1). SyncLog.Error, return Result mit Errors=1. Kein Crash.

### Audit-Felder auf Sage-Korrektur-StockMovement

| Feld | Wert |
|---|---|
| `UserId` | `null` |
| `WindowsUser` | `"system:sync"` (konstant — ermoeglicht Filter `WHERE WindowsUser = 'system:sync'` ueber alle Auto-Buchungen) |
| `Timestamp` | `DateTime.Now` |
| `ProductionOrder` | `null` |
| `Note` | `"Sage-Korrektur: WMS={alt}, Sage={neu}, Diff={delta}"` |
| `CreatedAt` | `DateTime.Now` |
| `CreatedBy` | `"system:sync"` |
| `CreatedByWindows` | `Environment.MachineName` |

### Trigger und Konfiguration

- ServiceSetting `Sync:LagerbestandEnabled` (Default `false` in `appsettings.json` und DB-Seed). Worker laeuft nur wenn aktiv.
- Optionales Override `Sync:LagerbestandIntervalMinutes` (Default `0` = nutze `WorkerSettings:SyncIntervalMinutes` (15 Min)).
  - Implementation analog `ShouldRunAutoPauseAsync` in `SyncWorker.cs:247`. Eigenes `_lastLagerbestandRun`-Tracking, Override-Check.
- Worker-Position: am Ende der Sync-Queue, nach Phase-1-Lagerplatz-Sync (Fixed-Reihenfolge: Lagerplatz-Sync muss zuerst laufen, sonst sind keine Sage-Lagerplaetze vorhanden, gegen die Korrekturen erzeugt werden koennen).

### Neue Repository-Methode

`StockMovementRepository.GetCurrentStockByArticleAndLocationAsync()`:

```csharp
Task<Dictionary<(int ArticleId, int StorageLocationId), decimal>> GetCurrentStockByArticleAndLocationAsync();
```

- Aggregiert alle StockMovements zu einem Dictionary.
- Beruecksichtigt alle MovementType-Werte (inklusive der neuen Sage*-Werte) — wenn die Aggregations-Audit (siehe unten) korrekt durchgezogen ist, fliessen alle Movement-Arten richtig ein.
- Wird nur vom Sync-Service genutzt — keine UI-Bindung.

## Aggregations-Audit (kritisch)

Die existierende Aggregations-Logik schaltet auf `MovementType` und entscheidet das Vorzeichen:

```csharp
sm.MovementType == MovementType.Einbuchung ? sm.Quantity :
sm.MovementType == MovementType.Umbuchung ? sm.Quantity :
                                            -sm.Quantity   // Ausbuchung (else-branch)
```

Mit der Erweiterung muss das ueberall werden:

```csharp
sm.MovementType == MovementType.Einbuchung || sm.MovementType == MovementType.SageEinbuchung
    ? sm.Quantity :
sm.MovementType == MovementType.Umbuchung
    ? sm.Quantity :
sm.MovementType == MovementType.Ausbuchung || sm.MovementType == MovementType.SageAusbuchung
    ? -sm.Quantity :
0   // unbekannter MovementType (defensiv)
```

**Bekannte Aggregations-Stellen** (per Grep gegen `MovementType\.`):

1. `IdealAkeWms/Data/Repositories/StockMovementRepository.cs:61-62` (GetCurrentStockAsync — Destination-Aggregation)
2. `IdealAkeWms/Data/Repositories/StockMovementRepository.cs:75` (GetCurrentStockAsync — Umbuchung-Quell-Where, **bleibt unveraendert** — Sage-Korrekturen sind nie Umbuchungen)
3. `IdealAkeWms/Data/Repositories/StockMovementRepository.cs:182` (GetStockByProductionOrderAsync — kollabierter Switch `Ausbuchung ? -Quantity : Quantity` — **gefaehrlich**, weil Default-Pfad neue Sage*-Werte als + behandelt → muss auf expliziten Switch umgebaut werden)
4. `IdealAkeWms/Data/Repositories/StockMovementRepository.cs:307-308, 317` (zweite GetCurrentStock-Variante)
5. `IdealAkeWms/Data/Repositories/StockMovementRepository.cs:361-362, 368` (dritte Variante)
6. `IdealAkeWms/Services/PickingTransferService.cs:198-199, 209` (Picking-Aggregation)
7. **Mglw. weitere** — `StockCheckService` per CLAUDE.md-Fallstricken hat eigene Aggregations-Logik. Plan-Task muss expliziten Grep `MovementType\.` ueber das gesamte Web-Projekt + Service-Projekt durchfuehren und eine **Mapping-Tabelle** als Audit-Artefakt produzieren.

**Risiko der `:182`-Stelle:** kollabierter Ternary-Switch behandelt unbekannte Enum-Werte still als +. Eine vergessene Stelle hier = SageAusbuchung (Minus-Korrektur) wird zu Plus aggregiert = Bestand-Drift. **Test E3** (siehe unten) deckt das ab — pro Aggregations-Methode ein expliziter Test.

## UI-Aenderungen

### Bewegungshistorie (`Views/StockMovements/Index.cshtml`)

- Filter-Dropdown ergaenzen:
  ```cshtml
  <option value="3" selected="@(Model.FilterMovementType == MovementType.SageEinbuchung)">Sage-Einbuchung</option>
  <option value="4" selected="@(Model.FilterMovementType == MovementType.SageAusbuchung)">Sage-Ausbuchung</option>
  ```
- Neue Spalte **"Notiz"** mit `data-col-key="note"` — Anzeige von `Note` auf der Zeile, leer bei Bewegungen ohne Notiz. Default sichtbar.
- Badge-Klassen `badge-sage-einbuchung` und `badge-sage-ausbuchung` mit Sage-typischer Farbgebung:
  ```css
  .badge-sage-einbuchung { background-color: var(--ake-primary);   color: white; }
  .badge-sage-ausbuchung { background-color: var(--ake-secondary); color: white; }
  ```
- `MovementTypeName`-Mapping in `StockMovementRepository.cs:274-275` erweitern um die zwei neuen Werte:
  ```csharp
  sm.MovementType == MovementType.SageEinbuchung ? "Sage-Einbuchung" :
  sm.MovementType == MovementType.SageAusbuchung ? "Sage-Ausbuchung" :
  ```

### Bestandsuebersicht

Keine Aenderung. Der Aggregations-Audit (oben) sorgt dafuer, dass Sage-Korrekturen automatisch in `GetCurrentStockAsync` einfliessen. Inactive-Badge-Logik (Phase 1) bleibt unveraendert.

### Sync-Protokoll-View (Phase 1)

Im `SyncLogController.KnownServices`-Array den Eintrag `"Lagerbestand"` ergaenzen — Service-Filter-Dropdown bekommt einen neuen Wert. Keine weitere View-Aenderung.

### Service-Einstellungen-View

Falls die existierende View ServiceSettings-Toggles dynamisch listet, erscheinen `Sync:LagerbestandEnabled` und `Sync:LagerbestandIntervalMinutes` automatisch. Falls die View hardcoded ist, separate Plan-Task fuer eine Wartungs-Aenderung — out-of-scope hier.

## Tests

### `IDEALAKEWMSService.Tests` — `LagerbestandSyncServiceTests`

Mit `FakeSageBestandReader` (analog Phase-1-Pattern):

1. **Insert-Plus** — WMS hat 0 fuer (A1, L1), Sage liefert 5 → SageEinbuchung mit Quantity=5, Note enthaelt "Diff=+5", CorrectionsPlus=1.
2. **Insert-Minus** — WMS hat 10 fuer (A1, L1), Sage liefert 7 → SageAusbuchung mit Quantity=3, Note enthaelt "Diff=-3", CorrectionsMinus=1.
3. **No-Change** — WMS-Bestand == Sage-Bestand → kein Insert, NoChange=1.
4. **Decimal-Diff** — WMS=5.7, Sage=6.0 → SageEinbuchung mit Quantity=0.3 (decimal-genau).
5. **Skip Unknown Article** — Sage liefert Artikelnummer "DOES-NOT-EXIST" → kein Insert, SyncLog.Warning, Skipped=1.
6. **Skip Unknown Location** — Sage liefert Lagerplatz-Code, der im WMS fehlt → Skipped=1 + Warning.
7. **Skip Manual-Source Location** — WMS-Lagerplatz existiert mit Source=Manual → Skipped=1 + Warning.
8. **Skip Inactive Location** — WMS-Lagerplatz existiert mit Source=Sage, aber IsActive=false → Skipped=1 + Warning.
9. **Bestand=NULL Handling** — Sage liefert Tupel mit Bestand=null → wird als 0 interpretiert (Diff zu WMS-Wert). Edge-case.
10. **Aggregation gegen mehrere Vor-Movements** — WMS hat (A1,L1) per Einbuchung 10 + Ausbuchung 4 + Umbuchung-Ziel 2 → effektiver Bestand 8. Sage liefert 6 → SageAusbuchung mit Quantity=2.
11. **DryRun-Modus** — `dryRun=true`: keine Inserts in DB, aber Summen-Log enthaelt Praefix `[DryRun]` und Counter sind korrekt.
12. **Sage-Connection-Fehler** — Reader wirft Exception → Errors=1, SyncLog.Error, kein Crash, kein partielles Save.
13. **Sage-Duplicate-Tupel** — Sage liefert 2 Rows fuer gleiche (Artikelnummer, Lagerplatz) (verschiedene Lagerorte) → ALLE Vorkommen geskippt, SyncLog.Warning, kein Insert.
14. **Audit-Felder** — Insert-Test verifiziert `WindowsUser = "system:sync"`, `UserId = null`, `Note` korrekt gesetzt, `Timestamp` und `CreatedAt` belegt.

### `IdealAkeWms.Tests` — Aggregations-Audit-Tests

15. **`StockMovementRepositoryTests.GetCurrentStockAsync_IncludesSageMovements`** — DB hat: Einbuchung 10, SageAusbuchung 3 → aggregierter Bestand = 7.
16. **`StockMovementRepositoryTests.GetStockByProductionOrderAsync_IncludesSageMovements`** — analoger Test fuer die zweite Aggregations-Methode.
17. **`StockMovementRepositoryTests.GetMovementsAsync_ReturnsNote`** — Note-Spalte fliesst durch ViewModel.
18. **`PickingTransferServiceTests.Aggregation_IncludesSageMovements`** — analoger Test fuer die Picking-Aggregation.

Tests 15+16+18 sind bewusst getrennt pro Aggregations-Methode — verhindert das Vergessen einer Site (siehe Aggregations-Audit-Risiko).

### Manuelle Verifikation (im Plan als expliziter Schritt)

- **DryRun gegen echte Sage-DB:** `Sync:LagerbestandEnabled=true` + `WorkerSettings:SyncDryRun=true`. SyncLog inspizieren — wieviele Korrekturen wuerden geschrieben? Plus/Minus-Verteilung sinnvoll?
- **Sage-Schema verifizieren:** Spalte `KHKLagerplatzbestaende.Bestand` ist tatsaechlich `decimal`/`numeric`? Falls nicht: `Convert.ToDecimal` muss greifen.
- **Mandant-Filter pruefen:** `KHKArtikel.Mandant = 1` ist korrekt? (Wir uebernehmen das aus deiner Query.)

## Operational Notes

### C1. Erst-Lauf Tsunami

Bei Aktivierung kann der erste Sync **mehrere tausend** Korrektur-Buchungen erzeugen — je nachdem, wie sehr WMS-Bestand vom Sage-Stand abweicht (typisch nach langer Phase-1-only-Phase: viele Sage-Lagerplaetze ohne Bestand im WMS → tausende SageEinbuchungen).

**Empfohlener Workflow:**
1. `Sync:LagerbestandEnabled = true` setzen.
2. **`WorkerSettings:SyncDryRun = true`** vor dem ersten Lauf aktivieren.
3. Worker neu starten, einen Sync-Lauf abwarten.
4. Sync-Protokoll-View oeffnen, `Service=Lagerbestand` filtern. Counter-Eintrag enthaelt `[DryRun] Sync OK: ... Korrekturen geplant`.
5. Stichproben aus den Warning-Eintraegen pruefen — passen sie zur Erwartung?
6. Falls plausibel: `WorkerSettings:SyncDryRun = false`, naechster Sync-Lauf schreibt die Korrekturen.

Plus: Hilfeseite-Eintrag dokumentiert diesen Workflow ausfuehrlich.

### C2. Phase-1-Reife als Voraussetzung

Strikte `Source=Sage`-Filterung bedeutet: solange im WMS Lagerplaetze noch `Source=Manual` sind (auch wenn der Code mit Sage matcht), werden alle Tupel auf diesen Plaetzen geskippt. Nach Phase-1-Aktivierung koennen viele Manual-Plaetze als "Konflikt" geloggt werden, ohne Aufloesung — Phase 2 skippt sie auch.

**Empfehlung:** Vor Phase-2-Aktivierung die Phase-1-Konflikt-Liste durcharbeiten:
- Manual-Lagerplaetze, die in Wirklichkeit aus Sage kommen → in DB direkt `Source = 'Sage'` setzen.
- Manual-Lagerplaetze, die wirklich manuell sind (z.B. Korrekturwagen `NAN`, Kommissionierwagen) → bleiben Manual und werden korrekt geskippt.

### C3. Eventually Consistent

Sync laeuft als Snapshot-Compare:
- Sage-Read bei T1
- WMS-Aggregat bei T2 (kurz danach)
- Inserts bei T3 (am Ende)

Konkurrierende User-Buchungen im T2-T3-Fenster fuehren zu kleinem Drift. Der naechste Sync-Lauf gleicht das aus (typisch nach 15 Min). Akzeptable Limitation fuer Phase 2.

### C4. Bewegungshistorie waechst

Pro Sync potenziell N neue Eintraege. Bei stabilem Betrieb (geringe Drift): wenige pro Lauf. Bei schlechter Sage-WMS-Synchronitaet: viele.

Phase-3-Kandidat: Retention/Cleanup von Sage-Korrektur-Buchungen alter Sync-Laeufe (z.B. nur die letzten N Korrekturen pro Tupel behalten, aeltere zu einem Saldo-Eintrag verdichten). Phase 2 macht nichts daran.

## Doku-Pflichten

- `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs`: **v1.10.0**, Datum 2026-05-06 (oder Implementations-Tag).
- `IdealAkeWms/Views/Help/Changelog.cshtml`: neuer v1.10.0-Eintrag.
- `IdealAkeWms/Views/Help/Index.cshtml`: neuer Abschnitt **"Lagerbestand-Sync mit Sage"** — Was wird synchronisiert, was bedeuten `Sage-Einbuchung`/`Sage-Ausbuchung`, DryRun-Empfehlung beim Erst-Lauf, Hinweis auf Phase-1-Reife als Voraussetzung.
- `PROJECT_STATUS.md`: Eintrag in Roadmap + Aenderungen.
- `CLAUDE.md`: ServiceSettings-Tabelle ergaenzen (`Sync:LagerbestandEnabled`, `Sync:LagerbestandIntervalMinutes`).
- Branch-Name: `feature/sage-lagerbestand-sync`.

## Annahmen, die bei Implementierung verifiziert werden

1. `KHKLagerplatzbestaende.Bestand` ist `decimal`/`numeric`. Defensive Lese-Variante via `Convert.ToDecimal(reader.GetValue(...))`.
2. `KHKArtikel.Mandant = 1` ist der korrekte Mandant fuer dieses Setup (uebernommen aus User-Query).
3. Sage's `LB.Bestand IS NOT NULL` filtert auch implizit Tupel mit Bestand=0? Oder kommen Bestand=0-Rows trotzdem zurueck? Verhalten implementations-seitig pruefen.
4. Aggregations-Stellen-Liste vollstaendig — Plan-Task muss `MovementType\.`-Grep + Audit ausfuehren.

Diese Punkte gehoeren in den Plan als explizite Verifikationsschritte zu Anfang.
