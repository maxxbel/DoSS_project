using System.Net;
using System.Text.Json;
using DocReview.Infrastructure;
using DocReview.Models.Api;

namespace DocReview.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(
            $"Unhandled exception occurred during processing of request. Type={exception.GetType().FullName}, Message={exception.Message}");

        var response = context.Response;
        var (statusCode, title, detail) = exception switch
        {
            InfrastructureNotFoundException => (HttpStatusCode.NotFound, "Not Found", exception.Message),
            InfrastructureForbiddenException => (HttpStatusCode.Forbidden, "Forbidden", exception.Message),
            InfrastructureConflictException => (HttpStatusCode.Conflict, "Conflict", exception.Message),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };
        
        response.StatusCode = (int)statusCode;
        response.ContentType = "application/json";
        
        var payload = new ErrorResponse((int)statusCode, title, detail, DateTime.UtcNow);
        await response.WriteAsJsonAsync(payload);
    }
}