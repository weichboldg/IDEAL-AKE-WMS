using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace IDEALAKEWMSService.Services;

public class HolidaySyncOptions
{
    public bool Enabled { get; set; }
    public string CountryCode { get; set; } = "AT";
    public string Region { get; set; } = "";
    public int JahreVoraus { get; set; } = 2;
    public bool DryRun { get; set; }
}

public class HolidaySyncService : IHolidaySyncService
{
    private readonly ApplicationDbContext _ctx;
    private readonly HttpClient _http;
    private readonly IOptions<HolidaySyncOptions> _options;
    private readonly ILogger<HolidaySyncService> _logger;

    public HolidaySyncService(ApplicationDbContext ctx, HttpClient http,
        IOptions<HolidaySyncOptions> options, ILogger<HolidaySyncService> logger)
    {
        _ctx = ctx;
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<HolidaySyncResult> RunAsync(CancellationToken ct)
    {
        var opts = _options.Value;
        if (!opts.Enabled)
            return new HolidaySyncResult(0, 0, new());

        var errors = new List<string>();
        var fetched = 0;
        var inserted = 0;

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
                errors.Add($"Year {year}: {ex.GetType().Name} - {ex.Message}");
                _ctx.ChangeTracker.Clear();
            }
        }

        _logger.LogInformation("HolidaySync: fetched={Fetched} inserted={Inserted} errors={Errors}",
            fetched, inserted, errors.Count);

        return new HolidaySyncResult(fetched, inserted, errors);
    }
}
