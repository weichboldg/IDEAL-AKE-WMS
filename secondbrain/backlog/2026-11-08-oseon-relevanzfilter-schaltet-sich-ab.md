---
typ: bug
created: 2026-11-08
quelle: "[[2026-11-08-oseon-teileverfolgung-doppelter-klickhandler-spec]]"
---
# OSEON-Teileverfolgung: Filter „Fremdsystem-Relevanz“ schaltet sich bei jedem Absenden ab

Zusatzfund beim Nachstellen von [[2026-11-08-oseon-teileverfolgung-doppelter-klickhandler-spec]] (AKE-DEV v1.79.0, 2026-11-08, im Browser
belegt). In `Views/Tracking/OseonIndex.cshtml:52-53` steht `<input type="hidden" name="useRelevanceFilter" value="false" />` **vor** der
Checkbox. Angehakt sendet das Formular `useRelevanceFilter=false&useRelevanceFilter=true`; der bool-Binder nimmt den ersten Wert (`false`),
`TrackingController.cs:303,371` (`bool useRelevanceFilter = true`). Folge: Jede Filteränderung (auch ein Artikel-Scan) schaltet den Relevanz-Filter
aus; der Haken ist danach leer. Fix-Richtung: verstecktes Feld **hinter** die Checkbox (Muster v1.81.0 `MissingParts/Index.cshtml`).
