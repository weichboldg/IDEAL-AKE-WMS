---
type: spec
title: Bestellvorschläge – Nachbesserung (Laufzeit, FA-Bedarfe, Mengenregel, Einheiten, Dringlichkeit, Rahmenverträge)
slug: 2026-11-16-bestellvorschlaege-nachbesserung-spec
status: InUmsetzung
created: 2026-11-16
updated: 2026-11-16
source_backlog: "[[2026-11-13-bestellvorschlaege-nachbesserung]]"
weitere_backlog: "[[2026-11-14-review-befunde-bestellvorschlaege]]"
bezug:
  - "[[2026-11-08-einkauf-bestellvorschlaege-spec]]"
  - "[[2026-11-16-einkauf-klaerung-gerald-till]]"
task: "[[2026-11-16-goal-lauf-bestellvorschlaege]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.31"
sql_vorschlag: "SQL/155"
testszenario_vorschlag: "TS-154"
affected_code:
  - IDEALAKEWMSService/Services/Sdata/ArtikelLieferantSyncService.cs
  - IDEALAKEWMSService/Workers/SyncWorker.cs
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs
  - IdealAkeWms/Models/SyncLogServices.cs
  - IdealAkeWms/Services/PurchaseProposalCalculator.cs
  - IdealAkeWms/Controllers/PurchaseProposalsController.cs
  - IdealAkeWms/Data/Repositories/PurchaseProposalRepository.cs
  - IdealAkeWms/Controllers/ArticlesController.cs
  - IdealAkeWms/Data/Repositories/BomCacheRepository.cs
  - IdealAkeWms/Data/Repositories/FaHierarchyBomRepository.cs
  - IdealAkeWms/Data/Repositories/BomRepositoryMasterSwitch.cs
  - IdealAkeWms/Views/PurchaseProposals/Index.cshtml
  - IdealAkeWms/Models/ViewModels/PurchaseProposalListViewModel.cs
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
  - IdealAkeWms/Services/ColumnFilterSyntax.cs
  - IdealAkeWms/Views/Articles/Info.cshtml
  - IdealAkeWms/Models/ViewModels/ArticleInfoViewModel.cs
  - IdealAkeWms/Models/AppSettingDefinitions.cs
  - "neu: IdealAkeWms/Services/PlannedConsumptionService.cs (gemeinsamer Bedarfs-Service: FA-Rest, Lagerbestellungen, Bedarfsmeldungen, Regeln)"
  - "neu: IdealAkeWms/Models/ArticleLastOrderUnassigned.cs + Migration + SQL/155"
  - SQL/00_FreshInstall.sql
  - scripts/deploy/nachlauf.psd1
  - docs/TESTSZENARIEN.md
  - docs/anwender/08-bestellvorschlaege.md
  - docs/betrieb/04-konfiguration.md
  - IdealAkeWms/Views/Help/Changelog.cshtml
open_questions:
  - "1. Doppelzählung: Lagerbestellungen haben keinen FA-Bezug. Regel je Artikel: Abzug = max(FA-Teil, offene Lagerbestellungen) (Empfehlung) – oder anders?"
  - "2. Teilweise kommissionierte FAs: nur den Rest (BOM-Menge minus FA-bezogene Ausbuchungen) abziehen (Empfehlung) oder voll?"
  - "3. „Fehlmenge bis über den Meldebestand“: V = Meldebestand − verfügbar + 1 Lagereinheit (Empfehlung) oder bis Meldebestand + letzte Bestellmenge?"
  - "4. Letzte Bestellung bei Lieferanten ohne Artikel-Lieferant-Zeile in neuer Tabelle speichern (SQL/155) und ihre Menge als letzte Bestellmenge verwenden, mit Hinweis (Empfehlung ja)?"
  - "5. Uhrzeiten-Defaults: nachts 3 Uhr, mittags 12 Uhr; nach Dienststart sofort ein Lauf, wenn der letzte fällige Termin verpasst wurde (Empfehlung ja)?"
  - "6. Dringlichkeit: offene Bestellungen im Zeitfenster heute + WBZ alle mitzählen (Empfehlung) oder nur mit Liefertermin ≤ heute + WBZ?"
  - "7. Artikelinfo „Verfügbarer Bestand“: in diesem Epic nur umstellen, Verhalten gleich (Empfehlung) – oder Rest-Regel/Doppelzählungsregel auch dort?"
  - "8. Rahmenvertrag: Lieferant des Rahmenvertrags hat Vorrang vor Hauptlieferant, Restmenge des Rahmens nur als Hinweis (Empfehlung) – oder Rahmen auch als Mengenquelle?"
epic: true
etappen:
  - "E0 Prüfabfragen P-N1–P-N9 auswerten (Mensch, kein Code)"
  - "E1 Eigene Laufzeit letzte Bestellung + Basismenge 0 (Dienst)"
  - "E2 Letzte Bestellung ohne Artikel-Lieferant + Hinweistexte + Lieferant fehlt (SQL/155)"
  - "E3 Gemeinsamer Bedarfs-Service (FA-Rest, Lagerbestellungen, Bedarfsmeldungen, Regeln), Artikelinfo auf dieselbe Regel"
  - "E4 FA-Bedarfe in Bestellvorschlägen + Spalten Matchcode/FA-Bedarf + Link Artikelinfo mit Rücksprung"
  - "E5 Dringlichkeit nach Bedarfstermin, WBZ und Liefertermin offener Bestellungen"
  - "E6 Mengenregel A"
  - "E7 Einheiten-Umrechnung (Ersatzlösung ohne P-N1/P-N2/P-N3/P-N4/P-N7: nur gleiche Einheiten, sonst Hinweis)"
  - "E8 Rahmenverträge (Blocker P-N6/P-N9: ohne Belege übersprungen)"
  - "E9 Abschluss: TS-154, Hilfe, docs/anwender/08 + docs/betrieb/04, Changelog, Version, Brain"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: "Empfehlungen 1–8 (6 Liefertermin ≤ heute+WBZ, 7 gleiche Regel in Artikelinfo) + Nachtrag 19:49 Matchcode/Link"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-16"
---

# Bestellvorschläge – Nachbesserung

Quelle: [[2026-11-13-bestellvorschlaege-nachbesserung]] (Punkte 1–4, 6 und Antworten Till 2026-11-16), Review-Befund BV-1 aus
[[2026-11-14-review-befunde-bestellvorschlaege]]. Grundlage ist die Spec [[2026-11-08-einkauf-bestellvorschlaege-spec]] (v1.99.20–22, Testbereit).
Die Antworten stehen in [[2026-11-16-einkauf-klaerung-gerald-till]]. Code-Stand: Bündel-Worktree `2026-08-07-ideal-teile-1-5`, v1.99.29.

## Ziel / Nutzen (das Warum)

Till hat die Liste auf AKE-DEV durchgesehen. Fünf Dinge machen sie für den Einkauf noch unbrauchbar:

- Die Vorschlagsmengen sind zu hoch (k-faches Vielfaches, EK015744).
- Einheiten werden falsch oder gar nicht verrechnet: EK003604, EK002400, EK007623, EK005564.
- „Kritisch“ erscheint zu früh (EK023973).
- FA-Bedarfe fehlen.
- Der Abgleich der letzten Bestellung bremst jeden Dienst-Zyklus um rund 5 Minuten (Q-3).

Ziel: Die Liste schlägt pro Artikel eine Menge vor, die der Einkauf ohne Nachrechnen übernehmen kann. Sie gibt dazu einen ehrlichen Hinweis,
wenn eine Quelle fehlt, und hält „Offene Bestellungen“ und die Lieferverzug-Mail nicht mehr auf.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

1. Eigene Laufzeit „letzte Bestellung“: zwei Uhrzeiten (nachts, mittags) als ServiceSetting. Der Schritt wird aus dem Zyklus des
   Artikel-Lieferanten-Abgleichs herausgelöst. (entschieden)
2. Basismenge 0 zählt nicht als letzte Bestellung. (entschieden)
3. Hinweis je Ursache statt pauschal „keine Bestellung in 3 Jahren“, darunter „kein Artikel-Lieferant in Sage“ mit Kto und Name des
   Lieferanten. (entschieden) Dazu gehört die Speicherung der letzten Bestellung bei Lieferanten ohne Artikel-Lieferant-Zeile in einer neuen
   Tabelle (SQL/155); ihre Menge gilt als letzte Bestellmenge (Antwort 4).
4. FA-Bedarfe einrechnen: Die Logik aus `ArticlesController.cs:286-328` kommt in einen gemeinsamen Bedarfs-Service. Je Artikel gilt
   `Abzug = max(FA-Teil, Σ offene Lagerbestellungen)` (Antwort 1); teilweise kommissionierte FAs zählen nur mit dem Rest (Antwort 2).
5. **Artikelinfo mit derselben Regel** (Antwort 7): Die Artikelinfo rechnet ihren „Verfügbaren Bestand“ über denselben Service mit denselben
   Bedarfen (FA-Rest, Lagerbestellungen, Bedarfsmeldungen, max-Regeln). Liste und Artikelinfo zeigen dieselben Bedarfe. Sichtbare Änderung
   für Lager und Produktion – im Anwender-Changelog ausdrücklich nennen.
6. Mengenregel Variante A ohne k-faches Vielfaches: `V = M − v + 1 Lagereinheit`, wenn L nicht reicht, danach MBM (Antwort 3; mit Till am
   Bildschirm bestätigen, Regel bleibt an einer Stelle anpassbar).
7. Einheiten-Umrechnung über die Sage-Mengeneinheiten für letzte Bestellmenge, Sage-Bestellmenge, MBM und offene Bestellungen (EK005564).
   Löst auch BV-1. Solange P-N1/P-N2/P-N3/P-N4/P-N7 offen sind, gilt die Ersatzlösung aus F7 (nur gleiche Einheiten, sonst Hinweis).
8. Dringlichkeit nach Bedarfstermin und WBZ (EK023973); im Fenster zählen nur offene Bestellungen mit Liefertermin ≤ heute + WBZ
   (Antwort 6). Dazu der belegte Grund, warum bei EK023973 kein Lieferant erscheint.
9. Rahmenverträge: Lieferant des offenen EK-Rahmenvertrags hat Vorrang vor dem Hauptlieferanten, Restmenge nur als Hinweis (Antwort 8; mit
   Till bestätigen). Blockiert durch P-N6/P-N9.
10. Liste: neue Spalte **Matchcode**, Artikelnummer als Link auf die Artikelinfo mit Rücksprung inkl. Filtern (Nachtrag 19:49).
11. Rolle `einkauf`: Die drei Stellen sind schon vorhanden (siehe unten). Hier wird nur geprüft, nichts gebaut.

**Out-of-Scope (gehört nicht dazu)**

- **Backlog Punkt 5:** waagrechter Scrollbalken in „Wareneingang zu Bestellung“ bei 1500 px. Betrifft eine andere Maske und wird als
  eigener Backlog-Punkt gemeldet.
- Direkte Sage-Bestellung bzw. Dispo-Stapel (E5 der Grundspec, blockiert).
- Sonderbedarfe Technik/Vertrieb ([[2026-11-13-sonderbedarfe-erfassen]]).
- `USER_bestaetigt` (Till prüft noch).
- Vergleich mit Sage `LagerbestandVerfuegbar`. Die Bedeutung ist in Sage nicht belegt. Er wird nicht als Rechengrundlage genommen, höchstens
  später als Prüfspalte.
