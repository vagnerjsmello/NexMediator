using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Extensions;

namespace NexMediator.Abstractions.Tests.Helpers;

public static class NexMediatorBuilder
{
    public static INexMediator BuildEmptyMediator()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddNexMediator(config => { });

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<INexMediator>();
    }

}
