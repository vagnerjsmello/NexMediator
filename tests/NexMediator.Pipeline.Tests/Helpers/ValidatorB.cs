using FluentValidation;

namespace NexMediator.Pipeline.Tests.Helpers;

public class ValidatorB : AbstractValidator<SampleValidationRequest>
{
    public ValidatorB() => RuleFor(x => x.PropB).Equal("B").WithMessage("Invalid B");
}
