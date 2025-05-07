namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Represents a write operation (Command) in the NexMediator pipeline.
/// </summary>
/// <typeparam name="TResponse">The expected response type.</typeparam>
public interface INexCommand<TResponse> : INexRequest<TResponse>
{
}
