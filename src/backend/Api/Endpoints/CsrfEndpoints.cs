using Api.Authentication;

namespace Api.Endpoints
{
    public static class CsrfEndpoints
    {
        public static IEndpointRouteBuilder MapCsrfEndpoints(this IEndpointRouteBuilder endpoints)
        {
            // The SPA calls this once on bootstrap, before issuing any state-changing request,
            // to receive the cookie half of the double-submit pair.
            endpoints.MapGet("/api/csrf-token", (HttpContext httpContext) =>
            {
                CsrfCookie.Issue(httpContext.Response);
                return Results.NoContent();
            })
            .AllowAnonymous()
            .WithName("GetCsrfToken")
            .WithTags("Csrf");

            return endpoints;
        }
    }
}
