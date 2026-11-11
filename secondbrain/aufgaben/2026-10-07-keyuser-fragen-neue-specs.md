---
typ: notiz
status: offen
created: 2026-10-06
bezug: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-uebersicht]]"
---
# Fragen an die Keyuser – neue Specs vom 06.10.2026

Zum Ausdrucken oder am Bildschirm durchgehen. Jede Frage hat ein Beispiel, was die Antwort bewirkt, und
einen Vorschlag (★). Wo der Vorschlag passt, genügt ein Haken. Die Kennung in Klammern (z. B. „FA-T2/3“)
zeigt, in welche Spec die Antwort gehört – die Koordination trägt sie danach ein.

Bereits beantwortet (06.10.): B3–B6 und F1 – mit ✅ markiert.

Teil A–F sind für die Keyuser. Teil G klärt Gerald mit der IT allein, Teil H sind Prüfabfragen vorab.

---

## A. FA-Struktur: Arbeitsgänge als Symbole und BDE-Status

Vorbild ist die Sage-Ansicht „Fertigungsaufträge bearbeiten“: Jede Zeile im Baum zeigt ihre Arbeitsschritte
als Kürzel, z. B. `G-KT-A-BODEN-1320-660` → **LS KA PG**. Diese Kürzel erscheinen im WMS künftig als kleine
Symbole. Mit aktivem BDE werden sie eingefärbt: grün = erledigt, grau = offen, rot = überfällig.

**A1. Die wichtigste Frage zuerst: An welchem Auftrag wird ein Arbeitsgang gebucht?** (FA-T2/7, W-1)
Heute legt das WMS für einen Sub-FA Arbeitsgänge aus seiner **eigenen Zeile und seinen direkten Kindern** an.
Beispiel: Die *Mittelholmblende* (eigener Sub-FA) trägt `VM`. Das WMS legt `VM` an der Mittelholmblende an
**und** am übergeordneten *Korpus*. Am BDE-Terminal ist `VM` damit an zwei Aufträgen buchbar.
- a) Richtig so – Vormontage gehört zu beiden Aufträgen.
- b) Falsch – `VM` gehört nur zur Mittelholmblende; am Korpus soll es nicht buchbar sein.
  (Folge: Korrektur der Erkennung im Bündel, vor dessen Abnahme.)
- ★ Keine Empfehlung – das ist eine Fertigungsfrage. Am besten an einem echten Auftrag am Terminal zeigen.
Antwort: ______________________b - und die Teile mit VLS KA müssen auch rückmeldbar sein

**A2. Wo stehen die Symbole?** (FA-T1/2)
- a) Direkt in jeder Baumzeile, wie in Sage – Überblick ohne Klick.
- b) Nur im Fenster „Arbeitsgänge“, das man je Auftrag öffnet (wie heute).
- ★ a), das Fenster bleibt für Details. Kein Mehraufwand beim Laden, weil die Kürzel schon da sind.
Antwort: ______________________a

**A3. Was steht im Symbol?** (FA-T1/3)
- a) Das Sage-Kürzel (`KA`, `PG`, `VM`) – b) die Werkbank-Nummer (z. B. `2300`).
- Name der Werkbank: nur beim Draufzeigen (am Touch-Terminal nicht sichtbar) oder auch beim Antippen?
- ★ Kürzel sichtbar; Name beim Draufzeigen, Antippen kommt später.
Antwort: ______________________a 

**A4. Woran erkennt man den Status ohne Farbe?** (FA-T2/1)
Farbe allein reicht an Terminals bei schlechtem Licht oder bei Farbschwäche nicht.
- ★ Zusätzlich ein Zeichen: Haken = erledigt, leerer Kreis = offen, Warndreieck = überfällig.
- Soll der Statustext sichtbar am Symbol stehen (braucht Platz) oder genügt das Zeichen?
Antwort: ______________________zusätzlich ein Zeichen - ja.

