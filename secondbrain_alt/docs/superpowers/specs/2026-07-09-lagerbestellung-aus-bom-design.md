# Lagerbestellung aus der Stückliste (BOM) + Modul-Master-Schalter — Design

**Datum:** 2026-07-09
**Status:** Approved-pending-review
**Branch/Worktree:** `feature/glas-bestellung` → `.claude/worktrees/glas-bestellung`
**Version:** in **v1.25.0** falten (kein Versionssprung).

Neben dem bestehenden **Bedarfsmeldung**-Button in der Stückliste bekommt jede Zeile einen zusätzlichen **Lagerbestellung**-Button. Damit kann die Werkbank Engpässe auch als Lager-/Glasbestellung abwickeln. Zusätzlich wird das gesamte Lagerbestellungs-Modul über einen neuen Master-Schalter `LagerbestellungAktiv` ein-/ausschaltbar.

---

## Ziel / Anforderung

1. In der BOM/Stückliste (Picking-BOM **und** read-only FA-Vorbau-BOM) ein zusätzlicher **Einzel-Button je Zeile** UND ein **Bulk-Button für markierte Zeilen** (Multi-Select, analog Bedarfsmeldung): **neue Lager-/Glasbestellung** erstellen.
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
- **Einzel:** Klick → kleiner Dialog `#warehouseOrderModal` mit vorbelegter, editierbarer **Menge** (aus `data-quantity`) → AJAX POST an die quick-add-API (1 Item).
- **Bulk (Multi-Select):** analog zum Bedarfsmeldung-Bulk. Die bestehenden Zeilen-Auswahl-Checkboxen (die der Bedarfsmeldung-Bulk nutzt) werden wiederverwendet (falls keine existieren: pro Nicht-Baugruppen-Zeile eine Checkbox ergänzen). Ein Bulk-Button **„Lagerbestellung (Auswahl)"** neben dem Bulk-Bedarfsmeldung-Button (gleich gated) → Dialog `#warehouseOrderBulkModal`, der die markierten Zeilen mit vorbelegter, editierbarer Menge je Zeile listet → Bestätigen → EIN AJAX POST mit der Item-Liste.
- **Navigation/Ergebnis:** bei Erfolg wenn **genau ein Typ** betroffen → `window.location = '/WarehouseRequisitions/Edit/' + requisitionId`; bei **gemischt** (Lager UND Glas) → `window.location = '/WarehouseRequisitions'` (Übersicht) + Erfolgsmeldung „X zu Lager, Y zu Glas hinzugefügt". **Übersprungene** Items (fehlendes Recht / Artikel nicht gefunden) werden in der Meldung gelistet. Bei Fehler Alert mit `error`-Meldung. (Bestehendes Bedarfsmeldung-Modal/JS bleibt unberührt — eigene Modals/Handler.)

### 3. Quick-Add-API

