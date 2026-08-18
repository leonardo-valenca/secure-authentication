using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record EnableTwoFactorCommand(Guid UserId, string Code) : IRequest<Result<IReadOnlyList<string>>>;
}
