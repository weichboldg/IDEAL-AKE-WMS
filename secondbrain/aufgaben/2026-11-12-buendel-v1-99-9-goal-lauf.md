---
type: aufgabe
status: erledigt
created: 2026-11-12
---
# /goal-Lauf 2026-11-12 (2): System-Kürzel Sage-Kennung, Lieferverzug-Mail, Barcode + CSV

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Start v1.99.8 `bb788f0e`.
Auftrag Gerald 2026-11-12 (Chat, /goal-run). Reihenfolge laut Auftrag; Versionen, SQL-Nummern und TS-Kapitel in Bau-Reihenfolge
(löst die Kollision v1.99.9/SQL 144/TS-128 der Entwürfe auf). Grenzen: keine Mail an externe Adressen (DEV nur Testbetrieb bzw.
Datei/Testpostfach), kein POST nach Sage, kein Schreiben auf echte UNC-Pfade außer einem DEV-Ordner, kein Lauf gegen Zielsysteme,
kein Push, kein Merge.

| Spec | Typ | SQL-Block | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]] (nur Freigabe-Nachtrag 14:20 System-Kürzel) | Epic (Nachtrag) | SQL/144 | 1.99.9 | **Testbereit** (QA 2026-11-12) | `115e8a09`, `c47f0136`, QA `4c1b5523`; Brain `443ee5fc`, QA `0aa7891c` | – |
| [[2026-11-12-offene-bestellungen-mahnmail-lieferverzug-spec]] | Spec | SQL/145 | 1.99.10 | **Testbereit** (QA 2026-11-12) | `37c831a4`, `dae22a49`; Brain `c29cdda7`, QA `ebc0e7ff` | – |
| [[2026-11-12-wareneingang-barcode-lieferschein-csv-export-spec]] | Spec | SQL/146 | 1.99.11 | **Testbereit** (QA 2026-11-12) | `895646a5`, `0dc0cea0`; Brain `5ff80451`, QA `56fda95c` | – |

- Vorprüfung 2026-11-12: Teil 2 stand auf Testbereit, der Auftrag nennt den Nachtrag ausdrücklich (ersetzt die Freigabe). Mahnmail und
  Barcode/CSV: Freigegeben, alle Freigabe-Antworten ausgefüllt, kein offener BLOCKER, Freigabe-Nachtrag Mahnmail (Antwort 7/12) gilt vor dem Rumpf.
- v1.99.9: SM# hat dasselbe Problem → gleiches Kürzel (`SM#<K>#<id>#`), Kennung je Queue-Eintrag gespeichert. Kürzel muss mit Buchstaben beginnen (enger als Nachtrag, begründet). DEV AKE/IDEAL deployt, `Sync:SageSystemKuerzel = DEV`, 5 Sync-Schalter true. Publish v1.99.9 `4c1b5523`.
- v1.99.10: TS-128. Neue Option `MailSettings:PickupDirectory` (.eml statt SMTP; in Prod NIE setzen). DEV AKE/IDEAL: Pickup `D:\Dev\mail-pickup\ake|ideal` bleibt gesetzt, `Lieferverzug:MailAktiv` false, Empfänger `.invalid`. QA-Lauf AKE: 92 Testbetrieb-Mails + 1 Sammelliste als .eml, 0 SMTP. Publish v1.99.10 `dae22a49`.
- v1.99.11: TS-129. CSV-Schritt auf DEV nur bei gestopptem Dienst per Harness geprüft, geschrieben nur nach `\localhost\D$\Dev\csv-ausgang\ake`. Sage-WE/Lagerbuchung bei laufendem Dienst nie an. QA-Folgeaufgabe [[2026-11-12-freshinstall-leere-db-fehler]] (00_FreshInstall auf leerer DB, außerhalb des Diffs).

## Abschluss (2026-11-12)

- Alle drei Specs Testbereit. Publish `<Worktree>\publish` = v1.99.11, Commit `0dc0cea0` (Web + Dienst, SQL/144–146 additiv).
- DEV-Endkontrolle AKE (selbst gelesen): 5 Sync-Schalter true; SageLagerbuchungAktiv/SageWareneingangBelegAktiv/WareneingangCsvAktiv false;
  `Sync:SageSystemKuerzel = DEV`; `MailSettings:PickupDirectory` in beiden DEV-Diensten gesetzt.
  **Auffällig:** `Lieferverzug:MailAktiv = true` mit Empfänger `gerald.weichbold@ake.at`, Reply-To/Absender `einkauf@ake.at` – abweichend vom
  QA-Bericht (false, `.invalid`). Nicht zurückgesetzt (sieht nach bewusster Änderung aus); Mails landen dank Pickup nur als .eml, Testbetrieb an.
- Kein Sage-POST, keine Mail per SMTP, kein Zielsystem, kein Push, kein Merge.
- Offen für Gerald: Schranke 2 je Spec (Checklisten), TS-116.26/27/28/29 in Sage, P-M1/P-M3/P-M4 vor Ende Testbetrieb, echtes SMTP-Relay,
  UNC-Freigabe mit Dienstkonto-Rechten (P-C1/P-C2), Netum/Kamera-Scan, Kürzel je Testsystem (AT/ATI/IT) setzen.
