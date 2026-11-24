using System.ComponentModel.DataAnnotations;
using DriveNow.Models;

namespace DriveNow.Api;

/// <summary>Result of a local API operation: an HTTP status code and an optional response body.</summary>
public sealed record ApiOutcome(int StatusCode, object? Body = null);

/// <summary>
/// Thread-safe in-memory storage for the optional local REST API. It mirrors the
/// egolance.com contract and enforces all business rules from the brief on the server side.
/// </summary>
public class InMemoryDataStore
{
    private readonly Lock _lock = new();
    private readonly List<Car> _cars;
    private readonly List<Booking> _bookings;
    private readonly List<Location> _locations;

    public InMemoryDataStore()
    {
        var today = DateTime.Today;

        _cars =
        [
            new() { Id = 1, RegistrationNumber = "231-D-10452", Make = "Toyota", Model = "Corolla", Year = 2023, DailyRate = 45, Available = true, FuelType = "Hybrid" },
            new() { Id = 2, RegistrationNumber = "221-D-88213", Make = "Volkswagen", Model = "Golf", Year = 2022, DailyRate = 50, Available = true, FuelType = "Petrol" },
            new() { Id = 3, RegistrationNumber = "242-D-30077", Make = "Tesla", Model = "Model 3", Year = 2024, DailyRate = 95, Available = true, FuelType = "Electric" },
            new() { Id = 4, RegistrationNumber = "201-C-55120", Make = "Ford", Model = "Focus", Year = 2020, DailyRate = 38, Available = true, FuelType = "Diesel" },
            new() { Id = 5, RegistrationNumber = "232-G-11904", Make = "Hyundai", Model = "Kona", Year = 2023, DailyRate = 60, Available = true, FuelType = "Electric" },
            new() { Id = 6, RegistrationNumber = "191-D-70345", Make = "Skoda", Model = "Octavia", Year = 2019, DailyRate = 40, Available = false, FuelType = "Diesel" },
            new() { Id = 7, RegistrationNumber = "241-KY-2093", Make = "BMW", Model = "X3", Year = 2024, DailyRate = 120, Available = true, FuelType = "Hybrid" },
            new() { Id = 8, RegistrationNumber = "222-L-40418", Make = "Renault", Model = "Clio", Year = 2022, DailyRate = 35, Available = true, FuelType = "Petrol" }
        ];

        _bookings =
        [
            new() { Id = 1, CarId = 1, LocationId = 1, CustomerName = "Aoife Murphy", StartDate = today.AddDays(-2), EndDate = today.AddDays(3) },
            new() { Id = 2, CarId = 3, LocationId = 2, CustomerName = "Liam O'Brien", StartDate = today, EndDate = today.AddDays(2) },
            new() { Id = 3, CarId = 2, LocationId = 3, CustomerName = "Sophie Walsh", StartDate = today.AddDays(5), EndDate = today.AddDays(9) },
            new() { Id = 4, CarId = 4, LocationId = 5, CustomerName = "Jack Byrne", StartDate = today.AddDays(-10), EndDate = today.AddDays(-6) }
        ];

        _locations =
        [
            new() { Id = 1, Name = "Dublin Airport", City = "Dublin", County = "Dublin", Address = "Terminal 2 Car Rental Centre, Dublin Airport, Co. Dublin", Phone = "+353 1 555 0101" },
            new() { Id = 2, Name = "Dublin City Centre", City = "Dublin", County = "Dublin", Address = "12 O'Connell Street Upper, Dublin 1", Phone = "+353 1 555 0102" },
            new() { Id = 3, Name = "Cork Airport", City = "Cork", County = "Cork", Address = "Arrivals Hall, Cork Airport, Co. Cork", Phone = "+353 21 555 0103" },
            new() { Id = 4, Name = "Shannon Airport", City = "Shannon", County = "Clare", Address = "Arrivals Hall, Shannon Airport, Co. Clare", Phone = "+353 61 555 0104" },
            new() { Id = 5, Name = "Galway City", City = "Galway", County = "Galway", Address = "8 Eyre Square, Galway", Phone = "+353 91 555 0105" },
            new() { Id = 6, Name = "Waterford City", City = "Waterford", County = "Waterford", Address = "22 The Quay, Waterford", Phone = "+353 51 555 0106" }
        ];

        RefreshAvailability();
    }

    // ----------------------------------------------------------------- Cars

    public List<Car> GetCars()
    {
        lock (_lock)
        {
            RefreshAvailability();
            return _cars.Select(c => c.Clone()).ToList();
        }
    }

    public Car? GetCar(int id)
    {
        lock (_lock)
        {
            RefreshAvailability();
            return _cars.FirstOrDefault(c => c.Id == id)?.Clone();
        }
    }

    public ApiOutcome AddCar(Car car)
    {
        lock (_lock)
        {
            if (Validate(car) is { } invalid) return invalid;
            if (RegistrationExists(car)) return Conflict("A car with this registration number already exists.");

            car.Id = _cars.Count == 0 ? 1 : _cars.Max(c => c.Id) + 1;
            car.RegistrationNumber = car.RegistrationNumber.Trim().ToUpperInvariant();
            _cars.Add(car.Clone());
            return new ApiOutcome(StatusCodes.Status201Created, car);
        }
    }

