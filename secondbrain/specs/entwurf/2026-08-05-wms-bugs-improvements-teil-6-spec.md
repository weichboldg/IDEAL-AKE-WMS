---
type: spec
title: "Lagerplatz ausbuchen: FA-Spalte in der Bestandstabelle + FA-Feld als echter Filter"
slug: 2026-08-05-wms-bugs-improvements-teil-6-spec
status: Entwurf
created: 2026-08-05
updated: 2026-11-19
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: "[[2026-08-05-wms-bugs-improvements-teil-1-spec]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
version_vorschlag: "1.99.46 (Vorschlag; 1.99.45 ist fuer die Updater-Spec vorgeschlagen – bei Umsetzung naechste freie Nummer nehmen)"
testszenario: "TS-169"
affected_code:
  - IdealAkeWms/Services/FaBuchungsText.cs (neuer Helfer Nummern)
  - IdealAkeWms/Data/Repositories/IStockMovementRepository.cs
  - IdealAkeWms/Data/Repositories/StockMovementRepository.cs (GetProductionOrdersAtLocationAsync, neue Methode je Artikel)
  - IdealAkeWms/Controllers/StockMovementsController.cs (OutboundAll, OutboundAllConfirm)
  - IdealAkeWms/Models/ViewModels/OutboundAllViewModel.cs
  - IdealAkeWms/Views/StockMovements/OutboundAll.cshtml (Spalte, Inline-JS Knopf/Bestaetigung)
  - IdealAkeWms.Tests/Repositories/StockMovementRepositoryProductionOrderTests.cs
  - IdealAkeWms.Tests/Controllers/LagerbuchungFaVerknuepfungTests.cs (Mocks fuer OutboundAllConfirm anpassen)
  - IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs + Views/Help/Changelog.cshtml
  - docs/TESTSZENARIEN.md (TS-169, Verweis in TS-2.4)
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "ENTSCHIEDEN (Gerald 2026-11-19): Variante A – das FA-Feld filtert; nur Artikel, die laut Bewegungen zu diesem FA gehoeren, werden ausgebucht."
  - "ENTSCHIEDEN (Gerald 2026-11-19): alle FA kommagetrennt in der FA-Spalte."
  - "ENTSCHIEDEN (Gerald 2026-11-19): wie Teil 1 – echter Bestand als Grundlage, FA nur aus Bewegungen mit Restbestand."
  - "Kommissionierwagen: Das FA-Feld wird dort heute automatisch mit allen FA des Wagens vorbelegt (StockMovementsController.cs:501-507). Mit Antwort 1 wirkt diese Vorbelegung als Filter – Artikel OHNE FA-Text bleiben dann auf dem Wagen (heute: alles wird ausgebucht, TS-2.4). (a) so lassen, Restartikel sichtbar melden (Empfehlung), (b) auf Wagen nicht mehr vorbelegen (leer = alles, Ausbuchung ohne FA-Text), (c) unveraenderte Vorbelegung gilt als 'kein Filter'?"
  - "Gemischte Zeile: Ein Artikel am Platz traegt FA X und Y (Spalte 'X, Y'), gefiltert wird nach X. Bestand ist nicht FA-genau (Antwort 3), also wird der GANZE Platz-Bestand dieses Artikels ausgebucht – inkl. des Anteils von Y, mit Sage-Meldung. (a) ganze Zeile ausbuchen und im Bestaetigungsdialog benennen (Empfehlung), (b) gemischte Zeilen beim Filter auslassen und melden?"
  - "IDEAL Haupt-/Sub-FA: Der Filter vergleicht den FA-TEXT der Bewegungen (wie Teil 1). Wer auf IDEAL die Haupt-FA eingibt, trifft Bewegungen, die mit der Sub-FA-Nummer (BelID) getaggt sind, NICHT. Reicht der reine Textvergleich (Empfehlung), oder sollen auf IDEAL zusaetzlich die ueber ProductionOrderId verknuepften Sub-FA einer eingegebenen Haupt-FA mitzaehlen?"
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

> [!info] Überarbeitung 2026-11-19 (spec-agent, Rework)
> Rumpf gegen den Bündel-Stand v1.99.44 (Worktree `2026-08-07-ideal-teile-1-5`) neu belegt und die
> Freigabe-Antworten 1–3 eingearbeitet. Der Ist-Zustand und der Lösungsentwurf von 2026-08-05 sind
> unten als **überholt** aufbewahrt. Neue Rückfragen 4–6 (Wagen-Vorbelegung, gemischte Zeile,
> IDEAL Haupt/Sub-FA). Antwort-, Prüf- und Nachtragsabschnitte unverändert.

## Ziel / Nutzen (das Warum)

