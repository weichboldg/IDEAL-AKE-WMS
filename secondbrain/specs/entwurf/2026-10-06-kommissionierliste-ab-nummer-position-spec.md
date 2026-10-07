---
type: spec
title: "IDEAL Kommissionierliste: AB-Nummer und Position anzeigen (Liste, Summiert, Druck, PDF)"
slug: 2026-10-06-kommissionierliste-ab-nummer-position-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-07
source_backlog: "[[2026-10-06-kommissionierliste-ab-nummer-position]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml Z. 115-138 (Gruppenkopf je HauptFA): zeigt heute bereits je OrderInfo `Montage-Abt. · Kunde · AB <ABNr> · KO <Datum>` (Z. 129), aber KEINE `Pos`. Nur im Restumfang (Pos in der Liste, nice to have, Ruecklegung offen): `Pos` hinter der AB-Nummer ergaenzen (Muster `Views/FaHierarchyBeschichtung/Index.cshtml` Z. 91-92: `· AB <ABNr>` + ` / <Pos>`). UEBERHOLT (Antworten 1/3, 2026-10-07): Ersatz durch ein Partial `_KommissionierAuftragsKopf` - Begruendung (vier Render-Stellen) entfaellt mit der Summiert-Ansicht."
  - "IdealAkeWms/Views/FaHierarchyKommissionierListen/Print.cshtml Z. 68-102 (Kombi-Hinweis + `head-table` mit Spalte `AB-Nr.`, Z. 84/95, KEINE `Pos`-Spalte): nur im Restumfang zusaetzliche Spalte `Pos` direkt nach `AB-Nr.` (Muster `Views/FaHierarchyBeschichtung/Print.cshtml` Z. 88-89/101-102). Wirkt automatisch auf das PDF, weil `Pdf` dieselbe View `Print` rendert (`FaHierarchyKommissionierListenController.cs` Z. 209). UEBERHOLT (Antworten 1/3, 2026-10-07): Ersatz durch ein Partial `_KommissionierAuftragsKopfDruck`."
  - "UEBERHOLT (Antwort 1, 2026-10-07: keine AB-Nummer in der summierten Ansicht) - IdealAkeWms/Views/FaHierarchyKommissionierListen/Summiert.cshtml Z. 103-116 (Gruppenkopf je HauptFA: nur `HauptFA` + PDF-Link, keine Auftrags-Kopfdaten): nach dem PDF-Link (Z. 114) `<partial name=\"_KommissionierAuftragsKopf\" model=\"...\" />` mit den OrderInfos dieser HauptFA aus `Model.OrderInfosByHauptFa`."
  - "UEBERHOLT (Antwort 1, 2026-10-07) - IdealAkeWms/Views/FaHierarchyKommissionierListen/PrintSummiert.cshtml Z. 61-69 (`group-head`: Titel + Barcode, keine Kopfdaten): nach dem Barcode-Block `<partial name=\"_KommissionierAuftragsKopfDruck\" ... />`. Wirkt automatisch auf `PdfSummiert` (rendert `PrintSummiert`, Controller Z. 268)."
  - "UEBERHOLT (Antworten 1/3, 2026-10-07: nur noch eine Bildschirm-Stelle) - NEU IdealAkeWms/Views/FaHierarchyKommissionierListen/_KommissionierAuftragsKopf.cshtml — Bildschirm-Gruppenkopf (Model `IReadOnlyList<FaHierarchyOrderInfo>`): Kombi-Badge `mehrdeutig (Kombigeraet)` bei `Count > 1` (bisher Index Z. 115-120), je OrderInfo eine `<span>` mit Montage-Abt., Kunde, `AB <ABNr>`, ` / <Pos>`, KO-Termin; leere Liste => `Keine Auftrags-Kopfdaten zu diesem HauptFA.` (bisher Index Z. 135-138)."
  - "UEBERHOLT (Antworten 1/3, 2026-10-07: nur noch eine Druck-Stelle) - NEU IdealAkeWms/Views/FaHierarchyKommissionierListen/_KommissionierAuftragsKopfDruck.cshtml — Druck-Kopf (Model wie oben): Kombi-Hinweis (bisher Print Z. 68-76) + `head-table` mit Spalten Montage-Abt. / Kunde / AB-Nr. / Pos / KO-Termin / FE-Termin; Styles `.head-table`/`.note` muessen in `PrintSummiert.cshtml` ergaenzt werden (dort fehlen sie heute, vgl. Print Z. 24-26/31 vs. PrintSummiert Z. 13-30)."
  - "UEBERHOLT (Antwort 1, 2026-10-07) - IdealAkeWms/Services/KommissionierListenService.cs `AggregateSummiert` Z. 147-211: `allInfos` ist bereits Parameter (Z. 149) und wird heute NUR fuer den KW-Vorfilter gelesen (Z. 161-164). NEU nach dem Gruppen-Paging (Z. 198-203): Lookup `OrderInfosByHauptFa` nur fuer die HauptFA der Ergebnisseite (`pageRows.Select(r => r.HauptFA).Distinct()`), gruppiert aus `allInfos ?? Enumerable.Empty<...>()` — fan-out-frei wie `FaHierarchyListBuilder.Build` Z. 151-163. Aggregation, Summierschluessel, Filter und Paging bleiben unveraendert."
  - "UEBERHOLT (Antwort 1, 2026-10-07) - IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierSummiertViewModel.cs — neue Eigenschaft `IReadOnlyDictionary<int, List<FaHierarchyOrderInfo>> OrderInfosByHauptFa` (leeres Dictionary als Default) an `FaHierarchyKommissionierSummiertResult` (Z. 43-52), `FaHierarchyKommissionierSummiertViewModel` (Z. 60-89) und `FaHierarchyKommissionierSummiertPrintViewModel` (Z. 96-112)."
  - "UEBERHOLT (Antwort 1, 2026-10-07) - IdealAkeWms/Controllers/FaHierarchyKommissionierListenController.cs — `OrderInfosByHauptFa = result.OrderInfosByHauptFa` durchreichen in `Summiert` (VM Z. 137-153), `PrintSummiert` (VM Z. 232-240) und `PdfSummiert` (VM Z. 256-264; dort ist die Ergebnismenge ungepaged, das Dictionary enthaelt also alle HauptFA — das Partial liest nur den Eintrag zu `hauptFa`). `Index`/`Print`/`Pdf` unveraendert (OrderInfos kommen dort schon ueber `FaHierarchyListGroupViewModel.OrderInfos`, `FaHierarchyListBuilder.cs` Z. 161-163)."
  - "UEBERHOLT (Antwort 1, 2026-10-07) - IdealAkeWms.Tests/Services/KommissionierListenServiceTests.cs — neue Tests fuer `AggregateSummiert`: (a) Kombi-HauptFA mit zwei OrderInfos => beide im Lookup, keine Aenderung an Zeilenzahl/Summen (kein kartesisches Produkt); (b) Gruppen-Paging `pageSize: 1` => Lookup enthaelt nur die HauptFA der Seite; (c) `allInfos` null/leer => leeres Dictionary, kein Fehler."
  - "IdealAkeWms/Views/Help/Index.cshtml (Abschnitt Kommissionierlisten, um Z. 396-420): AB-Nr./Pos im Gruppenkopf und Kombigeraete-Darstellung beschreiben."
  - "docs/TESTSZENARIEN.md — neues Kapitel (naechste freie Nummer zum Umsetzungszeitpunkt; Buendel-Stand 2026-10-06: hoechstes Kapitel TS-80) + secondbrain/tests/testszenarien-index.md."
  - "IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs (naechste freie Version zum Umsetzungszeitpunkt; Buendel-Stand 2026-10-06: 1.47.2) + Views/Help/Changelog.cshtml."
  - "ENTFAELLT (Antworten 1/3, 2026-10-07: keine Spalte) - NUR bei Rueckfrage 1 = Spalte: zusaetzlich ColumnDefinitions.FaHierarchyKommissionierListen/-Summiert, `#column-config`, `<th data-col-key>`, `ShowCol(...)` in Print/PrintSummiert, `ColumnMap`/`SummiertColumnMap` und der Drift-Guard `IdealAkeWms.Tests/Views/KommissionierSpaltenKonsistenzTests.cs` (vergleicht genau diese vier Quellen, Z. 42-43/56-71) — siehe Rueckfrage 4."
  - "ENTFAELLT (Antwort 3, 2026-10-07: Suche nicht gefordert) - NUR bei Rueckfrage 3 = Suche nach AB-Nummer gewuenscht: zusaetzlich Filterkarte beider Views, `KommissionierListenService.BuildAsync` (Z. 95-110) und `AggregateSummiert`, `ToggleSharedQueryKeys` (Z. 217-221) — siehe Rueckfrage 3."
