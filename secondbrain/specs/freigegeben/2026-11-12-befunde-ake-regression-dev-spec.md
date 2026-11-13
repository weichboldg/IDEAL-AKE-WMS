---
type: spec
title: "Bugfix-Bündel aus der AKE-Regression auf DEV v1.99.12: Filter in Anführungszeichen, FA-Hinweis Einbuchung, Bedarfsmeldung Lokalzeit, Auftragsdruck Lagerplatz, Wagen-Modal"
slug: 2026-11-12-befunde-ake-regression-dev-spec
status: InUmsetzung
created: 2026-11-13
updated: 2026-11-13
source_backlog: "[[2026-11-12-befunde-ake-regression-dev]]"
bug: "[[2026-11-12-befunde-ake-regression-dev-bug]]"
task: "[[2026-11-13-buendel-v1-99-13-goal-lauf]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - IdealAkeWms/Services/ColumnFilterSyntax.cs
  - IdealAkeWms/wwwroot/js/column-filter-syntax.js
  - IdealAkeWms.Tests/Js/column-filter-cases.json
  - IdealAkeWms/Views/Help/Index.cshtml
  - IdealAkeWms/Views/StockMovements/Inbound.cshtml
  - IdealAkeWms/Controllers/Api/PartRequisitionsApiController.cs
  - IdealAkeWms/Data/Repositories/PartRequisitionRepository.cs
  - IDEALAKEWMSService/Services/PartRequisitionEmailService.cs
  - IdealAkeWms/Controllers/PickingController.cs
  - IdealAkeWms/Data/Repositories/StockMovementRepository.cs
  - IdealAkeWms/Views/Picking/Bom.cshtml
  - IdealAkeWms.Tests/Repositories/PartRequisitionRepositoryTests.cs
  - docs/TESTSZENARIEN.md
  - "Bug-Record: [[2026-11-12-befunde-ake-regression-dev-bug]]"
open_questions:
  - "Punkt 2 (FA-Hinweis Einbuchung): Variante A = Verhalten bleibt wie in AK7 von [[2026-08-05-wms-bugs-improvements-teil-1-spec]] entschieden (Menge = Bestand am Platz), Hinweistext sagt das ausdrücklich, TS-2.13/2.15 und Protokoll werden angepasst; Variante B = Hinweis zeigt zusätzlich den mit diesem FA gebuchten Anteil („3 Stk am Platz, davon 1 mit FA gebucht“) und öffnet damit die Entscheidung AK7 neu. Empfehlung A. Welche Variante?"
  - "Punkt 1 (Anführungszeichen): Sollen Operatoren innerhalb der Anführungszeichen wie bisher wirken (\"=GL;0;0;0\" = genau, \"GL;0;0;0\"% = beginnt mit) - oder soll der Inhalt in Anführungszeichen immer wörtlich als „enthält“ gelten? Empfehlung: Operatoren wirken weiter (Inhalt läuft durch die bestehende Teil-Auswertung, kleinster Eingriff)."
  - "Punkt 4a (Auftragsdruck Spalte Lagerplatz): Was soll bei noch nicht umgebuchten Zeilen gedruckt werden - (a) der vorgeschlagene Quell-Lagerplatz nach derselben Regel wie die Auftragsseite (Hauptlagerplatz, sonst buchbarer Platz mit höchster Menge, sonst NAN) oder (b) die Bestandsplätze wie in der Spalte Lagerplatz der Seite (höchstens 3)? Empfehlung (a)."
  - "Punkt 3 (Bedarfsmeldungen Lokalzeit): Sollen die Altdaten in dbo.PartRequisitions (CreatedAt, ModifiedAt, FulfilledAt, CancelledAt, EmailSentAt, bisher UTC) per Daten-Skript auf Lokalzeit umgerechnet werden (Firma beide, Trockenlauf, nur Zeilen vor dem Deploy-Zeitpunkt) - oder bleiben sie, wie sie sind (Altdaten zeigen weiter 1 bzw. 2 Stunden zu früh)? Vorher Prüfabfrage im Abschnitt Migrations-/SQL-Auswirkungen auf beiden Prod-DBs laufen lassen."
  - "Punkt 4b (Prio): Die vorgeschlagene Prio 32 ist regelkonform (MAX+1, Beleg unten). Beim Prüfen fiel auf, dass eine nach „Freigabe zurücknehmen“ behaltene Prio nicht in das MAX eingeht und so doppelte Prios entstehen (im Lauf: 2614167 und 2614168 beide 32). Soll das in diese Spec oder als eigener Backlog-Eintrag? Empfehlung: eigener Backlog-Eintrag, nicht in dieser Spec."
  - "Zusatzfund: Weitere Schreiber setzen DateTime.UtcNow/GETUTCDATE() (u. a. ProductionOrderPickingStatusRepository.cs:78/88/246, UsersController, RolesController, OrderRecipientsController, TrackingController.cs:255, Service-Syncs mit ModifiedAt = GETUTCDATE()). Soll dafür ein eigener Backlog-Eintrag (Prüfung, wo das angezeigt wird) angelegt werden? Empfehlung ja; nicht in dieser Spec."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-13"
---

# Bugfix-Bündel aus der AKE-Regression auf DEV v1.99.12

Quelle: [[2026-11-12-befunde-ake-regression-dev]], Testbericht [[2026-11-12-ake-regression-dev-v1-99-browser-testbericht]]
(Teilbericht [[nachtrag-3-testdaten]]). Code gelesen im Bündel-Worktree `feature/2026-08-07-ideal-teile-1-5` (v1.99.12).
Alle Zeilenangaben beziehen sich auf diesen Stand.

## Ziel / Nutzen (das Warum)

Die AKE-Regression auf DEV hat vier FEHLER und mehrere Beobachtungen geliefert. Diese Spec fasst alles zusammen, was dabei an
Code hängt, belegt je Punkt die Ursache und trennt echte Fehler von Erwartungen, die nicht zum Code passen:

