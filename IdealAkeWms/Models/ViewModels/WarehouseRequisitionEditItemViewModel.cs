namespace IdealAkeWms.Models.ViewModels;

public record WarehouseRequisitionEditItemViewModel(
    int Id,
    int Position,
    string ArticleNumber,
    string ArticleDescription,
    string? Unit,
    decimal QuantityRequested);
