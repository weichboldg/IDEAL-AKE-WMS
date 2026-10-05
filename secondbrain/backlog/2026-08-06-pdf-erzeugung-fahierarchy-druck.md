---
typ: feature
---
# PDF-Erzeugung fuer FaHierarchy-Druckdokumente (Querschnitts-Baustein)

Herausgeloest aus [[2026-07-29-standort-ideal-teil-4-spec]] (Befund B-3). PDF-Erzeugung ist
Infrastruktur, kein Merkmal von Beschichtungsauftraegen \u2014 und Teil 3/4/5 teilen sich dasselbe
Druckgeruest. Wuerde Teil 4 sie bauen, muesste Teil 5 sie danach wieder anfassen.

## Worum geht es

Die IDEAL-Druckdokumente (Kommissionierliste, Beschichtungsauftrag, Vormontage) sollen sich nicht
nur drucken, sondern als **PDF-Datei erzeugen** lassen. Der bestehende `PrintService`
(`rundll32 mshtml.dll,PrintHTML`) druckt nur und kann das nicht.

## Entschieden

- **Renderweg: Headless Edge/Chrome ueber Prozessaufruf.**
  ```
  msedge.exe --headless --disable-gpu --print-to-pdf="<ziel>.pdf" --no-pdf-header-footer "<quelle>.html"
  ```
  Passt zum bestehenden Muster (`PrintService` ruft heute schon ein OS-Werkzeug auf), braucht kein
  NuGet-Paket und kein mitgeliefertes Chromium, und der Renderer wird ueber Windows Update
  gepflegt. **Dasselbe HTML** wie im Bildschirmdruck \u2014 ein Layout, keine zweite Wahrheit.