- Pflege der 255 fehlenden Artikel-Lieferanten in Sage (macht der Einkauf) und der Meldebestand der System-Leiche 00394205 (macht der Einkauf in Sage).
- `EinkaufAktiv` auf Test/Prod schalten (Entscheidung Gerald/Till, kein Code).
- Änderung am Cache-Flag `SagePurchaseOrderLine.EinheitAbweichend`. Es bleibt für den Wareneingang unverändert („nicht buchbar“,
  `PurchaseOrderReceiveViewModel.cs:47`).

## Fachliche Anforderungen

### F1 Eigene Laufzeit „letzte Bestellung“ (entschieden)

Ist-Zustand, belegt:

- `ArtikelLieferantSyncService` ruft den Schritt `LetzteBestellungAsync` im selben Lauf auf (`ArtikelLieferantSyncService.cs:180`).
- Der SyncWorker fährt sequenziell: Artikel-Lieferant (`SyncWorker.cs:463-473`) → Offene Bestellungen (`:476-486`) → Lieferverzug (`:490-499`).
- Der Schritt liest alle EBB/EBZ-Positionen der letzten 3 Jahre (`ArticleSupplier.LetzteBestellungRueckblickJahre = 3`). Auf DEV dauert das
  287 s bei 38.047 Zeilen (QA Q-3).

Soll:

- Der Artikel-Lieferanten-Abgleich läuft weiter in jedem Zyklus, aber **ohne** den Schritt „letzte Bestellung“.
- Die letzte Bestellung läuft als eigener Dienstlauf `LetzteBestellung`, nur wenn fällig. Fällig heißt: Ein konfigurierter Termin (nachts
  oder mittags) liegt heute oder gestern vor jetzt, und seit diesem Termin hat kein Lauf begonnen.
- Gate bleibt `Sync:ArtikelLieferantEnabled` (`ServiceSettingDefinitions.cs:117/276`). Ohne Artikel-Lieferant-Abgleich gibt es keine Zeilen,
  an die geschrieben würde.
- Der Lauf steht im Aktivitätsprotokoll unter einem eigenen Namen „Letzte Bestellung (Einkauf)“. Startmeldung, Endmeldung und deutsche
  Counts-Keys wie bisher (ADR 0010).

### F2 Basismenge 0 (entschieden)

- Positionen mit `MengeBasis <= 0` zählen bei der Bestimmung der jüngsten Bestellung je (Artikel, Lieferant) nicht. Die nächstjüngere
  Position mit Menge > 0 gilt.
- Heute übernimmt `ArtikelLieferantSyncService.cs:283` auch 0 (Q-4, EK023626).

### F3 Hinweis nach Ursache (entschieden) + Speicherung ohne Artikel-Lieferant (entschieden, Antwort 4)

Ist-Zustand, belegt:

- Jüngste Bestellungen zu Paaren ohne `ArticleSupplier`-Zeile werden nur gezählt und protokolliert, nicht gespeichert
  (`ArtikelLieferantSyncService.cs:293, 300-304`). Das sind auf DEV 255 Paare.
- Der Rechner kennt deshalb nur „keine letzte Bestellung“ → `HinweisFallback` „Fallback: Sage-Bestellmenge (keine Bestellung in 3 Jahren)“
  (`PurchaseProposalCalculator.cs:42-43`).

Soll: Der Hinweis in der Spalte „Hinweis“ unterscheidet die Ursachen. Die Texte sind Vorschläge und werden bei der Umsetzung festgelegt.

| Ursache | Hinweistext |
|---|---|
| letzte Bestellung bei einem Lieferanten ohne Artikel-Lieferant-Zeile (Antwort 4: Text nennt den Lieferanten) | „Letzte Bestellung bei {Kto} {Name} – kein Artikel-Lieferant in Sage“ |
| keine Bestellung (Menge > 0) im Rückblick | „Keine Bestellung in 3 Jahren – Sage-Bestellmenge“ (bzw. „– Meldebestand“, wenn auch die fehlt) |
| Einheit nicht umrechenbar (F6) | „Einheit {von} → {nach} nicht umrechenbar – Menge prüfen“ |
| kein Lieferant (weder Artikel-Lieferant noch Hauptlieferant noch Rahmenvertrag) | „Kein Lieferant in Sage hinterlegt“ |

- Entschieden (Antwort 4, vorher „Empfehlung zu Rückfrage 4“): Speicherung in einer neuen, schlanken Tabelle (siehe Technik). Deren Menge
  gilt als letzte Bestellmenge, wenn sie jünger ist als alle Bestellungen bei zugeordneten Lieferanten. Das entspricht der heutigen Regel „jüngste über alle Lieferanten“ in
  `GetLetzteBestellungenAsync`. Grund: Till will 30 statt 20 bestellen, das zeigt, dass der Einkauf die zuletzt bestellte Menge als Orientierung nimmt.

### F4 FA-Bedarfe einrechnen (entschieden; Regeln Antworten 1, 2, 7)

Ist-Zustand, belegt:

- Offene Bedarfe sind heute nur offene Lagerbestellungen (`PurchaseProposalRepository.cs:84-97`) und offene Bedarfsmeldungen (`:99-101`),
  gesteuert über das AppSetting `EinkaufOffeneBedarfe`.
- Die Artikelinfo rechnet den geplanten Verbrauch offener FAs (`ArticlesController.cs:286-327`):
  - Gerätenummern über die BOM-Weiche (`BomRepositoryMasterSwitch`).
  - FAs hierarchisch über `GetBySubOrderNumbersAsync` (`ProductionOrderRepository.cs:271`), flach über `GetByArticleNumbersAsync` (`:258`).
  - Offen = `!IsDone && !IsCancelled && !(PickingStatus?.IsDonePicking)` (`:300-302`).
  - Menge flach = BOM-Menge × `o.Quantity`, hierarchisch Summe je `SubOrderNumber` (`:324-328`).

Soll:

- Der verfügbare Bestand im Bestellvorschlag zieht zusätzlich die FA-Bedarfe ab, nach derselben Logik (ein Service, zwei Aufrufer).
- **Doppelzählungsregel (belegt, warum es nötig ist):**
  - `PartRequisition.ProductionOrderId` ist Pflicht, eine Bedarfsmeldung hängt also immer an genau einem FA. Je (FA, Artikel) zählt
    `max(FA-Bedarf, Σ offene Bedarfsmeldungen dieses FA)`. Die Bedarfsmeldung ist ein Zukauf zum FA, nicht zusätzlich zum FA.
  - `WarehouseRequisition` trägt nur `ProductionWorkplaceId`, keinen FA. Eine exakte Zuordnung Lagerbestellung ↔ FA ist damit **nicht
    möglich**. Entschieden (Antwort 1) je Artikel: `Abzug = max(FA-Teil, Σ offene Lagerbestellungen)`, wobei FA-Teil = Σ je FA aus der
    Regel davor. Damit wird nie doppelt gezählt; der Preis ist eine mögliche Unterschätzung, wenn eine Lagerbestellung ohne FA-Bezug neben
    FA-Bedarfen steht. Das Banner nennt die Regel.
  - Heute trägt `OffenerBedarf` keinen FA-Bezug (`PurchaseProposalRepository.cs:9`). Für die Bedarfsmeldungs-Regel bekommt der Record ein
    `ProductionOrderId` (aus `PartRequisition.ProductionOrderId`).
  - Die Einstellung `EinkaufOffeneBedarfe` (`AppSettingDefinitions.cs:171-173`) steuert weiter, ob Lagerbestellungen und Bedarfsmeldungen
    überhaupt zählen – jetzt für Liste **und** Artikelinfo (siehe unten und „Einstellungen“). FA-Bedarfe zählen immer.
- **Teilweise kommissionierte FAs (entschieden, Antwort 2: nur der Rest):**
  - FA-Bedarf = BOM-Menge − Σ Ausbuchungen dieses Artikels mit `StockMovement.ProductionOrderId = FA`, nie negativ.
  - Belegt ist: Kommissionier- und Buchungswege setzen `ProductionOrderId` (`StockMovementsController.cs:152/264/371/464/542`,
    `PickingTransferService.cs:217`).
  - Belegt ist auch: Die **Einbuchung** setzt sie ebenfalls (`StockMovementsController.cs:152`, `MovementType.Einbuchung`, Z. 153). Es dürfen
    deshalb nur Ausbuchungsarten zählen. Welche `MovementType`-Werte das sind, prüft P-N8.
  - Ohne diesen Abzug zählt ein halb kommissionierter FA doppelt: Der Bestand ist schon gemindert, und der Bedarf zählt trotzdem voll.
- **Einheit:** FA-Bedarfe aus der BOM haben keine Einheit am Knoten (`FaHierarchyNode`, fallstricke §18). Sie gelten wie in der Artikelinfo
  als Lagereinheit. Das ist eine ungeprüfte Annahme der Artikelinfo und wird **nicht** neu begründet: Das Banner sagt „FA-Bedarfe in
  Lagereinheit angenommen“.
- **Abdeckung AKE:** Der BOM-Cache enthält nur FAs mit `ProductionDate` im Fenster `DATEADD(week, @weeks, GETDATE())` und höchstens `@max` FAs
  (`BomCacheSyncService.cs:416-440`). FA-Bedarfe außerhalb fehlen. Pflicht-Banner: „FA-Bedarfe nur aus dem Stücklisten-Cache (FAs bis
  {Datum})“.

**Artikelinfo mit derselben Regel (Antwort 7, beide Standorte):**

- Ist (belegt): Die Artikelinfo zieht nur den geplanten FA-Verbrauch ab, voll und ohne Lagerbestellungen/Bedarfsmeldungen:
  `AvailableStock => TotalStock - PlannedConsumption` (`ArticleInfoViewModel.cs:21`), `PlannedConsumption` aus `ArticlesController.cs:324-328`,
  Anzeige `Views/Articles/Info.cshtml:109-129` (Tooltip „Gesamtbestand abzüglich geplanter Verbräuche offener Fertigungsaufträge“).
- Soll: Die Artikelinfo holt die Bedarfe des Artikels aus demselben Service wie die Liste und zieht dieselbe Summe ab: FA-Rest je FA, max mit
  Bedarfsmeldungen je FA, max mit Lagerbestellungen je Artikel, Einstellung `EinkaufOffeneBedarfe` wie in der Liste, abweichende Einheit
  nicht gerechnet. `Verfügbarer Bestand = Gesamtbestand − Bedarfe (angerechnet)`. Offene Sage-Bestellungen kommen in der Artikelinfo **nicht**
  dazu (wie heute; die Liste hat dafür die eigene Spalte).
- Die Artikelinfo zeigt die Aufschlüsselung unter dem verfügbaren Bestand: FA-Bedarf (Rest), offene Bedarfsmeldungen, offene
  Lagerbestellungen, **angerechnet** (nach max-Regel), dazu ein Satz zur Regel („Bedarfsmeldung und Lagerbestellung werden nicht zusätzlich zum
  FA-Bedarf gezählt; kommissionierte Mengen sind schon ausgebucht“). Tooltip entsprechend anpassen. Nicht umrechenbare Bedarfe (abweichende
  Einheit) werden genannt, nicht still verworfen.
- Die FA-Tabelle („Teil enthalten in folgenden Fertigungsaufträgen“, `Info.cshtml:137-176`) bekommt eine Spalte „Rest“ je FA, damit der Abzug
  nachvollziehbar ist.
