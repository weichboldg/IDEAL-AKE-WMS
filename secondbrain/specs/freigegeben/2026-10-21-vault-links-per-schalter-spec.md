---
type: spec
title: Vault- und enaio-Links konfigurierbar
slug: 2026-10-21-vault-links-per-schalter-spec
status: Testbereit
created: 2026-10-21
updated: 2026-10-22
source_backlog: "[[2026-10-21-vault-links-per-schalter]]"
task: "[[2026-10-21-vault-links-per-schalter-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - IdealAkeWms/Models/AppSettingKeys.cs
  - IdealAkeWms/Program.cs
  - IdealAkeWms/Models/Standort/StandortSettingsCatalog.cs
  - IdealAkeWms/Helpers/LinkVorlage.cs (neu, statische Build-Funktion fuer Vault und enaio)
  - IdealAkeWms/Views/Shared/_VaultLink.cshtml (neu)
  - IdealAkeWms/Models/ViewModels/VaultLinkViewModel.cs (neu)
  - IdealAkeWms/Views/Shared/_EnaioDmsBadges.cshtml
  - IdealAkeWms/Models/ViewModels/FaDocumentLinksViewModel.cs (nur Kommentar)
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs
  - IDEALAKEWMSService/Services/EnaioDmsSyncService.cs
  - IDEALAKEWMSService/Services/FaHierarchySql.cs (ValidateAndQuote mit optionalem maxSegments)
  - IDEALAKEWMSService.Tests/Services/FaHierarchySqlTests.cs
  - IDEALAKEWMSService.Tests/Services/EnaioDmsSyncServiceTests.cs
  - IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs
  - IdealAkeWms/Controllers/ArticlesController.cs
  - IdealAkeWms/Models/ViewModels/ArticleInfoViewModel.cs
  - IdealAkeWms/Views/Articles/Info.cshtml
  - IdealAkeWms/Views/Articles/Index.cshtml
  - IdealAkeWms/Views/StockOverview/Index.cshtml
  - IdealAkeWms/Views/StockMovements/Index.cshtml
  - IdealAkeWms/Views/Picking/Bom.cshtml
  - IdealAkeWms/Views/Shared/_FaDocumentLinks.cshtml
  - IdealAkeWms/Views/ProductionOrders/_ProductionOrderRow.cshtml
  - IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml
  - IdealAkeWms/Views/FaCompletion/_FaCompletionRow.cshtml
  - IdealAkeWms/Views/FaWorklist/_FaWorklistRow.cshtml
  - IdealAkeWms/Views/Help/Index.cshtml
  - README.md (AppSettings-Tabelle)
  - docs/TESTSZENARIEN.md
  - IdealAkeWms.Tests (neue Unit-Tests LinkVorlage.Build + Lesart, Katalog-Eintraege)
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-10-22"
---

# Vault- und enaio-Links konfigurierbar

## Ziel / Nutzen (das Warum)

IDEAL hat keinen Autodesk Vault. Trotzdem zeigt das WMS an zehn Stellen ein Zeichnungs-Symbol bzw. den Knopf „Im VAULT öffnen“,
das fest auf `http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search=<Nr>&searchContext=0` zeigt. An IDEAL führt jeder Klick ins
Leere. Dasselbe gilt für die enaio-Dokument-Symbole: Ihre Adresse `http://akeosapp.ake.at/oscontentviewer/viewer/<ObjektId>/?pagecount=1`
steht fest in `_EnaioDmsBadges.cshtml:21`, und die Quell-View des enaio-Syncs steht fest in `EnaioDmsSyncService.cs:52`.

Ziel (Freigabe-Antworten 1, 2, 7):
- je ein Standort-Schalter Ein/Aus für Vault-Links und enaio-Links (ADR [[0011-feature-toggles-ueber-appsettings]]),
- je eine Link-Vorlage mit Platzhalter als Einstellung statt fest im Code,
- der View-Name des enaio-Syncs als ServiceSetting (ADR [[0008-servicesettings-db-first-mit-typisiertem-katalog]]).

**AKE bleibt nach dem Deploy unverändert, ohne dass jemand etwas einstellt** (Antwort 3). Einen „Vault-Abgleich“ gibt es nicht und
baut diese Spec nicht (Antwort 1).

Umsetzung im Bündel-Worktree `feature/2026-08-07-ideal-teile-1-5` (Stand v1.66.1), **nach** der Aktivitätsprotokoll-Arbeit
([[2026-10-16-aktivitaetsprotokoll-filter-und-namen]]; dafür gibt es noch keine Spec). **Version und TS-Nummer im Dev-Lauf
vergeben:** v1.67.0, TS-100.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Vier AppSettings: Schalter `VaultLinksAktiv` und `EnaioLinksAktiv` (Default an, Lesart wie `LagerbestellungAktiv`), Vorlagen
  `VaultLinkVorlage` (Platzhalter `{Artikelnummer}`) und `EnaioLinkVorlage` (Platzhalter `{ObjektId}`); leere Vorlage = keine Links.
- Ein ServiceSetting `Sync:EnaioDmsViewName` (String, Default = heutiger Name `[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]`).
- Eine gemeinsame, reine Build-Funktion `LinkVorlage.Build` für beide Link-Arten; ein neues Partial `_VaultLink` für die zehn
  Vault-Fundstellen; das bestehende Partial `_EnaioDmsBadges` liest Schalter und Vorlage selbst.
- Pflege in den Standorteinstellungen, zwei neue Gruppen „Vault (Zeichnungen)“ und „enaio (Dokumente)“ (Antwort 2, Gruppen-Festlegung unten).
- Laufzeit-Seed der vier AppSettings in `Program.cs` mit den heutigen AKE-Werten (Antwort 3); der ServiceSetting-Key kommt über den
  bestehenden Katalog-Seed (`Program.cs:456-469`).
- Hilfe (`Views/Help/Index.cshtml`), README (AppSettings- und ServiceSettings-Tabelle), Testszenarien.

**Out-of-Scope**
- Ein „Vault-Abgleich“ (Sync). Nicht gemeint (Antwort 1); den gibt es im Code auch nicht (Negativbeleg unten).
- Der Schalter `Sync:EnaioDmsEnabled` (`ServiceSettingDefinitions.cs:24`, gelesen in `SyncWorker.cs:138`) bleibt **unverändert**: gleicher
  Key, gleicher Default `false`, gleicher Pflegeort `/ServiceSettings`, nicht in die Standort-Maske.
- Die Spaltenliste und die MERGE-Logik des enaio-Syncs (`EnaioDmsSyncService.cs:51`, `:124-145`) bleiben unverändert.
- Ein Ausblenden der enaio-Daten im Controller: Die Controller laden die Links weiter (z. B. `ProductionOrdersController.cs:448`); nur
  das Partial gibt bei Schalter aus nichts aus (Kosten unten).
- Die CSS-Klassen `.vault-link` (`site.css:405-416`) und `.dms-link` (`site.css:418-429`) bleiben unverändert, siehe Fallstrick.
- Anwender-Changelog früherer Versionen (`Views/Help/Changelog.cshtml:1529`) bleibt unverändert; das ist Historie.
- Zusatzfunde, nicht mitbauen: veralteter Kommentar `ColumnDefinitions.cs:59` („Icon/action column (enaio links)“; die Spalte
  `row-actions` enthält OSEON- und Erledigt-Knopf, `_ProductionOrderRow.cshtml:125-149`); unbenutzte Abhängigkeit
  `IEnaioDmsDocumentRepository` in `PickingController.cs:26,65`; TS-4.5/4.5a nennen `EnaioDmsEnabled` als „AppSetting“
  (`TESTSZENARIEN.md:1161,1177`), gemeint ist das ServiceSetting `Sync:EnaioDmsEnabled`.

## Fachliche Anforderungen

1. **Schalter `VaultLinksAktiv` und `EnaioLinksAktiv`.** Fehlende Zeile oder jeder Wert außer `false` bedeutet **an**. Nur `false`
   (Groß-/Kleinschreibung egal) schaltet aus. Das ist die Default-true-Lesart aus `fallstricke.md` („Feature-Toggle mit Default true“),
   **nicht** das `?.Equals("true") == true`-Muster.
2. **Vorlagen.** `VaultLinkVorlage` mit Platzhalter `{Artikelnummer}`, `EnaioLinkVorlage` mit Platzhalter `{ObjektId}` (Antwort 4). Leer
   oder fehlend: keine Links, auch wenn der Schalter an ist. Gefüllt: Der Platzhalter wird durch den URL-kodierten Wert
   (`Uri.EscapeDataString`) ersetzt (Antwort 6). Fehlt der Platzhalter in der Vorlage, wird der kodierte Wert ans Ende gehängt (so
   funktioniert auch eine Basisadresse, die auf `search=` endet). Der Platzhalter wird exakt (Ordinal, Groß-/Kleinschreibung wie angegeben) gesucht.
3. **Ein Link erscheint nur, wenn** Schalter an **und** Vorlage gefüllt **und** Wert nicht leer ist (Antwort 6: leeres Feld = kein Link).
   Sonst rendert die Stelle **kein** `<a>`. Die Artikelnummer bzw. FA-Nummer selbst bleibt immer stehen.
4. **enaio: Schalter aus = keine enaio-Badges.** Jedes enaio-Badge ist heute ein Link (`_EnaioDmsBadges.cshtml:38,42,46`, jeweils
   `<a href="@dmsUrl">`); ein Badge „nur Anzeige“ gibt es nicht. Bei Schalter aus oder leerer Vorlage gibt das Partial deshalb gar
   nichts aus. **Begründung:** Ein Symbol ohne Ziel wäre an IDEAL genau das „Klick ins Leere“, das diese Spec beseitigt, und ein neues,
   nicht anklickbares Badge wäre eine neue Optik ohne Auftrag.
5. **AKE ohne Zutun:** Nach dem Deploy erzeugt der Seed die vier AppSettings-Zeilen mit den heutigen AKE-Werten, der Katalog-Seed die
   Zeile `Sync:EnaioDmsViewName` mit dem heutigen View-Namen. An allen zehn Vault-Stellen und allen fünf enaio-Aufrufstellen sind die
   Links da und führen bei normalen Werten auf dieselbe URL wie heute; der enaio-Sync führt zeichengleich dasselbe SQL aus.
6. **IDEAL:** Der Admin schaltet beim Deploy `VaultLinksAktiv` aus. Ob `EnaioLinksAktiv` an IDEAL an bleibt, entscheidet Gerald beim
   Deploy über den Schalter (Antwort 5).
7. **enaio-View-Name.** `Sync:EnaioDmsViewName` bestimmt die Quelle des enaio-Syncs. Leer oder fehlend: der Default
   `[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]`. Der Name wird vor jedem Lauf geprüft und geklammert (Whitelist + QUOTENAME wie
   bei den Sage-Views, erlaubt bis zu drei Teile `[Datenbank].[Schema].[Name]`). Ungültiger Name: kein SQL, Lauf „fehlgeschlagen“ im
   Aktivitäts-Protokoll und Fehlermail. Die konfigurierte View muss die Spalten `id, angelegt, Typ, WaNummer, WaNummerTrimmed` liefern.
   Änderungen wirken ab dem nächsten Sync-Lauf, ohne Dienst-Neustart.
8. **Keine Layout-Lücke** bei Schalter aus: keine leere Spalte, keine doppelte Lücke in Knopfgruppen, kein verwaister Abstand.
9. **Der Stücklisten-Knopf bleibt.** `_FaDocumentLinks.cshtml:11-12` trägt die Klasse `vault-link`, ist aber der Knopf „Stückliste öffnen“.
   Er bleibt bei jedem Schalterstand sichtbar. **Kein Ausblenden über CSS-Klassen** (`.vault-link`, `.dms-link`).

## Technischer Lösungsentwurf

### Nachgemessene Fundstellen (Bündel-Worktree v1.66.1)

Gesucht mit `(?i)akevault|AKE-VAULT|AutodeskTC` über den ganzen Worktree. Treffer im Anwendungscode: genau diese zehn Dateien
(sonst nur historische Pläne unter `docs/superpowers/plans/` und `secondbrain_alt/`):

