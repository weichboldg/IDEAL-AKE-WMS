using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Middleware;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Server.IIS;
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

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.CommandTimeout(120)));

// DataProtection-Keys PERSISTENT ablegen. Sonst nutzt der IIS-App-Pool (ohne geladenes
// Benutzerprofil) ein ephemeres Key-Repository -> bei jedem Recycle neue Keys -> bereits
// ausgegebene Antiforgery-Tokens lassen sich nicht mehr entschluesseln -> Fehlerseite beim
// naechsten Formular-POST (z. B. Abmelden). Pfad ueber Config 'DataProtection:KeysPath'
// ueberschreibbar; Default unter %ProgramData% (ueberlebt Redeploys).
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtectionKeysPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "IdealAkeWms", "DataProtection-Keys");
}
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("IdealAkeWms");

// Authentication - Windows-Auth über IIS in-process Hosting
builder.Services.AddAuthentication(IISServerDefaults.AuthenticationScheme);

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
builder.Services.AddScoped<WindowsAutoLoginMiddleware>();
builder.Services.AddScoped<IChallengeIssuer, IdealAkeWms.Middleware.IISChallengeIssuer>();
builder.Services.AddScoped<IWorkstationRepository, WorkstationRepository>();
builder.Services.AddScoped<IProductionWorkplaceRepository, ProductionWorkplaceRepository>();
builder.Services.AddScoped<IStorageLocationRepository, StorageLocationRepository>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
// Sage-Lagerbuchungen: Enqueue-Decorator (Subclassing) auf IStockMovementRepository.
// Bei deaktiviertem Toggle reiner Pass-through (bit-identisches Verhalten, S7).
builder.Services.AddScoped<ISageBookingQueueRepository, SageBookingQueueRepository>();
builder.Services.AddScoped<IStockMovementRepository, SageBookingEnqueueingStockMovementRepository>();
builder.Services.AddScoped<IProductionOrderRepository, ProductionOrderRepository>();
builder.Services.AddScoped<IProductionOrderPickingStatusRepository, ProductionOrderPickingStatusRepository>();
builder.Services.AddScoped<IProductionOrderBdeStatusRepository, ProductionOrderBdeStatusRepository>();
builder.Services.AddScoped<IWorkStepRepository, WorkStepRepository>();
builder.Services.AddScoped<IFaWorkStepRepository, FaWorkStepRepository>();
builder.Services.AddScoped<IFaAttributeRepository, FaAttributeRepository>();
builder.Services.AddScoped<AppSettingRepository>();
builder.Services.AddScoped<IAppSettingRepository, CachedSettingRepository>();
// ServiceSettings: Durchsetzungs-Decorator (FA-Hierarchie-Einwegtor, Teil 7) um das echte Repo.
// Einziger Choke-Point fuer Schreibversuche auf die geschuetzten Keys (Master + Auto-Erledigt).
builder.Services.AddScoped<ServiceSettingRepository>();
builder.Services.AddScoped<IServiceSettingRepository>(sp =>
    new IdealAkeWms.Data.Repositories.GuardedServiceSettingRepository(
        sp.GetRequiredService<ServiceSettingRepository>(),
        sp.GetRequiredService<IProductionOrderRepository>()));
builder.Services.AddSingleton<IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturStatus>();
builder.Services.AddScoped<HolidayRepository>();
builder.Services.AddScoped<IHolidayRepository, CachedHolidayRepository>();
builder.Services.AddScoped<BomCacheRepository>();
builder.Services.AddScoped<BomRepository>();
builder.Services.AddScoped<CachedBomRepository>();
builder.Services.AddScoped<FaHierarchyBomRepository>();
// BOM-Bridge (Spec 2026-09-08): EINE Weiche fuer beide Interfaces, entscheidet pro Aufruf am Master.
// Lazy-Delegates, weil BomRepository selbst IBomCacheRepository (= die Weiche) injiziert — eager waere ein Zyklus.
builder.Services.AddScoped<BomRepositoryMasterSwitch>(sp => new BomRepositoryMasterSwitch(
    sp.GetRequiredService<IServiceSettingRepository>(),
    hierarchicalBom: () => sp.GetRequiredService<FaHierarchyBomRepository>(),
    hierarchicalCache: () => sp.GetRequiredService<FaHierarchyBomRepository>(),
    flatBom: () => sp.GetRequiredService<CachedBomRepository>(),
    flatCache: () => sp.GetRequiredService<BomCacheRepository>()));
