using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public record WarehouseRequisitionListItemViewModel(
    int Id,
    string WorkplaceName,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    int ItemCount,
    WarehouseRequisitionStatus Status);