open_questions:
  - "2 Kombinationsgeraete: alle AB/Pos des HauptFA anzeigen, nicht eine willkuerlich waehlen (Empfehlung der Notiz) - bestaetigt? (Nach Antworten 1/3 nur noch fuer `Pos` in der Liste relevant; die Liste zeigt die AB-Nr. heute schon fuer alle OrderInfos.)"
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

> [!info] Umsetzungsort (Hinweis, keine Entscheidung)
> Nach Schranke 2 des IDEAL-Buendels, aus `main` (neuer Worktree). Der gesamte betroffene Code
> (`FaHierarchyKommissionierListenController`, `KommissionierListenService`, die Views unter
> `Views/FaHierarchyKommissionierListen/`) existiert heute nur im nicht gemergten Buendel-Worktree
> `.claude/worktrees/2026-08-07-ideal-teile-1-5`; alle Zeilenangaben dieser Spec sind dort gemessen
> (Stand 2026-10-06: `AppVersion 1.47.2`, hoechstes Testszenario-Kapitel `TS-80`). Der Dev-Lauf
> gleicht die Zeilen gegen den dann aktuellen `main`-Stand ab. Die Backlog-Notiz nennt als Alternative
> eine Etappe im Buendel, falls Gerald es vor dem Merge braucht — das entscheidet der Mensch, nicht
> diese Spec.
>
> **Version und TS-Kapitel:** jeweils die naechste freie Nummer zum Umsetzungszeitpunkt.

