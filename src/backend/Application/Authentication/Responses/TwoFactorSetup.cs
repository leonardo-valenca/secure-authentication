namespace Application.Authentication.Responses
{
    /// <summary>AuthenticatorUri is the otpauth:// URI an authenticator app scans as a QR code; SharedKey is the same secret, for manual entry.</summary>
    public sealed record TwoFactorSetup(string SharedKey, string AuthenticatorUri);
}
