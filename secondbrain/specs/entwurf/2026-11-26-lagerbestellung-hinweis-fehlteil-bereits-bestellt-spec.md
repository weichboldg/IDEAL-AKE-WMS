---
type: spec
title: "Lagerbestellung: Hinweis „bereits als Fehlteil bestellt“ mit nächstem Liefertermin"
slug: 2026-11-26-lagerbestellung-hinweis-fehlteil-bereits-bestellt-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-lagerbestellung-hinweis-fehlteil-bereits-bestellt]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - IdealAkeWms/Services/LieferterminTexte.cs (neu, verschoben aus WarehousePickingController)
  - IdealAkeWms/Controllers/WarehousePickingController.cs (LieferterminTexteAsync/LieferterminZelle entfernen, Aufrufer :265 und :605 umstellen)
  - IdealAkeWms/Controllers/WarehouseRequisitionsController.cs (Edit, Konstruktor, FehlteilHinweis)
  - IdealAkeWms/Models/ViewModels/WarehouseRequisitionEditViewModel.cs
  - IdealAkeWms/Views/WarehouseRequisitions/Edit.cshtml
  - IdealAkeWms/Views/Help/Index.cshtml, Views/Help/Changelog.cshtml
  - IdealAkeWms.Tests (LieferterminOffeneBestellungenTests – Verweis auf LieferterminZelle; FehlteileKarteV181Tests, WarehouseRequisitionsControllerTests – nur Konstruktor; neue Tests)
open_questions:
  - "Stücklisten-Schnellerfassung (Bom.cshtml:1462 → quick-add): Hinweis dort nur beim späteren Öffnen des Entwurfs (Vorschlag) oder auch direkt in der Rückmeldung der Stückliste?"
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