**A5. Wann ist ein Arbeitsgang „überfällig“?** (FA-T2/2)
Es gibt kein Soll-Datum je Arbeitsgang, nur Termine am Auftrag.
- a) `FE_Termin` („Blechfertigung fertig“) ist überschritten und der Arbeitsgang ist nicht erledigt.
- b) `Neuer_PT_PPS` (aktueller Fertigstellungstermin aus dem PPS) ist überschritten.
- c) ein anderer Termin: __________
- Beispiel: FE_Termin 10.10., heute 12.10., `KA` noch offen → rot.
Antwort: ______________________berechnungslogik mit offset tagen wie bei oseon reporting - oseon arbeitsgänge und ansichten wie oseon reporting / teileverfolgung.

**A6. Wann ist ein Arbeitsgang „erledigt“?** (FA-T2/3)
- a) Wenn am BDE-Terminal abgeschlossen (so zeigt es das Terminal heute schon).
- b) Wenn in der Teileverfolgung manuell rückgemeldet.
- c) Wenn eines von beiden zutrifft.
- ★ a).
Antwort: ______________________BDE Rückmeldung

**A7. Was heißt „BDE aktiv“?** (FA-T2/4)
BDE lässt sich global einschalten und zusätzlich je Werkbank.
- ★ Beide müssen an sein; sonst bleiben die Symbole neutral grau (ohne Status).
Antwort: ______________________beide Aktiv - 

**A8. Ein Arbeitsgang wird gerade gebucht (läuft).** (FA-T2/5)
- a) grau wie offen – b) eigener Zustand „läuft“ (z. B. blau mit Uhr).
Antwort: ______________________eigener Zustand wie vorschlag

**A9. Aufträge im BDE anzeigen (Teil 3 – nur Rahmen klären).** (FA-T3/1–5)
- Wo soll die Anzeige sein: am BDE-Terminal je Werkbank, im BDE-Cockpit oder als neue Übersicht?
- Für wen: Werker, Schichtleitung, BDE-Admin, auch Planung/Leitstand?
- Welche Aufträge: alle offenen, nur die der gewählten Werkbank, nur ein Terminfenster (z. B. diese Woche)?
- Mit Status-Farben (A4–A8) oder nur mit den Symbolen?
- Nur IDEAL oder auch AKE?
Antwort: ______________________am bde terminal je werkbank bzw. ag sollen die offenen aufträge anzgezeigt werden können. bei AG wählen - wo hier haben wir ja schon eine logik 

---

## B. Beschichtungsauftrag

Heute ist der Beschichtungsauftrag nur eine gefilterte Liste aus Sage, ohne Nummer und ohne Status. Geplant
in vier Schritten: 1 summierte Ansicht, 2 Briefkopf, 3 Auftrag mit Nummer und Status, 4 Bedienung
(Vorschlagsliste, Reklamation, Dienstleister).

### B-1 Summierte Ansicht

**B1. Was wird zusammengezählt?** (BA-T1/1)
- ★ Gleicher HauptFA + gleiche Artikelnummer + gleiche Maße (Breite/Höhe/Tiefe) = eine Zeile mit Summe.
  Die RAL-Farbe steht am Auftragskopf und ist je HauptFA ohnehin gleich.
- Beispiel: 3 × `BLENDE-400` 400/80/2 im selben HauptFA → eine Zeile „Menge 3“.
Antwort: ______________________HAUPTFA + gleiche Artikelnummer wie beispiel

**B2. Standard und Druck.** (BA-T1/2)
- ★ Die Einzelliste bleibt Standard (aus ihr wird später der Auftrag gebildet). Die summierte Ansicht bekommt
  Druck und PDF – das ist das Blatt für den Beschichter.
Antwort: ______________________Summierte als Standard

### B-2 Briefkopf / Corporate Design

✅ **erledigt** · **B3. Gibt es eine Corporate-Design-Vorlage?** (BA-T2/2)
Logo in Druckqualität, Schrift, Farben, Anordnung des Adressblocks. Ohne sie bleibt das Ergebnis vorläufig.
Antwort / wer liefert: **CD-Manual 01/23 + Logos (Projekt) – eingetragen 06.10.**

