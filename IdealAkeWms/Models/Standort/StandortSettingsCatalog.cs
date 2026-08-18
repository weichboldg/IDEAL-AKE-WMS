namespace IdealAkeWms.Models.Standort;

public enum StandortFieldType { String, Int, Bool }

/// <summary>Ein editierbares Feld der Standorteinstellungen-Maske (kuratiert, kein Master).</summary>
public sealed record StandortField(
    string Key,
    string Label,
    StandortFieldType Type,
    StandortBackend Backend,
    string Group,
    string? Hint = null);

public enum StandortBackend { App, Service }

/// <summary>
/// IDEAL Teil 6: die EINE kuratierte Feldliste der Standorteinstellungen-Maske (Spec: „eigene,
/// explizite Feldliste"). Bewusst OHNE den Master-Key <c>ProduktionsauftragHierarchisch</c> — der ist
/// read-only (nur Anzeige, siehe Controller) und wird nie über diese Maske geschrieben (AK 2). Ein
/// Key, der hier nicht steht, wird beim POST ignoriert (Allow-List = Schutz gegen manipulierte
/// Requests auf den Master).
/// </summary>
public static class StandortSettingsCatalog
{
    public const string GroupFirmendaten = "Firmendaten";
    public const string GroupMandantViews = "Mandant / Sage-Views";
    public const string GroupToggles = "Feature-Schalter";
    public const string GroupImport = "Struktur-Import";

    private const string ViewNameHint =
        "Wird roh gespeichert; ein ungültiger Name fällt erst beim nächsten Sync-Lauf als Fehlermail auf.";

    public static readonly IReadOnlyList<StandortField> Fields = new List<StandortField>
    {
        // Firmendaten — neue AppSettings-Keys (Definitionsort AppSettingKeys.cs, Fallback-Regel).
        new(AppSettingKeys.Firmenname,      "Firmenname",            StandortFieldType.String, StandortBackend.App,     GroupFirmendaten),
        new(AppSettingKeys.Firmenanschrift, "Anschrift / Adresse",   StandortFieldType.String, StandortBackend.App,     GroupFirmendaten),

        // Mandant + Sage-View-Namen (ServiceSettings).
        new("SData:Dataset",                        "Mandant / Dataset",              StandortFieldType.String, StandortBackend.Service, GroupMandantViews),
        new("Sync:FaHierarchyListeViewName",        "Sage-View FAListe (Stückliste)", StandortFieldType.String, StandortBackend.Service, GroupMandantViews, ViewNameHint),
        new("Sync:FaHierarchyInfosViewName",        "Sage-View FAInfos (PPS/Auftrag)",StandortFieldType.String, StandortBackend.Service, GroupMandantViews, ViewNameHint),

        // Feature-Schalter der Teile 2–5 (AppSettings).
        new(AppSettingKeys.FaHierarchyMaxDepth,                 "Baum-Maximaltiefe",             StandortFieldType.Int,  StandortBackend.App, GroupToggles),
        new(AppSettingKeys.FaHierarchyKommissionierlistenAktiv, "Kommissionierlisten aktiv",     StandortFieldType.Bool, StandortBackend.App, GroupToggles),
        new(AppSettingKeys.FaHierarchyBeschichtungAktiv,        "Beschichtungsauftrag aktiv",    StandortFieldType.Bool, StandortBackend.App, GroupToggles),
        new(AppSettingKeys.FaHierarchyVormontageAktiv,          "Vormontage-Listen aktiv",       StandortFieldType.Bool, StandortBackend.App, GroupToggles),

        // Struktur-Import (ServiceSettings).
        new("Sync:HierarchicalFaEnabled",           "FA-Struktur-Import aktiv",       StandortFieldType.Bool,   StandortBackend.Service, GroupImport),
    };

    /// <summary>Der Master-Key — NUR read-only in dieser Maske angezeigt, nie geschrieben.</summary>
    public const string MasterKey = "ProduktionsauftragHierarchisch";

    public static readonly IReadOnlyList<string> GroupOrder = new[]
    {
        GroupFirmendaten, GroupMandantViews, GroupToggles, GroupImport
    };
}
