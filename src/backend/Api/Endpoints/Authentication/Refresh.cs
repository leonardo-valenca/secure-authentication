using Api.Authentication;
using Application.Authentication.Commands;
using Application.Authentication.Responses;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Authentication
{
    public static class Refresh
    {
        public static async Task<Results<Ok<AuthenticationResponse>, UnauthorizedHttpResult>> Handle(
            IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
        {
            if (!httpContext.Request.Cookies.TryGetValue(AuthCookies.RefreshToken, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
                return TypedResults.Unauthorized();

            var result = await mediator.Send(new RefreshCommand(refreshToken), cancellationToken);

            if (result.IsFailure)
            {
                AuthCookies.Clear(httpContext.Response);
                return TypedResults.Unauthorized();
            }

            var loginResult = result.Value;

            httpContext.Response.Cookies.Append(
                AuthCookies.AccessToken,
                loginResult.AccessToken,
                AuthCookies.Build(loginResult.AccessTokenExpiresAtUtc));

            httpContext.Response.Cookies.Append(
                AuthCookies.RefreshToken,
                loginResult.RefreshToken,
                AuthCookies.Build(loginResult.RefreshTokenExpiresAtUtc));

            return TypedResults.Ok(loginResult.User);
        }
    }
}
