using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Authentication.Commands;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Authentication
{
    public static class DisableTwoFactor
    {
        public sealed record Request(string CurrentPassword);

        public static async Task<Results<NoContent, UnauthorizedHttpResult, ValidationProblem>> Handle(
            Request request, ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken)
        {
            var idClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (idClaim is null || !Guid.TryParse(idClaim, out var userId))
                return TypedResults.Unauthorized();

            var result = await mediator.Send(new DisableTwoFactorCommand(userId, request.CurrentPassword), cancellationToken);

            if (result.IsFailure)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [result.Error.Code] = [result.Error.Message]
                });
            }

            return TypedResults.NoContent();
        }
    }
}
