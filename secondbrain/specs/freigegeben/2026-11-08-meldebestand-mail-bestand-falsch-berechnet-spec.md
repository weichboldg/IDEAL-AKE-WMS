---
type: spec
title: Meldebestand-Mail – Bestand nach der Regel der Web-App rechnen, Artikel ohne Bewegung einbeziehen
slug: 2026-11-08-meldebestand-mail-bestand-falsch-berechnet-spec
status: Testbereit
created: 2026-11-08
updated: 2026-11-08
source_backlog: "[[2026-11-08-meldebestand-mail-bestand-falsch-berechnet]]"
bug: "[[2026-11-08-meldebestand-mail-bestand-falsch-berechnet-bug]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
version: "1.93.0"
sql_nummern: keine
testszenario: "TS-122"
standort: beide
affected_code:
  - "IDEALAKEWMSService/Services/StockCheckService.cs:16-99 (GetArticlesBelowReorderLevelAsync: raw SQL ersetzen)"
  - "IDEALAKEWMSService/Services/IStockCheckService.cs:14 (Signatur bleibt)"
  - "IDEALAKEWMSService/Program.cs:87, 96 (IStockMovementRepository und IStockCheckService schon registriert, nichts neu)"
  - "IdealAkeWms/Data/Repositories/StockMovementRepository.cs:395-486 (GetStockByArticleNumbersAsync, wiederverwendet, unverändert)"
  - "IdealAkeWms.Tests/Repositories/StockMovementRepositoryTests.cs (neue Tests Umbuchung)"
  - "IDEALAKEWMSService.Tests (neuer Test für die reine Zusammenbau-Funktion)"
  - "docs/TESTSZENARIEN.md (neu TS-122)"
open_questions:
  - "Auftrag nennt für Notifications:MeldebestandEnabled den Default 'aus'; der Code sagt 'true' (ServiceSettingDefinitions.cs:230, SQL/25_AddServiceSettings.sql:25, SQL/00_FreshInstall.sql:1107, Worker-Fallback NotificationWorker.cs:33/38/40: jeder Wert außer 'false' und eine fehlende Zeile = an). Die Spec ändert den Schalter nicht. Nicht blockierend; bitte den tatsächlichen Wert je Standort prüfen (Abfrage im Abschnitt Offene Rückfragen), weil davon abhängt, ob die korrigierte Mail nach dem Deploy sofort verschickt wird."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold (Auftrag Nachbesserung)"
freigabe_am: 2026-11-08
---

## Ziel / Nutzen (das Warum)
Die Meldebestand-Mail soll denselben Bestand zeigen wie die Web-App. Heute rechnet sie bei Umbuchungen zwischen zwei regulären Plätzen zu hoch und übersieht Artikel ohne Bewegung. Die Folge: Eine nötige Nachbestellung wird nicht gemeldet. Bug-Datensatz: [[2026-11-08-meldebestand-mail-bestand-falsch-berechnet-bug]].

## Umfang (In-Scope / Out-of-Scope)
**In-Scope**
- `StockCheckService.GetArticlesBelowReorderLevelAsync` rechnet den Bestand über die vorhandene Regel der Web-App (`GetStockByArticleNumbersAsync`), nicht mehr über eigenes SQL.
- Artikel mit `ReorderLevel > 0` ohne Bewegung erscheinen mit Bestand 0 und leerer Lagerplatzliste.
- Tests, die rot werden, wenn die Regel bricht, sowie TS-122.

**Out-of-Scope**
- Schalter `Notifications:MeldebestandEnabled`, Intervall, Empfänger, Mail-Layout (`NotificationWorker.cs` bleibt unverändert).
- Zusatzfund, nur gemeldet und nicht gebaut: Die Mail geht bei jedem Lauf (Default 60 min, `NotificationWorker.cs:25`) mit der vollständigen Liste raus. Es gibt keinen Merker „schon gemeldet“ (`:49-65`). Das ist so seit jeher und nicht Teil dieses Fixes.
- Einkauf-Epic ([[2026-11-08-einkauf-bestellvorschlaege-spec]]) nutzt diesen Code nicht. Das bleibt so.

