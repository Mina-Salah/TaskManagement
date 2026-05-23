using System.Net;
using System.Text.Json;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Wrappers;

namespace TaskManagement.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, response) = exception switch
        {
            ValidationException vex => (
                HttpStatusCode.BadRequest,
                ApiResponse.FailResult("Validation failed", vex.Errors)),

            NotFoundException => (
                HttpStatusCode.NotFound,
                ApiResponse.FailResult(exception.Message)),

            ForbiddenException => (
                HttpStatusCode.Forbidden,
                ApiResponse.FailResult(exception.Message)),

            BadRequestException => (
                HttpStatusCode.BadRequest,
                ApiResponse.FailResult(exception.Message)),

            ConflictException => (
                HttpStatusCode.Conflict,
                ApiResponse.FailResult(exception.Message)),

            _ => (
                HttpStatusCode.InternalServerError,
                ApiResponse.FailResult("An unexpected error occurred. Please try again later."))
        };

        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
