---
type: aufgabe
status: offen
created: 2026-11-13
auftrag: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
---
# /goal-Lauf Wochenende: Code-Review, Regression DEV, Anwender-Doku

Auftrag [[2026-11-14-wochenend-review-test-anwenderdoku]] (Gerald 2026-11-13 19:35, /goal). Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`,
Branch `feature/2026-08-07-ideal-teile-1-5`, Stand v1.99.22 `90bbbbd7`. Reihenfolge A → B → C, Kommissionierung und Kommissionierbereiche jeweils
zuerst und am gründlichsten. Grenzen: nichts an Sage schreiben, keine Mails an externe Adressen, keine Einstellungen außerhalb DEV, kein
Anwendungscode-Fix ohne Spec (außer Tippfehler), kein Merge, kein Push.

| Teil | Inhalt | Status | Ergebnis |
|---|---|---|---|
| A1 | Review Kommissionierung + Kommissionierbereiche | **erledigt** | 20 Befunde – Kommissionierung K-1–K-15 (1 hoch K-1 doppelter Folgeauftrag, 5 mittel, 6 niedrig, 3 Hinweis), Bereiche B-1–B-5 (1 mittel B-1 Standardwechsel ohne Transaktion, 2 niedrig, 2 Hinweis): [[2026-11-14-review-befunde-kommissionierung]], [[2026-11-14-review-befunde-kommissionierbereiche]]. Tippfehler behoben Worktree `433da6c4` (Sage-Stückliste, Kommissionierer ändern) |
| A2 | Review Wareneingang 1–3, Sage-WE/CSV, Lieferverzug, Bestellvorschläge, Offene Bestellungen | **erledigt** | 11 Befunde (1 hoch WE-1, 2 mittel WE-2/LV-1, 5 niedrig, 3 Hinweis): [[2026-11-14-review-befunde-wareneingang]], [[2026-11-14-review-befunde-lieferverzug]], [[2026-11-14-review-befunde-offene-bestellungen]], [[2026-11-14-review-befunde-bestellvorschlaege]] |
| A3 | Review SData Teil 2, Update-Skript, UTC/Lokalzeit, übrige Bündel-Module | **erledigt** | 14 Befunde (0 hoch, 4 mittel SD-1 Kennwort an freie Adresse im Verbindungstest, SD-5 Klartext-Kennwort im Install-Skript, UP-1 Neustart-Update verliert Daten-Einträge, BDE-1 doppelte Fertigmeldung; 5 niedrig, 5 Hinweis): [[2026-11-14-review-befunde-sdata]], [[2026-11-14-review-befunde-update-skript]], [[2026-11-14-review-befunde-lokalzeit]], [[2026-11-14-review-befunde-bde]], [[2026-11-14-review-befunde-einstellungen]]. Gezielter `dotnet test` 261/0. „Fertigungmenge“ (UI, = DB-Spaltenname) nicht geändert → Frage an Gerald |
| B | Regression DEV AKE + IDEAL, Bericht `secondbrain/tests/2026-11-14-wochenend-regression-dev/` | B1+B2 erledigt, B3 läuft: IDEAL-Hierarchie/Beschichtung/BDE, B4 übrige Module folgen | B1 [[01-kommissionierung]]: 41 OK / 5 FEHLER / 11 NICHT PRUEFBAR; neu N-1 (hoch, unsichtbare Unterpositionen), N-2 (Deadlock-500); K-2/K-3/K-4/K-6/B-1 reproduziert, K-1 durch N-2 verdeckt. B2 [[02-wareneingang-einkauf]]: 79 OK / 1 FEHLER (WE-1 reproduziert) / 19 NICHT PRUEFBAR; WE-3 reproduziert; neu N-7–N-10 (Glasbestellung als „Lagerbestellung“, Einheit fehlt, Querscrollen, IDEAL-DEV Wareneingang an ohne Abgleich); Lieferverzug-Lauf nur Pickup (91 .eml) |
| C | Anwender-Doku `docs/anwender/`, README, Brain | offen | |

**Teil A Summe:** 45 Befunde (2 hoch: K-1 doppelter Folgeauftrag bei „Teilweise“, WE-1 doppeltes Absenden „Einbuchen“ bucht doppelt), alle als
Backlog-Notizen `2026-11-14-review-befunde-*`, nichts behoben (Schranke 1). Gemeinsames Muster: fehlende Sperre gegen Doppel-Absenden
(K-1, WE-1, BDE-1, WE-4) – Kandidat für eine Querschnitts-Spec.
