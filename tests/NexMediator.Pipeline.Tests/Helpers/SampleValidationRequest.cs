using NexMediator.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexMediator.Pipeline.Tests.Helpers;

public class SampleValidationRequest : INexRequest<SampleResponse>
{
    public string? PropA { get; set; }
    public string? PropB { get; set; }
}
