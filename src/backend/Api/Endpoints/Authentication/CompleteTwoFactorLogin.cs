using Api.Authentication;
using Application.Authentication.Commands;
using Application.Authentication.Responses;
using Domain.Users;
using Mediator;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Authentication
{
    public static class CompleteTwoFactorLogin
    {
        public sealed record Request(string Code);

        public static async Task<Results<Ok<AuthenticationResponse>, ValidationProblem>> Handle(
            Request request,
            IMediator mediator,
            HttpContext httpContext,
            IDataProtectionProvider dataProtectionProvider,
            CancellationToken cancellationToken)
        {
            var userId = MfaChallengeCookie.Validate(httpContext.Request, dataProtectionProvider);
            if (userId is null)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [UserErrors.TwoFactorChallengeInvalid.Code] = [UserErrors.TwoFactorChallengeInvalid.Message]
                });
            }

            var command = new CompleteTwoFactorLoginCommand(userId.Value, request.Code);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [result.Error.Code] = [result.Error.Message]
                });
            }

            // The challenge is spent either way past this point, a used-once code shouldn't be
            // replayable, and a fresh login attempt should start from a fresh cookie regardless.
            MfaChallengeCookie.Clear(httpContext.Response);
            AuthCookies.SetLoginCookies(httpContext.Response, result.Value);

            return TypedResults.Ok(result.Value.User);
        }
    }
}
