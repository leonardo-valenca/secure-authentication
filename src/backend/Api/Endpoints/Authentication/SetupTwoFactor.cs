using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Authentication.Commands;
using Application.Authentication.Responses;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Authentication
{
    public static class SetupTwoFactor
    {
        public static async Task<Results<Ok<TwoFactorSetup>, UnauthorizedHttpResult, NotFound>> Handle(
            ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken)
        {
            var idClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (idClaim is null || !Guid.TryParse(idClaim, out var userId))
                return TypedResults.Unauthorized();

            var result = await mediator.Send(new SetupTwoFactorCommand(userId), cancellationToken);

            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound();
        }
    }
}
