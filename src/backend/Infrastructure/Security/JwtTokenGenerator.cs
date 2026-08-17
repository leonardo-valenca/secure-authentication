using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Abstractions.Security;
using Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security
{
    internal sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
    {
        private static readonly JwtSecurityTokenHandler TokenHandler = new();

        public AccessToken Generate(User user)
        {
            var jwtOptions = options.Value;
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(jwtOptions.AccessTokenLifetimeMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Index 0 is always the current signing key - older keys stay in config only to keep
            // validating tokens issued before a rotation, never to sign new ones.
            var currentSigningKey = jwtOptions.SigningKeys[0];
            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(currentSigningKey.Key)) { KeyId = currentSigningKey.Id },
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtOptions.Issuer,
                audience: jwtOptions.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: signingCredentials);

            return new AccessToken(TokenHandler.WriteToken(token), expiresAtUtc);
        }
    }
}
