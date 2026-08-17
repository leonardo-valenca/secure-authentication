using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Authentication;
using Application.Authentication.Commands;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Authentication
{
    public static class ChangePassword
    {
        public sealed record Request(string CurrentPassword, string NewPassword);

        public static async Task<Results<NoContent, UnauthorizedHttpResult, ValidationProblem>> Handle(
            Request request, ClaimsPrincipal user, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
        {
            var idClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (idClaim is null || !Guid.TryParse(idClaim, out var userId))
                return TypedResults.Unauthorized();

            var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [result.Error.Code] = [result.Error.Message]
                });
            }

            // The refresh token backing this cookie was just revoked along with every other
            // session, clear it so the browser doesn't hold onto a cookie that no longer works.
            AuthCookies.Clear(httpContext.Response);

            return TypedResults.NoContent();
        }
    }
}
