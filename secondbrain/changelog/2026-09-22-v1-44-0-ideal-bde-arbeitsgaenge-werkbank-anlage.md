---
type: changelog
version: 1.44.0
date: 2026-09-22
---
# v1.44.0 — IDEAL: BDE-Arbeitsgänge (WorkOperation) aus der Struktur + Werkbank-Anlage aus Sage

Umsetzung des freigegebenen **Epics** [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] (zwei
Etappen, EIN Merge) im **Bündel-**Worktree `feature/2026-08-07-ideal-teile-1-5`. Umsetzungsnotiz
[[2026-09-22-ideal-bde-arbeitsgaenge-aus-struktur-umsetzung]]. **Ersetzt v1.41.0**
([[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]], superseded). Setzt den ADR-0014-Rückbau
(v1.43.0) voraus. **Migration 93** (nicht daten-destruktiv).

**Warum:** Am BDE-Terminal bucht IDEAL gegen `WorkOperation`, nicht gegen `FaWorkStep` — v1.41.0 zielte
auf die falsche Tabelle (`FaWorkStep`-Zeilen erscheinen am Terminal nie). Zugleich fehlten IDEAL die
Werkbänke ganz (0 Zeilen). Ein Arbeitsgang IST bei IDEAL ein Sage-Arbeitsplatz (`KHKPpsArbeitsplaetze`),
und ein Sage-Arbeitsplatz IST die Werkbank — die Zuordnung existiert in Sage (`USER_ArbeitsSchritt`),
keine neue Stammdatenpflege nötig.

## Etappe A — Werkbank-Anlage aus Sage (Baustein a), Commits `8be03982` (Backend) + `be0695fb` (Views)

- **`ProductionWorkplace`** +2 Felder: `SageArbeitsplatznummer` (NVARCHAR(31), Verknüpfungsschlüssel,
  **eindeutiger auf `IS NOT NULL` gefilterter Index**) und `ArbeitsschrittCode` (NVARCHAR(20), Match-Feld).
  **Migration `20260922083259` / `SQL/93`** (2 COL_LENGTH-Guards + Index mit eigenem Guard), FreshInstall
  beide Stellen.
- **`ProductionWorkplaceSyncService`** (neu) + `SageArbeitsplatzReader` (raw ADO.NET, `SageConnection`):
  liest aktive Arbeitsplätze (`Mandant`, `Aktiv=-1`, `USER_ArbeitsSchritt` gefüllt, nicht auf
  Ausschlussliste), **legt fehlende Werkbänke an** (Sage führend, `BdeAktiv=false`), zieht Abweichungen
  nach UND meldet sie (ADR-0014-Melde-Muster für Abweichungen), Insert je Zeile (eindeutiger Index
  isoliert eine Zeile), Verstoß je Zeile abgefangen + gemeldet, Lauf läuft weiter. Schlüssel: Zeichenkette,
  getrimmt, Ordinal.
- 3 ServiceSettings (`Sync:ProductionWorkplaceSyncEnabled`/`...Mandant`/`...Ausschlussliste`), 2
  Standorteinstellungen-Felder (Gruppe „Werkbänke"), `SyncLogServices.ProductionWorkplaceSync`,
  `IUnknownArbeitsplatzState` (S1), SyncWorker-**Doppel-Gate**, DI. UI `/ProductionWorkplaces`:
  Sage-Felder read-only, 2 neue Spalten + Spaltenfilter.

## Etappe B — WorkOperation-Struktur-Erkennung + Existenz-Check-Fix (Bausteine b+c), Commit `920aabb7`

- **2c-Umbau** `FaWorkStepStructureDetectionService` → **`WorkOperationStructureDetectionService`**:
  Ziel `WorkOperation` (statt `FaWorkStep`), Katalog `ProductionWorkplace.ArbeitsschrittCode` (statt
  `WorkStep.Code`). `OperationNumber` = Kürzel, `Name`/`ProductionWorkplaceId` aus der Werkbank kopiert,
  `Sequence` = Token-Position. Eindeutigkeit `(ProductionOrderId, OperationNumber)`, Nur-hinzufügen.
  Kein Katalog-FK → kein Zwei-Lauf-Ablauf mehr.
- **Neu ggü. v1.41.0:** Kürzel-**Mehrdeutigkeit** (mehrere Werkbänke gleicher Code) → gemeldet, kein
  Insert; **Ausschlussliste**-Kürzel → weder Arbeitsgang noch Unbekannt-Meldung (bewusst bekannt);
  unbekannte Kürzel → gemeldet (S1). `FaWorkStepSources.Struktur` **entfällt ersatzlos**.
- **Umbenennungen** (reines Rename, v1.41.0 nie deployt): Interface, `IUnknownWorkStepTokenState` →
  `IUnknownArbeitsschrittTokenState`, Key `Sync:FaWorkStepStructureDetectionEnabled` →
  `Sync:WorkOperationStructureDetectionEnabled`, `SyncLogServices.WorkOperationStructureDetection`,
  SyncWorker/Program.cs.
- **Baustein c:** `BdeDefaultWorkOperationService`-Existenz-Check über `OperationNumber == "01"` statt
  Name (AK 16) — sonst bucht NurFA still auf einen echten, namensgleichen Arbeitsgang.
  **`BdeScanResolver` unverändert** (AK 15); Terminal-Routing läuft bereits über `ProductionWorkplaceId`.

## Migration / Deploy

- **Migration 93** `AddProductionWorkplaceSageFields` — 2 nullable Spalten + eindeutiger gefilterter
  Index. Nicht daten-destruktiv, aber der erste scharfe Baustein-(a)-Lauf legt bei IDEAL ~60 Werkbänke an.
- **DryRun vor dem scharf schalten.** Doppel-Gate: Master `ProduktionsauftragHierarchisch` + je Toggle.
  Reihenfolge: erst `Sync:ProductionWorkplaceSyncEnabled` (Katalog), dann
  `Sync:WorkOperationStructureDetectionEnabled` (Arbeitsgänge). AKE (Master false) unberührt.
- Deploy: **Web + Service + Migration**. Merge/Deploy erst mit dem ganzen Bündel (Schranke 2).

## Nachweis

Build 0 Fehler; **Web 1392 + 1 skip / 0 Fehler**, **Service 268 / 0 Fehler**. Automatisiert: Baustein a
(6 Tests), Baustein b (13 Tests inkl. Mehrdeutigkeit AK 11), Existenz-Check-Fix (AK 16). **Manual-UAT**
(nicht InMemory-testbar): AK 10 (Ausschlussliste, ServiceSettings-DB), AK 20 (Doppelanlage, UNIQUE nicht
InMemory), alle Sage-Reads, Terminal-Anzeige — TS-77. Dauerwissen: [[fallstricke]] (drei Sage-Vokabulare),
[[services]] (Sync-Services-Tabelle), [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]
(Melde-Muster, hier zusätzlich mit Anlage).
