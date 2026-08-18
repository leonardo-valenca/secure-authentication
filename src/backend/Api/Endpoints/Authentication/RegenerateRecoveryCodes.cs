using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Authentication.Commands;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Authentication
{
    public static class RegenerateRecoveryCodes
    {
        public sealed record Request(string CurrentPassword);

        public sealed record Response(IReadOnlyList<string> RecoveryCodes);

        public static async Task<Results<Ok<Response>, UnauthorizedHttpResult, ValidationProblem>> Handle(
            Request request, ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken)
        {
            var idClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (idClaim is null || !Guid.TryParse(idClaim, out var userId))
                return TypedResults.Unauthorized();

            var result = await mediator.Send(new RegenerateRecoveryCodesCommand(userId, request.CurrentPassword), cancellationToken);

            if (result.IsFailure)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [result.Error.Code] = [result.Error.Message]
                });
            }

            return TypedResults.Ok(new Response(result.Value));
        }
    }
}
