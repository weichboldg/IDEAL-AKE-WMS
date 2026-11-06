namespace IdealAkeWms.Models;

public class ArticleAttributeValue : AuditableEntity
{
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public int ArticleAttributeDefinitionId { get; set; }
    public ArticleAttributeDefinition ArticleAttributeDefinition { get; set; } = null!;

    public bool? BooleanValue { get; set; }

    /// <summary>
    /// Freitext (Typ Text). Heute nur von Sage-gefuehrten Merkmalen (Benutzerfelder, SData-Artikel-Abgleich, Spec
    /// 2026-11-06-sage-sdata-stammdaten-sync E3b) geschrieben; Laenge wie <c>FaAttributeValue.TextValue</c>.
    /// </summary>
    public string? TextValue { get; set; }
    public const int TextValueMaxLength = 1000;

    public int? SelectedOptionId { get; set; }
    public ArticleAttributeOption? SelectedOption { get; set; }
}
