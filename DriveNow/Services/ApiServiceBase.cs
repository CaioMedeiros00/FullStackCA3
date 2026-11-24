using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DriveNow.Models;

namespace DriveNow.Services;

/// <summary>
/// Shared HttpClient plumbing for all API services: sends the request, turns
/// non-success status codes into friendly messages and catches network/JSON errors
/// so the UI keeps working when an API is down.
/// </summary>
public abstract class ApiServiceBase(HttpClient http, ILogger logger)
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected HttpClient Http { get; } = http;
    protected ILogger Logger { get; } = logger;

    protected Task<ServiceResult<T>> GetAsync<T>(string uri) =>
        SendAsync<T>(() => Http.GetAsync(uri), fallback: default);

    protected Task<ServiceResult<T>> PostAsync<T>(string uri, T body) =>
        SendAsync(() => Http.PostAsJsonAsync(uri, body, JsonOptions), fallback: body);

    protected Task<ServiceResult<T>> PutAsync<T>(string uri, T body) =>
        SendAsync(() => Http.PutAsJsonAsync(uri, body, JsonOptions), fallback: body);

    protected async Task<ServiceResult> DeleteAsync(string uri)
    {
        var result = await SendAsync<object>(() => Http.DeleteAsync(uri), fallback: new object());
        return result.Success ? ServiceResult.Ok() : ServiceResult.Fail(result.ErrorMessage!, result.StatusCode);
    }

    /// <param name="fallback">
    /// Returned when the API answers with an empty body (e.g. 204 No Content after a PUT).
    /// </param>
    private async Task<ServiceResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> send, T? fallback)
    {
        try
        {
            using var response = await send();
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractErrorMessage(content, response.StatusCode);
                Logger.LogWarning("API call {Method} {Uri} failed with {Status}: {Message}",
                    response.RequestMessage?.Method, response.RequestMessage?.RequestUri, (int)response.StatusCode, message);
                return ServiceResult<T>.Fail(message, (int)response.StatusCode);
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return fallback is not null
                    ? ServiceResult<T>.Ok(fallback)
                    : ServiceResult<T>.Fail("The server returned no data.");
            }

            var data = JsonSerializer.Deserialize<T>(content, JsonOptions);
            return data is not null
                ? ServiceResult<T>.Ok(data)
                : ServiceResult<T>.Fail("The server returned no data.");
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "API request timed out");
            return ServiceResult<T>.Fail("The server took too long to respond. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "API request failed");
            return ServiceResult<T>.Fail("Unable to reach the DriveNow server. Please check your connection and try again.");
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "API returned invalid JSON");
            return ServiceResult<T>.Fail("The server returned data in an unexpected format.");
        }
    }

    /// <summary>Reads ProblemDetails / { message } / plain-text error bodies, else a friendly default.</summary>
    private static string ExtractErrorMessage(string content, HttpStatusCode status)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    // Validation errors: { "errors": { "Field": ["msg", ...] } }
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var messages = errors.EnumerateObject()
                            .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                                ? p.Value.EnumerateArray().Select(e => e.ToString())
                                : new[] { p.Value.ToString() })
                            .Where(m => !string.IsNullOrWhiteSpace(m))
                            .ToList();
                        if (messages.Count > 0)
                        {
                            return string.Join(" ", messages);
                        }
                    }

                    foreach (var name in new[] { "message", "detail", "error", "title" })
                    {
                        if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String &&
                            !string.IsNullOrWhiteSpace(value.GetString()))
                        {
                            return value.GetString()!;
                        }
                    }
                }
                else if (root.ValueKind == JsonValueKind.String)
                {
                    return root.GetString()!;
                }
            }
            catch (JsonException)
            {
                // Not JSON - use the text if it is short and not an HTML page.
                if (content.Length <= 300 && !content.TrimStart().StartsWith('<'))
                {
                    return content.Trim();
                }
            }
        }

        return status switch
        {
            HttpStatusCode.BadRequest => "The request was invalid. Please check the details and try again.",
            HttpStatusCode.NotFound => "The requested item could not be found.",
            HttpStatusCode.Conflict => "This action conflicts with existing data.",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "You are not allowed to perform this action.",
            >= HttpStatusCode.InternalServerError => "The server encountered an error. Please try again later.",
            _ => $"The request failed ({(int)status})."
        };
    }
}
