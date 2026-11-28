---
type: spec
title: "BDE-Teilmeldungen: „passt nicht mehr zur FA-Struktur“ filtern, stornieren, umhängen (U-13 B)"
slug: 2026-11-26-bde-u13-waisen-anzeige-admin-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-28
source_backlog: "[[2026-11-26-bde-u13-waisen-anzeige-admin]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: IDEAL
affected_code:
  - IdealAkeWms/Services/BdeTeilelisteService.cs
  - IdealAkeWms/Controllers/BdeBookingsController.cs
  - IdealAkeWms/Views/BdeBookings/Teilmeldungen.cshtml
  - IdealAkeWms/Views/BdeTeileliste/_Kopf.cshtml
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
  - IdealAkeWms/Views/Help/Index.cshtml
  - IdealAkeWms/Views/Help/Changelog.cshtml
  - docs/anwender/17-*.md
  - docs/TESTSZENARIEN.md
  - IdealAkeWms.Tests/Services/BdeTeilelisteServiceTests.cs
  - IdealAkeWms.Tests/Controllers/BdeBookingsController*Tests.cs
open_questions:
  - "1. Umfang Umhängen: A = nur Kennzeichnen/Filtern + vorhandenes Storno; B = zusätzlich „Umhängen“ für Blechteile (Storno + Neuanlage auf die gewählte Strukturzeile). Empfehlung B, Baugruppen nur Storno. (Gerald)"
  - "2. Umhängen technisch als Storno der alten + neue Meldung (Historie sichtbar, kein Schema) oder Schlüssel in der vorhandenen Zeile überschreiben? Empfehlung Storno + Neuanlage. (Gerald)"
  - "3. Prüft Umhängen den Auftragsstatus des Ziels (erledigt/storniert, Sage verpackt/abgeholt) wie das Melden? Empfehlung: nein, es korrigiert bereits gemeldete Arbeit; Hinweistext im Dialog. (Gerald)"
  - "4. Werker-Hinweis im BDE-Kopf: bleibt er nach dieser Spec (Empfehlung) oder gilt die Formulierung aus U-13 Option 1 „dann nur noch dort“ (Hinweis nur noch für BDE-Admin)? Konflikt: Der Admin hat ohne den Werker-Hinweis kein Signal. (Gerald)"
  - "5. Wie oft kommt der Fall an IDEAL vor und was macht die AV/Fertigung heute damit – ist die Arbeit an der neuen Position wirklich getan (dann Umhängen) oder meist hinfällig (dann Storno)? Prüfabfrage P-1 liefert die Zahl. (Fachbereich: Walter Kronberger / Benjamin Andl-Braun)"
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

> Nummern **vorläufig**: Version v1.99.8x (nächste freie nach v1.99.82, die [[2026-11-26-bde-nur-fa-meldung-entfernen-spec]] vorläufig belegt;
> zwei weitere Specs entstehen parallel), TS-20x (TS-202 = v1.99.81). Kein SQL. Der Dev-Lauf prüft und vergibt die Nummern.
> Code-Belege: Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Stand v1.99.81; Pfade ohne Präfix unter `IdealAkeWms/`.

## Ziel / Nutzen (das Warum)

v1.99.81 (Entscheid 12, U-13 A) zeigt dem Werker nur „n Meldung(en) passen nicht mehr zur FA-Struktur – BDE-Admin informieren.“, den Link
bekommt nur der BDE-Admin (`Views/BdeTeileliste/_Kopf.cshtml:163-175`). Der Link führt auf *BDE-Buchungen › Teilmeldungen* ohne Parameter
(`_Kopf.cshtml:171`), und dort ist keine dieser Meldungen gekennzeichnet (Grep „Waise“ in `Views/BdeBookings/` und
`Controllers/BdeBookingsController.cs`: kein Treffer). Dazu filtert die Seite standardmäßig nur die letzten 7 Tage
(`BdeBookingsController.cs:273`), die Zählung im Kopf dagegen ohne Zeitraum. Ältere Fälle findet der Admin also gar nicht.

