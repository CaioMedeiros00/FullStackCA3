using DriveNow.Models;

namespace DriveNow.Api;

/// <summary>
/// Optional local REST API (Minimal APIs) with the same routes as https://egolance.com/api.
/// Useful for offline development/demo: set ApiSettings:UseLocalApi = true.
/// </summary>
public static class LocalApiEndpoints
{
    public static WebApplication MapDriveNowLocalApi(this WebApplication app)
    {
        var api = app.MapGroup("/api").WithTags("DriveNow Local API");

        // Cars - full CRUD
        api.MapGet("/cars", (InMemoryDataStore store) => Results.Ok(store.GetCars()));
        api.MapGet("/cars/{id:int}", (int id, InMemoryDataStore store) =>
            store.GetCar(id) is { } car ? Results.Ok(car) : Results.NotFound(new { message = "Car not found." }));
        api.MapPost("/cars", (Car car, InMemoryDataStore store) => ToResult(store.AddCar(car)));
        api.MapPut("/cars/{id:int}", (int id, Car car, InMemoryDataStore store) => ToResult(store.UpdateCar(id, car)));
        api.MapDelete("/cars/{id:int}", (int id, InMemoryDataStore store) => ToResult(store.DeleteCar(id)));

        // Bookings - list, details, create, delete
        api.MapGet("/bookings", (InMemoryDataStore store) => Results.Ok(store.GetBookings()));
        api.MapGet("/bookings/{id:int}", (int id, InMemoryDataStore store) =>
            store.GetBooking(id) is { } booking ? Results.Ok(booking) : Results.NotFound(new { message = "Booking not found." }));
        api.MapPost("/bookings", (Booking booking, InMemoryDataStore store) => ToResult(store.AddBooking(booking)));
        api.MapDelete("/bookings/{id:int}", (int id, InMemoryDataStore store) => ToResult(store.DeleteBooking(id)));

        // Locations - read only
        api.MapGet("/locations", (InMemoryDataStore store) => Results.Ok(store.GetLocations()));
        api.MapGet("/locations/{id:int}", (int id, InMemoryDataStore store) =>
            store.GetLocation(id) is { } location ? Results.Ok(location) : Results.NotFound(new { message = "Location not found." }));

        return app;
    }

    private static IResult ToResult(ApiOutcome outcome) =>
        outcome.Body is null ? Results.StatusCode(outcome.StatusCode) : Results.Json(outcome.Body, statusCode: outcome.StatusCode);
}
