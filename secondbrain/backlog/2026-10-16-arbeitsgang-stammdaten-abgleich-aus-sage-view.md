---
typ: feature
---
# Arbeitsgang-Stammdaten mit der Sage-View `vw_IDEAL_AKE_WMS_Arbeitsgaenge` abgleichen (IDEAL)

**Aufgenommen 2026-10-16 (Gerald), ausformuliert von der Koordination für die Spec.** Standort: IDEAL.

## Anforderung (vom Menschen)

- Die View `vw_IDEAL_AKE_WMS_Arbeitsgaenge` soll als **Abgleich zu unseren BDE-Arbeitsgängen** dienen, d. h. zu den
  Arbeitsgang-Stammdaten (BDE-Stammdaten › Reiter „Arbeitsgänge“, Tabelle `OperationConfigs`).
- Anlass (IDEAL-Test 2026-10-15): Unter „Nicht konfigurierte Kürzel“ stehen 15 IDEAL-Kürzel (BE, EG, EM, IF, ISO, KA, LS,
  PG, PL, PR, PU, SM, SW, SÄ, VM), die Tabelle enthält nur die 7 OSEON-Seed-Zeilen von AKE (BR, RE, RO, RS, SL, ST, ZB).

## Ist-Stand (Koordination, Bündel v1.60.2, nur gelesen)

- Die View liefert `Arbeitsgangnummer`, `Bezeichnung`, `AG` (varchar, `sys.columns` Gerald 2026-10-15), eine Zeile je
  Sage-Arbeitsplatz. Sie wird heute nur von der Werkbank-Anlage gelesen (`ProductionWorkplaceSyncService`,
  `Sync:ProductionWorkplaceSyncViewName`) -> `ProductionWorkplace` (Nummer, Name, `ArbeitsschrittCode`).
- Die Arbeitsgang-Stammdaten werden **von Hand** gepflegt (Entscheidung 3 vom 2026-10-09 in
  [[2026-10-09-arbeitsgang-stammdaten-vereinheitlichen-spec]]: ohne Eintrag Offset 0 + Liste „nicht konfiguriert“).
- „Nicht konfiguriert“ speist sich aus `OseonWorkOperations.Name` und `WorkOperations.OperationNumber`, also aus den
  **benutzten** Kürzeln, nicht aus der View (`OperationConfigRepository.cs:82-94`). Ein Kürzel, das noch an keinem FA
  hängt, erscheint dort nicht.
- `OperationConfig`: `OperationName` (Kürzel), `DisplayName`, `DueDateOffsetDays`, `IsExternalSystemRelevant`
  (Default true) — `Models/OperationConfig.cs:18-34`.

## Ausformulierung für die Spec (Vorschlag Koordination)

**Ziel:** Die View ist für IDEAL die führende Liste der Arbeitsgang-Kürzel. Der Abgleich sorgt dafür, dass jedes
Kürzel aus der View in den Stammdaten steht, und zeigt Abweichungen in beide Richtungen.

1. **Ort (entschieden Gerald 2026-10-16):** ein **eigener Sync „Arbeitsgang-Abgleich“** im SyncWorker, NICHT in der
   Werkbank-Anlage. Zwei neue ServiceSettings:
   - Schalter aktiv/inaktiv (Vorschlag `Sync:OperationConfigSyncEnabled`, Bool, Default **false**),
   - View-Name konfigurierbar (Vorschlag `Sync:OperationConfigSyncViewName`, String, Default
     `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]`, abgesichert über `FaHierarchySql.ValidateAndQuote`, fehlende View = Warnung
     + Skip wie bei den anderen View-Syncs).
   Default aus -> AKE bleibt unberührt, IDEAL schaltet von Hand ein. Ob zusätzlich das Master-Gate
   `ProduktionsauftragHierarchisch` gilt, entscheidet die Spec (Empfehlung: nein, eigener Schalter reicht).
2. **Fehlendes Kürzel** (in der View, nicht in `OperationConfigs`, Vergleich getrimmt, case-insensitiv): anlegen mit
   Offset 0, `IsExternalSystemRelevant = false`, Anzeigename siehe Frage 2. Audit `IDEALAKEWMSService`.
3. **Bestehendes Kürzel:** Offset, Fremdsystem-Kennzeichen und Anzeigename **nie** überschreiben (manuelle Pflege
   gewinnt).
