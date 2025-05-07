using FluentAssertions;
using NexMediator.Abstractions.Exceptions;
using NexMediator.Abstractions.Tests.Helpers;

namespace NexMediator.Abstractions.Tests.Exceptions;

/// <summary>
/// Unit tests for the NexMediatorException class, validating its constructors and exception behavior.
/// </summary>
public class NexMediatorExceptionTests
{
    private readonly string _defaultMessage;
    private readonly Exception _innerException;

    public NexMediatorExceptionTests()
    {
        _defaultMessage = "Pipeline configuration error";
        _innerException = new InvalidOperationException("Invalid order");
    }

    /// <summary>
    /// Ensures that the exception sets the message correctly when only a message is provided.
    /// </summary>
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Act
        var exception = new NexMediatorException("Handler not found");

        // Assert
        exception.Message.Should().Be("Handler not found");
        exception.InnerException.Should().BeNull();
    }

    /// <summary>
    /// Ensures that the exception sets both the message and inner exception when provided.
    /// </summary>
    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetProperties()
    {
        // Act
        var exception = new NexMediatorException(_defaultMessage, _innerException);

        // Assert
        exception.Message.Should().Be(_defaultMessage);
        exception.InnerException.Should().Be(_innerException);
    }

    /// <summary>
    /// Validates that an exception is thrown when no handler is registered for a request.
    /// </summary>
    [Fact]
    public async Task NexMediator_Should_Throw_When_Handler_Is_Missing()
    {
        // Arrange: build a mediator with no handlers registered
        var mediator = NexMediatorBuilder.BuildEmptyMediator();
        var request = new UnmappedRequest();

        // Act: attempt to send the request
        Func<Task> act = () => mediator.Send(request);

        // Assert: an InvalidOperationException is thrown and its message
        // contains the correct interface path for INexRequestHandler
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(act);
        ex.Message.Should().Contain(
            "No service for type 'NexMediator.Abstractions.Interfaces.INexRequestHandler",
            because: "the DI container should report the missing handler interface from the Abstractions namespace");
    }

}
