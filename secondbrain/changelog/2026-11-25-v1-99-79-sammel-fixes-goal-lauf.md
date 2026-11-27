---
type: changelog
version: 1.99.79
date: 2026-11-25
---
# v1.99.79 — Sammel-Fixes Schlusslauf goal-lauf 2026-11-25

Lauf-Notiz [[2026-11-25-kommission-lagerplatz-bde-goal-lauf]] (Tabelle „Sammel-Fixes“, Spalte „Schlusslauf v1.99.79“). Specs
[[2026-11-25-kommissionierauftraege-nachtrag-nachts-spec]], [[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]],
[[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]], [[2026-11-25-bde-masken-vereinheitlichung-spec]].
Commits `f1f0e4f2` + `a4feb6af` (Worktree `feature/2026-08-07-ideal-teile-1-5`). **Keine Migration**, kein neues SQL. TS-200.
Nicht gemergt; qa-agent steht aus.

## Kernpunkte

- Kommissionierung: `PickingController` ohne stillen `?? new List` (F-1); Leitstand „Altbestand geprüft“ fängt Fehler beim Anlegen →
  WarningMessage „Nachtlauf holt nach“ (F-2).
- Lagerplatz-Anlage Sage: Dienst-Stopp bleibt „Offen“ (`when (!ct.IsCancellationRequested …)`, F-5); 50014 nennt belegende PlatzID per
  `STRING_AGG` (F-6, read-only belegt auf ake_TEST2026: `HAUPT;0;0;0` trifft PlatzID 1 `Hauptlager;0;0;0`); Lagerkennung ohne `maxlength`,
  Meldung am Feld (F-7); Reader verbindet Plätze über Mandant + Lagerkennung (F-8).
- Fräsen-Pfad/Blechteile: Blechdicke ohne Einstellungszeile → Wert geleert + Hinweis (F-10, Spec AK 9); Bestandsspalte fixiert (F-12);
  Test Werkbank-Regel (F-13); Edit-Fehlerfall behält Fräsen-Pfad-Karte (F-17); Einmal-Skript bricht ohne `ProduktionsauftragHierarchisch` ab (F-18);
  Lagerbestellung Edit ohne `innerHTML` für Sage-Texte (F-19).
- BDE: Laufzeit-Ticker in „Meine Buchungen“ (F-26) + Sub-FA (F-35); neue Scan-Art `Running` → „läuft bereits“ (F-27); JSON-Aktionen prüfen
  `terminalId` gegen das Terminal des angemeldeten Benutzers (F-28); Anmeldung aus der Tageshistorie kehrt zurück (`ziel=historie`, F-31);
  0 statt FA/Sub-FA ausgeblendet (F-34); Kontrast `#0a58ca` 5,91:1 (F-33); Summen-Haken folgt Einzel-Haken (F-36); Fokus „OK“ (F-37).
- Doku: `docs/betrieb/03` 5.2a Zeile SQL/168 (F-24), TESTSZENARIEN-Fußzeile (F-15), Kapitel 11/17/19, Stand 1.99.79.

## Widerlegt / offen

- Widerlegt: F-25 (Spec Antwort 4 verlangt kein Gate), F-39 (Migration `20260910081155_AddProductionOrderMatchcode` in `47de3ba2` entfernt,
  nie deployt – die Zeile gehört nicht in FreshInstall).
- Bestätigt: F-9, F-32. Offen Gerald: F-3, F-4, F-11, F-14, F-16, F-20, F-21, F-22, F-29, F-30, F-38.

## Deploy-Risiken

- Keine Migration. JS/CSS: Browser-Cache (asp-append-version). F-28: Buchungen von einem Benutzer **ohne** Terminal-Zuordnung werden jetzt
  abgewiesen (die BDE-Seite verlangt das Terminal ohnehin).
- Tests: Web 3698/1/0 (Basis 3689), Dienst 848/0/0 (Basis 847); Build 0 Fehler, 32 Warnungen (Basis).
