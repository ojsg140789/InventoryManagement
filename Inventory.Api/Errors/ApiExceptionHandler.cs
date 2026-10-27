using Inventory.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Errors;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IProblemDetailsService problems)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499;
            return true;
        }

        var (status, title, detail) = exception switch
        {
            ResourceNotFoundException => (404, "Resource not found", exception.Message),
            InventoryConflictException => (409, "Inventory conflict", exception.Message),
            ArgumentException => (400, "Invalid request", exception.Message),
            _ => (500, "Unexpected error", "The operation could not be completed.")
        };
        if (status == 500) logger.LogError(exception, "Request failed. TraceId: {TraceId}", context.TraceIdentifier);
        else logger.LogWarning("Request rejected with {Status}. TraceId: {TraceId}", status, context.TraceIdentifier);

        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status, Title = title, Detail = detail, Instance = context.Request.Path,
                Extensions = { ["traceId"] = context.TraceIdentifier }
            }
        });
    }
}