✅ **erledigt** · **B4. Briefkopf auf jeder Seite oder nur auf der ersten?** „Seite 1 von 3“ gewünscht? (BA-T2/3)
- ★ Nur erste Seite, ohne Seitenzählung – „jede Seite“ ist deutlich aufwändiger.
Antwort: **Seite 1; Seitenzählung nice to have – eingetragen 06.10.**

✅ **erledigt** · **B5. Briefkopf auch auf den (internen) Kommissionierlisten?** (BA-T2/4)
- ★ Nein, vorerst nur auf dem Beschichtungsauftrag (geht später mit wenig Aufwand nach).
Antwort: **Nein – eingetragen 06.10.**

✅ **erledigt** · **B6. Firmenanschrift:** ein mehrzeiliges Feld oder getrennt Straße/PLZ/Ort/Land? (BA-T2/5)
- ★ Ein mehrzeiliges Feld in den Standorteinstellungen.
Antwort: **Mehrzeilig; Tippfehler in der Adresse wird korrigiert – eingetragen 06.10.**

### B-3 Auftrag mit Nummer und Ablauf

**B7. Nummer.** (BA-T3/1)
- ★ `BA-2026-00001`, jedes Jahr wieder ab 1.
Antwort: ______________________ja

**B8. Ablauf und Reklamation.** (BA-T3/2)
- ★ Entwurf → Übergeben an Beschichter → Beschichtung erledigt. Storno nur im Entwurf. Kein Zurückschalten
  (Korrektur = stornieren und neu anlegen).
- Reklamation: ★ als **neuer Auftrag mit Verweis** auf den ursprünglichen (BA-2026-00017 → Reklamation
  BA-2026-00023). Vorteil: Später ist sichtbar, woher die Reklamation kommt.
- Reklamation erst nach „erledigt“ – oder auch schon nach „übergeben“?
Antwort: ______________________Reklamation - Vorschlag

**B9. Wer darf Aufträge anlegen und Status setzen?** (BA-T3/3)
Heute gibt es für den Beschichtungsauftrag nur ein Leserecht.
- ★ Neues Recht „Beschichtungsauftrag bearbeiten“, ein Recht für alles. Oder: getrennt, z. B. nur das Lager
  darf „erledigt“ setzen?
- Welche Personen bekommen das Recht? __________
Antwort: neue Berechtigungsgruppe - Beschichtungsaufträge______________________

**B10. Teile, die schon in einem Auftrag sind.** (BA-T3/4, BA-T4/1)
- ★ In der Vorschlagsliste markiert (mit Auftragsnummer), nicht auswählbar – nicht ausgeblendet, sonst sieht
  niemand, warum ein Teil fehlt. Stornierte Aufträge geben ihre Teile wieder frei.
Antwort: ______________________sollte nur bei reklamationen der fall sein. sonst dürfte das nicht passieren.

**B11. Mehrere HauptFA in einem Auftrag?** (BA-T3/5)
- ★ Ja (eine Fahrt zum Beschichter), aber **ein** Beschichter und **eine** RAL-Farbe je Auftrag. Mischt man
  verschiedene RAL, gibt es eine Warnung.
Antwort: ______________________nein, soll nicht möglich sein. pro hauptfa ein auftrag

**B12. Menge und spätere Änderungen in Sage.** (BA-T3/8)
- ★ Beim Anlegen wird die Sollmenge übernommen und eingefroren; im Entwurf änderbar (Teilmenge möglich).
  Ändert sich danach etwas in Sage, ändert sich der Auftrag nicht automatisch.
- Beispiel: Auftrag mit 10 Stück angelegt, in Sage später 12 → Auftrag bleibt bei 10.
Antwort: ______________________wie vorgeschlagen.

