---
type: spec
title: "Bugfix: FA-Lagerplatz-Hinweis auf tatsächlichen Bestand statt Bewegungssaldo umstellen"
slug: 2026-08-05-wms-bugs-improvements-teil-1-spec
status: Gemerged
created: 2026-08-05
updated: 2026-08-06
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: ""
task: "[[2026-08-05-deploy-wms-bugs-teil-1-2-3]]"
worktree: ".claude/worktrees/2026-08-05-wms-bugs-improvements-teil-1-2-3"
branch: "feature/2026-08-05-wms-bugs-improvements-teil-1-2-3"
affected_code:
  - IdealAkeWms/Data/Repositories/StockMovementRepository.cs
  - IdealAkeWms/Data/Repositories/IStockMovementRepository.cs
  - IdealAkeWms.Tests/Repositories/StockMovementRepositoryTests.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "ENTSCHIEDEN (Schranke 1): 0-Bestand-Kandidat verschwindet komplett — fuer den onlyActualStock=true-Pfad (Inbound-Hint + Tracking-Modal) nur Ist-Bestand > 0 zeigen. Keine offene Rueckfrage mehr."
  - "ENTSCHIEDEN (Schranke 1, Variante B): differenziertes Verhalten je View. onlyActualStock=true fuer Inbound-Hint + Tracking-Modal; StockOverview-FA-Filter ruft explizit mit false und bleibt historisch (heutiges Verhalten). Keine offene Rueckfrage mehr."
  - "Kein Blocker, nur Dev-Lauf-Messhinweis: Kandidaten-Ermittlung + Bestandsberechnung laeuft in-memory ueber ggf. viele StockMovement-Zeilen je FA. Muster existiert bereits in GetCurrentStockAsync; im Dev-Lauf gegen reale Datenmengen messen und bei Bedarf auf SQL-seitige Summierung umstellen."
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

**Bewusst akzeptierte Ungenauigkeit (Mengen-Semantik):** Der Hinweis + das Tracking-Modal zeigen
nach dem Fix als Menge den **realen Ist-Bestand am Artikel/Lagerplatz-Paar**, nicht den FA-genauen
Anteil — Bestand ist nach der Buchung nicht mehr FA-attributiert. Liegt am selben Platz Bestand
eines anderen FA oder ungetaggter Bestand, ist die angezeigte Menge der Platz-Bestand, nicht der
FA-Anteil. Das ist gewollt und weiterhin deutlich besser als der heutige Phantom-Bestand. Der
**historische Pfad** (StockOverview-FA-Filter, `onlyActualStock=false`) bleibt bewusst unverändert
und zeigt weiterhin die FA-getaggte Netto-Summe — **inklusive** der bekannten Phantom-Menge bei
komplett ausgebuchten FAs; das ist für die Liste „Artikelbestände" akzeptiert.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Korrektur von `StockMovementRepository.GetStockByProductionOrderAsync` um einen Parameter
  `bool onlyActualStock = true`. Im `true`-Pfad entspricht die zurückgegebene Menge dem
  **tatsächlichen aktuellen Bestand** am jeweiligen Artikel/Lagerplatz-Paar (nicht dem Netto-Saldo
  der FA-getaggten Bewegungen allein), und Kandidaten mit Ist-Bestand `<= 0` fallen komplett raus.
