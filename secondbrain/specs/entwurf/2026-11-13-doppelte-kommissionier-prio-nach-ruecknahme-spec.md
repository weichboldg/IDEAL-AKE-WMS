---
type: spec
title: "Doppelte Kommissionier-Prio nach „Freigabe zurücknehmen“ (Varianten-Spec)"
slug: 2026-11-13-doppelte-kommissionier-prio-nach-ruecknahme-spec
status: Entwurf
created: 2026-11-13
updated: 2026-11-13
source_backlog: "[[2026-11-13-doppelte-kommissionier-prio-nach-ruecknahme]]"
herkunft: "[[2026-11-12-befunde-ake-regression-dev-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.16"
ts_vorschlag: "TS-133"
affected_code:
  - "IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs (GetMaxPickingPriorityAsync :211-222; bei Variante A zusätzlich SetReleaseAsync :82-86 und SetReleaseBatchAsync :278-282)"
  - "IdealAkeWms/Data/Repositories/IProductionOrderPickingStatusRepository.cs (Doc-Kommentar :29-33, :38)"
  - "IdealAkeWms.Tests/Repositories/ProductionOrderPickingStatusRepositoryTests.cs"
  - "IdealAkeWms/Views/Help/Index.cshtml (Priorisierung :1400-1404, Freigabe zurücknehmen :1406-1408)"
  - "IdealAkeWms/Views/Help/Changelog.cshtml + AppVersion.cs (Web + Service)"
  - "scripts/deploy/nachlauf.psd1 (Pruefung/DoppeltePrio, falls Rückfrage 2 = anzeigen)"
  - "docs/TESTSZENARIEN.md (TS-133 Vorschlag)"
open_questions:
  - "Variante: A = Prio beim Zurücknehmen löschen (erneute Freigabe bekommt MAX+1, FA rückt ans Ende); B = zurückgenommene, noch offene HauptFAs mit Prio zählen ins MAX (erneute Freigabe behält ihre alte Prio, neue FAs bekommen eine höhere); C = doppelte Prios fachlich zulassen, nur Hilfetext anpassen (Reihenfolge bei Gleichstand in /Picking nach Auftragsnummer-Id = Anlagezeitpunkt). Empfehlung B. Welche Variante? (freigabe_entscheidung)"
  - "Bestehende Doppelte in den Prod-DBs: (a) nur anzeigen – Pruefung-Eintrag im Nachlauf (Phase Nachher, anzeigen), der Leitstand korrigiert per Inline-Eingabe; (b) Einmal-Skript, das Doppelte umnummeriert; (c) nichts tun. Empfehlung (a). Vorher bitte die Prüfabfrage 1 auf AKE- und IDEAL-DB laufen lassen und die Trefferzahl mitliefern."
  - "Manuelle Inline-Eingabe einer Prio, die schon ein anderer FA hat (SetPriority prüft nicht): bleibt das erlaubt (z. B. zwei FAs bewusst gleich dringend) – oder soll der Leitstand eine Warnung sehen? Empfehlung: bleibt erlaubt, keine Änderung in dieser Spec."
  - "Nur falls Variante A: Kommissionieraufträge mit Fortschritt, die beim Zurücknehmen bestehen bleiben (PickingOrderWorkflow.FreigabeZuruecknehmenAsync), behalten ihre Prio in /Picking – sollen sie sie behalten (Empfehlung, sie werden ja noch abgearbeitet) oder ebenfalls auf leer gesetzt werden (rutschen in /Picking ans Ende)?"
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

# Doppelte Kommissionier-Prio nach „Freigabe zurücknehmen“

> [!info] Vorläufig
> Code-Belege neu gemessen im Worktree `feature/2026-08-07-ideal-teile-1-5` (Bündel v1.99.15) am 2026-11-13. Alle Pfade relativ zu
> `IdealAkeWms/`, sofern nicht anders angegeben. Varianten-Spec: `freigabe_entscheidung` bleibt leer, die Wahl ist Rückfrage 1.

## Ziel / Nutzen (das Warum)

