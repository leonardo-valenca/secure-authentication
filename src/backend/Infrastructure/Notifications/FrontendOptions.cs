using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Notifications
{
    public sealed class FrontendOptions
    {
        public const string SectionName = "Frontend";

        /// <summary>Origin the SPA is served from, used to build links embedded in outgoing emails.</summary>
        [Required]
        [Url]
        public string BaseUrl { get; init; } = string.Empty;
    }
}
