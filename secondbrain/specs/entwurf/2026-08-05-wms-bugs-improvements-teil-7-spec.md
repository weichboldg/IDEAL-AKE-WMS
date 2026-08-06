---
type: spec
title: "Lager-/Glasbestellung: Kommentarfunktion (Kopf-Ebene) + Dummy-Artikel bei unbekannter EK-Nummer"
slug: 2026-08-05-wms-bugs-improvements-teil-7-spec
status: Testbereit
created: 2026-08-05
updated: 2026-08-06
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: ""
task: "[[2026-08-05-deploy-wms-bugs-teil-4-8]]"
worktree: ".claude/worktrees/2026-08-05-wms-bugs-improvements-4-8"
branch: "feature/2026-08-05-wms-bugs-improvements-4-8"
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
  - IdealAkeWms/Controllers/ArticlesApiController.cs
  - IdealAkeWms/Services/GlasArticleGroupFilter.cs
  - IdealAkeWms/Views/WarehouseRequisitions/Edit.cshtml
  - IdealAkeWms/Views/WarehousePicking/Index.cshtml
  - IdealAkeWms/Views/WarehousePicking/Details.cshtml
  - IDEALAKEWMSService/Services/WarehouseRequisitionEmailService.cs
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - SQL/85_AddWarehouseRequisitionComment.sql (Schema; QA-Nachweis: reale Nummer 85, da Teil 8
    Nummer 84 belegt hat)
  - SQL/86_SeedDummyArticle.sql (reiner Daten-Seed; reale Nummer 86)
  - SQL/00_FreshInstall.sql
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Zwei unabhängige, aber im selben Modul (Lager-/Glasbestellung) liegende Verbesserungen:

1. **Kommentarfunktion:** Die Werkbank soll eine Bestellung mit einem freien Kommentar (Kopf-Ebene)
   versehen können, der auch dem Lager in den „Eingehenden Listen" **und** in der Submit-E-Mail
   sichtbar ist — heute gibt es nur die Positions-Notizen `Note`/`NoteEinkauf` je Zeile, aber keinen
   Kommentar auf Ebene der gesamten Bestellung.
2. **Dummy-Artikel:** Wird beim Hinzufügen einer Position die eingegebene EK-Nummer nicht gefunden
   (Artikel existiert noch nicht in der App/in Sage), soll die Werkbank trotzdem eine Position mit
   einer frei eingegebenen Bezeichnung anlegen können, statt blockiert zu sein. Dafür existiert
   **genau ein** vorab (per SQL-Seed bei der Installation) angelegter **DUMMY-Artikel**; die
   Werkbank wählt diesen und **muss** je Position eine eigene, aussagekräftige Bezeichnung vergeben.
   Es wird **kein** Artikel automatisch neu angelegt (Freigabe-Antwort 4).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neues Kommentarfeld auf `WarehouseRequisition` (Kopf-Ebene, nicht Item-Ebene).
- Anzeige/Editiermöglichkeit im Edit-Formular der Werkbank (`Views/WarehouseRequisitions/Edit.cshtml`),
  **nur vom Besteller/Ersteller im Draft-Status** (Freigabe-Antwort 1).
- Anzeige (read-only) in den „Lager Eingehenden Listen" (`Views/WarehousePicking/Index.cshtml`, neue
  Spalte) und in der Detailansicht (`Views/WarehousePicking/Details.cshtml`).
- Aufnahme des Kommentars in den **Submit-E-Mail-Body** (HTML **und** Text) im Windows-Service
  (`WarehouseRequisitionEmailService`, Freigabe-Antwort 2).
- Nutzung des einen, per SQL geseedeten **DUMMY-Artikels** in der Lager- **und** der
  Glasbestellung: „nicht gefunden"-UX im Edit-Formular, **Pflicht-Bezeichnung je Position**
  (Positions-Snapshot `WarehouseRequisitionItem.ArticleDescription`), Umgehung des
  Duplikat-Guards und der Glas-Artikelgruppen-Prüfung für den DUMMY-Schlüssel.

**Out-of-Scope**
- Keine Änderung an den bestehenden Positions-Notizen `Note`/`NoteEinkauf`.
- **Kein** automatisches Anlegen neuer `Article`-Datensätze aus der Bestellung heraus
  (Freigabe-Antwort 4 hat den ursprünglichen Auto-Create-Entwurf verworfen). Es existiert genau
  **ein** geseedeter DUMMY-Artikel.
- **Keine** eigene Markierung/kein Flag am DUMMY-Artikel (`Article.IsManuallyCreated` o. Ä.
  entfällt) — er ist ein normaler WMS-Artikel (Freigabe-Antwort 5).
- **Keine** Änderung der geteilten `Article.Description` bei DUMMY-Nutzung — die individuelle
  Bezeichnung lebt ausschließlich auf der Bestellposition.
- Der Kommentar wird **nicht** in die Storno-E-Mail aufgenommen (Freigabe-Antwort 2 nennt nur die
  Submit-Mail).
- Kein Multi-DUMMY / kein DUMMY-Präfix-Schema — genau ein reservierter Schlüssel `DUMMY`.

## Fachliche Anforderungen

### A) Kommentarfunktion

1. `WarehouseRequisition` bekommt ein neues Feld `Comment` (Freitext, `string?`, begrenzte Länge).
2. Im Edit-Formular der Werkbank ist der Kommentar unten (nach der Artikelliste) sichtbar und
   editierbar — **nur vom Besteller/Ersteller und nur im Draft-Status** (Freigabe-Antwort 1;
   bestehendes `CheckOwnershipAndDraft`-Muster). Ein zweites Feld „Kommentar Lager" entfällt.
3. In den „Lager Eingehenden Listen" (`/WarehousePicking`, `WarehousePickingController.Index`) ist
   der Kommentar als eigene Spalte sichtbar (read-only).
4. In der Detailansicht (`/WarehousePicking/Details/{id}`) ist der Kommentar ebenfalls read-only
   sichtbar.