- Gilt an **beiden** Standorten (AKE flach, IDEAL hierarchisch); die Artikelinfo ist nicht an `EinkaufAktiv` gebunden. Changelog-Hinweis
  „sichtbare Änderung für Lager/Produktion“: Der verfügbare Bestand kann sich gegenüber der Vorversion ändern (kleiner durch Lagerbestellungen/
  Bedarfsmeldungen, größer durch den FA-Rest).

### F5 Dringlichkeit nach Bedarfstermin und WBZ (entschieden, Antwort 6)

Ist-Zustand, belegt: `Kritisch`, sobald `verfuegbar < 0` (`PurchaseProposalCalculator.cs:62`), egal wann der Bedarf fällig ist. Bei
EK023973 liegen die Bedarfe laut Till im April.

Soll:

- Bedarfstermin:
  - FA-Bedarf: `ProductionOrder.ProductionDate`.
  - Lagerbestellung und Bedarfsmeldung: kein Termin, sie gelten als **sofort**.
  - FA ohne `ProductionDate`: gilt als sofort (vorsichtig).
- ~~`verfuegbarFenster = BestandWms + OffenBestellt − Σ Bedarfe mit Termin ≤ heute + WBZ`~~ (ueberholt durch Antwort 6: nicht alle offenen
  Bestellungen zählen im Fenster).
- `verfuegbarFenster = BestandWms + OffenBestelltFenster − Σ Bedarfe mit Termin ≤ heute + WBZ`.
  - `OffenBestelltFenster` = Σ Rest der offenen Bestellpositionen mit `Liefertermin ≤ heute + WBZ` (Antwort 6). Überfällige Positionen
    (Termin < heute) zählen mit, sie erfüllen die Bedingung.
  - Quelle des Liefertermins: Cache `SagePurchaseOrderLines.Liefertermin` (`SagePurchaseOrderLine.cs:55-56`, Spaltentyp `date`
    `ApplicationDbContext.cs:1412`). Geschrieben wird er im Abgleich als Positions-Liefertermin, ersatzweise Beleg-Liefertermin
    (`EinkaufBestellungenSyncService.cs:141`). Dieselbe Spalte liest die Lieferverzug-Mail (`LieferverzugPlaner.cs:80`) und schon heute die
    Spalte „Liefertermin“ der Liste (`PurchaseProposalsController.cs:181-183`). Rest je Position nach der einen Rest-Regel
    `ErfasstJeBelPos.Rest` (`:181`), wie bisher.
  - Positionen **ohne** Liefertermin erfüllen „≤ heute + WBZ“ nicht und zählen im Fenster nicht (Wortlaut Antwort 6; vorsichtig, eher
    „Kritisch“). Sie zählen weiter in `verfuegbar` (alle). Hat eine Zeile solche Positionen, steht der Hinweis „offene Bestellung ohne
    Liefertermin – für Dringlichkeit nicht gezählt“.
  - `Kritisch`, wenn `verfuegbarFenster < 0`. Dann reicht der Bestand nicht bis zum frühesten Liefertermin einer neuen Bestellung.
  - sonst `Nachbestellen`, wenn `verfuegbar` (alle Bedarfe, alle offenen Bestellungen) ≤ Meldebestand (wie heute).
- WBZ wie heute aus Lieferant bzw. `SageDetail` (`PurchaseProposalsController.cs:209`). Ohne WBZ gilt 0, also nur heute fällige Bedarfe. Das
  wird als Hinweis „ohne WBZ“ angezeigt.
- Neue Spalte „Nächster Bedarf“ (Datum, Server-Spaltenfilter als Datum, gefiltert nach der Berechnung, ADR 0005).

### F6 Mengenregel neu (entschieden, Antwort 3: Variante A)

Variante A gilt. Die Tabelle bleibt als Begründung; B und C werden nicht gebaut. Till bestätigt die Regel am Bildschirm (TS-154.6); eine
Änderung danach betrifft nur `PurchaseProposalCalculator.Berechne`.

Bezeichnungen: `v` = verfügbar (F4 inkl. FA-Bedarfe), `M` = Meldebestand, `L` = letzte Bestellmenge (in Lagereinheit, F7), `MBM` =
Mindestbestellmenge (in Lagereinheit, F7). Alles in der Lagereinheit.

| Variante | Regel | EK015744-Fall (L reicht nicht) | Bewertung |
|---|---|---|---|
| **A (entschieden)** | `V = L`; gilt `v + L ≤ M` („reicht nicht“, Till bestätigt), dann `V = M − v + 1 Lagereinheit` (Fehlmenge bis über den Meldebestand); danach `V = max(V, MBM)` | knapp über M | Tills Vorschlag; kein Vielfaches |
| B (heute) | `V = (⌊(M−v)/L⌋+1) × L`, dann MBM nur bei Faktor 1 | 2 × L | Till: „Verdopplung zu hoch“ |
| C | `V = (M − v) + L` | M + L | großzügig, keine Grundlage im Gespräch |

- Ohne L gilt: Sage-Bestellmenge (umgerechnet, F7). Sonst `M − v + 1`, wie heute `f > 0 ? f : Meldebestand` (`PurchaseProposalCalculator.cs:86`),
  nur mit der neuen „über“-Definition.
- Die MBM gilt **immer**, sobald sie umrechenbar ist (F7). Den Wächter „nur Faktor 1“ (`MindestmengeVergleichbar`, `:49-52`) ersetzt die
  Umrechnung. Nicht umrechenbar → MBM nicht anwenden, Hinweis (wie heute).
- EK000051 (MBM 300 > L 200): bleibt 300 (Till: „so lassen“). Das ist ein Testfall.
- Rundung: `V` wird auf `DezimalstellenLager` gerundet, und zwar **aufwärts**. Ohne Dezimalstellen gilt eine ganze Einheit.

### F7 Einheiten-Umrechnung

Belegt (E0 der Grundspec, Antworten Till):

- `MengeBasis = Menge × MengeEKUmrechnung`. `Menge` ist in der Bestelleinheit.
- Die MBM steht je Artikel-Lieferant in der Einkaufseinheit, wenn der Faktor ≠ 1 ist (EK002400: 24 PKG, Faktor 3; EK007623: 5 Stk, Faktor 5).
- Laut Sage-Maske (Till):
  - EK003604: Bestellmenge in Packung
  - EK002400: MBM in Packung
  - EK007623: MBM in Stück
- EK003604 hat Basis Stk und Lager PKG. Die Sage-Bestellmenge in Packung passt also zur **Lagereinheit**. Das ist eine Stütze für BV-1, aber
  nur ein Beispiel; ob SData denselben Wert liefert, prüft P-N1.
- **Nicht belegt:**
  - Richtung von `UmrechnungsFaktorLME` (Basis ÷ oder × Faktor = Lager) → P-N7
  - Einheit der Cache-Mengen `PositionGGBestellt`/`PositionGGGeliefert` (EBB) → P-N3
  - Auffälligkeit EK003604: jüngste Bestellung Menge 2 = Basis 2 trotz PKG → Stk → P-N4

Soll: **eine** reine Umrechnungsfunktion (Helper ohne DB) `Einheiten.InLager(menge, quelle)`. Die Quelle ist eine der folgenden; jede wird in
die Lagereinheit gebracht:

| Quelle | Feld | Einheit (laut Beleg/Prüfung) | Weg nach Lager |
|---|---|---|---|
| letzte Bestellung | `MengeBasis` (`ArtikelLieferantSyncService.cs:48`) | Basis | Basis → Lager über `UmrechnungsFaktorLME` (Richtung P-N7) |
| Sage-Bestellmenge | `ArticleSageDetail.Bestellmenge` | Lager (P-N1 bestätigt) | keine; bis P-N1 belegt: bei Basis ≠ Lager gesperrt (BV-1) |
| MBM | `ArticleSupplier.Mindestbestellmenge` | Einkaufseinheit | × `UmrechnungsfaktorEK` → Basis → Lager (P-N2) |
| offene Bestellung | Cache `SagePurchaseOrderLines` | Basis (P-N3) | Basis → Lager |
| Bedarf Lagerbestellung/Bedarfsmeldung | `Unit` | frei | gleich Lager: übernehmen; gleich Basis: umrechnen; sonst überspringen + Banner (wie heute `PurchaseProposalsController.cs:192`) |

- Offene Bestellungen mit `EinheitAbweichend` werden **umgerechnet und eingerechnet**, statt sie auszuschließen
  (`PurchaseProposalsController.cs:179-183`). Das löst EK005564. Das Banner „Positionen mit abweichender Einheit“ zählt danach nur noch die
  nicht umrechenbaren.
- Ist eine Umrechnung nicht möglich (Faktor 0/leer, Einheit unbekannt), wird die Menge nicht verwendet, und die Zeile bekommt einen Hinweis.
  Es wird nie still mit Faktor 1 gerechnet.

**Ersatzlösung, solange P-N1/P-N2/P-N3/P-N4/P-N7 nicht beantwortet sind (Stand 2026-11-16: alle offen).** E7 wird trotzdem gebaut, nur mit
den Daten, die schon im WMS liegen, und ohne eine unbelegte Richtung oder Bedeutung anzunehmen:

- Vorhandene Daten (belegt): `ArticleSageDetail.Basismengeneinheit`, `.Lagermengeneinheit`, `.UmrechnungsFaktorLME`
  (`ArticleSageDetail.cs:30-33`); `ArticleSupplier.Einkaufsmengeneinheit`, `.UmrechnungsfaktorEK` (`ArticleSupplier.cs:22-23`); je Cache-Position
  `Mengeneinheit`, `MengeBasiseinheit`, `Lagermengeneinheit` (`SagePurchaseOrderLine.cs:46-48`). Einen Faktor speichert der Cache je Position
  **nicht**.
- Der Helper `Einheiten.InLager` rechnet in der Ersatzlösung nur, wenn keine Richtung nötig ist:
  - **Quell- und Lagereinheit gleich (Kürzel, getrimmt, Groß/Klein egal) und der zugehörige Faktor leer oder 1** → Menge übernehmen.
  - Kürzel gleich, aber Faktor ≠ 1 → nicht umrechenbar, Hinweis „Einheit gleich, Faktor {f} – Menge prüfen“. Grund: Sage kennt gleiche
    Einheit mit Faktor ≠ 1 (Stk/Stk × 5, `PurchaseProposalCalculator.cs:45-46`).
  - Kürzel verschieden → nicht umrechenbar, Hinweis „Einheit {von} → {nach} nicht umrechenbar – Menge prüfen“, Banner zählt. Kein Faktor 1,
    keine angenommene Richtung von `UmrechnungsFaktorLME` (P-N7) oder `UmrechnungsfaktorEK` (P-N2).
- Je Quelle bedeutet das bis zu den Antworten:
  - letzte Bestellung (`MengeBasis`): nur bei Basis = Lager (wie heute `PurchaseProposalsController.cs:219-223`), sonst Hinweis.
  - Sage-Bestellmenge: nur bei Basis = Lager; bei Basis ≠ Lager keine Menge, Hinweis (BV-1; heute wird sie ungeprüft genommen, `:218`).
  - MBM: Wächter `MindestmengeVergleichbar` (`PurchaseProposalCalculator.cs:49-52`) bleibt bis P-N2 – gleiche Einheit und Faktor EK = 1.
  - offene Bestellung: Die Cache-Mengen sind laut Modell-Doku in der Basiseinheit (`SagePurchaseOrderLine.cs:9-11, 40-45`, E0 B-2; P-N3
    bestätigt). `EinheitAbweichend` ist gesetzt, wenn Bestell- **oder** Basiseinheit ≠ Lager (`:49-51`). Ersatzregel: Position mit
    `MengeBasiseinheit` = `Lagermengeneinheit` wird eingerechnet, auch wenn `EinheitAbweichend` (nur die Bestelleinheit weicht ab). Nur
    Positionen mit Basis ≠ Lager bleiben draußen und zählen im Banner. Ob EK005564 damit schon aus der Liste fällt, hängt an seinen Einheiten
    (P-N3); sonst erst nach P-N7.
