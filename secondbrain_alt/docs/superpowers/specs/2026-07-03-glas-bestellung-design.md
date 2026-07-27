# Glas-Bestellung — Design (v1.25.0)

**Datum:** 2026-07-03
**Status:** Approved-pending-review
**Branch/Worktree:** `feature/glas-bestellung` (ab `feature/windows-auth-ad-users`, Basis 67516d1) → `.claude/worktrees/glas-bestellung`
**Modul:** Lagerbestellung (WarehouseRequisitions) + Lager-Worklists

---

## 1. Ziel

Das bestehende Lagerbestellungs-Modul um einen zweiten **Bestelltyp „Glas"** neben dem bestehenden
**„Lager"** erweitern. Ein Auftrag ist genau EINES von beidem. Typ steuert: getrennte Empfänger-Mail,
erlaubte Artikelgruppen (harte Trennung) und die Sichtbarkeit über Rollen. Alle relevanten Listen
bekommen **Lager/Glas-Reiter**.

---

## 2. Getroffene Entscheidungen (verbindlich)

1. **Typ pro Bestellung, Filter erzwungen.** `Type`-Enum am Auftrag; Artikel-Hinzufügen ist auf die
   typ-erlaubten Artikelgruppen beschränkt (Suche + serverseitige AddItem-Prüfung).
2. **Sichtbarkeit über Rollen** (keine User-Flags). Neue Rolle `glasbestellung` analog `lagerbestellung`.
   Breite Lager-Rollen (`picking`, `stock`, `stock_keyuser`) + `admin` sehen **beide** Reiter;
   `lagerbestellung` = nur Lager, `glasbestellung` = nur Glas.
3. **EUZ-Ausnahme konfigurierbar** — AppSetting `GemeinsameArtikelgruppen` (kommasepariert, Default `EUZ`),
   Gruppen darin sind IMMER in beiden Listen verfügbar.
4. **Alle relevanten Ansichten** bekommen Lager/Glas-Reiter. In den Fehlteile-Ansichten (die bereits
   Fehlteil-Status-Reiter haben): **Typ außen, Fehlteil-Status innen** (zwei Ebenen).
5. **v1.25.0**, eigener Worktree ab `feature/windows-auth-ad-users`.

---

## 3. Scope

**In Scope:**
- Neuer Bestelltyp Lager/Glas (Enum + Spalte + Migration 77).
- Rolle `glasbestellung` + Zugriffs-/Sichtbarkeitslogik.
- Getrennte Empfänger-Mail je Typ (2. AppSetting) + Typ-Label in Mail-Betreff/Text.
- Artikelgruppen-Trennung (2 AppSettings + testbarer Helper) — erzwungen bei Suche + AddItem.
- Reiter Lager/Glas in: `WarehouseRequisitions/Index` + Anlage, `WarehousePicking/Index`,
  `MissingPartsLager`, `MissingParts` (Werker).
- Doku, Tests, Hilfeseite.

**Out of Scope (YAGNI, Step 1):**
- Umbuchen eines Auftrags zwischen Lager↔Glas.
- Gemischte Aufträge (Lager+Glas in einem Auftrag).
- Pro-Artikelgruppe-Empfänger (bleibt eine Gruppe je Typ; `ArticleGroupRecipientMapping` ist für
  Bedarfsmeldungen, nicht Lagerbestellung).
- Automatische Typ-Erkennung eines Artikels außerhalb der Suche/AddItem.

---

## 4. Datenmodell

### 4.1 Enum
`IdealAkeWms/Models/WarehouseRequisitionType.cs`
```csharp
public enum WarehouseRequisitionType
{
    Lager = 1,
    Glas  = 2
}
```

### 4.2 Spalte
`WarehouseRequisition.Type` : `WarehouseRequisitionType` (tinyint, NOT NULL, Default `1 = Lager`).

