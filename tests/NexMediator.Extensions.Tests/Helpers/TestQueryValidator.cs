using FluentValidation;

namespace NexMediator.Extensions.Tests.Helpers;

public class TestQueryValidator : AbstractValidator<TestQuery>
{
    public TestQueryValidator() => RuleFor(x => x.X).GreaterThan(0);
}