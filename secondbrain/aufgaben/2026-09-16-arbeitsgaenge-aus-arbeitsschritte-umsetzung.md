---
typ: notiz
spec: "[[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: IDEAL — FaWorkSteps aus FaHierarchyNode.Arbeitsschritte (Struktur statt Heuristik)

Spec [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]] · **bestehender Bündel-Worktree** (kein neuer,
nicht von `main` abgezweigt — Spec „Reihenfolge": „kein neuer Worktree, im Bündel-Zweig"). Version
**1.41.0**, Testkapitel **TS-76**. Keine Migration.

Voraussetzung für Teil-8-UAT (IDEAL hat kein OSEON → ohne diesen Baustein keine `FaWorkSteps` für
hierarchische Aufträge, seit dem Klasse-D-Gate v1.36.0). Dritte Struktur-Ableitung der Familie
(Werkbank aus `Arbeitsbereich`, `HasCoatingParts` aus `Beschichtet`, jetzt Arbeitsgänge aus
`Arbeitsschritte`).

## Pre-Flight-Verdikt (2026-09-16)

- status Freigegeben, `open_questions: []`, `freigabe_entscheidung/_von/_am` gesetzt → gültig.
- **B2 (eigener vs. parametrisierter Service) geklärt:** Freigabe-Antwort-Prosa forderte zunächst
  „prüfen ob parametrisierter Bestandsservice reicht / melden statt still bauen" — der Mensch hat das
  in „KORREKTUR meiner Antwort 1" + „AUFLÖSUNG von B1" + 3. kritischer Durchsicht (alle 2026-09-18)
  **selbst widerlegt** (5 gemessene Unterschiede; IDEAL sucht gar nicht) und **eigener Service
  bestätigt**. Kein offener „melden statt bauen"-Auftrag.
- Kritische Prüfung (2026-09-18, post-Freigabe): **BLOCKER keiner.** Offen für den Dev-Lauf: **S4**
  (Deploy-Doku mit B1-Auflösung harmonisieren: Deploy-zuerst statt Katalog-vorab) + H4–H6 (Betrieb).

## Vorab am Worktree-Code verifiziert (2026-09-16, vor Umsetzung)

- **1:1-Vorlage `FaWorkStepDetectionService.cs`** gelesen: `BeginRunAsync` → try → `FinishSuccessAsync`
  (deutsche Counts-Keys), `LogInfoAsync(msg, reference:)`, `SyncResult(Inserted,Updated,Errors)`
  (`ISageImportService.cs:3`), Nur-hinzufügen-Filter `!_db.FaWorkSteps.Any(f => f.ProductionOrderId==o.Id
  && f.WorkStepId==step.Id)`, Fertig-/Storno-Filter `!IsDone && !IsCancelled && !(PickingStatus!=null &&
  IsDonePicking)`. **Wichtig:** Vorlage hat internen `IHierarchicalModeReader`-Gate — **mein Service NICHT**
  (Design A: Gate nur im SyncWorker, Vorbild `FaMaterializationSyncService` ohne internen Gate).
- **`FaWorkStepSources`** (`Models/FaWorkStep.cs:3-7`) hat nur `Sync`/`Manual` → `Struktur = "Struktur"`
  ergänzen. `Source` ist NVARCHAR(20) (Zeile 27), passt.
- **`WorkStep.Code`** `[StringLength(20)]`, `IsActive` vorhanden (`Models/WorkStep.cs`).
- **`FaHierarchyNode`**: `SubFA` (int), `VaterFA` (int?), `Arbeitsschritte` (string?, leerzeichen-getrennt)
  vorhanden. `FaMaterializationSyncService` iteriert `Where(n => n.SubFA != 0)` und matcht über
  `ProductionOrder.SubOrderNumber == SubFA.ToString()` (Zeile 79-97) — identische Grundmenge/Zuordnung.
- **S1-Mail-Muster:** `IUnknownWorkplaceState.HasChanged(list)` + `SendUnknownWorkplaceDigestAsync`
  (`FaMaterializationSyncService.cs:227/329`) → `IMailService.SendAsync(subject, html, recipients, text, ct)`,
  Gate `ErrorNotification:Enabled` + `ErrorNotification:Recipients`. 1:1-Kopie als
  `IUnknownWorkStepTokenState` (eigene Singleton-Instanz).
