---
typ: bug
---
# LocationTransfer: Lagerplatz-Code roh in JS-String (`'@Html.Raw(...)'`)

**Aufgenommen 2026-09-25.** Herkunft: nachgeholtes Code-Review zu
[[2026-09-25-kommissionierung-nur-hauptfa-spec]] (Befund F-B, dort in `Bom.cshtml` behoben).

`IdealAkeWms/Views/StockMovements/LocationTransfer.cshtml` ~Z. 144:
`var source = '@Html.Raw(Model.SourceStorageLocationCode)';` — derselbe Musterfehler. Ein Lagerplatz-Code mit
Apostroph bricht das Bestätigungs-Script (Umbuchen-Knopf reagiert nicht); ein präparierter Code würde als
Script laufen. Risiko geringer als bei den Standardfiltern (Stammdaten, nicht Benutzer-Freitext), aber
gleiche Lösung: `var source = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(Model.SourceStorageLocationCode ?? ""));`
Ein-Zeilen-Fix; vorher `grep -rn "'@Html.Raw(" IdealAkeWms/Views` auf weitere Stellen prüfen
([[fallstricke]] §17).
