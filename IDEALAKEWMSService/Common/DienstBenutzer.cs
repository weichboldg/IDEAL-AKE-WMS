using IdealAkeWms.Services;

namespace IDEALAKEWMSService.Common;

/// <summary>
/// v1.99.70 (Spec 2026-11-25-kommissionierauftraege-nachtrag-nachts): <see cref="ICurrentUserService"/> fuer Web-Workflows im Dienst. Beide
/// Namen = fester Lauf-Name (ADR 0003: Dienste schreiben ihren Namen). Rollenabfragen werfen: Der Dienst hat keine Rollen, eine Abfrage waere
/// ein Fehler und soll laut scheitern statt still false zu liefern.
/// ponytail: Name fest je Registrierung; nutzt ein zweiter Dienst-Lauf Web-Workflows, den Namen je Scope setzen.
/// </summary>
public sealed class DienstBenutzer : ICurrentUserService
{
    private readonly string _name;

    public DienstBenutzer(string name) => _name = name;

    public string GetWindowsUserName() => _name;
    public string GetDisplayName() => _name;
    public int? GetCurrentAppUserId() => null;
    public string? GetCurrentAppUserName() => null;
    public bool IsLoggedIn() => false;
    public Task<int?> GetDefaultPageSizeAsync() => Task.FromResult<int?>(null);

    private static Task<bool> KeineRollen() => throw new NotSupportedException("Der Dienst hat keine Rollen.");

    public Task<bool> HasRoleAsync(string roleKey) => KeineRollen();
    public Task<bool> HasAnyRoleAsync(params string[] roleKeys) => KeineRollen();
    public Task<bool> IsAdminAsync() => KeineRollen();
    public Task<bool> HasMasterDataAccessAsync() => KeineRollen();
    public Task<bool> HasMasterDataReadAccessAsync() => KeineRollen();
    public Task<bool> CanPickAsync() => KeineRollen();
    public Task<bool> CanViewTrackingAsync() => KeineRollen();
    public Task<bool> CanReportOperationsAsync() => KeineRollen();
    public Task<bool> CanAccessStockAsync() => KeineRollen();
    public Task<bool> CanAccessStockReadAsync() => KeineRollen();
    public Task<bool> CanAccessLagerbestellungAsync() => KeineRollen();
    public Task<bool> CanAccessGlasbestellungAsync() => KeineRollen();
    public Task<bool> CanAccessEinkaufAsync() => KeineRollen();
    public Task<bool> CanOrderLagerAsync() => KeineRollen();
    public Task<bool> CanOrderGlasAsync() => KeineRollen();
    public Task<bool> CanSchachtelnAsync() => KeineRollen();
    public Task<bool> CanBfBestellenAsync() => KeineRollen();
    public Task<bool> CanPflegeFraesenPfadAsync() => KeineRollen();
    public Task<bool> CanProcessLagerAsync() => KeineRollen();
    public Task<bool> CanTransferStockAsync() => KeineRollen();
    public Task<bool> CanManagePickingReleaseAsync() => KeineRollen();
    public Task<bool> CanFaCompletionAsync() => KeineRollen();
    public Task<bool> HasVorbauAccessAsync() => KeineRollen();
    public Task<bool> HasBeschichtungsauftragAccessAsync() => KeineRollen();
    public Task<bool> HasBeschichtungsauftragWriteAccessAsync() => KeineRollen();
    public Task<bool> CanUseBdeAsync() => KeineRollen();
    public Task<bool> CanManageBdeShiftleadAsync() => KeineRollen();
    public Task<bool> CanManageBdeAdminAsync() => KeineRollen();
}
