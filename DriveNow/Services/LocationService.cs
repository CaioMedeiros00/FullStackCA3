using DriveNow.Models;

namespace DriveNow.Services;

/// <summary>Talks to the Locations API. Read-only by design: no create/edit/delete.</summary>
public class LocationService(HttpClient http, ILogger<LocationService> logger) : ApiServiceBase(http, logger)
{
    private const string Endpoint = "locations";

    public Task<ServiceResult<List<Location>>> GetLocationsAsync() => GetAsync<List<Location>>(Endpoint);

    public async Task<ServiceResult<Location>> GetLocationAsync(int id)
    {
        var result = await GetAsync<Location>($"{Endpoint}/{id}");
        if (result.Success || !result.IsNotFound)
        {
            return result;
        }

        // The details endpoint may not be available on every API version (egolance.com
        // currently returns 404 for it), so fall back to finding the location in the list.
        var all = await GetLocationsAsync();
        if (!all.Success)
        {
            return ServiceResult<Location>.Fail(all.ErrorMessage ?? "Could not load the location.", all.StatusCode);
        }

        var match = all.Data?.FirstOrDefault(l => l.Id == id);
        return match is not null
            ? ServiceResult<Location>.Ok(match)
            : ServiceResult<Location>.Fail($"There is no location with ID {id}.", 404);
    }
}