| # | Befund | Ergebnis der Prüfung |
|---|---|---|
| 1 | Spaltenfilter „GL;0;0;0“ (AKE-Lagerplätze `XX;n;n;n`) trifft 3302 statt 26 Zeilen | **Fehler der Grammatik** (kein Escape), Fix: Anführungszeichen |
| 2 | FA-Lagerplatz-Hinweis bei der Einbuchung zeigt 3 Stk statt 1 Stk | **Kein Codefehler**: so in AK7 der Spec [[2026-08-05-wms-bugs-improvements-teil-1-spec]] entschieden; Test-Erwartung und Hinweistext passen nicht dazu |
| 3 | Bedarfsmeldung „Bestellt am“ 19:06 statt 20:06 | **Fehler**: UTC geschrieben, Lokalzeit angezeigt |
| 4a | Auftragsdruck: Spalte Lagerplatz leer | **Lücke**: Druck kennt nur den Quell-Platz umgebuchter Zeilen |
| 4b | Jede Freigabe schlägt Prio 32 vor | **Kein Fehler** (Regel MAX+1); Nebenbefund doppelte Prio |
| 4c | Modal „Kommissionierwagen belegt“: FA-Liste wiederholt sich, läuft rechts hinaus | **Fehler**: FA-Texte am Wagen werden bei „Trotzdem buchen“ immer weiter verkettet |

**Ausgeklammert:** BV4 (Tausendertrennzeichen „5 500,000“ statt „5.500,000“, Härtung mit fester Kultur de-AT in `Program.cs`) kommt
**nicht** in diese Spec. Erst wird auf akenet01:4445 geprüft, ob es an der Umgebung liegt (Einordnung Koordination im Testbericht).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Spaltenfilter: ein Wert in Anführungszeichen wird nicht an `;` oder `,` getrennt, gleich in C#-Kern und JS-Spiegel, neue Fälle in
   `column-filter-cases.json`, Filterhilfe (Popover und Hilfeseite) ergänzt. Bestandsverhalten aller Eingaben ohne Anführungszeichen
   bleibt gleich.
2. FA-Hinweis Einbuchung: je nach Rückfrage 1 nur Text und Testszenarien (Variante A) oder zusätzlich der FA-Anteil (Variante B).
   Mengenformat wie in TS-2.13 (`3,000`).
3. Bedarfsmeldungen: alle Zeitstempel in Lokalzeit schreiben (Web und Dienst). Altdaten nur nach Rückfrage 4.
4. Auftragsdruck: Spalte Lagerplatz auch für offene Zeilen füllen (Regel nach Rückfrage 3).
5. Wagen-Konflikt: FA-Nummern am Wagen einzeln, ohne Wiederholung; Modal bricht lange Listen um.

**Out-of-Scope**
- BV4 (siehe oben).
- Prio 32 (4b) – nur Beleg und Prüfabfrage; die doppelte Prio nach Rücknahme ist Rückfrage 5.
- Andere UTC-Schreiber außerhalb der Bedarfsmeldungen (Rückfrage 6).
- B2.3 (Standard der FA-Liste) – von der Koordination als „Protokoll passt nicht“ eingeordnet.
- „Glasteile landen im Lager-Auftrag“ und „Lagerplatz umbuchen bucht alle Artikel“ – Einrichtung bzw. gewolltes Verhalten laut
  Testbericht, keine Code-Befunde.
- Bereinigung alter `StockMovement.ProductionOrder`-Texte mit Wiederholungen (die Anzeige normalisiert beim Lesen, siehe Punkt 4c).

## Fachliche Anforderungen

### Punkt 1 – Spaltenfilter: Wert in Anführungszeichen

**Ursache (belegt).** `ColumnFilterSyntax.Parse` trennt den ganzen Filterwert ohne Ausnahme an `;` und (in Textspalten) `,`:
`IdealAkeWms/Services/ColumnFilterSyntax.cs:250` (Trenner je Typ) und `:254` (`payload.Split(separators, …)`). Der JS-Spiegel tut
dasselbe: `wwwroot/js/column-filter-syntax.js:78-79`. Die Tabelle dokumentiert die Grenze sogar als Fall
`T31 Semikolon nicht suchbar` (`IdealAkeWms.Tests/Js/column-filter-cases.json:32`). Die Bestandsübersicht filtert die Spalte
Lagerplatz über `ColumnFilterHelper.Apply` → `Parse` + `Matches` (`Controllers/StockOverviewController.cs:25`, `:72-73`;
`Services/ColumnFilterHelper.cs:59-61`). „GL;0;0;0“ wird so zu „enthält gl ODER enthält 0“ (3302 Zeilen).

**Korrektur am Testbericht:** Auch „=GL;0;0;0“ trifft heute **nicht** genau. Es wird zu „genau gl ODER enthält 0“ zerlegt; die
2167 Zeilen sind alle Plätze mit einer 0, nicht der eine Platz (gleiche Stelle `:250-259`). Den Schritt B1.3 im Protokoll entsprechend
anpassen.

**Regel (neu).** Gilt für alle Spaltentypen und auf beiden Seiten gleich (ADR [[0017-ein-parser-fuer-die-spaltenfilter-syntax]]):
1. Beginnt ein Teil (nach dem führenden `!` des ganzen Werts und nach Leerzeichen) mit `"` oder mit `="`, reicht er bis zum nächsten
   `"`. `;` und `,` dazwischen trennen nicht.
2. Die beiden Anführungszeichen werden entfernt. Was bis zum nächsten Trenner hinter dem schließenden `"` steht (z. B. `%`), gehört
   zum selben Teil.
