using IDEALAKEWMSService.Services;
using IDEALAKEWMSService.Workers;
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

    // Services (Scoped für Worker über IServiceScopeFactory)
    builder.Services.AddScoped<ISageImportService, SageImportService>();
    builder.Services.AddScoped<IOseonSyncService, OseonSyncService>();
    builder.Services.AddScoped<IStockCheckService, StockCheckService>();
    builder.Services.AddScoped<IMailService, MailService>();

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
