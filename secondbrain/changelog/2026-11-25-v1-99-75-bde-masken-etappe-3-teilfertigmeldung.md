---
type: changelog
version: 1.99.75
date: 2026-11-25
---
# v1.99.75 — BDE-Teileliste: Teilfertigmeldung mit Restmenge

Spec [[2026-11-25-bde-masken-vereinheitlichung-spec]], Etappe 3. Commit `94be5444` (Worktree `feature/2026-08-07-ideal-teile-1-5`).
**Migration `20261125191311_AddBdeTeilMeldungIstAbschliessendUndStandardsicht`, SQL/169**, Web und Dienst (Dienst nur Versionsnummer). TS-196,
ADR [[0037-teilfertigmeldung-nur-abschliessende-meldung-eindeutig]]. Nicht gemergt.

## Kernpunkte

- Melden-Fenster mit „Teilfertig melden“ (Zeile bleibt offen, Gut > 0) und „Fertig melden“ (schließt ab); Vorgabe Gut = Restmenge.
- Statuszelle „gemeldet X von Soll“ + Liste aller Meldungen (Teil/Fertig), Zurücknehmen je eigener Meldung über ein eigenes Bestätigungsfenster.
- Block und Summen-Haken melden die Restmenge als Fertig; Teilfertig nur einzeln (Controller weist mehrere Schlüssel ab).
- Übermeldung per Teilfertig gespeichert mit Hinweis „Sollmenge erreicht – bitte Fertig melden“.
- Baugruppe: `ReportCompletionCoreAsync(..., istFinal)`; Teilfertig = nicht finale Buchung, Arbeitsgang bleibt offen.
- AG-Status: teilgemeldet ohne Abschluss = läuft (`TeilmeldungenAnwenden`); gemeldete ausblenden blendet nur Abgeschlossene aus.
- Admin-Liste Teilmeldungen: Spalte „Art“ (Teil/Fertig), server-filterbar.
- `ProductionWorkplaces.BdeStandardsichtTeile` angelegt und vorbelegt (noch **ohne Leser**, Wirkung ab Etappe 4a).
- Manifest: `SQL/169` (Schema, beide, automatisch), Info `v1.99.75-BdeTeilfertigInfo` (IDEAL, nachher).

## Deploy-Risiken

- SQL/169 **vor dem Start von Web und Dienst** (geteiltes EF-Modell). Index-Neuaufbau beider gefilterter UNIQUE (kleine Tabelle).
- Down nicht verlustfrei, sobald eine Zeile mehrere Meldungen hat.
- Browser-Cache am Terminal ggf. leeren (`bde-teileliste.js`).
