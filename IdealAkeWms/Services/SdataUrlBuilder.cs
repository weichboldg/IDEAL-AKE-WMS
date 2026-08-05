namespace IdealAkeWms.Services;

/// <summary>
/// Reine, geteilte SData-URL-Zusammensetzung (Web-Test-Button und Service-Client nutzen denselben
/// Builder, damit URL + Kodierung identisch sind).
/// <para>
/// <b>Kodierung (wichtig):</b> KEIN <see cref="Uri.EscapeDataString"/> auf Dataset/Resource. Das
/// Semikolon im Dataset (z. B. <c>ake_TEST2026;1</c>) und das <c>$</c> in Resourcen (<c>$service</c>,
/// <c>$schema</c>) sind laut RFC 3986 sub-delims und muessen <b>literal</b> bleiben (<c>;</c> nicht
/// <c>%3B</c>, <c>$</c> nicht <c>%24</c>). Zusammensetzung per String-Interpolation; die .NET-<see
/// cref="Uri"/>-Pipeline laesst diese sub-delims im Pfad unangetastet.
/// </para>
/// </summary>
public static class SdataUrlBuilder
{
    /// <summary>Feste SData-Wurzel im Pfad (nach dem Host, vor der Application).</summary>
    public const string SdataRoot = "sdata";

    /// <summary>Feste Ziel-Resource der Lagerbuchung. Beginnt mit '$' — MUSS literal bleiben.</summary>
    public const string LagerbuchungResource = "$service/LagerbuchungService";

    /// <summary>
    /// Baut <c>{BaseUrl}/sdata/{Application}/{ServiceContract}/{Dataset}/{relativeResource}</c>.
    /// Toleriert eine BaseUrl, die die <c>/sdata</c>-Wurzel bereits enthaelt (keine Verdoppelung).
    /// </summary>
    public static string BuildResourceUrl(
        string? baseUrl, string? application, string? serviceContract, string? dataset, string relativeResource)
    {
        var b = (baseUrl ?? string.Empty).TrimEnd('/');
        if (b.EndsWith("/" + SdataRoot, StringComparison.OrdinalIgnoreCase))
            b = b[..^(SdataRoot.Length + 1)].TrimEnd('/');

        var app = (application ?? string.Empty).Trim('/');
        var contract = (serviceContract ?? string.Empty).Trim('/');
        var ds = (dataset ?? string.Empty).Trim('/');
        var res = (relativeResource ?? string.Empty).TrimStart('/');

        return $"{b}/{SdataRoot}/{app}/{contract}/{ds}/{res}";
    }

    /// <summary>Ziel-URL fuer die Lagerbuchung (POST <c>$service/LagerbuchungService</c>).</summary>
    public static string BuildLagerbuchungUrl(
        string? baseUrl, string? application, string? serviceContract, string? dataset)
        => BuildResourceUrl(baseUrl, application, serviceContract, dataset, LagerbuchungResource);
}
