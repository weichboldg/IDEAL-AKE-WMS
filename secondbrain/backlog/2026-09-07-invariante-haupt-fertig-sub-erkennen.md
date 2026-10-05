---
typ: feature
---
# IDEAL: „Haupt-FA fertig ⇒ alle Sub-FAs fertig" — Verletzung erkennen & melden (Z3)

## Herkunft

Vertagt aus der Spec [[2026-08-18-fa-liste-hierarchie-anzeige-spec]], **Etappe D**, Punkt **Z3**
(Entscheidung Mensch, 2026-09-07: „Z3 ganz vertagen"). Die übrigen Etappen (A–C, Z4, Z1) sind
umgesetzt; nur dieses Sicherheitsnetz wurde herausgelöst, weil es den Deploy-Umfang berührt
(Service-Eingriff) und der Fall laut Spec **ausgeschlossen** ist.

## Fachlicher Fall

Die Kaskade „Alle Sub-FAs fertigmelden" (Etappe C, Commit `a305d6f`) stellt beim Setzen sicher:
Haupt-FA fertig ⇒ alle Nachfahren fertig. **ABER:** Der Materialisierungs-Sync (Sync-Regel 1,
`FaMaterializationSyncService`) legt neue Sub-FAs an. Taucht in Sage ein **neuer Sub-FA unter einem
bereits fertiggemeldeten Haupt-FA** auf, entsteht der verbotene Zustand „Haupt fertig, Sub offen" —
ohne Zutun eines Menschen.

Fachliche Einordnung (Spec, 2026-08-18): Nach dem initialen Erzeugen eines Auftrags kommen **keine
neuen Sub-FAs mehr dazu** → Fall gilt als **ausgeschlossen**, braucht **keine Auflösungslogik**.
Trotzdem erkennen und melden (das `SubFA = 0`-Muster galt auch als sicher und wurde vom ersten
echten Datenlauf widerlegt — das Banner hat es sichtbar gemacht).

## Verbindliche Regeln (aus der Spec, unverändert übernehmen)

- Der neue Sub-FA wird **offen** angelegt (bereits erfüllt — `FaMaterializationSyncService.cs:129`
  legt mit `IsDone = false` an; verifiziert H2 der Kritischen Prüfung).
- Der Haupt-FA wird **nicht** automatisch wieder geöffnet (widerspräche der Nicht-Kaskade bei der
  Rücknahme, Etappe C).
- Der Fall wird **protokolliert** (Log) **und in der Liste an der Gruppe sichtbar gekennzeichnet**
  (Badge). Ein Mensch entscheidet: Auftrag doch nicht fertig, oder Sage-Datenfehler.
- **Keine automatische Korrektur.**

## Warum herausgelöst (Umsetzungs-Fork, am Code belegt 2026-09-05)

1. **Zuverlässige Erkennung des Ereignisses braucht den Sync (Service-Projekt).** „Fertig" =
   `IsDoneBde` liegt auf der separaten Entität `ProductionOrderBdeStatus`, die der Sync heute
   **nicht lädt** (genau das schützt die Z1-Invariante). Um beim Anlegen zu wissen, ob der Haupt-FA
   fertig ist, müsste der Sync den BdeStatus des Haupt-FA (bzw. der Elternkette) laden → **Service-
   Produktionsänderung → Deploy würde `service: true`** (die Spec sagt `service: false`).
2. **Der Badge kann keinen persistierten Marker nutzen** (die Spec bleibt bewusst migrationsfrei).
   Er muss eine **Anzeige-Prüfung** sein: Haupt-FA-Zeile `IsDoneBde=true` **und** ≥1 Nachfahre
   `IsDoneBde=false`. Das erfordert `IsDoneBde` in der gruppierten Projektion (`LeitstandOrderRow`)
   + Identifikation der Haupt-Zeile (`SubOrderNumber == OrderNumber`) + **filter-unabhängige**
   Auswertung (der Default `showDone=false` blendet die fertige Haupt-Zeile aus). Nebeneffekt: die
   Prüfung erkennt jede Invariantenverletzung, auch eine manuelle BDE-Cockpit-Rücknahme — inhaltlich
   ok (Sicherheitsnetz), aber breiter als „neuer Sub-FA".

## Vorgeschlagene Umsetzung (wenn wieder aufgegriffen)

- **Service:** im Anlege-Pfad (`FaMaterializationSyncService`, `plan.ToCreate`) den BdeStatus des
  Haupt-FA prüfen; ist er `IsDoneBde=true`, `ILogger.LogWarning` + `run.LogWarningAsync` mit Haupt-FA,
  Sub-FA, Zeitstempel. Deploy dann `service: true`.
- **Web:** PickingLeitstand-Gruppenkopf (Wirt der Kaskade) zeigt ein Badge „Invariante verletzt —
  neuer/offener Sub-FA unter fertigem Haupt-FA". Repo-Bulk-Abfrage „welche dieser OrderNumbers haben
  Haupt-fertig-aber-Nachfahre-offen?" (filter-unabhängig).
- **Test:** Sync-Test (neuer Sub unter fertigem Haupt → Log); Controller/Repo-Test für die
  Badge-Bedingung.

## Bezug

- Spec: [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] (Z3, Z3b, AK-Kontext)
- Kaskade: Etappe C, Commit `a305d6f`
- Z1-Invariante (Sync fasst WMS-Zustände nicht an): Regressionstest
  `FaMaterializationSyncServiceTests.RunAsync_DoesNotResetIsDoneBde_OnExistingSubFa` (Service)
