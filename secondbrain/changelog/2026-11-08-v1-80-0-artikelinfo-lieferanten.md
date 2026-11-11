---
type: changelog
version: 1.80.0
date: 2026-11-08
---
# v1.80.0 — Artikelinfo: Lieferanten mit Kreditor, Hinweise, Verlinkung

Spec [[2026-11-08-artikeldetails-lieferanten-anzeigen-spec]], Lauf [[2026-11-08-buendel-2026-11-08-goal-lauf]]. Commit `ac92a15c`.
**Keine Migration**, keine Einstellung. Web; Dienst nur Version. TS-112 (TS-106.3.d überholt).

## Kernpunkte

- Artikelinfo: Karte „Lieferanten“ (Haupt-/letzter Lieferant als „Name (Konto)“ mit Link auf `/Creditors/Details`, „– inaktiv“ als Text;
  Tabelle aus `ArticleSuppliers` ⨝ `Creditors`, Hauptlieferant zuerst, ohne Einzelpreis).
- Hinweis statt ausgeblendeter Karte, wenn der Satellit bzw. die Lieferanten fehlen (nennt `Sync:Quelle:Artikel` / `Sync:ArtikelLieferantEnabled`).
- Artikelnummer in der Artikelliste verlinkt die Artikelinfo; Knopf „Artikelinfo“ auf „Artikel bearbeiten“.
- Neu: `IArticleRepository.GetSuppliersAsync` (`ArticleSupplierRow`), `ICreditorRepository.GetBySageKtosAsync`.

## Deploy-Risiken

- Keine. Nur lesend; Daten nur, wo der SData-Artikel- und Artikel-Lieferanten-Lauf gelaufen ist (AKE).
