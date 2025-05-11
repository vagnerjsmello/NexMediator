using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexMediator.Abstractions.Context;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Pipeline.Behaviors;

namespace NexMediator.Core;

/// <summary>
/// Configuration options for registering and ordering pipeline behaviors.
/// </summary>
public class NexMediatorOptions
{
    private readonly IServiceCollection _services;
    private readonly Dictionary<Type, BehaviorRegistration> _behaviorMap = new();

    private static readonly Type LoggingBehaviorType = typeof(LoggingBehavior<,>);
    private static readonly Type FluentValidationBehaviorType = typeof(FluentValidationBehavior<,>);
    private static readonly Type CachingBehaviorType = typeof(CachingBehavior<,>);
    private static readonly Type TransactionBehaviorType = typeof(TransactionBehavior<,>);

    /// <summary>
    /// Initializes a new instance of <see cref="NexMediatorOptions"/>.
    /// </summary>
    /// <param name="services">
    /// The service collection for registering behaviors and context.
    /// </param>
    public NexMediatorOptions(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Adds a pipeline behavior with a specific execution order.
    /// </summary>
    /// <param name="behaviorType">The open-generic type of the behavior.</param>
    /// <param name="order">The order in which the behavior runs (lower runs first).</param>
    /// <returns>The same <see cref="NexMediatorOptions"/> for chaining.</returns>
    public NexMediatorOptions AddBehavior(Type behaviorType, int order)
    {
        if (behaviorType == null)
            throw new ArgumentNullException(nameof(behaviorType));

        if (!behaviorType.IsGenericTypeDefinition)
            throw new ArgumentException("Type must be open generic.", nameof(behaviorType));

        if (!ImplementsPipelineBehavior(behaviorType))
            throw new ArgumentException("Type must implement INexPipelineBehavior<,>.", nameof(behaviorType));

        _behaviorMap[behaviorType] = new BehaviorRegistration(behaviorType, order);
        _services.AddTransient(typeof(INexPipelineBehavior<,>), behaviorType);

        return this;
    }

    /// <summary>
    /// Adds a pipeline behavior and optionally registers correlation context.
    /// </summary>
    /// <param name="behaviorType">The open-generic behavior type.</param>
    /// <param name="order">The run order for the behavior.</param>
    /// <param name="enableCorrelation">
    /// If true and behavior is <see cref="LoggingBehavior{,}"/>, adds <see cref="ICorrelationContext"/>.
    /// </param>
    /// <returns>The same <see cref="NexMediatorOptions"/> for chaining.</returns>
    public NexMediatorOptions AddBehavior(Type behaviorType, int order, bool enableCorrelation)
    {
        if (behaviorType == null)
            throw new ArgumentNullException(nameof(behaviorType));

        if (enableCorrelation && behaviorType == LoggingBehaviorType)
            _services.TryAddScoped<ICorrelationContext, CorrelationContext>();

        _behaviorMap[behaviorType] = new BehaviorRegistration(behaviorType, order);
        _services.AddTransient(typeof(INexPipelineBehavior<,>), behaviorType);

        return this;
    }

    /// <summary>
    /// Adds a pipeline behavior of type <typeparamref name="TBehavior"/>.
    /// </summary>
    /// <typeparam name="TBehavior">The open-generic behavior type.</typeparam>
    /// <param name="order">Execution order for this behavior.</param>
    /// <returns>The same <see cref="NexMediatorOptions"/> for chaining.</returns>
    public NexMediatorOptions AddBehavior<TBehavior>(int order)
        where TBehavior : class
    {
        return AddBehavior(typeof(TBehavior), order);
    }

    /// <summary>
    /// Removes a previously added behavior.
    /// </summary>
    /// <param name="behaviorType">The open-generic behavior type to remove.</param>
    /// <returns>The same <see cref="NexMediatorOptions"/> for chaining.</returns>
    public NexMediatorOptions RemoveBehavior(Type behaviorType)
    {
        if (behaviorType == null)
            throw new ArgumentNullException(nameof(behaviorType));

        if (_behaviorMap.Remove(behaviorType))
        {
            var descriptor = _services.FirstOrDefault(d =>
                d.ServiceType == typeof(INexPipelineBehavior<,>) &&
                d.ImplementationType == behaviorType);
            if (descriptor != null)
                _services.Remove(descriptor);
        }

        return this;
    }

    /// <summary>
    /// Validates the current behavior configuration.
    /// </summary>
    /// <returns>A read-only list of warnings if configuration issues exist.</returns>
    public IReadOnlyList<string> Validate()
    {
        var messages = new List<string>();

        var duplicateOrders = _behaviorMap.Values
            .GroupBy(b => b.Order)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateOrders)
        {
            var types = string.Join(", ", group.Select(b => b.BehaviorType.Name));
            messages.Add($"Multiple behaviors with order {group.Key}: {types}");
        }

        if (HasBehaviorOutOfOrder(LoggingBehaviorType, 1) ||
            HasBehaviorOutOfOrder(FluentValidationBehaviorType, 2) ||
            HasBehaviorOutOfOrder(CachingBehaviorType, 3) ||
            HasBehaviorOutOfOrder(TransactionBehaviorType, 4))
        {
            messages.Add("Built-in behaviors are not in recommended order: Logging (1), FluentValidation (2), Caching (3), Transaction (4)");
        }

        return messages;
    }

    /// <summary>
    /// Orders a list of behavior instances according to configured order.
    /// </summary>
    /// <param name="behaviors">Unordered behavior instances.</param>
    /// <returns>An ordered sequence of behaviors.</returns>
    public IEnumerable<object> OrderBehaviors(IEnumerable<object> behaviors)
    {
        return behaviors.OrderBy(b =>
        {
            var type = b.GetType().IsGenericType
                ? b.GetType().GetGenericTypeDefinition()
                : b.GetType();

            return _behaviorMap.TryGetValue(type, out var reg)
                ? reg.Order
                : int.MaxValue;
        });
    }

    /// <summary>
    /// Checks if a behavior of the given open-generic type is registered.
    /// </summary>
    /// <param name="behaviorType">The behavior type to check.</param>
    /// <returns>True if registered; otherwise false.</returns>
    public bool HasBehavior(Type behaviorType)
    {
        if (behaviorType == null)
            throw new ArgumentNullException(nameof(behaviorType));

        return _behaviorMap.ContainsKey(behaviorType);
    }

    /// <summary>
    /// Checks if <typeparamref name="TBehavior"/> is registered.
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type to check.</typeparam>
    /// <returns>True if registered; otherwise false.</returns>
    public bool HasBehavior<TBehavior>() where TBehavior : class
        => HasBehavior(typeof(TBehavior));

    private bool ImplementsPipelineBehavior(Type type)
        => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INexPipelineBehavior<,>));

    private bool HasBehaviorOutOfOrder(Type behaviorType, int expectedOrder)
        => _behaviorMap.TryGetValue(behaviorType, out var reg) && reg.Order != expectedOrder;

    private record BehaviorRegistration(Type BehaviorType, int Order);
}
