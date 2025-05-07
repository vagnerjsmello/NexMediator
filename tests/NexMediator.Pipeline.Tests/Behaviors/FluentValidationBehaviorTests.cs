using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Pipeline.Behaviors;
using NexMediator.Pipeline.Tests.Helpers;

namespace NexMediator.Tests.Pipeline;

/// <summary>
/// Unit tests for FluentValidationBehavior, verifying validation execution, failure handling, and logger usage.
/// </summary>
public class FluentValidationBehaviorTests
{
    private readonly Mock<IValidator<SampleRequest>> _mockValidator;
    private readonly Mock<ILogger<FluentValidationBehavior<SampleRequest, SampleResponse>>> _mockLogger;

    public FluentValidationBehaviorTests()
    {
        _mockValidator = new Mock<IValidator<SampleRequest>>();
        _mockLogger = new Mock<ILogger<FluentValidationBehavior<SampleRequest, SampleResponse>>>();
    }

    /// <summary>
    /// Ensures that the validator is called and request passes through when validation succeeds.
    /// </summary>
    [Fact]
    public async Task FluentValidationBehavior_Should_Validate_Request()
    {
        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new FluentValidationBehavior<SampleRequest, SampleResponse>(
            new[] { _mockValidator.Object }, _mockLogger.Object);

        var request = new SampleRequest();
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(new SampleResponse()));

        var response = await behavior.Handle(request, next, CancellationToken.None);

        response.Should().NotBeNull();
        _mockValidator.Verify(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Ensures a ValidationException is thrown when the request fails validation, and logs the error.
    /// </summary>
    [Fact]
    public async Task FluentValidationBehavior_Should_Throw_When_Validation_Fails()
    {
        var failures = new List<ValidationFailure>
        {
            new("Property1", "Property1 is required"),
            new("Property2", "Property2 must be greater than zero")
        };

        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var behavior = new FluentValidationBehavior<SampleRequest, SampleResponse>(
            new[] { _mockValidator.Object }, _mockLogger.Object);

        var request = new SampleRequest();
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(new SampleResponse()));

        Func<Task> act = () => behavior.Handle(request, next, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ValidationException>(act);
        exception.Errors.Should().BeEquivalentTo(failures);

        _mockLogger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) =>
                v.ToString()!.Contains("Validation failed") &&
                v.ToString()!.Contains("2 errors")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Skips validation entirely if no validators are registered for the request.
    /// </summary>
    [Fact]
    public async Task FluentValidationBehavior_Should_Skip_When_No_Validators()
    {
        var behavior = new FluentValidationBehavior<SampleRequest, SampleResponse>(
            Enumerable.Empty<IValidator<SampleRequest>>(), _mockLogger.Object);

        var request = new SampleRequest();
        var expected = new SampleResponse();
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(expected));

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be(expected);
    }

    /// <summary>
    /// Aggregates and throws all errors from multiple validators registered for a request.
    /// </summary>
    [Fact]
    public async Task FluentValidationBehavior_Should_Aggregate_Errors_From_Multiple_Validators()
    {
        // Arrange
        var validatorA = new ValidatorA();
        var validatorB = new ValidatorB();

        var validators = new IValidator<SampleValidationRequest>[] { validatorA, validatorB };

        var mockLogger = new Mock<ILogger<FluentValidationBehavior<SampleValidationRequest, SampleResponse>>>();

        var behavior = new FluentValidationBehavior<SampleValidationRequest, SampleResponse>(
            validators, mockLogger.Object);

        var request = new SampleValidationRequest
        {
            PropA = "X", // invalid
            PropB = "Y"  // invalid
        };

        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(new SampleResponse()));

        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(request, next, CancellationToken.None));

        // Assert
        ex.Errors.Should().Contain(e => e.ErrorMessage == "Invalid A" && e.PropertyName == "PropA");
        ex.Errors.Should().Contain(e => e.ErrorMessage == "Invalid B" && e.PropertyName == "PropB");
    }

    /// <summary>
    /// Ensures constructor throws ArgumentNullException when dependencies are null.
    /// </summary>
    [Theory]
    [InlineData(null, true, "validators")]
    [InlineData(true, null, "logger")]
    public void Constructor_Should_Throw_When_Dependency_Is_Null(object? provideValidators, object? provideLogger, string expectedParam)
    {
        // Arrange
        var validators = provideValidators is not null
            ? Enumerable.Empty<IValidator<SampleRequest>>()
            : null!;

        var logger = provideLogger is not null
            ? new Mock<ILogger<FluentValidationBehavior<SampleRequest, SampleResponse>>>().Object
            : null!;

        // Act
        Action act = () => new FluentValidationBehavior<SampleRequest, SampleResponse>(validators, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(expectedParam);
    }

}
