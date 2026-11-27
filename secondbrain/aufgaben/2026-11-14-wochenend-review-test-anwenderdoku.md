---
typ: aufgabe
status: erledigt
created: 2026-11-13
auftrag: "Gerald 2026-11-13 19:35"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Wochenend-Lauf: Code-Review, Test auf Herz und Nieren, Anwender-Doku mit Screenshots

**Auftrag Gerald 2026-11-13 19:35:** „Code-Review prüfen und nochmals alles durchtesten auf Herz und Nieren. Besonders die neue Kommissionierung
und die neuen Typen für die Kommissionierung, und danach alles sauber dokumentieren. Auch für Anwender. Starten mit einer Anleitung für die
einzelnen Module inkl. Screenshots und Hinweisen.“

Stand: Bündel v1.99.22 (`90bbbbd7`), DEV AKE (`localhost:8081`) und IDEAL (`localhost:8082`).

## Teil A – Code-Review (lesend)
- Gesamtes Bündel gegen `main` (`git diff main...HEAD`), Schwerpunkt **Kommissionierung**: KO-Aufträge (`PickingOrder`, `PickingOrderLine`,
  Nummern `KO-JJJJ-NNNNN`, v1.88.0 ff.), **Kommissionierbereiche/-typen** (`PickingArea`, `PickingAreaRule`; Lager, Glas, Vorkommissionierung),
  Freigabe/Prio (v1.99.17), Altbestand-Überführung, Wagen, Status Teilweise/Vollständig, Druck, Bedarfsmeldung im KO-Auftrag.
- Danach die übrigen Module des Bündels: Wareneingang Teil 1–3, Sage-WE/CSV, Lieferverzug, Bestellvorschläge, SData Teil 2, Update-Skript, UTC.
- Prüfkriterien: Korrektheit, Transaktionen/Nebenläufigkeit, Audit-Felder, Zugriffsschutz (Rollen, Gates), Spaltenfilter/Pagination-Muster,
  Melden statt still behandeln, Sage-Schreibwege (nur hinter Schaltern), Tests vorhanden.
- **Befunde nicht im Lauf beheben** (Schranke 1): je Befund Datei:Zeile, Schwere, Vorschlag → `secondbrain/backlog/2026-11-14-review-befunde-<modul>.md`.
  Ausnahme: reine Tippfehler in Texten/Doku.

## Teil B – Test auf Herz und Nieren (DEV, nur DEV)
- Alle TS-Kapitel des Bündels auf DEV AKE + IDEAL durchspielen, Kommissionierung zuerst und vollständig (alle Bereiche/Typen, Sammelfreigabe,
  Rücknahme, Teilweise, Folgeauftrag, Umbuchen auf Wagen, Druck, Fehlteile, Bedarfsmeldung, Leitstand, Terminal-Sicht).
- Negativfälle und Grenzfälle (leere Bereiche, kein Standardbereich, kein Kommissionierer, gleichzeitige Freigabe, Storno).
- Erlaubt: Testdaten auf DEV anlegen/buchen/freigeben (Spur führen); **verboten:** an Sage schreiben, Mails an externe Adressen, Test-/Prod-Systeme.
- Ergebnis: `secondbrain/tests/2026-11-14-wochenend-regression-dev/` (Bericht je Modul, Tabelle OK/FEHLER/NICHT PRUEFBAR, Screenshots der FEHLER).

## Teil C – Dokumentation
1. **Anwender-Anleitungen je Modul** (Start, Priorität), Deutsch, für Lager/Produktion/Einkauf/Leitstand – nicht für IT:
   Zweck, Wer darf (Rolle), Schritt-für-Schritt mit **Screenshots** (DEV, Testdaten, keine echten Personendaten), Hinweise/Stolperfallen, Häufige
   Fragen. Reihenfolge: Kommissionierung (Leitstand → Freigabe → KO-Auftrag → Wagen → Abschluss/Teilweise → Druck), Kommissionierbereiche
   (Stammdaten), Lagerbestellung/Glasbestellung, Bedarfsmeldung, Wareneingang zu Bestellung (Barcode, Überlieferung, Hinweise), Offene Bestellungen
   und Rahmenabrufe, Lieferverzug-Mail (für den Einkauf), Bestellvorschläge, Einbuchen/Ausbuchen/Umbuchen, Bestände/Spaltenfilter-Syntax,
   BDE-Terminal, Register Sage-Test (Admin).
   Ablage im Worktree `docs/anwender/<NN>-<modul>.md` + `docs/anwender/bilder/`, Index `docs/anwender/README.md`; zusätzlich je Modul ein Verweis
   in der Hilfeseite nur, wenn ohne Code-Änderung möglich (sonst als Vorschlag melden).
2. **Betriebs-/IT-Doku** nachziehen: `README.md` (AppSettings/ServiceSettings neu seit v1.99), Deploy-Hinweise.
3. **Brain:** feature-map, glossar (neue Begriffe), codebase-Karte, testszenarien-index; Lauf-Notiz in diesem Ordner.

## Grenzen
Nichts an Sage schreiben; keine Mails an externe Adressen; keine Einstellungen außerhalb DEV; kein Anwendungscode-Fix ohne Spec
(Ausnahme Tippfehler); kein Merge, kein Push.


Erledigt im /goal-Lauf [[2026-11-14-wochenend-goal-lauf]] (Abschluss 2026-11-14).
