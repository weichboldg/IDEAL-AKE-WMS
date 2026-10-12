using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>Reiter „Arbeitsgänge" der BDE-Stammdaten (Spec 2026-10-09-arbeitsgang-stammdaten-vereinheitlichen).</summary>
public class OperationConfigIndexViewModel
{
    /// <summary>Seite der gefilterten Arbeitsgang-Stammdaten.</summary>
    public List<OperationConfig> Configs { get; set; } = new();

    /// <summary>Kuerzel ohne Eintrag aus OSEON und WMS-Arbeitsgaengen (ohne „01").</summary>
    public List<UnconfiguredOperation> Unconfigured { get; set; } = new();

    /// <summary>Eintraege, die getrimmt und case-insensitiv gleich sind (Altdaten) — Warnhinweis, „melden statt still".</summary>
    public List<string> DuplicateNames { get; set; } = new();

    public PaginationState Pagination { get; set; } = new();
}
