using Domain.Common;

namespace Domain.Authentication
{
    public static class RefreshTokenErrors
    {
        public static readonly Error NotFound = new("RefreshToken.NotFound", "Refresh token is invalid.");

        public static readonly Error Expired = new("RefreshToken.Expired", "Refresh token has expired.");

        public static readonly Error ReuseDetected = new("RefreshToken.ReuseDetected", "Refresh token reuse detected; all sessions have been revoked.");
    }
}
