---
type: changelog
version: 1.99.21
date: 2026-11-13
---
# v1.99.21 — Einkauf Bestellvorschläge E3: letzte Bestellmenge (3 Jahre), Fallback Sage-Bestellmenge

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commit `ca2f49ed`. Spec [[2026-11-08-einkauf-bestellvorschlaege-spec]] (Etappe 3, Freigabe-Nachtrag 2
2026-11-13 18:07). /goal-Lauf [[2026-11-13-buendel-v1-99-19-goal-lauf]]. Nicht gemergt.

## Kernpunkte

- Dienst: `ArtikelLieferantSyncService.LetzteBestellungAsync` nach dem Hauptschritt liest SData `EinkaufPositionenAllInfo` (EBB und EBZ,
  `Belegdatum` ab heute minus 3 Jahre, `orderBy=BelPosId`, alle Seiten), jüngster Beleg je Artikel + `A0Empfaenger` clientseitig (Datum↓, BelId↓,
  BelPosId↓), gespeichert `MengeBasis`. Kein Treffer mehr → Werte geleert; Lesefehler/0 EBB → Warnung, Werte bleiben. Counts `letzte_bestellung_*`.
- Rückblick 3 Jahre als Konstante `ArticleSupplier.LetzteBestellungRueckblickJahre` (Web + Dienst bräuchten sonst dieselbe Einstellung).
- Liste: Spalten „letzte Bestellmenge“/„letzte Bestellung am“; Regel 11 (n × L) und 13 a (anderer Lieferant, Hinweis) mit Daten.
- **Fallback** ohne Bestellung in 3 Jahren: Sage-Bestellmenge (`ArticleSageDetail.Bestellmenge`) wie L behandelt, Badge + Hinweistext
  „Fallback: Sage-Bestellmenge (keine Bestellung in 3 Jahren)“, filterbar, Banner mit Anzahl; nur wenn letzte Bestellungen schon gelesen sind
  (sonst Banner „noch nicht gelesen“). Ohne Sage-Bestellmenge: Fehlmenge bzw. Meldebestand, „Menge prüfen“.
- **Unbelegt:** Einheit der Sage-Bestellmenge (angenommen Lagereinheit) → Prüfabfrage Gerald (z. B. EK003604).

## Migration / SQL

`20261113171808_AddArticleSupplierLetzteBestellung`, **SQL/148** (`ArticleSuppliers.LetzteBestellmenge` decimal(18,3), `LetzteBestellungAm` date,
`LetzteBestellungBeleg` nvarchar(45)); additiv, FreshInstall zwei Stellen, Manifest `SQL/148`.

## Deploy-Risiken

DB vor Web und Dienst. Lauf „Artikel-Lieferanten“ wird um geschätzt 5–6 min länger (am AKE-Test ~37 Seiten à 1000).
