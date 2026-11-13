---
type: aufgabe
status: offen
created: 2026-11-13
---
# /goal-Lauf 2026-11-13 (Abend): Lieferverzug-Nachbesserung, Einkauf Bestellvorschläge (Epic)

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Start v1.99.18 `d9e4c89e`.
Auftrag Gerald 2026-11-13 (Chat, /goal-run). Reihenfolge: Lieferverzug → Bestellvorschläge. Versionen ab 1.99.19, TS ab TS-136, SQL ab 148
in Lauf-Reihenfolge. Freigabe-Nachtrag 2026-11-13 16:27 geht je Spec dem Rumpf vor; Keyuser-Antworten 2026-11-13 verbindlich (Bestellvorschläge).
Bestellvorschläge: E0 lesend über DEV selbst (Abweichung = Blocker), Schalter Default aus, Rolle `einkauf`, E5 (Dispo-Stapel) blockiert – kein POST.
Nach jedem Build beide Apps nach `<Worktree>\publish`.
Grenzen: nichts an Sage schreiben, keine Mails an externe Adressen (Lieferverzug nur Testbetrieb/Pickup auf DEV), keine Prod-DB, kein Merge, kein Push.

| Spec | Typ | SQL-Block | Version | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|---|
| [[2026-11-13-lieferverzug-nachbesserung-keyuser-spec]] | Spec | – | 1.99.19 | TS-128 erweitert (128.16–128.26) | **Testbereit** (Nach-QA 2026-11-13) | `c31c06b0`, `def97b36`, QA `65496165` | – |
| [[2026-11-08-einkauf-bestellvorschlaege-spec]] | Epic | – (148 ungenutzt, E3 blockiert) | 1.99.20 | TS-136 | **Testbereit** E0+E1+E2+E4+E6 (QA 2026-11-13) | `f5209692`, `31ae0d69`, `9b63dfc6`, `f5f6bd5b`, QA `cf3f41cb` | E3 blockiert (A3 Leseweise → Gerald), E5 blockiert (gewollt) |

- Vorprüfung 2026-11-13: beide `status: Freigegeben` (Gerald 2026-11-13), Freigabe-Antworten vollständig (Lieferverzug 1–5, Bestellvorschläge
  1–10 + 11–13), keine Varianten-Spec, worktree/branch leer bzw. = Bündel. Nächste freie SQL im Worktree: 148 (letzte 147).
- Hinweis: Der Branch wurde vor diesem Lauf auf Geralds ausdrückliche Anweisung im Chat bis `d9e4c89e` gepusht (vor dem /goal-run-Auftrag);
  im Lauf selbst kein Push.
- v1.99.19 feature-complete `c31c06b0`: Build 0 Fehler, Web 2811/0/1 skip, Dienst 619/0; Publish v1.99.19 = `c31c06b0`. Keine Migration.
  **Befund P-L1 (AKE_WMS_DEV, Cache 13.11. 12:44):** erste Welle Kreditor-Mail trotz 180 Tagen 99 Kreditoren (ohne Abrufe 96) > `KreditorMaxJeLauf` 30
  → laut Nachtrag Filter unverändert, Limit entscheidet Gerald. EBZ „zu klären“ 73 Positionen (älteste 03.07.2019).
  Zusatzfund: `IdealAkeWms/appsettings.Development.json` zeigt auf `AKESQL20.ake.at`/`IDEAL_AKE_WMS` (Prod-Name) – nicht benutzt.
- Epic E0 2026-11-13 (nur lesend, Ergebnisse in der Spec „Ergebnisse E0“): Lesepfad `EinkaufPositionenAllInfo` trägt, Lieferant = `A0Empfaenger`,
  MengeBasis = Basiseinheit belegt. **A3 = Blocker E3** (1-Jahres-Fenster findet nur 55 % der letzten Bestellungen; Leseweise entscheidet Gerald) →
  SQL/148 bleibt ungenutzt. A2 (Wächter E4) konservativ: zusätzlich `UmrechnungsfaktorEK = 1`. A1 Ressourcenname Partner-Vertrag, A4 Belegnummer 45.
  P2-Maskenprüfung offen (Gerald). IDEAL_WMS_DEV: 0 Hauptlieferanten.
