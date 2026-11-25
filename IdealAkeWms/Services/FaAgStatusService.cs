using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Services;

/// <summary>Ergebnis eines Status-Laufs samt der Lookups, die ein Folgeschritt (Teilmeldungen der Teileliste) fuer den Soll-Termin braucht.</summary>
public sealed record FaAgStatusLauf(FaAgStatusErgebnis Ergebnis, IReadOnlyDictionary<string, int> Offsets, Func<DateTime, int, DateTime> Rechner,
    IReadOnlyList<ProductionWorkplace> Werkbaenke);

/// <summary>
/// v1.99.73 (Spec 2026-11-25-bde-masken-vereinheitlichung, E1-1): die Status-Ladung der Arbeitsgang-Symbole an EINEM Ort – vorher im
/// <c>FaHierarchyController</c>, jetzt auch von der BDE-Teileliste genutzt. Namen immer (Teil 1), Status nur bei BDE global an (Teil 2).
/// Kein Interface (eine Implementierung).
/// </summary>
public class FaAgStatusService
{
    private readonly IAppSettingRepository _settings;
    private readonly IWorkOperationRepository _workOperations;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly IHolidayRepository _holidays;
    private readonly IOperationConfigRepository _operationConfigs;
    private readonly IBusinessDayService _businessDays;

    public FaAgStatusService(IAppSettingRepository settings, IWorkOperationRepository workOperations, IProductionWorkplaceRepository workplaces,
        IHolidayRepository holidays, IOperationConfigRepository operationConfigs, IBusinessDayService businessDays)
    {
        _settings = settings; _workOperations = workOperations; _workplaces = workplaces; _holidays = holidays;
        _operationConfigs = operationConfigs; _businessDays = businessDays;
    }

    /// <summary>Setzt <see cref="FaHierarchyTreeNode.Arbeitsgaenge"/> (Namen) und – nur bei BDE global an und mindestens einer Struktur –
    /// deren Status. <c>null</c> = kein Status-Lauf (Symbole neutral wie Teil 1).</summary>
    public async Task<FaAgStatusLauf?> ZuordnenAsync(IReadOnlyList<FaHierarchyStructure> structures)
    {
        var workplaces = await _workplaces.GetAllOrderedAsync();
        FaArbeitsschrittSymbole.Zuordnen(structures, FaArbeitsschrittSymbole.NamenLookup(workplaces));

        var bdeAktiv = (await _settings.GetValueAsync(AppSettingKeys.BdeAktiv))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        if (!bdeAktiv || structures.Count == 0) return null;

        var ops = await _workOperations.GetForFaStatusAsync(structures.Select(s => s.HauptFA.ToString()).Distinct().ToList());
        // v1.63.0 (FA-a7): BDE-aktive plus Werkbaenke, deren Kuerzel an einer BDE-aktiven, aktuellen Werkbank steht.
        var aktiveWerkbaenke = WerkbankKuerzelFamilie.BdeRelevanteIds(workplaces);
        var bde = await _workOperations.GetBdeStatusAsync(ops
            .Where(o => o.ProductionWorkplaceId is int w && aktiveWerkbaenke.Contains(w)).Select(o => o.Id).ToList());
        var holidays = await _holidays.GetHolidayDatesAsync();
        var offsets = await _operationConfigs.GetDueDateOffsetsAsync();
        Func<DateTime, int, DateTime> rechner = (d, o) => OseonDueDateCalculator.Calculate(d, o, _businessDays, holidays);

        var ergebnis = FaArbeitsgangStatus.Zuordnen(structures, ops, aktiveWerkbaenke, bde, offsets, rechner, DateTime.Today);
        return new FaAgStatusLauf(ergebnis, offsets, rechner, workplaces);
    }
}
