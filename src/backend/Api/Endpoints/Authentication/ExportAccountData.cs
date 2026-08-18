using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Authentication.Commands;
using Application.Authentication.Responses;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Authentication
{
    public static class ExportAccountData
    {
        public static async Task<Results<Ok<AccountDataExport>, UnauthorizedHttpResult, NotFound>> Handle(
            ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken)
        {
            var idClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (idClaim is null || !Guid.TryParse(idClaim, out var userId))
                return TypedResults.Unauthorized();

            var result = await mediator.Send(new ExportAccountDataCommand(userId), cancellationToken);

            // 404, not the ValidationProblem shape every other endpoint here uses, this isn't a
            // bad request, the resource the request asked for genuinely doesn't exist (a stale
            // access token outliving the account it was issued for, see UserErrors.AccountNotFound).
            return result.IsSuccess
                ? TypedResults.Ok(result.Value)
                : TypedResults.NotFound();
        }
    }
}