**B13. Termine im Auftrag.** (BA-T3/9)
- ★ „Start Beschichtung“ und „Beschichten retour“ aus Sage vorbelegen, im Entwurf änderbar, nicht nach Sage
  zurückschreiben. Oder eigene WMS-Termine (Übergabe/Rückgabe)?
Antwort: ______________________Beschichtungstermin und Beschichten retour sollen herangezogen werden aus den WMS Terminen

### B-4 Bedienung

**B14. Auswahl in der Vorschlagsliste.** (BA-T4/2)
- ★ Häkchen je Teil plus Häkchen für die ganze HauptFA-Gruppe; gilt für die aktuelle Seite.
Antwort: ______________________vorschlagsliste automatisch übernehmen. Editieren ermöglichen.

**B15. Reklamation: Was ist editierbar?** (BA-T4/3)
- ★ Neuer Auftrag mit allen Teilen des Ursprungs vorbelegt; im Entwurf: Teile entfernen, Menge ändern, Grund/
  Bemerkung, Beschichter, Termine. Neue Teile nur aus dem Ursprungsauftrag.
Antwort: ____________________Teile auswählen, nichts vorbelegen - vermerk auf Ursprungsauftrag

**B16. Beschichter am Auftrag.** (BA-T4/4)
- ★ Pflicht erst beim Übergeben, im Entwurf optional. Vorbelegt, wenn der Sage-Text eindeutig einem
  Kreditor zugeordnet ist; der Sage-Text bleibt daneben sichtbar.
Antwort: ______________________Standardbeschichter aus WMS-View "PPS-View" wobei der Beschichter überschrieben werden können soll - besonders bei Reklamation

**B17. Druck des Auftrags.** (BA-T4/5, /6, /8)
- ★ Mit Briefkopf und Empfängeradresse des Beschichters; Teile summiert (wie B1); Barcode = Auftragsnummer.
- ★ Drucken schon im Entwurf erlaubt, ohne „ENTWURF“-Aufdruck (bis der Freigabeschritt kommt).
Antwort: ______________________1.

**B18. Was ist nach dem Übergeben noch änderbar?** (BA-T4/7)
- ★ Nur noch Status und Bemerkung; Kopf und Teile nur im Entwurf.
Antwort: ______________________

---

## C. Kommissionierliste: AB-Nummer und Position

**C1. Wo?** (KL/1, /2)
- ★ Im Gruppenkopf je HauptFA (dort steht die AB-Nummer in der Liste heute schon), bei Kombigeräten alle.
  In der summierten Ansicht fehlt sie bisher ganz; die Position fehlt überall.
Antwort: ______________________AB-Nummer in Summierter Ansicht nicht

**C2. Wo überall, und Suche?** (KL/3)
- ★ Liste, Summiert, Druck und PDF.
- Suche nach AB-Nummer gewünscht? (Extra-Suchfeld; ist eigener Aufwand, nicht im Plan.)
Antwort: ______________________AB Nummer in Listen nice to have - wäre normal in der PPS View vorhanden.

---

## D. Vormontage-Listen: Abteilung

Gemeint ist laut Gerald das Feld „Arbeitsbereich“ je Zeile (Werte wie `H2-02`, `K-02`, `S-01`) – wird mit
Abfrage P5 bestätigt.

**D1. Summierte Ansicht.** (VM/1)
- ★ Abteilung nur anzeigen und filtern; der Filter wirkt vor dem Zusammenzählen.
- Beispiel: Filter `H2-02` → Summe zählt nur die Teile aus H2-02, nicht die aus K-02.
Antwort: ______________________ja, genau

**D2. Spaltenname.** (VM/2)
- a) „Abteilung“ wie in Sage – b) „Arbeitsbereich“ wie in den Kommissionierlisten.
- Achtung: Im Gruppenkopf steht heute die Montage-Abteilung des Auftrags – das ist ein anderer Wert.
Antwort: ______________________Arbeitsbereich

---

## E. CSV-Export (alle Tabellen)

