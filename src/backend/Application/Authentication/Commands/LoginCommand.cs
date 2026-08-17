using Application.Authentication.Responses;
using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record LoginCommand(string Email, string Password) : IRequest<Result<LoginOutcome>>;

    public sealed record LoginResult(
        AuthenticationResponse User,
        string AccessToken,
        DateTime AccessTokenExpiresAtUtc,
        string RefreshToken,
        DateTime RefreshTokenExpiresAtUtc);

    /// <summary>
    /// A successful password check doesn't always mean a completed login. An account with 2FA
    /// enabled still needs a TOTP code before any token is issued. Modeled as a closed hierarchy
    /// (private constructor, two nested cases) rather than a nullable-everything DTO so the
    /// endpoint can't accidentally read tokens off a RequiresTwoFactor outcome or vice versa.
    /// </summary>
    public abstract record LoginOutcome
    {
        private LoginOutcome() { }

        public sealed record Completed(LoginResult Result) : LoginOutcome;

        public sealed record RequiresTwoFactor(Guid UserId) : LoginOutcome;
    }
}