Die Kommissionier-Prio legt fest, in welcher Reihenfolge Kommissionierer ihre Aufträge in `/Picking` sehen (1 = höchste). Die
Hilfeseite verspricht „Bei Freigabe wird automatisch die nächste **freie** Nummer vorgeschlagen“ (`Views/Help/Index.cshtml:1400-1404`).
Nach „Freigabe zurücknehmen“ und erneuter Freigabe stimmt das nicht: zwei offene FAs tragen dieselbe Zahl (DEV-Lauf 2026-11-12:
2614167 und 2614168 beide 32, Herleitung in [[2026-11-12-befunde-ake-regression-dev-spec]] Punkt 4b). Bei Gleichstand entscheidet
in `/Picking` dann die interne Auftrags-Id, nicht der Leitstand. Ziel: Die automatisch vergebene Prio ist wieder eindeutig unter den
offenen HauptFAs – oder es wird ausdrücklich festgelegt, dass Doppelte in Ordnung sind.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Regel für die automatische Prio-Vergabe bei Einzel- und Sammelfreigabe (eine gemeinsame Stelle `GetMaxPickingPriorityAsync`).
- Je nach Variante: Verhalten der Prio beim Zurücknehmen (einzeln und Sammel).
- Hilfetext „Priorisierung“/„Freigabe zurücknehmen“, Testszenario, Prüfabfrage und ggf. Pruefung-Eintrag im Nachlauf.

**Out-of-Scope**
- Manuelle Inline-Eingabe einer bereits vergebenen Zahl (Rückfrage 3; heute erlaubt, bleibt unverändert).
- Gleichzeitige Freigabe durch zwei Leitstand-Benutzer (Race, s. u. „Grenzen“) – kein Sperr- oder Unique-Mechanismus.
- `ReleasedAt`/`ModifiedAt` in UTC (`ProductionOrderPickingStatusRepository.cs:78`, `:88`, `:246`) – eigener Backlog
  [[2026-11-13-utc-schreiber-auf-lokalzeit]].
- Das Feld `FaHierarchyOrderInfo.Prio` (Sage-Kopf-Prio, IDEAL, `Models/FaHierarchyOrderInfo.cs:39`): das ist eine **andere** Prio, sie
  steuert die BDE-Terminal-Reihenfolge (`Services/ArbeitsgangStatusRegel.cs:49-51`, `:71-73`) und die Prio-Spalte der FA-Liste
  (`Controllers/ProductionOrdersController.cs:233`, `:301`, `:533`). Nicht betroffen.

## Fachliche Anforderungen

### Ist-Zustand (Beleg)

**Schreiber von `ProductionOrderPickingStatus.PickingPriority`** (vollständig; Suche nach `PickingPriority` in `*.cs`, `*.cshtml`,
`SQL/`, `IDEALAKEWMSService/`):

| # | Weg | Fundstelle | Verhalten |
|---|---|---|---|
| S1 | Einzelfreigabe | `Controllers/PickingLeitstandController.cs:395-401` → `Data/Repositories/ProductionOrderPickingStatusRepository.cs:75-81` | Hat der FA **keine** Prio: `MAX(exclude self) + 1`. Hat er eine (behaltene): bleibt sie (`newPriority = ps.PickingPriority`, `:396`; Repo setzt nur `if (priority.HasValue)`, `:80`). |
| S2 | Einzel-Rücknahme | `PickingLeitstandController.cs:395`, `:403-405` → Repo `:82-86` | Nur Kommissionierer wird geleert, **Prio bleibt stehen**. |
| S3 | Sammelfreigabe | Repo `SetReleaseBatchAsync` `:242-244` (MAX einmal vor der Schleife), `:270` | Nur FAs ohne Prio bekommen `++maxPrio`; FAs mit behaltener Prio behalten sie. |
| S4 | Sammel-Rücknahme | Repo `:278-282` | Wie S2: Prio bleibt. |
| S5 | Inline-Eingabe Leitstand | `PickingLeitstandController.cs:575-586` → Repo `SetPriorityAsync` `:123-135` | Beliebiger Wert inkl. leer, keine Eindeutigkeitsprüfung. Eingabefeld nur bei freigegebenen FAs sichtbar (`Views/PickingLeitstand/_PickingLeitstandRow.cshtml:269-274`, JS `Views/PickingLeitstand/Index.cshtml:552-566`). |
| S6 | Historisch | `SQL/60_ProductionOrderSplit.sql:198-205` | Einmalige Umkopie aus `ProductionOrders` (v1.x-Split), kein laufender Schreiber. |