| # | Fundstelle | Nummer | Umgebung / Layout | Leer-Guard heute |
|---|---|---|---|---|
| 1 | `Controllers/ArticlesController.cs:317` → `Views/Articles/Info.cshtml:55-61` | `article.ArticleNumber`, `Uri.EscapeDataString` | Knopf mit Link-Symbol + Text „Im VAULT öffnen“ im `card-header d-flex justify-content-between` (`Info.cshtml:53`) | entfällt (Artikel gefunden) |
| 2 | `Views/Articles/Index.cshtml:84-89` | `article.ArticleNumber` | Symbol nach `<strong>` in der Artikelnummer-Zelle | nein |
| 3 | `Views/StockOverview/Index.cshtml:113-118` | `item.ArticleNumber` | wie 2 | nein |
| 4 | `Views/StockMovements/Index.cshtml:100-105` | `item.ArticleNumber` | wie 2, danach Bezeichnung (`:106`) | nein |
| 5 | `Views/Picking/Bom.cshtml:239-244` | `item.Ressourcenummer` | wie 2 | **nein** (Link auch bei leerer Ressourcenummer) |
| 6 | `Views/Shared/_FaDocumentLinks.cshtml:22-31` | `Model.ArticleNumber` | 3. Element in `span.d-inline-flex gap-1` (`:10`); genutzt nur von `Views/FaCompletion/Edit.cshtml:9` | ja (`:22`) |
| 7 | `Views/ProductionOrders/_ProductionOrderRow.cshtml:81-83` | `item.ArticleNumber` | wie 2 | ja (`:78`) |
| 8 | `Views/PickingLeitstand/_PickingLeitstandRow.cshtml:62-67` | `item.ArticleNumber` | wie 2 | ja (`:59`) |
| 9 | `Views/FaCompletion/_FaCompletionRow.cshtml:28-36` | `item.ArticleNumber` | letztes Element in `span.d-inline-flex gap-2` (`:13`) | ja (`:28`) |
| 10 | `Views/FaWorklist/_FaWorklistRow.cshtml:34-42` | `item.ArticleNumber` | letztes Element in `span.d-inline-flex gap-2` (`:19`) | ja (`:34`) |

Die Liste im Backlog stimmt in Datei und Zeile. Die Views 2-10 setzen die Nummer mit `@...` in das `href`. Razor kodiert das als HTML, nicht als
URL. Nur Fundstelle 1 benutzt `Uri.EscapeDataString`. Antwort 6 legt fest: künftig überall URL-kodiert.

**Layout-Prüfung:** In keiner der zehn Stellen hat der Vault-Link eine eigene Spalte oder ein eigenes `<th>`; er steckt immer in einer Zelle mit
Text. In den Flex-Gruppen (6, 9, 10) wirkt `gap` nur zwischen vorhandenen Elementen, ein fehlendes Element hinterlässt also keine Lücke. In
2-5, 7, 8 trägt nur der Link selbst den Abstand (`.vault-link { margin-left: 4px }`, `site.css:406-411`); fehlt er, bleibt nur Leerraum-Text
nach `<strong>`. In Fundstelle 1 steht dann `<strong>` allein im Flex-Kopf. Leere Zellen oder Spalten entstehen nicht.

### Nachgemessene enaio-Fundstellen (Bündel-Worktree v1.66.1)

Gesucht mit `(?i)enaio|EnaioDms|dms-link|akeosapp|oscontentviewer` über `IdealAkeWms/` (ohne `Migrations/`, ohne `Changelog.cshtml`),
`IDEALAKEWMSService/`, `wwwroot/js/`, `README.md`, `appsettings*.json`, `SQL/AgentJobs/`.

**Link-Adresse: genau eine Stelle.** `Views/Shared/_EnaioDmsBadges.cshtml:21`:
`var dmsUrl = $"http://akeosapp.ake.at/oscontentviewer/viewer/{dms.EnaioDmsObjectId}/?pagecount=1";` (nachgemessen, wie in Antwort 7
angegeben). `akeosapp`/`oscontentviewer` kommt sonst nirgends im Anwendungscode, in JS, README oder appsettings vor.

**Aufrufer des Partials (fünf Stellen, alle rendern nur, wenn Links da sind):**

| # | Aufrufstelle | Umgebung / Layout | Bei Schalter aus |
|---|---|---|---|
| E1 | `Views/ProductionOrders/_ProductionOrderRow.cshtml:61-64` | FA-Nummer-Zelle, nach `<strong>` und Badges | nur die Symbole fehlen; jedes trägt seinen Abstand selbst (`.dms-link { margin-left: 4px }`, `site.css:419-425`) |
| E2 | `Views/PickingLeitstand/_PickingLeitstandRow.cshtml:51-54` | FA-Nummer-Zelle, nach `<strong>` (`:46`) | wie E1 |
| E3 | `Views/FaCompletion/_FaCompletionRow.cshtml:24-27` | in `span.d-inline-flex gap-2` (`:13`) | `gap` wirkt nur zwischen vorhandenen Elementen, keine Lücke |
| E4 | `Views/FaWorklist/_FaWorklistRow.cshtml:30-33` | in `span.d-inline-flex gap-2` (`:19`) | wie E3 |
| E5 | `Views/Shared/_FaDocumentLinks.cshtml:17-21` (`showLabel: Model.ShowEnaioLabel`), einziger Aufrufer `Views/FaCompletion/Edit.cshtml:9-17` mit `ShowEnaioLabel = true` | 2. Element in `span.d-inline-flex gap-1` (`:10`) | wie E3; der Stücklisten-Knopf (`:11-16`) bleibt |

Die Daten liefern die Controller per `GetByOrderNumbersAsync` (`ProductionOrdersController.cs:379,448`, `PickingLeitstandController.cs:239,310`,
`FaCompletionController.cs:183,216,385`, `FaWorklistController.cs:318,335`). Sie bleiben unverändert (siehe Kosten).

**Keine eigene Spalte.** Die enaio-Symbole stehen immer in der FA-Nummer-Zelle bzw. im Edit-Kopf. Die Spalte `row-actions`
(`ProductionOrders/Index.cshtml:113`, `PickingLeitstand/Index.cshtml:142`) enthält OSEON- und Erledigt-Knopf
(`_ProductionOrderRow.cshtml:125-149`), keine enaio-Links; der Kommentar `ColumnDefinitions.cs:59` ist veraltet (Zusatzfund). Bei
Schalter aus entsteht also keine leere Spalte und keine leere Zelle.

**CSV:** In den Listen (E1-E4) hat das Badge keinen Text (`ShowLabel=false`, `_EnaioDmsBadges.cshtml:38,42,46` gibt dann `null` aus). Die
Zelle hat Text (FA-Nummer in `<strong>`), deshalb liest `cellValue` nie das `title` des Badges (`csv-export.js:93-96`); `href` wird
nie gelesen. Der CSV-Wert ändert sich bei keinem Schalterstand.

**Druck/PDF:** Keine Druck-View enthält enaio (Suche `(?i)dms|enaio` über `IdealAkeWms/Views` trifft keine `Print*`-Datei). Keine Änderung.

**JS:** `wwwroot/js/**` enthält kein `enaio`/`dms-link`. Keine Änderung.

**Hilfe:** `Views/Help/Index.cshtml:353-360` („enaio DMS-Dokumente“, FA-Liste), `:759-763` (FA-Vervollständigung Edit-Kopf), `:937`
(FA-Abarbeitung „… und die enaio-Dokument-Links“), `:1872` (Service-Name `EnaioDms` im Aktivitäts-Protokoll, bleibt).

**README:** `README.md:106` (SQL-Skript 35), `:518` (SyncWorker), `:730` (enaio-Sync-Beschreibung), `:773` (`Sync:EnaioDmsEnabled`
in der Sync-Gates-Zeile). Ergänzt wird nur die Einstellungs-Doku (siehe Akzeptanzkriterium 18).

**enaio-Sync, View-Name heute:** `IDEALAKEWMSService/Services/EnaioDmsSyncService.cs:50-52` (fest
`FROM [enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]`, dreiteilig, nachgemessen). Gate `Sync:EnaioDmsEnabled` in `SyncWorker.cs:138`
(`GetBoolSafeAsync`, Default `false`), Fehler gehen über `RunResilientAsync` (`SyncWorker.cs:512-520`) in die Fehlermail. Die
Verbindung kommt aus `ConnectionStrings:EnaioDmsConnection` (`EnaioDmsSyncService.cs:32-33`; im Repo `IDEALAKEWMSService/appsettings.json:6`
mit `Database=enaio`; der Wert auf den Servern ist hier nicht einsehbar).

### Gemeinsamer Helfer (ponytail-Leiter)

Im Code gibt es keine eigenen TagHelper (`_ViewImports.cshtml:4` lädt nur die MVC-TagHelper, unter `IdealAkeWms/TagHelpers` liegt nichts).
Das Muster für wiederverwendete Link-Fragmente ist ein **Shared Partial mit kleinem ViewModel**: `_EnaioDmsBadges.cshtml` +
`EnaioDmsBadgesViewModel(links, showLabel)` (`EnaioDmsBadgesViewModel.cs:11`), Aufrufer z. B. `_ProductionOrderRow.cshtml:63`. Ein Partial,
das selbst AppSettings liest, gibt es ebenfalls schon: `_BdeMasterDataTabs.cshtml:3,8` (`@inject IAppSettingRepository`). Daraus:

1. **`LinkVorlage`** (neu, `IdealAkeWms/Helpers/LinkVorlage.cs`, neben `OseonStatusHelper.cs`): eine **statische, reine** Funktion
   `static string? Build(string? aktivRaw, string? vorlage, string platzhalter, string? wert)`:
   - `string.Equals(aktivRaw?.Trim(), "false", OrdinalIgnoreCase)` → `null`
   - `string.IsNullOrWhiteSpace(vorlage)` oder `string.IsNullOrWhiteSpace(wert)` → `null`
   - sonst `kodiert = Uri.EscapeDataString(wert.Trim())`; enthält `vorlage.Trim()` den Platzhalter (Ordinal), wird er durch `kodiert`
     ersetzt, sonst wird `kodiert` angehängt.
   Die Funktion ist die **einzige** Stelle, die die Regel kennt, gilt für Vault und enaio gleich und ist ohne DB testbar. Die
   Platzhalter stehen als Konstanten daneben: `LinkVorlage.PlatzhalterArtikelnummer = "{Artikelnummer}"`,
   `LinkVorlage.PlatzhalterObjektId = "{ObjektId}"`.
2. **`VaultLinkViewModel`** (neu, `Models/ViewModels/`): `ArticleNumber` (string?) und `ShowLabel` (bool, wie bei enaio). Keine Logik.
3. **`Views/Shared/_VaultLink.cshtml`** (neu): `@model VaultLinkViewModel`, `@inject IAppSettingRepository`, liest `VaultLinksAktiv` und
   `VaultLinkVorlage` über `AppSettingKeys`, ruft `LinkVorlage.Build(..., PlatzhalterArtikelnummer, Model.ArticleNumber)`. Ist das
   Ergebnis `null`, gibt das Partial **nichts** aus. Sonst kommt das heutige Markup: Klasse
   `vault-link`, `target="_blank"`, `title="Zeichnung in Vault öffnen"`, Dokument-Symbol. Bei `ShowLabel=true` kommt die Info-Variante mit
   Link-Symbol, `class="me-1"` und Text „Im VAULT öffnen“ (heute `Info.cshtml:55-61`).
   Zusätzlich `rel="noopener"` (heute fehlt es, `target="_blank"` auf fremden Host). Das ist eine Ein-Wort-Härtung und ändert das Verhalten nicht.
4. **Aufrufer:** Die Fundstellen 2-10 ersetzen das `<a>…</a>` (und, wo vorhanden, die eigene `IsNullOrEmpty`-Klammer um den Link) durch
   `<partial name="_VaultLink" model="new VaultLinkViewModel(item.ArticleNumber)" />`. Bei 5 steht dort `item.Ressourcenummer`.
   Fundstelle 1: `ArticleInfoViewModel.VaultUrl` (`ArticleInfoViewModel.cs:11`) und die Zuweisung `ArticlesController.cs:317` **entfallen**,
   `Info.cshtml` ruft das Partial mit `ShowLabel=true`. Dadurch braucht `ArticlesController` keine neue Abhängigkeit (der Konstruktor hat heute
   kein `IAppSettingRepository`, `ArticlesController.cs:13-21`), und `ArticlesControllerTests.cs:45` bleibt unverändert.
   **Achtung `_ProductionOrderRow`/`_PickingLeitstandRow`:** Dort umschließt `@if (!string.IsNullOrEmpty(item.ArticleNumber))` auch das
   `<strong>`. Diese Klammer bleibt; nur das `<a>` wird ersetzt.
