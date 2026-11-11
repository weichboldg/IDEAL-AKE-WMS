---
type: changelog
version: 1.85.0
date: 2026-11-08
---
# v1.85.0 — Artikel: Lieferanten/Mengeneinheiten, Kreditor-Karte „Artikel dieses Lieferanten“ (Nachbesserung A2)

Spec [[2026-11-08-artikeldetails-lieferanten-anzeigen-spec]] (Nachbesserung 19:17), Lauf [[2026-11-08-buendel-2026-11-08-goal-lauf-2]].
Commits `5e3340ce`, QA-Reparatur `1dac3bcd`. **Migration:** `SQL/140_AddArtikelMengeneinheiten.sql` (rein additiv). TS-112.12–112.16.

## Kernpunkte

- Partial `_ArtikelSageKarten` (Sage-Zusatzfelder + Lieferanten) auch auf „Artikel bearbeiten“; Gruppe „Mengeneinheiten“ (Basis/Lager/VK/VP/EK
  mit Faktor und Nachkommastellen) aus SData `Artikel` bzw. `ArtikelLieferantInfo`.
- Kreditor-Details: Karte „Artikel dieses Lieferanten“ mit Server-Spaltenfilter + Pagination (K30601 ~9.000 Artikel).
- Artikelliste: optionale Spalten Hauptlieferant/Letzter Lieferant (Spaltenauswahl, Default aus).
- `Articles.Unit` bleibt Lagermengeneinheit (Abweichung zur Nachbesserung, Bestätigung Gerald offen).

## Deploy-Risiken

- SQL/140 vor Web/Dienst; Werte erst nach dem nächsten Artikel- und Artikel-Lieferanten-Lauf (vorher „—“).
- Offen: Prüfabfrage Q-5 (Richtung der Sage-Umrechnungsfaktoren).
