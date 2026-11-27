---
type: spec
title: "Leitstand: Kaskade „Alle Sub-FAs fertigmelden“ nennt dieselbe Sub-FA-Zahl wie „+ n Sub-FAs“"
slug: 2026-11-26-leitstand-kaskaden-zaehler-ohne-hauptfa-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-leitstand-kaskaden-zaehler-ohne-hauptfa]]"
bug: "[[2026-11-26-leitstand-kaskaden-zaehler-ohne-hauptfa-bug]]"
uebersicht: "[[2026-11-26-leitstand-qa-befunde-f1-f3-uebersicht]]"
depends_on: ""
task: ""
jira: ""
worktree: ""
branch: ""
standort: IDEAL
affected_code:
  - "Bug-Record [[2026-11-26-leitstand-kaskaden-zaehler-ohne-hauptfa-bug]]"
  - IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs:62 + ProductionOrderRepository.cs:250-253 (CountByOrderNumberAsync, einziger Aufrufer CascadeDonePreview - ersetzen durch eine Sub-FA-Zählung)
  - IdealAkeWms/Data/Repositories/IProductionOrderBdeStatusRepository.cs:11-18 + ProductionOrderBdeStatusRepository.cs:32-61 (SetIsDoneBdeForOrderNumberAsync - Rückgabe = Anzahl Sub-FAs; Zeilenmenge laut Rückfrage 1/2)
  - IdealAkeWms/Controllers/PickingLeitstandController.cs:713-753 (CascadeDonePreview, CascadeDone - Log und Erfolgsmeldung)
  - IdealAkeWms/Views/PickingLeitstand/Index.cshtml:423-427 (Dialogtext), :524-535 (Skript füllt die Zahl), ggf. :250 und :153 (Rückfrage 3)
  - IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs:1062-1105, IdealAkeWms.Tests/Repositories/ProductionOrderBdeStatusRepositoryTests.cs:44-93
  - docs/TESTSZENARIEN.md (TS-69.15, TS-208.4, neues TS-210)
open_questions:
  - "1 HauptFA-Zeile in der Kaskade: Heute setzt „Alle Sub-FAs fertigmelden“ auch die HauptFA-Zeile selbst auf BDE fertig (ProductionOrderBdeStatusRepository.cs:37-40, Filter nur OrderNumber). Beibehalten und im Dialog als „HauptFA und n Sub-FAs“ benennen (Empfehlung), oder HauptFA-Zeile künftig auslassen?"
  - "2 Stornierte Zeilen: Kaskade zählt und setzt heute auch stornierte Sub-FAs (ProductionOrderRepository.cs:250-253, ProductionOrderBdeStatusRepository.cs:37-40), „+ n Sub-FAs“ zählt sie nicht. Stornierte künftig auslassen, damit Dialog = „+ n“ (Empfehlung), oder weiter mitsetzen und im Dialog getrennt nennen?"
  - "3 Gruppenansicht („Sub-FAs anzeigen“): Badge „n Sub-FAs“ am Gruppenkopf (Index.cshtml:250) und Zählzeile „y Sub-FAs“ (Index.cshtml:153) zählen die HauptFA-Zeile ebenfalls mit. Mitkorrigieren (Empfehlung: Badge zählt nur Sub-FA-Zeilen, Zählzeile heißt „y Zeilen“) oder so lassen?"
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

Wer an der IDEAL einen ganzen Auftrag per „Alle Sub-FAs fertigmelden“ abschließt, liest im Dialog eine Zahl, die er gegen die Zeile prüft.
Seit v1.99.88 steht „+ 38 Sub-FAs“ direkt neben dem Knopf, der Dialog sagt „39 Sub-FA(s)“. Eine Massenaktion, deren Bestätigungszahl nicht
zur Anzeige passt, untergräbt das Vertrauen in genau die Prüfung, für die der Dialog gebaut wurde (AK 12 der Kaskaden-Spec
[[2026-08-18-fa-liste-hierarchie-anzeige-spec]]).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (nur hierarchischer Modus; Dialog und Knopf gibt es nur bei `Model.Hierarchical && Model.CanPick`, `Index.cshtml:409`, `:497`)**

