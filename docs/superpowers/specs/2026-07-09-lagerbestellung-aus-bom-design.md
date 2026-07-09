# Lagerbestellung aus der Stückliste (BOM) + Modul-Master-Schalter — Design

**Datum:** 2026-07-09
**Status:** Approved-pending-review
**Branch/Worktree:** `feature/glas-bestellung` → `.claude/worktrees/glas-bestellung`
**Version:** in **v1.25.0** falten (kein Versionssprung).

Neben dem bestehenden **Bedarfsmeldung**-Button in der Stückliste bekommt jede Zeile einen zusätzlichen **Lagerbestellung**-Button. Damit kann die Werkbank Engpässe auch als Lager-/Glasbestellung abwickeln. Zusätzlich wird das gesamte Lagerbestellungs-Modul über einen neuen Master-Schalter `LagerbestellungAktiv` ein-/ausschaltbar.

---

## Ziel / Anforderung

1. In der BOM/Stückliste (Picking-BOM **und** read-only FA-Vorbau-BOM) ein zusätzlicher Button je Zeile: **neue Lager-/Glasbestellung** erstellen.
2. Sichtbar nur wenn (a) das Lagerbestellungs-Modul aktiv ist **und** (b) der User Lager-/Glasbestell-Rechte hat.
3. Typ (Lager vs. Glas) **automatisch** aus der Artikelgruppe; mehrdeutig (gemeinsame Gruppen, z. B. EUZ) → **Lager**.
4. Klick → offener Entwurf des Users je Typ wird wiederverwendet (sonst neu angelegt), Item hinzugefügt, dann zur Bestell-Bearbeiten-Seite navigiert.
5. Neuer AppSetting-**Master-Schalter** `LagerbestellungAktiv`, der das **komplette** Lagerbestellungs-Feature ein/aus schaltet.

---

## Komponenten

### 1. Master-Schalter `LagerbestellungAktiv` (AppSetting)

