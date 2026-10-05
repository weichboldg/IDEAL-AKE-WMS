---
typ: feature
---
# IDEAL: Kopfdaten und Matchcode auf die uebrigen fuenf Ansichten replizieren

**Vorbedingung: [[2026-09-10-fa-liste-ausbau-matchcode-spec]] ist umgesetzt und vom Menschen am
Testsystem gesichtet.** Erst Referenz, dann replizieren — dasselbe Muster, das in diesem Paket
zweimal getragen hat (Etappe A → B, und Teil 3 als Referenz fuer Teil 4/5).

## Worum es geht

Die FA-Liste (`/ProductionOrders`) bekommt mit dem Ausbau: Kopfdaten **je Zeile** verfuegbar
(Kunde, Termine, Prio, AB-Nummer, Montage-Abteilung), den HauptFA als **normale Zeile**, die
Freigabe-Kaskade und die **Matchcode-Spalte**.

**Die uebrigen fuenf Ansichten haben das nicht** — sie zeigen Kunde und Termine je Zeile, und die
sind fuer IDEAL leer:
`PickingLeitstand`, `Tracking/Index`, `Picking`, `FaWorklist`, `FaCompletion`.

## Warum die Replikation deutlich billiger wird als der Ausbau selbst

Der entscheidende Gewinn steckt in Punkt 1 des Ausbaus: **Die Kopfdaten sind dann je Zeile
verfuegbar.** Damit erben die fuenf Ansichten eine **Zeilenquelle** statt einer
Kopfzeilen-Sonderloesung — sie muessen die Werte nicht aus der Gruppe holen, sondern lesen sie wie
jede andere Spalte.
Dasselbe gilt fuer den **Matchcode**: Er sitzt nach dem Ausbau an `ProductionOrder`, ist also fuer
jede Ansicht, die daraus liest, ohne Zusatzarbeit da — es bleibt Spalte, `ColumnDefinitions` und
`#column-config` je View.

## Umfang je Ansicht — NICHT uniform

Die fuenf sind unterschiedlich nah an der Referenz. Das ist beim Schnitt zu beachten und war schon
in Etappe B der Fall:

| Ansicht | Zeilen-Entitaet | Naehe |
|---|---|---|
| `PickingLeitstand` | `ProductionOrder` | sehr nah — gleiche Quelle, Kaskade sitzt ohnehin dort |
| `FaCompletion` | `ProductionOrder` | nah |
| `Picking` | Picking-Zeilen | fremd — eigene Quelle |
| `FaWorklist` | Arbeitsschritte | fremd |
| `Tracking/Index` | `WorkOperation`, **dreistufig** | am weitesten weg |

**Vorschlag:** dieselbe Reihenfolge wie in Etappe B — die beiden nahen zuerst, dann die drei
fremden. Und wie dort: **nach jeder Ansicht anhalten**, solange sich das Muster noch nicht als
tragfaehig erwiesen hat. Zeigt sich nach zwei Ansichten, dass es sauber traegt, kann der Rest
gebuendelt werden — die Vorsicht ist am Anfang am meisten wert.

## Vor der Spezifizierung zu klaeren

1. **Kopfzeile auch in den fuenf?** Die FA-Liste behaelt eine schlanke Kopfzeile mit Kunde und
   Leittermin, weil sie bei zugeklappter Gruppe das Einzige ist, was sichtbar bleibt. Gilt dasselbe
   ueberall — oder reicht dort die Zeilendarstellung?
   *Einschaetzung:* dasselbe Argument gilt ueberall, wo man zuklappen kann. Also ja.
2. **Freigabe-Kaskade nur am Leitstand?** Sie sitzt dort, weil dort der `IsDoneBde`-Toggle und
   `BulkRelease` leben. In den uebrigen vier hat sie vermutlich nichts verloren — zu bestaetigen.
3. **`Tracking/Index` ist dreistufig.** Wo landen die Kopfdaten — auf der HauptFA-Ebene, der
   Sub-FA-Ebene oder beiden? Die Ansicht hat bereits Fortschrittsbalken auf beiden Ebenen; sie
   zusaetzlich mit Kunde und vier Terminen zu fuellen kann sie ueberladen.
4. **Matchcode in `Picking` und `FaWorklist`:** Diese Ansichten lesen **nicht** aus
   `ProductionOrder`, sondern aus Picking-Zeilen bzw. Arbeitsschritten. Ob der Matchcode dort ohne
   Zusatzarbeit verfuegbar ist, muss am Code geprueft werden — nicht annehmen.

## Bezug

[[2026-09-10-fa-liste-ausbau-matchcode-spec]] (Referenz) ·
[[2026-08-18-fa-liste-hierarchie-anzeige-spec]] (Etappe B, dieselben fuenf Ansichten) ·
[[2026-08-20-materialisierung-fachliche-felder-spec]] (K1/K2-Quellen)
