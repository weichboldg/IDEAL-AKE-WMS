namespace IdealAkeWms.Services.HierarchischeStruktur;

/// <summary>
/// Die vom <see cref="HierarchischeStrukturGuard"/> geschützten ServiceSettings-Keys
/// (Master-Einwegtor + Auto-Erledigt). Der Auto-Erledigt-Key wird hier bereits benannt
/// und geschützt; sein Katalog-Eintrag + die Fold-2-Verdrahtung folgen in Etappe D.
/// </summary>
public static class HierarchischeStrukturKeys
{
    public const string Master = "ProduktionsauftragHierarchisch";
    public const string AutoErledigt = "Sync:FaZusatzinfoAutoErledigtEnabled";

    public static bool IsGuarded(string key) =>
        string.Equals(key, Master, StringComparison.Ordinal)
        || string.Equals(key, AutoErledigt, StringComparison.Ordinal);

    public static GuardedSetting Map(string key) =>
        string.Equals(key, Master, StringComparison.Ordinal)
            ? GuardedSetting.Master
            : GuardedSetting.AutoErledigt;
}
