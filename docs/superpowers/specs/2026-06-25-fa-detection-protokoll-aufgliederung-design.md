# FA-AG-Erkennung: Aktivitäts-Protokoll granular aufgliedern — Design

> Status: Entwurf zur Review
> Datum: 2026-06-25
> Branch: `feature/windows-auth-ad-users` (Worktree `.claude/worktrees/missingparts-include-pd`)
> Ziel-Version: v1.23.0 (zusätzlicher Changelog-Punkt)

## 1. Kontext & Ziel

Der `FaWorkStepDetectionService` (Windows-Service) durchsucht den BOM-Cache: pro aktivem
WorkStep werden die komma-separierten **Suchbegriffe** (`WorkStep.SearchString`) in den
Bezeichnungen der BOM-Items gesucht; trifft ein Begriff, bekommen alle offenen FAs des
betroffenen Artikels den Arbeitsgang (`FaWorkStep`, `Source='Sync'`). Heute protokolliert der
Lauf im Aktivitäts-Protokoll nur zwei Zähler: `neu` + `uebersprungen`.

**Ziel:** Den Lauf granularer protokollieren, damit der Planer
1. die **nicht identifizierten Suchbegriffe** (Begriffe, die in diesem Lauf **null** BOM-Treffer
   hatten) sieht — zur Pflege des Suchbegriff-Katalogs, und
2. je **neu erkanntem FA** sieht, **welcher Begriff** (und welcher AG) die Erkennung ausgelöst
   hat — inkl. **FA-Nummer**.

## 2. Nicht-Ziele (YAGNI)

- **Kein** Datenmodell-/Migrations-Eingriff. Das Aktivitäts-Protokoll (`SyncLog`) trägt Counts +
  Detailzeilen bereits; es kommen nur neue Count-Keys, eine Message-Erweiterung und Info-Zeilen.
- **Keine** Änderung an der Erkennungs-**Logik** selbst (welche FAs erkannt werden). Nur die
  internen Zwischenergebnisse werden feiner geführt und protokolliert.
- **Keine** neue UI-Seite. Die Ausgabe erscheint im bestehenden Aktivitäts-Protokoll
  (`/SyncLog`, Service-Eintrag `FaWorkStepDetection`).
- **Keine** „Begriffs-Vorschläge" (Tokens im BOM, die kein Suchbegriff sind) — das ist ein
  anderes, größeres Thema.

## 3. Architektur & Datenfluss

Umbau in `IDEALAKEWMSService/Services/FaWorkStepDetectionService.DetectAsync`:

### 3.1 Feinere Zwischenergebnisse
Bisher wird pro WorkStep über alle Begriffe eine Artikel-Menge (`matchedArticles`, HashSet)
gebildet — die Zuordnung „welcher Begriff traf welchen Artikel" geht verloren. Neu:
- Pro Begriff die getroffenen Artikel merken: `Dictionary<string term, HashSet<string> articles>`
  (je WorkStep). Daraus ableitbar:
  - **Begriffe ohne Treffer** = Begriffe mit leerer Artikel-Menge.
  - **auslösende Begriffe je Artikel** = invertierte Map (Artikel → Begriffe), für die FA-Zeile.
- Die Kandidaten-Query liefert zusätzlich `OrderNumber` + `ArticleNumber` (nicht nur `Id`), damit
  die FA-Zeile FA-Nummer + auslösenden Begriff nennen kann.

### 3.2 Protokoll-Ausgabe (3 Teile)

**(a) Zusatz-Zähler** in `FinishSuccessAsync(counts)` — zu den bestehenden `neu`/`uebersprungen`:
- `suchbegriffe gesamt` — Anzahl verarbeiteter (WorkStep, Begriff)-Kombinationen.
- `mit treffer` — Kombinationen mit ≥ 1 getroffenem Artikel.
- `ohne treffer` — Kombinationen mit 0 getroffenen Artikeln.

**(b) Nicht gefundene Begriffe** — als **kompakte Liste in der Lauf-Zusammenfassungs-Message**
(`messageSuffix`), **eine** Zeile pro Lauf (kein Zeilen-Spam bei 15-Min-Intervall):
- Format: `Ohne Treffer: <begriff> (<AGCode>), <begriff> (<AGCode>), …`
- Nur, wenn es welche gibt; sonst kein Suffix.
- Cap: maximal 50 Begriffe gelistet, danach `… (+N weitere)`, damit die Message nicht ausufert.

