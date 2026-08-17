using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity
{
    public sealed class AppIdentityUser : IdentityUser<Guid>
    {
        public DateTime CreatedAtUtc { get; set; }
    }
}
