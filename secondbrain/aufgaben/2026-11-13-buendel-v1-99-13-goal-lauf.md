---
type: aufgabe
status: offen
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
| [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]] | Spec | ab 147 (Gate-Zeile löschen) | 1.99.14 | TS-131 | InUmsetzung | – | – |
| [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]] | Spec | – | 1.99.15 | TS-132 | InUmsetzung | – | E2 wartet auf Gerald (gewollt) |

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
