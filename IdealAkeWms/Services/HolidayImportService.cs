using System.Text.Json;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

public class HolidayImportService : IHolidayImportService
{
    private readonly HttpClient _httpClient;
    private readonly IHolidayRepository _holidayRepository;

    public HolidayImportService(HttpClient httpClient, IHolidayRepository holidayRepository)
    {
        _httpClient = httpClient;
        _holidayRepository = holidayRepository;
    }

    public async Task<int> ImportHolidaysAsync(int year, string createdBy, string createdByWindows)
    {
        var url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/AT";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var holidays = JsonSerializer.Deserialize<List<NagerHoliday>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (holidays == null || holidays.Count == 0)
            return 0;

        var existingDates = await _holidayRepository.GetHolidayDatesAsync();
        var now = DateTime.Now;
        var imported = 0;

        foreach (var h in holidays)
        {
            if (existingDates.Contains(h.Date.Date))
                continue;

            var holiday = new Holiday
            {
                Date = h.Date.Date,
                Description = h.LocalName,
                CreatedAt = now,
                CreatedBy = createdBy,
                CreatedByWindows = createdByWindows
            };

            await _holidayRepository.AddAsync(holiday);
            imported++;
        }

        return imported;
    }

    private class NagerHoliday
    {
        public DateTime Date { get; set; }
        public string LocalName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
    }
}
