---
typ: backlog
status: erledigt
created: 2026-11-19
herkunft: "Gerald 2026-11-19 09:32 (v1.99.43): Empfängergruppen unter Stammdaten nicht auswählbar, über Link schon"
standort: beide
art: bugfix
---
# Bugfix: Menü „Empfänger“ (Empfängergruppen) nur sichtbar, wenn Bedarfsmeldungen aktiv sind

## Befund (Koordination, Bündel v1.99.43, nur gelesen)
- `IdealAkeWms/Views/Shared/_Layout.cshtml:374-378`: die Einträge „Empfänger“ und „Artikelgruppen-Zuordnung“ (`OrderRecipients`)
  stehen in `@if (bestellungenAktiv)`; `bestellungenAktiv` = Einstellung `BestellungenAktiv` (`_Layout.cshtml:58`).
- Die Blechfertigung braucht seit v1.99.x ebenfalls eine Empfängergruppe (`BfEmpfaengergruppeId`, `Models/AppSettingDefinitions.cs:190`;
  ohne Gruppe ist „Abschicken“ gesperrt, `Services/BfAuftragWorkflow.cs:159-161`). An IDEAL ist `BestellungenAktiv` aus → Menü fehlt,
  die Seite `/OrderRecipients` ist per Link erreichbar.

## Vorschlag
- „Empfänger“ zeigen, wenn `BestellungenAktiv` **oder** `BlechfertigungAktiv` an ist (Rechte unverändert, Stammdaten-Lesen).
- „Artikelgruppen-Zuordnung“ betrifft nur Bedarfsmeldungen → bleibt an `BestellungenAktiv`.
- Vor dem Bau prüfen, ob weitere Nutzer der Gruppen existieren (Lagerbestellungs-/Lieferverzug-Mail), und die Bedingung dann daran ausrichten.
- Kleiner Fix, passt in den nächsten Sammellauf (mit Review S-2, Ziel-Kästchen Kommissionierliste, Rollen-Seed).

## Erledigt
2026-11-19 in v1.99.44, Commit `6aa5ac4f` (Bedingung zusätzlich `LagerbestellungAktiv`: Lager-/Glasbestellung nutzen die Gruppen ebenfalls), Bündel `feature/2026-08-07-ideal-teile-1-5` (nicht gemergt). Changelog [[2026-11-19-v1-99-44-sammel-bugfix]], TS-166.
