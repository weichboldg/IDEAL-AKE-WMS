using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class ArticleEditViewModel
{
    public Article Article { get; set; } = null!;
    public List<ArticleCategory> Categories { get; set; } = new();
    public List<AttributeEditItem> Attributes { get; set; } = new();
    public List<StorageLocation> StorageLocations { get; set; } = new();
    public bool IsPrimarySageControlled { get; set; }
}

public class AttributeEditItem
{
    public int DefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AttributeType AttributeType { get; set; }
    public bool? BooleanValue { get; set; }
    public int? SelectedOptionId { get; set; }
    /// <summary>Nur Anzeige: Sage-gefuehrtes Text-Merkmal (E3b), wird beim Speichern ignoriert.</summary>
    public string? TextValue { get; set; }
    public bool IsSageSynced { get; set; }
    public List<ArticleAttributeOption> Options { get; set; } = new();
}