Kein Schreiber im Dienst: `IDEALAKEWMSService/` enthält `PickingPriority` nicht (nur `PartRequisition`-Prioritäten in
`PartRequisitionEmailService.cs`). Kein aktiver AgentJob: in `SQL/AgentJobs/` nur ein Kommentar in `_archiv/01_Import_Produktionsauftraege.sql:41`.
Kein Unique-Index auf der Spalte (`SQL/00_FreshInstall.sql:432` nur Spaltendefinition).

**MAX-Regel** (`ProductionOrderPickingStatusRepository.cs:211-222`): HauptFA (`ProductionOrder.IsHauptFa`, `Models/ProductionOrder.cs:29-30`),
`IsReleasedForPicking = 1`, `!IsDone`, `!IsCancelled`, `PickingPriority != null`. `IsDonePicking` wird **nicht** ausgeschlossen.
Der Filter `IsReleasedForPicking` ist die Ursache: eine nach S2/S4 behaltene Prio zählt nicht mehr, ein anderer FA kann dieselbe Zahl
bekommen, und bei erneuter Freigabe (S1/S3) behält der erste FA seine Zahl → Doppelte. Bei S3 kann das sogar **im selben Sammellauf**
passieren (DEV: 2614167 behielt 32, 2614168 bekam 31 + 1 = 32).

**Kopien in Kommissionieraufträge** (`PickingOrder.PickingPriority`, `Models/PickingOrder.cs:51`):
- Anlage bei Freigabe: Kopie aus dem FA (`Services/PickingOrderWorkflow.cs:125`).
- Folgeauftrag „Teilweise“: Kopie aus dem Ursprungsauftrag (`PickingOrderWorkflow.cs:206`).
- Inline-Eingabe: Durchschreiben auf offene/in Arbeit befindliche Aufträge (`PickingOrderWorkflow.cs:272-273`, `:294-299`).
- Rücknahme: Aufträge ohne gepickte/umgebuchte Zeile → `Storniert`; Aufträge **mit** Fortschritt bleiben `Offen`/`InArbeit` und
  behalten ihre Prio (`PickingOrderWorkflow.cs:250-269`). Sie bleiben damit in `/Picking` sichtbar, obwohl der FA nicht mehr freigegeben ist
  (Liste filtert nur Auftragsstatus und FA erledigt/storniert, `Data/Repositories/PickingOrderRepository.cs:57-60`).
- Altbestand v1.88.0: `SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql:104-109` (einmalig, Kopie aus dem FA).

**Leser, die nach Prio sortieren** (wirkt auf die Picker-Reihenfolge):

| Leser | Fundstelle | Sortierung | Wirkung heute |
|---|---|---|---|
| `/Picking` Auftragsliste | `PickingOrderRepository.GetListeAsync` `:61`, Aufrufer `Controllers/PickingController.cs:137` | Prio vorhanden zuerst, Prio aufsteigend, dann `PickingOrder.Id` | **Einzige** Sortierung, die Kommissionierer sehen. Bei Gleichstand entscheidet die Id (= Anlagereihenfolge des Auftrags). |
| `GetReleasedForPickingAsync` | Repo `:174-185` | Prio, dann `ProductionDate` | Einziger Aufrufer `PickingLeitstandController.cs:505` nutzt nur die Id-Menge – Reihenfolge wirkungslos. |
| `GetReleasedForPickingByPickerAsync` | Repo `:187-203` | Prio, dann `ProductionDate` | Kein Aufrufer im Anwendungscode (nur `ProductionOrderPickingStatusRepositoryTests.cs:300`). |

