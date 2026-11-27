---
type: spec
title: "Vormontage-Listen (IDEAL): Spalte „Abteilung“ je HauptFA, neben „Arbeitsbereich“"
slug: 2026-11-11-vormontage-spalte-abteilung-hauptfa-spec
status: Testbereit
created: 2026-11-11
updated: 2026-11-11
freigabe_von: "Gerald Weichbold (Chat Koordination, 2026-11-11 20:40 „beide freigeben“)"
freigabe_am: 2026-11-11
source_backlog: "[[2026-11-11-vormontage-spalte-abteilung-hauptfa]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: IDEAL
affected_code:
  - "IdealAkeWms/Services/VormontageService.cs (KopfWerte :97, BuildKopfWerte :116-120, BuildPositionsSpalten :132-141, AggregateSummiert Select :317-335): Abteilung als dritter Kopfwert, Getter montage-abteilung, Anzeige Sicht 2"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyVormontageAggregatViewModel.cs (:33-40) neue Eigenschaft Abteilungen"
  - "IdealAkeWms/Views/FaHierarchyVormontage/Index.cshtml (colCount :6, th :76, Zelle :140, column-config :168)"
  - "IdealAkeWms/Views/FaHierarchyVormontage/Summiert.cshtml (colCount :7, Hinweistext :71-72, th :82, Zelle :105, column-config :128)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (FaHierarchyVormontageEinzeln :432-444, FaHierarchyVormontageSummiert :460-468)"
  - "IdealAkeWms.Tests/Services/VormontageNeueSpaltenTests.cs (neue Faelle; Drift-Waechter :180 bewusst angepasst)"
  - "docs/TESTSZENARIEN.md (neues Kapitel TS-127, Nummer prueft der Dev-Lauf)"
  - "IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs (nach 1.99.1), IdealAkeWms/Views/Help/Changelog.cshtml"
open_questions:
  - "1 Kombigeraet (mehrere verschiedene Abteilungen je HauptFA): Zelle zeigt den Hinweis „Kombigerät“ (Empfehlung), bleibt leer, oder listet doch alle Abteilungen?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
---