- **Abgelehnt:** *QuestPDF* (Layout in C# \u21d2 Druck-Layout doppelt gepflegt), *wkhtmltopdf*
  (veralteter WebKit, Wartungsstand fraglich), *PuppeteerSharp* (laedt eigenes Chromium ~150 MB;
  nur als Rueckfallebene, falls der Prozessaufruf an Grenzen stoesst).
- **Ausloesung:** auf Knopfdruck, **ein PDF je `HauptFA`** (nicht ein Sammeldokument \u2014 ein Auftrag
  geht an einen Empfaenger und muss mailbar/ablegbar sein). Der Knopf sitzt an der Gruppe.
- **Ablage:** vorerst **reiner Download** \u2014 temporaer erzeugen, ausliefern, loeschen. Keine
  serverseitige Ablage, keine Aufbewahrungsregel.
- **Benennung:** `<Dokumentart>_<HauptFA>_<yyyyMMdd-HHmm>.pdf`, z. B.
  `Beschichtungsauftrag_104857_20260806-1430.pdf`. Dokumentart zuerst (sortiert nach Art), HauptFA
  als Identifier (der einzige, in dem die Produktion kommuniziert), Zeitstempel zuletzt als
  Eindeutigkeit. Keine Klarnamen (Dienstleister/Kunde) \u2014 Umlaute und Sonderzeichen machen beim
  Mailversand und auf Freigaben Probleme.
- **Schnitt (wichtig):** **Erzeugung und Auslieferung trennen.** Ein Dienst erzeugt aus dem
  Print-HTML einen PDF-Bytestrom; was damit geschieht (heute Download-Response; spaeter Datei
  schreiben, Mail anhaengen, in enaio ablegen) ist Sache des Aufrufers. Vermischt man beides,
  kostet jede spaetere Verwendung einen Umbau.

## Zu beachten bei der Umsetzung

- **Alle Ressourcen im Print-HTML muessen eingebettet sein** \u2014 CSS inline, Bilder als Data-URI.
  Headless Edge folgt keinen authentifizierten Links und holt sich weder Stylesheets noch Logos
  vom Webserver. Klassischer Stolperstein dieses Ansatzes.
- Vorhandensein von Edge auf dem Zielserver pruefen (bei Server Core nicht selbstverstaendlich).
- Schreibbares Temp-Verzeichnis, Prozess-Timeout, Aufraeumen der Temp-Dateien, Verhalten bei
  Parallelaufrufen.

## Spaeter, bewusst NICHT im Umfang

Ablage auf einer Dateifreigabe, Archivierung in enaio, automatischer Mailversand an den
Dienstleister. Der Schnitt oben haelt diese Wege offen.

## NACHTRAG 2026-09-10: QuestPDF neu bewertet

Auf Wunsch nochmals geprueft (https://github.com/QuestPDF/QuestPDF). Die urspruengliche Ablehnung
oben war **einseitig begruendet** - sie nannte nur den Nachteil.

### Lizenz - zuerst zu klaeren, weil es eine Kaufentscheidung ist

QuestPDF ist **frei fuer Privatpersonen, Non-Profits, Open-Source-Projekte und Organisationen unter
1 Mio. USD Jahresumsatz**. Darueber ist eine **kommerzielle Lizenz** noetig.
IDEAL-AKE liegt mit zwei Standorten mit hoher Wahrscheinlichkeit darueber - das waere also ein
Einkaufsvorgang, kein `dotnet add package`. Laut Anbieter planbare Preise ohne Per-Seat- oder
Per-Server-Gebuehren; Konditionen vor der Entscheidung einholen.
**Headless Edge kostet nichts** - das ist ein realer Unterschied, kein Detail.

### Was fuer QuestPDF spricht (in der ersten Bewertung unterschlagen)

- **Kein externer Prozess, keine Edge-Abhaengigkeit** auf dem Server - einer der drei Stolpersteine
  oben faellt damit weg (Server Core!).
- **Keine Print-CSS-Ueberraschungen.** Genau diese Klasse haben wir gerade erlebt: Die Whitelist in
  `PrintBom.cshtml` kannte die neuen Spalten nicht, und der Ausdruck kam ohne sie.
- **Deterministisch** - kein Browser-Versionsdrift zwischen Testsystem und Produktion.
- Seitenumbrueche, Wiederholungskoepfe, PDF/A - fuer mehrseitige Stuecklisten relevant.

### Der Nachteil bleibt: doppelte Layout-Pflege

Das HTML-Druckgeruest existiert bereits (Teil 3/4/5, `PrintBom`, `PrintPicking`). Mit QuestPDF gaebe
es das Layout ein zweites Mal in C# - und zwei Fassungen desselben Layouts laufen auseinander. Das
ist dieselbe Fehlerklasse, die uns in diesem Paket mehrfach getroffen hat.

### Die Entscheidung haengt an EINER Frage - und die wurde bisher nicht gestellt

> **Ist das PDF DASSELBE Dokument wie der Bildschirmdruck, oder ein ANDERES?**

- **Dasselbe Dokument** (das Druck-HTML, nur als Datei statt am Drucker) -> **Headless Edge**.
  Ein Layout, zwei Ausgabewege. Eine zweite Fassung waere dann echte Doppelpflege.
- **Ein anderes Dokument** - etwa ein formeller **Beschichtungsauftrag**, der das Haus verlaesst,
  mit Briefkopf, Empfaengerfeld, Seitenzaehlung, Corporate Design -> **QuestPDF**.
  Dann ist es **keine Doppelpflege**, sondern ein eigenes Dokument, das zufaellig dieselben Daten
  zeigt - und der Hauptnachteil entfaellt.

Das Beschichtungsauftrags-Layout ist ohnehin noch **vorlaeufig** und wartet auf die
Corporate-Design-Vorlage (offener Punkt aus Teil 4). Faellt die Antwort auf "anderes Dokument", ist
QuestPDF der passendere Weg - und die Lizenzfrage wird zur naechsten Huerde.

**Denkbar ist auch beides:** Headless Edge fuer die internen Listen (identisch zum Bildschirmdruck),
QuestPDF fuer das eine Dokument, das an einen Dienstleister geht. Zwei Werkzeuge fuer zwei Zwecke
ist keine Inkonsequenz - doppelte Pflege **desselben** Dokuments waere eine.

### Ebenfalls geprueft (2026-09-10): pdfme

https://github.com/pdfme/pdfme - **MIT-Lizenz, wirklich frei, keine Umsatzschwelle.** Das ist der
eine Punkt, in dem es QuestPDF klar schlaegt.

**Aber: Es ist TypeScript, nicht Rust** (aufgebaut auf pdf-lib, fontkit, PDF.js, React) - und damit
ein anderes Oekosystem als unsere ASP.NET-Core-Anwendung. Es gibt keine .NET-Anbindung. Zwei Wege
waeren denkbar, beide mit einem Haken:

**(a) Node auf dem Server.** Wir taeten damit genau das, was wir mit der Ablehnung von
PuppeteerSharp und der Edge-Abhaengigkeit vermeiden wollten: eine **neue Laufzeitumgebung** auf dem
WMS-Server einfuehren, samt eigenem Update- und Sicherheitszyklus. Gegenueber Edge ist das eher
mehr Aufwand, nicht weniger - Edge ist auf Windows ohnehin da und wird ueber Windows Update
gepflegt.

**(b) Erzeugung im Browser.** Technisch reizvoll: kein Server-Dienst, keine Laufzeit, kein
Temp-Verzeichnis, keine Parallelitaetsfrage. **Widerspricht aber der Schnitt-Entscheidung oben.**
Dort steht ausdruecklich: Erzeugung und Auslieferung trennen, damit spaeter *Datei schreiben, Mail
anhaengen, in enaio ablegen* moeglich bleibt. Entsteht das PDF auf einem Fertigungsterminal, liegt
es **auf dem Terminal** - und genau diese Wege sind zu.
Fuer einen reinen Download waere (b) elegant. Fuer einen Beschichtungsauftrag, der spaeter
automatisch an den Dienstleister gehen soll, ist es der falsche Ort.

**Ein Punkt bleibt trotzdem interessant:** pdfme fuehrt Vorlagen als **JSON** mit einem
WYSIWYG-Designer. Das hiesse, das Beschichtungsauftrags-Layout koennte jemand **ohne
Programmierkenntnisse** anpassen - genau die offene Corporate-Design-Frage aus Teil 4. Bei QuestPDF
und bei Headless Edge braucht jede Layoutaenderung einen Entwickler.
Ob das den Oekosystembruch aufwiegt, ist eine Abwaegung - **aber sie gehoert gestellt**, bevor
entschieden wird.

### Zwischenstand der drei Kandidaten

| | Kosten | Server-Abhaengigkeit | Layout-Pflege |
|---|---|---|---|
| **Headless Edge** | frei | Edge (auf Windows ohnehin da) | dasselbe HTML - keine Doppelpflege |
| **QuestPDF** | **kommerziell** ab 1 Mio. USD Umsatz | keine | zweites Layout in C#, nur Entwickler |
| **pdfme** | **frei (MIT)** | Node auf dem Server **oder** Browser-Erzeugung | JSON-Vorlage, **auch ohne Entwickler** |

**Reihenfolge der Fragen:** Erst klaeren, ob das PDF dasselbe oder ein anderes Dokument ist (siehe
oben) - **das schliesst die Haelfte der Optionen aus, bevor ueber Lizenzen und Laufzeiten geredet
wird.**

## ENTSCHEIDUNGSGRUNDLAGE 2026-09-10: Implementierungsaufwand

**Vorgabe:** Der Aufwand ist das massgebliche Kriterium. **Layoutaenderungen passieren nur
gelegentlich.**

### Damit faellt pdfme aus

Sein staerkstes Argument war der WYSIWYG-Designer mit JSON-Vorlagen - also die Moeglichkeit,
Layouts **ohne Entwickler** zu aendern. Bei gelegentlichen Aenderungen wiegt dieser Vorteil kaum
etwas, waehrend der Preis dauerhaft bleibt: ein Oekosystembruch und entweder eine neue
Node-Laufzeit auf dem Server oder eine Browser-Erzeugung, die die Wege zu Mail und enaio zumauert.
**Dauerhafte Kosten fuer einen gelegentlichen Nutzen - abgelehnt.**

### Aufwandsvergleich der beiden verbleibenden

**Headless Edge - deutlich guenstiger, und die Vorarbeit ist schon geleistet:**
- Das Druck-HTML **existiert bereits** fuer alle drei Dokumente (Teil 3/4/5, `PrintBom`,
  `PrintPicking`).
- Die Vorbereitung dafuer ist **bewusst schon erfolgt**: Teil 4 hat entschieden, CSS eingebettet
  und Bilder als Data-URI zu fuehren - ausdruecklich als Headless-Renderer-Vorbereitung. Genau der
  Stolperstein, der diesen Weg sonst teuer macht, ist damit ausgeraeumt.
- Zu bauen bleibt: ein Dienst, der HTML entgegennimmt, temporaer schreibt, `msedge` aufruft, die
  Bytes liest und aufraeumt. Dazu Timeout, Parallelitaet, Temp-Verzeichnis. **Ueberschaubar** -
  Groessenordnung Tage, nicht Wochen.

**QuestPDF - deutlich teurer:**
- **Jedes** der drei Dokumente muesste in C# **neu gebaut** werden - Tabellen, Kopfzeilen,
  Seitenumbrueche, Positionierung. Das vorhandene HTML traegt nichts bei.
- Dazu die **Lizenzbeschaffung** als eigener Vorgang.
- Sein Nutzen (keine Server-Abhaengigkeit, deterministisch, praezise Seitensteuerung) ist real,
  rechtfertigt aber nicht, drei fertige Layouts ein zweites Mal zu bauen.

### Empfehlung

**Headless Edge als Grundentscheidung** - die urspruengliche Wahl bleibt bestehen, jetzt aber mit
dem Aufwandsargument statt nur mit dem Doppelpflege-Argument.

**Eine Tuer bleibt bewusst offen:** Zeigt sich bei der Corporate-Design-Vorlage fuer den
**Beschichtungsauftrag** (offener Punkt aus Teil 4), dass sich ein formelles Aussendokument mit
Print-CSS nicht sauber setzen laesst - Briefkopf, Seitenzaehlung, wiederholte Tabellenkoepfe,
praezise Positionierung -, dann ist **das** der Moment, QuestPDF fuer **dieses eine Dokument** zu
erwaegen. Mit dann bekanntem Bedarf statt auf Vorrat, und mit einer Lizenzfrage, die sich an einem
konkreten Nutzen bemisst.

**Nicht vorwegnehmen** - erst bauen, was billig ist, und die Ausnahme entscheiden, wenn sie sich
zeigt.

## Bezug

Teil 3, 4 und 5 des IDEAL-Pakets setzen darauf auf \u2014 siehe
[[2026-07-29-standort-ideal-uebersicht]]. Sinnvollerweise **zusammen mit oder direkt nach Teil 3**
umsetzen, weil dort das gemeinsame Druckgeruest entsteht.
