# Einbuchung: Automatische Lagerplatz-Übernahme bei bekannter FA — Design Spec

**Datum:** 2026-05-11
**Branch:** `feature/sage-lagerbestand-sync` (Phase-2-Bundle, weiter gefüllt)
**Status:** Approved → Plan
**Phase:** UX-Verbesserung Einbuchung. **Kein AppVersion-Bump** — Branch released im Bundle als v1.10.0; Feature wird in der bestehenden v1.10.0-Changelog-Card mit ergänzt.

---

## 1. Problemstellung

In `Views/StockMovements/Inbound.cshtml` muss der Benutzer aktuell Lagerplatz und FA-Auftrag unabhängig voneinander wählen / scannen. Liegt bereits ein Teil mit derselben FA auf einem Lagerplatz oder Wagen, weiß der Einbuchende das im Moment der Buchung typischerweise nicht. Folgen:

- **Doppelte Anlage:** Derselbe FA-Auftrag wird ein zweites Mal auf einem anderen Lagerplatz angelegt → Kommissionierer muss später sammeln, statt einfach mehr Stk auf denselben Platz zu legen.
- **Konsistenz-Lücke:** Wer den ersten Platz nicht kennt, kann ihn auch nicht bewusst wiederverwenden.

## 2. Ziele

1. **Anzeigen, wo die FA bereits liegt** — sofort beim Befüllen des `Fertigungsauftrag`-Felds (egal ob via QR-Scan, manuelle Eingabe oder Server-Rerender bei Validation-Fehler).
2. **Auto-Übernahme des Lagerplatzes** — falls die FA an genau **einem** Lagerplatz mit positivem Bestand liegt **und** der User noch keinen Lagerplatz manuell gewählt hat.
3. **Defensive Hinweise** in Sonderfällen (mehrere Plätze, User-Vorauswahl, Lagerplatz nicht im Dropdown).

## 3. Out-of-Scope

- **Outbound / Transfer / LocationTransfer** — andere Buchungsarten bekommen den Hint **nicht** in dieser Iteration. Ausbuchung kennt den Lagerplatz typischerweise schon (Bestand wird gewählt). Bei Bedarf später nachziehen.
- **Picker-Wahl per FA-Hint gating** — der Hint ist informativ, nicht hart-gating. Submit bleibt zulässig auch bei abweichender Wahl.
- **Server-Validierung gegen FA-Duplikat** — keine BadRequest-Response, wenn User trotz Hint auf anderen Platz bucht. Bewusster Verzicht: Workflow soll fluide bleiben, Konflikte sind selten und der Hint reicht.

## 4. Datenquelle / API-Wiederverwendung