- Lieferverzug Zwischen-QA (vor Nachtrag 2): Build 0, Web 2811/0/1, Dienst 619/0, DEV AKE+IDEAL v1.99.19 deployt; Review ohne Blocker (Q1–Q7,
  Q2/Q5 werden mit Nachtrag 2 erledigt, Q7 Testindex nach Nachtrag 2). Abgebrochen auf Anweisung, weil Gerald 16:42 **Freigabe-Nachtrag 2**
  (Lieferanten-Artikelnummer in Kreditor-Mail und Einkauf-Liste) nachgereicht hat → Umsetzung läuft, danach Nach-QA mit TS-128.16 ff. auf DEV.
- v1.99.19 Nachtrag 2 `def97b36` (Lief.-Art.-Nr. aus `ArticleSupplier.SageBestellnummer`, `EinkaufPositionenInfo` hat keine – GET belegt; Q2/Q5).
  Nach-QA: Build 0, Web 2811/0/1, Dienst 623/0; N1 (doppeltes Leerzeichen) im QA-Commit `65496165` behoben; DEV AKE+IDEAL v1.99.19; TS-128.16–.26 OK
  außer 128.25 teilweise (ArticleSuppliers nicht geleert). AKE_WMS_DEV: heutige `PurchaseOrderReminders` gelöscht/neu (1120 Einkauf-Zeilen bleiben),
  synthetische Cache-Zeilen 990101–990111 wieder entfernt, Einstellungen zurückgesetzt. **Testbereit**, Publish v1.99.19 = `65496165`.
- Epic v1.99.20: E1 `f5209692`, E2 `31ae0d69`, E4 `9b63dfc6` (Wächter konservativ A2), E6 `f5f6bd5b`; Build 0, Web 2845/0/1, Dienst 623/0;
  Publish v1.99.20 = `f5f6bd5b`. Keine Migration. Rolle `einkauf` ohne Seed (Manifest-Prüfung). QA läuft.
- Epic QA 2026-11-13: Build 0, Web 2845/0/1, Dienst 623/0; Q-1 (Leer-Text) behoben `cf3f41cb`; DEV AKE+IDEAL v1.99.20; TS-136 im Browser gegen
  SQL-Gegenproben OK (offene Bedarfe 13, offen bestellt 7, Banner 16/5/30, Gate, Rolle, Aufrunden 200/290, Wächter RLL); CSV/Buchungen/Update-Skript/
  leerer Cache → Mensch. **Testbereit** (E3/E5 blockiert). DEV: Rolle `einkauf` (Roles.Id 19) auf AKE-DEV bleibt, keinem Benutzer zugewiesen;
  `EinkaufAktiv`-Zeile gelöscht wie vorher; admin-Zuordnung gerald.weichbold (UserRoles 40) wiederhergestellt; Test-Bedarfsmeldungen 19–22 gelöscht.
  Publish v1.99.20 = `cf3f41cb`; `publish\deploy.zip` vom qa-agent neu gebaut (vorher Stand 08:15).

## Abschluss (2026-11-13)

- **Lieferverzug-Nachbesserung v1.99.19 Testbereit** (`c31c06b0`, Nachtrag 2 `def97b36`, QA `65496165`; TS-128.16–128.26). Keine Migration.
- **Einkauf Bestellvorschläge v1.99.20 Testbereit für E0/E1/E2/E4/E6** (`f5209692`, `31ae0d69`, `9b63dfc6`, `f5f6bd5b`, QA `cf3f41cb`; TS-136).
  Keine Migration; SQL/148 + 149 frei. **Blocker:** E3 (A3 Leseweise letzte Bestellmenge – Gerald), E5 (Dispo-Stapel schreibt an Sage – gewollt).
