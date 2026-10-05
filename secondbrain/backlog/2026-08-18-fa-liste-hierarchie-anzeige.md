---
typ: feature
---
# FA-Liste und verwandte Ansichten hierarchiefaehig darstellen

## Worum geht es — die dritte Fehlerklasse

Nach der Materialisierung (Teil 7) stehen die IDEAL-Auftraege als echte `ProductionOrders` im
System — beim ersten Lauf 130 Stueck. Damit erscheinen sie in FA-Liste, Leitstand, Arbeitsvorrat,
Fertigmeldung, Tracking und Picking. **Technisch funktioniert das; brauchbar ist es nicht.**

Dieselbe FA-Nummer erscheint mehrfach — einmal je Sub-FA — ohne dass die Zeilen unterscheidbar
waeren. `SubOrderNumber` steht nirgends, der Elternzeiger auch nicht, und eine Suche nach der
FA-Nummer liefert die ganze Gruppe.

Das ist eine **dritte Fehlerklasse**, die bisher keine Spec abdeckt:

| | Klasse | Symptom | Stand |
|---|---|---|---|
| 1 | mehrdeutige `OrderNumber`-Lookups | liefert zu viel | behoben (Teil 7, Etappe D) |
| 2 | hart verdrahtete AKE-Views | wirft (HTTP 500) | Guard drin, Volloesung offen |
| **3** | **Anzeige kennt die Hierarchie nicht** | **funktioniert, ist aber unbrauchbar** | **diese Notiz** |

Teil 7 hat die betroffenen Controller angefasst — aber ausdruecklich **nur fuer die
Lookup-Haertung**. Die Spec stellt Anzeige und Listen explizit out of scope. Das war damals
richtig: ohne Materialisierung gab es nichts anzuzeigen.

## Betroffene Ansichten

**Die Kandidatenliste steht bereits** — es sind die Controller aus dem Etappe-D-Sweep von Teil 7:
`ProductionOrders` (FA-Liste), `PickingLeitstand`, `FaWorklist`, `FaCompletion`, `Tracking`,
`Picking`, dazu das BDE-Cockpit aus Teil 8. Was fuer Lookups durchgegangen wurde, muss jetzt fuer
die Darstellung durchgegangen werden.

**Aber nicht alle sind Listen im ADR-0005-Sinn.** Leitstand und Cockpit haben eigene Layouts — dort
ist je Ansicht zu pruefen, ob Gruppierung ueberhaupt passt, statt sie pauschal aufzuzwingen.

## Entschieden (2026-08-18)

1. **Zwei Anzeige-Ebenen, nicht die volle Struktur.** Gruppen-Kopfzeile = `HauptFA`, darunter die
   Sub-FAs als flache Zeilen. **Keine** rekursive Baumdarstellung, keine Einrueckung nach Tiefe.
   *Begruendung:* Die FA-Liste ist eine **Arbeitsliste**, kein Struktur-Browser — den gibt es unter
   `/FaHierarchy`, und dort gehoert die Tiefe hin. Zwei Ebenen sind zudem exakt das Muster, das in
   den drei IDEAL-Listen bereits dreimal gebaut wurde: `<tbody>` je Gruppe plus Kopfzeile, kein
   neuer Mechanismus.

   **ABER — entscheidende Praezisierung: ALLE Sub-FAs erscheinen als Zeilen, unabhaengig von ihrer
   Tiefe.** „Zwei Ebenen" betrifft die **Darstellung**, nicht den Umfang. Wuerde man nur die
   direkten Kinder zeigen, verschwaenden Enkel und tiefere Ebenen vollstaendig aus der Liste —
   Auftraege, die man weder sieht noch bearbeiten kann, obwohl sie existieren und rueckmeldefaehig
   sind. Das waere ein stiller Datenverlust in der Oberflaeche.
   Die Eltern-Beziehung geht dabei nicht verloren: Sie steht in der Spalte `ParentSubOrderNumber`
   (`defaultHidden`, bei Bedarf einblendbar) und vollstaendig in der Baumansicht.

   *Nebengewinn:* Damit entfaellt jede rekursive Render-Logik in der FA-Liste — deutlich weniger
   Aufwand und weniger Fehlerflaeche als der Tree-Table-Umbau der Baumansicht.

2. **Standardsicht: Gruppen aufgeklappt.** Im Zwei-Ebenen-Modell gibt es keine „tieferen Ebenen"
   mehr, die man zuklappen koennte — die fruehere Formulierung „erste Ebene offen, tiefere zu"
   loest sich damit auf. Eine Arbeitsliste soll die Auftraege zeigen, nicht erst nach einem Klick.
   Zuklappen bleibt je Gruppe moeglich (und die Suche klappt bei einem Treffer automatisch auf,
   siehe Punkt 3).
