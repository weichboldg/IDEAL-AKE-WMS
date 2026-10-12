# Beschichtungsauftrag Teil 3 — Beschichtungsauftrag als eigenes Objekt Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Gespeicherter Beschichtungsauftrag (Kopf + Positionen, Nummernkreis, Statusautomat, Reklamation) als Backend-Baustein ohne Oberflaeche.

**Architecture:** Zwei neue `AuditableEntity`-Tabellen, ein Repository (ADR 0001) fuer Laden/Speichern/Vergaben, ein scoped `BeschichtungsauftragWorkflow` mit allen fachlichen Regeln und einem Ergebnisobjekt mit Klartext-Fehlern/Hinweisen. Erstauftrags-Pfade (Anlegen, Hinzufuegen im Erst-Entwurf) laufen auf SQL Server in einer Serializable-Transaktion; die Nummer wird per `MAX+1` vergeben, der UNIQUE-Index entscheidet, der Verlierer wiederholt (3 Versuche). Zeit ueber `TimeProvider` (Jahreswechsel testbar), Cache-Knoten ungecacht (`FaHierarchyNodeRepository`), damit „frisch" wirklich frisch ist.

**Tech Stack:** EF Core 10 (SqlServer + InMemory-Tests), xUnit/FluentAssertions/Moq.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec.md`

## Global Constraints

- Direkt im Buendel, Start `020bf8af`; Version **1.50.0**; SQL **94** (frei in allen lokalen Zweigen, Buendel endet 93, main 88).
- Nummer `BA-JJJJ-NNNNN`, Zaehler je Jahr ab 1, ohne Standortkuerzel, nie wiederverwendet.
- Status `Entwurf=10, Uebergeben=30, Erledigt=40, Storniert=90`; **20 nur als Kommentar** (kein Mitglied, keine Spalten, kein Code).
- Uebergaenge nur Entwurf→Uebergeben→Erledigt, Entwurf→Storniert; Uebergeben nur mit Dienstleister (nicht leer/Whitespace).
- Sperrend fuer Erstauftraege: jeder Auftrag mit Status ≠ Storniert (inkl. Reklamationen). Reklamations-Pfade pruefen keine Vergabe.
- Reklamation nur aus Erledigt, Kopf 1:1 vom Ursprung (ohne Bemerkung), Positionen nur aus dem direkten Ursprung (Snapshot inkl. Menge kopiert).
- Positionen: Herkunftsschluessel (HauptFA, VaterFA, SubFA, Position) + Snapshot (HauptArtnr, Artnr, Matchcode, Menge=Sollmenge, Breite/Hoehe/Tiefe); kein FK auf `FaHierarchyNode`.
- Audit-Felder bei jeder Anlage/Aenderung; abgelehnte Aktion aendert nichts. Klartext-Fehler, kein stiller No-op.
- Migration dreifach (ADR 0004): EF-Migration, `SQL/94_AddBeschichtungsauftrag.sql` (OBJECT_ID/sys.indexes-Guards, DDL je Batch, History separat), `00_FreshInstall.sql` an zwei Stellen. Namen wie EF sie erzeugt.
- Keine Oberflaeche.

## Review Focus

1. Herkunftsschluessel doppelt im Cache (zwei beschichtete Knoten mit gleichem Schluessel) — Anlegen meldet „Schluessel nicht eindeutig" statt still. Test `Anlegen_DoppelterSchluesselImCache_Hinweis`.
2. Menge ≤ 0 oder unbekannte Positions-Id beim Bearbeiten — Ablehnung, nichts geaendert. Test `Bearbeiten_UngueltigeMenge_Abgelehnt`.
3. Bemerkung ueber 1000 Zeichen — Ablehnung statt DB-Fehler. Test `Bemerkung_ZuLang_Abgelehnt`.
4. Unbekannte Auftrags-Id in jeder Operation — Klartext „nicht gefunden". Test `UnbekannteId_Klartext`.
5. Reklamations-Auswahl mit Positions-Id aus einem anderen Auftrag — ganz abgelehnt. Test `Reklamation_FremdePositionsId_Abgelehnt`.

---

### Task 1: Modell, DbContext, Repository, DI

**Files:** Create `Models/Beschichtungsauftrag.cs`, `Models/BeschichtungsauftragPosition.cs`, `Models/BeschichtungsauftragStatus.cs`, `Data/Repositories/IBeschichtungsauftragRepository.cs`, `Data/Repositories/BeschichtungsauftragRepository.cs`; Modify `Data/ApplicationDbContext.cs`, `Program.cs`.

```csharp
public readonly record struct PositionsSchluessel(int? VaterFA, int SubFA, int? Position);
public sealed record Vergabe(PositionsSchluessel Schluessel, int AuftragId, string Nummer);
public interface IBeschichtungsauftragRepository {
  Task<Beschichtungsauftrag?> GetMitPositionenAsync(int id);          // tracked, inkl. Positionen
  Task<int> GetMaxLaufendeNummerAsync(int jahr);
  Task<List<Vergabe>> GetVergabenAsync(int hauptFa);                  // Status != Storniert, eine Abfrage
  void Add(Beschichtungsauftrag auftrag);
  void Remove(BeschichtungsauftragPosition position);
  void Detach(Beschichtungsauftrag auftrag);
  Task SaveChangesAsync();
  Task<T> InSerializableTransactionAsync<T>(Func<Task<T>> action);    // relational: Serializable; InMemory: direkt
  void SetOriginalRowVersion(Beschichtungsauftrag auftrag, byte[] rowVersion);
}
```

### Task 2: Workflow + Tests (rot → gruen)

**Files:** Create `Services/BeschichtungsauftragWorkflow.cs`, `IdealAkeWms.Tests/Services/BeschichtungsauftragWorkflowTests.cs`, `IdealAkeWms.Tests/Repositories/BeschichtungsauftragDiResolutionTests.cs`.

```csharp
public sealed class BeschichtungsauftragErgebnis { bool Erfolg; Beschichtungsauftrag? Auftrag; List<string> Fehler; List<string> Hinweise; }
public sealed record KopfDaten(string? Dienstleister, string? Ral, DateTime? StartBeschichtung, DateTime? BeschichtenRetour);
public sealed record PositionsKandidat(PositionsSchluessel Schluessel, string? HauptArtnr, string? Artnr, string? Matchcode, decimal Menge, decimal Breite, decimal Hoehe, decimal Tiefe);
Task<BeschichtungsauftragErgebnis> AnlegenAsync(int hauptFa);
Task<BeschichtungsauftragErgebnis> EntwurfBearbeitenAsync(int id, KopfDaten kopf, IReadOnlyDictionary<int, decimal> mengen, IReadOnlyCollection<int> entfernteIds, byte[] rowVersion);
Task<List<PositionsKandidat>> GetHinzufuegbarePositionenAsync(int id);
Task<BeschichtungsauftragErgebnis> PositionenHinzufuegenAsync(int id, IReadOnlyCollection<PositionsSchluessel> schluessel, byte[] rowVersion);
Task<BeschichtungsauftragErgebnis> BemerkungAendernAsync(int id, string? text, byte[] rowVersion);
Task<BeschichtungsauftragErgebnis> StatusWechselnAsync(int id, BeschichtungsauftragStatus ziel, byte[] rowVersion);
Task<BeschichtungsauftragErgebnis> ReklamationAnlegenAsync(int ursprungId, IReadOnlyCollection<int> positionIds);
```
- Tests je AK 2–9, 11 + Review Focus 1–5 + Nummernkreis-Retry (Repository-Subklasse wirft einmal `DbUpdateException` nach Einfuegen eines Konkurrenten) + erschoepfte Versuche + Jahreswechsel (fester `TimeProvider`) + DI-Aufloesung.

### Task 3: Migration dreifach

`dotnet ef migrations add AddBeschichtungsauftrag --project IdealAkeWms` (Designer + Snapshot), `SQL/94_AddBeschichtungsauftrag.sql`, `00_FreshInstall.sql` (Abschnitt 17j + MigrationId). Test: Migration enthaelt beide Tabellen (Quelltext), SQL-Skript nennt dieselben Index-/FK-Namen wie die Migration.

### Task 4: Version, Changelog, TS, Brain

1.50.0, Anwender-Changelog (Vorbereitung, nicht sichtbar), TS-83 (LF pruefen), Testindex, Brain-Changelog, datenmodell.md, feature-map, Spec-Nachweis, qa-agent synchron.
