namespace Application.Authentication.Responses
{
    /// <summary>
    /// Everything this app persists about a user, self-service exportable, the domain currently
    /// stores nothing beyond identity/authentication data, so this and AuthenticationResponse
    /// happen to carry the same fields today. Kept as its own type anyway: a project built on top
    /// of this base template will add its own user data (profile, preferences, ...), and that data
    /// belongs in this export without needing to touch AuthenticationResponse, which is a login
    /// response contract, not a data-export one.
    /// </summary>
    public sealed record AccountDataExport(
        Guid Id,
        string Email,
        DateTime CreatedAtUtc
    );
}
