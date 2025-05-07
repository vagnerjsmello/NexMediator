using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexMediator.Abstractions.Context;
using NexMediator.Abstractions.Interfaces;
using NexMediator.Pipeline.Behaviors;

namespace NexMediator.Core;

/// <summary>
/// Configuration options for NexMediator pipeline behaviors.
/// </summary>
/// <remarks>
/// Responsibilities:
/// - Register pipeline behaviors
/// - Control execution order
/// - Validate configuration
/// </remarks>
public class NexMediatorOptions
{
    private readonly IServiceCollection _services;
    private readonly Dictionary<Type, BehaviorRegistration> _behaviorMap = new();

    private static readonly Type LoggingBehaviorType = typeof(LoggingBehavior<,>);
    private static readonly Type FluentValidationBehaviorType = typeof(FluentValidationBehavior<,>);
    private static readonly Type CachingBehaviorType = typeof(CachingBehavior<,>);
    private static readonly Type TransactionBehaviorType = typeof(TransactionBehavior<,>);

    /// <summary>
    /// Create a new NexMediatorOptions instance.
    /// </summary>
    /// <param name="services">The service collection used for registration.</param>
    public NexMediatorOptions(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Adds a pipeline behavior with an execution order.
    /// </summary>
    /// <param name="behaviorType">Open generic type of the behavior.</param>
    /// <param name="order">Execution order (lower runs first).</param>
    /// <returns>The same options for chaining.</returns>
    public NexMediatorOptions AddBehavior(Type behaviorType, int order)
    {
        if (behaviorType == null)
            throw new ArgumentNullException(nameof(behaviorType));

        if (!behaviorType.IsGenericTypeDefinition)
            throw new ArgumentException($"Type {behaviorType.Name} must be a generic type definition.");

        if (!ImplementsPipelineBehavior(behaviorType))
            throw new ArgumentException($"Type {behaviorType.Name} must implement INexPipelineBehavior<,>.");

        _behaviorMap[behaviorType] = new BehaviorRegistration(behaviorType, order);
        _services.AddTransient(typeof(INexPipelineBehavior<,>), behaviorType);

        return this;
    }

    /// <summary>
    /// Adds a pipeline behavior with an execution order and optionally enables correlation context registration.
    /// </summary>
    /// <param name="behaviorType">Open generic type of the behavior (e.g., typeof(LoggingBehavior&lt;,&gt;)).</param>
    /// <param name="order">Execution order (lower runs first).</param>
    /// <param name="enableCorrelation">
    /// If true and the behavior is LoggingBehavior, registers the CorrelationContext in the DI container.
    /// </param>
    /// <returns>The same options for chaining.</returns>
    public NexMediatorOptions AddBehavior(Type behaviorType, int order, bool enableCorrelation)
    {
        if (behaviorType == null)
            throw new ArgumentNullException(nameof(behaviorType));

        // If this is the LoggingBehavior and correlation is requested, register the context
        if (enableCorrelation && behaviorType == LoggingBehaviorType)
        {
            // Only add if not already registered
            _services.TryAddScoped<ICorrelationContext, CorrelationContext>();
        }

        // Register the behavior normally
        _behaviorMap[behaviorType] = new BehaviorRegistration(behaviorType, order);
        _services.AddTransient(typeof(INexPipelineBehavior<,>), behaviorType);

        return this;
    }

    /// <summary>
    /// Adds a pipeline behavior with an execution order.
    /// </summary>
    /// <typeparam name="TBehavior">Behavior type (open generic).</typeparam>
    /// <param name="order">Execution order.</param>
    public NexMediatorOptions AddBehavior<TBehavior>(int order)
        where TBehavior : class
    {
        return AddBehavior(typeof(TBehavior), order);
    }

    /// <summary>
    /// Removes a registered behavior by type.
    /// </summary>
    /// <param name="behaviorType">Behavior type to remove.</param>
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
    /// Validates the pipeline configuration.
    /// </summary>
    /// <returns>List of warnings or problems.</returns>
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
            messages.Add("Built-in behaviors are not in recommended order. " +
                         "Expected: Logging (1), FluentValidation (2), Caching (3), Transaction (4)");
        }

        return messages.AsReadOnly();
    }

    /// <summary>
    /// Sorts pipeline behaviors according to configuration.
    /// </summary>
    /// <param name="behaviors">Unordered behaviors.</param>
    /// <returns>Ordered list of behaviors.</returns>
    internal IEnumerable<object> OrderBehaviors(IEnumerable<object> behaviors)
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

    private bool ImplementsPipelineBehavior(Type type)
    {
        return type.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(INexPipelineBehavior<,>));
    }

    private bool HasBehaviorOutOfOrder(Type behaviorType, int expectedOrder)
    {
        return _behaviorMap.TryGetValue(behaviorType, out var registration) &&
               registration.Order != expectedOrder;
    }

    private record BehaviorRegistration(Type BehaviorType, int Order);
}
