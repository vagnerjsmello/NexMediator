using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NexMediator.Abstractions.Context;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Pipeline.Behaviors;
using NexMediator.Pipeline.Tests.Helpers;

namespace NexMediator.Tests.Pipeline;

/// <summary>
/// Unit tests for LoggingBehavior to verify log output during request handling.
/// </summary>
public class LoggingBehaviorTests
{
    private readonly Mock<ILogger<LoggingBehavior<SampleRequest, SampleResponse>>> _mockLogger;
    private readonly Mock<ICorrelationContext> _mockCorrelation;
    private readonly LoggingBehavior<SampleRequest, SampleResponse> _behavior;

    public LoggingBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<LoggingBehavior<SampleRequest, SampleResponse>>>();
        _mockCorrelation = new Mock<ICorrelationContext>();
        _mockCorrelation.Setup(c => c.CorrelationId).Returns("test-correlation-id");

        _behavior = new LoggingBehavior<SampleRequest, SampleResponse>(_mockLogger.Object, _mockCorrelation.Object);
    }

    /// <summary>
    /// Ensures the constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        var correlationMock = new Mock<ICorrelationContext>();

        Action act = () => new LoggingBehavior<SampleRequest, SampleResponse>(null!, correlationMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }


    /// <summary>
    /// Verifies that request and response logging occurs on successful execution.
    /// </summary>
    [Fact]
    public async Task LoggingBehavior_Should_Log_Request_And_Response()
    {
        var request = new SampleRequest();
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(new SampleResponse()));

        var response = await _behavior.Handle(request, next, CancellationToken.None);

        response.Should().NotBeNull();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v != null && v.ToString()!.Contains("Handling SampleRequest")),
                    null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v != null && v.ToString()!.Contains("Handled SampleRequest")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

    }

    /// <summary>
    /// Verifies that errors are logged when an exception occurs during request handling.
    /// </summary>
    [Fact]
    public async Task LoggingBehavior_Should_Log_Error_When_Exception_Thrown()
    {
        var request = new SampleRequest();
        var next = new RequestHandlerDelegate<SampleResponse>(() => throw new InvalidOperationException("Simulated failure"));

        Func<Task> act = () => _behavior.Handle(request, next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error handling")),
                It.Is<InvalidOperationException>(ex => ex.Message.Contains("Simulated failure")),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Ensures that elapsed time is logged as part of request processing.
    /// </summary>
    [Fact]
    public async Task LoggingBehavior_Should_Log_ElapsedTime()
    {
        var request = new SampleRequest();
        var next = new RequestHandlerDelegate<SampleResponse>(async () =>
        {
            await Task.Delay(50);
            return new SampleResponse();
        });

        await _behavior.Handle(request, next, CancellationToken.None);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("completed in")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that the actual response object is included in the log message.
    /// </summary>
    [Fact]
    public async Task LoggingBehavior_Should_Log_Response_Object()
    {
        var request = new SampleRequest();
        var expectedResponse = new SampleResponse();
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(expectedResponse));

        var response = await _behavior.Handle(request, next, CancellationToken.None);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("completed in") &&
                    state.ToString()!.Contains(expectedResponse.ToString()!)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
