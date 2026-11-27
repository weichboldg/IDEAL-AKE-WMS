---
type: aufgabe
status: erledigt
created: 2026-11-13
---
# /goal-Lauf 2026-11-13: Bugfix AKE-Regression, SData Teil 2, Wareneingang Teil 3

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Start v1.99.12 `56906459`.
Auftrag Gerald 2026-11-13 (Chat, /goal-run). Reihenfolge laut Auftrag (Bugfix → SData Teil 2 → WE Teil 3); Versionen ab 1.99.13,
TS ab TS-130, SQL ab 147 in Lauf-Reihenfolge. Freigabe-Nachtrag 2026-11-13 08:39 gilt je Spec vor dem Rumpf.
Grenzen: nichts an Sage schreiben (SData nur GET mit orderBy über DEV), keine Mails an externe Adressen, kein Merge, kein Push.
WE Teil 3: E0 (P-W8 lesend, Abweichung = Blocker), E1, E3; **vor E2 anhalten** – fertigen Request zeigen, auf Geralds OK warten.
Nach jedem Build beide Apps nach `<Worktree>\publish`.

| Spec | Typ | SQL-Block | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-12-befunde-ake-regression-dev-spec]] | Spec (Bugfix) | – | 1.99.13 | TS-130 | **Testbereit** (QA 2026-11-13) | `7cc8e684`, `a8dd9d2e` | – |
| [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]] | Spec | ab 147 (Gate-Zeile löschen) | 1.99.14 | TS-131 | **Testbereit** (QA 2026-11-13) | `7f3b1a66`, `cd38fbaa`, `0b1cc9a2`, `7048071a` | – |
| [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]] | Spec | – | 1.99.15 | TS-132 | **Testbereit** (QA 2026-11-13, E0+E1+E3) | `5c3538b3`, `47c95931`, `de4a85ac`, QA `e92894c9` | E2 wartet auf Gerald (gewollt) |

- Vorprüfung 2026-11-13: alle drei Freigegeben (Gerald 2026-11-13), alle Freigabe-Antworten ausgefüllt, keine Varianten-Spec ohne
  Entscheidung (Bugfix Punkt 2 = Variante A per Antwort 1), worktree/branch = Bündel.
- Bugfix Antworten 5/6: Backlog-Einträge [[2026-11-13-doppelte-kommissionier-prio-nach-ruecknahme]] und [[2026-11-13-utc-schreiber-auf-lokalzeit]]
  lagen schon vor (08:39/08:45, Freigabe-Sitzung). **Panne:** Der Lauf hat die Prio-Datei um 08:45 ohne vorheriges Lesen mit eigenem Text
  überschrieben (gleicher Befund, gleiche Fundstellen, plus Frage „löschen / MAX mitzählen / zulassen“); der ursprüngliche Text ist nicht
  wiederherstellbar (ungetrackt). Gerald bitte gegenlesen.
- v1.99.13: feature-complete `7cc8e684` (Build 0 Fehler; Web 2733/0/1 skip). Zusatzfund `a8dd9d2e`: `LieferverzugMailTests.Lauf_Testbetrieb_UmgeleitetProtokolliertEinmalAmTag`
  (v1.99.10) war ab 13.11. rot, weil `LieferverzugMailService.ProtokollAsync` mit `DateTime.Now` statt mit dem `jetzt` des Laufs stempelte →
  Laufzeit durchgereicht (Produktion übergibt `DateTime.Now`, `SyncWorker.cs:497`, kein Verhaltenswechsel); Service 599/599.
  Prüfabfrage Altdaten `dbo.PartRequisitions` (lesend): AKE Prod 3 Zeilen, AKE_WMS_DEV 16, IDEAL_WMS_DEV 0; **IDEAL Prod nicht erreichbar → Gerald**.
  Publish v1.99.13 = `a8dd9d2e`.
- v1.99.13 QA 2026-11-13: Build 0 Fehler, Web 2733/0/1 skip, Dienst 599/0; DEV AKE+IDEAL deployt; Browser-Teil erst im dritten Anlauf (zwei
  Chrome-Instanzen verbunden; Gerald wählte Browser 1, der localhost:8081 nicht erreicht, dann Browser 2). Alle TS-130 OK außer: 130.3.3
  (Mailversand, nur Testsystem), 130.4.2 abweichender Quell-Platz, 130.4.3/4 (hätte KO-2026-00008 abgeschlossen), Umbruch langer Wagen-Liste
  → Manual-UAT. Befunde R-1 (`AppVersion.Date` 12.11. – erledigt sich mit 1.99.14), R-2 Brain-Changelog (nachgezogen). Testdaten in AKE_WMS_DEV:
  Bedarfsmeldungen 17/18, Bewegungen 19426–19428, KO-2026-00008 Pos 9/20/31 umgebucht.