Weitere Leser ohne Sortierung: Leitstand-Anzeige (`PickingLeitstandController.cs:148`; der Leitstand selbst sortiert nach `OrderNumber`,
`Data/Repositories/ProductionOrderRepository.cs:49`, `:70`, `:83`), Spaltenfilter `/Picking` (`PickingController.cs:89`, `:167`),
Zähler im Menü (`Views/Shared/_Layout.cshtml:62` → Repo `:205-209`, ohne Prio).

**Standort:** beide. Derselbe Codepfad läuft auf AKE (flach, `SubOrderNumber = OrderNumber` bzw. leer) und IDEAL (hierarchisch, nur HauptFA
freigebbar, `PickingLeitstandController.cs:366-372`, Repo `:253-257`); `IsHauptFa` gilt in beiden Modi.

### Varianten

| | A – Prio beim Zurücknehmen löschen | B – behaltene Prio zählt ins MAX (Empfehlung) | C – Doppelte zulassen |
|---|---|---|---|
| Änderung | S2/S4: `row.PickingPriority = null` im Rücknahme-Zweig (`:82-86`, `:278-282`) | S1/S3 gemeinsam: in `GetMaxPickingPriorityAsync` den Filter `IsReleasedForPicking` streichen (`:215`) – eine Bedingung, eine Stelle | Kein Code; Hilfetext ehrlich machen |
| Erneute Freigabe | FA bekommt `MAX + 1` → steht in `/Picking` **hinten** | FA behält seine alte Prio → steht wieder **an seinem alten Platz** | wie heute |
| Neue FAs nach einer Rücknahme | `MAX + 1` über Freigegebene | `MAX + 1` über Freigegebene **und** zurückgenommene offene → Zahl kann höher sein als jede im Leitstand sichtbare (Lücke, nur kosmetisch) | wie heute |
| Sammelfreigabe mit gemischter Auswahl (DEV-Fall) | keine Doppelte (zurückgenommener FA hat keine Prio mehr) | keine Doppelte (behaltene Prio zählt schon vor der Schleife) | Doppelte |
| Weiterlaufende Aufträge mit Fortschritt nach Rücknahme | behalten ihre Prio (Rückfrage 4) → **können** mit einem neuen FA kollidieren, weil die FA-Prio weg ist und nicht mehr ins MAX zählt | FA-Prio bleibt und zählt → keine Kollision | Kollision möglich |
| Verlust an Leitstand-Arbeit | manuell gesetzte Prio geht bei versehentlicher Rücknahme verloren | keine | keine |
| Leitstand-Reihenfolge / Picker-Reihenfolge | Rücknahme = „hinten anstellen“ | Rücknahme = „pausieren“ | Gleichstand nach Auftrags-Id |
| Restrisiko Doppelte | manuell (S5), Race, Altbestand | manuell (S5), Race, Altbestand | bleibt Regelfall |

**Empfehlung B.** Gründe: (1) eine Bedingung in der einzigen Funktion, über die Einzel- **und** Sammelfreigabe laufen – kein zweiter
Pfad bleibt kaputt; (2) erhält die manuell gepflegte Leitstand-Prio bei versehentlicher Rücknahme; (3) schließt als einzige Variante
auch die Kollision mit weiterlaufenden Aufträgen nach Rücknahme aus; (4) keine Datenänderung beim Zurücknehmen, kein Audit-Effekt.
Nachteil: die vorgeschlagene Zahl kann größer sein als jede im Leitstand sichtbare, weil zurückgenommene FAs ihre Prio unsichtbar
behalten (Eingabefeld nur bei freigegebenen, `_PickingLeitstandRow.cshtml:269`) – derselbe Effekt wie die „32“ aus Punkt 4b; der
Hilfetext erklärt ihn.

Weitere geprüfte Varianten, verworfen:
- **Sortierung nach Freigabezeitpunkt als Gleichstandsregel:** `/Picking` bricht Gleichstand bereits nach `PickingOrder.Id` (≈ Anlagezeit des
  Auftrags, `PickingOrderRepository.cs:61`); `ReleasedAt` liegt am FA in UTC und wird bei erneuter Freigabe überschrieben (`:78`, `:268`).
  Bringt gegenüber C nichts.
