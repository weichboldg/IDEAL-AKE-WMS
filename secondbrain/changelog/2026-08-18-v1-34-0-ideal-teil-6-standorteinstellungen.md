---
type: changelog
version: 1.34.0
date: 2026-08-18
---
# v1.34.0 — IDEAL Teil 6: Standorteinstellungen-Maske

Umsetzung der freigegebenen Spec [[2026-07-29-standort-ideal-teil-6-spec]] (kein Epic) im **Bündel-**
Worktree `feature/2026-08-07-ideal-teile-1-5` (gemeinsam mit Teilen 1–5, 7, 8; kein Zwischen-Merge).
Aufgaben-/Umsetzungsnotiz [[2026-07-29-standort-ideal-teil-6]]. Eine kuratierte, gruppierte
Admin-Maske über die standortbezogenen Werte — kein zweiter Speicherort, die Keys bleiben im
bestehenden Katalog (ADR 0008/0011) und auch generisch editierbar.

**Warum im Bündel-Worktree (nicht frischer /dev-Worktree):** Teil 6 `depends_on` Teil 7 (Master-Status
`HierarchischeStrukturStatus` + Umschaltseite) und Teil 3 (Firmendaten-Fallback) — die existieren nur
dort, nicht auf `main`. Der worktree/branch-Leer-Gate von /dev greift hier bewusst nicht; Schranke 1 war
genommen (freigabe_von Gerald Weichbold, 2026-08-12).

## Umgesetzt (`c8ae47f`)
- **Neuer Controller/View** `StandortEinstellungenController` + `Views/StandortEinstellungen/Index.cshtml`
  (`[RequireAdminAccess]`, Class-Level — kein neuer Filter/keine neue Rolle). Nav-Link unter
  Einstellungen. Gruppen: Firmendaten · Mandant/Sage-Views · Feature-Schalter · Struktur-Import.
- **Kuratierte Feldliste** `StandortSettingsCatalog` (Single Source; ohne Master = Allow-List).
- **Atomarer Zwei-Backend-Write** `IStandortSettingsWriter`/`StandortSettingsWriter`: AppSettings +
  ServiceSettings in **einer** EF-Transaktion (der eine bewusst zugelassene Ort mit direktem
  `ApplicationDbContext` — dünner UoW, Spec-erlaubte ADR-0001-Ausnahme). `IsRelational`-Guard (InMemory
  ohne Transaktion, aber ein `SaveChanges`), Cache-Invalidierung **nach** Commit
  (`CachedSettingRepository.CachePrefix` public), ChangeTracker-Reset bei Rollback.
- **Int-Validierung im Controller VOR dem Write** → kein Partial-Save (AK 4); Fehleranzeige behält die
  eingegebenen Werte. Nicht übermittelte Felder werden übersprungen (Teil-Request unschädlich).
- **Master `ProduktionsauftragHierarchisch` read-only** (AK 2): Badge aktiv/flach + gesperrt ja/nein
  (`HierarchischeStrukturStatus`) + Link `/HierarchieUmstellung`. Nie im POST; Allow-List ignoriert
  einen manipulierten Master-Key. Kein `HierarchischeStrukturGuard`-Aufruf.
- **Firmendaten** neu als AppSettings-Keys `Firmenname`/`Firmenanschrift` (`AppSettingKeys.cs`,
  Fallback-Regel). Kein Seed, keine Migration — die kuratierte Maske rendert sie unabhängig von der
  DB-Zeile und legt sie beim ersten Speichern an (AK 5). Bewusste Folge: erscheinen erst danach in
  der generischen `/Settings`-Liste.
- **Nicht in der Maske:** PPS „berechnend vs. View" — der Key `Neuer_PT_PPS` wurde in Teil 5 bewusst
  nie gebaut; ohne Key kein Bedienelement.

## Tests
`StandortEinstellungenControllerTests` (6: Gruppen-Render/Master-read-only/Backend-Split/Int-Fehler-kein-
Write/Master-ignoriert/Admin-Attribut) + `StandortSettingsWriterTests` (3: Update+Insert über beide
Backends / null→"" / nur-Service). Web-Suite **1214 grün** (+9).

## Deploy-Umfang
- **Web:** ja (neue Maske). **Service:** nein. **Migration:** nein (Firmendaten ohne Seed/Katalog).

## Status
`status: InUmsetzung` bis zum QA-Gate (qa-agent). Merge-Commit offen (Schranke 2 fürs ganze Bündel =
Mensch). Nicht gepusht.