Ziel: Der BDE-Admin kommt mit einem Klick auf genau diese Meldungen und behandelt jede davon: stornieren (gibt es schon) oder auf die richtige
Strukturzeile umhängen (gibt es noch nicht).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- T1: Eine Regel „passt nicht mehr“ aus `BdeTeilelisteService.Bauen` herauslösen. Der Kopf-Zähler und die Admin-Seite nutzen beide diese Regel.
- T2: Teilmeldungen-Seite bekommt die Spalte „Struktur“ (Text „passt nicht mehr“), filterbar, und in der Filterkarte den Schalter „nur Meldungen, die nicht
  mehr zur FA-Struktur passen“. Ist der Schalter an und kein Datum gesetzt, gilt kein Zeitraum.
- T3: Der Kopf-Link öffnet die Seite mit Schalter an und Kürzel der Werkbank.
- T4 (nur bei Rückfrage 1 = B): Aktion „Umhängen“ für Blechteil-Meldungen, die nicht mehr passen, nur BDE-Admin.
- Hilfe, Anwenderdoku Kapitel 17, Testszenarien, Changelog.

**Out-of-Scope**
- Umhängen von Baugruppen-Meldungen (`SubFA != 0`). Die hängen an einer AG-Buchung der Sub-FA (`BdeTeilMeldung.cs:46-48`,
  `BdeTeilelisteService.cs:782-801`). Umhängen hieße, die Buchung auf einen anderen Arbeitsgang zu verlegen. Für Baugruppen bleibt Storno; der Werker meldet neu.
- Meldungen, deren Haupt-FA ganz aus `FaHierarchyNodes` verschwunden ist. Die Regel zählt sie bewusst nicht (`BdeTeilelisteService.cs:102`, `:408`): Eine
  ganz entfernte Haupt-FA ist nach ADR 0032 aufgeräumt (alle Aufträge erledigt oder Aufbewahrungsfrist abgelaufen). Das ist kein Strukturfehler.
- Lagerbestell-Meldungen (`WarehouseRequisitionItemId != null`). Sie haben keinen Strukturschlüssel und sind nie betroffen (`BdeTeilelisteService.cs:400`).
- Sammel-Storno/-Umhängen mehrerer Zeilen.
- Werker-Hinweis entfernen oder ändern (siehe Rückfrage 4).

## Fachliche Anforderungen

**Was „passt nicht mehr“ heute bedeutet (belegt, v1.99.81)**

Die Regel steht inline in `BdeTeilelisteService.Bauen` (`BdeTeilelisteService.cs:402-408`):
- Eingabe sind die nicht stornierten FA-Meldungen des Kürzels, deren Haupt-FA noch Knoten hat (`LadenAsync`, `:266-267`; Lagerbestellungen raus, `:400`).
- `treffer` = Schlüssel aller Knoten, deren Arbeitsschritte das Kürzel tragen (`:404-406`, Token-Regel `Traegt`, `:138-142`, Groß/Klein egal).
- Eine Meldung passt nicht mehr, wenn ihre Haupt-FA in der Struktur ist und ihr Schlüssel `(HauptFA, VaterFA, SubFA, Position, Artnr)` nicht in `treffer`
  liegt. Gezählt wird je Schlüssel, nicht je Meldung (`:408`, ADR 0037 „Folgen“).
- Die Knotenquelle ist `GetAllAsync` (`Data/Repositories/FaHierarchyNodeRepository.cs:28-34`), also **mit** in Sage verschwundenen Knoten
  (`VerschwundenSeit`). Eine Meldung auf einer verschwundenen Zeile passt deshalb weiter. Die Zeile ist nur nicht mehr meldbar (`BdeTeilelisteService.cs:73-74`).

Damit entsteht „passt nicht mehr“ in zwei Fällen:
1. Der Schlüssel existiert nicht mehr. Sage hat Position oder Artikel geändert, oder der Abgleich hat die Zeile entfernt (ADR 0032: „in Sage nicht
   gefunden“ bzw. „Position aus der Stückliste weg“).
2. Der Schlüssel existiert noch, die Zeile trägt das Kürzel aber nicht mehr in ihren Arbeitsschritten.

**Anforderungen**
- A1: Admin-Seite und Kopf-Zähler nutzen **dieselbe** Regel, eine Methode mit zwei Lesern. Der Zähler im Kopf ändert sein Ergebnis nicht.
- A2: Jede nicht stornierte FA-Meldung, für die die Regel gilt, zeigt in der Spalte „Struktur“ den Text „passt nicht mehr“ (Badge mit Text, Farbe nicht allein).
  Alle anderen Zeilen lassen die Zelle leer, auch stornierte Zeilen und Lagerbestell-Zeilen.
