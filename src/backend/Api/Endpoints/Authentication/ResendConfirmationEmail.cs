using Application.Authentication.Commands;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Authentication
{
    public static class ResendConfirmationEmail
    {
        public static async Task<Results<NoContent, ValidationProblem>> Handle(
            ResendConfirmationEmailCommand request, IMediator mediator, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(request, cancellationToken);

            if (result.IsFailure)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [result.Error.Code] = [result.Error.Message]
                });
            }

            return TypedResults.NoContent();
        }
    }
}
