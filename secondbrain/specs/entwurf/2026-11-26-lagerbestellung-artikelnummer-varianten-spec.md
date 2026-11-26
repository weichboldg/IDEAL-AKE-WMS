---
type: spec
title: "Lagerbestellung: Artikelnummer beim Schreiben auf die Stammschreibweise normalisieren, Hinweis-Reihenfolge fest sortieren"
slug: 2026-11-26-lagerbestellung-artikelnummer-varianten-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-lagerbestellung-artikelnummer-varianten]]"
bug: "[[2026-11-26-lagerbestellung-artikelnummer-varianten-bug]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs (AddItemAsync :115-157 Trim; ggf. GetMissingPartsAsync :427 Tie-Breaker, Rueckfrage 3)
  - IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs (AddItem :68/:82/:99, QuickAdd :281; ggf. DUMMY-Sperre in QuickAdd, Rueckfrage 2)
  - IdealAkeWms/Controllers/WarehouseRequisitionsController.cs (FehlteilHinweiseAsync :217-219 explizite Sortierung)
  - IdealAkeWms.Tests (WarehouseRequisitionsApiControllerTests, WarehouseRequisitionRepositoryTests, Tests zu FehlteilHinweise)
  - IdealAkeWms/Views/Help/Changelog.cshtml
  - docs/TESTSZENARIEN.md (neues Kapitel; Querverweis TS-207 Negativfall Gross/Klein)
  - scripts/deploy/nachlauf.psd1 (Pruefung, nach Rueckfrage 1)
  - "[[2026-11-26-lagerbestellung-artikelnummer-varianten-bug]]"
open_questions:
  - "1 Bestandsdaten: nur zählen und beim Update anzeigen (Manifest-Prüfung, keine Bereinigung), oder bei Treffern zusätzlich ein Einmal-Skript zum Normalisieren (eigener Folgeauftrag)?"
  - "2 Stücklisten-Schnellerfassung (QuickAdd) nimmt heute auch den DUMMY-Artikel über den normalen Weg an (keine Sperre wie in AddItem :82). Mitsperren (überspringen mit Grund)?"
  - "3 Gleiche Sortierung (ClosedAt, dann Listen-Id absteigend) auch in der Fehlteil-Liste selbst (Repository :427, ein Tie-Breaker, stabile Seiten), oder nur im Hinweis?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Eine Artikelnummer soll in einer Lagerbestellung genau so stehen wie im Artikelstamm. Varianten mit führendem Leerzeichen oder in anderer Groß/Klein-Schreibung
umgehen sonst Prüfungen, die in C# ordinal vergleichen, und im Extremfall auch den Unique-Index. Sie machen Auswertungen unscharf (Fehlteil-Hinweis,
Bestellvorschläge, Lager-Bestand). Daneben bekommt der Hinweis „bereits als Fehlteil bestellt“ (v1.99.86) eine feste Reihenfolge, statt sich auf
Implementierungsverhalten zu stützen. Bug-Datensatz: [[2026-11-26-lagerbestellung-artikelnummer-varianten-bug]].

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Normalisieren beim Schreiben an **dem einen** Schreibweg `AddItemAsync` und an seinen Aufrufern (H-3).
2. Prüfabfrage P-1 für den Bestand (nur lesend) plus Manifest-Prüfung (nach Rückfrage 1).
3. Feste Sortierung der Listennummern im Fehlteil-Hinweis (H-1/H-2).

**Out-of-Scope**
- Bereinigung der Bestandsdaten (Einmal-Skript): nur nach Rückfrage 1 und dann als eigener Folgeauftrag.
- Andere Tabellen mit Artikelnummer-Text (`PartRequisitions`, `PickingItems` …) und der Artikel-Sync selbst. Ob der Stamm Leerzeichen enthält, misst P-1 d.
- Lese-Konsumenten (`PurchaseProposalRepository.cs:106-119`, `BdeTeilelisteService.cs:338`) bleiben unverändert. Sie profitieren automatisch von
  normalisierten Neudaten.