Auf der Seite „Lagerplatz ausbuchen" (`/StockMovements/OutboundAll`) soll der Lagermitarbeiter
erkennen können, zu welchem Fertigungsauftrag die auf dem Lagerplatz liegenden Artikel gehören,
und gezielt nur die Artikel eines bestimmten FA ausbuchen können. Heute zeigt die Tabelle keine
FA-Spalte, und das FA-Feld ändert nur den Text der neuen Ausbuchungen. Ausgebucht wird **immer**
der gesamte Bestand des Lagerplatzes (Beleg unten, Ist-Zustand).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. FA-Spalte „Fertigungsauftrag" in der Bestandstabelle von `OutboundAll`: je Artikelzeile **alle**
   FA-Nummern kommagetrennt (Antwort 2), ermittelt nur aus Bewegungen an Artikel/Lagerplatz-Paaren
   mit echtem Restbestand > 0 (Antwort 3, Regel wie Teil 1).
2. FA-Feld als echter Filter (Antwort 1): Steht ein Wert im Feld, werden beim Bestätigen **nur** die
   Artikel ausgebucht, deren FA-Spalte eine der eingegebenen Nummern enthält.
3. Knopftext und Bestätigungsdialog zeigen, wie viele bzw. welche Artikel tatsächlich ausgebucht
   werden. Kein stiller Fallback auf „alles ausbuchen", wenn der Filter nichts trifft.

**Out-of-Scope**
- Leeres FA-Feld: Verhalten bleibt wie heute (gesamter Lagerplatz-Bestand).
- `LocationTransfer`/`LocationTransferConfirm` (Lagerplatz umbuchen) – das Backlog nennt nur
  „Lagerplatz ausbuchen".
- FA-genaue Mengen (Bestand ist nach der Buchung nicht FA-attributiert, Teil-1-Spec S1/AK7). Eine
  ausgebuchte Zeile umfasst immer den echten Platz-Bestand des Artikels (siehe Rückfrage 5).
- Auswahl einzelner Zeilen per Checkbox (Variante B, verworfen durch Antwort 1).
- Änderung an `GetStockByProductionOrderAsync` (Teil 1) und am Sage-Enqueue.

## Fachliche Anforderungen

1. Die Tabelle zeigt nach Artikelnummer, Bezeichnung, Einheit eine Spalte „Fertigungsauftrag" vor
   „Bestand". Inhalt: alle FA-Nummern der Zeile, einzeln, jede einmal, sortiert, mit „, " getrennt;
   leer, wenn keine Bewegung am Paar einen FA-Text trägt.
2. „Gehört zu FA" heißt: Am Paar (Artikel, dieser Lagerplatz) liegt echter Bestand > 0 **und**
   mindestens eine Bewegung mit `StorageLocationId` = dieser Platz trägt die Nummer im FA-Text
   (Texte wie „A;B" oder „A, B" werden in Einzelnummern zerlegt). Das ist dieselbe Regel, die heute
   lagerplatzweit für die Wagen-Vorbelegung und den Wagen-Konflikt gilt, nur je Artikel.
3. Bewegungsarten: alle zählen (auch eine Ausbuchung mit FA-Text), wie in der bestehenden Regel
   (`StockMovementRepository.cs:527-533`) und in der Teil-1-Kandidatensuche (`:198-202`).
4. FA-Feld mit Wert: Das Feld wird in Einzelnummern zerlegt (Trennzeichen `;` und `,`, getrimmt).
   Ausgebucht werden genau die Zeilen, deren FA-Nummern mindestens eine davon **exakt** enthalten
   (Groß-/Kleinschreibung egal). Kein Teilstring-Treffer: „123" trifft „1234567" nicht.
5. Menge je ausgebuchter Zeile = echter Platz-Bestand des Artikels (wie heute).
6. Trifft der Filter keine Zeile: nichts buchen, Warnung „Kein Artikel auf <Platz> gehört zu FA
   <Eingabe> – nichts ausgebucht.", zurück auf die Seite mit demselben Lagerplatz.
7. Gefilterte Ausbuchung erfolgreich: Erfolgsmeldung nennt Anzahl und FA; verbleiben Artikel am
   Platz, nennt die Meldung deren Anzahl. Rückkehr auf die Seite **mit** demselben Lagerplatz, damit
   der Rest sichtbar ist. Ungefilterte Ausbuchung: Rückkehr wie heute ohne Lagerplatz.
8. FA-Text und FA-Verknüpfung der neuen Ausbuchungen bleiben wie heute: Text = Feldinhalt,
   `ProductionOrderId` nur bei eindeutiger Auflösung (Liste → null, v1.87.0 Antwort 5).
9. Die Seite zeigt vor dem Absenden, was passiert: Knopf „Alle N Artikel ausbuchen" bei leerem Feld,
   „M von N Artikeln ausbuchen (FA …)" bei Wert, deaktiviert mit Hinweistext bei M = 0. Der
   Bestätigungsdialog nennt bei Filter die Artikelnummern der M Zeilen. Der Server entscheidet
   selbst neu (Seite und POST können auseinanderliegen); die Seite ist nur Vorschau.

## Ist-Zustand (Code-Referenzen, Bündel-Stand v1.99.44)

