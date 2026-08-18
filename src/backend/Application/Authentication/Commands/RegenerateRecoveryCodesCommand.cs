using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record RegenerateRecoveryCodesCommand(Guid UserId, string CurrentPassword) : IRequest<Result<IReadOnlyList<string>>>;
}
