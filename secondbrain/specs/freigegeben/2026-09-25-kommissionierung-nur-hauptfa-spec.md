---
type: spec
title: "IDEAL: Kommissionierung nur auf dem HauptFA — gemeinsame Relevanzregel, „Alle Ziele“ im Komm.-Ziel-Filter, Freigabe-Kaskade-Rückbau, Picking-Pfad auf HauptFA beschränkt"
slug: 2026-09-25-kommissionierung-nur-hauptfa-spec
status: Testbereit
created: 2026-09-25
updated: 2026-09-25
source_backlog: "[[2026-09-13-kommissionierung-nur-hauptfa]]"
task: "[[2026-09-25-kommissionierung-nur-hauptfa-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Services/KommissionierListenService.cs Z. 228-236 (`BuildFlagPredicate`) — Kriterium NICHT ändern (bleibt einzig: `Kommissionieren` nicht leer), aber in eine neue, gemeinsame Methode auslagern statt es dort inline zu prüfen. Kein Standorteinstellungen-Schalter (Antwort 2), kein Konstruktor-Parameter."
  - "IdealAkeWms/Services/KommissionierRelevanzFilter.cs (NEU) — EINE reine, statische Methode `IsRelevant(string? kommissionieren) => !string.IsNullOrWhiteSpace(kommissionieren)`. `BuildFlagPredicate` ruft sie auf. Dokumentierter Zweck: der Ort, an dem künftige Zusatzkriterien hinzukämen — heute EIN Verbraucher (Antwort 2), kein Vortäuschen einer zweiten Implementierung."
  - "IdealAkeWms/Models/ProductionOrder.cs — EINE Formel als Quelle, als `Expression` statt reiner Instanz-Eigenschaft (zweiter Prüfdurchgang S9/Punkt 1, ersetzt die zunächst geplante reine `IsNullOrEmpty`-Eigenschaft samt separater Inline-Kopie in den Warteschlangen-Abfragen): `public static readonly Expression<Func<ProductionOrder,bool>> IsHauptFa = p => p.SubOrderNumber == \"\" || p.SubOrderNumber == p.OrderNumber;` — `private static readonly Func<ProductionOrder,bool> IsHauptFaCompiled = IsHauptFa.Compile();` — `[NotMapped] public bool IsSubFa => !IsHauptFaCompiled(this);`. Neu: `using System.Linq.Expressions;` und `using System.ComponentModel.DataAnnotations.Schema;` (`[NotMapped]`) — beide bislang nicht in der Datei importiert (verifiziert: Stand hat nur `using System.ComponentModel.DataAnnotations;`). `null` gilt bei dieser Formel als Sub-FA (verschoben gegenüber der zuerst geplanten `IsNullOrEmpty`-Fassung), folgenlos, weil die Spalte `NOT NULL` mit Default `string.Empty` ist und kein Schreibpfad `null` erzeugt (verifiziert: `ApplicationDbContext.cs:414` `IsRequired()`) — bewusst kein eigener Testfall für `null`."
  - "IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs Z. 174-216 (`GetReleasedForPickingAsync`, `GetReleasedForPickingByPickerAsync`, `GetReleasedForPickingCountAsync`, `GetMaxPickingPriorityAsync`) — alle VIER Abfragen bekommen `.Where(ProductionOrder.IsHauptFa)`. `GetReleasedForPickingCountAsync` und `GetMaxPickingPriorityAsync` laufen heute auf der Navigation `_context.ProductionOrderPickingStatuses`/`s.ProductionOrder` (Z. 203-216) — sie werden auf die Wurzel `_context.ProductionOrders` umgeschrieben, mit demselben Prädikat, das `GetReleasedForPickingAsync` bereits nutzt (`p.PickingStatus != null && p.PickingStatus.IsReleasedForPicking && !p.IsDone && !p.IsCancelled`, bei Count zusätzlich `!p.PickingStatus.IsDonePicking`, bei Max `p.PickingStatus.PickingPriority != null` und `excludeProductionOrderId` auf `p.Id`, `MaxAsync(p => (int?)p.PickingStatus!.PickingPriority) ?? 0` — exakt wie heute, keine Semantikänderung). 1:1-Beziehung verifiziert: UNIQUE-Index `ProductionOrderId` (`ApplicationDbContext.cs:455`, `UQ_ProductionOrderPickingStatus_ProductionOrderId`) und `.WithOne(p => p.PickingStatus)` (Z. 463-466) erlauben den Wechsel der Wurzel ohne Verhaltensänderung."
  - "IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs Z. 218-281 (`SetReleaseBatchAsync`) — neuer Skip-Zweig VOR dem bestehenden `SkippedNoArticle`-Zweig (Z. 246-250): `if (row.ProductionOrder.IsSubFa) { result.SkippedSubFa.Add(row.ProductionOrder.OrderNumber); continue; }` — ungated von `release`, also Sub-FA wird in Bulk in BEIDEN Richtungen (Freigeben UND Zurücknehmen) übersprungen (konsistent zur beidseitigen Sperre in `ToggleRelease`). Neues Feld `BulkReleaseResult.SkippedSubFa` in `IdealAkeWms/Data/Repositories/IProductionOrderPickingStatusRepository.cs` Z. 5-9."
  - "IdealAkeWms/Controllers/PickingController.cs Z. 280-290 (`Bom`-Action) — neuer Guard direkt nach dem `order == null`-Check, VOR dem bestehenden Artikelnummer-Guard (Z. 286-290, dessen Muster als Vorbild dient): ist `order.IsSubFa`, `TempData[\"WarningMessage\"] = \"Kommissionierung erfolgt nur am HauptFA {order.OrderNumber}.\"` + `RedirectToAction(nameof(Index))` (exakt dasselbe Muster wie der Artikelnummer-Guard, kein `returnUrl` an dieser Action vorhanden)."
  - "IdealAkeWms/Controllers/PickingLeitstandController.cs Z. 356-405 (`ToggleRelease`) — Guard vor der Freigabe-Logik, unabhängig von der Richtung (`order.IsSubFa` blockt sowohl Freigeben als auch Zurücknehmen — Warteschlange filtert Sub-FAs ohnehin über die Repository-Änderung oben aus, ein Zurücknehmen-Sonderpfad ist daher nicht nötig, siehe Freigabe-Antwort 3)."
  - "IdealAkeWms/Controllers/PickingLeitstandController.cs Z. 409-451 (`BulkRelease`) — neue `TempData[\"WarningMessage\"]`-Zeile für `batch.SkippedSubFa`, analog der bestehenden Zeile für `SkippedNoArticle` (Z. 446-447)."
  - "IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml — lokale Variable `isHauptFaRow = Model.Hierarchical && !item.IsSubFa` — KEINE eigene Vergleichsformel (Freigabe-Nachtrag 2026-09-25: sonst waere es eine sechste Fassung der Regel, die `IsHauptFa` gerade auf eine zurueckfuehrt; ist `item` kein `ProductionOrder`, sondern ein ViewModel, dort ein Feld aus `IsSubFa` befuellen, statt die Formel erneut zu schreiben). Dieselbe Umstellung fuer die bestehende Variable in `Views/ProductionOrders/_ProductionOrderRow.cshtml` Z. 16-18 (heute `!IsNullOrEmpty && ==`); Freigabe-Spalte (Z. 217-259) UND Bulk-Checkbox-Spalte (Z. 18-29) zusätzlich auf `!Model.Hierarchical || isHauptFaRow` bedingt."
  - "IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml Z. 30-42 UND IdealAkeWms/Views/ProductionOrders/_ProductionOrderRow.cshtml Z. 30-49 — der interaktive Stückliste-Link (`asp-controller=\"Picking\" asp-action=\"Bom\"`, NICHT der read-only `FaWorklist`-Zweig) zusätzlich auf `!Model.Hierarchical || isHauptFaRow` bedingt. `FaWorklist/Bom` bleibt unverändert auf jeder Sub-FA-Zeile sichtbar."
  - "IdealAkeWms/Views/Picking/Bom.cshtml — KEINE eigene Checkbox, KEIN eigener Badge-Zweig. Stattdessen am bestehenden Komm.-Ziel-Select2 (`setupKommissionierzielDropdown`, Z. 1049-1083): neue, EXKLUSIVE erste Option `<option value=\"!(leer)\">Alle Ziele (nur kommissionier-relevant)</option>` im `<select multiple>`. Auswahl von „Alle Ziele“ deselektiert alle Einzelziele und umgekehrt (im `change`-Handler Z. 1075-1082, sonst entstünde `!(leer),KA-02`, das die Ausschluss-Syntax in `bomMatchesFilter` auslöst). Zusätzlich (zweiter Prüfdurchgang, Nebeneffekt von B3): Die Vorbelegung aus dem gespeicherten Filterwert (`pre`, Z. 1065) erkennt `!(leer)` case-insensitiv und bildet einen so erkannten Teil auf den exakten Options-Wert `!(leer)` ab, bevor `$sel.val(pre)` (Z. 1074) aufgerufen wird — sonst verwirft Select2 eine Vorbelegung wie `!(LEER)` still, weil keine Option so exakt heißt. Das behebt nebenbei H8 (ein aus `sessionStorage` kleingeschrieben wiederhergestelltes Einzelziel wie `ka-02` wird durch dieselbe case-insensitive Zuordnung ebenfalls erkannt) — kein eigener Umfang, nur ein Nebeneffekt derselben Mapping-Logik."
  - "IdealAkeWms/Views/Picking/Bom.cshtml Z. 830-837 (`bomMatchesFilter`) — EIN neuer Sonderfall am Anfang der Funktion: `val` kommt an dieser Stelle bereits kleingeschrieben aus `window.getActiveFilters()` (`table-filter.js:287`, `input.value.toLowerCase().trim()`, zweiter Prüfdurchgang verifiziert) — deshalb genügt der exakte Vergleich `val === '!(leer)'` ohne eigene `toLowerCase()`-Behandlung in dieser Funktion. Ist der Gesamtwert so `!(leer)`, dann `return text.trim() !== ''` (Zellinhalt nicht leer), statt der Ausschluss-Syntax (`val.startsWith('!')`) zu folgen, die `!(leer)` sonst als „enthält nicht '(leer)'“ läse und ALLES anzeigen würde. Dieser eine Sonderfall deckt alle drei Aufrufer ab: `updateBomVisibility` (Z. 855ff), `expandAncestorsOfMatching` (Z. 1086ff) und `checkKommissionierzielEmptyState` (Z. 1107ff) — keine Änderung an diesen drei Funktionen selbst nötig."
  - "IdealAkeWms/Views/Picking/Bom.cshtml Z. 999-1023 (`renderDefaultFilterBadges`) — Sonderfall für `cfg.key === 'kommissionieren'`: `bomFilterInputValue(cfg.key)` liefert den **rohen**, NICHT kleingeschriebenen Input-Wert (Z. 995-998, anders als `getActiveFilters`); der Vergleich erfolgt deshalb ausdrücklich case-insensitiv (`val.trim().toLowerCase() === '!(leer)'`, zweiter Prüfdurchgang) — Badge-Text dann „Alle Ziele (nur kommissionier-relevant)“ statt „Gefiltert: Komm.-Ziel = !(leer)“."
  - "IdealAkeWms/Views/Picking/Bom.cshtml Z. 1107-1125 (`checkKommissionierzielEmptyState`) — derselbe Grund wie bei `renderDefaultFilterBadges`: `bomFilterInputValue('kommissionieren')` liefert den rohen Wert, Vergleich deshalb case-insensitiv (`val.trim().toLowerCase() === '!(leer)'`). Statt „Standardfilter Kommissionierziel „!(leer)“ aktiv …“ dann die Formulierung „Standardfilter „Alle Ziele (nur kommissionier-relevant)“ aktiv — keine Treffer in dieser Stückliste.“"
  - "IdealAkeWms/Views/Picking/Bom.cshtml Z. 1167-1211 (`btnPrintBom`-Handler) — zwei Ergänzungen (zweiter Prüfdurchgang S11/Punkt 4): (1) Ist beim Klick keine Zeile sichtbar (`visiblePositions.length === 0`), öffnet der Handler **keinen** Druck (`window.open` entfällt). Stattdessen macht er die bestehende Box `#bomKzEmptyState` (Z. 136-138) mit dem Text „Keine sichtbaren Positionen — nichts zu drucken.“ sichtbar — direkt gesetzt, NICHT über `checkKommissionierzielEmptyState()`, weil diese nur den Komm.-Ziel-Filter kennt und der Hinweis bei JEDEM aktiven Filter greifen soll, nicht nur bei „Alle Ziele“. Der bereits gebundene Reset-Knopf `#bomKzEmptyStateReset` (ruft `resetAllBomFilters()`, Z. 1161-1162) bedient auch diesen Fall mit. Kein `alert()`. (2) Beim Aufbau von `filterParts` (Z. 1188-1190) Sonderfall für `col === 'kommissionieren'` mit Wert `!(leer)` (aus `getActiveFilters()`, dort bereits kleingeschrieben): Textbaustein „Komm.-Ziel=Alle Ziele (nur kommissionier-relevant)“ statt des Rohwerts. Keine Änderung an `PrintBom.cshtml`/`colNames`/der `PrintBom`-Action nötig (Whitelist bereits durch [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] erledigt)."
  - "NICHT ANFASSEN: wwwroot/js/table-filter.js (`applyFilters` Z. 293-330, `matchesFilter` Z. 272, `init()` Z. 229-233) — gemeinsames JS für alle Server-Mode-Listen. Begründung präzisiert (zweiter Prüfdurchgang S13/Punkt 3): Beim `window.setColumnFilter`-Aufruf und beim Tippen in die Filterzeile läuft `updateBomVisibility()` synchron im selben JS-Task wie table-filter.js — kein Flackern möglich. Beim Seitenaufbau mit einem aus `sessionStorage` wiederhergestellten Filter (inkl. eines gespeicherten `!(leer)`) ruft `init()` im `column-preferences-ready`-Handler `applyFilters()` auf (Z. 229-233), während die Bom-Korrektur erst im eigenen `setTimeout(…, 0)`-Task (Z. 1136-1163) läuft — dazwischen KANN ein Frame ungefiltert gezeichnet werden. Das besteht bereits heute für JEDEN Bom-Filter (auch `procurement`, `article-group`, `description1`), weil `applyFilters` den Baumzustand ignoriert — kein neues Verhalten durch `!(leer)`. Kein weiterer Pfad betroffen: `sortTable` filtert nicht, Bom hat keine Datumsspalte (Kalender-Pfad `applyColumnFilterNow` greift nicht), `data-clear-table-filters` kommt in Bom nicht vor. Die `!(leer)`-Sonderbehandlung bleibt in `bomMatchesFilter` isoliert. Die Vereinheitlichung der Mini-Syntax ist Sache von [[2026-09-23-suchsyntax-spaltenfilter-erweitern]]."
  - "IdealAkeWms/Views/Account/Profile.cshtml Z. 53 UND IdealAkeWms/Views/Users/Edit.cshtml Z. 117 (Freitext-Inputs `DefaultFilterBomKommissionierziel`) — Placeholder/Hilfetext ergänzen, der `!(leer)` als „Alle Ziele“ erklärt (z. B. `placeholder=\"z. B. KA-02 oder !(leer) für alle Ziele\"`). Keine Validierungsänderung: `ProfileViewModel` Z. 71-73/`UserEditViewModel` Z. 83-85/`User` Z. 96-98 haben nur `[StringLength(200)]`, `!` und Klammern sind bereits zulässig, `Html.Raw`-Vorbelegung in `Bom.cshtml` Z. 1146 ist für `!(leer)` unkritisch. ABER: keine Normalisierung mehr über eine bloße `Trim()`-Annahme — siehe eigener affected_code-Eintrag `KommissionierzielFilterWert`."
  - "IdealAkeWms/Services/KommissionierzielFilterWert.cs (NEU, zweiter Prüfdurchgang B3) — EINE statische Funktion `Normalize(string? input) => (string? Value, bool DroppedTargets)`. Regel: leer/Whitespace → `(null, false)`. Enthält irgendein durch Komma getrennter, getrimmter Teil — ohne Rücksicht auf Groß-/Kleinschreibung — den Wert `!(leer)`, dann Rückgabe `(\"!(leer)\", DroppedTargets)`, wobei `DroppedTargets` genau dann `true` ist, wenn neben `!(leer)` weitere Ziele angegeben waren und verworfen wurden — NICHT bei reiner Schreibweisen-Korrektur wie `!(LEER)` (Freigabe-Nachtrag 2026-09-25: gemeldet wird eine Aenderung der Bedeutung, nicht der Schreibweise). Normalisiert wird trotzdem jede Roheingabe, die nicht bereits exakt `\"!(leer)\"` war (deckt sowohl den Fall „kombiniert mit Einzelzielen“ als auch „nur andere Schreibweise“ ab) — Einzelziele entfallen in jedem Fall. Sonst `(getrimmter Wert, false)`. Aufgerufen von `AccountController.cs` Z. ~219 (Profil-POST) UND `UsersController.cs` Z. ~129 (Create-POST) UND Z. ~301 (Edit-POST) statt der bisherigen Inline-`Trim()`. Bei `DroppedTargets == true` (und nur dann) zusätzlich `TempData[\"WarningMessage\"] = \"Alle Ziele schließt Einzelziele aus — gespeichert wurde nur 'Alle Ziele'.\"` neben dem bestehenden `TempData[\"SuccessMessage\"]` (verifiziert: alle drei POSTs enden mit `SuccessMessage` + `RedirectToAction`; TempData kennt nur SuccessMessage/WarningMessage — Hausregel). Unit-Test (`IdealAkeWms.Tests`, neue Datei) deckt ab: `\"!(leer),KA-02\"` → `!(leer)` + DroppedTargets=true (Hinweis); `\"!(LEER)\"` → `!(leer)`, DroppedTargets=false, KEIN Hinweis (reine Schreibweise, Bedeutung unveraendert); `\"KA-02\"` → unverändert, nicht normalisiert; `\"  \"` → `null`, nicht normalisiert; `\"!(leer)\"` → unverändert, nicht normalisiert (kein Hinweis)."
  - "RÜCKBAU (Block 4/Tasks 12-14 aus [[2026-09-10-fa-liste-ausbau-matchcode-spec]], bereits gebaut, Testbereit, noch NICHT gemergt): IdealAkeWms/Controllers/PickingLeitstandController.cs Z. 543-616 (`CascadeReleasePreview`/`CascadeRelease` inkl. Kommentarblock Z. 543-549) vollständig entfernen. IdealAkeWms/Data/Repositories/IProductionOrderPickingStatusRepository.cs Z. 56 (`SetReleaseForOrderNumberAsync`) + Z. 62 (`CountReleasedByOrderNumberAsync`, verifiziert ohne Fremdverwendung außer `CascadeReleasePreview` Z. 563 und eigenen Tests — kann mitentfernt werden) + Implementierungen in ProductionOrderPickingStatusRepository.cs entfernen. IdealAkeWms/Views/PickingLeitstand/Index.cshtml Z. 206-217 (Button), Z. 398-451 (Modal), JS ab Z. 556 vollständig entfernen — Layout-Hinweis: der Button steht inline in derselben `<td colspan>` hinter „Alle Sub-FAs fertigmelden“, Entfernen hinterlässt keine Lücke. IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs (24 Fundstellen) + IdealAkeWms.Tests/Repositories/ProductionOrderPickingStatusRepositoryTests.cs (13 Fundstellen) entfernen. IdealAkeWms/Views/Help/Changelog.cshtml Z. 180 (Überschrift) + Z. 203-207 (Bullet) als zurückgenommen kennzeichnen. docs/TESTSZENARIEN.md: TS-73-Titel Z. 7726, Z. 7753, TS-73.11-13, Z. 7895, Z. 7940-7952, Schlusszeile Z. 8575 als „zurückgebaut, siehe TS-79“ kennzeichnen statt löschen (Vorbild TS-71-Rückbau-Vermerk). NICHT anfassen: `CascadeDonePreview`/`CascadeDone` (Z. 488-541, Fertigmeldungs-Kaskade, betrifft `IsDoneBde`, nicht `IsReleasedForPicking`). `CanManagePickingRelease` bleibt unverändert (wird an anderer Stelle weiter genutzt)."
  - "secondbrain/specs/freigegeben/2026-09-10-fa-liste-ausbau-matchcode-spec.md (HAUPTCHECKOUT, Brain-Update-Pflicht des Dev-Laufs, NICHT dieser Spec-Agent-Lauf) — Block 4/Tasks 12-14 als durch diese Spec überholt/zurückgebaut kennzeichnen."
  - "IdealAkeWms/Views/Help/Index.cshtml, Abschnitt „Leitstand (Kommissionier-Freigabe)“ (ab Z. 695) — ergänzen: „Freigabe und Stückliste sind nur an der HauptFA-Zeile möglich, Sub-FA-Zeilen zeigen diese Bedienelemente nicht mehr“ sowie ein Hinweis auf die Schaltfläche „Alle Ziele“ im Komm.-Ziel-Filter der Stückliste."
  - "secondbrain/architektur/fallstricke.md — neuer Eintrag: `Bom.cshtml` Z. 256 (`<td>@item.Kommissionieren</td>`) darf keinen Platzhalter (z. B. „–“) für leere Werte bekommen, sonst kippt sowohl die client-seitige Relevanzprüfung als auch der `!(leer)`-Sonderfall in `bomMatchesFilter` still (die Zelle gilt dann nie mehr als leer)."
  - "docs/TESTSZENARIEN.md (neues Kapitel TS-79) + secondbrain/tests/testszenarien-index.md (Eintrag TS-79)."
  - "IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs (Version) + Views/Help/Changelog.cshtml (neuer Eintrag)."
  - "Neue Backlog-Notiz (vom Orchestrator anzulegen, nicht Teil dieser Spec): [[2026-09-25-picking-warteschlange-gruppierung-rueckbau]] — Zwei-Ebenen-Gruppierung der Kommissionierer-Warteschlange (`PickingController.cs` Z. 199ff) wird durch diese Spec funktional überflüssig (H1)."
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "Kommissionierung nur am HauptFA. Alle Ziele als Bedeutungswert !(leer) im bestehenden Komm.-Ziel-Dropdown (keine Checkbox, keine Migration), beim Speichern normalisiert, Hinweis nur bei verworfenen Einzelzielen. Eine Sub-FA-Definition (ProductionOrder.IsHauptFa) fuer Abfragen, Controller und Views. Freigabe-Kaskade zurueckgebaut, Warteschlange nur HauptFAs, SQL-Bereinigung nur Testsystem. Keine Standorteinstellung."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-09-25
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> [!info] Umsetzungsort (Hinweis, keine Frontmatter-Vorgabe)
> Der gesamte betroffene Code (`FaHierarchyKommissionierListenController`, `KommissionierListenService`,
> `PickingController`, `PickingLeitstandController`, `Bom.cshtml` usw.) existiert **nur** im nicht
> gemergten Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch
> `feature/2026-08-07-ideal-teile-1-5`, aktueller Stand bei Spec-Überarbeitung: `AppVersion 1.45.0`,
> höchstes Testszenarien-Kapitel `TS-78`, höchste SQL-Nummer `93`). Im `main`-Checkout, in dem dieser
> Spec-Agent-Lauf ausgeführt wurde, existiert davon **nichts**. Die Umsetzung erfolgt dort als weitere
> Etappe desselben Bündels — kein neuer Worktree. Der Dev-Lauf verifiziert AppVersion/TS-Nummer/
> SQL-Nummer gegen den dann aktuellen Worktree-Stand, bevor er sie festschreibt.

