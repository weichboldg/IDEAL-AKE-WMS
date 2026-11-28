---
type: bug
title: SData-Geheimnisse und Sage-Test – Review-Befunde SD-1, SD-2, SD-3, SD-4, SD-6
status: offen
severity: mittel
created: 2026-11-28
affected_code:
  - "IdealAkeWms/Controllers/ServiceSettingsController.cs:46,176-242,277-323"
  - "IdealAkeWms/Controllers/SageTestController.cs:57-70"
  - "IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs:172-180"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs:449-453"
  - "IDEALAKEWMSService/Workers/SageBookingWorker.cs:64-87"
  - "scripts/deploy/Update-IdealAkeWms.ps1:414-415"
  - "scripts/deploy/nachlauf.psd1:269-272"
source_backlog: "[[2026-11-14-review-befunde-sdata]]"
spec: "[[2026-11-28-sdata-geheimnisse-review-befunde-spec]]"
---

## Symptom

- **SD-1 (mittel, Sicherheit, nur Admin):** Verbindungstest im Register Sage-Test schickt bei leerem Kennwortfeld die gespeicherten SData-Zugangsdaten
  per Basic-Auth an die frei eingetragene Adresse – auch über `http://` und mit abgeschalteter TLS-Prüfung.
- **SD-2 (niedrig):** Verbindungstest liest die ganze Antwort in den Speicher (z. B. ~237.000 Zeilen); in der Feldliste hängt ein `&` in orderBy/include
  beliebige Parameter an (z. B. ein zweites `where=`).
- **SD-3 (niedrig, fail-safe):** Freitext-Weg Create/Edit/Delete mit `sdata:password` gilt nicht als geheim und überschreibt bzw. löscht bei CI-Kollation
  die Zeile `SData:Password` (Klartext in DB/Backups). Gleiche Lücke am Einwegtor `ProduktionsauftragHierarchisch` (Nebenbefund der Spec).
- **SD-4 (niedrig):** Update-Skript-Prüfung `Pruefung/SDataKennwortAppsettings` sieht nur die Dienst-appsettings, nie die Web-Datei.
- **SD-6 (Hinweis):** Ein Fehler im Lagerbuchungs-Schritt des `SageBookingWorker` lässt Sage-Wareneingang und Wareneingang-CSV im selben Takt aus.

## Reproduktion

- SD-1: Register Sage-Test → Sage-Verbindung testen → Adresse `http://<eigener-rechner>:<port>/`, Kennwort leer → Testen; Listener erhält
  `Authorization: Basic …` (auf AKE-DEV 2026-11-14 nicht reproduzierbar, weil dort keine Zugangsdaten gespeichert sind; laut Code
  `ServiceSettingsController.cs:182-204`).
- SD-3: Kollation `Latin1_General_CI_AS` auf AKE-DEV und IDEAL-DEV belegt (2026-11-14); Freitext-Weg selbst nicht ausgeführt.
- SD-4: `-NurPruefen` auf AKE-DEV und IDEAL-DEV meldet nur die Dienst-Datei (2026-11-14).
- Details und Fundstellen: [[2026-11-14-review-befunde-sdata]], neu belegt gegen main in der Spec.

## Root Cause

- SD-1: URL und TLS kommen aus dem Request, die gespeicherten Zugangsdaten werden ohne Abgleich mit `SData:BaseUrl` eingesetzt.
- SD-2: `ResponseContentRead` + `ReadAsStringAsync` vor dem Kürzen; orderBy/include ungeprüft in `BuildQueryUrl`.
- SD-3: `IstGeheim` (und `HierarchischeStrukturKeys.IsGuarded`) vergleichen ordinal, SQL über `FindAsync` nach Spalten-Kollation (CI).
- SD-4: `Get-AppsettingsWert` liest fest `$e.Dienst`.
- SD-6: beide Schritte in einem `try`.

## Fix / Verweis

[[2026-11-28-sdata-geheimnisse-review-befunde-spec]]
