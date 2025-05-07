using FluentAssertions;
using NexMediator.Abstractions.Context;

namespace NexMediator.Abstractions.Tests.Context;

/// <summary>
/// Unit tests for the CorrelationContext component,
/// validating correlation ID generation, persistence, override, and isolation across async flows.
/// </summary>
public class CorrelationContextTests
{
    /// <summary>
    /// Validates that a new correlation ID is generated when none has been explicitly set.
    /// </summary>
    [Fact]
    public void CorrelationId_Should_Throw_If_Not_Set()
    {
        var context = new CorrelationContext();

        Action act = () => _ = context.CorrelationId;

        act.Should().Throw<InvalidOperationException>().WithMessage("*not been set*");
    }


    /// <summary>
    /// Validates that the same correlation ID is returned within the same async context.
    /// </summary>
    [Fact]
    public void CorrelationId_Should_Return_Same_Value_After_Set()
    {
        // Arrange
        var context = new CorrelationContext();
        var id = Guid.NewGuid().ToString();
        context.SetCorrelationId(id);

        // Act
        var id1 = context.CorrelationId;
        var id2 = context.CorrelationId;

        // Assert
        id1.Should().Be(id2);
    }


    /// <summary>
    /// Verifies that setting the correlation ID explicitly overrides the current value.
    /// </summary>
    [Fact]
    public void Set_Should_Override_CorrelationId()
    {
        // Arrange
        var expected = Guid.NewGuid().ToString();
        var context = new CorrelationContext();
        context.SetCorrelationId(expected);


        // Act        
        var actual = context.CorrelationId;

        // Assert
        actual.Should().Be(expected, "the manually set correlation ID should override the default");
    }

    /// <summary>
    /// Validates that different async flows maintain separate correlation IDs (AsyncLocal isolation).
    /// </summary>
    [Fact]
    public async Task CorrelationId_Should_Be_Isolated_Per_Async_Context()
    {
        // Arrange
        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();

        string? actual1 = null;
        string? actual2 = null;

        var task1 = Task.Run(() =>
        {
            var context = new CorrelationContext();
            context.SetCorrelationId(id1);
            actual1 = context.CorrelationId;
        });

        var task2 = Task.Run(() =>
        {
            var context = new CorrelationContext();
            context.SetCorrelationId(id2);
            actual2 = new CorrelationContext().CorrelationId;
        });

        await Task.WhenAll(task1, task2);

        // Assert
        actual1.Should().Be(id1, "the correlation ID in task1 should remain isolated");
        actual2.Should().Be(id2, "the correlation ID in task2 should remain isolated");
        actual1.Should().NotBe(actual2, "each async context should retain its own correlation ID");
    }
}