## Ziel / Nutzen (das Warum)

[[2026-09-13-kommissionierung-nur-hauptfa]] (Erweiterung 2026-09-23) macht die bisher offene
Scope-Frage endgültig: Kommissioniert wird **ausschließlich am HauptFA**, inklusive der kompletten
Stücklisten-Vollstruktur (Ruling 4 bleibt, Rückbau entfällt). Das löst den doppelten Materialzug
auf und macht eine bereits gebaute, aber jetzt gegenstandslose Freigabe-Kaskade auf Sub-FA-Ebene
überflüssig — und wirft die Frage auf, ob die Kommissionierliste ihre Positionen nach fest
codierten Regeln aussiebt, und ob dieselbe Regel auch der Stückliste hilft, aus der jetzt langen
Vollstruktur nur die wirklich zu holenden Positionen herauszufiltern. Diese Spec zieht vier
zusammenhängende Fäden zusammen, die alle aus derselben Entscheidung folgen:

1. Die Kommissionierliste (Teil 3) und die Stückliste (`/Picking`) sollen **dieselbe** Regel
   verwenden, statt zwei Implementierungen, die auseinanderlaufen können.
2. Eine bereits gebaute, aber durch die Entscheidung sinnlos gewordene Freigabe-Kaskade wird
   zurückgebaut, bevor sie in den Merge des Bündels geht.
3. Freigabe und interaktive Kommissionierung werden **strukturell** auf die HauptFA-Zeile
   beschränkt — ein Bedienelement, das nichts (mehr) bewirkt, verschwindet.
4. Die Druck-Whitelist-Erweiterung (Ebene/Komm.-Ziel), die der Backlog als „vor dem Merge fällig“
   markiert hatte, ist bereits durch [[2026-09-18-stueckliste-kommissionierziel-filter-spec]]
   (v1.42.0, Testbereit) erledigt — hier nur bestätigt, nicht erneut gebaut.

**Überarbeitung 2026-09-25 (nach kritischer Prüfung, alle Rückfragen beantwortet):** Der ursprüngliche
Entwurf hatte einen fünften, eigenständigen Checkbox-Filter „Nur kommissionierrelevant“ vorgesehen.
Der Mensch hat stattdessen die günstigere Alternative (S4 der kritischen Prüfung) gewählt: „Alle
Ziele“ als neue, exklusive Option im bereits bestehenden Komm.-Ziel-Dropdown. Diese Spec übernimmt
diese Entscheidung vollständig; ein eigener Checkbox-Filter wird **nicht** gebaut.

**Überarbeitung 2026-09-25, zweiter Prüfdurchgang:** Das Nachziehen der ersten Antworten hatte vier
eigene Risse (B3, S9-S13) — keine neuen Grundsatzfragen, sondern Ungenauigkeiten beim Umsetzen der
bereits getroffenen Entscheidungen. Der Mensch hat alle beantwortet (siehe „ANTWORTEN auf den zweiten
Prüfdurchgang“ am Dateiende); diese Spec zieht sie vollständig in Umfang, Anforderungen, Lösungsentwurf,
SQL, Akzeptanzkriterien und Testszenarien nach, ohne eine Entscheidung neu zu öffnen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

1. **Erhebung + Korrektur der Backlog-Annahme:** Die Kommissionierliste filtert heute
   ausschließlich nach EINEM Kriterium — Feld `Kommissionieren` nicht leer
   (`KommissionierListenService.BuildFlagPredicate`, Z. 228-236). SubFA-Filterung wurde am
   2026-08-13 (Nachtrag 1, `leafOnly:false`) explizit entfernt; Beschaffungsartikel/Artikelgruppe
   spielen für die Relevanz **keine** Rolle (sie existieren nur als eigenständige, vom Anwender
   frei wählbare Spaltenfilter, `ColumnMap`, nicht als Bestandteil der Grundmenge).
2. **Die Relevanzregel wird an EINER Stelle definiert** (`KommissionierRelevanzFilter.IsRelevant`)
   und ausschließlich von der Kommissionierliste genutzt (Antwort 2: ein einziges Kriterium
   rechtfertigt heute nur einen Verbraucher — die Extraktion ist trotzdem sinnvoll als Ort für
   künftige Zusatzkriterien). Die Stückliste verwendet **keine** eigene Regel mehr: Sie nutzt den
   bereits bestehenden Komm.-Ziel-Filter mit dem neuen Bedeutungswert „Alle Ziele“ (Punkt 3).
3. **„Alle Ziele“ im bestehenden Komm.-Ziel-Filter der Stückliste** (`/Picking`, nur
   `Model.Hierarchical`): Eine neue, exklusive Option im Select2-Dropdown
   (`setupKommissionierzielDropdown`) setzt den Bedeutungswert `!(leer)` = „Ziel ist nicht leer“ in
   das bestehende, versteckte `<input data-col-key="kommissionieren">` — keine eigene Checkbox,
   kein eigener Badge-Zweig, kein eigener Leerzustand. Badge, Reset, rekursive Suche,
   Vorfahren-Aufklappen, Druckhinweis und Persistenz je Benutzer laufen über den bestehenden
   Mechanismus aus [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] — nur die drei Stellen,
   die den Rohwert `!(leer)` anzeigen (Badge, Leerzustand, Druck), brauchen eine Textabbildung, und
   `bomMatchesFilter` braucht einen Sonderfall, damit die heutige Mini-Syntax `!(leer)` nicht als
   Ausschluss liest. **Persistenz-Normalisierung** (zweiter Prüfdurchgang B3): Speichert ein
   Benutzer im Profil oder ein Admin in der Benutzerverwaltung einen Freitext, der `!(leer)`
   (unabhängig von Groß-/Kleinschreibung) neben Einzelzielen enthält oder nur anders geschrieben
   ist, wird beim Speichern auf exakt `!(leer)` normalisiert; ein sichtbarer Hinweis erscheint, wenn dabei Einzelziele verworfen wurden (nicht
   bei reiner Schreibweisen-Korrektur) —
   siehe Anforderung 3 und Technischer Lösungsentwurf.
4. **Freigabe-Kaskade zurückbauen** (Block 4/Tasks 12-14 aus
   [[2026-09-10-fa-liste-ausbau-matchcode-spec]], Testbereit, noch nicht gemergt): Sie gäbe
   Sub-FAs frei, die künftig nie kommissioniert werden — ein Zustand ohne Bedeutung.
5. **Freigabe- und interaktiver Kommissionierpfad strukturell auf die HauptFA-Zeile beschränkt**:
   Zeilen-Toggle (`ToggleRelease`), Mehrfachauswahl (`BulkRelease`), die interaktive
   Stückliste-Aktion (`Picking/Bom`) — sowohl das Bedienelement in der UI als auch ein
   serverseitiger Guard (Verteidigung in der Tiefe, „Melden statt still behandeln“, nur an den
   Einstiegen, siehe Out-of-Scope).
6. **Warteschlangenkorrektur** (Antwort 3, zweiter Prüfdurchgang S9/Punkt 1 verfeinert):
   `ProductionOrder.IsHauptFa` als EINE gemeinsame `Expression<Func<ProductionOrder,bool>>`, aus der
   `IsSubFa` kompiliert wird — statt einer dritten, inline kopierten SQL-Formel. Genutzt von allen
   VIER Warteschlangen-Abfragen sowie `Bom`/`ToggleRelease`/`SetReleaseBatchAsync`. Einmaliger,
   korrigierter SQL-Lauf nur im Testsystem (Deploy-Abschnitt).
7. Hilfeseite (Picking/Leitstand-Abschnitt) und Testszenarien (neues Kapitel TS-79) inkl.
   Rückbau-Markierung in TS-73.

**Out-of-Scope**

- **Druck-Whitelist-Erweiterung (Ebene, Komm.-Ziel) in `PrintBom.cshtml`** — bereits umgesetzt durch
  [[2026-09-18-stueckliste-kommissionierziel-filter-spec]] (v1.42.0, verifiziert:
  `Views/Picking/PrintBom.cshtml` Z. 207-259 enthält bereits `ShowCol("kommissionieren")`,
  `ShowCol("hauptlagerplatz")`, `ShowCol("ebene")`). Diese Spec fügt lediglich eine Textabbildung im
  `filterInfo`-Parameter hinzu, wenn „Alle Ziele“ aktiv ist — keine neue Spalte, kein neuer
  `ShowCol`-Block.
- **BDE-Rückmeldung (Teil 8)** — bleibt unverändert auf Sub-FA-Ebene, nicht Gegenstand dieser Spec.
  Die `ProductionOrderPickingStatus`-Zeile der Sub-FAs wird **nicht** zurückgebaut (trägt weiterhin
  Glas/Fremdbezug/Lackierung, siehe [[2026-09-09-materialisierung-ohne-statuszeilen-bug]]).
- **Beschichtungsauftrag/Vormontage-Listen (Teil 4/5)** — gruppieren nach Arbeitsbereich, nicht
  nach FA-Ebene, unberührt.
- **`FaWorklist/Bom` (read-only Stücklisten-Referenz für BDE-Werker)** — bleibt auf jeder Sub-FA-
  Zeile sichtbar. Rückmeldung/Referenz ist nicht Kommissionierung.
- **AKE** — jede Änderung dieser Spec ist entweder rein datengetrieben (Sub-FA-Guard über
  `IsSubFa`, bei AKE wegen der Invarianz `SubOrderNumber == OrderNumber` immer falsch) oder hinter
  `Model.Hierarchical`/`@if (Model.Hierarchical)` gated — für AKE folgenlos, keine gesonderte
  Prüfung nötig, aber Testszenarien decken die Nicht-Wirkung ausdrücklich ab. Die Komm.-Ziel-Spalte
  existiert im AKE-Flachmodus ohnehin nicht, also auch kein Dropdown, also auch keine Option „Alle
  Ziele“.
- **Kombinationsgeräte** — unverändert außen vor (paketweit entschieden, siehe Materialisierungs-
  Spec).
- **Standorteinstellungen-Schalter** — wird bewusst **nicht** gebaut (Antwort 2). Mit nur einem
  Kriterium wäre der Schalter das Ausschalten der Kommissionierliste selbst, und sie summierte dann
  nach leerem Ziel, wofür sie nicht gebaut ist (H2 der kritischen Prüfung). Lohnt sich erst mit
  einem zweiten, echten Konfigurationswert.
- **Folgeaktionen jenseits der Einstiege** — gesperrt werden ausschließlich die Einstiege
  (`Bom`, `ToggleRelease`, `SetReleaseBatchAsync`). `PrintBom`, `PrintPicking`, `TransferPicked`,
  `SetPickingStatus`, `ToggleDone` (`PickingController`) sowie `SetPriority`/
  `ChangeAssignedPicker` (`PickingLeitstandController`) bleiben auf Sub-FA-Ids per direkter
  URL/POST erreichbar — bewusst, kein Anspruch auf Verteidigung in der Tiefe für jede Action, weil
  diese Aktionen ohne den gesperrten Einstieg fachlich nicht erreicht werden.
- **Zwei-Ebenen-Gruppierung der Kommissionierer-Warteschlange** (`PickingController.cs` Z. 199ff)
  wird durch diese Spec funktional überflüssig (jede Gruppe besteht künftig aus genau einer
  HauptFA-Zeile), der Rückbau ist aber **nicht** Teil dieser Spec — eigene Backlog-Notiz
  [[2026-09-25-picking-warteschlange-gruppierung-rueckbau]] (H1).

**Größenschätzung (aktualisiert nach zweitem Prüfdurchgang, H9):** Gegenüber der ersten Schätzung
(H3: ca. 14-16 Dateien in drei Schichten) kommt EINE neue Datei dazu (`KommissionierzielFilterWert.cs`,
Normalisierung), plus punktuelle Ergänzungen an bereits ohnehin geänderten Dateien (`AccountController`,
`UsersController`, `ProductionOrder.cs`, `ProductionOrderPickingStatusRepository.cs`, `Bom.cshtml`)
und ca. drei neue Testfälle (Normalisierungsfunktion, `IsHauptFa`/`IsSubFa`, Vier-Fixture-InMemory-Test
der Warteschlangen-Abfragen). Keine neue Schicht, keine neue Migration — weiterhin in einem Dev-Lauf
machbar, keine Aufteilung nötig.

## Fachliche Anforderungen

### 1 — Gemeinsame Kommissionier-Relevanzregel (nur Kommissionierliste)

Die Kommissionierliste (`KommissionierListenService.BuildFlagPredicate`) prüft heute inline
`!string.IsNullOrWhiteSpace(n.Kommissionieren)`. Diese Prüfung wandert unverändert (keine
Verhaltensänderung) in eine neue, benannte, pure Methode `KommissionierRelevanzFilter.IsRelevant`.
`BuildFlagPredicate` ruft diese Methode auf, statt die Prüfung erneut zu schreiben. Heute hat die
Methode genau einen Verbraucher (Antwort 2) — die Extraktion ist trotzdem der richtige Ort für ein
künftiges zweites Kriterium, nicht Attrappe für ein „geteiltes“ Verhalten, das es zwischen
Kommissionierliste und Stückliste nicht mehr gibt (die Stückliste bekommt keine eigene
Relevanzregel mehr, siehe Anforderung 3).

Der Ziel-Wert-Einschränkung (`targetValue`-Parameter des Dropdowns in der Kommissionierliste) bleibt
davon unberührt — sie ist eine zusätzliche, vom Anwender gewählte Einschränkung, keine Änderung der
Grundregel.

### 2 — Kein Standorteinstellungen-Schalter

Die Erhebung ergibt: Es gibt für die Kommissionierliste nur diesen einen Parameter, und er ist kein
Wert, sondern eine feste Feldsemantik (das Feld `Kommissionieren` kommt 1:1 aus der Sage-View und
ist nicht sinnvoll gegen ein anderes Feld austauschbar). Ein Standorteinstellungen-Eintrag ohne
echten Einstellwert wäre eine Attrappe (ponytail Sprosse 1). Diese Spec baut deshalb **keinen**
Schalter, keinen `AppSetting`-Key, keinen Standorteinstellungen-Eintrag — nur die Extraktion aus
Anforderung 1. Die Regel bleibt aber an einer eigenen Stelle (`KommissionierRelevanzFilter`), an der
ein künftiges zweites Kriterium anknüpfen könnte; **dann** lohnt eine Einstellung.

### 3 — „Alle Ziele“ im bestehenden Komm.-Ziel-Filter der Stückliste