- Dialog, Erfolgsmeldung und Log der Kaskade nennen die Sub-FAs mit derselben Basis wie „+ n Sub-FAs“ (nicht storniert, inkl. erledigter,
  ohne HauptFA-Zeile) und nennen die HauptFA-Zeile, falls sie mitgesetzt wird, ausdrücklich.
- Zeilenmenge der Kaskade nach Rückfrage 1 und 2.
- Nach Rückfrage 3: Gruppen-Badge und Zählzeile der Ansicht „Sub-FAs anzeigen“.

**Out-of-Scope**

- Flacher Modus (AKE): keine Kaskade, kein Dialog.
- Offene-Buchung-Zähler (`BdeBookingRepository.cs:66-73`): bleibt; er zählt Zeilen derselben `OrderNumber` mit offener Buchung und steht als
  „Davon …“ unter der Gesamtzahl. Wird die Zeilenmenge nach Rückfrage 1/2 geändert, zieht er mit (siehe Lösungsentwurf 3).
- Bedeutung oder Leser von `IsDoneBde`: unverändert. Es gibt heute keinen fachlichen Leser (Bug-Record, Root Cause).
- Keine Gruppen-Rücknahme, kein neuer Endpunkt (AK 15 der Kaskaden-Spec bleibt).

## Fachliche Anforderungen

### Ist-Stand (belegt; Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, v1.99.89)

- Vorschau: `CascadeDonePreview` liefert `totalSubFas = CountByOrderNumberAsync(orderNumber)` (`PickingLeitstandController.cs:718`), das zählt
  **alle** Zeilen der `OrderNumber` (`ProductionOrderRepository.cs:250-253`, ohne Storno- und HauptFA-Filter). Einziger Aufrufer
  (Suche `CountByOrderNumberAsync`: Interface `IProductionOrderRepository.cs:62`, Controller `:718`, Test `PickingLeitstandControllerTests.cs:1064`).
- Ausführung: `SetIsDoneBdeForOrderNumberAsync` holt die Ids **aller** Zeilen der `OrderNumber` (`ProductionOrderBdeStatusRepository.cs:37-40`)
  und setzt deren `ProductionOrderBdeStatus.IsDoneBde = true` mit einem Zeitstempel, ein `SaveChanges` (`:44-60`); Rückgabe = Anzahl
  geschriebener Statuszeilen (`:60`). Damit wird **die HauptFA-Zeile mitgesetzt**, ebenso stornierte Zeilen.
- Meldung und Log sprechen von „{Count} Sub-FA(s)“ mit dieser Rückgabe (`PickingLeitstandController.cs:744-750`).
- „+ n Sub-FAs“: `SubFaAnzahl = subs.Count` (`LeitstandHauptFaZusammenfassung.cs:26`), `subs` = nicht stornierte Zeilen des Auftrags ohne die
  HauptFA-Zeile (`ProductionOrderRepository.cs:197-208`, `PickingLeitstandController.cs:250-251`). Basis laut L11 der Teil-2-Spec: nicht
  storniert, inkl. erledigter.
- Dialogtext: „HauptFA X — es werden n Sub-FA(s) auf BDE fertig gesetzt (alle Nachfahren, unabhängig vom aktuellen Filter)“ (`Index.cshtml:423-427`),
  Zahl aus `d.totalSubFas` (`:527`).
- Gruppenansicht: Badge `group.Items.Count` mit Titel „Anzahl Sub-FAs in dieser Gruppe“ (`Index.cshtml:250`) zählt die gefilterten Zeilen der
  Gruppe inkl. HauptFA-Zeile; Zählzeile „x Aufträge · y Sub-FAs“ mit `HierarchicalRowCount = totalRowCount` (`Index.cshtml:153`,
  `PickingLeitstandController.cs:293`), `totalRowCount` zählt alle Zeilen inkl. HauptFA-Zeilen (`ProductionOrderRepository.cs:69`).
