using Microsoft.Extensions.DependencyInjection;
using NexMediator.Abstractions.Interfaces;
using System.Collections.Concurrent;
using System.Linq.Expressions;


namespace NexMediator.Core.Internal;

/// <summary>
/// Publishes a notification to all handlers in parallel.
/// 
/// Optimization:
/// - Uses compiled delegates via Expression to avoid runtime reflection.
/// - Executes all handlers in parallel using Task.WhenAll.
/// - Caches dispatcher delegate per notification type.
/// </summary>
internal static class NexNotificationExecutor
{
    private static readonly ConcurrentDictionary<Type, Func<object, IServiceProvider, CancellationToken, Task>> _cache = new();

    /// <summary>
    /// B1: Publishes a notification to all handlers in parallel.
    /// </summary>
    public static Task Publish<TNotification>(TNotification notification, IServiceProvider provider, CancellationToken ct)
        where TNotification : INexNotification
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        var notificationType = typeof(TNotification);

        var dispatcher = _cache.GetOrAdd(notificationType, static type =>
        {
            var handlerInterfaceType = typeof(INexNotificationHandler<>).MakeGenericType(type);
            var handleMethod = handlerInterfaceType.GetMethod(nameof(INexNotificationHandler<INexNotification>.Handle))!;

            var notificationParam = Expression.Parameter(typeof(object), "notification");
            var providerParam = Expression.Parameter(typeof(IServiceProvider), "provider");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

            var castedNotification = Expression.Convert(notificationParam, type);

            var getServicesMethod = typeof(ServiceProviderServiceExtensions)
                                        .GetMethods()
                                        .First(m =>
                                            m.Name == nameof(ServiceProviderServiceExtensions.GetServices) &&
                                            m.IsGenericMethodDefinition &&
                                            m.GetParameters().Length == 1 &&
                                            m.GetParameters()[0].ParameterType == typeof(IServiceProvider))
                                        .MakeGenericMethod(handlerInterfaceType);


            var getHandlersCall = Expression.Call(getServicesMethod, providerParam);

            var handlersVar = Expression.Variable(typeof(IEnumerable<>).MakeGenericType(handlerInterfaceType), "handlers");
            var taskListVar = Expression.Variable(typeof(List<Task>), "taskList");
            var handlerVar = Expression.Variable(handlerInterfaceType, "handler");

            var taskListCtor = Expression.New(typeof(List<Task>));
            var assignTaskList = Expression.Assign(taskListVar, taskListCtor);

            var callHandle = Expression.Call(handlerVar, handleMethod, castedNotification, ctParam);
            var addToList = Expression.Call(taskListVar, nameof(List<Task>.Add), null, callHandle);

            var loop = ForEach(handlerInterfaceType, handlersVar, handlerVar, addToList);

            var whenAllMethod = typeof(Task).GetMethod(nameof(Task.WhenAll), new[] { typeof(IEnumerable<Task>) })!;
            var returnCall = Expression.Call(whenAllMethod, taskListVar);

            var body = Expression.Block(
                new[] { handlersVar, taskListVar, handlerVar },
                Expression.Assign(handlersVar, getHandlersCall),
                assignTaskList,
                loop,
                returnCall
            );

            var lambda = Expression.Lambda<Func<object, IServiceProvider, CancellationToken, Task>>(
                body,
                notificationParam,
                providerParam,
                ctParam
            );

            return lambda.Compile();
        });

        return dispatcher(notification!, provider, ct);
    }

    /// <summary>
    /// Builds a foreach loop expression: foreach (var item in collection) { body }
    /// </summary>
    private static Expression ForEach(Type itemType, Expression collection, ParameterExpression loopVar, Expression body)
    {
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(itemType);
        var enumeratorType = typeof(IEnumerator<>).MakeGenericType(itemType);

        var enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
        var getEnumeratorCall = Expression.Call(collection, enumerableType.GetMethod(nameof(IEnumerable<object>.GetEnumerator))!);
        var assignEnum = Expression.Assign(enumeratorVar, getEnumeratorCall);

        var moveNextCall = Expression.Call(enumeratorVar, typeof(System.Collections.IEnumerator).GetMethod(nameof(System.Collections.IEnumerator.MoveNext))!);
        var breakLabel = Expression.Label("LoopBreak");

        var loop = Expression.Loop(
            Expression.IfThenElse(
                Expression.IsFalse(moveNextCall),
                Expression.Break(breakLabel),
                Expression.Block(
                    new[] { loopVar },
                    Expression.Assign(loopVar, Expression.Property(enumeratorVar, nameof(IEnumerator<object>.Current))),
                    body
                )
            ),
            breakLabel
        );

        return Expression.Block(new[] { enumeratorVar }, assignEnum, loop);
    }
}
