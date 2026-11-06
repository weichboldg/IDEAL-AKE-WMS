using IdealAkeWms.Services.SageSdata;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>DI-Registrierung des SData-Lese-Clients im Dienst (aus Program.cs gerufen und vom DI-Aufloesungstest).</summary>
public static class SdataRegistrierung
{
    /// <summary>
    /// Typisierter <see cref="HttpClient"/> fuer <see cref="ISageSdataReadClient"/>, Timeout = <see cref="SageSdataReadClient.SeitenTimeout"/>.
    /// TLS wie in der Web-App: der Callback liest den Schalter <c>SageLagerbuchungSslZertifikatPruefen</c> aus der Anfrage
    /// (vom <see cref="SdataLeseZielLeser"/> je Lauf aus ServiceSettings gesetzt) — kein DB-Zugriff im Handshake, wirkt nur auf diesen Client.
    /// </summary>
    public static IServiceCollection AddSageSdataReadClient(this IServiceCollection services)
    {
        services.AddHttpClient<ISageSdataReadClient, SageSdataReadClient>(c => c.Timeout = SageSdataReadClient.SeitenTimeout)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
                    SageSdataReadClient.ZertifikatAkzeptieren(request, errors)
            });
        return services;
    }

    /// <summary>
    /// E1: <see cref="ISageLagerplatzReader"/> = <see cref="LagerplatzQuelleSwitch"/>; der SQL-Reader und der SData-Reader als
    /// konkrete Typen (die Weiche nimmt beide ueber das Interface, daher Factory statt Ctor-Aufloesung).
    /// </summary>
    public static IServiceCollection AddLagerplatzQuelle(this IServiceCollection services)
    {
        services.AddScoped<SageLagerplatzReader>();
        services.AddScoped<SdataLagerplatzReader>();
        services.AddScoped<ISageLagerplatzReader>(sp => new LagerplatzQuelleSwitch(
            sp.GetRequiredService<SageLagerplatzReader>(),
            sp.GetRequiredService<SdataLagerplatzReader>(),
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<ISyncErrorNotifier>(),
            sp.GetRequiredService<IdealAkeWms.Services.SyncLogger.ISyncLogger>()));
        return services;
    }

    /// <summary>E2: <see cref="ISageBestandReader"/> = <see cref="BestandQuelleSwitch"/> (Muster wie <see cref="AddLagerplatzQuelle"/>).</summary>
    public static IServiceCollection AddBestandQuelle(this IServiceCollection services)
    {
        services.AddScoped<SageBestandReader>();
        services.AddScoped<SdataBestandReader>();
        services.AddScoped<ISageBestandReader>(sp => new BestandQuelleSwitch(
            sp.GetRequiredService<SageBestandReader>(),
            sp.GetRequiredService<SdataBestandReader>(),
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<ISyncErrorNotifier>(),
            sp.GetRequiredService<IdealAkeWms.Services.SyncLogger.ISyncLogger>()));
        return services;
    }

    /// <summary>E3a: <see cref="ISageArtikelReader"/> = <see cref="ArtikelQuelleSwitch"/> (Muster wie <see cref="AddLagerplatzQuelle"/>).</summary>
    public static IServiceCollection AddArtikelQuelle(this IServiceCollection services)
    {
        services.AddScoped<SageArtikelReader>();
        services.AddScoped<SdataArtikelReader>();
        services.AddScoped<ISageArtikelReader>(sp => new ArtikelQuelleSwitch(
            sp.GetRequiredService<SageArtikelReader>(),
            sp.GetRequiredService<SdataArtikelReader>(),
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<ISyncErrorNotifier>(),
            sp.GetRequiredService<IdealAkeWms.Services.SyncLogger.ISyncLogger>()));
        return services;
    }

    /// <summary>E6a: <see cref="ISageArbeitsplatzReader"/> = <see cref="ArbeitsplatzQuelleSwitch"/> (Werkbank-Anlage + Arbeitsgang-Kuerzel,
    /// Muster wie <see cref="AddLagerplatzQuelle"/>).</summary>
    public static IServiceCollection AddArbeitsplatzQuelle(this IServiceCollection services)
    {
        services.AddScoped<SageArbeitsplatzReader>();
        services.AddScoped<SdataArbeitsplatzReader>();
        services.AddScoped<ISageArbeitsplatzReader>(sp => new ArbeitsplatzQuelleSwitch(
            sp.GetRequiredService<SageArbeitsplatzReader>(),
            sp.GetRequiredService<SdataArbeitsplatzReader>(),
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<ISyncErrorNotifier>(),
            sp.GetRequiredService<IdealAkeWms.Services.SyncLogger.ISyncLogger>()));
        return services;
    }

    /// <summary>E4a: <see cref="ISageFaStrukturReader"/> = <see cref="FaStrukturQuelleSwitch"/> (Muster wie <see cref="AddLagerplatzQuelle"/>).</summary>
    public static IServiceCollection AddFaStrukturQuelle(this IServiceCollection services)
    {
        services.AddScoped<SageFaStrukturReader>();
        services.AddScoped<SdataFaStrukturReader>();
        services.AddScoped<ISageFaStrukturReader>(sp => new FaStrukturQuelleSwitch(
            sp.GetRequiredService<SageFaStrukturReader>(),
            sp.GetRequiredService<SdataFaStrukturReader>(),
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<ISyncErrorNotifier>(),
            sp.GetRequiredService<IdealAkeWms.Services.SyncLogger.ISyncLogger>()));
        return services;
    }
}
