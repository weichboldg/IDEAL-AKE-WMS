---
typ: feature
---
# Kommissionierliste: Summierung je Artikel + PDF nur gefiltert und nur sichtbare Spalten

**Aufgenommen 2026-09-23.** Betrifft die IDEAL-Kommissionierliste (Teil 3) und deren PDF
([[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]], v1.39).

## Anforderung

1. **Summieren:** Kommt dieselbe Artikelnummer (bzw. derselbe Matchcode) mehrfach vor, werden die
   Mengen zusammengefasst - eine Zeile je Artikel statt einer je Vorkommen.
2. **PDF-Druck** (z. B. Kommissionierliste) beruecksichtigt
   - **nur die gefilterten Artikel** und
   - **nur die angezeigten Spalten**.

## Summieren - drei Fallen, die vorher entschieden sein muessen

**Falle 1 - Summieren darf kein Ziel verschlucken.**
Dasselbe Teil kann in derselben Struktur an **verschiedene Kommissionierziele** gehen. Werden diese
Zeilen zu einer summiert, ist nicht mehr erkennbar, **wohin** wie viel gebracht werden muss - der
Kommissionierer holt die Gesamtmenge und weiss nicht, wie er sie verteilt.
**Zu entscheiden:** Summiert wird je **(Artikel, Kommissionierziel)** - dann bleibt das Ziel erhalten -
oder je **Artikel** allein, mit einer Aufschluesselung der Ziele in der Zeile? *Einschaetzung:* je
(Artikel, Kommissionierziel). Eine Summe, die das Ziel verliert, ist fuer die Kommissionierung
unbrauchbar.

**Falle 2 - nur gleiche Mengeneinheiten summieren.**
2 Stueck und 2 Meter ergeben nicht 4. Fuehren zwei Zeilen desselben Artikels verschiedene Einheiten
(selten, aber moeglich), duerfen sie **nicht** zusammengefasst werden - sie bleiben getrennt und fallen
auf. Summierschluessel damit mindestens **(Artikel, Einheit)**.

**Falle 3 - Artikelnummer ODER Matchcode?**
Der Matchcode ist artikelbezogen und je Artikel identisch (bestaetigt 2026-09-13,
[[2026-09-13-matchcode-artikelstamm-spec]]). Summiert wird daher ueber die **Artikelnummer**; der
Matchcode ist Anzeige. Zwei verschiedene Artikelnummern mit gleichem Matchcode werden **nicht**
zusammengefasst.

## Vorbild im eigenen Haus

Die Vormontage (Teil 5) hat bereits die Ansichten **"Index" und "Summiert"**. Das ist die naheliegende
Vorlage - dasselbe Umschalten zwischen Einzelzeilen und Summe, statt eine zweite Summier-Mechanik zu
erfinden (ponytail, Sprosse 2). **Zu entscheiden:** Wird die Kommissionierliste **immer** summiert
angezeigt, oder gibt es wie bei der Vormontage einen Schalter Einzeln/Summiert? *Einschaetzung:*
Schalter, Standard "Summiert" - die Einzelzeilen braucht man, um nachzuvollziehen, woher eine Summe
kommt.

## PDF - nur gefiltert, nur sichtbare Spalten

**Gefiltert:** Die PDF-Erzeugung traegt die Spaltenfilter laut Spec bereits mit (Query-Parameter
bleiben erhalten, Hinweis "Gefilterte Ansicht" im Kopf). **Am Code und am Testsystem pruefen**, ob das
fuer die Kommissionierliste tatsaechlich greift - die Anforderung deutet darauf hin, dass es das heute
nicht tut.

**Sichtbare Spalten:** Die Stueckliste kennt dafuer bereits einen `visibleColumns`-Parameter
(`PrintBom`). Fuer die Kommissionierliste pruefen, ob es einen Gegenpart gibt - sonst dasselbe Muster
uebernehmen. **Wichtig:** Die sichtbaren Spalten sind die **Benutzer-Spaltenpraeferenzen** (Zahnrad),
also je Anwender verschieden. Das PDF zeigt, was **dieser** Anwender gerade sieht.

**Summierung im PDF:** Druckt jemand die summierte Ansicht, muss das PDF summiert sein - und der Kopf
sollte das ausweisen (analog "Gefilterte Ansicht"), damit ein Ausdruck nicht als Einzelliste gelesen
wird.

**Eine Konsequenz, die benannt gehoert:** Wenn das PDF "was der Anwender sieht" ist, dann sind zwei
PDFs derselben Kommissionierliste je nach Anwender verschieden. Fuer ein internes Arbeitsdokument ist
das richtig. Sollte die Kommissionierliste je an Dritte gehen, waere es das nicht - dann braucht es
ein festes Layout. Heute gilt ersteres.

## Offene Fragen

1. Summierschluessel: je (Artikel, Kommissionierziel) oder je Artikel mit Aufschluesselung?
2. Immer summiert, oder Schalter Einzeln/Summiert wie bei der Vormontage?
3. Greift der Filter-Durchschlag im PDF der Kommissionierliste heute - oder fehlt er?

## Bezug

[[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]],
[[2026-09-13-matchcode-artikelstamm-spec]],
[[2026-09-13-kommissionierung-nur-hauptfa]] (dieselbe Filterlogik wirkt kuenftig auch in der Stueckliste)
