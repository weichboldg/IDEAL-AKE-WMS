---
typ: notiz
spec: "[[2026-10-21-vault-links-per-schalter-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Vault- und enaio-Links konfigurierbar

Spec [[2026-10-21-vault-links-per-schalter-spec]], direkt im Bündel. Start `845e38c9` (v1.66.2), Ziel **v1.67.0**, **TS-100**.
Keine Migration. Deploy: Web und Dienst.

## Plan (kompakt)

1. **Einstellungen:**
   - `AppSettingKeys`: `VaultLinksAktiv`, `VaultLinkVorlage`, `EnaioLinksAktiv`, `EnaioLinkVorlage`.
   - Laufzeit-Seed in `Program.cs`, nur wenn die Zeile fehlt.
   - `ServiceSettingDefinitions`: `Sync:EnaioDmsViewName`.
   - `StandortSettingsCatalog`: Gruppen „Vault (Zeichnungen)“ und „enaio (Dokumente)“.
2. **Helfer:** `Helpers/LinkVorlage.Build(aktivRaw, vorlage, platzhalter, wert)` als reine Funktion, mit Unit-Tests.
3. **Vault:** `_VaultLink`-Partial und `VaultLinkViewModel`; die zehn Fundstellen umstellen.
   - Artikelinfo mit `ShowLabel`; `ArticleInfoViewModel.VaultUrl` entfällt.
4. **enaio:** `_EnaioDmsBadges` liest Schalter und Vorlage, bei `null` keine Ausgabe.
5. **Dienst:**
   - `FaHierarchySql.ValidateAndQuote(name, maxSegments = 2)`.
   - `EnaioDmsSyncService.DefaultViewName`; den View-Namen vor dem `try` lesen und prüfen.
6. **Doku:** Hilfe, README, TS-100 sowie TS-2.8, TS-4.5/4.5a, TS-27.5, TS-38.13; v1.67.0 ×2, Changelog.