> [!note] Bezug und Messstand
> Ersetzt die verworfene Variante B von [[2026-10-06-vormontage-abteilung-spalte-filter-spec]] (dort jetzt als überholt
> markiert). Variante A, die Spalte „Arbeitsbereich“, ist mit v1.72.0 gebaut ([[2026-11-06-lagerplatz-etikett-rolle-und-vormontage-felder-spec]]).
> Alle `Datei:Zeile`-Angaben sind gegen den Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` gemessen,
> Stand v1.99.1 (`IdealAkeWms/AppVersion.cs:5`). Der Anhang `anhaenge/2026-11-11-vormontage-iso-abteilung.png` ist gelesen:
> Er zeigt Sicht „Einzelne Teile“, Reiter ISO, vier Gruppenköpfe mit den gelb markierten Werten `WKS`, `FKV`, `TK`, `GN`
> und in jeder Positionszeile Arbeitsbereich `H2-02`.

## Ziel / Nutzen (das Warum)

Die Vormontage arbeitet nach der Montage-Abteilung des Geräts (HauptFA). Heute steht diese Abteilung nur im Gruppenkopf von
„Einzelne Teile“. Man kann nicht nach ihr filtern, sie nicht als Spalte sortieren oder exportieren, und in „Summiert“ fehlt sie
ganz. Eine eigene Spalte „Abteilung“ in beiden Sichten schließt das. „Arbeitsbereich“ (Zielort je Position) bleibt daneben.
Gerald blendet ihn bei Bedarf über das Zahnrad aus.

**Standort IDEAL.** Die Vormontage-Listen hängen am Toggle `FaHierarchyVormontageAktiv` (Default aus, Gate
`FaHierarchyVormontageController.cs:29-30`). Die Kopftabelle füllt der Dienst aus der View in `Sync:FaHierarchyInfosViewName`,
die im Katalog als IDEAL-Einstellung geführt ist (`Models/ServiceSettingDefinitions.cs:55, :157`). AKE (Toggle aus) wird nicht
berührt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Sicht 1 „Einzelne Teile“ (`/FaHierarchyVormontage/Index`): Spalte „Abteilung“ mit der Abteilung des HauptFA je Positionszeile.
- Sicht 2 „Summiert“ (`/FaHierarchyVormontage/Summiert`): Spalte „Abteilung“ mit den Abteilungen aller HauptFA, die zur Zeile
  beitragen, verschieden und sortiert (z. B. `GN, TK`).
- Server-Spaltenfilter in beiden Sichten (beide Tabellen sind Server-Mode, `Index.cshtml:72`, `Summiert.cshtml:78`), Zahnrad,
  vollständige Registrierung, Drift-Wächter.
- CSV-Export: kommt ohne eigenen Code mit (siehe Anforderung 8).
- Tests, Kapitel in `docs/TESTSZENARIEN.md`, Version, Anwender- und Brain-Changelog.

**Out-of-Scope**
- Echte Trennung von Kombigeräten nach Abteilung (Positions-Split, getrennte Dokumente). Das bleibt
  [[2026-08-06-kombinationsgeraete-montageabteilung]]. Diese Spec legt nur fest, was die Zelle bei einem Kombigerät zeigt.
- Gruppenkopf von Sicht 1 (`Index.cshtml:106-135`): unverändert, er listet weiter jede Kopfzeile samt Abteilung.
- Spalte „Arbeitsbereich“: unverändert.
- Druck/PDF der Vormontage-Listen: Es gibt keinen. Der Controller hat nur `Index` (`:63`) und `Summiert` (`:102`), die Views tragen
  keinen `data-print-link` (Treffer nur in Kommissionierlisten, Beschichtung, Beschichtungsauftrag). Die Regel „nur sichtbare
  Spalten drucken“ aus v1.47.0 (`wwwroot/js/print-visible-columns.js`) greift hier deshalb nicht. Eine Druckansicht bleibt
  Backlog [[2026-09-14-vormontage-druckansicht]].
- Hilfeseite: Einen Vormontage-Abschnitt gibt es in `Views/Help/Index.cshtml` nicht (einziger Treffer `:1657` ist ein Nebensatz zum
  Matchcode). Wie in v1.72.0 nur der Changelog-Eintrag.
- FA-Liste (`montage-abteilung`, Label „Montage-Abt.“, `ColumnDefinitions.cs:36`) bleibt, wie sie ist.

## Fachliche Anforderungen

### Quelle (belegt, nicht aus dem Namen gedeutet)

Der Gruppenkopf zeigt heute **`FaHierarchyOrderInfo.MontageAbteilung`**:
- **Lesen im Gruppenkopf:** `Index.cshtml:118-121` läuft über `group.OrderInfos` und gibt je Kopfzeile
  `info.MontageAbteilung` aus, bei leerem Wert „—“. Das sind die gelb markierten Werte im Screenshot.
- **Woher `group.OrderInfos` kommt:** `VormontageService.BuildEinzelteileAsync` lädt `_orderInfos.GetAllAsync()` (`:223`) und gibt
  sie an `FaHierarchyListBuilder.Build` (`:234-238`). Der Baustein ordnet sie fan-out-frei je HauptFA zu: Dictionary `:153-155`,
  Zuweisung `OrderInfos` `:162-164`.
- **Einziger Schreibweg:** `SageFaStrukturReader.ReadOrderInfosAsync` setzt `MontageAbteilung = Str(r, oMontage)`
  (`IDEALAKEWMSService/Services/SageFaStrukturReader.cs:137, 151`). Gelesen wird aus `SELECT i.[Abteilung] AS [MontageAbteilung]`
  (`FaHierarchySql.cs:158`) der View aus `Sync:FaHierarchyInfosViewName` (`SageFaStrukturReader.cs:28, 60`; Default
  `[vw_IDEAL_AKE_WMS_FAInfos]`, `ServiceSettingDefinitions.cs:157`). `FaHierarchySyncService.cs:100-103` leert die Tabelle und
  schreibt sie neu (Full-Refresh). Eine Suche nach `MontageAbteilung` in `*.cs`, `*.cshtml`, `*.js` und `*.sql` zeigt keinen
  weiteren Entity-Schreiber. Die Treffer in `ProductionOrdersController.cs:235, 303, 311, 321` schreiben ViewModels, der Rest sind
  Tests, Migrationen, Mapping und Leser. In `SQL/` steht die Tabelle nur in `00_FreshInstall.sql` und `89_AddFaHierarchy.sql` (DDL).
- **Granularität:** eine Zeile je Auftrag (ABNr + Pos). Ein Kombigerät hat mehrere Zeilen zum selben HauptFA, die Abteilung ist
  dann der zweite Schlüssel (`Models/FaHierarchyOrderInfo.cs:5-9, 19-21`; Repo-Doku `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAInfos.sql:6-8, 28`).
- **„PPSInfo“ (Geralds Wort):** Die Repo-Doku nennt als Herkunft `[PPSImport].[dbo].[vw_PPSData]` (`…FAInfos.sql:9`). Sie beschreibt
  aber die ältere View `vw_IDEAL-AKE_Kommissionierung_FAInfos`. Ob die heute eingestellte View dieselbe Herkunft hat, steht nur in
  Sage. Das ist **unverifiziert**, Prüfabfrage P-A. Für diese Spec ist es nicht tragend: Die Spalte zeigt nachweislich dasselbe Feld
  wie der Gruppenkopf.
- **Abgrenzung:** `FaHierarchyNode.Arbeitsbereich` ist ein anderes Feld, je Position (Sage `USER_OSAbteilung` laut [[fallstricke]] §15).
  Darauf beruht die bestehende Spalte „Arbeitsbereich“ (`VormontageService.cs:135`).

### Anforderungen

1. **Spalte „Abteilung“, Col-Key `montage-abteilung`.** Der Key ist derselbe wie in der FA-Liste für dasselbe Feld
   (`ColumnDefinitions.cs:36`). Das Label folgt Geralds Wunsch. Der Key `abteilung` bleibt gesperrt (Drift-Wächter
   `VormontageNeueSpaltenTests.cs:180`, Entscheidung 2026-10-07). So bleibt im Code klar, dass hier nicht der Arbeitsbereich gemeint ist.
   `<th>` bekommt `title="Montage-Abteilung des HauptFA (aus den Auftragsdaten)"`, damit sich die Spalte von „Arbeitsbereich“
   unterscheiden lässt.
2. **Position:** jeweils direkt rechts neben `arbeitsbereich`. Sicht 1: `hauptfa`, `arbeitsbereich`, `montage-abteilung`, …
   Sicht 2: `matchcode`, `arbeitsbereich`, `montage-abteilung`, … Per Default sichtbar, nicht `locked`, ohne `defaultHidden`.
   Wer schon gespeicherte Spalten-Einstellungen hat, sieht die neue Spalte an der Nachbarposition, sichtbar
   (`column-preferences.js` `mergeWithDefaults` `:76`, Rückfall `visible: !c.defaultHidden` `:117`).
3. **Wert je HauptFA (eine Regel, beide Sichten).** Aus allen `FaHierarchyOrderInfo`-Zeilen des HauptFA die Werte von
   `MontageAbteilung` nehmen, getrimmt, leere verwerfen, verschieden ohne Groß/Klein-Unterschied (`OrdinalIgnoreCase`):
   - 0 Werte (keine Kopfzeile oder alle leer) → `null`, die Zelle bleibt leer. Der Gruppenkopf zeigt weiter „—“ (`Index.cshtml:121`).
   - genau 1 Wert → dieser Wert (auch wenn mehrere Kopfzeilen dieselbe Abteilung tragen).
   - mehr als 1 Wert (**Kombigerät**) → der feste Hinweistext **„Kombigerät“**. Es wird keine Abteilung still ausgewählt
     ([[glossar]] „Kombinationsgeraet“). Siehe Rückfrage 1.
4. **Sicht 1:** Jede Positionszeile zeigt den Wert ihres HauptFA. Alle Positionen einer Gruppe zeigen also denselben Wert. Die
   Positionszahl bleibt 1:1, kein Fan-out.
5. **Sicht 1, Filter `colf_montage-abteilung`:** geltende Suchsyntax ([[0017-ein-parser-fuer-die-spaltenfilter-syntax]]), je Position
   vor der Gruppierung (`FaHierarchyListBuilder.cs:136-138`). Weil alle Positionen einer Gruppe denselben Wert tragen, bleiben oder
   fallen ganze Gruppen. `TotalCount` zählt Gruppen mit Treffer. `TK` trifft Gruppen mit Abteilung TK, `Kombi` trifft Kombigeräte,
   `!TK` blendet TK-Gruppen aus.
6. **Sicht 2, Anzeige:** Die Zeile zeigt die Werte nach Regel 3 aller beitragenden HauptFA, verschieden, sortiert
   ordinal-ignore-case, getrennt durch `", "` (gleiche Regel wie die übrigen Listen-Spalten von Sicht 2, `VormontageService.FormatTexte`
   `:100-105`). Beispiel: HauptFA mit TK und HauptFA mit GN → `GN, TK`. Ist ein Kombigerät beteiligt, steht „Kombigerät“ an seiner
   alphabetischen Stelle, z. B. `GN, Kombigerät, TK`. Der Gruppierschlüssel bleibt `Matchcode`.
7. **Sicht 2, Filter:** wirkt je Position vor dem Zusammenzählen, wie alle Spalten aus v1.72.0 (Freigabe-Antwort 3 dort). Die
   Summen enthalten dann nur Mengen der HauptFA, deren Abteilungstext passt. Folge, bewusst so: Bei einem Abteilungsfilter
   (z. B. `TK`) fallen Mengen von Kombigeräten heraus, weil deren Text „Kombigerät“ lautet. Sie lassen sich mit dem Filter `Kombi`
   gezielt zeigen. Der Hinweistext unter der Filterkarte (`Summiert.cshtml:71-72`) nennt die Abteilung in der Aufzählung der
   Spalten, die vor dem Zusammenzählen filtern.
8. **CSV-Export:** Die Spalte erscheint, wenn sie sichtbar ist, mit dem angezeigten Text. `csv-export.js` läuft je
   `table.filterable-table` (`Views/Shared/_Layout.cshtml:458-460`) und liest den Zelltext (`csv-export.js:93`).
9. Zugriff, Toggle, Reiter (`VMBedarf`), KW-Filter, Paging-Einheiten und alle bestehenden Spalten bleiben unverändert.

## Technischer Loesungsentwurf

Muster: Listen-View-Pattern ([[0005-listen-view-pattern-mit-server-side-spaltenfilter]]); „Getter rendern wie die Zelle“
([[fallstricke]] §3 „Universal-Filter-Pattern“, §26). Leiter: Der Kopfwert-Pfad aus v1.72.0 trägt schon alles, was gebraucht wird:
fan-out-freier Lookup je HauptFA, eine Methode für Zelle und Getter, ein Ort für die Positions-Getter beider Sichten. Es kommt nur
ein dritter Kopfwert dazu. Kein neuer Repository-Aufruf, keine neue Klasse, keine Migration.

**T1. Kopfwert erweitern** (`VormontageService.cs`).
- `KopfWerte` (`:97`) um `string? Abteilung` ergänzen. Mit Default `= null` als drittem Positionsparameter bleibt der
  Bestandstest `VormontageNeueSpaltenTests.cs:86` unverändert grün, weil seine Infos keine Abteilung tragen. Ohne Default den Test
  mitziehen; das entscheidet der Dev-Lauf.
- Neue statische Methode `FormatAbteilung(IEnumerable<FaHierarchyOrderInfo>)` nach Regel 3. Konstante
  `public const string KombigeraetHinweis = "Kombigerät";` an genau einer Stelle.
- `BuildKopfWerte` (`:116-120`) füllt `Abteilung` über `FormatAbteilung`. `BuildKopfLookup` (`:123-126`) und der Lookup in
  `AggregateSummiert` (`:309`) laufen beide über `BuildKopfWerte` und brauchen keine Änderung.

**T2. Getter, ein Ort für beide Sichten.** In `BuildPositionsSpalten` (`:132-141`) ergänzen:
`["montage-abteilung"] = n => kopf.TryGetValue(n.HauptFA, out var k) ? k.Abteilung : null`.
Damit enthalten automatisch die Sicht-1-Map (`BuildEinzelnColumnMap` `:145-151`, Aufruf `:236`) und der Sicht-2-Vorfilter (`:311`)
den Key. **Nicht** in `AggregatColumnMap` (`:158-164`) aufnehmen: `Apply` ignoriert unbekannte Keys, der Nachfilter (`:341`)
wertet ihn deshalb kein zweites Mal aus. Das ist dasselbe Muster wie bei allen Spalten aus v1.72.0.

**T3. Sicht 2 Anzeige.** `FaHierarchyVormontageAggregatViewModel` (`:33-40`) bekommt `public string? Abteilungen { get; init; }`.
Im `Select` (`:317-335`): `Abteilungen = FormatTexte(hauptFas.Select(h => kopf.TryGetValue(h, out var k) ? k.Abteilung : null))`.
`hauptFas` sind schon nach dem Vorfilter bestimmt (`:319`), die Anzeige zeigt also nur beitragende HauptFA.

**T4. Views** (Skill `frontend-design` explizit aufrufen, CLAUDE.md; Hausstil Bootstrap 5, keine eigene Gestaltung).
- `Index.cshtml`: `<th data-filterable data-col-key="montage-abteilung" title="…">Abteilung</th>` nach `:76`; Zelle
  `<td>@kopf.Abteilung</td>` nach `:140`, `kopf` existiert je Gruppe (`:104`); `#column-config`-Eintrag nach `:168`;
  `colCount` 13 → 14 (`:6`, colspan der Kopf- und Leerzeile).
