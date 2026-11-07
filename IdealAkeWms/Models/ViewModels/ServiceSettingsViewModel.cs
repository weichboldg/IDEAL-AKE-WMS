using System.Collections.Generic;
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// ViewModel fuer /ServiceSettings: Katalog mit DB-Stand gruppiert (seit v1.77.0 dieselben <see cref="SettingGroup"/>/<see cref="SettingItem"/>
/// wie /Settings, Spec 2026-11-07-einstellungen-auswahlfelder-und-beschreibung E3), plus Orphan-Zeilen (DB-Keys ohne Katalog-Eintrag).
/// </summary>
public class ServiceSettingsViewModel
{
    public List<SettingGroup> Groups { get; set; } = new();
    public List<ServiceSetting> OrphanEntries { get; set; } = new();
}
