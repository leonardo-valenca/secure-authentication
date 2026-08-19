using Application.Authentication.Commands;

namespace Application.Tests.Authentication.Commands;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyEmailOrPassword_HasErrors()
    {
        Assert.False(_validator.Validate(new LoginCommand("", "password")).IsValid);
        Assert.False(_validator.Validate(new LoginCommand("user@example.com", "")).IsValid);
    }

    [Fact]
    public void Validate_NonEmptyValues_IsValid()
    {
        Assert.True(_validator.Validate(new LoginCommand("user@example.com", "anything")).IsValid);
    }
}
