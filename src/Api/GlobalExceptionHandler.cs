using Microsoft.AspNetCore.Diagnostics;

namespace Api;

public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            BadHttpRequestException badHttpRequestException => badHttpRequestException.StatusCode,
            _ => StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = statusCode,
                Title = statusCode switch
                {
                    StatusCodes.Status400BadRequest => "Bad Request",
                    _ => "An error occurred while processing your request."
                },
                Detail = statusCode switch
                {
                    StatusCodes.Status400BadRequest => "Некоректний запит або невірний формат JSON.",
                    _ => null
                }
            },
            Exception = exception
        });
    }
}
