---
type: backlog
status: offen
created: 2026-11-16
quelle: Gerald, Chat 2026-11-16 (/spec)
standort: IDEAL
---
# FA-Struktur IDEAL vollständig über SData (ohne FAInfos)

Auftrag Gerald 2026-11-16. Quellen: [[sage-sdata-benutzerfelder-lesetest]] Abschnitte L10–L12, [[2026-10-30-fa-rueckmeldung-an-sage]]
(IDEAL-Befund 16.11.). Basis Bündel v1.99.23, Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig
`feature/2026-08-07-ideal-teile-1-5`. Belegte Nummern: SQL 151/152, TS-139..145, ADR 0027/0028, Version 1.99.24/1.99.25 (Specs
[[2026-11-14-sicherheit-anmeldung-und-installer-spec]], [[2026-11-16-beschichtungslogik-je-standort-spec]]) – nächste frei: SQL 153, TS-146,
ADR 0029, Version 1.99.26.

## Schwerpunkte

1. Annahme G11 widerlegen und belegen: Knotenfelder der FAListe (`FaHierarchyNode`) aus SData – Unter-FA: `dtsFaBelegsuche` /
   `Sage.PPS KHKPpsFaBelege_USER_OS*`; Blatt: Artikel `USER_OS*`; Struktur: `StrukturID` + `PpsFaBelegePositionenInfo.BelIDNachfolger`.
   Feld-für-Feld-Zuordnung gegen die View-Definition FAListe (Prüfabfrage für Gerald); offen: Arbeitsbereich, Beschaffungsartikel,
   HauptArtnr – nicht aus Namen ableiten.
2. `SdataFaStrukturReader` um die Knotenfelder erweitern; Sperre P-F2 nur für die Struktur aufheben, FAInfos bleibt SQL/View. Modus
   „Vergleich“ zuerst (Abweichungen im Protokoll Sage-Quellenvergleich).
3. Beendete Haupt-FAs: `PpsFaBelegeInfo` liefert sie nicht (1059463) – Gegenprobe, Lösung über `dtsFaBelegsuche`.
4. Jede SData-Abfrage mit `where` braucht `orderBy` (sonst 500) – alle Reader prüfen.
5. Artikel IDEAL: `USER_OS*`-Felder (IstLagerartikel, Artikeltyp, Arbeitsschritte, Material, MaterialnummerPN, Abteilung,
   FertigungsInfoPN, Kommissionieren, VMBedarf, Beschichtet …) über `Sync:ArtikelBenutzerfelder`; Weg zur Aufhebung der Sperre P-R1
   beschreiben.
6. „Stückliste live“: FA-Modus auch für beendete Haupt-FAs; im Artikel-Modus klar kennzeichnen „Ressourcenliste (Vorlage), nicht
   FA-Stückliste“.

## Grenzen

Nur lesen von Sage, nichts schreiben, keine Einstellungen auf Test/Prod, kein Merge, kein Push.
