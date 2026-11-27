---
type: changelog
version: 1.99.86
date: 2026-11-26
---
# v1.99.86 — Lagerbestellung: Hinweis „bereits als Fehlteil bestellt“

Spec [[2026-11-26-lagerbestellung-hinweis-fehlteil-bereits-bestellt-spec]], Lauf-Notiz [[2026-11-26-buendel-1-lagerbestellung-goal-lauf]].
Worktree `feature/2026-08-07-ideal-teile-1-5`, Commit `300dbf25`. **Keine Migration, keine neue Einstellung.** TS-207. Nicht gemergt.

## Kernpunkte

- Lagerbestellung-Entwurf zeigt je Position unter der Artikel-Nr einen nicht blockierenden Hinweis, wenn der Artikel in „Meine
  Fehlteile“ (eigene Werkbank, gleicher Bestelltyp, WillBeRestocked) schon wartet: höchstens drei Nummern + „(+N)“, nächster
  Liefertermin aus dem Einkaufs-Cache.
- Liefertermin-Texte aus `WarehousePickingController` in die gemeinsame Stelle `Services/LieferterminTexte.cs` verschoben
  (`JeArtikelAsync`, `Zelle`) — eine Quelle für Lager-Details, Druck und Hinweis.

## Deploy-Risiken

- Nur Web (Dienst nur Version). Ohne Einkaufs-Abgleich (`Sync:EinkaufBestellungenEnabled`) steht „Liefertermin unbekannt“.
- QA-Testdaten „QA207 …“ auf AKE_WMS_DEV #78–#90 und IDEAL_WMS_DEV #14/#15 bleiben stehen.
