using Domain.Common;
using System.Text.RegularExpressions;

namespace Domain.Users
{
    public sealed partial record Email
    {
        // Matches the nvarchar(256) column Identity generates for Email/NormalizedEmail
        // See UserErrors.EmailTooLong
        private const int MaxLength = 256;

        public string Value { get; }

        private Email(string value)
        {
            Value = value;
        }

        public static Result<Email> Create(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Failure<Email>(UserErrors.EmailEmpty);

            var normalized = value.Trim().ToLowerInvariant();

            if (normalized.Length > MaxLength)
                return Result.Failure<Email>(UserErrors.EmailTooLong);

            if (!EmailRegex().IsMatch(normalized))
                return Result.Failure<Email>(UserErrors.EmailInvalidFormat);

            return Result.Success(new Email(normalized));
        }

        /// <summary>
        /// Reconstructs an Email from a value already persisted elsewhere, trusting it was valid when stored
        /// Unlike Create, this never fails, so it's not for untrusted input.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Email FromPersistence(string value)
        {
            return new Email(value);
        }

        public override string ToString() => Value;

        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
        private static partial Regex EmailRegex();
    }
}