5. **`FaDocumentLinksViewModel`** bleibt im Code unverändert. `ArticleNumber` (`FaDocumentLinksViewModel.cs:12-13`) wird nur noch an das
   Partial gereicht; die Kommentare „NULL/leer = kein Vault-Link“ bzw. „NULL/leer = keine enaio-Badges“ (`:24`) werden um „oder Schalter
   aus / Vorlage leer“ ergänzt.
6. **`_EnaioDmsBadges.cshtml`** (bestehend, einzige enaio-URL-Stelle): oben `@inject IAppSettingRepository`, einmal je Partial-Aufruf
   `EnaioLinksAktiv` und `EnaioLinkVorlage` lesen. Zeile 21 wird zu
   `var dmsUrl = LinkVorlage.Build(aktiv, vorlage, LinkVorlage.PlatzhalterObjektId, dms.EnaioDmsObjectId.ToString(CultureInfo.InvariantCulture));`
   und `if (dmsUrl == null) continue;`. Weil Schalter und Vorlage für alle Links gleich sind, kann das Partial auch vor der Schleife
   abbrechen (Schalter aus oder Vorlage leer → keine Ausgabe); beides ist zulässig, das Ergebnis ist dasselbe. Icons, Titel, Klassen,
   Reihenfolge bleiben wie heute; ergänzt wird nur `rel="noopener"`. Die fünf Aufrufer (E1-E5) bleiben **unverändert**: Ihre
   `TryGetValue`-/`Count > 0`-Klammern umschließen nur den Partial-Aufruf, ein leeres Partial hinterlässt nichts.
   `EnaioDmsObjectId` ist `long` (`EnaioDmsDocumentRepository.cs:14`), also nie leer; die Kodierung ist bei Ziffern ein No-op und läuft
   trotzdem durch dieselbe Funktion (Antwort 6).

**Kosten:** je Zeile zwei `IMemoryCache`-Treffer je Partial (`CachedSettingRepository.cs:21-30`, 2 Minuten) und ein Partial-Aufruf. Das
enaio-Partial wird heute schon je Zeile aufgerufen, dort kommen nur die zwei Cache-Treffer dazu. Auch der CSV-Export mit `pageSize=0`
(`csv-export.js:3`) bleibt damit unkritisch. Auf eine Vorab-Auflösung im Controller wird bewusst verzichtet: Sie bräuchte acht
Controller-Änderungen statt zwei Partials. Ebenso laden die Controller die enaio-Links bei Schalter aus weiter (eine Bulk-Abfrage je
Seite, z. B. `ProductionOrdersController.cs:448`); das Einsparen wäre eine Optimierung ohne Auftrag.

### Fallstrick Stücklisten-Knopf (Pflicht)

`_FaDocumentLinks.cshtml:11-12` rendert den **Stücklisten**-Knopf mit `class="vault-link" title="Stückliste öffnen"`. Ein Ausblenden über
`.vault-link` (CSS `display:none`, JS `querySelectorAll('.vault-link')`) würde ihn mit verstecken. Deshalb gilt:
- Abschalten **nur serverseitig im Partial** (kein `<a>` ausgeben), nie über CSS oder JS. Das gilt ebenso für enaio (`.dms-link`).
- `.vault-link` in `site.css:405-416` und `.dms-link` in `site.css:418-429` bleiben unverändert; es sind Optik-Klassen, keine Kennzeichen.
- Der Stücklisten-Knopf wird **nicht** umbenannt (Konsistenz-Regel; das Umbenennen wäre ein eigener Optik-Eingriff).
- Der Dev-Lauf ergänzt `fallstricke.md` um genau diesen Punkt (mit Warum).

### Pflegeort: Standorteinstellungen (festgelegt, Antwort 2)

- **Warum die Standorteinstellungen:** Der Unterschied ist ein reiner Standort-Unterschied (AKE hat Vault, IDEAL nicht). Genau dafür gibt
  es die kuratierte Maske (`StandortSettingsCatalog.cs:19-25`), und die jüngsten Standort-Schalter liegen ebenfalls dort (Hardware-Scanner,
  `StandortSettingsCatalog.cs:69-79`; Briefkopf-Pfad `:43-44`). Die Maske kann Bool und Text samt Hinweis darstellen (`StandortField.Hint`)
  und mischt AppSettings und ServiceSettings je Feld (`StandortField.Backend`, `StandortEinstellungenController.cs:94-102,128-130`).
- **Die Sage-View-Namen aus v1.60.0/v1.61.0 liegen ebenfalls dort** (`StandortSettingsCatalog.cs:46-53`, Backend `Service`, Hinweis
  `ViewNameHint` `:35-36`), ihre Schalter bleiben in `/ServiceSettings` (Kommentar `:52`, z. B. `Sync:OperationConfigSyncEnabled`). Gleich
  gehalten: `Sync:EnaioDmsViewName` kommt als Service-Feld in die Maske, `Sync:EnaioDmsEnabled` bleibt nur in `/ServiceSettings`.
  `/ServiceSettings` zeigt den neuen Key zusätzlich automatisch (katalog-getrieben, ADR 0008), Kategorie „Sync“ wie `Sync:EnaioDmsEnabled`.
- **Fallstrick dabei (belegt):** Die Maske zeigt ein Bool nur dann als an, wenn der Wert `true` ist (`StandortEinstellungenController.cs:139`,
  `IsTrue` `:155-158`). Bei fehlender Zeile rendert sie deshalb `hidden=false` (`Views/StandortEinstellungen/Index.cshtml:76`). Weil die Maske
  bei jedem Speichern **alle** Felder schickt (Kommentar `:54-55`) und Bool immer als `"true"`/`"false"` schreibt (`Save` `:64-65`), würde
  **das nächste Speichern irgendeines Standortwerts** einen Default-an-Schalter ohne Zeile auf `false` setzen. AKE würde dunkel, ohne dass
  jemand den Schalter angefasst hat. **Gegenmittel:** Der Seed legt `VaultLinksAktiv=true` und `EnaioLinksAktiv=true` an, also existieren die
  Zeilen immer. Eine Änderung an der Maske ist nicht nötig. Dasselbe gilt für die Vorlagen: Text-Felder werden roh übernommen (`Save`
  `:89-91`), ohne Zeile würde das Speichern `""` schreiben und die Links abschalten; der Seed verhindert das. Der Dev-Lauf hält das in
  `fallstricke.md` fest: Ein Default-true-Bool oder eine Default-Vorlage in `StandortSettingsCatalog` braucht eine geseedete Zeile.
- **Service-String in der Maske:** `Sync:EnaioDmsViewName` wird vom Katalog-Seed angelegt (`Program.cs:456-469`), die Maske zeigt also den
  Default. Leert ein Admin das Feld, speichert die Maske `""`; der Dienst nimmt dann den Default (Anforderung 7). Ein Leeren schaltet den
  Sync also nicht ab, dafür ist `Sync:EnaioDmsEnabled` da. Eine Namensprüfung beim Speichern gibt es wie bei den Sage-Views nicht
  (`ViewNameHint`: ungültiger Name fällt erst beim nächsten Lauf als Fehlermail auf).
- **/Settings:** Der Settings-Controller zeigt nur vorhandene Zeilen (`SettingsController.cs:34`, `GetAllAsync`). Nicht gruppierte Keys landen
  unter „Sonstige“ (`Views/Settings/Index.cshtml:48-49,89-120`). Mit dem Seed erscheinen die vier AppSettings dort automatisch. Das ist
  dieselbe DB-Zeile, also kein zweiter Speicherort und kein Konflikt. In `Settings/Index.cshtml` wird nichts geändert.
- Alle drei Masken sind admin-only (`StandortEinstellungenController.cs:20`, `SettingsController.cs:10`, `ServiceSettingsController.cs:16`).

### Gruppen: getrennt „Vault (Zeichnungen)“ und „enaio (Dokumente)“ (festgelegt)

Zwei Gruppen, je System eine. **Begründung:** (1) Die beiden Schalter werden je Standort unabhängig entschieden (Antwort 5: IDEAL schaltet
Vault aus, enaio entscheidet Gerald getrennt); eine gemeinsame Gruppe legt eine gemeinsame Entscheidung nahe. (2) Zur enaio-Gruppe gehört
zusätzlich der View-Name des Syncs; der passt nicht in „Mandant / Sage-Views“, weil enaio kein Sage ist und die Gruppe nach Sage benannt ist.
(3) Der Admin findet alles zu einem System an einer Stelle. Die Maske kennt bisher nur Gruppen mit einem Backend, der Controller arbeitet
aber je Feld (`StandortEinstellungenController.cs:94-102,128-130`); eine gemischte Gruppe braucht keine Code-Änderung am Controller.

Katalog-Einträge (`StandortSettingsCatalog.cs`, zwei neue Konstanten, beide ans Ende von `GroupOrder` `:85-88`):
- `GroupVault = "Vault (Zeichnungen)"`
  - `VaultLinksAktiv`, Label „Vault-Links anzeigen“, Bool, App, Hint: „Zeichnungs-Symbol und ‚Im VAULT öffnen‘ an Artikelnummern. Ohne Vault (IDEAL) ausschalten.“
  - `VaultLinkVorlage`, Label „Vault-Adresse (Vorlage)“, String, App, Hint: „{Artikelnummer} wird durch die kodierte Nummer ersetzt. Leer = keine Links.“
- `GroupEnaio = "enaio (Dokumente)"`
  - `EnaioLinksAktiv`, Label „enaio-Dokument-Links anzeigen“, Bool, App, Hint: „Dokument-Symbole an FA-Nummern (FA-Liste, Leitstand, FA-Vervollständigung, FA-Abarbeitung). Ohne enaio ausschalten.“
  - `EnaioLinkVorlage`, Label „enaio-Adresse (Vorlage)“, String, App, Hint: „{ObjektId} wird durch die enaio-Objekt-ID ersetzt. Leer = keine Links.“
  - `Sync:EnaioDmsViewName`, Label „enaio-View (Sync-Quelle)“, String, **Service**, Hint: `ViewNameHint` plus „Format [Datenbank].[Schema].[Name]; Spalten id, angelegt, Typ, WaNummer, WaNummerTrimmed. Sync-Schalter in den Service-Einstellungen.“

### Adressen für AKE ohne Zutun: Laufzeit-Seed (festgelegt, Antwort 3)

Variante A (Laufzeit-Seed in `Program.cs`) gilt für Vault und enaio. Vorbild `LagerbestellungAktiv` (`Program.cs:347`), Muster „nur anlegen,
wenn Zeile fehlt“ (`Program.cs:349-361`), läuft bei jedem Start nach `Migrate()` (`Program.cs:180-186`). Bestehende Werte bleiben. Keine
Migration, kein SQL. Die Zeilen existieren immer, damit greift der Masken-Fallstrick nicht. Auch Neuinstallationen bekommen die Zeilen beim
ersten Start. Im Code gilt: `null` (Zeile fehlt) = leer = keine Links; ein Fallback-Literal im Code gibt es nicht.

~~Varianten B (Code-Fallback) und C (SQL-Seed)~~ *ueberholt durch Antwort 3* (Begründung der Ablehnung bleibt: B verliert die Links beim
ersten Speichern der Maske, `Save` `:89-91`; C ist doppelte Pflege, `LagerbestellungAktiv` steht in keinem SQL-Skript).

Seed-Block (Vorbild `Program.cs:340-361`, eigener Block „Vault- und enaio-Links“):
- `(AppSettingKeys.VaultLinksAktiv, "true", "Vault-Zeichnungslinks an Artikelnummern anzeigen. Default true. Nur 'false' blendet aus (IDEAL ohne Vault).")`
- `(AppSettingKeys.VaultLinkVorlage, "http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search={Artikelnummer}&searchContext=0", "Vault-Adresse; {Artikelnummer} wird ersetzt. Leer = keine Links.")`
- `(AppSettingKeys.EnaioLinksAktiv, "true", "enaio-Dokument-Links an FA-Nummern anzeigen. Default true. Nur 'false' blendet aus.")`
- `(AppSettingKeys.EnaioLinkVorlage, "http://akeosapp.ake.at/oscontentviewer/viewer/{ObjektId}/?pagecount=1", "enaio-Viewer-Adresse; {ObjektId} wird ersetzt. Leer = keine Links.")`

`AppSetting.Value` ist `StringLength(500)` (`AppSetting.cs:13`); die Vorlagen haben rund 100 bzw. 70 Zeichen.

Der Seed setzt an beiden Standorten `true` (Antwort 5); IDEAL schaltet beim Deploy aus (siehe Deploy).

### Keys

