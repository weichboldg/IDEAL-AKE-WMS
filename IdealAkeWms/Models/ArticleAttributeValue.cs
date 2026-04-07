namespace IdealAkeWms.Models;

public class ArticleAttributeValue : AuditableEntity
{
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public int ArticleAttributeDefinitionId { get; set; }
    public ArticleAttributeDefinition ArticleAttributeDefinition { get; set; } = null!;

    public bool? BooleanValue { get; set; }

    public int? SelectedOptionId { get; set; }
    public ArticleAttributeOption? SelectedOption { get; set; }
}