- A3: Schalter „nur Meldungen, die nicht mehr zur FA-Struktur passen“ (`nurOhneStruktur=true`). Ist er an und sind `from`/`to` leer, gibt es keinen
  Zeitraum-Default. Gesetzte Daten gelten weiter. „zurückgenommene zeigen“ bleibt wirksam; stornierte Zeilen passen aber nie.
- A4: Der Kopf-Link (nur BDE-Admin, wie heute) öffnet `BdeBookings/Teilmeldungen?nurOhneStruktur=true&colf_kuerzel=<Kürzel der Werkbank>`. Er zeigt also
  genau die Meldungen, die der Zähler zählt. Mehrere Meldungen je Schlüssel erscheinen als einzelne Zeilen.
- A5: Storno bleibt der vorhandene Weg (`StornoTeilmeldung` → `ZuruecknehmenAsync`, Pflichtgrund, Baugruppen-AG-Buchung wird mitstorniert;
  `BdeBookingsController.cs:348-356`, `BdeTeilelisteService.cs:821-851`).
- A6 (nur B): „Umhängen“ erscheint nur für BDE-Admin, nur an Zeilen mit „passt nicht mehr“, nur bei Blechteilen (`SubFA == 0`). Der Dialog
  bietet als Ziel alle Strukturzeilen derselben Haupt-FA an, die das Kürzel der Meldung tragen und folgende Bedingungen erfüllen:
  - Blechteil (`SubFA == 0`)
  - nicht verschwunden
  - eindeutig
  - bei einer abschließenden Meldung ohne vorhandene abschließende Meldung am Ziel

  Reihenfolge der Ziele: gleiche Artnr zuerst (Position umnummeriert), dann gleiche Position (Artikel getauscht), dann Matchcode. Grund ist Pflicht.
  Gibt es kein Ziel, bleibt nur Storno und der Dialog sagt warum („keine passende Strukturzeile mit Kürzel X in Haupt-FA Y“).
- A7 (nur B): Der Server prüft am Schreibort (ADR 0026) erneut, ob die Meldung noch „passt nicht mehr“ ist, alle Zielbedingungen aus A6 und die Sperre.
  Jede Abweisung kommt als `WarningMessage` mit Grund, „nichts geändert“.

## Technischer Loesungsentwurf

**T1 – eine Regel (Pflicht, Grundlage für alles andere)**
- Neue statische Methode in `BdeTeilelisteService`, z. B.
  `public static HashSet<(TeilSchluessel, string)> PassenNichtMehr(IReadOnlyCollection<FaHierarchyNode> nodes, IEnumerable<BdeTeilMeldung> meldungen)`.
  - Sie nimmt nur nicht stornierte FA-Meldungen.
  - Ergebnis: je `(Schlüssel, Kürzel groß)` „Haupt-FA in `nodes` und kein Knoten mit diesem Schlüssel trägt das Kürzel“ (`Traegt`).
- `Bauen` ersetzt `:404-408` durch `PassenNichtMehr(nodes, meldungen).Count`. `kandidaten` bleibt für die Zeilenbildung.
  - Bauen erhält aus `LadenAsync` nur Meldungen eines Kürzels. Die Zahl bleibt daher gleich.
  - Die Tests `Waisen_ZaehlenMeldungenOhneZeile_NurFuerHauptFaInStruktur` (`IdealAkeWms.Tests/Services/BdeTeilelisteServiceTests.cs:97`) und
    `Bauen_GemeldeteAus_..._WaisenJeSchluessel` (`BdeTeilelisteTeilfertigTests.cs:160`) bleiben unverändert grün.
- Async-Hülle für die Admin-Seite: `Task<HashSet<...>> PassenNichtMehrAsync(IEnumerable<BdeTeilMeldung>)`. Sie lädt die Knoten über `_nodes.GetAllAsync()`, wie
  `LadenAsync:262`: keine `Contains`-Liste über Haupt-FAs, damit keine 2100-Parameter-Grenze (Muster `ZusatzAsync:309-310`). Ohne FA-Meldungen in der
  Liste lädt sie nichts.

