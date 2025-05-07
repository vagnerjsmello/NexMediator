using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Pipeline.Behaviors;
using NexMediator.Pipeline.Tests.Helpers;

namespace NexMediator.Tests.Pipeline;

/// <summary>
/// Unit tests for TransactionBehavior validating commit, rollback, and logging during request handling.
/// </summary>
public class TransactionBehaviorTests
{
    private readonly Mock<ITransactionManager> _mockTransactionManager;
    private readonly Mock<ILogger<TransactionBehavior<SampleRequest, SampleResponse>>> _mockLogger;
    private readonly TransactionBehavior<SampleRequest, SampleResponse> _behavior;

    public TransactionBehaviorTests()
    {
        _mockTransactionManager = new Mock<ITransactionManager>();
        _mockLogger = new Mock<ILogger<TransactionBehavior<SampleRequest, SampleResponse>>>();
        _behavior = new TransactionBehavior<SampleRequest, SampleResponse>(
            _mockTransactionManager.Object,
            _mockLogger.Object
        );
    }

    /// <summary>
    /// Verifies that a transaction is committed successfully when no exception is thrown.
    /// </summary>
    [Fact]
    public async Task TransactionBehavior_Should_Commit_When_No_Exception()
    {
        var request = new SampleRequest();
        var expectedResponse = new SampleResponse();
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(expectedResponse));

        var response = await _behavior.Handle(request, next, CancellationToken.None);

        response.Should().Be(expectedResponse);
        _mockTransactionManager.Verify(tm => tm.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransactionManager.Verify(tm => tm.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransactionManager.Verify(tm => tm.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that the transaction is rolled back when an exception is thrown by the handler.
    /// </summary>
    [Fact]
    public async Task TransactionBehavior_Should_Rollback_When_Exception_Thrown()
    {
        var request = new SampleRequest();
        var next = new RequestHandlerDelegate<SampleResponse>(() => throw new InvalidOperationException("Handler error"));

        Func<Task> act = () => _behavior.Handle(request, next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _mockTransactionManager.Verify(tm => tm.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockTransactionManager.Verify(tm => tm.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockTransactionManager.Verify(tm => tm.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that the transaction lifecycle is logged when processing completes successfully.
    /// </summary>
    [Fact]
    public async Task TransactionBehavior_Should_Log_TransactionLifecycle_OnSuccess()
    {
        var request = new SampleRequest();
        var response = new SampleResponse();
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(response));

        await _behavior.Handle(request, next, CancellationToken.None);

        _mockLogger.VerifyLog(LogLevel.Debug, Times.Exactly(2),
            msg => msg.Contains("Started transaction") || msg.Contains("Committed transaction"));
    }

    /// <summary>
    /// Verifies that rollback and exception information is logged when an exception is thrown.
    /// </summary>
    [Fact]
    public async Task TransactionBehavior_Should_Log_Rollback_OnException()
    {
        var capturedMessages = new List<string>();
        var capturedExceptionMessages = new List<string>();

        _mockLogger.Setup(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
        .Callback<LogLevel, EventId, object, Exception, Delegate>((_, _, state, ex, _) =>
        {
            capturedMessages.Add(state?.ToString() ?? string.Empty);
            if (ex != null)
                capturedExceptionMessages.Add(ex.Message);
        });

        var request = new SampleRequest();
        var next = new RequestHandlerDelegate<SampleResponse>(() => throw new InvalidOperationException("fail"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _behavior.Handle(request, next, CancellationToken.None));

        capturedMessages.Should().ContainSingle(m =>
            m.Contains("Rolled back transaction") &&
            m.Contains(nameof(SampleRequest)));

        capturedExceptionMessages.Should().ContainSingle(m => m.Contains("fail"));
    }

    /// <summary>
    /// Verifies that the TransactionBehavior skips transaction handling
    /// when the request does not implement <see cref="INexCommand{TResponse}"/>.
    /// </summary>
    /// <remarks>
    /// Ensures that Begin, Commit, and Rollback are never called
    /// for requests that are not considered commands.
    /// </remarks>
    [Fact]
    public async Task TransactionBehavior_Should_Skip_For_NonCommand()
    {
        var request = new SampleNexRequest();
        var next = new RequestHandlerDelegate<SampleResponse>(() => Task.FromResult(new SampleResponse()));

        var mockLogger = new Mock<ILogger<TransactionBehavior<SampleNexRequest, SampleResponse>>>();

        var behavior = new TransactionBehavior<SampleNexRequest, SampleResponse>(
            _mockTransactionManager.Object,
            mockLogger.Object
        );

        var result = await behavior.Handle(request, next, CancellationToken.None);

        _mockTransactionManager.Verify(tm => tm.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockTransactionManager.Verify(tm => tm.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockTransactionManager.Verify(tm => tm.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }




    /// <summary>
    /// Ensures constructor throws ArgumentNullException when required dependencies are null.
    /// </summary>
    [Theory]
    [InlineData(true, false, "logger")]
    [InlineData(false, true, "transactionManager")]
    public void Constructor_Should_Throw_When_Dependency_Is_Null(bool provideTransactionManager, bool provideLogger, string expectedParam)
    {
        // Arrange
        var transactionManager = provideTransactionManager ? new Mock<ITransactionManager>().Object : null!;
        var logger = provideLogger ? new Mock<ILogger<TransactionBehavior<SampleRequest, SampleResponse>>>().Object : null!;

        // Act
        Action act = () => new TransactionBehavior<SampleRequest, SampleResponse>(transactionManager, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(expectedParam);
    }

}
