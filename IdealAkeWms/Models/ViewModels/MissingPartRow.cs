using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

public record MissingPartRow(
    int RequisitionId,
    int ItemId,
    int Position,
    string WorkplaceName,
    string ArticleNumber,
    string ArticleDescription,
    decimal QuantityRequested,
    decimal QuantityPicked,
    decimal QuantityMissing,
    string? Unit,
    string? Note,
    string CreatedBy,
    DateTime? ClosedAt,
    ShortageStatus Status,
    string? NoteEinkauf);