- Index-Namensabweichung zwischen `SQL/00_FreshInstall.sql:1883` und `SQL/88` (Zusatzfund Z-1): nur gemeldet.

## Fachliche Anforderungen

### Belegte Ausgangslage (Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, v1.99.89)

**Alle Schreibwege von `WarehouseRequisitionItems.ArticleNumber`:**

| # | Weg | Fundstelle | Was gespeichert wird |
|---|---|---|---|
| W0 | Einziges Insert (EF) | `Data/Repositories/WarehouseRequisitionRepository.cs:137-148` (`AddItemAsync`) | der übergebene Text `articleNumber`, unverändert (`:140`) |
| W1 | Lagerbestellung bearbeiten → Artikelsuche → Klick | `Views/WarehouseRequisitions/Edit.cshtml:229,275-284` → `WarehouseRequisitionsApiController.AddItem` `:65-108` → `:99` | `body.ArticleNumber` ungetrimmt. Die Oberfläche schickt `a.text.split(' - ')[0]`, also `Articles.ArticleNumber` wie gespeichert (`ArticlesApiController.cs:53`). Ein direkter API-Aufruf kann jede CI-Variante schicken: Die Suche `GetByArticleNumberAsync` vergleicht in SQL (`ArticleRepository.cs:77`) und findet sie, gespeichert wird dann die Eingabe. |
| W2 | DUMMY-Position | `WarehouseRequisitionsApiController.cs:130-162` → `:153` | Konstante `DUMMY` (`Article.cs:12`) |
| W3 | Stücklisten-Schnellerfassung (einzeln und Sammel) | `Views/Picking/Bom.cshtml:1522,1555,1594` (Wert aus `data-article-number="@item.Ressourcenummer"`, `:272/385/423/442/446`) → `QuickAdd` `:171-296` → `:281` | Eingabe **getrimmt** (`:206`), aber in Stücklisten-Schreibweise statt `article.ArticleNumber` |

Gesucht wurde nach `WarehouseRequisitionItems`, `new WarehouseRequisitionItem`, `Items.Add(`, `AddItemAsync(` und `ArticleNumber =` im ganzen Worktree
ohne Migrations und Tests. Weitere Wege gibt es nicht:
- Der Dienst liest nur (`IDEALAKEWMSService/Services/WarehouseRequisitionEmailService.cs:162,209`, Mailtext).
- `SQL/AgentJobs/` erwähnt die Tabelle nicht.
- Die `SQL/Einmalig`-Skripte lesen (`2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql:92,135`) bzw. setzen nur `QuantityPicked`
  (`2026-09-28_Hotfix-1.30.1_…sql`).
- Nach dem Insert ändern nur noch `UpdateItemQuantityAsync` (`:159-168`, Menge) und das Lager (Mengen und Status) die Zeile, nie die Artikelnummer.
- Es gibt **keinen Import**. Der Backlog nennt ihn als Möglichkeit, im Code existiert er nicht.

**Warum die Doppelung auf DEV #81 trotzdem dasteht:** Die QA hat die Testdaten per SQL eingefügt (QA-Nachweis v1.99.86, Testdaten „QA207 …“, Schritt 207.8).
Über die Anwendung ist sie bei CI-Kollation nicht erzeugbar:
- Der Duplikat-Guard `AddItemAsync :122-124` und der Unique-Index (EF `ApplicationDbContext.cs:1685-1687`, `SQL/88:25-27`) vergleichen in SQL Server,
  also CI und ohne nachlaufende Leerzeichen.
- Ein führendes Leerzeichen kommt über W1 nicht durch, weil die Artikelsuche mit `= ' ek…'` nichts findet und die Anfrage mit „Artikel nicht gefunden“
  abbricht (`:68-70`). Ausnahme: Der Stammartikel selbst hat ein führendes Leerzeichen (P-1 d).
- W3 trimmt.
- Diese Aussagen gelten **nur bei CI-Kollation der Spalte**. FreshInstall legt die DB mit `Latin1_General_CI_AS` an (`SQL/00_FreshInstall.sql:11`).
  Für die Bestands-DBs ist das nicht belegt → P-1 a.

