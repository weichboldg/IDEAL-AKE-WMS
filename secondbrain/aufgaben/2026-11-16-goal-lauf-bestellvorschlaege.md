---
type: aufgabe
status: erledigt
created: 2026-11-16
quelle: Gerald, Chat 2026-11-16 (/goal + Dev-Lauf-Auftrag)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-16: Bestellvorschläge Nachbesserung

Auftrag Gerald 2026-11-16: [[2026-11-16-bestellvorschlaege-nachbesserung-spec]] im Bündel-Worktree (Stand v1.99.29). Spec liegt unter
`specs/entwurf/` mit `status: Freigegeben` – laut Auftrag gilt sie als freigegeben. Rumpf vorher nachgezogen (Antworten 1–8, Nachtrag 19:49;
`5099cce1`). Grenzen: nichts an Sage, keine Einstellungen auf Test/Prod, kein Merge, kein Push. Publish am Ende.

Nummern beim Start: Version 1.99.29 → **1.99.30** (1.99.27 reserviert Mapping); SQL: letzte 152, 151/153/154 reserviert → **SQL/155**;
TS: letzte 153 → **TS-154**. Parallel läuft im selben Worktree der Lauf [[2026-11-16-goal-lauf-beschichtung-fa-sdata-arbeitsgaenge]]
(FA-Struktur R1/R2) – nur explizite Pfade stagen, Versions-/Changelog-Dateien vor dem Bump frisch lesen.

Restfragen aus dem Rumpf-Nachzug: R1 (Artikelinfo-Link nur mit `stammdaten_lesen`; einkauf hat das Recht nicht – Gerald entscheidet:
/Roles oder Filter erweitern), R2 (Bestellungen ohne Liefertermin zählen nicht im Fenster – mit Till bestätigen), R3 (Info).

## Übersicht

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-16-bestellvorschlaege-nachbesserung-spec]] | Epic | 155 | 1.99.30 | **Testbereit** (qa-agent) | `223d7015`…`fd9f49db`, QA-Fix `54f04740` | E8 Blocker P-N6/P-N9; E7 Ersatzlösung bis P-N1..P-N4/P-N7; QA Q2 (Kommissionierwagen doppelt, P-N8) zur Entscheidung |

## Protokoll

