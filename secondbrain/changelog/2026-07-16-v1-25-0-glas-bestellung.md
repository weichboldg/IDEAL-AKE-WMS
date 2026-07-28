---
type: changelog
version: 1.25.0
date: 2026-07-16
---
# v1.25.0 — Glas-Bestellung, Hauptlagerplatz, FA-Reconciliation, Service-Haerte

Sammel-Release aus dem Akkumulations-Branch `feature/glas-bestellung`; mehrere Teile wurden ohne
eigenen Versions-Bump hineingefaltet. AppVersion-Datum 2026-07-03 → 2026-07-16.

- **Glas-Bestellung als eigener Bestelltyp:** `WarehouseRequisitionType` (Lager=1/Glas=2),
  **Migration 77** (additiv, Default Lager). Der Typ steht nur bei der Anlage fest. Neue Rolle
  `glasbestellung`, Helper `GlasArticleGroupFilter`, 3 neue AppSettings. Enforcement **zweifach**:
  Artikelsuche `?type=` und AddItem-API serverseitig.
- **Hauptlagerplatz am Artikel:** `PrimaryStorageLocationId` + `SagePrimaryStorageLocation`
  (**Migration 79**, additiv). Sage-Wert ⇒ in der App gesperrt. Zentrale Sortierung „Haupt zuerst",
  ⭐-Badge in der Bestandsuebersicht.
- **FA-Reconciliation:** in Sage geloeschte offene FAs werden storniert (`IsCancelled`,
  **Migration 80**) — mit Guard, Cap und Fehlermail, Opt-in
  `Sync:ProductionOrderReconcileEnabled`.
- **ServiceSettings typisiert + vollstaendig:** Katalog `ServiceSettingDefinitions.All` treibt
  Seeding und UI; DB gewinnt, nur MailSettings/ConnectionStrings bleiben appsettings →
  [[0008-servicesettings-db-first-mit-typisiertem-katalog]].
- **Lagerbestellung aus der Stueckliste** (Einzel + Bulk, Quick-Add-API) + Master-Schalter
  `LagerbestellungAktiv` (Default **true**, nur `"false"` sperrt). BOM-UX-Followup: Spalten
  umsortierbar, Sticky-Auswahlleiste.
- **Service-Resilienz:** Sync-Bloecke einzeln via `RunResilientAsync` — ein Fehler killt die
  anderen nicht mehr; plus Fehlermail (`ISyncErrorNotifier`) und `COL_LENGTH`-Check fuer
  `SubOrderNumber`.
- **Lagerbestand-Nullsetzen** verwaister Sage-Paare (`LagerbestandZeroingPlanner`, Leer-Guard + Cap).
- **Cleanup-Jobs:** neuer `CleanupWorker` (24h) mit Aktivitaets-Protokoll-Bereinigung
  (`Cleanup:AktivitaetsprotokollAufbewahrungTage`, Default 180). Live verifiziert 2026-07-16.
- **Rolle `stock_read`** (read-only Bestaende + Bewegungshistorie), **Migration 78**.
- **Windows-Auth UA-Gate + SSO-Button** sowie eine **vierteilige SSO-Fix-Kette** — inklusive
  Wurzel-Fix gegen Antiforgery-400 unter SSO (`NormalizeUserForSession`). AD-Benutzer:
  E-Mail-Uebernahme aus dem AD.
- Merge `feature/glas-bestellung` → `main` (`3b127f2`, `--no-ff`), nach `origin/main` gepusht.
- Deploy offen → [[2026-07-deploy-v1-25-0]].
