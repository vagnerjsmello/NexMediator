using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Pipeline.Tests.Helpers;

public class SampleRequest : INexCommand<SampleResponse>
{
    public string? Data { get; set; }
}