    public ApiOutcome UpdateCar(int id, Car car)
    {
        lock (_lock)
        {
            var existing = _cars.FirstOrDefault(c => c.Id == id);
            if (existing is null) return NotFound("Car not found.");
            car.Id = id;
            if (Validate(car) is { } invalid) return invalid;
            if (RegistrationExists(car)) return Conflict("A car with this registration number already exists.");

            existing.RegistrationNumber = car.RegistrationNumber.Trim().ToUpperInvariant();
            existing.Make = car.Make;
            existing.Model = car.Model;
            existing.Year = car.Year;
            existing.DailyRate = car.DailyRate;
            existing.FuelType = car.FuelType;
            // A car with an active booking must stay unavailable.
            existing.Available = car.Available && !HasActiveBooking(id);
            return new ApiOutcome(StatusCodes.Status200OK, existing.Clone());
        }
    }

    public ApiOutcome DeleteCar(int id)
    {
        lock (_lock)
        {
            var existing = _cars.FirstOrDefault(c => c.Id == id);
            if (existing is null) return NotFound("Car not found.");
            if (_bookings.Any(b => b.CarId == id && BookingRules.IsCurrentOrUpcoming(b, DateTime.Now)))
            {
                return Conflict("This car has active bookings and cannot be deleted.");
            }

            _cars.Remove(existing);
            _bookings.RemoveAll(b => b.CarId == id); // remove its completed booking history
            return new ApiOutcome(StatusCodes.Status204NoContent);
        }
    }

    // ------------------------------------------------------------- Bookings

    public List<Booking> GetBookings()
    {
        lock (_lock) return _bookings.Select(CloneBooking).ToList();
    }

    public Booking? GetBooking(int id)
    {
        lock (_lock)
        {
            var booking = _bookings.FirstOrDefault(b => b.Id == id);
            return booking is null ? null : CloneBooking(booking);
        }
    }

    public ApiOutcome AddBooking(Booking booking)
    {
        lock (_lock)
        {
            if (Validate(booking) is { } invalid) return invalid;

            var car = _cars.FirstOrDefault(c => c.Id == booking.CarId);
            if (car is null) return BadRequest("CarId", "The selected car does not exist.");
            if (_locations.All(l => l.Id != booking.LocationId)) return BadRequest("LocationId", "The selected location does not exist.");

            if (_bookings.Any(b => b.CarId == booking.CarId && BookingRules.Overlaps(b, booking)))
            {
                return Conflict("This car is already booked for the selected dates.");
            }

            booking.Id = _bookings.Count == 0 ? 1 : _bookings.Max(b => b.Id) + 1;
            booking.CustomerName = booking.CustomerName.Trim();
            _bookings.Add(CloneBooking(booking));
            RefreshAvailability();
            return new ApiOutcome(StatusCodes.Status201Created, booking);
        }
    }

    public ApiOutcome DeleteBooking(int id)
    {
        lock (_lock)
        {
            var booking = _bookings.FirstOrDefault(b => b.Id == id);
            if (booking is null) return NotFound("Booking not found.");

            _bookings.Remove(booking);
            // Car becomes available again once it has no active bookings.
            var car = _cars.FirstOrDefault(c => c.Id == booking.CarId);
            if (car is not null && !car.Available && !HasActiveBooking(car.Id))
            {
                car.Available = true;
            }
            return new ApiOutcome(StatusCodes.Status204NoContent);
        }
    }

    // ------------------------------------------------------------ Locations

    public List<Location> GetLocations()
    {
        lock (_lock) return _locations.ToList();
    }

    public Location? GetLocation(int id)
    {
        lock (_lock) return _locations.FirstOrDefault(l => l.Id == id);
    }

    // -------------------------------------------------------------- Helpers

    private bool HasActiveBooking(int carId) =>
        _bookings.Any(b => b.CarId == carId && BookingRules.IsActive(b, DateTime.Now));

    /// <summary>Cars are automatically unavailable while they have an active booking.</summary>
    private void RefreshAvailability()
    {
        foreach (var car in _cars.Where(c => c.Available && HasActiveBooking(c.Id)))
        {
            car.Available = false;
        }
    }

    private bool RegistrationExists(Car car) =>
        _cars.Any(c => c.Id != car.Id &&
                       string.Equals(c.RegistrationNumber.Trim(), car.RegistrationNumber.Trim(), StringComparison.OrdinalIgnoreCase));

    private static ApiOutcome? Validate(object model)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true))
        {
            return null;
        }

        var errors = results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty).Select(m => (Member: m, r.ErrorMessage)))
            .GroupBy(x => x.Member)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage ?? "Invalid value.").ToArray());

        return new ApiOutcome(StatusCodes.Status400BadRequest, new { title = "Validation failed.", status = 400, errors });
    }

    private static ApiOutcome BadRequest(string field, string message) =>
        new(StatusCodes.Status400BadRequest, new { title = message, status = 400, errors = new Dictionary<string, string[]> { [field] = [message] } });

    private static ApiOutcome Conflict(string message) => new(StatusCodes.Status409Conflict, new { message, status = 409 });

    private static ApiOutcome NotFound(string message) => new(StatusCodes.Status404NotFound, new { message, status = 404 });

    private static Booking CloneBooking(Booking b) => new()
    {
        Id = b.Id, CarId = b.CarId, LocationId = b.LocationId, CustomerName = b.CustomerName, StartDate = b.StartDate, EndDate = b.EndDate
    };
}
