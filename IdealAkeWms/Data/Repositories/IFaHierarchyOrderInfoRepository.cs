using System.Collections.Generic;
using System.Threading.Tasks;
using IdealAkeWms.Models;

namespace IdealAkeWms.Data.Repositories;

/// <summary>
/// Lesezugriff auf die lokal importierte <see cref="FaHierarchyOrderInfo"/>-Tabelle (PPS-/
/// Auftragsdaten je HauptFA, eine Zeile je Montage-Abteilung). Ohne Cache-Decorator (kleine
/// Tabelle). Befuellt wird sie ausschliesslich vom FaHierarchySyncService (Full-Refresh).
/// </summary>
public interface IFaHierarchyOrderInfoRepository
{
    /// <summary>Alle Auftragsdaten-Zeilen einer Struktur (HauptFA) — bei Kombinationsgeraeten
    /// mehrere (eine je Montage-Abteilung).</summary>
    Task<List<FaHierarchyOrderInfo>> GetByHauptFaAsync(int hauptFa);

    /// <summary>Alle Auftragsdaten-Zeilen (ungefiltert).</summary>
    Task<List<FaHierarchyOrderInfo>> GetAllAsync();

    /// <summary>
    /// Alle Auftragskoepfe zu einer Menge von HauptFA-Nummern — EINE Abfrage je angezeigter
    /// Seite (Fan-out-Vermeidung). Liefert bei Kombigeraeten bewusst MEHRERE Zeilen je HauptFA.
    /// </summary>
    Task<List<FaHierarchyOrderInfo>> GetByHauptFaKeysAsync(IReadOnlyCollection<int> hauptFaKeys);
}