**T2 – Teilmeldungen-Seite (Listen-View-Pattern ADR 0005, Bestand erweitern)**
- `BdeBookingsController.Teilmeldungen` (`:269-292`):
  - Parameter `bool nurOhneStruktur = false`.
  - Zeitraum-Default (`:273`) nur, wenn `!nurOhneStruktur`.
  - Nach dem Laden (`:282`) `passtNicht = await teileliste.PassenNichtMehrAsync(alle)`, mit `[FromServices] BdeTeilelisteService` wie `StornoTeilmeldung:351`.
  - Bei `nurOhneStruktur` auf die Treffer einschränken, dann der Spaltenfilter, dann die Pagination (Reihenfolge wie heute).
- Getter `struktur`:
  - `TeilmeldungSpalten` ist statisch (`:295-312`). Je Request eine Kopie mit `["struktur"] = m => passtNicht.Contains(...) ? "passt nicht mehr" : null`.
  - Die View rendert dieselbe Zelle aus einem gemeinsamen statischen Helfer `StrukturText(m, passtNicht)`, damit Zelle und Filter gleich sind (fallstricke
    „Universal-Filter-Pattern: Getter liefert gerenderten Text“).
- View `Views/BdeBookings/Teilmeldungen.cshtml`:
  - Filterkarte: Checkbox `nurOhneStruktur` (Muster `zurueckgenommene`, `:31-36`).
  - Neue `<th data-filterable data-col-key="struktur">Struktur</th>` und eine `<td>` mit demselben Schlüssel.
  - `colspan` der Leerzeile 15 → 16 (`:71`).
  - `#column-config` ergänzen (`:129-147`).
  - Leertext bei Schalter an: „Keine Meldungen, die nicht mehr zur FA-Struktur passen.“
- `ColumnDefinitions.BdeTeilmeldungen` (`Models/ViewModels/ColumnDefinitions.cs:489-...`) um `struktur` ergänzen (Drift-Wächter `ViewKeyDriftGuardTests`).
  Die Spalte ist Text, deshalb kein Eintrag in `ColumnFilterTypes` (`Services/ColumnFilterSyntax.cs:46`).
- CSV-Export ruft die Action ein zweites Mal mit `pageSize=0` auf. Die Action schreibt nichts, die Regel ADR 0005 (Nachtrag CSV) bleibt eingehalten.

**T3 – Kopf-Link**
- `_Kopf.cshtml:171`: `asp-route-nurOhneStruktur="true"` und `asp-route-colf_kuerzel="@Model.Kuerzel"`. `Model.Kuerzel` ist in der Teile-Sicht gesetzt
  (`Controllers/BdeTeilelisteController.cs:185`, `Models/ViewModels/BdeTeilelisteViewModel.cs:10`).
- Sichtbarkeit unverändert: nur `Model.BdeAdmin`. Der View-Test `Kopf_..._WaisenLinkNurBdeAdmin` (`IdealAkeWms.Tests/Views/BdeFaSichtViewTests.cs:85`)
  wird um die Route-Werte erweitert.

**T4 – Umhängen (nur bei Rückfrage 1 = B; Form nach Rückfrage 2, Empfehlung Storno + Neuanlage)**
- Umhängen gibt es heute nicht.
  - Grep `umh(ae|ä)ng|umzuordn|reassign` über `IdealAkeWms/**/*.{cs,cshtml,js}` trifft nur das Lager-Umbuchen.
  - `BdeTeilMeldung` wird nur an zwei Stellen geschrieben: Anlage in `SpeichernAsync` (`BdeTeilelisteService.cs:695-710`) und Storno in
    `ZuruecknehmenAsync` (`:839-849`). Der Schlüssel wird nie geändert.
  - Das sind alle Schreibwege. Weitere Leser im Web: `BdeBookingsController`, `BdeTageshistorie`. Im Dienst `IDEALAKEWMSService` gibt es keinen Treffer auf
    `BdeTeil`.
