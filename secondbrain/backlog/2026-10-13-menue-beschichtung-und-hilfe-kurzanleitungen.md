---
typ: feature
---
# Menüpunkt „Beschichtung“ bündeln + Kurzanleitungen in der App-Hilfe

**Aufgenommen 2026-10-13** – Gerald, nach Beschichtungsauftrag Teil 1–4 (v1.51.0) und CSV-Export (v1.53.0).

## Wunsch 1 – ein Menüpunkt „Beschichtung“

Alles zum Beschichtungsauftrag gesammelt in **einem** Dropdown-Menüpunkt **„Beschichtung“** (Name: Gerald
2026-10-13), Muster wie „Bestellungen“.

Befund Bündel v1.53.0 (`IdealAkeWms/Views/Shared/_Layout.cshtml`):
- Z. 112-123: heute **zwei** eigene Menüpunkte – „Beschichtungsauftrag“ (`FaHierarchyBeschichtung/Summiert`,
  Rolle `beschichtungsauftrag` + Toggle `FaHierarchyBeschichtungAktiv`) und „Beschichtungsaufträge“
  (`Beschichtungsauftraege/Index`).
- Muster Dropdown „Bestellungen“: Z. 212-236.

Vorschlag Einträge (Koordination, zu bestätigen in der Spec): Vorschlagsliste (summiert) · Vorschlagsliste (einzeln)
· Aufträge. Sichtbarkeit wie heute (Rolle + Toggle); Active-Markierung für beide Controller.

## Wunsch 2 – Kurzanleitungen in der App-Hilfe

Gerald 2026-10-13: Benutzeranleitung „beides“ – ausführliches Dokument (macht die Koordination) **und**
Kurzfassung in der App-Hilfe. Kurzanleitungen für: Beschichtungsauftrag (Vorschlagsliste, Auftrag anlegen,
Status, Reklamation, Druck mit Briefkopf), Arbeitsgang-Kürzel im FA-Baum, CSV-Export.
Bestehende Hilfeseiten der Läufe v1.48–v1.53 prüfen und zusammenführen statt doppeln.
