---
typ: feature
---
# IDEAL: Materialisierung um die fachlichen Felder erweitern (leere Spalten in den FA-Listen)

## Befund (2026-08-20, am Code belegt)

Die FA-Liste zeigt im hierarchischen Modus die Gruppierung korrekt (HauptFA 1035235, 39 Sub-FAs,
130 Sub-FAs gesamt) — aber **die meisten Spalten sind leer**: Kunde, Werkbank, Beschicht.,
BG-Termin, Fert.-Termin, Liefertermin, Komm., Lack-T.

Ursache: `FaMaterializationSyncService` schreibt beim Anlegen **genau sieben Felder**:

```
OrderNumber, SubOrderNumber, ParentSubOrderNumber,
Quantity, ArticleNumber, Description1, Description2
```

Beim Update sind es sogar nur vier (`Quantity`, `ArticleNumber`, `Description1/2`) plus der
Eltern-Zeiger. Alles Uebrige bleibt leer.

**Das ist kein Versehen, sondern ein ueberholter Umfang.** Teil 7 hatte als Ziel die
**BDE-Faehigkeit** — dafuer genuegen Identitaet und Menge. Dass die materialisierten Auftraege in
**Listen** erscheinen wuerden, war zu dem Zeitpunkt keine Anforderung; die „dritte Fehlerklasse"
(Anzeige) wurde erst danach entdeckt. Keine der Etappen B–E deckt die Datenbefuellung ab.

## Die leeren Spalten zerfallen in DREI Klassen — mit verschiedenen Loesungen

### K1 — Auftragsdaten aus `FaHierarchyOrderInfo` (je HauptFA)

**Kunde, BG-Termin, Fert.-Termin, Liefertermin, Prio, AB-Nummer, Montage-Abteilung.**

Diese Werte haengen am **HauptFA**, nicht am einzelnen Sub-FA. Sie gehoeren deshalb in die
**Gruppen-Kopfzeile**, nicht in jede Zeile: In 39 Zeilen denselben Kunden und denselben Termin zu
wiederholen ist Redundanz — in der Kopfzeile stehen sie einmal und richtig.

**Nicht nach `ProductionOrders` materialisieren.** Sie dort je Sub-FA zu duplizieren wuerde eine
1:n-Beziehung flachklopfen und beim naechsten Sync-Lauf zur Frage fuehren, welche Kopie stimmt.
Die Gruppen-Kopfzeile liest direkt aus `FaHierarchyOrderInfo` — eigene Abfrage je `HauptFA`, **kein
Fan-out-Join** (paketweite Regel).

**Achtung 1:n:** Zu einem `HauptFA` koennen mehrere `FaHierarchyOrderInfo`-Zeilen gehoeren
(Kombinationsgeraete). Dann gilt die bestehende Regel: alle Kopfvarianten im Klartext, Badge
„mehrdeutig", Log-Eintrag — nicht stillschweigend eine auswaehlen.

### K2 — Zeilendaten aus `FaHierarchyNode` (je Sub-FA), heute nicht uebernommen

**Werkbank/Arbeitsbereich, Beschichtet** — und zu pruefen: `Matchcode`, `Hauptlagerplatz`,
`Artikelgruppe`, `Artikeltyp`, `Material`, `EKBedarf`, `Fertigungmenge`, Masse.

Diese Werte liegen bereits je Zeile in der Struktur-Tabelle und **gehoeren in die
Materialisierung** — ein Feld-fuer-Feld-Mapping, kein neuer Datenweg.

### K3 — WMS-eigene Felder, zu Recht leer

**Komm.-Status, Lack-T** und alles, was erst durch Arbeit im WMS entsteht. Kein Handlungsbedarf.

## Der Knackpunkt: `Workplace` ist app-verwaltet

Der Klassenkommentar von `FaMaterializationSyncService` fuehrt **`Workplace`** ausdruecklich unter
den Feldern, die bei Updates **NIE ueberschrieben** werden — gemeinsam mit `IsDone`,
`PickingStatus`, `BdeStatus`, `Storno`, `ExtraInfo`. Diese Regel ist bewusst gesetzt (Z1) und hat
sich bewaehrt.

Soll die Werkbank kuenftig aus IDEAL kommen, **kollidiert das direkt damit**. Das ist keine
Anzeigefrage, sondern eine **Datenhoheits-Frage**, und sie muss vor der Umsetzung entschieden
werden:

| Variante | Bedeutung |
|---|---|
| **A — nur beim Anlegen** | Sage setzt den Startwert, danach gehoert das Feld dem WMS. Eine spaetere Aenderung in Sage kommt nicht mehr an. |
| **B — bei jedem Lauf** | Sage ist fuehrend. Eine manuelle Zuweisung im WMS wird beim naechsten Sync ueberschrieben — **still**. |
| **C — eigenes Feld** | Sage-Arbeitsbereich getrennt von der WMS-Werkbank; beide sichtbar, keine Kollision. |

*Einschaetzung:* **A oder C.** Variante B bricht die Z1-Regel und erzeugt genau die Klasse stiller
Datenverluste, gegen die das ganze Paket abgesichert wurde — jemand weist eine Werkbank zu, und
eine Viertelstunde spaeter ist sie weg. **Nicht raten, sondern fachlich entscheiden:** Wird die
Werkbank bei IDEAL in Sage gepflegt oder im WMS zugewiesen?