- 2026-11-16: Lauf gestartet, Rumpf nachgezogen (`5099cce1`).
- 2026-11-16: E1 erledigt (`223d7015`): eigener Lauf „Letzte Bestellung (Einkauf)“ (LetzteBestellungSyncService, Uhrzeiten 3/12 Uhr, Marker aus SyncLogs), MengeBasis 0 zählt nicht.
- 2026-11-16: E2 erledigt (`92e6fcc3`): ArticleLastOrderUnassigned + SQL/155, Hinweise nach Ursache, Lieferant-Kaskade. Abweichung von Spec-Satz „entspricht der heutigen Regel jüngste über alle“: heute gilt Hauptlieferant vor jüngster; umgesetzt wie Antwort 4 (ohne Zuordnung nur, wenn jünger als ALLE zugeordneten – dann auch vor dem Hauptlieferanten).
- 2026-11-16: E3 erledigt (`6a3e7502`): PlannedConsumptionService (FA-Rest nur `Ausbuchung` bis P-N8, max je FA mit Bedarfsmeldungen, max je Artikel mit Lagerbestellungen), Listen-BOM-Methode ersetzt die zwei Einzelmethoden, Artikelinfo mit Aufschlüsselung + Spalte „Rest“, EinkaufOffeneBedarfe-Beschreibung. Bedarfsmeldungen zu FAs ohne Stücklisten-Treffer zählen voll ohne Termin. 107 Tests im Filter grün.
- 2026-11-16: E4 erledigt (`6c2e039c`): Liste auf PlannedConsumptionService (Gleichheitstest Liste = Artikelinfo flach/hierarchisch), Spalten FA-Bedarf + Matchcode, Banner (Doppelzählung, Cache-Fenster, Lagereinheit), Link nur mit Stammdaten-Lesen, returnUrl in der Artikelinfo (Prüfung `Url.IsLocalUrl` in der View). Nachgezogen: Manifest-Eintrag SQL/155 (Drift-Guard war seit E2 rot). Web-Suite 2976 grün. Abweichung: kein Block-Lesen in 200er-Blöcken – eine Abfrage über OPENJSON, Ladezeitmessung AKE-DEV bleibt QA.
- 2026-11-16: E5 erledigt (`addd7c0b`): Kritisch über Fenster heute + WBZ (`PurchaseProposalCalculator.VerfuegbarBis`, `ArtikelBedarf.AngerechnetBis`), Hinweise ohne Liefertermin/ohne WBZ, Spalte „nächster Bedarf“ (Datumsfilter). Web-Suite 2984 grün. Festlegung: verfügbar (alle) > Meldebestand bleibt „kein Bedarf“, auch bei negativem Fenster (genug bestellt, nur spät) – zur Bestätigung durch Till.
- 2026-11-16: E6 erledigt (`34c38ce8`): Mengenregel A im Calculator (`BisUeber`, Aufrunden auf `DezimalstellenLager`, null = ganze Einheit), gilt auch für den Fallback Sage-Bestellmenge; ohne Mengenquelle jetzt M − v + 1 statt Fehlmenge/Meldebestand (Spec-Wortlaut F6). Erwartungswerte alter Tests (Vielfaches, 25 → 26) angepasst. Web-Suite 2991 grün. MBM-Wächter bleibt bis P-N2 (E7).
- 2026-11-16: E7 erledigt als Ersatzlösung (`4a580fc5`): `PositionRechenbar` (Basis = Lager trotz EinheitAbweichend → eingerechnet), `BasisNichtLager` sperrt letzte Bestellmenge und Sage-Bestellmenge bei Basis ≠ Lager oder UmrechnungsFaktorLME ≠ 1 (BV-1), Zeilenhinweise + Banner nur noch nicht umrechenbare. Abweichung: kein eigener Helper `Einheiten.InLager` – die Ersatzregel ist ein Gleichheits-Check, zwei statische Methoden am Controller neben `EinheitWeichtAb` reichen; der Helper kommt mit der ersten echten Umrechnung. **Blocker P-N1/P-N2/P-N3/P-N4/P-N7** (alle offen) für die Umrechnung über Einheitengrenzen. MBM-Wächter unverändert (P-N2). Web-Suite 2997 grün.
- 2026-11-16: E8 übersprungen – **Blocker P-N6/P-N9** (kein belegter Leseweg für EK-Rahmenverträge); nichts gebaut, SQL/156 unverbraucht. Lieferant-Kaskade `WaehleLieferant` hat die Stelle für die Rahmenvertrag-Stufe kommentiert.
- 2026-11-16 E9 Abschluss: fd9f49db (wip feature-complete), Web 2997 gruen/1 uebersprungen, Service 675 gruen, Migrationstest . gruen; Brain: Changelog, feature-map, Testindex TS-154, services.md, datenmodell.md, fallstricke §37, Spec-Deploy-Abgleich. Offen: qa-agent, Info-Manifest-Eintraege.

## Abschluss (2026-11-16)

Spec **Testbereit** (qa-agent). Worktree-HEAD `54f04740`, v1.99.30, SQL/155, TS-154. Tests Web 2997/0/1, Dienst 675/0/0. DEV AKE+IDEAL
v1.99.30, Migration auf beiden. Publish `<Worktree>\publish\` (publish-info: 1.99.30, `54f04740`); alte `publish\deploy.zip` nicht verwenden.

Offen für Gerald: Q2 (Bestand im Kommissionierwagen fehlt im Listen-Bestand, FA-Rest sinkt erst bei Ausbuchung → Liste schlägt zu viel vor;
Lösung a: FA-bezogene Umbuchung in Wagen mindert Rest, b: Liste zählt Wagenbestand wie Artikelinfo); P-N1..P-N9; Till: Mengenregel A und
Grenzfall E5 (TS-154.6); R1 Recht Einkauf auf Artikelinfo; Lager/Produktion über geänderte Artikelinfo informieren; Manual-Checkliste in der Spec.