- Sobald eine Prüfabfrage beantwortet ist, wird die zugehörige Regel im Helper freigeschaltet (Richtung/Faktor aus der Antwort), mit
  Unit-Test aus den Prüfwerten. Das kann im selben Epic oder als Folgeetappe passieren; der Blocker steht bis dahin in der Etappentabelle.

### F8 Lieferant fehlt (EK023973)

Belegt (`PurchaseProposalsController.cs:199-207`): Der Lieferant kommt nur aus `GetHauptlieferantenAsync` (`ArticleSupplier.IstHauptlieferant`),
sonst als Kto aus `SageDetail.Hauptlieferant`, sonst „kein Lieferant“. Für EK023973 ist also **beides leer**, wenn kein Lieferant erscheint.

- Woran das liegt, zeigen P-N5a (WMS) und P-N5b (Sage). Erst danach steht fest, ob ein Datenproblem in Sage vorliegt oder ein Abgleichfehler.
- Soll unabhängig vom Ergebnis: Fehlt ein Hauptlieferant, aber es gibt genau einen Artikel-Lieferanten oder eine letzte Bestellung (auch ohne
  Zuordnung, F3), dann wird dieser als Lieferant angezeigt, mit Hinweis „kein Hauptlieferant – {Quelle}“. Gibt es mehrere Artikel-Lieferanten
  und keinen Hauptlieferanten, gilt der Lieferant der jüngsten Bestellung, sonst bleibt die Zelle leer und der Hinweis lautet „mehrere
  Lieferanten, kein Hauptlieferant“.

### F9 Rahmenverträge (EK014367; Blocker P-N6, P-N9; entschieden, Antwort 8)

Belegt: Im Code gibt es **keinen** Leseweg für EK-Rahmenverträge (Grep `Rahmenvertrag` im Worktree: nur Feldlisten im Brain;
`IstRahmenabruf` betrifft Abrufe EBZ, nicht den Vertrag). In der Sage-DB `ake_TEST2026` gibt es die Belegart „Rahmenvertrag EK“
(522 Belege, E0 der Grundspec).

Soll (entschieden, Antwort 8; mit Till bestätigen): Hat ein Artikel einen offenen EK-Rahmenvertrag, hat dessen Lieferant Vorrang vor dem
Hauptlieferanten. Die Restmenge des Rahmens erscheint nur als Hinweis („Rahmenvertrag {Beleg} bei {Lieferant}, Rest {n} {Einheit}“). Die
Vorschlagsmenge bleibt nach F6; der Rahmen ist **keine** Mengenquelle.
Leseweg (SData-Ressource und `where`) erst nach P-N6/P-N9 festlegen. **Keine Annahme über Belegkennzeichen oder Spalten.**

Ohne Antworten auf P-N6/P-N9 wird in diesem Epic **nichts** für Rahmenverträge gebaut (kein leerer Platzhalter-Leser): E8 wird übersprungen,
der Blocker in der Etappentabelle vermerkt, SQL/156 bleibt unverbraucht. Die Lieferant-Kaskade aus F8 bleibt so geschrieben, dass der
Rahmenvertrag später als erste Stufe davor kommt.

### F11 Matchcode-Spalte und Link zur Artikelinfo (Nachtrag 19:49)

- Neue Spalte **Matchcode** (`Article.Matchcode`, gleiche Eigenschaft wie in der Artikelinfo `ArticlesController.cs:335`) direkt nach
  „Artikel“: standardmäßig sichtbar, im Zahnrad (`ColumnDefinitions.cs:701ff`, Key `matchcode`), Server-Spaltenfilter (`ColumnMap` +
  `<th data-col-key="matchcode">`), im CSV-Export (csv-export.js nimmt die sichtbaren Spalten).
- Die Artikelnummer wird ein Link auf `/Articles/Info?articleNumber={Nr}&returnUrl={Pfad+Query der Liste}` im selben Tab.
- Rücksprung: Die Artikelinfo bekommt einen optionalen Parameter `returnUrl`. Ist er gesetzt und `Url.IsLocalUrl(returnUrl)`, zeigt sie oben
  einen Knopf „Zurück zu Bestellvorschlägen“ auf genau diese URL; Filterkarte, Spaltenfilter, Seite und Seitengröße bleiben so erhalten,
  weil sie in der Query stehen. Muster wie `TrackingController.cs:267-268` / `Views/Tracking/Index.cshtml:168`. Eine Suche in der Artikelinfo
  (Formular) trägt `returnUrl` als verstecktes Feld weiter.
- **Zugriff (belegt):** Die Artikelinfo verlangt `RequireMasterDataReadAccess` (`ArticlesController.cs:10`) = admin, `stammdaten_lesen`
  oder `stammdaten` (`CurrentUserService.cs:88-89`). Ein reiner `einkauf`-Benutzer hätte keinen Zugriff. Bis Restfrage R1 entschieden ist:
  Der Link erscheint nur, wenn der Benutzer `HasMasterDataReadAccessAsync()` hat; sonst bleibt die Artikelnummer reiner Text mit Tooltip
  „Artikelinfo braucht die Rolle Stammdaten lesen“. Keine Änderung am Filter ohne Entscheidung.

### F10 Rolle `einkauf` (entschieden, nur Prüfung)

Die drei Pflichtstellen sind seit v1.99.20 vorhanden:

- `RoleKeys.cs:29` und `RequireEinkaufAccess` / `CanAccessEinkaufAsync` (`controller.md:82, 181-182`)
- `Views/Users/RoleOverview.cshtml:219-226`

Keine Änderung. Anlage über das Manifest `Pruefung/RolleEinkauf` (Grundspec). Die Personen weist Gerald in /Roles zu, nicht im Code.

## Technischer Loesungsentwurf

Muster: Repository-Pattern (ADR 0001), Listen-View-Pattern (ADR 0005), Rolle plus Gate kumulativ (ADR 0006/0011), ServiceSettings mit Katalog
(ADR 0008), Aktivitätsprotokoll (ADR 0010), Einstellungen (ADR 0020), Lokalzeit `DateTime.Now` (ADR 0025), Manifest (ADR 0022/0023).

1. **Dienst (E1):**
   - Schritt `LetzteBestellungAsync` wird zu einer eigenen öffentlichen Methode bzw. Klasse `LetzteBestellungSyncService`, mit eigenem
     `ISyncLogger` als **letztem** Ctor-Parameter. `ArtikelLieferantSyncService.cs:180` ruft ihn nicht mehr auf.
   - Der SyncWorker ruft ihn direkt nach dem Artikel-Lieferant-Block (`SyncWorker.cs:463-473`) auf, gleiches Gate, nur wenn
     `LetzteBestellungPlaner.Faellig(jetzt, letzterStart, uhrzeiten)`. Das ist ein reiner Helper nach dem Vorbild `LieferverzugPlaner.ImZeitfenster`.
   - Marker `letzterStart` = jüngste Zeile in `SyncLogs` mit `Service = 'LetzteBestellung'` und Startmeldung „Run gestartet“
     (`SyncLogger.cs:21`). Der Index `IX_SyncLogs_Service_Level` existiert. Damit gibt es keine neue Tabelle, und der Neustart ist abgedeckt.
     Die Aufbewahrung von 180 Tagen ist unkritisch.
   - Neuer Name in `SyncLogServices.All` + `DisplayNames`.
2. **Basismenge 0 (E1):** Filter `MengeBasis > 0` vor der Jüngsten-Bestimmung (`ArtikelLieferantSyncService.cs:283`). Test: Die jüngere
   0-Position wird übersprungen, die ältere mit Menge > 0 gilt.
3. **Speicherung ohne Zuordnung (E2, Antwort 4):**
   - Neue Entity `ArticleLastOrderUnassigned`: `Id`, `ArticleId` (FK), `Kto` (wie `ArticleSupplier`), `Menge` (Basis), `Am`, `Beleg`
     (45, wie `BelegnummerMaxLaenge`), Audit aus `AuditableEntity`.
   - UNIQUE (`ArticleId`, `Kto`). Je Lauf vollständig ersetzt; Empty-Guard wie heute: Bei Lesefehler oder 0 EBB wird nichts gelöscht.
   - Lesen in `PurchaseProposalRepository.GetLetzteBestellungenAsync` als zweite Quelle. Die jüngste gewinnt, die Quelle wird mitgegeben.
   - ponytail: eigene Tabelle statt Umbau der `ArticleSupplier`-Spalten (nicht destruktiv, additiv).
4. **FA-Bedarf (E3/E4):**
   - Neuer `IPlannedConsumptionService` (eine Implementierung, Grund: zwei Aufrufer und testbar per Moq, wie die übrigen Services) mit
     `GetJeArtikelAsync(IReadOnlyCollection<string> artikel)`.
   - Er liefert je Artikel eine Liste (FA-Id, FA-Nummer, `ProductionDate`, Menge = Rest). ~~Die Summe für die Artikelinfo bildet der
     Aufrufer.~~ (ueberholt durch Antwort 7: die angerechnete Summe bildet der Service, siehe unten.)
   - Dazu Listen-Varianten der zwei BOM-Methoden (`GetDeviceArticleNumbersByComponentAsync`, `GetComponentMengePerDeviceAsync`) in
     `BomCacheRepository` (`:145-172`), `FaHierarchyBomRepository` (`:204-223`) und `BomRepositoryMasterSwitch`. Je Block eine Abfrage, kein N+1.
     Die Einzelmethoden rufen die Listen-Variante auf.
   - ~~`ArticlesController.cs:286-327` ruft den Service auf; das Verhalten bleibt gleich (Rückfrage 7). Ein Test vergleicht alte und neue
     Summe …~~ (ueberholt durch Antwort 7: dieselbe Regel auch in der Artikelinfo).
   - Der Service liefert je Artikel zusätzlich die offenen Lagerbestellungen und Bedarfsmeldungen (aus `PurchaseProposalRepository`
     `GetOffeneLagerbestellungenAsync`/`GetOffeneBedarfsmeldungenAsync`, Einstellung `EinkaufOffeneBedarfe`) und wendet die Regeln aus F4
     **an einer Stelle** an: FA-Rest, max je FA mit Bedarfsmeldungen, max je Artikel mit Lagerbestellungen. Ergebnis je Artikel: FA-Bedarf
     (Rest) je FA mit Termin, Σ Bedarfsmeldungen, Σ Lagerbestellungen, angerechnete Summe, Zahl nicht gerechneter Bedarfe (Einheit).
   - Beide Aufrufer nehmen dieses Ergebnis: `ArticlesController.Info` (ein Artikel) und `PurchaseProposalsController` (Block aller
     Meldebestand-Artikel). `ArticleInfoViewModel.PlannedConsumption` wird durch die Aufschlüsselung ersetzt, `AvailableStock` rechnet mit der
     angerechneten Summe.
   - Tests: Gleichheitstest „Liste und Artikelinfo liefern für denselben Artikel dieselbe angerechnete Summe“ (AKE-flach und
     IDEAL-hierarchisch, `BomDiResolutionTests`-Muster für die Weiche); Regeltests mit Zahlen (FA + Bedarfsmeldung, FA + Lagerbestellung,
     Teilkommissionierung, Einbuchung mit FA-Bezug zählt nicht).
   - Rest-Abzug (Antwort 2) über eine Repository-Abfrage Σ Ausbuchungen je (FA, Artikel), für beide Aufrufer. Welche `MovementType`-Werte
     zählen (`MovementType.cs:5-9`: Einbuchung, Ausbuchung, Umbuchung, SageEinbuchung, SageAusbuchung), legt P-N8 fest; bis dahin nur
     `Ausbuchung` (die anderen nicht annehmen).
   - `OffenerBedarf` (`PurchaseProposalRepository.cs:9`) bekommt `int? ProductionOrderId` für die Bedarfsmeldungs-Regel.
