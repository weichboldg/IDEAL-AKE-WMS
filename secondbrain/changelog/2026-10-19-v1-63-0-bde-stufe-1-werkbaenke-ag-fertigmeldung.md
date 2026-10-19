---
type: changelog
version: 1.63.0
date: 2026-10-19
---
# v1.63.0 — BDE Stufe 1: Arbeitsgang an mehreren Werkbänken, veraltete Sage-Werkbänke, nur AG-Fertigmeldung

[[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec]], Umsetzung
[[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-umsetzung]]. **Direkt im Bündel**, Start `d75680f4` (v1.62.0),
Commits `4416aac8`, `13681983`, `27fdec11`, `8b51af5a`, `3efba9a9`, `501008f6`, QA-Fix `b3daf566`. Quellen
[[2026-10-19-bde-lastenheft-abgleich]] (Stufe 1 a–c) und [[2026-10-19-werkbank-katalog-veraltete-sage-arbeitsplaetze]] (Bug).
**Migration `20261019092105_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung` / `SQL/97`** (additiv).

## Kernpunkte

- **(b) Bug-Fix IDEAL:** Werkbank-Anlage kennzeichnet Sage-Werkbänke, deren Nummer nicht mehr in der Quelle steht
  (`ProductionWorkplace.SageMissingSince`, nie löschen, Empty-Guard, selbstheilend, Counts `nicht_in_sage`,
  `nicht_in_sage_neu`, `wieder_in_sage`). Gekennzeichnete zählen nicht für Struktur-Erkennung und FA-Baum-Namen → EG/KA
  für Sub-FA 1065150 werden wieder angelegt. Spalte „Nicht in Sage seit“ in Stammdaten › Werkbänke.
- **(a) Variante B:** Kürzel-Familie (`WerkbankKuerzelFamilie`) in Terminal, Resolve-Scan, FA-Baum-Gate; Struktur-Erkennung
  legt bei mehreren Werkbänken an der bevorzugten an (Count `arbeitsschritt_mehrere_werkbaenke`, `arbeitsschritt_mehrdeutig`
  entfällt). Kürzel an Werkbänken ohne Sage-Nr. im WMS pflegbar (FA-a8, serverseitig am DB-Wert).
- **FA-a9 Sicherheit:** Start/Fortsetzen/Fertigmeldung nehmen nur AGs der Familie an (NurFA: FA-Werkbank, Antwort 12).
- **(c)** Werkbank-Option `BdeNurAgFertigmeldung`: Terminal nur „Fertig melden“, Buchung Production/Finished mit
  StartedAt == EndedAt + IsFinal-Menge; Start-Sperre Rüsten/Produktion; NurFA gewinnt.
- QA-Fix Q1: `StartProductionForOrder` prüft vor dem Anlegen des Default-AG „01“ (sonst öffnete ein manipulierter POST
  die FA-a9-Lücke über diesen Endpunkt wieder). QA: Build + 2088 Web- / 316 Service-Tests grün, **Testbereit**.
- [[fallstricke]] §28 neu.

## Deploy-Risiken

Web + Dienst + `SQL/97`. Vor dem Scharfschalten bei IDEAL: P1–P3/P6 + DryRun (vermutlich viele Werkbänke gekennzeichnet,
deren Kürzel dann als „unbekannt“ gemeldet werden, inkl. Sammelmail). **Verhaltensänderung an beiden Standorten:**
Fortsetzen einer an einer fremden Werkbank pausierten Buchung wird abgewiesen — Werker vorher informieren.
