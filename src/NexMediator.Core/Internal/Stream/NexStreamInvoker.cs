using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Interfaces;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NexMediator.Core.Internal.Stream;

/// <summary>
/// Strongly-typed invoker for stream requests that calls the appropriate handler.
/// </summary>
/// <typeparam name="TRequest">Type of the stream request.</typeparam>
/// <typeparam name="TResponse">Type of the response item.</typeparam>
internal sealed class NexStreamInvoker<TRequest, TResponse> : INexStreamInvoker
    where TRequest : INexStreamRequest<TResponse>
{
    /// <summary>
    /// Invokes the stream request handler and returns results as object stream.
    /// </summary>
    /// <param name="request">The stream request.</param>
    /// <param name="provider">The service provider to resolve dependencies.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An async stream of responses boxed as objects.</returns>
    public async IAsyncEnumerable<object> Invoke(object request, IServiceProvider provider, [EnumeratorCancellation] CancellationToken ct)
    {
        var handler = provider.GetRequiredService<INexStreamRequestHandler<TRequest, TResponse>>();
        var stream = handler.Handle((TRequest)request, ct);

        await foreach (var item in stream.WithCancellation(ct).ConfigureAwait(false))
        {
            yield return item!;
        }
    }
}
