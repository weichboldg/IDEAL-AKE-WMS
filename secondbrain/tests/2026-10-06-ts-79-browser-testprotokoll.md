---
type: testprotokoll
title: "Browser-Testprotokoll TS-79 — IDEAL Kommissionierung nur am HauptFA, „Alle Ziele“ (v1.46.0)"
ausfuehrung: Claude Cowork (Abnahme)
system: "http://idealweb01.ideal.ideal-ake.at:88/"
spec: "[[2026-09-25-kommissionierung-nur-hauptfa-spec]]"
created: 2026-10-06
---

# Browser-Testprotokoll TS-79 — Kommissionierung nur am HauptFA (v1.46.0)

Grundlage: TS-79 in `docs/TESTSZENARIEN.md` und die manuelle Checkliste der Spec
[[2026-09-25-kommissionierung-nur-hauptfa-spec]]. Teil B läuft unter den Regeln der Arbeitsweise ABNAHME aus
[[cowork-projekt-ideal-ake-wms-anweisungen]]. Für v1.47.0 gilt weiter [[2026-09-29-ts-80-browser-testprotokoll]]
(offene Schritte siehe Teil C).

**Ablauf:** Teil A erledigst du vorab. Dann die Platzhalter `{...}` in Teil B ausfüllen und den Block als
Auftrag an Claude Cowork geben (eigene Aufgabe, Arbeitsweise Abnahme). Teil C machst du danach.

---

## Teil A — Vorbereitung (Mensch, vor dem Start)

**A1 — Einmaliger SQL-Lauf** (alte Sub-FA-Freigaben zurücksetzen): **erledigt** (Gerald, vor 2026-10-06).

**A2 — Testdaten auf der WMS-Datenbank des Testsystems:**

```sql
-- HFA: HauptFA mit HauptFA-Zeile, Artikelnummer und >= 2 offenen Sub-FAs
SELECT TOP 10 p.OrderNumber AS HauptFA,
       SUM(CASE WHEN p.SubOrderNumber <> p.OrderNumber THEN 1 ELSE 0 END) AS SubFAs
FROM dbo.ProductionOrders p
WHERE p.IsDone = 0 AND p.IsCancelled = 0
GROUP BY p.OrderNumber
HAVING SUM(CASE WHEN p.SubOrderNumber = p.OrderNumber AND ISNULL(p.ArticleNumber,'') <> '' THEN 1 ELSE 0 END) = 1
   AND SUM(CASE WHEN p.SubOrderNumber <> p.OrderNumber THEN 1 ELSE 0 END) >= 2
ORDER BY SubFAs DESC;

-- Ids und Sub-FA-Nummern zur gewaehlten HauptFA
SELECT Id, OrderNumber, SubOrderNumber,
       CASE WHEN SubOrderNumber = OrderNumber THEN 'HauptFA' ELSE 'Sub-FA' END AS Art
FROM dbo.ProductionOrders
WHERE OrderNumber = '<HFA>'
ORDER BY Art, SubOrderNumber;

-- HFA_ZIELE: HauptFA, deren Stueckliste Komm.-Ziele hat (moeglichst auch Positionen OHNE Ziel)
SELECT TOP 10 HauptFA,
       SUM(CASE WHEN LTRIM(RTRIM(ISNULL(Kommissionieren,''))) <> '' THEN 1 ELSE 0 END) AS MitZiel,
       SUM(CASE WHEN LTRIM(RTRIM(ISNULL(Kommissionieren,''))) =  '' THEN 1 ELSE 0 END) AS OhneZiel
FROM dbo.FaHierarchyNodes
GROUP BY HauptFA
HAVING SUM(CASE WHEN LTRIM(RTRIM(ISNULL(Kommissionieren,''))) <> '' THEN 1 ELSE 0 END) > 0
   AND SUM(CASE WHEN LTRIM(RTRIM(ISNULL(Kommissionieren,''))) =  '' THEN 1 ELSE 0 END) > 0
ORDER BY MitZiel DESC;

-- HFA_OHNE: HauptFA, deren Stueckliste KEIN einziges Komm.-Ziel hat
SELECT TOP 10 HauptFA FROM dbo.FaHierarchyNodes GROUP BY HauptFA
HAVING SUM(CASE WHEN LTRIM(RTRIM(ISNULL(Kommissionieren,''))) <> '' THEN 1 ELSE 0 END) = 0;

-- Id der HauptFA-Zeile zu HFA_ZIELE bzw. HFA_OHNE (fuer die Stuecklisten-Adresse)
SELECT Id, OrderNumber FROM dbo.ProductionOrders
WHERE OrderNumber IN ('<HFA_ZIELE>', '<HFA_OHNE>') AND SubOrderNumber = OrderNumber;
```

