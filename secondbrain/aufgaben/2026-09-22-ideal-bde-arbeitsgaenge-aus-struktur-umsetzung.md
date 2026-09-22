---
typ: notiz
spec: "[[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]"
status: InUmsetzung
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: IDEAL BDE-Arbeitsgänge (WorkOperation) aus Struktur + Werkbank-Anlage aus Sage (EPIC)

Spec [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] · **bestehender Bündel-Worktree** ·
**EPIC, zwei Etappen** (A = Baustein a; B = b+c). Supersedes v1.41.0-FaWorkStep-Spec.
`depends_on` ADR-0014-Rückbau (Testbereit im Worktree ✓).

## Pre-Flight-Verdikt (2026-09-22)

- status Freigegeben, open_questions [], freigabe_entscheidung/_von/_am gesetzt.
- Kritische Prüfung 2. Durchgang: **B6 (fehlender Unique-Index) ausgeräumt** — `affected_code` führt den
  eindeutigen gefilterten Index `IX_ProductionWorkplaces_SageArbeitsplatznummer WHERE ... IS NOT NULL`
  jetzt explizit auf. Kein offener Blocker.
- `depends_on` ADR-0014-Rückbau umgesetzt (Testbereit, Commit `0aceee8`). ✓
- Worktree-Stand: AppVersion **1.43.0**, SQL max **92** → SQL/**93** frei (Spec bestätigt).

## Etappen-Scope DIESER Lauf: NUR Etappe A (Baustein a), danach STOPP + melden

Etappe A = `ProductionWorkplace`-Anlage aus Sage `KHKPpsArbeitsplaetze`:
1. `ProductionWorkplace.cs`: `SageArbeitsplatznummer` (string?, NVARCHAR(31), **Verknüpfungsschlüssel**,
   String/getrimmt/Ordinal, **eindeutiger gefilterter Index** `WHERE IS NOT NULL`) + `ArbeitsschrittCode`
   (string?, MaxLength 20, getrimmt).
2. Migration `AddProductionWorkplaceSageFields` + `SQL/93_*.sql` (2 COL_LENGTH-Guards + Unique-Filter-Index
   mit eigenem IF-NOT-EXISTS) + FreshInstall (beide Stellen).
3. **`ProductionWorkplaceSyncService`** (NEU, raw Sage-Read `KHKPpsArbeitsplaetze`): Filter (Mandant,
   `Aktiv=-1`, `USER_ArbeitsSchritt` gefüllt+getrimmt, nicht auf Ausschlussliste) → Upsert je Zeile
   (kein Treffer=anlegen `BdeAktiv=false` / Abweichung=überschreiben+melden / gleich=nichts /
   Unique-Verstoß=je Zeile abfangen+melden+weiter); S1-Sammelmail; SyncLog `ProductionWorkplaceSync`.
4. `ServiceSettingDefinitions`: `Sync:ProductionWorkplaceSyncEnabled` (Bool false),
   `...Mandant` (Int 1), `...Ausschlussliste` (String `STO,XXX,x01,BS,BS2,FRE,AKE`).
5. `StandortSettingsCatalog`: 2 Felder (Mandant + Ausschlussliste, neue Gruppe).
6. `SyncLogServices`: Konstante `ProductionWorkplaceSync`.
7. `SyncWorker`: neuer **Doppel-Gate**-Block (Master + Toggle), vor Baustein b.
8. `Program.cs`: DI.
9. Views `/ProductionWorkplaces` (Index/Edit/Create): read-only Anzeige + neue Spalten + Spaltenfilter
   (frontend-design PFLICHT).

**NICHT dieser Lauf:** Etappe B (b+c) — WorkOperation-Umbau, Umbenennungen, BdeDefault-Fix, TS-77.
**Kein Testbereit** (Epic-Ende erst nach Etappe B). Etappe A = eigener buildbarer Commit, dann STOPP.

## Arbeitsstand

- 2026-09-22: Spec Freigegeben → InUmsetzung. Baustein-(a)-Entwurf gelesen. Bau startet.