- **Umnummerieren/Verdichten nach jeder Freigabe:** verändert Zahlen, die der Leitstand bewusst gesetzt hat; viel Code für ein Sortierfeld.

### Grenzen (bleiben bei jeder Variante)
- Zwei gleichzeitige Freigaben lesen dasselbe MAX (kein Lock, kein Unique-Index) → Doppelte möglich. Bewusst nicht gelöst; Abhilfe wäre
  eine serialisierbare Transaktion um MAX+Schreiben.
- Ein FA mit Prio, der von erledigt/storniert wieder offen wird, zählt danach wieder; die Schreibwege von `IsDone`/`IsCancelled` wurden für
  diese Spec nicht geprüft.
- Bereits vorhandene Doppelte werden durch A/B nicht bereinigt (Rückfrage 2).

## Technischer Loesungsentwurf

Muster: Repository-Pattern (ADR 0001), keine neue Klasse, kein neuer Pfad.

**Variante B (Empfehlung)**
1. `ProductionOrderPickingStatusRepository.GetMaxPickingPriorityAsync` (`:211-222`): Bedingung `p.PickingStatus.IsReleasedForPicking`
   entfernen; übrig bleiben HauptFA, `!IsDone`, `!IsCancelled`, `PickingPriority != null`, optional `excludeProductionOrderId`.
   Kommentar dazu: warum zurückgenommene mitzählen (Verweis auf diese Spec).
2. Doc-Kommentare in `IProductionOrderPickingStatusRepository.cs:29-33` und `:38` anpassen („über alle offenen HauptFAs mit Prio,
   freigegeben oder zurückgenommen“).
3. `PickingLeitstandController` und `SetReleaseBatchAsync` bleiben unverändert (beide rufen die geänderte Funktion).
4. Tests (InMemory reicht, die Abfrage ist reines LINQ; bestehendes Muster `ProductionOrderPickingStatusRepositoryTests.cs:281-304`):
   - `GetMaxPickingPriorityAsync` zählt zurückgenommenen offenen HauptFA mit Prio; ignoriert erledigten, stornierten und Sub-FA.
   - `SetReleaseBatchAsync` mit Auswahl {zurückgenommen mit Prio 32, ohne Prio}, MAX der Freigegebenen 31 → zweiter bekommt 33 (DEV-Fall).
   - Bestehenden Test `:302-303` prüfen; er bleibt grün (alle dort freigegeben).
5. Hilfe `Views/Help/Index.cshtml:1400-1408`: „Eine zurückgenommene Freigabe behält ihre Priorität; bei erneuter Freigabe steht der Auftrag
   wieder an seinem Platz. Neue Freigaben bekommen eine höhere Nummer, auch wenn der zurückgenommene Auftrag im Leitstand gerade keine Prio
   anzeigt.“ Den Satz „Der Auftrag verschwindet aus der Kommissionierliste“ (`:1407-1408`) richtigstellen: Aufträge mit Fortschritt
   bleiben (wie `:1366-1367` schon sagt).

**Variante A (falls gewählt)**: `row.PickingPriority = null;` in beiden Rücknahme-Zweigen (`:82-86`, `:278-282`), MAX unverändert; Tests
für Rücknahme einzeln/Sammel; Durchschreiben auf behaltene Aufträge nur nach Rückfrage 4. Hilfetext: „Zurücknehmen löscht die Priorität.“

**Variante C (falls gewählt)**: nur Hilfetext `:1400-1404` („nächste Nummer“ statt „nächste freie Nummer“, Gleichstand = Anlagereihenfolge).

## Migrations-/SQL-Auswirkungen

