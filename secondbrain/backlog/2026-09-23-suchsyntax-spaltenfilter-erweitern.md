---
typ: feature
---
# Spaltenfilter: Suchsyntax erweitern (beginnt mit, endet mit, von-bis) - querschnittlich

**Aufgenommen 2026-09-23.** Betrifft **alle** Listen mit Spaltenfilter.

## Anforderung (vom Menschen)

- `7%` - **beginnt mit** 7
- `%7` - **endet mit** 7
- `40000...70000` - **von bis**

## Ist-Stand - am Code geprueft (2026-09-23)

Die heutige Mini-Syntax kennt genau zwei Operatoren: **`,` fuer ODER** und **`!` fuer NICHT**. Alles
andere ist **"enthaelt"**, ohne Beachtung von Gross-/Kleinschreibung.

**Der entscheidende Befund: Die Syntax ist an DREI Stellen umgesetzt.**

> **Nachtrag 2026-09-25 — es sind VIER:** Die Stueckliste hat mit `bomMatchesFilter` in `Bom.cshtml` eine
> **eigene** Umsetzung der Filtersyntax (gefunden bei der Spec
> [[2026-09-25-kommissionierung-nur-hauptfa-spec]]). Sie steuert Sichtbarkeit, Vorfahren-Aufklappen und
> Leerzustand des Baums und **ueberschreibt** danach das Ergebnis von `table-filter.js`. Ab jener Spec
> kennt sie zudem den Sonderwert `!(leer)`. **Fuer Etappe B mitzaehlen** — und die Erhebung sollte gezielt
> nach weiteren solchen Einzelumsetzungen in Views suchen, nicht nur nach `EF.Functions.Like`.
1. im Browser - `table-filter.js`
2. serverseitig im Speicher - `ColumnFilterHelper.Apply`
3. serverseitig in SQL - und zwar **von jeder Liste selbst**: Jeder Controller bzw. jedes Repository
   uebersetzt die Tokens eigenhaendig, typischerweise als `EF.Functions.Like(o.Feld, $"%{t}%")`.

**Folge:** Wer die Syntax an einer Stelle erweitert, bekommt Listen, die sich je nach Filterweg
verschieden verhalten. `7%` wirkte dann in einer client-gefilterten Liste als "beginnt mit" und in einer
server-gefilterten als "enthaelt". Genau diese Art Inkonsistenz ist schwer zu finden, weil jede Liste
fuer sich plausibel aussieht.

## Vorschlaege - zur Anforderung hinzu

**Empfohlen, mit Begruendung aus den eigenen Daten:**

1. **`=KA` - genau gleich.** Der wichtigste Zusatz. "Enthaelt" macht kurze Codes mehrdeutig: `KA` findet
   auch `KA2`, `SW` findet `SWL`, `SW1`, `SW2`, die Nummer `2300` findet `23001`. Mit den
   Arbeitsschritt-Kuerzeln und Arbeitsplatznummern aus den letzten Specs ist das kein theoretisches
   Problem mehr.
2. **`>`, `<`, `>=`, `<=`** - fuer Zahlen und Datum. `>=01.10.2026` fuer "ab Oktober", `>5` fuer Mengen.
3. **Leer / nicht leer** - etwa `(leer)` und `!(leer)`. Macht Luecken **auffindbar**: "Artikel ohne
   Matchcode" (bei AKE bis zur View-Erweiterung), "Werkbaenke ohne Kuerzel", "Sub-FAs ohne Werkbank".
   Heute laesst sich nach einem leeren Feld gar nicht filtern.
4. **Syntax-Hilfe am Filterfeld** - ein kleines `?` oder ein Tooltip mit den Operatoren. Ohne das
   entdeckt niemand, dass `7%` oder `...` existieren; die Erweiterung bliebe ungenutzt.

**Zusammenspiel mit dem Bestehenden** (bleibt voll gueltig):
- `7%,8%` - beginnt mit 7 **oder** mit 8
- `!7%` - beginnt **nicht** mit 7
- `=KA,=SW` - genau KA **oder** genau SW

**Optional, eher nicht jetzt:**
- `*` als gleichwertige Alternative zu `%` - Windows-Anwender kennen `*`, Sage-Anwender eher `%`.
- relative Datumsangaben (`heute`, `KW40`) - nett, aber eigener Umfang.

## Pflichten und Fallen fuer den Entwurf

**P1 - EIN Parser, EINE Bedeutung, drei Verbraucher.**
`ColumnFilterHelper.Parse` liefert kuenftig nicht nur Texte, sondern **strukturierte Tokens** (Art:
enthaelt / beginnt / endet / genau / Bereich / Vergleich / leer; Werte; Negation). Daraus:
- **ein** zentraler SQL-Ausdrucksbauer, den **alle** Listen verwenden - statt der handgeschriebenen
  `Like`-Uebersetzungen je Liste;