builder.Services.AddScoped<IBomRepository>(sp => sp.GetRequiredService<BomRepositoryMasterSwitch>());
builder.Services.AddScoped<IBomCacheRepository>(sp => sp.GetRequiredService<BomRepositoryMasterSwitch>());

// FA-Hierarchie (IDEAL): Repo + Cache-Decorator (ADR 0001, 5-min IMemoryCache analog BOM-Cache)
builder.Services.AddScoped<FaHierarchyNodeRepository>();
builder.Services.AddScoped<IFaHierarchyNodeRepository, CachedFaHierarchyNodeRepository>();
builder.Services.AddScoped<IFaHierarchyOrderInfoRepository, FaHierarchyOrderInfoRepository>();
// IDEAL Teil 3: gemeinsamer Listen-/Druck-Baustein (Teil 4/5 erweitern ihn) + Teil-3-Service.
builder.Services.AddScoped<FaHierarchyListBuilder>();
builder.Services.AddScoped<KommissionierListenService>();
builder.Services.AddScoped<BeschichtungsauftragService>();
builder.Services.AddScoped<VormontageService>();
builder.Services.AddSingleton<IBarcodeService, BarcodeService>();
builder.Services.AddScoped<IPickingRepository, PickingRepository>();
builder.Services.AddScoped<IWorkOperationRepository, WorkOperationRepository>();
builder.Services.AddScoped<IOseonProductionOrderRepository, OseonProductionOrderRepository>();
builder.Services.AddScoped<OseonOperationConfigRepository>();
builder.Services.AddScoped<IOseonOperationConfigRepository, CachedOseonOperationConfigRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IEnaioDmsDocumentRepository, EnaioDmsDocumentRepository>();
builder.Services.AddScoped<IOrderRecipientRepository, OrderRecipientRepository>();
builder.Services.AddScoped<IPartRequisitionRepository, PartRequisitionRepository>();
builder.Services.AddScoped<IWarehouseRequisitionRepository, WarehouseRequisitionRepository>();
builder.Services.AddScoped<IArticleCategoryRepository, ArticleCategoryRepository>();
builder.Services.AddScoped<IArticleAttributeRepository, ArticleAttributeRepository>();
builder.Services.AddScoped<IUserViewPreferenceRepository, UserViewPreferenceRepository>();
builder.Services.AddScoped<IBdeOperatorRepository, BdeOperatorRepository>();
builder.Services.AddScoped<IBdeActivityRepository, BdeActivityRepository>();
builder.Services.AddScoped<IBdeTerminalRepository, BdeTerminalRepository>();
builder.Services.AddScoped<IBdeBookingRepository, BdeBookingRepository>();
builder.Services.AddScoped<ISyncLogRepository, SyncLogRepository>();
builder.Services.AddSingleton<IdealAkeWms.Services.SyncLogger.ISyncLogger,
                              IdealAkeWms.Services.SyncLogger.SyncLogger>();

// Caching
builder.Services.AddMemoryCache();

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IBusinessDayService, BusinessDayService>();
builder.Services.AddHttpClient<IHolidayImportService, HolidayImportService>();
builder.Services.AddScoped<IPrintService, PrintService>();

// PDF-Erzeugung (Spec 2026-08-06): Status + Renderdienst SINGLETON — das SemaphoreSlim im Dienst
// begrenzt die Edge-Prozesse nur anfrageuebergreifend, wenn es genau eine Instanz gibt.
builder.Services.AddSingleton<PdfRenderStatus>();
builder.Services.AddSingleton<IEdgeProcessRunner, EdgeProcessRunner>();
builder.Services.AddSingleton<IPdfRenderService, PdfRenderService>();
builder.Services.AddScoped<IRazorViewRenderer, RazorViewRenderer>();
builder.Services.AddScoped<ReadOnlyBomBuilder>();
builder.Services.AddScoped<FaHierarchyTreeBuilder>();
builder.Services.AddScoped<IPickingTransferService, PickingTransferService>();
builder.Services.AddScoped<IOseonTrafficLightService, OseonTrafficLightService>();
builder.Services.AddScoped<IBdeBookingService, BdeBookingService>();
builder.Services.AddScoped<IBdeDefaultWorkOperationService, BdeDefaultWorkOperationService>();
builder.Services.AddScoped<IBdeTimeSplitService, BdeTimeSplitService>();
builder.Services.AddScoped<IBdeShiftCalendarService, BdeShiftCalendarService>();
builder.Services.AddScoped<IBdeScanResolver, BdeScanResolver>();
builder.Services.AddScoped<IdealAkeWms.Services.Standort.IStandortSettingsWriter,
                          IdealAkeWms.Services.Standort.StandortSettingsWriter>();
