---
type: backlog
status: offen
created: 2026-11-16
quelle: Gerald, Chat 2026-11-16 14:30
standort: beide
---
# Beschichtungslogik je Standort (Standort-Einstellung „Beschichtungslogik“)

## Wunsch (Gerald)
- Erkennung „Beschichtung“ läuft an der AKE heute über OSEON, an der IDEAL (vermutlich) über die View (Beschichtungsdatum bzw. Beschichter).
- Eine Standort-Einstellung **„Beschichtungslogik“** (z. B. OSEON, View, …) soll festlegen, woher Beschichtet-Kennzeichen und Datum kommen.
- **Alle Features sollen unabhängig davon funktionieren:** z. B. an der AKE den **Beschichtungsauftrag** verwenden können, an der IDEAL in der
  **FA-Liste das (korrekte) Beschichtungsdatum** anzeigen.

## Ist-Stand (belegt, Bündel v1.99.23, nur gelesen – Spec prüft vollständig)
- **AKE-Weg:** `IDEALAKEWMSService/Services/CoatingDetectionService.cs` (Gate `Sync:CoatingDetectionEnabled`) setzt das Lackier-Flag über die
  Artikelkategorie aus AppSetting `LackierteilKategorieName` (`:56-73`); die Kategorien kommen aus dem OSEON-Artikelkategorie-Sync. Im
  hierarchischen Modus übersprungen (`:40-46`, „Klasse-D-Gate“).
- **IDEAL-Weg:** `IDEALAKEWMSService/Services/FaMaterializationCoating.cs` leitet das Flag aus `FaHierarchyNode.Beschichtet`
  (`IdealAkeWms/Models/FaHierarchyNode.cs:56`) ab (Sub-FA selbst oder direktes Blatt-Kind).
- Ergebnis landet in `ProductionOrderPickingStatus.HasCoatingParts` / `IsCoatingDone` (`IdealAkeWms/Models/ProductionOrderPickingStatus.cs:40-44`).
- **Datum:** Ein Feld „Beschichtungsdatum“ gibt es im WMS nicht (Suche ohne Treffer). Der **Beschichtungstermin wird berechnet**
  (`IdealAkeWms/Services/CoatingDateCalculator.cs`, Abholtage `BeschichtungAbholtage`, `AppSettingDefinitions.cs:63-65`; Verwendung FA-Liste,
  Leitstand, Werkbank-Arbeitsliste). Ob die IDEAL-View ein eigenes Datum/einen Beschichter liefert: **ungeprüft** → Prüfabfrage.
- **Beschichtungsauftrag** (`BeschichtungsauftraegeController.cs:28-29`) hängt am Gate `FaHierarchyBeschichtungAktiv` (Modul der IDEAL-
  FA-Struktur, `RequireFaHierarchyBeschichtungAktivAttribute.cs:43-45`) – an der AKE (flach, ohne FA-Struktur) daher heute nicht nutzbar.
