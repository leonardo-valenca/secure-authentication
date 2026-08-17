namespace Application.Authentication.Responses
{
    public sealed record AuthenticationResponse(
        Guid Id,
        string Email
    );
}
