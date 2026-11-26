---
typ: feature
status: offen
created: 2026-11-26
herkunft: "Besprechung 2026-11-26, Entscheid 8 (U-1: A umgesetzt in v1.99.81, B als Backlog)"
standort: beide
---
# BDE: geführter Arbeitsgang-Wechsel (U-1 Variante B)

Bezug: [[2026-11-26-loesungsvorschlaege-bde-ui]] (U-1), [[2026-11-26-besprechung-0830-kritische-pruefung]] (Block 3, Entscheid 8), [[2026-11-26-fix-lauf-v1-99-81]].

- v1.99.81 (A): Startet ein Werker an einer zweiten Zeile, während seine Produktion läuft, erklärt die Meldung den Grund und markiert die laufende
  Buchung im Kopf. Es wird nichts gebucht.
- Offen (B): statt nur zu melden einen geführten Wechsel anbieten (laufende Buchung pausieren oder fertig melden, dann den neuen Arbeitsgang
  starten) – in einem Dialog, mit denselben Wächtern.
- Vor der Spec klären: Ist ein Wechsel ohne Mengenmeldung fachlich erwünscht (Pause) oder nur mit Teilfertig?
