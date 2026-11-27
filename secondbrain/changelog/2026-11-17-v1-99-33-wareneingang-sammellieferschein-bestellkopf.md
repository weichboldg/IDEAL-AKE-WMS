---
type: changelog
version: 1.99.33
date: 2026-11-17
---
# v1.99.33 — Wareneingang: Sammellieferschein (`<Barcode>_n.csv`) und Bestellkopf mit AB-Nummer und Kommission

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commits `32338503` (Code, Tests, SQL/156, Manifest, Version) und `19ce63ef` (wip feature-complete:
Doku, TS-156). Spec [[2026-11-17-wareneingang-sammellieferschein-und-bestellkopf-spec]] (Freigabe-Nachtrag 11:23 geht dem Rumpf vor). Nicht
gemergt, noch ohne QA. Lauf-Notiz [[2026-11-17-goal-lauf-wareneingang-sammellieferschein]].

## Kernpunkte

- **Teil A Sammellieferschein (Web):** `PurchaseOrdersController.Receive` lässt einen schon vergebenen Barcode zu, wenn
  `SammellieferscheinGrund(erster, lieferantKto, lieferschein)` `null` liefert: gleicher Lieferant **und** gleiche, nicht leere Lieferschein-Nr.
  (OrdinalIgnoreCase). Sonst Fehler mit Grund („anderer Lieferant.“, „dort ohne Lieferschein-Nr. …“, „… dieselbe Lieferschein-Nr. … (dort: X)“).
  Der Hinweis „Sammellieferschein – Barcode … bereits bei Bestellung … verwendet …“ hängt an der Erfolgsmeldung (Freigabe-Antwort 7).
- **Teil A Dateiname (Dienst):** `WareneingangCsvService.SchreibenAsync` vergibt `<Barcode>.csv`, `<Barcode>_1.csv` … über
  `Dateiname(barcode, start, belegt)`. Start = Zahl der Ausgangszeilen mit gleichem Barcode (ohne Groß/Klein) **mit** `Datei` (Fehler-Zeilen
  zählen nicht); belegt = Dateiname einer anderen Zeile (egal welcher Barcode) oder Datei im Ordner. `File.Move(overwrite:false)` bleibt.
  `KollisionText` entsteht im Schreibpfad nicht mehr; Altzeilen bleiben in `NieWiederholen` (Freigabe-Antwort 4: nur anzeigen).
- **Teil B Kopf (Dienst):** `EinkaufBestellungenSyncService.KoepfeAsync` liest `KHKEKBelege_USER_AB`/`KHKEKBelege_USER_Kom` per GET aus
  `RealTimeData/{Dataset}/dtsEKBelegMst.Sage.Wawi`, `where` nur `BelID` in OR-Blöcken (`SageSdataQuellen.OrBloecke`), Zuordnung über `BelID`.
  Melden statt still: Feld fehlt in jeder Zeile → Warnung, Wert leer; Lesefehler → Werte vom letzten Lauf + Warnung; zu lang → `null`, Zählwert
  `kopf_zu_lang`, Warnung (nie kürzen); BelIDs ohne Treffer → `kopf_ohne_treffer`. Counts `ab_nummer`, `kommission`.
- **Teil B Anzeige (Web):** Spalten `ab-nummer`/`kommission` in `Views/PurchaseOrders/Index.cshtml` (th + column-config + `ColumnDefinitions`
  + `ColumnMap`, sichtbar, ausblendbar) – damit auch Einkauf → Offene Bestellungen; „Wareneingang zu Bestellung“ nur im Kopf. Nicht in der CSV.

## Abweichungen zur Spec

- Nummern: SQL/156 und TS-156 statt „voraussichtlich 157“ (Auftrag; BF-Spec rückt nach).
- Spaltenlängen 250/200 aus dem SData-Schema `EKBelegUserField` ([[sage-sdata-commonwawiservices]]); Prüfabfrage P-2a auf der Sage-DB steht aus.
- Keine Live-Leseprobe gegen AKE-Test im Dev-Lauf; Variante B ist per Unit-Test mit den echten Feldnamen belegt.

## Migration / SQL

- **SQL/156** `156_AddSagePurchaseOrderLineAbNummerKommission.sql`, MigrationId `20261117102804_AddSagePurchaseOrderLineAbNummerKommission`:
  zwei NULL-Spalten in `dbo.SagePurchaseOrderLines`, additiv, `COL_LENGTH`-Guard, FreshInstall an beiden Stellen. AgentJobs nicht betroffen.

## Deploy-Risiken

- Manifest: `SQL/156` (Schema, beide), `Pruefung/WareneingangCsvKollision` (AKE, Nachher, nur anzeigen), Info
  `v1.99.33-WareneingangCsvSammellieferschein` (AKE, **Vorher**): Prod erst nach enaio-Bestätigung E-1 (Import verarbeitet `_n`, liest Feld 1).
- Werte erscheinen erst nach dem ersten Abgleich „Offene Bestellungen (Sage)“ nach dem Deploy. Je Lauf zusätzliche GETs (ein Block je 50 Belege).
