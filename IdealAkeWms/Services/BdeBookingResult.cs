using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

public enum BdeBookingOutcome
{
    Success,
    CollisionOtherOperator,
    QuantityRequired,
    InvalidState,
    NotFound
}

public class BdeBookingResult
{
    public BdeBookingOutcome Outcome { get; init; }
    public BdeBooking? Booking { get; init; }
    public BdeBooking? CollidingBooking { get; init; }
    public string? Message { get; init; }

    public static BdeBookingResult Success(BdeBooking b) => new() { Outcome = BdeBookingOutcome.Success, Booking = b };
    public static BdeBookingResult Collision(BdeBooking colliding) => new() { Outcome = BdeBookingOutcome.CollisionOtherOperator, CollidingBooking = colliding };
    public static BdeBookingResult QuantityRequired(BdeBooking current) => new() { Outcome = BdeBookingOutcome.QuantityRequired, Booking = current };
    public static BdeBookingResult Invalid(string msg) => new() { Outcome = BdeBookingOutcome.InvalidState, Message = msg };
    public static BdeBookingResult NotFound() => new() { Outcome = BdeBookingOutcome.NotFound };
}