- Service: `Task<(bool Ok, string Text)> UmhaengenAsync(int meldungId, string? zielText, string? grund)` neben `ZuruecknehmenAsync`, in
  `InTransaktionAsync` (`:887-897`):
  1. Grund Pflicht. Meldung laden (nicht storniert, `WarehouseRequisitionItemId == null`, `SubFA == 0`).
  2. Die Meldung muss nach `PassenNichtMehr` noch „passt nicht mehr“ sein.
  3. `TeilSchluessel.Parse(zielText)` (`:22-30`). Gleiche Haupt-FA, Ziel ≠ Quelle.
  4. Sperre `AnwendungssperreAsync(_ctx, Sperre(ziel, kuerzel))` (`:879-881`, Muster `MeldeZeileAsync:716`).
  5. Ziel prüfen wie `MeldeZeileAsync:722-741`: genau ein Knoten, `Traegt`, nicht verschwunden, zusätzlich `SubFA == 0`.
  6. Ist die Meldung abschließend und gibt es am Ziel schon eine abschließende Meldung → „bereits gemeldet“ (`MeldungenAsync:869-873`, `BereitsText`).
  7. In **einem** `SaveChangesAsync`:
     - Alte Meldung `IsCancelled = true`, `CancellationReason = "umgehängt auf Pos. <p> (<Artnr>): <Grund>"`, `SetModified`.
     - Neue Meldung mit Zielschlüssel und `Matchcode` des Ziels. `Kuerzel`, `ProductionWorkplaceId`, `BdeOperatorId`, `BdeTerminalId`, Mengen,
       `SollmengeBeiMeldung`, `GemeldetAm` und `IstAbschliessend` werden aus der alten übernommen. `Created*` = BDE-Admin.
     - Der gefilterte UNIQUE (`UX_BdeTeilMeldungen_Schluessel`, ADR 0037) ist die letzte Wache: 2601/2627 → „bereits gemeldet“, nichts gespeichert.
       Muster `SpeichernAsync:703-709`, dort aber ohne Detach-und-weiter.
- Controller: `[HttpPost][RequireBdeAdminAccess][ValidateAntiForgeryToken] TeilmeldungUmhaengen(int id, string? ziel, string? reason)`. Danach Redirect
  zurück auf `Teilmeldungen` mit denselben Filtern (`returnUrl` lokal prüfen, Muster der Seite).
- View: Knopf „Umhängen“ neben „Stornieren“ (`Teilmeldungen.cshtml:91-116`). Modal mit `<select name="ziel">` (Wert = `TeilSchluessel.Text`,
  Text = „Pos. <p> · <Matchcode> · <Artnr> · Soll <n>“) und Pflichtgrund. Die Kandidaten rechnet der Controller nur für die Zeilen der aktuellen Seite mit
  „passt nicht mehr“ (dieselbe Knotenliste wie T1, keine zweite Abfrage). „Abbrechen“ als `btn-outline-secondary` (U-2-Befund; das vorhandene Storno-Modal
  nutzt noch `btn-secondary`, `:110`, und wird im selben Zug angeglichen).

**Wirkung auf Sage / OSEON (belegt: keine)**
- `BdeTeilMeldung` schreibt nichts nach Sage (`Models/BdeTeilMeldung.cs:10`, `Services/BdeTeilelisteService.cs:110`, ADR 0030).
- Der einzige Sage-Schreibweg ist die Lagerbuchungs-Queue über `StockMovement`. Kein `Bde*.cs` unter `Services/` verwendet `StockMovement` (Grep).
- `SageRueckmeldungAktiv` ist seit v1.79.0 entfernt, weil es keinen Leser gab (`Models/AppSettingDefinitions.cs:245`).
- Storno einer Baugruppen-Meldung storniert die verknüpfte `BdeBooking` (`BdeTeilelisteService.cs:843-848`). `BdeBookingService` hat ebenfalls keinen
  Sage-/Stock-Schreibweg (Grep `StockMovement|Sage` in `Services/BdeBookingService.cs`: nur das Lesen von `SageStatus` für die Sperre, `:590-609`).
- Wirkung im WMS: Der AG-Status der Teileliste und der FA-Struktur liest die Meldungen (`BdeTeilelisteService.cs:296-303`). Nach dem Umhängen zeigt die
  Zielzeile „läuft“ bzw. „erledigt“, die Quelle zählt nicht mehr.
- Eine spätere BDE→Sage-Rückmeldung ([[2026-10-30-fa-rueckmeldung-an-sage]], nur Idee) müsste stornierte Meldungen ignorieren. Das gilt heute schon
  für jeden Storno.

## Migrations-/SQL-Auswirkungen

Keine. Keine neue Spalte, kein Index. Umhängen nutzt die vorhandenen Spalten `IsCancelled`/`CancellationReason` und eine neue Zeile. Kein SQL-Skript, kein
FreshInstall-Eintrag. Falls Rückfrage 2 eine Rückverweis-Spalte verlangt (alte ↔ neue Meldung), entsteht daraus eine Migration plus `SQL/1xx` mit
`COL_LENGTH`-Guard. Das ist ausdrücklich nicht Teil der Empfehlung.

