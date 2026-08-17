using Application.Authentication.Commands;
using Application.Authentication.Responses;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Authentication
{
    public static class Register
    {
        public static async Task<Results<Ok<AuthenticationResponse>, ValidationProblem>> Handle(
            RegisterCommand request, IMediator mediator, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(request, cancellationToken);

            if (result.IsFailure)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [result.Error.Code] = [result.Error.Message]
                });
            }

            return TypedResults.Ok(result.Value);
        }
    }
}