- `Summiert.cshtml`: `<th …>` nach `:82`, Zelle `<td>@row.Abteilungen</td>` nach `:105`, `#column-config` nach `:128`,
  `colCount` 9 → 10 (`:7`, Kommentar mitziehen), Hinweistext `:71-72` um „Abteilung“ ergänzen.
- Kein `data-filter-type`, Textspalte. Kein Eintrag in `ColumnFilterTypes.Lists` (`ColumnFilterSyntax.cs:54-55`).
- Skript-Reihenfolge stimmt schon (`Index.cshtml:184-185`, `Summiert.cshtml:140-141`).

**T5. `ColumnDefinitions`.** `new ColumnDef("montage-abteilung", "Abteilung", Locked: false)` direkt nach `arbeitsbereich` in
`FaHierarchyVormontageEinzeln` (`:433`) und `FaHierarchyVormontageSummiert` (`:461`), in derselben Reihenfolge wie das
`#column-config`. Die ViewKeys sind registriert (`GetByViewKey` `:712-713`), kein Prefs-API-400 ([[fallstricke]] §3).

**T6. Drift-Wächter und Tests** (`VormontageNeueSpaltenTests.cs`).
- Die Drei-Quellen-Prüfung `:170-183` und die Map-Prüfung `:186-192` decken die neue Spalte ohne Umbau ab (A/B/C plus
  Sicht-1-Map gegen `ColumnDefinitions`).