3. Der Inhalt läuft durch die bestehende Teil-Auswertung (`ParsePart` bzw. `parsePart`): `=`, `%` am Anfang/Ende und `(leer)`
   wirken wie heute (vorbehaltlich Rückfrage 2). Beispiele: `"GL;0;0;0"` = enthält „gl;0;0;0“, `="GL;0;0;0"` und `"=GL;0;0;0"` = genau,
   `!"GL;0;0;0"` = enthält es nicht, `"GL;0;0;0";LL%` = enthält „gl;0;0;0“ ODER beginnt mit „ll“.
4. **Bestand bleibt:** Ein `"`, das nicht am Teil-Anfang steht (Zoll-Angaben wie `1/2"`), und ein `"` ohne schließendes Gegenstück
   sind normale Zeichen; getrennt wird dann wie heute. Einzige Verhaltensänderung: Ein Wert, der heute schon mit `"` beginnt und ein
   schließendes `"` hat, sucht nicht mehr nach den Anführungszeichen selbst.
5. `""` (leer) ist wirkungslos wie `=` (Fall T32).

Die Filterhilfe (Popover `column-filter-syntax.js:165-184`, Kurztext `:124-125`, Hilfeseite `Views/Help/Index.cshtml:452-489`)
bekommt eine Zeile: `"GL;0;0;0"` → genau dieser Text als ein Begriff (Semikolon und Komma trennen in Anführungszeichen nicht).

### Punkt 2 – FA-Lagerplatz-Hinweis bei der Einbuchung

**Ursache (belegt): Der Code tut, was entschieden wurde.** Der Hinweis ruft `GET /api/stock/by-order/{fa}`
(`Views/StockMovements/Inbound.cshtml:299`) → `StockApiController.GetStockByOrder` (`Controllers/StockApiController.cs:88-104`) →
`StockMovementRepository.GetStockByProductionOrderAsync(orderNumber)` mit Standard `onlyActualStock = true`
(`Data/Repositories/StockMovementRepository.cs:195`). Dieser Pfad sucht die Artikel/Platz-Paare über das FA-Tag, nimmt als Menge aber
den **ganzen Ist-Bestand** des Paars über alle Bewegungen (`:259-289`, `GetCurrentStockAtLocationAsync`). Die Bestandsübersicht ruft
dieselbe Methode mit `onlyActualStock: false` (`Controllers/StockOverviewController.cs:57`) und zeigt die FA-getaggte Netto-Summe
(`:207-257`) – daher dort 1,000.
Das ist Absicht: AK7 und S1 der Spec [[2026-08-05-wms-bugs-improvements-teil-1-spec]] (Ziel/Nutzen Zeilen 43-50, AK 7 Zeile 191),
abgesichert durch den Test `GetStockByProductionOrder_TruePath_MixedLocation_ShowsPlaceStockNotFaShare`
(`IdealAkeWms.Tests/Repositories/StockMovementRepositoryProductionOrderTests.cs:70-86`). Auf DEV lagen auf „Test“ 2 Stk ohne FA aus
Teil 2 und 1 Stk mit FA → 3; auf „TEST 1“ 1 + 1 → 2 ([[nachtrag-3-testdaten]], Spur der schreibenden Schritte).
TS-2.13/2.15 (`docs/TESTSZENARIEN.md:441-487`) haben nur Testdaten ohne Fremdbestand am Platz und benennen die Ungenauigkeit nicht;
das Protokoll hat daraus „Menge des FA“ abgeleitet.

**Mengenformat:** `Inbound.cshtml:192` formatiert mit `maximumFractionDigits: 3` ohne Mindeststellen → „3“, TS-2.13 erwartet „5,000“.
Das wird angeglichen (`minimumFractionDigits: 3`, wie die Bestandsübersicht).

**Variante A (Empfehlung):** Verhalten bleibt. Der Hinweis sagt, was die Zahl ist: „Lagerplatz **Test** — 3,000 Stk am Platz, Artikel
00102251“ und einmal darunter „Menge = gesamter Bestand des Artikels am Platz, nicht nur der Anteil dieses FA.“ TS-2.13/2.15 bekommen
den Mischfall als Negativfall; der Protokollschritt B1.2 wird angepasst. Kein Eingriff in Repository und Tests.

**Variante B:** Die API liefert je Paar zusätzlich den FA-Anteil (FA-getaggte Netto-Summe des Paars, gedeckelt auf den Ist-Bestand,
also `min(Netto_FA, Ist)`), der Hinweis zeigt „3,000 Stk am Platz, davon 1,000 mit FA 2604043 gebucht“. Öffnet die Entscheidung AK7.
Ein Feld mehr in `StockApiController.cs:96-103`, die Netto-Summe aus dem bestehenden `false`-Zweig wiederverwenden (keine dritte
Kopie der Bewegungsart-Regel). Das Tracking-Modal (`Views/Tracking/OseonIndex.cshtml:350`, nur AKE) nutzt dieselbe API und bliebe
unverändert, solange es das neue Feld nicht liest.

### Punkt 3 – Bedarfsmeldung „Bestellt am“ in Lokalzeit

**Ursache (belegt).** Konvention des Projekts ist Lokalzeit (`DateTime.Now`), die Oberfläche zeigt Zeitstempel ohne Umrechnung
(ADR [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]], [[fallstricke]] „`SyncLog.Timestamp` ist `DateTime.Now`, nicht `UtcNow`“;
Lagerbewegungen z. B. `Controllers/StockMovementsController.cs:154` `Timestamp = DateTime.Now`). Die Bedarfsmeldungen weichen davon
auf **allen** Schreibwegen ab:

| Feld | Schreiber | Stelle |
|---|---|---|
| `CreatedAt` | einziger Anleger (`new PartRequisition` nur hier) | `Controllers/Api/PartRequisitionsApiController.cs:106` `DateTime.UtcNow` |
| `FulfilledAt`, `ModifiedAt` | `FulfillAsync` | `Data/Repositories/PartRequisitionRepository.cs:115-116` `DateTime.UtcNow` |
| `CancelledAt`, `ModifiedAt` | `CancelAsync` | `PartRequisitionRepository.cs:128`, `:130` `DateTime.UtcNow` |
| `EmailSentAt` | Dienst, Mailversand | `IDEALAKEWMSService/Services/PartRequisitionEmailService.cs:269` `GETUTCDATE()` |