## Fachliche Anforderungen
1. Bestand je Artikel = Summe über alle **regulären** Lagerplätze (`IsPickingTransport = 0`). Am Ziel `StorageLocationId`: +Menge bei Einbuchung, SageEinbuchung und Umbuchung; −Menge bei Ausbuchung und SageAusbuchung. An der Quelle `SourceStorageLocationId`: −Menge bei Umbuchung. Eine Umbuchung regulär→regulär wirkt netto 0, regulär→Kommissionierwagen −Menge, Wagen→regulär +Menge. Eine Ausbuchung vom Wagen wirkt 0.
2. Gemeldet wird jeder Artikel mit `ReorderLevel > 0` und Bestand ≤ `ReorderLevel` (Grenze inklusive, wie bisher `:71`). Das gilt auch, wenn der Artikel keine Bewegung hat (Bestand 0).
3. Die Spalte „Lager:“ in der Mail zeigt die regulären Plätze mit Bestand ≠ 0. Reihenfolge: zuerst der Hauptlagerplatz, dann nach Menge absteigend. Hat ein Artikel keinen solchen Platz, bleibt die Liste leer, und die Mail zeigt „—“ (`NotificationWorker.cs:146`).
   *Verhaltensänderung:* Bisher standen dort alle regulären Plätze, auf die jemals gebucht wurde, auch leere (`StockCheckService.cs:47-56`).
4. Sortierung der Mail nach Artikelnummer, wie bisher (`:72`).

## Technischer Loesungsentwurf
**Warum die Regel der Web-App und warum sie wiederverwendbar ist.**
- Die Web-App rechnet in zwei Teilen: `StockMovementRepository.GetCurrentStockAsync` (`:41-79` Ziel-Seite, `:82-131` Quell-Seite) und `GetCurrentStockByArticleAndLocationAsync` (`:544-562`, Ziel plus eigene Quell-Abbuchung je Umbuchung). Beide buchen eine Umbuchung getrennt an zwei Plätzen. Ein „erster Treffer gewinnt“ gibt es dort nicht.
- `GetStockByArticleNumbersAsync` (`:395-486`) wendet dieselbe Regel an und schließt Kommissionierwagen auf **beiden** Seiten aus (`:405`, `:435`). Die Quell-Abbuchung zählt nur bei `SourceStorageLocationId != null` (`:433`). Genau das ist die Regel aus Anforderung 1. Pickliste, Stückliste und Fehlteile nutzen sie schon (`PickingController.cs:292, 544`, `ReadOnlyBomBuilder.cs:81`, `MissingPartsLagerController.cs:54`).
- Ein gemeinsamer Ort ist möglich: Der Dienst referenziert das Web-Projekt (`IDEALAKEWMSService.csproj:28`) und registriert `ApplicationDbContext` (`Program.cs:30`) sowie `IStockMovementRepository` (`Program.cs:87`). `LagerbestandSyncService.cs:109` nutzt das Repository dort bereits.
- Eine zweite Regel in SQL wird daher **nicht** gebaut (ponytail Sprosse 2).

**Änderung in `StockCheckService`**
1. Konstruktor: `IStockMovementRepository` zusätzlich injizieren. Die Lebensdauer passt: Der Service wird im Scope aufgelöst (`NotificationWorker.cs:29-30`).
2. Artikelstamm laden: `Id, ArticleNumber, Description, Unit, ReorderLevel FROM Articles WHERE ReorderLevel > 0`. Das bleibt eine schlichte Raw-SQL-Abfrage im bisherigen Stil der Klasse und enthält keine Bestandslogik.
3. `GetStockByArticleNumbersAsync(articleNumbers)` aufrufen. Auf DEV sind das 2814 Nummern. EF 10 übersetzt das `Contains` in **einen** `OPENJSON`-Parameter, gemessen (fallstricke.md „`Contains` über eine lange Liste“). Der Dienst nutzt dieselben `UseSqlServer`-Optionen wie die Web-App (Service `Program.cs:30-33` = Web `Program.cs:20-22`), damit gilt die 2100-Parameter-Grenze nicht.
4. Neue reine Funktion `internal static List<StockBelowReorderItem> BuildBelowReorder(IEnumerable<(string ArticleNumber, string? Description, string? Unit, decimal ReorderLevel)> articles, IReadOnlyDictionary<string, List<StockLocationInfo>> stock)`. Sie arbeitet so:
   - Bestand = `stock.TryGetValue(nr) ? Sum(Quantity) : 0`.
   - Filter: Bestand ≤ ReorderLevel.
   - `StorageLocations` = Codes in der Reihenfolge des Dictionarys, sonst leer.
   - Sortierung nach ArticleNumber (ordinal).

   Die Zeilen mit Menge 0, die `:474` wegfiltert, ändern die Summe nicht.