- **SyncWorker-Platzierung:** neuer Block NACH FA-Materialisierung (`SyncWorker.cs:357-369`), vor
  Lagerbestand (:371). Doppel-Gate `ProduktionsauftragHierarchisch` + `Sync:FaWorkStepStructureDetectionEnabled`,
  beide `GetBoolSafeAsync`, `RunResilientAsync` (S2 erfüllt — Nachbar-Block-Konvention).
- **Program.cs:** `IUnknownWorkplaceState`-Singleton (:51-52) + `IFaWorkStepDetectionService`-Scoped (:71)
  als Registrierungs-Vorbild.
- **SyncLogServices:** neue Konstante `FaWorkStepStructureDetection` + `.All`-Eintrag.

## Arbeitsstand

- 2026-09-16: Spec Freigegeben → InUmsetzung. Referenzcode gelesen, Umsetzung startet im Bündel-Worktree.
- 2026-09-16: **Umsetzung feature-complete** (Worktree-Commits `716a676`→ core `wip` → tests `wip` →
  feature-complete `wip`). Kern: `FaWorkStepStructureDetectionService` (+Interface),
  `UnknownWorkStepTokenState` (+Interface), `FaWorkStepSources.Struktur`, Service-Key
  `Sync:FaWorkStepStructureDetectionEnabled`, `SyncLogServices.FaWorkStepStructureDetection`,
  SyncWorker-Doppel-Gate-Block nach FA-Materialisierung, 2× DI. **14 Unit-Tests** grün.
  Build 0 Fehler, **Web 1391 +1 skip, Service 277** grün (+14). Version **1.41.0** ×2.
  Doku: Changelog v1.41.0, TS-76 (10 Szenarien), Hilfeseite Zwei-Lauf-Detail, README-Key.
  Brain: Changelog-Notiz, feature-map, codebase/services, [[fallstricke]] §13, testszenarien-index,
  Spec-Deploy S4-harmonisiert (Deploy-zuerst). **Keine Migration.**
- **Umsetzungsabweichungen (bewusst):** (1) Kein interner Master-Gate im Service (Spec Design A) —
  Doppel-Gate nur im SyncWorker. (2) Nur-hinzufügen-Prüfung als In-Memory-`HashSet<(orderId,stepId)>`
  statt `.Any()`-Subquery je Kandidat (eine Abfrage statt N) — Verhalten identisch. (3) H3: alle
  datenabhängigen AKs (3/5/8) + Gate (1/2) + Mailversand sind Manual-UAT; der Algorithmus ist voll
  InMemory-getestet.
- **Offen:** qa-agent (setzt `Testbereit`), dann Schranke 2 mit dem ganzen Bündel. Kein Merge/Push.
- 2026-09-16: **qa-agent:** Build 0 Fehler; Test Web 1391+1skip/0 Fehler, Service 277/0 Fehler (genau
  wie erwartet). AK 1–12 gegen den echten Diff abgeglichen (Tabelle im Spec-QA-Abschnitt); AK 1/2/3/5/8
  + Mailversand korrekt als Manual-UAT markiert (H3), Rest automatisiert grün. `docs/TESTSZENARIEN.md`
  TS-76 (10 Szenarien) geprüft — deckt AK 1–12 + H6 vollständig. **Lücke gefunden und geschlossen:**
  `secondbrain/tests/testszenarien-index.md` hatte trotz gegenteiliger Notiz oben **keine** TS-76-Zeile
  — vom qa-agent nachgetragen. Spec-Status → `Testbereit`, Deploy-Abschnitt finalisiert (web=true,
  service=true, migration=false, Publish-Befehle + Re-Publish-Hinweis nach Merge), QA-Nachweis +
  10-Punkte-Manual-Checkliste an die Spec angehängt. `feature-map.md` Statuszeile auf `Testbereit`
  gezogen. Kein Merge/Push — wartet auf Schranke 2 mit dem ganzen Bündel.
