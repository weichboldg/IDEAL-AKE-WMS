---
type: backlog
typ: feature
status: in-spec
jira: ""
created: 2026-11-28
standort: IDEAL
quelle: "Chat Gerald 2026-11-28: „Deaktivieren soll auch möglich sein“ (IDEAL-Sperre P-R1 des Artikel-Quellenschalters)"
spec: "[[2026-11-28-artikel-sdata-sperre-p-r1-abschaltbar-spec]]"
---
# Artikel-Abgleich: IDEAL-Sperre P-R1 für SData abschaltbar

## Wunsch
Die Sperre, die an IDEAL `Sync:Quelle:Artikel = SData` wie `View` wirken lässt, soll ein Admin aufheben können
(„Deaktivieren soll auch möglich sein“). Standard bleibt: Sperre aktiv.

## Befund (2026-11-28, nur gelesen)
- Sperre: `IDEALAKEWMSService/Services/Sdata/ArtikelQuelleSwitch.cs:43-49` → Regel `IdealAkeWms/Services/SageSdata/SageQuelleSperre.cs:17-25`
  (gesperrt, sobald `Sync:ArticlesViewName` gesetzt ist). Grund P-R1: Der IDEAL-Filter steht nur im Viewtext von `vw_IDEAL_AKE_WMS_Artikel`,
  `SdataArtikelReader` baut ihn nicht nach.
- Hintergrund: [[2026-11-06-sage-sdata-stammdaten-sync-spec]] (G16, P-R1), [[services]] (Sage-SData-Abgleich),
  [[0008-servicesettings-db-first-mit-typisiertem-katalog]], [[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]].

## Vorgaben
- Neue ServiceSetting in `ServiceSettingDefinitions.All`, Default = Sperre aktiv.
- Aufgehoben: Warnbanner in `/ServiceSettings` + Sammelmeldung je Lauf im Aktivitäts-Protokoll (gelesen SData vs. View, soweit verfügbar).
- Minimal: `SageQuelleWeiche`/`SageQuelleSperre` wiederverwenden, kein Filter-Nachbau (bleibt P-R1).
- Viewtext unbekannt (kein Zugriff auf IDEAL_TEST2026 mit Geralds Konto) → Prüfabfrage für den Menschen.
