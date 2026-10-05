---
type: adr
id: 0014
title: Werkbank bei IDEAL aus dem Sage-Arbeitsbereich (Sage fuehrend), mit unscharfer Abweichungsmeldung als Umschaltpunkt
status: accepted
date: 2026-09-09
supersedes: ""
superseded_by: ""
---

## Kontext und Problem

Am Standort IDEAL (Master `ProduktionsauftragHierarchisch = true`) traegt jeder Struktur-Knoten
einen `Arbeitsbereich` (`FaHierarchyNode.Arbeitsbereich`, z. B. `K-02`, `S-01`, `H4-04`). Im WMS
gibt es dafuer bereits ein Feld: `ProductionOrder.ProductionWorkplaceId`, ein **Fremdschluessel**
auf `ProductionWorkplace`. Der Fachbereich hat entschieden, dass die Werkbank kuenftig automatisch
aus dem Arbeitsbereich kommen soll.

Das kollidiert mit einer bewussten Regel des Pakets. Der Klassenkommentar von
`FaMaterializationSyncService` fuehrte `Workplace` ausdruecklich unter den Feldern, die bei Updates
**nie** ueberschrieben werden — zusammen mit `IsDone`, `PickingStatus`, `BdeStatus`, Storno und
ExtraInfo. Diese Regel (Z1) existiert, weil das Paket eine ganze Klasse stiller Datenverluste
verhindern soll: Ein Sync, der app-verwaltete Felder ueberschreibt, loescht Arbeit von Menschen,
ohne dass es jemand bemerkt.

Die Frage ist also nicht „kann der Sync das Feld schreiben", sondern **wem das Feld gehoert**.

## Entscheidungstreiber

- Bei AKE disponiert vermutlich der Leitstand die Werkbank von Hand. Bei IDEAL ist das Feld heute
  **leer** — es wurde nie geschrieben, also gibt es aktuell nichts zu ueberschreiben.
- Das Risiko liegt in der Zukunft: Sobald die Spalte gefuellt ist, wird jemand umdisponieren wollen.
- Ein Sync kann **nicht** unterscheiden, ob ein abweichender Bestandswert von einem Menschen gesetzt
  oder in Sage umgeplant wurde. `FaHierarchyNode` wird bei jedem Lauf vollstaendig ersetzt; der
  vorige Quellwert ist danach weg.
- `ProductionWorkplace.Name` hat **keinen** Unique-Index, und es gibt keinen Seed. Ein Name-Match
  kann mehrdeutig sein oder ins Leere laufen.

## Betrachtete Optionen

| Variante | Bedeutung | Bewertung |
|---|---|---|
| **A — nur beim Anlegen** | Sage setzt den Startwert, danach gehoert das Feld dem WMS. | Sicher gegen stillen Verlust, aber jede spaetere Sage-Umplanung bleibt unsichtbar. |
| **B — bei jedem Lauf, Sage fuehrend** | Eine manuelle Zuweisung wird beim naechsten Lauf ueberschrieben. | Bricht Z1 — **ausser** die Ueberschreibung wird gemeldet. |
| **C — zweites Feld** | Sage-Arbeitsbereich und WMS-Werkbank getrennt. | Sauberste Trennung, aber zwei aehnliche Felder in der Oberflaeche und eine Migration. |

## Entscheidung

**Variante B mit Meldepflicht.** Sage ist fuehrend; der Materialisierungs-Sync schreibt
`ProductionWorkplaceId` im Anlege- **und** im Update-Zweig aus `Arbeitsbereich`. `Workplace` faellt
damit ausdruecklich aus der Z1-Liste heraus, und der Klassenkommentar sagt das auch.

Drei Absicherungen machen die Entscheidung tragfaehig:

1. **Jede Ueberschreibung eines abweichenden Bestandswerts wird gemeldet** — im
   Aktivitaets-Protokoll des `FaMaterialization`-Laufs, mit altem und neuem Wert.
2. **Die Meldung ist bewusst unscharf formuliert:** „Werkbank weicht vom Quellwert ab". Sie
   behauptet **nicht**, jemand habe manuell zugewiesen, denn das kann der Sync nicht wissen. Eine
   Meldung, die mehr behauptet als sie weiss, ist schlimmer als keine.