- **ein** Speicher-Auswerter (`Apply`);
- **ein** JS-Spiegel in `table-filter.js`.
**Gemeinsame Testtabelle:** dieselben Eingaben, dieselben erwarteten Treffer - als C#-Test **und**
JS-Test. Nur so bleibt die Gleichheit ueber die drei Wege belegt, statt behauptet.

**P2 - LIKE-Escaping - ein vorbestehender Fehler.**
Heute geht die Eingabe **unmaskiert** in `LIKE`. Ein vom Anwender getipptes `_` oder `%` wirkt bereits
jetzt als SQL-Platzhalter: `A_1` findet auch `AX1`. Mit der neuen Syntax ist `%` nur am Anfang oder Ende
gewollt; alles andere muss maskiert werden (`[%]`, `[_]`, `[[]`). Das behebt nebenbei einen Fehler,
den es heute schon gibt.

**P3 - "von bis" haengt am Spaltentyp.**
- **Zahlenspalten** (Menge, Stueck): numerischer Vergleich.
- **Datumsspalten** (Termine): Datumsvergleich, deutsches Format `01.09.2026...30.09.2026`.
- **Textspalten** (Artikelnummer, FA-Nr.): Ein **lexikalischer** Vergleich waere **falsch** - `"5"` liegt
  lexikalisch zwischen `"40000"` und `"70000"`. Und Artikelnummern wie `50000356-1` sind gar keine Zahlen.
  -> **Zu entscheiden:** Bereich auf Textspalten nur fuer Werte, die vollstaendig numerisch sind
  (`TRY_CAST`), andere fallen heraus - oder auf Textspalten gar nicht erlaubt, mit Hinweis?
Der Spaltentyp muss aus den `ColumnDefinitions` kommen. Datumsspalten sind dort bereits bekannt (die
Filterzeile zeigt fuer sie das Kalender-Symbol); ob Zahlenspalten gekennzeichnet sind, ist zu pruefen.

**P4 - Gespeicherte Standardfilter koennen ihre Bedeutung aendern.**
Die Benutzer-Standardfilter (`DefaultFilter*` am Benutzer) nutzen dieselbe Syntax. Ein gespeicherter
Wert, der heute "enthaelt" bedeutet, koennte danach etwas anderes bedeuten - ein gespeichertes `7%` ist
heute zufaellig "enthaelt 7" (weil `%` unmaskiert als Platzhalter wirkt), danach "beginnt mit 7". Vor der
Umstellung die gespeicherten Werte einmal durchsehen. Geringes Risiko, aber benannt.

**P5 - Gleiche Treffer, egal welcher Weg.** Dieselbe Eingabe liefert in einer client- und einer
server-gefilterten Liste **dieselben** Treffer. Als Akzeptanzkriterium.

**P6 - Laufzeit.** "Beginnt mit" kann einen Index nutzen, "endet mit" und "enthaelt" nicht. Fuer die
heutigen Listen unkritisch; bei der Artikeltabelle mit 108.818 Zeilen gelten die bereits beschlossenen
Regeln (mindestens drei Zeichen, Entprellung).

## Zuschnitt - vermutlich ein Epic

Der Umfang ist nicht die Syntax, sondern das **Einsammeln** der verstreuten SQL-Uebersetzungen.
Vorschlag:
- **Etappe A:** Parser, SQL-Ausdrucksbauer, Speicher-Auswerter, JS-Spiegel, gemeinsame Testtabelle -
  und **eine** Referenzliste (die FA-Liste) umgestellt. STOPP.
- **Etappe B:** alle uebrigen Listen von ihren handgeschriebenen `Like`-Uebersetzungen auf den zentralen
  Bauer umstellen. Vorher **Erhebung**, wie viele Stellen es sind.
Dasselbe Muster wie die Anzeige-Etappen A/B im August: erst eine Referenz, ansehen, dann uebertragen.

## Offene Fragen

1. Bereich auf Textspalten: nur rein numerische Werte, oder dort gar nicht erlaubt?
2. `*` zusaetzlich zu `%`?
3. Schreibweise fuer "leer": `(leer)` oder etwas anderes?
4. Werden die Vorschlaege 1-4 uebernommen, oder nur die drei angeforderten Operatoren?

## Bezug

`IdealAkeWms/Services/ColumnFilterHelper.cs`, `wwwroot/js/table-filter.js`,
[[2026-08-12-listen-spaltenauswahl-spec]] (Spaltenpraeferenzen),
[[2026-09-18-stueckliste-kommissionierziel-filter-spec]] (gespeicherte Standardfilter mit Mini-Syntax),
[[2026-09-13-matchcode-artikelstamm-spec]] (Artikelsuche, 108.818 Zeilen)
