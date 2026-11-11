---
typ: feature
split: true
---
# Beschichtungsauftrag: summiert, Corporate-Design-Druck, eigene Nummer mit Ablauf-Status

**Aufgenommen 2026-10-06** (Anforderung Gerald, Koordination).

## Anforderung (Gerald)

- Der Beschichtungsauftrag soll auch **summiert** sein.
- Für den Druck eine **anpassbare HTML-Vorlage** im Corporate Design, mit **Logo und Anschrift aus den
  Standortinformationen**.
- Mehr Logik: **eigene Nummerierung mit Ablauf-Status**: bereits übergeben an Beschichter; Beschichtung
  erledigt; neue Beschichtung angefordert / Reklamation.
- **Zwei Buttons** im Reiter Beschichtungsauftrag:
  - Beschichtungsauftrag aus der **Vorschlagsliste** erstellen, zum Drucken der Aufträge.
  - **Reklamation**: dort sollen die Details leicht editierbar sein.
- Arbeitsablauf: speichern, freigeben, ausdrucken (**Freigabe kommt in Zukunft**).
- Dienstleister (Beschichter) in der WMS-Oberfläche editierbar – über den neuen Kreditoren-Stamm.
- **Vormerken, kommt in einer anderen Iteration:**
  - Häkchen für Reklamation/Neubeschichtung auf Positionsebene.
  - Bei Reklamation soll der ursprüngliche HauptFA ersichtlich sein.

## Ausgangslage (am Code belegt, Bündel-Worktree)

- Heute ist der Beschichtungsauftrag **eine gefilterte Ansicht** auf die Sage-Struktur (Filter
  `Beschichtet = -1`, [[2026-07-29-standort-ideal-teil-4-spec]]), **kein gespeichertes Objekt**: keine
  Nummer, kein Status. `FaHierarchyBeschichtungController` mit `[RequireBeschichtungsauftragAccess]` und
  `[RequireFaHierarchyBeschichtungAktiv]`; Views `Index.cshtml`, `Print.cshtml`.
- Druck und PDF sind **dasselbe Dokument** (Headless Edge, [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]]).
- Standortdaten vorhanden: `AppSettingKeys.Firmenname`, `AppSettingKeys.Firmenanschrift`
  (`Models/AppSettingKeys.cs:74-75`, Teil 6 Standorteinstellungen). **Ein Logo gibt es dort nicht.**
- Die Briefkopf-Frage ist bereits als [[2026-09-17-druck-briefkopf-logo-adresse]] aufgenommen
  (Lösungsrichtung: ein Partial `_PrintBriefkopf.cshtml` für alle Druckansichten, kein eigenes PDF-Layout).
  Diese Notiz **übernimmt** sie für den Beschichtungsauftrag.
- Dienstleister heute Freitext aus Sage (`FaHierarchyOrderInfo.Dienstleister`).

## Gewünschter Schnitt (split, Fundament zuerst)

1. **Summierte Ansicht** des heutigen Beschichtungsauftrags (unabhängig, klein).
2. **Briefkopf/Corporate-Design-Druck:** Logo + Anschrift aus den Standorteinstellungen, gemeinsamer
   Partial (übernimmt [[2026-09-17-druck-briefkopf-logo-adresse]]).
3. **Beschichtungsauftrag als eigenes Objekt:** Nummernkreis, Kopf (Dienstleister, RAL, Termine),
   Positionen mit Bezug auf HauptFA/Strukturknoten, Ablauf-Status und Statuswechsel mit Audit.
4. **Oberfläche:** Button „Aus Vorschlagsliste erstellen“ (Positionen aus der heutigen Ansicht wählen →
   Auftrag anlegen → speichern → drucken) und Button „Reklamation“ (Details editierbar). Dienstleister
   aus dem Kreditoren-Stamm wählbar.

Abhängigkeit: Baustein 4 braucht [[2026-10-06-kreditoren-stammdaten]].

## Für die Spec zu klären (Empfehlungen der Koordination in Klammern)

1. **Summierschlüssel** der summierten Ansicht: HauptFA + Artikelnummer + Maße (Breite/Höhe/Tiefe)?
   Ist RAL Teil des Schlüssels? Summiert wird nur, was fachlich gleich beschichtet wird.
2. **„Anpassbare HTML-Vorlage“:** anpassbar durch Anwender in der Oberfläche (gespeicherte Vorlage)
   oder als Datei, die die IT ändert? (Empfehlung: fester Partial mit Einstellungswerten für Logo,
   Anschrift, Fußtext – frei editierbares HTML in der Datenbank bringt Sicherheits- und Pflegerisiken.)
   Woher kommt das Logo (Upload in den Standorteinstellungen)?
3. **Nummernformat:** z. B. `BA-JJJJ-laufend`, je Standort getrennt?
4. **Status-Automat:** Anfangsstatus (Entwurf/angelegt), dann übergeben → erledigt; Reklamation als
   Status des Auftrags oder als neuer Folgeauftrag mit Verweis? (Empfehlung: Folgeauftrag mit Verweis
   auf den Ursprungsauftrag – bereitet „ursprünglicher HauptFA ersichtlich“ vor.) Welche Übergänge sind
   erlaubt, wer darf sie setzen?
5. **Was passiert mit Positionen, die schon in einem Auftrag stecken:** aus der Vorschlagsliste
   ausblenden oder markieren? Doppelvergabe verhindern.
6. **Freigabe-Schritt** kommt später – Datenmodell so anlegen, dass er ohne Umbau ergänzt werden kann,
   aber nicht vorab bauen.
7. **Vorgemerktes** (Positions-Häkchen Reklamation, Ursprungs-HauptFA) ausdrücklich **out of scope**,
   nur im Datenmodell nicht verbauen.
8. Kombinationsgeräte: mehrere FAInfos-Zeilen je HauptFA → mehrere Dienstleister/RAL möglich
   ([[2026-08-06-kombinationsgeraete-montageabteilung]]).

## Hinweis zur Reihenfolge

Baut auf dem Bündel auf (Teil 4, PDF). Umsetzung nach Schranke 2 des Bündels. Größtes Paket dieser
Runde; Baustein 3 ist eine Datenmodell-Erweiterung mit Migration.
