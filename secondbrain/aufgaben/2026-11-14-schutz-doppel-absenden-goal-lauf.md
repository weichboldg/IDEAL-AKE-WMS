---
type: aufgabe
status: erledigt
created: 2026-11-14
---
# /goal-Lauf 2026-11-14: Schutz gegen Doppel-Absenden und unsichtbare Positionen

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Start v1.99.22 `90bbbbd7`.
Auftrag Gerald 2026-11-14 (Chat, /goal-run). Freigabe-Nachtrag 2026-11-14 09:00 geht dem Rumpf vor (Client-Sperre **global** für alle
POST-Formulare, Ausnahmen GET / `target=_blank` / `data-mehrfach-erlaubt`, Freigabe nach 10 s und bei `pageshow`; alle POST-Formulare prüfen,
Opt-outs im QA-Nachweis). Version ab 1.99.23, SQL ab 150, TS ab TS-138. Nach jedem Build beide Apps nach `<Worktree>\publish`.
Tests auf DEV AKE und IDEAL inkl. Doppelklick-/Parallel-POST-Nachstellung K-1, N-2, WE-1, BDE-1, K-6.
Grenzen: nichts an Sage schreiben, keine Mails an externe Adressen, keine Einstellungen außerhalb DEV, kein Merge, kein Push.

| Spec | Typ | SQL-Block | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec]] | Spec | 150 | 1.99.23 | TS-138 | Testbereit (QA 2026-11-14) | `32dc3119`, `648d09fe`, `1969e3b3`, `7cd357f0`, `510b3020`, QA `1a73bc6a` | – |

- Vorprüfung 2026-11-14: `status: Freigegeben` (Gerald 2026-11-14), Antworten 1–6 vollständig, Nachtrag 09:00, keine Varianten-Spec,
  worktree/branch = Bündel. Nächste freie SQL im Worktree: 150 (letzte 149). Nächster ADR: 0026. Rückfrage 3 kein Blocker (Gerald 09:01).

## Plan (Reihenfolge, Spec-Abschnitte)

1. B1 `Nummernkreis.IstSqlFehler` (Kette) + Tests.
2. C1 K-1 Abschluss: Statusprüfung im Versuch, Fehler vor FA-Status-Block + Tests.
3. C2 K-6 `ToggleRelease(release)` + Views + Tests.
4. C3 WE-1 Absendekennung: Model, Migration, SQL/150, FreshInstall ×2, Repository, Controller, View + Tests.
5. C4 BDE-1 Applock + Client.
6. C5 N-1 `BomViewModel.VaterPosition` + View + Tests.
7. C6 CSV `cleanValue` + feste Kultur de-DE + Node-Check.
8. B2 globale Sperre `site.js` (Nachtrag) + Opt-out-Inventur aller POST-Formulare; AJAX-Helfer `wmsSperren`.
9. Version 1.99.23, Changelog, Hilfe, TESTSZENARIEN TS-138 + TS-86.5, Manifest; build/test, publish, qa-agent.

## Stand

- 2026-11-14 feature-complete `510b3020`: Build 0 Fehler, Web 2872/0/1 skip, Dienst 626/0; Node `csv-export.check.js` grün.
  Migration `20261114080950_AddPurchaseOrderReceiptAbsendekennung`, SQL/150, FreshInstall 2 Stellen, Manifest `SQL/150` + Info. Publish
  `<Worktree>\publish` = v1.99.23 `510b3020` (deploy.zip neu 09:19).
- POST-Formular-Inventur (Nachtrag): 129 POST-Formulare in 73 Views; keine POST-Aktion liefert eine Datei; kein `target="_blank"`/`formtarget`;
  Formulare mit Seiten-Listener (`Bom` transferForm, Leitstand Sammelfreigabe) brechen per `preventDefault` ab → automatisch frei;
  `confirm`-Formulare (6) → bei „Abbrechen“ frei; `form.submit()` aus Skripten (Filter-`onchange`, Druck) nicht betroffen. **Opt-outs
  `data-mehrfach-erlaubt`: keine nötig.**
- Brain: ADR 0026, [[fallstricke]] §38 + §8-Hinweis + „keine feste Request-Kultur“ als überholt markiert, Changelog
  [[2026-11-14-v1-99-23-schutz-doppel-absenden]], [[testszenarien-index]] TS-138, [[feature-map]].
- 2026-11-14 QA (qa-agent): **Testbereit**. Build 0 Fehler, Web 2872/0/1, Dienst 626/0. Deploy DEV AKE + IDEAL v1.99.23 (exit 0), SQL/150 beide DEV-DBs idempotent. Parallel-POST-Nachstellung K-1 (3+1 Läufe), N-2 (4 Läufe, verpackter Deadlock im Log wiederholt), K-6, WE-1 (4 Läufe, 2601 abgefangen), BDE-1 (6/6) grün. Befunde F1 (gleichzeitige Freigabe desselben FA: grüne „0 angelegt“-Meldung) offen, F2 (TS-138.6a) behoben `1a73bc6a`. Inventur der 129 POST-Formulare bestätigt: keine Opt-outs. Publish neu (Commit `1a73bc6a`). Nachweis in der Spec.

## Abschluss 2026-11-14

- **Testbereit** (qa-agent, QA `1a73bc6a`, Brain `73e819a0`): Build 0, Web 2872/0/1, Dienst 626/0; DEV AKE + IDEAL v1.99.23, SQL/150 auf beiden DEV-DBs.
  Gleichstände nachgestellt: 138.1b 3/3, 138.3 4/4, 138.4b 4/4, 138.5b 6/6 – kein 500, je genau ein Ergebnis. Nichts an Sage, keine Mail.
- Offen für Gerald (Checkliste in der Spec): 138.2c Modal, 138.4d Barcode mit Sage-WE, 138.5a/5c Touch-Terminal, 138.6b AKE Glas/VK, 138.7 CSV, 138.8c, 138.9;
  Entscheidung **F1** (gleichzeitige Freigabe desselben FA meldet grün „0 Aufträge … ohne offene Restmenge“ statt „bereits freigegeben“ – Daten korrekt).
- Hinweise F3–F5 (ERR-Logzeilen abgefangener 2601/1205, Applock nur Fertigmeldung, `assignedPickerId` ungeprüft) – nicht gebaut.
- Kein Merge, kein Push. Schranke 2 beim Menschen.

## Nach-QA F1 2026-11-14

- Gerald entschied F1: zweite gleichzeitige Freigabe meldet „FA … ist bereits freigegeben – nichts geändert.“ Umsetzung `08672fcc`
  (bedingtes UPDATE in `SetReleaseAsync`, Rückgabe `bool`), TS-138.3b.
- qa-agent: Build 0 Fehler, Web 2874/0/1, Dienst 626/0; Review ohne Befunde (Restrisiko ohne Transaktion akzeptiert, siehe Spec).
  DEV AKE + IDEAL v1.99.23 (exit 0). TS-138.3b auf AKE: 4 Läufe Freigabe parallel + 2 Läufe mit per `UPDLOCK` erzwungenem Gleichstand
  (Freigabe und Rücknahme) – je ein Auftrag, eine grüne + eine gelbe Antwort mit exaktem Text, Gewinner in der DB, kein 500.
  Publish neu (Commit `08672fcc`). **F1 erledigt, weiter Testbereit.** Nachweis: [[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec]].
