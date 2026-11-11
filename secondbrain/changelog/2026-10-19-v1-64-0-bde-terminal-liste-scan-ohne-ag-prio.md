---
type: changelog
version: 1.64.0
date: 2026-10-19
---
# v1.64.0 — BDE-Terminal: Liste nach Haupt-FA, Scan ohne AG, Prio

[[2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-spec]], Umsetzung [[2026-10-19-bde-terminal-liste-scan-ohne-ag-prio-umsetzung]].
**Direkt im Bündel**, Start `b3daf566` (v1.63.0), Commits `63bf006c`, `96e90402`, `5613e5a2`, QA-Fix `a9558301`. Quelle
[[2026-10-19-bde-terminal-liste-und-scan-ohne-ag]]. **Keine Migration**, kein SQL-Skript.

## Kernpunkte

- **IDEAL (Master an):** Terminal-Liste je Haupt-FA gruppiert, aufklappbar, sortiert nach Prio (aufsteigend, Kombigerät kleinster
  Wert, leer zuletzt), frühestem Termin, FA-Nummer (`BdeTerminalAuftraege.Ordnen`). Höchstens `BdeTerminalMaxHauptFa` (AppSetting,
  Standard 5, Startup-Seed) Gruppen, Rest über „Weitere“. AKE bleibt flach.
- Beide Standorte: gewählter AG markiert (`aria-pressed`, Rahmen, ✓), Aktionsknöpfe + „Gewählt: …“ direkt unter Karte 2.
- Scan ohne AG: Haupt-FA klappt die Gruppe auf (ohne Vorauswahl), Sub-FA wählt direkt, `FA,AG` wie bisher. Resolver Schritt 3b:
  existierende Sub-FA ohne offenen AG → NotInScope statt NotFound.
- Negative Mengen an allen sechs BDE-Mengeneingängen abgewiesen (Q4 aus v1.63.0), kein DB-CHECK.
- FA-Baum: Kürzel an mehreren Werkbänken → Name der bevorzugten (`WerkbankKuerzelFamilie.Bevorzugt`).
- Hilfe „Andere Listen“ korrigiert (fast alles Server-Filter). QA: 2111 Web- / 316 Service-Tests grün, **Testbereit**.

## Deploy-Risiken

Nur Web (Dienst im Bündel wegen v1.63.0 ohnehin). Nach dem Start `/Settings` › BDE: `BdeTerminalMaxHauptFa = 5`. Randfall AKE: mehrere
Aufträge mit gleicher OrderNumber stehen jetzt beieinander (Prüfabfrage in der Spec, Q4). Sage-Etikett-Inhalt (P3) im UAT notieren.
