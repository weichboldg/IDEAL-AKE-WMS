---
type: spec
title: "Bugfix: FA-Lagerplatz-Hinweis auf tatsächlichen Bestand statt Bewegungssaldo umstellen"
slug: 2026-08-05-wms-bugs-improvements-teil-1-spec
status: Entwurf
created: 2026-08-05
updated: 2026-08-05
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Data/Repositories/StockMovementRepository.cs
  - IdealAkeWms/Data/Repositories/IStockMovementRepository.cs
  - IdealAkeWms.Tests/Repositories/StockMovementRepositoryTests.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Soll ein Kandidat-Lagerplatz (aus FA-getaggten Bewegungen ermittelt) mit tatsächlichem Bestand 0 komplett aus der Antwort verschwinden (wie bisher bei Netto 0), oder als Info-Zeile mit Menge 0 erhalten bleiben (\"war hier, ist aber weg\")? Wirkt sich auf Inbound-Hint, StockOverview-FA-Filter und Tracking-Modal gleichermassen aus."
  - "GetStockByProductionOrderAsync wird an 3 Stellen verwendet (Inbound-Hint, StockOverview-FA-Filter, Tracking-Lagerbestand-Modal). Ist die Verhaltensaenderung (nur noch realer Bestand statt Bewegungssumme) fuer ALLE drei Call-Sites fachlich gewuenscht, oder soll z. B. der StockOverview-FA-Filter bewusst weiter \"wo wurde unter dieser FA jemals gebucht\" (auch historisch/0-Bestand) zeigen koennen?"
  - "Performance: die Kandidaten-Ermittlung + Bestandsberechnung laeuft in-memory ueber ggf. viele StockMovement-Zeilen je FA. Gibt es eine bekannte Obergrenze an Bewegungen pro FA-Nummer, die einen SQL-seitigen statt In-Memory-Ansatz noetig macht?"
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

Der Hinweis „FA `<Nummer>` liegt bereits: ..." bei der Einbuchung soll dem Lagermitarbeiter
zuverlässig zeigen, **wo aktuell tatsächlich Bestand liegt**, der zu diesem Fertigungsauftrag
gehört — nicht, wo irgendwann einmal etwas unter diesem FA-Tag gebucht wurde. Aktuell zeigt der
Hinweis auch dann Bestand an, wenn der Artikel am genannten Lagerplatz längst wieder ausgebucht
wurde (siehe Bug-Record [[2026-08-05-einbuchung-fa-hinweis-bewegungen-statt-bestand-bug]]). Das
führt zu falschen Einlagerungs-Entscheidungen und Vertrauensverlust in die Funktion.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Korrektur von `StockMovementRepository.GetStockByProductionOrderAsync`, sodass die
  zurückgegebene Menge dem **tatsächlichen aktuellen Bestand** am jeweiligen
  Artikel/Lagerplatz-Paar entspricht (nicht dem Netto-Saldo der FA-getaggten Bewegungen allein).
- Damit korrigiert sich automatisch das Verhalten an allen drei Aufrufstellen: Einbuchungs-Hinweis
  (`/api/stock/by-order/{fa}` → `Inbound.cshtml`), Bestandsübersicht-FA-Filter
  (`StockOverviewController.Index`) und Tracking-Lagerbestand-Modal (`OseonIndex.cshtml`).
- Repository-Tests für den korrigierten Pfad (insbesondere das Reproduktionsszenario aus dem
  Bug-Record: Einbuchung mit FA-Tag, Ausbuchung ohne FA-Tag → Ergebnis muss leer/0 sein).

**Out-of-Scope**
- Keine Änderung an `StockMovementCreateViewModel.ProductionOrder` (bleibt optional — ein
  Pflichtfeld auf allen drei Buchungsformularen wäre ein eigener, weitreichenderer
  Verhaltenswechsel und nicht Teil dieses Bugfixes).
- Keine Änderung am UI-Text/Design des Hinweises selbst (`Inbound.cshtml` Zeilen 190-270) — nur
  die zugrunde liegenden Daten ändern sich.
- Keine Änderung an `GetCurrentStockAsync` (unverändert korrekt für die normale Bestandsübersicht
  ohne FA-Filter).

## Fachliche Anforderungen

1. Der FA-Lagerplatz-Hinweis zeigt ausschließlich Artikel/Lagerplatz-Kombinationen, an denen
   **aktuell** ein positiver Bestand liegt.
2. Die Ermittlung „welche Lagerplätze könnten zu diesem FA gehören" bleibt weiterhin über das
   `ProductionOrder`-Tag der Bewegungen (Kandidatensuche) — nur die **Mengenberechnung** je
   Kandidat wechselt von „Summe der FA-getaggten Bewegungen" auf „Summe aller Bewegungen an
   diesem Artikel/Lagerplatz-Paar" (= echter Bestand, analog `GetCurrentStockAsync`).

## Ist-Zustand (Code-Referenzen)

`IdealAkeWms/Data/Repositories/StockMovementRepository.cs:190-248`
(`GetStockByProductionOrderAsync`):

```
var movements = await _dbSet
    .Include(sm => sm.Article)
    .Include(sm => sm.StorageLocation)
    .Where(sm => sm.ProductionOrder != null && sm.ProductionOrder.Contains(productionOrder))
    .ToListAsync();
...
// Summiert NUR die FA-getaggten Bewegungen je (ArticleId, StorageLocationId)
```

Root Cause im Detail: siehe Bug-Record
[[2026-08-05-einbuchung-fa-hinweis-bewegungen-statt-bestand-bug]]. Kurzfassung: Da
`StockMovementCreateViewModel.ProductionOrder` optional ist
(`IdealAkeWms/Models/ViewModels/StockMovementCreateViewModel.cs:20-22`), landet eine
Gegenbewegung (typischerweise die spätere Ausbuchung) oft **ohne** FA-Tag in der DB und wird von
der `Contains`-Filterung in Zeile 196 nicht erfasst — die FA-Summe bleibt künstlich positiv,
obwohl der tatsächliche Bestand (Summe **aller** Bewegungen an diesem Artikel/Lagerplatz-Paar,
vgl. `GetCurrentStockAtLocationAsync`, Zeilen 426-446) längst 0 oder negativ ist.

Drei Aufrufstellen mit identischem Fehlverhalten:
- `IdealAkeWms/Controllers/StockApiController.cs:19-35` (`GetStockByOrder`, aufgerufen von
  `Inbound.cshtml:282` und `OseonIndex.cshtml:347`).
- `IdealAkeWms/Controllers/StockOverviewController.cs:48-51` (direkter Repository-Aufruf im
  FA-Filter-Zweig von `Index`).

## Technischer Lösungsentwurf

`GetStockByProductionOrderAsync` zweistufig umbauen:

1. **Kandidaten-Ermittlung** (wie bisher): alle `(ArticleId, StorageLocationId)`-Paare, an denen
   irgendeine Bewegung mit `ProductionOrder.Contains(productionOrder)` existiert
   (`_dbSet.Where(sm => sm.ProductionOrder != null && sm.ProductionOrder.Contains(productionOrder))
   .Select(sm => new { sm.ArticleId, sm.StorageLocationId }).Distinct()`).
2. **Bestandsberechnung je Kandidat** (neu): für exakt diese Paare den tatsächlichen Bestand über
   **alle** Bewegungen berechnen — analog der bereits vorhandenen Aggregationslogik in
   `GetCurrentStockAsync` (Zeilen 13-188, Einbuchung/SageEinbuchung/Umbuchung-Ziel positiv,
   Ausbuchung/SageAusbuchung negativ, Umbuchung-Quelle subtrahiert) bzw. per Wiederverwendung/
   Refactoring eines gemeinsamen privaten Hilfsbausteins, um die Aggregationsregel nicht ein
   drittes Mal zu duplizieren (Fallstrick „`MovementType`-Erweiterung trifft 6 Stellen" beachten —
   ein neuer, siebter Ort mit eigener Kopie der Switch-Logik ist zu vermeiden).
3. Nur Paare mit tatsächlichem Bestand `!= 0` (bzw. `> 0`, siehe offene Rückfrage 1) zurückgeben.
4. Rückgabetyp (`List<StockOverviewItem>`) und die drei Call-Sites bleiben unverändert — der Fix
   ist rein in der Repository-Methode gekapselt, keine Signaturänderung nötig.

Kein neues Repository-Interface-Mitglied nötig (Methode bleibt `GetStockByProductionOrderAsync`
mit gleicher Signatur).

## Migrations-/SQL-Auswirkungen

Keine. Reine Query-Logik-Änderung, kein Schema-Impact, keine neue Migration, kein neues SQL-Skript.

## Audit-Feld-Auswirkungen

Keine. `StockMovement` bleibt unverändert; es werden keine neuen Schreibpfade eingeführt, nur ein
Lesepfad korrigiert.

## Rollen- und Zugriffsfilter-Auswirkungen

Keine Änderung. Betroffene Endpunkte bleiben unter ihren bestehenden Filtern:
`StockApiController` → `[RequireStockOrPickingOrTrackingAccess]`,
`StockOverviewController` → `[RequireStockReadAccess]` (class-level).

## Akzeptanzkriterien

1. Artikel A wird mit FA-Tag `1234567` auf Lagerplatz X eingebucht (Menge 5), anschließend ohne
   FA-Tag vollständig wieder ausgebucht (Menge 5) → `GetStockByProductionOrderAsync("1234567")`
   liefert für Artikel A/Lagerplatz X **keine** Zeile mehr (bzw. Menge 0, je nach Klärung der
   offenen Rückfrage 1).
2. Artikel A wird mit FA-Tag `1234567` auf Lagerplatz X eingebucht (Menge 5) und bleibt dort
   unverändert liegen → `GetStockByProductionOrderAsync("1234567")` liefert weiterhin Artikel
   A/Lagerplatz X mit Menge 5 (Regressionsschutz: der Normalfall funktioniert wie bisher).
3. Artikel A wird mit FA-Tag `1234567` auf Lagerplatz X eingebucht (Menge 5) und anschließend
   **teilweise** (Menge 2) ohne FA-Tag ausgebucht → die Methode liefert den realen Restbestand
   (Menge 3), nicht mehr die FA-Netto-Summe (die weiterhin 5 wäre, da die Ausbuchung ungetaggt
   ist).
4. Der Einbuchungs-Hinweis (`Inbound.cshtml`) zeigt nach dem Fix in Reproduktionsszenario 1 des
   Bug-Records **keinen** Hinweis mehr an (`faStorageHint` bleibt `display: none`).
5. Der FA-Filter in der Bestandsübersicht (`/StockOverview?filterProductionOrder=1234567`) zeigt
   in Reproduktionsszenario 1 keine Zeile für Artikel A/Lagerplatz X mehr.
6. Bestehende Repository-Tests zu `GetStockByProductionOrderAsync` (falls vorhanden) bleiben grün
   oder werden an das neue, korrekte Verhalten angepasst.

## Test-Szenarien

Ergänzung in `docs/TESTSZENARIEN.md` Kapitel 2 (Lager, TS-2.1–2.21, insbesondere im Umfeld von
TS zum FA-Lagerplatz-Hinweis `einbuchung-fa-autofill`) um ein neues Szenario:

- **Vorbedingung:** Artikel mit bekannter FA-Nummer eingebucht, anschließend ohne FA-Tag wieder
  vollständig ausgebucht.
- **Schritte:** Einbuchungsformular öffnen, dieselbe FA-Nummer eingeben.
- **Erwartetes Verhalten:** Kein „liegt bereits"-Hinweis, da tatsächlicher Bestand 0.
- **Negativfall:** Artikel liegt noch teilweise am Lagerplatz (reale Restmenge > 0) → Hinweis
  erscheint weiterhin, aber mit der **realen** Restmenge, nicht der ursprünglichen Einbuchmenge.
- Zusätzlich: FA-Filter in der Bestandsübersicht und das Lagerbestand-Modal in der
  OSEON-Teileverfolgung mit demselben Szenario gegenprüfen (drei Call-Sites, ein Fix).

`secondbrain/tests/testszenarien-index.md` Kapitel 2 entsprechend nachziehen (Hinweis auf den
korrigierten Bug-Record).

## Deploy

- **Web-App:** ja (Repository-Änderung in `IdealAkeWms`).
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rückfragen

1. Soll ein Kandidat-Lagerplatz (aus FA-getaggten Bewegungen ermittelt) mit tatsächlichem Bestand
   0 komplett aus der Antwort verschwinden (wie bisher bei Netto 0), oder als Info-Zeile mit
   Menge 0 erhalten bleiben („war hier, ist aber weg")? Wirkt sich auf Inbound-Hint,
   StockOverview-FA-Filter und Tracking-Modal gleichermaßen aus.
2. `GetStockByProductionOrderAsync` wird an 3 Stellen verwendet (Inbound-Hint,
   StockOverview-FA-Filter, Tracking-Lagerbestand-Modal). Ist die Verhaltensänderung (nur noch
   realer Bestand statt Bewegungssumme) für ALLE drei Call-Sites fachlich gewünscht, oder soll
   z. B. der StockOverview-FA-Filter bewusst weiter „wo wurde unter dieser FA jemals gebucht"
   (auch historisch/0-Bestand) zeigen können?
3. Performance: Die Kandidaten-Ermittlung + Bestandsberechnung läuft in-memory über ggf. viele
   `StockMovement`-Zeilen je FA. Gibt es eine bekannte Obergrenze an Bewegungen pro FA-Nummer, die
   einen SQL-seitigen statt In-Memory-Ansatz nötig macht?

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →
2. →
3. →
