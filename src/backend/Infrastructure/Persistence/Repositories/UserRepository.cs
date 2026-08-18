using Application.Abstractions.Persistence;
using Application.Authentication;
using Application.Authentication.Responses;
using Domain.Common;
using Domain.Users;
using Infrastructure.Identity;
using Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistence.Repositories
{
    internal sealed class UserRepository(
        UserManager<AppIdentityUser> userManager,
        SignInManager<AppIdentityUser> signInManager,
        IOptions<JwtOptions> jwtOptions,
        ILogger<UserRepository> logger) : IUserRepository
    {
        // Recovery codes are single-use, one-time-shown backup credentials for when the
        // authenticator device itself is unavailable, 10 matches what most authenticator-app
        // guidance recommends as enough to not run out before a user thinks to regenerate them.
        private const int RecoveryCodeCount = 10;

        public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken)
        {
            return await userManager.FindByEmailAsync(email.Value) is not null;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByIdAsync(id.ToString());
            return identityUser is null ? null : ToDomainUser(identityUser);
        }

        public async Task<Result<User>> CreateAsync(Email email, string password, CancellationToken cancellationToken)
        {
            var user = User.Create(email);

            var identityUser = new AppIdentityUser
            {
                Id = user.Id,
                Email = email.Value,
                UserName = email.Value,
                CreatedAtUtc = user.CreatedAtUtc,
            };

            var result = await userManager.CreateAsync(identityUser, password);
            if (result.Succeeded)
                return user;

            if (result.Errors.Any(error => error.Code is "DuplicateUserName" or "DuplicateEmail"))
                return Result.Failure<User>(UserErrors.EmailAlreadyInUse);

            return Result.Failure<User>(UserErrors.WeakPassword);
        }

        public async Task<Result<CredentialVerificationResult>> VerifyCredentialsAsync(Email email, string password, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByEmailAsync(email.Value);
            if (identityUser is null)
            {
                // Same generic error and log event as a wrong password against a real account
                // an operator watching for credential-stuffing sweeps needs both, and neither
                // should be distinguishable from the other in the response.
                logger.LoginFailedWrongPassword(email.Value);
                return Result.Failure<CredentialVerificationResult>(UserErrors.InvalidCredentials);
            }

            var result = await signInManager.CheckPasswordSignInAsync(identityUser, password, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var requiresTwoFactor = await userManager.GetTwoFactorEnabledAsync(identityUser);
                return new CredentialVerificationResult(ToDomainUser(identityUser), requiresTwoFactor);
            }

            // NotAllowed is what RequireConfirmedEmail (see Infrastructure.DependencyInjection)
            // turns an otherwise-correct password into, everything else (wrong password, unknown
            // email, locked out) stays behind the generic message on purpose. The API response
            // stays generic either way, logging the real reason here (not in the handler, which
            // never sees more than the collapsed error) is what makes a lockout distinguishable
            // from an ordinary bad password in the logs an operator would actually look at.
            if (result.IsNotAllowed)
            {
                logger.LoginBlockedEmailNotConfirmed(email.Value);
                return Result.Failure<CredentialVerificationResult>(UserErrors.EmailNotConfirmed);
            }

            if (result.IsLockedOut)
                logger.AccountLockedOut(email.Value);
            else
                logger.LoginFailedWrongPassword(email.Value);

            return Result.Failure<CredentialVerificationResult>(UserErrors.InvalidCredentials);
        }

        public async Task<string?> GeneratePasswordResetTokenAsync(Email email, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByEmailAsync(email.Value);
            return identityUser is null ? null : await userManager.GeneratePasswordResetTokenAsync(identityUser);
        }

        public async Task<Result<Guid>> ResetPasswordAsync(Email email, string token, string newPassword, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByEmailAsync(email.Value);
            if (identityUser is null)
                return Result.Failure<Guid>(UserErrors.InvalidResetToken);

            var result = await userManager.ResetPasswordAsync(identityUser, token, newPassword);
            if (result.Succeeded)
                return identityUser.Id;

            if (result.Errors.Any(error => error.Code == "InvalidToken"))
                return Result.Failure<Guid>(UserErrors.InvalidResetToken);

            return Result.Failure<Guid>(UserErrors.WeakPassword);
        }

        public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByIdAsync(userId.ToString());
            if (identityUser is null)
                return Result.Failure(UserErrors.CurrentPasswordIncorrect);

            var result = await userManager.ChangePasswordAsync(identityUser, currentPassword, newPassword);
            if (result.Succeeded)
                return Result.Success();

            if (result.Errors.Any(error => error.Code == "PasswordMismatch"))
                return Result.Failure(UserErrors.CurrentPasswordIncorrect);

            return Result.Failure(UserErrors.WeakPassword);
        }

        public async Task<string?> GenerateEmailConfirmationTokenAsync(Email email, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByEmailAsync(email.Value);
            return identityUser is null ? null : await userManager.GenerateEmailConfirmationTokenAsync(identityUser);
        }

        public async Task<Result> ConfirmEmailAsync(Email email, string token, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByEmailAsync(email.Value);
            if (identityUser is null)
                return Result.Failure(UserErrors.InvalidConfirmationToken);

            var result = await userManager.ConfirmEmailAsync(identityUser, token);
            return result.Succeeded ? Result.Success() : Result.Failure(UserErrors.InvalidConfirmationToken);
        }

        public async Task<Result> DeleteAccountAsync(Guid userId, string currentPassword, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByIdAsync(userId.ToString());
            if (identityUser is null)
                return Result.Failure(UserErrors.CurrentPasswordIncorrect);

            // CheckPasswordAsync, not CheckPasswordSignInAsync, this is a re-confirmation step on
            // an already-authenticated session, not a sign-in attempt, so it shouldn't apply
            // lockout/failed-attempt tracking on top of whatever brought the user here.
            if (!await userManager.CheckPasswordAsync(identityUser, currentPassword))
            {
                logger.AccountDeletionFailedWrongPassword(userId);
                return Result.Failure(UserErrors.CurrentPasswordIncorrect);
            }

            var result = await userManager.DeleteAsync(identityUser);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to delete user {userId}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            logger.AccountDeleted(userId);
            return Result.Success();
        }

        public async Task<Result<TwoFactorSetup>> GenerateTwoFactorSetupAsync(Guid userId, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByIdAsync(userId.ToString());
            if (identityUser is null)
                return Result.Failure<TwoFactorSetup>(UserErrors.AccountNotFound);

            // A key already exists whenever setup was started before but never confirmed with
            // EnableTwoFactorAsync, reuse it instead of invalidating an authenticator app entry
            // the user may have already scanned.
            var key = await userManager.GetAuthenticatorKeyAsync(identityUser);
            if (string.IsNullOrEmpty(key))
            {
                await userManager.ResetAuthenticatorKeyAsync(identityUser);
                key = await userManager.GetAuthenticatorKeyAsync(identityUser);
            }

            var issuer = Uri.EscapeDataString(jwtOptions.Value.Issuer);
            var label = Uri.EscapeDataString(identityUser.Email!);
            var authenticatorUri = $"otpauth://totp/{issuer}:{label}?secret={key}&issuer={issuer}&digits=6";

            return new TwoFactorSetup(key!, authenticatorUri);
        }

        public async Task<Result<IReadOnlyList<string>>> EnableTwoFactorAsync(Guid userId, string code, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByIdAsync(userId.ToString());
            if (identityUser is null)
                return Result.Failure<IReadOnlyList<string>>(UserErrors.AccountNotFound);

            if (await userManager.GetTwoFactorEnabledAsync(identityUser))
                return Result.Failure<IReadOnlyList<string>>(UserErrors.TwoFactorAlreadyEnabled);

            var codeValid = await userManager.VerifyTwoFactorTokenAsync(identityUser, TokenOptions.DefaultAuthenticatorProvider, code);
            if (!codeValid)
            {
                logger.TwoFactorEnableFailedInvalidCode(userId);
                return Result.Failure<IReadOnlyList<string>>(UserErrors.TwoFactorCodeInvalid);
            }

            await userManager.SetTwoFactorEnabledAsync(identityUser, true);
            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(identityUser, RecoveryCodeCount);

            logger.TwoFactorEnabled(userId);
            return recoveryCodes!.ToList();
        }

        public async Task<Result> DisableTwoFactorAsync(Guid userId, string currentPassword, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByIdAsync(userId.ToString());
            if (identityUser is null)
                return Result.Failure(UserErrors.CurrentPasswordIncorrect);

            if (!await userManager.CheckPasswordAsync(identityUser, currentPassword))
            {
                logger.TwoFactorDisableFailedWrongPassword(userId);
                return Result.Failure(UserErrors.CurrentPasswordIncorrect);
            }

            if (!await userManager.GetTwoFactorEnabledAsync(identityUser))
                return Result.Failure(UserErrors.TwoFactorNotEnabled);

            await userManager.SetTwoFactorEnabledAsync(identityUser, false);

            // Clears the shared secret too, not just the flag, a later re-enable starts from a
            // fresh key and a fresh QR code rather than silently reviving the old one.
            await userManager.ResetAuthenticatorKeyAsync(identityUser);

            logger.TwoFactorDisabled(userId);
            return Result.Success();
        }

        public async Task<Result<IReadOnlyList<string>>> RegenerateRecoveryCodesAsync(Guid userId, string currentPassword, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByIdAsync(userId.ToString());
            if (identityUser is null)
                return Result.Failure<IReadOnlyList<string>>(UserErrors.CurrentPasswordIncorrect);

            if (!await userManager.CheckPasswordAsync(identityUser, currentPassword))
                return Result.Failure<IReadOnlyList<string>>(UserErrors.CurrentPasswordIncorrect);

            if (!await userManager.GetTwoFactorEnabledAsync(identityUser))
                return Result.Failure<IReadOnlyList<string>>(UserErrors.TwoFactorNotEnabled);

            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(identityUser, RecoveryCodeCount);
            logger.RecoveryCodesRegenerated(userId);
            return recoveryCodes!.ToList();
        }

        public async Task<Result<User>> VerifyTwoFactorCodeAsync(Guid userId, string code, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByIdAsync(userId.ToString());
            if (identityUser is null)
                return Result.Failure<User>(UserErrors.TwoFactorCodeInvalid);

            if (await userManager.VerifyTwoFactorTokenAsync(identityUser, TokenOptions.DefaultAuthenticatorProvider, code))
            {
                logger.TwoFactorLoginSucceeded(userId);
                return ToDomainUser(identityUser);
            }

            var recoveryResult = await userManager.RedeemTwoFactorRecoveryCodeAsync(identityUser, code);
            if (recoveryResult.Succeeded)
            {
                logger.RecoveryCodeUsed(userId);
                return ToDomainUser(identityUser);
            }

            logger.TwoFactorLoginFailed(userId);
            return Result.Failure<User>(UserErrors.TwoFactorCodeInvalid);
        }

        public async Task<Result<bool>> GetTwoFactorStatusAsync(Guid userId, CancellationToken cancellationToken)
        {
            var identityUser = await userManager.FindByIdAsync(userId.ToString());
            if (identityUser is null)
                return Result.Failure<bool>(UserErrors.AccountNotFound);

            return await userManager.GetTwoFactorEnabledAsync(identityUser);
        }

        private static User ToDomainUser(AppIdentityUser identityUser)
        {
            var email = Email.FromPersistence(identityUser.Email!);
            return User.FromPersistence(identityUser.Id, email, identityUser.CreatedAtUtc);
        }
    }
}
