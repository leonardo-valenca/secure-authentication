using System.Net;

namespace Api.IntegrationTests;

/// <summary>
/// /alive and /ready are what Caddy and the orchestrator actually poll (see docker-compose.yml's
/// healthcheck and proxy/Caddyfile), unauthenticated and unrated-limited by design. This just
/// proves both are actually reachable and report healthy against a real database/Redis, not just
/// that MapHealthChecks compiles.
/// </summary>
public sealed class HealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Alive_ReturnsOk()
    {
        var response = await _client.GetAsync("/alive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_WithDatabaseReachable_ReturnsOk()
    {
        var response = await _client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