5. Das alte SQL `:32-73` entfällt komplett, ebenso der Kommentar `:21-31`. Die Regel steht ab dann am Repository.

## Migrations-/SQL-Auswirkungen
Keine Migration, keine SQL-Datei, `00_FreshInstall.sql` und `SQL/AgentJobs/` bleiben unberührt (`sql_nummern: keine`).

## Einstellungen
keine. `Notifications:MeldebestandEnabled` bleibt unverändert (siehe Offene Rückfrage 1 zum tatsächlichen Default).

## Audit-Feld-Auswirkungen
Keine. Der Fix liest nur, es gibt keinen Schreibweg.

## Nachgestellt
Der Aufrufer hat den Fehler am 2026-11-08 auf DEV nachgestellt, nur lesend: Server `.`, DB `AKE_WMS_DEV`.

Skript: `C:\Users\GERALD~1.WEI\AppData\Local\Temp\claude\8eae370df6a92eae\8d5dd4d6-9b10-461a-954c-3abd2e65c8bf\scratchpad\b_repro.sql`. Das Skript liegt im Scratchpad und ist nicht dauerhaft. Für den Beweis nach dem Fix bitte nach `SQL/Einmalig/` oder in den QA-Nachweis übernehmen.

- 2814 Artikel haben `ReorderLevel > 0`.
- 24 Umbuchungen gehen von regulär nach regulär.
- 9 Artikel mit `ReorderLevel > 0` haben keine einzige Bewegung und fehlen deshalb ganz in der Abfrage.

| Artikel | Meldebestand | Bestand wie Service | korrekt |
|---|---|---|---|
| 00725321 | 252 | 355 | 354 |
| 00725326 | 51 | 129 | 128 |
| 00730607 | 35000 | 45396 | 45392 |
| 00000120 | ohne | 5 | 2 |
| 00395357 | ohne | 13 | 6 |
| 86015790 | ohne | 1 | 0 |
| 87026853 | ohne | 2 | 1 |
| 87050323 | ohne | 0 | -1 |
| EK018255 | ohne | 2 | 1 |
| 00000905 | ohne | -9941 | -9970 |

Ergebnis: nachgestellt, der Fehler ist belegt. Keiner der drei Artikel mit Meldebestand kippt auf DEV über die Grenze. Die Abweichung zeigt sich deshalb heute nur in der Zahl, nicht in der Liste. Die 9 Artikel ohne Bewegung wären dagegen sofort neu in der Mail.

**Mail-Flut durch Artikel ohne Bewegung: Empfehlung einbeziehen, kein Blocker.** Begründung aus dem Code:
- Der Worker verschickt pro Lauf **eine** Sammelmail über alle Treffer (`NotificationWorker.cs:49-65`). Zusätzliche Treffer verlängern die Tabelle, sie erzeugen keine zusätzlichen Mails. Auf DEV kämen 9 Zeilen dazu.
- Neu wäre nur ein Fall: Bisher war die Liste leer (keine Mail, `:51-54`), und jetzt gibt es Treffer. Dann geht pro Intervall eine Mail raus. Diese Wiederholung je Intervall gibt es heute schon für jeden Treffer (Zusatzfund oben).
- `ReorderLevel` pflegt das WMS selbst (`ArticlesController.cs:193`). Der Sage-Import übernimmt keinen Meldebestand (`SQL/AgentJobs/02_Import_Artikel.sql:8`). Wer einen Meldebestand setzt, will gewarnt werden. Bestand 0 bei Meldebestand > 0 ist eine echte Unterschreitung.

Vor dem Deploy auf PROD je Standort zählen, nur lesend, Ergebnis im QA-Nachweis festhalten:
```sql
SELECT COUNT(*) AS OhneBewegungMitMeldebestand
FROM dbo.Articles a
WHERE a.ReorderLevel > 0
  AND NOT EXISTS (SELECT 1 FROM dbo.StockMovements sm WHERE sm.ArticleId = a.Id);
```

