namespace Infrastructure.Notifications
{
    internal static class PasswordResetLinkBuilder
    {
        /// <summary>Null if Frontend:BaseUrl isn't configured, callers decide how to degrade.</summary>
        public static string? Build(string baseUrl, string email, string resetToken)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            return $"{baseUrl.TrimEnd('/')}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(resetToken)}";
        }
    }
}