Statt eines fünften, eigenständigen Standardfilters wird die bestehende Option „Nur
kommissionierrelevant“ als **Bedeutungswert innerhalb des Komm.-Ziel-Filters** abgebildet — fachlich
ist „nur kommissionierrelevant“ dasselbe wie „alle Ziele ausgewählt“, und der bestehende Filter
bringt Badge, Reset, Leerzustand, rekursive Suche, Vorfahren-Aufklappen, Druckhinweis und
Speicherung je Benutzer bereits mit (S4 der kritischen Prüfung, vom Menschen übernommen):

- **Neue, erste Option** im Select2-Dropdown (`setupKommissionierzielDropdown`,
  `Views/Picking/Bom.cshtml` Z. 1049-1083): Text „Alle Ziele (nur kommissionier-relevant)“, Wert
  `!(leer)`. Gewählt, deselektiert sie automatisch alle konkreten Ziel-Werte und umgekehrt — sonst
  entstünde ein kombinierter Wert wie `!(leer),KA-02`, der die bestehende Ausschluss-Syntax in
  `bomMatchesFilter` fälschlich auslöst.
- **Bedeutungswert statt Momentaufnahme:** `!(leer)` bedeutet „Komm.-Ziel-Zelle ist nicht leer“, KEINE
  gespeicherte Werteliste der zum Zeitpunkt der Auswahl existierenden Ziele. Ein später neu
  angelegtes Kommissionierziel ist damit automatisch mit erfasst — der Nachteil einer
  Momentaufnahme (still ausgeblendete Positionen eines neuen Ziels bei jedem, der sich auf seinen
  gespeicherten Standard verlässt) entfällt dadurch bewusst.
- **Schreibweise `!(leer)`** — identisch zur künftigen Suchsyntax
  ([[2026-09-23-suchsyntax-spaltenfilter-erweitern]]), damit später keine zwei Notationen für
  dasselbe existieren. Bis die Suchsyntax gebaut ist, behandelt `bomMatchesFilter` diesen Wert
  **gesondert** (ein Sonderfall am Anfang der Funktion, siehe Technischer Lösungsentwurf) — die
  heutige Mini-Syntax würde `!(leer)` sonst als „enthält nicht '(leer)'“ lesen und ALLES anzeigen.
- **Badge, Leerzustand, Druckhinweis** zeigen bei diesem Wert den Text „Alle Ziele (nur
  kommissionier-relevant)“ statt des Rohwerts `!(leer)` bzw. statt einer Werteliste — case-insensitiv
  erkannt, weil `bomFilterInputValue` (anders als `getActiveFilters`) den Rohwert liefert (zweiter
  Prüfdurchgang).
- **Persistenz je Benutzer** läuft über das bereits bestehende `DefaultFilterBomKommissionierziel`
  (kein neues Feld, keine Migration). Ein Kommissionierer, der „Alle Ziele“ als seinen Standard
  setzt, speichert damit den Wert `!(leer)` in genau diesem Feld. Ein globaler Standard wird nicht
  gebaut.
- **Nur im hierarchischen Modus**, da die Spalte `kommissionieren` im AKE-Flachmodus gar nicht
  existiert — dasselbe Gate wie beim bisherigen Komm.-Ziel-Dropdown.
- **Normalisierung beim Speichern** (zweiter Prüfdurchgang B3): Das Profil-/Benutzerverwaltungs-Feld
  ist Freitext. Enthält der eingegebene Wert `!(leer)` (unabhängig von Groß-/Kleinschreibung), z. B.
  kombiniert mit Einzelzielen (`!(leer),KA-02`) oder nur andersgeschrieben (`!(LEER)`), wird beim
  Speichern auf exakt `!(leer)` normalisiert, Einzelziele entfallen. **Werden dabei Einzelziele verworfen**, macht
  eine `WarningMessage` das sichtbar; bei reiner Schreibweisen-Korrektur (`!(LEER)`) gibt es **keinen**
  Hinweis, weil sich die Bedeutung nicht aendert (Freigabe-Nachtrag). Beides
  laeuft ueber (`KommissionierzielFilterWert.Normalize`, siehe Technischer Lösungsentwurf und
  affected_code). Ohne diese Normalisierung würde die bestehende Ausschluss-Syntax in
  `bomMatchesFilter` einen kombinierten Wert wie `!(leer),KA-02` als „zeige alles außer '(leer)' oder
  'ka-02'“ lesen — das Gegenteil des Gewollten. Gilt für Profil UND Benutzerverwaltung (Create UND
  Edit).

### 4 — Rückbau der Freigabe-Kaskade

[[2026-09-10-fa-liste-ausbau-matchcode-spec]] Block 4 (Tasks 12-14) ist bereits gebaut, Status
`Testbereit`, **noch nicht gemergt** (verifiziert: `CascadeReleasePreview`/`CascadeRelease` in
`PickingLeitstandController.cs` Z. 543-616, Button+Modal in `Views/PickingLeitstand/Index.cshtml`,
Repository-Methode `SetReleaseForOrderNumberAsync`). Mit der Entscheidung „Kommissionierung nur am
HauptFA“ gibt sie Sub-FAs frei, die nie kommissioniert werden — ein Zustand ohne fachliche Bedeutung.

Vollständig zu entfernen: Controller-Actions, Repository-Methoden (`SetReleaseForOrderNumberAsync`,
`CountReleasedByOrderNumberAsync` — verifiziert ohne Fremdverwendung außerhalb der Kaskade und
eigener Tests), View-Button/-Modal, zugehörige Tests. **Nicht** zu entfernen: die
Fertigmeldungs-Kaskade `CascadeDonePreview`/`CascadeDone` (Teil 7/8, betrifft `IsDoneBde`, ein
anderer Sachverhalt). Der Zeilen-Toggle `ToggleRelease` und die Mehrfachauswahl `BulkRelease`
bleiben bestehen, werden aber durch Anforderung 5 auf HauptFA-Zeilen beschränkt.

Der Rückbau muss zusätzlich `Views/Help/Changelog.cshtml` (v1.38.0-Eintrag Z. 180 + Z. 203-207) als
zurückgenommen kennzeichnen — sonst haben Anwender die Kaskade weiter als gültige Funktion vor
Augen — und `docs/TESTSZENARIEN.md` vollständig mitziehen (TS-73-Titel Z. 7726, Z. 7753,
TS-73.11-13, Z. 7895, Z. 7940-7952, Schlusszeile Z. 8575), nicht nur die Kaskade-Einzelszenarien.
Vorbild ist der TS-71-Rückbau-Vermerk aus der ADR-0014-Korrektur (v1.43.0): kennzeichnen, nicht
löschen.

### 5 — Freigabe- und Picking-Pfad strukturell auf die HauptFA-Zeile beschränkt

Backlog-Frage 2 („Verschwindet der Freigabe-Knopf an Sub-FA-Zeilen?“) wird mit **ja** beantwortet.
Backlog-Frage 3 („Gilt das auch für AKE?“) wird mit **ja, aber folgenlos** beantwortet: Alle
Sperren dieser Spec sind entweder rein datengetrieben über `ProductionOrder.IsSubFa` (bei AKE immer
`false`, Fallstricke §9) oder zusätzlich hinter `Model.Hierarchical` gated — kein
Master-Switch-Read nötig, keine AKE-Sonderbehandlung.

Konkret, doppelt abgesichert (UI **und** Server — „Melden statt still behandeln“, ein Element, das
nichts bewirkt, verschwindet; ein direkter POST auf eine gesperrte Aktion wird abgewiesen, nicht
still ignoriert):

- **UI (Leitstand + FA-Liste):** Freigabe-Formular, Prioritäts-Input, Bulk-Checkbox und der
  **interaktive** Stückliste-Link (`Picking/Bom`) erscheinen auf Sub-FA-Zeilen im hierarchischen
  Modus nicht mehr. Die HauptFA-Zeile trägt sie unverändert.
- **Server:** `PickingController.Bom` und `PickingLeitstandController.ToggleRelease` weisen eine
  Sub-FA-Zeile mit einer sichtbaren `WarningMessage` zurück, statt sie stillschweigend zu
  verarbeiten. `ProductionOrderPickingStatusRepository.SetReleaseBatchAsync` überspringt Sub-FA-Ids
  in einer gemischten Bulk-Auswahl und meldet sie sichtbar — in **beiden** Richtungen (Freigeben
  und Zurücknehmen), konsistent zum `ToggleRelease`-Guard.
- **Umfang der Sperre bewusst begrenzt** (Out-of-Scope): Nur die Einstiege sind gesperrt.
  Folgeaktionen (`PrintBom`, `PrintPicking`, `TransferPicked`, `SetPickingStatus`, `ToggleDone`,
  `SetPriority`, `ChangeAssignedPicker`) bleiben auf Sub-FA-Ids per direkter URL/POST erreichbar,
  weil sie ohne den gesperrten Einstieg fachlich nicht erreicht werden — kein Anspruch auf
  Verteidigung in der Tiefe für jede einzelne Action.
- **Unverändert:** die read-only Stückliste (`FaWorklist/Bom`) für BDE-Referenz auf jeder Sub-FA-
  Zeile, sowie die Sichtbarkeit/Existenz der `ProductionOrderPickingStatus`-Zeile selbst (trägt
  weiterhin Glas/Fremdbezug/Lackierung-Flags unabhängig von der Freigabe).

### 6 — Eine Sub-FA-Formel statt dreier (bzw. vierer), und Bereinigung der Warteschlange

Die erste kritische Prüfung fand drei unterschiedliche, semantisch teils abweichende
Sub-FA-Erkennungen im geplanten Code; der zweite Prüfdurchgang fand die Formel dann sogar vierfach
kopiert, weil die vier Warteschlangen-Abfragen nicht auf derselben Wurzel liefen (S9). Diese Spec
schreibt deshalb EINE Definition fest, als `Expression<Func<ProductionOrder,bool>>`, nicht als reine
Instanz-Eigenschaft:

```csharp
public static readonly Expression<Func<ProductionOrder,bool>> IsHauptFa =
    p => p.SubOrderNumber == "" || p.SubOrderNumber == p.OrderNumber;
private static readonly Func<ProductionOrder,bool> IsHauptFaCompiled = IsHauptFa.Compile();
[NotMapped] public bool IsSubFa => !IsHauptFaCompiled(this);
```

`IsHauptFa` ist damit die einzige Quelle: `Bom`, `ToggleRelease` und `SetReleaseBatchAsync` lesen
`IsSubFa` auf dem bereits geladenen Objekt; alle VIER Warteschlangen-Abfragen
(`GetReleasedForPickingAsync`, `GetReleasedForPickingByPickerAsync`, `GetReleasedForPickingCountAsync`,
`GetMaxPickingPriorityAsync`) filtern mit `.Where(ProductionOrder.IsHauptFa)` auf derselben Wurzel
`_context.ProductionOrders` (die beiden zuletzt genannten liefen bisher auf
`_context.ProductionOrderPickingStatuses` und werden dafür umgeschrieben, 1:1-Beziehung über den
UNIQUE-Index belegt, siehe Technischer Lösungsentwurf). Unit-Test deckt `""`, `Sub == Order`,
`Sub != Order` ab; `null` ist bewusst kein Testfall (Spalte `NOT NULL`, kein Schreibpfad erzeugt
`null`).

Die ursprüngliche Einschätzung zu Rückfrage 3 („bereits freigegebene Sub-FAs im Testsystem stehen
lassen, folgenlos“) war am Code falsch (B2 der ersten kritischen Prüfung): Eine freigegebene Sub-FA
bliebe sonst in der Kommissionierer-Warteschlange sichtbar, zählte in der Home-Kennzahl mit und wäre
über die UI nicht mehr zurücknehmbar (Stückliste gesperrt, Bulk-Checkbox ausgeblendet). Korrigierte
Entscheidung (Freigabe-Antwort 3, SQL-Text korrigiert im zweiten Prüfdurchgang S10/Punkt 5): Die
vier Warteschlangen-Abfragen filtern Sub-FAs strukturell aus (über `ProductionOrder.IsHauptFa`),
zusätzlich ein einmaliger, wörtlich negierter SQL-Lauf nur im Testsystem, der bestehende Altlasten
bereinigt (siehe Migrations-/SQL-Auswirkungen und Deploy). Damit ist die Warteschlange auch dann
korrekt, wenn der SQL-Lauf einmal vergessen wird.

**Grenze:** Ein EF-InMemory-Test belegt nur die C#-Semantik von `IsHauptFa`. SQL Server vergleicht
in der Standard-Collation ohne Groß-/Kleinschreibung und ignoriert Leerzeichen am Ende
(`'123 ' = '123'`); die in-memory ausgewertete Eigenschaft und die serverseitig übersetzte Expression
können deshalb für Werte auseinanderlaufen, die sich nur darin unterscheiden. Praktisch folgenlos,
weil `OrderNumber` und `SubOrderNumber` bei jeder Zeile aus demselben Ursprungswert stammen
(AKE-Sync `@OrderNumber,@OrderNumber` in `SageProductionOrderSql.cs:17`, IDEAL-Materialisierung
`SubFA`/`HauptFA` aus derselben Quelle).

## Technischer Lösungsentwurf

**Muster:** Repository-Pattern (ADR 0001), Listen-View-Pattern mit Client-Mode-Ausnahme für
`Bom.cshtml` (ADR 0005 Design F, wie bereits in [[2026-09-18-stueckliste-kommissionierziel-filter-spec]]
etabliert).

- **Geteilte Regel, ein Verbraucher:** `KommissionierRelevanzFilter.IsRelevant` ersetzt die
  Inline-Prüfung in `KommissionierListenService.BuildFlagPredicate`. Die Stückliste bekommt **keine**
  eigene Relevanzregel mehr — sie nutzt stattdessen den bestehenden Komm.-Ziel-Filter mit dem neuen
  Bedeutungswert `!(leer)` (Anforderung 3). Datenquelle ist an beiden Stellen identisch:
  `FaHierarchyBomRepository.cs:186` (`Kommissionieren = node.Kommissionieren`) bzw. dasselbe Feld
  über `KommissionierListenService`.
- **„Alle Ziele“-Option im Select2:** `setupKommissionierzielDropdown()` fügt vor den
  distinct-ermittelten Ziel-Werten eine zusätzliche `<option value="!(leer)">` ein. Der
  `change`-Handler erzwingt Exklusivität: Wird `!(leer)` neu in `$sel.val()` aufgenommen, werden alle
  anderen Werte entfernt; wird ein anderer Wert neu gewählt, wird `!(leer)` entfernt (kleinste
  Variante: Vergleich der Vorher-/Nachher-Auswahl im `change`-Handler, kein zusätzlicher Knopf neben
  dem Dropdown — ponytail Sprosse 6, eine zusätzliche Option ist die kürzere Lösung als ein separates
  UI-Element mit eigener Synchronisationslogik zum Dropdown). Die Vorbelegung (`pre`, Z. 1065) bildet
  einen case-insensitiv erkannten `!(leer)`-Teil auf den exakten Options-Wert ab, bevor `$sel.val(pre)`
  aufgerufen wird (zweiter Prüfdurchgang, Nebeneffekt behebt H8).
- **`bomMatchesFilter`-Sonderfall:** EIN zusätzlicher `if`-Zweig am Anfang der Funktion (Z. 830-837):
  `val` kommt bereits kleingeschrieben aus `window.getActiveFilters()`; ist der getrimmte Gesamtwert
  so `!(leer)`, wird `text.trim() !== ''` zurückgegeben, statt der bestehenden Ausschluss-Logik zu
  folgen. Dieser eine Sonderfall genügt für alle drei Aufrufer (`updateBomVisibility`,
  `expandAncestorsOfMatching`, `checkKommissionierzielEmptyState`), weil sie alle über
  `bomMatchesFilter` laufen — keine dieser drei Funktionen muss selbst geändert werden. Das ist
  zugleich ein bewusster Zwischenstand: `bomMatchesFilter` ist bereits die vierte eigenständige
  Umsetzung der Filter-Mini-Syntax neben `table-filter.js`, `ColumnFilterHelper.Apply` und den
  SQL-Ausdrücken je Liste — [[2026-09-23-suchsyntax-spaltenfilter-erweitern]] kennt diese vierte
  Stelle noch nicht. Beim Bau der Suchsyntax muss `bomMatchesFilter` (inklusive dieses
  `!(leer)`-Sonderfalls) dort aufgehen, sonst bleiben zwei Implementierungen bestehen.
- **Anzeige-Textabbildungen:** `renderDefaultFilterBadges` und `checkKommissionierzielEmptyState`
  lesen den Wert über `bomFilterInputValue`, die anders als `getActiveFilters` NICHT kleinschreibt —
  ihr Sonderfall für `!(leer)` vergleicht deshalb ausdrücklich case-insensitiv
  (`val.trim().toLowerCase() === '!(leer)'`, zweiter Prüfdurchgang), Anzeigetext dann „Alle Ziele
  (nur kommissionier-relevant)“ statt Rohwert bzw. Werteliste.
- **Sub-FA-Formel — eine Definition, vier Verbraucher-Abfragen** (zweiter Prüfdurchgang S9/Punkt 1):
  `ProductionOrder.IsHauptFa` (`Expression<Func<ProductionOrder,bool>>`) ist die einzige Quelle;
  `IsSubFa` ([NotMapped]) wird aus der kompilierten Form abgeleitet. `Bom` und `ToggleRelease` lesen
  `order.IsSubFa` direkt auf dem bereits geladenen `ProductionOrder` (kein neuer Query). Alle VIER
  Warteschlangen-Abfragen nutzen `.Where(ProductionOrder.IsHauptFa)` gegen SQL Server; `Count`/`Max`
  werden dafür von der Navigation `_context.ProductionOrderPickingStatuses` auf die Wurzel
  `_context.ProductionOrders` umgestellt (1:1-Beziehung über den UNIQUE-Index `ProductionOrderId`
  belegt), mit demselben Prädikat, das `GetReleasedForPickingAsync` heute schon benutzt. Kein
  Kommentar „gleichwertig zu IsSubFa“ mehr nötig, weil es keine zweite, separat gepflegte Formel mehr
  gibt. **Grenze:** InMemory-Tests belegen nur die C#-Semantik; SQL Server vergleicht ohne
  Groß-/Kleinschreibung und ignoriert Leerzeichen am Ende — praktisch folgenlos, weil beide Spalten
  aus demselben Ursprungswert stammen (siehe Anforderung 6).
- **`SetReleaseBatchAsync`:** neuer Skip-Zweig für `row.ProductionOrder.IsSubFa`, VOR dem bestehenden
  `SkippedNoArticle`-Zweig (Z. 246-250) und unabhängig von `release` — Sub-FA wird in Bulk in beiden
  Richtungen übersprungen. Neues Feld `BulkReleaseResult.SkippedSubFa`. Kein neuer Repository-Aufruf
  im Controller nötig, weil die Methode `.Include(s => s.ProductionOrder)` bereits fährt.
