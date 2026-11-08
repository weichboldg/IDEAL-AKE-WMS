---
typ: notiz
status: Erledigt
worktree: .claude/worktrees/2026-08-07-ideal-teile-1-5
branch: feature/2026-08-07-ideal-teile-1-5
buendel: "[[2026-11-08-buendel-uebersicht]]"
---
# /goal-Lauf 2026-11-08 (3): Specs + Nachbesserungen vor dem Bündel-Merge

Auftrag Gerald 2026-11-08. Phase 1 Specs (Ausnahme Schranke 1 für a–c), Phase 2 Umsetzung im Bündel-Worktree.
Die drei Nachbesserungs-Specs (C2, A5, Wareneingang) stehen auf Testbereit; **der Auftrag nennt sie ausdrücklich und ersetzt damit die
Statusprüfung „Freigegeben“** (goal.md Schritt 1). Basis: Bündel v1.88.0, letzte SQL 140, letztes TS-120.

## Phase 1 – Specs

| # | Backlog | Spec | Status | Hinweis |
|---|---|---|---|---|
| a | [[2026-11-08-oseon-relevanzfilter-schaltet-sich-ab]] | [[2026-11-08-oseon-relevanzfilter-schaltet-sich-ab-spec]] | **Freigegeben** (Auftrag), `be0244c8` | v1.92.0, TS-121; Umfang um BDE-Edit IsFinal/Delete erweitert (gleicher Fehler) |
| b | [[2026-11-08-meldebestand-mail-bestand-falsch-berechnet]] | [[2026-11-08-meldebestand-mail-bestand-falsch-berechnet-spec]] | **Freigegeben** (Auftrag), `be0244c8` | **nachgestellt** auf AKE-DEV (lesend): Umbuchung regulär→regulär zählt +Qty, 9 Artikel mit Meldebestand ohne Bewegung fehlen. v1.93.0, TS-122 |
| c | [[2026-11-08-seiten-querscrollen-navigationsleiste]] | [[2026-11-08-seiten-querscrollen-navigationsleiste-spec]] | **Freigegeben** (Auftrag), `be0244c8` | v1.94.0, TS-123 |
| d | Nachtrag [[2026-11-08-bereinigungen-nach-buendel-2026-11-07-spec]] | – | **erledigt** `6987e2bd` | SQL/108 löscht `StammdatenADGruppe`; DEV AKE 2×, IDEAL 1× |
| e | [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe]] | [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]] | **Entwurf** `be0244c8` (10 Fragen, E4 blockiert; SQL-Vorschlag 142) | **P-W7 belegt: Kennzeichen `ELL`** (nicht `EWE`), Sage-SQL lesend AKE-Test; Schreibtest NICHT ausgeführt |

## Phase 2 – Umsetzung

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]] Q9–Q11 | Nachbesserung | 141 | 1.89.0 | **Testbereit** | `bc8be58c`, `53858c6c` | |
| [[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]] Q-1/Q-2 | Nachbesserung | – | 1.90.0 | **Testbereit** | `5d4df4be` | |
| [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] N-1 | Nachbesserung | – | 1.91.0 | **Testbereit** | `bacf3b82` | |
| a OSEON-Relevanzfilter | Bug | – | 1.92.0 | **Testbereit** (Reparatur 1 nach QA-Befund B-1) | `cf869e97`, `9c9046c4` | |
| b Meldebestand-Mail | Bug | – | 1.93.0 | **Testbereit** | `897f7a03` | |
| c Querscrollen/Kontrast | Bug | – | 1.94.0 | **Testbereit** | `2efdb023` | |

## Plan C2 v1.89.0

- **Q9:** `PickingOrderListItem.ZeilenStatusText`: Umgebucht und Gepickt → „gepickt“; `Bom.cshtml` nutzt dieselbe Funktion (eine Stelle).
  „Übertragen“ in Geralds Text = die heutige Anzeige nach dem Kommissionieren („umgebucht“); `Uebertragen` (Rest im Folgeauftrag) bleibt
  „in Folgeauftrag“.
- **Q10:** `PickingAreasController`: zweiten Standard ablehnen („Es gibt bereits einen Standardbereich („X“). Zuerst dort abwählen.“);
  Abwählen erlaubt (Warnung: ohne Standard meldet die Erzeugung einen Fehler, `PickingOrderWorkflow.cs:80`). Ein Speichern = eine
  Transaktion; die Zwei-Schritt-Logik aus Q2/Q13 entfällt, weil es nie zwei Standards gibt. Hilfetexte Edit/Index/Hilfe.
- **Q11:** Kennzeichen `PickingOrders.AltbestandPruefen` (SQL/141 + Migration + FreshInstall). Einmal-Skript setzt es für offene FAs mit
  Menge ≠ 1 und gibt die Liste aus (Trockenlauf und Ausführung). `ErzeugeOffeneAsync` (einziger Erzeugungsweg: Freigabe, Nachziehen,
  Öffnen ohne Leitstand) überspringt solche FAs mit Meldung. Leitstand-Knopf „Altbestand geprüft“ je FA löscht das Kennzeichen (Audit),
  danach erzeugt „Fehlende Kommissionieraufträge erzeugen“ wie gewohnt.

## Zwischenstand 2026-11-08

- Hinweis Meldebestand: `Notifications:MeldebestandEnabled` ist laut Code Default **an** (`ServiceSettingDefinitions.cs:230`), nicht aus;
  unverändert gelassen.
- DEV AKE war nach dem OSEON-QA-Lauf auf `main`-Stand und gestoppt (Schema-Konflikt); zwei liegengebliebene Kestrel-Prozesse auf Geralds
  Freigabe beendet, Bündel v1.94.0 neu deployt. Liegen geblieben: DB `AKE_WMS_QA_MAIN`, Ordner `D:\Dev\QA\` (Ersatzlauf OSEON-QA) – löschen
  nur nach Freigabe.

## Abschluss 2026-11-08

Alle sechs Specs Testbereit (QA 2026-11-08, Nach-QA v1.92.0). SQL/141 genutzt (nächste freie: 142, Vorschlag in Wareneingang Teil 2).
Bündel jetzt **v1.94.0**, letztes TS-123. Für Gerald offen: Fragen der Spec Wareneingang Teil 2 (Schranke 1, Schreibtest),
Deploy-Hinweis Meldebestand (IDEAL-DEV 185 Artikel mit Meldebestand ohne Bewegung, AKE 9), manuelle Checklisten am Ende jeder Spec.
