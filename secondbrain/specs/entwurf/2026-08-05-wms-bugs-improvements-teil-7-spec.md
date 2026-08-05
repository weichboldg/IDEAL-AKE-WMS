---
type: spec
title: "Lager-/Glasbestellung: Kommentarfunktion (Kopf-Ebene) + Dummy-Artikel bei unbekannter EK-Nummer"
slug: 2026-08-05-wms-bugs-improvements-teil-7-spec
status: Entwurf
created: 2026-08-05
updated: 2026-08-05
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Models/WarehouseRequisition.cs
  - IdealAkeWms/Models/ViewModels/WarehouseRequisitionEditViewModel.cs
  - IdealAkeWms/Models/ViewModels/WarehouseRequisitionListItemViewModel.cs
  - IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs
  - IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs
  - IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs
  - IdealAkeWms/Controllers/WarehouseRequisitionsController.cs
  - IdealAkeWms/Controllers/WarehousePickingController.cs
  - IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs
  - IdealAkeWms/Views/WarehouseRequisitions/Edit.cshtml
  - IdealAkeWms/Views/WarehousePicking/Index.cshtml
  - IdealAkeWms/Views/WarehousePicking/Details.cshtml
  - IdealAkeWms/Models/Article.cs (falls Dummy-Kennzeichnung per Flag entschieden wird)
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - SQL/84_*.sql (Platzhalter, naechste freie Nummer)
  - SQL/00_FreshInstall.sql
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Kommentarfeld: Soll der Kommentar (a) NUR vom Ersteller im Draft-Status editierbar sein (wie die Positions-Notiz \"Note\"), oder (b) waehrend des gesamten Lebenszyklus (auch nach Submit) editierbar bleiben - z. B. damit der Lagermitarbeiter in den Eingehenden Listen selbst einen Kommentar ergaenzen kann? Bestimmt, ob ein zweites Feld (Kommentar Werkbank vs. Kommentar Lager) noetig ist, analog Note/NoteEinkauf."
  - "Soll der Kommentar auch in die Submit-Benachrichtigungs-Mail (NotificationWorker) aufgenommen werden, oder ist die Sichtbarkeit ausschliesslich auf UI (Edit-Formular + Lager Eingehende Listen) beschraenkt, wie im Backlog woertlich verlangt?"
  - "Dummy-Artikel — zentrale Namensfrage: Soll die eingetippte, in Sage nicht gefundene EK-Nummer selbst als ArticleNumber des neu angelegten Artikels verwendet werden (Artikel wird spaeter beim naechsten Sage-Sync automatisch mit echten Sage-Daten anhand derselben ArticleNumber angereichert/upgedatet), ODER soll ArticleNumber buchstaeblich/literal auf \"DUMMY\" (bzw. ein generiertes DUMMY-Praefix-Schema wegen der UNIQUE-Constraint auf ArticleNumber) gesetzt werden? Das Backlog ist hier woertlich mehrdeutig (\"Der Artikel soll ... automatisch angelegt werden. 'DUMMY'.\")."
  - "Rollen/Zugriff: Artikel-Neuanlage ist heute exklusiv [RequireMasterDataAccess] vorbehalten. Soll die Dummy-Artikel-Anlage aus der Lager-/Glasbestellung heraus GENAU DAS umgehen (jeder mit lagerbestellung/glasbestellung/stock/picking-Rolle darf einen Dummy-Artikel anlegen), oder soll stattdessen ein Freigabe-/Review-Schritt durch masterdata dazwischengeschaltet werden (z. B. Bestellposition haengt in einem Pending-Status, bis masterdata den Artikel bestaetigt)?"
  - "Soll ein Dummy-Artikel dauerhaft von einem regulaer per Sage synchronisierten Artikel unterscheidbar bleiben (neues Bool-Flag z. B. Article.IsManuallyCreated, sichtbar/filterbar in der Artikelliste fuer eine spaetere Bereinigung durch masterdata), oder ist das bewusst nicht gewuenscht?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Zwei unabhängige, aber im selben Modul (Lager-/Glasbestellung) liegende Verbesserungen:

1. **Kommentarfunktion:** Die Werkbank soll eine Bestellung mit einem freien Kommentar versehen
   können, der auch dem Lager in den „Eingehenden Listen" sichtbar ist — heute gibt es nur die
   Positions-Notizen `Note`/`NoteEinkauf` je Zeile, aber keinen Kommentar auf Ebene der gesamten
   Bestellung.
2. **Dummy-Artikel:** Wird beim Hinzufügen eines Artikels zu einer Bestellung die eingegebene
   EK-Nummer nicht gefunden (Artikel existiert noch nicht in der App/in Sage), soll die Werkbank
   trotzdem eine Position mit einer frei eingegebenen Bezeichnung anlegen können, statt komplett
   blockiert zu sein — die App legt dafür selbstständig einen Platzhalter-Artikel an.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neues Kommentarfeld auf `WarehouseRequisition` (Kopf-Ebene, nicht Item-Ebene).
- Anzeige/Editiermöglichkeit im Edit-Formular der Werkbank (`Views/WarehouseRequisitions/Edit.cshtml`).
- Anzeige in den „Lager Eingehenden Listen" (`Views/WarehousePicking/Index.cshtml`, neue Spalte
  oder sichtbarer Hinweis) und in der Detailansicht (`Views/WarehousePicking/Details.cshtml`).
