using Application.Abstractions.Notifications;
using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Infrastructure.Identity;
// using Infrastructure.Notifications;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("Database"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure()));

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

            // Tagged "ready" rather than left untagged: liveness ("is the process responsive") and
            // readiness ("can it actually serve a request") are different questions with different
            // consequences for a caller, an orchestrator that restarts on a failed liveness check
            // would restart a healthy process just because its database is briefly unreachable. See
            // Program.cs's /alive (no tag - passes with zero checks) and /ready (this tag) mappings.
            services.AddHealthChecks().AddDbContextCheck<AppDbContext>(tags: ["ready"]);

            services
                .AddIdentityCore<AppIdentityUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;

                    options.SignIn.RequireConfirmedEmail = true;

                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = 8;

                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders()
                .AddSignInManager();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            services.AddSingleton<ITokenHasher, TokenHasher>();
            services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

            // Real delivery only when SMTP is actually configured - keeps local dev, CI, and
            // integration tests working with zero external dependencies otherwise.
            // if (string.IsNullOrWhiteSpace(configuration["Smtp:Host"]))
            // {
            //     services.AddSingleton<IEmailSender, LoggingEmailSender>();
            // }
            // else
            // {
            //     // ValidateOnStart forces every option below to actually bind and validate during
            //     // host startup, not lazily on the first request that happens to need it, a missing
            //     // or malformed value fails loudly with a clear message the moment the container
            //     // starts, instead of surfacing as a confusing runtime error against whichever
            //     // request loses that lottery first.
            //     services.AddOptions<SmtpOptions>()
            //         .Bind(configuration.GetSection(SmtpOptions.SectionName))
            //         .ValidateDataAnnotations()
            //         .ValidateOnStart();
            //     services.AddSingleton<IEmailSender, SmtpEmailSender>();
            // }

            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // services.AddOptions<FrontendOptions>()
            //     .Bind(configuration.GetSection(FrontendOptions.SectionName))
            //     .ValidateDataAnnotations()
            //     .ValidateOnStart();

            // services.AddHostedService<RefreshTokenCleanupBackgroundService>();

            return services;
        }
    }
}
