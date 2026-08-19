using Application.Authentication.Commands;

namespace Application.Tests.Authentication.Commands;

public class ConfirmEmailCommandValidatorTests
{
    private readonly ConfirmEmailCommandValidator _validator = new();

    [Theory]
    [InlineData("", "a-token")]
    [InlineData("not-an-email", "a-token")]
    [InlineData("user@example.com", "")]
    public void Validate_InvalidEmailOrToken_HasErrors(string email, string token)
    {
        Assert.False(_validator.Validate(new ConfirmEmailCommand(email, token)).IsValid);
    }

    [Fact]
    public void Validate_ValidEmailAndToken_IsValid()
    {
        Assert.True(_validator.Validate(new ConfirmEmailCommand("user@example.com", "a-token")).IsValid);
    }
}