- v1.99.14 QA 2026-11-13: Build 0 Fehler, Web 2779/0/1 skip, Dienst 603/0; DEV AKE+IDEAL deployt (v1.99.14), Publish `0b1cc9a2`.
  **Nicht Testbereit:** Befund Q-1 – Einrückung Stückliste live kulturabhängig (`Stueckliste.cshtml:106`, `0,5rem` ungültig), AK 11. Leichte
  Befunde Q-2…Q-6 in der Spec. DEV-Konfig: Dienst-`DataProtection:KeysPath` an AKE und IDEAL ergänzt (bleibt); SQL/147 auf IDEAL_WMS_DEV ausgeführt.
- v1.99.14 QA-Nachprüfung (Fix `7048071a`): Q-1/Q-2/Q-3/Q-6 erledigt, im Browser geprüft (Einrückung, `Anzahl=abc`/`FaNummer=abc`); Web 2781/0/1,
  Dienst 603/0; DEV AKE+IDEAL neu deployt. **Testbereit** 2026-11-13; Q-4/Q-5 als Hinweise in der Checkliste.
- v1.99.15 QA 2026-11-13: Build 0 Fehler, Web 2793/0/1 skip, Dienst 608/0; DEV AKE deployt (v1.99.15); P-W8 vom qa-agent reproduziert (108/0/0/2).
  Abgleich auf DEV per einmaligem Scratch-Lauf des Sync-Service (Schalter blieb aus): EBB 1059 + EBZ 108. TS-132.1/132.2/132.6/132.7 OK; 132.3 (Schalter
  Sage-WE + Dienst-Stopp vom Berechtigungssystem abgelehnt) und 132.1.8 offen für den Menschen. Befund D-1 (TS-132.2.9 Erwartung) im QA-Commit
  `e92894c9` behoben. **Testbereit** (E0+E1+E3); E2 offen. Publish `e92894c9`. Hinweis: 107/108 Abrufe überfällig (ab 2019) → Klärung mit Till vor Echtbetrieb.

## Abschluss (2026-11-13)

- Alle drei Specs Testbereit. Publish `<Worktree>\publish` = v1.99.15, Commit `e92894c9` (Web + Dienst; SQL/147 aus v1.99.14 additiv).
  Nächste freie SQL-Nummer 148, nächstes TS-Kapitel TS-133.
- Kein Sage-POST/PUT/DELETE, keine Mail per SMTP (Lieferverzug-Lauf nur als .eml in `D:\Dev\mail-pickup\ake`), kein Push, kein Merge.
- **E2 (WE Teil 3, Sage-Schreibtest) wartet auf Gerald:** Request in der Spec, Abschnitt E2. Vor dem Senden: `Sync:SageWareneingangMaxWiederholungen = 0`
  (auf DEV 3), Konstante `RahmenabrufMitSageWe = true` als eigener Commit + DEV-Deploy, Ziel-Platz GL;0;0;0, Memo-Id ist dann ≥ 13 (Id 11/12 durch QA belegt),
  Erfolg nur mit `ReferenzBelID = 363218`. Interaktiv, nicht im /goal-Lauf.
- DEV-Änderungen: `DataProtection:KeysPath` in beiden Dienst-`appsettings.DEV.json` (bleibt); SQL/147 auf IDEAL_WMS_DEV; AKE_WMS_DEV: alle alten
  `PurchaseOrderReminders` gelöscht und durch den Lauf 12:55 ersetzt, Cache EBB+EBZ, Testdaten (Bedarfsmeldungen 17/18, Bewegungen 19426–19431,
  Receipts 11/12, KO-2026-00008 Pos 9/20/31 umgebucht).
- Offen für Gerald: Schranke 2 je Spec (Checklisten), IDEAL-Prod-Prüfabfrage Bedarfsmeldungen (Changelog v1.99.13), echtes SData-Kennwort eintragen
  (Manual-UAT v1.99.14), Q-4/Q-5 (v1.99.14), TS-132.3 und 132.1.8, Klärung mit Till (107/108 Abrufe überfällig ab 2019 → würden erinnert),
  Prio-Backlog gegenlesen (überschriebene Datei), Zusatzfund Update-Skript `-NurPruefen` Endlosschleife ohne Eingabe / kein DEV-Ziel in `ziele.psd1`.
