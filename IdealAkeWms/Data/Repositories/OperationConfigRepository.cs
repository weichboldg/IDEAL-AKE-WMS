using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

/// <summary>Kuerzel ohne Arbeitsgang-Stammdaten-Eintrag mit seiner Herkunft („OSEON", „WMS-Arbeitsgang"; beide moeglich).</summary>
public sealed record UnconfiguredOperation(string Kuerzel, IReadOnlyList<string> Quellen);

public interface IOperationConfigRepository
{
    Task<List<OperationConfig>> GetAllAsync();
    Task<OperationConfig?> GetByIdAsync(int id);
    Task<OperationConfig?> GetByNameAsync(string operationName);
    /// <summary>OSEON-Teileverfolgung: case-SENSITIVES Dictionary (Default-Comparer) — bewusst unveraendert (FA 7).</summary>
    Task<Dictionary<string, OperationConfig>> GetAllAsDictionaryAsync();

    /// <summary>
    /// Offset-Lookup fuer FA-Struktur Teil 2/3 (Spec 2026-10-09 FA 6): alle Zeilen (auch nicht fremdsystem-relevante),
    /// Schluessel = <c>OperationName.Trim()</c>, Wert = <c>DueDateOffsetDays</c>, Comparer <c>OrdinalIgnoreCase</c>. Fehlender
    /// Schluessel = kein Eintrag (Aufrufer rechnet Offset 0), unterscheidbar von Wert 0. Doppelter normalisierter Schluessel:
    /// die Zeile mit der kleinsten Id gewinnt (die Seite meldet Dubletten). Aufrufer trimmen ihren Suchschluessel selbst.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> GetDueDateOffsetsAsync();

    /// <summary>„Nicht konfigurierte Kuerzel" (FA 5): OSEON-AG-Namen und WMS-<c>WorkOperation.OperationNumber</c> ohne Eintrag,
    /// Abgleich getrimmt + case-insensitiv wie <see cref="GetDueDateOffsetsAsync"/>; der BDE-Default-AG „01" nie.</summary>
    Task<List<UnconfiguredOperation>> GetUnconfiguredOperationsAsync();
    Task AddAsync(OperationConfig config);
    Task UpdateAsync(OperationConfig config);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(string operationName);
}

public class OperationConfigRepository : IOperationConfigRepository
{
    private readonly ApplicationDbContext _context;

    public OperationConfigRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OperationConfig>> GetAllAsync()
    {
        return await _context.OperationConfigs
            .OrderBy(c => c.OperationName)
            .ToListAsync();
    }

    public async Task<OperationConfig?> GetByIdAsync(int id)
    {
        return await _context.OperationConfigs.FindAsync(id);
    }

    public async Task<OperationConfig?> GetByNameAsync(string operationName)
    {
        return await _context.OperationConfigs
            .FirstOrDefaultAsync(c => c.OperationName == operationName);
    }

    public async Task<Dictionary<string, OperationConfig>> GetAllAsDictionaryAsync()
    {
        return await _context.OperationConfigs
            .ToDictionaryAsync(c => c.OperationName, c => c);
    }

    public async Task<IReadOnlyDictionary<string, int>> GetDueDateOffsetsAsync()
    {
        var rows = await _context.OperationConfigs.AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new { c.OperationName, c.DueDateOffsetDays })
            .ToListAsync();
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
            result.TryAdd(r.OperationName.Trim(), r.DueDateOffsetDays); // kleinste Id zuerst → gewinnt
        return result;
    }

    /// <summary>BDE-Default-Arbeitsgang (NurFA-Pfad, BdeDefaultWorkOperationService) — kein fachliches Kuerzel (Antwort 7).</summary>
    public const string DefaultOperationNumber = "01";

    public async Task<List<UnconfiguredOperation>> GetUnconfiguredOperationsAsync()
    {
        var configured = (await _context.OperationConfigs.AsNoTracking().Select(c => c.OperationName).ToListAsync())
            .Select(n => n.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var oseon = await _context.OseonWorkOperations.AsNoTracking().Select(op => op.Name).Distinct().ToListAsync();
        var wms = await _context.WorkOperations.AsNoTracking().Select(op => op.OperationNumber).Distinct().ToListAsync();

        return oseon.Select(k => (Kuerzel: k, Quelle: "OSEON"))
            .Concat(wms.Select(k => (Kuerzel: k, Quelle: "WMS-Arbeitsgang")))
            .Select(x => (Kuerzel: (x.Kuerzel ?? string.Empty).Trim(), x.Quelle))
            .Where(x => x.Kuerzel.Length > 0 && x.Kuerzel != DefaultOperationNumber && !configured.Contains(x.Kuerzel))
            .GroupBy(x => x.Kuerzel, StringComparer.OrdinalIgnoreCase)
            .Select(g => new UnconfiguredOperation(g.First().Kuerzel, g.Select(x => x.Quelle).Distinct().OrderBy(q => q).ToList()))
            .OrderBy(u => u.Kuerzel, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task AddAsync(OperationConfig config)
    {
        _context.OperationConfigs.Add(config);
        await _context.SaveChangesAsync();
    }

    /// <summary>Pflegefelder des Reiters — nur diese schreibt <see cref="UpdateAsync"/>.</summary>
    private static readonly string[] EditableProperties =
    {
        nameof(OperationConfig.DisplayName), nameof(OperationConfig.DueDateOffsetDays), nameof(OperationConfig.IsExternalSystemRelevant),
        nameof(OperationConfig.ModifiedAt), nameof(OperationConfig.ModifiedBy), nameof(OperationConfig.ModifiedByWindows),
    };

    /// <summary>
    /// Schreibt nur die Pflegefelder (kein <c>Update(config)</c>, das alle Spalten mit dem Ladestand zurueckschreibt): Source,
    /// SageMissingSince, OperationName und Created* gehoeren dem Arbeitsgang-Abgleich bzw. der Anlage (Spec 2026-10-16, AK 28).
    /// </summary>
    public async Task UpdateAsync(OperationConfig config)
    {
        var entry = _context.Entry(config);
        if (entry.State == EntityState.Detached)
            _context.OperationConfigs.Attach(config);
        foreach (var property in entry.Properties)
            property.IsModified = EditableProperties.Contains(property.Metadata.Name);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var config = await _context.OperationConfigs.FindAsync(id);
        if (config != null)
        {
            _context.OperationConfigs.Remove(config);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>Existiert das Kuerzel getrimmt und case-insensitiv schon? Vergleich in C# (nicht collation-abhaengig, FA 5).</summary>
    public async Task<bool> ExistsAsync(string operationName)
    {
        var key = (operationName ?? string.Empty).Trim();
        return (await _context.OperationConfigs.AsNoTracking().Select(c => c.OperationName).ToListAsync())
            .Any(n => string.Equals(n.Trim(), key, StringComparison.OrdinalIgnoreCase));
    }
}