5. Der Kommentar erscheint im **Submit-E-Mail-Body** (Freigabe-Antwort 2), im HTML-Body
   (`BuildSubmitBody`) **und** im Text-Body (`BuildSubmitText`), im Kopf der Mail (nach „Erfasser"/
   „Submit", vor der Positionstabelle), nur wenn befüllt. Die **Storno-Mail** bleibt unverändert.

### B) Dummy-Artikel

1. Es existiert **genau ein** DUMMY-Artikel mit dem reservierten `ArticleNumber`-Schlüssel `DUMMY`
   (außerhalb des Sage-Namensraums, damit der Artikel-Sync ihn nicht überschreibt/löscht). Dieser
   Artikel wird **einmalig per SQL-Seed** bei der Installation angelegt, **nicht** zur Laufzeit aus
   der Bestellung heraus (Freigabe-Antwort 4). Es gibt **keine** Artikel-Neuanlage im Bestellfluss.
2. Findet die Artikelsuche beim Hinzufügen einer Position (`article-search` in `Edit.cshtml`) keinen
   Treffer für die eingegebene EK-Nummer, wird dem Anwender die Möglichkeit angeboten, eine
   **DUMMY-Position** anzulegen und dabei eine **Bezeichnung als Pflichtfeld** einzugeben.
3. Die eingegebene Bezeichnung landet ausschließlich auf der **Bestellposition**
   (`WarehouseRequisitionItem.ArticleDescription`), **nicht** auf der geteilten `Article.Description`
   (Freigabe-Antwort 3 „Bezeichnung zwingend geändert" → Positions-Snapshot). Damit sehen andere
   Bestellungen denselben DUMMY-Artikel mit ihrer jeweils eigenen Bezeichnung.
4. Server-Validierung: Eine DUMMY-Position wird **abgelehnt**, wenn die Bezeichnung leer ist ODER
   unverändert der Default-Seed-Bezeichnung des DUMMY-Artikels entspricht („zwingend geändert").
5. Mehrere DUMMY-Positionen mit **unterschiedlichen** Bezeichnungen sind in **einer** Bestellung
   erlaubt (Duplikat-Guard wird für den DUMMY-Schlüssel umgangen).
6. Der DUMMY-Artikel ist in der **Lager- UND der Glasbestellung** wählbar (die
   Glas-Artikelgruppen-Prüfung wird für den DUMMY-Schlüssel als erlaubt behandelt).

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
- **Submit-Mail (Windows-Service):** `IDEALAKEWMSService/Services/WarehouseRequisitionEmailService.cs`
  — `BuildSubmitBody:146-167` (HTML) und `BuildSubmitText:189-209` (Text) erzeugen den Mail-Kopf
  (`Werkbank`/`Erfasser`/`Submit`) und die Positionstabelle. Die geladene `WarehouseRequisition`
  wird über `GetPendingSubmitEmailsAsync` (`WarehouseRequisitionRepository.cs:90-100`) ohnehin als
  Entität geladen; ein skalarer `Comment` ist ohne Query-Änderung verfügbar. `CheckOwnershipAndDraft`
  liegt in `WarehouseRequisitionsApiController.cs:49-60`.

**Dummy-Artikel:**
- `IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs:62-99` (`AddItem`): bricht bei
  unbekannter Artikelnummer hart ab (`return BadRequest(new { error = "Artikel nicht gefunden." })`,
  Zeile 66-67). Der DUMMY-Fall braucht einen eigenen Pfad, der den geseedeten DUMMY-Artikel
  referenziert (kein Abbruch), mit Pflicht-Bezeichnung.
- `AddItem` kopiert heute **stur** `article.Description ?? ""` in die Position
  (`WarehouseRequisitionsApiController.cs:90`). Es gibt **keinen** Weg, eine abweichende Bezeichnung
  je Position mitzugeben. Die Repo-Methode `AddItemAsync` **akzeptiert** dagegen bereits einen
  `description`-Parameter (`WarehouseRequisitionRepository.cs:114-115`) und schreibt ihn in
  `ArticleDescription` (Zeile 135) — der Override-Hebel existiert also im Repo, nur nicht im
  Controller.
- **Duplikat-Guard:** `AddItemAsync` lehnt eine zweite Position mit derselben `ArticleNumber` in
  einer Bestellung ab (`WarehouseRequisitionRepository.cs:117-124`, „Artikel '…' ist bereits in
  dieser Bestellung enthalten."). Für den einen geteilten DUMMY-Schlüssel muss diese Sperre umgangen
  werden, sonst ist nur **eine** unbekannte Position je Bestellung erfassbar.
- **Positions-Bezeichnung:** `WarehouseRequisitionItem.ArticleDescription`
  (`WarehouseRequisitionItem.cs:14-15`, `[Required][StringLength(500)]`) existiert bereits als
  Snapshot je Position — der korrekte, nicht-geteilte Ablageort für die DUMMY-Bezeichnung.
- **Glas-Filter:** `GlasArticleGroupFilter.IsAllowedForType(null, …)`
  (`IdealAkeWms/Services/GlasArticleGroupFilter.cs:35-43`) liefert für `Lager` → `true`, für
  `Glas` → **`false`** (`glasGroups.Contains("")` ist false). Ein DUMMY ohne Artikelgruppe ist damit
  in der Glas-gescopten Suche (`ArticlesApiController.cs:35-37`) unsichtbar **und** wird im Add-Guard
  (`WarehouseRequisitionsApiController.cs:80`) abgelehnt. Der DUMMY-Schlüssel muss daher explizit als
  „für beide Typen erlaubt" behandelt werden.
- `IdealAkeWms/Models/Article.cs:1-41`: `ArticleNumber` ist `[Required][StringLength(100)]` und in
  `ApplicationDbContext.cs:220` `HasIndex(e => e.ArticleNumber).IsUnique()` — der Seed-Schlüssel
  `DUMMY` muss diese Eindeutigkeit respektieren (einmaliger Insert, idempotenter `IF NOT EXISTS`-Guard).
- `IDEALAKEWMSService/Services/SageImportService.cs:481-514` (`SyncArticlesAsync`) upserted Artikel
  **anhand von `ArticleNumber`** aus Sage und hat **keinen** Delete-/Prune-Lauf — ein geseedeter
  DUMMY wird nie gelöscht (gut). Solange `DUMMY` nachweislich **nicht** als echte Sage-Nummer
  existiert, wird der Seed vom Sync auch nie überschrieben.

## Technischer Lösungsentwurf

### A) Kommentarfunktion

- `WarehouseRequisition`: neues Feld `Comment` (`string?`, `[StringLength(1000)]` — Vorschlag,
  analog `WarehouseRequisitionItem.Note`/`NoteEinkauf` mit 500, hier bewusst größer da Kopf-Ebene
  einen längeren Freitext rechtfertigen kann; Wert im Review zu bestätigen).
- `IWarehouseRequisitionRepository`: neue Methode `SaveCommentAsync(int id, string? comment,
  string user, string winUser)` analog `SaveNotesAsync` (Zeilen 38-44) — RowVersion bewusst
  ignoriert (Kommentar ist nicht konfliktrelevant, gleiches Muster wie Notizen).
- `WarehouseRequisitionsController`/`Api/WarehouseRequisitionsApiController`: neuer
  Speicherpfad `PUT /api/warehouserequisitions/{id}/comment` (analog dem bestehenden Item-`PUT`-
  Muster), mit demselben `CheckOwnershipAndDraft`-Guard wie die Item-Endpunkte
  (`WarehouseRequisitionsApiController.cs:49-60`) — Ersteller-Ownership **und** Draft-Status
  (Freigabe-Antwort 1: „nur vom Besteller/Eingeber"; Empfehlung SOLLTE 7: nur im Draft editierbar).
- `WarehouseRequisitionEditViewModel` + `Edit.cshtml`: `Comment`-Feld als `<textarea>` unterhalb
  der Artikelliste, mit Autosave-Pattern analog den bestehenden Notiz-Feldern. Kein Druck-Pfad in
  `Edit.cshtml` betroffen.
- `WarehouseRequisitionListItemViewModel`: `Comment`-Property ergänzen (record — an **beiden**
  Konstruktions-Stellen nachziehen: `WarehouseRequisitionsController.Index:76-83` und
  `WarehousePickingController.Index:73-75`).
- `WarehousePicking/Index.cshtml`: neue Spalte „Kommentar" (mit `data-col-key="comment"` gemäß
  Listen-View-Pattern, da diese View bereits `data-server-column-filter="true"` nutzt —
  Server-Spaltenfilter-Pflicht laut ADR 0005 gilt auch für die neue Spalte; Eintrag im `ColumnMap`
  von `WarehousePickingController.cs:39-54`, und falls die Spalte auch in
  `WarehouseRequisitionsController.Index` erscheinen soll, dort im `ColumnMap` Zeilen 35-51).
- `WarehouseRequisitionDetailViewModel` + `WarehousePicking/Details.cshtml`: `Comment` ebenfalls
  read-only anzeigen.
- **Submit-Mail (Windows-Service, `deploy.service=true`):** In
  `WarehouseRequisitionEmailService.BuildSubmitBody` (HTML, Zeile 146-167) und `BuildSubmitText`
  (Text, Zeile 189-209) den Kommentar im Mail-Kopf ausgeben, jeweils nur wenn
  `!string.IsNullOrWhiteSpace(r.Comment)`. HTML sauber escapen (`E(...)` wie die übrigen Felder).
  `BuildCancellationBody`/`BuildCancellationText` bleiben **unverändert** (kein Kommentar in der
  Storno-Mail).

### B) Dummy-Artikel

**Grundprinzip:** Kein Code legt Artikel an. Es gibt **genau einen**, per SQL geseedeten
DUMMY-Artikel mit dem reservierten `ArticleNumber` = `DUMMY`. Die Bezeichnung je Nutzung lebt auf
der Position, nicht am Artikel.

- **Reservierter Schlüssel:** Eine zentrale Konstante (Single Source of Truth), z. B.
  `Article.DummyArticleNumber = "DUMMY"`, damit Seed, Add-Guard, Duplikat-Guard und Glas-Filter
  denselben Wert referenzieren. Die Default-Seed-Bezeichnung (z. B.
  `"DUMMY – Bezeichnung bitte eintragen"`) ebenfalls als Konstante, damit die „zwingend geändert"-
  Validierung serverseitig dagegen vergleichen kann.
- `Edit.cshtml`-Suchskript: liefert `/api/articles/search` 0 Treffer, erscheint statt/zusätzlich
  zur leeren Ergebnisliste ein Button/Formular „Artikel nicht gefunden — DUMMY-Position anlegen"
  mit einem **Pflicht-Bezeichnungsfeld**. Der DUMMY wird **nicht** in die reguläre Suche
  eingemischt (kein Rauschen), sondern nur über diesen „nicht gefunden"-Zweig angeboten.
- **Neuer API-Endpunkt** `POST /api/warehouserequisitions/{id}/items/dummy` mit Body
  `{ Description, Quantity }`, der:
  1. denselben `CheckOwnershipAndDraft`-Guard durchläuft;
  2. den geseedeten DUMMY-Artikel per `GetByArticleNumberAsync(Article.DummyArticleNumber)` lädt
     (existiert er nicht → `BadRequest`, Hinweis „DUMMY-Artikel fehlt, Seed nicht eingespielt");
  3. **Bezeichnung validiert:** `Description` darf nicht leer sein und **nicht** gleich der
     Default-Seed-Bezeichnung (Trim/Case-insensitiver Vergleich) — sonst `BadRequest`
     („Bitte eine eigene Bezeichnung eingeben.");
  4. **Glas/Lager-Guard übersprungen** für den DUMMY-Schlüssel (der DUMMY ist in beiden Typen
     erlaubt) — siehe Glas-Filter-Punkt unten;
  5. die Position über `AddItemAsync(id, "DUMMY", description: <eingegeben>, unit: null, quantity,
     …)` hinzufügt — der bereits vorhandene `description`-Parameter (Repo Zeile 114/135) trägt die
     Positions-Bezeichnung.
- **Duplikat-Guard umgehen:** `AddItemAsync` (`WarehouseRequisitionRepository.cs:117-124`) darf für
  `articleNumber == Article.DummyArticleNumber` die „bereits enthalten"-Prüfung **überspringen**, so
  dass mehrere DUMMY-Positionen mit unterschiedlichen Bezeichnungen in einer Bestellung möglich sind.
  Umsetzung: Guard-Bedingung um `&& articleNumber != Article.DummyArticleNumber` erweitern (die
  Signatur bleibt, kein neuer Parameter nötig).
- **Glas/Lager wählbar:** `GlasArticleGroupFilter.IsAllowedForType`
  (`GlasArticleGroupFilter.cs:35-43`) so ergänzen, dass der DUMMY in **beiden** Typen erlaubt ist.
  **Empfohlene Umsetzung:** Der neue DUMMY-Endpunkt umgeht den Gruppen-Guard ganz (der DUMMY ist
  per Definition typ-neutral) — dann ist keine Änderung an `IsAllowedForType` nötig. Alternative
  (falls der DUMMY auch über die reguläre Suche wählbar sein soll): `IsAllowedForType` für den
  reservierten Schlüssel bzw. leere Gruppe explizit als „immer erlaubt" sonderregeln und die
  Glas-Suche (`ArticlesApiController.cs:35-37`) entsprechend durchlassen. Empfehlung: erste Variante
  (Guard nur im DUMMY-Endpunkt umgehen), da minimalinvasiv und deterministisch.
- **Keine** `Article.IsManuallyCreated`-Migration, **kein** Flag (Freigabe-Antwort 5) — der DUMMY
  ist ein normaler WMS-Artikel.

## Migrations-/SQL-Auswirkungen

Genau **eine** Schema-Änderung plus **ein** reiner Daten-Seed — beide nach ADR 0004-Disziplin,
aber unterschiedlicher Natur:

1. **Schema — `WarehouseRequisition.Comment`** (neue Spalte, `NVARCHAR(1000) NULL`): additiv, kein
   Backfill. Model → `dotnet ef migrations add AddWarehouseRequisitionComment` → idempotentes
   `SQL/84_AddWarehouseRequisitionComment.sql` mit `OBJECT_ID`/Spalten-Guard (DDL in eigenem Batch),
   `__EFMigrationsHistory`-Insert in **separatem** Batch → `SQL/00_FreshInstall.sql` an **beiden**
   Stellen (Schema-Objekt + `MigrationId`).
2. **Daten-Seed — DUMMY-Artikel** (Freigabe-Antwort 4, „einmalig per SQL-Insert bei Installation"):
   reiner Insert **einer** `Articles`-Zeile mit `ArticleNumber = 'DUMMY'`, Default-Bezeichnung,
   `ArticleGroup`/`Unit` leer, Audit-Felder gesetzt (`CreatedBy = 'System-Seed'`). **Kein** Schema,
   **kein** EF-Migration-/`__EFMigrationsHistory`-Eintrag. Umsetzung:
   - neues idempotentes `SQL/85_SeedDummyArticle.sql` mit `IF NOT EXISTS (SELECT 1 FROM [Articles]
     WHERE [ArticleNumber] = 'DUMMY')`-Guard;
   - Verankerung im **Seed-Teil** von `SQL/00_FreshInstall.sql` (Muster vorhanden, z. B. FA-Vorbau-
     Seeds, AppSettings, Roles), ebenfalls mit `IF NOT EXISTS`-Guard.
   - Der Seed-Schlüssel `DUMMY` muss im Review als **in Sage nicht vorhanden** bestätigt werden, damit
     `SyncArticlesAsync` ihn nie überschreibt (HINWEIS 8).

Zum Zeitpunkt dieser Spec ist main bei `SQL/83_AddSageBookingQueue.sql`; die konkreten Skript-Nummern
(84 Schema / 85 Seed) sind zum Umsetzungszeitpunkt als nächste freie Nummern zu bestätigen, da mehrere
Teil-Specs dieses Backlogs parallel um SQL-Nummern konkurrieren können.

**Reihenfolge Deploy:** DB-Migration `84` **und** Seed `85` vor dem Web-/Service-Publish einspielen
(der DUMMY-Endpunkt setzt den geseedeten Artikel voraus).

**QA-Bestätigung der realen Nummern (2026-08-06):** Teil 8 hat bei der Umsetzung `84` belegt
(`AddUserDefaultFilterFaWorklistDescription1`); dieser Teil bekam daher `85` (Schema) und `86`
(Seed) — siehe „## Deploy" und „## QA-Nachweis" unten für die verifizierten Dateinamen.

## Audit-Feld-Auswirkungen

- `WarehouseRequisition` ist bereits `AuditableEntity` — jede Kommentar-Änderung setzt
  `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` wie bei den bestehenden `SaveNotesAsync`/
  `SaveProgressAsync`-Pfaden.
- Das Hinzufügen einer DUMMY-Position läuft über den bestehenden `AddItemAsync`-Pfad und setzt die
  Audit-Felder der Position (`CreatedAt`/`CreatedBy`/`CreatedByWindows`) sowie `ModifiedAt/By` auf
  der Bestellung wie jede reguläre Position — **keine** neue Artikel-Zeile, daher keine
  Artikel-Audit-Felder zur Laufzeit. Die einzige DUMMY-`Article`-Zeile trägt ihre Audit-Werte aus
  dem SQL-Seed (`CreatedBy = 'System-Seed'`).

## Rollen- und Zugriffsfilter-Auswirkungen

- **Kommentarfunktion:** keine neue Rolle — Kommentar-Schreibpfad liegt unter denselben Filtern
  wie die bestehenden Item-Endpunkte (`[RequirePickingOrStockOrLagerbestellungAccess]` +
  `[RequireLagerbestellungAktiv]`), zusätzlich Ownership+Draft via `CheckOwnershipAndDraft`.
- **Dummy-Artikel:** **kein Rollen-Konflikt mehr.** Es gibt **keine** Artikel-Neuanlage aus der
  Bestellung (Freigabe-Antwort 4) — der bisher problematische Bypass von `[RequireMasterDataAccess]`
  entfällt vollständig. Der neue DUMMY-Endpunkt legt nur eine **Bestellposition** an (referenziert
  den geseedeten Artikel) und liegt unter denselben Filtern wie die übrigen Item-Endpunkte
  (`[RequirePickingOrStockOrLagerbestellungAccess]` + Ownership+Draft). Die Artikel-Neuanlage
  bleibt weiterhin ausschließlich `masterdata`/`admin` vorbehalten. **Keine** Änderung an
  `RoleOverview.cshtml`/`controller.md` nötig.

## Listen-View-Pattern-Pflichten

`WarehousePicking/Index.cshtml` ist bereits eine Server-Mode-Listen-View
(`data-server-column-filter="true"`) — die neue Kommentar-Spalte braucht laut ADR 0005 zwingend
einen `data-col-key="comment"` und einen passenden Eintrag im `ColumnMap` von
`WarehousePickingController.cs:39-54` (und, falls die Kommentar-Spalte auch in
`WarehouseRequisitionsController.Index` erscheinen soll, dort ebenfalls im dortigen `ColumnMap`,
Zeilen 35-51).

## Akzeptanzkriterien

**A) Kommentar**

1. Auf dem Bestellformular (`Edit.cshtml`) kann der **Besteller/Ersteller** im **Draft**-Status
   einen Kommentar eingeben und speichern; ein anderer Benutzer bzw. eine Bestellung nach Submit
   kann ihn **nicht** ändern (`CheckOwnershipAndDraft` → Forbid/BadRequest).
2. Der gespeicherte Kommentar erscheint read-only in der Liste der „Lager Eingehenden Listen"
   (`/WarehousePicking`, eigene Spalte) und in der Detailansicht.
3. Eine Bestellung ohne Kommentar zeigt eine leere Zelle/kein Symbol (kein Fehler).
4. Nach Submit enthält die **Submit-E-Mail** (HTML **und** Text) den Kommentar im Kopf, sofern
   befüllt; eine leere/fehlende Angabe erzeugt keine leere Zeile. Die **Storno-Mail** enthält
   **keinen** Kommentar.

**B) DUMMY-Artikel**

5. Der DUMMY-Artikel (`ArticleNumber = 'DUMMY'`) existiert nach Installation/Seed **genau einmal**;
   der Seed ist idempotent (zweiter Lauf legt keinen zweiten an).
6. Wird beim Artikel-Hinzufügen eine EK-Nummer eingegeben, die **keine** Treffer liefert, kann der
   Anwender eine DUMMY-Position anlegen und **muss** dabei eine Bezeichnung eingeben.
7. Die DUMMY-Position erscheint sofort in der aktuellen Bestellung mit der **eingegebenen**
   Bezeichnung (Positions-`ArticleDescription`); die geteilte `Article.Description` des DUMMY bleibt
   unverändert.
8. Eine DUMMY-Position mit **leerer** oder **unveränderter** (= Default-Seed-)Bezeichnung wird
   serverseitig **abgelehnt**.
9. **Zwei** DUMMY-Positionen mit **unterschiedlichen** Bezeichnungen sind in **einer** Bestellung
   erlaubt (kein „bereits enthalten"-Fehler).
10. Der DUMMY ist sowohl in einer **Lager-** als auch in einer **Glas**-Bestellung als DUMMY-Position
    anlegbar (Glas-Artikelgruppen-Prüfung blockiert ihn nicht).
11. Es wird zu **keinem** Zeitpunkt ein neuer `Article`-Datensatz angelegt (Artikel-Neuanlage bleibt
    `masterdata`/`admin` vorbehalten).

## Test-Szenarien

Ergänzung `docs/TESTSZENARIEN.md` Kapitel 46/52 (Glas-Bestellung / Lagerbestellung aus der
Stückliste, oder ein neues Kapitel — im Review zu entscheiden):

- **Kommentar (Anzeige + Ownership) — Vorbedingung:** Draft-Bestellung des eigenen Users existiert.
  **Schritte:** Kommentar eintragen, speichern, zur Lager-Ansicht (`/WarehousePicking`) wechseln.
  **Erwartetes Verhalten:** Kommentar read-only sichtbar in Eingehenden Listen (Spalte) + Details.
  **Negativfälle:** (a) Kommentar leer lassen → keine Anzeige-Fehler, leere Zelle; (b) Bestellung
  eines **anderen** Users bzw. bereits submittete Bestellung → Kommentar nicht editierbar
  (Forbid/„nicht mehr im Entwurf").
- **Kommentar im Submit-Mail — Vorbedingung:** Draft mit Kommentar, Benachrichtigungs-Mail aktiv.
  **Schritte:** Bestellung submitten, NotificationWorker durchlaufen lassen, Mail prüfen.
  **Erwartetes Verhalten:** Kommentar erscheint im Kopf des Submit-Mails (HTML **und** Text-Teil).
  **Negativfall:** Bestellung ohne Kommentar → Mail ohne Kommentarzeile; Storno-Mail nie mit
  Kommentar.
- **DUMMY-Position (Lager) — Vorbedingung:** DUMMY-Seed eingespielt; EK-Nummer, die nicht existiert.
  **Schritte:** Artikelsuche mit dieser Nummer, „nicht gefunden", DUMMY-Position mit eigener
  Bezeichnung anlegen.
  **Erwartetes Verhalten:** Position mit der eingegebenen Bezeichnung erscheint; `Article.Description`
  des DUMMY unverändert.
  **Negativfälle:** (a) Bezeichnung leer → Ablehnung; (b) Bezeichnung = Default-Seed-Text →
  Ablehnung.
- **DUMMY mehrfach je Bestellung — Schritte:** zwei DUMMY-Positionen mit **verschiedenen**
  Bezeichnungen in **einer** Bestellung anlegen. **Erwartetes Verhalten:** beide erscheinen, kein
  „bereits enthalten"-Fehler.
- **DUMMY in Glasbestellung — Schritte:** in einer **Glas**-Bestellung eine DUMMY-Position anlegen.
  **Erwartetes Verhalten:** anlegbar (kein „gehoert in die Lager-Bestellung"-Fehler).

`secondbrain/tests/testszenarien-index.md` entsprechend ergänzen.

## Deploy

**Finalisiert durch QA (2026-08-06) — aus dem echten Diff des gemeinsamen Worktrees
`feature/2026-08-05-wms-bugs-improvements-4-8`, nicht der provisorischen Spec-Agent-Schätzung.**

**Reale SQL-Nummern (bei Umsetzung vergeben):** Teil 8 hat `84` (`AddUserDefaultFilterFaWorklist
Description1`) belegt; dieser Teil bekam daher **`85`** (Schema, `WarehouseRequisition.Comment`)
und **`86`** (Daten-Seed, DUMMY-Artikel) — nicht die in der ursprünglichen Spec platzhalterhaft
genannten `84`/`85`. Beides in `SQL/00_FreshInstall.sql` verifiziert (Spalte `[Comment] NVARCHAR
(1000) NULL` in der `WarehouseRequisitions`-Definition, `__EFMigrationsHistory`-Insert für
`20260806081737_AddWarehouseRequisitionComment`, DUMMY-Seed-Block mit `IF NOT EXISTS`-Guard).

- **Web-App:** ja (Kommentar-UI/-Endpunkt, DUMMY-UX/-Endpunkt).
- **Service:** **ja** — der Submit-Mail-Body wird im Windows-Service erzeugt
  (`WarehouseRequisitionEmailService`, Freigabe-Antwort 2). Ohne Service-Publish würde der Kommentar
  nicht in der Mail erscheinen.
- **Migration:** **ja** — eine Schema-Migration (`WarehouseRequisition.Comment`,
  `SQL/85_AddWarehouseRequisitionComment.sql`) **und** ein reiner Daten-Seed (DUMMY-Artikel,
  `SQL/86_SeedDummyArticle.sql`). Beide vor dem Publish einspielen.
- **Publish-Befehle (aus dem Worktree, VOR dem Merge):**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSService
```

**Reihenfolge:** (1) `SQL/85_AddWarehouseRequisitionComment.sql` (Schema, additiv), (2)
`SQL/86_SeedDummyArticle.sql` (DUMMY-Seed) einspielen, dann (3) Web **und** Service publishen. Rein
additive Spalte + idempotenter Seed → kein Backup-Zwang über das Standard-Vorgehen hinaus.

Fluss: SQL 85+86 einspielen → Publish Web **und** Service **aus dem Worktree** → Testsystem →
manueller Test (unten) → dann Merge. Nach dem Merge nur dann erneut aus `main` publishen, wenn der
Merge tatsächlich getestete Dateien mit parallelen `main`-Änderungen kombiniert hat.

## QA-Nachweis (2026-08-06)

Verifiziert im Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-05-wms-bugs-improvements-4-8`
(Branch `feature/2026-08-05-wms-bugs-improvements-4-8`, HEAD `a1d4d76`), gemeinsam mit Teil 4/5/8:

- **Build:** `dotnet build IdealAkeWms.slnx` → **0 Fehler** (9 Vorbestehende Warnungen, keine neuen).
- **Tests:** `dotnet test` →
  - `IdealAkeWms.Tests`: **1075 bestanden, 1 übersprungen, 0 fehlgeschlagen** (1076 gesamt).
  - `IDEALAKEWMSService.Tests`: **197 bestanden, 0 fehlgeschlagen**.
- **Migrationen:** genau **zwei** neue EF-Migrationen im Worktree bestätigt —
  `20260806081737_AddWarehouseRequisitionComment` (dieser Teil) und
  `20260806081121_AddUserDefaultFilterFaWorklistDescription1` (Teil 8). Der DUMMY-Seed
  (`SQL/86_SeedDummyArticle.sql`) ist bewusst **kein** EF-Migration/`__EFMigrationsHistory`-Eintrag
  (reiner Daten-Seed, wie in ADR 0004 gefordert).
- **`SQL/00_FreshInstall.sql`** verifiziert: `[Comment] NVARCHAR(1000) NULL` in der
  `WarehouseRequisitions`-Tabellendefinition, `__EFMigrationsHistory`-Insert für die Comment-
  Migration, DUMMY-Seed-Block („17i. DUMMY-Artikel") mit `IF NOT EXISTS`-Guard vorhanden.
- **Code-Review (inline, kein Task-Subagent im QA-Environment verfügbar):** Diff gegen die
  „Finalisierung" geprüft — `AddItemAsync`-Duplikat-Guard überspringt korrekt nur
  `Article.DummyArticleNumber`; der neue `POST .../items/dummy`-Endpunkt läuft durch
  `CheckOwnershipAndDraft`, lehnt leere **und** unveränderte Default-Bezeichnung ab (case-insensitiver
  Vergleich gegen `Article.DummyDefaultDescription`), umgeht den Glas/Lager-Gruppen-Guard bewusst
  (typ-neutral) und schreibt die Bezeichnung ausschließlich als Positions-Snapshot
  (`ArticleDescription`), nicht auf `Article.Description`. `SaveCommentAsync` folgt exakt dem
  `SaveNotesAsync`-Muster (RowVersion bewusst ignoriert, Audit-Felder gesetzt). Submit-Mail-Body
  (HTML **und** Text) zeigt den Kommentar nur wenn befüllt, HTML escaped (`E(...)`); Storno-Mail
  unverändert (kein Kommentar). `WarehousePicking`/`WarehouseRequisitions`-`ColumnMap` um
  `comment` ergänzt (ADR 0005-konform, Server-Mode-Spaltenfilter).
- **`docs/TESTSZENARIEN.md`** (Worktree) ergänzt: TS-46.9 – TS-46.13 (Kapitel 46).
- **`secondbrain/tests/testszenarien-index.md`** (Hauptcheckout) nachgezogen (Kapitel 46).

## Manuelle Test-Checkliste (Schranke 2)

Am Testsystem **nach** Einspielen von `SQL/85_AddWarehouseRequisitionComment.sql` +
`SQL/86_SeedDummyArticle.sql` und Web- **und** Service-Publish durchzuführen — Referenz:
`docs/TESTSZENARIEN.md` TS-46.9 – TS-46.13.

1. **TS-46.9 (Kommentar Anzeige + Ownership):** Draft-Bestellung (Lager) anlegen, Kommentar
   eintragen + speichern, zu `/WarehousePicking` wechseln → Kommentar read-only sichtbar in der
   Spalte „Kommentar" **und** in der Detailansicht. Leere Bestellung → leere Zelle, kein Fehler.
   Als anderer User bzw. nach Submit versuchen zu ändern → nicht editierbar.
2. **TS-46.10 (Submit-Mail):** Draft mit Kommentar submitten, NotificationWorker abwarten, Mail
   prüfen (HTML **und** Text) → Kommentar im Kopf sichtbar. Bestellung ohne Kommentar → keine
   Kommentarzeile. Storno-Mail → nie mit Kommentar.
3. **TS-46.11 (DUMMY Lager):** EK-Nummer ohne Treffer suchen → „nicht gefunden" →
   DUMMY-Position mit eigener Bezeichnung anlegen → Position erscheint mit dieser Bezeichnung,
   `Article.Description` des DUMMY bleibt unverändert. Negativfälle: leere Bezeichnung → Ablehnung;
   unveränderte Default-Bezeichnung → Ablehnung.
4. **TS-46.12 (DUMMY mehrfach):** Zwei DUMMY-Positionen mit unterschiedlichen Bezeichnungen in
   einer Bestellung anlegen → beide erscheinen, kein „bereits enthalten"-Fehler.
5. **TS-46.13 (DUMMY in Glas):** In einer Glas-Bestellung eine DUMMY-Position anlegen → anlegbar,
   kein Glas-Gruppen-Fehler.
6. **TS-46.14 (DUMMY nicht ueber reguläre Suche, UAT-Fix-Regression):** Im Lager- **und** im
   Glas-Draft gezielt nach „DUMMY" suchen → **0** Treffer, nur der „nicht gefunden"-Block mit
   Pflicht-Bezeichnung erscheint. Normale Artikel weiterhin ueber Suche + Add-Pfad hinzufuegbar.

## Offene Rückfragen

Alle fünf ursprünglichen Rückfragen sind durch Schranke 1 beantwortet (siehe
„## Freigabe-Antworten" und „## Finalisierung"). Keine offenen Punkte mehr:

1. **Editierbarkeit Kommentar** → nur Besteller/Ersteller, nur im Draft (`CheckOwnershipAndDraft`);
   kein zweites Feld „Kommentar Lager".
2. **Kommentar im Submit-Mail** → ja (HTML + Text), Storno-Mail bleibt ohne Kommentar.
3. **DUMMY-Namensfrage / Bezeichnung** → ein geseedeter DUMMY-Artikel (`ArticleNumber = 'DUMMY'`);
   die individuelle, zwingend zu ändernde Bezeichnung lebt auf der Position
   (`WarehouseRequisitionItem.ArticleDescription`), nicht auf `Article.Description`.
4. **Rollen/Zugriff** → keine Artikel-Neuanlage; damit kein `[RequireMasterDataAccess]`-Bypass.
5. **DUMMY-Markierung** → keine; normaler WMS-Artikel, kein `IsManuallyCreated`-Flag.

## Größe / Schnitt (Umsetzungshinweis)

Diese Spec ist die **umfangreichste** des Backlogs und ein **voller Dev-Lauf**: Strang A (Kommentar)
und Strang B (DUMMY) sind weitgehend unabhängig, beide mit Web- **und** Service-/SQL-Anteil.
Empfehlung für die Umsetzung (ohne die Spec zu splitten):

- **Phase A zuerst — Kommentar:** Model-Feld + Migration `84` + `SaveCommentAsync` + Comment-Endpunkt
  + Edit-UI + Listen-Spalte + Details + Submit-Mail (Web **und** Service). In sich abgeschlossen und
  isoliert testbar.
- **Phase B danach — DUMMY:** SQL-Seed `85` + reservierte Konstante + „nicht gefunden"-UX +
  DUMMY-Endpunkt mit Pflicht-Bezeichnung + Duplikat-Guard-Ausnahme + Glas-Freigabe.

Reihenfolge A→B, weil A das kleinere, geradlinigere Risiko trägt und die Migrationsnummern
(84 Schema vor 85 Seed) in dieser Reihenfolge sauber vergeben werden. Beide Phasen laufen im selben
Worktree/derselben Spec.

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →nur vom Besteller / eingeber
2. →ja, auch im SUBMIT Mail
3. →Dummy Aritkel als eigenen Artikel - wenn dieser verwendet wird, muss die Bezeichnung zwingend geändert werden
4. →es ist keine artikelneuanlage gefordert, der DUMMY Artikel wird einmalig per SQL insert mit bei installation automatisch angelegt.
5. →dieser eine DUMMY Artikel braucht keine eigene Markierung - reicht ein normaler WMS Artikel

## Kritische Pruefung (2026-08-05)

Rolle: Anwalt des Teufels. Belege mit `datei:zeile`. Die Freigabe-Antworten 3/4/5 kippen den
Teil-B-Entwurf substanziell — die Spec beschreibt noch die **verworfene** Loesung
(„App legt bei unbekannter EK-Nummer selbststaendig einen neuen Artikel an"). Sie ist vor
Umsetzung neu zu schreiben.

### BLOCKER 1 — Gesamter Teil B basiert auf der abgelehnten Loesung (Auto-Artikelanlage)
Antwort 4 („**keine** Artikelneuanlage gefordert; **ein** DUMMY-Artikel wird einmalig per
SQL-Insert bei Installation angelegt") + Antwort 5 („ein normaler WMS-Artikel, keine Markierung")
invalidieren:
- Fachliche Anforderung B.1/B.2 (Zeile 99-104, „App legt automatisch einen neuen `Article` an"),
- den Technischen Loesungsentwurf B (Zeile 191-207, `POST .../items/dummy` das einen `Article`
  erzeugt, `GetByArticleNumberAsync`-Dedup, ArticleNumber-Herkunft),
- Akzeptanzkriterien 4-7 (Zeile 266-273: „neuen Artikel anlegen lassen", „in der regulaeren
  Artikelsuche auffindbar", „kein zweiter Artikel-Datensatz" — alles gegenstandslos),
- Offene Rueckfragen 3/4/5 (jetzt beantwortet → die daran haengenden Design-Zweige entfallen).
Neuer Ziel-Ablauf: **ein** vorab per SQL geseedeter DUMMY-Artikel; die Werkbank waehlt ihn (statt
Auto-Anlage) und vergibt eine Pflicht-Bezeichnung je Position. **Frage:** Bitte Teil B komplett auf
diesen Ablauf umschreiben (Anforderungen, Loesungsentwurf, Akzeptanzkriterien, Migrations-Abschnitt).
Antwort, ja b bitte komplett umschreiben
### BLOCKER 2 — „Bezeichnung zwingend aendern" (Antwort 3): Ablage + Zwang unspezifiziert und heute nicht unterstuetzt
Antwort 3 verlangt, dass die Bezeichnung bei Nutzung zwingend geaendert wird. Wo landet sie?
- **Korrekt: pro Position** — `WarehouseRequisitionItem.ArticleDescription`
  (`WarehouseRequisitionItem.cs:14-15`, `[Required][StringLength(500)]`) existiert bereits und
  wird beim Hinzufuegen als Snapshot gesetzt (`WarehouseRequisitionRepository.cs:135`). Damit sehen
  andere Bestellungen **nicht** denselben Text, und der Sage-Sync beruehrt sie nicht.
- **Verboten: die geteilte `Article.Description`** (`Article.cs:12-14`) aendern — das traefe alle
  DUMMY-Nutzungen zugleich und widerspraeche Antwort 5 („normaler WMS-Artikel"). Spec muss das
  explizit ausschliessen.
- **Fehlt technisch:** `AddItem` kopiert heute stur `article.Description`
  (`WarehouseRequisitionsApiController.cs:90`; `AddItemAsync`-Signatur ohne Bezeichnungs-Parameter,
  `WarehouseRequisitionRepository.cs:114-115`). Es gibt **keinen** Weg, eine abweichende Bezeichnung
  je Position mitzugeben, und **keine** serverseitige Pflicht-/Aenderungs-Pruefung.
**Frage:** Bitte definieren: (a) neuer Parameter `description` auf dem Add-Pfad, der die
Positions-`ArticleDescription` setzt; (b) Server-Guard, der die Anlage einer DUMMY-Position ablehnt,
wenn die Bezeichnung leer ist ODER unveraendert dem DUMMY-Default entspricht (woran erkennt der
Server „geaendert"? Vergleich gegen die Seed-`Article.Description`).

### BLOCKER 3 — Duplikat-Guard verhindert mehrere DUMMY-Positionen in einer Bestellung
`AddItemAsync` weist ein zweites Item mit derselben `ArticleNumber` in einer Bestellung ab
(`WarehouseRequisitionRepository.cs:117-124`, „Artikel '…' ist bereits in dieser Bestellung
enthalten"). Bei **einem** geteilten DUMMY-Artikel (Antwort 4) kann eine Werkbank damit **nur eine**
unbekannte Position je Bestellung erfassen — zwei verschiedene unbekannte Teile in derselben
Bestellung sind blockiert. **Frage:** Soll der DUMMY-Artikel vom Duplikat-Guard ausgenommen werden
(dann bricht die Annahme „ArticleNumber je Bestellung eindeutig"), oder ist eine DUMMY-Position pro
Bestellung fachlich akzeptabel?

### BLOCKER 4 — Glasbestellung kann den null-Gruppen-DUMMY nicht verwenden
Der geseedete DUMMY hat keine Artikelgruppe. `GlasArticleGroupFilter.IsAllowedForType(null, …)`
(`GlasArticleGroupFilter.cs:35-43`) liefert fuer `Lager` → `true`, fuer `Glas` → **`false`**
(`glasGroups.Contains("")` ist false). Folge: der DUMMY ist in der **Glas**-gescopten Suche
(`ArticlesApiController.cs:35-37`) unsichtbar **und** wird im Add-Guard
(`WarehouseRequisitionsApiController.cs:80`) abgelehnt. Das Backlog nennt aber explizit
„Lager **bzw. Glas**bestellung" (Backlog Zeile 16-18). **Frage:** Wie soll der eine DUMMY in beiden
Typen nutzbar sein — dem DUMMY eine gemeinsame Artikelgruppe (`GemeinsameArtikelgruppen`/EUZ) geben,
zwei DUMMY-Artikel (je einen fuer Glas/Lager) seeden, oder `IsAllowedForType` fuer leere Gruppe als
„immer erlaubt" sonderregeln? (Der Spec-Hinweis Zeile 204-207 hat das vermutet — hier bestaetigt.)

### BLOCKER 5 — Antwort 2 (Kommentar im SUBMIT-Mail) widerspricht Deploy-Matrix und Out-of-Scope
Antwort 2 = „ja, auch im SUBMIT Mail". Die Spec setzt aber `deploy.service: false` (Frontmatter
Zeile 42) und listet die Mail-Aufnahme ausdruecklich unter Out-of-Scope (Zeile 75-78). Der
Submit-Mail-Body wird im **Windows-Service** erzeugt
(`WarehouseRequisitionEmailService.BuildSubmitBody:146-167` + `BuildSubmitText:189-209`,
Projekt `IDEALAKEWMSService`). **Folge:** `deploy.service` muss auf `true`, Out-of-Scope ist zu
korrigieren, und die Mail-Template-Aenderung (Kommentar im Kopf des Submit-Mails, HTML **und** Text)
gehoert in den Scope. Der Kommentar ist ein Skalar auf `WarehouseRequisition`, `GetPendingSubmit
EmailsAsync` laedt die Entitaet ohnehin (`WarehouseRequisitionRepository.cs:90-100`) — keine
Query-Aenderung noetig. Klaerung: Storno-Mail bleibt ohne Kommentar (Antwort nennt nur Submit)?

### SOLLTE 6 — Migrations-/SQL-Abschnitt bildet den DUMMY-Seed nicht ab (ADR 0004)
Antwort 4 = „per SQL-Insert bei Installation". Das ist ein reiner **Daten**-Seed (kein Schema) und
braucht nach ADR 0004-Disziplin dennoch: (a) ein idempotentes `SQL/84_*.sql` mit `IF NOT EXISTS`-
Guard und (b) Verankerung im Seed-Teil von `SQL/00_FreshInstall.sql` (Muster vorhanden, z. B.
Zeile 658 FA-Vorbau-Seeds, 919 AppSettings, 1390 Roles). **Kein** EF-Migration/`__EFMigrationsHistory`-
Eintrag (keine Schema-Aenderung). Der aktuelle Abschnitt (Zeile 214-227) nennt den Seed gar nicht und
fuehrt stattdessen die durch Antwort 5 **entfallende** `Article.IsManuallyCreated`-Migration
(Zeile 222-223). Einzige verbleibende Schema-Aenderung: `WarehouseRequisition.Comment`.

### SOLLTE 7 — Antwort 1 (nur Besteller/Eingeber): eindeutig festschreiben, WANN
„Nur vom Besteller" ist umsetzbar — der vorhandene `CheckOwnershipAndDraft`
(`WarehouseRequisitionsApiController.cs:49-60`) erzwingt bereits Ersteller-Ownership **und**
Draft-Status. Damit ist die Lager-Seite (Eingehende Listen) read-only, ein zweites Feld
„Kommentar Lager" (offene Rueckfrage 1) entfaellt — bitte die Zwei-Feld-Option aus der Spec streichen.
Offen bleibt das **Wann**: Antwort 1 nennt nur das *Wer*. Empfehlung: Kommentar nur im **Draft**
editierbar (er wird vor Submit erfasst, danach in den Eingehenden Listen nur angezeigt), also
derselbe Guard wie fuer Items. Bitte bestaetigen.

### HINWEIS 8 — DUMMY-Artikelnummer ausserhalb des Sage-Namensraums waehlen
Der Artikel-Sync UPSERTet **anhand `ArticleNumber`** (`SageImportService.cs:481-514`) und hat
**keinen** Delete-/Prune-Lauf — ein geseedeter DUMMY wird also nie geloescht (gut). ABER: existierte
in Sage je eine Ressourcen-/Artikelnummer gleich dem DUMMY-Schluessel, wuerde der Sync dessen
`Description`/`ArticleGroup` ueberschreiben. Der Seed-Schluessel muss nachweislich ausserhalb des
Sage-Namensraums liegen (z. B. reservierter Wert `DUMMY`, im Review als in Sage nicht vorhanden
bestaetigt).

### HINWEIS 9 — Schnitt/Groesse
Nach der Korrektur sind es zwei weitgehend unabhaengige Straenge, beide mit Web- **und**
Service-Anteil (Teil A jetzt inkl. Submit-Mail): A = Kommentar (Spalte + Repo `SaveCommentAsync` +
Edit-UI + Listen-Spalte + Details + Submit-Mail), B = DUMMY (Seed + „nicht gefunden"-UX +
Bezeichnungs-Override + Guards fuer Glas/Duplikat). Ein Dev-Lauf ist plausibel, aber ein Schnitt in
zwei Sub-Tasks (A Kommentar / B DUMMY) reduziert das Risiko — zur Ueberlegung.

---
**NACHBESSERUNG NOETIG:** Teil B auf „ein geseedeter DUMMY-Artikel + Pflicht-Bezeichnung je Position"
umschreiben (BLOCKER 1-4); `deploy.service=true` + Submit-Mail in Scope (BLOCKER 5); DUMMY-Seed nach
ADR 0004 im Migrations-Abschnitt verankern und die entfallende `IsManuallyCreated`-Migration streichen
(SOLLTE 6).

## Finalisierung (2026-08-05)

Alle Befunde der „## Kritische Pruefung" wurden gemäß den Freigabe-Antworten aufgelöst; der Body ist
vollständig auf den neuen Ablauf umgeschrieben.

- **BLOCKER 1 (Auto-Artikelanlage verworfen)** → aufgelöst. Teil B komplett neu: **ein** per SQL
  geseedeter DUMMY-Artikel (`ArticleNumber = 'DUMMY'`), die Werkbank **wählt** ihn statt Auto-Anlage.
  Fachliche Anforderungen B.1-6, Technischer Lösungsentwurf B, Akzeptanzkriterien 5-11 und der
  Migrations-Abschnitt neu geschrieben; die gegenstandslosen alten AK zum Auto-Create entfernt.
- **BLOCKER 2 (Bezeichnung zwingend ändern / Ablage)** → aufgelöst. Bezeichnung landet auf der
  **Position** (`WarehouseRequisitionItem.ArticleDescription`), **nicht** auf `Article.Description`
  (explizit Out-of-Scope). Neuer DUMMY-Endpunkt nutzt den bereits vorhandenen `description`-Parameter
  von `AddItemAsync` (Repo 114/135) und lehnt leere ODER unveränderte (= Default-Seed-)Bezeichnungen
  serverseitig ab.
- **BLOCKER 3 (Duplikat-Guard)** → aufgelöst. `AddItemAsync`-Guard (Repo 117-124) wird für den
  DUMMY-Schlüssel übersprungen; mehrere DUMMY-Positionen mit unterschiedlichen Bezeichnungen erlaubt
  (Anforderung B.5 + AK 9).
- **BLOCKER 4 (Glas)** → aufgelöst. DUMMY in Lager **und** Glas wählbar; empfohlene Umsetzung: der
  DUMMY-Endpunkt umgeht den `GlasArticleGroupFilter`-Guard ganz (typ-neutral), Alternative
  „leere Gruppe/reservierter Schlüssel immer erlaubt" dokumentiert (Anforderung B.6 + AK 10).
- **BLOCKER 5 (Submit-Mail vs. Deploy-Matrix)** → aufgelöst. `deploy.service = true`; Submit-Mail
  (HTML + Text) in Scope, Out-of-Scope korrigiert; Storno-Mail bleibt ohne Kommentar. Publish-Befehl
  für den Service ergänzt.
- **SOLLTE 6 (DUMMY-Seed nach ADR 0004)** → aufgelöst. Migrations-Abschnitt trennt jetzt Schema
  (`84_AddWarehouseRequisitionComment.sql` + EF-Migration + FreshInstall an beiden Stellen) vom reinen
  Daten-Seed (`85_SeedDummyArticle.sql`, `IF NOT EXISTS`-Guard, FreshInstall-Seed-Verankerung, **kein**
  `__EFMigrationsHistory`-Eintrag). Die entfallende `Article.IsManuallyCreated`-Migration gestrichen.
- **SOLLTE 7 (Kommentar-Editierbarkeit WANN)** → aufgelöst. Nur Besteller/Ersteller, nur im Draft
  (`CheckOwnershipAndDraft`); Zwei-Feld-Option gestrichen.
- **HINWEIS 8 (Seed-Schlüssel außerhalb Sage-Namensraum)** → als Anforderung übernommen: `DUMMY` im
  Review als in Sage nicht vorhanden zu bestätigen.
- **HINWEIS 9 (Schnitt/Größe)** → als „## Größe / Schnitt" übernommen: voller Dev-Lauf, empfohlene
  Reihenfolge A (Kommentar) → B (DUMMY), ohne die Spec zu splitten.

`open_questions` (Frontmatter) auf `[]` getrimmt, alle fünf Punkte durch Schranke 1 beantwortet.
Der `## Freigabe-Antworten`-Block und die `## Kritische Pruefung` bleiben als Historie unverändert.

**BEREIT ZUR FREIGABE**

## QA-Re-Verify (2026-08-06, kombinierter Branch)

Erneut verifiziert im **kombinierten** Worktree
`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-05-wms-bugs-improvements-teil-1-2-3`
(Branch `feature/2026-08-05-wms-bugs-improvements-teil-1-2-3`, HEAD `2403038`) zusammen mit
Teil 1–3/4/5/8. Dieser Teil ist von den beiden Nutzer-Korrekturen (Teil-4-Rework, Teil-8-Nachtrag)
inhaltlich nicht betroffen.

- **Build:** `dotnet build IdealAkeWms.slnx` → **0 Fehler** (9 vorbestehende Warnungen).
- **Tests:** `dotnet test` → `IdealAkeWms.Tests`: **1092 bestanden, 1 übersprungen, 0
  fehlgeschlagen** (1093 gesamt); `IDEALAKEWMSService.Tests`: **197 bestanden, 0 fehlgeschlagen**.
  (Zahlen höher als im ursprünglichen QA-Nachweis, weil der kombinierte Branch zusätzlich Teil 1–3
  sowie die neuen Teil-4/8-Tests enthält.)
- **Migrationen/SQL erneut geprüft:** `SQL/85_AddWarehouseRequisitionComment.sql` (`COL_LENGTH`-
  Guard) und `SQL/86_SeedDummyArticle.sql` (`IF NOT EXISTS`-Guard, alle NOT-NULL-Spalten der
  `Articles`-Tabelle bedient: `ArticleNumber`, `CreatedAt`, `CreatedBy`, `CreatedByWindows`) weiterhin
  korrekt; `SQL/00_FreshInstall.sql` enthält `[Comment]` + `MigrationId` +
  DUMMY-Seed unverändert. `dotnet ef migrations has-pending-model-changes` → keine offenen
  Modelländerungen.
- **`docs/TESTSZENARIEN.md`** (Worktree): TS-46.9 – TS-46.13 unverändert vorhanden.

**Status bestätigt: Testbereit.** Deploy-Abschnitt (oben, Web **und** Service, Migration 85/86)
bleibt unverändert gültig.

## UAT-Fund + Fix (2026-08-06)

**Befund (Mensch beim Testen):** In einer Lager-Bestellung ließ sich der DUMMY über die **reguläre
Artikelsuche** auswählen und normal hinzufügen — dabei wurde die Default-Bezeichnung **ungeändert**
übernommen; die geforderte Pflicht-Überschreibung (Anforderung B.4, Freigabe-Antwort 3) wurde
umgangen.

**Root Cause:** Der DUMMY hat eine `NULL`-Artikelgruppe; `GlasArticleGroupFilter.IsAllowedForType`
liefert für **Lager** → `true`, also erschien der DUMMY in `/api/articles/search` (Lager-Scope) und
konnte über den normalen `AddItem`-Pfad rein (der `article.Description` = Default-Seed kopiert). Die
Spec-Intention „DUMMY **nicht** in die reguläre Suche einmischen" war in der Umsetzung nicht erfüllt.

**Fix (kombinierter Branch):**
- `ArticlesApiController.Search`: DUMMY wird in **beiden** Zweigen aus den Ergebnissen entfernt →
  bei unbekannter EK-Nummer liefert die Suche 0 Treffer → der „Artikel nicht gefunden →
  DUMMY-Position"-Zweig in `Edit.cshtml` greift (Pflicht-Bezeichnung).
- `WarehouseRequisitionsApiController.AddItem`: lehnt den DUMMY-Schlüssel am normalen Add-Pfad ab
  (defense in depth). Der DUMMY ist damit **ausschließlich** über `POST .../items/dummy` (mit
  Pflicht-Bezeichnung ≠ Default) anlegbar. Der Duplikat-Guard-Skip im Repo bleibt unberührt.
- +1 Regressions-Test `AddItem_DummyArtikel_WirdAbgelehnt`. Build grün (Web 1093/1 skip, Service 197).

## QA-Verifikation des UAT-Fixes (2026-08-06, formaler Lauf)

Verifiziert im **kombinierten** Worktree
`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-05-wms-bugs-improvements-teil-1-2-3`
(Branch `feature/2026-08-05-wms-bugs-improvements-teil-1-2-3`, HEAD `f2410fc` — Commit
„fix: teil-7 DUMMY — Pflicht-Bezeichnung erzwingen (UAT-Fund)").

- **Build:** `dotnet build IdealAkeWms.slnx` → **0 Fehler** (9 vorbestehende Warnungen, keine neuen).
- **Tests:** `dotnet test` → `IdealAkeWms.Tests`: **1093 bestanden, 1 übersprungen, 0
  fehlgeschlagen** (1094 gesamt); `IDEALAKEWMSService.Tests`: **197 bestanden, 0 fehlgeschlagen**.
  Der neue Regressionstest `AddItem_DummyArtikel_WirdAbgelehnt` isoliert per `--filter` grün
  bestätigt.
- **Code-Review (inline, kein Task-Subagent im QA-Environment verfügbar):**
  - `ArticlesApiController.Search`: DUMMY wird jetzt in **beiden** Zweigen ausgeschlossen —
    typ-gescoped (`.Where(a => a.ArticleNumber != Article.DummyArticleNumber)` vor dem
    Gruppen-Filter, Zeile 39) **und** ungetypt (`limit + 1` geladen, DUMMY gefiltert, dann auf
    `limit` gekappt, Zeile 45-48). Der `limit+1`-Kniff ist unschädlich: `Where` gefolgt von `Take`
    liefert einfach weniger Treffer, wenn `SearchAsync` weniger als `limit+1` zurückgibt — kein
    Off-by-one-Risiko. `Edit.cshtml` (Zeile 161) ruft die Suche **immer** mit `type=@Model.Type`
    auf, der typ-gescopte Zweig ist also der produktiv relevante Pfad; der ungetypte Zweig
    (Select2-Partial, InboundBulk, FaCompletion) ist zusätzliche Absicherung.
  - `WarehouseRequisitionsApiController.AddItem`: der DUMMY-Guard (Zeile 81-82) sitzt **nach**
    `CheckOwnershipAndDraft` (Zeile 75-76) und **vor** dem `_repo.AddItemAsync`-Aufruf (Zeile 98) —
    es wird keine Position angelegt. Vergleich per `string.Equals(…, StringComparison.
    OrdinalIgnoreCase)` — Groß-/Kleinschreibung der Artikelnummer spielt keine Rolle.
  - Der legitime DUMMY-Pfad (`POST .../items/dummy`, `AddDummyItem`) bleibt unverändert und voll
    funktionsfähig: Pflicht-Bezeichnung (leer **oder** unverändert = Default → `BadRequest`,
    Zeile 142-148), mehrere DUMMY-Positionen je Bestellung weiterhin erlaubt (Duplikat-Guard-Skip
    in `WarehouseRequisitionRepository.AddItemAsync:119` unverändert, `articleNumber !=
    Article.DummyArticleNumber`).
  - Regression normale Artikel: Der neue DUMMY-Guard in `AddItem` ist strikt auf
    `Article.DummyArticleNumber` begrenzt: Nicht-DUMMY-Artikel durchlaufen unverändert Ownership-
    /Draft-Guard → Glas/Lager-Gruppen-Guard → `AddItemAsync`. Keine Verhaltensänderung.
  - Keine Nebenwirkung auf Teil 1-5/8 festgestellt: Der Diff (3 Dateien: Test, `AddItem`,
    `ArticlesApiController.Search`) berührt ausschließlich DUMMY-spezifischen Code; die übrigen
    1092 Tests (inkl. aller Teil-1-5/8-Suiten) bleiben unverändert grün.
- **`docs/TESTSZENARIEN.md`** (Worktree) ergänzt: **TS-46.14** „DUMMY ist NICHT ueber die reguläre
  Artikelsuche waehlbar (Regression, UAT-Fix v1.30.0)" — deckt genau diesen UAT-Fund ab (Suche nach
  `DUMMY` liefert 0 Treffer in Lager **und** Glas, direkter API-Call auf den normalen Add-Pfad wird
  mit 400 abgelehnt, normale Artikel bleiben unbeeinträchtigt). Kapitel-46-Übersichtszeile auf
  „TS-46.1 – TS-46.14" aktualisiert.
- **`secondbrain/tests/testszenarien-index.md`** (Hauptcheckout) nachgezogen (Kapitel 46, TS-46.14
  ergänzt).

**Befund:** DUMMY-Fix korrekt und ohne Regression. **Status bleibt: Testbereit.** Deploy-Abschnitt
(oben, Web **und** Service, Migration 85/86) bleibt unverändert gültig — der Fix ändert nur
Web-Code (kein neues SQL/keine neue Migration). Der kombinierte Branch
`feature/2026-08-05-wms-bugs-improvements-teil-1-2-3` ist aus QA-Sicht weiterhin bereit für
Schranke 2 (manueller UAT durch den Menschen, danach Merge). Die manuelle Test-Checkliste oben ist
um Punkt 6 zu ergänzen: **TS-46.14 nachtesten** (Suche nach „DUMMY" liefert keine Treffer, nur der
„nicht gefunden"-Weg funktioniert).

## UAT-Fund + Fix #2 (2026-08-06): mehrere DUMMY-Positionen scheiterten am DB-Unique-Index

**Befund (Mensch, HTTP 500 auf `POST /api/warehouserequisitions/48/items/dummy`):** Die **zweite**
DUMMY-Position in derselben Bestellung warf einen `SqlException 2601` — Verletzung des Unique-Index
`IX_WarehouseRequisitionItems_WarehouseRequisitionId_ArticleNumber`, Schluessel `(48, DUMMY)`.

**Root Cause:** Anforderung B.5 („mehrere DUMMY-Positionen je Bestellung") war nur auf **App-Ebene**
umgesetzt (Duplikat-Guard in `AddItemAsync` fuer den DUMMY uebersprungen). Der **DB-seitige**
Unique-Index auf `(WarehouseRequisitionId, ArticleNumber)` blockierte aber weiterhin, weil alle
DUMMY-Positionen dieselbe `ArticleNumber = 'DUMMY'` tragen. **InMemory erzwingt Unique-Indizes nicht**
→ die Unit-Tests waren gruen, der reale SQL Server schlug fehl (bekannter Fallstrick
[[feedback_inmemory_unique_indexes]]).

**Fix:** Der Unique-Index wurde zu einem **gefilterten** Unique-Index umgebaut
(`WHERE [ArticleNumber] <> 'DUMMY'`, Muster wie der bestehende `IX` auf `User.UserId`). Normale
Artikel bleiben je Bestellung eindeutig; der DUMMY ist ausgenommen.
- `ApplicationDbContext`: `.HasFilter("[ArticleNumber] <> 'DUMMY'")` am Index.
- **Migration 88** `20260806120650_AllowMultipleDummyRequisitionItems` (drop + recreate gefiltert),
  `SQL/88_*.sql` (idempotent), `SQL/00_FreshInstall.sql` (Index-Filter + MigrationId).
- Auto-Apply via `db.Database.Migrate()` beim App-Start (EF-Migration).

**Deploy-Nachtrag:** zusaetzlich **Migration 88** (laeuft automatisch beim App-Start; manuell:
`SQL/88`). **UAT:** in einer Bestellung **zwei** DUMMY-Positionen mit **unterschiedlichen**
Bezeichnungen anlegen → beide werden gespeichert (kein 500).
