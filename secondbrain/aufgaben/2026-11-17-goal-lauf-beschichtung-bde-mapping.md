---
type: aufgabe
status: erledigt
created: 2026-11-17
quelle: Gerald, Chat 2026-11-17 (/goal)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-17: Beschichtung E2/E4, BDE vereinfacht (Teileliste Blechfertigung), Arbeitsgang-Mapping

Auftrag Gerald 2026-11-17: drei Teile nacheinander im Bündel-Worktree (Stand v1.99.31, `d3a00101`); je Teil Build + Tests grün, Commit,
qa-agent; am Ende Publish und DEV AKE (8081) + IDEAL (8082). Grenzen: nichts an Sage schreiben, keine Einstellungen auf Test/Prod, kein Merge,
kein Push, keine Specs nach freigegeben/ verschieben. Bei Blockern vermerken und weitermachen.

Nummern: Beschichtung bleibt Changelog-Version 1.99.25 (keine Schema-Änderung, AppVersion bleibt); BDE vereinfacht = **1.99.32** (Spec nannte
1.99.28, belegt durch Sicherheit), SQL/154 frei (151/153 reserviert, 155 belegt); Arbeitsgang-Mapping = Changelog-Version 1.99.27, SQL/153
(reserviert). Die AppVersion steigt nur einmal auf 1.99.32 (BDE) – Arbeitsgang trägt das reservierte Label 1.99.27 im Changelog, ohne die
AppVersion zurückzudrehen.

## Übersicht

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-16-beschichtungslogik-je-standort-spec]] E2/E4 | Epic | – | 1.99.25 (Auslieferung 1.99.32) | InUmsetzung – QA Teilumfang E2/E4 grün | `2336dced` E2, `b4972700` E4 | E5: H2 (P-A5/P-A3/P-A6) und H3 offen |
| [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]] | Spec | 154 | 1.99.32 | **Testbereit** | `6704fe2a`…`c7c968fe`, F1 `5ced750f` | – |
| [[2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer-spec]] | Spec | 153 | 1.99.27 (Auslieferung 1.99.32) | **Testbereit** | `5960b6a5` | OperationConfigSync-Lauf nur manuell (Sync-Schalter auf DEV tabu) |

## Protokoll

- 2026-11-17: Lauf gestartet.
- 2026-11-17: Beschichtung E2 (`2336dced`) und E4 (`b4972700`) gebaut; QA Teilumfang grün (Web 3024/0/1, Dienst 682/0/0; DEV AKE+IDEAL
  TS-141..143 OK, Regression 140/144 OK, Einstellungen/Testdaten zurückgesetzt). Befunde R-1 (Manifest-Info nur Text im 1.99.25-Eintrag) und R-2
  (Versions-Bump 1.99.32 vor Publish) gehen in den BDE-Teil. Status bleibt InUmsetzung (E5).
- 2026-11-17: BDE vereinfacht gestartet (1.99.32, SQL/154).
- 2026-11-17: BDE-Teileliste E1–E7, E9 gebaut (`6704fe2a`…`c7c968fe`), QA **Testbereit** (Web 3104/0/1, Dienst 685/0/0; DEV AKE+IDEAL v1.99.32,
  TS-152 inkl. N1 6 parallele POSTs → 1 Meldung; Einmal-Skript nur Trockenlauf `TREFFER: 119`; DEV-Daten zurück). Befund F1 (Umlaute) behoben
  `5ced750f`; F2 (Kontrast `.btn-success`/`.btn-secondary`, global, alt) → [[2026-11-17-kontrast-btn-success-secondary]]. Brain `abe66087`.
- 2026-11-17: Arbeitsgang-Mapping gestartet (Changelog 1.99.27, SQL/153).
- 2026-11-17: Arbeitsgang-Mapping gebaut (`5960b6a5`), QA **Testbereit** (Web 3110/0/1, Dienst 693/0/0; DEV AKE+IDEAL v1.99.32; SQL/153 zweimal
  und FreshInstall auf leerer DB grün; TS-151 Web-Schritte OK mit SQL-Testdaten, Sync-Schritte 151.1/151.2/N2 NICHT MÖGLICH). Befunde Q-1
  (Regeln doppelt: Ausschlussliste, Abweichung – Drift-Risiko, niedrig), Q-2 (Count `abweichung_hand` inkl. ausgeschlossener, akzeptiert).
  Brain `2975ad8c` (ADR 0031, fallstricke §45), Folgeaufgabe [[2026-11-17-werkbaenke-mit-arbeitsgangnummer-klaeren]].
- 2026-11-17: **QA-Befund Q-3 (DEV-Umgebung):** `D:\Dev\scripts\dev-deploy.ps1:35` leert den Zielordner nicht – eine ältere
  `column-filter-syntax.js` blieb liegen, Spaltenfilter auf DEV AKE+IDEAL tot (JS-Fehler); QA hat die Datei ersetzt. Empfehlung: Zielordner vor
  dem Publish leeren; prüfen, ob `Update-IdealAkeWms.ps1` dasselbe Risiko hat.
- 2026-11-17 10:19: **Publish** `<Worktree>\publish\` – v1.99.32, Commit `5960b6a5`, Web + Dienst + SQL + deploy, `publish.zip` neu.
  DEV AKE (8081) und IDEAL (8082) zeigen v1.99.32 (QA-Deploy 10:08 auf `5960b6a5` = Lauf-Ende, kein weiterer Deploy nötig).
  **Panne:** beim Schreiben der Publish-Info wurde `C:\git\IDEAL-AKE-WMS\publish\publish-info.txt` (Hauptcheckout, Hotfix-Paket v1.30.4 vom
  10.11.) überschrieben – rekonstruiert aus der DLL-ProductVersion (Version 1.30.4, Commit `02500726`), in der Datei als rekonstruiert markiert.
  Kein Merge, kein Push, nichts an Sage geschrieben, keine Einstellungen auf Test/Prod.

## Ergebnis

- Beschichtung E2/E4: QA Teilumfang grün, Spec bleibt **InUmsetzung** (E5 Blocker H2/H3).
- BDE-Teileliste v1.99.32: **Testbereit**.
- Arbeitsgang-Mapping v1.99.27: **Testbereit**.

## Fragen / offen für Gerald

- Beschichtung E5: H2 (P-A5, P-A3, P-A6) und H3 beantworten.
- Manuelle Checklisten in den drei Specs auf IDEAL TEST bzw. AKE TEST (u. a. BDE-Einmal-Skript SCHARF nach DB-Backup + zweiter Trockenlauf 0,
  N1 mit zwei echten Terminals; OperationConfigSync-Lauf; Haken-Änderung Beschichtung mit Anwendern abstimmen).
- 26 Werkbänke mit Arbeitsgangnummer auf IDEAL TEST ([[2026-11-17-werkbaenke-mit-arbeitsgangnummer-klaeren]]); Kontrast-Aufgabe
  [[2026-11-17-kontrast-btn-success-secondary]]; dev-deploy Q-3.
- Aus dem Vorlauf weiter offen: FA-Struktur SData R2/R4/R5.
- Schranke 2: Manual-UAT + Merge des Bündels.
