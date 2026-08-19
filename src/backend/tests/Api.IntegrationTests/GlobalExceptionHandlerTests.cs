using System.Net;
using System.Text;
using System.Text.Json;
using Application.Abstractions.Persistence;
using Application.Authentication.Responses;
using Domain.Common;
using Domain.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.IntegrationTests;

/// <summary>
/// There's no natural way to trigger a genuine unhandled exception through the rest of this suite -
/// every expected failure in this codebase returns a Result instead of throwing (see
/// Domain.Common.Result), which is exactly why GlobalExceptionHandler had zero coverage before this.
/// Forces one deliberately, by swapping in a repository that throws, to prove the handler actually
/// intercepts it and produces a 500 ProblemDetails response, not just that it compiles.
/// </summary>
public sealed class GlobalExceptionHandlerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GlobalExceptionHandlerTests(CustomWebApplicationFactory factory)
    {
        var throwingFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserRepository>();
                services.AddScoped<IUserRepository, ThrowingUserRepository>();
            });
        });

        _client = throwingFactory.CreateClient();
    }

    [Fact]
    public async Task UnhandledException_ReturnsProblemDetailsFiveHundred()
    {
        var jar = new CookieJar();

        var csrfResponse = await _client.GetAsync("/api/csrf-token");
        jar.CaptureFrom(csrfResponse);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/register")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { email = "exception-handler-test@example.com", password = "StrongPass1!" }),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("Cookie", jar.ToHeader());
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        using var problemDetails = JsonDocument.Parse(body);
        Assert.Equal(500, problemDetails.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred.", problemDetails.RootElement.GetProperty("title").GetString());
    }

    private sealed class ThrowingUserRepository : IUserRepository
    {
        private static InvalidOperationException Failure() => new("Simulated infrastructure failure for GlobalExceptionHandlerTests.");

        public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken) => throw Failure();
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => throw Failure();
        public Task<Result<User>> CreateAsync(Email email, string password, CancellationToken cancellationToken) => throw Failure();
        public Task<Result<CredentialVerificationResult>> VerifyCredentialsAsync(Email email, string password, CancellationToken cancellationToken) => throw Failure();
        public Task<string?> GeneratePasswordResetTokenAsync(Email email, CancellationToken cancellationToken) => throw Failure();
        public Task<Result<Guid>> ResetPasswordAsync(Email email, string token, string newPassword, CancellationToken cancellationToken) => throw Failure();
        public Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken) => throw Failure();
        public Task<string?> GenerateEmailConfirmationTokenAsync(Email email, CancellationToken cancellationToken) => throw Failure();
        public Task<Result> ConfirmEmailAsync(Email email, string token, CancellationToken cancellationToken) => throw Failure();
        public Task<Result> DeleteAccountAsync(Guid userId, string currentPassword, CancellationToken cancellationToken) => throw Failure();
        public Task<Result<TwoFactorSetup>> GenerateTwoFactorSetupAsync(Guid userId, CancellationToken cancellationToken) => throw Failure();
        public Task<Result<IReadOnlyList<string>>> EnableTwoFactorAsync(Guid userId, string code, CancellationToken cancellationToken) => throw Failure();
        public Task<Result> DisableTwoFactorAsync(Guid userId, string currentPassword, CancellationToken cancellationToken) => throw Failure();
        public Task<Result<IReadOnlyList<string>>> RegenerateRecoveryCodesAsync(Guid userId, string currentPassword, CancellationToken cancellationToken) => throw Failure();
        public Task<Result<User>> VerifyTwoFactorCodeAsync(Guid userId, string code, CancellationToken cancellationToken) => throw Failure();
        public Task<Result<bool>> GetTwoFactorStatusAsync(Guid userId, CancellationToken cancellationToken) => throw Failure();
    }
}
