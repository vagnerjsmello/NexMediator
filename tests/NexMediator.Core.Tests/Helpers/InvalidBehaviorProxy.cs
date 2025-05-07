using System.Reflection;

namespace NexMediator.Core.Tests.Helpers;

public class InvalidBehaviorProxy : DispatchProxy
{
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        return null;
    }
}