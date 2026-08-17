using Api.Authentication;
using Application.Authentication.Commands;
using Application.Authentication.Responses;
using Mediator;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Authentication
{
    public static class Login
    {
        /// <summary>User is null exactly when RequiresTwoFactor is true, no tokens exist yet to describe a user for.</summary>
        public sealed record Response(bool RequiresTwoFactor, AuthenticationResponse? User);

        public static async Task<Results<Ok<Response>, ValidationProblem>> Handle(
            LoginCommand request,
            IMediator mediator,
            HttpContext httpContext,
            IDataProtectionProvider dataProtectionProvider,
            CancellationToken cancellationToken)
        {
            var result = await mediator.Send(request, cancellationToken);

            if (result.IsFailure)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [result.Error.Code] = [result.Error.Message]
                });
            }

            switch (result.Value)
            {
                case LoginOutcome.RequiresTwoFactor requiresTwoFactor:
                    // MfaChallengeCookie.Issue(httpContext.Response, dataProtectionProvider, requiresTwoFactor.UserId);
                    return TypedResults.Ok(new Response(RequiresTwoFactor: true, User: null));

                case LoginOutcome.Completed completed:
                    AuthCookies.SetLoginCookies(httpContext.Response, completed.Result);
                    return TypedResults.Ok(new Response(RequiresTwoFactor: false, completed.Result.User));

                default:
                    throw new InvalidOperationException($"Unhandled {nameof(LoginOutcome)} case: {result.Value.GetType()}");
            }
        }
    }
}