3. **Suche findet beides — und findet auch, was zugeklappt ist.** Erst gegen `SubOrderNumber`
   (Treffer → genau diese Zeile), sonst gegen `OrderNumber` (Treffer → die Gruppe).
   **Entscheidend:** Trifft die Suche einen Sub-FA in einer **zugeklappten** Gruppe, wird die Gruppe
   **automatisch aufgeklappt** und der Treffer hervorgehoben. Sonst sucht der Anwender eine Nummer,
   bekommt scheinbar nichts, und haelt den Auftrag fuer nicht vorhanden — der Fehler waere still.
4. **Spalten: `defaultHidden` im flachen Modus, sichtbar im hierarchischen** — und **so weit wie
   moeglich auf bestehende Spalten mappen** statt neue anzulegen (siehe naechster Abschnitt).
5. **`SageMissingSince`-Anzeige: pruefen, nicht annehmen.** Der AKE-seitige Mechanismus existiert;
   fuer IDEAL wurde er noch nicht beobachtet. Wahrscheinlichste Erklaerung: Der erste
   Materialisierungs-Lauf meldete **„0 vermisst"** — es gab schlicht keinen Fall. Vor dem Bauen
   pruefen, ob AK 6 aus Teil 7 bereits erfuellt ist; falls ja, ist hier nichts zu tun, falls nein,
   ist es eine **Nachbesserung an Teil 7**, kein neuer Umfang.

## Spalten-Mapping statt Spalten-Wildwuchs

Deine Vorgabe: die IDEAL-Informationen **auf die bereits vorhandenen Spalten mappen**, nicht fuer
jedes Feld eine neue anlegen. Richtig — sonst waechst die Liste um vier Spalten, von denen im
flachen Modus alle leer sind.

**Die Leitregel: Eine Spalte behaelt ihre BEDEUTUNG, auch wenn sich die Quelle aendert.**

| Spalte | flach (AKE) | hierarchisch (IDEAL) | zulaessig? |
|---|---|---|---|
| FA-Nummer (Zeile) | `OrderNumber` | `SubOrderNumber` | **ja** — beides ist „die Nummer dieses Auftrags"; in AKE sind sie ohnehin gleich |
| Gruppen-Kopfzeile | entfaellt | `OrderNumber` (= HauptFA) | **ja** — neue Ebene, keine Umdeutung |
| Bezeichnung/Matchcode | wie bisher | wie bisher | ja |
| Elternzeiger | — | `ParentSubOrderNumber` | **eigene Spalte**, `defaultHidden` |
| „nicht mehr in Sage" | bestehender Mechanismus | `SageMissingSince` | erst pruefen (Punkt 5) |

**Wo NICHT gemappt werden darf:** Wenn eine Spalte im hierarchischen Modus etwas fachlich anderes
bedeutete als im flachen, bekommt sie eine eigene Spalte. Eine Spalte, die je nach Schalter etwas
anderes meint, ist eine Falle — spaetestens wenn jemand die Liste exportiert, ausdruckt oder eine
Nummer weitergibt.

**Deshalb Pflicht:** Das Mapping wird in der Spec **Feld fuer Feld dokumentiert**, mit Begruendung
je Zeile — nicht „sinngemaess uebernehmen".

## Fallstricke

**F1 — Gruppierung nur im hierarchischen Modus einschalten.** Im flachen Modus gilt
`OrderNumber == SubOrderNumber`; eine Gruppierung erzeugte dort N Gruppen mit je einer Zeile —
reiner Overhead, plus eine Kopfzeile ueber jeder einzelnen Zeile. Die Gruppierung haengt am
Master-Schalter, nicht an der Ansicht.

**F2 — Suche in zugeklappten Gruppen (siehe Entscheidung 3).** Der gefaehrlichste Fall, weil er
**still** ist: keine Fehlermeldung, nur ein leeres Ergebnis. Als eigenes Akzeptanzkriterium und
eigenes Testszenario verankern, nicht als Nebensatz.

**F3 — Paginierung ueber Gruppen, nicht ueber Zeilen.** Wie in allen anderen Listen: Eine Gruppe
wird nie ueber Seiten getrennt, `TotalCount` zaehlt Gruppen. Sonst entstehen Kopfzeilen ohne Zeilen
und abgeschnittene Strukturen.

**F4 — Aktionen: auf welcher Ebene? (OFFEN, siehe unten.)** Die FA-Liste traegt Aktionen
(Details, Kommissionieren, Fertigmelden). Auf einer Gruppen-Kopfzeile bedeuten sie etwas anderes
als auf einer Sub-FA-Zeile. Wird das nicht entschieden, raet der Dev-Lauf — und im
schlimmsten Fall meldet jemand den falschen Auftrag fertig.