Dieselbe Frage stellt sich fuer jedes weitere Feld aus K2, das eine WMS-Entsprechung hat.

## Fallstricke

**F1 — Update-Pfad muss mitwachsen.** Heute aktualisiert der Sync nur vier Felder. Neue K2-Felder
nur beim **Anlegen** zu setzen und beim **Update** zu vergessen, ergaebe: Bestandsauftraege bleiben
leer, neue sind gefuellt — und niemand versteht, warum manche Zeilen Werte haben und manche nicht.
Beide Pfade zusammen aendern.

**F2 — Die 130 Bestandsauftraege sind bereits angelegt.** Sie bekommen die neuen Felder erst, wenn
der Update-Pfad sie mitzieht (siehe F1). Nach der Umsetzung einen Sync-Lauf abwarten und pruefen —
nicht annehmen, dass die Anzeige sofort stimmt.

**F3 — Z1 gilt unveraendert.** Kein `SetValues`, kein Entitaets-Ersatz. Jedes neue Feld wird
**einzeln** gesetzt, und die app-verwaltete Liste bleibt tabu — sofern nicht ausdruecklich anders
entschieden (siehe Workplace).

**F4 — AKE unveraendert.** Der gesamte Materialisierungs-Pfad laeuft nur bei Master `true`. Bei
`false` aendert sich nichts, auch nicht an `ProductionOrders`.

**F5 — Reihenfolge zu Etappe B.** Diese Arbeit gehoert **nach** dem laufenden Anzeige-Epic. Sonst
bauen die fuenf View-Umbauten gegen ein bewegliches Ziel, und Anzeige- vermischt sich mit
Materialisierungsarbeit in denselben Laeufen. Die K1-Kopfzeilen-Felder beruehren allerdings die
Gruppen-Kopfzeile, die Etappe B gerade baut — **dort Platz vorsehen**, damit sie spaeter nur
gefuellt und nicht umgebaut werden muss.

## Offene Fragen

1. **Werkbank/Arbeitsbereich: Quelle ist der `Arbeitsbereich` aus der FA-Struktur.**
   [TEILWEISE ENTSCHIEDEN 2026-08-20] Die Werkbank soll **automatisch** aus dem
   `Arbeitsbereich`-Feld von `FaHierarchyNode` kommen (Werte wie `K-02`, `S-01`, `H4-04`).

   **Darin stecken ZWEI verschiedene Dinge — beide noch zu entscheiden:**

   **(a) Werkbank-Stammdaten.** Existieren `K-02`, `S-01`, `H4-04` ueberhaupt als Werkbank-Entitaeten
   im WMS? Falls nein, zeigt die Zuweisung ins Leere. „Automatisch anlegen" hiesse dann: der Sync
   legt fehlende Werkbaenke selbst an.
   *Risiko:* Stammdaten aus einer Fremdquelle automatisch zu erzeugen fuellt die Werkbank-Liste mit
   allem, was in Sage steht — auch mit Tippfehlern und Altlasten, und ohne dass jemand es
   entschieden hat. **Alternative:** Der Sync legt **nicht** an, sondern **meldet** unbekannte
   Arbeitsbereiche (Log + Sammelmail wie bei den vermissten FAs); ein Mensch pflegt sie einmal an.
   Bei einer ueberschaubaren, stabilen Zahl von Arbeitsbereichen ist das der sauberere Weg.
   -> **Zu entscheiden: anlegen oder melden?**

   **(b) Datenhoheit bei Updates.** Sage ist damit fuehrend, also **Variante B**. Konsequenz, die
   benannt sein muss: **Eine manuelle Werkbank-Zuweisung an einem IDEAL-Auftrag wird beim naechsten
   Sync-Lauf ueberschrieben** — still, innerhalb einer Viertelstunde.
   Das ist vertretbar, **solange fuer IDEAL-Auftraege niemand manuell zuweist**. Genau das gehoert
   bestaetigt, bevor die Regel gebaut wird.
   *Absicherung:* `Workplace` faellt damit aus der app-verwalteten Liste des
   `FaMaterializationSyncService` heraus — der Klassenkommentar dort ist entsprechend zu
   korrigieren, sonst widersprechen sich Kommentar und Verhalten. Und der Sync **meldet**, wenn er
   einen abweichenden Wert ueberschreibt; so faellt es auf, falls doch jemand manuell zuweist.
2. **Welche K2-Felder sollen ueberhaupt mit?** Die Struktur-Tabelle fuehrt mehr, als die Liste
   zeigt. Nur die Spalten befuellen, die eine Liste tatsaechlich anzeigt, oder alles Verfuegbare
   uebernehmen (Vorrat fuer spaeter)?
3. **K1 in der Kopfzeile bestaetigen:** Kunde und Termine nur in der Gruppen-Kopfzeile — oder
   erwartet jemand sie doch je Zeile (z. B. fuer Export oder Spaltenfilter)?

## Bezug

[[2026-07-29-standort-ideal-teil-7-spec]] (Materialisierung, Z1-Regel app-verwalteter Felder),
[[2026-08-18-fa-liste-hierarchie-anzeige-spec]] (Anzeige-Epic, Gruppen-Kopfzeile),
Anhang [[sage-views-ideal]] (Feldbestand FAListe/FAInfos).
