namespace EnterpriseWeb.API.Middleware;

using System.Net;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found");
            await WriteErrorResponse(context, HttpStatusCode.NotFound, "Resource not found.");
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access attempt");
            await WriteErrorResponse(context, HttpStatusCode.Forbidden, "Access denied.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred");
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError,
                "An internal server error occurred.");
        }
    }

    private static Task WriteErrorResponse(
        HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new { status = (int)statusCode, message });
    }
}