**Prüfabfrage P-1 (WMS-DB IDEAL, nur lesen; DEV und vor Prod):** Sie zählt Fall 1 („Schlüssel fehlt“). Fall 2 (Kürzel nicht mehr in den Arbeitsschritten)
braucht die C#-Token-Regel und ist hier nicht enthalten.
```sql
SELECT m.Kuerzel,
       COUNT(*) AS Meldungen,
       SUM(CASE WHEN m.SubFA <> 0 THEN 1 ELSE 0 END) AS davonBaugruppe,
       MIN(m.GemeldetAm) AS Aelteste
FROM dbo.BdeTeilMeldungen m
WHERE m.IsCancelled = 0 AND m.WarehouseRequisitionItemId IS NULL
  AND EXISTS (SELECT 1 FROM dbo.FaHierarchyNodes h WHERE h.HauptFA = m.HauptFA)
  AND NOT EXISTS (SELECT 1 FROM dbo.FaHierarchyNodes n
                  WHERE n.HauptFA = m.HauptFA AND n.SubFA = m.SubFA
                    AND (n.VaterFA = m.VaterFA OR (n.VaterFA IS NULL AND m.VaterFA IS NULL))
                    AND (n.Position = m.Position OR (n.Position IS NULL AND m.Position IS NULL))
                    AND ISNULL(NULLIF(n.Artnr, N''), N'') = ISNULL(NULLIF(m.Artnr, N''), N''))
GROUP BY m.Kuerzel;
```
Die Zahl entscheidet mit über Rückfrage 1/5. An AKE ist die Regel wirkungslos, wenn `FaHierarchyNodes` leer ist (`Bauen:407-408`). Ob die Tabelle an AKE
leer ist, ist hier nicht geprüft: `SELECT COUNT(*) FROM dbo.FaHierarchyNodes` an AKE DEV als Prüfschritt.

## Einstellungen

keine

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

- Storno: unverändert, `SetModified` an Meldung und verknüpfter Buchung (`BdeTeilelisteService.cs:842-847`).
- Umhängen (B):
  - Alte Meldung: `ModifiedAt/By/ByWindows` = BDE-Admin über `SetModified` (`:853-858`), `CancellationReason` nennt Ziel und Grund.
  - Neue Meldung: `CreatedAt/By/ByWindows` = BDE-Admin. `GemeldetAm`, `BdeOperatorId` und `BdeTerminalId` bleiben die des Werkers, weil sie die
    eigentliche Meldung beschreiben.
- Filter und Spalte: nur Lesen, keine Audit-Wirkung.

## Akzeptanzkriterien

1. `PassenNichtMehr` existiert einmal. `Bauen` nutzt sie (kein zweiter Inline-Ausdruck), die beiden vorhandenen Waisen-Tests bleiben unverändert grün.
2. Unit-Test: Meldung mit Schlüssel ohne Knoten → passt nicht mehr. Ein Knoten mit gleichem Schlüssel, aber ohne Kürzel in den Arbeitsschritten → passt nicht
   mehr. Ein Knoten mit `VerschwundenSeit` und Kürzel → passt. Haupt-FA ohne Knoten → passt (wird nicht gezählt). Eine stornierte Meldung und eine
   Lagerbestell-Meldung sind nie enthalten.
3. Teilmeldungen-Seite: Zeilen, die nicht mehr passen, zeigen in der Spalte „Struktur“ den Text „passt nicht mehr“. Der Spaltenfilter `colf_struktur=passt nicht mehr`
   und der Schalter liefern dieselben Zeilen. `TotalCount` und Pagination stimmen mit der gefilterten Menge überein.
4. Schalter an, keine Daten gesetzt: Meldungen älter als 7 Tage erscheinen. Schalter an mit gesetztem Zeitraum: Der Zeitraum gilt.
5. Kopf-Link als BDE-Admin führt auf die Seite mit Schalter an und Kürzel. Die Zahl der unterschiedlichen Schlüssel in der Liste ist gleich der Zahl im
   Kopf-Hinweis. Mehrere Meldungen je Schlüssel erscheinen als einzelne Zeilen.
6. Nicht-Admin (Schichtleiter): Spalte und Schalter sichtbar, kein „Stornieren“, kein „Umhängen“. POST auf die Aktionen ohne BDE-Admin → abgewiesen
   (`RequireBdeAdminAccess`).
