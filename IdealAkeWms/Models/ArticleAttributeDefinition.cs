using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models;

public enum AttributeType
{
    Boolean = 0,
    Dropdown = 1,
    // Text = Freitext-Merkmal. FaAttribute*: manuell (FaAttributeValue.TextValue). ArticleAttribute: NUR Sage-gefuehrt
    // (SyncSource = "Sage", Benutzerfelder aus dem SData-Artikel-Abgleich, ArticleAttributeValue.TextValue) —
    // die ArticleAttribute-UI bietet Text bei der Anlage NICHT an.
    Text = 2
}

public class ArticleAttributeDefinition : AuditableEntity
{
    [Required(ErrorMessage = "Name ist erforderlich")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Typ")]
    public AttributeType AttributeType { get; set; }

    [Display(Name = "Reihenfolge")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktiv")]
    public bool IsActive { get; set; } = true;

    /// <summary>"Sage" = vom Artikel-Abgleich (SData, Benutzerfeld <see cref="SyncFieldName"/>) angelegt und gefuehrt: Name, Typ und
    /// Werte sind in der Pflege gesperrt (Spec 2026-11-06-sage-sdata-stammdaten-sync E3b). null = manuelles Merkmal.</summary>
    [StringLength(50)]
    public string? SyncSource { get; set; }

    public const string SyncSourceSage = "Sage";

    public bool IsSageSynced => SyncSource == SyncSourceSage;

    [StringLength(200)]
    public string? SyncFieldName { get; set; }

    public ICollection<ArticleAttributeOption> Options { get; set; } = new List<ArticleAttributeOption>();
    public ICollection<ArticleAttributeValue> Values { get; set; } = new List<ArticleAttributeValue>();
}
