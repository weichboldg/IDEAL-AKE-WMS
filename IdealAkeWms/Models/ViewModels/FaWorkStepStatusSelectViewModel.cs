namespace IdealAkeWms.Models.ViewModels;

/// <summary>VM fuer das gemeinsame 3-State-Status-Dropdown (_FaWorkStepStatusSelect).</summary>
public class FaWorkStepStatusSelectViewModel
{
    public int FaWorkStepId { get; set; }
    public Models.FaWorkStepStatus Status { get; set; }
    public bool Disabled { get; set; }
}
