using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers;

public class SampleRequestHandler : INexRequestHandler<SampleRequest, SampleResponse>, IDisposable
{
    private bool _isDisposed;

    public async Task<SampleResponse> Handle(SampleRequest request, CancellationToken cancellationToken)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SampleRequestHandler));

        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Data))
            throw new ArgumentException("Data cannot be null or empty", nameof(request));

        await Task.Delay(100, cancellationToken);
        return new SampleResponse { Result = $"{request.Data} processed" };
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
