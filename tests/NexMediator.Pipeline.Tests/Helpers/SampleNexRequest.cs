using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Pipeline.Tests.Helpers;

public class SampleNexRequest : INexRequest<SampleResponse>
{
    public string? Data { get; set; }
}