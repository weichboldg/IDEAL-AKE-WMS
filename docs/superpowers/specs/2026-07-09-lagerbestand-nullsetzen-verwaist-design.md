# Lagerbestand-Nullsetzen bei verschwundenen Sage-Zeilen — Design

**Status:** Approved (Design), wartet auf Plan
**Version:** in v1.25.0 gefaltet (kein AppVersion-Bump), Service-seitig
**Scope:** Bugfix im `LagerbestandSyncService` (IDEALAKEWMSService)

## Problem

Der Lagerbestand-Sync (`LagerbestandSyncService.RunAsync`) liest alle Sage-Bestandszeilen
(`ISageBestandReader.GetAllAsync`) und bucht pro `(Artikel, Sage-Lagerplatz)` eine
Korrektur (`SageEinbuchung`/`SageAusbuchung`), sodass der WMS-Bestand dem Sage-Bestand
entspricht (`delta = sageBestand - wmsBestand`).

**Bug:** Wenn der Sage-Bestand eines Artikels auf einem Lagerplatz auf **0** fällt,
liefert Sage die Zeile **gar nicht mehr**. Der Sync sieht das Paar dann nie mehr und
lässt den alten WMS-Bestand stehen — die 0-Änderung fällt durch. Klassischer
„WHEN NOT MATCHED BY SOURCE"-Fall (wie bei der FA-Reconciliation).

## Ziel

Nach der bestehenden Korrektur-Schleife: für jedes `(Artikel, Lagerplatz)` mit
WMS-Bestand **≠ 0** auf einem **Sage-Quelle + aktiven** Lagerplatz, dessen Paar **nicht**
im aktuellen Sage-Snapshot vorkommt, eine Korrektur auf **exakt 0** buchen.

## Nicht-Ziel / bewusst ausgeschlossen

- **Nicht Sage-kontrollierte Bestände** bleiben unberührt: manuelle Lagerplätze
  (`Source != Sage`), inaktive Sage-Plätze (`IsActive == false`), Kommissionierwagen und
  NAN (die sind manuell/Source=Manual) — **exakt dieselbe Filter-Menge, die schon die
  Korrektur-Schleife bedient**. Kein zusätzlicher Enable-Schalter (das Nullsetzen ist
  fester Bestandteil des Syncs, der ohnehin hinter `Sync:LagerbestandEnabled` (Default AUS)
  liegt).

## Ablauf (in `LagerbestandSyncService.RunAsync`, NACH der Korrektur-Schleife, VOR `SaveChangesAsync`)

1. **„In-Sage-vorhanden"-Set** `sagePresentKeys: HashSet<(int ArticleId, int LocationId)>`:
   Über **alle roh gelesenen** Sage-Zeilen (VOR dem Dedup!) iterieren, Artikel+Lagerplatz
   auf WMS-IDs auflösen, jedes auflösbare Paar aufnehmen — **inklusive der Duplikat-Keys**
   (die sind in Sage vorhanden, nur mehrdeutig → dürfen NICHT genullt werden) und
   inklusive Paare auf inaktiven/manuellen Plätzen (schaden nicht, sind ohnehin keine
   Kandidaten).
2. **Managed-Bestand** `managedStock: Dictionary<(int,int), decimal>`: der vorab geladene
   `wmsStock` gefiltert auf: Lagerplatz `Source == Sage` **UND** `IsActive` **UND**
   `qty != 0m`. (`wmsStock` = `GetCurrentStockByArticleAndLocationAsync()` — enthält auch
   Netto-0-Paare und Umbuchungs-Quellseiten-Keys → beides muss rausgefiltert werden.)
3. **Reine Planer-Logik** `LagerbestandZeroingPlanner.Plan(sageRowCountRaw, sagePresentKeys,
   managedStock, maxPerRun)` → `LagerbestandZeroingPlan(ToZero, Skipped, SkipReason)`:
   - **Leer-Guard:** `sageRowCountRaw == 0` → `Skipped`, Reason „Sage-Read leer (0 Zeilen)".
   - **Kandidaten:** `managedStock`-Keys, die **nicht** in `sagePresentKeys` sind.
   - **Cap:** `candidates.Count > maxPerRun` → `Skipped`, Reason „Cap ueberschritten:
     {count} > {max} — kein Nullsetzen".
   - sonst `ToZero = candidates` (mit Menge).
4. **Ergebnis:**
   - `Skipped` → Warn-Zeile im Aktivitäts-Protokoll (`run.LogWarningAsync`, Reason). Bei
     **Cap** zusätzlich `ISyncErrorNotifier.NotifyAsync("Lagerbestand-Nullsetzen: Cap
     ueberschritten", <synthetische Exception mit Detail>, ct)`. **Kein throw** — die
     bereits gebuchten Korrekturen bleiben, Lauf endet `FinishSuccess`.
   - sonst je `ToZero`-Eintrag eine `StockMovement`-Korrektur auf 0:
     `delta = 0 - wmsBestand`; `MovementType = wmsBestand > 0 ? SageAusbuchung :
     SageEinbuchung`; `Quantity = Math.Abs(wmsBestand)`; Note „Sage-Korrektur: in Sage
     nicht mehr vorhanden → auf 0 gesetzt (WMS war {wmsBestand})". Audit-Felder wie die
     bestehenden Sage-Korrekturen (`WindowsUser = SyncUser`, `CreatedByWindows = MachineName`).
     Count `nullgesetzt++`; je Buchung eine Info-Detailzeile (gecappt auf 100 Zeilen/Lauf,
     der Count zählt ALLE).
