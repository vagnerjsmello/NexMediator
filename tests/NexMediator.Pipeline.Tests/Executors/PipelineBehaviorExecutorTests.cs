using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Core.Pipeline;
using NexMediator.Pipeline.Tests.Helpers;

namespace NexMediator.Tests.Pipeline;

/// <summary>
/// Tests for PipelineBehaviorExecutor covering execution flow, exception handling, and processor invocation.
/// </summary>
public class PipelineBehaviorExecutorTests
{
    /// <summary>
    /// Ensures that pre-processors, behaviors, handler, and post-processors execute in the correct order.
    /// </summary>
    [Fact]
    public async Task Execute_ShouldInvoke_AllPipelineStepsInOrder()
    {
        var request = new SampleRequest();
        var expectedResponse = new SampleResponse { Result = "Handled" };
        var callSequence = new List<string>();

        var preProcessor = new Mock<INexRequestPreProcessor<SampleRequest>>();
        preProcessor.Setup(p => p.Process(It.IsAny<SampleRequest>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask)
                    .Callback(() => callSequence.Add("pre"));

        var postProcessor = new Mock<INexRequestPostProcessor<SampleRequest, SampleResponse>>();
        postProcessor.Setup(p => p.Process(It.IsAny<SampleRequest>(), It.IsAny<SampleResponse>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask)
                     .Callback(() => callSequence.Add("post"));

        var behavior = new Mock<INexPipelineBehavior<SampleRequest, SampleResponse>>();
        behavior.Setup(b => b.Handle(It.IsAny<SampleRequest>(), It.IsAny<RequestHandlerDelegate<SampleResponse>>(), It.IsAny<CancellationToken>()))
                .Returns<SampleRequest, RequestHandlerDelegate<SampleResponse>, CancellationToken>((_, next, _) =>
                {
                    callSequence.Add("behavior");
                    return next();
                });

        var handler = new Mock<INexRequestHandler<SampleRequest, SampleResponse>>();
        handler.Setup(h => h.Handle(It.IsAny<SampleRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(expectedResponse)
               .Callback(() => callSequence.Add("handler"));

        var serviceProvider = new ServiceCollection()
            .AddSingleton(preProcessor.Object)
            .AddSingleton(postProcessor.Object)
            .BuildServiceProvider();

        var executor = new PipelineBehaviorExecutor<SampleRequest, SampleResponse>(
            serviceProvider, handler.Object, new[] { behavior.Object });

        var result = await executor.Execute(request, CancellationToken.None);

        result.Should().Be(expectedResponse);
        callSequence.Should().ContainInOrder("pre", "behavior", "handler", "post");
    }

    /// <summary>
    /// Ensures the pipeline throws if the cancellation token is already cancelled.
    /// </summary>
    [Fact]
    public async Task Execute_ShouldThrow_WhenCancelledBeforeExecution()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new Mock<INexRequestHandler<SampleRequest, SampleResponse>>();
        var executor = new PipelineBehaviorExecutor<SampleRequest, SampleResponse>(
            new ServiceCollection().BuildServiceProvider(),
            handler.Object,
            Enumerable.Empty<INexPipelineBehavior<SampleRequest, SampleResponse>>());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            executor.Execute(new SampleRequest(), cts.Token));
    }

    /// <summary>
    /// Ensures exceptions thrown by a pipeline behavior are propagated.
    /// </summary>
    [Fact]
    public async Task Execute_ShouldThrow_WhenBehaviorThrows()
    {
        var behavior = new Mock<INexPipelineBehavior<SampleRequest, SampleResponse>>();
        behavior.Setup(b => b.Handle(It.IsAny<SampleRequest>(), It.IsAny<RequestHandlerDelegate<SampleResponse>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

        var handler = new Mock<INexRequestHandler<SampleRequest, SampleResponse>>();
        var executor = new PipelineBehaviorExecutor<SampleRequest, SampleResponse>(
            new ServiceCollection().BuildServiceProvider(),
            handler.Object,
            new[] { behavior.Object });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.Execute(new SampleRequest(), CancellationToken.None));
    }

    /// <summary>
    /// Ensures the handler is executed directly when no behaviors are registered.
    /// </summary>
    [Fact]
    public async Task Execute_ShouldCallHandlerDirectly_WhenNoBehaviors()
    {
        var handlerCalled = false;

        var handler = new Mock<INexRequestHandler<SampleRequest, SampleResponse>>();
        handler.Setup(h => h.Handle(It.IsAny<SampleRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new SampleResponse { Result = "ok" })
               .Callback(() => handlerCalled = true);

        var executor = new PipelineBehaviorExecutor<SampleRequest, SampleResponse>(
            new ServiceCollection().BuildServiceProvider(),
            handler.Object,
            Enumerable.Empty<INexPipelineBehavior<SampleRequest, SampleResponse>>());

        var result = await executor.Execute(new SampleRequest(), CancellationToken.None);

        result.Result.Should().Be("ok");
        handlerCalled.Should().BeTrue();
    }

    /// <summary>
    /// Ensures multiple behaviors are executed in the order they were registered.
    /// </summary>
    [Fact]
    public async Task Execute_ShouldCallMultipleBehaviorsInCorrectOrder()
    {
        var callSequence = new List<string>();

        var behaviors = new[]
        {
            BehaviorFactory.MakeBehavior("first", callSequence),
            BehaviorFactory.MakeBehavior("second", callSequence),
            BehaviorFactory.MakeBehavior("third", callSequence),
        };

        var handler = new Mock<INexRequestHandler<SampleRequest, SampleResponse>>();
        handler.Setup(h => h.Handle(It.IsAny<SampleRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new SampleResponse { Result = "ok" })
               .Callback(() => callSequence.Add("handler"));

        var executor = new PipelineBehaviorExecutor<SampleRequest, SampleResponse>(
            new ServiceCollection().BuildServiceProvider(),
            handler.Object,
            behaviors);

        await executor.Execute(new SampleRequest(), CancellationToken.None);

        callSequence.Should().ContainInOrder("first", "second", "third", "handler");
    }

    /// <summary>
    /// Ensures an exception is thrown when a pre-processor fails.
    /// </summary>
    [Fact]
    public async Task Execute_ShouldThrow_WhenPreProcessorThrows()
    {
        var preProcessor = new Mock<INexRequestPreProcessor<SampleRequest>>();
        preProcessor.Setup(p => p.Process(It.IsAny<SampleRequest>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("PreProcessor failed"));

        var serviceProvider = new ServiceCollection()
            .AddSingleton(preProcessor.Object)
            .BuildServiceProvider();

        var handler = new Mock<INexRequestHandler<SampleRequest, SampleResponse>>();
        var executor = new PipelineBehaviorExecutor<SampleRequest, SampleResponse>(
            serviceProvider, handler.Object, Enumerable.Empty<INexPipelineBehavior<SampleRequest, SampleResponse>>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.Execute(new SampleRequest(), CancellationToken.None));
    }

    /// <summary>
    /// Ensures post-processors are executed after the handler completes.
    /// </summary>
    [Fact]
    public async Task Execute_ShouldInvoke_PostProcessors_AfterHandler()
    {
        var postCalled = false;

        var postProcessor = new Mock<INexRequestPostProcessor<SampleRequest, SampleResponse>>();
        postProcessor.Setup(p => p.Process(It.IsAny<SampleRequest>(), It.IsAny<SampleResponse>(), It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask)
                     .Callback(() => postCalled = true);

        var handler = new Mock<INexRequestHandler<SampleRequest, SampleResponse>>();
        handler.Setup(h => h.Handle(It.IsAny<SampleRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new SampleResponse { Result = "done" });

        var serviceProvider = new ServiceCollection()
            .AddSingleton(postProcessor.Object)
            .BuildServiceProvider();

        var executor = new PipelineBehaviorExecutor<SampleRequest, SampleResponse>(
            serviceProvider, handler.Object, Enumerable.Empty<INexPipelineBehavior<SampleRequest, SampleResponse>>());

        var result = await executor.Execute(new SampleRequest(), CancellationToken.None);

        result.Result.Should().Be("done");
        postCalled.Should().BeTrue();
    }

    /// <summary>
    /// Ensures an exception is thrown if a post-processor fails.
    /// </summary>
    [Fact]
    public async Task Execute_ShouldThrow_WhenPostProcessorThrows()
    {
        var postProcessor = new Mock<INexRequestPostProcessor<SampleRequest, SampleResponse>>();
        postProcessor.Setup(p => p.Process(It.IsAny<SampleRequest>(), It.IsAny<SampleResponse>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new InvalidOperationException("PostProcessor failed"));

        var handler = new Mock<INexRequestHandler<SampleRequest, SampleResponse>>();
        handler.Setup(h => h.Handle(It.IsAny<SampleRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new SampleResponse { Result = "ok" });

        var serviceProvider = new ServiceCollection()
            .AddSingleton(postProcessor.Object)
            .BuildServiceProvider();

        var executor = new PipelineBehaviorExecutor<SampleRequest, SampleResponse>(
            serviceProvider, handler.Object, Enumerable.Empty<INexPipelineBehavior<SampleRequest, SampleResponse>>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.Execute(new SampleRequest(), CancellationToken.None));

        ex.Message.Should().Be("PostProcessor failed");
    }

    /// <summary>
    /// Ensures that if a behavior throws after invoking the next delegate, the exception is still propagated.
    /// </summary>
    [Fact]
    public async Task Execute_ShouldThrow_WhenBehaviorThrowsAfterNext()
    {
        var behavior = new Mock<INexPipelineBehavior<SampleRequest, SampleResponse>>();
        behavior.Setup(b => b.Handle(It.IsAny<SampleRequest>(), It.IsAny<RequestHandlerDelegate<SampleResponse>>(), It.IsAny<CancellationToken>()))
                .Returns<SampleRequest, RequestHandlerDelegate<SampleResponse>, CancellationToken>(async (_, next, _) =>
                {
                    await next(); // Call handler
                    throw new InvalidOperationException("Behavior after next failed");
                });

        var handler = new Mock<INexRequestHandler<SampleRequest, SampleResponse>>();
        handler.Setup(h => h.Handle(It.IsAny<SampleRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new SampleResponse { Result = "ok" });

        var executor = new PipelineBehaviorExecutor<SampleRequest, SampleResponse>(
            new ServiceCollection().BuildServiceProvider(),
            handler.Object,
            new[] { behavior.Object });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.Execute(new SampleRequest(), CancellationToken.None));

        ex.Message.Should().Be("Behavior after next failed");
    }

    /// <summary>
    /// Ensures the constructor throws when required dependencies are null.
    /// </summary>
    [Theory]
    [InlineData(null, true, true, "serviceProvider")]
    [InlineData(true, null, true, "handler")]
    public void Constructor_Should_Throw_When_Dependencies_Null(object? providerFlag, object? handlerFlag, object? behaviorFlag, string expectedParam)
    {
        // Arrange
        var provider = providerFlag is not null ? new ServiceCollection().BuildServiceProvider() : null!;
        var handler = handlerFlag is not null ? new Mock<INexRequestHandler<SampleRequest, SampleResponse>>().Object : null!;
        var behaviors = behaviorFlag is not null
            ? Enumerable.Empty<INexPipelineBehavior<SampleRequest, SampleResponse>>()
            : null;

        // Act
        Action act = () => new PipelineBehaviorExecutor<SampleRequest, SampleResponse>(provider, handler, behaviors!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName(expectedParam);
    }

    /// <summary>
    /// Validates constructor null checks for PipelineBehaviorExecutor.
    /// </summary>
    [Theory]
    [InlineData(false, true, true, true, "serviceProvider")]
    [InlineData(true, false, true, true, "handler")]
    [InlineData(true, true, false, false, null)]
    public void Constructor_Should_Validate_Null_Arguments(
        bool hasProvider,
        bool hasHandler,
        bool hasBehaviors,
        bool shouldThrow,
        string? expectedParamName)
    {
        // Arrange
        var provider = hasProvider ? new ServiceCollection().BuildServiceProvider() : null!;
        var handler = hasHandler ? new Mock<INexRequestHandler<SampleRequest, SampleResponse>>().Object : null!;
        var behaviors = hasBehaviors ? Enumerable.Empty<INexPipelineBehavior<SampleRequest, SampleResponse>>() : null;

        // Act
        Action act = () => new PipelineBehaviorExecutor<SampleRequest, SampleResponse>(provider, handler, behaviors!);

        // Assert
        if (shouldThrow)
        {
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName(expectedParamName);
        }
        else
        {
            act.Should().NotBeNull(); // no exception expected
        }
    }

    /// <summary>
    /// Ensures that behaviors is safely replaced with an empty list when null is passed to the constructor.
    /// </summary>
    [Fact]
    public async Task Constructor_Should_Use_Empty_Behavior_List_When_Null_Is_Passed()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var handlerMock = new Mock<INexRequestHandler<SampleRequest, SampleResponse>>();
        handlerMock.Setup(h => h.Handle(It.IsAny<SampleRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new SampleResponse { Result = "no-behavior" });

        var executor = new PipelineBehaviorExecutor<SampleRequest, SampleResponse>(
            serviceProvider,
            handlerMock.Object,
            null!); // explicitly pass null

        // Act
        var response = await executor.Execute(new SampleRequest(), CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Result.Should().Be("no-behavior");
        handlerMock.Verify(h => h.Handle(It.IsAny<SampleRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }


}
