namespace Domain.Authentication
{
    public sealed class RefreshToken
    {
        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public string TokenHash { get; private set; } = null!;

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime ExpiresAtUtc { get; private set; }

        public DateTime? RevokedAtUtc { get; private set; }

        public Guid? ReplacedByTokenId { get; private set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

        public bool IsRevoked => RevokedAtUtc is not null;

        public bool IsActive => !IsRevoked && !IsExpired;

        private RefreshToken() { }

        private RefreshToken(Guid id, Guid userId, string tokenHash, DateTime createdAtUtc, DateTime expiresAtUtc)
        {
            Id = id;
            UserId = userId;
            TokenHash = tokenHash;
            CreatedAtUtc = createdAtUtc;
            ExpiresAtUtc = expiresAtUtc;
        }

        public static RefreshToken Create(Guid userId, string tokenHash, TimeSpan lifetime)
        {
            var now = DateTime.UtcNow;
            return new RefreshToken(Guid.NewGuid(), userId, tokenHash, now, now.Add(lifetime));
        }

        /// <summary>
        /// Revoking without a replacement marks reuse of this token (e.g. on the next refresh attempt)
        /// as a signal to invalidate the whole chain.
        /// </summary>
        public void Revoke(Guid? replacedByTokenId = null)
        {
            RevokedAtUtc = DateTime.UtcNow;
            ReplacedByTokenId = replacedByTokenId;
        }
    }
}
