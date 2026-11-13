---
type: changelog
version: 1.99.20
date: 2026-11-13
---
# v1.99.20 — Einkauf: Bestellvorschläge für Nachbestellartikel (Epic, Etappen E1/E2/E4/E6)

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commits `f5209692` (E1), `31ae0d69` (E2), `9b63dfc6` (E4), `f5f6bd5b` (E6), QA `cf3f41cb`. **Testbereit** (QA 2026-11-13, DEV AKE+IDEAL). Spec
[[2026-11-08-einkauf-bestellvorschlaege-spec]] (Epic; E0 lesend erledigt, **E3 blockiert** – Leseweise letzte Bestellmenge, **E5 blockiert** –
Dispo-Stapel schreibt an Sage). Nicht gemergt; /goal-Lauf [[2026-11-13-buendel-v1-99-19-goal-lauf]].

## Kernpunkte

- Neue Seite `/PurchaseProposals` (Menü „Einkauf → Bestellvorschläge“), nur Lesen + CSV. Rolle **`einkauf`** (admin oder einkauf), Gate
  `EinkaufAktiv` (AppSetting, **Default aus**, Gruppe „Einkauf“), `EinkaufOffeneBedarfe` (Mehrfachauswahl Lagerbestellungen/Bedarfsmeldungen).
- Eine Rechenstelle `PurchaseProposalCalculator`: Fehlmenge aus WMS-Bestand, Sage verfügbar, offenen Bedarfen, offen bestellt und Meldebestand.
  Regeln Rückfrage 11 (V = (⌊F/L⌋+1)×L) und 13 (a/b) implementiert; **ohne E3 ist L leer → Vorschlag = Fehlmenge, bei 0 der Meldebestand**,
  Hinweis in jeder Zeile.
- „Offen bestellt“ + „nächster Liefertermin“ aus dem Cache `SagePurchaseOrderLine` (EBB+EBZ) mit der vorhandenen Rest-Regel
  `ErfasstJeBelPos.Rest`; Positionen mit abweichender Einheit nicht gerechnet, Banner.
- Aufrunden auf die Mindestbestellmenge nur bei vergleichbarer Einheit **und** `UmrechnungsfaktorEK = 1` (E0-A2, konservativ), sonst Hinweis;
  `TODO(P2)` (Einheit der Mindestbestellmenge laut Sage-Maske offen).
- Feste Werte von `EinkaufOffeneBedarfe` liegen in `AppSettingKeys.EinkaufBedarfWerte` (verschachtelt), weil der Drift-Test jede
  String-Konstante in `AppSettingKeys` als Einstellungs-Key verlangt.

## Migration / SQL

Keine (SQL/148 war für E3 reserviert und bleibt frei, solange E3 blockiert ist).

## Deploy-Risiken

Web (Dienst nur Version). Rolle `einkauf` ohne Seed: Manifest `Pruefung/RolleEinkauf` legt sie an; Info `v1.99.20-EinkaufAktivieren`.
`EinkaufAktiv` bleibt aus; AKE erst nach erfolgreichen Läufen Artikel (SData), Kreditoren, Artikel-Lieferanten, Offene Bestellungen; IDEAL aus bis
zur SData-Schnittstelle (IDEAL_WMS_DEV hat 0 Hauptlieferanten).
