using Application.Abstractions.Persistence;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext<AppIdentityUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
