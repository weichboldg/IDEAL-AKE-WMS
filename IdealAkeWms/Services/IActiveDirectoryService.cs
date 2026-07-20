namespace IdealAkeWms.Services;

public interface IActiveDirectoryService
{
    /// <summary>Liest die Mitglieder der konfigurierten Berechtigungsgruppe. Bei jedem Fehler: leere Liste.</summary>
    Task<IReadOnlyList<AdUserCandidate>> GetAuthorizationGroupMembersAsync(CancellationToken ct = default);
}