## Akzeptanzkriterien
1. Ein Artikel mit Einbuchung 10 auf L01 und Umbuchung 4 L01→L02 (beide regulär) hat in der Mail-Rechnung den Bestand 10, nicht 14.
2. Umbuchung 4 von regulär auf einen Kommissionierwagen senkt den Bestand um 4. Eine anschließende Ausbuchung 4 vom Wagen ändert ihn nicht mehr.
3. Ein Artikel mit `ReorderLevel = 5` ohne jede Bewegung erscheint mit Bestand 0 und leerer `StorageLocations`-Liste. In der Mail steht „Lager: —“.
4. Bestand = `ReorderLevel` wird gemeldet, Bestand = `ReorderLevel` + 0,001 nicht.
5. Artikel mit `ReorderLevel` NULL oder 0 erscheinen nie.
6. `StockCheckService.cs` enthält keine eigene Bestandsregel mehr (kein `CASE`/`SUM` über `MovementType`). Der Bestand kommt aus `IStockMovementRepository.GetStockByArticleNumbersAsync`.
7. Nach dem Deploy liefert die Meldebestand-Prüfung auf DEV dieselbe Artikelmenge wie die Spalte „korrekt“ des Repro-Skripts, ergänzt um die 9 Artikel ohne Bewegung. Nachweis: Log-Zeile „{Count} Artikel …“ (`StockCheckService.cs:97`) gegen die SQL-Zählung.
8. `dotnet build IdealAkeWms.slnx` und `dotnet test` sind grün. Die Tests aus dem Abschnitt Test-Szenarien sind enthalten und schlagen gegen die alte Logik fehl.

## Test-Szenarien
**Automatisch.** Raw SQL ist InMemory nicht testbar, deshalb liegt die Regel im EF-Repository und der Zusammenbau in einer reinen Funktion.
- `IdealAkeWms.Tests/Repositories/StockMovementRepositoryTests.cs`, im vorhandenen Muster `SeedBaseData`/`CreateMovement`:
  - `GetStockByArticleNumbers_UmbuchungRegulaerZuRegulaer_NettoNull`: Summe aller Plätze = 10. Prüft AK 1 an der gemeinsamen Regel.
  - `GetStockByArticleNumbers_UmbuchungAufKommissionierwagen_ZaehltNicht`: AK 2.
- Service-Tests, neue Datei `StockCheckServiceTests` für `BuildBelowReorder`: AK 3, 4 und 5 sowie die Sortierung. Danach `IDEALAKEWMSService.Tests` per `InternalsVisibleTo` prüfen bzw. ergänzen.
- `NotificationWorkerTests` bleiben unverändert grün, das Interface ändert sich nicht.

**Manuell: TS-122 „Meldebestand-Mail: Bestandsregel“** in `docs/TESTSZENARIEN.md`, danach `secondbrain/tests/testszenarien-index.md`.

Vorbedingungen:
- DEV mit Dienst v1.93.0, `Notifications:MeldebestandEnabled = true`.
- Ein eigener Empfänger in `Notifications:Recipients`.
- `WorkerSettings:NotificationCheckIntervalMinutes` klein, z. B. 1.

Schritte:
- **122.1 Umbuchung netto 0.** Testartikel X: `ReorderLevel = 10`, Einbuchung 10 auf L01, dann Umbuchung 4 L01→L02. Erwartet: X steht in der Mail mit „Aktuell 10“ und „Lager: L01, L02“. Die alte Logik hätte 14 gerechnet und X nicht gemeldet.
- **122.2 Kommissionierwagen.** Umbuchung 3 von L02 auf einen Wagen. Erwartet: X mit 7, der Wagen fehlt in „Lager:“. Danach Ausbuchung 3 vom Wagen. Erwartet: weiterhin 7.
- **122.3 Ohne Bewegung.** Neuer Artikel Y mit `ReorderLevel = 5` und keiner Buchung. Erwartet: Y mit 0 und „Lager: —“.
- **122.4 Grenze.** X auf 10,001 Bestand bringen. Erwartet: X fehlt in der Mail. Bei genau 10 ist X wieder enthalten.
- **122.5 Abgleich Web.** Der Bestand von X und Y in der Mail stimmt mit der Summe der regulären Plätze in der Bestandsübersicht (`/StockOverview`) überein.

