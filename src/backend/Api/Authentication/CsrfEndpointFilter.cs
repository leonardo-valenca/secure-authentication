using System.Security.Cryptography;
using System.Text;

namespace Api.Authentication
{
    /// <summary>
    /// Double-submit cookie check for state-changing endpoints. A cross-site request will still carry
    /// our SameSite=Strict cookies... except SameSite=Strict already blocks that entirely; this exists
    /// as defense-in-depth for the browsers/proxies that don't fully honor SameSite. What it actually
    /// defends against: cross-origin JS cannot read this cookie (Same-Origin Policy), so it cannot
    /// reproduce the header value even if it could somehow get a request to fire.
    /// </summary>
    public sealed class CsrfEndpointFilter : IEndpointFilter
    {
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var request = context.HttpContext.Request;

            var cookieValue = request.Cookies[CsrfCookie.CookieName];
            var headerValue = request.Headers[CsrfCookie.HeaderName].ToString();

            if (string.IsNullOrEmpty(cookieValue) || string.IsNullOrEmpty(headerValue) ||
                !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(cookieValue), Encoding.UTF8.GetBytes(headerValue)))
            {
                return ValueTask.FromResult<object?>(Results.StatusCode(StatusCodes.Status403Forbidden));
            }

            return next(context);
        }
    }
}