- Eine Sub-FA-Definition: `ProductionOrder.IsHauptFa` / `IsSubFaOf` (`ProductionOrder.cs:24-40`) - keine zweite Formel.
- `IsDoneBde` hat keinen fachlichen Leser (Suche über `*.cs` ohne `Migrations/`, `*.cshtml`, `*.js`, `SQL/`): nur Schreiber, Anlage, Tests.
  Die Abweichung ist ein Anzeigefehler, kein Datenschaden.

### Soll

| ID | Anforderung |
|---|---|
| K1 | Die Zahl „Sub-FA(s)“ im Dialog hat dieselbe Basis wie „+ n Sub-FAs“: Zeilen der `OrderNumber`, die nach `IsSubFaOf` Sub-FA sind, nicht storniert (nach Rückfrage 2), inkl. erledigter. Bei 1064052 also 38. |
| K2 | Wird die HauptFA-Zeile mitgesetzt (Rückfrage 1), sagt der Dialog das: „HauptFA X und n Sub-FA(s) werden auf BDE fertig gesetzt …“. Hat der Auftrag keine HauptFA-Zeile (nur in „Sub-FAs anzeigen“ möglich), entfällt „und“: „n Sub-FA(s) …“. |
| K3 | Erfolgsmeldung und Log nennen dieselbe Sub-FA-Zahl wie K1 (Log: HauptFA, Anzahl Sub-FAs, ob die HauptFA-Zeile mitgesetzt wurde, Benutzer). Bei 0 Sub-FAs und ohne HauptFA-Zeile bleibt „keine Sub-FAs gefunden — nichts geändert“. |
| K4 | Die Kaskade schreibt genau die Zeilen, die der Dialog nennt (Rückfragen 1/2) - Vorschau und Ausführung verwenden dieselbe Zeilenmenge. |
| K5 | Nach Rückfrage 3: Gruppen-Badge zählt nur Sub-FA-Zeilen der Gruppe (`IsSubFa`), Zählzeile benennt die Zeilenzahl ehrlich. |
| K6 | Atomarität, gemeinsamer Zeitstempel, Audit-Felder, „nur auf true“ bleiben (AK 13-15 der Kaskaden-Spec). |

## Technischer Loesungsentwurf

Ponytail: kein neuer Endpunkt, keine neue Tabelle. Eine Zeilenmenge an **einer** Stelle, Vorschau und Ausführung lesen sie.

1. **Repository `ProductionOrderRepository`:** `CountByOrderNumberAsync` (einziger Aufrufer) ersetzen durch
   `Task<(int SubFas, bool MitHauptFa)> CountKaskadeAsync(string orderNumber)`: eine Abfrage
   `Where(o => o.OrderNumber == orderNumber && !o.IsCancelled).Select(o => o.SubOrderNumber)`, dann in C# `IsSubFaOf` zählen
   (eine Definition, `ProductionOrder.cs:39-40`). `!o.IsCancelled` nur, wenn Rückfrage 2 = auslassen. Altes Interface-Mitglied löschen.
2. **Repository `ProductionOrderBdeStatusRepository.SetIsDoneBdeForOrderNumberAsync`:** Zeilenfilter wie Schritt 1: `!IsCancelled` nach Rückfrage 2;
   `Id` und `SubOrderNumber` lokal laden (statt nur `Id`, `:37-40`) und die HauptFA-Zeile nur bei Rückfrage 1 = auslassen per `IsSubFaOf`
   herausnehmen. Rückgabe = Anzahl geschriebener **Sub-FA**-Statuszeilen (ebenfalls per `IsSubFaOf`); Signatur bleibt `Task<int>`.
3. **Offene Buchungen:** `CountSubFasWithOpenBookingForOrderNumberAsync` bekommt nur dann `&& !b.WorkOperation.ProductionOrder.IsCancelled`, wenn
   Rückfrage 2 = auslassen; HauptFA-Zeile zählt weiter mit, wenn sie mitgesetzt wird (der Text „Davon …“ bezieht sich auf alles, was gesetzt wird).