Weitere Schreibwege gibt es nicht: `UpdateAsync` (`PartRequisitionRepository.cs:35-39`) setzt keine Zeiten, und außer
`PartRequisitionsApiController.cs:92` legt niemand eine `PartRequisition` an (Suche nach `new PartRequisition` und
`PartRequisitions.Add` im Web- und Dienstprojekt). Angezeigt wird ohne Umrechnung: Liste `Views/PartRequisitions/Index.cshtml:132`
(„Bestellt am“) und `:136` (Tooltip „Gesendet“), Spaltenfilter `Controllers/PartRequisitionsController.cs:51-52`, Mail
„Zeitpunkt“ `PartRequisitionEmailService.cs:196`, Prüfantwort `PartRequisitionsApiController.cs:64`. Im November (MEZ = UTC+1) also eine
Stunde, im Sommer zwei Stunden zu früh.

**Korrektur:** alle vier Stellen auf Lokalzeit. Im Dienst den Zeitpunkt als Parameter `DateTime.Now` des Dienstrechners übergeben statt
`GETDATE()` – die Zeitzone des SQL-Servers ist nicht belegt; der Dienst schreibt sonst überall mit seiner eigenen Uhr.

### Punkt 4a – Auftragsdruck: Spalte Lagerplatz

**Ursache (belegt).** `PickingController.RenderAuftragDruckenAsync` füllt die Spalte nur aus `PickingOrderLine.SourceStorageLocationId`
(`Controllers/PickingController.cs:678`). Das Feld wird **nur beim Umbuchen** geschrieben (`Services/PickingTransferService.cs:230`);
beim Anlegen und im Folgeauftrag bleibt es leer (`Services/PickingOrderWorkflow.cs:127-132`, `:209-214`; sonst kein Schreiber, Suche
nach `SourceStorageLocationId =`). Der gedruckte KO-2026-00008 hatte nur offene Zeilen → 185 leere Zellen. Der Vorschlag, den die
Auftragsseite anzeigt, wird nur dort berechnet (`PickingController.cs:334-366`) und nicht gespeichert.

**Korrektur (nach Rückfrage 3, Empfehlung (a)):**
- Zeile `Umgebucht`: tatsächlicher Quell-Platz wie heute.
- Zeile `Offen`/`Gepickt`: vorgeschlagener Quell-Platz nach derselben Regel wie die Seite (Hauptlagerplatz mit buchbarem Bestand,
  sonst buchbarer Platz mit der höchsten Menge, sonst `NAN`).
- Zeile `Entfallen`/`Uebertragen`: leer (keine Entnahme).
- Die Regel wird aus `PickingController.cs:339-357` in **eine** statische Hilfsmethode gezogen und von Seite und Druck benutzt (keine
  zweite Kopie). Der Druck lädt dafür einmal `GetStockByArticleNumbersAsync` und die buchbaren Plätze wie `BomSeiteAsync`
  (`:296`, `:302-311`).

### Punkt 4b – Prio 32 (kein Fehler)

**Beleg.** Einzelfreigabe: Hat der FA keine Prio, gilt `MAX + 1` über freigegebene, nicht erledigte, nicht stornierte HauptFAs mit Prio
(`Controllers/PickingLeitstandController.cs:396-401` → `Data/Repositories/ProductionOrderPickingStatusRepository.cs:211-222`).
Sammelfreigabe: `++maxPrio` nur für FAs ohne Prio (`ProductionOrderPickingStatusRepository.cs:242-244`, `:270`). Beim Zurücknehmen bleibt
die Prio stehen (`SetReleaseAsync` `:76-86`, Sammel `:278-282`). Damit erklärt sich der Lauf vollständig mit MAX = 31 auf DEV: 2614167
bekam 32 und behielt sie nach der Rücknahme; in der Sammelfreigabe behielt 2614167 die 32, 2614168 bekam 31 + 1 = 32; 2625004 bekam
wieder 32. Dass `/Picking` nur Prio 15 zeigte, widerspricht dem nicht: Die Liste zeigt seit v1.88.0 Kommissionieraufträge, nicht
freigegebene FAs; freigegebene FAs aus der DB-Kopie ohne Auftrag zählen ins MAX, stehen aber nicht in `/Picking`.
**Prüfabfrage (DEV, lesend), um MAX = 31 zu bestätigen:**
```sql
SELECT po.OrderNumber, ps.PickingPriority, ps.ReleasedAt, po.IsDone, po.IsCancelled
FROM dbo.ProductionOrderPickingStatus ps
JOIN dbo.ProductionOrders po ON po.Id = ps.ProductionOrderId
WHERE ps.IsReleasedForPicking = 1 AND po.IsDone = 0 AND po.IsCancelled = 0
  AND ps.PickingPriority IS NOT NULL
  AND (po.SubOrderNumber = '' OR po.SubOrderNumber = po.OrderNumber)
ORDER BY ps.PickingPriority DESC;
```
Nebenbefund doppelte Prio → Rückfrage 5. In dieser Spec keine Code-Änderung.

### Punkt 4c – Modal „Kommissionierwagen belegt“

