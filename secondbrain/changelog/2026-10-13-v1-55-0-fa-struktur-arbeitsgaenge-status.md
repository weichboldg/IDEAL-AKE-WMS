---
type: changelog
version: 1.55.0
date: 2026-10-13
---
# v1.55.0 — FA-Struktur: Status je Arbeitsgang-Kürzel (Teil 2), Kontextzeilen AA-konform, Offset-Pflicht

[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]] (Antworten 1–11 + Nachtrag 2026-10-13). **Direkt im Bündel**
`feature/2026-08-07-ideal-teile-1-5`, Start `d56a340e` (v1.54.0), Commits `539be27f`, `e33cdc83`, `7690d281`, `6d85810b`,
`99a3b09e`, QA-Fixes `57b762a9`, `627c992a`. **Keine Migration.** Umsetzung [[2026-10-13-fa-struktur-arbeitsgaenge-symbole-teil-2-umsetzung]].

**Warum:** Teil 1 zeigt die Sage-Kürzel neutral; die Fertigung will auf einen Blick sehen, welcher Arbeitsgang erledigt ist,
läuft oder zu spät ist — ohne das Arbeitsgang-Fenster zu öffnen.

## Kernpunkte

- Status je Symbol nur bei `BdeAktiv` global **und** BDE-aktiver Werkbank: erledigt (IsFinal auf nicht stornierter Buchung),
  läuft (offene Buchung, pausiert zählt nicht), läuft + überfällig, überfällig, offen; „kein BDE-Arbeitsgang“ neutral (W-2 kein
  Vater-Rückfall, W-3 Waisen-Blatt). Mehrfach-Treffer zusammengefasst + eine `LogWarning` je Seitenaufbau (W-4).
- Soll = frühester FE_Termin der Struktur + Offset (Arbeitstage) aus `GetDueDateOffsetsAsync`, Rechnung über den unveränderten
  `OseonDueDateCalculator`; fehlender Offset → 0 + Sammelhinweis über dem Baum (ViewModel-Feld, nicht TempData).
- Abfragen bei BDE an: eine WorkOperation-Projektion + eine Status-Methode (2 Statements, OPENJSON) + Feiertage + Offsets
  (beide gecacht); bei BDE aus keine.
- Antwort 11 = C: Kontextzeilen beim Spaltenfilter kursiv + grauer Randstreifen statt `opacity: 0.6`. Kontrastwerte in der Spec.
- FA 4a: `OperationConfigsController.Add`/`Update` mit `int?`, fehlender Offset → Warnung, nichts gespeichert
  (erledigt [[2026-10-12-operationconfig-offset-leer-pruefen]]).
- TS-88 + TS-87.3-Negativfall; Tests Web 1826 (+31 gegenüber v1.54.0), Service 272.

## Deploy-Risiken

Nur Web. Produktiv ohne sichtbare Wirkung, solange `BdeAktiv` aus ist (Stand: an keinem Standort an); einzige sofort
sichtbare Änderungen sind die Kontextzeilen-Kennzeichnung im FA-Baum und die Offset-Pflicht in den Stammdaten. Abnahme am
Testsystem mit `BdeAktiv = true`.

Merge-Commit: — (Bündel, nicht gemergt)
