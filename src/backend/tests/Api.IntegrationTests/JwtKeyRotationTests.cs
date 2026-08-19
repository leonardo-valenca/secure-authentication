using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Api.IntegrationTests;

/// <summary>
/// CustomWebApplicationFactory configures two trusted signing keys (Current + Previous), the same
/// shape as a real deployment mid-rotation. These tests prove both halves of that design actually
/// work: a token signed with the non-current key still validates, and one signed with a key that
/// was never configured at all does not.
/// </summary>
public sealed class JwtKeyRotationTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Issuer = "SecureAuthentication"; // matches appsettings.json's Jwt:Issuer/Audience
    private const string Audience = "SecureAuthentication";

    private readonly HttpClient _client;

    public JwtKeyRotationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Me_WithAccessTokenSignedByPreviousKey_StillAuthenticates()
    {
        var token = IssueToken(CustomWebApplicationFactory.PreviousSigningKeyId, CustomWebApplicationFactory.PreviousSigningKey);

        var response = await SendWithAccessTokenAsync(token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithAccessTokenSignedByUnconfiguredKey_ReturnsUnauthorized()
    {
        var token = IssueToken("rogue", "a-key-never-configured-on-the-test-host-at-all-not-trusted");

        var response = await SendWithAccessTokenAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendWithAccessTokenAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/authentication/me");
        request.Headers.Add("Cookie", $"access_token={accessToken}");
        return await _client.SendAsync(request);
    }

    private static string IssueToken(string keyId, string key)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "rotation-test@example.com")
        };

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)) { KeyId = keyId },
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
