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
    /// Service collection to register behaviors and context.
    /// </param>
    public NexMediatorOptions(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Adds a pipeline behavior with a specific execution order.
    /// Registers both interface and concrete type for DI.
    /// </summary>
    /// <param name="behaviorType">Open-generic behavior type.</param>
    /// <param name="order">Execution order (lower runs first).</param>
    /// <returns>Same <see cref="NexMediatorOptions"/> for chaining.</returns>
    public NexMediatorOptions AddBehavior(Type behaviorType, int order)
    {
        if (behaviorType == null)
            throw new ArgumentNullException(nameof(behaviorType));
        if (!behaviorType.IsGenericTypeDefinition)
            throw new ArgumentException("Type must be open generic.", nameof(behaviorType));
        if (!ImplementsPipelineBehavior(behaviorType))
            throw new ArgumentException("Type must implement INexPipelineBehavior<,>.", nameof(behaviorType));

        // Track execution order
        _behaviorMap[behaviorType] = new BehaviorRegistration(behaviorType, order);

        // Register open-generic interface and concrete type
        _services.AddTransient(typeof(INexPipelineBehavior<,>), behaviorType);
        _services.AddTransient(behaviorType);

        return this;
    }

    /// <summary>
    /// Adds a pipeline behavior, optionally enabling correlation support.
    /// Registers with scoped lifetime if logging, otherwise transient.
    /// </summary>
    /// <param name="behaviorType">Open-generic behavior type.</param>
    /// <param name="order">Execution order.</param>
    /// <param name="enableCorrelation">
    /// If true and logging behavior, registers correlation context and scoped behavior.
    /// </param>
    /// <returns>Same <see cref="NexMediatorOptions"/> for chaining.</returns>
    public NexMediatorOptions AddBehavior(Type behaviorType, int order, bool enableCorrelation)
    {
        if (behaviorType == null)
            throw new ArgumentNullException(nameof(behaviorType));

        if (enableCorrelation && behaviorType == LoggingBehaviorType)
        {
            _services.TryAddScoped<ICorrelationContext, CorrelationContext>();
            _services.AddScoped(behaviorType);
        }
        else
        {
            _services.AddTransient(behaviorType);
        }

        // Track execution order
        _behaviorMap[behaviorType] = new BehaviorRegistration(behaviorType, order);
        // Register open-generic interface
        _services.AddTransient(typeof(INexPipelineBehavior<,>), behaviorType);

        return this;
    }

    /// <summary>
    /// Adds a pipeline behavior of the specified generic type.
    /// </summary>
    /// <typeparam name="TBehavior">Open-generic behavior type.</typeparam>
    /// <param name="order">Execution order.</param>
    /// <returns>Same <see cref="NexMediatorOptions"/> for chaining.</returns>
    public NexMediatorOptions AddBehavior<TBehavior>(int order)
        where TBehavior : class
        => AddBehavior(typeof(TBehavior), order);

    /// <summary>
    /// Removes a previously added behavior and its DI registrations.
    /// </summary>
    /// <param name="behaviorType">Open-generic behavior type to remove.</param>
    /// <returns>Same <see cref="NexMediatorOptions"/> for chaining.</returns>
    public NexMediatorOptions RemoveBehavior(Type behaviorType)
    {
        if (behaviorType == null)
            throw new ArgumentNullException(nameof(behaviorType));

        if (_behaviorMap.Remove(behaviorType))
        {
            // Remove all service descriptors for this behavior
            var descriptors = _services
                .Where(d => d.ImplementationType == behaviorType)
                .ToList();

            foreach (var descriptor in descriptors)
            {
                _services.Remove(descriptor);
            }
        }

        return this;
    }

    /// <summary>
    /// Validates the current behavior configuration and returns warnings.
    /// </summary>
    /// <returns>List of warning messages.</returns>
    public IReadOnlyList<string> Validate()
    {
        var messages = new List<string>();

        // Check for duplicate execution orders
        var duplicateGroups = _behaviorMap.Values
            .GroupBy(b => b.Order)
            .Where(g => g.Count() > 1);
        foreach (var group in duplicateGroups)
        {
            var names = string.Join(", ", group.Select(b => b.BehaviorType.Name));
            messages.Add($"Multiple behaviors with order {group.Key}: {names}");
        }

        // Warn if built-in behaviors are out of recommended order
        if (HasBehaviorOutOfOrder(LoggingBehaviorType, 1)
            || HasBehaviorOutOfOrder(FluentValidationBehaviorType, 2)
            || HasBehaviorOutOfOrder(CachingBehaviorType, 3)
            || HasBehaviorOutOfOrder(TransactionBehaviorType, 4))
        {
            messages.Add(
                "Built-in behaviors are not in recommended order: " +
                "Logging (1), FluentValidation (2), Caching (3), Transaction (4)");
        }

        return messages;
    }

    /// <summary>
    /// Orders a list of behavior instances by their configured execution order.
    /// </summary>
    /// <param name="behaviors">Unordered behavior instances.</param>
    /// <returns>Ordered behaviors.</returns>
    public IEnumerable<object> OrderBehaviors(IEnumerable<object> behaviors)
        => behaviors.OrderBy(b =>
        {
            var type = b.GetType().IsGenericType
                ? b.GetType().GetGenericTypeDefinition()
                : b.GetType();
            return _behaviorMap.TryGetValue(type, out var reg)
                ? reg.Order
                : int.MaxValue;
        });

    /// <summary>
    /// Checks if an open-generic behavior type is registered.
    /// </summary>
    public bool HasBehavior(Type behaviorType)
    {
        if (behaviorType == null)
            throw new ArgumentNullException(nameof(behaviorType));
        return _behaviorMap.ContainsKey(behaviorType);
    }

    private bool ImplementsPipelineBehavior(Type type)
        => type.GetInterfaces()
               .Any(i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(INexPipelineBehavior<,>));

    private bool HasBehaviorOutOfOrder(Type behaviorType, int expectedOrder)
        => _behaviorMap.TryGetValue(behaviorType, out var reg)
           && reg.Order != expectedOrder;

    private record BehaviorRegistration(Type BehaviorType, int Order);
}