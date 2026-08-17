using Domain.Common;
using FluentValidation;
using Mediator;

namespace Application.Abstractions.Behaviors
{
    public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : Result
    {
        public async ValueTask<TResponse> Handle(
            TRequest message,
            MessageHandlerDelegate<TRequest, TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!validators.Any())
                return await next(message, cancellationToken);

            var failures = validators
                .Select(validator => validator.Validate(message))
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToArray();

            if (failures.Length == 0)
                return await next(message, cancellationToken);

            var error = new Error(failures[0].PropertyName, failures[0].ErrorMessage);

            return CreateFailure(error);
        }

        private static TResponse CreateFailure(Error error)
        {
            if (typeof(TResponse) == typeof(Result))
                return (TResponse)(object)Result.Failure(error);

            var valueType = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
                .MakeGenericMethod(valueType);

            return (TResponse)failureMethod.Invoke(null, [error])!;
        }
    }
}