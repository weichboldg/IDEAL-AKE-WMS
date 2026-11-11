---
typ: feature
---
# Arbeitsgang-Stammdaten vereinheitlichen (OSEON + IDEAL) in den BDE-Stammdaten

**Aufgenommen 2026-10-09** – Gerald, aus der Klärung FA-Struktur Teil 2 / Rückfrage 8 (Termin-Offsets je
IDEAL-Kürzel). Vorbedingung für [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]].

## Wunsch (Gerald, sinngemäß)

- Die Arbeitsgang-Konfiguration (heute „Arbeitsgang-Konfiguration (OSEON)“ unter `/Settings/OperationConfig`)
  wandert als eigener Reiter **„Arbeitsgänge“** in die **BDE-Stammdaten** (neben Operatoren, Aktivitäten,
  Terminals).
- Eine gemeinsame Logik für OSEON und IDEAL: AG-Name (Kürzel), Anzeigename, Offset (Arbeitstage), Relevanz.
- **„OSEON-relevant“ heißt künftig „Fremdsystem-relevant“** – homogenisieren.
- Der Arbeitsgang-Abgleich (IDEAL-Struktur-Erkennung) prüft gegen diese Stammdaten.

## Befund Koordination (Bündel-Stand v1.48.0, 2026-10-09)

- Tabelle `OseonOperationConfigs` (`Models/OseonOperationConfig.cs`): `OperationName`, `DisplayName`,
  `DueDateOffsetDays` (Kommentar: relativ zum Stanztermin), `IsOseonRelevant`. Verwendet in 10 Dateien
  (OseonReporting, Tracking, Settings, OseonGroupViewModelBuilder u. a.).
- Seed `Program.cs:463-481` läuft auf **jeder** Datenbank, deren Tabelle leer ist – also auch auf IDEAL.
- Pflege heute nur Admin (`SettingsController.cs:10` `[RequireAdminAccess]`).
- BDE-Stammdaten: `BdeMasterDataController.cs:10-11` `[RequireBdeActive]` + `[RequireBdeShiftleadAccess]`.
  **Risiko:** Ist an einem Standort `BdeAktiv` nicht `true`, wäre die OSEON-Konfiguration dort nach dem
  Umzug nicht mehr erreichbar (Filter leitet auf Home um). Wert je Standort ungeprüft.
- IDEAL-Kürzel werden heute gegen `ProductionWorkplace.ArbeitsschrittCode` (Werkbank) abgeglichen
  (`WorkOperationStructureDetectionService.cs:78-85`); die Ist-Arbeitsgänge je FA stehen in `WorkOperation`.
  `WorkSteps` ist der AKE-Katalog für `FaWorkStep`, für IDEAL nicht genutzt (Service-Kommentar Z. 14-20).
- `WorkOperation.IsExternalSystem` („Fremdsystem-Rückmeldung“) existiert bereits – Begriff passt.

## Entscheidungen Gerald 2026-10-09

1. **BDE ist an keinem Standort aktiv** (`BdeAktiv`). Der Reiter „Arbeitsgänge“ muss daher **unabhängig von
   `BdeAktiv`** erreichbar sein (sonst wäre die OSEON-Pflege an der AKE und die IDEAL-Pflege nirgends
   erreichbar). Die übrigen BDE-Reiter bleiben an `BdeAktiv` gebunden.
2. **Ort: BDE-Stammdaten, Recht: Admin** (wie heute `/Settings/OperationConfig`), nicht Schichtleiter.
3. **Kürzel ohne Stammdaten-Eintrag:** Arbeitsgang wird trotzdem angelegt, **Offset 0**, Kürzel erscheint in
   einer Liste „nicht konfiguriert“ (Muster OSEON). Werkbank-Zuordnung über `ArbeitsschrittCode` bleibt.
4. **Offset neutral benennen:** „Offset (Arbeitstage) zum Bezugstermin“; Legende je Fremdsystem
   (OSEON: Stanztermin, IDEAL: FE-Termin).
5. „OSEON-relevant“ → **„Fremdsystem-relevant“**.