7. Spaltenwahl: `struktur` ist in `ColumnDefinitions.BdeTeilmeldungen` und `#column-config` registriert, der Drift-Test ist grün, die Einstellung bleibt nach dem Neuladen.
8. CSV-Export der Seite mit Schalter an enthält nur die gefilterten Zeilen und die Spalte „Struktur“.
9. (B) Umhängen einer Teilmeldung auf eine Zielzeile mit Kürzel:
   - Die alte Zeile ist storniert mit dem Grund „umgehängt auf Pos. …“.
   - Eine neue Zeile hat den Zielschlüssel, Werker, Zeitpunkt und Mengen der alten.
   - Der Kopf-Zähler sinkt um 1. Die Zielzeile in der Teileliste zeigt „gemeldet X von Soll“ inklusive der Menge.
10. (B) Abgewiesen mit Grund, DB unverändert (Unit-Tests):
    - Baugruppen-Meldung
    - Meldung, die passt
    - Ziel in anderer Haupt-FA
    - Ziel ohne Kürzel
    - Ziel verschwunden
    - Ziel nicht eindeutig
    - abschließende Meldung auf ein Ziel mit Abschluss
    - leerer Grund
    - bereits stornierte Meldung
11. (B) Kein Ziel vorhanden: Der Dialog zeigt keinen Auswahlknopf, sondern den Satz „keine passende Strukturzeile mit Kürzel X in Haupt-FA Y – bitte stornieren“.
12. Nach Storno oder Umhängen wurde nichts nach Sage gesendet: `SageBookingQueueItems` hat keine neue Zeile (DEV-Nachweis).

## Test-Szenarien

Neues Kapitel **TS-20x** (vorläufig) in `docs/TESTSZENARIEN.md`, Testindex nachziehen. Bezug TS-152.10 (Hinweis nach Positionsänderung), TS-193.9
(Storno), TS-202 Punkt 5 (Werker-Hinweis).
- 20x.1 Vorbereitung DEV IDEAL: eine Teilmeldung auf ein Blechteil anlegen. Dann ihren Schlüssel per SQL „verwaisen“ (Position der Meldung auf eine
  nicht vorhandene Position setzen, `qa-agent` als `ModifiedBy`). Eine zweite Meldung auf einer Baugruppe ebenso. Kopf zeigt „2 Meldungen passen nicht
  mehr …“.
- 20x.2 Als BDE-Admin den Link öffnen: Schalter an, Kürzel gefiltert, genau die zwei Zeilen mit „passt nicht mehr“. Gegenprobe ohne Schalter mit Zeitraum
  „letzte 7 Tage“.
- 20x.3 Meldung mit `GemeldetAm` vor 10 Tagen: erscheint mit Schalter an ohne Datum, nicht im Standard-Zeitraum.
- 20x.4 Spaltenfilter `passt nicht mehr` und `!passt nicht mehr`, CSV-Export, Spaltenwahl ein/aus und Neuladen.
- 20x.5 (B) Blechteil umhängen auf die Zeile mit gleicher Artnr: Erfolgsmeldung, alte Zeile storniert (mit „zurückgenommene zeigen“), neue Zeile
  mit Werker/Zeit der alten. Teileliste: Zielzeile zeigt die Menge, Kopf zählt 1.
- 20x.6 (B) Baugruppe: kein „Umhängen“, nur „Stornieren“. Storno storniert die AG-Buchung mit (BDE-Buchungen, „storniert“).
- 20x.7 (B) Manipulierte Requests (Devtools): fremde Haupt-FA, Ziel ohne Kürzel, passende Meldung → abgewiesen. Sonst durch Unit-Tests belegt.
- 20x.8 Schichtleiter ohne BDE-Admin: Spalte und Schalter sichtbar, keine Aktionen. Ohne zweites Konto ist das nur Manual-UAT.
- 20x.9 `SageBookingQueueItems` vor und nach den Schritten: unverändert.
- 20x.10 AKE DEV: Seite lädt, Spalte leer, Schalter liefert „Keine Meldungen …“.

## Etappen (nur bei epic: true)

Kein Epic. Reihenfolge im Dev-Lauf: T1 → T2 → T3 → (T4 nur bei B), je ein Commit.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Vorläufig.** Der QA-Lauf bestätigt die Angaben am echten Diff.

