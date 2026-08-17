namespace IdealAkeWms.Models.ViewModels;

public sealed class HierarchieUmstellungViewModel
{
    public bool MasterEnabled { get; init; }
    /// <summary>Gecachter Anzeige-Zustand: hierarchische Daten liegen vor -> Master gesperrt.</summary>
    public bool IsLocked { get; init; }
    public DateTimeOffset? StatusRefreshedUtc { get; init; }
}
