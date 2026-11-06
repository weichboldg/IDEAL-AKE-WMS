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
}
