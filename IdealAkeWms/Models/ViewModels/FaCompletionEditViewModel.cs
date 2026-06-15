using System.ComponentModel.DataAnnotations;
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// FaCompletion/Edit.cshtml — Top-Level-VM fuer eine FA-Vervollstaendigung (v1.22.0).
/// Enthaelt FA-Master (read-only), Werkbank-Zuordnung und einen Tab je aktivem
/// <see cref="FaWorkStep"/> (statt der frueheren 5 fixen AssemblyGroups).
/// </summary>
public class FaCompletionEditViewModel
{
    public int ProductionOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Customer { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Description1 { get; set; }
    public string? Description2 { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool IsDone { get; set; }

    /// <summary>Aktuell zugewiesene Werkbank des FA (null = keine).</summary>
    public int? ProductionWorkplaceId { get; set; }

    /// <summary>Alle Werkbaenke fuer das Zuweisungs-Dropdown.</summary>
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();

    /// <summary>Aktive Katalog-Arbeitsgaenge, die am FA noch NICHT aktiv sind ("AG hinzufuegen").</summary>
    public List<WorkStep> AvailableWorkSteps { get; set; } = new();

    /// <summary>Aktiv ausgewaehlter Tab (WorkStep.Code). Default = erster aktiver Tab.</summary>
    public string ActiveTab { get; set; } = string.Empty;

    public List<FaWorkStepTabViewModel> Tabs { get; set; } = new();
}

/// <summary>
/// Ein Tab pro aktivem FaWorkStep. Enthaelt Status, zugeordnete Merkmale und Specs.
/// </summary>
public class FaWorkStepTabViewModel
{
    public int FaWorkStepId { get; set; }
    public int WorkStepId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsSpecComplete { get; set; }
    public DateTime? SpecCompletedAt { get; set; }
    public string? SpecCompletedBy { get; set; }

    /// <summary>Dem Arbeitsgang zugeordnete aktive Merkmale inkl. aktuellem Wert des FA.</summary>
    public List<FaAttributeFieldViewModel> Attributes { get; set; } = new();

    public List<FaWorkStepSpecFormModel> Specs { get; set; } = new();
}

/// <summary>
/// Ein Merkmal-Eingabefeld innerhalb eines Tabs (Definition + Optionen + aktueller Wert).
/// </summary>
public class FaAttributeFieldViewModel
{
    public int DefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AttributeType AttributeType { get; set; }
    public List<FaAttributeOptionViewModel> Options { get; set; } = new();
    public int? SelectedOptionId { get; set; }
    public bool? BooleanValue { get; set; }
}

public class FaAttributeOptionViewModel
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// Form-Binding-Model fuer Add/Edit-POST einer Spec (Auspraegung).
/// Validation-Attribute spiegeln <see cref="Models.FaWorkStepSpec"/>.
/// Wird sowohl fuer die Read-Only-Anzeige als auch fuer Add/Edit-Form-Binding genutzt.
/// </summary>
public class FaWorkStepSpecFormModel
{
    public int Id { get; set; }

    [Required]
    public int FaWorkStepId { get; set; }

    public int? ArticleId { get; set; }

    /// <summary>
    /// Display-Text fuer Article-Select2 (z.B. "12345 - Kuehlaggregat"). Nicht persistiert
    /// — wird beim Edit-GET aus <see cref="Models.Article"/> abgeleitet.
    /// </summary>
    public string? ArticleText { get; set; }

    [Required(ErrorMessage = "Beschreibung ist erforderlich.")]
    [StringLength(500, ErrorMessage = "Beschreibung max. 500 Zeichen.")]
    public string Description { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string? Notes { get; set; }

    public int SortOrder { get; set; }
}
