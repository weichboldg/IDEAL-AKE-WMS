---
type: aufgabe
title: "FA-Liste und verwandte Ansichten hierarchiefaehig darstellen (Epic, Nachtrag Teil 7/8)"
status: InUmsetzung
spec: "[[2026-08-18-fa-liste-hierarchie-anzeige-spec]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
created: 2026-08-20
updated: 2026-08-20
---

# FA-Liste hierarchiefaehig (Epic)

Umsetzung der freigegebenen Spec [[2026-08-18-fa-liste-hierarchie-anzeige-spec]]. Am 2026-08-20 (Mensch)
zu einem **Epic** umstrukturiert (Umfang: 6 Views + Gruppen-Pagination + JS-Modul + Kaskade + Z4-Sweep +
Z1/Z3-Sync). Buendel-Worktree, kein Zwischen-Merge, QA erst Etappe E. Gates geprueft: Freigabe-Antworten
1–8 + Z1–Z5 beantwortet, BLOCKER BL1 in „ANTWORTEN auf die Kritische Pruefung (2026-08-20)" ausgeraeumt
(Kaskade-Wirt = PickingLeitstand-Gruppenkopfzeile).

## Etappen
| # | Etappe | Status |
|---|--------|--------|
| A | Anzeige-Fundament + ProductionOrders-Referenz-View + Z4-ERHEBUNG. **STOPP nach A.** | offen |
| B | 5 weitere Views (Anzeige-Teil) | offen |
| C | Kaskade Leitstand-Kopfzeile | offen |
| D | Z4-Sweep + Z3-Sync-Meldung + Z1-Regressionstest | offen |
| E | Testszenarien + Brain + qa-agent → Testbereit | offen |

## Verbindliche Entscheidungen (aus der Spec)
- **Wirksam nur bei Master `ProduktionsauftragHierarchisch == true`** (Lese-Muster wie
  `StandortEinstellungenController`: `IServiceSettingRepository.GetValueAsync(HierarchischeStrukturKeys.Master)`
  + IsTrue). Bei Master `false` **bit-identisch** zu heute (AKE-Regression, AK 10).
- **ALLE Nachfahren** je Gruppe als flache Zeilen (nicht nur direkte Kinder). Gruppen-Kopfzeile =
  `OrderNumber`, Zeilen-`order-number` = `SubOrderNumber`. Muster = `tbody`-je-HauptFA +
  colspan-Kopfzeile (wie `FaHierarchyKommissionierListen`) **plus** Chevron-Collapse (neu, aus OSEON).
- Neue Spalte `parent-sub-order-number` (`defaultHidden`), 6 viewKeys, additiv in ColumnDefinitions,
  keine Migration.
- Suche: erst `SubOrderNumber`, dann `OrderNumber`; **Auto-Expand bei Treffer in zugeklappter Gruppe**
  (F2, gefaehrlichster stiller Fehler) im neuen JS-Modul, KEIN Eingriff in table-filter.js.
- Pagination ueber **Gruppen** (TotalCount = Anzahl OrderNumber-Gruppen), Gruppe nie ueber Seiten getrennt.
- `SageMissingSince`-Badge: eigene Bedingung/Text, gleiche Render-Position wie IsCancelled-Badge,
  KEIN Feld-Mapping (Z5).
- Z2: Sortierung je Gruppe `SubOrderNumber` aufsteigend (Vorbelegung).
- Z4: „N Auftraege · M Sub-FAs" ueberall wo Auftraege gezaehlt werden; im Flachmodus eine Zahl.
- Kaskade (Etappe C): nur Leitstand-Gruppenkopfzeile, `IsDoneBde=true` alle Nachfahren, atomar,
  Dialog mit Offene-Buchung-Zahl (jede nicht beendete/nicht stornierte Buchung), ILogger; keine
  Gruppen-Ruecknahme; Zeilen-Toggle unveraendert. Kaskade schreibt an `ProductionOrderBdeStatus`.
- Z1: Sync fasst WMS-Zustaende nicht an (bereits erfuellt, nur Regressionstest in D).
- BDE-Cockpit ausgeklammert (Backlog); Kommissionierung kaskadiert NICHT.

## Fortschritt
- Setup (Epic-Umbau, Aufgabe) — erledigt.
- Etappe A — offen.
