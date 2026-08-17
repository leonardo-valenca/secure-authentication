using Domain.Common;

namespace Domain.Users
{
    public static class UserErrors
    {
        public static readonly Error EmailEmpty = new("User.EmailEmpty", "Email is required.");

        public static readonly Error EmailInvalidFormat = new("User.EmailInvalidFormat", "Email format is invalid.");

        // Matches the nvarchar(256) column ASP.NET Core Identity generates for Email/NormalizedEmail
        // (see the InitialCreate migration) without this, an oversized email passes domain
        // validation only to fail as an unhandled SQL truncation error at save time instead of a clean 400.
        public static readonly Error EmailTooLong = new("User.EmailTooLong", "Email must be 256 characters or fewer.");

        public static readonly Error EmailAlreadyInUse = new("User.EmailAlreadyInUse", "This email is already registered.");

        public static readonly Error InvalidCredentials = new("User.InvalidCredentials", "Email or password is incorrect.");

        public static readonly Error WeakPassword = new("User.WeakPassword", "Password does not meet the minimum security requirements.");

        public static readonly Error InvalidResetToken = new("User.InvalidResetToken", "The password reset token is invalid or has expired.");

        public static readonly Error CurrentPasswordIncorrect = new("User.CurrentPasswordIncorrect", "The current password is incorrect.");

        // Deliberately distinct from InvalidCredentials, unlike every other auth error the user
        // needs to know *why* login is failing when the password was actually correct. Accepted
        // trade-off: this one message confirms the account exists, where every other path doesn't.
        public static readonly Error EmailNotConfirmed = new("User.EmailNotConfirmed", "Please confirm your email address before logging in.");

        public static readonly Error InvalidConfirmationToken = new("User.InvalidConfirmationToken", "The email confirmation link is invalid or has expired.");

        // Only reachable via a still-valid access token issued before the account was deleted
        // JWTs aren't revocable before they expire (see JwtOptions.AccessTokenLifetimeMinutes), so
        // a handful of requests in that window can legitimately hit an already-deleted account.
        public static readonly Error AccountNotFound = new("User.AccountNotFound", "This account no longer exists.");

        public static readonly Error TwoFactorCodeInvalid = new("User.TwoFactorCodeInvalid", "That code is incorrect or has expired.");

        // Distinct from TwoFactorCodeInvalid: this means the mfa_challenge cookie itself is
        // missing, expired, or tampered with (see Api.Authentication.MfaChallengeCookie) - the
        // user needs to log in again from the start, a code alone can't fix it.
        public static readonly Error TwoFactorChallengeInvalid = new("User.TwoFactorChallengeInvalid", "Your session expired - please log in again.");

        public static readonly Error TwoFactorAlreadyEnabled = new("User.TwoFactorAlreadyEnabled", "Two-factor authentication is already enabled.");

        public static readonly Error TwoFactorNotEnabled = new("User.TwoFactorNotEnabled", "Two-factor authentication is not enabled.");
    }
}