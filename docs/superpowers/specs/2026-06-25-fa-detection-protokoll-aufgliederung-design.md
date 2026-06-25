# FA-AG-Erkennung + BOM-Cache: Aktivitäts-Protokoll granular aufgliedern — Design

> Status: Entwurf zur Review (v2 — um Cache-Abdeckung erweitert)
> Datum: 2026-06-25
> Branch: `feature/windows-auth-ad-users` (Worktree `.claude/worktrees/missingparts-include-pd`)
> Ziel-Version: v1.23.0 (zusätzlicher Changelog-Punkt)

## 1. Kontext & Ziel

Die automatische FA-zu-Arbeitsgang-Erkennung läuft als Kette:
`BomCache-Sync` (cacht die Stücklisten offener FAs im Fenster) → `FaWorkStepDetectionService`
(durchsucht die gecachten Stücklisten nach den Suchbegriffen der WorkSteps). Heute protokolliert
jeder Schritt nur grobe Zähler (`neu`/`aktualisiert`/`uebersprungen`).

**Auslöser:** Bei FA 2604089 / Artikel S1401380 wurde **kein** AG erkannt, obwohl die Stückliste
eindeutig passende Begriffe enthält. Root Cause (debuggt): Der Artikel lag **nicht im BOM-Cache**
(200er-Cap durch überfällige Alt-FAs überschritten) — die Erkennung sah seine Stückliste also nie.
Das war im Protokoll **nicht sichtbar**: weder „diese FA hat keinen Cache-Eintrag" noch „dieser
Begriff hat nichts getroffen".

**Ziel — zwei Transparenz-Erweiterungen im Aktivitäts-Protokoll:**
- **Teil A (FaWorkStepDetection):** je Lauf sichtbar machen, welche **Suchbegriffe nichts getroffen**
  haben und je **neu erkanntem FA**, welcher Begriff/AG ausgelöst hat (inkl. FA-Nummer).
- **Teil B (BomCache):** je Lauf sichtbar machen, **wie viele offene FAs im Fenster der Cap raus
  gedrängt** hat (→ keine Erkennung möglich) und welche Artikel im Fenster **keine BOM-Daten**
  liefern. So fällt ein Fall wie S1401380 sofort auf.

## 2. Nicht-Ziele (YAGNI)

- **Kein** Datenmodell-/Migrations-Eingriff. Das Aktivitäts-Protokoll (`SyncLog`) trägt Counts +
  Detailzeilen (Info/Warning) bereits.
- **Keine** Änderung an der Erkennungs- oder Cache-**Auswahllogik** (welche FAs/Artikel
  erkannt/gecacht werden). NUR feinere Zwischenergebnisse + Protokollierung. (Die `ORDER BY
  ProductionDate ASC`-Auswahl bzw. Cap-Höhe ist ein separates Thema — hier nur sichtbar machen.)
- **Keine** neue UI-Seite. Ausgabe erscheint im bestehenden Aktivitäts-Protokoll (`/SyncLog`,
  Service-Einträge `BomCache` + `FaWorkStepDetection`).
- **Keine** „Begriffs-Vorschläge" (Tokens im BOM, die kein Suchbegriff sind).

## 3. Teil A — FaWorkStepDetection granular

Umbau in `IDEALAKEWMSService/Services/FaWorkStepDetectionService.DetectAsync` (nutzt EF →
InMemory-testbar).

### 3.1 Feinere Zwischenergebnisse
Bisher wird pro WorkStep über alle Begriffe eine Artikel-Menge gebildet — „welcher Begriff traf
welchen Artikel" geht verloren. Neu: pro Begriff die getroffenen Artikel merken
(`term → HashSet<artikel>`, je WorkStep). Daraus ableitbar: Begriffe ohne Treffer + auslösende
Begriffe je Artikel. Die Kandidaten-Query liefert zusätzlich `OrderNumber` + `ArticleNumber`.

### 3.2 Protokoll-Ausgabe
**(a) Zusatz-Zähler** (zu `neu`/`uebersprungen`): `suchbegriffe gesamt`, `mit treffer`,
`ohne treffer` — je (WorkStep, Begriff)-Kombination.

**(b) Nicht gefundene Begriffe** — kompakte Liste in der Lauf-Zusammenfassung (`messageSuffix`),
**1 Zeile/Lauf**: `Ohne Treffer: kühl (VE), foo (VL)`. Cap 50 (`… +N weitere`). Kein Suffix, wenn
keine.

**(c) Je NEU erkanntem FA** eine **Info-Detailzeile** (`LogInfoAsync`):
`FA <OrderNumber> → AG <Code> <Name> erkannt (Begriff: <term[, term2]>)`, `reference` =
`OrderNumber`. **Nur** tatsächlich neu hinzugefügte Zeilen → im eingeschwungenen Zustand 0 Zeilen
(kein Spam). Mehrere auslösende Begriffe komma-getrennt (zeigt False-Positive-Trigger, z. B.
`spange, aufbau, rahmen`).

### 3.3 Schweregrad
(b)+(c) = **Info**. Vorbehalt (Doku): „ohne Treffer" kann auch nur am Cache-Fenster liegen.

### 3.4 DryRun
Gleiche Counts/Liste/Zeilen; Run trägt `[DryRun]`; keine Persistenz.

## 4. Teil B — BomCache Cache-Abdeckung sichtbar machen

Erweiterung in `IDEALAKEWMSService/Services/BomCacheSyncService.SyncBomCacheAsync` (nutzt raw
ADO.NET → **nicht** InMemory-testbar; testbare Logik wird in einen reinen Helper extrahiert).

