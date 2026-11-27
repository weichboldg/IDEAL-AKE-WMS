---
type: changelog
version: 1.99.78
date: 2026-11-25
---
# v1.99.78 — BDE: Tageshistorie als eigene Seite, Terminal-Seite abgelöst

Spec [[2026-11-25-bde-masken-vereinheitlichung-spec]], Etappe 5 (letzte Etappe). Commit `fbe29238` (Worktree `feature/2026-08-07-ideal-teile-1-5`).
**Keine Migration.** Web und Dienst (Dienst nur Versionsnummer). TS-199. Nicht gemergt, qa-agent steht aus.

## Kernpunkte

- Neue Seite Tageshistorie (`BdeTeilelisteController.Tageshistorie`, Quelle `Services/BdeTageshistorie.cs`): Buchungen des Werkers von heute an
  allen Werkbänken plus alle Teilmeldungen des Tages (Teilfertig/Fertig, FA und Lagerbestellung); Server-Spaltenfilter, Pagination.
  Knopf im Werker-Balken, NavZiel `BdeTageshistorie` im Menü BDE und auf der Startseite.
- `BdeTerminal/Index` leitet an beiden Standorten immer auf die BDE-Seite (`workplaceId ?? Terminal-Default`); Login- und Terminal-Prüfung bleiben.
- Gelöscht: `Views/BdeTerminal/Index.cshtml`, `wwwroot/js/bde-terminal.js`. JSON-Aktionen von `BdeTerminalController` bleiben (FA-Sicht bucht darüber).
- AppSetting `BdeTerminalMaxHauptFa` entfernt (DB-Zeile verwaist); Beschreibung `BdeNurFaMeldung` „Altlast“.
- Betriebsdoku `docs/betrieb/09` § 4.1 BDE am Handy (lokales Konto, Formular-Login, Terminal-Zuordnung).
- Manifest: `v1.99.78-BdeTerminalAbloesungInfo`, `v1.99.78-BdeHandyKontenInfo`.

## Deploy-Risiken

- Keine DB-Änderung. Lesezeichen auf `/BdeTerminal` funktionieren weiter (Weiterleitung). Browser-Cache am Terminal ggf. leeren.
- Werker kennen die alte Terminal-Seite nicht mehr – kurz einweisen (Kapitel 11). Handy-Konten sind noch nicht angelegt.
