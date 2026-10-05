---
type: changelog
version: 1.41.0
date: 2026-09-16
---
# v1.41.0 — IDEAL: FA-Arbeitsgänge aus der Struktur (FaHierarchyNode.Arbeitsschritte)

Umsetzung der freigegebenen Spec [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]] im
**Bündel-**Worktree `feature/2026-08-07-ideal-teile-1-5` (kein neuer Worktree, Spec „Reihenfolge").
Umsetzungsnotiz [[2026-09-16-arbeitsgaenge-aus-arbeitsschritte-umsetzung]]. **Keine Migration.**

**Warum:** Seit dem Klasse-D-Gate (v1.36.0, [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]])
läuft im hierarchischen Modus **gar keine** Arbeitsgang-Erkennung mehr — IDEAL-Aufträge bekamen keine
`FaWorkSteps`. Damit war Teil 8 (Sub-FA-BDE) am IDEAL-System nicht abnehmbar (UAT-Befund T7: kein OSEON →
keine WorkOperations). Diese Spec schließt die Lücke: **dritte** Struktur-Ableitung der Familie (Werkbank
aus `Arbeitsbereich`, `HasCoatingParts` aus `Beschichtet`, jetzt Arbeitsgänge aus `Arbeitsschritte`).

**AKE liefert AKE nicht mit — IDEAL schon.** AKE muss die Arbeitsgänge aus Bezeichnungen **raten**
(`FaWorkStepDetectionService`, `SearchString.Contains`). IDEAL bekommt die Arbeitsschritte je Position
**geliefert** (`FaHierarchyNode.Arbeitsschritte`, leerzeichen-getrennte Kürzel). Das Token **ist** der
Code — kein Suchen, kein Raten.

## Umgesetzt

- **Neuer Service `FaWorkStepStructureDetectionService`** (`IDEALAKEWMSService/Services/`,
  `IFaWorkStepStructureDetectionService`) — bewusst **eigener** Service statt parametrisierter
  Bestandsservice (Spec Design A/B, fünf gemessene Unterschiede zur AKE-Heuristik; kein Eingriff in den
  live laufenden AKE-Pfad). **Kein interner Master-Gate** — Vorbild `FaMaterializationSyncService`.
- **Ableitung (DetectAsync):** je materialisiertem Sub-FA (`SubFA != 0`) Token aus dem
  **DirectChildren-Scope** (eigene Zeile + direkte Kinder `VaterFA = SubFA`), leerzeichen-gesplittet,
  exakt gegen `WorkStep.Code` (case-insensitiv/getrimmt, **nicht** `SearchString`). Nur-hinzufügen wie
  AKE-Vorbild (aktive **und** `IsRemoved`-Zeilen sperren das Re-Add), Fertig-/Storno-Filter identisch.
  Zuordnung Sub-FA → `ProductionOrder` über `SubOrderNumber == SubFA.ToString()` (wie
  `FaMaterializationSyncService`, keine neue Lookup-Bahn).
- **Melden statt anlegen** (ADR 0014): unbekannte Token werden nie automatisch angelegt. Jeder Lauf loggt
  die Sammelmeldung (Token-Liste + Anzahl betroffener Sub-FA-Positionen); Sammelmail nur bei **Änderung
  der Token-Menge** (S1, neue Singleton `IUnknownWorkStepTokenState` — eigene Instanz, nicht die
  Werkbank-Singleton).
- **Verdrahtung:** `FaWorkStepSources.Struktur`; Service-Key `Sync:FaWorkStepStructureDetectionEnabled`
  (Default false, Kategorie FA-Hierarchie); `SyncLogServices.FaWorkStepStructureDetection` (+ `.All`);
  `SyncWorker`-Block **nach** FA-Materialisierung mit **Doppel-Gate** (`ProduktionsauftragHierarchisch` +
  Toggle, beide `GetBoolSafeAsync`, `RunResilientAsync`); zwei DI-Registrierungen in `Program.cs`.
- **14 Unit-Tests** (`FaWorkStepStructureDetectionServiceTests`, InMemory): Grundfall, Audit,
  DirectChildren-Gegenprobe (Enkel-Token beim Elternteil nicht beim Großvater), Nur-hinzufügen
  (`IsRemoved`/aktiv), Code-exakt-nicht-`SearchString`, unbekannt-gemeldet-nicht-angelegt, `IsDone`/
  `IsDonePicking`, fehlende Materialisierung, DryRun, `SubFA==0`, S1-Zustand.

## Bewusst

- **Doppel-Kontext (Design E, [[fallstricke]] §13):** Ist ein direktes Kind selbst ein Sub-FA, trägt
  dessen `Arbeitsschritte` in **zwei** Scopes bei (Eltern-DirectChildren **und** eigene Zeile). Kein
  Fehler — mechanische Folge der DirectChildren-Regel; am ersten echten Datenlauf mit dem Fachbereich
  gegenprüfen.
- **Kein Standort-Filter im Matcher (H4):** das Lexikon enthält AKE- **und** IDEAL-Codes; die 13 heutigen
  IDEAL-Token gleichen keinem AKE-Code (`AV ≠ VA`) → heute unkritisch, bei künftiger Code-Vergabe im Blick.

## Deploy (Zwei-Lauf, S4-harmonisiert)

- **Web: ja · Service: ja · Migration: nein** (nur neuer Katalog-/Laufzeit-Key, kein Schema).
- **Deploy zuerst**, dann Toggle mit globalem DryRun testen. Der erste Lauf **meldet** die real
  vorkommenden Kürzel (Sammelmeldung); ein Mensch legt je Kürzel einen `WorkStep` mit `Code = Token` +
  Klartext-Name auf `/WorkSteps` an (Umlaut zeichengleich, `SÄ` ≠ `SAE`); der nächste Lauf legt die
  `FaWorkStep`-Zeilen an. **Keine Vorab-Erhebung nötig** — der entworfene Weg, kein Notbehelf.
- **Betrieb (H5):** `IUnknownWorkStepTokenState` ist In-Memory → nach Dienst-Neustart im Fenster vor der
  Katalogpflege geht die Sammelmail einmal erneut raus. Bekannt, akzeptiert.
- **Merge-Commit:** offen (Schranke 2, Mensch).

## Nachweis

Build 0 Fehler; **Web 1391 grün +1 skip, Service 277 grün** (+14 neu). Testszenarien **TS-76.1–76.10**
([[testszenarien-index]] → `docs/TESTSZENARIEN.md`). Genaue Zahlen im Spec-QA-Abschnitt (qa-agent).

## Zugehörig

Spec [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte-spec]] ·
Umsetzung [[2026-09-16-arbeitsgaenge-aus-arbeitsschritte-umsetzung]] · [[fallstricke]] §13 ·
Voraussetzung [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]] (Klasse-D-Gate) +
[[2026-08-20-materialisierung-fachliche-felder-spec]] (materialisierte ProductionOrders) ·
Bündel [[2026-08-07-ideal-teile-1-5]].