Negativfälle:
- **122.6** Artikel mit `ReorderLevel` leer oder 0 und Bestand 0: nicht in der Mail.
- **122.7** Alle Treffer über dem Meldebestand: keine Mail, Log „Kein Artikel unter Meldebestand“.
- **122.8** `Notifications:MeldebestandEnabled = false`: keine Prüfung, keine Mail. Das Verhalten ist unverändert.

## Etappen (nur bei epic: true)
entfällt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy
*Vorläufig, der QA-Lauf bestätigt das gegen den echten Diff.*
- **Web-App:** ja. Grund sind nur Version und Anwender-Changelog (`AppVersion.cs`, `Views/Help/Changelog.cshtml`); keine fachliche Web-Änderung.
- **Service:** ja (`StockCheckService`).
- **Migration:** nein.
- **Publish-Befehle** (im Worktree):
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- Risiko: Ist der Schalter aktiv, enthält die nächste Mail nach dem Service-Neustart sofort die korrigierte Liste, z. B. die Artikel ohne Bewegung. Die Empfänger vorab informieren. Die Zählabfrage aus „Nachgestellt“ vorher je Standort laufen lassen.
- Checkliste Dev-Lauf:
  - Version 1.93.0 in **beiden** `AppVersion.cs`
  - Anwender-Changelog
  - Brain-Changelog
  - feature-map
  - TS-122 samt Index
  - `codebase/services.md` (StockCheckService nutzt jetzt das Repository)
  - Bug-Datensatz auf `behoben`

## Offene Rueckfragen
1. **(nicht blockierend, Widerspruch Auftrag ↔ Code)** Laut Auftrag ist der Default von `Notifications:MeldebestandEnabled` „aus“. Der Code sagt „true“:
   - `ServiceSettingDefinitions.cs:230`
   - Seed `SQL/25_AddServiceSettings.sql:25` und `SQL/00_FreshInstall.sql:1107`
   - Worker `NotificationWorker.cs:33/38/40`: Eine fehlende Zeile oder jeder Wert außer „false“ bedeutet „an“.

   Diese Spec ändert den Schalter nicht. Bitte den tatsächlichen Wert je Standort prüfen, denn davon hängt ab, ob nach dem Deploy sofort Mails mit der neuen Liste rausgehen:
   `SELECT [Key],[Value] FROM dbo.ServiceSettings WHERE [Key] IN ('Notifications:MeldebestandEnabled','Notifications:Recipients');`

   Soll der Default auf „aus“ gehen, ist das eine eigene Änderung mit Seed-Anpassung und gehört nicht hierher.

## Blocker (Grundsatzfragen)
keine. Zur Mail-Flut steht eine Empfehlung mit Begründung unter „Nachgestellt“.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  Ausnahme Schranke 1: Freigabe durch Auftrag Gerald Weichbold 2026-11-08 (Nachbesserung).
-->

1. →

## Umsetzung (Dev 2026-11-08, v1.93.0)

Commit `897f7a03`. `StockCheckService` lädt nur den Artikelstamm (`ReorderLevel > 0`) und holt den Bestand aus
`IStockMovementRepository.GetStockByArticleNumbersAsync`; neue reine Funktion `BuildBelowReorder` (public static, kein `InternalsVisibleTo`
nötig). Altes CASE/HAVING-SQL entfernt. Tests: `StockMovementRepositoryAggregationTests.GetStockByArticleNumbersAsync_UmbuchungRegulaerZuRegulaer_*`,
`IDEALAKEWMSService.Tests/Services/StockCheckServiceTests`. TS-122.

## QA v1.93.0 (2026-11-08) – Ergebnis: Testbereit

qa-agent. Prüfbereich: Commit `897f7a03`.

### 1. Build, Tests, Deploy
- Build 0 Fehler; Tests Web 2542/1/0, Dienst 507/0/0 (Worktree HEAD `2efdb023`).
- AKE-DEV und IDEAL-DEV laufen auf v1.94.0 (Web und Dienst) aus dem Worktree; IDEAL hat dieser QA-Lauf deployt (Exit 0).

