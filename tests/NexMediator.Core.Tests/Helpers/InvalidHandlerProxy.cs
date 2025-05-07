using System.Reflection;

namespace NexMediator.Core.Tests.Helpers;

public class InvalidHandlerProxy : DispatchProxy
{
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        return null;
    }
}