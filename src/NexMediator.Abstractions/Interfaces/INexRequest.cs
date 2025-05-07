namespace NexMediator.Abstractions.Interfaces;

/// <summary>
/// Base interface for requests in the mediator pipeline
/// </summary>
/// <typeparam name="TResponse">Expected response type</typeparam>
/// <remarks>
/// Types:
/// - Commands: Modify state
/// - Queries: Read data
/// 
/// Guidelines:
/// - Keep immutable
/// - Include required data
/// - Use clear naming
/// - Follow CQRS principles
/// </remarks>
public interface INexRequest<out TResponse> { }
