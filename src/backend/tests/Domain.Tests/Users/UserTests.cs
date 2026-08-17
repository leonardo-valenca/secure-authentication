using Domain.Users;

namespace Domain.Tests.Users;

public class UserTests
{
    [Fact]
    public void Create_SetsIdEmailAndCreationTimestamp()
    {
        var email = Email.Create("user@example.com").Value;
        var before = DateTime.UtcNow;
        var user = User.Create(email);
        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(email, user.Email);
        Assert.InRange(user.CreatedAtUtc, before, after);
    }

    [Fact]
    public void FromPersistence_RoundTripsGivenValues()
    {
        var id = Guid.NewGuid();
        var email = Email.Create("user@example.com").Value;
        var createdAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = User.FromPersistence(id, email, createdAtUtc);

        Assert.Equal(id, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal(createdAtUtc, user.CreatedAtUtc);
    }
}
