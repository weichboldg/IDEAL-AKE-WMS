---
typ: aufgabe
status: offen
spec: "[[2026-11-06-sage-sdata-stammdaten-sync-spec]]"
---
# Folgearbeit aus dem SData-Epic (v1.73.0)

- Prüfabfragen P-S1 (Feldnamen/Typen u. a. `LagerbestaendeArtikelInfo`, Chargen/Serien, `PpsRessourcenPositionenInfo`, Positionsmengen/-zeiten, USER-Felder),
  P-R1 (IDEAL-Artikelfilter → Sperre `ArtikelQuelleSwitch` aufheben), P-F2 (→ `FaStrukturQuelleSwitch`), P-A1 (`USER_ArbeitsSchritt` am IDEAL-Endpunkt), P-S2 (Mandant).
- `Adressen`-Schema lesen → Name/Adresse für Kreditoren/Debitoren statt Matchcode.
- `SageStammdatenWorker` (Senden der Outbox), sobald DPS einen Lagerplatz-/Mitarbeiter-Service liefert (G5).
- Fachlich klären: neue Kommissionierwagen (`IsPickingTransport`) werden „Übergabe offen“ und bleiben es, wenn es sie in Sage nie gibt – ausnehmen?
- `/SageBookingQueue`: `GetForMonitoringAsync` lädt `SourceStorageLocation` nicht (Umbuchung zeigt nur Ziel).
- Zurückgestellt (G13): E5 Stückliste BOM-Cache, AKE-Reader FA flach (A4) – bei AKE-Freigabe.
- Sage AKE-Test: PPS-Mitarbeiter anlegen (für TS-106.8).
