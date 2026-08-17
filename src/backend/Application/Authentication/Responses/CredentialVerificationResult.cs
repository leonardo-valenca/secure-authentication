using Domain.Users;

namespace Application.Authentication.Responses
{
    /// <summary>
    /// What VerifyCredentialsAsync actually established: the password was correct, and whether a
    /// second factor still stands between this and a completed login. Kept as one round trip
    /// rather than a separate "is 2FA enabled" call. LoginCommandHandler needs both facts
    /// together to decide whether to issue tokens now or wait for a TOTP code.
    /// </summary>
    public sealed record CredentialVerificationResult(User User, bool RequiresTwoFactor);
}