4. **Kürzel nur in den Stammdaten** (nicht in der View): nicht löschen, im Reiter kennzeichnen („nicht in Sage“) und im
   Sync-Protokoll melden. Betrifft auf IDEAL heute die 7 OSEON-Seed-Zeilen.
5. **Ausschlussliste** `Sync:ProductionWorkplaceSyncAusschlussliste` (STO, XXX, x01, BS, BS2, FRE, AKE) gilt auch hier.
6. Karte „Nicht konfigurierte Kürzel“ bleibt (Kürzel aus FAs, die in keiner Quelle stehen = echter Fehler).

## Offene Fragen an Gerald (mit Empfehlung)

1. **Automatisch anlegen oder nur anzeigen?** Empfehlung: anlegen (Punkt 2). Der Offset bleibt Handarbeit, aber kein
   Kürzel fehlt mehr, und neue Arbeitsgänge in Sage kommen ohne Zutun an.
2. **Anzeigename:** Die View hat eine Zeile je Arbeitsplatz; ein Kürzel kann an mehreren Arbeitsplätzen mit
   unterschiedlicher Bezeichnung hängen (Beispiel aus der Spec 2026-09-21: `KA` = „Kanterei W1“). Empfehlung: Name aus
   `Bezeichnung` nur übernehmen, wenn alle Zeilen des Kürzels dieselbe Bezeichnung haben, sonst leer lassen und im
   Protokoll melden. Alternative: Die View liefert je `AG` einen eigenen Arbeitsgang-Namen (Änderung an der View, Gerald).
3. **OSEON-Seed-Zeilen auf IDEAL** (BR … ZB): löschen (von Hand, vor dem ersten Abgleich) oder stehen lassen und
   kennzeichnen? Empfehlung: löschen, IDEAL hat kein OSEON.

## Antworten / Befunde (2026-10-16)

- **Frage 2 durch Daten entschieden:** Prüfabfrage auf IDEAL-Test (Gerald 2026-10-16): je `AG` genau **1** Arbeitsplatz
  und **1** Bezeichnung (sichtbar Zeilen 7–26, z. B. IF Isolierfräsen, KA Kanten, M31 Montieren H3-01, SL Schweißen Laser,
  ST Stanzen, VM Vormontieren). Anzeigename = `Bezeichnung` beim Anlegen. Die Regel „nur bei eindeutiger Bezeichnung,
  sonst leer + Meldung“ bleibt als Absicherung für künftige Mehrfachzuordnungen.
- **Frage 3 (Gerald 2026-10-16): Die 7 OSEON-Seed-Zeilen (BR, RE, RO, RS, SL, ST, ZB) auf IDEAL werden gelöscht.**
  Achtung: `SL` und `ST` kommen auch in der IDEAL-View vor (SL = „Schweißen Laser“, ST = „Stanzen“); sie entstehen beim
  Abgleich mit den IDEAL-Namen neu. Löschen also vor dem ersten Abgleich, sonst bleiben die AKE-Namen (Schlosser) stehen
  (Punkt 3: bestehende Einträge werden nie überschrieben).
- **Frage 1 (Gerald 2026-10-16): automatisch anlegen** in den BDE-Arbeitsgängen (`OperationConfigs`), eigener Abgleich mit Schalter und konfigurierbarer View (Punkt 1).
- **Befund für die Spec:** Im WMS haben 87 Werkbänke einen `ArbeitsschrittCode` (Zählung Gerald 2026-10-15), die View
  liefert je Kürzel einen Arbeitsplatz (~30 Zeilen, Gesamtzahl ungeprüft). Woher die übrigen Werkbänke kommen (alte
  Tabelle `KHKPpsArbeitsplaetze`, manuell), klärt die Gegenrichtung von P3b — vor der Spec belegen.

## Prüfabfrage vor der Spec (Sage-DB, Gerald)

```sql
SELECT AG, COUNT(*) AS Arbeitsplaetze, COUNT(DISTINCT Bezeichnung) AS Namen,
       MIN(Bezeichnung) AS Name_Bsp1, MAX(Bezeichnung) AS Name_Bsp2
FROM dbo.vw_IDEAL_AKE_WMS_Arbeitsgaenge
GROUP BY AG ORDER BY AG;
```

Zeigt, wie viele Kürzel mehrere Arbeitsplätze und mehrere Namen haben (entscheidet Frage 2).