**Ursache (belegt).** Bei „Trotzdem buchen“ wird der FA-Text der neuen Umbuchung aus **allen bisherigen Texten am Wagen** plus dem
neuen FA zusammengesetzt (`Services/PickingTransferService.cs:102`). `GetProductionOrdersAtLocationAsync` liefert die Texte so, wie sie
in `StockMovement.ProductionOrder` stehen, nur `Distinct` auf den ganzen Text (`Data/Repositories/StockMovementRepository.cs:510-532`).
Ein Text „2618052;2602108“ ist für `Distinct` etwas anderes als „2618052“, also wächst der Text mit jedem erzwungenen Buchen
(„2618052; 2618052;2602108; 2618052;2618052;2602108;2602011; …“, Screenshot im [[nachtrag-3-testdaten]]). Das Modal setzt den Text
als Ganzes ein (`Views/Picking/Bom.cshtml:1632`) in einen Absatz ohne Umbruch-Klasse (`:509`, `fs-5 fw-bold text-center`); die lange
Zeichenkette ohne Leerzeichen läuft rechts hinaus.
**Risiko über die Anzeige hinaus:** `StockMovement.ProductionOrder` ist auf 500 Zeichen begrenzt (`Models/StockMovement.cs:20-22`);
`:102` kürzt nicht. Ein oft „trotzdem“ belegter Wagen kann damit beim Umbuchen an der Spaltenlänge scheitern (nicht nachgestellt).

**Korrektur an einer Stelle:** `GetProductionOrdersAtLocationAsync` zerlegt jeden Text an `;` (und `,`), trimmt, verwirft Leere und
liefert jede FA-Nummer einmal, sortiert. Das wirkt für alle drei Aufrufer: Konfliktprüfung und Modal (`PickingTransferService.cs:82-96`),
Zusammenführen beim „Trotzdem buchen“ (`:102`) und die Vorbelegung bei „Lagerplatz ausbuchen“
(`Controllers/StockMovementsController.cs:504-506`). Zusätzlich `text-break` am Absatz `#conflictCurrentWa` (`Bom.cshtml:509`).
Die Konfliktregel bleibt fachlich gleich: Konflikt, sobald am Wagen eine andere FA-Nummer als die neue liegt (`:85`); mit
Einzelnummern ist das genauer als der heutige Vergleich ganzer Texte.

## Technischer Loesungsentwurf

- **Ein Filter-Kern (ADR 0017, [[fallstricke]] §26):** Die Zerlegung in Teile wird in `ColumnFilterSyntax.Parse` und `parse` durch einen
  kleinen Zerleger ersetzt, der die Regel aus Punkt 1 umsetzt (Zeichen für Zeichen, Zustand „in Anführungszeichen“ nur ab Teil-Anfang).
  Danach unverändert `ToLowerInvariant` + `ParsePart`. Kein Aufrufer, kein SQL-Bauer (`ColumnFilterSql`) und keine Liste ändert sich;
  alle Wege laufen schon über `Parse` (Suche nach `ColumnFilterSyntax.Parse` / `.parse(`: `ColumnFilterHelper`, Controller- und
  Repository-Postfilter, `table-filter.js:307`, `Bom.cshtml:920`, `fa-hierarchy-tree.js:289`).
- **Testtabelle zuerst:** neue Fälle in `column-filter-cases.json`, beide Läufe grün (`ColumnFilterSyntaxTests` und
  `column-filter-syntax.check.js` per Node). `T31` bleibt unverändert (Bestand ohne Anführungszeichen). Skizze (Werte klein, wie die
  Tabelle; `sql: true` wo der SQL-Bauer greift):
  | id | type | filter | cell | match |
  |---|---|---|---|---|
  | Q01 Anführung trennt nicht | text | `"gl;0;0;0"` | `GL;0;0;0` | true (sql) |
  | Q02 Anführung nicht anderer Platz | text | `"gl;0;0;0"` | `GL;1;0;0` | false (sql) |
  | Q03 genau vor Anführung | text | `="gl;0;0;0"` | `GL;0;0;01` | false (sql) |
  | Q04 genau in Anführung | text | `"=gl;0;0;0"` | `GL;0;0;0` | true |
  | Q05 Negation | text | `!"gl;0;0;0"` | `GL;0;0;0` | false (sql) |
  | Q06 Anführung ODER Rest | text | `"gl;0;0;0";ll%` | `LL;1;32;0` | true (sql) |
  | Q07 Komma in Anführung | text | `"a,b"` | `b` | false |
  | Q08 Prozent hinter Anführung | text | `"gl;0;0;0"%` | `GL;0;0;01` | true (sql) |
  | Q09 Zoll-Zeichen Bestand | text | `1/2"` | `Rohr 1/2"` | true |
  | Q10 ohne Schluss Bestand | text | `"a;b` | `b` | true |
  | Q11 leere Anführung wirkungslos | text | `""` | `x` | true |
  | Q12 Datum: Anführung ein Teil | date | `"kw41;kw42"` | `15.10.2026 kw42` | false |
- **Einbuchung:** nur `Inbound.cshtml` (Text + Format) bei Variante A; bei B zusätzlich `StockApiController` und
  `StockMovementRepository` mit Repository-Test zum FA-Anteil.
- **Bedarfsmeldungen:** `DateTime.UtcNow` → `DateTime.Now` (3 Stellen Web), `GETUTCDATE()` → Parameter `@jetzt = DateTime.Now` (Dienst).
- **Auftragsdruck:** statische Hilfsmethode für den Quell-Vorschlag (z. B. `QuellLagerplatzVorschlag.Bestimme(locations, buchbarIds,
  nanId)`), aufgerufen aus `BomSeiteAsync` und `RenderAuftragDruckenAsync`; Unit-Test der Methode (rein, ohne DB).
- **Wagen-Konflikt:** Normalisierung in `GetProductionOrdersAtLocationAsync`; Test im bestehenden Repository-Testmuster
  (InMemory: zwei Umbuchungen mit „A“ und „A;B“ → Ergebnis `[A, B]`).
- Muster: Repository-Pattern unverändert, kein neuer Dienst, keine neue Abhängigkeit, keine neue Liste.

## Migrations-/SQL-Auswirkungen

