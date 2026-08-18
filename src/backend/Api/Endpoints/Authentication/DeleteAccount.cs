using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Authentication;
using Application.Authentication.Commands;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Authentication
{
    public static class DeleteAccount
    {
        public sealed record Request(string CurrentPassword);

        public static async Task<Results<NoContent, UnauthorizedHttpResult, ValidationProblem>> Handle(
            Request request, ClaimsPrincipal user, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
        {
            var idClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (idClaim is null || !Guid.TryParse(idClaim, out var userId))
                return TypedResults.Unauthorized();

            var command = new DeleteAccountCommand(userId, request.CurrentPassword);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [result.Error.Code] = [result.Error.Message]
                });
            }

            // The account (and, by cascade, every refresh token it owned) is gone, clear the
            // cookies backing this session so the browser doesn't hold onto ones that no longer
            // resolve to anything.
            AuthCookies.Clear(httpContext.Response);

            return TypedResults.NoContent();
        }
    }
}
