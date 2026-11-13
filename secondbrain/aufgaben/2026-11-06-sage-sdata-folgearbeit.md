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

## Aus SData Teil 2 (v1.99.14, [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]], 2026-11-13)

- **appsettings-Rückfall entfernen** (Freigabe-Antwort 1): `SageLagerbuchung:Username/Password` in Dienst- und Web-appsettings gelten nur noch als
  Rückfall mit Warnung, solange `SData:Username` leer ist. Entfernen (Code + Dateien), sobald `Pruefung/SDataKennwortAppsettings` auf allen
  Zielsystemen 0 zeigt. ADR [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]].
- **DPAPI-Schutz des DataProtection-Schlüsselrings** (Freigabe-Antwort 5): Schlüssel liegen unverschlüsselt im Dateisystem; betrifft auch
  Anmeldung/Antiforgery → eigene Spec.
- **Fragen an DPS U1–U9** aus [[sage-sdata-benutzerfelder-lesetest]] stellen (u. a. `SpecificUserfields`/`CustomFields` als Ressource 500,
  `{Ressource}/$schema` 401, `KHKIFAuftraege` in keinem SData-Vertrag, IDEAL-Endpunkt L10).
- P-W1-Viewtexte (`vw_AKE_Kommissionierung_WAListe`, `vw_IDEAL_AKE_WMS_FAZusatzinformationen`) nach `SQL/sage-views/` legen (im Lesetest gelesen, nicht im Repo).
