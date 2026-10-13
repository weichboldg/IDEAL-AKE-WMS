---
type: changelog
version: 1.58.1
date: 2026-10-13
---
# v1.58.1 — Spaltenfilter-Suchsyntax, Etappe B (alle Listen, Typmarkierung, Rückbau Zwischenzustand)

[[2026-09-23-suchsyntax-spaltenfilter-erweitern-spec]] (Epic, Abschluss). **Direkt im Bündel** `feature/2026-08-07-ideal-teile-1-5`,
Start `10ccf1f5` (Endstand Etappe A, v1.58.0), Commits `0e82363b`, `3816542d`, `404a1b49`. **Keine Migration.**
Etappe A: [[2026-10-13-v1-58-0-suchsyntax-spaltenfilter-etappe-a]]. Umsetzung [[2026-10-13-suchsyntax-spaltenfilter-erweitern-umsetzung]].

**Warum:** Nach Etappe A verstanden nur FA-Liste, `Apply`-Listen und Client-Listen (Text) die neue Syntax; Leitstand-Postfilter,
Artikel, Bewegungen, BDE-Buchungen, Fehlteile, Stückliste und FA-Baum filterten noch mit eigener Bedeutung.

## Kernpunkte

- Alle übrigen Filterwege über den Kern: Leitstand (M2), Artikel (S2), Bewegungshistorie inkl. Datum in SQL tageweise (S4),
  BDE-Buchungen SQL + C# (S5/M3), Fehlteile mit `!` für alle Teile (S6, Antwort 5), Stückliste (J2), FA-Baum (J3).
  OSEON-Spaltenfilter gelöscht (Antwort 10). Alte `ColumnFilterHelper.Parse`-Signatur entfernt; kein `EF.Functions.Like` mehr.
- Register `ColumnFilterTypes` (32 Listen, 33 Datums-/48 Zahlspalten) + `<th>`-Markierung + Drift-Wächter
  `ColumnFilterTypesDriftTests`; Getter wie die Zelle (FaWorklist/FaCompletion Menge, Bestand).
- Zwischenzustand aus A (`data-filter-syntax`) zurückgebaut; „?“-Hilfe als gemeinsame Browser-Hilfe, wandert in die erste sichtbare
  Filterzelle.
- ADR 0005 Nachtrag, [[fallstricke]] §26; Tests Web 1998 (+62 gegenüber A), Service 272; gemeinsame Tabelle 84 Fälle.

## Deploy-Risiken

Nur Web (Service nur Versionsanzeige). **Vor dem Deploy:** P-1 (gespeicherte Standardfilter, AKE **und** IDEAL), **P-2 Kollation der
Datenbank und je Spalte (Pflicht — SQL-Filter ohne `LOWER`)**, P-3 Server-Kultur. Laufzeitmessung AK 12 am Testsystem (TS-91.B1).
**Anwender informieren:** „oder“ heißt jetzt `;` (Komma nur noch in Textspalten); Fehlteile `!a;b` = weder a noch b.

**QA Epic (2026-10-13):** Testbereit; QA-Fix `ead16fc4` — `ColumnFilterHelper.HasAny` kennt jetzt die Listentypen, sonst fehlte beim
Druck/PDF von Beschichtungsauftrag und Kommissionierliste nach einem Zahl-Vergleich (`Sollmenge >10`) das Banner „Gefilterte Ansicht“.

Merge-Commit: — (Bündel, nicht gemergt)
