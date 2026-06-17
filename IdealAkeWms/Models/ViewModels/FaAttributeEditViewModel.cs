namespace IdealAkeWms.Models.ViewModels;

/// <summary>Edit-Seite eines FA-Merkmals: Definition + Optionen + Arbeitsgang-Zuordnung.</summary>
public class FaAttributeEditViewModel
{
    public FaAttributeDefinition Definition { get; set; } = null!;

    /// <summary>Aktive Arbeitsgaenge fuer die Checkbox-Liste (POST-Wert kommt als int[] workStepIds).</summary>
    public List<WorkStep> AllWorkSteps { get; set; } = new();

    public List<int> SelectedWorkStepIds { get; set; } = new();
}