### 4.1 Zusätzliche Kennzahlen ermitteln
- `ReadOpenOrdersInWindowAsync` liefert zusätzlich die **Gesamtzahl** der im Fenster
  eignungsfähigen offenen FAs (gleiches `WHERE`, aber `COUNT(*)` **ohne** `TOP`), nicht nur die
  `TOP(@max)`-Auswahl. (Eine zusätzliche, billige Count-Query.)
- Im bestehenden Artikel-Loop wird mitgezählt, für welche Artikel SAGE **und** OSEON **keine**
  BOM-Items liefern (`items.Count == 0` → heute `continue`).

### 4.2 Protokoll-Ausgabe
**(a) Zusatz-Zähler** (zu `neu`/`aktualisiert`/`uebersprungen`):
`fa im fenster` (Gesamt eignungsfähig), `fa gecacht` (= `TOP`-Auswahl), `artikel ohne bom`
(Artikel im Fenster ohne BOM-Daten).

**(b) Cap-Warnung** (`LogWarningAsync`) **nur wenn** `fa im fenster > Cap`:
`Cap erreicht: <fa gecacht> von <fa im fenster> offenen FAs gecacht (Cap <max>) — <diff> FAs
ohne Cache-Eintrag, werden NICHT automatisch erkannt.` Das ist das Signal, das S1401380 sofort
sichtbar gemacht hätte.

**(c) Artikel-ohne-BOM-Hinweis** (`LogWarningAsync`) **nur wenn** `artikel ohne bom > 0`:
kompakte, gecappte Liste `Artikel ohne BOM-Daten (SAGE+OSEON leer): art1, art2, … (+N weitere)`.

### 4.3 Testbare Logik extrahieren
Die reine Auswertung/Formatierung kommt in einen Helper (z. B.
`BomCacheCoverage.Build(totalEligible, capped, cap, articlesWithoutBom)`), der die Counts-Keys +
die beiden Warn-Strings (oder `null`) liefert — voll unit-testbar. Die `COUNT(*)`-Query selbst
bleibt Manual-UAT (wie der restliche raw-SQL-Pfad des BomCacheSyncService).

## 5. Counts-Vokabular
Konsistent zur deutschsprachigen Konvention (`neu`/`aktualisiert`/`uebersprungen`):
- Detection: `suchbegriffe gesamt`, `mit treffer`, `ohne treffer`.
- BomCache: `fa im fenster`, `fa gecacht`, `artikel ohne bom`.
Klein, ohne Sonderzeichen (passt zum `"key=value, …"`-Renderer).

## 6. Tests
**Teil A — Service-Unit (`IDEALAKEWMSService.Tests`, InMemory + `FakeSyncLogger`):**
- Begriff ohne Treffer → in `ohne treffer`-Count + `messageSuffix`-Liste.
- Neu erkannter FA → genau **eine** `LogInfoAsync`-Zeile (FA-Nr, AG-Code, Begriff; `reference`=Nr).
- Bereits vorhandener/`IsRemoved`-FaWorkStep → **keine** Zeile (nur `uebersprungen`).
- Mehrere auslösende Begriffe → komma-getrennt in einer Zeile.
- DryRun → Counts/Liste/Zeilen vorhanden, keine Persistenz.

**Teil B — Pure-Helper-Unit (`BomCacheCoverage`):**
- `total > cap` → korrekte `fa ohne cache`-Diff + Cap-Warn-String.
- `total ≤ cap` → kein Cap-Warn-String.
- `articlesWithoutBom > 0` → Hinweis-String mit gecappter Liste; sonst `null`.
- Counts-Keys korrekt gefüllt.

> Voraussetzung: `FakeSyncLogger`/`FakeSyncRun` erfasst `LogInfoAsync`/`LogWarningAsync` +
> `FinishSuccessAsync`-Counts + `messageSuffix`. Falls Lücke → Fake im Plan minimal erweitern.

## 7. Doku & Versionierung
- `Changelog.cshtml`-Bullet (v1.23.0).
- `CLAUDE.md`: Fallstrick `FaWorkStepDetectionService` + `BomCacheSyncService` um die neue
  Protokoll-Ausgabe + den Cache-Fenster/Cap-Vorbehalt ergänzen.
- `docs/TESTSZENARIEN.md`: Szenario (Lauf auslösen → Protokoll prüfen; Cap-Warnung bei vollem Cache).
- `Views/Help/Index.cshtml`: kurzer Hinweis auf die Aufgliederung + Vorbehalt.
- `PROJECT_STATUS.md`: kurzer Eintrag.

## 8. Risiken / offene Punkte
- **Keine zusätzlichen BOM-Roundtrips** in Teil A (Treffer je Begriff aus der ohnehin laufenden
  Query). Teil B: **eine** zusätzliche `COUNT(*)`-Query je Lauf — vernachlässigbar.
- **Message-/Listen-Länge:** Caps (50 Begriffe / N Artikel + „+N weitere") schützen vor
  übergroßen Messages.
- **Volumen der Info-Zeilen (Teil A c):** nur bei Neu-Erkennung → eingeschwungen 0/Lauf.
- **Cap-Warnung wiederholt sich** je Lauf solange der Cap voll ist — bewusst (es IST ein
  Dauerzustand, der Handlung erfordert: Cap erhöhen oder Auswahl ändern). 1 Warn-Zeile/Lauf, kein
  Spam.
- **Separat (nicht Teil dieser Spec):** Ob die `ORDER BY ProductionDate ASC`-Auswahl / Cap-Höhe
  geändert werden muss, zeigt erst Teil B im Betrieb — eigener Bugfix bei Bedarf.