Keine Migration, kein Schema, keine neue SQL-Nummer, `00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt (kein AgentJob schreibt das Feld,
s. o.). Bei Rückfrage 2 = (b) käme ein Einmal-Skript (Trockenlauf) hinzu – nicht empfohlen.

**Prüfabfragen (lesend, je Standort-DB einzeln ausführen: AKE-DB und IDEAL-DB; DEV und Prod):**

```sql
-- 1) Doppelte Prio unter offenen HauptFAs – freigegeben ODER mit behaltener Prio nach Rücknahme
;WITH k AS (
    SELECT po.OrderNumber, ps.PickingPriority, ps.IsReleasedForPicking, ps.IsDonePicking,
           ps.ReleasedAt, ps.ModifiedAt, ps.ModifiedBy,
           COUNT(*) OVER (PARTITION BY ps.PickingPriority) AS AnzahlGleichePrio
    FROM dbo.ProductionOrderPickingStatus ps
    JOIN dbo.ProductionOrders po ON po.Id = ps.ProductionOrderId
    WHERE ps.PickingPriority IS NOT NULL
      AND po.IsDone = 0 AND po.IsCancelled = 0
      AND (po.SubOrderNumber = N'' OR po.SubOrderNumber = po.OrderNumber)   -- = ProductionOrder.IsHauptFa
)
SELECT * FROM k WHERE AnzahlGleichePrio > 1
ORDER BY PickingPriority, IsReleasedForPicking DESC, OrderNumber;

-- 2) Wie 1, nur aktuell Freigegebene (das sieht die heutige MAX-Regel): in Zeile "WHERE" von 1)
--    zusätzlich "AND ps.IsReleasedForPicking = 1" ergänzen.

-- 3) Doppelte in /Picking: offene/in Arbeit befindliche Aufträge VERSCHIEDENER FAs mit gleicher Prio
--    (ein FA mit mehreren Bereichen hat legitim mehrere Aufträge mit derselben Prio)
SELECT o.PickingPriority, COUNT(DISTINCT o.ProductionOrderId) AS AnzahlFAs, COUNT(*) AS AnzahlAuftraege
FROM dbo.PickingOrders o
JOIN dbo.ProductionOrders po ON po.Id = o.ProductionOrderId
WHERE o.Status IN (10, 20)                 -- PickingOrderStatus.Offen / InArbeit (Models/PickingOrder.cs:12-13)
  AND o.PickingPriority IS NOT NULL
  AND po.IsDone = 0 AND po.IsCancelled = 0
