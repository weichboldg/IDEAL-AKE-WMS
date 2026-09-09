---
type: bug
title: "FA-Materialisierung legt die 1:1-Statuszeilen nicht an — Sub-FA laesst sich im Leitstand nicht zur Kommissionierung freigeben"
status: offen
severity: hoch
created: 2026-09-09
gefunden_in: "Manual-UAT Schranke 2, IDEAL-Testsystem, Buendel feature/2026-08-07-ideal-teile-1-5 (v1.36.0)"
affected_code:
  - "IDEALAKEWMSService/Services/FaMaterializationSyncService.cs:117-133 (Create-Zweig legt NUR ProductionOrders an, keine ProductionOrderPickingStatus/ProductionOrderBdeStatus)"
  - "IDEALAKEWMSService/Services/SageImportService.cs:158-184 (einziger Eager-Create der beiden Statuszeilen; laeuft nur bei Sync:ProductionOrdersEnabled=true UND inserted > 0)"
  - "IdealAkeWms/Controllers/PickingLeitstandController.cs:332-334 (ToggleRelease: NotFound 'PickingStatus-Zeile fehlt.')"
  - "IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs:68-73 SetReleaseAsync, :95-100 SetAssignedPickerAsync, :127-132 SetPriorityAsync (werfen InvalidOperationException) und :218-250 SetReleaseBatchAsync (ueberspringt still)"
spec: "[[2026-07-29-standort-ideal-teil-7-spec]]"
---

## Symptom

Im Kommissionier-Leitstand laesst sich ein **materialisierter Sub-FA nicht zur Kommissionierung
freigeben**. Die Freigabe endet mit `HTTP 404 — "PickingStatus-Zeile fehlt."`

## Ursache (am Code verifiziert)

Zwei Tabellen haengen 1:1 an jedem Produktionsauftrag: `ProductionOrderPickingStatus` und
`ProductionOrderBdeStatus`. Sie werden **nicht** von der Anwendung beim ersten Zugriff erzeugt,
sondern beim Import eager angelegt.

- Die **FA-Materialisierung** (Teil 7) legt im Create-Zweig ausschliesslich die
  `ProductionOrders`-Zeile an. Die beiden Begleitzeilen fehlen — im ganzen Service gibt es dafuer
  keine Stelle.
- Der **einzige** Eager-Create liegt im **AKE-FA-Sync** (`SageImportService`), hinter
  `if (inserted > 0)`. Sein `MERGE` laeuft aber ueber **alle** `ProductionOrders`
  (`USING (SELECT Id FROM ProductionOrders)`), nicht nur ueber die neu eingefuegten.

Daraus folgt der eigentliche Punkt: **Solange `Sync:ProductionOrdersEnabled = true` stand, hat der
AKE-Sync die Statuszeilen der materialisierten Sub-FAs beilaeufig mitangelegt.** Das Freigeben
funktionierte also — aus einem Grund, der mit der Materialisierung nichts zu tun hat. Mit dem
**korrekten** IDEAL-Wert `false` (zwingend wegen Befund **H1**, siehe
[[2026-09-08-ideal-code-review-nachlese]]) faellt dieser Zufall weg und die Luecke wird sichtbar.

> Deshalb ist das **kein** Regressionsfehler der BOM-Bridge, sondern eine von Anfang an vorhandene
> Luecke in Teil 7, die eine falsche Konfiguration bisher verdeckt hat. Es ist derselbe
> Wirkmechanismus wie bei Kunde/Terminen (siehe Nachtrag in
> [[2026-09-08-uat-ergebnis-ideal-buendel-lauf-1]]): der AKE-Sync hat Loecher gestopft, die
> niemand als Loecher kannte.

## Auswirkung

| Pfad | Verhalten ohne Statuszeile |
|---|---|
| Leitstand „Freigeben" (`ToggleRelease`) | **HTTP 404** mit Klartext — der gemeldete Fall |
| Leitstand Prioritaet setzen, Kommissionierer aendern | **InvalidOperationException → HTTP 500** |
| Leitstand **Massenfreigabe** (`SetReleaseBatchAsync`) | **still uebersprungen**; die Erfolgsmeldung zaehlt nur die gefundenen Zeilen — der gefaehrlichste der drei |
| Haken HasGlass / Fremdbezug / Lackierung fertig / Kommissionierung fertig | funktioniert (`SetFieldAsync` legt die Zeile bei Bedarf an) |
| BDE-Rueckmeldung, Kaskade „Alle Sub-FAs fertigmelden" | `ProductionOrderBdeStatus` fehlt genauso — gleiche Klasse, eigener Nachweis noetig |

**Bewertung: merge-blockierend.** Der Picking-Workflow ist der Zweck der ganzen BOM-Bridge. Unter
der Konfiguration, die fuer IDEAL zwingend ist, laesst er sich fuer **keinen** materialisierten
Sub-FA starten.

## Loesungsvorschlag (noch nicht umgesetzt — Entscheidung des Menschen)

1. **Ursache beheben:** Der Create-Zweig der Materialisierung legt zu jedem neuen Auftrag beide
   Begleitzeilen mit an, mit denselben Vorgaben wie der AKE-Sync (alle Bool `0`, `CreatedBy` =
   Service-Name). Fachlich richtig, weil die Materialisierung fuer IDEAL genau die Rolle hat, die
   der Sage-Import fuer AKE hat.
2. **Bestand nachziehen:** einmaliges idempotentes SQL (`INSERT … WHERE NOT EXISTS`) fuer die
   bereits materialisierten Auftraege am Testsystem — sonst bleibt der vorhandene Bestand kaputt,
   auch wenn der Code stimmt.
3. **Zusaetzlich defensiv, nicht stattdessen:** `SetReleaseAsync` auf dasselbe Upsert-Muster wie
   `SetFieldAsync` heben und die Massenfreigabe fehlende Zeilen **melden** statt still
   ueberspringen. Punkt 3 allein wuerde das Symptom verstecken und `ProductionOrderBdeStatus`
   offen lassen.
4. **Nicht** den AKE-FA-Sync wieder einschalten. Das waere H1 und beschaedigt Mengen und Termine
   aller Sub-FAs.

## Testluecke, die das durchgelassen hat

In [[2026-09-08-ideal-code-review-nachlese]] steht bereits: „Der Materialisierungs-Service ist auf
DB-Ebene fast ungetestet. Fehlen: **Create-Zweig** (OrderNumber/SubOrderNumber/Parent/Audit)."
Genau dieser Zweig ist es. Ein Test „materialisierter Auftrag hat danach je eine Zeile in
`ProductionOrderPickingStatus` und `ProductionOrderBdeStatus`" haette den Fehler vor der Abnahme
gefunden.

## Bezug

[[2026-07-29-standort-ideal-teil-7-spec]], [[2026-09-08-ideal-code-review-nachlese]] (H1),
[[2026-09-08-uat-ergebnis-ideal-buendel-lauf-1]], [[2026-09-09-uat-protokoll-bom-bridge-chrome]]
(B-7/B-9 setzen einen freigegebenen Sub-FA voraus).
