using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public class ArticleCategory : AuditableEntity
{
    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Beschreibung")]
    public string? Description { get; set; }

    [Display(Name = "OSEON Typ")]
    public int? OseonTyp { get; set; }

    [StringLength(50)]
    [Display(Name = "Quelle")]
    public string? Source { get; set; }

    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