**(c) Je NEU erkanntem FA** eine **Info-Detailzeile** (`LogInfoAsync`):
- Format: `FA <OrderNumber> → AG <Code> <Name> erkannt (Begriff: <term[, term2]>)`
- `reference` = `OrderNumber`.
- **Nur** für tatsächlich neu hinzugefügte `FaWorkStep`-Zeilen (NICHT die bereits vorhandenen /
  `uebersprungen`). Dadurch im eingeschwungenen Zustand 0 Zeilen → kein Spam; Zeilen erscheinen
  nur bei echter Neu-Erkennung. Bei mehreren auslösenden Begriffen werden sie komma-getrennt
  genannt.

### 3.3 Schweregrad & Vorbehalt
- (b) und (c) sind **Info** (kein Warning). Begründung/Vorbehalt: „ohne Treffer" heißt nicht
  zwingend „falscher Begriff" — die passenden Artikel können auch nur außerhalb des aktuellen
  **BOM-Cache-Fensters** (`Sync:BomCacheWeeks` / `Sync:BomCacheMaxOrders`) liegen. Daher neutral.
  Dieser Vorbehalt wird in Hilfe + CLAUDE.md festgehalten.

### 3.4 DryRun
Im DryRun werden dieselben Counts + Liste + Info-Zeilen erzeugt (die Zeilen beschreiben, was
erkannt **würde**); der Run trägt wie bisher `[DryRun]` im Message-Suffix und persistiert nichts.

## 4. Counts-Semantik (Vokabular)
Konsistent zur bestehenden deutschsprachigen Counts-Konvention (`neu`/`uebersprungen`):
`suchbegriffe gesamt`, `mit treffer`, `ohne treffer`. (Klein, ohne Sonderzeichen — passt zum
`"key=value, key=value"`-Renderer von `FinishSuccessAsync`.)

## 5. Tests
**Service-Unit-Tests** (`IDEALAKEWMSService.Tests`, InMemory-DbContext + `FakeSyncLogger`):
- Begriff ohne BOM-Treffer → erscheint in `ohne treffer`-Count UND in der `messageSuffix`-Liste;
  `mit treffer` zählt nur die treffenden Begriffe; `gesamt` = Summe.
- Neu erkannter FA → genau **eine** `LogInfoAsync`-Zeile mit FA-Nummer, AG-Code und auslösendem
  Begriff; `reference` = OrderNumber.
- Bereits vorhandener (oder `IsRemoved`) FaWorkStep → **keine** Info-Zeile (nur `uebersprungen`-Count).
- Mehrere auslösende Begriffe für denselben FA → komma-getrennt in einer Zeile.
- DryRun → Counts/Liste/Zeilen vorhanden, aber keine DB-Persistenz.

> Voraussetzung: `FakeSyncLogger`/`FakeSyncRun` (Test-Helper aus der SyncLog-Infrastruktur) erfasst
> `LogInfoAsync`-Aufrufe + die `FinishSuccessAsync`-Counts + `messageSuffix`. Falls eine
> Erfassungs-Lücke besteht, wird der Fake im Plan minimal erweitert.

## 6. Doku & Versionierung
- `IDEALAKEWMSService`/`IdealAkeWms` AppVersion unverändert (v1.23.0); `Changelog.cshtml`-Bullet.
- `CLAUDE.md`: Fallstrick-Eintrag zum `FaWorkStepDetectionService` um die neue Protokoll-Ausgabe +
  den Cache-Fenster-Vorbehalt ergänzen.
- `docs/TESTSZENARIEN.md`: kurzes Szenario (Lauf auslösen → Protokoll prüfen).
- `Views/Help/Index.cshtml` (Aktivitäts-Protokoll/FA-Erkennung): kurzer Hinweis auf die neue
  Aufgliederung + Vorbehalt.
- `PROJECT_STATUS.md`: kurzer Eintrag.

## 7. Risiken / offene Punkte
- **Mehr BOM-Queries vermeiden:** Die getroffenen Artikel je Begriff werden aus der ohnehin
  ausgeführten Per-Begriff-Query gewonnen (keine zusätzlichen DB-Roundtrips gegenüber heute).
- **Message-Länge:** Cap (50 Begriffe + „+N weitere") schützt vor übergroßer End-Message.
- **Volumen der Info-Zeilen:** bewusst nur bei Neu-Erkennung → bei stabilem Cache 0 Zeilen/Lauf;
  ein einmaliger Burst beim erstmaligen Befüllen ist akzeptabel.
