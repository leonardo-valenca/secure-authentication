namespace Domain.Users
{
    public sealed class User
    {
        public Guid Id { get; private set; }

        public Email Email { get; private set; } = null!;

        public DateTime CreatedAtUtc { get; private set; }

        private User(Guid id, Email email, DateTime createdAtUtc)
        {
            Id = id;
            Email = email;
            CreatedAtUtc = createdAtUtc;
        }

        public static User Create(Email email)
        {
            return new User(Guid.NewGuid(), email, DateTime.UtcNow);
        }

        /// <summary>
        /// Reconstructs a User from data already persisted elsewhere (Identity owns the actual
        /// storage) distinct from Create because this isn't bringing a new user into existence.
        /// </summary>
        public static User FromPersistence(Guid id, Email email, DateTime createdAtUtc)
        {
            return new User(id, email, createdAtUtc);
        }
    }
}
