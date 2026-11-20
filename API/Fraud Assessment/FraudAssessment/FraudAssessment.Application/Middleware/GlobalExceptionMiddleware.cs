using FraudAssessment.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace FraudAssessment.API.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "An error occurred.");

            if (context.Response.HasStarted)
                throw;

            var statusCode = ex switch
            {
                NotFoundException => HttpStatusCode.NotFound,

                UnauthorizedException => HttpStatusCode.Unauthorized,

                ForbiddenException => HttpStatusCode.Forbidden,

                IdempotencyConflictException => HttpStatusCode.Conflict,

                ConflictException => HttpStatusCode.Conflict,

                ValidationException => HttpStatusCode.BadRequest,

                ExternalServiceException => HttpStatusCode.BadGateway,

                ServiceUnavailableException => HttpStatusCode.ServiceUnavailable,

                _ => HttpStatusCode.InternalServerError
            };

            var message = statusCode == HttpStatusCode.InternalServerError
                ? "An internal server error occurred."
                : ex.Message;

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                StatusCode = (int)statusCode,
                Message = message
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
    }
}