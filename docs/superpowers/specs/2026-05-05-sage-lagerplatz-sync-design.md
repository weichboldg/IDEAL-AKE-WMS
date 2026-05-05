# Design — Sage Lagerplatz-Sync (Phase 1)

**Datum:** 2026-05-05
**Branch:** `feature/sage-lagerplatz-sync`
**Status:** Spec, freigegeben

## Ziel und Scope

Lagerplatz-Stammdaten aus Sage (Tabellen `KHKLagerorte` und `KHKLagerplaetze`) periodisch in das WMS spiegeln. Sage ist Master fuer Sync-Records (Code, Zone, Bezeichnung, Aktiv-Status). WMS-eigene Felder (Capacity, IsPickingTransport) bleiben unter User-Hoheit. Manuell angelegte Lagerplaetze bleiben unangetastet.

**Explizit nicht in Phase 1:**
- Lagerbestaende-Uebernahme (separate Folge-Phase)
- UI fuer Konflikt-Aufloesung (zunaechst nur Logging, manuelle DB-Korrektur)
- SyncLog-Retention/Cleanup (wird ergaenzt wenn Tabellengroesse zum Problem wird)
- Manueller "Jetzt synchronisieren"-Button (entfaellt, weil Worker-Service ausreichend)

## Datenmodell

### `StorageLocation` Erweiterung

| Feld | Typ | Default | Bedeutung |
|---|---|---|---|
| `Source` | `string` (max 20, EF `HasConversion<string>()`) | `"Manual"` | `"Manual"` oder `"Sage"`. Bestimmt, ob der Sync den Datensatz beruehren darf. |
| `IsActive` | `bool` | `true` | Bei `Source=Sage`: spiegelt Sage-`Aktiv`. Bei `Source=Manual`: User-editierbar (z.B. Stilllegung). |

`Source` als String (nicht int-enum) wegen DB-Lesbarkeit und einfacherer SQL-Skripte. EF speichert via `HasConversion<string>()`. Werte werden ueber Konstanten in Code referenziert (`StorageLocationSource.Manual` / `StorageLocationSource.Sage`), nicht als Magic Strings.

### Neue Entity `SyncLog`

| Feld | Typ | Bedeutung |
|---|---|---|
| `Id` | `int` PK | |
| `Timestamp` | `DateTime` | Zeitpunkt des Eintrags |
| `Service` | `string` (max 50) | z.B. `"Lagerplatz"`, spaeter `"Lagerbestand"`, `"OseonTracking"` |
| `Level` | `string` (max 10, EF `HasConversion<string>()`) | `"Info"` / `"Warning"` / `"Error"` |
| `Message` | `string` (max 1000) | Klartext |
| `Reference` | `string?` (max 100) | optionale ID/Code fuer Drill-down (z.B. Lagerplatz-Code) |

Indizes: `IX_SyncLog_Timestamp` (DESC), `IX_SyncLog_Service_Level`.

`SyncLog` ist bewusst minimal und service-uebergreifend, sodass die naechsten Sync-Phasen (Bestand, Artikel-Verbesserungen) sofort dieselbe Tabelle und denselben View nutzen koennen.

### Migrationen

- `55_AddStorageLocationSyncFields.sql` — `Source` (nvarchar(20), Default `'Manual'`), `IsActive` (bit, Default 1), Backfill `Source='Manual'`, `IsActive=1` fuer alle bestehenden StorageLocations. Plus EF-Migration `AddStorageLocationSyncFields`.
- `56_AddSyncLog.sql` — Tabelle + Indizes. Plus EF-Migration `AddSyncLog`.

Beide mit `OBJECT_ID`-Guard, `__EFMigrationsHistory`-Insert in separatem Batch. `00_FreshInstall.sql` konsolidiert aktualisieren.

## Sync-Service-Architektur

### Komponenten im `IDEALAKEWMSService`

