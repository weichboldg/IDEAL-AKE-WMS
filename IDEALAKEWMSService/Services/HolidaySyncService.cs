using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace IDEALAKEWMSService.Services;

public class HolidaySyncOptions
{
    [ConfigurationKeyName("FeiertagSyncEnabled")]
    public bool Enabled { get; set; }

    [ConfigurationKeyName("FeiertagCountryCode")]
    public string CountryCode { get; set; } = "AT";

    [ConfigurationKeyName("FeiertagRegion")]
    public string Region { get; set; } = "";

    [ConfigurationKeyName("FeiertagJahreVoraus")]
    public int JahreVoraus { get; set; } = 2;

    public bool DryRun { get; set; }
}

public class HolidaySyncService : IHolidaySyncService
{
    private readonly ApplicationDbContext _ctx;
    private readonly HttpClient _http;
    private readonly IOptions<HolidaySyncOptions> _options;
    private readonly ISyncLogger _syncLogger;
    private readonly ILogger<HolidaySyncService> _logger;

    public HolidaySyncService(ApplicationDbContext ctx, HttpClient http,
        IOptions<HolidaySyncOptions> options, ILogger<HolidaySyncService> logger, ISyncLogger syncLogger)
    {
        _ctx = ctx;
        _http = http;
        _options = options;
        _syncLogger = syncLogger;
        _logger = logger;
    }

    public async Task<HolidaySyncResult> RunAsync(CancellationToken ct)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.Holiday, ct);
        var errors = new List<string>();
        var fetched = 0;
        var inserted = 0;

        try
        {
            var opts = _options.Value;
            if (!opts.Enabled)
            {
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["importiert"] = 0,
                    ["geholt"] = 0,
                }, ct: ct);
                return new HolidaySyncResult(0, 0, new());
            }

            var startYear = DateTime.Today.Year;
            for (int year = startYear; year <= startYear + opts.JahreVoraus; year++)
            {
                try
                {
                    var url = $"api/v3/PublicHolidays/{year}/{opts.CountryCode}";
                    var holidays = await _http.GetFromJsonAsync<List<NagerHoliday>>(url, ct);
                    if (holidays == null) continue;

                    var filtered = string.IsNullOrWhiteSpace(opts.Region)
                        ? holidays.Where(h => h.Counties == null || h.Counties.Length == 0)
                        : holidays.Where(h => h.Counties == null || h.Counties.Length == 0 || h.Counties.Contains(opts.Region));

                    foreach (var h in filtered)
                    {
                        fetched++;
                        if (!DateTime.TryParseExact(h.Date, "yyyy-MM-dd",
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out var date)) continue;

                        if (await _ctx.Holidays.AnyAsync(existing => existing.Date == date.Date, ct))
                            continue; // additive only

                        if (opts.DryRun) continue;

                        _ctx.Holidays.Add(new Holiday
                        {
                            Date = date.Date,
                            Description = h.LocalName,
                            Source = HolidaySource.NagerSync,
                            CreatedAt = DateTime.Now,
                            CreatedBy = "HolidaySync",
                            CreatedByWindows = "HolidaySync"
                        });
                        inserted++;
                    }

                    if (!opts.DryRun)
                        await _ctx.SaveChangesAsync(ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "HolidaySync failed for year {Year}", year);
                    var errorMsg = $"Year {year}: {ex.GetType().Name} - {ex.Message}";
                    errors.Add(errorMsg);
                    await run.LogWarningAsync(errorMsg, reference: year.ToString(), ct: ct);
                    _ctx.ChangeTracker.Clear();
                }
            }

            _logger.LogInformation("HolidaySync: fetched={Fetched} inserted={Inserted} errors={Errors}",
                fetched, inserted, errors.Count);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["importiert"] = inserted,
                ["geholt"] = fetched,
            }, ct: ct);

            return new HolidaySyncResult(fetched, inserted, errors);
        }
        catch (Exception ex)
        {
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, counts: new Dictionary<string, int>
            {
                ["importiert"] = inserted,
                ["geholt"] = fetched,
            }, ct: ct);
            throw;
        }
    }
}
