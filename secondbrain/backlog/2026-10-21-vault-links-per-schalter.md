---
typ: feature
---
# Vault-Links per Schalter abschaltbar (IDEAL hat keinen Vault)

**Aufgenommen 2026-10-21 (Gerald):** „Vault-Abgleich und Vault-Buttons sollen per Schalter in den Einstellungen
aktivierbar sein. IDEAL hat keinen Vault.“

## Befund (Koordination, Bündel-Worktree v1.66.0, nur gelesen)

- Die Vault-Adresse `http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search=<Artikelnr>&searchContext=0` ist an
  **10 Stellen fest verdrahtet**: `Controllers/ArticlesController.cs:317` (Artikel-Info, Knopf „Im VAULT öffnen“ in
  `Views/Articles/Info.cshtml:55-60`), `Views/Articles/Index.cshtml:84`, `Views/StockOverview/Index.cshtml:113`,
  `Views/StockMovements/Index.cshtml:100`, `Views/Picking/Bom.cshtml:239`, `Views/Shared/_FaDocumentLinks.cshtml:24`,
  `Views/ProductionOrders/_ProductionOrderRow.cshtml:81`, `Views/PickingLeitstand/_PickingLeitstandRow.cshtml:62`,
  `Views/FaCompletion/_FaCompletionRow.cshtml:30`, `Views/FaWorklist/_FaWorklistRow.cshtml:36`.
- **Fallstrick:** Die CSS-Klasse `vault-link` trägt auch der Stücklisten-Knopf in `_FaDocumentLinks.cshtml:11-12`
  („Stückliste öffnen“). Ein Ausblenden über die Klasse würde die Stückliste mit verstecken.
- **Einen „Vault-Abgleich“ (Sync) gibt es im Code nicht:** kein Treffer zu „vault“ im Dienst `IDEALAKEWMSService`, in `SQL/`
  oder in den `appsettings*.json`. Es gibt nur die Links. Was Gerald mit „Abgleich“ meint, ist offen (Rückfrage).
- Hilfe erwähnt den VAULT-Link (`Views/Help/Index.cshtml:142-143`, `:1529`).

## Vorschlag für die Spec

1. Ein AppSetting (UI-Schalter, ADR 0011), z. B. `VaultLinksAktiv`, Standard **an** (AKE unverändert), bei IDEAL aus.
   Pflege in den Einstellungen bzw. Standorteinstellungen.
2. Die Vault-Basisadresse als Einstellung statt 10-mal fest im Code (leer = keine Links).
3. Alle 10 Stellen über einen Helfer (Tag-Helper oder Partial) rendern; Schalter aus → kein Link, keine Lücke im Layout.
   Stücklisten-Knopf bleibt.
4. Hilfe-Texte zum VAULT-Link nur zeigen bzw. formulieren „falls eingeschaltet“.