### 4.3 Migration 77 (`AddWarehouseRequisitionType`)
- Rein additiv: `ADD [Type] tinyint NOT NULL DEFAULT 1`.
- Backfill implizit über Default (alle bestehenden = Lager).
- 3-Stellen-Muster: EF-Migration + `SQL/77_AddWarehouseRequisitionType.sql` (OBJECT_ID/COL_LENGTH-Guard,
  idempotent) + `SQL/00_FreshInstall.sql` (Spalte im konsolidierten Schema + History-Insert
  `20260703xxxxxx_AddWarehouseRequisitionType`).
- Index optional: gefilterter/normaler Index auf `Type` nur falls Query-Bedarf; Start ohne Index
  (Tabelle klein, In-Memory-Filter). **Entscheidung:** kein Index in Step 1.

---

## 5. Rollen, Zugriff & Sichtbarkeit

### 5.1 Rolle
- `RoleKeys.Glasbestellung = "glasbestellung"`.
- **Seed:** analog wie `lagerbestellung` (v1.23.0) angelegt wurde — im selben Mechanismus (Role-Seed).
  Beim Plan: die konkrete Seed-Stelle verifizieren (Program.cs Role-Seed ODER Migration/SQL). Rolle mit
  Beschreibung „Glas-Bestellungen erfassen + eigene Fehlteile verfolgen".

### 5.2 Helper (ICurrentUserService)
- `CanOrderLagerAsync()` = admin || picking || stock || stock_keyuser || lagerbestellung
- `CanOrderGlasAsync()`  = admin || picking || stock || stock_keyuser || glasbestellung

### 5.3 Zugriffsfilter
- `WarehouseRequisitionsController` / `-ApiController`: heute `[RequirePickingOrStockOrLagerbestellungAccess]`.
  Erweitern, sodass auch `glasbestellung` Zugriff hat. Ein reiner `glasbestellung`-User muss rein.
  **Umsetzung:** bestehenden Composite-Filter um `CanOrderGlasAsync()` ergänzen (oder neuer Filter
  `RequireBestellungAccess` = canOrderLager || canOrderGlas). **Entscheidung:** bestehenden Filter um die
  Glas-Bedingung erweitern (kleinster Diff, Name bleibt — Doku-Hinweis).
- `MissingPartsController` (Werker): heute `[RequireStockOrLagerbestellungAccess]`. Analog um Glas ergänzen.
- Lager-Worklist (`WarehousePicking`, `MissingPartsLager`): `[RequireLagerProcessingAccess]`
  (admin/stock/stock_keyuser) bleibt **unverändert** — diese User sehen beide Typen (Reiter = reiner Filter,
  keine Rollen-Gating auf Worklist-Seite).

### 5.4 Reiter-Sichtbarkeit (Order-Entry-Seite)
- `WarehouseRequisitions/Index` + Anlage + Werker-`MissingParts`: Lager-Reiter nur wenn `CanOrderLagerAsync`,
  Glas-Reiter nur wenn `CanOrderGlasAsync`. Ist nur genau ein Typ erlaubt → nur dieser Reiter, Default-Typ
  = der erlaubte.
- Wenn kein aktiver Reiter zum angeforderten `?type=` passt (z. B. Glas-Only-User ruft `?type=lager`):
  auf den ersten erlaubten Typ zurückfallen.

### 5.5 RoleOverview
`Views/Users/RoleOverview.cshtml` (hand-gepflegt) um `glasbestellung` + geänderte Filter aktualisieren.

---

## 6. Empfänger-Mail je Typ

- Neues AppSetting `DefaultGlasbestellempfaengerId` (leer = Glas-Submit blockt, wie Lager-Pendant).
- `WarehouseRequisitionsController.Submit`: Gruppen-ID nach `req.Type` wählen
  (`Glas` → `DefaultGlasbestellempfaengerId`, sonst `DefaultLagerbestellempfaengerId`), gleiche
  Validierung/Fehlermeldungen. `OrderRecipientGroupId` wird am Auftrag gesetzt wie bisher.
