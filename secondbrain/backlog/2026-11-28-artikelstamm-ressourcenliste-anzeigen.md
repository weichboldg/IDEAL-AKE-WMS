---
type: backlog
typ: feature
status: offen
jira: WMS-10
created: 2026-11-28
standort: IDEAL
quelle: "Mail „WMS Anpassungen“ (Benjamin Andl-Braun, IDEAL) Punkt 2 (zweiter Satz) + Ergänzung Gerald im Chat 2026-11-28 („Ressourcenliste im Artikelstamm als eigene anzeigbare Liste“)"
anhaenge:
  - anhaenge/2026-11-28-mail-wms-anpassungen-2.png
---
# Artikelstamm: Ressourcenliste des Artikels als eigene Liste anzeigen

## Wunsch
Hat ein Artikel in Sage eine **Ressourcenliste**, soll sie im Artikelstamm angezeigt werden – laut Gerald als **eigene, anzeigbare Liste**
(nicht nur als Feld auf der Artikelseite).

## Befund (2026-11-28, nur gelesen)
- Im Produktivteil des WMS gibt es keine Artikel-Ressourcenliste. Vorhanden nur im Testbereich „Stückliste live“ als „Ressourcenliste (Vorlage)“
  (`Controllers/SageTestController.cs`, `Views/SageTest/Stueckliste.cshtml`).
- Fallstrick: Ressourcenliste (Artikel) ≠ FA-Stückliste (Artikel 80141025: 6 Ressourcen, FA 1059463: 13 Positionen) – [[fallstricke]].
- Sage-Quelle: SData `strRessourcenlisteKopf` / `strRessourcenPosition` ([[sage-sdata-masterdata]]) oder Tabellen
  `KHKPpsRessourcenkopf` / `KHKPpsRessourcenPositionen`.
- Dieselbe Datenquelle braucht [[2026-11-24-bf-auftrag-baugruppen-aufloesen-und-als-fa-zeigen-spec]] (BG → Einzelteile) – gemeinsame Lesung planen.

## Rückfragen (Vorschlag)
- Live aus Sage lesen (wie der Testbereich) oder per Dienst ins WMS übernehmen (dann Filter/Spalten nach ADR 0005)?
- Einstieg: Knopf/Reiter auf der Artikelseite (Info/Bearbeiten) und/oder eigene Liste unter Stammdaten?
- Welche Spalten (Pos., Artikel, Menge, Arbeitsgänge, Teileart, Ist Lagerartikel)? Mehrstufig aufklappbar?
