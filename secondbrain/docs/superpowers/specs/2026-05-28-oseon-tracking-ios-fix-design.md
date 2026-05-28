# OSEON-Tracking — iOS-Safari-Bug + Performance-Refactor — Design

**Datum:** 2026-05-28
**Status:** Spec, Brainstorming abgeschlossen, wartet auf User-Review vor `writing-plans`
**Worktree:** `.claude/worktrees/oseon-tracking-ios` auf Branch `bugfix/oseon-tracking-ios`
**Scope:** Web (`IdealAkeWms/`), keine Service-/DB-Aenderungen

---

## 1. Problemstellung

Auf iOS Safari ist die Seite `/Tracking/OseonIndex` praktisch nicht bedienbar:

- **Symptom 1 — extreme Slowness/Stecken:** Die Seite rendert pro Page-Load alle 25 Kundenauftrag-Gruppen vollstaendig inklusive aller Subauftraege und Arbeitsgaenge (~1.150 Tabellenzeilen). Auf iOS Safari fuehrt das zu mehreren Sekunden Render-Latenz und sichtbarem Scroll-Stocken.
- **Symptom 2 — Artikelnummer-Input nicht bedienbar:** Eingaben ins Filter-Input gehen verloren oder loesen verzoegert aus. Vermutlich Folge der allgemeinen Slowness + iOS-Virtual-Keyboard-State-Issue nach Scanner-Modal-Dismiss.
- **Symptom 3 — QR-Code-Button funktioniert nicht:** `html5-qrcode@2.3.8` ruft `navigator.mediaDevices.getUserMedia()` erst nach Bootstrap-Modal-Show auf — iOS Safari verlangt strikt User-Gesture-Synchronitaet und verweigert die Camera-Permission.

