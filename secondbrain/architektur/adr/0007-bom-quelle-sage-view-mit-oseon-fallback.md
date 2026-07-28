---
type: adr
id: 0007
title: Stuecklisten primaer aus der Sage-View, Fallback auf OSEON-Stored-Procedure
status: accepted
date: 2026-03-10
supersedes: ""
superseded_by: ""
---

> **Nachtraeglich erfasst** (2026-07-27). Bestandsdokumentation; der Fallback wurde am
> 10.03.2026 ergaenzt („BOM-Datenquelle OSEON/TRUMPF Fallback").

## Kontext und Problem

Die Stueckliste (BOM) ist die zentrale Arbeitsgrundlage der Kommissionierung. Sie existiert in
zwei fuehrenden Systemen: in Sage (ERP, View `vw_AKE_Kommissionierung_StuecklistenDB`) und in
OSEON/TRUMPF (Fertigungssteuerung, ueber Stored Procedure). Sage ist die kaufmaennisch fuehrende
Quelle, liefert aber nicht fuer jeden Auftrag Daten — insbesondere bei Auftraegen, die nur in der
Fertigungssteuerung existieren, bleibt die View leer. Eine leere Stueckliste im Lager bedeutet:
der Auftrag kann nicht kommissioniert werden.

## Betrachtete Optionen

- **Nur Sage** — eine Quelle, klare Verantwortung, aber Auftraege ohne Sage-BOM sind im WMS
  arbeitsunfaehig.
- **Nur OSEON** — deckt die Fertigung ab, weicht aber von der kaufmaennisch fuehrenden Quelle ab.
- **Zusammenfuehren/Mergen beider Quellen** — theoretisch vollstaendig, praktisch nicht
  entscheidbar, welche Zeile bei Abweichung gilt.
- **Sage primaer, OSEON als Fallback, Quelle sichtbar machen** — deterministisch und fuer den
  Anwender transparent.

## Entscheidung

`BomRepository` fragt zuerst die Sage-View. Liefert sie keine Zeilen, wird auf die OSEON-SP
zurueckgefallen. Das Ergebnis ist ein `BomQueryResult(Items, DataSource)` — die **Quelle wird
mitgeliefert und in der UI angezeigt**, damit im Lager erkennbar ist, woher die Liste stammt.

Kein Merge: es gilt immer genau eine Quelle pro Abfrage. Vorrang hat Sage.

Darueber liegt `CachedBomRepository` (5 min MemoryCache, siehe
[[0001-repository-pattern-mit-decorator-fuer-caching]]). Fuer Auswertungen ausserhalb der
Abfrage (Artikelinfo „in welchen FAs kommt der Artikel vor", Lackierteil- und
FA-Arbeitsgang-Erkennung) gibt es zusaetzlich einen persistenten **BOM-Cache**
(`CachedBomHeader` / `CachedBomItem`), den der Windows-Service in einem Zeitfenster vorbefuellt
(`Sync:BomCacheWeeks`, `Sync:BomCacheMaxOrders`).

## Konsequenzen

**Positiv**
- Auftraege ohne Sage-BOM bleiben arbeitsfaehig.
- Der Anwender sieht die Herkunft der Liste — Abweichungen sind erklaerbar statt mysterioes.
- Kein nicht-entscheidbarer Merge-Konflikt.

**Negativ / Risiken**
- Zwei Abfragepfade mit unterschiedlichen Feldsemantiken; Fallstricke wie „Artikelnummer vs
  Ressourcenummer", „Artikelgruppe `940 - Kleinmaterial` vs `940`" und „OSEON `pa.ID` ist bigint"
  entstammen genau dieser Doppelquelle (siehe [[fallstricke]]).
- Der persistente BOM-Cache hat ein **Fenster und einen Cap**. Was nicht im Cache liegt, wird von
  abhaengigen Erkennungen (Lackierteil, FA-Arbeitsgaenge) nicht automatisch erfasst — manuelle
  Pflege bleibt immer moeglich, und der Cache-Lauf warnt, wenn der Cap greift.
- Die Cache-Befuellung laeuft ueber **raw SQL** (`BomCacheSyncService.ReadOpenOrdersInWindowAsync`),
  nicht ueber die gleichnamige EF-Repository-Methode. Wer die Auftragsauswahl aendert, muss beide
  Stellen pruefen — die EF-Methode ist Deko, die raw-SQL ist Produktion.
