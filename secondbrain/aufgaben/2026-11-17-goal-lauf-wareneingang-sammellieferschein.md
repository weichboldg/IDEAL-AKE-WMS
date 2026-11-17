---
type: aufgabe
status: offen
created: 2026-11-17
quelle: Gerald, Chat 2026-11-17 (/goal Wareneingang Sammellieferschein + Bestellkopf)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-17: Wareneingang Sammellieferschein + Bestellkopf AB-Nummer/Kommission

Auftrag Gerald 2026-11-17: [[2026-11-17-wareneingang-sammellieferschein-und-bestellkopf-spec]] im Bündel-Worktree umsetzen (Basis v1.99.32,
`5960b6a5`). Freigabe-Nachtrag 11:23 geht dem Rumpf vor: Teil B über RealTimeData `dtsEKBelegMst.Sage.Wawi` (nur GET, kein `where` auf
USER-Aliasse) jetzt mitbauen; Spalten in Lager-/Einkauf-Liste, bei der Einbuchung nur im Kopf; Teil A auf DEV, Prod erst nach enaio-Bestätigung.
Grenzen: nichts an Sage schreiben, keine Einstellungen auf Test/Prod, kein Merge, kein Push, nichts außerhalb Worktree und `secondbrain\`.

Nummern beim Start geprüft: Version 1.99.32 → **1.99.33**; SQL letzte 155 → **SQL/156** (laut Auftrag; BF-Spec noch Entwurf, rückt nach;
die weiche Reservierung „ggf. SQL/156“ für Bestellvorschläge E8 (Blocker) rückt ebenfalls nach); TS letzte 155 → **TS-156**.

## Übersicht

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-17-wareneingang-sammellieferschein-und-bestellkopf-spec]] | Spec | 156 | 1.99.33 | InUmsetzung (feature-complete, QA offen) | `32338503`, `19ce63ef` | E-1 enaio (nur Prod-Einschalten); P-2a Spaltenlängen |

## Protokoll

- 2026-11-17: Lauf gestartet, Spec auf InUmsetzung, Umsetzung im Worktree beauftragt.
- 2026-11-17: Umsetzung feature-complete im Worktree. `32338503` (Teil A Controller `SammellieferscheinGrund` + Dienst `Dateiname`/
  `BelegteDateienAsync`; Teil B `KoepfeAsync` über `dtsEKBelegMst`, SQL/156, Spalten + Katalog + column-config, Kopf der Einbuchung; Manifest;
  Version 1.99.33) und `19ce63ef` (Doku: Changelog, Hilfe, README, anwender 05/06, TS-156 + Hinweise TS-129.5/129.14). Build grün (28 Warnungen,
  Bestand), `dotnet test` grün: Web 3117 bestanden / 1 übersprungen (3118), Dienst 702/702.
- Bestandstests bewusst geändert: `WareneingangCsvServiceTests` AK11-Kollision → `DateiExistiert_NieUeberschrieben_NaechsteNummer_KeinFehler_AK8`
  (AK 8 ändert das Verhalten absichtlich); `EinkaufBestellungenSyncTests` Zählung der Client-Aufrufe nur noch `QueryAllAsync` (Kopf-Lesen ist ein
  zusätzlicher `GetAsync`); `NachlaufManifestDriftTests.SchemaReihenfolge` um SQL/156 ergänzt (Pflege-Liste).
- Vom Drift-Wächter gefunden: die dritte Spaltenquelle `column-config` in `PurchaseOrders/Index.cshtml` fehlte zunächst (`ViewKeyDriftGuardTests`).
- Offen: keine Live-Leseprobe gegen AKE-Test (Variante B nur per Unit-Test mit echten Feldnamen); P-2a (Spaltenlängen in Sage-SQL); E-1 enaio
  vor Prod; qa-agent-Lauf (nicht Teil dieses Auftrags).