5. **Rechner (E4–E7):**
   - `PurchaseProposalInput` bekommt `Bedarfe` als Liste (Menge, Termin?), offene Bestellungen als Liste (Rest, Liefertermin?) statt einer
     Summe (Antwort 6; heute summiert `BaueZeilen` je Artikel, `PurchaseProposalsController.cs:180-183`), `Wbz`, umgerechnete Mengen und die
     Hinweisquelle.
   - Dringlichkeit (F5), Mengenregel A (F6) und Hinweise (F3) bleiben in `PurchaseProposalCalculator.Berechne`. Die Rechenregel steht an
     **einer** Stelle.
   - Der Umrechnungs-Helper (F7) ist eine statische Klasse neben dem Rechner. Jeder Testfall-Artikel bekommt einen Unit-Test mit den Zahlen
     aus den Prüfabfragen.
6. **Controller/Liste:**
   - `BaueZeilen` (`PurchaseProposalsController.cs:174-239`): FA-Bedarfe, Umrechnung der offenen Bestellungen, Lieferant-Kaskade F8.
   - ColumnMap (`:45-72`) bekommt „Matchcode“ (Text), „Nächster Bedarf“ (Datum) und „FA-Bedarf“ (Zahl); Typen in
     `ColumnFilterSyntax.cs:78` (`naechster-bedarf` D, `fa-bedarf` N); Zahnrad-Einträge in `ColumnDefinitions.cs:701ff`.
   - Neue `<th>` mit `data-col-key`, Pagination unverändert. Artikelnummer als Link mit `returnUrl` (F11), Link nur bei
     `HasMasterDataReadAccessAsync()`.
   - Artikelinfo: Parameter `returnUrl` (nur lokal), Zurück-Knopf, verstecktes Feld im Suchformular (F11).
   - Banner (Pflicht, „melden statt still behandeln“): Doppelzählungsregel, BOM-Cache-Fenster, FA-Bedarfe in Lagereinheit, nicht umrechenbare
     Positionen/Bedarfe.
7. **Rahmenvertrag (E8):** erst nach P-N6/P-N9. Wahrscheinlich ein eigener Cache im Dienst analog `EinkaufBestellungenSyncService`. Ist
   dafür eine Tabelle nötig, bekommt sie eine **eigene** SQL-Nummer (156) in dieser Etappe; reserviert, nicht verbraucht. Ohne Belege:
   nichts bauen (F9).
8. **Dienst-Uhrzeiten (E1, Antwort 5):** Defaults 3 und 12; `LetzteBestellungPlaner.Faellig` holt einen verpassten Termin nach dem
   Dienststart genau einmal nach (Termin heute oder gestern vor jetzt, kein Lauf seit dem Termin).

**Laufzeit der Liste:** Der FA-Bedarf für alle Meldebestand-Artikel kommt über Listen-Abfragen in Blöcken (wie
`SdataArtikelReader.Blockgroesse = 200`). Der vorhandene Cache der Liste (`Index`, nur bei `stand != null`) bleibt. Die Messung auf AKE-DEV
(Ladezeit vorher/nachher) gehört zum QA-Nachweis.

## Migrations-/SQL-Auswirkungen

- **SQL/155** `155_AddArticleLastOrderUnassigned.sql` (E2):
  - EF-Migration `AddArticleLastOrderUnassigned`.
  - `IF OBJECT_ID(N'dbo.ArticleLastOrdersUnassigned', N'U') IS NULL CREATE TABLE …`, DDL im eigenen Batch.
  - UNIQUE-Index (`ArticleId`, `Kto`) und FK auf `Articles` mit eigenem Guard.
  - `__EFMigrationsHistory`-Insert im separaten Batch.
  - Additiv, nicht destruktiv.
- `SQL/00_FreshInstall.sql` an **beiden** Stellen (Schema und `MigrationId`).
- `SQL/AgentJobs/`: kein Treffer für `ArticleSupplier`, `SyncLogs`, `PurchaseProposal` (Grep 2026-11-16). Nicht betroffen.
- **SQL/156** nur reserviert für E8 (Rahmenvertrag-Cache), falls nötig.
- InMemory erzwingt UNIQUE nicht (Memory „InMemory Unique Indexes“). Ein App-Guard (Dictionary je Lauf) verhindert Duplikate vor dem Speichern.
- Nummern beim Start des Dev-Laufs neu prüfen. Stand 2026-11-16:
  - höchste vorhandene: 152
  - 151 bedingt reserviert (FreshInstall-Indizes)
  - 153 Arbeitsgang-Mapping
  - 154 BDE-Teileliste

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Sync:LetzteBestellungUhrzeitNacht` (Anzeigename „Letzte Bestellung – Uhrzeit nachts“) | Int | Auswahl 0–23, Einheit „Uhr“ (IntMeta Min/Max wie `Lieferverzug:Uhrzeit`), Server validiert | 3 (Antwort 5) | Ab dieser Stunde liest der Dienst einmal die letzten Bestellungen (3 Jahre EBB/EBZ) aus Sage neu | Bestellvorschläge: letzte Bestellmenge, Lieferant ohne Zuordnung | beide | – (nur lesend; Hinweis im Text: „Lauf dauert einige Minuten, in der Zeit warten Offene Bestellungen und Lieferverzug“) |
| `Sync:LetzteBestellungUhrzeitMittag` (Anzeigename „Letzte Bestellung – Uhrzeit mittags“) | Int | Auswahl 0–23, Einheit „Uhr“, Server validiert | 12 (Antwort 5) | zweiter täglicher Lauf | wie oben | beide | – (wie oben) |
| `EinkaufOffeneBedarfe` (AppSetting, **nur Beschreibung geändert**, `AppSettingDefinitions.cs:171-173`) | MultiChoice | unverändert: Lagerbestellungen, Bedarfsmeldungen (Auswahl) | unverändert „Lagerbestellungen,Bedarfsmeldungen“ | neu: mindert den verfügbaren Bestand in Bestellvorschlägen **und** in der Artikelinfo; angerechnet nach max-Regel mit den FA-Bedarfen (Antwort 1, 7) | Wirkung/Verwendung ergänzen: „Einkauf → Bestellvorschläge (Spalte „offene Bedarfe“) und Artikelinfo (Verfügbarer Bestand)“ | beide | – (nur lesend) |

- Beide Einträge kommen in `ServiceSettingDefinitions.All` mit Meta (Name, Verwendung, Standort). Der Drift-Guard-Test greift (ADR 0008).
- Sind beide gleich, gibt es einen Lauf pro Tag. Das steht im Beschreibungstext; keine eigene Validierung.
- `Sync:ArtikelLieferantEnabled` bleibt unverändert und schaltet auch den neuen Lauf.
- Das AppSetting `EinkaufOffeneBedarfe` bleibt in Key, Werten und Default unverändert; nur Wirkungs-/Verwendungstext wird ergänzt (Zeile
  oben). Ob FA-Bedarfe einen eigenen Wert in dieser Auswahl bekommen
  (`AppSettingKeys.EinkaufBedarfWerte`), ist **nicht** gefordert. FA-Bedarfe zählen immer (Entscheidung Gerald „ja“). ponytail: kein Schalter,
  bis jemand ihn verlangt.

## Audit-Feld-Auswirkungen

- `ArticleLastOrderUnassigned` erbt `AuditableEntity`. Der Dienst setzt `CreatedBy`/`ModifiedBy` = Service-Name `LetzteBestellung` (bisher
  `ArtikelLieferantSync`), `ModifiedAt = DateTime.Now`, bei jedem Update.
- `ArticleSupplier.LetzteBestellung*`: Schreiber wird `LetzteBestellung`. `ModifiedAt/By/ByWindows` werden wie heute gesetzt.
- Bestellvorschläge selbst bleiben nur lesend (keine POST-Action, Wächter-Test der Grundspec). Keine weiteren Audit-Wirkungen.

## Akzeptanzkriterien

1. Ein Artikel-Lieferant-Abgleich im normalen Zyklus liest `EinkaufPositionenAllInfo` **nicht** mehr. Im Aktivitätsprotokoll gibt es unter
   „Artikel-Lieferant“ keine Counts zur letzten Bestellung mehr.
2. Der Lauf „Letzte Bestellung (Einkauf)“ startet genau einmal nach jeder konfigurierten Uhrzeit. Ein Dienstneustart nach dem Lauf löst
   keinen zweiten Lauf aus. Ein Neustart nach einer verpassten Uhrzeit löst genau einen aus (Antwort 5).
3. Unit-Test `LetzteBestellungPlaner.Faellig`: Nacht- und Mittagstermin, Mitternachtswechsel, beide Uhrzeiten gleich, kein bisheriger Lauf.
4. Beide Uhrzeiten erscheinen unter /ServiceSettings als Auswahl 0–23 mit Anzeigename, Wirkung, Verwendung, Default und Standort. Der Wert 24
   wird beim Speichern abgelehnt.
5. Eine Position mit `MengeBasis = 0` wird nie letzte Bestellung (Unit-Test; auf DEV EK023626 nachprüfen).
6. Hat die jüngste Bestellung einen Lieferanten ohne Artikel-Lieferant-Zeile, zeigt der Hinweis Kto und Name mit „kein Artikel-Lieferant in
   Sage“. Der Text „keine Bestellung in 3 Jahren“ erscheint dann nicht.
7. ~~Die Artikelinfo zeigt für denselben Artikel vor und nach der Umstellung denselben „Verfügbaren Bestand“ …~~ (ueberholt durch Antwort 7.)
   **Neu:** Liste und Artikelinfo zeigen für denselben Artikel dieselben Bedarfe (FA-Bedarf Rest, Bedarfsmeldungen, Lagerbestellungen,
   angerechnete Summe), AKE-flach und IDEAL-hierarchisch (Gleichheitstest plus DEV-Stichprobe an drei Artikeln, darunter EK023973). Der
   „Verfügbare Bestand“ der Artikelinfo = Gesamtbestand − angerechnete Summe; die Aufschlüsselung steht sichtbar darunter.
7a. Ohne Lagerbestellungen, Bedarfsmeldungen und Teilkommissionierung zeigt die Artikelinfo denselben verfügbaren Bestand wie vorher
   (Regressionstest der FA-Summe flach/hierarchisch).
8. Ein Artikel mit FA-Bedarf erscheint im Bestellvorschlag mit dem FA-Bedarf in der Spalte „FA-Bedarf“, und `verfügbar` ist um diesen
   Betrag kleiner.
9. FA und Bedarfsmeldung zum selben FA und Artikel werden nur einmal gezählt (max). Lagerbestellung und FA-Bedarf zum selben Artikel:
   Abzug = max(FA-Teil, Σ Lagerbestellungen) (Unit-Test mit Zahlen, z. B. FA 10 + Lagerbestellung 4 → 10; FA 3 + Lagerbestellung 8 → 8).
10. Ein teilweise kommissionierter FA zählt nur mit dem Rest (BOM 10, ausgebucht 6 → 4), in Liste und Artikelinfo. Eine Einbuchung mit
    FA-Bezug mindert den Rest nicht.
11. EK023973: Mit Bedarfen nur nach heute + WBZ steht „Nachbestellen“, nicht „Kritisch“. Mit einem Bedarf ohne Termin, der `verfügbar < 0`
    macht, steht „Kritisch“.
11a. Offene Bestellungen zählen für „Kritisch“ nur mit Liefertermin ≤ heute + WBZ (Unit-Test: Bestand 0, Bedarf heute 5, Bestellung 5 mit
    Termin heute + WBZ + 1 → „Kritisch“; dieselbe Bestellung mit Termin heute + WBZ → nicht kritisch). Position ohne Liefertermin zählt im
    Fenster nicht, die Zeile zeigt den Hinweis „offene Bestellung ohne Liefertermin – für Dringlichkeit nicht gezählt“.
12. Die Spalte „Nächster Bedarf“ lässt sich serverseitig filtern und zeigt das Datum nach der Berechnung.
13. Mengenregel A: L reicht → V = L. L reicht nicht → V = M − v + 1 Lagereinheit. MBM größer → MBM (EK000051: 300). Nie ein Vielfaches von L
    (EK015744).
14. EK005564 (offene Bestellung mit abweichender Einheit): Ersatzlösung – eine Position mit `MengeBasiseinheit` = `Lagermengeneinheit` wird
    trotz `EinheitAbweichend` eingerechnet; reicht die offene Menge, steht der Artikel nicht mehr in der Liste. Ist bei EK005564 Basis ≠
    Lager, bleibt die Position bis P-N3/P-N7 draußen, mit Hinweis in der Zeile.
15. EK002400 und EK007623: Bis P-N2 belegt ist, gilt der Wächter (gleiche Einheit und Faktor EK 1); sonst „MBM nicht umrechenbar“ als
    Hinweis, keine MBM-Anwendung. Nach P-N2 wird die MBM mit den Prüfwerten richtig in die Lagereinheit umgerechnet (Unit-Test).
16. Sage-Bestellmenge bei Basis ≠ Lager: Bis P-N1 belegt ist, gibt es keine Menge, sondern einen Hinweis (BV-1; EK003604 ist der Testfall).
    Nach P-N1 wird sie nach der belegten Einheit verwendet.
17. Ist eine Umrechnung nicht möglich (Kürzel verschieden ohne belegte Richtung, Faktor 0/leer, gleiche Kürzel mit Faktor ≠ 1), steht ein
    Hinweis in der Zeile, und das Banner zählt die Fälle. Es wird nie mit Faktor 1 geraten (Unit-Test je Fall).
18. EK023973 zeigt einen Lieferanten nach F8 oder den Hinweis „Kein Lieferant in Sage hinterlegt“. Die Ursache ist mit P-N5 in der Spec
    dokumentiert.
19. (E8) EK014367 zeigt den Rahmenvertrag-Lieferanten (Vorrang vor dem Hauptlieferanten) und den Hinweis mit Restmenge, sobald P-N6/P-N9
    belegt sind; die Vorschlagsmenge bleibt nach F6. Ohne Belege: E8 übersprungen, Blocker in der Etappentabelle.
20. `/PurchaseProposals` bleibt nur für admin/einkauf mit `EinkaufAktiv = true` erreichbar. Eine andere Rolle bekommt Zugriff verweigert.
20a. Spalte „Matchcode“ ist standardmäßig sichtbar, im Zahnrad abwählbar, serverseitig filterbar und im CSV-Export enthalten.
20b. Die Artikelnummer verlinkt auf die Artikelinfo; „Zurück zu Bestellvorschlägen“ führt auf dieselbe Seite mit denselben Filtern
     (Filterkarte, Spaltenfilter, Seite, Seitengröße). Eine nicht lokale `returnUrl` (z. B. `https://example.org`) erzeugt keinen Knopf.
