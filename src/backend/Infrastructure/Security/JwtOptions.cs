using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Security
{
    public sealed class JwtSigningKeyOptions
    {
        [Required]
        public required string Id { get; init; }

        [Required]
        public required string Key { get; init; }
    }

    public sealed class JwtOptions : IValidatableObject
    {
        public const string SectionName = "Jwt";

        [Required]
        public required string Issuer { get; init; }

        [Required]
        public required string Audience { get; init; }

        // Ordered: index 0 signs new tokens; every entry stays valid for verifying tokens already
        // issued with it, so a key can be rotated out without invalidating every active session.
        [MinLength(1)]
        public required IReadOnlyList<JwtSigningKeyOptions> SigningKeys { get; init; }

        [Range(1, int.MaxValue)]
        public int AccessTokenLifetimeMinutes { get; init; } = 15;

        // Per-key checks (a non-empty Id, and a Key long enough for HMACSHA256, 32 bytes/256 bits
        // minimum) live here rather than as attributes on JwtSigningKeyOptions itself: DataAnnotations
        // validation does not recurse into collection elements on its own, so this is what actually
        // makes those checks run when JwtOptions is validated at startup.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            for (var i = 0; i < SigningKeys.Count; i++)
            {
                var signingKey = SigningKeys[i];

                if (string.IsNullOrWhiteSpace(signingKey.Id))
                    yield return new ValidationResult($"{SectionName}:SigningKeys:{i}:Id must not be empty.", [nameof(SigningKeys)]);

                if (string.IsNullOrWhiteSpace(signingKey.Key) || signingKey.Key.Length < 32)
                {
                    yield return new ValidationResult(
                        $"{SectionName}:SigningKeys:{i}:Key must be at least 32 characters (HMACSHA256 needs a 256-bit key).",
                        [nameof(SigningKeys)]);
                }
            }
        }
    }
}