3. **Unbekannter oder mehrdeutiger Arbeitsbereich fuehrt nie zu einer Zuweisung.** Fehlt der
   Werkbank-Stammsatz, wird gemeldet und **nichts** geschrieben; existiert der Name mehrfach,
   ebenfalls. Der Sync legt **keine** Werkbank-Stammdaten an — sonst wandern Tippfehler und
   Altlasten aus Sage unkontrolliert in die Stammdaten.

**Der Umschaltpunkt auf Variante C ist eine menschliche Beurteilung, kein Automatismus.** Haeufen
sich die Abweichungsmeldungen und zeigt die Sichtung, dass tatsaechlich von Hand disponiert wird,
ist **das** der Anlass, C mit ihrem zweiten Feld zu bauen. Vorher waere C Vorratsbau.

## Konsequenzen

**Gut:**
- Die Werkbank fuellt sich fuer IDEAL ohne manuelle Pflege, und die werkbank-spezifische
  Vorkommissionier-Abweichung (`OverridePrePickingDays`) wirkt dadurch ueberhaupt erst.
- Keine Migration, kein zweites Feld, keine Doppeldeutigkeit in der Oberflaeche.
- Der Wechsel auf C haengt an einem Beleg statt an einer Vermutung.

**Schlecht, und bewusst in Kauf genommen:**
- Eine manuelle Zuweisung an einem IDEAL-Auftrag ueberlebt hoechstens 15 Minuten. Wer das nicht
  weiss, haelt es fuer einen Fehler. Gehoert auf die Hilfeseite.
- Die Abweichungsmeldung kann nicht zwischen Mensch und Sage-Umplanung unterscheiden; sie ist ein
  Anlass zum Hinsehen, kein Beweis.
- **Zwei-Lauf-Ablauf beim Deploy:** Weil `ProductionWorkplaceId` ein Fremdschluessel ist, bleibt die
  Werkbank leer, solange der Stammsatz fehlt. Der erste Lauf meldet nur; erst der naechste fuellt.
  Empfehlung: die Arbeitsplaetze **vor** dem Deploy anlegen, die Namen sind aus
  `FaHierarchyNode.Arbeitsbereich` bekannt.

**Fuer AKE aendert sich nichts.** Der gesamte Pfad laeuft nur bei Master `true`.

## Bezug

Spec [[2026-08-20-materialisierung-fachliche-felder-spec]] (Werkbank-Datenhoheit, Antworten zu B4),
Umsetzung [[2026-09-09-materialisierung-fachliche-felder-umsetzung]],
Z1-Regel in [[2026-08-18-fa-liste-hierarchie-anzeige-spec]],
Satelliten-Tabellen [[0009-app-status-in-satelliten-tabellen-neben-sage-master]],
Aktivitaets-Protokoll [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]].

## Nachtrag 2026-09-22 — falsches Zielfeld korrigiert (ADR bleibt gültig)

Diese Entscheidung wurde ursprünglich (v1.37.0, August 2026) auf **`ProductionOrder.ProductionWorkplaceId`
aus `FaHierarchyNode.Arbeitsbereich`** angewandt — unter der damals vorläufigen Annahme „Werkbank =
Arbeitsbereich" (die Sage-Arbeitsplatz-Stammdaten lagen noch nicht vor).

**Am 2026-09-21 klargestellt und widerlegt:** Der **Arbeitsbereich** (`K-02`, `S-01`; Sage
`USER_OSAbteilung`) ist ein **Zielort** (wohin das Teil kommt), **keine** Werkbank. Die Werkbank ist der
Sage-Arbeitsplatz (`KHKPpsArbeitsplaetze`, `USER_ArbeitsSchritt`). Die Ableitung wurde deshalb
zurückgebaut, bevor sie je produktiv war ([[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]], v1.43.0;
`FaMaterializationSyncService.ApplyWorkplace`/`IUnknownWorkplaceState` entfernt).

**Das Muster dieser ADR bleibt richtig und gilt weiter** — „Sage führend, Abweichung unscharf melden,
Umschaltpunkt Variante C" — es wurde nur auf das **falsche Feld** angewandt. Seine **korrekte** Anwendung
ist die Werkbank-Anlage aus `KHKPpsArbeitsplaetze` (Sage führend, Abweichungsmeldung) — siehe
[[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] (Baustein a). Kein Supersede, nur diese Korrektur
des Zielfelds.
