using IdealAkeWms.Services;

namespace IdealAkeWms.Models.ViewModels;

public class AdUserCreateViewModel
{
    public List<AdUserCandidate> Candidates { get; set; } = new();
    public bool AdQueryFailed { get; set; }
    public List<RoleCheckboxItem> AvailableRoles { get; set; } = new();
    public string SamAccountName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public List<int> SelectedRoleIds { get; set; } = new();
}
