---
typ: notiz
spec: "[[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: BDE Stufe 1 — Werkbank-Familie, veraltete Sage-Werkbänke, nur AG-Fertigmeldung

Spec [[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec]] · **direkt im Bündel**, Start `d75680f4` (v1.62.0),
Ziel v1.63.0, TS-96, Migration SQL/97. Deploy: Web + Dienst + SQL. Variante B, Antworten 1–12, Freigabe-Nachtrag.

## Plan (kompakt — Details auf Datei:Zeile-Ebene in der Spec)

**Global:** Kürzel-Vergleich Trim + OrdinalIgnoreCase in C# (nie SQL); AKE ohne Kürzel = Familie `{W.Id}`; Meldung
„Dieser Arbeitsgang gehört nicht zu dieser Werkbank.“; keine neuen Settings-Keys/Rollen; Pausiert-Liste NICHT filtern.

1. **Schema** — `ProductionWorkplace.SageMissingSince` (DateTime?), `BdeNurAgFertigmeldung` (bool, Default false);
   EF-Migration `AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung`; `SQL/97_…sql` (COL_LENGTH-Guards, eigener
   Batch je DDL, History separat); `SQL/00_FreshInstall.sql` Tabelle + MigrationId.
2. **Helfer** `Services/WerkbankKuerzelFamilie` (statisch): `FamilieIds(w, alle) -> HashSet<int>` (FA-a1),
   `BdeRelevanteIds(alle) -> HashSet<int>` (FA-a7), `Bevorzugt(kandidaten) -> ProductionWorkplace` (BDE-aktiv, dann
   kleinste Id), `NurAgFertigmeldungWirksam(w, nurFaGlobal)` (FA-c2). Tests `WerkbankKuerzelFamilieTests`.
3. **(b) Dienst** — `ProductionWorkplaceSyncService`: Empty-Guard vor Upsert; nach Upsert Kennzeichnen/Entkennzeichnen über
   rohe Präsenz-Menge; Counts `nicht_in_sage`, `nicht_in_sage_neu`, `wieder_in_sage`; Detailzeilen; Warnung BDE-aktiv;
   DryRun ohne Schreiben; Audit `ProductionWorkplaceSync`. Tests in `ProductionWorkplaceSyncServiceTests`.
4. **(a) Erkennung** — Katalog ohne gekennzeichnete; mehrere → anlegen an `Bevorzugt`, Count
   `arbeitsschritt_mehrere_werkbaenke` + Info-Zeile; `arbeitsschritt_mehrdeutig` entfällt. Tests umstellen/ergänzen.
5. **(a) Web-Lesen** — `GetOpenByWorkplaceIdsAsync` (Repo, alte Methode delegiert); `BdeApiController.GetAvailableOperations`
   + `BdeScanResolver.BuildNormalCandidatesAsync` über Familie; FA-Baum-Gate `BdeRelevanteIds`; `NamenLookup` ohne
   gekennzeichnete. Tests Resolver/Controller/Repo/FA-Baum.
6. **FA-a9 + (c) Buchung** — `BdeBookingService`: `EnsureWorkOperationInFamilyAsync` (NurFA: `ProductionOrder.ProductionWorkplaceId
   == workplaceId`); Start-Sperre; `ReportCompletionAsync` (Prüfreihenfolge FA-c4 1–8, Speicherform FA-c5). Controller-Aktion
   `ReportCompletion` (mit `otherActiveBookings` bei multiMa). Terminal-View/JS: Knopf „Fertig melden“, Vorbelegung Sollmenge.
7. **FA-a8 + Stammdaten-UI** — Controller-Guard am DB-Wert; `ProductionWorkplaceRepository.UpdateAsync` nur Pflegefelder;
   Create/Edit: Kürzel (Nicht-Sage), Checkbox (c), Hinweise; Index: Spalte „Nicht in Sage seit“ + Badge
   „nur Fertigmeldung“; Register `ColumnFilterSyntax`. `frontend-design` vorher.
8. **Doku** — 2× AppVersion 1.63.0, Changelog, Hilfe, README, TS-96.1–96.10; Brain (Index, Changelog, feature-map,
   codebase, fallstricke).

## Stand

- 2026-10-19: Umsetzung gestartet.
- 2026-10-19: feature-complete (`4416aac8` … `501008f6`); Build 0 Fehler, 2085 Web- / 316 Service-Tests grün.
- 2026-10-19: QA: Q1 (Default-AG „01“ vor der Prüfung angelegt) behoben `b3daf566`, Q2/Q3 Kleinkram → **Testbereit**. Build + 2088 Web- / 316 Service-Tests grün. Offen gemeldet: Q4 (negative Mengen, alle Einstiege), Q5 (gleichzeitige Doppel-Fertigmeldung), `NamenLookup` bei mehreren aktuellen Werkbänken. Changelog [[2026-10-19-v1-63-0-bde-stufe-1-werkbaenke-ag-fertigmeldung]].
- Schranke 2 mit dem Bündel: TS-96, Manuelle Test-Checkliste in der Spec (14 Punkte).