- **Web-App:** ja (Controller, Views, Service-Klasse im Web-Projekt)
- **Service:** nein für die Funktion. Das Paket enthält wie üblich beide Komponenten, weil beide `AppVersion.cs` die Version tragen.
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine. Kein SQL, keine Einstellung, kein Leitstand-Schritt. Optional ein `Info`-Eintrag ist
  nicht nötig, weil sich für Werker nichts ändert.
- **Publish-Befehle** (im Worktree):
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  - `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- Hilfe (`Views/Help/Index.cshtml`, BDE-Teileliste/Teilmeldungen), Anwenderdoku Kapitel 17, `Changelog.cshtml`, Brain-Changelog,
  `codebase/controller.md` (neue Aktion `TeilmeldungUmhaengen`, nur bei B).

## Offene Rueckfragen

1. **Umfang Umhängen (Gerald).**
   - Ein Werker meldet am Blechteil Pos. 9 (KA). Danach nummeriert Sage die Position auf 10 um.
   - Die Meldung hängt dann an einem Schlüssel, den es nicht mehr gibt. Die neue Zeile Pos. 10 steht in der Teileliste als offen.
   - Storno allein lässt die Arbeit verschwinden. Der Werker müsste neu melden, und dann stimmen Zeit und Person nicht mehr.

   Antworten:
   - A: nur Kennzeichnen/Filtern + vorhandenes Storno.
   - B: zusätzlich „Umhängen“ für Blechteile auf eine Zeile derselben Haupt-FA mit dem Kürzel; Baugruppen nur Storno. **(empfohlen)**
     Aufwand S–M, Prüfungen aus `MeldeZeileAsync` wiederverwendet.

   Ist die Prüfabfrage P-1 an IDEAL nahezu 0, reicht A.
2. **Form des Umhängens (Gerald).**
   - a) Alte Meldung stornieren („umgehängt auf Pos. …“) und neu anlegen. Die Historie bleibt auf der Seite sichtbar, kein Schema. **(empfohlen)**
   - b) Schlüssel der vorhandenen Zeile überschreiben. Eine Zeile weniger, aber der alte Schlüssel ist danach nirgends mehr zu sehen; die Audit-Felder
     sagen nur wer und wann.
3. **Auftragsstatus beim Umhängen (Gerald).**
   - Melden weist ab, wenn der Auftrag erledigt oder storniert ist oder in Sage verpackt/abgeholt (`BdeTeilelisteService.cs:743-760`).
   - Umhängen korrigiert bereits gemeldete Arbeit. Empfehlung: **nicht** prüfen, im Dialog den Status des Ziels anzeigen.
   - Alternative: dieselben Sperren wie beim Melden.
4. **Werker-Hinweis (Gerald), Konflikt.**
   - Die gewählte Antwort zu U-13 lautete „später eine Waisen-Anzeige auf der Admin-Seite, dann nur noch dort“. Entscheid 12 hielt aber nur „A, Admin-Anzeige
     Backlog“ fest.
   - Nimmt man den Hinweis beim Werker weg, bekommt der Admin kein Signal mehr. Er öffnet die Teilmeldungen nicht von sich aus, und der Terminal-Benutzer ist
     nicht der Admin (U-13 Problem).
   - Empfehlung: **Hinweis beim Werker bleibt** wie in v1.99.81.
   - Alternative: Hinweis nur für BDE-Admin und dafür eine Zahl „n passen nicht mehr“ auf *BDE-Buchungen* (eigene kleine Erweiterung).
5. **Häufigkeit und fachlicher Umgang (Fachbereich: Walter Kronberger / Benjamin Andl-Braun).**
   - Wie oft ändert Sage an IDEAL Positionen oder Artikel einer laufenden FA-Struktur, nachdem schon gemeldet wurde?
   - Ist die gemeldete Arbeit dann an der neuen Position tatsächlich getan (→ Umhängen) oder meist hinfällig (→ Storno)?
   - Die Zahl liefert P-1; die Einordnung kann nur die Fertigung geben.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Umfang Umhängen: A (nur Filter + Storno) oder B (zusätzlich Umhängen für Blechteile)? →
2. Form: a) Storno + Neuanlage oder b) Schlüssel überschreiben? →
3. Auftragsstatus des Ziels beim Umhängen prüfen? →
4. Werker-Hinweis bleibt oder nur noch für BDE-Admin? →
5. (Fachbereich: Walter Kronberger / Benjamin Andl-Braun) Häufigkeit und Umhängen vs. Storno? →