- Publish `<Worktree>\publish` = v1.99.20 `cf3f41cb`. Nächste freie SQL 148, nächstes TS-Kapitel TS-137.
- Kein Sage-POST/PUT/DELETE (nur GETs mit `where`/`orderBy`), keine Mail per SMTP (nur `.eml` in `D:\Dev\mail-pickup\ake`), keine Prod-DB, kein Merge,
  im Lauf kein Push (Push `d9e4c89e` vorher auf Geralds Anweisung).
- **Offen für Gerald:** Schranke 2 je Spec (Checklisten am Spec-Ende); Limit-Entscheidung P-L1 (96–99 Kreditoren > `KreditorMaxJeLauf` 30);
  Durchsicht „Zu klären“ mit dem Einkauf + P-L2–P-L4; Mailclient-Darstellung (breitere Tabellen); E3-Leseweise (A3: Fenster + Nachlesen je Artikel /
  langes Fenster / nur je Artikel); P2 Einheit Mindestbestellmenge in der Sage-Maske (EK002400, EK007623); A2-Wächter ggf. lockern; Rolle `einkauf`
  zuweisen + `EinkaufAktiv` erst nach den vier Abgleichen; Zusatzfund `IdealAkeWms/appsettings.Development.json` zeigt auf Prod-DB-Namen;
  Nebenfund `SagePurchaseOrderLines.Belegnummer` nvarchar(20) < Sage 45; AKE-DEV `SData:Username/Password` leer (appsettings-Rückfall).

## Wiederaufnahme 2026-11-13 Abend (Auftrag Gerald, /goal-run): E3 + Freigabe-Nachtrag 3

Start v1.99.20 `cf3f41cb`. Auftrag gibt E3 und den Zusatz ausdrücklich frei (Freigabe-Nachträge 2 und 3, 18:07–18:12, gehen dem Rumpf vor); Spec
dafür von Testbereit zurück auf InUmsetzung. Versionen ab 1.99.21, TS ab TS-137, SQL ab 148. E5 bleibt blockiert.

| Teil | Version | SQL | TS | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| E3 letzte Bestellmenge (3 Jahre, Fallback Sage-Bestellmenge) | 1.99.21 | 148 | TS-136 erweitert (136.6–136.6g, 136.18–136.20) | umgesetzt, QA läuft | `ca2f49ed` | |
| Nachtrag 3: Belegnummer 45, Wareneingang Layout „Überlieferung“, Hinweis Lagerbestellungen/Bedarfsmeldungen | 1.99.22 | 149 | TS-137 | umgesetzt, QA läuft | `4689922c` | |
| DEV sichtbar (AKE-DEV Gate, Rolle `einkauf` für Gerald, Syncs) | – | – | – | qa-agent | | |
- E3 `ca2f49ed`: Migration `20261113171808_AddArticleSupplierLetzteBestellung` / SQL/148 (`ArticleSuppliers.LetzteBestellmenge`, `-Am`, `-Beleg` nvarchar(45));
  Dienst `ArtikelLieferantSyncService.LetzteBestellungAsync` (EBB+EBZ, 3 Jahre, alle Seiten, jüngster je Artikel/`A0Empfaenger`, `MengeBasis`);
  Rückblick als Konstante `ArticleSupplier.LetzteBestellungRueckblickJahre = 3`; Fallback Sage-Bestellmenge mit Badge/Hinweis/Banner, nur wenn
  letzte Bestellungen schon gelesen sind. Web 2849/0/1, Dienst 626/0.
- Nachtrag 3 `4689922c`: Migration `20261113172929_WidenBelegnummer45` / SQL/149 (Belegnummer 20→45 in 3 Tabellen, `SageWeBelegnummer` 30→45 – Sage-Views
  `BelegnummerFormatiert` varchar(45) belegt; Index `IX_SagePurchaseOrderLines_Belegnummer` drop/recreate); Wareneingang Spalte „Überlieferung“,
  Hinweis offene Lagerbestellungen/Bedarfsmeldungen mit Link (nur Anzeige). Web 2853/0/1, Dienst 626/0. Publish v1.99.22 `4689922c`;
  `deploy.zip` noch v1.99.20 (kein Bau-Skript). Offen: Einheit der Sage-Bestellmenge (angenommen Lagereinheit) → Prüfabfrage Gerald (EK003604).