- **Keine Schema-Änderung, keine EF-Migration**, `SQL/00_FreshInstall.sql` unberührt, `SQL/AgentJobs/` unberührt.
- **Altdaten Bedarfsmeldungen (nur nach Rückfrage 4):** Prüfabfrage vorab, lesend, beide Prod-DBs:
  ```sql
  SELECT COUNT(*) AS Zeilen, MIN(CreatedAt) AS Erste, MAX(CreatedAt) AS Letzte,
         SUM(CASE WHEN EmailSentAt IS NOT NULL THEN 1 ELSE 0 END) AS MitMail,
         SUM(CASE WHEN FulfilledAt IS NOT NULL THEN 1 ELSE 0 END) AS Erfuellt,
         SUM(CASE WHEN CancelledAt IS NOT NULL THEN 1 ELSE 0 END) AS Storniert
  FROM dbo.PartRequisitions;
  ```
  Falls umgerechnet wird: Einmal-Datenskript außerhalb der nummerierten Reihe (ADR [[0015-einmal-datenskripte-ausserhalb-der-nummerierten-sql-reihe]]),
  sommerzeitrichtig per `AT TIME ZONE 'UTC' AT TIME ZONE 'W. Europe Standard Time'`, nur Zeilen mit `CreatedAt` vor einem
  festen Stichtag = Deploy-Zeitpunkt (sonst würden neue, schon lokale Werte doppelt verschoben), je Spalte nur wenn nicht NULL.
  ADR 0023: genau ein `DECLARE @Trockenlauf BIT = 1;`, Trockenlauf-Zweig mit `TREFFER: n`, kein `ROLLBACK`, keine sqlcmd-Direktiven.
  Ein späterer zweiter Lauf darf nichts mehr verschieben (Marker `DeployScriptRuns` über das Update-Skript).
- Wagen-Texte (4c): keine Datenkorrektur; die Normalisierung wirkt beim Lesen auch auf Altbestand.

## Einstellungen

keine

## Audit-Feld-Auswirkungen

- `PartRequisition` (erbt `AuditableEntity`): `CreatedAt` und `ModifiedAt` künftig Lokalzeit; `ModifiedBy`/`ModifiedByWindows` werden in
  `FulfillAsync`/`CancelAsync` schon gesetzt (`PartRequisitionRepository.cs:117-118`, `:131-132`) – unverändert. Die Mail-Markierung
  im Dienst (`EmailSentAt`) setzt heute kein `ModifiedAt`; das bleibt so (kein fachliches Update, nur Versandstempel).
- `StockMovement`: nur der Inhalt von `ProductionOrder` bei erzwungener Wagen-Umbuchung wird kürzer/eindeutig; Audit-Felder unverändert
  (Neuanlage, `PickingTransferService.cs:219-224`).
- Filter, Hinweis, Druck: rein lesend, keine Audit-Wirkung.

## Akzeptanzkriterien

**Punkt 1 – Spaltenfilter**
1. `/StockOverview`, Spalte Lagerplatz, Filter `"GL;0;0;0"` zeigt nur Zeilen, deren Lagerplatz „GL;0;0;0“ enthält (auf AKE-DEV
   vergleichbar mit den 26 Zeilen auf akenet01; genaue Zahl nach DEV-Bestand).
2. `="GL;0;0;0"` und `"=GL;0;0;0"` treffen nur den Platz „GL;0;0;0“, nicht „GL;0;0;01“.
3. `!"GL;0;0;0"` blendet genau diese Zeilen aus.
4. `"GL;0;0;0";LL%` trifft „GL;0;0;0“ ODER alle Plätze, die mit „LL“ beginnen.
5. Ohne Anführungszeichen ist alles wie bisher: `GL;0;0;0` = ODER-Verknüpfung (3302 auf DEV), alle Bestandsfälle der Tabelle grün,
   `T31` unverändert.
