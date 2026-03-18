using IdealAkeWms.Data.Repositories;

namespace IdealAkeWms.Services;

public enum TrafficLightColor { Green, Blue, Yellow, Red, Gray }

public interface IOseonTrafficLightService
{
    Task<TrafficLightColor> GetColorAsync(int oseonStatus, DateTime? dueDate);
    Task<(int gelbTage, int blauTage)> GetThresholdsAsync();
}

public class OseonTrafficLightService : IOseonTrafficLightService
{
    private readonly IAppSettingRepository _settings;

    public OseonTrafficLightService(IAppSettingRepository settings)
    {
        _settings = settings;
    }

    public async Task<TrafficLightColor> GetColorAsync(int oseonStatus, DateTime? dueDate)
    {
        // Fertig oder Storniert → Grün
        if (oseonStatus is 90 or 95)
            return TrafficLightColor.Green;

        // Kein Termin → Grau
        if (!dueDate.HasValue)
            return TrafficLightColor.Gray;

        var (gelbTage, blauTage) = await GetThresholdsAsync();
        var today = DateTime.Today;
        var dueDateValue = dueDate.Value.Date;

        // Überfällig → Rot
        if (dueDateValue < today)
            return TrafficLightColor.Red;

        // Fällig innerhalb GelbTage → Gelb
        if (dueDateValue <= today.AddDays(gelbTage))
            return TrafficLightColor.Yellow;

        // Fällig innerhalb BlauTage → Blau
        if (dueDateValue <= today.AddDays(blauTage))
            return TrafficLightColor.Blue;

        // Noch nicht relevant → Grau
        return TrafficLightColor.Gray;
    }

    public async Task<(int gelbTage, int blauTage)> GetThresholdsAsync()
    {
        var gelbTage = await _settings.GetIntValueAsync("OseonAmpelGelbTage", 1);
        var blauTage = await _settings.GetIntValueAsync("OseonAmpelBlauTage", 2);
        return (gelbTage, blauTage);
    }
}
