# Einbuchung: Automatische Lagerplatz-Übernahme bei bekannter FA — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Beim Befüllen des FA-Felds in der Einbuchung erscheint ein Hinweis, ob die FA bereits auf einem Lagerplatz oder Wagen liegt. Bei genau einem Platz und leerem Lagerplatz-Dropdown wird der Platz automatisch übernommen. Quelle: bestehender Endpoint `GET /api/stock/by-order/{orderNumber}` (additiv um `unit` erweitert).

**Architecture:** Drei Schichten:
- **Permission** — Neuer Composite-Filter `RequireStockOrPickingOrTrackingAccessAttribute`, der `stock`/`stock_keyuser`-Rollen zusätzlich zur API zulässt.
- **API** — `StockApiController` wechselt auf den neuen Filter; Response wird additiv um `unit` erweitert.
- **View / Inline-JS** — `Inbound.cshtml` bekommt einen Hint-Container und einen IIFE-JS-Block mit `change`-Listener auf das `ProductionOrder`-Input, einem DOM-Ready-Initial-Check, `cache: 'no-store'` auf fetch und Race-Generation-Counter.

**Branch:** `feature/sage-lagerbestand-sync` (Phase-2-Bundle, weiter angereichert).
**Version:** **Bleibt v1.10.0** — Bundle-Release. Changelog-Card v1.10.0 wird erweitert, keine neue Version.

**Spec:** `docs/superpowers/specs/2026-05-11-einbuchung-fa-autofill-design.md`.

**Commit-Konvention:** `feat(stockmovements): ...` / `feat(filters): ...` / `docs: ...`. Co-Authored-By trailer.

**Files:**
- New: `IdealAkeWms/Filters/RequireStockOrPickingOrTrackingAccessAttribute.cs`
- Modify: `IdealAkeWms/Controllers/StockApiController.cs` (Permission + Response um `unit`)
- Modify: `IdealAkeWms/Views/StockMovements/Inbound.cshtml`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` (bestehende v1.10.0-Card erweitern)
- Modify: `docs/TESTSZENARIEN.md`

---

## Task 1: Permission-Filter + API-Anpassung + Unit-Mapping

**Files:**
- New: `IdealAkeWms/Filters/RequireStockOrPickingOrTrackingAccessAttribute.cs`
- Modify: `IdealAkeWms/Controllers/StockApiController.cs`

Composite-Filter `picking ∨ stock ∨ tracking`. API-Controller wechselt. Response-Shape additiv um `unit` erweitert.

- [ ] **Step 1: Filter-Datei anlegen**

`IdealAkeWms/Filters/RequireStockOrPickingOrTrackingAccessAttribute.cs`:

```csharp
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdealAkeWms.Filters;

public class RequireStockOrPickingOrTrackingAccessAttribute : TypeFilterAttribute
{
    public RequireStockOrPickingOrTrackingAccessAttribute()
        : base(typeof(RequireStockOrPickingOrTrackingAccessFilter)) { }
}

public class RequireStockOrPickingOrTrackingAccessFilter : IAsyncActionFilter
{
    private readonly ICurrentUserService _currentUserService;