- **Normalisierung beim Speichern** (zweiter Prüfdurchgang B3): `KommissionierzielFilterWert.Normalize(...)`
  ersetzt die bisherigen Inline-`Trim()`-Aufrufe in `AccountController` und `UsersController` (Profil,
  Create, Edit). Erkennt `!(leer)` case-insensitiv, auch kombiniert mit Einzelzielen, und normalisiert
  auf den exakten kanonischen Wert; nur wenn dabei Einzelziele verworfen wurden (nicht bei reiner Schreibweisen-Korrektur) zusätzlich
  `TempData["WarningMessage"]` neben dem bestehenden `SuccessMessage`. Dieselbe case-insensitive
  Erkennung läuft zusätzlich, unabhängig von der Normalisierung, an den JS-Stellen (Select2-Vorbelegung,
  Badge, Leerzustand) — Verteidigung in der Tiefe für Altdaten und den Fall, dass die serverseitige
  Normalisierung übersprungen wird (z. B. direkt in der DB gepflegte Werte).
- **Druck-Leerzustand statt leerem oder irreführendem Ausdruck** (zweiter Prüfdurchgang S11/Punkt 4):
  Ist beim Klick auf „Drucken“ keine Zeile sichtbar, öffnet `btnPrintBom` keinen neuen Tab. Stattdessen
  zeigt er die bestehende Box `#bomKzEmptyState` mit dem Text „Keine sichtbaren Positionen — nichts zu
  drucken.“ — sonst würde `PrintBom` ein fehlendes `visiblePositions`-Feld als „kein Filter“ lesen und
  alle Positionen unter dem irreführenden Kopf „Alle Ziele (nur kommissionier-relevant)“ drucken.
  `PrintBom` selbst bleibt unverändert.
- **Harte Testbedingung (S1 der kritischen Prüfung, übernommen):** Die bestehenden
  `SetReleaseBatchAsync`-/`BulkRelease`-Tests laufen **ohne Änderung ihrer Fixtures** grün. Werden
  Fixtures angepasst, damit Tests grün werden, ist das ein Befund (Hinweis auf eine noch
  abweichende Formel irgendwo im Testaufbau), kein Erfolg — nicht stillschweigend „reparieren“.

## Migrations-/SQL-Auswirkungen

**Keine Migration.** „Alle Ziele“ nutzt das bereits bestehende Feld `DefaultFilterBomKommissionierziel`
(kein neues Feld, keine Migration 94 — entfällt vollständig gegenüber dem ursprünglichen Entwurf).
Der neue Standorteinstellungen-Schalter wird nicht gebaut (Antwort 2). Der Rückbau der
Freigabe-Kaskade entfernt nur C#-Code — keine Schema-Änderung. `ProductionOrder.IsSubFa`/`IsHauptFa`
sind `[NotMapped]`/reine Expression und erzeugen keine Spalte.

**Einmaliger SQL-Lauf, NUR Testsystem, kein `SQL/XX`-Skript** (Freigabe-Antwort 3, korrigiert im
zweiten Prüfdurchgang S10/Punkt 5 — wörtliche Negation der Warteschlangen-Bedingung, verifizierter
Tabellenname im Singular `ProductionOrderPickingStatus` gegen `ApplicationDbContext.cs:445`
(`ToTable("ProductionOrderPickingStatus")`), `ModifiedByWindows = SYSTEM_USER` statt `NULL` gemäß
Hausregel):

```sql
-- Vorab zaehlen (Zahl ins Testprotokoll)
SELECT COUNT(*)
FROM [dbo].[ProductionOrderPickingStatus] s
JOIN [dbo].[ProductionOrders] p ON p.Id = s.ProductionOrderId
WHERE NOT (p.SubOrderNumber = '' OR p.SubOrderNumber = p.OrderNumber)
  AND s.IsReleasedForPicking = 1;

UPDATE s
SET s.IsReleasedForPicking = 0,
    s.ModifiedAt = GETUTCDATE(),
    s.ModifiedBy = 'Testsystem-Bereinigung kommissionierung-nur-hauptfa',
    s.ModifiedByWindows = SYSTEM_USER
FROM [dbo].[ProductionOrderPickingStatus] s
JOIN [dbo].[ProductionOrders] p ON p.Id = s.ProductionOrderId
WHERE NOT (p.SubOrderNumber = '' OR p.SubOrderNumber = p.OrderNumber)
  AND s.IsReleasedForPicking = 1;
```

`WHERE NOT (...)` ist die wörtliche Umkehrung von `ProductionOrder.IsHauptFa`. Dieser Lauf ersetzt
keinen Code-Guard — er räumt nur bestehende Testdaten auf; die dauerhafte Korrektheit trägt
`.Where(ProductionOrder.IsHauptFa)` in den vier Warteschlangen-Abfragen.

## Audit-Feld-Auswirkungen

Keine neue fachliche Entität. `ProductionOrderPickingStatus` bleibt `AuditableEntity` — die
entfernten Kaskade-Methoden hatten dieselben Audit-Aufrufe wie `SetReleaseAsync`/
`SetReleaseBatchAsync`, deren Rückbau ändert an den verbleibenden Schreibpfaden nichts. Der neue
Sub-FA-Skip-Zweig in `SetReleaseBatchAsync` schreibt für übersprungene Zeilen **keine**
Audit-Felder (analog zum bestehenden `SkippedNoArticle`-Zweig — Skip heißt keine Änderung, also
keine Modified-Felder). Der einmalige Testsystem-SQL-Lauf setzt `ModifiedAt` (`GETUTCDATE()`),
`ModifiedBy` (fester Text) und `ModifiedByWindows` (`SYSTEM_USER`, korrigiert im zweiten
Prüfdurchgang statt `NULL`) explizit, wie in der Checkliste gefordert. Die neue Normalisierungsfunktion
`KommissionierzielFilterWert.Normalize` ändert nur den zu speichernden Wert vor dem bestehenden
Schreibpfad — `AccountController`/`UsersController` setzen `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows`
wie bisher, unverändert durch diese Spec.

## Akzeptanzkriterien

1. `KommissionierListenService.BuildFlagPredicate` liefert für dasselbe Eingabe-Set exakt dieselbe
   Positionsmenge wie vor dieser Spec (reine Extraktion, keine Verhaltensänderung) — durch
   bestehende Unit-Tests der Kommissionierliste nachgewiesen.
2. `KommissionierRelevanzFilter.IsRelevant` ist eine eigenständig unit-testbare, reine Methode ohne
   DB-/HTTP-Abhängigkeit, mit genau einem Verbraucher (`BuildFlagPredicate`).
3. In der hierarchischen Stückliste (`Model.Hierarchical == true`) enthält das Komm.-Ziel-Dropdown
   eine erste Option „Alle Ziele (nur kommissionier-relevant)“; gewählt blendet sie alle Positionen
   mit leerem `Kommissionieren`-Feld aus. **Ehrlich beschrieben (zweiter Prüfdurchgang, Korrektur zu
   AK 3/TS-79):** Eine Baugruppen-Zeile ohne eigenes Komm.-Ziel wird dabei **immer** ausgeblendet,
   auch wenn Kinder sichtbar bleiben — das ist das unveränderte, bestehende Verhalten des
   Komm.-Ziel-Filters (`updateBomVisibility` prüft jede Zeile einzeln), **kein Fehler**. Die Kinder
   bleiben über das bestehende Vorfahren-Aufklappen (`expandAncestorsOfMatching`) sichtbar, stehen
   dann aber ohne Elternzeile — für Kommissionierer ergibt das eine Liste der zu holenden Teile.
4. Im flachen AKE-Modus existiert das Komm.-Ziel-Dropdown nicht — unverändert zum heutigen
   Verhalten, also auch keine Option „Alle Ziele“.
5. Ist „Alle Ziele“ gewählt, erscheint der Badge „Alle Ziele (nur kommissionier-relevant)“ (statt
   einer Werteliste) mit Ein-Klick-Reset über denselben Mechanismus wie die anderen Standardfilter —
   case-insensitiv erkannt (`renderDefaultFilterBadges`).
6. Die Wahl von „Alle Ziele“ deselektiert automatisch alle einzeln gewählten Ziele im Dropdown und
   umgekehrt — es entsteht nie ein kombinierter Filterwert wie `!(leer),KA-02` über das Dropdown
   selbst.
7. Ein neu angelegtes Kommissionierziel, das zum Zeitpunkt der Auswahl von „Alle Ziele“ noch nicht
   existierte, erscheint trotzdem in der gefilterten Ansicht (Bedeutungswert, keine Momentaufnahme).
8. Der bestehende „Alle Filter zurücksetzen“-Mechanismus dieser Ansicht setzt auch „Alle Ziele“
   zurück (unverändert, da derselbe Input/dieselbe Select2-Instanz genutzt wird).
9. Ein Ausdruck (`PrintBom`) mit aktivem „Alle Ziele“-Filter zeigt im Kopf „Komm.-Ziel=Alle Ziele
   (nur kommissionier-relevant)“ als Teil des Filterhinweises UND enthält nur die beim Klick
   sichtbar gewesenen Positionen (bestehender `visiblePositions`-Mechanismus, unverändert).
   **Ergänzt (zweiter Prüfdurchgang S11/Punkt 4):** Ist beim Klick keine Zeile sichtbar, wird
   **nicht** gedruckt — `window.open` entfällt, stattdessen erscheint ein sichtbarer
   Leerzustand-Hinweis („Keine sichtbaren Positionen — nichts zu drucken.“). Ein Ausdruck aller
   Positionen unter „nur kommissionier-relevant“ ist damit ausgeschlossen.
10. Ein Benutzer, der `!(leer)` als `DefaultFilterBomKommissionierziel` in seinem Profil speichert,
    findet beim nächsten Öffnen der Stückliste „Alle Ziele“ vorgewählt (Badge + Dropdown-Anzeige),
    ohne dass die Vorbelegung durch das Fehlen einer passenden Select2-Option stillschweigend
    verworfen wird.
11. **Neu (zweiter Prüfdurchgang B3):** Speichert ein Benutzer im Profil oder ein Admin in der
    Benutzerverwaltung (Create ODER Edit) den Freitext `!(leer),KA-02` in
    `DefaultFilterBomKommissionierziel`, wird exakt `!(leer)` gespeichert, eine `WarningMessage`
    „Alle Ziele schließt Einzelziele aus — gespeichert wurde nur 'Alle Ziele'.“ erscheint, und die
    Stückliste öffnet danach mit „Alle Ziele“ vorgewählt und zeigt alle Positionen mit gesetztem
    Ziel.
    **Schreibweise (Freigabe-Nachtrag):** `!(LEER)` wird ebenfalls zu `!(leer)` normalisiert, aber
    **ohne** Hinweis — die Bedeutung aendert sich nicht, gemeldet wird nur verworfener Inhalt.
12. `PickingLeitstandController` enthält nach dem Rückbau **keine** Actions `CascadeReleasePreview`/
    `CascadeRelease` mehr; `IProductionOrderPickingStatusRepository` enthält **keine**
    `SetReleaseForOrderNumberAsync`-Methode mehr; `Views/PickingLeitstand/Index.cshtml` enthält
    **keinen** `.btn-cascade-release`-Button und **kein** `cascadeReleaseModal` mehr. Nachweis:
    `grep -rEn "CascadeRelease|SetReleaseForOrderNumber|CountReleasedByOrderNumber"` über
    `IdealAkeWms/` und `IdealAkeWms.Tests/` liefert **0 Treffer**.
13. `CascadeDonePreview`/`CascadeDone` (Fertigmeldungs-Kaskade) sind vom Rückbau **unberührt** und
    funktionieren unverändert.
14. Auf einer Sub-FA-Zeile (hierarchischer Modus, `IsSubFa == true`) sind in
    `PickingLeitstand/Index` und `ProductionOrders/Index` weder das Freigabe-Bedienelement noch
    die Bulk-Checkbox noch der interaktive Stückliste-Link (`Picking/Bom`) sichtbar; auf der
    HauptFA-Zeile derselben Gruppe sind sie unverändert sichtbar.
15. Ein direkter `GET /Picking/Bom/{id}` auf eine Sub-FA-Id (hierarchischer Modus) liefert
    **keine** Stückliste, sondern eine Weiterleitung auf `/Picking` mit sichtbarer
    `WarningMessage` „Kommissionierung erfolgt nur am HauptFA {OrderNumber}.“.
16. Ein direkter `POST /PickingLeitstand/ToggleRelease` auf eine Sub-FA-Id liefert **keine**
    Zustandsänderung, sondern eine sichtbare `WarningMessage` — unabhängig davon, ob die Zeile
    zuvor freigegeben war oder nicht (Sperre wirkt in beide Richtungen).
17. `BulkRelease` mit einer gemischten Auswahl (HauptFA-Ids + Sub-FA-Ids) verarbeitet nur die
    HauptFA-Ids und meldet die übersprungenen Sub-FA-Ids sichtbar (`WarningMessage`, analog zum
    bestehenden `SkippedNoArticle`-Hinweis) — sowohl beim Freigeben als auch beim Zurücknehmen.
18. Im flachen AKE-Modus zeigt AK 14-17 **keine** Wirkung (jede Zeile ist dort ihre eigene
    HauptFA-Zeile per Invariante) — Regressionsnachweis.
19. `FaWorklist/Bom` (read-only) bleibt auf jeder Sub-FA-Zeile unverändert erreichbar.
20. Die `ProductionOrderPickingStatus`-Zeile einer Sub-FA bleibt bestehen und weiterhin über
    Glas/Fremdbezug/Lackierung-Checkboxen bedienbar, unabhängig vom Freigabe-Status.
21. **`ProductionOrder.IsHauptFa`/`IsSubFa`** (zweiter Prüfdurchgang S9/Punkt 1): Unit-Test deckt
    `""`, `Sub == Order`, `Sub != Order` ab. Ein InMemory-Test mit vier Fixtures (`""`,
    `Sub == Order`, `Sub != Order`, jeweils `IsReleasedForPicking = true`) belegt: Alle vier
    Warteschlangen-Abfragen (`GetReleasedForPickingAsync`, `GetReleasedForPickingByPickerAsync`,
    `GetReleasedForPickingCountAsync`, `GetMaxPickingPriorityAsync`) liefern bzw. zählen genau die
    HauptFA-Zeilen (`""` und `==`), auch wenn die Sub-FA-Zeile `IsReleasedForPicking = true` gesetzt
    hat (Regressionsnachweis unabhängig vom einmaligen SQL-Lauf).
22. **Harte Bedingung:** Bestehende `SetReleaseBatchAsync`-/`BulkRelease`-Unit-Tests laufen ohne
    Änderung ihrer Fixtures grün.

## Test-Szenarien

Neues Kapitel **TS-79** in `docs/TESTSZENARIEN.md` (nächstes freies Kapitel nach TS-78, am
Worktree zu verifizieren), Abschnitte:

- **Relevanzregel:** Kommissionierliste vor/nach der Extraktion liefert identische Ergebnismenge
  (Regressionsvergleich).
- **„Alle Ziele“:** Stückliste öffnen (hierarchisch), Dropdown öffnen → Option „Alle Ziele (nur
  kommissionier-relevant)“ wählen → nur Positionen mit gesetztem Komm.-Ziel bleiben sichtbar. Eine
  Baugruppen-Zeile ohne eigenes Komm.-Ziel wird dabei ausgeblendet, auch wenn ihre Kinder sichtbar
  bleiben — die Kinder stehen dann ohne Elternzeile. **Das ist erwartetes Verhalten, kein Fehler**
  (zweiter Prüfdurchgang, Korrektur — für Kommissionierer ergibt sich eine Liste der zu holenden
  Teile). Badge „Alle Ziele (nur kommissionier-relevant)“ erscheint; Reset über Badge UND über „Alle
  Filter zurücksetzen“ funktioniert; ein konkretes Einzelziel danach wählen → „Alle Ziele“ wird
  automatisch abgewählt und umgekehrt; Druck mit aktivem „Alle Ziele“ zeigt den Hinweis im Kopf und
  nur die sichtbaren Positionen.
- **Druck-Leerzustand (zweiter Prüfdurchgang S11):** Stückliste ohne ein einziges Komm.-Ziel öffnen,
  „Alle Ziele“ wählen (keine Zeile bleibt sichtbar), auf „Drucken“ klicken → **kein** neuer Tab
  öffnet sich, stattdessen erscheint der sichtbare Hinweis „Keine sichtbaren Positionen — nichts zu
  drucken.“ mit Reset-Möglichkeit.
- **Neues Ziel bleibt sichtbar:** Mit aktivem „Alle Ziele“ ein neues, zuvor nicht vorhandenes
  Kommissionierziel (z. B. per Sage-Sync) hinzufügen → Position bleibt/erscheint sichtbar, ohne dass
  der Filter erneut gesetzt werden muss.
- **Persistenz:** `!(leer)` als `DefaultFilterBomKommissionierziel` im Profil speichern → Stückliste
  öffnet mit „Alle Ziele“ vorgewählt (Dropdown-Anzeige + Badge), keine stillschweigend verworfene
  Vorbelegung. **Ergänzt (zweiter Prüfdurchgang B3):** `!(leer),KA-02` im Profil UND
  in der Benutzerverwaltung (Create UND Edit) speichern → gespeichert wird `!(leer)`, `WarningMessage`
  „Alle Ziele schließt Einzelziele aus — gespeichert wurde nur 'Alle Ziele'.“ sichtbar, Stückliste
  öffnet mit „Alle Ziele“ vorgewählt und zeigt alle Positionen mit gesetztem Ziel.
- **Schreibweise ohne Hinweis (Freigabe-Nachtrag):** `!(LEER)` im Profil speichern → gespeichert wird
  `!(leer)`, **kein** Hinweis, Stueckliste oeffnet mit "Alle Ziele" vorgewaehlt.
- **Persistenz-Reload (zweiter Prüfdurchgang S13):** Stückliste mit aktivem „Alle Ziele“ per F5 neu
  laden → Endzustand ist gefiltert, Badge sichtbar (ein kurzes, ungefiltertes Zwischenbild ist
  möglich und besteht bereits für alle Bom-Filter — kein Fehler dieser Spec).
- **Negativfall leeres Dropdown (zweiter Prüfdurchgang H7):** `!(leer)` als Standard gespeichert,
  Stückliste ohne ein einziges Komm.-Ziel geöffnet → Select2-Dropdown ist gesperrt (keine Werte),
  zeigt aber „Alle Ziele“ vorgewählt; der Leerzustand-Hinweis ist sichtbar, Zurücksetzen funktioniert
  über Badge oder über den Reset-Knopf im Leerzustand.
- **Negativfall AKE:** Flache Stückliste öffnen → kein Komm.-Ziel-Dropdown, keine Wirkung.
- **Rückbau Freigabe-Kaskade:** `/PickingLeitstand` öffnen → kein „Alle Sub-FAs freigeben“-Button
  in der Gruppen-Kopfzeile mehr; `CascadeReleasePreview`/`CascadeRelease`-Routen liefern 404.
  Fertigmeldungs-Kaskade („Alle Sub-FAs fertigmelden“) funktioniert unverändert.
  `grep -rEn "CascadeRelease|SetReleaseForOrderNumber|CountReleasedByOrderNumber"` über
  `IdealAkeWms/` und `IdealAkeWms.Tests/` liefert 0 Treffer.
