using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ArticleAttributeOption
{
    public int Id { get; set; }

    public int ArticleAttributeDefinitionId { get; set; }
    public ArticleAttributeDefinition ArticleAttributeDefinition { get; set; } = null!;

    [Required(ErrorMessage = "Wert ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Wert")]
    public string Value { get; set; } = string.Empty;

    [Display(Name = "Reihenfolge")]
    public int SortOrder { get; set; }
}
