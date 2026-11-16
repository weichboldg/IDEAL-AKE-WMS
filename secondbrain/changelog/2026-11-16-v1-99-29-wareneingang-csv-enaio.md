---
type: changelog
version: 1.99.29
date: 2026-11-16
---
# v1.99.29 — Wareneingang-CSV für enaio: ohne Kopfzeile, Erfasser = Windows-Kontoname

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commit `f12f46f7` (wip, feature-complete). Spec
[[2026-11-14-wareneingang-csv-erfasser-samaccountname-spec]], Vorgänger [[2026-11-12-wareneingang-barcode-lieferschein-csv-export-spec]]
(v1.99.11, TS-129). Nicht gemergt. Standort AKE.

## Kernpunkte

- `WareneingangCsvService.CsvInhalt`: **keine Kopfzeile** mehr, nur die Datenzeile mit 9 Feldern. Reihenfolge, `;`, CRLF, UTF-8 mit BOM,
  Datumsformat und Quoting unverändert. Ohne Kopfzeile ist die Position der Vertrag mit enaio ([[fallstricke]] §32).
- **Erfasser (Feld 7):** `WindowsAccountHelper.ExtractSam(StockMovement.User.WindowsUserName)` (ohne Domäne, Schreibweise wie gespeichert),
  leer → `User.Name`, Bewegung ohne `UserId` → `PurchaseOrderReceipt.CreatedBy`. Nie `CreatedByWindows`/`StockMovement.WindowsUser`
  (Sitzungs-Identität). `RunOnceAsync` lädt dafür `Include(StockMovement).ThenInclude(User)`.
- Nur die CSV: Mails, Druck, Sage-Belegtexte und gespeicherte `CreatedBy` bleiben unverändert.
- Hilfe (Wareneingang) nennt die 9 Felder nach Position, Anwender-Changelog v1.99.29, README (Abschnitt Wareneingang-CSV).
- Tests: `WareneingangCsvServiceTests` ohne Kopfzeile + Erfasser-Theorie (sam, `DOMAIN\`, `@`, Leerzeichen, leer → Name, ohne Bewegung/User
  → CreatedBy, `CreatedByWindows` ohne Wirkung), Escaping, `RunOnce` mit Include. TS-153, Hinweis an TS-129.11.

## Migration / SQL

Keine.

## Deploy-Risiken

- Web (Hilfe/Changelog/Version) und Dienst (schreibt die CSV) gemeinsam. Manifest-Info `v1.99.29-WareneingangCsvFormat` (AKE, Nachher).
- Der Export ist laut Gerald noch nicht produktiv, keine Altdateien. Auf DEV liegen ggf. Testdateien aus TS-129 im alten Format.
