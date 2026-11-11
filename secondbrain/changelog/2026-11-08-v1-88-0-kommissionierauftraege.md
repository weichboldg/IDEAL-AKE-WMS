---
type: changelog
version: 1.88.0
date: 2026-11-08
---
# v1.88.0 — Kommissionieraufträge (Kopf/Zeilen) mit Kommissionierbereichen (Epic C2)

Spec [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]], Lauf [[2026-11-08-buendel-2026-11-08-goal-lauf-2]], ADR
[[0021-kommissionierauftrag-als-beleg-neben-der-stueckliste]]. Commits `3500c2fe` (E1), `1f089b3a` (E2), `e0666d98`/`4dc165cc` (E3), `79bb524c` (E4),
`ff3015c6` (E5), `15a8e45f` (E6), QA-Reparatur `1ed5c72c`. **Migration:** `SQL/133_AddPickingAreasAndOrders.sql` (additiv, Seed). 134 nicht gebraucht.
**Einmal-Skript:** `SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql` (daten-umbauend). TS-120. E7 (Sage) Blocker laut Antwort 12.

## Kernpunkte

- Kommissioniert wird je Auftrag `KO-JJJJ-NNNNN` (je FA und Bereich), erzeugt bei der Leitstand-Freigabe bzw. „Fehlende Kommissionieraufträge erzeugen“.
- Auftragsseite mit Ist-Menge, Umbuchung je Zeile (FA-Feld aus v1.87.0, nie Sage), Abschluss vollständig/teilweise mit Folgeauftrag, FA erledigt,
  wenn kein Auftrag mehr offen ist.
- Stammdaten Kommissionierbereiche + Regeln; Leitstand zeigt Aufträge, Rücknahme storniert Aufträge ohne Fortschritt; Druck mit Code-128-Barcode.

## Deploy-Risiken

- DB-Backup → Web stoppen → SQL/133 → Einmal-Skript Trockenlauf, Zählung von Gerald freigeben → ausführen → Web v1.88.0 → Leitstand
  „Fehlende … für alle freigegebenen FAs erzeugen“. Wartungsfenster: zwischen Skript und Nachziehen niemand kommissionieren.
- Q11: Nach dem Altbestand legt das Nachziehen für offene FAs mit Auftragsmenge ≠ 1 einen Restauftrag an (Befund B1) – vorher entscheiden.
- OSEON-Hotfix Z3 (`066b136f`) nur in `main`: beim Sync/Merge Konflikt in `Views/Picking/Index.cshtml`/`PickingController` – die Auftragsliste gewinnt.
