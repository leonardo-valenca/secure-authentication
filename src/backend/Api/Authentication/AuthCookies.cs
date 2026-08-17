using Application.Authentication.Commands;

namespace Api.Authentication
{
    public static class AuthCookies
    {
        public const string AccessToken = "access_token";

        public const string RefreshToken = "refresh_token";

        public static CookieOptions Build(DateTime expiresAtUtc) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAtUtc,
            Path = "/"
        };

        /// <summary>Shared by every endpoint that ends in a completed login, a normal one and the second step of a 2FA one alike.</summary>
        public static void SetLoginCookies(HttpResponse response, LoginResult loginResult)
        {
            response.Cookies.Append(AccessToken, loginResult.AccessToken, Build(loginResult.AccessTokenExpiresAtUtc));
            response.Cookies.Append(RefreshToken, loginResult.RefreshToken, Build(loginResult.RefreshTokenExpiresAtUtc));
        }

        public static void Clear(HttpResponse response)
        {
            var options = new CookieOptions { Path = "/" };
            response.Cookies.Delete(AccessToken, options);
            response.Cookies.Delete(RefreshToken, options);
        }
    }
}
