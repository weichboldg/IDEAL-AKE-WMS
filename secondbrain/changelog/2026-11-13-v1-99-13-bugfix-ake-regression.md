---
type: changelog
version: 1.99.13
date: 2026-11-13
---
# v1.99.13 — Bugfix aus der AKE-Regression: Filter in Anführungszeichen, FA-Hinweis Einbuchung, Bedarfsmeldung Ortszeit, Auftragsdruck Lagerplatz, Wagen-Modal

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commits `7cc8e684` (feature-complete), `a8dd9d2e` (Zusatzfund Lieferverzug-Protokoll).
Spec [[2026-11-12-befunde-ake-regression-dev-spec]], Bug [[2026-11-12-befunde-ake-regression-dev-bug]]. Nicht gemergt, nicht gepusht;
/goal-Lauf [[2026-11-13-buendel-v1-99-13-goal-lauf]].

## Kernpunkte

- **Spaltenfilter:** Ein Teil, der mit `"` oder `="` beginnt, reicht bis zum nächsten `"` und wird nicht an `;`/`,` getrennt
  (`"GL;0;0;0"`). Operatoren `=`, `%` am Rand, `!`, `(leer)` wirken weiter. Ein `"` mitten im Wert oder ohne Gegenstück bleibt ein
  normales Zeichen → Bestandsverhalten unverändert. Ein Kern: `ColumnFilterSyntax.SplitParts` (C#) = `splitParts` (JS), Fälle Q01–Q14
  in `column-filter-cases.json`, Filterhilfe (Popover + Hilfeseite).
- **Einbuchung, FA-Lagerplatz-Hinweis:** Variante A – Menge bleibt der gesamte Bestand am Platz (AK7 aus
  [[2026-08-05-wms-bugs-improvements-teil-1-spec]]); Hinweis sagt das ausdrücklich, Menge mit 3 Nachkommastellen. TS-2.13/2.15 angepasst.
- **Bedarfsmeldungen:** `CreatedAt`, `FulfilledAt`, `CancelledAt`, `ModifiedAt` (Web) und `EmailSentAt` (Dienst) in Ortszeit statt UTC.
  **Altdaten nicht umgerechnet** (Freigabe-Antwort 4). Prüfabfrage `dbo.PartRequisitions` (lesend, 2026-11-13): AKE Prod **3** Zeilen
  (19.05.–31.08.2026, 0 mit Mail), AKE_WMS_DEV 16, IDEAL_WMS_DEV 0, **IDEAL Prod: nicht abgefragt (Server/DB in keiner erreichbaren
  appsettings) → Gerald**.
- **Auftragsdruck:** offene/gepickte Zeilen ohne Quell-Platz zeigen den Vorschlag der Auftragsseite (`PickingController.QuellVorschlag`,
  eine Regel für Seite und Druck; Hauptplatz → höchste Menge → NAN).
- **Wagen „belegt“:** `StockMovementRepository.GetProductionOrdersAtLocationAsync` zerlegt die FA-Texte in Einzelnummern (dedupliziert,
  sortiert) – wirkt für alle drei Aufrufer; der Text wächst bei „Trotzdem buchen“ nicht mehr; Modal bricht um.
- **Zusatzfund:** `LieferverzugMailService` stempelt das Protokoll mit dem `jetzt` des Laufs (vorher `DateTime.Now`; Test lief nur am
  12.11. grün). Produktion übergibt `DateTime.Now` → kein Verhaltenswechsel.

## Migration / SQL

Keine.

## Deploy-Risiken

Web + Dienst (Dienst wegen `EmailSentAt`). Keine Einstellung, kein Manifest-Eintrag. Alte Bedarfsmeldungen zeigen weiter die UTC-Zeit.

## Folgearbeit

[[2026-11-13-doppelte-kommissionier-prio-nach-ruecknahme]], [[2026-11-13-utc-schreiber-auf-lokalzeit]].
