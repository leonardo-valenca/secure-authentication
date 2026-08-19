using System.Net;
using System.Text;
using System.Text.Json;

namespace Api.IntegrationTests;

public sealed class AuthenticationFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthenticationFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullAuthenticationLifecycle_BehavesCorrectly()
    {
        var jar = new CookieJar();
        var email = $"flow-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var registerResponse = await SendAsync(HttpMethod.Post, "/api/authentication/register", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var duplicateResponse = await SendAsync(HttpMethod.Post, "/api/authentication/register", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);

        await ConfirmEmailAsync(jar, csrfToken, email);

        var loginResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var meResponse = await SendAsync(HttpMethod.Get, "/api/authentication/me", jar, csrfToken: null, body: null);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var oldRefreshTokenCookie = jar.Get("refresh_token")!;

        var refreshResponse = await SendAsync(HttpMethod.Post, "/api/authentication/refresh", jar, csrfToken, body: null);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var newRefreshTokenCookie = jar.Get("refresh_token")!;
        Assert.NotEqual(oldRefreshTokenCookie, newRefreshTokenCookie);

        // Replaying the now-revoked pre-rotation token should be rejected...
        var replayJar = jar.Clone();
        replayJar.Set("refresh_token", oldRefreshTokenCookie);
        var replayResponse = await SendAsync(HttpMethod.Post, "/api/authentication/refresh", replayJar, csrfToken, body: null);
        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);

        // ...and it should have revoked the freshly-rotated token too (whole family killed).
        var refreshWithNewTokenResponse = await SendAsync(HttpMethod.Post, "/api/authentication/refresh", jar, csrfToken, body: null);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshWithNewTokenResponse.StatusCode);

        var loginAgainResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, loginAgainResponse.StatusCode);

        var logoutResponse = await SendAsync(HttpMethod.Post, "/api/authentication/logout", jar, csrfToken, body: null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var meAfterLogoutResponse = await SendAsync(HttpMethod.Get, "/api/authentication/me", jar, csrfToken: null, body: null);
        Assert.Equal(HttpStatusCode.Unauthorized, meAfterLogoutResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithoutCsrfHeader_ReturnsForbidden()
    {
        var jar = new CookieJar();
        await PrimeCsrfAsync(jar);

        var response = await SendAsync(HttpMethod.Post, "/api/authentication/register", jar, csrfToken: null,
            body: new { email = $"nocsrf-{Guid.NewGuid():N}@example.com", password = "StrongPass1!" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        var jar = new CookieJar();
        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var response = await SendAsync(HttpMethod.Post, "/api/authentication/register", jar, csrfToken,
            new { email = $"weak-{Guid.NewGuid():N}@example.com", password = "weak" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_AfterTooManyFailedAttempts_LocksAccountEvenWithCorrectPassword()
    {
        var jar = new CookieJar();
        var email = $"lockout-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var registerResponse = await SendAsync(HttpMethod.Post, "/api/authentication/register", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        await ConfirmEmailAsync(jar, csrfToken, email);

        // AddIdentityCore is configured with MaxFailedAccessAttempts = 5.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var failedAttempt = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
                new { email, password = "WrongPassword1!" });
            Assert.Equal(HttpStatusCode.BadRequest, failedAttempt.StatusCode);
        }

        var lockedOutResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.BadRequest, lockedOutResponse.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ThenResetPassword_AllowsLoginWithNewPasswordOnly()
    {
        var jar = new CookieJar();
        var email = $"reset-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var registerResponse = await SendAsync(HttpMethod.Post, "/api/authentication/register", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        await ConfirmEmailAsync(jar, csrfToken, email);

        var loginResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var refreshTokenBeforeReset = jar.Get("refresh_token")!;

        var forgotResponse = await SendAsync(HttpMethod.Post, "/api/authentication/forgot-password", jar, csrfToken,
            new { email });
        Assert.Equal(HttpStatusCode.NoContent, forgotResponse.StatusCode);

        var resetToken = _factory.EmailSender.LastResetToken;
        Assert.NotNull(resetToken);

        var resetResponse = await SendAsync(HttpMethod.Post, "/api/authentication/reset-password", jar, csrfToken,
            new { email, token = resetToken, newPassword = "NewStrongPass1!" });
        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        var oldPasswordLoginResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.BadRequest, oldPasswordLoginResponse.StatusCode);

        var newPasswordLoginResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "NewStrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, newPasswordLoginResponse.StatusCode);

        // The session that existed before the reset (e.g. an attacker's, if that's why the user
        // reset their password) must not survive it.
        var replayJar = jar.Clone();
        replayJar.Set("refresh_token", refreshTokenBeforeReset);
        var refreshWithPreResetTokenResponse = await SendAsync(HttpMethod.Post, "/api/authentication/refresh", replayJar, csrfToken, body: null);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshWithPreResetTokenResponse.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_StillReturnsNoContent()
    {
        var jar = new CookieJar();
        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var response = await SendAsync(HttpMethod.Post, "/api/authentication/forgot-password", jar, csrfToken,
            new { email = $"unknown-{Guid.NewGuid():N}@example.com" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ThenLogin_RequiresNewPassword()
    {
        var jar = new CookieJar();
        var email = $"change-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var registerResponse = await SendAsync(HttpMethod.Post, "/api/authentication/register", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        await ConfirmEmailAsync(jar, csrfToken, email);

        var loginResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var refreshTokenBeforeChange = jar.Get("refresh_token")!;

        var changeResponse = await SendAsync(HttpMethod.Post, "/api/authentication/change-password", jar, csrfToken,
            new { currentPassword = "StrongPass1!", newPassword = "NewStrongPass1!" });
        Assert.Equal(HttpStatusCode.NoContent, changeResponse.StatusCode);
        Assert.True(string.IsNullOrEmpty(jar.Get("access_token")));
        Assert.True(string.IsNullOrEmpty(jar.Get("refresh_token")));

        var oldPasswordLoginResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.BadRequest, oldPasswordLoginResponse.StatusCode);

        var newPasswordLoginResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "NewStrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, newPasswordLoginResponse.StatusCode);

        // The session active before the change (i.e. the one that was just used to change the
        // password) must not still work afterwards either, not just other/attacker sessions.
        var replayJar = jar.Clone();
        replayJar.Set("refresh_token", refreshTokenBeforeChange);
        var refreshWithPreChangeTokenResponse = await SendAsync(HttpMethod.Post, "/api/authentication/refresh", replayJar, csrfToken, body: null);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshWithPreChangeTokenResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuthentication_ReturnsUnauthorized()
    {
        var jar = new CookieJar();
        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var response = await SendAsync(HttpMethod.Post, "/api/authentication/change-password", jar, csrfToken,
            new { currentPassword = "Whatever1", newPassword = "NewStrongPass1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WrongCurrentPassword_LeavesAccountIntact()
    {
        var jar = new CookieJar();
        var email = $"delete-wrong-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var registerResponse = await SendAsync(HttpMethod.Post, "/api/authentication/register", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        await ConfirmEmailAsync(jar, csrfToken, email);

        var loginResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var deleteResponse = await SendAsync(HttpMethod.Post, "/api/authentication/delete-account", jar, csrfToken,
            new { currentPassword = "WrongPassword1!" });
        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);

        // The account must still exist, the same credentials still work.
        var jar2 = new CookieJar();
        await PrimeCsrfAsync(jar2);
        var csrfToken2 = jar2.Get("XSRF-TOKEN")!;
        var loginAfterFailedDeleteResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar2, csrfToken2,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, loginAfterFailedDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_CorrectCurrentPassword_DeletesAccountAndRevokesEverySession()
    {
        var jar = new CookieJar();
        var email = $"delete-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var registerResponse = await SendAsync(HttpMethod.Post, "/api/authentication/register", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        await ConfirmEmailAsync(jar, csrfToken, email);

        var loginResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var refreshTokenBeforeDelete = jar.Get("refresh_token")!;

        var deleteResponse = await SendAsync(HttpMethod.Post, "/api/authentication/delete-account", jar, csrfToken,
            new { currentPassword = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.True(string.IsNullOrEmpty(jar.Get("access_token")));
        Assert.True(string.IsNullOrEmpty(jar.Get("refresh_token")));

        var loginAfterDeleteResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.BadRequest, loginAfterDeleteResponse.StatusCode);

        // The refresh token cascade-deletes with the account (RefreshTokenConfiguration), so replaying
        // it should behave exactly like any other unknown token, not a reuse-detected one.
        var replayJar = jar.Clone();
        replayJar.Set("refresh_token", refreshTokenBeforeDelete);
        var refreshAfterDeleteResponse = await SendAsync(HttpMethod.Post, "/api/authentication/refresh", replayJar, csrfToken, body: null);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithoutAuthentication_ReturnsUnauthorized()
    {
        var jar = new CookieJar();
        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var response = await SendAsync(HttpMethod.Post, "/api/authentication/delete-account", jar, csrfToken,
            new { currentPassword = "Whatever1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExportAccountData_Authenticated_ReturnsAccountData()
    {
        var jar = new CookieJar();
        var email = $"export-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var registerResponse = await SendAsync(HttpMethod.Post, "/api/authentication/register", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        await ConfirmEmailAsync(jar, csrfToken, email);

        var loginResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var exportResponse = await SendAsync(HttpMethod.Get, "/api/authentication/me/export", jar, csrfToken: null, body: null);
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);

        var export = JsonSerializer.Deserialize<JsonElement>(await exportResponse.Content.ReadAsStringAsync());
        Assert.Equal(email, export.GetProperty("email").GetString());
        Assert.True(export.TryGetProperty("id", out _));
        Assert.True(export.TryGetProperty("createdAtUtc", out _));
    }

    [Fact]
    public async Task ExportAccountData_WithoutAuthentication_ReturnsUnauthorized()
    {
        var jar = new CookieJar();

        var response = await SendAsync(HttpMethod.Get, "/api/authentication/me/export", jar, csrfToken: null, body: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_RequiresEmailConfirmation_BeforeLoginSucceeds()
    {
        var jar = new CookieJar();
        var email = $"confirm-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var registerResponse = await SendAsync(HttpMethod.Post, "/api/authentication/register", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginBeforeConfirmResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.BadRequest, loginBeforeConfirmResponse.StatusCode);

        await ConfirmEmailAsync(jar, csrfToken, email);

        var loginAfterConfirmResponse = await SendAsync(HttpMethod.Post, "/api/authentication/login", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, loginAfterConfirmResponse.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_ReturnsBadRequest()
    {
        var jar = new CookieJar();
        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var response = await SendAsync(HttpMethod.Post, "/api/authentication/confirm-email", jar, csrfToken,
            new { email = $"nobody-{Guid.NewGuid():N}@example.com", token = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmationEmail_UnknownEmail_StillReturnsNoContent()
    {
        var jar = new CookieJar();
        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var response = await SendAsync(HttpMethod.Post, "/api/authentication/resend-confirmation", jar, csrfToken,
            new { email = $"unknown-{Guid.NewGuid():N}@example.com" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmationEmail_KnownUnconfirmedEmail_IssuesTokenThatConfirms()
    {
        var jar = new CookieJar();
        var email = $"resend-{Guid.NewGuid():N}@example.com";

        await PrimeCsrfAsync(jar);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var registerResponse = await SendAsync(HttpMethod.Post, "/api/authentication/register", jar, csrfToken,
            new { email, password = "StrongPass1!" });
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var resendResponse = await SendAsync(HttpMethod.Post, "/api/authentication/resend-confirmation", jar, csrfToken,
            new { email });
        Assert.Equal(HttpStatusCode.NoContent, resendResponse.StatusCode);

        await ConfirmEmailAsync(jar, csrfToken, email);
    }

    private async Task PrimeCsrfAsync(CookieJar jar)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/csrf-token", jar, csrfToken: null, body: null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>Registration no longer implies a confirmed account, so most flow tests need this before their first login.</summary>
    private async Task ConfirmEmailAsync(CookieJar jar, string csrfToken, string email)
    {
        var confirmationToken = _factory.EmailSender.LastConfirmationToken;
        Assert.NotNull(confirmationToken);

        var response = await SendAsync(HttpMethod.Post, "/api/authentication/confirm-email", jar, csrfToken,
            new { email, token = confirmationToken });
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
