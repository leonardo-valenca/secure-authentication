using System.Security.Cryptography;

namespace Api.Authentication
{
    public static class CsrfCookie
    {
        public const string CookieName = "XSRF-TOKEN";

        public const string HeaderName = "X-XSRF-TOKEN";

        public static void Issue(HttpResponse response)
        {
            // Hex, not base64: base64's '+', '/', '=' get percent-encoded in the Set-Cookie header,
            // but browsers' document.cookie does not decode that, a JS reader echoing the cookie
            // value verbatim into the header would then never match the server's decoded copy.
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            response.Cookies.Append(CookieName, token, new CookieOptions
            {
                // Must be readable by JS so the SPA can echo it back in a header, that's the point of
                // the double-submit pattern. It carries no authority on its own, unlike the auth cookies.
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(1),
                Path = "/"
            });
        }
    }
}
