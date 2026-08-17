using Application.Authentication.Responses;
using Domain.Common;
using Domain.Users;

namespace Application.Abstractions.Persistence
{
    public interface IUserRepository
    {
        Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken);

        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>Hashes and persists the credential immediately. The adapter, not this port, owns how.</summary>
        Task<Result<User>> CreateAsync(Email email, string password, CancellationToken cancellationToken);

        /// <summary>
        /// Fails with InvalidCredentials for a wrong password/unknown email/locked-out account
        /// (deliberately indistinguishable, to avoid enumeration) or EmailNotConfirmed. 
        /// The one error in this port that does reveal the account exists, so the user knows why a
        /// correct password still didn't work. On success, also reports whether the account still
        /// needs a TOTP code before a login actually completes.
        /// </summary>
        Task<Result<CredentialVerificationResult>> VerifyCredentialsAsync(Email email, string password, CancellationToken cancellationToken);

        /// <summary>Null if no account exists for the email. Callers must not let that distinction reach the client.</summary>
        Task<string?> GeneratePasswordResetTokenAsync(Email email, CancellationToken cancellationToken);

        /// <summary>Returns the affected user's id on success, so callers can revoke their other sessions.</summary>
        Task<Result<Guid>> ResetPasswordAsync(Email email, string token, string newPassword, CancellationToken cancellationToken);

        Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

        /// <summary>Null if no account exists for the email. Same non-leaking shape as GeneratePasswordResetTokenAsync.</summary>
        Task<string?> GenerateEmailConfirmationTokenAsync(Email email, CancellationToken cancellationToken);

        Task<Result> ConfirmEmailAsync(Email email, string token, CancellationToken cancellationToken);

        /// <summary>
        /// Requires the current password, same confirmation bar as ChangePasswordAsync. 
        /// Deleting an account is at least as destructive. Refresh tokens cascade-delete at the database
        /// level (see RefreshTokenConfiguration), so no separate session revocation is needed.
        /// </summary>
        Task<Result> DeleteAccountAsync(Guid userId, string currentPassword, CancellationToken cancellationToken);

        /// <summary>
        /// Issues (or returns the existing, still-unconfirmed) authenticator key without enabling
        /// anything yet. 2FA only turns on once EnableTwoFactorAsync verifies a code against it.
        /// </summary>
        Task<Result<TwoFactorSetup>> GenerateTwoFactorSetupAsync(Guid userId, CancellationToken cancellationToken);

        /// <summary>Verifies the code against the key GenerateTwoFactorSetupAsync issued, then turns 2FA on and returns a fresh set of recovery codes. Shown to the caller exactly once.</summary>
        Task<Result<IReadOnlyList<string>>> EnableTwoFactorAsync(Guid userId, string code, CancellationToken cancellationToken);

        /// <summary>Requires the current password. Also resets the authenticator key, so a later re-enable starts from a clean secret rather than silently reviving the old one.</summary>
        Task<Result> DisableTwoFactorAsync(Guid userId, string currentPassword, CancellationToken cancellationToken);

        /// <summary>Requires the current password. Invalidates every previously issued recovery code.</summary>
        Task<Result<IReadOnlyList<string>>> RegenerateRecoveryCodesAsync(Guid userId, string currentPassword, CancellationToken cancellationToken);

        /// <summary>
        /// Tries the input as a TOTP code first, then as a recovery code. 
        /// One field on the client covers both without asking the user to say which kind they're holding.
        /// </summary>
        Task<Result<User>> VerifyTwoFactorCodeAsync(Guid userId, string code, CancellationToken cancellationToken);

        /// <summary>Live DB read, not derived from a claim. A settings page needs the current answer, not one that's stale for up to an access token's lifetime.</summary>
        Task<Result<bool>> GetTwoFactorStatusAsync(Guid userId, CancellationToken cancellationToken);
    }
}
