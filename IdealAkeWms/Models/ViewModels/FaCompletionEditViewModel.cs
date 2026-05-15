using System.ComponentModel.DataAnnotations;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>
/// FaCompletion/Edit.cshtml — Phase 4 Top-Level-VM fuer eine FA-Vervollstaendigung.
/// Enthaelt FA-Master (read-only) und 5 <see cref="AssemblyGroupTabViewModel"/>
/// (eine pro VK/VL/VE/VT/VA).
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

    /// <summary>Aktiv ausgewaehlter Tab (VK/VL/VE/VT/VA). Default "VK".</summary>
    public string ActiveTab { get; set; } = "VK";

    public List<AssemblyGroupTabViewModel> Tabs { get; set; } = new();
}

/// <summary>
/// Ein Tab pro AssemblyGroup (VK/VL/VE/VT/VA). Enthaelt Status der Gruppe und Liste
/// ihrer Auspraegungen.
/// </summary>
public class AssemblyGroupTabViewModel
{
    public int AssemblyGroupId { get; set; }
    public string GroupKey { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public bool IsApplicable { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }

    public List<AssemblyGroupSpecFormModel> Specs { get; set; } = new();
}

/// <summary>
/// Display-orientierte Sicht einer einzelnen Spec-Zeile innerhalb eines Tabs.
/// Wird sowohl fuer die Read-Only-Anzeige als auch fuer Add/Edit-Form-Binding
/// (POST-Body fuer AddSpec/EditSpec) genutzt.
/// </summary>
public class AssemblyGroupSpecItemViewModel
{
    public int Id { get; set; }
    public int AssemblyGroupId { get; set; }
    public int? ArticleId { get; set; }
    public string? ArticleNumber { get; set; }
    public string? ArticleDescription { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Form-Binding-Model fuer Add/Edit-POST einer Spec.
/// Validation-Attribute spiegeln <see cref="Models.ProductionOrderAssemblyGroupSpec"/>.
/// </summary>
public class AssemblyGroupSpecFormModel
{
    public int Id { get; set; }

    [Required]
    public int AssemblyGroupId { get; set; }

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