- **Bewusste Teständerung:** `:180` verlangt heute `NotContain("montage-abteilung")`. Die Zeile wird zu
  `.Contain("montage-abteilung")`, `NotContain("abteilung")` bleibt. Grund: Diese Spec führt genau diese Spalte ein. Die Absicht des
  Wächters, „Arbeitsbereich“ nicht unter dem Key `abteilung` zu führen, bleibt geprüft. Die Testmethode heißt dann sinngemäß
  `…_ArbeitsbereichUndMontageAbteilung`.
- Neue Fälle: siehe [[#Test-Szenarien]].

**T7. Changelog.** `Views/Help/Changelog.cshtml`: neue Spalte „Abteilung“ (Montage-Abteilung des HauptFA, wie im Gruppenkopf) in
beiden Vormontage-Sichten, in „Summiert“ als Liste. Kombigerät zeigt „Kombigerät“. Filter in „Summiert“ wirkt vor dem
Zusammenzählen, Kombigeräte fallen bei einem Abteilungsfilter heraus.

## Migrations-/SQL-Auswirkungen

Keine. `FaHierarchyOrderInfos.MontageAbteilung` besteht (`Migrations/20260807105825_AddFaHierarchy.cs:63`, `nvarchar(200)`;
`SQL/89_AddFaHierarchy.sql:62`; `SQL/00_FreshInstall.sql:1959`). Keine Migration, kein `SQL/XX_*.sql`, FreshInstall unverändert.
Der Sync bleibt unverändert. `SQL/AgentJobs/` ist nicht betroffen (die Tabelle steht nur in `00_FreshInstall.sql` und `89_*`).
Nichts ist daten-destruktiv.

## Einstellungen

keine

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Reine Lesefunktion. `FaHierarchyOrderInfo` ist bewusst keine `AuditableEntity` (Cache-Tabelle, `FaHierarchyOrderInfo.cs:10`)
und wird hier nicht geschrieben.

**Rollen/Zugriff:** unverändert, Class-Level `[RequireVorbauAccess]` + Gate `[RequireFaHierarchyVormontageAktiv]`
(`FaHierarchyVormontageController.cs:29-30`). Keine neue Rolle, kein neuer Filter, `RoleOverview` unberührt.

**Listen-View-Pattern:** Pagination, Filterkarte und Server-Mode bleiben. Die neue `<th>` trägt `data-col-key`, die Spalte steht in
`ColumnDefinitions` und `#column-config`. Es entsteht keine neue Tabellen-View.

## Akzeptanzkriterien

1. Sicht 1 zeigt die Spalte „Abteilung“ direkt rechts neben „Arbeitsbereich“. Jede Positionszeile zeigt die Abteilung ihres HauptFA,
   gleich dem Wert im Gruppenkopf (bei genau einer Abteilung).
2. HauptFA ohne Kopfzeile oder mit leerer Abteilung: Zelle leer, Gruppenkopf unverändert („—“ bzw. „Keine Auftrags-Kopfdaten …“).
3. HauptFA mit zwei Kopfzeilen und derselben Abteilung (auch mit abweichender Groß/Klein-Schreibung oder Rand-Leerzeichen): Zelle zeigt
   diese eine Abteilung.
4. HauptFA mit zwei verschiedenen Abteilungen (Kombigerät): Zelle zeigt „Kombigerät“, der Gruppenkopf listet weiter beide Abteilungen
   samt Badge „mehrdeutig (Kombigerät)“, die Positionszahl bleibt gleich.
5. Sicht 1, Filter `TK`: nur Gruppen mit Abteilung TK, vollständig; `TotalCount` zählt diese Gruppen; `!TK` blendet sie aus; `Kombi`
   zeigt nur Kombigeräte.
6. Sicht 2 zeigt „Abteilung“ direkt rechts neben „Arbeitsbereich“. Eine Zeile mit Positionen aus HauptFA TK und GN zeigt `GN, TK`;
   je Matchcode bleibt genau eine Zeile.
7. Sicht 2, Filter `TK`: Die Summen enthalten nur Mengen der TK-HauptFA (vor dem Zusammenzählen); Matchcodes ohne TK-Position
   verschwinden; `TotalCount` zählt Aggregatzeilen nach Filter; Filter auf Matchcode/Summen wirken wie bisher.
8. Die Spalte lässt sich in beiden Sichten über das Zahnrad aus- und einblenden und verschieben; die Wahl bleibt nach Reload
   (Prefs-API ohne 400). Benutzer mit alten Prefs sehen sie neben „Arbeitsbereich“.
9. Für beide Views sind die Key-Mengen von `<th data-col-key>`, `#column-config` und `ColumnDefinitions` gleich, mit gleicher
   Reihenfolge `#column-config` = `ColumnDefinitions`; sie enthalten `montage-abteilung` und nicht `abteilung`. Die Sicht-1-Filtermap
   hat dieselbe Schlüsselmenge wie `ColumnDefinitions.FaHierarchyVormontageEinzeln`.
10. CSV-Export beider Sichten enthält „Abteilung“ mit dem angezeigten Text, wenn sichtbar, sonst nicht.
11. Bestehende Spalten, Reiter, KW-Filter und Spaltenfilter verhalten sich unverändert (bestehende `VormontageServiceTests` und
    `VormontageNeueSpaltenTests` grün, außer der bewusst geänderten Zeile `:180`).
12. Zugriff unverändert: ohne Vorbau-Zugriff AccessDenied, Toggle aus → Redirect Startseite.
13. `dotnet build` und `dotnet test` grün.

## Test-Szenarien

Neues Kapitel **TS-127 — Vormontage-Listen: Spalte „Abteilung“ je HauptFA** in `docs/TESTSZENARIEN.md`. Letztes Kapitel im Bündel ist
TS-126 (`docs/TESTSZENARIEN.md:11525`), der Dev-Lauf prüft die Nummer erneut. Querverweise: TS-61 (Vormontage-Grundfunktion), TS-105
(v1.72.0-Spalten), TS-86 (CSV), TS-91 (Suchsyntax). Danach `secondbrain/tests/testszenarien-index.md` nachziehen.

**Automatisiert (DB-frei, `VormontageNeueSpaltenTests`):**
- `FormatAbteilung`: keine Infos → null; nur leer/Leerzeichen → null; eine → Wert getrimmt; zwei gleiche (auch `tk`/` TK `) → ein Wert;
  zwei verschiedene → „Kombigerät“.
- Sicht 1: Gruppe TK und Gruppe GN; Filter `TK` → eine vollständige Gruppe, `TotalGroupCount` 1; `!TK` → nur GN; Kombigerät mit `Kombi`
  gefunden; Getter = Zelle (`BuildKopfWerte(...).Abteilung`).
- Sicht 2: Anzeige `GN, TK`; Kombigerät beteiligt → `GN, Kombigerät, TK`; Filter `TK` reduziert die Summe auf TK-Mengen; Filter
  `montage-abteilung` steht nicht in `AggregatColumnMap`.
- Drift-Wächter `:170-192` mit geänderter Zeile `:180`.

**Manuell am IDEAL-Testsystem** (Vorbedingung: Toggle `FaHierarchyVormontageAktiv` an, Benutzer mit `vorbau` oder `admin`, echte Daten;
P-B vorher gelaufen, um ein Kombigerät und einen HauptFA ohne Abteilung zu finden):
1. Sicht 1, Reiter ISO (wie Screenshot): Spalte „Abteilung“ rechts neben „Arbeitsbereich“; Werte WKS/FKV/TK/GN gleich dem Gruppenkopf.
2. Filter `TK` + ENTER (URL `colf_montage-abteilung=TK`): nur TK-Gruppen, Seitenzahl passt; `!TK`; Filter entfernen.
3. Kombigerät aus P-B: Zelle „Kombigerät“, Kopf mit beiden Abteilungen und Badge; Filter `Kombi` findet es.
4. HauptFA ohne Abteilung aus P-B: Zelle leer, Kopf „—“.
5. Sicht 2: Spalte rechts neben „Arbeitsbereich“, Zeilen mit mehreren HauptFA zeigen z. B. `GN, TK`; Filter `TK` senkt Summen plausibel;
   Hinweistext nennt die Abteilung.
6. Zahnrad: „Abteilung“ ausblenden, „Arbeitsbereich“ ausblenden, Reload, beides bleibt; wieder einblenden.
7. CSV-Export beider Sichten mit und ohne ausgeblendete Spalte.
8. Kombination Reiterwechsel + KW-Filter + Abteilungsfilter.
9. Negativ: ohne Vorbau-Zugriff AccessDenied; AKE (Toggle aus) Redirect Startseite.
10. Regression: Kommissionierlisten, Beschichtung, FA-Liste (Spalte „Montage-Abt.“) unverändert.
11. Kontrast und Lesbarkeit der neuen Spalte am Terminal (WCAG AA, frontend-design).

## Pruefabfragen fuer Gerald

Nur lesend. P-B und P-C auf der **WMS-DB IDEAL** (der Cache spiegelt die View zum Zeitpunkt `SyncedAt`), P-A auf der **Sage-DB IDEAL**.

```sql
-- P-A (Sage-DB IDEAL): Herkunft "PPSInfo" der eingestellten FAInfos-View bestaetigen.
-- Zuerst auf der WMS-DB den View-Namen holen:
SELECT [Value] FROM dbo.ServiceSettings WHERE [Key] = N'Sync:FaHierarchyInfosViewName';
-- Dann auf der Sage-DB (Namen aus der Zeile oben einsetzen):
EXEC sp_helptext N'<FAInfos-View aus P-A>';
-- Im Text pruefen: Woher kommt die Spalte [Abteilung]? (Repo-Doku erwartet [PPSImport].[dbo].[vw_PPSData])

-- P-B (WMS-DB IDEAL): Kombigeraete mit verschiedenen Abteilungen und HauptFA ohne Abteilung in der Vormontage-Basis
SELECT i.HauptFA, COUNT(*) AS Kopfzeilen,
       COUNT(DISTINCT UPPER(LTRIM(RTRIM(i.MontageAbteilung)))) AS Abteilungen,
       STRING_AGG(i.MontageAbteilung, N' | ') AS Werte
FROM dbo.FaHierarchyOrderInfos i
WHERE i.HauptFA IN (SELECT n.HauptFA FROM dbo.FaHierarchyNodes n WHERE NULLIF(LTRIM(RTRIM(n.VMBedarf)), N'') IS NOT NULL)
GROUP BY i.HauptFA
HAVING COUNT(DISTINCT UPPER(LTRIM(RTRIM(i.MontageAbteilung)))) > 1
    OR MAX(NULLIF(LTRIM(RTRIM(i.MontageAbteilung)), N'')) IS NULL
ORDER BY Abteilungen DESC, i.HauptFA;

-- P-C (WMS-DB IDEAL): Wertevorrat; Komma im Wert wuerde mit dem ", "-Trenner von Sicht 2 und dem Komma-ODER im Filter kollidieren
SELECT DISTINCT MontageAbteilung FROM dbo.FaHierarchyOrderInfos ORDER BY 1;
SELECT COUNT(*) AS MitKomma FROM dbo.FaHierarchyOrderInfos WHERE MontageAbteilung LIKE N'%,%';
```

Liefert P-C Treffer mit Komma, ist die Sicht-2-Anzeige nicht eindeutig. Das wird gemeldet, nicht still gelöst.

## Etappen (nur bei epic: true)

Entfällt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | –      | –      | –      |

## Deploy

**Final (QA 2026-11-11, aus dem echten Diff `81b95ac2..d2d4ed46`):** `deploy.web: true` (Service, ViewModel, zwei Views,
`ColumnDefinitions`, Changelog, `AppVersion`), `deploy.service: true` nur wegen `IDEALAKEWMSService/AppVersion.cs` (kein Dienst-Code;
mitdeployen hält die Version gleich), `deploy.migration: false` (keine Datei unter `*/Migrations/`, kein `SQL/`). Manifest
`scripts/deploy/nachlauf.psd1`: kein Eintrag nötig (kein SQL, kein Einmal-Skript, keine Einstellung, kein manueller Schritt) – geprüft.
**Publish erledigt:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (IDEALAKEWMSWeb + IDEALAKEWMSWebService +
SQL + deploy, `publish-info.txt` Version 1.99.3, Commit `d2d4ed4692b78f8cd9911498000283c2a687ce54`). Reihenfolge: kein DB-Backup nötig;
Web, dann Dienst (nur Version). Nach dem Merge nur dann aus `main` neu publizieren, wenn der Merge getestete Dateien mit parallelen
`main`-Änderungen kombiniert hat.

Vorläufige Fassung des Spec-Agents (bestätigt):

- **Web-App:** ja (`VormontageService`, ViewModel, beide Vormontage-Views, `ColumnDefinitions`, Changelog, `AppVersion.cs`).
- **Service:** nur Versionsnummer (`IDEALAKEWMSService/AppVersion.cs`), kein Code; mitpublizieren hält die Version gleich.
- **Migration:** nein. Kein DB-Backup nötig.
- **Version:** nächste freie **nach 1.99.1**, vergibt der Dev-Lauf.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023): **keine**. Kein SQL-Skript, kein Einmal-Skript, keine
  Einstellung, kein Leitstand-Schritt, kein Ordner und kein SQL-prüfbarer Zustand. Die Spalte erscheint nach dem Web-Deploy von selbst
  (Default sichtbar), deshalb ist auch kein Info-Eintrag nötig. Die Anwender-Information steht im Changelog.
- **Reihenfolge:** Web publizieren → optional Dienst (nur Version). Ablauf Mensch: aus dem Worktree publizieren → Testsystem IDEAL →
  testen → Merge.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService` (nur Version)

## Offene Rueckfragen

1. **Kombigerät (mehrere verschiedene Abteilungen je HauptFA): was zeigt die Zelle?**
   Gerald: „vorerst auslassen“. Drei Möglichkeiten:
   - **(a) Hinweis „Kombigerät“ (Empfehlung, so im Rumpf beschrieben).** Sichtbar statt still („Melden statt still behandeln“),
     unterscheidbar von „keine Abteilung“ (leer), mit `Kombi` filterbar. Es wird keine Abteilung ausgewählt (Glossar). Das passt zum
     Gruppenkopf, der beim Kombigerät das Badge „mehrdeutig (Kombigerät)“ zeigt (`Index.cshtml:109-114`), und zur FA-Liste, die bei
     mehreren Köpfen keine Variante übernimmt (`ProductionOrdersController.cs:314-328`). Folge: Ein Abteilungsfilter in „Summiert“
     lässt Kombigeräte-Mengen weg (Anforderung 7).
   - (b) Leer. Dann ist ein Kombigerät nicht von einem HauptFA ohne Abteilung zu unterscheiden, und ein Filter findet es nie.
   - (c) Alle Abteilungen, z. B. `GN, TK`, wie Kunde/FE-Termin in v1.72.0. Das ist keine „Auslassung“ mehr: Ein Filter `TK` zählt dann
     in „Summiert“ die Mengen des ganzen Kombigeräts mit, auch die Teile für GN.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Kombigerät: Zelle zeigt (a) „Kombigerät“, (b) leer oder (c) alle Abteilungen? (Empfehlung: a) → **a (Gerald 2026-11-11).**

## Umsetzung (goal-Lauf 2026-11-11)

Buendel-Commit `d2d4ed46` (v1.99.3, auf v1.99.2 `81b95ac2`). Umgesetzt wie T1–T7, Freigabe-Antwort 1 = (a) „Kombigerät“.
- `VormontageService`: `KopfWerte` mit drittem Parameter `Abteilung = null`, `FormatAbteilung` (0 → null, 1 → Wert, >1 verschieden ohne
  Gross/Klein → `KombigeraetHinweis` „Kombigerät“), `BuildKopfWerte` fuellt ihn; Getter `montage-abteilung` in `BuildPositionsSpalten` (Sicht-1-Map
  und Sicht-2-Vorfilter), nicht in `AggregatColumnMap`; Sicht 2 `Abteilungen = FormatTexte(...)` der beitragenden HauptFA.
- Views: `<th>` mit `title`, Zelle, `#column-config`, `colCount` 14 bzw. 10, Hinweistext Summiert; `ColumnDefinitions` beider Sichten direkt nach
  `arbeitsbereich`. Skill `frontend-design` aufgerufen; Hausstil, keine eigene Gestaltung.
- Drift-Waechter `VormontageNeueSpaltenTests` bewusst geaendert (T6): `Contain("montage-abteilung")`, `NotContain("abteilung")`, Position direkt
  nach `arbeitsbereich`; Methode umbenannt `…_ArbeitsbereichUndMontageAbteilung`. Neue Tests: `FormatAbteilung_…`,
  `Sicht1_Abteilung_JeHauptFa_Filter_Negation_Kombi_GetterGleichZelle`, `Sicht2_Abteilungen_Sortiert_MitKombigeraet_FilterVorDemZusammenzaehlen`.
- Version 1.99.3, Changelog-Karte, TS-127 (127.1–127.9). Keine Migration, keine Einstellung, kein Manifest-Eintrag (Begruendung im Deploy-Abschnitt).

**Nachweis:** `dotnet build IdealAkeWms.slnx` 0 Fehler; `dotnet test` Web **2612/2613** (1 uebersprungen, Bestand), Dienst **512/512**;
Vormontage-/Spalten-Tests 35/35.

## QA-Nachweis (qa-agent, 2026-11-11)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Commit-Bereich `81b95ac2..d2d4ed46` (HEAD `d2d4ed46`), Version 1.99.3.

**1. Build und Tests** (im Worktree, ganze Lösung)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (Bestand).
- `dotnet test`: `IdealAkeWms.Tests` 2612 bestanden / 1 übersprungen (Bestand) / 0 Fehler (2613); `IDEALAKEWMSService.Tests` 512 / 0 / 0.

**2. Code-Review** (selbst, in diesem Lauf; gelesen: Diff plus `VormontageService` Kopfwert-Pfad, `AggregateSummiert` komplett,
`BuildEinzelnColumnMap`, `Index.cshtml` Gruppenkopf/`kopf`, `csv-export.js` Zellwert/Spaltenlogik) – **keine Befunde**.
- fallstricke §3: `montage-abteilung` in `<th data-col-key>`, `#column-config` und `ColumnDefinitions` beider ViewKeys, gleiche Reihenfolge,
  direkt nach `arbeitsbereich`; Drift-Wächter prüft das (inkl. Position).
- Getter = Zelle: Zelle Sicht 1 `@kopf.Abteilung` mit `kopf = BuildKopfWerte(group.OrderInfos)`, Getter über `BuildKopfLookup` →
  `BuildKopfWerte` → `FormatAbteilung`; eine Regel an einem Ort (`FormatAbteilung`), kein zweiter Weg in SQL/JS.
- Sicht 2: Getter nur in `BuildPositionsSpalten` (Vorfilter je Position), **nicht** in `AggregatColumnMap` – Test sichert das ab;
  Anzeige `Abteilungen` aus den `hauptFas` nach dem Vorfilter.
- Defektklassen geprüft, nicht betroffen: kein Wert in `<script>`, kein `hidden`+`d-*`, kein stiller Fallback (leer bleibt `null`,
  Kombigerät sichtbar), keine Parallel-Arrays/Model-Binding, keine reine Browser-Validierung.
- Hinweise ohne Befund-Rang: (H1) Sicht 2 lässt beitragende HauptFA ohne Abteilung in der Liste weg (`FormatTexte` verwirft leer, so
  Regel 6) – auf DEV gesehen: Zeile mit 1060414 (FKV) + 1065036 (leer) zeigte „FKV“. (H2) Der Filter ist Teiltreffer nach ADR 0017 –
  `TK` würde auch einen Code wie `TKX` treffen; P-C zeigt den Wertevorrat.

**3. Nachweisart je AC**
| AC | Nachweis |
|---|---|
| 1 | automatisiert (Getter = `BuildKopfWerte`) + DEV-Browser OK |
| 2 | automatisiert (HauptFA ohne Kopfzeile → null) + DEV-Browser OK (leere Abteilung) |
| 3 | automatisiert (`FormatAbteilung` `TK`/` tk `) |
| 4 | automatisiert + DEV-Browser OK |
| 5 | automatisiert (TK/!TK/Kombi, TotalGroupCount) + DEV-Browser OK (FKV/!FKV/Kombi) |
| 6 | automatisiert (`GN, TK`, `GN, Kombigerät, TK`) + DEV-Browser OK (`FKV, GN`) |
| 7 | automatisiert (Summe 4 bei TK) + DEV-Browser OK (Summe 4 → 3 bei `fkv`) |
| 8 | Manual-UAT (Browser-JS/Prefs-API): DEV teilweise OK (Ausblenden + Reload bleibt, Prefs gespeichert); alte Prefs ohne Key nicht geprüft |
| 9 | automatisiert (Drift-Wächter Theory, beide Views) |
| 10 | Manual-UAT (Browser-JS, Download) – offen |
| 11 | automatisiert (bestehende Tests grün) |
| 12 | Manual-UAT: AKE Redirect auf DEV OK; AccessDenied offen |
| 13 | automatisiert, grün |

**4. Geänderte bestehende Tests:** nur `VormontageNeueSpaltenTests.DreiQuellen_…` (umbenannt `…_ArbeitsbereichUndMontageAbteilung`):
`NotContain("montage-abteilung")` → `Contain("montage-abteilung")`, Positionsprüfung neu – **Verhalten bewusst durch die Spec geändert**
(T6, AC 9). `NotContain("abteilung")` bleibt. Keine sonstige Fixture-Anpassung.

**5. Checkliste:** keine Migration/kein SQL/kein FreshInstall/keine AgentJobs (korrekt, Schema besteht); Audit nicht betroffen (Lesefunktion);
Version 1.99.3 in beiden `AppVersion.cs`; Anwender-Changelog + Brain-Changelog [[2026-11-11-v1-99-3-vormontage-spalte-abteilung]];
TS-127 in `docs/TESTSZENARIEN.md` und im Testindex (Zeile 127); feature-map auf Testbereit gesetzt. **5a ADR 0020:** keine neue/geänderte
Einstellung im Diff.

**6. DEV-Deploy und Testlauf**
- `standort: IDEAL` laut Frontmatter; deployt wurden AKE **und** IDEAL (Web + Dienst, erhöhte Shell) aus dem Worktree:
  `dev-deploy.ps1 -Standort IDEAL` → „HTTP 200, Fusszeile v1.99.3“, Exit 0; `-Standort AKE` → „HTTP 200, Fusszeile v1.99.3“, Exit 0.
- Toggle `FaHierarchyVormontageAktiv` auf IDEAL_WMS_DEV war schon `true` (vorher/nachher `true`, nicht geändert); AKE_WMS_DEV `false` (unverändert).
- Testdaten (DEV IDEAL, danach zurückgesetzt): Zeile `FaHierarchyOrderInfos` HauptFA 1064052 `GN` / ABNr `QA-127` eingefügt (→ Kombigerät)
  und gelöscht; 1065036 `MontageAbteilung` vorübergehend `NULL`, wieder `GN`. Endzustand = Ausgangszustand (4 Zeilen FKV/WKS/TK/GN).

| Schritt | gemacht | Ergebnis | Status |
|---|---|---|---|
| 127.1 | Sicht 1 ISO | Spalte „Abteilung“ rechts neben „Arbeitsbereich“, WKS/FKV gleich Gruppenkopf | OK |
| 127.2 | URL `colf_montage-abteilung=FKV` / `!FKV` (Reiter DI) | FKV: 1 Gruppe, alle 3 Positionen, „1–1 von 1“; !FKV: 3 Gruppen, „1–3 von 3“ | OK |
| 127.3 | Kombigerät 1064052 | Zelle „Kombigerät“, Kopf „TK …“ + „GN …“ mit Badge „mehrdeutig (Kombigerät)“, 1 Position; `Kombi` → nur 1064052 | OK |
| 127.4 | 1065036 ohne Abteilung | Zelle leer, Kopf „—“ | OK |
| 127.5 | Summiert DI | Spalte rechts neben „Arbeitsbereich“; `Kombigerät`, `WKS`, `FKV, GN`; Filter `fkv` getippt + ENTER: Summe 4 → 3, nur 1060414; Hinweistext nennt „Abteilung“ | OK |
| 127.6 | Zahnrad Summiert: „Abteilung“ aus, Reload, wieder an | bleibt ausgeblendet nach Reload, `UserViewPreferences` Id 17 `visible:false` → wieder `true`; „Arbeitsbereich“ zusätzlich nicht getestet | OK (teilweise) |
| 127.7 | CSV | Download braucht Zustimmung des Menschen; Code-Pfad `csv-export.js` generisch (Zelltext je sichtbarer Spalte) | NICHT MOEGLICH |
| 127.8 | AKE `/FaHierarchyVormontage/Index` | Redirect auf Dashboard `/`; AccessDenied ohne Vorbau: kein zweiter Benutzer verfügbar | OK / NICHT MOEGLICH |
| 127.9 | IDEAL `/ProductionOrders` | Spalte „Montage-Abt.“ vorhanden, Seite fehlerfrei | OK (Stichprobe) |
| Kontrast | Sichtprüfung | neue Spalte ohne eigenes Styling, gleiche Bootstrap-Tabellenzellen wie Nachbarspalten (dunkler Text auf Weiß/Grau) | OK |

**7. Publish:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` – beide `dotnet publish` Exit 0, `SQL\` und `deploy\`
kopiert, `publish-info.txt` Version 1.99.3, Commit `d2d4ed4692b78f8cd9911498000283c2a687ce54`, Branch `feature/2026-08-07-ideal-teile-1-5`.

**Urteil: Testbereit.**

### Manuelle Checkliste (Mensch, IDEAL-Testsystem)
1. P-B und P-C auf der WMS-DB IDEAL ausführen (Kombigerät, HauptFA ohne Abteilung, Komma im Wert).
2. TS-127.1–127.5 mit echten Daten (Reiter ISO wie Screenshot; echtes Kombigerät aus P-B).
3. TS-127.6 vollständig: „Abteilung“ **und** „Arbeitsbereich“ ausblenden, Reload; zusätzlich Benutzer mit alten Prefs (ohne `montage-abteilung`) sieht die Spalte neben „Arbeitsbereich“.
4. TS-127.7 CSV-Export beider Sichten mit und ohne ausgeblendete Spalte.
5. TS-127.8 Benutzer ohne `vorbau`/`admin` → AccessDenied.
6. TS-127.9 Regression Kommissionierlisten, Beschichtung, FA-Liste.
7. Kontrast am Fertigungsterminal ansehen.
8. Publish `publish\` (v1.99.3) auf das Testsystem, danach Merge-Entscheidung (Schranke 2).