Diagnose-Quellen: View [Tracking/OseonIndex.cshtml](../../../../IdealAkeWms/Views/Tracking/OseonIndex.cshtml), Skript [barcode-scanner.js](../../../../IdealAkeWms/wwwroot/js/barcode-scanner.js), CDN-Import [unpkg.com/html5-qrcode@2.3.8](https://unpkg.com/html5-qrcode@2.3.8/).

## 2. Ziele

1. **iOS-Tauglichkeit** der OSEON-Tracking-Seite herstellen: fluessige Scroll-/Tipp-/Klick-Interaktion auf aktuellem iPhone/iPad (Safari).
2. **Desktop-Performance** als Nebenprodukt deutlich verbessern (50x weniger initiales DOM).
3. **QR-Scanner** zuverlaessig auf iOS arbeitsfaehig.
4. **Bestehende Workflows** (Filter, Sort, Bestand-Lookup, Ampel-Logik, Operations-Toggle, Rueckmelde-Aktion) bleiben funktional unveraendert.
5. **Out-of-Scope:** Live-Push-Updates, Server-Side-Detail-Cache, separate Mobile-View, Library-Replace, Native-App-Wrapper.

## 3. Architektur — Flat-Listing + Detail-AJAX

### 3.1 Status quo (Problem)

`/Tracking/OseonIndex` returnt komplett gerendertes HTML fuer:
- 25 Kundenauftrag-Gruppen (Pagination-Cap)
- pro Gruppe ~5 Subauftraege
- pro Subauftrag ~8 Arbeitsgaenge

= ~1.150 `<tr>`-Elemente, alle Collapse-Rows mit `display:none` aber im DOM. Plus pro Subauftrag mehrere `data-sort-*`-Attribute, SVG-Icons, Status-Badges.

### 3.2 Zielzustand

`/Tracking/OseonIndex` returnt im Normalfall (kein Artikel-Filter) nur die **25 Top-Level-Kundenauftrag-Zeilen**:
- Group-Header mit Sub-/AG-Counter-Badges, Worst-Color, aggregiertem Status, max. Termin
- Pro Gruppe ein leerer `<tbody class="oseon-group-details" data-loaded="false" data-customer-order="{key}">` als Platzhalter

Erstes Aufklappen einer Gruppe triggert AJAX-`GET /Tracking/OseonGroupDetails?customerOrderNumber={key}&...` und injiziert das Detail-HTML in das `<tbody>`.

### 3.3 Sonderfall Artikel-Filter (Prefetch)

Wenn `filterArticle` gesetzt ist (User sucht aktiv), prefetcht der Server alle Detail-Rows der gematchten Gruppen und rendert sie inline mit `data-loaded="true"` + initial sichtbar. Damit ist die Such-Sicht nicht langsamer als heute.

### 3.4 Datenfluss

| Szenario | Server-Last | Client-Last |
|---|---|---|
| Normale Browse-Sicht (kein Filter) | 25 Top-Level-Rows | 25 DOM-Rows initial |
| Aufklappen einer Gruppe | 1 AJAX-Call, 1 Group-Detail-Query | + ~50-200 DOM-Rows |
| Artikel-Filter aktiv | wie heute (alle Treffer prefetcht) | wie heute |

## 4. Komponenten

### 4.1 Backend

#### `TrackingController.OseonIndex(...)` — modifiziert
- Filter-/Pagination-Auswertung bleibt
- **Wenn `filterArticle` leer:** ViewModel-Items enthalten nur Top-Level-Gruppen-Aggregate (SubOrders-Liste leer)
- **Wenn `filterArticle` gesetzt:** bestehender Flow inklusive Sub-Order- und Operation-Load
- ViewBag/Marker `IsPrefetched` true/false fuer die View

#### `TrackingController.OseonGroupDetails(string customerOrderNumber, bool useRelevanceFilter = true, ...)` — neu
- Permission: `[RequireTrackingAccess]` (gleicher Filter wie OseonIndex)
- Holt SubOrders + Operations fuer genau diese Kundenauftrag-Gruppe via Repo-Method `GetByCustomerOrderNumberAsync(...)`
- Wendet dieselbe Ampel-/Termin-/Relevanz-Logik wie OseonIndex an (Code in Helper-Service auslagern, damit DRY)
- Returnt `PartialView("_OseonGroupDetails", subOrderViewModels)`
- 404 wenn keine Gruppe mit diesem Schluessel existiert oder kein Treffer

#### `OseonProductionOrderRepository` — kleine Erweiterung
- Neue Methode: `GetByCustomerOrderNumberAsync(string customerOrderNumber, HashSet<string>? relevantOpNames, CancellationToken ct)` returnt alle Sub-Orders + WorkOperations einer Gruppe
- Bestehende `GetPagedAsync` bekommt einen optionalen `bool topLevelOnly = false` Parameter — bei `true` werden nur Top-Level-Aggregate (Count + WorstColor + WorstStatus + max DueDate) geladen, kein WorkOperations-JOIN

#### Helper-Service `OseonGroupViewModelBuilder` — neu
- Extrahiert die heutige Inline-Logik aus `OseonIndex` (Ampel-/Termin-Berechnung, Worst-Color-Aggregation, Sub-Order-Mapping) in einen separaten Service.
- Wird sowohl von `OseonIndex` (im Prefetch-Pfad) als auch von `OseonGroupDetails` aufgerufen — DRY.
- Konstruktor-Dependencies: `IOseonTrafficLightService`, `IBusinessDayService`, `IOseonOperationConfigRepository`, `IHolidayRepository`.

### 4.2 Frontend

#### `Views/Tracking/OseonIndex.cshtml` — modifiziert
- Filter-Card unveraendert
- Top-Level-Tabelle rendert 25 Group-Header-Rows
- Pro Gruppe: `<tbody class="oseon-group-details" data-loaded="@(isPrefetched ? "true" : "false")" data-customer-order="@key">`
- Wenn `isPrefetched`: das Partial `_OseonGroupDetails` wird inline gerendert und ist initial sichtbar
- Wenn nicht: leeres `<tbody>` als Platzhalter (kein Detail-HTML)
- Toggle-Chevron-Click-Handler (im neuen JS, siehe unten) macht den AJAX-Lazy-Load

#### `Views/Tracking/_OseonGroupDetails.cshtml` — neu
- Empfaengt `List<OseonSubOrderViewModel>` als Model
- Rendert das heutige Sub-Order- + AG-Markup (extrahiert aus Zeilen 174-244 der bestehenden OseonIndex.cshtml)
- Keine Filter-Card, kein Top-Level-Pagination — nur die Rows fuer **eine** Gruppe

#### `wwwroot/js/oseon-tracking-lazy.js` — neu
- Eigene Datei (nicht inline), weil das JS nicht trivial ist und Caching benoetigt
- Verantwortlich fuer:
  - Click-Handler auf `.oseon-tree-group .toggle-chevron`
  - AJAX-Fetch via `fetch('/Tracking/OseonGroupDetails?customerOrderNumber=...&useRelevanceFilter=...')`
  - HTML-Inject in das passende `<tbody>`
  - `data-loaded="true"` setzen nach Erfolg
  - Loading-Spinner-Row + Error-Row + Retry-Button
  - Doppel-Click-Lock via `data-loading="true"`
- Erweitert/ersetzt die bestehende Inline-Script-Logik in OseonIndex.cshtml fuer den Toggle-Mechanismus

### 4.3 Scanner / iOS-Fixes

#### `wwwroot/js/barcode-scanner.js` — modifiziert
- **iOS-User-Gesture-Fix fuer `getUserMedia`:** Im Click-Handler von `btnScanArticle`/`btnScanCustomerOrder` wird ZUERST `navigator.mediaDevices.getUserMedia({video: {facingMode: 'environment'}})` aufgerufen (im synchronen User-Gesture-Stack). Resultat: Permission-Prompt erscheint. Bei OK: returned Stream wird sofort wieder gestopt (Permission-Pre-Warm), DANN wird das Bootstrap-Modal geoeffnet und html5-qrcode startet wie heute.
- **Form-Submit nach Scan:** Der callback in OseonIndex.cshtml [Zeile 412](../../../../IdealAkeWms/Views/Tracking/OseonIndex.cshtml#L412) wird in `setTimeout(() => form.submit(), 50)` gewrappt, damit iOS' Virtual-Keyboard-State-Teardown vor der Navigation komplett ist.

#### Lokales Hosting von html5-qrcode
- Datei `wwwroot/lib/html5-qrcode/html5-qrcode.min.js` neu erstellt (Inhalt aus `https://unpkg.com/html5-qrcode@2.3.8/html5-qrcode.min.js`)
- Script-Tag in OseonIndex.cshtml umstellen: `<script src="~/lib/html5-qrcode/html5-qrcode.min.js" asp-append-version="true"></script>`
- Eliminiert CDN-Network-Latenz auf Mobile

#### `column-preferences.js` Defer
- Init-Logik in `requestIdleCallback(fn, { timeout: 500 })` mit Fallback `setTimeout(fn, 100)` wrappen
- Erlaubt der OSEON-Tracking-Seite, den ersten Paint vor dem Spalten-Settings-Apply zu zeigen

#### Viewport-Meta-Check
- `_Layout.cshtml` enthaelt bereits `<meta name="viewport" content="width=device-width, initial-scale=1">` (vermutlich). Pruefen + ggf. ergaenzen `user-scalable=yes` und `viewport-fit=cover`.

## 5. Datenfluss & API-Vertrag

### 5.1 GET `/Tracking/OseonGroupDetails`

**Query-Parameter:**
- `customerOrderNumber` (string, required)
- `useRelevanceFilter` (bool, default `true`)
- `showFinished` (bool, default `false`)

**Response:**
- 200 OK + `text/html` mit dem PartialView-Rendering (`_OseonGroupDetails.cshtml`)
- 404 Not Found wenn die Gruppe nicht existiert oder keine Treffer hat
- 401/403 wenn User keine Tracking-Permission hat

**Beispiel-Response-Body (gekuerzt):**
```html
<tr class="oseon-tree-sub" data-sort-...>
  <td>...</td>
</tr>
<tr class="oseon-tree-op-container">
  <td colspan="N"><div class="collapse oseon-tree-ops-collapse" id="ops-...">
    <tr class="oseon-tree-op">...</tr>
  </div></td>
</tr>
```

### 5.2 Client-Side AJAX-Flow

```javascript
async function loadGroupDetails(customerOrderNumber, tbody) {
    if (tbody.dataset.loaded === 'true' || tbody.dataset.loading === 'true') return;
    tbody.dataset.loading = 'true';
    showSpinner(tbody);
    try {
        const url = `/Tracking/OseonGroupDetails?customerOrderNumber=${encodeURIComponent(customerOrderNumber)}&useRelevanceFilter=${getCurrentRelevanceFilter()}&showFinished=${getCurrentShowFinished()}`;
        const resp = await fetch(url, { headers: { 'Accept': 'text/html' } });
        if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
        const html = await resp.text();
        tbody.innerHTML = html;
        tbody.dataset.loaded = 'true';
    } catch (err) {
        showError(tbody, err.message);
    } finally {
        tbody.dataset.loading = 'false';
    }
}
```

## 6. Error-Handling

| Szenario | Verhalten |
|---|---|
| AJAX-Fetch schlaegt fehl (Network-Error) | Error-Row mit Retry-Button im `<tbody>`. Nach Klick auf Retry: neuer Fetch. |
| Server returnt 404 (Gruppe weg) | "Diese Gruppe ist nicht mehr verfuegbar. Bitte Seite neu laden." |
| Server returnt 5xx | "Fehler beim Laden — siehe Server-Log. Bitte erneut versuchen." mit Retry. |
| User klickt waehrend Loading mehrfach | `data-loading="true"`-Lock verhindert Doppel-Request. |
| Scanner getUserMedia-Permission verweigert | Bestehender Fallback in barcode-scanner.js: File-Upload-UI im Modal. Unveraendert. |
| Scanner-Pre-Warm wirft (z.B. iOS < 15) | Try/catch im Click-Handler — fallback auf alten Verhalten (Modal-Show + getUserMedia spaeter). User-Experience nicht schlechter als heute. |

## 7. Testing

### 7.1 Automatisierte Tests (.NET)

Datei `IdealAkeWms.Tests/Controllers/TrackingControllerTests.cs` (neu oder erweitert):

- `OseonGroupDetails_returns_partial_view_with_suborders` — Mock-Repo liefert 2 SubOrders + 3 Ops, Action returnt PartialView mit 2 SubOrder-Items
- `OseonGroupDetails_filters_irrelevant_operations` — Mock-OperationConfig setzt 1 von 3 Ops als nicht-relevant, bei `useRelevanceFilter=true` ist diese im Result-Markup nicht enthalten
- `OseonGroupDetails_returns_NotFound_for_unknown_customer_order` — Mock-Repo liefert leer, Action returnt `NotFound()`
- `OseonGroupDetails_respects_RequireTrackingAccess` — Tests via Integration-Filter wie bei bestehenden Controllers
- `OseonIndex_without_filterArticle_does_not_load_suborders` — Mock-Repo wird mit `topLevelOnly: true` aufgerufen
- `OseonIndex_with_filterArticle_prefetches_suborders` — Mock-Repo wird ohne `topLevelOnly` aufgerufen (Voll-Load)

### 7.2 Manuelle Tests (TESTSZENARIEN Kapitel 30)

1. **Desktop Chrome — Initial-Load-Performance:** OseonIndex oeffnen ohne Filter → erster Paint < 1s, 25 Gruppen sichtbar, alle initial collapsed.
2. **Desktop Chrome — Lazy-Aufklappen:** Click auf Chevron einer Gruppe → Spinner < 100ms → SubAufträge sichtbar < 500ms. Erneutes Click-Toggle: instant (kein AJAX).
3. **Desktop Chrome — Artikel-Filter:** `?filterArticle=12345` setzen → Treffer-Gruppen erscheinen mit inline-prefetched SubAuftraegen, initial expanded.
4. **iOS Safari — Bedienbarkeit:** Page fluessig, Scroll-Stocken weg, Artikel-Filter-Input akzeptiert Eingaben sofort.
5. **iOS Safari — QR-Scanner:** Button-Click → Permission-Prompt im selben User-Gesture-Stack → bei Accept: Kamera-Vorschau im Modal → Scan → Filter auto-submit ohne Lock-up.
6. **iOS Safari — QR-Fallback:** Wenn Camera-Permission verweigert: File-Upload-Variante erscheint.

### 7.3 iOS-Geraet als Test-Bed

User hat iOS-Geraet verfuegbar — pro Implementierungs-Schritt direkter Test moeglich. Plan-Phase plant entsprechende Test-Checkpoints zwischen Tasks.

## 8. Versionierung & Doku

- **AppVersion-Bump** auf `1.16.0` (Minor — strukturelle View-Aenderung mit User-sichtbarem Verhalten beim Aufklappen). Web. Service wird nicht beruehrt (kein Code-Change im Service-Projekt) — bleibt auf 1.15.3.
- **Changelog** [Views/Help/Changelog.cshtml](../../../../IdealAkeWms/Views/Help/Changelog.cshtml) — neuer v1.16.0-Block.
- **Hilfeseite** [Views/Help/Index.cshtml](../../../../IdealAkeWms/Views/Help/Index.cshtml) — Notiz beim OSEON-Tracking-Abschnitt: "Details werden beim Aufklappen geladen — ein Klick auf den Pfeil holt die Subauftrags-Liste".
- **TESTSZENARIEN** [docs/TESTSZENARIEN.md](../../../../docs/TESTSZENARIEN.md) — neues Kapitel 30 mit 6 Szenarien.
- **PROJECT_STATUS.md** — neue Fortschritts-Sektion + Hauptfunktion + Roadmap.
- **CLAUDE.md** — neuer Fallstrick: "iOS Safari + html5-qrcode: `getUserMedia` muss im synchronen User-Gesture-Stack stehen. Vor Modal-Show die Permission anfragen, nicht danach. Pattern siehe barcode-scanner.js".

## 9. Risiken & Mitigation

| Risiko | Mitigation |
|---|---|
| AJAX-Detail-Endpoint zu langsam (> 500ms) | Repo-Method ist klein (eine Group), sollte < 100ms sein. Falls langsam: Index-Pruefung auf `CustomerOrderNumber` in OseonProductionOrders. |
| User klickt mehrfach beim Loading | `data-loading="true"`-Lock im JS. |
| Artikel-Filter-Prefetch laesst Server haengen (viele Gruppen mit Treffern) | Pagination 25/Seite bleibt, Worst-Case = aktueller Stand. |
| html5-qrcode lokal hosten kollidiert mit Cache | `asp-append-version="true"` versioniert via Hash, Browser-Cache invalidiert sich bei jeder Code-Aenderung. |
| iOS-Permission-Pre-Warm verschlechtert Desktop-UX | Pre-Warm ist no-op wenn Permission schon erteilt. Auf Desktop nicht sichtbar. |
| Helper-Service `OseonGroupViewModelBuilder` introduziert Bugs in OseonIndex | Refactor via TDD — bestehende OseonIndex-Tests werden zuerst auf den Builder umgestellt (gruen halten), dann der Lazy-Pfad ergaenzt. |
| User hat noch alte Browser-Cache der OseonIndex.cshtml-View → kennt das neue `data-loaded`-Attribut nicht | Razor-View wird bei Deploy neu compiliert + ausgeliefert. Browser-Cache enthaelt nur statische Assets (JS/CSS) — die werden ueber `asp-append-version` invalidiert. |
| iOS < 15 kein `requestIdleCallback` | Fallback `setTimeout(fn, 100)` im column-preferences-Defer. |

## 10. Abgrenzung — NICHT in dieser Spec

- Keine Push-Live-Updates (z.B. SignalR fuer OseonStatus-Aenderungen)
- Keine separate Mobile-View
- Kein Replace von html5-qrcode durch andere Library
- Kein iOS-Native-App-Wrapper
- Keine Aenderung am Daten-Schema (OseonProductionOrders, WorkOperations bleiben unveraendert)
- Keine Aenderung am Sync-Worker / OSEON-Tracking-Sync — der ist separat
- Keine Erweiterung der Filter-Felder oder Spalten

## 11. Offen / In Plan-Phase zu klaeren

- **Genaue Param-Liste fuer `GetByCustomerOrderNumberAsync`** — abhaengig vom bestehenden Repo-Code, in Plan-Phase nach Lesen der Datei.
- **Wo lebt das `useRelevanceFilter`/`showFinished` zur Laufzeit?** Vermutlich URL-Query von OseonIndex — Plan-Phase verifiziert.
- **AJAX-Caching-Strategie** — vorgeschlagen: kein expliziter Cache; Browser-default. Plan-Phase entscheidet final.
- **Loading-Spinner-Styling** — Bootstrap-Spinner inline oder eigener Mini-Loader-Style. Plan-Phase entscheidet.
- **Fehler-Retry-UX** — vorgeschlagen: einfacher Button "Erneut versuchen". Plan-Phase entscheidet ob mehr Logik (Auto-Retry mit Backoff) noetig.
- **Code-Reorganisation OseonGroupViewModelBuilder** — Plan-Phase pruef, ob es ein neuer DI-Service oder eine statische Helper-Klasse werden soll.

---

**Naechster Schritt:** User reviewt die Spec. Bei Freigabe → `superpowers:writing-plans` → Implementierungsplan.