In `AppSettingKeys.cs`, eigener Block mit Kommentar zur Default-true-Lesart und zum Seed (Gegenstück zu `:53-55`):
`VaultLinksAktiv = "VaultLinksAktiv"`, `VaultLinkVorlage = "VaultLinkVorlage"`, `EnaioLinksAktiv = "EnaioLinksAktiv"`,
`EnaioLinkVorlage = "EnaioLinkVorlage"` (Antwort 4).

ServiceSetting in `ServiceSettingDefinitions.All`, direkt unter `Sync:EnaioDmsEnabled` (`:24`):
`new("Sync:EnaioDmsViewName", ServiceSettingType.String, "[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]", "Sync", "Quelle des enaio DMS-Syncs (Whitelist + QUOTENAME, bis zu drei Teile [Datenbank].[Schema].[Name]; Spalten id, angelegt, Typ, WaNummer, WaNummerTrimmed). Leer = Default. Ungueltiger Name: kein SQL, Fehler + Fehlermail. Der Default wirkt nur, wo die Zeile fehlt.")`.

### enaio-Sync: View-Name als ServiceSetting (Antwort 7)

Muster wie der Arbeitsgang-Abgleich (`OperationConfigSyncService.cs:39-40,69,88-100`):
1. **Konstante** `EnaioDmsSyncService.DefaultViewName = "[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]"`, per Test gleich dem
   Katalog-Default (Vorbild `OperationConfigSyncServiceTests.cs:62`).
2. **Lesen** direkt nach `BeginRunAsync` (`EnaioDmsSyncService.cs:25`) und **vor** dem bestehenden `try` (`:30`):
   `await ServiceSettings.GetValueSafeAsync(_configuration, "Sync:EnaioDmsViewName", ct)`; leer/`null` → `DefaultViewName`. Die
   Safe-Variante fällt bei einem DB-Fehler auf `null` zurück (`ServiceSettings.cs:72-77`), also auf den Default.
3. **Prüfen und klammern**, ebenfalls vor dem bestehenden `try`, mit eigenem `try/catch` wie `OperationConfigSyncService.cs:88-100`:
   `FaHierarchySql.ValidateAndQuote(name, maxSegments: 3)`. Bei `FaHierarchyViewNameException`: kein SQL, `run.LogErrorAsync` +
   `run.FinishFailedAsync` mit „Ungültiger View-Name: …“ und `throw new InvalidOperationException(...)` → Fehlermail über
   `RunResilientAsync` (`SyncWorker.cs:512-520`). **Warum vor dem bestehenden `try`:** Dessen allgemeiner `catch`
   (`EnaioDmsSyncService.cs:167-173`) ruft selbst `FinishFailedAsync`; läge die Prüfung darin, würde der Lauf zweimal beendet. Der
   bestehende Test `EnaioDmsSyncServiceTests.cs:12-27` (ohne Konfiguration) bleibt gültig: Ohne DB liefert die Safe-Variante `null`, der
   Default ist gültig, der Lauf scheitert wie heute am Verbindungs-Guard `:32-33`.
4. **SQL:** `$"SELECT id, angelegt, Typ, WaNummer, WaNummerTrimmed FROM {quoted}"` statt des Literals in `:50-52`. Mit dem Default ist
   der Bezeichner zeichengleich `[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]`.
5. **Fehlt die View:** Verhalten wie heute (SqlException im `try` → `catch` `:167-173` → Fehler + Fehlermail). Eine Existenzprüfung
   (`FaHierarchySql.SourceExistsAsync`) wird **nicht** eingebaut: Heute meldet eine fehlende View einen Fehler, das bleibt so.
6. **Kein Dienst-Neustart** für Wertänderungen: `ServiceSettings` liest bei jedem Aufruf aus der DB, ohne Cache (`ServiceSettings.cs:11-12`).

**Namensprüfung und Bindestrich (festgelegt):** `FaHierarchySql.ValidateAndQuote` erlaubt den Bindestrich ausdrücklich
(`FaHierarchySql.cs:26-30`, Zeichensatz `[A-Za-z0-9_-]`, Kommentar nennt `vw_IDEAL-AKE_...`), lehnt nur `--` ab (`:90-94`). Der heutige
Name hat einen einzelnen Bindestrich und besteht. **Er hat aber drei Teile**, und der Validator lässt nur ein oder zwei Teile zu
(`FaHierarchySql.cs:54-57`; Test `FaHierarchySqlTests.cs:42` prüft „zu viele Segmente“ ausdrücklich). Festlegung:
- `ValidateAndQuote(string? viewName, int maxSegments = 2)`: neuer optionaler Parameter, Prüfung `segments.Length < 1 || > maxSegments`.
  Alle bestehenden Aufrufer (`FaHierarchySyncService.cs:61-62`, `OperationConfigSyncService.cs:91`, `ProductionWorkplaceSyncService.cs:61`,
  `SageImportService.cs:385`) bleiben ohne Argument bei 2; `FaHierarchySqlTests.cs:42` bleibt grün. Nur der enaio-Sync übergibt 3.
- **Warum nicht auf zwei Teile kürzen** (`[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]`): Das wäre nur gleichwertig, wenn die
  `EnaioDmsConnection` auf den Servern auf die Datenbank `enaio` zeigt. Im Repo ist das so (`IDEALAKEWMSService/appsettings.json:6`),
  auf den Servern ist es nicht belegt. Mit drei Teilen bleibt das SQL zeichengleich, unabhängig vom Verbindungs-Default.
- **Warum nicht einen zweiten Validator:** Eine Regel an einem Ort (ponytail Sprosse 2); der Homoglyphen- und `--`-Schutz gilt dann auch für enaio.

### CSV-Export und Druck/PDF (geprüft)

- **CSV:** `csv-export.js` liest Zellen über `cellValue` (`csv-export.js:78-97`): erst `data-csv-value`, dann Eingabefelder, dann
  `td.textContent`, und **nur bei leerem Text** `title`/`aria-label`. `href` wird nie gelesen, eine Vault-URL landet also nie in der CSV. Die
  Vault-Links haben keinen Text (nur SVG), deshalb bleibt der Zellwert die Artikelnummer.
  **Randfall heute:** In der Stückliste (Fundstelle 5, kein Leer-Guard) mit leerer Ressourcenummer ist der Text leer. Dann exportiert die CSV
  das `title` des Links, also „Zeichnung in Vault öffnen“. Mit dem Helfer gibt es bei leerer Nummer keinen Link mehr, und der Randfall verschwindet.
- **Druck/PDF:** Durchsucht wurden alle Druck-Views (`Picking/PrintBom.cshtml`, `Picking/PrintPicking.cshtml`,
  `FaHierarchyKommissionierListen/Print*.cshtml`, `FaHierarchyBeschichtung/Print*.cshtml` und `_BeschichtungKopfDruck.cshtml`,
  `Beschichtungsauftraege/Print.cshtml`, `WarehousePicking/Print.cshtml`, `StorageLocations/PrintLabels.cshtml`, `Shared/_PrintBriefkopf*.cshtml`).
  **Keine** davon enthält „vault“: Die Suche `(?i)vault` in `IdealAkeWms/` trifft nur die zehn Fundstellen, `ArticleInfoViewModel.cs`,
  `FaDocumentLinksViewModel.cs`, `site.css`, `Help/Index.cshtml:142-143`, `Help/Changelog.cshtml:1529` und zwei Kommentare
  (`_FaCompletionRow.cshtml:12`, `_FaWorklistRow.cshtml:18`). Der Stücklisten-Druck geht als POST auf `PrintBom` (`Bom.cshtml:1300-1303`),
  rendert also eine eigene View ohne Vault-Link. Am Druck ändert sich nichts.
- **enaio:** CSV, Druck und JS unberührt, Belege im Abschnitt „Nachgemessene enaio-Fundstellen“.

### „Vault-Abgleich“: Negativbeleg (Rückfrage 1, beantwortet: nicht gemeint)

Suche `(?i)vault` (Treffer als Dateiliste):
- `IDEALAKEWMSService/**`, `IDEALAKEWMSService.Tests/**`, `IdealAkeWms.Tests/**`, `SQL/**` (inkl. `SQL/AgentJobs/`), `**/appsettings*.json`,
  `README.md`: **keine Treffer**.
- `IdealAkeWms/Services/**`, `IdealAkeWms/Data/**`, `IdealAkeWms/Migrations/**`, `IdealAkeWms/wwwroot/js/**`: **keine Treffer**.
  `IdealAkeWms/Models/**` trifft nur die zwei ViewModels oben. In `ServiceSettingDefinitions` gibt es also keinen `Sync:*Vault*`-Key.
- `scripts/**`: Treffer nur zum Obsidian-Vault (`setup-vault.ps1:2-3` „Second-Brain-Vault“, `new-worktree.ps1`,
  `sync-onedrive-specs.ps1`), nicht zum Autodesk Vault.

Ergebnis: Es gibt keinen Vault-Dienst, keinen Sync, keine Vault-Tabelle, keinen AgentJob und keine Konfiguration. Es gibt nur die zehn Links.
~~Was ein „Abgleich“ sein soll, kann nur Gerald beantworten.~~ *ueberholt:* Antwort 1 — nur die Links, ein Abgleich ist nicht gemeint.

### Rollen / Zugriff

Keine neue Rolle, kein neuer Filter, kein Feature-Gate am Controller. Die Schalter blenden nur UI-Elemente aus; dahinter liegt keine
erreichbare WMS-URL (die Links zeigen auf Vault bzw. den enaio-Viewer), also braucht es auch keinen `Require…Aktiv`-Filter (die „drei
Stellen“ aus ADR 0011 reduzieren sich hier auf die UI-Stelle). Wer die Views heute sieht, sieht sie weiter. Gepflegt werden Schalter,
Vorlagen und der enaio-View-Name nur durch Admins (siehe Pflegeort). Keine Änderung an `controller.md` oder `RoleOverview.cshtml`.

### Listen-View-Pattern

Es entsteht keine neue Tabelle und keine neue Spalte. Die bestehenden Listen behalten Pagination, Spaltenfilter und Spaltenauswahl
unverändert (in der Artikelnummer- bzw. FA-Nummer-Zelle verschwinden höchstens Symbole). Keine neue Pflicht aus ADR 0005.

## Migrations-/SQL-Auswirkungen

**Keine EF-Migration, kein `SQL/XX_*.sql`, keine Änderung an `SQL/00_FreshInstall.sql`, kein AgentJob.** Das Schema ändert sich nicht;
`AppSettings` hat schon Key/Value/Description (`AppSetting.cs:5-20`), `ServiceSettings` Key/Value/Category/Description
(`Program.cs:460-466`). Die vier AppSettings-Zeilen kommen aus dem Laufzeit-Seed in `Program.cs` (Antwort 3), die Zeile
`Sync:EnaioDmsViewName` aus dem bestehenden Katalog-Seed (`Program.cs:454-469`, legt nur fehlende Keys an). Beide laufen auch bei einer
Neuinstallation beim ersten Start, deshalb braucht FreshInstall keinen Eintrag. ~~Falls Rückfrage 3 auf Variante C entschieden wird, gilt
ADR 0004 …~~ *ueberholt durch Antwort 3* (Variante A). `SQL/AgentJobs/` enthält kein enaio und kein Vault (Suche ohne Treffer), dort
ändert sich nichts. Nicht daten-destruktiv; die Tabelle `EnaioDmsDocuments` und ihr MERGE (`EnaioDmsSyncService.cs:124-145`) bleiben unverändert.

## Audit-Feld-Auswirkungen

Keine. `AppSetting` (`AppSetting.cs:5`) und `ServiceSetting` (`ServiceSetting.cs:5`) erben nicht von `AuditableEntity`. Das gilt für alle
Schreibwege (`AppSettingRepository.SetValueAsync` `:32-47`, `StandortSettingsWriter`, `/ServiceSettings`). Die Audit-Felder der
enaio-Dokumente setzt der MERGE weiter mit `'EnaioDmsSync'` (`EnaioDmsSyncService.cs:134-141`); daran ändert sich nichts. Es wird keine
fachliche Entität geändert.

## Akzeptanzkriterien

**Vault**
1. **AKE-Regression Vault:** Nach Deploy und Neustart, ohne dass jemand etwas einstellt, zeigen alle zehn Fundstellen den Vault-Link. Bei
   einer Artikelnummer aus Ziffern und `-` ist die URL zeichengleich mit der heutigen.
