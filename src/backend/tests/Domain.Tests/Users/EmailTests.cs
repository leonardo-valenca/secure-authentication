using Domain.Users;

namespace Domain.Tests.Users;

public class EmailTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespace_ReturnsEmailEmptyFailure(string? value)
    {
        var result = Email.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailEmpty, result.Error);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at.com")]
    [InlineData("no-domain@")]
    public void Create_InvalidFormat_ReturnsInvalidFormatFailure(string value)
    {
        var result = Email.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailInvalidFormat, result.Error);
    }

    [Fact]
    public void Create_ValidEmail_NormalizesToLowercaseAndTrims()
    {
        var result = Email.Create("  User@Example.COM  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("user@example.com", result.Value.Value);
    }

    [Fact]
    public void Create_ExactlyMaxLength_Succeeds()
    {
        // 244 + "@example.com" (12) = 256. The nvarchar(256) column's exact limit.
        var email = $"{new string('a', 244)}@example.com";
        var result = Email.Create(email);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_ExceedsMaxLength_ReturnsEmailTooLongFailure()
    {
        // One character past the nvarchar(256) column this is meant to fit in
        // see UserErrors.EmailTooLong for why this can't be allowed to reach the database at all.
        var email = $"{new string('a', 245)}@example.com";
        var result = Email.Create(email);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailTooLong, result.Error);
    }
}
