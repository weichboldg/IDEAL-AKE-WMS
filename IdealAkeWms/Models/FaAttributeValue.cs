namespace IdealAkeWms.Models;

public class FaAttributeValue : AuditableEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int FaAttributeDefinitionId { get; set; }
    public FaAttributeDefinition Definition { get; set; } = null!;

    public int? SelectedOptionId { get; set; }
    public FaAttributeOption? SelectedOption { get; set; }

    public bool? BooleanValue { get; set; }

    /// <summary>Freitext-Wert fuer Merkmale vom Typ <see cref="AttributeType.Text"/> (NULL = leer).</summary>
    public string? TextValue { get; set; }
}
