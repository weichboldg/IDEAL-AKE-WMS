---
typ: feature
---
# Materialisierung fachliche Felder: Nachlese aus den Task-Reviews (2026-09-09)

Bewusst **nicht** merge-blockierende Befunde aus der Umsetzung von
[[2026-08-20-materialisierung-fachliche-felder-spec]] (Aufgabe
[[2026-09-09-materialisierung-fachliche-felder-umsetzung]]). Jeder Punkt wurde im Review gesehen,
bewertet und mit Begruendung geparkt. **Einmal gesammelt abarbeiten**, nach dem Buendel-Merge, aus
`main`.

> **Was NICHT hier steht:** Die vier Fehler, die im Lauf tatsaechlich gefunden und **sofort behoben**
> wurden — sie stehen im Changelog. Zwei davon sind bemerkenswert, weil sie nicht beim Programmieren,
> sondern beim **Schreiben von Tests** auffielen: die weggefilterte Blattebene in der Lack-Ableitung
> und die Entprellung, die ein wiederkehrendes Problem nie wieder gemeldet haette.

## A — Kleine Haertungen im Materialisierungs-Sync

1. **Tote Bedingungen nach dem DryRun-Ausstieg.** `RunAsync` verlaesst den DryRun-Fall mit einem
   fruehen `return`; die spaeteren `if (!dryRun)`-Abfragen bei Statuszeilen und Lack-Flag sind ab da
   immer wahr. Harmlos, aber irrefuehrend beim Lesen — entweder entfernen oder als bewusst defensiv
   kennzeichnen.
2. **`RunAsync` ist auf rund 250 Zeilen gewachsen** und deckt Quellaufbau, Plan, Anlegen/Update,
   Werkbank, Statuszeilen, Lack-Flag und Meldungen in einem Rumpf ab. Auffaellig inkonsistent: Der
   Statuszeilen-Block wurde ausgelagert (`EnsureStatusRowsAsync`), der strukturgleiche Lack-Block
   blieb inline. Konkreter Schnitt: `WriteCoatingFlagsAsync(subOrderNumbers, coating, ct) -> Task<int>`
   nach demselben Muster.
3. **Volle Knotentabelle je Lauf.** Seit der Filter-Korrektur (die Lack-Ableitung braucht die
   Blattebene) wird `FaHierarchyNodes` ungefiltert geladen; Blaetter sind in einer Stueckliste die
   Mehrheit. Muster im Projekt nicht neu (`GetAllAsync` laedt ebenfalls voll), Hintergrund-Sync ohne
   Latenzanforderung — daher nicht blockierend. **Beobachtungspunkt fuer die Produktivgroesse.**
   Sauberer Weg, falls noetig: zwei spaltenreduzierte Projektionen — eine breite fuer die
   Materialisierung, eine schlanke `{SubFA, VaterFA, Beschichtet}` fuer die Ableitung.
4. **Kombigeraet-Meldung ohne Laengenbegrenzung.** `string.Join` ueber alle betroffenen HauptFAs;
   bei mehreren hundert wird die Protokollzeile unlesbar. Cap mit „… und N weitere" waere die
   Entsprechung zum bestehenden Muster bei den vermissten FAs.
5. **DryRun-Vorschau zeigt die Kombigeraet-Zahl nicht.** Der Counts-Key `hauptfa_mehrdeutig` fehlt
   im DryRun-Pfad des Struktur-Syncs, obwohl die Daten dort bereits im Speicher stehen. Plan-Vorgabe,
   keine Umsetzer-Abweichung.

## B — Tests nachziehen

6. **Verkabelung der Mail-Entprellung ungetestet.** Der Aufruf steht bewusst **ausserhalb** des
   `if (unbekannte > 0)`-Blocks, damit ein behobener und spaeter wiederkehrender Arbeitsbereich
   erneut gemeldet wird. Die Zustandsklasse selbst ist getestet, die **Platzierung des Aufrufs**
   nicht — ein Zurueckverschieben in den Block wuerde von keinem Test rot gemeldet.
7. **`Aggregate` ohne Startwert** in `BuildCustomerColumnFilterPredicate` wirft bei leerer
   Tokenliste. Heute durch den Guard im Aufrufer gedeckt, die Methode selbst ist aber nicht robust
   (die abgeloeste Fassung war es). Startwert setzen oder Guard hineinziehen.

## C — Fachfragen, keine Codefragen

8. **Was genau ist ein Kombinationsgeraet?** Die Erkennung zaehlt **Zeilen** je HauptFA, nicht
   **verschiedene Montage-Abteilungen**. Zwei Kopfzeilen derselben Abteilung wuerden damit als
   mehrdeutig gelten und die Gruppe still aller Kopfdaten berauben. Ob die Sage-Quelle diesen Fall
   ueberhaupt erzeugen kann, ist eine Frage an den Fachbereich — nicht an den Code.
9. **Nicht indexnutzbarer Vergleich.** `HauptFA.ToString() == OrderNumber` erzeugt ein `CONVERT` im
   `EXISTS` und verhindert die Indexnutzung — auf der meistgenutzten Liste. Bewusst beibehalten
   (im Code kommentiert), weil dasselbe Muster im Freitextpfad ohnehin steht und die Alternative
   den Aufwand nicht aufwiegt. **Bei spuerbarer Laufzeit** ist die Vorab-Ermittlung der Schluessel
   mit `IN`-Liste der naechste Schritt.

## D — Aus der Spec bewusst ausgeklammert

10. **Nur die FA-Liste.** Leitstand, Tracking, Picking, Arbeitsvorrat und FA-Vervollstaendigung
    zeigen Kunde und Termine je Zeile und bleiben fuer IDEAL **leer** — bekannte Einschraenkung
    (Entscheidung B5), kein Versehen. Der Beschichtungstermin erscheint aus demselben Grund nur in
    der FA-Liste; Leitstand und Arbeitsvorrat rechnen dort weiter mit der alten Formel, die fuer
    IDEAL nichts liefert.
11. **Weitere Zeilenfelder** (Matchcode, Hauptlagerplatz, Artikelgruppe, Artikeltyp, Material,
    EK-Bedarf, Fertigungsmenge, Masse) wurden bewusst **nicht** materialisiert — Regel „nur was eine
    Liste tatsaechlich anzeigt". Kommt spaeter eine Anzeige, kommt das Feld mit ihr.

## Bezug

[[2026-08-20-materialisierung-fachliche-felder-spec]],
[[2026-09-09-materialisierung-fachliche-felder-umsetzung]],
[[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]],
[[2026-09-08-bom-bridge-nachlese]] (Vorlaeufer-Liste desselben Buendels).
