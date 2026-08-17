using Microsoft.Extensions.Logging;

namespace Application.Authentication
{
    /// <summary>
    /// Source-generated log methods for security-relevant authentication events. 
    /// The things an operator would actually need when investigating an incident (lockouts, token reuse,
    /// session revocations), not a log line for every request. Never passed a password or a raw
    /// token: only identifiers (user id, email) that are safe to sit in log storage.
    /// </summary>
    public static partial class AuthenticationEventLog
    {
        [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "User {UserId} registered with email {Email}")]
        public static partial void UserRegistered(this ILogger logger, Guid userId, string email);

        [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Failed login attempt for {Email}: incorrect password")]
        public static partial void LoginFailedWrongPassword(this ILogger logger, string email);

        [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "Account locked out after repeated failed login attempts: {Email}")]
        public static partial void AccountLockedOut(this ILogger logger, string email);

        [LoggerMessage(EventId = 1004, Level = LogLevel.Warning, Message = "Login blocked - email not confirmed: {Email}")]
        public static partial void LoginBlockedEmailNotConfirmed(this ILogger logger, string email);

        [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "User {UserId} logged in")]
        public static partial void UserLoggedIn(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1006, Level = LogLevel.Warning, Message = "Refresh token reuse detected for user {UserId} - all active sessions revoked")]
        public static partial void RefreshTokenReuseDetected(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1007, Level = LogLevel.Information, Message = "User {UserId} logged out")]
        public static partial void UserLoggedOut(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1008, Level = LogLevel.Information, Message = "Password changed for user {UserId} - all other sessions revoked")]
        public static partial void PasswordChanged(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1009, Level = LogLevel.Warning, Message = "Failed password change attempt for user {UserId}: current password incorrect")]
        public static partial void PasswordChangeFailed(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1010, Level = LogLevel.Information, Message = "Password reset requested for {Email}")]
        public static partial void PasswordResetRequested(this ILogger logger, string email);

        [LoggerMessage(EventId = 1011, Level = LogLevel.Information, Message = "Password reset completed for user {UserId} - all sessions revoked")]
        public static partial void PasswordResetCompleted(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1012, Level = LogLevel.Warning, Message = "Invalid or expired password reset token used for {Email}")]
        public static partial void PasswordResetTokenInvalid(this ILogger logger, string email);

        [LoggerMessage(EventId = 1013, Level = LogLevel.Information, Message = "Email confirmed for {Email}")]
        public static partial void EmailConfirmed(this ILogger logger, string email);

        [LoggerMessage(EventId = 1014, Level = LogLevel.Warning, Message = "Invalid or expired email confirmation token used for {Email}")]
        public static partial void EmailConfirmationTokenInvalid(this ILogger logger, string email);

        [LoggerMessage(EventId = 1015, Level = LogLevel.Warning, Message = "Rate limit exceeded for {RemoteIpAddress} on {Path}")]
        public static partial void RateLimitExceeded(this ILogger logger, string remoteIpAddress, string path);

        [LoggerMessage(EventId = 1016, Level = LogLevel.Information, Message = "Account {UserId} deleted")]
        public static partial void AccountDeleted(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1017, Level = LogLevel.Warning, Message = "Failed account deletion attempt for user {UserId}: current password incorrect")]
        public static partial void AccountDeletionFailedWrongPassword(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1018, Level = LogLevel.Information, Message = "Two-factor authentication enabled for user {UserId}")]
        public static partial void TwoFactorEnabled(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1019, Level = LogLevel.Warning, Message = "Failed attempt to enable two-factor authentication for user {UserId}: invalid code")]
        public static partial void TwoFactorEnableFailedInvalidCode(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1020, Level = LogLevel.Information, Message = "Two-factor authentication disabled for user {UserId}")]
        public static partial void TwoFactorDisabled(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1021, Level = LogLevel.Warning, Message = "Failed attempt to disable two-factor authentication for user {UserId}: current password incorrect")]
        public static partial void TwoFactorDisableFailedWrongPassword(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1022, Level = LogLevel.Information, Message = "Recovery codes regenerated for user {UserId}")]
        public static partial void RecoveryCodesRegenerated(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1023, Level = LogLevel.Information, Message = "Two-factor login completed for user {UserId}")]
        public static partial void TwoFactorLoginSucceeded(this ILogger logger, Guid userId);

        [LoggerMessage(EventId = 1024, Level = LogLevel.Warning, Message = "Failed two-factor login attempt for user {UserId}: invalid code")]
        public static partial void TwoFactorLoginFailed(this ILogger logger, Guid userId);

        // Distinct from a normal TOTP-based TwoFactorLoginSucceeded. A recovery code is the
        // weaker backup path, and an operator watching for account-takeover attempts (an attacker
        // who obtained a leaked recovery code list) needs to see this specifically, not just
        // "login succeeded."
        [LoggerMessage(EventId = 1025, Level = LogLevel.Warning, Message = "Recovery code used to complete login for user {UserId} - consider regenerating recovery codes")]
        public static partial void RecoveryCodeUsed(this ILogger logger, Guid userId);
    }
}
