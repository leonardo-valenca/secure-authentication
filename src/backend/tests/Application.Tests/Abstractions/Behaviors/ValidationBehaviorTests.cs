using Application.Abstractions.Behaviors;
using Domain.Common;
using FluentValidation;
using Mediator;

namespace Application.Tests.Abstractions.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record TestCommand(string Value) : IRequest<Result<string>>;

    public sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result<string>>([new TestCommandValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(new TestCommand("value"), (_, _) =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Result.Success("ok"));
        }, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ReturnsFailureWithoutCallingNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result<string>>([new TestCommandValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(new TestCommand(""), (_, _) =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Result.Success("ok"));
        }, CancellationToken.None);

        Assert.False(nextCalled);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NoValidatorsRegistered_CallsNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result<string>>([]);

        var result = await behavior.Handle(
            new TestCommand(""),
            (_, _) => ValueTask.FromResult(Result.Success("ok")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
