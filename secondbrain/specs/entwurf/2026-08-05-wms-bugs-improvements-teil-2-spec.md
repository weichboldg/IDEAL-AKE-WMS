---
type: spec
title: "Massen-/Mehrfachartikel-Einbuchung (mehrere Artikel + Menge, ein Lagerplatz + eine FA)"
slug: 2026-08-05-wms-bugs-improvements-teil-2-spec
status: Entwurf
created: 2026-08-05
updated: 2026-08-05
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/StockMovementsController.cs
  - IdealAkeWms/Models/ViewModels/StockMovementCreateViewModel.cs (neues Multi-ViewModel)
  - IdealAkeWms/Views/StockMovements/Inbound.cshtml
  - IdealAkeWms/Views/StockMovements/ (neue View fuer Mehrfach-Einbuchung, Name offen)
  - IdealAkeWms/wwwroot/js/barcode-scanner.js (ggf. Erweiterung fuer Mehrfach-Scan-Modus)
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "UX-Entscheidung noetig: eigene neue Seite/Route (z. B. /StockMovements/InboundBulk) oder ein Umschalt-Modus auf der bestehenden Einbuchungsseite? Wirkt sich auf Navigation, Rollen-Sichtbarkeit und Testszenarien aus."
  - "Soll das Scannen mehrerer Artikel per Kamera-Scanner sequenziell in einer Zeilen-Liste erfolgen (Scan fuegt automatisch eine neue Zeile hinzu), oder bleibt der Scan-Button je Zeile wie bisher (ein Scan pro Zeile, Zeilen manuell hinzufuegen)?"
  - "Bedarfsmeldungen-Erfuellung (fulfilledRequisitionIds, aktuell in Inbound() verdrahtet) - soll das im Mehrfach-Modus pro Zeile einzeln waehlbar sein, oder entfaellt diese Funktion dort bewusst (weniger Bildschirmplatz pro Zeile)?"
  - "Obergrenze der Zeilenzahl pro Mehrfach-Einbuchung (Performance/Bedienbarkeit auf mobilen Handscannern)? Bestehende Muster kennen z. B. PageSize.AllCap 5000 als Vorbild fuer eine bewusste, sichtbare Grenze."
  - "Negativ-/Fehlerverhalten: wenn eine von mehreren Zeilen ungueltig ist (z. B. Artikel nicht gefunden, Menge <= 0) - soll die gesamte Buchung transaktional abgelehnt werden (alles-oder-nichts) oder sollen gueltige Zeilen gebucht und ungueltige zurueckgemeldet werden (analog quick-add-Skipped-Pattern)?"
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

Aktuell erlaubt die Einbuchungs-Maske (`/StockMovements/Inbound`) nur einen Artikel pro
Formular-Absenden — Lagerplatz und FA-Nummer werden bei jedem Artikel neu eingetragen. Wenn ein
Wareneingang mehrere Artikel für denselben Lagerplatz/dieselbe FA umfasst (üblicher Fall bei
Sammel-Lieferungen), führt das zu unnötig vielen Einzelbuchungen mit wiederholter Eingabe. Ziel
ist eine Mehrfachartikel-Einbuchung, bei der Lagerplatz und FA **einmalig** erfasst werden und
darunter beliebig viele Artikel-Zeilen (Artikel + Menge) gescannt/eingetragen werden können.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neue Eingabe-Möglichkeit für mehrere Artikel-Zeilen (Artikel-Auswahl/Scan + Menge je Zeile)
  unter einem gemeinsamen Kopf (Lagerplatz + FA-Nummer, wie bisher einmalig).
- Serverseitige Verarbeitung: pro Zeile eine `StockMovement`-Zeile vom Typ `Einbuchung` (identisch
  zum bestehenden Einzel-Pfad in `StockMovementsController.Inbound(POST)`), gleicher Lagerplatz
  und gleiche FA-Nummer für alle Zeilen des Formulars.
- Wiederverwendung der bestehenden Audit-Feld-Logik (`ICurrentUserService`), unverändert.
- Testszenarien-Ergänzung.

**Out-of-Scope**
- Keine Änderung an Aus- oder Umbuchung (nur Einbuchung, wie im Backlog gefordert).
- Keine Änderung an der bestehenden Einzel-Einbuchungsseite selbst — sie bleibt als Weg für den
  Einzelfall bestehen (Ausnahme: falls die offene UX-Rückfrage 1 zugunsten eines kombinierten
  Umschalt-Modus entschieden wird, verschiebt sich dieser Punkt).
- Keine Änderung an der FA-Lagerplatz-Hinweis-Logik aus Teil 1 — beide Teile sind unabhängig
  mergbar; der Mehrfach-Pfad kann den (bereits gefixten oder noch ungefixten) Hinweis pro Zeile
  grundsätzlich wiederverwenden, ist aber nicht von Teil 1 blockiert.