Endpoint existiert: [`StockApiController.GetStockByOrder`](IdealAkeWms/Controllers/StockApiController.cs#L19) → [`StockMovementRepository.GetStockByProductionOrderAsync`](IdealAkeWms/Data/Repositories/StockMovementRepository.cs#L172). Wird **erweitert** in dieser Iteration:

- **Response-Shape additiv erweitert** um `unit`-Feld. Bestehende Konsumer (Oseon) bleiben kompatibel (neue Property wird einfach ignoriert).

**Response (neu, abwärts-kompatibel):**
```json
[
  { "articleNumber": "87015207", "description": "...", "storageLocation": "K-WAGEN-03", "quantity": 5, "unit": "Stk" }
]
```

Repo aggregiert über alle MovementTypes (Ein/Aus/Um, Sage-Korrekturen) und liefert pro (Article, Location)-Paar den Netto-Bestand für die FA. `IsPickingTransport`-Wagen sind enthalten (gewünscht — die FA kann auch auf einem Kommissionierwagen liegen).

**Vorhandene Aufrufer:** `Views/Tracking/OseonIndex.cshtml`. Heute also nur Tracking-/Picking-Rollen.

### 4.1 Bekannte Repo-Limitation (akzeptiert für v1)

`GetStockByProductionOrderAsync` nutzt `sm.ProductionOrder.Contains(productionOrder)` — bei sehr kurzen FA-Nummern (z. B. 3-stellige Test-FAs) sind False-Positives möglich. **Akzeptiert für v1**, weil produktive FA-Nummern ≥ 6 Stellen haben und das JS Komma-Suffixe schon vor dem API-Call abtrennt. Falls in der Praxis Probleme auftreten: Follow-up via API-Flag `?exact=true`.

### 4.2 Permission-Lücke

Bestehende Auth: `[RequirePickingOrTrackingAccess]` (Picking ∨ Tracking).

Einbuchung selbst (`StockMovementsController.Inbound`) hat aber `[RequireStockAccess]` (admin ∨ stock ∨ stock_keyuser ∨ picking) — siehe `CLAUDE.md` *Zugriffsschutz*. Ein User mit **nur** `stock`- oder `stock_keyuser`-Rolle würde den AJAX-Call mit 302→AccessDenied beantwortet bekommen → silent fail (kein Hint sichtbar).

**Lösung:** Neuer Composite-Filter `RequireStockOrPickingOrTrackingAccess`, der `CanPickAsync ∨ CanAccessStockAsync ∨ CanViewTrackingAsync` zulässt. `StockApiController` wechselt auf diesen Filter. Bestehende Aufrufer (OSEON) sind unbeeinträchtigt — die Menge der zugelassenen Rollen wird **erweitert**, nicht eingeschränkt.

## 5. Logik (Frontend)

### 5.1 Trigger

Zwei Trigger:

1. **`change`-Event** auf `<input asp-for="ProductionOrder">`. Feuert nach Scanner-Übernahme (siehe [`barcode-scanner.js:340-347`](IdealAkeWms/wwwroot/js/barcode-scanner.js#L340-L347) — `clearAndFillFaNumber` dispatcht `change`) und nach manueller Eingabe + Blur/Enter.
2. **`DOMContentLoaded`-Initial-Check.** Wenn die View nach einem Server-Render bereits einen FA-Wert hat (z. B. Validation-Error Rerender, oder ModelState pre-fill), wird derselbe Check einmal ausgelöst. Sonst würde der Hint nach einem fehlgeschlagenen Submit fehlen, obwohl die FA gesetzt ist.

Kein `keyup`/`input`-Listener — wir vermeiden Per-Tastendruck-Calls.

### 5.2 Debouncing über Last-Query-Cache

```js
let lastFaQuery = null;
input.addEventListener('change', () => {
  const fa = input.value.trim();
  if (fa === lastFaQuery) return;
  lastFaQuery = fa;
  // ... fetch + render
});
```

`change` feuert bei jedem Blur — auch ohne Wertänderung. Cache verhindert Redundant-Calls. Beim DOM-Ready-Init wird `lastFaQuery` zunächst nicht gesetzt, damit der erste Check tatsächlich passiert.

### 5.3 Fetch — Cache-Disable

```js
fetch(url, { credentials: 'same-origin', cache: 'no-store' })
```

`cache: 'no-store'` verhindert, dass Browser einen veralteten GET-Response cacht — wichtig, weil der User direkt nach einer eigenen Einbuchung sofort eine zweite machen kann und dann den aktuellen Stand sehen muss.

### 5.4 Render-Entscheidungsbaum

1. **FA leer** → Hint ausblenden, return.
2. **Fetch** `/api/stock/by-order/{encoded-fa}`.
   - HTTP error / non-2xx → Hint ausblenden, return (silent — kein UX-Block).
3. **Filter** Response auf `quantity > 0`. Items mit `quantity <= 0` sind komplett ausgebuchte FAs ohne aktuellen Lagerort.
4. **`items.length === 0`** → Hint ausblenden, return (neue / komplett ausgebuchte FA — kein Konflikt).
5. **`distinctLocations.length === 1`** AND **`StorageLocationId` Dropdown leer**:
   - Auto-Fill via Option-Text-Match (siehe 5.5).
   - Hint (`alert-info`): Lagerplatz wurde übernommen.
6. **`distinctLocations.length === 1`** AND **`StorageLocationId` Dropdown bereits gesetzt**:
   - Kein Overwrite.
   - Hint (`alert-info`): "FA liegt auf {X} — deine Auswahl ({Y}) bleibt."
7. **`distinctLocations.length > 1`**:
   - Kein Auto-Fill.
   - Hint (`alert-warning`): Liste aller Plätze mit Menge + Einheit + Artikel.

### 5.5 Auto-Fill-Mechanik

`<select id="StorageLocationId">` ist ein nativer `<select>` (kein Select2 — siehe `Inbound.cshtml:46-49`). Options sind via `new SelectList(Model.StorageLocations, "Id", "Code")` befüllt → `option.text === StorageLocation.Code`.

Auto-Fill iteriert über `select.options`, matched per **exakt** `text.trim() === apiLocationCode` und setzt `select.value = option.value`. Danach `select.dispatchEvent(new Event('change'))` für DOM-Konsistenz (kein direkter Konsumer aktuell, defensiv).

**Edge:** Match-Failure (z. B. der Lagerplatz aus dem API-Response ist nicht im Dropdown, weil `IsActive=false` oder `IstBuchbar=false`) → kein Auto-Fill, Hint zeigt `alert-warning` mit Vermerk "Lagerplatz nicht im Buchungs-Dropdown (evtl. inaktiv oder nicht buchbar)".

### 5.6 Hint-UI

Position: direkt unter dem `ProductionOrder`-Block, vor dem optionalen Bedarfsmeldungs-Container, vor dem Submit-Button. Bootstrap-Alert `alert-info` (blau) für Auto-Fill / Single-Hit-Info, `alert-warning` (gelb) bei Multi-Location oder Lagerplatz-nicht-im-Dropdown.

```html
<div id="faStorageHint" class="alert" style="display:none;" role="status" aria-live="polite"></div>
```

**Content-Aufbau (Beispiel `alert-info` mit Auto-Fill):**
```
FA 112233 liegt bereits:
  • Lagerplatz K-WAGEN-03 — 5,000 Stk Artikel 87015207
  • Lagerplatz K-WAGEN-03 — 3,000 Stk Artikel 87015208
→ Lagerplatz wurde übernommen.
```

Mengen via `Intl.NumberFormat('de-AT', { maximumFractionDigits: 3 })`. Einheit aus API-Field `unit` (Fallback "Stk" wenn null/leer).

### 5.7 HTML-Escape

Alle dynamischen Strings (articleNumber, storageLocation, description, unit, FA-Wert) durch `escapeHtml`-Helper (textContent-Trick) — schützt vor versehentlicher Cross-Site-Scripting-Lücke via API-Daten oder Eingabe.

## 6. Edge Cases

| Fall | Verhalten |
|---|---|
| FA-Feld leer geräumt | Hint blendet aus, `lastFaQuery = ''`. |
| FA per Substring-Match (z. B. "123" matcht "1234") | API-Repo nutzt `Contains` → kann Falsch-Treffer geben. Hint zeigt dann mehrere Plätze, Auto-Fill fällt deaktiviert aus. Bewusst akzeptiert für v1 (siehe 4.1). |
| Validation-Error-Rerender (FA pre-filled, Hint braucht) | DOM-Ready-Init triggert den Check einmal. |
| Lagerplatz aus API ist `IsPickingTransport`-Wagen | Behandelt wie normaler Platz. Wagen sind Teil der FA-Aufenthaltsorte. |
| Lagerplatz nicht im Dropdown (inaktiv / nicht buchbar) | Hint zeigt Platz als `alert-warning` an, Auto-Fill schlägt fehl → Hint vermerkt das. |
| User scannt zuerst Lagerplatz, dann FA-Artikel | Beim FA-`change` ist `StorageLocationId` schon gesetzt → kein Overwrite, Hint zeigt Status. |
| Mehrere Artikel auf derselben FA auf demselben Platz | API liefert pro (Article, Location) eine Zeile → mehrere Items, aber 1 distinct Location → Auto-Fill OK, Hint listet alle Artikel. |
| Network-Error / 5xx | `.catch(...)` blendet Hint aus, Console-Log via `console.warn`. Submit bleibt funktional. |
| Concurrent `change`-Event (z. B. Scan während alter Request läuft) | Generation-Counter `requestSeq` — jeder Request hat eine inkrementelle ID, Render prüft `id === currentId`. Älterer Response wird verworfen. |
| Browser-Cache liefert Stale-Daten nach eigener Buchung | `cache: 'no-store'` auf fetch. |
| Artikel hat ungewöhnliche Einheit (m, l, kg) | API liefert `unit`; falls null → Fallback "Stk". |

## 7. Datenmodell / DB-Änderungen

**Keine DB-Änderungen.** Repo + Endpoint existieren. Änderungen:
- Neuer Auth-Filter (~30 Zeilen)
- `StockApiController` mapped zusätzlich `Unit` ins Response (additiv, kompatibel)
- Frontend-Code in `Inbound.cshtml` (~100 Zeilen JS)

## 8. Tests

**Backend:**
- Optional: Unit-Test für `RequireStockOrPickingOrTrackingAccessFilter` (Pattern wie bestehende Filter-Tests, falls vorhanden).
- Bestehende API-Tests von `StockApiController` (falls vorhanden) bleiben grün — Response-Shape ist additiv erweitert.

**Frontend:**
- Keine JS-Tests (Projekt hat keine Infra). Abgedeckt via manuelle TESTSZENARIEN — siehe Sektion 9.

## 9. Manuelle Test-Szenarien (für `docs/TESTSZENARIEN.md`)

Eingeordnet unter Sektion 2 "Einbuchung". Exakte TS-Nummerierung im Plan-Schritt an bestehende Konvention anpassen.

### TS-2.x-A — FA neu (keine bestehende Buchung)
**Vorbedingung:** FA-Nummer ohne aktuellen Bestand im WMS.
**Schritte:** Einbuchung öffnen → Artikel wählen → FA `99999` eingeben → Tab.
**Erwartet:** Kein Hinweis-Alert. Lagerplatz-Dropdown bleibt unverändert.

### TS-2.x-B — FA mit genau 1 Lagerplatz, Storage-Dropdown leer
**Vorbedingung:** FA `12345` liegt aktuell mit positiver Menge auf `K-WAGEN-03` (5 Stk).
**Schritte:** Einbuchung öffnen → Artikel wählen → FA `12345` eingeben → Tab.
**Erwartet:**
- Lagerplatz-Dropdown wird auf `K-WAGEN-03` gesetzt.
- Blaues Info-Alert: "FA `12345` liegt bereits: Lagerplatz **K-WAGEN-03** — 5,000 Stk Artikel ..." mit Footer "→ Lagerplatz wurde übernommen."
- Submit-Button bleibt aktiv.

### TS-2.x-C — FA mit 1 Lagerplatz, Storage-Dropdown bereits gewählt
**Vorbedingung:** Wie TS-2.x-B, aber User wählt zuerst Lagerplatz `K-LAGER-05` manuell.
**Schritte:** Einbuchung öffnen → Artikel → Lagerplatz `K-LAGER-05` wählen → FA `12345` → Tab.
**Erwartet:**
- Lagerplatz bleibt `K-LAGER-05` (kein Overwrite).
- Blaues Info-Alert mit Footer "→ Deine Auswahl **K-LAGER-05** bleibt."

### TS-2.x-D — FA auf mehreren Lagerplätzen
**Vorbedingung:** FA `12345` liegt auf `K-WAGEN-03` (5 Stk) und `K-LAGER-05` (3 Stk).
**Schritte:** Einbuchung öffnen → FA `12345` → Tab.
**Erwartet:**
- Lagerplatz-Dropdown bleibt leer.
- Gelbes Warning-Alert mit Liste beider Plätze + Mengen, Footer "→ Bitte gezielt buchen."

### TS-2.x-E — QR-Scan triggert Hint
**Vorbedingung:** QR-Code mit FA-Suffix aktiviert (`QrMitFaNummer = true`), FA aus QR liegt bereits auf 1 Platz.
**Schritte:** Einbuchung öffnen → Artikel-QR scannen (mit FA-Teil).
**Erwartet:** Artikel + FA werden gefüllt, danach Hint erscheint mit Auto-Fill wie TS-2.x-B.

### TS-2.x-F — FA-Feld leeren
**Vorbedingung:** Nach TS-2.x-B (Hint sichtbar, Auto-Fill aktiv).
**Schritte:** FA-Feld leeren → Tab.
**Erwartet:** Hint verschwindet. Lagerplatz-Dropdown-Wert bleibt (User-Wahl wird nicht zurückgesetzt — defensive).

### TS-2.x-G — Lagerplatz aus API nicht im Dropdown (inaktiv)
**Vorbedingung:** FA `12345` liegt historisch auf `OLD-PLATZ`, der inzwischen `IsActive=false` ist.
**Schritte:** Einbuchung öffnen → FA `12345` → Tab.
**Erwartet:**
- Gelbes Warning-Alert listet `OLD-PLATZ` mit Vermerk "Lagerplatz nicht im Buchungs-Dropdown (evtl. inaktiv oder nicht buchbar)".
- Kein Auto-Fill. Dropdown bleibt leer.

### TS-2.x-H — Permission: stock-only User
**Vorbedingung:** User mit Rolle `stock` (NICHT `picking`, NICHT `tracking`). FA `12345` mit Bestand vorhanden.
**Schritte:** Login als `stock`-User → Einbuchung → FA `12345` → Tab.
**Erwartet:** Hint erscheint normal (Filter lässt durch). Kein AccessDenied-Redirect im Network-Tab.

### TS-2.x-I — Validation-Error-Rerender zeigt Hint
**Vorbedingung:** FA `12345` mit Bestand.
**Schritte:** Einbuchung öffnen → FA `12345` eingeben → Tab (Hint zeigt mit Auto-Fill) → Menge leeren → "Einbuchung speichern".
**Erwartet:**
- Server rendert View neu mit ValidationSummary-Fehler "Menge erforderlich".
- FA-Feld ist mit `12345` vorbefüllt.
- Hint-Alert wird beim Page-Load wieder angezeigt (DOM-Ready-Init feuert den Check).

### TS-2.x-J — Artikel mit Sondereinheit
**Vorbedingung:** Artikel mit `Unit = "m"` (Meter) — z. B. Stahlband. FA liegt auf 1 Platz.
**Schritte:** Einbuchung öffnen → FA eingeben → Tab.
**Erwartet:** Hint zeigt "... — 5,000 **m** Artikel ..." (nicht "Stk").

## 10. Risiken

### 10.1 Permission-Erweiterung exponiert Daten

Neuer Filter lässt `stock`-User auf den Endpoint zu. Endpoint liefert: ArticleNumber, Description, StorageLocationCode, Quantity, Unit pro FA. `stock`-User darf das laut Rollen-Konzept ohnehin sehen (Bestandsübersicht, Bewegungshistorie). Kein Vertraulichkeits-Bruch.

### 10.2 Substring-Match-Limitation (siehe 4.1)

Akzeptiert für v1. Risiko: niedrig, weil produktive FAs ≥ 6 Stellen.

### 10.3 Auto-Fill widerspricht User-Intent

Mögliche Frustration: "Ich wollte bewusst auf neuen Platz — warum wurde der überschrieben?" Mitigation: User-Intent-Check (StorageLocationId schon gewählt → kein Overwrite, siehe 5.4). Bei leerem Dropdown ist Auto-Fill der hilfreichste Default.

### 10.4 Stale Response (Race-Condition)

Generation-Counter (`requestSeq`) verhindert, dass ein verspäteter alter Response einen neueren überschreibt.

### 10.5 N+1-Last bei rapidem FA-Tippen

`change` feuert nur bei Blur. `lastFaQuery`-Cache verhindert Redundant-Calls bei Identical-Werten. Pro distinct-Wert: 1 Request. Akzeptabel.

### 10.6 Browser-Cache-Stale

Mitigation: `cache: 'no-store'` auf fetch.

## 11. Ablauf

1. Plan-Doc schreiben: `docs/superpowers/plans/2026-05-11-einbuchung-fa-autofill.md`.
2. Plan mit User abstimmen.
3. Implementation in 4 Tasks (Filter+API, UI, Doku, Verifikation).
4. Manuelle Verifikation gemäß TS-2.x-A bis J.
5. Bestehenden v1.10.0-Changelog-Eintrag erweitern (keine neue Version-Card).