6. `1/2"` sucht weiter nach dem Text mit Zoll-Zeichen; `"abc` (ohne Schluss-Anführungszeichen) verhält sich wie heute.
7. C#-Theory und Node-Lauf prüfen dieselben neuen Fälle und sind grün (Node-Lauf im QA-Nachweis belegt).
8. Die gleiche Eingabe liefert in einer Client-Liste (Browser-Kern) und einer Server-Liste (C#-Kern) dieselben Treffer.
9. Filterhilfe „?“ und Hilfeseite nennen die Anführungszeichen mit Beispiel.

**Punkt 2 – FA-Hinweis Einbuchung** (Variante A; bei B gelten 10, 12 und zusätzlich 11b)
10. Mengen im Hinweis mit drei Nachkommastellen („3,000 Stk“).
11. Variante A: Der Hinweis nennt die Menge als Bestand am Platz und erklärt in einem Satz, dass es nicht der FA-Anteil ist.
    11b (nur Variante B): zusätzlich „davon x mit FA … gebucht“, x = min(FA-Netto, Ist) je Platz.
12. Übernahme bei einem Platz, gelbe Warnung bei mehreren Plätzen, kein Hinweis bei FA ohne Bestand: unverändert (TS-2.12–2.18).

**Punkt 3 – Bedarfsmeldungen**
13. Eine neu angelegte Bedarfsmeldung zeigt in „Bestellt am“ die Ortszeit der Anlage (± 1 Minute), gleich der Uhrzeit einer
    Lagerbewegung derselben Minute.
14. Stornieren und Erfüllen (Einbuchung mit Zuordnung) schreiben `CancelledAt`/`FulfilledAt`/`ModifiedAt` in Ortszeit.
15. Nach dem Mailversand zeigt der Tooltip „Gesendet“ die Ortszeit des Versands; die Mail nennt als „Zeitpunkt“ die Ortszeit der Anlage.
16. Altdaten nach Entscheidung Rückfrage 4 (umgerechnet oder unverändert, im Deploy-Abschnitt festgehalten).

**Punkt 4a – Auftragsdruck**
17. Druck eines offenen Auftrags: Spalte Lagerplatz zeigt je offener Zeile denselben Platz, den die Auftragsseite in „Quell-Lagerplatz“
    vorschlägt (bei Rückfrage 3 = (a)); ohne buchbaren Bestand „NAN“.
18. Umgebuchte Zeilen zeigen den tatsächlichen Quell-Platz; entfallene/übertragene Zeilen bleiben leer.
19. Seite und Druck nutzen dieselbe Vorschlagsregel (eine Methode, Unit-Test).

**Punkt 4c – Wagen-Modal**
20. Liegen am Wagen Bewegungen mit Texten „A“, „A;B“, „A;A;B;C“, zeigt das Modal „A; B; C“ – jede Nummer einmal.
21. „Trotzdem buchen“ schreibt in die neue Bewegung jede FA-Nummer einmal (z. B. „A;B;C;D“), der Text wächst bei wiederholtem
    Buchen nicht.
22. Eine lange FA-Liste bricht im Modal um und bleibt innerhalb des Modals.
23. Konflikt wird weiter gemeldet, sobald am Wagen eine andere FA als die neue liegt; liegt nur dieselbe FA dort, kein Modal.
24. „Lagerplatz ausbuchen“ für einen Wagen belegt das FA-Feld mit den Einzelnummern („A; B; C“).

**Allgemein**
25. Build und alle Tests grün; neue Tests: Filterfälle (C# + Node), Vorschlagsregel, Normalisierung der Wagen-FAs,
    `FulfillAsync`/`CancelAsync` setzen Zeiten nahe `DateTime.Now` (Toleranz 1 Minute; schlägt auf einem Rechner mit UTC-Abstand fehl,
    wenn wieder `UtcNow` geschrieben wird).

## Test-Szenarien

Für `docs/TESTSZENARIEN.md` (Kapitelnummern vom Dev-Lauf gegenprüfen; danach `secondbrain/tests/testszenarien-index.md`):

**TS-91 erweitern (Spaltenfilter-Suchsyntax) – TS-91.x „Wert in Anführungszeichen“** (AKE und IDEAL)
- Vorbedingung: Bestandsübersicht mit Lagerplätzen, die `;` enthalten (AKE: „GL;0;0;0“, „GL;0;0;01“ falls vorhanden; IDEAL: einen
  Testplatz mit `;` im Code anlegen oder eine Textspalte mit `,` im Wert nehmen).
- Schritte/Erwartung: AK 1–4 je eine Zeile mit Trefferzahl. Dasselbe in einer Client-Liste (z. B. Kommissionierauftrags-Seite,
  Spalte Lagerplatz) → gleiche Treffer.
- Negativ: `GL;0;0;0` ohne Anführungszeichen = ODER wie bisher; `1/2"` in einer Bezeichnungsspalte trifft Zoll-Artikel; `"abc` ohne
  Schluss trifft wie heute; `""` filtert nicht; Popover „?“ zeigt die neue Zeile.

**TS-2.13 / TS-2.15 anpassen + TS-2.21 neu „Mischplatz“** (beide)
- Vorbedingung: Artikel X auf Platz P: 2 Stk ohne FA und 1 Stk mit FA F eingebucht.
- Schritte: Einbuchung, FA F eintippen, Tab.
- Erwartung A: Hinweis „P — 3,000 Stk am Platz …“ mit Erklärsatz; Platz übernommen. (B: „… davon 1,000 mit FA F gebucht“.)
- Negativ: FA ohne Bestand → kein Hinweis; zwei Plätze → gelbe Warnung, kein Übernehmen; Bestandsübersicht mit FA-Filter zeigt
  weiter 1,000 (historischer Pfad unverändert).

**TS-130 neu „Bedarfsmeldung: Zeiten in Ortszeit“** (beide)
- Schritte: offener Kommissionierauftrag → „Bestellen“ → Bedarfsmeldung anlegen; Uhrzeit notieren; `/PartRequisitions` prüfen;
  stornieren; zweite Meldung anlegen, Dienst-Mailversand abwarten (DEV: PickupDirectory), Tooltip „Gesendet“ und Mail-„Zeitpunkt“ prüfen;
  dritte Meldung per Einbuchung erfüllen und `FulfilledAt` per Abfrage prüfen.
- Erwartung: alle Zeiten = Ortszeit ± 1 Minute.
- Negativ: Spaltenfilter „Bestellt am“ `=<heute>` findet die neue Meldung; Altdaten wie in Rückfrage 4 entschieden (unverändert bzw.
  umgerechnet, Stichprobe).

**TS-120 erweitern – TS-120.x „Auftragsdruck Lagerplatz“** (beide)
- Vorbedingung: freigegebener FA mit offenem Kommissionierauftrag, mindestens ein Bauteil mit buchbarem Bestand, eines ohne.
- Schritte: „Auftrag drucken“ vor dem Umbuchen; zwei Zeilen umbuchen (eine mit geändertem Quell-Platz); erneut drucken;
  „Teilweise“ abschließen und Folgeauftrag drucken.
- Erwartung: offene Zeilen = Vorschlag der Seite, ohne Bestand „NAN“; umgebuchte Zeilen = tatsächlicher Quell-Platz (auch wenn vom
  Vorschlag abweichend); übertragene Zeilen im Teilauftrag leer.
- Negativ: Hauptlagerplatz nicht buchbar → nicht gedruckt (wie Seite).

**TS-120 erweitern – TS-120.y „Wagen belegt: FA-Liste“** (beide)
- Vorbedingung: Wagen W mit Bestand von FA A; dann mit „Trotzdem buchen“ FA B und FA C auf W.
- Schritte: Umbuchung von FA D auf W.
- Erwartung: Modal zeigt „A; B; C“ einmal je Nummer, umbrochen innerhalb des Modals; nach „Trotzdem buchen“ trägt die Bewegung
  „A;B;C;D“ (Bewegungshistorie).
- Negativ: Umbuchung von FA A auf W, wenn nur A dort liegt → kein Modal; „Lagerplatz ausbuchen“ belegt FA-Feld mit Einzelnummern.

**Protokoll** [[2026-10-20-ake-regression-ideal-version-browser-testprotokoll]]: B1.3 (Erwartung mit Anführungszeichen, Hinweis dass
`=GL;0;0;0` nicht genau trifft), B1.2 (Mischplatz-Erwartung nach Rückfrage 1) anpassen – Aufgabe der Koordination, nicht des Dev-Laufs.

## Etappen (nur bei epic: true)

entfällt (kein Epic)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Vorläufig** – der QA-Lauf bestätigt gegen den echten Diff. Version: nächste Patch-Version im Bündel nach v1.99.12 (Dev-Lauf prüft),
beide `AppVersion.cs`, Anwender-Changelog, Hilfeseite (Filter-Syntax, Einbuchung-Hinweis), Brain-Changelog.

- **Web-App:** ja (Filter-Kern C#/JS, Hilfe, Einbuchung, Bedarfsmeldungen, Auftragsdruck, Wagen-Modal)
- **Service:** ja (`PartRequisitionEmailService` – `EmailSentAt` in Ortszeit)
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine – **außer** Rückfrage 4 wird mit „umrechnen“ beantwortet, dann:
  `Id = BedarfsmeldungenOrtszeit`, Version = Version dieses Fixes, Typ `Einmalig`, Firma `beide`, Phase `Nachher`, Ausführung
  `Trockenlauf`, Text „Bedarfsmeldungen: alte Zeitstempel (UTC) einmalig in Ortszeit umrechnen; Trockenlauf zeigt TREFFER, dann
  bestätigen“. Skript unter `SQL/Einmalig/` nach ADR 0015/0023 (siehe Migrations-Abschnitt).
- **Publish-Befehle** (im Worktree):
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- QA testet auf AKE-DEV und IDEAL-DEV (`standort: beide`); Filter-Fälle mit `;` vor allem auf AKE (Lagerplatz-Codes).

## Offene Rueckfragen

1. **Punkt 2, Variante:** A = Verhalten bleibt wie in AK7 von [[2026-08-05-wms-bugs-improvements-teil-1-spec]] entschieden (Menge =
   Bestand am Platz), Hinweistext sagt das ausdrücklich, TS-2.13/2.15 und Protokoll werden angepasst; B = Hinweis zeigt zusätzlich den
   mit diesem FA gebuchten Anteil und öffnet AK7 neu. Empfehlung **A** (keine Neuverhandlung einer Schranke-1-Entscheidung, kleinster
   Eingriff; der FA-Anteil ist nach ungetaggten Ausbuchungen ohnehin nicht sicher zuordenbar).
2. **Punkt 1, Operatoren in Anführungszeichen:** wirken `=`, `%` am Rand und `(leer)` innerhalb der Anführungszeichen weiter, oder ist
   der Inhalt immer wörtlich „enthält“? Empfehlung: **wirken weiter**.
3. **Punkt 4a, Druck offener Zeilen:** (a) vorgeschlagener Quell-Platz wie die Seite oder (b) Bestandsplätze (höchstens 3)?
   Empfehlung **(a)**.
4. **Punkt 3, Altdaten:** alte UTC-Zeitstempel in `dbo.PartRequisitions` per Einmal-Skript (Trockenlauf, beide Firmen, Stichtag =
   Deploy) in Ortszeit umrechnen – oder unverändert lassen? Prüfabfrage siehe Migrations-Abschnitt; Ergebnis bitte mitliefern.
5. **Punkt 4b, doppelte Prio:** behaltene Prio nach Rücknahme geht nicht ins MAX ein → Doppelte möglich. In diese Spec oder eigener
   Backlog-Eintrag? Empfehlung: **eigener Backlog-Eintrag**.
6. **Zusatzfund UTC:** weitere `UtcNow`/`GETUTCDATE()`-Schreiber außerhalb der Bedarfsmeldungen (siehe Frontmatter). Eigenen
   Backlog-Eintrag anlegen? Empfehlung: **ja, nicht in dieser Spec**.

## Freigabe-Nachtrag 2026-11-13 08:39 (Gerald, geht dem Rumpf vor)
- Gemeinsamer /goal-Lauf mit [[2026-11-12-befunde-ake-regression-dev-spec]], [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]] und
  [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]] auf dem Bündel (v1.99.12). **Versions- und TS-Nummern vergibt der Lauf in
  Lauf-Reihenfolge neu** (die Nummern in den Specs sind Vorschläge; WE Teil 3 und Bugfix schlagen beide TS-130 vor). SQL ab 147.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Punkt 2 Variante A oder B → **A – AK7 bleibt, Hinweistext sagt „Bestand am Platz“, Mengenformat korrigieren (Gerald 2026-11-13).**
2. Operatoren in Anführungszeichen wirken weiter (ja/nein) → **Ja (Gerald 2026-11-13).**
3. Auftragsdruck offene Zeilen (a) Vorschlag oder (b) Bestandsplätze → **(a) vorgeschlagener Quell-Lagerplatz (Gerald 2026-11-13).**
4. Altdaten Bedarfsmeldungen umrechnen (ja/nein, Ergebnis Prüfabfrage) → **Nein, nicht umrechnen (AKE nie im Einsatz, IDEAL nur Anzeigeversatz 1 h). Prüfabfrage trotzdem vorher ausführen und Anzahl im Changelog nennen (Gerald 2026-11-13).**
5. Doppelte Prio: in diese Spec oder eigener Backlog → **Eigener Backlog-Eintrag (Gerald 2026-11-13).**
6. Zusatzfund UTC: eigener Backlog-Eintrag (ja/nein) → **Ja (Gerald 2026-11-13).**
