namespace Infrastructure.Notifications
{
    internal static class EmailConfirmationLinkBuilder
    {
        /// <summary>Null if Frontend:BaseUrl isn't configured, callers decide how to degrade.</summary>
        public static string? Build(string baseUrl, string email, string confirmationToken)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return null;

            return $"{baseUrl.TrimEnd('/')}/confirm-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(confirmationToken)}";
        }
    }
}
