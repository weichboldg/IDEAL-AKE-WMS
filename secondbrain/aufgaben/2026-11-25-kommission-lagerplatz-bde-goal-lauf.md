---
type: aufgabe
status: in-arbeit
created: 2026-11-25
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-25: Kommissionier-Nachtrag, Lagerplatz-Anlage Sage, BDE Isolierfräse, BDE-Masken-Epic

Auftrag Gerald 2026-11-25 (`/goal-run` mit vier Specs, Zielbedingung inkl. sauberer Tests + Definition of Done).
Reihenfolge des Auftrags. Start-Stand Worktree: v1.99.69 (eff16911), höchstes SQL 167 (Lücke 164/165 für die
Isolierfräse-Spec reserviert), höchstes TS 190, höchste ADR 0035, letztes Paket 19.

| Spec | Typ | SQL-Block | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-25-kommissionierauftraege-nachtrag-nachts-spec]] | Spec | 168 | 1.99.70 | 191 | **Testbereit** | fa1b754b, Fotos+Publish 6e426113 | Nachtlauf auf DEV nicht beobachtbar (Q4) |
| [[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]] | Spec | – (ADR 0036) | 1.99.71 | 192 (statt 189, vergeben) | **Testbereit** | 5b6fa696, Fotos 47a23d32, QA f085a8da | Live-Sage-Teil: Testlager + GRANTs fehlen (Gerald) |
| [[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]] | Spec | 164, 165 (reserviert) | 1.99.72 | 193 | offen | | |
| [[2026-11-25-bde-masken-vereinheitlichung-spec]] | Epic (1, 2, 3, 4a, 4b, 5) | 169 (statt 168) | 1.99.73–1.99.78 | 194–199 | offen | | |

ADR des Epics: 0037 (statt 0036, an die Lagerplatz-Spec vergeben).

## Vorprüfung
- Alle vier: status Freigegeben (Gerald 2026-11-25), worktree leer bzw. = Bündel, Freigabe-Antworten vollständig.
- Lagerplatz: Prüfbefunde eingearbeitet (b4b29db0), Sage-Test-SQL freigegeben (qa-agent.md ecc50041).
- Epic: B-1 entschieden (freigabe_entscheidung „Teile-Reiter nur mit BdeTeileliste“), S-1/S-3 nachgezogen (b474c4c5).
- Worktree sauber bei Start.

## Protokoll
- 2026-11-25 Start, Specs auf InUmsetzung.
- Kommissionier-Nachtrag v1.99.70: fa1b754b (Brain 639fbd0f), SQL/168 + Migration 20261125161741_AddPickingStatusStuecklisteKleiner, TS-191.1–191.16 + 191.11a, Build 0 Fehler/keine neuen Warnungen, Tests Web 3551/1 skip/0, Dienst 801/0. Befund: Tabelle heißt ProductionOrderPickingStatus (Spec korrigiert). Deploy-Hinweis: erster Dienststart legt offene Nachträge sofort an (Manifest-Info). QA läuft.
- v1.99.70 QA bestanden: DEV AKE+IDEAL v1.99.70, SQL/168 auf beiden DEV-DBs, Fotos 01-19/01-20/01-21 (6e426113). Befunde Q1–Q3 niedrig (belassen), Q4 mittel: DEV-Absicherung schaltet `Sync:KommissionierNachtragEnabled` (Muster `Sync:%Enabled`) ab → Nachtlauf auf DEV nicht beobachtbar, 191.1–191.7/191.9 NICHT MÖGLICH, bleibt Mensch. Prod-Default true = Sofortlauf beim ersten Dienststart. QA änderte kurz die eigene Rolle (UserRoles 40, DEV AKE) und setzte zurück.
- Lagerplatz-Anlage v1.99.71: 5b6fa696 (Brain 58b0caf1), keine Migration, ADR 0036, Fallstrick §66, TS-192, neue Keys SageLagerplatzAnlageAktiv (false) + SageLagerAnlageLagertechniken (Aus), SyncLog-Service SageLagerplatzAnlage, Manifest 4 Einträge. Build 0 Fehler/28 Warnungen, Web 3586/1/0, Dienst 847/0. Zusatzfund: SageLagerplatzReader.cs:26 verbindet ohne Mandant (nicht gebaut). QA läuft (Live-Teil nur mit Testlager + GRANTs + V3).
- v1.99.71 QA bestanden (f085a8da, Fotos 19-01..19-05 in 47a23d32): DEV beide v1.99.71, Build 0/32 Warnungen, Web 3586/1/0, Dienst 847/0. Befunde N1 (Abbruch beim Dienst-Stopp → Fehler statt Offen, Einzeiler vorgeschlagen), N2 (50014 ohne Sage-PlatzID, AK 13), H1/H2 Hinweise. V1/V2 (Testlager) und GRANTs fehlen in ake_TEST2026; V3 Teil 1+2 belegt (svc_idealakewms, DOTNET_ENVIRONMENT=DEV, AKESQL20/ake_TEST2026). Nichts in Sage angelegt (Tan 4378 unverändert), Schalter Endstand aus. Fotos 19-04/19-05 mit per SQL gesetzten Fehlerzuständen.

## Sammel-Fixes für den Schlusslauf (Auftrag Gerald 2026-11-25)
Alle Befunde der Specs werden gesammelt und nach der letzten Epic-Etappe in EINEM Fix-Lauf mit eigener Version + qa-agent behoben.

| # | Quelle | Befund | Vorschlag |
|---|---|---|---|
| F-1 | v1.99.70 Q1 | `PickingController.cs:258` stiller Fallback `?? new List<…>()` | entfernen (Repository liefert nie null) |
| F-2 | v1.99.70 Q3 | „Altbestand geprüft“: Ausnahme in `ErzeugeOffeneAsync` → Fehlerseite, Kennzeichen schon gelöscht | Ausnahme fangen, WarningMessage „Nachtlauf holt nach“ |
| F-3 | v1.99.70 Q2 | Merker „Run gestartet“ auch bei Gesamtfehler → Wiederholung erst am Folgetag | laut Spec F2 gewollt – nur prüfen, ggf. belassen |
| F-4 | v1.99.70 Q4 | DEV-Absicherung (`Sync:%Enabled`) schaltet `Sync:KommissionierNachtragEnabled` ab → Nachtlauf auf DEV nie beobachtbar | Entscheid Gerald: DEV-Ausnahme bzw. QA-Erlaubnis für diesen Schalter (kein Code) |
| F-5 | v1.99.71 N1 | Dienst-Stopp während Sage-Befehl → Eintrag „Fehler“ statt „Offen“ | Einzeiler `when (!ct.IsCancellationRequested && !IstTechnisch(ex))` in beiden catch |
| F-6 | v1.99.71 N2 | Konfliktmeldung 50014 ohne belegende Sage-PlatzID (AK 13) | PlatzID in Meldung; Live-Test gegen ake_TEST2026 nötig |
| F-7 | v1.99.71 QA | Lagercode-Feld kürzt 11 Zeichen still auf 10 | maxlength entfernen/Hinweis, Server-Meldung zeigen |
| F-8 | v1.99.71 Umsetzer | `SageLagerplatzReader.cs:26` verbindet ohne Mandant | Mandant wie Schreibweg |
| F-9 | v1.99.71 H1/H2 | kurzzeitig „fehlend“ beim parallelen Abgleich; Fehlermail je Takt bei unerwarteter Ausnahme | Hinweise, prüfen |
