using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services;
using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Workers;
using Microsoft.EntityFrameworkCore;
using Serilog;

// Windows Service: Working Directory ist C:\Windows\System32, nicht das EXE-Verzeichnis.
// Alle relativen Pfade (Logs, appsettings.json) würden sonst dort landen.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "IDEAL AKE WMS Service";
    });

    builder.Services.AddSerilog((sp, lc) => lc
        .ReadFrom.Configuration(builder.Configuration));

    // EF Core DbContext (für BDE-AutoPause + HolidaySync)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.CommandTimeout(120)));

    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions => sqlOptions.CommandTimeout(120)));

    // Caching (für CachedSettingRepository)
    builder.Services.AddMemoryCache();

    // Repositories (aus IdealAkeWms-Web-Projekt; werden von BdeAutoPause + ShiftCalendar benötigt)
    builder.Services.AddScoped<AppSettingRepository>();
    builder.Services.AddScoped<IAppSettingRepository, CachedSettingRepository>();
    builder.Services.AddScoped<ISyncLogRepository, SyncLogRepository>();
    builder.Services.AddSingleton<IdealAkeWms.Services.SyncLogger.ISyncLogger,
                                  IdealAkeWms.Services.SyncLogger.SyncLogger>();
    builder.Services.AddSingleton<IDEALAKEWMSService.Common.IHierarchicalModeReader,
                                  IDEALAKEWMSService.Common.HierarchicalModeReader>();
    builder.Services.AddSingleton<IDEALAKEWMSService.Common.IUnknownArbeitsschrittTokenState,
                                  IDEALAKEWMSService.Common.UnknownArbeitsschrittTokenState>();
    builder.Services.AddSingleton<IDEALAKEWMSService.Common.IUnknownArbeitsplatzState,
                                  IDEALAKEWMSService.Common.UnknownArbeitsplatzState>();

    // Services (Scoped für Worker über IServiceScopeFactory)
    builder.Services.AddScoped<ISageImportService, SageImportService>();
    // Artikel: Quellen-Weiche (Sync:Quelle:Artikel, Default View = bisheriger SQL-Lese-Code; SData gesperrt bis P-R1), v1.73.0 E3a.
    IDEALAKEWMSService.Services.Sdata.SdataRegistrierung.AddArtikelQuelle(builder.Services);
    // Lagerplaetze: Quellen-Weiche (Sync:Quelle:Lagerplaetze, Default View = SQL-Reader unveraendert), v1.73.0 E1.
    IDEALAKEWMSService.Services.Sdata.SdataRegistrierung.AddLagerplatzQuelle(builder.Services);
    // Arbeitsplaetze (Werkbank-Anlage + Arbeitsgang-Kuerzel): Quellen-Weiche (Sync:Quelle:Arbeitsplaetze, Default View = SQL-Reader unveraendert), v1.73.0 E6a.
    IDEALAKEWMSService.Services.Sdata.SdataRegistrierung.AddArbeitsplatzQuelle(builder.Services);
    // Lagerbestand: Quellen-Weiche (Sync:Quelle:Lagerbestand, Default View = SQL-Reader unveraendert), v1.73.0 E2.
    IDEALAKEWMSService.Services.Sdata.SdataRegistrierung.AddBestandQuelle(builder.Services);
    builder.Services.AddScoped<ISageZusatzinfoReader, SageZusatzinfoReader>();
    builder.Services.AddScoped<IFaZusatzinfoSyncService, FaZusatzinfoSyncService>();
    builder.Services.AddScoped<IOseonSyncService, OseonSyncService>();
    builder.Services.AddScoped<IBomCacheSyncService, BomCacheSyncService>();
    builder.Services.AddScoped<ILagerplatzSyncService, LagerplatzSyncService>();
    builder.Services.AddScoped<ILagerbestandSyncService, LagerbestandSyncService>();
    // FA-Struktur: Quellen-Weiche (Sync:Quelle:FaStruktur, Default View = SQL-Lese-Code unveraendert; SData gesperrt bis P-F2), v1.73.0 E4a.
    IDEALAKEWMSService.Services.Sdata.SdataRegistrierung.AddFaStrukturQuelle(builder.Services);
    builder.Services.AddScoped<IFaHierarchySyncService, FaHierarchySyncService>();
    builder.Services.AddScoped<IFaMaterializationSyncService, FaMaterializationSyncService>();
    builder.Services.AddScoped<IProductionWorkplaceSyncService, ProductionWorkplaceSyncService>();
    builder.Services.AddScoped<IOperationConfigSyncService, OperationConfigSyncService>();
    // Neue SData-Lese-Abgleiche ohne View-Weg (v1.73.0 E6b), Gates Sync:PpsMitarbeiterEnabled / Sync:StillstandsgruendeEnabled (Default false).
    builder.Services.AddScoped<IDEALAKEWMSService.Services.Sdata.PpsMitarbeiterSyncService>();
    builder.Services.AddScoped<IDEALAKEWMSService.Services.Sdata.StillstandsgruendeSyncService>();
    // E8c: Kontokorrent -> Kreditoren/Debitoren, ArtikelLieferantInfo -> ArticleSuppliers (Gates Sync:KreditorenEnabled /
    // Sync:DebitorenEnabled / Sync:ArtikelLieferantEnabled, Default false).
    builder.Services.AddScoped<IDEALAKEWMSService.Services.Sdata.KontokorrentSyncService>();
    builder.Services.AddScoped<IDEALAKEWMSService.Services.Sdata.ArtikelLieferantSyncService>();
    // v1.84.0 E1: offene Sage-Bestellungen (EBB, seit v1.99.15 auch Rahmenabrufe EBZ) -> SagePurchaseOrderLines (Gate Sync:EinkaufBestellungenEnabled, Default false).
    builder.Services.AddScoped<IDEALAKEWMSService.Services.Sdata.EinkaufBestellungenSyncService>();
    // v1.99.10: Lieferverzug-Mails aus dem Bestell-Cache (Gate Lieferverzug:MailAktiv, Default false); Rest-Regel ueber das Web-Repository.
    builder.Services.AddScoped<IdealAkeWms.Data.Repositories.IPurchaseOrderReceiptRepository, IdealAkeWms.Data.Repositories.PurchaseOrderReceiptRepository>();
    builder.Services.AddScoped<LieferverzugMailService>();
    builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();
    builder.Services.AddScoped<IProductionOrderPickingStatusRepository, ProductionOrderPickingStatusRepository>();
    builder.Services.AddScoped<ISageBookingQueueRepository, SageBookingQueueRepository>();
    // Lagerbuchungs-Lookup (Memo): Quellen-Weiche (Sync:Quelle:LagerbuchungLookup, Default View = SQL-Reader unveraendert), v1.73.0 E7.
    IDEALAKEWMSService.Services.Sdata.SdataRegistrierung.AddBuchungLookupQuelle(builder.Services);
    builder.Services.AddScoped<ICoatingDetectionService, CoatingDetectionService>();
    builder.Services.AddScoped<IFaWorkStepDetectionService, FaWorkStepDetectionService>();
    builder.Services.AddScoped<IWorkOperationStructureDetectionService, WorkOperationStructureDetectionService>();
    builder.Services.AddScoped<IEnaioDmsSyncService, EnaioDmsSyncService>();
    builder.Services.AddScoped<IStockCheckService, StockCheckService>();
    builder.Services.AddScoped<IMailService, MailService>();
    builder.Services.AddScoped<ISyncErrorNotifier, SyncErrorNotifier>();
    builder.Services.AddScoped<IPartRequisitionEmailService, PartRequisitionEmailService>();
    builder.Services.AddScoped<IWarehouseRequisitionRepository, WarehouseRequisitionRepository>();
    builder.Services.AddScoped<IWarehouseRequisitionEmailService, WarehouseRequisitionEmailService>();
    builder.Services.AddScoped<IBdeShiftCalendarService, BdeShiftCalendarService>();
    builder.Services.AddScoped<IBdeAutoPauseService, BdeAutoPauseService>();
    builder.Services.AddScoped<IActivityLogCleanupService, ActivityLogCleanupService>();

    // HolidaySync — typed HttpClient gegen Nager.Date
    builder.Services.Configure<HolidaySyncOptions>(builder.Configuration.GetSection("Sync"));
    builder.Services.AddHttpClient<IHolidaySyncService, HolidaySyncService>(client =>
    {
        client.BaseAddress = new Uri("https://date.nager.at/");
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    // Sage-SData-Schreib-Clients (Lagerbuchung, v1.99.4 Wareneingangsbeleg): EIN Handler-Rezept, Zertifikats-Schalter zur Laufzeit.
    static HttpMessageHandler SageSchreibHandler(IServiceProvider sp)
    {
        // Der Primary Handler wird EINMAL erzeugt und gepoolt. Der Zertifikats-Callback liest den
        // Schalter deshalb ZUR LAUFZEIT (nicht hier bei der Registrierung) aus ServiceSettings, damit
        // eine Aenderung ohne Dienst-Neustart greift. Wirkt ausschliesslich auf DIESEN Client.
        var configuration = sp.GetRequiredService<IConfiguration>();
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
            {
                if (errors == System.Net.Security.SslPolicyErrors.None)
                    return true;
                // Fail-safe: DB-Fehler -> null -> geprueft; nur explizit 'false' deaktiviert.
                var raw = IDEALAKEWMSService.Common.ServiceSettings
                    .GetValueSafeAsync(configuration, IdealAkeWms.Services.SageTlsPolicy.SettingKey)
                    .GetAwaiter().GetResult();
                // pruefen -> ungueltiges Zertifikat ablehnen; deaktiviert -> trotz Fehler akzeptieren.
                return !IdealAkeWms.Services.SageTlsPolicy.ShouldVerifyCertificate(raw);
            }
        };
    }

    // Sage-Lagerbuchung — typed HttpClient (BaseAddress wird pro Request aus ServiceSettings gebaut).
    builder.Services.AddHttpClient<ISageLagerbuchungClient, SageLagerbuchungClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(SageSchreibHandler);

    // v1.99.4 (Wareneingang Teil 2, E4): Sage-Wareneingangsbeleg (StandardBelegService) + Sende-Lauf im SageBookingWorker-Takt.
    builder.Services.AddHttpClient<ISageWareneingangClient, SageWareneingangClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60);
    })
    .ConfigurePrimaryHttpMessageHandler(SageSchreibHandler);
    builder.Services.AddScoped<SageWareneingangService>();
    builder.Services.AddScoped<WareneingangCsvService>();   // v1.99.11 Wareneingang-CSV (Schritt im SageBookingWorker)
    // v1.99.7 E5: Sage-Pruefung vor jedem erneuten Senden (EinkaufBelegeInfo, nur GET).
    builder.Services.AddScoped<IDEALAKEWMSService.Services.Sdata.ISageWareneingangLookup, IDEALAKEWMSService.Services.Sdata.SdataWareneingangLookupReader>();

    // Sage-SData-Lese-Client (Stammdaten-Abgleich, v1.73.0): Paging ueber CommonWawiServicesResources, TLS-Schalter je Anfrage.
    IDEALAKEWMSService.Services.Sdata.SdataRegistrierung.AddSageSdataReadClient(builder.Services);

    // Workers
    builder.Services.AddHostedService<SyncWorker>();
    builder.Services.AddHostedService<NotificationWorker>();
    builder.Services.AddHostedService<CleanupWorker>();
    builder.Services.AddHostedService<SageBookingWorker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Service konnte nicht gestartet werden.");
}
finally
{
    Log.CloseAndFlush();
}
