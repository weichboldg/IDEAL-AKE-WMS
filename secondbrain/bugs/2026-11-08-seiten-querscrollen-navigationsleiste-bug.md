---
type: bug
title: Seiten-Querscrollen durch die Navigationsleiste (992–ca. 1600 px); Kontrast Kartenlink „Fehlteile (wird nachgeliefert)“
status: offen
severity: mittel
created: 2026-11-08
affected_code:
  - IdealAkeWms/Views/Shared/_Layout.cshtml
  - IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml
source_backlog: "[[2026-11-08-seiten-querscrollen-navigationsleiste]]"
spec: "[[2026-11-08-seiten-querscrollen-navigationsleiste-spec]]"
---

## Symptom

- Zwischen 992 px und etwa 1600 px Fensterbreite scrollt die ganze Seite waagrecht. Der Benutzername rechts in der Menüleiste wird abgeschnitten („Gerald Weich…“). Das widerspricht der Regel aus v1.64.1 „genau ein Scrollbalken“ ([[2026-10-19-tabelle-seitenbreite-doppelter-scrollbalken-bug]]).
- Auf `/WarehouseRequisitions` ist der Kartenlink „N Fehlteile (wird nachgeliefert)“ `text-warning` (#ffc107) auf Weiß. Das ergibt etwa 1,6:1, WCAG AA verlangt 4,5:1.

## Reproduktion

1. Als Admin anmelden (sieht alle Menüpunkte), Fenster 1520 px breit, beliebige Seite (z. B. `/ServiceSettings`, `/StorageLocations`, Dashboard).
2. Am unteren Seitenrand erscheint eine Seiten-Querscrollleiste, rechts ist der Benutzername abgeschnitten.
3. Belege: IDEAL-DEV bei 1520 px (QA A1, [[2026-11-08-bereinigungen-nach-buendel-2026-11-07-spec]] Z. 606; QA A2, [[2026-11-08-artikeldetails-lieferanten-anzeigen-spec]] Z. 428), AKE-DEV bei 1520 px (QA B1 N-3, [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] Z. 761), IDEAL bei 1384 px mit `#navbarNav` bis x = 1600 (Browser-Testbericht 2026-10-21, Z1).

## Root Cause

`_Layout.cshtml:35` nutzt `navbar-expand-lg`. Ab 992 px stehen alle Menüpunkte in **einer** Zeile (`.navbar-nav` ohne `flex-wrap`, `_Layout.cshtml:47`), und der Benutzer-Knopf folgt rechts (`_Layout.cshtml:371`). Die Breite hängt an Rollen und Feature-Schaltern je Standort. Ist die Zeile breiter als das Fenster, läuft `#navbarNav` über den rechten Rand hinaus.

Der Kartenlink trägt `class="text-warning …"` (`Views/WarehouseRequisitions/Index.cshtml:50`) auf der weißen `.card` (`site.css:111-116`).

## Fix / Verweis

[[2026-11-08-seiten-querscrollen-navigationsleiste-spec]] (v1.94.0)