**Was über die Anwendung entsteht:** eine Variante als Einzelwert, also kleingeschrieben oder in Stücklisten-Schreibweise (W1 per API, W3), oder mit
nachlaufenden Leerzeichen (W1 per API). In C# ordinal vergleichende Leser sehen dann einen anderen Artikel. Der Fehlteil-Hinweis ist davor geschützt
(`WarehouseRequisitionsController.cs:205-207,213-214`: Trim + OrdinalIgnoreCase).

**Reihenfolge im Hinweis (H-1/H-2):** `FehlteilHinweiseAsync` nimmt `g.Select(x => x.RequisitionId).Distinct()` (`:219`) und verlässt sich auf die
Repository-Sortierung `OrderByDescending(ClosedAt ?? MinValue)` ohne Tie-Breaker (`WarehouseRequisitionRepository.cs:427`). `MissingPartRow` trägt
`ClosedAt` (`Models/ViewModels/MissingPartRow.cs:18`).

### Anforderungen

- **F1** Jede neue Position speichert die Artikelnummer **getrimmt** (alle Wege W1-W3, Guard im gemeinsamen `AddItemAsync`).
- **F2** W1 und W3 speichern die Schreibweise des **Stammartikels** (`article.ArticleNumber`, getrimmt), nicht die Eingabe. Beide Aufrufer haben den
  Artikel schon geladen (`:68`, `:214`), es kommt also keine Abfrage dazu.
- **F3** W1 sucht den Artikel mit der getrimmten Eingabe. ` EK013128 ` per API wird damit gefunden und als `EK013128` gespeichert (heute: „Artikel nicht
  gefunden“). Die DUMMY-Sperre `:82` prüft ebenfalls den getrimmten Wert.
- **F4** Die Meldung „bereits in dieser Bestellung enthalten“ nennt die normalisierte Nummer.
- **F5** Fehlteil-Hinweis: Listennummern je Artikel absteigend nach `ClosedAt` (fehlend zuletzt), bei Gleichstand nach Listen-Id absteigend. So gilt
  „neueste zuerst“ wie E4 der Spec v1.99.86. Jede Nummer kommt einmal vor, ohne dass die Reihenfolge an `Distinct()` hängt.
- **F6** (nach Rückfrage 2) QuickAdd überspringt den DUMMY-Artikel mit Grund, wie `AddItem :82`.
- **F7** (nach Rückfrage 3) Die Fehlteil-Liste bekommt denselben Tie-Breaker.

## Technischer Loesungsentwurf

Ponytail: ein Trim im gemeinsamen Schreibweg plus zwei Argumente tauschen. Kein neuer Helfer, keine Migration.

1. **`WarehouseRequisitionRepository.AddItemAsync`** (`:115`): Die erste Zeile wird `articleNumber = articleNumber.Trim();`. Der DUMMY-Vergleich `:120`, der
   Guard `:122-124` und das Insert `:140` arbeiten danach mit dem getrimmten Wert. Das deckt alle Aufrufer ab, auch künftige.
2. **`AddItem`** (`:65-108`): `var nr = body.ArticleNumber?.Trim() ?? "";` → Suche `:68` mit `nr`, DUMMY-Prüfung `:82` mit `nr`, Aufruf `:99` mit
   `article.ArticleNumber`.
3. **`QuickAdd`** (`:281`): `article.ArticleNumber` statt `articleNumber`. `skipped` nennt weiter die Eingabe, damit der Werker sie wiedererkennt.
   Nach Rückfrage 2 kommt vor `:221` `if (string.Equals(article.ArticleNumber, Article.DummyArticleNumber, StringComparison.OrdinalIgnoreCase)) { skipped.Add(…); continue; }` dazu.