```
ILagerplatzSyncService.SyncAsync()
  +-- ISageLagerplatzReader.GetAllActiveAsync() -> List<SageLagerplatzDto>   // raw SQL gegen SageConnection
  +-- ApplicationDbContext (WMS)                                              // existiert bereits im Service
  +-- ISyncLogger.LogInfo / LogWarning / LogError                             // schreibt SyncLog-Eintraege
```

`ISageLagerplatzReader` ist die Mock-Grenze fuer Tests — der Service-Test injiziert einen Fake-Reader und braucht keinen DB-Mock fuer Sage. Die echte Implementierung kapselt das raw-SQL.

`ISyncLogger` ist ein duennes Interface ueber `ApplicationDbContext.SyncLogs` (kein eigener Service-Layer noetig, aber so ist es injizierbar und testbar).

### Sage-Query (Annahme)

```sql
SELECT lo.Lagerkennung, lp.Kurzbezeichnung, lp.Platzbezeichnung
FROM KHKLagerorte lo
LEFT JOIN KHKLagerplaetze lp ON lo.Lagerkennung = lp.Lagerkennung
WHERE lo.Mandant = 1
  AND lo.Aktiv = -1
```

**Mandant-Filter** ist analog zum Artikel-Sync (`02_Import_Artikel.sql`). Annahme: `KHKLagerorte` hat Spalte `Mandant`. Bei der Implementierung gegen das echte Sage-Schema verifizieren — falls Spalte fehlt, Filter weglassen.

**Wichtig:** `Aktiv` wird nur bei `KHKLagerorte` gefiltert. `KHKLagerplaetze` selbst hat keinen Aktiv-Status; deaktivierte Plaetze gehoeren zu inaktiven Bereichen. Wenn ein einzelner Lagerort inaktiv wird, fallen alle seine Lagerplaetze aus der Query — Sync deaktiviert sie automatisch (siehe Algorithmus).

### Sync-Algorithmus

```
1. SageLagerplatzReader.GetAllActiveAsync() -> List<SageLagerplatzDto>
2. Sage-Duplicate-Detection:
     Gruppiere nach Kurzbezeichnung.
     Fuer jede Gruppe mit Anzahl > 1:
       SyncLogger.Warning("Sage liefert Lagerplatz-Code 'X' mehrfach (Bereiche A, B). Eintrag uebersprungen.", Reference=X)
       Alle Vorkommen aus der Verarbeitungsliste entfernen.
3. Pre-Validierung: pro Sage-Eintrag
     - Code.Length > 12       -> Warning, skip
     - Description.Length > 200 -> truncate auf 200, Info-Log "X gekuerzt"
4. Lade alle WMS-StorageLocations in Map<Code, StorageLocation> (Sync-Records inkl. inaktiver, Manual-Records).
5. Pro gueltigem Sage-Eintrag:
     existing = map.Lookup(Code)
     if (existing == null):
         INSERT new StorageLocation:
           Source="Sage", IsActive=true
           Code, Zone=Lagerkennung, Description=Platzbezeichnung
           BarcodeValue=Code   // Etikettendruck
           Capacity=null, IsPickingTransport=false
           CreatedAt/By/ByWindows = "system:sync" / Environment.MachineName
         counters.Inserted++
     else if (existing.Source == "Sage"):
         Diff Zone/Description/IsActive (re-aktivieren falls vorher false).
         BarcodeValue immer auf Code halten (defensiv).
         Wenn diff:
           Update + ModifiedAt/By/ByWindows = "system:sync" / Environment.MachineName
           counters.Updated++
     else: // existing.Source == "Manual"
         SyncLogger.Warning("Konflikt: Lagerplatz X existiert manuell, Sage-Eintrag ignoriert.", Reference=Code)
         counters.Conflicts++
6. Pro WMS-Record mit Source=Sage und IsActive=true, Code NICHT in Sage-Liste:
     IsActive = false, ModifiedAt/By gesetzt
     SyncLogger.Info("Lagerplatz X aus Sage entfernt -> deaktiviert.", Reference=Code)
     counters.Deactivated++
7. SaveChangesAsync()
8. SyncLogger.Info("Sync OK: N neu, M aktualisiert, K Konflikte, L deaktiviert", Service=Lagerplatz)
```