Ein Werker bestellt einen Artikel neu, auf den er schon als Fehlteil wartet. Das Lager bekommt dann eine zweite Anforderung
für dieselbe Lücke, und der Werker weiß nicht, dass die Ware schon kommt. Ein Hinweis direkt an der Position („bereits als
Fehlteil bestellt mit Lagerbestellung #xy, nächster Liefertermin …“) vermeidet die Doppelbestellung, ohne die Erfassung zu
blockieren. Daten und Regeln gibt es schon: die Fehlteil-Liste „Meine Fehlteile“ und den Liefertermin aus v1.99.85.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Lager- und Glasbestellung im Entwurf (`WarehouseRequisitions/Edit`): Hinweis je Position, deren Artikel für den Benutzer
   ein offenes Fehlteil ist.
2. Die Liefertermin-Logik aus v1.99.85 zieht vom `WarehousePickingController` an eine gemeinsame Stelle
   (`Services/LieferterminTexte.cs`), damit beide Controller sie nutzen. Das Verhalten bleibt unverändert.

**Out-of-Scope**
- Kein Blockieren, keine Rückfrage, kein Pflicht-Haken. Abschicken bleibt unverändert (`WarehouseRequisitionsController.cs:193-247`).
- Kein Hinweis an abgeschickten, erledigten oder stornierten Listen, nur im Entwurf.
- Stücklisten-Schnellerfassung (`Views/Picking/Bom.cshtml:1462` → `WarehouseRequisitionsApiController.QuickAdd`, `:171-296`):
  Ihre Rückmeldung bleibt unverändert. Der Hinweis erscheint, sobald der Benutzer den Entwurf öffnet (Vorschlag, Rückfrage 1).
- Keine Fehlteil-Menge im Hinweis, kein Link auf die Fehlteil-Liste (siehe E6).
- Keine Änderung an der Fehlteil-Liste, an „Lager: Eingehende Listen“, an der Artikelinfo oder an Bestellvorschlägen.

## Fachliche Anforderungen

### Belegte Ausgangslage (Code im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, v1.99.85)

- **Erfassungsweg = AJAX mit anschließendem Neuladen.** Die Artikelsuche in `Edit.cshtml` schickt
  `POST /api/warehouserequisitions/{id}/items` per `fetch` (`Views/WarehouseRequisitions/Edit.cshtml:274-278`). Bei Erfolg folgt
  sofort `location.reload()` (`:279-281`), bei Fehler `alert(j.error)` (`:282-284`). Der Endpunkt antwortet nur `Ok()` ohne Inhalt
  (`Controllers/Api/WarehouseRequisitionsApiController.cs:65-108`). Menge ändern (`:287-295`) und Entfernen (`:296-302`) laufen
  ebenfalls per `fetch`, Entfernen mit Neuladen. Abschicken ist ein Formular-Post (`Edit.cshtml:116-119`).
  → Eine Hinweis-Nachricht in der AJAX-Antwort würde das `location.reload()` sofort verwerfen. Der Hinweis wird deshalb
  **serverseitig in `Edit` gerendert**. Nach dem Hinzufügen lädt die Seite neu, und der Hinweis steht sofort an der neuen Zeile.
  Dafür braucht es keine JS-Änderung, und derselbe Weg gilt für alle Positionen im Entwurf, egal wie sie hineingekommen sind.
- **„Meine Fehlteile“: Regel und Filter.** Die Karte auf `WarehouseRequisitions/Index` (`Views/WarehouseRequisitions/Index.cshtml:40-55`)
  zählt über `GetShortageCountsForUserAsync(userId, activeType)` (`Controllers/WarehouseRequisitionsController.cs:93-94`) und
  verlinkt auf `MissingParts/Index?tab=WillBeRestocked&type=<aktiver Typ>&mineOnly=true` (`Index.cshtml:48-49`). Beide filtern gleich:
  - Fehlteil = `ShortageStatus == WillBeRestocked` und Listenstatus `Closed` oder `PartiallyDelivered`
    (`Data/Repositories/WarehouseRequisitionRepository.cs:400-402`; Karte `:480-481,487`).
  - Bestelltyp = der aktive Typ (`WarehouseRequisitionRepository.cs:404-405`; Karte `:485`).
  - Werkbank ∈ Werkbänke, denen der Benutzer zugeordnet ist (`ProductionWorkplaceUsers`): Liste über
    `_workplaces.GetByUserIdAsync(userId)` (`Controllers/MissingPartsController.cs:57-65`,
    `Data/Repositories/ProductionWorkplaceRepository.cs:72-80`) → `workplaceIds.Contains(ProductionWorkplaceId)`
    (`WarehouseRequisitionRepository.cs:408-409`); Karte `:473-482`.
  - Ohne Werkbank-Zuordnung gibt es keine Treffer (`MissingPartsController.cs:67-94`, Karte `WarehouseRequisitionRepository.cs:477`).
  - Ein Ersteller-Filter fehlt. Fehlteile von Kollegen an denselben Werkbänken zählen mit. Die Listennummer und den Ersteller
    sieht der Benutzer dort schon heute (`Views/MissingParts/Index.cshtml:119`).
- **Wer bearbeitet den Entwurf?** Nur der Ersteller. `Edit` gibt sonst `Forbid()` zurück (`WarehouseRequisitionsController.cs:168-175`).
  „Der Benutzer“ ist damit immer der Ersteller der neuen Liste.
- **Fehlteil-Status: Schreibwege.** `ShortageStatus` wird nur an zwei Stellen geschrieben: im Abschluss
  (`WarehouseRequisitionRepository.cs:224-225`) und in „Speichern“ (`:339-345`), beide aus `WarehousePickingController` (Lager).
  Einen eigenen Zustand „nachgeliefert“ gibt es nicht. Ein Fehlteil bleibt offen, bis das Lager den Status zurücksetzt.
  Ein Entwurf (`Draft`) ist nie Teil der Fehlteil-Menge (Statusfilter `:401-402`), die neue Liste trifft sich also nie selbst.
- **Liefertermin (v1.99.85).** `WarehousePickingController.LieferterminTexteAsync` (`Controllers/WarehousePickingController.cs:272-284`)
  liefert (Stand, Artikelnummer → Zelltext) mit fester Abfragezahl: Stand, Bestellpositionen über
  `ISagePurchaseOrderLineRepository.GetByArtikelnummernAsync`, Erfasst über `IPurchaseOrderReceiptRepository.GetErfasstJeBelPosAsync`,
  Rest über `ErfasstJeBelPos.Rest`. `DUMMY` ist ausgenommen (`:276-277`). Ist der Cache leer, kommt `(null, null)` zurück (`:275`).
  Den Zelltext baut `LieferterminZelle` (`:288-296`): „—“, „dd.MM.yyyy“, Zusatz „ überfällig“ bei Termin < heute,
  „ohne Termin“, „ (+N)“ für weitere offene Positionen. Aufrufer sind `Details` (`:265`) und der Druck-Filter (`:605`).
  Beides ist privat bzw. hängt am Lager-Controller → für die Wiederverwendung an eine gemeinsame Stelle (Technik 1).
- **Typen.** Der Entwurf hat genau einen Typ (`WarehouseRequisitionEditViewModel.Type`, `Models/ViewModels/WarehouseRequisitionEditViewModel.cs:10`).
  Lager- und Glasbestellung nutzen dieselbe View (`Edit.cshtml:3`).

### Entscheidungen

- **E1 „Für ihn“ (Entscheidung Gerald im Chat 2026-11-26):** Es gilt derselbe Benutzer bzw. dieselbe Werkbank, genau nach der
  bestehenden Logik von „Meine Fehlteile“, 1:1 übernommen. Belegt heißt das: Fehlteile (`WillBeRestocked`, Liste `Closed`/`PartiallyDelivered`)
  aus Listen **aller Werkbänke, denen der angemeldete Benutzer zugeordnet ist**, mit **demselben Bestelltyp** wie der Entwurf. Einen
  Ersteller-Filter gibt es nicht. Ohne Werkbank-Zuordnung gibt es keinen Hinweis. Fundstellen stehen oben unter „Meine Fehlteile“.
  Übernommen wird auch der Randfall „Liste `Closed` mit Haken Fehlteil“ (`DeriveStatus` schließt bei vollständiger Lieferung trotz Haken,
  `WarehouseRequisitionRepository.cs:240-250`). Die Fehlteil-Liste zeigt ihn heute ebenfalls, also auch der Hinweis.
- **E2 Zeitpunkt (Entscheidung Gerald im Chat 2026-11-26):** Der Hinweis kommt sofort bei der Eingabe, direkt nachdem der Artikel in
  die Liste aufgenommen wurde, nicht erst beim Abschicken. Umsetzung nach dem belegten Erfassungsweg (AJAX + `location.reload()`,
  `Edit.cshtml:274-281`): Der Hinweis wird in `Edit` je Position gerendert und erscheint damit sofort nach dem Neuladen an der neuen Zeile.
- **E3 Nur Hinweis (Vorschlag):** Der Hinweis blockiert nichts. Abschicken, Menge ändern und Entfernen bleiben unverändert.
- **E4 Mehrere Fehlteil-Listen (Vorschlag):** Die Listennummern stehen absteigend nach Abschlussdatum, wie die Liste sortiert
  (`WarehouseRequisitionRepository.cs:427`), höchstens drei, danach „und N weitere“. Das folgt dem Muster „Take(3) + Rest“ in
  `VorschlagHinweis` (`WarehousePickingController.cs:193-194`). Jede Nummer erscheint nur einmal, auch wenn eine Liste den Artikel
  mehrfach als Fehlteil hat.
- **E5 Liefertermin-Satz (Vorschlag), Text 1:1 aus `LieferterminZelle`, damit „überfällig“ und „(+N)“ wie in v1.99.85 lauten:**
  - Cache leer (`Stand == null`): „Liefertermin unbekannt (offene Bestellungen werden nicht aus Sage gelesen).“ Das betrifft
    typischerweise IDEAL ohne Abgleich (`Sync:EinkaufBestellungenEnabled`, siehe Prüfschritt P-1 der v1.99.85-Spec).
  - Cache gefüllt, kein Eintrag oder Zelltext „—“: „Keine offene Bestellung in Sage.“
  - Sonst: „Nächster Liefertermin: <Zelltext>.“, z. B. „Nächster Liefertermin: 12.12.2026 überfällig (+1).“ oder
    „Nächster Liefertermin: ohne Termin.“ Die Hilfe erklärt „(+N)“ als weitere offene Bestellpositionen.
- **E6 Kein Link (Vorschlag):** Die Listennummern sind reiner Text. Der Link der Fehlteil-Liste führt auf `WarehousePicking/Details`
  (`Views/MissingParts/Index.cshtml:119`), das `[RequireLagerProcessingAccess]` braucht (`WarehousePickingController.cs:13`). Ein Werker
  mit nur `lagerbestellung` hätte dort keinen Zugriff. ponytail: Ein Link kommt erst, wenn er gebraucht wird.
- **E7 DUMMY:** Positionen mit `DUMMY` (`Article.DummyArticleNumber`) bekommen nie einen Hinweis. Eine DUMMY-Fehlteilposition ist
  ein anderer Artikel, nur mit derselben Platzhalternummer.
- **E8 Artikelabgleich:** `Trim()` und Groß/Klein egal (`StringComparer.OrdinalIgnoreCase`), wie bei `FraesenPfade`/Liefertermin.

### Anforderungen

- **F1** In `WarehouseRequisitions/Edit` mit Status `Draft` steht unter der Artikelnummer jeder betroffenen Position ein Hinweis.
  Er sitzt in der gesperrten Spalte `article-number` (`Edit.cshtml:152` `"locked": true`), damit die Spaltenauswahl ihn nicht ausblenden kann.
  Der Text lautet:
  `Hinweis: bereits als Fehlteil bestellt mit <Lagerbestellung|Glasbestellung> #45[, #38, #31][ und N weitere] (wird nachgeliefert). <Liefertermin-Satz E5>`
  Bei Typ Glas steht „Glasbestellung“, sonst „Lagerbestellung“, passend zum Entwurf, denn der Typ ist nach E1 gleich.
- **F2** Darstellung als `div.small` in Bootstrap-Warnton mit dem vorangestellten Wort „Hinweis:“, damit die Farbe nicht der einzige
  Bedeutungsträger ist. Kontrast WCAG AA. Auf der Seite gibt es kein Banner und kein Modal.
- **F3** Ohne Werkbank-Zuordnung, ohne Treffer, bei abgeschickter Liste und bei DUMMY gibt es keinen Hinweis. Die Seite bleibt sonst unverändert.
- **F4** Abfragezahl: Die Werkbänke (1) und die Fehlteile (2: Count + Seite) werden nur geladen, wenn der Entwurf eine Nicht-DUMMY-Position hat.
  Die Liefertermine (3) werden nur geladen, wenn es mindestens einen Fehlteil-Treffer gibt. Keine Abfrage je Position.

## Technischer Loesungsentwurf

ponytail: vorhandene Regel und Repository-Methoden wiederverwenden, keine neue Repository-Methode, kein neuer Endpunkt, kein JS.

1. **Gemeinsame Stelle `IdealAkeWms/Services/LieferterminTexte.cs`** (statische Klasse; Muster: statischer Helfer mit Repository-Parameter
   wie `RequireFraesenPfadAktivFilter.IstAktivAsync(_settings)`, `WarehousePickingController.cs:261`):
   - `internal static string Zelle(IEnumerable<(DateTime? Liefertermin, decimal Offen)> positionen, DateTime heute)`, unverändert aus
     `WarehousePickingController.LieferterminZelle` (`:288-296`) verschoben.
   - `internal static async Task<(DateTime? Stand, Dictionary<string,string>? Texte)> JeArtikelAsync(ISagePurchaseOrderLineRepository poLines,
     IPurchaseOrderReceiptRepository receipts, IEnumerable<string> artikelnummern)`, unverändert aus `LieferterminTexteAsync` (`:272-284`) verschoben.
   - `WarehousePickingController`: Die beiden Methoden entfallen. Die Aufrufe `:265` und `:605` laufen über `LieferterminTexte.JeArtikelAsync(_poLines, _receipts, …)`.
     Das Verhalten bleibt gleich, die Bestandstests (`LieferterminOffeneBestellungenTests`, Druck-Filter-Test) bleiben grün.
     In `LieferterminOffeneBestellungenTests.cs:45` wird nur der Verweis auf `LieferterminTexte.Zelle` umgestellt.
   - Kein neuer DI-Eintrag (statisch). Die Repositories sind registriert, sie werden bereits in `WarehousePickingController` injiziert (`:27-28,41-42`).
2. **`WarehouseRequisitionsController`**: Der Konstruktor bekommt `ISagePurchaseOrderLineRepository` und `IPurchaseOrderReceiptRepository`
   dazu (`:21-30`). In `Edit` (`:164-191`) wird nur bei `r.Status == Draft` gerechnet:
   - `nummern` = Positions-Artikelnummern, getrimmt, ohne DUMMY, distinct (OrdinalIgnoreCase); leer → fertig.
   - `wpIds = (await _workplaces.GetByUserIdAsync(userId)).Select(w => w.Id).ToList()`; leer → fertig (wie `MissingPartsController.cs:67-69`).
   - `(rows, _) = await _repo.GetMissingPartsAsync(ShortageStatus.WillBeRestocked, r.Type, wpIds, null, null, null, 1, int.MaxValue)`.
     Das ist dieselbe Methode wie die Fehlteil-Liste, also wird die Regel nicht kopiert. Präzedenzfall für `int.MaxValue`:
     `WarehousePickingController.cs:97`. Die Methode sortiert nach `ClosedAt` absteigend (`WarehouseRequisitionRepository.cs:427`).
     ponytail: Es werden alle Fehlteile der eigenen Werkbänke geladen und im Speicher auf die Entwurfsartikel gefiltert. Eine Artikelnummer-Bedingung im
     Repository kommt erst, wenn die Menge messbar groß wird (die Karte „Meine Fehlteile“ zeigt die Zahl).
   - Treffer = `rows` mit `ArticleNumber.Trim()` ∈ `nummern`; gibt es keine → fertig.
   - `(stand, texte) = await LieferterminTexte.JeArtikelAsync(_poLines, _receipts, trefferArtikel)`.
   - `vm.FehlteilHinweise = Dictionary<string,string>(OrdinalIgnoreCase)`: Artikelnummer → `FehlteilHinweis(listenIds, r.Type, stand, texte?.GetValueOrDefault(nr))`.
   - Neu: `internal static string FehlteilHinweis(IReadOnlyList<int> listenIdsNeuesteZuerst, WarehouseRequisitionType typ, DateTime? stand, string? zelltext)`
     baut den Text nach F1/E4/E5. Die Methode ist ohne View testbar (Muster `VorschlagHinweis`).
3. **`WarehouseRequisitionEditViewModel`**: `public Dictionary<string, string>? FehlteilHinweise { get; set; }` (null/leer = kein Hinweis).
4. **`Edit.cshtml`**: In der Zelle `<td>@i.ArticleNumber</td>` (`:55`) folgt, wenn `Model.FehlteilHinweise?.TryGetValue(i.ArticleNumber.Trim(), out var h) == true`,
   `<div class="small text-warning-emphasis">@h</div>`. Razor-Encoding greift, keine Daten in `<script>`. Kein JS, keine Spaltenänderung,
   `column-config` unverändert. View-Änderung → Skill `frontend-design` explizit aufrufen, Kontrast prüfen
   (`text-warning-emphasis` wird schon auf `Index.cshtml:50` für Fehlteile genutzt).
5. **Hilfe** `Views/Help/Index.cshtml`, Abschnitt Lagerbestellung: Hinweis, Regel „Meine Fehlteile“, Bedeutung von „(+N)“, Glas gleich.
   Hilfe-Detail-Regel.

## Migrations-/SQL-Auswirkungen

Keine. Keine Model-/Schemaänderung, keine EF-Migration, kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unverändert.
Die Abfragen nutzen bestehende Tabellen und Indizes (`IX_WarehouseRequisitionItems_ShortageStatus_WillBeRestocked`,
`Migrations/20260529101707_ReplaceIsFinalShortageWithShortageStatus.cs:48`).

## Einstellungen

keine (neu oder geändert). Gelesen wird nur der bestehende Cache-Zustand aus `Sync:EinkaufBestellungenEnabled` (unverändert).

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| keine | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Es ist eine reine Leseanzeige im Entwurf, nichts wird geschrieben.

## Rollen / Zugriff

Unverändert: `[RequirePickingOrStockOrLagerbestellungAccess]` + `[RequireLagerbestellungAktiv]` an `WarehouseRequisitionsController`
(`:10-11`; Rollen admin, picking, stock, stock_keyuser, lagerbestellung oder glasbestellung, `secondbrain/codebase/controller.md:70`).
Der Hinweis zeigt nur Listennummern und den Liefertermin. Die Fehlteil-Liste mit denselben Nummern ist für dieselben Rollen offen
(`[RequireStockOrLagerbestellungAccess]`, `controller.md:71`). Es gibt keine neue Rolle und keinen neuen Filter.

## Listen-View-Pattern

Nicht betroffen: Es gibt keine neue Tabelle und keine neue Spalte. Die Positionstabelle des Entwurfs bleibt unpaginiert mit Zahnrad (`Edit.cshtml:34-37`).

## Akzeptanzkriterien

1. Benutzer A ist Werkbank W1 zugeordnet. In Lagerbestellung #L1 (Werkbank W1) ist Artikel X als Fehlteil markiert (wird nachgeliefert), #L1 ist teilgeliefert.
   A legt eine neue Lagerbestellung an und fügt X hinzu. Nach dem automatischen Neuladen steht unter der Artikelnummer
   „Hinweis: bereits als Fehlteil bestellt mit Lagerbestellung #L1 (wird nachgeliefert). …“.
2. Hat #L1 ein Kollege B an Werkbank W1 erfasst, erscheint der Hinweis bei A trotzdem (Werkbank-Regel, kein Ersteller-Filter).
3. Ist das Fehlteil an einer Werkbank, der A **nicht** zugeordnet ist, erscheint kein Hinweis.
4. Ohne Werkbank-Zuordnung von A erscheint nirgends ein Hinweis, und die Seite funktioniert unverändert.
5. Gleicher Artikel als Fehlteil in mehreren Listen: Nummern absteigend nach Abschlussdatum, höchstens drei, danach „und N weitere“.
   Jede Nummer kommt nur einmal vor.
6. Liefertermin-Satz: bei offener Sage-Bestellung „Nächster Liefertermin: <wie Spalte Liefertermin in Lager-Details>“, inklusive
   „überfällig“ bei Termin < heute und „(+N)“. Ohne offene Bestellung (Cache gefüllt) „Keine offene Bestellung in Sage.“.
   Bei leerem Cache „Liefertermin unbekannt (offene Bestellungen werden nicht aus Sage gelesen).“.
7. Der Zelltext im Hinweis ist für denselben Artikel und Tag identisch mit der Spalte „Liefertermin“ in `WarehousePicking/Details`
   (eine Quelle `LieferterminTexte`).
8. Glasbestellung: Ein Glas-Fehlteil erscheint im Glas-Entwurf als „…mit Glasbestellung #G1…“. Ein Glas-Fehlteil erscheint **nicht**
   in einem Lager-Entwurf und umgekehrt (Typ wie „Meine Fehlteile“).
9. Nicht mehr offen: Setzt das Lager den Fehlteil-Haken von #L1 zurück (Speichern/Abschließen), erscheint beim nächsten Öffnen des
   Entwurfs kein Hinweis mehr. „Wird nicht nachgeliefert“ (`NoRestock`) löst keinen Hinweis aus.
10. DUMMY-Positionen bekommen nie einen Hinweis, auch wenn es DUMMY-Fehlteile gibt.
11. Der Hinweis blockiert nichts: Abschicken, Menge ändern und Entfernen funktionieren wie bisher. Nach Abschicken zeigt die Liste (Status
    Abgeschickt) keinen Hinweis mehr.
12. Der Hinweis bleibt sichtbar, auch wenn per Zahnrad die Spalte „Bezeichnung“ ausgeblendet ist (er steht in der gesperrten Spalte Artikel-Nr).
13. `WarehousePicking/Details` und der Druck-Filter „Liefertermin“ verhalten sich nach dem Verschieben unverändert (TS-206 Regression).
14. `dotnet build` und `dotnet test` sind grün.

## Test-Szenarien

Neues Kapitel **TS-207 „Lagerbestellung: Hinweis ‚bereits als Fehlteil bestellt‘ mit nächstem Liefertermin“** in `docs/TESTSZENARIEN.md`
(Stand 2026-11-26: höchste vergebene TS-206, `docs/TESTSZENARIEN.md:14893`, Index `secondbrain/tests/testszenarien-index.md:179`;
TS-205 reserviert für die Beschichtungs-Übersicht. Der Dev-Lauf prüft das erneut). Danach den Index nachziehen.

Vorbedingungen: AKE-DEV mit gefülltem Cache `SagePurchaseOrderLines`. Benutzer A (Rolle lagerbestellung, Werkbank W1, zusätzlich Glas),
Benutzer B (W1), Werkbank W2 ohne A. Testdaten über das Lager (Rolle stock) erzeugen: Liste abschicken, in Details Fehlteil anhaken,
abschließen. Artikel mit offener Bestellung über die Abfrage aus TS-206 suchen. Prüfabfrage (WMS-DB, nur lesen):
```sql
SELECT r.Id, r.Type, r.Status, w.Name AS Werkbank, r.CreatedBy, r.ClosedAt, i.ArticleNumber
FROM dbo.WarehouseRequisitionItems i
JOIN dbo.WarehouseRequisitions r ON r.Id = i.WarehouseRequisitionId
JOIN dbo.ProductionWorkplaces w ON w.Id = r.ProductionWorkplaceId
WHERE i.ShortageStatus = 1 AND r.Status IN (3, 5)  -- WillBeRestocked; Closed=3, PartiallyDelivered=5 (Models/WarehouseRequisitionStatus.cs:7,9)
ORDER BY i.ArticleNumber, r.ClosedAt DESC;
```

- **TS-207.1 Grundfall:** A, neuer Lager-Entwurf an W1, Artikel X (Fehlteil in #L1) hinzufügen → nach dem Neuladen Hinweis mit #L1 und Liefertermin (AK 1, 6).
- **TS-207.2 Kollege:** #L1 von B erfasst → Hinweis auch bei A (AK 2).
- **TS-207.3 Mehrere Listen:** X als Fehlteil in 4 Listen → drei Nummern, neueste zuerst, „und 1 weitere“ (AK 5).
- **TS-207.4 Liefertermin-Varianten:** Artikel mit überfälliger Bestellung → „überfällig“, mit mehreren offenen Positionen → „(+N)“,
  ohne Bestellung → „Keine offene Bestellung in Sage.“. Gegenprobe: gleicher Text in Lager → Details (Spalte Liefertermin) (AK 6, 7).
- **TS-207.5 Cache leer (IDEAL-DEV):** `SELECT COUNT(*) FROM dbo.SagePurchaseOrderLines` = 0 (oder wie TS-206.5 temporär sichern und leeren)
  → Hinweis mit „Liefertermin unbekannt …“ (AK 6).
- **TS-207.6 Glas:** Glas-Fehlteil G → im Glas-Entwurf „Glasbestellung #…“; ein gemeinsamer Artikel als Glas-Fehlteil im Lager-Entwurf → kein Hinweis (AK 8).
- **TS-207.7 Nicht blockieren / Spalten:** mit Hinweis Menge ändern, abschicken → klappt; „Bezeichnung“ per Zahnrad aus → Hinweis bleibt (AK 11, 12).
- **TS-207.8 Regression TS-206:** Details-Spalte und Druck-Filter „Liefertermin“ unverändert (AK 13).
- **Negativfälle:** Fehlteil an fremder Werkbank W2 → kein Hinweis (AK 3). A ohne Werkbank-Zuordnung → kein Hinweis (AK 4).
  Haken zurückgesetzt bzw. `NoRestock` → kein Hinweis (AK 9). DUMMY-Position → kein Hinweis (AK 10). Abgeschickte Liste → kein Hinweis (AK 11).
  Artikelnummer mit anderer Groß/Klein-Schreibung bzw. Leerzeichen → Hinweis trotzdem (E8).

Unit-Tests (neu, xUnit/FluentAssertions/InMemory):
- `WarehouseRequisitionsController.FehlteilHinweis`: eine, drei und fünf Listen, Glas/Lager-Wort, Cache leer, „—“/kein Eintrag, Zelltext mit überfällig/(+N).
- `WarehouseRequisitionsController.Edit` (InMemory): Treffer an eigener Werkbank, Kollege, fremde Werkbank, ohne Zuordnung, anderer Typ,
  `NoRestock`, Liste `Draft`/`Submitted` als Quelle (zählt nicht), DUMMY, abgeschickter Entwurf → `FehlteilHinweise` leer/null.
- `LieferterminTexte.Zelle`: bestehende Fälle aus `LieferterminOffeneBestellungenTests` auf die neue Stelle umgestellt.
- View-Text-Test: `Edit.cshtml` rendert `FehlteilHinweise` in der Artikel-Nr-Zelle (Muster `PurchaseOrdersControllerTests.cs:292`).

Manual-UAT: Neuladen nach dem Hinzufügen im Browser, Kontrast, Groß/Klein in SQL (CI-Kollation, nicht InMemory-beweisbar).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

*Vorläufig, der QA-Lauf bestätigt es gegen den echten Diff.*

- **Web-App:** ja (Controller, Service-Helfer, ViewModel und View im Web-Projekt).
- **Service:** nein (Dienst unverändert, nur Versionsgleichstand `AppVersion.cs`).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine. Es gibt kein SQL, keine Einstellung, keinen Leitstand-Schritt und keinen Ordner.
  Die Datenvoraussetzung `Sync:EinkaufBestellungenEnabled` hat schon ihren Eintrag (`nachlauf.psd1:470`).
- **Publish-Befehle** (im Worktree): `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`.
  Keine Reihenfolge, Web austauschen genügt.
- Version in beiden `AppVersion.cs` hochzählen (nächste freie im Bündel, Stand 2026-11-26 v1.99.86, Dev-Lauf prüft), Anwender-Changelog
  `Views/Help/Changelog.cshtml`, Hilfe `Views/Help/Index.cshtml`, `docs/anwender/03-lagerbestellung-glasbestellung.md` mit Foto gegen DEV.

## Offene Rueckfragen

1. Stücklisten-Schnellerfassung (`Views/Picking/Bom.cshtml:1462` → `WarehouseRequisitionsApiController.QuickAdd`, `:171-296`) legt Positionen
   im Entwurf an, ohne die Edit-Seite zu öffnen. **Vorschlag:** Dort gibt es keinen eigenen Hinweis. Er erscheint, sobald der Werker den
   Entwurf öffnet. Soll er auch direkt in der Rückmeldung der Stückliste erscheinen? Das hieße Antwort und JS der Stückliste erweitern,
   also mehr Umfang.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

(kein Jira-Vorgang)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Stücklisten-Schnellerfassung: Hinweis nur beim Öffnen des Entwurfs (Vorschlag) oder auch in der Stücklisten-Rückmeldung? →