4. **`FehlteilHinweiseAsync`** (`:219`): `g.GroupBy(x => x.RequisitionId).OrderByDescending(l => l.First().ClosedAt).ThenByDescending(l => l.Key).Select(l => l.Key).ToList()`
   statt `Distinct()`. `ClosedAt` ist je Liste ein Wert. `null` sortiert in LINQ to Objects absteigend zuletzt, wie `?? DateTime.MinValue` im Repository.
   Den Kommentar `:217` anpassen.
5. Nach Rückfrage 3: `WarehouseRequisitionRepository.cs:427` `.ThenByDescending(i => i.WarehouseRequisitionId).ThenBy(i => i.Position)`.
6. **Tests** (xUnit/FluentAssertions/InMemory):
   - Repository: `AddItemAsync(" EK1 ")` speichert `EK1`. Danach wirft `AddItemAsync("EK1")` „bereits enthalten“.
   - API `AddItem` mit ` EK1 ` → gespeichert `EK1`. Mit ` DUMMY ` → 400 wie bisher.
   - API `QuickAdd`: gespeichert wird `article.ArticleNumber`. Groß/Klein ist unter InMemory nicht nachstellbar, denn die Suche ist ordinal (fallstricke
     „Groß/Klein in SQL macht die Kollation“). Bewiesen werden Trim und Stammwert, Groß/Klein ist Manual-UAT.
   - `FehlteilHinweiseAsync`: zwei Listen mit gleichem `ClosedAt` → höhere Id zuerst; eine Liste ohne `ClosedAt` → zuletzt.
7. Changelog-Eintrag. Keine Hilfe-Änderung nötig: Die Anwendersicht ändert sich nicht, außer dass ` EK…` per API jetzt angenommen wird. Die Version vergibt
   der Dev-Lauf.

## Migrations-/SQL-Auswirkungen

Keine Schemaänderung, keine EF-Migration, kein `SQL/XX_*.sql`. `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` bleiben unberührt. Kein Einmal-Skript in diesem
Auftrag (Rückfrage 1). Bestandszeilen bleiben, wie sie sind.

**Prüfabfrage P-1 (WMS-DB, nur lesend, je Standort AKE und IDEAL, DEV/Test/Prod; Ergebnis an Gerald):**
```sql
-- P-1 a) Kollation: erwartet *_CI_* fuer DB und Spalte (sonst gelten die Aussagen zum Unique-Index nicht)
SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS DbKollation,
       (SELECT c.collation_name FROM sys.columns c
         WHERE c.object_id = OBJECT_ID(N'dbo.WarehouseRequisitionItems') AND c.name = N'ArticleNumber') AS SpaltenKollation;

-- P-1 b) Positionen mit Leerraum am Rand bzw. mit anderer Schreibweise als der Stammartikel (DUMMY ausgenommen), je Listenstatus
--        DATALENGTH, weil '=' unter SQL Server nachlaufende Leerzeichen ignoriert; BIN2 fuer den exakten Vergleich
SELECT r.Status,                                       -- 1 Draft, 2 Submitted, 3 Closed, 4 Cancelled, 5 PartiallyDelivered (Models/WarehouseRequisitionStatus.cs)
       COUNT(*) AS Positionen,
       SUM(CASE WHEN DATALENGTH(i.ArticleNumber) <> DATALENGTH(LTRIM(RTRIM(i.ArticleNumber))) THEN 1 ELSE 0 END) AS MitLeerzeichenAmRand,
       SUM(CASE WHEN a.Id IS NOT NULL AND a.ArticleNumber COLLATE Latin1_General_BIN2 <> LTRIM(RTRIM(i.ArticleNumber)) COLLATE Latin1_General_BIN2
                THEN 1 ELSE 0 END) AS AndereSchreibweiseAlsStamm,
       SUM(CASE WHEN a.Id IS NULL THEN 1 ELSE 0 END) AS OhneStammartikel
FROM dbo.WarehouseRequisitionItems i
JOIN dbo.WarehouseRequisitions r ON r.Id = i.WarehouseRequisitionId
LEFT JOIN dbo.Articles a ON a.ArticleNumber = LTRIM(RTRIM(i.ArticleNumber))
WHERE LTRIM(RTRIM(i.ArticleNumber)) <> N'DUMMY'
GROUP BY r.Status ORDER BY r.Status;

-- P-1 c) Doppelte Artikel je Liste nach normalisiertem Schluessel (wie #81 auf DEV)
SELECT i.WarehouseRequisitionId, UPPER(LTRIM(RTRIM(i.ArticleNumber))) AS Schluessel, COUNT(*) AS Anzahl,
       STRING_AGG(N'[' + i.ArticleNumber + N']', N', ') AS Varianten
FROM dbo.WarehouseRequisitionItems i
WHERE LTRIM(RTRIM(i.ArticleNumber)) <> N'DUMMY'
GROUP BY i.WarehouseRequisitionId, UPPER(LTRIM(RTRIM(i.ArticleNumber)))
HAVING COUNT(*) > 1
ORDER BY i.WarehouseRequisitionId;

-- P-1 d) Stammartikel mit Leerraum am Rand (Quelle fuer W1, falls vorhanden)
SELECT COUNT(*) AS StammMitLeerzeichenAmRand
FROM dbo.Articles WHERE DATALENGTH(ArticleNumber) <> DATALENGTH(LTRIM(RTRIM(ArticleNumber)));

-- P-1 e) Unique-Indizes auf der Tabelle (Zusatzfund Z-1: Name/Filter je DB)
SELECT ix.name, ix.is_unique, ix.has_filter, ix.filter_definition
FROM sys.indexes ix WHERE ix.object_id = OBJECT_ID(N'dbo.WarehouseRequisitionItems') AND ix.is_unique = 1;
```
Bewertung:
- a ist CI, b `MitLeerzeichenAmRand` = 0 und c leer: keine Folgearbeit.
- c hat Treffer: Diese Listen haben echte Doppel (nur per SQL oder bei CS-Kollation entstanden). Zur Entscheidung an Gerald, Rückfrage 1.
- Nur `AndereSchreibweiseAlsStamm` > 0: kosmetisch, ab diesem Fix entsteht nichts Neues.