- Neuer Ablauf im Artikel-Hinzufügen-Dialog: bei „nicht gefunden" ein Eingabefeld für die
  Bezeichnung anbieten, das serverseitig einen neuen `Article`-Datensatz anlegt und diesen der
  Bestellung hinzufügt.

**Out-of-Scope**
- Keine Änderung an den bestehenden Positions-Notizen `Note`/`NoteEinkauf`.
- Keine Aufnahme des Kommentars in die Submit-/Storno-E-Mail (`NotificationWorker`) — siehe
  offene Rückfrage 2; falls gewünscht, ist das eine eigene Folge-Änderung am Mail-Template.
  Diese Spec beschränkt die Sichtbarkeit auf die im Backlog wörtlich genannten Stellen
  (Bestellformular + Eingehende Listen).
  Diese Spec beschränkt die Sichtbarkeit auf die im Backlog wörtlich genannten Stellen
  (Bestellformular + Eingehende Listen).
- Keine automatische Zusammenführung/Bereinigung von Dummy-Artikeln mit später echten
  Sage-Artikeln — die Selbstheilung über den bestehenden Sage-Artikel-Upsert-nach-ArticleNumber
  greift nur, wenn beide dieselbe `ArticleNumber` teilen (siehe technischer Lösungsentwurf und
  offene Rückfrage 3); ein aktives Abgleich-/Merge-Tool ist nicht Teil dieser Spec.

## Fachliche Anforderungen

### A) Kommentarfunktion

1. `WarehouseRequisition` bekommt ein neues Feld `Comment` (Freitext, begrenzte Länge).
2. Im Edit-Formular der Werkbank ist der Kommentar unten (nach der Artikelliste) sichtbar und
   editierbar — Umfang der Editierbarkeit je Status siehe offene Rückfrage 1.
3. In den „Lager Eingehenden Listen" (`/WarehousePicking`) ist der Kommentar sichtbar (Spalte
   oder Icon/Tooltip — Umsetzungsdetail im Review, Backlog verlangt nur „soll angezeigt werden").
4. In der Detailansicht (`/WarehousePicking/Details/{id}`) ist der Kommentar ebenfalls sichtbar.

### B) Dummy-Artikel

1. Findet die Artikelsuche beim Hinzufügen einer Position (`article-search` in
   `Edit.cshtml`, bzw. `POST /api/warehouserequisitions/{id}/items`) keinen Treffer für die
   eingegebene EK-Nummer, wird dem Anwender die Möglichkeit angeboten, eine Bezeichnung
   einzugeben.
2. Nach Bestätigung legt die App automatisch einen neuen `Article`-Datensatz an (Namens-/
   Nummernschema siehe offene Rückfrage 3) und fügt ihn als Position der Bestellung hinzu.
3. Der neu angelegte Artikel ist danach wie jeder andere Artikel im System sichtbar (Bestand,
   Bestellungen usw.).

## Ist-Zustand (Code-Referenzen)

