namespace IdealAkeWms.Services;

public interface IHolidayImportService
{
    Task<int> ImportHolidaysAsync(int year, string createdBy, string createdByWindows);
}
