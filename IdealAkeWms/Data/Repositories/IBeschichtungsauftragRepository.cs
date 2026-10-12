using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

/// <summary>Herkunftsschluessel einer Position innerhalb eines HauptFA (die Struktur-Felder von <see cref="FaHierarchyNode"/>;
/// der HauptFA steht am Auftrag). Bewusst ohne Knoten-Id — die wechselt mit jedem Sync.</summary>
public readonly record struct PositionsSchluessel(int? VaterFA, int SubFA, int? Position)
{
    public static PositionsSchluessel Von(FaHierarchyNode n) => new(n.VaterFA, n.SubFA, n.Position);
    public static PositionsSchluessel Von(BeschichtungsauftragPosition p) => new(p.VaterFA, p.SubFA, p.Position);
    public override string ToString() => $"Vater {VaterFA?.ToString() ?? "—"} / SubFA {SubFA} / Pos {Position?.ToString() ?? "—"}";
}

/// <summary>Eine Position steckt in einem sperrenden Auftrag (Status ≠ Storniert).</summary>
public sealed record Vergabe(PositionsSchluessel Schluessel, int AuftragId, string Nummer);

/// <summary>
/// Datenzugriff fuer den gespeicherten Beschichtungsauftrag (ADR 0001; Spec 2026-10-06-beschichtungsauftrag-ausbau-teil-3).
/// Die fachlichen Regeln liegen im <c>BeschichtungsauftragWorkflow</c>; hier nur Laden, Speichern, Vergabe-Abfrage.
/// </summary>
public interface IBeschichtungsauftragRepository
{
    /// <summary>Auftrag inkl. Positionen, getrackt (fuer Aenderungen). Null = nicht gefunden.</summary>
    Task<Beschichtungsauftrag?> GetMitPositionenAsync(int id);

    /// <summary>Hoechste vergebene laufende Nummer des Jahres (0 = noch keine).</summary>
    Task<int> GetMaxLaufendeNummerAsync(int jahr);

    /// <summary>Alle Positionen des HauptFA in Auftraegen mit Status ≠ Storniert — inkl. Reklamationen
    /// (Spec Anforderung 6, Herleitung 2026-10-12) — mit Auftragsnummer. Eine Abfrage, kein N+1.</summary>
    Task<List<Vergabe>> GetVergabenAsync(int hauptFa);

    void Add(Beschichtungsauftrag auftrag);
    void Remove(BeschichtungsauftragPosition position);

    /// <summary>Verwirft noch nicht gespeicherte neue Auftraege/Positionen aus dem Change-Tracker (Nummern-Konflikt,
    /// neuer Versuch bzw. Abbruch) — sonst wuerde der naechste SaveChanges sie erneut einfuegen.</summary>
    void VerwerfeNeue();

    /// <summary>Optimistische Nebenlaeufigkeit: der Aufrufer meldet die RowVersion, die er gesehen hat.</summary>
    void SetOriginalRowVersion(Beschichtungsauftrag auftrag, byte[] rowVersion);

    Task SaveChangesAsync();

    /// <summary>Fuehrt Pruefung + Schreiben in EINER Serializable-Transaktion aus (SQL Server: Key-Range-Locks gegen zwei
    /// gleichzeitige Vergaben derselben freien Position). Nicht-relationaler Provider (InMemory-Tests): direkt.</summary>
    Task<T> InSerializableTransactionAsync<T>(Func<Task<T>> action);
}
