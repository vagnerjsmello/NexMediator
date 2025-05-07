using FluentValidation;
using Microsoft.Extensions.Logging;
using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Pipeline.Behaviors;

/// <summary>
/// Pipeline behavior that runs FluentValidation rules on the request.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class FluentValidationBehavior<TRequest, TResponse> : INexPipelineBehavior<TRequest, TResponse>
    where TRequest : INexRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<FluentValidationBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Create the validation behavior.
    /// </summary>
    /// <param name="validators">A list of validators from DI.</param>
    /// <param name="logger">Logger instance for messages.</param>
    public FluentValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<FluentValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Run all validators for the request. If any errors, throw exception.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="next">Next handler in the pipeline.</param>
    /// <param name="cancellationToken">Token to cancel the work.</param>
    /// <returns>The response, or throws if request is invalid.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // If no validators, skip validation
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);

        // Remove duplicated validators (by type)
        var distinctValidators = _validators
            .Where(v => v.GetType() != typeof(InlineValidator<TRequest>))
            .GroupBy(v => v.GetType())
            .Select(g => g.First())
            .ToList();

        _logger.LogDebug("Running {ValidatorCount} validators for request {RequestType}", distinctValidators.Count, typeof(TRequest).Name);

        // Run all validations in parallel
        var validationResults = await Task.WhenAll(distinctValidators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Collect all errors
        var failures = validationResults.Where(r => !r.IsValid).SelectMany(r => r.Errors).ToList();

        // If any validation failed, log and throw
        if (failures.Any())
        {
            _logger.LogWarning("Validation failed for {RequestType} with {ErrorCount} errors", typeof(TRequest).Name, failures.Count);
            throw new ValidationException("One or more validation errors occurred.", failures);
        }

        // All validations passed
        return await next();
    }
}
