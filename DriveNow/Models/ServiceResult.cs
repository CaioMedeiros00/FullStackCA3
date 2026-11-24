namespace DriveNow.Models;

/// <summary>
/// Result wrapper returned by every service method so components never have to
/// deal with exceptions or raw HTTP responses.
/// </summary>
public class ServiceResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int? StatusCode { get; init; }

    public bool IsConflict => StatusCode == 409;
    public bool IsNotFound => StatusCode == 404;

    public static ServiceResult Ok() => new() { Success = true };
    public static ServiceResult Fail(string message, int? statusCode = null) =>
        new() { Success = false, ErrorMessage = message, StatusCode = statusCode };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static new ServiceResult<T> Fail(string message, int? statusCode = null) =>
        new() { Success = false, ErrorMessage = message, StatusCode = statusCode };
}
