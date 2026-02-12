namespace AKEBDELight.Models.ViewModels;

public class SettingsViewModel
{
    public List<AppSetting> Settings { get; set; } = new();
    public List<Holiday> Holidays { get; set; } = new();
    public string? SuccessMessage { get; set; }
}