2. **Seed:** Nach dem Start existieren `VaultLinksAktiv=true`, `VaultLinkVorlage=<AKE-Vault-Vorlage>`, `EnaioLinksAktiv=true`,
   `EnaioLinkVorlage=http://akeosapp.ake.at/oscontentviewer/viewer/{ObjektId}/?pagecount=1` und in `ServiceSettings`
   `Sync:EnaioDmsViewName=[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]`. Ist eine der Zeilen schon vorhanden, wird sie nicht
   überschrieben (Wert vor und nach dem Neustart gleich).
3. **Vault-Schalter aus:** Mit `VaultLinksAktiv=false` enthält der HTML-Quelltext an keiner der zehn Stellen ein `<a>` auf die Vault-Adresse.
   Die Artikelinfo zeigt keinen Knopf „Im VAULT öffnen“. Die enaio-Badges sind davon nicht betroffen.
4. **Stücklisten-Knopf bleibt:** Mit `VaultLinksAktiv=false` und `EnaioLinksAktiv=false` zeigt `FaCompletion/Edit` (Partial
   `_FaDocumentLinks`) den Knopf „Stückliste öffnen“ weiterhin, und er öffnet die read-only Stückliste. Die Knöpfe „Stückliste“ in
   FA-Vervollständigung und FA-Abarbeitung bleiben ebenfalls. `site.css` `.vault-link`/`.dms-link` sind unverändert, und kein CSS oder JS
   blendet über diese Klassen aus.
5. **Vault-Vorlage leer:** Mit `VaultLinksAktiv=true` und leerer `VaultLinkVorlage` erscheint an keiner Stelle ein Vault-Link.
6. **Leere Nummer:** Eine Stücklistenzeile mit leerer Ressourcenummer hat keinen Vault-Link (neu, Antwort 6). Ihr CSV-Wert in der
   Ressourcenummer-Spalte ist leer, nicht „Zeichnung in Vault öffnen“.

**enaio**
7. **AKE-Regression enaio:** Nach Deploy, ohne Einstellung, zeigen FA-Liste, Kommissionier-Leitstand, FA-Vervollständigung (Liste und
   Edit-Kopf mit Text-Label) und FA-Abarbeitung die enaio-Badges wie heute: gleiche Icons, gleiche Reihenfolge, gleiche Titel, URL
   zeichengleich `http://akeosapp.ake.at/oscontentviewer/viewer/<ObjektId>/?pagecount=1`.
8. **enaio-Schalter aus:** Mit `EnaioLinksAktiv=false` enthält der HTML-Quelltext an keiner der fünf Aufrufstellen ein enaio-Badge (kein
   `dms-link`, kein `akeosapp`). Die FA-Nummer steht weiter; Vault-Links sind davon nicht betroffen.
9. **enaio-Vorlage leer:** Mit `EnaioLinksAktiv=true` und leerer `EnaioLinkVorlage` erscheint kein enaio-Badge.
10. **enaio-Vorlage wirkt:** Eine Vorlage mit `{ObjektId}` an anderer Stelle (z. B. `https://test.example/view?id={ObjektId}`) erzeugt genau
    diese URL mit der Objekt-ID. Eine Vorlage ohne Platzhalter bekommt die ID angehängt (Unit-Test).

**Gemeinsam**
11. **Lesart Schalter:** `false`, `FALSE`, `False` (auch mit Leerzeichen außen) schalten aus. `true`, eine fehlende Zeile und jeder andere
    Wert (z. B. `nein`) lassen an (Unit-Test auf `LinkVorlage.Build`).
12. **Kodierung:** Ein Wert mit Leerzeichen, `&`, `+` oder `#` erscheint URL-kodiert (`Uri.EscapeDataString`) an der Platzhalter-Stelle
    (Unit-Test); Ziffern und `-` bleiben unverändert.
13. **Keine Layout-Lücke:** Bei jedem Schalterstand (Vault aus, enaio aus, beide aus) gibt es keine leere Spalte und keine leere Zelle; in
    jeder betroffenen Zelle steht die Artikel- bzw. FA-Nummer. In den Knopfgruppen von FA-Vervollständigung, FA-Abarbeitung und
    FA-Edit-Kopf entsteht keine doppelte Lücke. Der Kopf der Artikelinfo zeigt die Artikelnummer ohne verwaisten Abstand. Sichtprüfung an
    Desktop und Handheld.
14. **Vorlage wirkt sofort:** Nach dem Ändern einer Vorlage oder eines Schalters in den Standorteinstellungen zeigt der nächste
    Seitenaufruf den neuen Stand (`StandortSettingsWriter.cs:92` invalidiert den Cache). Über `/Settings` gilt dasselbe
    (`CachedSettingRepository.cs:38-42`).
15. **Masken-Regression:** Ein Speichern der Standorteinstellungen, ohne die Link-Felder anzufassen (z. B. nur Firmenname ändern), lässt
    `VaultLinksAktiv`, `EnaioLinksAktiv` und beide Vorlagen unverändert.
16. **Standorteinstellungen:** Die Gruppen „Vault (Zeichnungen)“ (Schalter, Vorlage) und „enaio (Dokumente)“ (Schalter, Vorlage,
    View-Name) erscheinen am Ende der Maske, je Feld mit Hinweis. Sie sind nur für Admins erreichbar.
17. **CSV/Druck:** Kein CSV-Export der betroffenen Listen enthält eine Vault- oder enaio-URL, weder bei Schalter an noch aus; die
    Spaltenwerte sind unverändert. Druck-Views unverändert.
18. **Doku:** Hilfe — Artikelinfo (`Help/Index.cshtml:142-143`), enaio-Abschnitte (`:353-360`, `:759-763`, `:937`) sagen, dass die Links
    nur bei eingeschaltetem Schalter erscheinen; der Hilfe-Abschnitt Standorteinstellungen (bei `:108`) nennt beide Gruppen mit Keys,
    Default und Pflegeort. README: AppSettings-Tabelle (bei `:411-418`) um die vier Keys, ServiceSettings-Tabelle (bei `:773-779`) um
    `Sync:EnaioDmsViewName`. `Help/Changelog.cshtml` früherer Versionen bleibt unverändert.
19. **Ein Ort:** Eine Suche nach `akevault24` bzw. `akeosapp` im Anwendungscode trifft nur noch den Seed in `Program.cs` (und ggf. Testdaten).

**enaio-Sync**
20. **View-Name Default:** Mit dem geseedeten Default (oder leerem/fehlendem Wert) führt der enaio-Sync zeichengleich
    `SELECT id, angelegt, Typ, WaNummer, WaNummerTrimmed FROM [enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]` aus und meldet wie
    heute „neu“/„aktualisiert“ im Aktivitäts-Protokoll (Service-Filter `EnaioDms`).
21. **Ungültiger View-Name:** Mit z. B. `vw;DROP`, `a.b.c.d`, `[vw--x]` oder einem Nicht-ASCII-Zeichen läuft kein SQL gegen enaio; der Lauf
    steht genau einmal als „fehlgeschlagen“ mit „Ungültiger View-Name: …“ im Aktivitäts-Protokoll, und eine Fehlermail geht raus (bei
    `ErrorNotification:Enabled=true`). Die Tabelle `EnaioDmsDocuments` bleibt unverändert.
22. **Bindestrich und drei Teile:** `ValidateAndQuote("[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]", maxSegments: 3)` liefert den
    Namen unverändert zurück; `ValidateAndQuote` ohne `maxSegments` lehnt drei Teile weiter ab (`FaHierarchySqlTests.cs:42` grün).
23. **Sync-Schalter unverändert:** `Sync:EnaioDmsEnabled` hat denselben Key, Default `false`, Kategorie „Sync“ und wird weiter nur in
    `/ServiceSettings` gepflegt (nicht in der Standort-Maske). Mit `false` läuft kein enaio-Sync, unabhängig vom View-Namen.
24. **Ohne Neustart:** Ein geänderter View-Name wirkt ab dem nächsten Sync-Lauf, ohne Dienst-Neustart.

**Nachweis**
25. Build grün, Web- und Service-Suite grün. Neue bzw. erweiterte Unit-Tests: `LinkVorlage.Build` (aus, leer, Leerzeichen-Wert,
    Platzhalter, ohne Platzhalter, Kodierung, Lesart), Katalog enthält die fünf Standort-Felder in den Gruppen Vault/enaio,
    `ServiceSettingDefinitionsTests` mit `Sync:EnaioDmsViewName` (Drift-Guard), `EnaioDmsSyncService.DefaultViewName` gleich
    Katalog-Default, `FaHierarchySqlTests` für `maxSegments: 3` (gültig) und ungültige Namen.

## Test-Szenarien

Neues Kapitel **TS-100 — Vault- und enaio-Links konfigurierbar** (Nummer beim Dev-Lauf vergeben) in `docs/TESTSZENARIEN.md`, danach
`secondbrain/tests/testszenarien-index.md`. Bestehende Szenarien ergänzen: **TS-2.8** (`TESTSZENARIEN.md:346-365`, Satz `:360` „Link VAULT
öffnet …“ → „bei eingeschalteten Vault-Links“), **TS-4.5** und **TS-4.5a** (`:1158-1190`, Vorbedingung „`EnaioLinksAktiv` an
(Standard)“ ergänzen), **TS-38.13** (`:5068-5090`, Vault- und enaio-Buttons nur bei Schalter an; der BOM-Knopf immer) und
**Szenario 27.5** (`:4082`, enaio-Sync-Lifecycle: Hinweis auf `Sync:EnaioDmsViewName`).

- **TS-100.1 AKE-Regression (Seed, nichts eingestellt):** Vorbedingung: frisches Deploy, keine Einstellung angefasst; eine FA mit
  enaio-Dokument und gepflegter Artikelnummer. Schritte: Artikel-Liste, Bestandsübersicht, Bewegungshistorie, Stückliste, FA-Liste,
  Kommissionier-Leitstand, FA-Vervollständigung (Liste und Edit), FA-Abarbeitung, Artikelinfo öffnen; je einen Vault-Link und ein
  enaio-Badge klicken. Erwartet: alle zehn Vault-Stellen und alle enaio-Stellen wie vor dem Deploy; neuer Tab, Vault-Suche bzw.
  enaio-Viewer mit der richtigen Nummer/ID. Standorteinstellungen zeigen die Gruppen Vault und enaio mit „an“, den AKE-Vorlagen und dem
  View-Namen `[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]`.
- **TS-100.2 Vault aus (IDEAL-Fall):** Standorteinstellungen → Vault → aus → Speichern; dieselben zehn Stellen öffnen. Erwartet: kein
  Vault-Symbol, kein „Im VAULT öffnen“; die Artikelnummer steht überall; keine Lücke, keine leere Spalte; enaio-Badges unverändert da.
- **TS-100.3 enaio aus:** Standorteinstellungen → enaio → aus → Speichern; FA-Liste, Leitstand, FA-Vervollständigung (Liste, Edit),
  FA-Abarbeitung öffnen. Erwartet: keine enaio-Symbole, kein enaio-Badge mit Text im Edit-Kopf; FA-Nummer steht; keine Lücke, keine
  leere Spalte; Vault-Links unverändert da. Der enaio-Sync läuft weiter, wenn `Sync:EnaioDmsEnabled=true` (Aktivitäts-Protokoll).
- **TS-100.4 Stücklisten-Knopf (Fallstrick):** Vault und enaio aus, FA-Vervollständigung → Edit öffnen. Erwartet: Knopf „Stückliste öffnen“
  sichtbar, Klick öffnet die read-only Stückliste. Ebenso der Knopf „Stückliste“ in den Listen von FA-Vervollständigung und FA-Abarbeitung.
- **TS-100.5 Vorlage leer:** Schalter an, Vault-Vorlage leeren → Speichern. Erwartet: keine Vault-Links. Dasselbe mit der enaio-Vorlage:
  keine enaio-Badges. Vorlagen wiederherstellen → Links wieder da.
- **TS-100.6 Vorlage ändern:** Vault-Vorlage auf eine Test-Adresse mit `{Artikelnummer}`, enaio-Vorlage auf eine mit `{ObjektId}` setzen.
  Erwartet: Links zeigen die neuen Adressen mit Nummer bzw. ID an der Platzhalter-Stelle.
- **TS-100.7 URL-Kodierung:** Artikel mit Leerzeichen oder `&` in der Artikelnummer (falls vorhanden; sonst Unit-Test als Nachweis) in der
  Artikel-Liste. Erwartet: im `href` steht `%20` bzw. `%26`, die Vault-Suche findet den Artikel.
