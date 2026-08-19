using Application.Authentication.Commands;

namespace Application.Tests.Authentication.Commands;

public class ResendConfirmationEmailCommandValidatorTests
{
    private readonly ResendConfirmationEmailCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_InvalidEmail_HasErrors(string email)
    {
        Assert.False(_validator.Validate(new ResendConfirmationEmailCommand(email)).IsValid);
    }

    [Fact]
    public void Validate_ValidEmail_IsValid()
    {
        Assert.True(_validator.Validate(new ResendConfirmationEmailCommand("user@example.com")).IsValid);
    }
}