20c. Benutzer ohne Stammdaten-Lesezugriff sehen die Artikelnummer ohne Link (bis Restfrage R1 anders entschieden ist).
20d. Der Anwender-Changelog nennt die Änderung des verfügbaren Bestands in der Artikelinfo als sichtbare Änderung für Lager/Produktion.
21. Build und alle Tests sind grün. Drift-Guard-Test `ServiceSettingDefinitions` und `SyncLogServices` sind grün.

## Test-Szenarien

Neues Kapitel **TS-154 Bestellvorschläge Nachbesserung** in `docs/TESTSZENARIEN.md`, danach `secondbrain/tests/testszenarien-index.md`.
Testsystem: AKE-DEV (Sage 4445) und IDEAL-DEV (nur die FA-Bedarf-Hierarchie, AK 7/8).

- **TS-154.1 Laufzeit**
  - Vorbedingung: Uhrzeit mittags = aktuelle Stunde.
  - Schritte: Dienst neu starten → Protokoll zeigt einen Lauf „Letzte Bestellung“. Erneut neu starten → kein zweiter Lauf.
  - Erwartet: Der Artikel-Lieferant-Lauf ist deutlich kürzer (Messwert vorher 287 s notieren).
  - Negativ: Wert 24 speichern → abgelehnt.
- **TS-154.2 Basismenge 0:** EK023626 hat nach dem Lauf keine letzte Bestellung mit Menge 0.
- **TS-154.3 Hinweis ohne Artikel-Lieferant:** ein Artikel aus den 255 Paaren (aus der Protokoll-Zählung bzw. SQL in P-N5a) zeigt „… kein
  Artikel-Lieferant in Sage“.
- **TS-154.4 FA-Bedarf (Liste und Artikelinfo)**
  - Artikel mit offenem FA: Spalte „FA-Bedarf“ = Artikelinfo-Aufschlüsselung „FA-Bedarf (Rest)“; „angerechnet“ gleich in beiden.
  - Bedarfsmeldung zum selben FA → keine Doppelzählung (max), in Liste und Artikelinfo.
  - Lagerbestellung zum selben Artikel → Abzug = max(FA-Teil, Σ Lagerbestellungen), in Liste und Artikelinfo.
  - Teilweise kommissionieren → nur der Rest, Spalte „Rest“ in der FA-Tabelle der Artikelinfo.
  - Negativ: Eine Einbuchung mit FA-Nummer ändert den Rest nicht.
  - Negativ: `EinkaufOffeneBedarfe` leer → Lagerbestellungen/Bedarfsmeldungen zählen weder in der Liste noch in der Artikelinfo.
- **TS-154.5 Dringlichkeit:** EK023973 (Bedarfe April) → „Nachbestellen“. Lagerbestellung über den Bestand hinaus anlegen → „Kritisch“.
  - Artikel mit offener Bestellung, Liefertermin nach heute + WBZ, Bedarf heute über dem Bestand → „Kritisch“; Liefertermin innerhalb
    heute + WBZ → nicht „Kritisch“ (in DEV über eine passende Cache-Position nachstellen oder im Unit-Test belegen).
  - Negativ: Position ohne Liefertermin → Hinweis „… ohne Liefertermin – für Dringlichkeit nicht gezählt“.
- **TS-154.6 Mengenregel (mit Till am Bildschirm):**
  - EK015744: kein Vielfaches der letzten Bestellmenge.
  - EK000051: MBM 300 > L 200 → Vorschlag 300.
  - Artikel ohne letzte Bestellung: Sage-Bestellmenge bzw. M − v + 1.
- **TS-154.7 Einheiten (Tills Beispielartikel; Erwartung je nach Stand der Prüfabfragen):**
  - EK003604 (Basis Stk, Lager PKG, Bestellmenge in Packung): ohne P-N1 keine Sage-Bestellmenge, Hinweis Basis ≠ Lager; nach P-N1 Menge in PKG.
  - EK002400 (MBM 24 PKG, Faktor 3): ohne P-N2 Hinweis „MBM nicht umrechenbar“; nach P-N2 MBM in Lagereinheit laut Prüfwert.
  - EK007623 (MBM 5 Stk, Faktor 5): wie EK002400 (gleiche Einheit mit Faktor ≠ 1 → Hinweis, nicht Faktor 1).
  - EK005564 (offene Bestellung, abweichende Einheit): Basis = Lager → eingerechnet, nicht mehr in der Liste; Basis ≠ Lager → Hinweis bis
    P-N3/P-N7.
  - Negativ: Artikel mit Faktor 0/leer → Hinweis „nicht umrechenbar“, keine Menge.
- **TS-154.8 Lieferant:** EK023973 zeigt den Lieferanten nach F8 oder „Kein Lieferant in Sage hinterlegt“.
  - 00394205 (System-Leiche, Meldebestand vom Einkauf in Sage zu pflegen): Zeile erscheint mit ehrlichem Hinweis (Lieferant/Mengenquelle),
    keine Rechenfehler; nach Pflege in Sage verschwindet sie bzw. rechnet regulär.
- **TS-154.9 Rahmenvertrag (E8):** EK014367 zeigt Rahmenvertrag-Lieferant (Vorrang) und Rest als Hinweis, Menge nach F6. Ein Artikel ohne
  Rahmen bleibt unverändert. Ohne P-N6/P-N9: entfällt, Blocker im Testprotokoll.
- **TS-154.10 Zugriff:** Benutzer ohne einkauf/admin → kein Zugriff. `EinkaufAktiv = false` → Startseite mit Warnung.
- **TS-154.11 Matchcode und Link:**
  - Spalte Matchcode sichtbar, im Zahnrad abwählbar, Spaltenfilter (z. B. Teil eines Matchcodes von EK000051) trifft, CSV enthält die Spalte.
  - Filter setzen (Lieferant, Spaltenfilter, Seite 2) → Artikelnummer EK014367 anklicken → Artikelinfo mit Bedarfen → „Zurück zu
    Bestellvorschlägen“ → dieselben Filter und dieselbe Seite.
  - Negativ: `returnUrl` von Hand auf eine externe Adresse ändern → kein Zurück-Knopf. Benutzer nur mit `einkauf` → Artikelnummer ohne Link.
- **TS-154.12 Artikelinfo (Lager/Produktion, beide Standorte):** AKE und IDEAL je ein Artikel mit offenem FA und offener Lagerbestellung →
  Aufschlüsselung FA-Bedarf (Rest) / Bedarfsmeldungen / Lagerbestellungen / angerechnet; verfügbarer Bestand = Gesamtbestand − angerechnet.
- **Regression:** TS-136/137 (Grundspec) durchlaufen. Artikelinfo: das bestehende Szenario „Verfügbarer Bestand“ wird an die neue Regel
  angepasst (gleiches Ergebnis nur ohne Lagerbestellungen/Bedarfsmeldungen/Teilkommissionierung, AK 7a).
- **Testartikel gesamt:** EK000051, EK014367, EK005564, EK023973, 00394205 (Tills Liste) sowie EK003604, EK002400, EK007623 (Einheiten),
  dazu EK015744 und EK023626.

## Etappen (nur bei epic: true)

