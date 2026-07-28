---
type: changelog
version: 1.17.0
date: 2026-05-28
---
# v1.17.0 — Artikel-Sync-Erweiterung (UNION + Meldebestand + Full-Update)

Anlass: der Sage-Artikel-Sync lief nur fuer Artikel, die als Ressource in einer Stueckliste
auftauchen, und uebernahm bei Updates nur die Artikelgruppe.

- Der Sync liest zusaetzlich aktive Bestellartikel (`IstBestellartikel = -1 AND Aktiv = -1`).
- **Fallstrick dokumentiert:** Sage-Booleans sind BIT mit **`-1`** fuer TRUE (VB6-Legacy) —
  `= 1` liefert stillschweigend null Zeilen.
- Meldebestand kommt aus `KHKArtikelvarianten`; bestehende Artikel bekommen ein Full-Update aller
  4 Sage-Felder.
- `SQL/AgentJobs/02_Import_Artikel.sql` als DEPRECATED markiert (der Service uebernimmt).
