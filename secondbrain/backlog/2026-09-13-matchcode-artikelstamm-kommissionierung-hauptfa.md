---
typ: feature
---
# Matchcode in den Artikelstammdaten

**Dringend (2026-09-13).** Der Matchcode soll in den **Artikelstammdaten** gefuehrt und **ueberall
suchbar** sein.

> **Hinweis:** Das urspruenglich hier mitgefuehrte Thema *„Kommissionierung nur auf HauptFA"* ist
> ausgelagert nach [[2026-09-13-kommissionierung-nur-hauptfa]] — beide Themen haben ihren eigenen
> Takt, der Matchcode laeuft zuerst.

## Thema 1 — Matchcode in den Artikelstammdaten [VORRANG]

Der Matchcode soll in den **Artikelstammdaten** gefuehrt werden.
Beispielwert: `G-KT-FRR-760-1500--1320B-180-V01`

### ZUSATZANFORDERUNG: Der Matchcode muss SUCHBAR sein — ueberall

**In jeder Suche, die heute Artikelnummer oder Bezeichnung findet, muss auch der Matchcode
gefunden werden** — Artikelinfo, Artikelstammliste, Bestandsansichten, Fehlteile, Bestellungen,
und die Artikel-Suchfelder der Listen.

**Damit ist die Architekturfrage entschieden, nicht mehr offen:** Suchbarkeit ueber alle Ansichten
funktioniert nur, wenn das Kennzeichen **am Artikel** haengt. Eine Kopie je `ProductionOrder` waere
fuer eine Artikelsuche unsichtbar — wer in der Artikelinfo nach `FRR-760` sucht, wuerde nichts
finden, obwohl der Wert im System steht.
→ **Variante (a)/(c): der Artikelstamm ist der Ort. Die geplante `ProductionOrder.Matchcode`-Spalte
wird damit ueberfluessig** (siehe Konflikt unten).

**Zwei Dinge, die aus der Suchbarkeit folgen und spezifiziert gehoeren:**

**Teilstring-Suche, nicht Exakttreffer.** Niemand tippt `G-KT-FRR-760-1500--1320B-180-V01` ab.
Gesucht wird nach Bruchstuecken: `FRR-760`, `1320B`, `V01`. Also `LIKE '%...%'`.
**Folge, die gemessen gehoert:** Eine fuehrende Wildcard kann keinen Index nutzen. Bei einem
Artikelstamm in der Groessenordnung mehrerer tausend Zeilen ist das vermutlich unkritisch — aber
„vermutlich" hat in diesem Paket schon mehrfach nicht gereicht. **Vor der Umsetzung die Anzahl
Artikel ermitteln und die Suche einmal messen**, statt den Index zu raten.

**ERHEBUNG statt Vermutung — gehoert in den Umfang:** Welche Suchen gibt es heute, die auf
Artikelnummer oder Bezeichnung gehen? Dieselbe Form wie die Z4-Zaehl-Erhebung: erst suchen, dann
entscheiden. Die oben genannten sind der Anfang, nicht die Liste.

### Warum der Artikelstamm ohnehin der richtige Ort ist

Der Wert sieht aus wie ein **Typenschluessel** — er beschreibt einen **Artikel**, nicht einen
Auftrag. Damit gehoert er fachlich an `Article`, nicht an `ProductionOrder`.

Vorteile gegenueber der bisher geplanten Loesung:
- **Eine Quelle statt einer je Auftrag.** Heute plant
  [[2026-09-10-fa-liste-ausbau-matchcode-spec]] eine `Matchcode`-Spalte an `ProductionOrder`,
  befuellt aus `FaHierarchyNode`. Dasselbe Kennzeichen laege dann in jeder Auftragszeile erneut.
- **Jede Ansicht, die Artikel kennt, bekommt ihn ohne Zusatzarbeit** — also auch Artikelinfo,
  Bestandslisten, Fehlteile, Bestellungen. Genau die Erhebung „wo steht heute eine
  Artikelbezeichnung" wird damit weitgehend gegenstandslos.
- **Kein Abgleichproblem.** Aendert sich der Matchcode im Artikelstamm, ist er ueberall neu — statt
  in N materialisierten Auftragszeilen nachgezogen werden zu muessen.

