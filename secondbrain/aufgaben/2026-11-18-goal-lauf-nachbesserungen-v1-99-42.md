---
type: aufgabe
status: erledigt
created: 2026-11-18
---
# /goal-Lauf 2026-11-18 abends: Nachbesserungen v1.99.42 (BF-CSV, Kürzel, Beschichtung, Userfelder, AG-Anlage N1)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Start bei 9b6b3e0d (v1.99.41). Nummern: v1.99.42, SQL/162 ff., TS-163.
Specs: [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]] (Befund P-1 (2), Walter 15:28), [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]]
(Befund 15:05, Backlog 15:14), [[2026-11-18-sage-userfelder-als-einstellung-spec]] (Befund 13:25), [[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]] (N1).
Auftrag: Eingabe bricht nach „andere Pakete und Dateien in publish\ NICHT“ ab → gelesen als „NICHT anfassen“; Grenzen wie im Lauf davor
(nichts an Sage, keine Einstellungen auf Test/Prod, `SageFaArbeitsgaengeAnlegenAktiv` aus, kein Merge, kein Push). Paket 7 nach `publish\paket7-v1.99.42\`.

## Plan (kompakt)
- P2 zuerst (P1 braucht das Kürzel): `Users.Kuerzel` (≤ 10, gefiltert UNIQUE, Großbuchstaben) + `BfAuftraege.BestellerKuerzel` (Snapshot bei Freigabe), SQL/162.
- P1: `BfAuftragPositionen.Arbeitsschritte` (Snapshot beim Hinzufügen aus dem Merkmal `USER_OSArbeitsschritte`), SQL/163; `BfCsv.Inhalt` exakt nach BF00000002 (13 Felder, Z10/Z12 leer, Z13 deutsch), Lagerplatz `;`→`,`, Hauptlagerplatz = Teil vor dem ersten Trenner, Mat.-Nr. ganzzahlig; Vorlage als Testdatei, Byte-Vergleich.
- P3 Beschichtung: Einmal-Skript (Trockenlauf) im Manifest, Kreditor in Übergeben wählbar solange leer, Druck (Barcode klein rechts oben, ruhiger, „Seite x von y“).
- P4 Userfelder: Einstellungsname bei „Ungültiger Spaltenname“ in allen Abgleichen, einmal je Lauf.
- P5 AG-Anlage N1: 2xx ohne Fund → „unklar“, keine Wiederholung, manuell bestätigen/erneut senden.

## Protokoll
- Start: Vorprüfung ok.
- P2 `1a972e72` Kürzel (SQL/162), P1 `9fd5181d` BF-CSV nach BF00000002 (SQL/163, Byte-Test), P3 `9d0a55e3` Beschichtung (Einmal-Skript,
  Kreditor in Übergeben, Druck), P4 `3e02dde7` Userfelder (Ursache: Hinweis hinten + 600-Zeichen-Kappung → Hinweis vorn aus `Diagnose`),
  P5 `cccf906f` N1 unklar + bestätigen; Doku `93d7795e` (v1.99.42, Changelog, Hilfe, README, TS-163, TS-157.9/.11/.21), Manifesttext `0af91b73`.
  Je Punkt Build + Tests grün; zuletzt Web 3288 (1 übersprungen), Dienst 790.
- Brain: Changelog [[2026-11-18-v1-99-42-nachbesserungen]], feature-map, Testindex 163, Fallstrick 53, datenmodell, Nachträge in den 4 Specs.
- Keine DB-Abfrage möglich (sqlcmd vom Klassifizierer abgelehnt) – P4-Ursache aus dem Code belegt (`SdataLeseException` kappt Message auf 600).
- QA (qa-agent, synchron): **Testbereit** für alle 4 Specs; Befund F1 (alte Unklar-Texte bis v1.99.41 wären nach Update automatisch wiederholt
  worden) behoben in `b5463336`; Tests Web 3291 (1 übersprungen), Dienst 790. DEV AKE + IDEAL v1.99.42 (Zielordner geleert), SQL/162+163,
  Einmal-Skript nur Trockenlauf. TS-163 auf DEV OK außer 163.8/163.11 (nicht prüfbar, per Unit-Test belegt). Druck/PDF-Nachweise
  `tests/2026-11-18-qa-v1-99-42-druck-pdf/` („Seite 1 von 2“/„Seite 2 von 2“ in Druck und PDF).
- Paket 7: `publish/paket7-v1.99.42` (Commit `b5463336`, publish-info.txt); Pakete 5/6 und übrige Dateien in `publish\` unverändert.
- Grenzen eingehalten: nichts an Sage, keine Einstellungen auf Test/Prod, Schalter Sage-AG aus, kein Merge, kein Push.
- **Offen (Mensch):** Kürzel aller BF-Besteller pflegen vor `BfCsvAktiv`; Manual-UAT TS-163.1 (WiCAM-Import), 163.8, 163.9 scharf (nach Backup),
  163.10 Edge-Drucker, 163.11 echtes SData, 163.12/E1b; dann Schranke 2.
