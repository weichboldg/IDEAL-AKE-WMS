---
type: changelog
version: 1.78.0
date: 2026-11-07
---
# v1.78.0 — EAN von Einkaufsartikeln scannen

Spec [[2026-11-07-ean-einkaufsartikel-scan-spec]], Lauf [[2026-11-07-buendel-2026-11-07-goal-lauf]]. Commit `8534da23`. **Keine Migration**,
keine Einstellung, nur Web (Dienst nur Version). TS-110.

## Kernpunkte

- `GET /api/articles/by-ean` (erst exakte Artikelnummer, dann `ArticleSageDetails.EANNummer`), Client-Zweig in `barcode-scanner.js` vor dem
  bisherigen Weg (8/12/13/14 Ziffern ohne QR-Trennzeichen), Auswahlfenster bei 2–5 Treffern.
- P-E1–P-E4 (AKE-Test lesend): Stamm-EAN nur `KHKArtikelVarianten.EANNummer`; nur 2 von 279.529 Varianten belegt (00726502, EK011488).

## Deploy-Risiken

- Keine. Wirkt nur, wo der SData-Artikellauf EANs geschrieben hat (AKE mit `Sync:Quelle:Artikel = SData`); an IDEAL ohne Wirkung.
