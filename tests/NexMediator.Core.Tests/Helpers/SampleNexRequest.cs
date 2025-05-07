using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers;

public class SampleNexRequest : INexRequest<SampleResponse>
{
    public string? Data { get; set; }
}
