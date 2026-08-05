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