**Kommentarfunktion:**
- `IdealAkeWms/Models/WarehouseRequisition.cs:1-40`: kein Kommentar-/Freitextfeld auf
  Kopf-Ebene. Die einzigen Freitextfelder in diesem Modul sind `WarehouseRequisitionItem.Note`
  und `WarehouseRequisitionItem.NoteEinkauf` (`IdealAkeWms/Models/WarehouseRequisitionItem.cs:26-37`)
  — beide **auf Positionsebene**, nicht auf Bestellungsebene.
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionEditViewModel.cs:1-14`: kein
  Kommentarfeld.
- `IdealAkeWms/Views/WarehouseRequisitions/Edit.cshtml:1-196`: kein Kommentarfeld, kein
  Speicherpfad dafür.
- `IdealAkeWms/Models/ViewModels/WarehouseRequisitionListItemViewModel.cs:1-13` (positional
  Record: `Id, WorkplaceName, CreatedBy, CreatedAt, SubmittedAt, ItemCount, Status`) — wird
  **identisch** in `WarehouseRequisitionsController.Index` (Zeile 76-83) **und**
  `WarehousePickingController.Index` (Zeile 73-75) verwendet; eine Erweiterung um `Comment`
  betrifft daher beide Call-Sites.
- `IdealAkeWms/Views/WarehousePicking/Index.cshtml:60-80`: Tabelle ohne Kommentar-Spalte.
- `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs:43-44` kennt bereits ein
  Muster für „nur ein Feld ohne Status-/RowVersion-Prüfung speichern"
  (`SaveNotesAsync`, dokumentiert als „Autosave, RowVersion bewusst ignoriert, weil nicht
  konfliktrelevant") — geeignetes Vorbild für einen analogen `SaveCommentAsync`.

**Dummy-Artikel:**
- `IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs:62-99` (`AddItem`):
  ```csharp
  var article = await _articles.GetByArticleNumberAsync(body.ArticleNumber);
  if (article == null)
      return BadRequest(new { error = "Artikel nicht gefunden." });
  ```
  Aktuell harter Abbruch ohne Möglichkeit, dennoch eine Position anzulegen.
- `IdealAkeWms/Views/WarehouseRequisitions/Edit.cshtml:130-147`: Artikelsuche über
  `/api/articles/search?q=...&type=...` (`ArticlesApiController.Search`,
  `IdealAkeWms/Controllers/ArticlesApiController.cs:21-48`) — bei 0 Treffern erscheint aktuell
  nur eine leere Ergebnisliste, kein „Dummy anlegen"-Pfad.
- `IdealAkeWms/Models/Article.cs:1-41`: `ArticleNumber` ist `[Required][StringLength(100)]` und
  in `ApplicationDbContext.cs:220` `HasIndex(e => e.ArticleNumber).IsUnique()` — jede
  Dummy-Anlage muss diese Eindeutigkeit respektieren.
- Artikel-Erstellung ist heute ausschließlich über `ArticlesController`
  unter `[RequireMasterDataAccess]` vorgesehen (`secondbrain/codebase/controller.md`,
  Zeile 50) — Rollen `admin`, `masterdata`. Die Rollen, die überhaupt Zugriff auf
  `WarehouseRequisitionsController`/`WarehouseRequisitionsApiController` haben
  (`[RequirePickingOrStockOrLagerbestellungAccess]`: admin, picking, stock, stock_keyuser,
  lagerbestellung, glasbestellung), sind eine **andere, breitere** Menge — insbesondere
  `lagerbestellung`/`glasbestellung` haben laut Glossar explizit **keinen** Stammdaten-Zugriff
  („eng abgegrenzt, ohne picking/stock"). Ein automatisches Anlegen von Artikeln aus diesem
  Endpunkt heraus wäre ein faktischer Rollen-Bypass für die Artikel-Neuanlage — siehe offene
  Rückfrage 4.
- `IDEALAKEWMSService/Services/SageImportService.cs:369-...` (`SyncArticlesAsync`) upserted
  Artikel **anhand von `ArticleNumber`** aus Sage — ein später in Sage angelegter Artikel mit
  derselben `ArticleNumber` würde einen bestehenden App-Artikel automatisch mit den echten
  Sage-Daten überschreiben (Description/Unit/ArticleGroup/ReorderLevel/PrimaryStorageLocation),
  **sofern** die Dummy-`ArticleNumber` mit der späteren echten Sage-`ArticleNumber`
  übereinstimmt (relevant für die Namensschema-Entscheidung, offene Rückfrage 3).

## Technischer Lösungsentwurf

### A) Kommentarfunktion

- `WarehouseRequisition`: neues Feld `Comment` (`string?`, `[StringLength(1000)]` — Vorschlag,
  analog `WarehouseRequisitionItem.Note`/`NoteEinkauf` mit 500, hier bewusst größer da Kopf-Ebene
  einen längeren Freitext rechtfertigen kann; Wert im Review zu bestätigen).
- `IWarehouseRequisitionRepository`: neue Methode `SaveCommentAsync(int id, string? comment,
  string user, string winUser)` analog `SaveNotesAsync` (Zeilen 38-44) — RowVersion bewusst
  ignoriert (Kommentar ist nicht konfliktrelevant, gleiches Muster wie Notizen).
- `WarehouseRequisitionsController`/`Api/WarehouseRequisitionsApiController`: neuer
  Speicherpfad, z. B. `PUT /api/warehouserequisitions/{id}/comment` (analog dem bestehenden
  Item-`PUT`-Muster), mit demselben Ownership+Draft-Guard wie `CheckOwnershipAndDraft` **falls**
  die Klärung aus offener Rückfrage 1 „nur im Draft editierbar" ergibt; andernfalls ein
  eigener, lockererer Guard.
- `WarehouseRequisitionEditViewModel` + `Edit.cshtml`: `Comment`-Feld als `<textarea>` unterhalb
  der Artikelliste, mit Autosave-Pattern analog den bestehenden Notiz-Feldern in
  `WarehousePicking/Details.cshtml` (Fallstrick „Notiz-Autosave vor Drucken" beachten, falls ein
  ähnlicher Druck-Pfad betroffen ist — hier nicht der Fall, da `Edit.cshtml` keinen Druck hat).
- `WarehouseRequisitionListItemViewModel`: `Comment`-Property ergänzen (record — an **beiden**
  Konstruktions-Stellen nachziehen: `WarehouseRequisitionsController.Index:76-83` und
  `WarehousePickingController.Index:73-75`).
- `WarehousePicking/Index.cshtml`: neue Spalte „Kommentar" (mit `data-col-key="comment"` gemäß
  Listen-View-Pattern, da diese View bereits `data-server-column-filter="true"` nutzt —
  Server-Spaltenfilter-Pflicht laut ADR 0005 gilt auch für die neue Spalte).
- `WarehouseRequisitionDetailViewModel` + `WarehousePicking/Details.cshtml`: `Comment` ebenfalls
  read-only anzeigen.

### B) Dummy-Artikel

- `Edit.cshtml`-Suchskript: liefert `/api/articles/search` 0 Treffer, erscheint statt/zusätzlich
  zur leeren Ergebnisliste ein Button/Formular „Artikel nicht gefunden — Dummy anlegen" mit
  einem Bezeichnungsfeld.
- Neuer API-Endpunkt (z. B. `POST /api/warehouserequisitions/{id}/items/dummy` mit Body
  `{ ArticleNumber, Description, Quantity }`), der:
  1. prüft, ob `ArticleNumber` bereits existiert (`GetByArticleNumberAsync`) — falls ja, normalen
     `AddItem`-Pfad nutzen (kein Duplikat anlegen);
  2. andernfalls einen neuen `Article` mit `ArticleNumber` (Herkunft: siehe offene Rückfrage 3)
     und `Description` = eingegebene Bezeichnung anlegt (`Unit`/`ArticleGroup` leer/null, da
     unbekannt);
  3. den neuen Artikel anschließend über den bestehenden `AddItemAsync`-Pfad der Bestellung
     hinzufügt (inkl. der bestehenden Glas/Lager-Artikelgruppen-Prüfung
     `GlasArticleGroupFilter.IsAllowedForType` — ein Dummy-Artikel ohne `ArticleGroup` muss diese
     Prüfung bestehen können, sonst blockiert er sich selbst; ggf. muss
     `IsAllowedForType` für `ArticleGroup == null` einen definierten, erlaubenden Fall haben —
     im Review gegen die bestehende Implementierung zu prüfen).
- Falls die Klärung aus offener Rückfrage 5 ein Unterscheidungs-Flag ergibt: `Article` bekommt
  ein neues Feld `IsManuallyCreated` (`bool`, Default `false`), das bei der Dummy-Anlage `true`
  gesetzt wird — sichtbar/filterbar in der Artikelliste (`ArticlesController`/`Views/Articles/`)
  für eine spätere masterdata-Bereinigung. **Nur falls die offene Rückfrage das verlangt** —
  sonst entfällt dieser Punkt und Migration B reduziert sich auf keine Schema-Änderung.

## Migrations-/SQL-Auswirkungen

Zwei potenziell unabhängige Schema-Änderungen (je nach Klärung der offenen Rückfragen):

1. `WarehouseRequisition.Comment` (neue Spalte, `NVARCHAR(1000) NULL`) — additiv, kein Backfill
   nötig. Migration + `SQL/<naechste freie Nummer>_AddWarehouseRequisitionComment.sql` mit
   `OBJECT_ID`-Guard, DDL in eigenem Batch, `__EFMigrationsHistory`-Insert in separatem Batch,
   `SQL/00_FreshInstall.sql` an beiden Stellen (Schema + `MigrationId`) — nach ADR 0004.
2. **Nur falls** offene Rückfrage 5 ein `Article.IsManuallyCreated`-Flag verlangt: weitere,
   unabhängige additive Migration (`BIT NOT NULL DEFAULT 0`).

Zum Zeitpunkt dieser Spec ist main bei `SQL/83_AddSageBookingQueue.sql` — die konkrete
Skript-Nummer ist zum Umsetzungszeitpunkt neu zu ermitteln (nächste freie Nummer), da mehrere
Teil-Specs dieses Backlogs parallel um SQL-Nummern konkurrieren können.

## Audit-Feld-Auswirkungen

- `WarehouseRequisition` ist bereits `AuditableEntity` — jede Kommentar-Änderung setzt
  `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` wie bei den bestehenden `SaveNotesAsync`/
  `SaveProgressAsync`-Pfaden.
- Neu angelegte Dummy-`Article`-Zeilen setzen `CreatedAt`/`CreatedBy`/`CreatedByWindows` aus
  `ICurrentUserService` wie jede reguläre Artikel-Anlage.

## Rollen- und Zugriffsfilter-Auswirkungen

- **Kommentarfunktion:** keine neue Rolle — Kommentar-Schreibpfad liegt unter denselben Filtern
  wie die bestehenden Item-Endpunkte (`[RequirePickingOrStockOrLagerbestellungAccess]` +
  `[RequireLagerbestellungAktiv]`).
- **Dummy-Artikel:** **wesentliche offene Frage** (siehe offene Rückfrage 4) — die Artikel-Neuanlage
  ist heute exklusiv `masterdata`/`admin` vorbehalten
  (`[RequireMasterDataAccess]`, `secondbrain/codebase/controller.md`). Ein neuer Endpunkt, der
  Artikel-Anlage aus `WarehouseRequisitionsApiController` heraus erlaubt, wäre ein bewusster,
  eng abgegrenzter Ausnahmefall zu diesem Rollenkonzept (ADR 0006) und muss **explizit** vom
  Menschen freigegeben werden, bevor er umgesetzt wird — hier **nicht** vorentschieden.

## Listen-View-Pattern-Pflichten

`WarehousePicking/Index.cshtml` ist bereits eine Server-Mode-Listen-View
(`data-server-column-filter="true"`) — die neue Kommentar-Spalte braucht laut ADR 0005 zwingend
einen `data-col-key="comment"` und einen passenden Eintrag im `ColumnMap` von
`WarehousePickingController.cs:39-54` (und, falls die Kommentar-Spalte auch in
`WarehouseRequisitionsController.Index` erscheinen soll, dort ebenfalls im dortigen `ColumnMap`,
Zeilen 35-51).

## Akzeptanzkriterien

1. Auf dem Bestellformular (`Edit.cshtml`) kann die Werkbank einen Kommentar eingeben und
   speichern.
2. Der gespeicherte Kommentar erscheint in der Liste der „Lager Eingehenden Listen"
   (`/WarehousePicking`) und in der Detailansicht.
3. Eine Bestellung ohne Kommentar zeigt eine leere Zelle/kein Symbol (kein Fehler).
4. Wird beim Artikel-Hinzufügen eine EK-Nummer eingegeben, die keine Treffer liefert, kann der
   Anwender eine Bezeichnung eingeben und einen neuen Artikel anlegen lassen.
5. Der neu angelegte Dummy-Artikel erscheint sofort als Position in der aktuellen Bestellung mit
   der eingegebenen Bezeichnung.
6. Der neu angelegte Dummy-Artikel ist danach auch in der regulären Artikelsuche
   (`/api/articles/search`) auffindbar.
7. Wird dieselbe (Dummy-)EK-Nummer ein zweites Mal in einer anderen Bestellung verwendet, wird
   **kein** zweiter Artikel-Datensatz angelegt (Wiederverwendung über `GetByArticleNumberAsync`),
   sondern der bestehende referenziert.

## Test-Szenarien

Ergänzung `docs/TESTSZENARIEN.md` Kapitel 46/52 (Glas-Bestellung / Lagerbestellung aus der
Stückliste, oder ein neues Kapitel — im Review zu entscheiden):

- **Kommentar — Vorbedingung:** Draft-Bestellung existiert.
  **Schritte:** Kommentar eintragen, speichern, zur Lager-Ansicht wechseln.
  **Erwartetes Verhalten:** Kommentar sichtbar in Eingehenden Listen + Details.
  **Negativfall:** Kommentar leer lassen → keine Anzeige-Fehler, leere Zelle.
- **Dummy-Artikel — Vorbedingung:** EK-Nummer, die in Sage/App nicht existiert.
  **Schritte:** Artikelsuche mit dieser Nummer, „nicht gefunden" bestätigen, Bezeichnung
  eingeben, anlegen.
  **Erwartetes Verhalten:** Neue Position mit eingegebener Bezeichnung erscheint in der
  Bestellung; Artikel ist danach über die normale Suche auffindbar.
  **Negativfall:** dieselbe EK-Nummer erneut in einer zweiten Bestellung verwenden → kein
  Duplikat, bestehender Artikel wird referenziert.

`secondbrain/tests/testszenarien-index.md` entsprechend ergänzen.

## Deploy

- **Web-App:** ja.
- **Service:** nein (Artikel-Sync im Service bleibt unverändert, betrifft aber die Selbstheilung
  von Dummy-Artikeln bei gleichnamiger `ArticleNumber` — siehe Ist-Zustand).
- **Migration:** ja (mindestens `WarehouseRequisition.Comment`; ggf. zusätzlich
  `Article.IsManuallyCreated`, siehe offene Rückfrage 5).
- **Publish-Befehle:**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

**Hinweis:** DB-Migration vor dem Web-Publish einspielen (additive Spalten, kein
Backup-Zwang über das ohnehin geltende Standard-Vorgehen hinaus, da rein additiv).

## Offene Rückfragen

1. Kommentarfeld: Soll der Kommentar (a) NUR vom Ersteller im Draft-Status editierbar sein (wie
   die Positions-Notiz „Note"), oder (b) während des gesamten Lebenszyklus (auch nach Submit)
   editierbar bleiben — z. B. damit der Lagermitarbeiter in den Eingehenden Listen selbst einen
   Kommentar ergänzen kann? Bestimmt, ob ein zweites Feld (Kommentar Werkbank vs. Kommentar
   Lager) nötig ist, analog `Note`/`NoteEinkauf`.
2. Soll der Kommentar auch in die Submit-Benachrichtigungs-Mail (`NotificationWorker`)
   aufgenommen werden, oder ist die Sichtbarkeit ausschließlich auf UI (Edit-Formular + Lager
   Eingehende Listen) beschränkt, wie im Backlog wörtlich verlangt?
3. Dummy-Artikel — zentrale Namensfrage: Soll die eingetippte, in Sage nicht gefundene EK-Nummer
   selbst als `ArticleNumber` des neu angelegten Artikels verwendet werden (Artikel wird später
   beim nächsten Sage-Sync automatisch mit echten Sage-Daten angereichert/upgedatet), ODER soll
   `ArticleNumber` buchstäblich/literal auf „DUMMY" (bzw. ein generiertes DUMMY-Präfix-Schema
   wegen der UNIQUE-Constraint auf `ArticleNumber`) gesetzt werden? Das Backlog ist hier wörtlich
   mehrdeutig („Der Artikel soll ... automatisch angelegt werden. „DUMMY".").
4. Rollen/Zugriff: Artikel-Neuanlage ist heute exklusiv `[RequireMasterDataAccess]` vorbehalten.
   Soll die Dummy-Artikel-Anlage aus der Lager-/Glasbestellung heraus genau das umgehen (jeder
   mit `lagerbestellung`/`glasbestellung`/`stock`/`picking`-Rolle darf einen Dummy-Artikel
   anlegen), oder soll stattdessen ein Freigabe-/Review-Schritt durch `masterdata`
   dazwischengeschaltet werden (z. B. Bestellposition hängt in einem Pending-Status, bis
   `masterdata` den Artikel bestätigt)?
5. Soll ein Dummy-Artikel dauerhaft von einem regulär per Sage synchronisierten Artikel
   unterscheidbar bleiben (neues Bool-Flag z. B. `Article.IsManuallyCreated`, sichtbar/filterbar
   in der Artikelliste für eine spätere Bereinigung durch `masterdata`), oder ist das bewusst
   nicht gewünscht?

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →
2. →
3. →
4. →
5. →
