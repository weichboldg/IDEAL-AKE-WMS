using IdealAkeWms.Data.Repositories;
using Microsoft.AspNetCore.DataProtection;

namespace IdealAkeWms.Services.SageSdata;

/// <summary>Aufgeloeste SData-Zugangsdaten. <see cref="Fehler"/> gesetzt = nicht an Sage senden (kein Rueckfall).</summary>
/// <param name="Quelle">„Service-Einstellungen“ | „appsettings“ | „keine“.</param>
/// <param name="Warnung">Rueckfall auf appsettings: einmal je Lauf protokollieren (ohne Werte).</param>
public sealed record SdataZugang(string Benutzer, string Kennwort, string Quelle, string? Fehler, string? Warnung)
{
    // Kennwort nie in Logs/Diagnose (Record-ToString wuerde es ausgeben).
    public override string ToString() => $"SdataZugang {{ Benutzer = {Benutzer}, Quelle = {Quelle}, Fehler = {Fehler} }}";
}

/// <summary>Stand der SData-Konfiguration fuer eine Web-Seite (Register Sage-Test, FA-Struktur live, Verbindungstest).</summary>
public sealed record SdataWebStand(string? BaseUrl, string Application, string ServiceContract, string? Dataset, bool ZertifikatPruefen,
    SdataZugang Zugang, string KennwortStatus, IReadOnlyList<string> NichtKonfiguriert)
{
    /// <summary>Ziel nur, wenn alles konfiguriert ist und das Kennwort aufloesbar war.</summary>
    public SdataLeseZiel? Ziel => NichtKonfiguriert.Count > 0 ? null
        : new SdataLeseZiel(BaseUrl!, Application, Dataset!, Zugang.Benutzer, Zugang.Kennwort, ZertifikatPruefen);
}

/// <summary>
/// Spec 2026-11-13-sdata-teil-2-userfelder-stueckliste F6 (ADR „SData-Kennwort verschluesselt in den Service-Einstellungen“): EINE Stelle fuer
/// Schuetzen und Aufloesen der SData-Zugangsdaten, genutzt von Web (Verbindungstest, Register Sage-Test, FA-Struktur live) und Dienst
/// (Sage-Buchungs-Worker, alle SData-Abgleiche ueber <c>SdataLeseZielLeser</c>).
/// <list type="number">
/// <item><c>SData:Username</c> gesetzt → DB gewinnt; Kennwort aus <c>SData:Password</c> (<c>dp1:</c> + DataProtection-Schutztext). Nicht
/// entschluesselbar (fremder Schluesselring, Klartext per SQL) → <see cref="SdataZugang.Fehler"/>, KEIN Rueckfall.</item>
/// <item><c>SData:Username</c> leer → Rueckfall appsettings <c>SageLagerbuchung:Username/Password</c> mit Warnung (Freigabe-Antwort 1).</item>
/// </list>
/// </summary>
public static class SdataZugangsdaten
{
    public const string BenutzerKey = "SData:Username";
    public const string KennwortKey = "SData:Password";
    public const string AppsettingsBenutzer = "SageLagerbuchung:Username";
    public const string AppsettingsKennwort = "SageLagerbuchung:Password";
    public const string Purpose = "IdealAkeWms.ServiceSettings.Geheim.v1";
    public const string Praefix = "dp1:";
    public const string ApplicationName = "IdealAkeWms";
    /// <summary>Laengstes Kennwort: der Schutztext passt dann sicher in ServiceSettings.Value (nvarchar(500)), Test belegt es.</summary>
    public const int MaxLaenge = 128;

    public const string FehlerNichtEntschluesselbar = "SData-Kennwort nicht entschlüsselbar – in den Service-Einstellungen neu eingeben";
    public const string WarnungAppsettings = "Zugangsdaten aus appsettings – bitte in die Service-Einstellungen übernehmen";

    public const string StatusGesetzt = "gesetzt";
    public const string StatusNichtGesetzt = "nicht gesetzt";
    public const string StatusNichtEntschluesselbar = "nicht entschlüsselbar – neu eingeben";
    public const string StatusAppsettings = "aus appsettings";

    public static string Schuetzen(string klartext, IDataProtectionProvider provider) =>
        Praefix + provider.CreateProtector(Purpose).Protect(klartext);

    /// <summary>Klartext oder null (leer/ohne Praefix/fremder Schluessel/kein Schluesselring).</summary>
    public static string? Entschluesseln(string? gespeichert, IDataProtectionProvider? provider)
    {
        if (provider is null || string.IsNullOrEmpty(gespeichert) || !gespeichert.StartsWith(Praefix, StringComparison.Ordinal)) return null;
        try { return provider.CreateProtector(Purpose).Unprotect(gespeichert[Praefix.Length..]); }
        catch (Exception) { return null; } // CryptographicException (fremder Schluessel, kein Schluessel), FormatException
    }

