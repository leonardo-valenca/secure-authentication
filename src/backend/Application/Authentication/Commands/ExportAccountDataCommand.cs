using Application.Authentication.Responses;
using Domain.Common;
using Mediator;

namespace Application.Authentication.Commands
{
    public sealed record ExportAccountDataCommand(Guid UserId) : IRequest<Result<AccountDataExport>>;
}