- **TS-100.8 Masken-Regression:** Schalter an; in den Standorteinstellungen nur den Firmennamen ändern → Speichern. Erwartet: beide
  Schalter weiterhin an, Vorlagen und View-Name unverändert, Links weiterhin da.
- **TS-100.9 CSV:** Bei Schaltern an und aus je einen CSV-Export aus Artikel-Liste, Stückliste und FA-Liste. Erwartet: keine URL in der
  Datei, Artikel- und FA-Nummer-Spalte gleich.
- **TS-100.10 enaio-View-Name (Service, Testsystem):** Vorbedingung `Sync:EnaioDmsEnabled=true`, `ErrorNotification:Enabled=true`.
  (a) Default unverändert → nächster Lauf „erfolgreich“ mit Zählern wie vor dem Deploy. (b) Standorteinstellungen → enaio-View auf
  `vw;DROP` → Speichern → nächster Lauf: „fehlgeschlagen — Ungültiger View-Name“, genau ein Eintrag, Fehlermail, `EnaioDmsDocuments`
  unverändert. (c) Auf einen gültigen, aber nicht vorhandenen Namen (`[enaio].[dbo].[vw_gibtsnicht]`) → Lauf „fehlgeschlagen“ wie heute
  bei fehlender View, Fehlermail. (d) Feld leeren → Speichern → nächster Lauf nimmt den Default, „erfolgreich“. (e) Wert zurück auf den
  Default. Kein Dienst-Neustart zwischen den Schritten.
- **TS-100.11 Sync-Schalter unverändert:** `/ServiceSettings` → `Sync:EnaioDmsEnabled` steht dort wie bisher (Kategorie Sync), nicht in
  den Standorteinstellungen; aus → kein enaio-Lauf im Aktivitäts-Protokoll, auch bei gültigem View-Namen.
- **TS-100.12 Hilfe und README:** Hilfe → Artikelinfo, FA-Liste (enaio), FA-Vervollständigung, FA-Abarbeitung, Standorteinstellungen.
  Erwartet: Texte bedingt, alle Keys genannt.
- **Negativfälle:** (a) In `/Settings` den Wert `FALSE` eintragen → aus. (b) Den Wert `nein` eintragen → an (nur `false` sperrt). (c)
  Stücklistenzeile mit leerer Ressourcenummer → kein Vault-Link, CSV-Zelle leer. (d) Nicht-Admin ruft `/StandortEinstellungen` auf →
  kein Zugriff. (e) Vorlage ohne Platzhalter (z. B. `https://test.example/?q=`) → Nummer bzw. ID wird angehängt.

## Etappen (nur bei epic: true)

Entfällt (kein Epic). Ein Commit im Bündel-Worktree.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| - | entfällt | - | - |

## Deploy

> **Finalisiert durch qa-agent 2026-10-22** gegen den echten Diff `845e38c9..4bfa4576` (b1cb87e1, 9fe7553f, qa 4bfa4576).

- **deploy.web: ja.** Geändert unter `IdealAkeWms/`: Views/Partials, `Helpers/LinkVorlage.cs`, Seed in `Program.cs`,
  `StandortSettingsCatalog`, `ServiceSettingDefinitions`, `AppVersion` 1.67.0, Hilfe + Changelog.
- **deploy.service: ja.** Geändert unter `IDEALAKEWMSService/`: `EnaioDmsSyncService` liest `Sync:EnaioDmsViewName`,
  `FaHierarchySql.ValidateAndQuote(…, maxSegments)`, `AppVersion` 1.67.0. Den Dienst mit dem neuen Build einmal neu starten
  (Binärtausch). **Spätere Wertänderungen** an `Sync:EnaioDmsViewName` brauchen **keinen** Neustart (`ServiceSettings.cs:11-12`).
- **deploy.migration: nein.** Keine neue Datei unter `*/Migrations/`, nichts unter `SQL/` oder `SQL/Einmalig/`, kein AgentJob
  (`git diff --stat` ohne Treffer). Kein Backup-Schritt nötig.
- **Reihenfolge:** Web zuerst empfohlen (Seed der vier AppSettings + Katalog-Seed `Sync:EnaioDmsViewName` beim ersten Start), dann
  Dienst. Umgekehrt unkritisch: Ohne Zeile nimmt der Dienst den Default, das SQL ist zeichengleich.