GROUP BY o.PickingPriority
HAVING COUNT(DISTINCT o.ProductionOrderId) > 1
ORDER BY o.PickingPriority;
```

Erwartung nach Umsetzung B und ohne Bereinigung: neue Doppelte entstehen nur noch durch S5 (manuell) oder Race; vorhandene bleiben
sichtbar, bis der Leitstand sie korrigiert.

## Einstellungen

keine

## Audit-Feld-Auswirkungen

- Variante B: keine neue Schreibstelle; die bestehenden Freigabe-Schreiber setzen `ModifiedAt/ModifiedBy/ModifiedByWindows` wie bisher
  (`ProductionOrderPickingStatusRepository.cs:88-90`, `:284-286`). `ModifiedAt` dort in UTC – Thema von
  [[2026-11-13-utc-schreiber-auf-lokalzeit]], hier nicht ändern.
- Variante A: das Leeren der Prio liegt im bereits auditierten Rücknahme-Zweig; keine neuen Felder.
- Rollen/Zugriff unverändert: Freigabe, Rücknahme und Prio-Eingabe hinter `[RequireLeitstandAccess]`
  (`PickingLeitstandController.cs:359`, `:428`, `:574`), Klasse `[RequirePickingOrLeitstandAccess]` (`:11`). Keine neue Rolle, keine
  neue Listen-View (Listen-View-Pattern nicht berührt).

## Akzeptanzkriterien

Gemeinsam (jede Variante):
1. Einzelfreigabe eines FA ohne Prio vergibt weiterhin `MAX + 1`; Sammelfreigabe vergibt fortlaufend in Verarbeitungsreihenfolge.
2. Sub-FAs (IDEAL) und erledigte/stornierte FAs gehen nicht ins MAX ein (bestehender Test `:302-303` grün).
3. Inline-Eingabe (S5) verhält sich unverändert (jeder Wert oder leer wird gespeichert – `min="1"` gilt nur im Browser, der Server prüft nicht, `ProductionOrderPickingStatusRepository.cs:123-135`; und auf offene Aufträge durchgeschrieben).
4. Hilfetext „Priorisierung“/„Freigabe zurücknehmen“ beschreibt das gewählte Verhalten; Anwender- und Brain-Changelog nennen es.
5. Build und alle Tests grün.

Nur Variante B:
6. `GetMaxPickingPriorityAsync` liefert das Maximum über offene HauptFAs mit Prio unabhängig von `IsReleasedForPicking` (Unit-Test).
7. FA X freigeben (Prio N), zurücknehmen, FA Y freigeben → Y bekommt N + 1 (nicht N).
8. X erneut freigeben → X behält N; Prüfabfrage 1 zeigt für N genau eine Zeile.
9. Sammelfreigabe {X zurückgenommen mit Prio N = aktuelles Gesamt-MAX, Y ohne Prio} → Y bekommt N + 1 (DEV-Fall 2614167/2614168).
10. Nach Rücknahme von X mit weiterlaufendem Auftrag (Fortschritt) und Freigabe von Y zeigt `/Picking` keine zwei FAs mit derselben Prio
    (Prüfabfrage 3 ohne neue Treffer).

Nur Variante A (ersetzt 6–10):
6A. Einzel- und Sammel-Rücknahme setzen `PickingPriority` auf leer (Unit-Tests beide Wege).
7A. Erneute Freigabe vergibt `MAX + 1`; der FA steht in `/Picking` hinter den bereits freigegebenen.
8A. Verhalten weiterlaufender Aufträge nach Rückfrage 4.

Nur Variante C (ersetzt 6–10):
6C. Kein Codeunterschied; Hilfetext sagt „nächste Nummer“ und nennt die Gleichstandsregel.

## Test-Szenarien

Neues Kapitel **TS-133 (Vorschlag) — Kommissionier-Prio nach „Freigabe zurücknehmen“ (v1.99.16 Vorschlag)** in `docs/TESTSZENARIEN.md`,
Querverweis aus TS-4.10 (Prio ändern) und TS-120 (Kommissionieraufträge). Standort: beide (AKE DEV flach, IDEAL DEV hierarchisch).
Erwartungen hier für Variante B; bei A/C passt der Dev-Lauf die Spalte „Erwartet“ an.

Vorbedingungen: `LeitstandAktiv = true`; Testbenutzer mit Rolle `leitstand`; drei offene HauptFAs X, Y, Z mit Artikelnummer, keine
freigegeben; aktuelles MAX mit Prüfabfrage 2 (Maximum der Spalte) notiert = M; aktiver Standard-Kommissionierbereich.

- **TS-133.1 Einzel: Rücknahme und Neufreigabe.** X freigeben → Prio M+1. X zurücknehmen. Y freigeben → **M+2**. X erneut freigeben →
  X hat wieder M+1. Prüfabfrage 1: keine Zeile für M+1/M+2.
- **TS-133.2 Sammel mit gemischter Auswahl (DEV-Fall).** X freigeben (M+1), zurücknehmen. X und Z markieren, Sammelfreigabe → X behält
  M+1, Z bekommt **M+2**. Prüfabfrage 1 leer für diese Zahlen.
- **TS-133.3 Sammel-Rücknahme.** X, Y, Z freigeben, alle per Sammel-Rücknahme zurücknehmen; neuen FA W freigeben → W bekommt eine Zahl
  größer als die von X, Y, Z. Im Leitstand ist bei X/Y/Z kein Prio-Feld sichtbar (nur bei Freigegebenen).
- **TS-133.4 Weiterlaufender Auftrag.** X freigeben, in `/Picking` eine Zeile eines Auftrags von X umbuchen, X zurücknehmen (Meldung
  „Aufträge mit … bleiben bestehen“). Y freigeben → Y bekommt eine andere Prio als der weiterlaufende Auftrag von X; Prüfabfrage 3 ohne
  neuen Treffer. In `/Picking` steht der Auftrag von X vor dem von Y.
- **TS-133.5 Reihenfolge in /Picking.** Nach 133.1 zeigt `/Picking` (Status offen) die Aufträge von X vor denen von Y.
- **Negativ TS-133.6 Manuelle Doppelvergabe.** Bei Y per Inline-Eingabe die Prio von X eintragen → wird gespeichert, keine Sperre, keine
  Fehlermeldung (Out-of-Scope, Rückfrage 3). Prüfabfrage 1 zeigt jetzt diese Zahl doppelt. Danach zurückstellen.
- **Negativ TS-133.7 Erledigt/storniert zählt nicht.** Einen offenen HauptFA mit sehr hoher Prio (z. B. 999, per Inline-Eingabe) im
  Testsystem erledigt melden (bzw. stornierten FA verwenden), dann neuen FA freigeben → bekommt nicht 1000.
- **Negativ TS-133.8 Sub-FA (nur IDEAL).** Freigabe-Knopf an einer Sub-FA → Warnung „Kommissionierung erfolgt nur am HauptFA …“, keine
  Prio vergeben; Sammelfreigabe mit Sub-FA → „Übersprungen (Sub-FA …)“, MAX unverändert.
- **Negativ TS-133.9 Ohne Recht.** Benutzer ohne `leitstand`: Freigabe, Rücknahme, Prio-Eingabe nicht möglich (POST wird abgewiesen).

Danach `secondbrain/tests/testszenarien-index.md` nachziehen.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

*Vorläufig – der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Repository, Hilfe, Changelog, Version)
- **Service:** nein (`IDEALAKEWMSService` liest/schreibt `PickingPriority` nicht)
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):
  - Nur bei Rückfrage 2 = (a): `Id='Pruefung/DoppeltePrio'; Version='1.99.16'; Typ='Pruefung'; Firma='beide'; Phase='Nachher';
    Ausfuehrung='anzeigen'`, `Pruefung` = Anzahl Prio-Werte mit mehr als einem offenen HauptFA (Kern von Prüfabfrage 1:
    `SELECT COUNT(*) FROM (SELECT ps.PickingPriority FROM dbo.ProductionOrderPickingStatus ps JOIN dbo.ProductionOrders po ON po.Id =
    ps.ProductionOrderId WHERE ps.PickingPriority IS NOT NULL AND po.IsDone = 0 AND po.IsCancelled = 0 AND (po.SubOrderNumber = N'''' OR
    po.SubOrderNumber = po.OrderNumber) GROUP BY ps.PickingPriority HAVING COUNT(*) > 1) d`), `Text='Offene FAs mit gleicher
    Kommissionier-Prio (Altbestand vor v1.99.16): im Leitstand per Prio-Eingabe korrigieren; Liste mit Prüfabfrage 1 der Spec.'`.
  - Sonst: keine.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

1. **Variante:** A (Prio beim Zurücknehmen löschen, erneute Freigabe ans Ende), B (behaltene Prio zählt ins MAX, erneute Freigabe an alten
   Platz) oder C (Doppelte zulassen, nur Hilfetext)? Empfehlung **B**. → setzt `freigabe_entscheidung`.
2. **Bestehende Doppelte** in den Prod-DBs: (a) nur anzeigen über Pruefung im Nachlauf, Leitstand korrigiert per Hand; (b) Einmal-Skript
   zum Umnummerieren; (c) nichts. Empfehlung **(a)**. Bitte vorher Prüfabfrage 1 auf AKE- und IDEAL-DB laufen lassen und Trefferzahl nennen.
3. **Manuelle Doppelvergabe** per Inline-Eingabe weiter erlaubt (Empfehlung, keine Änderung) oder Warnung im Leitstand?
4. **Nur bei Variante A:** weiterlaufende Kommissionieraufträge (mit Fortschritt) nach Rücknahme – Prio behalten (Empfehlung) oder leeren?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Variante A / B / C →
2. Bestehende Doppelte (a) anzeigen / (b) Skript / (c) nichts – Trefferzahl Prüfabfrage 1 AKE / IDEAL →
3. Manuelle Doppelvergabe erlaubt lassen oder Warnung →
4. Nur bei A: weiterlaufende Aufträge Prio behalten oder leeren →
