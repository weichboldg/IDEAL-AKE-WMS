---
type: aufgabe
status: offen
created: 2026-11-17
quelle: Gerald, Chat 2026-11-17 (/goal)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-17: Arbeitsgänge aus dem WMS in Sage-FAs anlegen (E0 + E1)

Auftrag Gerald 2026-11-17: [[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]] im Bündel-Worktree (Basis `1f0361dd`, v1.99.37)
umsetzen, E0 Vorschau und E1 Anlage über Dienst-Warteschlange gemäß Freigabe-Antworten 1–15 (20:13).
Grenzen: NICHTS an Sage schreiben (kein Schreibtest, auch nicht IDEAL-Test), Schalter auf keinem System einschalten, keine
Einstellungen auf Test/Prod, Publish nur unter `<Worktree>\publish\paket3-v<Version>\` (Paket 1/2 unverändert), kein Merge, kein Push.
Schreibpfad nur per Test mit Fake-Client; auf DEV nur lesend gegen Sage.

Nummern beim Start geprüft (Worktree `1f0361dd`): Version **1.99.38**, SQL **160**, TS-**159**, ADR **0033** (falls nötig) – frei.

## Übersicht

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]] | Epic | 160 | 1.99.38 | InUmsetzung (feature-complete, qa-agent ausstehend) | `825b8f58` `52a44b71` `e5ce82da` | E1b Schreibtest (Freigabe Gerald, Rückfrage 7); Feldnamen P-1..P-4 |

## Protokoll

- 2026-11-17: Lauf gestartet, Nummern geprüft, Spec auf InUmsetzung.
- 2026-11-17: E0 Vorschau `825b8f58`; E1a Warteschlange + Dienst-Schritt (Fake-HTTP) `52a44b71`; Version 1.99.38, Hilfe/Changelog, README,
  TS-159, docs/betrieb, Manifest `e5ce82da`. Build 0 Fehler; Web 3248/1 übersprungen, Service 766 grün. Nichts an Sage geschrieben, kein Schalter
  an, kein Publish. Brain: Spec-Nachweis, [[2026-11-17-v1-99-38-arbeitsgaenge-in-sage-fa-anlegen]], Fallstrick 49, kein ADR 0033 (Muster Wareneingang).
- **Nächste Schritte:** qa-agent (Publish Paket 3 nach `<Worktree>\publish\paket3-v1.99.38\`, TS-159 auf IDEAL-DEV mit leerem Bearbeiter);
  P-1..P-5 per GET am IDEAL-Ziel; danach E1b Schreibtest nur nach Freigabe Gerald.
