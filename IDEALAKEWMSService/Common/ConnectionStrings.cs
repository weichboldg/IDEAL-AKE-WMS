using Microsoft.Extensions.Configuration;

namespace IDEALAKEWMSService.Common;

/// <summary>
/// Centralised connection-string lookup helpers.
/// Throws InvalidOperationException with a consistent message if a connection
/// string is missing from configuration.
/// </summary>
public static class ConnectionStrings
{
    public static string GetRequired(IConfiguration config, string name)
        => config.GetConnectionString(name)
            ?? throw new InvalidOperationException($"{name} nicht konfiguriert.");

    public static string Wms(IConfiguration c) => GetRequired(c, "DefaultConnection");
    public static string Sage(IConfiguration c) => GetRequired(c, "SageConnection");
    public static string Oseon(IConfiguration c) => GetRequired(c, "OseonConnection");
    public static string EnaioDms(IConfiguration c) => GetRequired(c, "EnaioDmsConnection");
}
