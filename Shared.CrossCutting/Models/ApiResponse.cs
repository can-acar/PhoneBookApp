using System.Text.Json.Serialization;

namespace Shared.CrossCutting.Models;

public class ApiResponse
{
    public bool Success { get; set; }
    public int Code { get; set; }
    public string? Message { get; set; }
    public ApiError? Error { get; set; }

    public ApiResponse(bool success, int code, string message, ApiError? error)
    {
        Success = success;
        Code = code;
        Message = message;
        Error = error;
    }

    /// Success Result without Data
    /// <summary>
    /// Creates a successful ApiResponse with the specified success status, code, and optional error.
    /// </summary>
    /// <param name="success">Indicates whether the operation was successful.</param>
    /// <param name="code">The Message status code.</param>
    /// 
    public static ApiResponse Result(bool success, int code, ApiError? error = null) =>
        new(success, code, string.Empty, error);

    // Success Result with Message
    /// <summary>
    /// Creates a successful ApiResponse with the specified success status, code, message, and optional error.
    /// </summary>
    /// <param name="success">Indicates whether the operation was successful.</param>
    /// <param name="code">The Message status code.</param>
    /// <param name="message">The message to include in the response.</param>
    /// <param name="error">Optional error information to include in the response.</param>
    /// <returns>An ApiResponse object with the specified parameters.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the message is null or empty.</exception>
    public static ApiResponse Result(bool success, int code, string message, ApiError? error = null) => new(success, code, message, error);

    // Success Result with Data, Code and Message
    /// <summary>
    ///  Creates a successful ApiResponse with the specified success status, data, code, message, and optional error.
    ///  /// </summary>
    /// <param name="success">Indicates whether the operation was successful.</param>
    /// <param name="data">The data to include in the response.</param>
    /// <param name="code">The Message status code.</param>
    /// <param name="message">The message to include in the response.</param>
    /// <param name="error">Optional error information to include in the response.</param>
    /// <returns>An ApiResponse object with the specified parameters.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the message is null or empty.</exception>
    /// <typeparam name="T">The type of the data to include in the response.</typeparam>
    public static ApiResponse<T> Result<T>(bool success, T? data, int code, string message, ApiError? error = null) =>
        new(success, data, code, error)
        {
            Message = message
        };

    
}


public class ApiResponse<T> : ApiResponse
{
    private readonly T? _data;

    public ApiResponse(bool success, T? data, int code, ApiError? error) : base(success, code, string.Empty, error)
    {
        _data = data;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data => Success ? _data : default(T);
    

    public static implicit operator ApiResponse<T>(T? data) =>
        data is not null ? Result(true, data, 200, string.Empty) : Result(false, data, 404, "Data not found", ApiError.ToNullable);
}

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public ApiError(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public ApiError()
    {
    }

    public static ApiError ToNullable => new("NotFound", "Data not found");
}
