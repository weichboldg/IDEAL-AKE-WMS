---
type: backlog
typ: bug
status: in-spec
created: 2026-11-26
quelle: "Gerald im Chat 2026-11-26: alle Befunde als Backlog, Spec nach der Schluss-QA von [[2026-11-26-buendel-1-lagerbestellung-goal-lauf]]"
standort: AKE, IDEAL
---
# Kommissionierauftrag: Filterformular und „Zurücksetzen“ verlassen die Auftragsseite

Zusatzfund bei Etappe A von [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-3-spec]] (v1.99.89), vorbestehend.

- In `Views/Picking/Bom.cshtml` gehen im Auftrag-Modus Filterformular und „Zurücksetzen“ immer nach `Picking/Bom/{ProductionOrderId}` – der
  Anwender landet in der Stückliste des FA statt auf seinem Kommissionierauftrag.
- v1.99.89 behebt das nur, wenn der Umschalter Stückliste/Liste sichtbar ist (IDEAL mit Kommissionierlisten). AKE bzw. Kommissionierlisten aus: alt.
- Erwartet: im Auftrag-Modus bleibt Filtern immer auf `Picking/Auftrag/{id}`.