`WarehouseRequisitionsApiController` — neue Action `POST /api/warehouserequisitions/quick-add`, `[RequirePickingOrStockOrLagerbestellungAccess]` (class-level) + `[RequireLagerbestellungAktiv]`. **Ein Endpunkt für Einzel UND Bulk** (Einzel = Liste mit 1 Item).
Request: `QuickAddRequest { List<QuickAddItem> Items; }`, `QuickAddItem { string ArticleNumber; decimal Quantity; }`.
Ablauf (je Item, mit pro Typ genau EINEM Draft je Lauf):
1. Menge > 0 + Artikel-Existenz je Item validieren; ungültige/fehlende → in `skipped` mit Grund, NICHT abbrechen.
2. **Typ automatisch bestimmen:** `GlasArticleGroupFilter.NormalizeGroup(article.ArticleGroup)` ∈ `ParseGroups(GlasArtikelgruppen)` → **Glas**, sonst → **Lager**. (Gemeinsame Gruppen/EUZ sind KEINE reinen Glas-Gruppen → Lager. Konsistent mit „mehrdeutig → Lager".)
3. **Rechte-Check für den bestimmten Typ:** Glas → `CanOrderGlasAsync`, Lager → `CanOrderLagerAsync`. Fehlt → Item in `skipped` mit Grund („keine Glasbestell-Berechtigung" bzw. Lager), NICHT abbrechen.
4. **Draft je Typ (lazy, EINER pro Typ pro Lauf):** beim ersten Lager-Item den offenen Lager-Draft des Users holen/anlegen, beim ersten Glas-Item den Glas-Draft — dann für alle weiteren Items desselben Typs wiederverwenden.
   - **Offenen Entwurf finden:** User-eigener `WarehouseRequisition` mit `Status == Draft && Type == t` (neue Repo-Methode `GetOpenDraftForUserAndTypeAsync(userId, type)` ODER `GetForUserAsync`-Filter). Vorhanden → wiederverwenden.
   - **Sonst neu:** `CreateDraftAsync(workplaceId, t, userId, name, winName)` mit der **User-Default-Werkbank** (bestehende `CreateDraft`-Werkbank-Auflösung für `workplaceId == null`). Keine Werkbank auflösbar → gesamter Request `BadRequest("Bitte Standard-Werkbank im Profil hinterlegen")`.
5. **Item hinzufügen:** bestehendes `AddItemAsync(reqId, articleNumber, description, unit, quantity, user, winUser)`. Die vorhandene Typ-gegen-Artikelgruppe-Validierung greift zusätzlich — konsistent, weil der Typ aus der Artikelgruppe abgeleitet wurde.
6. Rückgabe `Ok(new { lagerRequisitionId?, glasRequisitionId?, addedLager, addedGlas, skipped: [{ articleNumber, reason }] })`. Wurde gar nichts hinzugefügt (alles skipped) → `BadRequest` mit der skipped-Liste. Das Frontend leitet die Navigation daraus ab (nur ein Typ → dessen Edit; beide → Übersicht).

**Kein neues Datenmodell / keine Migration** — Reuse von `CreateDraftAsync`, `AddItemAsync`, `GlasArticleGroupFilter`, `CanOrderLager/GlasAsync`.

---

## Zugriff / Rollen

- BOM sichtbar: picking (`Picking/Bom`), vorbau (`FaWorklist/Bom`).
- Button + quick-add: nur mit Lager-/Glasbestell-Recht (`lagerbestellung`/`glasbestellung`/`picking`/`stock`/`admin`) — also ein reiner Vorbau-User ohne Bestell-Recht sieht den Button NICHT.
- Master-Schalter `LagerbestellungAktiv` gilt zusätzlich für alle (auch admin) — bei aus ist das Modul komplett weg.

## Tests

- **`GlasArticleGroupFilter`/Typ-Ableitung** (falls nötig ergänzend): Glas-Gruppe → Glas, Nicht-Glas → Lager, EUZ/gemeinsam → Lager. (Der Filter ist bereits unit-getestet; ggf. ein Test der Quick-Add-Typ-Ableitung.)
- **Quick-Add (Controller-Test, InMemory):** (a) 1 Lager-Artikel → Lager-Draft (neu) + Item; (b) 1 Glas-Artikel → Glas-Draft; (c) zweiter Lager-Artikel → derselbe offene Draft (kein neuer); (d) fehlendes Recht für den Typ → Item in `skipped`; (e) Artikel nicht gefunden → `skipped`; (f) Menge ≤ 0 → `skipped`; (g) **alles skipped** → `BadRequest`.
- **Bulk (Controller-Test, InMemory):** (h) gemischte Liste (Lager+Glas) → genau EIN Lager-Draft + EIN Glas-Draft, Items korrekt verteilt, `addedLager`/`addedGlas`/`skipped` stimmen; (i) mehrere Lager-Items → alle in EINEN Lager-Draft (kein Draft je Item).
- **`RequireLagerbestellungAktivAttribute`** (Filter-Test oder Controller-Test): bei aus → Redirect (MVC) bzw. 404 (API).
- View-Button/Modal/JS + Layout-Gating = Build + Manual-UAT.

## Deploy

- Kein Schema-Change/keine Migration. AppSetting `LagerbestellungAktiv` (Default `true`) wird beim Start geseedet — **das Modul bleibt für Bestandssysteme aktiv** (kein Feature-Verlust). Zum Deaktivieren: Einstellungen → `LagerbestellungAktiv` aus.
- Voraussetzung für den BOM-Button: `GlasArtikelgruppen`/`GemeinsameArtikelgruppen` + `DefaultLagerbestellempfaengerId`/`DefaultGlasbestellempfaengerId` wie beim Glas-Rollout konfiguriert (sonst Typ-Ableitung/Submit unvollständig).
- Manuelle Abnahme: `docs/TESTSZENARIEN.md` neues Kapitel (Button-Sichtbarkeit je Rolle/ReadOnly, Lager- vs. Glas-Ableitung, Draft-Wiederverwendung, Rechte-Fehler, Master-Schalter aus → alles weg).

## Out of scope

- Kein Umbau des Bedarfsmeldung-Buttons/-Flows (bleibt an `BestellungenAktiv`) — Bulk-Lagerbestellung spiegelt nur dessen UX (Checkboxen + Bulk-Button + Mengen-Dialog).
- Werkbank-Wahl im Dialog (Werkbank = User-Default, FA-unabhängig — User-Entscheid).

## Betroffene Dateien (Überblick)

- **Neu:** `Filters/RequireLagerbestellungAktivAttribute.cs`; Tests (`WarehouseRequisitionsApiControllerTests` quick-add, Filter-Test).
- **Geändert:** `Models/AppSettingKeys.cs`, `Program.cs` (Seed), `Views/Settings/Index.cshtml` (Toggle); `Controllers/Api/WarehouseRequisitionsApiController.cs` (quick-add + Filter), `WarehouseRequisitionsController.cs`/`MissingPartsController.cs`/`MissingPartsLagerController.cs`/`WarehousePickingController.cs` (Filter class-level); `Controllers/PickingController.cs` + `Controllers/FaWorklistController.cs` (2 ViewBags); `Views/Picking/Bom.cshtml` (Einzel-Button + Bulk-Button + Zeilen-Checkboxen + 2 Modals + JS); `Views/Shared/_Layout.cshtml` (Menü-Gating); `IWarehouseRequisitionRepository`/Impl (`GetOpenDraftForUserAndTypeAsync`).
- **Doku:** `CLAUDE.md`, `Views/Help/Changelog.cshtml`, `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md`.
