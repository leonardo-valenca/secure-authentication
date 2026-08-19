using Application.Authentication.Commands;

namespace Application.Tests.Authentication.Commands;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_InvalidEmail_HasErrors(string email)
    {
        Assert.False(_validator.Validate(new ForgotPasswordCommand(email)).IsValid);
    }

    [Fact]
    public void Validate_ValidEmail_IsValid()
    {
        Assert.True(_validator.Validate(new ForgotPasswordCommand("user@example.com")).IsValid);
    }
}
