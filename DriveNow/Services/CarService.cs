using DriveNow.Models;

namespace DriveNow.Services;

/// <summary>Talks to the Cars API (full CRUD).</summary>
public class CarService(HttpClient http, ILogger<CarService> logger) : ApiServiceBase(http, logger)
{
    private const string Endpoint = "cars";

    public Task<ServiceResult<List<Car>>> GetCarsAsync() => GetAsync<List<Car>>(Endpoint);

    public Task<ServiceResult<Car>> GetCarAsync(int id) => GetAsync<Car>($"{Endpoint}/{id}");

    public async Task<ServiceResult<Car>> CreateCarAsync(Car car)
    {
        car.Id = 0;
        var result = await PostAsync(Endpoint, car);
        return result.IsConflict
            ? ServiceResult<Car>.Fail(result.ErrorMessage ?? "A car with this registration number already exists.", 409)
            : result;
    }

    public Task<ServiceResult<Car>> UpdateCarAsync(Car car) => PutAsync($"{Endpoint}/{car.Id}", car);

    public async Task<ServiceResult> DeleteCarAsync(int id)
    {
        var result = await DeleteAsync($"{Endpoint}/{id}");
        if (result.IsConflict)
        {
            // Business rule: cars with active bookings cannot be deleted (API returns 409).
            return ServiceResult.Fail(
                "This car has active bookings and cannot be deleted. Cancel its bookings first.", 409);
        }
        return result;
    }

    public async Task<ServiceResult> SetAvailabilityAsync(Car car, bool available)
    {
        if (car.Available == available)
        {
            return ServiceResult.Ok();
        }

        var updated = car.Clone();
        updated.Available = available;
        var result = await UpdateCarAsync(updated);
        if (result.Success)
        {
            car.Available = available;
        }
        return result;
    }

    /// <summary>Client-side check for the "RegistrationNumber must be unique" rule.</summary>
    public static bool IsRegistrationTaken(IEnumerable<Car> cars, Car candidate) =>
        cars.Any(c => c.Id != candidate.Id &&
                      string.Equals(c.RegistrationNumber.Trim(), candidate.RegistrationNumber.Trim(),
                          StringComparison.OrdinalIgnoreCase));
}
