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

## Bezug

Teil 3, 4 und 5 des IDEAL-Pakets setzen darauf auf \u2014 siehe
[[2026-07-29-standort-ideal-uebersicht]]. Sinnvollerweise **zusammen mit oder direkt nach Teil 3**
umsetzen, weil dort das gemeinsame Druckgeruest entsteht.
