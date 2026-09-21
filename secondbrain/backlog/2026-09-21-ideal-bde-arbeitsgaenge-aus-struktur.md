---
typ: feature
---
# IDEAL: BDE-Arbeitsgänge (WorkOperation) aus der Struktur + Werkbank-Routing aus Sage

**Vorgemerkt am 2026-09-21** nach gemeinsamem Brainstorming (Gerald + Claude). Löst das eigentliche
Ziel hinter [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]] (v1.41.0): die Struktur-Arbeitsgänge
sollen am **BDE-Terminal buchbar** sein.

> [!warning] Kernbefund — v1.41.0 zielte auf die falsche Tabelle
> v1.41.0 füllt `FaWorkStep` (FA-Abarbeitungsliste). Das **BDE-Terminal bucht aber gegen `WorkOperation`**
> (`BdeApiController`, `BdeScanResolver`, `BdeBooking` — verifiziert; kein FK/Verbindung zwischen
> `FaWorkStep` und `WorkOperation`). `FaWorkStep`-Zeilen erscheinen also **nie** am BDE-Terminal.
> Entscheidung: v1.41.0 ist für IDEAL das falsche Ziel und wird umgebaut (siehe „Entschieden 3").

## Ausgangslage / Warum

- IDEAL hat **kein OSEON** → keine `WorkOperation`-Zeilen aus dem OSEON-Sync. Heute legt
  `BdeDefaultWorkOperationService` beim Buchen nur **einen generischen Default-AG** je Werkbank an
  (`OperationNumber "01"`, Name = `ProductionWorkplace.BdeDefaultArbeitsgang`). Der Werker sieht also
  einen Sammel-AG statt der echten Arbeitsgänge (KA/LS/SW…).
- Die echten Arbeitsschritte je Sub-FA liefert die Struktur mit: `FaHierarchyNode.Arbeitsschritte`
  (leerzeichen-getrennte Kürzel, [[sage-views-ideal]]).
- **Zweiter Kernbefund (Sage-Arbeitsplatz-Stammdaten):** Jede Werkbank trägt in
  `USER_ArbeitsSchritt` genau ihr Kürzel, und die Arbeitsgang-**Namen** sind die Werkbank-Bezeichnungen.
  **Ein Arbeitsgang *ist* faktisch eine Werkbank** (KA = „Kanterei W1", SW = „Schweißerei W1",
  SÄ = „Schäumerei W1 (EG)" …). Die Werkbank↔Arbeitsgang-Zuordnung existiert damit **in Sage schon** —
  keine neue manuelle Stammdaten nötig (Sage führend, [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]).

## Entschieden (Schranke-1-Vorlage, aus dem Brainstorming)

1. **Ziel-Tabelle `WorkOperation`** (BDE-Terminal), nicht `FaWorkStep`. Die `FaWorkStep`-Tabelle bleibt
   für den AKE-Pfad unverändert (`FaWorkStepDetectionService`).
2. **Werkbank-Routing aus Sage `USER_ArbeitsSchritt`** (nicht manuell gepflegt). Modell:
   Werkbank kennt ihr Kürzel · Sub-FA hat eine Kürzel-Menge (Struktur) · **Terminal zeigt die
   Schnittmenge** — an Werkbank `KA` erscheint der `KA`-Arbeitsgang eines gescannten Sub-FA, wenn dessen
   `Arbeitsschritte` `KA` enthält.
3. **2c — Umbau statt Zweitbau:** Die Struktur-Ableitung von v1.41.0
   (`FaWorkStepStructureDetectionService`: DirectChildren-Scope, exakt-Code-Match, melden-statt-anlegen)
   wird **von `FaWorkStep` auf `WorkOperation` umgebaut** — im selben Bündel-Worktree, bevor v1.41.0 je
   nach Produktion geht. v1.41.0 wird dadurch **superseded** (Spec + Changelog entsprechend kennzeichnen).

## Bausteine (Skizze, nicht Spec)

- **(a) `ProductionWorkplace` um das Kürzel erweitern** — heute nur `Name`/`BdeDefaultArbeitsgang`, **kein**
  Arbeitsschritt-Feld. Neues Feld (z. B. `Arbeitsschritt`/`Code`) + Sync aus dem Sage-Arbeitsplatz
  (`USER_ArbeitsSchritt`). Das ist der eigentliche Routing-Baustein.
- **(b) Struktur → `WorkOperation`** je Sub-FA (2c-Umbau): je bekanntem Kürzel eine `WorkOperation`
  (Nur-hinzufügen, melden-statt-anlegen, Fertig-/Storno-Filter wie AKE-Vorbild). Zu klären: `OperationNumber`,
  `Sequence`, `IsReportable`, ob `ProductionWorkplaceId` gesetzt wird oder das Routing rein über die
  Kürzel-Schnittmenge läuft.
- **(c) BDE-Terminal / `BdeScanResolver` (Teil 8)**: heute Filter über `WorkOperation.ProductionWorkplaceId`.
  Neu: Schnittmenge Werkbank-Kürzel × Sub-FA-Kürzel. Zusammenspiel mit `BdeDefaultWorkOperationService`
  (generischer Default) klären — ersetzt der echte AG den Default für IDEAL?

## Token → Werkbank (abgeglichen 2026-09-21 gegen die Sage-Arbeitsplatz-Liste)

17 real gemeldete Kürzel (aus dem ersten Struktur-Lauf, SyncLog `FaWorkStepStructureDetection`).
**12 treffen eine Werkbank** (`USER_ArbeitsSchritt` → `Bezeichnung1`):
KA Kanterei W1 · LS Stanzerei W1 · SW Schweißerei W1 · EG Flächen entgraten · SÄ Schäumerei W1 (EG) ·
VM Elektrofertigung W1 (OG) · PL Punkten Ladenbau W1 · PG Punkten Gehäusebau W1 · BR Berohren W1 ·
EM Elektromontage W1 · ISO Isolierung W2 · SL Schleiferei W1.
**5 ohne Werkbank in der Liste:** MO, ZS, LÖ, PR, BE → heute nicht buchbar (Werkbank fehlt oder Liste
unvollständig) — Melde-/Zuordnungsthema.
Quelle: vollständige Arbeitsplatz-Stammdaten (Spalte `USER_ArbeitsSchritt`), vom Menschen am 2026-09-21
bereitgestellt; Sync-Quelle für Baustein (a).

## Offene Punkte für Schranke 1

- **Varianten-Regel:** Struktur nutzt Basis-Kürzel (`KA`); Sage hat Werkbank-Varianten (KA/KA2/KA4,
  SW/SW1/SW2/SWB/SWL, SÄ/SÄ2/SÄE/SÄO, VM/VM1/VM2, PG/PG2, PL/PL2, EM/EM2, SL/SLH). Nur exakt `KA` oder
  Präfix/Gruppe? (Vermutlich exakt Basis-Kürzel = die scan-relevante Haupt-Werkbank.)
- **5 unbekannte Kürzel** (MO/ZS/LÖ/PR/BE): melden, oder Werkbank in Sage nachtragen, oder aus dem
  Scope ausschließen.
- **`ProductionWorkplace`-Kürzel-Sync:** eigener Sync-Schritt vs. Erweiterung eines bestehenden
  Workplace-Syncs; Quelle Sage-Arbeitsplatz-View/Tabelle (Name + Whitelist wie die FA-Hierarchie-Views).
- **`BdeScanResolver`/`GetAvailableOperations` umbauen** (Kürzel-Schnittmenge) — Auswirkung auf
  Teil-8-Testszenarien (TS-66).
- **Koexistenz `BdeDefaultWorkOperationService`:** bei IDEAL abschalten/ersetzen, sobald echte
  `WorkOperation` vorliegen?
- **Katalog Kürzel → Name:** kommt aus der Werkbank-Bezeichnung (Arbeitsgang ≡ Werkbank) — separater
  WorkStep-Katalog vermutlich nicht nötig.

## Bezug

Ziel hinter [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]] (v1.41.0, wird superseded) ·
Backlog-Vorläufer [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte]] ·
BDE-Terminal [[2026-07-29-standort-ideal-teil-8-spec]] (TS-66) ·
Struktur/Werkbank-Datenhoheit [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]] ·
Bündel [[2026-08-07-ideal-teile-1-5]] · Gesamtpaket [[project_ideal_standort_spec_paket]].
