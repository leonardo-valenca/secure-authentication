using System.Net;
using System.Text;
using System.Text.Json;
using OtpNet;

namespace Api.IntegrationTests;

public sealed class TwoFactorAuthenticationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TwoFactorAuthenticationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EnableTwoFactor_ThenLogin_RequiresCodeBeforeCompletingAndThenSucceeds()
    {
        var jar = new CookieJar();
        var email = $"2fa-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        await RegisterAndConfirmAsync(jar, csrfToken, email);
        await LoginAsync(jar, csrfToken, email, "StrongPass1!");

        var sharedKey = await SetupTwoFactorAsync(jar, csrfToken);

        var enableResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/enable", jar, csrfToken,
            new { code = GenerateTotpCode(sharedKey) });
        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        var recoveryCodes = await ReadRecoveryCodesAsync(enableResponse);
        Assert.NotEmpty(recoveryCodes);

        // Logging in again now requires the second factor, so no access/refresh cookies yet.
        var jar2 = new CookieJar();
        await PrimeCsrfAsync(jar2);
        var csrfToken2 = jar2.Get("XSRF-TOKEN")!;

        var loginResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar2, csrfToken2,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginBody = JsonSerializer.Deserialize<JsonElement>(await loginResponse.Content.ReadAsStringAsync());
        Assert.True(loginBody.GetProperty("requiresTwoFactor").GetBoolean());
        Assert.True(string.IsNullOrEmpty(jar2.Get("access_token")));
        Assert.NotNull(jar2.Get("mfa_challenge"));

        var completeResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/login", jar2, csrfToken2,
            new { code = GenerateTotpCode(sharedKey) });
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        Assert.False(string.IsNullOrEmpty(jar2.Get("access_token")));

        var meResponse = await SendAsync(HttpMethod.Get, "/api/authentication/me", jar2, csrfToken: null, body: null);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_WrongCode_ReturnsBadRequestAndNoCookies()
    {
        var jar = new CookieJar();
        var email = $"2fa-wrong-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        await RegisterAndConfirmAsync(jar, csrfToken, email);
        await LoginAsync(jar, csrfToken, email, "StrongPass1!");
        var sharedKey = await SetupTwoFactorAsync(jar, csrfToken);
        await SendAsync(HttpMethod.Post, "/api/authentication/2fa/enable", jar, csrfToken, new { code = GenerateTotpCode(sharedKey) });

        var jar2 = new CookieJar();
        await PrimeCsrfAsync(jar2);
        var csrfToken2 = jar2.Get("XSRF-TOKEN")!;
        await SendAsync(HttpMethod.Post, "/api/authentication/login", jar2, csrfToken2, new { email, password = "StrongPass1!" });

        var wrongCodeResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/login", jar2, csrfToken2,
            new { code = "000000" });

        Assert.Equal(HttpStatusCode.BadRequest, wrongCodeResponse.StatusCode);
        Assert.True(string.IsNullOrEmpty(jar2.Get("access_token")));
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_WithoutChallengeCookie_ReturnsBadRequest()
    {
        var jar = new CookieJar();
        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var response = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/login", jar, csrfToken, new { code = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EnableTwoFactor_ThenLoginWithRecoveryCode_Succeeds()
    {
        var jar = new CookieJar();
        var email = $"2fa-recovery-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        await RegisterAndConfirmAsync(jar, csrfToken, email);
        await LoginAsync(jar, csrfToken, email, "StrongPass1!");
        var sharedKey = await SetupTwoFactorAsync(jar, csrfToken);

        var enableResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/enable", jar, csrfToken,
            new { code = GenerateTotpCode(sharedKey) });
        var recoveryCode = (await ReadRecoveryCodesAsync(enableResponse))[0];

        var jar2 = new CookieJar();
        await PrimeCsrfAsync(jar2);
        var csrfToken2 = jar2.Get("XSRF-TOKEN")!;
        await SendAsync(HttpMethod.Post, "/api/authentication/login", jar2, csrfToken2, new { email, password = "StrongPass1!" });

        var completeResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/login", jar2, csrfToken2,
            new { code = recoveryCode });
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        Assert.False(string.IsNullOrEmpty(jar2.Get("access_token")));

        // Recovery codes are single-use, so the same one must not work a second time.
        var jar3 = new CookieJar();
        await PrimeCsrfAsync(jar3);
        var csrfToken3 = jar3.Get("XSRF-TOKEN")!;
        await SendAsync(HttpMethod.Post, "/api/authentication/login", jar3, csrfToken3, new { email, password = "StrongPass1!" });
        var replayResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/login", jar3, csrfToken3,
            new { code = recoveryCode });
        Assert.Equal(HttpStatusCode.BadRequest, replayResponse.StatusCode);
    }

    [Fact]
    public async Task DisableTwoFactor_WrongPassword_LeavesTwoFactorEnabled()
    {
        var jar = new CookieJar();
        var email = $"2fa-disable-wrong-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        await RegisterAndConfirmAsync(jar, csrfToken, email);
        await LoginAsync(jar, csrfToken, email, "StrongPass1!");
        var sharedKey = await SetupTwoFactorAsync(jar, csrfToken);
        await SendAsync(HttpMethod.Post, "/api/authentication/2fa/enable", jar, csrfToken, new { code = GenerateTotpCode(sharedKey) });

        var disableResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/disable", jar, csrfToken,
            new { currentPassword = "WrongPassword1!" });
        Assert.Equal(HttpStatusCode.BadRequest, disableResponse.StatusCode);

        // Still enabled, so a fresh login should still demand a code.
        var jar2 = new CookieJar();
        await PrimeCsrfAsync(jar2);
        var csrfToken2 = jar2.Get("XSRF-TOKEN")!;
        var loginResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar2, csrfToken2,
            new { email, password = "StrongPass1!" });
        var loginBody = JsonSerializer.Deserialize<JsonElement>(await loginResponse.Content.ReadAsStringAsync());
        Assert.True(loginBody.GetProperty("requiresTwoFactor").GetBoolean());
    }

    [Fact]
    public async Task DisableTwoFactor_CorrectPassword_LoginNoLongerRequiresCode()
    {
        var jar = new CookieJar();
        var email = $"2fa-disable-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        await RegisterAndConfirmAsync(jar, csrfToken, email);
        await LoginAsync(jar, csrfToken, email, "StrongPass1!");
        var sharedKey = await SetupTwoFactorAsync(jar, csrfToken);
        await SendAsync(HttpMethod.Post, "/api/authentication/2fa/enable", jar, csrfToken, new { code = GenerateTotpCode(sharedKey) });

        var disableResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/disable", jar, csrfToken,
            new { currentPassword = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.NoContent, disableResponse.StatusCode);

        var jar2 = new CookieJar();
        await PrimeCsrfAsync(jar2);
        var csrfToken2 = jar2.Get("XSRF-TOKEN")!;
        var loginResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar2, csrfToken2,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginBody = JsonSerializer.Deserialize<JsonElement>(await loginResponse.Content.ReadAsStringAsync());
        Assert.False(loginBody.GetProperty("requiresTwoFactor").GetBoolean());
        Assert.False(string.IsNullOrEmpty(jar2.Get("access_token")));
    }

    [Fact]
    public async Task GetTwoFactorStatus_ReflectsEnableAndDisable()
    {
        var jar = new CookieJar();
        var email = $"2fa-status-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        await RegisterAndConfirmAsync(jar, csrfToken, email);
        await LoginAsync(jar, csrfToken, email, "StrongPass1!");

        Assert.False(await GetTwoFactorStatusAsync(jar));

        var sharedKey = await SetupTwoFactorAsync(jar, csrfToken);
        await SendAsync(HttpMethod.Post, "/api/authentication/2fa/enable", jar, csrfToken, new { code = GenerateTotpCode(sharedKey) });
        Assert.True(await GetTwoFactorStatusAsync(jar));

        await SendAsync(HttpMethod.Post, "/api/authentication/2fa/disable", jar, csrfToken, new { currentPassword = "StrongPass1!" });
        Assert.False(await GetTwoFactorStatusAsync(jar));
    }

    [Fact]
    public async Task RegenerateRecoveryCodes_WrongPassword_LeavesOldCodesValid()
    {
        var jar = new CookieJar();
        var email = $"2fa-regen-wrong-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        await RegisterAndConfirmAsync(jar, csrfToken, email);
        await LoginAsync(jar, csrfToken, email, "StrongPass1!");
        var sharedKey = await SetupTwoFactorAsync(jar, csrfToken);
        var enableResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/enable", jar, csrfToken,
            new { code = GenerateTotpCode(sharedKey) });
        var oldRecoveryCode = (await ReadRecoveryCodesAsync(enableResponse))[0];

        var regenerateResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/recovery-codes/regenerate", jar, csrfToken,
            new { currentPassword = "WrongPassword1!" });
        Assert.Equal(HttpStatusCode.BadRequest, regenerateResponse.StatusCode);

        // Regeneration failed, so the original recovery code must still log in.
        var jar2 = new CookieJar();
        await PrimeCsrfAsync(jar2);
        var csrfToken2 = jar2.Get("XSRF-TOKEN")!;
        await SendAsync(HttpMethod.Post, "/api/authentication/login", jar2, csrfToken2, new { email, password = "StrongPass1!" });
        var completeResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/login", jar2, csrfToken2,
            new { code = oldRecoveryCode });
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
    }

    [Fact]
    public async Task RegenerateRecoveryCodes_CorrectPassword_InvalidatesOldCodesAndIssuesNewOnes()
    {
        var jar = new CookieJar();
        var email = $"2fa-regen-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        await RegisterAndConfirmAsync(jar, csrfToken, email);
        await LoginAsync(jar, csrfToken, email, "StrongPass1!");
        var sharedKey = await SetupTwoFactorAsync(jar, csrfToken);
        var enableResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/enable", jar, csrfToken,
            new { code = GenerateTotpCode(sharedKey) });
        var oldRecoveryCode = (await ReadRecoveryCodesAsync(enableResponse))[0];

        var regenerateResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/recovery-codes/regenerate", jar, csrfToken,
            new { currentPassword = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, regenerateResponse.StatusCode);
        var newRecoveryCodes = await ReadRecoveryCodesAsync(regenerateResponse);
        Assert.NotEmpty(newRecoveryCodes);
        Assert.DoesNotContain(oldRecoveryCode, newRecoveryCodes);

        // The pre-regeneration code must no longer work.
        var jar2 = new CookieJar();
        await PrimeCsrfAsync(jar2);
        var csrfToken2 = jar2.Get("XSRF-TOKEN")!;
        await SendAsync(HttpMethod.Post, "/api/authentication/login", jar2, csrfToken2, new { email, password = "StrongPass1!" });
        var oldCodeResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/login", jar2, csrfToken2,
            new { code = oldRecoveryCode });
        Assert.Equal(HttpStatusCode.BadRequest, oldCodeResponse.StatusCode);

        // A freshly regenerated code must work.
        var jar3 = new CookieJar();
        await PrimeCsrfAsync(jar3);
        var csrfToken3 = jar3.Get("XSRF-TOKEN")!;
        await SendAsync(HttpMethod.Post, "/api/authentication/login", jar3, csrfToken3, new { email, password = "StrongPass1!" });
        var newCodeResponse = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/login", jar3, csrfToken3,
            new { code = newRecoveryCodes[0] });
        Assert.Equal(HttpStatusCode.OK, newCodeResponse.StatusCode);
    }

    private async Task<bool> GetTwoFactorStatusAsync(CookieJar jar)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/authentication/2fa/status", jar, csrfToken: null, body: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        return body.GetProperty("enabled").GetBoolean();
    }

    private static string GenerateTotpCode(string base32SharedKey)
    {
        var totp = new Totp(Base32Encoding.ToBytes(base32SharedKey));
        return totp.ComputeTotp();
    }

    private async Task<string> SetupTwoFactorAsync(CookieJar jar, string csrfToken)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/authentication/2fa/setup", jar, csrfToken, body: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        return body.GetProperty("sharedKey").GetString()!;
    }

    private static async Task<IReadOnlyList<string>> ReadRecoveryCodesAsync(HttpResponseMessage response)
    {
        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        return body.GetProperty("recoveryCodes").EnumerateArray().Select(e => e.GetString()!).ToList();
    }

    private async Task RegisterAndConfirmAsync(CookieJar jar, string csrfToken, string email)
    {
        var registerResponse = await SendAsync(HttpMethod.Post, "/api/authentication/register", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var confirmationToken = _factory.EmailSender.LastConfirmationToken;
        Assert.NotNull(confirmationToken);

        var confirmResponse = await SendAsync(HttpMethod.Post, "/api/authentication/confirm-email", jar, csrfToken,
            new { email, token = confirmationToken });
        Assert.Equal(HttpStatusCode.NoContent, confirmResponse.StatusCode);
    }

    private async Task LoginAsync(CookieJar jar, string csrfToken, string email, string password)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken, new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task PrimeCsrfAsync(CookieJar jar)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/csrf-token", jar, csrfToken: null, body: null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, CookieJar jar, string? csrfToken, object? body)
    {
        var request = new HttpRequestMessage(method, url);

        var cookieHeader = jar.ToHeader();
        if (!string.IsNullOrEmpty(cookieHeader))
            request.Headers.Add("Cookie", cookieHeader);

        if (csrfToken is not null)
            request.Headers.Add("X-XSRF-TOKEN", csrfToken);

        if (body is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request);
        jar.CaptureFrom(response);
        return response;
    }
}
