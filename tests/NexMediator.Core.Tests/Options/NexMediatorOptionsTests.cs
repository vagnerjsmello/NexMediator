using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Core.Tests.Helpers;
using NexMediator.Pipeline.Behaviors;

namespace NexMediator.Core.Tests.Options;

/// <summary>
/// Unit tests for the NexMediatorOptions class, validating behavior registration, ordering, and configuration.
/// </summary>
public class NexMediatorOptionsTests
{
    private readonly ServiceCollection _services;
    private readonly NexMediatorOptions _options;

    public NexMediatorOptionsTests()
    {
        _services = new ServiceCollection();
        _options = new NexMediatorOptions(_services);
    }

    /// <summary>
    /// Ensures that a previously registered behavior is removed correctly from the service collection.
    /// </summary>
    [Fact]
    public void RemoveBehavior_Should_Remove_Registered_Behavior()
    {
        var behaviorType = typeof(FakeBehavior<,>);
        _options.AddBehavior(behaviorType, 1);

        Assert.Contains(_services, d => d.ImplementationType == behaviorType);

        _options.RemoveBehavior(behaviorType);

        Assert.DoesNotContain(_services, d => d.ImplementationType == behaviorType);
    }

    /// <summary>
    /// Ensures that removing an unregistered behavior does not throw or affect the service collection.
    /// </summary>
    [Fact]
    public void RemoveBehavior_Should_Not_Fail_When_Not_Registered()
    {
        _options.RemoveBehavior(typeof(SampleBehavior));

        Assert.Empty(_services);
    }

    /// <summary>
    /// Ensures AddBehavior throws if the provided type is not an open generic definition.
    /// </summary>
    [Fact]
    public void AddBehavior_Should_Throw_If_Type_Is_Not_Generic_Definition()
    {
        // Arrange
        var nonGenericType = typeof(object);

        // Act
        var ex = Assert.Throws<ArgumentException>(() => _options.AddBehavior(nonGenericType, 1));

        // Assert
        // O código agora lança "Type must be open generic."
        Assert.Contains("Type must be open generic", ex.Message);
        Assert.Equal("behaviorType", ex.ParamName);
    }


    /// <summary>
    /// Ensures AddBehavior throws if the provided type does not implement INexPipelineBehavior.
    /// </summary>
    [Fact]
    public void AddBehavior_Should_Throw_If_Type_Does_Not_Implement_INexPipelineBehavior()
    {
        var type = typeof(InvalidBehavior<,>);

        var ex = Assert.Throws<ArgumentException>(() => _options.AddBehavior(type, 1));

        Assert.Contains("must implement INexPipelineBehavior", ex.Message);
    }

    /// <summary>
    /// Ensures Validate reports behaviors registered with duplicate order values.
    /// </summary>
    [Fact]
    public void Validate_Should_Report_Duplicate_Order_Values()
    {
        _options.AddBehavior(typeof(FakeBehavior<,>), 1);
        _options.AddBehavior(typeof(FakeBehaviorDuplicate<,>), 1);

        var messages = _options.Validate();

        Assert.Contains(messages, m => m.Contains("Multiple behaviors with order 1"));
    }

    /// <summary>
    /// Ensures Validate reports when built-in behaviors are not configured in the recommended order.
    /// </summary>
    [Fact]
    public void Validate_Should_Report_BuiltIn_Behaviors_If_Out_Of_Order()
    {
        _options.AddBehavior(typeof(LoggingBehavior<,>), 1);
        _options.AddBehavior(typeof(TransactionBehavior<,>), 2);
        _options.AddBehavior(typeof(CachingBehavior<,>), 3);
        _options.AddBehavior(typeof(FluentValidationBehavior<,>), 4);

        var messages = _options.Validate();

        Assert.Contains(messages, m => m.Contains("Built-in behaviors are not in recommended order"));
    }

    /// <summary>
    /// Ensures pipeline behaviors are executed in the order configured via AddBehavior.
    /// </summary>
    [Fact]
    public async Task OrderBehaviors_Should_Execute_Behaviors_In_Configured_Order()
    {
        var callSequence = new List<string>();

        _options.AddBehavior(typeof(FakeBehaviorB<,>), 2);
        _options.AddBehavior(typeof(FakeBehaviorA<,>), 1);

        var behaviors = new List<INexPipelineBehavior<SampleRequest, SampleResponse>>
        {
            BehaviorFactory.MakeBehavior("A", callSequence),
            BehaviorFactory.MakeBehavior("B", callSequence)
        };

        var ordered = _options
            .OrderBehaviors(behaviors.Cast<object>())
            .Cast<INexPipelineBehavior<SampleRequest, SampleResponse>>()
            .ToList();

        foreach (var behavior in ordered)
        {
            RequestHandlerDelegate<SampleResponse> next = () => Task.FromResult(new SampleResponse());
            await behavior.Handle(new SampleRequest(), next, CancellationToken.None);
        }

        Assert.Equal(new[] { "A", "B" }, callSequence);
    }

    /// <summary>
    /// Ensures AddBehavior<T> registers the behavior correctly and returns the same instance for chaining.
    /// </summary>
    [Fact]
    public void AddBehavior_Generic_Should_Register_And_Return_Same_Instance()
    {
        var result = _options.AddBehavior(typeof(FakeBehavior<,>), 10);

        result.Should().BeSameAs(_options);
        _services.Should().Contain(s => s.ImplementationType == typeof(FakeBehavior<,>));
    }
}
