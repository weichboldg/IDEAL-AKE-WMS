namespace IDEALAKEWMSService.Services;

public interface IHolidaySyncService
{
    Task<HolidaySyncResult> RunAsync(CancellationToken ct);
}

public record HolidaySyncResult(int FetchedCount, int InsertedCount, List<string> Errors);

public record NagerHoliday(
    string Date,        // "YYYY-MM-DD"
    string LocalName,
    string Name,
    string CountryCode,
    bool Fixed,
    bool Global,
    string[]? Counties,
    int? LaunchYear,
    string[] Types
);