4. **Controller:** `CascadeDonePreview` liefert `{ orderNumber, totalSubFas, mitHauptFa, withOpenBooking }`; `CascadeDone` formuliert Meldung und Log
   nach K3 (`mitHauptFa` aus Schritt 1 vor dem Schreiben).
5. **View:** Dialogsatz `Index.cshtml:423-427` um ein `<span id="cascadeDoneHauptFa">` („und“) ergänzen, das das Skript (`:526-534`) nach
   `d.mitHauptFa` ein-/ausblendet. Nach Rückfrage 3: Badge `:250` `group.Items.Count(i => i.IsSubFa)`; Zählzeile `:153` „Zeilen“ statt „Sub-FAs“.
6. **Tests:** `ProductionOrderBdeStatusRepositoryTests` um einen Fall mit HauptFA-Zeile (`SubOrderNumber = OrderNumber`), zwei Sub-FAs und einer
   stornierten Sub-FA ergänzen (Erwartung laut Rückfragen 1/2); Repository-Test für `CountKaskadeAsync` mit denselben Daten (InMemory reicht: reine
   Gleichheit + C#-Zählung). Controller-Tests `:1062-1105` auf die neue Methode und das Feld `mitHauptFa` umstellen.
7. Frontend: Skill `frontend-design` aufrufen (Dialogtext, Badge) - Bestandsmuster, kein neuer Stil.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein Schema, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt. Nur LINQ.

Optional zur Information (WMS-DB IDEAL DEV, nur lesen): wie viele Aufträge haben stornierte Sub-FAs - zeigt, wie oft Rückfrage 2 die Zahl ändert.

```sql
SELECT po.OrderNumber,
       SUM(CASE WHEN po.SubOrderNumber <> N'' AND po.SubOrderNumber <> po.OrderNumber AND po.IsCancelled = 0 THEN 1 ELSE 0 END) AS SubFasAktiv,
       SUM(CASE WHEN po.SubOrderNumber <> N'' AND po.SubOrderNumber <> po.OrderNumber AND po.IsCancelled = 1 THEN 1 ELSE 0 END) AS SubFasStorniert,
       SUM(CASE WHEN po.SubOrderNumber = N'' OR po.SubOrderNumber = po.OrderNumber THEN 1 ELSE 0 END) AS HauptFaZeilen
FROM dbo.ProductionOrders po
WHERE po.OrderNumber = N'1064052'   -- oder ohne WHERE + HAVING SUM(...Storniert...) > 0
GROUP BY po.OrderNumber;
```

## Einstellungen

keine

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Unverändert: `SetIsDoneBdeForOrderNumberAsync` setzt je geschriebener Zeile `ModifiedAt` (ein gemeinsamer Zeitstempel), `ModifiedBy`,
`ModifiedByWindows` aus `ICurrentUserService` (`ProductionOrderBdeStatusRepository.cs:49-56`, Controller `:739-741`). Werden stornierte Zeilen
ausgelassen (Rückfrage 2), bekommen sie keinen neuen Audit-Stempel. Rollen unverändert: `[RequirePickingAccess]` an beiden Aktionen (`:712`, `:730`).

## Akzeptanzkriterien

1. Auftrag mit HauptFA-Zeile, 3 Sub-FAs, davon 1 storniert: Zeile zeigt „+ 2 Sub-FAs“, Dialog nennt 2 Sub-FA(s) (Rückfrage 2 = auslassen).
2. Derselbe Auftrag, Rückfrage 1 = beibehalten: Dialog sagt „HauptFA X und 2 Sub-FA(s)“; nach „Fertigmelden“ tragen HauptFA-Zeile und die 2
   aktiven Sub-FAs `IsDoneBde = 1` mit identischem `ModifiedAt`, die stornierte bleibt unverändert.
3. Erfolgsmeldung und Log nennen 2 Sub-FAs und die HauptFA; Log ist ein Eintrag.
4. DEV IDEAL 1064052: Dialogzahl = Zahl hinter „+ … Sub-FAs“.
5. Auftrag ohne HauptFA-Zeile (nur über „Sub-FAs anzeigen“, Gruppenkopf): Dialog ohne „HauptFA … und“, Zahl = Sub-FA-Zeilen.
6. Nach Rückfrage 3 = ja: Gruppen-Badge einer Gruppe mit HauptFA-Zeile und 2 sichtbaren Sub-FA-Zeilen zeigt „2 Sub-FAs“; Zählzeile sagt „Zeilen“.
7. Abbrechen im Dialog ändert nichts; Vorschau-Fehler zeigt „?“, Absenden bleibt möglich (Bestand TS-69.15).
8. AKE (Master aus): kein Knopf, kein Dialog, Seite unverändert.
9. Build und alle Tests grün; neue Repository-Tests mit HauptFA-/Sub-FA-/Storno-Zeilen.

## Test-Szenarien

Neues Kapitel **TS-210** in `docs/TESTSZENARIEN.md` (Nummer beim Dev-Lauf nachprüfen), Index `secondbrain/tests/testszenarien-index.md`:

- **TS-210.1** Zahl gleich: HauptFA mit Sub-FAs, „+ n“ ablesen, Dialog öffnen → gleiche Zahl, Text nennt die HauptFA (AK 1, 2, 4). Abbrechen.
- **TS-210.2** Ausführen: Fertigmelden → Erfolgsmeldung mit derselben Zahl; Prüfabfrage `ProductionOrderBdeStatus` des Auftrags (HauptFA, Sub-FAs,
  stornierte) laut AK 2.
- **TS-210.3** Gruppenansicht: „Sub-FAs anzeigen“, Badge und Zählzeile (AK 6); Kaskade aus dem Gruppenkopf → gleiche Zahl wie Badge ohne Filter.
- **TS-210.4** Negativ: Auftrag ohne Sub-FAs → kein Knopf (Bestand TS-208.4); Benutzer ohne Picking-Recht → kein Knopf; AKE → keine Kaskade.
- TS-69.15 und TS-208.4 („zählt HauptFA-Zeile mit, F-1“) auf den neuen Stand bringen.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | kein Epic | – | – |

## Deploy

*Vorläufig - der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Controller, Repository, View)
- **Service:** nein
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree)