- **Freigabe/Picking nur am HauptFA:** In einer hierarchischen Gruppe mit mehreren Sub-FAs prüfen,
  dass nur die HauptFA-Zeile Freigabe-Button, Prioritäts-Input, Bulk-Checkbox und
  Stückliste-Icon zeigt; Sub-FA-Zeilen zeigen keines davon.
- **Negativfall direkter Zugriff:** `/Picking/Bom/{sub-fa-id}` direkt aufrufen →
  Weiterleitung + Warnhinweis, keine Stückliste. `POST /PickingLeitstand/ToggleRelease` mit
  Sub-FA-Id → keine Änderung + Warnhinweis, unabhängig vom bisherigen Freigabe-Status der Zeile.
- **BulkRelease gemischt:** HauptFA- und Sub-FA-Ids gemeinsam auswählen und freigeben → nur
  HauptFA wird freigegeben, Sub-FA wird mit Hinweis übersprungen. Gegenprobe: eine bereits
  freigegebene Sub-FA gemeinsam mit HauptFA-Ids „zurücknehmen“ auswählen → Sub-FA wird ebenfalls
  übersprungen und gemeldet.
- **Negativfall AKE (Freigabe):** Flacher Modus, `BulkRelease`/`ToggleRelease` auf eine normale
  FA-Id → funktioniert unverändert (jede Zeile ist ihre eigene HauptFA-Zeile).
- **Warteschlange bereinigt:** Im Testsystem eine Sub-FA mit `IsReleasedForPicking = 1` anlegen
  (oder eine bestehende Altlast verwenden), vorab die `SELECT COUNT(*)` aus dem SQL-Lauf notieren,
  Kommissionierer-Warteschlange (`/Picking`) öffnen → die Sub-FA erscheint dort **nicht** (weder vor
  noch nach dem einmaligen SQL-Lauf, da die Warteschlangen-Abfragen strukturell filtern);
  Home-Kennzahl (`GetReleasedForPickingCountAsync`) zählt sie nicht mit.
- **Warteschlangen-Gruppierung (Hinweis, kein Fehlerfall):** Die Warteschlange zeigt je HauptFA-
  Gruppe nur noch eine Zeile; Gruppen mit mehreren Sub-FAs sind nicht mehr zu erwarten (siehe
  Backlog-Notiz [[2026-09-25-picking-warteschlange-gruppierung-rueckbau]] — kein Fehler, wenn sie
  ausbleiben).
- **Unberührt:** `FaWorklist/Bom` weiterhin auf jeder Sub-FA-Zeile erreichbar; Glas/Fremdbezug/
  Lackierung-Checkboxen einer Sub-FA weiterhin bedienbar, auch wenn die Zeile nie freigegeben wird.
- **Hilfeseite:** Picking-/Leitstand-Hilfeabschnitt nennt „Freigabe und Stückliste nur am HauptFA“
  und die Schaltfläche „Alle Ziele“.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen (TS-79-Eintrag), TS-73
(v1.38.0) um den Rückbau-Vermerk bei den Kaskade-Szenarien ergänzen.

## Deploy

- **Web-App:** ja — Controller/Views/Services im `IdealAkeWms`-Projekt.
- **Service:** nein — `IDEALAKEWMSService` unberührt (keine Sync-/Worker-Änderung).
- **Migration:** nein — „Alle Ziele“ nutzt das bestehende Feld `DefaultFilterBomKommissionierziel`,
  keine Schemaänderung.
- **Einmaliger SQL-Lauf, NUR Testsystem** (siehe Migrations-/SQL-Auswirkungen — inkl. vorgeschaltetem
  `SELECT COUNT(*)` für das Testprotokoll), nach dem Deploy des Bündels auf das Testsystem, vor dem
  manuellen Testlauf von TS-79 „Warteschlange bereinigt“ auszuführen. Kein `SQL/XX`-Skript, kein
  Produktivlauf (Bündel ist nicht gemergt, keine Produktivdaten betroffen).
- **Kontext:** Der betroffene Code existiert ausschließlich im nicht gemergten Bündel-Worktree
  `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch `feature/2026-08-07-ideal-teile-1-5`).
  Diese Spec wird dort als weitere Etappe umgesetzt — kein zusätzlicher eigener Deploy-Schritt,
  sie geht im selben Publish/Merge des Bündels mit (Vorbedingung: der Dev-Lauf bestätigt diesen
  provisorischen Ausführungsort gegen den dann aktuellen Worktree-/Branch-Stand).
- **Publish-Befehle (nachgelagerter Fall, im Worktree auszuführen):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- Ablauf: Publish **aus dem Worktree** → Testsystem → einmaliger SQL-Lauf → Test → **danach** Merge
  (Schranke 2, Mensch). Nach dem Merge nur dann erneut aus `main` publishen, wenn der Merge
  tatsächlich getestete Dateien mit parallelen `main`-Änderungen zusammenführt (Bündel-Worktree mit
  vielen Etappen — Merge-Diff prüfen).
- **Brain-Nachzug (Dev-Lauf, HAUPTCHECKOUT):** `secondbrain/specs/freigegeben/2026-09-10-fa-liste-ausbau-matchcode-spec.md`
  um einen Hinweis ergänzen, dass Block 4/Tasks 12-14 durch diese Spec zurückgebaut wurden —
  sonst widerspricht die freigegebene Spec dem tatsächlichen Codezustand. Zusätzlich neue
  Backlog-Notiz [[2026-09-25-picking-warteschlange-gruppierung-rueckbau]] anlegen (H1) und
  `secondbrain/architektur/fallstricke.md` um den Platzhalter-Fallstrick der Komm.-Ziel-Zelle
  ergänzen.

## Offene Rückfragen

Alle Rückfragen aus beiden kritischen Prüfdurchgängen beantwortet, siehe Freigabe-Antworten;
Überarbeitung 2026-09-25 (erste und zweite Prüfrunde) vollständig eingearbeitet.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → **Keine eigene Checkbox — "Alle Ziele" im bestehenden Komm.-Ziel-Dropdown (S4 uebernommen).**
   "Nur kommissionier-relevant" ist fachlich dasselbe wie "alle Ziele gewaehlt". Der bestehende
   Komm.-Ziel-Filter bringt Badge, Reset, Leerzustand, rekursive Suche, Vorfahren-Aufklappen, Druckhinweis
   und Speicherung je Benutzer bereits mit. **Migration 94 entfaellt, S2 und S3 erledigen sich.**

   **Mit einer verbindlichen Ergaenzung — der benannte Nachteil ist kein kosmetischer:**
   Wuerde "Alle Ziele" die **aktuelle Werteliste** speichern, fehlte ein spaeter neu angelegtes
   Kommissionierziel im gespeicherten Standard. Die Positionen dieses Ziels waeren fuer jeden, der sich auf
   seinen Standard verlaesst, **still ausgeblendet** — genau der Fall "gefiltert, sieht aber vollstaendig
   aus". Deshalb:
   - **"Alle Ziele" setzt einen Bedeutungswert, keine Momentaufnahme:** "Ziel ist nicht leer". Neue Ziele
     sind damit automatisch enthalten.
   - **Schreibweise `!(leer)`** — dieselbe wie in der geplanten Suchsyntax
     ([[2026-09-23-suchsyntax-spaltenfilter-erweitern]]), damit es spaeter keine zwei Notationen gibt. Bis
     die Suchsyntax gebaut ist, behandelt der Komm.-Ziel-Filter diesen Wert **gesondert** — die heutige
     Mini-Syntax wuerde `!(leer)` sonst als "enthaelt nicht '(leer)'" lesen und alles zeigen.
   - **Der Badge zeigt dann "Alle Ziele (nur kommissionier-relevant)"** statt einer Werteliste — damit ist
     auch der zweite benannte Nachteil behoben.
   **Standard:** je Benutzer ueber das bestehende `DefaultFilterBomKommissionierziel`. Kommissionierer
   setzen "Alle Ziele" einmal als ihren Standard; ein globaler Standard wird nicht gebaut.

2. → **Nur Extraktion, kein Schalter.** Mit einem einzigen Kriterium waere der Schalter das Ausschalten
   der Kommissionierliste selbst — und dann summierte sie nach leerem Ziel, wofuer sie nicht gebaut ist
   (H2). Die Regel `KommissionierRelevanzFilter.IsRelevant` wird trotzdem herausgeloest: Sie ist der Ort,
   an dem kuenftige Kriterien hinzukaemen. **Dann** lohnt eine Einstellung — mit bekanntem Bedarf.

3. → **Einmaliger SQL-Lauf im Testsystem PLUS die Warteschlange filtert Sub-FAs aus.**
   *Korrektur der frueheren Empfehlung "stehen lassen, folgenlos" — sie war am Code falsch (B2):
   Freigegebene Sub-FAs bleiben in der Warteschlange, zaehlen in der Startseiten-Kennzahl und sind nicht
   mehr ruecknehmbar.*
   - **SQL-Lauf, nur Testsystem**, im Deploy-Abschnitt dokumentiert (kein `SQL/XX`):
     `IsReleasedForPicking = 0` fuer alle Zeilen mit `SubOrderNumber <> OrderNumber`.
   - **Warteschlange korrekt durch Konstruktion:** `GetReleasedForPickingAsync`,
     `GetReleasedForPickingCountAsync` und `GetMaxPickingPriorityAsync` beruecksichtigen nur HauptFAs
     (ueber `IsSubFa`, siehe S1). Damit schadet es nicht, falls der SQL-Lauf einmal vergessen wird —
     Altlasten tauchen in der Warteschlange gar nicht erst auf.
   - **Die Sperre bleibt in beide Richtungen** — kein zusaetzliches Zuruecknehmen-Element noetig, weil die
     Warteschlange Sub-FAs ohnehin nicht mehr zeigt.

## ANTWORTEN auf die Kritische Pruefung (2026-09-25)

**S1 — uebernommen, ausdruecklich.** Eine berechnete Eigenschaft `ProductionOrder.IsSubFa`, leer gilt als
HauptFA. Verwendet von `Bom`, `ToggleRelease`, `SetReleaseBatchAsync` **und** den drei
Warteschlangen-Abfragen aus Antwort 3. Unit-Test mit `""`, `Sub == Order`, `Sub != Order`.
**Harte Bedingung:** Die bestehenden `SetReleaseBatchAsync`-/`BulkRelease`-Tests laufen **ohne Aenderung
ihrer Fixtures** gruen. Werden Fixtures angepasst, damit Tests gruen werden, ist das ein Befund, kein
Erfolg.

**S5 — uebernommen.** Weiterleitung auf `/Picking` mit Hinweis "Kommissionierung erfolgt nur am HauptFA
{OrderNumber}", Vorbild der Artikelnummer-Pruefung derselben Action.

**S6 — uebernommen.** Changelog v1.38.0 als zurueckgenommen kennzeichnen (Anwender haben ihn sonst als
gueltige Funktion vor Augen), TS-73 vollstaendig mitziehen, `CountReleasedByOrderNumberAsync` entfernen.
AK: `grep CascadeRelease|SetReleaseForOrderNumber|CountReleasedByOrderNumber` ueber `IdealAkeWms*/`
liefert **0 Treffer**.

**S7 — uebernommen.** Out-of-Scope ausdruecklich: Gesperrt werden nur die **Einstiege** (Freigabe,
interaktive Stueckliste). Folgeaktionen sind ohne Einstieg nicht erreichbar und bleiben bewusst
ungeschuetzt.

**S8 — uebernommen.** Picking- und Leitstand-Hilfe: "Freigabe und Stueckliste nur am HauptFA" und die
Schaltflaeche "Alle Ziele".

**H1 — als eigener Backlog-Punkt festzuhalten:** Die Zwei-Ebenen-Gruppierung der
Kommissionierer-Warteschlange wird funktional ueberfluessig. Nicht Teil dieser Spec — aber im
Testprotokoll vermerken, damit niemand "Gruppe mit mehreren Sub-FAs in der Warteschlange" prueft und sie
nicht findet.

**Fallstrick aus Schwerpunkt 3 festhalten:** Die Stueckliste prueft auf dem gerenderten Zellentext. Setzt
jemand spaeter einen Platzhalter ("–") in die Komm.-Ziel-Zelle, kippt die Relevanzpruefung still.

## Kritische Pruefung (2026-09-25)

Geprüft am Code des Bündel-Worktrees `.claude/worktrees/2026-08-07-ideal-teile-1-5` (HEAD `2d06d1f0`,
AppVersion 1.45.0). Schwerpunkte laut Auftrag: AKE-Sicherheit der HauptFA-Regel, Vollständigkeit des
Rückbaus, eine Regel für zwei Verbraucher, fünfter Standardfilter.

### Schwerpunkt 1 — AKE-Sicherheit der HauptFA-Regel (Ergebnis: tragfähig, aber drei unterschiedliche Formeln)

- **NULL ist nachweislich ausgeschlossen.** `ApplicationDbContext.cs:414` `SubOrderNumber … IsRequired()`,
  DB-Spalte `NVARCHAR(100) NOT NULL` + UNIQUE (`SQL/90_InvertProductionOrderHierarchy.sql` Schritt 3,
  Backfill `SET SubOrderNumber = OrderNumber WHERE SubOrderNumber IS NULL` davor). Model-Default
  `string.Empty`, nicht `null`.
- **Der AKE-Sync setzt die Spalte bei jedem neuen Auftrag:** `SageProductionOrderSql.BuildUpsert(true)`
  schreibt im INSERT-Zweig `@OrderNumber,@OrderNumber` (`SageProductionOrderSql.cs:17`), der UPDATE-Zweig
  lässt sie unangetastet. Der Zweig ohne Spalte (`includeSubOrderNumber=false`) läuft nur gegen eine
  DB **vor** Migration 90. Gegen die sofort nach dem Deploy angelegte Spalte scheitert jeder INSERT
  ohne `SubOrderNumber` laut an NOT NULL (gilt auch für den archivierten AgentJob
  `_archiv/01_Import_Produktionsauftraege.sql`). Er legt keine NULL-Zeile an. Einziger zweiter Schreibpfad:
  `FaMaterializationSyncService.cs:144` (nur IDEAL, Wurzel mit `SubFA == HauptFA`). Weitere
  `new ProductionOrder` außerhalb der Tests gibt es nicht.
- **Leerer String** wird von keinem Schreibpfad erzeugt. Der UNIQUE-Index lässt ihn ohnehin nur für
  höchstens eine Zeile zu.
- **Der eigentliche Riss:** Die Spec schreibt die Sub-FA-Erkennung dreimal verschieden.
  (a) `PickingController.Bom`: „gesetzt **und** ungleich“, leer gilt also als HauptFA (sicher).
  (b) `SetReleaseBatchAsync`: nur `SubOrderNumber != OrderNumber`, leer gilt also als **Sub-FA**
  (gesperrt). (c) Die View-Vorlage `_ProductionOrderRow.cshtml:16-18` `isHauptFaRow` verlangt
  `!IsNullOrEmpty`. Leer gilt dort also als **nicht** HauptFA. Das ist harmlos, weil zusätzlich
  `Model.Hierarchical` gated.
  Für (b) ist das kein Produktivrisiko, aber ein Testrisiko: Bestehende Test-Fixtures legen
  `ProductionOrder` ohne `SubOrderNumber` an (`""`). Die `SetReleaseBatchAsync`-Tests würden dann still
  überspringen oder rot werden. Die Versuchung liegt nahe, die Fixtures „passend“ zu machen, statt die
  Formel zu vereinheitlichen.

### Schwerpunkt 2 — Rückbau der Freigabe-Kaskade (Ergebnis: fast vollständig, zwei Lücken)

Vollständige Fundstellenliste (`grep CascadeRelease|SetReleaseForOrderNumber|CountReleasedByOrderNumber|Sub-FAs freigeben`):
Controller (5), Interface (2), Repository (2), `Views/PickingLeitstand/Index.cshtml` (28: Button Z. 206-217,
Modal Z. 398-451, JS ab Z. 556), Tests (24 + 13), **`Views/Help/Changelog.cshtml` Z. 180 + Z. 203-207**,
`docs/TESTSZENARIEN.md` (TS-73-Titel Z. 7726, Z. 7753, TS-73.11-13, Z. 7895, 7940-7952, Schlusszeile Z. 8575).
`.superpowers/sdd/…` und `docs/superpowers/plans/…` sind historische Artefakte. Sie bleiben.

- `CountReleasedByOrderNumberAsync` hat **keine** Fremdverwendung (nur `CascadeReleasePreview` Z. 563
  und die eigenen Tests). Die Methode kann mit weg, die „vorher prüfen“-Klausel ist damit erledigt.
- `CanManagePickingRelease` bleibt. Es wird an zahlreichen anderen Stellen genutzt (Filter, Home, Spalten).
- **Layout:** Der Button steht als Inline-Element (`ms-2`) in derselben `<td colspan>` der Gruppen-Kopfzeile
  hinter „Alle Sub-FAs fertigmelden“. Entfernen hinterlässt **keine** Lücke. Nutzer mit Leitstand-Recht
  **ohne** Picking-Recht sehen danach eine Kopfzeile nur mit „HauptFA …“ und Badge. Das ist korrekt.

### Schwerpunkt 3 — Eine Regel, zwei Verbraucher (Ergebnis: Vermutung „SQL gegen In-Memory“ widerlegt)

- `BuildFlagPredicate` ist **kein** SQL-Ausdruck, sondern ein `Func<FaHierarchyNode,bool>` (In-Memory,
  `KommissionierListenService.cs:228`). Die Stückliste prüft im JS auf dem gerenderten Zellentext.
  Beide sind In-Memory. **Datenquelle identisch:** `FaHierarchyBomRepository.cs:186`
  `Kommissionieren = node.Kommissionieren`, also dasselbe Feld desselben Knotens.
- Die Regeln sind semantisch gleichwertig (`IsNullOrWhiteSpace` gegen `textContent.trim() !== ''`), solange
  die Zelle den Rohwert ohne Platzhalter rendert. Das ist heute so: `Bom.cshtml:256` `<td>@item.Kommissionieren</td>`.
  Setzt jemand später einen Platzhalter („–“) in die Zelle, kippt die JS-Seite still. Das gehört in
  den Fallstrick-Eintrag.
- **Die C#-Extraktion hat nur einen Verbraucher.** Die „geteilte“ Methode wird ausschließlich von
  `BuildFlagPredicate` aufgerufen. Die zweite Nutzung ist ein Kommentar. Das ist ehrlich benannt, aber
  keine geteilte Regel im technischen Sinn (siehe SOLLTE 4).

### Schwerpunkt 4 — Fünfter Standardfilter (Ergebnis: Badge ja, Reset nur halb, Baumverhalten falsch beschrieben)

- Badge: derselbe Container `#bomDefaultFilterBadges`. Format und Reset brauchen einen eigenen Zweig,
  weil `renderDefaultFilterBadges()` nur über `bomFilterInputValue(key)` iteriert. Das ist in der Spec benannt.
