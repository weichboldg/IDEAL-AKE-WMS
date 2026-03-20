using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog konfigurieren aus appsettings.json
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication - Windows/Negotiate
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "IdealAkeWms.Session";
});

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWorkstationRepository, WorkstationRepository>();
builder.Services.AddScoped<IProductionWorkplaceRepository, ProductionWorkplaceRepository>();
builder.Services.AddScoped<IStorageLocationRepository, StorageLocationRepository>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();
builder.Services.AddScoped<IProductionOrderRepository, ProductionOrderRepository>();
builder.Services.AddScoped<IAppSettingRepository, AppSettingRepository>();
builder.Services.AddScoped<IServiceSettingRepository, ServiceSettingRepository>();
builder.Services.AddScoped<IHolidayRepository, HolidayRepository>();
builder.Services.AddScoped<BomRepository>();
builder.Services.AddScoped<IBomRepository, CachedBomRepository>();
builder.Services.AddScoped<IPickingRepository, PickingRepository>();
builder.Services.AddScoped<IWorkOperationRepository, WorkOperationRepository>();
builder.Services.AddScoped<IOseonProductionOrderRepository, OseonProductionOrderRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

// Caching
builder.Services.AddMemoryCache();

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IBusinessDayService, BusinessDayService>();
builder.Services.AddHttpClient<IHolidayImportService, HolidayImportService>();
builder.Services.AddScoped<IPrintService, PrintService>();
builder.Services.AddScoped<IPickingTransferService, PickingTransferService>();
builder.Services.AddScoped<IOseonTrafficLightService, OseonTrafficLightService>();

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Ensure database is created and seed default data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

    db.Database.Migrate();

    // Standard-Lagerplatz NAN
    if (!db.StorageLocations.Any(sl => sl.Code == "NAN"))
    {
        db.StorageLocations.Add(new IdealAkeWms.Models.StorageLocation
        {
            Code = "NAN",
            Description = "Nicht zugeordnet (Fallback)",
            IsPickingTransport = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            CreatedByWindows = "system"
        });
        db.SaveChanges();
    }

    // Standard-Admin-Benutzer
    var adminUser = db.Users.FirstOrDefault(u => u.Name == "admin");
    if (adminUser == null)
    {
        db.Users.Add(new IdealAkeWms.Models.User
        {
            Name = "admin",
            IsActive = true,
            HasMasterDataAccess = true,
            PasswordHash = passwordService.HashPassword(""),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            CreatedByWindows = "system"
        });
        db.SaveChanges();
    }
    else if (adminUser.PasswordHash == null)
    {
        adminUser.PasswordHash = passwordService.HashPassword("");
        db.SaveChanges();
    }

    // Teileverfolgung AppSettings
    var trackingSettings = new (string Key, string Value, string Description)[]
    {
        ("TeileverfolgungAktiv", "false", "Globaler Schalter: Teileverfolgungs-Modul aktiviert"),
        ("OseonRueckmeldungAktiv", "false", "Rueckmeldungen duerfen an Oseon zurueckgeschrieben werden"),
        ("SageRueckmeldungAktiv", "false", "Rueckmeldungen duerfen an Sage zurueckgeschrieben werden"),
    };
    foreach (var (key, value, description) in trackingSettings)
    {
        if (!db.AppSettings.Any(s => s.Key == key))
        {
            db.AppSettings.Add(new IdealAkeWms.Models.AppSetting
            {
                Key = key,
                Value = value,
                Description = description
            });
        }
    }
    db.SaveChanges();

    // Standard Service-Settings
    var serviceSettingSeed = new (string Key, string Value, string Category, string Description)[]
    {
        ("Notifications:MeldebestandEnabled", "true", "Notifications", "Meldebestand-Mail aktiv (true/false)"),
        ("Notifications:MeldebestandSubject", "Meldebestand unterschritten — IDEAL AKE WMS", "Notifications", "Betreff der Meldebestand-Mail"),
        ("Notifications:Recipients", "", "Notifications", "Feste Empfänger für Meldebestand-Mail (kommagetrennt, z.B. lager@ake.at,leitung@ake.at)"),
        ("Notifications:AppBaseUrl", "", "Notifications", "Basis-URL der App für Links in Mails (z.B. https://wms.ake.at)"),
        ("Sync:ProductionOrdersEnabled", "true", "Sync", "Produktionsaufträge-Sync aus SAGE aktiv (true/false)"),
        ("Sync:ArticlesEnabled", "true", "Sync", "Artikel-Sync aus SAGE aktiv (true/false)"),
    };
    foreach (var (key, value, category, description) in serviceSettingSeed)
    {
        if (!db.ServiceSettings.Any(s => s.Key == key))
        {
            db.ServiceSettings.Add(new IdealAkeWms.Models.ServiceSetting
            {
                Key = key,
                Value = value,
                Category = category,
                Description = description
            });
        }
    }
    db.SaveChanges();
}

// Fotos-Verzeichnis erstellen
var fotosDir = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "Fotos", "Kommissionierung");
Directory.CreateDirectory(fotosDir);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// Serilog Request-Logging
app.UseSerilogRequestLogging();

// Login-Redirect Middleware: Wenn kein Benutzer in Session, auf Login umleiten
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? "";

    // Login-Seite und statische Dateien ausschließen
    if (path.StartsWith("/account/login") ||
        path.StartsWith("/account/logout") ||
        path.StartsWith("/api/") ||
        path.StartsWith("/lib/") ||
        path.StartsWith("/css/") ||
        path.StartsWith("/js/") ||
        path.StartsWith("/_framework/") ||
        path.Contains("."))
    {
        await next();
        return;
    }

    // Prüfen ob Benutzer in Session angemeldet
    var userId = context.Session.GetInt32("AppUserId");
    if (!userId.HasValue)
    {
        context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
        return;
    }

    await next();
});

// UseStaticFiles für dynamisch hochgeladene Dateien (Fotos)
app.UseStaticFiles();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
