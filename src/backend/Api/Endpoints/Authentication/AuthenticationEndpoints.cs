using Api.Authentication;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Endpoints.Authentication
{
    public static class AuthenticationEndpoints
    {
        public const string AuthRateLimiterPolicy = "auth";

        public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints
                .MapGroup("/api/authentication")
                .WithTags("Authentication");

            // Every route below shares one of three trait combinations, hoisted onto a sub-group
            // instead of repeated per endpoint. Logout is the one deliberate exception (CSRF, but no
            // rate limit, and reachable without a valid session) and stays on the parent group.
            //
            // No .Produces<T>()/.ProducesValidationProblem() calls below, every handler returns a
            // TypedResults union (Results<Ok<T>, ValidationProblem>, etc.) that already encodes its
            // exact set of possible responses, so OpenAPI generation reads that from the return type
            // directly instead of needing it restated here.

            var anonymousMutating = group
                .MapGroup("")
                .AllowAnonymous()
                .RequireRateLimiting(AuthRateLimiterPolicy)
                .AddEndpointFilter<CsrfEndpointFilter>();

            anonymousMutating.MapPost("/register", Register.Handle).WithName("Register");
            anonymousMutating.MapPost("/login", Login.Handle).WithName("Login");
            anonymousMutating.MapPost("/2fa/login", CompleteTwoFactorLogin.Handle).WithName("CompleteTwoFactorLogin");
            anonymousMutating.MapPost("/refresh", Refresh.Handle).WithName("Refresh");
            anonymousMutating.MapPost("/forgot-password", ForgotPassword.Handle).WithName("ForgotPassword");
            anonymousMutating.MapPost("/reset-password", ResetPassword.Handle).WithName("ResetPassword");
            anonymousMutating.MapPost("/confirm-email", ConfirmEmail.Handle).WithName("ConfirmEmail");
            anonymousMutating.MapPost("/resend-confirmation", ResendConfirmationEmail.Handle).WithName("ResendConfirmationEmail");

            group.MapPost("/logout", Logout.Handle)
                .AllowAnonymous()
                .AddEndpointFilter<CsrfEndpointFilter>()
                .WithName("Logout");

            var authenticatedMutating = group
                .MapGroup("")
                .RequireAuthorization()
                .RequireRateLimiting(AuthRateLimiterPolicy)
                .AddEndpointFilter<CsrfEndpointFilter>();

            authenticatedMutating.MapPost("/change-password", ChangePassword.Handle).WithName("ChangePassword");
            authenticatedMutating.MapPost("/delete-account", DeleteAccount.Handle).WithName("DeleteAccount");
            authenticatedMutating.MapPost("/2fa/setup", SetupTwoFactor.Handle).WithName("SetupTwoFactor");
            authenticatedMutating.MapPost("/2fa/enable", EnableTwoFactor.Handle).WithName("EnableTwoFactor");
            authenticatedMutating.MapPost("/2fa/disable", DisableTwoFactor.Handle).WithName("DisableTwoFactor");
            authenticatedMutating.MapPost("/2fa/recovery-codes/regenerate", RegenerateRecoveryCodes.Handle).WithName("RegenerateRecoveryCodes");

            var authenticatedReads = group
                .MapGroup("")
                .RequireAuthorization();

            authenticatedReads.MapGet("/me", Me.Handle).WithName("Me");
            authenticatedReads.MapGet("/me/export", ExportAccountData.Handle).WithName("ExportAccountData");
            authenticatedReads.MapGet("/2fa/status", GetTwoFactorStatus.Handle).WithName("GetTwoFactorStatus");

            return endpoints;
        }
    }
}
