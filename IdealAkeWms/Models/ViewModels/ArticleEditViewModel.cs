namespace IdealAkeWms.Models.ViewModels;

public class ArticleEditViewModel
{
    public Article Article { get; set; } = null!;
    public List<ArticleCategory> Categories { get; set; } = new();
    public List<AttributeEditItem> Attributes { get; set; } = new();
}

public class AttributeEditItem
{
    public int DefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AttributeType AttributeType { get; set; }
    public bool? BooleanValue { get; set; }
    public int? SelectedOptionId { get; set; }
    public List<ArticleAttributeOption> Options { get; set; } = new();
}