    public RequireStockOrPickingOrTrackingAccessFilter(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!await _currentUserService.CanPickAsync()
            && !await _currentUserService.CanAccessStockAsync()
            && !await _currentUserService.CanViewTrackingAsync())
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
```

- [ ] **Step 2: StockApiController umschalten + `unit` ins Response**

In `IdealAkeWms/Controllers/StockApiController.cs`:

1. Attribute-Tausch:
```csharp
[RequirePickingOrTrackingAccess]   // ALT
[RequireStockOrPickingOrTrackingAccess]  // NEU
```

2. Response-Projektion erweitern:
```csharp
return Ok(items.Select(i => new
{
    articleNumber = i.ArticleNumber,
    description = i.ArticleDescription ?? "",
    storageLocation = i.StorageLocationCode,
    quantity = i.CurrentQuantity,
    unit = i.Unit ?? ""    // NEU
}));
```

- [ ] **Step 3: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: 0 errors, alle bestehenden Tests grün. Bestehende OSEON-Tests bleiben kompatibel (neue `unit`-Property wird im OSEON-JS ignoriert).

- [ ] **Step 4: Commit**

```pwsh
git add IdealAkeWms/Filters/RequireStockOrPickingOrTrackingAccessAttribute.cs IdealAkeWms/Controllers/StockApiController.cs
git commit -m "feat(filters): add stock-or-picking-or-tracking composite filter; expose unit on by-order api" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Inbound-View — Hint-Container + Inline-JS

**Files:**
- Modify: `IdealAkeWms/Views/StockMovements/Inbound.cshtml`

Hint-Alert direkt nach dem ProductionOrder-Block einfügen. IIFE-JS-Block am Ende von `@section Scripts` ergänzen — mit DOM-Ready-Initial-Check, `cache: 'no-store'`, Race-Counter, Unit-Display.

- [ ] **Step 1: Hint-Container nach ProductionOrder einfügen**

In `IdealAkeWms/Views/StockMovements/Inbound.cshtml`, direkt nach dem ProductionOrder-`<div class="mb-3">`-Block (nach Zeile 64), VOR dem `BestellungenAktiv`-Block, einfügen:

```cshtml
<div id="faStorageHint" class="alert" style="display: none;" role="status" aria-live="polite"></div>
```

- [ ] **Step 2: Inline-JS-Block in `@section Scripts` ergänzen**

In `Inbound.cshtml`, im `@section Scripts`-Block, NACH dem bestehenden `if (ViewBag.BestellungenAktiv == true)`-Block (ganz am Ende des Sections vor dem schließenden `}` der `@section`), folgenden Block einfügen:

```cshtml
<script>
    (function () {
        var faInput = document.getElementById('ProductionOrder');
        var storageSelect = document.getElementById('StorageLocationId');
        var hint = document.getElementById('faStorageHint');
        if (!faInput || !storageSelect || !hint) return;

        var lastFaQuery = null;
        var requestSeq = 0;
        var deFormatter = new Intl.NumberFormat('de-AT', { maximumFractionDigits: 3 });

        function escapeHtml(s) {
            var div = document.createElement('div');
            div.textContent = (s === null || s === undefined) ? '' : String(s);
            return div.innerHTML;
        }

        function findOptionByText(select, text) {
            for (var i = 0; i < select.options.length; i++) {
                if (select.options[i].text.trim() === text) return select.options[i];
            }
            return null;
        }

        function renderHint(htmlBody, kind) {
            hint.className = 'alert ' + (kind === 'warn' ? 'alert-warning' : 'alert-info');
            hint.innerHTML = htmlBody;
            hint.style.display = 'block';
        }

        function hideHint() {
            hint.style.display = 'none';
            hint.innerHTML = '';
        }

        function buildItemsList(items) {
            var html = '<ul class="mb-0">';
            items.forEach(function (i) {
                var unit = i.unit && i.unit.trim() !== '' ? i.unit : 'Stk';
                html += '<li>Lagerplatz <strong>' + escapeHtml(i.storageLocation) + '</strong> &mdash; '
                     + escapeHtml(deFormatter.format(i.quantity)) + ' ' + escapeHtml(unit)
                     + ' Artikel ' + escapeHtml(i.articleNumber) + '</li>';
            });
            html += '</ul>';
            return html;
        }

        function handleResponse(fa, items) {
            items = items.filter(function (i) { return i.quantity > 0; });
            if (items.length === 0) {
                hideHint();
                return;
            }

            var distinctLocs = [];
            items.forEach(function (i) {
                if (distinctLocs.indexOf(i.storageLocation) === -1) {
                    distinctLocs.push(i.storageLocation);
                }
            });

            var userPickedLocation = !!storageSelect.value;
            var faSafe = escapeHtml(fa);
            var listHtml = buildItemsList(items);

            if (distinctLocs.length === 1) {
                var loc = distinctLocs[0];
                if (userPickedLocation) {
                    var currentText = storageSelect.options[storageSelect.selectedIndex]
                        ? storageSelect.options[storageSelect.selectedIndex].text.trim()
                        : '';
                    renderHint(
                        '<strong>FA <code>' + faSafe + '</code> liegt bereits:</strong>' + listHtml
                        + '<p class="mb-0 mt-2"><em>&rarr; Deine Auswahl <strong>'
                        + escapeHtml(currentText) + '</strong> bleibt.</em></p>',
                        'info'
                    );
                } else {
                    var option = findOptionByText(storageSelect, loc);
                    if (option) {
                        storageSelect.value = option.value;
                        storageSelect.dispatchEvent(new Event('change'));
                        renderHint(
                            '<strong>FA <code>' + faSafe + '</code> liegt bereits:</strong>' + listHtml
                            + '<p class="mb-0 mt-2"><em>&rarr; Lagerplatz wurde &uuml;bernommen.</em></p>',
                            'info'
                        );
                    } else {
                        renderHint(
                            '<strong>FA <code>' + faSafe + '</code> liegt bereits:</strong>' + listHtml
                            + '<p class="mb-0 mt-2"><em>&rarr; Lagerplatz nicht im Buchungs-Dropdown '
                            + '(evtl. inaktiv oder nicht buchbar). Bitte manuell w&auml;hlen.</em></p>',
                            'warn'
                        );
                    }
                }
            } else {
                renderHint(
                    '<strong>FA <code>' + faSafe + '</code> liegt auf mehreren Lagerpl&auml;tzen:</strong>'
                    + listHtml
                    + '<p class="mb-0 mt-2"><em>&rarr; Bitte gezielt buchen.</em></p>',
                    'warn'
                );
            }
        }

        function checkFa(fa) {
            if (fa === lastFaQuery) return;
            lastFaQuery = fa;

            if (!fa) {
                hideHint();
                return;
            }

            var seq = ++requestSeq;
            fetch('/api/stock/by-order/' + encodeURIComponent(fa), {
                credentials: 'same-origin',
                cache: 'no-store'
            })
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (items) {
                    if (seq !== requestSeq) return; // älterer Response — verwerfen
                    handleResponse(fa, items);
                })
                .catch(function () {
                    if (seq !== requestSeq) return;
                    hideHint();
                });
        }

        faInput.addEventListener('change', function () {
            checkFa(faInput.value.trim());
        });

        // DOM-Ready-Initial-Check: bei Validation-Error-Rerender kann FA bereits gesetzt sein.
        // change feuert nicht beim Page-Load, daher hier einmal manuell prüfen.
        if (faInput.value && faInput.value.trim() !== '') {
            checkFa(faInput.value.trim());
        }
    })();
</script>
```

WICHTIG:
- Block muss INNERHALB von `@section Scripts { ... }` stehen — sonst lädt der Browser ihn nicht (Razor-Section).
- IIFE-Wrapper hält den Scope sauber.
- DOM-Ready-Initial-Check läuft am Ende der IIFE — zu diesem Zeitpunkt ist das Script-Tag am Ende der Page und das DOM ist bereits parsed (Scripts laufen sequenziell, das Form-Markup steht weiter oben).
- `requestSeq` verhindert, dass ein verspäteter alter Response einen neueren überschreibt.

- [ ] **Step 3: Build verifizieren**

```pwsh
dotnet build IdealAkeWms/IdealAkeWms.csproj --nologo
```

Expected: 0 errors.

- [ ] **Step 4: Manuelles Smoke-Test (vor Commit)**

Web-App starten:
1. Einbuchung 1: Artikel + FA `12345` + Lagerplatz `K-WAGEN-03` + Menge 5 → Submit.
2. Einbuchung 2 öffnen → FA `12345` eingeben → Tab → Hint mit Auto-Fill auf K-WAGEN-03 erscheint.
3. FA leeren → Hint verschwindet.
4. FA `99999` (neu) eingeben → kein Hint.
5. Submit ohne Menge → Validation-Fehler → Page rendert neu → FA noch gesetzt → Hint erscheint wieder (DOM-Ready-Init).

Falls Smoke-Test fehlschlägt: nicht committen, debuggen.

- [ ] **Step 5: Commit**

```pwsh
git add IdealAkeWms/Views/StockMovements/Inbound.cshtml
git commit -m "feat(stockmovements): show fa-storage hint and auto-fill location on inbound" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Changelog-Erweiterung + Hilfeseite

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` (bestehende v1.10.0-Card erweitern)
- Modify: `IdealAkeWms/Views/Help/Index.cshtml`

**Kein AppVersion-Bump** — Branch released im Bundle als v1.10.0.

- [ ] **Step 1: Changelog v1.10.0 erweitern**

In `IdealAkeWms/Views/Help/Changelog.cshtml`, im **bestehenden** v1.10.0-Card-Block, an die `<ul>` zwei neue `<li>`-Einträge anhängen (vor dem schließenden `</ul>`):

```cshtml
<li><strong>Einbuchung &ndash; FA-Lagerplatz-Hinweis:</strong>
    Beim Bef&uuml;llen des Fertigungsauftrag-Felds (Scan oder manuell) pr&uuml;ft die Einbuchung automatisch,
    ob die FA bereits auf einem Lagerplatz oder Wagen liegt.
    Bei genau einem Treffer wird der Lagerplatz automatisch &uuml;bernommen (falls noch keiner gew&auml;hlt wurde).
    Bei mehreren Treffern erscheint eine gelbe Warnung mit allen Pl&auml;tzen.
    Verhindert doppelte Anlage derselben FA auf verschiedenen Lagerpl&auml;tzen.</li>
<li><strong>Zugriffsrecht erweitert:</strong>
    Der Stock-by-Order-API-Endpunkt steht nun auch Stock-/Stock-Keyuser-Rollen offen
    (vorher nur Picking + Tracking) und liefert zus&auml;tzlich die Mengeneinheit.</li>
```

- [ ] **Step 2: Hilfeseite Einbuchung erweitern**

In `IdealAkeWms/Views/Help/Index.cshtml`, im "Lager"-Card, den `<dd>` zu "Einbuchung" (Zeile 14-16) erweitern:

```cshtml
<dt>Einbuchung</dt>
<dd>
    Artikel auf einen Lagerplatz einbuchen. Artikel und Lagerplatz koennen per Barcode/QR-Code gescannt
    oder manuell ausgewaehlt werden.
    <ul class="mt-1 mb-1">
        <li>Sind offene Bedarfsmeldungen zum eingebuchten Artikel vorhanden, werden diese angezeigt
            und koennen mit der Buchung verknuepft werden.</li>
        <li><strong>FA-Lagerplatz-Hinweis:</strong> Sobald die FA-Nummer eingegeben oder gescannt wird,
            prueft die App, ob diese FA bereits auf einem Lagerplatz oder Wagen liegt.
            Liegt sie an genau einem Platz und ist der Lagerplatz noch nicht gewaehlt &rarr; Platz wird automatisch uebernommen.
            Liegt sie an mehreren Plaetzen &rarr; gelbe Warnung mit Mengen; bitte gezielt buchen.
            Liegt sie auf einem inaktiven/nicht buchbaren Platz &rarr; gelbe Warnung mit Hinweis,
            manuell zu waehlen.</li>
    </ul>
</dd>
```

- [ ] **Step 3: Build + Tests**

```pwsh
dotnet build --nologo
dotnet test --nologo --no-build
```

Expected: 0 errors, alle Tests grün.

- [ ] **Step 4: Commit**

```pwsh
git add IdealAkeWms/Views/Help/Changelog.cshtml IdealAkeWms/Views/Help/Index.cshtml
git commit -m "docs: extend v1.10.0 changelog and help with fa-storage hint feature" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: TESTSZENARIEN-Update

**Files:**
- Modify: `docs/TESTSZENARIEN.md`

Zehn neue Szenarien (TS-2.x-A bis J) im Sektion "2. Einbuchung" ergänzen.

- [ ] **Step 1: Inhaltsverzeichnis prüfen**

Im Inhaltsverzeichnis von `docs/TESTSZENARIEN.md` die Zeile zur Sektion "2. Einbuchung" um die neuen TS-Nummern erweitern (Pattern wie bei TS-7.6a–d):

```markdown
| 2. Einbuchung | [→](#2-einbuchung) | TS-2.1 – TS-2.n (inkl. TS-2.x-A bis J FA-Lagerplatz-Hinweis) |
```

Exakte Nummerierung an bestehende Konvention anpassen — falls TS-2 bereits TS-2.1 bis TS-2.5 hat, die neuen als TS-2.6 bis TS-2.15 nummerieren oder als TS-2.5a/b/c/... fortlaufend.

- [ ] **Step 2: Szenarien einfügen**

Am Ende von Sektion "2. Einbuchung" (vor dem `---` zum nächsten Sektionen-Header), folgenden Block ergänzen:

```markdown
### TS-2.x-A — FA neu (keine bestehende Buchung)

**Vorbedingungen:**
- FA-Nummer ohne aktuellen Bestand im WMS.

**Schritte:**
1. Einbuchung oeffnen.
2. Artikel auswaehlen.
3. FA-Nummer manuell eingeben → Tab.

**Erwartetes Verhalten:**
- Kein Info-Alert erscheint unter dem FA-Feld.
- Lagerplatz-Dropdown bleibt unveraendert.

---

### TS-2.x-B — FA liegt auf genau 1 Lagerplatz, Storage-Dropdown leer

**Vorbedingungen:**
- FA-Nummer `12345` liegt auf `K-WAGEN-03` mit positiver Menge (5 Stk Artikel 87015207).
- Lagerplatz `K-WAGEN-03` ist aktiv und buchbar.

**Schritte:**
1. Einbuchung oeffnen.
2. Artikel auswaehlen (Lagerplatz NICHT setzen).
3. FA `12345` eingeben oder per QR scannen → Tab.

**Erwartetes Verhalten:**
- Lagerplatz-Dropdown wird automatisch auf `K-WAGEN-03` gesetzt.
- Blaues Info-Alert: "FA `12345` liegt bereits: Lagerplatz **K-WAGEN-03** — 5,000 Stk Artikel 87015207. → Lagerplatz wurde uebernommen."
- Submit-Button bleibt aktiv.

---

### TS-2.x-C — FA mit 1 Lagerplatz, Storage bereits gewaehlt → kein Overwrite

**Vorbedingungen:**
- Wie TS-2.x-B.

**Schritte:**
1. Einbuchung oeffnen.
2. Artikel auswaehlen.
3. Lagerplatz `K-LAGER-05` manuell auswaehlen.
4. FA `12345` eingeben → Tab.

**Erwartetes Verhalten:**
- Lagerplatz bleibt `K-LAGER-05` (kein Overwrite).
- Blaues Info-Alert: Liste mit `K-WAGEN-03` + Hinweis "→ Deine Auswahl **K-LAGER-05** bleibt."

---

### TS-2.x-D — FA auf mehreren Lagerplaetzen → kein Auto-Fill

**Vorbedingungen:**
- FA `12345` liegt auf `K-WAGEN-03` (5 Stk) und `K-LAGER-05` (3 Stk).

**Schritte:**
1. Einbuchung oeffnen.
2. FA `12345` eingeben → Tab (Lagerplatz NICHT gewaehlt).

**Erwartetes Verhalten:**
- Lagerplatz-Dropdown bleibt leer.
- Gelbes Warning-Alert: "FA `12345` liegt auf mehreren Lagerplaetzen: K-WAGEN-03 — 5,000 Stk; K-LAGER-05 — 3,000 Stk. → Bitte gezielt buchen."

---

### TS-2.x-E — QR-Scan triggert Hint

**Vorbedingungen:**
- AppSetting `QrMitFaNummer = true`.
- QR-Code mit FA-Anteil generierbar (Artikelnummer;...;FA-Nummer[,Suffix];...).
- FA aus QR liegt bereits auf genau 1 Platz.

**Schritte:**
1. Einbuchung oeffnen.
2. Artikel-QR scannen.

**Erwartetes Verhalten:**
- Artikel-Dropdown wird gefuellt.
- FA-Feld wird gefuellt.
- Hint erscheint wie in TS-2.x-B mit Auto-Fill des Lagerplatzes.

---

### TS-2.x-F — FA-Feld leeren

**Vorbedingungen:**
- Nach TS-2.x-B (Hint sichtbar, Auto-Fill aktiv auf `K-WAGEN-03`).

**Schritte:**
1. FA-Feld leeren (alle Zeichen entfernen) → Tab.

**Erwartetes Verhalten:**
- Hint verschwindet.
- Lagerplatz-Dropdown behaelt `K-WAGEN-03` (Auswahl wird nicht zurueckgesetzt — defensive).

---

### TS-2.x-G — Lagerplatz aus API nicht im Dropdown (inaktiv)

**Vorbedingungen:**
- FA `12345` liegt auf Lagerplatz `OLD-PLATZ`.
- `OLD-PLATZ` ist `IsActive = false` ODER `IstBuchbar = false` → nicht im Buchungs-Dropdown.

**Schritte:**
1. Einbuchung oeffnen.
2. FA `12345` eingeben → Tab.

**Erwartetes Verhalten:**
- Gelbes Warning-Alert: "FA `12345` liegt bereits: Lagerplatz **OLD-PLATZ** — X Stk Artikel ... → Lagerplatz nicht im Buchungs-Dropdown (evtl. inaktiv oder nicht buchbar). Bitte manuell waehlen."
- Kein Auto-Fill. Dropdown bleibt leer.

---

### TS-2.x-H — Permission: stock-only User

**Vorbedingungen:**
- User mit Rolle `stock` (KEINE `picking`, KEINE `tracking`).
- FA `12345` mit Bestand vorhanden.

**Schritte:**
1. Login als `stock`-User.
2. Einbuchung oeffnen.
3. FA `12345` eingeben → Tab.

**Erwartetes Verhalten:**
- Hint erscheint normal (kein 302/AccessDenied im Network-Tab).
- Auto-Fill funktioniert wie in TS-2.x-B.

---

### TS-2.x-I — Validation-Error-Rerender zeigt Hint

**Vorbedingungen:**
- FA `12345` mit Bestand auf `K-WAGEN-03`.

**Schritte:**
1. Einbuchung oeffnen.
2. Artikel waehlen.
3. FA `12345` eingeben → Tab → Hint erscheint, Auto-Fill auf K-WAGEN-03.
4. Menge-Feld leer lassen.
5. "Einbuchung speichern" klicken.

**Erwartetes Verhalten:**
- Server rendert View neu mit ValidationSummary-Fehler "Menge erforderlich".
- FA-Feld ist mit `12345` vorbefuellt.
- Hint-Alert wird beim Page-Load wieder angezeigt (DOM-Ready-Init triggert den Check).
- Lagerplatz-Dropdown ist nach Rerender weiter auf `K-WAGEN-03` (ModelState-Restore + Auto-Fill).

---

### TS-2.x-J — Artikel mit Sondereinheit

**Vorbedingungen:**
- Artikel mit `Unit = "m"` (z. B. Stahlband-Meterware).
- FA `78901` liegt auf 1 Platz mit 12,5 m.

**Schritte:**
1. Einbuchung oeffnen.
2. FA `78901` eingeben → Tab.

**Erwartetes Verhalten:**
- Hint zeigt "Lagerplatz **X-Y-Z** — 12,500 **m** Artikel ..." (Einheit aus API-Field `unit`, nicht hardcoded "Stk").
- Falls Artikel-Unit leer ist: Fallback "Stk".

---
```

- [ ] **Step 3: Commit**

```pwsh
git add docs/TESTSZENARIEN.md
git commit -m "docs: testszenarien for inbound fa-storage hint and auto-fill" -m "Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Manuelle End-to-End-Verifikation (vor Merge)

Alle Szenarien TS-2.x-A bis J im Browser durchspielen, mit echten Daten.

Zusätzlich:
- **Submit-Funktionalität**: Einbuchung mit Auto-Fill-Lagerplatz speichern → Buchung landet korrekt in StockMovement (FA, Artikel, Lagerplatz, Menge).
- **Manuelle Override-Funktionalität**: Auto-Fill triggert → User wechselt Lagerplatz im Dropdown manuell → Submit → Buchung verwendet die manuell gewählte Position (nicht das Auto-Fill-Ziel).
- **Performance bei häufigem Tab**: FA-Feld 10 × ändern + Tab → keine N+1-Last (`lastFaQuery`-Cache fängt redundante Calls ab).
- **Cache-Disable wirksam**: Nach eigener Buchung 1 (FA `123`, K-WAGEN-03 → 5 Stk) sofort Buchung 2 öffnen, FA `123` eingeben → Hint zeigt aktuellen Bestand (5 Stk), nicht 0.

---

## Self-Review-Notiz

**Spec-Coverage:**
- Section 4 (Permission + Unit) → Task 1.
- Section 5 (Logik Frontend, inkl. DOM-Ready-Init und cache: no-store) → Task 2.
- Section 9 (Test-Szenarien) → Task 4.
- Changelog + Hilfeseite → Task 3.

**Reihenfolge ist wichtig:**
1. Task 1 zuerst — Permission muss da sein, sonst silent fail im UI-Test; Unit-Mapping muss da sein, sonst zeigt JS leere Einheit.
2. Task 2 vor Task 3 — Code vor Doku.
3. Task 3 vor Task 4 — Doku-Eintrag vor TESTSZENARIEN-Referenz.
4. Task 4 zuletzt — alle Szenarien validieren das fertige System.

**Differenzen zu v1-Plan (kritische Review):**
- ✅ Validation-Rerender: DOM-Ready-Init in Task 2 Step 2 ergänzt.
- ✅ Repository `Contains`-Match: akzeptiert für v1, in Spec 4.1 dokumentiert.
- ✅ Unit-Durchreichung: API mapped `Unit` ins Response, JS nutzt es mit Fallback "Stk".
- ✅ Versions-Strategie: kein AppVersion-Bump, bestehende v1.10.0-Card wird erweitert (Task 3 Step 1).
- ✅ Browser-Cache: `cache: 'no-store'` auf fetch (Task 2 Step 2).
- ✅ Permission-Semantik: pragmatisch bei Composite-Filter geblieben (Spec 4.2).

**No-Placeholder-Check:** Keine TBDs. Alle Code-Snippets vollständig.

**Commit-Frequency:** 4 Commits — einer pro Task.

**Branch-Bundling:** `feature/sage-lagerbestand-sync` hat bereits mehrere unrelated Features. Dies passt thematisch zur Stock-/Einbuchungs-Schiene und kann mit demselben Merge raus.