- Mailservice (`WarehouseRequisitionEmailService`): Empfänger-Auflösung über `OrderRecipientGroup`
  **unverändert**. Nur ein Typ-Label für Betreff/Text: „Glasbestellung #…" vs „Lagerbestellung #…".
  Umsetzung: Label aus `req.Type` ableiten (kleiner Parameter in `BuildSubmitText`/`BuildCancellationText`).
- Stammdaten-Pflege (Gruppen + Empfänger-Mails) bleibt die bestehende Empfänger-Verwaltung; der User
  hinterlegt zwei Gruppen und trägt die IDs in die zwei AppSettings ein.

---

## 7. Artikelgruppen-Trennung (erzwungen)

### 7.1 AppSettings
- `GlasArtikelgruppen` (kommasepariert, z. B. `GLAS,SPIEGEL`) — Default leer.
- `GemeinsameArtikelgruppen` (kommasepariert) — Default `EUZ`.

### 7.2 Regel (Helper `GlasArticleGroupFilter`, unit-testbar, keine DB)
Normalisierung eines Gruppen-Werts: `trim`, Uppercase, und defensiv Teil vor `" - "` nehmen
(Article.ArticleGroup speichert i. d. R. reinen Code wie `"940"`/`"EUZ"`, aber Sage-Formate wie
`"940 - Kleinmaterial"` defensiv abfangen).

- `IsAllowedForType(articleGroup, type, glasGroups, sharedGroups)`:
  - **Glas:** `g ∈ glasGroups ∪ sharedGroups`
  - **Lager:** `g ∉ glasGroups` **ODER** `g ∈ sharedGroups`
- Randfälle: leere/NULL `articleGroup` → in **Lager** erlaubt (nicht Glas), da nicht in glasGroups;
  leere `glasGroups` → Glas-Liste zeigt nur `sharedGroups`, Lager zeigt alles.

### 7.3 Durchsetzung
1. **Artikel-Suche** (`/api/articles/search`): neuer Parameter `type=lager|glas`. Server filtert die
   Trefferliste über den Helper (Config aus AppSettings). Frontend (Edit-View) übergibt den Typ des Drafts.
2. **AddItem** (`WarehouseRequisitionsApiController` / Repo): serverseitige Prüfung — Artikel dessen Gruppe
   für den Auftragstyp nicht erlaubt ist → `400 BadRequest` mit klarer Meldung (Defense-in-Depth gegen
   manuelle Requests / Typ-Wechsel).

---

## 8. UI / Reiter

Reiter-Optik konsistent (Bootstrap `nav nav-tabs`), Typ als `?type=lager|glas` in der URL, Server-seitige
Filterung (kein Client-Split). Bestehende Server-Column-Filter + Pagination je Reiter erhalten.

- **WarehouseRequisitions/Index** (eigene Liste): obere Reiter **Lager | Glas** (nur erlaubte).
  Liste nach `Type` gefiltert. „+ Neue Liste" legt Draft im aktiven Typ an (`CreateDraft(type)`).
  Missing-Parts-Alert bleibt.
- **WarehouseRequisitions/Edit**: Typ-Badge im Status-Card. Artikel-Autocomplete ruft
  `/api/articles/search?...&type=<typ>`. Kein Typ-Wechsel im Edit.
- **WarehousePicking/Index** (Lager: Eingehende Listen): neue Reiter **Lager | Glas**, filtert Aufträge
  nach `Type`. Open-Count-Badge je Typ. Beide Reiter für Lager-Rollen sichtbar.
- **MissingPartsLager** (Lager: Fehlteile): **Typ außen** (Lager|Glas), **Fehlteil-Status innen**
  (bestehende Reiter Offene Fehlteile / Wird nicht nachgeliefert). Counts je Kombination.
- **MissingParts** (Werker „Meine Fehlteile"): gleiche zweistufige Struktur; äußere Reiter nur für erlaubte
  Typen des Users.
- **_Layout „Bestellungen"-Dropdown**: Gating um `canOrderGlas` ergänzt (Dropdown sichtbar wenn
  `bestellungenAktiv && (canOrderLager || canOrderGlas || canProcessLager)`). Menü-Einträge bleiben
  einzeln; Typ steckt in den Reitern.

