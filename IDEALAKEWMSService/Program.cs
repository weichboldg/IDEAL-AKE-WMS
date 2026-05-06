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

    // Caching (für CachedSettingRepository)
    builder.Services.AddMemoryCache();

    // Repositories (aus IdealAkeWms-Web-Projekt; werden von BdeAutoPause + ShiftCalendar benötigt)
    builder.Services.AddScoped<AppSettingRepository>();
    builder.Services.AddScoped<IAppSettingRepository, CachedSettingRepository>();
    builder.Services.AddScoped<ISyncLogRepository, SyncLogRepository>();

    // Services (Scoped für Worker über IServiceScopeFactory)
    builder.Services.AddScoped<ISageImportService, SageImportService>();
    builder.Services.AddScoped<ISageLagerplatzReader, SageLagerplatzReader>();
    builder.Services.AddScoped<ISageBestandReader, SageBestandReader>();
    builder.Services.AddScoped<IOseonSyncService, OseonSyncService>();
    builder.Services.AddScoped<IBomCacheSyncService, BomCacheSyncService>();
    builder.Services.AddScoped<ILagerplatzSyncService, LagerplatzSyncService>();
    builder.Services.AddScoped<ICoatingDetectionService, CoatingDetectionService>();
    builder.Services.AddScoped<IEnaioDmsSyncService, EnaioDmsSyncService>();
    builder.Services.AddScoped<IStockCheckService, StockCheckService>();
    builder.Services.AddScoped<IMailService, MailService>();
    builder.Services.AddScoped<IPartRequisitionEmailService, PartRequisitionEmailService>();
    builder.Services.AddScoped<IWarehouseRequisitionRepository, WarehouseRequisitionRepository>();
    builder.Services.AddScoped<IWarehouseRequisitionEmailService, WarehouseRequisitionEmailService>();
    builder.Services.AddScoped<IBdeShiftCalendarService, BdeShiftCalendarService>();
    builder.Services.AddScoped<IBdeAutoPauseService, BdeAutoPauseService>();

    // HolidaySync — typed HttpClient gegen Nager.Date
    builder.Services.Configure<HolidaySyncOptions>(builder.Configuration.GetSection("Sync"));
    builder.Services.AddHttpClient<IHolidaySyncService, HolidaySyncService>(client =>
    {
        client.BaseAddress = new Uri("https://date.nager.at/");
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    // Workers
    builder.Services.AddHostedService<SyncWorker>();
    builder.Services.AddHostedService<NotificationWorker>();

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