**E1. Welche Zeilen?** (CSV/1, /9)
- ★ Alle Treffer des aktuellen Filters über alle Seiten (nicht nur die sichtbare Seite), höchstens 5.000,
  darüber mit Warnung.
Antwort: ______________________ja, genau

**E2. Welche Spalten und welche Reihenfolge?** (CSV/2, /10)
- ★ Die gerade sichtbaren Spalten, in Bildschirm-Reihenfolge und -Sortierung – „wie am Bildschirm“.
Antwort: ______________________wie eingeblendet und sortiert korrekt

**E3. Gruppierte Listen** (z. B. nach HauptFA). (CSV/4)
- ★ Flach, der HauptFA steht als eigene Spalte in jeder Zeile (gut für Excel-Filter).
Antwort: ______________________flach

**E4. Kennzeichen und Häkchen.** (CSV/11)
- ★ Häkchen als „Ja/Nein“, farbige Kennzeichen als Text.
Antwort: ______________________ja/nein korrekt - farbig egal

---

## F. Stückliste: Fehler im Zahnrad-Fenster

✅ **erledigt** · **F1. Was passiert genau?** (Backlog [[2026-10-06-stueckliste-zahnrad-bug-matchcode]])
→ Geklärt 06.10.: Zahnrad nicht sichtbar; Ursache gefunden (Platzierung seit v1.42), Korrektur v1.47.3 läuft.
Fehlen Spalten im Fenster, falsche Reihenfolge, Auswahl wird nicht gespeichert, Fenster geht nicht auf …?
Auf welchem System (AKE/IDEAL) und in welcher Stückliste? Am besten mit Screenshot.
Antwort: ______________________

---

## G. Klärt Gerald mit der IT (keine Keyuser-Frage)

Vorschläge der Koordination; ein Haken genügt.
- **FA-T2/7 W-2 bis W-4** (Randfälle): ★ W-2 kein Rückfall auf den Vater („kein BDE-Arbeitsgang“);
  W-3 verwaistes Teil: „kein BDE-Arbeitsgang“; W-4 mehrere gleiche Arbeitsgänge: erledigt erst, wenn alle
  erledigt.
- **BA-T2/1** Vorlage: ★ fester Baustein mit Einstellungen (Logo als Dateipfad, Anschrift, Fußtext), kein frei
  editierbares HTML.
- **BA-T3/1** Haben AKE und IDEAL je eine eigene Datenbank? (dann Nummer ohne Standortkürzel) – Antwort: ____
- **BA-T3/6, /7** Freigabe-Schritt und vorgemerkte Punkte nur vorbereiten, nicht bauen: ★ bestätigen.
- **KL/4** entfällt bei Gruppenkopf.
- **VM/3** Kombigeräte-Trennung bleibt eigener Schritt: ★ bestätigen.
- **CSV/3, /5–/8, /12**: ★ Semikolon/Dezimalkomma/UTF-8, Schutz vor Formel-Einschleusung, nur mit
  Leserecht, ein gemeinsames Skript (Variante A, Erzeugung im Browser), alle 40 Tabellen in einer Spec.
- **Kreditoren** (zurückgestellt): Pflichtfelder, Zuordnung über Kurzname, Standort, nur deaktivieren – bei
  Gelegenheit.

## H. Prüfabfragen vorab (IDEAL-WMS-Datenbank)

- **P5** (Vormontage-Spec) und **P-K1** (FA-Struktur Teil 1): Arbeitsbereich/Arbeitsschritte für die vier
  Artikel aus dem Sage-Screenshot. Erwartung: A-BODEN = `H1-03` / `LS KA PG`, FR-4301-…-1530 = `S-01` / `ZS`,
  Mittelholmblende = `H2-02` / `VM`, Korpus-Gepunktet = leer / `BE`.
- **P1** (FA-Struktur Teil 2): Bedeutung von `FE_Termin` und `Neuer_PT_PPS` an zwei bekannten Aufträgen
  prüfen – Grundlage für A5.
