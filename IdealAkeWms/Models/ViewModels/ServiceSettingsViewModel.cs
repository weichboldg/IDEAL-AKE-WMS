using System.Collections.Generic;
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>Ein typisiertes Feld auf der ServiceSettings-Seite.</summary>
public class ServiceSettingItem
{
    public string Key { get; set; } = string.Empty;
    public ServiceSettingType Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Multiline { get; set; }
}

/// <summary>Eine UI-Kategorie mit ihren Feldern.</summary>
public class ServiceSettingGroup
{
    public string Category { get; set; } = string.Empty;
    public List<ServiceSettingItem> Items { get; set; } = new();
}

/// <summary>
/// ViewModel fuer /ServiceSettings: Katalog∪DB gemergt und gruppiert, plus
/// Orphan-Zeilen (DB-Keys ohne Katalog-Eintrag) fuer den Freitext-Fallback.
/// </summary>
public class ServiceSettingsViewModel
{
    public List<ServiceSettingGroup> Groups { get; set; } = new();
    public List<ServiceSetting> OrphanEntries { get; set; } = new();
}