- **„Alle Filter zurücksetzen“ ist für die Checkbox praktisch unerreichbar:** Der Knopf lebt nur in
  `#bomKzEmptyState`. `checkKommissionierzielEmptyState()` blendet ihn ausschließlich ein, wenn der
  **Komm.-Ziel-Textfilter** gesetzt ist und keine Treffer liefert. Ergibt die Checkbox allein eine leere
  Tabelle, erscheint weder Leerzustand noch Reset-Knopf. Die Tabelle ist dann still leer, ein Verstoß
  gegen „Melden statt still behandeln“. AK 6 ist so nur über einen Umweg testbar.
- **Baumverhalten (AK 3) widerspricht dem Code:** `updateBomVisibility()` (Z. 854-893) filtert **jede**
  Zeile, auch Baugruppen-Eltern. Eine Baugruppe mit leerem `Kommissionieren` wird ausgeblendet, obwohl
  ein Kind sichtbar bleibt. „Eltern bleiben sichtbar, wenn ein Kind sichtbar ist — bestehendes
  Baum-Verhalten“ gibt es nicht.
  Die rekursive Suche und `expandAncestorsOfMatching` greifen nur über `getActiveFilters()`. Die
  Checkbox liegt außerhalb davon, Treffer in zugeklappten Baugruppen blieben unsichtbar.

---

### BLOCKER

**B1 — Alle drei Freigabe-Antworten sind leer.** Das war bei Einreichung angekündigt. Formal ist die Spec
damit nicht freigabefähig. Frage 2 ist eine Entweder-oder-Frage: Die Antwort muss „nur Extraktion“ oder
„Schalter bauen“ lauten, kein „ja“.

**B2 — Die Empfehlung zu Rückfrage 3 („stehen lassen, folgenlos“) ist am Code falsch.** Eine freigegebene
Sub-FA erscheint weiter in der **Kommissionierer-Warteschlange** `/Picking`
(`GetReleasedForPickingAsync`, `ProductionOrderPickingStatusRepository.cs:173-183`, Gruppierung
`PickingController.cs:199-206`). Sie zählt in `GetReleasedForPickingCountAsync` (Home-KPI) mit und
beeinflusst `GetMaxPickingPriorityAsync`.
Nach dieser Spec ist ihr Stückliste-Link gesperrt (Guard in `Bom`). Zurücknehmen lässt sie sich über
die UI auch nicht mehr: `ToggleRelease` ist ein **Umschalter**, und der geplante Guard sperrt beide
Richtungen. Die Bulk-Checkbox ist auf Sub-FA-Zeilen ausgeblendet. Ergebnis: tote Einträge in der
Warteschlange des Kommissionierers, die niemand mehr entfernen kann.
**Frage an den Menschen:** (a) Sollen die Sub-FA-Guards nur die Richtung **Freigeben** sperren, damit
Zurücknehmen erlaubt bleibt? Vorschlag: ja, Guard in `ToggleRelease` nur bei `!ps.IsReleasedForPicking`,
analog `SetReleaseBatchAsync` nur bei `release == true`. Dann braucht die Sub-FA-Zeile im Leitstand ein
Zurücknehmen-Element, solange sie freigegeben ist. (b) Oder setzt ein einmaliger SQL-Lauf im Testsystem
`IsReleasedForPicking = 0` für alle Zeilen mit `SubOrderNumber <> OrderNumber`? Dann gehört das Skript
in den Deploy-Abschnitt (kein `SQL/XX`, nur Testsystem). Zusätzlich sollte `GetReleasedForPickingAsync`
Sub-FAs defensiv ausfiltern, damit Altlasten die Warteschlange nicht verunreinigen.

### SOLLTE

**S1 — Eine Sub-FA-Formel statt dreier (Schwerpunkt 1).** Vorschlag: eine berechnete Eigenschaft
`ProductionOrder.IsSubFa => !string.IsNullOrEmpty(SubOrderNumber) && SubOrderNumber != OrderNumber`
(Leer gilt als HauptFA, sichere Richtung für AKE). Sie wird von `Bom`, `ToggleRelease` und
`SetReleaseBatchAsync` genutzt. Drei Aufrufer mit identischer Regel rechtfertigen den Einzeiler
(Ponytail Sprosse 2 statt dreifacher Inline-Kopie). Dazu kommt ein Unit-Test mit den Fällen
`""`, `Sub == Order` und `Sub != Order`.
Harte Akzeptanzbedingung ergänzen: „Bestehende `SetReleaseBatchAsync`-/`BulkRelease`-Tests laufen
**ohne Änderung ihrer Fixtures** grün.“

**S2 — AK 3 an den Code angleichen und den Filter in die Baum-Mechanik einhängen (Schwerpunkt 4).**
Entweder AK 3 ehrlich formulieren („Baugruppen ohne eigenes Komm.-Ziel werden ausgeblendet — wie beim
bestehenden Komm.-Ziel-Filter“) oder das neue Verhalten ausdrücklich bestellen. In jedem Fall muss die
Checkbox in `hasActiveFilter` einfließen (rekursive Suche) und Vorfahren aufklappen, sonst bleiben
relevante Positionen in zugeklappten Baugruppen verborgen. Ein neues AK dazu: „Relevante Position in
einer zugeklappten Baugruppe wird bei aktivierter Checkbox sichtbar.“

**S3 — Leerzustand auch für die Checkbox (Schwerpunkt 4).** `checkKommissionierzielEmptyState()` soll auch
greifen, wenn die Checkbox aktiv ist und keine Zeile übrig bleibt, mit Text „Nur kommissionierrelevant
aktiv — keine Positionen mit Komm.-Ziel in dieser Stückliste“. Erst dann ist „Alle Filter zurücksetzen“
(AK 6) regulär erreichbar.

**S4 — Die günstigere Alternative prüfen (Ponytail Sprosse 2), vor Rückfrage 1 entscheiden.** Das
Komm.-Ziel-Select2 (`setupKommissionierzielDropdown`) enthält bereits **alle** gesetzten Ziel-Werte.
„Nur kommissionierrelevant“ ist fachlich identisch mit „alle Ziele gewählt“. Eine Schaltfläche „Alle
Ziele“ neben dem Dropdown, die alle Optionen wählt, bekäme Badge, Einzel-Reset, Leerzustand, Rekursion,
Vorfahren-Aufklappen, Druck-`filterInfo` **und** die bestehende Persistenz
(`DefaultFilterBomKommissionierziel`) umsonst. Damit entfielen Migration 94 und S2/S3.
Nachteil: Der Badge zeigt die Werteliste statt „Nur kommissionierrelevant“, und ein Ziel, das nach dem
Speichern neu hinzukommt, fehlt im gespeicherten Default. **Frage an den Menschen:** eigene Checkbox
(Spec-Entwurf) oder „Alle Ziele“ im bestehenden Dropdown?

**S5 — Landestelle beim Bom-Guard festlegen, nicht „am Code zu verifizieren“.** `Bom` hat keinen
`returnUrl`. Der bestehende Artikelnummer-Guard derselben Action macht `RedirectToAction(nameof(Index))`
(`PickingController.cs:285-289`), Vorbild übernehmen. AK 11 dann: „Weiterleitung auf `/Picking` mit
WarningMessage ‚Kommissionierung erfolgt nur am HauptFA {OrderNumber}.‘“

**S6 — Rückbau-Liste um zwei Stellen ergänzen:** `Views/Help/Changelog.cshtml` v1.38.0-Eintrag (Überschrift
Z. 180 „…, Freigabe-Kaskade“ und Bullet Z. 203-207) als zurückgenommen kennzeichnen. Anwender haben den
Text sonst als gültige Funktion vor Augen. Außerdem den TS-73-Kapiteltitel, Z. 7753 und die
Schlusszeile Z. 8575 in `docs/TESTSZENARIEN.md` mitziehen, nicht nur TS-73.11-13. AK 8 sinngemäß
erweitern: „`grep CascadeRelease|SetReleaseForOrderNumber|CountReleasedByOrderNumber` über `IdealAkeWms*/`
liefert 0 Treffer.“ Das macht die Vollständigkeit objektiv prüfbar.

**S7 — Abgrenzung der übrigen Picking-Actions für Sub-FAs explizit machen.** Gesperrt werden nur `Bom`,
`ToggleRelease` und `SetReleaseBatchAsync`. Per direkter URL/POST bleiben `PrintBom`, `PrintPicking`,
`TransferPicked`, `SetPickingStatus`, `ToggleDone` (`PickingController`) und `SetPriority`/
`ChangeAssignedPicker` (`PickingLeitstandController`) auf Sub-FA-Ids erreichbar. Vorschlag für den
Out-of-Scope-Abschnitt: „Nur die Einstiege (Freigabe, interaktive Stückliste) werden gesperrt.
Folgeaktionen sind ohne Einstieg nicht erreichbar und bleiben ungeschützt. Bewusst, kein
Verteidigung-in-der-Tiefe-Anspruch für jede Action.“ Sonst liest der Dev-Lauf „Verteidigung in der
Tiefe“ als Auftrag für alle.

**S8 — Hilfeseite.** Weder die neue Checkbox noch „Freigabe und Stückliste nur am HauptFA“ stehen in der
Hilfe-Planung. Nach Hausregel gehören konkrete Hilfe-Details dazu (Picking-/Leitstand-Hilfe), nicht
nur der Changelog.

### HINWEIS

- **H1 — Warteschlange `/Picking` hierarchisch:** Ist nur noch die HauptFA freigebbar, besteht jede Gruppe
  der Kommissionierer-Liste aus genau einer Zeile. Die Zwei-Ebenen-Gruppierung (`PickingController.cs:199ff`)
  wird damit funktional überflüssig. Rückbau gehört nicht in diese Spec, sollte aber als Aufgabe
  festgehalten werden (sonst prüft ein Tester „Gruppe mit mehreren Sub-FAs in der Queue“ und findet sie nicht).
- **H2 — Rückfrage 2 / AK 17:** Mit dem Schalter „aus“ würde die Kommissionierliste auch Positionen mit
  leerem Ziel listen und nach Ziel `""` summieren. Dafür ist die Summierung nicht gebaut. Das stützt die
  Spec-Empfehlung, keinen Schalter zu bauen.
- **H3 — Größe:** Standardumfang ca. 14-16 Dateien in drei Schichten (Controller ×2, Repository + Interface,
  neue Klasse, Views ×3, Tests ×2-3, Docs, Changelog, Version ×2). Das ist in einem Dev-Lauf machbar.
  Mit Persistenz (Rückfrage 1 = ja, ohne S4) kommen etwa 10 Dateien plus Migration dazu. Das ist grenzwertig,
  dann wäre S4 das stärkste Argument.
- **H4 — Deploy:** Der Abschnitt ist plausibel (nur Web, im Bündel). Echte Produktivdaten entstehen nicht,
  der Bündel-Branch ist nicht gemergt. Die freigegebenen Sub-FAs aus B2 existieren nur im Testsystem.
- **H5 — Routen 404:** Entfernte Actions liefern bei konventionellem Routing 404. Der Testschritt in TS-79
  ist so abarbeitbar.

**NACHBESSERUNG NOETIG: Freigabe-Antworten leer; Empfehlung zu Rückfrage 3 erzeugt tote, nicht rücknehmbare Einträge in der Kommissionierer-Warteschlange (B2).**

## Kritische Pruefung — zweiter Durchgang (2026-09-25)

