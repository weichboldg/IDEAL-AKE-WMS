---
type: changelog
version: 1.99.23
date: 2026-11-14
---
# v1.99.23 — Schutz gegen Doppel-Absenden und unsichtbare Positionen

Buendel `feature/2026-08-07-ideal-teile-1-5`, feature-complete `510b3020` (wip `32dc3119` B1+K-1, `648d09fe` K-6, `1969e3b3` WE-1, `7cd357f0` BDE-1/Client/N-1). Spec
[[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec]], Bug [[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-bug]],
ADR [[0026-schreibaktionen-senden-die-absicht-der-server-prueft-am-schreibort]], /goal-Lauf [[2026-11-14-schutz-doppel-absenden-goal-lauf]].
Nicht gemergt.

## Kernpunkte

- **N-2:** `Nummernkreis.IstSqlFehler` prüft die ganze Ausnahme-Kette; ein EF-verpackter Deadlock wird wiederholt statt 500 (wirkt auch auf den
  BA-Nummernkreis).
- **K-1:** `PickingOrderWorkflow.AbschliessenAsync` prüft den Status im Wiederholversuch erneut; zweiter Abschluss → „… ist bereits … – nichts
  geändert.“, kein zweiter Folgeauftrag, FA-Status unverändert.
- **K-6:** `ToggleRelease(id, release, …)` mit Zielzustand; Zeilen- und Modal-Formular senden `release`. Schon im Zielzustand / ohne `release`
  → Warnung, nichts gespeichert, nichts storniert.
- **WE-1/WE-4:** `PurchaseOrderReceipt.Absendekennung` (Guid aus dem GET) an jedem Receipt; zweiter POST derselben Seite → „bereits gebucht“,
  ohne Kennung → „Seite ist veraltet“ (Freigabe-Antwort 5); gefilterter UNIQUE `(Absendekennung, BelPosId)` für den Gleichstand.
- **BDE-1:** `sp_getapplock` je Arbeitsgang vor der `IsFinal`-Prüfung in `ReportCompletionAsync`.
- **Client (Freigabe-Nachtrag):** globale Sperre aller POST-Formulare in `site.js` (Ausnahmen GET, `target=_blank`, `data-mehrfach-erlaubt`;
  Freigabe nach 10 s und `pageshow`), `window.wmsSperren` für die Abschluss-Knöpfe und „OK“ im BDE-Mengen-Modal. Opt-outs: keine nötig
  (Inventur 129 POST-Formulare, siehe Spec QA-Nachweis).
- **N-1:** `BomViewModel.VaterPosition` – Zeile nur versteckt, wenn ein vorhandener Vorfahre existiert.
- **N-B4-2:** CSV ohne Tausendertrenner (Dezimalkomma; ganze Zahlen nur in Zahl-Spalten), feste App-Kultur `de-DE` (Web; Dienst nicht, Antwort 2).

## Migration / SQL

`20261114080950_AddPurchaseOrderReceiptAbsendekennung`, **SQL/150** (Spalte `UNIQUEIDENTIFIER NULL`, gefilterter UNIQUE-Index, Guards je Batch);
additiv, FreshInstall zwei Stellen, Manifest `SQL/150` + Info `v1.99.23-SeitenNeuLaden`. AgentJobs nicht betroffen.

## Deploy-Risiken

- Offene Seiten von vor dem Update (Wareneingang, Leitstand, Handgeräte) werden mit „Seite ist veraltet“ abgewiesen – neu laden.
- Feste Kultur ändert auf einem Server mit anderer Kultur das Lesen von Dezimal-Eingaben (`asp-for` auf `decimal`). akenet01 ist mit
  Tausenderpunkt belegt; IDEAL soll `de-DE` sein und ist noch nicht produktiv (Gerald 09:01).
