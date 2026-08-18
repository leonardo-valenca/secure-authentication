using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Notifications
{
    public sealed class SmtpOptions
    {
        public const string SectionName = "Smtp";

        [Required]
        public required string Host { get; init; }

        [Range(1, 65535)]
        public int Port { get; init; } = 587;

        public string? Username { get; init; }

        public string? Password { get; init; }

        [Required]
        [EmailAddress]
        public required string FromAddress { get; init; }

        [Required]
        public string FromName { get; init; } = "Clean Authentication";
    }
}
