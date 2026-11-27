---
type: aufgabe
status: offen
created: 2026-11-25
lauf: "[[2026-11-25-goal-lauf-beschichtung-sage-buchungen]]"
specs:
  - "[[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]]"
  - "[[2026-11-23-sage-buchungen-gesamtsicht-spec]]"
  - "[[2026-11-24-sage-buchung-referenz-benutzer-abteilung-spec]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Folgearbeit goal-Lauf 2026-11-25 (Sage-Buchungen, Beschichtungsauftrag, v1.99.65–v1.99.69)

Offene Punkte nach dem Lauf [[2026-11-25-goal-lauf-beschichtung-sage-buchungen]]. Alle fünf Versionen sind Testbereit, nicht gemergt.

## Mensch / Entscheid

- [ ] **P-2 Sage-Testsystem** ([[2026-11-24-sage-buchung-referenz-benutzer-abteilung-spec]], QA R-1): Ausbuchung mit Abteilung senden, dann
  `SELECT TOP 5 BuchungsID, Memo, Referenz FROM dbo.KHKLagerplatzbuchungen WHERE Memo LIKE '%IdealAkeWms Lagerbuchung%' ORDER BY BuchungsID DESC;`
  – steht in `Referenz` `SM#…# <Abteilung> <Benutzer>`, oder füllt Sage sie weiter aus dem Kopf-Memo? Ist das Zeilen-Memo `WMS: …` sichtbar
  (Lagerbuchungsprotokoll)? Ebenso Sage-WE-Kopf-Memo `WMS: WE#…# <Benutzer>` und Wiedererkennung mit altem und neuem Memo.
- [ ] **B3-H1** ([[2026-11-23-sage-buchungen-gesamtsicht-spec]]): Einkauf-Menüeintrag „Sage-Wareneingänge (offen)“ bleibt bei Modul
  „Wareneingang zu Bestellung“ aus sichtbar und führt seit v1.99.69 in eine leere Liste. Ausblenden? – **Gerald entscheidet.**
- [ ] **Manual-UAT / Schranke 2** für v1.99.65–v1.99.69 mit dem Paket `publish\` v1.99.69 (Web + Dienst, SQL/167 über das Update-Skript) an AKE und
  IDEAL; Checklisten in den drei Specs (u. a. 162.30 an AKE, 189.10 als Einkauf ohne Lager-Recht, 190.3 ohne FA-Filter mit 3 Artikeln,
  190.10 manipulierter POST). Danach Merge.

## Code (eigene Spec/Bugfix nötig)

- [ ] `StockMovementsController.OutboundAllConfirm`: Parameter `Dictionary<int,string>? stueckGezeigt` ohne `[Bind(Prefix = "stueckGezeigt")]`
  (gleiches Muster wie N-1, [[fallstricke]] §64). Heute folgenlos, weil `ModelState` dort nicht geprüft wird; vor jeder ModelState-Prüfung nachziehen.

## Doku

- [ ] Fotos fehlen in `docs/anwender/`: Beschichtungsauftrag Übergeben-Fenster (`15-beschichtung-beschichtungsauftrag.md`) und Auswahl „Abteilung“
  bei Ausbuchung / Lagerplatz ausbuchen (`09-einbuchen-ausbuchen-umbuchen.md`). Per Playwright gegen DEV, nichts buchen.

## DEV-Testdaten aufräumen / beachten

- [ ] AKE_WMS_DEV: Bestand **−2** auf QA173-L für 00193009 und 00193050 (QA-Bewegungen 19600–19602, Abteilung C1/NULL/B2).
- [ ] IDEAL DEV: BA-8 „Start Beschichtung“ versehentlich auf **12.07.2026** gesetzt (QA v1.99.67). BA-2026-00011 steht nach der QA v1.99.69 auf Übergeben mit 1 Position.
