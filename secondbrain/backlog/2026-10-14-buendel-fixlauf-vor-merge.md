---
typ: bug
---
# Bündel IDEAL: Sammel-Fixlauf vor dem Merge (Kleinbefunde Suchsyntax, Fußzeile, Abnahme)

**Aufgenommen 2026-10-14 (Koordination).** Sammelt die bekannten Kleinbefunde aus QA und Erstellung der Testprotokolle,
damit sie mit den Funden der Abnahmeläufe TS-91 und TS-81–90 in **einem** /dev im Bündel
(`.claude/worktrees/2026-08-07-ideal-teile-1-5`) erledigt werden. Bewertung je Punkt erst nach den Testberichten.

## Muss vor dem Merge (Empfehlung)

- **Hilfeseite, Absatz „Andere Listen"** nennt BDE-Buchungen als nur clientseitig gefiltert; sie filtern seit v1.58.1
  serverseitig (`Views/Help/Index.cshtml:245`, Fund qa-agent bei TS-91-Protokoll).
- **Fußzeile Q-4:** Muster-Fußzeile hat ~0,4 mm Luft bis zur Höhengrenze 17,75 mm; mit den echten Nimbus-Schriften kann
  der Balken ohne Hinweis abgeschnitten werden. Pflicht, falls TS-92.5 am Server durchfällt; sonst Luft erhöhen.
- **Funde aus den Berichten** TS-91 und TS-81–90 (werden hier ergänzt).

## Kann (klein, gleiche Runde sinnvoll)

- **Fußzeile Q-2:** `class=briefkopf-fuss` ohne Anführungszeichen → Ersatz-Kopf, Meldung sagt fälschlich „ohne End-Tag".
- **Suchsyntax „—"-Zellen:** `(leer)` findet Zellen nicht, die „—" anzeigen.
- **Suchsyntax zahlenähnliche Textspalten:** FA-Vervollständigung „Anwendbar"/„Vervollständigt", Stückliste „Ebene",
  FA-Baum „Soll/Fert." bleiben Text (`>5` rot).

- **Sage-Views (v1.60.0, QA-Minor):** Kommentar im Rückweg-Block des Einmal-Skripts nennt `UQ_Articles_ArticleNumber`
  (`SQL/Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql:168`), auf EF-angelegten DBs heißt der Index
  `IX_Articles_ArticleNumber` (fallstricke §9) — beide Namen nennen.
- **Sage-Views:** dauerhafter Test, der den bisherigen Artikel-Select (leerer `Sync:ArticlesViewName`) gegen spätere
  Änderungen sichert (Nachweis war einmalig).

## Später / eigene Notiz

- **Suchsyntax Schlüssellisten:** Leitstand und FA-Liste entscheiden SQL- vs. C#-Filter über eigene Schlüssellisten statt
  über `ColumnFilterTypes`; heute deckungsgleich, ungeprüft.
- **Drift-Wächter** prüft nur, dass ein Register übergeben wird, nicht an welchem Aufruf.
- „Seite X von Y" im Sammeldruck mehrerer HauptFA: [[2026-10-09-briefkopf-seitenzaehlung]].

## Anwender-Information (kein Code)

- „;" ist ODER (Komma nur noch in Textspalten); in den Fehlteil-Listen heißt `!a;b` „weder a noch b", eine alte Eingabe
  `a,!b` heißt jetzt nur noch „a" → gezielt an die Fehlteil-Nutzer. Text in die Benutzeranleitung.

Quellen: [[2026-09-23-suchsyntax-spaltenfilter-erweitern-spec]] (QA-Abschnitte), [[2026-10-13-briefkopf-fusszeile-word-vorlage-spec]]
(QA Q-2/Q-4), [[2026-10-13-ts-91-browser-testprotokoll]] (Funde aus der Erstellung).