## Einstellungen

keine.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Unverändert. Das Insert setzt `CreatedAt/CreatedBy/CreatedByWindows` (`:145-147`) und an der Liste `ModifiedAt/ModifiedBy/ModifiedByWindows` (`:149-155`), wie
bisher. Kein neuer Update-Weg.

## Rollen / Zugriff

Unverändert: `[RequirePickingOrStockOrLagerbestellungAccess]` + `[RequireLagerbestellungAktiv]` am API-Controller (`WarehouseRequisitionsApiController.cs:12-13`),
Eigentümer- und Entwurfs-Guard `:52-63`. QuickAdd prüft das Recht je Typ (`:185-186,227-236`). Keine neue Rolle, kein neuer Filter.

## Listen-View-Pattern

Nicht betroffen. Es gibt keine neue Tabelle und keine neue Spalte. Rückfrage 3 ändert nur die Reihenfolge bei Gleichstand in der bestehenden Fehlteil-Liste.

## Akzeptanzkriterien

1. Lagerbestellung über die Suche um einen Artikel ergänzt → gespeichert genau `Articles.ArticleNumber` (getrimmt). Prüfung per SQL mit `DATALENGTH`.
2. `POST /api/warehouserequisitions/{id}/items` mit `" ek013128 "` (Artikel `EK013128` vorhanden) → 200, gespeichert `EK013128`.
3. Danach dieselbe Anfrage mit `"EK013128"` → 400 „Artikel 'EK013128' ist bereits in dieser Bestellung enthalten.“
4. `" DUMMY "` über `AddItem` → 400 wie bisher (DUMMY nur über den DUMMY-Weg).
5. Stücklisten-Schnellerfassung mit einer Ressourcenummer in anderer Groß/Klein-Schreibung als der Stamm → gespeichert die Stammschreibweise (Manual-UAT auf SQL).
   Ein zweiter Klick für denselben Artikel → übersprungen „bereits enthalten“.
