namespace IdealAkeWms.Models.ViewModels;

public class SettingsViewModel
{
    /// <summary>Katalog-Felder (AppSettingDefinitions) mit DB-Stand, in Katalog-Reihenfolge gruppiert (v1.77.0, E2).</summary>
    public List<SettingGroup> Groups { get; set; } = new();
    /// <summary>DB-Zeilen ohne Katalog-Eintrag — read-only unter „Nicht im Katalog“.</summary>
    public List<AppSetting> Orphans { get; set; } = new();
    public List<Holiday> Holidays { get; set; } = new();
    public string? SuccessMessage { get; set; }
}