Bei Sage-Connection-Fehler: `SyncLogger.Error("Sage-Connection fehlgeschlagen: <ex.Message>", Service=Lagerplatz)`, kein Crash, naechster Lauf versucht erneut.

### Trigger und Konfiguration

- ServiceSetting `Sync:LagerplaetzeEnabled` (Default `false` in `appsettings.json` und DB-Seed). Worker laeuft nur wenn aktiv.
- Worker-Reihenfolge: am Ende der Sync-Queue eingehaengt. Keine FK-Abhaengigkeit zu Articles oder ProductionOrders.
- Sync-Intervall: `WorkerSettings:SyncIntervalMinutes` (existing, Default 15).
- Kein manueller Sync-Button in Phase 1.

## UI-Aenderungen

### Lagerplaetze-Index

- Neue Spalte **Quelle** (Badge: "Manual" grau, "Sage" blau).
- Neue Spalte **Aktiv** (nur sichtbar wenn Filter "Auch inaktive zeigen" an).
- Default-Filter `IsActive = true`. Toggle "Auch inaktive zeigen" oben.
- Inaktive Lagerplaetze: Code grau-durchgestrichen, Bestand-Badge falls Bestand > 0 ("inaktiv mit Restbestand").
- Etikettendruck-Button: unveraendert (nutzt `BarcodeValue`, der durch Sync = `Code` gesetzt wird).

### Lagerplaetze-Edit

- Bei `Source = Manual`: alle Felder editierbar wie heute. Zusaetzlich: `IsActive` als Checkbox editierbar (neu).
- Bei `Source = Sage`: `Code`, `Zone`, `Description`, `IsActive` werden als `readonly` gerendert + Banner "Aus Sage synchronisiert. Aenderungen werden beim naechsten Abgleich ueberschrieben."
- `Capacity` und `IsPickingTransport` bleiben fuer Sage-Records editierbar (echte WMS-only Felder).
- `IsActive` ist nur fuer Manual-Records editierbar — bei Sage-Records ist es Sync-kontrolliert (sonst wuerde der naechste Sync-Lauf manuelle Aenderungen sofort ueberschreiben).

### Server-Side Schutz im Edit-POST (kritisch)

Bei `Source = Sage`: Aus dem eingehenden ViewModel werden `Code`, `Zone`, `Description` ignoriert. Nur `Capacity`, `IsPickingTransport`, `IsActive` werden uebernommen. Test dafuer ist Pflicht (POST-Test mit manipuliertem Body).

### Stock-Movement-Buchung (Einbuchung / Ausbuchung / Umbuchung / Stueckliste)

- Lagerplatz-Dropdowns: nur `IsActive = true` zeigen. Neue Repository-Methode `GetActiveAsync()`.
- **Audit aller bestehenden `IStorageLocationRepository`-Aufrufer im Web-Projekt** mit Mapping-Tabelle (Plan-Task):
  - Stock-Movement-Buchung: GetActiveAsync (neu)
  - Stueckliste Quell-/Ziel-Dropdown: GetActiveAsync (neu, IsPickingTransport-Filter bleibt)
  - Bestandsuebersicht Liste: GetAllAsync (bleibt — wir wollen Bestand auf inaktiven Plaetzen sehen)
  - Bewegungshistorie: GetAllAsync (bleibt — historisch korrekt)
  - StorageLocations-Index: GetAllAsync mit optionalem Filter
- Existing `IsPickingTransport`-Filter aus CLAUDE.md-Fallstricken bleibt erhalten und kombiniert sich mit `IsActive`.

