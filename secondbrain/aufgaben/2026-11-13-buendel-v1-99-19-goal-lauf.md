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
| [[2026-11-08-einkauf-bestellvorschlaege-spec]] | Epic | – (148 ungenutzt, E3 blockiert) | 1.99.20 | TS-136 | InUmsetzung (E0/E1/E2/E4/E6 erledigt, QA läuft) | `f5209692`, `31ae0d69`, `9b63dfc6`, `f5f6bd5b` | E3 blockiert (A3 Leseweise), E5 blockiert (gewollt) |

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