    /// <summary>Status des gespeicherten Werts fuer die Oberflaeche (nie der Wert).</summary>
    public static string Status(string? gespeichert, IDataProtectionProvider? provider) =>
        string.IsNullOrEmpty(gespeichert) ? StatusNichtGesetzt
        : Entschluesseln(gespeichert, provider) is null ? StatusNichtEntschluesselbar : StatusGesetzt;

    public static SdataZugang Aufloesen(string? dbBenutzer, string? dbKennwort, IConfiguration configuration, IDataProtectionProvider? provider)
    {
        if (!string.IsNullOrWhiteSpace(dbBenutzer))
        {
            var benutzer = dbBenutzer.Trim();
            if (string.IsNullOrEmpty(dbKennwort))
                return new(benutzer, "", "Service-Einstellungen", null, null); // Benutzer ohne Kennwort: Sage antwortet 401 (sichtbar)
            var klartext = Entschluesseln(dbKennwort, provider);
            return klartext is null
                ? new(benutzer, "", "Service-Einstellungen", FehlerNichtEntschluesselbar, null)
                : new(benutzer, klartext, "Service-Einstellungen", null, null);
        }
        var appBenutzer = configuration[AppsettingsBenutzer];
        if (string.IsNullOrWhiteSpace(appBenutzer))
            return new("", "", "keine", null, null);
        return new(appBenutzer, configuration[AppsettingsKennwort] ?? "", "appsettings", null, WarnungAppsettings);
    }

    /// <summary>Kennwort-Status fuer das Register (inkl. „aus appsettings“ beim Rueckfall).</summary>
    public static string KennwortStatus(SdataZugang zugang, string? dbKennwort, IDataProtectionProvider? provider) =>
        zugang.Quelle == "appsettings" ? StatusAppsettings : Status(dbKennwort, provider);

    /// <summary>Web: Endpunkt (ServiceSettings) + Zugang an einer Stelle; fehlende Teile in <see cref="SdataWebStand.NichtKonfiguriert"/>.</summary>
    public static async Task<SdataWebStand> WebStandAsync(IServiceSettingRepository settings, IConfiguration configuration, IDataProtectionProvider provider)
    {
        var baseUrl = (await settings.GetValueAsync("SData:BaseUrl"))?.Trim();
        var application = (await settings.GetValueAsync("SData:Application"))?.Trim();
        var contract = (await settings.GetValueAsync("SData:ServiceContract"))?.Trim();
        var dataset = (await settings.GetValueAsync("SData:Dataset"))?.Trim();
        var pruefen = SageTlsPolicy.ShouldVerifyCertificate(await settings.GetValueAsync(SageTlsPolicy.SettingKey));
        var dbKennwort = await settings.GetValueAsync(KennwortKey);
        var zugang = Aufloesen(await settings.GetValueAsync(BenutzerKey), dbKennwort, configuration, provider);

        var fehlt = new List<string>();
        if (string.IsNullOrEmpty(baseUrl)) fehlt.Add("SData:BaseUrl");
        else if (baseUrl.Contains('@')) fehlt.Add("SData:BaseUrl enthält Zugangsdaten (die gehören nicht in die URL)");
        if (string.IsNullOrEmpty(dataset)) fehlt.Add("SData:Dataset");
        if (zugang.Fehler is { } f) fehlt.Add(f);
        else if (string.IsNullOrWhiteSpace(zugang.Benutzer)) fehlt.Add(BenutzerKey);
        return new SdataWebStand(baseUrl, string.IsNullOrEmpty(application) ? "ol" : application,
            string.IsNullOrEmpty(contract) ? "CommonWawiServices" : contract, dataset, pruefen, zugang,
            KennwortStatus(zugang, dbKennwort, provider), fehlt);
    }

    // ===== Schluesselring (eine Stelle fuer Web und Dienst) =====

    /// <summary>Ordner des DataProtection-Schluesselrings: <c>DataProtection:KeysPath</c>, sonst <c>%ProgramData%\IdealAkeWms\DataProtection-Keys</c>.
    /// Der Dienst braucht in seiner appsettings.json denselben Wert wie die Web-App.</summary>
    public static string SchluesselPfad(IConfiguration configuration)
    {
        var pfad = configuration["DataProtection:KeysPath"];
        return string.IsNullOrWhiteSpace(pfad)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "IdealAkeWms", "DataProtection-Keys")
            : pfad;
    }

    private static readonly object Sperre = new();
    private static IDataProtectionProvider? _dienstProvider;

    /// <summary>Dienst: einmal erzeugter Provider auf dem Schluesselring der Web-App, nur lesend (legt nie Schluessel an).
    /// Fehlt der Ordner oder das Leserecht, ist jedes Kennwort „nicht entschluesselbar“ (Fehler, kein Rueckfall).</summary>
    public static IDataProtectionProvider DienstProvider(IConfiguration configuration)
    {
        lock (Sperre)
            return _dienstProvider ??= DataProtectionProvider.Create(new DirectoryInfo(SchluesselPfad(configuration)),
                b => b.SetApplicationName(ApplicationName).DisableAutomaticKeyGeneration());
    }
}
