using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace insightsAPI.Middleware
{
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
                _logger.LogError(ex, "An unhandled exception has occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Instance = context.Request.Path,
                Detail = exception.Message // Detailed message
            };

            if (exception is BadHttpRequestException or ArgumentException)
            {
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Bad Request";
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
            }
            else if (exception is KeyNotFoundException)
            {
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Title = "Not Found";
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
            }
            else
            {
                problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                problemDetails.Detail = "Internal Server Error from the custom middleware."; // Hide internal error in 500
            }

            context.Response.StatusCode = problemDetails.Status.Value;

            return context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}