**F5 — KORREKTUR (2026-08-18, am Code geprueft): Der Leitstand IST eine regulaere ADR-0005-Liste.**
Meine urspruengliche Behauptung, Leitstand und Cockpit haetten „eigene Layouts", war nur zur Haelfte
richtig. `PickingLeitstand` hat ein `<tbody>`, Server-Spaltenfilter und Pagination — es bekommt
dieselbe Gruppierung wie die uebrigen Listen, ohne Sonderweg.
**Die Ausnahme ist allein das BDE-Cockpit:** ein JS-Karten-Grid ohne Tabelle. Dort passt weder
Gruppierung noch Spaltenmechanik; es braucht eine eigene Ueberlegung oder bleibt bewusst
ausserhalb des Umfangs.

**F6 — AKE-Regression.** Bei Master `false` verhaelt sich jede Ansicht **bit-identisch** zu heute:
keine Gruppierung, keine neuen Spalten sichtbar, unveraenderte Suche. Harte Akzeptanzbedingung wie
in allen Teilen.

## Aktionen: Ebene und Kaskade [ENTSCHIEDEN 2026-08-18]

**Grundregel:**
- **Fertigmeldung am Haupt-FA** → alle zugehoerigen Sub-FAs werden **ebenfalls** fertig.
- **Fertigmeldung an einem Sub-FA** → wirkt **nur** auf diesen einen.

Damit tragen Gruppen-Kopfzeile und Sub-FA-Zeile jeweils eigene Aktionen — nicht dieselbe Aktion an
zwei Stellen.

### Was daraus folgt und mitspezifiziert werden muss

**A — „Alle Sub-FAs" heisst ALLE NACHFAHREN, nicht nur die direkten Kinder.**
Die Struktur ist mehrstufig (Befund B1). Kaskadierte man nur eine Ebene tief, bliebe bei einer
dreistufigen Struktur die unterste Ebene offen — der Haupt-FA waere fertig, ein Enkel noch nicht.
Die Kaskade laeuft ueber den gesamten Unterbaum.

**B — Die Kaskade ist eine Massenaktion und braucht eine Bestaetigung mit Zahl.**
Ein Klick schliesst potenziell zwanzig Auftraege. Vorgabe: Bestaetigungsdialog, der die **Anzahl
betroffener Sub-FAs** nennt — und **gesondert ausweist, wie viele davon noch offene Arbeitsgaenge
oder Rueckmeldungen haben**. Nicht blockieren, aber sichtbar machen: Wer einen Auftrag schliesst,
an dem noch gebucht wird, soll das wissen und nicht erst hinterher merken.

**C — Die Kaskade ist atomar.** Entweder alle Sub-FAs des Unterbaums werden fertig oder keiner —
eine Transaktion. Ein Abbruch mitten in der Kaskade hinterliesse eine Struktur, deren Zustand
niemand mehr erklaeren kann.

**D — Im Audit als EINE Kaskade erkennbar, nicht als zwanzig Einzelaktionen.** Sonst sieht es
Monate spaeter aus, als haette jemand zwanzigmal geklickt — und niemand versteht mehr, was
tatsaechlich passiert ist.

### Ruecknahme: kaskadiert NICHT [ENTSCHEIDUNG 2026-08-18]

Falls sich eine Fertigmeldung zuruecknehmen laesst, ist die Kaskade **bewusst asymmetrisch**:

> Beim **Setzen** ist sie eindeutig (Haupt fertig ⇒ alles fertig).
> Beim **Zuruecknehmen** waere sie es nicht: Oeffnete man am Haupt-FA alle Sub-FAs, gingen die
> **eigenstaendigen** Fertigmeldungen einzelner Sub-FAs verloren — die Information, dass Sub-FA 3
> bereits vorher fertig war, waere weg.

**Verbindlich: Die Ruecknahme wirkt nur auf den Auftrag, an dem sie ausgeloest wird.** Wer den
gesamten Unterbaum wieder oeffnen will, tut das bewusst je Sub-FA. Umstaendlicher — aber sie
zerstoert keine Zustaende, die jemand einzeln gesetzt hat.

Asymmetrie in der Oberflaeche **benennen**, nicht verstecken: Der Hinweis am Ruecknahme-Dialog des
Haupt-FA sagt, dass die Sub-FAs fertig bleiben. Sonst erwartet der Anwender die Umkehrung der
Kaskade und haelt das Ergebnis fuer einen Fehler.

**Falls es gar keine Ruecknahme gibt**, entfaellt dieser Abschnitt — dann im Dev-Lauf ersatzlos
streichen statt eine Funktion zu erfinden.

## Bezug

[[2026-07-29-standort-ideal-teil-7-spec]] (Materialisierung, Etappe-D-Sweep als Kandidatenliste),
[[2026-07-29-standort-ideal-teil-8-spec]] (Auswahleinheit Sub-FA),
[[2026-08-12-listen-spaltenauswahl-spec]] (Spaltenmechanik, `defaultHidden`),
[[2026-08-12-fa-struktur-darstellung-spec]] (Gruppierung, Auto-Expand bei Filtertreffer).
