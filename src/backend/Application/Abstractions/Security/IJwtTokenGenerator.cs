using Domain.Users;

namespace Application.Abstractions.Security
{
    public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

    public interface IJwtTokenGenerator
    {
        AccessToken Generate(User user);
    }
}