Belegt im Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`.

**Controller** `IdealAkeWms/Controllers/StockMovementsController.cs` (keine Klassen-Filter, `:11`):
- `OutboundAll(int? storageLocationId)` `:482-511`, `[RequireStockKeyUserAccess]` `:482`. Platzliste
  ohne Kommissionierwagen (`:487` → `StorageLocationRepository.cs:43-49`: `IsActive && !IsPickingTransport && IstBuchbar`).
  Zeilen = `GetCurrentStockAsync(filterStorageLocationId)` mit `CurrentQuantity > 0` (`:497-499`).
  Nur für Wagen: FA-Feld vorbelegt mit `string.Join("; ", GetProductionOrdersAtLocationAsync(...))`
  (`:501-507`).
- `OutboundAllConfirm(int storageLocationId, string? productionOrder)` `:513-557`,
  `[RequireStockKeyUserAccess]`, `[HttpPost]`, `[ValidateAntiForgeryToken]`. Auswahl
  `itemsToOutbound` = alle Zeilen > 0 (`:518-520`) – **`productionOrder` wirkt nicht auf die
  Auswahl**. Leer → Warnung (`:522-526`). FA-Auflösung einmal (`:532`, Kommentar `:531`: Liste →
  nicht verknüpft, keine Pflichtprüfung). Je Zeile `StockMovement` mit `Quantity = CurrentQuantity`,
  `ProductionOrder = productionOrder`, `ProductionOrderId = fa.Order?.Id`, `MovementType.Ausbuchung`,
  Audit `CreatedAt/CreatedBy/CreatedByWindows` (`:534-553`), über `AddAsync` (`:551`). Erfolg →
  Redirect **ohne** Lagerplatz (`:555-556`).

**View** `IdealAkeWms/Views/StockMovements/OutboundAll.cshtml`: Kopfzeile Artikelnummer/
Bezeichnung/Einheit/Bestand, **keine FA-Spalte** (`:61-67`), Zeilen `:70-78`, Fußzeile
`colspan="3"` (`:80-85`). Formular `:90-103`, FA-Feld `name="productionOrder"` (`:95`), Knopf
„Alle @Model.Items.Count Artikel ausbuchen" (`:98-100`). Kein eigenes JS-Modul: Inline-Skript
`confirm(...)` → `submit()` (`:117-123`); `barcode-scanner.js` (`:116`) nur für den Lagerplatz-Scan.
Wagen-Hinweis „FA-Nummer wurde automatisch ermittelt" `:45-53`.

**ViewModel** `IdealAkeWms/Models/ViewModels/OutboundAllViewModel.cs:3-11`: kein FA je Zeile.
`StockOverviewItem` (`StockOverviewViewModel.cs:19-34`): kein FA-Feld (wird breit genutzt, deshalb
nicht erweitern).

**Service-Schicht:** Es gibt keinen OutboundAll-Service; der Controller bucht direkt über das
Repository. `IStockMovementRepository` ist als `SageBookingEnqueueingStockMovementRepository`
registriert (`Program.cs:78`), eine Unterklasse von `StockMovementRepository`, die nur `AddAsync`
überschreibt (`SageBookingEnqueueingStockMovementRepository.cs:21,41-80`). Neue Lesemethoden erbt
sie ungeändert. `hatFaBezug` bremst nur Umbuchungen (`SageBookingEnqueueDecision.cs:66-75`),
Ausbuchungen gehen je nach Schalter + Lagerplatz-Flag `SageBuchungErlaubt` an Sage
(`SageBookingEnqueueingStockMovementRepository.cs:51-69`).

**Repository** `IdealAkeWms/Data/Repositories/StockMovementRepository.cs`:
- `GetCurrentStockAsync` `:16-193`: echter Bestand je Paar (Ziel-Summe `:41-79` + Umbuchungs-Quelle
  `:81-131`, Merge `:133-156`).
- `GetStockByProductionOrderAsync(string, bool onlyActualStock = true)` `:195-295` – **das ist die
  Teil-1-Umsetzung** (Spec [[2026-08-05-wms-bugs-improvements-teil-1-spec]], status Gemerged, v1.29.0):
  Kandidaten über `ProductionOrder.Contains(...)` (`:198-202`, Teilstring), `true`-Pfad = echter
  Bestand je Paar über `GetCurrentStockAtLocationAsync` (`:259-289`, `:492-512`), nur > 0.
  Aufrufer: `StockApiController` (Einbuchungs-Hinweis, Tracking-Modal) mit `true`, StockOverview mit `false`.
- `GetProductionOrdersAtLocationAsync(int)` `:514-542`: genau die Regel „FA nur aus Bewegungen mit
  Restbestand", aber **lagerplatzweit**: Artikel mit Bestand > 0 am Platz (`:518-522`), FA-Texte aller
  Bewegungen mit `StorageLocationId` = Platz für diese Artikel (`:527-533`), seit v1.99.13 in
  Einzelnummern zerlegt, distinct, ordinal sortiert (`:535-541`). Aufrufer: Wagen-Vorbelegung
  (`StockMovementsController.cs:504`) und Wagen-Konfliktprüfung (`PickingTransferService.cs:82-103`).
  Test: `StockMovementRepositoryProductionOrderTests.cs:20-35`.

**Trennzeichen-Konvention** `;`/`,`: `ProductionOrderRepository.cs:228` (Liste erkennen),
`StockMovementRepository.cs:538` (zerlegen). Wagen-Bewegungen tragen zusammengesetzte Texte wie
„A;B" (`PickingTransferService.cs:101-102`, Bewegung `:210-218`).

**Wagen-Einstieg:** Wagen stehen nicht in der Platzauswahl, kommen aber über den Konflikt-Dialog
der Kommissionierung auf die Seite (`Views/Picking/Bom.cshtml:1655-1660`, `conflictLocationId` aus
`:1627`).

**Rechte:** `RequireStockKeyUserAccess` → `CanTransferStockAsync` = admin, stock_keyuser, picking
(`CurrentUserService.cs:134-135`).

**Bestehende Tests/Szenarien, die den Pfad berühren:**
`LagerbuchungFaVerknuepfungTests.cs:109` (`OutboundAllConfirm(2, "1065141")`, Mock `GetCurrentStockAsync`
→ Artikel 9, `:76-77`) und `:115-119` (`"A; B"` → nicht verknüpft). `docs/TESTSZENARIEN.md` TS-2.4
(`:272-289`, Wagen „Alle Artikel ausbuchen"), Zeile 115.12 (`:11352`), 130.5.6 (`:11810`, Wagen-Vorbelegung).

### Was seit v1.2x schon umgesetzt ist (ehrlich)

- **Nicht umgesetzt:** FA-Spalte (View `:61-67`) und Filterwirkung (`:518-520`) – der Kern dieser
  Spec ist offen.
- **Umgesetzt, nutzbar:** Teil 1 (`onlyActualStock`, v1.29.0, gemerged); die lagerplatzweite
  Restbestand-Regel mit Zerlegung in Einzelnummern (v1.99.13); FA-Verknüpfung `ProductionOrderId`
  beim Lagerplatz-Ausbuchen (v1.87.0, `:532,542`). Nichts davon liefert FA je Artikelzeile.

> [!note]- Ist-Zustand vom 2026-08-05 (v1.2x) – **überholt**, nur Audit
> Zeilenangaben veraltet; Aussage „productionOrder wirkt nur als Tag" gilt weiterhin (jetzt `:518-541`).
> Der damalige Verweis auf `GetProductionOrdersAtLocationAsync` „:448-470, nur Wagen" ist überholt:
> heute `:514-542`, zusätzlich vom Wagen-Konflikt genutzt, mit Zerlegung (v1.99.13).
>
> `IdealAkeWms/Views/StockMovements/OutboundAll.cshtml:60-106`: Tabelle zeigt Spalten
> Artikelnummer/Bezeichnung/Einheit/Bestand — **keine** FA-Spalte. Das Formular
> (Zeilen 90-104) hat ein Textfeld `name="productionOrder"`, das per
> `OutboundAllConfirm(int storageLocationId, string? productionOrder)`
> (`IdealAkeWms/Controllers/StockMovementsController.cs:364-405`) verarbeitet wird.
> **Befund:** `productionOrder` beeinflusst **ausschließlich** das Tag der neu erzeugten
> Ausbuchungs-Bewegungen, **nicht** die Auswahl `itemsToOutbound`.
> `OutboundAllViewModel` (`:1-11`) und `StockOverviewItem` (`StockOverviewViewModel.cs:17-32`) haben
> **kein** Feld für einen FA-Bezug je Zeile. Vorbild `GetProductionOrdersAtLocationAsync`
> (`StockMovementRepository.cs:448-470`) — aggregiert über den ganzen Lagerplatz, nur Kommissionierwagen
> (`StockMovementsController.cs:352-358`).

## Technischer Lösungsentwurf

Kein neues Muster, keine Schemaänderung. Die vorhandene lagerplatzweite Regel wird **je Artikel**
geliefert und die alte Methode daraus abgeleitet – eine Regel an einem Ort.

1. **Zerlegen an einem Ort** – `FaBuchungsText` (`Services/FaBuchungsText.cs`, schon die FA-Text-Hilfe):
   `public static IEnumerable<string> Nummern(string? text)` = Split an `;`/`,`, `TrimEntries |
   RemoveEmptyEntries`. `StockMovementRepository.cs:538` nutzt ihn statt des eigenen Splits.
2. **Repository** – neue Methode in `IStockMovementRepository` + `StockMovementRepository`:
   `Task<Dictionary<int, List<string>>> GetProductionOrdersByArticleAtLocationAsync(int storageLocationId)`
   – Schlüssel `ArticleId`, Wert distinct, ordinal sortiert. Inhalt = heutiger Rumpf `:518-541`, nur
   mit `GroupBy(ArticleId)` statt globalem `Distinct`; Artikel mit Bestand > 0 ohne FA-Text erscheinen
   **nicht** im Dictionary (Aufrufer behandelt fehlend = leer).
   `GetProductionOrdersAtLocationAsync` wird zu: Vereinigung aller Werte, distinct, ordinal sortiert –
   Ergebnis bit-gleich (bestehender Test `:20-35` bleibt grün; Wagen-Konflikt unverändert).
3. **ViewModel** – `OutboundAllViewModel` bekommt `Dictionary<int, List<string>> FaJeArtikel` (leer
   initialisiert). `StockOverviewItem` bleibt unberührt.
4. **`OutboundAll` (GET)** – nach `:499` einmal `FaJeArtikel` laden. Die Wagen-Vorbelegung `:501-507`
   bildet ihren Text aus `FaJeArtikel.Values` (gleiches Ergebnis, ein Aufruf weniger). Verhalten der
   Vorbelegung bleibt bis zur Antwort auf Rückfrage 4 wie heute.
5. **`OutboundAllConfirm` (POST)** – `var filter = FaBuchungsText.Nummern(productionOrder).ToHashSet(StringComparer.OrdinalIgnoreCase)`.
   - `filter` leer → Pfad wie heute, unverändert.
   - sonst `FaJeArtikel` laden, `itemsToOutbound` auf Zeilen mit `FaJeArtikel[ArticleId].Overlaps(filter)`
     einschränken; keine Zeile → Warnung (Anforderung 6), Redirect mit `storageLocationId`, nichts buchen.
   - Buchungsschleife `:534-553` unverändert (Menge, Text, FK, Audit, `AddAsync` → Sage-Enqueue
     über den Decorator, kein Bypass). Erfolgsmeldung + Redirect nach Anforderung 7.
6. **View** – `<th>Fertigungsauftrag</th>` vor „Bestand", Zelle `string.Join(", ", liste)`,
   Zeile `data-fa="@string.Join(";", liste)"`, Fußzeile `colspan="4"`. Inline-Skript (`:117-123`)
   ergänzen: bei `input` im FA-Feld Nummern wie in C# zerlegen (`/[;,]/`, trim, case-insensitiv),
   passende Zeilen zählen, Knopftext/`disabled`/Hinweis setzen (Anforderung 9), Bestätigungstext mit
   Artikelnummern. Passende Zeilen zusätzlich mit `table-warning` markieren – nicht alleiniger
   Bedeutungsträger, Zahl und Dialog nennen es auch. `frontend-design`-Skill im Dev-Lauf aufrufen.
7. **Gemischte Zeilen** (Rückfrage 5, Empfehlung a): ganze Zeile ausbuchen; der Dialog nennt Zeilen,
   deren FA-Liste weitere Nummern außer den gefilterten enthält („enthält auch FA …").

Ponytail: keine neue Klasse, kein Service; zweimal `GetCurrentStockAsync` pro Seitenaufruf
(Controller + Repository) wird in Kauf genommen – lagerplatzgefiltert und klein. Erst zusammenlegen,
wenn es messbar stört.

> [!note]- Lösungsentwurf vom 2026-08-05 – **überholt**, nur Audit
> Neue Methode „analog `GetProductionOrdersAtLocationAsync`, gruppiert nach ArticleId" (bleibt, jetzt
> Punkt 2); Feld `ProductionOrders` in `StockOverviewItem` (verworfen – VM-Dictionary statt breit
> genutzter Klasse); Variante A mit `Contains`-Semantik wie `GetStockByProductionOrderAsync`
> (verworfen – exakte Einzelnummer, weil hier tatsächlich ausgebucht wird; Teilstring „123" würde
> „1234567" mit ausbuchen, Hinweis H1 der Teil-1-Prüfung); Variante B Checkboxen (durch Antwort 1 verworfen).

## Migrations-/SQL-Auswirkungen

Keine. Reine Lese-/Filterlogik auf `StockMovement`; keine Migration, kein `SQL/XX`, keine Änderung an
`SQL/00_FreshInstall.sql` oder `SQL/AgentJobs/`.

## Einstellungen

keine

## Audit-Feld-Auswirkungen

Keine Änderung. Neue Ausbuchungen werden wie heute angelegt (`CreatedAt/CreatedBy/CreatedByWindows`
aus `ICurrentUserService`, `StockMovementsController.cs:544-549`); `StockMovement` wird nicht
aktualisiert, also kein `Modified*`-Pfad.

## Rollen- und Zugriffsfilter-Auswirkungen

Keine. Beide Aktionen bleiben unter `[RequireStockKeyUserAccess]` (admin, stock_keyuser, picking,
`CurrentUserService.cs:134-135`). Keine neue Rolle → `controller.md`/`RoleOverview` unberührt.

## Listen-View-Pattern-Pflichten

`OutboundAll` ist eine unpaginierte Einzelplatz-Ansicht, keine Liste im Sinne von ADR 0005 – bleibt
begründete Ausnahme. Die neue Spalte bringt keine Spaltenfilter-/Pagination-Pflicht (eine Tabelle
über den Bestand **eines** Lagerplatzes).

## Akzeptanzkriterien

1. **Spalte:** Platz L mit Artikel A (eingebucht mit FA `1234567`), Artikel B (ohne FA), Artikel C
   (zweimal eingebucht, FA `111` und `222`). Die Tabelle zeigt A „1234567", B leer, C „111, 222".
2. **Zerlegung:** Eine Bewegung mit Text „A;B" am Paar ergibt in der Spalte „A, B"; Wiederholungen
   erscheinen einmal.
3. **Restbestand (Antwort 3):** Artikel D mit FA `333` eingebucht und ohne FA vollständig ausgebucht
   erscheint nicht in der Tabelle; ein FA-Text eines Artikels ohne Restbestand taucht in keiner Spalte
   auf und wird von keinem Filter getroffen.
4. **Filter (Antwort 1):** FA-Feld `1234567`, bestätigen → nur A ausgebucht (Menge = Platz-Bestand),
   B und C bleiben unverändert. Ausbuchung trägt Text `1234567` und, falls der FA im WMS eindeutig
   ist, `ProductionOrderId`.
5. **Exakt:** FA-Feld `123` trifft A (`1234567`) **nicht**; Groß-/Kleinschreibung egal, Leerzeichen
   um Nummern egal.
6. **Liste im Feld:** FA-Feld `111; 1234567` → A und C ausgebucht, B bleibt; Ausbuchungen ohne
   Verknüpfung (Liste, wie v1.87.0).
7. **Kein Treffer:** FA-Feld `999` → nichts gebucht, Warnmeldung nach Anforderung 6, Seite zeigt
   weiter Platz L; der Knopf war schon vor dem Absenden deaktiviert mit Hinweis.
8. **Leer = wie heute (Regression):** FA-Feld leer → alle Artikel mit Bestand > 0 ausgebucht,
   Meldung und Redirect wie bisher.
9. **Rückmeldung:** nach gefilterter Ausbuchung nennt die Erfolgsmeldung Anzahl, FA und Anzahl
   verbleibender Artikel; die Seite zeigt Platz L mit dem Rest.
10. **Vorschau:** Knopftext „Alle N Artikel ausbuchen" (leer) bzw. „M von N Artikeln ausbuchen (FA …)";
    Bestätigungsdialog listet bei Filter die Artikelnummern.
11. **Gemischte Zeile** (vorbehaltlich Rückfrage 5, Empfehlung a): FA-Feld `111` → C wird mit
    vollem Platz-Bestand ausgebucht; der Dialog nennt „C enthält auch FA 222".
12. **Wagen-Konflikt unverändert:** `GetProductionOrdersAtLocationAsync` liefert für denselben Platz
    dasselbe wie vorher (bestehender Test grün); Konflikt-Dialog der Kommissionierung unverändert.
13. **Wagen-Vorbelegung** (vorbehaltlich Rückfrage 4, Empfehlung a): Wagen mit Artikeln von FA A und
    B plus einem Artikel ohne FA → Feld vorbelegt „A; B", Knopf zeigt „2 von 3", nach Bestätigen
    bleibt der Artikel ohne FA auf dem Wagen und die Erfolgsmeldung nennt ihn als verbleibend.
14. **Sage:** Je ausgebuchter Zeile genau eine Ausbuchung über `AddAsync`; nicht ausgebuchte Zeilen
    erzeugen keinen Queue-Eintrag (Regel des Decorators unverändert).
15. **Tests:** Repository-Tests für `GetProductionOrdersByArticleAtLocationAsync` (AK 1–3),
    Controller-Tests für AK 4–8 und 11; `LagerbuchungFaVerknuepfungTests` angepasst (Mock der neuen
    Methode, sonst bucht `OutboundAllConfirm(2, "1065141")` nichts mehr) und grün.

## Test-Szenarien

Neues Szenario **TS-169** in `docs/TESTSZENARIEN.md` (Lagerplatz ausbuchen mit FA), Verweis aus
TS-2.4; danach `secondbrain/tests/testszenarien-index.md` nachziehen. Standort: AKE und IDEAL (die
Seite hat keinen Standortschalter, `StockMovementsController.cs:482-557`).

- **Vorbedingungen:** Rolle `stock_keyuser`. Platz L: A eingebucht 5 mit FA `1234567`; B 3 ohne FA;
  C 2 mit FA `111` + 1 mit FA `222`; D 4 mit FA `333` eingebucht, danach ohne FA 4 ausgebucht.
  Für 169.8 ein Wagen W mit Artikeln von FA A und B (Kommissionierung) plus ein Artikel ohne FA
  (manuelle Umbuchung). Sage-Lagerbuchung auf DEV aus oder L ohne `SageBuchungErlaubt`, außer bei 169.9.
- **Schritte / erwartet:**
  - 169.1 `/StockMovements/OutboundAll`, Platz L laden → Spalte „Fertigungsauftrag": A `1234567`, B leer,
    C `111, 222`; D fehlt.
  - 169.2 Feld `1234567` tippen → Knopf „1 von 3 Artikeln ausbuchen (FA 1234567)", Zeile A markiert;
    bestätigen → nur A weg, Meldung nennt 2 verbleibende, Seite zeigt L mit B, C.
  - 169.3 Feld `123` → Knopf deaktiviert, Hinweis „Kein Artikel gehört zu diesem FA"; per manipuliertem
    POST trotzdem senden → Warnung, nichts gebucht.
  - 169.4 Feld `111` → Dialog nennt C „enthält auch FA 222"; bestätigen → C komplett (3) ausgebucht.
  - 169.5 Feld `333` → kein Treffer (D ohne Restbestand).
  - 169.6 Bewegungshistorie: Ausbuchungen aus 169.2/169.4 tragen den FA-Text; 169.2 verknüpft, falls
    der FA im WMS existiert.
  - 169.7 **Regression:** Feld leer → alle restlichen Artikel ausgebucht, Verhalten wie TS-2.4.
  - 169.8 **Wagen** (nach Antwort 4): Aufruf über den Konflikt-Dialog der Kommissionierung → Feld
    vorbelegt; Verhalten laut Entscheidung prüfen (Empfehlung a: Artikel ohne FA bleibt, wird gemeldet).
  - 169.9 **Sage** (nur wenn auf DEV aktiv): je ausgebuchter Zeile ein Queue-Eintrag, für nicht
    ausgebuchte keiner.
- **Negativfälle:** 169.3, 169.5; Rolle `stock` ohne `stock_keyuser` → Zugriff verweigert (wie TS-2.4).
- **Automatisch:** `StockMovementRepositoryProductionOrderTests` (neue Fälle je Artikel + bestehender
  Zerlegungstest), Controller-Tests zu OutboundAllConfirm.

## Etappen (nur bei epic: true)

Kein Epic – ein Dev-Lauf, ein Commit. Reihenfolge im Lauf: Helfer + Repository + Tests → Controller
+ Tests → View/JS → Doku/Version.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (epic: false) | – | – |

## Abgleich mit der Kritischen Prüfung vom 2026-08-05

Der Prüfabschnitt unten bleibt unverändert. Stand der Merkposten:
- **Kern-Entscheidung A/B:** erledigt durch Antwort 1 (A).
- **Mehrere FA je Zeile:** erledigt durch Antwort 2 (alle, kommagetrennt).
- **Konsistenz mit Teil 1:** erledigt durch Antwort 3; Teil 1 ist gemerged (`onlyActualStock`).
  Abweichung bewusst: exakte Einzelnummer statt Teilstring, weil hier gebucht wird (siehe Entwurf).
- **Sage-Enqueue:** weiter gültig und eingehalten – Buchung bleibt über `AddAsync` des Decorators
  (AK 14). Neu sichtbar dadurch: gemischte Zeilen melden auch fremde FA-Anteile an Sage → Rückfrage 5.
- **Empfehlung „normale /review-Runde nachholen":** steht noch aus (vor Schranke 1).

## Deploy

**Vorläufig – der QA-Lauf bestätigt gegen den echten Diff.**

- **Version:** Vorschlag 1.99.46 (1.99.45 ist für die Updater-Spec vorgeschlagen); bei Umsetzung die
  nächste freie Nummer, beide `AppVersion.cs` + Anwender-Changelog + Brain-Changelog.
- **Web-App:** ja (Controller, Repository, ViewModel, View).
- **Service:** nein (nur Versionsnummer, falls im Bündel üblich mitgezogen).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine – kein SQL, keine Einstellung,
  kein Leitstand-Schritt, keine Ordner/Rechte.
- **Publish-Befehle:**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rückfragen

1. **ENTSCHIEDEN (Antwort 1):** Variante A – das FA-Feld filtert.
2. **ENTSCHIEDEN (Antwort 2):** alle FA kommagetrennt in der FA-Spalte.
3. **ENTSCHIEDEN (Antwort 3):** wie Teil 1 – echter Bestand als Grundlage, FA nur aus Bewegungen
   mit Restbestand.
4. **Kommissionierwagen-Vorbelegung:** Auf einem Wagen ist das FA-Feld heute automatisch mit allen
   FA des Wagens vorbelegt (`StockMovementsController.cs:501-507`). Mit Antwort 1 wirkt das als
   Filter: Artikel **ohne** FA-Text bleiben auf dem Wagen, heute wird alles ausgebucht (TS-2.4).
   (a) so lassen, Restartikel sichtbar melden (**Empfehlung** – Wagen werden über die Kommissionierung
   in der Regel mit FA-Text befüllt, `PickingTransferService.cs:75,210-218`; ohne FA am
   Kommissionierauftrag ist der Text leer – wie oft das vorkommt, ist ungeprüft; Text und Verknüpfung der Ausbuchung bleiben
   erhalten), (b) auf Wagen nicht mehr vorbelegen (leer = alles, Ausbuchung ohne FA-Text, TS-115.12 und
   130.5.6 ändern sich), (c) unveränderte Vorbelegung gilt als „kein Filter" (verdeckte Sonderregel)?
5. **Gemischte Zeile:** Trägt ein Artikel am Platz FA X und Y und gefiltert wird nach X, wird – da
   Bestand nicht FA-genau ist (Antwort 3) – der **ganze** Platz-Bestand ausgebucht, inklusive des
   Anteils von Y, mit Sage-Meldung. (a) ganze Zeile ausbuchen, im Bestätigungsdialog benennen
   (**Empfehlung**), (b) gemischte Zeilen beim Filter auslassen und melden?
6. **IDEAL Haupt-/Sub-FA:** Der Filter vergleicht den FA-**Text** der Bewegungen (wie Teil 1). Wer auf
   IDEAL die Haupt-FA eingibt, trifft keine Bewegungen, die mit der Sub-FA-Nummer (BelID) getaggt sind
   (Auflösung beider Formen nur in `FindForBookingAsync`, Test `LagerbuchungFaVerknuepfungTests.cs:29-47`).
   Reicht der Textvergleich (**Empfehlung**, ein Ort, wie Teil 1), oder sollen auf IDEAL zusätzlich die
   über `ProductionOrderId` verknüpften Sub-FA einer eingegebenen Haupt-FA mitzählen?

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. → **A: das FA-Feld filtert** – nur Artikel, die laut Bewegungen zu diesem FA gehören, werden ausgebucht (Gerald 2026-11-19 20:10)
2. → **alle FA kommagetrennt** in der FA-Spalte (Gerald 2026-11-19 20:10)
3. → **wie Teil 1**: echter Bestand als Grundlage, FA nur aus Bewegungen mit Restbestand (Gerald 2026-11-19 20:10)

klären wir später - bitte im backlog belassen.

## Freigabe-Antworten Runde 2 (Rückfragen 4–6, Mensch füllt aus — Schranke 1)

4. →
5. →
6. →

## Kritische Pruefung (2026-08-05)

**ZURUECKGESTELLT — keine Freigabe-Pruefung durchgefuehrt.** Der Mensch hat diese Anforderung
bewusst vertagt („klären wir später - bitte im backlog belassen", Command-Notiz „wir werden diese
anforderung später angehen"). Die drei Freigabe-Antworten sind **unbeantwortet** (nur Pfeile) — eine
Anwalt-des-Teufels-Freigabe-Pruefung waere gegenstandslos, weil es noch keine Entscheidungen zu
pruefen gibt. Die Spec bleibt als Entwurf in `specs/entwurf/` liegen und wird **nicht** finalisiert.

**Damit beim spaeteren Wiederaufgreifen nichts verloren geht** (kein Ersatz fuer eine echte Pruefung,
nur ein Merkzettel):
- **Kern-Entscheidung zuerst (open_question 1):** Was heisst „Aktivierung von FA-Ausbuchung"? —
  Variante **A** (das FA-Textfeld auf `OutboundAllConfirm` wirkt als **echter Filter**: nur die zu
  dieser FA gehoerenden Artikel werden ausgebucht, statt heute ALLE am Platz) vs. Variante **B**
  (Checkbox/Auswahl **je Zeile**). Ohne diese Wahl ist der Fix nicht spezifizierbar — das ist der
  erste Punkt, den der Mensch beim Wiederaufnehmen beantworten muss.
- **Mehrdeutige FA je Zeile (open_question 2):** wenn eine Artikel/Lagerplatz-Kombination aus
  mehreren FA-getaggten Bewegungen stammt — Anzeige als Liste oder nur juengste/haeufigste FA?
- **Konsistenz mit Teil 1:** Die FA-Spalten-Datenquelle beruehrt dieselbe „Bestand vs.
  Bewegungssaldo"-Frage wie Teil 1 (Bugfix FA-Hinweis). Beim Wiederaufgreifen mit dem dann
  finalisierten Teil-1-Verhalten (Variante B / nur Ist-Bestand) abgleichen.
- **Cross-Feature-Merkposten:** `OutboundAll` erzeugt `Ausbuchung`en ueber
  `IStockMovementRepository.AddAsync` — seit v1.28.0 haengt daran der Sage-Lagerbuchungs-Enqueue.
  Wenn die FA-Filterung (Variante A) die Menge der ausgebuchten Zeilen aendert, aendert sich damit
  auch die Menge der Sage-Meldungen; bei der Umsetzung mitdenken (kein Bypass des Repository-Pfads).

### Empfehlung

**ZURUECKGESTELLT (bewusst, durch den Menschen). Nicht freigeben; im Backlog/Entwurf belassen, bis
die Kern-Entscheidung (Variante A vs. B) getroffen ist. Danach normale /review-Runde nachholen.**

> [!important] Wiederaufnahme Gerald 2026-11-19 20:10: Kern-Entscheidungen 1–3 beantwortet (oben). Spec stammt aus v1.2x – Zeilenangaben und Code-Stand sind veraltet; vor Freigabe Überarbeitung durch den spec-agent gegen den Bündel-Stand v1.99.44.
