using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

public interface IBdeBookingService
{
    Task<BdeBookingResult> StartSetupAsync(int operatorId, int workOperationId, int workplaceId, int terminalId);
    Task<BdeBookingResult> StartProductionAsync(int operatorId, int workOperationId, int workplaceId, int terminalId);
    Task<BdeBookingResult> StartActivityAsync(int operatorId, int activityId, int workplaceId, int terminalId);
    Task<BdeBookingResult> PauseAsync(int bookingId, decimal? goodQty = null, decimal? scrapQty = null);
    Task<BdeBookingResult> ResumeAsync(int pausedBookingId, int operatorId, BdeBookingType resumeAs, int workplaceId, int terminalId);
    Task<BdeBookingResult> FinishAsync(int bookingId, decimal? goodQty = null, decimal? scrapQty = null);
    Task<BdeBookingResult> ReportPartialQuantityAsync(int bookingId, decimal goodQty, decimal scrapQty);
    Task<CloseOthersResult> CloseOtherBookingsOnWorkOperationAsync(int workOperationId, int exceptOperatorId);
}

public record CloseOthersResult(int ClosedCount);