### Bestandsuebersicht / Bewegungshistorie

- Inaktive Lagerplaetze mit Bestand > 0 -> Warnung-Badge "inaktiv" in Spalte Lagerplatz.
- Bewegungshistorie unveraendert (historisch korrekt).

### Sync-Protokoll

Neue View `Stammdaten -> Sync-Protokoll` (`SyncLogController.Index`, `[RequireMasterDataAccess]`):

- Tabelle: Zeitpunkt | Service | Level (Badge) | Reference | Message
- Filter: Service-Dropdown, Level-Dropdown, Datum von/bis, Reference-Suche
- Sortiert Timestamp DESC, Pagination 50/Seite
- Eintrag im Stammdaten-Menue (`_Layout.cshtml`)

## Tests

### `IDEALAKEWMSService.Tests`

`LagerplatzSyncServiceTests` (xUnit + InMemory + FluentAssertions, Fake-`ISageLagerplatzReader`):

1. **Insert** — leere DB + Sage liefert 3 Plaetze -> 3 INSERTs mit `Source=Sage`, `IsActive=true`, `BarcodeValue=Code`, `Capacity=null`, `IsPickingTransport=false`. SyncLog-Summe.
2. **Update bei Diff** — bestehender Sage-Record + Sage liefert geaenderte Description -> UPDATE, ModifiedAt gesetzt, Counter Updated=1.
3. **No-Op ohne Diff** — bestehender Sage-Record + Sage liefert identische Werte -> kein UPDATE, kein neuer SyncLog ausser Summe.
4. **Konflikt** — bestehender `Source=Manual` mit gleichem Code -> kein UPDATE, SyncLog Warning mit Reference, Counter Conflicts=1.
5. **Soft-Deactivate** — WMS-Sage-Record fehlt jetzt in Sage-Liste -> `IsActive=false`, SyncLog Info, Counter Deactivated=1.
6. **Reaktivierung** — Sage liefert wieder einen vorher deaktivierten Sage-Record -> `IsActive=true`, Counter Updated.
7. **Sage-Duplicate** — Sage-Liste enthaelt zwei `KHKLagerplaetze` mit gleichem Code -> alle uebersprungen, SyncLog Warning, kein INSERT.
8. **Length-Cap Code** — Sage-Code 15 Zeichen -> skip, SyncLog Warning.
9. **Length-Cap Description** — Sage-Description 250 Zeichen -> truncate auf 200, INSERT mit gekuerzter Description, SyncLog Info.
10. **Sage-Connection-Fehler** — Reader wirft Exception -> SyncLog Error, kein Crash, kein partielles Save.

### `IdealAkeWms.Tests`

- `StorageLocationRepositoryTests.GetActiveAsync` — filtert `IsActive=false` raus, kombiniert mit `IsPickingTransport`-Filter.
- `StorageLocationsControllerTests.Edit_Source_Sage_Ignores_Code_Zone_Description_IsActive` — POST mit manipuliertem Code/Zone/Description/IsActive bei Source=Sage uebernimmt nichts. POSTete Capacity/IsPickingTransport werden korrekt uebernommen.
- `StorageLocationsControllerTests.Edit_Source_Manual_AllowsAllFields` — POST bei Source=Manual uebernimmt alle Felder inkl. Code-Aenderung und IsActive-Toggle.
- `SyncLogControllerTests.Index_FiltersByServiceAndLevel` — Index mit Filter-Querystring liefert nur passende Eintraege.

### Manuelle Verifikation (im Plan als expliziter Schritt)

- Etikettendruck mit synchronisiertem Lagerplatz: Etikett oeffnen, drucken, Barcode scannen — alles funktioniert wie bei manuellem Lagerplatz.
- Worker-Lauf gegen echte Sage-DB beobachten (DryRun-Modus per `WorkerSettings:SyncDryRun=true`).

