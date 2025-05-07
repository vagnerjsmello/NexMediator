using FluentValidation;

namespace NexMediator.Pipeline.Tests.Helpers;

public class ValidatorA : AbstractValidator<SampleValidationRequest>
{
    public ValidatorA() => RuleFor(x => x.PropA).Equal("A").WithMessage("Invalid A");
}
