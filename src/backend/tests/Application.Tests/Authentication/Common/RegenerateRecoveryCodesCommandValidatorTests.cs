using Application.Authentication.Commands;

namespace Application.Tests.Authentication.Commands;

public class RegenerateRecoveryCodesCommandValidatorTests
{
    private readonly RegenerateRecoveryCodesCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var result = _validator.Validate(new RegenerateRecoveryCodesCommand(Guid.NewGuid(), "CorrectPass1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyCurrentPassword_HasErrors()
    {
        var result = _validator.Validate(new RegenerateRecoveryCodesCommand(Guid.NewGuid(), ""));

        Assert.False(result.IsValid);
    }
}
