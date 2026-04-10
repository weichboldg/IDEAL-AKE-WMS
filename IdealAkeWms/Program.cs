using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
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
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(120)));

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
builder.Services.AddScoped<AppSettingRepository>();
builder.Services.AddScoped<IAppSettingRepository, CachedSettingRepository>();
builder.Services.AddScoped<IServiceSettingRepository, ServiceSettingRepository>();
builder.Services.AddScoped<HolidayRepository>();
builder.Services.AddScoped<IHolidayRepository, CachedHolidayRepository>();
builder.Services.AddScoped<IBomCacheRepository, BomCacheRepository>();
builder.Services.AddScoped<BomRepository>();
builder.Services.AddScoped<IBomRepository, CachedBomRepository>();
builder.Services.AddScoped<IPickingRepository, PickingRepository>();
builder.Services.AddScoped<IWorkOperationRepository, WorkOperationRepository>();
builder.Services.AddScoped<IOseonProductionOrderRepository, OseonProductionOrderRepository>();
builder.Services.AddScoped<OseonOperationConfigRepository>();
builder.Services.AddScoped<IOseonOperationConfigRepository, CachedOseonOperationConfigRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IEnaioDmsDocumentRepository, EnaioDmsDocumentRepository>();
builder.Services.AddScoped<IOrderRecipientRepository, OrderRecipientRepository>();
builder.Services.AddScoped<IPartRequisitionRepository, PartRequisitionRepository>();
builder.Services.AddScoped<IArticleCategoryRepository, ArticleCategoryRepository>();
builder.Services.AddScoped<IArticleAttributeRepository, ArticleAttributeRepository>();
builder.Services.AddScoped<IUserViewPreferenceRepository, UserViewPreferenceRepository>();

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

    // Standard-Rollen
    var defaultRoles = new (string Key, string Name, string? Description, int SortOrder)[]
    {
        (RoleKeys.Admin, "Administrator", "Vollzugriff auf alle Funktionen", 0),
        (RoleKeys.MasterData, "Stammdaten", "Verwaltung von Benutzern, Arbeitsplätzen und Einstellungen", 10),
        (RoleKeys.Picking, "Kommissionierer", "Kommissionierung und vollständiger Lagerzugriff", 20),
        (RoleKeys.Stock, "Lager", "Einbuchung, Ausbuchung und Bestandsübersicht", 30),
        (RoleKeys.StockKeyUser, "Lager Keyuser", "Lager + Lagerplatz ausbuchen/umbuchen", 40),
        (RoleKeys.Tracking, "Teileverfolgung", "OSEON Teileverfolgung und Rückmeldungen", 50),
        (RoleKeys.Reporting, "Betriebsdaten (BDE)", "Arbeitsgänge stempeln und rückmelden", 60),
        (RoleKeys.Leitstand, "Leitstand", "Produktionsaufträge freigeben und priorisieren", 70),
    };
    foreach (var (key, name, description, sortOrder) in defaultRoles)
    {
        if (!db.Roles.Any(r => r.Key == key))
        {
            db.Roles.Add(new Role
            {
                Key = key,
                Name = name,
                Description = description,
                SortOrder = sortOrder,
                IsSystem = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system",
                CreatedByWindows = "system"
            });
        }
    }
    db.SaveChanges();

    // Standard-Admin-Benutzer
    var adminUser = db.Users.FirstOrDefault(u => u.Name == "admin");
    if (adminUser == null)
    {
        adminUser = new User
        {
            Name = "admin",
            IsActive = true,
            PasswordHash = passwordService.HashPassword(""),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            CreatedByWindows = "system"
        };
        db.Users.Add(adminUser);
        db.SaveChanges();
    }
    else if (adminUser.PasswordHash == null)
    {
        adminUser.PasswordHash = passwordService.HashPassword("");
        db.SaveChanges();
    }

    // Admin-Rolle zuweisen
    var adminRole = db.Roles.FirstOrDefault(r => r.Key == RoleKeys.Admin);
    if (adminRole != null && !db.UserRoles.Any(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id))
    {
        db.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
            CreatedByWindows = "system"
        });
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

    // Leitstand AppSettings
    var leitstandSettings = new (string Key, string Value, string Description)[]
    {
        ("LeitstandAktiv", "false", "Leitstand-Modul: Kommissionier-Freigabe und Priorisierung aktivieren"),
    ("KommissionierungMitZuweisung", "false", "Kommissionierung mit Anwenderzuweisung aktivieren"),
    };
    foreach (var (key, value, description) in leitstandSettings)
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

    // Lackierteil-Erkennung AppSettings
    var coatingSettings = new (string Key, string Value, string Description)[]
    {
        ("LackierteilKategorieName", "", "Name der Artikelkategorie die als Lackierteil gilt. Leer = Feature inaktiv (Beschichtungstermin wie vorher fuer alle)"),
    };
    foreach (var (key, value, description) in coatingSettings)
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

    // Bedarfsmeldungen AppSettings
    var requisitionSettings = new (string Key, string Value, string Description)[]
    {
        ("BestellungenAktiv", "false", "Bedarfsmeldungen aus Stueckliste aktivieren"),
    };
    foreach (var (key, value, description) in requisitionSettings)
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

    // Fehlende Settings nachseeden (wurden in SQL-Migrationen aber nicht im Seeding angelegt)
    var missingSettings = new (string Key, string Value, string Description)[]
    {
        ("OseonAmpelGelbTage", "1", "OSEON Ampel: Gelb ab X Tagen vor Termin"),
        ("OseonAmpelBlauTage", "2", "OSEON Ampel: Blau ab X Tagen vor Termin"),
        ("QrMitFaNummer", "false", "QR-Code enthaelt Fertigungsauftragsnummer"),
    };
    foreach (var (key, value, description) in missingSettings)
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
        ("Sync:BomCacheEnabled",         "false", "BOM-Cache",   "BOM-Cache-Sync aktiv (Top-N offene Auftraege werden gecacht)"),
        ("Sync:BomCacheWeeks",           "8",     "BOM-Cache",   "Wieviele Wochen Fertigungstermin in die Zukunft cachen"),
        ("Sync:BomCacheMaxOrders",       "200",   "BOM-Cache",   "Maximalanzahl Auftraege im BOM-Cache"),
        ("Sync:BomCacheMaxAgeHours",     "24",    "BOM-Cache",   "Sicherheitsnetz: Re-Sync wenn Cache-Eintrag aelter als X Stunden"),
        ("Sync:CoatingDetectionEnabled", "false", "Lackierteile","Lackierteil-Erkennung als separater Sync-Job aktiv"),
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

    // Standard-Arbeitsgang-Konfigurationen (OSEON)
    if (!db.OseonOperationConfigs.Any())
    {
        var defaultOpConfigs = new (string Name, string Display, int Offset, bool Relevant)[]
        {
            ("B",       "Belegen",         -1, true),
            ("ST",      "Stanzen",          0, true),
            ("EG",      "Entgraten",        0, true),
            ("BG",      "Biegen",           2, true),
            ("BG-SaP1", "Biegen SaP1",     2, true),
            ("RO",      "Rollen",           2, true),
            ("MS",      "Maschinenschub",   4, true),
            ("RS",      "Restschweissen",   4, true),
            ("SL",      "Schlosser",        5, true),
            ("RE",      "Reinigen",         5, true),
            ("ZB",      "Zusammenbau",      0, false),
            ("A-BT",    "Anlegen BT",       0, false),
        };
        foreach (var (name, display, offset, relevant) in defaultOpConfigs)
        {
            db.OseonOperationConfigs.Add(new IdealAkeWms.Models.OseonOperationConfig
            {
                OperationName = name,
                DisplayName = display,
                DueDateOffsetDays = offset,
                IsOseonRelevant = relevant
            });
        }
        db.SaveChanges();
    }
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
