using System.ComponentModel.DataAnnotations;
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public class BdeBookingEditViewModel
{
    public int Id { get; set; }
    [Display(Name = "Operator")] public int BdeOperatorId { get; set; }
    [Display(Name = "Werkbank")] public int ProductionWorkplaceId { get; set; }
    [Display(Name = "Start")] public DateTime StartedAt { get; set; }
    [Display(Name = "Ende")] public DateTime? EndedAt { get; set; }
    [Display(Name = "Status")] public BdeBookingStatus Status { get; set; }
    public List<BdeBookingQuantityEditRow> Quantities { get; set; } = new();
}

public class BdeBookingQuantityEditRow
{
    public int Id { get; set; }
    // v1.64.0 FA-5.3: negative Mengen auch in der Admin-Korrektur abweisen (Controller prueft ModelState).
    [Range(0, double.MaxValue, ErrorMessage = "Menge darf nicht negativ sein.")] public decimal GoodQuantity { get; set; }
    [Range(0, double.MaxValue, ErrorMessage = "Menge darf nicht negativ sein.")] public decimal ScrapQuantity { get; set; }
    public bool IsFinal { get; set; }
    public DateTime ReportedAt { get; set; }
    public bool Delete { get; set; }
}