builder.Services.AddScoped<IdealAkeWms.Services.Oseon.IOseonGroupViewModelBuilder,
                          IdealAkeWms.Services.Oseon.OseonGroupViewModelBuilder>();

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
        (RoleKeys.StockRead, "Lagerbestand-Ansicht", "Nur-Lesen-Zugriff auf Bestände und Bewegungshistorie", 35),
        (RoleKeys.Tracking, "Teileverfolgung", "OSEON Teileverfolgung und Rückmeldungen", 50),
        (RoleKeys.Reporting, "Betriebsdaten (BDE)", "Arbeitsgänge stempeln und rückmelden", 60),
        (RoleKeys.Leitstand, "Leitstand", "Produktionsaufträge freigeben und priorisieren", 70),
        (RoleKeys.FaCompletion, "FA-Vervollständigung", "Fertigungsaufträge nach Kommissionierung vervollständigen", 80),
        (RoleKeys.BdeUser, "BDE-Mitarbeiter", "Terminal-Buchung: Arbeitsgänge scannen, Status wechseln, Mengen melden", 100),
        (RoleKeys.BdeShiftlead, "BDE-Schichtleiter", "BDE-Anwender + Aktivitäts-Kategorien pflegen, Buchungsliste + Cockpit", 101),
        (RoleKeys.BdeAdmin, "BDE-Admin", "Vollzugriff: Buchungen korrigieren und stornieren, Terminals konfigurieren", 102),
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
        ("OseonReportingHorizonDays", "10", "Reporting: Tage in die Zukunft (Default-Horizont)"),
        ("OseonReportingOverdueLookbackDays", "90", "Reporting: Tage in die Vergangenheit fuer Ueberfaellig-Slice"),
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
    (IdealAkeWms.Models.AppSettingKeys.FaCompletionAktiv, "false", "FA-Vervollstaendigungs-Modul aktivieren (Pflege fehlender FA-Daten)"),
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

    // Bedarfsmeldungen + Lagerbestellungen AppSettings
    var requisitionSettings = new (string Key, string Value, string Description)[]
    {
        ("BestellungenAktiv", "false", "Bedarfsmeldungen aus Stueckliste aktivieren"),
        ("DefaultLagerbestellempfaengerId", "", "Default-OrderRecipientGroup-ID fuer Lagerbestellungen (leer = Submit blockt)"),
        ("DefaultGlasbestellempfaengerId", "", "Default-OrderRecipientGroup-ID fuer Glas-Bestellungen (leer = Submit blockt)"),
        ("GlasArtikelgruppen", "", "Kommaseparierte Artikelgruppen fuer Glas-Bestellungen (in der Lager-Bestellung ausgenommen)"),
        ("GemeinsameArtikelgruppen", "EUZ", "Kommaseparierte Artikelgruppen, die in Lager- UND Glas-Bestellungen verfuegbar sind"),
        (IdealAkeWms.Models.AppSettingKeys.LagerbestellungAktiv, "true", "Lagerbestellungs-Modul aktivieren (Lager+Glas, Meine Fehlteile, Lager-Worklists, BOM-Button). Default true — bestehende Systeme bleiben aktiv."),
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

    // Windows-Auth / AD AppSettings
    var windowsAuthSettings = new (string Key, string Value, string Description)[]
    {
        (IdealAkeWms.Models.AppSettingKeys.WindowsAuthAktiv, "false", "Windows-Anmeldung (Auto-Login) aktivieren"),
        (IdealAkeWms.Models.AppSettingKeys.WindowsAuthBerechtigungsgruppe, "", "AD-Berechtigungsgruppe (SAM-Name) fuer 'AD-Benutzer anlegen'"),
    };
    foreach (var (key, value, description) in windowsAuthSettings)
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

    // BDE Settings
    var bdeSettings = new (string Key, string Value, string Description)[]
    {
        ("BdeAktiv", "false", "BDE-Modul (Betriebsdatenerfassung) aktivieren"),
        ("BdeNurFaMeldung", "false", "Vereinfachter BDE-Modus: Buchung auf FA statt einzelne Arbeitsgaenge"),
        ("BdeDefaultArbeitsgang", "", "Default-Arbeitsgang fuer vereinfachten BDE-Modus (z.B. PRODUKTION)"),
        (IdealAkeWms.Models.AppSettingKeys.BdeMehrfachBuchungProOperator, "false", "Ein Mitarbeiter darf mehrere parallele Buchungen haben (auf verschiedenen Arbeitsgaengen)"),
        (IdealAkeWms.Models.AppSettingKeys.BdeMehrfachBuchungProArbeitsgang, "false", "Ein Arbeitsgang darf mehrere parallele Buchungen haben (durch verschiedene Mitarbeiter)"),
        (IdealAkeWms.Models.AppSettingKeys.BdeGleichzeitigerAbschlussBeiMehrfachStart, "false", "Alle parallel gestarteten Produktionsbuchungen eines Mitarbeiters muessen gemeinsam fertiggemeldet werden (nur wirksam wenn BdeMehrfachBuchungProOperator aktiv)"),
        (IdealAkeWms.Models.AppSettingKeys.BdeSchichtkalenderAktiv, "false", "Schichtkalender + Auto-Pause am Schichtende aktiv"),
    };
    foreach (var (key, value, description) in bdeSettings)
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

    // IDEAL FA-Hierarchie AppSettings (Teile 2–5). Ohne DB-Zeile sind die Feature-Toggles
    // per RequireFaHierarchy…Aktiv-Filter inaktiv und die Listen komplett gesperrt; deshalb
    // hier mit Default seeden, damit die Keys in /Settings (Gruppe „IDEAL") einschaltbar sind.
    var idealFaHierarchySettings = new (string Key, string Value, string Description)[]
    {
        (IdealAkeWms.Models.AppSettingKeys.FaHierarchyKommissionierlistenAktiv, "false", "IDEAL Kommissionierlisten-Modul aktivieren (Teil 3). Default false."),
        (IdealAkeWms.Models.AppSettingKeys.FaHierarchyBeschichtungAktiv, "false", "IDEAL Beschichtungsauftrag-Modul aktivieren (Teil 4). Default false."),
        (IdealAkeWms.Models.AppSettingKeys.FaHierarchyVormontageAktiv, "false", "IDEAL Vormontage-Listen aktivieren (Teil 5, zwei Sichten). Default false."),
        (IdealAkeWms.Models.AppSettingKeys.FaHierarchyMaxDepth, "500", "IDEAL FA-Hierarchie: max. Rekursionstiefe (Reissleine) beim Baumaufbau. Default 500."),
    };
    foreach (var (key, value, description) in idealFaHierarchySettings)
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

    // Standard Service-Settings — vollstaendig aus dem typisierten Katalog geseedet.
    // Idempotent: bestehende Zeilen (User-Werte) bleiben unberuehrt.
    foreach (var def in IdealAkeWms.Models.ServiceSettingDefinitions.All)
    {
        if (!db.ServiceSettings.Any(s => s.Key == def.Key))
        {
            db.ServiceSettings.Add(new IdealAkeWms.Models.ServiceSetting
            {
                Key = def.Key,
                Value = def.DefaultValue,
                Category = def.Category,
                Description = def.Description
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

// FA-Hierarchie-Anzeigezustand initial befuellen (Freigabe-Antwort 3: Refresh am Start).
// Der Schreibpfad prueft ohnehin live; die Anzeige (Umschalt-Seite) liest diesen Cache.
app.Services.GetRequiredService<IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturStatus>()
    .RefreshAsync().GetAwaiter().GetResult();

// msedge.exe einmal beim Start suchen (Freigabe-Antwort 2): fehlt Edge, bleibt der PDF-Knopf weg
// und das Log traegt die Warnung — der Anwender laeuft nicht in eine Wand.
app.Services.GetRequiredService<PdfRenderStatus>().Refresh();

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

// Windows-Auto-Login: vor dem Login-Redirect — versucht Session-Login per Windows-Identity
app.UseMiddleware<WindowsAutoLoginMiddleware>();

// Login-Redirect Middleware: Wenn kein Benutzer in Session, auf Login umleiten
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? "";

    // Login-Seite und statische Dateien ausschließen
    if (path.StartsWith("/account/login") ||
        path.StartsWith("/account/logout") ||
        path.StartsWith("/account/windowslogin") ||
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
