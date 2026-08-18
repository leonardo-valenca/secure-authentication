using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api
{
    /// <summary>
    /// Source-generated logging for genuinely unexpected failures, the ones that reach here are
    /// bugs or infrastructure outages, never an expected failure (those return a Result and never
    /// throw, see Domain.Common.Result). Kept separate from Application.Authentication.AuthenticationEventLog
    /// since this is a cross-cutting Api concern, not an authentication-domain event.
    /// </summary>
    public static partial class UnhandledExceptionLog
    {
        [LoggerMessage(EventId = 2001, Level = LogLevel.Error, Message = "Unhandled exception processing {RequestPath}")]
        public static partial void UnhandledExceptionOccurred(this ILogger logger, Exception exception, string requestPath);
    }

    /// <summary>
    /// Composable in front of the built-in ProblemDetails writer (still registered via
    /// AddProblemDetails/UseExceptionHandler) rather than a try/catch middleware: IExceptionHandler
    /// implementations run in registration order until one returns true, so a second, more specific
    /// handler could be added later (e.g. mapping a particular infrastructure exception to a
    /// different status code) without touching this one. This one is the catch-all, it always
    /// handles what reaches it and always produces a 500, reusing the same IProblemDetailsService
    /// (and its Development-only "exception" extension, see Program.cs's CustomizeProblemDetails)
    /// every other ProblemDetails response in this API already goes through.
    /// </summary>
    internal sealed class GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            logger.UnhandledExceptionOccurred(exception, httpContext.Request.Path);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred."
                }
            });
        }
    }
}
