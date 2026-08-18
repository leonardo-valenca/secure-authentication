using System.Text;
using System.Threading.RateLimiting;
using Api.Authentication;
using Api.Endpoints.Authentication;
using Application.Abstractions.Behaviors;
using Application.Authentication;
using FluentValidation;
using Infrastructure;
using Infrastructure.Persistence;
using Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RedisRateLimiting;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using StackExchange.Redis;

// A bootstrap logger covers failures that happen before the real Serilog pipeline (built from
// configuration further down) exists, a bad connection string or a config binding error would
// otherwise be lost, since nothing would be listening yet to log it.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // The plain static-Log.Logger form, not the lazy (context, services, configuration) => ...
    // overload: that one builds a ReloadableLogger which can only be frozen once, and
    // WebApplicationFactory<Program> (see the integration test suite) re-enters this same
    // top-level Program in-process, first via HostFactoryResolver to discover services (caught
    // above as HostAbortedException), then again to actually build the test host. A second
    // freeze attempt on the same reloadable logger throws; reassigning Log.Logger outright on
    // each entry doesn't have that problem.
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .WriteTo.Console(builder.Environment.IsDevelopment()
            ? new Serilog.Formatting.Display.MessageTemplateTextFormatter(
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            : new CompactJsonFormatter())
        .CreateLogger();

    builder.Host.UseSerilog();

    builder.Services.AddMediator(options =>
    {
        options.Assemblies = [typeof(Application.AssemblyReference)];
        options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
        options.ServiceLifetime = ServiceLifetime.Scoped;
    });
    builder.Services.AddValidatorsFromAssembly(typeof(Application.AssemblyReference).Assembly);
    builder.Services.AddInfrastructure(builder.Configuration);
    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            // Exception details (message/stack trace) are only useful, and only safe, locally.
            if (!builder.Environment.IsDevelopment())
                return;

            var exception = context.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (exception is not null)
                context.ProblemDetails.Extensions["exception"] = exception.ToString();
        };
    });

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Claim types are kept exactly as issued (e.g. "sub", "email") instead of ASP.NET Core's
            // default remapping to long http://schemas.xmlsoap.org/... ClaimTypes URIs.
            options.MapInboundClaims = false;

            // Every configured key stays valid for verification, not just the one currently signing
            // new tokens (see JwtTokenGenerator), so a key can be rotated out without invalidating
            // every session signed with it up to access-token-lifetime ago.
            var signingKeys = builder.Configuration.GetSection("Jwt:SigningKeys").GetChildren()
                .Select(section => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Key"]!)) { KeyId = section["Id"] })
                .ToArray();

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKeys = signingKeys,
                // Pinned explicitly rather than trusting the token's own header: HMAC is the only
                // algorithm we ever sign with, so nothing else should ever be accepted as valid.
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.Zero
            };

            // The access token travels as an HttpOnly cookie, never as an Authorization header,
            // so it never touches JavaScript, read it from there instead.
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.TryGetValue(AuthCookies.AccessToken, out var token))
                        context.Token = token;

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        // The reverse proxy container is the only thing that can reach this API at all, its own
        // port isn't published to the host (see docker-compose.yml), so trusting forwarded headers
        // unconditionally is safe here: there's no other path by which they could be spoofed.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        var permitLimit = builder.Configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
        var windowSeconds = builder.Configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60);

        options.OnRejected = (context, cancellationToken) =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var remoteIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            logger.RateLimitExceeded(remoteIp, context.HttpContext.Request.Path);
            return ValueTask.CompletedTask;
        };

        // A single-instance deployment (this project's own docker-compose.yml) works fine with
        // the in-memory limiter below, every request lands on the same process either way. It
        // stops being correct the moment there's more than one API instance behind a load
        // balancer: each instance would keep its own independent count, silently multiplying the
        // effective limit by the instance count. Redis-backed limiting closes that gap, same
        // policy name, same PermitLimit/Window semantics, just a shared counter instead of a
        // per-process one, and only turns on when ConnectionStrings:Redis is actually configured,
        // same optional-infrastructure pattern as SmtpEmailSender falling back to a logging stub.
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);

            options.AddPolicy(AuthenticationEndpoints.AuthRateLimiterPolicy, httpContext => RedisRateLimitPartition.GetFixedWindowRateLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new RedisFixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    ConnectionMultiplexerFactory = () => connectionMultiplexer
                }));
        }
        else
        {
            options.AddPolicy(AuthenticationEndpoints.AuthRateLimiterPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueLimit = 0
                }));
        }
    });

    var app = builder.Build();

    // Convenient for a single-instance demo/dev deployment; a multi-instance production rollout
    // would run migrations as a separate release step instead of racing every instance on startup.
    using (var migrationScope = app.Services.CreateScope())
    {
        var dbContext = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
    
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }
    else
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapAuthenticationEndpoints();

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    // HostAbortedException is excluded deliberately: WebApplicationFactory<Program> (see the
    // integration test suite) builds and immediately tears down a host to discover services,
    // which throws exactly this by design. It isn't a real startup failure and shouldn't be
    // logged as one.
    Log.Fatal(exception, "Clean Authentication API terminated unexpectedly during startup");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;