using Api.Authentication;
using Application.Authentication.Commands;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Authentication
{
    public static class Logout
    {
        public static async Task<NoContent> Handle(IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
        {
            httpContext.Request.Cookies.TryGetValue(AuthCookies.RefreshToken, out var refreshToken);

            await mediator.Send(new LogoutCommand(refreshToken ?? string.Empty), cancellationToken);

            AuthCookies.Clear(httpContext.Response);

            return TypedResults.NoContent();
        }
    }
}
