---
type: changelog
version: 1.47.1
date: 2026-10-05
---
# v1.47.1 — IDEAL-Buendel: AKE-Hotfix 1.30.1 per Vorwaerts-Merge uebernommen

Vorwaerts-Merge `main` → `feature/2026-08-07-ideal-teile-1-5` (Merge-Commit **`6c253c60`** im Buendel-Worktree),
Abschnitt „Uebertrag ins Buendel" von [[2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist-spec]].
Quelle in main: Merge-Commit `f89c0124` ([[2026-09-29-v1-30-1-ake-hotfix-stueckliste-druck-lagerbestellung-ist]]).
Merge-Protokoll mit Konfliktvergleich: [[2026-10-05-vorwaerts-merge-ake-hotfix-ins-buendel]]. **Keine Migration.**

## Was das Buendel jetzt traegt

- **Stuecklisten-Druck per POST** (HTTP 404.15) — eingehaengt **nach** der Buendel-Leerzustand-Sperre
  (`Bom.cshtml`: bei 0 sichtbaren Positionen weiterhin kein Druck-Tab, sondern `#bomKzEmptyState`).
  `RenderPrintBomAsync` traegt den **Buendel**-Rumpf des bisherigen `PrintBom` (Komm.-Ziel/Ebene/BomScopes).
- **Lagerbestellung: Ist-Menge bestaetigen** — Pflichtpruefung (Close + PrintAndClose, serverseitig),
  Autosave speichert leer als NULL, Laengen-/ModelState-Guard, kein Sammel-Dialog. Das Buendel hatte die
  Lagerbestellungs-Dateien seit dem Fork nicht geaendert → keine IDEAL-eigene Lesestelle betroffen
  (Suche nach alten Signaturen: nur 4 Buendel-Tests, Pflicht-Nachzug).
- Einmal-Skript `SQL/Einmalig/2026-09-28_Hotfix-1.30.1_ResetAutosaveZeroQuantityPicked.sql` liegt jetzt auch
  im Buendel — **NICHT ausgefuehrt**; auf IDEAL genau einmal NACH dem Buendel-Deploy
  ([[0015-einmal-datenskripte-ausserhalb-der-nummerierten-sql-reihe]]).

## Nachweis

`dotnet build IdealAkeWms.slnx` gruen (8 vorbestehende Warnungen). `dotnet test`: Web **1449/1450** (1
vorbestehend uebersprungen; vorher 1435/1436, +14 = genau die Hotfix-Tests), Service **268/268** (unveraendert).

## Deploy-Risiken

Keine zusaetzlichen gegenueber dem Buendel — wirkt erst mit dessen eigenem Deploy. Dann: Einmal-Skript
(Backup, ruhiges Fenster, Tabs neu laden, Protokoll) und Anwender-Info zur Pflicht-Ist-Menge.
