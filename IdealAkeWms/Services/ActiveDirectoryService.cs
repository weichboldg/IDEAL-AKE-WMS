using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly IAppSettingRepository _appSettings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ActiveDirectoryService> _logger;

    public ActiveDirectoryService(IAppSettingRepository appSettings, IConfiguration configuration,
        ILogger<ActiveDirectoryService> logger)
    {
        _appSettings = appSettings;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AdUserCandidate>> GetAuthorizationGroupMembersAsync(CancellationToken ct = default)
    {
        var group = await _appSettings.GetValueAsync(AppSettingKeys.WindowsAuthBerechtigungsgruppe);
        if (string.IsNullOrWhiteSpace(group) || !OperatingSystem.IsWindows())
            return Array.Empty<AdUserCandidate>();

        var domain = _configuration["Security:AdDomain"];
        try
        {
            return QueryMembers(group.Trim(), string.IsNullOrWhiteSpace(domain) ? null : domain);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AD-Abfrage der Berechtigungsgruppe '{Group}' fehlgeschlagen", group);
            return Array.Empty<AdUserCandidate>();
        }
    }

    [SupportedOSPlatform("windows")]
    private static List<AdUserCandidate> QueryMembers(string group, string? domain)
    {
        using var ctx = domain == null
            ? new PrincipalContext(ContextType.Domain)
            : new PrincipalContext(ContextType.Domain, domain);
        using var grp = GroupPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, group);

        var result = new List<AdUserCandidate>();
        if (grp == null)
            return result;

        foreach (var member in grp.GetMembers())
        {
            using (member)
            {
                if (member is UserPrincipal up && !string.IsNullOrEmpty(up.SamAccountName))
                    result.Add(new AdUserCandidate(up.SamAccountName, up.DisplayName, up.EmailAddress, up.Enabled ?? true));
            }
        }
        return result.OrderBy(c => c.DisplayName ?? c.SamAccountName).ToList();
    }
}