## Offene Rueckfragen

1. **HauptFA-Zeile in der Kaskade:** Heute setzt „Alle Sub-FAs fertigmelden“ auch die HauptFA-Zeile auf BDE fertig
   (`ProductionOrderBdeStatusRepository.cs:37-40`). Empfehlung: beibehalten - die Kaskaden-Spec sagt „Haupt fertig ⇒ alle Sub-FAs fertig“
   (Z3) und testet die Fertigmeldung „am Haupt-FA“; der Dialog nennt sie künftig ausdrücklich („HauptFA X und n Sub-FAs“). Alternative: HauptFA-Zeile auslassen.
2. **Stornierte Zeilen:** Die Kaskade zählt und setzt heute auch stornierte Sub-FAs, „+ n“ zählt sie nicht. Empfehlung: auslassen - eine
   stornierte Sub-FA „BDE fertig“ zu melden, behauptet Arbeit an einem Auftrag, den es nicht mehr gibt, und nur so stimmen Dialog und „+ n“
   überein. Alternative: weiter mitsetzen und im Dialog getrennt nennen („davon k storniert“).
3. **Gruppenansicht:** Badge „n Sub-FAs“ (`Index.cshtml:250`) und Zählzeile „y Sub-FAs“ (`:153`) zählen die HauptFA-Zeile ebenfalls mit.
   Empfehlung: mitkorrigieren (Badge nur Sub-FA-Zeilen, Zählzeile „Zeilen“) - zwei Ausdrücke im selben View. Alternative: so lassen.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. HauptFA-Zeile in der Kaskade mitsetzen und benennen (Empfehlung) oder auslassen? →
2. Stornierte Sub-FAs auslassen (Empfehlung) oder mitsetzen und getrennt nennen? →
3. Gruppen-Badge und Zählzeile der Ansicht „Sub-FAs anzeigen“ mitkorrigieren (Empfehlung) oder so lassen? →
