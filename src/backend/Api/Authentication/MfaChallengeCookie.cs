using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Api.Authentication
{
    /// <summary>
    /// The bridge between "password verified" and "token issued" while a second factor is
    /// pending. HttpOnly, same as the real access/refresh cookies, the user id it carries never
    /// needs to touch JavaScript, only get echoed back by the browser on the next request.
    /// Server-issued and server-verified only; the client never reads or constructs its value.
    /// </summary>
    public static class MfaChallengeCookie
    {
        public const string CookieName = "mfa_challenge";

        private const string Purpose = "MfaChallenge";

        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

        public static void Issue(HttpResponse response, IDataProtectionProvider dataProtectionProvider, Guid userId)
        {
            var protector = dataProtectionProvider.CreateProtector(Purpose).ToTimeLimitedDataProtector();
            var expiresAtUtc = DateTimeOffset.UtcNow.Add(Lifetime);
            var protectedPayload = protector.Protect(userId.ToString(), expiresAtUtc);

            response.Cookies.Append(CookieName, protectedPayload, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAtUtc,
                Path = "/"
            });
        }

        /// <summary>Null if the cookie is missing, expired, or fails to unprotect (tampered with or signed by a different key).</summary>
        public static Guid? Validate(HttpRequest request, IDataProtectionProvider dataProtectionProvider)
        {
            if (!request.Cookies.TryGetValue(CookieName, out var protectedPayload) || string.IsNullOrEmpty(protectedPayload))
                return null;

            var protector = dataProtectionProvider.CreateProtector(Purpose).ToTimeLimitedDataProtector();

            try
            {
                return Guid.Parse(protector.Unprotect(protectedPayload));
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        public static void Clear(HttpResponse response)
        {
            response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
        }
    }
}