### KONFLIKT, der aufzuloesen ist [WICHTIG]

[[2026-09-10-fa-liste-ausbau-matchcode-spec]] ist **freigegeben** und plant die Spalte an
`ProductionOrder` samt Migration. Beide Wege gleichzeitig zu bauen hiesse, dasselbe Kennzeichen an
zwei Orten zu fuehren — genau die Doppelpflege, die wir an anderen Stellen vermieden haben.

**Zu entscheiden:**
- **(a) Artikelstamm ERSETZT die ProductionOrder-Spalte.** Sauberer, aber die Listen muessen dann
  `Article` **joinen**. Zu pruefen, ob sie das heute schon tun — wenn nicht, ist es ein Join mehr
  je Liste, mit Blick auf die Laufzeit bei 130+ Zeilen.
- **(b) Beide.** Artikelstamm als Wahrheit, `ProductionOrder.Matchcode` als **denormalisierte
  Kopie** fuer die Listen (beim Materialisieren mitgeschrieben). Schneller, aber zwei Orte — dann
  muss festgelegt sein, dass der Artikelstamm fuehrt und die Kopie nur nachzieht.
- **(c) Nur Artikelstamm, ProductionOrder-Spalte aus der freigegebenen Spec streichen.**

*Einschaetzung:* **(a) oder (c)** — **durch die Suchbarkeits-Anforderung oben faktisch entschieden.**
Ein Kennzeichen, ein Ort. (b) waere nur zu erwaegen, wenn die Messung zeigt, dass der Join die
Listen spuerbar bremst — und selbst dann bliebe der Artikelstamm die Wahrheit, die Kopie nur
Anzeige-Beschleunigung.

### Offene Fragen zu Thema 1

1. **Quelle:** Liefert der Sage-Artikelstamm den Matchcode bereits, oder muss die Artikel-View
   erweitert werden (wie fuer die AKE-Kommissionier-View bereits zugesagt)?
2. **Gilt fuer beide Standorte?** Bei IDEAL kommt er aus `FaHierarchyNode.Matchcode`; im
   Artikelstamm braucht es eine artikelbezogene Quelle.
3. **Ist `G-KT-FRR-760-1500--1320B-180-V01` wirklich artikelbezogen?** → **BESTAETIGT (2026-09-13):
   Ja, fuer denselben Artikel in jedem Auftrag gleich.** Damit traegt das Konzept — der Matchcode
   gehoert an `Article`, und die Suchbarkeit ueber alle Ansichten ist herstellbar.
   *Diese Frage war die tragende:* Haette er je Auftrag variieren koennen, waere eine
   artikelbezogene Suche nicht moeglich gewesen und das ganze Vorhaben haette zurueck an den
   Auftrag gemusst.
4. Verhaeltnis zur bestehenden `Article`-Bezeichnung: eigene Spalte, oder ersetzt er ein Feld?
4b. **QR-Scan in der Artikelinfo:** Kann der gescannte Code auch ein Matchcode sein, oder traegt er
   immer die Artikelnummer? Falls beides moeglich ist, braucht die Aufloesung dieselbe
   Vorrang-Reihenfolge wie beim FA-Scan (erst das Eindeutige, dann das Mehrdeutige).

## Thema 2 — ausgelagert

*„Kommissionierung nur auf HauptFA"* ist eine eigene Notiz:
**[[2026-09-13-kommissionierung-nur-hauptfa]]**. Dort stehen die Auswirkungen auf die
Freigabe-Kaskade, auf B-0/Ruling 4 und die Abgrenzung zur BDE-Rueckmeldung.

## Reihenfolge / Auswirkung auf laufende Arbeit

**Diese Notiz (Matchcode) laeuft zuerst.** Sie beruehrt eine freigegebene, aber noch nicht
umgesetzte Spalte — die `ProductionOrder.Matchcode`-Spalte aus
[[2026-09-10-fa-liste-ausbau-matchcode-spec]] wird durch den Artikelstamm-Weg ueberfluessig und
ist dort zurueckzunehmen.

## Bezug

[[2026-09-10-fa-liste-ausbau-matchcode-spec]] (ProductionOrder-Matchcode — wird ueberfluessig) ·
[[2026-09-13-kommissionierung-nur-hauptfa]] (ausgelagertes Schwesterthema)