## Operational Notes

### B1 — First-Run-Konflikt-Tsunami

Wenn das WMS bereits manuelle Lagerplaetze enthaelt, deren Codes in Sage existieren, werden bei Aktivierung des Syncs alle als Konflikt geloggt und Sage-Eintraege ignoriert (`b2`-Verhalten).

Empfohlener Workflow vor erstem Aktivieren:

1. `Sync:LagerplaetzeEnabled` zunaechst auf `false` lassen.
2. Liste der WMS-Lagerplaetze gegen Sage-Liste manuell vergleichen.
3. Lagerplaetze, die "eigentlich aus Sage stammen", per direktem DB-Update auf `Source = 'Sage'` setzen (nur Codes, die in beiden Systemen existieren). Beispiel-Skript wird im Hilfe-Eintrag bereitgestellt.
4. Erst dann `Sync:LagerplaetzeEnabled = true`.

### B2 — Konflikt-Aufloesung in Phase 1

Bekannte Limitation: Es gibt **kein UI** zur Konflikt-Aufloesung. Wenn ein Konflikt gemeldet wird, kann der Admin entweder:

- den manuellen Lagerplatz loeschen (nur moeglich wenn keine StockMovements daran haengen — sonst FK-Verletzung), oder
- per direktem DB-Update `Source = 'Sage'` setzen.

Komfortabel ist das nicht. Phase 2+ kann hier ein "Promote to Sage"-Button ergaenzen, falls in der Praxis relevant.

### B3 — SyncLog-Wachstum

Pro Lauf entstehen mind. 1 Summen-Log + Konflikt-Logs + Status-Logs. Bei 4 Laeufen/Stunde = ~35.000 Eintraege/Jahr nur fuer Lagerplatz-Sync. Phase 1 implementiert keine Retention. Falls die Tabelle in Praxis zum Problem wird, Cleanup-Logik im SyncWorker oder als SQL Agent Job nachruesten.

### Deployment-Reihenfolge

Web-Projekt zuerst deployen (laesst die EF-Migration laufen, neue Schema-Felder werden erstellt). Erst danach Service-Update einspielen. Sonst koennte der Service kurz auf alter Schema laufen und sich am `Source`-Feld verschlucken.

## Doku-Pflichten

- AppVersion in Web + Service: **v1.9.0** (Minor-Bump wegen Schema + neue Sync-Funktion).
- Changelog-Eintrag.
- Hilfeseite `Views/Help/Index.cshtml` — neuer Abschnitt "Lagerplatz-Sync mit Sage": was wird synchronisiert, Quelle-Anzeige, Konflikt-Verhalten, Sync-Protokoll-View, Empfehlung fuer First-Run.
- `PROJECT_STATUS.md` — Eintrag in Roadmap-Sektion, Phase-2 ausblicken (Lagerbestand-Uebernahme).
- `CLAUDE.md` — ServiceSettings-Tabelle ergaenzen (`Sync:LagerplaetzeEnabled`).
- Branch-Name: `feature/sage-lagerplatz-sync`.

## Annahmen, die bei Implementierung verifiziert werden

1. `KHKLagerorte` hat eine `Mandant`-Spalte — falls nicht, Filter weglassen.
2. `lp.Kurzbezeichnung` wird typischerweise <= 12 Zeichen liefern. Wenn Sage real laenger ist, muessen wir mit dem User klaeren ob Code-Cap im WMS angehoben wird (Barcode-Risiko) oder Sage-Daten ignoriert werden.
3. Eine Sage-`Kurzbezeichnung` ist innerhalb der gefilterten Liste eindeutig. Duplikate werden geloggt und alle uebersprungen — bei realer Praxis-Haeufung muss ggf. der Match-Key auf `(Zone, Code)` umgestellt werden (Schema-Aenderung).

Diese Punkte gehoeren in den Plan als explizite Verifikationsschritte zu Anfang.
