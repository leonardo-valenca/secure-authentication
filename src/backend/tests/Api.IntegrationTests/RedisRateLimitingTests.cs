using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using StackExchange.Redis;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace Api.IntegrationTests;

/// <summary>
/// A dedicated factory and a deliberately low, isolated PermitLimit. The shared
/// CustomWebApplicationFactory raises the limit to 1000 specifically so the rest of the suite
/// doesn't trip on it, which is the opposite of what proving real enforcement here needs.
/// </summary>
public sealed class RedisRateLimitingTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine").Build();
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sqlContainer.StartAsync(), _redisContainer.StartAsync());

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Database", _sqlContainer.GetConnectionString());
            builder.UseSetting("ConnectionStrings:Redis", _redisContainer.GetConnectionString());
            builder.UseSetting("Jwt:SigningKeys:0:Id", "redis-rate-limiting-test");
            builder.UseSetting("Jwt:SigningKeys:0:Key", "test-signing-key-redis-rate-limiting-not-for-production-use");
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "3");
            builder.UseSetting("RateLimiting:Auth:WindowSeconds", "60");
        });

        // The CSRF cookie is Secure=true, and this test host talks plain HTTP internally, so a real
        // CookieContainer correctly refuses to send a Secure cookie back over a non-HTTPS
        // request, so relying on HttpClient's built-in cookie handling here would silently drop
        // it. A manual jar (same approach as AuthenticationFlowTests' CookieJar) sidesteps that
        // entirely by treating Set-Cookie/Cookie as plain headers, not something the transport
        // gets to apply Secure-flag policy to.
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();

        if (_factory is not null)
            await _factory.DisposeAsync();

        await _redisContainer.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task RateLimiter_WithRedisConfigured_EnforcesTheLimitAndPersistsCounterStateInRedis()
    {
        var jar = new CookieJar();

        var csrfResponse = await SendAsync(HttpMethod.Get, "/api/csrf-token", jar, csrfToken: null, body: null);
        Assert.Equal(HttpStatusCode.NoContent, csrfResponse.StatusCode);
        var csrfToken = jar.Get("XSRF-TOKEN")!;

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            var response = await SendAsync(HttpMethod.Post, "/api/authentication/forgot-password", jar, csrfToken,
                new { email = $"redis-rl-{i}@example.com" });
            statuses.Add(response.StatusCode);
        }

        // The first 3 fit inside PermitLimit=3; the 4th is the one that must be rejected.
        Assert.Equal(
            [HttpStatusCode.NoContent, HttpStatusCode.NoContent, HttpStatusCode.NoContent, HttpStatusCode.TooManyRequests],
            statuses);

        // Not just trusting the HTTP outcome, but directly confirming the counter actually lives in
        // Redis, which is the entire point of this feature (a shared counter across API
        // instances, not a per-process one). If the app had silently fallen back to the in-memory
        // limiter despite ConnectionStrings:Redis being set, the HTTP assertion above would still
        // pass but this one would fail with no keys found.
        await using var redisConnection = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());
        var redisServer = redisConnection.GetServer(redisConnection.GetEndPoints()[0]);
        var rateLimitKeys = redisServer.Keys(pattern: "rl:*").ToArray();
        Assert.NotEmpty(rateLimitKeys);
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

        var response = await _client!.SendAsync(request);
        jar.CaptureFrom(response);
        return response;
    }
}
