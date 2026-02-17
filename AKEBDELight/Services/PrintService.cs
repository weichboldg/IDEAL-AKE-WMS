using System.Diagnostics;

namespace AKEBDELight.Services;

public interface IPrintService
{
    Task<bool> PrintFileAsync(string printerPath, string filePath);
}

public class PrintService : IPrintService
{
    private readonly ILogger<PrintService> _logger;

    public PrintService(ILogger<PrintService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> PrintFileAsync(string printerPath, string filePath)
    {
        try
        {
            _logger.LogInformation("Druckauftrag: {File} an {Printer}", filePath, printerPath);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    Arguments = $"mshtml.dll,PrintHTML \"{filePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                _logger.LogError("Druckfehler: {Error}", error);
                return false;
            }

            _logger.LogInformation("Druckauftrag erfolgreich gesendet.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Drucken an {Printer}", printerPath);
            return false;
        }
    }
}
