using ContactService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ContactService.ApplicationService.Behaviors;

/// <summary>
/// MediatR behavior to ensure correlation ID is available in all request handlers
/// </summary>
public class CorrelationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
{
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<CorrelationBehavior<TRequest, TResponse>> _logger;

    public CorrelationBehavior(
        ICorrelationContext correlationContext,
        ILogger<CorrelationBehavior<TRequest, TResponse>> logger)
    {
        _correlationContext = correlationContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var correlationId = _correlationContext.CorrelationId;
        
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestType"] = typeof(TRequest).Name,
            ["ResponseType"] = typeof(TResponse).Name
        }))
        {
            _logger.LogDebug("Handling {RequestType} with correlation ID: {CorrelationId}", 
                typeof(TRequest).Name, correlationId);
            
            var response = await next();
            
            _logger.LogDebug("Completed {RequestType} with correlation ID: {CorrelationId}", 
                typeof(TRequest).Name, correlationId);
            
            return response;
        }
    }
}
