---
type: aufgabe
status: erledigt
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
| [[2026-11-17-wareneingang-sammellieferschein-und-bestellkopf-spec]] | Spec | 156 | 1.99.33 | **Testbereit** (qa-agent) | `32338503`, `19ce63ef`, QA-Fix `ed919976` | E-1 enaio (nur Prod-Einschalten); P-2a Spaltenlängen |

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
- 2026-11-17: QA **Testbereit** (Web 3117/0/1, Dienst 702/0/0), QA-Fix `ed919976` (F1 Hinweis „CSV erneut schreiben“ nennt `_n`). DEV AKE+IDEAL v1.99.33
  inkl. SQL/156, Zielordner vorher geleert (Q-3). TS-156.6–156.13/15/16 OK; Live-GET P-1a/P-1b bestätigt (Präfix nötig, ohne Präfix still leer;
  max. Länge 43/99). 156.1–156.5 und 156.14 auf DEV nicht möglich (Sage-WE-Schalter aus, `SData:BaseUrl` nicht verändert). DEV-Testdaten
  zurückgeräumt; AKE-DEV-Cache der offenen Bestellungen durch einen GET-only-Lauf aktualisiert.

## Abschluss (2026-11-17)

**Testbereit.** Worktree-HEAD `ed919976`, v1.99.33, SQL/156. Publish `<Worktree>\publish\` (publish-info: 1.99.33, `ed919976`, 11:57); alte
`publish\publish.zip` (10:19) und `deploy.zip` nicht verwenden. Kein Merge, kein Push, nichts an Sage, keine Einstellung geändert.

Offen für Gerald: E-1 enaio vor Prod AKE (Info Phase Vorher); TS-156.1–156.5 auf dem Testsystem; P-2a Spaltenlängen (250/200 aus SData-Schema),
P-3/P-4 auf Prod; H1 RealTimeData im Dienst ohne eigenen Schalter mit DPS abstimmen; H3 kosmetisch (Erfolgsmeldung nennt `_1`, wenn der
erste WE keine CSV schrieb); Zahnrad-Ausblenden nach Reload; manuelle Checkliste in der Spec.
