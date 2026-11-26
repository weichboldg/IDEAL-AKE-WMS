---
type: backlog
typ: feature
status: offen
created: 2026-11-26
quelle: "Gerald im Chat 2026-11-26: alle Befunde als Backlog, Spec nach der Schluss-QA von [[2026-11-26-buendel-1-lagerbestellung-goal-lauf]]"
standort: IDEAL
---
# Kommissionierauftrag Maske „Liste“: Nachbesserungen aus der QA (R-1 bis R-5)

Hinweise aus der QA von [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-3-spec]] (v1.99.89), alle ohne Blocker.

- **R-1 (Anzeige):** Mehrwertige Zellen über `VormontageService.FormatTexte` sind nicht natürlich sortiert – im summierten Druck steht
  Sage-Pos. „10, 8, 9“ für Positionen 6.8/6.9/6.10 (DEV IDEAL KO-2026-00004). Erwartet: natürliche Reihenfolge wie die Spalte Pos.
- **R-2:** Die Summenzeile trägt die Artikelnummer getrimmt und groß geschrieben, der Scan vergleicht exakt → eine Artikelnummer mit
  Kleinbuchstaben wird per Scan nicht angehakt. Bei IDEAL heute rein numerisch, also ohne praktische Wirkung – Vergleich angleichen.
- **R-3:** `AuftragDrucken?ansicht=liste&pdf=true` für einen Auftrag ohne FA-Struktur-Stückliste liefert still den normalen Druck je
  Position statt einer Meldung (nur per URL erreichbar). Erwartet: Hinweis statt stillem Ausweichen.
- **R-4 (Code):** Die Regel „offen = Offen oder Gepickt“ steht an etwa vier Stellen (`KommissionierauftragListe.Offen`, `OffeneZeilenIds`,
  Expansion und Einzelprüfung in `PickingTransferService`) – an einer Stelle zusammenführen.
- **R-5 (Code):** `IstMenge ?? 0m` in gebuchter Summe und Rest-Hinweis ist wirkungslos (gebuchte Zeilen haben immer eine Ist-Menge) – entfernen.
- Zusätzlich auf DEV gesehen: Quell-Lagerplatz zeigt im Druck „NAN“, wo kein Bestand gebucht ist – prüfen, ob für den Druck „—“ verständlicher ist.
