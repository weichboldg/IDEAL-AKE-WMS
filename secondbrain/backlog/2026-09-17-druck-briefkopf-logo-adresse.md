---
typ: feature
---
# Gemeinsamer Briefkopf fuer die Druckdokumente (Logo, Absender, Empfaenger)

**Aufgenommen 2026-09-17**, nachdem die PDF-Erzeugung (v1.39.0) am Testsystem laeuft und der
Beschichtungsauftrag erstmals als Datei vorlag.

## Ausgangslage

Die Druckansichten existieren bereits getrennt von den Bildschirmansichten
(`Views/FaHierarchyBeschichtung/Print.cshtml`, `Views/FaHierarchyKommissionierListen/Print.cshtml`).
Edge rendert genau diese Ansichten - **Bildschirmdruck und PDF sind dasselbe Dokument**, das war die
Kernentscheidung gegen QuestPDF/pdfme.

Was fehlt: **Briefkopf, Absenderadresse und Empfaengerfeld.** Das Layout des Beschichtungsauftrags
ist seit Teil 4 ausdruecklich **vorlaeufig** und wartet auf die Corporate-Design-Vorlage.

## Warum das mehr ist als eine Layoutaenderung

Der **Beschichtungsauftrag ist das einzige Dokument, das das Haus verlaesst.** Er geht an einen
externen Beschichter. Ein Dokument mit Aussenwirkung ohne Briefkopf, Absender und
Empfaengerangabe ist kein Schoenheitsfehler, sondern unvollstaendig.

Die Kommissionierlisten sind dagegen **intern** - dort ist ein Briefkopf nett, aber nicht noetig.
Das ist beim Schnitt zu beachten: Ein gemeinsamer Baustein heisst nicht, dass ihn jedes Dokument in
gleicher Form braucht.

## Loesungsrichtung: EIN Baustein, nicht vier Kopien

**`Views/Shared/_PrintBriefkopf.cshtml`** als Partial, eingebunden von jeder Druckansicht.
Aendert sich die Adresse, aendert sich eine Datei statt vier.

**Ausdruecklich NICHT: ein eigenes Layout nur fuers PDF.** Dann gaebe es dasselbe Dokument zweimal,
und die beiden Fassungen liefen auseinander - jemand aendert eine Spalte im Druck, vergisst das PDF,
und niemand merkt es, weil beide Wege einzeln funktionieren. Genau diese Doppelpflege war der Grund
fuer Headless Edge.

## Drei technische Pflichten (sonst sieht es am Bildschirm gut aus und im PDF nicht)

1. **Logo als Data-URI**, kein `<img src="/images/logo.png">`. Der Renderer laedt **keine externen
   Dateien**. Ein verlinktes Bild erscheint am Bildschirm und fehlt im PDF - ein Fehler, der genau
   dort auffaellt, wo er am teuersten ist.
2. **CSS eingebettet**, nicht als verlinktes Stylesheet. Dieselbe Begruendung; Teil 4 hat das
   bereits als Headless-Renderer-Vorbereitung entschieden.
3. **Wiederholung ueber Seiten** ist der anspruchsvolle Teil: Soll der Briefkopf auf **jeder** Seite
   erscheinen oder nur auf der ersten? Mehrseitig geht das ueber `position: fixed` oder
   Tabellen-`<thead>` - und beides verhaelt sich zwischen Bildschirm und Renderer nicht immer gleich.
   **Fruh pruefen, nicht am Ende.**

## Der Punkt, an dem QuestPDF wieder ins Spiel kaeme

In [[2026-08-06-pdf-erzeugung-fahierarchy-druck]] steht eine bewusst offene Tuer: Zeigt sich, dass
ein formelles Aussendokument mit Print-CSS **nicht sauber zu setzen** ist - Briefkopf auf jeder
Seite, Seitenzaehlung \u201eSeite X von Y\", praezise Positionierung -, dann ist **das** der Moment,
QuestPDF fuer **dieses eine Dokument** zu erwaegen.

**Nicht vorwegnehmen.** Erst mit Print-CSS versuchen; scheitert es an einem dieser Punkte, ist der
Befund da und die Lizenzfrage bemisst sich an einem konkreten Nutzen statt an einer Vermutung.

## Offene Fragen

1. **Corporate-Design-Vorlage:** Liegt sie vor (Logo in Druckaufloesung, Schrift, Farben,
   Adressblock-Anordnung)? Ohne sie ist das Ergebnis wieder vorlaeufig. **Das ist die
   Vorbedingung**, nicht eine Begleitfrage.
2. **Empfaengerfeld:** Woher kommen die Beschichter-Daten? Frei eingegeben, aus Stammdaten, oder
   aus `OrderRecipients`? Am Code zu pruefen - nicht annehmen.
3. **Briefkopf auf jeder Seite oder nur auf der ersten?** Siehe Pflicht 3.
4. **Gilt der Baustein auch fuer die internen Kommissionierlisten** - in gleicher Form, in
   reduzierter Form (nur Logo), oder gar nicht?
5. **Seitenzaehlung \u201eSeite X von Y\"** gewuenscht? Bei einem Aussendokument ueblich, mit Print-CSS
   aber nicht trivial - und ein Kandidat fuer den QuestPDF-Befund oben.
6. **Wer prueft das fertige Dokument, bevor es an einen Dienstleister geht?** Offener Punkt seit
   Teil 4, hier erneut faellig.

## Nebenbefund vom Testsystem (2026-09-17)

Im Beschichtungsauftrag zeigte eine Gruppe **\u201eDienstleister? \u00b7 RAL 0\"**, waehrend eine andere
vollstaendige Kopfdaten trug (Beschichter, RAL, Start- und Retourtermin). Vor dem Briefkopf-Ausbau
klaeren, ob das eine **Datenluecke** bei diesem Auftrag ist oder ein **Anzeigefehler**.
Auf einem Dokument, das an einen Dienstleister geht, ist ein fehlender Empfaenger mehr als
Kosmetik - und er faellt im Briefkopf staerker auf als in einer Tabellenzeile.

## Bezug

[[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]] (Erzeugung, QuestPDF-Tuer) \u00b7
[[2026-07-29-standort-ideal-teil-4-spec]] (vorlaeufiges Layout, CSS-inline/Data-URI-Entscheidung) \u00b7
Backlog-Punkt \u201eVormontage-Druckansicht\" (dritter kuenftiger Verbraucher des Bausteins)
