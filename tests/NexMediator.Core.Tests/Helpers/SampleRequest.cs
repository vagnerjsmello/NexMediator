using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Core.Tests.Helpers;

public class SampleRequest : INexCommand<SampleResponse>
{
    public string? Data { get; set; }
}
