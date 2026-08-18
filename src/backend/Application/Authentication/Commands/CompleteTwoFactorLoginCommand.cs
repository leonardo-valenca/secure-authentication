using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    /// <summary>
    /// UserId comes from the mfa_challenge cookie the endpoint already validated, not from the
    /// request body, a client only ever supplies the code, never the identity it's proving.
    /// </summary>
    public sealed record CompleteTwoFactorLoginCommand(Guid UserId, string Code) : IRequest<Result<LoginResult>>;
}
