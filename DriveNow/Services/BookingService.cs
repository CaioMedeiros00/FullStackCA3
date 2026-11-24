using DriveNow.Models;

namespace DriveNow.Services;

/// <summary>
/// Talks to the Bookings API and enforces the booking business rules that involve
/// cars (existing car, no overlaps, automatic availability updates).
/// </summary>
public class BookingService(HttpClient http, CarService carService, ILogger<BookingService> logger)
    : ApiServiceBase(http, logger)
{
    private const string Endpoint = "bookings";

    public Task<ServiceResult<List<Booking>>> GetBookingsAsync() => GetAsync<List<Booking>>(Endpoint);

    public Task<ServiceResult<Booking>> GetBookingAsync(int id) => GetAsync<Booking>($"{Endpoint}/{id}");

    public async Task<ServiceResult<Booking>> CreateBookingAsync(Booking booking)
    {
        booking.Id = 0;
        booking.CustomerName = booking.CustomerName.Trim();

        // Rule: CarId must reference an existing car.
        var carResult = await carService.GetCarAsync(booking.CarId);
        if (!carResult.Success || carResult.Data is null)
        {
            return ServiceResult<Booking>.Fail(carResult.IsNotFound
                ? "The selected car no longer exists."
                : carResult.ErrorMessage ?? "Could not verify the selected car.", carResult.StatusCode);
        }
        var car = carResult.Data;

        // Rule: a vehicle cannot have overlapping bookings (checked here AND by the API -> 409).
        var existing = await GetBookingsAsync();
        if (existing.Success && existing.Data is not null)
        {
            var clash = existing.Data.FirstOrDefault(b => b.CarId == booking.CarId && BookingRules.Overlaps(b, booking));
            if (clash is not null)
            {
                return ServiceResult<Booking>.Fail(
                    $"{car.Make} {car.Model} is already booked from {clash.StartDate:dd MMM yyyy} to {clash.EndDate:dd MMM yyyy}. Please choose different dates.",
                    409);
            }
        }

        var result = await PostAsync(Endpoint, booking);
        if (!result.Success)
        {
            return result.IsConflict
                ? ServiceResult<Booking>.Fail(result.ErrorMessage ?? "This car is already booked for the selected dates.", 409)
                : result;
        }

        // Rule: creating a booking marks the car unavailable during the booking period.
        if (BookingRules.IsActive(booking, DateTime.Now))
        {
            var update = await carService.SetAvailabilityAsync(car, false);
            if (!update.Success)
            {
                Logger.LogWarning("Booking created but car {CarId} could not be marked unavailable: {Error}",
                    car.Id, update.ErrorMessage);
            }
        }

        return result;
    }

    public async Task<ServiceResult> DeleteBookingAsync(Booking booking)
    {
        var result = await DeleteAsync($"{Endpoint}/{booking.Id}");
        if (!result.Success)
        {
            return result;
        }

        // Rule: once all active bookings have ended or been cancelled the car becomes available again.
        var carResult = await carService.GetCarAsync(booking.CarId);
        var remaining = await GetBookingsAsync();
        if (carResult.Data is { Available: false } car && remaining.Success && remaining.Data is not null)
        {
            var stillActive = remaining.Data.Any(b => b.CarId == car.Id && BookingRules.IsActive(b, DateTime.Now));
            if (!stillActive)
            {
                await carService.SetAvailabilityAsync(car, true);
            }
        }

        return ServiceResult.Ok();
    }

    /// <summary>
    /// Makes sure every car that currently has an active booking is flagged as unavailable
    /// (e.g. a booking created for a future date that has now started).
    /// </summary>
    public async Task<int> SyncCarAvailabilityAsync(IEnumerable<Car> cars, IEnumerable<Booking> bookings)
    {
        var now = DateTime.Now;
        var updated = 0;
        foreach (var car in cars.Where(c => c.Available))
        {
            if (bookings.Any(b => b.CarId == car.Id && BookingRules.IsActive(b, now)))
            {
                var result = await carService.SetAvailabilityAsync(car, false);
                if (result.Success)
                {
                    updated++;
                }
            }
        }
        return updated;
    }
}