5. `SaveChangesAsync` (bestehend) speichert Korrekturen + Nullbuchungen gemeinsam. DryRun
   respektiert (keine Writes, aber Count + Log). Counts-Dict um `nullgesetzt` erweitern.

## Konfiguration

- Neuer Katalog-Key `Sync:LagerbestandNullsetzenMaxPerRun` (`ServiceSettingType.Int`,
  Default `"100"`, Kategorie `Sync`) in `IdealAkeWms/Models/ServiceSettingDefinitions.cs` +
  appsettings.json (Service). Gelesen im Service via
  `ServiceSettings.GetIntSafeAsync(_config, "Sync:LagerbestandNullsetzenMaxPerRun", 100, ct)`
  → `IConfiguration` in den Service injizieren.

## Betroffene Dateien

- `IDEALAKEWMSService/Services/LagerbestandZeroingPlanner.cs` (**neu**, reine Logik + Records)
- `IDEALAKEWMSService/Services/LagerbestandSyncService.cs` (Zeroing-Block + ctor:
  `IConfiguration` + `ISyncErrorNotifier` ergänzen)
- DI-Registrierung des Service (Service-`Program.cs`/Worker-Setup) — ctor-Args nachziehen
- `IdealAkeWms/Models/ServiceSettingDefinitions.cs` (+ appsettings.json Service)
- `IDEALAKEWMSService.Tests/…` (**neu**): Planner-Unit-Tests + LagerbestandSyncService-InMemory-Tests
- Doku: Changelog v1.25.0, CLAUDE.md (Fallstrick), TESTSZENARIEN, PROJECT_STATUS

## Tests (TDD)

- **Planner (rein, xUnit):** (a) leerer Read → Skipped „leer"; (b) verwaistes Paar (in
  managedStock, nicht in sagePresentKeys) → in ToZero; (c) Paar in sagePresentKeys → NICHT
  in ToZero; (d) Kandidaten > Cap → Skipped „Cap"; (e) Kandidaten == Cap → nicht skipped.
- **Service (InMemory + Mocks):** (f) Sage-Snapshot ohne ein Paar, das WMS-Bestand > 0 auf
  Sage-aktivem Platz hat → genau eine `SageAusbuchung` auf 0 wird gebucht; (g) leerer
  Sage-Read → keine Nullbuchung; (h) Cap überschritten → keine Nullbuchung + `NotifyAsync`
  aufgerufen; (i) verwaistes Paar auf **manuellem**/inaktivem Platz → NICHT genullt;
  (j) Duplikat-Key in Sage → NICHT genullt; (k) DryRun → keine Writes.

## Fallstricke (verifiziert)

1. **`GetCurrentStockByArticleAndLocationAsync` enthält Netto-0-Paare** (z.&nbsp;B. +5/−5 →
   Key mit 0m im Dict) **und Umbuchungs-Quellseiten-Keys** → managedStock MUSS auf
   `qty != 0` **und** Sage-Quelle+aktiv filtern, sonst würde man 0-Paare/Fremdplätze „nullen".
2. **Duplikat-Keys** (die die bestehende Logik aus `sageRows` entfernt) müssen trotzdem ins
   `sagePresentKeys` → sonst würden mehrdeutige, in Sage vorhandene Paare fälschlich genullt.
   `sagePresentKeys` daher aus den **roh gelesenen** Zeilen (vor Dedup) bauen.
3. **Leer-Guard auf Roh-Read-Count** (vor Dedup) — nicht auf die deduplizierte Liste.
4. **Snapshot-Konsistenz:** `wmsStock` wird EINMAL vor der Korrektur-Schleife geladen; die
   Nullsetz-Kandidaten nutzen denselben Snapshot. Korrektur-gebuchte Paare sind per
   Definition in `sagePresentKeys` → nie Kandidat. Kein Doppel-Buchen.
5. **Kein neuer `MovementType`** — `SageAusbuchung`/`SageEinbuchung` werden wiederverwendet
   (schon in der Aggregation an 5 Stellen berücksichtigt). Keine Aggregations-Änderung nötig.
6. **ctor-Erweiterung** (`IConfiguration` + `ISyncErrorNotifier`) — es gibt aktuell KEINE
   `LagerbestandSyncService`-Tests, aber die **DI-Registrierung im Service** muss nachgezogen
   werden (sonst Laufzeit-DI-Fehler beim Start).
7. **Drift-Guard-Test** `ServiceSettingDefinitionsTests.All_ContainsDocumentedServiceReadKey`
   — beim neuen Cap-Key prüfen, ob die InlineData/Doku-Liste ergänzt werden muss.
8. **`ISyncErrorNotifier.NotifyAsync(stepName, Exception, ct)`** — Cap-Fall übergibt eine
   synthetische Exception; Notifier öffnet eigenen Scope, wirft nie. Cap-Skip wirft NICHT
   im Service (Korrekturen bleiben, `FinishSuccess`).
9. **NAN + Kommissionierwagen** sind `Source = Manual` → durch den Sage-Quelle-Filter
   automatisch ausgeschlossen (verifizieren, dass NAN wirklich Manual ist).
10. **Vorzeichen:** WMS-Bestand kann negativ sein (Sage-Platz) → `delta = -wmsBestand`
    bringt exakt auf 0 (bei negativem Bestand `SageEinbuchung`).
