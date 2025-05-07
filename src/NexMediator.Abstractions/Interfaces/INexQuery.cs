namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Represents a read-only query request in the NexMediator pipeline.
/// Queries should not modify application state.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the query.</typeparam>
public interface INexQuery<TResponse> : INexRequest<TResponse>
{
}