Geprüft wird die Überarbeitung `71c923e6` gegen den Bündel-Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5` (HEAD `2d06d1f0`). Die Entscheidungen werden nicht neu
aufgerollt. Geprüft wird nur, ob das Nachziehen vollständig und widerspruchsfrei ist.

**Vollständigkeit der Antworten:** Alle drei Freigabe-Antworten sind beantwortet und eindeutig, die
Antworten auf S1/S5-S8/H1/Fallstrick sind vorhanden. Der Rumpf übernimmt sie weitgehend. Die Befunde
unten sind Risse im Nachziehen, keine offenen Grundsatzfragen.

### Schwerpunkt 1 — Die Formel ist wieder vierfach (Ergebnis: vermeidbar, EINE Expression genügt)

- **Die vier Abfragen filtern NICHT auf derselben Wurzel.** `GetReleasedForPickingAsync` und
  `…ByPickerAsync` laufen auf `_context.ProductionOrders` (`p`), `GetReleasedForPickingCountAsync` und
  `GetMaxPickingPriorityAsync` dagegen auf `_context.ProductionOrderPickingStatuses` (`s`, Navigation
  `s.ProductionOrder`) (`ProductionOrderPickingStatusRepository.cs:173-216`). Eine
  `Expression<Func<ProductionOrder,bool>>` lässt sich auf `s.ProductionOrder` nicht ohne Hilfsbibliothek
  (LinqKit/`Invoke`) einsetzen.
- **Die Navigation verhindert es aber nicht wirklich:** Die Beziehung ist 1:1. `ProductionOrderPickingStatus`
  hat einen UNIQUE-Index auf `ProductionOrderId` (`ApplicationDbContext.cs:455`), dazu
  `.WithOne(p => p.PickingStatus)` (Z. 464) und `ProductionOrder.PickingStatus` (`ProductionOrder.cs:74`).
  Count und Max lassen sich deshalb gleichwertig von der Wurzel `ProductionOrders` aus schreiben, mit
  genau dem Prädikat, das `GetReleasedForPickingAsync` schon benutzt
  (`p.PickingStatus != null && p.PickingStatus.IsReleasedForPicking && !p.IsDone && !p.IsCancelled …`).
  Dann nutzen alle vier `.Where(ProductionOrder.IsHauptFa)`.
- **Das ergibt eine einzige Definition statt fünf.** Die Expression ist die Quelle. Die Eigenschaft
  wird aus ihr kompiliert, statt die Logik ein zweites Mal hinzuschreiben:
  `public static readonly Expression<Func<ProductionOrder,bool>> IsHauptFa = p => p.SubOrderNumber == "" || p.SubOrderNumber == p.OrderNumber;`
  `private static readonly Func<ProductionOrder,bool> IsHauptFaCompiled = IsHauptFa.Compile();`
  `[NotMapped] public bool IsSubFa => !IsHauptFaCompiled(this);`
  Damit gibt es keinen Kommentar „gleichwertig zu IsSubFa“ mehr, der gepflegt werden müsste.
  Gegenüber `IsNullOrEmpty` verschiebt sich nur der Fall `null`: Er gilt dann als Sub-FA. Das ist
  folgenlos, weil die Spalte NOT NULL ist, das Modell mit `string.Empty` vorbelegt und Test-Fixtures
  `""` tragen. Der Fall `null` ist bewusst nicht Teil der Unit-Testfälle.
- **Was ein Test belegen kann und was nicht:** Ein EF-InMemory-Test beweist nur die C#-Semantik.
  SQL Server vergleicht in der Standard-Collation ohne Groß-/Kleinschreibung und ignoriert Leerzeichen
  am Ende (`'123 ' = '123'`, `'  ' = ''`). Die in-memory ausgewertete Eigenschaft in `Bom`,
  `ToggleRelease` und `SetReleaseBatchAsync` und die SQL-Abfragen können deshalb für Werte
  auseinanderlaufen, die sich nur in Groß-/Kleinschreibung oder Leerzeichen am Ende unterscheiden.
  Praktisch folgenlos: Beide Spalten stammen aus demselben Wert (AKE `@OrderNumber,@OrderNumber`,
  IDEAL `n.SubFA.ToString()`/`n.HauptFA.ToString()`). Das gehört als ein Satz in die Spec, nicht in einen
  Test, der es nicht zeigen kann.

### Schwerpunkt 2 — Exklusivität von „Alle Ziele“ hat eine Hintertür (Ergebnis: bestätigt, nicht abgefangen)

- Die Spec sichert Exklusivität nur im Select2-`change`-Handler. Das Profilfeld
  (`Profile.cshtml:53`, `Users/Edit.cshtml:117`) ist Freitext. `AccountController.cs:219` und
  `UsersController.cs:129/301` machen nur `Trim()`.
- **Was bei `!(leer),KA-02` heute passiert:** Der geplante Sonderfall in `bomMatchesFilter` greift nur
  bei **exakt** `!(leer)`. Sonst läuft `val.startsWith('!')` in die Ausschlusslogik (Z. 831-834): Sie zeigt
  alles **außer** Zeilen, die „(leer)“ oder „ka-02“ enthalten, also das Gegenteil des Gewollten.
- **Was das Dropdown dann zeigt:** Die Vorbelegung (`pre = input.value.split(',')`, Z. 1065, 1074) findet
  **beide** Optionen. Select2 zeigt „Alle Ziele“ **und** „KA-02“ gleichzeitig ausgewählt, der Badge
  zeigt den Rohwert, und die Stückliste filtert nach Ausschluss. Drei widersprüchliche Signale.
- Nebenfall Großschreibung: `!(LEER)` im Profil wertet `bomMatchesFilter` korrekt aus, weil
  `getActiveFilters` den Wert klein schreibt. Die Select2-Vorbelegung vergleicht aber mit dem genauen
  Optionswert `!(leer)`, findet ihn nicht und verwirft die Vorbelegung still. Das ist genau der Fall,
  den AK 10 ausschließen will.

### Schwerpunkt 3 — Reihenfolge mit table-filter.js (Ergebnis: garantiert, kein neues Flackern; Begründung in der Spec ungenau)

- **`setColumnFilter`-Pfad:** Der Bom-Wrapper (Z. 979-983) ruft `_realSetColumnFilter` und dann
  synchron `updateBomVisibility()` im selben JS-Task. Der Browser zeichnet dazwischen nicht, es gibt
  also kein Flackern. Beim Tippen in die Filterzeile hängen beide Listener am selben `input`-Event,
  zuerst table-filter.js, dann Bom (Z. 1138-1140). Auch das läuft synchron und ohne Zeichnen.
- **Seitenaufbau mit Filter aus sessionStorage** (`data-view-key="Bom"`, gilt auch für ein
  gespeichertes `!(leer)`): `table-filter.js` `init()` stellt ihn im `column-preferences-ready`-Handler
  wieder her und ruft `applyFilters()` auf (Z. 229-233). Die Bom-Initialisierung korrigiert das erst in einem
  **eigenen** Task (`setTimeout(…, 0)`, Z. 1136-1163). Dazwischen **kann** ein Frame gezeichnet werden.
  Das ist aber schon heute so, für **jeden** Bom-Filter: `applyFilters` ignoriert den Baumzustand und
  blendet auch zugeklappte Kinder ein. `!(leer)` bringt kein neues Flackern, nur ein anderes Bild im
  selben Frame.
- Es gibt **keinen** weiteren Pfad, auf dem table-filter.js ohne nachfolgendes `updateBomVisibility`
  filtert. `sortTable` wendet keine Filter an. Der Kalender-Pfad (`applyColumnFilterNow` direkt) greift
  nur bei `th[data-date-filter]`, und Bom hat keine Datumsspalte. `data-clear-table-filters` kommt in
  Bom nicht vor.
- **Bewertung:** Die Komm.-Ziel-Spalte aus table-filter.js auszunehmen, würde gemeinsames JS für ein
  kosmetisches Ein-Frame-Problem anfassen, das ohnehin für alle Bom-Filter besteht. „Nicht anfassen“
  bleibt richtig. Die Begründung in der Spec soll aber den Seitenaufbau-Pfad ehrlich benennen, statt
  nur „wird danach überschrieben“ zu sagen.

### Schwerpunkt 4 — Druck (Ergebnis: stimmt, bis auf einen Randfall)

- `visiblePositions` entsteht aus `row.style.display !== 'none'` **nach** `updateBomVisibility` und
  spiegelt damit den Bildschirm. Die Positionen sind eindeutig (`FaHierarchyBomRepository.cs:92`
  `Unique(segment, used)`). `PrintBom` filtert mit `Ordinal` auf genau diese Menge
  (`PickingController.cs:592-597`). Bildschirm und Papier stimmen überein.
- **Randfall, genau bei „Alle Ziele“ wahrscheinlich:** Ist **keine** Zeile sichtbar (Stückliste ohne ein
  einziges Komm.-Ziel), lässt der Handler `visiblePositions` weg (`if (visiblePositions.length > 0)`).
  `PrintBom` wertet ein fehlendes Feld als „kein Filter“ (`if (!string.IsNullOrEmpty(visiblePositions))`)
  und druckt **alle** Positionen, unter dem Kopf „Komm.-Ziel=Alle Ziele (nur kommissionier-relevant)“.
  Das Papier behauptet dann einen Filter und zeigt alles. Die Lücke gab es schon vorher, AK 9 sagt aber
  „enthält nur die beim Klick sichtbar gewesenen Positionen“, und das stimmt dann nicht.

### Schwerpunkt 5 — SQL-Lauf im Testsystem (Ergebnis: nicht exakt die Negation, und falscher Tabellenname)

- Die Warteschlange zählt als HauptFA: `Sub = '' OR Sub = Order`. Die Negation lautet
  `Sub <> '' AND Sub <> Order`. Der SQL-Lauf prüft nur `Sub <> Order` und würde eine Zeile mit
  `Sub = ''` zurücksetzen, die die Warteschlange als HauptFA zeigt. Heute gibt es keine solche Zeile
  (siehe erster Durchgang, kein Schreibpfad erzeugt `''`), fachlich trifft der Lauf also dieselben
  Zeilen. Wörtlich deckungsgleich ist er nicht. Weil beides in SQL läuft, sind sich beide Seiten
  immerhin in Collation und Leerzeichen-Behandlung einig.
- **Tabellenname falsch:** Die Tabelle heißt `ProductionOrderPickingStatus` (Singular,
  `ApplicationDbContext.cs:445` `ToTable("ProductionOrderPickingStatus")`, `SQL/00_FreshInstall.sql:310`),
  nicht `ProductionOrderPickingStatuses`. Das ist nur der Name des `DbSet`. Der Lauf bräche laut ab,
  richtet also keinen Schaden an, blockiert aber den Testablauf.
- `ModifiedByWindows = NULL` widerspricht der Hausregel „bei jedem Update setzen“. Die SQL-Skripte des
  Projekts nutzen `SYSTEM_USER` (z. B. `SageProductionOrderSql.cs`).

### Weitere Risse im Nachziehen

- **AK 3 und TS-79 beschreiben das Baumverhalten wieder falsch.** Der Text sagt jetzt „Baugruppen-Eltern
  ohne eigenes Ziel werden ausgeblendet, **wenn keines ihrer Kinder sichtbar bleibt**“, TS-79 sagt „inkl.
  deren aufgeklappte Baugruppen-Eltern, sofern sie selbst ein Kind mit gesetztem Ziel haben“.
  `updateBomVisibility` (Z. 855-893) prüft den Filter aber für **jede Zeile einzeln** und kennt keine
  Kind-Abhängigkeit. Eine Baugruppen-Zeile ohne eigenes Ziel wird **immer** ausgeblendet. Ihre Kinder
  bleiben sichtbar, weil `expandAncestorsOfMatching` den Aufklapp-Zustand setzt, aber ohne
  Elternzeile. Ein Tester würde nach dem aktuellen Text einen Fehler melden, der keiner ist.
- **„Drei“ und „vier“ Warteschlangen-Abfragen gemischt:** Anforderung 6 und AK 20 sprechen von „drei“
  und zählen dann vier auf. Das sollte einheitlich „vier“ heißen, mit Begründung: Die ByPicker-Variante
  gehört dazu.
- **Leeres Dropdown:** `setupKommissionierzielDropdown` deaktiviert das Select2 bei
  `values.length === 0` (Z. 1073). Mit einem gespeicherten `!(leer)` und einer Stückliste ohne Ziele
  bleibt „Alle Ziele“ vorgewählt, das Dropdown ist aber gesperrt. Zurücksetzen geht dann nur über den
  Badge oder den Leerzustand. Das ist akzeptabel, sollte aber im Szenario stehen.

---

### BLOCKER

**B3 — Die Hintertür über das Profil-Freitextfeld ist nicht abgefangen (Schwerpunkt 2).** `!(leer),KA-02`
im Profil führt zur gegenteiligen Filterung, und das Dropdown zeigt zwei widersprüchliche Auswahlen.
**Frage an den Menschen:** Wo wird normalisiert?
Vorschlag, die kleinste sichere Variante: **Normalisieren beim Speichern**, in einer statischen Funktion,
die `AccountController` **und** `UsersController` aufrufen (heute zwei Inline-`Trim()`). Enthält ein durch
Komma getrennter Teil ohne Rücksicht auf Groß-/Kleinschreibung `!(leer)`, wird genau `!(leer)`
gespeichert. Dazu kommt eine sichtbare `WarningMessage` „‚Alle Ziele‘ lässt sich nicht mit einzelnen
Zielen kombinieren — gespeichert als ‚Alle Ziele‘.“ (Melden statt still). Zusätzlich vergleicht die
Select2-Vorbelegung ohne Rücksicht auf Groß-/Kleinschreibung.
Neues AK: „Profil mit `!(leer),KA-02` oder `!(LEER)` gespeichert → gespeichert wird `!(leer)`, Hinweis
sichtbar, Stückliste öffnet mit ‚Alle Ziele‘.“ Die Alternative, robust erst beim Lesen im JS
auszuwerten, verschiebt die Inkonsistenz nur. Die Datenbank hielte dann dauerhaft einen Wert, den keine
Oberfläche so darstellt.

### SOLLTE

**S9 — Eine Definition statt fünf (Schwerpunkt 1).** `ProductionOrder.IsHauptFa` als
`Expression<Func<ProductionOrder,bool>>` festschreiben, die Eigenschaft `IsSubFa` daraus kompilieren.
Count und Max auf die Wurzel `ProductionOrders` umstellen (1:1 belegt), sodass alle vier Abfragen
`.Where(ProductionOrder.IsHauptFa)` nutzen. AK 20 um einen InMemory-Test ergänzen: vier Fixtures
(`""`, `==`, `!=`, jeweils freigegeben) → `GetReleasedForPickingAsync`/`CountAsync` liefern genau die
HauptFA-Zeilen. Den Satz zur SQL-Collation und zu Leerzeichen am Ende (Schwerpunkt 1) als bekannte
Grenze in den Technischen Lösungsentwurf aufnehmen. Wird S9 **nicht** übernommen, dann gilt
mindestens der Äquivalenztest je Abfrage, den der Auftrag nennt.

**S10 — SQL-Lauf korrigieren (Schwerpunkt 5).**
`FROM [dbo].[ProductionOrderPickingStatus] s JOIN [dbo].[ProductionOrders] p ON p.Id = s.ProductionOrderId`
`WHERE NOT (p.SubOrderNumber = '' OR p.SubOrderNumber = p.OrderNumber) AND s.IsReleasedForPicking = 1`
Das ist wörtlich die Negation der Warteschlangen-Bedingung. Dazu `ModifiedByWindows = SYSTEM_USER`.
Vorgeschlagen ist außerdem ein vorgeschaltetes `SELECT COUNT(*)` mit derselben `WHERE`-Klausel, damit
im Testprotokoll steht, wie viele Zeilen betroffen waren.

**S11 — Druck-Randfall (Schwerpunkt 4).** Ist beim Klick auf „Drucken“ ein Filter aktiv, aber keine
Zeile sichtbar, öffnet der Handler **keinen** Druck. Stattdessen macht er den bestehenden Leerzustand
sichtbar (`checkKommissionierzielEmptyState()` bzw. Text „Keine sichtbaren Positionen — nichts zu
drucken“). `PrintBom` bleibt unverändert. Alternativ ginge ein Platzhalterwert in `visiblePositions`,
das wäre aber ein Trick. AK 9 um den Fall ergänzen.

**S12 — AK 3 und das TS-79-Szenario an den Code angleichen.** „Baugruppen-Zeilen ohne eigenes Komm.-Ziel
werden ausgeblendet, auch wenn ihre Kinder sichtbar sind. Die Kinder bleiben über das
Vorfahren-Aufklappen sichtbar, stehen dann aber ohne Elternzeile. Das ist das unveränderte Verhalten des
bestehenden Komm.-Ziel-Filters.“

**S13 — Begründung für „table-filter.js nicht anfassen“ präzisieren (Schwerpunkt 3).** Den
sessionStorage-Pfad beim Seitenaufbau benennen und festhalten, dass das mögliche Ein-Frame-Bild für
alle Bom-Filter schon besteht. Ein TS-Schritt dazu: „Stückliste mit aktivem ‚Alle Ziele‘ neu laden
(F5) → Endzustand gefiltert, Badge sichtbar.“

### HINWEIS

- **H6 — Zählung vereinheitlichen:** überall „vier Warteschlangen-Abfragen“ (Anforderung 6, Technischer
  Lösungsentwurf, AK 20).
- **H7 — Leeres Dropdown mit gespeichertem „Alle Ziele“:** Das gesperrte Select2 zeigt die Vorauswahl,
  zurückgesetzt wird über Badge oder Leerzustand. Als Negativfall in TS-79 aufnehmen.
- **H8 — Vorbestehend, nicht Teil dieser Spec:** Ein aus sessionStorage wiederhergestellter Komm.-Ziel-Wert
  ist klein geschrieben (`getActiveFilters` → `saveFiltersToStorage`). Die Select2-Vorbelegung verwirft
  dann ein Einzelziel wie `ka-02` still, weil die Option `KA-02` heißt. Die Vorbelegung ohne Rücksicht
  auf Groß-/Kleinschreibung aus B3 behebt das nebenbei.
- **H9 — Größe:** Mit S9-S11 kommen etwa zwei Dateien dazu (Hilfsfunktion zur Normalisierung,
  Repository-Umbau innerhalb derselben Datei). Das ist weiter in einem Dev-Lauf machbar.

**NACHBESSERUNG NOETIG: Profil-Freitext hebelt „Alle Ziele“ aus (B3); SQL-Lauf mit falschem Tabellennamen und nicht exakter Negation; AK 3 widerspricht erneut dem Baumverhalten.**


## ANTWORTEN auf den zweiten Pruefdurchgang (2026-09-25)

**B3 — ENTSCHIEDEN: Normalisieren beim Speichern.**
- EINE gemeinsame Funktion fuer `AccountController` und `UsersController`: Enthaelt der Wert `!(leer)`
  in beliebiger Gross-/Kleinschreibung, wird GENAU `!(leer)` gespeichert, Einzelziele entfallen.
  Sichtbarer Hinweis: "Alle Ziele schliesst Einzelziele aus — gespeichert wurde nur 'Alle Ziele'."
- Erkennung ohne Gross-/Kleinschreibung an ALLEN Stellen: Dropdown-Vorbelegung UND `bomMatchesFilter`.
  Sonst behandelt die Stueckliste `!(LEER)` anders als das Dropdown.
- AK + Testszenario: `!(leer),KA-02` und `!(LEER)` im Profil speichern -> gespeichert wird `!(leer)`,
  Hinweis sichtbar, Stueckliste zeigt alle Positionen mit Ziel.

**PUNKT 1 — uebernommen:** EINE Expression `ProductionOrder.IsHauptFa`. Count und Max ueber
`ProductionOrders` schreiben (1:1-Beziehung belegt). `IsSubFa` aus der Expression kompiliert. Ein Satz
in der Spec zur Grenze: InMemory-Tests belegen nur die C#-Semantik; SQL Server vergleicht ohne
Gross-/Kleinschreibung und ignoriert Leerzeichen am Ende — praktisch folgenlos, weil beide Spalten aus
demselben Wert stammen.

**PUNKT 5 — uebernommen:** korrigiertes SQL aus dem Pruefabschnitt (Tabelle
`ProductionOrderPickingStatus` im Singular, woertliche Umkehrung inkl. `''`, `SYSTEM_USER` fuer
`ModifiedByWindows`, Zaehl-SELECT vorab).

**PUNKT 4 — uebernommen:** Ist keine Zeile sichtbar, wird NICHT gedruckt; stattdessen der Leerzustand.
Ein Ausdruck aller Positionen unter "nur kommissionier-relevant" waere das Gegenteil seiner Ueberschrift.
AK dazu.

**PUNKT 3 — uebernommen:** "nicht anfassen" bleibt, Begruendung ehrlich um den Seitenaufbau ergaenzen
(ein Frame ungefiltert moeglich, vorbestehend fuer alle Stuecklisten-Filter).

**AK 3 / TS-79 — ehrlich beschreiben, NICHT aendern:** Eine Baugruppe ohne eigenes Ziel wird
ausgeblendet, auch wenn Kinder sichtbar sind; die Kinder stehen dann ohne Elternzeile. Das ist
beabsichtigt — fuer Kommissionierer ergibt sich eine Liste der zu holenden Teile. Im Testszenario
ausdruecklich als erwartetes Verhalten benennen, damit es niemand als Fehler meldet.

## FREIGABE-NACHTRAG (2026-09-25)

Zwei Korrekturen bei der abschliessenden Durchsicht vor der Freigabe, beide im Rumpf und in
`affected_code` eingearbeitet:

**1. Hinweis nur bei Aenderung der Bedeutung.** Die Antwort zu B3 verlangte den Hinweis fuer `!(leer),KA-02`
**und** `!(LEER)`. Das war ungenau: "Melden statt still" gilt fuer Aenderungen der **Bedeutung**. Wird
`!(LEER)` zu `!(leer)`, filtert die Stueckliste danach exakt dasselbe — ein Hinweis "schliesst Einzelziele
aus" waere dort schlicht falsch. Deshalb liefert `Normalize` jetzt `DroppedTargets` statt `WasNormalized`:
`true` nur, wenn Einzelziele verworfen wurden. Normalisiert wird weiterhin in beiden Faellen. AK 11 und
TS-79 entsprechend getrennt.

**2. Keine eigene Formel in den Views.** `_PickingLeitstandRow.cshtml` sollte `isHauptFaRow` "nach Vorbild"
der FA-Liste als `item.SubOrderNumber == item.OrderNumber` bilden — eine sechste Fassung der Regel, die
`ProductionOrder.IsHauptFa` gerade auf eine zurueckfuehren soll, und ohne den Leer-Fall. Stattdessen
`!item.IsSubFa`; die bestehende Variable in `_ProductionOrderRow.cshtml` wird mit umgestellt. Ist `item`
ein ViewModel statt eines `ProductionOrder`, wird dort ein Feld aus `IsSubFa` befuellt — nicht die
Formel neu geschrieben.

**Kein dritter Pruefdurchgang:** Der zweite hat ausschliesslich Nachziehpunkte gefunden, keine
Grundsatzfragen; beide Korrekturen hier sind Praezisierungen bereits getroffener Entscheidungen.

## QA-Nachweis (2026-09-25)

Geprüft im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch
`feature/2026-08-07-ideal-teile-1-5`), HEAD `787c5c90`, Diff-Basis `2d06d1f0` (letzter Commit vor
dieser Spec). 36 Dateien geändert, 1452(+)/521(-).

**1. Build**
```
dotnet build IdealAkeWms.slnx
→ Der Buildvorgang wurde erfolgreich ausgeführt. 0 Fehler, 12 Warnungen (alle vorbestehend:
  NU1902 MailKit/MimeKit, CS8602/CS8321 in unberührten Dateien).
```

**2. Tests**
```
dotnet test IdealAkeWms.Tests --no-build
→ Bestanden! Fehler: 0, erfolgreich: 1416, übersprungen: 1, gesamt: 1417 (5 s)
  (übersprungen: ProductionOrderEagerCreateAgentJobTests — vorbestehender Integrationstest, nicht
  Gegenstand dieser Spec)

