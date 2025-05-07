using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Extensions.Tests.Helpers;

public record TestQuery(int X) : INexRequest<int>;