---

## 9. AppSettings (neu, 3 Stück)

| Key | Default | Beschreibung |
|-----|---------|-------------|
| `DefaultGlasbestellempfaengerId` | (leer) | OrderRecipientGroup-ID für Glas-Bestellungen (leer = Submit blockt) |
| `GlasArtikelgruppen` | (leer) | Kommaseparierte Artikelgruppen, die zu Glas gehören |
| `GemeinsameArtikelgruppen` | `EUZ` | Kommaseparierte Artikelgruppen, die in BEIDEN Listen verfügbar sind |

Seed-Muster wie bestehend (AppSettingKeys-Konstante + Tuple in Program.cs-Seed-Loop + FreshInstall-Insert).

---

## 10. Tests

- **`GlasArticleGroupFilter`** (Kern-TDD): Lager/Glas × in/aus glasGroups × sharedGroups(EUZ) × NULL/leer ×
  Case/Trim/`" - "`-Normalisierung.
- **Controller**: `CreateDraft(type)` setzt Type; `Submit` wählt korrekte Empfänger-Gruppe je Typ
  (Lager vs Glas AppSetting, Block bei leer); `Index(type)` filtert korrekt; Reiter-Sichtbarkeit
  (Fake-User mit je nur einer Rolle).
- **AddItem**: nicht erlaubte Gruppe → BadRequest; erlaubte → OK; EUZ in beiden.
- **Zugriff**: reiner `glasbestellung`-User kommt in WarehouseRequisitions/MissingParts rein; sieht nur
  Glas-Reiter.
- InMemory-Hinweise beachten (kein rowversion → TestApplicationDbContext; UNIQUE-Indizes nicht enforced).

---

## 11. Doku-Checkliste

- [ ] `AppVersion.cs` (Web + Service) → v1.25.0
- [ ] `Views/Help/Changelog.cshtml` — v1.25.0-Card
- [ ] `Views/Help/*` — Hilfeseite-Details (Glas-Bestellung, Rollen, AppSettings, Artikelgruppen-Config)
- [ ] `CLAUDE.md` — Rollen-Tabelle (`glasbestellung`), Zugriffsschutz-Tabelle, AppSettings-Tabelle (+3),
      Fallstricke (Typ-Enforcement, EUZ-Ausnahme, Artikelgruppen-Normalisierung)
- [ ] `PROJECT_STATUS.md`
- [ ] `docs/TESTSZENARIEN.md` — neues Kapitel (Anlage Lager/Glas, Artikelgruppen-Trennung, EUZ in beiden,
      Empfänger je Typ, Reiter-Sichtbarkeit je Rolle, Fehlteile zweistufige Reiter)
- [ ] `SQL/00_FreshInstall.sql` (Spalte + History)
- [ ] Migration 77 + `SQL/77_*.sql`
- [ ] `Views/Users/RoleOverview.cshtml`

---

## 12. Risiken / Fallstricke

- **Artikelgruppen-Format**: `Article.ArticleGroup` speichert Code (`"940"`, `"EUZ"`); Config muss dieselben
  Codes tragen. Normalisierung im Helper deckt Trim/Case/`" - "` ab. Bei Abnahme mit echten Glas-Codes prüfen.
- **Rollen-Seed**: neue Rolle muss in bestehende DBs kommen (Seed idempotent). Deploy-Hinweis in Changelog.
- **Backward-Compat**: bestehende Aufträge = Lager (Default 1); heutige Lager-User bekommen zusätzlich den
  Glas-Reiter (nur sichtbar, kein Zwang).
- **Migration-Sync**: FreshInstall + History zwingend synchron (sonst Start-Fehler).

---

## 13. Offene Punkte

Keine blockierenden. Beim Plan zu verifizieren: exakte Role-Seed-Stelle, Signatur von
`/api/articles/search` (Query-Param ergänzen), `CreateDraft`-Signatur.
