---
type: changelog
version: 1.99.18
date: 2026-11-13
---
# v1.99.18 — Zeitstempel in Lokalzeit: UTC-Schreiber umgestellt, Wächter-Test

Buendel `feature/2026-08-07-ideal-teile-1-5`, Commits `99399be4` (Umstellung Web + Dienst, `ToLocalTime()` entfernt), `c703c1e3` (Wächter,
Version, Changelog, TS-135), `4716888b` (Abschluss, leer; Zahlen im Commit-Text falsch – richtig Web 24 + 1, Dienst 20), QA `d9e4c89e` (TS-135.6 Erwartungstext). **Testbereit** (QA 2026-11-13). Spec
[[2026-11-13-utc-schreiber-auf-lokalzeit-spec]], ADR [[0025-zeitstempel-in-lokalzeit-des-schreibenden-rechners]]. Nicht gemergt, nicht gepusht;
/goal-Lauf [[2026-11-13-buendel-v1-99-16-goal-lauf]].

## Kernpunkte

- **Web:** 24 × `DateTime.UtcNow` → `DateTime.Now` (Users, Account, Roles, OrderRecipients, Tracking, `Program.cs`-Seeds, Repositories Role,
  OrderRecipient, UserViewPreference, ProductionOrderPickingStatus); Karte „Sage-Zusatzfelder“ ohne `ToLocalTime()` (Freigabe-Antwort 12).
- **Dienst:** 20 × `GETUTCDATE()` → `@Jetzt` (`DateTime.Now`, einmal je Lauf) in `SageProductionOrderSql`, `SageImportService`,
  `OseonSyncService`, `CoatingDetectionService`, SData-Writer.
- Sichtbar behoben: `/Users` „Erstellt am“, Artikel aus dem Abgleich, Teileverfolgung „rückgemeldet am“ (AKE), NAN-Seed.
- Bewusst UTC (Ausnahmeliste im Wächter): PDF-Cache-Vergleich, Hierarchie-Stand (`DateTimeOffset`), BOM-Cache `CachedAt`.
- Bedarfsmeldungen: bereits v1.99.13.

## Migration / SQL

Keine. **Altdaten unverändert** (Freigabe-Antworten 1–11): Zeilen vor dem Deploy zeigen weiter die UTC-Zeit.

## Deploy-Risiken

Web + Dienst gemeinsam. Keine Einstellung, kein Manifest-Eintrag.
