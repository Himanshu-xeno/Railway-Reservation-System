using Shared.Common;
using Shared.Exceptions;
using System.Net;
using System.Text.Json;

namespace PaymentService.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred in PaymentService: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            BadRequestException badRequest => (HttpStatusCode.BadRequest, badRequest.Message),
            UnauthorizedException unauthorized => (HttpStatusCode.Unauthorized, unauthorized.Message),
            ForbiddenException forbidden => (HttpStatusCode.Forbidden, forbidden.Message),
            NotFoundException notFound => (HttpStatusCode.NotFound, notFound.Message),
            ConflictException conflict => (HttpStatusCode.Conflict, conflict.Message),
            HttpRequestException => (HttpStatusCode.ServiceUnavailable, "Downstream service is currently unavailable."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse((int)statusCode, message);
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}