- **Differenziertes Verhalten je Aufrufstelle (Variante B, Schranke-1-Entscheidung):**
  - Einbuchungs-Hinweis (`/api/stock/by-order/{fa}` → `Inbound.cshtml`) und
    Tracking-Lagerbestand-Modal (`OseonIndex.cshtml`) laufen über `StockApiController` und rufen mit
    **Default `onlyActualStock: true`** → nur Kandidaten mit Ist-Bestand `> 0`.
  - Der Bestandsübersicht-FA-Filter (`StockOverviewController.Index` → Liste „Artikelbestände")
    ruft **explizit `onlyActualStock: false`** und bleibt damit **bewusst unverändert** (historisch:
    „wo wurde je unter dieser FA gebucht", FA-getaggte Netto-Summe wie heute).
- Repository-Tests für **beide** Pfade: den korrigierten `true`-Pfad (Reproduktionsszenario aus dem
  Bug-Record: Einbuchung mit FA-Tag, Ausbuchung ohne FA-Tag → Ergebnis leer/0) **und** den
  Regressionsschutz für `false` (bit-identisch zum heutigen Verhalten).

**Out-of-Scope**
- Keine Änderung an `StockMovementCreateViewModel.ProductionOrder` (bleibt optional — ein
  Pflichtfeld auf allen drei Buchungsformularen wäre ein eigener, weitreichenderer
  Verhaltenswechsel und nicht Teil dieses Bugfixes).
- Keine Änderung am UI-Text/Design des Hinweises selbst (`Inbound.cshtml` Zeilen 190-270) — nur
  die zugrunde liegenden Daten ändern sich.
- Keine Änderung an `GetCurrentStockAsync` (unverändert korrekt für die normale Bestandsübersicht
  ohne FA-Filter).

## Fachliche Anforderungen

1. Der FA-Lagerplatz-Hinweis (Einbuchung) und das Tracking-Lagerbestand-Modal (`onlyActualStock=true`)
   zeigen ausschließlich Artikel/Lagerplatz-Kombinationen, an denen **aktuell** ein positiver Bestand
   (`> 0`) liegt.
2. Die Ermittlung „welche Lagerplätze könnten zu diesem FA gehören" bleibt weiterhin über das
   `ProductionOrder`-Tag der Bewegungen (Kandidatensuche) — im `true`-Pfad wechselt nur die
   **Mengenberechnung** je Kandidat von „Summe der FA-getaggten Bewegungen" auf „Summe aller
   Bewegungen an diesem Artikel/Lagerplatz-Paar" (= echter Bestand, analog `GetCurrentStockAsync`),
   und Kandidaten mit Ist-Bestand `<= 0` werden nicht mehr zurückgegeben.
3. Der `false`-Pfad (StockOverview-FA-Filter, Liste „Artikelbestände") behält das **heutige**
   Verhalten unverändert bei: FA-getaggte Netto-Summe, auch für historische/0-Bestand-Zeilen.

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

`GetStockByProductionOrderAsync` erhält einen Parameter
`bool onlyActualStock = true` (Signatur-Erweiterung in `IStockMovementRepository` **und**
`StockMovementRepository`) und wird zweistufig umgebaut:

1. **Kandidaten-Ermittlung** (wie bisher, in beiden Pfaden): alle
   `(ArticleId, StorageLocationId)`-Paare, an denen irgendeine Bewegung mit
   `ProductionOrder.Contains(productionOrder)` existiert.
2. **Mengenberechnung je Kandidat — pfadabhängig:**
   - `onlyActualStock == true`: für exakt diese Paare den tatsächlichen Bestand über **alle**
     Bewegungen berechnen — analog der bereits vorhandenen Aggregationslogik in
     `GetCurrentStockAsync` (Zeilen 13-188, Einbuchung/SageEinbuchung/Umbuchung-Ziel positiv,
     Ausbuchung/SageAusbuchung negativ, Umbuchung-Quelle subtrahiert) bzw. per Wiederverwendung/
     Refactoring eines gemeinsamen privaten Hilfsbausteins, um die Aggregationsregel nicht ein
     drittes Mal zu duplizieren (Fallstrick „`MovementType`-Erweiterung trifft 6 Stellen" beachten —
     ein neuer, siebter Ort mit eigener Kopie der Switch-Logik ist zu vermeiden). Anschließend nur
     Paare mit Bestand `> 0` zurückgeben (Schranke-1-Entscheidung „komplett weg").
   - `onlyActualStock == false`: **exakt das heutige Verhalten** — Summe **nur** der FA-getaggten
     Bewegungen je Paar; Zeilen mit Netto 0 fallen weiterhin wie bisher heraus. Diese Zweig-Logik
     bleibt bit-identisch zum Ist-Zustand (Regressionsschutz).
3. **Call-Sites:**
   - `StockApiController` (Einbuchungs-Hinweis + Tracking-Modal) ruft mit **Default `true`** —
     keine Änderung an der Aufrufzeile nötig (Default greift).
   - `StockOverviewController.Index` (FA-Filter) ruft **explizit `onlyActualStock: false`** und
     behält damit das historische Verhalten.
   - Rückgabetyp (`List<StockOverviewItem>`) bleibt unverändert.

Der Parameter-Default `true` stellt sicher, dass jeder künftige/übersehene Aufrufer den korrekten,
Ist-Bestand-basierten Pfad bekommt; nur der bewusst historische StockOverview-Aufruf opt-t via
`false` aus.

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
   (Default `onlyActualStock: true`) liefert für Artikel A/Lagerplatz X **keine Zeile** mehr
   (Ist-Bestand `> 0` ist die harte Bedingung; bei Ist-Bestand `<= 0` erscheint keine Zeile).
2. Artikel A wird mit FA-Tag `1234567` auf Lagerplatz X eingebucht (Menge 5) und bleibt dort
   unverändert liegen → `GetStockByProductionOrderAsync("1234567")` (`true`) liefert weiterhin
   Artikel A/Lagerplatz X mit Menge 5 (Regressionsschutz: der Normalfall funktioniert wie bisher).
3. Artikel A wird mit FA-Tag `1234567` auf Lagerplatz X eingebucht (Menge 5) und anschließend
   **teilweise** (Menge 2) ohne FA-Tag ausgebucht → die Methode (`true`) liefert den realen
   Restbestand (Menge 3), nicht mehr die FA-Netto-Summe (die weiterhin 5 wäre, da die Ausbuchung
   ungetaggt ist).
4. Der Einbuchungs-Hinweis (`Inbound.cshtml`) zeigt nach dem Fix in Reproduktionsszenario 1 des
   Bug-Records **keinen** Hinweis mehr an (`faStorageHint` bleibt `display: none`).
5. Der FA-Filter in der Bestandsübersicht (`/StockOverview?filterProductionOrder=1234567`, Aufruf
   mit `onlyActualStock: false`) zeigt in Reproduktionsszenario 1 die Zeile für Artikel A/Lagerplatz
   X **weiterhin** an — das historische Verhalten dieser Liste bleibt **unverändert** (Variante B).
6. **Regressions-Kriterium (historischer Pfad):** Für `onlyActualStock: false` ist die Rückgabe von
   `GetStockByProductionOrderAsync` **bit-identisch** zum heutigen Ist-Zustand (gleiche Zeilen,
   gleiche Mengen als FA-getaggte Netto-Summe, inkl. bekannter Phantom-Menge bei komplett
   ausgebuchten FAs). Der StockOverview-FA-Filter ist damit nachweislich unverändert.
7. **Mengen-Semantik am gemischten Platz (`true`-Pfad):** Liegt am selben Lagerplatz X zusätzlich
   Bestand eines anderen FA oder ungetaggter Bestand, ist die für Artikel A/Lagerplatz X angezeigte
   Menge der **reale Platz-Ist-Bestand**, nicht der FA-Anteil (bewusst akzeptierte Ungenauigkeit —
   Bestand ist nach der Buchung nicht FA-attributiert).
8. Bestehende Repository-Tests zu `GetStockByProductionOrderAsync` (falls vorhanden) bleiben grün
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
- Zusätzlich (Variante B, differenziert): Das **Lagerbestand-Modal** in der OSEON-Teileverfolgung
  (`true`-Pfad) mit demselben Szenario gegenprüfen → verhält sich wie der Einbuchungs-Hinweis
  (keine Zeile bei Ist-Bestand 0). Der **FA-Filter in der Bestandsübersicht** („Artikelbestände",
  `false`-Pfad) hingegen zeigt die Zeile **weiterhin** an (historisch, unverändert) — das ist der
  erwartete Regressions-Nachweis, kein Fehler.

`secondbrain/tests/testszenarien-index.md` Kapitel 2 entsprechend nachziehen (Hinweis auf den
korrigierten Bug-Record).

## Deploy

**Finalisiert durch QA (2026-08-05) anhand des echten Diffs im gemeinsamen Worktree
`.claude/worktrees/2026-08-05-wms-bugs-improvements-teil-1-2-3` (Branch
`feature/2026-08-05-wms-bugs-improvements-teil-1-2-3`, geteilt mit Teil 2 + Teil 3).**

- **Web-App:** ja (`StockMovementRepository.cs`, `IStockMovementRepository.cs`,
  `StockOverviewController.cs` — Teil des gemeinsamen Diffs).
- **Service:** nein (nur `IDEALAKEWMSService/AppVersion.cs` Versions-Bump auf 1.29.0, keine
  funktionale Service-Änderung).
- **Migration:** nein — `git diff main --stat` gegen den Worktree bestätigt: keine Datei unter
  `*/Migrations/` oder `SQL/` verändert.
- **Publish-Befehle (aus dem Worktree, VOR dem Merge):**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

Fluss: Publish aus dem Worktree → Testsystem → manueller Test (Schranke 2) → danach Merge. Der
Worktree ist mit Teil 2 und Teil 3 geteilt (ein gemeinsamer Branch) — ein einziger Publish deckt
alle drei Teile ab. **Hinweis:** Nach dem Merge nur dann erneut aus `main` publishen, wenn der
Merge tatsächlich getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.

## Offene Rückfragen

Alle fachlichen Rückfragen sind mit Schranke 1 entschieden — keine offene Blocker-Frage mehr.

1. **ENTSCHIEDEN — „komplett weg":** Ein Kandidat-Lagerplatz mit tatsächlichem Bestand `<= 0`
   verschwindet im `true`-Pfad (Inbound-Hint + Tracking-Modal) komplett aus der Antwort; es wird
   nur Ist-Bestand `> 0` gezeigt.
2. **ENTSCHIEDEN — Variante B (differenziert):** `true`-Pfad (Inbound-Hint + Tracking-Modal) nur
   realer Bestand; StockOverview-FA-Filter (Liste „Artikelbestände") ruft mit `false` und bleibt
   bewusst historisch („wo wurde je unter dieser FA gebucht"). Die **Bewegungshistorie**
   (`GetMovementHistoryAsync`) ist von diesem Fix nicht betroffen.
3. **Kein Blocker — Dev-Lauf-Messhinweis:** Die Kandidaten-Ermittlung + Bestandsberechnung läuft
   in-memory über ggf. viele `StockMovement`-Zeilen je FA. Dasselbe Muster existiert bereits in
   `GetCurrentStockAsync`. Im Dev-Lauf gegen reale Datenmengen messen und bei Bedarf auf eine
   SQL-seitige Summierung umstellen — keine Rückfrage an den Menschen nötig.

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →ich glaube komplett weg - sinnvoll?
2. →in der bewegungsübersicht sinnvoll wenn angezeigt wird, aber in anderen views nicht.
3. →bitte selbst prüfen

## Kritische Pruefung (2026-08-05)

Anwalt-des-Teufels-Durchsicht vor der Freigabe. Geprueft gegen Spec, Backlog
`[[2026-08-05-WmsBugs&Improvements]]`, Bug-Record, und den echten Code
(`StockMovementRepository.GetStockByProductionOrderAsync`, `StockApiController`,
`StockOverviewController`, `StockMovementsController`). Der Bugfix an sich ist gut recherchiert und
die Root Cause stimmt — **aber die Freigabe-Antworten 1 und 2 reissen den Entwurf auf.**

### BLOCKER — vor der Freigabe zu klaeren

**B1 — Antwort 2 widerspricht dem Ein-Methoden-Entwurf UND nennt eine View, die es so nicht gibt.**
Antwort 2: „in der bewegungsübersicht sinnvoll wenn angezeigt wird, aber in anderen views nicht."
Am Code verifiziert:
- `GetStockByProductionOrderAsync` hat **zwei** Code-Aufrufer: `StockApiController.cs:25` (bedient
  **beide** UI-Flaechen — Einbuchungs-Hinweis in `Inbound.cshtml` **und** das Tracking-Lagerbestand-Modal)
  und `StockOverviewController.cs:51` (FA-Filter der Liste **„Artikelbestände"**, `Views/StockOverview/Index.cshtml:6`).
- Eine View namens „**Bewegungsübersicht**" existiert nicht. Die **Bewegungshistorie**
  (`StockMovementsController.Index`) nutzt eine **andere** Methode (`GetMovementHistoryAsync`) und wird
  von diesem Fix **gar nicht** beruehrt.
- Der Entwurf sagt ausdruecklich „rein in der Repository-Methode gekapselt, **keine Signaturaenderung**"
  (Loesungsentwurf Punkt 4) — das aendert das Verhalten **aller** Aufrufer **gleich**. Die Antwort will
  aber **differenziertes** Verhalten (eine View historisch, die anderen nur Ist-Bestand). Das ist mit
  einer einzigen, unparametrisierten Methode **nicht** moeglich.

  **Konsequenz + Frage an den Menschen:** Bitte praezisieren, welche View konkret gemeint ist:
  (a) Meinst du die **Bewegungshistorie**? Dann ist die Antwort gegenstandslos — sie zeigt ohnehin
  einzelne Bewegungen (nicht Bestand) und wird nicht angefasst; der Fix betrifft sie nicht.
  (b) Meinst du die Liste **„Artikelbestände"** (StockOverview-FA-Filter)? Dann braucht es
  **differenziertes** Verhalten: Einbuchungs-Hinweis + Tracking-Modal → nur Ist-Bestand;
  „Artikelbestände"-FA-Filter → weiter „wo wurde je unter der FA gebucht" (historisch). Der Entwurf
  muss dann von „keine Signaturaenderung" auf einen **Parameter** (z. B. `bool onlyActualStock`) oder
  eine **zweite Methode** umgestellt werden, und **Akzeptanzkriterium 5** (StockOverview zeigt keine
  Zeile mehr) ist dann **falsch** und muss invertiert werden.
  **Empfehlung:** Variante (b) mit Parameter; Default `onlyActualStock: true`, StockOverview-Aufruf
  ruft bewusst mit `false` (historisch). So bleibt der eigentliche Bug (Einbuchungs-Hinweis) gefixt,
  ohne die Bestandsliste umzudeuten.
  ANTWORT: Variante B

**B2 — Antwort 1 ist keine Entscheidung, sondern eine Rueckfrage.** „ich glaube komplett weg -
sinnvoll?" beantwortet die Entweder-oder-Frage (0-Zeile behalten vs. ganz weg) **nicht** verbindlich,
sondern gibt sie an mich zurueck.
  **Empfehlung (bitte bestaetigen):** Fuer den **Einbuchungs-Hinweis** und das **Tracking-Modal** ist
  „komplett weg" (nur Ist-Bestand `> 0` zeigen) richtig — ein Hinweis „liegt bereits" auf einen Platz
  mit realem Bestand 0 waere genau der Bug, den wir beheben. Also: Kandidaten mit Ist-Bestand `<= 0`
  fallen raus. Bitte diese Formulierung als verbindliche Antwort setzen (dann wird AK1 eindeutig).
ANTWORT: „komplett weg" (nur Ist-Bestand `> 0` zeigen) richtig 
### SOLLTE — macht den Dev-Lauf sicherer

**S1 — Die angezeigte Menge ist der Ist-Bestand am Artikel/Lagerplatz-Paar, NICHT FA-spezifisch.**
Nach dem Fix werden die Kandidaten-Paare zwar ueber das FA-Tag ermittelt, die Menge aber ueber
**alle** Bewegungen an diesem Paar berechnet. Liegt am selben Lagerplatz Bestand eines **anderen** FA
(oder ungetaggter Bestand), zeigt der Hinweis „FA X liegt bereits: Platz — Menge N", obwohl N nicht
(nur) zu FA X gehoert. Das ist der Natur der Sache geschuldet (Bestand ist nach der Buchung nicht
FA-attributiert) und immer noch besser als der Phantom-Bestand von heute — aber der Spec-Text sollte
diese **bewusste Ungenauigkeit** benennen (Ziel/Nutzen + ein Akzeptanzkriterium „gemischter Platz:
Menge = realer Platz-Bestand, nicht FA-Anteil"), damit niemand spaeter eine FA-genaue Menge erwartet.

**S2 — Regressions-Akzeptanzkriterium fuer die NICHT umgestellte View fehlt.** Sobald B1(b)
entschieden ist, braucht es ein hartes Kriterium „Artikelbestände-FA-Filter zeigt weiterhin
historische/0-Bestand-Zeilen (Verhalten unveraendert)" bzw. — bei Entscheidung (a)/gegen
Differenzierung — „alle drei Flaechen zeigen identisch nur Ist-Bestand". Aktuell behaupten AK4/AK5
implizit die eine, Antwort 2 die andere Richtung.

### HINWEIS — Beobachtung ohne Handlungszwang

**H1 — Kandidatensuche nutzt `ProductionOrder.Contains(productionOrder)` (Teilstring).** FA „123"
matcht auch „1234567" oder „…-123…". Vorbestehend und nicht Gegenstand dieses Bugfixes, aber
verwandt mit Teil-3 (WA-Kuerzung) — dort ggf. mitdenken, ob die FA-Zuordnung praeziser werden soll.

**H2 — „drei Aufrufstellen" sind zwei Code-Call-Sites.** `StockApiController` bedient zwei
UI-Flaechen; die Formulierung „drei" meint UI-Flaechen, nicht Methoden — fachlich ok, nur zur
Klarstellung.

**H3 — Performance (Antwort 3 „bitte selbst pruefen"):** vertretbar. Das Muster (In-Memory-Aggregation
ueber `StockMovement`) existiert bereits in `GetCurrentStockAsync`; der Dev-Lauf misst gegen reale
Datenmengen und stellt bei Bedarf auf eine SQL-seitige Summierung um. Kein Blocker.

### Empfehlung

**NACHBESSERUNG NOETIG: Antwort 2 (differenziertes Verhalten je View) ist mit dem „keine
Signaturaenderung"-Entwurf unvereinbar und nennt eine nicht existierende View — bitte View praezisieren
und Parameter/zweite-Methode entscheiden; Antwort 1 ist noch keine verbindliche Entscheidung.**

## Kritische Pruefung — 2. Durchgang (2026-08-05)

Der Mensch hat die beiden Blocker inline beantwortet: **B1 → „Variante B"**, **B2 → „komplett weg
(nur Ist-Bestand > 0)"**. Beide Entscheidungen sind damit gefallen und eindeutig. Prueferisch bleibt
**ein** Riss — genau die Art „Antwort sagt X, Spec-Body sagt noch Y", vor der die Pruefung warnt.

### BLOCKER — vor der Freigabe zu schliessen

**B3 — Der Spec-BODY beschreibt weiterhin den verworfenen Ein-Methoden-Fix und widerspricht damit
Variante B.** Konkret unveraendert-falsch, jetzt wo Variante B gilt:
- **In-Scope** (Body): „korrigiert sich automatisch das Verhalten an **allen drei** Aufrufstellen" —
  falsch: die Liste „Artikelbestände" (StockOverview-FA-Filter) soll **bewusst unveraendert**
  (historisch) bleiben.
- **Loesungsentwurf Punkt 4:** „der Fix ist rein in der Repository-Methode gekapselt, **keine
  Signaturaenderung** noetig" — mit Variante B **doch** noetig (Parameter oder zweite Methode).
- **Akzeptanzkriterium 5:** „FA-Filter in der Bestandsuebersicht zeigt **keine Zeile** mehr" —
  unter Variante B genau **falsch herum**: die Zeile MUSS dort **weiterhin** erscheinen.
- **Akzeptanzkriterium 4** (Inbound-Hint zeigt keinen Hinweis mehr) bleibt korrekt.

  **Auftrag an den Dev-Lauf / Bitte um kurze Body-Angleichung vor der Freigabe:**
  `GetStockByProductionOrderAsync(string productionOrder, bool onlyActualStock = true)` —
  `StockApiController` (Inbound-Hint + Tracking-Modal) ruft mit **default `true`** (nur Ist-Bestand
  `> 0`); `StockOverviewController` ruft **explizit mit `false`** und behaelt das heutige Verhalten
  (historisch, „wo wurde je unter der FA gebucht"). In-Scope, Loesungsentwurf Punkt 4 und **AK5
  invertieren** (AK5 neu: „…zeigt die Zeile **weiterhin** an, Verhalten unveraendert"). Zusaetzlich
  ein **Regressions-AK**: „Fuer `onlyActualStock=false` ist die Rueckgabe von
  `GetStockByProductionOrderAsync` **bit-identisch** zum Ist-Zustand (StockOverview-FA-Filter
  unveraendert)."
  > Terminologie-Bruecke fuer den Dev-Lauf: „bewegungsübersicht" in Antwort 2 = die Liste
  > **„Artikelbestände"** (StockOverview), die einzige View, die historisch bleibt. Die
  > **Bewegungshistorie** (`GetMovementHistoryAsync`) ist NICHT betroffen.
bitte den bestmöglichen ansatz durchführen.
### SOLLTE

**S1 (unveraendert gueltig) — Mengen-Ungenauigkeit am gemischten Platz.** Fuer den `true`-Pfad ist die
angezeigte Menge der **Platz-Ist-Bestand**, nicht der FA-Anteil (Bestand ist nach der Buchung nicht
FA-attributiert). Bitte in Ziel/Nutzen benennen und als Akzeptanzkriterium fixieren („gemischter
Platz: Menge = realer Platz-Bestand"). Kein Blocker, aber verhindert falsche Erwartungen im UAT.

**S3 — Quantity-Semantik des historischen Pfads (`false`) definieren.** „Historisch/unveraendert"
heisst konkret: **heutiges** Verhalten beibehalten = FA-getaggte Netto-Summe (inkl. der bekannten
Phantom-Menge bei komplett ausgebuchten FAs). Das ist fuer die Liste „Artikelbestände" akzeptiert,
sollte aber **explizit** so im Body stehen, damit niemand spaeter die Phantom-Menge dort erneut als
Bug meldet.

### Empfehlung (2. Durchgang)

**NACHBESSERUNG NOETIG (nur noch redaktionell/klein): Entscheidungen stehen (Variante B + „nur >0"),
aber Body (In-Scope, Loesungsentwurf Punkt 4, AK4/AK5 + Regressions-AK) muss an Variante B angeglichen
werden — sonst baut/testet der Dev-Lauf gegen ein AK5, das der getroffenen Entscheidung genau
widerspricht.** Danach ist Teil-1 ein kleiner, sauberer Web-only-Dev-Lauf (eine Repository-Methode +
ein Aufruf-Flag + Tests, keine Migration).

## Finalisierung (2026-08-05)

Der Body wurde an die Schranke-1-Entscheidungen (Variante B, „nur Ist-Bestand > 0") angeglichen und
widerspruchsfrei gemacht. Auflösung je Punkt:

- **B1 / Antwort 2 (Variante B, differenziertes Verhalten):** `GetStockByProductionOrderAsync`
  bekommt Parameter `bool onlyActualStock = true`. `StockApiController` (Einbuchungs-Hinweis +
  Tracking-Modal) ruft mit Default `true` (nur Ist-Bestand `> 0`); `StockOverviewController.Index`
  ruft explizit mit `false` und behält das heutige, historische Verhalten. In-Scope,
  Fachliche Anforderungen (Punkt 2 + neuer Punkt 3) und Lösungsentwurf (Punkte 1–3 statt des alten
  „keine Signaturänderung"-Punkts 4) entsprechend umgeschrieben.
- **B2 / Antwort 1 („komplett weg"):** Für den `true`-Pfad fallen Kandidaten mit Ist-Bestand `<= 0`
  raus; nur `> 0` wird gezeigt. AK1 auf „keine Zeile bei `<= 0`" festgenagelt (kein „Menge 0"-Rest).
- **AK5 invertiert:** StockOverview-FA-Filter zeigt die Zeile unter Variante B **weiterhin** an
  (Verhalten unverändert) — vorher fälschlich „keine Zeile mehr".
- **Neues Regressions-AK (AK6):** Für `onlyActualStock=false` ist die Rückgabe bit-identisch zum
  heutigen Ist-Zustand (StockOverview-FA-Filter nachweislich unverändert).
- **Neues Mengen-Semantik-AK (AK7) + S1:** Gemischter Platz → angezeigte Menge = realer
  Platz-Ist-Bestand, nicht FA-Anteil (bewusst akzeptierte Ungenauigkeit, in Ziel/Nutzen benannt).
- **S3 (Semantik historischer Pfad):** In Ziel/Nutzen, In-Scope und AK6 explizit festgehalten, dass
  der `false`-Pfad die FA-getaggte Netto-Summe inkl. bekannter Phantom-Menge behält — bewusst
  akzeptiert, kein künftiger Bug-Report.
- **AK4:** unverändert korrekt gelassen (Inbound-Hint zeigt keinen Hinweis mehr).
- **Offene Fragen (Frontmatter + Body):** 1 und 2 als entschieden vermerkt; Performance (3) bleibt
  als Dev-Lauf-Messhinweis ohne Rückfrage an den Menschen.
- **Terminologie:** „Bewegungsübersicht" (Antwort 2) = Liste „Artikelbestände" (StockOverview);
  die Bewegungshistorie (`GetMovementHistoryAsync`) ist nicht betroffen — im Body klargestellt.

Nicht geändert (bewusst): `status` bleibt `Entwurf`, Datei-Ablage unverändert, der Block
„## Freigabe-Antworten" und beide „## Kritische Pruefung"-Abschnitte unangetastet, kein
Anwendungscode berührt, kein Commit.

BEREIT ZUR FREIGABE

## QA-Nachweis (2026-08-05)

Verifikation im Worktree `.claude/worktrees/2026-08-05-wms-bugs-improvements-teil-1-2-3`
(gemeinsamer Branch mit Teil 2 + Teil 3, HEAD `a1573d4`).

**Build:**
```
dotnet build IdealAkeWms.slnx
...
Der Buildvorgang wurde erfolgreich ausgeführt.
    9 Warnung(en)
    0 Fehler
```
(Warnungen: 8× NU1902 vorbestehende MailKit/MimeKit-Advisories, 1× CS8602 in
`TrackingController.cs` — beide vorbestehend, nicht durch diese Specs verursacht.)

**Tests — Web (`IdealAkeWms.Tests`):**
```
Bestanden! : Fehler: 0, erfolgreich: 1074, übersprungen: 1, gesamt: 1075
```
Neue Repository-Tests zum true-/false-Pfad in
`IdealAkeWms.Tests/Repositories/StockMovementRepositoryProductionOrderTests.cs` (6 Fälle, nicht 7
wie ursprünglich grob geschätzt — deckt AK1/2/3/6/7 sowie den false-Pfad-Gegenfall mit rein
FA-Anteil ab):
`GetStockByProductionOrder_TruePath_TaggedInThenUntaggedOut_ReturnsEmpty` (AK1),
`_TruePath_TaggedInOnly_ReturnsFullQuantity` (AK2),
`_TruePath_PartialUntaggedOut_ReturnsRealRemainder` (AK3),
`_TruePath_MixedLocation_ShowsPlaceStockNotFaShare` (AK7),
`_FalsePath_TaggedInThenUntaggedOut_StillShowsRow` (AK6, Regression),
`_FalsePath_MixedLocation_ShowsOnlyFaShare` (Regressions-Gegenprobe).

**Tests — Service (`IDEALAKEWMSService.Tests`):**
```
Bestanden! : Fehler: 0, erfolgreich: 195, übersprungen: 0, gesamt: 195
```

**Diff-Nachweis (kein Migrations-/SQL-Impact):**
```
git diff main --stat -- '*/Migrations/*' 'SQL/*'   → leer
```

**CLAUDE.md-Checkliste (dieser Teil):** Migration/SQL — n/a (keine Schema-Änderung). Audit-Felder —
n/a (reiner Lesepfad, keine neue Schreiblogik). Versions-Bump auf 1.29.0 in beiden `AppVersion.cs`
+ Anwender-Changelog (`Views/Help/Changelog.cshtml`) ergänzt — geteilt mit Teil 2/3, ein
gemeinsamer Release. `docs/TESTSZENARIEN.md` um TS-2.22 ergänzt (inkl. Gegenproben
Bestandsübersicht + Tracking-Modal), `secondbrain/tests/testszenarien-index.md` (Hauptcheckout)
Kapitel 2 nachgezogen.

Ergebnis: **Build 0 Fehler, alle Tests grün — Mindestbedingung erfüllt.**

## Manuelle Test-Checkliste (Schranke 2)

Referenz: `docs/TESTSZENARIEN.md` **TS-2.22 — FA-Hinweis nur bei tatsaechlichem Bestand (v1.29.0,
Teil 1)**.

1. Artikel A mit FA-Tag `1234567` auf Lagerplatz X einbuchen (Menge 5), anschließend **ohne**
   FA-Tag vollständig wieder ausbuchen (Menge 5).
2. Einbuchungsformular (`/StockMovements/Inbound`) öffnen, FA `1234567` eingeben, Feld verlassen.
   **Erwartet:** Kein „FA liegt bereits …"-Hinweis mehr (vorher fälschlich Platz X mit Menge 5).
3. Negativfall: dieselbe FA, aber nur teilweise ungetaggt ausgebucht (z. B. Einbuchung 5,
   Ausbuchung 2 → Rest 3). **Erwartet:** Hinweis erscheint weiterhin, zeigt aber die **reale**
   Restmenge (3), nicht die ursprüngliche Einbuchmenge (5).
4. Gegenprobe Bestandsübersicht (`/StockOverview`, Filter „Fertigungsauftrag" = `1234567` aus
   Schritt 1): Zeile für Artikel A/Lagerplatz X wird **weiterhin** angezeigt — historisches
   Verhalten bewusst unverändert (kein Fehler).
5. Gegenprobe Tracking-Lagerbestand-Modal (OSEON-Teileverfolgung) mit derselben FA aus Schritt 1:
   verhält sich wie der Einbuchungs-Hinweis (keine Zeile bei Ist-Bestand 0).
6. Gemischter Platz (AK7, optional falls Testdaten verfügbar): liegt am selben Lagerplatz
   zusätzlich Bestand eines anderen FA, zeigt der Hinweis den **realen Platz-Ist-Bestand**, nicht
   nur den FA-Anteil — bewusst akzeptierte Ungenauigkeit, kein Fehler.

