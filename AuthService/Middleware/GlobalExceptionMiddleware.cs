using Shared.Common;    //import ErrorResponse
using Shared.Exceptions;    //imports custom exceptions
using System.Net;   //Gives HttpStatusCode
using System.Text.Json; //Convert obj -> json

namespace AuthService.Middleware;   //keep middlweware classes together

//This is a Custom Middleware
public class GlobalExceptionMiddleware
{
    //Next middleware is in pipeline
    private readonly RequestDelegate _next;
    //Used for Logging 
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    //ASP.NET injects next, logger 
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    //Middleware must have Invoke/InvokeAsync: entry point(req enter here)
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            //GET /api/users/1  ->  Controller executes
            await _next(context);   //Execute the next middlware/Controller
        }
        catch (Exception ex)    //catches any exception
        {
            //Writes error into logs.
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            //Passes exception to helper method.Convert exception -> HTTP response
            await HandleExceptionAsync(context, ex);
        }
    }

    //Recieves HttpContext, Exception
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        //Tells client : I'm sending JSON data
        context.Response.ContentType = "application/json";

        //Switch Expression : Convert exception -> status code + message
        var (statusCode, message) = exception switch
        {
            BadRequestException badRequest => (HttpStatusCode.BadRequest, badRequest.Message),
            UnauthorizedException unauthorized => (HttpStatusCode.Unauthorized, unauthorized.Message),
            ForbiddenException forbidden => (HttpStatusCode.Forbidden, forbidden.Message),
            NotFoundException notFound => (HttpStatusCode.NotFound, notFound.Message),
            ConflictException conflict => (HttpStatusCode.Conflict, conflict.Message),
            //occurs when calling another service.
            //UserService -> calls BookingService -> BookingService Down
            //response : 503 service unreachable
            HttpRequestException httpEx => (HttpStatusCode.ServiceUnavailable, "Downstream service is currently unavailable."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
            //Any other exception(default case)
        };

        //Convert to sets response
        context.Response.StatusCode = (int)statusCode;

        //create response obj
        var response = new ErrorResponse((int)statusCode, message);
        //convert response to JSON (serialize)
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        //Sends JSON back to client.
        await context.Response.WriteAsync(jsonResponse);
    }
}