Kein Treffer für einen Fall: beim Platzhalter `KEIN_FALL` eintragen; der Agent wertet den Schritt dann als
NICHT PRÜFBAR.

**A3 — Browser und Benutzer:** In Chrome am Testsystem angemeldet, möglichst als **eigener Testbenutzer** mit
den Rollen `leitstand` + `picking` + `stock` (Leitstand, Stückliste, Kommissionierlisten). Der Benutzer darf
**keinen** gespeicherten Standard-Filter Kommissionierziel haben (Profil, Feld leer) — sonst ist B8 nicht
aussagekräftig.

**A4 — Einstellungen:** `ProduktionsauftragHierarchisch` an, `LeitstandAktiv` an.

**A5 — Freigabe der HauptFA** (für B6, die Warteschlange): Die HauptFA `{HFA}` im Leitstand **selbst
freigeben** — der Agent darf nicht freigeben. Ist sie schon freigegeben: nichts tun.

---

## Teil B — Auftrag an Claude Cowork (ab hier kopieren)

```text
ABNAHME - Browser-Testprotokoll TS-79 (Kommissionierung nur am HauptFA, v1.46.0)
Protokoll: C:\Git\IDEAL-AKE-WMS\secondbrain\tests\2026-10-06-ts-79-browser-testprotokoll.md
Spec: 2026-09-25-kommissionierung-nur-hauptfa-spec
Adresse: http://idealweb01.ideal.ideal-ake.at:88/
Es gelten die Regeln der Arbeitsweise ABNAHME aus den Projektanweisungen. Du pruefst, du reparierst nichts.

ZUSAETZLICH FUER DIESEN LAUF
- Im Leitstand NICHTS anklicken, was freigibt, zuweist, Prioritaeten setzt, fertigmeldet oder Glas/
  Fremdbezug/Lackierung umschaltet - auch nicht "Alle Sub-FAs fertigmelden". Du schaust nur, welche
  Bedienelemente vorhanden sind.
- Profil und Benutzerverwaltung nicht oeffnen.
- Die Stueckliste ist interaktiv: KEINE Positionen abhaken, nichts umbuchen, keine Lager- oder
  Glasbestellung ausloesen. Nur Filter, Dropdown, Zahnrad, Druckansicht.

TESTDATEN (von Gerald ausgefuellt; KEIN_FALL = Schritt NICHT PRUEFBAR)
- HauptFA mit Sub-FAs: {HFA}; ihre Sub-FA-Nummern: {SUBFA_NUMMERN}
- Id einer dieser Sub-FAs: {ID_SUBFA}; Id der HauptFA-Zeile: {ID_HFA}
- Stueckliste mit Zielen: HauptFA {HFA_ZIELE}, Id {ID_HFA_ZIELE}
- Stueckliste ohne Ziele: HauptFA {HFA_OHNE}, Id {ID_HFA_OHNE}

VOR DEM START
Leitstand, FA-Liste und Stueckliste kurz oeffnen und die sichtbaren Spalten notieren; am Ende
wiederherstellen.

SCHRITTE

B1 Leitstand: Bedienelemente nur an der HauptFA-Zeile (TS-79.11)
Menue Leitstand -> Kommissionierung. Die Gruppe "HauptFA {HFA}" finden (Spaltenfilter FA-Nummer oder
Blaettern) und aufklappen.
OK, wenn NUR die HauptFA-Zeile Freigeben-Knopf (bzw. Status "freigegeben"), Prioritaetsfeld,
Auswahl-Checkbox und Stuecklisten-Symbol zeigt und KEINE Sub-FA-Zeile eines davon zeigt; die Zellen der
Sub-FA-Zeilen stehen trotzdem unter den richtigen Spaltenkoepfen (nichts verrutscht). Screenshot.

B2 Leitstand: Gruppen-Kopfzeile ohne Freigabe-Kaskade (TS-79.10)
Dieselbe Gruppe, Kopfzeile ansehen.
OK, wenn es KEINEN Knopf "Alle Sub-FAs freigeben" gibt. Ob "Alle Sub-FAs fertigmelden" vorhanden ist,
notieren (nicht anklicken).

B3 FA-Liste: Stueckliste nur an der HauptFA-Zeile (TS-79.11, TS-79.18)
Menue -> FA-Liste (Produktionsauftraege), dieselbe HauptFA-Gruppe.
OK, wenn das blaue Stuecklisten-Symbol, das zur Kommissionier-Stueckliste fuehrt (Adresse
/Picking/Bom/...), nur an der HauptFA-Zeile steht. Notiere fuer eine Sub-FA-Zeile, ob dort ein anderes
Stuecklisten-Symbol (Adresse /FaWorklist/Bom/...) steht oder die Zelle leer ist - beides ist erlaubt,
haengt an der Vorbau-Rolle des Benutzers.

B4 Direkter Aufruf der Stueckliste einer Sub-FA (TS-79.12)
Adresse /Picking/Bom/{ID_SUBFA} aufrufen.
OK, wenn KEINE Stueckliste erscheint, sondern die Seite auf /Picking (Kommissionierung) weiterleitet und
oben die gelbe Meldung "Kommissionierung erfolgt nur am HauptFA {HFA}." steht. Screenshot.

B5 Direkter Aufruf der Stueckliste der HauptFA (TS-79.12, Gegenprobe)
Adresse /Picking/Bom/{ID_HFA} aufrufen.
OK, wenn die Stueckliste erscheint, ohne Warnmeldung. Positionszahl notieren.

B6 Kommissionier-Warteschlange nur mit HauptFAs (TS-79.16, TS-79.17)
Menue Kommissionierung (Warteschlange /Picking). Steht oben ein Haken "Alle Kommissionierauftraege
anzeigen", ihn setzen (reine Ansicht, aendert keine Daten) - sonst zeigt die Liste nur die dem Benutzer
zugewiesenen Auftraege.
OK, wenn die HauptFA {HFA} erscheint und KEINE der Sub-FA-Nummern {SUBFA_NUMMERN} irgendwo in der Liste
steht. Notiere, ob irgendeine Gruppe mehr als eine Zeile hat - erwartet ist je HauptFA genau eine Zeile;
mehrere Zeilen sind ein Befund (Ausnahme: der Spaltenfilter zeigt nur einen Ausschnitt).

B7 FA-Suche liefert nur HauptFAs (TS-79.22) - gleichwertiger Pruefweg
Die Such-Ansicht der Kommissionierung erscheint nur bei ausgeschaltetem Leitstand. Pruefe stattdessen
die Datenquelle dieser Suche: Adresse /api/productionorders/search?q={HFA} aufrufen. Die Antwort ist
eine Liste von Eintraegen mit "id" und "text"; der Text nennt nur die FA-Nummer (bei Sub-FAs dieselbe
Nummer {HFA}), deshalb zaehlt die id.
OK, wenn unter den Eintraegen, deren Text mit {HFA} beginnt, genau EINER ist und seine id {ID_HFA} ist
(Eintraege mit anderer FA-Nummer ignorieren). Vor v1.46.0 erschien {HFA} je Sub-FA erneut. Den Text der
Antwort in den Bericht kopieren.

B8 Stueckliste ohne Standardfilter: kein gelber Kasten (TS-79.20)
Adresse /Picking/Bom/{ID_HFA_ZIELE}, alle Filter geloescht (auch nach Neuladen).
OK, wenn ueber der Tabelle KEIN gelber Kasten "... Alle Filter zuruecksetzen" und KEINE leere
Badge-Zeile steht. Screenshot.

B9 "Alle Ziele" filtert auf Positionen mit Ziel (TS-79.2)
Dieselbe Stueckliste. Das Dropdown in der Spalte Komm.-Ziel oeffnen.
OK (Teil 1), wenn die ERSTE Option "Alle Ziele (nur kommissionier-relevant)" heisst.
Diese Option waehlen.
OK (Teil 2), wenn danach jede sichtbare Zeile in der Spalte Komm.-Ziel einen Wert hat, ein Badge
"Gefiltert: Alle Ziele (nur kommissionier-relevant)" erscheint und die Zeilenzahl kleiner ist als ohne
Filter. Baugruppen-Zeilen ohne eigenes Ziel verschwinden dabei, ihre Teile bleiben sichtbar - das ist
gewollt, KEIN Fehler. Zeilenzahlen vorher/nachher notieren. Screenshot.

B10 "Alle Ziele" und Einzelziele schliessen sich aus (TS-79.2)
Bei aktivem "Alle Ziele" im Dropdown zusaetzlich ein einzelnes Ziel waehlen.
OK, wenn "Alle Ziele" dabei automatisch abgewaehlt wird und nur noch das Einzelziel aktiv ist.
Dann wieder "Alle Ziele" waehlen: OK, wenn das Einzelziel abgewaehlt wird.

B11 Druckansicht mit "Alle Ziele" (TS-79.2)
Mit aktivem "Alle Ziele" auf "Drucken" klicken (Druckansicht oeffnet in neuem Tab; nicht drucken).
OK, wenn im Kopf der Filterhinweis "Komm.-Ziel=Alle Ziele (nur kommissionier-relevant)" steht und die
Druckansicht ungefaehr so viele Positionen zeigt wie der Bildschirm (Zahl notieren). Tab schliessen.

B12 Neu laden behaelt "Alle Ziele" (TS-79.6)
In der Stueckliste mit aktivem "Alle Ziele" die Seite neu laden (F5).
OK, wenn danach "Alle Ziele" im Dropdown steht, das Badge da ist und die Zeilenzahl wie in B9 ist.
(Ein kurzes Aufblitzen der ungefilterten Liste beim Laden ist bekannt, kein Befund.)

B13 Reset ueber das Badge (TS-79.2)
Auf das Kreuz im Badge klicken.
OK, wenn wieder alle Positionen erscheinen (Zeilenzahl wie vor B9) und das Dropdown leer ist.

B14 Nichts zu drucken (TS-79.3)
Adresse /Picking/Bom/{ID_HFA_OHNE}. "Alle Ziele" waehlen.
OK (Teil 1), wenn keine Zeile sichtbar bleibt und ein gelber Hinweis erscheint.
Dann "Drucken" klicken.
OK (Teil 2), wenn KEIN neuer Tab aufgeht und stattdessen der Hinweis "Keine sichtbaren Positionen -
nichts zu drucken." erscheint. Screenshot. Danach "Alle Ziele" wieder entfernen.

B15 Hilfe (TS-79.19)
Menue Hilfe. Suche (Strg+F) nach "nur am HauptFA" und nach "Alle Ziele".
OK, wenn beide Begriffe in einem erklaerenden Abschnitt vorkommen. Ueberschriften notieren.

ZUM SCHLUSS
Alle Filter loeschen, die zu Beginn notierte Spaltenauswahl wiederherstellen, 5 Sekunden warten,
neu laden und pruefen.

BERICHT
Wie in den Projektanweisungen (Chat-Tabelle, Berichtsordner
secondbrain\tests\<Datum>-ts-79-browser-testbericht\, eine Verweiszeile am Ende der Spec).
```

