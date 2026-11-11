---
type: changelog
version: 1.95.0
date: 2026-11-08
---
# v1.95.0 — Nachbesserung aus der Vorab-Abnahme (DEV)

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commits `897c5466`, QA-Reparatur `a26be406` (Hilfe-Restsatz). Quelle: [[2026-11-08-buendel-dev-v1-94-browser-testbericht]], Befunde 1–4.
Ausnahme Schranke 1 (Gerald): Nachbesserung der Specs [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]],
[[2026-11-08-lagerbuchungen-fa-feld-und-verlinkung-spec]], [[2026-11-08-kommissionierung-auftraege-kopf-zeilen-spec]]. Nicht gemergt.

## Kernpunkte

- Offene Bestellungen: viewKey `PurchaseOrders` registriert (`ColumnDefinitions`), Zahnrad + `#view-config`/`#column-config`; Drift-Wächter
  `ViewKeyDriftGuardTests` für alle Views mit `#view-config`.
- FA-Link (`_FaLink`, Prüfkarte-API): FA-Liste mit `showDone=true` – erledigte und fertig kommissionierte FAs werden gefunden.
- Hilfe QR/FA an v1.87.0 (Bestandsübersicht übernimmt nie die FA).
- Leitstand: Hinweis „Kein Benutzer als Kommissionierer gekennzeichnet“ mit Link in drei Dialogen.

## Deploy-Risiken

Nur Web (Dienst nur Version). Keine Migration. **DEV-Hinweis:** `dev-deploy.ps1` (DEV-Absicherung) schaltet alle `Sync:*Enabled` aus – nach
einem DEV-Deploy die gewollt eingeschalteten Schalter (AKE-DEV seit Abnahme 2026-11-08: Articles, ArtikelLieferant, Kreditoren, Debitoren,
EinkaufBestellungen) wieder setzen.
