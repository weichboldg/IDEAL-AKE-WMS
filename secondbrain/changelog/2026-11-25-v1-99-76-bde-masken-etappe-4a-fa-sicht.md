---
type: changelog
version: 1.99.76
date: 2026-11-25
---
# v1.99.76 — BDE-Seite: FA-Sicht, Anstempeln, BDE-Standardsicht

Spec [[2026-11-25-bde-masken-vereinheitlichung-spec]], Etappe 4a. Commit `878aa76d` (Worktree `feature/2026-08-07-ideal-teile-1-5`).
**Keine Migration** (Spalte `BdeStandardsichtTeile` kam mit SQL/169 in v1.99.75). Web und Dienst (Dienst nur Versionsnummer). TS-197. Nicht gemergt.

## Kernpunkte

- Wirksamkeits-Regel an einem Ort (`WerkbankKuerzelFamilie`), `BdeNurFaMeldung` wirkt auf der BDE-Seite nicht mehr (Antwort 2).
- BDE-Seite mit Reitern Teile Einzeln / Teile Summiert / FA; gemerkte Sicht je Werker und Werkbank; Startreiter aus `BdeStandardsichtTeile`.
- FA-Tabelle (`BdeFaAngebot`, gleiche Reihenfolge wie die Terminal-API), Spaltenfilter, Zahnrad, Pagination; AG-Status aller AG, Zeilen-AG markiert.
- Anstempeln Rüsten/Produktion je Zeile (POST/PRG); Kollision als gelbe Meldung, nichts gebucht. Auch an AKE (über `/BdeTeileliste`).
- Werkbank-Maske: Auswahl BDE-Standardsicht (ADR [[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]]),
  „Teile“ nur mit BDE-Teileliste + Kürzel + BDE aktiv; Liste zeigt „Standard Teile“.
- `BDE → Terminal` leitet nur noch bei Standardsicht Teile auf die Teileliste.
- Manifest: `Pruefung/BdeNurFaMeldungAn`, `Pruefung/BdeStandardsichtOhneTeileSicht`, Einstellung `v1.99.76-BdeStandardsichtEinstellung`, Info `v1.99.76-BdeFaSichtAkeInfo`.

## Deploy-Risiken

- Setzt SQL/169 (v1.99.75) voraus. `BdeNurFaMeldung = true` → Vorprüfung meldet; Alt-Zweige des Buchungsdienstes weisen weiter ab (Antwort 22).
- IDEAL: Teileliste-Werkbänke mit Standardsicht FA leiten am Terminal nicht mehr um (gewollt). Browser-Cache am Terminal ggf. leeren.