---

## Teil C — Nur durch den Menschen (nach dem Lauf)

- **TS-79.13 — Mehrfach-Freigabe gemischt:** im Leitstand mit HauptFA-Zeilen zweier Gruppen per Checkbox
  freigeben → beide freigegeben. Ein Mix mit Sub-FAs ist über die Oberfläche nicht mehr möglich (keine
  Checkbox); dieser Fall ist durch Unit-Tests belegt.
- **TS-79.5 / .7 / .21 — Profil-Standardfilter** (Profil speichern ist für den Agenten gesperrt):
    - `!(leer),KA-02` speichern → gespeichert `!(leer)`, gelber Hinweis „Alle Ziele schließt Einzelziele aus …“.
    - `!(LEER)` speichern → gespeichert `!(leer)`, **kein** Hinweis.
    - Stückliste öffnen → „Alle Ziele“ vorgewählt, Badge sichtbar.
    - Mit `!(leer)` die Stückliste `{HFA_OHNE}` öffnen → Dropdown gesperrt, „Alle Ziele“ vorgewählt,
      gelber Hinweis.
    - `KA'02` speichern → Stückliste lädt normal.
    - **Danach das Feld wieder leeren.**
- **TS-79.18 — Glas/Fremdbezug/Lackierung** einer Sub-FA im Leitstand umschalten (ändert Daten) → wirkt wie
  bisher; zurückschalten.
- **AKE-Gegenprobe** (TS-79.9, TS-79.14) auf `https://akenet01.ake.at:4444/`: Leitstand — jede Zeile hat
  Freigabe/Bulk/Stückliste; Stückliste ohne Komm.-Ziel-Dropdown; Profil ohne Feld „Standard-Filter
  Kommissionierziel“. *(Kann auch der Agent prüfen, wenn du das AKE-System im Auftrag ausdrücklich nennst.)*
- **TS-80-Nachlauf** ([[2026-09-29-ts-80-browser-testprotokoll]], erster Lauf
  [[2026-09-30-ts-80-browser-testbericht]]): Offen waren B2–B4, B7 und B10, weil Testdaten fehlten, sowie B6
  mit einer KW, die Daten enthält, B15 mit einer Lagerbestellung und der PDF-Download mit Freigabe.
  Testdaten aus dessen Teil A2 ergänzen und **nur diese Schritte** erneut laufen lassen.
- **Bericht des Agenten** liegt als eigener Ordner unter `secondbrain\tests\`; die Spec trägt am Ende eine
  Verweiszeile. Jeden FEHLER als Befund in die Spec eintragen.
