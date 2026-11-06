---
type: changelog
version: 1.73.0
date: 2026-11-06
---
# v1.73.0 — Sage-Abgleiche auf SData (View bleibt Rückfall), Kreditoren/Debitoren, Übergabe-Vorbereitung

Spec [[2026-11-06-sage-sdata-stammdaten-sync-spec]] (Epic, nimmt [[2026-10-06-kreditoren-stammdaten-spec]] auf),
Umsetzung [[2026-11-06-sage-sdata-stammdaten-sync-umsetzung]]. Bündel-Worktree, Basis `d91b1d1d` (v1.72.0).
Etappen-Commits `7fdcf656` (E0) … `e6821035` (E9), QA-Fix `c8c6b210`. E5 Stückliste und A4 AKE-FA flach zurückgestellt (G13).
Migrationen **SQL/98–107** (Nummern ab 102 eine tiefer als reserviert, weil E5 entfiel). TS-106.0–106.13.

## Kernpunkte

- **Quellen-Weiche je Abgleich** ([[0019-quellen-weiche-view-sdata-je-abgleich]]): `Sync:Quelle:<Bereich>` = `View` (Default) | `SData` | `Vergleich`
  für Lagerplätze, Lagerbestand, Artikel, FA-Struktur, Arbeitsplätze, Lagerbuchungs-Lookup. SQL-Reader unverändert hinter der Schnittstelle;
  Vergleich liest beide, schreibt nur View-Daten, Abweichungen im Lauf „Sage-Quellenvergleich“.
- **Gesperrt bis Prüfabfrage** (SData wirkt wie View + Warnung): Artikel bis P-R1, FA-Struktur bis P-F2. Arbeitsplätze im Modus SData legen bis
  P-A1 nichts an (Kürzel fehlt am AKE-Test).
- SData-Lese-Client `QueryAllAsync` (CommonWawiServicesResources, Paging ab `startIndex=0`, orderBy-Pflicht, Timeout 60 s, wirft statt leer).
- Neue Tabellen: `StorageAreas` (98), `ArticleSageDetails` (99), `ArticleAttributeValues.TextValue` (100), `ProductionOrderPositions` +
  `SageFaStates` (101, leer – Form des Rückmelde-Epics), Werkbank-Sage-Felder (102), `SagePpsMitarbeiter`/`Stillstandsgruende` +
  `BdeOperators.SageMitarbeiternummer` (103), `Creditors` (104), `Debitors` (105), `ArticleSuppliers` (106), `SageStammdatenOutbox` + Übergabe-Status
  an `StorageLocations`/`BdeOperators` (107).
- Oberflächen: `/Creditors`, `/Debitors` (masterdata_read/masterdata), Dienstleister-Meldekarte, Reiter „Sage-Zuordnung“ in der BDE-Personalpflege,
  Lagerort- und Übergabe-Spalten, Sage-Zusatzfelder in der Artikelinfo, Text-Merkmale „aus Sage“.
- Umbuchung über die bestehende Lagerbuchungs-Queue hinter neuem Schalter `SageLagerbuchungUmbuchungAktiv` (Default aus; Spec hatte keinen).
  Umbuchungen mit FA-Bezug gehen nie an Sage (QA B2, Spec F9).
- E9: Neue WMS-Lagerplätze/BDE-Mitarbeiter → Übergabe „Offen“ + Outbox-Eintrag; **kein Senden** (kein Sage-Service, G5).

## Deploy-Risiken

- 10 additive Migrationen, nicht daten-destruktiv; Reihenfolge über die Deploy-Liste (dreistellig ab 100 sortiert vor `11_`, G17). DB-Backup wie üblich.
- Default-Schalter = Verhalten unverändert. Neu sichtbar nach Deploy: Menü Kreditoren/Debitoren, Spalten Lagerort/Sperre/Übergabe, Info-Zeile
  „Quelle: View“ in den Abgleichsläufen, Übergabe „Offen“ für **neu** angelegte Lagerplätze/BDE-Mitarbeiter (auch an AKE).
- Vor `SData`/`Vergleich` an IDEAL: SData-Endpunkt kaufen (G2), Lizenz (G1); erste Läufe immer als `Vergleich` am AKE-Test.
- Viele Feldnamen/-typen nur angenommen (`TODO(P-S1)`), Fehlnamen enden als HTTP 500 (Lauf bricht ab, nichts wird genullt).