- **Publish-Befehle** (im Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Ablauf:** Publish aus dem Worktree → Testsystem → Test (Checkliste am Ende) → Merge (Teil des Bündel-Merges, Schranke 2).
- **Nach dem Merge** nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen kombiniert hat.
- **Nach dem Deploy an AKE:** nichts tun. Kontrolle: Standorteinstellungen zeigen beide Gruppen „an“ mit den AKE-Vorlagen und dem
  View-Namen `[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]`.
- **Nach dem Deploy an IDEAL (Pflicht):** Standorteinstellungen → „Vault (Zeichnungen)“ → „Vault-Links anzeigen“ aus → Speichern (Antwort 5).
- **enaio an IDEAL: Entscheidung Gerald beim Deploy** (Antwort 5): Standorteinstellungen → „enaio (Dokumente)“ → „enaio-Dokument-Links
  anzeigen“ an lassen oder aus. Der Sync-Schalter `Sync:EnaioDmsEnabled` (`/ServiceSettings`) ist davon getrennt; gibt es an IDEAL eine
  eigene enaio-View, dort den View-Namen eintragen.

## Offene Rueckfragen

Keine. Die Rückfragen 1–7 vom 2026-10-21 sind in den Freigabe-Antworten (Gerald 2026-10-22) beantwortet und in den Rumpf übernommen.
Neue Rückfragen gibt es nicht: Bindestrich, dreiteiliger View-Name, Pflegeort des View-Namens und Gruppen ließen sich aus dem Code
festlegen (siehe „enaio-Sync“, „Pflegeort“, „Gruppen“).

### Verlauf: Rückfragen vom 2026-10-21 (beantwortet, Wortlaut unverändert)

1. **„Vault-Abgleich“:** Gerald schreibt „Vault-Abgleich und Vault-Buttons“. Im Code gibt es keinen Abgleich (Negativbeleg oben: Dienst,
   SQL inkl. AgentJobs, appsettings, ServiceSettings-Katalog, Web-Services, JS). Gemeint ist vermutlich nur das Abschalten der Links. Oder
   ist ein künftiger Abgleich gemeint, z. B. „hat dieser Artikel eine Zeichnung im Vault“?
   **Empfehlung:** Diese Spec schaltet nur die Links ab. Ein Abgleich mit dem Vault wäre eine neue Integration (Schnittstelle, Zugangsdaten,
   Dienst) und bekäme ein eigenes Backlog.
2. **Pflegeort:** Standorteinstellungen oder `/Settings`?
   **Empfehlung:** Standorteinstellungen, neue Gruppe „Vault (Zeichnungen)“. Es ist ein Standort-Unterschied, die Maske ist kuratiert und
   admin-only, und die jüngsten Standort-Schalter liegen dort (Hardware-Scanner, Briefkopf). In `/Settings` erscheinen die geseedeten
   Zeilen ohne Zusatzcode unter „Sonstige“ (dieselbe Zeile). Voraussetzung ist der Seed aus Rückfrage 3, sonst setzt die Maske den
   Schalter beim nächsten Speichern auf `false`.
3. **Default der Adresse:** Laufzeit-Seed (A), Code-Fallback (B) oder SQL-Seed (C)?
   **Empfehlung:** A, Seed in `Program.cs` wie `LagerbestellungAktiv` (`Program.cs:347`). AKE behält die Links ohne Zutun, die Zeilen
   existieren immer, keine Migration. B verliert die Links beim ersten Speichern der Maske; C ist doppelte Pflege ohne Mehrwert.
4. **Format und Namen:** Die heutige URL hat nach der Nummer noch `&searchContext=0`, eine reine „Basisadresse“ reicht also nicht.
   **Empfehlung:** Vorlage mit Platzhalter `{Artikelnummer}` (Key `VaultLinkVorlage`). Fehlt der Platzhalter, wird die kodierte Nummer
   angehängt; so funktioniert auch eine Basisadresse, die auf `search=` endet. Schalter-Key `VaultLinksAktiv` wie im Backlog vorgeschlagen.
5. **IDEAL nach Deploy:** Der Seed legt auch an IDEAL `VaultLinksAktiv=true` an, die Links bleiben dort also wie heute sichtbar, bis der Admin
   ausschaltet. Soll der Seed stattdessen standortabhängig sein (z. B. aus dem Master `ProduktionsauftragHierarchisch`)?
   **Empfehlung:** Nein, einheitlicher Seed plus Deploy-Schritt „an IDEAL ausschalten“. Der Master ist ein Struktur-Schalter, kein
   Standort-Kennzeichen für den Vault; die Kopplung wäre eine Ableitung aus einem fremden Namen.
6. **Kleine Verhaltensänderungen an AKE:** (a) In den Views 2-10 wird die Nummer künftig URL-kodiert statt nur HTML-kodiert (bei Ziffern und
   `-` keine Änderung, bei Leerzeichen, `&`, `+`, `#` jetzt korrekt). (b) Eine Stücklistenzeile mit leerer Ressourcenummer hat keinen
   (nutzlosen) Link mehr, und die CSV liefert dort leer statt „Zeichnung in Vault öffnen“.
   **Empfehlung:** Beides annehmen. Es sind Korrekturen, sie entstehen ohne Zusatzcode aus dem gemeinsamen Helfer.
7. **Zusatzfund enaio:** Die enaio-Adresse `http://akeosapp.ake.at/oscontentviewer/...` steht ebenfalls fest im Code
   (`_EnaioDmsBadges.cshtml:21`). Ob IDEAL enaio-Daten hat, ist hier nicht belegt (Fremdsystem).
   **Empfehlung:** Nicht in dieser Spec mitbauen. Falls IDEAL enaio-Symbole sieht, die ins Leere führen, ein eigenes Backlog nach demselben
   Muster.

> [!note] ueberholt
> Die Empfehlung zu Rückfrage 7 ist durch Antwort 7 ueberholt: enaio (Link-Vorlage, Schalter, View-Name des Syncs) gehört zu dieser Spec.

## Umsetzung (Dev-Lauf 2026-10-22)

Basis `845e38c9` (v1.66.2). Commits `b1cb87e1` (Code + Tests) und `9fe7553f` (wip feature-complete: v1.67.0 ×2, Changelog, Hilfe,
README, TS-100). Keine Migration. Auftrag war der Rumpf; die Freigabe-Antworten 1–7 sind eingearbeitet.

**Vault: zehn Stellen über `_VaultLink`.** Alle zehn Stellen rufen jetzt `@await Html.PartialAsync("_VaultLink", new VaultLinkViewModel(…))`:
- Artikelinfo, mit `ShowLabel: true`. `ArticleInfoViewModel.VaultUrl` und die Zuweisung in `ArticlesController` sind entfallen.
- Artikel-Liste, Bestandsübersicht, Bewegungshistorie.
- Stückliste mit `item.Ressourcenummer`.
- `_FaDocumentLinks`, `_ProductionOrderRow`, `_PickingLeitstandRow`, `_FaCompletionRow`, `_FaWorklistRow`.

Die eigenen `IsNullOrEmpty`-Klammern, die nur den Link umschlossen, sind entfallen (`_FaDocumentLinks`, `_FaCompletionRow`,
`_FaWorklistRow`); den Leerfall erledigt das Partial. Die Klammern in `_ProductionOrderRow`/`_PickingLeitstandRow`, die auch das
`<strong>` umschließen, bleiben. Das Markup ist dasselbe wie vorher, dazu kommt `rel="noopener"`. Der Stücklisten-Knopf ist unverändert.

**enaio:**
- `_EnaioDmsBadges` liest `EnaioLinksAktiv` und `EnaioLinkVorlage` einmal je Aufruf und baut jede URL über `LinkVorlage.Build`.
  Bei `null` gibt es kein Badge (`continue`). Die fünf Aufrufer sind unverändert.
- Dienst: `FaHierarchySql.ValidateAndQuote(viewName, int maxSegments = 2)`.
- `EnaioDmsSyncService`:
  - `DefaultViewName`.
  - Lesen über `ServiceSettings.GetValueSafeAsync` und Prüfen über `ResolveSource` (`maxSegments: 3`), beides **vor** dem bestehenden
    `try`.
  - Bei ungültigem Namen: `LogError` + `FinishFailed` + `throw`.
  - Das SQL lautet `FROM {quotedSource}`.
- Die übrigen Aufrufer von `ValidateAndQuote` und `FaHierarchySqlTests` sind unverändert.

**Einstellungen:**
- Vier Keys in `AppSettingKeys`.
- Laufzeit-Seed in `Program.cs` (Block vor Windows-Auth, nur bei fehlender Zeile).
- `Sync:EnaioDmsViewName` in `ServiceSettingDefinitions` (über den Katalog-Seed).
- Gruppen `GroupVault`/`GroupEnaio` am Ende von `GroupOrder`, je Feld mit Hinweis. `Sync:EnaioDmsEnabled` bleibt nur in
  `/ServiceSettings`.

**Doku:**
- Hilfe: neue Karte „Vault- und enaio-Links“ nach der Scanner-Karte; Artikelinfo, FA-Liste (enaio), FA-Vervollständigung und
  FA-Abarbeitung bedingt formuliert.
- README: vier AppSettings-Zeilen und eine ServiceSettings-Zeile.
- Testszenarien: TS-100.1–.12 + Negativfälle; TS-2.8, TS-4.5/4.5a (dabei „AppSetting `EnaioDmsEnabled`“ zu „ServiceSetting
  `Sync:EnaioDmsEnabled`“ berichtigt), 27.5 und 38.13 nachgezogen.

**Tests (neu/erweitert):**
- `LinkVorlageTests`: AKE-Adressen zeichengleich, Lesart des Schalters, leere Vorlage, leerer Wert, Kodierung, ohne Platzhalter,
  Platzhalter Ordinal.
- `VaultEnaioLinksTests`:
  - Die Adressen stehen nur in `Program.cs`, genau die zehn Stellen nutzen das Partial.
  - Kein CSS/JS blendet über `.vault-link`/`.dms-link` aus, der Stücklisten-Knopf bleibt.
  - Seed-Werte und Katalog-Gruppen; `Sync:EnaioDmsEnabled` ist unverändert und nicht in der Maske.
- `ServiceSettingDefinitionsTests` (Drift-Guard) um `Sync:EnaioDmsViewName` ergänzt.
- `EnaioDmsSyncServiceTests`: Default gleich Katalog, `ResolveSource` mit Default/leer/dreiteilig ergibt das bisherige SQL, ungültige
  Namen werfen, ohne `maxSegments` bleibt es bei zwei Teilen.

**Abweichungen:**
- Die Tests für `maxSegments: 3` stehen in `EnaioDmsSyncServiceTests`, nicht in `FaHierarchySqlTests`. Auftrag: „FaHierarchySqlTests
  bleiben unverändert“; die Spec (AK 25) nannte die Datei.
- Dass ein ungültiger Name zu „genau einem fehlgeschlagenen Lauf“ führt, ist nur im Code belegt (Prüfung vor dem `try`). Eine
  Test-Einspeisung des Werts geht ohne Settings-DB nicht, weil `GetValueSafeAsync` direkt per SQL liest. Nachweis daher Manual-UAT
  TS-100.10 b.

**Zusatzfunde (nur gemeldet, nicht gebaut):**
- `ColumnDefinitions.cs:59`: veralteter Kommentar „enaio links“.
- `IEnaioDmsDocumentRepository` ist im `PickingController` (`:26,65`) eingebunden, aber ungenutzt.

**Nachweis:** `dotnet build IdealAkeWms.slnx` ergibt 0 Fehler, 28 Warnungen (alle schon vorher vorhanden). `dotnet test`: Web **2178
bestanden, 1 übersprungen** (schon vorher), Dienst **328 bestanden**.

## QA-Nachweis (qa-agent, 2026-10-22)

Geprüft im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Range `845e38c9..9fe7553f` (b1cb87e1 + 9fe7553f), danach
qa-Commit `4bfa4576`. Web und Dienst wurden nicht lokal gestartet.

### 1. Build und Tests (selbst ausgeführt)

- `dotnet build IdealAkeWms.slnx` → **0 Fehler, 28 Warnungen** (alle vorbestehend, z. B. CS8321 `FormatDateWithKw`, CS8601 in
  `BdeTerminalAuftraegeTests`; keine in geänderten Dateien).
- `dotnet test`:
  - `IdealAkeWms.Tests` → **2178 erfolgreich, 1 übersprungen, 0 Fehler** (2179 gesamt; der Skip ist vorbestehend).
  - `IDEALAKEWMSService.Tests` → **328 erfolgreich, 0 übersprungen, 0 Fehler**.
- Nach dem qa-Commit (nur Razor-Kommentar): Web-Build 0 Fehler, `LinkVorlageTests` + `VaultEnaioLinksTests` 24/24 bestanden.

### 2. Code-Review (selbst, in diesem Lauf)

Gelesen: der vollständige Code-Diff (33 Dateien) und der Code drumherum:
- `EnaioDmsSyncService` ganz (Lauf-Lebenszyklus, beide `try`),
- `FaHierarchySql.ValidateAndQuote`/`ValidateSegment`, `ServiceSettings.GetValueSafeAsync`,
- `CachedSettingRepository` (DI `Program.cs:80`: die Partials bekommen den Cache-Decorator),
- `StandortEinstellungenController.Save`/`BuildViewModelAsync`/`IsTrue`,
- `_ViewImports.cshtml` (Namespace `Models.ViewModels` für `VaultLinkViewModel`), `_FaDocumentLinks`, `_FaCompletionRow`.

Prüfpunkte aus dem Auftrag:
- **Zehn Vault-Stellen:** genau zehn `PartialAsync("_VaultLink"` (grep und `VaultEnaioLinksTests`). `akevault24`/`akeosapp` stehen im
  Anwendungscode nur noch in `Program.cs:367,369` (Seed; Historie in `Changelog.cshtml` ausgenommen). Info mit `ShowLabel: true`,
  Markup wie vorher (Link-Symbol, `me-1`, „Im VAULT öffnen“, ohne `title` wie vorher), dazu `rel="noopener"`. Die Stückliste übergibt
  `item.Ressourcenummer`.
- **Razor-Klammern:** entfernt nur in `_FaDocumentLinks`, `_FaCompletionRow`, `_FaWorklistRow`; dort umschlossen sie nur das `<a>`.
  In `_ProductionOrderRow`/`_PickingLeitstandRow` bleibt die `IsNullOrEmpty`-Klammer um `<strong>` + Partial. Der Razor-Build kompiliert.
- **Keine Layout-Lücke (Code-Sicht):** Ein leeres Partial gibt nichts aus. Keine neue Spalte, kein `<th>`. Die Optik ist Manual-UAT.
- **Stücklisten-Knopf:** `_FaDocumentLinks.cshtml:11-12` unverändert, `site.css` unverändert. Kein JS oder CSS blendet über
  `.vault-link`/`.dms-link` aus (grep und Test).
- **enaio-Partial:** liest Schalter und Vorlage einmal je Aufruf, `LinkVorlage.Build` je Badge, bei `null` → `continue`. Die fünf
  Aufrufer sind nicht im Diff. `ToString(InvariantCulture)` auf `long`.
- **Dienst:**
  - Die Prüfung steht vor dem bestehenden `try`, mit eigenem `catch (FaHierarchyViewNameException)` → genau ein `LogError` +
    `FinishFailed` + `throw`, also kein doppeltes `FinishFailed`.
  - `GetValueSafeAsync` wirft nur `OperationCanceledException`; die bleibt vor dem `try` ungefangen, wie vorher bei Abbruch.
  - SQL `SELECT id, angelegt, Typ, WaNummer, WaNummerTrimmed FROM {quotedSource}`, mit dem Default zeichengleich (Test
    `ResolveSource_Default_und_dreiteiliger_Name_ergeben_das_bisherige_SQL`).
  - `maxSegments` ist optional mit Default 2. Die übrigen Aufrufer und `FaHierarchySqlTests.cs` sind nicht im Diff.
  - `Sync:EnaioDmsEnabled` im Katalog unverändert (Key, Bool, `false`, Sync), nicht in der Maske.
- **Seed:** `Program.cs:363-383`, legt nur fehlende Zeilen an. Die Werte sind zeichengleich mit den alten URLs: Vault (alter Ausdruck
  `ArticlesController.cs:317` und Views) mit `{Artikelnummer}` an derselben Stelle, enaio (`_EnaioDmsBadges.cshtml:21`) mit `{ObjektId}`.
  Damit existieren die Bool-Zeilen immer, und der Masken-Fallstrick (Bool ohne Zeile = aus) ist abgedeckt.
- **URL-Kodierung:** nur in `LinkVorlage.Build` (`Uri.EscapeDataString(wert.Trim())`). Razor HTML-kodiert das Ergebnis im Attribut
  zusätzlich; das ist korrekt (`&` der Vorlage steht als `&amp;` im Quelltext, der Browser liest `&`).
- **Bekannte Defektklassen:** keine Werte in `<script>`; kein `hidden` + `d-*`; keine Bindung paralleler Arrays; keine Validierung nur im
  Browser (der View-Name wird wie bei den Sage-Views erst im Dienst geprüft, so spezifiziert, `ViewNameHint`); eine Regel an einem
  Ort (`LinkVorlage.Build` für Vault und enaio, `ValidateAndQuote` für alle Views). Stiller Fallback: Ein leerer oder fehlender
  View-Name bzw. ein DB-Fehler beim Lesen der Einstellung führt zum Default. Das ist spezifiziert (Anforderung 7, Lösungsentwurf
  Schritt 2), kein Befund.
- **Doku:**
  - Hilfe-Karte und bedingte Texte sind valides HTML (`div`/`dl`/`dd` geschlossen). `{Artikelnummer}`/`{ObjektId}` stehen in Markup,
    nicht in einem Razor-Codeblock, also unkritisch (der Build kompiliert).
  - README: vier AppSettings-Zeilen + eine ServiceSettings-Zeile.
  - TS-100 neu; TS-2.8, 4.5, 4.5a, 27.5 und 38.13 angepasst.
  - `AppVersion` in Web und Dienst `1.67.0`, `Date = "2026-10-22"` (heute). Anwender-Changelog v1.67.0 vom 22.10.2026.
- **CSV/Druck:** Keine Druck-View im Diff. Die CSV liest nie `href` (`csv-export.js`, Beleg in der Spec). Randfall Stückliste mit
  leerer Ressourcenummer: kein `<a>` mehr, die Zelle ist leer statt `title` (Code-Sicht; der Export selbst ist Manual-UAT).

**Befunde:**

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| Q-1 | niedrig (Kommentar) | Der Kopfkommentar `_FaDocumentLinks.cshtml:5` „Vault-Link (nur wenn ArticleNumber vorhanden)“ war nach der Umstellung unvollständig: Schalter und Vorlage fehlten, ebenso der Hinweis auf den Stücklisten-Knopf. | **Behoben** im qa-Commit `4bfa4576` (nur Kommentar). |
| Q-2 | niedrig (Hinweis, kein Code-Fix) | Zwei Lesarten desselben Schalters: `LinkVorlage.Build` liest „alles außer `false` = an“. Die Standort-Maske zeigt ein Bool nur bei `true`/`on`/`1` als an (`StandortEinstellungenController.cs:155-158`) und schreibt beim Speichern `"false"`. Ein in `/Settings` eingetragener Wert wie `nein` lässt die Links an, wird aber beim nächsten Speichern der Maske zu `false`. Mit dem geseedeten `true` tritt das nicht auf. | So spezifiziert (Anforderung 1, Negativfall b). Die Checkliste setzt nach Negativfall b den Wert auf `true` zurück. Kein Blocker. |
| Q-3 | niedrig (Brain) | `secondbrain/tests/testszenarien-index.md` hatte keinen TS-100-Eintrag. | **Behoben** durch den qa-agent (Hauptcheckout, Zeile „100.“). |
| Q-4 | niedrig (Brain, offen) | Brain-Changelog `secondbrain/changelog/2026-10-22-v1-67-0-…md` und der Eintrag in `feature-map.md` für v1.67.0 fehlen noch. | An den Aufrufer gemeldet (Brain-Abschluss des Dev-Laufs). Blockiert den Test nicht. |

Keine Blocker.

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweis |
|---|---|
| 1 AKE-Regression Vault | Regel automatisiert (`LinkVorlageTests.AkeVorlagen_ergeben_die_bisherigen_Adressen`) + Seed-Wächter; Rendering **Manual-UAT** |
| 2 Seed | Quelltext-Wächter (`VaultEnaioLinksTests.Seed_und_Standort_Katalog`); Start gegen SQL Server und „nicht überschreiben“ **Manual-UAT** |
| 3 Vault aus | Regel automatisiert (`Nur_false_schaltet_aus`); HTML der zehn Stellen **Manual-UAT** |
| 4 Stücklisten-Knopf | Quelltext-Wächter (`Stuecklisten_Knopf_bleibt_kein_Ausblenden_ueber_Klassen`); Anzeige **Manual-UAT** |
| 5 Vault-Vorlage leer | Regel automatisiert (`Leere_Vorlage_ergibt_keinen_Link`); Anzeige **Manual-UAT** |
| 6 Leere Ressourcenummer | Regel automatisiert (`Leerer_Wert_ergibt_keinen_Link`), Verdrahtung per Wächter; CSV **Manual-UAT** (Browser-JS) |
| 7 AKE-Regression enaio | Regel automatisiert (AKE-enaio-URL); Icons/Reihenfolge/Titel **Manual-UAT** |
| 8 enaio aus | Regel automatisiert; HTML **Manual-UAT** |
| 9 enaio-Vorlage leer | Regel automatisiert; Anzeige **Manual-UAT** |
| 10 enaio-Vorlage wirkt | **automatisiert** (`Ohne_Platzhalter_wird_angehaengt_Platzhalter_exakt`) |
| 11 Lesart Schalter | **automatisiert** (`Nur_false_schaltet_aus`, `Alles_ausser_false_laesst_an`) |
| 12 Kodierung | **automatisiert** (`Wert_wird_url_kodiert_eingesetzt`) |
| 13 Keine Layout-Lücke | **Manual-UAT** (Desktop + Handheld) |
| 14 Vorlage wirkt sofort | **Manual-UAT** (Cache-Invalidierung im echten Ablauf) |
| 15 Masken-Regression | **Manual-UAT** (echtes Model-Binding der Maske; Seed-Existenz nur als Wächter) |
| 16 Standorteinstellungen | Katalog automatisiert (Gruppen am Ende, Felder, Hinweise); Anzeige und Admin-only **Manual-UAT** |
| 17 CSV/Druck | **Manual-UAT** (Browser-JS); Druck-Views nicht im Diff |
| 18 Doku | **Manual-UAT** (Sichtprüfung) |
| 19 Ein Ort | **automatisiert** (`Adressen_stehen_nur_im_Seed_und_jede_Vault_Stelle_nutzt_das_Partial`) |
| 20 View-Name Default | Bezeichner automatisiert (`ResolveSource`); Lauf gegen enaio **Manual-UAT** (raw SQL, Fremdsystem) |
| 21 Ungültiger View-Name | Validator automatisiert (`ResolveSource_ungueltiger_Name_wirft`); „genau ein fehlgeschlagener Lauf + Fehlermail“ **Manual-UAT** (Settings-Read per raw SQL) |
| 22 Bindestrich, drei Teile | **automatisiert** (`ValidateAndQuote_ohne_maxSegments_bleibt_bei_zwei_Teilen`; `FaHierarchySqlTests` grün) |
| 23 Sync-Schalter unverändert | Katalog automatisiert; Lauf-Gate **Manual-UAT** |
| 24 Ohne Neustart | **Manual-UAT** |
| 25 Nachweis | erfüllt (siehe 1.). Abweichung: die Tests für `maxSegments: 3` stehen in `EnaioDmsSyncServiceTests`, `FaHierarchySqlTests.cs` bleibt bewusst unverändert |

Grün zählen nur die automatisiert bewiesenen Teile. Alles mit „Manual-UAT“ steht in der Checkliste am Ende.

### 4. Geänderte Bestandstests

- `IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs`: eine `InlineData("Sync:EnaioDmsViewName")` ergänzt. Das erweitert den
  Drift-Guard um den neuen Key (AK 25); kein bestehender Fall geändert.
- `IDEALAKEWMSService.Tests/Services/EnaioDmsSyncServiceTests.cs`: nur neue Tests angehängt. Der bestehende Test (ohne Konfiguration →
  fehlgeschlagen) ist unverändert und grün.
- Keine Fixture angepasst, keine Erwartung gelockert, kein Fall „sonstiges“.

### 5. CLAUDE.md-Checkliste

- Migration, SQL, FreshInstall, AgentJobs: entfällt (kein Schema).
- Audit-Felder: entfällt (`AppSetting`/`ServiceSetting` sind keine `AuditableEntity`).
- Version in beiden `AppVersion.cs` + Anwender-Changelog: ja.
- `docs/TESTSZENARIEN.md`: ja (TS-100 + Anpassungen). Testindex: ja (qa-agent).
- README + Hilfe: ja. `fallstricke.md`: Abschnitt „Default-an-Schalter und Vorlagen … (v1.67.0)“ vorhanden.
- Neue Rolle oder neuer Filter: keine.
- Brain-Changelog + feature-map: offen (Q-4).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. „Vault-Abgleich“: nur Links abschalten oder etwas anderes gemeint? → **Nur die Links. Ein Abgleich ist nicht gemeint.** (Gerald 2026-10-22)
2. Pflegeort Standorteinstellungen (Gruppe „Vault (Zeichnungen)“) statt nur /Settings? → **Ja: Die heute fest codierte Link-Adresse wird in den Einstellungen eingegeben, dazu der Schalter Ein/Aus.** (Gerald 2026-10-22)
3. Default der Adresse per Laufzeit-Seed in Program.cs (Variante A)? → **Wie empfohlen, für Vault und enaio: heutige Adressen per Laufzeit-Seed, AKE merkt keinen Unterschied.** (Gerald 2026-10-22)
4. Vorlage mit Platzhalter {Artikelnummer}, Keys VaultLinksAktiv + VaultLinkVorlage? → **Wie empfohlen: Vorlage mit Platzhalter, Vault `{Artikelnummer}`, enaio `{ObjektId}`.** (Gerald 2026-10-22)
5. Einheitlicher Seed „an“, IDEAL schaltet als Deploy-Schritt aus? → **Wie empfohlen: Seed „an“; IDEAL schaltet Vault beim Deploy aus. enaio bei IDEAL entscheidet Gerald beim Deploy über den Schalter.** (Gerald 2026-10-22)
6. URL-Kodierung überall + kein Link bei leerer Ressourcenummer in Ordnung? → **Ja, für Vault und enaio: Nummer kodiert, leeres Feld = kein Link.** (Gerald 2026-10-22)
7. enaio-Adresse nicht in dieser Spec (ggf. eigenes Backlog)? → **Doch, mitnehmen: enaio genauso – Link-Adresse konfigurierbar, Schalter Ein/Aus. Eventuell auch die enaio-View des Syncs konfigurierbar machen.** (Gerald 2026-10-22)

## Manuelle Test-Checkliste (Mensch, Testsystem)

Vorbedingung: Web und Dienst aus dem Worktree auf das Testsystem publiziert (Befehle im Deploy-Abschnitt), Web einmal gestartet.
Details der Szenarien: `docs/TESTSZENARIEN.md` TS-100.

1. **Seed (AK 2):** `/Settings` → „Sonstige“ zeigt `VaultLinksAktiv=true`, `VaultLinkVorlage`, `EnaioLinksAktiv=true` und
   `EnaioLinkVorlage` mit den AKE-Adressen; `/ServiceSettings` zeigt `Sync:EnaioDmsViewName=[enaio].[dbo].[vw_IDEAL-AKE_Fertigungsauftraege]`.
   Einen Wert ändern, Web neu starten → der Wert bleibt (kein Überschreiben). Danach zurücksetzen.
2. **AKE-Regression Vault (AK 1, TS-100.1):** Artikelinfo, Artikel-Liste, Bestandsübersicht, Bewegungshistorie, Stückliste, FA-Liste,
   Kommissionier-Leitstand, FA-Vervollständigung (Liste + Edit-Kopf), FA-Abarbeitung: Vault-Symbol bzw. „Im VAULT öffnen“ ist da. Je
   einen Link klicken → neuer Tab, Vault-Suche mit der richtigen Nummer, URL wie vor dem Deploy.
3. **AKE-Regression enaio (AK 7):** FA-Liste, Leitstand, FA-Vervollständigung (Liste + Edit-Kopf mit Text), FA-Abarbeitung: gleiche
   Icons, Reihenfolge und Titel; Klick → enaio-Viewer `…/viewer/<ObjektId>/?pagecount=1`.
4. **Standorteinstellungen (AK 16):** Gruppen „Vault (Zeichnungen)“ (2 Felder) und „enaio (Dokumente)“ (3 Felder) am Ende, jedes Feld
   mit Hinweis, beide Schalter „an“. Als Nicht-Admin `/StandortEinstellungen` aufrufen → kein Zugriff.
5. **Vault aus (AK 3, 13, TS-100.2):** Vault-Schalter aus → Speichern. An allen zehn Stellen: kein Vault-Symbol, kein „Im VAULT
   öffnen“, die Artikelnummer steht, keine Lücke, keine leere Spalte; im Seitenquelltext kein `akevault24`. enaio-Badges unverändert.
   Desktop und Handheld.
6. **enaio aus (AK 8, 13, TS-100.3):** enaio-Schalter aus → Speichern. An den fünf enaio-Stellen: kein `dms-link`, kein `akeosapp` im
   Quelltext, die FA-Nummer steht, keine Lücke. Vault-Links unverändert.
7. **Stücklisten-Knopf (AK 4, TS-100.4):** beide Schalter aus → FA-Vervollständigung Edit: „Stückliste öffnen“ ist sichtbar und öffnet
   die read-only Stückliste. Der Knopf „Stückliste“ in den Listen FA-Vervollständigung und FA-Abarbeitung ist ebenfalls da.
8. **Vorlage leer (AK 5, 9, TS-100.5):** Schalter an, Vault-Vorlage leeren → keine Vault-Links; enaio-Vorlage leeren → keine Badges.
   Vorlagen wiederherstellen → Links wieder da.
9. **Vorlage ändern, wirkt sofort (AK 10, 14, TS-100.6):** Vault `https://test.example/s?q={Artikelnummer}&x=1`, enaio
   `https://test.example/view?id={ObjektId}` → der nächste Seitenaufruf zeigt genau diese Adressen. Ohne Platzhalter
   (`https://test.example/?q=`) wird die Nummer bzw. ID angehängt. Danach die AKE-Vorlagen zurück.
10. **Masken-Regression (AK 15, TS-100.8):** nur den Firmennamen ändern → Speichern → beide Schalter an, Vorlagen und View-Name unverändert.
11. **Lesart (AK 11, Negativfälle a/b):** in `/Settings` `VaultLinksAktiv=FALSE` → aus; `nein` → an. **Danach wieder `true` eintragen**,
    sonst schreibt das nächste Speichern der Standort-Maske `false` (Befund Q-2).
12. **CSV (AK 6, 17, TS-100.9):** bei Schaltern an und aus je ein CSV-Export aus Artikel-Liste, Stückliste und FA-Liste → keine URL,
    Nummern-Spalten gleich. Stücklistenzeile ohne Ressourcennummer → Zelle leer (nicht „Zeichnung in Vault öffnen“).
13. **enaio-Dienst (AK 20, 21, 24, TS-100.10):** mit `Sync:EnaioDmsEnabled=true` und `ErrorNotification:Enabled=true`, ohne
    Dienst-Neustart zwischen den Schritten:
    a) Default → Lauf erfolgreich, Zähler „neu“/„aktualisiert“ wie vorher (Service-Filter `EnaioDms`).
    b) `vw;DROP` → **genau ein** Eintrag „fehlgeschlagen — Ungültiger View-Name“, Fehlermail, `EnaioDmsDocuments` unverändert
       (`SELECT COUNT(*), MAX(ModifiedAt) FROM EnaioDmsDocuments` vorher und nachher).
    c) `[enaio].[dbo].[vw_gibtsnicht]` → fehlgeschlagen + Fehlermail.
    d) Feld leeren → der Lauf nimmt den Default, erfolgreich.
    e) Default zurück.
14. **Sync-Schalter (AK 23, TS-100.11):** `Sync:EnaioDmsEnabled` steht nur in `/ServiceSettings` (Kategorie Sync); aus → kein enaio-Lauf.
15. **Doku (AK 18, TS-100.12):** Hilfe → Karte „Vault- und enaio-Links“, Artikelinfo, FA-Liste (enaio), FA-Vervollständigung,
    FA-Abarbeitung: bedingte Texte, Keys genannt. Changelog v1.67.0; Versionsanzeige 1.67.0 in Web und Dienst.
16. **IDEAL nach dem Einspielen:** Vault-Schalter aus; enaio nach Entscheidung Gerald.
