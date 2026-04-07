namespace IdealAkeWms.Models.ViewModels;

public class ArticleIndexViewModel
{
    public List<Article> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public string? Search { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Active attribute definitions (for dynamic column headers).</summary>
    public List<ArticleAttributeDefinition> AttributeDefinitions { get; set; } = new();

    /// <summary>Attribute values keyed by ArticleId, then by DefinitionId.</summary>
    public Dictionary<int, List<ArticleAttributeValue>> AttributeValuesByArticle { get; set; } = new();
}
