---
type: spec
title: "FA-Abarbeitungsliste: personalisiert abspeicherbarer Bezeichnung-1-Filter (analog Artikelgruppen-Filter)"
slug: 2026-08-05-wms-bugs-improvements-teil-8-spec
status: Testbereit
created: 2026-08-05
updated: 2026-08-06
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: ""
task: "[[2026-08-05-deploy-wms-bugs-teil-4-8]]"
worktree: ".claude/worktrees/2026-08-05-wms-bugs-improvements-4-8"
branch: "feature/2026-08-05-wms-bugs-improvements-4-8"
affected_code:
  - IdealAkeWms/Models/User.cs
  - IdealAkeWms/Models/ViewModels/ProfileViewModel.cs
  - IdealAkeWms/Controllers/AccountController.cs
  - IdealAkeWms/Controllers/UsersController.cs
  - IdealAkeWms/Controllers/FaWorklistController.cs
  - IdealAkeWms/Views/Account/Profile.cshtml
  - IdealAkeWms/Views/Users/Edit.cshtml (bzw. Create.cshtml, falls Feld dort ebenfalls gepflegt wird)
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - SQL/84_*.sql (Platzhalter, naechste freie Nummer)
  - SQL/00_FreshInstall.sql
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions: []
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

In der FA-Abarbeitungsliste (`/FaWorklist`) existiert bereits ein funktionierender, aber nicht
personalisierter Server-Spaltenfilter auf „Bezeichnung 1" (Col-Key `description1`). Jeder
Benutzer soll sich künftig einen eigenen Standard-Filterwert für dieses Feld hinterlegen können
(z. B. „Verdampfer"), der beim Öffnen der Liste automatisch vorausgewählt ist — ohne den Filter
jedes Mal neu eintippen zu müssen. Das Projekt kennt dieses Muster bereits für die
BOM-Ansicht: `User.DefaultFilterArtikelgruppe`/`DefaultFilterBeschaffung`, editierbar über das
Profil bzw. die Admin-Benutzerverwaltung und automatisch beim Öffnen der Stückliste angewandt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neues Feld `User.DefaultFilterFaWorklistDescription1` (Name siehe offene Rückfrage 3),
  editierbar über `/Account/Profile` (Selbstbedienung) und `UsersController.Edit` (Admin).
- Automatische Anwendung dieses Default-Werts als Vorbelegung des `description1`-Spaltenfilters
  in `FaWorklistController.Index`, wenn kein expliziter Query-Parameter
  (`?colf_description1=...`) übergeben wurde — analog dem bestehenden
  `DefaultWorkStepId`/`DefaultWorkbenches`-Vorbelegungsmuster in derselben Action.

**Out-of-Scope**
- Keine Änderung an der bereits bestehenden, generischen Server-Spaltenfilter-Funktion für
  `description1` selbst (`ApplyMovementColumnFilter`-Äquivalent in `FaWorklistController`,
  `BuildColumnMap`) — die Filterlogik ist bereits funktionsfähig, es fehlt nur die
  Personalisierung/Persistenz.
- Kein neues, generisches „Speicherbare Filter"-Framework für beliebige Spalten/Listen — die
  Anforderung ist explizit auf „Bezeichnung 1 in der Abarbeitungsliste, analog dem bestehenden
  Artikelgruppen-Filter-Muster" begrenzt.
- Keine Änderung an `DefaultFilterArtikelgruppe`/`DefaultFilterBeschaffung` selbst (bleiben
  unverändert für die BOM-Ansicht).

## Fachliche Anforderungen

1. Jeder Benutzer kann in seinem Profil (oder von einem Admin in der Benutzerverwaltung) einen
   Standard-Filterwert für „Bezeichnung 1" in der FA-Abarbeitungsliste hinterlegen.
2. Beim Öffnen von `/FaWorklist` ohne expliziten `description1`-Filter in der URL wird dieser
   gespeicherte Wert automatisch als Filter angewandt.
3. Ein expliziter Filter in der URL (z. B. über direktes Eintippen im Spaltenfilter-Feld)
   überschreibt den gespeicherten Default für diesen Aufruf (Session-artiges Override-Verhalten,
   analog dem bestehenden `workbenches`-Parameter-Handling in derselben Action).
4. Der gespeicherte Default bleibt bestehen, bis der Benutzer ihn im Profil ändert oder löscht
   (leeres Feld = kein Default = wie heute ungefiltert).

## Ist-Zustand (Code-Referenzen)

**Bestehendes Referenzmuster (BOM-Ansicht):**
- `IdealAkeWms/Models/User.cs:29-35`: `DefaultFilterBeschaffung`, `DefaultFilterArtikelgruppe`
  (`[StringLength(100)]`, beide `string?`).
- `IdealAkeWms/Controllers/AccountController.cs:151-167` (`Profile` GET) und
  `:169-199` (`Profile` POST): lädt/speichert beide Felder für den eingeloggten Benutzer.
- `IdealAkeWms/Controllers/UsersController.cs:108-109, 234-235, 274-275`: identisches
  Muster für die Admin-Pflege eines fremden Benutzers.
- `IdealAkeWms/Controllers/PickingController.cs:217-231, 365-366`: lädt die beiden Felder des
  aktuellen Benutzers und reicht sie unverändert an `BomViewModel.DefaultFilterBeschaffung`/
  `DefaultFilterArtikelgruppe` durch.
- `IdealAkeWms/Views/Picking/Bom.cshtml:934-937`:
  ```js
  var defaultBeschaffung = '@Html.Raw(Model.DefaultFilterBeschaffung)';
  var defaultArtikelgruppe = '@Html.Raw(Model.DefaultFilterArtikelgruppe)';
  ...
  if (defaultArtikelgruppe) window.setColumnFilter('article-group', defaultArtikelgruppe);
  ```
  **Wichtig:** Dieses Muster wirkt **client-seitig** beim Laden der Seite — die BOM-Ansicht ist
  eine der dokumentierten Ausnahmen vom Server-Mode-Spaltenfilter (Client-Mode, siehe
  `secondbrain/architektur/adr/0005-listen-view-pattern-mit-server-side-spaltenfilter.md`,
  Abschnitt „Begründete Ausnahmen"). Die FA-Abarbeitungsliste ist dagegen eine
  **Server-Mode**-Liste (`data-server-column-filter="true"`, vgl.
  `secondbrain/codebase/module.md`, Abschnitt „Inventar: Tabellen im Server-Filter-Mode" —
  „FaWorklist" ist dort explizit gelistet) — die Vorbelegung kann daher **nicht** 1:1 per
  `window.setColumnFilter` übernommen werden, sondern muss **serverseitig** in den
  `columnFilters`-Dictionary einfließen, bevor `ColumnFilterHelper.Apply` läuft (siehe
  Lösungsentwurf).

**Bereits funktionierender Ziel-Filter:**
- `IdealAkeWms/Controllers/FaWorklistController.cs:326-355` (`BuildColumnMap`): Col-Key
  `description1` ist bereits gemappt (`r => r.Description1`).
- `FaWorklistController.cs:242-244`:
  ```csharp
  var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
  var columnMap = BuildColumnMap(vm.AttributeColumns);
  var filteredRows = ColumnFilterHelper.Apply(rows, columnFilters, columnMap).ToList();
  ```
  `columnFilters` kommt ausschließlich aus der Query-String (`?colf_description1=...`) — kein
  Vorbelegungs-Mechanismus aus Benutzer-Defaults vorhanden.

**Bereits vorhandenes Vorbelegungs-Muster für einen ANDEREN Filter in derselben Action** (direktes
Vorbild für den serverseitigen Weg):
- `FaWorklistController.cs:92-111`: `DefaultWorkbenches`/`DefaultWorkStepId` werden nur
  angewandt, wenn der jeweilige Query-Parameter **nicht explizit** übergeben wurde
  (`workbenchesProvided`-Prüfung über `HttpContext.Request.Query.ContainsKey(...)`), sonst
  gewinnt der explizite Parameter — exakt das gewünschte Override-Verhalten aus Anforderung 3.

## Technischer Lösungsentwurf

1. `User.cs`: neues Feld `DefaultFilterFaWorklistDescription1` (`string?`,
   `[StringLength(200)]` — **entschieden in der Finalisierung**, siehe unten HINWEIS-3),
   Display-Name „Standard-Filter Bezeichnung 1 (FA-Abarbeitungsliste)".
2. `ProfileViewModel` + `AccountController.Profile` (GET/POST): Feld analog
   `DefaultFilterArtikelgruppe` laden/speichern.
3. `UsersController.Edit` (GET/POST) + zugehöriges ViewModel: Feld analog für die
   Admin-Pflege ergänzen.
4. `Views/Account/Profile.cshtml` + `Views/Users/Edit.cshtml`: neues Eingabefeld im Abschnitt
   der bestehenden Default-Filter.
5. `FaWorklistController.Index`: Vorbelegung als **Redirect-mit-Parameter** (ENTSCHEIDUNG der
   Finalisierung, siehe „## Finalisierung (2026-08-05)" → SOLLTE-1). Der gespeicherte Default wird
   **nicht** nur serverseitig in den `columnFilters`-Dictionary injiziert (das ließe den sichtbaren
   Filter-Input leer und den Filter in der Session un-löschbar), sondern beim **echten Erstaufruf**
   per `RedirectToAction` als `colf_description1`-URL-Parameter gesetzt. Danach restauriert
   `table-filter.js` den sichtbaren Input aus der URL (`restoreFiltersFromUrl`,
   `wwwroot/js/table-filter.js:41-52`), und Leeren wirkt wie ein normaler Spaltenfilter-Reset.

   Damit „Leeren" auch **über die Navigation hinweg** hält (sonst re-injiziert der nächste Load den
   Default sofort wieder), setzt der Redirect zusätzlich einen **Sentinel-Marker** `df1=1`. Er
   überlebt die Client-Navigation (`applyServerFilters` löscht nur `colf_*` und `page`,
   `table-filter.js:54-66`) und signalisiert „Default bereits angewandt/übersteuert → nicht erneut
   injizieren".

   ```csharp
   // appUserId/user HOCHGEZOGEN aus dem workStep/workbenches-Block (HINWEIS-2):
   // an der Apply-Stelle (Zeile 242) ist die im if-Block deklarierte appUserId nicht sichtbar.
   var appUserId = _currentUser.GetCurrentAppUserId();
   var currentUser = appUserId.HasValue ? await _userRepository.GetByIdAsync(appUserId.Value) : null;

   bool description1Provided = HttpContext?.Request?.Query.ContainsKey("colf_description1") ?? false;
   bool defaultFilterApplied = HttpContext?.Request?.Query.ContainsKey("df1") ?? false;

   // Erstaufruf (kein expliziter Filter, kein Sentinel) + gesetzter Default + Pflicht-Filter
   // workStepId vorhanden → einmalig auf colf_description1=<Default>&df1=1 umleiten.
   if (!description1Provided && !defaultFilterApplied && workStepId != null
       && !string.IsNullOrWhiteSpace(currentUser?.DefaultFilterFaWorklistDescription1))
   {
       var route = new RouteValueDictionary();
       foreach (var q in HttpContext.Request.Query)   // workStepId, workbenches, page, showDone … erhalten
           route[q.Key] = q.Value.ToString();
       route["colf_description1"] = currentUser.DefaultFilterFaWorklistDescription1;
       route["df1"] = "1";
       return RedirectToAction(nameof(Index), route);
   }
   ```
   Die Redirect-Prüfung muss **vor** dem Aufbau/Rendern der Liste erfolgen (spart die unnötige
   erste Abfrage). `columnFilters`/`Apply` (Zeilen 242-244) bleiben **unverändert** — der Default
   fließt jetzt über den regulären `?colf_description1=`-Pfad ein, nicht über eine Sonder-Injektion.

   **Gleichwertige Alternative (a) — sichtbaren Input direkt serverseitig vorbelegen:** den
   effektiven Default an die View durchreichen und als `value` des `colf_description1`-Inputs
   rendern (kein Redirect, keine zusätzliche Round-Trip-Navigation). Erfordert denselben
   `df1`-Sentinel, damit Leeren über die Navigation hält, und dass `table-filter.js` einen
   vorgerenderten Input-Wert nicht beim Init überschreibt (tut es heute nicht: `restoreFiltersFromUrl`
   setzt Werte nur für in der URL **vorhandene** `colf_`-Keys). Beide Wege liefern das freigegebene
   „wie Artikelgruppe"-Verhalten (sichtbar + löschbar); der Redirect-Weg ist Empfehlung, weil er
   Löschen/Übersteuern „gratis" über den normalen Spaltenfilter-Pfad erhält.
6. Kein Eingriff in `ColumnFilterHelper` selbst nötig — der Mechanismus nutzt ausschließlich die
   bereits bestehende Apply-Funktion; der Default kommt über den regulären `colf_description1`-Pfad.

## Migrations-/SQL-Auswirkungen

Additive Spalten-Erweiterung auf `Users`:
1. Model: `User.cs` um `DefaultFilterFaWorklistDescription1` erweitern.
2. `dotnet ef migrations add AddUserDefaultFilterFaWorklistDescription1 --project IdealAkeWms`.
3. Idempotentes Skript `SQL/<naechste freie Nummer>_AddUserDefaultFilterFaWorklistDescription1.sql`
   mit `OBJECT_ID`-Guard (`IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE ...)`), ein Batch
   (`ALTER TABLE [dbo].[Users] ADD [DefaultFilterFaWorklistDescription1] NVARCHAR(200) NULL`).
4. `__EFMigrationsHistory`-Insert in separatem Batch.
5. `SQL/00_FreshInstall.sql` an beiden Stellen (Schema-Objekt in der `Users`-Tabellendefinition +
   `MigrationId` im History-Insert-Block).
6. Daten-Charakter: rein additiv, kein Backfill, kein Backup-Zwang über das Standard-Vorgehen
   hinaus.

Zum Zeitpunkt dieser Spec ist main bei `SQL/83_AddSageBookingQueue.sql` — konkrete Nummer zum
Umsetzungszeitpunkt neu ermitteln (mehrere Teil-Specs dieses Backlogs können um dieselbe
nächste freie Nummer konkurrieren).

## Audit-Feld-Auswirkungen

`User` ist bereits `AuditableEntity` — jede Änderung des neuen Felds über Profile/Admin-Edit
setzt `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` wie die bestehenden Profil-Felder bereits
tun (kein zusätzlicher Aufwand, folgt dem bestehenden Speicherpfad).

## Rollen- und Zugriffsfilter-Auswirkungen

Keine neue Rolle. Das Feld ist über `/Account/Profile` für **jeden eingeloggten Benutzer** (kein
Rollen-Filter auf `AccountController.Profile`) editierbar — konsistent mit den bestehenden
`DefaultFilterArtikelgruppe`/`DefaultFilterBeschaffung`/`DefaultWorkbenches`-Feldern, die
demselben Muster folgen. Admin-Pflege über `UsersController` bleibt unter
`[RequireAdminAccess]` wie bisher.

## Listen-View-Pattern-Pflichten

Keine neue Spalte/kein neuer Col-Key — `description1` existiert bereits in
`ColumnDefinitions.FaWorklist` und in `BuildColumnMap`. Diese Änderung ergänzt ausschließlich
eine serverseitige Vorbelegung des bestehenden Filters, keine Änderung an Pagination oder
Spaltenfilter-Mechanik selbst.

## Akzeptanzkriterien

1. Im Profil (`/Account/Profile`) kann ein Benutzer einen Standard-Filterwert für
   „Bezeichnung 1 (FA-Abarbeitungsliste)" eingeben und speichern.
2. Öffnet derselbe Benutzer danach `/FaWorklist?workStepId=<id>` **ohne**
   `colf_description1`-Parameter, ist die Liste bereits auf den gespeicherten Wert gefiltert.
3. Ruft derselbe Benutzer `/FaWorklist?workStepId=<id>&colf_description1=<anderer Wert>` auf,
   gewinnt der explizite Parameter — der gespeicherte Default wird für diesen Aufruf
   überschrieben, nicht kombiniert.
4. Ein leerer/nicht gesetzter Default-Wert führt zu unverändertem Verhalten (keine Filterung,
   wie heute).
5. Ein Admin kann denselben Wert für einen anderen Benutzer über `/Users/Edit/{id}` pflegen.
6. Der gespeicherte Default ist im Bezeichnung-1-Filterfeld **sichtbar** und lässt sich in der
   Session **einmal löschen** (Feld leeren → Liste bleibt danach ungefiltert, bis der Nutzer die
   View neu lädt bzw. den Default im Profil neu speichert). Damit entspricht das Verhalten dem
   freigegebenen „wie Artikelgruppe"-Muster (sichtbarer, löschbarer Filterwert), nicht einem
   unsichtbaren, festgeklemmten Server-Filter.

## Test-Szenarien

Ergänzung `docs/TESTSZENARIEN.md` Kapitel 43 (FA-Abarbeitungsliste — Komma-Werkbank-Filter +
Bezeichnung):

- **Vorbedingung:** Benutzer mit Rolle `vorbau`, mindestens zwei FAs mit unterschiedlicher
  Bezeichnung 1 (z. B. „Verdampfer", „Kondensator") im gewählten Arbeitsgang.
- **Schritte:** Im Profil „Verdampfer" als Standard-Filter Bezeichnung 1 speichern,
  `/FaWorklist?workStepId=<id>` ohne weitere Parameter öffnen.
- **Erwartetes Verhalten:** Liste zeigt nur FAs mit „Verdampfer" in Bezeichnung 1, und der Wert
  „Verdampfer" steht **sichtbar** im Spaltenfilter-Feld „Bezeichnung 1" (URL enthält
  `colf_description1=Verdampfer` sowie den Sentinel `df1=1`).
- **Sichtbar + löschbar (Akzeptanzkriterium 6):** Das Feld „Bezeichnung 1" leeren und bestätigen
  (ENTER) → Liste zeigt wieder **alle** FAs des Arbeitsgangs; der Default greift **nicht** erneut,
  solange die View nicht frisch neu geladen wird. Erst ein frischer Aufruf von `/FaWorklist`
  (ohne `df1`) belegt den Default wieder vor.
- **Negativfall:** Filter im URL-Parameter explizit auf einen anderen Wert setzen →
  URL-Parameter gewinnt, nicht der gespeicherte Default.

`secondbrain/tests/testszenarien-index.md` Kapitel 43 entsprechend ergänzen.

## Deploy

**Finalisiert durch QA (2026-08-06) — aus dem echten Diff des gemeinsamen Worktrees
`feature/2026-08-05-wms-bugs-improvements-4-8`, nicht der provisorischen Spec-Agent-Schätzung.**

- **Web-App:** ja — `User.cs`, `ProfileViewModel`/`AccountController`, `UserEditViewModel`/
  `UsersController`, `FaWorklistController`, `Views/Account/Profile.cshtml`,
  `Views/Users/Edit.cshtml`.
- **Service:** nein — kein Diff unter `IDEALAKEWMSService/`.
- **Migration:** **ja** — additive Spalte `Users.DefaultFilterFaWorklistDescription1`
  (`NVARCHAR(200) NULL`). EF-Migration `20260806081121_AddUserDefaultFilterFaWorklistDescription1`,
  Skript `SQL/84_AddUserDefaultFilterFaWorklistDescription1.sql` (idempotenter `COL_LENGTH`-Guard,
  DDL + `__EFMigrationsHistory`-Insert in getrennten Batches), `SQL/00_FreshInstall.sql` an beiden
  Stellen (Spalte in der `Users`-Tabellendefinition **und** `MigrationId`) nachgezogen — verifiziert.
- **Reihenfolge:** SQL **vor** dem Web-Publish einspielen (additiv, kein Backup-Zwang über das
  Standard-Vorgehen hinaus).
- **Publish-Befehl (aus dem Worktree, VOR dem Merge):**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

Fluss: SQL 84 einspielen → Publish **aus dem Worktree** → Testsystem → manueller Test (unten) →
dann Merge. Nach dem Merge nur dann erneut aus `main` publishen, wenn der Merge tatsächlich
getestete Dateien mit parallelen `main`-Änderungen kombiniert hat.

## QA-Nachweis (2026-08-06)

Verifiziert im Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-05-wms-bugs-improvements-4-8`
(Branch `feature/2026-08-05-wms-bugs-improvements-4-8`, HEAD `a1d4d76`), gemeinsam mit Teil 4/5/7:

- **Build:** `dotnet build IdealAkeWms.slnx` → **0 Fehler** (9 Vorbestehende Warnungen, keine neuen).
- **Tests:** `dotnet test` →
  - `IdealAkeWms.Tests`: **1075 bestanden, 1 übersprungen, 0 fehlgeschlagen** (1076 gesamt).
  - `IDEALAKEWMSService.Tests`: **197 bestanden, 0 fehlgeschlagen**.
- **Migrationen:** genau **zwei** neue EF-Migrationen im Worktree bestätigt —
  `20260806081121_AddUserDefaultFilterFaWorklistDescription1` (dieser Teil) und
  `20260806081737_AddWarehouseRequisitionComment` (Teil 7). `SQL/84_*.sql` mit `COL_LENGTH`-Guard
  vorhanden; `SQL/00_FreshInstall.sql` enthält sowohl die Spalte
  `[DefaultFilterFaWorklistDescription1] NVARCHAR(200) NULL` in der `Users`-Definition als auch den
  `__EFMigrationsHistory`-Insert für `20260806081121_...`.
- **Code-Review (inline, kein Task-Subagent im QA-Environment verfügbar):** Diff gegen die
  „Finalisierung" (Redirect-mit-Parameter + Sentinel `df1`) geprüft —
  `FaWorklistController.Index` lädt `currentUser` jetzt einmalig (HINWEIS-2 adressiert, kein
  doppelter Repo-Load), leitet beim echten Erstaufruf (kein `colf_description1`, kein `df1`,
  `workStepId` gesetzt, Default nicht leer) per `RedirectToAction` auf
  `?...&colf_description1=<Default>&df1=1` um; `columnFilters`/`Apply` unverändert. `StringLength(200)`
  wie in HINWEIS-3 entschieden. Profil- und Admin-Speicherpfad (`AccountController`,
  `UsersController`) analog `DefaultFilterArtikelgruppe` umgesetzt.
- **`docs/TESTSZENARIEN.md`** (Worktree) ergänzt: TS-43.5 (Kapitel 43).
- **`secondbrain/tests/testszenarien-index.md`** (Hauptcheckout) nachgezogen (Kapitel 43).

## Manuelle Test-Checkliste (Schranke 2)

Am Testsystem **nach** Einspielen von `SQL/84_AddUserDefaultFilterFaWorklistDescription1.sql` und
Web-Publish durchzuführen — Referenz: `docs/TESTSZENARIEN.md` TS-43.5.

1. Im Profil (`/Account/Profile`) „Verdampfer" als „Standard-Filter Bezeichnung 1
   (FA-Abarbeitungsliste)" speichern.
2. `/FaWorklist?workStepId=<id>` **ohne** weitere Parameter öffnen → Liste zeigt nur FAs mit
   „Verdampfer"; der Wert steht **sichtbar** im Spaltenfilter-Feld „Bezeichnung 1"; URL enthält
   `colf_description1=Verdampfer` und `df1=1`.
3. Feld „Bezeichnung 1" leeren, ENTER → Liste zeigt wieder **alle** FAs; Default greift **nicht**
   erneut ohne frischen Aufruf.
4. Negativfall (Override): `/FaWorklist?workStepId=<id>&colf_description1=Kondensator` direkt
   aufrufen → expliziter Parameter gewinnt, nicht der gespeicherte Default.
5. Als Admin über `/Users/Edit/{id}` denselben Default für einen anderen Benutzer setzen und
   speichern → wirkt bei dessen nächstem Erstaufruf wie Schritt 2.
6. Default im Profil leeren/speichern, `/FaWorklist?workStepId=<id>` öffnen → unveränderte
   Filterung (kein Default, wie vor dieser Änderung).

## Offene Rückfragen

1. Reicht die exakte Wiederverwendung des bestehenden Musters (Bearbeitung NUR über
   `/Account/Profile` bzw. Admin-Edit, kein Inline-Button auf der Abarbeitungsliste selbst — wie
   es `DefaultFilterArtikelgruppe`/`DefaultFilterBeschaffung` heute für die BOM-Ansicht tun),
   oder wird ein direkter „Filter merken"-Button auf der FaWorklist-Seite selbst erwartet (mehr
   Komfort, aber ein neuer, bisher nicht vorhandener UI-Baustein)?
2. Soll der neue Default-Filter exakt wie der bestehende Spaltenfilter `description1`
   (Substring-Contains, Mini-Syntax OR/NOT mit `,` und `!`) funktionieren, oder als eigene,
   einfachere Textliste (z. B. nur OR-Vergleich exakter Werte wie „Verdampfer")? Bestimmt, ob die
   Mini-Syntax-Regeln 1:1 übernommen werden können.
3. Feldname/Label: Vorschlag `DefaultFilterFaWorklistDescription1` (oder kürzer) — eindeutiger
   Name zur Abstimmung, da `User.cs` bereits mehrere ähnlich benannte Default-Filter-Felder hat
   (`DefaultFilterBeschaffung`, `DefaultFilterArtikelgruppe`) und Verwechslungsgefahr besteht.

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →ja, reicht wie gehabt
2. →exatkt wie spaltenfilter, nur das dieser halt im benutzer settings gesetzt ist und beim laden der view den filter automatisch befüllt, hier haben wir zb. für Artikelgruppe bereits diese logik
3. →ja, passt.

## Kritische Pruefung (2026-08-05)

Rolle: Anwalt des Teufels. Alle technischen Kern-Behauptungen der Spec wurden gegen den echten
Code verifiziert — die Spec ist ungewoehnlich sauber recherchiert. Ein Befund betrifft aber eine
reale Verhaltens-Abweichung vom vom Menschen freigegebenen Referenzmuster.

**Verifiziert (Behauptungen halten):**
- Referenzlogik „Artikelgruppe"-Auto-Filter EXISTIERT wirklich —
  `IdealAkeWms/Views/Picking/Bom.cshtml:934-937` (`window.setColumnFilter('article-group', defaultArtikelgruppe)`).
  Antwort 2 ist damit **nicht** auf einer falschen Annahme gebaut. Die Spec erkennt korrekt, dass
  dieses Muster **Client-Mode** ist, FaWorklist dagegen **Server-Mode**
  (`IdealAkeWms/Views/FaWorklist/Index.cshtml:81` `data-server-column-filter="true"`).
- Col-Key `description1` existiert in Header (`Index.cshtml:87`), column-config (`:186`) und
  `BuildColumnMap` (`FaWorklistController.cs:334`, `r => r.Description1`).
- Query-Key-Schema stimmt: `ColumnFilterHelper.cs:15` Prefix `colf_`, `:30` strippt Prefix →
  Dictionary-Key `description1`. Der Spec-Snippet `columnFilters["description1"] = ...` ist korrekt.
- Override-Vorbild `workbenchesProvided` (`FaWorklistController.cs:92-111`) und die Apply-Stelle
  (`:242-244`) existieren wie zitiert. Mini-Syntax (OR `,` / NOT `!`, Substring-Contains) wird durch
  denselben `Apply`-Pfad automatisch erfuellt → Antwort 2 („exakt wie Spaltenfilter") ist ohne
  Zusatzaufwand gedeckt.
- Profil-/Admin-Speichermuster existiert 1:1: `AccountController.cs:155-156/188-189`,
  `UsersController.cs:108-109/234-235/274-275`. `User` erbt `AuditableEntity` (Audit-Felder ok).
- Cross-Cutting Sage-Lagerbuchung (v1.28.0): fuer diesen reinen Lese-/Filter-View irrelevant —
  keine Beruehrung.

**SOLLTE-1 (Kern-Befund) — Server-Mode macht den Default UNSICHTBAR und in der Session
faktisch UN-loeschbar; das weicht vom freigegebenen „wie Artikelgruppe"-Verhalten ab.**
Der Loesungsentwurf speist den Default nur serverseitig in den `columnFilters`-Dictionary ein
(Spec-Schritt 5) — er landet **nicht** in der URL. Der sichtbare Spaltenfilter-Input wird in
Server-Mode aber ausschliesslich aus der URL restauriert (`wwwroot/js/table-filter.js:46-49`:
`if (key.indexOf('colf_') !== 0) return; ... input.value = value;`). Folge:
- Die Liste ist gefiltert, das Filter-Feld „Bezeichnung 1" bleibt aber **leer** — der Benutzer
  sieht nicht, *warum* nur ein Teil der FAs erscheint.
- Will der Benutzer den Default fuer *einen* Aufruf abschalten (alle FAs sehen), loescht er das
  Feld → die URL verliert `colf_description1` → beim naechsten Load greift die Injection erneut
  → der Filter wirkt „festgeklemmt". Ohne Profil-Aenderung ist er in der Session nicht abschaltbar.
Das Referenzmuster (BOM/Artikelgruppe, Client-Mode) verhaelt sich **anders**: `setColumnFilter`
befuellt das sichtbare Feld, der Wert ist sichtbar und clientseitig sofort loeschbar. Der Mensch
hat mit Antwort 2 genau dieses sichtbare/loeschbare Verhalten als Erwartung gesetzt — die
Server-Mode-Umsetzung liefert es nicht.
*Vorschlag:* Default zusaetzlich im **sichtbaren** Input vorbelegen — entweder (a) den effektiven
Default an die View durchreichen und den `colf_description1`-Input-Wert serverseitig rendern (dann
muss `table-filter.js` den vorgerenderten Wert respektieren, statt ihn beim Init aus der leeren URL
zu ueberschreiben), oder (b) beim Erstaufruf ohne `colf_description1` per `RedirectToAction` auf
`?...&colf_description1=<Default>` umleiten (Wert steht dann in URL → Input + Restore + Teilbarkeit
„gratis", und Loeschen funktioniert wie bei jedem normalen Spaltenfilter). Akzeptanzkriterium
ergaenzen: „gespeicherter Default ist im Filter-Feld sichtbar und in der Liste (ohne Profil-
Aenderung) einmalig loeschbar/uebersteuerbar". Akzeptanzkriterium 3 (Uebersteuern durch anderen
Wert) funktioniert schon; die Luecke ist das *Leeren*.

ja, Das `setColumnFilter` befuellt das sichtbare Feld,

**HINWEIS-2 — `appUserId` ist im Spec-Snippet ausserhalb des Scopes.**
Im echten Code wird `var appUserId = _currentUser.GetCurrentAppUserId();` lokal **innerhalb** des
Blocks `if (workStepId == null || !workbenchesProvided)` deklariert
(`FaWorklistController.cs:98`). Der Spec-Snippet (Schritt 5) referenziert `appUserId.HasValue` an
der Apply-Stelle (Zeile 242) — dort ist die Variable nicht sichtbar. Umsetzer muss
`GetCurrentAppUserId()` erneut aufrufen oder die Variable hochziehen. Zudem wird `user` dann ein
**zweites Mal** aus dem Repo geladen (erster Load `:101`); bei gleichem `appUserId` liesse sich der
bereits geladene `user` wiederverwenden (kleine Effizienz-/Konsistenznote, kein Blocker).

**HINWEIS-3 — StringLength(200) vs. 100 der Referenzfelder.**
Spec waehlt `[StringLength(200)]` / `NVARCHAR(200)`, die Artikelgruppen-Felder sind `[StringLength(100)]`
(`User.cs:29-35`). Antwort 2 („exakt wie Spaltenfilter") legt eher 100 nahe. Innerhalb der Spec
konsistent (Model + SQL beide 200); nur die Begruendung im Review bestaetigen. Trivial.

**HINWEIS-4 — Migrationsnummer.** Spec nennt main bei `SQL/83_*`; zum Umsetzungszeitpunkt neu
ermitteln, da mehrere Teil-Specs desselben Backlogs um dieselbe naechste freie Nummer
konkurrieren. Bereits in der Spec vermerkt — nur zur Erinnerung.

NACHBESSERUNG NOETIG: SOLLTE-1 (Server-Mode: Default unsichtbar + in-Session un-loeschbar,
Abweichung vom freigegebenen „wie Artikelgruppe"-Verhalten) vor Freigabe entscheiden — sichtbares
Input vorbelegen oder Redirect-mit-Param — und Akzeptanzkriterium ergaenzen. HINWEIS 2-4 im Review
mitnehmen.

## Finalisierung (2026-08-05)

Alle in der „## Kritische Pruefung" offenen Punkte sind entschieden und in den Spec-Körper
eingearbeitet. Es bleiben keine offenen Rückfragen (`open_questions: []`).

**SOLLTE-1 — ENTSCHEIDUNG (gemäß Empfehlung Variante b):** Der gespeicherte Default wird als
**Redirect-mit-Parameter** umgesetzt, nicht als reine serverseitige Dictionary-Injektion. Beim
echten Erstaufruf (kein `colf_description1`, kein Sentinel `df1`, Pflicht-Filter `workStepId`
gesetzt, Default nicht leer) leitet `FaWorklistController.Index` per `RedirectToAction` auf
`…&colf_description1=<Default>&df1=1` um. Wirkung:
- Der Wert steht in der URL → `table-filter.js:restoreFiltersFromUrl` (`:41-52`) befüllt den
  **sichtbaren** Spaltenfilter-Input → der Nutzer sieht, warum gefiltert wird.
- Löschen wirkt wie ein normaler Spaltenfilter-Reset. Damit das **über die Navigation hinweg**
  hält, trägt der Redirect den Sentinel `df1=1`; er überlebt `applyServerFilters` (löscht nur
  `colf_*`/`page`, `:54-66`) und verhindert die Re-Injektion beim nächsten Load. Ergebnis: der
  Default ist **einmal** löschbar/übersteuerbar, die Liste bleibt danach ungefiltert bis zum
  frischen Neuladen — exakt das freigegebene „wie Artikelgruppe"-Verhalten. Details + Code-Skizze
  im „Technischen Lösungsentwurf", Schritt 5.
- **Gleichwertige Alternative (a):** den effektiven Default an die View durchreichen und direkt als
  `value` des `colf_description1`-Inputs rendern (ohne Redirect); braucht denselben `df1`-Sentinel
  für die Löschbarkeit. Ebenfalls in Schritt 5 dokumentiert. Redirect ist Empfehlung.
- **Akzeptanzkriterium 6** ergänzt (sichtbar + einmal löschbar) und ein passender Testfall in
  „Test-Szenarien" (Sichtbar + löschbar / Sentinel `df1`).

**HINWEIS-2 — appUserId-Scope korrigiert.** Der Snippet in Schritt 5 zieht
`appUserId = _currentUser.GetCurrentAppUserId()` und den `user`-Load auf Methoden-Ebene hoch (die
im `if (workStepId == null || !workbenchesProvided)`-Block deklarierte `appUserId` aus
`FaWorklistController.cs:98` ist an der Apply-Stelle nicht sichtbar). Da die Redirect-Prüfung
ohnehin früh — vor dem Listenaufbau — greift, entfällt der zweite Repo-Load des BOM-Musters; der
einmal geladene `currentUser` wird wiederverwendet (Effizienz-/Konsistenznote aus dem Review
adressiert).

**HINWEIS-3 — StringLength ENTSCHIEDEN: `[StringLength(200)]` / `NVARCHAR(200)`.** Bewusst länger
als die 100 der Artikelgruppen-Felder (`User.cs:29-35`), weil der gespeicherte Wert die volle
Spaltenfilter-Mini-Syntax nutzen darf (OR-Listen mit `,`, NOT mit `!`, mehrere Bezeichnungen wie
`Verdampfer,Kondensator`) und damit deutlich länger als ein einzelner Artikelgruppen-Code werden
kann. Model und SQL sind konsistent auf 200. Kein Blocker, kein Backfill.

**HINWEIS-4 — Migrationsnummer.** Zum Umsetzungszeitpunkt die nächste freie `SQL/NN_*`-Nummer neu
ermitteln (Stand Spec: main bei `SQL/83_AddSageBookingQueue.sql`; mehrere Teil-Specs desselben
Backlogs konkurrieren um dieselbe nächste Nummer). Speicherort/Muster: additive Spalte auf `Users`
analog den bestehenden `DefaultFilter*`-Feldern — Model → `dotnet ef migrations add
AddUserDefaultFilterFaWorklistDescription1` → idempotentes `SQL/NN_*.sql` mit `OBJECT_ID`/
`sys.columns`-Guard (DDL-Batch) → `__EFMigrationsHistory`-Insert (separater Batch) →
`SQL/00_FreshInstall.sql` an beiden Stellen (Schema + `MigrationId`).

**Feldname/Col-Key bestätigt:** Model-Feld `User.DefaultFilterFaWorklistDescription1` (Antwort 3);
Spaltenfilter-Col-Key `description1`, Query-Key `colf_description1` (verifiziert gegen
`ColumnFilterHelper.cs:15/30` und `FaWorklistController.BuildColumnMap:334`).

Status bleibt **Entwurf**; „## Freigabe-Antworten" (Schranke 1, Mensch) unverändert.

**BEREIT ZUR FREIGABE**