6. DUMMY-Position (Pflicht-Bezeichnung) funktioniert unverändert, mehrere DUMMY-Positionen in einer Liste bleiben möglich.
7. Fehlteil-Hinweis: zwei Listen mit gleichem `ClosedAt` → die höhere Nummer zuerst. Liste ohne `ClosedAt` → zuletzt. Neueste zuerst bleibt (TS-207.3 unverändert).
8. Nach Rückfrage 2: QuickAdd mit `DUMMY` → übersprungen mit Grund, keine Position.
9. Nach Rückfrage 3: Die Fehlteil-Liste zeigt bei gleichem Abschlussdatum die höhere Listennummer zuerst, Seitenwechsel ohne Springen.
10. P-1 auf DEV AKE und DEV IDEAL ausgeführt, Ergebnis in der Spec festgehalten.
11. `dotnet build` und `dotnet test` grün. TS-207 (Fehlteil-Hinweis) und TS-206 als Regression grün.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md`. Die Nummer vergibt der Dev-Lauf (Stand 2026-11-26: höchste TS-209, `docs/TESTSZENARIEN.md:15085`; die Beschichtungs-Spec
von heute kann parallel laufen). Den Index nachziehen. Im Negativfall „Groß/Klein/Leerzeichen“ von TS-207 einen Querverweis setzen.

- **Vorbedingungen:** DEV AKE und IDEAL, Benutzer mit `lagerbestellung` (Werkbank zugeordnet), Artikel `X` im Stamm, Stückliste mit `X`. P-1 vorher ausführen.
- **x.1 Suche:** Entwurf, `X` über die Suche hinzufügen → SQL: gespeichert = Stammwert, `DATALENGTH` gleich (AK 1).
- **x.2 API getrimmt:** per Browser-Konsole `fetch` mit `" x "` (klein, Leerzeichen) → 200, gespeichert `X` (AK 2). Nochmals `X` → Meldung „bereits enthalten“ (AK 3).
- **x.3 DUMMY:** `" DUMMY "` über `AddItem` → abgelehnt (AK 4). DUMMY-Weg mit eigener Bezeichnung zweimal → zwei Positionen (AK 6).
- **x.4 Stückliste:** Schnellerfassung `X` → Stammschreibweise. Zweites Mal → übersprungen (AK 5). Nach Rückfrage 2: DUMMY übersprungen (AK 8).
- **x.5 Hinweis-Reihenfolge:** zwei Fehlteil-Listen mit per SQL gleichgesetztem `ClosedAt` → höhere Nummer zuerst; `ClosedAt` NULL → zuletzt (AK 7).
- **x.6 Fehlteil-Liste** (nach Rückfrage 3): gleiche Daten, Liste sortiert stabil (AK 9).
- **x.7 Regression:** TS-207.1/207.3 und Negativfall Groß/Klein (` ek…` im Entwurf → Hinweis) weiter OK. Altdaten mit Variante werden weiter erkannt.
- **Negativfälle:** fremde Liste bzw. nicht Entwurf → 403/400 wie bisher. Unbekannter Artikel → „Artikel nicht gefunden“.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | (kein Epic) | – | – |

## Deploy

*Vorläufig. Der QA-Lauf bestätigt es am echten Diff.*

- **Web-App:** ja (API-Controller, Repository, `WarehouseRequisitionsController`, Changelog).
- **Service:** nein (fachlich; nur die Versionsnummer).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022), nach Rückfrage 1 (Empfehlung: anlegen):
  - `Id='Pruefung/LagerbestellungArtikelnummerVarianten'`; `Version=<Dev-Lauf>`; `Typ='Pruefung'`; `Firma='beide'`; `Phase='Nachher'`; `Ausfuehrung='anzeigen'`.
  - `Pruefung` = Zahl der Positionen aus P-1 b (`MitLeerzeichenAmRand`) plus Zahl der Doppel aus P-1 c, mit `OBJECT_ID`-Wächter wie `Pruefung/FaStrukturSchluesselDoppelt`
    (`nachlauf.psd1:139-142`).
  - `Text='Lagerbestellungen mit Artikelnummer-Varianten (Leerzeichen am Rand bzw. doppelt je Liste): seit <Version> normalisiert die Anwendung neue Positionen; Altbestand mit P-1 der Spec 2026-11-26-lagerbestellung-artikelnummer-varianten an Gerald, nicht automatisch bereinigen.'`
  - `Quelle=<Brain-Changelog-Slug>`.
  - Kein `Datei`, keine Reparatur, kein Trockenlauf-Skript (ADR 0023 greift erst bei einem Einmal-Skript).
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
  Reihenfolge: keine Besonderheit, kein DB-Schritt.

## Zusatzfunde (nur gemeldet, nicht im Umfang)

- **Z-1 Indexname weicht ab.** `SQL/53` und `SQL/00_FreshInstall.sql:1883` legen `IX_WarehouseRequisitionItems_RequisitionId_ArticleNumber` an. `SQL/88:11-27` dropt
  und baut aber `IX_WarehouseRequisitionItems_WarehouseRequisitionId_ArticleNumber` (EF-Name). Auf einer DB, die per `SQL/53` entstanden ist, bliebe der alte
  **ungefilterte** Index neben dem neuen bestehen. Mehrere DUMMY-Positionen je Liste schlügen dann mit Fehler 2601 fehl. Unbelegt, welche DB welchen Stand hat:
  P-1 e zeigt es. Bei zwei Unique-Indizes bitte eigener Backlog-Eintrag.
- **Z-2** `Edit.cshtml:229` nimmt die Artikelnummer aus dem Anzeigetext (`split(' - ')[0]`). Eine Artikelnummer, die selbst „ - “ enthält, würde abgeschnitten und
  dann „nicht gefunden“. Ein `data-art` aus einem eigenen API-Feld wäre robuster. Nicht gemessen, ob es solche Nummern gibt
  (`SELECT COUNT(*) FROM dbo.Articles WHERE ArticleNumber LIKE N'% - %'`).

## Offene Rueckfragen

1. **Bestandsdaten.** P-1 zählt die Varianten. Varianten: (a) nur zählen und bei jedem Update als Manifest-Prüfung „anzeigen“, nichts bereinigen; (b) zusätzlich ein
   Einmal-Skript (Trockenlauf/SCHARF, ADR 0023), das Altbestand auf die Stammschreibweise setzt. Doppel je Liste (P-1 c) müssten dabei zusammengeführt werden, das
   braucht eine fachliche Regel. **Empfehlung (a)** jetzt. (b) nur, wenn P-1 c Treffer außerhalb der QA-Testdaten zeigt, dann als eigener Auftrag.
2. **DUMMY in der Stücklisten-Schnellerfassung.** `QuickAdd` (`:204-289`) hat keine DUMMY-Sperre. Mit dem Stammwert (F2) würde ein `dummy` aus einer Stückliste als
   DUMMY-Position mit der Standard-Bezeichnung angelegt. `AddItem :79-83` verbietet genau das („Bezeichnung zwingend geändert“, Teil-7). **Empfehlung:** mitsperren
   (überspringen mit Grund „DUMMY nur über ‚Artikel nicht gefunden → DUMMY-Position‘“), eine Zeile.
3. **Sortierung auch in der Fehlteil-Liste?** Das Repository sortiert nur nach `ClosedAt` (`:427`). Bei Gleichstand ist die Reihenfolge der Liste und ihrer Seiten
   (Skip/Take) nicht festgelegt. **Empfehlung:** ja, Tie-Breaker `WarehouseRequisitionId` absteigend, dann `Position`, eine Zeile. Der Hinweis wäre dann zusätzlich
   abgesichert.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

(kein Jira-Vorgang)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Bestandsdaten: (a) nur zählen + Manifest-Prüfung „anzeigen“ / (b) zusätzlich Einmal-Skript als Folgeauftrag →
2. DUMMY in der Stücklisten-Schnellerfassung mitsperren? →
3. Tie-Breaker (Listen-Id, Position) auch in der Fehlteil-Liste? →
