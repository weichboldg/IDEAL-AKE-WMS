namespace IdealAkeWms.Models.Standort;

public class StandortEinstellungenViewModel
{
    /// <summary>Editierbare Felder je Gruppe, in Katalog-Reihenfolge.</summary>
    public List<StandortFieldGroup> Groups { get; set; } = new();

    /// <summary>Read-only Statusanzeige des Master-Einwegtors (Teil 7).</summary>
    public MasterStatusView Master { get; set; } = new();
}

public class StandortFieldGroup
{
    public string Name { get; set; } = string.Empty;
    public List<StandortFieldView> Fields { get; set; } = new();
}

public class StandortFieldView
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public StandortFieldType Type { get; set; }
    public string? Value { get; set; }
    public bool BoolValue { get; set; }
    public string? Hint { get; set; }
}

/// <summary>
/// Read-only Sicht auf den Master-Einwegtor. <see cref="Enabled"/> = aktueller Key-Wert,
/// <see cref="Locked"/> = hierarchische Daten vorhanden (nicht mehr über die App rücknehmbar),
/// <see cref="AsOfUtc"/> = Stand der gecachten Sperr-Prüfung.
/// </summary>
public class MasterStatusView
{
    public bool Enabled { get; set; }
    public bool Locked { get; set; }
    public DateTimeOffset? AsOfUtc { get; set; }
}