> [!important] Entscheidungsstand 2026-10-07 (Keyuser, Freigabe-Antworten 1 und 3)
> - **Summiert-Ansicht (Bildschirm, Druck, PDF): keine AB-Nummer** — verworfen.
> - **Liste:** AB-Nummer ist dort bereits vorhanden (belegt: `Index.cshtml` Z. 129, `Print.cshtml`
>   Z. 95); Ergaenzung ist nur **nice to have**. Offen bleibt damit allein `Pos` im bestehenden
>   Gruppenkopf der Liste.
> - **Keine Spalte** je Positionszeile, **keine Suche/Filter** nach AB-Nummer.
> - Ob der Restumfang (`Pos` in der Liste) umgesetzt oder die Spec zurueckgelegt wird, wird separat
>   entschieden — nicht in dieser Spec.
>
> Textstellen unten, die die Summiert-Ansicht, die Partials, eine Spalte oder eine Suche betreffen,
> sind als **ueberholt** bzw. **verworfen** markiert und bleiben als Begruendungsspur stehen.

## Ziel / Nutzen (das Warum)

Der Kommissionierer soll auf der IDEAL-Kommissionierliste (Bildschirm, Druck, PDF) sehen, zu welcher
**Auftragsbestaetigung und Position** ein HauptFA gehoert — ohne in die FA-Liste oder nach Sage
wechseln zu muessen.

**Zentraler Befund (am Code erhoben):** Die Anforderung ist zu einem Teil bereits erfuellt, zum
anderen nicht — die Luecke ist kleiner und anders verteilt, als die Notiz annimmt:

| Ansicht | AB-Nr. heute | Pos heute | Beleg |
|---|---|---|---|
| Liste (Bildschirm) | **ja**, im Gruppenkopf, alle OrderInfos | nein | `Index.cshtml` Z. 121-134, AB in Z. 129 |
| Liste Druck + PDF | **ja**, `head-table` im Gruppenkopf, alle OrderInfos | nein | `Print.cshtml` Z. 77-102, AB in Z. 84/95; `Pdf` rendert `Print` (Controller Z. 209) |
| Summiert (Bildschirm, **Standard-Einstieg seit v1.47.0**) | nein | nein | `Summiert.cshtml` Z. 103-116: nur HauptFA + PDF-Link |
| Summiert Druck + PDF | nein | nein | `PrintSummiert.cshtml` Z. 61-69; `PdfSummiert` rendert `PrintSummiert` (Controller Z. 268) |

