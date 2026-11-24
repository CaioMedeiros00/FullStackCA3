namespace DriveNow.Models;

public enum BookingStatus
{
    Upcoming,
    Active,
    Completed
}

/// <summary>Shared booking rules used by the UI, the services and the local API.</summary>
public static class BookingRules
{
    /// <summary>A booking is active while "now" is inside its booking period.</summary>
    public static bool IsActive(Booking booking, DateTime now) =>
        booking.StartDate <= now && booking.EndDate > now;

    /// <summary>Active or still to come - i.e. it has not finished yet.</summary>
    public static bool IsCurrentOrUpcoming(Booking booking, DateTime now) => booking.EndDate > now;

    public static BookingStatus GetStatus(Booking booking, DateTime now) =>
        booking.EndDate <= now ? BookingStatus.Completed
        : booking.StartDate > now ? BookingStatus.Upcoming
        : BookingStatus.Active;

    /// <summary>Two periods overlap when each one starts before the other ends.</summary>
    public static bool Overlaps(Booking a, Booking b) =>
        a.StartDate < b.EndDate && b.StartDate < a.EndDate;

    public static int RentalDays(Booking booking) =>
        Math.Max(1, (int)Math.Ceiling((booking.EndDate - booking.StartDate).TotalDays));

    public static decimal EstimatedCost(Booking booking, Car? car) =>
        car is null ? 0 : RentalDays(booking) * car.DailyRate;
}