**Warum Epic:** Die Arbeit betrifft Dienst, Web, eine Migration und den gemeinsamen BOM-Weg beider Standorte. Zwei Etappen hängen an
Sage-Prüfungen. Alles gehört fachlich zu einer Liste, die Till als Ganzes abnimmt, daher ein Worktree und ein Merge (Bündel-Worktree
`2026-08-07-ideal-teile-1-5` oder ein eigener, Entscheidung beim Start). Den Branch während der Arbeit mit `scripts/sync-worktree.ps1 -Slug
<slug>` aktuell halten. Jede Etappe baut und testet grün und endet mit einem eigenen Commit. Version für das ganze Epic: **1.99.31**.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 0 | Prüfabfragen P-N1–P-N9 auswerten (Mensch); Ergebnisse in diese Spec. Kein Code. | offen | – |
| 1 | Eigene Laufzeit `LetzteBestellung` + Planer (3/12 Uhr, verpassten Termin nach Dienststart nachholen) + Einstellungen + SyncLog-Name; Basismenge 0 (AK 1–5). Antwort 5 | offen | |
| 2 | `ArticleLastOrderUnassigned` + SQL/155 + FreshInstall; Hinweistexte F3 inkl. Lieferant; Lieferant-Kaskade F8 (AK 6, 18). Antwort 4 | offen | |
| 3 | Gemeinsamer Bedarfs-Service (`PlannedConsumptionService`) + Listen-BOM-Methoden + FA-Rest + max-Regeln + `OffenerBedarf.ProductionOrderId`; Artikelinfo auf dieselbe Regel mit Aufschlüsselung und Spalte „Rest“, `EinkaufOffeneBedarfe`-Beschreibung (AK 7, 7a, 9, 10 für die Artikelinfo). Antworten 1, 2, 7; Ausbuchungsarten bis P-N8 nur `Ausbuchung` | offen | |
| 4 | Bestellvorschläge auf den Service: Spalte „FA-Bedarf“, Banner; Spalte Matchcode, Link zur Artikelinfo mit `returnUrl` + Zurück-Knopf (AK 8–10, 20a–20c) | offen | |
| 5 | Dringlichkeit nach Termin/WBZ, offene Bestellungen nur mit Liefertermin ≤ heute + WBZ (Cache `SagePurchaseOrderLines.Liefertermin`), Spalte „Nächster Bedarf“ (AK 11, 11a, 12). Antwort 6 | offen | |
| 6 | Mengenregel A, MBM immer bei Umrechenbarkeit (AK 13). Antwort 3 | offen | |
| 7 | Umrechnungs-Helper **Ersatzlösung** (F7): nur gleiche Kürzel mit Faktor leer/1, sonst Hinweis; offene Bestellungen mit Basis = Lager trotz `EinheitAbweichend` eingerechnet; Sage-Bestellmenge bei Basis ≠ Lager gesperrt (BV-1) (AK 14–17 in der Ersatzform). **Blocker P-N1/P-N2/P-N3/P-N4/P-N7** nur für die Freischaltung der Umrechnung über Einheitengrenzen – liegt eine Antwort vor, Regel + Unit-Test mit Prüfwerten in dieser Etappe; sonst Blocker vermerken, nicht stoppen | offen | |
| 8 | **Blocker P-N6/P-N9:** Rahmenvertrag lesen und anzeigen (Antwort 8: Lieferant-Vorrang, Rest nur Hinweis; AK 19), ggf. SQL/156. Ohne Belege: Etappe überspringen (nichts bauen), Blocker vermerken | offen | |
| 9 | Abschluss: TS-154 (inkl. 154.11/154.12, Testartikel EK000051, EK014367, EK005564, EK023973, 00394205, EK003604, EK002400, EK007623), Hilfe (`Views/Help`, konkrete Details), **`docs/anwender/08-bestellvorschlaege.md`** (Rechenregel neu, Matchcode/Link, Abschnitt Artikelinfo-Hinweis: gleiche Bedarfe), **`docs/betrieb/04-konfiguration.md`** (§5.2: `Sync:LetzteBestellungUhrzeitNacht/-Mittag`), Anwender-Changelog mit Hinweis „sichtbare Änderung für Lager/Produktion“ (Artikelinfo), beide `AppVersion.cs`, README/ServiceSettings, Brain (feature-map, services.md, datenmodell.md, fallstricke §37 ergänzen, Testindex) | offen | |

## Pruefabfragen fuer Gerald (nur lesend; Sage 4445 / AKE-Test bzw. WMS-DEV)

SData-Abfragen über die bekannte Basis-URL des AKE-Test (wie in [[sage-sdata-api-vertraege]]). Die Feldnamen stammen aus dem Code
(`SdataArtikelReader.cs:47-58`, `ArtikelLieferantSyncService.cs:33-48`) bzw. dem Vertrags-Schema. Spalten der Sage-SQL-Tabellen sind
**nicht** belegt und werden deshalb mit `*` abgefragt.

- **P-N1 Sage-Bestellmenge EK003604** (BV-1)
  - `Artikel?where=Artikelnummer eq 'EK003604'&select=Artikelnummer,Basismengeneinheit,Lagermengeneinheit,UmrechnungsFaktorLME`
  - `ArtikelVariantenExtended?where=Artikelnummer eq 'EK003604'&select=Artikelnummer,Bestellmenge`
  - Erwartet: Bestellmenge = Wert der Sage-Maske in **Packung** (Till), Lager = PKG. Abweichung → BV-1-Sperre bleibt.
  - Dasselbe für einen zweiten Artikel mit Basis ≠ Lager und Bestellmenge > 0, falls vorhanden (WMS: `SELECT TOP 5 a.ArticleNumber,
    d.Basismengeneinheit, d.Lagermengeneinheit, d.Bestellmenge FROM ArticleSageDetails d JOIN Articles a ON a.Id = d.ArticleId WHERE
    d.Basismengeneinheit <> d.Lagermengeneinheit AND d.Bestellmenge > 0`).
- **P-N2 MBM EK002400 / EK007623**
  - `ArtikelLieferantInfo?where=Artikelnummer eq 'EK002400'&select=Lieferant,Artikelnummer,Mindestbestellmenge,Einkaufsmengeneinheit,UmrechnungsfaktorEK`
    (dasselbe für EK007623) plus P-N1-Felder beider Artikel.
  - Erwartet: EK002400 MBM 24 in PKG, Faktor 3; EK007623 MBM 5 in Stk, Faktor 5. Dazu notieren: Basis-/Lagereinheit und wie viel Lagereinheit
    die MBM laut Sage-Maske ergibt (Sollwert für den Unit-Test).
- **P-N3 Offene Bestellung EK005564**
  - `EinkaufPositionenAllInfo?where=Artikelnummer eq 'EK005564' and Belegkennzeichen eq 'EBB'&select=BelPosId,Belegkennzeichen,Menge,Mengeneinheit,MengeBasis,MengeBasiseinheit,Lagermengeneinheit,MengeEKUmrechnung,UmrechnungsFaktorLME`
  - WMS: `SELECT * FROM SagePurchaseOrderLines WHERE ArticleNumber = 'EK005564'` (falls die Spalte anders heißt: `WHERE BelPosId IN (…)`
    aus der SData-Abfrage).
  - Erwartet: offene Menge in Basiseinheit; welche Einheit `PositionGGBestellt`/`PositionGGGeliefert` haben (Vergleich mit Menge/MengeBasis
    derselben Position).
- **P-N4 EK003604 jüngste Bestellung**
  - `EinkaufPositionenAllInfo?where=Artikelnummer eq 'EK003604' and Belegdatum ge @2023-11-16@&select=BelPosId,Belegdatum,Menge,Mengeneinheit,MengeBasis,MengeBasiseinheit,MengeEKUmrechnung`
  - Erwartet: Wenn PKG → Stk mit Faktor ≠ 1, dann MengeBasis ≠ Menge. „Menge 2 = Basis 2“ heißt dann: Faktor 1 in dieser Position (Pflegefehler)
    oder Bestellung in Stk. Ergebnis entscheidet, ob MengeBasis die richtige Quelle für L ist.
- **P-N5 Lieferant EK023973**
  - a) WMS: `SELECT s.* FROM ArticleSuppliers s JOIN Articles a ON a.Id = s.ArticleId WHERE a.ArticleNumber = 'EK023973'; SELECT
    d.Hauptlieferant, d.LetzterLieferant, d.Wiederbeschaffungszeit FROM ArticleSageDetails d JOIN Articles a ON a.Id = d.ArticleId WHERE
    a.ArticleNumber = 'EK023973'`.
  - b) Sage: `ArtikelVariantenExtended?where=Artikelnummer eq 'EK023973'&select=Artikelnummer,Hauptlieferant,LetzterLieferant,Wiederbeschaffungszeit`
    und `ArtikelLieferantInfo?where=Artikelnummer eq 'EK023973'&select=Lieferant,Artikelnummer`.
  - Erwartet: Hauptlieferant leer in Sage (Datenpflege) oder in Sage gefüllt, im WMS leer (Abgleichfehler → eigener Bug).
- **P-N6 Rahmenvertrag EK014367** (Sage-SQL `ake_TEST2026`, nur lesend)
  - `SELECT TOP 20 * FROM KHKEKBelege WHERE Belegart = 'Rahmenvertrag EK' ORDER BY Belegdatum DESC`. Daraus Schlüssel- und Kennzeichen-Spalte
    ablesen.
  - Dann die Positionen dieses Belegs aus `KHKEKBelegePositionen` mit `*` und dem abgelesenen Schlüssel, gefiltert auf EK014367.
  - Erwartet: offener Rahmenvertrag für EK014367 (Castolin, laut Till evtl. Polen) mit Lieferant, Vertragsmenge, abgerufener Menge, Einheit kg.
- **P-N7 Richtung `UmrechnungsFaktorLME`**
  - Für EK003604 (Basis Stk, Lager PKG) Faktor ablesen (P-N1) und mit der Sage-Maske vergleichen: Wie viele Stk hat 1 PKG?
  - Erwartet: Faktor = Stk je PKG → Lager = Basis ÷ Faktor (oder umgekehrt). Ergebnis bestimmt die Umrechnung.
- **P-N8 Ausbuchungsarten mit FA-Bezug** (WMS-DEV)
  - `SELECT MovementType, COUNT(*) AS Anzahl, SUM(Quantity) AS Menge FROM StockMovements WHERE ProductionOrderId IS NOT NULL GROUP BY
    MovementType`. Dazu die Enum-Werte aus `MovementType` daneben legen.
  - Erwartet: Einbuchung und mindestens eine Ausbuchungsart. Nur die Ausbuchungsarten mindern den FA-Rest.
- **P-N9 Rahmenvertrag in SData**
  - `EinkaufPositionenAllInfo?where=Artikelnummer eq 'EK014367'&select=BelPosId,Belegkennzeichen,Belegdatum,A0Empfaenger,Menge,MengeBasis`.
  - Erwartet: Belegkennzeichen des Rahmenvertrags (Wert ablesen, nicht annehmen). Damit ist der SData-Leseweg festgelegt; sonst bleibt nur
    der Sage-SQL-Weg (dann Rückfrage, ob der Dienst Sage-SQL lesen darf).

## Deploy

**Vorläufig** – der QA-Lauf bestätigt gegen den echten Diff.