- Keine Änderung an Bedarfsmeldungen-Fulfillment-Logik über das bereits bestehende Verhalten
  hinaus (siehe offene Rückfrage 3).

## Fachliche Anforderungen

1. Ein Formular mit genau einem Lagerplatz-Feld und einem FA-Nummer-Feld (wie heute), darunter
   eine dynamische Liste von Zeilen mit je Artikel-Auswahl (Scan oder Suche, wie bisher via
   Select2/`btnScanArticle`) und Menge.
2. Mindestens eine Zeile ist Pflicht; Zeilen können hinzugefügt/entfernt werden (Client-seitig,
   analog dem bestehenden Artikel-Hinzufügen-Muster in `Views/WarehouseRequisitions/Edit.cshtml`).
3. Beim Absenden wird für jede gültige Zeile eine eigene `StockMovement`-Zeile
   (`MovementType.Einbuchung`) mit dem gemeinsamen Lagerplatz und der gemeinsamen FA-Nummer
   angelegt.
4. Erfolgsmeldung nennt die Anzahl gebuchter Artikel (`TempData["SuccessMessage"]`, analog
   `OutboundAllConfirm`: „{count} Artikel erfolgreich ausgebucht.").

## Ist-Zustand (Code-Referenzen)

- `IdealAkeWms/Controllers/StockMovementsController.cs:86-153` (`Inbound` GET/POST): verarbeitet
  genau **ein** `StockMovementCreateViewModel` (ein `ArticleId`, eine `Quantity`, ein
  `StorageLocationId`, ein `ProductionOrder`) pro Submit.
- `IdealAkeWms/Models/ViewModels/StockMovementCreateViewModel.cs:1-32`: flaches ViewModel, keine
  Zeilen-Liste.
- `IdealAkeWms/Views/StockMovements/Inbound.cshtml`: ein Artikel-Select2-Feld, ein Mengenfeld, ein
  Lagerplatz-Feld, ein FA-Feld — kein Mechanismus für mehrere Zeilen.
- Vorbild für „mehrere Zeilen unter einem Formular, client-seitig hinzufügen/entfernen" existiert
  bereits im Projekt: `Views/WarehouseRequisitions/Edit.cshtml:73-195` (Artikelsuche + dynamische
  Zeilenliste via Fetch-Calls, kein volles Form-Submit pro Zeile) — als Referenzmuster geeignet,
  auch wenn dort jede Zeile sofort einzeln per AJAX gespeichert wird (Draft-Bestellung), während
  die Einbuchung ein einmaliges Buchungs-Ereignis mit `Timestamp = DateTime.Now` für alle Zeilen
  gemeinsam sein sollte.

## Technischer Lösungsentwurf

**Empfohlener Ansatz** (vorbehaltlich Klärung offene Rückfrage 1): neue Action
`StockMovementsController.InboundBulk` (GET zeigt das Formular, POST verarbeitet alle Zeilen in
einem Request — kein Zeilen-für-Zeilen-AJAX wie bei WarehouseRequisitions, weil eine Einbuchung
ein einmaliges Buchungs-Ereignis ist und alle Zeilen denselben `Timestamp` tragen sollen).

- Neues ViewModel `StockMovementBulkInboundViewModel`:
  ```
  int StorageLocationId
  string? ProductionOrder
  List<StockMovementBulkInboundLine> Lines   // { int ArticleId, decimal Quantity, string? ArticleDisplay }
  List<StorageLocation> StorageLocations
  ```
- View mit Kopf-Feldern (Lagerplatz, FA-Nummer — je einmal) + dynamischer Zeilen-Tabelle
  (Artikel-Select2 + Menge je Zeile + „Zeile hinzufügen"/„Zeile entfernen"-Buttons, client-seitig
  ohne Server-Rundtrip pro Zeile).
- POST-Handler iteriert `Lines`, erzeugt je gültiger Zeile eine `StockMovement`
  (`ArticleId`, `Quantity`, `StorageLocationId` = Kopf-Wert, `ProductionOrder` = Kopf-Wert,
  `MovementType.Einbuchung`, `Timestamp = DateTime.Now` einmalig vor der Schleife ermittelt,
  Audit-Felder wie im bestehenden Einzel-Pfad), analog zu `OutboundAllConfirm`
  (`StockMovementsController.cs:367-405`), das bereits das Muster „mehrere `StockMovement` in
  einer Schleife anlegen, gemeinsamer `now`" zeigt.
- Fehlerbehandlung: siehe offene Rückfrage 5 (alles-oder-nichts vs. teilweise buchen +
  Rückmeldung).
- Scan-Integration: bestehendes `barcode-scanner.js` (`initScanner`) kann pro Zeile wiederverwendet
  werden (jede Zeile bekommt ihren eigenen Scan-Button mit eindeutiger Target-Id); ein
  „Scan fügt automatisch neue Zeile hinzu"-Modus wäre eine Erweiterung von
  `processScannedValue`/`initScanner` und ist Gegenstand offener Rückfrage 2.

## Migrations-/SQL-Auswirkungen

Keine. `StockMovement` bleibt unverändert; es werden nur mehrere Instanzen der bestehenden
Entität angelegt, wie es `OutboundAllConfirm` bereits heute für Ausbuchungen tut.

## Audit-Feld-Auswirkungen

Jede erzeugte `StockMovement`-Zeile setzt `CreatedAt`/`CreatedBy`/`CreatedByWindows` wie im
bestehenden Einzel-Einbuchungspfad (`_currentUserService`), identisch für alle Zeilen eines
Bulk-Submits (gemeinsamer `Timestamp`).

## Rollen- und Zugriffsfilter-Auswirkungen

Keine neue Rolle. Die neue Action liegt im selben Controller wie `Inbound` und erhält denselben
Filter `[RequireStockAccess]` (admin, stock, stock_keyuser, picking) — konsistent mit der
bestehenden Einbuchung.

## Listen-View-Pattern-Pflichten

Nicht anwendbar — die Mehrfach-Einbuchung ist ein Eingabeformular, keine Listen-View im Sinne von
ADR 0005 (keine Pagination/Spaltenfilter-Pflicht).

## Akzeptanzkriterien

1. Auf dem neuen Formular können mindestens 2 Zeilen mit unterschiedlichen Artikeln + Mengen
   erfasst und in einem Schritt gebucht werden.
2. Nach dem Absenden existiert für jede gültige Zeile genau eine neue `StockMovement`
   (`MovementType.Einbuchung`) mit dem gemeinsamen Lagerplatz und der gemeinsamen FA-Nummer.
3. Lagerplatz- und FA-Feld müssen nur einmal ausgefüllt werden, nicht pro Zeile.
4. Die Erfolgsmeldung nennt die Anzahl gebuchter Zeilen.
5. Eine leere Zeilenliste (0 Zeilen) wird abgelehnt (ModelState-Fehler, analog dem bestehenden
   `ModelState.IsValid`-Check).
6. Verhalten bei ungültigen Einzel-Zeilen entspricht der Klärung aus offener Rückfrage 5 und ist
   entsprechend testbar (z. B. „ungültige Zeile wird übersprungen, gültige werden gebucht" ODER
   „gesamte Buchung wird abgelehnt, wenn eine Zeile ungültig ist").

## Test-Szenarien

Neues Szenario in `docs/TESTSZENARIEN.md` Kapitel 2 (Lager):
- **Vorbedingung:** Rolle mit `RequireStockAccess` (z. B. `stock`), mindestens 2 existierende
  Artikel, ein buchbarer Lagerplatz.
- **Schritte:** Mehrfach-Einbuchung öffnen, Lagerplatz + FA-Nummer einmalig setzen, 3 Artikel-
  Zeilen mit unterschiedlichen Mengen erfassen, absenden.
- **Erwartetes Verhalten:** 3 neue `StockMovement`-Einträge in der Bewegungshistorie, alle mit
  demselben Lagerplatz/derselben FA-Nummer, jeweils korrekte Artikel/Menge.
- **Negativfall:** eine Zeile ohne Artikelauswahl oder mit Menge 0 → Verhalten je Klärung offene
  Rückfrage 5 gegenprüfen.

`secondbrain/tests/testszenarien-index.md` Kapitel 2 entsprechend ergänzen.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rückfragen

1. UX-Entscheidung nötig: eigene neue Seite/Route (z. B. `/StockMovements/InboundBulk`) oder ein
   Umschalt-Modus auf der bestehenden Einbuchungsseite? Wirkt sich auf Navigation,
   Rollen-Sichtbarkeit und Testszenarien aus.
2. Soll das Scannen mehrerer Artikel per Kamera-Scanner sequenziell in einer Zeilen-Liste
   erfolgen (Scan fügt automatisch eine neue Zeile hinzu), oder bleibt der Scan-Button je Zeile
   wie bisher (ein Scan pro Zeile, Zeilen manuell hinzufügen)?
3. Bedarfsmeldungen-Erfüllung (`fulfilledRequisitionIds`, aktuell in `Inbound()` verdrahtet) —
   soll das im Mehrfach-Modus pro Zeile einzeln wählbar sein, oder entfällt diese Funktion dort
   bewusst (weniger Bildschirmplatz pro Zeile)?
4. Obergrenze der Zeilenzahl pro Mehrfach-Einbuchung (Performance/Bedienbarkeit auf mobilen
   Handscannern)?
5. Negativ-/Fehlerverhalten: wenn eine von mehreren Zeilen ungültig ist (z. B. Artikel nicht
   gefunden, Menge ≤ 0) — soll die gesamte Buchung transaktional abgelehnt werden
   (alles-oder-nichts) oder sollen gültige Zeilen gebucht und ungültige zurückgemeldet werden
   (analog dem `quick-add`-Skipped-Pattern in `WarehouseRequisitionsApiController`)?

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →
2. →
3. →
4. →
5. →