dotnet test IDEALAKEWMSService.Tests --no-build
→ Bestanden! Fehler: 0, erfolgreich: 268, übersprungen: 0, gesamt: 268 (1 s)
```

**3. Harte Prüfungen**

- `grep -rEn "CascadeRelease|SetReleaseForOrderNumber|CountReleasedByOrderNumber" IdealAkeWms IdealAkeWms.Tests`
  (nur Quelltext, `--include="*.cs" --include="*.cshtml"`) → **0 Treffer**. Ohne Include-Filter
  meldet grep vier Treffer in `IdealAkeWms/bin|obj/Release/**/IdealAkeWms.dll` — das sind
  DLL-Binärartefakte eines älteren Release-Builds vor diesem Rückbau, keine Quelldateien; nach
  `dotnet build` (Debug, oben) unverändert stehen geblieben. Harmlos, kein Fund. AK 12 erfüllt.
- `git diff 2d06d1f0..HEAD -- IdealAkeWms.Tests/Repositories/ProductionOrderPickingStatusRepositoryTests.cs IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs`
  geprüft im Volltext: In beiden Dateien wurden ausschließlich die Kaskade-Tests entfernt
  (`SetReleaseForOrderNumber_*`, `CountReleasedByOrderNumberAsync_*`, `CascadeRelease*`,
  `CascadeReleasePreview_*`) und neue Sub-FA-Testfälle ergänzt
  (`Queue_ContainsOnlyHauptFa_EvenIfSubFaIsReleased`, `SetReleaseBatch_SkipsSubFa_InBothDirections`,
  `ToggleRelease_SubFa_NoChange_InBothDirections`, `BulkRelease_ReportsSkippedSubFa`). Keine
  bestehende `SetReleaseBatchAsync`-/`BulkRelease`-Fixture wurde verändert — harte Bedingung (AK 22)
  erfüllt.
- `ProductionOrder.cs`: genau eine Formel —
  `public static readonly Expression<Func<ProductionOrder,bool>> IsHauptFa = p => p.SubOrderNumber == "" || p.SubOrderNumber == p.OrderNumber;`,
  `IsSubFa`/`IsSubFaOf` daraus kompiliert. Grep über `IdealAkeWms` bestätigt: keine zweite
  Sub-FA-Vergleichsformel mehr außerhalb dieser Datei — `BomScope.ForOrder` nutzt `order.IsSubFa`,
  `ProductionOrderRepository.HierarchicalDataExistsAsync` nutzt `!AllAsync(ProductionOrder.IsHauptFa)`
  (beide Ruling R9 aus dem Umsetzungs-Ledger).
- `IdealAkeWms/AppVersion.cs` und `IDEALAKEWMSService/AppVersion.cs`: beide `"1.46.0"`.

**4. Fachliche Prüfung der Ledger-Abweichungen (progress.md, Rulings R1–R13)**

- **R2** (`BulkReleaseResult.SkippedSubFa` sammelt `SubOrderNumber`, nicht `OrderNumber` wie im
  Spec-Wortlaut Anforderung 6): im Code bestätigt
  (`ProductionOrderPickingStatusRepository.cs:255` `result.SkippedSubFa.Add(row.ProductionOrder.SubOrderNumber)`).
  Sachlich richtig — `OrderNumber` einer Sub-FA ist die HauptFA-Nummer, eine Meldung „übersprungen:
  100“ wäre irreführend. Deckt sich mit TS-79.13 (Text „<nummern>“, nicht spezifiziert welche Spalte)
  und AK 17 (spricht nur von „Sub-FA-Ids“, nicht von einer bestimmten Nummernspalte) — kein
  Widerspruch zur Spec, nur eine Präzisierung. Akzeptiert.
- **R4** (`BulkRelease` fasst `SkippedNoArticle`- und `SkippedSubFa`-Hinweis in einer
  `WarningMessage` zusammen statt den zweiten den ersten überschreiben zu lassen): im Code bestätigt
  (`PickingLeitstandController.cs` `warnings`-Liste, beide Zweige). Notwendig, weil sonst ein
  gemischter Bulk-Lauf eine der beiden Meldungen verlöre („Melden statt still“). Akzeptiert.
- **R7/R9** (weitere Sub-FA-Formeln in `BomScope.ForOrder` und
  `ProductionOrderRepository.HierarchicalDataExistsAsync` zusätzlich auf `IsHauptFa`/`IsSubFa`
  umgestellt, obwohl nicht explizit in `affected_code` gelistet; „“ gilt jetzt als HauptFA statt wie
  zuvor als Sub-FA): im Code bestätigt (siehe oben). Setzt die Spec-Leitplanke „EINE Definition“
  (Anforderung 6, Kritische Prüfung Schwerpunkt 1) konsequent um — vorher wären das zwei weitere,
  separat gepflegte Formeln geblieben. In Echtdaten folgenlos (Spalte `NOT NULL`, kein Schreibpfad
  erzeugt `""`). Akzeptiert, keine Nacharbeit nötig.
- **R8/R10** (`ReadOnlyBomBuilderTests`-Fixtures für Sub-FA-Fälle angepasst, weil AK 15 den
  interaktiven Sub-FA-Zugriff inzwischen verbietet): geprüft — betrifft ausschließlich
  `ReadOnlyBomBuilderTests` (FaWorklist-Pfad, bleibt für Sub-FAs unverändert erlaubt) und
  `PickingControllerTests`-Fälle, die eine hierarchische Mengen-/ViewModel-Prüfung ursprünglich an
  einer Sub-FA aufhängten. Die harte Fixture-Bedingung der Spec (AK 22, „Kritische Prüfung“ S1)
  bezieht sich wörtlich nur auf `SetReleaseBatchAsync`-/`BulkRelease`-Tests — diese sind unverändert
  (siehe Prüfpunkt oben). Der Sub-FA-Pfad bleibt weiterhin über `Bom_SubFa_RedirectsToIndexWithWarning`
  und die FaWorklist-Tests abgedeckt. Kein Verstoß gegen die harte Bedingung, sachlich zwingend (AK 15
  verbietet genau das, was die alten Fixtures voraussetzten). Akzeptiert.
- **FaWorklist-Link nur mit Vorbau-Zugriff** (`_ProductionOrderRow.cshtml`: der read-only
  `FaWorklist/Bom`-Link auf Sub-FA-Zeilen erscheint nur noch bei `Model.HasVorbauAccess`, statt wie
  im Spec-Text „auf jeder Sub-FA-Zeile“ unbedingt sichtbar zu sein; im Leitstand
  (`_PickingLeitstandRow.cshtml`) gibt es dort gar keinen Fallback-Link): per Diff bestätigt
  (vor dieser Spec unbedingter `else`-Zweig, jetzt `else if (Model.HasVorbauAccess)`). Das ist eine
  **echte, dokumentierte Abweichung** von „Unverändert: FaWorklist/Bom bleibt auf jeder Sub-FA-Zeile
  erreichbar“ (Out-of-Scope-Abschnitt) — als Minor-Befund im Final Review erkannt und **bewusst nicht
  in dieser Spec behoben**, sondern in der neuen Backlog-Notiz
  [[2026-09-25-leitstand-subfa-readonly-stueckliste]] (Hauptcheckout, vorhanden) festgehalten. Kein
  Sicherheitsverlust (der Link war ohnehin nur innerhalb der äußeren `Model.CanPick ||
  Model.HasVorbauAccess`-Zelle erreichbar), aber ein Nutzer mit reinem Picking-Recht (ohne
  Vorbau-Zugriff) sieht auf einer Sub-FA-Zeile der FA-Liste jetzt keinen Stückliste-Link mehr statt
  des read-only FaWorklist-Links. TS-79.11 benennt diesen Fall bereits explizit korrekt. **Akzeptiert
  als bekannte, im Brain festgehaltene Lücke — kein Blocker für Testbereit.**

**5. `docs/TESTSZENARIEN.md`**

- **TS-79** (Z. 8597–8845, 19 Szenarien TS-79.1–79.19 + Automatisiert-Übersicht +
  Out-of-Scope-Hinweis) vollständig, auf AK 1–22 gemappt, Manual-UAT-Schritte einzeln markiert.
- **TS-73** vollständig als zurückgebaut markiert: Kapiteltitel (Z. 7726/7753), TS-73.11–73.13,
  TS-73.14, TS-73.19, Schlusszeile (Z. 8575-Bereich) — jeweils mit „(zurückgebaut v1.46.0 — siehe
  TS-79)“ bzw. äquivalentem Verweis. Vorbild TS-71-Rückbau-Vermerk eingehalten.
- `secondbrain/tests/testszenarien-index.md` (Hauptcheckout) um Kapitel 79 ergänzt (in diesem
  QA-Lauf, siehe unten).

**6. Brain-Nachzug bereits durch den Dev-Lauf erledigt (Hauptcheckout, verifiziert, keine Nacharbeit
nötig):**
- `secondbrain/specs/freigegeben/2026-09-10-fa-liste-ausbau-matchcode-spec.md` Z. 44 — Callout „Block
  4/Tasks 12–14 zurückgebaut — v1.46.0“.
- `secondbrain/backlog/2026-09-25-picking-warteschlange-gruppierung-rueckbau.md` — vorhanden.
- `secondbrain/architektur/fallstricke.md` Z. 1199/1206 — beide Einträge (Platzhalter-Fallstrick +
  „vierte Umsetzung der Mini-Syntax“) vorhanden.
- `secondbrain/feature-map.md` Z. 537–556 — vollständiger Baustein-Block inkl. Folgenotizen.
- `secondbrain/changelog/2026-09-25-v1-46-0-kommissionierung-nur-hauptfa.md` — vorhanden.

**7. Akzeptanzkriterien — Verifikationsstatus**

| AK | Status | Wie geprüft |
|---|---|---|
| 1, 2 | Automatisiert | Bestehende Kommissionierlisten-Unit-Tests + `KommissionierRelevanzFilterTests` |
| 3 | Manual-UAT | Razor/JS-Baumverhalten am realen Datenbestand (TS-79.2) — Code-Pfad `updateBomVisibility` per Lesen verifiziert, Verhalten aber nur am Bildschirm sichtprüfbar |
| 4 | Manual-UAT | AKE-Flachmodus, kein Dropdown (TS-79.9) |
| 5, 6, 7, 8, 9, 10, 11 | Teils automatisiert (`KommissionierzielFilterWertTests`, `AccountControllerTests`/`UsersControllerTests`), Rest **Manual-UAT** — Select2-Verhalten, Badge-Text, Druckausgabe sind Razor/JS bzw. echter Druck-Tab (TS-79.2/79.3/79.5) |
| 12, 13 | Automatisiert | grep-Nachweis (oben) + Test-Diff-Nachweis (oben) |
| 14 | Manual-UAT | Sichtbarkeit der Bedienelemente je Zeile im laufenden Leitstand/FA-Liste (TS-79.11) — Code-Guards per Diff verifiziert |
| 15, 16, 17, 18 | Automatisiert | `Bom_SubFa_RedirectsToIndexWithWarning`, `ToggleRelease_SubFa_NoChange_InBothDirections`, `SetReleaseBatch_SkipsSubFa_InBothDirections`, `BulkRelease_ReportsSkippedSubFa` |
| 19, 20 | Automatisiert (Regressionsnachweis) + Manual-UAT-Sichtprüfung (TS-79.18) | `ReadOnlyBomBuilderTests` |
| 21 | Automatisiert | `ProductionOrderHauptFaTests`, `Queue_ContainsOnlyHauptFa_EvenIfSubFaIsReleased` |
| 22 | Automatisiert (harte Bedingung) | Diff-Prüfung oben — bestätigt |

**Grenze (unverändert aus der Spec, hier bestätigt statt wiederholt):** Kein Teil dieser QA-Prüfung
kann die SQL-Server-Übersetzung von `ProductionOrder.IsHauptFa` (Collation-/Leerzeichen-Verhalten)
oder tatsächliches Select2-/Druck-Verhalten im Browser zeigen — beides ist strukturell nur am
IDEAL-Testsystem prüfbar (dieselbe Einschränkung wie bei allen vorherigen IDEAL-Etappen dieses
Bündels, siehe [[fallstricke]] §8).

**Ergebnis:** Build grün, 1684 Tests grün (1416 + 268), 0 rot, 1 vorbestehend übersprungen. Alle
harten Prüfungen bestanden. Eine bekannte, im Brain dokumentierte Abweichung (FaWorklist-Link nur mit
Vorbau-Zugriff) — kein Blocker. **Status → Testbereit.**

## Manueller Testplan (Schranke 2 — abzuarbeiten ohne Rückfragen)

Vorbedingung für den gesamten Testplan: Deploy aus dem Worktree auf das Testsystem (siehe
Deploy-Abschnitt), danach den einmaligen SQL-Lauf auf dem Testsystem ausführen (siehe Schritt 0).
**Der SQL-Lauf verändert echte Daten auf dem Testsystem** (setzt `IsReleasedForPicking = 0` für
Sub-FA-Zeilen, die diese Spec ohnehin nie mehr anzeigt) — das ist beabsichtigt und Teil der
Deploy-Vorbedingung, kein Testschritt zum Verwerfen.

0. **SQL-Lauf (Vorbedingung, TS-79.16).** Auf dem Testsystem zuerst
   `SELECT COUNT(*) FROM [dbo].[ProductionOrderPickingStatus] s JOIN [dbo].[ProductionOrders] p ON p.Id = s.ProductionOrderId WHERE NOT (p.SubOrderNumber = '' OR p.SubOrderNumber = p.OrderNumber) AND s.IsReleasedForPicking = 1;`
   ausführen und die Zahl im Testprotokoll notieren. Danach den `UPDATE`-Block aus dem
   Deploy-Abschnitt (identische `WHERE`-Klausel, setzt `IsReleasedForPicking = 0`,
   `ModifiedBy = 'Testsystem-Bereinigung kommissionierung-nur-hauptfa'`, `ModifiedByWindows = SYSTEM_USER`)
   ausführen. **Echte Daten auf dem Testsystem werden dabei verändert.**
1. **Relevanzregel (TS-79.1).** Kommissionierliste öffnen, Positionsmenge mit dem Stand vor dieser
   Spec vergleichen (bzw. plausibilisieren, dass weiterhin nur Zeilen mit gesetztem `Kommissionieren`
   erscheinen). Erwartung: identisch.
2. **„Alle Ziele“ (TS-79.2).** Hierarchische Stückliste mit ≥2 Komm.-Zielen + mind. einer leeren
   Zelle öffnen (`/Picking/Bom/{HauptFA-id}`). Komm.-Ziel-Dropdown öffnen: erste Option „Alle Ziele
   (nur kommissionier-relevant)“, danach die echten Zielwerte alphabetisch. Auswählen → nur Zeilen
   mit gesetztem Ziel bleiben sichtbar; eine Baugruppen-Zeile ohne eigenes Ziel wird ausgeblendet,
   auch wenn ihre Kinder sichtbar bleiben (**das ist erwartetes Verhalten, kein Fehler**). Badge
   „Alle Ziele (nur kommissionier-relevant)“ erscheint. Reset über Badge UND über „Alle Filter
   zurücksetzen“ prüfen. Danach ein Einzelziel wählen → „Alle Ziele“ wird automatisch abgewählt, und
   umgekehrt (nie ein kombinierter Wert wie `!(leer),KA-02`).
3. **Druck-Leerzustand (TS-79.3).** Stückliste ohne ein einziges gesetztes Komm.-Ziel öffnen, „Alle
   Ziele“ wählen (keine Zeile sichtbar), auf „Stückliste drucken“ klicken. Erwartung: **kein** neuer
   Tab, stattdessen gelbe Box „Keine sichtbaren Positionen — nichts zu drucken.“ mit Reset.
   Gegenprobe: bei Treffern drucken → Kopf zeigt „Komm.-Ziel=Alle Ziele (nur kommissionier-relevant)“,
   nur sichtbare Positionen enthalten.
4. **Neues Ziel bleibt sichtbar (TS-79.4).** Mit aktivem „Alle Ziele“ ein neues Kommissionierziel per
   Sage-Sync anlegen lassen (oder simulieren). Erwartung: Position erscheint ohne erneutes Setzen des
   Filters.
5. **Persistenz + Normalisierung (TS-79.5).** `!(leer)` im Profil speichern → Stückliste öffnet mit
   „Alle Ziele“ vorgewählt. `!(leer),KA-02` im Profil UND in der Benutzerverwaltung (Create UND Edit)
   speichern → gespeichert wird exakt `!(leer)`, `WarningMessage` „Alle Ziele schließt Einzelziele
   aus — gespeichert wurde nur 'Alle Ziele'.“ erscheint. `!(LEER)` im Profil speichern → gespeichert
   wird `!(leer)`, **kein** Hinweis.
6. **Reload (TS-79.6).** Stückliste mit aktivem „Alle Ziele“ per F5 neu laden → Endzustand gefiltert,
   Badge sichtbar (ein kurzes ungefiltertes Zwischenbild ist kein Fehler).
7. **Leeres Dropdown (TS-79.7).** `!(leer)` als Standard gespeichert, Stückliste ohne ein einziges
   Komm.-Ziel öffnen → Dropdown gesperrt, „Alle Ziele“ vorgewählt, Leerzustand sichtbar, Reset über
   Badge oder Leerzustand-Knopf funktioniert.
8. **Legacy-Schreibweise (TS-79.8).** Einen Wert `!(LEER)` bzw. `!(LEER),KA-02` in `sessionStorage`
   simulieren oder direkt in der DB setzen, Stückliste öffnen → Dropdown zeigt exklusiv „Alle Ziele“,
   kein zusätzliches Chip.
9. **AKE-Negativfall Stückliste (TS-79.9).** Flache Stückliste öffnen → kein Komm.-Ziel-Dropdown.
10. **Rückbau Freigabe-Kaskade (TS-79.10).** `/PickingLeitstand` öffnen → kein „Alle Sub-FAs
    freigeben“-Button mehr in der Gruppen-Kopfzeile. `CascadeReleasePreview`/`CascadeRelease`-Routen
    direkt aufrufen (Browser/DevTools) → 404. Gegenprobe: „Alle Sub-FAs fertigmelden“ funktioniert
    unverändert.
11. **Freigabe/Picking nur am HauptFA (TS-79.11).** Hierarchische Gruppe mit mehreren Sub-FAs im
    Leitstand UND in der FA-Liste aufklappen. Erwartung: nur die HauptFA-Zeile zeigt
    Freigabe-Button/-Formular, Prioritäts-Input, Bulk-Checkbox und den interaktiven
    Stückliste-Link; Sub-FA-Zeilen zeigen keines davon. In der FA-Liste sieht ein Anwender mit
    Vorbau-Zugriff auf der Sub-FA-Zeile weiterhin den read-only `FaWorklist/Bom`-Link; **ohne**
    Vorbau-Zugriff bleibt die Zelle dort leer (bekannte, akzeptierte Abweichung, siehe QA-Nachweis
    oben und [[2026-09-25-leitstand-subfa-readonly-stueckliste]]) — kein Fehler, nicht melden.
12. **Direkter Zugriff Sub-FA (TS-79.12).** `GET /Picking/Bom/{sub-fa-id}` direkt aufrufen →
    Weiterleitung + `WarningMessage` „Kommissionierung erfolgt nur am HauptFA {OrderNumber}.“. `POST
    /PickingLeitstand/ToggleRelease` mit Sub-FA-Id — einmal unfreigegeben, einmal freigegeben →
    beide Male keine Zustandsänderung, sichtbare `WarningMessage`.
13. **BulkRelease gemischt (TS-79.13).** HauptFA- + Sub-FA-Ids gemeinsam freigeben → nur HauptFA wird
    freigegeben, Sub-FA übersprungen + gemeldet. Gegenprobe: eine bereits freigegebene Sub-FA
    gemeinsam mit HauptFA-Ids „zurücknehmen“ → Sub-FA bleibt übersprungen + gemeldet (keine stille
    Verarbeitung).
14. **AKE-Negativfall Freigabe (TS-79.14).** Flacher Modus, `BulkRelease`/`ToggleRelease` auf eine
    normale FA-Id → funktioniert unverändert.
15. **Warteschlange bereinigt (TS-79.16, nach Schritt 0).** `/Picking` öffnen → die in Schritt 0
    zurückgesetzte Sub-FA erscheint dort nicht (sie erschien auch vorher schon nicht, da die
    Warteschlangen-Abfragen strukturell filtern). Home-Kennzahl zählt sie nicht mit.
16. **Warteschlangen-Gruppierung (TS-79.17, Hinweis).** `/Picking` mit mehreren HauptFAs öffnen → je
    Gruppe nur noch eine Zeile. Kein Fehler, wenn Gruppen mit mehreren Sub-FAs ausbleiben.
17. **Unberührt (TS-79.18).** `FaWorklist/Bom` auf einer nie freigegebenen Sub-FA öffnen (read-only,
    über die FA-Struktur oder direkt) → weiterhin erreichbar. Glas-/Fremdbezug-/Lackierung-Checkboxen
    dieser Sub-FA weiterhin bedienbar.
18. **Hilfeseite (TS-79.19).** `/Help` öffnen, Abschnitte „Leitstand (Kommissionier-Freigabe)“ und
    „Kommissionierung“ (Stückliste) lesen → beide nennen „Freigabe und Stückliste nur am HauptFA“
    bzw. die Schaltfläche „Alle Ziele“ inkl. Profil-Eingabe `!(leer)`.

Bei jedem Abweichungsfund: **nicht** selbst nachbessern, sondern im Testprotokoll festhalten und an
den Menschen zurückmelden (Schranke 2 ist die menschliche Entscheidung über Merge/Nacharbeit).