Die Summiert-Ansicht hat die Kopfdaten in [[2026-09-25-kommissionierliste-summierung-pdf-spec]]
bewusst nicht bekommen („Aggregatzeile kennt keine `OrderInfos`, ein Nachladen ist nicht
angefordert — YAGNI"). Diese Anforderung ist genau dieses Nachladen; die damalige YAGNI-Begruendung
ist damit fuer den Gruppenkopf **ueberholt**.

**Praezedenz im Bestand:** Der Beschichtungsauftrag (Teil 4) zeigt AB-Nr. **und** Pos bereits im
Gruppenkopf (`Views/FaHierarchyBeschichtung/Index.cshtml` Z. 91-92, `Print.cshtml` Z. 88-89/101-102).
Die Kommissionierliste zieht nach diesem Muster nach — kein neues Darstellungsmuster.

## Umfang (In-Scope / Out-of-Scope)

Stand nach Freigabe-Antworten 1 und 3 (2026-10-07): Restumfang nur `Pos` im Gruppenkopf der
**Liste** (nice to have); Umsetzung oder Ruecklegung wird separat entschieden.

**In-Scope**

1. `Pos` im Gruppenkopf der **Liste** ergaenzen (Bildschirm, Druck, PDF) — AB-Nr. ist dort schon
   (nice to have, Antwort 3).
2. ~~AB-Nr. **und** Pos im Gruppenkopf der **Summiert-Ansicht** (Bildschirm, Druck, PDF) — dafuer die
   OrderInfos je HauptFA bis in die Summiert-ViewModels durchreichen.~~ **Verworfen** (Antwort 1:
   keine AB-Nummer in der summierten Ansicht).
3. Kombinationsgeraete: **alle** OrderInfo-Zeilen des HauptFA anzeigen, jeweils mit Montage-Abteilung
   als Unterscheidungsmerkmal, plus der bestehende Mehrdeutig-Hinweis — keine Auswahl einer Variante.
   (Rueckfrage 2 offen; die Liste verhaelt sich heute schon so, Index Z. 124-132.)
4. ~~Zwei kleine Partials, damit vier Render-Stellen dieselbe Kopfdarstellung zeigen (s. Loesungsentwurf).~~
   **Ueberholt:** Ohne Summiert bleiben je Kanal eine Render-Stelle (Index, Print); `Pos` wird dort
   direkt ergaenzt.
5. Hilfeseite, Anwender-Changelog, Testszenario, Version.

**Out-of-Scope**

- AB/Pos als Spalte je Positionszeile — **entschieden: nein** (Antworten 1/3).
- Suche/Filter nach AB-Nummer — **entschieden: nicht gefordert** (Antwort 3).
- Jede Aenderung an der Summiert-Ansicht (Kopfdaten, Summierschluessel, Paging, Spaltenfilter,
  Spaltenauswahl) — **entschieden** (Antwort 1).
- Andere Listen (Beschichtung hat es bereits; Vormontage zeigt AB, aber nicht Pos —
  `Views/FaHierarchyVormontage/Index.cshtml` Z. 116 — nicht angefordert, nicht angefasst).
- Sync/Schema: `ABNr`/`Pos` sind bereits importiert (s. u.), nichts am Service.
- AKE-Standort: Die Kommissionierlisten sind IDEAL-only (Toggle `FaHierarchyKommissionierlistenAktiv`,
  Default aus; `controller.md` Z. 77/105). AKE ist nicht betroffen.

## Fachliche Anforderungen

### Datenherkunft von `ABNr` und `Pos` (am Code belegt, Bedeutung in Sage unverifiziert)

- **Geschrieben** ausschliesslich vom Service-Sync `FaHierarchySyncService.ReadOrderInfosAsync`
  (`IDEALAKEWMSService/Services/FaHierarchySyncService.cs` Z. 263-279), 1:1 als String aus den
  Spalten `ABNr` und `Pos` der Sage-View `Sync:FaHierarchyInfosViewName`, Default
  `[vw_IDEAL-AKE_Kommissionierung_FAInfos]` (`ServiceSettingDefinitions.cs` Z. 39; SELECT in
  `FaHierarchySql.BuildInfosSelectSql`, Z. 135-143). Full-Refresh (DELETE + Insert in einer
  Transaktion, Z. 129-138). Keine Transformation, kein weiterer Schreibpfad (Grep `\.Pos\b`:
  einziger weiterer Treffer ist das Mapping `ApplicationDbContext.cs` Z. 1042).
- **Gelesen** heute: `ABNr` in Liste/Druck Kommissionierung, Beschichtung, Vormontage, FA-Struktur,
  FA-Liste und Freitextsuche `FaHierarchyController.MatchesFreeText` Z. 204-207; `Pos` nur in der
  Beschichtung (Index Z. 92, Print Z. 102).
- Beide Felder sind `string?`, `nvarchar(200)` (`ApplicationDbContext.cs` Z. 1041-1042) — Anzeige
  als Rohtext, keine Formatierung.
- **Was die Spalte `Pos` in Sage fachlich ist** (Auftragsposition der AB? Positionsnummer im
  PPS?), steht nur in der View-Definition und ist hier **nicht** verifiziert. Ebenso, ob die
  OrderInfo-Zeilen eines Kombigeraets dieselbe oder verschiedene AB/Pos tragen. Die Anzeige haengt
  davon nicht ab (Rohwert, alle Zeilen); die Pruefabfragen stehen im Deploy-Abschnitt (P1/P2).

### Anzeige im Gruppenkopf (entschieden 2026-10-07, Antworten 1/3: nur Liste, keine Spalte, nicht Summiert)

AB/Pos gehoeren zum Auftrag, nicht zur Kommissionier-Position: `FaHierarchyOrderInfo` ist laut
Klassenkommentar „ein Datensatz = ein Auftrag (ABNr + Pos)" (`Models/FaHierarchyOrderInfo.cs` Z. 7)
und wird ueber `HauptFA` verknuepft (Z. 17). Je Positionszeile waere der Wert bei jedem Kombigeraet
mehrdeutig. Deshalb Gruppenkopf, Format wie in der Beschichtung: `AB <ABNr> / <Pos>`; fehlt `Pos`,
nur `AB <ABNr>`; fehlt `ABNr`, entfaellt das AB-Fragment (wie heute Index Z. 129).

### Kombinationsgeraete (Rueckfrage 2 — offen; nach Antwort 1 nur noch die Liste betroffen)

Hat ein HauptFA mehrere OrderInfo-Zeilen (`FaHierarchyListGroupViewModel.IsAmbiguous`,
`FaHierarchyListGroupViewModel.cs` Z. 30), erscheinen **alle** Zeilen — je eine mit
Montage-Abteilung, Kunde, AB/Pos, KO-Termin (Bildschirm) bzw. eine Zeile der `head-table` (Druck).
Keine Variante wird gewaehlt, kein Join in die Positionszeilen (kartesisches Produkt, siehe
[[2026-08-06-kombinationsgeraete-montageabteilung]]). Das deckt sich mit dem heutigen Verhalten der
Liste (Index Z. 124-132) — ~~die Summiert-Ansicht uebernimmt es~~ (ueberholt, Antwort 1).

### Summierung bleibt unberuehrt

> **Ueberholt (Antwort 1, 2026-10-07):** Die Summiert-Ansicht wird gar nicht angefasst; der folgende
> Absatz ist gegenstandslos und bleibt nur als Begruendungsspur.

Die OrderInfos werden **neben** die Aggregatzeilen gelegt (Lookup je HauptFA), nicht in sie
hineingejoint. Zeilenzahl, `SummeSollmenge`, `TotalRowCount`, `TotalGroupCount` und
Spaltenfilter-Ergebnis bleiben identisch.

## Technischer Loesungsentwurf

Sprossenwahl (ponytail): Die Daten liegen bereits in der DB und beim Summiert-Aufruf sogar schon im
Speicher (`BuildSummiertAsync` laedt `allInfos`, `KommissionierListenService.cs` Z. 125). Das
Darstellungsmuster existiert (Beschichtung). Neu ist nur das Durchreichen fuer Summiert und ein
gemeinsames Markup.

### A — Liste: `Pos` ergaenzen

Kein Controller-/Service-Eingriff: `FaHierarchyListBuilder.Build` haengt die OrderInfos schon je
Gruppe an (Z. 151-163). Nur Markup — ueber die Partials aus C.

### B — Summiert: OrderInfos je HauptFA durchreichen

> **Verworfen (Antwort 1, 2026-10-07):** keine AB-Nummer in der summierten Ansicht. Abschnitt bleibt
> als Begruendungsspur.

In `AggregateSummiert` nach dem Gruppen-Paging (Z. 198-203):

```csharp
var pageHauptFas = pageRows.Select(r => r.HauptFA).ToHashSet();
var orderInfosByHauptFa = (allInfos ?? Enumerable.Empty<FaHierarchyOrderInfo>())
    .Where(o => pageHauptFas.Contains(o.HauptFA))
    .GroupBy(o => o.HauptFA)
    .ToDictionary(g => g.Key, g => g.ToList());
```

`FaHierarchyKommissionierSummiertResult` bekommt `OrderInfosByHauptFa` (Default leeres Dictionary);
der Controller reicht es in `Summiert`, `PrintSummiert`, `PdfSummiert` an die beiden ViewModels
weiter. Kein zusaetzlicher DB-Zugriff. `allInfos` ist mehrfach aufzaehlbar: `BuildSummiertAsync`
(Z. 125-126) uebergibt das Ergebnis von `FaHierarchyOrderInfoRepository.GetAllAsync`, eine
materialisierte `List<FaHierarchyOrderInfo>` (`FaHierarchyOrderInfoRepository.cs` Z. 27-33).

### C — Zwei Partials statt vier Kopien

> **Ueberholt (Antworten 1/3, 2026-10-07):** Die Begruendung (vier Render-Stellen) entfaellt mit der
> Summiert-Ansicht. Im Restumfang wird `Pos` direkt in `Index.cshtml` (nach Z. 129, Format wie
> Beschichtung) und in der `head-table` von `Print.cshtml` (Kopf Z. 84, Wert Z. 95) ergaenzt.

| Partial | Model | Ersetzt / neu in |
|---|---|---|
| `_KommissionierAuftragsKopf.cshtml` (Bildschirm) | `IReadOnlyList<FaHierarchyOrderInfo>` | ersetzt Index Z. 115-138; neu in Summiert nach Z. 114 |
| `_KommissionierAuftragsKopfDruck.cshtml` (Druck) | `IReadOnlyList<FaHierarchyOrderInfo>` | ersetzt Print Z. 68-102; neu in PrintSummiert nach Z. 68 |

Begruendung: Vier Render-Stellen muessen dieselbe Kombigeraete-Regel und dasselbe AB/Pos-Format
zeigen. Zwei Kopien je Kanal driften erfahrungsgemaess auseinander (Begruendung wie beim
`_EnaioDmsBadges`-Partial, [[fallstricke]] Abschnitt 1). Die Partials enthalten keinen Controller-
oder Spaltenzustand; der Mehrdeutig-Hinweis ergibt sich aus `Model.Count > 1`.

Bildschirm-Partial (Kern):

```cshtml
@model IReadOnlyList<IdealAkeWms.Models.FaHierarchyOrderInfo>
@if (Model.Count > 1)
{
    <span class="badge bg-info text-dark ms-2" title="Mehrere Montage-Abteilungen (Kombigerät) — alle Aufträge werden angezeigt">mehrdeutig (Kombigerät)</span>
}
@if (Model.Any())
{
    <span class="text-muted small ms-2">
        @foreach (var info in Model)
        {
            <span class="me-3">
                @(!string.IsNullOrWhiteSpace(info.MontageAbteilung) ? info.MontageAbteilung : "—")
                @if (!string.IsNullOrWhiteSpace(info.Kunde)) { <text> · @info.Kunde</text> }
                @if (!string.IsNullOrWhiteSpace(info.ABNr)) { <text> · AB @info.ABNr</text> }
                @if (!string.IsNullOrWhiteSpace(info.Pos)) { <text> / @info.Pos</text> }
                @if (info.KO_Termin.HasValue) { <text> · KO @info.KO_Termin.Value.ToString("dd.MM.yyyy")</text> }
            </span>
        }
    </span>
}
else
{
    <span class="text-muted small ms-2">Keine Auftrags-Kopfdaten zu diesem HauptFA.</span>
}
```

Aufruf in `Index.cshtml`: `<partial name="_KommissionierAuftragsKopf" model="group.OrderInfos" />`.
In `Summiert.cshtml`: `Model.OrderInfosByHauptFa.TryGetValue(group.Key, out var infos) ? infos :
new List<FaHierarchyOrderInfo>()`. Das bestehende Kombi-Badge der Liste (Index Z. 115-120) wandert
unveraendert ins Partial — sein Text bleibt gleich; der Tooltip-Text „Positionen werden NICHT
gesplittet" darf bleiben, er gilt fuer beide Ansichten.

Druck-Partial: Kombi-Hinweis (`.note`, Text aus Print Z. 70-75) + `head-table` mit Spalten
Montage-Abt. / Kunde / AB-Nr. / **Pos** / KO-Termin / FE-Termin. `PrintSummiert.cshtml` bekommt
die dafuer noetigen CSS-Regeln `.head-table …` und `.note` (heute nur in `Print.cshtml` Z. 24-26/31).

**Frontend-Pflicht:** Views werden geaendert ⇒ Skill `frontend-design` im Dev-Lauf aufrufen.
Kontrast: `text-muted small` ist der bestehende Stil dieser Kopfzeile (Index Z. 123) — unveraendert
uebernommen, keine neue Farbgebung. Information wird nicht ueber Farbe allein transportiert (Badge
hat Text).

### D — Was sich NICHT aendert

- `KommissionierSpaltenKonsistenzTests` (Vier-Stellen-Drift-Guard) bleibt gruen ohne Anpassung: er
  vergleicht nur `<th data-col-key>`, `#column-config`, `ColumnDefinitions` und `ShowCol("…")`
  (Test Z. 15-18/56-71). Die `head-table` hat keine `data-col-key` und kein `ShowCol` — der
  Gruppenkopf ist bewusst nicht spaltenkonfigurierbar (wie heute die AB-Nr. im Liste-Druck).
- `print-visible-columns.js`, `ResolveVisibleColumnsAsync`, Paging, Umschalter-Query.

## Migrations-/SQL-Auswirkungen

**Keine.** `ABNr` und `Pos` existieren als Spalten der Cache-Tabelle `FaHierarchyOrderInfos` seit
Migration `20260807105825_AddFaHierarchy` (Z. 64 ff.) und werden vom Sync bereits befuellt. Kein
Model-Change ⇒ keine EF-Migration, kein `SQL/XX_*.sql`, kein `OBJECT_ID`-Guard, keine Aenderung an
`SQL/00_FreshInstall.sql`, keine `SQL/AgentJobs/*`. Nicht daten-destruktiv.

## Audit-Feld-Auswirkungen

**Keine.** Reine Lese-Anzeige. `FaHierarchyOrderInfo` ist bewusst **keine** `AuditableEntity`
(reine Cache-Tabelle, `Models/FaHierarchyOrderInfo.cs` Z. 10); es wird nichts geschrieben.

## Rollen / Zugriffsfilter

Unveraendert: Class-Level `[RequireLagerProcessingAccess]` (admin, stock, stock_keyuser) +
Feature-Gate `[RequireFaHierarchyKommissionierlistenAktiv]` (`FaHierarchyKommissionierListenController.cs`
Z. 30-31; `controller.md` Z. 63/77/105). Keine neue Action, keine neue Rolle, kein neuer Filter ⇒
`RoleOverview.cshtml` unberuehrt.

## Listen-View-Pattern

Keine neue Tabellen-View. Pagination, Filterkarte, Server-Spaltenfilter und Spaltenauswahl beider
Views bleiben unveraendert (Gruppenkopf-Variante). ~~Nur bei Rueckfrage 1 = Spalte greifen die vollen
Pflichten aus [[0005-listen-view-pattern-mit-server-side-spaltenfilter]] inkl. `GetByViewKey`-
Registrierung und Drift-Guard (Rueckfrage 4).~~ Entfaellt — keine Spalte (Antworten 1/3, 2026-10-07).

## Akzeptanzkriterien

Stand 2026-10-07 (Antworten 1/3): Es gelten nur AK 1, 2, 3, 6 (auf die Liste beschraenkt), 9 (auf die
Liste beschraenkt) und 11 — und auch diese nur, falls der Restumfang umgesetzt statt zurueckgelegt
wird. AK 4, 5, 7, 8, 10 sind **verworfen** (Summiert-Ansicht).

1. **Liste Bildschirm:** Im Gruppenkopf jedes HauptFA mit OrderInfo steht je OrderInfo `AB <ABNr> / <Pos>`;
   ist `Pos` leer, steht nur `AB <ABNr>` ohne Schraegstrich.
2. **Liste Druck:** Die `head-table` jeder HauptFA-Seite hat die Spalte `Pos` direkt rechts von `AB-Nr.`
   mit dem Wert je OrderInfo-Zeile.
3. **Liste PDF:** Das PDF einer HauptFA-Gruppe zeigt dieselbe `head-table` inkl. `Pos` wie AK 2.
4. ~~**Summiert Bildschirm:** Der Gruppenkopf zeigt nach HauptFA und PDF-Link dieselben Kopfdaten wie die
   Liste (Montage-Abt., Kunde, AB/Pos, KO-Termin); ohne OrderInfo erscheint
   „Keine Auftrags-Kopfdaten zu diesem HauptFA."~~ **Verworfen** (Antwort 1).
5. ~~**Summiert Druck und PDF:** Jede HauptFA-Seite enthaelt unter dem Barcode die `head-table` wie AK 2
   (gleiche Spalten, gleiche Formatierung, Rahmen sichtbar).~~ **Verworfen** (Antwort 1).
6. **Kombigeraet:** Ein HauptFA mit zwei OrderInfo-Zeilen zeigt in ~~allen vier Ausgaben~~ Liste
   Bildschirm, Druck und PDF **beide** AB/Pos (je mit Montage-Abteilung) und den Mehrdeutig-Hinweis;
   keine wird weggelassen. (Vorbehaltlich Rueckfrage 2.)
7. ~~**Summierung unveraendert:** Fuer denselben Filterstand sind Zeilenzahl, `Summe Sollmenge` und
   Seitenzahl der Summiert-Ansicht vor und nach der Aenderung identisch (auch bei Kombigeraeten).~~
   **Verworfen** — Summiert wird nicht angefasst (Antwort 1).
8. ~~**Paging:** Auf Seite 2 der Summiert-Ansicht tragen die dortigen HauptFA ihre eigenen Kopfdaten
   (keine leeren Koepfe durch einen nur fuer Seite 1 gebauten Lookup).~~ **Verworfen** (Antwort 1).
9. **Spaltenauswahl unberuehrt:** Ausblenden beliebiger Spalten aendert die Kopfdaten der Liste im
   Bildschirm, Druck und PDF nicht; `KommissionierSpaltenKonsistenzTests` ist ohne Anpassung gruen.
10. ~~**Unit-Tests:** Die drei neuen `AggregateSummiert`-Tests (Kombi, Paging, leere/null `allInfos`)
    sind gruen; alle bestehenden Tests bleiben gruen.~~ **Verworfen** (Antwort 1); bestehende Tests
    bleiben gruen.
11. **Hilfe/Changelog:** Hilfeseite (Abschnitt Kommissionierlisten) und Anwender-Changelog nennen ~~AB-Nr./Pos~~
    `Pos` im Gruppenkopf der Liste und die Kombigeraete-Darstellung.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (naechste freie Nummer zum Umsetzungszeitpunkt; Stand
Buendel: TS-80 ist das hoechste), Index in [[testszenarien-index]] nachziehen.

**Vorbedingungen:** IDEAL-Testsystem, `FaHierarchyKommissionierlistenAktiv` an, Anwender mit Rolle
`stock`; FaHierarchy-Sync gelaufen. Testdaten per SQL auf der WMS-DB ermitteln:
- HauptFA mit genau einer OrderInfo und gesetzter `Pos`:
  `SELECT TOP 5 HauptFA, MAX(ABNr) AS ABNr, MAX(Pos) AS Pos FROM dbo.FaHierarchyOrderInfos GROUP BY HauptFA HAVING COUNT(*) = 1 AND MAX(Pos) <> '';`
- Kombigeraet: `SELECT HauptFA, COUNT(*) FROM dbo.FaHierarchyOrderInfos GROUP BY HauptFA HAVING COUNT(*) > 1;`
- HauptFA mit leerer `Pos` bzw. ohne OrderInfo (falls vorhanden).

**Schritte / Erwartung (Skizze):**
1. Liste, HauptFA mit einer OrderInfo → Kopf zeigt `AB x / y` (AK 1).
2. Drucken, dann PDF dieser Gruppe → Spalte `Pos` in der Kopftabelle (AK 2/3).
3. ~~Umschalten auf Summiert (oder Menue „Kommissionierlisten") → derselbe Kopf (AK 4); Drucken + PDF (AK 5).~~
   **Verworfen** (Antwort 1). Stattdessen: Summiert oeffnen → unveraendert, keine AB/Pos im Kopf.
4. Kombigeraet in ~~allen vier Ausgaben~~ Liste, Druck und PDF → beide AB/Pos + Hinweis (AK 6).
5. ~~Summiert: Seitengroesse 1, auf Seite 2 blaettern → Kopf der zweiten HauptFA korrekt (AK 8).~~
   **Verworfen** (Antwort 1).
6. In ~~beiden Ansichten~~ der Liste einige nicht gesperrte Spalten (z. B. `Hauptlagerplatz`,
   `Kommissionieren`) ausblenden, dann Drucken und PDF → Kopfdaten unveraendert (AK 9).

**Negativfaelle:** HauptFA ohne OrderInfo → Hinweistext statt leerem Kopf (Liste ~~und Summiert~~);
`Pos` leer → kein haengender Schraegstrich; Toggle aus → Redirect wie bisher (keine Regression);
~~Summenwerte vor/nach identisch (AK 7, Stichprobe zwei HauptFA)~~ (verworfen, Antwort 1).

## Etappen (nur bei epic: true)

Entfaellt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vorlaeufig — der QA-Lauf bestaetigt gegen den echten Diff.*

- **Web-App:** ja (Views, Service-Klasse und ViewModels im Web-Projekt).
- **Service:** nein (Sync liest `ABNr`/`Pos` bereits).
- **Migration:** nein.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

**Pruefschritte fuer den Menschen auf der Sage-DB IDEAL** (Fakten, die nur in Sage liegen — vor der
Abnahme, nicht blockierend fuer die Umsetzung, weil Rohwerte angezeigt werden):

- **P1 — Bedeutung von `Pos`:** `EXEC sp_helptext 'vw_IDEAL-AKE_Kommissionierung_FAInfos';`
  → aus welcher Tabelle/Spalte kommt `Pos` (Auftragsposition der AB oder etwas anderes)? Ist es nicht
  die AB-Position, ist die Beschriftung „Pos" im Kopf fachlich falsch — melden, nicht still umbenennen.
- **P2 — AB/Pos bei Kombigeraeten:**
  `SELECT HauptFA, COUNT(*) AS Zeilen, COUNT(DISTINCT CONCAT(ABNr, '|', Pos)) AS AbPos FROM [vw_IDEAL-AKE_Kommissionierung_FAInfos] GROUP BY HauptFA HAVING COUNT(*) > 1 ORDER BY HauptFA;`
  → `AbPos < Zeilen` heisst: mehrere Kopfzeilen tragen dieselbe AB/Pos (nur Montage-Abteilung
  unterscheidet sie). Die Anzeige zeigt dann dieselbe AB/Pos mehrfach, je mit Montage-Abteilung — das
  ist korrekt, aber zur Kenntnis zu nehmen.

## Offene Rueckfragen

Stand 2026-10-07: 1, 3 beantwortet; 4 damit gegenstandslos; offen bleibt nur 2.

1. **[Beantwortet 2026-10-07, Antwort 1 + 3: keine AB in Summiert; Liste behaelt den bestehenden
   Gruppenkopf, keine Spalte]** **Wo:** AB-Nr./Pos im **Gruppenkopf je HauptFA** oder als **Spalte in jeder Positionszeile**?
   *Empfehlung der Notiz:* Gruppenkopf — AB/Pos haengen am Auftrag, nicht an der Position.
   *Befund dazu:* Die Liste zeigt die AB-Nr. heute schon im Gruppenkopf (Index Z. 129, Print Z. 95);
   die Beschichtung zeigt AB/Pos ebenfalls dort. Eine Spalte waere bei Kombigeraeten je Zeile
   mehrdeutig und braeuchte die volle Spaltenregistrierung (Rueckfrage 4). Diese Spec ist auf den
   Gruppenkopf ausgearbeitet.
2. **Kombinationsgeraete:** alle AB/Pos des HauptFA anzeigen statt eine willkuerlich zu waehlen?
   *Empfehlung der Notiz:* alle anzeigen. *Befund:* So verhaelt sich die Liste heute schon fuer die
   AB-Nr. (Index Z. 124-132); die Summiert-Ansicht wuerde es uebernehmen.
3. **[Beantwortet 2026-10-07, Antwort 3: Liste nice to have (AB dort schon vorhanden), Summiert nicht
   (Antwort 1), Suche nicht gefordert]** **Umfang und Suche:** beide Ansichten (Summiert, Liste), Druckansicht und PDF — bestaetigt? Und ist
   **Suche/Filter nach AB-Nummer** gewuenscht?
   *Empfehlung der Notiz:* alle vier Ausgaben; zur Suche gibt die Notiz **keine** Empfehlung.
   *Befund zur Suche:* Es gibt keinen Spaltenfilter auf Kopfdaten — die Server-Spaltenfilter laufen
   ueber Positions- bzw. Aggregatzeilen (`ColumnMap` Z. 54-68, `SummiertColumnMap` Z. 75-84). Eine
   AB-Suche waere ein eigenes Filterkarten-Feld, das die HauptFA-Menge vorfiltert (Vorbild
   `FaHierarchyController.MatchesFreeText` Z. 204-207), in `BuildAsync` **und** `AggregateSummiert`,
   plus `ToggleSharedQueryKeys` und Druck/PDF-Durchschlag. Nicht im aktuellen Umfang dieser Spec.
4. **[Gegenstandslos 2026-10-07: keine Spalte, Antworten 1/3]** **Nur bei Spalten-Loesung:** Spaltenauswahl-Registrierung (`ColumnDefinitions`, `GetByViewKey`,
   [[fallstricke]] Abschnitt 3) plus `#column-config`, `<th data-col-key>`, `ShowCol(...)`,
   `ColumnMap`/`SummiertColumnMap` und Drift-Guard `KommissionierSpaltenKonsistenzTests` mitziehen?
   *Empfehlung der Notiz:* ja, falls Spalte. *Befund:* Bei Gruppenkopf entfaellt das vollstaendig
   (der Drift-Guard sieht die `head-table` nicht, Test Z. 56-71).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Wo (Gruppenkopf oder Spalte) → **AB-Nummer in der summierten Ansicht nicht (Keyuser 2026-10-07).**
2. Kombinationsgeraete: alle AB/Pos anzeigen →
3. Umfang (Liste, Summiert, Druck, PDF) und Suche/Filter nach AB-Nummer → **AB-Nummer in der Liste nice to have – ist dort über die PPS-View normal schon vorhanden (Keyuser 2026-10-07). Suche nicht gefordert.**
4. Nur bei Spalte: Spaltenregistrierung + Drift-Guard →
