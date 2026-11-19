---
type: adr
id: 0034
title: Menü und Startseiten-Kacheln aus einer Quelle (NavigationKatalog)
status: accepted
date: 2026-11-19
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-19-dashboard-kacheln-nach-kategorie-und-berechtigung-spec]] (v1.99.45, TS-170, keine Migration). Ergänzt
> [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]] und [[0011-feature-toggles-ueber-appsettings]]. Bündel
> `feature/2026-08-07-ideal-teile-1-5`, Commit `0df40eeb`.

## Kontext und Problem

- `_Layout.cshtml` und `Home/Index.cshtml` hatten je eigene `@if`-Ketten aus Rechten und Schaltern; der `HomeController` füllte dafür ViewBag.
  Die Ketten liefen auseinander: Kacheln führten auf „Zugriff verweigert“ (Artikelinfo ohne `masterdata_read`), neue Funktionen fehlten
  auf der Startseite, Dropdowns blieben leer stehen.

## Entscheidung

- **Ein Katalog** `IdealAkeWms/Services/Navigation/NavigationKatalog.cs`: `NavZiel(Key, Titel, Controller, Action, Kategorie?, Beschreibung?)`,
  je Ziel ein Feld in `NavZiele` (Feldname = Key), `Kategorien` in fester Reihenfolge. Kategorie `null` = nur Menü.
- **Eine Regel je Ziel** in `NavigationSicht` (scoped, je Request einmal berechnet): ein `switch` über den Key, unbekannter Key wirft.
  Schalterlesart wie bisher (`"true"` = an; `LagerbestellungAktiv` nur `"false"` = aus).
- Menü: jeder Link im eigenen `@if (nav.Zeigt(NavZiele.X))`, Dropdowns `ZeigtEines(...)`. Startseite: `Kacheln` (nur sichtbare, nicht leere Kategorien).
- **Wächter:** `NavigationSichtTests` – Kacheln ⊆ Menü über eine Rollen×Schalter-Matrix; Filter-Kongruenz (die Regel lässt nie durch, was der
  `RequireXxxAccess`-Filter der Ziel-Action abweist); Quelltext-Wächter: jeder Menülink steht unter seinem eigenen Key, Home ohne ViewBag.

## Folgen

- Neuer Menüpunkt = `NavZiel` + Regel im `switch` + Link im Layout (Test schlägt sonst an). [[fallstricke]] §58.
- Inline-Gates im Controller (z. B. FA-Abarbeitungsliste, Arbeitsgänge → Sage) sind im Test als Ausnahme dokumentiert und müssen dort von Hand
  mitgezogen werden.
- Verworfen: Kacheln per Reflection aus den Filterattributen ableiten (Schalter-Gates stehen nicht an den Attributen) und ein zweites
  Kachel-Partial je Modul (genau die Drift, die abgestellt wird).
