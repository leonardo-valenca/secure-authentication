using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Authentication.Responses;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Authentication
{
    public static class Me
    {
        public static Results<Ok<AuthenticationResponse>, UnauthorizedHttpResult> Handle(ClaimsPrincipal user)
        {
            var idClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var emailClaim = user.FindFirstValue(JwtRegisteredClaimNames.Email);

            if (idClaim is null || emailClaim is null || !Guid.TryParse(idClaim, out var id))
                return TypedResults.Unauthorized();

            return TypedResults.Ok(new AuthenticationResponse(id, emailClaim));
        }
    }
}
