---
type: spec
title: "IDEAL-Standort Teil 3 — Kommissionierlisten"
slug: 2026-07-29-standort-ideal-teil-3-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-06
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/IdealKommissionierListenController.cs (neu, Name provisorisch)
  - IdealAkeWms/Services/KommissionierListenService.cs (neu)
  - IdealAkeWms/Views/IdealKommissionierListen/Index.cshtml (neu)
  - IdealAkeWms/Views/IdealKommissionierListen/Print.cshtml (neu)
  - IdealAkeWms/Models/AppSettingKeys.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "B2/B-5: Scan liefert HauptFA (Gruppe, nicht eindeutig) — was passiert nach dem Scan in der Kommissionierliste? Ganze Gruppe anzeigen oder Werker waehlt Sub-FA?"
  - "Offene Frage 3 (Notiz): Geschwister-Materialfluss — gibt es ein Kennzeichen fuer hausintern gefertigte Positionen, oder muss ueber Artikelnummer gegen Geschwister-FAs gematcht werden? Muss das WMS eine Reihenfolge/Verfuegbarkeit erzwingen, oder ist das reine Information (PPS steuert)?"
  - "Offene Frage 4 (Notiz, harter Blocker) — Kommissionier-Doppelzaehlung: Haupt-FA fuehrt Baugruppen seiner Sub-FAs UND jeder Sub-FA fuehrt eigene Bestandteile. Auf welcher Ebene wird tatsaechlich kommissioniert (Haupt-FA, je Sub-FA, gemischt Zukauf/Fertigungsmaterial)? Vorbedingung fuer korrekte Mengen."
  - "Vorpruefung vor Feinspezifikation: gibt es Zeilen mit SubFA != 0 UND gesetztem Kommissionieren? Falls nein, ist Kommissionieren allein die Trennlinie Lagerentnahme/Eigenfertigung und es kann nichts doppelt gezaehlt werden."
  - "Rollen/Zugriff: bestehende Rolle (picking) wiederverwenden oder neue IDEAL-Rolle?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Filterbare Kommissionierlisten je Kommissionier-Ziel (`Kommissionieren`) fuer die IDEAL-Linie —
das druckbare Arbeitsdokument fuer die Lagerentnahme, analog zur AKE-Kommissionierung, aber auf
Basis der IDEAL-Struktur (Teil 1) statt der AKE-BOM-Kette.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Liste/Druck aller `IdealFaStruktur`-Positionen mit gesetztem `Kommissionieren`,
gruppiert nach `HauptFA` (+ Montage-Abteilung), Kopf aus `IdealFaInfo`, Barcode `HauptFA`.

**Out-of-Scope:** tatsaechliche Buchung/Transfer (dieser Teil druckt/listet, bucht aber nicht —
eine Buchungsfunktion setzt echte `ProductionOrders` voraus, also fruehestens nach Teil 7/8, falls
ueberhaupt gewuenscht — nicht Teil dieser Spec); `ProductionOrders`/AKE unveraendert.

## Fachliche Anforderungen

- Filter: `Kommissionieren IS NOT NULL AND Kommissionieren <> ''`, zusaetzlich Filter nach
  konkretem Ziel-Wert.
- Gruppierung nach `HauptFA` (+ `MontageAbteilung` aus `IdealFaInfo` bei Kombinationsgeraeten).
- Kopf aus `IdealFaInfo` (`ABNr`, `HauptFA`, Kunde/Termine je nach Layout-Bedarf).
- Barcode = `HauptFA` (einziger Produktions-Identifier laut Anhang).
- **Vor der Feinspezifizierung zwingend zu pruefen** (Notiz-Vorgabe): existieren Zeilen mit
  `SubFA != 0` UND gesetztem `Kommissionieren`? Wenn nein, trennt `Kommissionieren` selbst
  Lagerentnahme von Eigenfertigung sauber. Wenn ja, zusaetzlich ueber `SubFA = 0` bzw.
  `Beschaffungsartikel` filtern, um keine Baugruppen-internen Positionen mitzuziehen.

## Technischer Loesungsentwurf

`KommissionierListenService` liest ueber `IIdealFaStrukturRepository`/`IIdealFaInfoRepository`
(Teil 1), wendet Filter/Gruppierung an, liefert ein Druck-ViewModel analog zum bestehenden
`WarehousePickingPrintLayout`-Muster (GUI-Spiegelung: gleiche Filter/Sortierung im Druck wie in
der Liste).

## Migrations-/SQL-Auswirkungen

Keine — reine Lesefunktion.

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten. Falls eine spaetere Ausbaustufe eine Buchungsfunktion ergaenzt, ist das
gegen echte `ProductionOrders`/`StockMovement` zu bauen (Teil 7/8), nicht gegen die
Struktur-Cache-Tabellen.

## Akzeptanzkriterien

1. Liste zeigt ausschliesslich Positionen mit gesetztem `Kommissionieren`.
2. Gruppierung trennt Kombinationsgeraete korrekt nach Montage-Abteilung.
3. Druck spiegelt exakt Filter/Sortierung der Bildschirmliste (analog Lagerbestellungs-Druck-Muster).
4. **Keine Doppel- oder Nullzaehlung** von Mengen (Akzeptanzkriterium, sobald offene Rueckfrage 3
   geklaert ist — bis dahin Blocker fuer den Dev-Lauf).
5. AKE-Verhalten unveraendert.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 3 — Kommissionierlisten": Filter auf ein konkretes Kommissionier-Ziel;
Kombinationsgeraet mit zwei Montage-Abteilungen korrekt getrennt; Mengenabgleich Struktur vs.
Liste (kein Doppelzaehlen); Druck-Vergleich Bildschirm vs. Papier.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch).

## Offene Rueckfragen

1. B2/B-5 — Scan = Gruppe: Was passiert nach dem Scan (ganze Gruppe zeigen vs. Sub-FA-Auswahl)?
2. Offene Frage 3 der Notiz — Geschwister-Materialfluss: Kennzeichen fuer Eigenfertigung
   vorhanden, oder Artikelnummer-Matching noetig? Braucht das WMS eine Reihenfolge-/
   Verfuegbarkeitslogik?
3. Offene Frage 4 der Notiz (harter Blocker) — auf welcher Ebene wird kommissioniert, um
   Doppelzaehlung zu vermeiden?
4. Existieren Zeilen mit `SubFA != 0` UND gesetztem `Kommissionieren` am Testsystem?
5. Rollen/Zugriff fuer diese Liste.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
2. →
3. →
4. →
5. →
