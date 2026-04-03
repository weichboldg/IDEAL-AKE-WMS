using System.ComponentModel.DataAnnotations;
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class OrderRecipientGroupViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Beschreibung")]
    public string? Description { get; set; }

    public List<OrderRecipientEditModel> Recipients { get; set; } = new();
}

public class OrderRecipientEditModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-Mail ist erforderlich")]
    [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse")]
    [StringLength(300)]
    [Display(Name = "E-Mail")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;
}

public class ArticleGroupMappingViewModel
{
    public string ArticleGroup { get; set; } = string.Empty;
    public List<int> SelectedGroupIds { get; set; } = new();
}

public class ArticleGroupMappingsPageViewModel
{
    public List<ArticleGroupMappingViewModel> Mappings { get; set; } = new();
    public List<OrderRecipientGroup> AvailableGroups { get; set; } = new();
}
