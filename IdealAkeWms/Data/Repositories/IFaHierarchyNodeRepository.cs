using System.Collections.Generic;
using System.Threading.Tasks;
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// Lesezugriff auf die lokal importierte <see cref="FaHierarchyNode"/>-Tabelle (Struktur +
/// Stueckliste je IDEAL-Fertigungsauftrag). Befuellt wird sie ausschliesslich vom
/// FaHierarchySyncService (Full-Refresh) — hier nur Reads.
/// </summary>
public interface IFaHierarchyNodeRepository
{
    /// <summary>Alle Positionszeilen einer Struktur (HauptFA), hierarchisch sortiert
    /// (VaterFA, Position).</summary>
    Task<List<FaHierarchyNode>> GetByHauptFaAsync(int hauptFa);

    /// <summary>Alle Knoten (ungefiltert). Fuer spaetere Listen-/Baum-Features.</summary>
    Task<List<FaHierarchyNode>> GetAllAsync();

    /// <summary>v1.99.82: Strukturwurzeln (<c>SubFA == HauptFA</c>) zu einer Menge HauptFA - EINE Abfrage je Seite.
    /// Nicht verschwundene Wurzeln zuerst; mehr als eine je HauptFA meldet der Aufrufer.</summary>
    Task<List<FaHierarchyNode>> GetRootsByHauptFaKeysAsync(IReadOnlyCollection<int> hauptFaKeys);
}