- Neuer Key in `AppSettingKeys.cs` + Seeding in `Program.cs` (Kategorie „Bestellungen") + Anzeige in `Views/Settings/Index.cshtml` (Bool-Toggle).
- **Default `true`** — bewusst abweichend von den Opt-in-Modulen (`BestellungenAktiv`/`LeitstandAktiv`/`FaCompletionAktiv` default `false`): das Lagerbestellungs-Modul ist **heute bereits aktiv** (nur rollen-gated). Ein Default `false` würde ein in Betrieb befindliches Feature beim Deploy abschalten. Der Admin kann es jederzeit ausschalten.
- **Gate = „komplett ein/aus" (User-Entscheid):** bei `false` sind unsichtbar/gesperrt:
  - Lagerbestellungen (Lager+Glas): `WarehouseRequisitionsController` + `WarehouseRequisitionsApiController`
  - Meine Fehlteile: `MissingPartsController`
  - Lager: Eingehende Listen: `WarehousePickingController`
  - Lager: Fehlteile: `MissingPartsLagerController`
  - der neue BOM-Button
- **Enforcement:**
  - **Layout** (`_Layout.cshtml`): die vier Menü-Einträge nur rendern wenn `LagerbestellungAktiv` (zusätzlich zu den bestehenden Rollen-Checks). Ein `ViewBag`/Layout-Helper liefert den Wert (z. B. via `ICurrentUserService`/`IAppSettingRepository` im Layout, wie andere AppSetting-Reads im Layout).
  - **Controller-Gate:** neues Filter-Attribut `RequireLagerbestellungAktivAttribute` (TypeFilter, liest `AppSettingKeys.LagerbestellungAktiv` über `IAppSettingRepository`), class-level auf die 4 MVC-Controller + den API-Controller.
    - MVC (Controller erbt `Controller`): bei aus → `RedirectToAction("Index","Home")` + `TempData["WarningMessage"]`.
    - API (`[ApiController]`): bei aus → `NotFoundResult` (kein Redirect für JSON-Endpunkte).
    - Der Filter kumuliert mit den bestehenden Rollen-Filtern (beide müssen passieren).

### 2. BOM-Button

`Views/Picking/Bom.cshtml` (wird von `PickingController.Bom` interaktiv UND `FaWorklistController.Bom` read-only genutzt):
- Neuer Button **je Nicht-Baugruppen-Zeile** neben `.order-single-btn` (z. B. Klasse `.order-warehouse-btn`, eigenes Icon), mit `data-`-Attributen: `article-number`, `description`, `quantity` (BOM-Menge), `unit`.
- **Sichtbarkeit:** `@if (ViewBag.LagerbestellungAktiv == true && ViewBag.CanOrderWarehouse == true)` — **unabhängig von `Model.ReadOnly`** (also auch im Vorbau-Read-only sichtbar). Die bestehende Bedarfsmeldung-Spalte bleibt an `BestellungenAktiv && !ReadOnly` — die zwei Buttons sind unabhängig gated.
- **Beide Controller** setzen die zwei ViewBags:
  - `ViewBag.LagerbestellungAktiv` = AppSetting.
  - `ViewBag.CanOrderWarehouse` = `await _user.CanOrderLagerAsync() || await _user.CanOrderGlasAsync()`.
- Klick → kleiner Dialog `#warehouseOrderModal` mit vorbelegter, editierbarer **Menge** (aus `data-quantity`) → AJAX POST an die quick-add-API; bei Erfolg `window.location = '/WarehouseRequisitions/Edit/' + requisitionId`; bei Fehler Alert mit `error`-Meldung. (Bestehendes Bedarfsmeldung-Modal/JS bleibt unberührt — eigenes Modal/Handler.)

### 3. Quick-Add-API

`WarehouseRequisitionsApiController` — neue Action `POST /api/warehouserequisitions/quick-add`, `[RequirePickingOrStockOrLagerbestellungAccess]` (class-level) + `[RequireLagerbestellungAktiv]`.
Request: `QuickAddRequest { string ArticleNumber; decimal Quantity; }`.
Ablauf:
1. Menge > 0 validieren; Artikel per `ArticleNumber` laden (existiert? sonst `BadRequest("Artikel nicht gefunden")`).
2. **Typ automatisch bestimmen:** `GlasArticleGroupFilter.NormalizeGroup(article.ArticleGroup)` ∈ `ParseGroups(GlasArtikelgruppen)` → **Glas**, sonst → **Lager**. (Gemeinsame Gruppen/EUZ sind KEINE reinen Glas-Gruppen → Lager. Konsistent mit „mehrdeutig → Lager".)
3. **Rechte-Check für den bestimmten Typ:** Glas → `CanOrderGlasAsync`, Lager → `CanOrderLagerAsync`. Fehlt → `BadRequest` mit klarer Meldung („Artikel gehört zur Glas-Bestellung — dir fehlt die Glasbestell-Berechtigung." bzw. Lager).
4. **Offenen Entwurf finden:** der User-eigene `WarehouseRequisition` mit `Status == Draft && Type == bestimmterTyp` (via `GetForUserAsync(userId)` oder eine neue Repo-Methode `GetOpenDraftForUserAndTypeAsync(userId, type)`). Vorhanden → wiederverwenden.
5. **Sonst neu anlegen:** `CreateDraftAsync(workplaceId, type, userId, name, winName)` mit der **User-Default-Werkbank** (die bestehende `CreateDraft`-Werkbank-Auflösung für `workplaceId == null` wiederverwenden). Ist keine Werkbank auflösbar → `BadRequest("Bitte Standard-Werkbank im Profil hinterlegen")`.
6. **Item hinzufügen:** bestehendes `AddItemAsync(reqId, articleNumber, description, unit, quantity, user, winUser)`. Die vorhandene Typ-gegen-Artikelgruppe-Validierung (aus dem Glas-Rollout) greift zusätzlich — konsistent, weil der Typ ja aus der Artikelgruppe abgeleitet wurde.
7. Rückgabe `Ok(new { requisitionId, type })`.

**Kein neues Datenmodell / keine Migration** — Reuse von `CreateDraftAsync`, `AddItemAsync`, `GlasArticleGroupFilter`, `CanOrderLager/GlasAsync`.

---

## Zugriff / Rollen

- BOM sichtbar: picking (`Picking/Bom`), vorbau (`FaWorklist/Bom`).
- Button + quick-add: nur mit Lager-/Glasbestell-Recht (`lagerbestellung`/`glasbestellung`/`picking`/`stock`/`admin`) — also ein reiner Vorbau-User ohne Bestell-Recht sieht den Button NICHT.
- Master-Schalter `LagerbestellungAktiv` gilt zusätzlich für alle (auch admin) — bei aus ist das Modul komplett weg.

## Tests

- **`GlasArticleGroupFilter`/Typ-Ableitung** (falls nötig ergänzend): Glas-Gruppe → Glas, Nicht-Glas → Lager, EUZ/gemeinsam → Lager. (Der Filter ist bereits unit-getestet; ggf. ein Test der Quick-Add-Typ-Ableitung.)
- **Quick-Add (Controller-Test, InMemory):** (a) Lager-Artikel → Lager-Draft (neu) + Item; (b) Glas-Artikel → Glas-Draft; (c) zweiter Lager-Artikel → derselbe offene Draft (kein neuer); (d) fehlendes Recht für den Typ → BadRequest; (e) Artikel nicht gefunden → BadRequest; (f) Menge ≤ 0 → BadRequest.
- **`RequireLagerbestellungAktivAttribute`** (Filter-Test oder Controller-Test): bei aus → Redirect (MVC) bzw. 404 (API).
- View-Button/Modal/JS + Layout-Gating = Build + Manual-UAT.

## Deploy

- Kein Schema-Change/keine Migration. AppSetting `LagerbestellungAktiv` (Default `true`) wird beim Start geseedet — **das Modul bleibt für Bestandssysteme aktiv** (kein Feature-Verlust). Zum Deaktivieren: Einstellungen → `LagerbestellungAktiv` aus.
- Voraussetzung für den BOM-Button: `GlasArtikelgruppen`/`GemeinsameArtikelgruppen` + `DefaultLagerbestellempfaengerId`/`DefaultGlasbestellempfaengerId` wie beim Glas-Rollout konfiguriert (sonst Typ-Ableitung/Submit unvollständig).
- Manuelle Abnahme: `docs/TESTSZENARIEN.md` neues Kapitel (Button-Sichtbarkeit je Rolle/ReadOnly, Lager- vs. Glas-Ableitung, Draft-Wiederverwendung, Rechte-Fehler, Master-Schalter aus → alles weg).

## Out of scope

- **Bulk** (mehrere markierte Zeilen auf einmal in die Lagerbestellung) — pro-Zeile ist der Kern; Bulk (mit Typ-Split über mehrere Drafts) ggf. als Folge.
- Kein Umbau des Bedarfsmeldung-Buttons/-Flows (bleibt an `BestellungenAktiv`).
- Werkbank-Wahl im Dialog (Werkbank = User-Default, FA-unabhängig — User-Entscheid).

## Betroffene Dateien (Überblick)

- **Neu:** `Filters/RequireLagerbestellungAktivAttribute.cs`; Tests (`WarehouseRequisitionsApiControllerTests` quick-add, Filter-Test).
- **Geändert:** `Models/AppSettingKeys.cs`, `Program.cs` (Seed), `Views/Settings/Index.cshtml` (Toggle); `Controllers/Api/WarehouseRequisitionsApiController.cs` (quick-add + Filter), `WarehouseRequisitionsController.cs`/`MissingPartsController.cs`/`MissingPartsLagerController.cs`/`WarehousePickingController.cs` (Filter class-level); `Controllers/PickingController.cs` + `Controllers/FaWorklistController.cs` (2 ViewBags); `Views/Picking/Bom.cshtml` (Button + Modal + JS); `Views/Shared/_Layout.cshtml` (Menü-Gating); ggf. `IWarehouseRequisitionRepository`/Impl (Open-Draft-Query).
- **Doku:** `CLAUDE.md`, `Views/Help/Changelog.cshtml`, `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md`.
