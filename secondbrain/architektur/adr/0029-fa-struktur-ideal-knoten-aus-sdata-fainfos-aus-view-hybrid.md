---
type: adr
id: 0029
title: FA-Struktur IDEAL – Knotenfelder aus SData, FAInfos aus der View (hybrid); beendete Haupt-FAs über dtsFaBelegsuche
status: accepted
date: 2026-11-16
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]] (v1.99.26, Freigabe-Antworten 1–10). Gebaut E1–E6 (`21b3fb65` … `5163df17`);
> Umschalten auf SData bleibt gesperrt (Auflage Gerald 2026-11-16).

## Kontext und Problem

- Annahme G11 ([[2026-11-06-sage-sdata-stammdaten-sync-spec]]): die 13 Knotenfelder der FAListe gäbe es in SData nicht – deshalb liefen
  FA-Struktur-Knoten nur über die View. L10–L12 in [[sage-sdata-benutzerfelder-lesetest]] zeigen: `USER_OS*` am FA-Kopf kommen über
  `dtsFaBelegsuche` (RealTimeData), am Blatt über den Artikel.
- `PpsFaBelegeInfo` liefert beendete Haupt-FAs nicht (Beleg FA 1059463); `dtsFaBelegsuche` schon.
- FAInfos stammt aus `[PPSImport].[dbo].[vw_PPSData]`, nicht aus Sage – SData kann sie nicht ersetzen.
- Die View-Texte der Knotenfelder sind nicht belegt (P-FS1); DPS hat RealTimeData im Dienst noch nicht freigegeben (P-FS3).

## Entscheidung

- **Knotenfelder:** FA-Knoten (`SubFA != 0`, Wurzel eingeschlossen) nehmen `USER_OS*` ihres FA-Kopfs aus `dtsFaBelegsuche`; Blätter die
  `USER_OS*` des Artikels; Matchcode, Bezeichnung, Artikelgruppe, Beschaffungsartikel, Hauptlagerplatz kommen für jeden Knoten vom Artikel.
  Zuordnung an genau einer Stelle (`SdataFaStrukturReader.FelderSetzen`), eine Bool-Regel für View und SData (`SageFaStrukturReader.SageBool`).
- **Beendete Haupt-FAs:** Köpfe fehlender Wurzeln aus derselben `dtsFaBelegsuche`-Runde (Blöcke à 50, `orderBy=BelID`).
- **RealTimeData im Dienst nur lesend und nur im Modus „Vergleich“** – Regel an einer Stelle `FaStrukturQuelleSwitch.RealTimeDataErlaubt`.
- **SData-Modus = hybrid:** Knoten aus SData + FAInfos aus der View (Filter „mit FAInfos“). Gebaut, aber über `SageQuelleSperre` gesperrt bis
  P-FS3 (DPS) und Rückfragen 4/5; gesperrt wirkt SData wie View mit Warnung.
- **Jede SData-Abfrage mit `where` braucht `orderBy`** – `SageSdataReadClient.BuildUrl` wirft sonst vor dem Request.

## Folgen

- Im Vergleich sichtbar und erwartet: Blattfelder ohne Quelle (Beschichtet/Breite/Höhe/Tiefe/EKBedarf) und Artikelgruppe nur als Code.
  Früh umschalten würde `Beschichtet` an Blättern auf false setzen und damit die Beschichtungslogik ([[0028-beschichtungsquelle-je-achse-ueber-appsettings-mit-automatisch]]) ändern – deshalb die Sperre.
- Mehr Sage-Aufrufe je Lauf im Vergleich (Artikel ⌈n/200⌉ + Köpfe ⌈n/50⌉), nicht gemessen (P-FS6).
- G11 ist für Knotenfelder überholt. Die Quellen-Weiche [[0019-quellen-weiche-view-sdata-je-abgleich]], das Migrationstor [[0012-fa-hierarchie-einweg-migrationstor]] und
  [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]] bleiben unverändert.
