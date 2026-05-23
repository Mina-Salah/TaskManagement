namespace TaskManagement.Application.Common.Wrappers;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public IEnumerable<string>? Errors { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string message = "Operation completed successfully")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> FailResult(string message, IEnumerable<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IEnumerable<string>? Errors { get; set; }

    public static ApiResponse SuccessResult(string message = "Operation completed successfully")
        => new() { Success = true, Message = message };

    public static ApiResponse FailResult(string message, IEnumerable<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}
