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
            context.Response.ContentType = "application/json";
            
            // Allow throwing specific custom exceptions in the future to map to 404, 422, etc.
            if (exception is BadHttpRequestException or ArgumentException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            else if (exception is KeyNotFoundException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = context.Response.StatusCode == 500 ? "Internal Server Error from the custom middleware." : exception.Message,
                Detailed = exception.Message // For debugging, usually omit in prod, keeping it here for API parity with python MVP's FastAPI exceptions which leak details
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
