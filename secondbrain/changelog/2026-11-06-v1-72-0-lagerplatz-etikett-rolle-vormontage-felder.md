---
type: changelog
version: 1.72.0
date: 2026-11-06
---
# v1.72.0 — Lagerplatz-Etikett 93 × 55 mm und neue Spalten in den Vormontage-Listen

Spec [[2026-11-06-lagerplatz-etikett-rolle-und-vormontage-felder-spec]] (nimmt [[2026-10-06-vormontage-abteilung-spalte-filter-spec]] auf),
Umsetzung [[2026-11-06-lagerplatz-etikett-rolle-und-vormontage-felder-umsetzung]]. Basis `6969620a` (v1.71.1), Commits `48349786` (Teil 1),
`43a3988f` (Teil 2). **Keine Migration**, kein Seed, nur Web (Dienst nur Version). TS-104, TS-105.

## Kernpunkte

- Teil 1: Standort-Schalter `LagerplatzEtikettRolle93x55` (Gruppe „Druck“, Default aus = A4 wie bisher). An: `PrintLabels.cshtml` druckt ein Etikett
  93 × 55 mm je Seite ohne Rahmen (Einzel- und Sammeldruck), Treiber-Hinweis in Leiste und Hilfe. Browser-Druck, `PrintService` unberührt.
- Teil 2: Vormontage „Einzelne Teile“ und „Summiert“ mit Arbeitsbereich (je Position), Kunde, FE-Termin (Datumsfilter), Bemerkung PN, Fertigungsinfo PN;
  Summiert zusätzlich HauptFA. Summiert: Schlüssel bleibt Matchcode, Werte als Liste, Filter auf den neuen Spalten je Position **vor** dem Zusammenzählen.
  Kombigerät: Kopfwerte als Liste, Datumsfilter auf frühestem FE-Termin. Untertitel „VM-Bedarf“.
- Arbeitsbereich = Sage-Spalte „Abteilung“ ist **ungeprüft** (P5 entfallen, Gerald 2026-11-06).

## Deploy-Risiken

- Keine Migration. Teil 1 wirkt erst, wenn ein Admin den Schalter am Standort mit Rollendrucker setzt; Druckbild/Barcode-Lesbarkeit nur am echten Drucker (TS-104).
- Teil 2 nur an IDEAL (Toggle `FaHierarchyVormontageAktiv`). Gespeicherte Spalten-Einstellungen bleiben gültig, neue Spalten erscheinen sichtbar.