### 2. Code-Review
Gelesen: `StockCheckService` (neu), `StockMovementRepository.GetStockByArticleNumbersAsync:395-486`, `Program.cs:87/96` (beide Scoped; `NotificationWorker.cs:29-30` löst im Scope auf), die neuen Tests.
- Die Regel steht an einem Ort (Repository); im Dienst gibt es kein `CASE`/`SUM` über `MovementType` mehr (AK 6).
- Artikel ohne Bewegung ergeben Bestand 0 und eine leere Liste.
- Die Plätze kommen in der Repository-Reihenfolge (Hauptlagerplatz, dann Menge absteigend) und nur mit Bestand ≠ 0 (Anf. 3).
- `StockMovements.StorageLocationId IS NULL`: 0 Zeilen auf beiden DEV-DBs. Der INNER JOIN des Repositorys verliert also nichts, was die alte Abfrage gezählt hätte.

**Befunde: keine.**

### 3. Beweisart je AK
| AK | Beweis |
|---|---|
| 1, 2 Umbuchung netto 0 / Kommissionierwagen | automatisch (`StockMovementRepositoryAggregationTests`, InMemory über die EF-Regel) |
| 3, 4, 5 ohne Bewegung / Grenze / ohne Meldebestand | automatisch (`StockCheckServiceTests.BuildBelowReorder`) |
| 6 keine eigene Regel | Review |
| 7 DEV-Menge | Manual-UAT, erledigt: Der echte `StockCheckService` lief gegen beide DEV-DBs (nur lesend, Hilfsprogramm im Scratchpad, ohne Mail), Vergleich mit einer unabhängigen SQL-Zählung |
| Mailversand selbst | NICHT MOEGLICH auf DEV (`Notifications:MeldebestandEnabled = false`, darf nicht eingeschaltet werden) |

### 4. Geänderte bestehende Tests
`StockMovementRepositoryAggregationTests`: nur ein neuer Test, keine bestehende Erwartung geändert. `StockCheckServiceTests` ist neu.

### 5. Checkliste
Keine Migration, keine Einstellung. Version 1.93.0, Anwender- und Brain-Changelog (`2026-11-08-v1-93-0-meldebestand-mail-bestandsregel`) vorhanden. TS-122 in `docs/TESTSZENARIEN.md` und im Index.

### 6. DEV-Testlauf
| Schritt | Ergebnis | |
|---|---|---|
| Repro-Zahlen der Spec, AKE_WMS_DEV (SQL nach der Web-Regel) | 00725321 = 354, 00725326 = 128, 00730607 = 45392, 00000120 = 2, 00395357 = 6, 86015790 = 0, 87026853 = 1, 87050323 = -1, EK018255 = 1, 00000905 = -9970. Das entspricht Spalte „korrekt“ in allen 10 Zeilen | OK |
| AK 7 AKE_WMS_DEV | 2814 Artikel mit Meldebestand, davon 9 ohne Bewegung (alle gemeldet). SQL-Zählung „gemeldet“ = **313**; `StockCheckService.GetArticlesBelowReorderLevelAsync` liefert **313** | OK |
| AK 7 IDEAL_WMS_DEV | 1511 mit Meldebestand, 185 ohne Bewegung. SQL **339** = Dienst **339** | OK |
| Artikel ohne Bewegung | z. B. 00393279 (Melde 34): Bestand 0, Lager [] → Mail „—“ | OK |
| 122.1–122.8 echte Mail | `MeldebestandEnabled = false` auf beiden DEV-DBs | NICHT MOEGLICH, für den Menschen |

Prüfabfrage vor dem PROD-Deploy (aus der Spec) auf DEV: AKE 9, IDEAL **185** Artikel ohne Bewegung mit Meldebestand. Auf IDEAL wird die Mail nach dem Deploy deutlich länger, falls der Schalter dort an ist; die Empfänger vorher informieren.
SQL-Skript (temporär): `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude/8eae370df6a92eae/8d5dd4d6-9b10-461a-954c-3abd2e65c8bf/scratchpad/ts122.sql`.

### Deploy (aus dem echten Diff)
web: ja (nur Version/Changelog) · service: ja (`StockCheckService`) · migration: nein.
```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
```
Nach dem Merge nur dann aus main neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen kombiniert hat.

### Manuelle Checkliste (offen)
1. Je Standort vor dem Deploy den Wert `Notifications:MeldebestandEnabled` und die Zählung „ohne Bewegung mit Meldebestand“ festhalten (DEV: AKE 9, IDEAL 185).
2. Testsystem mit eigenem Empfänger: TS-122.1–122.5 (Mail-Zeilen „Aktuell“ und „Lager:“).
3. TS-122.7/122.8 Negativfälle.
