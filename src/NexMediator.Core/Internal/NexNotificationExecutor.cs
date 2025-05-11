using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Interfaces;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace NexMediator.Core.Internal;

/// <summary>
/// Builds and caches a dispatcher for each notification type.
/// The dispatcher:
/// 1. Gets all notification handlers.
/// 2. Calls each handler.Handle in parallel.
/// 3. Uses Task.WhenAll to wait for all handlers to finish.
/// </summary>
internal static class NexNotificationExecutor
{
    private static readonly ConcurrentDictionary<Type, Func<object, IServiceProvider, CancellationToken, Task>> _cache = new();

    /// <summary>
    /// Publishes a notification by invoking all handlers in parallel.
    /// </summary>
    /// <typeparam name="TNotification">Notification type.</typeparam>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="provider">Service provider to resolve handlers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A Task that completes when all handlers have run.</returns>
    /// <exception cref="ArgumentNullException">Thrown if notification is null.</exception>
    public static Task Publish<TNotification>(TNotification notification, IServiceProvider provider, CancellationToken ct)
        where TNotification : INexNotification
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        var type = typeof(TNotification);
        var dispatcher = _cache.GetOrAdd(type, CreateDispatcher);
        return dispatcher(notification!, provider, ct);
    }

    /// <summary>
    /// Creates a compiled dispatcher delegate for a notification type.
    /// </summary>
    private static Func<object, IServiceProvider, CancellationToken, Task> CreateDispatcher(Type type)
    {
        var handlerInterface = typeof(INexNotificationHandler<>).MakeGenericType(type);
        var method = handlerInterface.GetMethod(nameof(INexNotificationHandler<INexNotification>.Handle))!;

        var notificationParam = Expression.Parameter(typeof(object), "notification");
        var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var castNotification = Expression.Convert(notificationParam, type);
        var getServices = typeof(ServiceProviderServiceExtensions)
            .GetMethods()
            .First(m => m.Name == nameof(ServiceProviderServiceExtensions.GetServices)
                        && m.IsGenericMethodDefinition)
            .MakeGenericMethod(handlerInterface);
        var handlersCall = Expression.Call(getServices, providerParam);

        var handlersVar = Expression.Variable(typeof(IEnumerable<>).MakeGenericType(handlerInterface), "handlers");
        var tasksVar = Expression.Variable(typeof(List<Task>), "tasks");
        var handlerVar = Expression.Variable(handlerInterface, "handler");

        var newList = Expression.New(typeof(List<Task>));
        var assignList = Expression.Assign(tasksVar, newList);
        var assignHandlers = Expression.Assign(handlersVar, handlersCall);

        var callHandle = Expression.Call(handlerVar, method, castNotification, ctParam);
        var addCall = Expression.Call(tasksVar, nameof(List<Task>.Add), null, callHandle);
        var loop = ForEach(handlerInterface, handlersVar, handlerVar, addCall);
        var whenAll = Expression.Call(typeof(Task).GetMethod(nameof(Task.WhenAll), new[] { typeof(IEnumerable<Task>) })!, tasksVar);

        var body = Expression.Block(
            new[] { handlersVar, tasksVar, handlerVar },
            assignHandlers,
            assignList,
            loop,
            whenAll);

        return Expression.Lambda<Func<object, IServiceProvider, CancellationToken, Task>>(
            body,
            notificationParam,
            providerParam,
            ctParam)
        .Compile();
    }

    /// <summary>
    /// Helper to build a foreach over a collection expression.
    /// </summary>
    /// <param name="itemType">Type of items in the collection.</param>
    /// <param name="collection">Expression for the collection.</param>
    /// <param name="loopVar">Loop variable expression.</param>
    /// <param name="body">Loop body expression.</param>
    /// <returns>An expression representing the foreach loop.</returns>
    private static Expression ForEach(Type itemType, Expression collection, ParameterExpression loopVar, Expression body)
    {
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(itemType);
        var enumeratorType = typeof(IEnumerator<>).MakeGenericType(itemType);

        var enumVar = Expression.Variable(enumeratorType, "enumerator");
        var getEnum = Expression.Call(collection, enumerableType.GetMethod(nameof(IEnumerable<object>.GetEnumerator))!);
        var assignEnum = Expression.Assign(enumVar, getEnum);

        var moveNext = Expression.Call(enumVar, typeof(System.Collections.IEnumerator).GetMethod(nameof(System.Collections.IEnumerator.MoveNext))!);
        var breakLabel = Expression.Label("LoopBreak");

        var loop = Expression.Loop(
            Expression.IfThenElse(
                Expression.IsFalse(moveNext),
                Expression.Break(breakLabel),
                Expression.Block(
                    new[] { loopVar },
                    Expression.Assign(loopVar, Expression.Property(enumVar, nameof(IEnumerator<object>.Current))),
                    body)),
            breakLabel);

        return Expression.Block(new[] { enumVar }, assignEnum, loop);
    }
}
