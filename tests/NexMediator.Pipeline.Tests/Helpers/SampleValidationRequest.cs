using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Pipeline.Tests.Helpers;

public class SampleValidationRequest : INexRequest<SampleResponse>
{
    public string? PropA { get; set; }
    public string? PropB { get; set; }
}
