using Application.Abstractions.Notifications;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;

namespace Api.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Pinned to the same image docker-compose.yml runs in a real deployment, rather than
    // whatever tag Testcontainers' now-obsolete parameterless constructor happened to default to.
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    // Two keys configured (like a mid-rotation deployment) so tests can prove both halves of key
    // rotation: a token signed with the non-current key still validates, and one signed with a
    // key that was never configured at all is rejected. See JwtKeyRotationTests.
    public const string CurrentSigningKeyId = "current";
    public const string CurrentSigningKey = "test-signing-key-current-not-for-production-use-in-ci";
    public const string PreviousSigningKeyId = "previous";
    public const string PreviousSigningKey = "test-signing-key-previous-not-for-production-use-in-ci";

    public CapturingEmailSender EmailSender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Database", _sqlContainer.GetConnectionString());

        // appsettings.Development.json intentionally ships with no real signing key (see README) -
        // the test host needs its own, just like a real deployment would supply one out-of-band.
        builder.UseSetting("Jwt:SigningKeys:0:Id", CurrentSigningKeyId);
        builder.UseSetting("Jwt:SigningKeys:0:Key", CurrentSigningKey);
        builder.UseSetting("Jwt:SigningKeys:1:Id", PreviousSigningKeyId);
        builder.UseSetting("Jwt:SigningKeys:1:Key", PreviousSigningKey);

        // The "auth" rate limit policy is tuned for production brute-force protection, not for a
        // test suite that legitimately issues many requests in a row, so raise it here to keep
        // functional tests from tripping on it.
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "1000");

        // Swap the real (logging-only) email sender for a spy so reset-password tests can read
        // the token that would otherwise only be visible to whatever inbox received it.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);
        });
    }

    Task IAsyncLifetime.InitializeAsync() => _sqlContainer.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
