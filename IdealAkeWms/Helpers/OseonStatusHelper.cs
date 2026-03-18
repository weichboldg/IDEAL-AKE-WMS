namespace IdealAkeWms.Helpers;

public static class OseonStatusHelper
{
    public static string GetStatusText(int status) => status switch
    {
        10 => "Unvollständig",
        20 => "Gültig",
        30 => "Freigegeben",
        60 => "In Arbeit",
        70 => "Gesperrt",
        90 => "Fertig",
        95 => "Storniert",
        _ => $"Unbekannt ({status})"
    };

    public static string GetStatusBadgeClass(int status) => status switch
    {
        10 => "bg-secondary",
        20 => "bg-secondary",
        30 => "bg-info text-dark",
        60 => "bg-primary",
        70 => "bg-danger",
        90 => "bg-success",
        95 => "bg-dark",
        _ => "bg-secondary"
    };
}
