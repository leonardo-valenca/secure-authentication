using Application.Authentication.Commands;

namespace Application.Tests.Authentication.Commands;

public class RefreshCommandValidatorTests
{
    private readonly RefreshCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyRefreshToken_HasErrors()
    {
        Assert.False(_validator.Validate(new RefreshCommand("")).IsValid);
    }

    [Fact]
    public void Validate_NonEmptyRefreshToken_IsValid()
    {
        Assert.True(_validator.Validate(new RefreshCommand("a-refresh-token")).IsValid);
    }
}