- **Web-App:** ja (Rechner, Controller, Liste inkl. Matchcode/Link, Artikelinfo mit neuer Bedarfsregel, Service).
- **Service:** ja (Laufzeit, Basismenge 0, Speicherung ohne Zuordnung, ggf. Rahmenvertrag).
- **Migration:** ja (SQL/155; ggf. SQL/156 aus E8).
- Reihenfolge: SQL/155 vor dem Dienststart (der Dienst schreibt in die neue Tabelle). Dienst und Web aus demselben Stand.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):
  - `@{ Id='SQL/155'; Version='1.99.31'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'; Datei='155_AddArticleLastOrderUnassigned.sql'; MigrationId='<yyyyMMddHHmmss>_AddArticleLastOrderUnassigned'; Voraussetzung='additiv'; Quelle='<changelog-slug v1.99.31>' }`
  - `@{ Id='v1.99.31-LetzteBestellungUhrzeiten'; Version='1.99.31'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Letzte Bestellung (Einkauf) laeuft jetzt nur zu Sync:LetzteBestellungUhrzeitNacht/-Mittag (Default 3/12 Uhr); erster Lauf direkt nach Dienststart. Im Aktivitaetsprotokoll pruefen.' }`
  - `@{ Id='v1.99.31-ArtikelinfoBedarfe'; Version='1.99.31'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Artikelinfo: Verfuegbarer Bestand rechnet jetzt wie die Bestellvorschlaege (FA-Rest, Lagerbestellungen/Bedarfsmeldungen nur einmal). Lager/Produktion informieren; Zahlen koennen von der Vorversion abweichen.' }`
  - Nur falls E8 eine Tabelle braucht: `SQL/156` analog (Typ Schema, automatisch).
- **Doku-Pflicht** (im Worktree, Dateien belegt in `docs/` des Bündel-Worktrees): `docs/anwender/08-bestellvorschlaege.md` (Rechenregel,
  Dringlichkeit, Einheiten-Hinweise, Matchcode/Link, Hinweis „Artikelinfo zeigt dieselben Bedarfe“) und `docs/betrieb/04-konfiguration.md`
  (§5.2 „Liest aus Sage und ändert WMS-Daten“: zwei Uhrzeiten-Einstellungen mit Fundstelle `ServiceSettingDefinitions.cs`).
  - Kein Daten-/Einmal-Skript, keine Pruefung (die Rolle `einkauf` ist schon über `Pruefung/RolleEinkauf` abgedeckt). Die Einstellungen
    kommen mit Default aus dem Katalog, daher kein Typ `Einstellung`.
- **Publish-Befehle** (im Worktree):
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  - `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

**Nummern-Hinweis:**

- Die BDE-Teileliste-Spec ([[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]]) nennt noch Version 1.99.28. Die ist inzwischen von
  „Sicherheit Anmeldung“ belegt; BDE braucht eine neue Nummer (Vorschlag 1.99.30).
- Diese Spec nimmt **1.99.31 / SQL/155 / TS-154**. Der Dev-Lauf prüft das beim Start erneut.

## Nachtrag 2026-11-16 19:49 (Gerald, Koordination) – Artikelnummer, Matchcode, Link zur Artikelinfo

**Anforderung Gerald:** In der Liste Bestellvorschläge Artikelnummer **und Matchcode** anzeigen und die Artikelnummer zu den Artikeldetails
verlinken, „damit man den Artikel inkl. Bedarfe ansehen kann“.

**Ist (belegt):** Spalten Artikel/Bezeichnung ohne Matchcode und ohne Link (`Views/PurchaseProposals/Index.cshtml`); die Artikelinfo
(`/Articles/Info`, `ArticlesController.cs:286-327`) zeigt Bestand und verfügbaren Bestand inkl. Bedarfe.

**Soll:** neue Spalte **Matchcode** (Standard sichtbar, Zahnrad, Spaltenfilter, CSV – Listen-View-Pflichten); Artikelnummer als Link auf
die **Artikelinfo** des Artikels (neuer Tab nicht nötig, Rücksprung mit erhaltenen Filtern); die Bedarfe dort nach derselben Regel wie die
Liste (siehe Rückfrage 7 – bei „nur Service umstellen, Verhalten gleich“ zeigt die Artikelinfo ggf. abweichende Zahlen → in der Artikelinfo
kennzeichnen, welche Bedarfe die Bestellvorschläge rechnen, oder Rückfrage 7 = gleiche Regel). AK + Testfall ergänzen (spec-agent bzw. Dev-Lauf).

**Zur Frage „Bedarfe aus dem WMS?“ (belegt):** Ja. Heute nur WMS-Daten: offene **Lagerbestellungen** und **Bedarfsmeldungen** nach
Einstellung `EinkaufOffeneBedarfe` (`PurchaseProposalsController.cs:88-95`). Mit dieser Spec kommen die **FA-Bedarfe** dazu, ebenfalls im WMS
berechnet (FA-/Stücklistendaten aus dem Sage-Abgleich, Logik wie Artikelinfo). Die Spalte „Sage verfügbar“ ist nur Vergleichswert aus Sage.

> [!note] Rumpf nachgezogen 2026-11-16 (Antworten 1–8, Nachtrag 19:49)
> spec-agent: Umfang, F3–F9, neu F11, Technik, affected_code, Einstellungen, AK, TS-154, Etappen, Deploy/Manifest auf die Antworten 1–8 und
> den Nachtrag umgestellt; überholte Stellen durchgestrichen und als „ueberholt“ markiert. Antwort-, Prüf- und Nachtragsabschnitte
> unverändert. Restfragen aus dem Nachzug siehe unten.

### Restfragen aus dem Rumpf-Nachzug (keine Blocker; der Rumpf baut mit dem genannten Vorgabewert)

- **R1 Artikelinfo-Zugriff für `einkauf`:** Die Artikelinfo verlangt Stammdaten-Lesezugriff (`ArticlesController.cs:10`,
  `CurrentUserService.cs:88-89`); die Rolle `einkauf` hat ihn nicht. Vorgabe im Rumpf: Link nur mit Stammdaten-Lesezugriff. Alternative:
  Gerald gibt den Einkäufern zusätzlich `stammdaten_lesen` in /Roles (kein Code) oder `RequireMasterDataReadAccess` wird für `Info` um
  `einkauf` erweitert (Code + drei Rollen-Stellen). → Entscheidung Gerald.
- **R2 Offene Bestellung ohne Liefertermin (Antwort 6):** Nach dem Wortlaut zählt sie im Dringlichkeitsfenster nicht (Rumpf so, mit
  Hinweis). Bitte mit Till bestätigen; die Gegenvariante („ohne Termin = kommt rechtzeitig“) wäre eine Zeile im Rechner.
- **R3 `EinkaufOffeneBedarfe` wirkt jetzt auch auf die Artikelinfo:** Folge aus Antwort 7 („dieselben Bedarfe“). Steht im Rumpf so; ein
  Standort, der die Einstellung leer lässt, sieht dann in der Artikelinfo nur FA-Bedarfe. Nur zur Kenntnis, sonst melden.

## Offene Rueckfragen

1. **Doppelzählung Lagerbestellung ↔ FA-Bedarf:** Eine Lagerbestellung hat keinen FA-Bezug (`WarehouseRequisition` nur
   `ProductionWorkplaceId`), eine exakte Zuordnung ist also unmöglich. Empfehlung je Artikel: Abzug = max(FA-Teil, Σ offene Lagerbestellungen).
   Damit wird nie doppelt gezählt, eine unabhängige Lagerbestellung neben einem FA kann aber unterschätzt werden. Passt das, oder lieber
   Lagerbestellungen immer voll und FA-Bedarf nur für den Teil darüber (gleiches Ergebnis), bzw. eine andere Regel?
2. **Teilweise kommissionierte FAs:** nur den Rest (BOM-Menge − FA-bezogene Ausbuchungen) abziehen (Empfehlung) oder voll? Hintergrund:
   Kommissioniertes ist schon aus dem WMS-Bestand ausgebucht, voll zählen wäre doppelt.
3. **„Fehlmenge bis über den Meldebestand“:** V = Meldebestand − verfügbar + 1 Lagereinheit (Empfehlung, knapp über dem Meldebestand)
   oder bis Meldebestand + letzte Bestellmenge? Danach immer MBM.
4. **Letzte Bestellung ohne Artikel-Lieferant:** in einer neuen Tabelle speichern (SQL/155) und ihre Menge als letzte Bestellmenge verwenden,
   mit Hinweis „kein Artikel-Lieferant in Sage“ (Empfehlung ja)? Ohne Speicherung kann der Hinweis den Lieferanten nicht nennen.
5. **Uhrzeiten:** Default nachts 3 Uhr, mittags 12 Uhr? Nach einem Dienststart sofort einen Lauf, wenn der letzte fällige Termin verpasst
   wurde (Empfehlung ja)?
6. **Dringlichkeit:** Im Fenster heute + WBZ alle offenen Bestellungen mitzählen (Empfehlung, einfach) oder nur die mit Liefertermin ≤
   heute + WBZ (genauer, braucht den Liefertermin aus dem Cache)?
7. **Artikelinfo:** In diesem Epic nur auf den gemeinsamen Service umstellen, Verhalten gleich (Empfehlung)? Oder sollen Rest-Regel und
   Doppelzählungsregel auch dort gelten (sichtbare Änderung für Lager und Produktion)?
8. **Rahmenvertrag:** Lieferant des offenen Rahmenvertrags hat Vorrang vor dem Hauptlieferanten, die Restmenge des Rahmens nur als Hinweis
   (Empfehlung). Oder Rahmen auch als Mengenquelle (z. B. EK014367: 30 kg)?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Doppelzählung Lagerbestellung ↔ FA-Bedarf (max je Artikel?) → **Gerald 2026-11-16 19:49 „passt“ (Empfehlung Koordination): Abzug = max(FA-Teil, Σ offene Lagerbestellungen) je Artikel (Till: nur einmal).**
2. Teilweise kommissionierte FAs: Rest oder voll? → **Gerald 2026-11-16 19:49 „passt“ (Empfehlung Koordination): nur der Rest.**
3. „Über den Meldebestand“: M − v + 1 Lagereinheit oder M + L? → **Gerald 2026-11-16 19:49 „passt“ (Empfehlung Koordination): M − v + 1 Lagereinheit, danach MBM; mit Till am Bildschirm bestätigen, Regel bleibt anpassbar.**
4. Letzte Bestellung ohne Artikel-Lieferant speichern (SQL/155) und als L verwenden? → **Gerald 2026-11-16 19:49 „passt“ (Empfehlung Koordination): ja, mit Hinweis „kein Artikel-Lieferant in Sage“ inkl. Lieferant.**
5. Uhrzeiten 3/12 Uhr, Nachholen nach Dienststart? → **Gerald 2026-11-16 19:49 „passt“ (Empfehlung Koordination): 3:00 und 12:00, verpassten Termin nach Dienststart nachholen.**
6. Dringlichkeit: alle offenen Bestellungen oder nur Liefertermin ≤ heute + WBZ? → **Gerald 2026-11-16 19:49 „passt“ (Koordination, abweichend vom spec-agent): nur offene Bestellungen mit Liefertermin ≤ heute + WBZ (Liefertermin aus dem Cache SagePurchaseOrderLines, wie Lieferverzug-Mail) – Tills Fall EK023973.**
7. Artikelinfo: nur umstellen oder Regeln auch dort? → **Gerald 2026-11-16 19:49 „gerne noch adaptieren“: dieselbe Regel auch in der Artikelinfo (nur einmal abziehen, Rest kommissionierter FAs) – Liste und Artikelinfo zeigen dieselben Bedarfe (Nachtrag 19:49: Link aus der Liste). Sichtbare Änderung für Lager/Produktion im Changelog nennen.**
8. Rahmenvertrag: Lieferant-Vorrang + Hinweis oder auch Mengenquelle? → **Gerald 2026-11-16 19:49 „passt“ (Empfehlung Koordination): Lieferant des offenen Rahmenvertrags hat Vorrang, Restmenge nur Hinweis; mit Till bestätigen.**
